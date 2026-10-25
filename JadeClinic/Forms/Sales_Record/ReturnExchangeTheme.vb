Imports System.Drawing
Imports System.Drawing.Drawing2D

' Shared palette, typography and lightweight vector icons for the redesigned
' Return / Exchange dialog and its reusable controls. Kept dependency-free so
' the dialog renders identically regardless of installed icon fonts.
Friend Module ReturnExchangeTheme

    ' ---- Palette ----------------------------------------------------------
    Public ReadOnly PrimaryGold As Color = ColorTranslator.FromHtml("#C49A2C")
    Public ReadOnly GoldHover As Color = ColorTranslator.FromHtml("#AD8520")
    Public ReadOnly LightGoldBg As Color = ColorTranslator.FromHtml("#FFF8E6")
    Public ReadOnly MainBg As Color = ColorTranslator.FromHtml("#FFFFFF")
    Public ReadOnly SecondaryBg As Color = ColorTranslator.FromHtml("#FAFAF9")
    Public ReadOnly BorderColor As Color = ColorTranslator.FromHtml("#E9E7E2")
    Public ReadOnly PrimaryText As Color = ColorTranslator.FromHtml("#252525")
    Public ReadOnly SecondaryText As Color = ColorTranslator.FromHtml("#737780")
    Public ReadOnly MutedText As Color = ColorTranslator.FromHtml("#9CA3AF")
    Public ReadOnly SuccessGreen As Color = ColorTranslator.FromHtml("#2E7D32")
    Public ReadOnly SuccessBg As Color = ColorTranslator.FromHtml("#EAF6ED")
    Public ReadOnly ErrorRed As Color = ColorTranslator.FromHtml("#C62828")
    Public ReadOnly ErrorBg As Color = ColorTranslator.FromHtml("#FDEDEE")
    Public ReadOnly NeutralFill As Color = ColorTranslator.FromHtml("#F5F5F4")
    Public ReadOnly NeutralHover As Color = ColorTranslator.FromHtml("#EAEAEA")
    Public ReadOnly HeaderTextColor As Color = ColorTranslator.FromHtml("#5F6773")

    ' ---- Typography -------------------------------------------------------
    Private _family As String = Nothing

    Public Function FontFamilyName() As String
        If _family IsNot Nothing Then Return _family
        Dim installed As New HashSet(Of String)()
        Try
            For Each family As FontFamily In System.Drawing.FontFamily.Families
                installed.Add(family.Name)
            Next
        Catch
        End Try
        If installed.Contains("Poppins") Then
            _family = "Poppins"
        ElseIf installed.Contains("Segoe UI") Then
            _family = "Segoe UI"
        Else
            _family = "Arial"
        End If
        Return _family
    End Function

    Public Function MakeFont(size As Single, Optional style As FontStyle = FontStyle.Regular) As Font
        Try
            Return New Font(FontFamilyName(), size, style)
        Catch
            Return New Font("Segoe UI", size, style)
        End Try
    End Function

    ' ---- Vector icons -----------------------------------------------------
    Public Function SwapIcon(size As Integer, color As Color) As Bitmap
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Dim penW As Single = Math.Max(1.6F, size / 11.0F)
            Using p As New Pen(color, penW)
                p.StartCap = LineCap.Round
                p.EndCap = LineCap.ArrowAnchor
                Dim m As Single = size * 0.24F
                g.DrawLine(p, m, size * 0.36F, size - m, size * 0.36F)
                g.DrawLine(p, size - m, size * 0.64F, m, size * 0.64F)
            End Using
        End Using
        Return bmp
    End Function

    Public Function CloseIcon(size As Integer, color As Color) As Bitmap
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Using p As New Pen(color, Math.Max(1.8F, size / 9.0F))
                p.StartCap = LineCap.Round
                p.EndCap = LineCap.Round
                Dim m As Single = size * 0.28F
                g.DrawLine(p, m, m, size - m, size - m)
                g.DrawLine(p, size - m, m, m, size - m)
            End Using
        End Using
        Return bmp
    End Function

    Public Function DocumentIcon(size As Integer, color As Color) As Bitmap
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Using p As New Pen(color, Math.Max(1.4F, size / 13.0F))
                p.LineJoin = LineJoin.Round
                Dim r As New RectangleF(size * 0.26F, size * 0.14F, size * 0.48F, size * 0.72F)
                g.DrawRectangle(p, r.X, r.Y, r.Width, r.Height)
                g.DrawLine(p, size * 0.37F, size * 0.38F, size * 0.63F, size * 0.38F)
                g.DrawLine(p, size * 0.37F, size * 0.5F, size * 0.63F, size * 0.5F)
                g.DrawLine(p, size * 0.37F, size * 0.62F, size * 0.53F, size * 0.62F)
            End Using
        End Using
        Return bmp
    End Function

    Public Function PackageIcon(size As Integer, color As Color) As Bitmap
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Using p As New Pen(color, Math.Max(1.4F, size / 13.0F))
                p.LineJoin = LineJoin.Round
                g.DrawRectangle(p, size * 0.22F, size * 0.28F, size * 0.56F, size * 0.5F)
                g.DrawLine(p, size * 0.22F, size * 0.43F, size * 0.78F, size * 0.43F)
                g.DrawLine(p, size * 0.5F, size * 0.28F, size * 0.5F, size * 0.43F)
            End Using
        End Using
        Return bmp
    End Function

    Public Function InfoIcon(size As Integer, color As Color) As Bitmap
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Using b As New SolidBrush(color)
                g.FillEllipse(b, 0.5F, 0.5F, size - 1.0F, size - 1.0F)
            End Using
            Dim penW As Single = Math.Max(1.6F, size / 9.0F)
            Using p As New Pen(Color.White, penW)
                p.StartCap = LineCap.Round
                p.EndCap = LineCap.Round
                g.DrawLine(p, size / 2.0F, size * 0.44F, size / 2.0F, size * 0.72F)
            End Using
            Using b As New SolidBrush(Color.White)
                Dim d As Single = penW * 1.15F
                g.FillEllipse(b, size / 2.0F - d / 2.0F, size * 0.27F, d, d)
            End Using
        End Using
        Return bmp
    End Function

    ' Neutral product placeholder: a soft square with the product initial.
    Public Function AvatarImage(size As Integer, text As String) As Bitmap
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAliasGridFit
            g.Clear(NeutralFill)
            Dim initial As String = "?"
            If Not String.IsNullOrWhiteSpace(text) Then
                initial = text.Trim().Substring(0, 1).ToUpperInvariant()
            End If
            Using f As New Font(FontFamilyName(), size * 0.4F, FontStyle.Bold)
                Using sf As New StringFormat() With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                    Using b As New SolidBrush(Color.FromArgb(150, SecondaryText))
                        g.DrawString(initial, f, b, New RectangleF(0, 0, size, size), sf)
                    End Using
                End Using
            End Using
        End Using
        Return bmp
    End Function

    Public Function DotImage(size As Integer, color As Color) As Bitmap
        Dim bmp As New Bitmap(size, size)
        Using g As Graphics = Graphics.FromImage(bmp)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Using b As New SolidBrush(color)
                g.FillEllipse(b, 0, 0, size - 1, size - 1)
            End Using
        End Using
        Return bmp
    End Function

End Module
