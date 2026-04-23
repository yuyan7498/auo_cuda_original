<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class TuneForm
    Inherits System.Windows.Forms.Form

    'Form 覆寫 Dispose 以清除元件清單。
    <System.Diagnostics.DebuggerNonUserCode()>
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
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.TableLayoutPanel2 = New System.Windows.Forms.TableLayoutPanel()
        Me.TableLayoutPanel1 = New System.Windows.Forms.TableLayoutPanel()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.Ppg_ParameterRecipe = New System.Windows.Forms.PropertyGrid()
        Me.btn_LoadImage = New System.Windows.Forms.Button()
        Me.btn_TestImage = New System.Windows.Forms.Button()
        Me.lbl_TotalTime = New System.Windows.Forms.Label()
        Me.lbl_FFTTime = New System.Windows.Forms.Label()
        Me.TableLayoutPanel1.SuspendLayout()
        Me.GroupBox1.SuspendLayout()
        Me.SuspendLayout()
        '
        'TableLayoutPanel2
        '
        Me.TableLayoutPanel2.ColumnCount = 3
        Me.TableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333!))
        Me.TableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333!))
        Me.TableLayoutPanel2.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333!))
        Me.TableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel2.Location = New System.Drawing.Point(3, 3)
        Me.TableLayoutPanel2.Name = "TableLayoutPanel2"
        Me.TableLayoutPanel2.RowCount = 2
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel2.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50.0!))
        Me.TableLayoutPanel2.Size = New System.Drawing.Size(1028, 755)
        Me.TableLayoutPanel2.TabIndex = 0
        '
        'TableLayoutPanel1
        '
        Me.TableLayoutPanel1.ColumnCount = 2
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.ColumnStyles.Add(New System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150.0!))
        Me.TableLayoutPanel1.Controls.Add(Me.TableLayoutPanel2, 0, 0)
        Me.TableLayoutPanel1.Controls.Add(Me.GroupBox1, 1, 0)
        Me.TableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.TableLayoutPanel1.Location = New System.Drawing.Point(0, 0)
        Me.TableLayoutPanel1.Name = "TableLayoutPanel1"
        Me.TableLayoutPanel1.RowCount = 1
        Me.TableLayoutPanel1.RowStyles.Add(New System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100.0!))
        Me.TableLayoutPanel1.Size = New System.Drawing.Size(1184, 761)
        Me.TableLayoutPanel1.TabIndex = 2
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.Add(Me.lbl_FFTTime)
        Me.GroupBox1.Controls.Add(Me.lbl_TotalTime)
        Me.GroupBox1.Controls.Add(Me.Ppg_ParameterRecipe)
        Me.GroupBox1.Controls.Add(Me.btn_LoadImage)
        Me.GroupBox1.Controls.Add(Me.btn_TestImage)
        Me.GroupBox1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GroupBox1.Location = New System.Drawing.Point(1037, 3)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(144, 755)
        Me.GroupBox1.TabIndex = 1
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Parament Setting"
        '
        'Ppg_ParameterRecipe
        '
        Me.Ppg_ParameterRecipe.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Ppg_ParameterRecipe.Location = New System.Drawing.Point(9, 175)
        Me.Ppg_ParameterRecipe.Name = "Ppg_ParameterRecipe"
        Me.Ppg_ParameterRecipe.Size = New System.Drawing.Size(138, 520)
        Me.Ppg_ParameterRecipe.TabIndex = 1
        '
        'lbl_TotalTime
        '
        Me.lbl_TotalTime.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lbl_TotalTime.Font = New System.Drawing.Font("Microsoft JhengHei UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lbl_TotalTime.Location = New System.Drawing.Point(6, 705)
        Me.lbl_TotalTime.Name = "lbl_TotalTime"
        Me.lbl_TotalTime.Size = New System.Drawing.Size(132, 20)
        Me.lbl_TotalTime.TabIndex = 2
        Me.lbl_TotalTime.Text = "總時間: -- ms"
        '
        'lbl_FFTTime
        '
        Me.lbl_FFTTime.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.lbl_FFTTime.Font = New System.Drawing.Font("Microsoft JhengHei UI", 9.0!, System.Drawing.FontStyle.Bold)
        Me.lbl_FFTTime.ForeColor = System.Drawing.Color.DarkBlue
        Me.lbl_FFTTime.Location = New System.Drawing.Point(6, 728)
        Me.lbl_FFTTime.Name = "lbl_FFTTime"
        Me.lbl_FFTTime.Size = New System.Drawing.Size(132, 20)
        Me.lbl_FFTTime.TabIndex = 3
        Me.lbl_FFTTime.Text = "FFT時間: -- ms"
        '
        'btn_LoadImage
        '
        Me.btn_LoadImage.Location = New System.Drawing.Point(6, 98)
        Me.btn_LoadImage.Name = "btn_LoadImage"
        Me.btn_LoadImage.Size = New System.Drawing.Size(132, 71)
        Me.btn_LoadImage.TabIndex = 0
        Me.btn_LoadImage.Text = "載圖"
        Me.btn_LoadImage.UseVisualStyleBackColor = True
        '
        'btn_TestImage
        '
        Me.btn_TestImage.Location = New System.Drawing.Point(6, 21)
        Me.btn_TestImage.Name = "btn_TestImage"
        Me.btn_TestImage.Size = New System.Drawing.Size(132, 71)
        Me.btn_TestImage.TabIndex = 0
        Me.btn_TestImage.Text = "測試"
        Me.btn_TestImage.UseVisualStyleBackColor = True
        '
        'TuneForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1184, 761)
        Me.Controls.Add(Me.TableLayoutPanel1)
        Me.Name = "TuneForm"
        Me.Text = "TuneForm"
        Me.TableLayoutPanel1.ResumeLayout(False)
        Me.GroupBox1.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents TableLayoutPanel2 As TableLayoutPanel
    Friend WithEvents TableLayoutPanel1 As TableLayoutPanel
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents Ppg_ParameterRecipe As PropertyGrid
    Friend WithEvents btn_LoadImage As Button
    Friend WithEvents btn_TestImage As Button
    Friend WithEvents lbl_TotalTime As Label
    Friend WithEvents lbl_FFTTime As Label
End Class
