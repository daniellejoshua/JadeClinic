Imports Guna.UI2.WinForms
Imports System.Windows.Forms

Public Module NavigationModule
    ' Navigation panel reference
    Private currentNavigationPanel As Guna2Panel = Nothing
    Private currentForm As Form = Nothing

    ' Color constants using JadeClinic palette
    Private ReadOnly DeepCharcoal As Color = Color.FromArgb(26, 29, 31)      ' #1A1D1F
    Private ReadOnly DarkSlate As Color = Color.FromArgb(43, 47, 50)         ' #2B2F32
    Private ReadOnly Graphite As Color = Color.FromArgb(61, 65, 69)          ' #3D4145
    Private ReadOnly SteelGray As Color = Color.FromArgb(74, 79, 84)         ' #4A4F54
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)      ' #FFFFFF
    Private ReadOnly LightSilver As Color = Color.FromArgb(225, 229, 233)    ' #E1E5E9
    Private ReadOnly SoftGray As Color = Color.FromArgb(184, 188, 193)       ' #B8BCC1
    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)    ' #FECF10
    Private ReadOnly RichOlive As Color = Color.FromArgb(190, 154, 48)       ' #BE9A30
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)     ' #10D862
    Private ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)         ' #FF4757

    ' User permission levels
    Public Enum UserRole
        Staff = 1
        Manager = 2
        Admin = 3
    End Enum

    ' Navigation item structure
    Public Structure NavigationItem
        Public Text As String
        Public Icon As String
        Public FormType As Type
        Public MinRole As UserRole
        Public IsSystemFunction As Boolean

        Public Sub New(text As String, icon As String, formType As Type, minRole As UserRole, Optional isSystem As Boolean = False)
            Me.Text = text
            Me.Icon = icon
            Me.FormType = formType
            Me.MinRole = minRole
            Me.IsSystemFunction = isSystem
        End Sub
    End Structure

    ' Define all navigation items with role permissions
    Private ReadOnly NavigationItems As NavigationItem() = {
        New NavigationItem("?? Dashboard", "??", GetType(Dashboard), UserRole.Staff),
        New NavigationItem("?? POS / Sales", "??", GetType(Sales), UserRole.Staff),
        New NavigationItem("?? Inventory", "??", GetType(Inventory), UserRole.Manager),
        New NavigationItem("?? Sales Records", "??", GetType(Sales), UserRole.Manager),
        New NavigationItem("?? Staff Management", "??", GetType(Staff), UserRole.Manager),
        New NavigationItem("?? Inventory Logs", "??", GetType(InventoryLog), UserRole.Manager),
        New NavigationItem("?? Audit Logs", "??", Nothing, UserRole.Admin),
        New NavigationItem("?? System Settings", "??", Nothing, UserRole.Admin, True)
    }

    ''' <summary>
    ''' Creates role-based navigation menu for any form
    ''' </summary>
    ''' <param name="targetForm">The form to add navigation to</param>
    ''' <param name="navigationPanel">The Guna2Panel to use for navigation</param>
    ''' <param name="logoControl">Optional logo control to preserve</param>
    ''' <param name="activePageName">Name of currently active page</param>
    Public Sub CreateNavigationMenu(targetForm As Form, navigationPanel As Guna2Panel, Optional logoControl As Control = Nothing, Optional activePageName As String = "")
        Try
            currentForm = targetForm
            currentNavigationPanel = navigationPanel

            Console.WriteLine($"Creating role-based navigation for {targetForm.GetType().Name}")
            Console.WriteLine($"User role: {frmLoginvb.LoggedInRole}")

            ' Clear existing controls except logo
            ClearNavigationPanel(logoControl)

            ' Set up navigation panel styling
            SetupNavigationPanelStyling()

            ' Add header section (logo + title)
            CreateNavigationHeader(logoControl)

            ' Add navigation buttons based on user role
            CreateRoleBasedNavigation(activePageName)

            ' Add user info section at bottom
            CreateUserInfoSection()

            Console.WriteLine("Navigation menu created successfully")

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
            MessageBox.Show($"Error creating navigation: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ''' <summary>
    ''' Get user role enum from logged in role string
    ''' </summary>
    Private Function GetCurrentUserRole() As UserRole
        Try
            If String.IsNullOrEmpty(frmLoginvb.LoggedInRole) Then
                Return UserRole.Staff ' Default to lowest permission
            End If

            Select Case frmLoginvb.LoggedInRole.ToUpper()
                Case "ADMIN", "ADMINISTRATOR"
                    Return UserRole.Admin
                Case "MANAGER"
                    Return UserRole.Manager
                Case Else
                    Return UserRole.Staff
            End Select
        Catch ex As Exception
            Console.WriteLine($"Error getting user role: {ex.Message}")
            Return UserRole.Staff ' Default to lowest permission on error
        End Try
    End Function

    ''' <summary>
    ''' Clear navigation panel while preserving specified control
    ''' </summary>
    Private Sub ClearNavigationPanel(logoControl As Control)
        Try
            For i = currentNavigationPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = currentNavigationPanel.Controls(i)
                If logoControl IsNot Nothing AndAlso control Is logoControl Then
                    Continue For ' Preserve logo
                ElseIf logoControl Is Nothing AndAlso TypeOf control Is PictureBox Then
                    Continue For ' Preserve any PictureBox if no specific logo specified
                Else
                    currentNavigationPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next
        Catch ex As Exception
            Console.WriteLine($"Error clearing navigation panel: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Setup navigation panel styling
    ''' </summary>
    Private Sub SetupNavigationPanelStyling()
        currentNavigationPanel.FillColor = DarkSlate
        currentNavigationPanel.BorderRadius = 0 ' Keep clean edges for navigation
    End Sub

    ''' <summary>
    ''' Create navigation header with logo and title
    ''' </summary>
    Private Sub CreateNavigationHeader(logoControl As Control)
        Try
            Dim availableWidth As Integer = currentNavigationPanel.Width - 40

            ' Bring logo to front if it exists
            If logoControl IsNot Nothing Then
                logoControl.BringToFront()
            End If

            ' Add title label
            Dim titleLabel As New Label()
            titleLabel.Text = "JADE CLINIC"
            titleLabel.Font = New Font("Segoe UI Emoji", 14, FontStyle.Bold)
            titleLabel.ForeColor = GoldenYellow
            titleLabel.BackColor = Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New Size(availableWidth, 30)
            titleLabel.Location = New Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            currentNavigationPanel.Controls.Add(titleLabel)

            ' Add subtitle
            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Segoe UI", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = LightSilver
            subtitleLabel.BackColor = Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New Size(availableWidth, 25)
            subtitleLabel.Location = New Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            currentNavigationPanel.Controls.Add(subtitleLabel)

            ' Add separator
            Dim separator As New Panel()
            separator.BackColor = SteelGray
            separator.Size = New Size(availableWidth - 20, 2)
            separator.Location = New Point(30, 185)
            currentNavigationPanel.Controls.Add(separator)

            ' Add navigation label
            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Segoe UI", 10, FontStyle.Bold)
            navLabel.ForeColor = SoftGray
            navLabel.BackColor = Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New Size(availableWidth, 25)
            navLabel.Location = New Point(20, 200)
            navLabel.TextAlign = ContentAlignment.MiddleCenter
            currentNavigationPanel.Controls.Add(navLabel)

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation header: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Create navigation buttons based on user role
    ''' </summary>
    Private Sub CreateRoleBasedNavigation(activePageName As String)
        Try
            Dim currentRole As UserRole = GetCurrentUserRole()
            Dim availableWidth As Integer = currentNavigationPanel.Width - 40
            Dim startY As Integer = 240
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonIndex As Integer = 0

            Console.WriteLine($"Creating navigation for role: {currentRole}")

            ' Filter navigation items based on user role
            Dim allowedItems = NavigationItems.Where(Function(item) currentRole >= item.MinRole).ToArray()

            Console.WriteLine($"User has access to {allowedItems.Length} navigation items")

            ' Create buttons for allowed items
            For Each item In allowedItems
                Dim isActive As Boolean = (activePageName.ToLower() = item.Text.Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").ToLower())

                Dim navButton As Guna2Button = CreateNavigationButton(
                    item.Text,
                    startY + buttonIndex * (buttonHeight + buttonSpacing),
                    isActive,
                    availableWidth - 5,
                    buttonHeight,
                    item.FormType
                )

                buttonIndex += 1
            Next

            ' Add separator before logout
            Dim separator As New Panel()
            separator.BackColor = SteelGray
            separator.Size = New Size(availableWidth - 40, 2)
            separator.Location = New Point(40, startY + buttonIndex * (buttonHeight + buttonSpacing) + 10)
            currentNavigationPanel.Controls.Add(separator)

            ' Add logout button
            Dim logoutButton As Guna2Button = CreateLogoutButton(
                startY + buttonIndex * (buttonHeight + buttonSpacing) + 30,
                availableWidth - 5,
                buttonHeight
            )

        Catch ex As Exception
            Console.WriteLine($"Error creating role-based navigation: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Create individual navigation button
    ''' </summary>
    Private Function CreateNavigationButton(text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer, formType As Type) As Guna2Button
        Dim btn As New Guna2Button()

        ' Button properties
        btn.Text = text
        btn.Size = New Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Segoe UI", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left
        btn.Cursor = Cursors.Hand

        ' Styling based on active state
        If isActive Then
            btn.FillColor = GoldenYellow
            btn.ForeColor = DeepCharcoal
            btn.BorderThickness = 0
        Else
            btn.FillColor = Color.Transparent
            btn.ForeColor = PureWhite
            btn.BorderThickness = 1
            btn.BorderColor = SteelGray
        End If

        ' Add shadow
        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = DeepCharcoal
        btn.ShadowDecoration.Depth = 5
        btn.ShadowDecoration.Shadow = New Padding(0, 2, 5, 5)

        ' Add hover effects
        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = Graphite
                                           btn.BorderColor = RichOlive
                                           btn.Font = New Font("Segoe UI", 10, FontStyle.Bold)
                                       End If
                                   End Sub

        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = Color.Transparent
                                           btn.BorderColor = SteelGray
                                           btn.Font = New Font("Segoe UI", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        ' Add click handler
        If formType IsNot Nothing Then
            AddHandler btn.Click, Sub() NavigateToForm(formType)
        Else
            ' Handle special cases like Audit Logs or System Settings
            If text.Contains("Audit Logs") Then
                AddHandler btn.Click, Sub() MessageBox.Show("Audit Logs feature coming soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
            ElseIf text.Contains("System Settings") Then
                AddHandler btn.Click, Sub() MessageBox.Show("System Settings feature coming soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End If

        currentNavigationPanel.Controls.Add(btn)
        Return btn
    End Function

    ''' <summary>
    ''' Create logout button with special styling
    ''' </summary>
    Private Function CreateLogoutButton(yPosition As Integer, buttonWidth As Integer, buttonHeight As Integer) As Guna2Button
        Dim btn As New Guna2Button()

        btn.Text = "?? Logout"
        btn.Size = New Size(buttonWidth, buttonHeight)
        btn.Location = New Point(20, yPosition)
        btn.BorderRadius = 12
        btn.Font = New Font("Segoe UI", 10, FontStyle.Regular)
        btn.TextAlign = HorizontalAlignment.Left
        btn.Cursor = Cursors.Hand
        btn.FillColor = AlertRed
        btn.ForeColor = PureWhite
        btn.BorderThickness = 0

        ' Add shadow
        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = DeepCharcoal
        btn.ShadowDecoration.Depth = 5

        ' Add hover effect
        AddHandler btn.MouseEnter, Sub()
                                       btn.FillColor = Color.FromArgb(255, 50, 50)
                                       btn.Font = New Font("Segoe UI", 10, FontStyle.Bold)
                                   End Sub

        AddHandler btn.MouseLeave, Sub()
                                       btn.FillColor = AlertRed
                                       btn.Font = New Font("Segoe UI", 10, FontStyle.Regular)
                                   End Sub

        ' Add click handler for logout
        AddHandler btn.Click, Sub() HandleLogout()

        currentNavigationPanel.Controls.Add(btn)
        Return btn
    End Function

    ''' <summary>
    ''' Create user info section at bottom
    ''' </summary>
    Private Sub CreateUserInfoSection()
        Try
            Dim userInfoY As Integer = currentNavigationPanel.Height - 140

            ' User info panel
            Dim userInfoPanel As New Guna2Panel()
            userInfoPanel.FillColor = Graphite
            userInfoPanel.BorderRadius = 12
            userInfoPanel.Size = New Size(currentNavigationPanel.Width - 40, 100)
            userInfoPanel.Location = New Point(20, userInfoY)
            userInfoPanel.ShadowDecoration.Enabled = True
            userInfoPanel.ShadowDecoration.Color = DeepCharcoal
            userInfoPanel.ShadowDecoration.Depth = 5
            currentNavigationPanel.Controls.Add(userInfoPanel)

            ' Profile picture
            Dim profilePicture As New Guna2CircleButton()
            profilePicture.Size = New Size(50, 50)
            profilePicture.Location = New Point(15, 25)
            profilePicture.FillColor = GoldenYellow
            profilePicture.Font = New Font("Segoe UI", 18, FontStyle.Bold)
            profilePicture.ForeColor = DeepCharcoal
            profilePicture.Text = If(String.IsNullOrEmpty(frmLoginvb.LoggedInUsername), "?", frmLoginvb.LoggedInUsername.Substring(0, 1).ToUpper())
            profilePicture.BorderThickness = 2
            profilePicture.BorderColor = RichOlive
            userInfoPanel.Controls.Add(profilePicture)

            ' Welcome label
            Dim welcomeLabel As New Label()
            welcomeLabel.Text = "Welcome back!"
            welcomeLabel.Font = New Font("Segoe UI", 9, FontStyle.Regular)
            welcomeLabel.ForeColor = LightSilver
            welcomeLabel.BackColor = Color.Transparent
            welcomeLabel.Location = New Point(80, 15)
            welcomeLabel.AutoSize = True
            userInfoPanel.Controls.Add(welcomeLabel)

            ' User name
            Dim userNameLabel As New Label()
            userNameLabel.Text = If(String.IsNullOrEmpty(frmLoginvb.LoggedInUsername), "Guest User", frmLoginvb.LoggedInUsername)
            userNameLabel.Font = New Font("Segoe UI", 11, FontStyle.Bold)
            userNameLabel.ForeColor = GoldenYellow
            userNameLabel.BackColor = Color.Transparent
            userNameLabel.Location = New Point(80, 35)
            userNameLabel.AutoSize = True
            userInfoPanel.Controls.Add(userNameLabel)

            ' Role label
            Dim roleLabel As New Label()
            Dim roleText As String = If(String.IsNullOrEmpty(frmLoginvb.LoggedInRole), "Staff", frmLoginvb.LoggedInRole)
            roleLabel.Text = $"?? {roleText}"
            roleLabel.Font = New Font("Segoe UI", 9, FontStyle.Regular)
            roleLabel.ForeColor = SoftGray
            roleLabel.BackColor = Color.Transparent
            roleLabel.Location = New Point(80, 58)
            roleLabel.AutoSize = True
            userInfoPanel.Controls.Add(roleLabel)

            ' Online status
            Dim statusDot As New Label()
            statusDot.Text = "?"
            statusDot.Font = New Font("Segoe UI", 12, FontStyle.Regular)
            statusDot.ForeColor = SuccessGreen
            statusDot.BackColor = Color.Transparent
            statusDot.Location = New Point(80, 75)
            statusDot.AutoSize = True
            userInfoPanel.Controls.Add(statusDot)

            Dim statusLabel As New Label()
            statusLabel.Text = "Online"
            statusLabel.Font = New Font("Segoe UI", 8, FontStyle.Regular)
            statusLabel.ForeColor = SuccessGreen
            statusLabel.BackColor = Color.Transparent
            statusLabel.Location = New Point(95, 78)
            statusLabel.AutoSize = True
            userInfoPanel.Controls.Add(statusLabel)

        Catch ex As Exception
            Console.WriteLine($"Error creating user info section: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Navigate to specified form type
    ''' </summary>
    Private Sub NavigateToForm(formType As Type)
        Try
            Console.WriteLine($"Navigating to: {formType.Name}")

            ' Create new instance of the form
            Dim targetForm As Form = CType(Activator.CreateInstance(formType), Form)

            ' Set navigation flag on current form if it supports it
            If currentForm IsNot Nothing Then
                ' Use reflection to set isNavigating flag if it exists
                Dim navigatingField = currentForm.GetType().GetField("isNavigating",
                    Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
                If navigatingField IsNot Nothing Then
                    navigatingField.SetValue(currentForm, True)
                End If
            End If

            ' Show new form and close current
            targetForm.Show()
            currentForm?.Close()

        Catch ex As Exception
            Console.WriteLine($"Error navigating to form: {ex.Message}")
            MessageBox.Show($"Error opening {formType.Name}: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Handle logout process
    ''' </summary>
    Private Sub HandleLogout()
        Try
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ' Clear user session
                frmLoginvb.LogoutUser()

                ' Set navigation flag if supported
                If currentForm IsNot Nothing Then
                    Dim navigatingField = currentForm.GetType().GetField("isNavigating",
                        Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
                    If navigatingField IsNot Nothing Then
                        navigatingField.SetValue(currentForm, True)
                    End If
                End If

                ' Close current form and show login
                currentForm?.Close()
                Dim loginForm As New frmLoginvb()
                loginForm.Show()
            End If

        Catch ex As Exception
            Console.WriteLine($"Error during logout: {ex.Message}")
            MessageBox.Show($"Error during logout: {ex.Message}", "Logout Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Check if user has permission for specific action
    ''' </summary>
    Public Function HasPermission(requiredRole As UserRole) As Boolean
        Return GetCurrentUserRole() >= requiredRole
    End Function

    ''' <summary>
    ''' Get user's role display text
    ''' </summary>
    Public Function GetRoleDisplayText() As String
        Select Case GetCurrentUserRole()
            Case UserRole.Admin
                Return "Administrator"
            Case UserRole.Manager
                Return "Manager"
            Case UserRole.Staff
                Return "Staff"
            Case Else
                Return "Unknown"
        End Select
    End Function

End Module