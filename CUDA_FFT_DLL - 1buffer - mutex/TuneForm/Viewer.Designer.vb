<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Viewer
    Inherits System.Windows.Forms.UserControl

    'UserControl 覆寫 Dispose 以清除元件清單。
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    '為 Windows Form 設計工具的必要項
    Private components As System.ComponentModel.IContainer

    '注意: 以下為 Windows Form 設計工具所需的程序
    '可以使用 Windows Form 設計工具進行修改。
    '請勿使用程式碼編輯器進行修改。
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.gb_ImageDisplayer = New System.Windows.Forms.GroupBox()
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel()
        Me.Pnl_DisplayInfo = New System.Windows.Forms.Panel()
        Me.lab_RectSizeXValue = New System.Windows.Forms.Label()
        Me.lab_RectSizeYValue = New System.Windows.Forms.Label()
        Me.lab_RectStartXValue = New System.Windows.Forms.Label()
        Me.lab_RectEndXValue = New System.Windows.Forms.Label()
        Me.lab_RectEndYValue = New System.Windows.Forms.Label()
        Me.lab_RectStartYValue = New System.Windows.Forms.Label()
        Me.lab_RectStartX = New System.Windows.Forms.Label()
        Me.lab_RectSizeX = New System.Windows.Forms.Label()
        Me.lab_RectEndX = New System.Windows.Forms.Label()
        Me.lab_RectSizeY = New System.Windows.Forms.Label()
        Me.lab_RectStartY = New System.Windows.Forms.Label()
        Me.lab_RectEndY = New System.Windows.Forms.Label()
        Me.lab_SelectIndex = New System.Windows.Forms.Label()
        Me.lab_ImageSize = New System.Windows.Forms.Label()
        Me.lab_GrayMeanValue = New System.Windows.Forms.Label()
        Me.lab_ZoomValue = New System.Windows.Forms.Label()
        Me.lab_MousePt = New System.Windows.Forms.Label()
        Me.Pnl_ImageDisplay = New System.Windows.Forms.Panel()
        Me.gb_ImageDisplayer.SuspendLayout()
        Me.TableLayoutPanel1.SuspendLayout()
        Me.Pnl_DisplayInfo.SuspendLayout()
        Me.SuspendLayout()
        '
        'gb_ImageDisplayer
        '
        Me.gb_ImageDisplayer.Controls.Add(Me.TableLayoutPanel1)
        Me.gb_ImageDisplayer.Dock = System.Windows.Forms.DockStyle.Fill
        Me.gb_ImageDisplayer.Location = New System.Drawing.Point(0, 0)
        Me.gb_ImageDisplayer.Name = "gb_ImageDisplayer"
        Me.gb_ImageDisplayer.Size = New System.Drawing.Size(546, 536)
        Me.gb_ImageDisplayer.TabIndex = 10
        Me.gb_ImageDisplayer.TabStop = False
        Me.gb_ImageDisplayer.Text = "ImageDisplayer"
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.ColumnCount = 1
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.Pnl_DisplayInfo, 0, 1)
        Me.TableLayoutPanel1.Controls.Add(Me.Pnl_ImageDisplay, 0, 0)
        Me.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(3, 18)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 2
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(540, 515)
        Me.TableLayoutPanel1.TabIndex = 5
        '
        'Pnl_DisplayInfo
        '
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectSizeXValue)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectSizeYValue)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectStartXValue)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectEndXValue)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectEndYValue)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectStartYValue)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectStartX)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectSizeX)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectEndX)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectSizeY)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectStartY)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_RectEndY)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_SelectIndex)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_ImageSize)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_GrayMeanValue)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_ZoomValue)
        Me.Pnl_DisplayInfo.Controls.Add(Me.lab_MousePt)
        Me.Pnl_DisplayInfo.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Pnl_DisplayInfo.Location = New System.Drawing.Point(3, 418)
        Me.Pnl_DisplayInfo.Name = "Pnl_DisplayInfo"
        Me.Pnl_DisplayInfo.Size = New System.Drawing.Size(534, 94)
        Me.Pnl_DisplayInfo.TabIndex = 7
        '
        'lab_RectSizeXValue
        '
        Me.lab_RectSizeXValue.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectSizeXValue.AutoSize = True
        Me.lab_RectSizeXValue.Location = New System.Drawing.Point(289, 7)
        Me.lab_RectSizeXValue.Name = "lab_RectSizeXValue"
        Me.lab_RectSizeXValue.Size = New System.Drawing.Size(11, 12)
        Me.lab_RectSizeXValue.TabIndex = 23
        Me.lab_RectSizeXValue.Text = "0"
        '
        'lab_RectSizeYValue
        '
        Me.lab_RectSizeYValue.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectSizeYValue.AutoSize = True
        Me.lab_RectSizeYValue.Location = New System.Drawing.Point(289, 24)
        Me.lab_RectSizeYValue.Name = "lab_RectSizeYValue"
        Me.lab_RectSizeYValue.Size = New System.Drawing.Size(11, 12)
        Me.lab_RectSizeYValue.TabIndex = 22
        Me.lab_RectSizeYValue.Text = "0"
        '
        'lab_RectStartXValue
        '
        Me.lab_RectStartXValue.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectStartXValue.AutoSize = True
        Me.lab_RectStartXValue.Location = New System.Drawing.Point(157, 7)
        Me.lab_RectStartXValue.Name = "lab_RectStartXValue"
        Me.lab_RectStartXValue.Size = New System.Drawing.Size(11, 12)
        Me.lab_RectStartXValue.TabIndex = 21
        Me.lab_RectStartXValue.Text = "0"
        '
        'lab_RectEndXValue
        '
        Me.lab_RectEndXValue.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectEndXValue.AutoSize = True
        Me.lab_RectEndXValue.Location = New System.Drawing.Point(159, 42)
        Me.lab_RectEndXValue.Name = "lab_RectEndXValue"
        Me.lab_RectEndXValue.Size = New System.Drawing.Size(11, 12)
        Me.lab_RectEndXValue.TabIndex = 20
        Me.lab_RectEndXValue.Text = "0"
        '
        'lab_RectEndYValue
        '
        Me.lab_RectEndYValue.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectEndYValue.AutoSize = True
        Me.lab_RectEndYValue.Location = New System.Drawing.Point(159, 58)
        Me.lab_RectEndYValue.Name = "lab_RectEndYValue"
        Me.lab_RectEndYValue.Size = New System.Drawing.Size(11, 12)
        Me.lab_RectEndYValue.TabIndex = 18
        Me.lab_RectEndYValue.Text = "0"
        '
        'lab_RectStartYValue
        '
        Me.lab_RectStartYValue.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectStartYValue.AutoSize = True
        Me.lab_RectStartYValue.Location = New System.Drawing.Point(157, 25)
        Me.lab_RectStartYValue.Name = "lab_RectStartYValue"
        Me.lab_RectStartYValue.Size = New System.Drawing.Size(11, 12)
        Me.lab_RectStartYValue.TabIndex = 18
        Me.lab_RectStartYValue.Text = "0"
        '
        'lab_RectStartX
        '
        Me.lab_RectStartX.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectStartX.AutoSize = True
        Me.lab_RectStartX.Location = New System.Drawing.Point(98, 7)
        Me.lab_RectStartX.Name = "lab_RectStartX"
        Me.lab_RectStartX.Size = New System.Drawing.Size(60, 12)
        Me.lab_RectStartX.TabIndex = 17
        Me.lab_RectStartX.Text = "Roi Start X:"
        '
        'lab_RectSizeX
        '
        Me.lab_RectSizeX.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectSizeX.AutoSize = True
        Me.lab_RectSizeX.Location = New System.Drawing.Point(230, 7)
        Me.lab_RectSizeX.Name = "lab_RectSizeX"
        Me.lab_RectSizeX.Size = New System.Drawing.Size(58, 12)
        Me.lab_RectSizeX.TabIndex = 16
        Me.lab_RectSizeX.Text = "Roi Size X:"
        '
        'lab_RectEndX
        '
        Me.lab_RectEndX.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectEndX.AutoSize = True
        Me.lab_RectEndX.Location = New System.Drawing.Point(100, 42)
        Me.lab_RectEndX.Name = "lab_RectEndX"
        Me.lab_RectEndX.Size = New System.Drawing.Size(58, 12)
        Me.lab_RectEndX.TabIndex = 16
        Me.lab_RectEndX.Text = "Roi End X:"
        '
        'lab_RectSizeY
        '
        Me.lab_RectSizeY.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectSizeY.AutoSize = True
        Me.lab_RectSizeY.Location = New System.Drawing.Point(230, 25)
        Me.lab_RectSizeY.Name = "lab_RectSizeY"
        Me.lab_RectSizeY.Size = New System.Drawing.Size(58, 12)
        Me.lab_RectSizeY.TabIndex = 15
        Me.lab_RectSizeY.Text = "Roi Size Y:"
        '
        'lab_RectStartY
        '
        Me.lab_RectStartY.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectStartY.AutoSize = True
        Me.lab_RectStartY.Location = New System.Drawing.Point(98, 25)
        Me.lab_RectStartY.Name = "lab_RectStartY"
        Me.lab_RectStartY.Size = New System.Drawing.Size(60, 12)
        Me.lab_RectStartY.TabIndex = 14
        Me.lab_RectStartY.Text = "Roi Start Y:"
        '
        'lab_RectEndY
        '
        Me.lab_RectEndY.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_RectEndY.AutoSize = True
        Me.lab_RectEndY.Location = New System.Drawing.Point(100, 58)
        Me.lab_RectEndY.Name = "lab_RectEndY"
        Me.lab_RectEndY.Size = New System.Drawing.Size(58, 12)
        Me.lab_RectEndY.TabIndex = 15
        Me.lab_RectEndY.Text = "Roi End Y:"
        '
        'lab_SelectIndex
        '
        Me.lab_SelectIndex.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_SelectIndex.AutoSize = True
        Me.lab_SelectIndex.Location = New System.Drawing.Point(387, 25)
        Me.lab_SelectIndex.Name = "lab_SelectIndex"
        Me.lab_SelectIndex.Size = New System.Drawing.Size(62, 12)
        Me.lab_SelectIndex.TabIndex = 13
        Me.lab_SelectIndex.Text = "SelectIndex:"
        '
        'lab_ImageSize
        '
        Me.lab_ImageSize.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_ImageSize.AutoSize = True
        Me.lab_ImageSize.Location = New System.Drawing.Point(387, 7)
        Me.lab_ImageSize.Name = "lab_ImageSize"
        Me.lab_ImageSize.Size = New System.Drawing.Size(27, 12)
        Me.lab_ImageSize.TabIndex = 13
        Me.lab_ImageSize.Text = "Size:"
        '
        'lab_GrayMeanValue
        '
        Me.lab_GrayMeanValue.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_GrayMeanValue.AutoSize = True
        Me.lab_GrayMeanValue.Location = New System.Drawing.Point(387, 58)
        Me.lab_GrayMeanValue.Name = "lab_GrayMeanValue"
        Me.lab_GrayMeanValue.Size = New System.Drawing.Size(60, 12)
        Me.lab_GrayMeanValue.TabIndex = 1
        Me.lab_GrayMeanValue.Text = "Gray Mean:"
        '
        'lab_ZoomValue
        '
        Me.lab_ZoomValue.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_ZoomValue.AutoSize = True
        Me.lab_ZoomValue.Location = New System.Drawing.Point(387, 42)
        Me.lab_ZoomValue.Name = "lab_ZoomValue"
        Me.lab_ZoomValue.Size = New System.Drawing.Size(36, 12)
        Me.lab_ZoomValue.TabIndex = 0
        Me.lab_ZoomValue.Text = "Zoom:"
        '
        'lab_MousePt
        '
        Me.lab_MousePt.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lab_MousePt.AutoSize = True
        Me.lab_MousePt.Location = New System.Drawing.Point(387, 76)
        Me.lab_MousePt.Name = "lab_MousePt"
        Me.lab_MousePt.Size = New System.Drawing.Size(44, 12)
        Me.lab_MousePt.TabIndex = 0
        Me.lab_MousePt.Text = "( X , Y )"
        '
        'Pnl_ImageDisplay
        '
        Me.Pnl_ImageDisplay.Dock = System.Windows.Forms.DockStyle.Fill
        Me.Pnl_ImageDisplay.Location = New System.Drawing.Point(3, 3)
        Me.Pnl_ImageDisplay.Name = "Pnl_ImageDisplay"
        Me.Pnl_ImageDisplay.Size = New System.Drawing.Size(534, 409)
        Me.Pnl_ImageDisplay.TabIndex = 4
        '
        'Viewer
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.gb_ImageDisplayer)
        Me.Name = "Viewer"
        Me.Size = New System.Drawing.Size(546, 536)
        Me.gb_ImageDisplayer.ResumeLayout(False)
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.Pnl_DisplayInfo.ResumeLayout(False)
        Me.Pnl_DisplayInfo.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents gb_ImageDisplayer As GroupBox
    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents Pnl_DisplayInfo As Panel
    Friend WithEvents lab_RectStartXValue As Label
    Friend WithEvents lab_RectEndXValue As Label
    Friend WithEvents lab_RectEndYValue As Label
    Friend WithEvents lab_RectStartYValue As Label
    Friend WithEvents lab_RectStartX As Label
    Friend WithEvents lab_RectEndX As Label
    Friend WithEvents lab_RectStartY As Label
    Friend WithEvents lab_RectEndY As Label
    Friend WithEvents lab_SelectIndex As Label
    Friend WithEvents lab_ImageSize As Label
    Friend WithEvents lab_GrayMeanValue As Label
    Friend WithEvents lab_ZoomValue As Label
    Friend WithEvents lab_MousePt As Label
    Friend WithEvents Pnl_ImageDisplay As Panel
    Friend WithEvents lab_RectSizeXValue As Label
    Friend WithEvents lab_RectSizeYValue As Label
    Friend WithEvents lab_RectSizeX As Label
    Friend WithEvents lab_RectSizeY As Label
End Class
