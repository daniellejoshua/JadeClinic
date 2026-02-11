Imports System.Windows.Forms
Imports Microsoft.Data.SqlClient
Imports BCrypt.Net
Imports System.Threading.Tasks

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
    Private ReadOnly IDLE_TIMEOUT_SECONDS As Integer = 1800 ' 30 seconds for testing
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

            ' Ensure timer exists and is properly configured
            If idleTimer Is Nothing Then
                Console.WriteLine("Creating new timer in StartMonitoring")
                idleTimer = New Timer()
                idleTimer.Interval = IDLE_TIMEOUT_SECONDS * 1000
                AddHandler idleTimer.Tick, AddressOf OnIdleTimeout
            End If

            ' CRITICAL: Use delayed start to prevent immediate timeout
            Console.WriteLine("Starting monitoring with delay to prevent immediate timeout")
            StartTimerWithDelay()

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

    ' Restart timer with delay to prevent immediate timeout after validation
    Private Sub RestartTimerWithDelay()
        Try
            Console.WriteLine("Restarting timer with delay to prevent immediate timeout")

            ' Ensure timer is stopped and dialog is cleared
            If idleTimer IsNot Nothing Then
                idleTimer.Stop()
            End If

            ' Clear dialog reference to prevent double triggering
            If passwordDialog IsNot Nothing Then
                passwordDialog = Nothing
            End If

            ' Set enabled state
            isTimerEnabled = True

            ' Create a delay timer to restart the idle timer
            Dim delayTimer As New Timer()
            delayTimer.Interval = 5000 ' Increased to 5 second delay to prevent immediate re-trigger
            AddHandler delayTimer.Tick, Sub()
                                            Try
                                                delayTimer.Stop()
                                                delayTimer.Dispose()

                                                ' Now safely restart the idle timer
                                                If isTimerEnabled AndAlso idleTimer IsNot Nothing AndAlso
                                                currentForm IsNot Nothing AndAlso Not currentForm.IsDisposed AndAlso
                                                Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) AndAlso
                                                passwordDialog Is Nothing Then ' Additional check to ensure no dialog is active

                                                    idleTimer.Start()
                                                    Console.WriteLine("Idle timer restarted after delay")
                                                Else
                                                    Console.WriteLine("Cannot restart timer - invalid state or dialog still active")
                                                End If
                                            Catch ex As Exception
                                                Console.WriteLine($"Error in delay timer: {ex.Message}")
                                            End Try
                                        End Sub

            delayTimer.Start()
            Console.WriteLine("Delay timer started - idle timer will restart in 5 seconds")

        Catch ex As Exception
            Console.WriteLine($"Error restarting timer with delay: {ex.Message}")
        End Try
    End Sub

    ' Start timer with delay to prevent immediate timeout when monitoring starts
    Private Sub StartTimerWithDelay()
        Try
            Console.WriteLine("Starting timer with delay to prevent immediate timeout")

            ' Ensure timer is stopped
            If idleTimer IsNot Nothing Then
                idleTimer.Stop()
            End If

            ' Set enabled state
            isTimerEnabled = True

            ' Create a delay timer to start the idle timer
            Dim delayTimer As New Timer()
            delayTimer.Interval = 3000 ' 3 second delay for initial start
            AddHandler delayTimer.Tick, Sub()
                                            Try
                                                delayTimer.Stop()
                                                delayTimer.Dispose()

                                                ' Now safely start the idle timer
                                                If isTimerEnabled AndAlso idleTimer IsNot Nothing AndAlso
                                                currentForm IsNot Nothing AndAlso Not currentForm.IsDisposed AndAlso
                                                Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then

                                                    idleTimer.Start()
                                                    Console.WriteLine("Idle timer started after initial delay")
                                                Else
                                                    Console.WriteLine("Cannot start timer - invalid state")
                                                End If
                                            Catch ex As Exception
                                                Console.WriteLine($"Error in initial delay timer: {ex.Message}")
                                            End Try
                                        End Sub

            delayTimer.Start()
            Console.WriteLine("Initial delay timer started - idle timer will start in 3 seconds")

        Catch ex As Exception
            Console.WriteLine($"Error starting timer with delay: {ex.Message}")
        End Try
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
            ' Check if we have a valid current form and user session AND no dialog is already showing
            If currentForm IsNot Nothing AndAlso Not currentForm.IsDisposed AndAlso
               Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) AndAlso
               passwordDialog Is Nothing Then ' Ensure no dialog is already active
                idleTimer.Stop()
                ShowPasswordDialog()
            Else
                ' Invalid state or dialog already showing - stop the timer
                Console.WriteLine("Invalid state detected in OnIdleTimeout or dialog already showing - stopping timer")
                DisableTimer()
                If passwordDialog IsNot Nothing Then
                    Console.WriteLine("Password dialog already active, not creating new one")
                End If
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
               String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) OrElse
               passwordDialog IsNot Nothing Then ' Prevent multiple dialogs
                Console.WriteLine("Cannot show password dialog - invalid state or dialog already exists")
                If passwordDialog IsNot Nothing Then
                    Console.WriteLine("Password dialog already active")
                End If
                Return
            End If

            ' Disable timer while showing dialog
            DisableTimer()

            ' Create overlay to block interaction with main form
            CreateOverlay()

            ' Create password dialog with new design - Dim Gray background with Gold/White text
            passwordDialog = New Form()
            passwordDialog.Text = "Session Timeout"
            passwordDialog.Size = New Size(500, 380) ' Increased size for better spacing
            passwordDialog.StartPosition = FormStartPosition.CenterParent
            passwordDialog.FormBorderStyle = FormBorderStyle.FixedDialog
            passwordDialog.MaximizeBox = False
            passwordDialog.MinimizeBox = False
            passwordDialog.ControlBox = False ' Disable X button - user must choose Continue or Logout
            passwordDialog.BackColor = System.Drawing.Color.FromArgb(70, 70, 70) ' Dim Gray background
            passwordDialog.TopMost = True

            ' Title label with Golden Yellow
            Dim lblTitle As New Label()
            lblTitle.Text = "🔒 Session Timeout"
            lblTitle.Font = New Font("Segoe UI", 18, FontStyle.Bold)
            lblTitle.ForeColor = System.Drawing.Color.FromArgb(254, 191, 16) ' Golden Yellow
            lblTitle.BackColor = System.Drawing.Color.Transparent
            lblTitle.AutoSize = True
            lblTitle.Location = New Point(0, 25)
            passwordDialog.Controls.Add(lblTitle)

            ' Instruction label with White text
            Dim lblInstruction As New Label()
            lblInstruction.Text = "Your session has timed out due to inactivity." & vbCrLf & "Please enter your password to continue."
            lblInstruction.Font = New Font("Segoe UI", 11, FontStyle.Regular)
            lblInstruction.ForeColor = System.Drawing.Color.White ' White text
            lblInstruction.BackColor = System.Drawing.Color.Transparent
            lblInstruction.AutoSize = False
            lblInstruction.Size = New Size(440, 60)
            lblInstruction.Location = New Point(30, 80)
            lblInstruction.TextAlign = ContentAlignment.MiddleCenter
            passwordDialog.Controls.Add(lblInstruction)

            ' Username label with Golden Yellow
            Dim lblUsername As New Label()
            lblUsername.Text = $"User: {frmLoginvb.LoggedInUsername}"
            lblUsername.Font = New Font("Segoe UI", 10, FontStyle.Bold)
            lblUsername.ForeColor = System.Drawing.Color.FromArgb(254, 191, 16) ' Golden Yellow
            lblUsername.BackColor = System.Drawing.Color.Transparent
            lblUsername.AutoSize = True
            lblUsername.Location = New Point(30, 155)
            passwordDialog.Controls.Add(lblUsername)

            ' Password label with White text
            Dim lblPasswordLabel As New Label()
            lblPasswordLabel.Text = "Password:"
            lblPasswordLabel.Font = New Font("Segoe UI", 10, FontStyle.Regular)
            lblPasswordLabel.ForeColor = System.Drawing.Color.White ' White text
            lblPasswordLabel.BackColor = System.Drawing.Color.Transparent
            lblPasswordLabel.AutoSize = True
            lblPasswordLabel.Location = New Point(30, 185)
            passwordDialog.Controls.Add(lblPasswordLabel)

            ' Password textbox with darker gray background and white text
            Dim txtPassword As New TextBox()
            txtPassword.PasswordChar = "•"c
            txtPassword.Font = New Font("Segoe UI", 12, FontStyle.Regular)
            txtPassword.BackColor = System.Drawing.Color.FromArgb(50, 50, 50) ' Darker gray
            txtPassword.ForeColor = System.Drawing.Color.White ' White text
            txtPassword.Location = New Point(30, 210)
            txtPassword.Size = New Size(440, 35)
            txtPassword.BorderStyle = BorderStyle.FixedSingle
            passwordDialog.Controls.Add(txtPassword)

            ' Continue button with Golden Yellow background and black text
            Dim btnContinue As New Button()
            btnContinue.Text = "Continue"
            btnContinue.Font = New Font("Segoe UI", 11, FontStyle.Bold)
            btnContinue.BackColor = System.Drawing.Color.FromArgb(254, 191, 16) ' Golden Yellow
            btnContinue.ForeColor = System.Drawing.Color.Black ' Black text for contrast
            btnContinue.FlatStyle = FlatStyle.Flat
            btnContinue.FlatAppearance.BorderSize = 0
            btnContinue.Size = New Size(130, 40)
            btnContinue.Location = New Point(210, 270)
            btnContinue.Cursor = Cursors.Hand

            ' Add hover effect for Continue button
            AddHandler btnContinue.MouseEnter, Sub()
                                                   btnContinue.BackColor = System.Drawing.Color.FromArgb(220, 165, 12) ' Darker gold on hover
                                               End Sub
            AddHandler btnContinue.MouseLeave, Sub()
                                                   btnContinue.BackColor = System.Drawing.Color.FromArgb(254, 191, 16) ' Back to original gold
                                               End Sub

            ' Logout button with Alert Red background and white text
            Dim btnLogout As New Button()
            btnLogout.Text = "Logout"
            btnLogout.Font = New Font("Segoe UI", 11, FontStyle.Regular)
            btnLogout.BackColor = System.Drawing.Color.FromArgb(255, 71, 87) ' Alert Red
            btnLogout.ForeColor = System.Drawing.Color.White ' White text
            btnLogout.FlatStyle = FlatStyle.Flat
            btnLogout.FlatAppearance.BorderSize = 0
            btnLogout.Size = New Size(130, 40)
            btnLogout.Location = New Point(350, 270)
            btnLogout.Cursor = Cursors.Hand

            ' Add hover effect for Logout button
            AddHandler btnLogout.MouseEnter, Sub()
                                                 btnLogout.BackColor = System.Drawing.Color.FromArgb(220, 50, 50) ' Darker red on hover
                                             End Sub
            AddHandler btnLogout.MouseLeave, Sub()
                                                 btnLogout.BackColor = System.Drawing.Color.FromArgb(255, 71, 87) ' Back to original red
                                             End Sub

            passwordDialog.Controls.AddRange({btnContinue, btnLogout})

            ' Center title after form is created
            AddHandler passwordDialog.Load, Sub()
                                                lblTitle.Location = New Point((passwordDialog.Width - lblTitle.Width) / 2, 25)
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

            ' IMPORTANT: Use ShowDialog asynchronously to prevent timer conflicts
            Task.Run(Sub()
                         Try
                             Me.currentForm.Invoke(Sub()
                                                       passwordDialog.ShowDialog(currentForm)
                                                   End Sub)
                         Catch ex As Exception
                             Console.WriteLine($"Error showing dialog: {ex.Message}")
                         End Try
                     End Sub)

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

                        ' CRITICAL: Add delay before restarting timer to prevent immediate timeout
                        Console.WriteLine("Password validated - restarting timer with delay")
                        RestartTimerWithDelay()

                        ' Dispose the dialog to prevent memory leaks
                        passwordDialog = Nothing
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