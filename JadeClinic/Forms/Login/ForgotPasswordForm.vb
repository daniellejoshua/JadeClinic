Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient

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

    Public Sub New()
        Me.Text = "Forgot Password"
        Me.Size = New Drawing.Size(520, 340)
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.StartPosition = FormStartPosition.CenterParent
        Me.BackColor = Drawing.Color.FromArgb(41, 44, 45)
        InitializeStepControls()
        ShowStep(1)
    End Sub

    Private Sub InitializeStepControls()
        Dim marginX As Integer = 30
        Dim marginY As Integer = 30
        Dim controlWidth As Integer = 440
        Dim buttonWidth As Integer = 110
        Dim buttonHeight As Integer = 40
        Dim buttonSpacing As Integer = 20
        Dim buttonY As Integer = 230

        lblStep = New Label() With {
            .Text = "Step 1",
            .Font = New Drawing.Font("Poppins", 18, Drawing.FontStyle.Bold),
            .Location = New Drawing.Point(marginX, marginY),
            .ForeColor = Drawing.Color.White,
            .AutoSize = True,
            .BackColor = Drawing.Color.Transparent
        }
        lblInstruction = New Label() With {
            .Text = "",
            .Location = New Drawing.Point(marginX, marginY + 50),
            .ForeColor = Drawing.Color.White,
            .Font = New Drawing.Font("Poppins", 12, Drawing.FontStyle.Regular),
            .AutoSize = True,
            .BackColor = Drawing.Color.Transparent
        }
        txtInput = New TextBox() With {
            .Location = New Drawing.Point(marginX, marginY + 95),
            .Width = controlWidth,
            .Font = New Drawing.Font("Poppins", 12, Drawing.FontStyle.Regular),
            .ForeColor = Drawing.Color.White,
            .BackColor = Drawing.Color.FromArgb(61, 65, 66),
            .BorderStyle = BorderStyle.FixedSingle,
            .TextAlign = HorizontalAlignment.Center
        }
        btnBack = New Button() With {
            .Text = "Back",
            .Location = New Drawing.Point(marginX, buttonY),
            .Width = buttonWidth,
            .Height = buttonHeight,
            .Font = New Drawing.Font("Poppins", 11, Drawing.FontStyle.Regular),
            .BackColor = Drawing.Color.FromArgb(61, 65, 66),
            .ForeColor = Drawing.Color.White,
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
            .BackColor = Drawing.Color.FromArgb(255, 204, 77),
            .ForeColor = Drawing.Color.Black,
            .FlatStyle = FlatStyle.Flat
        }
        btnNext.FlatAppearance.BorderSize = 0
        btnCancel = New Button() With {
            .Text = "Cancel",
            .Location = New Drawing.Point(marginX + 2 * (buttonWidth + buttonSpacing), buttonY),
            .Width = buttonWidth,
            .Height = buttonHeight,
            .Font = New Drawing.Font("Poppins", 11, Drawing.FontStyle.Regular),
            .BackColor = Drawing.Color.FromArgb(61, 65, 66),
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
            .ForeColor = Drawing.Color.White,
            .BackColor = Drawing.Color.FromArgb(61, 65, 66),
            .BorderStyle = BorderStyle.FixedSingle,
            .TextAlign = HorizontalAlignment.Center
        }
        txtConfirmPassword = New TextBox() With {
            .Location = New Drawing.Point(marginX, marginY + 145),
            .Width = controlWidth,
            .PasswordChar = "*"c,
            .Visible = False,
            .Font = New Drawing.Font("Poppins", 12, Drawing.FontStyle.Regular),
            .ForeColor = Drawing.Color.White,
            .BackColor = Drawing.Color.FromArgb(61, 65, 66),
            .BorderStyle = BorderStyle.FixedSingle,
            .TextAlign = HorizontalAlignment.Center
        }

        AddHandler btnNext.Click, AddressOf btnNext_Click
        AddHandler btnBack.Click, AddressOf btnBack_Click
        AddHandler btnCancel.Click, Sub() Me.Close()

        Me.Controls.Add(lblStep)
        Me.Controls.Add(lblInstruction)
        Me.Controls.Add(txtInput)
        Me.Controls.Add(txtNewPassword)
        Me.Controls.Add(txtConfirmPassword)
        Me.Controls.Add(btnNext)
        Me.Controls.Add(btnBack)
        Me.Controls.Add(btnCancel)
    End Sub

    Private Sub ShowStep(stepNum As Integer)
        currentStep = stepNum
        txtInput.Visible = (stepNum = 1 Or stepNum = 2)
        txtNewPassword.Visible = (stepNum = 3)
        txtConfirmPassword.Visible = (stepNum = 3)
        btnBack.Visible = (stepNum > 1)
        txtInput.Text = ""
        txtNewPassword.Text = ""
        txtConfirmPassword.Text = ""
        Select Case stepNum
            Case 1
                lblStep.Text = "Step 1: Enter Username"
                lblInstruction.Text = "Please enter your username."
                txtInput.PasswordChar = ControlChars.NullChar
                txtInput.Focus()
            Case 2
                lblStep.Text = "Step 2: Enter Passkey"
                lblInstruction.Text = "Please enter your passkey (recovery code)."
                txtInput.PasswordChar = "*"c
                txtInput.Focus()
            Case 3
                lblStep.Text = "Step 3: New Password"
                lblInstruction.Text = "Enter your new password and confirm it."
                txtNewPassword.Visible = True
                txtConfirmPassword.Visible = True
                Try
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
            ' Check if username exists
            Dim query = "SELECT COUNT(*) FROM Users WHERE Username = @Username"
            Dim param = New SqlParameter("@Username", username)
            Dim count = Convert.ToInt32(Utilities.ExecuteScalar(query, {param}))
            If count = 0 Then
                MessageBox.Show("Username not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
            ShowStep(2)
        ElseIf currentStep = 2 Then
            passkey = txtInput.Text.Trim()
            If String.IsNullOrEmpty(passkey) Then
                MessageBox.Show("Please enter your passkey.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If
            ' Validate passkey (check if passkey is in the comma-separated list)
            Dim query = "SELECT COUNT(*) FROM Users WHERE Username = @Username AND (',' + ISNULL(Passkeys,'') + ',') LIKE '%,' + @Passkey + ',%'"
            Dim param1 = New SqlParameter("@Username", username)
            Dim param2 = New SqlParameter("@Passkey", passkey)
            Dim count = Convert.ToInt32(Utilities.ExecuteScalar(query, {param1, param2}))
            If count = 0 Then
                MessageBox.Show("Incorrect passkey.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                Return
            End If
            ShowStep(3)
        ElseIf currentStep = 3 Then
            Dim newPass = txtNewPassword.Text.Trim()
            Dim confirmPass = txtConfirmPassword.Text.Trim()

            If String.IsNullOrEmpty(newPass) OrElse String.IsNullOrEmpty(confirmPass) Then
                MessageBox.Show("Please enter and confirm your new password.", "Input Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Minimum length requirement
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
                ' Hash the new password with BCrypt (work factor 12)
                Dim hashed As String = BCrypt.Net.BCrypt.HashPassword(newPass, workFactor:=12)

                ' Update DB: use your actual password-hash column name (PasswordHash used here)
                Dim query = "UPDATE Users SET PasswordHash = @PasswordHash WHERE Username = @Username"
                Dim param1 = New SqlParameter("@PasswordHash", hashed)
                Dim param2 = New SqlParameter("@Username", username)
                Utilities.ExecuteNonQuery(query, {param1, param2})

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
End Class