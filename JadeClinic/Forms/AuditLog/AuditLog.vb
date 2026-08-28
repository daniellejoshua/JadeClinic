Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports Guna.UI2.WinForms
Imports System.Data.Common

Public Class AuditLog
    Private overlayPanel As Panel
    Private isNavigating As Boolean = False

    ' Pagination state
    Private Const PageSize As Integer = 50
    Private _currentPage As Integer = 1

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

    Private Async Sub AuditLog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.BackColor = Color.FromArgb(248, 248, 247)
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)
        Me.FormBorderStyle = FormBorderStyle.None
        Me.TopMost = True
        Me.WindowState = FormWindowState.Normal
        Me.Bounds = Screen.PrimaryScreen.Bounds
        Me.WindowState = FormWindowState.Maximized

        ' Create and configure overlay panel (same color as form)
        overlayPanel = New Panel() With {
        .Dock = DockStyle.Fill,
        .BackColor = Me.BackColor,
        .Visible = False
    }
        Me.Controls.Add(overlayPanel)
        overlayPanel.BringToFront()

        ' Initialize controls
        InitializeDataGridView()
        InitializeFilterTypeComboBox()
        InitializeUserAccountsCombo()

        ' Create navigation menu (same style/behavior as other pages)
        CreateNavigationMenu()

        InitializeProfileSection()
        ' Wire events
        AddHandler cmbAccounts.SelectedIndexChanged, AddressOf Filters_Changed
        AddHandler filtertype.SelectedIndexChanged, AddressOf Filters_Changed
        AddHandler Guna2DateTimePicker1.ValueChanged, AddressOf Filters_Changed
        AddHandler Exportbtn.Click, AddressOf Exportbtn_Click
        If PaginationControl1 IsNot Nothing Then
            AddHandler PaginationControl1.PageChanged, AddressOf PaginationControl1_PageChanged
        End If
        If Guna2Panel1 IsNot Nothing Then
            AddHandler Guna2Panel1.Resize, AddressOf AlignPaginationToPanel
        End If

        ' Fix DateTimePicker dropdown for hosted forms
        AddHandler Guna2DateTimePicker1.DropDown, AddressOf DateTimePicker_DropDown

        ' Default date filter to Today on start
        Try
            Guna2DateTimePicker1.ShowCheckBox = True
            Guna2DateTimePicker1.Value = Date.Today
            Guna2DateTimePicker1.Checked = True
        Catch ex As Exception
            ' Ignore if control not available or doesn't support Checked
        End Try

        ' Align grid + pagination to the panel (now that the form is maximized and laid out)
        AlignPaginationToPanel(Nothing, EventArgs.Empty)

        ' Load data (with today's date filter active by default)
        Await LoadAuditLogsAsync()

        SetupTabIndex()
    End Sub

    Private Sub SetupTabIndex()
        Guna2DateTimePicker1.TabIndex = 0
        filtertype.TabIndex = 1
        cmbAccounts.TabIndex = 2
        Exportbtn.TabIndex = 3
        Utilities.ApplyInputFocusEffects(Me)
    End Sub

    Private Sub DateTimePicker_DropDown(sender As Object, e As EventArgs)
        ' DateTimePicker dropdown now works without TopMost toggle
    End Sub

    Private Sub DateTimePicker_CloseUp(sender As Object, e As EventArgs)
        ' No longer needed
    End Sub

    ' Call this from AuditLog_Load (after InitializeFilterTypeComboBox)
    Private Sub InitializeUserAccountsCombo()
        Try
            ' Ensure a ComboBox named cmbAccounts exists on the form (designer)
            cmbAccounts.Items.Clear()
            cmbAccounts.Items.Add("All Accounts")

            Dim query As String = "SELECT Username FROM Users WHERE Username IS NOT NULL AND Username <> '' ORDER BY Username"
            Using rdr As DbDataReader = Utilities.ExecuteReader(query)
                While rdr.Read()
                    If Not IsDBNull(rdr("Username")) Then
                        cmbAccounts.Items.Add(rdr("Username").ToString())
                    End If
                End While
            End Using

            If cmbAccounts.Items.Count > 0 Then
                cmbAccounts.SelectedIndex = 0
            End If

            RemoveHandler cmbAccounts.SelectedIndexChanged, AddressOf Filters_Changed
            AddHandler cmbAccounts.SelectedIndexChanged, AddressOf Filters_Changed
        Catch ex As Exception
            Console.WriteLine($"InitializeUserAccountsCombo error: {ex.Message}")
        End Try
    End Sub
    Private Sub Exportbtn_Click(sender As Object, e As EventArgs)
        Try
            Dim selectedFilterType As String = If(filtertype IsNot Nothing AndAlso filtertype.SelectedItem IsNot Nothing,
                                                  StripLeadingEmoji(filtertype.SelectedItem.ToString()),
                                                  "All Logs")

            Dim selectedUser As String = If(cmbAccounts IsNot Nothing AndAlso cmbAccounts.SelectedItem IsNot Nothing,
                                            cmbAccounts.SelectedItem.ToString(),
                                            "All Accounts")

            Dim selectedDate As DateTime? = Nothing
            If Guna2DateTimePicker1 IsNot Nothing AndAlso Guna2DateTimePicker1.ShowCheckBox AndAlso Guna2DateTimePicker1.Checked Then
                selectedDate = Guna2DateTimePicker1.Value.Date
            End If

            AuditExporter.ExportAuditLogsReport(
                sortOrder:="Newest First",
                filterType:=selectedFilterType,
                filterDate:=selectedDate,
                selectedUser:=selectedUser)

        Catch ex As Exception
            MessageBox.Show($"Error exporting audit logs: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Async Sub Filters_Changed(sender As Object, e As EventArgs)
        ' Reset to first page when the filters change.
        _currentPage = 1
        ' Kick off async refresh but don't block UI
        Await LoadAuditLogsAsync()
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
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Audit Log.")
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

        ' Add Audit ID column
        Dim colAuditID As New DataGridViewTextBoxColumn()
        colAuditID.Name = "AuditID"
        colAuditID.HeaderText = "ID"
        colAuditID.ReadOnly = True
        colAuditID.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        colAuditID.FillWeight = 6
        InventoryLogDataGrid.Columns.Add(colAuditID)

        ' Add Username column
        Dim colUsername As New DataGridViewTextBoxColumn()
        colUsername.Name = "Username"
        colUsername.HeaderText = "Username"
        colUsername.ReadOnly = True
        colUsername.DefaultCellStyle = New DataGridViewCellStyle() With {
        .Alignment = DataGridViewContentAlignment.MiddleCenter,
        .Font = New Font("Poppins SemiBold", 9.0F, FontStyle.Regular),
        .ForeColor = Color.FromArgb(51, 51, 51)
    }
        colUsername.FillWeight = 12
        InventoryLogDataGrid.Columns.Add(colUsername)

        ' Add Action column
        Dim colAction As New DataGridViewTextBoxColumn()
        colAction.Name = "Action"
        colAction.HeaderText = "Action"
        colAction.ReadOnly = True
        colAction.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        colAction.FillWeight = 18
        InventoryLogDataGrid.Columns.Add(colAction)

        ' Add Details column
        Dim colDetails As New DataGridViewTextBoxColumn()
        colDetails.Name = "Details"
        colDetails.HeaderText = "Details"
        colDetails.ReadOnly = True
        colDetails.DefaultCellStyle = New DataGridViewCellStyle() With {
        .Alignment = DataGridViewContentAlignment.MiddleLeft,
        .WrapMode = DataGridViewTriState.True,
        .ForeColor = Color.FromArgb(102, 102, 102)
    }
        colDetails.FillWeight = 30
        InventoryLogDataGrid.Columns.Add(colDetails)

        ' Add ActionTime column
        Dim colActionTime As New DataGridViewTextBoxColumn()
        colActionTime.Name = "ActionTime"
        colActionTime.HeaderText = "Date & Time"
        colActionTime.ReadOnly = True
        colActionTime.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        colActionTime.FillWeight = 18
        InventoryLogDataGrid.Columns.Add(colActionTime)

        ' Add Action Type indicator column
        Dim colActionType As New DataGridViewTextBoxColumn()
        colActionType.Name = "ActionType"
        colActionType.HeaderText = "Type"
        colActionType.ReadOnly = True
        colActionType.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        colActionType.FillWeight = 10
        InventoryLogDataGrid.Columns.Add(colActionType)
    End Sub

    Private Sub InitializeFilterTypeComboBox()
        filtertype.Items.Clear()
        filtertype.Items.Add("All Logs")
        filtertype.Items.Add("Authentication Events")
        filtertype.Items.Add("Data Creation")
        filtertype.Items.Add("Data Updates")
        filtertype.Items.Add("Void Activities")
        filtertype.Items.Add("Export Activities")
        filtertype.Items.Add("Session Management")
        filtertype.Items.Add("System Errors")
        filtertype.Items.Add("Information Events")
        filtertype.SelectedIndex = 0
    End Sub

    Private Function StripLeadingEmoji(source As String) As String
        If String.IsNullOrWhiteSpace(source) Then Return source
        Dim i As Integer = 0
        While i < source.Length AndAlso Not Char.IsLetter(source(i))
            i += 1
        End While
        Return source.Substring(i).Trim()
    End Function

    ' Updated signature: added selectedUser filter parameter
    Private Async Function GetAuditLogsDataAsync(filterType As String, Optional filterDate As DateTime? = Nothing, Optional selectedUser As String = "All Accounts", Optional pageNumber As Integer = 1, Optional pageSize As Integer = PageSize) As Task(Of List(Of Dictionary(Of String, Object)))
        Return Await Task.Run(Function()
                                  Dim auditLogs As New List(Of Dictionary(Of String, Object))()

                                  Dim ft As String = StripLeadingEmoji(filterType).Trim().ToLowerInvariant()

                                  Dim query As String = "SELECT a.AuditID, u.Username, a.Action, a.Details, a.ActionTime FROM AuditLog a LEFT JOIN Users u ON a.UserID = u.UserID"
                                  Dim whereClauses As New List(Of String)()
                                  Dim parameters As New List(Of SqlParameter)()

                                  ' Build filter clauses
                                  Select Case ft
                                      Case "authentication events"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%login%' OR LOWER(a.Action) LIKE '%logged%' OR LOWER(a.Action) LIKE '%logout%' OR LOWER(a.Action) LIKE '%log in%' OR LOWER(a.Action) LIKE '%log out%' OR LOWER(a.Action) LIKE '%authentication%' OR LOWER(a.Action) LIKE '%password%' OR LOWER(a.Action) LIKE '%passkey%' OR LOWER(a.Action) LIKE '%attempt%' OR LOWER(a.Action) LIKE '%exit%')")
                                      Case "data creation"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%add%' OR LOWER(a.Action) LIKE '%create%' OR LOWER(a.Action) LIKE '%added%' OR LOWER(a.Action) LIKE '%created%')")
                                      Case "data updates"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%update%' OR LOWER(a.Action) LIKE '%modify%' OR LOWER(a.Action) LIKE '%edit%' OR LOWER(a.Action) LIKE '%edited%')")
                                      Case "void activities"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%void%' OR LOWER(a.Action) LIKE '%delete%' OR LOWER(a.Action) LIKE '%remove%' OR LOWER(a.Action) LIKE '%deleted%')")
                                      Case "export activities"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%export%' OR LOWER(a.Action) LIKE '%report%')")
                                      Case "session management"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%session%' OR LOWER(a.Action) LIKE '%pin%' OR LOWER(a.Action) LIKE '%timeout%')")
                                      Case "system errors"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%error%' OR LOWER(a.Action) LIKE '%failed%' OR LOWER(a.Action) LIKE '%exception%')")
                                      Case "information events"
                                          whereClauses.Add("LOWER(a.Action) NOT LIKE '%log%' AND LOWER(a.Action) NOT LIKE '%logout%' AND LOWER(a.Action) NOT LIKE '%error%' AND LOWER(a.Action) NOT LIKE '%failed%' AND LOWER(a.Action) NOT LIKE '%navigation%' AND LOWER(a.Action) NOT LIKE '%add%' AND LOWER(a.Action) NOT LIKE '%create%' AND LOWER(a.Action) NOT LIKE '%update%' AND LOWER(a.Action) NOT LIKE '%delete%' AND LOWER(a.Action) NOT LIKE '%void%' AND LOWER(a.Action) NOT LIKE '%exit%' AND LOWER(a.Action) NOT LIKE '%export%'")
                                      Case "all logs", ""
                                          ' no where clause
                                      Case Else
                                          If ft.StartsWith("authentication") Then whereClauses.Add("(LOWER(a.Action) LIKE '%login%' OR LOWER(a.Action) LIKE '%logged%' OR LOWER(a.Action) LIKE '%logout%' OR LOWER(a.Action) LIKE '%log in%' OR LOWER(a.Action) LIKE '%log out%' OR LOWER(a.Action) LIKE '%authentication%' OR LOWER(a.Action) LIKE '%password%' OR LOWER(a.Action) LIKE '%passkey%' OR LOWER(a.Action) LIKE '%attempt%' OR LOWER(a.Action) LIKE '%exit%')")
                                  End Select

                                  ' Date filter
                                  If filterDate.HasValue Then
                                      whereClauses.Add("DATE(a.ActionTime) = @FilterDate")
                                      parameters.Add(New SqlParameter("@FilterDate", filterDate.Value.Date.ToString("yyyy-MM-dd")))
                                  End If

                                  ' User filter
                                  If Not String.IsNullOrWhiteSpace(selectedUser) AndAlso selectedUser <> "All Accounts" Then
                                      whereClauses.Add("u.Username = @Username")
                                      parameters.Add(New SqlParameter("@Username", selectedUser))
                                  End If

                                  If whereClauses.Count > 0 Then
                                      query += " WHERE " & String.Join(" AND ", whereClauses)
                                  End If

                                  ' Fixed sort (newest first)
                                  query += " ORDER BY a.ActionTime DESC"

                                  ' Paging
                                  Dim offset As Integer = (pageNumber - 1) * pageSize
                                  query &= $" LIMIT {pageSize} OFFSET {offset}"

                                  Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters.ToArray())
                                      While reader.Read()
                                          Dim auditData As New Dictionary(Of String, Object) From {
                                          {"AuditID", Convert.ToInt32(reader("AuditID"))},
                                          {"Username", If(IsDBNull(reader("Username")), "", reader("Username").ToString())},
                                          {"Action", If(IsDBNull(reader("Action")), "", reader("Action").ToString())},
                                          {"Details", If(IsDBNull(reader("Details")), "", reader("Details").ToString())},
                                          {"ActionTime", Convert.ToDateTime(reader("ActionTime"))}
                                      }
                                          auditLogs.Add(auditData)
                                      End While
                                  End Using

                                  Return auditLogs
                              End Function)
    End Function
    Private Async Function CountAuditLogsAsync(filterType As String, Optional filterDate As DateTime? = Nothing, Optional selectedUser As String = "All Accounts") As Task(Of Integer)
        Return Await Task.Run(Function()
                                  Dim ft As String = StripLeadingEmoji(filterType).Trim().ToLowerInvariant()
                                  Dim query As String = "SELECT COUNT(*) FROM AuditLog a LEFT JOIN Users u ON a.UserID = u.UserID"
                                  Dim whereClauses As New List(Of String)()
                                  Dim parameters As New List(Of SqlParameter)()

                                  Select Case ft
                                      Case "authentication events"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%login%' OR LOWER(a.Action) LIKE '%logged%' OR LOWER(a.Action) LIKE '%logout%' OR LOWER(a.Action) LIKE '%log in%' OR LOWER(a.Action) LIKE '%log out%' OR LOWER(a.Action) LIKE '%authentication%' OR LOWER(a.Action) LIKE '%password%' OR LOWER(a.Action) LIKE '%passkey%' OR LOWER(a.Action) LIKE '%attempt%' OR LOWER(a.Action) LIKE '%exit%')")
                                      Case "data creation"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%add%' OR LOWER(a.Action) LIKE '%create%' OR LOWER(a.Action) LIKE '%added%' OR LOWER(a.Action) LIKE '%created%')")
                                      Case "data updates"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%update%' OR LOWER(a.Action) LIKE '%modify%' OR LOWER(a.Action) LIKE '%edit%' OR LOWER(a.Action) LIKE '%edited%')")
                                      Case "void activities"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%void%' OR LOWER(a.Action) LIKE '%delete%' OR LOWER(a.Action) LIKE '%remove%' OR LOWER(a.Action) LIKE '%deleted%')")
                                      Case "export activities"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%export%' OR LOWER(a.Action) LIKE '%report%')")
                                      Case "session management"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%session%' OR LOWER(a.Action) LIKE '%pin%' OR LOWER(a.Action) LIKE '%timeout%')")
                                      Case "system errors"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%error%' OR LOWER(a.Action) LIKE '%failed%' OR LOWER(a.Action) LIKE '%exception%')")
                                      Case "information events"
                                          whereClauses.Add("LOWER(a.Action) NOT LIKE '%log%' AND LOWER(a.Action) NOT LIKE '%logout%' AND LOWER(a.Action) NOT LIKE '%error%' AND LOWER(a.Action) NOT LIKE '%failed%' AND LOWER(a.Action) NOT LIKE '%navigation%' AND LOWER(a.Action) NOT LIKE '%add%' AND LOWER(a.Action) NOT LIKE '%create%' AND LOWER(a.Action) NOT LIKE '%update%' AND LOWER(a.Action) NOT LIKE '%delete%' AND LOWER(a.Action) NOT LIKE '%void%' AND LOWER(a.Action) NOT LIKE '%exit%' AND LOWER(a.Action) NOT LIKE '%export%'")
                                  End Select

                                  If filterDate.HasValue Then
                                      whereClauses.Add("DATE(a.ActionTime) = @FilterDate")
                                      parameters.Add(New SqlParameter("@FilterDate", filterDate.Value.Date.ToString("yyyy-MM-dd")))
                                  End If

                                  If Not String.IsNullOrWhiteSpace(selectedUser) AndAlso selectedUser <> "All Accounts" Then
                                      whereClauses.Add("u.Username = @Username")
                                      parameters.Add(New SqlParameter("@Username", selectedUser))
                                  End If

                                  If whereClauses.Count > 0 Then
                                      query += " WHERE " & String.Join(" AND ", whereClauses)
                                  End If

                                  Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters.ToArray())
                                      If reader.Read() Then
                                          Return Convert.ToInt32(reader(0))
                                      End If
                                  End Using
                                  Return 0
                              End Function)
    End Function

    Private Async Function LoadAuditLogsAsync() As Task
        Try
            ' Show inline loading label on DataGridView
            ShowLoadingLabel("Loading filters...")

            Dim selectedFilterType As String = If(filtertype.SelectedItem IsNot Nothing, filtertype.SelectedItem.ToString(), "All Logs")
            Dim filterDate As DateTime? = Nothing
            If Guna2DateTimePicker1 IsNot Nothing AndAlso Guna2DateTimePicker1.ShowCheckBox AndAlso Guna2DateTimePicker1.Checked Then
                filterDate = Guna2DateTimePicker1.Value.Date
            End If

            Dim selectedUser As String = "All Accounts"
            If cmbAccounts IsNot Nothing AndAlso cmbAccounts.SelectedItem IsNot Nothing Then
                selectedUser = cmbAccounts.SelectedItem.ToString()
            End If

            ' Fetch the total count (for pagination) and just the current page.
            Dim totalCount As Integer = Await CountAuditLogsAsync(selectedFilterType, filterDate, selectedUser)
            Dim results = Await GetAuditLogsDataAsync(selectedFilterType, filterDate, selectedUser, _currentPage, PageSize)

            ' Configure pagination (clamps current page if it exceeds total pages)
            If PaginationControl1 IsNot Nothing Then
                PaginationControl1.Configure(totalCount, PageSize, _currentPage)
                _currentPage = PaginationControl1.CurrentPage
            End If

            ' Remove any existing no-records label
            Dim existingLbl = Me.Controls.OfType(Of Label)().FirstOrDefault(Function(l) l.Name = "lblNoAuditLogs")
            If existingLbl IsNot Nothing Then
                Me.Controls.Remove(existingLbl)
                existingLbl.Dispose()
            End If

            ' Clear grid before updating
            InventoryLogDataGrid.Rows.Clear()
            
            ' Hide any existing "No records" message
            DataGridViewHelper.HideNoRecordsMessage()

            ' Handle no results
            If results Is Nothing OrElse results.Count = 0 Then
                DataGridViewHelper.ShowNoRecordsMessage(InventoryLogDataGrid, "No Audit Logs Found")
                HideLoadingLabel()
                Return
            End If

            ' Populate grid
            For Each rowData In results
                Dim rowIndex As Integer = InventoryLogDataGrid.Rows.Add()
                InventoryLogDataGrid.Rows(rowIndex).Cells("AuditID").Value = If(rowData.ContainsKey("AuditID"), rowData("AuditID").ToString(), String.Empty)
                InventoryLogDataGrid.Rows(rowIndex).Cells("Username").Value = If(rowData.ContainsKey("Username"), rowData("Username").ToString(), String.Empty)
                InventoryLogDataGrid.Rows(rowIndex).Cells("Action").Value = If(rowData.ContainsKey("Action"), rowData("Action").ToString(), String.Empty)
                InventoryLogDataGrid.Rows(rowIndex).Cells("Details").Value = If(rowData.ContainsKey("Details"), rowData("Details").ToString(), String.Empty)

                If rowData.ContainsKey("ActionTime") AndAlso TypeOf rowData("ActionTime") Is DateTime Then
                    InventoryLogDataGrid.Rows(rowIndex).Cells("ActionTime").Value = CType(rowData("ActionTime"), DateTime).ToString("MM/dd/yyyy HH:mm")
                Else
                    InventoryLogDataGrid.Rows(rowIndex).Cells("ActionTime").Value = String.Empty
                End If

                Dim actionText As String = If(rowData.ContainsKey("Action"), rowData("Action").ToString(), String.Empty)
                Dim actionType As String = GetActionType(actionText)

                InventoryLogDataGrid.Rows(rowIndex).Cells("ActionType").Value = actionType
                InventoryLogDataGrid.Rows(rowIndex).Cells("ActionType").Style.ForeColor = GetActionTypeColor(actionText)
                InventoryLogDataGrid.Rows(rowIndex).Cells("ActionType").Style.SelectionForeColor = Color.Black
                InventoryLogDataGrid.Rows(rowIndex).Cells("ActionType").Style.Font = New Font("Poppins SemiBold", 9.0F, FontStyle.Bold)

                ' Optional: tag row with original data for later use
                InventoryLogDataGrid.Rows(rowIndex).Tag = rowData
            Next

            InventoryLogDataGrid.ClearSelection()
            InventoryLogDataGrid.Refresh()

        Catch ex As Exception
            MessageBox.Show($"Error loading audit logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            HideLoadingLabel()
        End Try
    End Function ' Map action text to emoji + short label for grid
    Private Function GetActionType(action As String) As String
        Dim a As String = If(action, "").ToLowerInvariant()
        If a.Contains("login") OrElse a.Contains("logged") OrElse a.Contains("logout") OrElse a.Contains("authentication") OrElse a.Contains("password") OrElse a.Contains("passkey") OrElse a.Contains("attempt") OrElse a.Contains("exit") Then
            Return Char.ConvertFromUtf32(&H1F511) & " AUTH"
        ElseIf a.Contains("navigation") OrElse a.Contains("access") OrElse a.Contains("view") Then
            Return Char.ConvertFromUtf32(&H1F9ED) & " NAV"
        ElseIf a.Contains("add") OrElse a.Contains("create") OrElse a.Contains("added") OrElse a.Contains("created") Then
            Return ChrW(&H2795) & " CREATE"
        ElseIf a.Contains("update") OrElse a.Contains("modify") OrElse a.Contains("edit") OrElse a.Contains("edited") Then
            Return Char.ConvertFromUtf32(&H1F504) & " UPDATE"
        ElseIf a.Contains("void") OrElse a.Contains("delete") OrElse a.Contains("remove") OrElse a.Contains("deleted") Then
            Return Char.ConvertFromUtf32(&H1F6AB) & " VOID"
        ElseIf a.Contains("export") OrElse a.Contains("report") Then
            Return Char.ConvertFromUtf32(&H1F4E4) & " EXPORT"
        ElseIf a.Contains("error") OrElse a.Contains("failed") Then
            Return ChrW(&H26A0) & " ERROR"
        ElseIf a.Contains("security") OrElse a.Contains("unauthorized") Then
            Return Char.ConvertFromUtf32(&H1F6E1) & " SECURITY"
        ElseIf a.Contains("product") OrElse a.Contains("inventory") Then
            Return Char.ConvertFromUtf32(&H1F4E6) & " INVENTORY"
        ElseIf a.Contains("session") OrElse a.Contains("pin") Then
            Return ChrW(&H23F1) & " SESSION"
        Else
            Return ChrW(&H2139) & " INFO"
        End If
    End Function

    Private Function GetActionTypeColor(action As String) As Color
        Dim a As String = If(action, "").ToLowerInvariant()

        If a.Contains("login") OrElse a.Contains("logout") OrElse a.Contains("logged") OrElse a.Contains("authentication") OrElse a.Contains("password") OrElse a.Contains("passkey") OrElse a.Contains("attempt") OrElse a.Contains("exit") Then
            Return Color.FromArgb(52, 152, 219)      ' Blue - Auth
        ElseIf a.Contains("navigation") OrElse a.Contains("access") OrElse a.Contains("view") Then
            Return Color.FromArgb(155, 89, 182)      ' Purple - Navigation
        ElseIf a.Contains("add") OrElse a.Contains("create") OrElse a.Contains("added") OrElse a.Contains("created") Then
            Return Color.FromArgb(46, 204, 113)      ' Green - Create
        ElseIf a.Contains("update") OrElse a.Contains("modify") OrElse a.Contains("edit") OrElse a.Contains("edited") Then
            Return Color.FromArgb(212, 172, 13)      ' Dark gold - Update
        ElseIf a.Contains("void") OrElse a.Contains("delete") OrElse a.Contains("remove") OrElse a.Contains("deleted") Then
            Return Color.FromArgb(231, 76, 60)       ' Red - Void/Delete
        ElseIf a.Contains("export") OrElse a.Contains("report") Then
            Return Color.FromArgb(26, 188, 156)      ' Teal - Export
        ElseIf a.Contains("error") OrElse a.Contains("failed") Then
            Return Color.FromArgb(255, 71, 87)       ' Error red
        ElseIf a.Contains("security") OrElse a.Contains("unauthorized") Then
            Return Color.FromArgb(230, 126, 34)      ' Orange - Security
        ElseIf a.Contains("product") OrElse a.Contains("inventory") Then
            Return Color.FromArgb(39, 174, 96)       ' Dark green - Inventory
        ElseIf a.Contains("session") OrElse a.Contains("pin") Then
            Return Color.FromArgb(142, 68, 173)      ' Deep purple - Session
        Else
            Return Color.FromArgb(153, 153, 153)     ' Medium gray - Info
        End If
    End Function

    ' Insert these methods inside the AuditLog class
    Private Sub CreateNavigationMenu()
        NavigationBuilder.Build(DashboardPanel, Me, "AuditLog")
    End Sub

    Private Sub InitializeProfileSection()
        ProfileManager.InitializeProfile(Me, lblUsername, Guna2CirclePictureBox5, AddressOf NavigateToProfileSettings)
    End Sub

    Private Sub NavigateToProfileSettings()
        Try
            isNavigating = True
            ProfileManager.HideProfileDropdown(Me)

            Dim profileForm As New ProfileSettings()
            profileForm.StartPosition = FormStartPosition.CenterScreen
            profileForm.Show()

            Me.Close()
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open Profile Settings: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub AuditLog_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)
        If isNavigating Then
            Return
        End If
        ' Show confirmation only for user-initiated close (X button)
        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = EscForm.ConfirmExit(Me)

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

    Private loadingLabel As Label = Nothing

    Private Sub ShowLoadingLabel(message As String)
        If loadingLabel Is Nothing Then
            loadingLabel = New Label() With {
                .Text = message,
                .Font = New Font("Poppins", 11, FontStyle.Italic),
                .ForeColor = Color.FromArgb(51, 51, 51),
                .BackColor = Color.Transparent,
                .AutoSize = True,
                .Name = "loadingLabel"
            }
        Else
            loadingLabel.Text = message
        End If

        ' Position over the DataGridView
        If InventoryLogDataGrid IsNot Nothing Then
            loadingLabel.Location = New Point(
                InventoryLogDataGrid.Left + (InventoryLogDataGrid.Width \ 2) - (loadingLabel.Width \ 2),
                InventoryLogDataGrid.Top + (InventoryLogDataGrid.Height \ 2) - (loadingLabel.Height \ 2)
            )
            InventoryLogDataGrid.Parent.Controls.Add(loadingLabel)
            loadingLabel.BringToFront()
        End If
    End Sub

    Private Sub HideLoadingLabel()
        If loadingLabel IsNot Nothing Then
            Try
                loadingLabel.Parent.Controls.Remove(loadingLabel)
                loadingLabel.Dispose()
            Catch
            End Try
            loadingLabel = Nothing
        End If
    End Sub

    Private Async Sub PaginationControl1_PageChanged(page As Integer)
        _currentPage = page
        Await LoadAuditLogsAsync()
    End Sub

    Private Sub AlignPaginationToPanel(sender As Object, e As EventArgs)
        If PaginationControl1 Is Nothing OrElse Guna2Panel1 Is Nothing OrElse InventoryLogDataGrid Is Nothing Then Return

        ' Pagination anchored to the bottom of the panel.
        PaginationControl1.Width = Guna2Panel1.Width - 8
        PaginationControl1.Location = New Point(4, Guna2Panel1.Height - PaginationControl1.Height - 10)

        ' Grid fills the panel above the pagination.
        InventoryLogDataGrid.Width = Guna2Panel1.Width - 8
        InventoryLogDataGrid.Location = New Point(8, 72)
        InventoryLogDataGrid.Height = PaginationControl1.Top - InventoryLogDataGrid.Top - 6
    End Sub
End Class