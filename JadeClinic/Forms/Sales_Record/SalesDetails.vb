Imports System.Data.Common
Imports System.Text
Imports System.Drawing.Printing

Public Class SalesDetails
    Private ReadOnly _saleId As Integer
    Private txtReceipt As TextBox
    Private receiptPreview As PrintPreviewControl
    Private receiptDocument As PrintDocument

    Private saleRecord As Dictionary(Of String, Object) = Nothing
    Private saleItems As New List(Of Dictionary(Of String, Object))()

    Public Sub New(saleId As Integer)
        ' This call is required by the designer.
        InitializeComponent()

        ' Store sale id
        _saleId = saleId

        ' Ensure a receipt textbox exists (use designer control if present)
        txtReceipt = Me.Controls.OfType(Of TextBox)().FirstOrDefault(Function(t) t.Name = "txtReceipt")
        If txtReceipt Is Nothing Then
            txtReceipt = New TextBox() With {
                .Name = "txtReceipt",
                .Multiline = True,
                .ReadOnly = True,
                .ScrollBars = ScrollBars.Vertical,
                .Font = New Drawing.Font("Courier New", 9),
                .Dock = DockStyle.Fill
            }
            Me.Controls.Add(txtReceipt)
            txtReceipt.BringToFront()
        End If
    End Sub

    Private Sub SalesDetails_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Try
            LoadSaleData(_saleId)
            If saleRecord Is Nothing Then
                txtReceipt.Text = $"Sale ID {_saleId} not found."
                Return
            End If

            ShowReceiptPreviewLikeSalesForm()
            Me.Text = $"Receipt - Sale #{DisplaySaleNumber()}"
        Catch ex As Exception
            MessageBox.Show($"Error loading sale details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Function DisplaySaleNumber() As String
        If saleRecord IsNot Nothing AndAlso saleRecord.ContainsKey("SaleNumber") Then
            Dim num As String = Convert.ToString(saleRecord("SaleNumber"))
            If Not String.IsNullOrWhiteSpace(num) Then Return num
        End If
        Return _saleId.ToString()
    End Function

    Private Sub LoadSaleData(saleId As Integer)
        saleRecord = Nothing
        saleItems.Clear()

        Dim saleQuery As String = "SELECT s.SaleID, IFNULL(s.SaleNumber, '') AS SaleNumber, s.SaleDate, s.CustomerName, s.CustomerTIN, s.TotalAmount, s.AmountPaid, s.PaymentMethod, s.Reference, s.SalesData, u.Username " &
                                  "FROM Sales s LEFT JOIN Users u ON s.UserID = u.UserID WHERE s.SaleID = @SaleID"
        Using reader As DbDataReader = Utilities.ExecuteReader(saleQuery, New SqlParameter("@SaleID", saleId))
            If reader.Read() Then
                saleRecord = New Dictionary(Of String, Object) From {
                    {"SaleID", If(IsDBNull(reader("SaleID")), saleId, reader("SaleID"))},
                    {"SaleNumber", If(IsDBNull(reader("SaleNumber")), "", reader("SaleNumber").ToString())},
                    {"SaleDate", If(IsDBNull(reader("SaleDate")), DateTime.MinValue, Convert.ToDateTime(reader("SaleDate")))},
                    {"CustomerName", If(IsDBNull(reader("CustomerName")), "", reader("CustomerName").ToString())},
                    {"CustomerTIN", If(IsDBNull(reader("CustomerTIN")), "", reader("CustomerTIN").ToString())},
                    {"TotalAmount", If(IsDBNull(reader("TotalAmount")), 0D, Convert.ToDecimal(reader("TotalAmount")))},
                    {"AmountPaid", If(IsDBNull(reader("AmountPaid")), 0D, Convert.ToDecimal(reader("AmountPaid")))},
                    {"PaymentMethod", If(IsDBNull(reader("PaymentMethod")), "Cash", reader("PaymentMethod").ToString())},
                    {"Reference", If(IsDBNull(reader("Reference")), "", reader("Reference").ToString())},
                    {"SalesData", If(IsDBNull(reader("SalesData")), "", reader("SalesData").ToString())},
                    {"Cashier", If(IsDBNull(reader("Username")), frmLoginvb.LoggedInUsername, reader("Username").ToString())}
                }
            End If
        End Using

        If saleRecord Is Nothing Then Return

        Dim itemsQuery As String = "SELECT si.SaleItemID, si.ProductID, IFNULL(p.ProductName, 'Unknown') AS ProductName, si.Quantity, si.UnitPrice " &
                                   "FROM SaleItems si LEFT JOIN Products p ON si.ProductID = p.ProductID WHERE si.SaleID = @SaleID ORDER BY si.SaleItemID"
        Using reader As DbDataReader = Utilities.ExecuteReader(itemsQuery, New SqlParameter("@SaleID", saleId))
            While reader.Read()
                Dim it As New Dictionary(Of String, Object) From {
                    {"SaleItemID", If(IsDBNull(reader("SaleItemID")), 0, Convert.ToInt32(reader("SaleItemID")))},
                    {"ProductID", If(IsDBNull(reader("ProductID")), 0, Convert.ToInt32(reader("ProductID")))},
                    {"ProductName", If(IsDBNull(reader("ProductName")), "Unknown", reader("ProductName").ToString())},
                    {"Quantity", If(IsDBNull(reader("Quantity")), 0, Convert.ToInt32(reader("Quantity")))},
                    {"UnitPrice", If(IsDBNull(reader("UnitPrice")), 0D, Convert.ToDecimal(reader("UnitPrice")))}
                }
                saleItems.Add(it)
            End While
        End Using
    End Sub

    Private Sub ShowReceiptPreviewLikeSalesForm()
        If txtReceipt IsNot Nothing AndAlso txtReceipt.Parent IsNot Nothing Then
            txtReceipt.Visible = False
        End If

        If receiptPreview Is Nothing Then
            receiptPreview = New PrintPreviewControl() With {
                .Dock = DockStyle.Fill,
                .Zoom = 1.0,
                .AutoZoom = True,
                .UseAntiAlias = True
            }
            Me.Controls.Add(receiptPreview)
            receiptPreview.BringToFront()
        End If

        If receiptDocument Is Nothing Then
            receiptDocument = New PrintDocument()
            receiptDocument.DefaultPageSettings.PaperSize = New PaperSize("Receipt", 300, 700)
            receiptDocument.DefaultPageSettings.Margins = New Margins(10, 10, 10, 10)
            AddHandler receiptDocument.PrintPage, AddressOf ReceiptDocument_PrintPage
        End If

        receiptPreview.Document = receiptDocument
        receiptPreview.InvalidatePreview()
    End Sub

    Private Sub ReceiptDocument_PrintPage(sender As Object, e As PrintPageEventArgs)
        Try
            If saleRecord Is Nothing Then
                e.Graphics.DrawString("No receipt data available.", New Font("Arial", 10), Brushes.Black, 10, 10)
                Return
            End If

            Dim regularFont As New Font("Arial", 8)
            Dim boldFont As New Font("Arial", 10, FontStyle.Bold)
            Dim headerFont As New Font("Arial", 12, FontStyle.Bold)
            Dim sectionHeaderFont As New Font("Arial", 9, FontStyle.Bold)
            Dim brush As New SolidBrush(Color.Black)
            Dim yPosition As Integer = 10
            Dim marginLeft As Integer = 10
            Dim contentWidth As Integer = e.MarginBounds.Width - (marginLeft * 2)
            Dim centerX As Integer = e.MarginBounds.Width \ 2
            Dim colGap As Integer = 20
            Dim colWidth As Integer = (contentWidth - colGap) \ 2
            Dim leftColX As Integer = marginLeft
            Dim rightColX As Integer = marginLeft + colWidth + colGap

            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")
            Dim companyPhone As String = CompanySettingsManager.Instance.GetSettingString("Phone", "")
            Dim companyAddress As String = CompanySettingsManager.Instance.GetSettingString("Address", "")
            Dim companyWebsite As String = CompanySettingsManager.Instance.GetSettingString("Website", "")
            Dim companyTIN As String = CompanySettingsManager.Instance.GetSettingString("TIN", "")
            Dim birAuthNumber As String = CompanySettingsManager.Instance.GetSettingString("BIRAuthNumber", "ATP-2024-000001")
            Dim ptuNumber As String = CompanySettingsManager.Instance.GetSettingString("PTUNumber", "PTU-2024-001")
            Dim footerMessage As String = CompanySettingsManager.Instance.GetSettingString("ReceiptFooter", "Thank you for your business!" & vbCrLf & "Have a great day!")

            e.Graphics.DrawString(companyName, headerFont, brush, CSng(centerX - (e.Graphics.MeasureString(companyName, headerFont).Width / 2)), CSng(yPosition))
            yPosition += 24
            e.Graphics.DrawString("Dental Supply Management", regularFont, brush, CSng(centerX - (e.Graphics.MeasureString("Dental Supply Management", regularFont).Width / 2)), CSng(yPosition))
            yPosition += 14

            If Not String.IsNullOrEmpty(companyTIN) Then
                Dim tinLine = $"TIN: {companyTIN} (VAT Registered)"
                e.Graphics.DrawString(tinLine, regularFont, brush, CSng(centerX - (e.Graphics.MeasureString(tinLine, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            If Not String.IsNullOrEmpty(companyPhone) Then
                Dim telLine = $"Tel: {companyPhone}"
                e.Graphics.DrawString(telLine, regularFont, brush, CSng(centerX - (e.Graphics.MeasureString(telLine, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            If Not String.IsNullOrEmpty(companyAddress) Then
                e.Graphics.DrawString(companyAddress, regularFont, brush, CSng(centerX - (e.Graphics.MeasureString(companyAddress, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            If Not String.IsNullOrEmpty(companyWebsite) Then
                e.Graphics.DrawString(companyWebsite, regularFont, brush, CSng(centerX - (e.Graphics.MeasureString(companyWebsite, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 14
            End If

            e.Graphics.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 16

            e.Graphics.DrawString("SALES INVOICE", boldFont, brush, CSng(centerX - (e.Graphics.MeasureString("SALES INVOICE", boldFont).Width / 2)), CSng(yPosition))
            yPosition += 22
            e.Graphics.DrawString($"Receipt #: {DisplaySaleNumber()}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            e.Graphics.DrawString($"Date: {Convert.ToDateTime(saleRecord("SaleDate")):MM/dd/yyyy HH:mm:ss}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            e.Graphics.DrawString($"Cashier: {saleRecord("Cashier")}", regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            e.Graphics.DrawString("Customer Details:", regularFont, brush, marginLeft, yPosition)
            yPosition += 12

            Dim printedName As String = If(String.IsNullOrWhiteSpace(Convert.ToString(saleRecord("CustomerName"))), "________________", Convert.ToString(saleRecord("CustomerName")))
            Dim printedTIN As String = If(String.IsNullOrWhiteSpace(Convert.ToString(saleRecord("CustomerTIN"))), "________________", Convert.ToString(saleRecord("CustomerTIN")))
            Dim printedPhone As String = "________________"
            Dim printedEmail As String = "________________"

            Dim salesDataJson As String = Convert.ToString(saleRecord("SalesData"))
            If Not String.IsNullOrWhiteSpace(salesDataJson) Then
                Try
                    Dim j = Newtonsoft.Json.Linq.JObject.Parse(salesDataJson)
                    Dim p As String = j.SelectToken("customer.phone")?.ToString()
                    Dim m As String = j.SelectToken("customer.email")?.ToString()
                    If Not String.IsNullOrWhiteSpace(p) Then printedPhone = p
                    If Not String.IsNullOrWhiteSpace(m) Then printedEmail = m
                Catch
                End Try
            End If

            e.Graphics.DrawString($"Name: {printedName}", regularFont, brush, leftColX, yPosition)
            e.Graphics.DrawString($"TIN: {printedTIN}", regularFont, brush, rightColX, yPosition)
            yPosition += 12
            e.Graphics.DrawString($"Phone: {printedPhone}", regularFont, brush, leftColX, yPosition)
            e.Graphics.DrawString($"Email: {printedEmail}", regularFont, brush, rightColX, yPosition)
            yPosition += 14

            e.Graphics.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            Dim subtotalVatInclusive As Decimal = 0D
            For Each item In saleItems
                Dim itemName As String = Convert.ToString(item("ProductName"))
                Dim quantity As Integer = Convert.ToInt32(item("Quantity"))
                Dim unitVatInc As Decimal = Convert.ToDecimal(item("UnitPrice"))
                Dim lineTotal As Decimal = Math.Round(unitVatInc * quantity, 2)
                subtotalVatInclusive += lineTotal

                e.Graphics.DrawString($"{quantity}x {itemName}", regularFont, brush, marginLeft, yPosition)
                yPosition += 12
                e.Graphics.DrawString($"@ {ChrW(&H20B1)}{unitVatInc:F2}", regularFont, brush, marginLeft + 8, yPosition)
                e.Graphics.DrawString($"{ChrW(&H20B1)}{lineTotal:F2}", regularFont, brush, CSng(e.MarginBounds.Right - e.Graphics.MeasureString($"{ChrW(&H20B1)}{lineTotal:F2}", regularFont).Width), CSng(yPosition))
                yPosition += 15
            Next

            e.Graphics.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            Dim discountAmt As Decimal = 0D
            Dim discountTypeText As String = "None"
            Dim paymentMethod As String = Convert.ToString(saleRecord("PaymentMethod"))
            Dim paymentReference As String = Convert.ToString(saleRecord("Reference"))
            Dim amountReceived As Decimal = Convert.ToDecimal(saleRecord("AmountPaid"))

            If Not String.IsNullOrWhiteSpace(salesDataJson) Then
                Try
                    Dim j = Newtonsoft.Json.Linq.JObject.Parse(salesDataJson)
                    If j.SelectToken("payment.discount.amount") IsNot Nothing Then
                        discountAmt = j.SelectToken("payment.discount.amount").ToObject(Of Decimal)()
                    End If
                    If j.SelectToken("payment.discount.type") IsNot Nothing Then
                        discountTypeText = j.SelectToken("payment.discount.type").ToString()
                    End If
                    If j.SelectToken("payment.method") IsNot Nothing Then
                        paymentMethod = j.SelectToken("payment.method").ToString()
                    End If
                    If j.SelectToken("payment.reference") IsNot Nothing Then
                        paymentReference = j.SelectToken("payment.reference").ToString()
                    End If
                    If j.SelectToken("payment.received") IsNot Nothing Then
                        amountReceived = j.SelectToken("payment.received").ToObject(Of Decimal)()
                    End If
                Catch
                End Try
            End If

            Dim remainingVatInclusive As Decimal = Math.Max(0D, subtotalVatInclusive - discountAmt)
            Dim vatAmt As Decimal = Math.Round(remainingVatInclusive * (0.12D / 1.12D), 2)
            Dim vatableNet As Decimal = Math.Round(remainingVatInclusive - vatAmt, 2)
            Dim totalDue As Decimal = Math.Round(Convert.ToDecimal(saleRecord("TotalAmount")), 2)
            Dim changeAmount As Decimal = Math.Round(amountReceived - totalDue, 2)

            e.Graphics.DrawString("SUBTOTAL (VAT-INC):", regularFont, brush, marginLeft, yPosition)
            e.Graphics.DrawString($"{ChrW(&H20B1)}{subtotalVatInclusive:F2}", regularFont, brush, CSng(e.MarginBounds.Right - e.Graphics.MeasureString($"{ChrW(&H20B1)}{subtotalVatInclusive:F2}", regularFont).Width), CSng(yPosition))
            yPosition += 12

            If discountAmt > 0D Then
                e.Graphics.DrawString($"Less: Discount ({discountTypeText}):", regularFont, brush, marginLeft, yPosition)
                e.Graphics.DrawString($"-{ChrW(&H20B1)}{discountAmt:F2}", regularFont, brush, CSng(e.MarginBounds.Right - e.Graphics.MeasureString($"-{ChrW(&H20B1)}{discountAmt:F2}", regularFont).Width), CSng(yPosition))
                yPosition += 12
            End If

            e.Graphics.DrawString("VATABLE SALES (NET):", regularFont, brush, marginLeft, yPosition)
            e.Graphics.DrawString($"{ChrW(&H20B1)}{vatableNet:F2}", regularFont, brush, CSng(e.MarginBounds.Right - e.Graphics.MeasureString($"{ChrW(&H20B1)}{vatableNet:F2}", regularFont).Width), CSng(yPosition))
            yPosition += 12

            e.Graphics.DrawString("VAT (12%):", regularFont, brush, marginLeft, yPosition)
            e.Graphics.DrawString($"{ChrW(&H20B1)}{vatAmt:F2}", regularFont, brush, CSng(e.MarginBounds.Right - e.Graphics.MeasureString($"{ChrW(&H20B1)}{vatAmt:F2}", regularFont).Width), CSng(yPosition))
            yPosition += 12

            e.Graphics.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 12

            e.Graphics.DrawString("TOTAL AMOUNT DUE:", boldFont, brush, marginLeft, yPosition)
            e.Graphics.DrawString($"{ChrW(&H20B1)}{totalDue:F2}", boldFont, brush, CSng(e.MarginBounds.Right - e.Graphics.MeasureString($"{ChrW(&H20B1)}{totalDue:F2}", boldFont).Width), CSng(yPosition))
            yPosition += 18

            e.Graphics.DrawString("PAYMENT INFORMATION", sectionHeaderFont, brush, marginLeft, yPosition)
            yPosition += 14
            e.Graphics.DrawString($"Payment Method: {If(String.IsNullOrWhiteSpace(paymentMethod), "N/A", paymentMethod)}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            If Not String.IsNullOrWhiteSpace(paymentReference) Then
                e.Graphics.DrawString($"Reference: {paymentReference}", regularFont, brush, marginLeft, yPosition)
                yPosition += 12
            End If
            e.Graphics.DrawString($"Amount Received: {ChrW(&H20B1)}{amountReceived:F2}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            e.Graphics.DrawString($"Change: {ChrW(&H20B1)}{changeAmount:F2}", regularFont, brush, marginLeft, yPosition)
            yPosition += 14

            e.Graphics.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            e.Graphics.DrawString($"BIR Authority to Print No.: {birAuthNumber}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            e.Graphics.DrawString($"PTU No.: {ptuNumber}", regularFont, brush, marginLeft, yPosition)
            yPosition += 12
            e.Graphics.DrawString(New String("="c, Math.Min(36, CInt(contentWidth / 6))), regularFont, brush, marginLeft, yPosition)
            yPosition += 12

            Dim footerLines() As String = footerMessage.Split({vbCrLf, vbLf}, StringSplitOptions.RemoveEmptyEntries)
            For Each line As String In footerLines
                e.Graphics.DrawString(line, regularFont, brush, CSng(centerX - (e.Graphics.MeasureString(line, regularFont).Width / 2)), CSng(yPosition))
                yPosition += 12
            Next
        Catch ex As Exception
            e.Graphics.DrawString($"Receipt render error: {ex.Message}", New Font("Arial", 10), Brushes.Black, 10, 10)
        End Try
    End Sub
End Class