Imports Microsoft.Data.SqlClient
Imports System.IO
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure
Imports System.Drawing
Imports System.Drawing.Imaging

Public Class Staff
    ' Navigation flag to prevent exit confirmation on programmatic close
    Private isNavigating As Boolean = False

    Private Sub Staff_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize QuestPDF
        QuestPDF.Settings.License = LicenseType.Community
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.MinimumSize = Me.Size
        Me.MaximumSize = Me.Size
        ' Initialize form
        Me.Text = "Staff Management"
        ' Prevent resizing of all columns and rows

        ' Check if user has Admin role - restrict access
        If Not IsUserAdmin() Then
            MessageBox.Show("Access denied. Only administrators can access Staff Management.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Me.Close()
            Return
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

        ' Load logged-in user's profile picture
        LoadUserProfilePicture()

        ' Update form title to show logged-in user
        Me.Text = $"Staff Management - {frmLoginvb.LoggedInUsername}"

        ' Start idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)
    End Sub

    ' Profile dropdown panel
    Private profileDropdownPanel As Panel = Nothing
    Private isProfileDropdownVisible As Boolean = False

    Private Sub InitializeProfileSection()
        Try
            ' Set username without emoji
            lblUsername.Text = frmLoginvb.LoggedInUsername
            lblUsername.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
            lblUsername.ForeColor = System.Drawing.Color.White

            ' Load user profile picture
            LoadUserProfilePicture()

            ' Add click event to profile picture
            AddHandler Guna2CirclePictureBox5.Click, AddressOf ProfilePicture_Click
            AddHandler lblUsername.Click, AddressOf ProfilePicture_Click

            ' Add hover effects
            AddHandler Guna2CirclePictureBox5.MouseEnter, Sub()
                                                              Guna2CirclePictureBox5.Cursor = Cursors.Hand
                                                          End Sub
            AddHandler lblUsername.MouseEnter, Sub()
                                                   lblUsername.Cursor = Cursors.Hand
                                               End Sub

        Catch ex As Exception
            ' Fallback if there's an error
            lblUsername.Text = frmLoginvb.LoggedInUsername
            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
        End Try
    End Sub

    Private Sub LoadUserProfilePicture()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                ' Query to get the logged-in user's photo
                Dim query As String = "SELECT Photo FROM Users WHERE Username = @Username"
                Dim parameters As SqlParameter() = {
                    New SqlParameter("@Username", frmLoginvb.LoggedInUsername)
                }

                Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                    If reader.Read() Then
                        ' Configure the PictureBox for circular profile picture
                        Guna2CirclePictureBox5.SizeMode = PictureBoxSizeMode.Zoom
                        Guna2CirclePictureBox5.BorderStyle = BorderStyle.None

                        If Not IsDBNull(reader("Photo")) Then
                            ' Load user's actual photo
                            Dim photoBytes As Byte() = CType(reader("Photo"), Byte())
                            Using ms As New IO.MemoryStream(photoBytes)
                                Guna2CirclePictureBox5.Image = System.Drawing.Image.FromStream(ms)
                            End Using
                        Else
                            ' Create and display default avatar
                            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(frmLoginvb.LoggedInUsername)
                        End If
                    End If
                End Using
            End If
        Catch ex As Exception
            ' If there's an error, show default avatar
            Guna2CirclePictureBox5.Image = CreateDefaultProfileAvatar(If(frmLoginvb.LoggedInUsername, "User"))
        End Try
    End Sub

    ' Single CreateDefaultProfileAvatar method for profile picture
    Private Function CreateDefaultProfileAvatar(username As String) As System.Drawing.Image
        Dim bitmap As New Bitmap(50, 50)
        Using g As Graphics = Graphics.FromImage(bitmap)
            ' Enable anti-aliasing for smooth circles
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            ' Fill background with a color based on username
            Dim colors() As System.Drawing.Color = {
                System.Drawing.Color.FromArgb(255, 107, 107),
                System.Drawing.Color.FromArgb(78, 205, 196),
                System.Drawing.Color.FromArgb(85, 98, 112),
                System.Drawing.Color.FromArgb(129, 236, 236),
                System.Drawing.Color.FromArgb(116, 185, 255)
            }
            Dim colorIndex As Integer = Math.Abs(username.GetHashCode()) Mod colors.Length
            g.FillEllipse(New SolidBrush(colors(colorIndex)), 0, 0, 50, 50)

            ' Draw initials
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

        ' Create dropdown panel
        profileDropdownPanel = New Panel()
        profileDropdownPanel.Size = New System.Drawing.Size(200, 100)
        profileDropdownPanel.BackColor = System.Drawing.Color.FromArgb(41, 44, 45)
        profileDropdownPanel.BorderStyle = BorderStyle.FixedSingle

        ' Position below the profile picture
        Dim profileLocation = Guna2CirclePictureBox5.Location
        profileDropdownPanel.Location = New Point(profileLocation.X - 90, profileLocation.Y + Guna2CirclePictureBox5.Height + 5)

        ' Create Profile Settings button
        Dim btnProfileSettings As New Label()
        btnProfileSettings.Text = "⚙️ Profile Settings"
        btnProfileSettings.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnProfileSettings.ForeColor = System.Drawing.Color.White
        btnProfileSettings.BackColor = System.Drawing.Color.Transparent
        btnProfileSettings.Size = New System.Drawing.Size(190, 40)
        btnProfileSettings.Location = New Point(5, 5)
        btnProfileSettings.TextAlign = ContentAlignment.MiddleLeft
        btnProfileSettings.Cursor = Cursors.Hand

        ' Add hover effect to Profile Settings
        AddHandler btnProfileSettings.MouseEnter, Sub()
                                                      btnProfileSettings.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
                                                  End Sub
        AddHandler btnProfileSettings.MouseLeave, Sub()
                                                      btnProfileSettings.BackColor = System.Drawing.Color.Transparent
                                                  End Sub

        ' Add click event to Profile Settings
        AddHandler btnProfileSettings.Click, Sub()
                                                 HideProfileDropdown()
                                                 NavigateToProfileSettings()
                                             End Sub

        ' Create Log Out button
        Dim btnLogOut As New Label()
        btnLogOut.Text = "🚪 Log Out"
        btnLogOut.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnLogOut.ForeColor = System.Drawing.Color.White
        btnLogOut.BackColor = System.Drawing.Color.Transparent
        btnLogOut.Size = New System.Drawing.Size(190, 40)
        btnLogOut.Location = New Point(5, 50)
        btnLogOut.TextAlign = ContentAlignment.MiddleLeft
        btnLogOut.Cursor = Cursors.Hand

        ' Add hover effect to Log Out
        AddHandler btnLogOut.MouseEnter, Sub()
                                             btnLogOut.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
                                         End Sub
        AddHandler btnLogOut.MouseLeave, Sub()
                                             btnLogOut.BackColor = System.Drawing.Color.Transparent
                                         End Sub

        ' Add click event to Log Out
        AddHandler btnLogOut.Click, Sub()
                                        ' Log the logout action
                                        If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                                            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Log Out", "User logged out of the application.")
                                        End If

                                        ' Close the application and the main form (frmLoginvb)
                                        frmLoginvb.Close()
                                        Application.Exit()
                                    End Sub

        ' Add buttons to panel
        profileDropdownPanel.Controls.Add(btnProfileSettings)
        profileDropdownPanel.Controls.Add(btnLogOut)

        ' Add panel to form
        Me.Controls.Add(profileDropdownPanel)
        profileDropdownPanel.BringToFront()

        ' Add click event to form to hide dropdown when clicked elsewhere
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

        ' Remove form click event
        RemoveHandler Me.Click, AddressOf Form_Click
    End Sub

    Private Sub Form_Click(sender As Object, e As EventArgs)
        ' Hide dropdown when clicking elsewhere on the form
        HideProfileDropdown()
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
            Return currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR"
        Catch ex As Exception
            Console.WriteLine($"Error checking admin role: {ex.Message}")
            Return False
        End Try
    End Function

    Private Sub InitializeDataGridView()
        ' Clear existing columns
        Guna2DataGridView1.Columns.Clear()

        ' Configure DataGridView appearance with consistent gray colors and white row separators
        Guna2DataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(41, 44, 45)
        Guna2DataGridView1.GridColor = System.Drawing.Color.White ' Thin white line as row separator
        Guna2DataGridView1.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66) ' Consistent gray for all rows
        Guna2DataGridView1.AlternatingRowsDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66) ' Match odd and even rows
        Guna2DataGridView1.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
        Guna2DataGridView1.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 204, 77)
        Guna2DataGridView1.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
        Guna2DataGridView1.DefaultCellStyle.Font = New System.Drawing.Font("Poppins", 9.0F, System.Drawing.FontStyle.Regular)
        Guna2DataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        ' Configure header style with gray colors and remove blue selection color
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(30, 30, 30)
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(30, 30, 30) ' Match header background to remove blue color
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle.Font = New System.Drawing.Font("Poppins SemiBold", 10.0F, System.Drawing.FontStyle.Regular)
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Guna2DataGridView1.ColumnHeadersHeight = 50
        Guna2DataGridView1.RowTemplate.Height = 60

        ' Ensure row borders are visible
        Guna2DataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal

        ' Add User ID column FIRST
        Dim colUserID As New DataGridViewTextBoxColumn()
        colUserID.Name = "UserID"
        colUserID.HeaderText = "ID"
        colUserID.Width = 80
        colUserID.ReadOnly = True
        colUserID.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Guna2DataGridView1.Columns.Add(colUserID)

        ' Add Photo column for user photos
        Dim colPhoto As New DataGridViewImageColumn()
        colPhoto.Name = "Photo"
        colPhoto.HeaderText = "Photo"
        colPhoto.Width = 70
        colPhoto.ReadOnly = True
        colPhoto.ImageLayout = DataGridViewImageCellLayout.Zoom
        colPhoto.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        colPhoto.DefaultCellStyle.Padding = New Padding(5, 5, 5, 5)
        Guna2DataGridView1.Columns.Add(colPhoto)

        ' Add Username column
        Dim colUsername As New DataGridViewTextBoxColumn()
        colUsername.Name = "Username"
        colUsername.HeaderText = "Username"
        colUsername.Width = 180
        colUsername.ReadOnly = True
        colUsername.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        colUsername.DefaultCellStyle.Padding = New Padding(10, 0, 10, 0)
        Guna2DataGridView1.Columns.Add(colUsername)

        ' Add Full Name column (replacing Email for now)
        Dim colFullName As New DataGridViewTextBoxColumn()
        colFullName.Name = "FullName"
        colFullName.HeaderText = "Full Name"
        colFullName.Width = 220
        colFullName.ReadOnly = True
        colFullName.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        colFullName.DefaultCellStyle.Padding = New Padding(10, 0, 10, 0)
        Guna2DataGridView1.Columns.Add(colFullName)

        ' Add Role column
        Dim colRole As New DataGridViewTextBoxColumn()
        colRole.Name = "UserRole"
        colRole.HeaderText = "Role"
        colRole.Width = 100
        colRole.ReadOnly = True
        colRole.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        colRole.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        colRole.DefaultCellStyle.ForeColor = System.Drawing.Color.LightGray
        colRole.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.FromArgb(255, 204, 77)
        colRole.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.Black
        colRole.DefaultCellStyle.Font = New System.Drawing.Font("Poppins", 9.0F, System.Drawing.FontStyle.Regular)
        Guna2DataGridView1.Columns.Add(colRole)

        ' Add Status column
        Dim colStatus As New DataGridViewTextBoxColumn()
        colStatus.Name = "IsActive"
        colStatus.HeaderText = "Status"
        colStatus.Width = 100
        colStatus.ReadOnly = True
        colStatus.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        Guna2DataGridView1.Columns.Add(colStatus)

        ' Actions column with separated icons and borders
        Dim colActions As New DataGridViewTextBoxColumn()
        colActions.Name = "Actions"
        colActions.HeaderText = "Actions"
        colActions.Width = 200
        colActions.ReadOnly = True
        colActions.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        colActions.DefaultCellStyle.Font = New System.Drawing.Font("Segoe UI Emoji", 11.0F, System.Drawing.FontStyle.Regular)
        colActions.DefaultCellStyle.Padding = New Padding(10, 0, 10, 0)
        Guna2DataGridView1.Columns.Add(colActions)

        ' Configure DataGridView properties
        Guna2DataGridView1.AllowUserToAddRows = False
        Guna2DataGridView1.AllowUserToDeleteRows = False
        Guna2DataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Guna2DataGridView1.MultiSelect = False
        Guna2DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
    End Sub

    Private Sub InitializeSortComboBox()
        SortBy.Items.Clear()
        SortBy.Items.Add("All Users")
        SortBy.Items.Add("Admins Only")
        SortBy.Items.Add("Managers Only")
        SortBy.Items.Add("Staff Only")
        SortBy.Items.Add("Sort by Username (A-Z)")
        SortBy.Items.Add("Sort by Username (Z-A)")
        SortBy.Items.Add("Sort by User ID (Ascending)")
        SortBy.Items.Add("Sort by User ID (Descending)")
        SortBy.SelectedIndex = 0
    End Sub

    ' CLEANED VERSION - Removed all QR code references
    Private Sub LoadUsersData(Optional sortOrder As String = "")
        Try
            ' Clear existing rows
            Guna2DataGridView1.Rows.Clear()

            ' Build query without QR code and password fields
            Dim query As String = "SELECT UserID, Username, PIN, FullName, UserRole, Photo, IsActive FROM Users"
            Dim whereClause As String = ""

            Select Case sortOrder
                Case "Admins Only"
                    whereClause = " WHERE UserRole = 'Admin'"
                Case "Managers Only"
                    whereClause = " WHERE UserRole = 'Manager'"
                Case "Staff Only"
                    whereClause = " WHERE UserRole = 'Staff'"
            End Select

            If whereClause <> "" Then
                query += whereClause
            End If

            Select Case sortOrder
                Case "Sort by Username (A-Z)"
                    query += " ORDER BY Username ASC"
                Case "Sort by Username (Z-A)"
                    query += " ORDER BY Username DESC"
                Case "Sort by User ID (Ascending)"
                    query += " ORDER BY UserID ASC"
                Case "Sort by User ID (Descending)"
                    query += " ORDER BY UserID DESC"
                Case Else
                    If Not (sortOrder = "Admins Only" Or sortOrder = "Managers Only" Or sortOrder = "Staff Only") Then
                        query += " ORDER BY UserID ASC" ' Default sorting
                    End If
            End Select

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, Nothing)
                While reader.Read()
                    Dim userId As Integer = Convert.ToInt32(reader("UserID"))
                    Dim username As String = reader("Username").ToString()
                    Dim pin As String = reader("PIN").ToString()
                    Dim fullName As String = If(IsDBNull(reader("FullName")), "", reader("FullName").ToString())
                    Dim userRole As String = If(IsDBNull(reader("UserRole")), "Staff", reader("UserRole").ToString())
                    Dim isActive As Boolean = If(IsDBNull(reader("IsActive")), True, Convert.ToBoolean(reader("IsActive")))

                    ' Handle photo
                    Dim userPhoto As System.Drawing.Image = Nothing
                    If Not IsDBNull(reader("Photo")) Then
                        Dim photoBytes As Byte() = CType(reader("Photo"), Byte())
                        Using ms As New MemoryStream(photoBytes)
                            userPhoto = System.Drawing.Image.FromStream(ms)
                        End Using
                    Else
                        ' Create default avatar with initials
                        userPhoto = CreateDefaultAvatar(username)
                    End If

                    ' Add row to DataGridView
                    Dim rowIndex As Integer = Guna2DataGridView1.Rows.Add()

                    ' Set individual column values
                    Guna2DataGridView1.Rows(rowIndex).Cells("UserID").Value = userId
                    Guna2DataGridView1.Rows(rowIndex).Cells("Photo").Value = userPhoto
                    Guna2DataGridView1.Rows(rowIndex).Cells("Username").Value = username
                    Guna2DataGridView1.Rows(rowIndex).Cells("FullName").Value = fullName
                    Guna2DataGridView1.Rows(rowIndex).Cells("UserRole").Value = userRole
                    Guna2DataGridView1.Rows(rowIndex).Cells("IsActive").Value = If(isActive, "✅ Active", "❌ Inactive")
                    Guna2DataGridView1.Rows(rowIndex).Cells("Actions").Value = "👁️  |  ✏️  |  🗑️"

                    ' Store actual data in row tag for editing purposes (REMOVED PASSWORD AND QR CODE FIELDS)
                    Guna2DataGridView1.Rows(rowIndex).Tag = New Dictionary(Of String, Object) From {
                        {"UserID", userId},
                        {"Username", username},
                        {"PIN", pin},
                        {"FullName", fullName},
                        {"UserRole", userRole},
                        {"Photo", If(Not IsDBNull(reader("Photo")), reader("Photo"), Nothing)},
                        {"IsActive", isActive}
                    }
                End While
            End Using

        Catch ex As Exception
            MessageBox.Show($"Error loading staff data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

            ' Log the error
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Staff Data Load Failed", $"Error: {ex.Message}")
            End If
        End Try
    End Sub

    ' Separate method for DataGridView avatars (different size)
    Private Function CreateDefaultAvatar(username As String) As System.Drawing.Image
        Dim bitmap As New Bitmap(45, 45)  ' Slightly larger for photo column
        Using g As Graphics = Graphics.FromImage(bitmap)
            ' Enable anti-aliasing for smooth circles
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            ' Fill background with a color based on username
            Dim colors() As System.Drawing.Color = {
                System.Drawing.Color.FromArgb(255, 107, 107),
                System.Drawing.Color.FromArgb(78, 205, 196),
                System.Drawing.Color.FromArgb(85, 98, 112),
                System.Drawing.Color.FromArgb(129, 236, 236),
                System.Drawing.Color.FromArgb(116, 185, 255)
            }
            Dim colorIndex As Integer = Math.Abs(username.GetHashCode()) Mod colors.Length
            g.FillEllipse(New SolidBrush(colors(colorIndex)), 0, 0, 45, 45)

            ' Draw initials
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

            Using font As New System.Drawing.Font("Poppins", 12, System.Drawing.FontStyle.Bold)  ' Slightly larger font
                Dim textSize = g.MeasureString(initials, font)
                g.DrawString(initials, font, Brushes.White,
                    (45 - textSize.Width) / 2, (45 - textSize.Height) / 2)
            End Using
        End Using
        Return bitmap
    End Function

    Private Sub SortBy_SelectedIndexChanged(sender As Object, e As EventArgs) Handles SortBy.SelectedIndexChanged
        If SortBy.SelectedItem IsNot Nothing Then
            LoadUsersData(SortBy.SelectedItem.ToString())
        End If
    End Sub

    Private Sub Guna2DataGridView1_CellClick(sender As Object, e As DataGridViewCellEventArgs) Handles Guna2DataGridView1.CellClick
        ' Handle clicks on the Actions column
        If e.RowIndex >= 0 AndAlso e.ColumnIndex = Guna2DataGridView1.Columns("Actions").Index Then
            Dim userData As Dictionary(Of String, Object) = CType(Guna2DataGridView1.Rows(e.RowIndex).Tag, Dictionary(Of String, Object))

            ' Get the cell rectangle to determine click position
            Dim cellRect As Rectangle = Guna2DataGridView1.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, False)

            ' Calculate relative click position within the cell
            Dim mousePos As Point = Guna2DataGridView1.PointToClient(MousePosition)
            Dim clickX As Integer = mousePos.X - cellRect.X

            ' Add click effect - briefly change cell background
            Guna2DataGridView1.Rows(e.RowIndex).Cells("Actions").Style.BackColor = System.Drawing.Color.FromArgb(255, 204, 77)
            Dim clickTimer As New Timer()
            clickTimer.Interval = 150
            AddHandler clickTimer.Tick, Sub()
                                            Guna2DataGridView1.Rows(e.RowIndex).Cells("Actions").Style.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
                                            clickTimer.Stop()
                                        End Sub
            clickTimer.Start()

            ' Determine which action was clicked based on position
            If clickX < 65 Then
                ' View action
                ViewUser(userData)
            ElseIf clickX < 130 Then
                ' Edit action
                EditUser(userData)
            Else
                ' Delete action
                DeleteUser(userData)
            End If
        End If
    End Sub

    ' Add hover effects for DataGridView
    Private Sub Guna2DataGridView1_CellMouseEnter(sender As Object, e As DataGridViewCellEventArgs) Handles Guna2DataGridView1.CellMouseEnter
        If e.RowIndex >= 0 AndAlso e.ColumnIndex = Guna2DataGridView1.Columns("Actions").Index Then
            ' Add hover effect for actions column
            Guna2DataGridView1.Rows(e.RowIndex).Cells("Actions").Style.BackColor = System.Drawing.Color.FromArgb(81, 85, 86)
            Guna2DataGridView1.Cursor = Cursors.Hand
        End If
    End Sub

    Private Sub Guna2DataGridView1_CellMouseLeave(sender As Object, e As DataGridViewCellEventArgs) Handles Guna2DataGridView1.CellMouseLeave
        If e.RowIndex >= 0 AndAlso e.ColumnIndex = Guna2DataGridView1.Columns("Actions").Index Then
            ' Remove hover effect for actions column
            Guna2DataGridView1.Rows(e.RowIndex).Cells("Actions").Style.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
            Guna2DataGridView1.Cursor = Cursors.Default
        End If
    End Sub

    Private Sub ViewUser(userData As Dictionary(Of String, Object))
        ' For now, just show user details in a message box
        ' Later you can create a proper view form
        Dim userInfo As String = $"User Details:{vbCrLf}{vbCrLf}" &
                                $"ID: {userData("UserID")}{vbCrLf}" &
                                $"Username: {userData("Username")}{vbCrLf}" &
                                $"Full Name: {userData("FullName")}{vbCrLf}" &
                                $"Role: {userData("UserRole")}{vbCrLf}" &
                                $"Status: {If(CBool(userData("IsActive")), "Active", "Inactive")}"

        MessageBox.Show(userInfo, "Staff Details", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' PIN confirmation dialog for editing staff
    Private Function ShowPinDialog(promptText As String) As Boolean
        Dim pinDialog As New Form()
        pinDialog.Text = "PIN Confirmation"
        pinDialog.Size = New System.Drawing.Size(320, 180)
        pinDialog.StartPosition = FormStartPosition.CenterParent
        pinDialog.BackColor = System.Drawing.Color.FromArgb(41, 44, 45)
        pinDialog.FormBorderStyle = FormBorderStyle.FixedDialog
        pinDialog.MaximizeBox = False
        pinDialog.MinimizeBox = False

        Dim lblPrompt As New Label()
        lblPrompt.Text = promptText
        lblPrompt.ForeColor = System.Drawing.Color.White
        lblPrompt.Font = New Font("Poppins", 10)
        lblPrompt.AutoSize = True
        lblPrompt.Location = New System.Drawing.Point(20, 20)

        Dim txtPin As New TextBox()
        txtPin.PasswordChar = "●"c
        txtPin.MaxLength = 4
        txtPin.Location = New System.Drawing.Point(20, 50)
        txtPin.Size = New System.Drawing.Size(260, 30)
        txtPin.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        txtPin.ForeColor = System.Drawing.Color.White
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
        btnCancel.BackColor = System.Drawing.Color.Gray
        btnCancel.ForeColor = System.Drawing.Color.White
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
        pinDialog.ShowDialog()

        Return pinAccepted
    End Function

    Private Sub EditUser(userData As Dictionary(Of String, Object))
        ' Open AddStaff form in edit mode
        Try
            Dim addStaffForm As New AddStaff()

            ' Set the form to edit mode and pass the user data
            addStaffForm.SetEditMode(userData)

            ' Show the form as a modal dialog
            Dim result = addStaffForm.ShowDialog()

            ' Refresh the staff list after editing
            LoadUsersData(If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), ""))

        Catch ex As Exception
            MessageBox.Show($"Error opening Edit Staff form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DeleteUser(userData As Dictionary(Of String, Object))
        Dim username As String = userData("Username").ToString()
        Dim userId As Integer = CInt(userData("UserID"))

        ' Prevent deletion of current user
        If username = frmLoginvb.LoggedInUsername Then
            MessageBox.Show("You cannot delete your own account while logged in.", "Action Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        ' Confirm deletion
        Dim result As DialogResult = MessageBox.Show(
            $"Are you sure you want to delete user '{username}'?" & vbCrLf & vbCrLf &
            "This action cannot be undone.",
            "Confirm Deletion",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning
        )

        If result = DialogResult.Yes Then
            Try
                Dim query As String = "DELETE FROM Users WHERE UserID = @UserID"
                Dim parameters As SqlParameter() = {
                    New SqlParameter("@UserID", userId)
                }

                Dim rowsAffected As Integer = Utilities.ExecuteNonQuery(query, parameters)

                If rowsAffected > 0 Then
                    MessageBox.Show("User deleted successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

                    ' Log the deletion
                    If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                        Utilities.LogAudit(frmLoginvb.LoggedInUsername, "User Deleted", $"Deleted user: {username} (UserID: {userId})")
                    End If

                    ' Refresh the data
                    LoadUsersData(If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), ""))
                Else
                    MessageBox.Show("Failed to delete user.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If

            Catch ex As Exception
                MessageBox.Show($"Error deleting user: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

                ' Log the error
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "User Delete Failed", $"Error deleting {username}: {ex.Message}")
                End If
            End Try
        End If
    End Sub

    Private Sub btnDiscount_Click(sender As Object, e As EventArgs) Handles btnDiscount.Click
        ' Open AddStaff form for adding new staff members
        Try
            Dim addStaffForm As New AddStaff()
            addStaffForm.ShowDialog()

            ' Refresh the staff list after adding new staff
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
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to exit the application?", "Exit Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

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

    Private Sub CreateNavigationMenu()
        Try
            ' Clear existing controls except PictureBox9 (logo)
            For i = DashboardPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = DashboardPanel.Controls(i)
                If TypeOf control IsNot PictureBox Then
                    DashboardPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            ' Set Navigation Panel Background to White
            DashboardPanel.FillColor = System.Drawing.Color.White

            ' Calculate available space (DashboardPanel is 236x999)
            Dim availableWidth As Integer = DashboardPanel.Width - 40 ' 20px margins on each side
            Dim availableHeight As Integer = DashboardPanel.Height - 160 ' Space for logo and title

            ' Logo area (keep existing PictureBox9)
            PictureBox9.BringToFront()

            ' Add title label - positioned below logo with Golden Yellow
            Dim titleLabel As New Label()
            titleLabel.Text = "JADE CLINIC"
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = System.Drawing.Color.FromArgb(254, 191, 16) ' Golden Yellow #FECF10
            titleLabel.BackColor = System.Drawing.Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New System.Drawing.Size(availableWidth, 30)
            titleLabel.Location = New Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(titleLabel)

            ' Subtitle with Dark Gray color (visible on white background)
            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Poppins", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = System.Drawing.Color.FromArgb(100, 100, 100) ' Dark Gray for visibility on white
            subtitleLabel.BackColor = System.Drawing.Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New System.Drawing.Size(availableWidth, 25)
            subtitleLabel.Location = New Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(subtitleLabel)

            ' Navigation section separator with Light Gray (visible on white background)
            Dim separator1 As New Panel()
            separator1.BackColor = System.Drawing.Color.FromArgb(220, 220, 220) ' Light Gray for white background
            separator1.Size = New System.Drawing.Size(availableWidth - 20, 2)
            separator1.Location = New Point(30, 190)
            DashboardPanel.Controls.Add(separator1)

            ' Navigation section label with Dark Gray (visible on white background)
            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Poppins", 10, FontStyle.Bold)
            navLabel.ForeColor = System.Drawing.Color.FromArgb(80, 80, 80) ' Dark Gray for visibility on white
            navLabel.BackColor = System.Drawing.Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New System.Drawing.Size(availableWidth, 25)
            navLabel.Location = New Point(20, 205)
            navLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(navLabel)

            ' Calculate button positioning for role-based navigation
            Dim startY As Integer = 250
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonWidth As Integer = availableWidth - 5
            Dim buttonIndex As Integer = 0

            ' Get current user role for navigation filtering
            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "Staff").ToUpper()

            ' Create navigation buttons based on role
            ' Dashboard Button (not active)
            Dim navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
            buttonIndex += 1

            ' POS/Sales Button (all roles)
            Dim navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navPOSBtn.Click, AddressOf NavPOS_Click
            buttonIndex += 1

            ' Manager and Admin only buttons - Inventory moved here
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                ' Inventory Button (only for Manager and Admin)
                Dim navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
                buttonIndex += 1

                ' Sales Records Button
                Dim navSalesRecordsBtn = CreateLargeNavButton("📊 Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navSalesRecordsBtn.Click, AddressOf NavSalesRecords_Click
                buttonIndex += 1

                ' Staff Management Button (ACTIVE - we're on this page) - Only for Admin
                If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                    Dim navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
                    buttonIndex += 1
                End If

                ' Inventory Logs Button
                Dim navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
                buttonIndex += 1
            End If

            ' Admin only buttons
            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                ' Audit Logs Button
                Dim navAuditLogBtn = CreateLargeNavButton("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navAuditLogBtn.Click, AddressOf NavAuditLog_Click
                buttonIndex += 1

                ' System Settings Button
                Dim systemSettingsBtn = CreateLargeNavButton("⚙️ System", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler systemSettingsBtn.Click, Sub() MessageBox.Show("System Settings feature coming soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
                buttonIndex += 1
            End If

            ' Add separator line before logout
            Dim separator2 As New Panel()
            separator2.BackColor = System.Drawing.Color.FromArgb(220, 220, 220) ' Light Gray for white background
            separator2.Size = New System.Drawing.Size(availableWidth - 40, 2)
            separator2.Location = New Point(40, startY + buttonIndex * (buttonHeight + buttonSpacing) + 10)
            DashboardPanel.Controls.Add(separator2)

            ' Logout Button (at bottom with Alert Red styling)
            Dim navLogoutBtn = CreateLargeNavButton("🚪 Logout", startY + buttonIndex * (buttonHeight + buttonSpacing) + 30, False, buttonWidth, buttonHeight)
            navLogoutBtn.FillColor = System.Drawing.Color.FromArgb(255, 71, 87) ' Alert Red #FF4757
            navLogoutBtn.ForeColor = System.Drawing.Color.White

            ' Override hover effects for logout button to maintain red background
            RemoveHandler navLogoutBtn.MouseEnter, Nothing
            RemoveHandler navLogoutBtn.MouseLeave, Nothing
            AddHandler navLogoutBtn.MouseEnter, Sub()
                                                    navLogoutBtn.FillColor = System.Drawing.Color.FromArgb(220, 50, 50) ' Slightly darker red on hover
                                                    navLogoutBtn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                                End Sub
            AddHandler navLogoutBtn.MouseLeave, Sub()
                                                    navLogoutBtn.FillColor = System.Drawing.Color.FromArgb(255, 71, 87) ' Back to original red
                                                    navLogoutBtn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                                End Sub

            AddHandler navLogoutBtn.Click, AddressOf NavLogout_Click

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
        End Try
    End Sub

    Private Function CreateLargeNavButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer) As Guna.UI2.WinForms.Guna2Button
        Dim btn As New Guna.UI2.WinForms.Guna2Button()

        ' Button properties with improved sizing and new color scheme
        btn.Text = text
        btn.Size = New System.Drawing.Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left

        ' Apply new color scheme
        btn.FillColor = If(isActive, System.Drawing.Color.FromArgb(254, 191, 16), System.Drawing.Color.Transparent) ' Golden Yellow if active #FECF10
        btn.ForeColor = If(isActive, System.Drawing.Color.FromArgb(26, 29, 31), System.Drawing.Color.FromArgb(50, 50, 50)) ' Deep Charcoal text on active, Dark Gray text on inactive for white background
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(200, 200, 200)) ' Light Gray border for white background
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        ' Add subtle shadow for depth
        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = System.Drawing.Color.FromArgb(26, 29, 31) ' Deep Charcoal shadow
        btn.ShadowDecoration.Depth = 5
        btn.ShadowDecoration.Shadow = New Padding(0, 2, 5, 5)

        ' Improved hover effects with new color scheme
        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.FromArgb(240, 240, 240) ' Light Gray hover for white background
                                           btn.BorderColor = System.Drawing.Color.FromArgb(190, 154, 48) ' Rich Olive border #BE9A30
                                           btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                       End If
                                   End Sub

        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.Transparent
                                           btn.BorderColor = System.Drawing.Color.FromArgb(200, 200, 200) ' Light Gray border
                                           btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        ' Add to panel
        DashboardPanel.Controls.Add(btn)

        Return btn
    End Function

    ' Navigation event handlers
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

    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sales.Show()
        Me.Close()
    End Sub

    Private Sub NavInventoryLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        InventoryLog.Show()
        Me.Close()
    End Sub

    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        ' For now, show coming soon message
        MessageBox.Show("Audit Logs feature coming soon!", "Feature Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
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

    Private Sub NavigateToProfileSettings()
        If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Navigation", "Navigated from Staff to ProfileSettings")
        End If
        isNavigating = True
        ' Implement ProfileSettings form later
        MessageBox.Show("Profile Settings will be implemented.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub PictureBox9_Click(sender As Object, e As EventArgs)

    End Sub
End Class