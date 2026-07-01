Imports System.Globalization
Imports System.IO
Imports System.Linq
Imports Guna.UI2.WinForms
Imports Microsoft.Data.SqlClient

Public Class AuditLog
    Private overlayPanel As Panel
    Private isNavigating As Boolean = False
    ' Add these near the other private fields at the top of the class
    ' Profile managed by ProfileManager
    Private Async Sub AuditLog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me) ' Ensure form background is set (designer has BackColor)
        Me.BackColor = Color.FromArgb(30, 30, 30)
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

        ' Create navigation menu (same style/behavior as SalesRecord)
        CreateNavigationMenu()

        InitializeProfileSection()
        ' Wire events
        AddHandler cmbAccounts.SelectedIndexChanged, AddressOf Filters_Changed
        AddHandler filtertype.SelectedIndexChanged, AddressOf Filters_Changed
        AddHandler Guna2DateTimePicker1.ValueChanged, AddressOf Filters_Changed
        AddHandler Exportbtn.Click, AddressOf Exportbtn_Click

        ' Fix DateTimePicker dropdown for hosted forms
        AddHandler Guna2DateTimePicker1.DropDown, AddressOf DateTimePicker_DropDown

        ' Default date filter to Today and enable the checkbox so filter is active on start
        Try
            Guna2DateTimePicker1.ShowCheckBox = True
            Guna2DateTimePicker1.Value = Date.Today
            Guna2DateTimePicker1.Checked = True
        Catch ex As Exception
            ' Ignore if control not available or doesn't support Checked
        End Try

        ' Load data (with today's date filter active by default)
        Await LoadAuditLogsAsync()
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
            Using rdr As SqlDataReader = Utilities.ExecuteReader(query)
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
                                                  filtertype.SelectedItem.ToString(),
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
        ' Clear existing columns
        InventoryLogDataGrid.Columns.Clear()

        ' Configure DataGridView appearance with consistent gray colors and white row separators
        InventoryLogDataGrid.BackgroundColor = System.Drawing.Color.FromArgb(41, 44, 45)
        InventoryLogDataGrid.GridColor = System.Drawing.Color.White
        InventoryLogDataGrid.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        InventoryLogDataGrid.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        InventoryLogDataGrid.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
        InventoryLogDataGrid.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 204, 77)
        InventoryLogDataGrid.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
        InventoryLogDataGrid.DefaultCellStyle.Font = New System.Drawing.Font("Poppins", 9.0F, System.Drawing.FontStyle.Regular)
        InventoryLogDataGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        ' Configure header style with gray colors and remove blue selection color
        InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
        InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
        InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(30, 30, 30)
        InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.Font = New System.Drawing.Font("Poppins SemiBold", 10.0F, System.Drawing.FontStyle.Regular)
        InventoryLogDataGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        InventoryLogDataGrid.ColumnHeadersHeight = 50
        InventoryLogDataGrid.RowTemplate.Height = 60

        ' Ensure row borders are visible
        InventoryLogDataGrid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal

        ' Set AutoSizeColumnsMode to Fill
        InventoryLogDataGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill

        ' Add Audit ID column
        Dim colAuditID As New DataGridViewTextBoxColumn()
        colAuditID.Name = "AuditID"
        colAuditID.HeaderText = "ID"
        colAuditID.ReadOnly = True
        colAuditID.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        InventoryLogDataGrid.Columns.Add(colAuditID)

        ' Add Username column
        Dim colUsername As New DataGridViewTextBoxColumn()
        colUsername.Name = "Username"
        colUsername.HeaderText = "Username"
        colUsername.ReadOnly = True
        colUsername.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        InventoryLogDataGrid.Columns.Add(colUsername)

        ' Add Action column
        Dim colAction As New DataGridViewTextBoxColumn()
        colAction.Name = "Action"
        colAction.HeaderText = "Action"
        colAction.ReadOnly = True
        colAction.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        InventoryLogDataGrid.Columns.Add(colAction)

        ' Add Details column
        Dim colDetails As New DataGridViewTextBoxColumn()
        colDetails.Name = "Details"
        colDetails.HeaderText = "Details"
        colDetails.ReadOnly = True
        colDetails.DefaultCellStyle = New DataGridViewCellStyle() With {
        .Alignment = DataGridViewContentAlignment.MiddleCenter,
        .WrapMode = DataGridViewTriState.True
    }
        InventoryLogDataGrid.Columns.Add(colDetails)

        ' Add ActionTime column
        Dim colActionTime As New DataGridViewTextBoxColumn()
        colActionTime.Name = "ActionTime"
        colActionTime.HeaderText = "Date & Time"
        colActionTime.ReadOnly = True
        colActionTime.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        InventoryLogDataGrid.Columns.Add(colActionTime)

        ' Add Action Type indicator column
        Dim colActionType As New DataGridViewTextBoxColumn()
        colActionType.Name = "ActionType"
        colActionType.HeaderText = "Type"
        colActionType.ReadOnly = True
        colActionType.DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
        InventoryLogDataGrid.Columns.Add(colActionType)

        ' Configure DataGridView properties
        InventoryLogDataGrid.AllowUserToAddRows = False
        InventoryLogDataGrid.AllowUserToDeleteRows = False
        InventoryLogDataGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        InventoryLogDataGrid.MultiSelect = False
        InventoryLogDataGrid.ScrollBars = ScrollBars.Vertical

        InventoryLogDataGrid.RowHeadersVisible = False
    End Sub

    Private Sub InitializeFilterTypeComboBox()
        filtertype.Items.Clear()
        filtertype.Items.Add("All Logs")
        filtertype.Items.Add("Authentication Events")
        filtertype.Items.Add("Navigation & Access")
        filtertype.Items.Add("Data Creation")
        filtertype.Items.Add("Data Updates")
        filtertype.Items.Add("Data Deletion")
        filtertype.Items.Add("Export Activities")
        filtertype.Items.Add("Session Management")
        filtertype.Items.Add("System Errors")
        filtertype.Items.Add("Information Events")
        filtertype.SelectedIndex = 0
    End Sub

    ' Updated signature: added selectedUser filter parameter
    Private Async Function GetAuditLogsDataAsync(filterType As String, Optional filterDate As DateTime? = Nothing, Optional selectedUser As String = "All Accounts") As Task(Of List(Of Dictionary(Of String, Object)))
        Return Await Task.Run(Function()
                                  Dim auditLogs As New List(Of Dictionary(Of String, Object))()

                                  Dim ft As String = If(filterType, "").Trim().ToLowerInvariant()

                                  Dim query As String = "SELECT a.AuditID, u.Username, a.Action, a.Details, a.ActionTime FROM AuditLog a LEFT JOIN Users u ON a.UserID = u.UserID"
                                  Dim whereClauses As New List(Of String)()
                                  Dim parameters As New List(Of SqlParameter)()

                                  ' Build filter clauses
                                  Select Case ft
                                      Case "authentication events"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%log%' OR LOWER(a.Action) LIKE '%logged%')")
                                      Case "navigation & access"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%navigation%' OR LOWER(a.Action) LIKE '%navigate%' OR LOWER(a.Action) LIKE '%access%')")
                                      Case "data creation"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%add%' OR LOWER(a.Action) LIKE '%create%' OR LOWER(a.Action) LIKE '%added%' OR LOWER(a.Action) LIKE '%created%')")
                                      Case "data updates"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%update%' OR LOWER(a.Action) LIKE '%modify%' OR LOWER(a.Action) LIKE '%edit%' OR LOWER(a.Action) LIKE '%edited%')")
                                      Case "data deletion"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%delete%' OR LOWER(a.Action) LIKE '%remove%' OR LOWER(a.Action) LIKE '%deleted%')")
                                      Case "export activities"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%export%' OR LOWER(a.Action) LIKE '%report%')")
                                      Case "session management"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%session%' OR LOWER(a.Action) LIKE '%pin%' OR LOWER(a.Action) LIKE '%timeout%')")
                                      Case "system errors"
                                          whereClauses.Add("(LOWER(a.Action) LIKE '%error%' OR LOWER(a.Action) LIKE '%failed%' OR LOWER(a.Action) LIKE '%exception%')")
                                      Case "information events"
                                          whereClauses.Add("LOWER(a.Action) NOT LIKE '%log%' AND LOWER(a.Action) NOT LIKE '%logout%' AND LOWER(a.Action) NOT LIKE '%error%' AND LOWER(a.Action) NOT LIKE '%failed%' AND LOWER(a.Action) NOT LIKE '%navigation%' AND LOWER(a.Action) NOT LIKE '%add%' AND LOWER(a.Action) NOT LIKE '%create%' AND LOWER(a.Action) NOT LIKE '%update%' AND LOWER(a.Action) NOT LIKE '%delete%' AND LOWER(a.Action) NOT LIKE '%export%'")
                                      Case "all logs", ""
                                          ' no where clause
                                      Case Else
                                          If ft.StartsWith("authentication") Then whereClauses.Add("(LOWER(a.Action) LIKE '%log%' OR LOWER(a.Action) LIKE '%logged%')")
                                  End Select

                                  ' Date filter
                                  If filterDate.HasValue Then
                                      whereClauses.Add("CAST(a.ActionTime AS DATE) = @FilterDate")
                                      parameters.Add(New SqlParameter("@FilterDate", System.Data.SqlDbType.Date) With {.Value = filterDate.Value.Date})
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

                                  Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters.ToArray())
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

            Dim results = Await GetAuditLogsDataAsync(selectedFilterType, filterDate, selectedUser)

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
        If a.Contains("login") OrElse a.Contains("logout") OrElse a.Contains("logged") Then
            Return "🔐 AUTH"
        ElseIf a.Contains("navigation") OrElse a.Contains("access") OrElse a.Contains("view") Then
            Return "🧭 NAV"
        ElseIf a.Contains("add") OrElse a.Contains("create") OrElse a.Contains("added") OrElse a.Contains("created") Then
            Return "➕ CREATE"
        ElseIf a.Contains("update") OrElse a.Contains("modify") OrElse a.Contains("edit") OrElse a.Contains("edited") Then
            Return "📝 UPDATE"
        ElseIf a.Contains("delete") OrElse a.Contains("remove") OrElse a.Contains("deleted") Then
            Return "🗑️ DELETE"
        ElseIf a.Contains("export") OrElse a.Contains("report") Then
            Return "📄 EXPORT"
        ElseIf a.Contains("error") OrElse a.Contains("failed") Then
            Return "❌ ERROR"
        ElseIf a.Contains("security") OrElse a.Contains("unauthorized") Then
            Return "🛡️ SECURITY"
        ElseIf a.Contains("product") OrElse a.Contains("inventory") Then
            Return "📦 INVENTORY"
        ElseIf a.Contains("session") OrElse a.Contains("pin") Then
            Return "🔑 SESSION"
        Else
            Return "ℹ️ INFO"
        End If
    End Function

    Private Function GetActionTypeColor(action As String) As Color
        Dim a As String = If(action, "").ToLowerInvariant()

        If a.Contains("login") OrElse a.Contains("logout") OrElse a.Contains("logged") Then
            Return Color.FromArgb(52, 152, 219)      ' Blue - Auth
        ElseIf a.Contains("navigation") OrElse a.Contains("access") OrElse a.Contains("view") Then
            Return Color.FromArgb(155, 89, 182)      ' Purple - Navigation
        ElseIf a.Contains("add") OrElse a.Contains("create") OrElse a.Contains("added") OrElse a.Contains("created") Then
            Return Color.FromArgb(46, 204, 113)      ' Green - Create
        ElseIf a.Contains("update") OrElse a.Contains("modify") OrElse a.Contains("edit") OrElse a.Contains("edited") Then
            Return Color.FromArgb(241, 196, 15)      ' Yellow - Update
        ElseIf a.Contains("delete") OrElse a.Contains("remove") OrElse a.Contains("deleted") Then
            Return Color.FromArgb(231, 76, 60)       ' Red - Delete
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
            Return Color.FromArgb(225, 229, 233)     ' Light gray - Info
        End If
    End Function

    ' Navigation menu (copied/styled same as SalesRecord.CreateNavigationMenu)
    Private Sub CreateNavigationMenu()
        NavigationBuilder.Build(DashboardPanel, Me, "AuditLog")
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
        ' already on this form; keep focus
    End Sub

    Private Sub NavSuppliers_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            Supplier.Show()
            Me.Close()
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open Suppliers: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    ' Insert these methods inside the AuditLog class
    Private Sub InitializeProfileSection()
        ProfileManager.InitializeProfile(Me, lblUsername, Guna2CirclePictureBox5, AddressOf NavigateToProfileSettings)
    End Sub

    Private Sub NavigateToProfileSettings()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from AuditLog to ProfileSettings")
            End If

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
                .ForeColor = Color.LightGray,
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
End Class