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
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)
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


    End Sub

    ' Create navigation menu
    Private Sub CreateNavigationMenu()
        NavigationBuilder.Build(DashboardPanel, Me, "Sys")
    End Sub
    Private Sub NavSuppliers_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            Supplier.Show()
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Unable to open Suppliers: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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

        ' Consistent palette used across forms
        btn.FillColor = If(isActive, GoldenYellow, System.Drawing.Color.Transparent)
        btn.ForeColor = If(isActive, DeepCharcoal, PureWhite)
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(80, 80, 80))
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = DeepCharcoal
        btn.ShadowDecoration.Depth = 4

        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.FromArgb(48, 52, 54)
                                           btn.BorderColor = RichOlive
                                           btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                       End If
                                   End Sub

        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.Transparent
                                           btn.BorderColor = System.Drawing.Color.FromArgb(80, 80, 80)
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
        profileDropdownPanel.Size = New System.Drawing.Size(200, 100)
        profileDropdownPanel.BackColor = System.Drawing.Color.FromArgb(41, 44, 45)
        profileDropdownPanel.BorderStyle = BorderStyle.FixedSingle

        Dim profileLocation = Guna2CirclePictureBox1.Location
        profileDropdownPanel.Location = New Point(profileLocation.X - 90, profileLocation.Y + Guna2CirclePictureBox1.Height + 5)

        Dim btnProfileSettings As New Label()
        btnProfileSettings.Text = "⚙️ Profile Settings"
        btnProfileSettings.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnProfileSettings.ForeColor = System.Drawing.Color.White
        btnProfileSettings.Size = New System.Drawing.Size(190, 40)
        btnProfileSettings.Location = New System.Drawing.Point(5, 5)
        btnProfileSettings.TextAlign = ContentAlignment.MiddleLeft
        btnProfileSettings.Cursor = Cursors.Hand

        AddHandler btnProfileSettings.MouseEnter, Sub() btnProfileSettings.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        AddHandler btnProfileSettings.MouseLeave, Sub() btnProfileSettings.BackColor = System.Drawing.Color.Transparent
        AddHandler btnProfileSettings.Click, Sub()
                                                 HideProfileDropdown()
                                                 NavigateToProfileSettings()
                                             End Sub

        Dim btnLogOut As New Label()
        btnLogOut.Text = "🚪 Log Out"
        btnLogOut.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnLogOut.ForeColor = System.Drawing.Color.White
        btnLogOut.Size = New System.Drawing.Size(190, 40)
        btnLogOut.Location = New System.Drawing.Point(5, 50)
        btnLogOut.TextAlign = ContentAlignment.MiddleLeft
        btnLogOut.Cursor = Cursors.Hand

        AddHandler btnLogOut.MouseEnter, Sub() btnLogOut.BackColor = Graphite
        AddHandler btnLogOut.MouseLeave, Sub() btnLogOut.BackColor = System.Drawing.Color.Transparent
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
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from System to ProfileSettings")
            End If

            ' Prevent the form-closing confirmation and hide dropdown first
            isNavigating = True
            HideProfileDropdown()

            ' Open ProfileSettings form (centered) and close this form
            Dim profileForm As New ProfileSettings()
            profileForm.StartPosition = FormStartPosition.CenterScreen
            profileForm.Show()

            Me.Close()
        Catch ex As Exception
            ' Restore flag on failure and show error
            isNavigating = False
            MessageBox.Show($"Unable to open Profile Settings: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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


    Private Sub ShowDatabaseBackupDialog()
        Dim backupForm As New Form()
        backupForm.Text = "Database Backup & Restore"
        backupForm.Size = New Size(500, 390)
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
                Dim backupPath As String = saveDialog.FileName
                If Not backupPath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) Then
                    backupPath &= ".bak"
                End If

                Dim backupDir As String = Path.GetDirectoryName(backupPath)
                If Not String.IsNullOrWhiteSpace(backupDir) AndAlso Not Directory.Exists(backupDir) Then
                    Directory.CreateDirectory(backupDir)
                End If

                Dim connBuilder As New SqlConnectionStringBuilder(Connection.GetConnectionString())
                Dim databaseName As String = If(String.IsNullOrWhiteSpace(connBuilder.InitialCatalog), "JadeDentalSupply", connBuilder.InitialCatalog)
                Dim escapedPath As String = backupPath.Replace("'", "''")

                Dim backupSql As String =
                    $"BACKUP DATABASE [{databaseName}] TO DISK = N'{escapedPath}' WITH INIT, FORMAT, STATS = 10;"

                Using conn As New SqlConnection(Connection.GetConnectionString())
                    conn.Open()
                    Using cmd As New SqlCommand(backupSql, conn)
                        cmd.CommandTimeout = 0
                        cmd.ExecuteNonQuery()
                    End Using
                End Using

                MessageBox.Show($"Database backup created successfully!{vbCrLf}Location: {backupPath}", "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)

                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Database Backup", $"Database backed up to: {backupPath}")
            End If
        Catch ex As Exception
            MessageBox.Show($"Error creating backup: {ex.Message}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Console.WriteLine($"Backup error details: {ex}")
        End Try
    End Sub

    Private Sub PerformDatabaseRestore()
        Try
            Dim openDialog As New OpenFileDialog()
            openDialog.Filter = "Backup files (*.bak)|*.bak|All files (*.*)|*.*"
            openDialog.Title = "Select Database Backup File"

            If openDialog.ShowDialog() = DialogResult.OK Then
                Dim result = MessageBox.Show("Are you sure you want to restore the database? This will overwrite all current data!" & vbCrLf & vbCrLf & "IMPORTANT: The application will close after restore.",
                                           "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

                If result = DialogResult.Yes Then
                    Dim backupPath As String = openDialog.FileName
                    If Not File.Exists(backupPath) Then
                        Throw New FileNotFoundException("Selected backup file was not found.")
                    End If

                    Dim connBuilder As New SqlConnectionStringBuilder(Connection.GetConnectionString())
                    Dim databaseName As String = If(String.IsNullOrWhiteSpace(connBuilder.InitialCatalog), "JadeDentalSupply", connBuilder.InitialCatalog)
                    Dim escapedPath As String = backupPath.Replace("'", "''")
                    Dim masterConnStr As String = "Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=True;"

                    Dim restoreSql As String =
                        $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;" &
                        $"RESTORE DATABASE [{databaseName}] FROM DISK = N'{escapedPath}' WITH REPLACE, RECOVERY;" &
                        $"ALTER DATABASE [{databaseName}] SET MULTI_USER;"

                    Using conn As New SqlConnection(masterConnStr)
                        conn.Open()
                        Using cmd As New SqlCommand(restoreSql, conn)
                            cmd.CommandTimeout = 0
                            cmd.ExecuteNonQuery()
                        End Using
                    End Using

                    MessageBox.Show("Database restored successfully! The application will now close.", "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)

                    Try
                        Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Database Restore", $"Database restored from: {backupPath}")
                    Catch
                    End Try

                    Application.Exit()
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
            Dim result As DialogResult = EscForm.ConfirmExit(Me)

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
    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            Dim salesRecordsForm As New SalesRecord()
            salesRecordsForm.StartPosition = FormStartPosition.CenterScreen
            salesRecordsForm.Show()
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Unable to open Sales Records: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            Dim auditLogForm As New AuditLog()
            auditLogForm.StartPosition = FormStartPosition.CenterScreen
            auditLogForm.Show()
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Unable to open Audit Logs: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub Guna2Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Guna2Panel1.Paint

    End Sub
End Class