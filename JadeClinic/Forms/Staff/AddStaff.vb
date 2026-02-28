Imports Microsoft.Data.SqlClient
Imports System.Drawing
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text

Public Class AddStaff
    Private staffPhotoBytes As Byte() = Nothing
    Private isEditMode As Boolean = False
    Private editingStaffId As Integer = 0
    Private originalImagePath As String = ""
    Private editingUserData As Dictionary(Of String, Object) = Nothing
    Private originalIsActive As Boolean = True ' track original status for edit-mode behavior

    Private Sub AddStaff_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SetupForm()
        SetDefaultImage()
        InitializeRoleComboBox()

        ' If not in edit mode, perform initial validation
        If Not isEditMode Then
            ValidateForm()
        Else
            ' In edit mode, ensure the image is properly displayed
            Console.WriteLine("Form loaded in edit mode - refreshing display")
        End If

        ' Add Shown event handler for edit mode image refresh
        AddHandler Me.Shown, AddressOf AddStaff_Shown
    End Sub

    Private Sub AddStaff_Shown(sender As Object, e As EventArgs)
        Try
            If isEditMode Then
                Console.WriteLine("Form shown in edit mode - refreshing image")

                ' Small delay to ensure form is fully rendered
                System.Threading.Thread.Sleep(50)
                Application.DoEvents()

                ' Refresh image display
                RefreshImageDisplay()
            End If
        Catch ex As Exception
            Console.WriteLine($"Error in AddStaff_Shown: {ex.Message}")
        End Try
    End Sub

    Private Sub SetupForm()
        ' Configure form appearance
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False

        ' Set default values
        cmbRole.SelectedIndex = 0 ' Default to "Staff"

        ' Configure PIN field to only accept 4 numbers
        txtPin.MaxLength = 4
        txtPin.PlaceholderText = "Enter 4-digit PIN"
        AddHandler txtPin.KeyPress, AddressOf txtPin_KeyPress ' Only allow numbers

        ' Configure phone field
        txtPhone.MaxLength = 11
        txtPhone.PlaceholderText = "09xxxxxxxxx"

        ' Configure email field
        txtEmail.PlaceholderText = "example@gmail.com"

        ' Configure image click event
        AddHandler ProductImage.Click, AddressOf ProductImage_Click
        AddHandler lblStaffPicture.Click, AddressOf ProductImage_Click

        ' Add validation events
        AddHandler txtUsername.TextChanged, AddressOf ValidateForm
        AddHandler txtPassword.TextChanged, AddressOf ValidateForm
        AddHandler txtEmail.TextChanged, AddressOf ValidateForm
        AddHandler txtPhone.TextChanged, AddressOf ValidateForm
        AddHandler txtPin.TextChanged, AddressOf ValidateForm

        ' Initially disable save button
        btnAddStock.Enabled = False
        btnAddStock.Text = "Add Staff"

        ' Set up cancel functionality
        AddHandler Guna2HtmlLabel1.Click, AddressOf CancelAddStaff

        ' Position the validation/message label to top-right as requested
        Try
            Guna2HtmlLabel15.Location = New Point(200, 21)
            Guna2HtmlLabel15.BringToFront()
        Catch
            ' Ignore if control not present or designer-managed
        End Try

        ' Hide status controls by default (only visible in edit mode)
        Try
            lblStatus.Visible = False
            Guna2ComboBox1.Visible = False
        Catch
            ' Designer may name them differently; ignore if missing
        End Try
    End Sub

    ' Event handler to only allow numeric input for PIN
    Private Sub txtPin_KeyPress(sender As Object, e As KeyPressEventArgs)
        ' Only allow numbers and control characters (backspace, etc.)
        If Not Char.IsDigit(e.KeyChar) AndAlso Not Char.IsControl(e.KeyChar) Then
            e.Handled = True
        End If
    End Sub

    Private Sub SetDefaultImage()
        Try
            ' Dispose existing image if any
            If ProductImage.Image IsNot Nothing Then
                ProductImage.Image.Dispose()
                ProductImage.Image = Nothing
            End If

            ' Create a simple placeholder
            Dim placeholder As New Bitmap(196, 140)
            Using g As Graphics = Graphics.FromImage(placeholder)
                g.Clear(Color.FromArgb(240, 240, 240))

                ' Draw a border
                Using pen As New Pen(Color.FromArgb(200, 200, 200), 2)
                    g.DrawRectangle(pen, 1, 1, 194, 138)
                End Using

                Using font As New Font("Segoe UI", 12, FontStyle.Regular)
                    Dim text As String = "Click to" & vbCrLf & "Upload Photo"
                    Dim brush As New SolidBrush(Color.Gray)
                    Dim format As New StringFormat With {
                        .Alignment = StringAlignment.Center,
                        .LineAlignment = StringAlignment.Center
                    }
                    g.DrawString(text, font, brush, New Rectangle(0, 0, 196, 140), format)
                End Using
            End Using

            ProductImage.Image = placeholder
            ProductImage.SizeMode = PictureBoxSizeMode.CenterImage

            ' Reset photo bytes if this is called during edit mode reset
            If Not isEditMode Then
                staffPhotoBytes = Nothing
            End If

            Console.WriteLine("Default image set successfully")

        Catch ex As Exception
            Console.WriteLine($"Error setting default image: {ex.Message}")
        End Try
    End Sub

    Private Sub InitializeRoleComboBox()
        cmbRole.Items.Clear()
        ' Limit roles to only Staff, Admin, and Manager as requested
        cmbRole.Items.AddRange(New String() {"Staff", "Admin", "Manager"})
        cmbRole.SelectedIndex = 0 ' Default to "Staff"
    End Sub

    Private Sub ProductImage_Click(sender As Object, e As EventArgs)
        UploadStaffPhoto()
    End Sub

    Private Sub lblStaffPicture_Click(sender As Object, e As EventArgs) Handles lblStaffPicture.Click
        UploadStaffPhoto()
    End Sub

    Private Sub UploadStaffPhoto()
        Try
            Using openFileDialog As New OpenFileDialog()
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.tiff|All Files|*.*"
                openFileDialog.FilterIndex = 1
                openFileDialog.Title = "Select Staff Photo"
                openFileDialog.Multiselect = False

                If openFileDialog.ShowDialog() = DialogResult.OK Then
                    Dim selectedFile As String = openFileDialog.FileName

                    ' Validate image file
                    If Not ImageCompression.IsValidImageFile(selectedFile) Then
                        MessageBox.Show("Please select a valid image file.", "Invalid File",
                                      MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    ' Check file size (limit to 10MB before compression)
                    Dim fileInfo As New FileInfo(selectedFile)
                    If fileInfo.Length > 10 * 1024 * 1024 Then ' 10MB
                        MessageBox.Show("Image file is too large. Please select an image smaller than 10MB.",
                                      "File Too Large", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If

                    ProcessAndCompressImage(selectedFile)
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error uploading image: {ex.Message}", "Upload Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Replace the existing ProcessAndCompressImage method with this version.
    ' - Creates a scaled display bitmap that fills the PictureBox (prevents Zoom-like cropping).
    ' - Stores a compressed bytes copy for DB (quality + max size).
    ' - Ensures PictureBox shows stretched/full image.
    Private Sub ProcessAndCompressImage(imagePath As String)
        Try
            ' Show processing indicator
            lblStaffPicture.Text = "Processing..."
            Application.DoEvents()

            ' Load original image from file
            Using originalImage As Image = Image.FromFile(imagePath)
                originalImagePath = imagePath

                ' Create a display bitmap sized to the PictureBox to avoid "zoom" appearance.
                Dim displayW As Integer = Math.Max(1, ProductImage.Width)
                Dim displayH As Integer = Math.Max(1, ProductImage.Height)
                Dim displayBitmap As New Bitmap(displayW, displayH)

                Using g As Graphics = Graphics.FromImage(displayBitmap)
                    g.Clear(Color.Transparent)
                    g.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                    g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.HighQuality
                    ' Draw the original image stretched to fill the control (will behave like StretchImage)
                    g.DrawImage(originalImage, New Rectangle(0, 0, displayW, displayH))
                End Using

                ' Replace PictureBox image safely
                If ProductImage.Image IsNot Nothing Then
                    ProductImage.Image.Dispose()
                End If
                ProductImage.Image = displayBitmap
                ProductImage.SizeMode = PictureBoxSizeMode.Normal

                ' Compress image for database storage (quality 70, max 400x400) - kept as before
                staffPhotoBytes = ImageCompression.CompressImage(originalImage, 70, 400, 400)

                ' Show compression results
                Dim originalSize As Long = New FileInfo(imagePath).Length
                Dim compressedSize As Long = If(staffPhotoBytes IsNot Nothing, staffPhotoBytes.Length, 0)
                Dim compressionRatio As Double = If(originalSize > 0, (1 - (CDbl(compressedSize) / originalSize)) * 100, 0)

                lblStaffPicture.Text = $"Optimized ({ImageCompression.FormatFileSize(compressedSize)}, {compressionRatio:F0}% smaller)"
                Console.WriteLine($"Image compressed: {ImageCompression.FormatFileSize(originalSize)} → {ImageCompression.FormatFileSize(compressedSize)} ({compressionRatio:F1}% reduction)")
            End Using

            ' Re-validate form after image upload
            ValidateForm()

        Catch ex As Exception
            lblStaffPicture.Text = "Upload"
            MessageBox.Show($"Error processing image: {ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Replace the existing ValidateForm method with this more robust, trim-aware version.
    ' Replace ValidateForm so edit mode skips validation and still shows messages on Guna2HtmlLabel15 (top-right).
    Private Sub ValidateForm()
        Try
            ' If editing an existing user: relax validation entirely and keep Update enabled.
            If isEditMode Then
                btnAddStock.Enabled = True
                Try
                    Guna2HtmlLabel15.Text = String.Empty
                Catch
                End Try
                Return
            End If

            ' Normal add-staff validation (unchanged)
            Dim username = If(txtUsername.Text, String.Empty).Trim()
            Dim password = If(txtPassword.Text, String.Empty).Trim()
            Dim email = If(txtEmail.Text, String.Empty).Trim()
            Dim phone = If(txtPhone.Text, String.Empty).Trim()
            Dim pin = If(txtPin.Text, String.Empty).Trim()
            Dim roleSelected = (cmbRole.SelectedIndex <> -1)

            Dim isValid As Boolean = True
            Dim errorMessage As String = String.Empty

            If String.IsNullOrWhiteSpace(username) Then
                isValid = False
                errorMessage = "Username is required."
            ElseIf username.Length < 3 Then
                isValid = False
                errorMessage = "Username must be at least 3 characters."
            ElseIf Not isEditMode AndAlso String.IsNullOrWhiteSpace(password) Then
                isValid = False
                errorMessage = "Password is required."
            ElseIf Not String.IsNullOrWhiteSpace(password) AndAlso password.Length < 6 Then
                isValid = False
                errorMessage = "Password must be at least 6 characters."
            ElseIf String.IsNullOrWhiteSpace(email) Then
                isValid = False
                errorMessage = "Email is required."
            ElseIf Not IsValidEmailFormat(email) Then
                isValid = False
                errorMessage = "Email must be a valid @gmail.com address."
            ElseIf String.IsNullOrWhiteSpace(phone) Then
                isValid = False
                errorMessage = "Phone number is required."
            ElseIf Not IsValidPhoneFormat(phone) Then
                isValid = False
                errorMessage = "Phone must be 11 digits starting with 09."
            ElseIf String.IsNullOrWhiteSpace(pin) Then
                isValid = False
                errorMessage = "PIN is required."
            ElseIf Not IsValidPinFormat(pin) Then
                isValid = False
                errorMessage = "PIN must be exactly 4 numbers."
            ElseIf Not roleSelected Then
                isValid = False
                errorMessage = "Please select a role."
            End If

            If isValid AndAlso Not String.IsNullOrWhiteSpace(username) Then
                If Not isEditMode AndAlso CheckDuplicateUsername(username) Then
                    isValid = False
                    errorMessage = "Username already exists."
                End If
            End If

            btnAddStock.Enabled = isValid

            ' Show validation message at top-right label (Guna2HtmlLabel15)
            Try
                Guna2HtmlLabel15.Text = If(isValid, String.Empty, errorMessage)
                Guna2HtmlLabel15.ForeColor = If(isValid, Color.FromArgb(16, 216, 98), Color.FromArgb(255, 71, 87))
            Catch
            End Try

            If Not isValid AndAlso Not String.IsNullOrEmpty(errorMessage) Then
                Console.WriteLine($"Validation Error: {errorMessage}")
            End If

        Catch ex As Exception
            btnAddStock.Enabled = False
            Console.WriteLine($"Form validation error: {ex.Message}")
        End Try
    End Sub

    ' Smart email validation - must be @gmail.com
    Private Function IsValidEmailFormat(email As String) As Boolean
        Try
            If String.IsNullOrWhiteSpace(email) Then Return False

            ' Must contain @gmail.com
            If Not email.ToLower().EndsWith("@gmail.com") Then Return False

            ' Basic email format validation
            Dim addr As New System.Net.Mail.MailAddress(email)
            Return addr.Address = email
        Catch
            Return False
        End Try
    End Function

    ' Smart phone validation - must be 11 digits starting with 09
    Private Function IsValidPhoneFormat(phone As String) As Boolean
        Try
            If String.IsNullOrWhiteSpace(phone) Then Return False

            ' Remove any non-digit characters
            Dim cleanPhone As String = System.Text.RegularExpressions.Regex.Replace(phone, "[^\d]", "")

            ' Must be exactly 11 digits and start with 09
            Return cleanPhone.Length = 11 AndAlso cleanPhone.StartsWith("09")
        Catch
            Return False
        End Try
    End Function

    ' Smart PIN validation - must be exactly 4 numbers
    Private Function IsValidPinFormat(pin As String) As Boolean
        Try
            If String.IsNullOrWhiteSpace(pin) Then Return False

            ' Must be exactly 4 digits
            Return pin.Length = 4 AndAlso pin.All(AddressOf Char.IsDigit)
        Catch
            Return False
        End Try
    End Function

    Private Function CheckDuplicateUsername(username As String) As Boolean
        Try
            Dim query As String = "SELECT COUNT(*) FROM Users WHERE Username = @Username"
            Dim parameters() As SqlParameter = {
                New SqlParameter("@Username", username)
            }

            Dim count As Integer = CInt(Utilities.ExecuteScalar(query, parameters))
            Return count > 0
        Catch ex As Exception
            Console.WriteLine($"Error checking duplicate username: {ex.Message}")
            Return False ' Assume no duplicate if error occurs
        End Try
    End Function

    Private Sub btnAddStock_Click(sender As Object, e As EventArgs) Handles btnAddStock.Click
        SaveStaffMember()
    End Sub

    Private Sub SaveStaffMember()
        Try
            ' Disable button to prevent double-click
            btnAddStock.Enabled = False
            If isEditMode Then
                btnAddStock.Text = "Updating..."
            Else
                btnAddStock.Text = "Saving..."
            End If

            ' Validate form one more time
            ValidateForm()
            If Not btnAddStock.Enabled Then
                btnAddStock.Text = If(isEditMode, "Update Staff", "Add Staff")
                Return
            End If

            If isEditMode Then
                UpdateExistingStaff()
            Else
                CreateNewStaff()
            End If

        Catch ex As Exception
            btnAddStock.Enabled = True
            btnAddStock.Text = If(isEditMode, "Update Staff", "Add Staff")
            MessageBox.Show($"Error saving staff member: {ex.Message}", "Save Error",
                      MessageBoxButtons.OK, MessageBoxIcon.Error)
            Console.WriteLine($"Staff save error: {ex.Message}")
        End Try
    End Sub

    Private Sub CreateNewStaff()
        ' Generate QR code for the user
        Dim userCode As String = GenerateUserCode()

        ' Hash the password using the same method as login
        Dim hashedPassword As String = frmLoginvb.HashPassword(txtPassword.Text.Trim())

        ' Prepare staff data
        Dim username As String = txtUsername.Text.Trim()
        Dim email As String = txtEmail.Text.Trim()
        Dim phone As String = txtPhone.Text.Trim()
        Dim pin As String = txtPin.Text.Trim()
        Dim role As String = cmbRole.SelectedItem.ToString()
        Dim fullName As String = username ' You might want to add separate first/last name fields

        ' Insert staff member into Users table (matching your actual schema)
        Dim insertQuery As String = "
        INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, UserRole, pin, QRCode, IsActive, CreatedAt, UpdatedAt) 
        VALUES (@Username, @PasswordHash, @FullName, @Email, @Phone, @UserRole, @Pin, @QRCode, @IsActive, @CreatedAt, @UpdatedAt);
        SELECT SCOPE_IDENTITY();"

        Dim parameters() As SqlParameter = {
        New SqlParameter("@Username", username),
        New SqlParameter("@PasswordHash", hashedPassword),
        New SqlParameter("@FullName", fullName),
        New SqlParameter("@Email", email),
        New SqlParameter("@Phone", phone),
        New SqlParameter("@UserRole", role),
        New SqlParameter("@Pin", Convert.ToInt32(pin)),
        New SqlParameter("@QRCode", userCode),
        New SqlParameter("@IsActive", True),
        New SqlParameter("@CreatedAt", DateTime.Now),
        New SqlParameter("@UpdatedAt", DateTime.Now)
    }

        ' Execute insert and get new user ID
        Dim newUserId As Integer = Convert.ToInt32(Utilities.ExecuteScalar(insertQuery, parameters))

        ' Generate and save passkeys for forgot password functionality
        Dim passkeys As String() = GenerateUserPasskeys(newUserId)

        ' Save staff photo if uploaded (to the Photo field in Users table)
        If staffPhotoBytes IsNot Nothing AndAlso staffPhotoBytes.Length > 0 Then
            SaveStaffPhoto(newUserId)
        End If

        ' Log the action
        Utilities.LogAudit(username, "Staff Added", $"New staff member added: {fullName} ({role})", newUserId)

        ' Use Environment.NewLine instead of vbNewLine
        Dim nl As String = System.Environment.NewLine
        Dim successMessage As String =
        "Staff member '" & fullName & "' has been successfully added!" & nl & nl &
        "Username: " & username & nl &
        "Role: " & role & nl &
        "QR Code: " & userCode & nl & nl &
        "🔑 Recovery Passkeys (for forgot password):" & nl &
        String.Join(nl, passkeys) & nl & nl &
        "⚠️ Please save these passkeys securely! They can be used for password recovery."

        MessageBox.Show(successMessage, "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        ' Close the AddStaff modal after successful addition
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub UpdateExistingStaff()
        ' Prepare staff data
        Dim email As String = txtEmail.Text.Trim()
        Dim phone As String = txtPhone.Text.Trim()
        Dim pin As String = txtPin.Text.Trim()
        Dim role As String = cmbRole.SelectedItem.ToString()
        Dim username As String = txtUsername.Text.Trim()

        ' Build update query - only update password if provided
        Dim updateQuery As String
        Dim parameters As New List(Of SqlParameter)

        ' Determine IsActive from status combobox (default to original if control missing)
        Dim isActiveValue As Boolean = originalIsActive
        Try
            If Guna2ComboBox1 IsNot Nothing AndAlso Guna2ComboBox1.SelectedItem IsNot Nothing Then
                isActiveValue = (Guna2ComboBox1.SelectedItem.ToString().Equals("Active", StringComparison.OrdinalIgnoreCase))
            End If
        Catch
            isActiveValue = originalIsActive
        End Try

        If Not String.IsNullOrWhiteSpace(txtPassword.Text) Then
            ' Update with new password
            Dim hashedPassword As String = frmLoginvb.HashPassword(txtPassword.Text.Trim())
            updateQuery = "UPDATE Users SET Email = @Email, Phone = @Phone, UserRole = @UserRole, pin = @Pin, PasswordHash = @PasswordHash, IsActive = @IsActive, UpdatedAt = @UpdatedAt WHERE UserID = @UserID"
            parameters.AddRange({
                New SqlParameter("@Email", email),
                New SqlParameter("@Phone", phone),
                New SqlParameter("@UserRole", role),
                New SqlParameter("@Pin", Convert.ToInt32(pin)),
                New SqlParameter("@PasswordHash", hashedPassword),
                New SqlParameter("@IsActive", isActiveValue),
                New SqlParameter("@UpdatedAt", DateTime.Now),
                New SqlParameter("@UserID", editingStaffId)
            })
        Else
            ' Update without changing password
            updateQuery = "UPDATE Users SET Email = @Email, Phone = @Phone, UserRole = @UserRole, pin = @Pin, IsActive = @IsActive, UpdatedAt = @UpdatedAt WHERE UserID = @UserID"
            parameters.AddRange({
                New SqlParameter("@Email", email),
                New SqlParameter("@Phone", phone),
                New SqlParameter("@UserRole", role),
                New SqlParameter("@Pin", Convert.ToInt32(pin)),
                New SqlParameter("@IsActive", isActiveValue),
                New SqlParameter("@UpdatedAt", DateTime.Now),
                New SqlParameter("@UserID", editingStaffId)
            })
        End If

        ' Execute update
        Dim rowsAffected As Integer = Utilities.ExecuteNonQuery(updateQuery, parameters.ToArray())

        If rowsAffected > 0 Then
            ' Save staff photo if uploaded
            If staffPhotoBytes IsNot Nothing AndAlso staffPhotoBytes.Length > 0 Then
                SaveStaffPhoto(editingStaffId)
            End If

            ' Log the action
            Utilities.LogAudit(username, "Staff Updated", $"Staff member updated: {username} ({role})", editingStaffId)

            MessageBox.Show($"Staff member '{username}' has been successfully updated!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

            ' Close the form
            Me.Close()
        Else
            MessageBox.Show("Failed to update staff member.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End If
    End Sub

    ' Replace the existing passkey helpers with these safer, more diagnostic implementations.

    ' Returns True when DB check shows passkey is unused; if the DB call fails we return True
    ' so generation will continue (prevents permanent failure when DB check cannot run).
    Private Function IsPasskeyUnique(passkey As String) As Boolean
        Try
            Dim query As String = "SELECT COUNT(*) FROM Users WHERE Passkey1 = @p OR Passkey2 = @p OR Passkey3 = @p"
            Dim param As New SqlParameter("@p", passkey)
            Dim count As Integer = CInt(Utilities.ExecuteScalar(query, New SqlParameter() {param}))
            Return count = 0
        Catch ex As Exception
            ' Log and treat as unique to avoid blocking generation when DB is unavailable or schema doesn't contain columns.
            Console.WriteLine($"Passkey uniqueness check failed (treating as unique): {ex.Message}")
            Return True
        End Try
    End Function

    ' Cryptographically-secure random hex token
    Private Function GenerateHexToken(byteLength As Integer) As String
        Dim bytes(byteLength - 1) As Byte
        Using rng = RandomNumberGenerator.Create()
            rng.GetBytes(bytes)
        End Using
        Return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant()
    End Function

    ' Generate three secure passkeys, attempt to persist them.
    ' Each passkey will be 12 hex characters (6 bytes -> 12 hex chars).
    ' If persistence fails we still return generated keys (and log the error) instead of returning ERROR-KEYs.
    Public Function GenerateUserPasskeys(userId As Integer) As String()
        Try
            Const byteLength As Integer = 6 ' 6 bytes -> 12 hex chars
            Const maxAttempts As Integer = 200

            Dim passkeys(2) As String
            Dim used As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

            For i As Integer = 0 To 2
                Dim key As String = Nothing
                Dim localAttempts As Integer = 0

                Do
                    If localAttempts >= maxAttempts Then
                        Throw New Exception("Unable to generate a unique passkey after many attempts.")
                    End If
                    localAttempts += 1

                    Dim token = GenerateHexToken(byteLength) ' 12 chars
                    key = token.ToUpperInvariant()

                    ' If DB check fails it returns True (see IsPasskeyUnique) so loop will still progress.
                Loop While used.Contains(key) OrElse Not IsPasskeyUnique(key)

                used.Add(key)
                passkeys(i) = key
            Next

            ' Try to persist passkeys; don't fail creation if DB update fails — log and return keys.
            Try
                Dim updateQuery As String = "UPDATE Users SET Passkey1 = @p1, Passkey2 = @p2, Passkey3 = @p3 WHERE UserID = @UserID"
                Dim sqlParams() As SqlParameter = {
                    New SqlParameter("@p1", passkeys(0)),
                    New SqlParameter("@p2", passkeys(1)),
                    New SqlParameter("@p3", passkeys(2)),
                    New SqlParameter("@UserID", userId)
                }
                Utilities.ExecuteNonQuery(updateQuery, sqlParams)
            Catch ex As Exception
                ' Persist failed: log but return the generated keys so caller can show them.
                Console.WriteLine($"Unable to persist passkeys for user {userId}: {ex.Message}")
            End Try

            Console.WriteLine($"Generated passkeys for user {userId}: {String.Join(", ", passkeys)}")
            Return passkeys
        Catch ex As Exception
            Console.WriteLine($"Error generating passkeys: {ex.Message}")
            ' Return clear fallback so caller can detect an unexpected failure easily.
            Return New String() {"ERROR-KEY1", "ERROR-KEY2", "ERROR-KEY3"}
        End Try
    End Function

    Public Function GenerateUserCode() As String
        ' Produces a longer, unique user code containing a zero-padded user number and timestamp.
        Try
            Dim countQuery As String = "SELECT COUNT(*) FROM Users"
            Dim userCount As Integer = CInt(Utilities.ExecuteScalar(countQuery))

            ' Format: User-0004-20260228123045234 (padded number + timestamp with milliseconds)
            Return $"User-{(userCount + 1):D4}-{DateTime.Now:yyyyMMddHHmmssfff}"
        Catch ex As Exception
            Return $"User-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}-{DateTime.Now:yyyyMMddHHmmssfff}"
        End Try
    End Function


    Private Sub SaveStaffPhoto(userId As Integer)
        Try
            ' Save photo directly to the Photo field in Users table (matching your schema)
            Dim updatePhotoQuery As String = "UPDATE Users SET Photo = @PhotoData WHERE UserID = @UserID"

            Dim photoParams() As SqlParameter = {
                New SqlParameter("@UserID", userId),
                New SqlParameter("@PhotoData", staffPhotoBytes)
            }

            Utilities.ExecuteNonQuery(updatePhotoQuery, photoParams)
            Console.WriteLine($"Staff photo saved for user ID: {userId}")

        Catch ex As Exception
            Console.WriteLine($"Error saving staff photo: {ex.Message}")
            ' Don't throw exception here as the staff member was already created
        End Try
    End Sub



    Private Sub ResetForm()
        ' Clear all text fields
        txtUsername.Clear()
        txtPassword.Clear()
        txtEmail.Clear()
        txtPhone.Clear()
        txtPin.Clear()

        ' Reset combo box
        cmbRole.SelectedIndex = 0

        ' Reset image
        SetDefaultImage()
        staffPhotoBytes = Nothing
        originalImagePath = ""
        lblStaffPicture.Text = "Upload"

        ' Reset button
        btnAddStock.Text = "Add Staff"
        btnAddStock.Enabled = False

        ' Hide status controls when not in edit mode
        Try
            lblStatus.Visible = False
            Guna2ComboBox1.Visible = False
        Catch
        End Try

        ' Focus on first field
        txtUsername.Focus()
    End Sub

    Private Sub CancelAddStaff(sender As Object, e As EventArgs)
        Dim result As DialogResult = MessageBox.Show("Are you sure you want to cancel? All entered data will be lost.",
                                                   "Confirm Cancel",
                                                   MessageBoxButtons.YesNo,
                                                   MessageBoxIcon.Question)
        If result = DialogResult.Yes Then
            Me.Close()
        End If
    End Sub

    Private Sub txtPassword_TextChanged(sender As Object, e As EventArgs) Handles txtPassword.TextChanged
        ValidateForm()
    End Sub

    Private Sub txtUsername_TextChanged(sender As Object, e As EventArgs) Handles txtUsername.TextChanged
        ValidateForm()
    End Sub

    Private Sub txtEmail_TextChanged(sender As Object, e As EventArgs) Handles txtEmail.TextChanged
        ValidateForm()
    End Sub

    Private Sub txtPhone_TextChanged(sender As Object, e As EventArgs) Handles txtPhone.TextChanged
        ValidateForm()
    End Sub

    Private Sub txtPin_TextChanged(sender As Object, e As EventArgs) Handles txtPin.TextChanged
        ValidateForm()
    End Sub

    Private Sub cmbRole_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbRole.SelectedIndexChanged
        ValidateForm()
    End Sub

    Private Sub Guna2HtmlLabel15_Click(sender As Object, e As EventArgs) Handles Guna2HtmlLabel15.Click
        ' Optional: Add help or information about username requirements
    End Sub

    ' Public method to set edit mode
    ' Replace the existing SetEditMode method with this updated edit-mode behavior.
    Public Sub SetEditMode(userData As Dictionary(Of String, Object))
        Try
            Console.WriteLine("SetEditMode called - entering edit view (validation disabled)")

            ' Enable edit mode flag so other code can adapt.
            isEditMode = True
            editingUserData = userData
            editingStaffId = CInt(userData("UserID"))

            ' Update form title and button text for editing
            Me.Text = "Edit Staff Member"
            btnAddStock.Text = "Update Staff"

            ' Populate visible fields with existing data
            PopulateFormWithUserData(userData)

            ' In edit mode we only allow viewing/updating a subset:
            ' - Everything read-only except status (status editable only to set to Inactive)
            txtUsername.ReadOnly = True
            txtPassword.ReadOnly = True
            txtPassword.Text = String.Empty
            txtPin.ReadOnly = True
            txtPin.Text = String.Empty
            txtPin.PasswordChar = "*"c
            txtPin.Enabled = False ' ensure no editing
            txtEmail.ReadOnly = True
            txtPhone.ReadOnly = True
            cmbRole.Enabled = False

            ' Mask password and pin fields (do not show real values)
            Try
                txtPassword.PlaceholderText = "******"
                txtPin.PlaceholderText = "****"
            Catch
                ' Older WinForms targets may not support PlaceholderText; ignore.
            End Try

            ' Disable image upload in edit view (view-only photo)
            Try
                RemoveHandler ProductImage.Click, AddressOf ProductImage_Click
                RemoveHandler lblStaffPicture.Click, AddressOf ProductImage_Click
                lblStaffPicture.Text = "View Only"
            Catch
            End Try

            ' Show status controls and configure them
            Try
                lblStatus.Visible = True
                Guna2ComboBox1.Visible = True

                ' Build status list and set original status
                Dim currentIsActive As Boolean = True
                If userData.ContainsKey("IsActive") Then
                    Try
                        currentIsActive = Convert.ToBoolean(userData("IsActive"))
                    Catch
                        currentIsActive = True
                    End Try
                End If
                originalIsActive = currentIsActive

                Guna2ComboBox1.Items.Clear()
                Guna2ComboBox1.Items.AddRange(New String() {"Active", "Inactive"})
                Guna2ComboBox1.DropDownStyle = ComboBoxStyle.DropDownList
                Guna2ComboBox1.SelectedItem = If(currentIsActive, "Active", "Inactive")

                ' If user is currently active: allow changing to Inactive (but prevent re-activating)
                ' If user is currently inactive: do not allow editing status here.
                Guna2ComboBox1.Enabled = currentIsActive

                ' Attach handler to control allowed transitions
                RemoveHandler Guna2ComboBox1.SelectedIndexChanged, AddressOf Guna2ComboBox1_SelectedIndexChanged
                AddHandler Guna2ComboBox1.SelectedIndexChanged, AddressOf Guna2ComboBox1_SelectedIndexChanged
            Catch ex As Exception
                Console.WriteLine($"Error configuring status control: {ex.Message}")
            End Try

            ' Enable update button (validation is relaxed in edit mode)
            btnAddStock.Enabled = True

            ' Clear any validation message shown on top-right label
            Try
                Guna2HtmlLabel15.Text = String.Empty
            Catch
            End Try

            Console.WriteLine("Form set to edit view successfully")

        Catch ex As Exception
            Console.WriteLine($"Error in SetEditMode: {ex.Message}")
            MessageBox.Show($"Unable to open staff in edit mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub Guna2ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            If Not isEditMode Then Return

            If Guna2ComboBox1.SelectedItem Is Nothing Then Return

            Dim selected As String = Guna2ComboBox1.SelectedItem.ToString()
            ' If original was active and user selected Inactive -> allow change then lock control to prevent re-activation
            If originalIsActive AndAlso selected.Equals("Inactive", StringComparison.OrdinalIgnoreCase) Then
                ' allowed: user changed to Inactive; prevent further edits to avoid re-activation here
                Guna2ComboBox1.Enabled = False
                originalIsActive = False
            ElseIf Not originalIsActive AndAlso selected.Equals("Active", StringComparison.OrdinalIgnoreCase) Then
                ' Not allowed to reactivate in this edit view; revert and inform user
                Try
                    Guna2ComboBox1.SelectedItem = "Inactive"
                Catch
                End Try
                MessageBox.Show("This edit view only allows changing status to Inactive. Reactivation must be done via the System or Admin panel.", "Status Edit Restricted", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            Console.WriteLine($"Status change handler error: {ex.Message}")
        End Try
    End Sub

    ' Replace the existing PopulateFormWithUserData method so it only fills username, email, phone and photo for edit-view.
    Private Sub PopulateFormWithUserData(userData As Dictionary(Of String, Object))
        Try
            ' Fill the form fields with existing data for viewing/editing subset
            txtUsername.Text = If(userData.ContainsKey("Username"), userData("Username").ToString(), String.Empty)

            ' Do not populate password for security
            txtPassword.Text = String.Empty

            ' Basic fields: email and phone (editable in edit mode)
            If userData.ContainsKey("Email") Then txtEmail.Text = If(userData("Email") IsNot Nothing, userData("Email").ToString(), String.Empty)
            If userData.ContainsKey("Phone") Then txtPhone.Text = If(userData("Phone") IsNot Nothing, userData("Phone").ToString(), String.Empty)

            ' Try set role display if available
            If userData.ContainsKey("UserRole") Then
                Dim userRole As String = If(userData("UserRole") IsNot Nothing, userData("UserRole").ToString(), "Staff")
                For i As Integer = 0 To cmbRole.Items.Count - 1
                    If cmbRole.Items(i).ToString().Equals(userRole, StringComparison.OrdinalIgnoreCase) Then
                        cmbRole.SelectedIndex = i
                        Exit For
                    End If
                Next
            End If

            ' Load photo if provided in dictionary or fetch from DB
            If userData.ContainsKey("Photo") AndAlso userData("Photo") IsNot Nothing Then
                Dim bytes = TryCast(userData("Photo"), Byte())
                If bytes IsNot Nothing AndAlso bytes.Length > 0 Then
                    Using ms As New MemoryStream(bytes)
                        If ProductImage.Image IsNot Nothing Then
                            ProductImage.Image.Dispose()
                        End If
                        ProductImage.Image = Image.FromStream(ms)
                        ' Use scaled rendering consistent with display code
                        ProductImage.SizeMode = PictureBoxSizeMode.Normal
                    End Using
                    staffPhotoBytes = bytes
                    lblStaffPicture.Text = "Photo Loaded"
                Else
                    SetDefaultImage()
                End If
            ElseIf userData.ContainsKey("UserID") Then
                ' Defensive DB fetch for photo only
                Try
                    Dim query As String = "SELECT Photo FROM Users WHERE UserID = @UserID"
                    Dim parameters() As SqlParameter = {New SqlParameter("@UserID", CInt(userData("UserID")))}
                    Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                        If reader.Read() AndAlso Not IsDBNull(reader("Photo")) Then
                            Dim dbBytes = CType(reader("Photo"), Byte())
                            Using ms As New MemoryStream(dbBytes)
                                If ProductImage.Image IsNot Nothing Then
                                    ProductImage.Image.Dispose()
                                End If
                                ProductImage.Image = Image.FromStream(ms)
                                ProductImage.SizeMode = PictureBoxSizeMode.Normal
                            End Using
                            staffPhotoBytes = dbBytes
                            lblStaffPicture.Text = "Photo Loaded"
                        Else
                            SetDefaultImage()
                        End If
                    End Using
                Catch ex As Exception
                    Console.WriteLine($"Error fetching photo in PopulateFormWithUserData: {ex.Message}")
                    SetDefaultImage()
                End Try
            Else
                SetDefaultImage()
            End If

            ' Re-validate form (but in edit mode validation is relaxed by ValidateForm)
            ValidateForm()

        Catch ex As Exception
            MessageBox.Show($"Error populating form data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    ' Separate method to handle photo loading in edit mode
    ' Replace LoadExistingPhoto contents (the portion that creates display image) with this to render stored photos stretched to control.
    Private Sub LoadExistingPhoto(reader As SqlDataReader)
        Try
            If Not IsDBNull(reader("Photo")) Then
                Dim photoBytes As Byte() = CType(reader("Photo"), Byte())

                If photoBytes IsNot Nothing AndAlso photoBytes.Length > 0 Then
                    staffPhotoBytes = photoBytes

                    Using ms As New IO.MemoryStream(photoBytes)
                        ms.Seek(0, SeekOrigin.Begin)
                        Using originalImage As Image = Image.FromStream(ms, True, False)
                            ' Create scaled display image sized to the PictureBox
                            Dim displayW As Integer = Math.Max(1, ProductImage.Width)
                            Dim displayH As Integer = Math.Max(1, ProductImage.Height)
                            Dim displayImage As New Bitmap(displayW, displayH)

                            Using g As Graphics = Graphics.FromImage(displayImage)
                                g.Clear(Color.Transparent)
                                g.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                                g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.HighQuality
                                g.DrawImage(originalImage, New Rectangle(0, 0, displayW, displayH))
                            End Using

                            If ProductImage.Image IsNot Nothing Then
                                ProductImage.Image.Dispose()
                            End If

                            ProductImage.Image = displayImage
                            ProductImage.SizeMode = PictureBoxSizeMode.Normal
                            lblStaffPicture.Text = "Photo Loaded"
                        End Using
                    End Using
                Else
                    SetDefaultImage()
                    lblStaffPicture.Text = "Upload"
                End If
            Else
                SetDefaultImage()
                lblStaffPicture.Text = "Upload"
            End If
        Catch ex As Exception
            Console.WriteLine($"Error loading existing photo: {ex.Message}")
            SetDefaultImage()
            lblStaffPicture.Text = "Upload"
        End Try
    End Sub
    ' Method to refresh image display - can be called after form is shown
    ' Replace RefreshImageDisplay with this to use the same scaled rendering (prevents perceived zoom)
    Public Sub RefreshImageDisplay()
        Try
            If isEditMode AndAlso staffPhotoBytes IsNot Nothing AndAlso staffPhotoBytes.Length > 0 Then
                Using ms As New IO.MemoryStream(staffPhotoBytes)
                    ms.Seek(0, SeekOrigin.Begin)
                    Using originalImage As Image = Image.FromStream(ms, True, False)
                        Dim displayW As Integer = Math.Max(1, ProductImage.Width)
                        Dim displayH As Integer = Math.Max(1, ProductImage.Height)
                        Dim displayImage As New Bitmap(displayW, displayH)

                        Using g As Graphics = Graphics.FromImage(displayImage)
                            g.Clear(Color.Transparent)
                            g.InterpolationMode = Drawing.Drawing2D.InterpolationMode.HighQualityBicubic
                            g.SmoothingMode = Drawing.Drawing2D.SmoothingMode.HighQuality
                            g.DrawImage(originalImage, New Rectangle(0, 0, displayW, displayH))
                        End Using

                        If ProductImage.Image IsNot Nothing Then
                            ProductImage.Image.Dispose()
                        End If

                        ProductImage.Image = displayImage
                        ProductImage.SizeMode = PictureBoxSizeMode.Normal
                        ProductImage.Refresh()

                        lblStaffPicture.Text = "Photo Loaded"
                    End Using
                End Using
            End If
        Catch ex As Exception
            Console.WriteLine($"Error refreshing image display: {ex.Message}")
        End Try
    End Sub
End Class