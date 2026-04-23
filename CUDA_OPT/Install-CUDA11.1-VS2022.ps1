# Install-CUDA11.1-VS2022.ps1
# Copies CUDA 11.1 MSBuild integration files into every VS 2022 Community
# toolset directory that could resolve $(VCTargetsPath). Covers v160/v170 so
# the project works regardless of whether PlatformToolset is v142 or v143.

# --- Auto-elevate to Administrator if not already ---
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
if (-not $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Write-Host "Requesting administrator privileges..." -ForegroundColor Yellow
    Start-Process powershell.exe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    exit
}

$ErrorActionPreference = 'Stop'

$src = "C:\Program Files\NVIDIA GPU Computing Toolkit\CUDA\v11.1\extras\visual_studio_integration\MSBuildExtensions"

$vcRoot = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Microsoft\VC"
$destinations = @(
    (Join-Path $vcRoot "v160\BuildCustomizations"),  # used when PlatformToolset = v142 (VS 2019)
    (Join-Path $vcRoot "v170\BuildCustomizations")   # used when PlatformToolset = v143 (VS 2022)
)

$files = @(
    "CUDA 11.1.props",
    "CUDA 11.1.targets",
    "CUDA 11.1.xml",
    "Nvda.Build.CudaTasks.v11.1.dll"
)

Write-Host ""
Write-Host "=== CUDA 11.1 -> VS 2022 Integration ===" -ForegroundColor Cyan
Write-Host "Source : $src"
Write-Host ""

# --- Verify source ---
if (-not (Test-Path -LiteralPath $src)) {
    Write-Host "ERROR: CUDA 11.1 source directory not found." -ForegroundColor Red
    Write-Host "       Is CUDA 11.1 installed at the expected location?" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

$totalOk = 0
$totalFail = 0
$totalExpected = 0

foreach ($dst in $destinations) {
    Write-Host "--- Target: $dst ---" -ForegroundColor Cyan
    if (-not (Test-Path -LiteralPath $dst)) {
        Write-Host "SKIP  : directory does not exist (toolset probably not installed)" -ForegroundColor Yellow
        Write-Host ""
        continue
    }

    foreach ($f in $files) {
        $totalExpected++
        $from = Join-Path $src $f
        $to   = Join-Path $dst $f
        if (-not (Test-Path -LiteralPath $from)) {
            Write-Host "SKIP  (missing at source): $f" -ForegroundColor Yellow
            $totalFail++
            continue
        }
        try {
            Copy-Item -LiteralPath $from -Destination $to -Force
            if (Test-Path -LiteralPath $to) {
                Write-Host "OK    : $f" -ForegroundColor Green
                $totalOk++
            } else {
                Write-Host "FAIL  : $f (copy reported success but target missing)" -ForegroundColor Red
                $totalFail++
            }
        } catch {
            Write-Host "FAIL  : $f  ->  $($_.Exception.Message)" -ForegroundColor Red
            $totalFail++
        }
    }
    Write-Host ""
}

Write-Host "Done. Copied: $totalOk / $totalExpected" -ForegroundColor Cyan
if ($totalFail -eq 0) {
    Write-Host "All integration files are in place. Restart Visual Studio and reload the project." -ForegroundColor Green
} else {
    Write-Host "$totalFail file(s) failed. Review the errors above." -ForegroundColor Red
}
Write-Host ""
Read-Host "Press Enter to close"
