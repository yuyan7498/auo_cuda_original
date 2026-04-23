Imports System.ComponentModel
Imports System.Threading
Imports Matrox.MatroxImagingLibrary
Imports Matrox.MatroxImagingLibrary.MIL

Public Class TuneForm

    Private m_MilSystem As MIL_ID
    Private WithEvents Viewer1 As CudaFFTComponent.Viewer         'Source Image
    Private WithEvents Viewer2 As CudaFFTComponent.Viewer         'FFT Image
    Private WithEvents Viewer3 As CudaFFTComponent.Viewer         'FFT Binary Threshold Image
    Private WithEvents Viewer4 As CudaFFTComponent.Viewer         'FFT Mask Image
    Private WithEvents Viewer5 As CudaFFTComponent.Viewer         'IFFT Result Image

    Private m_mImgSizeX As MIL_INT
    Private m_mImgSizeY As MIL_INT
    Private m_mImgDepth As MIL_INT

    Private m_CudaCore As CudaCore.CudaInterFace
    Private m_ParametetRecipe As ParameterConfig

    Private m_mImgLoad As MIL_ID
    Private m_mImgSource As MIL_ID
    Private m_mImgFFT As MIL_ID
    Private m_mImgBinarize As MIL_ID
    Private m_mImgMask As MIL_ID
    Private m_mImgIFFT As MIL_ID

    Private m_ArySource(0) As Single
    Private m_AryFFT(0) As Single
    Private m_AryBinarize(0) As Single
    Private m_AryMask(0) As Single
    Private m_AryIFFT(0) As Single

    Private m_RunProcThread As System.Threading.Thread = Nothing
    Private m_RunProcCycle As Boolean
    Private m_RunProcResetEvent As AutoResetEvent

    ReadOnly Property IFFT As MIL_ID
        Get
            Return Me.m_mImgIFFT
        End Get
    End Property

    ReadOnly Property ScaleValue As Integer
        Get
            Return Me.m_ParametetRecipe.ImageScaleValue
        End Get
    End Property

    ReadOnly Property ThresholdValue As Integer
        Get
            Return Me.m_ParametetRecipe.BinaryThreshold
        End Get
    End Property

    ReadOnly Property MaskWidth As Integer
        Get
            Return Me.m_ParametetRecipe.MaskSizeX
        End Get
    End Property

    ReadOnly Property MaskHeight As Integer
        Get
            Return Me.m_ParametetRecipe.MaskSizeY
        End Get
    End Property

    Public Sub New(ByVal mSys As MIL_ID, ByVal CudaCoreIF As CudaCore.CudaInterFace)
        InitializeComponent()
        Me.m_MilSystem = mSys
        Me.m_CudaCore = CudaCoreIF

        Me.m_ParametetRecipe = New ParameterConfig
        Me.Ppg_ParameterRecipe.SelectedObject = Me.m_ParametetRecipe


        Me.Viewer1 = New CudaFFTComponent.Viewer()
        Me.Viewer1.Dock = DockStyle.Fill
        Me.Viewer1.SetMSystem(Me.m_MilSystem)

        Me.Viewer2 = New CudaFFTComponent.Viewer()
        Me.Viewer2.Dock = DockStyle.Fill
        Me.Viewer2.SetMSystem(Me.m_MilSystem)

        Me.Viewer3 = New CudaFFTComponent.Viewer()
        Me.Viewer3.Dock = DockStyle.Fill
        Me.Viewer3.SetMSystem(Me.m_MilSystem)

        Me.Viewer4 = New CudaFFTComponent.Viewer()
        Me.Viewer4.Dock = DockStyle.Fill
        Me.Viewer4.SetMSystem(Me.m_MilSystem)

        Me.Viewer5 = New CudaFFTComponent.Viewer()
        Me.Viewer5.Dock = DockStyle.Fill
        Me.Viewer5.SetMSystem(Me.m_MilSystem)

        Me.m_RunProcResetEvent = New AutoResetEvent(False)
        Me.m_RunProcCycle = True
        Me.m_RunProcThread = New System.Threading.Thread(AddressOf Me.RunProc)
        Me.m_RunProcThread.SetApartmentState(ApartmentState.STA)
        Me.m_RunProcThread.Start()

    End Sub

    Public Sub LoadImageByMilID(ByVal mImg As MIL_ID,
                           Optional ByVal ScaleValue As Integer = 255,
                           Optional ByVal ThresholdValue As Integer = 128,
                           Optional ByVal MaskWidth As Integer = 100,
                           Optional ByVal MaskHeiht As Integer = 100)

        Me.m_ParametetRecipe.ImageScaleValue = ScaleValue
        Me.m_ParametetRecipe.BinaryThreshold = ThresholdValue
        Me.m_ParametetRecipe.MaskSizeX = MaskWidth
        Me.m_ParametetRecipe.MaskSizeY = MaskHeight

    End Sub

    Public Sub UpdateImage(ByVal mImg As MIL_ID, ByVal ScaleValue As Integer, ByVal ThresholdValue As Integer, ByVal MaskWidth As Integer, ByVal MaskHeight As Integer, ByVal FilePath As String)
        Me.m_ParametetRecipe.ImageScaleValue = ScaleValue
        Me.m_ParametetRecipe.BinaryThreshold = ThresholdValue
        Me.m_ParametetRecipe.MaskSizeX = MaskWidth
        Me.m_ParametetRecipe.MaskSizeY = MaskHeight
        Me.m_ParametetRecipe.FilePath = FilePath

        If Me.m_mImgSizeX <> MbufInquire(mImg, M_SIZE_X) Or Me.m_mImgSizeY <> MbufInquire(mImg, M_SIZE_Y) Then

            Me.m_mImgSizeX = MbufInquire(mImg, M_SIZE_X)
            Me.m_mImgSizeY = MbufInquire(mImg, M_SIZE_Y)

            ' Souce
            If Me.m_mImgSource <> M_NULL Then
                MbufFree(Me.m_mImgSource)
                Me.m_mImgSource = M_NULL
            End If
            MbufAlloc2d(Me.m_MilSystem, Me.m_mImgSizeX, Me.m_mImgSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC + M_DISP, Me.m_mImgSource)
            ReDim Me.m_ArySource(Me.m_mImgSizeX * Me.m_mImgSizeY - 1)

            ' FFT
            If Me.m_mImgFFT <> M_NULL Then
                MbufFree(Me.m_mImgFFT)
                Me.m_mImgFFT = M_NULL
            End If
            MbufAlloc2d(Me.m_MilSystem, Me.m_mImgSizeX, Me.m_mImgSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC + M_DISP, Me.m_mImgFFT)
            ReDim Me.m_AryFFT(Me.m_mImgSizeX * Me.m_mImgSizeY - 1)

            'Binarize
            If Me.m_mImgBinarize <> M_NULL Then
                MbufFree(Me.m_mImgBinarize)
                Me.m_mImgBinarize = M_NULL
            End If
            MbufAlloc2d(Me.m_MilSystem, Me.m_mImgSizeX, Me.m_mImgSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC + M_DISP, Me.m_mImgBinarize)
            ReDim Me.m_AryBinarize(Me.m_mImgSizeX * Me.m_mImgSizeY - 1)

            'Mask
            If Me.m_mImgMask <> M_NULL Then
                MbufFree(Me.m_mImgMask)
                Me.m_mImgMask = M_NULL
            End If
            MbufAlloc2d(Me.m_MilSystem, Me.m_mImgSizeX, Me.m_mImgSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC + M_DISP, Me.m_mImgMask)
            ReDim Me.m_AryMask(Me.m_mImgSizeX * Me.m_mImgSizeY - 1)

            'IFFT
            If Me.m_mImgIFFT <> M_NULL Then
                MbufFree(Me.m_mImgIFFT)
                Me.m_mImgIFFT = M_NULL
            End If
            MbufAlloc2d(Me.m_MilSystem, Me.m_mImgSizeX, Me.m_mImgSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC + M_DISP, Me.m_mImgIFFT)
            ReDim Me.m_AryIFFT(Me.m_mImgSizeX * Me.m_mImgSizeY - 1)

        End If

        MbufClear(Me.m_mImgSource, M_COLOR_BLACK)
        MbufClear(Me.m_mImgFFT, M_COLOR_BLACK)
        MbufClear(m_mImgBinarize, 0)
        MbufClear(Me.m_mImgMask, M_COLOR_BLACK)
        MbufClear(Me.m_mImgIFFT, M_COLOR_BLACK)

        MbufCopy(mImg, Me.m_mImgSource)
        MbufGet(Me.m_mImgSource, Me.m_ArySource)

        Me.ViewerChange(Me.Viewer1, Me.m_mImgSource)
        Me.ViewerChange(Me.Viewer2, Me.m_mImgFFT)
        Me.ViewerChange(Me.Viewer3, Me.m_mImgBinarize)
        Me.ViewerChange(Me.Viewer4, Me.m_mImgMask)
        Me.ViewerChange(Me.Viewer5, Me.m_mImgIFFT)

    End Sub

    Private Sub TuneForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        Me.TableLayoutPanel2.Controls.Add(Me.Viewer1, 0, 0)
        Me.TableLayoutPanel2.Controls.Add(Me.Viewer2, 1, 0)
        Me.TableLayoutPanel2.Controls.Add(Me.Viewer3, 2, 0)
        Me.TableLayoutPanel2.Controls.Add(Me.Viewer4, 0, 1)
        Me.TableLayoutPanel2.Controls.Add(Me.Viewer5, 1, 1)

    End Sub

    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing

        Me.m_RunProcCycle = False
        Me.m_RunProcResetEvent.Set()
        Me.m_RunProcThread.Join()
        Me.m_RunProcThread.Abort()
        Me.m_RunProcThread = Nothing

        Me.Viewer1.Free()
        Me.Viewer2.Free()
        Me.Viewer3.Free()
        Me.Viewer4.Free()
        Me.Viewer5.Free()

        If Me.m_mImgLoad <> M_NULL Then
            MbufFree(Me.m_mImgLoad)
            Me.m_mImgLoad = M_NULL
        End If

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

    End Sub

    Private Sub btn_TestImage_Click(sender As Object, e As EventArgs) Handles btn_TestImage.Click

        Me.Invoke(Sub()
                      btn_LoadImage.Enabled = False
                      btn_TestImage.Enabled = False
                      Ppg_ParameterRecipe.Enabled = False
                  End Sub)

        Me.m_RunProcResetEvent.Set()
    End Sub

    Private Sub btn_LoadImage_Click(sender As Object, e As EventArgs) Handles btn_LoadImage.Click

        Dim tmp_InputImageSizeX As MIL_INT = 1
        Dim tmp_InputImageSizeY As MIL_INT = 1
        Dim tmp_InputImageDepth As MIL_INT = 16

        Dim tmp_openDialog As New OpenFileDialog()
        tmp_openDialog.Filter = "影像檔案|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff"

        If tmp_openDialog.ShowDialog() = DialogResult.OK Then
            MbufDiskInquire(tmp_openDialog.FileName, M_SIZE_X, tmp_InputImageSizeX)
            MbufDiskInquire(tmp_openDialog.FileName, M_SIZE_Y, tmp_InputImageSizeY)
            MbufDiskInquire(tmp_openDialog.FileName, M_SIZE_BIT, tmp_InputImageDepth)

            If Me.m_mImgLoad <> M_NULL Then
                MbufFree(Me.m_mImgLoad)
                Me.m_mImgLoad = M_NULL
            End If

            MbufAlloc2d(Me.m_MilSystem, tmp_InputImageSizeX, tmp_InputImageSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC + M_DISP, Me.m_mImgLoad)
            MbufLoad(tmp_openDialog.FileName, Me.m_mImgLoad)
            Me.UpdateImage(Me.m_mImgLoad, Me.m_ParametetRecipe.ImageScaleValue, Me.m_ParametetRecipe.BinaryThreshold, Me.m_ParametetRecipe.MaskSizeX, Me.m_ParametetRecipe.MaskSizeY, tmp_openDialog.FileName)
        End If


    End Sub

    Private Sub RunProc()

        While Me.m_RunProcCycle

            Me.m_RunProcResetEvent.WaitOne()

            If Not Me.m_RunProcCycle Then Exit While

            Dim TotalWatch As New Stopwatch
            Dim FFTWatch As New Stopwatch
            TotalWatch.Start()

            FFTWatch.Start()
            Me.m_CudaCore.CudaSetFFTConfig(192, Me.m_mImgSizeX, Me.m_mImgSizeY, Me.m_ParametetRecipe.ImageScaleValue, Me.m_ParametetRecipe.BinaryThreshold, Me.m_ParametetRecipe.MaskSizeX, Me.m_ParametetRecipe.MaskSizeY, False)
            Me.m_CudaCore.CudaExcuteFFT2D(Me.m_ArySource, Me.m_AryIFFT, Me.m_AryFFT, Me.m_AryBinarize, Me.m_AryMask)
            FFTWatch.Stop()

            MbufPut(Me.m_mImgFFT, Me.m_AryFFT)
            Me.ViewerChange(Me.Viewer2, Me.m_mImgFFT)

            MbufPut(Me.m_mImgBinarize, Me.m_AryBinarize)
            Me.ViewerChange(Me.Viewer3, Me.m_mImgBinarize)

            MbufPut(Me.m_mImgMask, Me.m_AryMask)
            Me.ViewerChange(Me.Viewer4, Me.m_mImgMask)

            MbufPut(Me.m_mImgIFFT, Me.m_AryIFFT)
            Me.ViewerChange(Me.Viewer5, Me.m_mImgIFFT)

            Dim tmp_IFFTSave As MIL_ID
            Dim tmp_FFTFileExt As String = System.IO.Path.GetExtension(Me.m_ParametetRecipe.FilePath)
            Dim tmp_FFTFilePath As String = Me.m_ParametetRecipe.FilePath.Replace(tmp_FFTFileExt, "_FFT" + tmp_FFTFileExt)

            MbufAlloc2d(Me.m_MilSystem, Me.m_mImgSizeX, Me.m_mImgSizeY, 16 + M_UNSIGNED, M_IMAGE + M_PROC + M_DISP, tmp_IFFTSave)

            MimClip(Me.m_mImgIFFT, Me.m_mImgIFFT, M_LESS, 0.0, M_NULL, 0, M_NULL)
            MimConvert(Me.m_mImgIFFT, tmp_IFFTSave, M_DEFAULT)
            MbufSave(tmp_FFTFilePath, tmp_IFFTSave)

            MbufFree(tmp_IFFTSave)
            tmp_IFFTSave = M_NULL

            TotalWatch.Stop()

            Dim totalMs As Long = TotalWatch.ElapsedMilliseconds
            Dim fftMs As Long = FFTWatch.ElapsedMilliseconds

            Me.Invoke(Sub()
                          btn_TestImage.Enabled = True
                          btn_LoadImage.Enabled = True
                          Ppg_ParameterRecipe.Enabled = True
                          lbl_TotalTime.Text = String.Format("總時間: {0} ms", totalMs)
                          lbl_FFTTime.Text = String.Format("FFT時間: {0} ms", fftMs)
                      End Sub)

        End While
    End Sub

    Delegate Sub ViewerChangeCallback(ByVal Viewer_Component As CudaFFTComponent.Viewer, ByVal mImg As MIL_ID)

    Private Sub ViewerChange(ByVal Viewer_Component As CudaFFTComponent.Viewer, ByVal mImg As MIL_ID)
        If Viewer_Component.InvokeRequired Then
            Me.Invoke(New ViewerChangeCallback(AddressOf Me.ViewerChange), New Object() {Viewer_Component, mImg})
        Else
            Viewer_Component.LoadImageMILID(mImg)
            Me.Update()
        End If

    End Sub

    Private Sub TuneForm_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        Me.Viewer1.RefreshUI()
    End Sub
End Class

Public Class ParameterConfig

    <Category("ParameterSetting"), DisplayName("ImageScaleValue"), DefaultValue(255), Description("scale.")>
    Public Property ImageScaleValue As String

    <Category("ParameterSetting"), DisplayName("BinaryThreshold"), DefaultValue(128), Description("threshold.")>
    Public Property BinaryThreshold As String

    <Category("ParameterSetting"), DisplayName("MaskSizeX"), DefaultValue(100), Description("mask size x")>
    Public Property MaskSizeX As UInteger

    <Category("ParameterSetting"), DisplayName("MaskSizeY"), DefaultValue(100), Description("mask size y")>
    Public Property MaskSizeY As UInteger

    <Category("ParameterSetting"), DisplayName("FilePath"), DefaultValue(""), Description("filePath")>
    Public Property FilePath As String

    Sub New()
        Me.ImageScaleValue = 255
        Me.BinaryThreshold = 128
        Me.MaskSizeX = 100
        Me.MaskSizeY = 100
        Me.FilePath = ""
    End Sub

End Class
