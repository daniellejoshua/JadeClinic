Imports System.Data
Imports System.Linq
Imports Guna.UI2.WinForms

Public Class SyncLogs
    Private isNavigating As Boolean = False
    Private _headerUserControl As HeaderUserControl
    Private _refreshTimer As Timer
    Private _refreshing As Boolean = False

    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)
    Private ReadOnly RichOlive As Color = Color.FromArgb(190, 154, 48)
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)
    Private ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)

    Private Const OnlineWindowMinutes As Integer = 2

    Private Sub SyncLogs_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.BackColor = Color.FromArgb(248, 248, 247)
        Try
            Me.KeyPreview = True

            If Not IsHostedInMainShell() Then
                Me.FormBorderStyle = FormBorderStyle.None
                Me.WindowState = FormWindowState.Maximized
            End If

            If Not ValidateAdminSession() Then Return

            IdleTimeoutManager.Instance.StartMonitoring(Me)
            InitializeProfileSection()

            NavigationBuilder.Build(NavigationPanel, Me, "SyncLogs")

            InitializeDataGrids()

            AddHandler btnRefresh.Click, AddressOf btnRefresh_Click
            AddHandler btnExport.Click, AddressOf btnExport_Click

            RefreshAll()

            _refreshTimer = New Timer() With {.Interval = 8000}
            AddHandler _refreshTimer.Tick, AddressOf RefreshTimer_Tick
            _refreshTimer.Start()

            Me.Activate()
            Me.Focus()
        Catch ex As Exception
            MessageBox.Show($"Error initializing Sync Logs page: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SyncLogs_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        IdleTimeoutManager.Instance.StopMonitoring(Me)
        If _refreshTimer IsNot Nothing Then
            _refreshTimer.Stop()
        End If
    End Sub

    Private Function ValidateAdminSession() As Boolean
        If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            MessageBox.Show("User session expired. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            If IsHostedInMainShell() Then
                GetMainShell().ShowPage(GetType(frmLoginvb))
            Else
                frmLoginvb.Show()
            End If
            Me.Close()
            Return False
        End If

        Dim role As String = If(frmLoginvb.LoggedInRole, "").ToUpperInvariant()
        If role <> "ADMIN" AndAlso role <> "ADMINISTRATOR" Then
            MessageBox.Show("Only administrators can view sync logs.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            NavigationBuilder.NavigateToForm(Me, GetType(Dashboard))
            Return False
        End If
        Return True
    End Function

    Private Sub InitializeProfileSection()
        _headerUserControl = New HeaderUserControl()
        Me.Controls.Add(_headerUserControl)
        _headerUserControl.BringToFront()
        _headerUserControl.Initialize(Me, AddressOf NavigateToProfileSettings)
    End Sub

    Private Sub NavigateToProfileSettings()
        Try
            isNavigating = True
            Dim profileForm As New ProfileSettings()
            profileForm.StartPosition = FormStartPosition.CenterScreen
            profileForm.Show()
            If Not IsHostedInMainShell() Then Me.Close()
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open Profile Settings: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnRefresh_Click(sender As Object, e As EventArgs)
        RefreshAll()
    End Sub

    Private Sub RefreshTimer_Tick(sender As Object, e As EventArgs)
        RefreshAll()
    End Sub

    Private Sub InitializeDataGrids()
        StyleGrid(dgvPCs)
        StyleGrid(dgvSync)

        dgvPCs.Columns.Clear()
        dgvPCs.AutoGenerateColumns = False
        dgvPCs.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "TerminalName", .HeaderText = "Terminal Name", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Font = New Font("Poppins SemiBold", 9.0F, FontStyle.Regular),
                .WrapMode = DataGridViewTriState.False
            },
            .FillWeight = 32
        })
        dgvPCs.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "HostName", .HeaderText = "PC Name", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 22
        })
        dgvPCs.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "LastSeen", .HeaderText = "Last Seen", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 24
        })
        dgvPCs.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Status", .HeaderText = "Status", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 22
        })

        dgvSync.Columns.Clear()
        dgvSync.AutoGenerateColumns = False
        dgvSync.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Started", .HeaderText = "Started", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 16
        })
        dgvSync.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Event", .HeaderText = "Event", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 9
        })
        dgvSync.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Source", .HeaderText = "Source", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 11
        })
        dgvSync.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Mode", .HeaderText = "Mode", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 8
        })
        dgvSync.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Duration", .HeaderText = "Duration", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 8
        })
        dgvSync.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Rows", .HeaderText = "Rows", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 6
        })
        dgvSync.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Deleted", .HeaderText = "Deleted", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 6
        })
        dgvSync.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "SnapSize", .HeaderText = "Snapshot", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 8
        })
        dgvSync.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Status", .HeaderText = "Status", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 8
        })
        dgvSync.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Details", .HeaderText = "Details", .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleLeft,
                .WrapMode = DataGridViewTriState.False
            },
            .FillWeight = 30
        })

        AlignGridsToPanel()
    End Sub

    Private Sub StyleGrid(grid As Guna.UI2.WinForms.Guna2DataGridView)
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.ReadOnly = True
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.MultiSelect = False
        grid.ScrollBars = ScrollBars.None
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        grid.RowHeadersVisible = False
        grid.EnableHeadersVisualStyles = False
        grid.BackgroundColor = Color.FromArgb(250, 249, 246)
        grid.GridColor = Color.FromArgb(220, 220, 220)
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal

        grid.DefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = Color.White,
            .ForeColor = Color.FromArgb(51, 51, 51),
            .SelectionBackColor = Color.FromArgb(235, 228, 200),
            .SelectionForeColor = Color.FromArgb(51, 51, 51),
            .Font = New Font("Poppins", 9.0F, FontStyle.Regular),
            .Alignment = DataGridViewContentAlignment.MiddleCenter,
            .Padding = New Padding(6, 5, 6, 5)
        }

        grid.AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = Color.FromArgb(250, 249, 246)
        }

        grid.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = Color.FromArgb(250, 249, 246),
            .ForeColor = Color.FromArgb(68, 68, 68),
            .SelectionBackColor = Color.FromArgb(250, 249, 246),
            .SelectionForeColor = Color.FromArgb(68, 68, 68),
            .Font = New Font("Poppins", 8.5F, FontStyle.Bold),
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        }
        grid.ColumnHeadersHeight = 40
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        grid.AllowUserToResizeColumns = False
        grid.AllowUserToResizeRows = False
    End Sub

    Private Sub RefreshAll()
        If _refreshing Then Return
        _refreshing = True
        Try
            LoadPCs()
            LoadSyncEvents()
        Catch ex As Exception
            Console.WriteLine($"SyncLogs.RefreshAll error: {ex.Message}")
        Finally
            _refreshing = False
        End Try
    End Sub

    Private Sub LoadPCs()
        Try
            Dim list As List(Of TerminalManager.TerminalInfo) = TerminalManager.GetAllTerminals()
            dgvPCs.Rows.Clear()
            Dim onlineCount As Integer = 0

            For Each t In list
                Dim isOnline As Boolean = t.LastSeenAt IsNot Nothing AndAlso
                                          DateTime.Now.Subtract(t.LastSeenAt.Value).TotalMinutes <= OnlineWindowMinutes
                If isOnline Then onlineCount += 1

                Dim rowIndex As Integer = dgvPCs.Rows.Add()
                dgvPCs.Rows(rowIndex).Cells("TerminalName").Value = If(String.IsNullOrWhiteSpace(t.TerminalName), t.HostName, t.TerminalName)
                dgvPCs.Rows(rowIndex).Cells("HostName").Value = t.HostName
                dgvPCs.Rows(rowIndex).Cells("LastSeen").Value = If(t.LastSeenAt Is Nothing, "-", t.LastSeenAt.Value.ToString("MMM dd, yyyy HH:mm"))

                Dim statusText As String = If(isOnline, "Online", "Offline") & "  /  " & If(t.IsActive, "Allowed", "Blocked")
                dgvPCs.Rows(rowIndex).Cells("Status").Value = statusText
                If isOnline AndAlso t.IsActive Then
                    dgvPCs.Rows(rowIndex).Cells("Status").Style.ForeColor = SuccessGreen
                ElseIf Not t.IsActive Then
                    dgvPCs.Rows(rowIndex).Cells("Status").Style.ForeColor = AlertRed
                Else
                    dgvPCs.Rows(rowIndex).Cells("Status").Style.ForeColor = Color.FromArgb(150, 150, 150)
                End If
            Next

            lblInfo.Text = $"{list.Count} connected PC(s) - {onlineCount} online now"
            dgvPCs.ClearSelection()
        Catch ex As Exception
            Console.WriteLine($"SyncLogs.LoadPCs error: {ex.Message}")
        End Try
    End Sub

    Private Sub LoadSyncEvents()
        Try
            Dim dt As DataTable = LocalSyncLog.GetEvents(500)
            dgvSync.Rows.Clear()

            For Each row As DataRow In dt.Rows
                Dim status As String = Convert.ToString(row("Status"))
                If String.IsNullOrEmpty(status) Then status = "running"

                Dim rowIndex As Integer = dgvSync.Rows.Add()
                dgvSync.Rows(rowIndex).Cells("Started").Value = Convert.ToString(row("Started"))
                dgvSync.Rows(rowIndex).Cells("Event").Value = Convert.ToString(row("Event"))
                dgvSync.Rows(rowIndex).Cells("Source").Value = Convert.ToString(row("Source"))
                dgvSync.Rows(rowIndex).Cells("Mode").Value = Convert.ToString(row("Mode"))
                dgvSync.Rows(rowIndex).Cells("Duration").Value = Convert.ToString(row("Duration"))
                dgvSync.Rows(rowIndex).Cells("Rows").Value = Convert.ToInt32(row("Rows")).ToString()
                dgvSync.Rows(rowIndex).Cells("Deleted").Value = Convert.ToInt32(row("Deleted")).ToString()
                dgvSync.Rows(rowIndex).Cells("SnapSize").Value = Convert.ToString(row("Snap Size"))
                dgvSync.Rows(rowIndex).Cells("Status").Value = status
                dgvSync.Rows(rowIndex).Cells("Details").Value = Convert.ToString(row("Error"))

                Select Case status
                    Case "success"
                        dgvSync.Rows(rowIndex).Cells("Status").Style.ForeColor = SuccessGreen
                    Case "failed"
                        dgvSync.Rows(rowIndex).Cells("Status").Style.ForeColor = AlertRed
                        If Not String.IsNullOrEmpty(Convert.ToString(row("Error"))) Then
                            dgvSync.Rows(rowIndex).Cells("Details").Style.ForeColor = AlertRed
                        End If
                    Case Else
                        dgvSync.Rows(rowIndex).Cells("Status").Style.ForeColor = RichOlive
                End Select
            Next

            dgvSync.ClearSelection()
        Catch ex As Exception
            Console.WriteLine($"SyncLogs.LoadSyncEvents error: {ex.Message}")
        End Try
    End Sub

    Private Sub btnExport_Click(sender As Object, e As EventArgs)
        Try
            If dgvSync.Rows.Count = 0 Then
                MessageBox.Show("No sync events to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If
            Using sfd As New SaveFileDialog()
                sfd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
                sfd.FileName = $"SyncLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                If sfd.ShowDialog() = DialogResult.OK Then
                    Using sw As New IO.StreamWriter(sfd.FileName, False, System.Text.Encoding.UTF8)
                        Dim headerParts As New List(Of String)()
                        For Each col As DataGridViewColumn In dgvSync.Columns
                            headerParts.Add(col.HeaderText.Replace(",", " "))
                        Next
                        sw.WriteLine(String.Join(",", headerParts))

                        For Each r As DataGridViewRow In dgvSync.Rows
                            If r.IsNewRow Then Continue For
                            Dim parts As New List(Of String)()
                            For Each cell As DataGridViewCell In r.Cells
                                Dim val As String = If(cell.Value Is Nothing, "", cell.Value.ToString().Replace(","c, " "))
                                parts.Add(val)
                            Next
                            sw.WriteLine(String.Join(",", parts))
                        Next
                        sw.Flush()
                    End Using
                    MessageBox.Show("Export completed.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AlignGridsToPanel()
        Try
            If Guna2Panel1 Is Nothing Then Return
            If dgvPCs IsNot Nothing Then
                dgvPCs.Location = New Point(8, 44)
                dgvPCs.Width = Guna2Panel1.Width - 16
                dgvPCs.Height = 250
            End If
            If dgvSync IsNot Nothing Then
                dgvSync.Location = New Point(8, 336)
                dgvSync.Width = Guna2Panel1.Width - 16
                dgvSync.Height = Math.Max(100, Guna2Panel1.Height - 336 - 16)
            End If
        Catch
        End Try
    End Sub

    Private Sub SyncLogs_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        AlignGridsToPanel()
    End Sub

    Private Function IsHostedInMainShell() As Boolean
        Dim parent As Control = Me.Parent
        While parent IsNot Nothing
            If TypeOf parent Is MainShell Then
                Return True
            End If
            parent = parent.Parent
        End While
        Return False
    End Function

    Private Function GetMainShell() As MainShell
        Dim parent As Control = Me.Parent
        While parent IsNot Nothing
            If TypeOf parent Is MainShell Then
                Return CType(parent, MainShell)
            End If
            parent = parent.Parent
        End While
        Return Nothing
    End Function
End Class