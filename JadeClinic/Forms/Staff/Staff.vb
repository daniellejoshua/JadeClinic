Imports System.Data.Common
Imports System.IO
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Linq
Imports SD = System.Drawing  ' <-- added alias for System.Drawing


Public Class Staff
    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    Private Const PageSize As Integer = 50
    Private _currentPage As Integer = 1
    Private _headerUserControl As HeaderUserControl

    Private Sub Staff_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.BackColor = Drawing.Color.FromArgb(248, 248, 247)
        ' Initialize QuestPDF
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)
        QuestPDF.Settings.License = LicenseType.Community
        Me.FormBorderStyle = FormBorderStyle.None
        Me.TopMost = True
        Me.WindowState = FormWindowState.Normal
        Me.Bounds = Screen.PrimaryScreen.Bounds
        Me.WindowState = FormWindowState.Maximized
        ' Initialize form
        Me.Text = "Staff Management"
        ' Prevent resizing of all columns and rows

        ' Check if user has Admin role - restrict access
        If Not IsUserAdmin() Then
            MessageBox.Show("Access denied. Only administrators can access Staff Management.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Me.Close()
            Return
        End If

        ' Hide Add Staff button for Manager role (only allow for Admin)
        Dim currentRole As String = If(frmLoginvb.LoggedInRole, "").ToUpper()
        If currentRole = "MANAGER" Then
            btnDiscount.Visible = False
        Else
            btnDiscount.Visible = True
        End If

        ' Create navigation menu (hardcoded from Dashboard)
        CreateNavigationMenu()

        ' Validate user session
        If Not ValidateUserSession() Then
            Return
        End If

        ' Initialize profile section
        InitializeProfileSection()

        ' Initialize DataGridView
        InitializeDataGridView()
        Guna2DataGridView1.AllowUserToResizeColumns = False
        Guna2DataGridView1.AllowUserToResizeRows = False
        Guna2DataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
        Guna2DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        ' Load users data
        LoadUsersData()

        ' Initialize Sort ComboBox
        InitializeSortComboBox()

        ' Wire pagination
        If PaginationControl1 IsNot Nothing Then
            RemoveHandler PaginationControl1.PageChanged, AddressOf PaginationControl1_PageChanged
            AddHandler PaginationControl1.PageChanged, AddressOf PaginationControl1_PageChanged
        End If

        ' Update form title to show logged-in user
        Me.Text = $"Staff Management - {frmLoginvb.LoggedInUsername}"

        ' Start idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)
    End Sub


    ' Profile managed by ProfileManager

    Private Sub InitializeProfileSection()
        _headerUserControl = New HeaderUserControl()
        Me.Controls.Add(_headerUserControl)
        _headerUserControl.BringToFront()
        _headerUserControl.Initialize(Me, AddressOf NavigateToProfileSettings)
    End Sub

    ' Helper method to validate user session
    Private Function ValidateUserSession() As Boolean
        If String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            MessageBox.Show("User session expired. Please log in again.", "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            frmLoginvb.Show()
            Me.Hide()
            Return False
        End If
        Return True
    End Function

    ' Helper method to check if current user is Admin
    Private Function IsUserAdmin() As Boolean
        Try
            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "").ToUpper()
            ' Allow both Manager and Admin to access Staff Management page
            Return currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Or currentRole = "MANAGER"
        Catch ex As Exception
            Console.WriteLine($"Error checking admin role: {ex.Message}")
            Return False
        End Try
    End Function

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
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Staff Management.")
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

    Private Sub InitializeSortComboBox()
        SortBy.Items.Clear()
        SortBy.Items.Add("All Users")
        SortBy.Items.Add("Admins Only")
        SortBy.Items.Add("Managers Only")
        SortBy.Items.Add("Staff Only")
        SortBy.SelectedIndex = 0
    End Sub

    ' CLEANED VERSION - Removed all QR code references
    ' Separate method for DataGridView avatars (different size)
    Private Sub InitializeDataGridView()
        ' Clear existing columns
        Guna2DataGridView1.Columns.Clear()
        ' Center all cell text
        Guna2DataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        ' Light themed DataGridView
        Guna2DataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(250, 249, 246)
        Guna2DataGridView1.GridColor = System.Drawing.Color.FromArgb(220, 220, 220)
        Guna2DataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        Guna2DataGridView1.EnableHeadersVisualStyles = False

        Guna2DataGridView1.DefaultCellStyle = New DataGridViewCellStyle() With {
        .BackColor = System.Drawing.Color.White,
        .ForeColor = System.Drawing.Color.FromArgb(51, 51, 51),
        .SelectionBackColor = System.Drawing.Color.FromArgb(235, 228, 200),
        .SelectionForeColor = System.Drawing.Color.FromArgb(51, 51, 51),
        .Font = New Font("Poppins", 9.0F, FontStyle.Regular),
        .Alignment = DataGridViewContentAlignment.MiddleCenter,
        .Padding = New Padding(8, 6, 8, 6)
    }

        Guna2DataGridView1.AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {
        .BackColor = System.Drawing.Color.FromArgb(250, 249, 246)
    }

        Guna2DataGridView1.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
        .BackColor = System.Drawing.Color.FromArgb(250, 249, 246),
        .ForeColor = System.Drawing.Color.FromArgb(68, 68, 68),
        .SelectionBackColor = System.Drawing.Color.FromArgb(250, 249, 246),
        .SelectionForeColor = System.Drawing.Color.FromArgb(68, 68, 68),
        .Font = New Font("Poppins", 8.5F, FontStyle.Bold),
        .Alignment = DataGridViewContentAlignment.MiddleCenter
    }
        Guna2DataGridView1.ColumnHeadersHeight = 44
        Guna2DataGridView1.RowTemplate.Height = 72
        Guna2DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        Guna2DataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None

        Guna2DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        Guna2DataGridView1.AllowUserToAddRows = False
        Guna2DataGridView1.AllowUserToDeleteRows = False
        Guna2DataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Guna2DataGridView1.MultiSelect = False

        ' Add columns (center aligned by default, except Username/Email/Phone)
        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
        .Name = "UserID",
        .HeaderText = "ID",
        .ReadOnly = True,
        .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
    })

Dim photoCol As New DataGridViewImageColumn() With {
        .Name = "Photo",
        .HeaderText = "Photo",
        .ImageLayout = DataGridViewImageCellLayout.Zoom,
        .ReadOnly = True,
        .Width = 90,
        .AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
        .Resizable = DataGridViewTriState.False,
        .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter,
                                                               .Padding = New Padding(0)}
    }
        Guna2DataGridView1.Columns.Add(photoCol)

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
        .Name = "Username",
        .HeaderText = "Username",
        .ReadOnly = True,
        .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
    })

        ' Replace FullName column with Email and Phone columns
        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
        .Name = "Email",
        .HeaderText = "Email",
        .ReadOnly = True,
        .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
    })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
        .Name = "Phone",
        .HeaderText = "Phone",
        .ReadOnly = True,
        .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
    })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
        .Name = "UserRole",
        .HeaderText = "Role",
        .ReadOnly = True,
        .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
    })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
        .Name = "IsActive",
        .HeaderText = "Status",
        .ReadOnly = True,
        .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
    })

        Dim actionsCol As New DataGridViewTextBoxColumn() With {
        .Name = "Actions",
        .HeaderText = "Actions",
        .ReadOnly = True,
        .DefaultCellStyle = New DataGridViewCellStyle() With {
            .Alignment = DataGridViewContentAlignment.MiddleCenter,
            .Font = New Font("Segoe UI Emoji", 12, FontStyle.Regular),
            .ForeColor = System.Drawing.Color.FromArgb(51, 51, 51)
        }
    }
        Guna2DataGridView1.Columns.Add(actionsCol)
    End Sub

    Private Sub LoadUsersData(Optional sortOrder As String = "", Optional pageNumber As Integer = -1)
        Try
            If pageNumber >= 1 Then
                _currentPage = pageNumber
            End If

            ' Clear existing rows
            Guna2DataGridView1.Rows.Clear()

            ' Hide any existing "No records" message
            DataGridViewHelper.HideNoRecordsMessage()

            Dim whereClause As String = ""
            Select Case sortOrder
                Case "Admins Only"
                    whereClause = " WHERE UserRole = 'Admin'"
                Case "Managers Only"
                    whereClause = " WHERE UserRole = 'Manager'"
                Case "Staff Only"
                    whereClause = " WHERE UserRole = 'Staff'"
            End Select

            ' Configure pagination with the filtered total count.
            If PaginationControl1 IsNot Nothing Then
                Dim totalCount As Integer = CountUsers(sortOrder)
                PaginationControl1.Configure(totalCount, PageSize, _currentPage)
            End If

            Dim offset As Integer = (_currentPage - 1) * PageSize
            Dim query As String = "SELECT UserID, Username, PIN, Email, Phone, UserRole, PhotoPath, IsActive, EmployeeCode FROM Users" &
                                  whereClause &
                                  " ORDER BY UserID ASC" &
                                  $" LIMIT {PageSize} OFFSET {offset}"

            Using reader As DbDataReader = Utilities.ExecuteReader(query, Nothing)
                While reader.Read()
                    Dim userId As Integer = Convert.ToInt32(reader("UserID"))
                    Dim username As String = reader("Username").ToString()
                    Dim pin As String = If(IsDBNull(reader("PIN")), "", reader("PIN").ToString())
                    Dim email As String = If(IsDBNull(reader("Email")), "", reader("Email").ToString())
                    Dim phone As String = If(IsDBNull(reader("Phone")), "", reader("Phone").ToString())
                    Dim userRole As String = If(IsDBNull(reader("UserRole")), "Staff", reader("UserRole").ToString())
                    Dim isActive As Boolean = If(IsDBNull(reader("IsActive")), True, Convert.ToBoolean(reader("IsActive")))
                    Dim employeeCode As String = If(IsDBNull(reader("EmployeeCode")), "", reader("EmployeeCode").ToString())

                    Dim userPhoto As System.Drawing.Image = Nothing
                    Dim photoFileName As String = If(Not IsDBNull(reader("PhotoPath")), reader("PhotoPath").ToString(), Nothing)
                    If Not String.IsNullOrEmpty(photoFileName) Then
                        Dim fullPath As String = System.IO.Path.Combine(Connection.GetImagesFolder("users"), photoFileName)
                        If System.IO.File.Exists(fullPath) Then
                            Using original As System.Drawing.Image = System.Drawing.Image.FromFile(fullPath)
                                userPhoto = ResizeForCell(original)
                            End Using
                        End If
                    End If
                    If userPhoto Is Nothing Then
                        userPhoto = CreateDefaultAvatarImage()
                    End If

                    ' Add row to DataGridView
                    Dim rowIndex As Integer = Guna2DataGridView1.Rows.Add()

                    ' Set individual column values (Email and Phone replace FullName)
                    Guna2DataGridView1.Rows(rowIndex).Cells("UserID").Value = employeeCode
                    Guna2DataGridView1.Rows(rowIndex).Cells("Photo").Value = userPhoto
                    Guna2DataGridView1.Rows(rowIndex).Cells("Username").Value = username
                    Guna2DataGridView1.Rows(rowIndex).Cells("Email").Value = email
                    Guna2DataGridView1.Rows(rowIndex).Cells("Phone").Value = phone
                    Guna2DataGridView1.Rows(rowIndex).Cells("UserRole").Value = userRole
                    Guna2DataGridView1.Rows(rowIndex).Cells("IsActive").Value = If(isActive, ChrW(&H2713) & " Active", ChrW(&H2718) & " Inactive")
                    Guna2DataGridView1.Rows(rowIndex).Cells("Actions").Value = "👁️      |    ✏️  "
                    Dim statusCell = Guna2DataGridView1.Rows(rowIndex).Cells("IsActive")
                    statusCell.Value = If(isActive, ChrW(&H2713) & " Active", ChrW(&H2718) & " Inactive")
                    statusCell.Style.ForeColor = If(isActive, SD.Color.FromArgb(16, 216, 98), SD.Color.FromArgb(255, 71, 87))
                    statusCell.Style.Font = New Font(Guna2DataGridView1.Font, FontStyle.Bold)

                    ' Store actual data in row tag for editing purposes (include Email and Phone)
                    Guna2DataGridView1.Rows(rowIndex).Tag = New Dictionary(Of String, Object) From {
                    {"UserID", userId},
                    {"Username", username},
                    {"PIN", pin},
                    {"Email", email},
                    {"Phone", phone},
                    {"UserRole", userRole},
                    {"PhotoPath", photoFileName},
                    {"IsActive", isActive},
                    {"EmployeeCode", employeeCode}
                }
                End While
            End Using

            ' Show "No staff found" message if empty
            If Guna2DataGridView1.Rows.Count = 0 Then
                DataGridViewHelper.ShowNoRecordsMessage(Guna2DataGridView1, "No Staff Members Found")
            End If

            AlignPaginationToPanel()

        Catch ex As Exception
            MessageBox.Show($"Error loading staff data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

            ' Log the error
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Staff Data Load Failed", $"Error: {ex.Message}")
            End If
        End Try
    End Sub

    Private Function CountUsers(Optional sortOrder As String = "") As Integer
        Dim count As Integer = 0
        Try
            Dim whereClause As String = ""
            Select Case sortOrder
                Case "Admins Only"
                    whereClause = " WHERE UserRole = 'Admin'"
                Case "Managers Only"
                    whereClause = " WHERE UserRole = 'Manager'"
                Case "Staff Only"
                    whereClause = " WHERE UserRole = 'Staff'"
            End Select

            Dim result = Utilities.ExecuteScalar("SELECT COUNT(*) FROM Users" & whereClause, Nothing)
            If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                count = Convert.ToInt32(result)
            End If
        Catch
        End Try
        Return count
    End Function
    Private Function CreateDefaultAvatarImage() As System.Drawing.Image
        Const size As Integer = 60
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic

            ' Opaque background so transparent corners don't render black in the grid cell
            g.Clear(System.Drawing.Color.White)

            ' Draw the shared default avatar resource centered, preserving aspect ratio
            Dim src As System.Drawing.Image = My.Resources.avatar_default_svgrepo_com
            Dim scale As Single = Math.Min(size / src.Width, size / src.Height)
            Dim dw As Integer = CInt(src.Width * scale)
            Dim dh As Integer = CInt(src.Height * scale)
            g.DrawImage(src, (size - dw) \ 2, (size - dh) \ 2, dw, dh)
        End Using
        Return bmp
    End Function

    Private Function ResizeForCell(img As SD.Image) As SD.Image
        Const size As Integer = 60
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
            g.Clear(System.Drawing.Color.White)
            Dim scale As Single = Math.Min(size / img.Width, size / img.Height)
            Dim dw As Integer = CInt(img.Width * scale)
            Dim dh As Integer = CInt(img.Height * scale)
            g.DrawImage(img, (size - dw) \ 2, (size - dh) \ 2, dw, dh)
        End Using
        Return bmp
    End Function

    Private Sub SortBy_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SortBy.SelectedIndexChanged
        If SortBy.SelectedItem IsNot Nothing Then
            _currentPage = 1
            LoadUsersData(SortBy.SelectedItem.ToString())
        End If
    End Sub

    Private Sub Guna2DataGridView1_CellMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles Guna2DataGridView1.CellMouseClick
        ' Handle clicks on the Actions column, detect left/right half
        If e.RowIndex >= 0 Then
            Dim colName As String = Guna2DataGridView1.Columns(e.ColumnIndex).Name
            If colName = "Actions" Then
                Dim userData As Dictionary(Of String, Object) = CType(Guna2DataGridView1.Rows(e.RowIndex).Tag, Dictionary(Of String, Object))
                Dim cellBounds As Rectangle = Guna2DataGridView1.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, False)
                Dim isLeftHalf As Boolean = e.X < cellBounds.Width \ 2

                ' Add click effect - briefly change cell background
                Guna2DataGridView1.Rows(e.RowIndex).Cells(e.ColumnIndex).Style.BackColor = System.Drawing.Color.FromArgb(255, 204, 77)
                Dim clickTimer As New Timer()
                clickTimer.Interval = 150
                AddHandler clickTimer.Tick, Sub()
                                                Guna2DataGridView1.Rows(e.RowIndex).Cells(e.ColumnIndex).Style.BackColor = System.Drawing.Color.White
                                                clickTimer.Stop()
                                            End Sub
                clickTimer.Start()

                If isLeftHalf Then
                    ViewUser(userData)
                Else
                    EditUser(userData)
                End If
            End If
        End If
    End Sub

    ' Add hover effects for DataGridView
    Private Sub Guna2DataGridView1_CellMouseEnter(sender As Object, e As DataGridViewCellEventArgs) Handles Guna2DataGridView1.CellMouseEnter
        If e.RowIndex >= 0 Then
            Dim colName As String = Guna2DataGridView1.Columns(e.ColumnIndex).Name
            If colName = "Actions" Then
                Guna2DataGridView1.Rows(e.RowIndex).Cells(e.ColumnIndex).Style.BackColor = System.Drawing.Color.FromArgb(237, 237, 237)
                Guna2DataGridView1.Cursor = Cursors.Hand
            End If
        End If
    End Sub

    Private Sub Guna2DataGridView1_CellMouseLeave(sender As Object, e As DataGridViewCellEventArgs) Handles Guna2DataGridView1.CellMouseLeave
        If e.RowIndex >= 0 Then
            Dim colName As String = Guna2DataGridView1.Columns(e.ColumnIndex).Name
            If colName = "Actions" Then
                Guna2DataGridView1.Rows(e.RowIndex).Cells(e.ColumnIndex).Style.BackColor = System.Drawing.Color.White
                Guna2DataGridView1.Cursor = Cursors.Default
            End If
        End If
    End Sub

    ' Replace the existing ViewUser method with this one to open the IdCard modal.
    Private Sub ViewUser(userData As Dictionary(Of String, Object))
        Try
            If userData Is Nothing Then
                Return
            End If

            ' Ensure we have a UserID key
            If Not userData.ContainsKey("UserID") AndAlso userData.ContainsKey("ID") Then
                userData("UserID") = userData("ID")
            End If

            Dim needsQr As Boolean = Not userData.ContainsKey("QRCode") OrElse String.IsNullOrWhiteSpace(If(userData("QRCode"), String.Empty).ToString())
            Dim needsEmail As Boolean = Not userData.ContainsKey("Email") OrElse String.IsNullOrWhiteSpace(If(userData("Email"), String.Empty).ToString())
            Dim needsPhone As Boolean = Not userData.ContainsKey("Phone") OrElse String.IsNullOrWhiteSpace(If(userData("Phone"), String.Empty).ToString())

            If (needsQr Or needsEmail Or needsPhone) AndAlso userData.ContainsKey("UserID") Then
                Try
                    Dim userId As Integer = Convert.ToInt32(userData("UserID"))
                    Using reader As DbDataReader = Utilities.ExecuteReader(
                    "SELECT QRCode, Email, Phone FROM Users WHERE UserID = @UserID",
                    New SqlParameter("@UserID", userId))
                        If reader.Read() Then
                            If needsQr AndAlso Not IsDBNull(reader("QRCode")) Then
                                userData("QRCode") = reader("QRCode").ToString()
                            End If
                            If needsEmail AndAlso Not IsDBNull(reader("Email")) Then
                                userData("Email") = reader("Email").ToString()
                            End If
                            If needsPhone AndAlso Not IsDBNull(reader("Phone")) Then
                                userData("Phone") = reader("Phone").ToString()
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Console.WriteLine($"Error fetching QRCode/Email/Phone from DB: {ex.Message}")
                End Try
            End If

            ' Open IdCard form and populate it
            Dim idForm As New IdCard()
            idForm.StartPosition = FormStartPosition.CenterParent
            idForm.LoadFromUserData(userData)
            Utilities.EnableEscCloseModal(idForm)
            idForm.ShowDialog(Me)
        Catch ex As Exception
            MessageBox.Show($"Unable to show ID Card: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    ' PIN confirmation dialog for editing staff
    Private Function ShowPinDialog(promptText As String) As Boolean
        Dim pinDialog As New Form()
        pinDialog.Text = "PIN Confirmation"
        pinDialog.Size = New System.Drawing.Size(320, 180)
        pinDialog.StartPosition = FormStartPosition.CenterParent
        pinDialog.BackColor = System.Drawing.Color.White
        pinDialog.FormBorderStyle = FormBorderStyle.FixedDialog
        pinDialog.MaximizeBox = False
        pinDialog.MinimizeBox = False

        Dim lblPrompt As New Label()
        lblPrompt.Text = promptText
        lblPrompt.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51)
        lblPrompt.Font = New Font("Poppins", 10)
        lblPrompt.AutoSize = True
        lblPrompt.Location = New System.Drawing.Point(20, 20)

        Dim txtPin As New TextBox()
        txtPin.PasswordChar = "?"c
        txtPin.MaxLength = 4
        txtPin.Location = New System.Drawing.Point(20, 50)
        txtPin.Size = New System.Drawing.Size(260, 30)
        txtPin.BackColor = System.Drawing.Color.FromArgb(237, 237, 237)
        txtPin.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51)
        txtPin.Font = New Font("Poppins", 12)

        Dim btnConfirm As New Button()
        btnConfirm.Text = "Confirm"
        btnConfirm.Location = New System.Drawing.Point(20, 90)
        btnConfirm.Size = New System.Drawing.Size(100, 32)
        btnConfirm.BackColor = System.Drawing.Color.FromArgb(255, 204, 77)
        btnConfirm.ForeColor = System.Drawing.Color.Black
        btnConfirm.Font = New Font("Poppins", 10)
        btnConfirm.FlatStyle = FlatStyle.Flat

        Dim btnCancel As New Button()
        btnCancel.Text = "Cancel"
        btnCancel.Location = New System.Drawing.Point(140, 90)
        btnCancel.Size = New System.Drawing.Size(100, 32)
        btnCancel.BackColor = System.Drawing.Color.FromArgb(200, 200, 200)
        btnCancel.ForeColor = System.Drawing.Color.FromArgb(51, 51, 51)
        btnCancel.Font = New Font("Poppins", 10)
        btnCancel.FlatStyle = FlatStyle.Flat

        Dim pinAccepted As Boolean = False
        AddHandler btnConfirm.Click, Sub()
                                         If txtPin.Text = frmLoginvb.LoggedInPIN Then
                                             pinAccepted = True
                                             pinDialog.Close()
                                         Else
                                             MessageBox.Show("Invalid PIN.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                             txtPin.Clear()
                                             txtPin.Focus()
                                         End If
                                     End Sub
        AddHandler btnCancel.Click, Sub()
                                        pinDialog.Close()
                                    End Sub
        AddHandler txtPin.KeyDown, Sub(s, eArgs)
                                       If eArgs.KeyCode = Keys.Enter Then
                                           btnConfirm.PerformClick()
                                       End If
                                   End Sub

        pinDialog.Controls.AddRange({lblPrompt, txtPin, btnConfirm, btnCancel})
        txtPin.Focus()
        Utilities.EnableEscCloseModal(pinDialog)
        pinDialog.ShowDialog(Me)

        Return pinAccepted
    End Function

    Private Sub EditUser(userData As Dictionary(Of String, Object))
        Try
            If userData Is Nothing Then
                Return
            End If

            ' Check if current user is Manager - restrict editing for Managers
            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "").ToUpper()
            If currentRole = "MANAGER" Then
                MessageBox.Show("Access denied. Managers can only view staff information, not edit.", "Edit Restricted", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Resolve UserID safely
            Dim targetUserId As Integer = 0
            If userData.ContainsKey("UserID") Then
                Integer.TryParse(userData("UserID").ToString(), targetUserId)
            End If

            ' If user tried to open their own account, show info and do NOT open AddStaff
            If targetUserId = frmLoginvb.LoggedInUserID Then
                MessageBox.Show("You cannot edit your own account here. Please use Profile Settings to update your account.", "Action Restricted", MessageBoxButtons.OK, MessageBoxIcon.Information)

                ' Offer to go to Profile Settings
                If MessageBox.Show("Open Profile Settings now?", "Open Profile", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                    NavigateToProfileSettings()
                End If

                Return
            End If



            ' Create and show AddStaff in edit mode for other users
            Using addStaffForm As New AddStaff()
                If Not addStaffForm.SetEditMode(userData) Then
                    ' SetEditMode failed or refused (defensive)
                    Return
                End If

                addStaffForm.StartPosition = FormStartPosition.CenterParent
                Utilities.EnableEscCloseModal(addStaffForm)
                addStaffForm.ShowDialog(Me)
            End Using

            ' Refresh the staff list after possible edits
            _currentPage = 1
            LoadUsersData(If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), ""))
        Catch ex As Exception
            MessageBox.Show($"Error opening Edit Staff form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub


    Private Sub btnDiscount_Click(sender As Object, e As EventArgs) Handles btnDiscount.Click
        ' Open AddStaff form for adding new staff members
        Try
            Dim addStaffForm As New AddStaff()
            Utilities.EnableEscCloseModal(addStaffForm)
            addStaffForm.ShowDialog()

            ' Refresh the staff list after adding new staff
            _currentPage = 1
            LoadUsersData(If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), ""))

        Catch ex As Exception
            MessageBox.Show($"Error opening Add Staff form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Staff_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)

        ' If this is programmatic navigation, don't show confirmation
        If isNavigating Then
            Return
        End If

        ' Prevent multiple confirmations by checking the close reason
        If e.CloseReason = CloseReason.ApplicationExitCall Then
            ' If Application.Exit() was already called, don't show confirmation again
            Return
        End If

        ' Show confirmation only for user-initiated close (X button)
        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = EscForm.ConfirmExit(Me)

            If result = DialogResult.Yes Then
                ' Log the exit action
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Staff Management form")
                End If

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

    ' Export button functionality for Staff Management
    Private Sub Exportbtn_Click(sender As Object, e As EventArgs) Handles Exportbtn.Click
        Try
            ' Get current sort order
            Dim sortOrder As String = If(SortBy?.SelectedItem?.ToString(), "")

            ' Call StaffExporter with current sort order
            StaffExporter.ExportStaffReport(sortOrder)

        Catch ex As Exception
            MessageBox.Show($"Export failed: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

            ' Log export failure
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Staff Export Failed", $"Error: {ex.Message}")
            End If
        End Try
    End Sub

    Private Sub CreateNavigationMenu()
        NavigationBuilder.Build(DashboardPanel, Me, "Staff")
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
        btn.ForeColor = If(isActive, System.Drawing.Color.FromArgb(26, 29, 31), System.Drawing.Color.FromArgb(51, 51, 51))
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(200, 200, 200))
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = System.Drawing.Color.FromArgb(200, 200, 200)
        btn.ShadowDecoration.Depth = 4

        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.FromArgb(237, 237, 237)
                                           btn.BorderColor = System.Drawing.Color.FromArgb(254, 191, 16)
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
    Private Sub NavigateToProfileSettings()
        ' Navigate to ProfileSettings form (preserve audit and dropdown state).
        Try
            ' Prevent the form-closing confirmation and hide the dropdown first
            isNavigating = True
            ProfileManager.HideProfileDropdown(Me)

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
    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)
        isNavigating = True
        SalesRecord.Show()
        Me.Close()
    End Sub

    ' Helper: fetch primary image bytes for a product
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
    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        AuditLog.Show()
        Me.Close()
    End Sub

    ' Show ID Card form for given user data. If Photo/QRCode missing, fetch from DB using UserID
    Private Sub ShowIdCardForRow(rowTag As Dictionary(Of String, Object))
        Try
            If rowTag Is Nothing Then Return

            ' Ensure we have UserID if possible
            If Not rowTag.ContainsKey("UserID") AndAlso rowTag.ContainsKey("ID") Then
                rowTag("UserID") = rowTag("ID")
            End If

            If Not rowTag.ContainsKey("QRCode") OrElse String.IsNullOrWhiteSpace(If(rowTag("QRCode"), String.Empty).ToString()) Then
                If rowTag.ContainsKey("UserID") Then
                    Try
                        Dim userId As Integer = Convert.ToInt32(rowTag("UserID"))
                        Using reader As DbDataReader = Utilities.ExecuteReader("SELECT QRCode FROM Users WHERE UserID = @UserID", New SqlParameter("@UserID", userId))
                            If reader.Read() Then
                                If Not IsDBNull(reader("QRCode")) Then
                                    rowTag("QRCode") = reader("QRCode").ToString()
                                End If
                            End If
                        End Using
                    Catch ex As Exception
                        Console.WriteLine($"Error fetching QRCode from DB: {ex.Message}")
                    End Try
                End If
            End If

            ' Show IdCard form
            Dim idForm As New IdCard()
            idForm.StartPosition = FormStartPosition.CenterParent
            idForm.LoadFromUserData(rowTag)
            Utilities.EnableEscCloseModal(idForm)
            idForm.ShowDialog(Me)

        Catch ex As Exception
            MessageBox.Show($"Unable to display ID Card: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub NavLogout_Click(sender As Object, e As EventArgs)
        ' Confirm logout
        Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            ' Clear user session
            frmLoginvb.LogoutUser()

            ' Navigate to login
            isNavigating = True
            Me.Close()
            Dim loginForm As New frmLoginvb()
            loginForm.Show()
        End If
    End Sub



    Private Sub PictureBox9_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub PaginationControl1_PageChanged(page As Integer)
        _currentPage = page
        LoadUsersData(If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), ""), page)
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

    Private Sub Staff_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        AlignPaginationToPanel()
    End Sub
End Class