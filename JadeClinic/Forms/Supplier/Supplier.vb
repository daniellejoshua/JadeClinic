Imports Microsoft.Data.SqlClient

Public Class Supplier
    Private isNavigating As Boolean = False
    Private profileDropdownPanel As Panel = Nothing
    Private isProfileDropdownVisible As Boolean = False

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
            InventoryLogDataGrid.RowTemplate.Height = 50

            InventoryLogDataGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
            InventoryLogDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            InventoryLogDataGrid.AllowUserToAddRows = False
            InventoryLogDataGrid.AllowUserToDeleteRows = False
            InventoryLogDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
            InventoryLogDataGrid.MultiSelect = False
            InventoryLogDataGrid.ScrollBars = ScrollBars.Vertical
            InventoryLogDataGrid.RowHeadersVisible = False

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "SupplierID",
                .HeaderText = "ID",
                .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "SupplierCode",
                .HeaderText = "Code",
                .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "SupplierName",
                .HeaderText = "Supplier Name",
                .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "ContactPerson",
                .HeaderText = "Contact Person",
                .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "Phone",
                .HeaderText = "Phone",
                .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "Email",
                .HeaderText = "Email",
                .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            })

            InventoryLogDataGrid.Columns.Add(New DataGridViewTextBoxColumn() With {
                .Name = "Status",
                .HeaderText = "Status",
                .ReadOnly = True,
                .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
            })

            Dim actionCol As New DataGridViewTextBoxColumn()
            actionCol.Name = "Action"
            actionCol.HeaderText = "Action"
            actionCol.ReadOnly = True
            actionCol.DefaultCellStyle = New DataGridViewCellStyle() With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .Font = New Font("Segoe UI Emoji", 12, FontStyle.Regular),
                .ForeColor = System.Drawing.Color.LightGray
            }
            actionCol.Width = 80
            InventoryLogDataGrid.Columns.Add(actionCol)

        Catch ex As Exception
            MessageBox.Show($"Error preparing suppliers grid: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
                    Dim rowIndex As Integer = InventoryLogDataGrid.Rows.Add()
                    InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierID").Value = Convert.ToInt32(reader("SupplierID"))
                    InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierCode").Value = If(IsDBNull(reader("SupplierCode")), "", reader("SupplierCode").ToString())
                    InventoryLogDataGrid.Rows(rowIndex).Cells("SupplierName").Value = If(IsDBNull(reader("SupplierName")), "", reader("SupplierName").ToString())
                    InventoryLogDataGrid.Rows(rowIndex).Cells("ContactPerson").Value = If(IsDBNull(reader("ContactPerson")), "", reader("ContactPerson").ToString())
                    InventoryLogDataGrid.Rows(rowIndex).Cells("Phone").Value = If(IsDBNull(reader("Phone")), "", reader("Phone").ToString())
                    InventoryLogDataGrid.Rows(rowIndex).Cells("Email").Value = If(IsDBNull(reader("Email")), "", reader("Email").ToString())
                    InventoryLogDataGrid.Rows(rowIndex).Cells("Status").Value = If(Convert.ToBoolean(reader("IsActive")), "Active", "Inactive")
                    InventoryLogDataGrid.Rows(rowIndex).Cells("Action").Value = "👁️"

                    InventoryLogDataGrid.Rows(rowIndex).Tag = New Dictionary(Of String, Object) From {
                        {"SupplierID", Convert.ToInt32(reader("SupplierID"))},
                        {"SupplierCode", If(IsDBNull(reader("SupplierCode")), "", reader("SupplierCode").ToString())},
                        {"SupplierName", If(IsDBNull(reader("SupplierName")), "", reader("SupplierName").ToString())}
                    }

                    count += 1
                End While

                lblUsername.Text = $"{count} Items"
            End Using

            InventoryLogDataGrid.ClearSelection()
            InventoryLogDataGrid.Refresh()

        Catch ex As Exception
            MessageBox.Show($"Error loading suppliers: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
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
                                                 MessageBox.Show("Profile Settings not implemented.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                             End Sub

        Dim btnLogOut As New Label()
        btnLogOut.Text = "🚪 Log Out"
        btnLogOut.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnLogOut.ForeColor = System.Drawing.Color.White
        btnLogOut.Size = New System.Drawing.Size(190, 40)
        btnLogOut.Location = New System.Drawing.Point(5, 50)
        btnLogOut.TextAlign = ContentAlignment.MiddleLeft
        btnLogOut.Cursor = Cursors.Hand

        AddHandler btnLogOut.MouseEnter, Sub() btnLogOut.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        AddHandler btnLogOut.MouseLeave, Sub() btnLogOut.BackColor = System.Drawing.Color.Transparent
        AddHandler btnLogOut.Click, Sub()
                                        If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                                            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Log Out", "User logged out of the application.")
                                        End If
                                        Me.Close()
                                    End Sub

        profileDropdownPanel.Controls.Add(btnProfileSettings)
        profileDropdownPanel.Controls.Add(btnLogOut)

        Me.Controls.Add(profileDropdownPanel)
        profileDropdownPanel.BringToFront()

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

            Dim navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
            buttonIndex += 1

            Dim navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navPOSBtn.Click, AddressOf NavPOS_Click
            buttonIndex += 1

            ' Inventory (mark inactive here)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
                buttonIndex += 1
            End If

            ' Suppliers (ACTIVE on this page)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navSuppliersBtn = CreateLargeNavButton("🏷️ Suppliers", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
                ' Active view - no navigation handler attached
                buttonIndex += 1
            End If

            Dim navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
            buttonIndex += 1

            Dim navAuditLogBtn = CreateLargeNavButton("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navAuditLogBtn.Click, AddressOf NavAuditLog_Click
            buttonIndex += 1

            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navStaffBtn.Click, AddressOf NavStaff_Click
                buttonIndex += 1
            End If

            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim systemSettingsBtn = CreateLargeNavButton("⚙️ System", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler systemSettingsBtn.Click, AddressOf NavSystemSettings_Click
                buttonIndex += 1
            End If

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
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
End Class