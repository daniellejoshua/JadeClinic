Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Printing
Imports System.Text.RegularExpressions
Imports System.Windows.Forms
Imports Guna.UI2.WinForms
Imports Microsoft.Data.SqlClient

Public Class Sales
    Private originalCategoryPanelControls As List(Of Control)
    Private originalOrderPanelControls As List(Of Control)
    Private originalTotalPanelControls As List(Of Control)

    ' Customer and payment flow variables
    Private isDiscountDialogOpen As Boolean = False
    Private pinPanelButtons As List(Of Guna.UI2.WinForms.Guna2Button) ' Repurposed for customer panel buttons
    Private totalPanelButtons As List(Of Guna.UI2.WinForms.Guna2Button)
    Private pinPanelActive As Boolean = False ' Repurposed for customer selection panel
    Private totalPanelActive As Boolean = False

    Private enteredAmount As String = ""
    Private lblAmountDisplay As Guna.UI2.WinForms.Guna2HtmlLabel

    Private productCardControls As New List(Of Control)()

    ' Receipt printing variables
    Private printDocument As PrintDocument
    Private receiptOrderId As String
    Private receiptCustomerName As String = "Walk-in Customer"
    Private receiptTotalAmount As Decimal
    Private receiptAmountReceived As Decimal
    Private receiptChange As Decimal
    Private receiptItems As List(Of Dictionary(Of String, Object))
    Private receiptSubtotal As Decimal
    Private receiptTax As Decimal

    ' Discount variables
    Private discountType As String = "None" ' "Percentage", "Fixed", "None"
    Private discountValue As Decimal = 0
    Private discountAmount As Decimal = 0 ' Fixed variable name (was discountAmouAnt)

    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    ' Customer data variables
    Private selectedCustomerId As Integer? = Nothing
    Private selectedCustomerName As String = "Walk-in Customer"
    Private selectedCustomerPhone As String = ""
    Private selectedCustomerEmail As String = ""

    ' Helper function to normalize category names
    Private Function NormalizeCategory(name As String) As String
        Return name.Replace("-", "").Replace(" ", "").ToLower()
    End Function

    ' List of normalized main categories for dental supplies
    Private ReadOnly mainCategoryNames As New HashSet(Of String) From {
        "ortho", "consumables", "surgery", "resto", "endo", "cosmetic"
    }

    ' Profile dropdown panel
    Private profileDropdownPanel As Panel = Nothing
    Private isProfileDropdownVisible As Boolean = False

    Private WithEvents txtBarcodeInput As New TextBox With {.Visible = True, .TabIndex = 0}

    ' Dental Clinic Color Palette Constants
    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)      ' #FECF10 - Primary brand color
    Private ReadOnly RichOlive As Color = Color.FromArgb(190, 154, 48)         ' #BE9A30 - Secondary accent
    Private ReadOnly DeepCharcoal As Color = Color.FromArgb(26, 29, 31)        ' #1A1D1F - Primary dark
    Private ReadOnly DarkSlate As Color = Color.FromArgb(43, 47, 50)           ' #2B2F32 - Secondary dark
    Private ReadOnly Graphite As Color = Color.FromArgb(61, 65, 69)            ' #3D4145 - Card background
    Private ReadOnly SteelGray As Color = Color.FromArgb(74, 79, 84)           ' #4A4F54 - Interactive elements
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)        ' #FFFFFF - Text on dark
    Private ReadOnly LightSilver As Color = Color.FromArgb(225, 229, 233)      ' #E1E5E9 - Secondary text
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)       ' #10D862 - Success states
    Private ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)           ' #FF4757 - Error/Alert states

    Private pinPanel As Guna.UI2.WinForms.Guna2Panel = Nothing ' Repurposed for customer selection
    Private totalReceivedPanel As Guna.UI2.WinForms.Guna2Panel = Nothing

    Private Sub Sales_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.KeyPreview = True
        originalCategoryPanelControls = New List(Of Control)(CategoryPanel.Controls.Cast(Of Control)())

        ' Make form non-resizable
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Enhanced CategoryPanel (main focus area) - Updated colors
        CategoryPanel.BorderColor = GoldenYellow ' Golden Yellow
        CategoryPanel.ShadowDecoration.Depth = 8 ' Deep shadow
        CategoryPanel.BorderRadius = 12 ' Rounded corners

        ' Create navigation menu (hardcoded)
        CreateNavigationMenu()

        ' Validate user session
        If Not ValidateUserSession() Then
            Return
        End If

        ' Initialize profile section
        InitializeProfileSection()

        ' Update form title to show logged-in user
        Me.Text = $"Sales - {frmLoginvb.LoggedInUsername}"

        ' Start idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)

        AttachClickHandlersToAllControls(Me)

        ' Initialize print document
        printDocument = New PrintDocument()
        AddHandler printDocument.PrintPage, AddressOf OnPrintPage

        ' Add tooltip for all category buttons
        Dim toolTip As New ToolTip()
        Dim categoryButtons As New Dictionary(Of Guna.UI2.WinForms.Guna2Button, String) From {
            {Me.OrthoCatBtn, "ORTHO"},
            {Me.ConsumablesCatBtn, "CONSUMABLES"},
            {Me.SurgeryCatBtn, "SURGERY"},
            {RestoCatBtn, "RESTO"},
            {Me.EndoCatBtn, "ENDO"},
            {Me.CosmeticCatBtn, "COSMETIC"}
        }

        For Each kvp In categoryButtons
            toolTip.SetToolTip(kvp.Key, $"Click to view {kvp.Value} products")
            AddHandler kvp.Key.Click, Sub(senderBtn, eBtn)
                                          ShowCategoryProducts(kvp.Value)
                                      End Sub
            AddHandler kvp.Key.MouseEnter, Sub(senderBtn, eBtn)
                                               Dim btn = CType(senderBtn, Guna.UI2.WinForms.Guna2Button)
                                               btn.HoverState.FillColor = btn.FillColor
                                               btn.HoverState.BorderColor = PureWhite
                                               btn.BorderThickness = 2
                                               btn.Cursor = Cursors.Hand
                                           End Sub
            AddHandler kvp.Key.MouseLeave, Sub(senderBtn, eBtn)
                                               Dim btn = CType(senderBtn, Guna.UI2.WinForms.Guna2Button)
                                               btn.BorderThickness = 0
                                           End Sub
        Next

        ' Add hover effect to all category buttons in CategoryPanel (including dynamic)
        For Each ctrl As Control In CategoryPanel.Controls
            If TypeOf ctrl Is Guna.UI2.WinForms.Guna2Button Then
                Dim btn = CType(ctrl, Guna.UI2.WinForms.Guna2Button)
                AddHandler btn.MouseEnter, Sub(senderBtn, eBtn)
                                               Dim b = CType(senderBtn, Guna.UI2.WinForms.Guna2Button)
                                               b.HoverState.FillColor = b.FillColor
                                               b.HoverState.BorderColor = PureWhite
                                               b.BorderThickness = 2
                                               b.Cursor = Cursors.Hand
                                           End Sub
                AddHandler btn.MouseLeave, Sub(senderBtn, eBtn)
                                               Dim b = CType(senderBtn, Guna.UI2.WinForms.Guna2Button)
                                               b.BorderThickness = 0
                                           End Sub
            End If
        Next

        backCategory.Visible = False

        ' Add new category buttons from DB after designer buttons
        AddNewCategoryButtonsFromDB()
        ' Arrange buttons in flex-wrap style
        ArrangeCategoryButtonsFlexWrap()

        ' Show next possible Sale ID in lblOrderId
        Dim nextSaleId As Integer = 1
        Try
            Dim query As String = "SELECT ISNULL(MAX(SaleID), 0) + 1 AS NextSaleID FROM Sales"
            Using reader As SqlDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                If reader.Read() Then
                    nextSaleId = Convert.ToInt32(reader("NextSaleID"))
                End If
            End Using
        Catch ex As Exception
            nextSaleId = 1
        End Try

        If lblOrderId IsNot Nothing Then
            lblOrderId.Text = "Sale ID: " & nextSaleId.ToString()
        End If

        If lblSubTotal IsNot Nothing Then lblSubTotal.Text = "0.00"
        If taxLbl IsNot Nothing Then taxLbl.Text = "0.00"
        If totalLbl IsNot Nothing Then totalLbl.Text = "0.00"
        If totalRLbl IsNot Nothing Then totalRLbl.Text = "0.00"

        UpdateCategoryItemCounts()

        Me.Controls.Add(txtBarcodeInput)
        txtBarcodeInput.Location = New Point(300, 200) ' Off-screen
        txtBarcodeInput.Width = 200
        txtBarcodeInput.Height = 20
        txtBarcodeInput.BackColor = Color.White
        txtBarcodeInput.Focus()
        AddHandler Me.Activated, Sub() FocusBarcodeInputIfAllowed()
        AddHandler Me.Click, Sub() FocusBarcodeInputIfAllowed()
    End Sub

    Public Sub ShowCategoryProducts(categoryName As String)
        ' Clear the CategoryPanel
        CategoryPanel.Controls.Clear()
        productCardControls.Clear()
        backCategory.Visible = True
        LabelTitle.Text = categoryName

        ' Query products from database where Category matches - Always get fresh data
        Dim query As String = "SELECT ProductID, ProductName, SellingPrice, ProductCode, ReorderLevel, CurrentStock, Category FROM Products WHERE Category = @Category AND IsActive = 1"
        Dim param As New SqlParameter("@Category", categoryName)
        Using reader As SqlDataReader = Utilities.ExecuteReader(query, {param})
            Dim cardWidth As Integer = 230
            Dim cardHeight As Integer = 220
            Dim marginX As Integer = 28
            Dim marginY As Integer = 18
            Dim currentX As Integer = marginX
            Dim currentY As Integer = marginY

            While reader.Read()
                ' Create a new panel for the product card - Updated colors
                Dim productCard As New Guna.UI2.WinForms.Guna2Panel()
                productCard.Size = New Size(cardWidth, cardHeight)
                productCard.BorderRadius = 10
                productCard.FillColor = DarkSlate ' Updated to match clinic theme
                productCard.BorderColor = RichOlive ' Golden accent border
                productCard.BorderThickness = 1

                ' Set the Tag property to the ProductID for UpdateStockLabel method
                productCard.Tag = reader("ProductID").ToString()

                ' Check if the card exceeds the width of the CategoryPanel
                If currentX + cardWidth > CategoryPanel.Width Then
                    currentX = marginX ' Reset X position
                    currentY += cardHeight + marginY ' Move to the next row
                End If
                productCard.Location = New Point(currentX, currentY)
                currentX += cardWidth + marginX ' Update X position for the next card

                ' Add product image placeholder
                Dim productImage As New Guna.UI2.WinForms.Guna2PictureBox()
                Try
                    productImage.Image = My.Resources.Jade_Dental_Logo ' Use your dental logo as placeholder
                Catch
                    ' Create a simple placeholder rectangle
                    Dim placeholderBitmap As New Bitmap(90, 90)
                    Using g As Graphics = Graphics.FromImage(placeholderBitmap)
                        g.FillRectangle(New SolidBrush(Graphite), 0, 0, 90, 90)
                        g.DrawString("No Image", New Font("Arial", 8), New SolidBrush(PureWhite), 10, 35)
                    End Using
                    productImage.Image = placeholderBitmap
                End Try

                productImage.Size = New Size(90, 90)
                productImage.Location = New Point(cardWidth - productImage.Width - 10, 10)
                productImage.SizeMode = PictureBoxSizeMode.StretchImage
                productImage.BorderRadius = 10
                productCard.Controls.Add(productImage)

                ' Add product name - use limited size label with tooltip
                Dim fullProductName As String = reader("ProductName").ToString()
                Dim maxNameLength As Integer = 18
                Dim displayName As String = If(fullProductName.Length > maxNameLength, fullProductName.Substring(0, maxNameLength) & "...", fullProductName)

                Dim lblProductName As New Guna.UI2.WinForms.Guna2HtmlLabel()
                lblProductName.Text = displayName
                lblProductName.Font = New Font("Poppins Light", 9.0F, FontStyle.Regular)
                lblProductName.ForeColor = PureWhite ' Updated color
                lblProductName.Location = New Point(10, cardHeight - 120)
                lblProductName.AutoSize = True
                productCard.Controls.Add(lblProductName)

                ' Add tooltip for full product name if truncated
                If fullProductName.Length > maxNameLength Then
                    Dim toolTip As New ToolTip()
                    toolTip.SetToolTip(lblProductName, fullProductName)
                End If

                ' Add product price
                Dim originalPrice As Decimal = Convert.ToDecimal(reader("SellingPrice"))
                Dim lblProductPrice As New Label()
                lblProductPrice.Text = $"Price: ?{originalPrice:F2}"
                lblProductPrice.ForeColor = LightSilver ' Updated color
                lblProductPrice.Font = New Font("Poppins Light", 9.0F, FontStyle.Regular)
                lblProductPrice.Location = New Point(10, cardHeight - 90)
                lblProductPrice.AutoSize = True
                productCard.Controls.Add(lblProductPrice)

                ' Add product code
                Dim lblProductCode As New Label()
                lblProductCode.Text = $"Code: {reader("ProductCode").ToString()}"
                lblProductCode.Font = New Font("Poppins", 7.5F, FontStyle.Regular)
                lblProductCode.ForeColor = GoldenYellow ' Updated to match brand
                lblProductCode.AutoSize = True
                lblProductCode.Location = New Point(10, cardHeight - 70)
                productCard.Controls.Add(lblProductCode)

                ' Add available stock - Get CURRENT stock from database
                Dim lblStock As New Label()
                Dim stock As Integer = Convert.ToInt32(reader("CurrentStock"))

                ' Check if this product is in current order and adjust display accordingly
                Dim reservedQty As Integer = 0
                For Each orderItem In currentOrderList
                    If orderItem("ProductID").ToString() = reader("ProductID").ToString() Then
                        reservedQty += CInt(orderItem("Quantity"))
                    End If
                Next

                Dim displayStock As Integer = Math.Max(0, stock - reservedQty)
                lblStock.Text = $"Stock: {displayStock}"
                lblStock.ForeColor = If(displayStock > 0, SuccessGreen, AlertRed) ' Updated colors
                lblStock.Font = New Font("Poppins Light", 9.0F, FontStyle.Regular)
                lblStock.Location = New Point(10, cardHeight - 50)
                lblStock.AutoSize = True
                productCard.Controls.Add(lblStock)

                ' Add hover effect to product card
                AddHandler productCard.MouseEnter, Sub()
                                                       productCard.BorderThickness = 2
                                                       productCard.BorderColor = GoldenYellow
                                                       productCard.Cursor = Cursors.Hand
                                                   End Sub
                AddHandler productCard.MouseLeave, Sub()
                                                       productCard.BorderThickness = 1
                                                       productCard.BorderColor = RichOlive
                                                   End Sub

                ' Save product data for details
                Dim productData As New Dictionary(Of String, Object) From {
                    {"ProductID", reader("ProductID")},
                    {"ProductName", reader("ProductName")},
                    {"Price", originalPrice},
                    {"ProductCode", reader("ProductCode")},
                    {"Category", reader("Category")},
                    {"CurrentStock", stock}
                }

                ' Add click handler to show details
                AddHandler productCard.Click, Sub(sender2, e2)
                                                  ShowProductDetailsPanel(productData)
                                                  FocusBarcodeInputIfAllowed() ' Ensure focus after product selection
                                              End Sub

                CategoryPanel.Controls.Add(productCard)
                productCardControls.Add(productCard)
            End While
        End Using
    End Sub

    Private Sub ShowProductDetailsPanel(productData As Dictionary(Of String, Object))
        ' Prevent product clicks when customer selection or payment panels are active
        If pinPanelActive OrElse totalPanelActive Then
            Return ' Exit without adding to order
        End If

        ' Check if the product stock is 0
        If productData.ContainsKey("CurrentStock") AndAlso CInt(productData("CurrentStock")) = 0 Then
            MessageBox.Show("This product is out of stock and cannot be added to the order.", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return ' Exit without adding to order
        End If

        ' Check if product already exists in the order list
        Dim foundIndex As Integer = -1
        For i = 0 To currentOrderList.Count - 1
            If currentOrderList(i)("ProductID").ToString() = productData("ProductID").ToString() Then
                foundIndex = i
                Exit For
            End If
        Next

        Dim priceToUse As Decimal = Convert.ToDecimal(productData("Price"))

        If foundIndex <> -1 Then
            ' Check if we have enough stock for the increase
            Dim currentQuantity As Integer = CInt(currentOrderList(foundIndex)("Quantity"))
            Dim availableStock As Integer = CInt(productData("CurrentStock"))

            ' Get already reserved quantity in order
            Dim reservedQuantity As Integer = 0
            For Each item In currentOrderList
                If item("ProductID").ToString() = productData("ProductID").ToString() Then
                    reservedQuantity = CInt(item("Quantity"))
                    Exit For
                End If
            Next

            If reservedQuantity >= availableStock Then
                MessageBox.Show("Cannot add more items. Not enough stock available.", "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Increase quantity by 1 (fixed the issue where it was set to 2)
            currentOrderList(foundIndex)("Quantity") = currentQuantity + 1
        Else
            ' Add product with quantity 1
            productData("Quantity") = 1
            productData("Price") = priceToUse
            currentOrderList.Add(productData)
        End If

        ' ONLY deduct stock from UI display, NOT from database yet
        productData("CurrentStock") = CInt(productData("CurrentStock")) - 1

        ' Update lblStock for the product
        UpdateStockLabel(productData("ProductID").ToString(), CInt(productData("CurrentStock")))

        ' Refresh the order display
        RefreshOrderDisplay()
    End Sub

    ' New method to reduce item quantity or remove item
    Private Sub ReduceItemQuantity(itemIndex As Integer)
        If itemIndex < 0 Or itemIndex >= currentOrderList.Count Then
            Return
        End If

        Dim currentQuantity As Integer = CInt(currentOrderList(itemIndex)("Quantity"))

        If currentQuantity > 1 Then
            ' Reduce quantity by 1
            currentOrderList(itemIndex)("Quantity") = currentQuantity - 1

            ' Restore stock in UI display (since we haven't deducted from database yet)
            currentOrderList(itemIndex)("CurrentStock") = CInt(currentOrderList(itemIndex)("CurrentStock")) + 1

            ' Update lblStock for the product
            UpdateStockLabel(currentOrderList(itemIndex)("ProductID").ToString(), CInt(currentOrderList(itemIndex)("CurrentStock")))
        Else
            ' Remove item from list
            Dim productName As String = currentOrderList(itemIndex)("ProductName").ToString()
            Dim result As DialogResult = MessageBox.Show($"Remove '{productName}' from order?", "Remove Item", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ' Restore ALL stock for this item in UI display (since we haven't deducted from database yet)
                currentOrderList(itemIndex)("CurrentStock") = CInt(currentOrderList(itemIndex)("CurrentStock")) + currentQuantity

                ' Update lblStock for the product
                UpdateStockLabel(currentOrderList(itemIndex)("ProductID").ToString(), CInt(currentOrderList(itemIndex)("CurrentStock")))

                currentOrderList.RemoveAt(itemIndex)
            End If
        End If

        ' Refresh the display
        RefreshOrderDisplay()
        UpdateCategoryItemCounts()
    End Sub

    Private Sub UpdateStockLabel(productId As String, newStock As Integer)
        For Each productCard As Control In productCardControls
            If TypeOf productCard Is Guna.UI2.WinForms.Guna2Panel AndAlso productCard.Tag IsNot Nothing Then
                If productCard.Tag.ToString() = productId Then
                    ' Find the stock label within this product card
                    Dim lblStock As Label = productCard.Controls.OfType(Of Label)().FirstOrDefault(Function(lbl) lbl.Text.StartsWith("Stock:"))
                    If lblStock IsNot Nothing Then
                        lblStock.Text = $"Stock: {newStock}"
                        lblStock.ForeColor = If(newStock > 0, SuccessGreen, AlertRed) ' Updated colors
                    End If
                    Exit For
                End If
            End If
        Next
    End Sub

    Private Sub UpdateCategoryItemCounts()
        ' Dictionary to map category names to their respective labels
        Dim categoryLabels As New Dictionary(Of String, Guna2HtmlLabel) From {
            {"ORTHO", Guna2HtmlLabel5},
            {"CONSUMABLES", Guna2HtmlLabel7},
            {"SURGERY", Guna2HtmlLabel1},
            {"RESTO", Guna2HtmlLabel3},
            {"ENDO", Guna2HtmlLabel9},
            {"COSMETIC", Guna2HtmlLabel11}
        }

        ' Query the database to get the count of distinct products for each category
        Try
            Dim query As String = "SELECT Category, COUNT(*) AS TotalProducts FROM Products WHERE IsActive = 1 GROUP BY Category"
            Using reader As SqlDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                While reader.Read()
                    Dim category As String = reader("Category").ToString().ToUpper()
                    Dim totalProducts As Integer = Convert.ToInt32(reader("TotalProducts"))

                    ' Update the corresponding label if it exists in the dictionary
                    If categoryLabels.ContainsKey(category) Then
                        Dim label As Guna2HtmlLabel = categoryLabels(category)
                        label.Text = $"{totalProducts.ToString()} Items"
                    End If
                End While
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error updating category counts: {ex.Message}")
        End Try

        ' Set labels for categories with no products to "0"
        For Each kvp In categoryLabels
            If String.IsNullOrEmpty(kvp.Value.Text) OrElse kvp.Value.Text = "0 Items" Then
                kvp.Value.Text = "0 Items"
            End If
        Next
    End Sub

    Private Sub AddNewCategoryButtonsFromDB()
        ' Get all categories from DB
        Dim query As String = "SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND Category <> '' AND IsActive = 1"
        Using reader As SqlDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
            While reader.Read()
                Dim catName As String = reader("Category").ToString().ToUpper()
                ' Skip if category is a main category (normalize)
                If mainCategoryNames.Contains(NormalizeCategory(catName)) Then
                    Continue While
                End If

                ' Check if a designer button already exists for this category
                Dim exists As Boolean = False
                For Each ctrl As Control In CategoryPanel.Controls
                    If TypeOf ctrl Is Guna.UI2.WinForms.Guna2Button Then
                        Dim btn = CType(ctrl, Guna.UI2.WinForms.Guna2Button)
                        If NormalizeCategory(btn.Text) = NormalizeCategory(catName) Then
                            exists = True
                            Exit For
                        End If
                    End If
                Next

                If Not exists Then
                    ' Create a new button styled like OrthoCatBtn
                    Dim btnCategory As New Guna.UI2.WinForms.Guna2Button()
                    btnCategory.Text = catName
                    btnCategory.Size = Me.OrthoCatBtn.Size
                    btnCategory.BorderRadius = Me.OrthoCatBtn.BorderRadius
                    btnCategory.FillColor = Me.OrthoCatBtn.FillColor
                    btnCategory.Font = Me.OrthoCatBtn.Font
                    btnCategory.ForeColor = Me.OrthoCatBtn.ForeColor
                    btnCategory.BackColor = Me.OrthoCatBtn.BackColor
                    btnCategory.BorderColor = Me.OrthoCatBtn.BorderColor

                    Dim toolTip As New ToolTip()
                    toolTip.SetToolTip(btnCategory, $"Click to view {catName} products")
                    AddHandler btnCategory.Click, Sub(senderBtn, eBtn)
                                                      ShowCategoryProducts(catName)
                                                  End Sub
                    AddHandler btnCategory.Click, AddressOf Control_Click
                    AddHandler btnCategory.MouseEnter, Sub(senderBtn, eBtn)
                                                           Dim btn = CType(senderBtn, Guna.UI2.WinForms.Guna2Button)
                                                           btn.HoverState.FillColor = btn.FillColor
                                                           btn.HoverState.BorderColor = PureWhite
                                                           btn.BorderThickness = 2
                                                           btn.Cursor = Cursors.Hand
                                                       End Sub
                    AddHandler btnCategory.MouseLeave, Sub(senderBtn, eBtn)
                                                           Dim btn = CType(senderBtn, Guna.UI2.WinForms.Guna2Button)
                                                           btn.BorderThickness = 0
                                                       End Sub
                    CategoryPanel.Controls.Add(btnCategory)
                End If
            End While
        End Using
    End Sub

    ' Helper to arrange dynamic category buttons properly with the main buttons
    Private Sub ArrangeCategoryButtonsFlexWrap()
        Dim marginX As Integer = 10
        Dim marginY As Integer = 10
        Dim panelWidth As Integer = CategoryPanel.Width
        Dim buttonWidth As Integer = 167 ' From designer
        Dim buttonHeight As Integer = 146 ' From designer

        ' List of main category buttons with their intended positions
        Dim mainButtons As New List(Of Guna.UI2.WinForms.Guna2Button) From {
            Me.OrthoCatBtn, Me.ConsumablesCatBtn, Me.SurgeryCatBtn, RestoCatBtn, Me.EndoCatBtn, Me.CosmeticCatBtn
        }

        ' Find the lowest Y position of main buttons to place dynamic ones below
        Dim maxY As Integer = 0
        For Each btn In mainButtons
            If CategoryPanel.Controls.Contains(btn) Then
                Dim bottomY = btn.Location.Y + btn.Height
                If bottomY > maxY Then
                    maxY = bottomY
                End If
            End If
        Next

        ' Start dynamic buttons below the main buttons
        Dim startX As Integer = marginX + 25
        Dim startY As Integer = maxY + marginY + 20 ' Add some extra spacing
        Dim currentX As Integer = startX + 25
        Dim currentY As Integer = startY

        ' Arrange only dynamic buttons (not in mainButtons)
        For Each ctrl As Control In CategoryPanel.Controls
            If TypeOf ctrl Is Guna.UI2.WinForms.Guna2Button Then
                Dim btn = CType(ctrl, Guna.UI2.WinForms.Guna2Button)
                If Not mainButtons.Contains(btn) Then
                    ' Check if button fits in current row
                    If currentX + btn.Width > panelWidth - marginX Then
                        currentX = startX
                        currentY += btn.Height + marginY
                    End If

                    btn.Location = New Point(currentX, currentY)
                    currentX += btn.Width + marginX
                End If
            End If
        Next

        ' Ensure CategoryPanel can scroll if content exceeds visible area
        CategoryPanel.AutoScroll = True
    End Sub

    Private Sub backCategory_Click(sender As Object, e As EventArgs) Handles backCategory.Click
        ' Store the current state before clearing
        CategoryPanel.SuspendLayout()

        ' Clear the CategoryPanel
        CategoryPanel.Controls.Clear()

        ' Restore the original designer controls
        For Each control As Control In originalCategoryPanelControls
            CategoryPanel.Controls.Add(control)
        Next

        ' Add new category buttons from DB
        AddNewCategoryButtonsFromDB()

        ' Arrange buttons properly
        ArrangeCategoryButtonsFlexWrap()

        LabelTitle.Text = "Categories"
        backCategory.Visible = False

        ' Reset scroll position to top
        CategoryPanel.AutoScrollPosition = New Point(0, 0)

        ' Resume layout
        CategoryPanel.ResumeLayout(True)
        CategoryPanel.Refresh()
    End Sub

    Private Sub AttachClickHandlersToAllControls(parentControl As Control)
        ' Add click handler to the parent control itself
        AddHandler parentControl.Click, AddressOf Control_Click

        ' Recursively add handlers to all child controls
        For Each ctrl As Control In parentControl.Controls
            ' Skip specific panels that handle their own interactions
            If ctrl IsNot txtBarcodeInput AndAlso
           ctrl IsNot pinPanel AndAlso ' Now used for customer selection
           ctrl IsNot totalReceivedPanel AndAlso
           ctrl IsNot profileDropdownPanel Then

                AddHandler ctrl.Click, AddressOf Control_Click

                ' If the control has children, recursively attach handlers
                If ctrl.HasChildren Then
                    AttachClickHandlersToAllControls(ctrl)
                End If
            End If
        Next
    End Sub

    Private Sub Control_Click(sender As Object, e As EventArgs)
        ' Focus the barcode input when any control is clicked
        FocusBarcodeInputIfAllowed()
    End Sub

    Private Sub FocusBarcodeInputIfAllowed()
        ' Don't focus barcode input when customer selection or payment panels are active
        If Not pinPanelActive AndAlso Not totalPanelActive AndAlso Not isProfileDropdownVisible Then
            Try
                If txtBarcodeInput IsNot Nothing AndAlso Not txtBarcodeInput.IsDisposed Then
                    txtBarcodeInput.Focus()
                End If
            Catch ex As Exception
                ' Handle any focus-related exceptions silently
            End Try
        End If
    End Sub

    ' Make currentOrderList accessible
    Public currentOrderList As New List(Of Dictionary(Of String, Object))

    ' Helper method to validate user session
    Private Function ValidateUserSession() As Boolean
        If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            MessageBox.Show("User session expired. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            frmLoginvb.Show()
            Me.Hide()
            Return False
        End If
        Return True
    End Function

    ' Barcode scanning functionality
    Private Sub txtBarcodeInput_KeyDown(sender As Object, e As KeyEventArgs) Handles txtBarcodeInput.KeyDown
        If e.KeyCode = Keys.Enter Then
            Dim scannedCode As String = txtBarcodeInput.Text.Trim()
            If Not String.IsNullOrEmpty(scannedCode) Then
                ' Process barcode - look up product by ProductCode
                ProcessBarcodeScan(scannedCode)
                txtBarcodeInput.Clear()
            End If
            e.Handled = True
        End If
    End Sub

    Private Sub ProcessBarcodeScan(barcode As String)
        Try
            ' Look for product by ProductCode
            Dim query As String = "SELECT ProductID, ProductName, SellingPrice, ProductCode, CurrentStock, Category FROM Products WHERE ProductCode = @ProductCode AND IsActive = 1"
            Dim param As New SqlParameter("@ProductCode", barcode)

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, {param})
                If reader.Read() Then
                    Dim productData As New Dictionary(Of String, Object) From {
                        {"ProductID", reader("ProductID")},
                        {"ProductName", reader("ProductName")},
                        {"Price", reader("SellingPrice")},
                        {"ProductCode", reader("ProductCode")},
                        {"Category", reader("Category")},
                        {"CurrentStock", reader("CurrentStock")}
                    }

                    ShowProductDetailsPanel(productData)
                    MessageBox.Show($"Product '{reader("ProductName")}' added to order!", "Scanned", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    MessageBox.Show($"Product with code '{barcode}' not found.", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error processing barcode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Enhanced receipt printing
    Private Sub OnPrintPage(sender As Object, e As PrintPageEventArgs)
        Try
            Dim regularFont As New Font("Arial", 8)
            Dim boldFont As New Font("Arial", 10, FontStyle.Bold)
            Dim headerFont As New Font("Arial", 12, FontStyle.Bold)
            Dim brush As New SolidBrush(Color.Black)
            Dim yPosition As Integer = 10
            Dim centerX As Integer = e.MarginBounds.Width \ 2

            ' Store Header
            e.Graphics.DrawString("JADE CLINIC", headerFont, brush, centerX - 60, yPosition)
            yPosition += 25
            e.Graphics.DrawString("Dental Supply Management", regularFont, brush, centerX - 80, yPosition)
            yPosition += 15
            e.Graphics.DrawString("Tel: (123) 456-7890", regularFont, brush, centerX - 50, yPosition)
            yPosition += 20
            e.Graphics.DrawString("=====================================", regularFont, brush, 10, yPosition)
            yPosition += 20

            ' Receipt details
            e.Graphics.DrawString("SALES RECEIPT", boldFont, brush, centerX - 45, yPosition)
            yPosition += 25
            e.Graphics.DrawString($"Receipt #: {receiptOrderId}", regularFont, brush, 10, yPosition)
            yPosition += 15
            e.Graphics.DrawString($"Date: {DateTime.Now:MM/dd/yyyy HH:mm:ss}", regularFont, brush, 10, yPosition)
            yPosition += 15
            e.Graphics.DrawString($"Cashier: {frmLoginvb.LoggedInUsername}", regularFont, brush, 10, yPosition)
            yPosition += 15
            e.Graphics.DrawString($"Customer: {receiptCustomerName}", regularFont, brush, 10, yPosition)
            yPosition += 20
            e.Graphics.DrawString("=====================================", regularFont, brush, 10, yPosition)
            yPosition += 15

            ' Items
            For Each item In receiptItems
                Dim itemName As String = item("ProductName").ToString()
                Dim quantity As Integer = CInt(item("Quantity"))
                Dim price As Decimal = Convert.ToDecimal(item("Price"))
                Dim total As Decimal = price * quantity

                ' Truncate long product names
                If itemName.Length > 25 Then
                    itemName = itemName.Substring(0, 22) & "..."
                End If

                e.Graphics.DrawString($"{itemName}", regularFont, brush, 10, yPosition)
                yPosition += 15
                e.Graphics.DrawString($"  {quantity}x ?{price:F2}", regularFont, brush, 20, yPosition)
                e.Graphics.DrawString($"?{total:F2}", regularFont, brush, e.MarginBounds.Width - 60, yPosition)
                yPosition += 15
            Next

            yPosition += 10
            e.Graphics.DrawString("-------------------------------------", regularFont, brush, 10, yPosition)
            yPosition += 15

            ' Totals
            e.Graphics.DrawString($"Subtotal:", regularFont, brush, 10, yPosition)
            e.Graphics.DrawString($"?{receiptSubtotal:F2}", regularFont, brush, e.MarginBounds.Width - 80, yPosition)
            yPosition += 15

            If discountAmount > 0 Then
                e.Graphics.DrawString($"Discount ({discountType}):", regularFont, brush, 10, yPosition)
                e.Graphics.DrawString($"-?{discountAmount:F2}", regularFont, brush, e.MarginBounds.Width - 80, yPosition)
                yPosition += 15
            End If

            e.Graphics.DrawString($"Tax (12%):", regularFont, brush, 10, yPosition)
            e.Graphics.DrawString($"?{receiptTax:F2}", regularFont, brush, e.MarginBounds.Width - 80, yPosition)
            yPosition += 15

            e.Graphics.DrawString($"TOTAL:", boldFont, brush, 10, yPosition)
            e.Graphics.DrawString($"?{receiptTotalAmount:F2}", boldFont, brush, e.MarginBounds.Width - 80, yPosition)
            yPosition += 20

            e.Graphics.DrawString($"Amount Received:", regularFont, brush, 10, yPosition)
            e.Graphics.DrawString($"?{receiptAmountReceived:F2}", regularFont, brush, e.MarginBounds.Width - 80, yPosition)
            yPosition += 15

            e.Graphics.DrawString($"Change:", boldFont, brush, 10, yPosition)
            e.Graphics.DrawString($"?{receiptChange:F2}", boldFont, brush, e.MarginBounds.Width - 80, yPosition)
            yPosition += 30

            ' Footer
            e.Graphics.DrawString("=====================================", regularFont, brush, 10, yPosition)
            yPosition += 20
            e.Graphics.DrawString("Thank you for your business!", regularFont, brush, centerX - 70, yPosition)
            yPosition += 15
            e.Graphics.DrawString("Have a great day!", regularFont, brush, centerX - 45, yPosition)

        Catch ex As Exception
            ' If printing fails, just log it
            Console.WriteLine($"Print error: {ex.Message}")
        End Try
    End Sub

    Private Sub CreateNavigationMenu()
        Try
            ' Clear existing controls except PictureBox9 (logo)
            For i = DashboardPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = DashboardPanel.Controls(i)
                If TypeOf control IsNot PictureBox Then
                    DashboardPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            ' Set Navigation Panel Background to White
            DashboardPanel.FillColor = System.Drawing.Color.White

            ' Calculate available space (DashboardPanel is 236x885)
            Dim availableWidth As Integer = DashboardPanel.Width - 40 ' 20px margins on each side
            Dim availableHeight As Integer = DashboardPanel.Height - 160 ' Space for logo and title

            ' Logo area (keep existing PictureBox9)
            PictureBox9.BringToFront()

            ' Add title label - positioned below logo with Golden Yellow
            Dim titleLabel As New Label()
            titleLabel.Text = "JADE CLINIC"
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = System.Drawing.Color.FromArgb(254, 191, 16) ' Golden Yellow #FECF10
            titleLabel.BackColor = System.Drawing.Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New System.Drawing.Size(availableWidth, 30)
            titleLabel.Location = New Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(titleLabel)

            ' Subtitle with Dark Gray color (visible on white background)
            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Poppins", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100) ' Dark Gray for visibility on white
            subtitleLabel.BackColor = System.Drawing.Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New System.Drawing.Size(availableWidth, 25)
            subtitleLabel.Location = New Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(subtitleLabel)

            ' Navigation section separator with Light Gray (visible on white background)
            Dim separator1 As New Panel()
            separator1.BackColor = System.Drawing.Color.FromArgb(220, 220, 220) ' Light Gray for white background
            separator1.Size = New System.Drawing.Size(availableWidth - 20, 2)
            separator1.Location = New Point(30, 190)
            DashboardPanel.Controls.Add(separator1)

            ' Navigation section label with Dark Gray (visible on white background)
            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Poppins", 10, FontStyle.Bold)
            navLabel.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80) ' Dark Gray for visibility on white
            navLabel.BackColor = System.Drawing.Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New System.Drawing.Size(availableWidth, 25)
            navLabel.Location = New Point(20, 205)
            navLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(navLabel)

            ' Calculate button positioning for role-based navigation
            Dim startY As Integer = 250
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonWidth As Integer = availableWidth - 5
            Dim buttonIndex As Integer = 0

            ' Get current user role for navigation filtering
            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "Staff").ToUpper()

            ' Create navigation buttons based on role
            ' Dashboard Button (not active)
            Dim navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
            buttonIndex += 1

            ' POS/Sales Button (ACTIVE - we're on this page)
            Dim navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
            buttonIndex += 1

            ' Manager and Admin only buttons - Inventory moved here
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                ' Inventory Button (only for Manager and Admin)
                Dim navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
                buttonIndex += 1

                ' Sales Records Button
                Dim navSalesRecordsBtn = CreateLargeNavButton("📊 Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navSalesRecordsBtn.Click, AddressOf NavSalesRecords_Click
                buttonIndex += 1

                ' Staff Management Button
                Dim navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navStaffBtn.Click, AddressOf NavStaff_Click
                buttonIndex += 1

                ' Inventory Logs Button
                Dim navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
                buttonIndex += 1
            End If

            ' Admin only buttons
            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                ' Audit Logs Button
                Dim navAuditLogBtn = CreateLargeNavButton("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navAuditLogBtn.Click, AddressOf NavAuditLog_Click
                buttonIndex += 1

                ' System Settings Button
                Dim systemSettingsBtn = CreateLargeNavButton("⚙️ System", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler systemSettingsBtn.Click, Sub() MessageBox.Show("System Settings feature coming soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
                buttonIndex += 1
            End If

            ' No more logout button in navigation - removed separator and logout button

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
        End Try
    End Sub

    Private Function CreateLargeNavButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer) As Guna.UI2.WinForms.Guna2Button
        Dim btn As New Guna.UI2.WinForms.Guna2Button()

        ' Button properties with improved sizing and new color scheme
        btn.Text = text
        btn.Size = New System.Drawing.Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left

        ' Apply new color scheme
        btn.FillColor = If(isActive, System.Drawing.Color.FromArgb(254, 191, 16), System.Drawing.Color.Transparent) ' Golden Yellow if active #FECF10
        btn.ForeColor = If(isActive, System.Drawing.Color.FromArgb(26, 29, 31), System.Drawing.Color.FromArgb(50, 50, 50)) ' Deep Charcoal text on active, Dark Gray text on inactive for white background
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(200, 200, 200)) ' Light Gray border for white background
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        ' Add subtle shadow for depth
        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = System.Drawing.Color.FromArgb(26, 29, 31) ' Deep Charcoal shadow
        btn.ShadowDecoration.Depth = 5
        btn.ShadowDecoration.Shadow = New Padding(0, 2, 5, 5)

        ' Improved hover effects with new color scheme
        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.FromArgb(240, 240, 240) ' Light Gray hover for white background
                                           btn.BorderColor = System.Drawing.Color.FromArgb(190, 154, 48) ' Rich Olive border #BE9A30
                                           btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                       End If
                                   End Sub

        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.Transparent
                                           btn.BorderColor = System.Drawing.Color.FromArgb(200, 200, 200) ' Light Gray border
                                           btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        ' Add to panel
        DashboardPanel.Controls.Add(btn)

        Return btn
    End Function


    Private Sub Sales_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)

        ' If this is programmatic navigation, don't show confirmation
        If isNavigating Then
            Return
        End If

        ' Prevent multiple confirmations by checking the close reason
        If e.CloseReason = CloseReason.ApplicationExitCall Then
            ' If Application.Exit() was already called, don't show confirmation again
            Return
        End If

        ' Show confirmation only for user-initiated close (X button)
        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to exit the application?", "Exit Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ' Log the exit action
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Sales form")
                End If

                ' Close all forms properly
                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then
                        form.Close()
                    End If
                Next

                ' Now exit the application
                Application.Exit()
            Else
                ' Cancel the form closing
                e.Cancel = True
            End If
        End If
    End Sub

    ' Event handlers for form events
    Private Sub CategoryPanel_Paint(sender As Object, e As PaintEventArgs) Handles CategoryPanel.Paint
    End Sub

    Private Sub Guna2HtmlLabel16_Click(sender As Object, e As EventArgs)
    End Sub

    Private Sub btnShorts_Click(sender As Object, e As EventArgs)
    End Sub

    Private Sub Guna2HtmlLabel39_Click(sender As Object, e As EventArgs)
    End Sub

    Private Sub PictureBox12_Click(sender As Object, e As EventArgs)
    End Sub

    Private Sub orderSummaryPanel_Paint(sender As Object, e As PaintEventArgs) Handles orderSummaryPanel.Paint
    End Sub

    Private Sub totalPanel_Paint(sender As Object, e As PaintEventArgs) Handles totalPanel.Paint
    End Sub

    Private Sub Guna2Button2_Click(sender As Object, e As EventArgs)
    End Sub

    ' Customer data variables (moved to top of class)
    ' Private selectedCustomerId As Integer? = Nothing - REMOVED DUPLICATE
    ' Private selectedCustomerName As String = "Walk-in Customer" - REMOVED DUPLICATE

    ' Payment and discount methods - Updated to remove PIN
    Private Sub btnPayment_Click(sender As Object, e As EventArgs) Handles btnPayment.Click
        ' Validate user session
        If Not ValidateUserSession() Then
            Return
        End If

        ' Validate that there are items in the order
        If currentOrderList.Count = 0 Then
            MessageBox.Show("Please add items to the order before proceeding to payment.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Show customer selection panel instead of PIN
        ShowCustomerSelectionPanel()
    End Sub

    Private Sub ShowCustomerSelectionPanel()
        pinPanelActive = False
        totalPanelActive = True
        If pinPanel IsNot Nothing AndAlso Me.Controls.Contains(pinPanel) Then
            Me.Controls.Remove(pinPanel)
        End If
        totalReceivedPanel = New Guna.UI2.WinForms.Guna2Panel()
        totalReceivedPanel.Size = orderSummaryPanel.Size
        totalReceivedPanel.BorderRadius = 10
        totalReceivedPanel.FillColor = DarkSlate ' Updated color
        totalReceivedPanel.BorderColor = GoldenYellow
        totalReceivedPanel.BorderThickness = 1
        totalReceivedPanel.Location = orderSummaryPanel.Location

        ' Reset entered amount for keyboard handling
        enteredAmount = ""

        Dim lblTitle As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblTitle.Text = "Enter Payment Amount"
        lblTitle.Font = New Font("Poppins SemiBold", 15.0F, FontStyle.Regular)
        lblTitle.ForeColor = PureWhite ' Updated color
        lblTitle.AutoSize = True
        lblTitle.Location = New Point((totalReceivedPanel.Width - lblTitle.Width) / 2, 20)
        totalReceivedPanel.Controls.Add(lblTitle)

        ' Show selected customer
        Dim lblCustomer As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblCustomer.Text = $"Customer: {selectedCustomerName}"
        lblCustomer.Font = New Font("Poppins", 11.0F, FontStyle.Regular)
        lblCustomer.ForeColor = GoldenYellow
        lblCustomer.AutoSize = True
        lblCustomer.Location = New Point((totalReceivedPanel.Width - lblCustomer.Width) / 2, 45)
        totalReceivedPanel.Controls.Add(lblCustomer)

        lblAmountDisplay = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblAmountDisplay.Text = "0.00"
        lblAmountDisplay.Font = New Font("Poppins SemiBold", 28.0F, FontStyle.Regular)
        lblAmountDisplay.ForeColor = GoldenYellow ' Updated color for emphasis
        lblAmountDisplay.AutoSize = True
        lblAmountDisplay.Location = New Point((totalReceivedPanel.Width - lblAmountDisplay.Width) / 2, 80)
        totalReceivedPanel.Controls.Add(lblAmountDisplay)

        ' Reset and style the existing lblChange
        lblChange.Text = "0.00"
        lblChange.ForeColor = SuccessGreen ' Updated color
        lblChange.Font = New Font("Poppins SemiBold", 9.0F, FontStyle.Regular)
        lblChange.Visible = True

        Dim buttonSize As Integer = 60
        Dim buttonSpacing As Integer = 10
        Dim buttonStartX As Integer = (totalReceivedPanel.Width - (buttonSize * 3 + buttonSpacing * 2)) / 2
        Dim buttonStartY As Integer = 150
        Dim buttonTexts As String() = {"1", "2", "3", "4", "5", "6", "7", "8", "9", ".", "0", "X"}

        totalPanelButtons = New List(Of Guna.UI2.WinForms.Guna2Button)()
        For i = 0 To buttonTexts.Length - 1
            Dim button As New Guna.UI2.WinForms.Guna2Button()
            button.Size = New Size(buttonSize, buttonSize)
            button.BorderRadius = 10
            button.FillColor = SteelGray ' Updated color
            button.BackColor = DarkSlate
            button.ForeColor = PureWhite
            button.Font = New Font("Poppins SemiBold", 12.0F, FontStyle.Regular)
            button.Text = buttonTexts(i)
            button.TabStop = False

            ' Add hover effect
            AddHandler button.MouseEnter, Sub()
                                              button.FillColor = GoldenYellow
                                              button.ForeColor = DeepCharcoal
                                          End Sub
            AddHandler button.MouseLeave, Sub()
                                              button.FillColor = SteelGray
                                              button.ForeColor = PureWhite
                                          End Sub

            Dim row = i \ 3
            Dim col = i Mod 3
            button.Location = New Point(buttonStartX + col * (buttonSize + buttonSpacing), buttonStartY + row * (buttonSize + buttonSpacing))

            AddHandler button.Click, Sub(sender As Object, e As EventArgs)
                                         Dim btn As Guna.UI2.WinForms.Guna2Button = CType(sender, Guna.UI2.WinForms.Guna2Button)
                                         If btn.Text = "X" Then
                                             If enteredAmount.Length > 0 Then
                                                 enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1)
                                             End If
                                         ElseIf btn.Text = "." Then
                                             ' Allow only one decimal point
                                             If Not enteredAmount.Contains(".") AndAlso enteredAmount.Length > 0 Then
                                                 enteredAmount &= "."
                                             End If
                                         ElseIf btn.Text >= "0" And btn.Text <= "9" Then
                                             If enteredAmount.Length < 10 Then
                                                 enteredAmount &= btn.Text
                                             End If
                                         End If
                                         UpdateAmountDisplay()
                                     End Sub
            totalReceivedPanel.Controls.Add(button)
            totalPanelButtons.Add(button)
        Next

        ' Quick amount buttons
        Dim totalAmount As Decimal = 0D
        If totalLbl IsNot Nothing Then
            Decimal.TryParse(totalLbl.Text, totalAmount)
        End If

        ' Exact amount button
        Dim btnExact As New Guna.UI2.WinForms.Guna2Button()
        btnExact.Text = $"Exact: ?{totalAmount:F2}"
        btnExact.Size = New Size(120, 35)
        btnExact.Location = New Point(20, buttonStartY + 5 * (buttonSize + buttonSpacing))
        btnExact.Font = New Font("Poppins", 9, FontStyle.Bold)
        btnExact.ForeColor = DeepCharcoal
        btnExact.FillColor = LightSilver
        btnExact.BorderRadius = 8
        AddHandler btnExact.Click, Sub()
                                       enteredAmount = totalAmount.ToString("F2").Replace(".", "")
                                       If enteredAmount.EndsWith("00") Then
                                           enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 2)
                                       End If
                                       UpdateAmountDisplay()
                                   End Sub
        totalReceivedPanel.Controls.Add(btnExact)

        Dim btnConfirm As New Guna.UI2.WinForms.Guna2Button()
        btnConfirm.Text = "Complete Sale"
        btnConfirm.Font = New Font("Poppins SemiBold", 12.0F, FontStyle.Regular)
        btnConfirm.ForeColor = DeepCharcoal
        btnConfirm.FillColor = SuccessGreen ' Updated color
        btnConfirm.Size = New Size(160, 40)
        btnConfirm.BorderRadius = 10
        btnConfirm.Location = New Point(150, buttonStartY + 5 * (buttonSize + buttonSpacing))
        btnConfirm.TabStop = False

        ' Add hover effect
        AddHandler btnConfirm.MouseEnter, Sub()
                                              btnConfirm.FillColor = GoldenYellow
                                          End Sub
        AddHandler btnConfirm.MouseLeave, Sub()
                                              btnConfirm.FillColor = SuccessGreen
                                          End Sub

        AddHandler btnConfirm.Click, Sub(sender As Object, e As EventArgs)
                                         confirmBtn.PerformClick()
                                     End Sub
        totalReceivedPanel.Controls.Add(btnConfirm)

        ' Add a back button to total received panel
        Dim btnBackTotal As New Guna.UI2.WinForms.Guna2Button()
        btnBackTotal.Text = "‹"
        btnBackTotal.Font = New Font("Poppins", 12.0F, FontStyle.Regular)
        btnBackTotal.ForeColor = PureWhite
        btnBackTotal.FillColor = AlertRed
        btnBackTotal.Size = New Size(34, 33)
        btnBackTotal.BackColor = DarkSlate
        btnBackTotal.BorderRadius = 8
        btnBackTotal.Location = New Point(totalReceivedPanel.Width - 400, 15)
        AddHandler btnBackTotal.Click, Sub(sender, e)
                                           ' Remove the payment panel
                                           If totalReceivedPanel IsNot Nothing AndAlso Me.Controls.Contains(totalReceivedPanel) Then
                                               Me.Controls.Remove(totalReceivedPanel)
                                               totalReceivedPanel = Nothing
                                           End If

                                           ' Reset panel states
                                           totalPanelActive = False
                                           pinPanelActive = False

                                           ' Reset labels
                                           If lblChange IsNot Nothing Then
                                               lblChange.Text = "0.00"
                                           End If
                                           If totalRLbl IsNot Nothing Then
                                               totalRLbl.Text = "0.00"
                                           End If

                                           ' Reset entered amount
                                           enteredAmount = ""

                                           ' Show the original payment button and hide confirm button
                                           btnPayment.Visible = True
                                           confirmBtn.Visible = False

                                           ' Make sure the order summary panel is visible and brought to front
                                           If orderSummaryPanel IsNot Nothing Then
                                               orderSummaryPanel.Visible = True
                                               orderSummaryPanel.BringToFront()
                                           End If

                                           ' Refresh the order display to show all cart items
                                           RefreshOrderDisplay()

                                           ' Re-enable barcode input focus
                                           FocusBarcodeInputIfAllowed()
                                       End Sub
        totalReceivedPanel.Controls.Add(btnBackTotal)

        Me.Controls.Add(totalReceivedPanel)
        totalPanelActive = True

        ' Hide btnPayment and show confirmBtn
        btnPayment.Visible = False
        confirmBtn.Visible = True

        ' Bring totalReceivedPanel to front
        totalReceivedPanel.BringToFront()
    End Sub

    Private Sub UpdateAmountDisplay()
        ' Format as decimal with two places
        Dim displayValue As Decimal = 0D
        Dim amountText As String = enteredAmount

        ' Handle decimal formatting
        If amountText.Contains(".") Then
            ' User entered a decimal point
            If Decimal.TryParse(amountText, displayValue) Then
                lblAmountDisplay.Text = displayValue.ToString("F2")
            Else
                lblAmountDisplay.Text = "0.00"
            End If
        Else
            ' No decimal point, treat as cents
            If amountText.Length = 0 Then
                displayValue = 0
            ElseIf Decimal.TryParse(amountText, displayValue) Then
                displayValue = displayValue / 100
            End If
            lblAmountDisplay.Text = displayValue.ToString("F2")
        End If

        If lblAmountDisplay IsNot Nothing AndAlso totalReceivedPanel IsNot Nothing Then
            lblAmountDisplay.Location = New Point((totalReceivedPanel.Width - lblAmountDisplay.Width) / 2, lblAmountDisplay.Location.Y)
        End If

        ' Update totalRLbl in real-time
        If totalRLbl IsNot Nothing Then
            totalRLbl.Text = lblAmountDisplay.Text
        End If

        ' Compute change in real-time - only if an amount has been entered
        If enteredAmount.Length = 0 Then
            ' No amount entered yet, show 0.00 for change
            If lblChange IsNot Nothing Then
                lblChange.Text = "0.00"
                lblChange.ForeColor = SuccessGreen
                lblChange.Visible = True
            End If
        Else
            Dim orderTotal As Decimal = 0D
            If totalLbl IsNot Nothing AndAlso Decimal.TryParse(totalLbl.Text, orderTotal) Then
                Dim changeVal As Decimal = displayValue - orderTotal
                If lblChange IsNot Nothing Then
                    lblChange.Text = changeVal.ToString("F2")
                    lblChange.ForeColor = SuccessGreen
                    lblChange.Visible = True
                End If
            End If
        End If
    End Sub

    ' Refresh the order display in the order summary panel
    Private Sub RefreshOrderDisplay()
        ' Reset change label and totalRLbl when in normal order summary mode
        If Not pinPanelActive AndAlso Not totalPanelActive Then
            If lblChange IsNot Nothing Then
                lblChange.Text = "0.00"
                totalRLbl.Text = "0.00"
            End If
        End If

        ' Remove only product panels, keep Order ID and OrderName labels
        For i = orderSummaryPanel.Controls.Count - 1 To 0 Step -1
            Dim ctrl = orderSummaryPanel.Controls(i)
            If TypeOf ctrl Is Guna.UI2.WinForms.Guna2Panel Then
                orderSummaryPanel.Controls.RemoveAt(i)
            End If
        Next

        ' Add all products in the order list as rows
        Dim panelHeight As Integer = 50
        Dim marginY As Integer = 10
        Dim currentY As Integer = 50 ' Start after Order ID/OrderName labels
        Dim subtotal As Decimal = 0
        For i = 0 To currentOrderList.Count - 1
            Dim prod = currentOrderList(i)
            Dim orderPanel As New Guna.UI2.WinForms.Guna2Panel()
            orderPanel.Size = New Size(orderSummaryPanel.Width - 25, panelHeight)
            orderPanel.BorderRadius = 10
            orderPanel.FillColor = Color.FromArgb(61, 65, 66)
            orderPanel.Location = New Point(20, currentY)
            currentY += panelHeight + marginY

            ' Store the product index in the Tag property for easy access
            orderPanel.Tag = i

            ' Add double-click event handler to reduce quantity
            AddHandler orderPanel.DoubleClick, Sub(sender As Object, e As EventArgs)
                                                   ReduceItemQuantity(CInt(orderPanel.Tag))
                                               End Sub

            ' Order ID
            Dim lblOrderId As New Guna.UI2.WinForms.Guna2HtmlLabel()
            lblOrderId.Text = (i + 1).ToString("D2")
            lblOrderId.Font = New Font("Poppins Light", 9.0F, FontStyle.Regular)
            lblOrderId.ForeColor = Color.White
            lblOrderId.Location = New Point(12, 10)
            lblOrderId.AutoSize = True

            ' Product Name with ellipsis and tooltip
            Dim fullProductName As String = prod("ProductName").ToString()
            Dim maxNameLength As Integer = 15
            Dim displayName As String = If(fullProductName.Length > maxNameLength, fullProductName.Substring(0, maxNameLength) & "...", fullProductName)

            Dim lblCustomer As New Guna.UI2.WinForms.Guna2HtmlLabel()
            lblCustomer.Text = displayName
            lblCustomer.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            lblCustomer.ForeColor = Color.White
            lblCustomer.Location = New Point(lblOrderId.Right + 20, 10)
            lblCustomer.AutoSize = True

            ' Add tooltip for full product name if truncated
            If fullProductName.Length > maxNameLength Then
                Dim toolTip As New ToolTip()
                toolTip.SetToolTip(lblCustomer, fullProductName)
            End If
            orderPanel.Controls.Add(lblCustomer)

            ' Check if this item has promotion
            Dim hasPromotion As Boolean = If(prod.ContainsKey("HasPromotion"), CBool(prod("HasPromotion")), False)

            ' Quantity
            Dim lblQuantity As New Guna.UI2.WinForms.Guna2HtmlLabel()
            lblQuantity.Text = prod("Quantity").ToString() & "x"
            lblQuantity.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            lblQuantity.ForeColor = Color.FromArgb(119, 121, 121)
            lblQuantity.Location = New Point(290, 10)
            lblQuantity.AutoSize = True

            ' Price (use the promotional price if available)
            Dim priceVal As Decimal = Convert.ToDecimal(prod("Price")) * CInt(prod("Quantity"))
            Dim lblTotal As New Guna.UI2.WinForms.Guna2HtmlLabel()
            lblTotal.Text = priceVal.ToString("F2")
            lblTotal.Font = New Font("Poppins Regular", 9.0F)
            lblTotal.ForeColor = If(hasPromotion, Color.FromArgb(255, 69, 0), Color.White) ' Orange for promo items
            lblTotal.Location = New Point(orderPanel.Width - 70, 12)
            lblTotal.AutoSize = True

            ' Check if this item has promotion and show indicator
            If hasPromotion Then
                ' Show promotion indicator
                Dim lblPromoIndicator As New Label()
                lblPromoIndicator.Text = "🏷️"  ' Sale tag emoji
                lblPromoIndicator.Font = New Font("Segoe UI Emoji", 10.0F, FontStyle.Regular)
                lblPromoIndicator.ForeColor = Color.White
                lblPromoIndicator.Location = New Point(170, 12)
                lblPromoIndicator.AutoSize = True
                orderPanel.Controls.Add(lblPromoIndicator)
            End If

            ' Add double-click handlers to all child controls so they trigger the panel's double-click
            AddHandler lblOrderId.DoubleClick, Sub(sender As Object, e As EventArgs)
                                                   ReduceItemQuantity(CInt(orderPanel.Tag))
                                               End Sub
            AddHandler lblCustomer.DoubleClick, Sub(sender As Object, e As EventArgs)
                                                    ReduceItemQuantity(CInt(orderPanel.Tag))
                                                End Sub
            AddHandler lblQuantity.DoubleClick, Sub(sender As Object, e As EventArgs)
                                                    ReduceItemQuantity(CInt(orderPanel.Tag))
                                                End Sub
            AddHandler lblTotal.DoubleClick, Sub(sender As Object, e As EventArgs)
                                                 ReduceItemQuantity(CInt(orderPanel.Tag))
                                             End Sub

            ' Add hover effect to indicate interactivity
            AddHandler orderPanel.MouseEnter, Sub(sender As Object, e As EventArgs)
                                                  orderPanel.FillColor = Color.FromArgb(71, 75, 76)
                                                  orderPanel.Cursor = Cursors.Hand
                                              End Sub
            AddHandler orderPanel.MouseLeave, Sub(sender As Object, e As EventArgs)
                                                  orderPanel.FillColor = Color.FromArgb(61, 65, 66)
                                                  orderPanel.Cursor = Cursors.Default
                                              End Sub

            orderPanel.Controls.Add(lblOrderId)
            orderPanel.Controls.Add(lblCustomer)
            orderPanel.Controls.Add(lblQuantity)
            orderPanel.Controls.Add(lblTotal)

            orderSummaryPanel.Controls.Add(orderPanel)
            subtotal += priceVal
        Next

        ' Update subtotal label
        If lblSubTotal IsNot Nothing Then
            lblSubTotal.Text = subtotal.ToString("F2")
        End If

        ' Apply discount if any
        Dim discountedSubtotal As Decimal = subtotal - discountAmount

        ' Calculate and show 12% tax on discounted amount
        Dim totalWithTax As Decimal = discountedSubtotal * 1.12D
        If totalLbl IsNot Nothing Then
            totalLbl.Text = totalWithTax.ToString("F2")
        End If
        ' Do NOT set totalRLbl here
        ' Show taxLbl as 12%
        ' If taxLbl IsNot Nothing Then
        '     taxLbl.Text = "12%"
        ' End If
    End Sub

    ' Confirm payment and process order
    Private Sub confirmBtn_Click(sender As Object, e As EventArgs) Handles confirmBtn.Click
        Try
            ' Validate user session
            If Not ValidateUserSession() Then
                Return
            End If

            ' Payment confirmation logic
            Dim orderTotal As Decimal = 0D
            Dim receivedAmount As Decimal = 0D
            If totalLbl IsNot Nothing Then
                Decimal.TryParse(totalLbl.Text, orderTotal)
            End If
            If lblAmountDisplay IsNot Nothing Then
                Decimal.TryParse(lblAmountDisplay.Text, receivedAmount)
            End If

            If receivedAmount < orderTotal Then
                MessageBox.Show("Total received must be greater than or equal to order total.", "Payment Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If totalRLbl IsNot Nothing Then
                totalRLbl.Text = lblAmountDisplay.Text
            End If

            ' Get userId from logged-in username
            Dim userIdQuery As String = "SELECT UserID FROM Users WHERE Username = @Username"
            Dim userIdParams As SqlParameter() = {
                New SqlParameter("@Username", frmLoginvb.LoggedInUsername)
            }
            Dim userIdResult = Utilities.ExecuteScalar(userIdQuery, userIdParams)

            If userIdResult Is Nothing OrElse IsDBNull(userIdResult) Then
                MessageBox.Show("Invalid user session. Please log in again.", "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                frmLoginvb.Show()
                Me.Hide()
                Return
            End If

            Dim userId As Integer = Convert.ToInt32(userIdResult)

            ' Calculate change
            Dim changeAmount As Decimal = receivedAmount - orderTotal

            ' Insert sale record with customer information
            Dim saleQuery As String = "INSERT INTO Sales (CustomerID, UserID, SaleDate, TotalAmount, DiscountAmount, TaxAmount, Status, CustomerName, CustomerPhone, CustomerEmail, AmountReceived, ChangeAmount) OUTPUT INSERTED.SaleID VALUES (@CustomerID, @UserID, @SaleDate, @TotalAmount, @DiscountAmount, @TaxAmount, @Status, @CustomerName, @CustomerPhone, @CustomerEmail, @AmountReceived, @ChangeAmount)"
            Dim saleParams As SqlParameter() = {
                New SqlParameter("@CustomerID", If(selectedCustomerId.HasValue, selectedCustomerId.Value, DBNull.Value)),
                New SqlParameter("@UserID", userId),
                New SqlParameter("@SaleDate", DateTime.Now),
                New SqlParameter("@TotalAmount", orderTotal),
                New SqlParameter("@DiscountAmount", discountAmount),
                New SqlParameter("@TaxAmount", orderTotal - (orderTotal / 1.12D)),
                New SqlParameter("@Status", "Completed"),
                New SqlParameter("@CustomerName", selectedCustomerName),
                New SqlParameter("@CustomerPhone", If(String.IsNullOrEmpty(selectedCustomerPhone), DBNull.Value, selectedCustomerPhone)),
                New SqlParameter("@CustomerEmail", If(String.IsNullOrEmpty(selectedCustomerEmail), DBNull.Value, selectedCustomerEmail)),
                New SqlParameter("@AmountReceived", receivedAmount),
                New SqlParameter("@ChangeAmount", changeAmount)
            }

            Dim saleId As Integer = Convert.ToInt32(Utilities.ExecuteScalar(saleQuery, saleParams))

            ' Insert sale items and update stock
            For Each item In currentOrderList
                ' Insert sale item
                Dim itemQuery As String = "INSERT INTO SaleItems (SaleID, ProductID, Quantity, UnitPrice, TotalPrice) VALUES (@SaleID, @ProductID, @Quantity, @UnitPrice, @TotalPrice)"
                Dim itemParams As SqlParameter() = {
                    New SqlParameter("@SaleID", saleId),
                    New SqlParameter("@ProductID", item("ProductID")),
                    New SqlParameter("@Quantity", item("Quantity")),
                    New SqlParameter("@UnitPrice", item("Price")),
                    New SqlParameter("@TotalPrice", Convert.ToDecimal(item("Price")) * CInt(item("Quantity")))
                }
                Utilities.ExecuteNonQuery(itemQuery, itemParams)

                ' Update product stock
                Dim stockQuery As String = "UPDATE Products SET CurrentStock = CurrentStock - @Quantity WHERE ProductID = @ProductID"
                Dim stockParams As SqlParameter() = {
                    New SqlParameter("@Quantity", item("Quantity")),
                    New SqlParameter("@ProductID", item("ProductID"))
                }
                Utilities.ExecuteNonQuery(stockQuery, stockParams)
            Next

            ' Prepare receipt data
            receiptOrderId = saleId.ToString()
            receiptCustomerName = selectedCustomerName
            receiptTotalAmount = orderTotal
            receiptAmountReceived = receivedAmount
            receiptChange = changeAmount
            receiptSubtotal = orderTotal / 1.12D
            receiptTax = orderTotal - receiptSubtotal
            receiptItems = New List(Of Dictionary(Of String, Object))(currentOrderList)

            ' Print receipt
            PrintReceipt()

            ' Build order details for audit log
            Dim orderDetails As New List(Of String)()
            For Each item In currentOrderList
                orderDetails.Add($"{item("ProductName")} x{item("Quantity")} = ₱{Convert.ToDecimal(item("Price")) * CInt(item("Quantity")):F2}")
            Next

            ' Insert comprehensive audit log
            Dim auditDetails As String = $"Sale ID: {saleId}, Customer: {selectedCustomerName}, Items: [{String.Join(", ", orderDetails)}], Subtotal: ₱{receiptSubtotal:F2}"
            If discountAmount > 0 Then
                auditDetails += $", Discount ({discountType}): -₱{discountAmount:F2}"
            End If
            auditDetails += $", Tax: ₱{receiptTax:F2}, Total: ₱{orderTotal:F2}, Received: ₱{receivedAmount:F2}, Change: ₱{changeAmount:F2}"

            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Sale Completed", auditDetails)

            ' Show success message
            MessageBox.Show($"Sale completed successfully! Sale ID: {saleId}{Environment.NewLine}Customer: {selectedCustomerName}", "Sale Completed", MessageBoxButtons.OK, MessageBoxIcon.Information)

            ' Reset for next sale
            ResetSale()

        Catch ex As Exception
            MessageBox.Show($"Error processing sale: {ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Sale Failed", $"Error: {ex.Message}")
        End Try
    End Sub

    ' Print receipt
    Private Sub PrintReceipt()
        Try
            Dim printDoc As New PrintDocument()
            printDoc.DefaultPageSettings.PaperSize = New PaperSize("Receipt", 300, 700)
            printDoc.DefaultPageSettings.Margins = New Margins(10, 10, 10, 10)

            AddHandler printDoc.PrintPage, AddressOf OnPrintPage

            Dim printPreview As New PrintPreviewDialog()
            printPreview.Document = printDoc
            printPreview.Text = "Receipt Preview - Jade Clinic"
            printPreview.WindowState = FormWindowState.Maximized
            printPreview.ShowDialog()
        Catch ex As Exception
            MessageBox.Show($"Error printing receipt: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Reset sale data for next transaction
    Private Sub ResetSale()
        ' Clear order
        currentOrderList.Clear()

        ' Reset customer info
        selectedCustomerId = Nothing
        selectedCustomerName = "Walk-in Customer"
        selectedCustomerPhone = ""
        selectedCustomerEmail = ""

        ' Reset discount
        discountAmount = 0
        discountType = "None"
        discountValue = 0

        ' Clear panels
        If totalReceivedPanel IsNot Nothing AndAlso Me.Controls.Contains(totalReceivedPanel) Then
            Me.Controls.Remove(totalReceivedPanel)
            totalReceivedPanel = Nothing
        End If

        If pinPanel IsNot Nothing AndAlso Me.Controls.Contains(pinPanel) Then
            Me.Controls.Remove(pinPanel)
            pinPanel = Nothing
        End If

        ' Reset panel states
        pinPanelActive = False
        totalPanelActive = False

        ' Reset displays
        RefreshOrderDisplay()

        ' Return to main category view
        If backCategory.Visible Then
            backCategory.PerformClick()
        End If

        ' Reset entered amount
        enteredAmount = ""

        ' Update order ID for next sale
        InitializeOrderId()

        ' Update category counts
        UpdateCategoryItemCounts()

        ' Reset UI state
        confirmBtn.Visible = False
        btnPayment.Visible = True

        ' Ensure orderSummaryPanel is visible and properly restored
        If Not Me.Controls.Contains(orderSummaryPanel) Then
            Me.Controls.Add(orderSummaryPanel)
        End If
        orderSummaryPanel.Visible = True
        orderSummaryPanel.BringToFront()
    End Sub

    ' Initialize order ID display
    Private Sub InitializeOrderId()
        Try
            Dim nextOrderId As Integer = 1
            Dim query As String = "SELECT ISNULL(MAX(SaleID), 0) + 1 AS NextOrderID FROM Sales"
            Using reader As SqlDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                If reader.Read() Then
                    nextOrderId = Convert.ToInt32(reader("NextOrderID"))
                End If
            End Using
            lblOrderId.Text = $"Order ID: {nextOrderId}"
        Catch ex As Exception
            lblOrderId.Text = "Order ID: 1"
        End Try
    End Sub

    ' Discount button click handler
    Private Sub btnDiscount_Click(sender As Object, e As EventArgs) Handles btnDiscount.Click
        ' Only allow discount if there are items in the order
        If currentOrderList.Count = 0 Then
            MessageBox.Show("Please add items to the order before applying a discount.", "No Items", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Show simple discount dialog
        ShowSimpleDiscountDialog()
    End Sub

    ' Simple discount dialog
    Private Sub ShowSimpleDiscountDialog()
        Dim discountForm As New Form()
        discountForm.Text = "Apply Discount"
        discountForm.Size = New Size(350, 250)
        discountForm.StartPosition = FormStartPosition.CenterParent
        discountForm.FormBorderStyle = FormBorderStyle.FixedDialog
        discountForm.MaximizeBox = False
        discountForm.MinimizeBox = False
        discountForm.BackColor = DarkSlate

        ' Percentage discount
        Dim lblPercentage As New Label()
        lblPercentage.Text = "Percentage Discount (%):"
        lblPercentage.Location = New Point(20, 30)
        lblPercentage.Size = New Size(150, 25)
        lblPercentage.Font = New Font("Poppins", 10, FontStyle.Regular)
        lblPercentage.ForeColor = PureWhite
        discountForm.Controls.Add(lblPercentage)

        Dim txtPercentage As New TextBox()
        txtPercentage.Location = New Point(180, 30)
        txtPercentage.Size = New Size(80, 25)
        txtPercentage.Text = "0"
        discountForm.Controls.Add(txtPercentage)

        Dim btnApplyPercentage As New Button()
        btnApplyPercentage.Text = "Apply %"
        btnApplyPercentage.Location = New Point(270, 30)
        btnApplyPercentage.Size = New Size(60, 25)
        btnApplyPercentage.BackColor = GoldenYellow
        btnApplyPercentage.ForeColor = DeepCharcoal
        AddHandler btnApplyPercentage.Click, Sub()
                                                 Dim percentage As Decimal
                                                 If Decimal.TryParse(txtPercentage.Text, percentage) Then
                                                     ApplyPercentageDiscount(percentage)
                                                     discountForm.Close()
                                                 Else
                                                     MessageBox.Show("Please enter a valid percentage.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                 End If
                                             End Sub
        discountForm.Controls.Add(btnApplyPercentage)

        ' Fixed discount
        Dim lblFixed As New Label()
        lblFixed.Text = "Fixed Discount (₱):"
        lblFixed.Location = New Point(20, 80)
        lblFixed.Size = New Size(150, 25)
        lblFixed.Font = New Font("Poppins", 10, FontStyle.Regular)
        lblFixed.ForeColor = PureWhite
        discountForm.Controls.Add(lblFixed)

        Dim txtFixed As New TextBox()
        txtFixed.Location = New Point(180, 80)
        txtFixed.Size = New Size(80, 25)
        txtFixed.Text = "0.00"
        discountForm.Controls.Add(txtFixed)

        Dim btnApplyFixed As New Button()
        btnApplyFixed.Text = "Apply ₱"
        btnApplyFixed.Location = New Point(270, 80)
        btnApplyFixed.Size = New Size(60, 25)
        btnApplyFixed.BackColor = GoldenYellow
        btnApplyFixed.ForeColor = DeepCharcoal
        AddHandler btnApplyFixed.Click, Sub()
                                            Dim amount As Decimal
                                            If Decimal.TryParse(txtFixed.Text, amount) Then
                                                ApplyFixedDiscount(amount)
                                                discountForm.Close()
                                            Else
                                                MessageBox.Show("Please enter a valid amount.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                            End If
                                        End Sub
        discountForm.Controls.Add(btnApplyFixed)

        ' Remove discount button
        Dim btnRemoveDiscount As New Button()
        btnRemoveDiscount.Text = "Remove Discount"
        btnRemoveDiscount.Size = New Size(150, 30)
        btnRemoveDiscount.BackColor = AlertRed
        btnRemoveDiscount.ForeColor = PureWhite
        btnRemoveDiscount.Location = New Point((discountForm.ClientSize.Width - btnRemoveDiscount.Width) \ 2, 130)
        AddHandler btnRemoveDiscount.Click, Sub()
                                                RemoveDiscount()
                                                discountForm.Close()
                                            End Sub
        discountForm.Controls.Add(btnRemoveDiscount)

        ' Show current discount if any
        If discountAmount > 0 Then
            Dim lblCurrentDiscount As New Label()
            lblCurrentDiscount.Text = $"Current: {discountType} ₱{discountAmount:F2}"
            lblCurrentDiscount.Location = New Point(20, 180)
            lblCurrentDiscount.Size = New Size(300, 25)
            lblCurrentDiscount.Font = New Font("Poppins", 9, FontStyle.Regular)
            lblCurrentDiscount.ForeColor = GoldenYellow
            discountForm.Controls.Add(lblCurrentDiscount)
        End If

        discountForm.ShowDialog()
    End Sub

    ' Apply percentage discount
    Private Sub ApplyPercentageDiscount(percentage As Decimal)
        If percentage < 0 Or percentage > 100 Then
            MessageBox.Show("Discount percentage must be between 0 and 100.", "Invalid Discount", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        discountType = "Percentage"
        discountValue = percentage

        Dim currentSubtotal As Decimal = 0
        If lblSubTotal IsNot Nothing Then
            Decimal.TryParse(lblSubTotal.Text, currentSubtotal)
        End If

        discountAmount = currentSubtotal * (percentage / 100)
        RefreshOrderDisplay()

        MessageBox.Show($"Applied {percentage}% discount (₱{discountAmount:F2})", "Discount Applied", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' Apply fixed discount
    Private Sub ApplyFixedDiscount(amount As Decimal)
        Dim currentSubtotal As Decimal = 0
        If lblSubTotal IsNot Nothing Then
            Decimal.TryParse(lblSubTotal.Text, currentSubtotal)
        End If

        If amount < 0 Or amount > currentSubtotal Then
            MessageBox.Show($"Discount amount must be between ₱0 and ₱{currentSubtotal:F2}.", "Invalid Discount", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        discountType = "Fixed"
        discountValue = amount
        discountAmount = amount
        RefreshOrderDisplay()

        MessageBox.Show($"Applied fixed discount of ₱{amount:F2}", "Discount Applied", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' Remove discount
    Private Sub RemoveDiscount()
        discountType = "None"
        discountValue = 0
        discountAmount = 0
        RefreshOrderDisplay()

        MessageBox.Show("Discount removed", "Discount Removed", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub InitializeProfileSection()
        Try
            ' Set username without emoji
            lblUsername.Text = frmLoginvb.LoggedInUsername
            lblUsername.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
            lblUsername.ForeColor = PureWhite

            ' Load user profile picture
            LoadUserProfilePicture()

            ' Add click event to profile picture and username
            AddHandler Guna2CirclePictureBox5.Click, AddressOf ProfilePicture_Click
            AddHandler lblUsername.Click, AddressOf ProfilePicture_Click

            ' Add hover effects
            AddHandler Guna2CirclePictureBox5.MouseEnter, Sub()
                                                              Guna2CirclePictureBox5.Cursor = Cursors.Hand
                                                          End Sub
            AddHandler lblUsername.MouseEnter, Sub()
                                                   lblUsername.Cursor = Cursors.Hand
                                               End Sub

        Catch ex As Exception
            ' Fallback if there's an error
            lblUsername.Text = frmLoginvb.LoggedInUsername
            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
        End Try
    End Sub

    Private Sub LoadUserProfilePicture()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                ' Query to get the logged-in user's photo
                Dim query As String = "SELECT Photo FROM Users WHERE Username = @Username"
                Dim parameters As SqlParameter() = {
                    New SqlParameter("@Username", frmLoginvb.LoggedInUsername)
                }

                Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                    If reader.Read() Then
                        ' Configure the PictureBox for circular profile picture
                        Guna2CirclePictureBox5.SizeMode = PictureBoxSizeMode.Zoom
                        Guna2CirclePictureBox5.BorderStyle = BorderStyle.None

                        If Not IsDBNull(reader("Photo")) Then
                            ' Load user's actual photo
                            Dim photoBytes As Byte() = CType(reader("Photo"), Byte())
                            Using ms As New IO.MemoryStream(photoBytes)
                                Dim loadedImage As Image = Image.FromStream(ms)
                                Guna2CirclePictureBox5.Image = New Bitmap(loadedImage)
                                loadedImage.Dispose()
                            End Using
                        Else
                            ' Create and display default avatar
                            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
                        End If
                    End If
                End Using
            End If
        Catch ex As Exception
            ' If there's an error, show default avatar
            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(If(frmLoginvb.LoggedInUsername, "User"))
        End Try
    End Sub

    ' Create default profile avatar method
    Private Function CreateDefaultProfileAvatar(username As String) As System.Drawing.Image
        Dim bitmap As New Bitmap(50, 50)
        Using g As Graphics = Graphics.FromImage(bitmap)
            ' Enable anti-aliasing for smooth circles
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            ' Fill background with a color based on username
            Dim colors() As System.Drawing.Color = {
                System.Drawing.Color.FromArgb(255, 107, 107),
                System.Drawing.Color.FromArgb(78, 205, 196),
                System.Drawing.Color.FromArgb(85, 98, 112),
                System.Drawing.Color.FromArgb(129, 236, 236),
                System.Drawing.Color.FromArgb(116, 185, 255)
            }
            Dim colorIndex As Integer = Math.Abs(username.GetHashCode()) Mod colors.Length
            g.FillEllipse(New SolidBrush(colors(colorIndex)), 0, 0, 50, 50)

            ' Draw initials
            Dim initials As String = ""
            If username.Length > 0 Then
                initials = username.Substring(0, 1).ToUpper()
                If username.Length > 1 Then
                    For i As Integer = 1 To username.Length - 1
                        If Char.IsUpper(username(i)) OrElse username(i) = " "c Then
                            If username(i) <> " "c Then
                                initials += username(i).ToString().ToUpper()
                                Exit For
                            End If
                        End If
                    Next
                End If
            End If

            Using font As New System.Drawing.Font("Poppins", 14, System.Drawing.FontStyle.Bold)
                Dim textSize = g.MeasureString(initials, font)
                g.DrawString(initials, font, New SolidBrush(PureWhite),
                    (50 - textSize.Width) / 2, (50 - textSize.Height) / 2)
            End Using
        End Using
        Return bitmap
    End Function

    Private Sub ProfilePicture_Click(sender As Object, e As EventArgs)
        ToggleProfileDropdown()
    End Sub

    Private Sub ToggleProfileDropdown()
        If isProfileDropdownVisible Then
            HideProfileDropdown()
        Else
            ShowProfileDropdown()
        End If
    End Sub

    Private Sub ShowProfileDropdown()
        If profileDropdownPanel IsNot Nothing Then
            HideProfileDropdown()
        End If

        ' Create dropdown panel
        profileDropdownPanel = New Panel()
        profileDropdownPanel.Size = New System.Drawing.Size(200, 100)
        profileDropdownPanel.BackColor = DarkSlate ' Updated color
        profileDropdownPanel.BorderStyle = BorderStyle.FixedSingle

        ' Position below the profile picture
        Dim profileLocation = Guna2CirclePictureBox5.Location
        profileDropdownPanel.Location = New Point(profileLocation.X - 90, profileLocation.Y + Guna2CirclePictureBox5.Height + 5)

        ' Create Profile Settings button
        Dim btnProfileSettings As New Label()
        btnProfileSettings.Text = "⚙️ Profile Settings"
        btnProfileSettings.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnProfileSettings.ForeColor = PureWhite
        btnProfileSettings.BackColor = System.Drawing.Color.Transparent
        btnProfileSettings.Size = New System.Drawing.Size(190, 40)
        btnProfileSettings.Location = New Point(5, 5)
        btnProfileSettings.TextAlign = ContentAlignment.MiddleLeft
        btnProfileSettings.Cursor = Cursors.Hand

        ' Add hover effect to Profile Settings
        AddHandler btnProfileSettings.MouseEnter, Sub()
                                                      btnProfileSettings.BackColor = Graphite
                                                  End Sub
        AddHandler btnProfileSettings.MouseLeave, Sub()
                                                      btnProfileSettings.BackColor = System.Drawing.Color.Transparent
                                                  End Sub

        ' Add click event to Profile Settings
        AddHandler btnProfileSettings.Click, Sub()
                                                 HideProfileDropdown()
                                                 NavigateToProfileSettings()
                                             End Sub

        ' Create Log Out button
        Dim btnLogOut As New Label()
        btnLogOut.Text = "🚪 Log Out"
        btnLogOut.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnLogOut.ForeColor = PureWhite
        btnLogOut.BackColor = System.Drawing.Color.Transparent
        btnLogOut.Size = New System.Drawing.Size(190, 40)
        btnLogOut.Location = New Point(5, 50)
        btnLogOut.TextAlign = ContentAlignment.MiddleLeft
        btnLogOut.Cursor = Cursors.Hand

        ' Add hover effect to Log Out
        AddHandler btnLogOut.MouseEnter, Sub()
                                             btnLogOut.BackColor = Graphite
                                         End Sub
        AddHandler btnLogOut.MouseLeave, Sub()
                                             btnLogOut.BackColor = System.Drawing.Color.Transparent
                                         End Sub

        ' Add click event to Log Out
        AddHandler btnLogOut.Click, Sub()
                                        ' Confirm logout before proceeding
                                        Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

                                        If result = DialogResult.Yes Then
                                            ' Log the logout action
                                            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                                                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Log Out", "User logged out of the application.")
                                            End If

                                            ' Clear user session and return to login
                                            frmLoginvb.LogoutUser()

                                            ' Navigate to login form without closing the application
                                            isNavigating = True
                                            Me.Hide()
                                            Dim loginForm As New frmLoginvb()
                                            loginForm.Show()
                                        End If
                                    End Sub

        ' Add buttons to panel
        profileDropdownPanel.Controls.Add(btnProfileSettings)
        profileDropdownPanel.Controls.Add(btnLogOut)

        ' Add panel to form
        Me.Controls.Add(profileDropdownPanel)
        profileDropdownPanel.BringToFront()

        ' Add click event to form to hide dropdown when clicked elsewhere
        AddHandler Me.Click, AddressOf Form_Click

        isProfileDropdownVisible = True
    End Sub
    ' Navigation event handlers
    Private Sub NavDashboard_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Dashboard.Show()
        Me.Close()
    End Sub

    Private Sub NavInventory_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Inventory.Show()
        Me.Close()
    End Sub

    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)
        ' For now, stay on this form since this is the Sales page
        MessageBox.Show("You are already on the Sales page!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub NavStaff_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Staff.Show()
        Me.Close()
    End Sub

    Private Sub NavInventoryLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Me.Close()
    End Sub

    Private Sub HideProfileDropdown()
        If profileDropdownPanel IsNot Nothing Then
            Me.Controls.Remove(profileDropdownPanel)
            profileDropdownPanel.Dispose()
            profileDropdownPanel = Nothing
        End If
        isProfileDropdownVisible = False

        ' Remove form click event
        RemoveHandler Me.Click, AddressOf Form_Click
    End Sub

    Private Sub Form_Click(sender As Object, e As EventArgs)
        ' Hide dropdown when clicking elsewhere on the form
        HideProfileDropdown()
    End Sub

    Private Sub NavigateToProfileSettings()
        If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from Sales to ProfileSettings")
        End If
        isNavigating = True
        ' Implement ProfileSettings form later
        MessageBox.Show("Profile Settings will be implemented.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        ' For now, show coming soon message
        MessageBox.Show("Audit Logs feature coming soon!", "Feature Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub
    Private Sub HandleTotalAmountKeyboardInput(e As KeyEventArgs)
        ' Handle numeric keys (0-9)
        If (e.KeyCode >= Keys.D0 AndAlso e.KeyCode <= Keys.D9) OrElse
           (e.KeyCode >= Keys.NumPad0 AndAlso e.KeyCode <= Keys.NumPad9) Then

            Dim digit As String = ""
            If e.KeyCode >= Keys.D0 AndAlso e.KeyCode <= Keys.D9 Then
                digit = (e.KeyCode - Keys.D0).ToString()
            Else
                digit = (e.KeyCode - Keys.NumPad0).ToString()
            End If

            If enteredAmount.Length < 10 Then
                enteredAmount &= digit
                UpdateAmountDisplay()
            End If
            e.Handled = True

            ' Handle backspace
        ElseIf e.KeyCode = Keys.Back Then
            If enteredAmount.Length > 0 Then
                enteredAmount = enteredAmount.Substring(0, enteredAmount.Length - 1)
                UpdateAmountDisplay()
            End If
            e.Handled = True

            ' Handle Enter key to confirm payment
        ElseIf e.KeyCode = Keys.Enter Then
            confirmBtn.PerformClick()
            e.Handled = True

            ' Handle Escape to go back to PIN panel
        ElseIf e.KeyCode = Keys.Escape Then
            If totalReceivedPanel IsNot Nothing AndAlso Me.Controls.Contains(totalReceivedPanel) Then
                Me.Controls.Remove(totalReceivedPanel)
                totalPanelActive = False
            End If
            ' Reset change label and totalRLbl when going back to PIN panel
            If lblChange IsNot Nothing Then
                lblChange.Text = "0.00"
                totalRLbl.Text = "0.00"
            End If
            e.Handled = True
        End If
    End Sub
    ' Add this method to handle form-level key events
    Private Sub Sales_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        ' Only handle keyboard input when the total amount panel is active
        If totalPanelActive Then
            HandleTotalAmountKeyboardInput(e)
        End If
    End Sub

End Class