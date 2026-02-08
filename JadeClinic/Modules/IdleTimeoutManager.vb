Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient
Imports BCrypt.Net

Public Class IdleTimeoutManager
    ' Singleton instance
    Private Shared _instance As IdleTimeoutManager
    Public Shared ReadOnly Property Instance As IdleTimeoutManager
        Get
            If _instance Is Nothing Then
                _instance = New IdleTimeoutManager()
            End If
            Return _instance
        End Get
    End Property

    ' Timer and settings
    Private WithEvents idleTimer As Timer
    Private ReadOnly IDLE_TIMEOUT_SECONDS As Integer = 5 ' 30 seconds for testing
    Private isTimerEnabled As Boolean = True
    Private currentForm As Form
    Private overlay As Panel
    Private passwordDialog As Form

    Private Sub New()
        ' Initialize the idle timer
        idleTimer = New Timer()
        idleTimer.Interval = IDLE_TIMEOUT_SECONDS * 1000 ' Convert to milliseconds
        AddHandler idleTimer.Tick, AddressOf OnIdleTimeout
    End Sub

    ' Start monitoring idle time for a form
    Public Sub StartMonitoring(form As Form)
        Try
            Console.WriteLine($"Starting monitoring for form: {form?.Name}")

            ' Stop any existing monitoring first
            If currentForm IsNot Nothing AndAlso currentForm IsNot form Then
                Console.WriteLine($"Stopping previous monitoring for: {currentForm?.Name}")
                StopMonitoring(currentForm)
            End If

            ' Set new current form
            currentForm = form

            ' Ensure timer is enabled and properly configured
            isTimerEnabled = True

            ' Reset and start timer
            ResetIdleTimer()

            ' Attach event handlers to form and all controls
            AttachEventHandlers(form)

            Console.WriteLine($"Monitoring started successfully for {form?.Name}")

        Catch ex As Exception
            Console.WriteLine($"Error starting monitoring: {ex.Message}")
        End Try
    End Sub

    ' Stop monitoring when form closes
    Public Sub StopMonitoring(form As Form)
        Try
            If currentForm Is form Then
                Console.WriteLine($"Stopping monitoring for form: {form?.Name}")

                ' Stop the timer
                idleTimer.Stop()

                ' Detach event handlers if form is not disposed
                If form IsNot Nothing AndAlso Not form.IsDisposed Then
                    DetachEventHandlers(form)
                End If

                ' Remove overlay if it exists
                If overlay IsNot Nothing AndAlso overlay.Parent IsNot Nothing Then
                    overlay.Parent.Controls.Remove(overlay)
                    overlay.Dispose()
                    overlay = Nothing
                End If

                ' Close password dialog if it's open
                If passwordDialog IsNot Nothing AndAlso Not passwordDialog.IsDisposed Then
                    passwordDialog.Close()
                    passwordDialog = Nothing
                End If

                ' Clear current form reference
                currentForm = Nothing

                Console.WriteLine("Monitoring stopped successfully")
            End If
        Catch ex As Exception
            Console.WriteLine($"Error stopping monitoring: {ex.Message}")
            ' Force cleanup even on error
            currentForm = Nothing
            overlay = Nothing
            passwordDialog = Nothing
        End Try
    End Sub

    ' Reset the idle timer (call this on any user activity)
    Public Sub ResetIdleTimer()
        If isTimerEnabled Then
            idleTimer.Stop()
            idleTimer.Start()
        End If
    End Sub

    ' Temporarily disable timer (for specific operations)
    Public Sub DisableTimer()
        isTimerEnabled = False
        idleTimer.Stop()
    End Sub

    ' Re-enable timer
    Public Sub EnableTimer()
        isTimerEnabled = True
        ResetIdleTimer()
    End Sub

    ' Attach event handlers to form and all its controls
    Private Sub AttachEventHandlers(control As Control)
        ' Add event handlers for user activity
        AddHandler control.MouseMove, AddressOf OnUserActivity
        AddHandler control.MouseClick, AddressOf OnUserActivity
        AddHandler control.KeyDown, AddressOf OnUserActivity
        AddHandler control.KeyPress, AddressOf OnUserActivity

        ' Recursively attach to all child controls
        For Each childControl As Control In control.Controls
            AttachEventHandlers(childControl)
        Next
    End Sub

    ' Remove event handlers
    Private Sub DetachEventHandlers(control As Control)
        RemoveHandler control.MouseMove, AddressOf OnUserActivity
        RemoveHandler control.MouseClick, AddressOf OnUserActivity
        RemoveHandler control.KeyDown, AddressOf OnUserActivity
        RemoveHandler control.KeyPress, AddressOf OnUserActivity

        ' Recursively detach from all child controls
        For Each childControl As Control In control.Controls
            DetachEventHandlers(childControl)
        Next
    End Sub

    ' Handle user activity events
    Private Sub OnUserActivity(sender As Object, e As EventArgs)
        ResetIdleTimer()
    End Sub

    ' Handle idle timeout
    Private Sub OnIdleTimeout(sender As Object, e As EventArgs) Handles idleTimer.Tick
        Try
            ' Check if we have a valid current form and user session
            If currentForm IsNot Nothing AndAlso Not currentForm.IsDisposed AndAlso
               Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                idleTimer.Stop()
                ShowPasswordDialog()
            Else
                ' Invalid state - stop the timer and reset
                Console.WriteLine("Invalid state detected in OnIdleTimeout - resetting manager")
                DisableTimer()
                ResetManagerState()
            End If
        Catch ex As Exception
            Console.WriteLine($"Error in OnIdleTimeout: {ex.Message}")
            ' On error, disable the timer to prevent further issues
            DisableTimer()
        End Try
    End Sub

    ' Show password re-authentication dialog
    Private Sub ShowPasswordDialog()
        Try
            ' Additional validation before showing dialog
            If currentForm Is Nothing OrElse currentForm.IsDisposed OrElse
               String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Console.WriteLine("Cannot show password dialog - invalid state")
                ResetManagerState()
                Return
            End If

            ' Disable timer while showing dialog
            DisableTimer()

            ' Create overlay to block interaction with main form
            CreateOverlay()

            ' Create password dialog - Made wider to prevent text cutoff
            passwordDialog = New Form()
            passwordDialog.Text = "Session Timeout"
            passwordDialog.Size = New Size(480, 320) ' Increased from 400x300 to 480x320
            passwordDialog.StartPosition = FormStartPosition.CenterParent
            passwordDialog.FormBorderStyle = FormBorderStyle.FixedDialog
            passwordDialog.MaximizeBox = False
            passwordDialog.MinimizeBox = False
            passwordDialog.BackColor = System.Drawing.Color.FromArgb(41, 44, 45)
            passwordDialog.TopMost = True

            ' Title label
            Dim lblTitle As New Label()
            lblTitle.Text = "?? Session Timeout"
            lblTitle.Font = New Font("Poppins", 16, FontStyle.Bold)
            lblTitle.ForeColor = System.Drawing.Color.FromArgb(254, 191, 16) ' Golden Yellow
            lblTitle.BackColor = System.Drawing.Color.Transparent
            lblTitle.AutoSize = True
            lblTitle.Location = New Point(0, 30)
            passwordDialog.Controls.Add(lblTitle)

            ' Instruction label - Increased width for better text layout
            Dim lblInstruction As New Label()
            lblInstruction.Text = "Your session has timed out due to inactivity." & vbCrLf & "Please enter your password to continue."
            lblInstruction.Font = New Font("Poppins", 10, FontStyle.Regular)
            lblInstruction.ForeColor = System.Drawing.Color.White
            lblInstruction.BackColor = System.Drawing.Color.Transparent
            lblInstruction.AutoSize = False
            lblInstruction.Size = New Size(420, 50) ' Increased from 350 to 420
            lblInstruction.Location = New Point(30, 80) ' Adjusted left margin slightly
            lblInstruction.TextAlign = ContentAlignment.MiddleCenter
            passwordDialog.Controls.Add(lblInstruction)

            ' Username label (read-only)
            Dim lblUsername As New Label()
            lblUsername.Text = $"User: {frmLoginvb.LoggedInUsername}"
            lblUsername.Font = New Font("Poppins", 9, FontStyle.Regular)
            lblUsername.ForeColor = System.Drawing.Color.FromArgb(190, 154, 48) ' Rich Olive
            lblUsername.BackColor = System.Drawing.Color.Transparent
            lblUsername.AutoSize = True
            lblUsername.Location = New Point(30, 140) ' Adjusted left margin
            passwordDialog.Controls.Add(lblUsername)

            ' Password textbox - Made wider to match form width
            Dim txtPassword As New TextBox()
            txtPassword.PasswordChar = "•"c
            txtPassword.Font = New Font("Poppins", 12, FontStyle.Regular)
            txtPassword.BackColor = System.Drawing.Color.FromArgb(61, 65, 66)
            txtPassword.ForeColor = System.Drawing.Color.White
            txtPassword.Location = New Point(30, 170)
            txtPassword.Size = New Size(420, 35) ' Increased from 330 to 420
            txtPassword.BorderStyle = BorderStyle.FixedSingle
            passwordDialog.Controls.Add(txtPassword)

            ' Continue button - Better positioning with more space
            Dim btnContinue As New Button()
            btnContinue.Text = "Continue"
            btnContinue.Font = New Font("Poppins", 10, FontStyle.Bold)
            btnContinue.BackColor = System.Drawing.Color.FromArgb(254, 191, 16) ' Golden Yellow
            btnContinue.ForeColor = System.Drawing.Color.Black
            btnContinue.FlatStyle = FlatStyle.Flat
            btnContinue.FlatAppearance.BorderSize = 0
            btnContinue.Size = New Size(120, 35) ' Increased width from 100 to 120
            btnContinue.Location = New Point(200, 230) ' Repositioned for better centering
            btnContinue.Cursor = Cursors.Hand

            ' Logout button - Better positioning with more space
            Dim btnLogout As New Button()
            btnLogout.Text = "Logout"
            btnLogout.Font = New Font("Poppins", 10, FontStyle.Regular)
            btnLogout.BackColor = System.Drawing.Color.FromArgb(255, 71, 87) ' Alert Red
            btnLogout.ForeColor = System.Drawing.Color.White
            btnLogout.FlatStyle = FlatStyle.Flat
            btnLogout.FlatAppearance.BorderSize = 0
            btnLogout.Size = New Size(120, 35) ' Increased width from 100 to 120
            btnLogout.Location = New Point(330, 230) ' Repositioned for better spacing
            btnLogout.Cursor = Cursors.Hand

            passwordDialog.Controls.AddRange({btnContinue, btnLogout})

            ' Center title after form is created
            AddHandler passwordDialog.Load, Sub()
                                                lblTitle.Location = New Point((passwordDialog.Width - lblTitle.Width) / 2, 30)
                                            End Sub

            ' Event handlers
            AddHandler btnContinue.Click, Sub()
                                              ValidatePasswordAndContinue(txtPassword.Text, passwordDialog)
                                          End Sub

            AddHandler btnLogout.Click, Sub()
                                            LogoutUser(passwordDialog)
                                        End Sub

            AddHandler txtPassword.KeyDown, Sub(s, eArgs)
                                                If eArgs.KeyCode = Keys.Enter Then
                                                    ValidatePasswordAndContinue(txtPassword.Text, passwordDialog)
                                                End If
                                            End Sub

            ' Show dialog
            txtPassword.Focus()
            passwordDialog.ShowDialog(currentForm)

        Catch ex As Exception
            Console.WriteLine($"Error showing password dialog: {ex.Message}")
            ' If there's an error, reset the manager state
            ResetManagerState()
        End Try
    End Sub

    ' Create overlay to block interaction
    Private Sub CreateOverlay()
        If currentForm IsNot Nothing Then
            overlay = New Panel()
            overlay.BackColor = System.Drawing.Color.FromArgb(100, 0, 0, 0) ' Semi-transparent black
            overlay.Dock = DockStyle.Fill
            overlay.BringToFront()

            ' Add to current form
            currentForm.Controls.Add(overlay)
            overlay.BringToFront()
        End If
    End Sub

    ' Validate password and continue or show error
    Private Sub ValidatePasswordAndContinue(enteredPassword As String, dialog As Form)
        Try
            If String.IsNullOrEmpty(enteredPassword) Then
                MessageBox.Show("Please enter your password.", "Password Required", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return
            End If

            ' Get user's password hash from database
            Dim query As String = "SELECT PasswordHash FROM Users WHERE Username = @Username"
            Dim parameters As SqlParameter() = {
                New SqlParameter("@Username", frmLoginvb.LoggedInUsername)
            }

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                If reader.Read() Then
                    Dim storedPasswordHash As String = reader("PasswordHash").ToString()
                    Dim isPasswordValid As Boolean = False

                    ' Check if it's a BCrypt hash
                    If storedPasswordHash.StartsWith("$2a$") OrElse storedPasswordHash.StartsWith("$2b$") Then
                        ' Use BCrypt verification
                        isPasswordValid = BCrypt.Net.BCrypt.Verify(enteredPassword, storedPasswordHash)
                    Else
                        ' Legacy SHA256 hash
                        Dim enteredPasswordSHA256 As String = HashPasswordSHA256(enteredPassword)
                        isPasswordValid = (enteredPasswordSHA256 = storedPasswordHash)
                    End If

                    If isPasswordValid Then
                        ' Password is correct - continue session
                        dialog.Close()
                        RemoveOverlay()

                        ' Log the session continuation
                        Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Session Continued", "User re-authenticated after idle timeout")

                        ' Re-enable timer
                        EnableTimer()
                    Else
                        ' Invalid password
                        MessageBox.Show("Incorrect password. Please try again.", "Authentication Failed", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    End If
                Else
                    ' User not found (should not happen)
                    MessageBox.Show("User not found. Logging out.", "Authentication Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                    LogoutUser(dialog)
                End If
            End Using

        Catch ex As Exception
            MessageBox.Show($"Authentication error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            LogoutUser(dialog)
        End Try
    End Sub

    ' Logout user
    Private Sub LogoutUser(dialog As Form)
        Try
            ' Log the logout action
            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Session Timeout Logout", "User logged out due to idle timeout")
            End If

            ' CRITICAL: Store reference to current form before clearing it
            Dim formToClose As Form = currentForm

            ' Stop monitoring completely before logout
            DisableTimer()

            ' Clear monitoring but keep the form reference for closing
            If currentForm IsNot Nothing Then
                StopMonitoring(currentForm) ' This will set currentForm = Nothing
            End If

            ' Close the timeout dialog first
            If dialog IsNot Nothing AndAlso Not dialog.IsDisposed Then
                dialog.Close()
            End If
            RemoveOverlay()

            ' Clear user session
            frmLoginvb.LogoutUser()

            ' Now close the actual form that was being monitored - SET NAVIGATION FLAG
            If formToClose IsNot Nothing AndAlso Not formToClose.IsDisposed Then
                Console.WriteLine($"Closing monitored form: {formToClose.Name}")

                ' Check if the form has an isNavigating property (like Dashboard)
                Try
                    Dim isNavigatingField = formToClose.GetType().GetField("isNavigating",
                        Reflection.BindingFlags.NonPublic Or Reflection.BindingFlags.Instance)
                    If isNavigatingField IsNot Nothing Then
                        isNavigatingField.SetValue(formToClose, True)
                        Console.WriteLine("Set isNavigating flag to True")
                    End If
                Catch
                    ' If field doesn't exist, continue without error
                End Try

                formToClose.Close() ' Close instead of Hide to properly dispose
            End If

            ' Reset the singleton instance state completely
            ResetManagerState()

            ' Show login form
            Try
                Dim loginForm As New frmLoginvb()
                loginForm.Show()
            Catch loginEx As Exception
                Console.WriteLine($"Error creating login form: {loginEx.Message}")
                ' If we can't show login form, try to exit gracefully
                MessageBox.Show("Session ended. Please restart the application.", "Logout Complete",
                               MessageBoxButtons.OK, MessageBoxIcon.Information)
                Application.Exit()
            End Try

        Catch ex As Exception
            Console.WriteLine($"Error during logout: {ex.Message}")

            ' Don't exit immediately - try to recover
            Try
                ' Clean up what we can
                DisableTimer()
                ResetManagerState()

                ' Close any open dialogs
                If dialog IsNot Nothing AndAlso Not dialog.IsDisposed Then
                    dialog.Close()
                End If

                ' Clear session
                frmLoginvb.LogoutUser()

                ' Try to show login form
                Dim loginForm As New frmLoginvb()
                loginForm.Show()

                ' Show a user-friendly message
                MessageBox.Show("Your session has been ended due to a timeout. Please login again.",
                               "Session Timeout", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Catch recoveryEx As Exception
                Console.WriteLine($"Error during logout recovery: {recoveryEx.Message}")
                ' Only exit as last resort
                MessageBox.Show("Session ended due to an error. The application will now close.",
                               "Session Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Application.Exit()
            End Try
        End Try
    End Sub

    ' Add a new method to reset the manager state completely
    Private Sub ResetManagerState()
        Try
            ' Stop and dispose timer
            If idleTimer IsNot Nothing Then
                idleTimer.Stop()
                RemoveHandler idleTimer.Tick, AddressOf OnIdleTimeout
                idleTimer.Dispose()
                idleTimer = Nothing
            End If

            ' Clear form references
            currentForm = Nothing

            ' Remove overlay if exists
            RemoveOverlay()

            ' Close password dialog if open
            If passwordDialog IsNot Nothing AndAlso Not passwordDialog.IsDisposed Then
                passwordDialog.Close()
                passwordDialog = Nothing
            End If

            ' Reset state variables
            isTimerEnabled = True

            ' Recreate the timer for next login session
            idleTimer = New Timer()
            idleTimer.Interval = IDLE_TIMEOUT_SECONDS * 1000
            AddHandler idleTimer.Tick, AddressOf OnIdleTimeout

            Console.WriteLine("IdleTimeoutManager state reset successfully")

        Catch ex As Exception
            Console.WriteLine($"Error resetting manager state: {ex.Message}")
        End Try
    End Sub

    ' Also add a public method to reset from external calls
    Public Sub ResetManager()
        Try
            Console.WriteLine("Public ResetManager called")

            ' Complete reset of the singleton instance
            DisableTimer()

            ' Clear all references
            currentForm = Nothing
            overlay = Nothing
            passwordDialog = Nothing

            ' Reset timer state
            isTimerEnabled = True

            ' Dispose and recreate timer
            If idleTimer IsNot Nothing Then
                idleTimer.Stop()
                RemoveHandler idleTimer.Tick, AddressOf OnIdleTimeout
                idleTimer.Dispose()
                idleTimer = Nothing
            End If

            ' Create fresh timer
            idleTimer = New Timer()
            idleTimer.Interval = IDLE_TIMEOUT_SECONDS * 1000
            AddHandler idleTimer.Tick, AddressOf OnIdleTimeout

            Console.WriteLine("IdleTimeoutManager completely reset for new session")

        Catch ex As Exception
            Console.WriteLine($"Error in public ResetManager: {ex.Message}")
        End Try
    End Sub

    ' Remove overlay
    Private Sub RemoveOverlay()
        If overlay IsNot Nothing AndAlso overlay.Parent IsNot Nothing Then
            overlay.Parent.Controls.Remove(overlay)
            overlay.Dispose()
            overlay = Nothing
        End If
    End Sub

    ' Get hash function (to avoid dependency issues)
    Private Function HashPasswordSHA256(password As String) As String
        Using sha256 As System.Security.Cryptography.SHA256 = System.Security.Cryptography.SHA256.Create()
            Dim bytes As Byte() = System.Text.Encoding.UTF8.GetBytes(password)
            Dim hash As Byte() = sha256.ComputeHash(bytes)
            Return Convert.ToBase64String(hash)
        End Using
    End Function
End Class