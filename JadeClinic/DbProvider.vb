Imports System.Data.Common
Imports Microsoft.Data.Sqlite
Public Class DbProvider

    Public Shared Function CreateConnection(connString As String) As DbConnection
        Return New SqliteConnection(connString)
    End Function

    Public Shared Function ConvertSqlParameters(sqlParams() As SqlParameter) As DbParameter()
        If sqlParams Is Nothing Then Return Nothing
        Dim list As New List(Of DbParameter)()
        For Each p In sqlParams
            Dim name As String = p.ParameterName
            If Not name.StartsWith("@") Then name = "@" & name
            Dim value As Object = If(p.Value Is Nothing, DBNull.Value, p.Value)
            Dim param As New SqliteParameter(name, value)
            Try
                If p.DbType <> Nothing Then
                    param.DbType = p.DbType
                End If
            Catch
            End Try
            list.Add(param)
        Next
        Return list.ToArray()
    End Function

    Public Shared Function LastInsertRowId(conn As DbConnection) As Long
        Try
            Dim sqliteConn = TryCast(conn, SqliteConnection)
            If sqliteConn IsNot Nothing Then
                Using cmd = sqliteConn.CreateCommand()
                    cmd.CommandText = "SELECT last_insert_rowid();"
                    Dim result = cmd.ExecuteScalar()
                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        Return Convert.ToInt64(result)
                    End If
                End Using
            End If
        Catch
        End Try
        Return -1
    End Function
End Class