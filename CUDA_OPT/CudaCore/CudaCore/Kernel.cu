// Kernel.cu - Optimized CUDA FFT pipeline for CUDA_OPT
// Keeps the exact same public C API as the original CudaCore DLL
// (InitDevice / SetFFTConfig / ExcuteFFT2D / GetLastCudaError / ClearLastCudaError)
// so existing VB callers (TuneForm, SimIP, FunctionIF) work without modification.
//
// Summary of optimizations vs original Kernel.cu:
//    1. Persistent cuFFT plans           (avoids ~54 ms plan creation per call)
//    2. Persistent device memory pool    (avoids 4-18 ms cudaMalloc/Free per call)
//    3. Automatic smooth-prime padding   (avoids cuFFT Bluestein slow path on
//                                         prime dimensions like 14192 = 2^4 * 887)
//    4. Skipped intermediate logMag buffer: magnitude max is reduced on the raw
//       squared magnitude; ApplyMask recomputes log(1+sqrt()) inline.
//    5. Warp-shuffle + shared-mem reduction: 1 atomicMax per block instead of
//       per thread.
//    6. Device-resident max value: ApplyMask reads d_maxSqMag from device
//       memory - no CPU round-trip / stream sync between reduction and mask.
//    7. Compute + Copy streams with event sync: IFFT runs on compute stream
//       concurrently with diagnostic D2H copies on copy stream.
//    8. Persistent pinned (page-locked) host staging buffers + async memcpy
//       (fallback when the caller's buffer cannot be host-registered).
//    9. Fused init kernel writes zero-pad region and checkerboard-initialised
//       values in a single pass.
//   10. R2C/C2R FFT instead of C2C: input is real, so we use the half-spectrum
//       (padW/2+1 complex columns) - cuts FFT work, mask work, max-reduction
//       work and FFT memory traffic roughly in half. The mask is centro-
//       symmetric so applying it to the stored half preserves Hermitian
//       symmetry required by C2R.
//   11. Cached cudaHostRegister of the caller's VB-side buffers: skips the
//       pageable->pinned std::memcpy step on every call after the first
//       (~60 ms saved per direction). Falls back to staged memcpy via the
//       persistent pinned buffers if registration fails.

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
int s_padW       = 0;
int s_padH       = 0;
int s_halfW      = 0;   // padW/2 + 1: width of R2C half-spectrum
int s_padBlocks  = 1;   // launch config for padW*padH element kernels
int s_origBlocks = 1;   // launch config for origW*origH element kernels
int s_halfBlocks = 1;   // launch config for halfW*padH (R2C) element kernels

// Persistent GPU resources
cufftHandle  s_planFwd = 0;   // R2C (forward)
cufftHandle  s_planInv = 0;   // C2R (inverse)
int          s_planW   = 0;
int          s_planH   = 0;

char*        s_pool    = nullptr;
size_t       s_poolSz  = 0;

cudaStream_t s_stmCompute = nullptr;
cudaStream_t s_stmCopy    = nullptr;
cudaEvent_t  s_evMaskDone = nullptr;

// Persistent pinned host buffers (fallback when host registration fails)
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

// Cached host-buffer registrations. The C++/CLI wrapper pin_ptr's the VB array
// for the duration of each call; large (>85KB) .NET Framework arrays live in
// the LOH which is never auto-compacted, so the VA is stable across calls and
// we can keep a single registration alive for the lifetime of the DLL.
struct HostReg { void* ptr; size_t sz; };
HostReg s_regIn   = { nullptr, 0 };
HostReg s_regOut  = { nullptr, 0 };
HostReg s_regFFT  = { nullptr, 0 };
HostReg s_regBin  = { nullptr, 0 };
HostReg s_regMask = { nullptr, 0 };

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

inline int DivUp(int a, int b) { return (a + b - 1) / b; }

// Try to page-lock the caller's buffer so cudaMemcpyAsync runs truly async.
// On a cache hit (same ptr+sz as last call) returns true with no work.
bool TryRegisterHost(HostReg* cache, void* userPtr, size_t needSz) {
    if (!userPtr || needSz == 0) return false;
    if (cache->ptr == userPtr && cache->sz == needSz) return true;
    if (cache->ptr) {
        // Old registration may already be invalid if the .NET array was freed;
        // ignore the error in that case and move on.
        cudaHostUnregister(cache->ptr);
        cudaGetLastError();
        cache->ptr = nullptr;
        cache->sz  = 0;
    }
    cudaError_t e = cudaHostRegister(userPtr, needSz, cudaHostRegisterDefault);
    if (e != cudaSuccess) {
        cudaGetLastError();   // clear sticky error
        return false;
    }
    cache->ptr = userPtr;
    cache->sz  = needSz;
    return true;
}

void ClearHostReg(HostReg* cache) {
    if (cache->ptr) {
        cudaHostUnregister(cache->ptr);
        cudaGetLastError();
        cache->ptr = nullptr;
        cache->sz  = 0;
    }
}

} // anonymous namespace

// ==============================================================================
// Device kernels
// ==============================================================================

extern "C" {

// Kernel 1: copy origW x origH input into padW x padH real array with
// checkerboard sign-flip. Pixels outside the image are left as zero (zero-pad).
// Output is real (R2C input), not complex.
__global__ void k_InitCheckerPadReal(
    const float* __restrict__ src,
    float* __restrict__ dst,
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
    dst[idx] = v;
}

// Kernel 2: max reduction of squared magnitude over the R2C half-spectrum.
// |F[k]| = |F[-k]| for real input, so max over the stored half == max over
// the full spectrum.
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

// Kernel 3: apply mask to the R2C half-spectrum.
// The mask (rect / ellipse / threshold-based binarize) is centro-symmetric in
// (kx, ky) around (padW/2, padH/2), and so is |F'|^2, so applying it to the
// stored half preserves the Hermitian symmetry that C2R requires; the unstored
// half is implicitly handled by the C2R IFFT's conjugate fill.
//
// Diagnostic outputs (origW x origH float buffers) are written for both the
// stored half and its Hermitian-mirror partner so the original full-padded
// semantic is preserved when origW > halfW.
__global__ void k_ApplyMaskFusedR2C(
    cuFloatComplex* __restrict__ fft,
    const int*   __restrict__ d_maxSqMagInt,
    int halfW, int padW, int padH, int origW, int origH,
    float scaleVal, float thresholdVal,
    int cx, int cy, float hw, float hh, int useEllipse,
    float* outFFT, float* outBin, float* outMask)
{
    const int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= halfW * padH) return;

    // Broadcast: every thread reads the same int, goes through read-only cache
    const float maxSqMag  = __int_as_float(__ldg(d_maxSqMagInt));
    const float maxLogMag = logf(1.0f + sqrtf(maxSqMag));

    const int kx = idx % halfW;
    const int py = idx / halfW;

    const float re = fft[idx].x;
    const float im = fft[idx].y;
    const float sqmag  = re * re + im * im;
    const float logmag = logf(1.0f + sqrtf(sqmag));

    float norm = 0.0f;
    if (maxLogMag > 0.0f)
        norm = fminf(fmaxf((logmag / maxLogMag) * scaleVal, 0.0f), scaleVal);

    const float bin = (norm > thresholdVal) ? 0.0f : 1.0f;

    const float dx = (float)(kx - cx);
    const float dy = (float)(py - cy);
    const bool  inRect   = (fabsf(dx) <= hw) && (fabsf(dy) <= hh);
    const float rectMask = inRect ? 1.0f : bin;

    float finalMask = rectMask;
    if (useEllipse) {
        const bool inEllipse = (hw > 0.0f && hh > 0.0f) &&
            ((dx * dx) / (hw * hw) + (dy * dy) / (hh * hh)) <= 1.0f;
        finalMask = rectMask * (inEllipse ? 1.0f : 0.0f);
    }

    fft[idx].x *= finalMask;
    fft[idx].y *= finalMask;

    // Diagnostic primary write (covers stored half: kx in [0, halfW))
    if (kx < origW && py < origH) {
        const int oIdx = py * origW + kx;
        if (outFFT)  outFFT [oIdx] = norm;
        if (outBin)  outBin [oIdx] = bin;
        if (outMask) outMask[oIdx] = finalMask;
    }

    // Diagnostic mirror write for the unstored half (kx in [halfW, padW)).
    // norm/bin/finalMask are centro-symmetric so the value at (padW-kx,padH-py)
    // equals the value at (kx, py). Skip kx==0 and kx==halfW-1: those columns
    // are self-paired and already covered by another thread's primary write.
    if (kx > 0 && kx < halfW - 1) {
        const int mkx = padW - kx;
        const int mpy = (py == 0) ? 0 : (padH - py);
        if (mkx < origW && mpy < origH) {
            const int mIdx = mpy * origW + mkx;
            if (outFFT)  outFFT [mIdx] = norm;
            if (outBin)  outBin [mIdx] = bin;
            if (outMask) outMask[mIdx] = finalMask;
        }
    }
}

// Kernel 4: normalise C2R output (real, padded) + undo checkerboard + crop
// back to origW x origH.
__global__ void k_NormalizeCrop(
    const float* __restrict__ in,
    float* __restrict__ out,
    int origW, int origH, int padW, int padH)
{
    const int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= origW * origH) return;
    const int x = idx % origW;
    const int y = idx / origW;
    const int padIdx = y * padW + x;
    float v = in[padIdx] / (float)(padW * padH);
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
    if (THREAD_NUM & 31) THREAD_NUM = (THREAD_NUM + 31) & ~31;

    IMAGE_WIDTH   = ImageWidth;
    IMAGE_HEIGHT  = ImageHeight;
    SCALE_VAL     = (float)ScaleValue;
    THRESHOLD_VAL = ThresholdValue;
    MASK_WIDTH    = MaskWidth;
    MASK_HEIGHT   = MaskHeight;
    USE_ELLIPSE   = UseEllipse;

    s_padW  = NextSmooth(ImageWidth);
    s_padH  = NextSmooth(ImageHeight);
    s_halfW = s_padW / 2 + 1;

    BLOCK_NUM    = DivUp(ImageWidth * ImageHeight, THREAD_NUM);
    s_padBlocks  = DivUp(s_padW * s_padH, THREAD_NUM);
    s_origBlocks = DivUp(ImageWidth * ImageHeight, THREAD_NUM);
    s_halfBlocks = DivUp(s_halfW * s_padH, THREAD_NUM);
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
    const int    halfN   = s_halfW * s_padH;
    const size_t szFOrig = (size_t)sizeof(float) * origN;
    const size_t szFPad  = (size_t)sizeof(float) * padN;
    const size_t szCHalf = (size_t)sizeof(cuFloatComplex) * halfN;
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

    float*           d_stage    = nullptr;
    float*           d_padReal  = nullptr;   // R2C input; reused as C2R output
    cuFloatComplex*  d_fft      = nullptr;   // R2C output (half-spectrum)
    int*             d_maxSqMag = nullptr;
    float*           d_fftOut   = nullptr;
    float*           d_binOut   = nullptr;
    float*           d_maskOut  = nullptr;
    float*           d_output   = nullptr;

    // Declared up front so the early `goto CLEANUP` paths don't bypass any
    // non-trivial initializer (nvcc warning #546).
    bool inReg = false, outReg = false, fftReg = false, binReg = false, mskReg = false;

    // Pool layout: stage(orig float) + padReal(pad float) + fft(half complex)
    //            + max(int) + diagnostics(orig float each, optional)
    //            + output(orig float)
    size_t total = A256(szFOrig) + A256(szFPad) + A256(szCHalf)
                 + A256(szI) + A256(szFOrig);
    if (FFTImage)       total += A256(szFOrig);
    if (BinarizeImage)  total += A256(szFOrig);
    if (MaskImage)      total += A256(szFOrig);

    // Streams (once) - non-blocking to avoid implicit sync with stream 0
    if (!s_stmCompute) {
        CC(cudaStreamCreateWithFlags(&s_stmCompute, cudaStreamNonBlocking));
        CC(cudaStreamCreateWithFlags(&s_stmCopy,    cudaStreamNonBlocking));
    }

    // Event (once)
    if (!s_evMaskDone) {
        CC(cudaEventCreateWithFlags(&s_evMaskDone, cudaEventDisableTiming));
    }

    // R2C / C2R plans (recreated only when padded dims change)
    if (s_planFwd == 0 || s_planInv == 0 ||
        s_planW != s_padW || s_planH != s_padH) {
        if (s_planFwd) { cufftDestroy(s_planFwd); s_planFwd = 0; }
        if (s_planInv) { cufftDestroy(s_planInv); s_planInv = 0; }
        CF(cufftPlan2d(&s_planFwd, s_padH, s_padW, CUFFT_R2C));
        CF(cufftPlan2d(&s_planInv, s_padH, s_padW, CUFFT_C2R));
        CF(cufftSetStream(s_planFwd, s_stmCompute));
        CF(cufftSetStream(s_planInv, s_stmCompute));
        s_planW = s_padW;
        s_planH = s_padH;
    }

    // Device memory pool (regrown only when needed)
    if (!s_pool || s_poolSz < total) {
        if (s_pool) { cudaFree(s_pool); s_pool = nullptr; }
        CC(cudaMalloc(&s_pool, total));
        s_poolSz = total;
    }

    // Try to register the caller's buffers as pinned. On cache hit (same ptr
    // as last call) this is a no-op; on miss it's a one-time ~30-50 ms cost
    // that pays back as ~60 ms saved per call thereafter. Falls back to the
    // persistent pinned staging buffers + std::memcpy if registration fails.
    inReg  = TryRegisterHost(&s_regIn,  ImageInput,  szFOrig);
    outReg = TryRegisterHost(&s_regOut, ImageOutput, szFOrig);
    fftReg = (FFTImage      != nullptr) &&
             TryRegisterHost(&s_regFFT,  FFTImage,      szFOrig);
    binReg = (BinarizeImage != nullptr) &&
             TryRegisterHost(&s_regBin,  BinarizeImage, szFOrig);
    mskReg = (MaskImage     != nullptr) &&
             TryRegisterHost(&s_regMask, MaskImage,     szFOrig);

    if (!inReg)  { if (!EnsurePinned(&s_hIn,  &s_hInElems,  origN)) goto CLEANUP; }
    if (!outReg) { if (!EnsurePinned(&s_hOut, &s_hOutElems, origN)) goto CLEANUP; }
    if (FFTImage      && !fftReg) { if (!EnsurePinned(&s_hFFT,  &s_hFFTElems,  origN)) goto CLEANUP; }
    if (BinarizeImage && !binReg) { if (!EnsurePinned(&s_hBin,  &s_hBinElems,  origN)) goto CLEANUP; }
    if (MaskImage     && !mskReg) { if (!EnsurePinned(&s_hMask, &s_hMaskElems, origN)) goto CLEANUP; }

    {
        size_t off = 0;
        d_stage    = reinterpret_cast<float*>(s_pool + off);          off += A256(szFOrig);
        d_padReal  = reinterpret_cast<float*>(s_pool + off);          off += A256(szFPad);
        d_fft      = reinterpret_cast<cuFloatComplex*>(s_pool + off); off += A256(szCHalf);
        d_maxSqMag = reinterpret_cast<int*>(s_pool + off);            off += A256(szI);
        if (FFTImage)       { d_fftOut  = reinterpret_cast<float*>(s_pool + off); off += A256(szFOrig); }
        if (BinarizeImage)  { d_binOut  = reinterpret_cast<float*>(s_pool + off); off += A256(szFOrig); }
        if (MaskImage)      { d_maskOut = reinterpret_cast<float*>(s_pool + off); off += A256(szFOrig); }
        d_output   = reinterpret_cast<float*>(s_pool + off);
    }

    // ------------------ Pipeline ------------------

    // Reset max accumulator on device
    CC(cudaMemsetAsync(d_maxSqMag, 0, szI, s_stmCompute));

    // Stage input H2D. With registration, ImageInput is already pinned and
    // cudaMemcpyAsync runs truly async. Without it, copy via persistent pinned
    // staging buffer.
    {
        const float* hostIn;
        if (inReg) {
            hostIn = ImageInput;
        } else {
            std::memcpy(s_hIn, ImageInput, szFOrig);
            hostIn = s_hIn;
        }
        CC(cudaMemcpyAsync(d_stage, hostIn, szFOrig,
                           cudaMemcpyHostToDevice, s_stmCompute));
    }

    // Pad + checkerboard -> padded real array
    k_InitCheckerPadReal<<<s_padBlocks, THREAD_NUM, 0, s_stmCompute>>>(
        d_stage, d_padReal, IMAGE_WIDTH, IMAGE_HEIGHT, s_padW, s_padH);
    CC(cudaGetLastError());

    // Forward R2C: padded real -> half-spectrum complex
    CF(cufftExecR2C(s_planFwd, d_padReal, d_fft));

    // Max of squared magnitude over half-spectrum (== max over full spectrum)
    k_SqMagMax<<<s_halfBlocks, THREAD_NUM, 0, s_stmCompute>>>(
        d_fft, halfN, d_maxSqMag);
    CC(cudaGetLastError());

    // Apply mask in half-spectrum (Hermitian-symmetric in mask values)
    k_ApplyMaskFusedR2C<<<s_halfBlocks, THREAD_NUM, 0, s_stmCompute>>>(
        d_fft, d_maxSqMag,
        s_halfW, s_padW, s_padH, IMAGE_WIDTH, IMAGE_HEIGHT,
        SCALE_VAL, (float)THRESHOLD_VAL,
        cX_pad, cY_pad, hw_pad, hh_pad, USE_ELLIPSE ? 1 : 0,
        d_fftOut, d_binOut, d_maskOut);
    CC(cudaGetLastError());

    // Signal: mask work done (gates diagnostic D2H on the copy stream)
    CC(cudaEventRecord(s_evMaskDone, s_stmCompute));

    // Inverse C2R: half-spectrum -> padded real (overwrites d_padReal).
    // C2R may also modify the input (d_fft) - we don't need it after this.
    CF(cufftExecC2R(s_planInv, d_fft, d_padReal));

    k_NormalizeCrop<<<s_origBlocks, THREAD_NUM, 0, s_stmCompute>>>(
        d_padReal, d_output, IMAGE_WIDTH, IMAGE_HEIGHT, s_padW, s_padH);
    CC(cudaGetLastError());

    // Diagnostic D2H on copy stream, gated by mask-done event
    CC(cudaStreamWaitEvent(s_stmCopy, s_evMaskDone, 0));

    if (FFTImage && d_fftOut) {
        float* hostFFT = fftReg ? FFTImage : s_hFFT;
        CC(cudaMemcpyAsync(hostFFT, d_fftOut, szFOrig,
                           cudaMemcpyDeviceToHost, s_stmCopy));
    }
    if (BinarizeImage && d_binOut) {
        float* hostBin = binReg ? BinarizeImage : s_hBin;
        CC(cudaMemcpyAsync(hostBin, d_binOut, szFOrig,
                           cudaMemcpyDeviceToHost, s_stmCopy));
    }
    if (MaskImage && d_maskOut) {
        float* hostMask = mskReg ? MaskImage : s_hMask;
        CC(cudaMemcpyAsync(hostMask, d_maskOut, szFOrig,
                           cudaMemcpyDeviceToHost, s_stmCopy));
    }

    // Main output D2H on compute stream (after Normalize)
    {
        float* hostOut = outReg ? ImageOutput : s_hOut;
        CC(cudaMemcpyAsync(hostOut, d_output, szFOrig,
                           cudaMemcpyDeviceToHost, s_stmCompute));
    }

    // Sync compute stream; if not registered, copy pinned output to caller
    CC(cudaStreamSynchronize(s_stmCompute));
    if (!outReg) std::memcpy(ImageOutput, s_hOut, szFOrig);

    // Sync copy stream; if not registered, copy pinned diagnostics to caller
    CC(cudaStreamSynchronize(s_stmCopy));
    if (FFTImage      && !fftReg) std::memcpy(FFTImage,      s_hFFT,  szFOrig);
    if (BinarizeImage && !binReg) std::memcpy(BinarizeImage, s_hBin,  szFOrig);
    if (MaskImage     && !mskReg) std::memcpy(MaskImage,     s_hMask, szFOrig);

CLEANUP:
    return;
}

// Optional teardown (not exposed in the original API, but safe to call if
// the host process ever wants to release persistent resources).
void CudaCleanup()
{
    if (s_planFwd)    { cufftDestroy(s_planFwd);         s_planFwd = 0; }
    if (s_planInv)    { cufftDestroy(s_planInv);         s_planInv = 0; }
    if (s_pool)       { cudaFree(s_pool);                s_pool = nullptr; s_poolSz = 0; }
    if (s_evMaskDone) { cudaEventDestroy(s_evMaskDone);  s_evMaskDone = nullptr; }
    if (s_stmCompute) { cudaStreamDestroy(s_stmCompute); s_stmCompute = nullptr; }
    if (s_stmCopy)    { cudaStreamDestroy(s_stmCopy);    s_stmCopy    = nullptr; }
    if (s_hIn)        { cudaFreeHost(s_hIn);             s_hIn   = nullptr; s_hInElems   = 0; }
    if (s_hOut)       { cudaFreeHost(s_hOut);            s_hOut  = nullptr; s_hOutElems  = 0; }
    if (s_hFFT)       { cudaFreeHost(s_hFFT);            s_hFFT  = nullptr; s_hFFTElems  = 0; }
    if (s_hBin)       { cudaFreeHost(s_hBin);            s_hBin  = nullptr; s_hBinElems  = 0; }
    if (s_hMask)      { cudaFreeHost(s_hMask);           s_hMask = nullptr; s_hMaskElems = 0; }
    ClearHostReg(&s_regIn);
    ClearHostReg(&s_regOut);
    ClearHostReg(&s_regFFT);
    ClearHostReg(&s_regBin);
    ClearHostReg(&s_regMask);
}

} // extern "C"
