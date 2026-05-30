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

    Private ReadOnly GoldenYellow As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 254, 191, 16)
    Private ReadOnly RichOlive As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 190, 154, 48)
    Private ReadOnly DeepCharcoal As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 26, 29, 31)
    Private ReadOnly DarkSlate As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 43, 47, 50)
    Private ReadOnly Graphite As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 61, 65, 69)
    Private ReadOnly SteelGray As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 74, 79, 84)
    Private ReadOnly PureWhite As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 255, 255, 255)
    Private ReadOnly LightSilver As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 225, 229, 233)
    Private ReadOnly SuccessGreen As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 16, 216, 98)
    Private ReadOnly AlertRed As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 255, 71, 87)


    Public Sub New()
        InitializeComponent()

        ' Initialize loadingPanel to match the form background and not shown by default
        loadingPanel = New Panel() With {
        .Dock = DockStyle.Fill,
        .Visible = False,
        .BackColor = Me.BackColor
    }

        ' Create centered loading label
        loadingLabel = New Label() With {
        .Text = "Loading Inventory Logs...",
        .ForeColor = System.Drawing.Color.White,
        .Font = New Font("Poppins", 16, FontStyle.Regular),
        .AutoSize = True,
        .BackColor = System.Drawing.Color.Transparent,
        .TextAlign = ContentAlignment.MiddleCenter
    }

        loadingPanel.Controls.Add(loadingLabel)
        Me.Controls.Add(loadingPanel)

        ' Re-center label whenever overlay size changes (handles initial layout and resizes)
        AddHandler loadingPanel.SizeChanged, Sub(sender As Object, ev As EventArgs)
                                                 loadingLabel.Location = New Point((loadingPanel.ClientSize.Width - loadingLabel.Width) \ 2,
                                                                              (loadingPanel.ClientSize.Height - loadingLabel.Height) \ 2)
                                             End Sub

        ' Attempt initial centering in case sizes are already available
        loadingLabel.Location = New Point((loadingPanel.ClientSize.Width - loadingLabel.Width) \ 2,
                                      (loadingPanel.ClientSize.Height - loadingLabel.Height) \ 2)

        loadingPanel.BringToFront()
    End Sub

    Private Async Sub InventoryLog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            ' Setup form
            SetupForm()

            ' Ensure loading panel matches the form background color so it appears as an overlay
            If loadingPanel IsNot Nothing Then
                loadingPanel.BackColor = Me.BackColor
            End If

            ' Show loading panel early so it can render while we prepare the UI
            If loadingPanel IsNot Nothing Then
                loadingPanel.Visible = True
                loadingPanel.BringToFront()
                ' allow a short time for the overlay to render before doing heavier UI work
                Await Task.Delay(150)
            End If

            ' Validate user session
            If Not ValidateUserSession() Then
                Return
            End If

            ' Create navigation menu
            CreateNavigationMenu()

            ' Initialize profile section
            InitializeProfileSection()

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

                ' Ensure date change handler is wired so the filter actually works
                RemoveHandler Guna2DateTimePicker1.ValueChanged, AddressOf Guna2DateTimePicker1_ValueChanged
                AddHandler Guna2DateTimePicker1.ValueChanged, AddressOf Guna2DateTimePicker1_ValueChanged
            End If

            ' Setup events - only if controls exist
            If SortBy IsNot Nothing Then
                RemoveHandler SortBy.SelectedIndexChanged, AddressOf SortBy_SelectedIndexChanged
                AddHandler SortBy.SelectedIndexChanged, AddressOf SortBy_SelectedIndexChanged
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

    ' --- Modifications: add Action column, show summarized Notes in grid, add modal viewer ---
    ' Changes:
    ' 1) In SetupDataGrid() - added an "Action" DataGridViewButtonColumn and wired CellContentClick handler.
    ' 2) In LoadInventoryLogsDataOnUI(...) - Notes column is summarized (preview) rather than full text.
    ' 3) New handler InventoryLogDataGrid_CellContentClick to open a modal on View click.
    ' 4) New method ShowInventoryLogDetailsModal(...) to display all details of the selected log in a modal.

    ' Insert these changes into the existing InventoryLog class (file already contains these methods).
    ' Only the new/updated portions are shown below to be merged into the file.

    Private Sub SetupDataGrid()
        Try
            If InventoryLogDataGrid Is Nothing Then
                Throw New InvalidOperationException("InventoryLogDataGrid control is not initialized")
            End If

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

            ' Theme & general cell style
            InventoryLogDataGrid.BackgroundColor = System.Drawing.Color.FromArgb(41, 44, 45)
            InventoryLogDataGrid.GridColor = System.Drawing.Color.White
            InventoryLogDataGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal

            InventoryLogDataGrid.DefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = System.Drawing.Color.FromArgb(61, 65, 66),
            .ForeColor = System.Drawing.Color.LightGray,
            .SelectionBackColor = System.Drawing.Color.FromArgb(255, 204, 77),
            .SelectionForeColor = System.Drawing.Color.Black,
            .Font = New Font("Poppins", 9.0F, FontStyle.Regular),
            .Alignment = DataGridViewContentAlignment.MiddleCenter,
            .Padding = New Padding(8, 6, 8, 6)
        }

            InventoryLogDataGrid.AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        }

            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = System.Drawing.Color.FromArgb(30, 30, 30),
            .ForeColor = System.Drawing.Color.LightGray,
            .SelectionBackColor = System.Drawing.Color.FromArgb(30, 30, 30),
            .Font = New Font("Poppins SemiBold", 10.0F, FontStyle.Regular),
            .Alignment = DataGridViewContentAlignment.MiddleCenter
        }
            InventoryLogDataGrid.ColumnHeadersHeight = 50
            InventoryLogDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            InventoryLogDataGrid.RowTemplate.Height = 50

            InventoryLogDataGrid.AllowUserToResizeColumns = False
            InventoryLogDataGrid.AllowUserToResizeRows = False
            InventoryLogDataGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing

            ' Columns
            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "LogID",
            .HeaderText = "ID",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 6
        })

            ' Center product text as requested
            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "ProductName",
            .HeaderText = "Product",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Padding = New Padding(10, 6, 10, 6),
                .Font = New Font("Poppins SemiBold", 9.0F, FontStyle.Regular),
                .ForeColor = System.Drawing.Color.LightGray,
                .WrapMode = DataGridViewTriState.False
            },
            .FillWeight = 28
        })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "TransactionType",
            .HeaderText = "Type",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 12
        })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Quantity",
            .HeaderText = "Qty",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 8
        })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "PreviousStock",
            .HeaderText = "Prev",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 8
        })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "NewStock",
            .HeaderText = "New",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 8
        })

            ' Supplier and Reference columns - show in grid and useful in modal fallback
            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "SupplierName",
            .HeaderText = "Supplier",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 16
        })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Reference",
            .HeaderText = "Reference",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .WrapMode = DataGridViewTriState.False},
            .FillWeight = 18
        })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Notes",
            .HeaderText = "Notes (Preview)",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleLeft,
                .WrapMode = DataGridViewTriState.True,
                .ForeColor = System.Drawing.Color.LightGray
            },
            .FillWeight = 20
        })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "CreatedAt",
            .HeaderText = "Date & Time",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
            .FillWeight = 18
        })

            ' Action column - view button (eye)
            Dim actionCol As New DataGridViewButtonColumn() With {
            .Name = "Action",
            .HeaderText = "",
            .Text = "👁️",
            .UseColumnTextForButtonValue = True,
            .FlatStyle = FlatStyle.Flat,
            .FillWeight = 6
        }
            InventoryLogDataGrid.Columns.Add(actionCol)

            ' Wire up double-click handler
            RemoveHandler InventoryLogDataGrid.CellDoubleClick, AddressOf InventoryLogDataGrid_CellDoubleClick
            AddHandler InventoryLogDataGrid.CellDoubleClick, AddressOf InventoryLogDataGrid_CellDoubleClick

            ' Wire up button clicks (Action column)
            RemoveHandler InventoryLogDataGrid.CellContentClick, AddressOf InventoryLogDataGrid_CellContentClick
            AddHandler InventoryLogDataGrid.CellContentClick, AddressOf InventoryLogDataGrid_CellContentClick

        Catch ex As Exception
            MessageBox.Show($"Error setting up DataGrid: {ex.Message}", "DataGrid Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub InventoryLogDataGrid_CellDoubleClick(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If e.RowIndex < 0 OrElse InventoryLogDataGrid Is Nothing Then
                Return
            End If

            Dim row = InventoryLogDataGrid.Rows(e.RowIndex)
            Dim logRecord As Dictionary(Of String, Object) = Nothing

            If row.Tag IsNot Nothing AndAlso TypeOf row.Tag Is Dictionary(Of String, Object) Then
                logRecord = CType(row.Tag, Dictionary(Of String, Object))
            Else
                ' Fallback: build minimal record from visible cells
                logRecord = New Dictionary(Of String, Object) From {
                {"LogID", If(row.Cells("LogID").Value, 0)},
                {"ProductName", If(row.Cells("ProductName").Value?.ToString(), "")},
                {"TransactionType", If(row.Cells("TransactionType").Value?.ToString(), "")},
                {"Quantity", If(row.Cells("Quantity").Value, 0)},
                {"PreviousStock", If(row.Cells("PreviousStock").Value, 0)},
                {"NewStock", If(row.Cells("NewStock").Value, 0)},
                {"Notes", If(row.Cells("Notes").Value?.ToString(), "")},
                {"CreatedAt", If(row.Cells("CreatedAt").Value?.ToString(), DateTime.Now.ToString())}
            }
            End If

            ShowInventoryLogDetailsModal(logRecord)
        Catch ex As Exception
            MessageBox.Show($"Unable to open log details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Update SetRowColorCoding to color the TransactionType cell text appropriately

    Private Sub InventoryLogDataGrid_CellContentClick(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If e.RowIndex < 0 OrElse InventoryLogDataGrid Is Nothing Then
                Return
            End If

            Dim colName As String = InventoryLogDataGrid.Columns(e.ColumnIndex).Name
            If colName = "Action" Then
                Dim row = InventoryLogDataGrid.Rows(e.RowIndex)
                Dim logRecord As Dictionary(Of String, Object) = Nothing
                If row.Tag IsNot Nothing AndAlso TypeOf row.Tag Is Dictionary(Of String, Object) Then
                    logRecord = CType(row.Tag, Dictionary(Of String, Object))
                End If

                If logRecord Is Nothing Then
                    ' Build a record from cell values as fallback
                    logRecord = New Dictionary(Of String, Object) From {
                        {"LogID", If(row.Cells("LogID").Value, 0)},
                        {"ProductName", If(row.Cells("ProductName").Value?.ToString(), "")},
                        {"TransactionType", If(row.Cells("TransactionType").Value?.ToString(), "")},
                        {"Quantity", If(row.Cells("Quantity").Value, 0)},
                        {"PreviousStock", If(row.Cells("PreviousStock").Value, 0)},
                        {"NewStock", If(row.Cells("NewStock").Value, 0)},
                        {"SupplierName", If(row.Cells("SupplierName").Value?.ToString(), "")},
                        {"Reference", If(row.Cells("Reference").Value?.ToString(), "")},
                        {"Notes", If(row.Cells("Notes").Value?.ToString(), "")},
                        {"CreatedAt", If(row.Cells("CreatedAt").Value?.ToString(), DateTime.Now.ToString())}
                    }
                End If

                ShowInventoryLogDetailsModal(logRecord)
            End If
        Catch ex As Exception
            MessageBox.Show($"Unable to open log details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowInventoryLogDetailsModal(logRecord As Dictionary(Of String, Object))
        Try
            ' Build a clean modal that uses a two-column grid (Field | Value) for spacing and readability
            Dim detailForm As New Form() With {
            .Text = "Inventory Log Details",
            .Size = New Size(760, 520),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .BackColor = DarkSlate,
            .ShowInTaskbar = False
        }

            Dim pad As Integer = 16

            ' DataGridView to show field/value pairs
            Dim dgv As New DataGridView() With {
            .Location = New Point(pad, pad),
            .Size = New Size(detailForm.ClientSize.Width - pad * 2, detailForm.ClientSize.Height - 120),
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .BackgroundColor = DarkSlate,
            .BorderStyle = BorderStyle.None,
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .MultiSelect = False
        }

            ' Visual styling for the grid
            dgv.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = Color.FromArgb(30, 30, 30),
            .ForeColor = Color.LightGray,
            .Font = New Font("Poppins SemiBold", 10),
            .Alignment = DataGridViewContentAlignment.MiddleLeft
        }
            dgv.EnableHeadersVisualStyles = False

            dgv.DefaultCellStyle = New DataGridViewCellStyle() With {
            .BackColor = Color.FromArgb(61, 65, 66),
            .ForeColor = Color.LightGray,
            .SelectionBackColor = Color.FromArgb(61, 65, 66), ' make selection visually neutral
            .SelectionForeColor = Color.LightGray,
            .Font = New Font("Poppins", 10),
            .WrapMode = DataGridViewTriState.True
        }
            dgv.RowTemplate.Height = 36
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells

            ' Columns: Field, Value
            dgv.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Field",
            .HeaderText = "Field",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Font = New Font("Poppins SemiBold", 10), .Alignment = DataGridViewContentAlignment.MiddleLeft},
            .FillWeight = 35
        })
            dgv.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Value",
            .HeaderText = "Value",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Font = New Font("Poppins", 10), .Alignment = DataGridViewContentAlignment.MiddleLeft, .WrapMode = DataGridViewTriState.True},
            .FillWeight = 65
        })

            detailForm.Controls.Add(dgv)

            ' Extract values (safe)
            Dim logId = If(logRecord.ContainsKey("LogID"), logRecord("LogID").ToString(), "")
            Dim productName = If(logRecord.ContainsKey("ProductName"), logRecord("ProductName").ToString(), "")
            Dim transactionType = If(logRecord.ContainsKey("TransactionType"), logRecord("TransactionType").ToString(), "")
            Dim quantity = If(logRecord.ContainsKey("Quantity"), logRecord("Quantity").ToString(), "0")
            Dim previousStock = If(logRecord.ContainsKey("PreviousStock"), logRecord("PreviousStock").ToString(), "0")
            Dim newStock = If(logRecord.ContainsKey("NewStock"), logRecord("NewStock").ToString(), "0")
            Dim supplierName = If(logRecord.ContainsKey("SupplierName"), logRecord("SupplierName").ToString(), "")
            Dim reference = If(logRecord.ContainsKey("Reference"), logRecord("Reference").ToString(), "")
            Dim notes = If(logRecord.ContainsKey("Notes"), logRecord("Notes").ToString(), "")
            Dim createdAtStr = If(logRecord.ContainsKey("CreatedAt"), Convert.ToDateTime(logRecord("CreatedAt")).ToString("MM/dd/yyyy HH:mm:ss"), "")

            ' Prefer Category/batch/expiry from supplied logRecord
            Dim productCategory As String = If(logRecord.ContainsKey("Category"), logRecord("Category").ToString(), "")
            Dim batchNumber As String = If(logRecord.ContainsKey("BatchNumber") AndAlso logRecord("BatchNumber") IsNot Nothing, logRecord("BatchNumber")?.ToString(), "")
            Dim expiryStr As String = ""
            If logRecord.ContainsKey("ExpiryDate") AndAlso logRecord("ExpiryDate") IsNot Nothing AndAlso Not TypeOf logRecord("ExpiryDate") Is DBNull Then
                Try
                    expiryStr = Convert.ToDateTime(logRecord("ExpiryDate")).ToString("MM/dd/yyyy")
                Catch
                    expiryStr = If(logRecord("ExpiryDate")?.ToString(), "")
                End Try
            End If

            ' Fallback DB lookup only if key data missing
            If (String.IsNullOrWhiteSpace(productCategory) OrElse String.IsNullOrWhiteSpace(batchNumber) OrElse String.IsNullOrWhiteSpace(expiryStr)) AndAlso Not String.IsNullOrWhiteSpace(logId) Then
                Try
                    Dim connStr = Connection.GetConnectionString()
                    If Not String.IsNullOrEmpty(connStr) Then
                        Using conn As New SqlConnection(connStr)
                            conn.Open()
                            Dim q As String = "SELECT TOP 1 p.Category, il.BatchNumber, il.ExpiryDate " &
                                          "FROM InventoryLog il LEFT JOIN Products p ON il.ProductID = p.ProductID " &
                                          "WHERE il.LogID = @LogID"
                            Using cmd As New SqlCommand(q, conn)
                                cmd.Parameters.AddWithValue("@LogID", Convert.ToInt32(logId))
                                Using r As SqlDataReader = cmd.ExecuteReader()
                                    If r.Read() Then
                                        If String.IsNullOrWhiteSpace(productCategory) AndAlso Not IsDBNull(r("Category")) Then productCategory = r("Category").ToString()
                                        If String.IsNullOrWhiteSpace(batchNumber) AndAlso r.GetSchemaTable().Rows.Cast(Of DataRow)().Any(Function(rr) rr("ColumnName").ToString() = "BatchNumber") Then
                                            If Not IsDBNull(r("BatchNumber")) Then batchNumber = r("BatchNumber").ToString()
                                        End If
                                        If String.IsNullOrWhiteSpace(expiryStr) AndAlso r.GetSchemaTable().Rows.Cast(Of DataRow)().Any(Function(rr) rr("ColumnName").ToString() = "ExpiryDate") Then
                                            If Not IsDBNull(r("ExpiryDate")) Then expiryStr = Convert.ToDateTime(r("ExpiryDate")).ToString("MM/dd/yyyy")
                                        End If
                                    End If
                                End Using
                            End Using
                        End Using
                    End If
                Catch
                    ' non-fatal
                End Try
            End If

            ' Populate grid with field/value rows
            Dim AddRow = Sub(f As String, v As String)
                             Dim idx As Integer = dgv.Rows.Add()
                             dgv.Rows(idx).Cells("Field").Value = f
                             dgv.Rows(idx).Cells("Value").Value = v
                         End Sub

            AddRow("Log ID", logId)
            AddRow("Product", productName)
            If Not String.IsNullOrWhiteSpace(productCategory) Then AddRow("Category", productCategory) ' <-- show Category
            AddRow("Transaction Type", transactionType)
            AddRow("Quantity", quantity)
            AddRow("Previous Stock", previousStock)
            AddRow("New Stock", newStock)
            AddRow("Supplier", supplierName)
            AddRow("Reference", reference)
            ' Show batch & expiry when present (especially for ENDO category)
            If Not String.IsNullOrWhiteSpace(batchNumber) Then AddRow("Batch Number", batchNumber)
            If Not String.IsNullOrWhiteSpace(expiryStr) Then AddRow("Expiry Date", expiryStr)
            AddRow("Notes", notes)
            AddRow("Created At", createdAtStr)

            ' Remove visible selection/active highlight
            dgv.ClearSelection()
            dgv.CurrentCell = Nothing
            dgv.ColumnHeadersDefaultCellStyle.SelectionBackColor = dgv.ColumnHeadersDefaultCellStyle.BackColor

            ' Footer buttons
            Dim btnClose As New Button() With {
            .Text = "Close",
            .Size = New Size(120, 38),
            .Location = New Point(detailForm.ClientSize.Width - pad - 120, detailForm.ClientSize.Height - 70),
            .BackColor = GoldenYellow,
            .ForeColor = DeepCharcoal,
            .Font = New Font("Poppins", 10, FontStyle.Bold),
            .FlatStyle = FlatStyle.Flat
        }
            AddHandler btnClose.Click, Sub() detailForm.Close()
            detailForm.Controls.Add(btnClose)

            ' Export button
            Dim btnExport As New Button() With {
            .Text = "Export",
            .Size = New Size(120, 38),
            .Location = New Point(detailForm.ClientSize.Width - pad - 260, detailForm.ClientSize.Height - 70),
            .BackColor = SteelGray,
            .ForeColor = Color.White,
            .Font = New Font("Poppins", 10, FontStyle.Regular),
            .FlatStyle = FlatStyle.Flat
        }
            AddHandler btnExport.Click, Sub()
                                            Try
                                                Using sfd As New SaveFileDialog()
                                                    sfd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
                                                    sfd.FileName = $"InventoryLog_{logId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                                                    If sfd.ShowDialog() = DialogResult.OK Then
                                                        Using sw As New IO.StreamWriter(sfd.FileName, False, System.Text.Encoding.UTF8)
                                                            sw.WriteLine("Field,Value")
                                                            For Each r As DataGridViewRow In dgv.Rows
                                                                Dim f = r.Cells("Field").Value?.ToString().Replace(","c, " ")
                                                                Dim v = r.Cells("Value").Value?.ToString().Replace(","c, " ")
                                                                sw.WriteLine($"{f},{v}")
                                                            Next
                                                            sw.Flush()
                                                        End Using
                                                        MessageBox.Show("Export completed.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                    End If
                                                End Using
                                            Catch ex As Exception
                                                MessageBox.Show($"Export failed: {ex.Message}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                            End Try
                                        End Sub
            detailForm.Controls.Add(btnExport)

            detailForm.ShowDialog()
            detailForm.Dispose()
        Catch ex As Exception
            MessageBox.Show($"Error showing log details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
             "il.Reference, il.Notes, il.BatchNumber, il.ExpiryDate, p.Category, il.CreatedAt " &  ' <- include Category (product category)
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
                {"BatchNumber", If(reader.FieldCount > 0 AndAlso Not IsDBNull(reader("BatchNumber")), reader("BatchNumber").ToString(), "")},
                {"ExpiryDate", If(reader.FieldCount > 0 AndAlso Not IsDBNull(reader("ExpiryDate")), Convert.ToDateTime(reader("ExpiryDate")), DBNull.Value)},
                {"Category", If(reader.FieldCount > 0 AndAlso Not IsDBNull(reader("Category")), reader("Category").ToString(), "")}, ' <- use Category
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
                ' Update record count without touching lblUsername
                ' Ensure no default selection when empty
                InventoryLogDataGrid.ClearSelection()
                InventoryLogDataGrid.CurrentCell = Nothing
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

            ' Prevent DataGrid from automatically selecting the first row after refresh
            InventoryLogDataGrid.ClearSelection()
            Try
                InventoryLogDataGrid.CurrentCell = Nothing
            Catch
                ' Some DataGridView configurations can throw when setting CurrentCell to Nothing; ignore safely
            End Try

            ' Update record count without overwriting lblUsername

        Catch ex As Exception
            MessageBox.Show($"Error displaying inventory logs: {ex.Message}", "Display Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub NavigateToProfileSettings()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from InventoryLog to ProfileSettings")
            End If

            ' Prevent the form-closing confirmation and hide the dropdown first
            isNavigating = True
            HideProfileDropdown()

            ' Open ProfileSettings and close InventoryLog
            Dim profileForm As New ProfileSettings()
            profileForm.StartPosition = FormStartPosition.CenterScreen
            profileForm.Show()

            Me.Close()
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open Profile Settings: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Function GetTransactionIndicator(transactionType As String) As String
        If String.IsNullOrWhiteSpace(transactionType) Then Return "ℹ️ INFO"
        Select Case transactionType.Trim().ToLowerInvariant()
            Case "stock in", "in", "stock_in", "inbound"
                Return "📈 IN"
            Case "stock out", "out", "stock_out", "outbound", "sold"
                Return "📉 OUT"
            Case "adjustments", "adjust", "adj"
                Return "⚖️ ADJ"
            Case Else
                Return "ℹ️ INFO"
        End Select
    End Function

    Private Sub SetRowColorCoding(rowIndex As Integer, transactionType As String, transactionIndicator As String)
        Try
            If InventoryLogDataGrid Is Nothing OrElse rowIndex < 0 OrElse rowIndex >= InventoryLogDataGrid.Rows.Count Then
                Return
            End If

            Dim row = InventoryLogDataGrid.Rows(rowIndex)
            If row Is Nothing Then Return

            Dim txNorm As String = If(transactionType, "").Trim().ToLowerInvariant()

            ' Keep uniform row background for all rows
            row.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
            row.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray

            ' Configure TransactionType cell styles only (do not color full row)
            If InventoryLogDataGrid.Columns.Contains("TransactionType") Then
                Dim cell = row.Cells("TransactionType")
                cell.Style.Font = New Font(InventoryLogDataGrid.Font, FontStyle.Regular)
                cell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                ' Default foreground
                cell.Style.ForeColor = System.Drawing.Color.LightGray
                ' Ensure selection does not override the ForeColor — set SelectionForeColor to same
                cell.Style.SelectionForeColor = cell.Style.ForeColor
                ' Keep selection background consistent with rest of grid
                cell.Style.SelectionBackColor = InventoryLogDataGrid.DefaultCellStyle.SelectionBackColor
            End If

            ' Ensure product is centered and uses default colors
            If InventoryLogDataGrid.Columns.Contains("ProductName") Then
                Dim pcell = row.Cells("ProductName")
                pcell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                pcell.Style.ForeColor = System.Drawing.Color.LightGray
            End If

            Select Case txNorm
                Case "stock in", "in", "stock_in", "inbound"
                    If InventoryLogDataGrid.Columns.Contains("TransactionType") Then
                        Dim cell = row.Cells("TransactionType")
                        cell.Style.ForeColor = SuccessGreen
                        cell.Style.SelectionForeColor = SuccessGreen
                        cell.Style.Font = New Font(InventoryLogDataGrid.Font, FontStyle.Bold)
                    End If
                Case "stock out", "out", "stock_out", "outbound", "sold"
                    If InventoryLogDataGrid.Columns.Contains("TransactionType") Then
                        Dim cell = row.Cells("TransactionType")
                        cell.Style.ForeColor = AlertRed
                        cell.Style.SelectionForeColor = AlertRed
                        cell.Style.Font = New Font(InventoryLogDataGrid.Font, FontStyle.Bold)
                    End If
                Case "adjustments", "adjust", "adj"
                    If InventoryLogDataGrid.Columns.Contains("TransactionType") Then
                        Dim cell = row.Cells("TransactionType")
                        cell.Style.ForeColor = System.Drawing.Color.FromArgb(255, 180, 100)
                        cell.Style.SelectionForeColor = System.Drawing.Color.FromArgb(255, 180, 100)
                        cell.Style.Font = New Font(InventoryLogDataGrid.Font, FontStyle.Bold)
                    End If
                Case Else
                    ' leave defaults
            End Select

        Catch ex As Exception
            ' Silent fail to avoid breaking UI rendering
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
        If Guna2DateTimePicker1 IsNot Nothing Then
            ' reset UI and reload data
            Guna2DateTimePicker1.Value = DateTime.Today
        End If
        Await LoadInventoryLogsAsync()
    End Sub

    Private Async Sub AddInventoryLog_Click(sender As Object, e As EventArgs)
        ' Create overlay panel for modal effect
        Dim overlayPanel As New Panel()
        ' use darker overlay that matches the app dark background (semi-opaque)
        Dim baseColor = CompanySettingsManager.Instance.GetColor("backgrounddark")
        overlayPanel.BackColor = Color.FromArgb(200, baseColor.R, baseColor.G, baseColor.B)
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
        NavigationBuilder.Build(DashboardPanel, Me, "InventoryLog")
    End Sub

    Private Sub NavSuppliers_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            Supplier.Show()
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Unable to open Suppliers: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub NavSystemSettings_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sys.Show()
        Me.Close()
    End Sub

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
        SalesRecord.Show()
        Me.Close()
    End Sub

    Private Sub NavStaff_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Staff.Show()
        Me.Close()
    End Sub
    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        AuditLog.Show()
        Me.Close()
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
            Dim result As DialogResult = EscForm.ConfirmExit(Me)

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
        Try
            ' Get current sort order and filter settings
            Dim sortOrder As String = If(SortBy?.SelectedItem?.ToString(), "Date (Newest First)")
            Dim filterType As String = "All Logs" ' You can expand this to match actual filter types
            Dim filterDate As DateTime? = selectedDate

            ' Call the inventory log exporter
            InventoryLogExporter.ExportInventoryLogsReport(sortOrder, filterType, filterDate)
        Catch ex As Exception
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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



    Private Async Sub SortBy_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            ' Refresh logs when sorting option changes
            Await LoadInventoryLogsAsync()
        Catch ex As Exception
            MessageBox.Show($"Error sorting inventory logs: {ex.Message}", "Sorting Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Exportbtn_Click_1(sender As Object, e As EventArgs) Handles Exportbtn.Click
        Try
            ' Get current sort order and filter settings
            Dim sortOrder As String = If(SortBy?.SelectedItem?.ToString(), "Date (Newest First)")
            Dim filterType As String = "All Logs" ' You can expand this to match actual filter types
            Dim filterDate As DateTime? = selectedDate

            ' Call the inventory log exporter
            InventoryLogExporter.ExportInventoryLogsReport(sortOrder, filterType, filterDate)
        Catch ex As Exception
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class