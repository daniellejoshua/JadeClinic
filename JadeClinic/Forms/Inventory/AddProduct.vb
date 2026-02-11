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
        ' Start idle timeout monitoring for modal forms
        IdleTimeoutManager.Instance.StartMonitoring(Me)

        LoadCategories()
        ' Removed: LoadSuppliers() - no longer needed
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
        ' Removed: SupplierCMbBox.SelectedIndex = -1
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

        ' Hide supplier controls since we're removing this functionality
        If Me.Controls.Contains(SupplierCMbBox) Then
            SupplierCMbBox.Visible = False
        End If

        ' Hide supplier label too
        For Each ctrl As Control In Me.Controls
            If TypeOf ctrl Is Label AndAlso ctrl.Text.ToLower().Contains("supplier") Then
                ctrl.Visible = False
            End If
        Next
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

        ' NEW: Validate that selling price is higher than cost price
        If sellingPrice <= costPrice Then
            MessageBox.Show("Selling price must be higher than cost price!" & Environment.NewLine &
                          $"Cost Price: ₱{costPrice:N2}" & Environment.NewLine &
                          $"Selling Price: ₱{sellingPrice:N2}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            SellingPriceTextBox.Focus()
            Return False
        End If

        ' Validate wholesale price if provided
        Dim wholesalePrice As Decimal
        If Not String.IsNullOrWhiteSpace(WholeSaleTextbox.Text) Then
            If Not Decimal.TryParse(WholeSaleTextbox.Text.Trim(), wholesalePrice) OrElse wholesalePrice <= 0 Then
                MessageBox.Show("Wholesale price must be a valid number greater than 0!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                WholeSaleTextbox.Focus()
                Return False
            End If

            ' NEW: Validate wholesale price is between cost price and selling price
            If wholesalePrice < costPrice Then
                MessageBox.Show("Wholesale price cannot be lower than cost price!" & Environment.NewLine &
                              $"Cost Price: ₱{costPrice:N2}" & Environment.NewLine &
                              $"Wholesale Price: ₱{wholesalePrice:N2}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                WholeSaleTextbox.Focus()
                Return False
            End If

            If wholesalePrice >= sellingPrice Then
                MessageBox.Show("Wholesale price must be lower than selling price!" & Environment.NewLine &
                              $"Wholesale Price: ₱{wholesalePrice:N2}" & Environment.NewLine &
                              $"Selling Price: ₱{sellingPrice:N2}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                WholeSaleTextbox.Focus()
                Return False
            End If
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

                        ' Insert product with temporary product code (removed SupplierID)
                        Dim insertQuery As String = "INSERT INTO Products (ProductCode, ProductName, Category, Unit, " &
                                                   "CurrentStock, ReorderLevel, CostPrice, SellingPrice, WholesalePrice, " &
                                                   "HasExpiry, ExpiryDate, IsActive, Created, UpdatedAt) " &
                                                   "VALUES (@ProductCode, @ProductName, @Category, @Unit, " &
                                                   "@CurrentStock, @ReorderLevel, @CostPrice, @SellingPrice, @WholesalePrice, " &
                                                   "@HasExpiry, @ExpiryDate, 1, GETDATE(), GETDATE()); SELECT SCOPE_IDENTITY()"

                        Dim productId As Integer

                        Using cmd As New SqlCommand(insertQuery, conn, transaction)
                            cmd.Parameters.AddWithValue("@ProductCode", "TEMP_CODE")
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

                            productId = Convert.ToInt32(cmd.ExecuteScalar())
                        End Using

                        ' Update with final ProductCode (no separate barcode field)
                        Dim finalProductCode As String = Utilities.GenerateProductCode(productId)

                        Dim updateQuery As String = "UPDATE Products SET ProductCode = @ProductCode WHERE ProductID = @ProductID"
                        Using cmdUpdate As New SqlCommand(updateQuery, conn, transaction)
                            cmdUpdate.Parameters.AddWithValue("@ProductCode", finalProductCode)
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

                        ' Update product WITHOUT changing ProductCode (keep original for barcode consistency) and removed SupplierID
                        Dim updateQuery As String = "UPDATE Products SET ProductName = @ProductName, Category = @Category, Unit = @Unit, " &
                                                   "ReorderLevel = @ReorderLevel, CostPrice = @CostPrice, SellingPrice = @SellingPrice, " &
                                                   "WholesalePrice = @WholesalePrice, HasExpiry = @HasExpiry, ExpiryDate = @ExpiryDate, " &
                                                   "UpdatedAt = GETDATE() WHERE ProductID = @ProductID"

                        Using cmd As New SqlCommand(updateQuery, conn, transaction)
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

                            ' Removed: supplier ID logic since we're not using suppliers
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

                        ' Get the existing ProductCode for barcode display (don't regenerate)
                        Dim getCodeQuery As String = "SELECT ProductCode FROM Products WHERE ProductID = @ProductID"
                        Using getCodeCmd As New SqlCommand(getCodeQuery, conn)
                            getCodeCmd.Parameters.AddWithValue("@ProductID", editProductId)
                            Dim existingProductCode As String = getCodeCmd.ExecuteScalar()?.ToString()

                            ' Display barcode with existing ProductCode (no regeneration)
                            If Not String.IsNullOrEmpty(existingProductCode) Then
                                GenerateAndDisplayBarcode(existingProductCode)
                            End If
                        End Using

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

    Private Sub SaveProductImage(conn As SqlConnection, transaction As SqlTransaction, productId As Integer, imagePath As String)
        Try
            ' Read image file and compress if needed
            Dim originalBytes As Byte() = IO.File.ReadAllBytes(imagePath)
            Dim imageBytes As Byte()

            ' Use compression if image is larger than 500KB
            If originalBytes.Length > 500000 Then
                ' Assuming ImageCompression utility exists
                imageBytes = ImageCompression.CompressImage(originalBytes, 85) ' 85% quality
            Else
                imageBytes = originalBytes
            End If

            ' Generate hash for the image
            Dim imageHash As String = Utilities.GenerateImageHash(imageBytes)

            ' Check if image with same hash already exists
            Dim existingImageId As Integer? = Nothing

            ' Check for existing image with same hash
            Dim checkHashQuery As String = "SELECT TOP 1 ImageID FROM ProductImages WHERE ImageHash = @ImageHash"
            Using checkCmd As New SqlCommand(checkHashQuery, conn, transaction)
                checkCmd.Parameters.AddWithValue("@ImageHash", imageHash)
                Dim result = checkCmd.ExecuteScalar()

                If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                    existingImageId = Convert.ToInt32(result)
                End If
            End Using

            Dim imageId As Integer

            If existingImageId.HasValue Then
                ' Reuse existing image
                imageId = existingImageId.Value
                Console.WriteLine($"Reusing existing image with hash: {imageHash}")
            Else
                ' Save new image with hash
                Dim insertImageQuery As String = "INSERT INTO ProductImages (ImageHash, ImageType, ImageData, CreatedAt, UpdatedAt) " &
                                               "VALUES (@ImageHash, 'thumb', @ImageData, GETDATE(), GETDATE()); SELECT SCOPE_IDENTITY()"

                Using insertCmd As New SqlCommand(insertImageQuery, conn, transaction)
                    insertCmd.Parameters.AddWithValue("@ImageHash", imageHash)
                    insertCmd.Parameters.AddWithValue("@ImageData", imageBytes)
                    imageId = Convert.ToInt32(insertCmd.ExecuteScalar())
                End Using

                Console.WriteLine($"Saved new image with hash: {imageHash}")
            End If

            ' Create mapping between product and image (delete existing mapping first)
            Dim deleteMappingQuery As String = "DELETE FROM ProductImageMapping WHERE ProductID = @ProductID"
            Using deleteCmd As New SqlCommand(deleteMappingQuery, conn, transaction)
                deleteCmd.Parameters.AddWithValue("@ProductID", productId)
                deleteCmd.ExecuteNonQuery()
            End Using

            ' Insert new mapping
            Dim insertMappingQuery As String = "INSERT INTO ProductImageMapping (ProductID, ImageID, CreatedAt) " &
                                             "VALUES (@ProductID, @ImageID, GETDATE())"

            Using mapCmd As New SqlCommand(insertMappingQuery, conn, transaction)
                mapCmd.Parameters.AddWithValue("@ProductID", productId)
                mapCmd.Parameters.AddWithValue("@ImageID", imageId)
                mapCmd.ExecuteNonQuery()
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

                Dim query As String = "SELECT p.*, pi.ImageData AS ProductImage " +
                                     "FROM Products p " +
                                     "LEFT JOIN ProductImageMapping pim ON p.ProductID = pim.ProductID " +
                                     "LEFT JOIN ProductImages pi ON pim.ImageID = pi.ImageID " +
                                     "WHERE p.ProductID = @ProductID"

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

                            ' Removed: supplier loading logic since we're not using suppliers

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

                            ' Load and display barcode using ProductCode
                            If Not IsDBNull(reader("ProductCode")) Then
                                Dim productCode As String = reader("ProductCode").ToString()
                                GenerateAndDisplayBarcode(productCode)
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
        ' Removed: SupplierCMbBox.SelectedIndex = -1
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

    ' Form closing event to stop idle timeout monitoring
    Private Sub AddProduct_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Stop idle timeout monitoring when form closes
        IdleTimeoutManager.Instance.StopMonitoring(Me)
    End Sub

    Private Function IsNumeric(text As String) As Boolean
        Dim dummy As Double
        Return Double.TryParse(text, dummy)
    End Function

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

    Private Sub GenerateAndDisplayBarcode(productCode As String)
        Try
            ' Create barcode encoder using ProductCode (which now serves as both identifier and barcode)
            Dim encoder As New BarcodeEncoder()

            ' Generate barcode image using ProductCode
            Dim barcodeImg As Bitmap = encoder.Encode(BarcodeFormat.Code128, productCode)

            ' Display in picture box
            BarcodeImage.Image = barcodeImg

            ' Store current product code as barcode
            currentBarcode = productCode

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
End Class