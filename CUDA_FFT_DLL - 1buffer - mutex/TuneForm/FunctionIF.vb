Imports System.ComponentModel
Imports CudaCore.CudaInterFace
Imports Matrox.MatroxImagingLibrary
Imports Matrox.MatroxImagingLibrary.MIL
Imports System.Runtime.InteropServices ' 用來呼叫 C++ DLL
Imports System.Threading ' 用來使用 Mutex

Public Class FunctionIF
    Implements IDisposable

    Private Const MutexName As String = "Global\MyGpuFFT_Mutex"
    Private m_MilSystem As MIL_ID

    Private m_TuneForm As CudaFFTComponent.TuneForm
    Private m_CudaCore As CudaCore.CudaInterFace

    Private m_mImgSource As MIL_ID
    Private m_mImgFFT As MIL_ID
    Private m_mImgBinarize As MIL_ID
    Private m_mImgMask As MIL_ID
    Private m_mImgIFFT As MIL_ID

    Private m_AryInput(0) As Single
    Private m_AryOutput(0) As Single

    Private m_ArySource(0) As Single
    Private m_AryFFT(0) As Single
    Private m_AryBinarize(0) As Single
    Private m_AryMask(0) As Single
    Private m_AryIFFT(0) As Single

    Public Sub New(ByVal mSys As MIL_ID)
        Me.m_MilSystem = mSys
        Me.m_CudaCore = New CudaCore.CudaInterFace
        Me.m_CudaCore.CudaInitDevice(0)
    End Sub

    Public Sub TuneFormShowUI(ByVal mImgSource As MIL_ID,
                              Optional ByRef ScaleValue As Integer = 255,
                              Optional ByRef ThresholdValue As Integer = 128,
                              Optional ByRef MaskWidth As Integer = 10,
                              Optional ByRef MaskHeight As Integer = 10,
                              Optional ByRef FilePath As String = "")

        ' Tune Dialog
        Me.m_TuneForm = New CudaFFTComponent.TuneForm(Me.m_MilSystem, Me.m_CudaCore)
        Me.m_TuneForm.UpdateImage(mImgSource, ScaleValue, ThresholdValue, MaskWidth, MaskHeight, FilePath)
        Me.m_TuneForm.ShowDialog()

        ' Update Data
        ScaleValue = Me.m_TuneForm.ScaleValue
        ThresholdValue = Me.m_TuneForm.ThresholdValue
        MaskWidth = Me.m_TuneForm.MaskWidth
        MaskHeight = Me.m_TuneForm.MaskHeight

        Me.m_TuneForm.Dispose()
    End Sub

    Public Sub CudaFFTCalculate(ByVal mImgSource As MIL_ID,
                              ByVal mImgResult As MIL_ID,
                              Optional ByRef ScaleValue As Integer = 255,
                              Optional ByRef ThresholdValue As Integer = 128,
                              Optional ByRef MaskWidth As Integer = 10,
                              Optional ByRef MaskHeight As Integer = 10)

        Dim P1 As New Stopwatch
        P1.Start()

        Dim gpuMutex As Mutex = Nothing
        Dim hasHandle As Boolean = False
        Try
            gpuMutex = New Mutex(False, MutexName)
            hasHandle = gpuMutex.WaitOne()
        Catch ex As Exception
            hasHandle = True
        End Try
        Dim tmp_InputImageSizeX As MIL_INT = MbufInquire(mImgSource, M_SIZE_X)
        Dim tmp_InputImageSizeY As MIL_INT = MbufInquire(mImgSource, M_SIZE_Y)

        If (tmp_InputImageSizeX * tmp_InputImageSizeY) <> Me.m_AryInput.Length Then
            ReDim Me.m_AryInput(tmp_InputImageSizeX * tmp_InputImageSizeY - 1)
            ReDim Me.m_AryOutput(tmp_InputImageSizeX * tmp_InputImageSizeY - 1)
        End If

        MbufGet(mImgSource, Me.m_AryInput)

        Try
            If hasHandle Then

                Dim T1 As New Stopwatch
                T1.Start()
                Me.m_CudaCore.CudaSetFFTConfig(192, tmp_InputImageSizeX, tmp_InputImageSizeY, ScaleValue, ThresholdValue, MaskWidth, MaskHeight, False)
                Me.m_CudaCore.CudaExcuteFFT2D(Me.m_AryInput, Me.m_AryOutput, Nothing, Nothing, Nothing)

                T1.Stop()
                P1.Stop()
                Console.WriteLine(String.Format("總花費時間 - [{1}ms], FFT花費時間 - [{0}ms]", T1.ElapsedMilliseconds, P1.ElapsedMilliseconds))

                MbufPut(mImgResult, Me.m_AryOutput)
            End If
        Catch ex As Exception

        Finally

            ' 只有當我們真的拿到鎖 (hasHandle = True) 時才釋放
            If hasHandle AndAlso gpuMutex IsNot Nothing Then
                gpuMutex.ReleaseMutex()
                Console.WriteLine("已釋放鎖。")
            End If

            ' 清理 Mutex 物件本身
            If gpuMutex IsNot Nothing Then
                gpuMutex.Dispose()
            End If
        End Try



    End Sub

#Region "IDisposable Support"
    Private disposedValue As Boolean ' 偵測多餘的呼叫

    ' IDisposable
    Protected Overridable Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then

                ' TODO: 處置 Managed 狀態 (Managed 物件)。
                If Me.m_mImgSource <> M_NULL Then
                    MbufFree(Me.m_mImgSource)
                    Me.m_mImgSource = M_NULL
                End If

                If Me.m_mImgFFT <> M_NULL Then
                    MbufFree(Me.m_mImgFFT)
                    Me.m_mImgFFT = M_NULL
                End If

                If Me.m_mImgBinarize <> M_NULL Then
                    MbufFree(Me.m_mImgBinarize)
                    Me.m_mImgBinarize = M_NULL
                End If

                If Me.m_mImgMask <> M_NULL Then
                    MbufFree(Me.m_mImgMask)
                    Me.m_mImgMask = M_NULL
                End If

                If Me.m_mImgIFFT <> M_NULL Then
                    MbufFree(Me.m_mImgIFFT)
                    Me.m_mImgIFFT = M_NULL
                End If

                Me.m_CudaCore.Dispose()
            End If

            ' TODO: 釋放 Unmanaged 資源 (Unmanaged 物件) 並覆寫下方的 Finalize()。
            ' TODO: 將大型欄位設為 null。
        End If
        disposedValue = True
    End Sub

    ' TODO: 只有當上方的 Dispose(disposing As Boolean) 具有要釋放 Unmanaged 資源的程式碼時，才覆寫 Finalize()。
    'Protected Overrides Sub Finalize()
    '    ' 請勿變更這個程式碼。請將清除程式碼放在上方的 Dispose(disposing As Boolean) 中。
    '    Dispose(False)
    '    MyBase.Finalize()
    'End Sub

    ' Visual Basic 加入這個程式碼的目的，在於能正確地實作可處置的模式。
    Public Sub Dispose() Implements IDisposable.Dispose
        ' 請勿變更這個程式碼。請將清除程式碼放在上方的 Dispose(disposing As Boolean) 中。
        Dispose(True)
        ' TODO: 覆寫上列 Finalize() 時，取消下行的註解狀態。
        ' GC.SuppressFinalize(Me)
    End Sub
#End Region

End Class
