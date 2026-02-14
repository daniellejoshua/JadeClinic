Imports Microsoft.Data.SqlClient
Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Security.Cryptography
Imports System.Text
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

    Private pinInput As String = "" ' Class-level for consistency

    ' Dynamic color properties that use the theme system
    Private ReadOnly Property GoldenYellow As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("PrimaryColor")
        End Get
    End Property

    Private ReadOnly Property RichOlive As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("SecondaryColor")
        End Get
    End Property

    Private ReadOnly Property DeepCharcoal As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("BackgroundDark")
        End Get
    End Property

    Private ReadOnly Property DarkSlate As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("BackgroundMid")
        End Get
    End Property

    Private ReadOnly Property Graphite As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("BackgroundLight")
        End Get
    End Property

    Private ReadOnly Property SteelGray As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("InteractiveColor")
        End Get
    End Property

    Private ReadOnly Property PureWhite As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("TextPrimary")
        End Get
    End Property

    Private ReadOnly Property LightSilver As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("TextSecondary")
        End Get
    End Property

    Private ReadOnly Property SuccessGreen As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("SuccessColor")
        End Get
    End Property

    Private ReadOnly Property AlertRed As Color
        Get
            Return CompanySettingsManager.Instance.GetColor("ErrorColor")
        End Get
    End Property
    Private Sub ApplyCustomTheme()
        ' Apply colors using CompanySettingsManager directly
        Me.BackColor = CompanySettingsManager.Instance.GetColor("BackgroundDark")  ' Deep Charcoal
        Guna2Panel1.FillColor = CompanySettingsManager.Instance.GetColor("BackgroundMid")  ' Dark Slate
        Guna2Panel1.BorderColor = CompanySettingsManager.Instance.GetColor("PrimaryColor")  ' Golden Yellow

        ' Apply to buttons and labels
        BtnLogin.FillColor = CompanySettingsManager.Instance.GetColor("PrimaryColor")  ' Golden Yellow
        BtnLogin.ForeColor = CompanySettingsManager.Instance.GetColor("BackgroundDark")  ' Deep Charcoal
        Guna2CheckBox1.ForeColor = CompanySettingsManager.Instance.GetColor("TextSecondary")  ' Light Silver
        lblForgotPass.ForeColor = CompanySettingsManager.Instance.GetColor("TextSecondary")  ' Light Silver
        Guna2HtmlLabel5.ForeColor = CompanySettingsManager.Instance.GetColor("TextPrimary")  ' Pure White

        ' Apply to PictureBox1 (logo) - keep as is or adjust if needed
        ' PictureBox1.BackColor = CompanySettingsManager.Instance.GetColor("BackgroundDark")

        ' Debug message (optional - remove after testing)
        MessageBox.Show("Theme applied to Login form!", "Theme Applied")
    End Sub
    Private Sub frmLoginvb_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Guna2Panel1.BorderRadius = 50
        Me.MaximizeBox = False
        ApplyCustomTheme()

        ' Center Guna2Panel1
        Guna2Panel1.Left = (Me.ClientSize.Width - Guna2Panel1.Width) \ 2

        ' Center PictureBox1 above the panel
        PictureBox1.Left = (Me.ClientSize.Width - PictureBox1.Width) \ 2

        ' Set password char to bullet on form load
        txtPassword.PasswordChar = "•"c

        ' Initialize database on form load - CRITICAL FIX!
        InitializeDatabaseOnStartup()

        ' Add Enter key support for login
        AddHandler txtPassword.KeyDown, AddressOf txtPassword_KeyDown

        ' Add click handler for QR login label
        AddHandler Guna2HtmlLabel5.Click, AddressOf Guna2HtmlLabel5_Click

        ' Add hover effect for QR login label with proper cursor
        AddHandler Guna2HtmlLabel5.MouseEnter, Sub()
                                                   Guna2HtmlLabel5.ForeColor = Color.FromArgb(255, 204, 77) ' Orange color on hover
                                                   Guna2HtmlLabel5.Cursor = Cursors.Hand ' Hand cursor on hover
                                               End Sub
        AddHandler Guna2HtmlLabel5.MouseLeave, Sub()
                                                   Guna2HtmlLabel5.ForeColor = Color.White ' Back to white
                                                   Guna2HtmlLabel5.Cursor = Cursors.Default ' Default cursor
                                               End Sub
    End Sub

    ' Initialize database on startup - THIS FIXES THE ERROR!
    Private Sub InitializeDatabaseOnStartup()
        Try
            Console.WriteLine("Initializing database on startup...")

            ' Test if database initialization is needed
            If Connection.InitializeDatabase() Then
                Console.WriteLine("✅ Database is ready for login!")
            Else
                Console.WriteLine("❌ Database initialization failed!")
                MessageBox.Show("Database initialization failed. Please check your LocalDB installation.",
                              "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

        Catch ex As Exception
            Console.WriteLine($"Database initialization error: {ex.Message}")
            MessageBox.Show($"Database initialization error: {ex.Message}" & vbCrLf &
                          "Please ensure SQL Server LocalDB is installed.",
                          "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Event handler for QR login label click
    Private Sub Guna2HtmlLabel5_Click(sender As Object, e As EventArgs)
        ShowQRScanDialog()
    End Sub

    ' Create and show QR scan dialog (QR scanner only, no manual typing)
    Private Sub ShowQRScanDialog()
        ' Create QR scan dialog form
        Dim qrDialog As New Form()
        qrDialog.Text = "QR Code Scanner - Staff Login"
        qrDialog.Size = New Size(550, 480)
        qrDialog.StartPosition = FormStartPosition.CenterParent
        qrDialog.BackColor = Color.FromArgb(41, 44, 45)
        qrDialog.FormBorderStyle = FormBorderStyle.FixedDialog
        qrDialog.MaximizeBox = False
        qrDialog.MinimizeBox = False
        qrDialog.ShowIcon = False
        qrDialog.KeyPreview = True ' Enable key preview for the dialog

        ' Create QR input textbox (hidden for scanner input only)
        Dim txtQRInput As New TextBox()
        txtQRInput.Location = New Point(10, 10) ' Visible for debugging
        txtQRInput.Size = New Size(200, 20)
        txtQRInput.BackColor = Color.FromArgb(61, 65, 66)
        txtQRInput.ForeColor = Color.White
        txtQRInput.BorderStyle = BorderStyle.FixedSingle
        txtQRInput.TabIndex = 0
        txtQRInput.TabStop = True

        ' Debug label to show what's being typed
        Dim lblDebug As New Label()
        lblDebug.Text = "Debug: (empty)"
        lblDebug.Font = New Font("Poppins", 8.0F, FontStyle.Regular)
        lblDebug.ForeColor = Color.Yellow
        lblDebug.BackColor = Color.Transparent
        lblDebug.AutoSize = True
        lblDebug.Visible = True ' Made visible for debugging
        lblDebug.Location = New Point(10, 40)

        ' Auto-clear timer to clear accidentally typed text
        Dim autoClearTimer As New Timer()
        autoClearTimer.Interval = 3000 ' Clear after 3 seconds of inactivity

        ' Title label
        Dim lblTitle As New Label()
        lblTitle.Text = "🔍 QR Code Scanner"
        lblTitle.Font = New Font("Poppins", 18.0F, FontStyle.Bold)
        lblTitle.ForeColor = Color.White
        lblTitle.BackColor = Color.Transparent
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(0, 70) ' Temporary position
        qrDialog.Controls.Add(lblTitle)

        ' Instruction label
        Dim lblInstruction As New Label()
        lblInstruction.Text = "📱 Point your QR scanner at the staff QR code"
        lblInstruction.Font = New Font("Poppins", 11.0F, FontStyle.Regular)
        lblInstruction.ForeColor = Color.FromArgb(255, 204, 77)
        lblInstruction.BackColor = Color.Transparent
        lblInstruction.AutoSize = True
        lblInstruction.Location = New Point(0, 120) ' Temporary position
        qrDialog.Controls.Add(lblInstruction)

        ' Secondary instruction
        Dim lblInstruction2 As New Label()
        lblInstruction2.Text = "Scanner will automatically detect and process QR codes"
        lblInstruction2.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        lblInstruction2.ForeColor = Color.LightGray
        lblInstruction2.BackColor = Color.Transparent
        lblInstruction2.AutoSize = True
        lblInstruction2.Location = New Point(0, 150) ' Temporary position
        qrDialog.Controls.Add(lblInstruction2)

        ' Status label
        Dim lblStatus As New Label()
        lblStatus.Text = "🔍 Ready to scan QR code..."
        lblStatus.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        lblStatus.ForeColor = Color.LightGreen
        lblStatus.BackColor = Color.Transparent
        lblStatus.AutoSize = True
        lblStatus.Location = New Point(0, 200) ' Temporary position
        qrDialog.Controls.Add(lblStatus)

        ' QR indicator (blinking effect)
        Dim lblQRIndicator As New Label()
        lblQRIndicator.Text = "🟢 Scanner Active - Waiting for QR code..."
        lblQRIndicator.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        lblQRIndicator.ForeColor = Color.FromArgb(100, 255, 100)
        lblQRIndicator.BackColor = Color.Transparent
        lblQRIndicator.AutoSize = True
        lblQRIndicator.Location = New Point(0, 230) ' Temporary position
        qrDialog.Controls.Add(lblQRIndicator)

        ' Close button (centered)
        Dim btnClose As New Button()
        btnClose.Text = "✕ Close Scanner"
        btnClose.Size = New Size(140, 40)
        btnClose.Location = New Point((qrDialog.ClientSize.Width - 140) / 2, 320)
        btnClose.BackColor = Color.FromArgb(255, 100, 100)
        btnClose.ForeColor = Color.White
        btnClose.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.FlatAppearance.BorderSize = 0
        btnClose.Cursor = Cursors.Hand
        AddHandler btnClose.Click, Sub()
                                       Console.WriteLine("Close button clicked")
                                       qrDialog.Close()
                                   End Sub

        ' Add hover effect to close button
        AddHandler btnClose.MouseEnter, Sub()
                                            btnClose.BackColor = Color.FromArgb(255, 50, 50)
                                        End Sub
        AddHandler btnClose.MouseLeave, Sub()
                                            btnClose.BackColor = Color.FromArgb(255, 100, 100)
                                        End Sub

        ' Add all controls to dialog first
        qrDialog.Controls.AddRange({txtQRInput, lblDebug, btnClose})

        ' Force layout calculation and then center labels properly
        qrDialog.PerformLayout()
        Application.DoEvents()

        ' Now center all labels properly after AutoSize has calculated their actual sizes
        lblTitle.Location = New Point((qrDialog.ClientSize.Width - lblTitle.Width) / 2, 70)
        lblInstruction.Location = New Point((qrDialog.ClientSize.Width - lblInstruction.Width) / 2, 120)
        lblInstruction2.Location = New Point((qrDialog.ClientSize.Width - lblInstruction2.Width) / 2, 150)
        lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)
        lblQRIndicator.Location = New Point((qrDialog.ClientSize.Width - lblQRIndicator.Width) / 2, 230)

        ' Add blinking timer for QR indicator
        Dim blinkTimer As New Timer()
        blinkTimer.Interval = 1000
        AddHandler blinkTimer.Tick, Sub()
                                        Try
                                            If lblQRIndicator.ForeColor = Color.FromArgb(100, 255, 100) Then
                                                lblQRIndicator.ForeColor = Color.Gray
                                                lblQRIndicator.Text = "🔘 Scanner Active - Waiting for QR code..."
                                            Else
                                                lblQRIndicator.ForeColor = Color.FromArgb(100, 255, 100)
                                                lblQRIndicator.Text = "🟢 Scanner Active - Waiting for QR code..."
                                            End If
                                            ' Recenter after text change
                                            lblQRIndicator.Location = New Point((qrDialog.ClientSize.Width - lblQRIndicator.Width) / 2, 230)
                                        Catch ex As Exception
                                            Console.WriteLine($"Blink timer error: {ex.Message}")
                                        End Try
                                    End Sub
        blinkTimer.Start()

        ' Auto-clear timer event
        AddHandler autoClearTimer.Tick, Sub()
                                            Try
                                                autoClearTimer.Stop()
                                                If Not String.IsNullOrEmpty(txtQRInput.Text) AndAlso Not txtQRInput.Text.Trim().StartsWith("User-") Then
                                                    Console.WriteLine($"Auto-clearing: '{txtQRInput.Text}'")
                                                    txtQRInput.Clear()
                                                    lblStatus.Text = "🗑️ Cleared accidental input - Ready to scan..."
                                                    lblStatus.ForeColor = Color.Orange
                                                    lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)
                                                    lblDebug.Text = "Debug: Auto-cleared"

                                                    ' Reset status after showing clear message
                                                    Dim resetStatusTimer As New Timer()
                                                    resetStatusTimer.Interval = 1500
                                                    AddHandler resetStatusTimer.Tick, Sub()
                                                                                          Try
                                                                                              resetStatusTimer.Stop()
                                                                                              lblStatus.Text = "🔍 Ready to scan QR code..."
                                                                                              lblStatus.ForeColor = Color.LightGreen
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

        ' QR input event handlers with improved validation
        AddHandler txtQRInput.TextChanged, Sub(s, eArgs)
                                               Try
                                                   ' Update debug label
                                                   lblDebug.Text = $"Debug: '{txtQRInput.Text}'"

                                                   ' Reset and start auto-clear timer when text changes
                                                   autoClearTimer.Stop()
                                                   If Not String.IsNullOrEmpty(txtQRInput.Text) Then
                                                       autoClearTimer.Start()
                                                   End If

                                                   ' Show input feedback
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
                                                   autoClearTimer.Stop() ' Stop auto-clear when processing

                                                   Dim fullInput As String = txtQRInput.Text.Trim()
                                                   Console.WriteLine($"Processing Enter key with input: '{fullInput}'")

                                                   ' Look for valid QR code pattern in the input
                                                   Dim qrCode As String = ExtractQRCodeFromInput(fullInput)

                                                   If Not String.IsNullOrEmpty(qrCode) Then
                                                       Console.WriteLine($"Valid QR code found: {qrCode}")
                                                       lblStatus.Text = "🔄 Processing QR code..."
                                                       lblStatus.ForeColor = Color.Yellow
                                                       lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)

                                                       lblQRIndicator.Text = "⏳ Processing..."
                                                       lblQRIndicator.ForeColor = Color.Yellow
                                                       lblQRIndicator.Location = New Point((qrDialog.ClientSize.Width - lblQRIndicator.Width) / 2, 230)

                                                       ' Small delay to show processing state
                                                       Application.DoEvents()
                                                       Threading.Thread.Sleep(500)

                                                       If ProcessQRLogin(qrCode) Then
                                                           Console.WriteLine("QR login successful - closing dialog")
                                                           blinkTimer.Stop()
                                                           autoClearTimer.Stop()
                                                           qrDialog.Close()
                                                       Else
                                                           Console.WriteLine("QR login failed")
                                                           lblStatus.Text = "❌ Invalid QR code. Please try again."
                                                           lblStatus.ForeColor = Color.Red
                                                           lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)

                                                           lblQRIndicator.Text = "🔴 Error - Ready for next scan"
                                                           lblQRIndicator.ForeColor = Color.Red
                                                           lblQRIndicator.Location = New Point((qrDialog.ClientSize.Width - lblQRIndicator.Width) / 2, 230)
                                                           txtQRInput.Clear()
                                                           lblDebug.Text = "Debug: (cleared after error)"

                                                           ' Reset to scanning state after 3 seconds
                                                           Dim resetTimer As New Timer()
                                                           resetTimer.Interval = 3000
                                                           AddHandler resetTimer.Tick, Sub()
                                                                                           Try
                                                                                               resetTimer.Stop()
                                                                                               lblStatus.Text = "🔍 Ready to scan QR code..."
                                                                                               lblStatus.ForeColor = Color.LightGreen
                                                                                               lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)

                                                                                               lblQRIndicator.Text = "🟢 Scanner Active - Waiting for QR code..."
                                                                                               lblQRIndicator.ForeColor = Color.FromArgb(100, 255, 100)
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
                                                       ' No valid QR code found, clear input and show message
                                                       lblStatus.Text = "⚠️ No valid QR code detected. Please scan again."
                                                       lblStatus.ForeColor = Color.Orange
                                                       lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)
                                                       txtQRInput.Clear()
                                                       lblDebug.Text = "Debug: No valid QR code"

                                                       ' Reset to scanning state after 2 seconds
                                                       Dim resetTimer As New Timer()
                                                       resetTimer.Interval = 2000
                                                       AddHandler resetTimer.Tick, Sub()
                                                                                       Try
                                                                                           resetTimer.Stop()
                                                                                           lblStatus.Text = "🔍 Ready to scan QR code..."
                                                                                           lblStatus.ForeColor = Color.LightGreen
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

        ' Clean up timers when dialog closes
        AddHandler qrDialog.FormClosed, Sub()
                                            Try
                                                Console.WriteLine("QR Dialog closing - cleaning up timers")
                                                blinkTimer.Stop()
                                                autoClearTimer.Stop()
                                            Catch ex As Exception
                                                Console.WriteLine($"FormClosed error: {ex.Message}")
                                            End Try
                                        End Sub

        ' Focus on QR input and show dialog
        Console.WriteLine("Showing QR Dialog")
        txtQRInput.Focus()
        qrDialog.ShowDialog(Me)
        Console.WriteLine("QR Dialog closed")
    End Sub

    ' Helper function to extract valid QR code from mixed input
    Private Function ExtractQRCodeFromInput(input As String) As String
        Try
            If String.IsNullOrEmpty(input) Then Return ""

            Console.WriteLine($"Extracting QR code from: '{input}'")

            ' Look for User-XXXXX pattern in the input
            Dim pattern As String = "User-\d{5}"
            Dim regex As New Regex(pattern)
            Dim match = regex.Match(input)

            If match.Success Then
                Console.WriteLine($"Found QR code via regex: {match.Value}")
                Return match.Value
            End If

            ' If no pattern found, check if the entire input is a valid QR code
            If input.StartsWith("User-") AndAlso input.Length >= 9 Then
                Dim userIdPart As String = input.Substring(5)
                If userIdPart.All(AddressOf Char.IsDigit) Then
                    Console.WriteLine($"Direct QR code match: {input}")
                    Return input
                End If
            End If

            Console.WriteLine($"No valid QR code found in: '{input}'")
            Return ""
        Catch ex As Exception
            Console.WriteLine($"ExtractQRCodeFromInput error: {ex.Message}")
            Return ""
        End Try
    End Function

    ' Process QR login (return true if successful, false if failed)
    Private Function ProcessQRLogin(userCode As String) As Boolean
        Try
            Console.WriteLine($"Processing QR Login for: {userCode}")

            ' Extract UserID from the scanned code (User-00001 -> 1)
            Dim userIdStr As String = userCode.Substring(5) ' Remove "User-"
            Dim userId As Integer

            If Not Integer.TryParse(userIdStr, userId) Then
                Console.WriteLine("Invalid user ID format")
                MessageBox.Show("Invalid user ID format in QR code.", "QR Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return False
            End If

            Console.WriteLine($"Looking up user ID: {userId}")

            ' Get user details from database using UserID
            Dim query As String = "SELECT Username, pin FROM Users WHERE UserID = @UserID"
            Dim parameters As SqlParameter() = {
                New SqlParameter("@UserID", userId)
            }

            Dim username As String = Nothing
            Dim pinValue As String = Nothing

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                If reader.Read() Then
                    username = reader("Username").ToString()
                    pinValue = reader("pin").ToString()
                    Console.WriteLine($"Found user: {username}")
                End If
            End Using

            If username IsNot Nothing AndAlso pinValue IsNot Nothing Then
                ' Store username for later use
                LoggedInUsername = username

                ' Show success message and proceed to PIN entry
                MessageBox.Show($"QR Code scanned successfully!{vbCrLf}User: {username}{vbCrLf}Please enter your PIN.", "QR Login", MessageBoxButtons.OK, MessageBoxIcon.Information)

                ' Log the QR scan attempt
                Utilities.LogAudit(username, "QR Login Attempt", $"User {username} attempted login via QR code scan")

                ' Show PIN entry panel
                ShowPinEntryPanel(pinValue)
                Return True
            Else
                Console.WriteLine("User not found in database")
                MessageBox.Show("Invalid QR code or user not found.", "QR Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Utilities.LogAudit("Unknown", "QR Login Failed", $"Invalid QR code scanned: {userCode}")
                Return False
            End If

        Catch ex As Exception
            Console.WriteLine($"ProcessQRLogin error: {ex.Message}")
            MessageBox.Show($"Error processing QR code: {ex.Message}", "QR Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Utilities.LogAudit("Unknown", "QR Login Error", $"Error processing QR code {userCode}: {ex.Message}")
            Return False
        End Try
    End Function

    ' Allow pressing Enter in password box to trigger login
    Private Sub txtPassword_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            BtnLogin_Click(BtnLogin, EventArgs.Empty)
            e.Handled = True
        End If
    End Sub

    Private Sub frmLoginvb_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Log logout when form is closing (if user was logged in)
        If Not String.IsNullOrEmpty(LoggedInUsername) Then
            Utilities.LogAudit(LoggedInUsername, "Logged Out", $"User {LoggedInUsername} logged out or application closed.")
        End If
    End Sub

    Private Sub Guna2Panel1_SizeChanged(sender As Object, e As EventArgs) Handles Guna2Panel1.SizeChanged
        Dim path As New Drawing2D.GraphicsPath()
        path.AddArc(0, 0, 100, 100, 180, 90)
        path.AddArc(Guna2Panel1.Width - 100, 0, 100, 100, 270, 90)
        path.AddArc(Guna2Panel1.Width - 100, Guna2Panel1.Height - 100, 100, 100, 0, 90)
        path.AddArc(0, Guna2Panel1.Height - 100, 100, 100, 90, 90)
        path.CloseAllFigures()
        Guna2Panel1.Region = New Region(path)
    End Sub

    Private Sub Guna2Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Guna2Panel1.Paint
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
    End Sub

    Private Sub BtnLogin_Click(sender As Object, e As EventArgs) Handles BtnLogin.Click
        Try
            ' Validate input
            If String.IsNullOrEmpty(txtUserName.Text.Trim()) OrElse String.IsNullOrEmpty(txtPassword.Text.Trim()) Then
                MessageBox.Show("Please enter both username and password.", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Query to get user credentials and check password
            Dim query As String = "
                SELECT UserID, Username, FullName, UserRole, pin, PasswordHash, IsActive 
                FROM Users 
                WHERE Username = @Username 
                AND IsActive = 1"

            Dim parameters As SqlParameter() = {
                New SqlParameter("@Username", txtUserName.Text.Trim())
            }

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                If reader.Read() Then
                    Dim storedPasswordHash As String = reader("PasswordHash").ToString()
                    Dim enteredPassword As String = txtPassword.Text.Trim()
                    Dim isPasswordValid As Boolean = False

                    ' Check if it's a BCrypt hash (starts with $2a$ or $2b$)
                    If storedPasswordHash.StartsWith("$2a$") OrElse storedPasswordHash.StartsWith("$2b$") Then
                        ' Use BCrypt verification
                        isPasswordValid = BCrypt.Net.BCrypt.Verify(enteredPassword, storedPasswordHash)
                    Else
                        ' Legacy SHA256 hash - verify and upgrade to BCrypt
                        Dim enteredPasswordSHA256 As String = HashPasswordSHA256(enteredPassword)
                        isPasswordValid = (enteredPasswordSHA256 = storedPasswordHash)
                        
                        If isPasswordValid Then
                            ' Upgrade to BCrypt hash
                            UpgradeUserPasswordToBCrypt(txtUserName.Text.Trim(), enteredPassword)
                        End If
                    End If

                    If isPasswordValid Then
                        ' Login successful
                        LoggedInUserID = Convert.ToInt32(reader("UserID"))
                        LoggedInUsername = reader("Username").ToString()
                        LoggedInFullName = reader("FullName").ToString()
                        LoggedInRole = reader("UserRole").ToString()
                        Dim pinValue As String = reader("pin").ToString()

                        ' Log successful login
                        Utilities.LogAudit(LoggedInUsername, "Login", "User logged in successfully")

                        ' Show PIN entry panel instead of going directly to Dashboard
                        ShowPinEntryPanel(pinValue)
                    Else
                        ' Login failed
                        MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                        Utilities.LogAudit(txtUserName.Text.Trim(), "Login Failed", "Invalid username or password")
                    End If
                Else
                    ' User not found
                    MessageBox.Show("Invalid username or password.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    Utilities.LogAudit(txtUserName.Text.Trim(), "Login Failed", "Username not found")
                End If
            End Using

        Catch ex As Exception
            MessageBox.Show($"Login error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' BCrypt password hashing function (PRODUCTION READY!)
    Public Shared Function HashPassword(password As String) As String
        ' BCrypt with salt rounds = 12 (very secure)
        ' Each hash takes ~0.3 seconds to compute (good for security)
        Return BCrypt.Net.BCrypt.HashPassword(password, 12)
    End Function

    ' Legacy SHA256 password hashing (for backward compatibility during transition)
    Private Function HashPasswordSHA256(password As String) As String
        Using sha256 As SHA256 = SHA256.Create()
            Dim bytes As Byte() = Encoding.UTF8.GetBytes(password)
            Dim hash As Byte() = sha256.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function

    ' Upgrade user's password from SHA256 to BCrypt
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

            ' Log the upgrade
            Utilities.LogAudit(username, "Password Upgraded", "Password hash upgraded from SHA256 to BCrypt")
        Catch ex As Exception
            Console.WriteLine($"Error upgrading password for {username}: {ex.Message}")
        End Try
    End Sub

    Private Sub ShowPinEntryPanel(expectedPin As String)
        pinPanel = New Guna.UI2.WinForms.Guna2Panel()
        pinPanel.Size = Guna2Panel1.Size
        pinPanel.BorderRadius = 10
        pinPanel.FillColor = Color.FromArgb(190, 154, 48) ' Light gray background for visibility
        pinPanel.Location = Guna2Panel1.Location
        pinPanel.TabStop = True

        Dim lblTitle As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblTitle.Text = "Enter your PIN"
        lblTitle.Font = New Font("Poppins SemiBold", 18.0F, FontStyle.Regular)
        lblTitle.ForeColor = Color.FromArgb(30, 30, 30) ' Darker gray for better contrast on light gray
        lblTitle.Location = New Point((pinPanel.Width - 200) \ 2, 30) ' Estimate width for centering
        lblTitle.AutoSize = True
        pinPanel.Controls.Add(lblTitle)

        Dim pinIndicators As New List(Of Guna.UI2.WinForms.Guna2CircleButton)()
        Dim indicatorSize As Integer = 32
        Dim indicatorSpacing As Integer = 25
        Dim indicatorStartX As Integer = (pinPanel.Width - (indicatorSize * 4 + indicatorSpacing * 3)) \ 2
        For i = 0 To 3
            Dim indicator As New Guna.UI2.WinForms.Guna2CircleButton()
            indicator.Size = New Size(indicatorSize, indicatorSize)
            indicator.FillColor = Color.FromArgb(240, 240, 240) ' Light gray for empty indicators
            indicator.BackColor = Color.FromArgb(190, 154, 48)
            indicator.BorderColor = Color.FromArgb(200, 200, 200)
            indicator.BorderThickness = 2
            indicator.Location = New Point(indicatorStartX + i * (indicatorSize + indicatorSpacing), 90)
            pinIndicators.Add(indicator)
            pinPanel.Controls.Add(indicator)
        Next

        Dim btnBack As New Guna.UI2.WinForms.Guna2Button()
        btnBack.Text = "<"
        btnBack.Font = New Font("Poppins SemiBold", 16.0F, FontStyle.Regular)
        btnBack.Size = New Size(50, 50)
        btnBack.BorderRadius = 10
        btnBack.FillColor = Color.FromArgb(220, 220, 220) ' Slightly darker gray for back button
        btnBack.ForeColor = Color.FromArgb(30, 30, 30) ' Dark gray text
        btnBack.BackColor = Color.FromArgb(245, 245, 245) ' Match panel background
        btnBack.BorderColor = Color.FromArgb(200, 200, 200)
        btnBack.BorderThickness = 1
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
            button.FillColor = Color.White ' Keep white button background for contrast
            button.BackColor = Color.FromArgb(190, 154, 48) ' Match panel background
            button.ForeColor = Color.FromArgb(190, 154, 48) ' Rich Olive color for numbers #BE9A30
            button.Font = New Font("Poppins SemiBold", 18.0F, FontStyle.Regular)
            button.Text = buttonTexts(i)
            button.BorderColor = Color.FromArgb(200, 200, 200) ' Light gray border
            button.BorderThickness = 2

            ' Special styling for X (delete) button
            If buttonTexts(i) = "X" Then
                button.ForeColor = Color.FromArgb(255, 71, 87) ' Red for delete button
            End If

            Dim row = i \ 3
            Dim col = i Mod 3
            button.Location = New Point(buttonStartX + col * (buttonSize + buttonSpacing), buttonStartY + row * (buttonSize + buttonSpacing))

            ' Add hover effects
            AddHandler button.MouseEnter, Sub()
                                              If buttonTexts(Array.IndexOf(buttonTexts, button.Text)) = "X" Then
                                                  button.FillColor = Color.FromArgb(255, 71, 87) ' Red background on hover for X
                                                  button.ForeColor = Color.White ' White text on red background
                                              Else
                                                  button.FillColor = Color.Yellow ' Golden yellow on hover
                                                  button.ForeColor = Color.FromArgb(26, 29, 31) ' Dark text on golden background
                                              End If
                                          End Sub

            AddHandler button.MouseLeave, Sub()
                                              button.FillColor = Color.White ' Back to white
                                              If buttonTexts(Array.IndexOf(buttonTexts, button.Text)) = "X" Then
                                                  button.ForeColor = Color.FromArgb(255, 71, 87) ' Red for delete button
                                              Else
                                                  button.ForeColor = Color.FromArgb(190, 154, 48) ' Rich Olive for numbers
                                              End If
                                          End Sub

            AddHandler button.Click, Sub(senderBtn, eBtn)
                                         HandlePinInput(CType(senderBtn, Guna.UI2.WinForms.Guna2Button).Text, expectedPin, pinIndicators, pinPanel)
                                         pinPanel.Focus() ' Always return focus to panel after click
                                     End Sub
            pinPanel.Controls.Add(button)
            pinPanelButtons.Add(button)
        Next

        ' Add key event handler for PIN entry (including Enter)
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

    ' Improved PIN input logic with Enter key support and consistent state
    Private Sub HandlePinInput(input As String, expectedPin As String, pinIndicators As List(Of Guna.UI2.WinForms.Guna2CircleButton), pinPanel As Control)
        If input = "X" Then
            If pinInput.Length > 0 Then
                pinInput = pinInput.Substring(0, pinInput.Length - 1)
                pinIndicators(pinInput.Length).FillColor = Color.FromArgb(240, 240, 240) ' Light gray for empty
            End If
        ElseIf input = "ENTER" Then
            If pinInput.Length = 4 Then
                ValidatePin(expectedPin, pinIndicators, pinPanel)
            End If
        ElseIf input >= "0" And input <= "9" Then
            If pinInput.Length < 4 Then
                pinInput &= input
                pinIndicators(pinInput.Length - 1).FillColor = Color.Yellow ' Rich Olive for filled indicators
            End If
            If pinInput.Length = 4 Then
                ValidatePin(expectedPin, pinIndicators, pinPanel)
            End If
        End If
    End Sub

    Private Sub ValidatePin(expectedPin As String, pinIndicators As List(Of Guna.UI2.WinForms.Guna2CircleButton), pinPanel As Control)
        If pinInput = expectedPin Then
            LoggedInPIN = pinInput
            Utilities.LogAudit(LoggedInUsername, "Logged In", $"User {LoggedInUsername} successfully logged in at {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.Controls.Remove(pinPanel)
            Me.Hide()
            pinInput = ""

            ' Add error handling for Dashboard instantiation
            Try
                Console.WriteLine("Creating Dashboard from PIN validation...")
                Dim dashboardForm As New Dashboard()
                Console.WriteLine("Showing Dashboard from PIN...")
                dashboardForm.Show()
                Console.WriteLine("Dashboard shown successfully from PIN!")
            Catch ex As Exception
                Console.WriteLine($"Error showing dashboard from PIN: {ex.Message}")
                Console.WriteLine($"Stack trace: {ex.StackTrace}")

                ' Show the login form again and display error
                Me.Show()
                MessageBox.Show($"Error opening dashboard: {ex.Message}{vbCrLf}{vbCrLf}Please try logging in again.",
                              "Dashboard Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        Else
            failedPinAttempts += 1
            Utilities.LogAudit(LoggedInUsername, "PIN Attempt Failed", $"Incorrect PIN attempt #{failedPinAttempts} for user {LoggedInUsername}")
            If failedPinAttempts >= 3 Then
                MessageBox.Show("Too many incorrect PIN attempts. The application will now close.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Application.Exit()
            Else
                MessageBox.Show("Incorrect PIN.", "Login Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                pinInput = ""
                For Each indicator In pinIndicators
                    indicator.FillColor = Color.FromArgb(240, 240, 240) ' Light gray for empty state
                Next
            End If
        End If
    End Sub

    ' Public method to handle logout from other forms
    Public Shared Sub LogoutUser()
        If Not String.IsNullOrEmpty(LoggedInUsername) Then
            Utilities.LogAudit(LoggedInUsername, "Logged Out", $"User {LoggedInUsername} logged out at {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            LoggedInUsername = Nothing
            LoggedInPIN = Nothing
        End If
    End Sub

    Private Sub Guna2CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles Guna2CheckBox1.CheckedChanged
        If Guna2CheckBox1.Checked Then
            txtPassword.PasswordChar = ControlChars.NullChar ' Show password
        Else
            txtPassword.PasswordChar = "•"c ' Hide password
        End If
    End Sub

    Private Sub lblForgotPass_Click(sender As Object, e As EventArgs) Handles lblForgotPass.Click
        MessageBox.Show("Forgot Password functionality will be implemented.", "Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    ' Remove the old Guna2Button1_Click method and keep only the new login system
    Private Sub Guna2Button1_Click(sender As Object, e As EventArgs)
        ' This button appears to be duplicate - redirect to main login
        BtnLogin_Click(sender, e)
    End Sub
End Class