Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports Guna.UI2.WinForms
Imports Microsoft.Data.SqlClient
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
        ' Start idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)
        QuestPDF.Settings.License = LicenseType.Community
        Me.FormBorderStyle = FormBorderStyle.None
        Me.TopMost = True
        Me.WindowState = FormWindowState.Normal
        Me.Bounds = Screen.PrimaryScreen.Bounds
        Me.WindowState = FormWindowState.Maximized

        Me.Text = "Sales Records - Jade Dental"

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

        ' Ensure date filter defaults to Today on form start and apply filters
        Guna2DateTimePicker1.Value = Date.Today
        If Guna2DateTimePicker1.ShowCheckBox Then
            Guna2DateTimePicker1.Checked = True
        End If

        ' Load sales records data with today's date filter active by default
        ApplyFilters()

        ' Update form title to show logged-in user
        Me.Text = $"Sales Records - {frmLoginvb.LoggedInUsername}"
    End Sub
    Private Sub NavProfileSettings_Click(sender As Object, e As EventArgs)

    End Sub

    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If keyData = Keys.Escape Then
            If Me.OwnedForms.Cast(Of Form)().Any(Function(f) f.Visible) Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            If Not Me.ContainsFocus Then
                Return MyBase.ProcessCmdKey(msg, keyData)
            End If

            If isNavigating Then
                Return True
            End If

            Dim result As DialogResult = EscForm.ConfirmExit(Me)
            Me.Activate()
            If result = DialogResult.Yes Then
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Sales Records.")
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
            .Name = "PaymentMethod",
            .HeaderText = "Payment Method",
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
        ' Use existing designer controls only
        SortBy.Items.Clear()
        SortBy.Items.AddRange(New Object() {
        "Sale Date (Newest First)",
        "Sale Date (Oldest First)",
        "Sale ID (Ascending)",
        "Sale ID (Descending)",
        "Total Amount (Highest First)",
        "Total Amount (Lowest First)"
    })
        If SortBy.Items.Count > 0 Then
            SortBy.SelectedIndex = 0
        End If

        ' Default date picker to today's date and enable the checkbox so the filter is active on start
        Guna2DateTimePicker1.ShowCheckBox = True
        Guna2DateTimePicker1.Value = Date.Today
        Guna2DateTimePicker1.Checked = True

        RemoveHandler SortBy.SelectedIndexChanged, AddressOf SortBy_SelectedIndexChanged
        AddHandler SortBy.SelectedIndexChanged, AddressOf SortBy_SelectedIndexChanged

        RemoveHandler Guna2DateTimePicker1.ValueChanged, AddressOf DtpSaleDate_ValueChanged
        AddHandler Guna2DateTimePicker1.ValueChanged, AddressOf DtpSaleDate_ValueChanged

        RemoveHandler Guna2DateTimePicker1.CheckedChanged, AddressOf DtpSaleDate_CheckedChanged
        AddHandler Guna2DateTimePicker1.CheckedChanged, AddressOf DtpSaleDate_CheckedChanged

        ' Fix DateTimePicker dropdown for hosted forms
        AddHandler Guna2DateTimePicker1.DropDown, AddressOf DateTimePicker_DropDown
    End Sub
    Private Sub LoadOrderRecordsData(Optional sortOrder As String = "", Optional filterDate As DateTime? = Nothing)
        Try
            Guna2DataGridView1.Rows.Clear()
            
            ' Hide any existing "No records" message
            DataGridViewHelper.HideNoRecordsMessage()

            Dim query As String = "SELECT s.SaleID, u.Username, s.SaleDate, s.PaymentMethod, s.TotalAmount, s.AmountPaid, " &
                          "(s.AmountPaid - s.TotalAmount) AS Change, " &
                          "ISNULL(JSON_VALUE(s.SalesData, '$.payment.discount.type'), '') AS DiscountType, " &
                          "ISNULL(TRY_CONVERT(decimal(18,2), JSON_VALUE(s.SalesData, '$.payment.discount.amount')), 0) AS DiscountAmount " &
                          "FROM Sales s LEFT JOIN Users u ON s.UserID = u.UserID"

            Dim parameters As New List(Of SqlParameter)()

            If filterDate.HasValue Then
                query &= " WHERE CAST(s.SaleDate AS date) = @FilterDate"
                parameters.Add(New SqlParameter("@FilterDate", filterDate.Value.Date))
            End If

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

            ' Replace the inner part of the reader loop in LoadOrderRecordsData with this (keeps behavior but sets PaymentMethod cell color)
            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters.ToArray())
                While reader.Read()
                    Dim saleId As Integer = If(IsDBNull(reader("SaleID")), 0, Convert.ToInt32(reader("SaleID")))
                    Dim username As String = If(IsDBNull(reader("Username")), "Unknown", reader("Username").ToString())
                    Dim saleDate As DateTime = If(IsDBNull(reader("SaleDate")), DateTime.MinValue, Convert.ToDateTime(reader("SaleDate")))
                    Dim paymentMethod As String = If(IsDBNull(reader("PaymentMethod")), "N/A", reader("PaymentMethod").ToString())
                    Dim totalAmount As Decimal = If(IsDBNull(reader("TotalAmount")), 0D, Convert.ToDecimal(reader("TotalAmount")))
                    Dim amountPaid As Decimal = If(IsDBNull(reader("AmountPaid")), 0D, Convert.ToDecimal(reader("AmountPaid")))
                    Dim changeVal As Decimal = If(IsDBNull(reader("Change")), amountPaid - totalAmount, Convert.ToDecimal(reader("Change")))
                    Dim discountType As String = If(IsDBNull(reader("DiscountType")), "", reader("DiscountType").ToString())
                    Dim discountAmount As Decimal = If(IsDBNull(reader("DiscountAmount")), 0D, Convert.ToDecimal(reader("DiscountAmount")))

                    Dim rowIndex As Integer = Guna2DataGridView1.Rows.Add()

                    Guna2DataGridView1.Rows(rowIndex).Cells("OrderID").Value = saleId
                    Guna2DataGridView1.Rows(rowIndex).Cells("CreatedBy").Value = username
                    Guna2DataGridView1.Rows(rowIndex).Cells("OrderDate").Value = If(saleDate = DateTime.MinValue, "", saleDate.ToString("MM/dd/yyyy HH:mm"))

                    ' Set payment method value and color based on method
                    Guna2DataGridView1.Rows(rowIndex).Cells("PaymentMethod").Value = paymentMethod
                    Dim pmColor As System.Drawing.Color = GetPaymentMethodColor(paymentMethod)
                    Guna2DataGridView1.Rows(rowIndex).Cells("PaymentMethod").Style.ForeColor = pmColor

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
            {"PaymentMethod", paymentMethod},
            {"TotalAmount", totalAmount},
            {"AmountPaid", amountPaid},
            {"Change", changeVal},
            {"DiscountType", discountType},
            {"DiscountAmount", discountAmount}
        }
                End While
            End Using

            If Guna2DataGridView1.Rows.Count = 0 Then
                DataGridViewHelper.ShowNoRecordsMessage(Guna2DataGridView1, "No Sales Records Found")
            End If

            Guna2DataGridView1.ClearSelection()

        Catch ex As Exception
            MessageBox.Show($"Error loading sales records: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Sales Records Load Failed", $"Error: {ex.Message}")
            End If
        End Try
    End Sub
    Private isSyncingFilters As Boolean = False

    Private Sub SortBy_SelectedIndexChanged(sender As Object, e As EventArgs)
        If isSyncingFilters Then Return
        ApplyFilters()
    End Sub

    Private Sub DtpSaleDate_ValueChanged(sender As Object, e As EventArgs)
        If isSyncingFilters Then Return
        ApplyFilters()
    End Sub

    Private Sub DtpSaleDate_CheckedChanged(sender As Object, e As EventArgs)
        If isSyncingFilters Then Return
        ApplyFilters()
    End Sub

    Private Sub ApplyFilters()
        isSyncingFilters = True

        Dim sortOrder As String = If(SortBy IsNot Nothing AndAlso SortBy.SelectedItem IsNot Nothing,
                                     SortBy.SelectedItem.ToString(),
                                     "Sale Date (Newest First)")

        If Guna2DateTimePicker1 IsNot Nothing AndAlso Guna2DateTimePicker1.Checked Then
            selectedDate = Guna2DateTimePicker1.Value.Date
        Else
            selectedDate = Nothing
        End If

        LoadOrderRecordsData(sortOrder, selectedDate)

        isSyncingFilters = False
    End Sub

    ' Initialize profile section
    ' Profile managed by ProfileManager

    Private Sub InitializeProfileSection()
        ProfileManager.InitializeProfile(Me, lblUsername, Guna2CirclePictureBox5, AddressOf NavigateToProfileSettings)
    End Sub

    ' Navigation handlers (kept as regular methods, designer does not define nav controls here)


    Private Sub NavigateToProfileSettings()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from System to ProfileSettings")
            End If

            ' Prevent the form-closing confirmation and hide dropdown first
            isNavigating = True
            ProfileManager.HideProfileDropdown(Me)

            ' Open ProfileSettings form (centered) and close this form
            Dim profileForm As New ProfileSettings()
            profileForm.StartPosition = FormStartPosition.CenterScreen
            profileForm.Show()

            Me.Close()
        Catch ex As Exception
            ' Restore flag on failure and show error
            isNavigating = False
            MessageBox.Show($"Unable to open Profile Settings: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    ' Add this helper after the ReadOnly color declarations (near top of the class)
    Private Function GetPaymentMethodColor(method As String) As System.Drawing.Color
        If String.IsNullOrWhiteSpace(method) Then
            Return LightSilver
        End If

        Dim m As String = method.ToLowerInvariant()

        ' GCash -> Blue
        If m.Contains("gcash") Then
            Return System.Drawing.Color.FromArgb(255, 66, 133, 244) ' Blue
        End If

        ' Card / Credit / Debit -> Golden yellow (use existing GoldenYellow)
        If m.Contains("card") OrElse m.Contains("credit") OrElse m.Contains("debit") Then
            Return GoldenYellow
        End If

        ' Cash -> Green (use existing SuccessGreen)
        If m.Contains("cash") Then
            Return SuccessGreen
        End If

        ' Default
        Return LightSilver
    End Function
    Private Sub Exportbtn_Click(sender As Object, e As EventArgs) Handles Exportbtn.Click
        Try

            Dim sortOrder As String = "Sale Date (Newest First)"
            If SortBy IsNot Nothing AndAlso SortBy.SelectedItem IsNot Nothing Then
                sortOrder = SortBy.SelectedItem.ToString()
            End If

            Dim filterDate As DateTime? = Nothing
            If Guna2DateTimePicker1 IsNot Nothing Then
                If Not Guna2DateTimePicker1.ShowCheckBox OrElse Guna2DateTimePicker1.Checked Then
                    filterDate = Guna2DateTimePicker1.Value.Date
                End If
            End If

            SalesRecordExporter.ExportOrderRecordsReport(sortOrder, "All Sales", filterDate)

        Catch ex As Exception
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Public Sub ClearDateFilter()
        ' No date filter control in this form; just reload
    End Sub


    Private Sub CreateNavigationMenu()
        NavigationBuilder.Build(DashboardPanel, Me, "SalesRecord")
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
    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        AuditLog.Show()
        Me.Close()
    End Sub
    ' Navigation event handlers
    Private Sub NavSuppliers_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            Supplier.Show()
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Unable to open Suppliers: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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

    Private Sub NavSystemSettings_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sys.Show()
        Me.Close()
    End Sub

    Private Sub SalesRecord_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)
        If isNavigating Then
            Return
        End If
        ' Show confirmation only for user-initiated close (X button)
        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to exit the application?", "Exit Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
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

    Private Sub DateTimePicker_DropDown(sender As Object, e As EventArgs)
        ' DateTimePicker dropdown now works without TopMost toggle
    End Sub

    Private Sub DateTimePicker_CloseUp(sender As Object, e As EventArgs)
        ' No longer needed
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
End Class