Imports Microsoft.Data.SqlClient
Imports System.IO
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Public Class SalesRecord
    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    ' Sort selection variable
    Private selectedDate As DateTime? = Nothing

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

    Private Sub SalesRecord_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize QuestPDF
        QuestPDF.Settings.License = LicenseType.Community
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.MinimumSize = Me.Size
        Me.MaximumSize = Me.Size
       
    Me.Text = "Sales Records - Lokal Recipe POS"

        ' Validate user session
        If Not ValidateUserSession() Then
            Return
        End If

        ' Initialize profile section
        InitializeProfileSection()

        ' Create navigation menu (builds dashboard panel buttons)
        CreateNavigationMenu()

        ' Initialize DataGridView
        InitializeDataGridView()
        ' Prevent resizing of all columns and rows
        Guna2DataGridView1.AllowUserToResizeColumns = False
        Guna2DataGridView1.AllowUserToResizeRows = False
        Guna2DataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
        Guna2DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing

        ' Initialize Sort ComboBox
        InitializeSortComboBox()

        ' Load sales records data (show all by default)
        LoadOrderRecordsData()

        ' Update form title to show logged-in user
        Me.Text = $"Sales Records - {frmLoginvb.LoggedInUsername}"
    End Sub

    Private Function ValidateUserSession() As Boolean
        If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            MessageBox.Show("User session expired. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            frmLoginvb.Show()
            Me.Hide()
            Return False
        End If
        Return True
    End Function

    Private Sub InitializeDataGridView()
        ' Clear existing columns
        Guna2DataGridView1.Columns.Clear()

        ' Configure DataGridView appearance
        Guna2DataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(41, 44, 45)
        Guna2DataGridView1.GridColor = System.Drawing.Color.White
        Guna2DataGridView1.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        Guna2DataGridView1.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        Guna2DataGridView1.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
        Guna2DataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 204, 77)
        Guna2DataGridView1.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
        Guna2DataGridView1.DefaultCellStyle.Font = New System.Drawing.Font("Poppins", 9.0F, System.Drawing.FontStyle.Regular)
        Guna2DataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        ' Configure header style
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(30, 30, 30)
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle.Font = New System.Drawing.Font("Poppins SemiBold", 10.0F, System.Drawing.FontStyle.Regular)
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Guna2DataGridView1.ColumnHeadersHeight = 50
        Guna2DataGridView1.RowTemplate.Height = 50

        ' Ensure row borders are visible
        Guna2DataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal

        ' Set AutoSizeColumnsMode to Fill
        Guna2DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

        ' Add columns dynamically
        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "OrderID",
            .HeaderText = "Sale ID",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "CreatedBy",
            .HeaderText = "Cashier",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "OrderDate",
            .HeaderText = "Sale Date",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "TotalAmount",
            .HeaderText = "Total Amount",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "TotalReceived",
            .HeaderText = "Amount Paid",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "Change",
            .HeaderText = "Change",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "DiscountType",
            .HeaderText = "Discount Type",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "DiscountAmount",
            .HeaderText = "Discount Amount",
            .ReadOnly = True,
            .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        })

        ' Add Action label column with eye emoji
        Dim actionCol As New DataGridViewTextBoxColumn()
        actionCol.Name = "Action"
        actionCol.HeaderText = "Action"
        actionCol.ReadOnly = True
        actionCol.DefaultCellStyle = New DataGridViewCellStyle() With {
            .Alignment = DataGridViewContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI Emoji", 12, FontStyle.Regular),
            .ForeColor = System.Drawing.Color.LightGray
        }
        actionCol.Width = 60
        Guna2DataGridView1.Columns.Add(actionCol)

        ' Configure DataGridView properties
        Guna2DataGridView1.AllowUserToAddRows = False
        Guna2DataGridView1.AllowUserToDeleteRows = False
        Guna2DataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Guna2DataGridView1.MultiSelect = False
        Guna2DataGridView1.ScrollBars = ScrollBars.Vertical ' Disable horizontal scroll
    End Sub

    Private Sub InitializeSortComboBox()
        SortBy.Items.Clear()
        SortBy.Items.Add("Sale Date (Newest First)")
        SortBy.Items.Add("Sale Date (Oldest First)")
        SortBy.Items.Add("Sale ID (Ascending)")
        SortBy.Items.Add("Sale ID (Descending)")
        SortBy.Items.Add("Total Amount (Highest First)")
        SortBy.Items.Add("Total Amount (Lowest First)")
        SortBy.SelectedIndex = 0
    End Sub

    Private Sub LoadOrderRecordsData(Optional sortOrder As String = "")
        Try
            Guna2DataGridView1.Rows.Clear()

            Dim query As String = "SELECT s.SaleID, u.Username, s.SaleDate, s.TotalAmount, s.AmountPaid, " &
                          "(s.AmountPaid - s.TotalAmount) AS Change, " &
                          "ISNULL(JSON_VALUE(s.SalesData, '$.payment.discount.type'), '') AS DiscountType, " &
                          "ISNULL(TRY_CONVERT(decimal(18,2), JSON_VALUE(s.SalesData, '$.payment.discount.amount')), 0) AS DiscountAmount " &
                          "FROM Sales s LEFT JOIN Users u ON s.UserID = u.UserID"

            Select Case sortOrder
                Case "Sale Date (Newest First)"
                    query += " ORDER BY s.SaleDate DESC"
                Case "Sale Date (Oldest First)"
                    query += " ORDER BY s.SaleDate ASC"
                Case "Sale ID (Ascending)"
                    query += " ORDER BY s.SaleID ASC"
                Case "Sale ID (Descending)"
                    query += " ORDER BY s.SaleID DESC"
                Case "Total Amount (Highest First)"
                    query += " ORDER BY s.TotalAmount DESC"
                Case "Total Amount (Lowest First)"
                    query += " ORDER BY s.TotalAmount ASC"
                Case Else
                    query += " ORDER BY s.SaleDate DESC"
            End Select

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                While reader.Read()
                    Dim saleId As Integer = If(IsDBNull(reader("SaleID")), 0, Convert.ToInt32(reader("SaleID")))
                    Dim username As String = If(IsDBNull(reader("Username")), "Unknown", reader("Username").ToString())
                    Dim saleDate As DateTime = If(IsDBNull(reader("SaleDate")), DateTime.MinValue, Convert.ToDateTime(reader("SaleDate")))
                    Dim totalAmount As Decimal = If(IsDBNull(reader("TotalAmount")), 0D, Convert.ToDecimal(reader("TotalAmount")))
                    Dim amountPaid As Decimal = If(IsDBNull(reader("AmountPaid")), 0D, Convert.ToDecimal(reader("AmountPaid")))
                    Dim changeVal As Decimal = If(IsDBNull(reader("Change")), amountPaid - totalAmount, Convert.ToDecimal(reader("Change")))
                    Dim discountType As String = If(IsDBNull(reader("DiscountType")), "", reader("DiscountType").ToString())
                    Dim discountAmount As Decimal = If(IsDBNull(reader("DiscountAmount")), 0D, Convert.ToDecimal(reader("DiscountAmount")))

                    Dim rowIndex As Integer = Guna2DataGridView1.Rows.Add()

                    Guna2DataGridView1.Rows(rowIndex).Cells("OrderID").Value = saleId
                    Guna2DataGridView1.Rows(rowIndex).Cells("CreatedBy").Value = username
                    Guna2DataGridView1.Rows(rowIndex).Cells("OrderDate").Value = If(saleDate = DateTime.MinValue, "", saleDate.ToString("MM/dd/yyyy HH:mm"))
                    Guna2DataGridView1.Rows(rowIndex).Cells("TotalAmount").Value = "₱" & totalAmount.ToString("F2")
                    Guna2DataGridView1.Rows(rowIndex).Cells("TotalReceived").Value = "₱" & amountPaid.ToString("F2")
                    Guna2DataGridView1.Rows(rowIndex).Cells("Change").Value = "₱" & changeVal.ToString("F2")
                    Guna2DataGridView1.Rows(rowIndex).Cells("DiscountType").Value = discountType
                    Guna2DataGridView1.Rows(rowIndex).Cells("DiscountAmount").Value = "₱" & discountAmount.ToString("F2")
                    Guna2DataGridView1.Rows(rowIndex).Cells("Action").Value = "👁️"

                    ' store raw values for later use
                    Guna2DataGridView1.Rows(rowIndex).Tag = New Dictionary(Of String, Object) From {
                    {"SaleID", saleId},
                    {"Username", username},
                    {"SaleDate", saleDate},
                    {"TotalAmount", totalAmount},
                    {"AmountPaid", amountPaid},
                    {"Change", changeVal},
                    {"DiscountType", discountType},
                    {"DiscountAmount", discountAmount}
                }
                End While
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error loading sales records: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Sales Records Load Failed", $"Error: {ex.Message}")
            End If
        End Try
    End Sub
    Private isSyncingFilters As Boolean = False

    Private Sub SortBy_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SortBy.SelectedIndexChanged
        If isSyncingFilters Then Return
        isSyncingFilters = True
        If SortBy.SelectedItem IsNot Nothing Then
            LoadOrderRecordsData(SortBy.SelectedItem.ToString())
        End If
        isSyncingFilters = False
    End Sub

    ' Removed filtertype and datepicker handlers because those controls are not present in the designer.

    ' Initialize profile section
    Private profileDropdownPanel As Panel = Nothing
    Private isProfileDropdownVisible As Boolean = False

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

        Catch ex As Exception
            lblUsername.Text = frmLoginvb.LoggedInUsername
            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
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
        Catch ex As Exception
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
                g.DrawString(initials, font, Brushes.White,
                    (50 - textSize.Width) / 2, (50 - textSize.Height) / 2)
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
        btnProfileSettings.Location = New Point(5, 5)
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
        btnLogOut.Location = New Point(5, 50)
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

    ' Navigation handlers (kept as regular methods, designer does not define nav controls here)


    Private Sub NavigateToProfileSettings()
        If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from Sales Records to ProfileSettings")
        End If
        isNavigating = True
        MessageBox.Show("Profile Settings not implemented.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub Exportbtn_Click(sender As Object, e As EventArgs) Handles Exportbtn.Click
        MessageBox.Show("Export not implemented in this build.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Public Sub ClearDateFilter()
        ' No date filter control in this form; just reload
        LoadOrderRecordsData(If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), ""))
    End Sub


    Private Sub CreateNavigationMenu()
        Try
            ' Remove all controls except the logo
            For i = DashboardPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = DashboardPanel.Controls(i)
                If TypeOf control IsNot PictureBox Then
                    DashboardPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            DashboardPanel.FillColor = System.Drawing.Color.FromArgb(61, 65, 66)
            PictureBox9.BringToFront()

            Dim availableWidth As Integer = DashboardPanel.Width - 40
            Dim startY As Integer = 250
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonWidth As Integer = availableWidth - 5
            Dim buttonIndex As Integer = 0

            ' Title and subtitle
            Dim titleLabel As New Label()
            titleLabel.Text = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = GoldenYellow
            titleLabel.BackColor = System.Drawing.Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New System.Drawing.Size(availableWidth, 30)
            titleLabel.Location = New System.Drawing.Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(titleLabel)

            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Poppins", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = LightSilver
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
            navLabel.ForeColor = LightSilver
            navLabel.BackColor = System.Drawing.Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New System.Drawing.Size(availableWidth, 25)
            navLabel.Location = New System.Drawing.Point(20, 205)
            navLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(navLabel)

            ' Role logic
            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "Staff").ToUpper()

            ' Dashboard
            Dim navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
            buttonIndex += 1

            ' POS/Sales
            Dim navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navPOSBtn.Click, AddressOf NavPOS_Click
            buttonIndex += 1

            ' Inventory (Manager/Admin)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
                buttonIndex += 1
            End If

            ' Sales Records (ACTIVE)
            Dim navSalesRecordsBtn = CreateLargeNavButton("📊 Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
            buttonIndex += 1

            ' Staff (Manager/Admin)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navStaffBtn.Click, AddressOf NavStaff_Click
                buttonIndex += 1

                Dim navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
                buttonIndex += 1
            End If

            ' Admin only
            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navAuditLogBtn = CreateLargeNavButton("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navAuditLogBtn.Click, AddressOf NavAuditLog_Click
                buttonIndex += 1

                Dim systemSettingsBtn = CreateLargeNavButton("⚙️ System", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler systemSettingsBtn.Click, AddressOf NavSystemSettings_Click
                buttonIndex += 1
            End If

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
        End Try
    End Sub

    ' Add this handler for POS/Sales navigation
    Private Sub NavPOS_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Me.Close()
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
    Private Sub Guna2DataGridView1_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles Guna2DataGridView1.CellClick
        Try
            If e.RowIndex >= 0 AndAlso Guna2DataGridView1.Columns(e.ColumnIndex).Name = "Action" Then
                Dim saleIdObj = Guna2DataGridView1.Rows(e.RowIndex).Cells("OrderID").Value
                If saleIdObj IsNot Nothing AndAlso Integer.TryParse(saleIdObj.ToString(), Nothing) Then
                    Dim saleId As Integer = Convert.ToInt32(saleIdObj)
                    ' Open SalesDetails form as modal and pass the SaleID
                    Dim detailsForm As New SalesDetails(saleId)
                    detailsForm.StartPosition = FormStartPosition.CenterParent
                    detailsForm.ShowDialog(Me)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show($"Error opening sale details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub NavDashboard_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Dashboard.Show()
        Me.Close()
    End Sub

    Private Sub NavInventory_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Inventory.Show()
        Me.Close()
    End Sub

    Private Sub NavStaff_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Staff.Show()
        Me.Close()
    End Sub

    Private Sub NavInventoryLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Me.Close()
    End Sub

    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Me.Close()
    End Sub

    Private Sub NavSystemSettings_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sys.Show()
        Me.Close()
    End Sub
End Class