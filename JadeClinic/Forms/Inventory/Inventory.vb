Imports Microsoft.Data.SqlClient
Imports System.Data
Imports System.IO

Public Class Inventory
    Private allProducts As New List(Of Dictionary(Of String, Object))
    Private visibleStartIndex As Integer = 0
    Private visibleItemCount As Integer = 15 ' Number of visible items
    Private itemHeight As Integer = 80 ' Height of each product panel

    Private Sub Inventory_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Enable double buffering for smooth scrolling
        SetDoubleBuffered(stockPanel)

        ' Add scroll event handler
        AddHandler stockPanel.Scroll, AddressOf StockPanel_Scroll

        ' Make form non-resizable
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        LoadProducts()
    End Sub

    Private Sub SetDoubleBuffered(ctrl As Control)
        Try
            Dim prop = ctrl.GetType().GetProperty("DoubleBuffered", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
            If prop IsNot Nothing Then prop.SetValue(ctrl, True, Nothing)
        Catch ex As Exception
            ' Silent fail for double buffering
        End Try
    End Sub

    Private Sub LoadProducts()
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

                        ' Update item count
                        lblUsername.Text = $"{allProducts.Count} Items"
                    End Using
                End Using
            End Using

            ' Set up virtual scrolling
            stockPanel.AutoScroll = True
            stockPanel.Controls.Clear()

            ' Create a spacer panel to enable scrolling
            Dim spacer As New Panel()
            spacer.Size = New Size(1, allProducts.Count * itemHeight)
            spacer.Location = New Point(0, 0)
            stockPanel.Controls.Add(spacer)

            ' Render only visible items
            RenderVisibleItems()

        Catch ex As Exception
            MessageBox.Show("Error loading products: " & ex.Message & vbCrLf & vbCrLf &
                          "Stack Trace: " & ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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
        Dim endIndex As Integer = Math.Min(allProducts.Count - 1, startIndex + visibleItemCount + 4) ' 2 item buffer below

        ' Clear existing product panels (keep the spacer)
        For i As Integer = stockPanel.Controls.Count - 1 To 0 Step -1
            If TypeOf stockPanel.Controls(i) Is Guna.UI2.WinForms.Guna2Panel Then
                stockPanel.Controls.RemoveAt(i)
            End If
        Next

        ' Render visible items
        For i As Integer = startIndex To endIndex
            If i >= 0 AndAlso i < allProducts.Count Then
                CreateProductPanel(allProducts(i), i)
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
        overlayPanel.BackColor = Color.FromArgb(100, 0, 0, 0) ' Semi-transparent black, less dark
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
        overlayPanel.BackColor = Color.FromArgb(100, 0, 0, 0) ' Semi-transparent black, less dark
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
End Class