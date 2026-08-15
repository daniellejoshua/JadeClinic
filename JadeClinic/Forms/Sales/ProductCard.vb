Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Windows.Forms
Imports Guna.UI2.WinForms

Public Class ProductCard
    Inherits UserControl

    Public Event ProductClicked As EventHandler

    ' Jade Clinic palette (mirrors Sales.vb constants; self-contained so this control has no form dependency)
    Private Shared ReadOnly ColorGoldenYellow As Color = Color.FromArgb(254, 191, 16)      ' #FECF10 - Primary brand gold
    Private Shared ReadOnly ColorJadeOlive As Color = Color.FromArgb(190, 154, 48)         ' #BE9A30 - Secondary gold accent
    Private Shared ReadOnly ColorOffWhite As Color = Color.FromArgb(249, 249, 249)         ' #F9F9F9 - Main background
    Private Shared ReadOnly ColorDarkText As Color = Color.FromArgb(51, 51, 51)            ' #333333 - Primary text
    Private Shared ReadOnly ColorMutedText As Color = Color.FromArgb(150, 150, 150)        ' Muted gray for codes
    Private Shared ReadOnly ColorSuccessGreen As Color = Color.FromArgb(16, 216, 98)       ' #10D862 - Stock OK
    Private Shared ReadOnly ColorAmber As Color = Color.FromArgb(240, 173, 24)             ' Low stock warning
    Private Shared ReadOnly ColorAlertRed As Color = Color.FromArgb(255, 71, 87)           ' #FF4757 - Out of stock
    Private Shared ReadOnly ColorCardBorder As Color = Color.FromArgb(233, 231, 226)       ' Light warm-gray border
    Private Shared ReadOnly ColorHoverFill As Color = Color.FromArgb(253, 251, 243)        ' Warm gold tint on hover
    Private Shared ReadOnly ColorImageFill As Color = Color.FromArgb(246, 242, 228)        ' Yellowish-gray circle behind image

    Private Const CardWidth As Integer = 215
    Private Const CardHeight As Integer = 285

    Private ReadOnly _cardPanel As Guna2Panel
    Private ReadOnly _pbProduct As Guna2PictureBox
    Private ReadOnly _lblName As Label
    Private ReadOnly _lblPrice As Label
    Private ReadOnly _lblCode As Label
    Private ReadOnly _lblStock As Label
    Private ReadOnly _lineDivider As Panel
    Private ReadOnly _tip As ToolTip

    Private _productData As Dictionary(Of String, Object)
    Private _productId As String = ""
    Private _isHovering As Boolean = False

    Public Sub New()
        SetStyle(ControlStyles.SupportsTransparentBackColor OrElse ControlStyles.OptimizedDoubleBuffer, True)
        Me.Size = New Size(CardWidth, CardHeight)
        Me.Margin = New Padding(8, 10, 8, 10)
        Me.BackColor = Color.White

        _tip = New ToolTip()

        ' Root panel: white, rounded, subtle warm-gray border, soft shadow
        ' Inset a few px so the shadow is not clipped by the UserControl bounds.
        _cardPanel = New Guna2Panel()
        _cardPanel.Size = New Size(CardWidth - 6, CardHeight - 8)
        _cardPanel.Location = New Point(3, 3)
        _cardPanel.FillColor = Color.White
        _cardPanel.BorderThickness = 0
        _cardPanel.BorderRadius = 12
        _cardPanel.Cursor = Cursors.Hand
        Me.Controls.Add(_cardPanel)

        ' Product image
        _pbProduct = New Guna2PictureBox()
        _pbProduct.Size = New Size(90, 90)
        _pbProduct.Location = New Point((_cardPanel.Width - 90) \ 2, 18)
        _pbProduct.SizeMode = PictureBoxSizeMode.Zoom
        _pbProduct.FillColor = ColorImageFill
        _pbProduct.BackColor = Color.Transparent
        _pbProduct.BorderRadius = 45
        _pbProduct.Cursor = Cursors.Hand
        _cardPanel.Controls.Add(_pbProduct)

        ' Product name (2 fixed lines, ellipsis)
        _lblName = New Label()
        _lblName.Location = New Point(14, 122)
        _lblName.Size = New Size(_cardPanel.Width - 28, 36)
        _lblName.Font = New Font(ResolveFontFamily({"Poppins SemiBold", "Poppins", "Segoe UI"}), 9.5F, FontStyle.Regular)
        _lblName.ForeColor = ColorDarkText
        _lblName.BackColor = Color.Transparent
        _lblName.AutoEllipsis = True
        _lblName.TextAlign = ContentAlignment.TopLeft
        _lblName.Cursor = Cursors.Hand
        _cardPanel.Controls.Add(_lblName)

        ' Price (gold, no label prefix)
        _lblPrice = New Label()
        _lblPrice.Location = New Point(14, 172)
        _lblPrice.AutoSize = True
        _lblPrice.MaximumSize = New Size(_cardPanel.Width - 28, 0)
        _lblPrice.Font = New Font(ResolveFontFamily({"Poppins SemiBold", "Poppins", "Segoe UI"}), 11.5F, FontStyle.Regular)
        _lblPrice.ForeColor = ColorJadeOlive
        _lblPrice.BackColor = Color.Transparent
        _lblPrice.Cursor = Cursors.Hand
        _cardPanel.Controls.Add(_lblPrice)

        ' Product code (muted, truncated)
        _lblCode = New Label()
        _lblCode.Location = New Point(14, 202)
        _lblCode.Size = New Size(_cardPanel.Width - 28, 16)
        _lblCode.Font = New Font(ResolveFontFamily({"Poppins", "Segoe UI"}), 8.0F, FontStyle.Regular)
        _lblCode.ForeColor = ColorMutedText
        _lblCode.BackColor = Color.Transparent
        _lblCode.AutoEllipsis = True
        _lblCode.TextAlign = ContentAlignment.TopLeft
        _lblCode.Cursor = Cursors.Hand
        _cardPanel.Controls.Add(_lblCode)

        ' Subtle divider separating product info from the stock footer
        _lineDivider = New Panel()
        _lineDivider.Location = New Point(14, 224)
        _lineDivider.Size = New Size(_cardPanel.Width - 28, 1)
        _lineDivider.BackColor = ColorCardBorder
        _lineDivider.Cursor = Cursors.Hand
        _cardPanel.Controls.Add(_lineDivider)

        ' Stock (green default; amber low; red out)
        _lblStock = New Label()
        _lblStock.Location = New Point(14, 254)
        _lblStock.AutoSize = True
        _lblStock.Font = New Font(ResolveFontFamily({"Poppins SemiBold", "Poppins", "Segoe UI"}), 9.5F, FontStyle.Regular)
        _lblStock.ForeColor = ColorSuccessGreen
        _lblStock.BackColor = Color.Transparent
        _lblStock.Cursor = Cursors.Hand
        _cardPanel.Controls.Add(_lblStock)

        WireCardEvents(_cardPanel)
    End Sub

    Public ReadOnly Property ProductId As String
        Get
            Return _productId
        End Get
    End Property

    Public ReadOnly Property DisplayedStock As Integer
        Get
            Dim text As String = _lblStock.Text.Replace("Stock:", "").Trim()
            Dim stock As Integer = 0
            Integer.TryParse(text, stock)
            Return stock
        End Get
    End Property

    Public Property ProductData As Dictionary(Of String, Object)
        Get
            Return _productData
        End Get
        Set(value As Dictionary(Of String, Object))
            _productData = value
            _productId = ""
            If value IsNot Nothing AndAlso value.ContainsKey("ProductID") Then
                _productId = value("ProductID").ToString()
            End If
        End Set
    End Property

    Public Sub Populate(productData As Dictionary(Of String, Object), productImage As Image)
        Me.ProductData = productData
        If productData Is Nothing Then Return

        Dim productName As String = If(productData.ContainsKey("ProductName"), productData("ProductName").ToString(), "")
        _lblName.Text = productName
        _tip.SetToolTip(_lblName, productName)
        _tip.SetToolTip(_pbProduct, productName)

        Dim price As Decimal = 0D
        If productData.ContainsKey("Price") Then
            Try
                price = Convert.ToDecimal(productData("Price"))
            Catch
            End Try
        End If
        _lblPrice.Text = $"₱{price.ToString("N2")}"

        Dim code As String = If(productData.ContainsKey("ProductCode"), productData("ProductCode").ToString(), "")
        _lblCode.Text = $"Code: {code}"
        _tip.SetToolTip(_lblCode, code)

        If productImage IsNot Nothing Then
            _pbProduct.Image = productImage
        Else
            _pbProduct.Image = My.Resources.product_placeholder
        End If

        Dim stock As Integer = 0
        If productData.ContainsKey("CurrentStock") Then
            Try
                stock = Convert.ToInt32(productData("CurrentStock"))
            Catch
            End Try
        End If
        UpdateStock(stock)
    End Sub

    Public Sub UpdateStock(stock As Integer)
        _lblStock.Text = $"Stock: {stock}"
        If stock <= 0 Then
            _lblStock.ForeColor = ColorAlertRed
        ElseIf stock <= 8 Then
            _lblStock.ForeColor = ColorAmber
        Else
            _lblStock.ForeColor = ColorSuccessGreen
        End If
    End Sub

    ' The whole card is clickable - forward clicks from every child control.
    Private Sub WireCardEvents(root As Control)
        AddHandler root.Click, AddressOf OnCardClick
        AddHandler root.MouseEnter, AddressOf OnCardMouseEnter
        AddHandler root.MouseLeave, AddressOf OnCardMouseLeave
        For Each child As Control In root.Controls
            WireCardEvents(child)
        Next
    End Sub

    Private Sub OnCardClick(sender As Object, e As EventArgs)
        RaiseEvent ProductClicked(Me, EventArgs.Empty)
    End Sub

    Private Sub OnCardMouseEnter(sender As Object, e As EventArgs)
        _isHovering = True
        ApplyVisualState()
    End Sub

    Private Sub OnCardMouseLeave(sender As Object, e As EventArgs)
        ' Ignore leave events that fire while the cursor is still over a sibling child control.
        Dim cursorPoint As Point = Me.PointToClient(Cursor.Position)
        If Me.ClientRectangle.Contains(cursorPoint) Then Return
        _isHovering = False
        ApplyVisualState()
    End Sub

    Private Sub ApplyVisualState()
        If _isHovering Then
            _cardPanel.BorderThickness = 2
            _cardPanel.BorderColor = ColorGoldenYellow
            _cardPanel.FillColor = ColorHoverFill
        Else
            _cardPanel.BorderThickness = 0
            _cardPanel.FillColor = Color.White
        End If
    End Sub

    Private Shared Function ResolveFontFamily(priorityNames As String()) As String
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
End Class
