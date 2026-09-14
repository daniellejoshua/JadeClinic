Imports System.Data.Common

' Ensures the shared Terminals table exists. Used in multi-PC (LAN shared DB)
' setups so the admin can name each connected computer and control its access.
' Safe to call repeatedly; runs automatically at startup.
'
' Access is tracked with a Status column ('pending' | 'approved' | 'blocked').
' New computers start 'pending' and must be approved by the admin before the
' app will open on them. The migration also upgrades older databases that only
' had the IsActive column.
Public Class TerminalMigration

    Public Shared Sub EnsureTable()
        Try
            Dim sql As String =
                "CREATE TABLE IF NOT EXISTS Terminals (" &
                "TerminalID TEXT PRIMARY KEY, " &
                "TerminalName TEXT NULL, " &
                "HostName TEXT NULL, " &
                "IsActive INTEGER NOT NULL DEFAULT 0, " &
                "Status TEXT NOT NULL DEFAULT 'pending', " &
                "LastSeenAt DATETIME NULL, " &
                "CreatedAt DATETIME NOT NULL DEFAULT (CURRENT_TIMESTAMP))"

            Dim connStr As String = Connection.GetConnectionString()
            Using conn As DbConnection = DbProvider.CreateConnection(connStr)
                conn.Open()
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = sql
                    cmd.ExecuteNonQuery()
                End Using

                ' On databases created before the Status column existed, add it
                ' now and derive an initial value from each row's IsActive flag.
                Dim hasStatus As Boolean = False
                Using checkCmd As DbCommand = conn.CreateCommand()
                    checkCmd.CommandText = "PRAGMA table_info(Terminals)"
                    Using rdr As DbDataReader = checkCmd.ExecuteReader()
                        While rdr.Read()
                            If String.Equals(Convert.ToString(rdr("name")), "Status", StringComparison.OrdinalIgnoreCase) Then
                                hasStatus = True
                            End If
                        End While
                    End Using
                End Using

                If Not hasStatus Then
                    Using alterCmd As DbCommand = conn.CreateCommand()
                        alterCmd.CommandText = "ALTER TABLE Terminals ADD COLUMN Status TEXT NOT NULL DEFAULT 'pending'"
                        alterCmd.ExecuteNonQuery()
                    End Using
                    Using migrateCmd As DbCommand = conn.CreateCommand()
                        migrateCmd.CommandText =
                            "UPDATE Terminals SET Status = CASE WHEN IsActive = 1 THEN 'approved' ELSE 'blocked' END"
                        migrateCmd.ExecuteNonQuery()
                    End Using
                End If
            End Using
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not create/migrate Terminals table: {ex.Message}")
        End Try
    End Sub

End Class