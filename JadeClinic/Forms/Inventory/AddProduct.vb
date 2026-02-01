Imports Microsoft.Data.SqlClient
Imports System.IO
Imports System.Drawing.Imaging
Imports MessagingToolkit.Barcode
Imports MessagingToolkit.Barcode.Common

Public Class AddProduct
    Private selectedImagePath As String = ""
    Private customCategories As New List(Of String)
    Private isEditMode As Boolean = False
    Private editProductId As Integer = 0
    Private currentBarcode As String = ""

    ' Public property to set edit mode
    Public Sub SetEditMode(productId As Integer)
        isEditMode = True
        editProductId = productId
    End Sub

    Private Sub AddProduct_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadCategories()
        LoadSuppliers()
        SetupFormDefaults()
        SetupExpiryDateVisibility()
        SetupNumericInputValidation()

        ' Add close button for borderless form
        Dim btnClose As New Label()
        btnClose.Text = "✕"
        btnClose.Font = New Font("Arial", 16, FontStyle.Bold)
        btnClose.ForeColor = Color.Gray
        btnClose.Cursor = Cursors.Hand
        btnClose.Location = New Point(Me.ClientSize.Width - 40, 15)
        btnClose.Size = New Size(30, 30)
        btnClose.TextAlign = ContentAlignment.MiddleCenter
        AddHandler btnClose.Click, Sub(s, ev) Me.Close()
        AddHandler btnClose.MouseEnter, Sub(s, ev) btnClose.ForeColor = Color.Red
        AddHandler btnClose.MouseLeave, Sub(s, ev) btnClose.ForeColor = Color.Gray
        Me.Controls.Add(btnClose)
        btnClose.BringToFront()

        ' Make form topless by hiding title elements
        Guna2HtmlLabel6.Visible = False
        Guna2Panel1.Visible = False
        Guna2Panel2.Location = New Point(Guna2Panel2.Location.X, 20) ' Move up to fill space

        ' Load product data if in edit mode
        If isEditMode Then
            LoadProductData()
            ' Change title to Edit Product
            Guna2HtmlLabel6.Text = "Edit Product"
            ' Show barcode section for edit mode
            BarcodeImage.Visible = True
            PrintBarcodeTextBox.Visible = True
        Else
            ' Hide barcode section for add mode
            BarcodeImage.Visible = False
            PrintBarcodeTextBox.Visible = False
        End If
    End Sub

    Private Sub SetupFormDefaults()
        ' Set default values
        cmbCategory.SelectedIndex = -1
        SupplierCMbBox.SelectedIndex = -1
        Guna2DateTimePicker1.Value = DateTime.Now.AddMonths(12) ' Default expiry 1 year from now
        Guna2DateTimePicker1.Visible = False
        Guna2HtmlLabel8.Visible = False

        ' Setup Unit dropdown
        UnitCmbBox.Items.Clear()
        UnitCmbBox.Items.AddRange(New String() {"PCS", "BOX", "PACK", "BOTTLE", "TUBE", "SET", "PAIR", "DOZEN", "REAM"})
        UnitCmbBox.SelectedItem = "PCS" ' Default to PCS

        ' Add "Add Custom" option to category combo
        cmbCategory.Items.Add("Add Custom Category...")

        ' Set placeholder text for numeric fields
        CostPriceTextBox.PlaceholderText = "0.00"
        SellingPriceTextBox.PlaceholderText = "0.00"
        WholeSaleTextbox.PlaceholderText = "0.00"
        ReOrderLevelTextBox.PlaceholderText = "0"
    End Sub

    Private Sub LoadCategories()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                ' Get distinct categories from existing products
                Dim query As String = "SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND IsActive = 1 ORDER BY Category"
                Using cmd As New SqlCommand(query, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        cmbCategory.Items.Clear()
                        While reader.Read()
                            If Not IsDBNull(reader("Category")) Then
                                cmbCategory.Items.Add(reader("Category").ToString())
                            End If
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading categories: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadSuppliers()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                Dim query As String = "SELECT SupplierID, SupplierName FROM Suppliers WHERE IsActive = 1 ORDER BY SupplierName"
                Using cmd As New SqlCommand(query, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        SupplierCMbBox.Items.Clear()

                        ' Add "New Supplier" option at the top
                        SupplierCMbBox.Items.Add("+ Add New Supplier")

                        ' Create a dictionary to store supplier data
                        Dim supplierData As New Dictionary(Of String, Integer)

                        While reader.Read()
                            Dim supplierName As String = reader("SupplierName").ToString()
                            Dim supplierId As Integer = Convert.ToInt32(reader("SupplierID"))

                            SupplierCMbBox.Items.Add(supplierName)
                            supplierData.Add(supplierName, supplierId)
                        End While

                        ' Store supplier data for later use
                        SupplierCMbBox.Tag = supplierData
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading suppliers: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetupExpiryDateVisibility()
        ' Initially hide expiry date controls - only show for ENDO type categories
        Guna2DateTimePicker1.Visible = False
        Guna2HtmlLabel8.Visible = False
    End Sub

    Private Sub cmbCategory_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCategory.SelectedIndexChanged
        If cmbCategory.SelectedItem IsNot Nothing Then
            Dim selectedCategory As String = cmbCategory.SelectedItem.ToString()

            If selectedCategory = "Add Custom Category..." Then
                ' Show dialog to add custom category
                AddCustomCategory()
            Else
                ' Check if selected category is ENDO type (contains "endo" case insensitive)
                Dim isEndo As Boolean = selectedCategory.ToLower().Contains("endo")

                ' Show/hide expiry date based on category
                Guna2DateTimePicker1.Visible = isEndo
                Guna2HtmlLabel8.Visible = isEndo

                If isEndo Then
                    Guna2HtmlLabel8.Text = "Expiry Date *"
                    Guna2HtmlLabel8.ForeColor = Color.Red ' Indicate required
                End If
            End If
        End If
    End Sub

    Private Sub AddCustomCategory()
        Dim customCategory As String = InputBox("Enter new category name:", "Add Custom Category")

        If Not String.IsNullOrWhiteSpace(customCategory) Then
            ' Check if category already exists
            If Not cmbCategory.Items.Contains(customCategory) Then
                cmbCategory.Items.Insert(cmbCategory.Items.Count - 1, customCategory) ' Insert before "Add Custom..."
                cmbCategory.SelectedItem = customCategory
                customCategories.Add(customCategory)
            Else
                MessageBox.Show("Category already exists!", "Duplicate Category", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                cmbCategory.SelectedItem = customCategory
            End If
        Else
            cmbCategory.SelectedIndex = -1
        End If
    End Sub

    Private Sub lblProductPicturetrigger_Click(sender As Object, e As EventArgs) Handles lblProductPicturetrigger.Click
        Using openFileDialog As New OpenFileDialog()
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp"
            openFileDialog.Title = "Select Product Image"

            If openFileDialog.ShowDialog() = DialogResult.OK Then
                selectedImagePath = openFileDialog.FileName
                ProductImage.Image = Image.FromFile(selectedImagePath)
            End If
        End Using
    End Sub

    Private Sub btnAddStock_Click(sender As Object, e As EventArgs) Handles btnAddStock.Click
        If ValidateForm() Then
            If isEditMode Then
                If UpdateProduct() Then
                    MessageBox.Show("Product updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                End If
            Else
                If SaveProduct() Then
                    MessageBox.Show("Product added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    ClearForm()
                    Me.DialogResult = DialogResult.OK
                End If
            End If
        End If
    End Sub

    Private Function ValidateForm() As Boolean
        ' Validate required fields
        If String.IsNullOrWhiteSpace(txtProductName.Text) Then
            MessageBox.Show("Product name is required!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtProductName.Focus()
            Return False
        End If

        If cmbCategory.SelectedItem Is Nothing OrElse cmbCategory.SelectedItem.ToString() = "Add Custom Category..." Then
            MessageBox.Show("Please select a valid category!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            cmbCategory.Focus()
            Return False
        End If

        ' Validate reorder level
        Dim reorderLevel As Integer
        If Not String.IsNullOrWhiteSpace(ReOrderLevelTextBox.Text) AndAlso (Not Integer.TryParse(ReOrderLevelTextBox.Text.Trim(), reorderLevel) OrElse reorderLevel < 0) Then
            MessageBox.Show("Reorder level must be a valid non-negative number!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ReOrderLevelTextBox.Focus()
            Return False
        End If

        ' Validate cost price
        Dim costPrice As Decimal
        If String.IsNullOrWhiteSpace(CostPriceTextBox.Text) OrElse Not Decimal.TryParse(CostPriceTextBox.Text.Trim(), costPrice) OrElse costPrice <= 0 Then
            MessageBox.Show("Valid cost price (greater than 0) is required!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            CostPriceTextBox.Focus()
            Return False
        End If

        ' Validate selling price
        Dim sellingPrice As Decimal
        If String.IsNullOrWhiteSpace(SellingPriceTextBox.Text) OrElse Not Decimal.TryParse(SellingPriceTextBox.Text.Trim(), sellingPrice) OrElse sellingPrice <= 0 Then
            MessageBox.Show("Valid selling price (greater than 0) is required!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            SellingPriceTextBox.Focus()
            Return False
        End If

        ' Validate wholesale price if provided
        Dim wholesalePrice As Decimal
        If Not String.IsNullOrWhiteSpace(WholeSaleTextbox.Text) AndAlso (Not Decimal.TryParse(WholeSaleTextbox.Text.Trim(), wholesalePrice) OrElse wholesalePrice <= 0) Then
            MessageBox.Show("Wholesale price must be a valid number greater than 0!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            WholeSaleTextbox.Focus()
            Return False
        End If

        ' Check if endo category and expiry date is required
        Dim selectedCategory As String = cmbCategory.SelectedItem.ToString()
        Dim isEndo As Boolean = selectedCategory.ToLower().Contains("endo")

        If isEndo AndAlso Guna2DateTimePicker1.Value.Date <= DateTime.Now.Date Then
            MessageBox.Show("Expiry date must be in the future for endo products!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Guna2DateTimePicker1.Focus()
            Return False
        End If

        Return True
    End Function

    Private Function SaveProduct() As Boolean
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using transaction As SqlTransaction = conn.BeginTransaction()
                    Try
                        ' Prepare product data
                        Dim selectedCategory As String = cmbCategory.SelectedItem.ToString()
                        Dim isEndo As Boolean = selectedCategory.ToLower().Contains("endo")
                        Dim expiryDate As Date? = If(isEndo, Guna2DateTimePicker1.Value.Date, Nothing)

                        ' Insert product with temporary product code
                        Dim insertQuery As String = "INSERT INTO Products (ProductCode, Barcode, ProductName, Category, Unit, " &
                                                   "CurrentStock, ReorderLevel, CostPrice, SellingPrice, WholesalePrice, " &
                                                   "HasExpiry, ExpiryDate, SupplierID, IsActive, Created, UpdatedAt) " &
                                                   "VALUES (@ProductCode, @Barcode, @ProductName, @Category, @Unit, " &
                                                   "@CurrentStock, @ReorderLevel, @CostPrice, @SellingPrice, @WholesalePrice, " &
                                                   "@HasExpiry, @ExpiryDate, @SupplierID, 1, GETDATE(), GETDATE()); SELECT SCOPE_IDENTITY()"

                        Dim productId As Integer

                        Using cmd As New SqlCommand(insertQuery, conn, transaction)
                            cmd.Parameters.AddWithValue("@ProductCode", "TEMP_CODE")
                            cmd.Parameters.AddWithValue("@Barcode", "TEMP_BARCODE")
                            cmd.Parameters.AddWithValue("@ProductName", txtProductName.Text.Trim())
                            cmd.Parameters.AddWithValue("@Category", selectedCategory)
                            cmd.Parameters.AddWithValue("@Unit", If(UnitCmbBox.SelectedItem IsNot Nothing, UnitCmbBox.SelectedItem.ToString(), "PCS"))
                            cmd.Parameters.AddWithValue("@CurrentStock", 0)
                            cmd.Parameters.AddWithValue("@ReorderLevel", If(String.IsNullOrWhiteSpace(ReOrderLevelTextBox.Text), 10, Convert.ToInt32(ReOrderLevelTextBox.Text.Trim())))
                            cmd.Parameters.AddWithValue("@CostPrice", Convert.ToDecimal(CostPriceTextBox.Text.Trim()))
                            cmd.Parameters.AddWithValue("@SellingPrice", Convert.ToDecimal(SellingPriceTextBox.Text.Trim()))
                            cmd.Parameters.AddWithValue("@WholesalePrice", If(String.IsNullOrWhiteSpace(WholeSaleTextbox.Text), DBNull.Value, Convert.ToDecimal(WholeSaleTextbox.Text.Trim())))
                            cmd.Parameters.AddWithValue("@HasExpiry", isEndo)
                            If isEndo Then
                                cmd.Parameters.AddWithValue("@ExpiryDate", Guna2DateTimePicker1.Value.Date)
                            Else
                                cmd.Parameters.AddWithValue("@ExpiryDate", DBNull.Value)
                            End If

                            ' Get selected supplier ID
                            Dim supplierId As Object = DBNull.Value
                            If SupplierCMbBox.SelectedItem IsNot Nothing AndAlso SupplierCMbBox.SelectedItem.ToString() <> "+ Add New Supplier" Then
                                Dim supplierData As Dictionary(Of String, Integer) = TryCast(SupplierCMbBox.Tag, Dictionary(Of String, Integer))
                                If supplierData IsNot Nothing AndAlso supplierData.ContainsKey(SupplierCMbBox.SelectedItem.ToString()) Then
                                    supplierId = supplierData(SupplierCMbBox.SelectedItem.ToString())
                                End If
                            End If
                            cmd.Parameters.AddWithValue("@SupplierID", supplierId)

                            productId = Convert.ToInt32(cmd.ExecuteScalar())
                        End Using

                        ' Update with final ProductCode and simple Barcode
                        Dim finalProductCode As String = GenerateFinalProductCode(productId)
                        Dim simpleBarcode As String = $"P{productId.ToString("D8")}" ' Simple barcode for new products

                        Dim updateQuery As String = "UPDATE Products SET ProductCode = @ProductCode, Barcode = @Barcode WHERE ProductID = @ProductID"
                        Using cmdUpdate As New SqlCommand(updateQuery, conn, transaction)
                            cmdUpdate.Parameters.AddWithValue("@ProductCode", finalProductCode)
                            cmdUpdate.Parameters.AddWithValue("@Barcode", simpleBarcode)
                            cmdUpdate.Parameters.AddWithValue("@ProductID", productId)
                            cmdUpdate.ExecuteNonQuery()
                        End Using

                        ' Save product image if selected
                        If Not String.IsNullOrWhiteSpace(selectedImagePath) AndAlso IO.File.Exists(selectedImagePath) Then
                            SaveProductImage(conn, transaction, productId, selectedImagePath)
                        End If

                        transaction.Commit()
                        Return True

                    Catch ex As Exception
                        transaction.Rollback()
                        Throw ex
                    End Try
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error saving product: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Private Function UpdateProduct() As Boolean
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using transaction As SqlTransaction = conn.BeginTransaction()
                    Try
                        ' Prepare product data
                        Dim selectedCategory As String = cmbCategory.SelectedItem.ToString()
                        Dim isEndo As Boolean = selectedCategory.ToLower().Contains("endo")
                        Dim expiryDate As Date? = If(isEndo, Guna2DateTimePicker1.Value.Date, Nothing)

                        ' Generate new barcode for edited product
                        Dim newBarcode As String = GenerateBarcode(editProductId)

                        ' Update product
                        Dim updateQuery As String = "UPDATE Products SET Barcode = @Barcode, ProductName = @ProductName, Category = @Category, Unit = @Unit, " &
                                                   "ReorderLevel = @ReorderLevel, CostPrice = @CostPrice, SellingPrice = @SellingPrice, " &
                                                   "WholesalePrice = @WholesalePrice, HasExpiry = @HasExpiry, ExpiryDate = @ExpiryDate, " &
                                                   "SupplierID = @SupplierID, UpdatedAt = GETDATE() WHERE ProductID = @ProductID"

                        Using cmd As New SqlCommand(updateQuery, conn, transaction)
                            cmd.Parameters.AddWithValue("@Barcode", newBarcode)
                            cmd.Parameters.AddWithValue("@ProductName", txtProductName.Text.Trim())
                            cmd.Parameters.AddWithValue("@Category", selectedCategory)
                            cmd.Parameters.AddWithValue("@Unit", If(UnitCmbBox.SelectedItem IsNot Nothing, UnitCmbBox.SelectedItem.ToString(), "PCS"))
                            cmd.Parameters.AddWithValue("@ReorderLevel", If(String.IsNullOrWhiteSpace(ReOrderLevelTextBox.Text), 10, Convert.ToInt32(ReOrderLevelTextBox.Text.Trim())))
                            cmd.Parameters.AddWithValue("@CostPrice", Convert.ToDecimal(CostPriceTextBox.Text.Trim()))
                            cmd.Parameters.AddWithValue("@SellingPrice", Convert.ToDecimal(SellingPriceTextBox.Text.Trim()))
                            cmd.Parameters.AddWithValue("@WholesalePrice", If(String.IsNullOrWhiteSpace(WholeSaleTextbox.Text), DBNull.Value, Convert.ToDecimal(WholeSaleTextbox.Text.Trim())))
                            cmd.Parameters.AddWithValue("@HasExpiry", isEndo)
                            If isEndo Then
                                cmd.Parameters.AddWithValue("@ExpiryDate", Guna2DateTimePicker1.Value.Date)
                            Else
                                cmd.Parameters.AddWithValue("@ExpiryDate", DBNull.Value)
                            End If

                            ' Get selected supplier ID
                            Dim supplierId As Object = DBNull.Value
                            If SupplierCMbBox.SelectedItem IsNot Nothing AndAlso SupplierCMbBox.SelectedItem.ToString() <> "+ Add New Supplier" Then
                                Dim supplierData As Dictionary(Of String, Integer) = TryCast(SupplierCMbBox.Tag, Dictionary(Of String, Integer))
                                If supplierData IsNot Nothing AndAlso supplierData.ContainsKey(SupplierCMbBox.SelectedItem.ToString()) Then
                                    supplierId = supplierData(SupplierCMbBox.SelectedItem.ToString())
                                End If
                            End If
                            cmd.Parameters.AddWithValue("@SupplierID", supplierId)
                            cmd.Parameters.AddWithValue("@ProductID", editProductId)

                            cmd.ExecuteNonQuery()
                        End Using

                        ' Update product image if new one selected
                        If Not String.IsNullOrWhiteSpace(selectedImagePath) AndAlso IO.File.Exists(selectedImagePath) Then
                            ' Delete old image
                            Dim deleteImageQuery As String = "DELETE FROM ProductImages WHERE ProductID = @ProductID"
                            Using cmdDelete As New SqlCommand(deleteImageQuery, conn, transaction)
                                cmdDelete.Parameters.AddWithValue("@ProductID", editProductId)
                                cmdDelete.ExecuteNonQuery()
                            End Using

                            ' Save new image
                            SaveProductImage(conn, transaction, editProductId, selectedImagePath)
                        End If

                        transaction.Commit()

                        ' Generate and display barcode after successful update
                        GenerateAndDisplayBarcode(newBarcode)

                        Return True

                    Catch ex As Exception
                        transaction.Rollback()
                        Throw ex
                    End Try
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error updating product: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Private Function GenerateProductCode(conn As SqlConnection, transaction As SqlTransaction) As String
        ' Generate unique product code using ProductID + DateTime format: P[ID]-YYYYMMDD-HHMMSS
        ' This will be finalized after getting the ProductID from the database
        Return "TEMP_CODE"
    End Function

    Private Function GenerateFinalProductCode(productId As Integer) As String
        ' Generate unique product code: P[ID]-YYYYMMDD-HHmmss
        Dim dateTimeStr As String = DateTime.Now.ToString("yyyyMMdd-HHmmss")
        Return $"P{productId.ToString("D5")}-{dateTimeStr}"
    End Function

    Private Function GenerateBarcode(productId As Integer) As String
        ' Generate barcode: format as PXXXXYYYYMMDDHHMMSS where XXXX is product ID
        Dim dateTimeStr As String = DateTime.Now.ToString("yyyyMMddHHmmss")
        Return $"P{productId.ToString("D4")}{dateTimeStr}"
    End Function

    Private Sub SaveProductImage(conn As SqlConnection, transaction As SqlTransaction, productId As Integer, imagePath As String)
        Try
            ' Read image file
            Dim imageBytes As Byte() = IO.File.ReadAllBytes(imagePath)

            ' Insert into ProductImages table
            Dim query As String = "INSERT INTO ProductImages (ProductID, ImageType, ImageData, CreatedAt, UpdatedAt) " &
                                 "VALUES (@ProductID, 'thumb', @ImageData, GETDATE(), GETDATE())"

            Using cmd As New SqlCommand(query, conn, transaction)
                cmd.Parameters.AddWithValue("@ProductID", productId)
                cmd.Parameters.AddWithValue("@ImageData", imageBytes)
                cmd.ExecuteNonQuery()
            End Using
        Catch ex As Exception
            ' Log error but don't fail the entire transaction
            Console.WriteLine("Error saving product image: " & ex.Message)
        End Try
    End Sub

    Private Sub LoadProductData()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                Dim query As String = "SELECT p.*, " &
                                     "(SELECT TOP 1 ImageData FROM ProductImages WHERE ProductID = p.ProductID) AS ProductImage " &
                                     "FROM Products p WHERE p.ProductID = @ProductID"

                Using cmd As New SqlCommand(query, conn)
                    cmd.Parameters.AddWithValue("@ProductID", editProductId)

                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            ' Populate form fields
                            txtProductName.Text = reader("ProductName").ToString()

                            ' Set category
                            Dim category As String = reader("Category").ToString()
                            If cmbCategory.Items.Contains(category) Then
                                cmbCategory.SelectedItem = category
                            End If

                            ' Set unit
                            Dim unit As String = reader("Unit").ToString()
                            If UnitCmbBox.Items.Contains(unit) Then
                                UnitCmbBox.SelectedItem = unit
                            End If

                            ' Set prices
                            If Not IsDBNull(reader("CostPrice")) Then
                                CostPriceTextBox.Text = Convert.ToDecimal(reader("CostPrice")).ToString("0.00")
                            End If

                            If Not IsDBNull(reader("SellingPrice")) Then
                                SellingPriceTextBox.Text = Convert.ToDecimal(reader("SellingPrice")).ToString("0.00")
                            End If

                            If Not IsDBNull(reader("WholesalePrice")) Then
                                WholeSaleTextbox.Text = Convert.ToDecimal(reader("WholesalePrice")).ToString("0.00")
                            End If

                            ' Set reorder level
                            If Not IsDBNull(reader("ReorderLevel")) Then
                                ReOrderLevelTextBox.Text = reader("ReorderLevel").ToString()
                            End If

                            ' Set supplier
                            If Not IsDBNull(reader("SupplierID")) Then
                                Dim supplierId As Integer = Convert.ToInt32(reader("SupplierID"))
                                Dim supplierData As Dictionary(Of String, Integer) = TryCast(SupplierCMbBox.Tag, Dictionary(Of String, Integer))
                                If supplierData IsNot Nothing Then
                                    For Each kvp In supplierData
                                        If kvp.Value = supplierId Then
                                            SupplierCMbBox.SelectedItem = kvp.Key
                                            Exit For
                                        End If
                                    Next
                                End If
                            End If

                            ' Load expiry date if applicable
                            If Not IsDBNull(reader("HasExpiry")) AndAlso Convert.ToBoolean(reader("HasExpiry")) Then
                                If Not IsDBNull(reader("ExpiryDate")) Then
                                    Guna2DateTimePicker1.Value = Convert.ToDateTime(reader("ExpiryDate"))
                                    Guna2DateTimePicker1.Visible = True
                                    Guna2HtmlLabel8.Visible = True
                                End If
                            End If

                            ' Load product image
                            If Not IsDBNull(reader("ProductImage")) Then
                                Dim imgBytes As Byte() = CType(reader("ProductImage"), Byte())
                                Using ms As New MemoryStream(imgBytes)
                                    ProductImage.Image = Image.FromStream(ms)
                                End Using
                            End If

                            ' Load and display barcode
                            If Not IsDBNull(reader("Barcode")) Then
                                Dim barcode As String = reader("Barcode").ToString()
                                GenerateAndDisplayBarcode(barcode)
                            End If
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading product data: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ClearForm()
        txtProductName.Clear()
        cmbCategory.SelectedIndex = -1
        SupplierCMbBox.SelectedIndex = -1
        CostPriceTextBox.Clear()
        SellingPriceTextBox.Clear()
        WholeSaleTextbox.Clear()
        ReOrderLevelTextBox.Clear()
        ProductImage.Image = Nothing
        selectedImagePath = ""
        Guna2DateTimePicker1.Value = DateTime.Now.AddMonths(12)
        SetupExpiryDateVisibility()
    End Sub

    Private Sub Guna2HtmlLabel1_Click(sender As Object, e As EventArgs) Handles Guna2HtmlLabel1.Click
        ' Cancel button
        Me.Close()
    End Sub

    Private Function IsNumeric(text As String) As Boolean
        Dim dummy As Double
        Return Double.TryParse(text, dummy)
    End Function

    Private Sub SupplierCMbBox_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SupplierCMbBox.SelectedIndexChanged
        If SupplierCMbBox.SelectedItem IsNot Nothing Then
            If SupplierCMbBox.SelectedItem.ToString() = "+ Add New Supplier" Then
                AddNewSupplier()
            End If
        End If
    End Sub

    Private Sub SetupNumericInputValidation()
        ' Add numeric input validation for price fields and reorder level
        AddHandler CostPriceTextBox.KeyPress, AddressOf NumericTextBox_KeyPress
        AddHandler SellingPriceTextBox.KeyPress, AddressOf NumericTextBox_KeyPress
        AddHandler WholeSaleTextbox.KeyPress, AddressOf NumericTextBox_KeyPress
        AddHandler ReOrderLevelTextBox.KeyPress, AddressOf IntegerTextBox_KeyPress
    End Sub

    Private Sub NumericTextBox_KeyPress(sender As Object, e As KeyPressEventArgs)
        ' Allow digits, decimal point, and backspace for decimal numbers
        If Not Char.IsDigit(e.KeyChar) AndAlso Not e.KeyChar = "." AndAlso Not e.KeyChar = ChrW(Keys.Back) Then
            e.Handled = True
        End If

        ' Allow only one decimal point
        If e.KeyChar = "." AndAlso DirectCast(sender, TextBox).Text.Contains(".") Then
            e.Handled = True
        End If
    End Sub

    Private Sub IntegerTextBox_KeyPress(sender As Object, e As KeyPressEventArgs)
        ' Allow only digits and backspace for integers
        If Not Char.IsDigit(e.KeyChar) AndAlso Not e.KeyChar = ChrW(Keys.Back) Then
            e.Handled = True
        End If
    End Sub

    Private Sub GenerateAndDisplayBarcode(barcodeText As String)
        Try
            ' Create barcode encoder
            Dim encoder As New BarcodeEncoder()

            ' Generate barcode image
            Dim barcodeImg As Bitmap = encoder.Encode(BarcodeFormat.Code128, barcodeText)

            ' Display in picture box
            BarcodeImage.Image = barcodeImg

            ' Store current barcode
            currentBarcode = barcodeText

        Catch ex As Exception
            MessageBox.Show("Error generating barcode: " & ex.Message, "Barcode Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PrintBarcodeTextBox_Click(sender As Object, e As EventArgs) Handles PrintBarcodeTextBox.Click
        If String.IsNullOrWhiteSpace(currentBarcode) Then
            MessageBox.Show("No barcode to print. Please save the product first.", "Print Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            ' Create print dialog
            Using printDialog As New PrintDialog()
                If printDialog.ShowDialog() = DialogResult.OK Then
                    ' Create print document
                    Dim printDoc As New Printing.PrintDocument()
                    printDoc.PrinterSettings = printDialog.PrinterSettings

                    AddHandler printDoc.PrintPage, Sub(s, ev)
                                                       ' Print the barcode image
                                                       If BarcodeImage.Image IsNot Nothing Then
                                                           ' Center the barcode on the page
                                                           Dim x As Integer = (ev.PageBounds.Width - BarcodeImage.Image.Width) \ 2
                                                           Dim y As Integer = 100
                                                           ev.Graphics.DrawImage(BarcodeImage.Image, x, y)

                                                           ' Print barcode text below
                                                           Dim font As New Font("Arial", 12, FontStyle.Bold)
                                                           Dim textSize As SizeF = ev.Graphics.MeasureString(currentBarcode, font)
                                                           Dim textX As Single = (ev.PageBounds.Width - textSize.Width) / 2
                                                           Dim textY As Single = y + BarcodeImage.Image.Height + 20
                                                           ev.Graphics.DrawString(currentBarcode, font, Brushes.Black, textX, textY)

                                                           ' Print product name
                                                           Dim nameFont As New Font("Arial", 10, FontStyle.Regular)
                                                           Dim nameSize As SizeF = ev.Graphics.MeasureString(txtProductName.Text, nameFont)
                                                           Dim nameX As Single = (ev.PageBounds.Width - nameSize.Width) / 2
                                                           Dim nameY As Single = textY + 30
                                                           ev.Graphics.DrawString(txtProductName.Text, nameFont, Brushes.Black, nameX, nameY)
                                                       End If
                                                   End Sub

                    printDoc.Print()
                    MessageBox.Show("Barcode sent to printer successfully!", "Print Complete", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("Error printing barcode: " & ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AddNewSupplier()
        ' Create overlay panel for modal effect
        Dim overlayPanel As New Panel()
        overlayPanel.BackColor = Color.FromArgb(150, 0, 0, 0)
        overlayPanel.Dock = DockStyle.Fill
        overlayPanel.Location = New Point(0, 0)
        overlayPanel.Size = Me.ClientSize
        Me.Controls.Add(overlayPanel)
        overlayPanel.BringToFront()

        ' Create supplier form
        Dim supplierForm As New Form()
        supplierForm.Text = ""
        supplierForm.Size = New Size(500, 450)
        supplierForm.StartPosition = FormStartPosition.CenterParent
        supplierForm.FormBorderStyle = FormBorderStyle.None
        supplierForm.BackColor = Color.FromArgb(30, 30, 30)
        supplierForm.ShowInTaskbar = False

        ' Add rounded corners
        Dim path As New System.Drawing.Drawing2D.GraphicsPath()
        path.AddArc(0, 0, 20, 20, 180, 90)
        path.AddArc(supplierForm.Width - 20, 0, 20, 20, 270, 90)
        path.AddArc(supplierForm.Width - 20, supplierForm.Height - 20, 20, 20, 0, 90)
        path.AddArc(0, supplierForm.Height - 20, 20, 20, 90, 90)
        path.CloseAllFigures()
        supplierForm.Region = New Region(path)

        ' Add border panel
        Dim borderPanel As New Panel()
        borderPanel.BackColor = Color.FromArgb(61, 65, 66)
        borderPanel.Dock = DockStyle.Fill
        borderPanel.Padding = New Padding(2)
        supplierForm.Controls.Add(borderPanel)

        ' Inner panel
        Dim contentPanel As New Panel()
        contentPanel.BackColor = Color.FromArgb(30, 30, 30)
        contentPanel.Dock = DockStyle.Fill
        borderPanel.Controls.Add(contentPanel)

        ' Title panel
        Dim titlePanel As New Panel()
        titlePanel.BackColor = Color.FromArgb(40, 40, 40)
        titlePanel.Dock = DockStyle.Top
        titlePanel.Height = 60
        contentPanel.Controls.Add(titlePanel)

        Dim lblTitle As New Label()
        lblTitle.Text = "Add New Supplier"
        lblTitle.Font = New Font("Poppins SemiBold", 14, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.Location = New Point(20, 15)
        lblTitle.AutoSize = True
        titlePanel.Controls.Add(lblTitle)

        ' Close button
        Dim btnClose As New Label()
        btnClose.Text = "✕"
        btnClose.Font = New Font("Arial", 16, FontStyle.Bold)
        btnClose.ForeColor = Color.Gray
        btnClose.Cursor = Cursors.Hand
        btnClose.Location = New Point(460, 15)
        btnClose.Size = New Size(30, 30)
        btnClose.TextAlign = ContentAlignment.MiddleCenter
        AddHandler btnClose.Click, Sub(s, ev)
                                       overlayPanel.Dispose()
                                       supplierForm.Close()
                                   End Sub
        AddHandler btnClose.MouseEnter, Sub(s, ev) btnClose.ForeColor = Color.Red
        AddHandler btnClose.MouseLeave, Sub(s, ev) btnClose.ForeColor = Color.Gray
        titlePanel.Controls.Add(btnClose)

        ' Main panel
        Dim mainPanel As New Panel()
        mainPanel.Location = New Point(0, 60)
        mainPanel.Size = New Size(500, 390)
        mainPanel.BackColor = Color.FromArgb(30, 30, 30)
        mainPanel.AutoScroll = True
        contentPanel.Controls.Add(mainPanel)

        ' Supplier Name
        Dim lblName As New Label()
        lblName.Text = "Supplier Name *"
        lblName.Font = New Font("Poppins", 10)
        lblName.ForeColor = Color.White
        lblName.Location = New Point(30, 20)
        lblName.AutoSize = True
        mainPanel.Controls.Add(lblName)

        Dim txtName As New TextBox()
        txtName.Font = New Font("Poppins", 10)
        txtName.Location = New Point(30, 50)
        txtName.Size = New Size(430, 35)
        txtName.BackColor = Color.FromArgb(45, 45, 45)
        txtName.ForeColor = Color.White
        txtName.BorderStyle = BorderStyle.FixedSingle
        mainPanel.Controls.Add(txtName)

        ' Contact Person
        Dim lblContact As New Label()
        lblContact.Text = "Contact Person"
        lblContact.Font = New Font("Poppins", 10)
        lblContact.ForeColor = Color.White
        lblContact.Location = New Point(30, 100)
        lblContact.AutoSize = True
        mainPanel.Controls.Add(lblContact)

        Dim txtContact As New TextBox()
        txtContact.Font = New Font("Poppins", 10)
        txtContact.Location = New Point(30, 130)
        txtContact.Size = New Size(430, 35)
        txtContact.BackColor = Color.FromArgb(45, 45, 45)
        txtContact.ForeColor = Color.White
        txtContact.BorderStyle = BorderStyle.FixedSingle
        mainPanel.Controls.Add(txtContact)

        ' Phone
        Dim lblPhone As New Label()
        lblPhone.Text = "Phone"
        lblPhone.Font = New Font("Poppins", 10)
        lblPhone.ForeColor = Color.White
        lblPhone.Location = New Point(30, 180)
        lblPhone.AutoSize = True
        mainPanel.Controls.Add(lblPhone)

        Dim txtPhone As New TextBox()
        txtPhone.Font = New Font("Poppins", 10)
        txtPhone.Location = New Point(30, 210)
        txtPhone.Size = New Size(430, 35)
        txtPhone.BackColor = Color.FromArgb(45, 45, 45)
        txtPhone.ForeColor = Color.White
        txtPhone.BorderStyle = BorderStyle.FixedSingle
        mainPanel.Controls.Add(txtPhone)

        ' Email
        Dim lblEmail As New Label()
        lblEmail.Text = "Email"
        lblEmail.Font = New Font("Poppins", 10)
        lblEmail.ForeColor = Color.White
        lblEmail.Location = New Point(30, 260)
        lblEmail.AutoSize = True
        mainPanel.Controls.Add(lblEmail)

        Dim txtEmail As New TextBox()
        txtEmail.Font = New Font("Poppins", 10)
        txtEmail.Location = New Point(30, 290)
        txtEmail.Size = New Size(430, 35)
        txtEmail.BackColor = Color.FromArgb(45, 45, 45)
        txtEmail.ForeColor = Color.White
        txtEmail.BorderStyle = BorderStyle.FixedSingle
        mainPanel.Controls.Add(txtEmail)

        ' Button Panel
        Dim buttonPanel As New Panel()
        buttonPanel.Location = New Point(30, 340)
        buttonPanel.Size = New Size(430, 40)
        buttonPanel.BackColor = Color.Transparent
        mainPanel.Controls.Add(buttonPanel)

        ' Save Button
        Dim btnSave As New Button()
        btnSave.Text = "Save Supplier"
        btnSave.Font = New Font("Poppins SemiBold", 10, FontStyle.Bold)
        btnSave.Location = New Point(300, 0)
        btnSave.Size = New Size(130, 40)
        btnSave.BackColor = Color.White
        btnSave.ForeColor = Color.Black
        btnSave.FlatStyle = FlatStyle.Flat
        btnSave.FlatAppearance.BorderSize = 0
        btnSave.Cursor = Cursors.Hand
        AddHandler btnSave.Click, Sub(s, ev)
                                      If String.IsNullOrWhiteSpace(txtName.Text) Then
                                          MessageBox.Show("Supplier name is required!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                          txtName.Focus()
                                          Return
                                      End If

                                      If CheckSupplierExists(txtName.Text.Trim()) Then
                                          MessageBox.Show("A supplier with this name already exists!", "Duplicate Supplier", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                          txtName.Focus()
                                          Return
                                      End If

                                      If SaveSupplier(txtName.Text.Trim(), txtContact.Text.Trim(), txtPhone.Text.Trim(), txtEmail.Text.Trim()) Then
                                          MessageBox.Show("Supplier added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                          LoadSuppliers()
                                          SupplierCMbBox.SelectedItem = txtName.Text.Trim()
                                          overlayPanel.Dispose()
                                          supplierForm.Close()
                                      Else
                                          MessageBox.Show("Failed to add supplier. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                      End If
                                  End Sub
        buttonPanel.Controls.Add(btnSave)

        ' Cancel Button
        Dim btnCancel As New Button()
        btnCancel.Text = "Cancel"
        btnCancel.Font = New Font("Poppins", 10)
        btnCancel.Location = New Point(160, 0)
        btnCancel.Size = New Size(130, 40)
        btnCancel.BackColor = Color.FromArgb(60, 60, 60)
        btnCancel.ForeColor = Color.White
        btnCancel.FlatStyle = FlatStyle.Flat
        btnCancel.FlatAppearance.BorderSize = 0
        btnCancel.Cursor = Cursors.Hand
        AddHandler btnCancel.Click, Sub(s, ev)
                                        SupplierCMbBox.SelectedIndex = -1
                                        overlayPanel.Dispose()
                                        supplierForm.Close()
                                    End Sub
        buttonPanel.Controls.Add(btnCancel)

        ' Handle form closing
        AddHandler supplierForm.FormClosed, Sub(s, ev)
                                                If overlayPanel IsNot Nothing AndAlso Not overlayPanel.IsDisposed Then
                                                    Me.Controls.Remove(overlayPanel)
                                                    overlayPanel.Dispose()
                                                End If
                                            End Sub

        supplierForm.ShowDialog(Me)
    End Sub

    Private Function CheckSupplierExists(supplierName As String) As Boolean
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Dim query As String = "SELECT COUNT(*) FROM Suppliers WHERE LOWER(SupplierName) = LOWER(@SupplierName) AND IsActive = 1"
                Using cmd As New SqlCommand(query, conn)
                    cmd.Parameters.AddWithValue("@SupplierName", supplierName)
                    Dim count As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                    Return count > 0
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error checking supplier: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Private Function SaveSupplier(supplierName As String, contactPerson As String, phone As String, email As String) As Boolean
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                ' Generate unique supplier code
                Dim supplierCode As String = GenerateSupplierCode(conn)

                Dim query As String = "INSERT INTO Suppliers (SupplierCode, SupplierName, ContactPerson, Phone, Email, IsActive) " +
                                     "VALUES (@SupplierCode, @SupplierName, @ContactPerson, @Phone, @Email, 1)"
                Using cmd As New SqlCommand(query, conn)
                    cmd.Parameters.AddWithValue("@SupplierCode", supplierCode)
                    cmd.Parameters.AddWithValue("@SupplierName", supplierName)
                    cmd.Parameters.AddWithValue("@ContactPerson", If(String.IsNullOrWhiteSpace(contactPerson), DBNull.Value, contactPerson))
                    cmd.Parameters.AddWithValue("@Phone", If(String.IsNullOrWhiteSpace(phone), DBNull.Value, phone))
                    cmd.Parameters.AddWithValue("@Email", If(String.IsNullOrWhiteSpace(email), DBNull.Value, email))
                    cmd.ExecuteNonQuery()
                    Return True
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error saving supplier: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

    Private Function GenerateSupplierCode(conn As SqlConnection) As String
        Try
            Dim query As String = "SELECT ISNULL(MAX(CAST(SUBSTRING(SupplierCode, 2, LEN(SupplierCode)) AS INT)), 0) + 1 FROM Suppliers WHERE SupplierCode LIKE 'S%' AND ISNUMERIC(SUBSTRING(SupplierCode, 2, LEN(SupplierCode))) = 1"
            Using cmd As New SqlCommand(query, conn)
                Dim result As Object = cmd.ExecuteScalar()
                Dim nextId As Integer = If(result Is Nothing OrElse IsDBNull(result), 1, Convert.ToInt32(result))
                Return "S" & nextId.ToString("D5")
            End Using
        Catch ex As Exception
            ' Fallback - generate code based on timestamp
            Return "S" & DateTime.Now.Ticks.ToString().Substring(DateTime.Now.Ticks.ToString().Length - 5)
        End Try
    End Function
End Class