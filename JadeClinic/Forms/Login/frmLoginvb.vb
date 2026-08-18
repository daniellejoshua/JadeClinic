Imports System.Data.Common
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading.Tasks
Imports System.Configuration
Imports BCrypt.Net

Public Class frmLoginvb
    ' Shared variables to store logged-in user information
    Public Shared LoggedInUserID As Integer
    Public Shared LoggedInUsername As String
    Public Shared LoggedInFullName As String
    Public Shared LoggedInRole As String
    Public Shared LoggedInPIN As String

    Private pinPanel As Guna.UI2.WinForms.Guna2Panel
    Private pinPanelButtons As List(Of Guna.UI2.WinForms.Guna2Button)
    Private failedPinAttempts As Integer = 0
    Private failedLoginAttempts As Integer = 0
    Private Const MaxLoginAttempts As Integer = 3
    Private pinInput As String = ""
    Private qrScannerEnabled As Boolean = True
    Private qrScannerActive As Boolean = False
    Private passwordVisible As Boolean = False

    ' Runtime UI controls
    Private cardPanel As Guna.UI2.WinForms.Guna2Panel
    Private shadowLayers As List(Of Guna.UI2.WinForms.Guna2Panel)
    Private picLogo As Guna.UI2.WinForms.Guna2PictureBox
    Private lblTitle As Guna.UI2.WinForms.Guna2HtmlLabel
    Private lblSubtitle As Guna.UI2.WinForms.Guna2HtmlLabel
    Private lblUsernameLabel As Guna.UI2.WinForms.Guna2HtmlLabel
    Private txtUsername As Guna.UI2.WinForms.Guna2TextBox
    Private lblPasswordLabel As Guna.UI2.WinForms.Guna2HtmlLabel
    Private txtPassword As Guna.UI2.WinForms.Guna2TextBox
    Private lblEyeToggle As Label
    Private WithEvents lnkForgotPassword As Guna.UI2.WinForms.Guna2HtmlLabel
    Private WithEvents btnLogin As Guna.UI2.WinForms.Guna2Button
    Private pnlDivider As Panel
    Private lblOr As Label
    Private WithEvents btnQRLogin As Guna.UI2.WinForms.Guna2Button
    Private pnlAccentLine As Guna.UI2.WinForms.Guna2Panel

    Private Const TitleBarHoverHeight As Integer = 8
    Private isTitleBarVisible As Boolean = False

    ' Custom title bar
    Private titleBarPanel As Guna.UI2.WinForms.Guna2Panel
    Private WithEvents btnMinimize As Guna.UI2.WinForms.Guna2Button
    Private WithEvents btnMaximize As Guna.UI2.WinForms.Guna2Button
    Private WithEvents btnCloseTitle As Guna.UI2.WinForms.Guna2Button

    ' Drag support
    Private isDragging As Boolean = False
    Private dragOffset As Point

    ' Splash screen
    Private splashPanel As Panel
    Private splashTimer As Timer
    Private splashProgress As Single = 0F
    Private splashFadingOut As Boolean = False
    Private splashOpacity As Integer = 255
    Private splashBuilt As Boolean = False
    Private splashLogoImg As Image = Nothing
    Private splashBuffer As Bitmap = Nothing

    Private Sub EnableTitleBarHover()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.ControlBox = False
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.TopMost = False
    End Sub

    Private Sub CreateTitleBar()
        Dim btnWidth As Integer = 46
        Dim btnCount As Integer = 3

        titleBarPanel = New Guna.UI2.WinForms.Guna2Panel() With {
            .Height = 35,
            .Width = btnWidth * btnCount,
            .FillColor = Color.White,
            .Visible = False,
            .Anchor = AnchorStyles.Top Or AnchorStyles.Right
        }

        btnCloseTitle = New Guna.UI2.WinForms.Guna2Button() With {
            .Dock = DockStyle.Right,
            .Width = 46,
            .FillColor = Color.White,
            .ForeColor = Color.FromArgb(42, 42, 42),
            .Font = New Font("Segoe UI", 10),
            .Text = ChrW(&H2715),
            .BorderColor = Color.Transparent,
            .BorderRadius = 0,
            .Cursor = Cursors.Hand
        }
        AddHandler btnCloseTitle.Click, Sub() Me.Close()
        AddHandler btnCloseTitle.MouseEnter, Sub()
                                                btnCloseTitle.FillColor = Color.FromArgb(220, 80, 70)
                                                btnCloseTitle.ForeColor = Color.White
                                            End Sub
        AddHandler btnCloseTitle.MouseLeave, Sub()
                                                btnCloseTitle.FillColor = Color.White
                                                btnCloseTitle.ForeColor = Color.FromArgb(42, 42, 42)
                                            End Sub

        btnMaximize = New Guna.UI2.WinForms.Guna2Button() With {
            .Dock = DockStyle.Right,
            .Width = 46,
            .FillColor = Color.White,
            .ForeColor = Color.FromArgb(42, 42, 42),
            .Font = New Font("Segoe UI", 10),
            .Text = ChrW(&H2752),
            .BorderColor = Color.Transparent,
            .BorderRadius = 0,
            .Cursor = Cursors.Hand
        }
        AddHandler btnMaximize.Click, Sub()
                                          If Me.WindowState = FormWindowState.Maximized Then
                                              Me.WindowState = FormWindowState.Normal
                                              Dim w As Integer = CInt(Screen.PrimaryScreen.WorkingArea.Width * 0.8)
                                              Dim h As Integer = CInt(Screen.PrimaryScreen.WorkingArea.Height * 0.85)
                                              Dim x As Integer = CInt((Screen.PrimaryScreen.WorkingArea.Width - w) / 2)
                                              Dim y As Integer = CInt((Screen.PrimaryScreen.WorkingArea.Height - h) / 2)
                                              Me.Bounds = New Rectangle(x, y, w, h)
                                              btnMaximize.Text = ChrW(&H25A1)
                                          Else
                                              Me.WindowState = FormWindowState.Maximized
                                              btnMaximize.Text = ChrW(&H2752)
                                          End If
                                          CenterLoginLayout()
                                      End Sub
        AddHandler btnMaximize.MouseEnter, Sub()
                                               btnMaximize.FillColor = Color.FromArgb(230, 230, 230)
                                           End Sub
        AddHandler btnMaximize.MouseLeave, Sub()
                                               btnMaximize.FillColor = Color.White
                                           End Sub

        btnMinimize = New Guna.UI2.WinForms.Guna2Button() With {
            .Dock = DockStyle.Right,
            .Width = 46,
            .FillColor = Color.White,
            .ForeColor = Color.FromArgb(42, 42, 42),
            .Font = New Font("Segoe UI", 10),
            .Text = ChrW(&H2013),
            .BorderColor = Color.Transparent,
            .BorderRadius = 0,
            .Cursor = Cursors.Hand
        }
        AddHandler btnMinimize.Click, Sub() Me.WindowState = FormWindowState.Minimized
        AddHandler btnMinimize.MouseEnter, Sub()
                                               btnMinimize.FillColor = Color.FromArgb(230, 230, 230)
                                           End Sub
        AddHandler btnMinimize.MouseLeave, Sub()
                                               btnMinimize.FillColor = Color.White
                                           End Sub

        titleBarPanel.Controls.Add(btnMinimize)
        titleBarPanel.Controls.Add(btnMaximize)
        titleBarPanel.Controls.Add(btnCloseTitle)

        Me.Controls.Add(titleBarPanel)
        titleBarPanel.BringToFront()
        PositionTitleBar()
    End Sub

    Private Sub PositionTitleBar()
        If titleBarPanel Is Nothing Then Return
        titleBarPanel.Location = New Point(Me.ClientSize.Width - titleBarPanel.Width, 0)
    End Sub

    ' ================================================================
    '  SPLASH SCREEN — smooth brush-stroke animation
    ' ================================================================
    Private Sub ShowSplashScreen()
        splashPanel = New Panel()
        splashPanel.Dock = DockStyle.Fill
        splashPanel.BackColor = Color.FromArgb(250, 247, 242)

        Dim props As Reflection.BindingFlags = Reflection.BindingFlags.SetProperty Or Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic
        GetType(Control).InvokeMember("DoubleBuffered", props, Nothing, splashPanel, New Object() {True})

        Me.Controls.Add(splashPanel)
        splashPanel.BringToFront()

        Try
            splashLogoImg = My.Resources.Resources.CleanJadeLogo_1_
        Catch
        End Try
        If splashLogoImg Is Nothing Then
            Try
                splashLogoImg = My.Resources.Resources.JadeLogo
            Catch
            End Try
        End If

        AddHandler splashPanel.Paint, AddressOf splashPanel_Paint

        splashProgress = 0F
        splashFadingOut = False
        splashOpacity = 255
        splashBuilt = False

        splashTimer = New Timer()
        splashTimer.Interval = 16
        AddHandler splashTimer.Tick, AddressOf splashTimer_Tick
        splashTimer.Start()
    End Sub

    Private Sub splashTimer_Tick(sender As Object, e As EventArgs)
        If splashFadingOut Then
            splashTimer.Stop()
            splashTimer.Dispose()
            RemoveHandler splashTimer.Tick, AddressOf splashTimer_Tick
            RemoveHandler splashPanel.Paint, AddressOf splashPanel_Paint

            ' Paint BackgroundImage onto splash buffer as final frame
            If splashBuffer IsNot Nothing AndAlso Me.BackgroundImage IsNot Nothing Then
                Using bg As Graphics = Graphics.FromImage(splashBuffer)
                    bg.DrawImage(Me.BackgroundImage, 0, 0, splashBuffer.Width, splashBuffer.Height)
                End Using
                splashPanel.Invalidate()
                splashPanel.Update()
            End If

            ' Force ALL child controls to repaint
            ForcePaintAllControls(Me)
            Me.Update()
            Application.DoEvents()

            ' NOW remove splash — everything underneath is painted
            Me.Controls.Remove(splashPanel)
            splashPanel.Dispose()
            splashPanel = Nothing
            If splashBuffer IsNot Nothing Then
                splashBuffer.Dispose()
                splashBuffer = Nothing
            End If
        Else
            splashProgress += 0.008F
            If splashProgress >= 1.0F Then
                splashProgress = 1.0F
                If splashBuilt Then
                    splashFadingOut = True
                End If
            End If
        End If

        If splashPanel IsNot Nothing Then
            splashPanel.Invalidate()
        End If
    End Sub

    Private Sub splashPanel_Paint(sender As Object, e As PaintEventArgs)
        Dim w As Integer = splashPanel.Width
        Dim h As Integer = splashPanel.Height
        If w <= 0 OrElse h <= 0 Then Return

        If splashBuffer Is Nothing OrElse splashBuffer.Width <> w OrElse splashBuffer.Height <> h Then
            If splashBuffer IsNot Nothing Then splashBuffer.Dispose()
            splashBuffer = New Bitmap(w, h, Drawing.Imaging.PixelFormat.Format32bppPArgb)
        End If

        Using bg As Graphics = Graphics.FromImage(splashBuffer)
            bg.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            bg.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit
            bg.Clear(Color.FromArgb(250, 247, 242))

            Dim cx As Integer = w \ 2
            Dim gold As Color = Color.FromArgb(190, 154, 48)
            Dim darkText As Color = Color.FromArgb(37, 37, 37)
            Dim grayText As Color = Color.FromArgb(153, 153, 153)

            Dim p As Single = splashProgress

            ' ── LOGO ──
            If p > 0.05F AndAlso splashLogoImg IsNot Nothing Then
                Dim logoAlpha As Integer = CInt(Math.Min(255, Math.Min(255, ((p - 0.05F) / 0.2F) * 255)))
                Dim logoSz As Integer = 90
                Dim logoRect As New Rectangle(cx - logoSz \ 2, CInt(h * 0.28F) - logoSz \ 2, logoSz, logoSz)
                If logoAlpha < 255 Then
                    Dim cm As New Drawing.Imaging.ColorMatrix()
                    cm.Matrix33 = logoAlpha / 255.0F
                    Using ia As New Drawing.Imaging.ImageAttributes()
                        ia.SetColorMatrix(cm)
                        bg.DrawImage(splashLogoImg, logoRect, 0, 0, splashLogoImg.Width, splashLogoImg.Height, GraphicsUnit.Pixel, ia)
                    End Using
                Else
                    bg.DrawImage(splashLogoImg, logoRect)
                End If
            End If

            ' ── "JADE CLINIC" TEXT ──
            Dim titleText As String = "Jade Clinic"
            Dim titleFont As New Font("Poppins", 32.0F, FontStyle.Regular)
            Dim titleSize As SizeF = bg.MeasureString(titleText, titleFont)
            Dim titleX As Single = (w - titleSize.Width) / 2.0F
            Dim titleY As Single = h * 0.4F

            If p > 0.15F Then
                Dim reveal As Single = Math.Min(1.0F, (p - 0.15F) / 0.45F)
                Dim titleAlpha As Integer = CInt(Math.Min(255, reveal * 255 * 2))

                Using path As New Drawing2D.GraphicsPath()
                    path.AddString(titleText, titleFont.FontFamily, CInt(titleFont.Style), titleFont.Size, New PointF(titleX, titleY), StringFormat.GenericDefault)
                    Using brush As New SolidBrush(Color.FromArgb(Math.Min(255, titleAlpha), darkText))
                        bg.SetClip(path)
                        Dim brushStrokeX As Integer = CInt(w * reveal)
                        Dim gradRect As New Rectangle(brushStrokeX - 60, 0, 120, h)
                        bg.FillRectangle(New SolidBrush(Color.FromArgb(0, 250, 247, 242)), 0, 0, w, h)
                        bg.ResetClip()
                    End Using
                End Using

                Using titlePath As New Drawing2D.GraphicsPath()
                    titlePath.AddString(titleText, titleFont.FontFamily, CInt(titleFont.Style), titleFont.Size, New PointF(titleX, titleY), StringFormat.GenericDefault)
                    Dim clipX As Integer = CInt(titleX - 10 + (titleSize.Width + 20) * reveal)
                    If clipX > CInt(titleX - 10) Then
                        Using clipPath As New Drawing2D.GraphicsPath()
                            clipPath.AddRectangle(New Rectangle(CInt(titleX - 10), CInt(titleY - 10), clipX - CInt(titleX - 10), CInt(titleSize.Height + 20)))
                            bg.SetClip(clipPath)
                            Using brush As New SolidBrush(Color.FromArgb(Math.Min(255, titleAlpha), darkText))
                                bg.DrawString(titleText, titleFont, brush, titleX, titleY)
                            End Using
                            bg.ResetClip()
                        End Using
                    End If
                End Using
            End If
            titleFont.Dispose()

            ' ── "POINT OF SALE SYSTEM" ──
            Dim subText As String = "Point of Sale & Inventory System"
            Dim subFont As New Font("Poppins", 12.0F, FontStyle.Regular)
            Dim subSize As SizeF = bg.MeasureString(subText, subFont)
            Dim subX As Single = (w - subSize.Width) / 2.0F
            Dim subY As Single = titleY + titleSize.Height + 8

            If p > 0.45F Then
                Dim subReveal As Single = Math.Min(1.0F, (p - 0.45F) / 0.3F)
                Dim subAlpha As Integer = CInt(subReveal * 255)
                Dim clipSubX As Integer = CInt(subX - 5 + (subSize.Width + 10) * subReveal)
                If clipSubX > CInt(subX - 5) Then
                    Using clipPath As New Drawing2D.GraphicsPath()
                        clipPath.AddRectangle(New Rectangle(CInt(subX - 5), CInt(subY - 5), clipSubX - CInt(subX - 5), CInt(subSize.Height + 10)))
                        bg.SetClip(clipPath)
                        Using brush As New SolidBrush(Color.FromArgb(subAlpha, grayText))
                            bg.DrawString(subText, subFont, brush, subX, subY)
                        End Using
                        bg.ResetClip()
                    End Using
                End If
            End If
            subFont.Dispose()

            ' ── GOLD ACCENT LINE ──
            If p > 0.6F Then
                Dim lineReveal As Single = Math.Min(1.0F, (p - 0.6F) / 0.2F)
                Dim lineW As Integer = CInt(100 * lineReveal)
                Dim lineY As Integer = CInt(subY + subSize.Height + 16)
                Using pen As New Pen(gold, 2.0F)
                    bg.DrawLine(pen, cx - lineW, lineY, cx + lineW, lineY)
                End Using
            End If
        End Using

        e.Graphics.DrawImageUnscaled(splashBuffer, 0, 0)
    End Sub

    Private Sub ForcePaintAllControls(parent As Control)
        For Each c As Control In parent.Controls
            If c IsNot splashPanel AndAlso c.IsHandleCreated Then
                c.Invalidate(True)
                ForcePaintAllControls(c)
            End If
        Next
    End Sub

    Private Sub frmLoginvb_MouseMove(sender As Object, e As MouseEventArgs)
        Dim shouldShow = e.Y <= TitleBarHoverHeight
        If shouldShow <> isTitleBarVisible Then
            isTitleBarVisible = shouldShow
            If titleBarPanel IsNot Nothing Then
                PositionTitleBar()
                titleBarPanel.Visible = shouldShow
                If shouldShow Then titleBarPanel.BringToFront()
            End If
        End If

        ' Form drag when not maximized
        If isDragging AndAlso Me.WindowState <> FormWindowState.Maximized Then
            Me.Location = New Point(Cursor.Position.X - dragOffset.X, Cursor.Position.Y - dragOffset.Y)
        End If
    End Sub

    Private Sub frmLoginvb_MouseLeave(sender As Object, e As EventArgs)
        If isTitleBarVisible Then
            isTitleBarVisible = False
            If titleBarPanel IsNot Nothing Then titleBarPanel.Visible = False
        End If
    End Sub

    Private Sub frmLoginvb_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Enable double buffering on form to prevent repaint flicker
        Dim props As Reflection.BindingFlags = Reflection.BindingFlags.SetProperty Or Reflection.BindingFlags.Instance Or Reflection.BindingFlags.NonPublic
        GetType(Control).InvokeMember("DoubleBuffered", props, Nothing, Me, New Object() {True})

        Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Me.MaximizeBox = False
        Me.WindowState = FormWindowState.Maximized
        Me.FormBorderStyle = FormBorderStyle.None
        Me.KeyPreview = True

        ' ── SHOW SPLASH IMMEDIATELY ──
        ShowSplashScreen()
        Application.DoEvents()

        ' ── BUILD ALL CONTROLS (underneath splash) ──
        BuildLoginCard()
        CenterLoginLayout()
        EnableTitleBarHover()
        CreateTitleBar()
        AddHandler Me.Resize, Sub()
                                  CenterLoginLayout()
                                  PositionTitleBar()
                              End Sub
        AddHandler Me.KeyDown, AddressOf frmLoginvb_KeyDown
        AddHandler Me.MouseMove, AddressOf frmLoginvb_MouseMove
        AddHandler Me.MouseLeave, AddressOf frmLoginvb_MouseLeave
        AddHandler Me.MouseDown, Sub(s2, e2)
                                     If e2.Button = MouseButtons.Left AndAlso e2.Y <= TitleBarHoverHeight AndAlso Me.WindowState <> FormWindowState.Maximized Then
                                         isDragging = True
                                         dragOffset = New Point(e2.X, e2.Y)
                                     End If
                                 End Sub
        AddHandler Me.MouseUp, Sub(s2, e2)
                                   isDragging = False
                               End Sub

        InitializeDatabaseOnStartup()

        AddHandler txtPassword.KeyDown, AddressOf txtPassword_KeyDown

        AddHandler txtUsername.KeyPress, AddressOf ProtectFromQRInput
        AddHandler txtPassword.KeyPress, AddressOf ProtectFromQRInput
        AddHandler txtUsername.TextChanged, AddressOf ValidateInputForQRCodes
        AddHandler txtPassword.TextChanged, AddressOf ValidateInputForQRCodes

        SetupTabIndex()

        ' ── LOAD BACKGROUND IMAGE LAST (heaviest — splash covers it) ──
        Try
            Me.BackgroundImage = My.Resources.Resources.ChatGPT_Image_Aug_17__2026__11_39_37_AM
            Me.BackgroundImageLayout = ImageLayout.Stretch
        Catch
        End Try

        ' ── SIGNAL: splash can finish ──
        splashBuilt = True
    End Sub

    ' ================================================================
    '  BUILD LOGIN CARD — all UI created at runtime
    ' ================================================================
    Private Sub BuildLoginCard()
        Dim cardW As Integer = 660
        Dim cardH As Integer = 680
        Dim contentX As Integer = 65
        Dim contentW As Integer = cardW - (contentX * 2)
        Dim inputH As Integer = 48
        Dim logoSize As Integer = 120
        Dim logoSpacing As Integer = 30
        Dim iconSize As New Size(20, 20)

        Dim primaryGold As Color = Color.FromArgb(190, 154, 48)
        Dim hoverGold As Color = Color.FromArgb(168, 134, 39)
        Dim inputBorder As Color = Color.FromArgb(217, 217, 217)
        Dim primaryText As Color = Color.FromArgb(37, 37, 37)
        Dim labelText As Color = Color.FromArgb(51, 51, 51)
        Dim subtitleColor As Color = Color.FromArgb(119, 119, 119)
        Dim dividerColor As Color = Color.FromArgb(232, 230, 224)
        Dim placeholderColor As Color = Color.FromArgb(153, 153, 153)

        ' ── LOGO (on form, above card) ──
        Dim logoImg As Image = Nothing
        Try
            logoImg = My.Resources.Resources.Jade_Dental_Logo

        Catch
        End Try
        If logoImg Is Nothing Then
            Try
                logoImg = My.Resources.Resources.JadeLogo
            Catch
            End Try
        End If

        picLogo = New Guna.UI2.WinForms.Guna2PictureBox()
        picLogo.Size = New Size(logoSize, logoSize)
        picLogo.SizeMode = PictureBoxSizeMode.Zoom
        picLogo.BackColor = Color.Transparent
        If logoImg IsNot Nothing Then picLogo.Image = logoImg
        Me.Controls.Add(picLogo)

        ' ── CARD PANEL ──
        cardPanel = New Guna.UI2.WinForms.Guna2Panel()
        cardPanel.Size = New Size(cardW, cardH)
        cardPanel.FillColor = Color.White
        cardPanel.BorderRadius = 22
        cardPanel.BackColor = Color.Transparent
        cardPanel.ShadowDecoration.Enabled = False
        Me.Controls.Add(cardPanel)

        ' ── CUSTOM ROUNDED SHADOW (layered panels behind card, downward offset) ──
        ' Shadow: color #BE9A30, 10-12% opacity, Y offset 8px, blur ~25-30px, no X offset
        ' Layers spread outward only downward; top edge is nearly flush with card
        shadowLayers = New List(Of Guna.UI2.WinForms.Guna2Panel)()
        Dim shadowData(,) As Integer = {
            {3, 2, 3, 8, 30},    ' spreadL, spreadT, spreadR, spreadB, alpha
            {5, 3, 5, 13, 24},
            {8, 4, 8, 18, 18},
            {10, 5, 10, 23, 12},
            {12, 5, 12, 28, 7}
        }
        For i As Integer = 0 To shadowData.GetUpperBound(0)
            Dim sL As Integer = shadowData(i, 0)
            Dim sT As Integer = shadowData(i, 1)
            Dim sR As Integer = shadowData(i, 2)
            Dim sB As Integer = shadowData(i, 3)
            Dim a As Integer = shadowData(i, 4)
            Dim sp As New Guna.UI2.WinForms.Guna2Panel()
            sp.Size = New Size(cardW + sL + sR, cardH + sT + sB)
            sp.BorderRadius = 22 + Math.Max(Math.Max(sL, sR), Math.Max(sT, sB))
            sp.FillColor = Color.FromArgb(a, 190, 154, 48)
            sp.BackColor = Color.Transparent
            sp.ShadowDecoration.Enabled = False
            Me.Controls.Add(sp)
            sp.SendToBack()
            shadowLayers.Add(sp)
        Next

        ' ── TITLE ──
        lblTitle = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblTitle.Text = "Welcome Back!"
        lblTitle.Font = New Font("Poppins", 22.0F, FontStyle.Regular)
        lblTitle.ForeColor = primaryText
        lblTitle.BackColor = Color.Transparent
        lblTitle.AutoSize = True
        cardPanel.Controls.Add(lblTitle)

        ' ── GOLD ACCENT LINE (below title) ──
        pnlAccentLine = New Guna.UI2.WinForms.Guna2Panel()
        pnlAccentLine.Size = New Size(56, 6)
        pnlAccentLine.BorderRadius = 3
        pnlAccentLine.FillColor = primaryGold
        cardPanel.Controls.Add(pnlAccentLine)

        ' ── SUBTITLE ──
        lblSubtitle = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblSubtitle.Text = "Please enter your credentials to continue"
        lblSubtitle.Font = New Font("Poppins", 9.5F, FontStyle.Regular)
        lblSubtitle.ForeColor = subtitleColor
        lblSubtitle.BackColor = Color.Transparent
        lblSubtitle.AutoSize = True
        cardPanel.Controls.Add(lblSubtitle)

        ' ── USERNAME LABEL ──
        lblUsernameLabel = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblUsernameLabel.Text = "Username"
        lblUsernameLabel.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        lblUsernameLabel.ForeColor = labelText
        lblUsernameLabel.BackColor = Color.Transparent
        lblUsernameLabel.AutoSize = True
        cardPanel.Controls.Add(lblUsernameLabel)

        ' ── USERNAME TEXTBOX ──
        txtUsername = New Guna.UI2.WinForms.Guna2TextBox()
        txtUsername.Size = New Size(contentW, inputH)
        txtUsername.BorderRadius = 9
        txtUsername.FillColor = Color.White
        txtUsername.BorderColor = inputBorder
        txtUsername.BorderThickness = 1
        txtUsername.FocusedState.BorderColor = primaryGold
        txtUsername.HoverState.BorderColor = primaryGold
        txtUsername.ForeColor = primaryText
        txtUsername.PlaceholderForeColor = placeholderColor
        txtUsername.PlaceholderText = "Enter username"
        txtUsername.Font = New Font("Segoe UI", 10.0F)
        txtUsername.BackColor = Color.Transparent
        txtUsername.TextAlign = HorizontalAlignment.Left
        txtUsername.IconLeft = CreateUserIcon(primaryGold)
        txtUsername.IconLeftSize = New Size(18, 18)
        txtUsername.IconLeftOffset = New Point(10, 0)
        txtUsername.TextOffset = New Point(14, 0)
        cardPanel.Controls.Add(txtUsername)

        ' ── PASSWORD LABEL ──
        lblPasswordLabel = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblPasswordLabel.Text = "Password"
        lblPasswordLabel.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        lblPasswordLabel.ForeColor = labelText
        lblPasswordLabel.BackColor = Color.Transparent
        lblPasswordLabel.AutoSize = True
        cardPanel.Controls.Add(lblPasswordLabel)

        ' ── PASSWORD TEXTBOX ──
        txtPassword = New Guna.UI2.WinForms.Guna2TextBox()
        txtPassword.Size = New Size(contentW, inputH)
        txtPassword.BorderRadius = 9
        txtPassword.FillColor = Color.White
        txtPassword.BorderColor = inputBorder
        txtPassword.BorderThickness = 1
        txtPassword.FocusedState.BorderColor = primaryGold
        txtPassword.HoverState.BorderColor = primaryGold
        txtPassword.ForeColor = primaryText
        txtPassword.PlaceholderForeColor = placeholderColor
        txtPassword.PlaceholderText = "Enter password"
        txtPassword.PasswordChar = "•"c
        txtPassword.UseSystemPasswordChar = False
        txtPassword.Font = New Font("Segoe UI", 10.0F)
        txtPassword.BackColor = Color.Transparent
        txtPassword.TextAlign = HorizontalAlignment.Left
        txtPassword.IconLeft = CreateLockIcon(primaryGold)
        txtPassword.IconLeftSize = New Size(18, 18)
        txtPassword.IconLeftOffset = New Point(10, 0)
        txtPassword.TextOffset = New Point(14, 0)
        cardPanel.Controls.Add(txtPassword)

        ' ── EYE TOGGLE ──
        lblEyeToggle = New Label()
        lblEyeToggle.Size = New Size(28, 28)
        lblEyeToggle.BackColor = Color.Transparent
        lblEyeToggle.Cursor = Cursors.Hand
        lblEyeToggle.Image = CreateEyeIcon(True)
        cardPanel.Controls.Add(lblEyeToggle)
        lblEyeToggle.BringToFront()
        AddHandler lblEyeToggle.Click, Sub() TogglePasswordVisibility()

        ' ── FORGOT PASSWORD LINK ──
        lnkForgotPassword = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lnkForgotPassword.Text = "Forgot Password?"
        lnkForgotPassword.Font = New Font("Poppins", 9.5F, FontStyle.Regular)
        lnkForgotPassword.ForeColor = primaryGold
        lnkForgotPassword.BackColor = Color.Transparent
        lnkForgotPassword.AutoSize = True
        lnkForgotPassword.Cursor = Cursors.Hand
        cardPanel.Controls.Add(lnkForgotPassword)

        ' ── LOGIN BUTTON ──
        btnLogin = New Guna.UI2.WinForms.Guna2Button()
        btnLogin.Size = New Size(contentW, 52)
        btnLogin.BorderRadius = 10
        btnLogin.FillColor = primaryGold
        btnLogin.ForeColor = Color.White
        btnLogin.Font = New Font("Poppins", 10.5F, FontStyle.Regular)
        btnLogin.Text = "➜  Login"
        btnLogin.TextAlign = HorizontalAlignment.Center
        btnLogin.BackColor = Color.Transparent
        btnLogin.HoverState.FillColor = hoverGold
        cardPanel.Controls.Add(btnLogin)

        ' ── DIVIDER ──
        pnlDivider = New Panel()
        pnlDivider.Size = New Size(contentW, 22)
        pnlDivider.BackColor = Color.Transparent
        cardPanel.Controls.Add(pnlDivider)

        Dim lineLeft As New Panel()
        lineLeft.Size = New Size(contentW \ 2 - 26, 1)
        lineLeft.BackColor = dividerColor
        lineLeft.Location = New Point(0, 10)
        pnlDivider.Controls.Add(lineLeft)

        lblOr = New Label()
        lblOr.Text = "OR"
        lblOr.Font = New Font("Poppins", 8.5F, FontStyle.Regular)
        lblOr.ForeColor = Color.FromArgb(153, 153, 153)
        lblOr.BackColor = Color.Transparent
        lblOr.AutoSize = True
        lblOr.Location = New Point(contentW \ 2 - 13, 0)
        pnlDivider.Controls.Add(lblOr)

        Dim lineRight As New Panel()
        lineRight.Size = New Size(contentW \ 2 - 26, 1)
        lineRight.BackColor = dividerColor
        lineRight.Location = New Point(contentW \ 2 + 26, 10)
        pnlDivider.Controls.Add(lineRight)

        ' ── QR LOGIN BUTTON ──
        btnQRLogin = New Guna.UI2.WinForms.Guna2Button()
        btnQRLogin.Size = New Size(contentW, 48)
        btnQRLogin.BorderRadius = 10
        btnQRLogin.FillColor = Color.White
        btnQRLogin.BorderColor = primaryGold
        btnQRLogin.BorderThickness = 1
        btnQRLogin.Text = "Scan QR Code to Login"
        btnQRLogin.ForeColor = primaryGold
        btnQRLogin.Font = New Font("Poppins", 9.5F, FontStyle.Regular)
        btnQRLogin.TextAlign = HorizontalAlignment.Left
        btnQRLogin.BackColor = Color.Transparent
        btnQRLogin.HoverState.FillColor = Color.FromArgb(10, 190, 154, 48)
        btnQRLogin.Image = CreateQRIcon(primaryGold)
        btnQRLogin.ImageSize = New Size(24, 24)
        btnQRLogin.ImageAlign = HorizontalAlignment.Left
        btnQRLogin.ImageOffset = New Point(143, 0)
        btnQRLogin.TextOffset = New Point(173, 0)
        cardPanel.Controls.Add(btnQRLogin)

        ' ── LAYOUT POSITIONS ──
        Dim y As Integer = 36

        lblTitle.Location = New Point((cardW - lblTitle.Width) \ 2, y)
        y += lblTitle.Height + 12

        pnlAccentLine.Location = New Point((cardW - 56) \ 2, y)
        y += 6 + 14

        lblSubtitle.Location = New Point((cardW - lblSubtitle.Width) \ 2, y)
        y += lblSubtitle.Height + 44

        lblUsernameLabel.Location = New Point(contentX, y)
        y += lblUsernameLabel.Height + 10

        txtUsername.Location = New Point(contentX, y)
        y += inputH + 30

        lblPasswordLabel.Location = New Point(contentX, y)
        y += lblPasswordLabel.Height + 10

        txtPassword.Location = New Point(contentX, y)
        lblEyeToggle.Location = New Point(contentX + contentW - 38, y + 10)
        y += inputH + 10

        lnkForgotPassword.Location = New Point(contentX + contentW - lnkForgotPassword.Width, y)
        y += 34

        btnLogin.Location = New Point(contentX, y)
        y += 52 + 22

        pnlDivider.Location = New Point(contentX, y)
        y += 22 + 20

        btnQRLogin.Location = New Point(contentX, y)
    End Sub

    ' ================================================================
    '  EYE ICON HELPERS
    ' ================================================================
    Private Function CreateEyeIcon(open As Boolean) As Image
        Dim bmp As New Bitmap(20, 20)
        Using g = Graphics.FromImage(bmp)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Dim c As Color = Color.FromArgb(153, 153, 153)
            Using pen As New Pen(c, 1.8F)
                g.DrawArc(pen, 1, 5, 18, 10, 180, 180)
                g.DrawArc(pen, 1, 3, 18, 10, 0, 180)
                If open Then
                    Using brush As New SolidBrush(c)
                        g.FillEllipse(brush, 7, 6, 6, 6)
                    End Using
                Else
                    g.DrawLine(pen, 2, 14, 18, 4)
                End If
            End Using
        End Using
        Return bmp
    End Function

    Private Function CreateUserIcon(iconColor As Color) As Image
        Dim bmp As New Bitmap(20, 20)
        Using g = Graphics.FromImage(bmp)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Using pen As New Pen(iconColor, 1.6F)
                g.DrawEllipse(pen, 6, 1, 8, 8)
                g.DrawArc(pen, 2, 10, 16, 10, 180, 180)
            End Using
        End Using
        Return bmp
    End Function

    Private Function CreateLockIcon(iconColor As Color) As Image
        Dim bmp As New Bitmap(20, 20)
        Using g = Graphics.FromImage(bmp)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Using pen As New Pen(iconColor, 1.6F)
                g.DrawRectangle(pen, 4, 10, 12, 8)
                g.DrawArc(pen, 6, 3, 8, 8, 180, 180)
                g.DrawLine(pen, 6, 7, 14, 7)
            End Using
        End Using
        Return bmp
    End Function

    Private Function CreateLoginIcon(iconColor As Color) As Image
        Dim bmp As New Bitmap(20, 20)
        Using g = Graphics.FromImage(bmp)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Using pen As New Pen(iconColor, 1.8F)
                g.DrawLine(pen, 3, 10, 16, 10)
                g.DrawLine(pen, 11, 5, 16, 10)
                g.DrawLine(pen, 11, 15, 16, 10)
                g.DrawLine(pen, 7, 3, 7, 17)
            End Using
        End Using
        Return bmp
    End Function

    Private Function CreateQRIcon(iconColor As Color) As Image
        Dim bmp As New Bitmap(20, 20)
        Using g = Graphics.FromImage(bmp)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Using pen As New Pen(iconColor, 1.6F)
                g.DrawRectangle(pen, 1, 1, 7, 7)
                g.DrawRectangle(pen, 12, 1, 7, 7)
                g.DrawRectangle(pen, 1, 12, 7, 7)
                Using brush As New SolidBrush(iconColor)
                    g.FillRectangle(brush, 3, 3, 3, 3)
                    g.FillRectangle(brush, 14, 3, 3, 3)
                    g.FillRectangle(brush, 3, 14, 3, 3)
                    g.FillRectangle(brush, 13, 13, 2, 2)
                    g.FillRectangle(brush, 16, 13, 2, 2)
                    g.FillRectangle(brush, 13, 16, 2, 2)
                End Using
            End Using
        End Using
        Return bmp
    End Function

    Private Sub TogglePasswordVisibility()
        passwordVisible = Not passwordVisible
        If passwordVisible Then
            txtPassword.PasswordChar = ChrW(0)
            lblEyeToggle.Image = CreateEyeIcon(False)
        Else
            txtPassword.PasswordChar = "•"c
            lblEyeToggle.Image = CreateEyeIcon(True)
        End If
    End Sub

    Private Sub CheckForUpdateInternal()
        Try
            Dim role As String = If(LoggedInRole, "").ToUpper()
            If role <> "ADMIN" AndAlso role <> "MANAGER" Then Return

            Dim updatePath As String = ConfigurationManager.AppSettings("UpdatePath")
            If String.IsNullOrWhiteSpace(updatePath) OrElse Not Directory.Exists(updatePath) Then Return

            Dim remoteVersion As Version = AutoUpdater.CheckForUpdate(updatePath)
            If remoteVersion Is Nothing Then Return

            Dim localVersion As Version = AutoUpdater.GetCurrentVersion()
            Me.Invoke(Sub()
                          Dim result As DialogResult = MessageBox.Show(
                              $"New version {remoteVersion} available (current: {localVersion}). Update now?" & vbCrLf &
                              "The application will restart after update.",
                              "Update Available", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                          If result = DialogResult.Yes Then
                              Dim updateDir As String = Path.Combine(
                                  Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                                  "JadeClinic", "Update")
                              If Directory.Exists(updateDir) Then Directory.Delete(updateDir, True)
                              AutoUpdater.DownloadUpdate(updatePath, updateDir)
                              Dim appDir As String = AppDomain.CurrentDomain.BaseDirectory
                              AutoUpdater.ApplyUpdateAndRestart(updateDir, appDir)
                          End If
                      End Sub)
        Catch ex As Exception
            Console.WriteLine($"Update check failed: {ex.Message}")
        End Try
    End Sub

    Private Sub SetupTabIndex()
        txtUsername.TabIndex = 0
        txtPassword.TabIndex = 1
        btnLogin.TabIndex = 2
        Utilities.ApplyInputFocusEffects(Me)
    End Sub

    Private Sub CenterLoginLayout()
        If cardPanel Is Nothing OrElse picLogo Is Nothing Then Return
        Dim logoSize As Integer = 120
        Dim logoSpacing As Integer = 30
        Dim groupHeight As Integer = logoSize + logoSpacing + cardPanel.Height
        Dim groupTop As Integer = Math.Max(0, (Me.ClientSize.Height - groupHeight) \ 2)

        picLogo.Left = (Me.ClientSize.Width - logoSize) \ 2
        picLogo.Top = groupTop

        Dim cardX As Integer = (Me.ClientSize.Width - cardPanel.Width) \ 2
        Dim cardY As Integer = groupTop + logoSize + logoSpacing
        cardPanel.Left = cardX
        cardPanel.Top = cardY

        If shadowLayers IsNot Nothing Then
            Dim spreadData(,) As Integer = {
                {3, 2, 3, 8},
                {5, 3, 5, 13},
                {8, 4, 8, 18},
                {10, 5, 10, 23},
                {12, 5, 12, 28}
            }
            For i As Integer = 0 To shadowLayers.Count - 1
                shadowLayers(i).Left = cardX - spreadData(i, 0)
                shadowLayers(i).Top = cardY - spreadData(i, 1)
                shadowLayers(i).SendToBack()
            Next
        End If
    End Sub

    Private Sub InitializeDatabaseOnStartup()
        Try
            Console.WriteLine("Checking database connectivity on startup...")
            If Connection.TestConnection() Then
                Console.WriteLine("? Database connection is ready for login.")
            Else
                Console.WriteLine("? Database connection failed.")
                MessageBox.Show("Unable to connect to the database server. Please check network and SQL settings.",
                                "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            Console.WriteLine($"Database startup check error: {ex.Message}")
            MessageBox.Show($"Database connection error: {ex.Message}",
                            "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' ================================================================
    '  QR INPUT PROTECTION
    ' ================================================================
    Private Sub ProtectFromQRInput(sender As Object, e As KeyPressEventArgs)
        If Not qrScannerActive Then
            If Char.IsControl(e.KeyChar) Then Return

            Dim currentText As String = ""
            Dim newChar As String = e.KeyChar.ToString()

            If TypeOf sender Is Guna.UI2.WinForms.Guna2TextBox Then
                currentText = CType(sender, Guna.UI2.WinForms.Guna2TextBox).Text
            ElseIf TypeOf sender Is TextBox Then
                currentText = CType(sender, TextBox).Text
            Else
                Return
            End If

            Dim potentialText As String = currentText + newChar
            If IsDefiniteQRCodeInput(potentialText) Then
                e.Handled = True
                Console.WriteLine($"Blocked QR code input: '{potentialText}'")
            End If
        End If
    End Sub

    Private Sub ValidateInputForQRCodes(sender As Object, e As EventArgs)
        If Not qrScannerActive Then
            Dim text As String = ""
            If TypeOf sender Is Guna.UI2.WinForms.Guna2TextBox Then
                Dim gunaTextBox = CType(sender, Guna.UI2.WinForms.Guna2TextBox)
                text = gunaTextBox.Text
                If IsDefiniteQRCodeInput(text) Then
                    gunaTextBox.Clear()
                    Console.WriteLine($"Cleared QR code input: '{text}'")
                End If
            ElseIf TypeOf sender Is TextBox Then
                Dim textBox = CType(sender, TextBox)
                text = textBox.Text
                If IsDefiniteQRCodeInput(text) Then
                    textBox.Clear()
                    Console.WriteLine($"Cleared QR code input: '{text}'")
                End If
            End If
        End If
    End Sub

    Private Function IsDefiniteQRCodeInput(input As String) As Boolean
        If String.IsNullOrEmpty(input) Then Return False
        If input.StartsWith("User-", StringComparison.OrdinalIgnoreCase) AndAlso input.Length >= 8 Then
            Dim userIdPart As String = input.Substring(5)
            If userIdPart.All(AddressOf Char.IsDigit) AndAlso userIdPart.Length >= 3 Then Return True
        End If
        If input.Length >= 8 AndAlso input.All(AddressOf Char.IsDigit) Then Return True
        Return False
    End Function

    Private Function IsLikelyQRCodeInput(input As String) As Boolean
        If String.IsNullOrEmpty(input) Then Return False
        If input.StartsWith("User-", StringComparison.OrdinalIgnoreCase) Then Return True
        If input.Length > 3 AndAlso input.All(AddressOf Char.IsDigit) Then Return True
        If input.Length > 5 AndAlso Not input.Contains(" ") Then
            Dim alphaCount = input.Count(AddressOf Char.IsLetter)
            Dim digitCount = input.Count(AddressOf Char.IsDigit)
            Dim symbolCount = input.Count(Function(c) Not Char.IsLetterOrDigit(c))
            If (alphaCount > 0 AndAlso digitCount > 0) OrElse symbolCount > 0 Then Return True
        End If
        Return False
    End Function

    Private Sub CheckAndShowQRCode(sender As Object, e As EventArgs)
        If Not qrScannerActive Then
            Dim usernameText As String = txtUsername.Text
            Dim passwordText As String = txtPassword.Text
            If Not String.IsNullOrWhiteSpace(usernameText) AndAlso Not String.IsNullOrWhiteSpace(passwordText) Then
                ShowUserQRCode()
            End If
        End If
    End Sub

    Private Sub ShowUserQRCode()
        Try
            If qrScannerActive Then Return
            Dim username As String = txtUsername.Text.Trim()
            If String.IsNullOrWhiteSpace(username) Then Return

            Dim query As String = "SELECT UserID, FullName FROM Users WHERE Username = @Username AND IsActive = 1"
            Dim parameters As SqlParameter() = {New SqlParameter("@Username", username)}

            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
                If reader.Read() Then
                    Dim userId As Integer = Convert.ToInt32(reader("UserID"))
                    Dim fullName As String = If(IsDBNull(reader("FullName")), username, reader("FullName").ToString())
                    Dim qrCode As String = $"User-{userId:D5}"

                    Dim qrForm As New Form()
                    qrForm.Text = "Your QR Code"
                    qrForm.Size = New Size(400, 300)
                    qrForm.StartPosition = FormStartPosition.CenterParent
                    qrForm.BackColor = Color.White
                    qrForm.FormBorderStyle = FormBorderStyle.FixedDialog
                    qrForm.MaximizeBox = False
                    qrForm.MinimizeBox = False

                    Dim lblName As New Label()
                    lblName.Text = $"Name: {fullName}"
                    lblName.Font = New Font("Poppins", 12, FontStyle.Bold)
                    lblName.Location = New Point(50, 30)
                    lblName.AutoSize = True
                    qrForm.Controls.Add(lblName)

                    Dim lblQR As New Label()
                    lblQR.Text = $"QR Code: {qrCode}"
                    lblQR.Font = New Font("Courier New", 16, FontStyle.Bold)
                    lblQR.Location = New Point(50, 80)
                    lblQR.AutoSize = True
                    lblQR.ForeColor = Color.Blue
                    qrForm.Controls.Add(lblQR)

                    Dim lblInstr As New Label()
                    lblInstr.Text = "Use this code with the QR scanner to login quickly!"
                    lblInstr.Font = New Font("Poppins", 10)
                    lblInstr.Location = New Point(50, 130)
                    lblInstr.AutoSize = True
                    qrForm.Controls.Add(lblInstr)

                    Dim btnClose As New Button()
                    btnClose.Text = "Close"
                    btnClose.Size = New Size(100, 30)
                    btnClose.Location = New Point(150, 180)
                    btnClose.DialogResult = DialogResult.OK
                    qrForm.Controls.Add(btnClose)

                    Dim btnDisableQR As New Button()
                    btnDisableQR.Text = If(qrScannerEnabled, "Disable QR", "Enable QR")
                    btnDisableQR.Size = New Size(100, 30)
                    btnDisableQR.Location = New Point(150, 220)
                    AddHandler btnDisableQR.Click, Sub()
                                                       ToggleQRScanner()
                                                       btnDisableQR.Text = If(qrScannerEnabled, "Disable QR", "Enable QR")
                                                   End Sub
                    qrForm.Controls.Add(btnDisableQR)

                    qrForm.ShowDialog(Me)
                End If
            End Using
        Catch ex As Exception
            Console.WriteLine($"Error showing QR code: {ex.Message}")
        End Try
    End Sub

    Private Sub btnQRLogin_Click(sender As Object, e As EventArgs) Handles btnQRLogin.Click
        If qrScannerEnabled Then
            ShowQRScanDialog()
        Else
            MessageBox.Show("QR Scanner is currently disabled. Please use username/password login.", "QR Scanner Disabled", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub ToggleQRScanner()
        qrScannerEnabled = Not qrScannerEnabled
        If qrScannerEnabled Then
            btnQRLogin.Text = "Scan QR Code to Login"
            btnQRLogin.ForeColor = Color.FromArgb(190, 154, 48)
        Else
            btnQRLogin.Text = "QR Scanner Disabled"
            btnQRLogin.ForeColor = Color.Gray
        End If
    End Sub

    ' ================================================================
    '  QR SCAN DIALOG
    ' ================================================================
    Private Sub ShowQRScanDialog()
        qrScannerActive = True

        Dim qrDialog As New Form()
        qrDialog.Text = "QR Code Scanner - Staff Login"
        qrDialog.Size = New Size(550, 480)
        qrDialog.StartPosition = FormStartPosition.CenterParent
        qrDialog.BackColor = Color.White
        qrDialog.FormBorderStyle = FormBorderStyle.FixedDialog
        qrDialog.MaximizeBox = False
        qrDialog.MinimizeBox = False
        qrDialog.ShowIcon = False
        qrDialog.KeyPreview = True

        Dim txtQRInput As New TextBox()
        txtQRInput.Location = New Point(-1000, 10)
        txtQRInput.Size = New Size(200, 20)
        txtQRInput.BackColor = Color.FromArgb(237, 237, 237)
        txtQRInput.ForeColor = Color.FromArgb(51, 51, 51)
        txtQRInput.BorderStyle = BorderStyle.FixedSingle
        txtQRInput.TabIndex = 0
        txtQRInput.TabStop = True

        Dim lblDebug As New Label()
        lblDebug.Text = "Debug: (empty)"
        lblDebug.Font = New Font("Poppins", 8.0F, FontStyle.Regular)
        lblDebug.ForeColor = Color.FromArgb(102, 102, 102)
        lblDebug.BackColor = Color.Transparent
        lblDebug.AutoSize = True
        lblDebug.Visible = True
        lblDebug.Location = New Point(-1010, 40)

        Dim autoClearTimer As New Timer()
        autoClearTimer.Interval = 3000

        Dim lblTitle As New Label()
        lblTitle.Text = "QR Code Scanner"
        lblTitle.Font = New Font("Poppins", 18.0F, FontStyle.Bold)
        lblTitle.ForeColor = Color.FromArgb(51, 51, 51)
        lblTitle.BackColor = Color.Transparent
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(0, 70)
        qrDialog.Controls.Add(lblTitle)

        Dim lblInstruction As New Label()
        lblInstruction.Text = "Point your QR scanner at the staff QR code"
        lblInstruction.Font = New Font("Poppins", 11.0F, FontStyle.Regular)
        lblInstruction.ForeColor = Color.FromArgb(51, 51, 51)
        lblInstruction.BackColor = Color.Transparent
        lblInstruction.AutoSize = True
        lblInstruction.Location = New Point(0, 120)
        qrDialog.Controls.Add(lblInstruction)

        Dim lblInstruction2 As New Label()
        lblInstruction2.Text = "Scanner will automatically detect and process QR codes"
        lblInstruction2.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        lblInstruction2.ForeColor = Color.FromArgb(102, 102, 102)
        lblInstruction2.BackColor = Color.Transparent
        lblInstruction2.AutoSize = True
        lblInstruction2.Location = New Point(0, 150)
        qrDialog.Controls.Add(lblInstruction2)

        Dim lblStatus As New Label()
        lblStatus.Text = "Ready to scan QR code..."
        lblStatus.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        lblStatus.ForeColor = Color.FromArgb(80, 160, 80)
        lblStatus.BackColor = Color.Transparent
        lblStatus.AutoSize = True
        lblStatus.Location = New Point(0, 200)
        qrDialog.Controls.Add(lblStatus)

        Dim lblQRIndicator As New Label()
        lblQRIndicator.Text = "Scanner Active - Waiting for QR code..."
        lblQRIndicator.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        lblQRIndicator.ForeColor = Color.FromArgb(80, 160, 80)
        lblQRIndicator.BackColor = Color.Transparent
        lblQRIndicator.AutoSize = True
        lblQRIndicator.Location = New Point(0, 230)
        qrDialog.Controls.Add(lblQRIndicator)

        Dim btnClose As New Button()
        btnClose.Text = "Close Scanner"
        btnClose.Size = New Size(140, 40)
        btnClose.Location = New Point((qrDialog.ClientSize.Width - 140) / 2, 320)
        btnClose.BackColor = Color.FromArgb(220, 80, 70)
        btnClose.ForeColor = Color.White
        btnClose.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.FlatAppearance.BorderSize = 0
        btnClose.Cursor = Cursors.Hand
        AddHandler btnClose.Click, Sub()
                                       Console.WriteLine("Close button clicked")
                                       qrDialog.Close()
                                   End Sub

        AddHandler btnClose.MouseEnter, Sub()
                                            btnClose.BackColor = Color.FromArgb(190, 60, 50)
                                        End Sub
        AddHandler btnClose.MouseLeave, Sub()
                                            btnClose.BackColor = Color.FromArgb(220, 80, 70)
                                        End Sub

        qrDialog.Controls.AddRange({txtQRInput, lblDebug, btnClose})

        qrDialog.PerformLayout()
        Application.DoEvents()

        lblTitle.Location = New Point((qrDialog.ClientSize.Width - lblTitle.Width) / 2, 70)
        lblInstruction.Location = New Point((qrDialog.ClientSize.Width - lblInstruction.Width) / 2, 120)
        lblInstruction2.Location = New Point((qrDialog.ClientSize.Width - lblInstruction2.Width) / 2, 150)
        lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)
        lblQRIndicator.Location = New Point((qrDialog.ClientSize.Width - lblQRIndicator.Width) / 2, 230)

        Dim blinkTimer As New Timer()
        blinkTimer.Interval = 1000
        AddHandler blinkTimer.Tick, Sub()
                                        Try
                                            If lblQRIndicator.ForeColor = Color.FromArgb(80, 160, 80) Then
                                                lblQRIndicator.ForeColor = Color.FromArgb(102, 102, 102)
                                                lblQRIndicator.Text = "Scanner Active - Waiting for QR code..."
                                            Else
                                                lblQRIndicator.ForeColor = Color.FromArgb(80, 160, 80)
                                                lblQRIndicator.Text = "Scanner Active - Waiting for QR code..."
                                            End If
                                            lblQRIndicator.Location = New Point((qrDialog.ClientSize.Width - lblQRIndicator.Width) / 2, 230)
                                        Catch ex As Exception
                                            Console.WriteLine($"Blink timer error: {ex.Message}")
                                        End Try
                                    End Sub
        blinkTimer.Start()

        AddHandler autoClearTimer.Tick, Sub()
                                            Try
                                                autoClearTimer.Stop()
                                                If Not String.IsNullOrEmpty(txtQRInput.Text) AndAlso Not txtQRInput.Text.Trim().StartsWith("User-") Then
                                                    Console.WriteLine($"Auto-clearing: '{txtQRInput.Text}'")
                                                    txtQRInput.Clear()
                                                    lblStatus.Text = "Cleared accidental input - Ready to scan..."
                                                    lblStatus.ForeColor = Color.FromArgb(230, 150, 40)
                                                    lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)
                                                    lblDebug.Text = "Debug: Auto-cleared"

                                                    Dim resetStatusTimer As New Timer()
                                                    resetStatusTimer.Interval = 1500
                                                    AddHandler resetStatusTimer.Tick, Sub()
                                                                                          Try
                                                                                              resetStatusTimer.Stop()
                                                                                              lblStatus.Text = "Ready to scan QR code..."
                                                                                              lblStatus.ForeColor = Color.FromArgb(80, 160, 80)
                                                                                              lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)
                                                                                              lblDebug.Text = "Debug: (empty)"
                                                                                          Catch ex As Exception
                                                                                              Console.WriteLine($"Reset status timer error: {ex.Message}")
                                                                                          End Try
                                                                                      End Sub
                                                    resetStatusTimer.Start()
                                                End If
                                            Catch ex As Exception
                                                Console.WriteLine($"Auto-clear timer error: {ex.Message}")
                                            End Try
                                        End Sub

        AddHandler txtQRInput.TextChanged, Sub(s, eArgs)
                                               Try
                                                   lblDebug.Text = $"Debug: '{txtQRInput.Text}'"
                                                   autoClearTimer.Stop()
                                                   If Not String.IsNullOrEmpty(txtQRInput.Text) Then
                                                       autoClearTimer.Start()
                                                   End If
                                                   Console.WriteLine($"QR Input changed: '{txtQRInput.Text}'")
                                               Catch ex As Exception
                                                   Console.WriteLine($"TextChanged error: {ex.Message}")
                                               End Try
                                           End Sub

        AddHandler txtQRInput.KeyDown, Sub(s, eArgs)
                                           Try
                                               Console.WriteLine($"KeyDown: {eArgs.KeyCode}")

                                               If eArgs.KeyCode = Keys.Enter Then
                                                   Console.WriteLine("Enter key pressed - processing QR code")
                                                   autoClearTimer.Stop()

                                                   Dim fullInput As String = txtQRInput.Text.Trim()
                                                   Console.WriteLine($"Processing Enter key with input: '{fullInput}'")

                                                   Dim qrCode As String = ExtractQRCodeFromInput(fullInput)

                                                   If Not String.IsNullOrEmpty(qrCode) Then
                                                       Console.WriteLine($"Valid QR code found: {qrCode}")
                                                       lblStatus.Text = "Processing QR code..."
                                                       lblStatus.ForeColor = Color.FromArgb(230, 150, 40)
                                                       lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)

                                                       lblQRIndicator.Text = "Processing..."
                                                       lblQRIndicator.ForeColor = Color.FromArgb(230, 150, 40)
                                                       lblQRIndicator.Location = New Point((qrDialog.ClientSize.Width - lblQRIndicator.Width) / 2, 230)

                                                       Application.DoEvents()
                                                       Threading.Thread.Sleep(500)

                                                       If ProcessQRLogin(qrCode) Then
                                                           Console.WriteLine("QR login successful - closing dialog")
                                                           blinkTimer.Stop()
                                                           autoClearTimer.Stop()
                                                           qrDialog.Close()
                                                       Else
                                                           Console.WriteLine("QR login failed")
                                                           lblStatus.Text = "Invalid QR code. Please try again."
                                                           lblStatus.ForeColor = Color.FromArgb(220, 80, 70)
                                                           lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)

                                                           lblQRIndicator.Text = "Error - Ready for next scan"
                                                           lblQRIndicator.ForeColor = Color.FromArgb(220, 80, 70)
                                                           lblQRIndicator.Location = New Point((qrDialog.ClientSize.Width - lblQRIndicator.Width) / 2, 230)
                                                           txtQRInput.Clear()
                                                           lblDebug.Text = "Debug: (cleared after error)"

                                                           Dim resetTimer As New Timer()
                                                           resetTimer.Interval = 3000
                                                           AddHandler resetTimer.Tick, Sub()
                                                                                           Try
                                                                                               resetTimer.Stop()
                                                                                               lblStatus.Text = "Ready to scan QR code..."
                                                                                               lblStatus.ForeColor = Color.FromArgb(80, 160, 80)
                                                                                               lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)

                                                                                               lblQRIndicator.Text = "Scanner Active - Waiting for QR code..."
                                                                                               lblQRIndicator.ForeColor = Color.FromArgb(80, 160, 80)
                                                                                               lblQRIndicator.Location = New Point((qrDialog.ClientSize.Width - lblQRIndicator.Width) / 2, 230)
                                                                                               lblDebug.Text = "Debug: (empty)"
                                                                                           Catch ex As Exception
                                                                                               Console.WriteLine($"Reset timer error: {ex.Message}")
                                                                                           End Try
                                                                                       End Sub
                                                           resetTimer.Start()
                                                       End If
                                                   Else
                                                       Console.WriteLine("No valid QR code found")
                                                       lblStatus.Text = "No valid QR code detected. Please scan again."
                                                       lblStatus.ForeColor = Color.FromArgb(230, 150, 40)
                                                       lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)
                                                       txtQRInput.Clear()
                                                       lblDebug.Text = "Debug: No valid QR code"

                                                       Dim resetTimer As New Timer()
                                                       resetTimer.Interval = 2000
                                                       AddHandler resetTimer.Tick, Sub()
                                                                                       Try
                                                                                           resetTimer.Stop()
                                                                                           lblStatus.Text = "Ready to scan QR code..."
                                                                                           lblStatus.ForeColor = Color.FromArgb(80, 160, 80)
                                                                                           lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)
                                                                                           lblDebug.Text = "Debug: (empty)"
                                                                                       Catch ex As Exception
                                                                                           Console.WriteLine($"Reset timer 2 error: {ex.Message}")
                                                                                       End Try
                                                                                   End Sub
                                                       resetTimer.Start()
                                                   End If
                                                   eArgs.Handled = True
                                               End If
                                           Catch ex As Exception
                                               Console.WriteLine($"KeyDown error: {ex.Message}")
                                           End Try
                                       End Sub

        AddHandler qrDialog.FormClosed, Sub()
                                            Try
                                                Console.WriteLine("QR Dialog closing - cleaning up timers")
                                                blinkTimer.Stop()
                                                autoClearTimer.Stop()
                                                qrScannerActive = False
                                            Catch ex As Exception
                                                Console.WriteLine($"FormClosed error: {ex.Message}")
                                            End Try
                                        End Sub

        Console.WriteLine("Showing QR Dialog")
        txtQRInput.Focus()
        qrDialog.ShowDialog(Me)
        Console.WriteLine("QR Dialog closed")

        Try
            If pinPanel IsNot Nothing Then
                Me.Activate()
                Me.Focus()
                Me.ActiveControl = pinPanel
                pinPanel.Focus()
            Else
                Me.Activate()
                Me.Focus()
            End If
        Catch ex As Exception
            Console.WriteLine($"Error focusing PIN panel after QR dialog: {ex.Message}")
        End Try
    End Sub

    Private Function ExtractQRCodeFromInput(input As String) As String
        Try
            If String.IsNullOrEmpty(input) Then Return ""
            Console.WriteLine($"Extracting QR code from: '{input}'")

            Dim pattern As String = "User-\d{1,5}"
            Dim regex As New Regex(pattern)
            Dim match = regex.Match(input)

            If match.Success Then
                Console.WriteLine($"Found QR code via regex: {match.Value}")
                Return match.Value
            End If

            If input.StartsWith("User-") AndAlso input.Length >= 6 Then
                Dim userIdPart As String = input.Substring(5)
                If userIdPart.All(AddressOf Char.IsDigit) AndAlso userIdPart.Length >= 1 Then
                    Console.WriteLine($"Direct QR code match: {input}")
                    Return input
                End If
            End If

            If input.All(AddressOf Char.IsDigit) AndAlso input.Length >= 1 AndAlso input.Length <= 5 Then
                Dim qrCode As String = $"User-{input.PadLeft(5, "0"c)}"
                Console.WriteLine($"Constructed QR code from number: {qrCode}")
                Return qrCode
            End If

            Console.WriteLine($"No valid QR code found in: '{input}'")
            Return ""
        Catch ex As Exception
            Console.WriteLine($"ExtractQRCodeFromInput error: {ex.Message}")
            Return ""
        End Try
    End Function

    Private Function ProcessQRLogin(userCode As String) As Boolean
        Try
            Console.WriteLine($"Processing QR Login for: {userCode}")

            Dim userIdStr As String = userCode.Substring(5)
            Dim userId As Integer

            If Not Integer.TryParse(userIdStr, userId) Then
                Console.WriteLine("Invalid user ID format")
                failedLoginAttempts += 1
                If failedLoginAttempts >= MaxLoginAttempts Then
                    Try
                        Dim auditUser = If(String.IsNullOrEmpty(userCode), "Unknown", userCode)
                        Utilities.LogAudit(auditUser, "Too Many Login Attempts", $"Exceeded maximum login attempts ({MaxLoginAttempts}) via QR. Application closing.")
                    Catch ex As Exception
                        Console.WriteLine($"Failed to write audit on max QR attempts: {ex.Message}")
                    End Try
                    MessageBox.Show("Too many incorrect login attempts. The application will now close.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Application.Exit()
                Else
                    MessageBox.Show("Invalid QR code format.", "QR Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
                Return False
            End If

            Console.WriteLine($"Looking up user ID: {userId}")

            Dim query As String = "SELECT Username, pin, IsActive, UserRole, FullName FROM Users WHERE UserID = @UserID"
            Dim parameters As SqlParameter() = {New SqlParameter("@UserID", userId)}

            Dim username As String = Nothing
            Dim pinValue As String = Nothing
            Dim isActive As Boolean = True
            Dim userRole As String = String.Empty
            Dim fullName As String = String.Empty

            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
                If reader.Read() Then
                    username = If(IsDBNull(reader("Username")), Nothing, reader("Username").ToString())
                    pinValue = If(IsDBNull(reader("pin")), Nothing, reader("pin").ToString())
                    userRole = If(IsDBNull(reader("UserRole")), String.Empty, reader("UserRole").ToString())
                    fullName = If(IsDBNull(reader("FullName")), String.Empty, reader("FullName").ToString())
                    Try
                        If Not IsDBNull(reader("IsActive")) Then
                            isActive = Convert.ToBoolean(reader("IsActive"))
                        End If
                    Catch
                        isActive = True
                    End Try
                    Console.WriteLine($"Found user: {username}, Role: {userRole}, IsActive: {isActive}")
                End If
            End Using

            If username IsNot Nothing AndAlso pinValue IsNot Nothing Then
                If Not isActive Then
                    MessageBox.Show("This account is inactive. Please contact your administrator.", "Account Inactive", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Utilities.LogAudit(username, "QR Login Attempt (Inactive)", $"Inactive account attempted QR login: {username}")
                    Return False
                End If

                LoggedInUserID = userId
                LoggedInUsername = username
                LoggedInRole = userRole
                If Not String.IsNullOrEmpty(fullName) Then
                    LoggedInFullName = fullName
                End If

                MessageBox.Show($"QR Code scanned successfully!{vbCrLf}User: {username}{vbCrLf}Please enter your PIN.", "QR Login", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Utilities.LogAudit(username, "QR Login Attempt", $"User {username} attempted login via QR code scan")
                failedLoginAttempts = 0
                ShowPinEntryPanel(pinValue)
                Return True
            Else
                Console.WriteLine("User not found in database")
                failedLoginAttempts += 1
                If failedLoginAttempts >= MaxLoginAttempts Then
                    Try
                        Dim auditUser = If(String.IsNullOrEmpty(userCode), "Unknown", userCode)
                        Utilities.LogAudit(auditUser, "Too Many Login Attempts", $"Exceeded maximum login attempts ({MaxLoginAttempts}) via QR. Application closing.")
                    Catch ex As Exception
                        Console.WriteLine($"Failed to write audit on max QR attempts (user not found): {ex.Message}")
                    End Try
                    MessageBox.Show("Too many incorrect login attempts. The application will now close.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Application.Exit()
                Else
                    MessageBox.Show("Invalid QR code or user not found.", "QR Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End If
                Return False
            End If

        Catch ex As Exception
            Console.WriteLine($"ProcessQRLogin error: {ex.Message}")
            MessageBox.Show($"Error processing QR code: {ex.Message}", "QR Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Try
                Utilities.LogAudit("Unknown", "QR Login Error", $"Error processing QR code {userCode}: {ex.Message}")
            Catch
            End Try
            Return False
        End Try
    End Function

    Private Sub txtPassword_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            btnLogin_Click(btnLogin, EventArgs.Empty)
            e.Handled = True
        End If
    End Sub

    Private Sub frmLoginvb_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not String.IsNullOrEmpty(LoggedInUsername) Then
            Utilities.LogAudit(LoggedInUsername, "Logged Out", $"User {LoggedInUsername} logged out or application closed.")
        End If
    End Sub

    Private Sub frmLoginvb_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            Dim result As DialogResult = EscForm.ConfirmExit(Me)
            If result = DialogResult.Yes Then
                Application.Exit()
            End If
            e.Handled = True
        End If
    End Sub

    ' ================================================================
    '  LOGIN
    ' ================================================================
    Private Sub btnLogin_Click(sender As Object, e As EventArgs) Handles btnLogin.Click
        Try
            If String.IsNullOrEmpty(txtUsername.Text.Trim()) OrElse String.IsNullOrEmpty(txtPassword.Text.Trim()) Then
                MessageBox.Show("Please enter both username and password.", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim query As String = "
            SELECT UserID, Username, FullName, UserRole, pin, PasswordHash, IsActive 
            FROM Users 
            WHERE Username = @Username"

            Dim parameters As SqlParameter() = {
            New SqlParameter("@Username", txtUsername.Text.Trim())
        }

            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters)
                If reader.Read() Then
                    Dim isActiveObj = reader("IsActive")
                    Dim isActive As Boolean = True
                    Try
                        If Not IsDBNull(isActiveObj) Then
                            isActive = Convert.ToBoolean(isActiveObj)
                        End If
                    Catch
                        isActive = True
                    End Try

                    Dim usernameDb As String = reader("Username").ToString()

                    If Not isActive Then
                        MessageBox.Show("This account is inactive. Please contact your administrator.", "Account Inactive", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Utilities.LogAudit(usernameDb, "Login Attempt (Inactive)", $"Inactive account attempted to login: {usernameDb}")
                        Return
                    End If

                    Dim storedPasswordHash As String = reader("PasswordHash").ToString()
                    Dim enteredPassword As String = txtPassword.Text.Trim()
                    Dim isPasswordValid As Boolean = False

                    If storedPasswordHash.StartsWith("$2a$") OrElse storedPasswordHash.StartsWith("$2b$") Then
                        isPasswordValid = BCrypt.Net.BCrypt.Verify(enteredPassword, storedPasswordHash)
                    Else
                        Dim enteredPasswordSHA256 As String = HashPasswordSHA256(enteredPassword)
                        isPasswordValid = (enteredPasswordSHA256 = storedPasswordHash)
                        If isPasswordValid Then
                            UpgradeUserPasswordToBCrypt(txtUsername.Text.Trim(), enteredPassword)
                        End If
                    End If

                    If isPasswordValid Then
                        failedLoginAttempts = 0
                        failedPinAttempts = 0

                        LoggedInUserID = Convert.ToInt32(reader("UserID"))
                        LoggedInUsername = reader("Username").ToString()
                        LoggedInFullName = reader("FullName").ToString()
                        LoggedInRole = reader("UserRole").ToString()
                        Dim pinValue As String = reader("pin").ToString()

                        Utilities.LogAudit(LoggedInUsername, "Login", "User logged in successfully")
                        ShowPinEntryPanel(pinValue)
                    Else
                        failedLoginAttempts += 1

                        If failedLoginAttempts >= MaxLoginAttempts Then
                            Try
                                Dim auditUser = If(String.IsNullOrEmpty(txtUsername.Text.Trim()), "Unknown", txtUsername.Text.Trim())
                                Utilities.LogAudit(auditUser, "Too Many Login Attempts", $"User exceeded maximum login attempts ({MaxLoginAttempts}). Application closing.")
                            Catch ex As Exception
                                Console.WriteLine($"Failed to write audit on max login attempts: {ex.Message}")
                            End Try
                            MessageBox.Show("Too many incorrect login attempts. The application will now close.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error)
                            Application.Exit()
                        Else
                            MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        End If
                    End If
                Else
                    failedLoginAttempts += 1

                    If failedLoginAttempts >= MaxLoginAttempts Then
                        Try
                            Dim auditUser = If(String.IsNullOrEmpty(txtUsername.Text.Trim()), "Unknown", txtUsername.Text.Trim())
                            Utilities.LogAudit(auditUser, "Too Many Login Attempts", $"User exceeded maximum login attempts ({MaxLoginAttempts}). Application closing.")
                        Catch ex As Exception
                            Console.WriteLine($"Failed to write audit on max login attempts (username not found): {ex.Message}")
                        End Try
                        MessageBox.Show("Too many incorrect login attempts. The application will now close.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Application.Exit()
                    Else
                        MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                End If
            End Using

        Catch ex As Exception
            MessageBox.Show($"Login error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Public Shared Function HashPassword(password As String) As String
        Return BCrypt.Net.BCrypt.HashPassword(password, 12)
    End Function

    Private Function HashPasswordSHA256(password As String) As String
        Using sha256 As SHA256 = SHA256.Create()
            Dim bytes As Byte() = Encoding.UTF8.GetBytes(password)
            Dim hash As Byte() = sha256.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function

    Private Sub UpgradeUserPasswordToBCrypt(username As String, plainPassword As String)
        Try
            Dim newBCryptHash As String = HashPassword(plainPassword)
            Dim updateQuery As String = "UPDATE Users SET PasswordHash = @NewHash WHERE Username = @Username"
            Dim updateParams As SqlParameter() = {
                New SqlParameter("@NewHash", newBCryptHash),
                New SqlParameter("@Username", username)
            }
            Utilities.ExecuteNonQuery(updateQuery, updateParams)
            Console.WriteLine($"Successfully upgraded password for user: {username} to BCrypt")
            Utilities.LogAudit(username, "Password Upgraded", "Password hash upgraded from SHA256 to BCrypt")
        Catch ex As Exception
            Console.WriteLine($"Error upgrading password for {username}: {ex.Message}")
        End Try
    End Sub

    ' ================================================================
    '  PIN ENTRY PANEL
    ' ================================================================
    Private Sub ShowPinEntryPanel(expectedPin As String)
        pinPanel = New Guna.UI2.WinForms.Guna2Panel()
        pinPanel.Size = cardPanel.Size
        pinPanel.BorderRadius = 22
        pinPanel.FillColor = Color.FromArgb(250, 250, 249)
        pinPanel.Location = cardPanel.Location
        pinPanel.TabStop = True

        Dim lblPinTitle As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblPinTitle.Text = "Enter your PIN"
        lblPinTitle.Font = New Font("Poppins SemiBold", 18.0F, FontStyle.Regular)
        lblPinTitle.ForeColor = Color.FromArgb(51, 51, 51)
        lblPinTitle.AutoSize = True
        lblPinTitle.Location = New Point((pinPanel.Width - lblPinTitle.Width) \ 2, 30)
        pinPanel.Controls.Add(lblPinTitle)

        Dim pinIndicators As New List(Of Guna.UI2.WinForms.Guna2CircleButton)()
        Dim indicatorSize As Integer = 32
        Dim indicatorSpacing As Integer = 25
        Dim indicatorStartX As Integer = (pinPanel.Width - (indicatorSize * 4 + indicatorSpacing * 3)) \ 2
        For i = 0 To 3
            Dim indicator As New Guna.UI2.WinForms.Guna2CircleButton()
            indicator.Size = New Size(indicatorSize, indicatorSize)
            indicator.FillColor = Color.FromArgb(237, 237, 237)
            indicator.BackColor = Color.FromArgb(250, 250, 249)
            indicator.BorderColor = Color.FromArgb(200, 200, 200)
            indicator.Location = New Point(indicatorStartX + i * (indicatorSize + indicatorSpacing), 90)
            pinIndicators.Add(indicator)
            pinPanel.Controls.Add(indicator)
        Next

        Dim btnBack As New Guna.UI2.WinForms.Guna2Button()
        btnBack.Text = "<"
        btnBack.Font = New Font("Poppins SemiBold", 16.0F, FontStyle.Regular)
        btnBack.Size = New Size(50, 50)
        btnBack.BorderRadius = 10
        btnBack.FillColor = Color.FromArgb(237, 237, 237)
        btnBack.ForeColor = Color.FromArgb(51, 51, 51)
        btnBack.BackColor = Color.FromArgb(250, 250, 249)
        btnBack.Location = New Point(20, 20)
        AddHandler btnBack.Click, Sub()
                                      Me.Controls.Remove(pinPanel)
                                      LoggedInUsername = Nothing
                                      pinInput = ""
                                  End Sub
        pinPanel.Controls.Add(btnBack)

        Dim buttonSize As Integer = 80
        Dim buttonSpacing As Integer = 18
        Dim buttonStartX As Integer = (pinPanel.Width - (buttonSize * 3 + buttonSpacing * 2)) \ 2
        Dim buttonStartY As Integer = 160
        Dim buttonTexts As String() = {"1", "2", "3", "4", "5", "6", "7", "8", "9", "0", "X"}

        pinPanelButtons = New List(Of Guna.UI2.WinForms.Guna2Button)()

        For i = 0 To buttonTexts.Length - 1
            Dim button As New Guna.UI2.WinForms.Guna2Button()
            button.Size = New Size(buttonSize, buttonSize)
            button.BorderRadius = 16
            button.FillColor = Color.FromArgb(237, 237, 237)
            button.BackColor = Color.FromArgb(250, 250, 249)
            button.ForeColor = Color.FromArgb(51, 51, 51)
            button.Font = New Font("Poppins SemiBold", 18.0F, FontStyle.Regular)
            button.Text = buttonTexts(i)

            If button.Text = "X" Then
                button.ForeColor = Color.FromArgb(220, 80, 70)
            End If

            Dim row = i \ 3
            Dim col = i Mod 3
            button.Location = New Point(buttonStartX + col * (buttonSize + buttonSpacing), buttonStartY + row * (buttonSize + buttonSpacing))

            AddHandler button.MouseEnter, Sub()
                                              Try
                                                  If button.Text = "X" Then
                                                      button.FillColor = Color.FromArgb(220, 80, 70)
                                                      button.ForeColor = Color.White
                                                  Else
                                                      button.FillColor = Color.FromArgb(254, 191, 16)
                                                      button.ForeColor = Color.FromArgb(51, 51, 51)
                                                  End If
                                              Catch
                                              End Try
                                          End Sub
            AddHandler button.MouseLeave, Sub()
                                              Try
                                                  button.FillColor = Color.FromArgb(237, 237, 237)
                                                  If button.Text = "X" Then
                                                      button.ForeColor = Color.FromArgb(220, 80, 70)
                                                  Else
                                                      button.ForeColor = Color.FromArgb(51, 51, 51)
                                                  End If
                                              Catch
                                              End Try
                                          End Sub

            AddHandler button.Click, Sub(senderBtn, eBtn)
                                         HandlePinInput(CType(senderBtn, Guna.UI2.WinForms.Guna2Button).Text, expectedPin, pinIndicators, pinPanel)
                                         pinPanel.Focus()
                                     End Sub

            pinPanel.Controls.Add(button)
            pinPanelButtons.Add(button)
        Next

        Dim lblForgotPin As New Label()
        lblForgotPin.Text = "Forgot PIN?"
        lblForgotPin.Font = New Font("Poppins", 10.0F, FontStyle.Underline)
        lblForgotPin.ForeColor = Color.FromArgb(254, 191, 16)
        lblForgotPin.BackColor = Color.FromArgb(250, 250, 249)
        lblForgotPin.AutoSize = True
        lblForgotPin.Cursor = Cursors.Hand
        lblForgotPin.Location = New Point((pinPanel.Width - 90) \ 2, buttonStartY + 4 * (buttonSize + buttonSpacing) + 8)
        AddHandler lblForgotPin.Click,
            Sub()
                If String.IsNullOrWhiteSpace(LoggedInUsername) Then
                    MessageBox.Show("Session not found. Please login again.", "Forgot PIN", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Return
                End If

                Dim resetOk As Boolean = ShowForgotPinDialog(LoggedInUsername)
                If resetOk Then
                    Try
                        Me.Controls.Remove(pinPanel)
                    Catch
                    End Try
                    pinInput = ""
                    failedPinAttempts = 0
                    MessageBox.Show("PIN reset successful. Please login again.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If
            End Sub
        pinPanel.Controls.Add(lblForgotPin)

        AddHandler pinPanel.KeyDown, Sub(senderObj, eArgs)
                                         Dim key As Keys = eArgs.KeyCode
                                         If key >= Keys.D0 And key <= Keys.D9 Then
                                             HandlePinInput((key - Keys.D0).ToString(), expectedPin, pinIndicators, pinPanel)
                                         ElseIf key >= Keys.NumPad0 And key <= Keys.NumPad9 Then
                                             HandlePinInput((key - Keys.NumPad0).ToString(), expectedPin, pinIndicators, pinPanel)
                                         ElseIf key = Keys.Back Or key = Keys.Delete Then
                                             HandlePinInput("X", expectedPin, pinIndicators, pinPanel)
                                         ElseIf key = Keys.Enter Or key = Keys.Return Then
                                             HandlePinInput("ENTER", expectedPin, pinIndicators, pinPanel)
                                         End If
                                     End Sub

        Me.Controls.Add(pinPanel)
        pinPanel.BringToFront()
        Me.ActiveControl = pinPanel
        pinPanel.Focus()
    End Sub

    Private Function ShowForgotPinDialog(targetUsername As String) As Boolean
        If String.IsNullOrWhiteSpace(targetUsername) Then Return False

        Dim dlg As New Form With {
            .Text = "Forgot PIN",
            .Size = New Size(560, 430),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .BackColor = Color.White,
            .KeyPreview = True
        }

        Dim currentStep As Integer = 1
        Dim success As Boolean = False

        Dim lblTitle As New Label With {
            .Text = "RESET PIN",
            .Font = New Font("Poppins", 16, FontStyle.Bold),
            .ForeColor = Color.FromArgb(51, 51, 51),
            .AutoSize = False,
            .Size = New Size(520, 34),
            .Location = New Point(20, 14),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        dlg.Controls.Add(lblTitle)

        Dim lblUser As New Label With {
            .Text = $"User: {targetUsername}",
            .Font = New Font("Poppins", 9, FontStyle.Regular),
            .ForeColor = Color.FromArgb(102, 102, 102),
            .AutoSize = False,
            .Size = New Size(520, 24),
            .Location = New Point(20, 48),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        dlg.Controls.Add(lblUser)

        Dim lblStep1 As New Label With {
            .Text = "1. Verify Passkeys",
            .Font = New Font("Poppins", 9, FontStyle.Bold),
            .ForeColor = Color.FromArgb(254, 191, 16),
            .AutoSize = True,
            .BackColor = Color.Transparent,
            .Location = New Point(90, 84)
        }
        Dim lblStep2 As New Label With {
            .Text = "2. Set New PIN",
            .Font = New Font("Poppins", 9, FontStyle.Bold),
            .ForeColor = Color.FromArgb(102, 102, 102),
            .AutoSize = True,
            .BackColor = Color.Transparent,
            .Location = New Point(360, 84)
        }
        Dim stepLine As New Panel With {
            .Size = New Size(120, 2),
            .Location = New Point(220, 94),
            .BackColor = Color.FromArgb(200, 200, 200)
        }
        dlg.Controls.Add(lblStep1)
        dlg.Controls.Add(stepLine)
        dlg.Controls.Add(lblStep2)

        Dim lblInstruction As New Label With {
            .Text = "",
            .Font = New Font("Poppins", 10, FontStyle.Regular),
            .ForeColor = Color.FromArgb(51, 51, 51),
            .AutoSize = False,
            .Size = New Size(520, 28),
            .Location = New Point(20, 112),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        dlg.Controls.Add(lblInstruction)

        Dim txtK1 As New TextBox With {.Location = New Point(50, 154), .Size = New Size(460, 30), .TextAlign = HorizontalAlignment.Center, .BackColor = Color.FromArgb(237, 237, 237), .ForeColor = Color.FromArgb(51, 51, 51)}
        Dim txtK2 As New TextBox With {.Location = New Point(50, 194), .Size = New Size(460, 30), .TextAlign = HorizontalAlignment.Center, .BackColor = Color.FromArgb(237, 237, 237), .ForeColor = Color.FromArgb(51, 51, 51)}
        Dim txtK3 As New TextBox With {.Location = New Point(50, 234), .Size = New Size(460, 30), .TextAlign = HorizontalAlignment.Center, .BackColor = Color.FromArgb(237, 237, 237), .ForeColor = Color.FromArgb(51, 51, 51)}
        Try
            txtK1.PlaceholderText = "Passkey 1"
            txtK2.PlaceholderText = "Passkey 2"
            txtK3.PlaceholderText = "Passkey 3"
        Catch
        End Try
        dlg.Controls.Add(txtK1)
        dlg.Controls.Add(txtK2)
        dlg.Controls.Add(txtK3)

        Dim txtNewPin As New TextBox With {
            .Location = New Point(50, 174),
            .Size = New Size(220, 30),
            .TextAlign = HorizontalAlignment.Center,
            .MaxLength = 4,
            .UseSystemPasswordChar = True,
            .Visible = False,
            .BackColor = Color.FromArgb(237, 237, 237),
            .ForeColor = Color.FromArgb(51, 51, 51)
        }
        Dim txtConfirmPin As New TextBox With {
            .Location = New Point(290, 174),
            .Size = New Size(220, 30),
            .TextAlign = HorizontalAlignment.Center,
            .MaxLength = 4,
            .UseSystemPasswordChar = True,
            .Visible = False,
            .BackColor = Color.FromArgb(237, 237, 237),
            .ForeColor = Color.FromArgb(51, 51, 51)
        }
        Try
            txtNewPin.PlaceholderText = "New 4-digit PIN"
            txtConfirmPin.PlaceholderText = "Confirm PIN"
        Catch
        End Try
        AddHandler txtNewPin.KeyPress, Sub(s, e)
                                           If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) Then
                                               e.Handled = True
                                           End If
                                       End Sub
        AddHandler txtConfirmPin.KeyPress, Sub(s, e)
                                               If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) Then
                                                   e.Handled = True
                                               End If
                                           End Sub
        dlg.Controls.Add(txtNewPin)
        dlg.Controls.Add(txtConfirmPin)

        Dim lblStatus As New Label With {
            .Text = "",
            .ForeColor = Color.FromArgb(220, 80, 70),
            .AutoSize = False,
            .Size = New Size(520, 24),
            .Location = New Point(20, 274),
            .TextAlign = ContentAlignment.MiddleCenter,
            .BackColor = Color.Transparent
        }
        dlg.Controls.Add(lblStatus)

        Dim btnBack As New Button With {
            .Text = "Back",
            .Size = New Size(110, 38),
            .Location = New Point(120, 318),
            .BackColor = Color.FromArgb(237, 237, 237),
            .ForeColor = Color.FromArgb(51, 51, 51),
            .FlatStyle = FlatStyle.Flat,
            .Visible = False
        }
        btnBack.FlatAppearance.BorderSize = 0

        Dim btnNext As New Button With {
            .Text = "Next",
            .Size = New Size(110, 38),
            .Location = New Point(240, 318),
            .BackColor = Color.FromArgb(254, 191, 16),
            .ForeColor = Color.FromArgb(51, 51, 51),
            .FlatStyle = FlatStyle.Flat
        }
        btnNext.FlatAppearance.BorderSize = 0

        Dim btnCancel As New Button With {
            .Text = "Cancel",
            .Size = New Size(110, 38),
            .Location = New Point(360, 318),
            .BackColor = Color.FromArgb(220, 80, 70),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnCancel.FlatAppearance.BorderSize = 0

        dlg.Controls.Add(btnBack)
        dlg.Controls.Add(btnNext)
        dlg.Controls.Add(btnCancel)

        Dim setStep As Action(Of Integer) =
        Sub(stepNo As Integer)
            currentStep = stepNo
            lblStatus.Text = ""

            If currentStep = 1 Then
                lblInstruction.Text = "Enter your three recovery passkeys."
                lblStep1.ForeColor = Color.FromArgb(254, 191, 16)
                lblStep2.ForeColor = Color.FromArgb(102, 102, 102)
                stepLine.BackColor = Color.FromArgb(200, 200, 200)

                txtK1.Visible = True
                txtK2.Visible = True
                txtK3.Visible = True
                txtNewPin.Visible = False
                txtConfirmPin.Visible = False

                btnBack.Visible = False
                btnNext.Text = "Next"
                txtK1.Focus()
            Else
                lblInstruction.Text = "Create and confirm your new 4-digit PIN."
                lblStep1.ForeColor = Color.FromArgb(80, 160, 80)
                lblStep2.ForeColor = Color.FromArgb(254, 191, 16)
                stepLine.BackColor = Color.FromArgb(254, 191, 16)

                txtK1.Visible = False
                txtK2.Visible = False
                txtK3.Visible = False
                txtNewPin.Visible = True
                txtConfirmPin.Visible = True

                btnBack.Visible = True
                btnNext.Text = "Reset PIN"
                txtNewPin.Focus()
            End If
        End Sub

        AddHandler btnCancel.Click, Sub()
                                        dlg.DialogResult = DialogResult.Cancel
                                        dlg.Close()
                                    End Sub

        AddHandler btnBack.Click, Sub()
                                      setStep(1)
                                  End Sub

        AddHandler btnNext.Click,
        Sub()
            If currentStep = 1 Then
                Dim k1 = txtK1.Text.Trim().ToUpperInvariant()
                Dim k2 = txtK2.Text.Trim().ToUpperInvariant()
                Dim k3 = txtK3.Text.Trim().ToUpperInvariant()

                If String.IsNullOrWhiteSpace(k1) OrElse String.IsNullOrWhiteSpace(k2) OrElse String.IsNullOrWhiteSpace(k3) Then
                    lblStatus.Text = "Please enter all 3 passkeys."
                    Return
                End If

                If k1 = k2 OrElse k1 = k3 OrElse k2 = k3 Then
                    lblStatus.Text = "Passkeys must be different."
                    Return
                End If

                If Not VerifyThreePasskeysForUser(targetUsername, k1, k2, k3) Then
                    lblStatus.Text = "Incorrect passkeys."
                    Return
                End If

                setStep(2)
            Else
                Dim np = txtNewPin.Text.Trim()
                Dim cp = txtConfirmPin.Text.Trim()

                If np.Length <> 4 OrElse Not np.All(AddressOf Char.IsDigit) Then
                    lblStatus.Text = "New PIN must be exactly 4 digits."
                    Return
                End If

                If np <> cp Then
                    lblStatus.Text = "PIN confirmation does not match."
                    Return
                End If

                If UpdateUserPinByUsername(targetUsername, np) Then
                    Try
                        Utilities.LogAudit(targetUsername, "PIN Reset via Passkeys", "User reset PIN using 3 passkeys")
                    Catch
                    End Try
                    success = True
                    dlg.DialogResult = DialogResult.OK
                    dlg.Close()
                Else
                    lblStatus.Text = "Failed to update PIN."
                End If
            End If
        End Sub

        AddHandler dlg.KeyDown,
        Sub(s, e)
            If e.KeyCode = Keys.Escape Then
                btnCancel.PerformClick()
            ElseIf e.KeyCode = Keys.Enter Then
                btnNext.PerformClick()
            End If
        End Sub

        setStep(1)
        dlg.ShowDialog(Me)
        Return success
    End Function

    Private Function VerifyThreePasskeysForUser(targetUsername As String, p1 As String, p2 As String, p3 As String) As Boolean
        Try
            Dim query As String = "SELECT Passkeys FROM Users WHERE Username = @Username AND IsActive = 1"
            Dim stored As New List(Of String)()

            Using reader As DbDataReader = Utilities.ExecuteReader(query, {New SqlParameter("@Username", targetUsername)})
                If reader.Read() Then
                    If IsDBNull(reader("Passkeys")) Then Return False
                    Dim raw As String = reader("Passkeys").ToString()
                    stored = raw.Split(","c).
                         Select(Function(s) s.Trim().ToUpperInvariant()).
                         Where(Function(s) Not String.IsNullOrEmpty(s)).
                         ToList()
                End If
            End Using

            If stored.Count <> 3 Then Return False

            Dim inputKeys As New List(Of String) From {
                p1.Trim().ToUpperInvariant(),
                p2.Trim().ToUpperInvariant(),
                p3.Trim().ToUpperInvariant()
            }

            inputKeys.Sort()
            stored.Sort()

            Return inputKeys.SequenceEqual(stored)
        Catch ex As Exception
            Console.WriteLine($"VerifyThreePasskeysForUser error: {ex.Message}")
            Return False
        End Try
    End Function

    Private Function UpdateUserPinByUsername(targetUsername As String, newPin As String) As Boolean
        Try
            Dim q As String = "UPDATE Users SET pin = @Pin WHERE Username = @Username"
            Dim rows = Utilities.ExecuteNonQuery(q, {
                New SqlParameter("@Pin", Convert.ToInt32(newPin)),
                New SqlParameter("@Username", targetUsername)
            })
            Return rows > 0
        Catch
            Return False
        End Try
    End Function

    Private Sub ValidatePin(expectedPin As String, pinIndicators As List(Of Guna.UI2.WinForms.Guna2CircleButton), pinPanel As Control)
        Const MaxPinAttempts As Integer = 3

        If pinInput = expectedPin Then
            failedPinAttempts = 0

            LoggedInPIN = pinInput
            Utilities.LogAudit(LoggedInUsername, "Logged In", $"User {LoggedInUsername} successfully logged in at {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.Controls.Remove(pinPanel)
            Me.Hide()
            pinInput = ""

            Try
                Console.WriteLine($"Routing user {LoggedInUsername} with role: {LoggedInRole}")
                Dim userRole As String = If(LoggedInRole, "Staff").ToUpper()

                Dim shell As New MainShell()
                shell.Show()
                shell.ShowInitialPage()

                Task.Run(Sub() CheckForUpdateInternal())

            Catch ex As Exception
                Console.WriteLine($"Error showing target form: {ex.Message}")
                Console.WriteLine($"Stack trace: {ex.StackTrace}")
                Me.Show()
                MessageBox.Show($"Error opening application: {ex.Message}{vbCrLf}{vbCrLf}Please try logging in again.",
                          "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        Else
            failedPinAttempts += 1

            If failedPinAttempts >= MaxPinAttempts Then
                Try
                    Dim auditUser As String = If(String.IsNullOrEmpty(LoggedInUsername), "Unknown", LoggedInUsername)
                    Utilities.LogAudit(auditUser, "Too Many PIN Attempts", $"User exceeded maximum PIN attempts ({MaxPinAttempts}). Application closing.")
                Catch ex As Exception
                    Console.WriteLine($"Failed to write audit on max PIN attempts: {ex.Message}")
                End Try
                MessageBox.Show("Too many incorrect PIN attempts. The application will now close.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Application.Exit()
            Else
                MessageBox.Show("Incorrect PIN.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                pinInput = ""
                For Each indicator In pinIndicators
                    indicator.FillColor = Color.FromArgb(237, 237, 237)
                Next
            End If
        End If
    End Sub

    Public Shared Sub LogoutUser()
        If Not String.IsNullOrEmpty(LoggedInUsername) Then
            Utilities.LogAudit(LoggedInUsername, "Logged Out", $"User {LoggedInUsername} logged out at {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            LoggedInUsername = Nothing
            LoggedInPIN = Nothing
        End If
    End Sub

    Private Sub HandlePinInput(input As String, expectedPin As String, pinIndicators As List(Of Guna.UI2.WinForms.Guna2CircleButton), pinPanel As Control)
        If input = "X" Then
            If pinInput.Length > 0 Then
                pinInput = pinInput.Substring(0, pinInput.Length - 1)
                pinIndicators(pinInput.Length).FillColor = Color.FromArgb(237, 237, 237)
            End If
        ElseIf input = "ENTER" Then
            If pinInput.Length = 4 Then
                ValidatePin(expectedPin, pinIndicators, pinPanel)
            End If
        ElseIf input >= "0" And input <= "9" Then
            If pinInput.Length < 4 Then
                pinInput &= input
                pinIndicators(pinInput.Length - 1).FillColor = Color.FromArgb(254, 191, 16)
            End If
            If pinInput.Length = 4 Then
                ValidatePin(expectedPin, pinIndicators, pinPanel)
            End If
        End If
    End Sub

    Private Sub lnkForgotPassword_Click(sender As Object, e As EventArgs) Handles lnkForgotPassword.Click
        Try
            Dim forgotForm As New ForgotPasswordForm

            Try
                Dim initialUsername = txtUsername.Text.Trim
                If Not String.IsNullOrEmpty(initialUsername) Then
                    Dim prop = forgotForm.GetType.GetProperty("InitialUsername")
                    If prop IsNot Nothing AndAlso prop.CanWrite Then
                        prop.SetValue(forgotForm, initialUsername)
                    End If
                End If
            Catch ex As Exception
                Console.WriteLine($"Prefill attempt failed: {ex.Message}")
            End Try

            forgotForm.ShowDialog(Me)
        Catch ex As Exception
            Console.WriteLine($"Error opening Forgot Password dialog: {ex.Message}")
            MessageBox.Show("Unable to open the Forgot Password dialog. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
