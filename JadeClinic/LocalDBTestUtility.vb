Imports Microsoft.Data.Sqlite
Imports System.Configuration

Public Class LocalDBTestUtility
    
    Public Shared Sub RunAllTests()
        Console.WriteLine("=== LocalDB Development Test ===")
        Console.WriteLine()
        
        ' Test 1: Connection
        TestConnection()
        
        ' Test 2: Database Info
        ShowDatabaseInfo()
        
        ' Test 3: Initialize Database
        TestDatabaseInitialization()
        
        ' Test 4: Show Tables
        ShowTables()
        
        ' Test 5: Test Data Operations
        TestDataOperations()
        
        Console.WriteLine()
        Console.WriteLine("=== Test Complete ===")
    End Sub
    
    Private Shared Sub TestConnection()
        Console.WriteLine("?? Testing LocalDB Connection...")
        
        Try
            If Connection.TestConnection() Then
                Console.WriteLine("? Connection successful!")
                Console.WriteLine($"   Environment: {Connection.GetEnvironmentName()}")
                Console.WriteLine($"   Info: {Connection.GetDatabaseInfo()}")
            Else
                Console.WriteLine("? Connection failed!")
            End If
        Catch ex As Exception
            Console.WriteLine($"? Connection error: {ex.Message}")
        End Try
        
        Console.WriteLine()
    End Sub
    
    Private Shared Sub ShowDatabaseInfo()
        Console.WriteLine("?? Database Information...")
        
        Try
            Using conn As New SqliteConnection(Connection.GetConnectionString())
                conn.Open()
                
                ' Get database name and server
                Console.WriteLine($"   Database: {conn.Database}")
                Console.WriteLine($"   Server: {conn.DataSource}")
                Console.WriteLine($"   Connection String: {Connection.GetConnectionString()}")
                
                ' Get SQL Server version
                Using cmd As New SqliteCommand("SELECT @@VERSION", conn)
                    Dim version As String = cmd.ExecuteScalar().ToString()
                    Console.WriteLine($"   Version: {version.Split(vbCrLf)(0)}")
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"? Error getting database info: {ex.Message}")
        End Try
        
        Console.WriteLine()
    End Sub
    
    Private Shared Sub TestDatabaseInitialization()
        Console.WriteLine("??? Testing Database Initialization...")
        
        Try
            If Connection.InitializeDatabase() Then
                Console.WriteLine("? Database initialization successful!")
            Else
                Console.WriteLine("? Database initialization failed!")
            End If
        Catch ex As Exception
            Console.WriteLine($"? Initialization error: {ex.Message}")
        End Try
        
        Console.WriteLine()
    End Sub
    
    Private Shared Sub ShowTables()
        Console.WriteLine("?? Database Tables...")
        
        Try
            Dim query As String = "
            SELECT TABLE_NAME, TABLE_TYPE 
            FROM INFORMATION_SCHEMA.TABLES 
            WHERE TABLE_TYPE = 'BASE TABLE'
            ORDER BY TABLE_NAME"
            
            Dim dt As DataTable = DatabaseHelper.ExecuteQuery(query, Nothing)
            
            If dt.Rows.Count > 0 Then
                Console.WriteLine("   Tables found:")
                For Each row As DataRow In dt.Rows
                    Console.WriteLine($"   ? {row("TABLE_NAME")}")
                Next
            Else
                Console.WriteLine("   No tables found - run database initialization first")
            End If
            
        Catch ex As Exception
            Console.WriteLine($"? Error listing tables: {ex.Message}")
        End Try
        
        Console.WriteLine()
    End Sub
    
    Private Shared Sub TestDataOperations()
        Console.WriteLine("?? Testing Basic Data Operations...")
        
        Try
            ' Test if Users table exists and has data
            Dim userCountQuery As String = "SELECT COUNT(*) FROM Users"
            Dim userCount As Integer = CInt(DatabaseHelper.ExecuteScalar(userCountQuery, Nothing))
            Console.WriteLine($"   Users in database: {userCount}")
            
            ' Test if Categories table exists and has data  
            Dim categoryCountQuery As String = "SELECT COUNT(*) FROM Categories"
            Dim categoryCount As Integer = CInt(DatabaseHelper.ExecuteScalar(categoryCountQuery, Nothing))
            Console.WriteLine($"   Categories in database: {categoryCount}")
            
            ' Test if Settings table exists and has data
            Dim settingsCountQuery As String = "SELECT COUNT(*) FROM Settings"
            Dim settingsCount As Integer = CInt(DatabaseHelper.ExecuteScalar(settingsCountQuery, Nothing))
            Console.WriteLine($"   Settings in database: {settingsCount}")
            
            Console.WriteLine("? Basic data operations successful!")
            
        Catch ex As Exception
            Console.WriteLine($"? Error in data operations: {ex.Message}")
        End Try
        
        Console.WriteLine()
    End Sub
    
    ' Quick method to get connection info for SSMS
    Public Shared Sub ShowSSMSConnectionInfo()
        Console.WriteLine("=== SSMS Connection Info ===")
        Console.WriteLine("Server Name: (localdb)\MSSQLLocalDB")
        Console.WriteLine("Authentication: Windows Authentication")
        Console.WriteLine("Database: JadeDentalSupply")
        Console.WriteLine()
        Console.WriteLine("After connecting, refresh to see your database!")
        Console.WriteLine("================================")
    End Sub
End Class