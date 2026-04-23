#include "Comm.h"
#include <cufft.h>

extern "C" {

	// ===== Kernels =====

	// 將 host 上傳的 float 影像在 device 端做 checkerboard，並初始化成 FP32 複數頻譜
	__global__ void InitComplexWithCheckerboardKernel(const float* input, cuFloatComplex* output, int width, int height) {
		int idx = blockIdx.x * blockDim.x + threadIdx.x;
		int size = width * height;
		if (idx < size) {
			int x = idx % width;
			int y = idx / width;
			float v = ((x + y) % 2 == 0) ? input[idx] : -input[idx];
			output[idx] = make_cuFloatComplex(v, 0.0f);
		}
	}

	// 計算 log(1 + magnitude) 的全域最大值（用 float-as-int 的 atomicMax）
	__global__ void FFTLogMagMaxKernel(const cuFloatComplex* fft, int size, int* maxValInt) {
		int idx = blockIdx.x * blockDim.x + threadIdx.x;
		if (idx < size) {
			float re = fft[idx].x;
			float im = fft[idx].y;
			float mag = sqrtf(re * re + im * im);
			float val = logf(1.0f + mag);
			atomicMax(maxValInt, __float_as_int(val));
		}
	}

	// 在頻域即時計算 Normalize、Binarize、幾何遮罩，並乘回 d_fft
	// 同時可選擇輸出 FFTImage / BinarizeImage / MaskImage（若指標為 nullptr 則略過）
	__global__ void ApplyMaskInlineKernel(
		cuFloatComplex* fft,
		int width, int height,
		float maxVal, float scaleVal, int thresholdVal,
		int cx, int cy, int maskW, int maskH, bool useEllipse,
		float* optOutFFT, float* optOutBin, float* optOutMask
	) {
		int idx = blockIdx.x * blockDim.x + threadIdx.x;
		int size = width * height;
		if (idx >= size) return;

		int x = idx % width;
		int y = idx / width;

		// 頻域強度與 log-magnitude
		float re = fft[idx].x;
		float im = fft[idx].y;
		float mag = sqrtf(re * re + im * im);
		float logmag = logf(1.0f + mag);

		// Normalize 到 [0, scaleVal]
		float norm = (maxVal > 0.0f) ? fminf(fmaxf(logmag / maxVal * scaleVal, 0.0f), scaleVal) : 0.0f;

		if (optOutFFT) optOutFFT[idx] = norm;

		// 閾值化（norm > Threshold → 0，否則 1）
		float bin = (norm > (float)thresholdVal) ? 0.0f : 1.0f;
		if (optOutBin) optOutBin[idx] = bin;

		// 幾何遮罩：矩形 + 可選橢圓
		bool inRect = (abs(x - cx) <= maskW / 2) && (abs(y - cy) <= maskH / 2);
		float rect = inRect ? 1.0f : bin;

		float finalMask = rect;
		if (useEllipse) {
			float dx = (float)(x - cx);
			float dy = (float)(y - cy);
			float a = maskW / 2.0f;
			float b = maskH / 2.0f;
			bool inEllipse = ((dx * dx) / (a * a) + (dy * dy) / (b * b)) <= 1.0f;
			float ellipse = inEllipse ? 1.0f : 0.0f;
			finalMask = rect * ellipse;
		}
		if (optOutMask) optOutMask[idx] = finalMask;

		// 直接套用遮罩到頻譜
		fft[idx].x *= finalMask;
		fft[idx].y *= finalMask;
	}

	// IFFT 後結果正規化 + 取消 checkerboard（將頻域 checkerboard 的效果還原）
	__global__ void NormalizeIFFTAndUncheckerboardKernel(const cuFloatComplex* input, float* output, int width, int height) {
		int idx = blockIdx.x * blockDim.x + threadIdx.x;
		int size = width * height;
		if (idx < size) {
			int x = idx % width;
			int y = idx / width;
			float v = input[idx].x / (float)size; // cuFFT IFFT 不自動正規化
												  // 還原 checkerboard（空間域反相）
			v *= ((x + y) % 2 == 0) ? 1.0f : -1.0f;
			output[idx] = v;
		}
	}

	// ===== Host-side functions =====

	bool InitDevice(int gpuindex)
	{
		cudaDeviceProp dp;
		int DevCnt = 0;

		if (cudaGetDeviceCount(&DevCnt)) {
			SetLastCudaError("GetDeviceCountFailed");
			return false;
		}

		if (DevCnt >= 1 && DevCnt > gpuindex)
		{
			if (cudaGetDeviceProperties(&dp, gpuindex)) {
				SetLastCudaError("GetDeviceNameFailed");
				return false;
			}

			if (cudaSetDevice(gpuindex)) {
				SetLastCudaError("SetDeviceFailed");
				return false;
			}
		}
		else
		{
			SetLastCudaError("NoDevice");
			return false;
		}

		return true;
	}

	void SetFFTConfig(int tidnumber, int ImageWidth, int ImageHeight, int ScaleValue, int ThresholdValue, int MaskWidth, int MaskHeight, bool UseEllipse) {
		THREAD_NUM = tidnumber;
		BLOCK_NUM = ((ImageWidth * ImageHeight) + THREAD_NUM - 1) / THREAD_NUM;
		IMAGE_WIDTH = ImageWidth;
		IMAGE_HEIGHT = ImageHeight;
		SCALE_VAL = static_cast<float>(ScaleValue);
		THRESHOLD_VAL = ThresholdValue;
		MASK_WIDTH = MaskWidth;
		MASK_HEIGHT = MaskHeight;
		USE_ELLIPSE = UseEllipse;
	}

	void ExcuteFFT2D(float *ImageInput, float *ImageOutput, float* FFTImage, float *BinarizeImage, float* MaskImage)
	{
		const int ImgSize = IMAGE_WIDTH * IMAGE_HEIGHT;
		const int ImgCenterX = IMAGE_WIDTH / 2;
		const int ImgCenterY = IMAGE_HEIGHT / 2;

		// 必要的持久緩衝：僅 d_fft + d_maxValInt + FFT plan
		cuFloatComplex *d_fft		= nullptr;
		int            *d_maxValInt = nullptr;

		cudaMalloc(&d_fft, sizeof(cuFloatComplex) * ImgSize);
		cudaMalloc(&d_maxValInt, sizeof(int));
		cudaMemset(d_maxValInt, 0, sizeof(int));

		cufftHandle plan;
		cufftPlan2d(&plan, IMAGE_HEIGHT, IMAGE_WIDTH, CUFFT_C2C);

		// 暫時 staging：上傳影像到 device 並完成 checkerboard + init complex，完成後釋放
		float *d_stage = nullptr;
		cudaMalloc(&d_stage, sizeof(float) * ImgSize);
		cudaMemcpy(d_stage, ImageInput, sizeof(float) * ImgSize, cudaMemcpyHostToDevice);
		
		InitComplexWithCheckerboardKernel << <BLOCK_NUM, THREAD_NUM >> >(d_stage, d_fft, IMAGE_WIDTH, IMAGE_HEIGHT);
		// 立刻釋放，不持久佔用
		cudaFree(d_stage);

		// FFT
		cufftExecC2C(plan, d_fft, d_fft, CUFFT_FORWARD);

		// Pass-1：計算 log-magnitude 最大值
		FFTLogMagMaxKernel << <BLOCK_NUM, THREAD_NUM >> >(d_fft, ImgSize, d_maxValInt);
		int   maxValInt = 0;
		float maxVal = 0.0f;
		cudaMemcpy(&maxValInt, d_maxValInt, sizeof(int), cudaMemcpyDeviceToHost);
		memcpy(&maxVal, &maxValInt, sizeof(float));

		// 準備可選輸出緩衝（臨時，僅在需要時分配）
		float *d_fft_out = nullptr;
		float *d_bin_out = nullptr;
		float *d_mask_out = nullptr;

		if (FFTImage)      cudaMalloc(&d_fft_out, sizeof(float) * ImgSize);
		if (BinarizeImage) cudaMalloc(&d_bin_out, sizeof(float) * ImgSize);
		if (MaskImage)     cudaMalloc(&d_mask_out, sizeof(float) * ImgSize);

		// Pass-2：頻域即時 Normalize + Binarize + 幾何遮罩 + 乘回 d_fft
		ApplyMaskInlineKernel << <BLOCK_NUM, THREAD_NUM >> >(d_fft,	IMAGE_WIDTH, IMAGE_HEIGHT,	maxVal, SCALE_VAL, THRESHOLD_VAL, ImgCenterX, ImgCenterY, MASK_WIDTH, MASK_HEIGHT, USE_ELLIPSE, d_fft_out, d_bin_out, d_mask_out );

		// 可選輸出：拷回主機後立刻釋放
		if (FFTImage) {
			cudaMemcpy(FFTImage, d_fft_out, sizeof(float) * ImgSize, cudaMemcpyDeviceToHost);
			cudaFree(d_fft_out);
		}

		if (BinarizeImage) {
			cudaMemcpy(BinarizeImage, d_bin_out, sizeof(float) * ImgSize, cudaMemcpyDeviceToHost);
			cudaFree(d_bin_out);
		}

		if (MaskImage) {
			cudaMemcpy(MaskImage, d_mask_out, sizeof(float) * ImgSize, cudaMemcpyDeviceToHost);
			cudaFree(d_mask_out);
		}

		// IFFT
		cufftExecC2C(plan, d_fft, d_fft, CUFFT_INVERSE);

		// 最終輸出（Normalize + 取消 checkerboard）→ 臨時緩衝 d_out，拷回主機後釋放
		float *d_out = nullptr;
		cudaMalloc(&d_out, sizeof(float) * ImgSize);
		NormalizeIFFTAndUncheckerboardKernel << <BLOCK_NUM, THREAD_NUM >> >(d_fft, d_out, IMAGE_WIDTH, IMAGE_HEIGHT);
		cudaMemcpy(ImageOutput, d_out, sizeof(float) * ImgSize, cudaMemcpyDeviceToHost);
		cudaFree(d_out);

		// 釋放持久資源
		cufftDestroy(plan);
		cudaFree(d_fft);
		cudaFree(d_maxValInt);
	}
}
