// 這是主要 DLL 檔案。

#include "stdafx.h"
#include "CudaCore.h"

extern "C" const char* GetLastCudaError();
extern "C" void ClearLastCudaError();
extern "C" bool InitDevice(int gpuinex);
extern "C" void SetFFTConfig(int tidnumber, int ImageWidth, int ImageHeight, int ScaleValue, int ThresholdValue, int MaskWidth, int MaskHeight, bool UseEllipse);
extern "C" void ExcuteFFT2D(float *ImageInput, float *ImageOutput, float* FFTImage, float *BinarizeImage, float* MaskImage);

using namespace CudaCore;

CudaInterFace::CudaInterFace(){}
CudaInterFace::~CudaInterFace(){}

System::String^ CudaInterFace::CudaGetLastError() {
	const char* nativeMsg = GetLastCudaError();
	return gcnew System::String(nativeMsg);
}

void CudaInterFace::CudaClearLastError() {
	ClearLastCudaError();
}

bool CudaInterFace::CudaInitDevice(int gpuindex)
{	
	return InitDevice(gpuindex);
}

void CudaInterFace::CudaSetFFTConfig(int tidnumber, int ImageWidth, int ImageHeight, int ScaleValue, int ThresholdValue, int MaskWidth, int MaskHeight, bool UseEllipse)
{
	SetFFTConfig(tidnumber, ImageWidth, ImageHeight, ScaleValue, ThresholdValue, MaskWidth, MaskHeight, UseEllipse);
}

void CudaInterFace::CudaExcuteFFT2D(array<float>^ input, array<float>^ output, array<float>^ offtimage, array<float>^ obinarizeimage, array<float>^ omaskimage)
{
	pin_ptr<float> pInput = &input[0];
	pin_ptr<float> pOutput = &output[0];
	pin_ptr<float> pFFT;
	pin_ptr<float> pBinarize;
	pin_ptr<float> pMask;

	if (offtimage != nullptr)
		pFFT = &offtimage[0];
	else
		pFFT = nullptr;
	
	if (obinarizeimage != nullptr)
		pBinarize = &obinarizeimage[0];
	else
		pBinarize = nullptr;

	if (omaskimage != nullptr)
		pMask = &omaskimage[0];
	else
		pMask = nullptr;

	ExcuteFFT2D(pInput, pOutput, pFFT, pBinarize, pMask);
}
