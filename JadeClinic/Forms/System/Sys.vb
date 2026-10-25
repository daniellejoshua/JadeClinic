Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.Linq
Imports System.Threading.Tasks
Imports Guna.UI2.WinForms
Imports Npgsql
Imports System.Data.Common
Imports System.IO
Imports System.Diagnostics
Imports System.Configuration

Public Class Sys
    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False
    Private _headerUserControl As HeaderUserControl

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
        Me.BackColor = Color.FromArgb(248, 248, 247)
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
            ApplyRoleGating()

            Console.WriteLine($"Sys_Load: completed. Controls count={Me.Controls.Count}")

            ' Set focus to form so ESC key works immediately
            Me.Activate()
            Me.Focus()

            SetupTabIndex()

            ' Load recent sync history in the background
            LoadSyncLogsAsync()

            ' Listen for background sync progress from the queue
            AddHandler SyncQueue.Instance.Progress, AddressOf OnSyncQueueProgress
            AddHandler SyncQueue.Instance.Completed, AddressOf OnSyncQueueCompleted
            AddHandler SyncQueue.Instance.Failed, AddressOf OnSyncQueueFailed
        Catch ex As Exception
            MessageBox.Show($"Sys_Load error: {ex.Message}", "Sys Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetupTabIndex()
        btnDatabaseBackup.TabIndex = 0
        btnColorCustomization.TabIndex = 1
        btnSyncCloud.TabIndex = 2
        Utilities.ApplyInputFocusEffects(Me)
    End Sub

    ' Hide capabilities the signed-in role is not allowed to use. The System
    ' Settings page itself is only reachable by Admin/SuperAdmin, so this only
    ' further restricts the SuperAdmin-only tiles for plain Admins.
    Private Sub ApplyRoleGating()
        btnDatabaseBackup.Visible = Rbac.CanCreateBackup()
        btnSyncCloud.Visible = Rbac.CanRunCloudSync()
        btnSyncLogs.Visible = Rbac.CanViewSyncLogs()
        btnTerminalSetup.Visible = Rbac.CanManageTerminals()
        btnLoadDemoData.Visible = Rbac.CanLoadDemoData()

        chkForceFull.Enabled = Rbac.CanForceFullSync()
        chkForceFull.Visible = Rbac.CanForceFullSync()
    End Sub

    Private Sub InitializeButtons()
        ' Database Backup button
        btnDatabaseBackup.FillColor = PureWhite

        AddHandler btnDatabaseBackup.MouseEnter, Sub()
                                                     btnDatabaseBackup.FillColor = GoldenYellow
                                                     btnDatabaseBackup.ForeColor = DeepCharcoal
                                                     btnDatabaseBackup.BorderColor = GoldenYellow
                                                     btnDatabaseBackup.BorderThickness = 1
                                                 End Sub
        AddHandler btnDatabaseBackup.MouseLeave, Sub()
                                                     btnDatabaseBackup.FillColor = PureWhite
                                                     btnDatabaseBackup.ForeColor = Color.FromArgb(51, 51, 51)
                                                     btnDatabaseBackup.BorderColor = Color.FromArgb(232, 232, 232)
                                                     btnDatabaseBackup.BorderThickness = 1
                                                 End Sub

        ' Sync to Cloud button
        btnSyncCloud.FillColor = PureWhite

        AddHandler btnSyncCloud.MouseEnter, Sub()
                                                btnSyncCloud.FillColor = GoldenYellow
                                                btnSyncCloud.ForeColor = DeepCharcoal
                                                btnSyncCloud.BorderColor = GoldenYellow
                                                btnSyncCloud.BorderThickness = 1
                                            End Sub
        AddHandler btnSyncCloud.MouseLeave, Sub()
                                                btnSyncCloud.FillColor = PureWhite
                                                btnSyncCloud.ForeColor = Color.FromArgb(51, 51, 51)
                                                btnSyncCloud.BorderColor = Color.FromArgb(232, 232, 232)
                                                btnSyncCloud.BorderThickness = 1
                                            End Sub

        ' Load Demo Data button
        btnLoadDemoData.FillColor = PureWhite

        AddHandler btnLoadDemoData.MouseEnter, Sub()
                                                   btnLoadDemoData.FillColor = GoldenYellow
                                                   btnLoadDemoData.ForeColor = DeepCharcoal
                                                   btnLoadDemoData.BorderColor = GoldenYellow
                                                   btnLoadDemoData.BorderThickness = 1
                                               End Sub
        AddHandler btnLoadDemoData.MouseLeave, Sub()
                                                   btnLoadDemoData.FillColor = PureWhite
                                                   btnLoadDemoData.ForeColor = Color.FromArgb(51, 51, 51)
                                                   btnLoadDemoData.BorderColor = Color.FromArgb(232, 232, 232)
                                                   btnLoadDemoData.BorderThickness = 1
                                               End Sub

        ' PC Terminals button
        btnTerminalSetup.FillColor = PureWhite

        AddHandler btnTerminalSetup.MouseEnter, Sub()
                                                    btnTerminalSetup.FillColor = GoldenYellow
                                                    btnTerminalSetup.ForeColor = DeepCharcoal
                                                    btnTerminalSetup.BorderColor = GoldenYellow
                                                    btnTerminalSetup.BorderThickness = 1
                                                End Sub
        AddHandler btnTerminalSetup.MouseLeave, Sub()
                                                    btnTerminalSetup.FillColor = PureWhite
                                                    btnTerminalSetup.ForeColor = Color.FromArgb(51, 51, 51)
                                                    btnTerminalSetup.BorderColor = Color.FromArgb(232, 232, 232)
                                                    btnTerminalSetup.BorderThickness = 1
                                                End Sub

        ' Sync Logs button
        btnSyncLogs.FillColor = PureWhite

        AddHandler btnSyncLogs.MouseEnter, Sub()
                                               btnSyncLogs.FillColor = GoldenYellow
                                               btnSyncLogs.ForeColor = DeepCharcoal
                                               btnSyncLogs.BorderColor = GoldenYellow
                                               btnSyncLogs.BorderThickness = 1
                                           End Sub
        AddHandler btnSyncLogs.MouseLeave, Sub()
                                               btnSyncLogs.FillColor = PureWhite
                                               btnSyncLogs.ForeColor = Color.FromArgb(51, 51, 51)
                                               btnSyncLogs.BorderColor = Color.FromArgb(232, 232, 232)
                                               btnSyncLogs.BorderThickness = 1
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
        _headerUserControl = New HeaderUserControl()
        Me.Controls.Add(_headerUserControl)
        _headerUserControl.BringToFront()
        _headerUserControl.Initialize(Me, AddressOf NavigateToProfileSettings)
    End Sub

    Private Sub NavigateToProfileSettings()
        Try
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
    Private Sub btnTerminalSetup_Click(sender As Object, e As EventArgs) Handles btnTerminalSetup.Click
        Try
            If Not Rbac.CanManageTerminals() Then
                MessageBox.Show("Only a Super Admin can manage terminals.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            NavigationBuilder.NavigateToForm(Me, GetType(TerminalSetup))
        Catch ex As Exception
            MessageBox.Show($"Error opening PC Terminals: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnSyncLogs_Click(sender As Object, e As EventArgs) Handles btnSyncLogs.Click
        Try
            If Not Rbac.CanViewSyncLogs() Then
                MessageBox.Show("You do not have permission to view sync logs.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            NavigationBuilder.NavigateToForm(Me, GetType(SyncLogs))
        Catch ex As Exception
            MessageBox.Show($"Error opening Sync Logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnDatabaseBackup_Click(sender As Object, e As EventArgs) Handles btnDatabaseBackup.Click
        Try
            If Not Rbac.CanCreateBackup() Then
                MessageBox.Show("You do not have permission to back up the database.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            ShowDatabaseBackupDialog()
        Catch ex As Exception
            MessageBox.Show($"Error accessing database backup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnSyncCloud_Click(sender As Object, e As EventArgs) Handles btnSyncCloud.Click
        Try
            If Not Rbac.CanRunCloudSync() Then
                MessageBox.Show("You do not have permission to run cloud sync.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            SyncQueue.Instance.EnqueueSmart(forceFull:=chkForceFull.Checked)
            lblSyncStatus.Text = If(chkForceFull.Checked,
                "Cloud sync queued (full) - running in the background...",
                "Cloud sync queued - running in the background...")
            lblSyncStatus.ForeColor = Color.FromArgb(70, 70, 70)
        Catch ex As Exception
            lblSyncStatus.Text = "Could not start cloud sync: " & ex.Message
            lblSyncStatus.ForeColor = AlertRed
        End Try
    End Sub

    Private Sub btnLoadDemoData_Click(sender As Object, e As EventArgs) Handles btnLoadDemoData.Click
        Try
            If Not Rbac.CanLoadDemoData() Then
                MessageBox.Show("Only a Super Admin can load demo data.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim result As DialogResult = MessageBox.Show(
                "Load demo data?" & vbCrLf & vbCrLf &
                "This will DELETE all products, suppliers, sales, sale items, inventory logs, and audit logs " &
                "and replace them with realistic sample data spanning 3 years of history." & vbCrLf & vbCrLf &
                "It will also ADD demo users (manager, 3 staff, and 1 inactive admin) if they don't already exist." & vbCrLf &
                "Your admin account and company settings will NOT be affected." & vbCrLf & vbCrLf &
                "Are you sure you want to continue?",
                "Load Demo Data", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)

            If result <> DialogResult.Yes Then
                Return
            End If

            btnLoadDemoData.Enabled = False
            lblSyncStatus.Text = "Loading demo data... (this may take a minute or two)"
            lblSyncStatus.ForeColor = Color.FromArgb(70, 70, 70)

            Dim summary As String = ""
            Dim errorMsg As String = ""

            Task.Run(Sub()
                         Try
                             summary = DemoDataSeeder.SeedDemoData()
                         Catch ex As Exception
                             errorMsg = ex.Message
                         End Try
                     End Sub).Wait()

            If Not String.IsNullOrEmpty(errorMsg) Then
                lblSyncStatus.Text = "Demo data load failed: " & errorMsg
                lblSyncStatus.ForeColor = AlertRed
                MessageBox.Show($"Demo data load failed: {errorMsg}", "Demo Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Else
                lblSyncStatus.Text = summary
                lblSyncStatus.ForeColor = SuccessGreen
                MessageBox.Show(summary, "Demo Data Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            lblSyncStatus.Text = "Demo data load failed: " & ex.Message
            lblSyncStatus.ForeColor = AlertRed
            MessageBox.Show($"Demo data load failed: {ex.Message}", "Demo Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            btnLoadDemoData.Enabled = True
        End Try
    End Sub

    Private Sub LoadSyncLogsAsync()
        Task.Run(Sub()
                     Dim lines As New List(Of String)
                     Dim dt As DataTable = LocalSyncLog.GetEvents(8)
                     For Each row As DataRow In dt.Rows
                         Dim status As String = Convert.ToString(row("Status"))
                         Dim statusIcon As String = If(status = "success", "[OK]", If(status = "failed", "[FAIL]", "[...]"))
                         Dim mode As String = Convert.ToString(row("Mode"))
                         If String.IsNullOrEmpty(mode) Then mode = "delta"
                         Dim line As String = $"{statusIcon} {row("Started")}  {row("Rows")} rows  mode: {mode}"
                         Dim deleted As Integer = Convert.ToInt32(row("Deleted"))
                         If deleted > 0 Then line &= $"  deleted: {deleted}"
                         If Convert.ToString(row("Snap Local")) = "Yes" Then line &= "  snapshot: yes"
                         Dim err As String = Convert.ToString(row("Error"))
                         If err <> "" Then line &= "  - " & err
                         lines.Add(line)
                     Next
                     If Not IsDisposed Then
                         BeginInvoke(New Action(Sub() DisplaySyncLogs(lines)))
                     End If
                 End Sub)
    End Sub

    Private Sub DisplaySyncLogs(lines As List(Of String))
        If lines Is Nothing OrElse lines.Count = 0 Then
            lblSyncLogs.Text = "Sync history: (none yet)"
        Else
            lblSyncLogs.Text = "Recent syncs:" & vbCrLf & String.Join(vbCrLf, lines)
        End If
    End Sub

    ' SyncQueue events fire on the worker thread - marshal to the UI thread.
    Private Sub OnSyncQueueProgress(message As String)
        If IsDisposed Then Return
        BeginInvoke(New Action(Sub()
                                   lblSyncStatus.Text = message
                                   lblSyncStatus.ForeColor = Color.FromArgb(70, 70, 70)
                               End Sub))
    End Sub

    Private Sub OnSyncQueueCompleted(success As Boolean, summary As String)
        If IsDisposed Then Return
        BeginInvoke(New Action(Sub()
                                   lblSyncStatus.Text = "Sync complete:" & vbCrLf & summary
                                   lblSyncStatus.ForeColor = SuccessGreen
                                   LoadSyncLogsAsync()
                               End Sub))
    End Sub

    Private Sub OnSyncQueueFailed(errorMessage As String)
        If IsDisposed Then Return
        BeginInvoke(New Action(Sub()
                                   lblSyncStatus.Text = "Sync failed: " & errorMessage
                                   lblSyncStatus.ForeColor = AlertRed
                                   LoadSyncLogsAsync()
                               End Sub))
    End Sub


    Private Sub ShowDatabaseBackupDialog()
        Dim backupForm As New Form()
        backupForm.Text = "Database Backup & Restore"
        backupForm.Size = New Size(500, 390)
        backupForm.StartPosition = FormStartPosition.CenterParent
        backupForm.FormBorderStyle = FormBorderStyle.FixedDialog
        backupForm.MaximizeBox = False
        backupForm.MinimizeBox = False
        backupForm.BackColor = Color.FromArgb(250, 249, 246)

        ' Title
        Dim lblTitle As New Label()
        lblTitle.Text = "DATABASE BACKUP & RESTORE"
        lblTitle.Font = New Font("Poppins", 16, FontStyle.Bold)
        lblTitle.ForeColor = DeepCharcoal
        lblTitle.Location = New Point(20, 20)
        lblTitle.Size = New Size(460, 30)
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        backupForm.Controls.Add(lblTitle)

        ' Backup section
        Dim lblBackup As New Label()
        lblBackup.Text = "Backup Database"
        lblBackup.Font = New Font("Poppins", 12, FontStyle.Bold)
        lblBackup.ForeColor = RichOlive
        lblBackup.Location = New Point(20, 70)
        lblBackup.Size = New Size(200, 25)
        backupForm.Controls.Add(lblBackup)

        Dim lblBackupDesc As New Label()
        lblBackupDesc.Text = "Create a backup copy of your database"
        lblBackupDesc.Font = New Font("Poppins", 9, FontStyle.Regular)
        lblBackupDesc.ForeColor = Color.FromArgb(119, 119, 119)
        lblBackupDesc.Location = New Point(20, 95)
        lblBackupDesc.Size = New Size(300, 20)
        backupForm.Controls.Add(lblBackupDesc)

        Dim btnBackup As New Guna.UI2.WinForms.Guna2Button()
        btnBackup.Text = "Create Backup"
        btnBackup.Size = New Size(200, 40)
        btnBackup.Location = New Point(20, 125)
        btnBackup.BorderRadius = 8
        btnBackup.FillColor = SuccessGreen
        btnBackup.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnBackup.ForeColor = PureWhite
        AddHandler btnBackup.Click, Sub() PerformDatabaseBackup()
        backupForm.Controls.Add(btnBackup)

        ' Restore section (Super Admin only)
        If Rbac.CanRestoreDatabase() Then
            Dim lblRestore As New Label()
            lblRestore.Text = "Restore Database"
            lblRestore.Font = New Font("Poppins", 12, FontStyle.Bold)
            lblRestore.ForeColor = RichOlive
            lblRestore.Location = New Point(20, 190)
            lblRestore.Size = New Size(200, 25)
            backupForm.Controls.Add(lblRestore)

            Dim lblRestoreDesc As New Label()
            lblRestoreDesc.Text = "Restore database from backup file"
            lblRestoreDesc.Font = New Font("Poppins", 9, FontStyle.Regular)
            lblRestoreDesc.ForeColor = Color.FromArgb(119, 119, 119)
            lblRestoreDesc.Location = New Point(20, 215)
            lblRestoreDesc.Size = New Size(300, 20)
            backupForm.Controls.Add(lblRestoreDesc)

            Dim btnRestore As New Guna.UI2.WinForms.Guna2Button()
            btnRestore.Text = "Restore Backup"
            btnRestore.Size = New Size(200, 40)
            btnRestore.Location = New Point(20, 245)
            btnRestore.BorderRadius = 8
            btnRestore.FillColor = Color.FromArgb(255, 140, 0)
            btnRestore.Font = New Font("Poppins", 10, FontStyle.Bold)
            btnRestore.ForeColor = PureWhite
            AddHandler btnRestore.Click, Sub() PerformDatabaseRestore()
            backupForm.Controls.Add(btnRestore)
        End If

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

        Utilities.EnableEscCloseModal(backupForm)
        backupForm.ShowDialog()
        backupForm.Dispose()
    End Sub

    Private Sub PerformDatabaseBackup()
        Try
            Dim saveDialog As New SaveFileDialog()
            saveDialog.Filter = "PostgreSQL backup files (*.bak)|*.bak|All files (*.*)|*.*"
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

                ' Use pg_dump for PostgreSQL backup
                Dim host As String = ConfigurationManager.AppSettings("PgHost")
                Dim port As String = ConfigurationManager.AppSettings("PgPort")
                Dim db As String = ConfigurationManager.AppSettings("PgDatabase")
                Dim user As String = ConfigurationManager.AppSettings("PgUser")
                Dim password As String = ConfigurationManager.AppSettings("PgPassword")

                Dim pgBin As String = "C:\Program Files\PostgreSQL\18\bin\pg_dump.exe"
                If Not File.Exists(pgBin) Then pgBin = "pg_dump"

                Dim psi As New ProcessStartInfo()
                psi.FileName = pgBin
                psi.Arguments = $"--host={host} --port={port} --username={user} --dbname={db} --format=custom --file=""{backupPath}"""
                psi.UseShellExecute = False
                psi.CreateNoWindow = True
                psi.EnvironmentVariables("PGPASSWORD") = password

                Dim success As Boolean = False
                Try
                    Using p As Process = Process.Start(psi)
                        p.WaitForExit(120000)
                        success = p.HasExited AndAlso p.ExitCode = 0
                    End Using
                Catch ex As Exception
                    Console.WriteLine($"pg_dump failed: {ex.Message}")
                End Try

                If success Then
                    MessageBox.Show($"Database backup created successfully!{vbCrLf}Location: {backupPath}", "Backup Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Database Backup", $"Database backed up to: {backupPath}")
                Else
                    MessageBox.Show("Backup failed. Check console for details.", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error creating backup: {ex.Message}", "Backup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Console.WriteLine($"Backup error details: {ex}")
        End Try
    End Sub

    Private Sub PerformDatabaseRestore()
        Try
            If Not Rbac.CanRestoreDatabase() Then
                MessageBox.Show("Only a Super Admin can restore the database.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

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

                    ' Use pg_restore for PostgreSQL restore
                    Dim host As String = ConfigurationManager.AppSettings("PgHost")
                    Dim port As String = ConfigurationManager.AppSettings("PgPort")
                    Dim db As String = ConfigurationManager.AppSettings("PgDatabase")
                    Dim user As String = ConfigurationManager.AppSettings("PgUser")
                    Dim password As String = ConfigurationManager.AppSettings("PgPassword")

                    Dim pgBin As String = "C:\Program Files\PostgreSQL\18\bin\pg_restore.exe"
                    If Not File.Exists(pgBin) Then pgBin = "pg_restore"

                    Dim psi As New ProcessStartInfo()
                    psi.FileName = pgBin
                    psi.Arguments = $"--host={host} --port={port} --username={user} --dbname={db} --clean --if-exists --no-owner --no-privileges ""{backupPath}"""
                    psi.UseShellExecute = False
                    psi.CreateNoWindow = True
                    psi.EnvironmentVariables("PGPASSWORD") = password

                    Dim success As Boolean = False
                    Try
                        Using p As Process = Process.Start(psi)
                            p.WaitForExit(180000)
                            success = p.HasExited AndAlso p.ExitCode = 0
                        End Using
                    Catch ex As Exception
                        Console.WriteLine($"pg_restore failed: {ex.Message}")
                    End Try

                    If success Then
                        MessageBox.Show("Database restored successfully! The application will now close.", "Restore Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        Try
                            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Database Restore", $"Database restored from: {backupPath}")
                        Catch
                        End Try

                        Application.Exit()
                    Else
                        MessageBox.Show("Restore failed. Check console for details.", "Restore Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error restoring database: {ex.Message}", "Restore Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Sys_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        IdleTimeoutManager.Instance.StopMonitoring(Me)

        RemoveHandler SyncQueue.Instance.Progress, AddressOf OnSyncQueueProgress
        RemoveHandler SyncQueue.Instance.Completed, AddressOf OnSyncQueueCompleted
        RemoveHandler SyncQueue.Instance.Failed, AddressOf OnSyncQueueFailed

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
