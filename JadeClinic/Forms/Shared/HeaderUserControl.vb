Imports Guna.UI2.WinForms
Imports System.Drawing
Imports System.Windows.Forms

Public Class HeaderUserControl
    Inherits UserControl

    Private _lblUsername As Label
    Private _picAvatar As Guna2CirclePictureBox
    Private _navigateAction As Action
    Private _hostForm As Form
    Private _dropdown As Panel
    Private _dropdownVisible As Boolean = False
    Private _layoutHandlerWired As Boolean = False
    Private _maxUsernameWidth As Integer = 65
    Private Const RightMargin As Integer = 30

    Public Sub New()
        InitializeComponent()
    End Sub

    Private Sub InitializeComponent()
        Me.SuspendLayout()

        Me.BackColor = Color.Transparent
        Me.AutoScaleMode = AutoScaleMode.None
        Me.Size = New Size(105, 50)
        Me.Anchor = AnchorStyles.Top Or AnchorStyles.Left

        _picAvatar = New Guna2CirclePictureBox() With {
            .Name = "picAvatar",
            .Size = New Size(31, 28),
            .Location = New Point(0, 0),
            .ImageRotate = 0F,
            .TabStop = False,
            .Cursor = Cursors.Hand
        }
        _picAvatar.ShadowDecoration.Mode = Guna.UI2.WinForms.Enums.ShadowMode.Circle

        _lblUsername = New Label() With {
            .Name = "lblUsername",
            .Size = New Size(_maxUsernameWidth, 28),
            .Location = New Point(38, 0),
            .Font = New Font("Poppins Light", 9F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(59, 59, 59),
            .BackColor = Color.Transparent,
            .Text = "",
            .AutoSize = False,
            .TextAlign = ContentAlignment.MiddleLeft,
            .AutoEllipsis = True,
            .Cursor = Cursors.Hand
        }

        Me.Controls.Add(_lblUsername)
        Me.Controls.Add(_picAvatar)

        AddHandler _picAvatar.Click, AddressOf OnHeaderClick
        AddHandler _lblUsername.Click, AddressOf OnHeaderClick
        AddHandler _picAvatar.MouseEnter, Sub() _picAvatar.Cursor = Cursors.Hand
        AddHandler _lblUsername.MouseEnter, Sub() _lblUsername.Cursor = Cursors.Hand

        Me.ResumeLayout(False)
    End Sub

    Public Sub Initialize(form As Form, navigateToProfileSettings As Action)
        _navigateAction = navigateToProfileSettings
        _hostForm = form

        If _lblUsername IsNot Nothing Then
            _lblUsername.Text = frmLoginvb.LoggedInUsername
        End If

        ProfileManager.LoadUserProfilePicture(form, _picAvatar)

        PinToTopRight(form)
    End Sub

    Public Sub PinToTopRight(host As Control)
        If host Is Nothing Then Return
        ' The header stays at a fixed full-screen top-right position and does NOT
        ' follow the shrinking form edge on minimize. Anchoring Top|Left (not
        ' Top|Right) plus a fixed Location keeps it from drifting over the grid.
        Me.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        Me.Location = New Point(host.ClientSize.Width - Me.Width - RightMargin, 20)

        ' Force the host's scroll position back to the top after every layout pass.
        ' Without this, the form's AutoScroll drifts to the middle/bottom whenever
        ' the window is minimized and restored.
        If Not _layoutHandlerWired Then
            _layoutHandlerWired = True
            AddHandler host.Layout, Sub(s, e)
                                        Dim sc As ScrollableControl = TryCast(host, ScrollableControl)
                                        If sc IsNot Nothing Then
                                            Try
                                                If sc.AutoScrollPosition.X <> 0 OrElse sc.AutoScrollPosition.Y <> 0 Then
                                                    sc.AutoScrollPosition = New Point(0, 0)
                                                End If
                                            Catch
                                            End Try
                                        End If
                                    End Sub
        End If
    End Sub

    Private Sub OnHeaderClick(sender As Object, e As EventArgs)
        ToggleDropdown()
    End Sub

    Private Sub ToggleDropdown()
        If _dropdownVisible Then
            HideDropdown()
        Else
            ShowDropdown()
        End If
    End Sub

    Private Sub ShowDropdown()
        If _hostForm Is Nothing OrElse _dropdownVisible Then Return

        _dropdown = New Panel()
        _dropdown.Size = New Size(210, 90)
        _dropdown.BackColor = Color.White
        _dropdown.BorderStyle = BorderStyle.FixedSingle

        ' Position the dropdown just below the avatar using form coordinates
        Dim avatarFormLocation As Point = _hostForm.PointToClient(_picAvatar.Parent.PointToScreen(_picAvatar.Location))
        Dim dropdownX As Integer = avatarFormLocation.X - ((_dropdown.Width - _picAvatar.Width) \ 2)
        Dim dropdownY As Integer = avatarFormLocation.Y + _picAvatar.Height + 5
        _dropdown.Location = New Point(Math.Max(0, dropdownX), Math.Max(0, dropdownY))

        Dim btnProfileSettings As New Label()
        btnProfileSettings.Text = "⚙️ Profile Settings"
        btnProfileSettings.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnProfileSettings.ForeColor = Color.FromArgb(51, 51, 51)
        btnProfileSettings.BackColor = Color.Transparent
        btnProfileSettings.Size = New Size(200, 40)
        btnProfileSettings.Location = New Point(5, 5)
        btnProfileSettings.TextAlign = ContentAlignment.MiddleLeft
        btnProfileSettings.Cursor = Cursors.Hand

        AddHandler btnProfileSettings.MouseEnter, Sub() btnProfileSettings.BackColor = Color.FromArgb(240, 240, 240)
        AddHandler btnProfileSettings.MouseLeave, Sub() btnProfileSettings.BackColor = Color.Transparent
        AddHandler btnProfileSettings.Click, Sub()
                                                 HideDropdown()
                                                 _navigateAction?.Invoke()
                                             End Sub

        Dim btnLogOut As New Label()
        btnLogOut.Text = "🚪 Log Out"
        btnLogOut.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        btnLogOut.ForeColor = Color.FromArgb(51, 51, 51)
        btnLogOut.BackColor = Color.Transparent
        btnLogOut.Size = New Size(200, 40)
        btnLogOut.Location = New Point(5, 50)
        btnLogOut.TextAlign = ContentAlignment.MiddleLeft
        btnLogOut.Cursor = Cursors.Hand

        AddHandler btnLogOut.MouseEnter, Sub() btnLogOut.BackColor = Color.FromArgb(240, 240, 240)
        AddHandler btnLogOut.MouseLeave, Sub() btnLogOut.BackColor = Color.Transparent
        AddHandler btnLogOut.Click, Sub()
                                        HideDropdown()
                                        PerformLogout()
                                    End Sub

        _dropdown.Controls.Add(btnProfileSettings)
        _dropdown.Controls.Add(btnLogOut)

        _hostForm.Controls.Add(_dropdown)
        _dropdown.BringToFront()
        _dropdownVisible = True

        ' Close the dropdown when the form background is clicked
        AddHandler _hostForm.Click, Sub(s, e2) HideDropdown()
    End Sub

    Private Sub HideDropdown()
        If _dropdown IsNot Nothing Then
            If _hostForm IsNot Nothing Then
                _hostForm.Controls.Remove(_dropdown)
            End If
            _dropdown.Dispose()
            _dropdown = Nothing
        End If
        _dropdownVisible = False
    End Sub

    Private Sub PerformLogout()
        Try
            Dim result As DialogResult = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
            If result <> DialogResult.Yes Then Return

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Log Out", "User logged out of the application.")
            End If

            frmLoginvb.LogoutUser()
            If _hostForm IsNot Nothing Then
                _hostForm.Hide()
            End If
            Dim loginForm As New frmLoginvb()
            loginForm.Show()
        Catch ex As Exception
            MessageBox.Show($"Unable to log out: {ex.Message}", "Logout Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Public Property UsernameText As String
        Get
            Return _lblUsername.Text
        End Get
        Set(value As String)
            _lblUsername.Text = value
        End Set
    End Property

    Public ReadOnly Property AvatarPictureBox As Guna2CirclePictureBox
        Get
            Return _picAvatar
        End Get
    End Property

    Public ReadOnly Property UsernameLabel As Label
        Get
            Return _lblUsername
        End Get
    End Property
End Class