Imports System.Linq
Imports System.Reflection

Public Class MainShell
    Private _currentPage As Form
    Private ReadOnly _loadingOverlay As Panel

    Public Sub New()
        InitializeComponent()
        Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Me.FormBorderStyle = FormBorderStyle.None
        Me.WindowState = FormWindowState.Normal
        Me.Bounds = Screen.PrimaryScreen.Bounds
        Me.WindowState = FormWindowState.Maximized
        ContentPanel.BackColor = Color.FromArgb(26, 29, 31)
        _loadingOverlay = New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.FromArgb(26, 29, 31),
            .Visible = False
        }
    End Sub

    Public Sub ShowPage(pageType As Type)
        If pageType Is Nothing Then Return

        If _currentPage IsNot Nothing Then
            _currentPage.Close()
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
