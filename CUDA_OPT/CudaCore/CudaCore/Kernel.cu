// Kernel.cu - Optimized CUDA FFT pipeline for CUDA_OPT
// Keeps the exact same public C API as the original CudaCore DLL
// (InitDevice / SetFFTConfig / ExcuteFFT2D / GetLastCudaError / ClearLastCudaError)
// so existing VB callers (TuneForm, SimIP, FunctionIF) work without modification.
//
// Summary of optimizations vs original Kernel.cu:
//   1. Persistent cuFFT plan            (avoids ~54 ms plan creation per call)
//   2. Persistent device memory pool    (avoids 4-18 ms cudaMalloc/Free per call)
//   3. Automatic smooth-prime padding   (avoids cuFFT Bluestein slow path on
//                                        prime dimensions like 14192 = 2^4 * 887)
//   4. Skipped intermediate logMag buffer: magnitude max is reduced on the raw
//      squared magnitude; ApplyMask recomputes log(1+sqrt()) inline - saves one
//      full pass over a padded-size float buffer (~1 GB less memory traffic).
//   5. Warp-shuffle + shared-mem reduction: 1 atomicMax per block instead of
//      per thread (~256x fewer atomics).
//   6. Device-resident max value: ApplyMask reads d_maxSqMag directly from
//      device memory - no CPU round-trip / Stream sync between reduction
//      and mask application.
//   7. Compute + Copy streams with event sync: IFFT runs on compute stream
//      concurrently with diagnostic D2H copies on copy stream.
//   8. Persistent pinned (page-locked) host staging buffers + async memcpy:
//      lets cudaMemcpyAsync actually run asynchronously (pageable copies
//      block the CPU until DMA completes, defeating stream overlap).
//   9. Fused init kernel writes zero-pad region and checkerboard-initialised
//      complex values in a single pass.

#include "Comm.h"

#include <cufft.h>
#include <cuComplex.h>
#include <cuda_runtime.h>

#include <cmath>
#include <cstring>
#include <string>

// ==============================================================================
// Internal state
// ==============================================================================

namespace {

// Auto-computed padded FFT dimensions (smooth radix: factors in {2,3,5,7})
int s_padW = 0;
int s_padH = 0;
int s_padBlocks  = 1;   // launch config for padW*padH element kernels
int s_origBlocks = 1;   // launch config for origW*origH element kernels

// Persistent GPU resources
cufftHandle  s_plan    = 0;
int          s_planW   = 0;
int          s_planH   = 0;

char*        s_pool    = nullptr;
size_t       s_poolSz  = 0;

cudaStream_t s_stmCompute = nullptr;
cudaStream_t s_stmCopy    = nullptr;
cudaEvent_t  s_evMaskDone = nullptr;

// Persistent pinned host buffers
float*  s_hIn        = nullptr;
size_t  s_hInElems   = 0;
float*  s_hOut       = nullptr;
size_t  s_hOutElems  = 0;
float*  s_hFFT       = nullptr;
size_t  s_hFFTElems  = 0;
float*  s_hBin       = nullptr;
size_t  s_hBinElems  = 0;
float*  s_hMask      = nullptr;
size_t  s_hMaskElems = 0;

// Error helpers ----------------------------------------------------------------
inline void SetErr(const std::string& m) { g_lastCudaError = m; }

inline bool ChkCuda(cudaError_t e, const char* expr, const char* f, int l) {
    if (e == cudaSuccess) return true;
    SetErr(std::string("CUDA: ") + expr + " @ " + f + ":" +
           std::to_string(l) + " -- " + cudaGetErrorString(e));
    return false;
}
inline bool ChkCufft(cufftResult e, const char* expr, const char* f, int l) {
    if (e == CUFFT_SUCCESS) return true;
    SetErr(std::string("CUFFT: ") + expr + " @ " + f + ":" +
           std::to_string(l) + " code=" + std::to_string((int)e));
    return false;
}

#define CC(x)  do { if (!ChkCuda ((x), #x, __FILE__, __LINE__)) goto CLEANUP; } while(0)
#define CF(x)  do { if (!ChkCufft((x), #x, __FILE__, __LINE__)) goto CLEANUP; } while(0)

// Smallest m >= n whose only prime factors are in {2, 3, 5, 7}.
// cuFFT runs radix-2/3/5/7 at full speed; other primes (esp. 887 in 14192)
// fall through to Bluestein and are ~3x slower.
int NextSmooth(int n) {
    if (n <= 1) return 1;
    for (int m = n; ; ++m) {
        int v = m;
        while ((v & 1) == 0) v >>= 1;
        while (v % 3 == 0) v /= 3;
        while (v % 5 == 0) v /= 5;
        while (v % 7 == 0) v /= 7;
        if (v == 1) return m;
    }
}

// Round up block count
inline int DivUp(int a, int b) { return (a + b - 1) / b; }

} // anonymous namespace

// ==============================================================================
// Device kernels
// ==============================================================================

extern "C" {

// Kernel 1: copy origW x origH input into padW x padH complex array with
// checkerboard sign-flip. Pixels outside the image are left as zero (zero-pad).
__global__ void k_InitCheckerPad(
    const float* __restrict__ src,
    cuFloatComplex* __restrict__ dst,
    int origW, int origH, int padW, int padH)
{
    const int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= padW * padH) return;
    const int px = idx % padW;
    const int py = idx / padW;
    float v = 0.0f;
    if (px < origW && py < origH) {
        v = __ldg(&src[py * origW + px]);
        if ((px + py) & 1) v = -v;
    }
    dst[idx] = make_cuFloatComplex(v, 0.0f);
}

// Kernel 2: max reduction of squared magnitude.
// log(1 + sqrt(x)) is monotonic in x, so argmax of logmag == argmax of sqmag.
// We defer the log/sqrt to ApplyMask so we don't spend transcendentals here.
__global__ void k_SqMagMax(
    const cuFloatComplex* __restrict__ fft,
    int size,
    int* __restrict__ d_maxSqMagInt)
{
    const int idx    = blockIdx.x * blockDim.x + threadIdx.x;
    const int tid    = threadIdx.x;
    const int warpId = tid >> 5;
    const int laneId = tid & 31;

    float val = 0.0f;
    if (idx < size) {
        const float re = fft[idx].x;
        const float im = fft[idx].y;
        val = re * re + im * im;
    }

    // Warp reduction
    #pragma unroll
    for (int off = 16; off > 0; off >>= 1)
        val = fmaxf(val, __shfl_down_sync(0xffffffff, val, off));

    __shared__ float smem[32];
    if (laneId == 0) smem[warpId] = val;
    __syncthreads();

    if (warpId == 0) {
        const int warps = (blockDim.x + 31) >> 5;
        val = (laneId < warps) ? smem[laneId] : 0.0f;
        #pragma unroll
        for (int off = 16; off > 0; off >>= 1)
            val = fmaxf(val, __shfl_down_sync(0xffffffff, val, off));
        if (laneId == 0)
            atomicMax(d_maxSqMagInt, __float_as_int(val));
    }
}

// Kernel 3: apply mask in padded domain. Recomputes log(1+sqrt(sqmag)) inline.
//   - Reads d_maxSqMagInt from device memory (no CPU sync).
//   - Writes diagnostic outputs (origW x origH) only for pixels within the
//     original image region so caller's W*H buffers are untouched elsewhere.
__global__ void k_ApplyMaskFused(
    cuFloatComplex* __restrict__ fft,
    const int*   __restrict__ d_maxSqMagInt,
    int padW, int padH, int origW, int origH,
    float scaleVal, float thresholdVal,
    int cx, int cy, float hw, float hh, int useEllipse,
    float* outFFT, float* outBin, float* outMask)
{
    const int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= padW * padH) return;

    // Broadcast: every thread reads the same int, goes through read-only cache
    const float maxSqMag  = __int_as_float(__ldg(d_maxSqMagInt));
    const float maxLogMag = logf(1.0f + sqrtf(maxSqMag));

    const int px = idx % padW;
    const int py = idx / padW;

    const float re = fft[idx].x;
    const float im = fft[idx].y;
    const float sqmag  = re * re + im * im;
    const float logmag = logf(1.0f + sqrtf(sqmag));

    float norm = 0.0f;
    if (maxLogMag > 0.0f)
        norm = fminf(fmaxf((logmag / maxLogMag) * scaleVal, 0.0f), scaleVal);

    const float bin = (norm > thresholdVal) ? 0.0f : 1.0f;

    const float dx = (float)(px - cx);
    const float dy = (float)(py - cy);
    const bool  inRect   = (fabsf(dx) <= hw) && (fabsf(dy) <= hh);
    const float rectMask = inRect ? 1.0f : bin;

    float finalMask = rectMask;
    if (useEllipse) {
        const bool inEllipse = (hw > 0.0f && hh > 0.0f) &&
            ((dx * dx) / (hw * hw) + (dy * dy) / (hh * hh)) <= 1.0f;
        finalMask = rectMask * (inEllipse ? 1.0f : 0.0f);
    }

    // Diagnostic outputs (origW x origH) - only write for original region pixels
    if (px < origW && py < origH) {
        const int oIdx = py * origW + px;
        if (outFFT)  outFFT [oIdx] = norm;
        if (outBin)  outBin [oIdx] = bin;
        if (outMask) outMask[oIdx] = finalMask;
    }

    fft[idx].x *= finalMask;
    fft[idx].y *= finalMask;
}

// Kernel 4: normalise IFFT + undo checkerboard + crop padded to orig dims.
__global__ void k_NormalizeCrop(
    const cuFloatComplex* __restrict__ in,
    float* __restrict__ out,
    int origW, int origH, int padW, int padH)
{
    const int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= origW * origH) return;
    const int x = idx % origW;
    const int y = idx / origW;
    const int padIdx = y * padW + x;
    float v = in[padIdx].x / (float)(padW * padH);
    if ((x + y) & 1) v = -v;
    out[idx] = v;
}

// ==============================================================================
// Host-side API (extern "C", signatures identical to original)
// ==============================================================================

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

void SetFFTConfig(int tidnumber, int ImageWidth, int ImageHeight,
                  int ScaleValue, int ThresholdValue,
                  int MaskWidth, int MaskHeight, bool UseEllipse)
{
    THREAD_NUM    = (tidnumber > 0) ? tidnumber : 256;
    // Ensure block size is a multiple of 32 (warp) for the reduction kernel.
    if (THREAD_NUM & 31) THREAD_NUM = (THREAD_NUM + 31) & ~31;

    IMAGE_WIDTH   = ImageWidth;
    IMAGE_HEIGHT  = ImageHeight;
    SCALE_VAL     = (float)ScaleValue;
    THRESHOLD_VAL = ThresholdValue;
    MASK_WIDTH    = MaskWidth;
    MASK_HEIGHT   = MaskHeight;
    USE_ELLIPSE   = UseEllipse;

    // Automatic smooth-radix padding. If the image dimensions already factor
    // into {2,3,5,7} the pad is a no-op (padW == origW).
    s_padW = NextSmooth(ImageWidth);
    s_padH = NextSmooth(ImageHeight);

    BLOCK_NUM    = DivUp(ImageWidth * ImageHeight, THREAD_NUM);
    s_padBlocks  = DivUp(s_padW * s_padH, THREAD_NUM);
    s_origBlocks = DivUp(ImageWidth * ImageHeight, THREAD_NUM);
}

// Helper: ensure pinned buffer is large enough
static bool EnsurePinned(float** buf, size_t* curElems, size_t needElems) {
    if (*curElems >= needElems) return true;
    if (*buf) { cudaFreeHost(*buf); *buf = nullptr; }
    cudaError_t e = cudaMallocHost(buf, sizeof(float) * needElems);
    if (e != cudaSuccess) {
        SetErr(std::string("cudaMallocHost: ") + cudaGetErrorString(e));
        *curElems = 0;
        return false;
    }
    *curElems = needElems;
    return true;
}

void ExcuteFFT2D(float* ImageInput, float* ImageOutput,
                 float* FFTImage, float* BinarizeImage, float* MaskImage)
{
    if (!ImageInput || !ImageOutput) {
        SetLastCudaError("ImageInput or ImageOutput is nullptr");
        return;
    }
    if (IMAGE_WIDTH <= 0 || IMAGE_HEIGHT <= 0 || s_padW <= 0 || s_padH <= 0) {
        SetLastCudaError("Invalid image size - call SetFFTConfig first");
        return;
    }
    if (THREAD_NUM <= 0) {
        SetLastCudaError("Invalid thread config");
        return;
    }

    const int    origN   = IMAGE_WIDTH * IMAGE_HEIGHT;
    const int    padN    = s_padW * s_padH;
    const size_t szFOrig = (size_t)sizeof(float) * origN;
    const size_t szCPad  = (size_t)sizeof(cuFloatComplex) * padN;
    const size_t szI     = (size_t)sizeof(int);
    const size_t ALN     = 256UL;
    #define A256(x) (((x) + ALN - 1) & ~(ALN - 1))

    // Mask parameters scaled to padded domain (preserves normalised frequency)
    const int   cX_pad = s_padW / 2;
    const int   cY_pad = s_padH / 2;
    const int   mW_pad = (int)((long long)MASK_WIDTH  * s_padW / IMAGE_WIDTH);
    const int   mH_pad = (int)((long long)MASK_HEIGHT * s_padH / IMAGE_HEIGHT);
    const float hw_pad = mW_pad * 0.5f;
    const float hh_pad = mH_pad * 0.5f;

    cuFloatComplex* d_fft       = nullptr;
    float*          d_stage     = nullptr;
    int*            d_maxSqMag  = nullptr;
    float*          d_fftOut    = nullptr;
    float*          d_binOut    = nullptr;
    float*          d_maskOut   = nullptr;
    float*          d_output    = nullptr;

    // Pool layout: fft (padN complex), stage (origN float), maxInt,
    // output (origN float), optional diagnostics (origN float each)
    size_t total = A256(szCPad) + A256(szFOrig) + A256(szI) + A256(szFOrig);
    if (FFTImage)       total += A256(szFOrig);
    if (BinarizeImage)  total += A256(szFOrig);
    if (MaskImage)      total += A256(szFOrig);

    // Streams (once) - use non-blocking flag to avoid implicit sync with stream 0
    if (!s_stmCompute) {
        CC(cudaStreamCreateWithFlags(&s_stmCompute, cudaStreamNonBlocking));
        CC(cudaStreamCreateWithFlags(&s_stmCopy,    cudaStreamNonBlocking));
    }

    // Event (once)
    if (!s_evMaskDone) {
        CC(cudaEventCreateWithFlags(&s_evMaskDone, cudaEventDisableTiming));
    }

    // cuFFT plan (recreated only when padded dims change)
    if (s_plan == 0 || s_planW != s_padW || s_planH != s_padH) {
        if (s_plan) { cufftDestroy(s_plan); s_plan = 0; }
        CF(cufftPlan2d(&s_plan, s_padH, s_padW, CUFFT_C2C));
        CF(cufftSetStream(s_plan, s_stmCompute));
        s_planW = s_padW;
        s_planH = s_padH;
    }

    // Device memory pool (regrown only when needed)
    if (!s_pool || s_poolSz < total) {
        if (s_pool) { cudaFree(s_pool); s_pool = nullptr; }
        CC(cudaMalloc(&s_pool, total));
        s_poolSz = total;
    }

    // Pinned host buffers for input + output (always); diagnostics on demand
    if (!EnsurePinned(&s_hIn,  &s_hInElems,  origN)) goto CLEANUP;
    if (!EnsurePinned(&s_hOut, &s_hOutElems, origN)) goto CLEANUP;
    if (FFTImage)      { if (!EnsurePinned(&s_hFFT,  &s_hFFTElems,  origN)) goto CLEANUP; }
    if (BinarizeImage) { if (!EnsurePinned(&s_hBin,  &s_hBinElems,  origN)) goto CLEANUP; }
    if (MaskImage)     { if (!EnsurePinned(&s_hMask, &s_hMaskElems, origN)) goto CLEANUP; }

    {
        size_t off = 0;
        d_fft      = reinterpret_cast<cuFloatComplex*>(s_pool + off); off += A256(szCPad);
        d_stage    = reinterpret_cast<float*>(s_pool + off);           off += A256(szFOrig);
        d_maxSqMag = reinterpret_cast<int*>(s_pool + off);             off += A256(szI);
        if (FFTImage)       { d_fftOut  = reinterpret_cast<float*>(s_pool + off); off += A256(szFOrig); }
        if (BinarizeImage)  { d_binOut  = reinterpret_cast<float*>(s_pool + off); off += A256(szFOrig); }
        if (MaskImage)      { d_maskOut = reinterpret_cast<float*>(s_pool + off); off += A256(szFOrig); }
        d_output   = reinterpret_cast<float*>(s_pool + off);
    }

    // ------------------ Pipeline ------------------

    // Reset max accumulator on device
    CC(cudaMemsetAsync(d_maxSqMag, 0, szI, s_stmCompute));

    // Stage input: pageable -> pinned (CPU memcpy) then truly async H2D
    std::memcpy(s_hIn, ImageInput, szFOrig);
    CC(cudaMemcpyAsync(d_stage, s_hIn, szFOrig,
                       cudaMemcpyHostToDevice, s_stmCompute));

    // Init checkerboard + zero-pad -> padded complex array
    k_InitCheckerPad<<<s_padBlocks, THREAD_NUM, 0, s_stmCompute>>>(
        d_stage, d_fft, IMAGE_WIDTH, IMAGE_HEIGHT, s_padW, s_padH);
    CC(cudaGetLastError());

    // Forward FFT (padded size -> only small-prime radices)
    CF(cufftExecC2C(s_plan, d_fft, d_fft, CUFFT_FORWARD));

    // Max of squared magnitude via warp-level reduction
    k_SqMagMax<<<s_padBlocks, THREAD_NUM, 0, s_stmCompute>>>(
        d_fft, padN, d_maxSqMag);
    CC(cudaGetLastError());

    // Apply mask (reads device-resident max; no CPU sync)
    k_ApplyMaskFused<<<s_padBlocks, THREAD_NUM, 0, s_stmCompute>>>(
        d_fft, d_maxSqMag,
        s_padW, s_padH, IMAGE_WIDTH, IMAGE_HEIGHT,
        SCALE_VAL, (float)THRESHOLD_VAL,
        cX_pad, cY_pad, hw_pad, hh_pad, USE_ELLIPSE ? 1 : 0,
        d_fftOut, d_binOut, d_maskOut);
    CC(cudaGetLastError());

    // Signal: mask work done
    CC(cudaEventRecord(s_evMaskDone, s_stmCompute));

    // Queue IFFT + normalize on compute stream BEFORE the diagnostic copies.
    // Because the diagnostic copies are into pinned memory they are truly
    // async and the CPU can keep enqueueing; the GPU compute engine runs the
    // IFFT concurrently with DMA-engine diagnostic D2H.
    CF(cufftExecC2C(s_plan, d_fft, d_fft, CUFFT_INVERSE));
    k_NormalizeCrop<<<s_origBlocks, THREAD_NUM, 0, s_stmCompute>>>(
        d_fft, d_output, IMAGE_WIDTH, IMAGE_HEIGHT, s_padW, s_padH);
    CC(cudaGetLastError());

    // Diagnostic D2H on copy stream, gated by mask-done event
    CC(cudaStreamWaitEvent(s_stmCopy, s_evMaskDone, 0));

    if (FFTImage && d_fftOut)
        CC(cudaMemcpyAsync(s_hFFT, d_fftOut, szFOrig,
                           cudaMemcpyDeviceToHost, s_stmCopy));
    if (BinarizeImage && d_binOut)
        CC(cudaMemcpyAsync(s_hBin, d_binOut, szFOrig,
                           cudaMemcpyDeviceToHost, s_stmCopy));
    if (MaskImage && d_maskOut)
        CC(cudaMemcpyAsync(s_hMask, d_maskOut, szFOrig,
                           cudaMemcpyDeviceToHost, s_stmCopy));

    // Main output D2H on compute stream (after Normalize)
    CC(cudaMemcpyAsync(s_hOut, d_output, szFOrig,
                       cudaMemcpyDeviceToHost, s_stmCompute));

    // Sync compute stream; copy pinned output to caller's (pageable) buffer
    CC(cudaStreamSynchronize(s_stmCompute));
    std::memcpy(ImageOutput, s_hOut, szFOrig);

    // Sync copy stream; copy any diagnostic pinned buffers to caller
    CC(cudaStreamSynchronize(s_stmCopy));
    if (FFTImage)      std::memcpy(FFTImage,      s_hFFT,  szFOrig);
    if (BinarizeImage) std::memcpy(BinarizeImage, s_hBin,  szFOrig);
    if (MaskImage)     std::memcpy(MaskImage,     s_hMask, szFOrig);

CLEANUP:
    return;
}

// Optional teardown (not exposed in the original API, but safe to call if
// the host process ever wants to release persistent resources).
void CudaCleanup()
{
    if (s_plan)       { cufftDestroy(s_plan);            s_plan = 0; }
    if (s_pool)       { cudaFree(s_pool);                s_pool = nullptr; s_poolSz = 0; }
    if (s_evMaskDone) { cudaEventDestroy(s_evMaskDone);  s_evMaskDone = nullptr; }
    if (s_stmCompute) { cudaStreamDestroy(s_stmCompute); s_stmCompute = nullptr; }
    if (s_stmCopy)    { cudaStreamDestroy(s_stmCopy);    s_stmCopy    = nullptr; }
    if (s_hIn)        { cudaFreeHost(s_hIn);             s_hIn   = nullptr; s_hInElems   = 0; }
    if (s_hOut)       { cudaFreeHost(s_hOut);            s_hOut  = nullptr; s_hOutElems  = 0; }
    if (s_hFFT)       { cudaFreeHost(s_hFFT);            s_hFFT  = nullptr; s_hFFTElems  = 0; }
    if (s_hBin)       { cudaFreeHost(s_hBin);            s_hBin  = nullptr; s_hBinElems  = 0; }
    if (s_hMask)      { cudaFreeHost(s_hMask);           s_hMask = nullptr; s_hMaskElems = 0; }
}

} // extern "C"
