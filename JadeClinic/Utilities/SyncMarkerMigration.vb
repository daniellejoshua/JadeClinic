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
                        "MarkerID BIGINT PRIMARY KEY CHECK (MarkerID = 1), " &
                        "LastChange TEXT NOT NULL DEFAULT '0')"
                    cmd.ExecuteNonQuery()
                End Using

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = "INSERT INTO SyncMarker (MarkerID, LastChange) VALUES (1, '0') ON CONFLICT (MarkerID) DO NOTHING"
                    cmd.ExecuteNonQuery()
                End Using

                ' One shared trigger function that bumps the monotonic counter.
                ' Updates the separate sync_marker table so PostgreSQL does not
                ' recurse into the original trigger event. NOTICE: the update fires
                ' on OTHER AFTER INSERT/UPDATE/DELETE triggers too; sync_marker has
                ' none, so the bump happens exactly once per row operation.
                Dim functionSql As String =
                    "CREATE OR REPLACE FUNCTION fn_sync_marker_bump() RETURNS trigger AS $$ " &
                    "BEGIN " &
                    "  INSERT INTO SyncMarker (MarkerID, LastChange) VALUES (1, COALESCE((SELECT LastChange FROM SyncMarker WHERE MarkerID = 1), '0')) ON CONFLICT (MarkerID) DO NOTHING; " &
                    "  UPDATE SyncMarker SET LastChange = (CAST(LastChange AS BIGINT) + 1)::TEXT WHERE MarkerID = 1; " &
                    "  RETURN NULL; " &
                    "END; $$ LANGUAGE plpgsql"
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = functionSql
                    cmd.ExecuteNonQuery()
                End Using

                ' Drop then recreate so the updated body takes effect on upgrade.
                Dim triggerNames As String() = {
                    "trg_SyncMarker_Products_StockUpdate",
                    "trg_SyncMarker_Products_Insert",
                    "trg_SyncMarker_Products_Delete"
                }
                For Each trig In triggerNames
                    Using cmd As DbCommand = conn.CreateCommand()
                        cmd.CommandText = $"DROP TRIGGER IF EXISTS {trig} ON Products"
                        cmd.ExecuteNonQuery()
                    End Using
                Next

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TRIGGER trg_SyncMarker_Products_StockUpdate " &
                        "AFTER UPDATE OF CurrentStock ON Products " &
                        "FOR EACH ROW EXECUTE FUNCTION fn_sync_marker_bump()"
                    cmd.ExecuteNonQuery()
                End Using

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TRIGGER trg_SyncMarker_Products_Insert " &
                        "AFTER INSERT ON Products " &
                        "FOR EACH ROW EXECUTE FUNCTION fn_sync_marker_bump()"
                    cmd.ExecuteNonQuery()
                End Using

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText =
                        "CREATE TRIGGER trg_SyncMarker_Products_Delete " &
                        "AFTER DELETE ON Products " &
                        "FOR EACH ROW EXECUTE FUNCTION fn_sync_marker_bump()"
                    cmd.ExecuteNonQuery()
                End Using

                Console.WriteLine("SyncMarker migration complete.")
            End Using
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not update database for SyncMarker: {ex.Message}")
        End Try
    End Sub
End Class