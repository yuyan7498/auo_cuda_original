#pragma once

#include <cuda.h>
#include <cufft.h>
#include <cuComplex.h>
#include <cuda_runtime.h>
#include <string>

#define COPY_MAX_PATH 255

int		BLOCK_NUM;
int		THREAD_NUM;

float	SCALE_VAL;
int		THRESHOLD_VAL;

int		IMAGE_WIDTH;
int		IMAGE_HEIGHT;
int		MASK_WIDTH;
int		MASK_HEIGHT;

bool	USE_ELLIPSE;

static std::string g_lastCudaError;

void SetLastCudaError(const char* msg) {
	g_lastCudaError = msg;
}

extern "C" {	
	
	const char* GetLastCudaError() {
		return g_lastCudaError.c_str();
	}

	extern "C" void ClearLastCudaError() {
		g_lastCudaError.clear();
	}

	bool InitDevice(int gpuindex);
	void SetFFTConfig(int tidnumber, int ImgeWidth, int ImageHeight, int ScaleValue, int ThresholdValue, int MaskWidth, int MaskHeight, bool UseEllipse);
	void ExcuteFFT2D(float* ImageInput, float* ImageOutput,float* FFTImage, float* BinarizeImage, float* MaskImage);
}