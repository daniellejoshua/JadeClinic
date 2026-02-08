Imports Microsoft.Data.SqlClient
Imports System.Data
Imports System.IO
Imports System.Threading.Tasks

Public Class InventoryLog
    Private allLogs As New List(Of Dictionary(Of String, Object))

    ' Loading panel fields
    Private loadingPanel As Panel
    Private loadingLabel As Label
    Private selectedDate As DateTime? = Nothing

    ' Navigation flag for proper form closing
    Private isNavigating As Boolean = False

    Public Sub New()
        InitializeComponent()

        ' Initialize loadingPanel
        loadingPanel = New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = System.Drawing.Color.FromArgb(128, 0, 0, 0),
            .Visible = False
        }
        loadingLabel = New Label With {
            .Text = "Loading Inventory Logs...",
            .ForeColor = System.Drawing.Color.White,
            .Font = New Font("Poppins", 16),
            .AutoSize = True,
            .BackColor = System.Drawing.Color.Transparent
        }
        loadingPanel.Controls.Add(loadingLabel)
        Me.Controls.Add(loadingPanel)

        AddHandler loadingPanel.SizeChanged, Sub()
                                                 loadingLabel.Location = New Point((loadingPanel.Width - loadingLabel.Width) \ 2, (loadingPanel.Height - loadingLabel.Height) \ 2)
                                             End Sub

        loadingPanel.BringToFront()
    End Sub

    Private Async Sub InventoryLog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            ' Setup form
            SetupForm()

            ' Show loading panel
            If loadingPanel IsNot Nothing Then
                loadingPanel.Visible = True
                loadingPanel.BringToFront()
                Await Task.Delay(200) ' Let UI render the overlay
            End If

            ' Setup controls
            SetupControls()

            ' Load data
            Await LoadInventoryLogsAsync()

            ' Hide loading panel
            If loadingPanel IsNot Nothing Then
                loadingPanel.Visible = False
            End If

        Catch ex As Exception
            ' Hide loading panel in case of error
            If loadingPanel IsNot Nothing Then
                loadingPanel.Visible = False
            End If
            MessageBox.Show($"Error initializing InventoryLog form: {ex.Message}{vbCrLf}{vbCrLf}Stack Trace: {ex.StackTrace}", "Initialization Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetupForm()
        ' Make form non-resizable
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.MinimumSize = Me.Size
        Me.MaximumSize = Me.Size

        ' Set double buffering for better performance
        SetDoubleBuffered(InventoryLogDataGrid)
    End Sub

    Private Sub SetDoubleBuffered(ctrl As Control)
        Try
            Dim prop = ctrl.GetType().GetProperty("DoubleBuffered", Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic)
            If prop IsNot Nothing Then prop.SetValue(ctrl, True, Nothing)
        Catch ex As Exception
            ' Silent fail for double buffering
        End Try
    End Sub

    Private Sub SetupControls()
        Try
            ' Setup Sort ComboBox - check if control exists first
            If SortBy IsNot Nothing Then
                SortBy.Items.Clear()
                SortBy.Items.AddRange(New String() {"Date (Newest First)", "Date (Oldest First)", "Product (A-Z)", "Product (Z-A)", "Transaction Type", "Quantity (High to Low)", "Quantity (Low to High)"})
                SortBy.SelectedIndex = 0
            End If

            ' Setup DataGrid
            SetupDataGrid()

            ' Setup date filter - default to today - check if control exists first
            If Guna2DateTimePicker1 IsNot Nothing Then
                Guna2DateTimePicker1.Value = DateTime.Now.Date
                selectedDate = DateTime.Today
            End If

            ' Setup events - only if controls exist
            If SortBy IsNot Nothing Then
            End If

            If Guna2DateTimePicker1 IsNot Nothing Then
            End If

            If AddInventoryLog IsNot Nothing Then
                AddHandler AddInventoryLog.Click, AddressOf AddInventoryLog_Click
                ' Make add button visible
                AddInventoryLog.Visible = True
            End If

        Catch ex As Exception
            MessageBox.Show($"Error setting up controls: {ex.Message}", "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetupDataGrid()
        Try
            ' Check if the DataGrid control exists first
            If InventoryLogDataGrid Is Nothing Then
                Throw New InvalidOperationException("InventoryLogDataGrid control is not initialized")
            End If

            ' Clear existing columns
            InventoryLogDataGrid.Columns.Clear()

            ' Configure DataGridView appearance with consistent gray colors and white row separators
            InventoryLogDataGrid.BackgroundColor = System.Drawing.Color.FromArgb(41, 44, 45)
            InventoryLogDataGrid.GridColor = System.Drawing.Color.White ' Thin white line as row separator
            InventoryLogDataGrid.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66) ' Consistent gray for all rows
            InventoryLogDataGrid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66) ' Match odd and even rows
            InventoryLogDataGrid.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            InventoryLogDataGrid.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 204, 77)
            InventoryLogDataGrid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
            InventoryLogDataGrid.DefaultCellStyle.Font = New System.Drawing.Font("Poppins", 9.0F, System.Drawing.FontStyle.Regular)
            InventoryLogDataGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

            ' Configure header style with gray colors
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(30, 30, 30)
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.Font = New System.Drawing.Font("Poppins SemiBold", 10.0F, System.Drawing.FontStyle.Regular)
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            InventoryLogDataGrid.ColumnHeadersHeight = 50
            InventoryLogDataGrid.RowTemplate.Height = 60

            ' Ensure row borders are visible
            InventoryLogDataGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal

            ' Setup DataGrid properties
            InventoryLogDataGrid.AutoGenerateColumns = False
            InventoryLogDataGrid.AllowUserToAddRows = False
            InventoryLogDataGrid.AllowUserToDeleteRows = False
            InventoryLogDataGrid.ReadOnly = True
            InventoryLogDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            InventoryLogDataGrid.MultiSelect = False
            InventoryLogDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            InventoryLogDataGrid.ScrollBars = ScrollBars.Vertical

            ' Prevent resizing of all columns and rows
            InventoryLogDataGrid.AllowUserToResizeColumns = False
            InventoryLogDataGrid.AllowUserToResizeRows = False
            InventoryLogDataGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
            InventoryLogDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing

            ' Add Log ID column
            Dim colLogID As New DataGridViewTextBoxColumn()
            colLogID.Name = "LogID"
            colLogID.HeaderText = "ID"
            colLogID.ReadOnly = True
            colLogID.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colLogID)

            ' Add Product Name column
            Dim colProductName As New DataGridViewTextBoxColumn()
            colProductName.Name = "ProductName"
            colProductName.HeaderText = "Product"
            colProductName.ReadOnly = True
            colProductName.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colProductName)

            ' Add Transaction Type column
            Dim colTransactionType As New DataGridViewTextBoxColumn()
            colTransactionType.Name = "TransactionType"
            colTransactionType.HeaderText = "Type"
            colTransactionType.ReadOnly = True
            colTransactionType.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colTransactionType)

            ' Add Quantity column
            Dim colQuantity As New DataGridViewTextBoxColumn()
            colQuantity.Name = "Quantity"
            colQuantity.HeaderText = "Quantity"
            colQuantity.ReadOnly = True
            colQuantity.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colQuantity)

            ' Add Previous Stock column
            Dim colPreviousStock As New DataGridViewTextBoxColumn()
            colPreviousStock.Name = "PreviousStock"
            colPreviousStock.HeaderText = "Previous Stock"
            colPreviousStock.ReadOnly = True
            colPreviousStock.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colPreviousStock)

            ' Add New Stock column
            Dim colNewStock As New DataGridViewTextBoxColumn()
            colNewStock.Name = "NewStock"
            colNewStock.HeaderText = "New Stock"
            colNewStock.ReadOnly = True
            colNewStock.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colNewStock)

            ' Add Supplier column
            Dim colSupplierName As New DataGridViewTextBoxColumn()
            colSupplierName.Name = "SupplierName"
            colSupplierName.HeaderText = "Supplier"
            colSupplierName.ReadOnly = True
            colSupplierName.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colSupplierName)

            ' Add Reference column
            Dim colReference As New DataGridViewTextBoxColumn()
            colReference.Name = "Reference"
            colReference.HeaderText = "Reference"
            colReference.ReadOnly = True
            colReference.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colReference)

            ' Add Notes column
            Dim colNotes As New DataGridViewTextBoxColumn()
            colNotes.Name = "Notes"
            colNotes.HeaderText = "Notes"
            colNotes.ReadOnly = True
            colNotes.DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .WrapMode = DataGridViewTriState.True
            }
            InventoryLogDataGrid.Columns.Add(colNotes)

            ' Add Date & Time column
            Dim colCreatedAt As New DataGridViewTextBoxColumn()
            colCreatedAt.Name = "CreatedAt"
            colCreatedAt.HeaderText = "Date & Time"
            colCreatedAt.ReadOnly = True
            colCreatedAt.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colCreatedAt)

            ' Add Transaction Indicator column
            Dim colTransactionIndicator As New DataGridViewTextBoxColumn()
            colTransactionIndicator.Name = "TransactionIndicator"
            colTransactionIndicator.HeaderText = "Status"
            colTransactionIndicator.ReadOnly = True
            colTransactionIndicator.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colTransactionIndicator)

        Catch ex As Exception
            MessageBox.Show($"Error setting up DataGrid: {ex.Message}", "DataGrid Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Function LoadInventoryLogsAsync() As Task
        Try
            ' Load data in background thread
            Dim inventoryData = Await Task.Run(Function() GetInventoryLogsData("Date (Newest First)", selectedDate))

            ' Update UI on main thread
            LoadInventoryLogsDataOnUI(inventoryData)

        Catch ex As Exception
            MessageBox.Show("Error loading inventory logs: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Function

    Private Function GetInventoryLogsData(sortOrder As String, Optional filterDate As DateTime? = Nothing) As List(Of Dictionary(Of String, Object))
        Dim inventoryLogs As New List(Of Dictionary(Of String, Object))()
        Dim query As String = "SELECT il.LogID, il.ProductID, p.ProductName, il.TransactionType, " &
                             "il.Quantity, il.PreviousStock, il.NewStock, s.SupplierName, " &
                             "il.Reference, il.Notes, il.CreatedAt " &
                             "FROM InventoryLog il " &
                             "INNER JOIN Products p ON il.ProductID = p.ProductID " &
                             "LEFT JOIN Suppliers s ON il.SupplierID = s.SupplierID"

        Dim whereClauses As New List(Of String)()
        Dim parameters As New List(Of SqlParameter)()

        ' Add date filter if provided
        If filterDate.HasValue Then
            whereClauses.Add("CAST(il.CreatedAt AS DATE) = @FilterDate")
            parameters.Add(New SqlParameter("@FilterDate", filterDate.Value.Date))
        End If

        ' Build WHERE clause
        If whereClauses.Count > 0 Then
            query += " WHERE " & String.Join(" AND ", whereClauses)
        End If

        ' Add sorting based on sort selection
        Select Case sortOrder
            Case "Date (Newest First)"
                query += " ORDER BY il.CreatedAt DESC"
            Case "Date (Oldest First)"
                query += " ORDER BY il.CreatedAt ASC"
            Case "Product (A-Z)"
                query += " ORDER BY p.ProductName ASC, il.CreatedAt DESC"
            Case "Product (Z-A)"
                query += " ORDER BY p.ProductName DESC, il.CreatedAt DESC"
            Case "Transaction Type"
                query += " ORDER BY il.TransactionType ASC, il.CreatedAt DESC"
            Case "Quantity (High to Low)"
                query += " ORDER BY il.Quantity DESC, il.CreatedAt DESC"
            Case "Quantity (Low to High)"
                query += " ORDER BY il.Quantity ASC, il.CreatedAt DESC"
            Case Else
                query += " ORDER BY il.CreatedAt DESC" ' Default sorting
        End Select

        Try
            Dim connStr As String = Connection.GetConnectionString()
            If String.IsNullOrEmpty(connStr) Then
                inventoryLogs.Add(New Dictionary(Of String, Object) From {{"Error", "Database connection string is not configured"}})
                Return inventoryLogs
            End If

            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using cmd As New SqlCommand(query, conn)
                    ' Add parameters
                    For Each param In parameters
                        cmd.Parameters.Add(param)
                    Next

                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim logData As New Dictionary(Of String, Object) From {
                                {"LogID", If(IsDBNull(reader("LogID")), 0, reader("LogID"))},
                                {"ProductID", If(IsDBNull(reader("ProductID")), 0, reader("ProductID"))},
                                {"ProductName", If(IsDBNull(reader("ProductName")), "", reader("ProductName").ToString())},
                                {"TransactionType", If(IsDBNull(reader("TransactionType")), "", reader("TransactionType").ToString())},
                                {"Quantity", If(IsDBNull(reader("Quantity")), 0, Convert.ToInt32(reader("Quantity")))},
                                {"PreviousStock", If(IsDBNull(reader("PreviousStock")), 0, Convert.ToInt32(reader("PreviousStock")))},
                                {"NewStock", If(IsDBNull(reader("NewStock")), 0, Convert.ToInt32(reader("NewStock")))},
                                {"SupplierName", If(IsDBNull(reader("SupplierName")), "", reader("SupplierName").ToString())},
                                {"Reference", If(IsDBNull(reader("Reference")), "", reader("Reference").ToString())},
                                {"Notes", If(IsDBNull(reader("Notes")), "", reader("Notes").ToString())},
                                {"CreatedAt", If(IsDBNull(reader("CreatedAt")), DateTime.Now, Convert.ToDateTime(reader("CreatedAt")))}
                            }
                            inventoryLogs.Add(logData)
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            ' Return error in the list with more detailed information
            inventoryLogs.Add(New Dictionary(Of String, Object) From {{"Error", $"Database error: {ex.Message}"}})
        End Try

        Return inventoryLogs
    End Function

    Private Sub LoadInventoryLogsDataOnUI(inventoryData As List(Of Dictionary(Of String, Object)))
        Try
            ' Check if DataGrid exists
            If InventoryLogDataGrid Is Nothing Then
                MessageBox.Show("DataGrid control is not available", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Ensure columns exist before adding rows
            If InventoryLogDataGrid.Columns.Count = 0 Then
                SetupDataGrid()
            End If

            ' Clear existing rows
            InventoryLogDataGrid.Rows.Clear()

            ' Check for error in data
            If inventoryData IsNot Nothing AndAlso inventoryData.Count > 0 AndAlso inventoryData(0).ContainsKey("Error") Then
                MessageBox.Show($"Error loading inventory logs: {inventoryData(0)("Error")}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            ' Handle empty data
            If inventoryData Is Nothing OrElse inventoryData.Count = 0 Then
                ' Update count for empty data
                If lblUsername IsNot Nothing Then
                    lblUsername.Text = "0 Records"
                End If
                Return
            End If

            ' Load data into DataGridView
            For Each logRecord In inventoryData
                If logRecord IsNot Nothing Then
                    Dim logId As Integer = If(logRecord.ContainsKey("LogID"), CInt(logRecord("LogID")), 0)
                    Dim productName As String = If(logRecord.ContainsKey("ProductName"), logRecord("ProductName").ToString(), "")
                    Dim transactionType As String = If(logRecord.ContainsKey("TransactionType"), logRecord("TransactionType").ToString(), "")
                    Dim quantity As Integer = If(logRecord.ContainsKey("Quantity"), CInt(logRecord("Quantity")), 0)
                    Dim previousStock As Integer = If(logRecord.ContainsKey("PreviousStock"), CInt(logRecord("PreviousStock")), 0)
                    Dim newStock As Integer = If(logRecord.ContainsKey("NewStock"), CInt(logRecord("NewStock")), 0)
                    Dim supplierName As String = If(logRecord.ContainsKey("SupplierName"), logRecord("SupplierName").ToString(), "")
                    Dim reference As String = If(logRecord.ContainsKey("Reference"), logRecord("Reference").ToString(), "")
                    Dim notes As String = If(logRecord.ContainsKey("Notes"), logRecord("Notes").ToString(), "")
                    Dim createdAt As DateTime = If(logRecord.ContainsKey("CreatedAt"), CDate(logRecord("CreatedAt")), DateTime.Now)

                    ' Add row to DataGridView
                    Dim rowIndex As Integer = InventoryLogDataGrid.Rows.Add()

                    ' Set individual column values safely
                    If InventoryLogDataGrid.Columns.Contains("LogID") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("LogID").Value = logId
                    End If
                    If InventoryLogDataGrid.Columns.Contains("ProductName") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("ProductName").Value = productName
                    End If
                    If InventoryLogDataGrid.Columns.Contains("TransactionType") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("TransactionType").Value = transactionType
                    End If
                    If InventoryLogDataGrid.Columns.Contains("Quantity") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("Quantity").Value = quantity
                    End If
                    If InventoryLogDataGrid.Columns.Contains("PreviousStock") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("PreviousStock").Value = previousStock
                    End If
                    If InventoryLogDataGrid.Columns.Contains("NewStock") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("NewStock").Value = newStock
                    End If
                    If InventoryLogDataGrid.Columns.Contains("SupplierName") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierName").Value = supplierName
                    End If
                    If InventoryLogDataGrid.Columns.Contains("Reference") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("Reference").Value = reference
                    End If
                    If InventoryLogDataGrid.Columns.Contains("Notes") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("Notes").Value = notes
                    End If
                    If InventoryLogDataGrid.Columns.Contains("CreatedAt") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("CreatedAt").Value = createdAt.ToString("MM/dd/yyyy HH:mm:ss")
                    End If

                    ' Determine transaction indicator and set color-coded indicators
                    Dim transactionIndicator As String = GetTransactionIndicator(transactionType)
                    If InventoryLogDataGrid.Columns.Contains("TransactionIndicator") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("TransactionIndicator").Value = transactionIndicator
                    End If

                    ' Set color coding based on transaction type
                    SetRowColorCoding(rowIndex, transactionType, transactionIndicator)

                    ' Store log data in row tag for potential future use
                    InventoryLogDataGrid.Rows(rowIndex).Tag = logRecord
                End If
            Next

            ' Update count
            If lblUsername IsNot Nothing Then
                lblUsername.Text = $"{inventoryData.Count} Records"
            End If

        Catch ex As Exception
            MessageBox.Show($"Error displaying inventory logs: {ex.Message}", "Display Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function GetTransactionIndicator(transactionType As String) As String
        Select Case transactionType.ToLower()
            Case "stock in"
                Return "📈 IN"
            Case "stock out"
                Return "📉 OUT"
            Case "adjustments"
                Return "⚖️ ADJ"
            Case Else
                Return "ℹ️ INFO"
        End Select
    End Function

    Private Sub SetRowColorCoding(rowIndex As Integer, transactionType As String, transactionIndicator As String)
        Try
            ' Check if the DataGrid and row exist
            If InventoryLogDataGrid Is Nothing OrElse rowIndex < 0 OrElse rowIndex >= InventoryLogDataGrid.Rows.Count Then
                Return
            End If

            Dim row = InventoryLogDataGrid.Rows(rowIndex)
            If row Is Nothing Then Return

            ' Set background color based on transaction type
            Select Case transactionType.ToLower()
                Case "stock in"
                    ' Stock In - Light green background
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(45, 70, 45)
                    If InventoryLogDataGrid.Columns.Contains("TransactionIndicator") AndAlso row.Cells("TransactionIndicator") IsNot Nothing Then
                        row.Cells("TransactionIndicator").Style.ForeColor = System.Drawing.Color.FromArgb(100, 255, 100)
                    End If
                Case "stock out"
                    ' Stock Out - Light red background
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(70, 45, 45)
                    If InventoryLogDataGrid.Columns.Contains("TransactionIndicator") AndAlso row.Cells("TransactionIndicator") IsNot Nothing Then
                        row.Cells("TransactionIndicator").Style.ForeColor = System.Drawing.Color.FromArgb(255, 100, 100)
                    End If
                Case "adjustments"
                    ' Adjustments - Light orange background
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(70, 55, 40)
                    If InventoryLogDataGrid.Columns.Contains("TransactionIndicator") AndAlso row.Cells("TransactionIndicator") IsNot Nothing Then
                        row.Cells("TransactionIndicator").Style.ForeColor = System.Drawing.Color.FromArgb(255, 180, 100)
                    End If
                Case Else
                    ' Default gray background
                    row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
                    If InventoryLogDataGrid.Columns.Contains("TransactionIndicator") AndAlso row.Cells("TransactionIndicator") IsNot Nothing Then
                        row.Cells("TransactionIndicator").Style.ForeColor = System.Drawing.Color.LightGray
                    End If
            End Select
        Catch ex As Exception
            ' Silent fail for color coding errors to prevent disrupting the main process
        End Try
    End Sub



    Private Async Sub Guna2DateTimePicker1_ValueChanged(sender As Object, e As EventArgs) Handles Guna2DateTimePicker1.ValueChanged
        selectedDate = Guna2DateTimePicker1.Value.Date
    End Sub

    ' Method to clear date filter (can be called from a clear filter button if needed)
    Public Async Sub ClearDateFilter()
        selectedDate = Nothing
        Guna2DateTimePicker1.Value = DateTime.Today
    End Sub

    Private Sub AddInventoryLog_Click(sender As Object, e As EventArgs)
        ' Create overlay panel for modal effect
        Dim overlayPanel As New Panel()
        overlayPanel.BackColor = Color.FromArgb(100, 0, 0, 0) ' Semi-transparent black
        overlayPanel.Dock = DockStyle.Fill
        overlayPanel.Location = New Point(0, 0)
        overlayPanel.Size = Me.ClientSize
        Me.Controls.Add(overlayPanel)
        overlayPanel.BringToFront()

        ' Create and show AddInventoryLog form
        Dim addLogForm As New AddInventoryLogForm()
        addLogForm.StartPosition = FormStartPosition.CenterParent

        ' Handle form closing to remove overlay
        AddHandler addLogForm.FormClosed, Sub(s, ev)
                                              If overlayPanel IsNot Nothing AndAlso Not overlayPanel.IsDisposed Then
                                                  Me.Controls.Remove(overlayPanel)
                                                  overlayPanel.Dispose()
                                              End If
                                          End Sub

        Dim result As DialogResult = addLogForm.ShowDialog(Me)

        ' Cleanup overlay if still exists
        If overlayPanel IsNot Nothing AndAlso Not overlayPanel.IsDisposed Then
            Me.Controls.Remove(overlayPanel)
            overlayPanel.Dispose()
        End If

        ' Refresh the logs if a new log was added
        If result = DialogResult.OK Then
            LoadInventoryLogsAsync()
        End If
    End Sub

    ' Navigation methods
    Private Sub InventoryLog_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' If this is programmatic navigation, don't show confirmation
        If isNavigating Then
            Return
        End If

        ' Add any cleanup code here if needed
    End Sub

    Private Sub Guna2CircleButton3_Click(sender As Object, e As EventArgs) Handles Guna2CircleButton3.Click
        isNavigating = True
        Inventory.Show()
        Me.Close()
    End Sub

    Private Sub Exportbtn_Click(sender As Object, e As EventArgs) Handles Exportbtn.Click
        ' Export functionality can be implemented later
        MessageBox.Show("Export functionality will be implemented soon.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub
End Class