Imports Microsoft.Data.SqlClient

Imports System.IO

Public Class AuditLog
    Private overlayPanel As Panel
    Private isNavigating As Boolean = False

    Private Async Sub AuditLog_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Ensure form background is set (designer has BackColor)
        Me.BackColor = Color.FromArgb(30, 30, 30)

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
        InitializeSortComboBox()
        InitializeFilterTypeComboBox()

        ' Create navigation menu (same style/behavior as SalesRecord)
        CreateNavigationMenu()

        ' Wire events
        AddHandler SortBy.SelectedIndexChanged, AddressOf Filters_Changed
        AddHandler filtertype.SelectedIndexChanged, AddressOf Filters_Changed
        AddHandler Guna2DateTimePicker1.ValueChanged, AddressOf Filters_Changed
        AddHandler Exportbtn.Click, AddressOf Exportbtn_Click

        ' Prevent unintended default date filtering: start unchecked and default to today
        Try
            Guna2DateTimePicker1.Checked = False
            Guna2DateTimePicker1.Value = DateTime.Now
        Catch ex As Exception
            ' Ignore if control not available or doesn't support Checked
        End Try

        ' Load data
        Await LoadAuditLogsAsync()
    End Sub

    Private Sub Exportbtn_Click(sender As Object, e As EventArgs)
        MessageBox.Show("Export not implemented.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Async Sub Filters_Changed(sender As Object, e As EventArgs)
        ' Kick off async refresh but don't block UI
        Await LoadAuditLogsAsync()
    End Sub

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

    Private Sub InitializeSortComboBox()
        SortBy.Items.Clear()
        SortBy.Items.Add("Date (Newest First)")
        SortBy.Items.Add("Date (Oldest First)")
        SortBy.Items.Add("Username (A-Z)")
        SortBy.Items.Add("Username (Z-A)")
        SortBy.Items.Add("Action Type (A-Z)")
        SortBy.Items.Add("Action Type (Z-A)")
        SortBy.Items.Add("Audit ID (Ascending)")
        SortBy.Items.Add("Audit ID (Descending)")
        SortBy.SelectedIndex = 0
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

    Private Async Function GetAuditLogsDataAsync(sortOrder As String, filterType As String, Optional filterDate As DateTime? = Nothing) As Task(Of List(Of Dictionary(Of String, Object)))
        Return Await Task.Run(Function()
                                  Dim auditLogs As New List(Of Dictionary(Of String, Object))()

                                  Dim ft As String = If(filterType, "").Trim().ToLowerInvariant()
                                  Dim so As String = If(sortOrder, "").Trim()

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

                                  If whereClauses.Count > 0 Then
                                      query += " WHERE " & String.Join(" AND ", whereClauses)
                                  End If

                                  ' Sorting
                                  Select Case so
                                      Case "Date (Newest First)"
                                          query += " ORDER BY a.ActionTime DESC"
                                      Case "Date (Oldest First)"
                                          query += " ORDER BY a.ActionTime ASC"
                                      Case "Username (A-Z)"
                                          query += " ORDER BY u.Username ASC, a.ActionTime DESC"
                                      Case "Username (Z-A)"
                                          query += " ORDER BY u.Username DESC, a.ActionTime DESC"
                                      Case "Action Type (A-Z)"
                                          query += " ORDER BY a.Action ASC, a.ActionTime DESC"
                                      Case "Action Type (Z-A)"
                                          query += " ORDER BY a.Action DESC, a.ActionTime DESC"
                                      Case "Audit ID (Ascending)"
                                          query += " ORDER BY a.AuditID ASC"
                                      Case "Audit ID (Descending)"
                                          query += " ORDER BY a.AuditID DESC"
                                      Case Else
                                          query += " ORDER BY a.ActionTime DESC"
                                  End Select

                                  ' Execute reader and build results (no modal debug here)
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

                                  Console.WriteLine($"GetAuditLogsDataAsync returned {auditLogs.Count} rows")

                                  Return auditLogs
                              End Function)
    End Function

    Private Async Function LoadAuditLogsAsync() As Task
        Try
            overlayPanel.Visible = True
            overlayPanel.BringToFront()

            Dim sortOrder As String = If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), "Date (Newest First)")
            Dim selectedFilterType As String = If(filtertype.SelectedItem IsNot Nothing, filtertype.SelectedItem.ToString(), "All Logs")
            Dim filterDate As DateTime? = Nothing
            If Guna2DateTimePicker1.Checked Then
                filterDate = Guna2DateTimePicker1.Value.Date
            End If

            Dim results = Await GetAuditLogsDataAsync(sortOrder, selectedFilterType, filterDate)

            ' Remove any previous "no records" label
            Dim existingLbl = Me.Controls.OfType(Of Label)().FirstOrDefault(Function(l) l.Name = "lblNoAuditLogs")
            If existingLbl IsNot Nothing Then
                Me.Controls.Remove(existingLbl)
                existingLbl.Dispose()
            End If

            ' Clear grid before updating
            InventoryLogDataGrid.Rows.Clear()

            ' If no results, show a single styled row inside the DataGridView
            If results Is Nothing OrElse results.Count = 0 Then
                Dim rowIndex As Integer = InventoryLogDataGrid.Rows.Add()
                ' Ensure all cells are empty first
                For i As Integer = 0 To InventoryLogDataGrid.Columns.Count - 1
                    InventoryLogDataGrid.Rows(rowIndex).Cells(i).Value = String.Empty
                Next

                ' Put the message in the Details column if present, otherwise first column
                If InventoryLogDataGrid.Columns.Contains("Details") Then
                    InventoryLogDataGrid.Rows(rowIndex).Cells("Details").Value = "No audit logs found."
                Else
                    InventoryLogDataGrid.Rows(rowIndex).Cells(0).Value = "No audit logs found."
                End If

                ' Style the no-records row to stand out and be centered
                Dim noRow As DataGridViewRow = InventoryLogDataGrid.Rows(rowIndex)
                noRow.ReadOnly = True
                noRow.DefaultCellStyle.ForeColor = Color.LightGray
                noRow.DefaultCellStyle.BackColor = InventoryLogDataGrid.DefaultCellStyle.BackColor
                noRow.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
                noRow.DefaultCellStyle.Font = New Font(InventoryLogDataGrid.DefaultCellStyle.Font.FontFamily, InventoryLogDataGrid.DefaultCellStyle.Font.Size, FontStyle.Italic)

                InventoryLogDataGrid.ClearSelection()
                overlayPanel.Visible = False
                Return
            End If

            ' Populate grid with results
            For Each rowData In results
                Dim rowIndex As Integer = InventoryLogDataGrid.Rows.Add()
                InventoryLogDataGrid.Rows(rowIndex).Cells("AuditID").Value = rowData("AuditID").ToString()
                InventoryLogDataGrid.Rows(rowIndex).Cells("Username").Value = rowData("Username").ToString()
                InventoryLogDataGrid.Rows(rowIndex).Cells("Action").Value = rowData("Action").ToString()
                InventoryLogDataGrid.Rows(rowIndex).Cells("Details").Value = rowData("Details").ToString()
                InventoryLogDataGrid.Rows(rowIndex).Cells("ActionTime").Value = CType(rowData("ActionTime"), DateTime).ToString("MM/dd/yyyy HH:mm")
                InventoryLogDataGrid.Rows(rowIndex).Cells("ActionType").Value = GetActionType(rowData("Action").ToString())
            Next

            InventoryLogDataGrid.ClearSelection()
            InventoryLogDataGrid.Refresh()

        Catch ex As Exception
            MessageBox.Show($"Error loading audit logs: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            overlayPanel.Visible = False
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

    ' Navigation menu (copied/styled same as SalesRecord.CreateNavigationMenu)
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

            ' Render company logo into existing PictureBox9
            If PictureBox9 IsNot Nothing Then
                Try
                    Dim logoImg As System.Drawing.Image = CompanySettingsManager.Instance.GetCompanyLogo()
                    If logoImg IsNot Nothing Then
                        PictureBox9.Image = logoImg
                        PictureBox9.Location = New Point(81, 15)
                    End If
                Catch ex As Exception
                    Console.WriteLine($"Unable to set dashboard logo: {ex.Message}")
                End Try
                PictureBox9.BringToFront()
            End If

            Dim availableWidth As Integer = DashboardPanel.Width - 40
            Dim startY As Integer = 250
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonWidth As Integer = availableWidth - 5
            Dim buttonIndex As Integer = 0

            ' Title and subtitle
            Dim titleLabel As New Label() With {
            .Text = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC"),
            .Font = New Font("Poppins", 14, FontStyle.Bold),
            .ForeColor = Color.FromArgb(254, 191, 16),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(availableWidth, 30),
            .Location = New Point(20, 110),
            .TextAlign = ContentAlignment.MiddleCenter
        }
            DashboardPanel.Controls.Add(titleLabel)

            Dim subtitleLabel As New Label() With {
            .Text = "Dental Supply Management",
            .Font = New Font("Poppins", 10, FontStyle.Regular),
            .ForeColor = Color.FromArgb(225, 229, 233),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(availableWidth, 25),
            .Location = New Point(20, 145),
            .TextAlign = ContentAlignment.MiddleCenter
        }
            DashboardPanel.Controls.Add(subtitleLabel)

            Dim separator1 As New Panel() With {
            .BackColor = Color.FromArgb(50, 50, 50),
            .Size = New Size(availableWidth - 20, 2),
            .Location = New Point(30, 190)
        }
            DashboardPanel.Controls.Add(separator1)

            Dim navLabel As New Label() With {
            .Text = "NAVIGATION",
            .Font = New Font("Poppins", 10, FontStyle.Bold),
            .ForeColor = Color.FromArgb(225, 229, 233),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(availableWidth, 25),
            .Location = New Point(20, 205),
            .TextAlign = ContentAlignment.MiddleCenter
        }
            DashboardPanel.Controls.Add(navLabel)

            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "Staff").ToUpper()

            ' Ordered navigation:
            ' Dashboard -> POS / Sales -> Inventory (role) -> Sales Records -> Staff (role) ->
            ' Inventory Logs -> Suppliers -> Audit Logs (ACTIVE) -> System (admin)

            ' 1. Dashboard
            Dim navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
            buttonIndex += 1

            ' 2. POS / Sales
            Dim navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navPOSBtn.Click, AddressOf NavPOS_Click
            buttonIndex += 1

            ' 3. Inventory (Manager/Admin)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
                buttonIndex += 1
            End If

            ' 4. Sales Records
            Dim navSalesRecordsBtn = CreateLargeNavButton("📊 Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navSalesRecordsBtn.Click, AddressOf NavSalesRecords_Click
            buttonIndex += 1

            ' 5. Staff (Manager/Admin)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navStaffBtn.Click, AddressOf NavStaff_Click
                buttonIndex += 1
            End If

            ' 6. Inventory Logs
            Dim navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
            buttonIndex += 1

            ' 7. Suppliers (Manager/Admin)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navSuppliersBtn = CreateLargeNavButton("🏷️ Suppliers", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navSuppliersBtn.Click, AddressOf NavSuppliers_Click
                buttonIndex += 1
            End If

            ' 8. Audit Logs (ACTIVE)
            Dim navAuditLogBtn = CreateLargeNavButton("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
            ' Clicking active Audit Logs refreshes the grid
            AddHandler navAuditLogBtn.Click, Async Sub(sender As Object, ev As EventArgs)
                                                 Try
                                                     Await LoadAuditLogsAsync()
                                                 Catch
                                                 End Try
                                             End Sub
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
End Class