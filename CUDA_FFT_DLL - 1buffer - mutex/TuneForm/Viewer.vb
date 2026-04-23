Imports System
Imports System.IO
Imports System.Drawing
Imports Matrox.MatroxImagingLibrary
Imports Matrox.MatroxImagingLibrary.MIL

Public Class Viewer

    Private MilSystem As MIL_ID
    Private MilDisplay As MIL_ID = M_NULL

    Private MouseRightDownEventDelegate As MIL_DISP_HOOK_FUNCTION_PTR
    Private MouseLeftDownEventDelegate As MIL_DISP_HOOK_FUNCTION_PTR
    Private MouseLeftUpEventDelegate As MIL_DISP_HOOK_FUNCTION_PTR
    Private MouseMoveEventDelegate As MIL_DISP_HOOK_FUNCTION_PTR
    Private MouseWheelEventDelegate As MIL_DISP_HOOK_FUNCTION_PTR
    Private MilImage As MIL_ID = M_NULL
    Private MilImageBand As MIL_INT = 1
    Private MilGraphicsContext As MIL_ID = M_NULL
    Private MilGraphicsList As MIL_ID = M_NULL
    Private SelectIndex As MIL_INT = -1
    Private MouseLeftPress As Boolean = False

    Public Sub SetMSystem(ByVal msys As MIL_ID)
        Me.MilSystem = msys

        MIL.MdispAlloc(Me.MilSystem, MIL.M_DEFAULT, "M_DEFAULT", MIL.M_WINDOWED, Me.MilDisplay)

        ' Display mouse event
        Me.MouseRightDownEventDelegate = New MIL_DISP_HOOK_FUNCTION_PTR(AddressOf Me.DisplayMouseRightDownEvent)
        Me.MouseLeftDownEventDelegate = New MIL_DISP_HOOK_FUNCTION_PTR(AddressOf Me.DisplayMouseLeftDownEvent)
        Me.MouseLeftUpEventDelegate = New MIL_DISP_HOOK_FUNCTION_PTR(AddressOf Me.DisplayMouseLeftUpEvent)
        Me.MouseMoveEventDelegate = New MIL_DISP_HOOK_FUNCTION_PTR(AddressOf Me.DisplayMouseMoveEvent)
        Me.MouseWheelEventDelegate = New MIL_DISP_HOOK_FUNCTION_PTR(AddressOf Me.DisplayMouseWheelEvent)
        MIL.MdispHookFunction(Me.MilDisplay, MIL.M_MOUSE_RIGHT_BUTTON_DOWN, Me.MouseRightDownEventDelegate, IntPtr.Zero)
        MIL.MdispHookFunction(Me.MilDisplay, MIL.M_MOUSE_LEFT_BUTTON_DOWN, Me.MouseLeftDownEventDelegate, IntPtr.Zero)
        MIL.MdispHookFunction(Me.MilDisplay, MIL.M_MOUSE_LEFT_BUTTON_UP, Me.MouseLeftUpEventDelegate, IntPtr.Zero)
        MIL.MdispHookFunction(Me.MilDisplay, MIL.M_MOUSE_MOVE, Me.MouseMoveEventDelegate, IntPtr.Zero)
        MIL.MdispHookFunction(Me.MilDisplay, MIL.M_MOUSE_WHEEL, Me.MouseWheelEventDelegate, IntPtr.Zero)

        ' Allocate graphics
        MIL.MgraAlloc(Me.MilSystem, Me.MilGraphicsContext)
        MIL.MgraAllocList(Me.MilSystem, MIL.M_DEFAULT, Me.MilGraphicsList)
        MIL.MgraControlList(Me.MilGraphicsList, MIL.M_LIST, MIL.M_DEFAULT, MIL.M_INTERACTIVE_ANNOTATIONS_COLOR, MIL.M_COLOR_RED)
        MIL.MgraControlList(Me.MilGraphicsList, MIL.M_LIST, MIL.M_DEFAULT, MIL.M_SELECTED_COLOR, MIL.M_COLOR_GREEN)
        MIL.MgraControlList(Me.MilGraphicsList, MIL.M_LIST, MIL.M_DEFAULT, MIL.M_MULTIPLE_SELECTION, MIL.M_DISABLE)
        MIL.MgraControlList(Me.MilGraphicsList, MIL.M_LIST, MIL.M_DEFAULT, MIL.M_ACTION_KEYS, MIL.M_ENABLE)

        ' Display setting And enable graphic interactive
        MIL.MdispControl(Me.MilDisplay, MIL.M_ASSOCIATED_GRAPHIC_LIST_ID, Me.MilGraphicsList)
        MIL.MdispControl(Me.MilDisplay, MIL.M_GRAPHIC_LIST_INTERACTIVE, MIL.M_ENABLE)
        MIL.MdispControl(Me.MilDisplay, MIL.M_MOUSE_USE, MIL.M_ENABLE)
        MIL.MdispControl(Me.MilDisplay, MIL.M_CENTER_DISPLAY, MIL.M_ENABLE)
        MIL.MdispControl(Me.MilDisplay, MIL.M_BACKGROUND_COLOR, MIL.M_RGB888(240, 240, 240))
        MIL.MdispControl(Me.MilDisplay, MIL.M_VIEW_MODE, MIL.M_AUTO_SCALE)
        MIL.MdispControl(Me.MilDisplay, M_SCALE_DISPLAY, M_ENABLE)
    End Sub

    Public Sub LoadImageMILID(ByVal mImg As MIL_ID)

        Dim tmp_ImgSizeX As MIL_INT
        Dim tmp_ImgSizeY As MIL_INT

        '	Free Image Buff
        'If Me.MilImage <> M_NULL Then MbufFree(Me.MilImage)
        'If Me.MilGraphicsList <> M_NULL Then MgraFree(Me.MilGraphicsList)
        'If Me.MilGraphicsContext <> M_NULL Then MgraFree(Me.MilGraphicsContext)

        Me.MilImage = mImg
        MbufInquire(Me.MilImage, M_SIZE_X, tmp_ImgSizeX)
        MbufInquire(Me.MilImage, M_SIZE_Y, tmp_ImgSizeY)
        'MbufInquire(mImg, M_SIZE_BIT, tmp_ImgBits)
        'MbufInquire(mImg, M_SIGN, tmp_ImgSign)
        'MbufInquire(mImg, M_SIZE_BAND, tmp_ImgBand)

        'If tmp_ImgBand = 1 Then

        '    MbufAlloc2d(Me.MilSystem, tmp_ImgSizeX, tmp_ImgSizeY, tmp_ImgBits + M_SIGNED, M_IMAGE + M_PROC + M_DISP, Me.MilImage)
        '    MbufClear(Me.MilImage, M_COLOR_BLACK)

        'ElseIf (tmp_ImgBand = 3) Then

        '    MbufAllocColor(Me.MilSystem, 3, tmp_ImgSizeX, tmp_ImgSizeY, tmp_ImgBits + tmp_ImgSign, M_IMAGE + M_PROC + M_DISP, Me.MilImage)
        '    MbufClear(Me.MilImage, M_COLOR_BLACK)

        'Else
        '    Throw New Exception(String.Format("Unknow Image Band.[{0}]", tmp_ImgBand))
        'End If

        'Load Image
        'MIL.MbufCopy(mImg, Me.MilImage)

        'select display window
        MIL.MdispSelectWindow(Me.MilDisplay, Me.MilImage, Me.Pnl_ImageDisplay.Handle)
        'MIL.MappControl(Me.MilApplication, MIL.M_ERROR, MIL.M_THROW_EXCEPTION)



        ' Show image size
        Me.lab_ImageSize.Text = String.Format("Image Size:( {0} , {1} )", tmp_ImgSizeX, tmp_ImgSizeY)

    End Sub


    Public Sub Free()

        '	Free Image Buff
        'If Me.MilImage <> M_NULL Then
        '    MbufFree(Me.MilImage)
        'End If

        If Me.MilDisplay <> M_NULL Then
            MdispFree(Me.MilDisplay)
            Me.MilDisplay = M_NULL
        End If

        If Me.MilGraphicsList <> M_NULL Then
            MgraFree(Me.MilGraphicsList)
        End If

        If Me.MilGraphicsContext <> M_NULL Then
            MgraFree(Me.MilGraphicsContext)
        End If

    End Sub

    Private Function DisplayMouseRightDownEvent(ByVal hookType As MIL_INT, ByVal eventId As MIL_ID, ByVal userObjectPtr As IntPtr) As MIL_INT
        Try

            Dim tmp_NumberOfPrimitives As MIL_INT = 0
            MIL.MgraInquireList(MilGraphicsList, MIL.M_LIST, MIL.M_DEFAULT, MIL.M_NUMBER_OF_GRAPHICS, tmp_NumberOfPrimitives)

            If tmp_NumberOfPrimitives >= 1 Then
                Me.DelViewerRectRoi(tmp_NumberOfPrimitives)
            Else
                Me.AddViewerRectRoi(tmp_NumberOfPrimitives)
            End If

        Catch ex As Exception

        End Try

        Return 0
    End Function

    Private Function DisplayMouseLeftDownEvent(ByVal hookType As MIL_INT, ByVal eventId As MIL_ID, ByVal userObjectPtr As IntPtr) As MIL_INT
        Try
            Me.MouseLeftPress = True
            Dim numberOfPrimitives As MIL_INT = 0
            Dim selectFlag As MIL_INT = 0
            Me.SelectIndex = -1
            Me.lab_SelectIndex.Text = "SelectIndex:-1"
            MIL.MgraInquireList(MilGraphicsList, MIL.M_LIST, MIL.M_DEFAULT, MIL.M_NUMBER_OF_GRAPHICS, numberOfPrimitives)

            For i As MIL_INT = 0 To numberOfPrimitives - 1
                MIL.MgraInquireList(MilGraphicsList, MIL.M_GRAPHIC_INDEX(i), MIL.M_DEFAULT, MIL.M_GRAPHIC_SELECTED, selectFlag)

                If selectFlag = CType(1, MIL_INT) Then
                    Me.SelectIndex = i
                    Me.lab_SelectIndex.Text = String.Format("SelectIndex:{0}", i)
                    Exit For
                End If
            Next

        Catch ex As Exception
        End Try

        Return 0
    End Function

    Private Function DisplayMouseLeftUpEvent(ByVal hookType As MIL_INT, ByVal eventId As MIL_ID, ByVal userObjectPtr As IntPtr) As MIL_INT
        Me.MouseLeftPress = False
        Return 0
    End Function

    Private Function DisplayMouseMoveEvent(ByVal hookType As MIL_INT, ByVal eventId As MIL_ID, ByVal userObjectPtr As IntPtr) As MIL_INT
        Dim zoomX As Double = 0
        Dim zoomY As Double = 0
        Dim imgSizeX As MIL_INT = 0
        Dim imgSizeY As MIL_INT = 0
        Dim imgAxisX As Double = 0
        Dim imgAxisY As Double = 0

        Try

            If Me.MilDisplay = MIL.M_NULL OrElse Me.MilImage = MIL.M_NULL Then
                Return 0
            End If

            MIL.MbufInquire(Me.MilImage, MIL.M_SIZE_X, imgSizeX)
            MIL.MbufInquire(Me.MilImage, MIL.M_SIZE_Y, imgSizeY)
            MIL.MdispInquire(Me.MilDisplay, MIL.M_ZOOM_FACTOR_X, zoomX)
            MIL.MdispInquire(Me.MilDisplay, MIL.M_ZOOM_FACTOR_Y, zoomY)
            MIL.MdispGetHookInfo(eventId, MIL.M_MOUSE_POSITION_BUFFER_X, imgAxisX)
            MIL.MdispGetHookInfo(eventId, MIL.M_MOUSE_POSITION_BUFFER_Y, imgAxisY)
            Me.lab_ZoomValue.Text = String.Format("Zoom:{0}", zoomX)
            Me.lab_MousePt.Text = String.Format("(X,Y):({0}, {1})", CInt(imgAxisX), CInt(imgAxisY))

            If imgAxisX < 0 OrElse imgAxisY < 0 OrElse CType(imgAxisX, MIL_INT) >= imgSizeX OrElse CType(imgAxisY, MIL_INT) >= imgSizeY Then
                Me.lab_GrayMeanValue.Text = String.Format("Gray Mean:Mull")
            ElseIf Me.MilImageBand = 3 Then
                Dim redValue As Integer() = New Integer(0) {0}
                Dim greenValue As Integer() = New Integer(0) {0}
                Dim blueValue As Integer() = New Integer(0) {0}
                MIL.MbufGetColor2d(Me.MilImage, MIL.M_SINGLE_BAND, MIL.M_RED, CType(imgAxisX, MIL_INT), CType(imgAxisY, MIL_INT), 1, 1, redValue)
                MIL.MbufGetColor2d(Me.MilImage, MIL.M_SINGLE_BAND, MIL.M_GREEN, CType(imgAxisX, MIL_INT), CType(imgAxisY, MIL_INT), 1, 1, greenValue)
                MIL.MbufGetColor2d(Me.MilImage, MIL.M_SINGLE_BAND, MIL.M_BLUE, CType(imgAxisX, MIL_INT), CType(imgAxisY, MIL_INT), 1, 1, blueValue)
                Me.lab_GrayMeanValue.Text = String.Format("Gray Mean:({0}, {1}, {2})", redValue(0), greenValue(0), blueValue(0))
            Else
                Dim grayValue As Single() = New Single(0) {0}
                MIL.MbufGet2d(Me.MilImage, CType(imgAxisX, MIL_INT), CType(imgAxisY, MIL_INT), 1, 1, grayValue)
                Me.lab_GrayMeanValue.Text = String.Format("Gray Mean:{0}", grayValue(0))
            End If

            If Not Me.MouseLeftPress OrElse Me.SelectIndex < 0 Then
                Return 0
            End If

            Dim tmp_StartX As Double = 0
            Dim tmp_StartY As Double = 0
            Dim tmp_EndX As Double = 0
            Dim tmp_EndY As Double = 0
            MIL.MgraInquireList(MilGraphicsList, MIL.M_GRAPHIC_INDEX(Me.SelectIndex), MIL.M_DEFAULT, MIL.M_CORNER_TOP_LEFT_X, tmp_StartX)
            MIL.MgraInquireList(MilGraphicsList, MIL.M_GRAPHIC_INDEX(Me.SelectIndex), MIL.M_DEFAULT, MIL.M_CORNER_TOP_LEFT_Y, tmp_StartY)
            MIL.MgraInquireList(MilGraphicsList, MIL.M_GRAPHIC_INDEX(Me.SelectIndex), MIL.M_DEFAULT, MIL.M_CORNER_BOTTOM_RIGHT_X, tmp_EndX)
            MIL.MgraInquireList(MilGraphicsList, MIL.M_GRAPHIC_INDEX(Me.SelectIndex), MIL.M_DEFAULT, MIL.M_CORNER_BOTTOM_RIGHT_Y, tmp_EndY)
            Me.lab_RectStartXValue.Text = String.Format("{0}", Math.Round(tmp_StartX, MidpointRounding.AwayFromZero))
            Me.lab_RectStartYValue.Text = String.Format("{0}", Math.Round(tmp_StartY, MidpointRounding.AwayFromZero))
            Me.lab_RectEndXValue.Text = String.Format("{0}", Math.Round(tmp_EndX, MidpointRounding.AwayFromZero))
            Me.lab_RectEndYValue.Text = String.Format("{0}", Math.Round(tmp_EndY, MidpointRounding.AwayFromZero))
            Me.lab_RectSizeXValue.Text = String.Format("{0}", Convert.ToUInt32(Me.lab_RectEndXValue.Text) - Convert.ToUInt32(Me.lab_RectStartXValue.Text))
            Me.lab_RectSizeYValue.Text = String.Format("{0}", Convert.ToUInt32(Me.lab_RectEndYValue.Text) - Convert.ToUInt32(Me.lab_RectStartYValue.Text))
        Catch ex As Exception

        End Try

        Return 0
    End Function

    Private Function DisplayMouseWheelEvent(ByVal hookType As MIL_INT, ByVal eventId As MIL_ID, ByVal userObjectPtr As IntPtr) As MIL_INT
        Dim tmp_ZoomValue As Double = 0

        Try
            MIL.MdispInquire(Me.MilDisplay, MIL.M_ZOOM_FACTOR_X, tmp_ZoomValue)
            Me.lab_ZoomValue.Text = String.Format("Zoom:{0}", tmp_ZoomValue)
        Catch ex As Exception
        End Try

        Return 0
    End Function

    Public Function SetDisplayZoomFactor() As MIL_INT
        Dim tmp_ZoomValue As Double = 0.125

        Try
            MIL.MdispZoom(Me.MilDisplay, MIL.M_ZOOM_FACTOR_X, tmp_ZoomValue)
            MIL.MdispZoom(Me.MilDisplay, MIL.M_ZOOM_FACTOR_Y, tmp_ZoomValue)
            Me.lab_ZoomValue.Text = String.Format("Zoom:{0}", tmp_ZoomValue)
        Catch ex As Exception

        End Try

        Return 0
    End Function

    Private Sub AddViewerRectRoi(ByVal NumberOfPrimitives As MIL_INT)

        MIL.MgraInquireList(MilGraphicsList, MIL.M_LIST, MIL.M_DEFAULT, MIL.M_NUMBER_OF_GRAPHICS, NumberOfPrimitives)
        MIL.MgraColor(Me.MilGraphicsContext, MIL.M_COLOR_RED)

        MIL.MgraRect(Me.MilGraphicsContext, Me.MilGraphicsList, 1, 1, 10, 10)
        MIL.MgraControlList(Me.MilGraphicsList, MIL.M_LIST, MIL.M_DEFAULT, MIL.M_MODE_RESIZE, MIL.M_DEFAULT)

        MIL.MgraControlList(Me.MilGraphicsList, MIL.M_GRAPHIC_INDEX(NumberOfPrimitives), MIL.M_DEFAULT, MIL.M_VISIBLE, MIL.M_TRUE)
        MIL.MgraControlList(Me.MilGraphicsList, MIL.M_GRAPHIC_INDEX(NumberOfPrimitives), MIL.M_DEFAULT, MIL.M_ROTATABLE, MIL.M_DISABLE)
        MIL.MgraControlList(Me.MilGraphicsList, MIL.M_GRAPHIC_INDEX(NumberOfPrimitives), MIL.M_DEFAULT, MIL.M_RESIZABLE, MIL.M_ENABLE)
    End Sub

    Private Sub DelViewerRectRoi(ByVal NumberOfPrimitives As MIL_INT)

        ' Delete All
        'MIL.MgraControlList(Me.MilGraphicsList, MIL.M_ALL, MIL.M_DEFAULT, MIL.M_DELETE, MIL.M_DEFAULT)

        Dim tmp_SelectFlag As MIL_INT = 0
        For i As MIL_INT = 0 To NumberOfPrimitives - 1
            MIL.MgraInquireList(MilGraphicsList, MIL.M_GRAPHIC_INDEX(i), MIL.M_DEFAULT, MIL.M_GRAPHIC_SELECTED, tmp_SelectFlag)

            If tmp_SelectFlag = CType(1, MIL_INT) Then
                MIL.MgraControlList(Me.MilGraphicsList, MIL.M_GRAPHIC_INDEX(i), MIL.M_DEFAULT, MIL.M_DELETE, MIL.M_DEFAULT)
                Exit For
            End If
        Next
    End Sub

    Public Sub RefreshUI()

        MIL.MgraRect(Me.MilGraphicsContext, Me.MilGraphicsList, 1, 1, 10, 10)
        MIL.MgraControlList(Me.MilGraphicsList, MIL.M_LIST, MIL.M_DEFAULT, MIL.M_MODE_RESIZE, MIL.M_DEFAULT)
        MIL.MgraControlList(Me.MilGraphicsList, MIL.M_GRAPHIC_INDEX(0), MIL.M_DEFAULT, MIL.M_DELETE, MIL.M_DEFAULT)

    End Sub

End Class
