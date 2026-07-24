Imports System.Linq
Imports System.Reflection
Imports System.Runtime.InteropServices

Public Class MainShell
    Private _currentPage As Form
    Private _currentPageType As Type
    Private ReadOnly _loadingOverlay As Panel
    Private _isMaximized As Boolean = False
    Private _wasMaximizedBeforeMinimize As Boolean = False
    Private _isShowingPage As Boolean = False
    Private WithEvents _hoverTimer As New Timer() With {.Interval = 100}
    Private WithEvents _resizeDebounceTimer As New Timer() With {.Interval = 300, .Enabled = False}

    Private titleBarPanel As Guna.UI2.WinForms.Guna2Panel
    Private btnMinimize As Guna.UI2.WinForms.Guna2Button
    Private btnMaximize As Guna.UI2.WinForms.Guna2Button
    Private btnCloseTitle As Guna.UI2.WinForms.Guna2Button

    Private Const WM_NCHITTEST As Integer = &H84
    Private Const HTLEFT As Integer = 10
    Private Const HTRIGHT As Integer = 11
    Private Const HTTOP As Integer = 12
    Private Const HTTOPLEFT As Integer = 13
    Private Const HTTOPRIGHT As Integer = 14
    Private Const HTBOTTOM As Integer = 15
    Private Const HTBOTTOMLEFT As Integer = 16
    Private Const HTBOTTOMRIGHT As Integer = 17
    Private Const HTCAPTION As Integer = 2
    Private Const BORDERWIDTH As Integer = 6

    Public Sub New()
        InitializeComponent()
        Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        ContentPanel.BackColor = Color.FromArgb(26, 29, 31)
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
        titleBarPanel = New Guna.UI2.WinForms.Guna2Panel() With {
            .Dock = DockStyle.Top,
            .Height = 35,
            .FillColor = Color.White,
            .Visible = False
        }

        btnCloseTitle = New Guna.UI2.WinForms.Guna2Button() With {
            .Dock = DockStyle.Right,
            .Width = 46,
            .FillColor = Color.Transparent,
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
                                                 btnCloseTitle.FillColor = Color.Transparent
                                                 btnCloseTitle.ForeColor = Color.FromArgb(42, 42, 42)
                                             End Sub

        btnMaximize = New Guna.UI2.WinForms.Guna2Button() With {
            .Dock = DockStyle.Right,
            .Width = 46,
            .FillColor = Color.Transparent,
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
                                              Me.Bounds = New Rectangle(
                                                  CInt(Screen.PrimaryScreen.WorkingArea.Width * 0.1),
                                                  CInt(Screen.PrimaryScreen.WorkingArea.Height * 0.1),
                                                  CInt(Screen.PrimaryScreen.WorkingArea.Width * 0.8),
                                                  CInt(Screen.PrimaryScreen.WorkingArea.Height * 0.8))
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
                                               btnMaximize.FillColor = Color.Transparent
                                           End Sub

        btnMinimize = New Guna.UI2.WinForms.Guna2Button() With {
            .Dock = DockStyle.Right,
            .Width = 46,
            .FillColor = Color.Transparent,
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
                                               btnMinimize.FillColor = Color.Transparent
                                           End Sub

        titleBarPanel.Controls.Add(btnCloseTitle)
        titleBarPanel.Controls.Add(btnMaximize)
        titleBarPanel.Controls.Add(btnMinimize)

        Me.Controls.Add(titleBarPanel)
        titleBarPanel.BringToFront()
    End Sub

    Private Sub _hoverTimer_Tick(sender As Object, e As EventArgs) Handles _hoverTimer.Tick
        Dim screenY As Integer = Cursor.Position.Y
        Dim screenBounds As Rectangle = Screen.PrimaryScreen.Bounds

        If _isMaximized Then
            If Not titleBarPanel.Visible AndAlso screenY <= screenBounds.Top + 4 Then
                titleBarPanel.Visible = True
                titleBarPanel.BringToFront()
            ElseIf titleBarPanel.Visible AndAlso screenY > screenBounds.Top + titleBarPanel.Height + 10 Then
                titleBarPanel.Visible = False
            End If
        Else
            If Not titleBarPanel.Visible Then
                titleBarPanel.Visible = True
                titleBarPanel.BringToFront()
            End If
        End If
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_NCHITTEST AndAlso Me.WindowState = FormWindowState.Normal Then
            Dim mp As Point = Me.PointToClient(Cursor.Position)

            Dim onLeft As Boolean = mp.X < BORDERWIDTH
            Dim onRight As Boolean = mp.X > Me.ClientSize.Width - BORDERWIDTH
            Dim onTop As Boolean = mp.Y < BORDERWIDTH
            Dim onBottom As Boolean = mp.Y > Me.ClientSize.Height - BORDERWIDTH

            If onTop AndAlso onLeft Then
                m.Result = HTTOPLEFT
                Return
            ElseIf onTop AndAlso onRight Then
                m.Result = HTTOPRIGHT
                Return
            ElseIf onBottom AndAlso onLeft Then
                m.Result = HTBOTTOMLEFT
                Return
            ElseIf onBottom AndAlso onRight Then
                m.Result = HTBOTTOMRIGHT
                Return
            ElseIf onTop Then
                m.Result = HTTOP
                Return
            ElseIf onBottom Then
                m.Result = HTBOTTOM
                Return
            ElseIf onLeft Then
                m.Result = HTLEFT
                Return
            ElseIf onRight Then
                m.Result = HTRIGHT
                Return
            End If
        End If

        MyBase.WndProc(m)
    End Sub

    Private Sub MainShell_Resize(sender As Object, e As EventArgs) Handles Me.Resize
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
        _resizeDebounceTimer.Stop()
        _resizeDebounceTimer.Start()
    End Sub

    Private Sub _resizeDebounceTimer_Tick(sender As Object, e As EventArgs) Handles _resizeDebounceTimer.Tick
        _resizeDebounceTimer.Stop()
        If _currentPageType IsNot Nothing AndAlso Me.WindowState <> FormWindowState.Minimized Then
            ShowPage(_currentPageType)
        End If
    End Sub

    Public Sub ShowPage(pageType As Type)
        If pageType Is Nothing Then Return
        _isShowingPage = True

        If _currentPage IsNot Nothing Then
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
