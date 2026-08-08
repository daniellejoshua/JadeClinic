Imports System.IO
Imports System.Data
Imports Microsoft.Data.Sqlite

' Local SQLite log of sync / backup / restore / seed events.
' Source of truth for the Sync Logs page. The cloud sync_log table is kept
' in sync too, but it is NOT required to view history (works offline).
Public Module LocalSyncLog

    Public Const MaxRows As Integer = 500

    Private Function Conn() As SqliteConnection
        Return New SqliteConnection(Connection.GetConnectionString())
    End Function

    ' Called once at startup: creates the table (existing DBs included), marks
    ' any leftover 'running' rows as failed, and trims to MaxRows.
    Public Sub Initialize()
        Try
            EnsureTable()
            MarkStaleRunningAsFailed()
            TrimRows()
        Catch ex As Exception
            Console.WriteLine($"LocalSyncLog.Initialize error: {ex.Message}")
        End Try
    End Sub

    Public Sub EnsureTable()
        Dim sql As String =
            "CREATE TABLE IF NOT EXISTS SyncLog (" &
            "LogID INTEGER PRIMARY KEY AUTOINCREMENT, " &
            "StartedAt DATETIME NOT NULL, " &
            "CompletedAt DATETIME, " &
            "RowsSynced INTEGER NOT NULL DEFAULT 0, " &
            "Status TEXT NOT NULL DEFAULT 'running', " &
            "Error TEXT, " &
            "Mode TEXT, " &
            "ReconcileDeleted INTEGER NOT NULL DEFAULT 0, " &
            "SnapshotLocal INTEGER NOT NULL DEFAULT 0, " &
            "SnapshotCloud INTEGER NOT NULL DEFAULT 0, " &
            "SnapshotSizeBytes INTEGER NOT NULL DEFAULT 0, " &
            "EventType TEXT NOT NULL DEFAULT 'sync', " &
            "Source TEXT)"

        Using c As SqliteConnection = Conn()
            c.Open()
            Using cmd As New SqliteCommand(sql, c)
                cmd.ExecuteNonQuery()
            End Using
            Try
                Using cmd As New SqliteCommand("CREATE INDEX IF NOT EXISTS IX_SyncLog_Started ON SyncLog (StartedAt DESC)", c)
                    cmd.ExecuteNonQuery()
                End Using
            Catch
            End Try
        End Using
    End Sub

    ' Begin an event and return its LogID. Any stale 'running' rows from a
    ' crashed session are marked failed before the new event is written.
    Public Function StartEvent(eventType As String, source As String,
                               Optional mode As String = "",
                               Optional reconcile As Boolean = False) As Integer
        EnsureTable()
        MarkStaleRunningAsFailed()
        Using c As SqliteConnection = Conn()
            c.Open()
            Using cmd As New SqliteCommand(
                "INSERT INTO SyncLog (StartedAt, Status, Mode, EventType, Source, ReconcileDeleted) " &
                "VALUES (@t, 'running', @mode, @et, @src, @rec); SELECT last_insert_rowid()", c)
                cmd.Parameters.AddWithValue("@t", DateTime.Now)
                cmd.Parameters.AddWithValue("@mode", If(String.IsNullOrEmpty(mode), CObj(DBNull.Value), CObj(mode)))
                cmd.Parameters.AddWithValue("@et", eventType)
                cmd.Parameters.AddWithValue("@src", If(String.IsNullOrEmpty(source), CObj(DBNull.Value), CObj(source)))
                cmd.Parameters.AddWithValue("@rec", If(reconcile, 1, 0))
                Return Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        End Using
    End Function

    Public Sub CompleteEvent(logId As Integer, status As String, rows As Integer,
                             Optional err As String = "",
                             Optional snapshotLocal As Boolean = False,
                             Optional snapshotCloud As Boolean = False,
                             Optional snapshotSizeBytes As Long = 0,
                             Optional reconcileDeleted As Integer = 0)
        If logId <= 0 Then Return
        Try
            Using c As SqliteConnection = Conn()
                c.Open()
                Using cmd As New SqliteCommand(
                    "UPDATE SyncLog SET CompletedAt = @done, Status = @st, RowsSynced = @rows, Error = @err, " &
                    "SnapshotLocal = @sl, SnapshotCloud = @sc, SnapshotSizeBytes = @sz, ReconcileDeleted = @rd " &
                    "WHERE LogID = @id", c)
                    cmd.Parameters.AddWithValue("@done", DateTime.Now)
                    cmd.Parameters.AddWithValue("@st", status)
                    cmd.Parameters.AddWithValue("@rows", rows)
                    cmd.Parameters.AddWithValue("@err", If(String.IsNullOrEmpty(err), CObj(DBNull.Value), CObj(err)))
                    cmd.Parameters.AddWithValue("@sl", If(snapshotLocal, 1, 0))
                    cmd.Parameters.AddWithValue("@sc", If(snapshotCloud, 1, 0))
                    cmd.Parameters.AddWithValue("@sz", snapshotSizeBytes)
                    cmd.Parameters.AddWithValue("@rd", reconcileDeleted)
                    cmd.Parameters.AddWithValue("@id", logId)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"LocalSyncLog.CompleteEvent error: {ex.Message}")
        End Try
    End Sub

    ' Any event still 'running' was interrupted (app closed/crashed mid-run).
    Public Sub MarkStaleRunningAsFailed()
        Try
            Using c As SqliteConnection = Conn()
                c.Open()
                Using cmd As New SqliteCommand(
                    "UPDATE SyncLog SET CompletedAt = @done, Status = 'failed', " &
                    "Error = 'Interrupted (application closed before completion)' WHERE Status = 'running'", c)
                    cmd.Parameters.AddWithValue("@done", DateTime.Now)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"LocalSyncLog stale fix error: {ex.Message}")
        End Try
    End Sub

    Public Sub TrimRows()
        Try
            Using c As SqliteConnection = Conn()
                c.Open()
                Using cmd As New SqliteCommand(
                    "DELETE FROM SyncLog WHERE LogID NOT IN " &
                    "(SELECT LogID FROM SyncLog ORDER BY LogID DESC LIMIT " & MaxRows & ")", c)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"LocalSyncLog trim error: {ex.Message}")
        End Try
    End Sub

    Public Function GetEvents(Optional limit As Integer = MaxRows) As DataTable
        Dim dt As New DataTable()
        dt.Columns.Add("LogID", GetType(Integer))
        dt.Columns.Add("Event", GetType(String))
        dt.Columns.Add("Source", GetType(String))
        dt.Columns.Add("Mode", GetType(String))
        dt.Columns.Add("Started", GetType(String))
        dt.Columns.Add("Duration", GetType(String))
        dt.Columns.Add("Rows", GetType(Integer))
        dt.Columns.Add("Deleted", GetType(Integer))
        dt.Columns.Add("Snap Local", GetType(String))
        dt.Columns.Add("Snap Cloud", GetType(String))
        dt.Columns.Add("Snap Size", GetType(String))
        dt.Columns.Add("Status", GetType(String))
        dt.Columns.Add("Error", GetType(String))

        Try
            Using c As SqliteConnection = Conn()
                c.Open()
                Using cmd As New SqliteCommand(
                    "SELECT LogID, EventType, Source, Mode, StartedAt, CompletedAt, RowsSynced, Status, Error, " &
                    "ReconcileDeleted, SnapshotLocal, SnapshotCloud, SnapshotSizeBytes " &
                    "FROM SyncLog ORDER BY LogID DESC LIMIT @lim", c)
                    cmd.Parameters.AddWithValue("@lim", limit)
                    Using rdr As SqliteDataReader = cmd.ExecuteReader()
                        While rdr.Read()
                            Dim row As DataRow = dt.NewRow()
                            row("LogID") = Convert.ToInt32(rdr("LogID"))
                            row("Event") = If(IsDBNull(rdr("EventType")), "", rdr("EventType").ToString())
                            row("Source") = If(IsDBNull(rdr("Source")), "", rdr("Source").ToString())
                            row("Mode") = If(IsDBNull(rdr("Mode")), "", rdr("Mode").ToString())

                            Dim started As DateTime = Convert.ToDateTime(rdr("StartedAt"))
                            row("Started") = started.ToString("MMM dd, yyyy HH:mm:ss")

                            Dim durText As String = ""
                            If Not IsDBNull(rdr("CompletedAt")) Then
                                Dim done As DateTime = Convert.ToDateTime(rdr("CompletedAt"))
                                Dim dur As TimeSpan = done - started
                                durText = If(dur.TotalSeconds < 1, "<1s", $"{dur.TotalSeconds.ToString("0.0")}s")
                            End If
                            row("Duration") = durText

                            row("Rows") = Convert.ToInt32(rdr("RowsSynced"))
                            row("Deleted") = Convert.ToInt32(rdr("ReconcileDeleted"))
                            row("Snap Local") = If(Convert.ToInt32(rdr("SnapshotLocal")) <> 0, "Yes", "-")
                            row("Snap Cloud") = If(Convert.ToInt32(rdr("SnapshotCloud")) <> 0, "Yes", "-")
                            Dim szBytes As Long = Convert.ToInt64(rdr("SnapshotSizeBytes"))
                            row("Snap Size") = If(szBytes > 0, FormatSize(szBytes), "-")
                            row("Status") = If(IsDBNull(rdr("Status")), "", rdr("Status").ToString())
                            row("Error") = If(IsDBNull(rdr("Error")), "", rdr("Error").ToString())
                            dt.Rows.Add(row)
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Dim row As DataRow = dt.NewRow()
            row("Event") = "Error"
            row("Status") = "failed"
            row("Error") = ex.Message
            dt.Rows.Add(row)
        End Try
        Return dt
    End Function

    Private Function FormatSize(bytes As Long) As String
        If bytes >= 1024 * 1024 Then Return $"{bytes / 1024.0 / 1024.0:0.0} MB"
        If bytes >= 1024 Then Return $"{bytes / 1024.0:0.0} KB"
        Return $"{bytes} B"
    End Function

End Module
