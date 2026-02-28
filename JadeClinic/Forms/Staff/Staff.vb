Imports Microsoft.Data.SqlClient
Imports System.IO
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure
Imports System.Drawing
Imports System.Drawing.Imaging
Imports SD = System.Drawing  ' <-- added alias for System.Drawing


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
        ' Center all cell text
        Guna2DataGridView1.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        ' (Optional) Center header text
        ' Dark themed DataGridView (match SalesRecord / Inventory)
        Guna2DataGridView1.BackgroundColor = System.Drawing.Color.FromArgb(41, 44, 45)
        Guna2DataGridView1.GridColor = System.Drawing.Color.White
        Guna2DataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        Guna2DataGridView1.EnableHeadersVisualStyles = False

        Guna2DataGridView1.DefaultCellStyle = New DataGridViewCellStyle() With {
        .BackColor = System.Drawing.Color.FromArgb(61, 65, 66),
        .ForeColor = System.Drawing.Color.LightGray,
        .SelectionBackColor = System.Drawing.Color.FromArgb(255, 204, 77),
        .SelectionForeColor = System.Drawing.Color.Black,
        .Font = New Font("Poppins", 9.0F, FontStyle.Regular),
        .Alignment = DataGridViewContentAlignment.MiddleCenter,
        .Padding = New Padding(8, 6, 8, 6)
    }

        Guna2DataGridView1.AlternatingRowsDefaultCellStyle = New DataGridViewCellStyle() With {
        .BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
    }

        Guna2DataGridView1.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle() With {
        .BackColor = System.Drawing.Color.FromArgb(30, 30, 30),
        .ForeColor = System.Drawing.Color.LightGray,
        .SelectionBackColor = System.Drawing.Color.FromArgb(30, 30, 30),
        .Font = New Font("Poppins SemiBold", 10.0F, FontStyle.Regular),
        .Alignment = DataGridViewContentAlignment.MiddleCenter
    }
        Guna2DataGridView1.ColumnHeadersHeight = 50
        Guna2DataGridView1.RowTemplate.Height = 60
        Guna2DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing

        ' Add columns (center aligned by default, except Username/FullName)
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
        .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleCenter}
    }
        Guna2DataGridView1.Columns.Add(photoCol)

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
        .Name = "Username",
        .HeaderText = "Username",
        .ReadOnly = True,
        .DefaultCellStyle = New DataGridViewCellStyle() With {.Alignment = DataGridViewContentAlignment.MiddleLeft}
    })

        Guna2DataGridView1.Columns.Add(New DataGridViewTextBoxColumn() With {
        .Name = "FullName",
        .HeaderText = "Full Name",
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
            .ForeColor = System.Drawing.Color.LightGray
        }
    }
        Guna2DataGridView1.Columns.Add(actionsCol)

        ' Grid behavior
        Guna2DataGridView1.AllowUserToAddRows = False
        Guna2DataGridView1.AllowUserToDeleteRows = False
        Guna2DataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        Guna2DataGridView1.MultiSelect = False
        Guna2DataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
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
                    Guna2DataGridView1.Rows(rowIndex).Cells("Actions").Value = "👁️      |    ✏️  "
                    Dim statusCell = Guna2DataGridView1.Rows(rowIndex).Cells("IsActive")
                    statusCell.Value = If(isActive, "✅ Active", "❌ Inactive")

                    statusCell.Style.ForeColor = If(isActive, SD.Color.FromArgb(16, 216, 98), SD.Color.FromArgb(255, 71, 87))
                    statusCell.Style.Font = New Font(Guna2DataGridView1.Font, FontStyle.Bold)

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

            ' If Photo or QRCode missing, fetch from DB using UserID
            Dim needsPhoto As Boolean = Not userData.ContainsKey("Photo") OrElse userData("Photo") Is Nothing
            Dim needsQr As Boolean = Not userData.ContainsKey("QRCode") OrElse String.IsNullOrWhiteSpace(If(userData("QRCode"), String.Empty).ToString())

            If (needsPhoto OrElse needsQr) AndAlso userData.ContainsKey("UserID") Then
                Try
                    Dim userId As Integer = Convert.ToInt32(userData("UserID"))
                    Using reader As SqlDataReader = Utilities.ExecuteReader("SELECT Photo, QRCode FROM Users WHERE UserID = @UserID", New SqlParameter("@UserID", userId))
                        If reader.Read() Then
                            If needsPhoto AndAlso Not IsDBNull(reader("Photo")) Then
                                userData("Photo") = CType(reader("Photo"), Byte())
                            End If
                            If needsQr AndAlso Not IsDBNull(reader("QRCode")) Then
                                userData("QRCode") = reader("QRCode").ToString()
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    ' Non-fatal: if DB fetch fails we'll still show the card with whatever we have
                    Console.WriteLine($"Error fetching Photo/QRCode from DB: {ex.Message}")
                End Try
            End If

            ' Open IdCard form and populate it
            Dim idForm As New IdCard()
            idForm.StartPosition = FormStartPosition.CenterParent
            idForm.LoadFromUserData(userData)
            idForm.ShowDialog()
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
        Try
            If userData Is Nothing Then
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
                addStaffForm.ShowDialog()
            End Using

            ' Refresh the staff list after possible edits
            LoadUsersData(If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), ""))
        Catch ex As Exception
            MessageBox.Show($"Error opening Edit Staff form: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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
            ' Remove all controls except the logo
            For i = DashboardPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = DashboardPanel.Controls(i)
                If TypeOf control IsNot PictureBox Then
                    DashboardPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            DashboardPanel.FillColor = System.Drawing.Color.FromArgb(61, 65, 66)

            ' Render company logo into existing PictureBox9 WITHOUT resizing or adding handlers
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
            subtitleLabel.Text = "Staff Management"
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

            ' Role logic
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

            ' 5. Staff (ACTIVE on this page)
            Dim navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), True, buttonWidth, buttonHeight)
            ' Keep active but allow refresh if clicked
            AddHandler navStaffBtn.Click, Sub()
                                              ' refresh staff list when clicking active button
                                              Try
                                                  LoadUsersData(If(SortBy.SelectedItem IsNot Nothing, SortBy.SelectedItem.ToString(), ""))
                                              Catch ex As Exception
                                              End Try
                                          End Sub
            buttonIndex += 1

            ' 6. Inventory Logs
            Dim navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
            buttonIndex += 1

            ' 7. Suppliers
            Dim navSuppliersBtn = CreateLargeNavButton("🏷️ Suppliers", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navSuppliersBtn.Click, AddressOf NavSuppliers_Click
            buttonIndex += 1

            ' 8. Audit Logs (admin only)
            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navAuditLogBtn = CreateLargeNavButton("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navAuditLogBtn.Click, AddressOf NavAuditLog_Click
                buttonIndex += 1
            End If

            ' 9. System (admin only)
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

            ' If Photo or QRCode not provided, attempt DB fetch
            If (Not rowTag.ContainsKey("Photo") OrElse rowTag("Photo") Is Nothing) OrElse (Not rowTag.ContainsKey("QRCode") OrElse String.IsNullOrWhiteSpace(If(rowTag("QRCode"), String.Empty).ToString())) Then
                If rowTag.ContainsKey("UserID") Then
                    Try
                        Dim userId As Integer = Convert.ToInt32(rowTag("UserID"))
                        Using reader As SqlDataReader = Utilities.ExecuteReader("SELECT Photo, QRCode FROM Users WHERE UserID = @UserID", New SqlParameter("@UserID", userId))
                            If reader.Read() Then
                                If Not IsDBNull(reader("Photo")) Then
                                    rowTag("Photo") = CType(reader("Photo"), Byte())
                                End If
                                If Not IsDBNull(reader("QRCode")) Then
                                    rowTag("QRCode") = reader("QRCode").ToString()
                                End If
                            End If
                        End Using
                    Catch ex As Exception
                        ' ignore DB errors, proceed with whatever we have
                        Console.WriteLine($"Error fetching photo/qr from DB: {ex.Message}")
                    End Try
                End If
            End If

            ' Show IdCard form
            Dim idForm As New IdCard()
            idForm.StartPosition = FormStartPosition.CenterParent
            idForm.LoadFromUserData(rowTag)
            idForm.ShowDialog()

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
End Class