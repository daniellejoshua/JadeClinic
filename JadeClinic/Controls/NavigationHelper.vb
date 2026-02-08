Imports Guna.UI2.WinForms
Imports System.Windows.Forms

''' <summary>
''' Navigation helper class that creates role-based navigation for any form
''' This replaces the NavigationModule with a more reliable approach
''' </summary>
Public Class NavigationHelper
    ' Color constants using JadeClinic palette
    Private Shared ReadOnly DeepCharcoal As Color = Color.FromArgb(26, 29, 31)      ' #1A1D1F
    Private Shared ReadOnly DarkSlate As Color = Color.FromArgb(43, 47, 50)         ' #2B2F32
    Private Shared ReadOnly Graphite As Color = Color.FromArgb(61, 65, 69)          ' #3D4145
    Private Shared ReadOnly SteelGray As Color = Color.FromArgb(74, 79, 84)         ' #4A4F54
    Private Shared ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)      ' #FFFFFF
    Private Shared ReadOnly LightSilver As Color = Color.FromArgb(225, 229, 233)    ' #E1E5E9
    Private Shared ReadOnly SoftGray As Color = Color.FromArgb(184, 188, 193)       ' #B8BCC1
    Private Shared ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)    ' #FECF10
    Private Shared ReadOnly RichOlive As Color = Color.FromArgb(190, 154, 48)       ' #BE9A30
    Private Shared ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)     ' #10D862
    Private Shared ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)         ' #FF4757

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

    ' Define all navigation items with role permissions - using working emojis
    Private Shared ReadOnly NavigationItems As NavigationItem() = {
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
    ''' <param name="parentForm">The form that contains the navigation</param>
    ''' <param name="navigationPanel">The Guna2Panel to use for navigation</param>
    ''' <param name="logoControl">Optional logo control to preserve</param>
    ''' <param name="activePageName">Name of currently active page</param>
    Public Shared Sub CreateNavigation(parentForm As Form, navigationPanel As Guna2Panel, Optional logoControl As Control = Nothing, Optional activePageName As String = "")
        Try
            Console.WriteLine($"Creating role-based navigation for {parentForm.GetType().Name}")
            Console.WriteLine($"User role: {GetLoggedInRole()}")

            ' Clear existing controls except logo
            ClearNavigationPanel(navigationPanel, logoControl)

            ' Set up navigation panel styling
            SetupNavigationPanelStyling(navigationPanel)

            ' Add header section (logo + title)
            CreateNavigationHeader(navigationPanel, logoControl)

            ' Add navigation buttons based on user role
            CreateRoleBasedNavigation(parentForm, navigationPanel, activePageName)

            ' User info section removed - just navigation buttons

            Console.WriteLine("Navigation menu created successfully")

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation menu: {ex.Message}")
            MessageBox.Show($"Error creating navigation: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ''' <summary>
    ''' Get user role enum from logged in role string
    ''' </summary>
    Private Shared Function GetCurrentUserRole() As UserRole
        Try
            Dim loggedRole As String = GetLoggedInRole()
            If String.IsNullOrEmpty(loggedRole) Then
                Return UserRole.Staff ' Default to lowest permission
            End If

            Select Case loggedRole.ToUpper()
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
    ''' Get logged in role - safer access to user info
    ''' </summary>
    Private Shared Function GetLoggedInRole() As String
        Try
            Return frmLoginvb.LoggedInRole
        Catch
            Return "Staff" ' Default fallback
        End Try
    End Function

    ''' <summary>
    ''' Get logged in username - safer access to user info
    ''' </summary>
    Private Shared Function GetLoggedInUsername() As String
        Try
            Return frmLoginvb.LoggedInUsername
        Catch
            Return "Guest" ' Default fallback
        End Try
    End Function

    ''' <summary>
    ''' Clear navigation panel while preserving specified control
    ''' </summary>
    Private Shared Sub ClearNavigationPanel(navigationPanel As Guna2Panel, logoControl As Control)
        Try
            For i = navigationPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = navigationPanel.Controls(i)
                If logoControl IsNot Nothing AndAlso control Is logoControl Then
                    Continue For ' Preserve logo
                ElseIf logoControl Is Nothing AndAlso TypeOf control Is PictureBox Then
                    Continue For ' Preserve any PictureBox if no specific logo specified
                Else
                    navigationPanel.Controls.Remove(control)
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
    Private Shared Sub SetupNavigationPanelStyling(navigationPanel As Guna2Panel)
        navigationPanel.FillColor = Color.White
        navigationPanel.BorderRadius = 0 ' Keep clean edges for navigation
    End Sub

    ''' <summary>
    ''' Create navigation header with logo and title
    ''' </summary>
    Private Shared Sub CreateNavigationHeader(navigationPanel As Guna2Panel, logoControl As Control)
        Try
            Dim availableWidth As Integer = navigationPanel.Width - 40

            ' Bring logo to front if it exists
            If logoControl IsNot Nothing Then
                logoControl.BringToFront()
            End If

            ' Add title label
            Dim titleLabel As New Label()
            titleLabel.Text = "JADE CLINIC"
            titleLabel.Font = New Font("Segoe UI", 14, FontStyle.Bold)
            titleLabel.ForeColor = GoldenYellow
            titleLabel.BackColor = Color.Transparent
            titleLabel.AutoSize = False
            titleLabel.Size = New Size(availableWidth, 30)
            titleLabel.Location = New Point(20, 110)
            titleLabel.TextAlign = ContentAlignment.MiddleCenter
            navigationPanel.Controls.Add(titleLabel)

            ' Add subtitle
            Dim subtitleLabel As New Label()
            subtitleLabel.Text = "Dental Supply Management"
            subtitleLabel.Font = New Font("Segoe UI", 10, FontStyle.Regular)
            subtitleLabel.ForeColor = Color.FromArgb(100, 100, 100) ' Dark Gray for visibility on white
            subtitleLabel.BackColor = Color.Transparent
            subtitleLabel.AutoSize = False
            subtitleLabel.Size = New Size(availableWidth, 25)
            subtitleLabel.Location = New Point(20, 145)
            subtitleLabel.TextAlign = ContentAlignment.MiddleCenter
            navigationPanel.Controls.Add(subtitleLabel)

            ' Add separator
            Dim separator As New Panel()
            separator.BackColor = Color.FromArgb(220, 220, 220) ' Light Gray for white background
            separator.Size = New Size(availableWidth - 20, 2)
            separator.Location = New Point(30, 185)
            navigationPanel.Controls.Add(separator)

            ' Add navigation label
            Dim navLabel As New Label()
            navLabel.Text = "NAVIGATION"
            navLabel.Font = New Font("Segoe UI", 10, FontStyle.Bold)
            navLabel.ForeColor = Color.FromArgb(80, 80, 80) ' Dark Gray for visibility on white
            navLabel.BackColor = Color.Transparent
            navLabel.AutoSize = False
            navLabel.Size = New Size(availableWidth, 25)
            navLabel.Location = New Point(20, 200)
            navLabel.TextAlign = ContentAlignment.MiddleCenter
            navigationPanel.Controls.Add(navLabel)

        Catch ex As Exception
            Console.WriteLine($"Error creating navigation header: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Create navigation buttons based on user role
    ''' </summary>
    Private Shared Sub CreateRoleBasedNavigation(parentForm As Form, navigationPanel As Guna2Panel, activePageName As String)
        Try
            Dim currentRole As UserRole = GetCurrentUserRole()
            Dim availableWidth As Integer = navigationPanel.Width - 40
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
                Dim isActive As Boolean = IsActivePage(activePageName, item.Text)

                Dim navButton As Guna2Button = CreateNavigationButton(
                    parentForm,
                    navigationPanel,
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
            separator.BackColor = Color.FromArgb(220, 220, 220) ' Light Gray for white background
            separator.Size = New Size(availableWidth - 40, 2)
            separator.Location = New Point(40, startY + buttonIndex * (buttonHeight + buttonSpacing) + 10)
            navigationPanel.Controls.Add(separator)

            ' Add logout button
            Dim logoutButton As Guna2Button = CreateLogoutButton(
                parentForm,
                navigationPanel,
                startY + buttonIndex * (buttonHeight + buttonSpacing) + 30,
                availableWidth - 5,
                buttonHeight
            )

        Catch ex As Exception
            Console.WriteLine($"Error creating role-based navigation: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Check if current page is active
    ''' </summary>
    Private Shared Function IsActivePage(activePageName As String, itemText As String) As Boolean
        Dim cleanItemText As String = itemText.Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").Replace("?? ", "").Replace("?? ", "")
        Return activePageName.ToLower().Contains(cleanItemText.ToLower()) OrElse cleanItemText.ToLower().Contains(activePageName.ToLower())
    End Function

    ''' <summary>
    ''' Create individual navigation button
    ''' </summary>
    Private Shared Function CreateNavigationButton(parentForm As Form, navigationPanel As Guna2Panel, text As String, yPosition As Integer, isActive As Boolean, buttonWidth As Integer, buttonHeight As Integer, formType As Type) As Guna2Button
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
            btn.ForeColor = Color.FromArgb(50, 50, 50) ' Dark Gray text on inactive for white background
            btn.BorderThickness = 1
            btn.BorderColor = Color.FromArgb(200, 200, 200) ' Light Gray border for white background
        End If

        ' Add shadow
        btn.ShadowDecoration.Enabled = True
        btn.ShadowDecoration.Color = DeepCharcoal
        btn.ShadowDecoration.Depth = 5
        btn.ShadowDecoration.Shadow = New Padding(0, 2, 5, 5)

        ' Improved hover effects with new color scheme
        AddHandler btn.MouseEnter, Sub()
                                       If Not isActive Then
                                           btn.FillColor = Color.FromArgb(240, 240, 240) ' Light Gray hover for white background
                                           btn.BorderColor = RichOlive
                                           btn.Font = New Font("Segoe UI", 10, FontStyle.Bold)
                                       End If
                                   End Sub

        AddHandler btn.MouseLeave, Sub()
                                       If Not isActive Then
                                           btn.FillColor = Color.Transparent
                                           btn.BorderColor = Color.FromArgb(200, 200, 200) ' Light Gray border
                                           btn.Font = New Font("Segoe UI", 10, FontStyle.Regular)
                                       End If
                                   End Sub

        ' Add click handler
        If formType IsNot Nothing Then
            AddHandler btn.Click, Sub() NavigateToForm(parentForm, formType)
        Else
            ' Handle special cases like Audit Logs or System Settings
            If text.Contains("Audit Logs") Then
                AddHandler btn.Click, Sub() MessageBox.Show("Audit Logs feature coming soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
            ElseIf text.Contains("System Settings") Then
                AddHandler btn.Click, Sub() MessageBox.Show("System Settings feature coming soon!", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End If

        navigationPanel.Controls.Add(btn)
        Return btn
    End Function

    ''' <summary>
    ''' Create logout button with special styling
    ''' </summary>
    Private Shared Function CreateLogoutButton(parentForm As Form, navigationPanel As Guna2Panel, yPosition As Integer, buttonWidth As Integer, buttonHeight As Integer) As Guna2Button
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

        ' Override hover effects for logout button to maintain red background
        RemoveHandler btn.MouseEnter, Nothing
        RemoveHandler btn.MouseLeave, Nothing
        AddHandler btn.MouseEnter, Sub()
                                       btn.FillColor = Color.FromArgb(220, 50, 50) ' Slightly darker red on hover
                                       btn.Font = New Font("Segoe UI", 10, FontStyle.Bold)
                                   End Sub
        AddHandler btn.MouseLeave, Sub()
                                       btn.FillColor = AlertRed
                                       btn.Font = New Font("Segoe UI", 10, FontStyle.Regular)
                                   End Sub

        ' Add click handler for logout
        AddHandler btn.Click, Sub() HandleLogout(parentForm)

        navigationPanel.Controls.Add(btn)
        Return btn
    End Function

    ''' <summary>
    ''' Navigate to specified form type
    ''' </summary>
    Private Shared Sub NavigateToForm(currentForm As Form, formType As Type)
        Try
            Console.WriteLine($"Navigating to: {formType.Name}")

            ' Create new instance of the form
            Dim targetForm As Form = CType(Activator.CreateInstance(formType), Form)

            ' Set navigation flag on current form if it supports it
            SetNavigatingFlag(currentForm, True)

            ' Show new form and close current
            targetForm.Show()
            currentForm.Close()

        Catch ex As Exception
            Console.WriteLine($"Error navigating to form: {ex.Message}")
            MessageBox.Show($"Error opening {formType.Name}: {ex.Message}", "Navigation Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Handle logout process
    ''' </summary>
    Private Shared Sub HandleLogout(currentForm As Form)
        Try
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

            If result = DialogResult.Yes Then
                ' Clear user session
                frmLoginvb.LogoutUser()

                ' Set navigation flag
                SetNavigatingFlag(currentForm, True)

                ' Close current form and show login
                currentForm.Close()
                Dim loginForm As New frmLoginvb()
                loginForm.Show()
            End If

        Catch ex As Exception
            Console.WriteLine($"Error during logout: {ex.Message}")
            MessageBox.Show($"Error during logout: {ex.Message}", "Logout Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ''' <summary>
    ''' Safely set isNavigating flag using reflection
    ''' </summary>
    Private Shared Sub SetNavigatingFlag(form As Form, value As Boolean)
        Try
            Dim navigatingField = form.GetType().GetField("isNavigating",
                Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
            If navigatingField IsNot Nothing Then
                navigatingField.SetValue(form, value)
            End If
        Catch ex As Exception
            ' Silently ignore if field doesn't exist
            Console.WriteLine($"Could not set isNavigating flag: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Check if user has permission for specific action
    ''' </summary>
    Public Shared Function HasPermission(requiredRole As UserRole) As Boolean
        Return GetCurrentUserRole() >= requiredRole
    End Function

    ''' <summary>
    ''' Get user's role display text
    ''' </summary>
    Public Shared Function GetRoleDisplayText() As String
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

End Class