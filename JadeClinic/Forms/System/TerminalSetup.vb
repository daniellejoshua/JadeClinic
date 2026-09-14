Imports System.Data.Common
Imports System.Linq
Imports Guna.UI2.WinForms

Public Class TerminalSetup
    Private isNavigating As Boolean = False
    Private _headerUserControl As HeaderUserControl
    Private _refreshTimer As Timer
    Private _refreshing As Boolean = False

    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)
    Private ReadOnly RichOlive As Color = Color.FromArgb(190, 154, 48)
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)
    Private ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)
    Private ReadOnly PendingAmber As Color = Color.FromArgb(224, 160, 0)

    Private Const OnlineWindowMinutes As Integer = 3

    Private Sub TerminalSetup_Load(sender As Object, e As EventArgs) Handles MyBase.Load
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

            NavigationBuilder.Build(NavigationPanel, Me, "TerminalSetup")

            InitializeDataGridView()

            AddHandler btnRefresh.Click, AddressOf btnRefresh_Click

            LoadTerminals()

            _refreshTimer = New Timer() With {.Interval = 10000}
            AddHandler _refreshTimer.Tick, AddressOf RefreshTimer_Tick
            _refreshTimer.Start()

            Me.Activate()
            Me.Focus()
        Catch ex As Exception
            MessageBox.Show($"Error initializing PC Terminals page: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub TerminalSetup_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
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
            MessageBox.Show("Only administrators can manage PC terminals.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
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
        LoadTerminals()
    End Sub

    Private Sub RefreshTimer_Tick(sender As Object, e As EventArgs)
        LoadTerminals()
    End Sub

    Private Sub InitializeDataGridView()
        Try
            TermsGrid.Columns.Clear()
            TermsGrid.AutoGenerateColumns = False
            TermsGrid.AllowUserToAddRows = False
            TermsGrid.AllowUserToDeleteRows = False
            TermsGrid.ReadOnly = True
            TermsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            TermsGrid.MultiSelect = False
            TermsGrid.ScrollBars = ScrollBars.None
            TermsGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            TermsGrid.RowHeadersVisible = False
            TermsGrid.EnableHeadersVisualStyles = False

            TermsGrid.BackgroundColor = Color.FromArgb(250, 249, 246)
            TermsGrid.GridColor = Color.FromArgb(220, 220, 220)
            TermsGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal

            TermsGrid.DefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = Color.White,
                .ForeColor = Color.FromArgb(51, 51, 51),
                .SelectionBackColor = Color.FromArgb(235, 228, 200),
                .SelectionForeColor = Color.FromArgb(51, 51, 51),
                .Font = New Font("Poppins", 9.0F, FontStyle.Regular),
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Padding = New Padding(8, 6, 8, 6)
            }

            TermsGrid.AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = Color.FromArgb(250, 249, 246)
            }

            TermsGrid.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = Color.FromArgb(250, 249, 246),
                .ForeColor = Color.FromArgb(68, 68, 68),
                .SelectionBackColor = Color.FromArgb(250, 249, 246),
                .SelectionForeColor = Color.FromArgb(68, 68, 68),
                .Font = New Font("Poppins", 8.5F, FontStyle.Bold),
                .Alignment = DataGridViewContentAlignment.MiddleCenter
            }
            TermsGrid.ColumnHeadersHeight = 44
            TermsGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            TermsGrid.RowTemplate.Height = 50
            TermsGrid.AllowUserToResizeColumns = False
            TermsGrid.AllowUserToResizeRows = False

            TermsGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "TerminalName", .HeaderText = "Terminal Name", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {
                    .Alignment = DataGridViewContentAlignment.MiddleCenter,
                    .Font = New Font("Poppins SemiBold", 9.0F, FontStyle.Regular),
                    .ForeColor = Color.FromArgb(51, 51, 51),
                    .WrapMode = DataGridViewTriState.False
                },
                .FillWeight = 28
            })

            TermsGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "HostName", .HeaderText = "PC Name", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 18
            })

            TermsGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "LastSeen", .HeaderText = "Last Seen", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 16
            })

            TermsGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "Status", .HeaderText = "Status", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 18
            })

            Dim actionCol As New DataGridViewTextBoxColumn() With {
                .Name = "Action", .HeaderText = "", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {
                    .Alignment = DataGridViewContentAlignment.MiddleCenter,
                    .Font = New Font("Segoe UI Emoji", 12, FontStyle.Regular),
                    .ForeColor = Color.FromArgb(51, 51, 51)
                },
                .FillWeight = 8
            }
            TermsGrid.Columns.Add(actionCol)

            RemoveHandler TermsGrid.CellClick, AddressOf TermsGrid_CellClick
            AddHandler TermsGrid.CellClick, AddressOf TermsGrid_CellClick

            RemoveHandler TermsGrid.CellMouseEnter, AddressOf TermsGrid_CellMouseEnter
            AddHandler TermsGrid.CellMouseEnter, AddressOf TermsGrid_CellMouseEnter

            RemoveHandler TermsGrid.CellMouseLeave, AddressOf TermsGrid_CellMouseLeave
            AddHandler TermsGrid.CellMouseLeave, AddressOf TermsGrid_CellMouseLeave

            AlignGridToPanel()
        Catch ex As Exception
            MessageBox.Show($"Error preparing terminals grid: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadTerminals()
        If _refreshing Then Return
        _refreshing = True
        Try
            Dim list As List(Of TerminalManager.TerminalInfo) = TerminalManager.GetAllTerminals()
            TermsGrid.Rows.Clear()
            Dim onlineCount As Integer = 0
            Dim pendingCount As Integer = 0
            Dim totalCount As Integer = list.Count

            For Each t In list
                Dim isOnline As Boolean = t.LastSeenAt IsNot Nothing AndAlso
                                          DateTime.Now.Subtract(t.LastSeenAt.Value).TotalMinutes <= OnlineWindowMinutes
                If isOnline Then onlineCount += 1

                Dim status As String = If(String.IsNullOrWhiteSpace(t.Status), "pending", t.Status.Trim().ToLowerInvariant())
                Dim isPending As Boolean = status = "pending"
                Dim isAllowed As Boolean = status = "approved"
                If isPending Then pendingCount += 1

                Dim rowIndex As Integer = TermsGrid.Rows.Add()
                TermsGrid.Rows(rowIndex).Cells("TerminalName").Value = If(String.IsNullOrWhiteSpace(t.TerminalName), t.HostName, t.TerminalName)
                TermsGrid.Rows(rowIndex).Cells("HostName").Value = t.HostName
                TermsGrid.Rows(rowIndex).Cells("LastSeen").Value = If(t.LastSeenAt Is Nothing, "-", t.LastSeenAt.Value.ToString("MMM dd, yyyy HH:mm"))

                Dim statusText As String = "Offline  /  "
                If isPending Then
                    statusText = If(isOnline, "Online  /  ", "Offline  /  ") & "Pending approval"
                ElseIf isAllowed Then
                    statusText = If(isOnline, "Online", "Offline") & "  /  Allowed"
                Else
                    statusText = If(isOnline, "Online  /  ", "Offline  /  ") & "Blocked"
                End If
                TermsGrid.Rows(rowIndex).Cells("Status").Value = statusText
                If isPending Then
                    TermsGrid.Rows(rowIndex).Cells("Status").Style.ForeColor = PendingAmber
                ElseIf isAllowed AndAlso isOnline Then
                    TermsGrid.Rows(rowIndex).Cells("Status").Style.ForeColor = SuccessGreen
                ElseIf Not isAllowed Then
                    TermsGrid.Rows(rowIndex).Cells("Status").Style.ForeColor = AlertRed
                Else
                    TermsGrid.Rows(rowIndex).Cells("Status").Style.ForeColor = Color.FromArgb(150, 150, 150)
                End If

                TermsGrid.Rows(rowIndex).Cells("Action").Value = ChrW(&H270F)
                TermsGrid.Rows(rowIndex).Tag = t
            Next

            lblInfo.Text = $"{totalCount} registered computer(s) - {onlineCount} online now"
            If pendingCount > 0 Then
                lblInfo.Text &= $" - {pendingCount} awaiting approval"
            End If
            TermsGrid.ClearSelection()
        Catch ex As Exception
            Console.WriteLine($"TerminalSetup.LoadTerminals error: {ex.Message}")
        Finally
            _refreshing = False
        End Try
    End Sub

    Private Sub TermsGrid_CellMouseEnter(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If TermsGrid Is Nothing Then Return
            If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 Then
                If TermsGrid.Columns(e.ColumnIndex).Name = "Action" Then
                    TermsGrid.Cursor = Cursors.Hand
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub TermsGrid_CellMouseLeave(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If TermsGrid Is Nothing Then Return
            If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 Then
                If TermsGrid.Columns(e.ColumnIndex).Name = "Action" Then
                    TermsGrid.Cursor = Cursors.Default
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub TermsGrid_CellClick(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If e.RowIndex < 0 OrElse TermsGrid Is Nothing Then Return
            If e.ColumnIndex < 0 OrElse e.ColumnIndex >= TermsGrid.Columns.Count Then Return
            If TermsGrid.Columns(e.ColumnIndex).Name = "Action" Then
                Dim row = TermsGrid.Rows(e.RowIndex)
                Dim t = TryCast(row.Tag, TerminalManager.TerminalInfo)
                If t IsNot Nothing Then
                    ShowEditTerminalModal(t)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error processing action click: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowEditTerminalModal(info As TerminalManager.TerminalInfo)
        Try
            Dim accentGold As Color = Color.FromArgb(190, 154, 48)
            Dim textColor As Color = Color.FromArgb(51, 51, 51)
            Dim labelColor As Color = Color.FromArgb(80, 80, 80)
            Dim inputBg As Color = Color.FromArgb(250, 249, 246)
            Dim cr As Integer = 10
            Dim spacing As Integer = 14
            Dim controlH As Integer = 38

            Dim editForm As New Form() With {
                .Text = "Edit Terminal",
                .Size = New Size(680, 620),
                .MinimumSize = New Size(600, 560),
                .StartPosition = FormStartPosition.CenterParent,
                .FormBorderStyle = FormBorderStyle.FixedDialog,
                .MaximizeBox = False,
                .MinimizeBox = False,
                .BackColor = Color.White,
                .ShowInTaskbar = False
            }

            Dim px As Integer = 36
            Dim y As Integer = 24
            Dim labelW As Integer = 170
            Dim inputW As Integer = editForm.ClientSize.Width - px * 2 - labelW - 12

            Dim currentName As String = If(String.IsNullOrWhiteSpace(info.TerminalName), info.HostName, info.TerminalName)
            Dim status As String = If(String.IsNullOrWhiteSpace(info.Status), "pending", info.Status.Trim().ToLowerInvariant())

            Dim lblHeader As New Label() With {
                .Text = "Edit Terminal",
                .Font = New Font("Poppins SemiBold", 16, FontStyle.Bold),
                .ForeColor = accentGold,
                .Location = New Point(px, y),
                .AutoSize = True
            }
            editForm.Controls.Add(lblHeader)
            y += 46

            Dim sep As New Panel() With {
                .Location = New Point(px, y),
                .Size = New Size(editForm.ClientSize.Width - 72, 1),
                .BackColor = Color.FromArgb(235, 234, 230)
            }
            editForm.Controls.Add(sep)
            y += 26

            ' ---- Computer details section ----
            MakeSectionLabel(editForm, "PC DETAILS", y)
            y += 34

            MakeLabel(editForm, "Terminal Name:", y)
            Dim txtName As New Guna.UI2.WinForms.Guna2TextBox() With {
                .Text = currentName,
                .Location = New Point(px + labelW + 12, y),
                .Size = New Size(inputW, controlH),
                .BackColor = Color.Transparent,
                .FillColor = inputBg,
                .ForeColor = textColor,
                .BorderRadius = cr,
                .BorderThickness = 1,
                .BorderColor = Color.FromArgb(220, 220, 220),
                .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
                .Cursor = Cursors.IBeam
            }
            editForm.Controls.Add(txtName)
            y += controlH + spacing

            MakeLabel(editForm, "PC Name:", y)
            editForm.Controls.Add(New Label() With {
                .Text = info.HostName,
                .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
                .ForeColor = labelColor,
                .Location = New Point(px + labelW + 12, y + 6),
                .AutoSize = True
            })
            y += controlH + spacing

            MakeLabel(editForm, "Status:", y)
            Dim statusColor As Color = PendingAmber
            Dim statusCaption As String = "Pending approval - must be approved before it can open the app"
            If status = "approved" Then
                statusColor = SuccessGreen
                statusCaption = "Allowed - this PC can open the app"
            ElseIf status = "blocked" Then
                statusColor = AlertRed
                statusCaption = "Blocked - this PC cannot open the app"
            End If
            editForm.Controls.Add(New Label() With {
                .Text = statusCaption,
                .Font = New Font("Poppins", 9.5F, FontStyle.Bold),
                .ForeColor = statusColor,
                .Location = New Point(px + labelW + 12, y + 6),
                .AutoSize = True
            })
            y += controlH + spacing

            MakeLabel(editForm, "Last Seen:", y)
            editForm.Controls.Add(New Label() With {
                .Text = If(info.LastSeenAt Is Nothing, "-", info.LastSeenAt.Value.ToString("MMM dd, yyyy HH:mm")),
                .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
                .ForeColor = labelColor,
                .Location = New Point(px + labelW + 12, y + 6),
                .AutoSize = True
            })
            y += controlH + spacing

            MakeLabel(editForm, "Created:", y)
            editForm.Controls.Add(New Label() With {
                .Text = If(info.CreatedAt Is Nothing, "-", info.CreatedAt.Value.ToString("MMM dd, yyyy HH:mm")),
                .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
                .ForeColor = labelColor,
                .Location = New Point(px + labelW + 12, y + 6),
                .AutoSize = True
            })
            y += controlH + spacing

            ' ---- Access control section ----
            MakeSectionLabel(editForm, "ACCESS CONTROL", y)
            y += 34

            MakeLabel(editForm, "Allow Access:", y)
            Dim chkActive As New Guna.UI2.WinForms.Guna2ToggleSwitch() With {
                .Location = New Point(px + labelW + 12, y),
                .Size = New Size(44, 22),
                .Checked = (status = "approved")
            }
            chkActive.CheckedState.FillColor = accentGold
            editForm.Controls.Add(chkActive)

            Dim lblStatusText As New Label() With {
                .Text = If(status = "approved", "Allowed - this PC can open the app", "Blocked - this PC cannot open the app"),
                .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
                .ForeColor = If(status = "approved", Color.FromArgb(46, 125, 50), Color.FromArgb(180, 60, 60)),
                .Location = New Point(px + labelW + 72, y),
                .AutoSize = True
            }
            editForm.Controls.Add(lblStatusText)
            AddHandler chkActive.CheckedChanged, Sub()
                                                     lblStatusText.Text = If(chkActive.Checked, "Allowed - this PC can open the app", "Blocked - this PC cannot open the app")
                                                     lblStatusText.ForeColor = If(chkActive.Checked, Color.FromArgb(46, 125, 50), Color.FromArgb(180, 60, 60))
                                                 End Sub
            y += controlH + spacing

            editForm.Controls.Add(New Label() With {
                .Text = "Enabling access lets the PC open the app. Blocking prevents it from opening on the next launch." &
                        vbCrLf & "If the PC is still waiting for approval, switching this ON will approve it.",
                .Font = New Font("Poppins", 8.5F, FontStyle.Regular),
                .ForeColor = Color.FromArgb(150, 150, 150),
                .Location = New Point(px, y + 4),
                .AutoSize = True
            })
            y += 34

            Dim btnY As Integer = editForm.ClientSize.Height - 70
            Dim btnSave As New Guna.UI2.WinForms.Guna2Button() With {
                .Text = "Save Changes",
                .Size = New Size(150, 40),
                .Location = New Point(editForm.ClientSize.Width - 310, btnY),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
                .FillColor = accentGold,
                .ForeColor = Color.White,
                .Font = New Font("Poppins", 10, FontStyle.Regular),
                .BorderRadius = cr,
                .Cursor = Cursors.Hand
            }
            Dim btnCancel As New Guna.UI2.WinForms.Guna2Button() With {
                .Text = "Cancel",
                .Size = New Size(110, 40),
                .Location = New Point(editForm.ClientSize.Width - 150, btnY),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
                .FillColor = Color.FromArgb(220, 53, 69),
                .ForeColor = Color.White,
                .Font = New Font("Poppins", 10, FontStyle.Regular),
                .BorderRadius = cr,
                .Cursor = Cursors.Hand
            }
            editForm.Controls.Add(btnSave)
            editForm.Controls.Add(btnCancel)
            AddHandler btnCancel.Click, Sub() editForm.Close()

            AddHandler btnSave.Click, Sub()
                                          Try
                                              If String.IsNullOrWhiteSpace(txtName.Text) Then
                                                  MessageBox.Show("Terminal name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                  txtName.Focus()
                                                  Return
                                              End If

                                              If txtName.Text.Trim() <> currentName Then
                                                  TerminalManager.RenameTerminal(info.TerminalID, txtName.Text.Trim())
                                              End If
                                              If chkActive.Checked <> (status = "approved") Then
                                                  TerminalManager.SetTerminalActive(info.TerminalID, chkActive.Checked)
                                                  If chkActive.Checked Then
                                                      Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Terminal Allowed", $"Access allowed for PC '{txtName.Text.Trim()}' ({info.HostName})")
                                                  Else
                                                      Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Terminal Blocked", $"Access blocked for PC '{txtName.Text.Trim()}' ({info.HostName})")
                                                  End If
                                              ElseIf txtName.Text.Trim() <> currentName Then
                                                  Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Terminal Renamed", $"PC '{info.HostName}' renamed to '{txtName.Text.Trim()}'")
                                              End If

                                              editForm.Close()
                                              LoadTerminals()
                                          Catch ex As Exception
                                              MessageBox.Show($"Error saving terminal: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                          End Try
                                      End Sub

            Utilities.EnableEscCloseModal(editForm)
            editForm.ShowDialog()
            editForm.Dispose()
        Catch ex As Exception
            MessageBox.Show($"Error opening terminal modal: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub MakeSectionLabel(editForm As Form, text As String, top As Integer)
        Dim l As New Label() With {
            .Text = text,
            .Font = New Font("Poppins", 9.5F, FontStyle.Bold),
            .ForeColor = Color.FromArgb(190, 154, 48),
            .Location = New Point(36, top),
            .AutoSize = True,
            .TextAlign = ContentAlignment.MiddleLeft
        }
        editForm.Controls.Add(l)
    End Sub

    Private Sub MakeLabel(editForm As Form, text As String, top As Integer, Optional x As Integer = 30, Optional width As Integer = 160)
        Dim l As New Label() With {
            .Text = text,
            .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(80, 80, 80),
            .Location = New Point(x, top),
            .Size = New Size(width, 22),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        editForm.Controls.Add(l)
    End Sub

    Private Sub AlignGridToPanel()
        Try
            If Guna2Panel1 Is Nothing OrElse TermsGrid Is Nothing Then Return
            TermsGrid.Location = New Point(8, 72)
            TermsGrid.Width = Guna2Panel1.Width - 16
            TermsGrid.Height = Math.Max(100, Guna2Panel1.Height - 72 - 16)
        Catch
        End Try
    End Sub

    Private Sub TerminalSetup_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        AlignGridToPanel()
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