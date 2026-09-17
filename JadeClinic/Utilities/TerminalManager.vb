Imports System.Data.Common
Imports System.IO
Imports System.Text.Json
Imports Npgsql

' Tracks the PCs that connect to the shared JadeClinic database so the admin
' can name them and control their access. Each PC keeps a small identity file
' (terminal.json) and reports itself to the shared Terminals table.
'
' Access lifecycle: a brand-new PC registers as 'pending'. It cannot open the
' app until the administrator approves it (Status -> 'approved'). The admin can
' also block a PC ('blocked'). While the app is running a background heartbeat
' keeps LastSeenAt fresh so the PC Terminals page shows an accurate online state.
Public Class TerminalManager

    Public Enum TerminalAccessStatus
        Allowed
        Pending
        Blocked
    End Enum

    Public Class TerminalInfo
        Public Property TerminalID As String
        Public Property TerminalName As String
        Public Property HostName As String
        Public Property IsActive As Boolean
        Public Property Status As String
        Public Property LastSeenAt As DateTime?
        Public Property CreatedAt As DateTime?
    End Class

    Private Shared ReadOnly IdentityFileName As String = "terminal.json"
    Private Shared _currentTerminalId As String = ""
    Private Shared _currentTerminalName As String = ""
    Private Shared _heartbeatTimer As System.Threading.Timer
    Private Shared _heartbeatStarted As Boolean = False

    Public Shared Function GetIdentityFilePath() As String
        Dim dataDir As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JadeClinic")
        If Not Directory.Exists(dataDir) Then
            Directory.CreateDirectory(dataDir)
        End If
        Return Path.Combine(dataDir, IdentityFileName)
    End Function

    ' Shared-LAN mode is active only when a DBPath override is configured.
    Public Shared Function IsSharedMode() As Boolean
        Try
            Dim configuredPath As String = System.Configuration.ConfigurationManager.AppSettings("DBPath")
            Return Not String.IsNullOrWhiteSpace(configuredPath)
        Catch
            Return False
        End Try
    End Function

    ' The admin PC is identified by matching the installed machine name against
    ' the AdminMachineName setting. This is what makes the admin the only PC
    ' that can approve new terminals (both PCs ship the same publish folder).
    Public Shared Function IsAdminMachine() As Boolean
        Try
            Dim adminMachine As String = System.Configuration.ConfigurationManager.AppSettings("AdminMachineName")
            If String.IsNullOrWhiteSpace(adminMachine) Then Return False
            Return String.Equals(Environment.MachineName, adminMachine.Trim(), StringComparison.OrdinalIgnoreCase)
        Catch
            Return False
        End Try
    End Function

    ' Create (or load) the per-PC identity. If the file was copied from another
    ' machine (different hostname) a fresh ID is generated.
    Public Shared Function GetOrCreateIdentity() As TerminalInfo
        Try
            Dim path As String = GetIdentityFilePath()
            Dim hostName As String = Environment.MachineName

            If File.Exists(path) Then
                Try
                    Dim json As String = File.ReadAllText(path)
                    Dim info As TerminalInfo = JsonSerializer.Deserialize(Of TerminalInfo)(json)
                    If info IsNot Nothing AndAlso
                       Not String.IsNullOrWhiteSpace(info.TerminalID) AndAlso
                       String.Equals(info.HostName, hostName, StringComparison.OrdinalIgnoreCase) Then
                        _currentTerminalId = info.TerminalID
                        _currentTerminalName = If(String.IsNullOrWhiteSpace(info.TerminalName), hostName, info.TerminalName)
                        Return info
                    End If
                Catch
                End Try
            End If

            Dim newId As String = Guid.NewGuid().ToString("N").ToUpperInvariant()
            Dim newInfo As New TerminalInfo() With {
                .TerminalID = newId,
                .TerminalName = hostName,
                .HostName = hostName,
                .IsActive = False,
                .Status = "pending",
                .LastSeenAt = DateTime.Now,
                .CreatedAt = DateTime.Now
            }
            _currentTerminalId = newId
            _currentTerminalName = hostName
            SaveIdentity(newInfo)
            Return newInfo
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.GetOrCreateIdentity error: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Private Shared Sub SaveIdentity(info As TerminalInfo)
        Try
            File.WriteAllText(GetIdentityFilePath(), JsonSerializer.Serialize(info))
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.SaveIdentity error: {ex.Message}")
        End Try
    End Sub

    ' Idempotent: ensures the Terminals table exists and this PC has a row, then
    ' updates its last-seen time. New rows are inserted 'pending' (not allowed
    ' yet). The admin's confirm/block choices are never overwritten.
    Public Shared Sub RegisterCurrentTerminal()
        Try
            TerminalMigration.EnsureTable()
            Dim info As TerminalInfo = GetOrCreateIdentity()
            If info Is Nothing Then Return

            Using conn As DbConnection = DbProvider.CreateConnection(Connection.GetConnectionString())
                conn.Open()
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText =
                        "INSERT INTO Terminals (TerminalID, TerminalName, HostName, IsActive, Status, LastSeenAt) " &
                        "VALUES (@id, @name, @host, 0, 'pending', @now) " &
                        "ON CONFLICT (TerminalID) DO NOTHING"
                    cmd.Parameters.Add(New NpgsqlParameter("@id", info.TerminalID))
                    cmd.Parameters.Add(New NpgsqlParameter("@name", info.TerminalName))
                    cmd.Parameters.Add(New NpgsqlParameter("@host", info.HostName))
                    cmd.Parameters.Add(New NpgsqlParameter("@now", DateTime.Now))
                    cmd.ExecuteNonQuery()
                End Using
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText =
                        "UPDATE Terminals SET HostName = @host, LastSeenAt = @now, " &
                        "TerminalName = COALESCE(NULLIF(Trim(TerminalName), ''), @name) WHERE TerminalID = @id"
                    cmd.Parameters.Add(New NpgsqlParameter("@id", info.TerminalID))
                    cmd.Parameters.Add(New NpgsqlParameter("@host", info.HostName))
                    cmd.Parameters.Add(New NpgsqlParameter("@name", info.TerminalName))
                    cmd.Parameters.Add(New NpgsqlParameter("@now", DateTime.Now))
                    cmd.ExecuteNonQuery()
                End Using
            End Using

            StartHeartbeat()
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.RegisterCurrentTerminal error: {ex.Message}")
        End Try
    End Sub

    ' The admin (or the very first PC ever in a fresh store) is auto-approved so
    ' the clinic can never lock itself out with no one left to approve a PC.
    Public Shared Sub EnsureAdminApproval()
        Try
            If Not IsSharedMode() Then Return
            TerminalMigration.EnsureTable()
            Dim info As TerminalInfo = GetOrCreateIdentity()
            If info Is Nothing Then Return

            Dim shouldApprove As Boolean = IsAdminMachine()
            If Not shouldApprove Then
                Using conn As DbConnection = DbProvider.CreateConnection(Connection.GetConnectionString())
                    conn.Open()
                    Using cmd As DbCommand = conn.CreateCommand()
                        cmd.CommandText = "SELECT COUNT(*) FROM Terminals"
                        Dim total As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                        If total <= 1 Then shouldApprove = True
                    End Using
                End Using
            End If

            If shouldApprove Then
                ApproveTerminal(info.TerminalID)
            End If
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.EnsureAdminApproval error: {ex.Message}")
        End Try
    End Sub

    ' Access gate used at startup. In shared-LAN mode a PC that is still pending
    ' (blocked or waiting for admin approval) cannot run the app.
    Public Shared Function GetTerminalAccessStatus() As TerminalAccessStatus
        Try
            If Not IsSharedMode() Then
                Return TerminalAccessStatus.Allowed
            End If

            TerminalMigration.EnsureTable()
            Dim info As TerminalInfo = GetOrCreateIdentity()
            If info Is Nothing Then Return TerminalAccessStatus.Allowed

            Using conn As DbConnection = DbProvider.CreateConnection(Connection.GetConnectionString())
                conn.Open()
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = "SELECT Status FROM Terminals WHERE TerminalID = @id"
                    cmd.Parameters.Add(New NpgsqlParameter("@id", info.TerminalID))
                    Dim result As Object = cmd.ExecuteScalar()
                    If result Is Nothing OrElse IsDBNull(result) Then
                        ' Registered moments ago but not visible yet - keep pending.
                        Return TerminalAccessStatus.Pending
                    End If
                    Select Case Convert.ToString(result).Trim().ToLowerInvariant()
                        Case "approved"
                            Return TerminalAccessStatus.Allowed
                        Case "blocked"
                            Return TerminalAccessStatus.Blocked
                        Case Else
                            Return TerminalAccessStatus.Pending
                    End Select
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.GetTerminalAccessStatus error: {ex.Message}")
            ' If we cannot read the shared DB the app already fails elsewhere;
            ' allow startup rather than making the gate the failure point.
            Return TerminalAccessStatus.Allowed
        End Try
    End Function

    Public Shared Sub ApproveTerminal(terminalId As String)
        SetTerminalAccess(terminalId, "approved")
    End Sub

    Public Shared Sub BlockTerminal(terminalId As String)
        SetTerminalAccess(terminalId, "blocked")
    End Sub

    Private Shared Sub SetTerminalAccess(terminalId As String, status As String)
        If String.IsNullOrWhiteSpace(terminalId) Then Return
        Try
            Dim normalized As String = If(String.Equals(status, "approved", StringComparison.OrdinalIgnoreCase), "approved", "blocked")
            Using conn As DbConnection = DbProvider.CreateConnection(Connection.GetConnectionString())
                conn.Open()
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = "UPDATE Terminals SET Status = @status, IsActive = @active WHERE TerminalID = @id"
                    cmd.Parameters.Add(New NpgsqlParameter("@status", normalized))
                    cmd.Parameters.Add(New NpgsqlParameter("@active", If(normalized = "approved", 1, 0)))
                    cmd.Parameters.Add(New NpgsqlParameter("@id", terminalId))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.SetTerminalAccess error: {ex.Message}")
        End Try
    End Sub

    ' Kept for compatibility - maps a boolean toggle to approved/blocked.
    Public Shared Sub SetTerminalActive(terminalId As String, active As Boolean)
        SetTerminalAccess(terminalId, If(active, "approved", "blocked"))
    End Sub

    Public Shared Function GetCurrentTerminalId() As String
        Return _currentTerminalId
    End Function

    ' The current PC's display name. Prefers the administrator-editable
    ' TerminalName from the shared Terminals table (so renames in the PC
    ' Terminals page show up without a restart), then the cached identity,
    ' then the Windows hostname as a last resort.
    Public Shared Function GetCurrentTerminalName() As String
        Try
            If Not String.IsNullOrWhiteSpace(_currentTerminalId) Then
                TerminalMigration.EnsureTable()
                Using conn As DbConnection = DbProvider.CreateConnection(Connection.GetConnectionString())
                    conn.Open()
                    Using cmd As DbCommand = conn.CreateCommand()
                        cmd.CommandText = "SELECT TerminalName FROM Terminals WHERE TerminalID = @id"
                        cmd.Parameters.Add(New NpgsqlParameter("@id", _currentTerminalId))
                        Dim value As Object = cmd.ExecuteScalar()
                        If value IsNot Nothing AndAlso Not IsDBNull(value) AndAlso
                           Not String.IsNullOrWhiteSpace(Convert.ToString(value)) Then
                            _currentTerminalName = Convert.ToString(value)
                            Return _currentTerminalName
                        End If
                    End Using
                End Using
            End If
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.GetCurrentTerminalName error: {ex.Message}")
        End Try

        If Not String.IsNullOrWhiteSpace(_currentTerminalName) Then Return _currentTerminalName
        Return Environment.MachineName
    End Function

    Public Shared Function GetAllTerminals() As List(Of TerminalInfo)
        Dim list As New List(Of TerminalInfo)()
        Try
            TerminalMigration.EnsureTable()
            Using conn As DbConnection = DbProvider.CreateConnection(Connection.GetConnectionString())
                conn.Open()
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = "SELECT TerminalID, TerminalName, HostName, IsActive, Status, LastSeenAt, CreatedAt " &
                                      "FROM Terminals ORDER BY TerminalName ASC"
                    Using rdr As DbDataReader = cmd.ExecuteReader()
                        While rdr.Read()
                            list.Add(New TerminalInfo() With {
                                .TerminalID = Convert.ToString(rdr("TerminalID")),
                                .TerminalName = Convert.ToString(rdr("TerminalName")),
                                .HostName = Convert.ToString(rdr("HostName")),
                                .IsActive = Convert.ToInt32(rdr("IsActive")) = 1,
                                .Status = If(IsDBNull(rdr("Status")), "pending", Convert.ToString(rdr("Status"))),
                                .LastSeenAt = If(IsDBNull(rdr("LastSeenAt")), CObj(Nothing), CObj(Convert.ToDateTime(rdr("LastSeenAt")))),
                                .CreatedAt = If(IsDBNull(rdr("CreatedAt")), CObj(Nothing), CObj(Convert.ToDateTime(rdr("CreatedAt"))))
                            })
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.GetAllTerminals error: {ex.Message}")
        End Try
        Return list
    End Function

    ' Terminals that are asking for access right now (used by the admin prompt).
    Public Shared Function GetPendingTerminals() As List(Of TerminalInfo)
        Dim list As New List(Of TerminalInfo)()
        Try
            TerminalMigration.EnsureTable()
            Using conn As DbConnection = DbProvider.CreateConnection(Connection.GetConnectionString())
                conn.Open()
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = "SELECT TerminalID, TerminalName, HostName, IsActive, Status, LastSeenAt, CreatedAt " &
                                      "FROM Terminals WHERE Status = 'pending' ORDER BY CreatedAt ASC"
                    Using rdr As DbDataReader = cmd.ExecuteReader()
                        While rdr.Read()
                            list.Add(New TerminalInfo() With {
                                .TerminalID = Convert.ToString(rdr("TerminalID")),
                                .TerminalName = Convert.ToString(rdr("TerminalName")),
                                .HostName = Convert.ToString(rdr("HostName")),
                                .IsActive = Convert.ToInt32(rdr("IsActive")) = 1,
                                .Status = Convert.ToString(rdr("Status")),
                                .LastSeenAt = If(IsDBNull(rdr("LastSeenAt")), CObj(Nothing), CObj(Convert.ToDateTime(rdr("LastSeenAt")))),
                                .CreatedAt = If(IsDBNull(rdr("CreatedAt")), CObj(Nothing), CObj(Convert.ToDateTime(rdr("CreatedAt"))))
                            })
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.GetPendingTerminals error: {ex.Message}")
        End Try
        Return list
    End Function

    Public Shared Sub RenameTerminal(terminalId As String, newName As String)
        If String.IsNullOrWhiteSpace(terminalId) Then Return
        Try
            Using conn As DbConnection = DbProvider.CreateConnection(Connection.GetConnectionString())
                conn.Open()
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = "UPDATE Terminals SET TerminalName = @name WHERE TerminalID = @id"
                    cmd.Parameters.Add(New NpgsqlParameter("@name", newName.Trim()))
                    cmd.Parameters.Add(New NpgsqlParameter("@id", terminalId))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.RenameTerminal error: {ex.Message}")
        End Try
    End Sub

    ' Background heartbeat - keeps LastSeenAt fresh while the app is open so the
    ' PC Terminals page reports an accurate online/offline state.
    Public Shared Sub Heartbeat(Optional terminalId As String = Nothing)
        Try
            If Not IsSharedMode() Then Return
            Dim id As String = If(String.IsNullOrWhiteSpace(terminalId), _currentTerminalId, terminalId)
            If String.IsNullOrWhiteSpace(id) Then Return

            Using conn As DbConnection = DbProvider.CreateConnection(Connection.GetConnectionString())
                conn.Open()
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = "UPDATE Terminals SET LastSeenAt = @now WHERE TerminalID = @id"
                    cmd.Parameters.Add(New NpgsqlParameter("@id", id))
                    cmd.Parameters.Add(New NpgsqlParameter("@now", DateTime.Now))
                    cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.Heartbeat error: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub StartHeartbeat()
        Try
            If _heartbeatStarted OrElse Not IsSharedMode() Then Return
            _heartbeatStarted = True
            _heartbeatTimer = New System.Threading.Timer(
                    AddressOf HeartbeatTick,
                    Nothing,
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(30))
        Catch ex As Exception
            Console.WriteLine($"TerminalManager.StartHeartbeat error: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub HeartbeatTick(state As Object)
        Heartbeat()
    End Sub

End Class