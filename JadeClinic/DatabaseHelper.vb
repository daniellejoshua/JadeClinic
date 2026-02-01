Imports Microsoft.Data.SqlClient
Imports System.Configuration
Imports System.IO
Imports System.Data

Public Class DatabaseHelper
    ' Get connection string (smart detection)
    Public Shared Function GetConnectionString() As String
        Return Connection.GetConnectionString()
    End Function

    ' Test connection to a server
    Public Shared Function TestConnection(Optional serverName As String = "localhost") As Boolean
        Return Connection.TestConnection(serverName)
    End Function

    ' Save connection string to config
    Public Shared Sub SaveConnectionString(serverName As String)
        Connection.SaveConnectionString(serverName)
    End Sub

    ' Execute non-query command (INSERT, UPDATE, DELETE)
    Public Shared Function ExecuteNonQuery(query As String, parameters As SqlParameter()) As Integer
        Try
            Using conn As New SqlConnection(GetConnectionString())
                Using cmd As New SqlCommand(query, conn)
                    If parameters IsNot Nothing Then
                        cmd.Parameters.AddRange(parameters)
                    End If
                    conn.Open()
                    Return cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Database error: {ex.Message}")
        End Try
    End Function

    ' Execute scalar command (COUNT, MAX, etc.)
    Public Shared Function ExecuteScalar(query As String, parameters As SqlParameter()) As Object
        Try
            Using conn As New SqlConnection(GetConnectionString())
                Using cmd As New SqlCommand(query, conn)
                    If parameters IsNot Nothing Then
                        cmd.Parameters.AddRange(parameters)
                    End If
                    conn.Open()
                    Return cmd.ExecuteScalar()
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Database error: {ex.Message}")
        End Try
    End Function

    ' Execute query and return DataTable
    Public Shared Function ExecuteQuery(query As String, parameters As SqlParameter()) As DataTable
        Try
            Using conn As New SqlConnection(GetConnectionString())
                Using cmd As New SqlCommand(query, conn)
                    If parameters IsNot Nothing Then
                        cmd.Parameters.AddRange(parameters)
                    End If
                    Using adapter As New SqlDataAdapter(cmd)
                        Dim dt As New DataTable()
                        adapter.Fill(dt)
                        Return dt
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Database error: {ex.Message}")
        End Try
    End Function

    ' Execute query and return SqlDataReader
    Public Shared Function ExecuteReader(query As String, parameters As SqlParameter()) As SqlDataReader
        Try
            Dim conn As New SqlConnection(GetConnectionString())
            Dim cmd As New SqlCommand(query, conn)
            If parameters IsNot Nothing Then
                cmd.Parameters.AddRange(parameters)
            End If
            conn.Open()
            Return cmd.ExecuteReader(CommandBehavior.CloseConnection)
        Catch ex As Exception
            Throw New Exception($"Database error: {ex.Message}")
        End Try
    End Function

    ' Check if database exists and create if not
    Public Shared Sub InitializeDatabase()
        Try
            Using conn As New SqlConnection(GetConnectionString())
                conn.Open()
                ' Database exists if we can connect
            End Using
        Catch ex As Exception
            ' Create database and tables
            CreateDatabaseStructure()
        End Try
    End Sub

    ' Create database structure
    Private Shared Sub CreateDatabaseStructure()
        Dim queries As String() = {
            "CREATE TABLE IF NOT EXISTS Categories (
                CategoryID INT IDENTITY(1,1) PRIMARY KEY,
                CategoryName NVARCHAR(100) NOT NULL UNIQUE,
                Description NVARCHAR(500),
                RequiresExpiry BIT DEFAULT 0,
                CreatedDate DATETIME DEFAULT GETDATE()
            );",
            "CREATE TABLE IF NOT EXISTS Products (
                ProductID INT IDENTITY(1,1) PRIMARY KEY,
                ProductCode NVARCHAR(50),
                Barcode NVARCHAR(100),
                ProductName NVARCHAR(200) NOT NULL,
                Category NVARCHAR(100),
                Unit NVARCHAR(50),
                CurrentStock INT DEFAULT 0,
                ReorderLevel INT DEFAULT 0,
                CostPrice DECIMAL(10,2) NOT NULL,
                SellingPrice DECIMAL(10,2) NOT NULL,
                WholesalePrice DECIMAL(10,2),
                Supplier NVARCHAR(200),
                HasExpiry BIT DEFAULT 0,
                ExpiryDate DATE,
                IsActive BIT DEFAULT 1,
                CreatedDate DATETIME DEFAULT GETDATE()
            );",
            "CREATE TABLE IF NOT EXISTS ProductImages (
                ImageID INT IDENTITY(1,1) PRIMARY KEY,
                ProductID INT NOT NULL,
                ImageData VARBINARY(MAX),
                ImageName NVARCHAR(255),
                CreatedDate DATETIME DEFAULT GETDATE()
            );"
        }

        For Each query In queries
            ExecuteNonQuery(query, Nothing)
        Next
    End Sub
End Class