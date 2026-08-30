Imports System.Data.Common
Imports System.Linq
Imports System.Reflection

Public Class Supplier
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

    Private _supplierSearchTimer As Timer
    Private _pagination As PaginationControl
    Private _currentPage As Integer = 1
    Private _pageSize As Integer = 15
    Private _allSuppliers As New List(Of Dictionary(Of String, Object))
    Private _currentSearch As String = ""
    Private _currentFilter As String = ""
    Private _filterPlaceholder As Label

    Private Sub Supplier_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.BackColor = Color.FromArgb(248, 248, 247)
        NavigationPanel.Location = New Point(-10, 5)
        Try
            Me.KeyPreview = True

            If Not IsHostedInMainShell() Then
                Me.FormBorderStyle = FormBorderStyle.None
                Me.WindowState = FormWindowState.Maximized
            End If

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

            SetDoubleBuffered(InventoryLogDataGrid)

            IdleTimeoutManager.Instance.StartMonitoring(Me)
            InitializeProfileSection()

            NavigationBuilder.Build(NavigationPanel, Me, "Supplier")

            InitializeDataGridView()

            ' Wire search with debounce
            _supplierSearchTimer = New Timer()
            _supplierSearchTimer.Interval = 400
            _supplierSearchTimer.Enabled = False
            AddHandler _supplierSearchTimer.Tick, AddressOf SupplierSearchTimer_Tick
            AddHandler TxtSearch.TextChanged, AddressOf TxtSearch_TextChanged

            ' Wire sort
            InitializeSortComboBox()
            AddHandler SortBy.SelectedIndexChanged, AddressOf SortBy_SelectedIndexChanged
            AddFilterPlaceholder()

            ' Wire export
            AddHandler Exportbtn.Click, AddressOf Exportbtn_Click

            ' Wire add supplier
            AddHandler Guna2Button1.Click, AddressOf Guna2Button1_Click

            LoadSuppliersData()
            SetupPagination()

            Me.Activate()
            Me.Focus()

            AlignDataGridViewToPanel()
            SetupTabIndex()
        Catch ex As Exception
            MessageBox.Show($"Error initializing Suppliers page: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SetupTabIndex()
        TxtSearch.TabIndex = 0
        SortBy.TabIndex = 1
        Exportbtn.TabIndex = 2
        Utilities.ApplyInputFocusEffects(Me)
    End Sub

    Private Sub AlignDataGridViewToPanel()
        Try
            If Guna2Panel1 Is Nothing OrElse InventoryLogDataGrid Is Nothing Then Return
            Dim gridTop As Integer = 72
            Dim paginationHeight As Integer = If(_pagination IsNot Nothing AndAlso _pagination.Visible, _pagination.Height + 4, 0)
            Dim availableHeight As Integer = Guna2Panel1.Height - gridTop - paginationHeight
            InventoryLogDataGrid.Location = New Point(8, gridTop)
            InventoryLogDataGrid.Width = Guna2Panel1.Width - 16
            If availableHeight > 100 Then
                InventoryLogDataGrid.Height = availableHeight
            End If
            If _pagination IsNot Nothing AndAlso _pagination.Visible Then
                _pagination.Location = New Point(4, Guna2Panel1.Height - _pagination.Height - 2)
                _pagination.Width = Guna2Panel1.Width - 8
            End If
        Catch
        End Try
    End Sub

    Private Sub Supplier_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        AlignDataGridViewToPanel()
    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            If isNavigating Then Return True
            If Me.OwnedForms.Cast(Of Form)().Any(Function(f) f.Visible) Then Return MyBase.ProcessCmdKey(msg, keyData)
            If Application.OpenForms.Cast(Of Form)().Any(Function(f) f IsNot Me AndAlso f.Visible AndAlso f.Modal) Then Return MyBase.ProcessCmdKey(msg, keyData)
            If Not Me.ContainsFocus Then Return MyBase.ProcessCmdKey(msg, keyData)

            Dim result As DialogResult = EscForm.ConfirmExit(Me)
            Me.Activate()
            If result = DialogResult.Yes Then
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Supplier.")
                End If
                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then form.Close()
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
            InventoryLogDataGrid.AutoGenerateColumns = False
            InventoryLogDataGrid.AllowUserToAddRows = False
            InventoryLogDataGrid.AllowUserToDeleteRows = False
            InventoryLogDataGrid.ReadOnly = True
            InventoryLogDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            InventoryLogDataGrid.MultiSelect = False
            InventoryLogDataGrid.ScrollBars = ScrollBars.None
            InventoryLogDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            InventoryLogDataGrid.RowHeadersVisible = False
            InventoryLogDataGrid.EnableHeadersVisualStyles = False

            InventoryLogDataGrid.BackgroundColor = Color.FromArgb(250, 249, 246)
            InventoryLogDataGrid.GridColor = Color.FromArgb(220, 220, 220)
            InventoryLogDataGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal

            InventoryLogDataGrid.DefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = Color.White,
                .ForeColor = Color.FromArgb(51, 51, 51),
                .SelectionBackColor = Color.FromArgb(235, 228, 200),
                .SelectionForeColor = Color.FromArgb(51, 51, 51),
                .Font = New Font("Poppins", 9.0F, FontStyle.Regular),
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Padding = New Padding(8, 6, 8, 6)
            }

            InventoryLogDataGrid.AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = Color.FromArgb(250, 249, 246)
            }

            InventoryLogDataGrid.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = Color.FromArgb(250, 249, 246),
                .ForeColor = Color.FromArgb(68, 68, 68),
                .SelectionBackColor = Color.FromArgb(250, 249, 246),
                .SelectionForeColor = Color.FromArgb(68, 68, 68),
                .Font = New Font("Poppins", 8.5F, FontStyle.Bold),
                .Alignment = DataGridViewContentAlignment.MiddleCenter
            }
            InventoryLogDataGrid.ColumnHeadersHeight = 44
            InventoryLogDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            InventoryLogDataGrid.RowTemplate.Height = 50

            InventoryLogDataGrid.AllowUserToResizeColumns = False
            InventoryLogDataGrid.AllowUserToResizeRows = False
            InventoryLogDataGrid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "SupplierID", .HeaderText = "ID", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 5
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "SupplierCode", .HeaderText = "Code", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 8
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "SupplierName", .HeaderText = "Supplier Name", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {
                    .Alignment = DataGridViewContentAlignment.MiddleCenter,
                    .Padding = New Padding(10, 6, 10, 6),
                    .Font = New Font("Poppins SemiBold", 9.0F, FontStyle.Regular),
                    .ForeColor = Color.FromArgb(51, 51, 51),
                    .WrapMode = DataGridViewTriState.False
                },
                .FillWeight = 36
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "ContactPerson", .HeaderText = "Contact Person", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 18
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "Phone", .HeaderText = "Phone", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 10
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "Email", .HeaderText = "Email", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {
                    .Alignment = DataGridViewContentAlignment.MiddleCenter,
                    .WrapMode = DataGridViewTriState.False
                },
                .FillWeight = 18
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "StockIns", .HeaderText = "Stock In Count", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 8
            })

            Dim actionCol As New DataGridViewTextBoxColumn() With {
                .Name = "Action", .HeaderText = "", .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {
                    .Alignment = DataGridViewContentAlignment.MiddleCenter,
                    .Font = New Font("Segoe UI Emoji", 12, FontStyle.Regular),
                    .ForeColor = Color.FromArgb(51, 51, 51)
                },
                .FillWeight = 5
            }
            InventoryLogDataGrid.Columns.Add(actionCol)

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

    Private Sub SetupPagination()
        If _pagination Is Nothing Then
            _pagination = New PaginationControl()
            AddHandler _pagination.PageChanged, AddressOf OnPaginationPageChanged
            Guna2Panel1.Controls.Add(_pagination)
            _pagination.BringToFront()
        End If
        _pagination.Width = Guna2Panel1.Width - 8
        _pagination.Location = New Point(4, Guna2Panel1.Height - _pagination.Height - 2)
        _pagination.Configure(_allSuppliers.Count, _pageSize, _currentPage)
        _currentPage = _pagination.CurrentPage
        _pagination.Visible = _allSuppliers.Count > 0
        RenderCurrentPage()
        AlignDataGridViewToPanel()
    End Sub

    Private Sub OnPaginationPageChanged(page As Integer)
        _currentPage = page
        RenderCurrentPage()
    End Sub

    Private Sub RenderCurrentPage()
        Try
            InventoryLogDataGrid.Rows.Clear()
            If _allSuppliers Is Nothing OrElse _allSuppliers.Count = 0 Then Return

            Dim pageItems = _allSuppliers.Skip((_currentPage - 1) * _pageSize).Take(_pageSize)
            For Each s In pageItems
                Dim supplierId As Integer = Convert.ToInt32(s("SupplierID"))
                Dim rowIndex As Integer = InventoryLogDataGrid.Rows.Add()
                InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierID").Value = supplierId
                InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierCode").Value = s("SupplierCode").ToString()
                InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierName").Value = s("SupplierName").ToString()
                InventoryLogDataGrid.Rows(rowIndex).Cells("ContactPerson").Value = s("ContactPerson").ToString()
                InventoryLogDataGrid.Rows(rowIndex).Cells("Phone").Value = s("Phone").ToString()
                InventoryLogDataGrid.Rows(rowIndex).Cells("Email").Value = s("Email").ToString()
                InventoryLogDataGrid.Rows(rowIndex).Cells("StockIns").Value = s("StockIns").ToString()
                InventoryLogDataGrid.Rows(rowIndex).Cells("Action").Value = ChrW(&H270F)
                InventoryLogDataGrid.Rows(rowIndex).Tag = s
            Next
            InventoryLogDataGrid.ClearSelection()
        Catch
        End Try
    End Sub

    Private Sub InitializeSortComboBox()
        SortBy.Items.Clear()
        SortBy.Items.Add("Active")
        SortBy.Items.Add("Inactive")
        SortBy.SelectedIndex = -1
    End Sub

    Private Sub AddFilterPlaceholder()
        Dim arrowWidth As Integer = 30
        _filterPlaceholder = New Label() With {
            .Text = "🔍  Filter",
            .Font = SortBy.Font,
            .ForeColor = Color.DarkGray,
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(SortBy.Width - arrowWidth - 10, SortBy.Height - 4),
            .Location = New Point(SortBy.Location.X + 5, SortBy.Location.Y + 2),
            .TextAlign = ContentAlignment.MiddleLeft,
            .Cursor = Cursors.Hand,
            .Enabled = False
        }
        SortBy.Parent.Controls.Add(_filterPlaceholder)
        _filterPlaceholder.BringToFront()
        UpdateFilterPlaceholder()
        AddHandler SortBy.SelectedIndexChanged, Sub(s, e) UpdateFilterPlaceholder()
    End Sub

    Private Sub UpdateFilterPlaceholder()
        If _filterPlaceholder IsNot Nothing Then
            _filterPlaceholder.Visible = (SortBy.SelectedIndex = -1)
        End If
    End Sub

    Private Sub TxtSearch_TextChanged(sender As Object, e As EventArgs)
        _supplierSearchTimer.Stop()
        _supplierSearchTimer.Start()
    End Sub

    Private Sub SupplierSearchTimer_Tick(sender As Object, e As EventArgs)
        _supplierSearchTimer.Stop()
        _currentSearch = TxtSearch.Text.Trim()
        _currentPage = 1
        LoadSuppliersData()
        SetupPagination()
    End Sub

    Private Sub SortBy_SelectedIndexChanged(sender As Object, e As EventArgs)
        If SortBy.SelectedItem Is Nothing Then
            _currentFilter = ""
        Else
            _currentFilter = SortBy.SelectedItem.ToString()
        End If
        _currentPage = 1
        LoadSuppliersData()
        SetupPagination()
    End Sub

    Private Sub LoadSuppliersData()
        Try
            _allSuppliers.Clear()

            Dim query As String = "SELECT SupplierID, SupplierCode, SupplierName, ContactPerson, Phone, Email, IsActive FROM Suppliers"
            Dim params As New List(Of SqlParameter)()
            Dim conditions As New List(Of String)()

            If Not String.IsNullOrWhiteSpace(_currentSearch) Then
                conditions.Add("(SupplierName LIKE @SearchText OR ContactPerson LIKE @SearchText OR SupplierCode LIKE @SearchText)")
                params.Add(New SqlParameter("@SearchText", "%" & _currentSearch & "%"))
            End If

            If _currentFilter = "Active" Then
                conditions.Add("IsActive = 1")
            ElseIf _currentFilter = "Inactive" Then
                conditions.Add("IsActive = 0")
            End If

            If conditions.Count > 0 Then
                query += " WHERE " & String.Join(" AND ", conditions)
            End If

            query += " ORDER BY SupplierName ASC"

            Using reader As DbDataReader = Utilities.ExecuteReader(query, params.ToArray())
                While reader.Read()
                    Dim supplierId As Integer = Convert.ToInt32(reader("SupplierID"))
                    Dim stockIns As Integer = GetSupplierStockInCount(supplierId)
                    _allSuppliers.Add(New Dictionary(Of String, Object) From {
                        {"SupplierID", supplierId},
                        {"SupplierCode", If(IsDBNull(reader("SupplierCode")), "", reader("SupplierCode").ToString())},
                        {"SupplierName", If(IsDBNull(reader("SupplierName")), "", reader("SupplierName").ToString())},
                        {"ContactPerson", If(IsDBNull(reader("ContactPerson")), "", reader("ContactPerson").ToString())},
                        {"Phone", If(IsDBNull(reader("Phone")), "", reader("Phone").ToString())},
                        {"Email", If(IsDBNull(reader("Email")), "", reader("Email").ToString())},
                        {"StockIns", stockIns},
                        {"IsActive", If(IsDBNull(reader("IsActive")), True, Convert.ToBoolean(reader("IsActive")))}
                    })
                End While
            End Using
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
            If result Is Nothing OrElse IsDBNull(result) Then Return 0
            Return Convert.ToInt32(result)
        Catch
            Return 0
        End Try
    End Function

    Private Sub InventoryLogDataGrid_CellMouseEnter(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If InventoryLogDataGrid Is Nothing Then Return
            If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 Then
                If InventoryLogDataGrid.Columns(e.ColumnIndex).Name = "Action" Then
                    InventoryLogDataGrid.Cursor = Cursors.Hand
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub InventoryLogDataGrid_CellMouseLeave(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If InventoryLogDataGrid Is Nothing Then Return
            If e.RowIndex >= 0 AndAlso e.ColumnIndex >= 0 Then
                If InventoryLogDataGrid.Columns(e.ColumnIndex).Name = "Action" Then
                    InventoryLogDataGrid.Cursor = Cursors.Default
                End If
            End If
        Catch
        End Try
    End Sub

    Private Sub InventoryLogDataGrid_CellClick(sender As Object, e As DataGridViewCellEventArgs)
        Try
            If e.RowIndex < 0 OrElse InventoryLogDataGrid Is Nothing Then Return
            If e.ColumnIndex < 0 OrElse e.ColumnIndex >= InventoryLogDataGrid.Columns.Count Then Return

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
            isNavigating = True
            Dim profileForm As New ProfileSettings()
            profileForm.StartPosition = FormStartPosition.CenterScreen
            profileForm.Show()
            If Not IsHostedInMainShell() Then Me.Close()
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open Profile Settings: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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

            Dim accentGold As Color = Color.FromArgb(196, 154, 48)
            Dim textColor As Color = Color.FromArgb(51, 51, 51)
            Dim labelColor As Color = Color.FromArgb(80, 80, 80)
            Dim inputBg As Color = Color.FromArgb(250, 249, 246)
            Dim cr As Integer = 10

            Dim editForm As New Form() With {
                .Text = $"Edit Supplier - {supplierName}",
                .Size = New Size(900, 590),
                .StartPosition = FormStartPosition.CenterParent,
                .FormBorderStyle = FormBorderStyle.FixedToolWindow,
                .BackColor = Color.White,
                .ShowInTaskbar = False
            }

            Dim px As Integer = 30
            Dim leftColW As Integer = 320
            Dim rightColX As Integer = px + leftColW + 24
            Dim y As Integer = 24
            Dim labelW As Integer = 120
            Dim inputW As Integer = leftColW - labelW - 10
            Dim controlH As Integer = 38
            Dim spacing As Integer = 16

            Dim lblHeader As New Label() With {
                .Text = $"Edit Supplier - {supplierName}",
                .Font = New Font("Poppins SemiBold", 14, FontStyle.Bold),
                .ForeColor = accentGold,
                .Location = New Point(px, y),
                .AutoSize = True
            }
            editForm.Controls.Add(lblHeader)
            y += 40

            Dim sep As New Panel() With {
                .Location = New Point(px, y),
                .Size = New Size(leftColW + 20, 1),
                .BackColor = Color.FromArgb(235, 234, 230)
            }
            editForm.Controls.Add(sep)
            y += 14

            Dim MakeLabel = Function(text As String, top As Integer) As Label
                                Dim l As New Label() With {
                                    .Text = text,
                                    .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
                                    .ForeColor = labelColor,
                                    .Location = New Point(px, top),
                                    .Size = New Size(labelW, 22),
                                    .TextAlign = ContentAlignment.MiddleLeft
                                }
                                editForm.Controls.Add(l)
                                Return l
                            End Function

            Dim MakeInput = Function(value As String, top As Integer) As Guna.UI2.WinForms.Guna2TextBox
                                Dim t As New Guna.UI2.WinForms.Guna2TextBox() With {
                                    .Text = value,
                                    .Location = New Point(px + labelW + 10, top),
                                    .Size = New Size(inputW, controlH),
                                    .BackColor = Color.Transparent,
                                    .FillColor = inputBg,
                                    .ForeColor = textColor,
                                    .PlaceholderForeColor = Color.FromArgb(170, 170, 170),
                                    .BorderRadius = cr,
                                    .BorderThickness = 1,
                                    .BorderColor = Color.FromArgb(220, 220, 220),
                                    .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
                                    .Cursor = Cursors.IBeam
                                }
                                t.FocusedState.BorderColor = Color.FromArgb(232, 232, 232)
                                t.HoverState.BorderColor = Color.FromArgb(232, 232, 232)
                                editForm.Controls.Add(t)
                                Return t
                            End Function

            MakeLabel("Supplier ID:", y)
            Dim txtID = MakeInput(supplierId.ToString(), y)
            txtID.ReadOnly = True
            y += controlH + spacing

            MakeLabel("Supplier Code:", y)
            Dim txtCode = MakeInput(supplierCode, y)
            txtCode.ReadOnly = True
            y += controlH + spacing

            MakeLabel("Supplier Name:", y)
            Dim txtName = MakeInput(supplierName, y)
            y += controlH + spacing

            MakeLabel("Contact Person:", y)
            Dim txtContact = MakeInput(contactPerson, y)
            y += controlH + spacing

            MakeLabel("Phone:", y)
            Dim txtPhone = MakeInput(phone, y)
            y += controlH + spacing

            MakeLabel("Email:", y)
            Dim txtEmail = MakeInput(email, y)
            y += controlH + spacing

            MakeLabel("Status:", y)
            Dim chkActive As New Guna.UI2.WinForms.Guna2ToggleSwitch() With {
                .Location = New Point(px + labelW + 10, y + 2),
                .Size = New Size(44, 22),
                .Checked = isActive
            }
            chkActive.CheckedState.FillColor = accentGold
            editForm.Controls.Add(chkActive)

            Dim lblStatusText As New Label() With {
                .Text = If(isActive, "Active", "Inactive"),
                .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
                .ForeColor = If(isActive, Color.FromArgb(46, 125, 50), Color.FromArgb(180, 60, 60)),
                .Location = New Point(px + labelW + 64, y + 2),
                .AutoSize = True
            }
            editForm.Controls.Add(lblStatusText)
            AddHandler chkActive.CheckedChanged, Sub()
                                                     lblStatusText.Text = If(chkActive.Checked, "Active", "Inactive")
                                                     lblStatusText.ForeColor = If(chkActive.Checked, Color.FromArgb(46, 125, 50), Color.FromArgb(180, 60, 60))
                                                 End Sub

            ' ── Recent stock-ins grid (right column) ──
            Dim dgvRecent As New DataGridView() With {
                .Location = New Point(rightColX, 56),
                .Size = New Size(editForm.ClientSize.Width - rightColX - px - 10, editForm.ClientSize.Height - 130),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right,
                .ReadOnly = True,
                .AllowUserToAddRows = False,
                .AllowUserToDeleteRows = False,
                .RowHeadersVisible = False,
                .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                .BackgroundColor = Color.FromArgb(250, 249, 246),
                .BorderStyle = BorderStyle.None,
                .DefaultCellStyle = New DataGridViewCellStyle() With {
                    .BackColor = Color.White,
                    .ForeColor = Color.FromArgb(51, 51, 51),
                    .SelectionBackColor = Color.FromArgb(235, 228, 200),
                    .SelectionForeColor = Color.FromArgb(51, 51, 51)
                }
            }
            editForm.Controls.Add(dgvRecent)

            dgvRecent.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "CreatedAt", .HeaderText = "Date",
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 30
            })
            dgvRecent.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "Product", .HeaderText = "Product",
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter, .Font = New Font("Poppins SemiBold", 9)},
                .FillWeight = 45
            })
            dgvRecent.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "Quantity", .HeaderText = "Qty",
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 12
            })
            dgvRecent.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "Reference", .HeaderText = "Reference",
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter},
                .FillWeight = 25
            })

            dgvRecent.EnableHeadersVisualStyles = False
            dgvRecent.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
                .BackColor = Color.FromArgb(250, 249, 246),
                .ForeColor = Color.FromArgb(51, 51, 51),
                .Font = New Font("Poppins SemiBold", 10),
                .Alignment = DataGridViewContentAlignment.MiddleCenter
            }
            dgvRecent.ColumnHeadersHeight = 40

            Try
                Dim stockQuery As String = "SELECT il.CreatedAt, IFNULL(p.ProductName, '') AS ProductName, il.Quantity, IFNULL(il.Reference, '') AS Reference " &
                                       "FROM InventoryLog il LEFT JOIN Products p ON il.ProductID = p.ProductID " &
                                       "WHERE il.SupplierID = @SupplierID AND (LOWER(il.TransactionType) = 'in' OR il.TransactionType IN ('IN','INBOUND','Stock In','stock in')) " &
                                       "ORDER BY il.CreatedAt DESC LIMIT 12"
                Using reader As DbDataReader = Utilities.ExecuteReader(stockQuery, New SqlParameter() {New SqlParameter("@SupplierID", supplierId)})
                    While reader.Read()
                        Dim dt As DateTime = If(IsDBNull(reader("CreatedAt")), DateTime.MinValue, Convert.ToDateTime(reader("CreatedAt")))
                        Dim prod As String = If(IsDBNull(reader("ProductName")), "", reader("ProductName").ToString())
                        Dim qty As String = If(IsDBNull(reader("Quantity")), "0", reader("Quantity").ToString())
                        Dim ref As String = If(IsDBNull(reader("Reference")), "", reader("Reference").ToString())
                        dgvRecent.Rows.Add(dt.ToString("MM/dd/yyyy HH:mm"), prod, qty, ref)
                    End While
                End Using
            Catch
            End Try

            ' ── Buttons at bottom ──
            Dim btnY As Integer = editForm.ClientSize.Height - 70
            Dim btnSave As New Guna.UI2.WinForms.Guna2Button() With {
                .Text = "Save Changes",
                .Size = New Size(150, 40),
                .Location = New Point(editForm.ClientSize.Width - 430, btnY),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
                .FillColor = accentGold,
                .ForeColor = Color.White,
                .Font = New Font("Poppins", 10, FontStyle.Regular),
                .BorderRadius = cr,
                .Cursor = Cursors.Hand
            }
            Dim btnExport As New Guna.UI2.WinForms.Guna2Button() With {
                .Text = "Export",
                .Size = New Size(110, 40),
                .Location = New Point(editForm.ClientSize.Width - 270, btnY),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
                .FillColor = SteelGray,
                .ForeColor = Color.White,
                .Font = New Font("Poppins", 10, FontStyle.Regular),
                .BorderRadius = cr,
                .Cursor = Cursors.Hand
            }
            Dim btnCancel As New Guna.UI2.WinForms.Guna2Button() With {
                .Text = "Cancel",
                .Size = New Size(110, 40),
                .Location = New Point(editForm.ClientSize.Width - 148, btnY),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
                .FillColor = Color.FromArgb(220, 53, 69),
                .ForeColor = Color.White,
                .Font = New Font("Poppins", 10, FontStyle.Regular),
                .BorderRadius = cr,
                .Cursor = Cursors.Hand
            }
            editForm.Controls.Add(btnSave)
            editForm.Controls.Add(btnExport)
            editForm.Controls.Add(btnCancel)
            AddHandler btnCancel.Click, Sub() editForm.Close()

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
                                                MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                            End Try
                                        End Sub

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
                                                  Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Supplier Updated", $"SupplierID {supplierId} updated.")
                                                  MessageBox.Show("Supplier updated successfully.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                  editForm.Close()
                                                  LoadSuppliersData()
                                                  SetupPagination()
                                              Else
                                                  MessageBox.Show("No changes saved.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                              End If
                                          Catch ex As Exception
                                              MessageBox.Show($"Error saving supplier: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                          End Try
                                      End Sub

            Utilities.EnableEscCloseModal(editForm)
            editForm.ShowDialog()
            editForm.Dispose()
        Catch ex As Exception
            MessageBox.Show($"Error opening edit modal: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Guna2Button1_Click(sender As Object, e As EventArgs)
        ShowAddSupplierModal()
    End Sub

    Private Sub ShowAddSupplierModal()
        Try
            Dim accentGold As Color = Color.FromArgb(196, 154, 48)
            Dim textColor As Color = Color.FromArgb(51, 51, 51)
            Dim labelColor As Color = Color.FromArgb(80, 80, 80)
            Dim inputBg As Color = Color.FromArgb(250, 249, 246)
            Dim cr As Integer = 10

            Dim addForm As New Form() With {
                .Text = "Add New Supplier",
                .Size = New Size(500, 400),
                .StartPosition = FormStartPosition.CenterParent,
                .FormBorderStyle = FormBorderStyle.FixedToolWindow,
                .BackColor = Color.White,
                .ShowInTaskbar = False
            }

            Dim px As Integer = 30
            Dim y As Integer = 24
            Dim controlH As Integer = 38
            Dim labelH As Integer = 22
            Dim labelW As Integer = 130
            Dim inputW As Integer = addForm.ClientSize.Width - px * 2 - labelW - 10
            Dim spacing As Integer = 16

            Dim lblHeader As New Label() With {
                .Text = "Add New Supplier",
                .Font = New Font("Poppins SemiBold", 14, FontStyle.Bold),
                .ForeColor = accentGold,
                .Location = New Point(px, y),
                .AutoSize = True
            }
            addForm.Controls.Add(lblHeader)
            y += 40

            Dim sep As New Panel() With {
                .Location = New Point(px, y),
                .Size = New Size(addForm.ClientSize.Width - 60, 1),
                .BackColor = Color.FromArgb(235, 234, 230),
                .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
            }
            addForm.Controls.Add(sep)
            y += 14

            Dim MakeLabel = Function(text As String, top As Integer) As Label
                                Dim l As New Label() With {
                                    .Text = text,
                                    .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
                                    .ForeColor = labelColor,
                                    .Location = New Point(px, top),
                                    .Size = New Size(labelW, labelH),
                                    .TextAlign = ContentAlignment.MiddleLeft
                                }
                                addForm.Controls.Add(l)
                                Return l
                            End Function

            Dim MakeInput = Function(top As Integer) As Guna.UI2.WinForms.Guna2TextBox
                                Dim t As New Guna.UI2.WinForms.Guna2TextBox() With {
                                    .Location = New Point(px + labelW + 10, top),
                                    .Size = New Size(inputW, controlH),
                                    .Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right,
                                    .BackColor = Color.Transparent,
                                    .FillColor = inputBg,
                                    .ForeColor = textColor,
                                    .PlaceholderForeColor = Color.FromArgb(170, 170, 170),
                                    .BorderRadius = cr,
                                    .BorderThickness = 1,
                                    .BorderColor = Color.FromArgb(220, 220, 220),
                                    .Font = New Font("Poppins", 9.5F, FontStyle.Regular),
                                    .Cursor = Cursors.IBeam
                                }
                                t.FocusedState.BorderColor = Color.FromArgb(232, 232, 232)
                                t.HoverState.BorderColor = Color.FromArgb(232, 232, 232)
                                addForm.Controls.Add(t)
                                Return t
                            End Function

            MakeLabel("Supplier Name:", y)
            Dim txtName = MakeInput(y)
            y += controlH + spacing

            MakeLabel("Contact Person:", y)
            Dim txtContact = MakeInput(y)
            y += controlH + spacing

            MakeLabel("Phone:", y)
            Dim txtPhone = MakeInput(y)
            y += controlH + spacing

            MakeLabel("Email:", y)
            Dim txtEmail = MakeInput(y)
            y += controlH + spacing + 8

            Dim btnY As Integer = addForm.ClientSize.Height - 70
            Dim btnSave As New Guna.UI2.WinForms.Guna2Button() With {
                .Text = "Save Supplier",
                .Size = New Size(150, 40),
                .Location = New Point(addForm.ClientSize.Width - 310, btnY),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
                .FillColor = accentGold,
                .ForeColor = Color.White,
                .Font = New Font("Poppins", 10, FontStyle.Regular),
                .BorderRadius = cr,
                .Cursor = Cursors.Hand
            }
            Dim btnCancel As New Guna.UI2.WinForms.Guna2Button() With {
                .Text = "Cancel",
                .Size = New Size(110, 40),
                .Location = New Point(addForm.ClientSize.Width - 150, btnY),
                .Anchor = AnchorStyles.Bottom Or AnchorStyles.Right,
                .FillColor = Color.FromArgb(220, 53, 69),
                .ForeColor = Color.White,
                .Font = New Font("Poppins", 10, FontStyle.Regular),
                .BorderRadius = cr,
                .Cursor = Cursors.Hand
            }
            addForm.Controls.Add(btnSave)
            addForm.Controls.Add(btnCancel)
            AddHandler btnCancel.Click, Sub() addForm.Close()

            AddHandler btnSave.Click, Sub()
                                          Try
                                              If String.IsNullOrWhiteSpace(txtName.Text) Then
                                                  MessageBox.Show("Supplier name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                                  txtName.Focus()
                                                  Return
                                              End If

                                              Dim supplierCode As String = GenerateNextSupplierCode()
                                              Dim insertQuery As String = "INSERT INTO Suppliers (SupplierCode, SupplierName, ContactPerson, Phone, Email, IsActive) " &
                                                                         "VALUES (@SupplierCode, @SupplierName, @ContactPerson, @Phone, @Email, 1)"
                                              Dim parms As SqlParameter() = {
                                                  New SqlParameter("@SupplierCode", supplierCode),
                                                  New SqlParameter("@SupplierName", txtName.Text.Trim()),
                                                  New SqlParameter("@ContactPerson", If(String.IsNullOrWhiteSpace(txtContact.Text), DBNull.Value, CObj(txtContact.Text.Trim()))),
                                                  New SqlParameter("@Phone", If(String.IsNullOrWhiteSpace(txtPhone.Text), DBNull.Value, CObj(txtPhone.Text.Trim()))),
                                                  New SqlParameter("@Email", If(String.IsNullOrWhiteSpace(txtEmail.Text), DBNull.Value, CObj(txtEmail.Text.Trim())))
                                              }

                                              Dim rowsAffected As Integer = Utilities.ExecuteNonQuery(insertQuery, parms)
                                              If rowsAffected > 0 Then
                                                  Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Supplier Created", $"Supplier '{txtName.Text.Trim()}' ({supplierCode}) created.")
                                                  MessageBox.Show($"Supplier added successfully.{vbCrLf}Code: {supplierCode}", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                                  addForm.Close()
                                                  LoadSuppliersData()
                                                  SetupPagination()
                                              Else
                                                  MessageBox.Show("No supplier created.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                              End If
                                          Catch ex As Exception
                                              MessageBox.Show($"Error saving supplier: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                          End Try
                                      End Sub

            Utilities.EnableEscCloseModal(addForm)
            addForm.ShowDialog()
            addForm.Dispose()
        Catch ex As Exception
            MessageBox.Show($"Error opening add supplier form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function GenerateNextSupplierCode() As String
        Try
            Dim query As String = "SELECT IFNULL(MAX(CAST(SUBSTR(SupplierCode, 2) AS INTEGER)), 0) + 1 FROM Suppliers WHERE SupplierCode LIKE 'S%'"
            Dim result As Object = Utilities.ExecuteScalar(query, New SqlParameter() {})
            Dim nextId As Integer = If(result Is Nothing OrElse IsDBNull(result), 1, Convert.ToInt32(result))
            Return "S" & nextId.ToString("D5")
        Catch
            Return "S" & DateTime.Now.Ticks.ToString().Substring(DateTime.Now.Ticks.ToString().Length - 5)
        End Try
    End Function

    Private Sub Exportbtn_Click(sender As Object, e As EventArgs)
        Try
            Exportbtn.Enabled = False

            Dim filterType As String = "All Suppliers"
            Dim filterDate As DateTime? = Nothing

            If _currentFilter = "Active" Then
                filterType = "Active Suppliers"
            ElseIf _currentFilter = "Inactive" Then
                filterType = "Inactive Suppliers"
            End If

            SupplierExporter.ExportOrderRecordsReport("", filterType, filterDate)
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
        If isNavigating Then Return
        If IsHostedInMainShell() Then Return
        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = EscForm.ConfirmExit(Me)
            If result = DialogResult.Yes Then
                For Each form As Form In Application.OpenForms.Cast(Of Form).ToArray()
                    If form IsNot Me Then form.Close()
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
            If TypeOf parent Is MainShell Then Return True
            parent = parent.Parent
        End While
        Return False
    End Function

    Private Function GetMainShell() As MainShell
        Dim parent As Control = Me.Parent
        While parent IsNot Nothing
            If TypeOf parent Is MainShell Then Return CType(parent, MainShell)
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
End Class
