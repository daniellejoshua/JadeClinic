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
        btnClose.Text = "?"
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

        cmbProduct = New ComboBox()
        cmbProduct.Font = New Font("Poppins", 10)
        cmbProduct.Location = New Point(30, yPos + 30)
        cmbProduct.Size = New Size(540, 35)
        cmbProduct.DropDownStyle = ComboBoxStyle.DropDownList
        cmbProduct.BackColor = Color.FromArgb(61, 65, 66)
        cmbProduct.ForeColor = Color.White
        For Each product In products
            cmbProduct.Items.Add(product("ProductName").ToString())
        Next
        ' Add event handler to show/hide batch fields based on product category
        AddHandler cmbProduct.SelectedIndexChanged, AddressOf cmbProduct_SelectedIndexChanged
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
        cmbTransactionType.Items.AddRange(New String() {"IN", "OUT", "ADJUST"})
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

        ' Supplier (optional)
        Dim lblSupplier As New Label()
        lblSupplier.Text = "Supplier (Optional)"
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
        cmbSupplier.SelectedIndex = 0
        Me.Controls.Add(cmbSupplier)

        yPos += 90

        ' Reference
        Dim lblReference As New Label()
        lblReference.Text = "Reference"
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

    ' Event handlers for showing/hiding batch fields
    Private Sub cmbProduct_SelectedIndexChanged(sender As Object, e As EventArgs)
        UpdateBatchFieldsVisibility()
    End Sub

    Private Sub cmbTransactionType_SelectedIndexChanged(sender As Object, e As EventArgs)
        UpdateBatchFieldsVisibility()
    End Sub

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

        ' Validate batch fields for ENDO products during Stock IN
        Dim selectedProduct = products(cmbProduct.SelectedIndex)
        Dim productCategory As String = selectedProduct("Category").ToString().ToUpper()
        Dim transactionType As String = cmbTransactionType.SelectedItem.ToString()
        
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

            ' Get supplier ID if selected
            Dim supplierId As Object = DBNull.Value
            If cmbSupplier.SelectedIndex > 0 Then
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
                            cmd.Parameters.AddWithValue("@Reference", If(String.IsNullOrWhiteSpace(txtReference.Text), DBNull.Value, txtReference.Text.Trim()))
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
End Class