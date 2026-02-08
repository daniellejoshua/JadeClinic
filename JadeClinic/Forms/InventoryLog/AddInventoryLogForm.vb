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
        Me.Size = New Size(600, 700)

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

                Dim query As String = "SELECT ProductID, ProductName, CurrentStock FROM Products WHERE IsActive = 1 ORDER BY ProductName"
                Using cmd As New SqlCommand(query, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        products.Clear()

                        While reader.Read()
                            Dim productData As New Dictionary(Of String, Object) From {
                                {"ProductID", reader("ProductID")},
                                {"ProductName", reader("ProductName").ToString()},
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
        cmbTransactionType.Items.AddRange(New String() {"Stock In", "Stock Out", "Adjustments"})
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

    Private Sub SaveInventoryLog(sender As Object, e As EventArgs)
        Try
            ' Validate inputs
            If Not ValidateInputs() Then Return

            ' Get selected product
            Dim selectedProduct = products(cmbProduct.SelectedIndex)
            Dim productId = Convert.ToInt32(selectedProduct("ProductID"))
            Dim currentStock = Convert.ToInt32(selectedProduct("CurrentStock"))

            ' Calculate new stock
            Dim quantity = Convert.ToInt32(txtQuantity.Text)
            Dim newStock = currentStock
            
            Select Case cmbTransactionType.SelectedItem.ToString()
                Case "Stock In", "Adjustments"
                    newStock = currentStock + quantity
                Case "Stock Out"
                    newStock = currentStock - quantity
                    If newStock < 0 Then
                        MessageBox.Show("Insufficient stock! Current stock: " & currentStock, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Return
                    End If
            End Select

            ' Get supplier ID if selected
            Dim supplierId As Object = DBNull.Value
            If cmbSupplier.SelectedIndex > 0 Then
                supplierId = suppliers(cmbSupplier.SelectedIndex - 1)("SupplierID")
            End If

            ' Save to database
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using transaction = conn.BeginTransaction()
                    Try
                        ' Insert inventory log
                        Dim logQuery = "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, SupplierID, Reference, Notes, CreatedAt) " &
                                      "VALUES (@ProductID, @TransactionType, @Quantity, @PreviousStock, @NewStock, @SupplierID, @Reference, @Notes, GETDATE())"

                        Using cmd As New SqlCommand(logQuery, conn, transaction)
                            cmd.Parameters.AddWithValue("@ProductID", productId)
                            cmd.Parameters.AddWithValue("@TransactionType", cmbTransactionType.SelectedItem.ToString())
                            cmd.Parameters.AddWithValue("@Quantity", quantity)
                            cmd.Parameters.AddWithValue("@PreviousStock", currentStock)
                            cmd.Parameters.AddWithValue("@NewStock", newStock)
                            cmd.Parameters.AddWithValue("@SupplierID", supplierId)
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
                        MessageBox.Show("Inventory log saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                        
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

        Return True
    End Function
End Class