Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports System.IO
Imports Guna.UI2.WinForms

' Reusable, lightweight pagination bar for the POS product listings.
'
' Layout (right side): First <<  Previous <  [ Current ]  > Next  >> Last
' Left side shows a subtle item-count label. The only strong-gold element is the
' current-page indicator; the navigation buttons stay minimal and only turn gold
' on hover/press.
'
' The navigation buttons are plain Labels, not Guna2Buttons: this Guna2 build
' renders Guna2Button text white for transparent/white fills, which made the
' gray arrows invisible on the white bar. Labels render their ForeColor exactly.
Public Class PaginationControl
    Inherits UserControl

    ' Jade Clinic palette (matches Sales.vb brand constants)
    Private Shared ReadOnly PrimaryGold As Color = Color.FromArgb(238, 188, 27)     ' #EEBC1B - Current page fill
    Private Shared ReadOnly DeepGold As Color = Color.FromArgb(190, 154, 48)        ' #BE9A30 - Hover icon
    Private Shared ReadOnly SoftGold As Color = Color.FromArgb(251, 247, 236)       ' #FBF7EC - Hover background
    Private Shared ReadOnly BorderColor As Color = Color.FromArgb(232, 232, 232)    ' #E8E8E8 - Container border
    Private Shared ReadOnly IconNormal As Color = Color.FromArgb(136, 136, 136)     ' #888888 - Nav icon normal
    Private Shared ReadOnly IconDisabled As Color = Color.FromArgb(200, 200, 200)   ' #C8C8C8 - Disabled icon
    Private Shared ReadOnly SecondaryText As Color = Color.FromArgb(102, 102, 102)  ' #666666 - Item count

    ' Layout metrics
    Private Const NavWidth As Integer = 32
    Private Const NavHeight As Integer = 36
    Private Const CurrentWidth As Integer = 36
    Private Const CurrentHeight As Integer = 36
    Private Const GapSmall As Integer = 4
    Private Const GapLarge As Integer = 6
    Private Const RightMargin As Integer = 20
    Private Const LeftMargin As Integer = 20

    Private lblItemCount As Label
    Private btnFirst As Label
    Private btnPrevious As Label
    Private btnCurrentPage As Guna2Button
    Private btnNext As Label
    Private btnLast As Label

    ' Public state (read after Configure / on PageChanged)
    Public Property CurrentPage As Integer = 1
    Public Property TotalPages As Integer = 1
    Public Property TotalItems As Integer = 0
    Public Property ItemsPerPage As Integer = 8

    Public Event PageChanged(page As Integer)

    Private Shared _instanceCount As Integer
    Private _instanceId As Integer

    Private Sub LogDiagnostic(message As String)
        Try
            Dim logPath As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "JadeClinic", "diag.log")
            System.IO.File.AppendAllText(logPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {message}{Environment.NewLine}")
        Catch
        End Try
    End Sub

    Public Sub New()
        _instanceCount += 1
        _instanceId = _instanceCount
        LogDiagnostic($"PAG created instanceId={_instanceId} hashCode={Me.GetHashCode()} totalInstances={_instanceCount}")
        Me.BackColor = Color.White
        Me.DoubleBuffered = True
        Me.Size = New Size(480, 62)
        Me.MinimumSize = New Size(360, 62)
        Me.MaximumSize = New Size(0, 62)

        lblItemCount = New Label()
        lblItemCount.Font = New Font("Poppins", 10.5F, FontStyle.Regular)
        lblItemCount.ForeColor = SecondaryText
        lblItemCount.AutoSize = True
        lblItemCount.Text = ""
        Me.Controls.Add(lblItemCount)

        btnFirst = CreateNavButton("<<")
        btnPrevious = CreateNavButton("<")
        btnCurrentPage = CreateCurrentPageButton()
        btnNext = CreateNavButton(">")
        btnLast = CreateNavButton(">>")

        AddHandler btnFirst.Click, Sub(s, e) SetPage(1)
        AddHandler btnPrevious.Click, Sub(s, e) SetPage(CurrentPage - 1)
        AddHandler btnNext.Click, Sub(s, e) SetPage(CurrentPage + 1)
        AddHandler btnLast.Click, Sub(s, e) SetPage(TotalPages)

        Me.Controls.Add(btnFirst)
        Me.Controls.Add(btnPrevious)
        Me.Controls.Add(btnCurrentPage)
        Me.Controls.Add(btnNext)
        Me.Controls.Add(btnLast)

        AddHandler Me.SizeChanged, Sub(s, e) LayoutControls()

        LayoutControls()
        RefreshState()
    End Sub

    ' Sets up the pagination for a listing. Does not raise PageChanged.
    Public Sub Configure(totalItemsCount As Integer, itemsPerPageCount As Integer, Optional startPage As Integer = 1)
        Me.TotalItems = Math.Max(0, totalItemsCount)
        Me.ItemsPerPage = Math.Max(1, itemsPerPageCount)
        Me.TotalPages = If(Me.TotalItems = 0, 1, CInt(Math.Ceiling(CDbl(Me.TotalItems) / Me.ItemsPerPage)))
        Me.CurrentPage = Math.Max(1, Math.Min(startPage, Me.TotalPages))
        LogDiagnostic($"PAG Configure instanceId={_instanceId} totalItems={Me.TotalItems} itemsPerPage={Me.ItemsPerPage} totalPages={Me.TotalPages} currentPage={Me.CurrentPage}")
        LayoutControls()
        RefreshState()
    End Sub

    ' Moves to the given page (clamped) and raises PageChanged if it changed.
    Private Sub SetPage(page As Integer)
        Dim newPage As Integer = Math.Max(1, Math.Min(page, TotalPages))
        If newPage = CurrentPage Then Return
        CurrentPage = newPage
        RefreshState()
        RaiseEvent PageChanged(CurrentPage)
    End Sub

    Private Function CreateNavButton(text As String) As Label
        Dim btn As New Label()
        btn.Size = New Size(NavWidth, NavHeight)
        btn.Text = text
        btn.Font = New Font("Poppins", 12.0F, FontStyle.Regular)
        btn.TextAlign = ContentAlignment.MiddleCenter
        btn.ForeColor = IconNormal
        btn.BackColor = Color.White
        btn.Cursor = Cursors.Hand
        btn.TabStop = False
        btn.Tag = True ' enabled state

        ' Hover: soft gold background + deep gold arrow (only when enabled)
        AddHandler btn.MouseEnter, Sub(s, e)
                                       If CBool(btn.Tag) Then
                                           btn.BackColor = SoftGold
                                           btn.ForeColor = DeepGold
                                       End If
                                   End Sub
        AddHandler btn.MouseLeave, Sub(s, e)
                                       btn.BackColor = Color.White
                                       btn.ForeColor = If(CBool(btn.Tag), IconNormal, IconDisabled)
                                   End Sub
        Return btn
    End Function

    ' Toggles the visual enabled state of a nav label (clicks are still safe:
    ' SetPage clamps to valid pages, so disabled clicks are harmless no-ops)
    Private Sub SetNavEnabled(btn As Label, enabled As Boolean)
        btn.Tag = enabled
        btn.ForeColor = If(enabled, IconNormal, IconDisabled)
        btn.Cursor = If(enabled, Cursors.Hand, Cursors.Default)
    End Sub

    Private Function CreateCurrentPageButton() As Guna2Button
        Dim btn As New Guna2Button()
        btn.Size = New Size(CurrentWidth, CurrentHeight)
        btn.Text = "1"
        btn.Font = New Font("Poppins", 10.5F, FontStyle.Bold)
        btn.FillColor = PrimaryGold
        btn.ForeColor = Color.White
        btn.BorderThickness = 0
        btn.BorderRadius = 9
        btn.Cursor = Cursors.Default
        Return btn
    End Function

    Private Sub LayoutControls()
        If lblItemCount Is Nothing Then Return

        Dim top As Integer = (Me.ClientSize.Height - CurrentHeight) \ 2

        btnLast.Location = New Point(Me.ClientSize.Width - RightMargin - NavWidth, top)
        btnNext.Location = New Point(btnLast.Left - GapSmall - NavWidth, top)
        btnCurrentPage.Location = New Point(btnNext.Left - GapLarge - CurrentWidth, top)
        btnPrevious.Location = New Point(btnCurrentPage.Left - GapLarge - NavWidth, top)
        btnFirst.Location = New Point(btnPrevious.Left - GapSmall - NavWidth, top)

        lblItemCount.AutoSize = True
        lblItemCount.Location = New Point(LeftMargin, (Me.ClientSize.Height - lblItemCount.PreferredHeight) \ 2)
    End Sub

    Private Sub RefreshState()
        If lblItemCount Is Nothing Then Return

        Dim page As Integer = Math.Max(1, Math.Min(CurrentPage, TotalPages))
        CurrentPage = page

        Dim startIndex As Integer = If(TotalItems = 0, 0, (page - 1) * ItemsPerPage + 1)
        Dim endIndex As Integer = If(TotalItems = 0, 0, Math.Min(TotalItems, page * ItemsPerPage))
        lblItemCount.Text = $"Showing {startIndex} to {endIndex} of {TotalItems} items"
        LogDiagnostic($"PAG Refresh instanceId={_instanceId} totalItems={TotalItems} page={page} label='{lblItemCount.Text}'")

        btnCurrentPage.Text = page.ToString()
        SetNavEnabled(btnFirst, page > 1)
        SetNavEnabled(btnPrevious, page > 1)
        SetNavEnabled(btnNext, page < TotalPages)
        SetNavEnabled(btnLast, page < TotalPages)
    End Sub

    Private Function CreateRoundedRect(rect As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim d As Integer = radius * 2
        path.AddArc(rect.X, rect.Y, d, d, 180, 90)
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90)
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90)
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        If Me.ClientSize.Width <= 2 OrElse Me.ClientSize.Height <= 2 Then Return
        Dim rect As New Rectangle(1, 1, Me.ClientSize.Width - 2, Me.ClientSize.Height - 2)
        Using path As GraphicsPath = CreateRoundedRect(rect, 12)
            Using pen As New Pen(BorderColor, 1.0F)
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
                e.Graphics.DrawPath(pen, path)
            End Using
        End Using
    End Sub
End Class
