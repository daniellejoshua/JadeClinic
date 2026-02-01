Imports Microsoft.Data.SqlClient
Imports System.Configuration

Module Connection

    ' Get connection string (smart detection)
    Public Function GetConnectionString() As String
        Dim connStr As String = ConfigurationManager.ConnectionStrings("JadeDentalConnection").ConnectionString

        If String.IsNullOrEmpty(connStr) Then
            ' First run - use localhost for admin PC
            Return "Server=localhost\SQLEXPRESS01;Database=JadeDentalSupply;Trusted_Connection=True;TrustServerCertificate=True;"
        End If

        Return connStr
    End Function

    ' Test connection to a server
    Public Function TestConnection(Optional serverName As String = "localhost") As Boolean
        Try
            Dim connStr As String

            If serverName = "localhost" OrElse serverName = "." Then
                connStr = "Server=localhost\SQLEXPRESS01;Database=JadeDentalSupply;Trusted_Connection=True;TrustServerCertificate=True;"
            Else
                connStr = $"Server={serverName}\SQLEXPRESS01;Database=JadeDentalSupply;Trusted_Connection=True;TrustServerCertificate=True;"
            End If

            Using conn As New SqlConnection(connStr)
                conn.Open()
                Return True
            End Using
        Catch ex As Exception
            Return False
        End Try
    End Function

    ' Save connection string to config
    Public Sub SaveConnectionString(serverName As String)
        Dim config As Configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None)

        Dim connStr As String
        If serverName = "localhost" OrElse serverName = "." Then
            connStr = "Server=localhost\SQLEXPRESS01;Database=JadeDentalSupply;Trusted_Connection=True;TrustServerCertificate=True;"
        Else
            connStr = $"Server={serverName}\SQLEXPRESS01;Database=JadeDentalSupply;Trusted_Connection=True;TrustServerCertificate=True;"
        End If

        config.ConnectionStrings.ConnectionStrings("JadeDentalConnection").ConnectionString = connStr
        config.Save(ConfigurationSaveMode.Modified)
        ConfigurationManager.RefreshSection("connectionStrings")
    End Sub

    ' Build connection string for a given server
    Public Function BuildConnectionString(server As String) As String
        If server = "localhost" Or server = "." Then
            Return "Server=localhost\SQLEXPRESS01;Database=JadeDentalSupply;Trusted_Connection=True;TrustServerCertificate=True;"
        Else
            Return $"Server={server}\SQLEXPRESS01;Database=JadeDentalSupply;Trusted_Connection=True;TrustServerCertificate=True;"
        End If
    End Function
End Module
