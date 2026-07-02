Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Security.Cryptography
Imports System.Text
Imports Guna.UI2.WinForms
Imports Microsoft.Data.SqlClient

Public Class AddStaff
    Private staffPhotoBytes As Byte() = Nothing
    Private isEditMode As Boolean = False
    Private editingStaffId As Integer = 0
    Private originalImagePath As String = ""
    Private editingUserData As Dictionary(Of String, Object) = Nothing
    Private originalIsActive As Boolean = True ' track original status for edit-mode behavior
    ' Dental Clinic Color Palette Constants
    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)      ' #FECF10 - Primary brand color
    Private ReadOnly RichOlive As Color = Color.FromArgb(190, 154, 48)         ' #BE9A30 - Secondary accent
    Private ReadOnly DeepCharcoal As Color = Color.FromArgb(51, 51, 51)        ' #333333 - Dark text
    Private ReadOnly DarkSlate As Color = Color.White                          ' White - Card background
    Private ReadOnly Graphite As Color = Color.FromArgb(61, 65, 69)            ' #3D4145 - Card background (unused)
    Private ReadOnly SteelGray As Color = Color.FromArgb(254, 191, 16)         ' #FECF10 - GoldenYellow
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)        ' #FFFFFF - White
    Private ReadOnly LightSilver As Color = Color.FromArgb(102, 102, 102)      ' #666666 - Medium text
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)       ' #10D862 - Success states
    Private ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)
    Private Sub AddStaff_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        SetupForm()
        SetDefaultImage()
        InitializeRoleComboBox()
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me)
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

        ' Role combo - populate if needed
        Try
            If cmbRole.Items.Count = 0 Then
                cmbRole.Items.AddRange(New String() {"Staff", "Admin", "Manager"})
            End If
        Catch
        End Try

        ' Only set default role when not editing
        If Not isEditMode Then
            Try
                If cmbRole.Items.Count > 0 Then cmbRole.SelectedIndex = 0 ' Default to "Staff"
            Catch
            End Try
        End If

        ' Configure PIN field to only accept 4 numbers
        txtPin.MaxLength = 4
        Try
            If Not isEditMode Then
                txtPin.PlaceholderText = "Enter 4-digit PIN"
                AddHandler txtPin.KeyPress, AddressOf txtPin_KeyPress ' Only allow numbers
            Else
                ' In edit mode keep PIN masked / non-editable (SetEditMode will enforce)
                txtPin.PlaceholderText = "****"
            End If
        Catch
        End Try

        ' Configure phone field
        txtPhone.MaxLength = 11
        Try
            If Not isEditMode Then txtPhone.PlaceholderText = "09xxxxxxxxx"
        Catch
        End Try

        ' Configure email field
        Try
            If Not isEditMode Then txtEmail.PlaceholderText = "example@gmail.com"
        Catch
        End Try

        ' Configure image click event only for add mode (edit mode disables upload)
        Try
            If Not isEditMode Then
                ' ProductImage: attach handler (ensure not attached multiple times)
                Try
                    RemoveHandler ProductImage.Click, AddressOf ProductImage_Click
                Catch
                End Try
                AddHandler ProductImage.Click, AddressOf ProductImage_Click

                ' Do NOT add a second handler for lblStaffPicture here:
                ' the Designer method `lblStaffPicture_Click(... ) Handles lblStaffPicture.Click`
                ' already handles the label click. Adding another handler caused the dialog to open twice.
            Else
                ' Ensure upload handlers are not attached in edit mode
                Try
                    RemoveHandler ProductImage.Click, AddressOf ProductImage_Click
                Catch
                End Try
            End If
        Catch
        End Try

        ' Add validation events (safe to attach once during form load)
        Try
            AddHandler txtUsername.TextChanged, AddressOf ValidateForm
            AddHandler txtPassword.TextChanged, AddressOf ValidateForm
            AddHandler txtEmail.TextChanged, AddressOf ValidateForm
            AddHandler txtPhone.TextChanged, AddressOf ValidateForm
            AddHandler txtPin.TextChanged, AddressOf ValidateForm
        Catch
        End Try

        ' Button and title - reflect current mode
        If isEditMode Then
            btnAddStock.Enabled = True
            btnAddStock.Text = "Update Staff"
            Try
                Guna2HtmlLabel6.Text = "Edit Staff Member"
            Catch
            End Try
        Else
            btnAddStock.Enabled = False
            btnAddStock.Text = "Add Staff"
            Try
                Guna2HtmlLabel6.Text = "Add Staff"
            Catch
            End Try
        End If

        ' Set up cancel functionality
        Try
            AddHandler Guna2HtmlLabel1.Click, AddressOf CancelAddStaff
        Catch
        End Try

        ' Position the validation/message label to top-right as requested
        Try
            Guna2HtmlLabel15.Location = New Point(180, 21)
            Guna2HtmlLabel15.Font = New Font("Poppins", 9.0F, FontStyle.Regular)
            Guna2HtmlLabel15.BringToFront()
        Catch
            ' Ignore if control not present or designer-managed
        End Try

        ' Show or hide status controls depending on mode
        Try
            If isEditMode Then
                lblStatus.Visible = True
                Guna2ComboBox1.Visible = True
            Else
                lblStatus.Visible = False
                Guna2ComboBox1.Visible = False
            End If
        Catch
        End Try
    End Sub    ' Event handler to only allow numeric input for PIN
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

            ' Reset photo bytes if this is called during add-mode reset
            If Not isEditMode Then
                staffPhotoBytes = Nothing
                ' label instructs user in add mode
                Try
                    lblStaffPicture.Text = "Click to Upload"
                Catch
                End Try
            Else
                ' In edit mode, if there is no photo show a neutral label (but SetEditMode may override)
                Try
                    If staffPhotoBytes Is Nothing OrElse staffPhotoBytes.Length = 0 Then
                        lblStaffPicture.Text = "No Photo"
                    Else
                        lblStaffPicture.Text = "Photo Loaded"
                    End If
                Catch
                End Try
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
            ' Prevent any image selection when the form is in edit mode.
            If isEditMode Then
                ' If the currently opened account is the logged-in user, show a specific restriction message.
                If editingStaffId = frmLoginvb.LoggedInUserID Then
                    Try
                        Guna2HtmlLabel15.Text = "You cannot edit your own account here."
                    Catch
                    End Try

                    MessageBox.Show("You cannot edit your own account information from this dialog.", "Action Restricted", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Else
                    ' For other users in edit mode still disallow changing the image from this dialog.
                    Try
                        Guna2HtmlLabel15.Text = "Image cannot be changed in edit mode."
                    Catch
                    End Try

                    MessageBox.Show("You cannot change the staff photo while viewing/editing an existing account here.", "Read‑Only", MessageBoxButtons.OK, MessageBoxIcon.Information)
                End If

                ' Ensure placeholder shows "No Photo" when no photo exists (match add-mode messaging style).
                If staffPhotoBytes Is Nothing OrElse staffPhotoBytes.Length = 0 Then
                    Try
                        SetDefaultImage()
                        lblStaffPicture.Text = "No Photo"
                    Catch
                    End Try
                End If

                Return
            End If

            ' Normal add-mode behaviour: allow selecting an image.
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
        Dim fullName As String = username ' Keep simple: username used as full name

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

        ' -- Build and show the "New Staff Created" modal that displays user info and passkeys and offers printing --
        Dim dlg As New Form With {
        .Text = "New Staff Created",
        .Size = New Size(560, 520),
        .StartPosition = FormStartPosition.CenterParent,
        .FormBorderStyle = FormBorderStyle.FixedDialog,
        .MaximizeBox = False,
        .MinimizeBox = False,
        .BackColor = DarkSlate,
        .ShowInTaskbar = False,
        .KeyPreview = True
    }

        Dim titleLbl As New Label With {
        .Text = "STAFF ACCOUNT CREATED",
        .Font = New Font("Poppins", 14, FontStyle.Bold),
        .ForeColor = DeepCharcoal,
        .AutoSize = False,
        .Size = New Size(520, 30),
        .Location = New Point(20, 12),
        .TextAlign = ContentAlignment.MiddleCenter
    }
        dlg.Controls.Add(titleLbl)

        Dim infoPanel As New Panel With {
        .Location = New Point(20, 52),
        .Size = New Size(520, 260),
        .BackColor = Color.Transparent
    }
        dlg.Controls.Add(infoPanel)

        Dim lblUser As New Label With {
        .Text = $"Username: {username}",
        .Font = New Font("Poppins", 10, FontStyle.Regular),
        .ForeColor = LightSilver,
        .AutoSize = False,
        .Size = New Size(500, 22),
        .Location = New Point(0, 0)
    }
        infoPanel.Controls.Add(lblUser)

        Dim lblRoleLocal As New Label With {
        .Text = $"Role: {role}",
        .Font = New Font("Poppins", 10, FontStyle.Regular),
        .ForeColor = LightSilver,
        .AutoSize = False,
        .Size = New Size(500, 22),
        .Location = New Point(0, 28)
    }
        infoPanel.Controls.Add(lblRoleLocal)

        Dim lblQr As New Label With {
        .Text = $"QR Code Value: {userCode}",
        .Font = New Font("Poppins", 10, FontStyle.Regular),
        .ForeColor = LightSilver,
        .AutoSize = False,
        .Size = New Size(500, 22),
        .Location = New Point(0, 56)
    }
        infoPanel.Controls.Add(lblQr)

        Dim lblContact As New Label With {
        .Text = $"Email: {email}    Phone: {phone}",
        .Font = New Font("Poppins", 9, FontStyle.Regular),
        .ForeColor = LightSilver,
        .AutoSize = False,
        .Size = New Size(500, 22),
        .Location = New Point(0, 84)
    }
        infoPanel.Controls.Add(lblContact)

        Dim sep As New Panel With {
        .Size = New Size(500, 1),
        .Location = New Point(0, 112),
        .BackColor = Color.FromArgb(200, 200, 200)
    }
        infoPanel.Controls.Add(sep)

        Dim passkeysLbl As New Label With {
        .Text = "Recovery Passkeys (store securely):",
        .Font = New Font("Poppins", 10, FontStyle.Bold),
        .ForeColor = GoldenYellow,
        .AutoSize = False,
        .Size = New Size(500, 22),
        .Location = New Point(0, 130)
    }
        infoPanel.Controls.Add(passkeysLbl)

        Dim passkeysBox As New TextBox With {
        .Multiline = True,
        .ReadOnly = True,
        .ScrollBars = ScrollBars.Vertical,
        .Font = New Font("Consolas", 10, FontStyle.Regular),
        .BackColor = Color.FromArgb(245, 245, 245),
        .ForeColor = DeepCharcoal,
        .Location = New Point(0, 158),
        .Size = New Size(500, 90),
        .Text = String.Join(Environment.NewLine, passkeys)
    }
        infoPanel.Controls.Add(passkeysBox)

        ' Buttons: Print, Copy, Close
        Dim btnPrint As New Guna2Button With {
        .Text = "Print",
        .Size = New Size(120, 40),
        .Location = New Point(140, 330),
        .FillColor = RichOlive,
        .ForeColor = PureWhite,
        .BorderRadius = 10
    }
        dlg.Controls.Add(btnPrint)

        Dim btnCopy As New Guna2Button With {
        .Text = "Copy Passkeys",
        .Size = New Size(140, 40),
        .Location = New Point(270, 330),
        .FillColor = SteelGray,
        .ForeColor = DeepCharcoal,
        .BorderRadius = 10
    }
        dlg.Controls.Add(btnCopy)

        Dim btnClose As New Guna2Button With {
        .Text = "Close",
        .Size = New Size(120, 40),
        .Location = New Point(410, 330),
        .FillColor = AlertRed,
        .ForeColor = PureWhite,
        .BorderRadius = 10
    }
        dlg.Controls.Add(btnClose)

        ' Prepare a PrintDocument that prints the displayed information
        ' Prepare a PrintDocument with improved layout and A5 paper size
        Dim pd As New PrintDocument()
        pd.DefaultPageSettings.Margins = New Margins(40, 40, 40, 40) ' 0.4" margins
        pd.DefaultPageSettings.PaperSize = New PaperSize("A5", 582, 827) ' A5 in hundredths of an inch (approx)

        AddHandler pd.PrintPage, Sub(s, ev)
                                     Try
                                         ev.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias

                                         ' Layout helpers
                                         Dim left As Single = ev.MarginBounds.Left
                                         Dim top As Single = ev.MarginBounds.Top
                                         Dim width As Single = ev.MarginBounds.Width
                                         Dim y As Single = top

                                         ' Header: colored bar with title centered
                                         Using headerBrush As New SolidBrush(GoldenYellow)
                                             ev.Graphics.FillRectangle(headerBrush, left, y, width, 56)
                                         End Using
                                         Using titleFont As New Font("Segoe UI", 14, FontStyle.Bold)
                                             Using titleBrush As New SolidBrush(DeepCharcoal)
                                                 Dim title As String = "New Staff Account"
                                                 Dim titleSize = ev.Graphics.MeasureString(title, titleFont)
                                                 ev.Graphics.DrawString(title, titleFont, titleBrush, left + (width - titleSize.Width) / 2, y + 12)
                                             End Using
                                         End Using
                                         y += 72.0F

                                         ' Sub-header / clinic label
                                         Using subFont As New Font("Segoe UI", 9, FontStyle.Regular)
                                             Using subBrush As New SolidBrush(LightSilver)
                                                 ev.Graphics.DrawString($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}", subFont, subBrush, left, y)
                                             End Using
                                         End Using
                                         y += 18.0F

                                         ' Info block background
                                         Dim infoHeight As Single = 110.0F
                                         Using infoBg As New SolidBrush(Color.FromArgb(245, 245, 245))
                                             ev.Graphics.FillRectangle(infoBg, left, y, width, infoHeight)
                                         End Using
                                         Using borderPen As New Pen(Color.FromArgb(200, 200, 200), 1)
                                             ev.Graphics.DrawRectangle(borderPen, left, y, width, infoHeight)
                                         End Using

                                         ' Draw user details inside info block
                                         Dim labelFont As New Font("Segoe UI", 9, FontStyle.Bold)
                                         Dim valueFont As New Font("Segoe UI", 9, FontStyle.Regular)
                                         Dim padding As Single = 8.0F
                                         Dim col1X As Single = left + padding
                                         Dim col2X As Single = left + width * 0.55F
                                         Dim curY As Single = y + padding

                                         ev.Graphics.DrawString("Username:", labelFont, Brushes.Black, col1X, curY)
                                         ev.Graphics.DrawString(username, valueFont, Brushes.Black, col1X + 80, curY)
                                         ev.Graphics.DrawString("Role:", labelFont, Brushes.Black, col2X, curY)
                                         ev.Graphics.DrawString(role, valueFont, Brushes.Black, col2X + 40, curY)
                                         curY += 20.0F

                                         ev.Graphics.DrawString("Email:", labelFont, Brushes.Black, col1X, curY)
                                         ev.Graphics.DrawString(email, valueFont, Brushes.Black, col1X + 50, curY)
                                         ev.Graphics.DrawString("Phone:", labelFont, Brushes.Black, col2X, curY)
                                         ev.Graphics.DrawString(phone, valueFont, Brushes.Black, col2X + 50, curY)
                                         curY += 20.0F


                                         curY += 20.0F

                                         ev.Graphics.DrawString("Note:", labelFont, Brushes.Black, col1X, curY)
                                         Using noteFont As New Font("Segoe UI", 8, FontStyle.Italic)
                                             ev.Graphics.DrawString("Keep recovery passkeys private. Print/copy only when secure.", noteFont, Brushes.DarkSlateGray, col1X + 40, curY)
                                         End Using

                                         y += infoHeight + 18.0F

                                         ' Passkeys block: light background with monospace font for clarity
                                         Using pkBg As New SolidBrush(Color.FromArgb(250, 250, 250))
                                             ev.Graphics.FillRectangle(pkBg, left, y, width, 22.0F + passkeys.Length * 18.0F)
                                         End Using
                                         Using pkBorder As New Pen(Color.FromArgb(200, 200, 200), 1)
                                             ev.Graphics.DrawRectangle(pkBorder, left, y, width, 22.0F + passkeys.Length * 18.0F)
                                         End Using

                                         Dim pkTitleFont As New Font("Segoe UI", 10, FontStyle.Bold)
                                         ev.Graphics.DrawString("Recovery Passkeys:", pkTitleFont, New SolidBrush(DeepCharcoal), left + padding, y + 6)
                                         Dim pkFont As New Font("Consolas", 9, FontStyle.Regular)
                                         Dim pkStartY As Single = y + 30.0F
                                         For Each pk As String In passkeys
                                             ev.Graphics.DrawString(pk, pkFont, Brushes.Black, left + padding + 4, pkStartY)
                                             pkStartY += 18.0F
                                         Next

                                         y += 22.0F + passkeys.Length * 18.0F + 12.0F

                                         ' Footer: instructions and clinic contact (small)
                                         Using footerFont As New Font("Segoe UI", 8, FontStyle.Regular)
                                             Using footerBrush As New SolidBrush(LightSilver)
                                                 Dim footerText As String = "This printout contains sensitive recovery keys. Store securely and destroy paper copy if not needed."
                                                 ev.Graphics.DrawString(footerText, footerFont, footerBrush, left, ev.MarginBounds.Bottom - 40)
                                             End Using
                                         End Using

                                         ev.HasMorePages = False
                                     Catch ex As Exception
                                         Console.WriteLine($"PrintPage error: {ex.Message}")
                                         ev.HasMorePages = False
                                     End Try
                                 End Sub

        Dim printPreview As New PrintPreviewDialog() With {
        .Document = pd,
        .WindowState = FormWindowState.Maximized,
        .Text = "Print - New Staff"
    }

        ' Button handlers
        AddHandler btnPrint.Click, Sub()
                                       Try
                                           printPreview.ShowDialog(dlg)
                                       Catch ex As Exception
                                           MessageBox.Show($"Print failed: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                                       End Try
                                   End Sub

        AddHandler btnCopy.Click, Sub()
                                      Try
                                          Clipboard.SetText(passkeysBox.Text)
                                          MessageBox.Show("Passkeys copied to clipboard. Keep them secure.", "Copied", MessageBoxButtons.OK, MessageBoxIcon.Information)
                                      Catch
                                          MessageBox.Show("Unable to copy to clipboard on this system.", "Copy Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                                      End Try
                                  End Sub

        AddHandler btnClose.Click, Sub()
                                       dlg.DialogResult = DialogResult.OK
                                       dlg.Close()
                                   End Sub

        AddHandler dlg.KeyDown, Sub(s, ke)
                                    If ke.KeyCode = Keys.Escape Then
                                        btnClose.PerformClick()
                                    End If
                                End Sub

        ' Show the dialog (modal) — do not auto-close the AddStaff form until user closes the print/passkey modal
        dlg.ShowDialog(Me)
        dlg.Dispose()

        ' Close the AddStaff dialog with OK so calling code can refresh staff list
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub
    Private Sub UpdateExistingStaff()
        ' Prepare staff data
        Dim email As String = txtEmail.Text.Trim()
        Dim phone As String = txtPhone.Text.Trim()
        Dim role As String = cmbRole.SelectedItem.ToString()
        Dim username As String = txtUsername.Text.Trim()

        ' Resolve PIN: prefer numeric textbox value; otherwise fall back to original value from editingUserData.
        Dim resolvedPin As Integer
        Dim pinText As String = txtPin.Text.Trim()

        If Integer.TryParse(pinText, resolvedPin) = False Then
            ' try fallback from editingUserData
            resolvedPin = 0 ' default
            If editingUserData IsNot Nothing AndAlso editingUserData.ContainsKey("PIN") Then
                Try
                    resolvedPin = Convert.ToInt32(editingUserData("PIN"))
                Catch ex As Exception
                    Console.WriteLine($"Unable to parse original PIN fallback: {ex.Message}")
                    ' resolvedPin remains 0 (or choose another safe default)
                End Try
            End If
        End If

        ' Build update query - only update password if provided
        Dim updateQuery As String
        Dim parameters As New List(Of SqlParameter)

        ' Determine IsActive from status combobox (default to original if control missing)
        Dim isActiveValue As Boolean = originalIsActive
        Try
            If Guna2ComboBox1 IsNot Nothing AndAlso Guna2ComboBox1.SelectedItem IsNot Nothing Then
                ' Prevent activating or changing status of your own account here
                If editingStaffId = frmLoginvb.LoggedInUserID Then
                    isActiveValue = originalIsActive
                Else
                    isActiveValue = (Guna2ComboBox1.SelectedItem.ToString().Equals("Active", StringComparison.OrdinalIgnoreCase))
                End If
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
            New SqlParameter("@Pin", resolvedPin),
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
            New SqlParameter("@Pin", resolvedPin),
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
    ' Replace your existing GenerateUserPasskeys method with this corrected version:
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

                    ' Check uniqueness using the correct column name 'Passkeys'
                Loop While used.Contains(key) OrElse Not IsPasskeyUniqueInPasskeysColumn(key)

                used.Add(key)
                passkeys(i) = key
            Next

            ' Save to the correct single 'Passkeys' column as comma-separated values
            Try
                Dim passkeysCombined As String = String.Join(",", passkeys)
                Dim updateQuery As String = "UPDATE Users SET Passkeys = @Passkeys WHERE UserID = @UserID"
                Dim sqlParams() As SqlParameter = {
                New SqlParameter("@Passkeys", passkeysCombined),
                New SqlParameter("@UserID", userId)
            }
                Utilities.ExecuteNonQuery(updateQuery, sqlParams)
                Console.WriteLine($"Successfully saved passkeys for user {userId}: {passkeysCombined}")
            Catch ex As Exception
                Console.WriteLine($"Unable to persist passkeys for user {userId}: {ex.Message}")
            End Try

            Console.WriteLine($"Generated passkeys for user {userId}: {String.Join(", ", passkeys)}")
            Return passkeys
        Catch ex As Exception
            Console.WriteLine($"Error generating passkeys: {ex.Message}")
            Return New String() {"ERROR-KEY1", "ERROR-KEY2", "ERROR-KEY3"}
        End Try
    End Function
    Private Sub txtPhone_KeyPress(sender As Object, e As KeyPressEventArgs)
        ' Only allow digits and control characters (backspace, delete, arrows, etc.)
        If Not Char.IsDigit(e.KeyChar) AndAlso Not Char.IsControl(e.KeyChar) Then
            e.Handled = True
        End If
    End Sub

    ' Add this new method to check uniqueness in the Passkeys column
    Private Function IsPasskeyUniqueInPasskeysColumn(passkey As String) As Boolean
        Try
            ' Check if the passkey exists in the comma-separated Passkeys column
            Dim query As String = "SELECT COUNT(*) FROM Users WHERE (',' + ISNULL(Passkeys, '') + ',') LIKE '%,' + @p + ',%'"
            Dim param As New SqlParameter("@p", passkey)
            Dim count As Integer = CInt(Utilities.ExecuteScalar(query, New SqlParameter() {param}))
            Return count = 0
        Catch ex As Exception
            Console.WriteLine($"Passkey uniqueness check failed (treating as unique): {ex.Message}")
            Return True
        End Try
    End Function

    ' Remove or comment out the old IsPasskeyUnique method since it references non-existent columns
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
    ' Add near the other private fields
    Private statusLocked As Boolean = False

    ' Updated SetEditMode to preserve visual styling for disabled controls and to use statusLocked.
    Public Function SetEditMode(userData As Dictionary(Of String, Object)) As Boolean
        Try
            ' Determine target user id first (do NOT enter edit mode yet)
            editingUserData = userData
            Dim targetUserId As Integer = 0
            If userData IsNot Nothing AndAlso userData.ContainsKey("UserID") Then
                targetUserId = Convert.ToInt32(userData("UserID"))
            End If

            ' Enter edit mode for other users
            isEditMode = True
            editingStaffId = targetUserId

            ' UI text
            Me.Text = "Edit Staff Member"
            btnAddStock.Text = "Update Staff"
            btnAddStock.Refresh()

            ' Populate visible fields
            PopulateFormWithUserData(userData)

            ' Ensure image placeholder reflects edit-mode semantics:
            ' if no photo is present, show "No Photo" instead of "Click to Upload".
            Try
                If isEditMode Then
                    If staffPhotoBytes Is Nothing OrElse staffPhotoBytes.Length = 0 Then
                        lblStaffPicture.Text = "No Photo"
                    Else
                        lblStaffPicture.Text = "Photo Loaded"
                    End If
                End If
            Catch
            End Try

            ' Make all inputs read-only except status. Keep controls Enabled where possible so styling doesn't change.
            Try
                txtUsername.ReadOnly = True

                txtPassword.Text = String.Empty
                txtPassword.ReadOnly = True
                Try
                    txtPassword.PlaceholderText = "******"
                Catch
                End Try

                ' PIN: keep blank and read-only but enabled so background color stays the same
                txtPin.Text = String.Empty
                txtPin.ReadOnly = True
                Try
                    txtPin.PasswordChar = "*"c
                    txtPin.PlaceholderText = "****"
                Catch
                End Try

                txtEmail.ReadOnly = True
                txtPhone.ReadOnly = True

                ' Prevent role changes but preserve appearance
                Try
                    cmbRole.Enabled = False
                    cmbRole.DisabledState.FillColor = cmbRole.FillColor
                    cmbRole.DisabledState.ForeColor = cmbRole.ForeColor
                    cmbRole.DisabledState.BorderColor = cmbRole.FocusedState.BorderColor
                Catch
                End Try

                ' Disable image upload interactions (view-only)
                Try
                    RemoveHandler ProductImage.Click, AddressOf ProductImage_Click
                    RemoveHandler lblStaffPicture.Click, AddressOf ProductImage_Click
                    lblStaffPicture.Text = If(lblStaffPicture.Text = String.Empty, "View Only", lblStaffPicture.Text)
                Catch
                End Try
            Catch ex As Exception
                Console.WriteLine($"Error setting fields readonly in SetEditMode: {ex.Message}")
            End Try

            ' Configure and show status controls (always enabled for other users)
            Try
                lblStatus.Visible = True
                Guna2ComboBox1.Visible = True

                Guna2ComboBox1.Items.Clear()
                Guna2ComboBox1.Items.AddRange(New String() {"Active", "Inactive"})
                Guna2ComboBox1.DropDownStyle = ComboBoxStyle.DropDownList

                Dim currentIsActive As Boolean = True
                If userData IsNot Nothing AndAlso userData.ContainsKey("IsActive") Then
                    Try
                        currentIsActive = Convert.ToBoolean(userData("IsActive"))
                    Catch
                        currentIsActive = True
                    End Try
                End If
                originalIsActive = currentIsActive

                ' Set selection safely
                RemoveHandler Guna2ComboBox1.SelectedIndexChanged, AddressOf Guna2ComboBox1_SelectedIndexChanged
                If currentIsActive Then
                    Guna2ComboBox1.SelectedItem = "Active"
                Else
                    Guna2ComboBox1.SelectedItem = "Inactive"
                End If
                AddHandler Guna2ComboBox1.SelectedIndexChanged, AddressOf Guna2ComboBox1_SelectedIndexChanged

                ' Always allow status editing here
                Guna2ComboBox1.Enabled = True
                btnAddStock.Enabled = True
                Guna2HtmlLabel15.Text = String.Empty
            Catch ex As Exception
                Console.WriteLine($"Error configuring status control in SetEditMode: {ex.Message}")
            End Try

            Me.Refresh()
            Application.DoEvents()

            Return True
        Catch ex As Exception
            Console.WriteLine($"SetEditMode failed: {ex.Message}")
            MessageBox.Show($"Unable to open staff in edit mode: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Return False
        End Try
    End Function
    'pdated status handler — Do Not disable combobox; enforce one-way change With statusLocked.
    Private Sub Guna2ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs)
        Try
            If Not isEditMode Then Return
            If Guna2ComboBox1.SelectedItem Is Nothing Then Return

            ' Update originalIsActive to reflect current selection (status always editable for other users).
            Dim selected As String = Guna2ComboBox1.SelectedItem.ToString()
            originalIsActive = selected.Equals("Active", StringComparison.OrdinalIgnoreCase)

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

    Private Sub AddStaff_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)
    End Sub
End Class