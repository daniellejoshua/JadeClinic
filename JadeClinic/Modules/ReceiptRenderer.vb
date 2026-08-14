Imports System.Drawing

' Data model for the GDI receipt page. Shared by the Sales form preview and the
' Sales Record "eye" view (SalesDetails) so both render identically.

Public Class ReceiptLineItem
    Public Property ProductName As String = ""
    Public Property Quantity As Integer = 0
    Public Property UnitVatInc As Decimal = 0D
    Public Property LineTotal As Decimal = 0D
End Class

Public Class ReceiptData
    Public Property ReceiptNumber As String = ""
    Public Property SaleDate As DateTime = DateTime.Now
    Public Property Cashier As String = ""
    Public Property CustomerName As String = "________________"
    Public Property CustomerTIN As String = "________________"
    Public Property CustomerPhone As String = "________________"
    Public Property CustomerEmail As String = "________________"
    Public Property Items As New List(Of ReceiptLineItem)()
    Public Property SubtotalVatInclusive As Decimal = 0D
    Public Property DiscountAmount As Decimal = 0D
    Public Property DiscountType As String = "None"
    Public Property DiscountedItemName As String = ""
    Public Property VatableNet As Decimal = 0D
    Public Property VatAmount As Decimal = 0D
    Public Property TotalDue As Decimal = 0D
    Public Property PaymentMethod As String = "Cash"
    Public Property PaymentReference As String = ""
    Public Property AmountReceived As Decimal = 0D
    Public Property Change As Decimal = 0D
End Class

Public Module ReceiptRenderer
    ' Draws the receipt page using the same layout as the Sales Record "eye"
    ' view: Arial fonts, real peso sign, two-column customer block.
    Public Sub DrawReceipt(g As Graphics, marginBounds As Rectangle, data As ReceiptData)
        Try
            If data Is Nothing Then
                g.DrawString("No receipt data available.", New Font("Arial", 10), Brushes.Black, 10, 10)
                Return
            End If

            Dim regularFont As New Font("Arial", 8)
            Dim boldFont As New Font("Arial", 10, FontStyle.Bold)
            Dim headerFont As New Font("Arial", 12, FontStyle.Bold)
            Dim sectionHeaderFont As New Font("Arial", 9, FontStyle.Bold)
            Dim brush As New SolidBrush(Color.Black)
            Dim yPosition As Integer = 10
            Dim marginLeft As Integer = 10
            Dim contentWidth As Integer = marginBounds.Width - (marginLeft * 2)
            Dim centerX As Integer = marginBounds.Width \ 2
            Dim colGap As Integer = 20
            Dim colWidth As Integer = (contentWidth - colGap) \ 2
            Dim leftColX As Integer = marginLeft
            Dim rightColX As Integer = marginLeft + colWidth + colGap
            Dim peso As String = ChrW(&H20B1)

            ' Company header
            Dim cm As CompanySettingsManager = CompanySettingsManager.Instance
            Dim companyName As String = cm.GetSettingString("CompanyName", "JADE CLINIC")
            Dim companyPhone As String = cm.GetSettingString("Phone", "")
            Dim companyAddress As String = cm.GetSettingString("Address", "")
            Dim companyWebsite As String = cm.GetSettingString("Website", "")
            Dim companyTIN As String = cm.GetSettingString("TIN", "")
            Dim birAuthNumber As String = cm.GetSettingString("BIRAuthNumber", "ATP-2024-000001")
            Dim ptuNumber As String = cm.GetSettingString("PTUNumber", "PTU-2024-001")
            Dim footerMessage As String = cm.GetSettingString("ReceiptFooter", "Thank you for your business!" & vbCrLf & "Have a great day!")

            g.DrawString(companyName, headerFont, brush, CSng(centerX - (g.MeasureString(companyName, headerFont).Width / 2)), CSng(yPosition))
            yPosition += 24
            g.DrawString("Dental Supply Management", regularFont, brush, CSng(centerX - (g.MeasureString("Dental Supply Management", regularFont).Width / 2)), CSng(yPosition))
            yPosition += 14

            If Not String.IsNullOrEmpty(companyTIN) Then
                Dim tinLine = $"TIN: {companyTIN} (VAT Registered)"
                g.DrawString(tinLine, regularFont, brush, CSng(centerX - (g.MeasureString(tinLine, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            If Not String.IsNullOrEmpty(companyPhone) Then
                Dim telLine = $"Tel: {companyPhone}"
                g.DrawString(telLine, regularFont, brush, CSng(centerX - (g.MeasureString(telLine, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            If Not String.IsNullOrEmpty(companyAddress) Then
                g.DrawString(companyAddress, regularFont, brush, CSng(centerX - (g.MeasureString(companyAddress, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            If Not String.IsNullOrEmpty(companyWebsite) Then
                g.DrawString(companyWebsite, regularFont, brush, CSng(centerX - (g.MeasureString(companyWebsite, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            g.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 16

            ' Document title and metadata
            g.DrawString("SALES INVOICE", boldFont, brush, CSng(centerX - (g.MeasureString("SALES INVOICE", boldFont).Width / 2)), CSng(yPosition))
            yPosition += 22
            g.DrawString($"Receipt #: {data.ReceiptNumber}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            g.DrawString($"Date: {data.SaleDate:MM/dd/yyyy HH:mm:ss}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            g.DrawString($"Cashier: {data.Cashier}", regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            ' Customer block (2x2 layout)
            g.DrawString("Customer Details:", regularFont, brush, marginLeft, yPosition)
            yPosition += 12

            g.DrawString($"Name: {data.CustomerName}", regularFont, brush, leftColX, yPosition)
            g.DrawString($"TIN: {data.CustomerTIN}", regularFont, brush, rightColX, yPosition)
            yPosition += 12
            g.DrawString($"Phone: {data.CustomerPhone}", regularFont, brush, leftColX, yPosition)
            g.DrawString($"Email: {data.CustomerEmail}", regularFont, brush, rightColX, yPosition)
            yPosition += 14

            g.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            ' Items
            For Each item As ReceiptLineItem In data.Items
                g.DrawString($"{item.Quantity}x {item.ProductName}", regularFont, brush, marginLeft, yPosition)
                yPosition += 12
                g.DrawString($"@ {peso}{item.UnitVatInc:F2}", regularFont, brush, marginLeft + 8, yPosition)
                g.DrawString($"{peso}{item.LineTotal:F2}", regularFont, brush, CSng(marginBounds.Right - g.MeasureString($"{peso}{item.LineTotal:F2}", regularFont).Width), CSng(yPosition))
                yPosition += 15
            Next

            g.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            ' VAT / totals
            g.DrawString("SUBTOTAL (VAT-INC):", regularFont, brush, marginLeft, yPosition)
            g.DrawString($"{peso}{data.SubtotalVatInclusive:F2}", regularFont, brush, CSng(marginBounds.Right - g.MeasureString($"{peso}{data.SubtotalVatInclusive:F2}", regularFont).Width), CSng(yPosition))
            yPosition += 12

            If data.DiscountAmount > 0D Then
                Dim discountLabel As String = $"Less: Discount ({data.DiscountType})"
                If Not String.IsNullOrEmpty(data.DiscountedItemName) Then discountLabel &= $" on {data.DiscountedItemName}"
                g.DrawString(discountLabel & ":", regularFont, brush, marginLeft, yPosition)
                g.DrawString($"-{peso}{data.DiscountAmount:F2}", regularFont, brush, CSng(marginBounds.Right - g.MeasureString($"-{peso}{data.DiscountAmount:F2}", regularFont).Width), CSng(yPosition))
                yPosition += 12
            End If

            g.DrawString("VATABLE SALES (NET):", regularFont, brush, marginLeft, yPosition)
            g.DrawString($"{peso}{data.VatableNet:F2}", regularFont, brush, CSng(marginBounds.Right - g.MeasureString($"{peso}{data.VatableNet:F2}", regularFont).Width), CSng(yPosition))
            yPosition += 12

            g.DrawString("VAT (12%):", regularFont, brush, marginLeft, yPosition)
            g.DrawString($"{peso}{data.VatAmount:F2}", regularFont, brush, CSng(marginBounds.Right - g.MeasureString($"{peso}{data.VatAmount:F2}", regularFont).Width), CSng(yPosition))
            yPosition += 12

            g.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 12

            g.DrawString("TOTAL AMOUNT DUE:", boldFont, brush, marginLeft, yPosition)
            g.DrawString($"{peso}{data.TotalDue:F2}", boldFont, brush, CSng(marginBounds.Right - g.MeasureString($"{peso}{data.TotalDue:F2}", boldFont).Width), CSng(yPosition))
            yPosition += 18

            ' Payment info
            g.DrawString("PAYMENT INFORMATION", sectionHeaderFont, brush, marginLeft, yPosition)
            yPosition += 14
            g.DrawString($"Payment Method: {If(String.IsNullOrWhiteSpace(data.PaymentMethod), "N/A", data.PaymentMethod)}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            If Not String.IsNullOrWhiteSpace(data.PaymentReference) Then
                g.DrawString($"Reference: {data.PaymentReference}", regularFont, brush, marginLeft, yPosition)
                yPosition += 12
            End If
            g.DrawString($"Amount Received: {peso}{data.AmountReceived:F2}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            g.DrawString($"Change: {peso}{data.Change:F2}", regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            g.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            g.DrawString($"BIR Authority to Print No.: {birAuthNumber}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            g.DrawString($"PTU No.: {ptuNumber}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            g.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 12

            Dim footerLines() As String = footerMessage.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            For Each line As String In footerLines
                g.DrawString(line, regularFont, brush, CSng(centerX - (g.MeasureString(line, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 12
            Next

            regularFont.Dispose()
            boldFont.Dispose()
            headerFont.Dispose()
            sectionHeaderFont.Dispose()
            brush.Dispose()
        Catch ex As Exception
            g.DrawString($"Receipt render error: {ex.Message}", New Font("Arial", 10), Brushes.Black, 10, 10)
        End Try
    End Sub
End Module
