Imports Microsoft.VisualBasic.ApplicationServices
Imports System.IO
Imports System.Windows.Forms

Namespace My
    Partial Friend Class MyApplication
        Private Sub MyApplication_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
            ' Updater mode (--apply-update "<updateDir>" "<appDir>" <oldPid>):
            ' launched by the previous instance to install a downloaded build.
            ' Runs before any database or terminal initialization, then exits.
            Dim cliArgs As String() = Environment.GetCommandLineArgs()
            If cliArgs.Length >= 5 AndAlso
               String.Equals(cliArgs(1), "--apply-update", StringComparison.OrdinalIgnoreCase) Then
                Dim updateDir As String = cliArgs(2)
                Dim appDir As String = cliArgs(3)
                Dim oldPid As Integer = 0
                Integer.TryParse(cliArgs(4), oldPid)
                Using form As New ApplyingUpdateForm(updateDir, appDir, oldPid)
                    form.ShowDialog()
                End Using
                e.Cancel = True
                Return
            End If

            ' Headless demo-data seed (--seed-demo):
            ' Initializes the PostgreSQL schema then loads the full demo store,
            ' the same as the "Load Demo Data" button in System Settings.
            If cliArgs.Length >= 2 AndAlso
               String.Equals(cliArgs(1), "--seed-demo", StringComparison.OrdinalIgnoreCase) Then
                Try
                    Console.WriteLine("Seeding demo data...")
                    If Not AutoInitializeDatabase() Then
                        Console.WriteLine("SEED FAILED: could not initialize the database.")
                        e.Cancel = True
                        Return
                    End If
                    Dim summary As String = DemoDataSeeder.SeedDemoData()
                    Console.WriteLine("SEED DONE: " & summary)
                Catch ex As Exception
                    Console.WriteLine("SEED FAILED: " & ex.ToString())
                End Try
                e.Cancel = True
                Return
            End If

            Dim appPath As String = AppDomain.CurrentDomain.BaseDirectory
            AppDomain.CurrentDomain.SetData("DataDirectory", appPath)

            Dim appDataPath As String = Path.Combine(appPath, "App_Data")
            If Not Directory.Exists(appDataPath) Then
                Directory.CreateDirectory(appDataPath)
            End If

            If Not AutoInitializeDatabase() Then
                e.Cancel = True
            Else
                ' Sync/backup history lives in the local DB - prepare the table
                ' and mark any crashed runs as failed before the UI starts.
                LocalSyncLog.Initialize()

                ' Report this PC to the shared Terminals table and enforce the
                ' admin's access control (only in shared-LAN mode). New PCs stay
                ' 'pending' until the administrator approves them.
                TerminalManager.RegisterCurrentTerminal()
                TerminalManager.EnsureAdminApproval()

                Select Case TerminalManager.GetTerminalAccessStatus()
                    Case TerminalManager.TerminalAccessStatus.Blocked
                        MessageBox.Show(
                            "This computer has been blocked by the administrator." & vbCrLf & vbCrLf &
                            "Contact your administrator to enable access for this PC in the PC Terminals page.",
                            "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        e.Cancel = True
                        Return
                    Case TerminalManager.TerminalAccessStatus.Pending
                        Using waitForm As New TerminalApprovalWaitForm()
                            If waitForm.ShowDialog() <> DialogResult.OK Then
                                e.Cancel = True
                                Return
                            End If
                        End Using
                End Select
            End If
        End Sub

        Private Function AutoInitializeDatabase() As Boolean
            Try
                ' For zero-install deployment we use a local SQLite file. Do not require LocalDB.
                Return Connection.InitializeDatabase()
            Catch ex As Exception
                Console.WriteLine($"AutoInitializeDatabase error: {ex.Message}")
                Return False
            End Try
        End Function

    End Class
End Namespace