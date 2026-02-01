Imports Microsoft.Data.SqlClient
Imports System.IO
Imports System.Drawing.Imaging

Public Class AddProduct
    Private selectedImagePath As String = ""
    Private customCategories As New List(Of String)

    Private Sub AddProduct_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LoadCategories()
        LoadSuppliers()
        SetupFormDefaults()
        SetupExpiryDateVisibility()
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
            If SaveProduct() Then
                MessageBox.Show("Product added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                ClearForm()
                Me.DialogResult = DialogResult.OK ' Set dialog result to OK for parent form to handle
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
        If Not String.IsNullOrWhiteSpace(ReOrderLevelTextBox.Text) AndAlso Not Integer.TryParse(ReOrderLevelTextBox.Text.Trim(), reorderLevel) Then
            MessageBox.Show("Reorder level must be a valid number!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ReOrderLevelTextBox.Focus()
            Return False
        End If

        ' Validate cost price
        Dim costPrice As Decimal
        If String.IsNullOrWhiteSpace(CostPriceTextBox.Text) OrElse Not Decimal.TryParse(CostPriceTextBox.Text.Trim(), costPrice) Then
            MessageBox.Show("Valid cost price is required!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            CostPriceTextBox.Focus()
            Return False
        End If

        ' Validate selling price
        Dim sellingPrice As Decimal
        If String.IsNullOrWhiteSpace(SellingPriceTextBox.Text) OrElse Not Decimal.TryParse(SellingPriceTextBox.Text.Trim(), sellingPrice) Then
            MessageBox.Show("Valid selling price is required!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            SellingPriceTextBox.Focus()
            Return False
        End If

        ' Validate wholesale price if provided
        Dim wholesalePrice As Decimal
        If Not String.IsNullOrWhiteSpace(WholeSaleTextbox.Text) AndAlso Not Decimal.TryParse(WholeSaleTextbox.Text.Trim(), wholesalePrice) Then
            MessageBox.Show("Wholesale price must be a valid number!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
                        ' Generate unique product code
                        Dim productCode As String = GenerateProductCode(conn, transaction)

                        ' Prepare product data
                        Dim selectedCategory As String = cmbCategory.SelectedItem.ToString()
                        Dim isEndo As Boolean = selectedCategory.ToLower().Contains("endo")
                        Dim expiryDate As Date? = If(isEndo, Guna2DateTimePicker1.Value.Date, Nothing)

                        ' Insert product
                        Dim insertQuery As String = "INSERT INTO Products (ProductCode, Barcode, ProductName, Category, Unit, " &
                                                   "CurrentStock, ReorderLevel, CostPrice, SellingPrice, WholesalePrice, " &
                                                   "HasExpiry, ExpiryDate, SupplierID, IsActive, Created, UpdatedAt) " &
                                                   "VALUES (@ProductCode, @Barcode, @ProductName, @Category, @Unit, " &
                                                   "@CurrentStock, @ReorderLevel, @CostPrice, @SellingPrice, @WholesalePrice, " &
                                                   "@HasExpiry, @ExpiryDate, @SupplierID, 1, GETDATE(), GETDATE()); SELECT SCOPE_IDENTITY()"

                        Using cmd As New SqlCommand(insertQuery, conn, transaction)
                            cmd.Parameters.AddWithValue("@ProductCode", productCode)

                            ' Generate unique barcode - use product code or combination
                            Dim barcode As String = productCode & DateTime.Now.Millisecond.ToString()
                            cmd.Parameters.AddWithValue("@Barcode", barcode)

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

                            Dim productId As Integer = Convert.ToInt32(cmd.ExecuteScalar())

                            ' Save product image if selected
                            If Not String.IsNullOrWhiteSpace(selectedImagePath) AndAlso IO.File.Exists(selectedImagePath) Then
                                SaveProductImage(conn, transaction, productId, selectedImagePath)
                            End If
                        End Using

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

    Private Function GenerateProductCode(conn As SqlConnection, transaction As SqlTransaction) As String
        ' Generate unique product code like P00001, P00002, etc.
        Try
            Dim query As String = "SELECT ISNULL(MAX(CAST(SUBSTRING(ProductCode, 2, LEN(ProductCode)) AS INT)), 0) + 1 FROM Products WHERE ProductCode LIKE 'P%' AND ISNUMERIC(SUBSTRING(ProductCode, 2, LEN(ProductCode))) = 1"
            Using cmd As New SqlCommand(query, conn, transaction)
                Dim result As Object = cmd.ExecuteScalar()
                Dim nextId As Integer = If(result Is Nothing OrElse IsDBNull(result), 1, Convert.ToInt32(result))
                Return "P" & nextId.ToString("D5")
            End Using
        Catch ex As Exception
            ' Fallback - generate code based on timestamp
            Return "P" & DateTime.Now.Ticks.ToString().Substring(DateTime.Now.Ticks.ToString().Length - 5)
        End Try
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

    Private Sub AddNewSupplier()
        ' Create overlay panel for modal effect
        Dim overlayPanel As New Panel()
        overlayPanel.BackColor = Color.FromArgb(150, 0, 0, 0) ' Semi-transparent black
        overlayPanel.Dock = DockStyle.Fill
        overlayPanel.Location = New Point(0, 0)
        overlayPanel.Size = Me.ClientSize
        Me.Controls.Add(overlayPanel)
        overlayPanel.BringToFront()

        ' Create a simple input form for supplier details
        Dim supplierForm As New Form()
        supplierForm.Text = ""
        supplierForm.Size = New Size(500, 450)
        supplierForm.StartPosition = FormStartPosition.CenterParent
        supplierForm.FormBorderStyle = FormBorderStyle.None
        supplierForm.MaximizeBox = False
        supplierForm.MinimizeBox = False
        supplierForm.BackColor = Color.FromArgb(30, 30, 30)
        supplierForm.ShowInTaskbar = False

        ' Add rounded corners effect
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

        ' Inner panel for content
        Dim contentPanel As New Panel()
        contentPanel.BackColor = Color.FromArgb(30, 30, 30)
        contentPanel.Dock = DockStyle.Fill
        borderPanel.Controls.Add(contentPanel)

        ' Title Label with close button area
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

        ' Main content panel
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
                                      ' Validate supplier name
                                      If String.IsNullOrWhiteSpace(txtName.Text) Then
                                          MessageBox.Show("Supplier name is required!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                          txtName.Focus()
                                          Return
                                      End If

                                      ' Check if supplier already exists
                                      If CheckSupplierExists(txtName.Text.Trim()) Then
                                          MessageBox.Show("A supplier with this name already exists!", "Duplicate Supplier", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                          txtName.Focus()
                                          Return
                                      End If

                                      ' Save supplier
                                      If SaveSupplier(txtName.Text.Trim(), txtContact.Text.Trim(), txtPhone.Text.Trim(), txtEmail.Text.Trim()) Then
                                          MessageBox.Show("Supplier added successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                          LoadSuppliers() ' Reload suppliers
                                          ' Select the newly added supplier
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

        ' Handle form closing to remove overlay
        AddHandler supplierForm.FormClosed, Sub(s, ev)
                                                If overlayPanel IsNot Nothing AndAlso Not overlayPanel.IsDisposed Then
                                                    Me.Controls.Remove(overlayPanel)
                                                    overlayPanel.Dispose()
                                                End If
                                            End Sub

        ' Prevent closing with escape or alt+F4
        AddHandler supplierForm.FormClosing, Sub(s, ev)
                                                 If ev.CloseReason = CloseReason.UserClosing Then
                                                     ' Only allow closing via buttons
                                                     ev.Cancel = False
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