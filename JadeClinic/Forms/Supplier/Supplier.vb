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
    Private _currentSort As String = ""

    ' KPI card labels
    Private _lblTotalValue As Label
    Private _lblTotalSub As Label
    Private _lblActiveValue As Label
    Private _lblActiveSub As Label
    Private _lblStockValue As Label
    Private _lblStockSub As Label

    Private Sub Supplier_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        DashboardPanel.Location = New Point(-10, 5)
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

            NavigationBuilder.Build(DashboardPanel, Me, "Supplier")

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

            ' Wire export
            AddHandler Exportbtn.Click, AddressOf Exportbtn_Click

            LoadSuppliersData()
            BuildKPICards()
            UpdateKPICards()
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
                .ForeColor = Color.FromArgb(51, 51, 51),
                .SelectionBackColor = Color.FromArgb(250, 249, 246),
                .Font = New Font("Poppins SemiBold", 10.5F, FontStyle.Bold),
                .Alignment = DataGridViewContentAlignment.MiddleCenter
            }
            InventoryLogDataGrid.ColumnHeadersHeight = 50
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

    ' Emoji constants (surrogate pairs - can't use ChrW for these)
    Private Const EmojiUsers As String = "👥"
    Private Const EmojiCheck As String = "✅"
    Private Const EmojiPackage As String = "📦"
    Private CircleBg As Color = Color.FromArgb(251, 247, 236)  ' #FBF7EC

    Private Sub BuildKPICards()
        Dim cardY As Integer = 102
        Dim cardH As Integer = 155
        Dim startX As Integer = 242
        Dim totalWidth As Integer = 1650
        Dim cardGap As Integer = 20
        Dim cardW As Integer = (totalWidth - (cardGap * 2)) \ 3
        Dim iconSize As Integer = 60

        Dim cardBg As Color = Color.White
        Dim cardBorder As Color = Color.FromArgb(240, 239, 235)
        Dim hoverBg As Color = Color.FromArgb(255, 254, 249)
        Dim hoverBorder As Color = Color.FromArgb(196, 154, 44)

        ' ── Card 1: Total Suppliers ──
        Dim card1 As New Guna.UI2.WinForms.Guna2Panel() With {
            .Location = New Point(startX, cardY), .Size = New Size(cardW, cardH),
            .FillColor = cardBg, .BorderRadius = 14, .BorderThickness = 1,
            .BorderColor = cardBorder, .Cursor = Cursors.Hand
        }
        Dim icon1 As New Guna.UI2.WinForms.Guna2Panel() With {
            .Location = New Point(22, (cardH - iconSize) \ 2), .Size = New Size(iconSize, iconSize),
            .FillColor = CircleBg, .BorderColor = CircleBg, .BorderThickness = 0, .BorderRadius = iconSize \ 2
        }
        AddHandler icon1.Paint, Sub(s, ev) DrawEmojiOnCircle(ev.Graphics, icon1, EmojiUsers, Color.FromArgb(196, 154, 44))
        card1.Controls.Add(icon1)
        card1.Controls.Add(New Label() With {.Text = "TOTAL SUPPLIERS", .Font = New Font("Poppins", 9.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(119, 119, 119), .BackColor = Color.Transparent, .Location = New Point(86, 18), .AutoSize = True})
        _lblTotalValue = New Label() With {.Text = "0", .Font = New Font("Poppins", 30.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(34, 34, 34), .BackColor = Color.Transparent, .Location = New Point(84, 42), .AutoSize = True}
        card1.Controls.Add(_lblTotalValue)
        _lblTotalSub = New Label() With {.Text = "All suppliers", .Font = New Font("Poppins", 9.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(46, 125, 50), .BackColor = Color.Transparent, .Location = New Point(88, cardH - 30), .AutoSize = True}
        card1.Controls.Add(_lblTotalSub)

        ' ── Card 2: Active Suppliers ──
        Dim card2 As New Guna.UI2.WinForms.Guna2Panel() With {
            .Location = New Point(startX + cardW + cardGap, cardY), .Size = New Size(cardW, cardH),
            .FillColor = cardBg, .BorderRadius = 14, .BorderThickness = 1,
            .BorderColor = cardBorder, .Cursor = Cursors.Hand
        }
        Dim icon2 As New Guna.UI2.WinForms.Guna2Panel() With {
            .Location = New Point(22, (cardH - iconSize) \ 2), .Size = New Size(iconSize, iconSize),
            .FillColor = CircleBg, .BorderColor = CircleBg, .BorderThickness = 0, .BorderRadius = iconSize \ 2
        }
        AddHandler icon2.Paint, Sub(s, ev) DrawEmojiOnCircle(ev.Graphics, icon2, EmojiCheck, Color.FromArgb(46, 125, 50))
        card2.Controls.Add(icon2)
        card2.Controls.Add(New Label() With {.Text = "ACTIVE SUPPLIERS", .Font = New Font("Poppins", 9.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(119, 119, 119), .BackColor = Color.Transparent, .Location = New Point(86, 18), .AutoSize = True})
        _lblActiveValue = New Label() With {.Text = "0", .Font = New Font("Poppins", 30.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(34, 34, 34), .BackColor = Color.Transparent, .Location = New Point(84, 42), .AutoSize = True}
        card2.Controls.Add(_lblActiveValue)
        _lblActiveSub = New Label() With {.Text = "All operational", .Font = New Font("Poppins", 9.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(46, 125, 50), .BackColor = Color.Transparent, .Location = New Point(88, cardH - 30), .AutoSize = True}
        card2.Controls.Add(_lblActiveSub)

        ' ── Card 3: Stock Ins ──
        Dim card3 As New Guna.UI2.WinForms.Guna2Panel() With {
            .Location = New Point(startX + (cardW + cardGap) * 2, cardY), .Size = New Size(cardW, cardH),
            .FillColor = cardBg, .BorderRadius = 14, .BorderThickness = 1,
            .BorderColor = cardBorder, .Cursor = Cursors.Hand
        }
        Dim icon3 As New Guna.UI2.WinForms.Guna2Panel() With {
            .Location = New Point(22, (cardH - iconSize) \ 2), .Size = New Size(iconSize, iconSize),
            .FillColor = CircleBg, .BorderColor = CircleBg, .BorderThickness = 0, .BorderRadius = iconSize \ 2
        }
        AddHandler icon3.Paint, Sub(s, ev) DrawEmojiOnCircle(ev.Graphics, icon3, EmojiPackage, Color.FromArgb(255, 152, 0))
        card3.Controls.Add(icon3)
        card3.Controls.Add(New Label() With {.Text = "STOCK INS", .Font = New Font("Poppins", 9.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(119, 119, 119), .BackColor = Color.Transparent, .Location = New Point(86, 18), .AutoSize = True})
        _lblStockValue = New Label() With {.Text = "0", .Font = New Font("Poppins", 30.0F, FontStyle.Bold), .ForeColor = Color.FromArgb(34, 34, 34), .BackColor = Color.Transparent, .Location = New Point(84, 42), .AutoSize = True}
        card3.Controls.Add(_lblStockValue)
        _lblStockSub = New Label() With {.Text = "Total inbound", .Font = New Font("Poppins", 9.0F, FontStyle.Regular), .ForeColor = Color.FromArgb(119, 119, 119), .BackColor = Color.Transparent, .Location = New Point(88, cardH - 30), .AutoSize = True}
        card3.Controls.Add(_lblStockSub)

        ' Add hover handlers to all cards
        For Each card As Guna.UI2.WinForms.Guna2Panel In {card1, card2, card3}
            Dim c As Color = cardBg
            Dim hb As Color = hoverBg
            Dim hc As Color = hoverBorder
            AddHandler card.MouseEnter, Sub(s, e)
                                            card.FillColor = hb
                                            card.BorderColor = hc
                                        End Sub
            AddHandler card.MouseLeave, Sub(s, e)
                                            card.FillColor = c
                                            card.BorderColor = cardBorder
                                        End Sub
        Next

        Me.Controls.Add(card3)
        Me.Controls.Add(card2)
        Me.Controls.Add(card1)
        card1.BringToFront()
        card2.BringToFront()
        card3.BringToFront()
    End Sub

    Private Sub DrawEmojiOnCircle(g As Graphics, circle As Control, emoji As String, fallbackColor As Color)
        Try
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit
            Dim emojiFontName As String = ResolveEmojiFontFamily()
            Using f As New Font(emojiFontName, 20.0F, FontStyle.Regular)
                Dim sz As Size = TextRenderer.MeasureText(emoji, f)
                Dim x As Integer = (circle.Width - sz.Width) \ 2
                Dim y As Integer = (circle.Height - sz.Height) \ 2
                TextRenderer.DrawText(g, emoji, f, New Point(x, y), fallbackColor)
            End Using
        Catch
        End Try
    End Sub

    Private Function ResolveEmojiFontFamily() As String
        Try
            Dim installed As New HashSet(Of String)()
            For Each family As FontFamily In Drawing.FontFamily.Families
                installed.Add(family.Name)
            Next
            For Each name As String In {"Segoe UI Emoji", "Segoe UI Symbol", "Noto Color Emoji", "Arial"}
                If installed.Contains(name) Then Return name
            Next
        Catch
        End Try
        Return "Segoe UI"
    End Function

    Private Sub UpdateKPICards()
        If _allSuppliers Is Nothing Then Return

        Dim total As Integer = _allSuppliers.Count
        Dim active As Integer = _allSuppliers.Where(Function(s) Convert.ToBoolean(s("IsActive"))).Count()
        Dim stockIns As Integer = _allSuppliers.Sum(Function(s) Convert.ToInt32(s("StockIns")))

        If _lblTotalValue IsNot Nothing Then _lblTotalValue.Text = total.ToString("D2")
        If _lblTotalSub IsNot Nothing Then _lblTotalSub.Text = If(total = 0, "No suppliers yet", $"Active rate: {If(total > 0, CInt(active * 100 / total), 0)}%")

        If _lblActiveValue IsNot Nothing Then _lblActiveValue.Text = active.ToString("D2")
        If _lblActiveSub IsNot Nothing Then _lblActiveSub.Text = If(active = total AndAlso total > 0, "All operational", $"{If(total > 0, CInt(active * 100 / total), 0)}% of total")

        If _lblStockValue IsNot Nothing Then _lblStockValue.Text = stockIns.ToString("N0")
        If _lblStockSub IsNot Nothing Then _lblStockSub.Text = "Total inbound"
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
        SortBy.Items.Add("Name (A-Z)")
        SortBy.Items.Add("Name (Z-A)")
        SortBy.Items.Add("Code (Ascending)")
        SortBy.Items.Add("Code (Descending)")
        SortBy.Items.Add("Status (Active First)")
        SortBy.SelectedIndex = 0
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
        UpdateKPICards()
        SetupPagination()
    End Sub

    Private Sub SortBy_SelectedIndexChanged(sender As Object, e As EventArgs)
        If SortBy.SelectedItem Is Nothing Then Return
        _currentSort = SortBy.SelectedItem.ToString()
        _currentPage = 1
        LoadSuppliersData()
        UpdateKPICards()
        SetupPagination()
    End Sub

    Private Sub LoadSuppliersData()
        Try
            _allSuppliers.Clear()

            Dim query As String = "SELECT SupplierID, SupplierCode, SupplierName, ContactPerson, Phone, Email, IsActive FROM Suppliers"
            Dim params As New List(Of SqlParameter)()

            If Not String.IsNullOrWhiteSpace(_currentSearch) Then
                query += " WHERE SupplierName LIKE @SearchText OR ContactPerson LIKE @SearchText OR SupplierCode LIKE @SearchText"
                params.Add(New SqlParameter("@SearchText", "%" & _currentSearch & "%"))
            End If

            Select Case _currentSort
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
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from Supplier to ProfileSettings")
            End If
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

            Dim editForm As New Form() With {
                .Text = $"Edit Supplier - {supplierName}",
                .Size = New Size(900, 560),
                .StartPosition = FormStartPosition.CenterParent,
                .FormBorderStyle = FormBorderStyle.FixedDialog,
                .BackColor = Color.White
            }

            Dim padLeft As Integer = 20
            Dim leftColW As Integer = 320
            Dim rightColX As Integer = padLeft + leftColW + 24
            Dim y As Integer = 18
            Dim labelW As Integer = 120
            Dim controlW As Integer = leftColW - labelW - 10
            Dim h As Integer = 30

            Dim header As New Label() With {
                .Text = $"Edit Supplier - {supplierName}",
                .Font = New Font("Poppins SemiBold", 14, FontStyle.Bold),
                .ForeColor = GoldenYellow,
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
                                   .ForeColor = Color.FromArgb(51, 51, 51),
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
                                     .ForeColor = Color.FromArgb(51, 51, 51),
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

            AddLabel("Status:", y)
            Dim chkActive As New CheckBox() With {
                .Location = New Point(padLeft + labelW + 10, y),
                .Size = New Size(20, 20),
                .Checked = isActive,
                .BackColor = Color.Transparent
            }
            editForm.Controls.Add(chkActive)

            Dim lblStatusText As New Label() With {
                .Text = If(isActive, "Active", "Inactive"),
                .ForeColor = Color.FromArgb(51, 51, 51),
                .Font = New Font("Poppins", 9),
                .Location = New Point(padLeft + labelW + 36, y - 2),
                .Size = New Size(100, 24),
                .TextAlign = ContentAlignment.MiddleLeft
            }
            editForm.Controls.Add(lblStatusText)
            AddHandler chkActive.CheckedChanged, Sub() lblStatusText.Text = If(chkActive.Checked, "Active", "Inactive")
            y += 54

            Dim dgvRecent As New DataGridView() With {
                .Location = New Point(rightColX, 56),
                .Size = New Size(editForm.ClientSize.Width - rightColX - padLeft - 10, editForm.ClientSize.Height - 180),
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

            Dim btnSave As New Button() With {
                .Text = "Save", .Size = New Size(120, 36),
                .Location = New Point(editForm.ClientSize.Width - 380, editForm.ClientSize.Height - 70),
                .BackColor = SuccessGreen, .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat, .Font = New Font("Poppins", 10, FontStyle.Regular)
            }
            Dim btnExport As New Button() With {
                .Text = "Export", .Size = New Size(120, 36),
                .Location = New Point(editForm.ClientSize.Width - 255, editForm.ClientSize.Height - 70),
                .BackColor = SteelGray, .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat, .Font = New Font("Poppins", 10, FontStyle.Regular)
            }
            Dim btnCancel As New Button() With {
                .Text = "Cancel", .Size = New Size(120, 36),
                .Location = New Point(editForm.ClientSize.Width - 130, editForm.ClientSize.Height - 70),
                .BackColor = GoldenYellow, .ForeColor = DeepCharcoal,
                .FlatStyle = FlatStyle.Flat, .Font = New Font("Poppins", 10, FontStyle.Regular)
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
                                                MessageBox.Show($"Export failed: {ex.Message}", "Export", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
                                                  UpdateKPICards()
                                                  SetupPagination()
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

    Private Sub Exportbtn_Click(sender As Object, e As EventArgs)
        Try
            Exportbtn.Enabled = False

            Dim sortOrder As String = ""
            If SortBy IsNot Nothing AndAlso SortBy.SelectedItem IsNot Nothing Then
                sortOrder = SortBy.SelectedItem.ToString()
            End If

            Dim filterType As String = "All Suppliers"
            Dim filterDate As DateTime? = Nothing

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
