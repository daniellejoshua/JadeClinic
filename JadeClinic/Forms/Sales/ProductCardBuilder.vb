Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms

' Product cards are built here manually from plain WinForms controls (Panel,
' PictureBox, Label) and painted with GDI+. No Guna2 controls are used, so there
' is no hidden default shadow / border that can paint a dark ring around a card.
' Sales.vb keeps using a List(Of Control); the card's Tag carries a
' ProductCardInfo object exposing ProductId / DisplayedStock / UpdateStock.
Public Module ProductCardBuilder

    Private Const CardWidth As Integer = 230
    Private Const CardHeight As Integer = 380
    Private Const CardRadius As Integer = 12

    ' The card face is inset slightly so a soft shadow can peek out bottom-right.
    Private Const FaceWidth As Integer = CardWidth - 10
    Private Const FaceHeight As Integer = CardHeight - 12

    Private ReadOnly ColorGoldenYellow As Color = Color.FromArgb(254, 191, 16)      ' #FECF10 - Primary brand gold
    Private ReadOnly ColorJadeOlive As Color = Color.FromArgb(190, 154, 48)         ' #BE9A30 - Secondary gold accent
    Private ReadOnly ColorDarkText As Color = Color.FromArgb(51, 51, 51)            ' #333333 - Primary text
    Private ReadOnly ColorMutedText As Color = Color.FromArgb(150, 150, 150)        ' Muted gray for codes
    Private ReadOnly ColorSuccessGreen As Color = Color.FromArgb(16, 216, 98)       ' #10D862 - Stock OK
    Private ReadOnly ColorAmber As Color = Color.FromArgb(240, 173, 24)             ' Low stock warning
    Private ReadOnly ColorAlertRed As Color = Color.FromArgb(255, 71, 87)           ' #FF4757 - Out of stock
    Private ReadOnly ColorCardBorder As Color = Color.FromArgb(233, 231, 226)       ' Light warm-gray border
    Private ReadOnly ColorHoverFill As Color = Color.FromArgb(253, 251, 243)        ' Warm gold tint on hover
    Private ReadOnly ColorImageFill As Color = Color.FromArgb(251, 247, 236)        ' #FBF7EC - Faint yellow circle behind image

    ' Build a product card control with the exact same layout as before, rendered
    ' entirely with plain controls. onProductClicked is raised when the card (or
    ' any of its child controls) is clicked.
    Public Function Create(productData As Dictionary(Of String, Object),
                           productImage As Image,
                           onProductClicked As Action) As Control
        Dim productId As String = ""
        If productData IsNot Nothing AndAlso productData.ContainsKey("ProductID") Then
            productId = productData("ProductID").ToString()
        End If
        Dim productName As String = ""
        If productData IsNot Nothing AndAlso productData.ContainsKey("ProductName") Then
            productName = productData("ProductName").ToString()
        End If

        Dim info As New ProductCardInfo() With {
            .ProductId = productId,
            .ProductData = productData
        }

        ' Card surface: plain white Panel with a thin rounded border painted by hand.
        Dim card As New Panel()
        card.Size = New Size(CardWidth, CardHeight)
        card.Margin = New Padding(8, 10, 8, 10)
        card.BackColor = Color.White
        card.Tag = info
        AddHandler card.Paint, AddressOf PaintCardBorder
        WireCard(card, card, info, onProductClicked)

        ' Circular product image (plain PictureBox clipped to an ellipse region).
        Dim pb As New PictureBox()
        pb.Size = New Size(100, 100)
        pb.Location = New Point((FaceWidth - 100) \ 2, 28)
        pb.SizeMode = PictureBoxSizeMode.Zoom
        pb.BackColor = ColorImageFill
        Try
            Using path As New GraphicsPath()
                path.AddEllipse(0, 0, pb.Width - 1, pb.Height - 1)
                pb.Region = New Region(path)
            End Using
        Catch
        End Try
        If productImage IsNot Nothing Then
            pb.Image = productImage
        Else
            Try
                pb.Image = My.Resources.product_placeholder
            Catch
            End Try
        End If
        card.Controls.Add(pb)
        info.ImageBox = pb
        WireCard(pb, card, info, onProductClicked)

        ' Product name (2 fixed lines, ellipsis)
        Dim lblName As New Label()
        lblName.Location = New Point(15, 152)
        lblName.Size = New Size(190, 40)
        lblName.Font = New Font(ResolveFontFamily({"Poppins SemiBold", "Poppins", "Segoe UI"}), 10.0F, FontStyle.Regular)
        lblName.ForeColor = ColorDarkText
        lblName.BackColor = Color.Transparent
        lblName.AutoEllipsis = True
        lblName.TextAlign = ContentAlignment.TopLeft
        lblName.Text = productName
        card.Controls.Add(lblName)
        info.NameLabel = lblName
        WireCard(lblName, card, info, onProductClicked)

        ' Price (gold, no label prefix)
        Dim lblPrice As New Label()
        lblPrice.Location = New Point(15, 210)
        lblPrice.AutoSize = True
        lblPrice.MaximumSize = New Size(190, 0)
        lblPrice.Font = New Font(ResolveFontFamily({"Poppins SemiBold", "Poppins", "Segoe UI"}), 12.0F, FontStyle.Regular)
        lblPrice.ForeColor = ColorJadeOlive
        lblPrice.BackColor = Color.Transparent
        Dim price As Decimal = 0D
        If productData IsNot Nothing AndAlso productData.ContainsKey("Price") Then
            Try
                price = Convert.ToDecimal(productData("Price"))
            Catch
            End Try
        End If
        lblPrice.Text = $"₱{price.ToString("N2")}"
        card.Controls.Add(lblPrice)
        WireCard(lblPrice, card, info, onProductClicked)

        ' Unit (muted, small, below price)
        Dim lblUnit As New Label()
        lblUnit.Location = New Point(15, 240)
        lblUnit.Size = New Size(190, 18)
        lblUnit.Font = New Font(ResolveFontFamily({"Poppins", "Segoe UI"}), 9.0F, FontStyle.Regular)
        lblUnit.ForeColor = Color.FromArgb(90, 90, 90)
        lblUnit.BackColor = Color.Transparent
        lblUnit.TextAlign = ContentAlignment.TopLeft
        Dim unit As String = ""
        If productData IsNot Nothing AndAlso productData.ContainsKey("Unit") Then
            unit = productData("Unit").ToString()
        End If
        lblUnit.Text = If(String.IsNullOrEmpty(unit), "", $"Unit: {unit}")
        If lblUnit.Text <> "" Then
            card.Controls.Add(lblUnit)
            WireCard(lblUnit, card, info, onProductClicked)
        End If

        ' Product code (muted, truncated)
        Dim lblCode As New Label()
        lblCode.Location = New Point(15, 270)
        lblCode.Size = New Size(190, 16)
        lblCode.Font = New Font(ResolveFontFamily({"Poppins", "Segoe UI"}), 8.0F, FontStyle.Regular)
        lblCode.ForeColor = ColorMutedText
        lblCode.BackColor = Color.Transparent
        lblCode.AutoEllipsis = True
        lblCode.TextAlign = ContentAlignment.TopLeft
        Dim code As String = ""
        If productData IsNot Nothing AndAlso productData.ContainsKey("ProductCode") Then
            code = productData("ProductCode").ToString()
        End If
        lblCode.Text = $"Code: {code}"
        card.Controls.Add(lblCode)
        WireCard(lblCode, card, info, onProductClicked)

        ' Subtle divider separating product info from the stock footer
        Dim divider As New Panel()
        divider.Location = New Point(15, 310)
        divider.Size = New Size(190, 1)
        divider.BackColor = ColorCardBorder
        card.Controls.Add(divider)
        WireCard(divider, card, info, onProductClicked)

        ' Stock (green default; amber low; red out) - color managed by ProductCardInfo.UpdateStock
        Dim lblStock As New Label()
        lblStock.Location = New Point(15, 330)
        lblStock.AutoSize = True
        lblStock.Font = New Font(ResolveFontFamily({"Poppins SemiBold", "Poppins", "Segoe UI"}), 9.5F, FontStyle.Regular)
        lblStock.ForeColor = ColorSuccessGreen
        lblStock.BackColor = Color.Transparent
        card.Controls.Add(lblStock)
        info.StockLabel = lblStock
        WireCard(lblStock, card, info, onProductClicked)

        Dim tip As New ToolTip()
        tip.SetToolTip(lblName, productName)
        tip.SetToolTip(pb, productName)

        Return card
    End Function

    Public Function GetProductId(card As Control) As String
        Dim info = TryCast(card.Tag, ProductCardInfo)
        Return If(info Is Nothing, "", info.ProductId)
    End Function

    Public Sub UpdateStock(card As Control, stock As Integer)
        Dim info = TryCast(card.Tag, ProductCardInfo)
        If info IsNot Nothing Then info.UpdateStock(stock)
    End Sub

    Public Function GetDisplayedStock(card As Control) As Integer
        Dim info = TryCast(card.Tag, ProductCardInfo)
        Return If(info Is Nothing, 0, info.DisplayedStock)
    End Function

    ' Whole card clickable + hover-aware, including every child control.
    Private Sub WireCard(c As Control, card As Control, info As ProductCardInfo, onProductClicked As Action)
        c.Cursor = Cursors.Hand
        AddHandler c.Click, Sub()
                                If onProductClicked IsNot Nothing Then onProductClicked.Invoke()
                            End Sub
        AddHandler c.MouseEnter, Sub()
                                     info.IsHovered = True
                                     card.BackColor = ColorHoverFill
                                     card.Invalidate()
                                 End Sub
        AddHandler c.MouseLeave, Sub()
                                     If Not CursorWithin(card) Then
                                         info.IsHovered = False
                                         card.BackColor = Color.White
                                         card.Invalidate()
                                     End If
                                 End Sub
    End Sub

    Private Function CursorWithin(card As Control) As Boolean
        Try
            Dim pt As Point = card.PointToClient(Control.MousePosition)
            Return card.ClientRectangle.Contains(pt)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub PaintCardBorder(sender As Object, e As PaintEventArgs)
        Dim card = TryCast(sender, Control)
        If card Is Nothing Then Return
        Dim info = TryCast(card.Tag, ProductCardInfo)
        Dim hovering As Boolean = info IsNot Nothing AndAlso info.IsHovered
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias

        ' Very subtle soft shadow: a single radial fade from near the face edge to
        ' fully transparent at the card boundary. No hard edges, no dark outline.
        PaintSoftShadow(e.Graphics, New Rectangle(0, 0, CardWidth, CardHeight), CardRadius, hovering)

        ' Card face (inset so the soft shadow stays visible on the right/bottom).
        Dim faceRect As New Rectangle(0, 0, FaceWidth, FaceHeight)
        Dim fillColor As Color = If(hovering, ColorHoverFill, Color.White)
        Using path = CreateRoundRectPath(faceRect, CardRadius)
            Using brush As New SolidBrush(fillColor)
                e.Graphics.FillPath(brush, path)
            End Using
        End Using

        ' Border
        Dim borderColor As Color = If(hovering, ColorGoldenYellow, ColorCardBorder)
        Dim thickness As Integer = If(hovering, 2, 1)
        Using path = CreateRoundRectPath(faceRect, CardRadius)
            Using pen As New Pen(borderColor, thickness)
                pen.Alignment = PenAlignment.Inset
                e.Graphics.DrawPath(pen, path)
            End Using
        End Using
    End Sub

    ' Shared soft drop shadow used by product cards and category tiles. Radial
    ' fade from ~10% opacity near the face edge to fully transparent at the
    ' control boundary. Slightly stronger on hover. Never a harsh dark edge.
    Private Sub PaintSoftShadow(g As Graphics, bounds As Rectangle, radius As Integer, hovering As Boolean)
        Dim alpha As Integer = If(hovering, 34, 26)
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using path = CreateRoundRectPath(bounds, radius)
            Using brush As New PathGradientBrush(path)
                brush.CenterColor = Color.FromArgb(alpha, 0, 0, 0)
                brush.SurroundColors = New Color() {Color.FromArgb(0, 0, 0, 0)}
                brush.FocusScales = New PointF(0.9F, 0.92F)
                g.FillPath(brush, path)
            End Using
        End Using
    End Sub

    ' Builds a shadow host for a category tile: a plain Panel that paints the
    ' same soft radial shadow as the product cards. The tile face (a Guna2Button)
    ' is added as a child on top, so the shadow peeks out on the right/bottom.
    Public Function CreateSoftShadowHost(hostSize As Size, radius As Integer) As Panel
        Dim host As New Panel()
        host.Size = hostSize
        host.BackColor = Color.White
        host.Tag = New CategoryTileInfo()
        AddHandler host.Paint, Sub(s As Object, ev As PaintEventArgs)
                                   Dim h = TryCast(s, Panel)
                                   If h Is Nothing Then Return
                                   Dim info = TryCast(h.Tag, CategoryTileInfo)
                                   Dim hovering As Boolean = info IsNot Nothing AndAlso info.IsHovered
                                   PaintSoftShadow(ev.Graphics, New Rectangle(0, 0, h.Width, h.Height), radius, hovering)
                               End Sub
        Return host
    End Function

    ' Update the shadow strength on a tile host (called from tile hover handlers).
    Public Sub SetSoftShadowHover(host As Control, hovering As Boolean)
        Dim h = TryCast(host, Panel)
        If h Is Nothing Then Return
        Dim info = TryCast(h.Tag, CategoryTileInfo)
        If info Is Nothing Then Return
        info.IsHovered = hovering
        h.Invalidate()
    End Sub

    Private Function CreateRoundRectPath(r As Rectangle, radius As Integer) As GraphicsPath
        Dim path As New GraphicsPath()
        Dim d As Integer = radius * 2
        path.AddArc(r.X, r.Y, d, d, 180, 90)
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90)
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90)
        path.CloseFigure()
        Return path
    End Function

    Private Function ResolveFontFamily(priorityNames As String()) As String
        Try
            Dim installed As New HashSet(Of String)()
            For Each family As FontFamily In System.Drawing.FontFamily.Families
                installed.Add(family.Name)
            Next
            For Each name As String In priorityNames
                If installed.Contains(name) Then Return name
            Next
        Catch
        End Try
        Return "Segoe UI"
    End Function
End Module

' Payload attached to each card's Tag so Sales.vb can look up ProductId and
' update / read stock without knowing anything about the card's internals.
Public Class ProductCardInfo
    Public Property ProductId As String = ""
    Public Property ProductData As Dictionary(Of String, Object)
    Public Property StockLabel As Label
    Public Property NameLabel As Label
    Public Property ImageBox As PictureBox
    Public IsHovered As Boolean

    Public ReadOnly Property DisplayedStock As Integer
        Get
            Dim text As String = If(StockLabel Is Nothing, "0", StockLabel.Text.Replace("Stock:", "").Trim())
            Dim stock As Integer = 0
            Integer.TryParse(text, stock)
            Return stock
        End Get
    End Property

    Public Sub UpdateStock(stock As Integer)
        If StockLabel Is Nothing Then Return
        StockLabel.Text = $"Stock: {stock}"
        If stock <= 0 Then
            StockLabel.ForeColor = Color.FromArgb(255, 71, 87)
        ElseIf stock <= 8 Then
            StockLabel.ForeColor = Color.FromArgb(240, 173, 24)
        Else
            StockLabel.ForeColor = Color.FromArgb(16, 216, 98)
        End If
    End Sub
End Class

' Hover state attached to a category tile's shadow host so the soft shadow can
' strengthen slightly on hover.
Public Class CategoryTileInfo
    Public IsHovered As Boolean
End Class
