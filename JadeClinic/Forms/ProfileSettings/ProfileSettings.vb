Imports Microsoft.Data.SqlClient
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Drawing.Imaging

Public Class ProfileSettings
    ' Store original panel controls for PIN change functionality
    Private originalPanel1Controls As List(Of Control)
    Private pinPanelActive As Boolean = False
    Private pinInput As String = ""
    Private oldPinInput As String = ""
    Private newPinInput As String = ""
    Private confirmPinInput As String = ""
    Private pinChangeStep As Integer = 0 ' 0=old pin, 1=new pin, 2=confirm pin
    Private pinPanelButtons As List(Of Guna.UI2.WinForms.Guna2Button)
    Private pinIndicators As List(Of Guna.UI2.WinForms.Guna2CircleButton)
    Private currentUserData As Dictionary(Of String, Object)

    ' Store the pending profile picture change
    Private pendingProfileImage As Image = Nothing
    Private pendingProfileImageBytes As Byte() = Nothing
    Private hasProfileImageChanged As Boolean = False

    ' Navigation flag to prevent modal dialogs during programmatic navigation
    Private isNavigating As Boolean = False

    ' Dental Clinic Color Palette Constants
    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)      ' #FECF10 - Primary brand color
    Private ReadOnly RichOlive As Color = Color.FromArgb(190, 154, 48)         ' #BE9A30 - Secondary accent
    Private ReadOnly DeepCharcoal As Color = Color.FromArgb(26, 29, 31)        ' #1A1D1F - Primary dark
    Private ReadOnly DarkSlate As Color = Color.FromArgb(43, 47, 50)           ' #2B2F32 - Secondary dark
    Private ReadOnly Graphite As Color = Color.FromArgb(61, 65, 69)            ' #3D4145 - Card background
    Private ReadOnly SteelGray As Color = Color.FromArgb(74, 79, 84)           ' #4A4F54 - Interactive elements
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)        ' #FFFFFF - Text on dark
    Private ReadOnly LightSilver As Color = Color.FromArgb(225, 229, 233)      ' #E1E5E9 - Secondary text
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)       ' #10D862 - Success states
    Private ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)

    Private Sub ProfileSettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me) ' Initialize form
        Me.MaximizeBox = False
        Me.Text = "Profile Settings - Personal Information"

        ' Validate user session
        If Not ValidateUserSession() Then
            Return
        End If

        ' Load current user data
        LoadCurrentUserData()

        ' Store original controls for PIN change functionality (after data is loaded)
        originalPanel1Controls = New List(Of Control)(Guna2Panel1.Controls.Cast(Of Control)())

        ' Initialize navigation (adds DashboardPanel menu and profile area)
        InitializeNavigation()

        ' Initialize side panel events
        InitializeSidePanelEvents()

        ' Set password fields to password mode
        txtNewPassword.PasswordChar = "●"c
        txtConfirmPassword.PasswordChar = "●"c

        ' Ensure the profile settings panel is visible and active by default
        Guna2Panel1.Visible = True
        Guna2Panel1.BringToFront()

        ' Enable keyboard input for the form
        Me.KeyPreview = True

        ' Set focus to form to ensure keyboard events are captured
        Me.Focus()
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

    Private Sub LoadCurrentUserData()
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                ' Query to get the logged-in user's details - updated to match current schema
                ' NOTE: passkeys are stored in a single column named "Passkeys" in our schema.
                Dim query As String =
                "SELECT UserID, Username, FullName, Email, Phone, Photo, PasswordHash, pin, Passkeys, UserRole " &
                "FROM Users WHERE Username = @Username AND IsActive = 1"

                Dim parameters As SqlParameter() = {
                New SqlParameter("@Username", frmLoginvb.LoggedInUsername)
            }

                Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                    If reader.Read() Then
                        ' Read combined passkeys column (single column in current schema)
                        Dim passkeysCombined As String = If(IsDBNull(reader("Passkeys")), "", reader("Passkeys").ToString())

                        currentUserData = New Dictionary(Of String, Object) From {
                        {"UserID", reader("UserID")},
                        {"Username", reader("Username").ToString()},
                        {"FullName", If(IsDBNull(reader("FullName")), "", reader("FullName").ToString())},
                        {"PasswordHash", If(IsDBNull(reader("PasswordHash")), "", reader("PasswordHash").ToString())},
                        {"PIN", If(IsDBNull(reader("pin")), "", reader("pin").ToString())},
                        {"Email", If(IsDBNull(reader("Email")), "", reader("Email").ToString())},
                        {"Phone", If(IsDBNull(reader("Phone")), "", reader("Phone").ToString())},
                        {"Passkeys", passkeysCombined},
                        {"Photo", If(Not IsDBNull(reader("Photo")), reader("Photo"), Nothing)},
                        {"Role", If(IsDBNull(reader("UserRole")), "", reader("UserRole").ToString())}
                    }

                        ' Populate form fields
                        PopulateFormFields()
                    End If
                End Using
            End If
        Catch ex As SqlException
            ' If the selected column name doesn't exist for some reason, fall back to a safe query
            If ex.Message.Contains("Invalid column name") Then
                Try
                    Dim fallbackQuery As String = "SELECT UserID, Username, FullName, Email, Phone, Photo, PasswordHash, pin, UserRole FROM Users WHERE Username = @Username AND IsActive = 1"
                    Dim parameters As SqlParameter() = {
                    New SqlParameter("@Username", frmLoginvb.LoggedInUsername)
                }
                    Using reader As SqlDataReader = Utilities.ExecuteReader(fallbackQuery, parameters)
                        If reader.Read() Then
                            currentUserData = New Dictionary(Of String, Object) From {
                            {"UserID", reader("UserID")},
                            {"Username", reader("Username").ToString()},
                            {"FullName", If(IsDBNull(reader("FullName")), "", reader("FullName").ToString())},
                            {"PasswordHash", If(IsDBNull(reader("PasswordHash")), "", reader("PasswordHash").ToString())},
                            {"PIN", If(IsDBNull(reader("pin")), "", reader("pin").ToString())},
                            {"Email", If(IsDBNull(reader("Email")), "", reader("Email").ToString())},
                            {"Phone", If(IsDBNull(reader("Phone")), "", reader("Phone").ToString())},
                            {"Passkeys", ""}, ' no passkeys column available
                            {"Photo", If(Not IsDBNull(reader("Photo")), reader("Photo"), Nothing)},
                            {"Role", If(IsDBNull(reader("UserRole")), "", reader("UserRole").ToString())}
                        }
                            PopulateFormFields()
                        End If
                    End Using
                Catch innerEx As Exception
                    MessageBox.Show($"Error loading user data (fallback): {innerEx.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            Else
                MessageBox.Show($"Error loading user data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error loading user data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PopulateFormFields()
        Try
            ' Populate text fields
            lblUsername.Text = currentUserData("Username").ToString()
            lblRole.Text = If(currentUserData.ContainsKey("Role") AndAlso Not String.IsNullOrEmpty(currentUserData("Role").ToString()), currentUserData("Role").ToString(), "Unknown")
            txtUserName.Text = currentUserData("Username").ToString()
            txtEmail.Text = currentUserData("Email").ToString()
            txtPhone.Text = currentUserData("Phone").ToString()

            ' Load profile picture (main panel)
            LoadProfilePicture()

            ' Load passkeys (split by comma if multiple)
            LoadPasskeys()

            ' NAVIGATION: ensure company logo remains in the nav PictureBox (PictureBox9).
            ' Do NOT overwrite the nav logo with the user's photo here.
            Try
                Dim companyLogo As Image = Nothing
                Try
                    companyLogo = CompanySettingsManager.Instance.GetCompanyLogo()
                Catch
                    companyLogo = Nothing
                End Try

                If PictureBox9 IsNot Nothing AndAlso companyLogo IsNot Nothing Then
                    PictureBox9.Image = companyLogo
                    PictureBox9.SizeMode = PictureBoxSizeMode.StretchImage
                End If

                ' Put user avatar into the Guna circle picture box (nav avatar).
                Dim userImage As Image = Nothing
                If currentUserData IsNot Nothing AndAlso currentUserData("Photo") IsNot Nothing Then
                    Dim photoBytes As Byte() = CType(currentUserData("Photo"), Byte())
                    Using ms As New MemoryStream(photoBytes)
                        userImage = Image.FromStream(ms)
                    End Using
                Else
                    userImage = CreateDefaultAvatar(currentUserData("Username").ToString())
                End If

                If Guna2CirclePictureBox1 IsNot Nothing AndAlso userImage IsNot Nothing Then
                    Guna2CirclePictureBox1.Image = userImage
                    Guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.Zoom
                End If
            Catch
                ' ignore nav avatar failures - never overwrite company logo
            End Try

        Catch ex As Exception
            MessageBox.Show($"Error populating form fields: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadProfilePicture()
        Try
            If currentUserData("Photo") IsNot Nothing Then
                Dim photoBytes As Byte() = CType(currentUserData("Photo"), Byte())
                Using ms As New MemoryStream(photoBytes)
                    Guna2CirclePictureBox7.Image = System.Drawing.Image.FromStream(ms)
                    Guna2CirclePictureBox7.SizeMode = PictureBoxSizeMode.Zoom
                End Using
            Else
                ' Create default avatar
                Guna2CirclePictureBox7.Image = CreateDefaultAvatar(currentUserData("Username").ToString())
            End If
        Catch ex As Exception
            ' Set default avatar on error
            Guna2CirclePictureBox7.Image = CreateDefaultAvatar(currentUserData("Username").ToString())
        End Try
    End Sub

    Private Function CreateDefaultAvatar(username As String) As System.Drawing.Image
        Dim bitmap As New Bitmap(100, 100)
        Using g As Graphics = Graphics.FromImage(bitmap)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

            ' Fill background with color based on username
            Dim colors() As System.Drawing.Color = {
                System.Drawing.Color.FromArgb(255, 107, 107),
                System.Drawing.Color.FromArgb(78, 205, 196),
                System.Drawing.Color.FromArgb(85, 98, 112),
                System.Drawing.Color.FromArgb(129, 236, 236),
                System.Drawing.Color.FromArgb(116, 185, 255)
            }
            Dim colorIndex As Integer = Math.Abs(username.GetHashCode()) Mod colors.Length
            g.FillEllipse(New SolidBrush(colors(colorIndex)), 0, 0, 100, 100)

            ' Draw initials
            Dim initials As String = username.Substring(0, Math.Min(2, username.Length)).ToUpper()
            Using font As New System.Drawing.Font("Poppins", 24, System.Drawing.FontStyle.Bold)
                Dim textSize = g.MeasureString(initials, font)
                g.DrawString(initials, font, Brushes.White,
                    (100 - textSize.Width) / 2, (100 - textSize.Height) / 2)
            End Using
        End Using
        Return bitmap
    End Function

    Private Sub LoadPasskeys()
        Try
            Dim passkeysText As String = If(currentUserData.ContainsKey("Passkeys"), currentUserData("Passkeys").ToString(), "")
            If Not String.IsNullOrEmpty(passkeysText) Then
                Dim passkeys = passkeysText.Split(","c)

                ' Hide all passkey labels initially
                lblpasskey1.Text = "••••••"
                lblpasskey2.Text = "••••••"
                lblpasskey3.Text = "••••••"

                ' Show available passkeys (hidden by default)
                If passkeys.Length >= 1 AndAlso Not String.IsNullOrEmpty(passkeys(0).Trim()) Then
                    lblpasskey1.Tag = passkeys(0).Trim()
                End If
                If passkeys.Length >= 2 AndAlso Not String.IsNullOrEmpty(passkeys(1).Trim()) Then
                    lblpasskey2.Tag = passkeys(1).Trim()
                End If
                If passkeys.Length >= 3 AndAlso Not String.IsNullOrEmpty(passkeys(2).Trim()) Then
                    lblpasskey3.Tag = passkeys(2).Trim()
                End If
            End If
        Catch ex As Exception
            ' Silent fail for passkeys
        End Try
    End Sub

    Private Sub Guna2CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles Guna2CheckBox1.CheckedChanged
        If Guna2CheckBox1.Checked Then
            ' Prompt for PIN before showing passkeys
            Dim pinDialog As New Form()
            pinDialog.Text = "PIN Confirmation"
            pinDialog.Size = New Size(320, 180)
            pinDialog.StartPosition = FormStartPosition.CenterParent
            pinDialog.BackColor = System.Drawing.Color.FromArgb(41, 44, 45)
            pinDialog.FormBorderStyle = FormBorderStyle.FixedDialog
            pinDialog.MaximizeBox = False
            pinDialog.MinimizeBox = False

            Dim lblPrompt As New Label()
            lblPrompt.Text = "Enter your PIN to view passkeys:"
            lblPrompt.ForeColor = Color.White
            lblPrompt.Font = New Font("Poppins", 10)
            lblPrompt.AutoSize = True
            lblPrompt.Location = New Point(20, 20)

            Dim txtPin As New TextBox()
            txtPin.PasswordChar = "●"c
            txtPin.MaxLength = 4
            txtPin.Location = New Point(20, 50)
            txtPin.Size = New Size(260, 30)
            txtPin.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
            txtPin.ForeColor = Color.White
            txtPin.Font = New Font("Poppins", 12)

            Dim btnConfirm As New Button()
            btnConfirm.Text = "Confirm"
            btnConfirm.Location = New Point(20, 90)
            btnConfirm.Size = New Size(100, 32)
            btnConfirm.BackColor = System.Drawing.Color.FromArgb(255, 204, 77)
            btnConfirm.ForeColor = Color.Black
            btnConfirm.Font = New Font("Poppins", 10)
            btnConfirm.FlatStyle = FlatStyle.Flat

            Dim btnCancel As New Button()
            btnCancel.Text = "Cancel"
            btnCancel.Location = New Point(140, 90)
            btnCancel.Size = New Size(100, 32)
            btnCancel.BackColor = Color.Gray
            btnCancel.ForeColor = Color.White
            btnCancel.Font = New Font("Poppins", 10)
            btnCancel.FlatStyle = FlatStyle.Flat

            Dim pinAccepted As Boolean = False
            AddHandler btnConfirm.Click, Sub()
                                             If txtPin.Text = currentUserData("PIN").ToString() Then
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
            pinDialog.ShowDialog(Me)

            If pinAccepted Then
                If lblpasskey1.Tag IsNot Nothing Then lblpasskey1.Text = lblpasskey1.Tag.ToString()
                If lblpasskey2.Tag IsNot Nothing Then lblpasskey2.Text = lblpasskey2.Tag.ToString()
                If lblpasskey3.Tag IsNot Nothing Then lblpasskey3.Text = lblpasskey3.Tag.ToString()
            Else
                ' PIN not accepted, keep passkeys hidden and uncheck
                lblpasskey1.Text = "••••••"
                lblpasskey2.Text = "••••••"
                lblpasskey3.Text = "••••••"
                Guna2CheckBox1.Checked = False
            End If
        Else
            lblpasskey1.Text = "••••••"
            lblpasskey2.Text = "••••••"
            lblpasskey3.Text = "••••••"
        End If
    End Sub

    Private Sub ShowProfileSettingsPanel()
        ' Always restore to profile settings panel and ensure it's visible
        If pinPanelActive Then
            RestorePanel1Controls()
        End If

        ' Ensure the panel is visible and bring it to front
        Guna2Panel1.Visible = True
        Guna2Panel1.BringToFront()

        ' Update the form title to reflect current view
        Me.Text = "Profile Settings - Personal Information"

        ' Set focus back to form for keyboard handling
        Me.Focus()
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        ' Validate all fields before asking for PIN
        If Not ValidateAllFields() Then
            Return
        End If

        ' Ask for PIN confirmation before saving
        ShowPinConfirmationDialog(Sub() SaveUserChanges())
    End Sub

    Private Function ValidateAllFields() As Boolean
        ' Validate email
        If Not String.IsNullOrEmpty(txtEmail.Text) AndAlso Not txtEmail.Text.EndsWith("@gmail.com") Then
            MessageBox.Show("Email must be a valid @gmail.com address.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            txtEmail.Focus()
            Return False
        End If

        ' Validate phone (must start with 09 and be 11 characters)
        If Not String.IsNullOrEmpty(txtPhone.Text) Then
            If Not txtPhone.Text.StartsWith("09") OrElse txtPhone.Text.Length <> 11 OrElse Not IsNumeric(txtPhone.Text) Then
                MessageBox.Show("Phone number must start with '09' and be exactly 11 digits.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtPhone.Focus()
                Return False
            End If
        End If

        ' Validate password (must be 8 characters if provided)
        If Not String.IsNullOrEmpty(txtNewPassword.Text) Then
            If txtNewPassword.Text.Length < 8 Then
                MessageBox.Show("Password must be at least 8 characters long.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtNewPassword.Focus()
                Return False
            End If

            ' Check if passwords match
            If txtNewPassword.Text <> txtConfirmPassword.Text Then
                MessageBox.Show("Passwords do not match.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtConfirmPassword.Focus()
                Return False
            End If
        End If

        Return True
    End Function

    Private Sub ShowPinConfirmationDialog(onSuccess As Action)
        Dim pinDialog As New Form()
        pinDialog.Text = "PIN Confirmation"
        pinDialog.Size = New Size(320, 220)
        pinDialog.StartPosition = FormStartPosition.CenterParent
        pinDialog.BackColor = System.Drawing.Color.FromArgb(41, 44, 45)
        pinDialog.FormBorderStyle = FormBorderStyle.FixedDialog
        pinDialog.MaximizeBox = False
        pinDialog.MinimizeBox = False

        Dim lblPrompt As New Label()
        lblPrompt.Text = "Enter your PIN to confirm changes:"
        lblPrompt.ForeColor = Color.White
        lblPrompt.Font = New Font("Poppins", 10)
        lblPrompt.AutoSize = True
        lblPrompt.Location = New Point(20, 20)

        Dim txtPin As New TextBox()
        txtPin.PasswordChar = "●"c
        txtPin.MaxLength = 4
        txtPin.Location = New Point(20, 50)
        txtPin.Size = New Size(260, 30)
        txtPin.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        txtPin.ForeColor = Color.White
        txtPin.Font = New Font("Poppins", 12)

        Dim btnConfirm As New Button()
        btnConfirm.Text = "Confirm"
        btnConfirm.Location = New Point(20, 100)
        btnConfirm.Size = New Size(100, 35)
        btnConfirm.BackColor = System.Drawing.Color.FromArgb(255, 204, 77)
        btnConfirm.ForeColor = Color.Black
        btnConfirm.Font = New Font("Poppins", 10)
        btnConfirm.FlatStyle = FlatStyle.Flat

        Dim btnCancel As New Button()
        btnCancel.Text = "Cancel"
        btnCancel.Location = New Point(140, 100)
        btnCancel.Size = New Size(100, 35)
        btnCancel.BackColor = Color.Gray
        btnCancel.ForeColor = Color.White
        btnCancel.Font = New Font("Poppins", 10)
        btnCancel.FlatStyle = FlatStyle.Flat

        AddHandler btnConfirm.Click, Sub()
                                         If txtPin.Text = currentUserData("PIN").ToString() Then
                                             pinDialog.Close()
                                             onSuccess.Invoke()
                                         Else
                                             MessageBox.Show("Invalid PIN.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                             txtPin.Clear()
                                             txtPin.Focus()
                                         End If
                                     End Sub

        AddHandler btnCancel.Click, Sub() pinDialog.Close()

        ' Allow Enter key to confirm
        AddHandler txtPin.KeyDown, Sub(sender As Object, e As KeyEventArgs)
                                       If e.KeyCode = Keys.Enter Then
                                           btnConfirm.PerformClick()
                                       End If
                                   End Sub

        pinDialog.Controls.AddRange({lblPrompt, txtPin, btnConfirm, btnCancel})
        txtPin.Focus()
        pinDialog.ShowDialog(Me)
    End Sub

    Private Sub SaveUserChanges()
        Try
            Dim updateQuery As New Text.StringBuilder("UPDATE Users SET ")
            Dim parameters As New List(Of SqlParameter)
            Dim updates As New List(Of String)
            Dim changedFields As New List(Of String)

            ' Track what fields are being changed for audit logging
            If txtUserName.Text <> currentUserData("Username").ToString() Then
                updates.Add("Username = @Username")
                parameters.Add(New SqlParameter("@Username", txtUserName.Text))
                changedFields.Add($"Username changed from '{currentUserData("Username")}' to '{txtUserName.Text}'")
            End If

            If txtEmail.Text <> currentUserData("Email").ToString() Then
                updates.Add("Email = @Email")
                parameters.Add(New SqlParameter("@Email", txtEmail.Text))
                changedFields.Add($"Email changed from '{currentUserData("Email")}' to '{txtEmail.Text}'")
            End If

            If txtPhone.Text <> currentUserData("Phone").ToString() Then
                updates.Add("Phone = @Phone")
                parameters.Add(New SqlParameter("@Phone", txtPhone.Text))
                changedFields.Add($"Phone changed from '{currentUserData("Phone")}' to '{txtPhone.Text}'")
            End If

            If Not String.IsNullOrEmpty(txtNewPassword.Text) Then
                ' Hash the new password using the project's hash helper
                Dim hashed As String = frmLoginvb.HashPassword(txtNewPassword.Text)
                updates.Add("PasswordHash = @PasswordHash")
                parameters.Add(New SqlParameter("@PasswordHash", hashed))
                changedFields.Add("Password updated")
            End If

            ' Add profile picture update if changed
            If hasProfileImageChanged AndAlso pendingProfileImageBytes IsNot Nothing Then
                updates.Add("Photo = @Photo")
                parameters.Add(New SqlParameter("@Photo", pendingProfileImageBytes))
                changedFields.Add("Profile picture updated")
            End If

            If updates.Count = 0 Then
                MessageBox.Show("No changes to save.", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            ' Complete the query
            updateQuery.Append(String.Join(", ", updates))
            updateQuery.Append(" WHERE UserID = @UserID")
            parameters.Add(New SqlParameter("@UserID", currentUserData("UserID")))

            ' Execute update
            Dim rowsAffected As Integer = Utilities.ExecuteNonQuery(updateQuery.ToString(), parameters.ToArray())

            If rowsAffected > 0 Then
                MessageBox.Show("Profile updated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

                ' Log changes including profile picture if it was updated
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Profile Information Updated", String.Join("; ", changedFields))
                End If

                ' Update logged in username if it was changed
                If txtUserName.Text <> currentUserData("Username").ToString() Then
                    frmLoginvb.LoggedInUsername = txtUserName.Text
                End If

                ' Update current user data with new photo if changed
                If hasProfileImageChanged AndAlso pendingProfileImageBytes IsNot Nothing Then
                    currentUserData("Photo") = pendingProfileImageBytes
                End If

                ' Reset pending changes
                hasProfileImageChanged = False
                pendingProfileImage = Nothing
                pendingProfileImageBytes = Nothing

                ' Reload user data
                LoadCurrentUserData()

                ' Clear password fields
                txtNewPassword.Text = ""
                txtConfirmPassword.Text = ""
            Else
                MessageBox.Show("Failed to update profile.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

        Catch ex As Exception
            MessageBox.Show($"Error saving changes: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub lblChangePin_Click(sender As Object, e As EventArgs) Handles lblChangePin.Click
        ShowPinChangePanel()

        ' Update visual state of side panel
        lblProfileSettings.BackColor = System.Drawing.Color.Transparent
        lblChangePin.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        lblLogout.BackColor = System.Drawing.Color.Transparent

        ' Update form title
        Me.Text = "Profile Settings - Change PIN"
    End Sub

    Private Sub ShowPinChangePanel()
        ' Store original controls if not already stored
        If originalPanel1Controls Is Nothing OrElse originalPanel1Controls.Count = 0 Then
            originalPanel1Controls = New List(Of Control)(Guna2Panel1.Controls.Cast(Of Control)())
        End If

        ' Clear panel
        Guna2Panel1.Controls.Clear()
        pinPanelActive = True
        pinChangeStep = 0
        pinInput = ""
        oldPinInput = ""
        newPinInput = ""
        confirmPinInput = ""

        CreatePinChangeLayout("Enter your current PIN")

        ' Ensure form can receive keyboard input
        Me.Focus()
        Me.Select()
    End Sub

    Private Sub CreatePinChangeLayout(promptText As String)
        ' Title
        Dim lblTitle As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblTitle.Text = promptText
        lblTitle.Font = New Font("Poppins", 16.0F, FontStyle.Regular)
        lblTitle.ForeColor = Color.White
        lblTitle.AutoSize = True
        lblTitle.Location = New Point((Guna2Panel1.Width - 400) / 2, 50)
        Guna2Panel1.Controls.Add(lblTitle)

        ' Back button with improved functionality
        Dim btnBack As New Guna.UI2.WinForms.Guna2CircleButton()
        btnBack.Text = "←"
        btnBack.Font = New Font("Poppins", 16.0F, FontStyle.Bold)
        btnBack.Size = New Size(50, 50)
        btnBack.FillColor = System.Drawing.Color.FromArgb(61, 65, 66)
        btnBack.BackColor = Color.FromArgb(41, 40, 45)
        btnBack.ForeColor = Color.White
        btnBack.Location = New Point(30, 30)
        AddHandler btnBack.Click, Sub() RestorePanel1Controls()
        Guna2Panel1.Controls.Add(btnBack)

        ' PIN indicators
        pinIndicators = New List(Of Guna.UI2.WinForms.Guna2CircleButton)()
        Dim indicatorSize As Integer = 25
        Dim indicatorSpacing As Integer = 20
        Dim indicatorStartX As Integer = (Guna2Panel1.Width - (indicatorSize * 4 + indicatorSpacing * 3)) / 2

        For i = 0 To 3
            Dim indicator As New Guna.UI2.WinForms.Guna2CircleButton()
            indicator.Size = New Size(indicatorSize, indicatorSize)
            indicator.FillColor = System.Drawing.Color.FromArgb(61, 65, 66)
            indicator.Location = New Point(indicatorStartX + i * (indicatorSize + indicatorSpacing), 150)
            indicator.BackColor = Color.FromArgb(41, 40, 45)
            pinIndicators.Add(indicator)
            Guna2Panel1.Controls.Add(indicator)
        Next

        ' Numeric keypad
        pinPanelButtons = New List(Of Guna.UI2.WinForms.Guna2Button)()
        Dim buttonSize As Integer = 80
        Dim buttonSpacing As Integer = 15
        Dim buttonStartX As Integer = (Guna2Panel1.Width - (buttonSize * 3 + buttonSpacing * 2)) / 2
        Dim buttonStartY As Integer = 200

        Dim buttonLabels() As String = {"1", "2", "3", "4", "5", "6", "7", "8", "9", "×", "0", "✓"}

        For i = 0 To buttonLabels.Length - 1
            Dim button As New Guna.UI2.WinForms.Guna2Button()
            button.Size = New Size(buttonSize, buttonSize)
            button.BorderRadius = 15
            button.FillColor = System.Drawing.Color.FromArgb(61, 65, 66)
            button.ForeColor = Color.White
            button.Font = New Font("Poppins", 16.0F, FontStyle.Bold)
            button.Text = buttonLabels(i)
            button.BackColor = Color.FromArgb(41, 40, 45)
            Dim row = i \ 3
            Dim col = i Mod 3
            button.Location = New Point(buttonStartX + col * (buttonSize + buttonSpacing),
                                      buttonStartY + row * (buttonSize + buttonSpacing) + 20)

            ' Special colors for special buttons
            If button.Text = "×" Then
                button.FillColor = System.Drawing.Color.FromArgb(255, 100, 100)
            ElseIf button.Text = "✓" Then
                button.FillColor = System.Drawing.Color.FromArgb(100, 255, 100)
            End If

            ' Add click handler
            AddHandler button.Click, Sub(sender As Object, e As EventArgs)
                                         HandlePinButtonClick(CType(sender, Guna.UI2.WinForms.Guna2Button).Text)
                                         ' Return focus to form to maintain keyboard handling
                                         Me.Focus()
                                     End Sub

            Guna2Panel1.Controls.Add(button)
            pinPanelButtons.Add(button)
        Next

        ' Add step indicator
        Dim lblStep As New Guna.UI2.WinForms.Guna2HtmlLabel()
        Select Case pinChangeStep
            Case 0
                lblStep.Text = "Step 1 of 3: Current PIN"
            Case 1
                lblStep.Text = "Step 2 of 3: New PIN"
            Case 2
                lblStep.Text = "Step 3 of 3: Confirm New PIN"
        End Select
        lblStep.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        lblStep.ForeColor = Color.Gray
        lblStep.AutoSize = True
        lblStep.Location = New Point((Guna2Panel1.Width - 200) / 2, 110)
        Guna2Panel1.Controls.Add(lblStep)
    End Sub

    Private Sub HandlePinButtonClick(buttonText As String)
        Select Case buttonText
            Case "×"
                ' Backspace
                If pinInput.Length > 0 Then
                    pinInput = pinInput.Substring(0, pinInput.Length - 1)
                    pinIndicators(pinInput.Length).FillColor = System.Drawing.Color.FromArgb(61, 65, 66)
                End If

            Case "✓"
                ' Confirm/Submit
                If pinInput.Length = 4 Then
                    ProcessPinInput()
                End If

            Case Else
                ' Numeric input
                If pinInput.Length < 4 AndAlso Char.IsDigit(buttonText(0)) Then
                    pinInput &= buttonText
                    pinIndicators(pinInput.Length - 1).FillColor = System.Drawing.Color.FromArgb(255, 204, 77)
                End If
        End Select
    End Sub

    Private Sub ProcessPinInput()
        Select Case pinChangeStep
            Case 0
                ' Verify current PIN
                If pinInput = currentUserData("PIN").ToString() Then
                    oldPinInput = pinInput
                    pinInput = ""
                    pinChangeStep = 1
                    Guna2Panel1.Controls.Clear()
                    CreatePinChangeLayout("Enter your new PIN")
                Else
                    MessageBox.Show("Incorrect current PIN.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    ResetPinInput()
                End If

            Case 1
                ' Set new PIN
                newPinInput = pinInput
                pinInput = ""
                pinChangeStep = 2
                Guna2Panel1.Controls.Clear()
                CreatePinChangeLayout("Confirm your new PIN")

            Case 2
                ' Confirm new PIN
                confirmPinInput = pinInput
                If newPinInput = confirmPinInput Then
                    ' Save new PIN to database
                    SaveNewPin(newPinInput)
                Else
                    MessageBox.Show("PINs do not match. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    pinInput = ""
                    pinChangeStep = 1
                    Guna2Panel1.Controls.Clear()
                    CreatePinChangeLayout("Enter your new PIN")
                End If
        End Select
    End Sub

    Private Sub SaveNewPin(newPin As String)
        Try
            Dim query As String = "UPDATE Users SET pin = @PIN WHERE UserID = @UserID"
            Dim parameters As SqlParameter() = {
                New SqlParameter("@PIN", newPin),
                New SqlParameter("@UserID", currentUserData("UserID"))
            }

            Dim rowsAffected As Integer = Utilities.ExecuteNonQuery(query, parameters)

            If rowsAffected > 0 Then
                MessageBox.Show("PIN changed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

                ' Log PIN change
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "PIN Changed", "User successfully changed their PIN")
                End If

                ' Update current user data
                currentUserData("PIN") = newPin

                ' Restore original panel
                RestorePanel1Controls()
            Else
                MessageBox.Show("Failed to change PIN.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

        Catch ex As Exception
            MessageBox.Show($"Error changing PIN: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ResetPinInput()
        pinInput = ""
        For Each indicator In pinIndicators
            indicator.FillColor = System.Drawing.Color.FromArgb(61, 65, 66)
        Next
    End Sub

    Private Sub RestorePanel1Controls()
        Try
            ' Clear current controls
            Guna2Panel1.Controls.Clear()

            ' Restore original controls if they exist
            If originalPanel1Controls IsNot Nothing AndAlso originalPanel1Controls.Count > 0 Then
                ' Create a new list to avoid modification during enumeration
                Dim controlsToRestore As New List(Of Control)
                For Each ctrl In originalPanel1Controls
                    If ctrl IsNot Nothing AndAlso Not ctrl.IsDisposed Then
                        controlsToRestore.Add(ctrl)
                    End If
                Next

                ' Add controls back to panel
                For Each ctrl In controlsToRestore
                    Try
                        Guna2Panel1.Controls.Add(ctrl)
                    Catch ex As Exception
                        ' Skip controls that can't be added
                        Continue For
                    End Try
                Next
            End If

            ' Reset PIN panel state
            pinPanelActive = False

            ' Update visual state of side panel
            lblProfileSettings.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
            lblChangePin.BackColor = System.Drawing.Color.Transparent
            lblLogout.BackColor = System.Drawing.Color.Transparent

            ' Update form title
            Me.Text = "Profile Settings - Personal Information"

            ' Ensure panel is visible
            Guna2Panel1.Visible = True
            Guna2Panel1.BringToFront()

            ' Refresh the data to ensure everything is current
            If currentUserData IsNot Nothing Then
                PopulateFormFields()
            End If

            ' Return focus to form for keyboard handling
            Me.Focus()

        Catch ex As Exception
            MessageBox.Show($"Error restoring panel: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ' Enhanced keyboard input handling for PIN panel
    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        ' Only handle keyboard input when PIN panel is active
        If pinPanelActive AndAlso pinPanelButtons IsNot Nothing AndAlso pinPanelButtons.Count > 0 Then
            Select Case keyData
                Case Keys.D0, Keys.NumPad0
                    HandlePinButtonClick("0")
                    Return True
                Case Keys.D1, Keys.NumPad1
                    HandlePinButtonClick("1")
                    Return True
                Case Keys.D2, Keys.NumPad2
                    HandlePinButtonClick("2")
                    Return True
                Case Keys.D3, Keys.NumPad3
                    HandlePinButtonClick("3")
                    Return True
                Case Keys.D4, Keys.NumPad4
                    HandlePinButtonClick("4")
                    Return True
                Case Keys.D5, Keys.NumPad5
                    HandlePinButtonClick("5")
                    Return True
                Case Keys.D6, Keys.NumPad6
                    HandlePinButtonClick("6")
                    Return True
                Case Keys.D7, Keys.NumPad7
                    HandlePinButtonClick("7")
                    Return True
                Case Keys.D8, Keys.NumPad8
                    HandlePinButtonClick("8")
                    Return True
                Case Keys.D9, Keys.NumPad9
                    HandlePinButtonClick("9")
                    Return True
                Case Keys.Back, Keys.Delete
                    HandlePinButtonClick("×")
                    Return True
                Case Keys.Enter
                    HandlePinButtonClick("✓")
                    Return True
                Case Keys.Escape
                    RestorePanel1Controls()
                    Return True
            End Select
        End If
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ' Additional KeyDown event handler for extra reliability
    Private Sub ProfileSettings_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        ' Only handle when PIN panel is active
        If pinPanelActive Then
            Select Case e.KeyCode
                Case Keys.D0, Keys.NumPad0
                    HandlePinButtonClick("0")
                    e.Handled = True
                Case Keys.D1, Keys.NumPad1
                    HandlePinButtonClick("1")
                    e.Handled = True
                Case Keys.D2, Keys.NumPad2
                    HandlePinButtonClick("2")
                    e.Handled = True
                Case Keys.D3, Keys.NumPad3
                    HandlePinButtonClick("3")
                    e.Handled = True
                Case Keys.D4, Keys.NumPad4
                    HandlePinButtonClick("4")
                    e.Handled = True
                Case Keys.D5, Keys.NumPad5
                    HandlePinButtonClick("5")
                    e.Handled = True
                Case Keys.D6, Keys.NumPad6
                    HandlePinButtonClick("6")
                    e.Handled = True
                Case Keys.D7, Keys.NumPad7
                    HandlePinButtonClick("7")
                    e.Handled = True
                Case Keys.D8, Keys.NumPad8
                    HandlePinButtonClick("8")
                    e.Handled = True
                Case Keys.D9, Keys.NumPad9
                    HandlePinButtonClick("9")
                    e.Handled = True
                Case Keys.Back, Keys.Delete
                    HandlePinButtonClick("×")
                    e.Handled = True
                Case Keys.Enter
                    HandlePinButtonClick("✓")
                    e.Handled = True
                Case Keys.Escape
                    RestorePanel1Controls()
                    e.Handled = True
            End Select
        End If
    End Sub

    ' Navigation methods

    Private Sub InitializeSidePanelEvents()
        ' Add click events for side panel options
        AddHandler lblProfileSettings.Click, Sub() ShowProfileSettingsPanel()
        AddHandler lblChangePin.Click, AddressOf lblChangePin_Click
        AddHandler lblLogout.Click, AddressOf lblLogout_Click ' Use the proper event handler instead of inline Sub

        ' Add hover effects
        AddHoverEffect(lblProfileSettings)
        AddHoverEffect(lblChangePin)
        AddHoverEffect(lblLogout)

        ' Set initial visual state - Profile Settings should be highlighted by default
        lblProfileSettings.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
        lblChangePin.BackColor = System.Drawing.Color.Transparent
        lblLogout.BackColor = System.Drawing.Color.Transparent
    End Sub

    Private Sub AddHoverEffect(lbl As Label)
        AddHandler lbl.MouseEnter, Sub()
                                       If Not (lbl Is lblProfileSettings AndAlso Not pinPanelActive) Then
                                           lbl.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
                                       End If
                                   End Sub
        AddHandler lbl.MouseLeave, Sub()
                                       ' Keep Profile Settings highlighted when in profile mode
                                       If lbl Is lblProfileSettings AndAlso Not pinPanelActive Then
                                           lbl.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
                                       ElseIf lbl IsNot lblProfileSettings Then
                                           lbl.BackColor = System.Drawing.Color.Transparent
                                       Else
                                           lbl.BackColor = System.Drawing.Color.Transparent
                                       End If
                                   End Sub
        lbl.Cursor = Cursors.Hand
    End Sub

    ' Handle form closing
    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ' Skip modal dialogs if navigating programmatically
        If isNavigating Then
            MyBase.OnFormClosing(e)
            Return
        End If

        If pinPanelActive Then
            RestorePanel1Controls()
        End If

        MyBase.OnFormClosing(e)
    End Sub

    ' Modified profile picture click handler - now only selects image, doesn't save
    Private Sub Guna2CirclePictureBox7_Click(sender As Object, e As EventArgs) Handles Guna2CirclePictureBox7.Click
        Try
            ' Open file dialog to select an image
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp"
                openFileDialog.Title = "Select Profile Picture"

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    ' Load the selected image
                    Dim selectedImage As Image = Image.FromFile(openFileDialog.FileName)

                    ' Convert the image to a byte array for storage
                    Using ms As New MemoryStream()
                        selectedImage.Save(ms, ImageFormat.Png)
                        pendingProfileImageBytes = ms.ToArray()
                    End Using

                    ' Update the display immediately but don't save to database yet
                    Guna2CirclePictureBox7.Image = selectedImage
                    Guna2CirclePictureBox7.SizeMode = PictureBoxSizeMode.Zoom

                    ' Store the image and mark as changed
                    pendingProfileImage = selectedImage
                    hasProfileImageChanged = True

                    ' Show a message to inform the user
                    MessageBox.Show("Profile picture selected. Click 'Save Changes' to save your new profile picture.", "Image Selected", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error selecting profile picture: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' --- NEW: Navigation/menu helpers to match other pages (minimal, safe) ---
    Private Sub InitializeNavigation()
        Try
            CreateNavigationMenu()
            InitializeProfileSection()
        Catch
            ' ignore to avoid breaking form load
        End Try
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

            ' Set Navigation Panel Background to the new dark navigation color (61,65,66)
            DashboardPanel.FillColor = System.Drawing.Color.FromArgb(61, 65, 66)

            ' Calculate available space (DashboardPanel is 236x885)
            Dim availableWidth As Integer = DashboardPanel.Width - 40 ' 20px margins on each side
            Dim availableHeight As Integer = DashboardPanel.Height - 160 ' Space for logo and title

            ' Logo area (keep existing PictureBox9)
            If PictureBox9 IsNot Nothing Then
                Try
                    ' Render company logo from settings into the existing PictureBox.
                    ' Do NOT change PictureBox size or add click handlers.
                    Dim logoImg As Image = CompanySettingsManager.Instance.GetCompanyLogo()
                    If logoImg IsNot Nothing Then
                        PictureBox9.Image = logoImg
                        PictureBox9.SizeMode = PictureBoxSizeMode.StretchImage
                    End If
                Catch ex As Exception
                    Console.WriteLine($"Unable to set dashboard logo: {ex.Message}")
                End Try

                PictureBox9.BringToFront()
            End If

            ' UPDATED: Get company name from settings
            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")

            ' Add title label - positioned below logo with Golden Yellow
            Dim titleLabel As New Label()
            titleLabel.Text = companyName
            titleLabel.Font = New Font("Poppins", 14, FontStyle.Bold)
            titleLabel.ForeColor = GoldenYellow
            titleLabel.BackColor = Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New Size(availableWidth, 30)
            titleLabel.Location = New Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(titleLabel)

            ' Subtitle with LightSilver (visible on dark nav background)
            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Poppins", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = LightSilver
            subtitleLabel.BackColor = Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New Size(availableWidth, 25)
            subtitleLabel.Location = New Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            DashboardPanel.Controls.Add(subtitleLabel)

            ' Navigation section separator with a subtle darker line
            Dim separator1 As New Panel()
            separator1.BackColor = System.Drawing.Color.FromArgb(50, 50, 50)
            separator1.Size = New System.Drawing.Size(availableWidth - 20, 2)
            separator1.Location = New Point(30, 190)
            DashboardPanel.Controls.Add(separator1)

            ' Navigation section label with LightSilver (visible on dark background)
            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Poppins", 10, FontStyle.Bold)
            navLabel.ForeColor = LightSilver
            navLabel.BackColor = Color.Transparent
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

            ' Dashboard Button (not active)
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then

                Dim navDashboardBtn = CreateLargeNavButton("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navDashboardBtn.Click, AddressOf NavDashboard_Click
                buttonIndex += 1
            End If
            ' POS/Sales Button (not active here — this form is ProfileSettings)
            Dim navPOSBtn = CreateLargeNavButton("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
            AddHandler navPOSBtn.Click, AddressOf NavPOS_Click
            buttonIndex += 1

            ' Manager and Admin only buttons - Inventory moved here
            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                Dim navInventoryBtn = CreateLargeNavButton("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryBtn.Click, AddressOf NavInventory_Click
                buttonIndex += 1

                Dim navSalesRecordsBtn = CreateLargeNavButton("📊 Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navSalesRecordsBtn.Click, AddressOf NavSalesRecords_Click
                buttonIndex += 1

                Dim navStaffBtn = CreateLargeNavButton("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navStaffBtn.Click, AddressOf NavStaff_Click
                buttonIndex += 1

                Dim navInventoryLogBtn = CreateLargeNavButton("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navInventoryLogBtn.Click, AddressOf NavInventoryLog_Click
                buttonIndex += 1

                Dim navSuppliersBtn = CreateLargeNavButton("🏷️ Suppliers", startY + buttonIndex * (buttonHeight + buttonSpacing), False, buttonWidth, buttonHeight)
                AddHandler navSuppliersBtn.Click, AddressOf NavSuppliers_Click
                buttonIndex += 1
            End If

            ' Admin only buttons
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

    ' Add Nav handler for POS / Sales navigation
    Private Sub NavPOS_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            ' Show the Sales form (POS)
            Sales.Show()
            Me.Close()
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open POS / Sales: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub NavSystemSettings_Click(sender As Object, e As EventArgs)
        isNavigating = True
        Sys.Show()
        Me.Close()
    End Sub
    Private Sub NavSuppliers_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True
            Supplier.Show()
            Me.Close()
        Catch ex As Exception
            MessageBox.Show($"Unable to open Suppliers: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    Private Sub NavAuditLog_Click(sender As Object, e As EventArgs)
        isNavigating = True
        AuditLog.Show()
        Me.Close()
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

    Private Sub NavSalesRecords_Click(sender As Object, e As EventArgs)
        Try
            isNavigating = True



            ' Open SalesRecord form
            Dim salesRecordForm As New SalesRecord()
            salesRecordForm.Show()

            ' Close current form
            Me.Close()
        Catch ex As Exception
            isNavigating = False
            MessageBox.Show($"Unable to open Sales Records: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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

    Private Function CreateLargeNavButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer) As Guna.UI2.WinForms.Guna2Button
        Dim btn As New Guna.UI2.WinForms.Guna2Button()

        ' Button properties with improved sizing and new color scheme for dark navigation
        btn.Text = text
        btn.Size = New System.Drawing.Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Poppins", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left

        ' Apply color scheme for dark navigation panel (idle = transparent, text = white)
        btn.FillColor = If(isActive, GoldenYellow, System.Drawing.Color.Transparent) ' Golden for active
        btn.ForeColor = If(isActive, DeepCharcoal, PureWhite) ' Dark text on active gold, white on dark background when inactive
        btn.BorderThickness = If(isActive, 0, 1)
        btn.BorderColor = If(isActive, System.Drawing.Color.Transparent, System.Drawing.Color.FromArgb(80, 80, 80)) ' subtle border on dark bg
        btn.BackColor = System.Drawing.Color.Transparent
        btn.Cursor = Cursors.Hand

        ' Add subtle shadow for depth (tuned for dark nav)
        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = System.Drawing.Color.FromArgb(30, 30, 30)
        btn.ShadowDecoration.Depth = 4
        btn.ShadowDecoration.Shadow = New Padding(0, 1, 4, 4)

        ' Improved hover effects for dark navigation
        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = System.Drawing.Color.FromArgb(48, 52, 54) ' slightly lighter than nav bg
                                           btn.BorderColor = GoldenYellow
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

        ' Add to panel
        DashboardPanel.Controls.Add(btn)

        Return btn
    End Function
    Private Sub InitializeProfileSection()
        Try
            ' Update a small profile area in nav if present
            If lblUsername IsNot Nothing Then
                lblUsername.Text = frmLoginvb.LoggedInUsername
                lblUsername.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
                lblUsername.ForeColor = System.Drawing.Color.White
            End If

            ' Ensure nav shows company logo in PictureBox9. Do NOT overwrite it with user photo.
            If PictureBox9 IsNot Nothing Then
                Try
                    Dim companyLogo As Image = Nothing
                    Try
                        companyLogo = CompanySettingsManager.Instance.GetCompanyLogo()
                    Catch
                        companyLogo = Nothing
                    End Try

                    If companyLogo IsNot Nothing Then
                        PictureBox9.Image = companyLogo
                        PictureBox9.SizeMode = PictureBoxSizeMode.StretchImage
                    End If
                Catch ex As Exception
                    Console.WriteLine($"InitializeProfileSection (logo) error: {ex.Message}")
                End Try
            End If

            ' Place the user avatar into the Guna circle picture box (do NOT touch PictureBox9).
            Try
                Dim userImg As Image = Nothing
                If currentUserData Is Nothing Then
                    LoadCurrentUserData()
                End If

                If currentUserData IsNot Nothing AndAlso currentUserData("Photo") IsNot Nothing Then
                    Dim photoBytes As Byte() = CType(currentUserData("Photo"), Byte())
                    Using ms As New MemoryStream(photoBytes)
                        userImg = Image.FromStream(ms)
                    End Using
                Else
                    userImg = CreateDefaultAvatar(If(currentUserData IsNot Nothing, currentUserData("Username").ToString(), frmLoginvb.LoggedInUsername))
                End If

                If Guna2CirclePictureBox1 IsNot Nothing AndAlso userImg IsNot Nothing Then
                    Guna2CirclePictureBox1.Image = userImg
                    Guna2CirclePictureBox1.SizeMode = PictureBoxSizeMode.Zoom
                End If
            Catch
                ' ignore avatar failures
            End Try

        Catch
            ' ignore
        End Try
    End Sub

    Private Sub lblLogout_Click(sender As Object, e As EventArgs) Handles lblLogout.Click
        Try
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result = DialogResult.Yes Then
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Log Out", "User logged out of the application.")
                End If
                frmLoginvb.LogoutUser()
                isNavigating = True
                Me.Close()
                Dim loginForm As New frmLoginvb()
                loginForm.Show()
            End If
        Catch ex As Exception
            MessageBox.Show($"Error during logout: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ProfileSettings_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)

        ' If this is programmatic navigation (like logout), don't show confirmation
        If isNavigating Then
            Return
        End If

        ' Only show confirmation for user-initiated close (X button)
        If e.CloseReason = CloseReason.UserClosing Then
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to exit the application?", "Exit Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ' Log the exit action
                If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                    Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Application Exit", "User exited the application via Profile Settings form")
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
End Class