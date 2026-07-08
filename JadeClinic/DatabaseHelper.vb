Imports Microsoft.Data.Sqlite
Imports System.Configuration
Imports System.IO
Imports System.Data
Imports System.Data.Common
Imports System.Collections.Generic

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
            Using conn As DbConnection = DbProvider.CreateConnection(GetConnectionString())
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = query
                    If parameters IsNot Nothing Then
                        Dim dbParams = DbProvider.ConvertSqlParameters(parameters)
                        cmd.Parameters.AddRange(dbParams)
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
            Using conn As DbConnection = DbProvider.CreateConnection(GetConnectionString())
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = query
                    If parameters IsNot Nothing Then
                        Dim dbParams = DbProvider.ConvertSqlParameters(parameters)
                        cmd.Parameters.AddRange(dbParams)
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
            Using conn As DbConnection = DbProvider.CreateConnection(GetConnectionString())
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = query
                    If parameters IsNot Nothing Then
                        Dim dbParams = DbProvider.ConvertSqlParameters(parameters)
                        cmd.Parameters.AddRange(dbParams)
                    End If
                    conn.Open()
                    Using reader As DbDataReader = cmd.ExecuteReader()
                        Dim dt As New DataTable()
                        dt.Load(reader)
                        Return dt
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Database error: {ex.Message}")
        End Try
    End Function

    ' Execute query and return DbDataReader
    Public Shared Function ExecuteReader(query As String, parameters As SqlParameter()) As DbDataReader
        Try
            Dim conn As DbConnection = DbProvider.CreateConnection(GetConnectionString())
            Dim cmd As DbCommand = conn.CreateCommand()
            cmd.CommandText = query
            If parameters IsNot Nothing Then
                Dim dbParams = DbProvider.ConvertSqlParameters(parameters)
                cmd.Parameters.AddRange(dbParams)
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
            Using conn As DbConnection = DbProvider.CreateConnection(GetConnectionString())
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
                CategoryID INTEGER PRIMARY KEY AUTOINCREMENT,
                CategoryName TEXT NOT NULL UNIQUE,
                Description TEXT,
                RequiresExpiry INTEGER DEFAULT 0,
                CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP
            );",
            "CREATE TABLE IF NOT EXISTS Products (
                ProductID INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductCode TEXT,
                ProductName TEXT NOT NULL,
                Category TEXT,
                Unit TEXT,
                CurrentStock INTEGER DEFAULT 0,
                ReorderLevel INTEGER DEFAULT 0,
                CostPrice REAL NOT NULL,
                SellingPrice REAL NOT NULL,
                WholesalePrice REAL,
                Supplier TEXT,
                HasExpiry INTEGER DEFAULT 0,
                ExpiryDate TEXT,
                IsActive INTEGER DEFAULT 1,
                CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP
            );",
            "CREATE TABLE IF NOT EXISTS ProductImages (
                ImageID INTEGER PRIMARY KEY AUTOINCREMENT,
                ProductID INTEGER NOT NULL,
                ImageData BLOB,
                ImageName TEXT,
                CreatedDate TEXT DEFAULT CURRENT_TIMESTAMP
            );"
        }

        For Each query In queries
            ExecuteNonQuery(query, Nothing)
        Next
    End Sub
End Class