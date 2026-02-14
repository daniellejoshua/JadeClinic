Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports Guna.UI2.WinForms
Imports Microsoft.Data.SqlClient
Imports System.IO

Public Class Sys
    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    ' Profile dropdown panel
    Private profileDropdownPanel As Panel = Nothing
    Private isProfileDropdownVisible As Boolean = False

    ' Dental Clinic Color Palette Constants
    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)      ' #FECF10 - Primary brand color
    Private ReadOnly RichOlive As Color = Color.FromArgb(190, 154, 48)         ' #BE9A30 - Secondary accent
    Private ReadOnly DeepCharcoal As Color = Color.FromArgb(26, 29, 31)        ' #1A1D1F - Primary dark
    Private ReadOnly DarkSlate As Color = Color.FromArgb(43, 47, 50)           ' #2B2F32 - Secondary dark
    Private ReadOnly Graphite As Color = Color.FromArgb(61, 65, 69)            ' #3D4145 - Card background
    Private ReadOnly SteelGray As Color = Color.FromArgb(74, 79, 84)           ' #4A4F54 - Interactive elements
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)        ' #FFFFFF - Text on dark
    Private ReadOnly LightSilver As Color = Color.FromArgb(225, 229, 233)      ' #E1E5E9 - Secondary text
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)       ' #10D862 - Success states
    Private ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)           ' #FF4757 - Error/Alert states

    Private Sub Sys_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Make form non-resizable
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Create navigation menu
        CreateNavigationMenu()

        ' Validate user session
        If Not ValidateUserSession() Then
            Return
        End If

        ' Initialize profile section
        InitializeProfileSection()

        ' Update form title to show logged-in user
        Me.Text = $"System Settings - {frmLoginvb.LoggedInUsername}"

        ' Start idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)

        ' Initialize UI
        InitializeButtons()
    End Sub

    Private Sub InitializeButtons()
        ' Company Settings button
        btnCompanySettings.BorderRadius = 15
        btnCompanySettings.ShadowDecoration.Enabled = True
        btnCompanySettings.ShadowDecoration.Depth = 8
        btnCompanySettings.FillColor = DarkSlate

        AddHandler btnCompanySettings.MouseEnter, Sub()
                                                      btnCompanySettings.FillColor = SteelGray
                                                      btnCompanySettings.BorderThickness = 2
                                                      btnCompanySettings.BorderColor = GoldenYellow
                                                  End Sub
        AddHandler btnCompanySettings.MouseLeave, Sub()
                                                      btnCompanySettings.FillColor = DarkSlate
                                                      btnCompanySettings.BorderThickness = 0
                                                  End Sub

        ' Database Backup button
        btnDatabaseBackup.BorderRadius = 15
        btnDatabaseBackup.ShadowDecoration.Enabled = True
        btnDatabaseBackup.ShadowDecoration.Depth = 8
        btnDatabaseBackup.FillColor = DarkSlate

        AddHandler btnDatabaseBackup.MouseEnter, Sub()
                                                     btnDatabaseBackup.FillColor = SteelGray
                                                     btnDatabaseBackup.BorderThickness = 2
                                                     btnDatabaseBackup.BorderColor = GoldenYellow
                                                 End Sub
        AddHandler btnDatabaseBackup.MouseLeave, Sub()
                                                     btnDatabaseBackup.FillColor = DarkSlate
                                                     btnDatabaseBackup.BorderThickness = 0
                                                 End Sub

        ' Color Customization button
        btnColorCustomization.BorderRadius = 15
        btnColorCustomization.ShadowDecoration.Enabled = True
        btnColorCustomization.ShadowDecoration.Depth = 8
        btnColorCustomization.FillColor = DarkSlate

        AddHandler btnColorCustomization.MouseEnter, Sub()
                                                         btnColorCustomization.FillColor = SteelGray
                                                         btnColorCustomization.BorderThickness = 2
                                                         btnColorCustomization.BorderColor = GoldenYellow
                                                     End Sub
        AddHandler btnColorCustomization.MouseLeave, Sub()
                                                         btnColorCustomization.FillColor = DarkSlate
                                                         btnColorCustomization.BorderThickness = 0
                                                     End Sub
    End Sub

    ' Create navigation menu
    Private Sub CreateNavigationMenu()
        Try
            ' Clear existing controls except PictureBox9 (logo)
            For i = DashboardPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = DashboardPanel.Controls(i)
                If TypeOf control IsNot PictureBox Then
                    DashboardPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            ' Set Navigation Panel Background to White
            DashboardPanel.FillColor = Color.White

            ' Calculate available space
            Dim availableWidth As Integer = DashboardPanel.Width - 40
            Dim availableHeight As Integer = DashboardPanel.Height - 160

            ' Logo area (keep existing PictureBox9)
            PictureBox9.BringToFront()

            ' Add title label
            Dim titleLabel As New Label()
            titleLabel.Text = "JADE CLINIC"
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = GoldenYellow
            titleLabel.BackColor = Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New Size(availableWidth, 30)
            titleLabel.Location = New Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(titleLabel)

            ' Subtitle
            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Poppins", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = Color.FromArgb(100, 100, 100)
            subtitleLabel.BackColor = Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New Size(availableWidth, 25)
            subtitleLabel.Location = New Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(subtitleLabel)

            ' Navigation section separator
            Dim separator1 As New Panel()
            separator1.BackColor = Color.FromArgb(220, 220, 220)
            separator1.Size = New Size(availableWidth - 20, 2)
            separator1.Location = New Point(30, 190)
            DashboardPanel.Controls.Add(separator1)

            ' Navigation section label
            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Poppins", 10, FontStyle.Bold)
            navLabel.ForeColor = Color.FromArgb(80, 80, 80)
            navLabel.BackColor = Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New Size(availableWidth, 25)
            navLabel.Location = New Point(20, 205)
            navLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(navLabel)

            ' Calculate button positioning
            Dim startY As Integer = 250
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonWidth As Integer = availableWidth - 5
            Dim buttonIndex As Integer = 0

            ' Get current user role
            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "Staff").ToUpper()

            ' Dashboard Button
            Dim navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
            buttonIndex += 1

            ' POS/Sales Button
            Dim navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navPOSBtn.Click, AddressOf NavSales_Click
            buttonIndex += 1

            ' Manager and Admin only buttons
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                ' Inventory Button
                Dim navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
                buttonIndex += 1

                ' Sales Records Button
                Dim navSalesRecordsBtn = CreateLargeNavButton("📊 Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navSalesRecordsBtn.Click, AddressOf NavSalesRecords_Click
                buttonIndex += 1

                ' Staff Management Button
                Dim navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navStaffBtn.Click, AddressOf NavStaff_Click
                buttonIndex += 1

                ' Inventory Logs Button
                Dim navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
                buttonIndex += 1
            End If

            ' Admin only buttons
            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                ' Audit Logs Button
                Dim navAuditLogBtn = CreateLargeNavButton("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navAuditLogBtn.Click, AddressOf NavAuditLog_Click
                buttonIndex += 1

                ' System Settings Button (ACTIVE)
                Dim systemSettingsBtn = CreateLargeNavButton("⚙️ System", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
                buttonIndex += 1
            End If

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
        End Try
    End Sub

    Private Function CreateLargeNavButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer) As Guna.UI2.WinForms.Guna2Button
        Dim btn As New Guna.UI2.WinForms.Guna2Button()

        btn.Text = text
        btn.Size = New Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left

        btn.FillColor = If(isActive, GoldenYellow, Color.Transparent)
        btn.ForeColor = If(isActive, DeepCharcoal, Color.FromArgb(50, 50, 50))
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, Color.Transparent, Color.FromArgb(200, 200, 200))
        btn.BackColor = Color.Transparent
        btn.Cursor = Cursors.Hand

        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = DeepCharcoal
        btn.ShadowDecoration.Depth = 5
        btn.ShadowDecoration.Shadow = New Padding(0, 2, 5, 5)

        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = Color.FromArgb(240, 240, 240)
                                           btn.BorderColor = RichOlive
                                           btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                       End If
                                   End Sub

        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = Color.Transparent
                                           btn.BorderColor = Color.FromArgb(200, 200, 200)
                                           btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        DashboardPanel.Controls.Add(btn)
        Return btn
    End Function

    ' Helper method to validate user session
    Private Function ValidateUserSession() As Boolean
        If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            MessageBox.Show("User session expired. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            frmLoginvb.Show()
            Me.Hide()
            Return False
        End If
        Return True
    End Function

    Private Sub InitializeProfileSection()
        Try
            lblUsername.Text = frmLoginvb.LoggedInUsername
            lblUsername.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
            lblUsername.ForeColor = PureWhite

            LoadUserProfilePicture()

            AddHandler Guna2CirclePictureBox1.Click, AddressOf ProfilePicture_Click
            AddHandler lblUsername.Click, AddressOf ProfilePicture_Click

            AddHandler Guna2CirclePictureBox1.MouseEnter, Sub()
                                                              Guna2CirclePictureBox1.Cursor = Cursors.Hand
                                                          End Sub
            AddHandler lblUsername.MouseEnter, Sub()
                                                   lblUsername.Cursor = Cursors.Hand
                                               End Sub

        Catch ex As Exception
            lblUsername.Text = frmLoginvb.LoggedInUsername
            Guna2CirclePictureBox1.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
        End Try
    End Sub

    Private Sub LoadUserProfilePicture()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Dim query As String = "SELECT Photo FROM Users WHERE Username = @Username"
                Dim parameters As SqlParameter() = {
                    New SqlParameter("@Username", frmLoginvb.LoggedInUsername)
                }

                Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                    If reader.Read() Then
                        Guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.Zoom
                        Guna2CirclePictureBox1.BorderStyle = BorderStyle.None

                        If Not IsDBNull(reader("Photo")) Then
                            Dim photoBytes As Byte() = CType(reader("Photo"), Byte())
                            Using ms As New MemoryStream(photoBytes)
                                Dim loadedImage As Image = Image.FromStream(ms)
                                Guna2CirclePictureBox1.Image = New Bitmap(loadedImage)
                                loadedImage.Dispose()
                            End Using
                        Else
                            Guna2CirclePictureBox1.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
                        End If
                    End If
                End Using
            End If
        Catch ex As Exception
            Guna2CirclePictureBox1.Image = CreateDefaultProfileAvatar(If(frmLoginvb.LoggedInUsername, "User"))
        End Try
    End Sub

    Private Function CreateDefaultProfileAvatar(username As String) As Image
        Dim bitmap As New Bitmap(50, 50)
        Using g As Graphics = Graphics.FromImage(bitmap)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            Dim colors() As Color = {
                Color.FromArgb(255, 107, 107),
                Color.FromArgb(78, 205, 196),
                Color.FromArgb(85, 98, 112),
                Color.FromArgb(129, 236, 236),
                Color.FromArgb(116, 185, 255)
            }
            Dim colorIndex As Integer = Math.Abs(username.GetHashCode()) Mod colors.Length
            g.FillEllipse(New SolidBrush(colors(colorIndex)), 0, 0, 50, 50)

            Dim initials As String = ""
            If username.Length > 0 Then
                initials = username.Substring(0, 1).ToUpper()
                If username.Length > 1 Then
                    For i As Integer = 1 To username.Length - 1
                        If Char.IsUpper(username(i)) OrElse username(i) = " "c Then
                            If username(i) <> " "c Then
                                initials += username(i).ToString().ToUpper()
                                Exit For
                            End If
                        End If
                    Next
                End If
            End If

            Using font As New Font("Poppins", 14, FontStyle.Bold)
                Dim textSize = g.MeasureString(initials, font)
                g.DrawString(initials, font, New SolidBrush(PureWhite),
                    (50 - textSize.Width) / 2, (50 - textSize.Height) / 2)
            End Using
        End Using
        Return bitmap
    End Function

    Private Sub ProfilePicture_Click(sender As Object, e As EventArgs)
        ToggleProfileDropdown()
    End Sub

    Private Sub ToggleProfileDropdown()
        If isProfileDropdownVisible Then
            HideProfileDropdown()
        Else
            ShowProfileDropdown()
        End If
    End Sub

    Private Sub ShowProfileDropdown()
        If profileDropdownPanel IsNot Nothing Then
            HideProfileDropdown()
        End If

        profileDropdownPanel = New Panel()
        profileDropdownPanel.Size = New Size(200, 100)
        profileDropdownPanel.BackColor = DarkSlate
        profileDropdownPanel.BorderStyle = BorderStyle.FixedSingle

        Dim profileLocation = Guna2CirclePictureBox1.Location
        profileDropdownPanel.Location = New Point(profileLocation.X - 90, profileLocation.Y + Guna2CirclePictureBox1.Height + 5)

        Dim btnProfileSettings As New Label()
        btnProfileSettings.Text = "⚙️ Profile Settings"
        btnProfileSettings.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnProfileSettings.ForeColor = PureWhite
        btnProfileSettings.BackColor = Color.Transparent
        btnProfileSettings.Size = New Size(190, 40)
        btnProfileSettings.Location = New Point(5, 5)
        btnProfileSettings.TextAlign = ContentAlignment.MiddleLeft
        btnProfileSettings.Cursor = Cursors.Hand

        AddHandler btnProfileSettings.MouseEnter, Sub() btnProfileSettings.BackColor = Graphite
        AddHandler btnProfileSettings.MouseLeave, Sub() btnProfileSettings.BackColor = Color.Transparent
        AddHandler btnProfileSettings.Click, Sub()
                                                 HideProfileDropdown()
                                                 NavigateToProfileSettings()
                                             End Sub

        Dim btnLogOut As New Label()
        btnLogOut.Text = "🚪 Log Out"
        btnLogOut.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnLogOut.ForeColor = PureWhite
        btnLogOut.BackColor = Color.Transparent
        btnLogOut.Size = New Size(190, 40)
        btnLogOut.Location = New Point(5, 50)
        btnLogOut.TextAlign = ContentAlignment.MiddleLeft
        btnLogOut.Cursor = Cursors.Hand

        AddHandler btnLogOut.MouseEnter, Sub() btnLogOut.BackColor = Graphite
        AddHandler btnLogOut.MouseLeave, Sub() btnLogOut.BackColor = Color.Transparent
        AddHandler btnLogOut.Click, Sub()
                                        Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                                        If result = DialogResult.Yes Then
                                            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                                                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Log Out", "User logged out of the application.")
                                            End If
                                            frmLoginvb.LogoutUser()
                                            isNavigating = True
                                            Me.Hide()
                                            Dim loginForm As New frmLoginvb()
                                            loginForm.Show()
                                        End If
                                    End Sub

        profileDropdownPanel.Controls.Add(btnProfileSettings)
        profileDropdownPanel.Controls.Add(btnLogOut)

        Me.Controls.Add(profileDropdownPanel)
        profileDropdownPanel.BringToFront()

        AddHandler Me.Click, AddressOf Form_Click
        isProfileDropdownVisible = True
    End Sub

    Private Sub HideProfileDropdown()
        If profileDropdownPanel IsNot Nothing Then
            Me.Controls.Remove(profileDropdownPanel)
            profileDropdownPanel.Dispose()
            profileDropdownPanel = Nothing
        End If
        isProfileDropdownVisible = False
        RemoveHandler Me.Click, AddressOf Form_Click
    End Sub

    Private Sub Form_Click(sender As Object, e As EventArgs)
        HideProfileDropdown()
    End Sub

    Private Sub NavigateToProfileSettings()
        If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from System to ProfileSettings")
        End If
        isNavigating = True
        MessageBox.Show("Profile Settings will be implemented.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' Button click handlers
    Private Sub btnCompanySettings_Click(sender As Object, e As EventArgs) Handles btnCompanySettings.Click
        Try
            Dim companySettingsForm As New CompanySettings()
            companySettingsForm.ShowDialog()
        Catch ex As Exception
            MessageBox.Show($"Error opening Company Settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnDatabaseBackup_Click(sender As Object, e As EventArgs) Handles btnDatabaseBackup.Click
        Try
            ShowDatabaseBackupDialog()
        Catch ex As Exception
            MessageBox.Show($"Error accessing database backup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnColorCustomization_Click(sender As Object, e As EventArgs) Handles btnColorCustomization.Click
        Try
            Dim colorCustomizationForm As New ColorCustomization()
            colorCustomizationForm.ShowDialog()
        Catch ex As Exception
            MessageBox.Show($"Error opening Color Customization: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowDatabaseBackupDialog()
        Dim backupForm As New Form()
        backupForm.Text = "Database Backup & Restore"
        backupForm.Size = New Size(500, 350)
        backupForm.StartPosition = FormStartPosition.CenterParent
        backupForm.FormBorderStyle = FormBorderStyle.FixedDialog
        backupForm.MaximizeBox = False
        backupForm.MinimizeBox = False
        backupForm.BackColor = DarkSlate

        ' Title
        Dim lblTitle As New Label()
        lblTitle.Text = "DATABASE BACKUP & RESTORE"
        lblTitle.Font = New Font("Poppins", 16, FontStyle.Bold)
        lblTitle.ForeColor = PureWhite
        lblTitle.Location = New Point(20, 20)
        lblTitle.Size = New Size(460, 30)
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        backupForm.Controls.Add(lblTitle)

        ' Backup section
        Dim lblBackup As New Label()
        lblBackup.Text = "Backup Database"
        lblBackup.Font = New Font("Poppins", 12, FontStyle.Bold)
        lblBackup.ForeColor = GoldenYellow
        lblBackup.Location = New Point(20, 70)
        lblBackup.Size = New Size(200, 25)
        backupForm.Controls.Add(lblBackup)

        Dim lblBackupDesc As New Label()
        lblBackupDesc.Text = "Create a backup copy of your database"
        lblBackupDesc.Font = New Font("Poppins", 9, FontStyle.Regular)
        lblBackupDesc.ForeColor = LightSilver
        lblBackupDesc.Location = New Point(20, 95)
        lblBackupDesc.Size = New Size(300, 20)
        backupForm.Controls.Add(lblBackupDesc)

        Dim btnBackup As New Guna.UI2.WinForms.Guna2Button()
        btnBackup.Text = "💾 Create Backup"
        btnBackup.Size = New Size(200, 40)
        btnBackup.Location = New Point(20, 125)
        btnBackup.BorderRadius = 8
        btnBackup.FillColor = SuccessGreen
        btnBackup.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnBackup.ForeColor = PureWhite
        AddHandler btnBackup.Click, Sub() PerformDatabaseBackup()
        backupForm.Controls.Add(btnBackup)

        ' Restore section
        Dim lblRestore As New Label()
        lblRestore.Text = "Restore Database"
        lblRestore.Font = New Font("Poppins", 12, FontStyle.Bold)
        lblRestore.ForeColor = GoldenYellow
        lblRestore.Location = New Point(20, 190)
        lblRestore.Size = New Size(200, 25)
        backupForm.Controls.Add(lblRestore)

        Dim lblRestoreDesc As New Label()
        lblRestoreDesc.Text = "Restore database from backup file"
        lblRestoreDesc.Font = New Font("Poppins", 9, FontStyle.Regular)
        lblRestoreDesc.ForeColor = LightSilver
        lblRestoreDesc.Location = New Point(20, 215)
        lblRestoreDesc.Size = New Size(300, 20)
        backupForm.Controls.Add(lblRestoreDesc)

        Dim btnRestore As New Guna.UI2.WinForms.Guna2Button()
        btnRestore.Text = "📁 Restore Backup"
        btnRestore.Size = New Size(200, 40)
        btnRestore.Location = New Point(20, 245)
        btnRestore.BorderRadius = 8
        btnRestore.FillColor = Color.FromArgb(255, 140, 0)
        btnRestore.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnRestore.ForeColor = PureWhite
        AddHandler btnRestore.Click, Sub() PerformDatabaseRestore()
        backupForm.Controls.Add(btnRestore)

        ' Close button
        Dim btnClose As New Guna.UI2.WinForms.Guna2Button()
        btnClose.Text = "Close"
        btnClose.Size = New Size(100, 35)
        btnClose.Location = New Point(380, 280)
        btnClose.BorderRadius = 8
        btnClose.FillColor = AlertRed
        btnClose.Font = New Font("Poppins", 10, FontStyle.Regular)
        btnClose.ForeColor = PureWhite
        AddHandler btnClose.Click, Sub() backupForm.Close()
        backupForm.Controls.Add(btnClose)

        backupForm.ShowDialog()
        backupForm.Dispose()
    End Sub

    Private Sub PerformDatabaseBackup()
        Try
            Dim saveDialog As New SaveFileDialog()
            saveDialog.Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
            saveDialog.Title = "Save Database Backup"
            saveDialog.FileName = $"JadeDentalSupply_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.bak"

            If saveDialog.ShowDialog() = DialogResult.OK Then
                ' Simple file copy backup for LocalDB
                Dim sourcePath As String = Path.Combine(Application.StartupPath, "App_Data", "JadeDentalSupply.mdf")
                File.Copy(sourcePath, saveDialog.FileName, True)

                MessageBox.Show("Database backup created successfully!", "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)

                ' Log the backup
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Database Backup", $"Database backed up to: {saveDialog.FileName}")
            End If
        Catch ex As Exception
            MessageBox.Show($"Error creating backup: {ex.Message}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PerformDatabaseRestore()
        Try
            Dim openDialog As New OpenFileDialog()
            openDialog.Filter = "Backup files (*.bak)|*.bak|Database files (*.mdf)|*.mdf|All files (*.*)|*.*"
            openDialog.Title = "Select Database Backup File"

            If openDialog.ShowDialog() = DialogResult.OK Then
                Dim result = MessageBox.Show("Are you sure you want to restore the database? This will overwrite all current data!",
                                           "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

                If result = DialogResult.Yes Then
                    ' Simple file copy restore for LocalDB
                    Dim targetPath As String = Path.Combine(Application.StartupPath, "App_Data", "JadeDentalSupply.mdf")
                    File.Copy(openDialog.FileName, targetPath, True)

                    MessageBox.Show("Database restored successfully! Please restart the application.", "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)

                    ' Log the restore
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Database Restore", $"Database restored from: {openDialog.FileName}")
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error restoring database: {ex.Message}", "Restore Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Navigation event handlers
    Private Sub NavDashboard_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Dashboard.Show()
        Me.Close()
    End Sub

    Private Sub NavSales_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Me.Close()
    End Sub

    Private Sub NavInventory_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Inventory.Show()
        Me.Close()
    End Sub

    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)
        isNavigating = True
        MessageBox.Show("Sales Records feature coming soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub NavStaff_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Staff.Show()
        Me.Close()
    End Sub

    Private Sub NavInventoryLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Me.Close()
    End Sub

    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        MessageBox.Show("Audit Logs feature coming soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub Sys_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)

        ' If this is programmatic navigation, don't show confirmation
        If isNavigating Then
            Return
        End If

        ' Prevent multiple confirmations by checking the close reason
        If e.CloseReason = CloseReason.ApplicationExitCall Then
            Return
        End If

        ' Show confirmation only for user-initiated close (X button)
        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to exit the application?", "Exit Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ' Log the exit action
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via System Settings form")
                End If

                ' Close all forms properly
                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then
                        form.Close()
                    End If
                Next

                Application.Exit()
            Else
                e.Cancel = True
            End If
        End If
    End Sub
End Class