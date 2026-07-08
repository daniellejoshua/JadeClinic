Imports System.IO
Imports System.Drawing.Imaging
Imports MessagingToolkit.QRCode.Codec
Imports System.Data.Common
Imports System.Drawing.Printing

Public Class IdCard
    Private Sub IdCard_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StartMonitoring(Me) ' Wire up buttons if not done in designer
        AddHandler btnClose.Click, Sub() Me.Close()
        AddHandler btnPrint.Click, AddressOf OnPrintClicked
        ' Ensure info labels render on top of Guna2Panel1
        lblRole.BringToFront()
        lblEmail.BringToFront()
        lblEmailTitle.BringToFront()
        lblPhone.BringToFront()
        lblPhoneTitle.BringToFront()
        lblUserID.BringToFront()
        Guna2HtmlLabel1.BringToFront()
    End Sub

    ' Populate the ID card from a user dictionary (keys: UserID, Username, FullName, UserRole, Email, Phone, Photo (byte()), QRCode)
    ' Replace the existing LoadFromUserData method with this version that also renders company logo.
    Public Sub LoadFromUserData(userData As Dictionary(Of String, Object))
        Try
            If userData Is Nothing Then Return

            Dim fullName As String = If(userData.ContainsKey("FullName"), If(userData("FullName"), String.Empty).ToString(), String.Empty)
            Dim username As String = If(userData.ContainsKey("Username"), If(userData("Username"), String.Empty).ToString(), String.Empty)
            Dim role As String = If(userData.ContainsKey("UserRole"), If(userData("UserRole"), String.Empty).ToString(), String.Empty)
            Dim email As String = If(userData.ContainsKey("Email"), If(userData("Email"), String.Empty).ToString(), String.Empty)
            Dim phone As String = If(userData.ContainsKey("Phone"), If(userData("Phone"), String.Empty).ToString(), String.Empty)
            Dim userId As String = If(userData.ContainsKey("UserID"), If(userData("UserID"), String.Empty).ToString(), String.Empty)

            ' Fill UI fields
            Try
                txtUsername.Text = fullName
            Catch
                Try
                    txtUsername.DefaultText = fullName
                Catch
                End Try
            End Try

            txtUsername.Text = username
            lblRole.Text = role
            lblEmail.Text = email
            lblPhone.Text = phone
            lblUserID.Text = If(String.IsNullOrWhiteSpace(userId), lblUserID.Text, userId)

            ' Render company logo (if available) into picCompanyLogo
            Try
                Dim logoImg As System.Drawing.Image = Nothing
                Try
                    logoImg = CompanySettingsManager.Instance.GetCompanyLogo()
                Catch ex As Exception
                    Console.WriteLine($"Unable to get company logo: {ex.Message}")
                End Try

                If logoImg IsNot Nothing Then
                    DisposePictureBoxImage(picCompanyLogo)
                    picCompanyLogo.Image = New Bitmap(logoImg)
                    picCompanyLogo.SizeMode = PictureBoxSizeMode.Zoom
                Else
                    ' If no logo, show company name label (designer had it hidden)
                End If
            Catch ex As Exception
                Console.WriteLine($"Company logo rendering failed: {ex.Message}")
            End Try

            ' Photo handling - prefer supplied byte[] in dictionary
            Dim photoSet As Boolean = False
            If userData.ContainsKey("Photo") AndAlso userData("Photo") IsNot Nothing Then
                Dim photoBytes = TryCast(userData("Photo"), Byte())
                If photoBytes IsNot Nothing AndAlso photoBytes.Length > 0 Then
                    Using ms As New MemoryStream(photoBytes)
                        DisposePictureBoxImage(picStaffPhoto)
                        picStaffPhoto.Image = Image.FromStream(ms)
                        picStaffPhoto.SizeMode = PictureBoxSizeMode.Zoom
                        photoSet = True
                    End Using
                End If
            End If

            ' QR handling - prefer supplied text in dictionary
            Dim qrText As String = Nothing
            If userData.ContainsKey("QRCode") AndAlso userData("QRCode") IsNot Nothing Then
                qrText = userData("QRCode").ToString().Trim()
            End If

            ' If photo or QR not provided in dictionary, try fetching from DB using UserID
            If (Not photoSet OrElse String.IsNullOrWhiteSpace(qrText)) AndAlso Not String.IsNullOrWhiteSpace(userId) Then
                Try
                    Using rdr As DbDataReader = Utilities.ExecuteReader("SELECT Photo, QRCode FROM Users WHERE UserID = @UserID", New SqlParameter("@UserID", userId))
                        If rdr.Read() Then
                            If (Not photoSet) AndAlso Not IsDBNull(rdr("Photo")) Then
                                Dim dbPhoto = CType(rdr("Photo"), Byte())
                                Using ms As New MemoryStream(dbPhoto)
                                    DisposePictureBoxImage(picStaffPhoto)
                                    picStaffPhoto.Image = Image.FromStream(ms)
                                    picStaffPhoto.SizeMode = PictureBoxSizeMode.Zoom
                                    photoSet = True
                                End Using
                            End If

                            If String.IsNullOrWhiteSpace(qrText) AndAlso Not IsDBNull(rdr("QRCode")) Then
                                qrText = rdr("QRCode").ToString()
                            End If
                        End If
                    End Using
                Catch ex As Exception
                    Console.WriteLine($"IdCard DB fetch error: {ex.Message}")
                End Try
            End If

            ' If still no photo, show placeholder (designer may already show one)
            If Not photoSet Then
                DisposePictureBoxImage(picStaffPhoto)
                picStaffPhoto.Image = Nothing
            End If

            ' If QR text present, generate QR image; otherwise generate from username/userid fallback
            If Not String.IsNullOrWhiteSpace(qrText) Then
                DisposePictureBoxImage(QrCodePicturebox)
                QrCodePicturebox.Image = GenerateQrBitmap(qrText)
                QrCodePicturebox.SizeMode = PictureBoxSizeMode.StretchImage
            Else
                Dim fallback As String = If(Not String.IsNullOrWhiteSpace(username), $"USER:{username}", If(Not String.IsNullOrWhiteSpace(userId), $"USERID:{userId}", Guid.NewGuid().ToString()))
                DisposePictureBoxImage(QrCodePicturebox)
                QrCodePicturebox.Image = GenerateQrBitmap(fallback)
                QrCodePicturebox.SizeMode = PictureBoxSizeMode.StretchImage
            End If

        Catch ex As Exception
            MessageBox.Show($"Unable to load ID card: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
    ' Replace the existing GenerateQrBitmap with this MessagingToolkit implementation
    Private Function GenerateQrBitmap(content As String) As Bitmap
        Try
            If String.IsNullOrWhiteSpace(content) Then
                content = Guid.NewGuid().ToString()
            End If

            Dim encoder As New QRCodeEncoder()
            ' Adjust scale to fit your picturebox size (2..8 typically)
            encoder.QRCodeScale = 4

            ' Optional: set colors (white background, black foreground)
            Try
                encoder.QRCodeBackgroundColor = Color.White
                encoder.QRCodeForegroundColor = Color.Black
            Catch
                ' Older versions may not expose color properties � ignore if not available
            End Try

            Dim bmp As Bitmap = encoder.Encode(content)
            Return New Bitmap(bmp)
        Catch ex As Exception
            ' Return blank image on error
            Dim bmp As New Bitmap(If(QrCodePicturebox IsNot Nothing, QrCodePicturebox.Width, 200), If(QrCodePicturebox IsNot Nothing, QrCodePicturebox.Height, 200))
            Using g = Graphics.FromImage(bmp)
                g.Clear(Color.White)
            End Using
            Return bmp
        End Try
    End Function

    ' Safe dispose helper to avoid locked image streams
    Private Sub DisposePictureBoxImage(pb As PictureBox)
        Try
            If pb IsNot Nothing AndAlso pb.Image IsNot Nothing Then
                Dim old = pb.Image
                pb.Image = Nothing
                old.Dispose()
            End If
        Catch
        End Try
    End Sub

    ' Print button handler - print the ID card panel
    Private Sub OnPrintClicked(sender As Object, e As EventArgs)
        Try
            Using pd As New PrintDocument()
                AddHandler pd.PrintPage, Sub(s, ev)
                                             Using bmp As New Bitmap(pnlIDCard.Width, pnlIDCard.Height)
                                                 pnlIDCard.DrawToBitmap(bmp, New Rectangle(0, 0, bmp.Width, bmp.Height))
                                                 ' Fit to printable area keeping aspect
                                                 Dim area = ev.MarginBounds
                                                 ev.Graphics.DrawImage(bmp, area)
                                             End Using
                                         End Sub

                Using dlg As New PrintPreviewDialog()
                    dlg.Document = pd
                    dlg.WindowState = FormWindowState.Maximized
                    dlg.ShowDialog()
                End Using
            End Using
        Catch ex As Exception
            MessageBox.Show($"Print failed: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub picStaffPhoto_Click(sender As Object, e As EventArgs) Handles picStaffPhoto.Click

    End Sub

    Private Sub picCompanyLogo_Click(sender As Object, e As EventArgs) Handles picCompanyLogo.Click

    End Sub

    Private Sub lblPin_Click(sender As Object, e As EventArgs) Handles lblPin.Click

    End Sub

    Private Sub Guna2Panel1_Paint(sender As Object, e As PaintEventArgs) Handles Guna2Panel1.Paint

    End Sub

    Private Sub QrCodePicturebox_Click(sender As Object, e As EventArgs) Handles QrCodePicturebox.Click

    End Sub

    Private Sub IdCard_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ' Stop idle timeout monitoring
        IdleTimeoutManager.Instance.StopMonitoring(Me)
    End Sub
End Class