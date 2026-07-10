Imports System.Windows.Forms
Imports System.Data.Common
Imports System.Linq

Public Class ForgotPasswordForm
    Inherits Form
    Private currentStep As Integer = 1
    Private username As String = ""
    Private passkey As String = ""

    Private lblStep As Label
    Private txtInput As TextBox
    Private lblInstruction As Label
    Private btnNext As Button
    Private btnBack As Button
    Private btnCancel As Button
    Private txtNewPassword As TextBox
    Private txtConfirmPassword As TextBox
    Private txtPasskey1 As TextBox
    Private txtPasskey2 As TextBox
    Private txtPasskey3 As TextBox

    Public Sub New()
        Me.Text = "Forgot Password"
        Me.Size = New Drawing.Size(520, 420)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Drawing.Color.White
        InitializeStepControls()
        ShowStep(1)
        Me.KeyPreview = True
        AddHandler Me.KeyDown, AddressOf ForgotPasswordForm_KeyDown

    End Sub

    Private Sub InitializeStepControls()
        Dim marginX As Integer = 30
        Dim marginY As Integer = 30
        Dim controlWidth As Integer = 440
        Dim buttonWidth As Integer = 110
        Dim buttonHeight As Integer = 40
        Dim buttonSpacing As Integer = 20
        Dim buttonY As Integer = 310

        lblStep = New Label() With {
            .Text = "Step 1",
            .Font = New Drawing.Font("Poppins", 18, Drawing.FontStyle.Bold),
            .Location = New Drawing.Point(marginX, marginY),
            .ForeColor = Drawing.Color.FromArgb(51, 51, 51),
            .AutoSize = True,
            .BackColor = Drawing.Color.Transparent
        }
        lblInstruction = New Label() With {
            .Text = "",
            .Location = New Drawing.Point(marginX, marginY + 50),
            .ForeColor = Drawing.Color.FromArgb(51, 51, 51),
            .Font = New Drawing.Font("Poppins", 12, Drawing.FontStyle.Regular),
            .AutoSize = True,
            .BackColor = Drawing.Color.Transparent
        }
        txtInput = New TextBox() With {
            .Location = New Drawing.Point(marginX, marginY + 95),
            .Width = controlWidth,
            .Font = New Drawing.Font("Poppins", 12, Drawing.FontStyle.Regular),
            .ForeColor = Drawing.Color.FromArgb(51, 51, 51),
            .BackColor = Drawing.Color.FromArgb(237, 237, 237),
            .BorderStyle = BorderStyle.FixedSingle,
            .TextAlign = HorizontalAlignment.Center
        }

        txtPasskey1 = New TextBox() With {
            .Location = New Drawing.Point(marginX, marginY + 95),
            .Width = controlWidth,
            .Font = New Drawing.Font("Poppins", 12, Drawing.FontStyle.Regular),
            .ForeColor = Drawing.Color.FromArgb(51, 51, 51),
            .BackColor = Drawing.Color.FromArgb(237, 237, 237),
            .BorderStyle = BorderStyle.FixedSingle,
            .TextAlign = HorizontalAlignment.Center,
            .Visible = False
        }
        txtPasskey2 = New TextBox() With {
            .Location = New Drawing.Point(marginX, marginY + 140),
            .Width = controlWidth,
            .Font = New Drawing.Font("Poppins", 12, Drawing.FontStyle.Regular),
            .ForeColor = Drawing.Color.FromArgb(51, 51, 51),
            .BackColor = Drawing.Color.FromArgb(237, 237, 237),
            .BorderStyle = BorderStyle.FixedSingle,
            .TextAlign = HorizontalAlignment.Center,
            .Visible = False
        }
        txtPasskey3 = New TextBox() With {
            .Location = New Drawing.Point(marginX, marginY + 185),
            .Width = controlWidth,
            .Font = New Drawing.Font("Poppins", 12, Drawing.FontStyle.Regular),
            .ForeColor = Drawing.Color.FromArgb(51, 51, 51),
            .BackColor = Drawing.Color.FromArgb(237, 237, 237),
            .BorderStyle = BorderStyle.FixedSingle,
            .TextAlign = HorizontalAlignment.Center,
            .Visible = False
        }

        btnBack = New Button() With {
            .Text = "Back",
            .Location = New Drawing.Point(marginX, buttonY),
            .Width = buttonWidth,
            .Height = buttonHeight,
            .Font = New Drawing.Font("Poppins", 11, Drawing.FontStyle.Regular),
            .BackColor = Drawing.Color.FromArgb(237, 237, 237),
            .ForeColor = Drawing.Color.FromArgb(51, 51, 51),
            .FlatStyle = FlatStyle.Flat,
            .Visible = False
        }
        btnBack.FlatAppearance.BorderSize = 0
        btnNext = New Button() With {
            .Text = "Next",
            .Location = New Drawing.Point(marginX + buttonWidth + buttonSpacing, buttonY),
            .Width = buttonWidth,
            .Height = buttonHeight,
            .Font = New Drawing.Font("Poppins", 11, Drawing.FontStyle.Regular),
            .BackColor = Drawing.Color.FromArgb(254, 191, 16),
            .ForeColor = Drawing.Color.FromArgb(51, 51, 51),
            .FlatStyle = FlatStyle.Flat
        }
        btnNext.FlatAppearance.BorderSize = 0
        btnCancel = New Button() With {
            .Text = "Cancel",
            .Location = New Drawing.Point(marginX + 2 * (buttonWidth + buttonSpacing), buttonY),
            .Width = buttonWidth,
            .Height = buttonHeight,
            .Font = New Drawing.Font("Poppins", 11, Drawing.FontStyle.Regular),
            .BackColor = Drawing.Color.FromArgb(220, 80, 70),
            .ForeColor = Drawing.Color.White,
            .FlatStyle = FlatStyle.Flat
        }
        btnCancel.FlatAppearance.BorderSize = 0
        txtNewPassword = New TextBox() With {
            .Location = New Drawing.Point(marginX, marginY + 95),
            .Width = controlWidth,
            .PasswordChar = "*"c,
            .Visible = False,
            .Font = New Drawing.Font("Poppins", 12, Drawing.FontStyle.Regular),
            .ForeColor = Drawing.Color.FromArgb(51, 51, 51),
            .BackColor = Drawing.Color.FromArgb(237, 237, 237),
            .BorderStyle = BorderStyle.FixedSingle,
            .TextAlign = HorizontalAlignment.Center
        }
        txtConfirmPassword = New TextBox() With {
            .Location = New Drawing.Point(marginX, marginY + 145),
            .Width = controlWidth,
            .PasswordChar = "*"c,
            .Visible = False,
            .Font = New Drawing.Font("Poppins", 12, Drawing.FontStyle.Regular),
            .ForeColor = Drawing.Color.FromArgb(51, 51, 51),
            .BackColor = Drawing.Color.FromArgb(237, 237, 237),
            .BorderStyle = BorderStyle.FixedSingle,
            .TextAlign = HorizontalAlignment.Center
        }

        AddHandler btnNext.Click, AddressOf btnNext_Click
        AddHandler btnBack.Click, AddressOf btnBack_Click
        AddHandler btnCancel.Click, Sub() Me.Close()

        Me.Controls.Add(lblStep)
        Me.Controls.Add(lblInstruction)
        Me.Controls.Add(txtInput)
        Me.Controls.Add(txtPasskey1)
        Me.Controls.Add(txtPasskey2)
        Me.Controls.Add(txtPasskey3)
        Me.Controls.Add(txtNewPassword)
        Me.Controls.Add(txtConfirmPassword)
        Me.Controls.Add(btnNext)
        Me.Controls.Add(btnBack)
        Me.Controls.Add(btnCancel)
    End Sub

    Private Sub ShowStep(stepNum As Integer)
        currentStep = stepNum
        txtInput.Visible = (stepNum = 1)
        txtPasskey1.Visible = (stepNum = 2)
        txtPasskey2.Visible = (stepNum = 2)
        txtPasskey3.Visible = (stepNum = 2)
        txtNewPassword.Visible = (stepNum = 3)
        txtConfirmPassword.Visible = (stepNum = 3)
        btnBack.Visible = (stepNum > 1)

        txtInput.Text = ""
        txtPasskey1.Text = ""
        txtPasskey2.Text = ""
        txtPasskey3.Text = ""
        txtNewPassword.Text = ""
        txtConfirmPassword.Text = ""

        Select Case stepNum
            Case 1
                lblStep.Text = "Step 1: Enter Username"
                lblInstruction.Text = "Please enter your username."
                txtInput.PasswordChar = ControlChars.NullChar
                txtInput.Focus()
            Case 2
                lblStep.Text = "Step 2: Enter 3 Passkeys"
                lblInstruction.Text = "Enter all three recovery passkeys."
                txtPasskey1.Focus()
            Case 3
                lblStep.Text = "Step 3: New Password"
                lblInstruction.Text = "Enter your new password and confirm it."
                txtNewPassword.Visible = True
                txtConfirmPassword.Visible = True
                Try
                    txtPasskey1.PlaceholderText = "Passkey 1"
                    txtPasskey2.PlaceholderText = "Passkey 2"
                    txtPasskey3.PlaceholderText = "Passkey 3"
                    txtNewPassword.PlaceholderText = "New Password"
                    txtConfirmPassword.PlaceholderText = "Confirm Password"
                Catch
                    ' PlaceholderText may not be available in older WinForms targets; ignore if not supported.
                End Try
                txtNewPassword.Focus()
        End Select
    End Sub

    Private Sub btnNext_Click(sender As Object, e As EventArgs)
        If currentStep = 1 Then
            username = txtInput.Text.Trim()
            If String.IsNullOrEmpty(username) Then
                MessageBox.Show("Please enter your username.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            Dim query = "SELECT COUNT(*) FROM Users WHERE Username = @Username"
            Dim param = New SqlParameter("@Username", username)
            Dim count = Convert.ToInt32(Utilities.ExecuteScalar(query, {param}))
            If count = 0 Then
                MessageBox.Show("Username not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
            ShowStep(2)

        ElseIf currentStep = 2 Then
            Dim p1 As String = txtPasskey1.Text.Trim().ToUpperInvariant()
            Dim p2 As String = txtPasskey2.Text.Trim().ToUpperInvariant()
            Dim p3 As String = txtPasskey3.Text.Trim().ToUpperInvariant()

            If String.IsNullOrEmpty(p1) OrElse String.IsNullOrEmpty(p2) OrElse String.IsNullOrEmpty(p3) Then
                MessageBox.Show("Please enter all three passkeys.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If p1 = p2 OrElse p1 = p3 OrElse p2 = p3 Then
                MessageBox.Show("Passkeys must be three different values.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Read single comma-separated Passkey column
            Dim query = "SELECT Passkeys FROM Users WHERE Username = @Username"
            Dim param = New SqlParameter("@Username", username)

            Dim storedList As New List(Of String)()
            Try
                Dim result = Utilities.ExecuteScalar(query, {param})
                If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                    Dim raw As String = result.ToString()
                    storedList = raw.Split(","c).
                              Select(Function(s) s.Trim().ToUpperInvariant()).
                              Where(Function(s) Not String.IsNullOrEmpty(s)).
                              ToList()
                End If
            Catch ex As Exception
                MessageBox.Show($"Error reading passkeys: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End Try

            If storedList.Count <> 3 Then
                MessageBox.Show("This account does not have 3 configured passkeys.", "Passkey Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            Dim inputKeys As New List(Of String) From {p1, p2, p3}
            inputKeys.Sort()
            storedList.Sort()

            If Not inputKeys.SequenceEqual(storedList) Then
                MessageBox.Show("Incorrect passkeys.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            passkey = String.Join(",", inputKeys)
            ShowStep(3)

        ElseIf currentStep = 3 Then
            Dim newPass = txtNewPassword.Text.Trim()
            Dim confirmPass = txtConfirmPassword.Text.Trim()

            If String.IsNullOrEmpty(newPass) OrElse String.IsNullOrEmpty(confirmPass) Then
                MessageBox.Show("Please enter and confirm your new password.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            If newPass.Length < 8 Then
                MessageBox.Show("Password must be at least 8 characters long.", "Password Too Short", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtNewPassword.Focus()
                Return
            End If

            If newPass <> confirmPass Then
                MessageBox.Show("Passwords do not match.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If

            Try
                Dim hashed As String = BCrypt.Net.BCrypt.HashPassword(newPass, workFactor:=12)

                Dim q = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Username = @Username"
                Dim param1 = New SqlParameter("@PasswordHash", hashed)
                Dim param2 = New SqlParameter("@Username", username)
                Utilities.ExecuteNonQuery(q, {param1, param2})

                MessageBox.Show("Password updated successfully! You may now log in.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Me.Close()
            Catch ex As Exception
                MessageBox.Show($"Failed to update password: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End Try
        End If
    End Sub
    Private Sub btnBack_Click(sender As Object, e As EventArgs)
        If currentStep = 2 Then
            ShowStep(1)
        ElseIf currentStep = 3 Then
            ShowStep(2)
        End If
    End Sub

    Private Sub ForgotPasswordForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SetupTabIndex()
    End Sub

    Private Sub SetupTabIndex()
        txtInput.TabIndex = 0
        txtPasskey1.TabIndex = 1
        txtPasskey2.TabIndex = 2
        txtPasskey3.TabIndex = 3
        txtNewPassword.TabIndex = 4
        txtConfirmPassword.TabIndex = 5
        btnNext.TabIndex = 6
        btnBack.TabIndex = 7
        btnCancel.TabIndex = 8
        Utilities.ApplyInputFocusEffects(Me)
    End Sub

    ' KeyDown handler: Enter => Next, Esc => Cancel, B => Back
    Private Sub ForgotPasswordForm_KeyDown(sender As Object, e As KeyEventArgs)
        Try
            If e.KeyCode = Keys.Enter Then
                ' Prevent clicking when a multiline control would consume Enter
                btnNext.PerformClick()
                e.SuppressKeyPress = True
                e.Handled = True
            ElseIf e.KeyCode = Keys.Escape Then
                btnCancel.PerformClick()
                e.SuppressKeyPress = True
                e.Handled = True
            ElseIf e.KeyCode = Keys.B Then
                If btnBack.Visible Then
                    btnBack.PerformClick()
                    e.SuppressKeyPress = True
                    e.Handled = True
                End If
            End If
        Catch ex As Exception
            Console.WriteLine($"ForgotPasswordForm_KeyDown error: {ex.Message}")
        End Try
    End Sub
End Class