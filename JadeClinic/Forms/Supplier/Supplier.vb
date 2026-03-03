Imports Microsoft.Data.SqlClient

Public Class Supplier
    Private isNavigating As Boolean = False
    Private profileDropdownPanel As Panel = Nothing
    Private isProfileDropdownVisible As Boolean = False
    Private ReadOnly Graphite As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 61, 65, 69)


    Private Sub Supplier_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            Me.BackColor = Color.FromArgb(30, 30, 30)

            ' Validate session
            If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                MessageBox.Show("User session expired. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                frmLoginvb.Show()
                Me.Close()

                Return
            End If

            ' Initialize profile section
            InitializeProfileSection()

            ' Create navigation (match other pages)
            CreateNavigationMenu()

            ' Initialize grid and controls
            InitializeDataGridView()
            InitializeSortComboBox()

            ' Wire events
            AddHandler SortBy.SelectedIndexChanged, AddressOf SortBy_SelectedIndexChanged
            AddHandler Exportbtn.Click, AddressOf Exportbtn_Click
            AddHandler Guna2DateTimePicker1.ValueChanged, AddressOf FilterDateChanged

            ' Load data on UI thread to avoid cross-thread control access
            LoadSuppliersData()
        Catch ex As Exception
            MessageBox.Show($"Error initializing Suppliers page: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Exportbtn_Click(sender As Object, e As EventArgs)
        MessageBox.Show("Export not implemented.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub FilterDateChanged(sender As Object, e As EventArgs)
        ' Suppliers does not really use date filter but keep for parity
        LoadSuppliersData(If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), ""))
    End Sub

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
    ' Profile section (copied minimal behavior from other forms)
    Private Sub InitializeProfileSection()
        Try
            lblUsername.Text = frmLoginvb.LoggedInUsername
            lblUsername.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
            lblUsername.ForeColor = System.Drawing.Color.White

            LoadUserProfilePicture()

            AddHandler Guna2CirclePictureBox5.Click, AddressOf ProfilePicture_Click
            AddHandler lblUsername.Click, AddressOf ProfilePicture_Click

            AddHandler Guna2CirclePictureBox5.MouseEnter, Sub() Guna2CirclePictureBox5.Cursor = Cursors.Hand
            AddHandler lblUsername.MouseEnter, Sub() lblUsername.Cursor = Cursors.Hand

        Catch
            lblUsername.Text = frmLoginvb.LoggedInUsername
        End Try
    End Sub

    Private Sub LoadUserProfilePicture()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Dim query As String = "SELECT Photo FROM Users WHERE Username = @Username"
                Dim parameters As SqlParameter() = {New SqlParameter("@Username", frmLoginvb.LoggedInUsername)}

                Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                    If reader.Read() Then
                        Guna2CirclePictureBox5.SizeMode = PictureBoxSizeMode.Zoom
                        Guna2CirclePictureBox5.BorderStyle = BorderStyle.None

                        If Not IsDBNull(reader("Photo")) Then
                            Dim photoBytes As Byte() = CType(reader("Photo"), Byte())
                            Using ms As New IO.MemoryStream(photoBytes)
                                Guna2CirclePictureBox5.Image = System.Drawing.Image.FromStream(ms)
                            End Using
                        Else
                            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
                        End If
                    End If
                End Using
            End If
        Catch
            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(If(frmLoginvb.LoggedInUsername, "User"))
        End Try
    End Sub

    Private Function CreateDefaultProfileAvatar(username As String) As System.Drawing.Image
        Dim bitmap As New Bitmap(50, 50)
        Using g As Graphics = Graphics.FromImage(bitmap)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            Dim colors() As System.Drawing.Color = {
                System.Drawing.Color.FromArgb(255, 107, 107),
                System.Drawing.Color.FromArgb(78, 205, 196),
                System.Drawing.Color.FromArgb(85, 98, 112),
                System.Drawing.Color.FromArgb(129, 236, 236),
                System.Drawing.Color.FromArgb(116, 185, 255)
            }
            Dim colorIndex As Integer = Math.Abs(username.GetHashCode()) Mod colors.Length
            g.FillEllipse(New SolidBrush(colors(colorIndex)), 0, 0, 50, 50)

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
                g.DrawString(initials, font, Brushes.White, (50 - textSize.Width) / 2, (50 - textSize.Height) / 2)
            End Using
        End Using
        Return bitmap
    End Function

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

        profileDropdownPanel = New Panel()
        profileDropdownPanel.Size = New System.Drawing.Size(200, 100)
        profileDropdownPanel.BackColor = System.Drawing.Color.FromArgb(41, 44, 45)
        profileDropdownPanel.BorderStyle = BorderStyle.FixedSingle

        Dim profileLocation = Guna2CirclePictureBox5.Location
        profileDropdownPanel.Location = New Point(profileLocation.X - 90, profileLocation.Y + Guna2CirclePictureBox5.Height + 5)

        Dim btnProfileSettings As New Label()
        btnProfileSettings.Text = "⚙️ Profile Settings"
        btnProfileSettings.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnProfileSettings.ForeColor = System.Drawing.Color.White
        btnProfileSettings.Size = New System.Drawing.Size(190, 40)
        btnProfileSettings.Location = New System.Drawing.Point(5, 5)
        btnProfileSettings.TextAlign = ContentAlignment.MiddleLeft
        btnProfileSettings.Cursor = Cursors.Hand

        AddHandler btnProfileSettings.MouseEnter, Sub() btnProfileSettings.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        AddHandler btnProfileSettings.MouseLeave, Sub() btnProfileSettings.BackColor = System.Drawing.Color.Transparent
        AddHandler btnProfileSettings.Click, Sub()
                                                 HideProfileDropdown()
                                                 NavigateToProfileSettings()
                                             End Sub

        Dim btnLogOut As New Label()
        btnLogOut.Text = "🚪 Log Out"
        btnLogOut.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnLogOut.ForeColor = System.Drawing.Color.White
        btnLogOut.Size = New System.Drawing.Size(190, 40)
        btnLogOut.Location = New System.Drawing.Point(5, 50)
        btnLogOut.TextAlign = ContentAlignment.MiddleLeft
        btnLogOut.Cursor = Cursors.Hand

        AddHandler btnLogOut.MouseEnter, Sub() btnLogOut.BackColor = Graphite
        AddHandler btnLogOut.MouseLeave, Sub() btnLogOut.BackColor = System.Drawing.Color.Transparent
        AddHandler btnLogOut.Click, Sub()
                                        Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                                        If result = DialogResult.Yes Then
                                            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                                                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Log Out", "User logged out of the application.")
                                            End If
                                            frmLoginvb.LogoutUser()
                                            isNavigating = True
                                            Me.Hide()
                                            Dim loginForm As New frmLoginvb()
                                            loginForm.Show()
                                        End If
                                    End Sub

        profileDropdownPanel.Controls.Add(btnProfileSettings)
        profileDropdownPanel.Controls.Add(btnLogOut)

        Me.Controls.Add(profileDropdownPanel)
        profileDropdownPanel.BringToFront()

        AddHandler Me.Click, AddressOf Form_Click

        isProfileDropdownVisible = True
    End Sub
    Private Sub NavigateToProfileSettings()
        ' Navigate to ProfileSettings form (preserve audit and dropdown state).
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from Inventory to ProfileSettings")
            End If

            ' Prevent the form-closing confirmation and hide the dropdown first
            isNavigating = True
            HideProfileDropdown()

            ' Open ProfileSettings and close Inventory
            Dim profileForm As New ProfileSettings()
            profileForm.StartPosition = FormStartPosition.CenterScreen
            profileForm.Show()

            Me.Close()
        Catch ex As Exception
            ' Restore navigating flag on failure and show error
            isNavigating = False
            MessageBox.Show($"Unable to open Profile Settings: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub HideProfileDropdown()
        If profileDropdownPanel IsNot Nothing Then
            Me.Controls.Remove(profileDropdownPanel)
            profileDropdownPanel.Dispose()
            profileDropdownPanel = Nothing
        End If
        isProfileDropdownVisible = False

        RemoveHandler Me.Click, AddressOf Form_Click
    End Sub

    Private Sub Form_Click(sender As Object, e As EventArgs)
        HideProfileDropdown()
    End Sub

    ' Navigation menu (match style from other pages) - Inventory marked active because suppliers belong to inventory
    Private Sub CreateNavigationMenu()
        Try
            For i = DashboardPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = DashboardPanel.Controls(i)
                If TypeOf control IsNot PictureBox Then
                    DashboardPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            DashboardPanel.FillColor = System.Drawing.Color.FromArgb(61, 65, 66)

            If PictureBox9 IsNot Nothing Then
                Try
                    Dim logoImg As System.Drawing.Image = CompanySettingsManager.Instance.GetCompanyLogo()
                    If logoImg IsNot Nothing Then
                        PictureBox9.Image = logoImg
                        PictureBox9.Location = New Point(81, 15)
                    End If
                Catch
                End Try
                PictureBox9.BringToFront()
            End If

            Dim availableWidth As Integer = DashboardPanel.Width - 40
            Dim startY As Integer = 250
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonWidth As Integer = availableWidth - 5
            Dim buttonIndex As Integer = 0

            Dim titleLabel As New Label()
            titleLabel.Text = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = System.Drawing.Color.FromArgb(254, 191, 16)
            titleLabel.BackColor = System.Drawing.Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New System.Drawing.Size(availableWidth, 30)
            titleLabel.Location = New System.Drawing.Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(titleLabel)

            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Poppins", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(225, 229, 233)
            subtitleLabel.BackColor = System.Drawing.Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New System.Drawing.Size(availableWidth, 25)
            subtitleLabel.Location = New System.Drawing.Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(subtitleLabel)

            Dim separator1 As New Panel()
            separator1.BackColor = System.Drawing.Color.FromArgb(50, 50, 50)
            separator1.Size = New System.Drawing.Size(availableWidth - 20, 2)
            separator1.Location = New System.Drawing.Point(30, 190)
            DashboardPanel.Controls.Add(separator1)

            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Poppins", 10, FontStyle.Bold)
            navLabel.ForeColor = System.Drawing.Color.FromArgb(225, 229, 233)
            navLabel.BackColor = System.Drawing.Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New System.Drawing.Size(availableWidth, 25)
            navLabel.Location = New System.Drawing.Point(20, 205)
            navLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(navLabel)

            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "Staff").ToUpper()

            ' 1. Dashboard
            Dim navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
            buttonIndex += 1

            ' 2. POS / Sales
            Dim navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navPOSBtn.Click, AddressOf NavPOS_Click
            buttonIndex += 1

            ' 3. Inventory (visible to Manager/Admin)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
                buttonIndex += 1
            End If

            ' 4. Sales Records
            Dim navSalesRecordsBtn = CreateLargeNavButton("📊 Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navSalesRecordsBtn.Click, AddressOf NavSalesRecords_Click
            buttonIndex += 1

            ' 5. Staff (visible to Manager/Admin)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navStaffBtn.Click, AddressOf NavStaff_Click
                buttonIndex += 1
            End If

            ' 6. Inventory Logs
            Dim navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
            buttonIndex += 1

            ' 7. Suppliers (ACTIVE on this page)
            Dim navSuppliersBtn = CreateLargeNavButton("🏷️ Suppliers", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
            ' Keep Suppliers active (no navigation). Optionally refresh when clicked.
            AddHandler navSuppliersBtn.Click, Sub()
                                                  LoadSuppliersData()
                                              End Sub
            buttonIndex += 1

            ' 8. Audit Logs
            Dim navAuditLogBtn = CreateLargeNavButton("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navAuditLogBtn.Click, AddressOf NavAuditLog_Click
            buttonIndex += 1

            ' 9. System (Admin only)
            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim systemSettingsBtn = CreateLargeNavButton("⚙️ System", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler systemSettingsBtn.Click, AddressOf NavSystemSettings_Click
                buttonIndex += 1
            End If

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
        End Try
    End Sub
    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            Dim salesRecordForm As New SalesRecord()
            salesRecordForm.Show()
            Me.Close()
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open Sales Records: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Function CreateLargeNavButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer) As Guna.UI2.WinForms.Guna2Button
        Dim btn As New Guna.UI2.WinForms.Guna2Button()
        btn.Text = text
        btn.Size = New System.Drawing.Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left

        btn.FillColor = If(isActive, System.Drawing.Color.FromArgb(254, 191, 16), System.Drawing.Color.Transparent)
        btn.ForeColor = If(isActive, System.Drawing.Color.FromArgb(26, 29, 31), System.Drawing.Color.White)
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(80, 80, 80))
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = System.Drawing.Color.FromArgb(30, 30, 30)
        btn.ShadowDecoration.Depth = 4

        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.FromArgb(48, 52, 54)
                                           btn.BorderColor = System.Drawing.Color.FromArgb(254, 191, 16)
                                           btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                       End If
                                   End Sub
        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.Transparent
                                           btn.BorderColor = System.Drawing.Color.FromArgb(80, 80, 80)
                                           btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        DashboardPanel.Controls.Add(btn)
        Return btn
    End Function

    ' Navigation handlers
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

    Private Sub NavInventoryLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Me.Close()
    End Sub

    Private Sub NavStaff_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Staff.Show()
        Me.Close()
    End Sub

    Private Sub NavSystemSettings_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sys.Show()
        Me.Close()
    End Sub

    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        AuditLog.Show()
        Me.Close()
    End Sub

    Private Sub NavSuppliers_Click(sender As Object, e As EventArgs)
        ' Already on suppliers - no navigation needed, but keep consistent behavior
        ' Close nothing; optionally refresh
        LoadSuppliersData()
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
End Class