Imports Microsoft.Data.SqlClient
Imports System.Data

Public Class AddInventoryLogForm
    Private products As New List(Of Dictionary(Of String, Object))
    Private suppliers As New List(Of Dictionary(Of String, Object))

    ' Control variables to store references
    Private cmbProduct As ComboBox
    Private cmbTransactionType As ComboBox
    Private txtQuantity As TextBox
    Private cmbSupplier As ComboBox
    Private txtReference As TextBox
    Private txtNotes As TextBox

    ' Field to prevent recursive TextChanged handling
    Private suppressProductTextChanged As Boolean = False

    Private Sub AddInventoryLogForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Start idle timeout monitoring for modal forms
        IdleTimeoutManager.Instance.StartMonitoring(Me)

        ' Setup form
        SetupForm()

        ' Load data
        LoadProducts()
        LoadSuppliers()

        ' Setup controls
        SetupControls()
    End Sub

    Private Sub SetupForm()
        ' Make form non-resizable
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Color.FromArgb(30, 30, 30)
        Me.Size = New Size(600, 750) ' Increased height for batch fields

        ' Add border
        AddHandler Me.Paint, Sub(s, e)
                                 Using pen As New Pen(Color.FromArgb(61, 65, 66), 2)
                                     e.Graphics.DrawRectangle(pen, 0, 0, Me.Width - 1, Me.Height - 1)
                                 End Using
                             End Sub
    End Sub

    Private Sub LoadProducts()
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                ' Include Category in the query to determine if batch tracking is needed
                Dim query As String = "SELECT ProductID, ProductName, Category, CurrentStock FROM Products WHERE IsActive = 1 ORDER BY ProductName"
                Using cmd As New SqlCommand(query, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        products.Clear()

                        While reader.Read()
                            Dim productData As New Dictionary(Of String, Object) From {
                                {"ProductID", reader("ProductID")},
                                {"ProductName", reader("ProductName").ToString()},
                                {"Category", reader("Category").ToString()}, ' Added category for batch logic
                                {"CurrentStock", If(IsDBNull(reader("CurrentStock")), 0, Convert.ToInt32(reader("CurrentStock")))}
                            }
                            products.Add(productData)
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading products: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
                        suppliers.Clear()

                        While reader.Read()
                            Dim supplierData As New Dictionary(Of String, Object) From {
                                {"SupplierID", reader("SupplierID")},
                                {"SupplierName", reader("SupplierName").ToString()}
                            }
                            suppliers.Add(supplierData)
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show("Error loading suppliers: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetupControls()
        ' Create and setup all controls programmatically
        CreateControls()
    End Sub

    Private Sub CreateControls()
        ' Title
        Dim lblTitle As New Label()
        lblTitle.Text = "Add Inventory Log"
        lblTitle.Font = New Font("Poppins", 16, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.Location = New Point(30, 30)
        lblTitle.AutoSize = True
        Me.Controls.Add(lblTitle)

        ' Close button
        Dim btnClose As New Label()
        btnClose.Text = "X"
        btnClose.Font = New Font("Arial", 16, FontStyle.Bold)
        btnClose.ForeColor = Color.Gray
        btnClose.Cursor = Cursors.Hand
        btnClose.Location = New Point(560, 30)
        btnClose.Size = New Size(30, 30)
        btnClose.TextAlign = ContentAlignment.MiddleCenter
        AddHandler btnClose.Click, Sub(s, ev) Me.Close()
        AddHandler btnClose.MouseEnter, Sub(s, ev) btnClose.ForeColor = Color.Red
        AddHandler btnClose.MouseLeave, Sub(s, ev) btnClose.ForeColor = Color.Gray
        Me.Controls.Add(btnClose)

        Dim yPos = 100

        ' Product Selection
        Dim lblProduct As New Label()
        lblProduct.Text = "Product *"
        lblProduct.Font = New Font("Poppins", 10, FontStyle.Bold)
        lblProduct.ForeColor = Color.White
        lblProduct.Location = New Point(30, yPos)
        lblProduct.AutoSize = True
        Me.Controls.Add(lblProduct)

        cmbProduct = New ComboBox() With {
            .Font = New Font("Poppins", 10),
            .Location = New Point(30, yPos + 30),
            .Size = New Size(540, 35),
            .DropDownStyle = ComboBoxStyle.DropDown, ' allow typing
            .BackColor = Color.FromArgb(61, 65, 66),
            .ForeColor = Color.White,
            .AutoCompleteMode = AutoCompleteMode.None,
            .AutoCompleteSource = AutoCompleteSource.None
        }

        ' Populate with full product list initially (use helper)
        PopulateComboWithProducts(cmbProduct)

        ' Wire handlers
        AddHandler cmbProduct.SelectedIndexChanged, AddressOf cmbProduct_SelectedIndexChanged
        AddHandler cmbProduct.TextChanged, AddressOf cmbProduct_TextChanged
        AddHandler cmbProduct.KeyDown, AddressOf cmbProduct_KeyDown
        AddHandler cmbProduct.Leave, AddressOf cmbProduct_Leave

        Me.Controls.Add(cmbProduct)

        yPos += 90

        ' Transaction Type
        Dim lblTransactionType As New Label()
        lblTransactionType.Text = "Transaction Type *"
        lblTransactionType.Font = New Font("Poppins", 10, FontStyle.Bold)
        lblTransactionType.ForeColor = Color.White
        lblTransactionType.Location = New Point(30, yPos)
        lblTransactionType.AutoSize = True
        Me.Controls.Add(lblTransactionType)

        cmbTransactionType = New ComboBox()
        cmbTransactionType.Font = New Font("Poppins", 10)
        cmbTransactionType.Location = New Point(30, yPos + 30)
        cmbTransactionType.Size = New Size(260, 35)
        cmbTransactionType.DropDownStyle = ComboBoxStyle.DropDownList
        cmbTransactionType.BackColor = Color.FromArgb(61, 65, 66)
        cmbTransactionType.ForeColor = Color.White
        cmbTransactionType.Items.AddRange(New String() {"IN", "OUT"})
        ' Add event handler to show/hide batch fields based on transaction type
        AddHandler cmbTransactionType.SelectedIndexChanged, AddressOf cmbTransactionType_SelectedIndexChanged
        Me.Controls.Add(cmbTransactionType)

        ' Quantity
        Dim lblQuantity As New Label()
        lblQuantity.Text = "Quantity *"
        lblQuantity.Font = New Font("Poppins", 10, FontStyle.Bold)
        lblQuantity.ForeColor = Color.White
        lblQuantity.Location = New Point(310, yPos)
        lblQuantity.AutoSize = True
        Me.Controls.Add(lblQuantity)

        txtQuantity = New TextBox()
        txtQuantity.Font = New Font("Poppins", 10)
        txtQuantity.Location = New Point(310, yPos + 30)
        txtQuantity.Size = New Size(260, 35)
        txtQuantity.BackColor = Color.FromArgb(61, 65, 66)
        txtQuantity.ForeColor = Color.White
        txtQuantity.BorderStyle = BorderStyle.FixedSingle
        Me.Controls.Add(txtQuantity)

        yPos += 90

        ' Batch Number (for ENDO products and Stock IN operations)
        Dim lblBatchNumber As New Label()
        lblBatchNumber.Text = "Batch Number"
        lblBatchNumber.Font = New Font("Poppins", 10, FontStyle.Bold)
        lblBatchNumber.ForeColor = Color.White
        lblBatchNumber.Location = New Point(30, yPos)
        lblBatchNumber.AutoSize = True
        lblBatchNumber.Name = "lblBatchNumber"
        Me.Controls.Add(lblBatchNumber)

        Dim txtBatchNumber As New TextBox()
        txtBatchNumber.Font = New Font("Poppins", 10)
        txtBatchNumber.Location = New Point(30, yPos + 30)
        txtBatchNumber.Size = New Size(260, 35)
        txtBatchNumber.BackColor = Color.FromArgb(61, 65, 66)
        txtBatchNumber.ForeColor = Color.White
        txtBatchNumber.BorderStyle = BorderStyle.FixedSingle
        txtBatchNumber.PlaceholderText = "e.g., BATCH-001"
        txtBatchNumber.Name = "txtBatchNumber"
        Me.Controls.Add(txtBatchNumber)

        ' Expiry Date (for ENDO products and Stock IN operations)
        Dim lblExpiryDate As New Label()
        lblExpiryDate.Text = "Expiry Date"
        lblExpiryDate.Font = New Font("Poppins", 10, FontStyle.Bold)
        lblExpiryDate.ForeColor = Color.White
        lblExpiryDate.Location = New Point(310, yPos)
        lblExpiryDate.AutoSize = True
        lblExpiryDate.Name = "lblExpiryDate"
        Me.Controls.Add(lblExpiryDate)

        Dim dtpExpiryDate As New DateTimePicker()
        dtpExpiryDate.Font = New Font("Poppins", 10)
        dtpExpiryDate.Location = New Point(310, yPos + 30)
        dtpExpiryDate.Size = New Size(260, 35)
        dtpExpiryDate.BackColor = Color.FromArgb(61, 65, 66)
        dtpExpiryDate.ForeColor = Color.White
        dtpExpiryDate.Format = DateTimePickerFormat.Short
        dtpExpiryDate.Value = DateTime.Now.AddYears(1) ' Default to 1 year from now
        dtpExpiryDate.Name = "dtpExpiryDate"
        Me.Controls.Add(dtpExpiryDate)

        ' Initially hide batch fields
        lblBatchNumber.Visible = False
        txtBatchNumber.Visible = False
        lblExpiryDate.Visible = False
        dtpExpiryDate.Visible = False

        yPos += 90

        ' Supplier (required)
        Dim lblSupplier As New Label()
        lblSupplier.Text = "Supplier *"
        lblSupplier.Font = New Font("Poppins", 10, FontStyle.Bold)
        lblSupplier.ForeColor = Color.White
        lblSupplier.Location = New Point(30, yPos)
        lblSupplier.AutoSize = True
        Me.Controls.Add(lblSupplier)

        cmbSupplier = New ComboBox()
        cmbSupplier.Font = New Font("Poppins", 10)
        cmbSupplier.Location = New Point(30, yPos + 30)
        cmbSupplier.Size = New Size(540, 35)
        cmbSupplier.DropDownStyle = ComboBoxStyle.DropDownList
        cmbSupplier.BackColor = Color.FromArgb(61, 65, 66)
        cmbSupplier.ForeColor = Color.White
        cmbSupplier.Items.Add("-- Select Supplier --")
        For Each supplier In suppliers
            cmbSupplier.Items.Add(supplier("SupplierName").ToString())
        Next
        cmbSupplier.Items.Add("Add New Supplier...")
        cmbSupplier.SelectedIndex = 0
        ' Add event handler for handling new supplier option
        AddHandler cmbSupplier.SelectedIndexChanged, AddressOf cmbSupplier_SelectedIndexChanged
        Me.Controls.Add(cmbSupplier)

        yPos += 90

        ' Reference (required)
        Dim lblReference As New Label()
        lblReference.Text = "Reference *"
        lblReference.Font = New Font("Poppins", 10, FontStyle.Bold)
        lblReference.ForeColor = Color.White
        lblReference.Location = New Point(30, yPos)
        lblReference.AutoSize = True
        Me.Controls.Add(lblReference)

        txtReference = New TextBox()
        txtReference.Font = New Font("Poppins", 10)
        txtReference.Location = New Point(30, yPos + 30)
        txtReference.Size = New Size(540, 35)
        txtReference.BackColor = Color.FromArgb(61, 65, 66)
        txtReference.ForeColor = Color.White
        txtReference.BorderStyle = BorderStyle.FixedSingle
        txtReference.PlaceholderText = "Purchase Order #, Invoice #, etc."
        Me.Controls.Add(txtReference)

        yPos += 90

        ' Notes
        Dim lblNotes As New Label()
        lblNotes.Text = "Notes"
        lblNotes.Font = New Font("Poppins", 10, FontStyle.Bold)
        lblNotes.ForeColor = Color.White
        lblNotes.Location = New Point(30, yPos)
        lblNotes.AutoSize = True
        Me.Controls.Add(lblNotes)

        txtNotes = New TextBox()
        txtNotes.Font = New Font("Poppins", 10)
        txtNotes.Location = New Point(30, yPos + 30)
        txtNotes.Size = New Size(540, 80)
        txtNotes.BackColor = Color.FromArgb(61, 65, 66)
        txtNotes.ForeColor = Color.White
        txtNotes.BorderStyle = BorderStyle.FixedSingle
        txtNotes.Multiline = True
        txtNotes.PlaceholderText = "Additional notes about this transaction..."
        Me.Controls.Add(txtNotes)

        yPos += 140

        ' Buttons
        Dim btnCancel As New Button()
        btnCancel.Text = "Cancel"
        btnCancel.Font = New Font("Poppins", 10)
        btnCancel.Location = New Point(350, yPos)
        btnCancel.Size = New Size(100, 40)
        btnCancel.BackColor = Color.FromArgb(60, 60, 60)
        btnCancel.ForeColor = Color.White
        btnCancel.FlatStyle = FlatStyle.Flat
        btnCancel.FlatAppearance.BorderSize = 0
        btnCancel.Cursor = Cursors.Hand
        AddHandler btnCancel.Click, Sub(s, ev) Me.Close()
        Me.Controls.Add(btnCancel)

        Dim btnSave As New Button()
        btnSave.Text = "Save Log"
        btnSave.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnSave.Location = New Point(470, yPos)
        btnSave.Size = New Size(100, 40)
        btnSave.BackColor = Color.White
        btnSave.ForeColor = Color.Black
        btnSave.FlatStyle = FlatStyle.Flat
        btnSave.FlatAppearance.BorderSize = 0
        btnSave.Cursor = Cursors.Hand
        AddHandler btnSave.Click, AddressOf SaveInventoryLog
        Me.Controls.Add(btnSave)
    End Sub

    ' Helper to populate combo with full products list (keeps product order in sync)
    Private Sub PopulateComboWithProducts(cb As ComboBox)
        cb.BeginUpdate()
        cb.Items.Clear()
        For Each p In products
            cb.Items.Add(p("ProductName").ToString())
        Next
        cb.EndUpdate()
    End Sub

    ' Event handlers for showing/hiding batch fields
    Private Sub cmbProduct_SelectedIndexChanged(sender As Object, e As EventArgs)
        UpdateBatchFieldsVisibility()
    End Sub

    Private Sub cmbTransactionType_SelectedIndexChanged(sender As Object, e As EventArgs)
        UpdateBatchFieldsVisibility()
    End Sub

    ' Event handler for supplier dropdown selection
    Private Sub cmbSupplier_SelectedIndexChanged(sender As Object, e As EventArgs)
        If cmbSupplier.SelectedItem IsNot Nothing Then
            Dim selectedSupplier As String = cmbSupplier.SelectedItem.ToString()

            If selectedSupplier = "Add New Supplier..." Then
                ' Show dialog to add new supplier
                AddNewSupplier()
            End If
        End If
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
        btnClose.Text = "?"
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
                                          ' Rebuild supplier dropdown
                                          cmbSupplier.Items.Clear()
                                          cmbSupplier.Items.Add("-- Select Supplier --")
                                          For Each supplier In suppliers
                                              cmbSupplier.Items.Add(supplier("SupplierName").ToString())
                                          Next
                                          cmbSupplier.Items.Add("Add New Supplier...")
                                          ' Select the newly added supplier
                                          cmbSupplier.SelectedItem = txtName.Text.Trim()
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
                                        cmbSupplier.SelectedIndex = 0 ' Reset to "-- Select Supplier --"
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

    Private Sub UpdateBatchFieldsVisibility()
        Try
            ' Get references to batch controls
            Dim lblBatchNumber As Label = Me.Controls.OfType(Of Label).FirstOrDefault(Function(c) c.Name = "lblBatchNumber")
            Dim txtBatchNumber As TextBox = Me.Controls.OfType(Of TextBox).FirstOrDefault(Function(c) c.Name = "txtBatchNumber")
            Dim lblExpiryDate As Label = Me.Controls.OfType(Of Label).FirstOrDefault(Function(c) c.Name = "lblExpiryDate")
            Dim dtpExpiryDate As DateTimePicker = Me.Controls.OfType(Of DateTimePicker).FirstOrDefault(Function(c) c.Name = "dtpExpiryDate")

            ' Show batch fields if:
            ' 1. Product is ENDO category AND
            ' 2. Transaction type is "IN" (Stock In)
            Dim shouldShowBatchFields As Boolean = False

            If cmbProduct.SelectedIndex >= 0 AndAlso cmbTransactionType.SelectedIndex >= 0 Then
                Dim selectedProduct = products(cmbProduct.SelectedIndex)
                Dim productCategory As String = selectedProduct("Category").ToString().ToUpper()
                Dim transactionType As String = cmbTransactionType.SelectedItem.ToString()

                ' Show batch fields for ENDO products during Stock IN operations
                shouldShowBatchFields = (productCategory = "ENDO" AndAlso transactionType = "IN")

                ' Auto-generate batch number if showing batch fields
                If shouldShowBatchFields AndAlso txtBatchNumber IsNot Nothing Then
                    Dim productId As Integer = Convert.ToInt32(selectedProduct("ProductID"))
                    Dim nextBatchNumber As String = GenerateNextBatchNumber(productId, productCategory)
                    txtBatchNumber.Text = nextBatchNumber
                    txtBatchNumber.ReadOnly = True ' Make it read-only since it's auto-generated
                End If
            End If

            ' Update visibility
            If lblBatchNumber IsNot Nothing Then lblBatchNumber.Visible = shouldShowBatchFields
            If txtBatchNumber IsNot Nothing Then
                txtBatchNumber.Visible = shouldShowBatchFields
                If Not shouldShowBatchFields Then
                    txtBatchNumber.Text = "" ' Clear when hidden
                    txtBatchNumber.ReadOnly = False
                End If
            End If
            If lblExpiryDate IsNot Nothing Then
                lblExpiryDate.Visible = shouldShowBatchFields
                If shouldShowBatchFields Then
                    lblExpiryDate.Text = "Expiry Date *" ' Make it required for ENDO
                    lblExpiryDate.ForeColor = Color.FromArgb(255, 100, 100) ' Light red to indicate required
                End If
            End If
            If dtpExpiryDate IsNot Nothing Then dtpExpiryDate.Visible = shouldShowBatchFields

        Catch ex As Exception
            ' Silent fail - batch fields will remain in their current state
        End Try
    End Sub

    Private Function GenerateNextBatchNumber(productId As Integer, productCategory As String) As String
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                ' Get the highest batch number for this product
                Dim query As String = "SELECT MAX(BatchNumber) FROM InventoryLog WHERE ProductID = @ProductID AND BatchNumber IS NOT NULL"
                Using cmd As New SqlCommand(query, conn)
                    cmd.Parameters.AddWithValue("@ProductID", productId)

                    Dim lastBatch As String = cmd.ExecuteScalar()?.ToString()

                    If String.IsNullOrWhiteSpace(lastBatch) Then
                        ' First batch for this product
                        Return $"{productCategory}-BATCH-001"
                    Else
                        ' Extract the number from the last batch and increment
                        Dim batchParts() As String = lastBatch.Split("-"c)
                        If batchParts.Length >= 3 Then
                            Dim lastNumber As Integer
                            If Integer.TryParse(batchParts(batchParts.Length - 1), lastNumber) Then
                                Dim nextNumber As Integer = lastNumber + 1
                                Return $"{productCategory}-BATCH-{nextNumber:D3}"
                            End If
                        End If

                        ' Fallback: if we can't parse, start from 001
                        Return $"{productCategory}-BATCH-001"
                    End If
                End Using
            End Using
        Catch ex As Exception
            ' Fallback batch number generation
            Return $"{productCategory}-BATCH-{DateTime.Now:yyyyMMdd}-001"
        End Try
    End Function

    Private Function ValidateInputs() As Boolean
        If cmbProduct.SelectedIndex = -1 Then
            MessageBox.Show("Please select a product!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            cmbProduct.Focus()
            Return False
        End If

        If cmbTransactionType.SelectedIndex = -1 Then
            MessageBox.Show("Please select a transaction type!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            cmbTransactionType.Focus()
            Return False
        End If

        Dim quantity As Integer
        If String.IsNullOrWhiteSpace(txtQuantity.Text) OrElse Not Integer.TryParse(txtQuantity.Text, quantity) OrElse quantity <= 0 Then
            MessageBox.Show("Please enter a valid quantity (greater than 0)!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtQuantity.Focus()
            Return False
        End If

        ' Validate supplier selection (now required)
        If cmbSupplier.SelectedIndex <= 0 OrElse cmbSupplier.SelectedItem.ToString() = "-- Select Supplier --" OrElse cmbSupplier.SelectedItem.ToString() = "Add New Supplier..." Then
            MessageBox.Show("Please select a supplier!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            cmbSupplier.Focus()
            Return False
        End If

        ' Validate reference (required)
        If String.IsNullOrWhiteSpace(txtReference.Text) Then
            MessageBox.Show("Please enter a reference!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            txtReference.Focus()
            Return False
        End If

        ' Require notes for Stock OUT transactions
        Dim transactionType As String = If(cmbTransactionType.SelectedItem, "").ToString().Trim().ToUpperInvariant()
        If transactionType = "OUT" Then
            If String.IsNullOrWhiteSpace(txtNotes.Text) Then
                MessageBox.Show("Notes are required for Stock OUT transactions!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                txtNotes.Focus()
                Return False
            End If
        End If

        ' Validate batch fields for ENDO products during Stock IN
        Dim selectedProduct = products(cmbProduct.SelectedIndex)
        Dim productCategory As String = selectedProduct("Category").ToString().ToUpper()

        If productCategory = "ENDO" AndAlso transactionType = "IN" Then
            ' Get batch controls
            Dim txtBatchNumber As TextBox = Me.Controls.OfType(Of TextBox).FirstOrDefault(Function(c) c.Name = "txtBatchNumber")
            Dim dtpExpiryDate As DateTimePicker = Me.Controls.OfType(Of DateTimePicker).FirstOrDefault(Function(c) c.Name = "dtpExpiryDate")

            ' Validate batch number
            If txtBatchNumber IsNot Nothing AndAlso String.IsNullOrWhiteSpace(txtBatchNumber.Text) Then
                MessageBox.Show("Batch number is required for ENDO products!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                txtBatchNumber.Focus()
                Return False
            End If

            ' Validate expiry date
            If dtpExpiryDate IsNot Nothing AndAlso dtpExpiryDate.Value.Date <= DateTime.Now.Date Then
                MessageBox.Show("Expiry date must be in the future for ENDO products!", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                dtpExpiryDate.Focus()
                Return False
            End If
        End If

        Return True
    End Function
    Private Sub SaveInventoryLog(sender As Object, e As EventArgs)
        Try
            ' Validate inputs
            If Not ValidateInputs() Then Return

            ' Get selected product
            Dim selectedProduct = products(cmbProduct.SelectedIndex)
            Dim productId = Convert.ToInt32(selectedProduct("ProductID"))
            Dim currentStock = Convert.ToInt32(selectedProduct("CurrentStock"))
            Dim productCategory As String = selectedProduct("Category").ToString().ToUpper()

            ' Calculate new stock
            Dim quantity = Convert.ToInt32(txtQuantity.Text)
            Dim newStock = currentStock

            Select Case cmbTransactionType.SelectedItem.ToString()
                Case "IN"
                    newStock = currentStock + quantity
                Case "OUT"
                    newStock = currentStock - quantity
                    If newStock < 0 Then
                        MessageBox.Show("Insufficient stock! Current stock: " & currentStock, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Return
                    End If
                Case "ADJUST"
                    newStock = quantity ' For adjustments, quantity is the new stock level
            End Select

            ' Get supplier ID (now required)
            Dim supplierId As Object = DBNull.Value
            If cmbSupplier.SelectedIndex > 0 AndAlso cmbSupplier.SelectedItem.ToString() <> "-- Select Supplier --" AndAlso cmbSupplier.SelectedItem.ToString() <> "Add New Supplier..." Then
                supplierId = suppliers(cmbSupplier.SelectedIndex - 1)("SupplierID")
            End If

            ' Get batch information for ENDO products during Stock IN
            Dim batchNumber As String = Nothing
            Dim expiryDate As DateTime? = Nothing

            If productCategory = "ENDO" AndAlso cmbTransactionType.SelectedItem.ToString() = "IN" Then
                Dim txtBatchNumber As TextBox = Me.Controls.OfType(Of TextBox).FirstOrDefault(Function(c) c.Name = "txtBatchNumber")
                Dim dtpExpiryDate As DateTimePicker = Me.Controls.OfType(Of DateTimePicker).FirstOrDefault(Function(c) c.Name = "dtpExpiryDate")

                If txtBatchNumber IsNot Nothing Then batchNumber = txtBatchNumber.Text.Trim()
                If dtpExpiryDate IsNot Nothing Then expiryDate = dtpExpiryDate.Value.Date
            End If

            ' Save to database
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        ' Insert inventory log with batch information
                        Dim logQuery = "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, BatchNumber, ExpiryDate, SupplierID, UserID, Reference, Notes, CreatedAt) " &
                                  "VALUES (@ProductID, @TransactionType, @Quantity, @PreviousStock, @NewStock, @BatchNumber, @ExpiryDate, @SupplierID, @UserID, @Reference, @Notes, GETDATE())"

                        Using cmd As New SqlCommand(logQuery, conn, transaction)
                            cmd.Parameters.AddWithValue("@ProductID", productId)
                            cmd.Parameters.AddWithValue("@TransactionType", cmbTransactionType.SelectedItem.ToString())
                            cmd.Parameters.AddWithValue("@Quantity", quantity)
                            cmd.Parameters.AddWithValue("@PreviousStock", currentStock)
                            cmd.Parameters.AddWithValue("@NewStock", newStock)
                            cmd.Parameters.AddWithValue("@BatchNumber", If(String.IsNullOrWhiteSpace(batchNumber), DBNull.Value, batchNumber))
                            cmd.Parameters.AddWithValue("@ExpiryDate", If(expiryDate.HasValue, expiryDate.Value, DBNull.Value))
                            cmd.Parameters.AddWithValue("@SupplierID", supplierId)
                            cmd.Parameters.AddWithValue("@UserID", frmLoginvb.LoggedInUserID)
                            cmd.Parameters.AddWithValue("@Reference", txtReference.Text.Trim())
                            cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(txtNotes.Text), DBNull.Value, txtNotes.Text.Trim()))
                            cmd.ExecuteNonQuery()
                        End Using

                        ' Update product stock
                        Dim updateQuery = "UPDATE Products SET CurrentStock = @NewStock, UpdatedAt = GETDATE() WHERE ProductID = @ProductID"
                        Using cmd As New SqlCommand(updateQuery, conn, transaction)
                            cmd.Parameters.AddWithValue("@NewStock", newStock)
                            cmd.Parameters.AddWithValue("@ProductID", productId)
                            cmd.ExecuteNonQuery()
                        End Using

                        transaction.Commit()
                        MessageBox.Show("Inventory log saved successfully!" &
                                  If(Not String.IsNullOrWhiteSpace(batchNumber), Environment.NewLine & $"Batch: {batchNumber}", ""),
                                  "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

                        Me.DialogResult = DialogResult.OK
                        Me.Close()

                    Catch ex As Exception
                        transaction.Rollback()
                        Throw ex
                    End Try
                End Using
            End Using

        Catch ex As Exception
            MessageBox.Show("Error saving inventory log: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Form closing event to stop idle timeout monitoring
    Private Sub AddInventoryLogForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Stop idle timeout monitoring when form closes
        IdleTimeoutManager.Instance.StopMonitoring(Me)
    End Sub

    ' Replace the existing cmbProduct_TextChanged with this implementation.
    Private Sub cmbProduct_TextChanged(sender As Object, e As EventArgs)
        Try
            If suppressProductTextChanged Then Return

            Dim cb = CType(sender, ComboBox)
            Dim originalText As String = If(cb.Text, "")
            Dim caretPos As Integer = Math.Max(0, Math.Min(cb.SelectionStart, originalText.Length))
            Dim input = originalText.Trim()

            ' Build matches
            Dim matches As New List(Of String)
            If String.IsNullOrEmpty(input) Then
                For Each p In products
                    matches.Add(p("ProductName").ToString())
                Next
            Else
                For Each p In products
                    Dim name = p("ProductName").ToString()
                    If name.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0 Then
                        matches.Add(name)
                    End If
                Next
            End If

            ' Update items without altering the user's typed text
            suppressProductTextChanged = True
            cb.BeginUpdate()
            cb.Items.Clear()
            For Each m In matches
                cb.Items.Add(m)
            Next
            cb.EndUpdate()

            cb.DroppedDown = (matches.Count > 0)

            ' Restore typed text and caret
            cb.Text = originalText
            cb.SelectionStart = Math.Min(caretPos, cb.Text.Length)
            cb.SelectionLength = 0
        Finally
            suppressProductTextChanged = False
        End Try
    End Sub

    ' Update KeyDown to accept suggestion on Tab (or Enter) and set SelectedIndex to the true product index
    Private Sub cmbProduct_KeyDown(sender As Object, e As KeyEventArgs)
        Try
            Dim cb = CType(sender, ComboBox)

            Select Case e.KeyCode
                Case Keys.Down
                    If cb.Items.Count > 0 Then cb.DroppedDown = True
                Case Keys.Escape
                    cb.DroppedDown = False
                Case Keys.Enter, Keys.Tab
                    If cb.Items.Count > 0 Then
                        ' Determine the suggestion to accept (highlighted in dropdown or first)
                        Dim suggestedText As String = Nothing
                        If cb.SelectedItem IsNot Nothing Then
                            suggestedText = cb.SelectedItem.ToString()
                        ElseIf cb.Items.Count > 0 Then
                            suggestedText = cb.Items(0).ToString()
                        End If

                        If Not String.IsNullOrEmpty(suggestedText) Then
                            ' Find product index in the master list
                            Dim productIndex As Integer = -1
                            For idx As Integer = 0 To products.Count - 1
                                If String.Equals(products(idx)("ProductName").ToString(), suggestedText, StringComparison.OrdinalIgnoreCase) Then
                                    productIndex = idx
                                    Exit For
                                End If
                            Next

                            If productIndex >= 0 Then
                                ' Restore full list and select the correct product index so downstream code uses the right index
                                suppressProductTextChanged = True
                                PopulateComboWithProducts(cb)
                                cb.SelectedIndex = productIndex
                                cb.DroppedDown = False
                                suppressProductTextChanged = False

                                ' Move focus to next control when Tab/Enter accepted
                                e.Handled = True
                                e.SuppressKeyPress = True
                                If e.KeyCode = Keys.Tab Then
                                    Me.SelectNextControl(cb, True, True, True, True)
                                End If
                            End If
                        End If
                    End If
            End Select
        Catch
            ' ignore
        End Try
    End Sub

    Private Sub cmbProduct_Leave(sender As Object, e As EventArgs)
        Try
            Dim cb = CType(sender, ComboBox)
            Dim typed = If(cb.Text, "").Trim()
            If String.IsNullOrEmpty(typed) Then
                cb.SelectedIndex = -1
                Return
            End If

            ' Try to find exact match in the full products list (case-insensitive)
            For idx As Integer = 0 To products.Count - 1
                If String.Equals(products(idx)("ProductName").ToString(), typed, StringComparison.OrdinalIgnoreCase) Then
                    ' Restore full list then select the exact product position
                    cb.BeginUpdate()
                    cb.Items.Clear()
                    For Each p In products
                        cb.Items.Add(p("ProductName").ToString())
                    Next
                    cb.EndUpdate()
                    cb.SelectedIndex = idx
                    Return
                End If
            Next

            ' If not found, leave SelectedIndex as -1 so validation will catch it
            cb.SelectedIndex = -1
        Catch
            ' ignore
        End Try
    End Sub


End Class