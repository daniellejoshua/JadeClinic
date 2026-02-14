<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Sys
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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Sys))
        Me.Guna2Panel1 = New Guna.UI2.WinForms.Guna2Panel()
        Me.lblUsername = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Me.Guna2CirclePictureBox1 = New Guna.UI2.WinForms.Guna2CirclePictureBox()
        Me.DashboardPanel = New Guna.UI2.WinForms.Guna2Panel()
        Me.PictureBox9 = New System.Windows.Forms.PictureBox()
        Me.MainContentPanel = New Guna.UI2.WinForms.Guna2Panel()
        Me.HeaderPanel = New Guna.UI2.WinForms.Guna2Panel()
        Me.lblPageTitle = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Me.ContentAreaPanel = New Guna.UI2.WinForms.Guna2Panel()
        Me.btnCompanySettings = New Guna.UI2.WinForms.Guna2Button()
        Me.btnDatabaseBackup = New Guna.UI2.WinForms.Guna2Button()
        Me.btnColorCustomization = New Guna.UI2.WinForms.Guna2Button()
        Me.Guna2Panel1.SuspendLayout()
        CType(Me.Guna2CirclePictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.DashboardPanel.SuspendLayout()
        CType(Me.PictureBox9, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.MainContentPanel.SuspendLayout()
        Me.HeaderPanel.SuspendLayout()
        Me.ContentAreaPanel.SuspendLayout()
        Me.SuspendLayout()
        '
        'Guna2Panel1
        '
        Me.Guna2Panel1.BackColor = System.Drawing.Color.FromArgb(CType(CType(43, Byte), Integer), CType(CType(47, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.Guna2Panel1.Controls.Add(Me.lblUsername)
        Me.Guna2Panel1.Controls.Add(Me.Guna2CirclePictureBox1)
        Me.Guna2Panel1.Dock = System.Windows.Forms.DockStyle.Top
        Me.Guna2Panel1.Location = New System.Drawing.Point(236, 0)
        Me.Guna2Panel1.Name = "Guna2Panel1"
        Me.Guna2Panel1.Size = New System.Drawing.Size(1164, 80)
        Me.Guna2Panel1.TabIndex = 0
        '
        'lblUsername
        '
        Me.lblUsername.BackColor = System.Drawing.Color.Transparent
        Me.lblUsername.Font = New System.Drawing.Font("Poppins", 10.0F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
        Me.lblUsername.ForeColor = System.Drawing.Color.White
        Me.lblUsername.Location = New System.Drawing.Point(1020, 30)
        Me.lblUsername.Name = "lblUsername"
        Me.lblUsername.Size = New System.Drawing.Size(77, 32)
        Me.lblUsername.TabIndex = 1
        Me.lblUsername.Text = "Username"
        '
        'Guna2CirclePictureBox1
        '
        Me.Guna2CirclePictureBox1.ImageRotate = 0!
        Me.Guna2CirclePictureBox1.Location = New System.Drawing.Point(1110, 20)
        Me.Guna2CirclePictureBox1.Name = "Guna2CirclePictureBox1"
        Me.Guna2CirclePictureBox1.ShadowDecoration.Mode = Guna.UI2.WinForms.Enums.ShadowMode.Circle
        Me.Guna2CirclePictureBox1.Size = New System.Drawing.Size(50, 50)
        Me.Guna2CirclePictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.Guna2CirclePictureBox1.TabIndex = 0
        Me.Guna2CirclePictureBox1.TabStop = False
        '
        'DashboardPanel
        '
        Me.DashboardPanel.BackColor = System.Drawing.Color.White
        Me.DashboardPanel.Controls.Add(Me.PictureBox9)
        Me.DashboardPanel.Dock = System.Windows.Forms.DockStyle.Left
        Me.DashboardPanel.Location = New System.Drawing.Point(0, 0)
        Me.DashboardPanel.Name = "DashboardPanel"
        Me.DashboardPanel.Size = New System.Drawing.Size(236, 885)
        Me.DashboardPanel.TabIndex = 1
        '
        'PictureBox9
        '
        Me.PictureBox9.Image = CType(resources.GetObject("PictureBox9.Image"), System.Drawing.Image)
        Me.PictureBox9.Location = New System.Drawing.Point(65, 25)
        Me.PictureBox9.Name = "PictureBox9"
        Me.PictureBox9.Size = New System.Drawing.Size(100, 70)
        Me.PictureBox9.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.PictureBox9.TabIndex = 0
        Me.PictureBox9.TabStop = False
        '
        'MainContentPanel
        '
        Me.MainContentPanel.BackColor = System.Drawing.Color.FromArgb(CType(CType(26, Byte), Integer), CType(CType(29, Byte), Integer), CType(CType(31, Byte), Integer))
        Me.MainContentPanel.Controls.Add(Me.ContentAreaPanel)
        Me.MainContentPanel.Controls.Add(Me.HeaderPanel)
        Me.MainContentPanel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.MainContentPanel.Location = New System.Drawing.Point(236, 80)
        Me.MainContentPanel.Name = "MainContentPanel"
        Me.MainContentPanel.Padding = New System.Windows.Forms.Padding(20)
        Me.MainContentPanel.Size = New System.Drawing.Size(1164, 805)
        Me.MainContentPanel.TabIndex = 2
        '
        'HeaderPanel
        '
        Me.HeaderPanel.BackColor = System.Drawing.Color.Transparent
        Me.HeaderPanel.Controls.Add(Me.lblPageTitle)
        Me.HeaderPanel.Dock = System.Windows.Forms.DockStyle.Top
        Me.HeaderPanel.Location = New System.Drawing.Point(20, 20)
        Me.HeaderPanel.Name = "HeaderPanel"
        Me.HeaderPanel.Size = New System.Drawing.Size(1124, 60)
        Me.HeaderPanel.TabIndex = 0
        '
        'lblPageTitle
        '
        Me.lblPageTitle.BackColor = System.Drawing.Color.Transparent
        Me.lblPageTitle.Font = New System.Drawing.Font("Poppins", 24.0F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point)
        Me.lblPageTitle.ForeColor = System.Drawing.Color.FromArgb(CType(CType(254, Byte), Integer), CType(CType(191, Byte), Integer), CType(CType(16, Byte), Integer))
        Me.lblPageTitle.Location = New System.Drawing.Point(0, 0)
        Me.lblPageTitle.Name = "lblPageTitle"
        Me.lblPageTitle.Size = New System.Drawing.Size(307, 72)
        Me.lblPageTitle.TabIndex = 0
        Me.lblPageTitle.Text = "System Settings"
        '
        'ContentAreaPanel
        '
        Me.ContentAreaPanel.BackColor = System.Drawing.Color.Transparent
        Me.ContentAreaPanel.Controls.Add(Me.btnColorCustomization)
        Me.ContentAreaPanel.Controls.Add(Me.btnDatabaseBackup)
        Me.ContentAreaPanel.Controls.Add(Me.btnCompanySettings)
        Me.ContentAreaPanel.Dock = System.Windows.Forms.DockStyle.Fill
        Me.ContentAreaPanel.Location = New System.Drawing.Point(20, 80)
        Me.ContentAreaPanel.Name = "ContentAreaPanel"
        Me.ContentAreaPanel.Size = New System.Drawing.Size(1124, 705)
        Me.ContentAreaPanel.TabIndex = 1
        '
        'btnCompanySettings
        '
        Me.btnCompanySettings.BorderRadius = 15
        Me.btnCompanySettings.DisabledState.BorderColor = System.Drawing.Color.DarkGray
        Me.btnCompanySettings.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray
        Me.btnCompanySettings.DisabledState.FillColor = System.Drawing.Color.FromArgb(CType(CType(169, Byte), Integer), CType(CType(169, Byte), Integer), CType(CType(169, Byte), Integer))
        Me.btnCompanySettings.DisabledState.ForeColor = System.Drawing.Color.FromArgb(CType(CType(141, Byte), Integer), CType(CType(141, Byte), Integer), CType(CType(141, Byte), Integer))
        Me.btnCompanySettings.FillColor = System.Drawing.Color.FromArgb(CType(CType(43, Byte), Integer), CType(CType(47, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.btnCompanySettings.Font = New System.Drawing.Font("Poppins", 14.0F, System.Drawing.FontStyle.Bold)
        Me.btnCompanySettings.ForeColor = System.Drawing.Color.White
        Me.btnCompanySettings.Image = CType(resources.GetObject("btnCompanySettings.Image"), System.Drawing.Image)
        Me.btnCompanySettings.ImageAlign = System.Windows.Forms.HorizontalAlignment.Left
        Me.btnCompanySettings.ImageSize = New System.Drawing.Size(40, 40)
        Me.btnCompanySettings.Location = New System.Drawing.Point(50, 50)
        Me.btnCompanySettings.Name = "btnCompanySettings"
        Me.btnCompanySettings.Size = New System.Drawing.Size(320, 120)
        Me.btnCompanySettings.TabIndex = 0
        Me.btnCompanySettings.Text = "🏢 Company Settings" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Configure business information"
        Me.btnCompanySettings.TextAlign = System.Windows.Forms.HorizontalAlignment.Left
        '
        'btnDatabaseBackup
        '
        Me.btnDatabaseBackup.BorderRadius = 15
        Me.btnDatabaseBackup.DisabledState.BorderColor = System.Drawing.Color.DarkGray
        Me.btnDatabaseBackup.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray
        Me.btnDatabaseBackup.DisabledState.FillColor = System.Drawing.Color.FromArgb(CType(CType(169, Byte), Integer), CType(CType(169, Byte), Integer), CType(CType(169, Byte), Integer))
        Me.btnDatabaseBackup.DisabledState.ForeColor = System.Drawing.Color.FromArgb(CType(CType(141, Byte), Integer), CType(CType(141, Byte), Integer), CType(CType(141, Byte), Integer))
        Me.btnDatabaseBackup.FillColor = System.Drawing.Color.FromArgb(CType(CType(43, Byte), Integer), CType(CType(47, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.btnDatabaseBackup.Font = New System.Drawing.Font("Poppins", 14.0F, System.Drawing.FontStyle.Bold)
        Me.btnDatabaseBackup.ForeColor = System.Drawing.Color.White
        Me.btnDatabaseBackup.Image = CType(resources.GetObject("btnDatabaseBackup.Image"), System.Drawing.Image)
        Me.btnDatabaseBackup.ImageAlign = System.Windows.Forms.HorizontalAlignment.Left
        Me.btnDatabaseBackup.ImageSize = New System.Drawing.Size(40, 40)
        Me.btnDatabaseBackup.Location = New System.Drawing.Point(400, 50)
        Me.btnDatabaseBackup.Name = "btnDatabaseBackup"
        Me.btnDatabaseBackup.Size = New System.Drawing.Size(320, 120)
        Me.btnDatabaseBackup.TabIndex = 1
        Me.btnDatabaseBackup.Text = "💾 Database Backup" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Backup & restore database"
        Me.btnDatabaseBackup.TextAlign = System.Windows.Forms.HorizontalAlignment.Left
        '
        'btnColorCustomization
        '
        Me.btnColorCustomization.BorderRadius = 15
        Me.btnColorCustomization.DisabledState.BorderColor = System.Drawing.Color.DarkGray
        Me.btnColorCustomization.DisabledState.CustomBorderColor = System.Drawing.Color.DarkGray
        Me.btnColorCustomization.DisabledState.FillColor = System.Drawing.Color.FromArgb(CType(CType(169, Byte), Integer), CType(CType(169, Byte), Integer), CType(CType(169, Byte), Integer))
        Me.btnColorCustomization.DisabledState.ForeColor = System.Drawing.Color.FromArgb(CType(CType(141, Byte), Integer), CType(CType(141, Byte), Integer), CType(CType(141, Byte), Integer))
        Me.btnColorCustomization.FillColor = System.Drawing.Color.FromArgb(CType(CType(43, Byte), Integer), CType(CType(47, Byte), Integer), CType(CType(50, Byte), Integer))
        Me.btnColorCustomization.Font = New System.Drawing.Font("Poppins", 14.0F, System.Drawing.FontStyle.Bold)
        Me.btnColorCustomization.ForeColor = System.Drawing.Color.White
        Me.btnColorCustomization.Image = CType(resources.GetObject("btnColorCustomization.Image"), System.Drawing.Image)
        Me.btnColorCustomization.ImageAlign = System.Windows.Forms.HorizontalAlignment.Left
        Me.btnColorCustomization.ImageSize = New System.Drawing.Size(40, 40)
        Me.btnColorCustomization.Location = New System.Drawing.Point(750, 50)
        Me.btnColorCustomization.Name = "btnColorCustomization"
        Me.btnColorCustomization.Size = New System.Drawing.Size(320, 120)
        Me.btnColorCustomization.TabIndex = 2
        Me.btnColorCustomization.Text = "🎨 Color Customization" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Customize app colors & theme"
        Me.btnColorCustomization.TextAlign = System.Windows.Forms.HorizontalAlignment.Left
        '
        'Sys
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(8.0F, 20.0F)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(26, Byte), Integer), CType(CType(29, Byte), Integer), CType(CType(31, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(1400, 885)
        Me.Controls.Add(Me.MainContentPanel)
        Me.Controls.Add(Me.Guna2Panel1)
        Me.Controls.Add(Me.DashboardPanel)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "Sys"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "System Settings - Jade Clinic"
        Me.Guna2Panel1.ResumeLayout(False)
        Me.Guna2Panel1.PerformLayout()
        CType(Me.Guna2CirclePictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.DashboardPanel.ResumeLayout(False)
        CType(Me.PictureBox9, System.ComponentModel.ISupportInitialize).EndInit()
        Me.MainContentPanel.ResumeLayout(False)
        Me.HeaderPanel.ResumeLayout(False)
        Me.HeaderPanel.PerformLayout()
        Me.ContentAreaPanel.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents Guna2Panel1 As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents lblUsername As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents Guna2CirclePictureBox1 As Guna.UI2.WinForms.Guna2CirclePictureBox
    Friend WithEvents DashboardPanel As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents PictureBox9 As PictureBox
    Friend WithEvents MainContentPanel As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents HeaderPanel As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents lblPageTitle As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents ContentAreaPanel As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents btnCompanySettings As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents btnDatabaseBackup As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents btnColorCustomization As Guna.UI2.WinForms.Guna2Button
End Class
