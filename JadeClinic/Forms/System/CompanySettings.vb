Imports System.Drawing
Imports System.IO
Imports System.Windows.Forms
Imports Guna.UI2.WinForms
Imports Microsoft.Data.SqlClient

Public Class CompanySettings
    Private currentCompanyData As Dictionary(Of String, Object) = Nothing
    Private logoChanged As Boolean = False
    Private logoData As Byte() = Nothing
    Private originalValues As Dictionary(Of String, String) = Nothing ' Track original values

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
        ' Load current company settings
        LoadCompanySettings()

        ' Initialize UI
        InitializeUI()

        ' Store original values for change detection
        StoreOriginalValues()
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
            {"ReceiptFooter", txtReceiptFooter.Text}
        }
    End Sub

    Private Sub InitializeUI()
        ' Set tab control style
        TabControl1.Appearance = TabAppearance.Buttons

        ' Add hover effects to buttons
        AddButtonHoverEffects()
    End Sub

    Private Sub AddButtonHoverEffects()
        ' Save button hover effect
        AddHandler btnSave.MouseEnter, Sub() btnSave.FillColor = Color.FromArgb(12, 190, 85)
        AddHandler btnSave.MouseLeave, Sub() btnSave.FillColor = SuccessGreen

        ' Cancel button hover effect
        AddHandler btnCancel.MouseEnter, Sub() btnCancel.FillColor = Color.FromArgb(220, 60, 75)
        AddHandler btnCancel.MouseLeave, Sub() btnCancel.FillColor = AlertRed

        ' Preview button hover effect
        AddHandler btnPreviewReceipt.MouseEnter, Sub() btnPreviewReceipt.FillColor = RichOlive
        AddHandler btnPreviewReceipt.MouseLeave, Sub() btnPreviewReceipt.FillColor = GoldenYellow

        ' Logo buttons hover effects
        AddHandler btnChangeLogo.MouseEnter, Sub() btnChangeLogo.FillColor = Color.FromArgb(12, 190, 85)
        AddHandler btnChangeLogo.MouseLeave, Sub() btnChangeLogo.FillColor = SuccessGreen

        AddHandler btnRemoveLogo.MouseEnter, Sub() btnRemoveLogo.FillColor = Color.FromArgb(220, 60, 75)
        AddHandler btnRemoveLogo.MouseLeave, Sub() btnRemoveLogo.FillColor = AlertRed
    End Sub

    Private Sub LoadCompanySettings()
        Try
            Dim query As String = "SELECT * FROM CompanySettings WHERE IsActive = 1 ORDER BY DateCreated DESC"
            Using reader As SqlDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
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

        ' Load logo if available
        If currentCompanyData.ContainsKey("Logo") AndAlso currentCompanyData("Logo") IsNot Nothing Then
            Try
                Dim logoBytes As Byte() = CType(currentCompanyData("Logo"), Byte())
                If logoBytes.Length > 0 Then
                    Using ms As New MemoryStream(logoBytes)
                        picLogo.Image = Image.FromStream(ms)
                    End Using
                End If
            Catch ex As Exception
                ' Ignore logo loading errors
            End Try
        Else
            ' Set default logo placeholder
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
        SetDefaultLogoPlaceholder()
    End Sub

    Private Sub SetDefaultLogoPlaceholder()
        ' Create a simple placeholder image
        Dim placeholder As New Bitmap(200, 150)
        Using g As Graphics = Graphics.FromImage(placeholder)
            g.FillRectangle(New SolidBrush(Color.LightGray), 0, 0, 200, 150)
            g.DrawString("Company Logo", New Font("Poppins", 12), New SolidBrush(Color.Gray), 50, 65)
        End Using
        picLogo.Image = placeholder
    End Sub

    Private Sub btnChangeLogo_Click(sender As Object, e As EventArgs) Handles btnChangeLogo.Click
        Try
            Dim openFileDialog As New OpenFileDialog()
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif|All Files|*.*"
            openFileDialog.Title = "Select Company Logo"

            If openFileDialog.ShowDialog() = DialogResult.OK Then
                ' Load and display the image
                Dim image As Image = Image.FromFile(openFileDialog.FileName)
                picLogo.Image = image

                ' Convert to byte array for database storage
                Using ms As New MemoryStream()
                    image.Save(ms, System.Drawing.Imaging.ImageFormat.Png)
                    logoData = ms.ToArray()
                End Using

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
            logoData = Nothing
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
        ' Create a preview form
        Dim previewForm As New Form()
        previewForm.Text = "Receipt Preview"
        previewForm.Size = New Size(400, 600)
        previewForm.StartPosition = FormStartPosition.CenterParent
        previewForm.FormBorderStyle = FormBorderStyle.FixedDialog
        previewForm.MaximizeBox = False
        previewForm.MinimizeBox = False
        previewForm.BackColor = Color.White

        ' Create a text box to show the receipt
        Dim txtPreview As New TextBox()
        txtPreview.Multiline = True
        txtPreview.ScrollBars = ScrollBars.Vertical
        txtPreview.Font = New Font("Courier New", 9)
        txtPreview.Dock = DockStyle.Fill
        txtPreview.ReadOnly = True

        ' Generate sample receipt content
        Dim receiptContent As String = GenerateSampleReceipt()
        txtPreview.Text = receiptContent

        previewForm.Controls.Add(txtPreview)
        previewForm.ShowDialog()
        previewForm.Dispose()
    End Sub

    Private Function GenerateSampleReceipt() As String
        Dim receipt As New System.Text.StringBuilder()

        receipt.AppendLine("================================================")
        receipt.AppendLine($"                {txtCompanyName.Text}")
        receipt.AppendLine("        Dental Supply Management")
        receipt.AppendLine($"        TIN: {txtTIN.Text} (VAT)")
        receipt.AppendLine($"        Tel: {txtPhone.Text}")
        receipt.AppendLine("================================================")
        receipt.AppendLine("")
        receipt.AppendLine($"SOLD TO: Sample Customer        TIN: N/A")
        receipt.AppendLine($"ADDRESS: Walk-in Customer")
        receipt.AppendLine($"DATE: {DateTime.Now:MM/dd/yyyy}        INVOICE #: 12345")
        receipt.AppendLine($"CASHIER: {frmLoginvb.LoggedInUsername}")
        receipt.AppendLine("")
        receipt.AppendLine("================================================")
        receipt.AppendLine("QTY | ITEM                    | PRICE   | AMOUNT")
        receipt.AppendLine("----|-------------------------|---------|--------")
        receipt.AppendLine("  1 | Sample Product A        | 100.00  | 100.00")
        receipt.AppendLine("  2 | Sample Product B        |  50.00  | 100.00")
        receipt.AppendLine("================================================")
        receipt.AppendLine("SUB-TOTAL (VAT Inclusive)               200.00")
        receipt.AppendLine("================================================")
        receipt.AppendLine("VATa Sales                             178.57")
        receipt.AppendLine("VAT (12%)                               21.43")
        receipt.AppendLine("")
        receipt.AppendLine("================================================")
        receipt.AppendLine("TOTAL AMOUNT DUE                       200.00")
        receipt.AppendLine("================================================")
        receipt.AppendLine("")
        receipt.AppendLine("PAYMENT INFORMATION:")
        receipt.AppendLine("Payment Method: Cash")
        receipt.AppendLine("Amount Received: ₱200.00")
        receipt.AppendLine("Change: ₱0.00")
        receipt.AppendLine("")
        receipt.AppendLine("**For SC/PWD (if applicable):**")
        receipt.AppendLine("SC/PWD ID: ____________  Discount: ₱____")
        receipt.AppendLine("Signature: ___________________")
        receipt.AppendLine("")
        receipt.AppendLine($"BIR Authority to Print No.: {txtBIRAuth.Text}")
        receipt.AppendLine($"PTU No.: {txtPTUNumber.Text}")
        receipt.AppendLine($"""This Invoice is valid for {nudValidityYears.Value} years from ATP date.""")
        receipt.AppendLine("")
        receipt.AppendLine("================================================")
        receipt.AppendLine(txtReceiptFooter.Text.Replace(vbCrLf, vbCrLf))
        receipt.AppendLine("")

        Return receipt.ToString()
    End Function

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Try
            ' Validate required fields
            If String.IsNullOrWhiteSpace(txtCompanyName.Text) Then
                MessageBox.Show("Company name is required.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                txtCompanyName.Focus()
                Return
            End If

            ' Save company settings
            SaveCompanySettings()

            ' CRITICAL FIX: Refresh the CompanySettingsManager cache
            CompanySettingsManager.Instance.RefreshCache()

            MessageBox.Show("Company settings saved successfully!", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)

            ' Log the action
            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Company Settings Updated", "Company settings configuration changed")

            ' Reset the changed flags
            logoChanged = False
            StoreOriginalValues() ' Update original values

            Me.DialogResult = DialogResult.OK
            Me.Close()

        Catch ex As Exception
            MessageBox.Show($"Error saving company settings: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            ' Log the error for debugging
            Console.WriteLine($"Save Error Details: {ex.ToString()}")
        End Try
    End Sub

    Private Sub SaveCompanySettings()
        Try
            ' ENHANCED: Check if we have existing settings more reliably
            Dim existingCount As Integer = 0
            Dim countQuery As String = "SELECT COUNT(*) FROM CompanySettings WHERE IsActive = 1"
            Dim countResult = Utilities.ExecuteScalar(countQuery, New SqlParameter() {})
            If countResult IsNot Nothing Then
                existingCount = Convert.ToInt32(countResult)
            End If

            Dim isUpdate As Boolean = (existingCount > 0)

            Dim sql As String
            Dim parameters As New List(Of SqlParameter)()

            If isUpdate Then
                ' Update existing record
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
                      "LastModified = @LastModified"

                If logoChanged Then
                    sql += ", Logo = @Logo"
                End If

                sql += " WHERE IsActive = 1"
            Else
                ' Insert new record
                sql = "INSERT INTO CompanySettings " &
                      "(CompanyName, TIN, Address, Phone, Email, Website, Logo, " &
                      "BIRAuthNumber, PTUNumber, ValidityYears, ReceiptFooter, " &
                      "IsActive, DateCreated, LastModified) " &
                      "VALUES " &
                      "(@CompanyName, @TIN, @Address, @Phone, @Email, @Website, @Logo, " &
                      "@BIRAuthNumber, @PTUNumber, @ValidityYears, @ReceiptFooter, " &
                      "1, @DateCreated, @LastModified)"

                parameters.Add(New SqlParameter("@DateCreated", DateTime.Now))
            End If

            ' Add common parameters
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
            parameters.Add(New SqlParameter("@LastModified", DateTime.Now))

            ' Handle logo data
            If logoChanged OrElse Not isUpdate Then
                If logoData IsNot Nothing Then
                    parameters.Add(New SqlParameter("@Logo", logoData))
                Else
                    parameters.Add(New SqlParameter("@Logo", DBNull.Value))
                End If
            End If

            ' Execute the query
            Dim rowsAffected As Integer = Utilities.ExecuteNonQuery(sql, parameters.ToArray())

            If rowsAffected = 0 Then
                Throw New Exception("No rows were affected by the save operation. Please check the database connection.")
            End If

            ' DEBUG: Log successful save
            Console.WriteLine($"CompanySettings saved successfully. Rows affected: {rowsAffected}")

        Catch ex As Exception
            Console.WriteLine($"SaveCompanySettings Error: {ex.ToString()}")
            Throw ' Re-throw to be handled by calling method
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
               logoChanged
    End Function
End Class