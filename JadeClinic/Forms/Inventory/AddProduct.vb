Imports Microsoft.Data.Sqlite
Imports System.Data.Common
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

    Public Sub SetEditMode(productId As Integer)
        isEditMode = True
        editProductId = productId
    End Sub

    Private Sub AddProduct_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            ' Start idle timeout monitoring
            IdleTimeoutManager.Instance.StartMonitoring(Me)

            ' Initialize UI
            InitializeUI()
            LoadCategories()
            SetupFormDefaults()
            SetDefaultProductImage()
            SetupNumericInputValidation()

            ' Load product data if in edit mode
            If isEditMode Then
                LoadProductData()
                Guna2HtmlLabel6.Text = "Edit Product"
                ShowBarcodeSection()
            Else
                HideBarcodeSection()
            End If

            SetupTabIndex()

        Catch ex As Exception
            MessageBox.Show($"Error initializing form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetupTabIndex()
        txtProductName.TabIndex = 0
        cmbCategory.TabIndex = 1
        UnitCmbBox.TabIndex = 2
        CostPriceTextBox.TabIndex = 4
        SellingPriceTextBox.TabIndex = 5
        WholeSaleTextbox.TabIndex = 6
        ReOrderLevelTextBox.TabIndex = 3
        lblProductPicturetrigger.TabIndex = 7
        cmbStatus.TabIndex = 8
        PrintBarcodeTextBox.TabIndex = 9
        btnAddStock.TabIndex = 10
        Utilities.ApplyInputFocusEffects(Me)
    End Sub

    Private Sub InitializeUI()
        ' Configure status controls
        ConfigureStatusControls(isEditMode)

        ' Ensure form properties are set correctly
        Me.FormBorderStyle = FormBorderStyle.None
        Me.BackColor = Color.White
        Me.TopMost = False
    End Sub

    Private Sub ShowBarcodeSection()
        BarcodeImage.Visible = True
        PrintBarcodeTextBox.Visible = True
    End Sub

    Private Sub HideBarcodeSection()
        BarcodeImage.Visible = False
        PrintBarcodeTextBox.Visible = False
    End Sub

    Public Sub ConfigureStatusControls(show As Boolean, Optional isActive As Nullable(Of Boolean) = Nothing)
        Try
            If cmbStatus IsNot Nothing Then
                cmbStatus.DropDownStyle = ComboBoxStyle.DropDownList

                If cmbStatus.Items.Count = 0 Or show Then
                    cmbStatus.Items.Clear()
                    cmbStatus.Items.AddRange(New String() {"Active", "Inactive"})
                End If

                If isActive.HasValue Then
                    cmbStatus.SelectedItem = If(isActive.Value, "Active", "Inactive")
                ElseIf cmbStatus.SelectedIndex = -1 Then
                    cmbStatus.SelectedItem = "Active"
                End If

                cmbStatus.Visible = show
            End If

            If lblStatus IsNot Nothing Then
                lblStatus.Visible = show
            End If

        Catch ex As Exception
            Console.WriteLine($"ConfigureStatusControls warning: {ex.Message}")
        End Try
    End Sub

    Private Sub SetupFormDefaults()
        ' Set default values
        cmbCategory.SelectedIndex = -1
        UnitCmbBox.Items.Clear()
        UnitCmbBox.Items.AddRange(New String() {"PCS", "BOX", "PACK", "BOTTLE", "TUBE", "SET", "PAIR", "DOZEN", "REAM"})
        UnitCmbBox.SelectedIndex = -1

        ' Initialize categories
        InitializeMainCategories()

        ' Overlay placeholder labels on the combo boxes
        AddComboPlaceholder(cmbCategory, "Select Category")
        AddComboPlaceholder(UnitCmbBox, "Select Unit")

        ' Dashed border on the product image placeholder
        AddHandler ProductImage.Paint, AddressOf ProductImage_DashedBorder_Paint

        ' Set placeholder text
        CostPriceTextBox.PlaceholderText = "0.00"
        SellingPriceTextBox.PlaceholderText = "0.00"
        WholeSaleTextbox.PlaceholderText = "0.00"
        ReOrderLevelTextBox.PlaceholderText = "0"
    End Sub

    Private Sub AddComboPlaceholder(combo As ComboBox, text As String)
        Dim arrowWidth As Integer = 30
        Dim placeholder As New Label() With {
            .Text = text,
            .Font = combo.Font,
            .ForeColor = Color.DarkGray,
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(combo.Width - arrowWidth - 10, combo.Height - 4),
            .Location = New Point(combo.Location.X + 5, combo.Location.Y + 2),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Cursor = Cursors.Hand,
            .Enabled = False
        }
        combo.Parent.Controls.Add(placeholder)
        placeholder.BringToFront()

        Dim updatePlaceholder As Action =
            Sub()
                placeholder.Visible = (combo.SelectedIndex = -1)
            End Sub

        AddHandler combo.SelectedIndexChanged, Sub(s, e) updatePlaceholder()
        updatePlaceholder()
    End Sub

    Private Sub LoadCategories()
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
                AddCustomCategory()
            End If
        End If
    End Sub

    Private Sub AddCustomCategory()
        Dim customCategory As String = InputBox("Enter new category name:", "Add Custom Category")

        If Not String.IsNullOrWhiteSpace(customCategory) Then
            If Not cmbCategory.Items.Contains(customCategory) Then
                cmbCategory.Items.Insert(cmbCategory.Items.Count - 1, customCategory)
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

    Private Sub ProductImage_DashedBorder_Paint(sender As Object, e As PaintEventArgs)
        Using pen As New Pen(Color.LightGray, 2)
            pen.DashStyle = Drawing2D.DashStyle.Dash
            Dim rect As New Rectangle(1, 1, ProductImage.Width - 3, ProductImage.Height - 3)
            e.Graphics.DrawRectangle(pen, rect)
        End Using
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

    Public Function UpdateProductStatus(productId As Integer, isActive As Boolean) As Boolean
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqliteConnection(connStr)
                conn.Open()

                Dim updateQuery As String = "UPDATE Products SET IsActive = @IsActive, UpdatedAt = datetime('now') WHERE ProductID = @ProductID"
                Using cmd As New SqliteCommand(updateQuery, conn)
                    cmd.Parameters.AddWithValue("@IsActive", If(isActive, 1, 0))
                    cmd.Parameters.AddWithValue("@ProductID", productId)

                    Dim affected As Integer = cmd.ExecuteNonQuery()
                    If affected > 0 Then
                        Try
                            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Product Status Updated", $"ProductID: {productId} IsActive: {isActive}")
                        Catch
                            ' Don't block on audit logging failure
                        End Try
                        Return True
                    End If

                    Return False
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error updating product status: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function

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
        ' Validate product name
        If String.IsNullOrWhiteSpace(txtProductName.Text) Then
            MessageBox.Show("Product name is required!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtProductName.Focus()
            Return False
        End If

        ' Validate category
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

        ' Validate price relationship
        If sellingPrice <= costPrice Then
            MessageBox.Show("Selling price must be higher than cost price!" & Environment.NewLine &
                          $"Cost Price: ?{costPrice:N2}" & Environment.NewLine &
                          $"Selling Price: ?{sellingPrice:N2}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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

            If wholesalePrice < costPrice Then
                MessageBox.Show("Wholesale price cannot be lower than cost price!" & Environment.NewLine &
                              $"Cost Price: ?{costPrice:N2}" & Environment.NewLine &
                              $"Wholesale Price: ?{wholesalePrice:N2}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                WholeSaleTextbox.Focus()
                Return False
            End If

            If wholesalePrice >= sellingPrice Then
                MessageBox.Show("Wholesale price must be lower than selling price!" & Environment.NewLine &
                              $"Wholesale Price: ?{wholesalePrice:N2}" & Environment.NewLine &
                              $"Selling Price: ?{sellingPrice:N2}", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                WholeSaleTextbox.Focus()
                Return False
            End If
        End If

        Return True
    End Function

    Private Function SaveProduct() As Boolean
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqliteConnection(connStr)
                conn.Open()
                Using transaction As SqliteTransaction = conn.BeginTransaction()
                    Try
                        Dim selectedCategory As String = cmbCategory.SelectedItem.ToString()

                        Dim insertQuery As String = "INSERT INTO Products (ProductCode, ProductName, Category, Unit, " &
                                                   "CurrentStock, ReorderLevel, CostPrice, SellingPrice, WholesalePrice, " &
                                                   "IsActive, Created, UpdatedAt) " &
                                                   "VALUES (@ProductCode, @ProductName, @Category, @Unit, " &
                                                   "@CurrentStock, @ReorderLevel, @CostPrice, @SellingPrice, @WholesalePrice, " &
                                                    "1, datetime('now'), datetime('now')); SELECT last_insert_rowid()"

                        Dim productId As Integer

                        Using cmd As New SqliteCommand(insertQuery, conn, transaction)
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

                        ' Update with final ProductCode
                        Dim finalProductCode As String = Utilities.GenerateProductCode(productId)

                        Dim updateQuery As String = "UPDATE Products SET ProductCode = @ProductCode WHERE ProductID = @ProductID"
                        Using cmdUpdate As New SqliteCommand(updateQuery, conn, transaction)
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
            Using conn As New SqliteConnection(connStr)
                conn.Open()
                Using transaction As SqliteTransaction = conn.BeginTransaction()
                    Try
                        Dim selectedCategory As String = cmbCategory.SelectedItem.ToString()

                        Dim updateQuery As String = "UPDATE Products SET ProductName = @ProductName, Category = @Category, Unit = @Unit, " &
                                               "ReorderLevel = @ReorderLevel, CostPrice = @CostPrice, SellingPrice = @SellingPrice, " &
                                                "WholesalePrice = @WholesalePrice, IsActive = @IsActive, UpdatedAt = datetime('now') WHERE ProductID = @ProductID"

                        Using cmd As New SqliteCommand(updateQuery, conn, transaction)
                            cmd.Parameters.AddWithValue("@ProductName", txtProductName.Text.Trim())
                            cmd.Parameters.AddWithValue("@Category", selectedCategory)
                            cmd.Parameters.AddWithValue("@Unit", If(UnitCmbBox.SelectedItem IsNot Nothing, UnitCmbBox.SelectedItem.ToString(), "PCS"))
                            cmd.Parameters.AddWithValue("@ReorderLevel", If(String.IsNullOrWhiteSpace(ReOrderLevelTextBox.Text), 10, Convert.ToInt32(ReOrderLevelTextBox.Text.Trim())))
                            cmd.Parameters.AddWithValue("@CostPrice", Convert.ToDecimal(CostPriceTextBox.Text.Trim()))
                            cmd.Parameters.AddWithValue("@SellingPrice", Convert.ToDecimal(SellingPriceTextBox.Text.Trim()))
                            cmd.Parameters.AddWithValue("@WholesalePrice", If(String.IsNullOrWhiteSpace(WholeSaleTextbox.Text), DBNull.Value, Convert.ToDecimal(WholeSaleTextbox.Text.Trim())))

                            Dim isActiveFlag As Boolean = True
                            If cmbStatus IsNot Nothing AndAlso cmbStatus.SelectedItem IsNot Nothing Then
                                isActiveFlag = (cmbStatus.SelectedItem.ToString() = "Active")
                            End If
                            cmd.Parameters.AddWithValue("@IsActive", If(isActiveFlag, 1, 0))
                            cmd.Parameters.AddWithValue("@ProductID", editProductId)

                            cmd.ExecuteNonQuery()
                        End Using

                        Dim imageWasUpdated As Boolean = False

                        If Not String.IsNullOrWhiteSpace(selectedImagePath) AndAlso IO.File.Exists(selectedImagePath) Then
                            Dim deleteMappingQuery As String = "DELETE FROM ProductImageMapping WHERE ProductID = @ProductID"
                            Using cmdDelete As New SqliteCommand(deleteMappingQuery, conn, transaction)
                                cmdDelete.Parameters.AddWithValue("@ProductID", editProductId)
                                cmdDelete.ExecuteNonQuery()
                            End Using

                            SaveProductImage(conn, transaction, editProductId, selectedImagePath)
                            imageWasUpdated = True
                        End If

                        transaction.Commit()

                        If imageWasUpdated Then
                            CleanupOrphanedImages()
                        End If

                        Dim getCodeQuery As String = "SELECT ProductCode FROM Products WHERE ProductID = @ProductID"
                        Using getCodeCmd As New SqliteCommand(getCodeQuery, conn)
                            getCodeCmd.Parameters.AddWithValue("@ProductID", editProductId)
                            Dim existingProductCode As String = getCodeCmd.ExecuteScalar()?.ToString()
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

    Private Sub SaveProductImage(conn As SqliteConnection, transaction As SqliteTransaction, productId As Integer, imagePath As String)
        Try
            Dim imageBytes As Byte() = File.ReadAllBytes(imagePath)
            Dim imageHash As String = Convert.ToBase64String(System.Security.Cryptography.SHA256.Create().ComputeHash(imageBytes))

            Dim existingImageId As Object = Nothing
            Dim checkImageQuery As String = "SELECT ImageID FROM ProductImages WHERE ImageHash = @ImageHash"
            Using checkCmd As New SqliteCommand(checkImageQuery, conn, transaction)
                checkCmd.Parameters.AddWithValue("@ImageHash", imageHash)
                existingImageId = checkCmd.ExecuteScalar()
            End Using

            Dim imageId As Integer

            If existingImageId IsNot Nothing Then
                imageId = Convert.ToInt32(existingImageId)
            Else
                Dim insertImageQuery As String = "INSERT INTO ProductImages (ImageHash, ImageType, FilePath, CreatedAt, UpdatedAt) VALUES (@ImageHash, @ImageType, '', datetime('now'), datetime('now')); SELECT last_insert_rowid()"
                Using cmdImage As New SqliteCommand(insertImageQuery, conn, transaction)
                    cmdImage.Parameters.AddWithValue("@ImageHash", imageHash)
                    cmdImage.Parameters.AddWithValue("@ImageType", "thumb")
                    imageId = Convert.ToInt32(cmdImage.ExecuteScalar())
                End Using

                Dim fileName As String = $"img_{imageId}.jpg"
                Dim destDir As String = Connection.GetImagesFolder("products")
                Dim destPath As String = Path.Combine(destDir, fileName)

                Dim compressed As Byte() = ImageCompression.CompressImage(imageBytes, 80)
                File.WriteAllBytes(destPath, compressed)

                Dim updatePathQuery As String = "UPDATE ProductImages SET FilePath = @FilePath WHERE ImageID = @ImageID"
                Using cmdUpdate As New SqliteCommand(updatePathQuery, conn, transaction)
                    cmdUpdate.Parameters.AddWithValue("@FilePath", fileName)
                    cmdUpdate.Parameters.AddWithValue("@ImageID", imageId)
                    cmdUpdate.ExecuteNonQuery()
                End Using
            End If

            Dim insertMappingQuery As String = "INSERT INTO ProductImageMapping (ProductID, ImageID, CreatedAt) VALUES (@ProductID, @ImageID, datetime('now'))"
            Using cmdMapping As New SqliteCommand(insertMappingQuery, conn, transaction)
                cmdMapping.Parameters.AddWithValue("@ProductID", productId)
                cmdMapping.Parameters.AddWithValue("@ImageID", imageId)
                cmdMapping.ExecuteNonQuery()
            End Using

        Catch ex As Exception
            Throw New Exception("Error saving product image: " & ex.Message, ex)
        End Try
    End Sub

    Private Sub LoadProductData()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqliteConnection(connStr)
                conn.Open()

                Dim query As String = "SELECT p.*, pi.FilePath AS ProductImage " &
                                  "FROM Products p " &
                                  "LEFT JOIN ProductImageMapping pim ON p.ProductID = pim.ProductID " &
                                  "LEFT JOIN ProductImages pi ON pim.ImageID = pi.ImageID " &
                                  "WHERE p.ProductID = @ProductID"

                Using cmd As New SqliteCommand(query, conn)
                    cmd.Parameters.AddWithValue("@ProductID", editProductId)

                    Using reader As DbDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            txtProductName.Text = reader("ProductName").ToString()

                            Dim category As String = reader("Category").ToString()
                            If cmbCategory.Items.Contains(category) Then
                                cmbCategory.SelectedItem = category
                            End If

                            Dim unit As String = reader("Unit").ToString()
                            If UnitCmbBox.Items.Contains(unit) Then
                                UnitCmbBox.SelectedItem = unit
                            End If

                            If Not IsDBNull(reader("CostPrice")) Then
                                CostPriceTextBox.Text = Convert.ToDecimal(reader("CostPrice")).ToString("0.00")
                            End If
                            If Not IsDBNull(reader("SellingPrice")) Then
                                SellingPriceTextBox.Text = Convert.ToDecimal(reader("SellingPrice")).ToString("0.00")
                            End If
                            If Not IsDBNull(reader("WholesalePrice")) Then
                                WholeSaleTextbox.Text = Convert.ToDecimal(reader("WholesalePrice")).ToString("0.00")
                            End If
                            If Not IsDBNull(reader("ReorderLevel")) Then
                                ReOrderLevelTextBox.Text = reader("ReorderLevel").ToString()
                            End If

                            If Not IsDBNull(reader("ProductImage")) Then
                                Try
                                    Dim filePath As String = reader("ProductImage").ToString()
                                    If Not String.IsNullOrEmpty(filePath) Then
                                        Dim fullPath As String = Path.Combine(Connection.GetImagesFolder("products"), filePath)
                                        If IO.File.Exists(fullPath) Then
                                            ProductImage.Image = Image.FromFile(fullPath)
                                        Else
                                            SetDefaultProductImage()
                                        End If
                                    Else
                                        SetDefaultProductImage()
                                    End If
                                Catch
                                    SetDefaultProductImage()
                                End Try
                            Else
                                SetDefaultProductImage()
                            End If

                            If Not IsDBNull(reader("ProductCode")) Then
                                Dim productCode As String = reader("ProductCode").ToString()
                                GenerateAndDisplayBarcode(productCode)
                            End If

                            If Not IsDBNull(reader("IsActive")) Then
                                Dim isActiveVal As Boolean = Convert.ToBoolean(reader("IsActive"))
                                ConfigureStatusControls(True, isActiveVal)
                            Else
                                ConfigureStatusControls(True, True)
                            End If
                        End If
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading product data: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetDefaultProductImage()
        ProductImage.Image = My.Resources.product_placeholder
    End Sub

    Private Sub ClearForm()
        txtProductName.Clear()
        cmbCategory.SelectedIndex = -1
        CostPriceTextBox.Clear()
        SellingPriceTextBox.Clear()
        WholeSaleTextbox.Clear()
        ReOrderLevelTextBox.Clear()
        SetDefaultProductImage()
        selectedImagePath = ""
    End Sub

    Private Sub InitializeMainCategories()
        Try
            Dim mainCategories As String() = {"ORTHO", "CONSUMABLES", "SURGERY", "RESTO", "ENDO", "COSMETIC"}

            cmbCategory.Items.Clear()

            For Each category As String In mainCategories
                cmbCategory.Items.Add(category)
            Next

            LoadAdditionalCategories()
            cmbCategory.Items.Add("Add Custom Category...")

        Catch ex As Exception
            MessageBox.Show("Error initializing categories: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadAdditionalCategories()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqliteConnection(connStr)
                conn.Open()

                Dim mainCategoriesString As String = "'ORTHO','CONSUMABLES','SURGERY','RESTO','ENDO','COSMETIC'"
                Dim query As String = $"SELECT DISTINCT Category FROM Products WHERE Category IS NOT NULL AND IsActive = 1 AND Category NOT IN ({mainCategoriesString}) ORDER BY Category"

                Using cmd As New SqliteCommand(query, conn)
                    Using reader As DbDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            If Not IsDBNull(reader("Category")) Then
                                Dim category As String = reader("Category").ToString()
                                If Not cmbCategory.Items.Contains(category) Then
                                    cmbCategory.Items.Add(category)
                                End If
                            End If
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine("Note: Could not load additional categories: " & ex.Message)
        End Try
    End Sub

    Private Sub SetupNumericInputValidation()
        AddHandler CostPriceTextBox.KeyPress, AddressOf NumericTextBox_KeyPress
        AddHandler SellingPriceTextBox.KeyPress, AddressOf NumericTextBox_KeyPress
        AddHandler WholeSaleTextbox.KeyPress, AddressOf NumericTextBox_KeyPress
        AddHandler ReOrderLevelTextBox.KeyPress, AddressOf IntegerTextBox_KeyPress
    End Sub

    Private Sub NumericTextBox_KeyPress(sender As Object, e As KeyPressEventArgs)
        Try
            Dim decimalSep As String = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator
            Dim decimalChar As Char = decimalSep(0)

            If e.KeyChar = ChrW(Keys.Back) Then
                Return
            End If

            If Char.IsDigit(e.KeyChar) Then
                Return
            End If

            If e.KeyChar = decimalChar Then
                Dim tbText As String = String.Empty
                Dim selStart As Integer = 0
                Dim selLen As Integer = 0

                Dim tbBase = TryCast(sender, TextBoxBase)
                Dim gunaTb = TryCast(sender, Guna.UI2.WinForms.Guna2TextBox)

                If tbBase IsNot Nothing Then
                    tbText = tbBase.Text
                    selStart = tbBase.SelectionStart
                    selLen = tbBase.SelectionLength
                ElseIf gunaTb IsNot Nothing Then
                    tbText = gunaTb.Text
                    selStart = gunaTb.SelectionStart
                    selLen = gunaTb.SelectionLength
                ElseIf TypeOf sender Is Control Then
                    tbText = CType(sender, Control).Text
                End If

                Dim candidate As String = tbText
                If selLen > 0 AndAlso selStart >= 0 Then
                    candidate = tbText.Remove(selStart, selLen)
                End If

                If candidate.Contains(decimalChar) Then
                    e.Handled = True
                End If

                Return
            End If

            e.Handled = True

        Catch ex As Exception
            e.Handled = True
        End Try
    End Sub

    Private Sub IntegerTextBox_KeyPress(sender As Object, e As KeyPressEventArgs)
        If Not Char.IsDigit(e.KeyChar) AndAlso Not e.KeyChar = ChrW(Keys.Back) Then
            e.Handled = True
        End If
    End Sub

    Private Sub GenerateAndDisplayBarcode(productCode As String)
        Try
            Dim encoder As New BarcodeEncoder()
            Dim barcodeImg As Bitmap = encoder.Encode(BarcodeFormat.Code128, productCode)

            BarcodeImage.Image = barcodeImg
            currentBarcode = productCode

        Catch ex As Exception
            MessageBox.Show("Error generating barcode: " & ex.Message, "Barcode Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub CleanupOrphanedImages()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using cleanupConn As New SqliteConnection(connStr)
                cleanupConn.Open()

                Dim orphanQuery As String = "SELECT FilePath FROM ProductImages WHERE ImageID NOT IN (SELECT DISTINCT ImageID FROM ProductImageMapping)"
                Dim orphanFiles As New List(Of String)
                Using orphanCmd As New SqliteCommand(orphanQuery, cleanupConn)
                    Using reader As DbDataReader = orphanCmd.ExecuteReader()
                        While reader.Read()
                            If Not IsDBNull(reader("FilePath")) Then
                                orphanFiles.Add(reader("FilePath").ToString())
                            End If
                        End While
                    End Using
                End Using

                Dim countQuery As String = "SELECT COUNT(*) FROM ProductImages WHERE ImageID NOT IN (SELECT DISTINCT ImageID FROM ProductImageMapping)"
                Dim orphanCount As Integer
                Using countCmd As New SqliteCommand(countQuery, cleanupConn)
                    orphanCount = Convert.ToInt32(countCmd.ExecuteScalar())
                End Using

                If orphanCount > 0 Then
                    Dim deleteQuery As String = "DELETE FROM ProductImages WHERE ImageID NOT IN (SELECT DISTINCT ImageID FROM ProductImageMapping)"
                    Dim deletedCount As Integer
                    Using deleteCmd As New SqliteCommand(deleteQuery, cleanupConn)
                        deletedCount = deleteCmd.ExecuteNonQuery()
                    End Using

                    For Each filePath As String In orphanFiles
                        Try
                            Dim fullPath As String = Path.Combine(Connection.GetImagesFolder("products"), filePath)
                            If IO.File.Exists(fullPath) Then
                                IO.File.Delete(fullPath)
                            End If
                        Catch
                        End Try
                    Next

                    Console.WriteLine($"??? Cleaned up {deletedCount} orphaned image(s) during product update")
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Image Cleanup",
                                     $"Cleaned up {deletedCount} orphaned product images during product update")
                Else
                    Console.WriteLine("? No orphaned images found - database optimized")
                End If
            End Using

        Catch ex As Exception
            Console.WriteLine($"?? Image cleanup warning: {ex.Message}")
        End Try
    End Sub

    Private Sub Guna2HtmlLabel1_Click(sender As Object, e As EventArgs) Handles Guna2HtmlLabel1.Click
        Close()
    End Sub

    Private Sub AddProduct_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        IdleTimeoutManager.Instance.StopMonitoring(Me)
    End Sub

    Private printDocument As Printing.PrintDocument
    Private printPreviewDialog As PrintPreviewDialog

    Private Sub PrintBarcodeTextBox_Click(sender As Object, e As EventArgs) Handles PrintBarcodeTextBox.Click
        If String.IsNullOrWhiteSpace(currentBarcode) Then
            MessageBox.Show("No barcode to print. Please save the product first.", "Print Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            printDocument = New Printing.PrintDocument()

            Dim margin As Integer = 12
            Dim contentWidth As Integer = 210
            Dim totalHeight As Integer = margin + 32 + 22 + 18 +
                                       If(BarcodeImage.Image IsNot Nothing, 44, 0) + 16 + margin

            Dim paperWidth As Integer = contentWidth + 2 * margin
            Dim paperHeight As Integer = totalHeight

            Dim tagSize As New Printing.PaperSize("ProductLabel", paperWidth, paperHeight)
            printDocument.DefaultPageSettings.PaperSize = tagSize
            printDocument.DefaultPageSettings.Margins = New Printing.Margins(0, 0, 0, 0)

            AddHandler printDocument.PrintPage, AddressOf OnPrintBarcodePage

            printPreviewDialog = New PrintPreviewDialog()
            printPreviewDialog.Document = printDocument
            printPreviewDialog.Text = "Product Barcode Print Preview"
            printPreviewDialog.WindowState = FormWindowState.Maximized
            printPreviewDialog.ShowDialog()

        Catch ex As Exception
            MessageBox.Show("Error setting up barcode print: " & ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub OnPrintBarcodePage(sender As Object, e As Printing.PrintPageEventArgs)
        Try
            Dim margin As Integer = 12
            Dim contentWidth As Integer = 210
            Dim y As Integer = margin
            Dim g = e.Graphics
            g.Clear(Color.White)

            Dim fontName As New Font("Arial", 11, FontStyle.Bold)
            Dim fontLabel As New Font("Arial", 8, FontStyle.Regular)
            Dim fontPrice As New Font("Arial", 12, FontStyle.Bold)
            Dim fontBarcode As New Font("Courier New", 8, FontStyle.Regular)

            Dim totalHeight As Integer = margin + 32 + 22 + 18 +
                                       If(BarcodeImage.Image IsNot Nothing, 44, 0) + 16 + margin

            Dim paperWidth As Integer = contentWidth + 2 * margin
            Dim paperHeight As Integer = totalHeight
            e.PageSettings.PaperSize = New Printing.PaperSize("ProductLabel", paperWidth, paperHeight)

            y = margin

            ' Product Name
            Dim nameRect As New RectangleF(margin, y, contentWidth, 32)
            Dim productName As String = txtProductName.Text.Trim()
            If productName.Length > 30 Then
                productName = productName.Substring(0, 27) + "..."
            End If
            g.DrawString(productName, fontName, Brushes.Black, nameRect,
                        New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Near})
            y += 32

            ' Price
            Dim priceText As String = "₱" & SellingPriceTextBox.Text.Trim()
            g.DrawString(priceText, fontPrice, Brushes.Black,
                        New RectangleF(margin, y, contentWidth, 20),
                        New StringFormat With {.Alignment = StringAlignment.Center})
            y += 22

            ' Category and Unit
            Dim categoryUnit As String = $"Category: {cmbCategory.Text.Trim()}   Unit: {UnitCmbBox.Text.Trim()}"
            g.DrawString(categoryUnit, fontLabel, Brushes.Black,
                        New RectangleF(margin, y, contentWidth, 16),
                        New StringFormat With {.Alignment = StringAlignment.Center})
            y += 18

            ' Barcode
            If BarcodeImage.Image IsNot Nothing Then
                Dim barcodeWidth As Integer = 140
                Dim barcodeHeight As Integer = 40
                Dim barcodeX As Integer = margin + (contentWidth - barcodeWidth) \ 2
                g.DrawImage(BarcodeImage.Image, barcodeX, y, barcodeWidth, barcodeHeight)
                y += barcodeHeight + 4
            End If

            ' Barcode value
            g.DrawString(currentBarcode, fontBarcode, Brushes.Black,
                        New RectangleF(margin, y, contentWidth, 14),
                        New StringFormat With {.Alignment = StringAlignment.Center})

            fontName.Dispose()
            fontLabel.Dispose()
            fontPrice.Dispose()
            fontBarcode.Dispose()

        Catch ex As Exception
            MessageBox.Show("Error during barcode printing: " & ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PrintBarcodeDirectly()
        If String.IsNullOrWhiteSpace(currentBarcode) Then
            MessageBox.Show("No barcode to print. Please save the product first.", "Print Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Using printDialog As New PrintDialog()
                printDocument = New Printing.PrintDocument()
                printDialog.Document = printDocument

                If printDialog.ShowDialog() = DialogResult.OK Then
                    Dim margin As Integer = 12
                    Dim contentWidth As Integer = 210
                    Dim totalHeight As Integer = margin + 32 + 22 + 18 +
                                               If(BarcodeImage.Image IsNot Nothing, 44, 0) + 16 + margin

                    Dim paperWidth As Integer = contentWidth + 2 * margin
                    Dim paperHeight As Integer = totalHeight

                    Dim tagSize As New Printing.PaperSize("ProductLabel", paperWidth, paperHeight)
                    printDocument.DefaultPageSettings.PaperSize = tagSize
                    printDocument.DefaultPageSettings.Margins = New Printing.Margins(0, 0, 0, 0)
                    printDocument.PrinterSettings = printDialog.PrinterSettings

                    AddHandler printDocument.PrintPage, AddressOf OnPrintBarcodePage
                    printDocument.Print()

                    MessageBox.Show("Barcode label sent to printer successfully!", "Print Complete",
                                  MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show("Error printing barcode: " & ex.Message, "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Guna2Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Guna2Panel1.Paint

    End Sub
End Class
