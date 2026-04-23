<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class MainForm
    Inherits System.Windows.Forms.Form

    'Form 覆寫 Dispose 以清除元件清單。
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
        Me.btn_TuneForm = New System.Windows.Forms.Button()
        Me.btn_FFTCalculate = New System.Windows.Forms.Button()
        Me.tb_IP = New System.Windows.Forms.TextBox()
        Me.lab_IP = New System.Windows.Forms.Label()
        Me.lab_Port = New System.Windows.Forms.Label()
        Me.nud_Port = New System.Windows.Forms.NumericUpDown()
        Me.btn_StartListen = New System.Windows.Forms.Button()
        CType(Me.nud_Port, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'btn_TuneForm
        '
        Me.btn_TuneForm.Location = New System.Drawing.Point(12, 12)
        Me.btn_TuneForm.Name = "btn_TuneForm"
        Me.btn_TuneForm.Size = New System.Drawing.Size(203, 71)
        Me.btn_TuneForm.TabIndex = 0
        Me.btn_TuneForm.Text = "TuneForm"
        Me.btn_TuneForm.UseVisualStyleBackColor = True
        '
        'btn_FFTCalculate
        '
        Me.btn_FFTCalculate.Location = New System.Drawing.Point(12, 89)
        Me.btn_FFTCalculate.Name = "btn_FFTCalculate"
        Me.btn_FFTCalculate.Size = New System.Drawing.Size(203, 71)
        Me.btn_FFTCalculate.TabIndex = 0
        Me.btn_FFTCalculate.Text = "FFTCalculate"
        Me.btn_FFTCalculate.UseVisualStyleBackColor = True
        '
        'tb_IP
        '
        Me.tb_IP.Location = New System.Drawing.Point(87, 243)
        Me.tb_IP.Name = "tb_IP"
        Me.tb_IP.Size = New System.Drawing.Size(128, 22)
        Me.tb_IP.TabIndex = 3
        Me.tb_IP.Text = "127.0.0.1"
        '
        'lab_IP
        '
        Me.lab_IP.AutoSize = True
        Me.lab_IP.Location = New System.Drawing.Point(10, 246)
        Me.lab_IP.Name = "lab_IP"
        Me.lab_IP.Size = New System.Drawing.Size(15, 12)
        Me.lab_IP.TabIndex = 1
        Me.lab_IP.Text = "IP"
        '
        'lab_Port
        '
        Me.lab_Port.AutoSize = True
        Me.lab_Port.Location = New System.Drawing.Point(10, 271)
        Me.lab_Port.Name = "lab_Port"
        Me.lab_Port.Size = New System.Drawing.Size(24, 12)
        Me.lab_Port.TabIndex = 1
        Me.lab_Port.Text = "Port"
        '
        'nud_Port
        '
        Me.nud_Port.Location = New System.Drawing.Point(87, 271)
        Me.nud_Port.Maximum = New Decimal(New Integer() {999999, 0, 0, 0})
        Me.nud_Port.Name = "nud_Port"
        Me.nud_Port.Size = New System.Drawing.Size(128, 22)
        Me.nud_Port.TabIndex = 2
        Me.nud_Port.Value = New Decimal(New Integer() {9000, 0, 0, 0})
        '
        'btn_StartListen
        '
        Me.btn_StartListen.Location = New System.Drawing.Point(12, 166)
        Me.btn_StartListen.Name = "btn_StartListen"
        Me.btn_StartListen.Size = New System.Drawing.Size(203, 71)
        Me.btn_StartListen.TabIndex = 0
        Me.btn_StartListen.Text = "StartListen"
        Me.btn_StartListen.UseVisualStyleBackColor = True
        '
        'MainForm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 12.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(227, 298)
        Me.Controls.Add(Me.tb_IP)
        Me.Controls.Add(Me.nud_Port)
        Me.Controls.Add(Me.lab_Port)
        Me.Controls.Add(Me.lab_IP)
        Me.Controls.Add(Me.btn_StartListen)
        Me.Controls.Add(Me.btn_FFTCalculate)
        Me.Controls.Add(Me.btn_TuneForm)
        Me.Name = "MainForm"
        Me.Text = "Form1"
        CType(Me.nud_Port, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents btn_TuneForm As Button
    Friend WithEvents btn_FFTCalculate As Button
    Friend WithEvents tb_IP As TextBox
    Friend WithEvents lab_IP As Label
    Friend WithEvents lab_Port As Label
    Friend WithEvents nud_Port As NumericUpDown
    Friend WithEvents btn_StartListen As Button
End Class
