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
            .BackColor = System.Drawing.Color.DarkGray,
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

            ' Validate user session
            If Not ValidateUserSession() Then
                Return
            End If

            ' Create navigation menu
            CreateNavigationMenu()

            ' Initialize profile section
            InitializeProfileSection()

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

            ' Update form title to show logged-in user
            Me.Text = $"Inventory Logs - {frmLoginvb.LoggedInUsername}"

            ' Start idle timeout monitoring
            IdleTimeoutManager.Instance.StartMonitoring(Me)

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
                AddHandler SortBy.SelectedIndexChanged, AddressOf SortBy_SelectedIndexChanged
            End If

            If Guna2DateTimePicker1 IsNot Nothing Then
                ' Add handler for date picker value changes are already handled above
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
            ' Validate control
            If InventoryLogDataGrid Is Nothing Then
                Throw New InvalidOperationException("InventoryLogDataGrid control is not initialized")
            End If

            ' Palette (match Inventory.vb where appropriate)
            Dim RichOlive As Color = Color.FromArgb(190, 154, 48)
            Dim GoldenYellow As Color = Color.FromArgb(254, 191, 16)
            Dim White As Color = Color.FromArgb(255, 255, 255)
            Dim LightGray As Color = Color.FromArgb(235, 235, 235) ' selection color (light gray)
            Dim DarkGray As Color = Color.FromArgb(80, 80, 80)
            Dim BorderGray As Color = Color.FromArgb(220, 220, 220) ' thin borders

            ' Clear and configure grid
            InventoryLogDataGrid.Columns.Clear()
            InventoryLogDataGrid.AutoGenerateColumns = False
            InventoryLogDataGrid.AllowUserToAddRows = False
            InventoryLogDataGrid.AllowUserToDeleteRows = False
            InventoryLogDataGrid.ReadOnly = True
            InventoryLogDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            InventoryLogDataGrid.MultiSelect = False
            InventoryLogDataGrid.ScrollBars = ScrollBars.Vertical
            InventoryLogDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            InventoryLogDataGrid.RowHeadersVisible = False
            InventoryLogDataGrid.EnableHeadersVisualStyles = False

            ' Background & general styling
            InventoryLogDataGrid.BackgroundColor = White
            InventoryLogDataGrid.GridColor = BorderGray
            InventoryLogDataGrid.BorderStyle = BorderStyle.None
            InventoryLogDataGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal

            ' Cell styling: default rows white, selection light gray
            InventoryLogDataGrid.DefaultCellStyle.BackColor = White
            InventoryLogDataGrid.AlternatingRowsDefaultCellStyle.BackColor = White
            InventoryLogDataGrid.DefaultCellStyle.ForeColor = DarkGray
            InventoryLogDataGrid.DefaultCellStyle.SelectionBackColor = LightGray
            InventoryLogDataGrid.DefaultCellStyle.SelectionForeColor = Color.Black
            InventoryLogDataGrid.DefaultCellStyle.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            InventoryLogDataGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            InventoryLogDataGrid.DefaultCellStyle.Padding = New Padding(8, 6, 8, 6)

            ' Header styling (keep RichOlive header)
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.BackColor = RichOlive
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = RichOlive
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.Font = New Font("Poppins SemiBold", 10.0F, FontStyle.Regular)
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            InventoryLogDataGrid.ColumnHeadersHeight = 48
            InventoryLogDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            InventoryLogDataGrid.RowTemplate.Height = 60

            ' Prevent resizing
            InventoryLogDataGrid.AllowUserToResizeColumns = False
            InventoryLogDataGrid.AllowUserToResizeRows = False
            InventoryLogDataGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing

            ' Add columns (keeps previous column definitions but with new styles)
            Dim colLogID As New DataGridViewTextBoxColumn()
            colLogID.Name = "LogID"
            colLogID.HeaderText = "ID"
            colLogID.ReadOnly = True
            colLogID.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colLogID)

            Dim colProductName As New DataGridViewTextBoxColumn()
            colProductName.Name = "ProductName"
            colProductName.HeaderText = "Product"
            colProductName.ReadOnly = True
            colProductName.DefaultCellStyle = New DataGridViewCellStyle() With {
            .Alignment = DataGridViewContentAlignment.MiddleLeft,
            .Padding = New Padding(10, 6, 10, 6),
            .Font = New Font("Poppins SemiBold", 9.0F, FontStyle.Regular),
            .ForeColor = DarkGray,
            .SelectionBackColor = LightGray,
            .SelectionForeColor = Color.Black
        }
            InventoryLogDataGrid.Columns.Add(colProductName)

            Dim colTransactionType As New DataGridViewTextBoxColumn()
            colTransactionType.Name = "TransactionType"
            colTransactionType.HeaderText = "Type"
            colTransactionType.ReadOnly = True
            colTransactionType.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colTransactionType)

            Dim colQuantity As New DataGridViewTextBoxColumn()
            colQuantity.Name = "Quantity"
            colQuantity.HeaderText = "Quantity"
            colQuantity.ReadOnly = True
            colQuantity.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colQuantity)

            Dim colPreviousStock As New DataGridViewTextBoxColumn()
            colPreviousStock.Name = "PreviousStock"
            colPreviousStock.HeaderText = "Previous Stock"
            colPreviousStock.ReadOnly = True
            colPreviousStock.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colPreviousStock)

            Dim colNewStock As New DataGridViewTextBoxColumn()
            colNewStock.Name = "NewStock"
            colNewStock.HeaderText = "New Stock"
            colNewStock.ReadOnly = True
            colNewStock.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colNewStock)

            Dim colSupplierName As New DataGridViewTextBoxColumn()
            colSupplierName.Name = "SupplierName"
            colSupplierName.HeaderText = "Supplier"
            colSupplierName.ReadOnly = True
            colSupplierName.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colSupplierName)

            Dim colReference As New DataGridViewTextBoxColumn()
            colReference.Name = "Reference"
            colReference.HeaderText = "Reference"
            colReference.ReadOnly = True
            colReference.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colReference)

            Dim colNotes As New DataGridViewTextBoxColumn()
            colNotes.Name = "Notes"
            colNotes.HeaderText = "Notes"
            colNotes.ReadOnly = True
            colNotes.DefaultCellStyle = New DataGridViewCellStyle() With {
            .Alignment = DataGridViewContentAlignment.MiddleCenter,
            .WrapMode = DataGridViewTriState.True
        }
            InventoryLogDataGrid.Columns.Add(colNotes)

            Dim colCreatedAt As New DataGridViewTextBoxColumn()
            colCreatedAt.Name = "CreatedAt"
            colCreatedAt.HeaderText = "Date & Time"
            colCreatedAt.ReadOnly = True
            colCreatedAt.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            InventoryLogDataGrid.Columns.Add(colCreatedAt)

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
            ' Show loading panel first with minimum display time
            If loadingPanel IsNot Nothing Then
                loadingPanel.Visible = True
                loadingPanel.BringToFront()
                Await Task.Delay(200) ' Let UI render the overlay
            End If

            ' Start timing to ensure minimum display duration
            Dim startTime As DateTime = DateTime.Now

            ' Get current sort order
            Dim sortOrder As String = If(SortBy?.SelectedItem?.ToString(), "Date (Newest First)")

            ' Load data in background thread
            Dim inventoryData = Await Task.Run(Function() GetInventoryLogsData(sortOrder, selectedDate))

            ' Ensure minimum loading display time of 1 second
            Dim elapsedMs As Integer = CInt((DateTime.Now - startTime).TotalMilliseconds)
            If elapsedMs < 1000 Then
                Await Task.Delay(1000 - elapsedMs)
            End If

            ' Update UI on main thread
            LoadInventoryLogsDataOnUI(inventoryData)

            ' Hide loading panel
            If loadingPanel IsNot Nothing Then
                loadingPanel.Visible = False
            End If

        Catch ex As Exception
            ' Hide loading panel in case of error
            If loadingPanel IsNot Nothing Then
                loadingPanel.Visible = False
            End If
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



    Private Async Sub Guna2DateTimePicker1_ValueChanged(sender As Object, e As EventArgs)
        Try
            selectedDate = Guna2DateTimePicker1.Value.Date
            Await LoadInventoryLogsAsync()
        Catch ex As Exception
            MessageBox.Show($"Error filtering by date: {ex.Message}", "Filter Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Method to clear date filter (can be called from a clear filter button if needed)
    Public Async Sub ClearDateFilter()
        selectedDate = Nothing
        Guna2DateTimePicker1.Value = DateTime.Today
    End Sub

    Private Async Sub AddInventoryLog_Click(sender As Object, e As EventArgs)
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
            Await LoadInventoryLogsAsync()
        End If
    End Sub

    Private Sub CreateNavigationMenu()
        Try
            ' Clear existing controls except PictureBox9 (logo)
            For i = DashboardPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = DashboardPanel.Controls(i)
                If TypeOf control IsNot PictureBox Then
                    DashboardPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            ' Set Navigation Panel Background to White
            DashboardPanel.FillColor = System.Drawing.Color.White

            ' Calculate available space (DashboardPanel is 236x885)
            Dim availableWidth As Integer = DashboardPanel.Width - 40 ' 20px margins on each side
            Dim availableHeight As Integer = DashboardPanel.Height - 160 ' Space for logo and title

            ' Logo area (keep existing PictureBox9)
            PictureBox9.BringToFront()

            ' Add title label - positioned below logo with Golden Yellow
            Dim titleLabel As New Label()
            titleLabel.Text = "JADE CLINIC"
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = System.Drawing.Color.FromArgb(254, 191, 16) ' Golden Yellow #FECF10
            titleLabel.BackColor = System.Drawing.Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New System.Drawing.Size(availableWidth, 30)
            titleLabel.Location = New Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(titleLabel)

            ' Subtitle with Dark Gray color (visible on white background)
            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Poppins", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100) ' Dark Gray for visibility on white
            subtitleLabel.BackColor = System.Drawing.Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New System.Drawing.Size(availableWidth, 25)
            subtitleLabel.Location = New Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(subtitleLabel)

            ' Navigation section separator with Light Gray (visible on white background)
            Dim separator1 As New Panel()
            separator1.BackColor = System.Drawing.Color.FromArgb(220, 220, 220) ' Light Gray for white background
            separator1.Size = New System.Drawing.Size(availableWidth - 20, 2)
            separator1.Location = New Point(30, 190)
            DashboardPanel.Controls.Add(separator1)

            ' Navigation section label with Dark Gray (visible on white background)
            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Poppins", 10, FontStyle.Bold)
            navLabel.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80) ' Dark Gray for visibility on white
            navLabel.BackColor = System.Drawing.Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New System.Drawing.Size(availableWidth, 25)
            navLabel.Location = New Point(20, 205)
            navLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(navLabel)

            ' Calculate button positioning for role-based navigation
            Dim startY As Integer = 250
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonWidth As Integer = availableWidth - 5
            Dim buttonIndex As Integer = 0

            ' Get current user role for navigation filtering
            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "Staff").ToUpper()

            ' Check if user has Manager or Admin access - INVENTORY LOGS REQUIRES MANAGER/ADMIN ACCESS
            If Not (currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR") Then
                ' Show access denied message and close form
                MessageBox.Show("Access denied. Only managers and administrators can access Inventory Logs.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                isNavigating = True
                Dashboard.Show()
                Me.Close()
                Return
            End If

            ' Create navigation buttons based on role (only Manager/Admin can reach here)
            ' Dashboard Button (not active)
            Dim navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
            buttonIndex += 1

            ' POS/Sales Button (all roles)
            Dim navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navPOSBtn.Click, AddressOf NavPOS_Click
            buttonIndex += 1

            ' Inventory Button (Manager and Admin only)
            Dim navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
            buttonIndex += 1

            ' Sales Records Button (Manager and Admin only)
            Dim navSalesRecordsBtn = CreateLargeNavButton("📊 Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navSalesRecordsBtn.Click, AddressOf NavSalesRecords_Click
            buttonIndex += 1

            ' Staff Management Button (Admin only)
            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navStaffBtn.Click, AddressOf NavStaff_Click
                buttonIndex += 1
            End If

            ' Inventory Logs Button (ACTIVE - we're on this page) (Manager and Admin only)
            Dim navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
            buttonIndex += 1

            ' Admin only buttons
            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                ' Audit Logs Button
                Dim navAuditLogBtn = CreateLargeNavButton("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navAuditLogBtn.Click, AddressOf NavAuditLog_Click
                buttonIndex += 1

                ' System Settings Button
                Dim systemSettingsBtn = CreateLargeNavButton("⚙️ System", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler systemSettingsBtn.Click, Sub() MessageBox.Show("System Settings feature coming soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
                buttonIndex += 1
            End If

            Console.WriteLine($"Navigation menu created successfully for role: {currentRole}")

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
        End Try
    End Sub

    Private Function CreateLargeNavButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer) As Guna.UI2.WinForms.Guna2Button
        Dim btn As New Guna.UI2.WinForms.Guna2Button()

        ' Button properties with improved sizing and new color scheme
        btn.Text = text
        btn.Size = New System.Drawing.Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left

        ' Apply new color scheme
        btn.FillColor = If(isActive, System.Drawing.Color.FromArgb(254, 191, 16), System.Drawing.Color.Transparent) ' Golden Yellow if active #FECF10
        btn.ForeColor = If(isActive, System.Drawing.Color.FromArgb(26, 29, 31), System.Drawing.Color.FromArgb(50, 50, 50)) ' Deep Charcoal text on active, Dark Gray text on inactive for white background
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(200, 200, 200)) ' Light Gray border for white background
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        ' Add subtle shadow for depth
        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = System.Drawing.Color.FromArgb(26, 29, 31) ' Deep Charcoal shadow
        btn.ShadowDecoration.Depth = 5
        btn.ShadowDecoration.Shadow = New Padding(0, 2, 5, 5)

        ' Improved hover effects with new color scheme
        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.FromArgb(240, 240, 240) ' Light Gray hover for white background
                                           btn.BorderColor = System.Drawing.Color.FromArgb(190, 154, 48) ' Rich Olive border #BE9A30
                                           btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                       End If
                                   End Sub

        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.Transparent
                                           btn.BorderColor = System.Drawing.Color.FromArgb(200, 200, 200) ' Light Gray border
                                           btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        ' Add to panel
        DashboardPanel.Controls.Add(btn)

        Return btn
    End Function

    Private Sub NavDashboard_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Dashboard.Show()
        Me.Close()
    End Sub

    Private Sub NavPOS_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Me.Close()
    End Sub

    Private Sub NavInventory_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Inventory.Show()
        Me.Close()
    End Sub

    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Me.Close()
    End Sub

    Private Sub NavStaff_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Staff.Show()
        Me.Close()
    End Sub

    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        ' For now, show coming soon message
        MessageBox.Show("Audit Logs feature coming soon!", "Feature Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' Navigation methods
    Private Sub InventoryLog_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)

        ' If this is programmatic navigation, don't show confirmation
        If isNavigating Then
            Return
        End If

        ' Prevent multiple confirmations by checking the close reason
        If e.CloseReason = CloseReason.ApplicationExitCall Then
            ' If Application.Exit() was already called, don't show confirmation again
            Return
        End If

        ' Show confirmation only for user-initiated close (X button)
        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to exit the application?", "Exit Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ' Log the exit action
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via InventoryLog form")
                End If

                ' Close all forms properly
                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then
                        form.Close()
                    End If
                Next

                ' Now exit the application
                Application.Exit()
            Else
                ' Cancel the form closing
                e.Cancel = True
            End If
        End If
    End Sub

    Private Sub Guna2CircleButton3_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Inventory.Show()
        Close()
    End Sub

    Private Sub Exportbtn_Click(sender As Object, e As EventArgs)
        ' Export functionality can be implemented later
        MessageBox.Show("Export functionality will be implemented soon.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' Helper method to validate user session
    Private Function ValidateUserSession() As Boolean
        If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            MessageBox.Show("User session expired. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            frmLoginvb.Show()
            Me.Hide()
            Return False
        End If
        Return True
    End Function

    Private Sub InitializeProfileSection()
        Try
            ' Set username if lblUsername control exists
            If lblUsername IsNot Nothing Then
                lblUsername.Text = frmLoginvb.LoggedInUsername
                lblUsername.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
                lblUsername.ForeColor = System.Drawing.Color.White
            End If

            ' Load user profile picture if control exists
            LoadUserProfilePicture()

            ' Add click event to profile picture and username if they exist
            If Guna2CirclePictureBox5 IsNot Nothing Then
                AddHandler Guna2CirclePictureBox5.Click, AddressOf ProfilePicture_Click
                AddHandler Guna2CirclePictureBox5.MouseEnter, Sub()
                                                                  Guna2CirclePictureBox5.Cursor = Cursors.Hand
                                                              End Sub
            End If

            If lblUsername IsNot Nothing Then
                AddHandler lblUsername.Click, AddressOf ProfilePicture_Click
                AddHandler lblUsername.MouseEnter, Sub()
                                                       lblUsername.Cursor = Cursors.Hand
                                                   End Sub
            End If

        Catch ex As Exception
            ' Fallback if there's an error
            If lblUsername IsNot Nothing Then
                lblUsername.Text = frmLoginvb.LoggedInUsername
            End If
        End Try
    End Sub

    Private Sub LoadUserProfilePicture()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) AndAlso Guna2CirclePictureBox5 IsNot Nothing Then
                ' Query to get the logged-in user's photo
                Dim query As String = "SELECT Photo FROM Users WHERE Username = @Username"
                Dim parameters As SqlParameter() = {
                New SqlParameter("@Username", frmLoginvb.LoggedInUsername)
            }

                Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                    If reader.Read() Then
                        ' Configure the PictureBox for circular profile picture
                        Guna2CirclePictureBox5.SizeMode = PictureBoxSizeMode.Zoom
                        Guna2CirclePictureBox5.BorderStyle = BorderStyle.None

                        If Not IsDBNull(reader("Photo")) Then
                            ' Load user's actual photo
                            Dim photoBytes As Byte() = CType(reader("Photo"), Byte())
                            Using ms As New IO.MemoryStream(photoBytes)
                                Dim loadedImage As Image = Image.FromStream(ms)
                                Guna2CirclePictureBox5.Image = New Bitmap(loadedImage)
                                loadedImage.Dispose()
                            End Using
                        Else
                            ' Create and display default avatar
                            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
                        End If
                    End If
                End Using
            End If
        Catch ex As Exception
            ' If there's an error, show default avatar
            If Guna2CirclePictureBox5 IsNot Nothing Then
                Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(If(frmLoginvb.LoggedInUsername, "User"))
            End If
        End Try
    End Sub

    ' Create default profile avatar method
    Private Function CreateDefaultProfileAvatar(username As String) As System.Drawing.Image
        Dim bitmap As New Bitmap(50, 50)
        Using g As Graphics = Graphics.FromImage(bitmap)
            ' Enable anti-aliasing for smooth circles
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            ' Fill background with a color based on username
            Dim colors() As System.Drawing.Color = {
            System.Drawing.Color.FromArgb(255, 107, 107),
            System.Drawing.Color.FromArgb(78, 205, 196),
            System.Drawing.Color.FromArgb(85, 98, 112),
            System.Drawing.Color.FromArgb(129, 236, 236),
            System.Drawing.Color.FromArgb(116, 185, 255)
        }
            Dim colorIndex As Integer = Math.Abs(username.GetHashCode()) Mod colors.Length
            g.FillEllipse(New SolidBrush(colors(colorIndex)), 0, 0, 50, 50)

            ' Draw initials
            Dim initials As String = ""
            If username.Length > 0 Then
                initials = username.Substring(0, 1).ToUpper()
                If username.Length > 1 Then
                    For i As Integer = 1 To username.Length - 1
                        If Char.IsUpper(username(i)) OrElse username(i) = " "c Then
                            If username(i) <> " "c Then
                                initials += username(i).ToString().ToUpper()
                                Exit For
                            End If
                        End If
                    Next
                End If
            End If

            Using font As New System.Drawing.Font("Poppins", 14, System.Drawing.FontStyle.Bold)
                Dim textSize = g.MeasureString(initials, font)
                g.DrawString(initials, font, Brushes.White,
                (50 - textSize.Width) / 2, (50 - textSize.Height) / 2)
            End Using
        End Using
        Return bitmap
    End Function

    ' Profile dropdown panel
    Private profileDropdownPanel As Panel = Nothing
    Private isProfileDropdownVisible As Boolean = False

    Private Sub ProfilePicture_Click(sender As Object, e As EventArgs)
        ToggleProfileDropdown()
    End Sub

    Private Sub ToggleProfileDropdown()
        If isProfileDropdownVisible Then
            HideProfileDropdown()
        Else
            ShowProfileDropdown()
        End If
    End Sub

    Private Sub ShowProfileDropdown()
        If profileDropdownPanel IsNot Nothing Then
            HideProfileDropdown()
        End If

        ' Create dropdown panel
        profileDropdownPanel = New Panel()
        profileDropdownPanel.Size = New System.Drawing.Size(200, 100)
        profileDropdownPanel.BackColor = System.Drawing.Color.FromArgb(41, 44, 45)
        profileDropdownPanel.BorderStyle = BorderStyle.FixedSingle

        ' Position below the profile picture
        If Guna2CirclePictureBox5 IsNot Nothing Then
            Dim profileLocation = Guna2CirclePictureBox5.Location
            profileDropdownPanel.Location = New Point(profileLocation.X - 90, profileLocation.Y + Guna2CirclePictureBox5.Height + 5)
        End If

        ' Create Profile Settings button
        Dim btnProfileSettings As New Label()
        btnProfileSettings.Text = "⚙️ Profile Settings"
        btnProfileSettings.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnProfileSettings.ForeColor = System.Drawing.Color.White
        btnProfileSettings.BackColor = System.Drawing.Color.Transparent
        btnProfileSettings.Size = New System.Drawing.Size(190, 40)
        btnProfileSettings.Location = New Point(5, 5)
        btnProfileSettings.TextAlign = ContentAlignment.MiddleLeft
        btnProfileSettings.Cursor = Cursors.Hand

        ' Add hover effect to Profile Settings
        AddHandler btnProfileSettings.MouseEnter, Sub()
                                                      btnProfileSettings.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
                                                  End Sub
        AddHandler btnProfileSettings.MouseLeave, Sub()
                                                      btnProfileSettings.BackColor = System.Drawing.Color.Transparent
                                                  End Sub

        ' Add click event to Profile Settings
        AddHandler btnProfileSettings.Click, Sub()
                                                 HideProfileDropdown()
                                                 NavigateToProfileSettings()
                                             End Sub

        ' Create Log Out button
        Dim btnLogOut As New Label()
        btnLogOut.Text = "🚪 Log Out"
        btnLogOut.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnLogOut.ForeColor = System.Drawing.Color.White
        btnLogOut.BackColor = System.Drawing.Color.Transparent
        btnLogOut.Size = New System.Drawing.Size(190, 40)
        btnLogOut.Location = New Point(5, 50)
        btnLogOut.TextAlign = ContentAlignment.MiddleLeft
        btnLogOut.Cursor = Cursors.Hand

        ' Add hover effect to Log Out
        AddHandler btnLogOut.MouseEnter, Sub()
                                             btnLogOut.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
                                         End Sub
        AddHandler btnLogOut.MouseLeave, Sub()
                                             btnLogOut.BackColor = System.Drawing.Color.Transparent
                                         End Sub

        ' Add click event to Log Out - JUST LOGOUT, DON'T EXIT APPLICATION
        AddHandler btnLogOut.Click, Sub()
                                        ' Confirm logout before proceeding
                                        Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

                                        If result = DialogResult.Yes Then
                                            ' Log the logout action
                                            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                                                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Log Out", "User logged out of the application.")
                                            End If

                                            ' Clear user session and return to login (don't exit application)
                                            frmLoginvb.LogoutUser()

                                            ' Navigate to login form without closing the application
                                            isNavigating = True
                                            Me.Hide()
                                            Dim loginForm As New frmLoginvb()
                                            loginForm.Show()
                                        End If
                                    End Sub

        ' Add buttons to panel
        profileDropdownPanel.Controls.Add(btnProfileSettings)
        profileDropdownPanel.Controls.Add(btnLogOut)

        ' Add panel to form
        Me.Controls.Add(profileDropdownPanel)
        profileDropdownPanel.BringToFront()

        ' Add click event to form to hide dropdown when clicked elsewhere
        AddHandler Me.Click, AddressOf Form_Click

        isProfileDropdownVisible = True
    End Sub

    Private Sub HideProfileDropdown()
        If profileDropdownPanel IsNot Nothing Then
            Me.Controls.Remove(profileDropdownPanel)
            profileDropdownPanel.Dispose()
            profileDropdownPanel = Nothing
        End If
        isProfileDropdownVisible = False

        ' Remove form click event
        RemoveHandler Me.Click, AddressOf Form_Click
    End Sub

    Private Sub Form_Click(sender As Object, e As EventArgs)
        ' Hide dropdown when clicking elsewhere on the form
        HideProfileDropdown()
    End Sub

    Private Sub NavigateToProfileSettings()
        If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from InventoryLog to ProfileSettings")
        End If
        isNavigating = True
        ' Implement ProfileSettings form later
        MessageBox.Show("Profile Settings will be implemented.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Async Sub SortBy_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            ' Refresh logs when sorting option changes
            Await LoadInventoryLogsAsync()
        Catch ex As Exception
            MessageBox.Show($"Error sorting inventory logs: {ex.Message}", "Sorting Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class