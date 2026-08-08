Imports Microsoft.VisualBasic.ApplicationServices
Imports System.IO

Namespace My
    Partial Friend Class MyApplication
        Private Sub MyApplication_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
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