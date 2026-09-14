Imports System.Linq
Imports System.Reflection
Imports System.Runtime.InteropServices

' Any page that wants its draft state (e.g. the POS cart) saved before the
' shell disposes it during navigation should implement this. Form.Dispose()
' does not raise FormClosing, so pages cannot rely on that event.
Public Interface IDraftPersistable
    Sub PersistDraft()
End Interface

Public Class MainShell
    Private _currentPage As Form
    Private _currentPageType As Type
    Private ReadOnly _loadingOverlay As Panel
    Private _isMaximized As Boolean = False
    Private _wasMaximizedBeforeMinimize As Boolean = False
    Private _isShowingPage As Boolean = False
    Private _lastReloadTick As Integer = 0
    Private WithEvents _hoverTimer As New Timer() With {.Interval = 100}
    Private WithEvents _resizeDebounceTimer As New Timer() With {.Interval = 300, .Enabled = False}
    Private WithEvents _approvalTimer As New Timer() With {.Interval = 10000}
    Private _approvalPromptOpen As Boolean = False
    Private _approvalPromptedIds As New HashSet(Of String)()

    Private titleBarPanel As Guna.UI2.WinForms.Guna2Panel
    Private btnMinimize As Guna.UI2.WinForms.Guna2Button
    Private btnMaximize As Guna.UI2.WinForms.Guna2Button
    Private btnCloseTitle As Guna.UI2.WinForms.Guna2Button

    Private Const WM_NCHITTEST As Integer = &H84
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HTLEFT As Integer = 10
    Private Const HTRIGHT As Integer = 11
    Private Const HTTOP As Integer = 12
    Private Const HTTOPLEFT As Integer = 13
    Private Const HTTOPRIGHT As Integer = 14
    Private Const HTBOTTOM As Integer = 15
    Private Const HTBOTTOMLEFT As Integer = 16
    Private Const HTBOTTOMRIGHT As Integer = 17
    Private Const HTCAPTION As Integer = 2
    Private Const BORDERWIDTH As Integer = 8
    Private _pendingResizeEdge As Integer = 0

    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As Integer, lParam As Integer) As Integer
    End Function

    Private _childFormHook As NativeWindow = Nothing

    Public Sub New()
        InitializeComponent()
        Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        _loadingOverlay = New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(26, 29, 31),
            .Visible = False
        }
        CreateTitleBar()
        AddHandler ContentPanel.Resize, AddressOf ContentPanel_Resize
        SetupDragStrip()
        _hoverTimer.Start()
        _approvalTimer.Start()
    End Sub

    Private WithEvents dragStrip As Panel

    Private Sub SetupDragStrip()
        dragStrip = New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 35,
            .BackColor = Color.FromArgb(1, 26, 29, 31),
            .Cursor = Cursors.SizeAll,
            .Visible = False
        }
        AddHandler dragStrip.MouseDown, Sub(s, e)
                                            If e.Button <> MouseButtons.Left Then Return
                                            ReleaseCapture()
                                            SendMessage(Me.Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0)
                                        End Sub
        Me.Controls.Add(dragStrip)
    End Sub

    Private Sub CreateTitleBar()
        Dim btnWidth As Integer = 46
        Dim btnCount As Integer = 3
        titleBarPanel = New Guna.UI2.WinForms.Guna2Panel() With {
            .Height = 35,
            .Width = btnWidth * btnCount,
            .FillColor = Color.White,
            .Visible = False,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }

        btnCloseTitle = New Guna.UI2.WinForms.Guna2Button() With {
            .Dock = DockStyle.Right,
            .Width = 46,
            .FillColor = Color.White,
            .ForeColor = Color.FromArgb(42, 42, 42),
            .Font = New Font("Segoe UI", 10),
            .Text = ChrW(&H2715),
            .BorderColor = Color.Transparent,
            .BorderRadius = 0,
            .Cursor = Cursors.Hand
        }
        AddHandler btnCloseTitle.Click, Sub()
                                            Dim result = EscForm.ConfirmExit(Me)
                                            If result = DialogResult.Yes Then
                                                Application.Exit()
                                            End If
                                        End Sub
        AddHandler btnCloseTitle.MouseEnter, Sub()
                                                 btnCloseTitle.FillColor = Color.FromArgb(220, 80, 70)
                                                 btnCloseTitle.ForeColor = Color.White
                                             End Sub
        AddHandler btnCloseTitle.MouseLeave, Sub()
                                                 btnCloseTitle.FillColor = Color.White
                                                 btnCloseTitle.ForeColor = Color.FromArgb(42, 42, 42)
                                             End Sub

        btnMaximize = New Guna.UI2.WinForms.Guna2Button() With {
            .Dock = DockStyle.Right,
            .Width = 46,
            .FillColor = Color.White,
            .ForeColor = Color.FromArgb(42, 42, 42),
            .Font = New Font("Segoe UI", 10),
            .Text = ChrW(&H25A1),
            .BorderColor = Color.Transparent,
            .BorderRadius = 0,
            .Cursor = Cursors.Hand
        }
        AddHandler btnMaximize.Click, Sub()
                                          If Me.WindowState = FormWindowState.Maximized Then
                                              Me.WindowState = FormWindowState.Normal
                                              Dim w As Integer = CInt(Screen.PrimaryScreen.WorkingArea.Width * 0.8)
                                              Dim h As Integer = CInt(Screen.PrimaryScreen.WorkingArea.Height * 0.85)
                                              Dim x As Integer = CInt((Screen.PrimaryScreen.WorkingArea.Width - w) / 2)
                                              Dim y As Integer = CInt((Screen.PrimaryScreen.WorkingArea.Height - h) / 2)
                                              Me.Bounds = New Rectangle(x, y, w, h)
                                              _isMaximized = False
                                              btnMaximize.Text = ChrW(&H25A1)
                                          Else
                                              Me.WindowState = FormWindowState.Maximized
                                              _isMaximized = True
                                              btnMaximize.Text = ChrW(&H2752)
                                          End If
                                      End Sub
        AddHandler btnMaximize.MouseEnter, Sub()
                                               btnMaximize.FillColor = Color.FromArgb(230, 230, 230)
                                           End Sub
        AddHandler btnMaximize.MouseLeave, Sub()
                                               btnMaximize.FillColor = Color.White
                                           End Sub

        btnMinimize = New Guna.UI2.WinForms.Guna2Button() With {
            .Dock = DockStyle.Right,
            .Width = 46,
            .FillColor = Color.White,
            .ForeColor = Color.FromArgb(42, 42, 42),
            .Font = New Font("Segoe UI", 10),
            .Text = ChrW(&H2013),
            .BorderColor = Color.Transparent,
            .BorderRadius = 0,
            .Cursor = Cursors.Hand
        }
        AddHandler btnMinimize.Click, Sub()
                                          Me.WindowState = FormWindowState.Minimized
                                      End Sub
        AddHandler btnMinimize.MouseEnter, Sub()
                                               btnMinimize.FillColor = Color.FromArgb(230, 230, 230)
                                           End Sub
        AddHandler btnMinimize.MouseLeave, Sub()
                                               btnMinimize.FillColor = Color.White
                                           End Sub

        titleBarPanel.Controls.Add(btnMinimize)
        titleBarPanel.Controls.Add(btnMaximize)
        titleBarPanel.Controls.Add(btnCloseTitle)

        Me.Controls.Add(titleBarPanel)
        titleBarPanel.BringToFront()
        PositionTitleBar()
    End Sub

    Private Sub PositionTitleBar()
        If titleBarPanel Is Nothing Then Return
        titleBarPanel.Location = New Point(Me.ClientSize.Width - titleBarPanel.Width, 0)
    End Sub

    Private Sub _hoverTimer_Tick(sender As Object, e As EventArgs) Handles _hoverTimer.Tick
        Dim screenY As Integer = Cursor.Position.Y
        Dim screenBounds As Rectangle = Screen.PrimaryScreen.Bounds

        If _isMaximized Then
            If Not titleBarPanel.Visible AndAlso screenY <= screenBounds.Top + 4 Then
                PositionTitleBar()
                titleBarPanel.Visible = True
                titleBarPanel.BringToFront()
            ElseIf titleBarPanel.Visible AndAlso screenY > screenBounds.Top + titleBarPanel.Height + 10 Then
                titleBarPanel.Visible = False
            End If
        Else
            If Not titleBarPanel.Visible Then
                PositionTitleBar()
                titleBarPanel.Visible = True
                titleBarPanel.BringToFront()
            End If
        End If
    End Sub

    Friend Function GetEdgeHit(screenPos As Point) As Integer
        If _isMaximized Then Return 0

        Dim mp As New Point(screenPos.X - Me.Location.X, screenPos.Y - Me.Location.Y)
        Dim cw As Integer = Me.ClientSize.Width
        Dim ch As Integer = Me.ClientSize.Height

        Dim hitLeft As Boolean = mp.X <= BORDERWIDTH
        Dim hitRight As Boolean = mp.X >= cw - BORDERWIDTH
        Dim hitBottom As Boolean = mp.Y >= ch - BORDERWIDTH

        If hitLeft AndAlso hitBottom Then Return HTBOTTOMLEFT
        If hitRight AndAlso hitBottom Then Return HTBOTTOMRIGHT
        If hitLeft Then Return HTLEFT
        If hitRight Then Return HTRIGHT
        If hitBottom Then Return HTBOTTOM

        Return 0
    End Function

    Friend Sub BeginEdgeResize(edge As Integer)
        ReleaseCapture()
        SendMessage(Me.Handle, WM_NCLBUTTONDOWN, edge, 0)
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_NCHITTEST Then
            Dim hit As Integer = GetEdgeHit(Cursor.Position)
            If hit <> 0 Then
                m.Result = New IntPtr(hit)
                Return
            End If
            If Not _isMaximized Then
                Dim mp As New Point(Cursor.Position.X - Me.Location.X, Cursor.Position.Y - Me.Location.Y)
                If mp.Y <= 35 Then
                    m.Result = New IntPtr(HTCAPTION)
                    Return
                End If
            End If
        End If
        MyBase.WndProc(m)
    End Sub

    Private Sub MainShell_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        PositionTitleBar()

        If btnMaximize Is Nothing Then Return

        If Me.WindowState = FormWindowState.Minimized Then
            _wasMaximizedBeforeMinimize = _isMaximized
            titleBarPanel.Visible = False
            Return
        End If

        If _wasMaximizedBeforeMinimize AndAlso Me.WindowState <> FormWindowState.Minimized Then
            _wasMaximizedBeforeMinimize = False
            If Not _isMaximized Then
                _isMaximized = True
                Me.FormBorderStyle = FormBorderStyle.None
                Me.Bounds = Screen.PrimaryScreen.Bounds
                btnMaximize.Text = ChrW(&H2752)
            End If
            Return
        End If

        If Me.WindowState = FormWindowState.Maximized Then
            If Not _isMaximized Then
                _isMaximized = True
                Me.FormBorderStyle = FormBorderStyle.None
                Me.Bounds = Screen.PrimaryScreen.Bounds
                btnMaximize.Text = ChrW(&H2752)
            End If
        ElseIf Me.WindowState = FormWindowState.Normal Then
            If _isMaximized Then
                _isMaximized = False
                btnMaximize.Text = ChrW(&H25A1)
            End If
        End If

        If dragStrip IsNot Nothing Then dragStrip.Visible = Not _isMaximized
    End Sub

    Private Sub ContentPanel_Resize(sender As Object, e As EventArgs)
        If _isShowingPage OrElse _currentPageType Is Nothing Then Return
        If Environment.TickCount - _lastReloadTick < 1000 Then Return
        _resizeDebounceTimer.Stop()
        _resizeDebounceTimer.Start()
    End Sub

    Private Sub _resizeDebounceTimer_Tick(sender As Object, e As EventArgs) Handles _resizeDebounceTimer.Tick
        _resizeDebounceTimer.Stop()
        If _currentPageType IsNot Nothing AndAlso Me.WindowState <> FormWindowState.Minimized Then
            _lastReloadTick = Environment.TickCount
            ShowPage(_currentPageType)
        End If
    End Sub

    Public Sub ShowPage(pageType As Type)
        If pageType Is Nothing Then Return
        pageType = EnforceRoleAccess(pageType)
        _isShowingPage = True

        If _currentPage IsNot Nothing Then
            Dim persistable As IDraftPersistable = TryCast(_currentPage, IDraftPersistable)
            If persistable IsNot Nothing Then
                persistable.PersistDraft()
            End If

            If _childFormHook IsNot Nothing Then
                _childFormHook.ReleaseHandle()
                _childFormHook = Nothing
            End If
            ContentPanel.Controls.Remove(_currentPage)
            _currentPage.Hide()
            _currentPage.Dispose()
            _currentPage = Nothing
        End If

        ContentPanel.Controls.Clear()

        Dim page As Form = CType(Activator.CreateInstance(pageType), Form)
        page.TopLevel = False
        page.FormBorderStyle = FormBorderStyle.None
        page.Dock = DockStyle.Fill
        ContentPanel.Controls.Add(page)
        page.Show()
        page.Activate()
        page.Focus()

        _currentPage = page
        _currentPageType = pageType
        _isShowingPage = False

        _childFormHook = New EdgeHitHook(Me, page)
    End Sub

    ' Persist the current page's draft (e.g. the POS cart) when the shell closes.
    ' Pages are disposed without FormClosing (see IDraftPersistable comment above),
    ' so the shell is the reliable place to save the live page state before exit.
    Private Sub MainShell_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            If _currentPage IsNot Nothing Then
                Dim persistable As IDraftPersistable = TryCast(_currentPage, IDraftPersistable)
                If persistable IsNot Nothing Then
                    persistable.PersistDraft()
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub ShowOverlay(visible As Boolean)
        If _loadingOverlay Is Nothing Then Return
        _loadingOverlay.Visible = visible
        If visible Then
            _loadingOverlay.BringToFront()
        End If
    End Sub

    Public Sub ShowInitialPage()
        Dim role As String = If(frmLoginvb.LoggedInRole, String.Empty).ToUpperInvariant()
        Dim startType As Type = If(role = "STAFF", GetType(Sales), GetType(Dashboard))
        ShowPage(startType)
    End Sub

    ' Central role guard: Staff can only ever open the POS/Sales page and
    ' Managers can open everything except the System Settings tools. Admin is
    ' unrestricted. Guards even programmatic navigation attempts.
    Private Function EnforceRoleAccess(pageType As Type) As Type
        Try
            Dim role As String = If(frmLoginvb.LoggedInRole, String.Empty).ToUpperInvariant()
            If role = "ADMIN" OrElse role = "ADMINISTRATOR" Then Return pageType

            If role = "MANAGER" Then
                If pageType Is GetType(Sys) OrElse
                   pageType Is GetType(TerminalSetup) OrElse
                   pageType Is GetType(SyncLogs) Then
                    Return GetType(Dashboard)
                End If
                Return pageType
            End If

            ' Staff - only POS/Sales is reachable.
            If pageType IsNot GetType(Sales) Then
                Return GetType(Sales)
            End If
        Catch
        End Try
        Return pageType
    End Function

    ' On the admin PC, periodically check for computers waiting for approval and
    ' ask the admin whether to allow or block each one.
    Private Sub _approvalTimer_Tick(sender As Object, e As EventArgs) Handles _approvalTimer.Tick
        Try
            If _approvalPromptOpen Then Return
            If Not TerminalManager.IsSharedMode() Then Return
            If Not TerminalManager.IsAdminMachine() Then Return
            If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then Return

            Dim pendingList As List(Of TerminalManager.TerminalInfo) = TerminalManager.GetPendingTerminals()
            Dim target As TerminalManager.TerminalInfo = pendingList.FirstOrDefault(Function(t) Not _approvalPromptedIds.Contains(t.TerminalID))
            If target Is Nothing Then Return

            Dim displayName As String = If(String.IsNullOrWhiteSpace(target.TerminalName), target.HostName, target.TerminalName)

            _approvalPromptOpen = True
            Try
                Dim result As DialogResult = ShowAccessRequestDialog(displayName, target.HostName)

                ' Never ask about this PC again this session, even if the
                ' admin chose 'Not Now' (they can use the PC Terminals page).
                _approvalPromptedIds.Add(target.TerminalID)

                If result = DialogResult.Yes Then
                    TerminalManager.ApproveTerminal(target.TerminalID)
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Terminal Allowed",
                                       $"Access allowed for PC '{displayName}' ({target.HostName})")
                ElseIf result = DialogResult.No Then
                    TerminalManager.BlockTerminal(target.TerminalID)
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Terminal Blocked",
                                       $"Access blocked for PC '{displayName}' ({target.HostName})")
                End If
            Finally
                _approvalPromptOpen = False
            End Try
        Catch ex As Exception
            Console.WriteLine($"MainShell approval prompt error: {ex.Message}")
        End Try
    End Sub

    ' Modal shown on the admin PC when a new computer asks for access. Laid out
    ' from the client area so the three action buttons stay fully visible and
    ' evenly spaced at any DPI / window size.
    Private Function ShowAccessRequestDialog(displayName As String, hostName As String) As DialogResult
        Dim dlg As New Form() With {
            .Text = "Access Request",
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .ShowInTaskbar = False,
            .BackColor = Color.White,
            .ClientSize = New Size(620, 380)
        }

        ' Coloured header band with title + subtitle
        Dim header As New Panel() With {
            .Dock = DockStyle.Top,
            .Height = 86,
            .BackColor = Color.FromArgb(190, 154, 48)
        }
        Dim lblTitle As New Label() With {
            .Text = "🔐 New Access Request",
            .Font = New Font("Segoe UI", 16, FontStyle.Bold),
            .ForeColor = Color.White,
            .BackColor = Color.Transparent,
            .AutoSize = True,
            .Location = New Point(28, 16)
        }
        Dim lblSub As New Label() With {
            .Text = "A computer is requesting access to the clinic system",
            .Font = New Font("Segoe UI", 9.5, FontStyle.Regular),
            .ForeColor = Color.FromArgb(255, 246, 222),
            .BackColor = Color.Transparent,
            .AutoSize = True,
            .Location = New Point(31, 50)
        }
        header.Controls.Add(lblTitle)
        header.Controls.Add(lblSub)

        Dim lblMessage As New Label() With {
            .Text = "PC '" & displayName & "' (" & hostName & ")" & vbCrLf &
                    "is requesting access to the clinic system." & vbCrLf & vbCrLf &
                    "Allow it only if this is a clinic computer you recognise.",
            .Font = New Font("Segoe UI", 11, FontStyle.Regular),
            .ForeColor = Color.FromArgb(51, 51, 51),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(560, 130),
            .Location = New Point((620 - 560) \ 2, 110),
            .TextAlign = ContentAlignment.MiddleCenter
        }

        ' Three buttons, centred with even gaps, pinned near the bottom
        Dim btnW As Integer = 160
        Dim btnH As Integer = 48
        Dim gap As Integer = 22
        Dim totalW As Integer = (btnW * 3) + (gap * 2)
        Dim startX As Integer = (620 - totalW) \ 2
        Dim btnY As Integer = 380 - btnH - 30

        Dim btnAllow As New Button() With {
            .Text = "Allow Access",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .BackColor = Color.FromArgb(190, 154, 48),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Size = New Size(btnW, btnH),
            .Location = New Point(startX, btnY),
            .Cursor = Cursors.Hand,
            .DialogResult = DialogResult.Yes
        }
        btnAllow.FlatAppearance.BorderSize = 0

        Dim btnBlock As New Button() With {
            .Text = "Block",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .BackColor = Color.FromArgb(255, 71, 87),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Size = New Size(btnW, btnH),
            .Location = New Point(startX + btnW + gap, btnY),
            .Cursor = Cursors.Hand,
            .DialogResult = DialogResult.No
        }
        btnBlock.FlatAppearance.BorderSize = 0

        Dim btnNotNow As New Button() With {
            .Text = "Not Now",
            .Font = New Font("Segoe UI", 11, FontStyle.Regular),
            .BackColor = Color.FromArgb(237, 237, 237),
            .ForeColor = Color.FromArgb(80, 80, 80),
            .FlatStyle = FlatStyle.Flat,
            .Size = New Size(btnW, btnH),
            .Location = New Point(startX + (btnW + gap) * 2, btnY),
            .Cursor = Cursors.Hand,
            .DialogResult = DialogResult.Cancel
        }
        btnNotNow.FlatAppearance.BorderSize = 0

        dlg.Controls.Add(header)
        dlg.Controls.Add(lblMessage)
        dlg.Controls.Add(btnAllow)
        dlg.Controls.Add(btnBlock)
        dlg.Controls.Add(btnNotNow)
        dlg.AcceptButton = btnAllow
        dlg.CancelButton = btnNotNow

        Utilities.EnableEscCloseModal(dlg)
        Return dlg.ShowDialog(Me)
    End Function
End Class

Friend Class EdgeHitHook
    Inherits NativeWindow

    Private Const WM_NCHITTEST As Integer = &H84
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1

    Private ReadOnly _shell As MainShell

    Public Sub New(shell As MainShell, childForm As Form)
        _shell = shell
        If childForm.Handle <> IntPtr.Zero Then
            Me.AssignHandle(childForm.Handle)
        End If
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_NCHITTEST Then
            Dim hit As Integer = _shell.GetEdgeHit(Cursor.Position)
            If hit <> 0 Then
                m.Result = New IntPtr(hit)
                Return
            End If
        ElseIf m.Msg = WM_NCLBUTTONDOWN Then
            Dim hit As Integer = _shell.GetEdgeHit(Cursor.Position)
            If hit <> 0 Then
                _shell.BeginEdgeResize(hit)
                Return
            End If
        End If
        MyBase.WndProc(m)
    End Sub
End Class
