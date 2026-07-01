Imports Microsoft.Data.SqlClient
Imports System.Linq
Imports System.Reflection

Public Class Supplier
    Private isNavigating As Boolean = False
    ' Profile managed by ProfileManager
    Private ReadOnly Graphite As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 61, 65, 69)


    Private Sub Supplier_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        DashboardPanel.Location = New Point(-10, 5)
        Try
            Me.KeyPreview = True
            Me.BackColor = Color.FromArgb(30, 30, 30)

            ' Only set standalone form properties when not hosted in MainShell
            If Not IsHostedInMainShell() Then
                Me.FormBorderStyle = FormBorderStyle.None
                Me.WindowState = FormWindowState.Maximized
            End If

            ' Validate session
            If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                MessageBox.Show("User session expired. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                If IsHostedInMainShell() Then
                    GetMainShell().ShowPage(GetType(frmLoginvb))
                Else
                    frmLoginvb.Show()
                End If
                Me.Close()
                Return
            End If

            ' Enable double buffering for smooth scrolling
            SetDoubleBuffered(InventoryLogDataGrid)

            ' Start idle timeout monitoring
            IdleTimeoutManager.Instance.StartMonitoring(Me)
            ' Initialize profile section
            InitializeProfileSection()

            ' Create navigation directly using shared builder
            NavigationBuilder.Build(DashboardPanel, Me, "Supplier")

            ' Initialize grid and controls
            InitializeDataGridView()
            InitializeSortComboBox()

            ' Wire events
            AddHandler SortBy.SelectedIndexChanged, AddressOf SortBy_SelectedIndexChanged
            AddHandler Exportbtn.Click, AddressOf Exportbtn_Click
            ' Load data on UI thread to avoid cross-thread control access
            LoadSuppliersData()

            ' Set focus to form so ESC key works immediately
            Me.Activate()
            Me.Focus()

            ' Align DataGridView bottom with DashboardPanel bottom
            AlignDataGridViewToPanel()
        Catch ex As Exception
            MessageBox.Show($"Error initializing Suppliers page: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AlignDataGridViewToPanel()
        If DashboardPanel IsNot Nothing AndAlso InventoryLogDataGrid IsNot Nothing Then
            Dim panelBottom As Integer = DashboardPanel.Location.Y + DashboardPanel.Size.Height
            Dim newHeight As Integer = panelBottom - InventoryLogDataGrid.Location.Y
            If newHeight > 100 Then
                InventoryLogDataGrid.Size = New Size(InventoryLogDataGrid.Size.Width, newHeight)
            End If
        End If
    End Sub

    Private Sub Supplier_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        AlignDataGridViewToPanel()
    End Sub

    Private Sub Exportbtn_Click(sender As Object, e As EventArgs)
        MessageBox.Show("Export not implemented.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub FilterDateChanged(sender As Object, e As EventArgs)
        ' Suppliers does not really use date filter but keep for parity
        LoadSuppliersData(If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), ""))
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            If isNavigating Then
                Return True
            End If

            If Me.OwnedForms.Cast(Of Form)().Any(Function(f) f.Visible) Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            If Application.OpenForms.Cast(Of Form)().Any(Function(f) f IsNot Me AndAlso f.Visible AndAlso f.Modal) Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            If Not Me.ContainsFocus Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            Dim result As DialogResult = EscForm.ConfirmExit(Me)
            Me.Activate()
            If result = DialogResult.Yes Then
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Supplier.")
                End If

                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then
                        form.Close()
                    End If
                Next

                Application.Exit()
            End If

            Return True
        End If

        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    Private Sub InitializeDataGridView()
        Try
            InventoryLogDataGrid.Columns.Clear()
            InventoryLogDataGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter ' center all cells by default

            InventoryLogDataGrid.BackgroundColor = System.Drawing.Color.FromArgb(41, 44, 45)
            InventoryLogDataGrid.GridColor = System.Drawing.Color.White
            InventoryLogDataGrid.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
            InventoryLogDataGrid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
            InventoryLogDataGrid.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            InventoryLogDataGrid.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 204, 77)
            InventoryLogDataGrid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
            InventoryLogDataGrid.DefaultCellStyle.Font = New System.Drawing.Font("Poppins", 9.0F, System.Drawing.FontStyle.Regular)
            InventoryLogDataGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(30, 30, 30)
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.Font = New System.Drawing.Font("Poppins SemiBold", 10.0F, System.Drawing.FontStyle.Regular)
            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            InventoryLogDataGrid.ColumnHeadersHeight = 50

            ' slightly taller rows to avoid clipping and to allow center visually
            InventoryLogDataGrid.RowTemplate.Height = 60

            InventoryLogDataGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            InventoryLogDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            InventoryLogDataGrid.AllowUserToAddRows = False
            InventoryLogDataGrid.AllowUserToDeleteRows = False
            InventoryLogDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            InventoryLogDataGrid.MultiSelect = False
            InventoryLogDataGrid.ScrollBars = ScrollBars.Vertical
            InventoryLogDataGrid.RowHeadersVisible = False

            ' ID - small fixed-ish width (FillWeight low)
            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "SupplierID",
            .HeaderText = "ID",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Padding = New Padding(2, 0, 2, 0)
            },
            .FillWeight = 5
        })

            ' Code
            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "SupplierCode",
            .HeaderText = "Code",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Padding = New Padding(4, 0, 4, 0)
            },
            .FillWeight = 8
        })

            ' Supplier Name - CENTERED and minimal padding to avoid perceived left offset
            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "SupplierName",
            .HeaderText = "Supplier Name",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Padding = New Padding(2, 0, 2, 0),
                .WrapMode = DataGridViewTriState.False
            },
            .FillWeight = 36
        })

            ' Contact Person
            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "ContactPerson",
            .HeaderText = "Contact Person",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Padding = New Padding(4, 0, 4, 0)
            },
            .FillWeight = 18
        })

            ' Phone
            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Phone",
            .HeaderText = "Phone",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Padding = New Padding(4, 0, 4, 0)
            },
            .FillWeight = 10
        })

            ' Email
            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Email",
            .HeaderText = "Email",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Padding = New Padding(4, 0, 4, 0),
                .WrapMode = DataGridViewTriState.False
            },
            .FillWeight = 18
        })

            ' Stock Ins
            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "StockIns",
            .HeaderText = "Stock In Count",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter
            },
            .FillWeight = 8
        })

            ' Action (pencil)
            Dim actionCol As New DataGridViewTextBoxColumn()
            actionCol.Name = "Action"
            actionCol.HeaderText = ""
            actionCol.ReadOnly = True
            actionCol.DefaultCellStyle = New DataGridViewCellStyle() With {
            .Alignment = DataGridViewContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI Emoji", 12, FontStyle.Regular),
            .ForeColor = System.Drawing.Color.LightGray
        }
            actionCol.FillWeight = 5
            InventoryLogDataGrid.Columns.Add(actionCol)

            ' Enforce centered header alignment
            For Each col As DataGridViewColumn In InventoryLogDataGrid.Columns
                col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
                If col.DefaultCellStyle Is Nothing Then col.DefaultCellStyle = New DataGridViewCellStyle()
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
            Next

            ' Wire events
            RemoveHandler InventoryLogDataGrid.CellClick, AddressOf InventoryLogDataGrid_CellClick
            AddHandler InventoryLogDataGrid.CellClick, AddressOf InventoryLogDataGrid_CellClick

            RemoveHandler InventoryLogDataGrid.CellMouseEnter, AddressOf InventoryLogDataGrid_CellMouseEnter
            AddHandler InventoryLogDataGrid.CellMouseEnter, AddressOf InventoryLogDataGrid_CellMouseEnter

            RemoveHandler InventoryLogDataGrid.CellMouseLeave, AddressOf InventoryLogDataGrid_CellMouseLeave
            AddHandler InventoryLogDataGrid.CellMouseLeave, AddressOf InventoryLogDataGrid_CellMouseLeave

        Catch ex As Exception
            MessageBox.Show($"Error preparing suppliers grid: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub InventoryLogDataGrid_CellMouseEnter(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If InventoryLogDataGrid Is Nothing Then Return
            If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 Then
                Dim colName = InventoryLogDataGrid.Columns(e.ColumnIndex).Name
                If colName = "Action" Then
                    InventoryLogDataGrid.Cursor = Cursors.Hand
                    ' subtle hover styling for the action cell
                    Dim cell = InventoryLogDataGrid.Rows(e.RowIndex).Cells(e.ColumnIndex)
                    cell.Style.BackColor = System.Drawing.Color.FromArgb(81, 85, 86)
                    cell.Style.ForeColor = System.Drawing.Color.White
                End If
            End If
        Catch
            ' silent
        End Try
    End Sub

    Private Sub InventoryLogDataGrid_CellMouseLeave(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If InventoryLogDataGrid Is Nothing Then Return
            If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 Then
                Dim colName = InventoryLogDataGrid.Columns(e.ColumnIndex).Name
                If colName = "Action" Then
                    InventoryLogDataGrid.Cursor = Cursors.Default
                    ' restore default style for the action cell
                    Dim cell = InventoryLogDataGrid.Rows(e.RowIndex).Cells(e.ColumnIndex)
                    cell.Style.BackColor = InventoryLogDataGrid.DefaultCellStyle.BackColor
                    cell.Style.ForeColor = InventoryLogDataGrid.DefaultCellStyle.ForeColor
                End If
            End If
        Catch
            ' silent
        End Try
    End Sub
    Private Sub InitializeSortComboBox()
        SortBy.Items.Clear()
        SortBy.Items.Add("Name (A-Z)")
        SortBy.Items.Add("Name (Z-A)")
        SortBy.Items.Add("Code (Ascending)")
        SortBy.Items.Add("Code (Descending)")
        SortBy.Items.Add("Status (Active First)")
        SortBy.SelectedIndex = 0
    End Sub

    Private Sub SortBy_SelectedIndexChanged(sender As Object, e As EventArgs)
        If SortBy.SelectedItem IsNot Nothing Then
            LoadSuppliersData(SortBy.SelectedItem.ToString())
        End If
    End Sub

    Private Sub LoadSuppliersData(Optional sortOrder As String = "")
        Try
            InventoryLogDataGrid.Rows.Clear()

            Dim query As String = "SELECT SupplierID, SupplierCode, SupplierName, ContactPerson, Phone, Email, IsActive FROM Suppliers"

            Select Case sortOrder
                Case "Name (A-Z)"
                    query += " ORDER BY SupplierName ASC"
                Case "Name (Z-A)"
                    query += " ORDER BY SupplierName DESC"
                Case "Code (Ascending)"
                    query += " ORDER BY SupplierCode ASC"
                Case "Code (Descending)"
                    query += " ORDER BY SupplierCode DESC"
                Case "Status (Active First)"
                    query += " ORDER BY IsActive DESC, SupplierName ASC"
                Case Else
                    query += " ORDER BY SupplierName ASC"
            End Select

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                Dim count As Integer = 0
                While reader.Read()
                    Dim supplierId As Integer = Convert.ToInt32(reader("SupplierID"))
                    Dim rowIndex As Integer = InventoryLogDataGrid.Rows.Add()
                    InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierID").Value = supplierId
                    InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierCode").Value = If(IsDBNull(reader("SupplierCode")), "", reader("SupplierCode").ToString())
                    InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierName").Value = If(IsDBNull(reader("SupplierName")), "", reader("SupplierName").ToString())
                    InventoryLogDataGrid.Rows(rowIndex).Cells("ContactPerson").Value = If(IsDBNull(reader("ContactPerson")), "", reader("ContactPerson").ToString())
                    InventoryLogDataGrid.Rows(rowIndex).Cells("Phone").Value = If(IsDBNull(reader("Phone")), "", reader("Phone").ToString())
                    InventoryLogDataGrid.Rows(rowIndex).Cells("Email").Value = If(IsDBNull(reader("Email")), "", reader("Email").ToString())

                    ' Get stock-in count for this supplier
                    Dim stockIns As Integer = GetSupplierStockInCount(supplierId)
                    If InventoryLogDataGrid.Columns.Contains("StockIns") Then
                        InventoryLogDataGrid.Rows(rowIndex).Cells("StockIns").Value = stockIns
                    End If

                    ' Show pencil for edit
                    InventoryLogDataGrid.Rows(rowIndex).Cells("Action").Value = "✏️"

                    ' Store full data in Tag including status
                    InventoryLogDataGrid.Rows(rowIndex).Tag = New Dictionary(Of String, Object) From {
                    {"SupplierID", supplierId},
                    {"SupplierCode", If(IsDBNull(reader("SupplierCode")), "", reader("SupplierCode").ToString())},
                    {"SupplierName", If(IsDBNull(reader("SupplierName")), "", reader("SupplierName").ToString())},
                    {"ContactPerson", If(IsDBNull(reader("ContactPerson")), "", reader("ContactPerson").ToString())},
                    {"Phone", If(IsDBNull(reader("Phone")), "", reader("Phone").ToString())},
                    {"Email", If(IsDBNull(reader("Email")), "", reader("Email").ToString())},
                    {"StockIns", stockIns},
                    {"IsActive", If(IsDBNull(reader("IsActive")), True, Convert.ToBoolean(reader("IsActive")))}
                }

                    count += 1
                End While

                ' DO NOT modify lblUsername here — keep it showing the logged-in username.
                ' (Removed previous lblUsername = "{count} Items" update.)
            End Using

            InventoryLogDataGrid.ClearSelection()
            InventoryLogDataGrid.Refresh()

        Catch ex As Exception
            MessageBox.Show($"Error loading suppliers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Function GetSupplierStockInCount(supplierId As Integer) As Integer
        Try
            Dim query As String = "SELECT COUNT(1) FROM InventoryLog WHERE SupplierID = @SupplierID AND " &
                              "(TransactionType IN ('IN', 'INBOUND', 'Stock In', 'stock in') OR LOWER(TransactionType) = 'in')"
            Dim param As New SqlParameter("@SupplierID", supplierId)
            Dim result As Object = Utilities.ExecuteScalar(query, New SqlParameter() {param})
            If result Is Nothing OrElse IsDBNull(result) Then
                Return 0
            End If
            Return Convert.ToInt32(result)
        Catch
            Return 0
        End Try
    End Function
    Private Sub InventoryLogDataGrid_CellClick(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If e.RowIndex < 0 OrElse InventoryLogDataGrid Is Nothing Then
                Return
            End If

            Dim colName As String = InventoryLogDataGrid.Columns(e.ColumnIndex).Name

            If colName = "Action" Then
                Dim row = InventoryLogDataGrid.Rows(e.RowIndex)
                Dim tag = TryCast(row.Tag, Dictionary(Of String, Object))
                If tag IsNot Nothing AndAlso tag.ContainsKey("SupplierID") Then
                    ShowEditSupplierModal(tag, rowIndex:=e.RowIndex)
                Else
                    MessageBox.Show("Unable to determine supplier details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error processing action click: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub InitializeProfileSection()
        ProfileManager.InitializeProfile(Me, lblUsername, Guna2CirclePictureBox5, AddressOf NavigateToProfileSettings)
    End Sub

    Private Sub NavigateToProfileSettings()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from Supplier to ProfileSettings")
            End If

            isNavigating = True

            Dim profileForm As New ProfileSettings()
            profileForm.StartPosition = FormStartPosition.CenterScreen
            profileForm.Show()

            If Not IsHostedInMainShell() Then
                Me.Close()
            End If
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open Profile Settings: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowSupplierDetails(supplierTag As Dictionary(Of String, Object))
        Try
            Dim supplierId As Integer = Convert.ToInt32(supplierTag("SupplierID"))
            Dim supplierCode As String = If(supplierTag.ContainsKey("SupplierCode"), supplierTag("SupplierCode").ToString(), "")
            Dim supplierName As String = If(supplierTag.ContainsKey("SupplierName"), supplierTag("SupplierName").ToString(), "")
            Dim stockIns As Integer = If(supplierTag.ContainsKey("StockIns"), Convert.ToInt32(supplierTag("StockIns")), GetSupplierStockInCount(supplierId))

            Dim detailForm As New Form() With {
                .Text = $"Supplier - {supplierName}",
                .Size = New Size(520, 300),
                .StartPosition = FormStartPosition.CenterParent,
                .FormBorderStyle = FormBorderStyle.FixedDialog,
                .BackColor = Color.FromArgb(41, 44, 45)
            }

            Dim y As Integer = 18
            Dim AddRow = Sub(labelText As String, valueText As String)
                             Dim lbl As New Label() With {
                                 .Text = labelText,
                                 .ForeColor = Color.LightGray,
                                 .Font = New Font("Poppins", 10, FontStyle.Regular),
                                 .Location = New Point(20, y),
                                 .Size = New Size(140, 28),
                                 .TextAlign = ContentAlignment.MiddleLeft
                             }
                             detailForm.Controls.Add(lbl)

                             Dim val As New TextBox() With {
                                 .ReadOnly = True,
                                 .Text = valueText,
                                 .Location = New Point(170, y),
                                 .Size = New Size(320, 28),
                                 .BackColor = Color.White,
                                 .ForeColor = Color.Black,
                                 .BorderStyle = BorderStyle.FixedSingle
                             }
                             detailForm.Controls.Add(val)
                             y += 36
                         End Sub

            AddRow("Supplier ID:", supplierId.ToString())
            AddRow("Supplier Code:", supplierCode)
            AddRow("Supplier Name:", supplierName)
            AddRow("Stock In Count:", stockIns.ToString())

            Dim btnClose As New Button() With {
                .Text = "Close",
                .Size = New Size(100, 36),
                .Location = New Point((detailForm.ClientSize.Width - 100) \ 2, y + 10),
                .BackColor = Color.FromArgb(255, 204, 77),
                .ForeColor = Color.Black,
                .Font = New Font("Poppins", 10, FontStyle.Regular)
            }
            AddHandler btnClose.Click, Sub() detailForm.Close()
            detailForm.Controls.Add(btnClose)

            detailForm.ShowDialog()
            detailForm.Dispose()
        Catch ex As Exception
            MessageBox.Show($"Error showing supplier details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub ShowEditSupplierModal(supplierTag As Dictionary(Of String, Object), Optional rowIndex As Integer = -1)
        Try
            Dim supplierId As Integer = Convert.ToInt32(supplierTag("SupplierID"))
            Dim supplierCode As String = If(supplierTag.ContainsKey("SupplierCode"), supplierTag("SupplierCode").ToString(), "")
            Dim supplierName As String = If(supplierTag.ContainsKey("SupplierName"), supplierTag("SupplierName").ToString(), "")
            Dim contactPerson As String = If(supplierTag.ContainsKey("ContactPerson"), supplierTag("ContactPerson").ToString(), "")
            Dim phone As String = If(supplierTag.ContainsKey("Phone"), supplierTag("Phone").ToString(), "")
            Dim email As String = If(supplierTag.ContainsKey("Email"), supplierTag("Email").ToString(), "")
            Dim isActive As Boolean = If(supplierTag.ContainsKey("IsActive"), Convert.ToBoolean(supplierTag("IsActive")), True)

            ' Larger, cleaner modal with two-column layout and recent stock-in grid
            Dim editForm As New Form() With {
            .Text = $"Edit Supplier — {supplierName}",
            .Size = New Size(900, 560),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .BackColor = Color.FromArgb(41, 44, 45)
        }

            Dim padLeft As Integer = 20
            Dim leftColW As Integer = 460
            Dim rightColX As Integer = padLeft + leftColW + 24
            Dim y As Integer = 18
            Dim labelW As Integer = 120
            Dim controlW As Integer = leftColW - labelW - 10
            Dim h As Integer = 30

            ' Header
            Dim header As New Label() With {
            .Text = $"Edit Supplier — {supplierName}",
            .Font = New Font("Poppins SemiBold", 14, FontStyle.Bold),
            .ForeColor = Color.FromArgb(254, 191, 16),
            .AutoSize = False,
            .Size = New Size(editForm.ClientSize.Width - 40, 36),
            .Location = New Point(padLeft, 8),
            .TextAlign = ContentAlignment.MiddleLeft
        }
            editForm.Controls.Add(header)

            y = 56

            Dim AddLabel = Function(text As String, top As Integer) As Label
                               Dim l As New Label() With {
                               .Text = text,
                               .ForeColor = Color.LightGray,
                               .Font = New Font("Poppins", 10),
                               .Location = New Point(padLeft, top),
                               .Size = New Size(labelW, h),
                               .TextAlign = ContentAlignment.MiddleLeft
                           }
                               editForm.Controls.Add(l)
                               Return l
                           End Function

            Dim AddTextBox = Function(value As String, top As Integer) As TextBox
                                 Dim t As New TextBox() With {
                                 .Text = value,
                                 .Location = New Point(padLeft + labelW + 10, top),
                                 .Size = New Size(controlW, h),
                                 .BackColor = Color.White,
                                 .ForeColor = Color.Black,
                                 .BorderStyle = BorderStyle.FixedSingle
                             }
                                 editForm.Controls.Add(t)
                                 Return t
                             End Function

            AddLabel("Supplier ID:", y)
            Dim txtID As TextBox = AddTextBox(supplierId.ToString(), y)
            txtID.ReadOnly = True
            y += 44

            AddLabel("Supplier Code:", y)
            Dim txtCode As TextBox = AddTextBox(supplierCode, y)
            txtCode.ReadOnly = True
            y += 44

            AddLabel("Supplier Name:", y)
            Dim txtName As TextBox = AddTextBox(supplierName, y)
            y += 44

            AddLabel("Contact Person:", y)
            Dim txtContact As TextBox = AddTextBox(contactPerson, y)
            y += 44

            AddLabel("Phone:", y)
            Dim txtPhone As TextBox = AddTextBox(phone, y)
            y += 44

            AddLabel("Email:", y)
            Dim txtEmail As TextBox = AddTextBox(email, y)
            y += 44

            ' Status control (checkbox + label)
            AddLabel("Status:", y)
            Dim chkActive As New CheckBox() With {
            .Location = New Point(padLeft + labelW + 10, y),
            .Size = New Size(20, 20),
            .Checked = isActive,
            .BackColor = Color.Transparent,
            .ForeColor = Color.White
        }
            editForm.Controls.Add(chkActive)

            Dim lblStatusText As New Label() With {
            .Text = If(isActive, "Active", "Inactive"),
            .ForeColor = Color.LightGray,
            .Font = New Font("Poppins", 9),
            .Location = New Point(padLeft + labelW + 36, y - 2),
            .Size = New Size(100, 24),
            .TextAlign = ContentAlignment.MiddleLeft
        }
            editForm.Controls.Add(lblStatusText)
            AddHandler chkActive.CheckedChanged, Sub() lblStatusText.Text = If(chkActive.Checked, "Active", "Inactive")
            y += 54

            ' Right side: recent stock-in DataGridView (widened)
            Dim dgvRecent As New DataGridView() With {
            .Location = New Point(rightColX, 56),
            .Size = New Size(editForm.ClientSize.Width - rightColX - padLeft - 10, editForm.ClientSize.Height - 180),
            .ReadOnly = True,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .RowHeadersVisible = False,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .BackgroundColor = Color.FromArgb(41, 44, 45),
            .DefaultCellStyle = New DataGridViewCellStyle() With {.BackColor = Color.FromArgb(61, 65, 66), .ForeColor = Color.LightGray, .SelectionBackColor = Color.FromArgb(255, 204, 77), .SelectionForeColor = Color.Black}
        }
            editForm.Controls.Add(dgvRecent)

            dgvRecent.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "CreatedAt", .HeaderText = "Date", .FillWeight = 30})
            dgvRecent.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Product", .HeaderText = "Product", .FillWeight = 45})
            dgvRecent.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Quantity", .HeaderText = "Qty", .FillWeight = 12})
            dgvRecent.Columns.Add(New DataGridViewTextBoxColumn() With {.Name = "Reference", .HeaderText = "Reference", .FillWeight = 25})

            ' Load last 12 stock-in entries for this supplier
            Try
                Dim stockQuery As String = "SELECT TOP 12 il.CreatedAt, ISNULL(p.ProductName, '') AS ProductName, il.Quantity, ISNULL(il.Reference, '') AS Reference " &
                                       "FROM InventoryLog il LEFT JOIN Products p ON il.ProductID = p.ProductID " &
                                       "WHERE il.SupplierID = @SupplierID AND (LOWER(il.TransactionType) = 'in' OR il.TransactionType IN ('IN','INBOUND','Stock In','stock in')) " &
                                       "ORDER BY il.CreatedAt DESC"
                Using reader As SqlDataReader = Utilities.ExecuteReader(stockQuery, New SqlParameter() {New SqlParameter("@SupplierID", supplierId)})
                    While reader.Read()
                        Dim dt As DateTime = If(IsDBNull(reader("CreatedAt")), DateTime.MinValue, Convert.ToDateTime(reader("CreatedAt")))
                        Dim prod As String = If(IsDBNull(reader("ProductName")), "", reader("ProductName").ToString())
                        Dim qty As String = If(IsDBNull(reader("Quantity")), "0", reader("Quantity").ToString())
                        Dim ref As String = If(IsDBNull(reader("Reference")), "", reader("Reference").ToString())

                        dgvRecent.Rows.Add(dt.ToString("MM/dd/yyyy HH:mm"), prod, qty, ref)
                    End While
                End Using
            Catch
                ' ignore recent list errors
            End Try

            ' Save / Export / Cancel buttons - prominent
            Dim btnSave As New Button() With {
            .Text = "Save",
            .Size = New Size(120, 36),
            .Location = New Point(editForm.ClientSize.Width - 380, editForm.ClientSize.Height - 70),
            .BackColor = Color.FromArgb(16, 216, 98),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Poppins", 10, FontStyle.Regular)
        }

            Dim btnExport As New Button() With {
            .Text = "Export",
            .Size = New Size(120, 36),
            .Location = New Point(editForm.ClientSize.Width - 255, editForm.ClientSize.Height - 70),
            .BackColor = Color.FromArgb(74, 79, 84),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Poppins", 10, FontStyle.Regular)
        }

            Dim btnCancel As New Button() With {
            .Text = "Cancel",
            .Size = New Size(120, 36),
            .Location = New Point(editForm.ClientSize.Width - 130, editForm.ClientSize.Height - 70),
            .BackColor = Color.FromArgb(255, 204, 77),
            .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat,
            .Font = New Font("Poppins", 10, FontStyle.Regular)
        }

            editForm.Controls.Add(btnSave)
            editForm.Controls.Add(btnExport)
            editForm.Controls.Add(btnCancel)
            AddHandler btnCancel.Click, Sub() editForm.Close()

            ' Export handler for recent list (CSV)
            AddHandler btnExport.Click, Sub()
                                            Try
                                                If dgvRecent.Rows.Count = 0 Then
                                                    MessageBox.Show("No recent stock-in records to export.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                    Return
                                                End If

                                                Using sfd As New SaveFileDialog()
                                                    sfd.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*"
                                                    sfd.FileName = $"Supplier_{supplierCode}_StockIns_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
                                                    If sfd.ShowDialog() = DialogResult.OK Then
                                                        Using sw As New IO.StreamWriter(sfd.FileName, False, System.Text.Encoding.UTF8)
                                                            ' Header
                                                            sw.WriteLine("Date,Product,Quantity,Reference")
                                                            For Each r As DataGridViewRow In dgvRecent.Rows
                                                                If r.IsNewRow Then Continue For
                                                                Dim dateVal = r.Cells("CreatedAt").Value?.ToString().Replace(","c, " ")
                                                                Dim prodVal = r.Cells("Product").Value?.ToString().Replace(","c, " ")
                                                                Dim qtyVal = r.Cells("Quantity").Value?.ToString().Replace(","c, " ")
                                                                Dim refVal = r.Cells("Reference").Value?.ToString().Replace(","c, " ")
                                                                sw.WriteLine($"{dateVal},{prodVal},{qtyVal},{refVal}")
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

            ' Save handler - update DB and grid
            AddHandler btnSave.Click, Sub()
                                          Try
                                              If String.IsNullOrWhiteSpace(txtName.Text) Then
                                                  MessageBox.Show("Supplier name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                  txtName.Focus()
                                                  Return
                                              End If

                                              Dim updateQuery As String = "UPDATE Suppliers SET SupplierName = @Name, ContactPerson = @Contact, Phone = @Phone, Email = @Email, IsActive = @IsActive WHERE SupplierID = @SupplierID"
                                              Dim parms As SqlParameter() = {
                                              New SqlParameter("@Name", txtName.Text.Trim()),
                                              New SqlParameter("@Contact", txtContact.Text.Trim()),
                                              New SqlParameter("@Phone", txtPhone.Text.Trim()),
                                              New SqlParameter("@Email", txtEmail.Text.Trim()),
                                              New SqlParameter("@IsActive", If(chkActive.Checked, 1, 0)),
                                              New SqlParameter("@SupplierID", supplierId)
                                          }

                                              Dim rowsAffected As Integer = Utilities.ExecuteNonQuery(updateQuery, parms)
                                              If rowsAffected > 0 Then
                                                  ' Update grid row values if rowIndex provided
                                                  If rowIndex >= 0 AndAlso rowIndex < InventoryLogDataGrid.Rows.Count Then
                                                      InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierName").Value = txtName.Text.Trim()
                                                      InventoryLogDataGrid.Rows(rowIndex).Cells("ContactPerson").Value = txtContact.Text.Trim()
                                                      InventoryLogDataGrid.Rows(rowIndex).Cells("Phone").Value = txtPhone.Text.Trim()
                                                      InventoryLogDataGrid.Rows(rowIndex).Cells("Email").Value = txtEmail.Text.Trim()
                                                      ' update tag
                                                      Dim tag = TryCast(InventoryLogDataGrid.Rows(rowIndex).Tag, Dictionary(Of String, Object))
                                                      If tag IsNot Nothing Then
                                                          tag("SupplierName") = txtName.Text.Trim()
                                                          tag("ContactPerson") = txtContact.Text.Trim()
                                                          tag("Phone") = txtPhone.Text.Trim()
                                                          tag("Email") = txtEmail.Text.Trim()
                                                          tag("IsActive") = chkActive.Checked
                                                      End If
                                                  End If

                                                  Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Supplier Updated", $"SupplierID {supplierId} updated.")
                                                  MessageBox.Show("Supplier updated successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                  editForm.Close()
                                              Else
                                                  MessageBox.Show("No changes saved.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                              End If
                                          Catch ex As Exception
                                              MessageBox.Show($"Error saving supplier: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                          End Try
                                      End Sub

            editForm.ShowDialog()
            editForm.Dispose()
        Catch ex As Exception
            MessageBox.Show($"Error opening edit modal: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Exportbtn_Click_1(sender As Object, e As EventArgs) Handles Exportbtn.Click
        Try
            ' Prevent double-clicks
            Exportbtn.Enabled = False

            ' Determine sort order from UI
            Dim sortOrder As String = ""
            If SortBy IsNot Nothing AndAlso SortBy.SelectedItem IsNot Nothing Then
                sortOrder = SortBy.SelectedItem.ToString()
            End If

            ' Determine filter type if a filter control exists (fallback to All Suppliers)
            Dim filterType As String = "All Suppliers"
            If Me.Controls IsNot Nothing AndAlso Me.Controls.ContainsKey("FilterType") Then
                Dim ctrl = Me.Controls("FilterType")
                If TypeOf ctrl Is ComboBox Then
                    Dim cb = DirectCast(ctrl, ComboBox)
                    If cb.SelectedItem IsNot Nothing Then
                        filterType = cb.SelectedItem.ToString()
                    End If
                End If
            End If

            ' Determine optional date filter if a date picker is present (keeps parity with other forms)
            Dim filterDate As DateTime? = Nothing
            If Me.Controls IsNot Nothing AndAlso Me.Controls.ContainsKey("Guna2DateTimePicker1") Then
                Dim dtp = TryCast(Me.Controls("Guna2DateTimePicker1"), DateTimePicker)
                If dtp IsNot Nothing Then
                    If Not dtp.ShowCheckBox OrElse dtp.Checked Then
                        filterDate = dtp.Value.Date
                    End If
                End If
            End If

            ' Call supplier exporter (keeps PDF layout from SalesRecord exporter but with supplier data)
            SupplierExporter.ExportOrderRecordsReport(sortOrder, filterType, filterDate)

        Catch ex As Exception
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Supplier Export Failed", $"Error: {ex.Message}")
            End If
        Finally
            Exportbtn.Enabled = True
        End Try
    End Sub

    Private Sub Supplier_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        IdleTimeoutManager.Instance.StopMonitoring(Me)
        If isNavigating Then
            Return
        End If
        ' Skip exit confirmation when hosted in MainShell
        If IsHostedInMainShell() Then
            Return
        End If
        ' Show confirmation only for user-initiated close (X button)
        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = EscForm.ConfirmExit(Me)

            If result = DialogResult.Yes Then
                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then
                        form.Close()
                    End If
                Next
                Application.Exit()
            Else
                e.Cancel = True
            End If
        End If
    End Sub

    Private Function IsHostedInMainShell() As Boolean
        Dim parent As Control = Me.Parent
        While parent IsNot Nothing
            If TypeOf parent Is MainShell Then
                Return True
            End If
            parent = parent.Parent
        End While
        Return False
    End Function

    Private Function GetMainShell() As MainShell
        Dim parent As Control = Me.Parent
        While parent IsNot Nothing
            If TypeOf parent Is MainShell Then
                Return CType(parent, MainShell)
            End If
            parent = parent.Parent
        End While
        Return Nothing
    End Function

    Private Sub SetDoubleBuffered(ctrl As Control)
        Try
            Dim prop = ctrl.GetType().GetProperty("DoubleBuffered", BindingFlags.Instance Or BindingFlags.NonPublic)
            If prop IsNot Nothing Then prop.SetValue(ctrl, True, Nothing)
        Catch
        End Try
    End Sub

    Private Sub DashboardPanel_Paint(sender As Object, e As PaintEventArgs) Handles DashboardPanel.Paint

    End Sub

    Private Sub SortBy_SelectedIndexChanged_1(sender As Object, e As EventArgs) Handles SortBy.SelectedIndexChanged

    End Sub
End Class