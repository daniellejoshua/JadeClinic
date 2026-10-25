Imports Npgsql
Imports System.Configuration
Imports System.IO

Module Connection

    Private Function GetConfigValue(key As String, defaultValue As String) As String
        Try
            Return ConfigurationManager.AppSettings(key)
        Catch
            Return defaultValue
        End Try
    End Function

    Private Function GetImagesFolderRoot() As String
        ' Shared images folder so every terminal sees the same product images.
        Dim configuredPath As String = GetConfigValue("ImagesPath", "")
        If Not String.IsNullOrWhiteSpace(configuredPath) Then
            Try
                If Not Directory.Exists(configuredPath) Then
                    Directory.CreateDirectory(configuredPath)
                End If
                Return configuredPath
            Catch ex As Exception
                Console.WriteLine($"? ImagesPath failure ({configuredPath}), falling back to local folder: {ex.Message}")
            End Try
        End If

        Dim dataDir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JadeClinic")
        If Not Directory.Exists(dataDir) Then
            Directory.CreateDirectory(dataDir)
        End If
        Return dataDir
    End Function

    Public Function GetConnectionString() As String
        Dim builder As New NpgsqlConnectionStringBuilder()
        builder.Host = GetConfigValue("PgHost", "localhost")
        builder.Port = CInt(GetConfigValue("PgPort", "5432"))
        builder.Database = GetConfigValue("PgDatabase", "jadeclinic")
        builder.Username = GetConfigValue("PgUser", "jadeclinic")
        builder.Password = GetConfigValue("PgPassword", "")
        builder.SSLMode = SslMode.Disable
        builder.Timeout = 15
        builder.CommandTimeout = 30
        Return builder.ConnectionString
    End Function

    Public Function GetImagesFolder(Optional subfolder As String = "") As String
        Dim folder As String = Path.Combine(GetImagesFolderRoot(), "Images")
        If Not String.IsNullOrEmpty(subfolder) Then
            folder = Path.Combine(folder, subfolder)
        End If
        If Not Directory.Exists(folder) Then
            Directory.CreateDirectory(folder)
        End If
        Return folder
    End Function

    Public Function GetBackupFolder() As String
        Dim configuredPath As String = GetConfigValue("BackupPath", "")
        If Not String.IsNullOrWhiteSpace(configuredPath) Then
            Try
                If Not Directory.Exists(configuredPath) Then
                    Directory.CreateDirectory(configuredPath)
                End If
                Return configuredPath
            Catch ex As Exception
                Console.WriteLine($"? BackupPath failure ({configuredPath}), falling back to local folder: {ex.Message}")
            End Try
        End If

        Dim dataDir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JadeClinic", "Backups")
        If Not Directory.Exists(dataDir) Then
            Directory.CreateDirectory(dataDir)
        End If
        Return dataDir
    End Function

    Public Function GetDatabaseFolder() As String
        Dim dataDir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JadeClinic")
        If Not Directory.Exists(dataDir) Then
            Directory.CreateDirectory(dataDir)
        End If
        Return dataDir
    End Function

    Public Function TestConnection(Optional serverName As String = "") As Boolean
        Try
            Dim connStr As String = GetConnectionString()
            Using conn As New NpgsqlConnection(connStr)
                conn.Open()
                Console.WriteLine($"? Connected to PostgreSQL: {conn.DataSource}")
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
        Console.WriteLine("SaveConnectionString: PostgreSQL strings are read from app.config; no file-based save required.")
    End Sub

    Public Function InitializeDatabase() As Boolean
        Try
            Using conn As New NpgsqlConnection(GetConnectionString())
                conn.Open()

                ' Deployment check: no tables exist yet -> create full schema + seed.
                Dim checkTablesQuery As String = "SELECT to_regclass('public.users') IS NULL"
                Using cmd As New NpgsqlCommand(checkTablesQuery, conn)
                    Dim noSchema As Boolean = Convert.ToBoolean(cmd.ExecuteScalar())

                    If noSchema Then
                        Console.WriteLine("No tables found. Creating database schema...")
                        DatabaseInitializer.CreateDatabaseSchema()
                        Console.WriteLine("? Database schema initialized successfully!")
                    Else
                        ' Existing database: run schema maintenance/migrations so new
                        ' columns land on already-created tables too.
                        Try
                            SalesStatusMigration.UpdateDatabaseForSalesStatus()
                        Catch
                        End Try
                        Try
                            EmployeeCodeMigration.UpdateDatabaseForEmployeeCode()
                        Catch
                        End Try
                        Try
                            SyncMarkerMigration.UpdateDatabaseForSyncMarker()
                        Catch
                        End Try
                        Try
                            TerminalMigration.EnsureTable()
                        Catch
                        End Try
                        Try
                            ReturnMigration.EnsureTables()
                        Catch
                        End Try
                        Try
                            SuperAdminSeeder.EnsureSuperAdmin()
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
            Using conn As New NpgsqlConnection(GetConnectionString())
                conn.Open()
                Return $"PostgreSQL: {conn.Host}:{conn.Port}/{conn.Database} | State: Connected"
            End Using
        Catch ex As Exception
            Return $"PostgreSQL: Disconnected | Error: {ex.Message}"
        End Try
    End Function
End Module