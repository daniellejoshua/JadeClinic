Imports Microsoft.Data.SqlClient
Imports System.Configuration

Module Connection

    Public Function GetConnectionString() As String
        Dim conn = ConfigurationManager.ConnectionStrings("JadeDentalConnection")?.ConnectionString
        If String.IsNullOrWhiteSpace(conn) Then
            Throw New InvalidOperationException("Connection string 'JadeDentalConnection' is missing.")
        End If

        Return conn
    End Function

    Public Function TestConnection(Optional serverName As String = "") As Boolean
        Try
            Dim connStr As String = If(String.IsNullOrWhiteSpace(serverName), GetConnectionString(), BuildConnectionString(serverName))

            Using conn As New SqlConnection(connStr)
                conn.Open()
                Console.WriteLine($"✅ Connected to: {conn.Database} on {conn.DataSource}")
                Return True
            End Using
        Catch ex As Exception
            Console.WriteLine($"❌ Connection test failed: {ex.Message}")
            Return False
        End Try
    End Function

    Public Function CreateDatabaseIfNotExists() As Boolean
        ' Production LAN mode: database should already exist on server.
        Return TestConnection()
    End Function

    Public Sub SaveConnectionString(Optional serverName As String = "")
        Try
            Dim config As Configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)

            If Not String.IsNullOrWhiteSpace(serverName) Then
                config.ConnectionStrings.ConnectionStrings("JadeDentalConnection").ConnectionString = BuildConnectionString(serverName)
            End If

            If config.AppSettings.Settings("IsFirstRun") IsNot Nothing Then
                config.AppSettings.Settings("IsFirstRun").Value = "false"
            End If

            config.Save(ConfigurationSaveMode.Modified)
            ConfigurationManager.RefreshSection("connectionStrings")
            ConfigurationManager.RefreshSection("appSettings")

            Console.WriteLine("✅ Connection string saved successfully!")
        Catch ex As Exception
            Console.WriteLine($"❌ Error saving connection string: {ex.Message}")
        End Try
    End Sub

    Public Function BuildConnectionString(Optional server As String = "") As String
        Dim builder As New SqlConnectionStringBuilder(GetConnectionString())

        If Not String.IsNullOrWhiteSpace(server) Then
            builder.DataSource = server
        End If

        Return builder.ConnectionString
    End Function

    Public Function InitializeDatabase() As Boolean
        Try
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()

                Dim checkTablesQuery As String = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
                Using cmd As New SqlCommand(checkTablesQuery, conn)
                    Dim tableCount As Integer = CInt(cmd.ExecuteScalar())

                    If tableCount = 0 Then
                        Console.WriteLine("No tables found. Creating database schema...")
                        DatabaseInitializer.CreateDatabaseSchema()
                        Console.WriteLine("✅ Database schema initialized successfully!")
                    End If
                End Using
            End Using

            Return True
        Catch ex As Exception
            Console.WriteLine($"❌ Database initialization failed: {ex.Message}")
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
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()
                Return $"Database: {conn.Database} | Server: {conn.DataSource} | State: Connected"
            End Using
        Catch ex As Exception
            Return $"Database: Disconnected | Error: {ex.Message}"
        End Try
    End Function
End Module
