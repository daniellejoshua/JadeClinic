Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Linq
Imports Guna.UI2.WinForms
Imports Microsoft.Data.Sqlite
Imports System.Data.Common
Imports System.IO

Public Class Sys
    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    ' Profile dropdown panel
    ' Profile managed by ProfileManager

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
        Try
            Console.WriteLine($"Sys_Load: starting. Form size={Me.Size}, ClientSize={Me.ClientSize}, TopLevel={Me.TopLevel}, IsHosted={IsHostedInMainShell()}")

            Me.KeyPreview = True
            IdleTimeoutManager.Instance.StartMonitoring(Me)

            ' Only set standalone form properties when not hosted in MainShell
            If Not IsHostedInMainShell() Then
                Me.FormBorderStyle = FormBorderStyle.None
                Me.WindowState = FormWindowState.Maximized
            End If

            Console.WriteLine("Sys_Load: creating nav menu")
            ' Create navigation menu directly using shared builder
            NavigationBuilder.Build(DashboardPanel, Me, "Sys")

            Console.WriteLine("Sys_Load: validating session")
            ' Validate user session
            If Not ValidateUserSession() Then
                Return
            End If

            Console.WriteLine("Sys_Load: initializing profile")
            ' Initialize profile section
            InitializeProfileSection()

            ' Update form title to show logged-in user
            Me.Text = $"System Settings - {frmLoginvb.LoggedInUsername}"

            Console.WriteLine("Sys_Load: starting idle timeout")
            ' Start idle timeout monitoring
            IdleTimeoutManager.Instance.StartMonitoring(Me)

            Console.WriteLine("Sys_Load: initializing buttons")
            ' Initialize UI
            InitializeButtons()

            Console.WriteLine($"Sys_Load: completed. Controls count={Me.Controls.Count}")

            ' Set focus to form so ESC key works immediately
            Me.Activate()
            Me.Focus()

            SetupTabIndex()
        Catch ex As Exception
            MessageBox.Show($"Sys_Load error: {ex.Message}", "Sys Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetupTabIndex()
        btnCompanySettings.TabIndex = 0
        btnDatabaseBackup.TabIndex = 1
        btnColorCustomization.TabIndex = 2
        Utilities.ApplyInputFocusEffects(Me)
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

    Private Function ValidateUserSession() As Boolean
        If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            MessageBox.Show("User session expired. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            If IsHostedInMainShell() Then
                GetMainShell().ShowPage(GetType(frmLoginvb))
            Else
                frmLoginvb.Show()
            End If
            Me.Close()
            Return False
        End If
        Return True
    End Function

    Private Sub InitializeProfileSection()
        ProfileManager.InitializeProfile(Me, lblUsername, Guna2CirclePictureBox1, AddressOf NavigateToProfileSettings)
    End Sub

    Private Sub NavigateToProfileSettings()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from System to ProfileSettings")
            End If

            isNavigating = True
            ProfileManager.HideProfileDropdown(Me)

            Dim profileForm As New ProfileSettings()
            profileForm.StartPosition = FormStartPosition.CenterScreen
            profileForm.Show()

            If Not IsHostedInMainShell() Then
                Me.Close()
            End If
        Catch ex As Exception
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
        btnBackup.Text = "?? Create Backup"
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
        btnRestore.Text = "?? Restore Backup"
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

                Dim connBuilder As New SqliteConnectionStringBuilder(Connection.GetConnectionString())
                Dim dbPath As String = connBuilder.DataSource
                If Not String.IsNullOrWhiteSpace(dbPath) AndAlso File.Exists(dbPath) Then
                    File.Copy(dbPath, backupPath, overwrite:=True)
                    MessageBox.Show($"Database backup created successfully!{vbCrLf}Location: {backupPath}", "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Database Backup", $"Database backed up to: {backupPath}")
                Else
                    MessageBox.Show("Database file not found.", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
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

                    Dim connBuilder As New SqliteConnectionStringBuilder(Connection.GetConnectionString())
                    Dim dbPath As String = connBuilder.DataSource
                    If Not String.IsNullOrWhiteSpace(dbPath) Then
                        File.Copy(backupPath, dbPath, overwrite:=True)
                    End If

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

    Private Sub Sys_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        IdleTimeoutManager.Instance.StopMonitoring(Me)

        If isNavigating Then
            Return
        End If

        ' Skip exit confirmation when hosted in MainShell
        If IsHostedInMainShell() Then
            Return
        End If

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

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If (keyData And Keys.KeyCode) = Keys.Escape Then
            If isNavigating Then
                Return True
            End If

            If Me.OwnedForms.Cast(Of Form)().Any(Function(f) f.Visible) Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            If Application.OpenForms.Cast(Of Form)().Any(Function(f) f IsNot Me AndAlso f.Visible AndAlso f.Modal) Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            If Not Me.ContainsFocus Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            Dim result As DialogResult = EscForm.ConfirmExit(Me)

            If Me.Visible Then
                Me.Activate()
                If Me.CanFocus Then
                    Me.Focus()
                End If
            End If

            If result = DialogResult.Yes Then
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via System Settings form")
                End If

                isNavigating = True
                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then
                        form.Close()
                    End If
                Next

                Application.Exit()
            End If

            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function
    Private Sub Guna2Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Guna2Panel1.Paint

    End Sub

    Private Function IsHostedInMainShell() As Boolean
        Dim parent As Control = Me.Parent
        While parent IsNot Nothing
            If TypeOf parent Is MainShell Then
                Return True
            End If
            parent = parent.Parent
        End While
        Return False
    End Function

    Private Function GetMainShell() As MainShell
        Dim parent As Control = Me.Parent
        While parent IsNot Nothing
            If TypeOf parent Is MainShell Then
                Return CType(parent, MainShell)
            End If
            parent = parent.Parent
        End While
        Return Nothing
    End Function
End Class
