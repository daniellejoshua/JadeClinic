Imports System.Data.Common

Public Class SyncMarkerMigration
    ''' <summary>
    ''' Creates the SyncMarker table (a single-row change marker) and attaches
    ''' triggers to Products so any stock change - sale stock-out, inventory
    ''' IN/OUT/ADJUST, supplier restock, product edits - bumps the marker
    ''' atomically inside the writer's transaction. Pages poll the marker and
    ''' refresh their visible stock without a manual reload. Safe to call
    ''' repeatedly; runs automatically at startup.
    ''' </summary>
    Public Shared Sub UpdateDatabaseForSyncMarker()
        Try
            Console.WriteLine("Checking database for SyncMarker...")

            Dim connStr As String = Connection.GetConnectionString()
            Using conn As DbConnection = DbProvider.CreateConnection(connStr)
                conn.Open()

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TABLE IF NOT EXISTS SyncMarker (" &
                        "MarkerID INTEGER PRIMARY KEY CHECK (MarkerID = 1), " &
                        "LastChange TEXT NOT NULL DEFAULT '')"
                    cmd.ExecuteNonQuery()
                End Using

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = "INSERT OR IGNORE INTO SyncMarker (MarkerID, LastChange) VALUES (1, '')"
                    cmd.ExecuteNonQuery()
                End Using

                ' Bump the marker whenever product stock/set changes, so every PC
                ' sharing this database can detect remote stock changes cheaply.
                Dim triggerBody As String =
                    "INSERT OR IGNORE INTO SyncMarker (MarkerID, LastChange) VALUES (1, ''); " &
                    "UPDATE SyncMarker SET LastChange = strftime('%Y%m%d%H%M%f','now') WHERE MarkerID = 1;"

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TRIGGER IF NOT EXISTS trg_SyncMarker_Products_StockUpdate " &
                        "AFTER UPDATE OF CurrentStock ON Products " &
                        "BEGIN " & triggerBody & " END"
                    cmd.ExecuteNonQuery()
                End Using

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TRIGGER IF NOT EXISTS trg_SyncMarker_Products_Insert " &
                        "AFTER INSERT ON Products " &
                        "BEGIN " & triggerBody & " END"
                    cmd.ExecuteNonQuery()
                End Using

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TRIGGER IF NOT EXISTS trg_SyncMarker_Products_Delete " &
                        "AFTER DELETE ON Products " &
                        "BEGIN " & triggerBody & " END"
                    cmd.ExecuteNonQuery()
                End Using

                Console.WriteLine("SyncMarker migration complete.")
            End Using
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not update database for SyncMarker: {ex.Message}")
        End Try
    End Sub
End Class