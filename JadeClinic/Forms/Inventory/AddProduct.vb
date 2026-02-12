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

        ' Setup Unit dropdown
        UnitCmbBox.Items.Clear()
        UnitCmbBox.Items.AddRange(New String() {"PCS", "BOX", "PACK", "BOTTLE", "TUBE", "SET", "PAIR", "DOZEN", "REAM"})
        UnitCmbBox.SelectedItem = "PCS" ' Default to PCS

        ' Initialize main categories first, then load existing ones
        InitializeMainCategories()

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
        ' This method is now replaced by InitializeMainCategories
        ' Keeping for compatibility but redirect to new method
        Try
            InitializeMainCategories()
        Catch ex As Exception
            MessageBox.Show("Error loading categories: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub cmbCategory_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbCategory.SelectedIndexChanged
        If cmbCategory.SelectedItem IsNot Nothing Then
            Dim selectedCategory As String = cmbCategory.SelectedItem.ToString()

            If selectedCategory = "Add Custom Category..." Then
                ' Show dialog to add custom category
                AddCustomCategory()
            End If
            ' Note: Expiry date logic removed - expiry tracking moved to InventoryLog
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

        ' Validate that selling price is higher than cost price
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

            ' Validate wholesale price is between cost price and selling price
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

        ' Note: Expiry date validation removed - expiry tracking moved to InventoryLog

        Return True
    End Function

    Private Function SaveProduct() As Boolean
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using transaction As SqlTransaction = conn.BeginTransaction()
                    Try
                        ' Prepare product data (removed expiry logic)
                        Dim selectedCategory As String = cmbCategory.SelectedItem.ToString()

                        ' Insert product with temporary product code (removed SupplierID and expiry fields)
                        Dim insertQuery As String = "INSERT INTO Products (ProductCode, ProductName, Category, Unit, " &
                                                   "CurrentStock, ReorderLevel, CostPrice, SellingPrice, WholesalePrice, " &
                                                   "IsActive, Created, UpdatedAt) " &
                                                   "VALUES (@ProductCode, @ProductName, @Category, @Unit, " &
                                                   "@CurrentStock, @ReorderLevel, @CostPrice, @SellingPrice, @WholesalePrice, " &
                                                   "1, GETDATE(), GETDATE()); SELECT SCOPE_IDENTITY()"

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
                        ' Prepare product data (removed expiry logic)
                        Dim selectedCategory As String = cmbCategory.SelectedItem.ToString()

                        ' Update product WITHOUT changing ProductCode (keep original for barcode consistency) and removed expiry fields
                        Dim updateQuery As String = "UPDATE Products SET ProductName = @ProductName, Category = @Category, Unit = @Unit, " &
                                                   "ReorderLevel = @ReorderLevel, CostPrice = @CostPrice, SellingPrice = @SellingPrice, " &
                                                   "WholesalePrice = @WholesalePrice, UpdatedAt = GETDATE() WHERE ProductID = @ProductID"

                        Using cmd As New SqlCommand(updateQuery, conn, transaction)
                            cmd.Parameters.AddWithValue("@ProductName", txtProductName.Text.Trim())
                            cmd.Parameters.AddWithValue("@Category", selectedCategory)
                            cmd.Parameters.AddWithValue("@Unit", If(UnitCmbBox.SelectedItem IsNot Nothing, UnitCmbBox.SelectedItem.ToString(), "PCS"))
                            cmd.Parameters.AddWithValue("@ReorderLevel", If(String.IsNullOrWhiteSpace(ReOrderLevelTextBox.Text), 10, Convert.ToInt32(ReOrderLevelTextBox.Text.Trim())))
                            cmd.Parameters.AddWithValue("@CostPrice", Convert.ToDecimal(CostPriceTextBox.Text.Trim()))
                            cmd.Parameters.AddWithValue("@SellingPrice", Convert.ToDecimal(SellingPriceTextBox.Text.Trim()))
                            cmd.Parameters.AddWithValue("@WholesalePrice", If(String.IsNullOrWhiteSpace(WholeSaleTextbox.Text), DBNull.Value, Convert.ToDecimal(WholeSaleTextbox.Text.Trim())))
                            cmd.Parameters.AddWithValue("@ProductID", editProductId)

                            cmd.ExecuteNonQuery()
                        End Using

                        ' Flag to track if we need cleanup (only when image is actually updated)
                        Dim imageWasUpdated As Boolean = False

                        ' Update product image if new one selected
                        If Not String.IsNullOrWhiteSpace(selectedImagePath) AndAlso IO.File.Exists(selectedImagePath) Then
                            ' Delete old image mapping (not the image itself, as it might be used by other products)
                            Dim deleteMappingQuery As String = "DELETE FROM ProductImageMapping WHERE ProductID = @ProductID"
                            Using cmdDelete As New SqlCommand(deleteMappingQuery, conn, transaction)
                                cmdDelete.Parameters.AddWithValue("@ProductID", editProductId)
                                cmdDelete.ExecuteNonQuery()
                            End Using

                            ' Save new image using the corrected method
                            SaveProductImage(conn, transaction, editProductId, selectedImagePath)
                            imageWasUpdated = True
                        End If

                        transaction.Commit()

                        ' Only cleanup orphaned images if an image was actually updated
                        If imageWasUpdated Then
                            CleanupOrphanedImages()
                        End If

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
            ' Read the image file and convert to byte array
            Dim imageData As Byte() = File.ReadAllBytes(imagePath)

            ' Create a hash for the image to prevent duplicates
            Dim imageHash As String = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(imageData))

            ' First, check if this image already exists in ProductImages
            Dim existingImageId As Object = Nothing
            Dim checkImageQuery As String = "SELECT ImageID FROM ProductImages WHERE ImageHash = @ImageHash"
            Using checkCmd As New SqlCommand(checkImageQuery, conn, transaction)
                checkCmd.Parameters.AddWithValue("@ImageHash", imageHash)
                existingImageId = checkCmd.ExecuteScalar()
            End Using

            Dim imageId As Integer

            If existingImageId IsNot Nothing Then
                ' Use existing image
                imageId = Convert.ToInt32(existingImageId)
            Else
                ' Insert new image into ProductImages table
                Dim insertImageQuery As String = "INSERT INTO ProductImages (ImageHash, ImageType, ImageData, CreatedAt, UpdatedAt) VALUES (@ImageHash, @ImageType, @ImageData, GETDATE(), GETDATE()); SELECT SCOPE_IDENTITY()"
                Using cmdImage As New SqlCommand(insertImageQuery, conn, transaction)
                    cmdImage.Parameters.AddWithValue("@ImageHash", imageHash)
                    cmdImage.Parameters.AddWithValue("@ImageType", "thumb") ' Default type
                    cmdImage.Parameters.AddWithValue("@ImageData", imageData)

                    imageId = Convert.ToInt32(cmdImage.ExecuteScalar())
                End Using
            End If

            ' Now create the mapping between product and image
            Dim insertMappingQuery As String = "INSERT INTO ProductImageMapping (ProductID, ImageID, CreatedAt) VALUES (@ProductID, @ImageID, GETDATE())"
            Using cmdMapping As New SqlCommand(insertMappingQuery, conn, transaction)
                cmdMapping.Parameters.AddWithValue("@ProductID", productId)
                cmdMapping.Parameters.AddWithValue("@ImageID", imageId)

                cmdMapping.ExecuteNonQuery()
            End Using

            ' Optionally, optimize the image (resize, compress, etc.) before saving
            OptimizeImage(imagePath)

        Catch ex As Exception
            Throw New Exception("Error saving product image: " & ex.Message, ex)
        End Try
    End Sub

    Private Sub OptimizeImage(imagePath As String)
        ' Simplified image optimization to avoid GDI+ errors
        Try
            ' Skip optimization if it's causing issues
            ' Just validate the image can be loaded
            Using testImage As Image = Image.FromFile(imagePath)
                ' If we can load it, it's fine to use as is
                Console.WriteLine($"Image loaded successfully: {testImage.Width}x{testImage.Height}")
            End Using
        Catch ex As Exception
            ' Log the warning but don't break the process
            Console.WriteLine($"Warning: Image optimization skipped: {ex.Message}")
            ' The image was already saved successfully, so this doesn't affect functionality
        End Try
    End Sub

    Private Function ResizeImage(originalImage As Image, maxWidth As Integer, maxHeight As Integer) As Image
        Dim ratioX As Double = maxWidth / originalImage.Width
        Dim ratioY As Double = maxHeight / originalImage.Height
        Dim ratio As Double = Math.Min(ratioX, ratioY)

        Dim newWidth As Integer = CInt(originalImage.Width * ratio)
        Dim newHeight As Integer = CInt(originalImage.Height * ratio)

        Dim newImage As New Bitmap(newWidth, newHeight)
        Using g As Graphics = Graphics.FromImage(newImage)
            g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
            g.DrawImage(originalImage, 0, 0, newWidth, newHeight)
        End Using

        Return newImage
    End Function

    Private Function GetEncoderInfo(mimeType As String) As ImageCodecInfo
        Dim codecs As ImageCodecInfo() = ImageCodecInfo.GetImageDecoders()
        For Each c As ImageCodecInfo In codecs
            If c.MimeType = mimeType Then
                Return c
            End If
        Next
        Return Nothing
    End Function

    Private Sub LoadProductData()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                ' Updated query to use the correct table structure
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

                            ' Load product image with proper error handling
                            If Not IsDBNull(reader("ProductImage")) Then
                                Try
                                    Dim imgBytes As Byte() = CType(reader("ProductImage"), Byte())
                                    Using ms As New MemoryStream(imgBytes)
                                        ProductImage.Image = Image.FromStream(ms)
                                    End Using
                                Catch imgEx As Exception
                                    ' If image loading fails, just skip it (don't break the form load)
                                    Console.WriteLine($"Warning: Could not load product image: {imgEx.Message}")
                                    ProductImage.Image = Nothing
                                End Try
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
        ' Note: Expiry date clearing removed - expiry tracking moved to InventoryLog
    End Sub

    Private Sub InitializeMainCategories()
        Try
            ' Main categories for dental supply management
            Dim mainCategories As String() = {"ORTHO", "CONSUMABLES", "SURGERY", "RESTO", "ENDO", "COSMETIC"}

            ' Clear existing categories and add main categories first
            cmbCategory.Items.Clear()

            For Each category As String In mainCategories
                cmbCategory.Items.Add(category)
            Next

            ' Load additional categories from database (existing products)
            LoadAdditionalCategories()

            ' Add "Add Custom" option at the end
            cmbCategory.Items.Add("Add Custom Category...")

        Catch ex As Exception
            MessageBox.Show("Error initializing categories: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadAdditionalCategories()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                ' Get distinct categories from existing products that are not in main categories
                Dim mainCategoriesString As String = "'ORTHO','CONSUMABLES','SURGERY','RESTO','ENDO','COSMETIC'"
                Dim query As String = $"SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND IsActive = 1 AND Category NOT IN ({mainCategoriesString}) ORDER BY Category"

                Using cmd As New SqlCommand(query, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            If Not IsDBNull(reader("Category")) Then
                                Dim category As String = reader("Category").ToString()
                                ' Only add if it's not already in the list
                                If Not cmbCategory.Items.Contains(category) Then
                                    cmbCategory.Items.Add(category)
                                End If
                            End If
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            ' Silent fail - main categories are already loaded
            Console.WriteLine("Note: Could not load additional categories: " & ex.Message)
        End Try
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

    ' Smart cleanup for orphaned images - only runs when images are actually updated
    Private Sub CleanupOrphanedImages()
        Try
            ' Run cleanup in a separate transaction to avoid affecting main operation
            Dim connStr As String = Connection.GetConnectionString()
            Using cleanupConn As New SqlConnection(connStr)
                cleanupConn.Open()

                ' Get count of orphaned images first
                Dim countQuery As String = "SELECT COUNT(*) FROM ProductImages WHERE ImageID NOT IN (SELECT DISTINCT ImageID FROM ProductImageMapping)"
                Dim orphanCount As Integer

                Using countCmd As New SqlCommand(countQuery, cleanupConn)
                    orphanCount = Convert.ToInt32(countCmd.ExecuteScalar())
                End Using

                If orphanCount > 0 Then
                    ' Delete orphaned images
                    Dim deleteQuery As String = "DELETE FROM ProductImages WHERE ImageID NOT IN (SELECT DISTINCT ImageID FROM ProductImageMapping)"
                    Dim deletedCount As Integer

                    Using deleteCmd As New SqlCommand(deleteQuery, cleanupConn)
                        deletedCount = deleteCmd.ExecuteNonQuery()
                    End Using

                    ' Log the cleanup activity
                    Console.WriteLine($"🗑️ Cleaned up {deletedCount} orphaned image(s) during product update")

                    ' Log audit trail for cleanup
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Image Cleanup",
                                     $"Cleaned up {deletedCount} orphaned product images during product update")
                Else
                    Console.WriteLine("✅ No orphaned images found - database optimized")
                End If
            End Using

        Catch ex As Exception
            ' Log error but don't fail the main operation since product update succeeded
            Console.WriteLine($"⚠️ Image cleanup warning: {ex.Message}")
            ' Don't show error to user since the main product update was successful
        End Try
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

    Private Sub Guna2Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Guna2Panel1.Paint

    End Sub
End Class