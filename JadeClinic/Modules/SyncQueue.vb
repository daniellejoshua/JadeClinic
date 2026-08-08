Imports System.Threading
Imports System.Threading.Tasks

' Background job queue for Supabase cloud sync.
' Runs a single serialized worker over a FIFO queue so syncs never block the
' UI and never run concurrently. Callers trigger work with Enqueue(full),
' MarkDataChanged() (debounced auto-sync after data changes) or
' StartScheduledSync() (periodic delta sync while the app is open).
'
' Events are raised on the worker thread - subscribers MUST marshal to the UI
' thread (e.g. BeginInvoke) before touching controls.
Public Class SyncQueue

    ' Singleton instance
    Private Shared _instance As SyncQueue
    Public Shared ReadOnly Property Instance As SyncQueue
        Get
            If _instance Is Nothing Then
                _instance = New SyncQueue()
            End If
            Return _instance
        End Get
    End Property

    Public Event Progress(message As String)
    Public Event Completed(success As Boolean, summary As String)
    Public Event Failed(errorMessage As String)

    Private ReadOnly _queue As New Queue(Of Boolean)()   ' True = full sync
    Private ReadOnly _lock As New Object()
    Private _running As Boolean = False

    Private ReadOnly _debounceTimer As System.Windows.Forms.Timer
    Private ReadOnly _scheduleTimer As System.Windows.Forms.Timer
    Private Const DebounceMs As Integer = 60000          ' 1 minute after last change
    Private Const ScheduleMs As Integer = 30 * 60 * 1000 ' every 30 minutes

    Private Sub New()
        _debounceTimer = New System.Windows.Forms.Timer()
        _debounceTimer.Interval = DebounceMs
        AddHandler _debounceTimer.Tick, Sub(s, e)
                                            Try
                                                _debounceTimer.Stop()
                                                Enqueue(full:=False)
                                            Catch ex As Exception
                                                Console.WriteLine($"SyncQueue debounce error: {ex.Message}")
                                            End Try
                                        End Sub

        _scheduleTimer = New System.Windows.Forms.Timer()
        _scheduleTimer.Interval = ScheduleMs
        AddHandler _scheduleTimer.Tick, Sub(s, e)
                                            Try
                                                _scheduleTimer.Stop()
                                                Enqueue(full:=False)
                                                _scheduleTimer.Start()
                                            Catch ex As Exception
                                                Console.WriteLine($"SyncQueue schedule error: {ex.Message}")
                                            End Try
                                        End Sub
    End Sub

    Public ReadOnly Property IsRunning As Boolean
        Get
            SyncLock _lock
                Return _running
            End SyncLock
        End Get
    End Property

    Public ReadOnly Property PendingCount As Integer
        Get
            SyncLock _lock
                Return _queue.Count
            End SyncLock
        End Get
    End Property

    ' Request a sync after data changed. Debounced: repeated calls within the
    ' debounce window collapse into a single delta sync.
    Public Sub MarkDataChanged()
        Try
            _debounceTimer.Stop()
            _debounceTimer.Start()
        Catch ex As Exception
            Console.WriteLine($"SyncQueue MarkDataChanged error: {ex.Message}")
        End Try
    End Sub

    Public Sub StartScheduledSync()
        Try
            If Not _scheduleTimer.Enabled Then
                _scheduleTimer.Start()
            End If
        Catch ex As Exception
            Console.WriteLine($"SyncQueue StartScheduledSync error: {ex.Message}")
        End Try
    End Sub

    Public Sub StopScheduledSync()
        Try
            _scheduleTimer.Stop()
        Catch ex As Exception
            Console.WriteLine($"SyncQueue StopScheduledSync error: {ex.Message}")
        End Try
    End Sub

    ' Add a sync job. full = True forces a complete upsert of every table
    ' (used by the manual "Sync Cloud" button as the safety valve).
    Public Sub Enqueue(full As Boolean)
        Dim startWorker As Boolean = False
        SyncLock _lock
            _queue.Enqueue(full)
            If Not _running Then
                _running = True
                startWorker = True
            End If
        End SyncLock

        If startWorker Then
            Task.Run(AddressOf ProcessQueue)
        End If
    End Sub

    Private Sub ProcessQueue()
        While True
            Dim full As Boolean = False
            SyncLock _lock
                If _queue.Count = 0 Then
                    _running = False
                    Exit While
                End If
                full = _queue.Dequeue()
            End SyncLock

            RaiseProgress(If(full, "Cloud sync starting (full)...", "Cloud sync starting (delta)..."))
            Try
                Dim result As SyncResult = SupabaseSync.RunFullSync(AddressOf OnSyncProgress, full)
                If result.Success Then
                    RaiseProgress("Cloud sync complete.")
                    RaiseEvent Completed(True, String.Join(Environment.NewLine, result.Summary))
                Else
                    RaiseProgress("Cloud sync failed: " & result.ErrorMessage)
                    RaiseEvent Failed(result.ErrorMessage)
                End If
            Catch ex As Exception
                RaiseProgress("Cloud sync failed: " & ex.Message)
                RaiseEvent Failed(ex.Message)
            End Try
        End While
    End Sub

    Private Sub OnSyncProgress(message As String)
        RaiseEvent Progress(message)
    End Sub

    Private Sub RaiseProgress(message As String)
        RaiseEvent Progress(message)
    End Sub

End Class
