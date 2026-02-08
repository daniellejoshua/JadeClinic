Imports Microsoft.Data.SqlClient
Imports System.Configuration
Imports System.IO

Module Connection

    ' Get connection string - simplified for LocalDB everywhere
    Public Function GetConnectionString() As String
        Try
            ' Always use pure LocalDB connection during development/debugging
            ' This avoids file path issues in Debug folder
            Return GetLocalDBConnectionString()
        Catch ex As Exception
            Console.WriteLine($"Error getting connection string: {ex.Message}")
            Return GetLocalDBConnectionString() ' Fallback
        End Try
    End Function

    ' Get LocalDB connection string (no file attachment - pure LocalDB)
    Public Function GetLocalDBConnectionString() As String
        Return "Server=(localdb)\MSSQLLocalDB;Database=JadeDentalSupply;Integrated Security=true;TrustServerCertificate=True;Persist Security Info=False;"
    End Function

    ' Get LocalDB connection string with file attachment (for production deployment)
    Public Function GetLocalDBWithFileConnectionString() As String
        Dim dataDirectory As String = Path.Combine(Application.StartupPath, "App_Data")
        If Not Directory.Exists(dataDirectory) Then
            Directory.CreateDirectory(dataDirectory)
        End If
        
        Dim dbPath As String = Path.Combine(dataDirectory, "JadeDentalSupply.mdf")
        Return $"Server=(localdb)\MSSQLLocalDB;Database=JadeDentalSupply;Integrated Security=true;AttachDbFilename={dbPath};TrustServerCertificate=True;"
    End Function

    ' Test connection
    Public Function TestConnection(Optional serverName As String = "") As Boolean
        Try
            Dim connStr As String = GetConnectionString()
            Console.WriteLine($"Testing connection with: {connStr}")

            Using conn As New SqlConnection(connStr)
                conn.Open()
                Console.WriteLine($"✅ Connected to: {conn.Database} on {conn.DataSource}")
                Return True
            End Using
        Catch ex As Exception
            Console.WriteLine($"❌ Connection test failed: {ex.Message}")
            
            ' Try to create database if it doesn't exist
            Try
                Console.WriteLine("Attempting to create database...")
                Return CreateDatabaseIfNotExists()
            Catch createEx As Exception
                Console.WriteLine($"❌ Database creation failed: {createEx.Message}")
                Return False
            End Try
        End Try
    End Function
    
    ' Create database if it doesn't exist
    Public Function CreateDatabaseIfNotExists() As Boolean
        Try
            ' Connect to master database first to create our database
            Dim masterConnStr As String = "Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=true;TrustServerCertificate=True;"
            
            Using conn As New SqlConnection(masterConnStr)
                conn.Open()
                
                ' Check if database exists
                Dim checkQuery As String = "SELECT COUNT(*) FROM sys.databases WHERE name = 'JadeDentalSupply'"
                Using cmd As New SqlCommand(checkQuery, conn)
                    Dim count As Integer = CInt(cmd.ExecuteScalar())
                    
                    If count = 0 Then
                        ' Create database
                        Console.WriteLine("Creating JadeDentalSupply database...")
                        Dim createQuery As String = "CREATE DATABASE JadeDentalSupply"
                        Using createCmd As New SqlCommand(createQuery, conn)
                            createCmd.ExecuteNonQuery()
                        End Using
                        Console.WriteLine("✅ Database created successfully!")
                    Else
                        Console.WriteLine("Database already exists.")
                    End If
                End Using
            End Using
            
            ' Now test connection to our database
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()
                Console.WriteLine("✅ Connected to JadeDentalSupply database!")
                Return True
            End Using
            
        Catch ex As Exception
            Console.WriteLine($"❌ Error creating database: {ex.Message}")
            Return False
        End Try
    End Function

    ' Save connection string
    Public Sub SaveConnectionString(Optional serverName As String = "")
        Try
            Dim config As Configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)
            
            ' Always use LocalDB
            config.ConnectionStrings.ConnectionStrings("JadeDentalConnection").ConnectionString = GetLocalDBConnectionString()
            
            ' Mark as no longer first run
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

    ' Build connection string - always LocalDB now
    Public Function BuildConnectionString(Optional server As String = "") As String
        Return GetLocalDBConnectionString()
    End Function
    
    ' Initialize database if it doesn't exist
    Public Function InitializeDatabase() As Boolean
        Try
            ' First ensure database exists
            If Not CreateDatabaseIfNotExists() Then
                Return False
            End If
            
            Dim connStr As String = GetConnectionString()
            
            Using conn As New SqlConnection(connStr)
                conn.Open()
                
                ' Check if tables exist, if not create them
                Dim checkTablesQuery As String = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'"
                Using cmd As New SqlCommand(checkTablesQuery, conn)
                    Dim tableCount As Integer = CInt(cmd.ExecuteScalar())
                    
                    If tableCount = 0 Then
                        Console.WriteLine("No tables found. Creating database schema...")
                        DatabaseInitializer.CreateDatabaseSchema()
                        Console.WriteLine("✅ Database schema initialized successfully!")
                    Else
                        Console.WriteLine($"Database already has {tableCount} tables.")
                    End If
                End Using
                
                Return True
            End Using
        Catch ex As Exception
            Console.WriteLine($"❌ Database initialization failed: {ex.Message}")
            Return False
        End Try
    End Function
    
    ' Get current environment name
    Public Function GetEnvironmentName() As String
        Dim appMode As String = ConfigurationManager.AppSettings("ApplicationMode")
        Return If(String.IsNullOrEmpty(appMode), "Development", appMode)
    End Function
    
    ' Check if running in development mode
    Public Function IsDevelopmentMode() As Boolean
        Return True ' Always development mode for now since we're using LocalDB everywhere
    End Function
    
    ' Get database info for debugging
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
