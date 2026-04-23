Imports System
Imports System.IO
Imports AUO.SubSystemControl
Imports CudaFFTComponent.FunctionIF
Imports Matrox.MatroxImagingLibrary
Imports Matrox.MatroxImagingLibrary.MIL

Public Class MainForm
    Private m_MilApplication As MIL_ID
    Private m_MilSystem As MIL_ID
    Private m_FuncIF As CudaFFTComponent.FunctionIF
    Private WithEvents m_ControllerDispatcher As CSubSystemDispatcher   'SubSystem Control Server

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        MappAlloc(M_NULL, M_DEFAULT, Me.m_MilApplication)
        MsysAlloc(Me.m_MilApplication, "M_SYSTEM_HOST", M_DEFAULT, M_DEFAULT, Me.m_MilSystem)
        Me.m_FuncIF = New CudaFFTComponent.FunctionIF(Me.m_MilSystem)
    End Sub

    Private Sub btn_TuneForm_Click(sender As Object, e As EventArgs) Handles btn_TuneForm.Click

        Dim tmp_InputImage As MIL_ID = M_NULL
        Dim tmp_InputImageSizeX As MIL_INT = 1
        Dim tmp_InputImageSizeY As MIL_INT = 1

        Dim tmp_ScaleValue As MIL_INT = 255
        Dim tmp_ThresholdValue As MIL_INT = 120
        Dim tmp_MaskWidth As MIL_INT = 2700
        Dim tmp_MaskHeight As MIL_INT = 3800

        Dim tmp_openDialog As New OpenFileDialog()
        tmp_openDialog.Filter = "影像檔案|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff"

        If tmp_openDialog.ShowDialog() = DialogResult.OK Then
            MbufDiskInquire(tmp_openDialog.FileName, M_SIZE_X, tmp_InputImageSizeX)
            MbufDiskInquire(tmp_openDialog.FileName, M_SIZE_Y, tmp_InputImageSizeY)

            MbufAlloc2d(Me.m_MilSystem, tmp_InputImageSizeX, tmp_InputImageSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC + M_DISP, tmp_InputImage)
            MbufLoad(tmp_openDialog.FileName, tmp_InputImage)

            Me.m_FuncIF.TuneFormShowUI(tmp_InputImage, tmp_ScaleValue, tmp_ThresholdValue, tmp_MaskWidth, tmp_MaskHeight, tmp_openDialog.FileName)
        End If

        If tmp_InputImage <> M_NULL Then
            MbufFree(tmp_InputImage)
            tmp_InputImage = M_NULL
        End If

    End Sub

    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing

        If Me.m_mImgLoadFFT <> M_NULL Then
            MbufFree(Me.m_mImgLoadFFT)
            Me.m_mImgLoadFFT = M_NULL
        End If

        If Me.m_mImgResultFFT <> M_NULL Then
            MbufFree(Me.m_mImgResultFFT)
            Me.m_mImgResultFFT = M_NULL
        End If

        If Me.m_ControllerDispatcher IsNot Nothing Then
            Me.m_ControllerDispatcher.Disconnect()
            Me.m_ControllerDispatcher.StopListen()
            Me.m_ControllerDispatcher = Nothing
        End If

        Me.m_FuncIF.Dispose()
        MsysFree(Me.m_MilSystem)
        MappFree(Me.m_MilApplication)
    End Sub

    Private Sub btn_FFTCalculate_Click(sender As Object, e As EventArgs) Handles btn_FFTCalculate.Click

        Dim tmp_InputImage As MIL_ID = M_NULL
        Dim tmp_ResultImage As MIL_ID = M_NULL
        Dim tmp_InputImageSizeX As MIL_INT = 1
        Dim tmp_InputImageSizeY As MIL_INT = 1

        Dim tmp_ScaleValue As MIL_INT = 255
        Dim tmp_ThresholdValue As MIL_INT = 120
        Dim tmp_MaskWidth As MIL_INT = 2700
        Dim tmp_MaskHeight As MIL_INT = 3800

        Dim tmp_openDialog As New OpenFileDialog()
        tmp_openDialog.Filter = "影像檔案|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff"

        If tmp_openDialog.ShowDialog() = DialogResult.OK Then
            MbufDiskInquire(tmp_openDialog.FileName, M_SIZE_X, tmp_InputImageSizeX)
            MbufDiskInquire(tmp_openDialog.FileName, M_SIZE_Y, tmp_InputImageSizeY)

            MbufAlloc2d(Me.m_MilSystem, tmp_InputImageSizeX, tmp_InputImageSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC, tmp_InputImage)
            MbufAlloc2d(Me.m_MilSystem, tmp_InputImageSizeX, tmp_InputImageSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC, tmp_ResultImage)
            MbufLoad(tmp_openDialog.FileName, tmp_InputImage)
            Me.m_FuncIF.CudaFFTCalculate(tmp_InputImage, tmp_ResultImage, tmp_ScaleValue, tmp_ThresholdValue, tmp_MaskWidth, tmp_MaskHeight)
            MbufSave("D:\tmp_ResultImage-1buffer-mutex.tif", tmp_ResultImage)
        End If

        If tmp_InputImage <> M_NULL Then
            MbufFree(tmp_InputImage)
            tmp_InputImage = M_NULL
        End If

        If tmp_ResultImage <> M_NULL Then
            MbufFree(tmp_ResultImage)
            tmp_ResultImage = M_NULL
        End If

    End Sub

    Private m_mImgLoadFFT As MIL_ID = M_NULL
    Private m_mImgResultFFT As MIL_ID = M_NULL
    Private m_LoadSizeX As MIL_INT = 1
    Private m_LoadSizeY As MIL_INT = 1

    Private m_BinarizeValue As MIL_INT = 128
    Private m_MaskSizeX As MIL_INT = 2700
    Private m_MaskSizeY As MIL_INT = 3800

    Private Sub MemoryTestFFT(ByVal binvalue As Integer, ByVal maskx As Integer, ByVal masky As Integer, ByVal inputpath As String, ByVal outputpath As String)

        Me.m_FuncIF.CudaFFTCalculate(Me.m_mImgLoadFFT, Me.m_mImgResultFFT, 255, binvalue, maskx, maskx)
        MbufSave(inputpath, Me.m_mImgLoadFFT)
        MbufSave(outputpath, Me.m_mImgResultFFT)
    End Sub

    Private Sub MemoryLoadFFT(ByVal filepath As String)

        If Me.m_mImgLoadFFT <> M_NULL Then
            MbufFree(Me.m_mImgLoadFFT)
            Me.m_mImgLoadFFT = M_NULL
        End If

        If Me.m_mImgResultFFT <> M_NULL Then
            MbufFree(Me.m_mImgResultFFT)
            Me.m_mImgResultFFT = M_NULL
        End If

        MbufDiskInquire(filepath, M_SIZE_X, Me.m_LoadSizeX)
        MbufDiskInquire(filepath, M_SIZE_Y, Me.m_LoadSizeY)

        MbufAlloc2d(Me.m_MilSystem, Me.m_LoadSizeX, Me.m_LoadSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC, Me.m_mImgLoadFFT)
        MbufAlloc2d(Me.m_MilSystem, Me.m_LoadSizeX, Me.m_LoadSizeY, 32 + M_FLOAT, M_IMAGE + M_PROC, Me.m_mImgResultFFT)
        MbufLoad(filepath, Me.m_mImgLoadFFT)

    End Sub

    Private Sub m_SystemDispatcher_RemoteConnectComing() Handles m_ControllerDispatcher.RemoteConnectComing
        Console.WriteLine("[System] Controller Connected.")
    End Sub

    Private Sub m_SystemDispatcher_RemoteDisconnect() Handles m_ControllerDispatcher.RemoteDisconnect
        Try
            Console.WriteLine("[System] Controller DisConnect.")
        Catch ex As System.Threading.ThreadAbortException
            Console.WriteLine("[RemoteDisconnect]" & ex.Message & ex.StackTrace, "Error")
        Catch ex As Exception
            Console.WriteLine("[RemoteDisconnect]" & ex.Message & ex.StackTrace, "Error")
        End Try
    End Sub

    Private Sub m_SystemDispatcher_ReceiveOccurError(ByVal ErrMessage As String) Handles m_ControllerDispatcher.ReceiveOccurError
        Try
            Console.WriteLine(String.Format("[Error] Receive Error,Message={0}", ErrMessage))
        Catch ex As System.Threading.ThreadAbortException
            Console.WriteLine("[ReceiveOccurError]" & ex.Message & ex.StackTrace, "Error")
        Catch ex As Exception
            Console.WriteLine("[ReceiveOccurError]" & ex.Message & ex.StackTrace, "Error")
        End Try
    End Sub

    Private Sub m_SystemDispatcher_RemoteControlEventHandler(ByVal Request As CRequest) Handles m_ControllerDispatcher.RemoteControl

        Dim retParam(9) As String
        Dim SubSystemResult As CResponseResult

        Try

            For Each s As String In retParam
                s = ""
            Next

            Console.WriteLine("[Cmd] => " & Request.Command & " " & Request.Param1 & " " & Request.Param2 & " " & Request.Param3 & " " & Request.Param4 & " " & Request.Param5 & " " & Request.Param6 & " " & Request.Param7 & " " & Request.Param8 & " " & Request.Param9)

            Select Case Request.Command.Trim.ToUpper()

                Case "LOADIMAGE"
                    Console.WriteLine("LoadImageFFT")
                    Me.MemoryLoadFFT(Request.Param1)

                Case "EXCUTEFFT"
                    Console.WriteLine("Excute MemoryTestFFT")
                    Me.MemoryTestFFT(Request.Param1, Request.Param2, Request.Param3, Request.Param4, Request.Param5)

            End Select

        Catch ex As System.Threading.ThreadAbortException
            Console.WriteLine("[RemoteControlEventHandler]" & ex.Message & ex.StackTrace, "Error")
        Catch ex As Exception
            Console.WriteLine("[RemoteControlEventHandler]" & ex.Message & ex.StackTrace, "Error")
        End Try
    End Sub

    Private Sub StartListen_Click(sender As Object, e As EventArgs) Handles btn_StartListen.Click

        Me.m_ControllerDispatcher = New CSubSystemDispatcher
        Me.m_ControllerDispatcher.CreateListener(Me.tb_IP.Text, Me.nud_Port.Value)
        Me.m_ControllerDispatcher.StartListen()

    End Sub

End Class
