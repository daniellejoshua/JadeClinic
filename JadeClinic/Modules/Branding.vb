Imports System.Drawing
Imports System.Drawing.Drawing2D

''' <summary>
''' Hardcoded clinic branding values.
'''
''' This replaces the former CompanySettingsManager. The Company Settings
''' feature was removed; these values are now fixed in code and cannot be
''' edited by users at runtime.
''' </summary>
Public Class Branding

    Private Shared _instance As Branding

    Public Shared ReadOnly Property Instance As Branding
        Get
            If _instance Is Nothing Then
                _instance = New Branding()
            End If
            Return _instance
        End Get
    End Property

    Private Sub New()
    End Sub

    ' Canonical hardcoded company values.
    Private Shared ReadOnly Defaults As New Dictionary(Of String, Object)(StringComparer.OrdinalIgnoreCase) From {
        {"CompanyName", "JADE CLINIC"},
        {"TIN", "123-456-789-000"},
        {"Address", "123 Medical Plaza, Makati City, Philippines"},
        {"Phone", "(02) 8123-4567"},
        {"Email", "admin@jadeclinic.com"},
        {"Website", "www.jadeclinic.com"},
        {"BIRAuthNumber", "ATP-2024-000001"},
        {"PTUNumber", "PTU-2024-001"},
        {"ValidityYears", 5},
        {"ReceiptFooter", "Thank you for your business!" & vbCrLf & "Have a great day!"},
        {"CompanyHours", "Mon-Fri: 9:00 AM - 5:00 PM" & vbCrLf & "Sat: 9:00 AM - 1:00 PM" & vbCrLf & "Sun: Closed"}
    }

    Public Function GetSetting(settingKey As String, Optional defaultValue As Object = Nothing) As Object
        If settingKey IsNot Nothing AndAlso Defaults.ContainsKey(settingKey) Then
            Return Defaults(settingKey)
        End If
        Return defaultValue
    End Function

    Public Function GetSettingString(settingKey As String, Optional defaultValue As String = "") As String
        Dim value = GetSetting(settingKey, defaultValue)
        Return If(value?.ToString(), defaultValue)
    End Function

    Public Function GetColor(colorKey As String, Optional defaultColor As Color = Nothing) As Color
        Return If(defaultColor = Nothing OrElse defaultColor = Color.Empty, GetDefaultColor(colorKey), defaultColor)
    End Function

    Private Function GetDefaultColor(colorKey As String) As Color
        Select Case If(colorKey, "").ToLower()
            Case "primarycolor"
                Return Color.FromArgb(254, 191, 16)      ' Golden Yellow
            Case "secondarycolor"
                Return Color.FromArgb(190, 154, 48)      ' Rich Olive
            Case "backgrounddark"
                Return Color.FromArgb(26, 29, 31)        ' Deep Charcoal
            Case "backgroundmid"
                Return Color.FromArgb(43, 47, 50)        ' Dark Slate
            Case "backgroundlight"
                Return Color.FromArgb(61, 65, 69)        ' Graphite
            Case "interactivecolor"
                Return Color.FromArgb(74, 79, 84)        ' Steel Gray
            Case "textprimary"
                Return Color.FromArgb(255, 255, 255)     ' Pure White
            Case "textsecondary"
                Return Color.FromArgb(225, 229, 233)     ' Light Silver
            Case "successcolor"
                Return Color.FromArgb(16, 216, 98)       ' Success Green
            Case "errorcolor"
                Return Color.FromArgb(255, 71, 87)       ' Alert Red
            Case Else
                Return Color.Black
        End Select
    End Function

    Public Function GetCompanyLogo() As Image
        Return CreateDefaultJadeLogo()
    End Function

    Public Function GetReceiptHeader() As String
        Dim header As New System.Text.StringBuilder()

        header.AppendLine("================================================")
        header.AppendLine($"                {GetSettingString("CompanyName", "JADE CLINIC")}")
        header.AppendLine("        Dental Supply Management")
        header.AppendLine($"        TIN: {GetSettingString("TIN", "123-456-789-000")} (VAT)")
        header.AppendLine($"        Tel: {GetSettingString("Phone", "(02) 8123-4567")}")

        Dim address = GetSettingString("Address")
        If Not String.IsNullOrWhiteSpace(address) Then
            header.AppendLine($"        {address}")
        End If

        Dim website = GetSettingString("Website")
        If Not String.IsNullOrWhiteSpace(website) Then
            header.AppendLine($"        {website}")
        End If

        header.AppendLine("================================================")

        Return header.ToString()
    End Function

    Public Function GetReceiptBIRFooter() As String
        Dim footer As New System.Text.StringBuilder()

        footer.AppendLine($"BIR Authority to Print No.: {GetSettingString("BIRAuthNumber", "ATP-2024-000001")}")
        footer.AppendLine($"PTU No.: {GetSettingString("PTUNumber", "PTU-2024-001")}")

        Dim validityYears = GetSetting("ValidityYears", 5)
        footer.AppendLine($"""This Invoice is valid for {validityYears} years from ATP date.""")

        Return footer.ToString()
    End Function

    Public Function GetReceiptFooter() As String
        Return GetSettingString("ReceiptFooter", "Thank you for your business!" & vbCrLf & "Have a great day!")
    End Function

    Public Sub RefreshCache()
        ' No cache to refresh - values are hardcoded.
    End Sub

    Public Sub ClearColorCache()
        ' No cache to clear - colors are hardcoded.
    End Sub

    Public Function IsConfigured() As Boolean
        Return True
    End Function

    Public Sub DebugColorLoading()
        ' No-op: colors are hardcoded.
    End Sub

    Public Sub DebugColorLoadingWithMessageBox()
        ' No-op: colors are hardcoded.
    End Sub

    Private Function CreateDefaultJadeLogo() As Image
        Try
            If My.Resources.Jade_Dental_Logo IsNot Nothing Then
                Return New Bitmap(My.Resources.Jade_Dental_Logo)
            End If
        Catch ex As Exception
            Console.WriteLine($"Could not load Jade_Dental_Logo resource: {ex.Message}")
        End Try

        Dim logo As New Bitmap(200, 150)
        Using g As Graphics = Graphics.FromImage(logo)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias

            Using brush As New LinearGradientBrush(
                New Rectangle(0, 0, 200, 150),
                Color.FromArgb(254, 191, 16),
                Color.FromArgb(190, 154, 48),
                LinearGradientMode.Vertical)

                g.FillRectangle(brush, 0, 0, 200, 150)
            End Using

            Using titleFont As New Font("Poppins", 20, FontStyle.Bold)
                Using titleBrush As New SolidBrush(Color.FromArgb(26, 29, 31))
                    Dim titleSize = g.MeasureString("JADE", titleFont)
                    g.DrawString("JADE", titleFont, titleBrush,
                        (200 - titleSize.Width) / 2, 15)
                End Using
            End Using

            Using subtitleFont As New Font("Poppins", 14, FontStyle.Regular)
                Using subtitleBrush As New SolidBrush(Color.FromArgb(26, 29, 31))
                    Dim subtitleSize = g.MeasureString("CLINIC", subtitleFont)
                    g.DrawString("CLINIC", subtitleFont, subtitleBrush,
                        (200 - subtitleSize.Width) / 2, 45)
                End Using
            End Using

            Using borderPen As New Pen(Color.FromArgb(26, 29, 31), 2)
                g.DrawRectangle(borderPen, 1, 1, 198, 148)
            End Using
        End Using

        Return logo
    End Function

End Class
