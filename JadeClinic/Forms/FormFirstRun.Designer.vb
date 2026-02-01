<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormFirstRun
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Dim CustomizableEdges9 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges10 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges11 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges12 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges13 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges14 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges15 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges16 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        rbLocal = New Guna.UI2.WinForms.Guna2RadioButton()
        rbNetwork = New Guna.UI2.WinForms.Guna2RadioButton()
        txtServer = New Guna.UI2.WinForms.Guna2TextBox()
        btnTest = New Guna.UI2.WinForms.Guna2Button()
        btnSave = New Guna.UI2.WinForms.Guna2Button()
        lblStatus = New Guna.UI2.WinForms.Guna2HtmlLabel()
        btnCancel = New Guna.UI2.WinForms.Guna2Button()
        lblComputerName = New Guna.UI2.WinForms.Guna2HtmlLabel()
        SuspendLayout()
        ' 
        ' rbLocal
        ' 
        rbLocal.AutoSize = True
        rbLocal.CheckedState.BorderColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        rbLocal.CheckedState.BorderThickness = 0
        rbLocal.CheckedState.FillColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        rbLocal.CheckedState.InnerColor = Color.White
        rbLocal.CheckedState.InnerOffset = -4
        rbLocal.Location = New Point(204, 60)
        rbLocal.Name = "rbLocal"
        rbLocal.Size = New Size(65, 24)
        rbLocal.TabIndex = 0
        rbLocal.Text = "Local"
        rbLocal.UncheckedState.BorderColor = Color.FromArgb(CByte(125), CByte(137), CByte(149))
        rbLocal.UncheckedState.BorderThickness = 2
        rbLocal.UncheckedState.FillColor = Color.Transparent
        rbLocal.UncheckedState.InnerColor = Color.Transparent
        ' 
        ' rbNetwork
        ' 
        rbNetwork.AutoSize = True
        rbNetwork.CheckedState.BorderColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        rbNetwork.CheckedState.BorderThickness = 0
        rbNetwork.CheckedState.FillColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        rbNetwork.CheckedState.InnerColor = Color.White
        rbNetwork.CheckedState.InnerOffset = -4
        rbNetwork.Location = New Point(392, 60)
        rbNetwork.Name = "rbNetwork"
        rbNetwork.Size = New Size(86, 24)
        rbNetwork.TabIndex = 1
        rbNetwork.Text = "Network"
        rbNetwork.UncheckedState.BorderColor = Color.FromArgb(CByte(125), CByte(137), CByte(149))
        rbNetwork.UncheckedState.BorderThickness = 2
        rbNetwork.UncheckedState.FillColor = Color.Transparent
        rbNetwork.UncheckedState.InnerColor = Color.Transparent
        ' 
        ' txtServer
        ' 
        txtServer.CustomizableEdges = CustomizableEdges9
        txtServer.DefaultText = ""
        txtServer.DisabledState.BorderColor = Color.FromArgb(CByte(208), CByte(208), CByte(208))
        txtServer.DisabledState.FillColor = Color.FromArgb(CByte(226), CByte(226), CByte(226))
        txtServer.DisabledState.ForeColor = Color.FromArgb(CByte(138), CByte(138), CByte(138))
        txtServer.DisabledState.PlaceholderForeColor = Color.FromArgb(CByte(138), CByte(138), CByte(138))
        txtServer.FocusedState.BorderColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        txtServer.Font = New Font("Segoe UI", 9F)
        txtServer.HoverState.BorderColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        txtServer.Location = New Point(204, 120)
        txtServer.Margin = New Padding(3, 4, 3, 4)
        txtServer.Name = "txtServer"
        txtServer.PlaceholderText = ""
        txtServer.SelectedText = ""
        txtServer.ShadowDecoration.CustomizableEdges = CustomizableEdges10
        txtServer.Size = New Size(286, 60)
        txtServer.TabIndex = 2
        ' 
        ' btnTest
        ' 
        btnTest.CustomizableEdges = CustomizableEdges11
        btnTest.DisabledState.BorderColor = Color.DarkGray
        btnTest.DisabledState.CustomBorderColor = Color.DarkGray
        btnTest.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        btnTest.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        btnTest.Font = New Font("Segoe UI", 9F)
        btnTest.ForeColor = Color.White
        btnTest.Location = New Point(416, 254)
        btnTest.Name = "btnTest"
        btnTest.ShadowDecoration.CustomizableEdges = CustomizableEdges12
        btnTest.Size = New Size(225, 56)
        btnTest.TabIndex = 3
        btnTest.Text = "test"
        ' 
        ' btnSave
        ' 
        btnSave.CustomizableEdges = CustomizableEdges13
        btnSave.DisabledState.BorderColor = Color.DarkGray
        btnSave.DisabledState.CustomBorderColor = Color.DarkGray
        btnSave.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        btnSave.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        btnSave.Font = New Font("Segoe UI", 9F)
        btnSave.ForeColor = Color.White
        btnSave.Location = New Point(35, 254)
        btnSave.Name = "btnSave"
        btnSave.ShadowDecoration.CustomizableEdges = CustomizableEdges14
        btnSave.Size = New Size(225, 56)
        btnSave.TabIndex = 4
        btnSave.Text = "Save"
        ' 
        ' lblStatus
        ' 
        lblStatus.BackColor = Color.Transparent
        lblStatus.Location = New Point(300, 331)
        lblStatus.Name = "lblStatus"
        lblStatus.Size = New Size(122, 22)
        lblStatus.TabIndex = 5
        lblStatus.Text = "Guna2HtmlLabel1"
        ' 
        ' btnCancel
        ' 
        btnCancel.CustomizableEdges = CustomizableEdges15
        btnCancel.DisabledState.BorderColor = Color.DarkGray
        btnCancel.DisabledState.CustomBorderColor = Color.DarkGray
        btnCancel.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        btnCancel.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        btnCancel.Font = New Font("Segoe UI", 9F)
        btnCancel.ForeColor = Color.White
        btnCancel.Location = New Point(563, 171)
        btnCancel.Name = "btnCancel"
        btnCancel.ShadowDecoration.CustomizableEdges = CustomizableEdges16
        btnCancel.Size = New Size(225, 56)
        btnCancel.TabIndex = 6
        btnCancel.Text = "cancel"
        ' 
        ' lblComputerName
        ' 
        lblComputerName.BackColor = Color.Transparent
        lblComputerName.Location = New Point(339, 214)
        lblComputerName.Name = "lblComputerName"
        lblComputerName.Size = New Size(122, 22)
        lblComputerName.TabIndex = 7
        lblComputerName.Text = "Guna2HtmlLabel1"
        ' 
        ' FormFirstRun
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(800, 450)
        Controls.Add(lblComputerName)
        Controls.Add(btnCancel)
        Controls.Add(lblStatus)
        Controls.Add(btnSave)
        Controls.Add(btnTest)
        Controls.Add(txtServer)
        Controls.Add(rbNetwork)
        Controls.Add(rbLocal)
        Name = "FormFirstRun"
        Text = "FormFirstRun"
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents rbLocal As Guna.UI2.WinForms.Guna2RadioButton
    Friend WithEvents rbNetwork As Guna.UI2.WinForms.Guna2RadioButton
    Friend WithEvents txtServer As Guna.UI2.WinForms.Guna2TextBox
    Friend WithEvents btnTest As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents btnSave As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents lblStatus As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents btnCancel As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents lblComputerName As Guna.UI2.WinForms.Guna2HtmlLabel
End Class
