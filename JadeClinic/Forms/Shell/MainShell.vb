Imports System.Linq

Public Class MainShell
    Private _currentPage As Form

    Public Sub New()
        InitializeComponent()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.TopMost = True
        Me.WindowState = FormWindowState.Normal
        Me.Bounds = Screen.PrimaryScreen.Bounds
        Me.WindowState = FormWindowState.Maximized
    End Sub

    Public Sub ShowPage(pageType As Type)
        If pageType Is Nothing Then
            Return
        End If

        If _currentPage IsNot Nothing Then
            Try
                _currentPage.Close()
            Catch
            End Try
            Try
                _currentPage.Dispose()
            Catch
            End Try
            _currentPage = Nothing
        End If

        Dim page As Form = CType(Activator.CreateInstance(pageType), Form)
        page.TopLevel = False
        page.FormBorderStyle = FormBorderStyle.None
        page.Dock = DockStyle.Fill
        page.StartPosition = FormStartPosition.Manual

        ContentPanel.Controls.Clear()
        ContentPanel.Controls.Add(page)

        _currentPage = page
        page.Show()
        page.BringToFront()
        page.Focus()
    End Sub

    Public Sub ShowInitialPage()
        Dim role As String = If(frmLoginvb.LoggedInRole, String.Empty).ToUpperInvariant()
        Dim startType As Type = If(role = "STAFF", GetType(Sales), GetType(Dashboard))
        ShowPage(startType)
    End Sub
End Class
