// CudaCore.h

#pragma once

#include <time.h>

namespace CudaCore {

	using namespace System;

	public ref class CudaInterFace
	{
		// TODO: 在此加入這個類別的方法。
	public:
		CudaInterFace();
		~CudaInterFace();		

		/*static System::String^ GetLastErrorMessage() {
			const char* nativeMsg = GetLastCudaError();
			return gcnew System::String(nativeMsg);
		}

		static void ClearLastErrorMessage() {
			SetLastCudaError("");
		}*/

		String^ CudaGetLastError();
		void	CudaClearLastError();
		bool	CudaInitDevice( int gpuindex);
		void	CudaSetFFTConfig(int tidnumber, int ImageWidth, int ImageHeight, int ScaleValue, int ThresholdValue, int MaskWidth, int MaskHeight, bool UseEllipse);
		void	CudaExcuteFFT2D(array<float>^ input, array<float>^ output, array<float>^ offtimage, array<float>^ obinarizeimage, array<float>^ omaskimage);
	
	};
}