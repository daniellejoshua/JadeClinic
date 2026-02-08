Imports Microsoft.Data.SqlClient
Imports System.Data
Imports System.IO
Imports System.Threading.Tasks

Public Class Inventory
    Private allProducts As New List(Of Dictionary(Of String, Object))
    Private filteredProducts As New List(Of Dictionary(Of String, Object))
    Private visibleStartIndex As Integer = 0
    Private visibleItemCount As Integer = 15 ' Number of visible items
    Private itemHeight As Integer = 80 ' Height of each product panel
    Private loadingOverlay As Panel

    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    Private Sub Inventory_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Enable double buffering for smooth scrolling
        SetDoubleBuffered(stockPanel)

        ' Add scroll event handler
        AddHandler stockPanel.Scroll, AddressOf StockPanel_Scroll

        ' Make form non-resizable
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Create navigation menu (hardcoded from Dashboard)
        CreateNavigationMenu()

        ' Setup filter events
        SetupFilterEvents()

        ' Load categories for filter
        LoadCategoriesForFilter()

        ' Set button text
        btnManagePromotions.Text = "Manage Stock"

        ' Show loading overlay and load products asynchronously
        ShowLoadingOverlay()
        LoadProductsAsync()
    End Sub

    Private Sub SetDoubleBuffered(ctrl As Control)
        Try
            Dim prop = ctrl.GetType().GetProperty("DoubleBuffered", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
            If prop IsNot Nothing Then prop.SetValue(ctrl, True, Nothing)
        Catch ex As Exception
            ' Silent fail for double buffering
        End Try
    End Sub

    Private Sub ShowLoadingOverlay()
        ' Create loading overlay
        loadingOverlay = New Panel()
        loadingOverlay.BackColor = Color.FromArgb(200, 30, 30, 30) ' Semi-transparent dark
        loadingOverlay.Dock = DockStyle.Fill
        loadingOverlay.Location = New Point(0, 0)
        loadingOverlay.Size = Me.ClientSize


        ' Add overlay to form
        Me.Controls.Add(loadingOverlay)
        loadingOverlay.BringToFront()
    End Sub

    Private Sub HideLoadingOverlay()
        If loadingOverlay IsNot Nothing Then
            Me.Controls.Remove(loadingOverlay)
            loadingOverlay.Dispose()
            loadingOverlay = Nothing
        End If
    End Sub

    Private Async Sub LoadProductsAsync()
        Try
            ' Run the loading operation on a background thread
            Await Task.Run(Sub()
                               LoadProductsFromDatabase()
                           End Sub)

            ' Update UI on the main thread
            Me.Invoke(Sub()
                          ' Initially show all products
                          filteredProducts = New List(Of Dictionary(Of String, Object))(allProducts)

                          ' Update item count
                          lblUsername.Text = $"{filteredProducts.Count} Items"

                          ' Set up virtual scrolling and render
                          RefreshProductDisplay()

                          ' Hide loading overlay
                          HideLoadingOverlay()
                      End Sub)

        Catch ex As Exception
            ' Handle errors on main thread
            Me.Invoke(Sub()
                          HideLoadingOverlay()
                          MessageBox.Show("Error loading products: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                      End Sub)
        End Try
    End Sub

    Private Sub LoadProductsFromDatabase()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                ' Query to get ALL products but store in memory
                Dim query As String = "SELECT p.ProductID, p.ProductCode, p.Barcode, p.ProductName, p.Category, " &
                                     "p.Unit, p.CurrentStock, p.ReorderLevel, p.CostPrice, p.SellingPrice, " &
                                     "(SELECT TOP 1 ImageData FROM ProductImages WHERE ProductID = p.ProductID) AS ProductImage " &
                                     "FROM Products p WHERE p.IsActive = 1 ORDER BY p.ProductName"

                Using cmd As New SqlCommand(query, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        allProducts.Clear()

                        While reader.Read()
                            Dim productData As New Dictionary(Of String, Object) From {
                                {"ProductID", reader("ProductID")},
                                {"ProductCode", reader("ProductCode")},
                                {"Barcode", reader("Barcode")},
                                {"ProductName", reader("ProductName")},
                                {"Category", reader("Category")},
                                {"Unit", reader("Unit")},
                                {"CurrentStock", If(IsDBNull(reader("CurrentStock")), 0, Convert.ToInt32(reader("CurrentStock")))},
                                {"ReorderLevel", If(IsDBNull(reader("ReorderLevel")), 0, Convert.ToInt32(reader("ReorderLevel")))},
                                {"CostPrice", If(IsDBNull(reader("CostPrice")), 0D, Convert.ToDecimal(reader("CostPrice")))},
                                {"SellingPrice", If(IsDBNull(reader("SellingPrice")), 0D, Convert.ToDecimal(reader("SellingPrice")))},
                                {"ProductImage", If(Not IsDBNull(reader("ProductImage")), reader("ProductImage"), Nothing)}
                            }
                            allProducts.Add(productData)
                        End While
                    End Using
                End Using
            End Using

        Catch ex As Exception
            Throw ex ' Re-throw to be handled by the calling async method
        End Try
    End Sub

    Private Sub SetupFilterEvents()
        ' Setup filter events
        AddHandler txtSearch.TextChanged, AddressOf ApplyFilters
        AddHandler Guna2ComboBox1.SelectedIndexChanged, AddressOf ApplyFilters
        AddHandler StockCmbBox.SelectedIndexChanged, AddressOf ApplyFilters
        AddHandler txtFilterQuantity.TextChanged, AddressOf ApplyFilters
        AddHandler txtFilterPrice.TextChanged, AddressOf ApplyFilters
        AddHandler btnResetFilter.Click, AddressOf ResetFilters_Click

        ' Set default values
        StockCmbBox.SelectedIndex = 0 ' Select "All"

        ' Set placeholder text for better UX
        txtSearch.PlaceholderText = "Search by name, code, category, or barcode..."
        txtFilterQuantity.PlaceholderText = "Minimum quantity (e.g., 10)"
        txtFilterPrice.PlaceholderText = "Minimum price (e.g., 100.00)"
    End Sub

    Private Sub LoadCategoriesForFilter()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                ' Get distinct categories from existing products
                Dim query As String = "SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND IsActive = 1 ORDER BY Category"
                Using cmd As New SqlCommand(query, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        Guna2ComboBox1.Items.Clear()
                        Guna2ComboBox1.Items.Add("All Categories") ' Add default option

                        While reader.Read()
                            If Not IsDBNull(reader("Category")) Then
                                Guna2ComboBox1.Items.Add(reader("Category").ToString())
                            End If
                        End While

                        ' Select "All Categories" by default
                        Guna2ComboBox1.SelectedIndex = 0
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading categories for filter: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub RefreshProductDisplay()
        ' Set up virtual scrolling
        stockPanel.AutoScroll = True
        stockPanel.Controls.Clear()

        ' Create a spacer panel to enable scrolling
        Dim spacer As New Panel()
        spacer.Size = New Size(1, filteredProducts.Count * itemHeight)
        spacer.Location = New Point(0, 0)
        stockPanel.Controls.Add(spacer)

        ' Render only visible items
        RenderVisibleItems()
    End Sub

    Private Sub StockPanel_Scroll(sender As Object, e As ScrollEventArgs)
        ' Only re-render on vertical scroll
        If e.ScrollOrientation = ScrollOrientation.VerticalScroll Then
            RenderVisibleItems()
        End If
    End Sub

    Private Sub RenderVisibleItems()
        ' Calculate which items should be visible based on scroll position
        Dim scrollPos As Integer = stockPanel.VerticalScroll.Value
        Dim startIndex As Integer = Math.Max(0, (scrollPos \ itemHeight) - 2) ' 2 item buffer above
        Dim endIndex As Integer = Math.Min(filteredProducts.Count - 1, startIndex + visibleItemCount + 4) ' 2 item buffer below

        ' Clear existing product panels (keep the spacer)
        For i As Integer = stockPanel.Controls.Count - 1 To 0 Step -1
            If TypeOf stockPanel.Controls(i) Is Guna.UI2.WinForms.Guna2Panel Then
                stockPanel.Controls.RemoveAt(i)
            End If
        Next

        ' Render visible items
        For i As Integer = startIndex To endIndex
            If i >= 0 AndAlso i < filteredProducts.Count Then
                CreateProductPanel(filteredProducts(i), i)
            End If
        Next
    End Sub

    Private Sub CreateProductPanel(productData As Dictionary(Of String, Object), index As Integer)
        ' Create product panel
        Dim productPanel As New Guna.UI2.WinForms.Guna2Panel()
        productPanel.Size = New Size(870, 70)
        productPanel.Location = New Point(10, index * itemHeight + 10)
        productPanel.FillColor = Color.White
        productPanel.BorderRadius = 8
        productPanel.BorderColor = Color.FromArgb(240, 240, 240)
        productPanel.BorderThickness = 1

        ' Product image
        Dim picProductImage As New Guna.UI2.WinForms.Guna2PictureBox()
        picProductImage.Size = New Size(60, 60)
        picProductImage.Location = New Point(10, 5)
        picProductImage.BackColor = Color.White
        picProductImage.BorderRadius = 8
        picProductImage.SizeMode = PictureBoxSizeMode.Zoom

        If productData("ProductImage") IsNot Nothing Then
            Try
                Dim imgBytes As Byte() = CType(productData("ProductImage"), Byte())
                Using ms As New MemoryStream(imgBytes)
                    picProductImage.Image = Image.FromStream(ms)
                End Using
            Catch
                ' Image failed to load
            End Try
        End If
        productPanel.Controls.Add(picProductImage)

        ' Product Name
        Dim lblProductName As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblProductName.Text = productData("ProductName").ToString()
        lblProductName.Font = New Font("Poppins", 9, FontStyle.Bold)
        lblProductName.ForeColor = Color.FromArgb(60, 60, 60)
        lblProductName.Location = New Point(80, 8)
        lblProductName.AutoSize = True
        productPanel.Controls.Add(lblProductName)

        ' Product Code
        Dim lblProductCode As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblProductCode.Text = "Code: " & productData("ProductCode").ToString()
        lblProductCode.Font = New Font("Poppins", 7.5F, FontStyle.Regular)
        lblProductCode.ForeColor = Color.Gray
        lblProductCode.Location = New Point(80, 28)
        lblProductCode.AutoSize = True
        productPanel.Controls.Add(lblProductCode)

        ' Barcode
        Dim lblBarcode As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblBarcode.Text = "📦 " & productData("Barcode").ToString()
        lblBarcode.Font = New Font("Poppins", 7.5F, FontStyle.Regular)
        lblBarcode.ForeColor = Color.Gray
        lblBarcode.Location = New Point(80, 46)
        lblBarcode.AutoSize = True
        productPanel.Controls.Add(lblBarcode)

        ' Category
        Dim lblCategoryTitle As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblCategoryTitle.Text = "Category"
        lblCategoryTitle.Font = New Font("Poppins", 8, FontStyle.Bold)
        lblCategoryTitle.ForeColor = Color.Gray
        lblCategoryTitle.Location = New Point(300, 8)
        lblCategoryTitle.AutoSize = True
        productPanel.Controls.Add(lblCategoryTitle)

        Dim lblCategory As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblCategory.Text = productData("Category").ToString()
        lblCategory.Font = New Font("Poppins", 8, FontStyle.Regular)
        lblCategory.ForeColor = Color.FromArgb(60, 60, 60)
        lblCategory.Location = New Point(300, 28)
        lblCategory.AutoSize = True
        productPanel.Controls.Add(lblCategory)

        ' Unit
        Dim lblUnitTitle As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblUnitTitle.Text = "Unit"
        lblUnitTitle.Font = New Font("Poppins", 8, FontStyle.Bold)
        lblUnitTitle.ForeColor = Color.Gray
        lblUnitTitle.Location = New Point(410, 8)
        lblUnitTitle.AutoSize = True
        productPanel.Controls.Add(lblUnitTitle)

        Dim lblUnit As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblUnit.Text = productData("Unit").ToString()
        lblUnit.Font = New Font("Poppins", 8, FontStyle.Regular)
        lblUnit.ForeColor = Color.FromArgb(60, 60, 60)
        lblUnit.Location = New Point(410, 28)
        lblUnit.AutoSize = True
        productPanel.Controls.Add(lblUnit)

        ' Current Stock
        Dim lblStockTitle As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblStockTitle.Text = "Stock"
        lblStockTitle.Font = New Font("Poppins", 8, FontStyle.Bold)
        lblStockTitle.ForeColor = Color.Gray
        lblStockTitle.Location = New Point(490, 8)
        lblStockTitle.AutoSize = True
        productPanel.Controls.Add(lblStockTitle)

        Dim currentStock As Integer
        If Not Integer.TryParse(productData("CurrentStock").ToString(), currentStock) Then
            currentStock = 0
        End If

        Dim reorderLevel As Integer
        If Not Integer.TryParse(productData("ReorderLevel").ToString(), reorderLevel) Then
            reorderLevel = 0
        End If

        Dim lblStock As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblStock.Text = currentStock.ToString()
        lblStock.Font = New Font("Poppins", 9, FontStyle.Bold)
        lblStock.Location = New Point(490, 28)
        lblStock.AutoSize = True

        ' Color code stock levels
        If currentStock = 0 Then
            lblStock.ForeColor = Color.Red
        ElseIf currentStock <= reorderLevel Then
            lblStock.ForeColor = Color.Orange
        Else
            lblStock.ForeColor = Color.Green
        End If
        productPanel.Controls.Add(lblStock)

        ' Cost Price
        Dim lblCostTitle As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblCostTitle.Text = "Cost"
        lblCostTitle.Font = New Font("Poppins", 8, FontStyle.Bold)
        lblCostTitle.ForeColor = Color.Gray
        lblCostTitle.Location = New Point(560, 8)
        lblCostTitle.AutoSize = True
        productPanel.Controls.Add(lblCostTitle)

        Dim lblCost As New Label()
        lblCost.Text = "₱" & Convert.ToDecimal(productData("CostPrice")).ToString("N2")
        lblCost.Font = New Font("Poppins", 8, FontStyle.Regular)
        lblCost.ForeColor = Color.FromArgb(60, 60, 60)
        lblCost.Location = New Point(560, 28)
        lblCost.AutoSize = True
        lblCost.BackColor = Color.White
        productPanel.Controls.Add(lblCost)

        ' Selling Price
        Dim lblPriceTitle As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblPriceTitle.Text = "Price"
        lblPriceTitle.Font = New Font("Poppins", 8, FontStyle.Bold)
        lblPriceTitle.ForeColor = Color.Gray
        lblPriceTitle.Location = New Point(650, 8)
        lblPriceTitle.AutoSize = True
        productPanel.Controls.Add(lblPriceTitle)

        Dim lblPrice As New Label()
        lblPrice.Text = "₱" & Convert.ToDecimal(productData("SellingPrice")).ToString("N2")
        lblPrice.Font = New Font("Poppins", 9, FontStyle.Bold)
        lblPrice.ForeColor = Color.FromArgb(100, 88, 255)
        lblPrice.Location = New Point(650, 28)
        lblPrice.AutoSize = True
        lblPrice.BackColor = Color.White
        productPanel.Controls.Add(lblPrice)

        ' Edit icon
        Dim lblEdit As New Label()
        lblEdit.Text = "✏️"
        lblEdit.Font = New Font("Segoe UI Emoji", 14, FontStyle.Regular)
        lblEdit.Cursor = Cursors.Hand
        lblEdit.Location = New Point(770, 20)
        lblEdit.Size = New Size(35, 30)
        lblEdit.BackColor = Color.White
        lblEdit.Tag = productData("ProductID")
        AddHandler lblEdit.Click, AddressOf EditProduct_Click
        productPanel.Controls.Add(lblEdit)

        ' Add panel to stock panel
        stockPanel.Controls.Add(productPanel)
        productPanel.BringToFront()
    End Sub

    Private Sub EditProduct_Click(sender As Object, e As EventArgs)
        Dim productId As Integer = Convert.ToInt32(DirectCast(sender, Label).Tag)

        ' Create overlay panel for modal effect
        Dim overlayPanel As New Panel()
        overlayPanel.BackColor = Color.FromArgb(100, 0, 0, 0) ' Semi-transparent black
        overlayPanel.Dock = DockStyle.Fill
        overlayPanel.Location = New Point(0, 0)
        overlayPanel.Size = Me.ClientSize
        Me.Controls.Add(overlayPanel)
        overlayPanel.BringToFront()

        ' Create and show Edit Product form
        Dim editProductForm As New AddProduct()
        editProductForm.SetEditMode(productId)
        editProductForm.StartPosition = FormStartPosition.CenterParent

        ' Handle form closing to remove overlay
        AddHandler editProductForm.FormClosed, Sub(s, ev)
                                                   If overlayPanel IsNot Nothing AndAlso Not overlayPanel.IsDisposed Then
                                                       Me.Controls.Remove(overlayPanel)
                                                       overlayPanel.Dispose()
                                                   End If
                                               End Sub

        Dim result As DialogResult = editProductForm.ShowDialog(Me)

        ' Cleanup overlay if still exists
        If overlayPanel IsNot Nothing AndAlso Not overlayPanel.IsDisposed Then
            Me.Controls.Remove(overlayPanel)
            overlayPanel.Dispose()
        End If

        ' Refresh the inventory list if product was updated
        If result = DialogResult.OK Then
            LoadProducts()
        End If
    End Sub

    Private Sub Guna2Button1_Click(sender As Object, e As EventArgs) Handles Guna2Button1.Click
        ' Create overlay panel for modal effect
        Dim overlayPanel As New Panel()
        overlayPanel.BackColor = Color.FromArgb(100, 0, 0, 0) ' Semi-transparent black
        overlayPanel.Dock = DockStyle.Fill
        overlayPanel.Location = New Point(0, 0)
        overlayPanel.Size = Me.ClientSize
        Me.Controls.Add(overlayPanel)
        overlayPanel.BringToFront()

        ' Create and show AddProduct form
        Dim addProductForm As New AddProduct()
        addProductForm.StartPosition = FormStartPosition.CenterParent

        ' Handle form closing to remove overlay
        AddHandler addProductForm.FormClosed, Sub(s, ev)
                                                  If overlayPanel IsNot Nothing AndAlso Not overlayPanel.IsDisposed Then
                                                      Me.Controls.Remove(overlayPanel)
                                                      overlayPanel.Dispose()
                                                  End If
                                              End Sub

        Dim result As DialogResult = addProductForm.ShowDialog(Me)

        ' Cleanup overlay if still exists
        If overlayPanel IsNot Nothing AndAlso Not overlayPanel.IsDisposed Then
            Me.Controls.Remove(overlayPanel)
            overlayPanel.Dispose()
        End If

        ' Refresh the inventory list if product was added
        If result = DialogResult.OK Then
            LoadProducts()
        End If
    End Sub

    Private Sub ApplyFilters(sender As Object, e As EventArgs)
        Try
            filteredProducts.Clear()

            For Each product In allProducts
                If MatchesFilter(product) Then
                    filteredProducts.Add(product)
                End If
            Next

            ' Update item count
            lblUsername.Text = $"{filteredProducts.Count} Items"

            ' Refresh display
            RefreshProductDisplay()

        Catch ex As Exception
            ' Silent fail for filter errors
        End Try
    End Sub

    Private Function MatchesFilter(product As Dictionary(Of String, Object)) As Boolean
        Try
            ' Search filter
            Dim searchText As String = txtSearch.Text.Trim().ToLower()
            If Not String.IsNullOrWhiteSpace(searchText) Then
                Dim productName As String = product("ProductName").ToString().ToLower()
                Dim productCode As String = product("ProductCode").ToString().ToLower()
                Dim category As String = product("Category").ToString().ToLower()
                Dim barcode As String = product("Barcode").ToString().ToLower()

                If Not (productName.Contains(searchText) Or productCode.Contains(searchText) Or
                       category.Contains(searchText) Or barcode.Contains(searchText)) Then
                    Return False
                End If
            End If

            ' Category filter
            If Guna2ComboBox1.SelectedItem IsNot Nothing AndAlso
               Guna2ComboBox1.SelectedItem.ToString() <> "All Categories" Then
                If product("Category").ToString() <> Guna2ComboBox1.SelectedItem.ToString() Then
                    Return False
                End If
            End If

            ' Stock status filter
            If StockCmbBox.SelectedItem IsNot Nothing Then
                Dim currentStock As Integer = Convert.ToInt32(product("CurrentStock"))
                Dim reorderLevel As Integer = Convert.ToInt32(product("ReorderLevel"))
                Dim stockFilter As String = StockCmbBox.SelectedItem.ToString()

                Select Case stockFilter
                    Case "Out of Stock"
                        If currentStock > 0 Then Return False
                    Case "Low on Stock"
                        If currentStock = 0 Or currentStock > reorderLevel Then Return False
                    Case "Active"
                        If currentStock = 0 Then Return False
                        ' "All" doesn't filter anything
                End Select
            End If

            ' Quantity filter
            If Not String.IsNullOrWhiteSpace(txtFilterQuantity.Text) Then
                Dim filterQty As Integer
                If Integer.TryParse(txtFilterQuantity.Text.Trim(), filterQty) Then
                    Dim currentStock As Integer = Convert.ToInt32(product("CurrentStock"))
                    If currentStock < filterQty Then Return False
                End If
            End If

            ' Price filter
            If Not String.IsNullOrWhiteSpace(txtFilterPrice.Text) Then
                Dim filterPrice As Decimal
                If Decimal.TryParse(txtFilterPrice.Text.Trim(), filterPrice) Then
                    Dim sellingPrice As Decimal = Convert.ToDecimal(product("SellingPrice"))
                    If sellingPrice < filterPrice Then Return False
                End If
            End If

            Return True

        Catch ex As Exception
            Return True ' Include item if filter check fails
        End Try
    End Function

    Private Sub ResetFilters_Click(sender As Object, e As EventArgs)
        ' Clear all filter inputs
        txtSearch.Text = ""
        txtFilterQuantity.Text = ""
        txtFilterPrice.Text = ""

        ' Reset dropdowns to default
        If Guna2ComboBox1.Items.Count > 0 Then
            Guna2ComboBox1.SelectedIndex = 0 ' "All Categories"
        End If

        If StockCmbBox.Items.Count > 0 Then
            StockCmbBox.SelectedIndex = 0 ' "All"
        End If

        ' Apply filters (which will show all products)
        ApplyFilters(Nothing, Nothing)
    End Sub

    Private Sub Guna2HtmlLabel13_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Close()
    End Sub

    Private Sub LoadProducts()
        ' This method is now just for refreshing after edits
        ' The main loading is handled by LoadProductsAsync
        Try
            LoadProductsFromDatabase()

            ' Initially show all products
            filteredProducts = New List(Of Dictionary(Of String, Object))(allProducts)

            ' Update item count
            lblUsername.Text = $"{filteredProducts.Count} Items"

            ' Set up virtual scrolling and render
            RefreshProductDisplay()

        Catch ex As Exception
            MessageBox.Show("Error loading products: " & ex.Message & vbCrLf & vbCrLf &
                          "Stack Trace: " & ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' FormClosing event handler with exit confirmation
    Private Sub Inventory_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
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
            DashboardPanel.FillColor = Color.White

            ' Calculate available space (DashboardPanel is 236x999)
            Dim availableWidth As Integer = DashboardPanel.Width - 40 ' 20px margins on each side
            Dim availableHeight As Integer = DashboardPanel.Height - 160 ' Space for logo and title

            ' Logo area (keep existing PictureBox9)
            PictureBox9.BringToFront()

            ' Add title label - positioned below logo with Golden Yellow
            Dim titleLabel As New Label()
            titleLabel.Text = "JADE CLINIC"
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = Color.FromArgb(254, 191, 16) ' Golden Yellow #FECF10
            titleLabel.BackColor = Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New Size(availableWidth, 30)
            titleLabel.Location = New Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(titleLabel)

            ' Subtitle with Dark Gray color (visible on white background)
            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Poppins", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = Color.FromArgb(100, 100, 100) ' Dark Gray for visibility on white
            subtitleLabel.BackColor = Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New Size(availableWidth, 25)
            subtitleLabel.Location = New Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(subtitleLabel)

            ' Navigation section separator with Light Gray (visible on white background)
            Dim separator1 As New Panel()
            separator1.BackColor = Color.FromArgb(220, 220, 220) ' Light Gray for white background
            separator1.Size = New Size(availableWidth - 20, 2)
            separator1.Location = New Point(30, 190)
            DashboardPanel.Controls.Add(separator1)

            ' Navigation section label with Dark Gray (visible on white background)
            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Poppins", 10, FontStyle.Bold)
            navLabel.ForeColor = Color.FromArgb(80, 80, 80) ' Dark Gray for visibility on white
            navLabel.BackColor = Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New Size(availableWidth, 25)
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
            ' Dashboard Button (not active - we're on Inventory)
            Dim navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
            buttonIndex += 1

            ' POS/Sales Button (all roles)
            Dim navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navPOSBtn.Click, AddressOf NavPOS_Click
            buttonIndex += 1

            ' Inventory Button (ACTIVE - we're on this page)
            Dim navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
            buttonIndex += 1

            ' Manager and Admin only buttons
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
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

            ' Add separator line before logout
            Dim separator2 As New Panel()
            separator2.BackColor = Color.FromArgb(220, 220, 220) ' Light Gray for white background
            separator2.Size = New Size(availableWidth - 40, 2)
            separator2.Location = New Point(40, startY + buttonIndex * (buttonHeight + buttonSpacing) + 10)
            DashboardPanel.Controls.Add(separator2)

            ' Logout Button (at bottom with Alert Red styling)
            Dim navLogoutBtn = CreateLargeNavButton("🚪 Logout", startY + buttonIndex * (buttonHeight + buttonSpacing) + 30, False, buttonWidth, buttonHeight)
            navLogoutBtn.FillColor = Color.FromArgb(255, 71, 87) ' Alert Red #FF4757
            navLogoutBtn.ForeColor = Color.White

            ' Override hover effects for logout button to maintain red background
            RemoveHandler navLogoutBtn.MouseEnter, Nothing
            RemoveHandler navLogoutBtn.MouseLeave, Nothing
            AddHandler navLogoutBtn.MouseEnter, Sub()
                                                    navLogoutBtn.FillColor = Color.FromArgb(220, 50, 50) ' Slightly darker red on hover
                                                    navLogoutBtn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                                End Sub
            AddHandler navLogoutBtn.MouseLeave, Sub()
                                                    navLogoutBtn.FillColor = Color.FromArgb(255, 71, 87) ' Back to original red
                                                    navLogoutBtn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                                End Sub

            AddHandler navLogoutBtn.Click, AddressOf NavLogout_Click

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
        End Try
    End Sub

    Private Function CreateLargeNavButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer) As Guna.UI2.WinForms.Guna2Button
        Dim btn As New Guna.UI2.WinForms.Guna2Button()

        ' Button properties with improved sizing and new color scheme
        btn.Text = text
        btn.Size = New Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left

        ' Apply new color scheme
        btn.FillColor = If(isActive, Color.FromArgb(254, 191, 16), Color.Transparent) ' Golden Yellow if active #FECF10
        btn.ForeColor = If(isActive, Color.FromArgb(26, 29, 31), Color.FromArgb(50, 50, 50)) ' Deep Charcoal text on active, Dark Gray text on inactive for white background
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, Color.Transparent, Color.FromArgb(200, 200, 200)) ' Light Gray border for white background
        btn.BackColor = Color.Transparent
        btn.Cursor = Cursors.Hand

        ' Add subtle shadow for depth
        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = Color.FromArgb(26, 29, 31) ' Deep Charcoal shadow
        btn.ShadowDecoration.Depth = 5
        btn.ShadowDecoration.Shadow = New Padding(0, 2, 5, 5)

        ' Improved hover effects with new color scheme
        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = Color.FromArgb(240, 240, 240) ' Light Gray hover for white background
                                           btn.BorderColor = Color.FromArgb(190, 154, 48) ' Rich Olive border #BE9A30
                                           btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                       End If
                                   End Sub

        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = Color.Transparent
                                           btn.BorderColor = Color.FromArgb(200, 200, 200) ' Light Gray border
                                           btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        ' Add to panel
        DashboardPanel.Controls.Add(btn)

        Return btn
    End Function

    ' Navigation event handlers
    Private Sub NavDashboard_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Dashboard.Show()
        Me.Close()
    End Sub

    Private Sub NavPOS_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Me.Close()
    End Sub

    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Me.Close()
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

    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        ' For now, show coming soon message
        MessageBox.Show("Audit Logs feature coming soon!", "Feature Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub NavLogout_Click(sender As Object, e As EventArgs)
        ' Confirm logout
        Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            ' Clear user session
            frmLoginvb.LogoutUser()

            ' Navigate to login
            isNavigating = True
            Me.Close()
            Dim loginForm As New frmLoginvb()
            loginForm.Show()
        End If
    End Sub

    Private Sub PictureBox9_Click(sender As Object, e As EventArgs) Handles PictureBox9.Click

    End Sub
End Class