Imports System.Data.Common

Public Class EmployeeCodeMigration
    ''' <summary>
    ''' Migrates existing databases so the Users table has the EmployeeCode column.
    ''' Adds the column if missing and backfills a numbers-only employee code
    ''' (UserID + registration date/time) for any row that doesn't have one yet.
    ''' Safe to call repeatedly; runs automatically at startup.
    ''' </summary>
    Public Shared Sub UpdateDatabaseForEmployeeCode()
        Try
            Console.WriteLine("Checking database for EmployeeCode column...")

            Dim connStr As String = Connection.GetConnectionString()
            Using conn As DbConnection = DbProvider.CreateConnection(connStr)
                conn.Open()

                Dim hasColumn As Boolean = False
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = "SELECT name FROM pragma_table_info('Users')"
                    Using reader As DbDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            If String.Equals(Convert.ToString(reader("name")), "EmployeeCode", StringComparison.OrdinalIgnoreCase) Then
                                hasColumn = True
                                Exit While
                            End If
                        End While
                    End Using
                End Using

                If Not hasColumn Then
                    Console.WriteLine("Adding EmployeeCode column to Users table...")
                    Using cmd As DbCommand = conn.CreateCommand()
                        cmd.CommandText = "ALTER TABLE Users ADD COLUMN EmployeeCode TEXT NULL"
                        cmd.ExecuteNonQuery()
                    End Using
                End If

                ' Backfill any rows still missing an employee code.
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText =
                        "UPDATE Users SET EmployeeCode = printf('%d%s', UserID, strftime('%Y%m%d%H%M%S', CreatedAt)) " &
                        "WHERE EmployeeCode IS NULL OR Trim(EmployeeCode) = ''"
                    cmd.ExecuteNonQuery()
                End Using

                Console.WriteLine("EmployeeCode migration complete.")
            End Using
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not update database for EmployeeCode: {ex.Message}")
        End Try
    End Sub
End Class
