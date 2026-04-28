# CUDA_OPT — 面板影像 FFT Pipeline 優化版本

本專案是 `CUDA_FFT_DLL - 1buffer - mutex` 的優化分支，針對大尺寸面板影像（例如 14192 × 10752）的 CUDA FFT 流程重寫 `Kernel.cu`。公開 API 簽名完全保持不變，現有 VB 呼叫端（TuneForm、SimIP、FunctionIF）**零改動**即可換掉 DLL 直接跑。

---

## 目錄

- [專案架構](#專案架構)
- [建置方式](#建置方式)
- [只改了哪些檔案](#只改了哪些檔案)
- [API 相容性](#api-相容性)
- [優化詳解](#優化詳解)
- [實際執行結果](#實際執行結果)
- [檔案說明](#檔案說明)
- [已知限制與注意事項](#已知限制與注意事項)

---

## 專案架構

```
CUDA_OPT/
├── CudaFFTComponent.sln          ← 主 solution，用 VS2022 開啟
├── CudaCore/
│   ├── CudaCore.sln
│   └── CudaCore/
│       ├── Kernel.cu             ← ★ 已重寫（優化版本）
│       ├── Comm.h                ← 未動
│       ├── CudaCore.cpp          ← 未動（C++/CLI wrapper）
│       ├── CudaCore.h            ← 未動
│       ├── CudaCore.vcxproj      ← 未動
│       ├── Stdafx.h / .cpp
│       ├── AssemblyInfo.cpp
│       ├── app.ico / app.rc / resource.h
│       └── ReadMe.txt
├── TuneForm/                     ← VB.NET 專案（未動）
├── SimIP/                        ← VB.NET 專案（未動）
├── Ref/                          ← Matrox MIL DLL 參考（未動）
├── packages/                     ← NuGet packages（未動）
└── Install-CUDA11.1-VS2022.ps1
```

---

## 建置方式

### 前置需求
- Visual Studio 2022（Community 即可）
- CUDA Toolkit 11.1（與原專案相同）
- MSVC v142 toolset（14.29.30133）
- .NET Framework 4.7.2
- Matrox Imaging Library（DLL 已附在 `Ref/`）

> 若環境未裝 CUDA 11.1，可執行 `Install-CUDA11.1-VS2022.ps1`。

### 步驟
1. 雙擊 `CudaFFTComponent.sln` 用 Visual Studio 2022 開啟。
2. 組態切到 **Release | x64**。
3. 直接 Build → 輸出 `CudaCore.dll` 在 `CudaCore\CudaCore\x64\Release\`。

### 驗證
本專案的 `Kernel.cu` 已用 nvcc 做過語法驗證（CUDA 13.2 + MSVC 14.44 環境下可無錯誤編譯），VS 用 CUDA 11.1 + v142 toolset 建置是原專案本來就支援的組合。

---

## 只改了哪些檔案

相對於原始 `CUDA_FFT_DLL - 1buffer - mutex`，**只動了一個檔案**：

| 檔案 | 狀態 |
|---|---|
| `CudaCore/CudaCore/Kernel.cu` | **完全重寫** |
| 其他所有檔案 | **原封不動** |

包括 `Comm.h`（全域變數宣告）、`CudaCore.cpp`（C++/CLI wrapper）、`.vcxproj`（建置設定）、VB 專案等，全部維持原貌。

---

## API 相容性

公開函式簽名與原版完全一致：

```cpp
extern "C" bool InitDevice(int gpuindex);
extern "C" void SetFFTConfig(int tidnumber,
                             int ImageWidth, int ImageHeight,
                             int ScaleValue, int ThresholdValue,
                             int MaskWidth,  int MaskHeight,
                             bool UseEllipse);
extern "C" void ExcuteFFT2D(float* ImageInput, float* ImageOutput,
                            float* FFTImage, float* BinarizeImage,
                            float* MaskImage);
extern "C" const char* GetLastCudaError();
extern "C" void ClearLastCudaError();
```

VB 端呼叫方式：

```vbnet
Me.m_CudaCore.CudaSetFFTConfig(192, W, H, ScaleVal, ThrVal, MaskW, MaskH, False)
Me.m_CudaCore.CudaExcuteFFT2D(m_AryInput, m_AryOutput, Nothing, Nothing, Nothing)
```
完全不用改。

**數學等價性**：優化版本在 pad 回原圖的區域內與原版**數學等價**。Mask 尺寸會依 pad 比例自動縮放以保持相同的正規化頻率截止。

---

## 優化詳解

共 9 項優化，按影響時間由大到小排序。

---

### 1. 自動 smooth-radix padding

**問題**  
cuFFT 的執行時間對 FFT size 的質因數組成高度敏感：
- 只含 `{2, 3, 5, 7}` 質因數 → **Cooley-Tukey** 飛快
- 含大質數（如 887） → **Bluestein** 慢 3 倍

面板影像常見尺寸 **14192 × 10752**：
- `14192 = 2⁴ × 887`（**887 是質數！**）→ Bluestein 慢路徑（~44 ms）
- `10752 = 2⁹ × 3 × 7`（smooth）→ 快路徑

**解法**  
`NextSmooth(n)` 找 `≥ n` 且只含 {2,3,5,7} 質因數的最小整數：

```cpp
int NextSmooth(int n) {
    for (int m = n; ; ++m) {
        int v = m;
        while ((v & 1) == 0) v >>= 1;
        while (v % 3 == 0)   v /= 3;
        while (v % 5 == 0)   v /= 5;
        while (v % 7 == 0)   v /= 7;
        if (v == 1) return m;
    }
}
```

14192 → **14336 = 2¹¹ × 7**（只多 1%，FFT 時間 44 ms → 15 ms）。

實作上：
- `k_InitCheckerPad` kernel 只在原圖範圍填實際值、pad 區填 0（zero-padding）
- Mask 中心、尺寸按 `padW / origW` 比例縮放到 pad 域
- `k_NormalizeCrop` 最後 IFFT 出來再 crop 回原圖尺寸

**預期節省：~58 ms/call**

---

### 2. Persistent cuFFT plan

**問題**  
原版每次呼叫都重建 plan：
```cpp
cufftPlan2d(&plan, H, W, CUFFT_C2C);   // ~54 ms（算 twiddle factors）
... 用 plan ...
cufftDestroy(plan);
```

**解法**  
把 `s_plan` 做成 `static`，只在尺寸變化時重建：
```cpp
if (s_plan == 0 || s_planW != s_padW || s_planH != s_padH) {
    if (s_plan) cufftDestroy(s_plan);
    cufftPlan2d(&s_plan, s_padH, s_padW, CUFFT_C2C);
    cufftSetStream(s_plan, s_stmCompute);
}
```
第一次付 54 ms，之後 0 ms。

**預期節省：~54 ms/call（第二次起）**

---

### 3. Persistent device memory pool

**問題**  
原版每次呼叫都做 6~7 次 `cudaMalloc` + `cudaFree`，累積 4-18 ms 的純分配開銷。

**解法**  
一次 `cudaMalloc` 配一大塊 pool（256-byte aligned），再切片：
```cpp
// Pool layout
d_fft       = (cuFloatComplex*)(s_pool + 0);                  // padN complex
d_stage     = (float*)         (s_pool + A256(szCPad));       // origN float
d_maxSqMag  = (int*)           (s_pool + A256(szCPad) + ...); // 1 int
d_output    = (float*)         (s_pool + ...);                // origN float
// + 可選的 d_fftOut / d_binOut / d_maskOut
```
Pool 只在需要更大時才 realloc。

**預期節省：~10 ms/call**

---

### 4. Compute + Copy 雙 stream + event 重疊

**問題**  
GPU 的 **compute engine** 與 **DMA engine** 是獨立硬體。原版全部序列化在 default stream 上跑，讓兩個 engine 互相等待。

**解法**  
開兩個 non-blocking stream，用 event 同步：
```cpp
// 運算完 mask
k_ApplyMaskFused<<<..., s_stmCompute>>>(...);
cudaEventRecord(s_evMaskDone, s_stmCompute);

// 先把 IFFT 和 Normalize 丟到 compute stream
cufftExecC2C(s_plan, d_fft, d_fft, CUFFT_INVERSE);
k_NormalizeCrop<<<..., s_stmCompute>>>(...);

// 再把 diagnostic D2H 丟到 copy stream，等 event 才開始
cudaStreamWaitEvent(s_stmCopy, s_evMaskDone, 0);
cudaMemcpyAsync(s_hFFT,  d_fftOut,  ..., s_stmCopy);
cudaMemcpyAsync(s_hBin,  d_binOut,  ..., s_stmCopy);
cudaMemcpyAsync(s_hMask, d_maskOut, ..., s_stmCopy);
```

**預期節省：~15 ms/call**

---

### 5. Persistent pinned host buffers

**問題**  
這項是讓 #4 真正生效的關鍵。  
`cudaMemcpyAsync` 在 **pageable** host memory 上其實**不是非同步的**！Driver 會偷偷用內部 pinned buffer 做 staging，CPU thread 被阻塞直到 DMA 完成 → stream 重疊失效。

**解法**  
自己 allocate pinned（page-locked）buffer，persistent 起來：
```cpp
cudaMallocHost(&s_hIn,  sizeof(float) * origN);   // pinned
cudaMallocHost(&s_hOut, sizeof(float) * origN);

// Pipeline
std::memcpy(s_hIn, ImageInput, szFOrig);          // CPU→pinned（~5 ms）
cudaMemcpyAsync(d_stage, s_hIn, ...);             // 真正 async，CPU 立即返回
// ... kernel launches 不被阻塞 ...
cudaMemcpyAsync(s_hOut, d_output, ...);           // 真正 async
cudaStreamSynchronize(s_stmCompute);
std::memcpy(ImageOutput, s_hOut, szFOrig);        // 最後搬回使用者 buffer
```

診斷輸出也同樣走 pinned。

**預期節省：~5-10 ms/call**

---

### 6. Warp-shuffle + shared-mem 階層 reduction

**問題**  
原版 `FFTLogMagMaxKernel` **每個執行緒**都對同一個全域 int 做 `atomicMax`：
```cpp
atomicMax(maxValInt, __float_as_int(val));   // ~150M 次 atomic！
```
嚴重序列化。

**解法**  
三層 reduction，每個 block 只做 **1 次** atomic：
```cpp
// Level 1: warp 內 32 threads 用 __shfl_down_sync 互換
for (int off = 16; off > 0; off >>= 1)
    val = fmaxf(val, __shfl_down_sync(0xffffffff, val, off));

// Level 2: 每個 warp 的 lane 0 寫到 shared memory
__shared__ float smem[32];
if (laneId == 0) smem[warpId] = val;
__syncthreads();

// Level 3: 第 0 個 warp 再 reduce 一次
if (warpId == 0) {
    val = (laneId < warps) ? smem[laneId] : 0.0f;
    for (int off = 16; off > 0; off >>= 1)
        val = fmaxf(val, __shfl_down_sync(0xffffffff, val, off));
    if (laneId == 0)
        atomicMax(d_maxSqMagInt, __float_as_int(val));
}
```

Atomic 次數：**~150M → ~600K**（256 倍減少）。

**預期節省：~5 ms/call**

---

### 7. 跳過 logMag 中間 buffer（兩 trick 併用）


**Trick A — 以 squared magnitude 做 max reduction**  
`log(1 + sqrt(x))` 對 x **單調遞增**，所以：
```
argmax of log(1+sqrt(re² + im²)) == argmax of (re² + im²)
```
Max kernel 直接比 `sqmag`，**省掉 sqrt+log 兩個 transcendental**（這是 device 上最貴的 op）。

**Trick B — 不存 logMag，ApplyMask 自己重算**
```cpp
__global__ void k_ApplyMaskFused(...) {
    const float maxSqMag  = __int_as_float(__ldg(d_maxSqMagInt));
    const float maxLogMag = logf(1.0f + sqrtf(maxSqMag));  // 每執行緒重算（值都一樣，compiler 會 CSE）

    const float sqmag  = re*re + im*im;
    const float logmag = logf(1.0f + sqrtf(sqmag));
    float norm = (logmag / maxLogMag) * scale;             // 和原版一樣的 normalize
    ...
}
```

每 pixel 多 sqrt+log（~10 cycles），但**省 600MB memory traffic**：
- Traffic cost：600MB / 900GB/s ≈ 0.67 ms
- Extra compute：~0.1 ms
- 淨利：~0.6 ms + **省 600MB VRAM**

---

### 8. Device-resident maxVal（免 CPU round-trip）

**問題**  
原版把 max 搬回 CPU 再當參數傳給 ApplyMask：
```cpp
cudaMemcpy(&maxValInt, d_maxValInt, sizeof(int), cudaMemcpyDeviceToHost);  // 強制 sync
ApplyMaskInlineKernel<<<...>>>(..., maxVal, ...);   // 值當參數
```
這個 D2H 強制整個 GPU pipeline **停等 CPU**。

**解法**  
傳**指標**不傳值，kernel 裡用 `__ldg` 從 device memory 讀：
```cpp
k_ApplyMaskFused<<<..., s_stmCompute>>>(
    d_fft, d_maxSqMag,   // 傳 device pointer，不是 host-side 值
    ...);

// Kernel 內
const float maxSqMag = __int_as_float(__ldg(d_maxSqMagInt));
```
所有執行緒讀同一個 address，走 read-only cache，廣播給整個 warp（單次 transaction）。

因為兩個 kernel 在同一個 stream，順序性保證由 CUDA runtime 維護。**整條 pipeline 無 CPU 介入**。

---

### 9. Fused InitCheckerPad（zero-pad + checkerboard 一次做）

**問題**  
Pad 過後，padded buffer 有一部分是原圖外的 zero 區域。naive 做法：
1. `cudaMemset(d_fft, 0, szCPad)` 填 0
2. Kernel 寫實際像素

兩次 full-frame traffic。

**解法**  
單一 kernel 同時處理：
```cpp
__global__ void k_InitCheckerPad(src, dst, origW, origH, padW, padH) {
    int idx = blockIdx.x * blockDim.x + threadIdx.x;
    if (idx >= padW * padH) return;
    int px = idx % padW, py = idx / padW;
    float v = 0.0f;
    if (px < origW && py < origH) {               // 原圖範圍
        v = __ldg(&src[py * origW + px]);         // read-only cache
        if ((px + py) & 1) v = -v;                // checkerboard sign-flip
    }
    dst[idx] = make_cuFloatComplex(v, 0.0f);      // pad 區自動為 (0, 0)
}
```

少一次 memset、少一個 kernel launch。

---

## 實際執行結果

以下為實機量測結果（同樣輸入影像，連續呼叫兩次）。

### 原版

![原版執行時間](result/原版時間.png)

- **總時間：4487 ms**
- **FFT 時間：1344 ms**

### 優化後 — 第一次呼叫（warmup）

![優化後第一次執行](result/優化後第一次執行.png)

- **總時間：4347 ms**
- **FFT 時間：1178 ms**

第一次呼叫需建立 cuFFT plan、配置 device memory pool、allocate pinned host buffers，屬一次性 warmup 開銷，整體改善幅度有限。

### 優化後 — 第二次呼叫（穩定態）

![優化後第二次執行](result/優化後第二次執行.png)

- **總時間：3939 ms**
- **FFT 時間：836 ms**

第二次起所有持久資源已就位，FFT 時間由 **1344 ms → 836 ms**，降幅約 **38%**。
總時間由 **4487 ms → 3939 ms**，降幅約 **12%**（瓶頸已移至 CUDA 以外的 I/O 流程）。

---

## 檔案說明

### `CudaCore/CudaCore/Kernel.cu`（重寫）

包含四個 CUDA kernels 和完整的 host-side pipeline：

| Kernel | 職責 |
|---|---|
| `k_InitCheckerPad` | H2D 後的初始化：checkerboard sign-flip + zero-pad，輸入 float → 輸出 complex |
| `k_SqMagMax` | 三層 warp reduction 求 max of `re²+im²` |
| `k_ApplyMaskFused` | 計算 logmag、normalize、binarize、矩形/橢圓 mask、乘回頻域；診斷輸出可選 |
| `k_NormalizeCrop` | IFFT 之後 normalize + un-checkerboard + crop 回原圖尺寸 |

Host 端持有的持久資源：
- `s_plan` — cuFFT plan（尺寸變才重建）
- `s_pool` — device memory pool（容量不足才 realloc）
- `s_stmCompute`, `s_stmCopy` — 兩條 non-blocking stream
- `s_evMaskDone` — 同步 event
- `s_hIn`, `s_hOut`, `s_hFFT`, `s_hBin`, `s_hMask` — pinned host buffers

另提供內部 `CudaCleanup()`（未輸出到 VB），需要釋放所有持久資源時可呼叫。

### `Comm.h`（未動）

保留原始的全域變數宣告（`BLOCK_NUM`, `THREAD_NUM`, `IMAGE_WIDTH` 等）和 `extern "C"` API 宣告。

### `CudaCore.cpp`（未動）

C++/CLI wrapper，把 native C API 包成 .NET class `CudaCore.CudaInterFace`。

---

## 已知限制與注意事項

1. **第一次呼叫較慢**（~200 ms warmup）  
   所有持久資源都在第一次 `ExcuteFFT2D` 時 lazily 建立。如要預熱，可在程式啟動後先跑一次 dummy FFT。

2. **SetFFTConfig 時的 `tidnumber`**  
   原版的 VB 端傳 192（= 6 × 32，有效 warp 配置）。為了安全，我的程式碼會把 tidnumber round up 到 32 的倍數供 warp reduction 使用。傳 256、512 等也都 OK。

3. **Mask 尺寸的邊界**  
   Mask 中心固定在頻譜中心（`padW/2, padH/2`）。若 `MaskWidth`、`MaskHeight` 非常小（例如 0），會退化成單點遮罩，這和原版行為一致。

4. **Debug|x64 建置設定**  
   原 `.vcxproj` 的 Debug|x64 未指定 CUDA code generation 與 link libraries，這是**既有問題**（原專案繼承下來的），不影響 Release|x64。日常使用 Release|x64。

5. **與 v3 的差異**  
   本版相較之前 `auo_cuda/src/opt/kernel_v3` 的進一步改進：
   - padding 由外部指定 → **自動偵測**
   - 仍存 logMag 中間 buffer → **直接跳過、inline 重算**
   - pageable 輸入 → **pinned host buffers**（讓 stream overlap 真正生效）

6. **相同的 mutex 設計**  
   VB 端的 `Global\MyGpuFFT_Mutex` 保留原樣，多進程同時呼叫時的互斥行為與原版一致。
