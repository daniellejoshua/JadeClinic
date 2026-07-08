<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class EscForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
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

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim CustomizableEdges7 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges8 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges1 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges2 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(EscForm))
        Dim CustomizableEdges3 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges4 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges5 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges6 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        mainPanel = New Guna.UI2.WinForms.Guna2Panel()
        btnClose = New Guna.UI2.WinForms.Guna2Button()
        lblIcon = New Label()
        lblTitle = New Label()
        lblSubtitle = New Label()
        separatorPanel = New Panel()
        lblDetails = New Label()
        btnExit = New Guna.UI2.WinForms.Guna2Button()
        btnCancel = New Guna.UI2.WinForms.Guna2Button()
        mainPanel.SuspendLayout()
        SuspendLayout()
        ' 
        ' mainPanel
        ' 
        mainPanel.BorderRadius = 18
        mainPanel.Controls.Add(btnClose)
        mainPanel.Controls.Add(lblTitle)
        mainPanel.Controls.Add(lblSubtitle)
        mainPanel.Controls.Add(separatorPanel)
        mainPanel.Controls.Add(lblDetails)
        mainPanel.Controls.Add(btnExit)
        mainPanel.Controls.Add(btnCancel)
        mainPanel.CustomizableEdges = CustomizableEdges7
        mainPanel.FillColor = Color.FromArgb(250, 249, 246)
        mainPanel.Location = New Point(20, 20)
        mainPanel.Name = "mainPanel"
        mainPanel.ShadowDecoration.CustomizableEdges = CustomizableEdges8
        mainPanel.ShadowDecoration.Depth = 5
        mainPanel.ShadowDecoration.Enabled = True
        mainPanel.Size = New Size(420, 520)
        mainPanel.TabIndex = 0
        ' 
        ' btnClose
        ' 
        btnClose.BorderRadius = 10
        btnClose.CustomizableEdges = CustomizableEdges1
        btnClose.DisabledState.BorderColor = Color.DarkGray
        btnClose.DisabledState.CustomBorderColor = Color.DarkGray
        btnClose.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        btnClose.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        btnClose.FillColor = Color.FromArgb(191, 155, 48)
        btnClose.Font = New Font("Poppins Medium", 9.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        btnClose.ForeColor = Color.White
        btnClose.Location = New Point(372, 14)
        btnClose.Name = "btnClose"
        btnClose.ShadowDecoration.CustomizableEdges = CustomizableEdges2
        btnClose.Size = New Size(34, 34)
        btnClose.TabIndex = 0
        btnClose.Text = ChrW(&H2715)
        ' 
        ' lblTitle
        ' 
        lblTitle.Font = New Font("Poppins", 18.0F, FontStyle.Bold)
        lblTitle.ForeColor = Color.FromArgb(42, 42, 42)
        lblTitle.Location = New Point(20, 40)
        lblTitle.Name = "lblTitle"
        lblTitle.Size = New Size(380, 40)
        lblTitle.TabIndex = 1
        lblTitle.Text = "Exit Application"
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' lblSubtitle
        ' 
        lblSubtitle.Font = New Font("Poppins", 10.0F)
        lblSubtitle.ForeColor = Color.FromArgb(59, 59, 59)
        lblSubtitle.Location = New Point(30, 90)
        lblSubtitle.Name = "lblSubtitle"
        lblSubtitle.Size = New Size(360, 24)
        lblSubtitle.TabIndex = 2
        lblSubtitle.Text = "Are you sure you want to close the system?"
        lblSubtitle.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' separatorPanel
        ' 
        separatorPanel.BackColor = Color.FromArgb(220, 220, 220)
        separatorPanel.Location = New Point(40, 130)
        separatorPanel.Name = "separatorPanel"
        separatorPanel.Size = New Size(340, 2)
        separatorPanel.TabIndex = 3
        ' 
        ' lblDetails
        ' 
        lblDetails.Font = New Font("Poppins", 9.5F)
        lblDetails.ForeColor = Color.FromArgb(59, 59, 59)
        lblDetails.Location = New Point(40, 150)
        lblDetails.Name = "lblDetails"
        lblDetails.Size = New Size(340, 200)
        lblDetails.TabIndex = 4
        lblDetails.Text = resources.GetString("lblDetails.Text")
        ' 
        ' btnExit
        ' 
        btnExit.BorderRadius = 8
        btnExit.CustomizableEdges = CustomizableEdges3
        btnExit.FillColor = Color.FromArgb(220, 80, 70)
        btnExit.Font = New Font("Poppins", 10.0F, FontStyle.Bold)
        btnExit.ForeColor = Color.White
        btnExit.Location = New Point(20, 415)
        btnExit.Name = "btnExit"
        btnExit.ShadowDecoration.CustomizableEdges = CustomizableEdges4
        btnExit.Size = New Size(185, 44)
        btnExit.TabIndex = 5
        btnExit.Text = "Exit Application"
        ' 
        ' btnCancel
        ' 
        btnCancel.BorderRadius = 8
        btnCancel.CustomizableEdges = CustomizableEdges5
        btnCancel.FillColor = Color.FromArgb(191, 155, 48)
        btnCancel.Font = New Font("Poppins", 9.0F, FontStyle.Bold)
        btnCancel.ForeColor = Color.White
        btnCancel.Location = New Point(217, 415)
        btnCancel.Name = "btnCancel"
        btnCancel.ShadowDecoration.CustomizableEdges = CustomizableEdges6
        btnCancel.Size = New Size(195, 44)
        btnCancel.TabIndex = 6
        btnCancel.Text = "Stay in Application"
        ' 
        ' EscForm
        ' 
        AutoScaleDimensions = New SizeF(8.0F, 20.0F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.White
        ClientSize = New Size(460, 560)
        Controls.Add(mainPanel)
        FormBorderStyle = FormBorderStyle.None
        KeyPreview = True
        Name = "EscForm"
        StartPosition = FormStartPosition.CenterParent
        Text = "Exit Application"
        mainPanel.ResumeLayout(False)
        ResumeLayout(False)
    End Sub

    Friend WithEvents mainPanel As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents btnClose As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents lblIcon As Label
    Friend WithEvents lblTitle As Label
    Friend WithEvents lblSubtitle As Label
    Friend WithEvents separatorPanel As Panel
    Friend WithEvents lblDetails As Label
    Friend WithEvents btnExit As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents btnCancel As Guna.UI2.WinForms.Guna2Button
End Class
