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
        _hoverTimer.Start()
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

        Dim mp As Point = Me.PointToClient(screenPos)
        Dim cw As Integer = Me.ClientSize.Width
        Dim ch As Integer = Me.ClientSize.Height

        Dim hitLeft As Boolean = mp.X <= BORDERWIDTH
        Dim hitRight As Boolean = mp.X >= cw - BORDERWIDTH
        Dim hitTop As Boolean = mp.Y <= BORDERWIDTH
        Dim hitBottom As Boolean = mp.Y >= ch - BORDERWIDTH

        If hitLeft AndAlso hitTop Then Return HTTOPLEFT
        If hitRight AndAlso hitTop Then Return HTTOPRIGHT
        If hitLeft AndAlso hitBottom Then Return HTBOTTOMLEFT
        If hitRight AndAlso hitBottom Then Return HTBOTTOMRIGHT
        If hitLeft Then Return HTLEFT
        If hitRight Then Return HTRIGHT
        If hitTop Then Return HTTOP
        If hitBottom Then Return HTBOTTOM

        Return 0
    End Function

    Friend Sub BeginEdgeResize(edge As Integer)
        ReleaseCapture()
        SendMessage(Me.Handle, WM_NCLBUTTONDOWN, edge, 0)
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
