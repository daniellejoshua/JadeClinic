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
    ' Add near other private fields
    Private failedLoginAttempts As Integer = 0
    Private Const MaxLoginAttempts As Integer = 3
    Private pinInput As String = "" ' Class-level for consistency
    Private qrScannerEnabled As Boolean = True ' Track QR scanner state
    Private qrScannerActive As Boolean = False ' Track if QR scanner dialog is currently open

    Private Const TitleBarHoverHeight As Integer = 8
    Private isTitleBarVisible As Boolean = False
    Private pictureBoxTopSpacing As Integer = 20
    Private Sub EnableTitleBarHover()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.ControlBox = False
        Me.MinimizeBox = False
        Me.MaximizeBox = False
        Me.TopMost = False
    End Sub

    Private Sub frmLoginvb_MouseMove(sender As Object, e As MouseEventArgs)
        Dim shouldShow = e.Y <= TitleBarHoverHeight
        If shouldShow <> isTitleBarVisible Then
            isTitleBarVisible = shouldShow
            Me.ControlBox = shouldShow
            Me.MinimizeBox = shouldShow
        End If
    End Sub

    Private Sub frmLoginvb_MouseLeave(sender As Object, e As EventArgs)
        If isTitleBarVisible Then
            isTitleBarVisible = False
            Me.ControlBox = False
            Me.MinimizeBox = False
        End If
    End Sub
    Private Sub frmLoginvb_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Guna2Panel1.BorderRadius = 50
        Me.MaximizeBox = False
        Me.WindowState = FormWindowState.Maximized
        Me.FormBorderStyle = FormBorderStyle.None
        Me.KeyPreview = True

        pictureBoxTopSpacing = Math.Max(20, Guna2Panel1.Top - (PictureBox1.Top + PictureBox1.Height))
        CenterLoginLayout()
        EnableTitleBarHover()

        AddHandler Me.KeyDown, AddressOf frmLoginvb_KeyDown

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
                                                   Guna2HtmlLabel5.ForeColor = Color.FromArgb(254, 191, 16) ' GoldenYellow on hover
                                                   Guna2HtmlLabel5.Cursor = Cursors.Hand ' Hand cursor on hover
                                               End Sub
        AddHandler Guna2HtmlLabel5.MouseLeave, Sub()
                                                   Guna2HtmlLabel5.ForeColor = Color.FromArgb(51, 51, 51) ' Back to DarkText
                                                   Guna2HtmlLabel5.Cursor = Cursors.Default ' Default cursor
                                               End Sub

        ' Add event handlers to show QR code when both fields are filled (DISABLED for security)
        ' AddHandler txtUserName.TextChanged, AddressOf CheckAndShowQRCode
        ' AddHandler txtPassword.TextChanged, AddressOf CheckAndShowQRCode

        ' Add QR code protection to input fields
        AddHandler txtUserName.KeyPress, AddressOf ProtectFromQRInput
        AddHandler txtPassword.KeyPress, AddressOf ProtectFromQRInput
        AddHandler txtUserName.TextChanged, AddressOf ValidateInputForQRCodes
        AddHandler txtPassword.TextChanged, AddressOf ValidateInputForQRCodes
    End Sub

    Private Sub CenterLoginLayout()
        Dim groupHeight As Integer = PictureBox1.Height + pictureBoxTopSpacing + Guna2Panel1.Height
        Dim groupTop As Integer = Math.Max(0, (Me.ClientSize.Height - groupHeight) \ 2)

        PictureBox1.Top = groupTop
        PictureBox1.Left = (Me.ClientSize.Width - PictureBox1.Width) \ 2

        Guna2Panel1.Top = PictureBox1.Bottom + pictureBoxTopSpacing
        Guna2Panel1.Left = (Me.ClientSize.Width - Guna2Panel1.Width) \ 2

    End Sub

    ' Initialize database on startup - THIS FIXES THE ERROR!
    Private Sub InitializeDatabaseOnStartup()
        Try
            Console.WriteLine("Checking database connectivity on startup...")

            ' Production-safe startup check:
            ' do NOT run schema initialization from login screen on every client.
            If Connection.TestConnection() Then
                Console.WriteLine("✅ Database connection is ready for login.")
            Else
                Console.WriteLine("❌ Database connection failed.")
                MessageBox.Show("Unable to connect to the database server. Please check network and SQL settings.",
                                "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

        Catch ex As Exception
            Console.WriteLine($"Database startup check error: {ex.Message}")
            MessageBox.Show($"Database connection error: {ex.Message}",
                            "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Protect input fields from QR code input when scanner is not active
    Private Sub ProtectFromQRInput(sender As Object, e As KeyPressEventArgs)
        ' If QR scanner is not active, block QR-like input patterns
        If Not qrScannerActive Then
            ' Always allow control characters (backspace, delete, etc.)
            If Char.IsControl(e.KeyChar) Then
                Return ' Allow all control characters
            End If

            Dim currentText As String = ""
            Dim newChar As String = e.KeyChar.ToString()

            ' Handle both Guna2TextBox and regular TextBox
            If TypeOf sender Is Guna.UI2.WinForms.Guna2TextBox Then
                Dim gunaTextBox As Guna.UI2.WinForms.Guna2TextBox = CType(sender, Guna.UI2.WinForms.Guna2TextBox)
                currentText = gunaTextBox.Text
            ElseIf TypeOf sender Is TextBox Then
                Dim textBox As TextBox = CType(sender, TextBox)
                currentText = textBox.Text
            Else
                ' Unknown control type, skip validation
                Return
            End If

            ' Only block if this would create a clear QR code pattern
            Dim potentialText As String = currentText + newChar
            If IsDefiniteQRCodeInput(potentialText) Then
                e.Handled = True ' Block the input
                Console.WriteLine($"Blocked QR code input: '{potentialText}'")

                ' Just show a brief tooltip-style message without modal dialog
                ' You can implement a non-blocking notification here if desired
            End If
        End If
    End Sub

    ' Validate text input for QR code patterns
    Private Sub ValidateInputForQRCodes(sender As Object, e As EventArgs)
        If Not qrScannerActive Then
            Dim text As String = ""

            ' Handle both Guna2TextBox and regular TextBox
            If TypeOf sender Is Guna.UI2.WinForms.Guna2TextBox Then
                Dim gunaTextBox As Guna.UI2.WinForms.Guna2TextBox = CType(sender, Guna.UI2.WinForms.Guna2TextBox)
                text = gunaTextBox.Text

                ' Only clear if this is definitely a complete QR code
                If IsDefiniteQRCodeInput(text) Then
                    ' Clear the field silently
                    gunaTextBox.Clear()
                    Console.WriteLine($"Cleared QR code input: '{text}'")
                End If
            ElseIf TypeOf sender Is TextBox Then
                Dim textBox As TextBox = CType(sender, TextBox)
                text = textBox.Text

                ' Only clear if this is definitely a complete QR code
                If IsDefiniteQRCodeInput(text) Then
                    ' Clear the field silently
                    textBox.Clear()
                    Console.WriteLine($"Cleared QR code input: '{text}'")
                End If
            End If
        End If
    End Sub

    ' Check if input is definitely a QR code (more restrictive than IsLikelyQRCodeInput)
    Private Function IsDefiniteQRCodeInput(input As String) As Boolean
        If String.IsNullOrEmpty(input) Then Return False

        ' Only trigger on exact User-XXXXX pattern
        If input.StartsWith("User-", StringComparison.OrdinalIgnoreCase) AndAlso input.Length >= 8 Then
            Dim userIdPart As String = input.Substring(5)
            If userIdPart.All(AddressOf Char.IsDigit) AndAlso userIdPart.Length >= 3 Then
                Return True
            End If
        End If

        ' Only trigger on very long numeric sequences (8+ digits)
        If input.Length >= 8 AndAlso input.All(AddressOf Char.IsDigit) Then
            Return True
        End If

        Return False
    End Function

    ' Check if input looks like a QR code (original function, kept for compatibility)
    Private Function IsLikelyQRCodeInput(input As String) As Boolean
        If String.IsNullOrEmpty(input) Then Return False

        ' Check for User-XXXXX pattern
        If input.StartsWith("User-", StringComparison.OrdinalIgnoreCase) Then
            Return True
        End If

        ' Check for rapid sequential numeric input (typical of QR scanners)
        If input.Length > 3 AndAlso input.All(AddressOf Char.IsDigit) Then
            Return True
        End If

        ' Check for mixed alphanumeric that could be a QR code
        If input.Length > 5 AndAlso Not input.Contains(" ") Then
            Dim alphaCount = input.Count(AddressOf Char.IsLetter)
            Dim digitCount = input.Count(AddressOf Char.IsDigit)
            Dim symbolCount = input.Count(Function(c) Not Char.IsLetterOrDigit(c))

            ' If it has a mix of characters typical of QR codes
            If (alphaCount > 0 AndAlso digitCount > 0) OrElse symbolCount > 0 Then
                Return True
            End If
        End If

        Return False
    End Function

    ' Check if both username and password are filled, then show QR code
    Private Sub CheckAndShowQRCode(sender As Object, e As EventArgs)
        ' Only show QR code if scanner is not currently active and both fields are filled
        If Not qrScannerActive Then
            Dim usernameText As String = ""
            Dim passwordText As String = ""

            ' Get text from username field (handle both control types)
            If TypeOf txtUserName Is Guna.UI2.WinForms.Guna2TextBox Then
                usernameText = CType(txtUserName, Guna.UI2.WinForms.Guna2TextBox).Text
            End If

            ' Get text from password field (handle both control types)
            If TypeOf txtPassword Is Guna.UI2.WinForms.Guna2TextBox Then
                passwordText = CType(txtPassword, Guna.UI2.WinForms.Guna2TextBox).Text
            End If

            If Not String.IsNullOrWhiteSpace(usernameText) AndAlso Not String.IsNullOrWhiteSpace(passwordText) Then
                ShowUserQRCode()
            End If
        End If
    End Sub

    ' Show the user's QR code based on their username
    Private Sub ShowUserQRCode()
        Try
            ' Only show QR code if scanner is not active
            If qrScannerActive Then
                Return ' Exit immediately if scanner is active
            End If

            Dim username As String = ""

            ' Get username from the appropriate control type
            If TypeOf txtUserName Is Guna.UI2.WinForms.Guna2TextBox Then
                username = CType(txtUserName, Guna.UI2.WinForms.Guna2TextBox).Text.Trim()

            Else
                ' If we can't determine the control type, try accessing .Text property directly
                username = txtUserName.Text.Trim()
            End If

            If String.IsNullOrWhiteSpace(username) Then
                Return ' Don't show QR code if username is empty
            End If

            ' Get user details from database
            Dim query As String = "SELECT UserID, FullName FROM Users WHERE Username = @Username AND IsActive = 1"
            Dim parameters As SqlParameter() = {New SqlParameter("@Username", username)}

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                If reader.Read() Then
                    Dim userId As Integer = Convert.ToInt32(reader("UserID"))
                    Dim fullName As String = If(IsDBNull(reader("FullName")), username, reader("FullName").ToString())
                    Dim qrCode As String = $"User-{userId:D5}"

                    ' Create a form to display the QR code nicely
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

    ' Event handler for QR login label click
    Private Sub Guna2HtmlLabel5_Click(sender As Object, e As EventArgs)
        If qrScannerEnabled Then
            ShowQRScanDialog()
        Else
            MessageBox.Show("QR Scanner is currently disabled. Please use username/password login.", "QR Scanner Disabled", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    ' Toggle QR scanner enabled/disabled
    Private Sub ToggleQRScanner()
        qrScannerEnabled = Not qrScannerEnabled

        If qrScannerEnabled Then
            Guna2HtmlLabel5.Text = "🔍 Scan QR Code"
            Guna2HtmlLabel5.ForeColor = Color.White
        Else
            Guna2HtmlLabel5.Text = "🚫 QR Scanner Disabled"
            Guna2HtmlLabel5.ForeColor = Color.Gray
        End If
    End Sub

    ' Create and show QR scan dialog (QR scanner only, no manual typing)
    Private Sub ShowQRScanDialog()
        ' Set QR scanner as active
        qrScannerActive = True

        ' Create QR scan dialog form
        Dim qrDialog As New Form()
        qrDialog.Text = "QR Code Scanner - Staff Login"
        qrDialog.Size = New Size(550, 480)
        qrDialog.StartPosition = FormStartPosition.CenterParent
        qrDialog.BackColor = Color.White
        qrDialog.FormBorderStyle = FormBorderStyle.FixedDialog
        qrDialog.MaximizeBox = False
        qrDialog.MinimizeBox = False
        qrDialog.ShowIcon = False
        qrDialog.KeyPreview = True ' Enable key preview for the dialog

        ' Create QR input textbox (hidden for scanner input only)
        Dim txtQRInput As New TextBox()
        txtQRInput.Location = New Point(-1000, 10) ' Visible for debugging
        txtQRInput.Size = New Size(200, 20)
        txtQRInput.BackColor = Color.FromArgb(237, 237, 237)
        txtQRInput.ForeColor = Color.FromArgb(51, 51, 51)
        txtQRInput.BorderStyle = BorderStyle.FixedSingle
        txtQRInput.TabIndex = 0
        txtQRInput.TabStop = True

        ' Debug label to show what's being typed
        Dim lblDebug As New Label()
        lblDebug.Text = "Debug: (empty)"
        lblDebug.Font = New Font("Poppins", 8.0F, FontStyle.Regular)
        lblDebug.ForeColor = Color.FromArgb(102, 102, 102)
        lblDebug.BackColor = Color.Transparent
        lblDebug.AutoSize = True
        lblDebug.Visible = True ' Made visible for debugging
        lblDebug.Location = New Point(-1010, 40)

        ' Auto-clear timer to clear accidentally typed text
        Dim autoClearTimer As New Timer()
        autoClearTimer.Interval = 3000 ' Clear after 3 seconds of inactivity

        ' Title label
        Dim lblTitle As New Label()
        lblTitle.Text = "QR Code Scanner"
        lblTitle.Font = New Font("Poppins", 18.0F, FontStyle.Bold)
        lblTitle.ForeColor = Color.FromArgb(51, 51, 51)
        lblTitle.BackColor = Color.Transparent
        lblTitle.AutoSize = True
        lblTitle.Location = New Point(0, 70) ' Temporary position
        qrDialog.Controls.Add(lblTitle)

        ' Instruction label
        Dim lblInstruction As New Label()
        lblInstruction.Text = "Point your QR scanner at the staff QR code"
        lblInstruction.Font = New Font("Poppins", 11.0F, FontStyle.Regular)
        lblInstruction.ForeColor = Color.FromArgb(51, 51, 51)
        lblInstruction.BackColor = Color.Transparent
        lblInstruction.AutoSize = True
        lblInstruction.Location = New Point(0, 120) ' Temporary position
        qrDialog.Controls.Add(lblInstruction)

        ' Secondary instruction
        Dim lblInstruction2 As New Label()
        lblInstruction2.Text = "Scanner will automatically detect and process QR codes"
        lblInstruction2.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
        lblInstruction2.ForeColor = Color.FromArgb(102, 102, 102)
        lblInstruction2.BackColor = Color.Transparent
        lblInstruction2.AutoSize = True
        lblInstruction2.Location = New Point(0, 150) ' Temporary position
        qrDialog.Controls.Add(lblInstruction2)

        ' Status label
        Dim lblStatus As New Label()
        lblStatus.Text = "Ready to scan QR code..."
        lblStatus.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        lblStatus.ForeColor = Color.FromArgb(80, 160, 80)
        lblStatus.BackColor = Color.Transparent
        lblStatus.AutoSize = True
        lblStatus.Location = New Point(0, 200) ' Temporary position
        qrDialog.Controls.Add(lblStatus)

        ' QR indicator (blinking effect)
        Dim lblQRIndicator As New Label()
        lblQRIndicator.Text = "Scanner Active - Waiting for QR code..."
        lblQRIndicator.Font = New Font("Poppins", 10.0F, FontStyle.Regular)
        lblQRIndicator.ForeColor = Color.FromArgb(80, 160, 80)
        lblQRIndicator.BackColor = Color.Transparent
        lblQRIndicator.AutoSize = True
        lblQRIndicator.Location = New Point(0, 230) ' Temporary position
        qrDialog.Controls.Add(lblQRIndicator)

        ' Close button (centered)
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

        ' Add hover effect to close button
        AddHandler btnClose.MouseEnter, Sub()
                                            btnClose.BackColor = Color.FromArgb(190, 60, 50)
                                        End Sub
        AddHandler btnClose.MouseLeave, Sub()
                                            btnClose.BackColor = Color.FromArgb(220, 80, 70)
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
                                            If lblQRIndicator.ForeColor = Color.FromArgb(80, 160, 80) Then
                                                lblQRIndicator.ForeColor = Color.FromArgb(102, 102, 102)
                                                lblQRIndicator.Text = "Scanner Active - Waiting for QR code..."
                                            Else
                                                lblQRIndicator.ForeColor = Color.FromArgb(80, 160, 80)
                                                lblQRIndicator.Text = "Scanner Active - Waiting for QR code..."
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
                                                    lblStatus.Text = "Cleared accidental input - Ready to scan..."
                                                    lblStatus.ForeColor = Color.FromArgb(230, 150, 40)
                                                    lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)
                                                    lblDebug.Text = "Debug: Auto-cleared"

                                                    ' Reset status after showing clear message
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
                                                       lblStatus.Text = "Processing QR code..."
                                                       lblStatus.ForeColor = Color.FromArgb(230, 150, 40)
                                                       lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)

                                                       lblQRIndicator.Text = "Processing..."
                                                       lblQRIndicator.ForeColor = Color.FromArgb(230, 150, 40)
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
                                                           lblStatus.Text = "Invalid QR code. Please try again."
                                                           lblStatus.ForeColor = Color.FromArgb(220, 80, 70)
                                                           lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)

                                                           lblQRIndicator.Text = "Error - Ready for next scan"
                                                           lblQRIndicator.ForeColor = Color.FromArgb(220, 80, 70)
                                                           lblQRIndicator.Location = New Point((qrDialog.ClientSize.Width - lblQRIndicator.Width) / 2, 230)
                                                           txtQRInput.Clear()
                                                           lblDebug.Text = "Debug: (cleared after error)"

                                                           ' Reset to scanning state after 3 seconds
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
                                                       ' No valid QR code found, clear input and show message
                                                       lblStatus.Text = "No valid QR code detected. Please scan again."
                                                       lblStatus.ForeColor = Color.FromArgb(230, 150, 40)
                                                       lblStatus.Location = New Point((qrDialog.ClientSize.Width - lblStatus.Width) / 2, 200)
                                                       txtQRInput.Clear()
                                                       lblDebug.Text = "Debug: No valid QR code"

                                                       ' Reset to scanning state after 2 seconds
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

        ' Clean up timers when dialog closes
        AddHandler qrDialog.FormClosed, Sub()
                                            Try
                                                Console.WriteLine("QR Dialog closing - cleaning up timers")
                                                blinkTimer.Stop()
                                                autoClearTimer.Stop()
                                                qrScannerActive = False ' Reset active state
                                            Catch ex As Exception
                                                Console.WriteLine($"FormClosed error: {ex.Message}")
                                            End Try
                                        End Sub

        ' Focus on QR input and show dialog
        Console.WriteLine("Showing QR Dialog")
        txtQRInput.Focus()
        qrDialog.ShowDialog(Me)
        Console.WriteLine("QR Dialog closed")

        ' Ensure the main form and PIN panel receive focus after the scanner dialog closes so the user can immediately type the PIN.
        Try
            If pinPanel IsNot Nothing Then
                Me.Activate()
                Me.Focus()
                Me.ActiveControl = pinPanel
                pinPanel.Focus()
            Else
                ' If PIN panel hasn't been created yet, just activate the main form so it gets focus.
                Me.Activate()
                Me.Focus()
            End If
        Catch ex As Exception
            Console.WriteLine($"Error focusing PIN panel after QR dialog: {ex.Message}")
        End Try
    End Sub

    ' Helper function to extract valid QR code from mixed input
    Private Function ExtractQRCodeFromInput(input As String) As String
        Try
            If String.IsNullOrEmpty(input) Then Return ""

            Console.WriteLine($"Extracting QR code from: '{input}'")

            ' Look for User-XXXXX pattern in the input (more flexible)
            Dim pattern As String = "User-\d{1,5}"
            Dim regex As New Regex(pattern)
            Dim match = regex.Match(input)

            If match.Success Then
                Console.WriteLine($"Found QR code via regex: {match.Value}")
                Return match.Value
            End If

            ' If no pattern found, check if the entire input is a valid QR code
            If input.StartsWith("User-") AndAlso input.Length >= 6 Then
                Dim userIdPart As String = input.Substring(5)
                If userIdPart.All(AddressOf Char.IsDigit) AndAlso userIdPart.Length >= 1 Then
                    Console.WriteLine($"Direct QR code match: {input}")
                    Return input
                End If
            End If

            ' Also check for just the number part
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

    ' Process QR login (return true if successful, false if failed)
    Private Function ProcessQRLogin(userCode As String) As Boolean
        Try
            Console.WriteLine($"Processing QR Login for: {userCode}")

            ' Extract UserID from the scanned code (User-00001 -> 1)
            Dim userIdStr As String = userCode.Substring(5) ' Remove "User-"
            Dim userId As Integer

            If Not Integer.TryParse(userIdStr, userId) Then
                Console.WriteLine("Invalid user ID format")

                ' Count as a failed login attempt; only audit on reaching max.
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

            ' Get user details from database using UserID (include IsActive and UserRole)
            Dim query As String = "SELECT Username, pin, IsActive, UserRole, FullName FROM Users WHERE UserID = @UserID"
            Dim parameters As SqlParameter() = {
            New SqlParameter("@UserID", userId)
        }

            Dim username As String = Nothing
            Dim pinValue As String = Nothing
            Dim isActive As Boolean = True
            Dim userRole As String = String.Empty
            Dim fullName As String = String.Empty

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
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
                ' If account inactive, show error and audit log (keep this as a distinct security event)
                If Not isActive Then
                    MessageBox.Show("This account is inactive. Please contact your administrator.", "Account Inactive", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Utilities.LogAudit(username, "QR Login Attempt (Inactive)", $"Inactive account attempted QR login: {username}")
                    Return False
                End If

                ' Store user context for subsequent flows
                LoggedInUserID = userId
                LoggedInUsername = username
                LoggedInRole = userRole
                If Not String.IsNullOrEmpty(fullName) Then
                    LoggedInFullName = fullName
                End If

                ' Show success message and proceed to PIN entry
                MessageBox.Show($"QR Code scanned successfully!{vbCrLf}User: {username}{vbCrLf}Please enter your PIN.", "QR Login", MessageBoxButtons.OK, MessageBoxIcon.Information)

                ' Log the QR scan attempt (successful detection)
                Utilities.LogAudit(username, "QR Login Attempt", $"User {username} attempted login via QR code scan")

                ' Reset failed login counter on successful identification
                failedLoginAttempts = 0

                ' Show PIN entry panel and ensure the main form will receive focus after the scanner closes so PIN keystrokes are captured.
                ShowPinEntryPanel(pinValue)
                Return True
            Else
                Console.WriteLine("User not found in database")

                ' Count as a failed attempt; only audit on reaching max.
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

            ' Log unexpected errors (distinct from failed attempts)
            Try
                Utilities.LogAudit("Unknown", "QR Login Error", $"Error processing QR code {userCode}: {ex.Message}")
            Catch
            End Try

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

    Private Sub frmLoginvb_KeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Escape Then
            Dim result As DialogResult = EscForm.ConfirmExit(Me)
            If result = DialogResult.Yes Then
                Application.Exit()
            End If
            e.Handled = True
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
            ' Validate input (do not count these as failed attempts)
            If String.IsNullOrEmpty(txtUserName.Text.Trim()) OrElse String.IsNullOrEmpty(txtPassword.Text.Trim()) Then
                MessageBox.Show("Please enter both username and password.", "Login Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Query to get user credentials (do not filter by IsActive here so we can detect inactive attempts)
            Dim query As String = "
            SELECT UserID, Username, FullName, UserRole, pin, PasswordHash, IsActive 
            FROM Users 
            WHERE Username = @Username"

            Dim parameters As SqlParameter() = {
            New SqlParameter("@Username", txtUserName.Text.Trim())
        }

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
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

                    ' If account inactive, show error and audit log immediately (keep this as a distinct security event)
                    If Not isActive Then
                        MessageBox.Show("This account is inactive. Please contact your administrator.", "Account Inactive", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Utilities.LogAudit(usernameDb, "Login Attempt (Inactive)", $"Inactive account attempted to login: {usernameDb}")
                        Return
                    End If

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
                        ' Reset failed login counters on success
                        failedLoginAttempts = 0
                        failedPinAttempts = 0

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
                        ' Wrong password: increment counter and only audit on reaching max
                        failedLoginAttempts += 1

                        If failedLoginAttempts >= MaxLoginAttempts Then
                            Try
                                Dim auditUser = If(String.IsNullOrEmpty(txtUserName.Text.Trim()), "Unknown", txtUserName.Text.Trim())
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
                    ' Username not found: count toward the same max attempts policy
                    failedLoginAttempts += 1

                    If failedLoginAttempts >= MaxLoginAttempts Then
                        Try
                            Dim auditUser = If(String.IsNullOrEmpty(txtUserName.Text.Trim()), "Unknown", txtUserName.Text.Trim())
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
        pinPanel.FillColor = Color.FromArgb(250, 250, 249)
        pinPanel.Location = Guna2Panel1.Location
        pinPanel.TabStop = True

        Dim lblTitle As New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblTitle.Text = "Enter your PIN"
        lblTitle.Font = New Font("Poppins SemiBold", 18.0F, FontStyle.Regular)
        lblTitle.ForeColor = Color.FromArgb(51, 51, 51)
        lblTitle.AutoSize = True
        lblTitle.Location = New Point((pinPanel.Width - lblTitle.Width) \ 2, 30)
        pinPanel.Controls.Add(lblTitle)

        Dim pinIndicators As New List(Of Guna.UI2.WinForms.Guna2CircleButton)()
        Dim indicatorSize As Integer = 32
        Dim indicatorSpacing As Integer = 25
        Dim indicatorStartX As Integer = (pinPanel.Width - (indicatorSize * 4 + indicatorSpacing * 3)) \ 2
        For i = 0 To 3
            Dim indicator As New Guna.UI2.WinForms.Guna2CircleButton()
            indicator.Size = New Size(indicatorSize, indicatorSize)
            indicator.FillColor = Color.FromArgb(237, 237, 237)   ' empty indicator color
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

            ' Special styling for X (delete) button
            If button.Text = "X" Then
                button.ForeColor = Color.FromArgb(220, 80, 70)
            End If

            Dim row = i \ 3
            Dim col = i Mod 3
            button.Location = New Point(buttonStartX + col * (buttonSize + buttonSpacing), buttonStartY + row * (buttonSize + buttonSpacing))

            ' Hover effects
            AddHandler button.MouseEnter, Sub()
                                              Try
                                                  If button.Text = "X" Then
                                                      button.FillColor = Color.FromArgb(220, 80, 70)
                                                      button.ForeColor = Color.White
                                                  Else
                                                      button.FillColor = Color.FromArgb(254, 191, 16) ' hover GoldenYellow
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

        ' Inside ShowPinEntryPanel(expectedPin As String), update lblForgotPin setup:
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

        ' Key handler for PIN entry (including Enter)
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

        ' Step 1 controls
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

        ' Step 2 controls
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

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, {New SqlParameter("@Username", targetUsername)})
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
            ' Reset failed attempts on success
            failedPinAttempts = 0

            LoggedInPIN = pinInput
            Utilities.LogAudit(LoggedInUsername, "Logged In", $"User {LoggedInUsername} successfully logged in at {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            MessageBox.Show("Login successful!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Me.Controls.Remove(pinPanel)
            Me.Hide()
            pinInput = ""

            ' Route user to appropriate form based on their role
            Try
                Console.WriteLine($"Routing user {LoggedInUsername} with role: {LoggedInRole}")

                ' Check user role and route accordingly
                Dim userRole As String = If(LoggedInRole, "Staff").ToUpper()

                Dim shell As New MainShell()
                shell.Show()
                shell.ShowInitialPage()

            Catch ex As Exception
                Console.WriteLine($"Error showing target form: {ex.Message}")
                Console.WriteLine($"Stack trace: {ex.StackTrace}")

                ' Show the login form again and display error
                Me.Show()
                MessageBox.Show($"Error opening application: {ex.Message}{vbCrLf}{vbCrLf}Please try logging in again.",
                          "Application Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        Else
            failedPinAttempts += 1

            ' Do NOT audit log every failed attempt. Only log once when the maximum is reached.
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
    ' Public method to handle logout from other forms
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

    Private Sub lblForgotPass_Click(sender As Object, e As EventArgs) Handles lblForgotPass.Click
        Try
            ' Open the dedicated ForgotPasswordForm so the user can recover/reset credentials.
            ' If the login username field is populated, attempt to prefill it on the forgot form (best-effort).
            Dim forgotForm As New ForgotPasswordForm()

            Try
                Dim initialUsername = txtUserName.Text.Trim()
                If Not String.IsNullOrEmpty(initialUsername) Then
                    ' Best-effort: if the ForgotPasswordForm exposes a public property named InitialUsername, set it.
                    Dim prop = forgotForm.GetType().GetProperty("InitialUsername")
                    If prop IsNot Nothing AndAlso prop.CanWrite Then
                        prop.SetValue(forgotForm, initialUsername)
                    End If
                End If
            Catch ex As Exception
                ' Non-fatal: prefilling is optional
                Console.WriteLine($"Prefill attempt failed: {ex.Message}")
            End Try

            forgotForm.ShowDialog(Me)
        Catch ex As Exception
            Console.WriteLine($"Error opening Forgot Password dialog: {ex.Message}")
            MessageBox.Show("Unable to open the Forgot Password dialog. Please try again.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Guna2CheckBox1_CheckedChanged(sender As Object, e As EventArgs) Handles Guna2CheckBox1.CheckedChanged
        Try
            If Guna2CheckBox1.Checked Then
                ' Reveal password (clear PasswordChar)
                txtPassword.PasswordChar = ChrW(0)
            Else
                ' Hide password (use bullet as on form load)
                txtPassword.PasswordChar = "•"c
            End If
        Catch ex As Exception
            Console.WriteLine($"Show/hide password toggle failed: {ex.Message}")
        End Try
    End Sub
End Class