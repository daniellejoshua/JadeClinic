Imports System.Drawing
Imports System.Windows.Forms
Imports Guna.UI2.WinForms
Imports Microsoft.Data.SqlClient

Public Class ColorCustomization
    Inherits Form

    Private currentColorData As Dictionary(Of String, Object) = Nothing
    Private colorChanged As Boolean = False

    ' Current theme colors (will be loaded from database or defaults)
    Private primaryColor As Color = Color.FromArgb(254, 191, 16)        ' Golden Yellow
    Private secondaryColor As Color = Color.FromArgb(190, 154, 48)      ' Rich Olive
    Private backgroundDarkColor As Color = Color.FromArgb(26, 29, 31)   ' Deep Charcoal
    Private backgroundMidColor As Color = Color.FromArgb(43, 47, 50)    ' Dark Slate
    Private backgroundLightColor As Color = Color.FromArgb(61, 65, 69)  ' Graphite
    Private interactiveColor As Color = Color.FromArgb(74, 79, 84)      ' Steel Gray
    Private textPrimaryColor As Color = Color.FromArgb(255, 255, 255)   ' Pure White
    Private textSecondaryColor As Color = Color.FromArgb(225, 229, 233) ' Light Silver
    Private successColor As Color = Color.FromArgb(16, 216, 98)         ' Success Green
    Private errorColor As Color = Color.FromArgb(255, 71, 87)           ' Alert Red

    ' Dental Clinic Color Palette Constants (for UI of this form)
    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)
    Private ReadOnly RichOlive As Color = Color.FromArgb(190, 154, 48)
    Private ReadOnly DeepCharcoal As Color = Color.FromArgb(26, 29, 31)
    Private ReadOnly DarkSlate As Color = Color.FromArgb(43, 47, 50)
    Private ReadOnly Graphite As Color = Color.FromArgb(61, 65, 69)
    Private ReadOnly SteelGray As Color = Color.FromArgb(74, 79, 84)
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)
    Private ReadOnly LightSilver As Color = Color.FromArgb(225, 229, 233)
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 216, 98)
    Private ReadOnly AlertRed As Color = Color.FromArgb(255, 71, 87)

    Private Sub ColorCustomization_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Initialize form
        InitializeForm()

        ' Load current color settings
        LoadColorSettings()

        ' Initialize UI
        InitializeUI()
    End Sub

    Private Sub InitializeForm()
        ' Set form properties
        Me.Text = "Color Customization - Jade Clinic"
        Me.Size = New Size(800, 650)
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.BackColor = DarkSlate
        Me.ShowInTaskbar = False

        ' Create main panel
        Dim mainPanel As New Panel()
        mainPanel.Dock = DockStyle.Fill
        mainPanel.BackColor = Color.Transparent
        mainPanel.Padding = New Padding(20)
        Me.Controls.Add(mainPanel)

        ' Create title
        Dim lblTitle As New Label()
        lblTitle.Text = "🎨 COLOR CUSTOMIZATION"
        lblTitle.Font = New Font("Poppins", 18, FontStyle.Bold)
        lblTitle.ForeColor = PureWhite
        lblTitle.Location = New Point(0, 0)
        lblTitle.Size = New Size(760, 40)
        lblTitle.TextAlign = ContentAlignment.MiddleCenter
        mainPanel.Controls.Add(lblTitle)

        ' Create subtitle
        Dim lblSubtitle As New Label()
        lblSubtitle.Text = "Customize the application's color scheme and theme"
        lblSubtitle.Font = New Font("Poppins", 11, FontStyle.Regular)
        lblSubtitle.ForeColor = LightSilver
        lblSubtitle.Location = New Point(0, 45)
        lblSubtitle.Size = New Size(760, 25)
        lblSubtitle.TextAlign = ContentAlignment.MiddleCenter
        mainPanel.Controls.Add(lblSubtitle)

        ' Create separator
        Dim separator As New Panel()
        separator.BackColor = RichOlive
        separator.Size = New Size(720, 2)
        separator.Location = New Point(20, 80)
        mainPanel.Controls.Add(separator)

        ' Create color sections
        CreateColorSections(mainPanel)

        ' Create action buttons
        CreateActionButtons(mainPanel)
    End Sub

    Private Sub CreateColorSections(parentPanel As Panel)
        Dim currentY As Integer = 100
        Dim sectionHeight As Integer = 80
        Dim spacing As Integer = 20

        ' Primary Colors Section
        CreateColorSection(parentPanel, "Primary Colors", currentY, {
            New ColorOption("Primary Brand", primaryColor, Sub(c) primaryColor = c),
            New ColorOption("Secondary Accent", secondaryColor, Sub(c) secondaryColor = c)
        })
        currentY += sectionHeight + spacing

        ' Background Colors Section
        CreateColorSection(parentPanel, "Background Colors", currentY, {
            New ColorOption("Dark Background", backgroundDarkColor, Sub(c) backgroundDarkColor = c),
            New ColorOption("Mid Background", backgroundMidColor, Sub(c) backgroundMidColor = c),
            New ColorOption("Light Background", backgroundLightColor, Sub(c) backgroundLightColor = c)
        })
        currentY += sectionHeight + spacing

        ' Interactive Colors Section
        CreateColorSection(parentPanel, "Interactive Colors", currentY, {
            New ColorOption("Interactive Elements", interactiveColor, Sub(c) interactiveColor = c),
            New ColorOption("Success Color", successColor, Sub(c) successColor = c),
            New ColorOption("Error Color", errorColor, Sub(c) errorColor = c)
        })
        currentY += sectionHeight + spacing

        ' Text Colors Section
        CreateColorSection(parentPanel, "Text Colors", currentY, {
            New ColorOption("Primary Text", textPrimaryColor, Sub(c) textPrimaryColor = c),
            New ColorOption("Secondary Text", textSecondaryColor, Sub(c) textSecondaryColor = c)
        })
    End Sub

    Private Sub CreateColorSection(parentPanel As Panel, sectionTitle As String, yPosition As Integer, colorOptions As ColorOption())
        ' Section title
        Dim lblSection As New Label()
        lblSection.Text = sectionTitle
        lblSection.Font = New Font("Poppins", 12, FontStyle.Bold)
        lblSection.ForeColor = GoldenYellow
        lblSection.Location = New Point(0, yPosition)
        lblSection.Size = New Size(200, 25)
        parentPanel.Controls.Add(lblSection)

        ' Color options
        Dim startX As Integer = 20
        Dim optionWidth As Integer = 200
        Dim optionSpacing As Integer = 20

        For i = 0 To colorOptions.Length - 1
            Dim colorOption = colorOptions(i)
            Dim xPos = startX + i * (optionWidth + optionSpacing)

            CreateColorPicker(parentPanel, colorOption.Name, colorOption.CurrentColor, colorOption.UpdateAction, xPos, yPosition + 30)
        Next
    End Sub

    Private Sub CreateColorPicker(parentPanel As Panel, colorName As String, currentColor As Color, updateAction As Action(Of Color), x As Integer, y As Integer)
        ' Color name label
        Dim lblName As New Label()
        lblName.Text = colorName
        lblName.Font = New Font("Poppins", 10, FontStyle.Regular)
        lblName.ForeColor = LightSilver
        lblName.Location = New Point(x, y)
        lblName.Size = New Size(180, 20)
        parentPanel.Controls.Add(lblName)

        ' Color preview button
        Dim btnColor As New Guna.UI2.WinForms.Guna2Button()
        btnColor.Size = New Size(40, 30)
        btnColor.Location = New Point(x, y + 22)
        btnColor.FillColor = currentColor
        btnColor.BorderRadius = 5
        btnColor.BorderThickness = 1
        btnColor.BorderColor = PureWhite
        btnColor.Text = ""
        btnColor.Cursor = Cursors.Hand

        ' Color hex label
        Dim lblHex As New Label()
        lblHex.Text = $"#{currentColor.R:X2}{currentColor.G:X2}{currentColor.B:X2}"
        lblHex.Font = New Font("Consolas", 9, FontStyle.Regular)
        lblHex.ForeColor = LightSilver
        lblHex.Location = New Point(x + 50, y + 25)
        lblHex.Size = New Size(80, 20)
        parentPanel.Controls.Add(lblHex)

        ' Click handler for color picker
        AddHandler btnColor.Click, Sub()
                                       Dim colorDialog As New ColorDialog()
                                       colorDialog.Color = currentColor
                                       colorDialog.FullOpen = True

                                       If colorDialog.ShowDialog() = DialogResult.OK Then
                                           Dim newColor = colorDialog.Color
                                           btnColor.FillColor = newColor
                                           lblHex.Text = $"#{newColor.R:X2}{newColor.G:X2}{newColor.B:X2}"
                                           updateAction(newColor)
                                           colorChanged = True
                                       End If
                                   End Sub

        ' Hover effects
        AddHandler btnColor.MouseEnter, Sub() btnColor.BorderThickness = 2
        AddHandler btnColor.MouseLeave, Sub() btnColor.BorderThickness = 1

        parentPanel.Controls.Add(btnColor)
    End Sub

    Private Sub CreateActionButtons(parentPanel As Panel)
        Dim buttonY As Integer = 520
        Dim buttonSpacing As Integer = 20

        ' Reset to Default button
        Dim btnReset As New Guna.UI2.WinForms.Guna2Button()
        btnReset.Text = "🔄 Reset to Default"
        btnReset.Size = New Size(150, 40)
        btnReset.Location = New Point(150, buttonY)
        btnReset.BorderRadius = 8
        btnReset.FillColor = SteelGray
        btnReset.Font = New Font("Poppins", 10, FontStyle.Regular)
        btnReset.ForeColor = PureWhite
        AddHandler btnReset.Click, AddressOf BtnReset_Click
        AddHandler btnReset.MouseEnter, Sub() btnReset.FillColor = Graphite
        AddHandler btnReset.MouseLeave, Sub() btnReset.FillColor = SteelGray
        parentPanel.Controls.Add(btnReset)

        ' Preview button
        Dim btnPreview As New Guna.UI2.WinForms.Guna2Button()
        btnPreview.Text = "👁️ Preview"
        btnPreview.Size = New Size(120, 40)
        btnPreview.Location = New Point(320, buttonY)
        btnPreview.BorderRadius = 8
        btnPreview.FillColor = RichOlive
        btnPreview.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnPreview.ForeColor = DeepCharcoal
        AddHandler btnPreview.Click, AddressOf BtnPreview_Click
        AddHandler btnPreview.MouseEnter, Sub() btnPreview.FillColor = GoldenYellow
        AddHandler btnPreview.MouseLeave, Sub() btnPreview.FillColor = RichOlive
        parentPanel.Controls.Add(btnPreview)

        ' Save button
        Dim btnSave As New Guna.UI2.WinForms.Guna2Button()
        btnSave.Text = "💾 Save Changes"
        btnSave.Size = New Size(140, 40)
        btnSave.Location = New Point(460, buttonY)
        btnSave.BorderRadius = 8
        btnSave.FillColor = SuccessGreen
        btnSave.Font = New Font("Poppins", 10, FontStyle.Bold)
        btnSave.ForeColor = PureWhite
        AddHandler btnSave.Click, AddressOf BtnSave_Click
        AddHandler btnSave.MouseEnter, Sub() btnSave.FillColor = Color.FromArgb(12, 190, 85)
        AddHandler btnSave.MouseLeave, Sub() btnSave.FillColor = SuccessGreen
        parentPanel.Controls.Add(btnSave)

        ' Cancel button
        Dim btnCancel As New Guna.UI2.WinForms.Guna2Button()
        btnCancel.Text = "❌ Cancel"
        btnCancel.Size = New Size(100, 40)
        btnCancel.Location = New Point(620, buttonY)
        btnCancel.BorderRadius = 8
        btnCancel.FillColor = AlertRed
        btnCancel.Font = New Font("Poppins", 10, FontStyle.Regular)
        btnCancel.ForeColor = PureWhite
        AddHandler btnCancel.Click, AddressOf BtnCancel_Click
        AddHandler btnCancel.MouseEnter, Sub() btnCancel.FillColor = Color.FromArgb(220, 60, 75)
        AddHandler btnCancel.MouseLeave, Sub() btnCancel.FillColor = AlertRed
        parentPanel.Controls.Add(btnCancel)
    End Sub

    Private Sub LoadColorSettings()
        Try
            Dim query As String = "SELECT * FROM ColorSettings WHERE IsActive = 1 ORDER BY DateCreated DESC"
            Using reader As SqlDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                If reader.Read() Then
                    ' Store current data
                    currentColorData = New Dictionary(Of String, Object)
                    For i = 0 To reader.FieldCount - 1
                        currentColorData(reader.GetName(i)) = If(reader.IsDBNull(i), Nothing, reader.GetValue(i))
                    Next

                    ' Load colors from database - Fixed: Convert to string first
                    primaryColor = ColorFromString(GetColorSettingString("PrimaryColor", ColorToString(primaryColor)))
                    secondaryColor = ColorFromString(GetColorSettingString("SecondaryColor", ColorToString(secondaryColor)))
                    backgroundDarkColor = ColorFromString(GetColorSettingString("BackgroundDark", ColorToString(backgroundDarkColor)))
                    backgroundMidColor = ColorFromString(GetColorSettingString("BackgroundMid", ColorToString(backgroundMidColor)))
                    backgroundLightColor = ColorFromString(GetColorSettingString("BackgroundLight", ColorToString(backgroundLightColor)))
                    interactiveColor = ColorFromString(GetColorSettingString("InteractiveColor", ColorToString(interactiveColor)))
                    textPrimaryColor = ColorFromString(GetColorSettingString("TextPrimary", ColorToString(textPrimaryColor)))
                    textSecondaryColor = ColorFromString(GetColorSettingString("TextSecondary", ColorToString(textSecondaryColor)))
                    successColor = ColorFromString(GetColorSettingString("SuccessColor", ColorToString(successColor)))
                    errorColor = ColorFromString(GetColorSettingString("ErrorColor", ColorToString(errorColor)))
                End If
            End Using
        Catch ex As Exception
            ' Use default colors if loading fails
            Console.WriteLine($"Error loading color settings: {ex.Message}")
        End Try
    End Sub

    Private Function GetColorSettingString(key As String, defaultValue As String) As String
        If currentColorData IsNot Nothing AndAlso currentColorData.ContainsKey(key) AndAlso currentColorData(key) IsNot Nothing Then
            Return currentColorData(key).ToString()
        End If
        Return defaultValue
    End Function

    Private Function ColorFromString(colorString As String) As Color
        Try
            If String.IsNullOrEmpty(colorString) Then Return Color.Black

            If colorString.StartsWith("#") Then
                Return ColorTranslator.FromHtml(colorString)
            Else
                ' Assume it's in format "R,G,B"
                Dim parts = colorString.Split(","c)
                If parts.Length = 3 Then
                    Return Color.FromArgb(Integer.Parse(parts(0)), Integer.Parse(parts(1)), Integer.Parse(parts(2)))
                End If
            End If
        Catch
            ' Return default if parsing fails
        End Try
        Return Color.Black
    End Function

    Private Function ColorToString(color As Color) As String
        Return $"#{color.R:X2}{color.G:X2}{color.B:X2}"
    End Function

    Private Sub BtnReset_Click(sender As Object, e As EventArgs)
        ' Reset to default Jade Clinic colors
        primaryColor = Color.FromArgb(254, 191, 16)        ' Golden Yellow
        secondaryColor = Color.FromArgb(190, 154, 48)      ' Rich Olive
        backgroundDarkColor = Color.FromArgb(26, 29, 31)   ' Deep Charcoal
        backgroundMidColor = Color.FromArgb(43, 47, 50)    ' Dark Slate
        backgroundLightColor = Color.FromArgb(61, 65, 69)  ' Graphite
        interactiveColor = Color.FromArgb(74, 79, 84)      ' Steel Gray
        textPrimaryColor = Color.FromArgb(255, 255, 255)   ' Pure White
        textSecondaryColor = Color.FromArgb(225, 229, 233) ' Light Silver
        successColor = Color.FromArgb(16, 216, 98)         ' Success Green
        errorColor = Color.FromArgb(255, 71, 87)           ' Alert Red

        colorChanged = True
        MessageBox.Show("Colors reset to default Jade Clinic theme!", "Colors Reset", MessageBoxButtons.OK, MessageBoxIcon.Information)

        ' Refresh the form
        Me.Controls.Clear()
        InitializeForm()
    End Sub

    Private Sub BtnPreview_Click(sender As Object, e As EventArgs)
        MessageBox.Show("Color preview functionality will be implemented to show how forms will look with the new colors.", "Preview Coming Soon", MessageBoxButtons.OK, MessageBoxIcon.Information)
    End Sub

    Private Sub BtnSave_Click(sender As Object, e As EventArgs)
        Try
            Console.WriteLine("=== SAVING COLOR SETTINGS ===")
            SaveColorSettings()

            ' 🔥 CRITICAL: Clear the cache
            CompanySettingsManager.Instance.RefreshCache()
            Console.WriteLine("Cache refreshed after save")

            MessageBox.Show("Color settings saved successfully!" & vbCrLf & vbCrLf &
                       "Changes will be applied when you restart the application.",
                       "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information)

            ' Log the action
            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Color Settings Updated", "Application color scheme changed")

            Me.DialogResult = DialogResult.OK
            Me.Close()
        Catch ex As Exception
            Console.WriteLine($"❌ Save error: {ex.ToString()}")
            MessageBox.Show($"Error saving color settings: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub SaveColorSettings()
        Try
            MessageBox.Show("=== SAVING COLOR SETTINGS ===")

            ' Check if we need to create the table first
            EnsureColorSettingsTableExists()

            ' Check if an active record already exists
            Dim existingCount As Integer = 0
            Dim countQuery As String = "SELECT COUNT(*) FROM ColorSettings WHERE IsActive = 1"
            Dim countResult = Utilities.ExecuteScalar(countQuery, New SqlParameter() {})
            If countResult IsNot Nothing Then
                existingCount = Convert.ToInt32(countResult)
            End If

            MessageBox.Show($"Found {existingCount} active color records")

            Dim sql As String
            Dim parameters As New List(Of SqlParameter)()

            If existingCount > 0 Then
                ' UPDATE existing active record
                MessageBox.Show("Updating existing color record")
                sql = "UPDATE ColorSettings SET " &
                  "PrimaryColor = @PrimaryColor, " &
                  "SecondaryColor = @SecondaryColor, " &
                  "BackgroundDark = @BackgroundDark, " &
                  "BackgroundMid = @BackgroundMid, " &
                  "BackgroundLight = @BackgroundLight, " &
                  "InteractiveColor = @InteractiveColor, " &
                  "TextPrimary = @TextPrimary, " &
                  "TextSecondary = @TextSecondary, " &
                  "SuccessColor = @SuccessColor, " &
                  "ErrorColor = @ErrorColor, " &
                  "LastModified = @LastModified " &
                  "WHERE IsActive = 1"
            Else
                ' INSERT new record (first time only)
                MessageBox.Show("Inserting new color record")
                sql = "INSERT INTO ColorSettings (PrimaryColor, SecondaryColor, BackgroundDark, BackgroundMid, BackgroundLight, InteractiveColor, TextPrimary, TextSecondary, SuccessColor, ErrorColor, IsActive, DateCreated, LastModified) VALUES (@PrimaryColor, @SecondaryColor, @BackgroundDark, @BackgroundMid, @BackgroundLight, @InteractiveColor, @TextPrimary, @TextSecondary, @SuccessColor, @ErrorColor, 1, @DateCreated, @LastModified)"
                parameters.Add(New SqlParameter("@DateCreated", DateTime.Now))
            End If

            ' Add common parameters
            parameters.Add(New SqlParameter("@PrimaryColor", ColorToString(primaryColor)))
            parameters.Add(New SqlParameter("@SecondaryColor", ColorToString(secondaryColor)))
            parameters.Add(New SqlParameter("@BackgroundDark", ColorToString(backgroundDarkColor)))
            parameters.Add(New SqlParameter("@BackgroundMid", ColorToString(backgroundMidColor)))
            parameters.Add(New SqlParameter("@BackgroundLight", ColorToString(backgroundLightColor)))
            parameters.Add(New SqlParameter("@InteractiveColor", ColorToString(interactiveColor)))
            parameters.Add(New SqlParameter("@TextPrimary", ColorToString(textPrimaryColor)))
            parameters.Add(New SqlParameter("@TextSecondary", ColorToString(textSecondaryColor)))
            parameters.Add(New SqlParameter("@SuccessColor", ColorToString(successColor)))
            parameters.Add(New SqlParameter("@ErrorColor", ColorToString(errorColor)))
            parameters.Add(New SqlParameter("@LastModified", DateTime.Now))

            ' DEBUG: Show colors being saved
            MessageBox.Show($"Saving colors:{vbCrLf}" &
                       $"Primary: {ColorToString(primaryColor)}{vbCrLf}" &
                       $"Secondary: {ColorToString(secondaryColor)}{vbCrLf}" &
                       $"Background Dark: {ColorToString(backgroundDarkColor)}{vbCrLf}" &
                       $"Background Mid: {ColorToString(backgroundMidColor)}")

            Dim result = Utilities.ExecuteNonQuery(sql, parameters.ToArray())
            MessageBox.Show($"{If(existingCount > 0, "Updated", "Inserted")} {result} color record")

            If result = 0 Then
                Throw New Exception("No color settings were saved to the database")
            End If

            MessageBox.Show("--- SaveColorSettings Complete ---")

        Catch ex As Exception
            MessageBox.Show($"❌ SaveColorSettings error: {ex.Message}")
            Throw
        End Try
    End Sub
    Private Sub BtnCancel_Click(sender As Object, e As EventArgs)
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub



    Private Sub EnsureColorSettingsTableExists()
        Try
            Dim createTableSql As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ColorSettings' AND xtype='U') " &
                "CREATE TABLE ColorSettings(" &
                "SettingID int IDENTITY(1,1) PRIMARY KEY, " &
                "PrimaryColor nvarchar(20) NOT NULL, " &
                "SecondaryColor nvarchar(20) NOT NULL, " &
                "BackgroundDark nvarchar(20) NOT NULL, " &
                "BackgroundMid nvarchar(20) NOT NULL, " &
                "BackgroundLight nvarchar(20) NOT NULL, " &
                "InteractiveColor nvarchar(20) NOT NULL, " &
                "TextPrimary nvarchar(20) NOT NULL, " &
                "TextSecondary nvarchar(20) NOT NULL, " &
                "SuccessColor nvarchar(20) NOT NULL, " &
                "ErrorColor nvarchar(20) NOT NULL, " &
                "IsActive bit NOT NULL DEFAULT 1, " &
                "DateCreated datetime2 NOT NULL DEFAULT GETDATE(), " &
                "LastModified datetime2 NOT NULL DEFAULT GETDATE())"

            Utilities.ExecuteNonQuery(createTableSql, New SqlParameter() {})
        Catch ex As Exception
            Console.WriteLine($"Error creating ColorSettings table: {ex.Message}")
        End Try
    End Sub

    Private Sub InitializeUI()
        ' Add any additional UI initialization here
    End Sub

    ' Helper class for color options
    Public Class ColorOption
        Public Property Name As String
        Public Property CurrentColor As Color
        Public Property UpdateAction As Action(Of Color)

        Public Sub New(name As String, currentColor As Color, updateAction As Action(Of Color))
            Me.Name = name
            Me.CurrentColor = currentColor
            Me.UpdateAction = updateAction
        End Sub
    End Class
End Class