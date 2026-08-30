Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports Guna.UI2.WinForms
Imports System.Data.Common
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Public Class SalesRecord
    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    ' Sort selection variable
    Private selectedDate As DateTime? = Nothing

    ' Pagination state
    Private Const PageSize As Integer = 50
    Private _currentPage As Integer = 1
    Private _searchTerm As String = ""

    ' Day-capital display label (top filter row)
    Private lblCapital As Label

    Private ReadOnly GoldenYellow As System.Drawing.Color = System.Drawing.Color.FromArgb(254, 191, 16)
    Private ReadOnly JadeOlive As System.Drawing.Color = System.Drawing.Color.FromArgb(191, 155, 48)
    Private ReadOnly DarkText As System.Drawing.Color = System.Drawing.Color.FromArgb(51, 51, 51)
    Private ReadOnly MediumText As System.Drawing.Color = System.Drawing.Color.FromArgb(102, 102, 102)
    Private ReadOnly PanelFill As System.Drawing.Color = System.Drawing.Color.FromArgb(250, 250, 249)
    Private ReadOnly LightGray As System.Drawing.Color = System.Drawing.Color.FromArgb(237, 237, 237)
    Private ReadOnly SuccessGreen As System.Drawing.Color = System.Drawing.Color.FromArgb(80, 160, 80)
    Private ReadOnly AlertRed As System.Drawing.Color = System.Drawing.Color.FromArgb(220, 80, 70)
    Private ReadOnly OliveSelection As System.Drawing.Color = System.Drawing.Color.FromArgb(235, 228, 200)
    Private ReadOnly White As System.Drawing.Color = System.Drawing.Color.FromArgb(255, 255, 255)

    Private Sub SalesRecord_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.BackColor = Drawing.Color.FromArgb(248, 248, 247)
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

        ' Initialize Sort ComboBox
        InitializeSortComboBox()

        ' Date filter defaults to Today on form start
        Guna2DateTimePicker1.Value = Date.Today
        Guna2DateTimePicker1.ShowCheckBox = True
        Guna2DateTimePicker1.Checked = True

        ' Show the day's capital (opening capital for the selected date)
        InitializeCapitalLabel()

        ' Load sales records data with today's date filter active by default
        ApplyFilters()

        ' Update form title to show logged-in user
        Me.Text = $"Sales Records - {frmLoginvb.LoggedInUsername}"

        SetupTabIndex()
    End Sub

    Private Sub SetupTabIndex()
        Guna2DateTimePicker1.TabIndex = 0
        SortBy.TabIndex = 1
        Exportbtn.TabIndex = 2
        Utilities.ApplyInputFocusEffects(Me)
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
        Guna2DataGridView1.Columns.Clear()

        Guna2DataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Guna2DataGridView1.MultiSelect = False
        Guna2DataGridView1.ScrollBars = ScrollBars.Vertical
        Guna2DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
            .Name = "OrderID",
            .HeaderText = "Sale No",
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
            .ForeColor = DarkText
        }
        actionCol.Width = 60
        actionCol.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter
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
        SortBy.Items.Add("All Users")

        Try
            Dim query As String = "SELECT DISTINCT u.Username FROM Users u INNER JOIN Sales s ON s.UserID = u.UserID ORDER BY u.Username"
            Using reader As DbDataReader = Utilities.ExecuteReader(query)
                While reader.Read()
                    If Not IsDBNull(reader("Username")) Then
                        SortBy.Items.Add(reader("Username").ToString())
                    End If
                End While
            End Using
        Catch
        End Try

        If SortBy.Items.Count > 0 Then
            SortBy.SelectedIndex = 0
        End If

        Guna2DateTimePicker1.ShowCheckBox = True
        Guna2DateTimePicker1.Value = Date.Today
        Guna2DateTimePicker1.Checked = True

        RemoveHandler SortBy.SelectedIndexChanged, AddressOf SortBy_SelectedIndexChanged
        AddHandler SortBy.SelectedIndexChanged, AddressOf SortBy_SelectedIndexChanged

        RemoveHandler Guna2DateTimePicker1.ValueChanged, AddressOf DtpSaleDate_ValueChanged
        AddHandler Guna2DateTimePicker1.ValueChanged, AddressOf DtpSaleDate_ValueChanged

        RemoveHandler Guna2DateTimePicker1.CheckedChanged, AddressOf DtpSaleDate_CheckedChanged
        AddHandler Guna2DateTimePicker1.CheckedChanged, AddressOf DtpSaleDate_CheckedChanged

        AddHandler Guna2DateTimePicker1.DropDown, AddressOf DateTimePicker_DropDown

        If TxtSearch IsNot Nothing Then
            RemoveHandler TxtSearch.KeyDown, AddressOf TxtSearch_KeyDown
            AddHandler TxtSearch.KeyDown, AddressOf TxtSearch_KeyDown
            RemoveHandler TxtSearch.TextChanged, AddressOf TxtSearch_TextChanged
            AddHandler TxtSearch.TextChanged, AddressOf TxtSearch_TextChanged
        End If

        If PaginationControl1 IsNot Nothing Then
            RemoveHandler PaginationControl1.PageChanged, AddressOf PaginationControl1_PageChanged
            AddHandler PaginationControl1.PageChanged, AddressOf PaginationControl1_PageChanged
        End If
    End Sub
    Private Function LoadOrderRecordsData(Optional sortOrder As String = "Sale Date (Newest First)", Optional filterDate As DateTime? = Nothing, Optional userFilter As String = Nothing, Optional searchTerm As String = "", Optional pageNumber As Integer = 1, Optional pageSize As Integer = 50) As List(Of Dictionary(Of String, Object))
        Dim sales As New List(Of Dictionary(Of String, Object))()
        Try
            Dim query As String = "SELECT s.SaleID, IFNULL(s.SaleNumber, '') AS SaleNumber, u.Username, s.SaleDate, s.PaymentMethod, s.TotalAmount, s.AmountPaid, " &
                          "(s.AmountPaid - s.TotalAmount) AS Change, s.SalesData, IFNULL(s.Status, 'Completed') AS Status " &
                          "FROM Sales s LEFT JOIN Users u ON s.UserID = u.UserID"

            Dim whereClauses As New List(Of String)()
            Dim parameters As New List(Of SqlParameter)()

            If filterDate.HasValue Then
                whereClauses.Add("DATE(s.SaleDate) = @FilterDate")
                parameters.Add(New SqlParameter("@FilterDate", filterDate.Value.Date.ToString("yyyy-MM-dd")))
            End If

            If Not String.IsNullOrWhiteSpace(userFilter) AndAlso userFilter <> "All Users" Then
                whereClauses.Add("u.Username = @Username")
                parameters.Add(New SqlParameter("@Username", userFilter))
            End If

            If Not String.IsNullOrWhiteSpace(searchTerm) Then
                whereClauses.Add("(IFNULL(s.SaleNumber, '') LIKE @Search OR IFNULL(u.Username, '') LIKE @Search)")
                parameters.Add(New SqlParameter("@Search", "%" & searchTerm & "%"))
            End If

            If whereClauses.Count > 0 Then
                query &= " WHERE " & String.Join(" AND ", whereClauses)
            End If

            Select Case sortOrder
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

            Dim offset As Integer = (pageNumber - 1) * pageSize
            query &= $" LIMIT {pageSize} OFFSET {offset}"

            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters.ToArray())
                While reader.Read()
                    Dim saleId As Integer = If(IsDBNull(reader("SaleID")), 0, Convert.ToInt32(reader("SaleID")))
                    Dim saleNumber As String = If(IsDBNull(reader("SaleNumber")), "", reader("SaleNumber").ToString())
                    If String.IsNullOrWhiteSpace(saleNumber) Then saleNumber = saleId.ToString()
                    Dim username As String = If(IsDBNull(reader("Username")), "Unknown", reader("Username").ToString())
                    Dim saleDate As DateTime = If(IsDBNull(reader("SaleDate")), DateTime.MinValue, Convert.ToDateTime(reader("SaleDate")))
                    Dim paymentMethod As String = If(IsDBNull(reader("PaymentMethod")), "N/A", reader("PaymentMethod").ToString())
                    Dim totalAmount As Decimal = If(IsDBNull(reader("TotalAmount")), 0D, Convert.ToDecimal(reader("TotalAmount")))
                    Dim amountPaid As Decimal = If(IsDBNull(reader("AmountPaid")), 0D, Convert.ToDecimal(reader("AmountPaid")))
                    Dim changeVal As Decimal = If(IsDBNull(reader("Change")), amountPaid - totalAmount, Convert.ToDecimal(reader("Change")))
                    Dim salesDataJson As String = If(IsDBNull(reader("SalesData")), "{}", reader("SalesData").ToString())
                    Dim status As String = If(IsDBNull(reader("Status")), "Completed", reader("Status").ToString())
                    Dim discountType As String = ""
                    Dim discountAmount As Decimal = 0D
                    Try
                        Dim jObj = Newtonsoft.Json.Linq.JObject.Parse(salesDataJson)
                        Dim discount = jObj.SelectToken("payment.discount")
                        If discount IsNot Nothing Then
                            discountType = If(discount("type") IsNot Nothing, discount("type").ToString(), "")
                            discountAmount = If(discount("amount") IsNot Nothing, Convert.ToDecimal(discount("amount")), 0D)
                        End If
                    Catch
                    End Try

                    sales.Add(New Dictionary(Of String, Object) From {
                        {"SaleID", saleId},
                        {"SaleNumber", saleNumber},
                        {"Username", username},
                        {"SaleDate", saleDate},
                        {"PaymentMethod", paymentMethod},
                        {"TotalAmount", totalAmount},
                        {"AmountPaid", amountPaid},
                        {"Change", changeVal},
                        {"DiscountType", discountType},
                        {"DiscountAmount", discountAmount},
                        {"Status", status}
                    })
                End While
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error loading sales records: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Sales Records Load Failed", $"Error: {ex.Message}")
            End If
        End Try

        Return sales
    End Function

    Private Function CountSalesRecords(Optional filterDate As DateTime? = Nothing, Optional userFilter As String = Nothing, Optional searchTerm As String = "") As Integer
        Dim count As Integer = 0
        Try
            Dim query As String = "SELECT COUNT(*) FROM Sales s LEFT JOIN Users u ON s.UserID = u.UserID"
            Dim whereClauses As New List(Of String)()
            Dim parameters As New List(Of SqlParameter)()

            If filterDate.HasValue Then
                whereClauses.Add("DATE(s.SaleDate) = @FilterDate")
                parameters.Add(New SqlParameter("@FilterDate", filterDate.Value.Date.ToString("yyyy-MM-dd")))
            End If

            If Not String.IsNullOrWhiteSpace(userFilter) AndAlso userFilter <> "All Users" Then
                whereClauses.Add("u.Username = @Username")
                parameters.Add(New SqlParameter("@Username", userFilter))
            End If

            If Not String.IsNullOrWhiteSpace(searchTerm) Then
                whereClauses.Add("(IFNULL(s.SaleNumber, '') LIKE @Search OR IFNULL(u.Username, '') LIKE @Search)")
                parameters.Add(New SqlParameter("@Search", "%" & searchTerm & "%"))
            End If

            If whereClauses.Count > 0 Then
                query &= " WHERE " & String.Join(" AND ", whereClauses)
            End If

            Dim result = Utilities.ExecuteScalar(query, parameters.ToArray())
            If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                count = Convert.ToInt32(result)
            End If
        Catch
        End Try
        Return count
    End Function

    Private Sub PopulateGridFromData(data As List(Of Dictionary(Of String, Object)))
        Guna2DataGridView1.Rows.Clear()
        DataGridViewHelper.HideNoRecordsMessage()

        For Each record In data
            Dim saleNumber As String = If(record.ContainsKey("SaleNumber"), record("SaleNumber").ToString(), "")
            Dim username As String = If(record.ContainsKey("Username"), record("Username").ToString(), "")
            Dim saleDate As DateTime = If(record.ContainsKey("SaleDate"), CDate(record("SaleDate")), DateTime.MinValue)
            Dim paymentMethod As String = If(record.ContainsKey("PaymentMethod"), record("PaymentMethod").ToString(), "")
            Dim totalAmount As Decimal = If(record.ContainsKey("TotalAmount"), CDec(record("TotalAmount")), 0D)
            Dim amountPaid As Decimal = If(record.ContainsKey("AmountPaid"), CDec(record("AmountPaid")), 0D)
            Dim changeVal As Decimal = If(record.ContainsKey("Change"), CDec(record("Change")), 0D)
            Dim discountType As String = If(record.ContainsKey("DiscountType"), record("DiscountType").ToString(), "")
            Dim discountAmount As Decimal = If(record.ContainsKey("DiscountAmount"), CDec(record("DiscountAmount")), 0D)
            Dim status As String = If(record.ContainsKey("Status"), record("Status").ToString(), "Completed")
            Dim isAborted As Boolean = String.Equals(status, "Aborted", StringComparison.OrdinalIgnoreCase)

            Dim rowIndex As Integer = Guna2DataGridView1.Rows.Add()
            Guna2DataGridView1.Rows(rowIndex).Cells("OrderID").Value = saleNumber
            Guna2DataGridView1.Rows(rowIndex).Cells("CreatedBy").Value = username
            Guna2DataGridView1.Rows(rowIndex).Cells("OrderDate").Value = If(saleDate = DateTime.MinValue, "", saleDate.ToString("MM/dd/yyyy HH:mm"))
            Guna2DataGridView1.Rows(rowIndex).Cells("PaymentMethod").Value = If(isAborted, "Aborted", paymentMethod)
            ' Stamp aborted rows clearly and de-emphasize their (zero) values
            If isAborted Then
                Guna2DataGridView1.Rows(rowIndex).Cells("OrderID").Value = saleNumber & "  (ABORTED)"
                Guna2DataGridView1.Rows(rowIndex).Cells("OrderID").Style.ForeColor = Drawing.Color.FromArgb(170, 40, 40)
                Guna2DataGridView1.Rows(rowIndex).Cells("OrderID").Style.Font = New Font("Poppins", 9, FontStyle.Bold)
                Guna2DataGridView1.Rows(rowIndex).Cells("PaymentMethod").Style.ForeColor = Drawing.Color.FromArgb(190, 60, 60)
            End If
            Dim pmColor As Drawing.Color = GetPaymentMethodColor(paymentMethod)
            Guna2DataGridView1.Rows(rowIndex).Cells("PaymentMethod").Style.ForeColor = pmColor
            Guna2DataGridView1.Rows(rowIndex).Cells("TotalAmount").Value = ChrW(&H20B1) & totalAmount.ToString("F2")
            Guna2DataGridView1.Rows(rowIndex).Cells("TotalReceived").Value = ChrW(&H20B1) & amountPaid.ToString("F2")
            Guna2DataGridView1.Rows(rowIndex).Cells("Change").Value = ChrW(&H20B1) & changeVal.ToString("F2")
            Guna2DataGridView1.Rows(rowIndex).Cells("DiscountType").Value = discountType
            Guna2DataGridView1.Rows(rowIndex).Cells("DiscountAmount").Value = ChrW(&H20B1) & discountAmount.ToString("F2")
            Guna2DataGridView1.Rows(rowIndex).Cells("Action").Value = "👁️"
            Guna2DataGridView1.Rows(rowIndex).Tag = record
        Next

        If Guna2DataGridView1.Rows.Count = 0 Then
            DataGridViewHelper.ShowNoRecordsMessage(Guna2DataGridView1, "No Sales Records Found")
        End If

        Guna2DataGridView1.ClearSelection()
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
        _currentPage = 1

        Dim userFilter As String = If(SortBy IsNot Nothing AndAlso SortBy.SelectedItem IsNot Nothing,
                                       SortBy.SelectedItem.ToString(),
                                       Nothing)

        If Guna2DateTimePicker1 IsNot Nothing AndAlso Guna2DateTimePicker1.Checked Then
            selectedDate = Guna2DateTimePicker1.Value.Date
        Else
            selectedDate = Nothing
        End If

        LoadPage()
        isSyncingFilters = False
    End Sub

    Private Sub LoadPage()
        Dim userFilter As String = If(SortBy IsNot Nothing AndAlso SortBy.SelectedItem IsNot Nothing,
                                       SortBy.SelectedItem.ToString(),
                                       Nothing)

        Dim hasSearch As Boolean = Not String.IsNullOrWhiteSpace(_searchTerm)

        Dim filterDate As DateTime? = If(hasSearch, Nothing, selectedDate)
        Dim filterUser As String = If(hasSearch, Nothing, userFilter)

        Dim totalCount As Integer = CountSalesRecords(filterDate, filterUser, _searchTerm)
        Dim data = LoadOrderRecordsData("Sale Date (Newest First)", filterDate, filterUser, _searchTerm, _currentPage, PageSize)

        PopulateGridFromData(data)

        If PaginationControl1 IsNot Nothing Then
            PaginationControl1.Configure(totalCount, PageSize, _currentPage)
        End If
        AlignPaginationToPanel()
        UpdateCapitalLabel(filterDate)
    End Sub

    Private Sub InitializeCapitalLabel()
        Try
            If lblCapital IsNot Nothing Then Return
            lblCapital = New Label() With {
                .AutoSize = True,
                .Font = New Font("Poppins", 10, FontStyle.Bold),
                .ForeColor = DarkText,
                .BackColor = System.Drawing.Color.Transparent,
                .Anchor = AnchorStyles.Top Or AnchorStyles.Right,
                .Text = "Capital: --"
            }
            lblCapital.Location = New Point(Me.ClientSize.Width - 280, 82)
            Me.Controls.Add(lblCapital)
            lblCapital.BringToFront()
        Catch
        End Try
    End Sub

    Private Sub UpdateCapitalLabel(filterDate As DateTime?)
        Try
            If lblCapital Is Nothing Then Return
            Dim capitalDate As Date = If(filterDate.HasValue, filterDate.Value.Date, Date.Today)
            Dim capital As Decimal = GetOpeningCapitalForDate(capitalDate)
            Dim peso As String = ChrW(&H20B1)
            If capital > 0D Then
                lblCapital.Text = "Capital: " & peso & capital.ToString("N2", Globalization.CultureInfo.GetCultureInfo("en-PH"))
            Else
                lblCapital.Text = "Capital: No capital set"
            End If
        Catch
        End Try
    End Sub

    Private Function GetOpeningCapitalForDate(targetDate As Date) As Decimal
        Try
            Dim capitalObj = Utilities.ExecuteScalar(
                "SELECT OpeningAmount FROM DailyOpeningCapital WHERE CashDate = @CashDate",
                New SqlParameter("@CashDate", targetDate.Date))
            If capitalObj Is Nothing OrElse capitalObj Is DBNull.Value Then
                Return 0D
            End If
            Return Convert.ToDecimal(capitalObj)
        Catch
            Return 0D
        End Try
    End Function

    ' Initialize profile section
    ' Profile managed by ProfileManager

    Private Sub InitializeProfileSection()
        ProfileManager.InitializeProfile(Me, lblUsername, Guna2CirclePictureBox5, AddressOf NavigateToProfileSettings)
    End Sub

    ' Navigation handlers (kept as regular methods, designer does not define nav controls here)


    Private Sub NavigateToProfileSettings()
        Try
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
            Return DarkText
        End If

        Dim m As String = method.ToLowerInvariant()

        ' GCash -> Blue
        If m.Contains("gcash") Then
            Return System.Drawing.Color.FromArgb(66, 133, 244)
        End If

        ' Card / Credit / Debit -> Golden yellow
        If m.Contains("card") OrElse m.Contains("credit") OrElse m.Contains("debit") Then
            Return GoldenYellow
        End If

        ' Cash -> Green
        If m.Contains("cash") Then
            Return SuccessGreen
        End If

        ' Default
        Return DarkText
    End Function
    Private Sub Exportbtn_Click(sender As Object, e As EventArgs) Handles Exportbtn.Click
        Try
            Dim filterDate As DateTime? = Nothing
            If Guna2DateTimePicker1 IsNot Nothing Then
                If Not Guna2DateTimePicker1.ShowCheckBox OrElse Guna2DateTimePicker1.Checked Then
                    filterDate = Guna2DateTimePicker1.Value.Date
                End If
            End If

            Dim userFilter As String = If(SortBy IsNot Nothing AndAlso SortBy.SelectedItem IsNot Nothing,
                                           SortBy.SelectedItem.ToString(),
                                           Nothing)

            SalesRecordExporter.ExportOrderRecordsReport("Sale Date (Newest First)", userFilter, filterDate, _searchTerm)

        Catch ex As Exception
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Public Sub ClearDateFilter()
        If Guna2DateTimePicker1 IsNot Nothing Then
            Guna2DateTimePicker1.Value = Date.Today
            Guna2DateTimePicker1.Checked = True
        End If
        selectedDate = Date.Today
        ApplyFilters()
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

        btn.FillColor = If(isActive, GoldenYellow, System.Drawing.Color.Transparent)
        btn.ForeColor = If(isActive, DarkText, DarkText)
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(200, 200, 200))
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = System.Drawing.Color.FromArgb(200, 200, 200)
        btn.ShadowDecoration.Depth = 4

        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = LightGray
                                           btn.BorderColor = GoldenYellow
                                           btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                       End If
                                   End Sub
        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.Transparent
                                           btn.BorderColor = System.Drawing.Color.FromArgb(200, 200, 200)
                                           btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        DashboardPanel.Controls.Add(btn)
        Return btn
    End Function
    Private Sub Guna2DataGridView1_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles Guna2DataGridView1.CellClick
        Try
            If e.RowIndex >= 0 AndAlso Guna2DataGridView1.Columns(e.ColumnIndex).Name = "Action" Then
                Dim saleId As Integer = 0
                Dim tag = TryCast(Guna2DataGridView1.Rows(e.RowIndex).Tag, Dictionary(Of String, Object))
                If tag IsNot Nothing AndAlso tag.ContainsKey("SaleID") Then
                    saleId = Convert.ToInt32(tag("SaleID"))
                End If
                If saleId > 0 Then
                    ' Open SalesDetails form as modal and pass the SaleID
                    Dim detailsForm As New SalesDetails(saleId)
                    detailsForm.StartPosition = FormStartPosition.CenterParent
                    Utilities.EnableEscCloseModal(detailsForm)
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

    Private Sub TxtSearch_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            _searchTerm = If(TxtSearch?.Text?.Trim(), "")
            _currentPage = 1
            LoadPage()
        End If
    End Sub

    Private Sub TxtSearch_TextChanged(sender As Object, e As EventArgs)
        If String.IsNullOrWhiteSpace(TxtSearch?.Text) AndAlso _searchTerm <> "" Then
            _searchTerm = ""
            _currentPage = 1
            LoadPage()
        End If
    End Sub

    Private Sub PaginationControl1_PageChanged(page As Integer)
        _currentPage = page
        LoadPage()
    End Sub

    Private Sub AlignPaginationToPanel()
        Try
            If PaginationControl1 IsNot Nothing AndAlso Guna2Panel1 IsNot Nothing AndAlso Guna2DataGridView1 IsNot Nothing Then
                ' Pagination anchored to the bottom of the panel.
                PaginationControl1.Width = Guna2Panel1.Width - 8
                PaginationControl1.Location = New Point(4, Guna2Panel1.Height - PaginationControl1.Height - 2)
                PaginationControl1.BringToFront()

                ' Grid fills the panel above the pagination.
                Guna2DataGridView1.Width = Guna2Panel1.Width - 8
                Guna2DataGridView1.Location = New Point(8, 72)
                Guna2DataGridView1.Height = PaginationControl1.Top - Guna2DataGridView1.Top - 6
            End If
        Catch
        End Try
    End Sub

    Private Sub SalesRecord_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        AlignPaginationToPanel()
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

    Private Sub Guna2DataGridView1_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles Guna2DataGridView1.CellContentClick

    End Sub
End Class