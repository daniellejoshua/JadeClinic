Imports System.Drawing
Imports System.Drawing.Printing
Imports System.IO
Imports System.Windows.Forms
Imports Guna.UI2.WinForms
Imports System.Data.Common
Imports System.Linq

Public Class CompanySettings
    Private currentCompanyData As Dictionary(Of String, Object) = Nothing
    Private logoChanged As Boolean = False
    Private logoData As Byte() = Nothing
    Private originalValues As Dictionary(Of String, String) = Nothing ' Track original values
    Private txtCompanyHours As TextBox ' Summary/preview field for company hours
    Private dtOpeningTime As DateTimePicker
    Private dtClosingTime As DateTimePicker
    Private clbClosedDays As CheckedListBox

    ' Dental Clinic Color Palette Constants
    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)      ' #FECF10 - Primary brand color
    Private ReadOnly RichOlive As Color = Color.FromArgb(190, 154, 48)         ' #BE9A30 - Secondary accent
    Private ReadOnly DeepCharcoal As Color = Color.FromArgb(26, 29, 31)        ' #1A1D1F - Primary dark
    Private ReadOnly DarkSlate As Color = Color.FromArgb(43, 47, 50)           ' #2B2F32 - Secondary dark
    Private ReadOnly Graphite As Color = Color.FromArgb(61, 65, 69)            ' #3D4145 - Card background
    Private ReadOnly SteelGray As Color = Color.FromArgb(74, 79, 84)           ' #4A4F54 - Interactive elements
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)        ' #FFFFFF - Text on dark
    Private ReadOnly LightSilver As Color = Color.FromArgb(225, 229, 233)      ' #E1E5E9 - Secondary text
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)       ' #10D862 - Success states
    Private ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)           ' #FF4757 - Error/Alert states

    Private Sub CompanySettings_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize UI first
        InitializeUI()

        ' Load current company settings
        LoadCompanySettings()

        ' Store original values for change detection
        StoreOriginalValues()

        SetupTabIndex()
    End Sub

    Private Sub SetupTabIndex()
        TabControl1.TabIndex = 0
        txtCompanyName.TabIndex = 1
        txtTIN.TabIndex = 2
        txtAddress.TabIndex = 3
        txtPhone.TabIndex = 4
        txtEmail.TabIndex = 5
        txtWebsite.TabIndex = 6
        txtBIRAuth.TabIndex = 7
        txtPTUNumber.TabIndex = 8
        nudValidityYears.TabIndex = 9
        txtReceiptFooter.TabIndex = 10
        dtOpeningTime.TabIndex = 11
        dtClosingTime.TabIndex = 12
        clbClosedDays.TabIndex = 13
        btnChangeLogo.TabIndex = 14
        btnRemoveLogo.TabIndex = 15
        btnPreviewReceipt.TabIndex = 16
        btnSave.TabIndex = 17
        btnCancel.TabIndex = 18
        Utilities.ApplyInputFocusEffects(Me)
    End Sub

    Private Sub StoreOriginalValues()
        originalValues = New Dictionary(Of String, String) From {
            {"CompanyName", txtCompanyName.Text},
            {"TIN", txtTIN.Text},
            {"Address", txtAddress.Text},
            {"Phone", txtPhone.Text},
            {"Email", txtEmail.Text},
            {"Website", txtWebsite.Text},
            {"BIRAuthNumber", txtBIRAuth.Text},
            {"PTUNumber", txtPTUNumber.Text},
            {"ValidityYears", nudValidityYears.Value.ToString()},
            {"ReceiptFooter", txtReceiptFooter.Text},
            {"CompanyHours", If(txtCompanyHours?.Text, "")} ' <-- added
        }
    End Sub

    Private Sub InitializeUI()
        ' Set tab control style
        TabControl1.Appearance = TabAppearance.Buttons

        ' Add new tab for Company Hours
        Dim hoursTab As New TabPage("Company Hours")
        hoursTab.BackColor = Color.White

        Dim lblOpening As New Label()
        lblOpening.Text = "Opening Time:"
        lblOpening.Location = New Point(20, 20)
        lblOpening.AutoSize = True
        lblOpening.Font = New Font("Poppins", 10, FontStyle.Bold)

        dtOpeningTime = New DateTimePicker()
        dtOpeningTime.Format = DateTimePickerFormat.Custom
        dtOpeningTime.CustomFormat = "hh:mm tt"
        dtOpeningTime.ShowUpDown = True
        dtOpeningTime.Location = New Point(20, 45)
        dtOpeningTime.Width = 180

        Dim lblClosing As New Label()
        lblClosing.Text = "Closing Time:"
        lblClosing.Location = New Point(220, 20)
        lblClosing.AutoSize = True
        lblClosing.Font = New Font("Poppins", 10, FontStyle.Bold)

        dtClosingTime = New DateTimePicker()
        dtClosingTime.Format = DateTimePickerFormat.Custom
        dtClosingTime.CustomFormat = "hh:mm tt"
        dtClosingTime.ShowUpDown = True
        dtClosingTime.Location = New Point(220, 45)
        dtClosingTime.Width = 180

        Dim lblClosedDays As New Label()
        lblClosedDays.Text = "Closed Days:"
        lblClosedDays.Location = New Point(20, 85)
        lblClosedDays.AutoSize = True
        lblClosedDays.Font = New Font("Poppins", 10, FontStyle.Bold)

        clbClosedDays = New CheckedListBox()
        clbClosedDays.Location = New Point(20, 110)
        clbClosedDays.Size = New Size(220, 230)
        clbClosedDays.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left
        clbClosedDays.CheckOnClick = True
        clbClosedDays.Items.AddRange(New Object() {"Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"})

        Dim lblSummary As New Label()
        lblSummary.Text = "Working Hours Summary:"
        lblSummary.Location = New Point(280, 85)
        lblSummary.AutoSize = True
        lblSummary.Font = New Font("Poppins", 10, FontStyle.Bold)

        txtCompanyHours = New TextBox()
        txtCompanyHours.Location = New Point(280, 110)
        txtCompanyHours.Size = New Size(340, 230)
        txtCompanyHours.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        txtCompanyHours.Multiline = True
        txtCompanyHours.ScrollBars = ScrollBars.Vertical
        txtCompanyHours.Font = New Font("Poppins", 9)
        txtCompanyHours.ReadOnly = True

        AddHandler dtOpeningTime.ValueChanged, Sub() RefreshCompanyHoursSummary()
        AddHandler dtClosingTime.ValueChanged, Sub() RefreshCompanyHoursSummary()
        AddHandler clbClosedDays.ItemCheck,
        Sub(sender, e)
            BeginInvoke(New Action(Sub() RefreshCompanyHoursSummary()))
        End Sub

        hoursTab.Controls.Add(lblOpening)
        hoursTab.Controls.Add(dtOpeningTime)
        hoursTab.Controls.Add(lblClosing)
        hoursTab.Controls.Add(dtClosingTime)
        hoursTab.Controls.Add(lblClosedDays)
        hoursTab.Controls.Add(clbClosedDays)
        hoursTab.Controls.Add(lblSummary)
        hoursTab.Controls.Add(txtCompanyHours)

        TabControl1.TabPages.Add(hoursTab)

        RefreshCompanyHoursSummary()

        ' Add hover effects to buttons
        AddButtonHoverEffects()
    End Sub

    Private Sub AddButtonHoverEffects()
    End Sub

    Private Sub LoadCompanySettings()
        Try
            Dim query As String = "SELECT * FROM CompanySettings WHERE IsActive = 1 ORDER BY DateCreated DESC"
            Using reader As DbDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                If reader.Read() Then
                    ' Store current data
                    currentCompanyData = New Dictionary(Of String, Object)
                    For i = 0 To reader.FieldCount - 1
                        currentCompanyData(reader.GetName(i)) = If(reader.IsDBNull(i), Nothing, reader.GetValue(i))
                    Next

                    ' Populate form fields
                    PopulateFormFields()
                Else
                    ' No settings found, use defaults
                    SetDefaultValues()
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Error loading company settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            SetDefaultValues()
        End Try
    End Sub


    Private Sub PopulateFormFields()
        If currentCompanyData Is Nothing Then Return

        ' Basic information
        txtCompanyName.Text = GetSettingValue("CompanyName", "JADE CLINIC")
        txtTIN.Text = GetSettingValue("TIN", "123-456-789-000")
        txtAddress.Text = GetSettingValue("Address", "")
        txtPhone.Text = GetSettingValue("Phone", "")
        txtEmail.Text = GetSettingValue("Email", "")
        txtWebsite.Text = GetSettingValue("Website", "")

        ' Receipt settings
        txtBIRAuth.Text = GetSettingValue("BIRAuthNumber", "ATP-2024-000001")
        txtPTUNumber.Text = GetSettingValue("PTUNumber", "PTU-2024-001")
        nudValidityYears.Value = Convert.ToDecimal(GetSettingValue("ValidityYears", 5))
        txtReceiptFooter.Text = GetSettingValue("ReceiptFooter", "Thank you for your business!" & vbCrLf & "Have a great day!")

        ' Company hours (structured)
        ApplyCompanyHoursFromStoredValue(GetSettingValue("CompanyHours", ""))

        Dim logoPath As String = GetSettingValue("LogoPath", "")
        If Not String.IsNullOrEmpty(logoPath) Then
            Try
                Dim fullPath As String = Path.Combine(Connection.GetImagesFolder("company"), logoPath)
                If IO.File.Exists(fullPath) Then
                    picLogo.Image = Image.FromFile(fullPath)
                Else
                    SetDefaultLogoPlaceholder()
                End If
            Catch
                SetDefaultLogoPlaceholder()
            End Try
        Else
            SetDefaultLogoPlaceholder()
        End If
    End Sub

    Private Function GetSettingValue(key As String, defaultValue As Object) As String
        If currentCompanyData IsNot Nothing AndAlso currentCompanyData.ContainsKey(key) AndAlso currentCompanyData(key) IsNot Nothing Then
            Return currentCompanyData(key).ToString()
        End If
        Return defaultValue.ToString()
    End Function

    Private Sub SetDefaultValues()
        txtCompanyName.Text = "JADE CLINIC"
        txtTIN.Text = "123-456-789-000"
        txtAddress.Text = "123 Medical Plaza, Makati City, Philippines"
        txtPhone.Text = "(02) 8123-4567"
        txtEmail.Text = "admin@jadeclinic.com"
        txtWebsite.Text = "www.jadeclinic.com"
        txtBIRAuth.Text = "ATP-2024-000001"
        txtPTUNumber.Text = "PTU-2024-001"
        nudValidityYears.Value = 5
        txtReceiptFooter.Text = "Thank you for your business!" & vbCrLf & "Have a great day!"

        If dtOpeningTime IsNot Nothing Then dtOpeningTime.Value = Date.Today.AddHours(9)
        If dtClosingTime IsNot Nothing Then dtClosingTime.Value = Date.Today.AddHours(17)
        If clbClosedDays IsNot Nothing Then
            For i As Integer = 0 To clbClosedDays.Items.Count - 1
                clbClosedDays.SetItemChecked(i, False)
            Next
            clbClosedDays.SetItemChecked(clbClosedDays.Items.IndexOf("Sunday"), True)
        End If
        RefreshCompanyHoursSummary()

        SetDefaultLogoPlaceholder()
    End Sub

    Private Sub SetDefaultLogoPlaceholder()
        ' Try loading embedded resource first
        Try
            Dim resImg As Image = TryCast(My.Resources.Jade_Dental_Logo1, Image)
            If resImg IsNot Nothing Then
                ' Clone so the resource image isn't locked
                picLogo.Image = New Bitmap(resImg)
                Return
            End If
        Catch
            ' Fall through to drawn placeholder on error
        End Try

        ' Fallback: draw programmatic placeholder
        Dim placeholder As New Bitmap(200, 150)
        Using g As Graphics = Graphics.FromImage(placeholder)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.FillRectangle(New SolidBrush(Color.LightGray), 0, 0, 200, 150)
            Using f As New Font("Poppins", 12, FontStyle.Bold)
                g.DrawString("Company Logo", f, New SolidBrush(Color.Gray), 35, 60)
            End Using
        End Using
        picLogo.Image = placeholder
    End Sub

    Private Function GetDefaultLogoBytes() As Byte()
        ' Try returning embedded resource bytes first
        Try
            Dim resImg As Image = TryCast(My.Resources.FinalLogoOfJAde, Image)
            If resImg IsNot Nothing Then
                Using ms As New MemoryStream()
                    resImg.Save(ms, System.Drawing.Imaging.ImageFormat.Png)
                    Return ms.ToArray()
                End Using
            End If
        Catch
            ' Fall through to generated placeholder bytes
        End Try

        ' Create the same fallback placeholder and return PNG bytes
        Dim placeholder As New Bitmap(200, 150)
        Using g As Graphics = Graphics.FromImage(placeholder)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.Clear(Color.LightGray)
            Using f As New Font("Poppins", 12, FontStyle.Bold)
                Dim text As String = "Company Logo"
                Dim textSize = g.MeasureString(text, f)
                g.DrawString(text, f, Brushes.Gray, (placeholder.Width - textSize.Width) / 2.0F, (placeholder.Height - textSize.Height) / 2.0F)
            End Using
        End Using

        Using ms As New MemoryStream()
            placeholder.Save(ms, System.Drawing.Imaging.ImageFormat.Png)
            Return ms.ToArray()
        End Using
    End Function

    Private Sub RefreshCompanyHoursSummary()
        If txtCompanyHours Is Nothing OrElse dtOpeningTime Is Nothing OrElse dtClosingTime Is Nothing OrElse clbClosedDays Is Nothing Then Return

        Dim closedDays = clbClosedDays.CheckedItems.Cast(Of Object)().Select(Function(x) x.ToString()).ToList()
        Dim closedText As String = If(closedDays.Count > 0, String.Join(", ", closedDays), "None")

        txtCompanyHours.Text =
            $"Opening: {dtOpeningTime.Value.ToString("hh:mm tt")}{vbCrLf}" &
            $"Closing: {dtClosingTime.Value.ToString("hh:mm tt")}{vbCrLf}" &
            $"Closed Days: {closedText}"
    End Sub

    Private Sub ApplyCompanyHoursFromStoredValue(value As String)
        ' Defaults
        dtOpeningTime.Value = Date.Today.AddHours(9)
        dtClosingTime.Value = Date.Today.AddHours(17)
        For i As Integer = 0 To clbClosedDays.Items.Count - 1
            clbClosedDays.SetItemChecked(i, False)
        Next
        clbClosedDays.SetItemChecked(clbClosedDays.Items.IndexOf("Sunday"), True)

        If Not String.IsNullOrWhiteSpace(value) Then
            Dim lines = value.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)

            For Each line In lines
                If line.StartsWith("Opening:", StringComparison.OrdinalIgnoreCase) Then
                    Dim timePart = line.Substring(8).Trim()
                    Dim parsed As DateTime
                    If DateTime.TryParse(timePart, parsed) Then
                        dtOpeningTime.Value = Date.Today.Add(parsed.TimeOfDay)
                    End If
                ElseIf line.StartsWith("Closing:", StringComparison.OrdinalIgnoreCase) Then
                    Dim timePart = line.Substring(8).Trim()
                    Dim parsed As DateTime
                    If DateTime.TryParse(timePart, parsed) Then
                        dtClosingTime.Value = Date.Today.Add(parsed.TimeOfDay)
                    End If
                ElseIf line.StartsWith("Closed Days:", StringComparison.OrdinalIgnoreCase) Then
                    Dim daysPart = line.Substring(12).Trim()
                    For i As Integer = 0 To clbClosedDays.Items.Count - 1
                        Dim dayName = clbClosedDays.Items(i).ToString()
                        clbClosedDays.SetItemChecked(i, daysPart.IndexOf(dayName, StringComparison.OrdinalIgnoreCase) >= 0)
                    Next
                End If
            Next
        End If

        RefreshCompanyHoursSummary()
    End Sub

    Private Sub btnChangeLogo_Click(sender As Object, e As EventArgs) Handles btnChangeLogo.Click
        Try
            Dim openFileDialog As New OpenFileDialog()
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*"
            openFileDialog.Title = "Select Company Logo"

            If openFileDialog.ShowDialog() = DialogResult.OK Then
                Dim image As Image = Image.FromFile(openFileDialog.FileName)
                picLogo.Image = image

                Dim destDir As String = Connection.GetImagesFolder("company")
                Dim destPath As String = Path.Combine(destDir, "logo.png")
                image.Save(destPath, System.Drawing.Imaging.ImageFormat.Png)

                logoChanged = True
                MessageBox.Show("Logo updated! Click Save to apply changes.", "Logo Changed", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Catch ex As Exception
            MessageBox.Show($"Error loading logo: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnRemoveLogo_Click(sender As Object, e As EventArgs) Handles btnRemoveLogo.Click
        Dim result = MessageBox.Show("Are you sure you want to remove the company logo?", "Confirm Remove", MessageBoxButtons.YesNo, MessageBoxIcon.Question)

        If result = DialogResult.Yes Then
            SetDefaultLogoPlaceholder()
            logoChanged = True
            MessageBox.Show("Logo removed! Click Save to apply changes.", "Logo Removed", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    Private Sub btnPreviewReceipt_Click(sender As Object, e As EventArgs) Handles btnPreviewReceipt.Click
        Try
            ShowReceiptPreview()
        Catch ex As Exception
            MessageBox.Show($"Error generating receipt preview: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ShowReceiptPreview()
        Try
            Dim printDoc As New PrintDocument()
            printDoc.DefaultPageSettings.PaperSize = New PaperSize("ReceiptPreview", 300, 900)
            printDoc.DefaultPageSettings.Margins = New Margins(10, 10, 10, 10)

            AddHandler printDoc.PrintPage, AddressOf OnCompanySettingsPreviewPrintPage

            Dim printPreview As New PrintPreviewDialog()
            printPreview.Document = printDoc
            printPreview.Text = $"Receipt Preview - {If(String.IsNullOrWhiteSpace(txtCompanyName.Text), "JADE CLINIC", txtCompanyName.Text.Trim())}"
            printPreview.WindowState = FormWindowState.Maximized
            printPreview.ShowDialog()
        Catch ex As Exception
            MessageBox.Show($"Error showing print preview: {ex.Message}", "Preview Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub DrawCenteredLine(g As Graphics, text As String, f As Font, brush As Brush, y As Single, bounds As Rectangle)
        Dim textWidth As Single = g.MeasureString(text, f).Width
        Dim x As Single = bounds.Left + ((bounds.Width - textWidth) / 2.0F)
        g.DrawString(text, f, brush, x, y)
    End Sub

    Private Sub DrawLeftRightLine(g As Graphics, leftText As String, rightText As String, f As Font, brush As Brush, leftX As Single, rightX As Single, y As Single)
        g.DrawString(leftText, f, brush, leftX, y)
        Dim rightWidth As Single = g.MeasureString(rightText, f).Width
        g.DrawString(rightText, f, brush, rightX - rightWidth, y)
    End Sub

    Private Sub OnCompanySettingsPreviewPrintPage(sender As Object, e As PrintPageEventArgs)
        Try
            Dim g As Graphics = e.Graphics
            Dim brush As New SolidBrush(Color.Black)

            Dim headerFont As New Font("Arial", 14, FontStyle.Bold)
            Dim titleFont As New Font("Arial", 12, FontStyle.Bold)
            Dim sectionFont As New Font("Arial", 11, FontStyle.Bold)
            Dim regularFont As New Font("Arial", 10, FontStyle.Regular)
            Dim totalFont As New Font("Arial", 14, FontStyle.Bold)

            Dim leftX As Single = e.MarginBounds.Left
            Dim rightX As Single = e.MarginBounds.Right
            Dim y As Single = e.MarginBounds.Top
            Dim lineH As Single = 16.0F
            Dim sep As String = "========================================"

            DrawCenteredLine(g, If(String.IsNullOrWhiteSpace(txtCompanyName.Text), "JADE CLINIC", txtCompanyName.Text.Trim()), headerFont, brush, y, e.MarginBounds)
            y += 28
            DrawCenteredLine(g, "Dental Supply Management", regularFont, brush, y, e.MarginBounds)
            y += lineH

            If Not String.IsNullOrWhiteSpace(txtTIN.Text) Then
                DrawCenteredLine(g, $"TIN: {txtTIN.Text} (VAT Registered)", regularFont, brush, y, e.MarginBounds)
                y += lineH
            End If
            If Not String.IsNullOrWhiteSpace(txtPhone.Text) Then
                DrawCenteredLine(g, $"Tel: {txtPhone.Text}", regularFont, brush, y, e.MarginBounds)
                y += lineH
            End If
            If Not String.IsNullOrWhiteSpace(txtAddress.Text) Then
                DrawCenteredLine(g, txtAddress.Text, regularFont, brush, y, e.MarginBounds)
                y += lineH
            End If
            If Not String.IsNullOrWhiteSpace(txtWebsite.Text) Then
                DrawCenteredLine(g, txtWebsite.Text, regularFont, brush, y, e.MarginBounds)
                y += lineH
            End If

            If Not String.IsNullOrWhiteSpace(txtCompanyHours.Text) Then
                y += 6
                DrawCenteredLine(g, "Clinic Hours", sectionFont, brush, y, e.MarginBounds)
                y += lineH
                For Each line As String In txtCompanyHours.Text.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                    DrawCenteredLine(g, line.Trim(), regularFont, brush, y, e.MarginBounds)
                    y += lineH
                Next
            End If

            g.DrawString(sep, regularFont, brush, leftX, y)
            y += lineH + 2

            DrawCenteredLine(g, "SALES INVOICE", titleFont, brush, y, e.MarginBounds)
            y += 24

            g.DrawString("Receipt #: 9", regularFont, brush, leftX, y)
            y += lineH
            g.DrawString($"Date: {DateTime.Now:MM/dd/yyyy HH:mm:ss}", regularFont, brush, leftX, y)
            y += lineH
            g.DrawString($"Cashier: {frmLoginvb.LoggedInEmployeeCode}", regularFont, brush, leftX, y)
            y += lineH

            g.DrawString("Customer Details:", regularFont, brush, leftX, y)
            y += lineH
            DrawLeftRightLine(g, "Name: wawa", "TIN: ______________", regularFont, brush, leftX, rightX, y)
            y += lineH
            DrawLeftRightLine(g, "Phone: ______________", "Email: ______________", regularFont, brush, leftX, rightX, y)
            y += lineH

            g.DrawString(sep, regularFont, brush, leftX, y)
            y += lineH
            g.DrawString("1x Applicator tip", regularFont, brush, leftX, y)
            y += lineH
            DrawLeftRightLine(g, "@ ?150.00", "?150.00", regularFont, brush, leftX + 10, rightX, y)
            y += lineH
            g.DrawString("1x Alginate (hygedent)", regularFont, brush, leftX, y)
            y += lineH
            DrawLeftRightLine(g, "@ ?280.00", "?280.00", regularFont, brush, leftX + 10, rightX, y)
            y += lineH

            g.DrawString(sep, regularFont, brush, leftX, y)
            y += lineH
            DrawLeftRightLine(g, "SUBTOTAL (VAT-INC):", "?430.00", regularFont, brush, leftX, rightX, y)
            y += lineH
            DrawLeftRightLine(g, "Less: Discount (Fixed):", "-?100.00", regularFont, brush, leftX, rightX, y)
            y += lineH
            DrawLeftRightLine(g, "VATABLE SALES (NET):", "?294.64", regularFont, brush, leftX, rightX, y)
            y += lineH
            DrawLeftRightLine(g, "VAT (12%):", "?35.36", regularFont, brush, leftX, rightX, y)
            y += lineH

            g.DrawString(sep, regularFont, brush, leftX, y)
            y += lineH
            DrawLeftRightLine(g, "TOTAL AMOUNT DUE:", "?330.00", totalFont, brush, leftX, rightX, y)
            y += 28

            g.DrawString("PAYMENT INFORMATION", sectionFont, brush, leftX, y)
            y += lineH
            g.DrawString("Payment Method: Cash", regularFont, brush, leftX, y)
            y += lineH
            g.DrawString("Amount Received: ?330.00", regularFont, brush, leftX, y)
            y += lineH
            g.DrawString("Change: ?0.00", regularFont, brush, leftX, y)
            y += lineH

            g.DrawString(sep, regularFont, brush, leftX, y)
            y += lineH
            g.DrawString($"BIR Authority to Print No.: {txtBIRAuth.Text}", regularFont, brush, leftX, y)
            y += lineH
            g.DrawString($"PTU No.: {txtPTUNumber.Text}", regularFont, brush, leftX, y)
            y += lineH
            g.DrawString(sep, regularFont, brush, leftX, y)
            y += lineH

            If Not String.IsNullOrWhiteSpace(txtReceiptFooter.Text) Then
                For Each line As String In txtReceiptFooter.Text.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                    DrawCenteredLine(g, line.Trim(), regularFont, brush, y, e.MarginBounds)
                    y += lineH
                Next
            Else
                DrawCenteredLine(g, "Thankyou for your business!", regularFont, brush, y, e.MarginBounds)
            End If

            brush.Dispose()
            headerFont.Dispose()
            titleFont.Dispose()
            sectionFont.Dispose()
            regularFont.Dispose()
            totalFont.Dispose()
        Catch ex As Exception
            Console.WriteLine($"Preview print rendering error: {ex.Message}")
        End Try
    End Sub

    Private Function GenerateSampleReceipt() As String
        Dim receipt As New System.Text.StringBuilder()
        receipt.AppendLine("Receipt preview uses print document rendering.")
        Return receipt.ToString()
    End Function

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Try
            If String.IsNullOrWhiteSpace(txtCompanyName.Text) Then
                MessageBox.Show("Company name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCompanyName.Focus()
                Return
            End If

            SaveCompanySettings()

            CompanySettingsManager.Instance.RefreshCache()

            MessageBox.Show("Company settings saved successfully!", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Company Settings Updated", "Company settings configuration changed")

            logoChanged = False
            StoreOriginalValues()

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Error saving company settings: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            Console.WriteLine($"Save Error Details: {ex.ToString()}")
        End Try
    End Sub

    Private Sub SaveCompanySettings()
        Try
            Dim existingCount As Integer = 0
            Dim countQuery As String = "SELECT COUNT(*) FROM CompanySettings WHERE IsActive = 1"
            Dim countResult = Utilities.ExecuteScalar(countQuery, New SqlParameter() {})
            If countResult IsNot Nothing Then
                existingCount = Convert.ToInt32(countResult)
            End If

            Dim isUpdate As Boolean = (existingCount > 0)

            Dim sql As String
            Dim parameters As New List(Of SqlParameter)()

            If logoChanged Then
                Dim destDir As String = Connection.GetImagesFolder("company")
                Dim destPath As String = Path.Combine(destDir, "logo.png")
                If Not IO.File.Exists(destPath) Then
                    Try
                        Using resImg As Image = My.Resources.CleanJadeLogo_1_
                            If resImg IsNot Nothing Then
                                resImg.Save(destPath, System.Drawing.Imaging.ImageFormat.Png)
                            End If
                        End Using
                    Catch
                    End Try
                End If
            End If

            If isUpdate Then
                sql = "UPDATE CompanySettings SET " &
                  "CompanyName = @CompanyName, " &
                  "TIN = @TIN, " &
                  "Address = @Address, " &
                  "Phone = @Phone, " &
                  "Email = @Email, " &
                  "Website = @Website, " &
                  "BIRAuthNumber = @BIRAuthNumber, " &
                  "PTUNumber = @PTUNumber, " &
                  "ValidityYears = @ValidityYears, " &
                  "ReceiptFooter = @ReceiptFooter, " &
                  "CompanyHours = @CompanyHours, " &
                  "LastModified = @LastModified"

                If logoChanged Then
                    sql += ", LogoPath = @LogoPath"
                End If

                sql += " WHERE IsActive = 1"
            Else
                sql = "INSERT INTO CompanySettings " &
                  "(CompanyName, TIN, Address, Phone, Email, Website, LogoPath, " &
                  "BIRAuthNumber, PTUNumber, ValidityYears, ReceiptFooter, CompanyHours, " &
                  "IsActive, DateCreated, LastModified) " &
                  "VALUES " &
                  "(@CompanyName, @TIN, @Address, @Phone, @Email, @Website, @LogoPath, " &
                  "@BIRAuthNumber, @PTUNumber, @ValidityYears, @ReceiptFooter, @CompanyHours, " &
                  "1, @DateCreated, @LastModified)"

                parameters.Add(New SqlParameter("@DateCreated", DateTime.Now))
            End If

            parameters.Add(New SqlParameter("@CompanyName", If(String.IsNullOrWhiteSpace(txtCompanyName.Text), "JADE CLINIC", txtCompanyName.Text.Trim())))
            parameters.Add(New SqlParameter("@TIN", If(txtTIN.Text, "").Trim()))
            parameters.Add(New SqlParameter("@Address", If(txtAddress.Text, "").Trim()))
            parameters.Add(New SqlParameter("@Phone", If(txtPhone.Text, "").Trim()))
            parameters.Add(New SqlParameter("@Email", If(txtEmail.Text, "").Trim()))
            parameters.Add(New SqlParameter("@Website", If(txtWebsite.Text, "").Trim()))
            parameters.Add(New SqlParameter("@BIRAuthNumber", If(txtBIRAuth.Text, "ATP-2024-000001").Trim()))
            parameters.Add(New SqlParameter("@PTUNumber", If(txtPTUNumber.Text, "PTU-2024-001").Trim()))
            parameters.Add(New SqlParameter("@ValidityYears", CInt(nudValidityYears.Value)))
            parameters.Add(New SqlParameter("@ReceiptFooter", If(txtReceiptFooter.Text, "Thank you for your business!").Trim()))
            parameters.Add(New SqlParameter("@CompanyHours", If(txtCompanyHours.Text, "").Trim()))
            parameters.Add(New SqlParameter("@LastModified", DateTime.Now))

            If logoChanged OrElse Not isUpdate Then
                Dim logoPathValue As Object = DBNull.Value
                Dim destPath As String = Path.Combine(Connection.GetImagesFolder("company"), "logo.png")
                If IO.File.Exists(destPath) Then
                    logoPathValue = "logo.png"
                End If
                parameters.Add(New SqlParameter("@LogoPath", logoPathValue))
            End If

            Dim rowsAffected As Integer = Utilities.ExecuteNonQuery(sql, parameters.ToArray())

            If rowsAffected = 0 Then
                Throw New Exception("No rows were affected by the save operation. Please check the database connection.")
            End If

            Console.WriteLine($"CompanySettings saved successfully. Rows affected: {rowsAffected}")

        Catch ex As Exception
            Console.WriteLine($"SaveCompanySettings Error: {ex.ToString()}")
            Throw
        End Try
    End Sub
    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub CompanySettings_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        ' Check for unsaved changes
        If HasUnsavedChanges() AndAlso e.CloseReason = CloseReason.UserClosing AndAlso Me.DialogResult <> DialogResult.OK Then
            Dim result = MessageBox.Show("You have unsaved changes. Do you want to save before closing?",
                                       "Unsaved Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question)

            Select Case result
                Case DialogResult.Yes
                    btnSave.PerformClick()
                    If Me.DialogResult <> DialogResult.OK Then
                        e.Cancel = True ' Cancel closing if save failed
                    End If
                Case DialogResult.Cancel
                    e.Cancel = True
            End Select
        End If
    End Sub

    Private Function HasUnsavedChanges() As Boolean
        If originalValues Is Nothing Then Return False ' No original data to compare

        ' Check if any field has changed from original values
        Return txtCompanyName.Text <> originalValues("CompanyName") OrElse
               txtTIN.Text <> originalValues("TIN") OrElse
               txtAddress.Text <> originalValues("Address") OrElse
               txtPhone.Text <> originalValues("Phone") OrElse
               txtEmail.Text <> originalValues("Email") OrElse
               txtWebsite.Text <> originalValues("Website") OrElse
               txtBIRAuth.Text <> originalValues("BIRAuthNumber") OrElse
               txtPTUNumber.Text <> originalValues("PTUNumber") OrElse
               nudValidityYears.Value.ToString() <> originalValues("ValidityYears") OrElse
               txtReceiptFooter.Text <> originalValues("ReceiptFooter") OrElse
               txtCompanyHours.Text <> originalValues("CompanyHours") OrElse
               logoChanged
    End Function

    Private Sub MainPanel_Paint(sender As Object, e As PaintEventArgs) Handles MainPanel.Paint

    End Sub
End Class