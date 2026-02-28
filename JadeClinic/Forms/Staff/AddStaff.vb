Imports Microsoft.Data.SqlClient
Imports System.Drawing
Imports System.IO

Public Class AddStaff
    Private staffPhotoBytes As Byte() = Nothing
    Private isEditMode As Boolean = False
    Private editingStaffId As Integer = 0
    Private originalImagePath As String = ""
    Private editingUserData As Dictionary(Of String, Object) = Nothing

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

    Private Sub ProcessAndCompressImage(imagePath As String)
        Try
            ' Show processing indicator
            lblStaffPicture.Text = "Processing..."
            Application.DoEvents()

            ' Load and display original image
            Using originalImage As Image = Image.FromFile(imagePath)
                ' Display in PictureBox
                ProductImage.Image = New Bitmap(originalImage)
                originalImagePath = imagePath

                ' Compress image for database storage
                ' Use higher compression for database (quality 70, max 400x400)
                staffPhotoBytes = ImageCompression.CompressImage(originalImage, 70, 400, 400)

                ' Show compression results
                Dim originalSize As Long = New FileInfo(imagePath).Length
                Dim compressedSize As Long = staffPhotoBytes.Length
                Dim compressionRatio As Double = (1 - (CDbl(compressedSize) / originalSize)) * 100

                lblStaffPicture.Text = $"Optimized ({ImageCompression.FormatFileSize(compressedSize)}, " &
                                     $"{compressionRatio:F0}% smaller)"

                Console.WriteLine($"Image compressed: {ImageCompression.FormatFileSize(originalSize)} → " &
                                $"{ImageCompression.FormatFileSize(compressedSize)} " &
                                $"({compressionRatio:F1}% reduction)")
            End Using

            ' Validate form after image upload
            ValidateForm()

        Catch ex As Exception
            lblStaffPicture.Text = "Upload"
            MessageBox.Show($"Error processing image: {ex.Message}", "Processing Error",
                          MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ValidateForm()
        Try
            Dim isValid As Boolean = True
            Dim errorMessage As String = ""

            ' Validate required fields
            If String.IsNullOrWhiteSpace(txtUsername.Text) Then
                isValid = False
                errorMessage = "Username is required."
            ElseIf txtUsername.Text.Length < 3 Then
                isValid = False
                errorMessage = "Username must be at least 3 characters."
            ElseIf String.IsNullOrWhiteSpace(txtPassword.Text) AndAlso Not isEditMode Then
                isValid = False
                errorMessage = "Password is required."
            ElseIf Not String.IsNullOrWhiteSpace(txtPassword.Text) AndAlso txtPassword.Text.Length < 6 Then
                isValid = False
                errorMessage = "Password must be at least 6 characters."
            ElseIf String.IsNullOrWhiteSpace(txtEmail.Text) Then
                isValid = False
                errorMessage = "Email is required."
            ElseIf Not IsValidEmailFormat(txtEmail.Text) Then
                isValid = False
                errorMessage = "Email must be a valid @gmail.com address."
            ElseIf String.IsNullOrWhiteSpace(txtPhone.Text) Then
                isValid = False
                errorMessage = "Phone number is required."
            ElseIf Not IsValidPhoneFormat(txtPhone.Text) Then
                isValid = False
                errorMessage = "Phone must be 11 digits starting with 09."
            ElseIf String.IsNullOrWhiteSpace(txtPin.Text) Then
                isValid = False
                errorMessage = "PIN is required."
            ElseIf Not IsValidPinFormat(txtPin.Text) Then
                isValid = False
                errorMessage = "PIN must be exactly 4 numbers."
            ElseIf cmbRole.SelectedIndex = -1 Then
                isValid = False
                errorMessage = "Please select a role."
            End If

            ' Check for duplicate username (only for new users or if username changed in edit mode)
            If isValid AndAlso Not String.IsNullOrWhiteSpace(txtUsername.Text) Then
                If Not isEditMode AndAlso CheckDuplicateUsername(txtUsername.Text.Trim()) Then
                    isValid = False
                    errorMessage = "Username already exists. Please choose a different username."
                End If
            End If

            ' Enable/disable save button
            btnAddStock.Enabled = isValid

            ' Show error message if validation fails
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

        ' Reset form or close
        ResetForm()
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

        If Not String.IsNullOrWhiteSpace(txtPassword.Text) Then
            ' Update with new password
            Dim hashedPassword As String = frmLoginvb.HashPassword(txtPassword.Text.Trim())
            updateQuery = "UPDATE Users SET Email = @Email, Phone = @Phone, UserRole = @UserRole, pin = @Pin, PasswordHash = @PasswordHash, UpdatedAt = @UpdatedAt WHERE UserID = @UserID"
            parameters.AddRange({
                New SqlParameter("@Email", email),
                New SqlParameter("@Phone", phone),
                New SqlParameter("@UserRole", role),
                New SqlParameter("@Pin", Convert.ToInt32(pin)),
                New SqlParameter("@PasswordHash", hashedPassword),
                New SqlParameter("@UpdatedAt", DateTime.Now),
                New SqlParameter("@UserID", editingStaffId)
            })
        Else
            ' Update without changing password
            updateQuery = "UPDATE Users SET Email = @Email, Phone = @Phone, UserRole = @UserRole, pin = @Pin, UpdatedAt = @UpdatedAt WHERE UserID = @UserID"
            parameters.AddRange({
                New SqlParameter("@Email", email),
                New SqlParameter("@Phone", phone),
                New SqlParameter("@UserRole", role),
                New SqlParameter("@Pin", Convert.ToInt32(pin)),
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

    Private Function GenerateUserPasskeys(userId As Integer) As String()
        Try
            ' Generate 3 random passkeys for forgot password functionality (6 letters each)
            Dim passkeys As String() = DatabaseInitializer.GenerateRandomPasskeys(3)

            ' Update the Users table with the passkeys directly
            Dim updatePasskeysQuery As String = "UPDATE Users SET Passkey1 = @Passkey1, Passkey2 = @Passkey2, Passkey3 = @Passkey3 WHERE UserID = @UserID"

            Dim passkeyParams() As SqlParameter = {
                New SqlParameter("@UserID", userId),
                New SqlParameter("@Passkey1", passkeys(0)),
                New SqlParameter("@Passkey2", passkeys(1)),
                New SqlParameter("@Passkey3", passkeys(2))
            }

            Utilities.ExecuteNonQuery(updatePasskeysQuery, passkeyParams)

            Console.WriteLine($"Generated passkeys for user {userId}: {String.Join(", ", passkeys)}")
            Return passkeys

        Catch ex As Exception
            Console.WriteLine($"Error generating passkeys: {ex.Message}")
            ' Return empty array if passkey generation fails (staff member is still created)
            Return New String() {"ERROR1", "ERROR2", "ERROR3"}
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

    Private Function GenerateUserCode() As String
        Try
            ' Get the next user number
            Dim countQuery As String = "SELECT COUNT(*) FROM Users"
            Dim userCount As Integer = CInt(Utilities.ExecuteScalar(countQuery))

            ' Generate QR code in format User-XXXXX
            Return $"User-{(userCount + 1):D5}"
        Catch ex As Exception
            ' Fallback to GUID-based code
            Return $"User-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}"
        End Try
    End Function

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
    Public Sub SetEditMode(userData As Dictionary(Of String, Object))
        Try
            Console.WriteLine("SetEditMode called - entering read-only view (editing disabled)")

            ' Do NOT flip isEditMode to true — editing is intentionally disabled.
            editingUserData = userData
            editingStaffId = CInt(userData("UserID"))

            ' Update form title to reflect view-only mode
            Me.Text = "View Staff Member (Edit Disabled)"
            btnAddStock.Text = "Edit Disabled"
            btnAddStock.Enabled = False

            ' Pre-fill the form with existing data and then make controls read-only/disabled
            PopulateFormWithUserData(userData)

            ' Make fields read-only / controls inactive
            txtUsername.ReadOnly = True
            txtPassword.ReadOnly = True
            txtPassword.PlaceholderText = "Password editing is disabled"
            txtEmail.ReadOnly = True
            txtPhone.ReadOnly = True
            txtPin.ReadOnly = True
            cmbRole.Enabled = False

            ' Disable image upload
            RemoveHandler ProductImage.Click, AddressOf ProductImage_Click
            RemoveHandler lblStaffPicture.Click, AddressOf ProductImage_Click
            lblStaffPicture.Text = "View Only"

            ' Prevent accidental save
            btnAddStock.Enabled = False

            Console.WriteLine("Form set to read-only view successfully")

        Catch ex As Exception
            Console.WriteLine($"Error in SetEditMode (read-only): {ex.Message}")
            MessageBox.Show($"Unable to open staff in read-only mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub PopulateFormWithUserData(userData As Dictionary(Of String, Object))
        Try
            ' Fill the form fields with existing data for viewing
            txtUsername.Text = userData("Username").ToString()
            txtUsername.ReadOnly = True ' Always readonly when viewing existing user

            ' Keep password empty for security
            txtPassword.Text = ""
            txtPassword.PlaceholderText = "Password cannot be viewed"

            ' Fetch remaining user details from DB and display
            Dim query As String = "SELECT Email, Phone, FullName, UserRole, PIN, Photo FROM Users WHERE UserID = @UserID"
            Dim parameters() As SqlParameter = {
                New SqlParameter("@UserID", editingStaffId)
            }

            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                If reader.Read() Then
                    txtEmail.Text = If(IsDBNull(reader("Email")), "", reader("Email").ToString())
                    txtPhone.Text = If(IsDBNull(reader("Phone")), "", reader("Phone").ToString())
                    txtPin.Text = If(IsDBNull(reader("PIN")), "", reader("PIN").ToString())

                    ' Set role display (combo disabled in view mode)
                    Dim userRole As String = If(IsDBNull(reader("UserRole")), "Staff", reader("UserRole").ToString())
                    For i As Integer = 0 To cmbRole.Items.Count - 1
                        If cmbRole.Items(i).ToString().Equals(userRole, StringComparison.OrdinalIgnoreCase) Then
                            cmbRole.SelectedIndex = i
                            Exit For
                        End If
                    Next

                    ' Load existing photo for viewing
                    LoadExistingPhoto(reader)
                End If
            End Using

            ' After populating, ensure controls are not editable
            txtEmail.ReadOnly = True
            txtPhone.ReadOnly = True
            txtPin.ReadOnly = True
            cmbRole.Enabled = False

            ' Re-validate form (keeps save disabled)
            ValidateForm()
        Catch ex As Exception
            MessageBox.Show($"Error populating form data: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    ' Separate method to handle photo loading in edit mode
    Private Sub LoadExistingPhoto(reader As SqlDataReader)
        Try
            If Not IsDBNull(reader("Photo")) Then
                Dim photoBytes As Byte() = CType(reader("Photo"), Byte())

                ' Validate that we have actual photo data
                If photoBytes IsNot Nothing AndAlso photoBytes.Length > 0 Then
                    Console.WriteLine($"Loading existing photo, size: {photoBytes.Length} bytes")

                    ' Store the photo bytes for potential updates
                    staffPhotoBytes = photoBytes

                    ' Load the image using a more robust method
                    Using ms As New IO.MemoryStream(photoBytes)
                        ms.Seek(0, SeekOrigin.Begin) ' Ensure we're at the beginning

                        ' Create a completely independent copy of the image
                        Dim originalImage As Image = Image.FromStream(ms, True, False)

                        ' Create a new bitmap from the original
                        Dim displayImage As New Bitmap(originalImage.Width, originalImage.Height)
                        Using g As Graphics = Graphics.FromImage(displayImage)
                            g.DrawImage(originalImage, 0, 0)
                        End Using

                        ' Dispose the original image
                        originalImage.Dispose()

                        ' Set the image to the PictureBox
                        If ProductImage.Image IsNot Nothing Then
                            ProductImage.Image.Dispose()
                        End If

                        ProductImage.Image = displayImage
                        ProductImage.SizeMode = PictureBoxSizeMode.Zoom

                        ' Update label
                        lblStaffPicture.Text = "Photo Loaded"

                        Console.WriteLine("Photo loaded successfully in edit mode")
                    End Using
                Else
                    Console.WriteLine("Photo data is empty or null")
                    SetDefaultImage()
                    lblStaffPicture.Text = "Upload"
                End If
            Else
                Console.WriteLine("No photo found in database")
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
    Public Sub RefreshImageDisplay()
        Try
            If isEditMode AndAlso staffPhotoBytes IsNot Nothing AndAlso staffPhotoBytes.Length > 0 Then
                Console.WriteLine("Refreshing image display in edit mode")

                ' Reload the image from stored bytes
                Using ms As New IO.MemoryStream(staffPhotoBytes)
                    ms.Seek(0, SeekOrigin.Begin)

                    Dim originalImage As Image = Image.FromStream(ms, True, False)
                    Dim displayImage As New Bitmap(originalImage.Width, originalImage.Height)

                    Using g As Graphics = Graphics.FromImage(displayImage)
                        g.DrawImage(originalImage, 0, 0)
                    End Using

                    originalImage.Dispose()

                    If ProductImage.Image IsNot Nothing Then
                        ProductImage.Image.Dispose()
                    End If

                    ProductImage.Image = displayImage
                    ProductImage.SizeMode = PictureBoxSizeMode.Zoom
                    ProductImage.Refresh()

                    lblStaffPicture.Text = "Photo Loaded"
                    Console.WriteLine("Image refreshed successfully")
                End Using
            End If
        Catch ex As Exception
            Console.WriteLine($"Error refreshing image display: {ex.Message}")
        End Try
    End Sub
End Class