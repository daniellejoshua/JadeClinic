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

    Private Function BuildReceiptData() As ReceiptData
        Dim data As New ReceiptData()
        If saleRecord Is Nothing Then Return data

        data.ReceiptNumber = DisplaySaleNumber()
        data.SaleDate = Convert.ToDateTime(saleRecord("SaleDate"))
        data.Cashier = Convert.ToString(saleRecord("Cashier"))

        data.CustomerName = If(String.IsNullOrWhiteSpace(Convert.ToString(saleRecord("CustomerName"))), "________________", Convert.ToString(saleRecord("CustomerName")))
        data.CustomerTIN = If(String.IsNullOrWhiteSpace(Convert.ToString(saleRecord("CustomerTIN"))), "________________", Convert.ToString(saleRecord("CustomerTIN")))
        data.CustomerPhone = "________________"
        data.CustomerEmail = "________________"

        Dim salesDataJson As String = Convert.ToString(saleRecord("SalesData"))
        If Not String.IsNullOrWhiteSpace(salesDataJson) Then
            Try
                Dim j = Newtonsoft.Json.Linq.JObject.Parse(salesDataJson)
                Dim p As String = j.SelectToken("customer.phone")?.ToString()
                Dim m As String = j.SelectToken("customer.email")?.ToString()
                If Not String.IsNullOrWhiteSpace(p) Then data.CustomerPhone = p
                If Not String.IsNullOrWhiteSpace(m) Then data.CustomerEmail = m
            Catch
            End Try
        End If

        Dim subtotalVatInclusive As Decimal = 0D
        For Each item In saleItems
            Dim itemName As String = Convert.ToString(item("ProductName"))
            Dim quantity As Integer = Convert.ToInt32(item("Quantity"))
            Dim unitVatInc As Decimal = Convert.ToDecimal(item("UnitPrice"))
            Dim lineTotal As Decimal = Math.Round(unitVatInc * quantity, 2)
            subtotalVatInclusive += lineTotal

            data.Items.Add(New ReceiptLineItem() With {
                .ProductName = itemName,
                .Quantity = quantity,
                .UnitVatInc = unitVatInc,
                .LineTotal = lineTotal
            })
        Next
        data.SubtotalVatInclusive = Math.Round(subtotalVatInclusive, 2)

        Dim discountAmt As Decimal = 0D
        Dim discountTypeText As String = "None"
        data.PaymentMethod = Convert.ToString(saleRecord("PaymentMethod"))
        data.PaymentReference = Convert.ToString(saleRecord("Reference"))
        data.AmountReceived = Convert.ToDecimal(saleRecord("AmountPaid"))

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
                    data.PaymentMethod = j.SelectToken("payment.method").ToString()
                End If
                If j.SelectToken("payment.reference") IsNot Nothing Then
                    data.PaymentReference = j.SelectToken("payment.reference").ToString()
                End If
                If j.SelectToken("payment.received") IsNot Nothing Then
                    data.AmountReceived = j.SelectToken("payment.received").ToObject(Of Decimal)()
                End If
            Catch
            End Try
        End If

        data.DiscountAmount = discountAmt
        data.DiscountType = discountTypeText

        Dim remainingVatInclusive As Decimal = Math.Max(0D, data.SubtotalVatInclusive - discountAmt)
        data.VatAmount = Math.Round(remainingVatInclusive * (0.12D / 1.12D), 2)
        data.VatableNet = Math.Round(remainingVatInclusive - data.VatAmount, 2)
        data.TotalDue = Math.Round(Convert.ToDecimal(saleRecord("TotalAmount")), 2)
        data.Change = Math.Round(data.AmountReceived - data.TotalDue, 2)

        Return data
    End Function

    Private Sub ReceiptDocument_PrintPage(sender As Object, e As PrintPageEventArgs)
        Try
            If saleRecord Is Nothing Then
                e.Graphics.DrawString("No receipt data available.", New Font("Arial", 10), Brushes.Black, 10, 10)
                Return
            End If

            ReceiptRenderer.DrawReceipt(e.Graphics, e.MarginBounds, BuildReceiptData())
        Catch ex As Exception
            e.Graphics.DrawString($"Receipt render error: {ex.Message}", New Font("Arial", 10), Brushes.Black, 10, 10)
        End Try
    End Sub
End Class