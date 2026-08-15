' Empty Cart State
' ================
' Reusable empty-state component shown inside the Order Summary / cart area
' when the current sale has no products. Plain container: white background,
' 2px dashed soft-warm-gold border, rounded corners. Content is a gray cart
' icon inside a warm-gold circle, a title, and a short description - all
' centered both horizontally and vertically.
'
' Palette:
'   Container bg   #FFFFFF
'   Dashed border  #F1E0B2
'   Circle bg      #FBF7EC
'   Icon           #9CA3AF
'   Title          #222222
'   Description    #787878

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Text
Imports System.Windows.Forms

Public Module EmptyCartStateBuilder

    Private ReadOnly EmptyStateBorder As Color = Color.FromArgb(241, 224, 178) ' #F1E0B2
    Private ReadOnly EmptyStateCircleBg As Color = Color.FromArgb(251, 247, 236) ' #FBF7EC
    Private ReadOnly EmptyStateIcon As Color = Color.FromArgb(156, 163, 175)    ' #9CA3AF
    Private ReadOnly EmptyStateTitle As Color = Color.FromArgb(34, 34, 34)      ' #222222
    Private ReadOnly EmptyStateDesc As Color = Color.FromArgb(120, 120, 120)    ' #787878

    ' Cart emoji (U+1F6D2) built from its surrogate pair so it survives any file encoding
    Private Const CartEmoji As String = "🛒"

    ' Build the complete, self-laying-out empty state panel.
    Public Function BuildEmptyCartState() As Guna.UI2.WinForms.Guna2Panel
        Dim pnl As New Guna.UI2.WinForms.Guna2Panel()
        pnl.Dock = DockStyle.Fill
        pnl.BackColor = Color.White
        pnl.FillColor = Color.White
        pnl.BorderColor = EmptyStateBorder
        pnl.BorderThickness = 2
        pnl.BorderRadius = 12
        pnl.BorderStyle = DashStyle.Dash

        ' Icon circle (Guna2Panel with radius = half its size makes a perfect circle)
        Dim circle As New Guna.UI2.WinForms.Guna2Panel()
        circle.Size = New Size(96, 96)
        circle.FillColor = EmptyStateCircleBg
        circle.BorderColor = EmptyStateCircleBg
        circle.BorderThickness = 0
        circle.BorderRadius = 42

        ' Cart emoji (mirrored) drawn directly on the circle so the circular
        ' cream background stays fully visible behind the glyph.
        AddHandler circle.Paint, Sub(s As Object, ev As PaintEventArgs)
                                     DrawCartEmoji(ev.Graphics, circle)
                                 End Sub

        ' Title
        Dim title As New Label()
        title.Text = "No items added"
        title.AutoSize = False
        title.TextAlign = ContentAlignment.MiddleCenter
        title.ForeColor = EmptyStateTitle
        title.BackColor = Color.White
        title.Font = New Font(ResolveFontFamily({"Poppins SemiBold", "Poppins", "Segoe UI"}), 14.0F, FontStyle.Regular)

        ' Description (two centered lines)
        Dim desc As New Label()
        desc.Text = "Select a product category" & vbCrLf & "and add items to this sale."
        desc.AutoSize = False
        desc.TextAlign = ContentAlignment.MiddleCenter
        desc.ForeColor = EmptyStateDesc
        desc.BackColor = Color.White
        desc.Font = New Font(ResolveFontFamily({"Poppins", "Segoe UI"}), 11.5F, FontStyle.Regular)

        pnl.Controls.Add(circle)
        pnl.Controls.Add(title)
        pnl.Controls.Add(desc)

        AddHandler pnl.Resize, Sub(s As Object, e As EventArgs)
                                   LayoutEmptyCartState(pnl, circle, title, desc)
                               End Sub
        LayoutEmptyCartState(pnl, circle, title, desc)

        Return pnl
    End Function

    ' Center the vertical stack (circle -> title -> description) in the panel.
    ' Label heights are derived from the real glyph metrics so text is never
    ' clipped; if the panel is too short, the circle shrinks to make room.
    Private Sub LayoutEmptyCartState(pnl As Control, circle As Control, title As Label, desc As Label)
        Dim w As Integer = pnl.ClientSize.Width
        Dim h As Integer = pnl.ClientSize.Height
        If w <= 0 OrElse h <= 0 Then Return

        Const stackEdge As Integer = 8
        Const gapCircleTitle As Integer = 14
        Const gapTitleDesc As Integer = 5
        Const maxCircle As Integer = 96

        ' Available text width (20px margins each side)
        Dim textW As Integer = Math.Max(120, w - 40)
        Dim titleHeight As Integer = MeasureTextHeight(title, textW)
        Dim descHeight As Integer = MeasureTextHeight(desc, textW)

        ' Shrink the circle if the panel is too short to hold the whole stack
        Dim availableH As Integer = h - (stackEdge * 2)
        Dim fixedH As Integer = gapCircleTitle + titleHeight + gapTitleDesc + descHeight
        Dim circleSize As Integer = maxCircle
        If fixedH + circleSize > availableH Then
            circleSize = Math.Max(48, availableH - fixedH)
        End If

        Dim totalHeight As Integer = circleSize + fixedH
        Dim y As Integer = stackEdge + Math.Max(0, (h - (stackEdge * 2) - totalHeight) \ 2)

        circle.Size = New Size(circleSize, circleSize)
        CType(circle, Guna.UI2.WinForms.Guna2Panel).BorderRadius = circleSize \ 2
        circle.Location = New Point((w - circleSize) \ 2, y)

        title.Width = textW
        title.Height = titleHeight
        title.Location = New Point(20, y + circleSize + gapCircleTitle)

        desc.Width = textW
        desc.Height = descHeight
        desc.Location = New Point(20, y + circleSize + gapCircleTitle + titleHeight + gapTitleDesc)
    End Sub

    ' Height the label's text actually needs at the given width (with a small
    ' buffer), so the Label never clips the glyphs on the Y axis.
    Private Function MeasureTextHeight(lbl As Label, width As Integer) As Integer
        Try
            Dim size As Size = TextRenderer.MeasureText(lbl.Text, lbl.Font,
                                                        New Size(width, Integer.MaxValue),
                                                        TextFormatFlags.TextBoxControl Or
                                                        TextFormatFlags.WordBreak Or
                                                        TextFormatFlags.NoPadding)
            Return size.Height + 6
        Catch
            Return lbl.Font.Height + 4
        End Try
    End Function

    ' Render the cart emoji centered in the circle. Drawn straight onto the
    ' circle's surface (no bitmap/flip, so no pixelation); TextRenderer applies
    ' the gray ForeColor on systems that render the emoji as a monochrome glyph.
    Private Sub DrawCartEmoji(g As Graphics, circle As Control)
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit
        Using font As New Font(ResolveEmojiFont(), 28.0F, FontStyle.Regular)
            Dim sz As Size = TextRenderer.MeasureText(CartEmoji, font)
            ' Emoji glyphs carry asymmetric side bearings, so nudge the drawn
            ' position slightly right of the measured center to look centered.
            Dim x As Integer = ((circle.Width - sz.Width) \ 2) + 4
            Dim y As Integer = (circle.Height - sz.Height) \ 2
            TextRenderer.DrawText(g, CartEmoji, font, New Point(x, y), EmptyStateIcon)
        End Using
    End Sub

    ' Best available family for rendering the cart emoji.
    Private Function ResolveEmojiFont() As String
        Try
            Dim installed As New HashSet(Of String)()
            For Each family As FontFamily In System.Drawing.FontFamily.Families
                installed.Add(family.Name)
            Next
            For Each name As String In {"Segoe UI Emoji", "Segoe UI Symbol", "Noto Color Emoji", "Arial"}
                If installed.Contains(name) Then Return name
            Next
        Catch
        End Try
        Return "Segoe UI"
    End Function

    Private Function ResolveFontFamily(priorityNames As String()) As String
        Try
            Dim installed As New HashSet(Of String)()
            For Each family As FontFamily In System.Drawing.FontFamily.Families
                installed.Add(family.Name)
            Next
            For Each name As String In priorityNames
                If installed.Contains(name) Then Return name
            Next
        Catch
        End Try
        Return "Segoe UI"
    End Function
End Module
