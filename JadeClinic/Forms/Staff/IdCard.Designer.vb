<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class IdCard
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
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

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(IdCard))
        Dim CustomizableEdges6 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges7 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges1 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges2 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges3 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges4 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges5 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges8 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges9 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges10 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges11 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges12 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges13 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        pnlIDCard = New Guna.UI2.WinForms.Guna2Panel()
        QrCodePicturebox = New PictureBox()
        Guna2HtmlLabel1 = New Guna.UI2.WinForms.Guna2HtmlLabel()
        picCompanyLogo = New Guna.UI2.WinForms.Guna2PictureBox()
        lblPasskeys = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblPasskeysTitle = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblPin = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblPinTitle = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblUserID = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblPhone = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblPhoneTitle = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblEmail = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblEmailTitle = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblRole = New Guna.UI2.WinForms.Guna2HtmlLabel()
        txtUsername = New Guna.UI2.WinForms.Guna2TextBox()
        picStaffPhoto = New Guna.UI2.WinForms.Guna2CirclePictureBox()
        lblCompanyName = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Guna2GradientPanel1 = New Guna.UI2.WinForms.Guna2GradientPanel()
        btnClose = New Guna.UI2.WinForms.Guna2Button()
        btnPrint = New Guna.UI2.WinForms.Guna2Button()
        pnlIDCard.SuspendLayout()
        CType(QrCodePicturebox, ComponentModel.ISupportInitialize).BeginInit()
        CType(picCompanyLogo, ComponentModel.ISupportInitialize).BeginInit()
        CType(picStaffPhoto, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' pnlIDCard
        ' 
        pnlIDCard.BackColor = Color.Black
        pnlIDCard.BackgroundImage = CType(resources.GetObject("pnlIDCard.BackgroundImage"), Image)
        pnlIDCard.BackgroundImageLayout = ImageLayout.Stretch
        pnlIDCard.BorderColor = Color.FromArgb(CByte(255), CByte(204), CByte(77))
        pnlIDCard.BorderRadius = 15
        pnlIDCard.BorderThickness = 2
        pnlIDCard.Controls.Add(QrCodePicturebox)
        pnlIDCard.Controls.Add(Guna2HtmlLabel1)
        pnlIDCard.Controls.Add(picCompanyLogo)
        pnlIDCard.Controls.Add(lblPasskeys)
        pnlIDCard.Controls.Add(lblPasskeysTitle)
        pnlIDCard.Controls.Add(lblPin)
        pnlIDCard.Controls.Add(lblPinTitle)
        pnlIDCard.Controls.Add(lblUserID)
        pnlIDCard.Controls.Add(lblPhone)
        pnlIDCard.Controls.Add(lblPhoneTitle)
        pnlIDCard.Controls.Add(lblEmail)
        pnlIDCard.Controls.Add(lblEmailTitle)
        pnlIDCard.Controls.Add(lblRole)
        pnlIDCard.Controls.Add(txtUsername)
        pnlIDCard.Controls.Add(picStaffPhoto)
        pnlIDCard.Controls.Add(lblCompanyName)
        pnlIDCard.CustomizableEdges = CustomizableEdges6
        pnlIDCard.FillColor = Color.Transparent
        pnlIDCard.Location = New Point(30, 30)
        pnlIDCard.Name = "pnlIDCard"
        pnlIDCard.ShadowDecoration.CustomizableEdges = CustomizableEdges7
        pnlIDCard.Size = New Size(400, 550)
        pnlIDCard.TabIndex = 0
        ' 
        ' QrCodePicturebox
        ' 
        QrCodePicturebox.BackColor = Color.White
        QrCodePicturebox.BorderStyle = BorderStyle.FixedSingle
        QrCodePicturebox.Location = New Point(134, 414)
        QrCodePicturebox.Name = "QrCodePicturebox"
        QrCodePicturebox.Size = New Size(138, 115)
        QrCodePicturebox.SizeMode = PictureBoxSizeMode.StretchImage
        QrCodePicturebox.TabIndex = 17
        QrCodePicturebox.TabStop = False
        ' 
        ' Guna2HtmlLabel1
        ' 
        Guna2HtmlLabel1.BackColor = Color.Transparent
        Guna2HtmlLabel1.Font = New Font("Poppins Medium", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2HtmlLabel1.ForeColor = Color.White
        Guna2HtmlLabel1.Location = New Point(80, 279)
        Guna2HtmlLabel1.Name = "Guna2HtmlLabel1"
        Guna2HtmlLabel1.Size = New Size(46, 28)
        Guna2HtmlLabel1.TabIndex = 16
        Guna2HtmlLabel1.Text = "ID No:"
        ' 
        ' picCompanyLogo
        ' 
        picCompanyLogo.BackColor = Color.Transparent
        picCompanyLogo.BorderRadius = 5
        picCompanyLogo.CustomizableEdges = CustomizableEdges1
        picCompanyLogo.ImageRotate = 0F
        picCompanyLogo.Location = New Point(159, 12)
        picCompanyLogo.Name = "picCompanyLogo"
        picCompanyLogo.ShadowDecoration.CustomizableEdges = CustomizableEdges2
        picCompanyLogo.Size = New Size(85, 66)
        picCompanyLogo.SizeMode = PictureBoxSizeMode.Zoom
        picCompanyLogo.TabIndex = 15
        picCompanyLogo.TabStop = False
        ' 
        ' lblPasskeys
        ' 
        lblPasskeys.BackColor = Color.Transparent
        lblPasskeys.Font = New Font("Poppins", 8.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblPasskeys.ForeColor = Color.FromArgb(CByte(64), CByte(64), CByte(64))
        lblPasskeys.Location = New Point(-99, 128)
        lblPasskeys.Name = "lblPasskeys"
        lblPasskeys.Size = New Size(182, 25)
        lblPasskeys.TabIndex = 14
        lblPasskeys.Text = "key1,key2,key3,key4,key5,key6"
        lblPasskeys.Visible = False
        ' 
        ' lblPasskeysTitle
        ' 
        lblPasskeysTitle.BackColor = Color.Transparent
        lblPasskeysTitle.Font = New Font("Poppins Medium", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblPasskeysTitle.ForeColor = Color.Black
        lblPasskeysTitle.Location = New Point(196, 275)
        lblPasskeysTitle.Name = "lblPasskeysTitle"
        lblPasskeysTitle.Size = New Size(149, 28)
        lblPasskeysTitle.TabIndex = 13
        lblPasskeysTitle.Text = "Recovery Passkeys:"
        lblPasskeysTitle.Visible = False
        ' 
        ' lblPin
        ' 
        lblPin.BackColor = Color.Transparent
        lblPin.Font = New Font("Poppins", 12.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblPin.ForeColor = Color.FromArgb(CByte(255), CByte(204), CByte(77))
        lblPin.Location = New Point(278, 76)
        lblPin.Name = "lblPin"
        lblPin.Size = New Size(48, 38)
        lblPin.TabIndex = 12
        lblPin.Text = "1234"
        lblPin.Visible = False
        ' 
        ' lblPinTitle
        ' 
        lblPinTitle.BackColor = Color.Transparent
        lblPinTitle.Font = New Font("Poppins Medium", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblPinTitle.ForeColor = Color.Black
        lblPinTitle.Location = New Point(278, 76)
        lblPinTitle.Name = "lblPinTitle"
        lblPinTitle.Size = New Size(30, 28)
        lblPinTitle.TabIndex = 11
        lblPinTitle.Text = "PIN:"
        lblPinTitle.Visible = False
        ' 
        ' lblUserID
        ' 
        lblUserID.BackColor = Color.Transparent
        lblUserID.Font = New Font("Poppins", 10.2F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblUserID.ForeColor = Color.FromArgb(CByte(255), CByte(204), CByte(77))
        lblUserID.Location = New Point(148, 275)
        lblUserID.Name = "lblUserID"
        lblUserID.Size = New Size(20, 32)
        lblUserID.TabIndex = 10
        lblUserID.Text = "01"
        ' 
        ' lblPhone
        ' 
        lblPhone.BackColor = Color.Transparent
        lblPhone.Font = New Font("Poppins", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblPhone.ForeColor = Color.FromArgb(CByte(224), CByte(224), CByte(224))
        lblPhone.Location = New Point(146, 369)
        lblPhone.Name = "lblPhone"
        lblPhone.Size = New Size(87, 28)
        lblPhone.TabIndex = 8
        lblPhone.Text = "+123456789"
        ' 
        ' lblPhoneTitle
        ' 
        lblPhoneTitle.BackColor = Color.Transparent
        lblPhoneTitle.Font = New Font("Poppins Medium", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblPhoneTitle.ForeColor = Color.White
        lblPhoneTitle.Location = New Point(80, 369)
        lblPhoneTitle.Name = "lblPhoneTitle"
        lblPhoneTitle.Size = New Size(54, 28)
        lblPhoneTitle.TabIndex = 7
        lblPhoneTitle.Text = "Phone:"
        ' 
        ' lblEmail
        ' 
        lblEmail.BackColor = Color.Transparent
        lblEmail.Font = New Font("Poppins", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblEmail.ForeColor = Color.FromArgb(CByte(224), CByte(224), CByte(224))
        lblEmail.Location = New Point(146, 322)
        lblEmail.Name = "lblEmail"
        lblEmail.Size = New Size(162, 28)
        lblEmail.TabIndex = 6
        lblEmail.Text = "user@company.com"
        ' 
        ' lblEmailTitle
        ' 
        lblEmailTitle.BackColor = Color.Transparent
        lblEmailTitle.Font = New Font("Poppins", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblEmailTitle.ForeColor = Color.White
        lblEmailTitle.Location = New Point(80, 322)
        lblEmailTitle.Name = "lblEmailTitle"
        lblEmailTitle.Size = New Size(47, 28)
        lblEmailTitle.TabIndex = 5
        lblEmailTitle.Text = "Email:"
        ' 
        ' lblRole
        ' 
        lblRole.BackColor = Color.Transparent
        lblRole.Font = New Font("Poppins", 10.0F, FontStyle.Bold, GraphicsUnit.Point, CByte(0))
        lblRole.ForeColor = Color.FromArgb(CByte(255), CByte(204), CByte(77))
        lblRole.Location = New Point(171, 240)
        lblRole.Name = "lblRole"
        lblRole.Size = New Size(47, 32)
        lblRole.TabIndex = 4
        lblRole.Text = "Staff"
        ' 
        ' txtUsername
        ' 
        txtUsername.BackColor = Color.Transparent
        txtUsername.BackgroundImage = CType(resources.GetObject("txtUsername.BackgroundImage"), Image)
        txtUsername.BackgroundImageLayout = ImageLayout.Stretch
        txtUsername.BorderRadius = 10
        txtUsername.BorderThickness = 0
        txtUsername.CustomizableEdges = CustomizableEdges3
        txtUsername.DefaultText = ""
        txtUsername.DisabledState.BorderColor = Color.Transparent
        txtUsername.DisabledState.FillColor = Color.Transparent
        txtUsername.DisabledState.ForeColor = Color.White
        txtUsername.DisabledState.PlaceholderForeColor = Color.White
        txtUsername.FillColor = Color.Transparent
        txtUsername.FocusedState.BorderColor = Color.Transparent
        txtUsername.Font = New Font("Microsoft Sans Serif", 18.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        txtUsername.ForeColor = Color.White
        txtUsername.HoverState.BorderColor = Color.Transparent
        txtUsername.Location = New Point(14, 182)
        txtUsername.Margin = New Padding(3, 4, 3, 4)
        txtUsername.Name = "txtUsername"
        txtUsername.PlaceholderText = ""
        txtUsername.ReadOnly = True
        txtUsername.SelectedText = ""
        txtUsername.ShadowDecoration.CustomizableEdges = CustomizableEdges4
        txtUsername.Size = New Size(371, 38)
        txtUsername.TabIndex = 2
        txtUsername.TabStop = False
        txtUsername.TextAlign = HorizontalAlignment.Center
        ' 
        ' picStaffPhoto
        ' 
        picStaffPhoto.BackColor = Color.Transparent
        picStaffPhoto.ImageRotate = 0F
        picStaffPhoto.Location = New Point(148, 76)
        picStaffPhoto.Name = "picStaffPhoto"
        picStaffPhoto.ShadowDecoration.CustomizableEdges = CustomizableEdges5
        picStaffPhoto.ShadowDecoration.Mode = Guna.UI2.WinForms.Enums.ShadowMode.Circle
        picStaffPhoto.Size = New Size(100, 100)
        picStaffPhoto.SizeMode = PictureBoxSizeMode.Zoom
        picStaffPhoto.TabIndex = 1
        picStaffPhoto.TabStop = False
        ' 
        ' lblCompanyName
        ' 
        lblCompanyName.BackColor = Color.Transparent
        lblCompanyName.Font = New Font("Poppins", 18.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblCompanyName.ForeColor = Color.FromArgb(CByte(255), CByte(204), CByte(77))
        lblCompanyName.Location = New Point(110, 12)
        lblCompanyName.Name = "lblCompanyName"
        lblCompanyName.Size = New Size(232, 55)
        lblCompanyName.TabIndex = 0
        lblCompanyName.Text = "Employee Card"
        lblCompanyName.Visible = False
        ' 
        ' Guna2GradientPanel1
        ' 
        Guna2GradientPanel1.CustomizableEdges = CustomizableEdges8
        Guna2GradientPanel1.Location = New Point(12, 651)
        Guna2GradientPanel1.Name = "Guna2GradientPanel1"
        Guna2GradientPanel1.ShadowDecoration.CustomizableEdges = CustomizableEdges9
        Guna2GradientPanel1.Size = New Size(193, 42)
        Guna2GradientPanel1.TabIndex = 17
        ' 
        ' btnClose
        ' 
        btnClose.BorderRadius = 10
        btnClose.CustomizableEdges = CustomizableEdges10
        btnClose.DisabledState.BorderColor = Color.DarkGray
        btnClose.DisabledState.CustomBorderColor = Color.DarkGray
        btnClose.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        btnClose.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        btnClose.FillColor = Color.FromArgb(CByte(224), CByte(224), CByte(224))
        btnClose.Font = New Font("Poppins Medium", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        btnClose.ForeColor = Color.Black
        btnClose.Location = New Point(350, 600)
        btnClose.Name = "btnClose"
        btnClose.ShadowDecoration.CustomizableEdges = CustomizableEdges11
        btnClose.Size = New Size(80, 35)
        btnClose.TabIndex = 1
        btnClose.Text = "Close"
        ' 
        ' btnPrint
        ' 
        btnPrint.BorderRadius = 10
        btnPrint.CustomizableEdges = CustomizableEdges12
        btnPrint.DisabledState.BorderColor = Color.DarkGray
        btnPrint.DisabledState.CustomBorderColor = Color.DarkGray
        btnPrint.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        btnPrint.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        btnPrint.FillColor = Color.FromArgb(CByte(255), CByte(204), CByte(77))
        btnPrint.Font = New Font("Poppins Medium", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        btnPrint.ForeColor = Color.Black
        btnPrint.Location = New Point(260, 600)
        btnPrint.Name = "btnPrint"
        btnPrint.ShadowDecoration.CustomizableEdges = CustomizableEdges13
        btnPrint.Size = New Size(80, 35)
        btnPrint.TabIndex = 2
        btnPrint.Text = "Print"
        ' 
        ' StaffIDCard
        ' 
        AutoScaleDimensions = New SizeF(8.0F, 20.0F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        BackgroundImage = CType(resources.GetObject("$this.BackgroundImage"), Image)
        BackgroundImageLayout = ImageLayout.Stretch
        ClientSize = New Size(460, 650)
        Controls.Add(Guna2GradientPanel1)
        Controls.Add(btnPrint)
        Controls.Add(btnClose)
        Controls.Add(pnlIDCard)
        FormBorderStyle = FormBorderStyle.FixedDialog
        MaximizeBox = False
        MinimizeBox = False
        Name = "StaffIDCard"
        StartPosition = FormStartPosition.CenterParent
        Text = "Staff ID Card"
        pnlIDCard.ResumeLayout(False)
        pnlIDCard.PerformLayout()
        CType(QrCodePicturebox, ComponentModel.ISupportInitialize).EndInit()
        CType(picCompanyLogo, ComponentModel.ISupportInitialize).EndInit()
        CType(picStaffPhoto, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents pnlIDCard As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents picCompanyLogo As Guna.UI2.WinForms.Guna2PictureBox
    Friend WithEvents picStaffPhoto As Guna.UI2.WinForms.Guna2CirclePictureBox
    Friend WithEvents lblUsername As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblRole As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblEmailTitle As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblEmail As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblPhoneTitle As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblPhone As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblUserID As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblPinTitle As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblPin As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblPasskeysTitle As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents btnClose As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents btnPrint As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents Guna2HtmlLabel1 As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblCompanyName As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents Guna2GradientPanel1 As Guna.UI2.WinForms.Guna2GradientPanel
    Friend WithEvents lblPasskeys As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents QrCodePicturebox As PictureBox
    Friend WithEvents txtUsername As Guna.UI2.WinForms.Guna2TextBox
End Class