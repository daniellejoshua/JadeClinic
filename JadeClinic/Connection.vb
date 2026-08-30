Imports Microsoft.Data.Sqlite
Imports System.Configuration
Imports System.IO

Module Connection

    Private Function GetDefaultDatabasePath() As String
        Dim dataDir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JadeClinic")
        If Not Directory.Exists(dataDir) Then
            Directory.CreateDirectory(dataDir)
        End If
        Return Path.Combine(dataDir, "jadeclinic.db")
    End Function

    Public Function GetConnectionString() As String
        Dim dbPath As String = GetDefaultDatabasePath()
        Return $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate"
    End Function

    Public Function GetDatabaseFolder() As String
        Return Path.GetDirectoryName(GetDefaultDatabasePath())
    End Function

    Public Function GetImagesFolder(Optional subfolder As String = "") As String
        Dim folder As String = Path.Combine(GetDatabaseFolder(), "Images")
        If Not String.IsNullOrEmpty(subfolder) Then
            folder = Path.Combine(folder, subfolder)
        End If
        If Not Directory.Exists(folder) Then
            Directory.CreateDirectory(folder)
        End If
        Return folder
    End Function

    Public Function TestConnection(Optional serverName As String = "") As Boolean
        Try
            Dim connStr As String = GetConnectionString()
            Using conn As New SqliteConnection(connStr)
                conn.Open()
                Console.WriteLine($"? Connected to SQLite DB: {conn.DataSource}")
                Return True
            End Using
        Catch ex As Exception
            Console.WriteLine($"? Connection test failed: {ex.Message}")
            Return False
        End Try
    End Function

    Public Function CreateDatabaseIfNotExists() As Boolean
        Return TestConnection()
    End Function

    Public Sub SaveConnectionString(Optional serverName As String = "")
        Console.WriteLine("SaveConnectionString: SQLite uses local file; no action taken.")
    End Sub

    Public Function InitializeDatabase() As Boolean
        Try
            Using conn As New SqliteConnection(GetConnectionString())
                conn.Open()

                Dim checkTablesQuery As String = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'"
                Using cmd As New SqliteCommand(checkTablesQuery, conn)
                    Dim tableCount As Integer = Convert.ToInt32(cmd.ExecuteScalar())

                    If tableCount = 0 Then
                        Console.WriteLine("No tables found. Creating database schema...")
                        DatabaseInitializer.CreateDatabaseSchema()
                        Console.WriteLine("? Database schema initialized successfully!")
                    Else
                        ' Existing database: run schema maintenance/migrations so new
                        ' columns land on already-created tables too (e.g. Sales
                        ' ApprovedBy/AbortReason and the IsVoid drop).
                        Try
                            SalesStatusMigration.UpdateDatabaseForSalesStatus()
                        Catch
                        End Try
                    End If
                End Using
            End Using

            Return True
        Catch ex As Exception
            Console.WriteLine($"? Database initialization failed: {ex.Message}")
            Return False
        End Try
    End Function

    Public Function GetEnvironmentName() As String
        Dim appMode As String = ConfigurationManager.AppSettings("ApplicationMode")
        Return If(String.IsNullOrEmpty(appMode), "Production", appMode)
    End Function

    Public Function IsDevelopmentMode() As Boolean
        Return False
    End Function

    Public Function GetDatabaseInfo() As String
        Try
            Using conn As New SqliteConnection(GetConnectionString())
                conn.Open()
                Return $"Database file: {conn.DataSource} | State: Connected"
            End Using
        Catch ex As Exception
            Return $"Database: Disconnected | Error: {ex.Message}"
        End Try
    End Function
End Module