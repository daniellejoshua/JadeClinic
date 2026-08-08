Imports System.Data.Common
Imports System.IO

Public Module ProfileManager
    Private profileDropdownPanels As New Dictionary(Of Form, Panel)()
    Private dropdownParents As New Dictionary(Of Form, Control)()
    Private isDropdownVisible As New Dictionary(Of Form, Boolean)()
    Private navigateCallbacks As New Dictionary(Of Form, Action)()

    Public Function IsProfileDropdownVisible(form As Form) As Boolean
        Return isDropdownVisible.ContainsKey(form) AndAlso isDropdownVisible(form)
    End Function

    Public Sub InitializeProfile(form As Form, lblUsername As Control, profilePic As PictureBox, navigateToProfileSettings As Action)
        Try
            If lblUsername IsNot Nothing Then
                lblUsername.Text = frmLoginvb.LoggedInUsername
                lblUsername.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
                lblUsername.ForeColor = Color.FromArgb(51, 51, 51)
            End If

            LoadUserProfilePicture(form, profilePic)
            navigateCallbacks(form) = navigateToProfileSettings

            If profilePic IsNot Nothing Then
                AddHandler profilePic.Click, Sub() ToggleProfileDropdown(form, profilePic)
                AddHandler profilePic.MouseEnter, Sub() profilePic.Cursor = Cursors.Hand
            End If

            If lblUsername IsNot Nothing Then
                AddHandler lblUsername.Click, Sub() ToggleProfileDropdown(form, profilePic)
                AddHandler lblUsername.MouseEnter, Sub() lblUsername.Cursor = Cursors.Hand
            End If

        Catch ex As Exception
            If lblUsername IsNot Nothing Then
                lblUsername.Text = frmLoginvb.LoggedInUsername
            End If
        End Try
    End Sub

    Public Sub LoadUserProfilePicture(form As Form, profilePic As PictureBox)
        Try
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) AndAlso profilePic IsNot Nothing Then
                Dim query As String = "SELECT PhotoPath FROM Users WHERE Username = @Username"
                Dim parameters As SqlParameter() = {
                    New SqlParameter("@Username", frmLoginvb.LoggedInUsername)
                }

                Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
                    If reader.Read() Then
                        profilePic.SizeMode = PictureBoxSizeMode.Zoom
                        profilePic.BorderStyle = BorderStyle.None

                        If Not IsDBNull(reader("PhotoPath")) Then
                            Dim photoPath As String = reader("PhotoPath").ToString()
                            Dim fullPath As String = Path.Combine(Connection.GetImagesFolder("users"), photoPath)
                            If IO.File.Exists(fullPath) Then
                                Dim loadedImage As Image = Image.FromFile(fullPath)
                                profilePic.Image = New Bitmap(loadedImage)
                                loadedImage.Dispose()
                            Else
                                profilePic.Image = New Bitmap(My.Resources.avatar_default_svgrepo_com)
                            End If
                        Else
                            profilePic.Image = New Bitmap(My.Resources.avatar_default_svgrepo_com)
                        End If
                    End If
                End Using
            End If
        Catch ex As Exception
            If profilePic IsNot Nothing Then
                profilePic.Image = New Bitmap(My.Resources.avatar_default_svgrepo_com)
            End If
        End Try
    End Sub

    Private Sub ToggleProfileDropdown(form As Form, profilePic As PictureBox)
        If isDropdownVisible.ContainsKey(form) AndAlso isDropdownVisible(form) Then
            HideProfileDropdown(form)
        Else
            ShowProfileDropdown(form, profilePic)
        End If
    End Sub

    Public Sub ShowProfileDropdown(form As Form, profilePic As PictureBox, Optional parentContainer As Control = Nothing, Optional beforeLogout As Action = Nothing)
        If isDropdownVisible.ContainsKey(form) AndAlso isDropdownVisible(form) Then
            HideProfileDropdown(form)
        End If

        Dim dropdown As New Panel()
        dropdown.Size = New Size(200, 100)
        dropdown.BackColor = Color.White
        dropdown.BorderStyle = BorderStyle.FixedSingle

        If profilePic IsNot Nothing Then
            Dim picLocation = profilePic.Location
            If parentContainer IsNot Nothing Then
                picLocation = parentContainer.PointToClient(profilePic.Parent.PointToScreen(profilePic.Location))
            End If
            Dim dropdownX As Integer = picLocation.X - ((dropdown.Width - profilePic.Width) \ 2)
            Dim dropdownY As Integer = picLocation.Y + profilePic.Height + 5
            dropdown.Location = New Point(Math.Max(0, dropdownX), Math.Max(0, dropdownY))
        End If

        Dim btnProfileSettings As New Label()
        btnProfileSettings.Text = "⚙️ Profile Settings"
        btnProfileSettings.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnProfileSettings.ForeColor = Color.FromArgb(51, 51, 51)
        btnProfileSettings.BackColor = Color.Transparent
        btnProfileSettings.Size = New Size(190, 40)
        btnProfileSettings.Location = New Point(5, 5)
        btnProfileSettings.TextAlign = ContentAlignment.MiddleLeft
        btnProfileSettings.Cursor = Cursors.Hand

        AddHandler btnProfileSettings.MouseEnter, Sub() btnProfileSettings.BackColor = Color.FromArgb(240, 240, 240)
        AddHandler btnProfileSettings.MouseLeave, Sub() btnProfileSettings.BackColor = Color.Transparent
        AddHandler btnProfileSettings.Click, Sub()
                                                 HideProfileDropdown(form)
                                                 If navigateCallbacks.ContainsKey(form) Then
                                                     navigateCallbacks(form).Invoke()
                                                 End If
                                             End Sub

        Dim btnLogOut As New Label()
        btnLogOut.Text = "🚪 Log Out"
        btnLogOut.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnLogOut.ForeColor = Color.FromArgb(51, 51, 51)
        btnLogOut.BackColor = Color.Transparent
        btnLogOut.Size = New Size(190, 40)
        btnLogOut.Location = New Point(5, 50)
        btnLogOut.TextAlign = ContentAlignment.MiddleLeft
        btnLogOut.Cursor = Cursors.Hand

        AddHandler btnLogOut.MouseEnter, Sub() btnLogOut.BackColor = Color.FromArgb(240, 240, 240)
        AddHandler btnLogOut.MouseLeave, Sub() btnLogOut.BackColor = Color.Transparent
        AddHandler btnLogOut.Click, Sub()
                                        Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                                        If result = DialogResult.Yes Then
                                            If beforeLogout IsNot Nothing Then
                                                beforeLogout.Invoke()
                                            End If
                                            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                                                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Log Out", "User logged out of the application.")
                                            End If
                                            frmLoginvb.LogoutUser()
                                            HideProfileDropdown(form)
                                            form.Hide()
                                            Dim loginForm As New frmLoginvb()
                                            loginForm.Show()
                                        End If
                                    End Sub

        dropdown.Controls.Add(btnProfileSettings)
        dropdown.Controls.Add(btnLogOut)

        Dim parent As Control = If(parentContainer, form)
        parent.Controls.Add(dropdown)
        dropdown.BringToFront()

        AddHandler parent.Click, Sub(s, e) HideProfileDropdown(form)

        profileDropdownPanels(form) = dropdown
        dropdownParents(form) = parent
        isDropdownVisible(form) = True
    End Sub

    Public Sub HideProfileDropdown(form As Form)
        If profileDropdownPanels.ContainsKey(form) AndAlso profileDropdownPanels(form) IsNot Nothing Then
            Dim parent As Control = form
            If dropdownParents.ContainsKey(form) AndAlso dropdownParents(form) IsNot Nothing Then
                parent = dropdownParents(form)
            End If
            parent.Controls.Remove(profileDropdownPanels(form))
            profileDropdownPanels(form).Dispose()
            profileDropdownPanels(form) = Nothing
            dropdownParents(form) = Nothing
        End If
        isDropdownVisible(form) = False
    End Sub
End Module
