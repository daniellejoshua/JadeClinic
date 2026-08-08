Imports System.IO
Imports System.IO.Compression
Imports System.Linq
Imports Microsoft.Data.Sqlite

' Automatic database backups:
'   - Rolling snapshots on every sync (FIFO-keep 5, local + cloud gzip).
'   - Monthly permanent .bak on the first sync of each calendar month.
'   - Forced permanent .bak before restore/seed so real data is never lost.
' All uploads are gzipped and guarded against the Supabase free-plan 50 MB
' per-file cap (~45 MB guard). Failures are logged and never throw.
Public Module DatabaseSnapshot

    Public Const SnapshotKeep As Integer = 5
    Public Const MaxUploadBytes As Long = 45L * 1024 * 1024

    Private Const SnapshotPrefix As String = "snap_"
    Private Const CloudSnapshotPrefix As String = "backups/snapshots/"
    Private Const CloudBackupPrefix As String = "backups/manual/"

    Public Structure SnapshotOutcome
        Public CreatedLocal As Boolean
        Public UploadedCloud As Boolean
        Public SizeBytes As Long
        Public FileName As String
    End Structure

    Public Structure BackupOutcome
        Public CreatedLocal As Boolean
        Public UploadedCloud As Boolean
        Public FilePath As String
        Public FileName As String
        Public Skipped As Boolean
    End Structure

    Public Function GetDbPath() As String
        Dim builder As New SqliteConnectionStringBuilder(Connection.GetConnectionString())
        Return builder.DataSource
    End Function

    Private Function GetBackupRoot() As String
        Return Path.Combine(Connection.GetDatabaseFolder(), "Backups")
    End Function

    Private Function GetSnapshotDir() As String
        Dim d As String = Path.Combine(GetBackupRoot(), "snapshots")
        If Not Directory.Exists(d) Then Directory.CreateDirectory(d)
        Return d
    End Function

    Private Function GetManualDir() As String
        Dim d As String = Path.Combine(GetBackupRoot(), "manual")
        If Not Directory.Exists(d) Then Directory.CreateDirectory(d)
        Return d
    End Function

    ' Rolling snapshot of the current database. Throttled to once per 10
    ' minutes and skipped when the database is unchanged since the last
    ' snapshot (force:=True bypasses both). Best-effort - never throws.
    Public Function EnsureRollingSnapshot(Optional force As Boolean = False) As SnapshotOutcome
        Dim outcome As New SnapshotOutcome()
        Try
            Dim dir As String = GetSnapshotDir()
            Dim dbPath As String = GetDbPath()
            If String.IsNullOrWhiteSpace(dbPath) OrElse Not File.Exists(dbPath) Then Return outcome

            Dim latest As FileInfo = GetLatestSnapshot(dir)
            If latest IsNot Nothing AndAlso Not force Then
                Dim dbInfo As New FileInfo(dbPath)
                Dim recentlyMade As Boolean = (DateTime.Now - latest.LastWriteTime).TotalMinutes < 10
                Dim unchanged As Boolean = dbInfo.LastWriteTimeUtc <= latest.LastWriteTimeUtc
                If recentlyMade OrElse unchanged Then Return outcome
            End If

            Dim name As String = SnapshotPrefix & DateTime.Now.ToString("yyyyMMdd_HHmmss") & ".db"
            Dim dest As String = Path.Combine(dir, name)
            If File.Exists(dest) Then File.Delete(dest)

            If Not CreateConsistentCopy(dest) Then Return outcome
            outcome.CreatedLocal = True
            outcome.FileName = name

            Dim gzPath As String = dest & ".gz"
            Try
                GzipFile(dest, gzPath)
                Dim gzInfo As New FileInfo(gzPath)
                outcome.SizeBytes = gzInfo.Length
                If gzInfo.Length <= MaxUploadBytes Then
                    outcome.UploadedCloud = SupabaseSync.UploadFileToBucket(gzPath, CloudSnapshotPrefix & name & ".gz")
                    If outcome.UploadedCloud Then
                        CloudFifo(CloudSnapshotPrefix, SnapshotKeep)
                    End If
                Else
                    Console.WriteLine($"Snapshot too large for cloud upload ({gzInfo.Length} bytes): {name}")
                End If
            Finally
                DeleteIfExists(gzPath)
            End Try

            LocalFifo(dir, SnapshotPrefix & "*.db", SnapshotKeep)
        Catch ex As Exception
            Console.WriteLine($"Snapshot error: {ex}")
        End Try
        Return outcome
    End Function

    ' Permanent monthly .bak. Runs on the first sync of each calendar month
    ' (checked by file existence - one per month, kept forever).
    Public Function CreateMonthlyBackupIfDue() As BackupOutcome
        Dim outcome As New BackupOutcome()
        Try
            Dim dir As String = GetManualDir()
            Dim name As String = "JadeClinic_Monthly_" & DateTime.Now.ToString("yyyyMM") & ".bak"
            Dim dest As String = Path.Combine(dir, name)
            If File.Exists(dest) Then
                outcome.Skipped = True
                Return outcome
            End If
            outcome = CreatePermanentBackupInternal("monthly", name, dest)
        Catch ex As Exception
            Console.WriteLine($"Monthly backup error: {ex}")
        End Try
        Return outcome
    End Function

    ' Forced permanent .bak of the current data (used before restore/seed so
    ' real records survive FIFO churn). Kept forever.
    Public Function CreatePermanentBackup(source As String) As BackupOutcome
        Dim outcome As New BackupOutcome()
        Try
            Dim dir As String = GetManualDir()
            Dim stamp As String = DateTime.Now.ToString("yyyyMMdd_HHmmss")
            Dim name As String
            Select Case source.ToLowerInvariant()
                Case "restore" : name = "JadeClinic_PreRestore_" & stamp & ".bak"
                Case "seed" : name = "JadeClinic_PreSeed_" & stamp & ".bak"
                Case Else : name = "JadeClinic_Manual_" & stamp & ".bak"
            End Select
            outcome = CreatePermanentBackupInternal(source, name, Path.Combine(dir, name))
        Catch ex As Exception
            Console.WriteLine($"Permanent backup error: {ex}")
        End Try
        Return outcome
    End Function

    Private Function CreatePermanentBackupInternal(source As String, name As String, dest As String) As BackupOutcome
        Dim outcome As New BackupOutcome()
        outcome.FileName = name
        outcome.FilePath = dest
        Try
            If File.Exists(dest) Then File.Delete(dest)
            If Not CreateConsistentCopy(dest) Then
                LocalSyncLog.CompleteEvent(LocalSyncLog.StartEvent("backup", source, "backup"), "failed", 0,
                    "Could not create backup copy of the database")
                Return outcome
            End If
            outcome.CreatedLocal = True

            Dim eventId As Integer = LocalSyncLog.StartEvent("backup", source, "backup")
            Dim gzPath As String = dest & ".gz"
            Try
                GzipFile(dest, gzPath)
                Dim gzInfo As New FileInfo(gzPath)
                If gzInfo.Length <= MaxUploadBytes Then
                    outcome.UploadedCloud = SupabaseSync.UploadFileToBucket(gzPath, CloudBackupPrefix & name & ".gz")
                Else
                    Console.WriteLine($"Backup too large for cloud upload ({gzInfo.Length} bytes): {name}")
                End If
                LocalSyncLog.CompleteEvent(eventId, If(outcome.UploadedCloud, "success", "failed"), 0,
                    If(outcome.UploadedCloud,
                       "Permanent backup saved (local + cloud)",
                       "Permanent backup saved locally; cloud upload failed"),
                    True, outcome.UploadedCloud, gzInfo.Length)
            Finally
                DeleteIfExists(gzPath)
            End Try
        Catch ex As Exception
            Console.WriteLine($"Permanent backup error ({name}): {ex}")
        End Try
        Return outcome
    End Function

    ' Upload a user-chosen backup file (manual "Create Backup" dialog) to the
    ' cloud permanently. Returns True when the gzipped upload succeeded.
    Public Function UploadBackupToCloud(localPath As String, remoteName As String) As Boolean
        Try
            If String.IsNullOrWhiteSpace(localPath) OrElse Not File.Exists(localPath) Then Return False
            Dim gzPath As String = localPath & ".gz"
            Try
                GzipFile(localPath, gzPath)
                Dim gzInfo As New FileInfo(gzPath)
                If gzInfo.Length > MaxUploadBytes Then
                    Console.WriteLine($"Backup too large for cloud upload ({gzInfo.Length} bytes): {remoteName}")
                    Return False
                End If
                Return SupabaseSync.UploadFileToBucket(gzPath, CloudBackupPrefix & remoteName & ".gz")
            Finally
                DeleteIfExists(gzPath)
            End Try
        Catch ex As Exception
            Console.WriteLine($"Backup cloud upload error: {ex}")
            Return False
        End Try
    End Function

    Private Function GetLatestSnapshot(dir As String) As FileInfo
        Try
            Dim files As FileInfo() = New DirectoryInfo(dir).GetFiles(SnapshotPrefix & "*.db")
            If files Is Nothing OrElse files.Length = 0 Then Return Nothing
            Return files.OrderByDescending(Function(f) f.LastWriteTime).FirstOrDefault()
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Sub LocalFifo(dir As String, pattern As String, keep As Integer)
        Try
            Dim files As List(Of FileInfo) = New DirectoryInfo(dir).GetFiles(pattern).
                OrderByDescending(Function(f) f.LastWriteTime).ToList()
            For i As Integer = keep To files.Count - 1
                DeleteIfExists(files(i).FullName)
            Next
        Catch ex As Exception
            Console.WriteLine($"Local snapshot FIFO error: {ex.Message}")
        End Try
    End Sub

    Private Sub CloudFifo(prefix As String, keep As Integer)
        Try
            Dim objs As List(Of SupabaseSync.S3ObjectInfo) = SupabaseSync.ListBucketObjects(prefix)
            If objs Is Nothing OrElse objs.Count <= keep Then Return
            Dim ordered As List(Of SupabaseSync.S3ObjectInfo) =
                objs.OrderByDescending(Function(o) o.LastModified).ToList()
            For i As Integer = keep To ordered.Count - 1
                SupabaseSync.DeleteBucketObject(ordered(i).Key)
            Next
        Catch ex As Exception
            Console.WriteLine($"Cloud snapshot FIFO error: {ex.Message}")
        End Try
    End Sub

    ' Consistent copy via VACUUM INTO (busy-safe); falls back to File.Copy.
    ' Never throws - returns False when the database is busy.
    Private Function CreateConsistentCopy(dest As String) As Boolean
        Dim dbPath As String = GetDbPath()
        Try
            Using conn As New SqliteConnection(Connection.GetConnectionString())
                conn.Open()
                Using cmd As New SqliteCommand("PRAGMA busy_timeout = 5000", conn)
                    cmd.ExecuteNonQuery()
                End Using
                Using cmd As New SqliteCommand("VACUUM INTO '" & dest.Replace("'", "''") & "'", conn)
                    cmd.ExecuteNonQuery()
                End Using
            End Using
            Return True
        Catch ex As Exception
            Console.WriteLine($"VACUUM INTO failed ({ex.Message}) - falling back to file copy")
            Try
                If File.Exists(dest) Then File.Delete(dest)
                File.Copy(dbPath, dest)
                Return True
            Catch ex2 As Exception
                Console.WriteLine($"File copy fallback failed (database busy?): {ex2.Message}")
                If File.Exists(dest) Then
                    Try : File.Delete(dest) : Catch : End Try
                End If
                Return False
            End Try
        End Try
    End Function

    Private Sub GzipFile(src As String, dest As String)
        Using fs As New FileStream(src, FileMode.Open, FileAccess.Read)
            Using gz As New FileStream(dest, FileMode.Create)
                Using gzStream As New GZipStream(gz, CompressionLevel.Optimal)
                    fs.CopyTo(gzStream)
                End Using
            End Using
        End Using
    End Sub

    Private Sub DeleteIfExists(path As String)
        Try
            If File.Exists(path) Then File.Delete(path)
        Catch
        End Try
    End Sub

End Module
