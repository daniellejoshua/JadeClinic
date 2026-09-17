Imports System.Data.Common
Imports Npgsql

Public Class DbProvider

    Public Shared Function CreateConnection(connString As String) As DbConnection
        Return New NpgsqlConnection(connString)
    End Function

    Public Shared Function ConvertNpgsqlParameters(sqlParams() As NpgsqlParameter) As DbParameter()
        If sqlParams Is Nothing Then Return Nothing
        Dim list As New List(Of DbParameter)()
        For Each p In sqlParams
            Dim name As String = p.ParameterName
            If Not name.StartsWith("@") Then name = "@" & name
            Dim value As Object = If(p.Value Is Nothing, DBNull.Value, p.Value)
            Dim param As New NpgsqlParameter(name, value)
            Try
                If p.NpgsqlDbType <> 0 Then
                    param.NpgsqlDbType = p.NpgsqlDbType
                End If
            Catch
            End Try
            list.Add(param)
        Next
        Return list.ToArray()
    End Function

    Public Shared Function LastInsertRowId(conn As DbConnection, Optional tableName As String = "", Optional pkName As String = "") As Long
        Try
            Dim pgConn = TryCast(conn, NpgsqlConnection)
            If pgConn IsNot Nothing Then
                If Not String.IsNullOrWhiteSpace(tableName) AndAlso Not String.IsNullOrWhiteSpace(pkName) Then
                    Using cmd = pgConn.CreateCommand()
                        cmd.CommandText = "SELECT currval(pg_get_serial_sequence('" & tableName & "', '" & pkName & "'))"
                        Dim result = cmd.ExecuteScalar()
                        If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                            Return Convert.ToInt64(result)
                        End If
                    End Using
                Else
                    ' lastval() returns the most recent sequence value retrieved via nextval()
                    ' on the current connection (single-table bigserial inserts).
                    Using cmd = pgConn.CreateCommand()
                        cmd.CommandText = "SELECT lastval();"
                        Dim result = cmd.ExecuteScalar()
                        If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                            Return Convert.ToInt64(result)
                        End If
                    End Using
                End If
            End If
        Catch
        End Try
        Return -1
    End Function
End Class