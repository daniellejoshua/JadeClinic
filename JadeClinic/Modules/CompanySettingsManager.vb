Imports System.Data.Common
Imports System.IO
Imports System.Drawing

''' <summary>
''' Utility class for managing company settings throughout the application
''' </summary>
Public Class CompanySettingsManager
    Private Shared _instance As CompanySettingsManager
    Private _cachedSettings As Dictionary(Of String, Object) = Nothing
    Private _cachedColors As Dictionary(Of String, Color) = Nothing
    Private _lastCacheUpdate As DateTime = DateTime.MinValue
    Private _lastColorCacheUpdate As DateTime = DateTime.MinValue
    Private ReadOnly _cacheExpiryMinutes As Integer = 30

    Public Shared ReadOnly Property Instance As CompanySettingsManager
        Get
            If _instance Is Nothing Then
                _instance = New CompanySettingsManager()
            End If
            Return _instance
        End Get
    End Property

    Private Sub New()
        ' Private constructor for singleton pattern
    End Sub

    ''' <summary>
    ''' Get company setting value with caching
    ''' </summary>
    Public Function GetSetting(settingKey As String, Optional defaultValue As Object = Nothing) As Object
        Try
            RefreshCacheIfNeeded()
            
            If _cachedSettings IsNot Nothing AndAlso _cachedSettings.ContainsKey(settingKey) Then
                Return _cachedSettings(settingKey)
            End If
            
            Return defaultValue
        Catch ex As Exception
            Console.WriteLine($"Error getting company setting '{settingKey}': {ex.Message}")
            Return defaultValue
        End Try
    End Function

    ''' <summary>
    ''' Get company setting as string
    ''' </summary>
    Public Function GetSettingString(settingKey As String, Optional defaultValue As String = "") As String
        Dim value = GetSetting(settingKey, defaultValue)
        Return If(value?.ToString(), defaultValue)
    End Function

    ''' <summary>
    ''' Get color setting with caching and default fallback
    ''' </summary>
    Public Function GetColor(colorKey As String, Optional defaultColor As Color = Nothing) As Color
        Try
            RefreshColorCacheIfNeeded()
            
            If _cachedColors IsNot Nothing AndAlso _cachedColors.ContainsKey(colorKey) Then
                Return _cachedColors(colorKey)
            End If
            
            Return If(defaultColor = Nothing, GetDefaultColor(colorKey), defaultColor)
        Catch ex As Exception
            Console.WriteLine($"Error getting color setting '{colorKey}': {ex.Message}")
            Return If(defaultColor = Nothing, GetDefaultColor(colorKey), defaultColor)
        End Try
    End Function

    ''' <summary>
    ''' Get default Jade Clinic color for a specific key
    ''' </summary>
    Private Function GetDefaultColor(colorKey As String) As Color
        Select Case colorKey.ToLower()
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

    ''' <summary>
    ''' Convert color string to Color object
    ''' </summary>
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

    ''' <summary>
    ''' Get company logo as Image - ENHANCED to use Jade Dental Logo resource as fallback
    ''' </summary>
    Public Function GetCompanyLogo() As Image
        Try
            Dim logoData = GetSetting("Logo")
            If logoData IsNot Nothing AndAlso TypeOf logoData Is Byte() Then
                Dim logoBytes As Byte() = CType(logoData, Byte())
                If logoBytes.Length > 0 Then
                    Using ms As New MemoryStream(logoBytes)
                        Return Image.FromStream(ms)
                    End Using
                End If
            End If
        Catch ex As Exception
            Console.WriteLine($"Error loading company logo: {ex.Message}")
        End Try
        
        ' Return Jade Dental Logo from resources as default fallback
        Return CreateDefaultJadeLogo()
    End Function

    ''' <summary>
    ''' Get formatted company header for receipts - ENHANCED with logo integration
    ''' </summary>
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

    ''' <summary>
    ''' Get BIR compliance footer for receipts
    ''' </summary>
    Public Function GetReceiptBIRFooter() As String
        Dim footer As New System.Text.StringBuilder()
        
        footer.AppendLine($"BIR Authority to Print No.: {GetSettingString("BIRAuthNumber", "ATP-2024-000001")}")
        footer.AppendLine($"PTU No.: {GetSettingString("PTUNumber", "PTU-2024-001")}")
        
        Dim validityYears = GetSetting("ValidityYears", 5)
        footer.AppendLine($"""This Invoice is valid for {validityYears} years from ATP date.""")
        
        Return footer.ToString()
    End Function

    ''' <summary>
    ''' Get receipt footer message
    ''' </summary>
    Public Function GetReceiptFooter() As String
        Return GetSettingString("ReceiptFooter", "Thank you for your business!" & vbCrLf & "Have a great day!")
    End Function

    ''' <summary>
    ''' Force refresh of cached settings and colors
    ''' </summary>
    Public Sub RefreshCache()
        _cachedSettings = Nothing
        _cachedColors = Nothing
        _lastCacheUpdate = DateTime.MinValue
        _lastColorCacheUpdate = DateTime.MinValue
        RefreshCacheIfNeeded()
        RefreshColorCacheIfNeeded()
    End Sub

    ''' <summary>
    ''' Clear only the color cache to force reload of color settings
    ''' </summary>
    Public Sub ClearColorCache()
        _cachedColors = Nothing
        _lastColorCacheUpdate = DateTime.MinValue
        Console.WriteLine("Color cache cleared - colors will reload on next access")
    End Sub

    ''' <summary>
    ''' Check if settings have been configured
    ''' </summary>
    Public Function IsConfigured() As Boolean
        Try
            RefreshCacheIfNeeded()
            Return _cachedSettings IsNot Nothing AndAlso _cachedSettings.Count > 0
        Catch
            Return False
        End Try
    End Function

    Private Sub RefreshCacheIfNeeded()
        If _cachedSettings Is Nothing OrElse DateTime.Now.Subtract(_lastCacheUpdate).TotalMinutes > _cacheExpiryMinutes Then
            LoadSettingsFromDatabase()
        End If
    End Sub

    Private Sub RefreshColorCacheIfNeeded()
        If _cachedColors Is Nothing OrElse DateTime.Now.Subtract(_lastColorCacheUpdate).TotalMinutes > _cacheExpiryMinutes Then
            LoadColorsFromDatabase()
        End If
    End Sub

    Private Sub LoadSettingsFromDatabase()
        Try
            _cachedSettings = New Dictionary(Of String, Object)()
            
            Dim query As String = "SELECT * FROM CompanySettings WHERE IsActive = 1 ORDER BY DateCreated DESC"
            Using reader As DbDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                If reader.Read() Then
                    For i = 0 To reader.FieldCount - 1
                        Dim fieldName = reader.GetName(i)
                        Dim fieldValue = If(reader.IsDBNull(i), Nothing, reader.GetValue(i))
                        _cachedSettings(fieldName) = fieldValue
                    Next
                End If
            End Using
            
            _lastCacheUpdate = DateTime.Now
            
        Catch ex As Exception
            Console.WriteLine($"Error loading company settings from database: {ex.Message}")
            ' Don't throw here to allow application to continue with defaults
        End Try
    End Sub

    Private Sub LoadColorsFromDatabase()
        Try
            Console.WriteLine("?? Loading colors from database...")



            _cachedColors = New Dictionary(Of String, Color)()

            Dim query As String = "SELECT * FROM ColorSettings WHERE IsActive = 1 ORDER BY DateCreated DESC"
            Using reader As DbDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                If reader.Read() Then
                    Console.WriteLine("? Found active ColorSettings record")
                    For i = 0 To reader.FieldCount - 1
                        Dim fieldName = reader.GetName(i)
                        If Not reader.IsDBNull(i) AndAlso fieldName.ToLower().Contains("color") Then
                            Dim colorString = reader.GetValue(i).ToString()
                            _cachedColors(fieldName) = ColorFromString(colorString)
                            Console.WriteLine($"  Loaded {fieldName}: {colorString} -> {ColorFromString(colorString)}")
                        End If
                    Next
                    Console.WriteLine($"? Loaded {_cachedColors.Count} colors from database")
                Else
                    Console.WriteLine("? No active ColorSettings found - using defaults")
                End If
            End Using

            _lastColorCacheUpdate = DateTime.Now

        Catch ex As Exception
            Console.WriteLine($"? Error loading color settings from database: {ex.Message}")
            Console.WriteLine($"? Full exception: {ex.ToString()}")

            ' ?? IMPORTANT: Don't override saved colors, just use what we have
            ' If there's an error, still try to load defaults but don't clear existing cache
            If _cachedColors Is Nothing OrElse _cachedColors.Count = 0 Then
                Console.WriteLine("??  No cached colors available, loading defaults as fallback")
            Else
                Console.WriteLine("??  Keeping existing cached colors despite database error")
            End If

            _lastColorCacheUpdate = DateTime.Now
        End Try
    End Sub
    ''' <summary>
    ''' Debug method to see what's happening with color loading
    ''' </summary>
    Public Sub DebugColorLoading()
        Console.WriteLine("=== COMPANYSETTINGSMANAGER DEBUG ===")
        Console.WriteLine($"Color cache last updated: {_lastColorCacheUpdate}")
        Console.WriteLine($"Color cache exists: {_cachedColors IsNot Nothing}")

        If _cachedColors IsNot Nothing Then
            Console.WriteLine($"Cached colors count: {_cachedColors.Count}")
            For Each kvp In _cachedColors
                Console.WriteLine($"  {kvp.Key}: {kvp.Value}")
            Next
        End If

        ' Force reload and show what we get
        Console.WriteLine("--- Forcing color reload from database ---")
        Try
            _cachedColors = New Dictionary(Of String, Color)()

            Dim query As String = "SELECT * FROM ColorSettings WHERE IsActive = 1 ORDER BY DateCreated DESC"
            Using reader As DbDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                If reader.Read() Then
                    Console.WriteLine("? Found ColorSettings record in database")
                    For i = 0 To reader.FieldCount - 1
                        Dim fieldName = reader.GetName(i)
                        Dim fieldValue = If(reader.IsDBNull(i), "NULL", reader.GetValue(i).ToString())
                        Console.WriteLine($"  Database field {fieldName}: {fieldValue}")

                        If Not reader.IsDBNull(i) AndAlso fieldName.ToLower().Contains("color") Then
                            Dim colorString = reader.GetValue(i).ToString()
                            _cachedColors(fieldName) = ColorFromString(colorString)
                            Console.WriteLine($"    --> Parsed as color: {ColorFromString(colorString)}")
                        End If
                    Next
                Else
                    Console.WriteLine("? No ColorSettings record found in database")
                End If
            End Using

            _lastColorCacheUpdate = DateTime.Now
            Console.WriteLine($"After manual reload - colors count: {_cachedColors.Count}")

        Catch ex As Exception
            Console.WriteLine($"? Error in debug color reload: {ex.Message}")
        End Try
        Console.WriteLine("=====================================")
    End Sub
    ''' <summary>
    ''' Debug method to show color loading with MessageBox
    ''' </summary>
    Public Sub DebugColorLoadingWithMessageBox()
        Dim debugMsg As String = "=== COMPANYSETTINGSMANAGER DEBUG ===" & vbCrLf
        debugMsg &= $"Color cache exists: {_cachedColors IsNot Nothing}" & vbCrLf

        If _cachedColors IsNot Nothing Then
            debugMsg &= $"Cached colors count: {_cachedColors.Count}" & vbCrLf
            For Each kvp In _cachedColors
                debugMsg &= $"  {kvp.Key}: {kvp.Value}" & vbCrLf
            Next
        End If

        ' Force reload and show what we get
        debugMsg &= "--- Forcing color reload from database ---" & vbCrLf
        Try
            _cachedColors = New Dictionary(Of String, Color)()

            Dim query As String = "SELECT * FROM ColorSettings WHERE IsActive = 1 ORDER BY DateCreated DESC"
            Using reader As DbDataReader = Utilities.ExecuteReader(query, New SqlParameter() {})
                If reader.Read() Then
                    debugMsg &= "? Found ColorSettings record in database" & vbCrLf
                    For i = 0 To reader.FieldCount - 1
                        Dim fieldName = reader.GetName(i)
                        Dim fieldValue = If(reader.IsDBNull(i), "NULL", reader.GetValue(i).ToString())
                        debugMsg &= $"  Database field {fieldName}: {fieldValue}" & vbCrLf

                        If Not reader.IsDBNull(i) AndAlso fieldName.ToLower().Contains("color") Then
                            Dim colorString = reader.GetValue(i).ToString()
                            _cachedColors(fieldName) = ColorFromString(colorString)
                            debugMsg &= $"    --> Parsed as color: {ColorFromString(colorString)}" & vbCrLf
                        End If
                    Next
                Else
                    debugMsg &= "? No ColorSettings record found in database" & vbCrLf
                End If
            End Using

            _lastColorCacheUpdate = DateTime.Now
            debugMsg &= $"After manual reload - colors count: {_cachedColors.Count}" & vbCrLf

        Catch ex As Exception
            debugMsg &= $"? Error in debug color reload: {ex.Message}" & vbCrLf
        End Try
        debugMsg &= "=====================================" & vbCrLf

        MessageBox.Show(debugMsg, "Color Debug Info")
    End Sub

    ''' <summary>
    ''' Create default logo using Jade Dental Logo resource or fallback
    ''' </summary>
    Private Function CreateDefaultJadeLogo() As Image
        Try
            ' First try to use the actual Jade Dental Logo from resources
            If My.Resources.Jade_Dental_Logo IsNot Nothing Then
                Return New Bitmap(My.Resources.Jade_Dental_Logo)
            End If
        Catch ex As Exception
            Console.WriteLine($"Could not load Jade_Dental_Logo resource: {ex.Message}")
        End Try

        ' Fallback: Create a professional-looking default logo with Jade Clinic branding
        Dim logo As New Bitmap(200, 150)
        Using g As Graphics = Graphics.FromImage(logo)
            ' Enable anti-aliasing for smooth graphics
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias

            ' Create gradient background with clinic colors
            Using brush As New Drawing2D.LinearGradientBrush(
                New Rectangle(0, 0, 200, 150),
                Color.FromArgb(254, 191, 16),  ' Golden Yellow
                Color.FromArgb(190, 154, 48),  ' Rich Olive
                Drawing2D.LinearGradientMode.Vertical)
                
                g.FillRectangle(brush, 0, 0, 200, 150)
            End Using

            ' Draw company name with professional styling
            Using titleFont As New Font("Poppins", 20, FontStyle.Bold)
                Using titleBrush As New SolidBrush(Color.FromArgb(26, 29, 31)) ' Deep Charcoal
                    Dim titleSize = g.MeasureString("JADE", titleFont)
                    g.DrawString("JADE", titleFont, titleBrush,
                        (200 - titleSize.Width) / 2, 15)
                End Using
            End Using

            Using subtitleFont As New Font("Poppins", 14, FontStyle.Regular)
                Using subtitleBrush As New SolidBrush(Color.FromArgb(26, 29, 31)) ' Deep Charcoal
                    Dim subtitleSize = g.MeasureString("CLINIC", subtitleFont)
                    g.DrawString("CLINIC", subtitleFont, subtitleBrush,
                        (200 - subtitleSize.Width) / 2, 45)
                End Using
            End Using

            ' Add a subtle dental icon or border
            Using borderPen As New Pen(Color.FromArgb(26, 29, 31), 2)
                g.DrawRectangle(borderPen, 1, 1, 198, 148)
            End Using
        End Using
        
        Return logo
    End Function
End Class