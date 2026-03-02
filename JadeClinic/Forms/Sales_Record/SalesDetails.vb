Imports Microsoft.Data.SqlClient
Imports System.Text

Public Class SalesDetails
    Private ReadOnly _saleId As Integer
    Private txtReceipt As TextBox

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
            LoadAndRenderReceipt(_saleId)
        Catch ex As Exception
            MessageBox.Show($"Error loading sale details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub LoadAndRenderReceipt(saleId As Integer)
        Dim saleRecord As Dictionary(Of String, Object) = Nothing
        Dim saleItems As New List(Of Dictionary(Of String, Object))()

        ' Load sale header
        Dim saleQuery As String = "SELECT s.SaleID, s.SaleDate, s.CustomerName, s.CustomerTIN, s.TotalAmount, s.AmountPaid, s.PaymentMethod, s.Reference, s.SalesData, u.Username " &
                                  "FROM Sales s LEFT JOIN Users u ON s.UserID = u.UserID WHERE s.SaleID = @SaleID"
        Using reader As SqlDataReader = Utilities.ExecuteReader(saleQuery, New SqlParameter("@SaleID", saleId))
            If reader.Read() Then
                saleRecord = New Dictionary(Of String, Object) From {
                    {"SaleID", If(IsDBNull(reader("SaleID")), saleId, reader("SaleID"))},
                    {"SaleDate", If(IsDBNull(reader("SaleDate")), DateTime.MinValue, Convert.ToDateTime(reader("SaleDate")))},
                    {"CustomerName", If(IsDBNull(reader("CustomerName")), "Walk-in Customer", reader("CustomerName").ToString())},
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

        If saleRecord Is Nothing Then
            txtReceipt.Text = $"Sale ID {saleId} not found."
            Return
        End If

        ' Load sale items
        Dim itemsQuery As String = "SELECT si.SaleItemID, si.ProductID, ISNULL(p.ProductName, 'Unknown') AS ProductName, si.Quantity, si.UnitPrice " &
                                   "FROM SaleItems si LEFT JOIN Products p ON si.ProductID = p.ProductID WHERE si.SaleID = @SaleID ORDER BY si.SaleItemID"
        Using reader As SqlDataReader = Utilities.ExecuteReader(itemsQuery, New SqlParameter("@SaleID", saleId))
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

        ' Build receipt text
        Dim sb As New StringBuilder()
        Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")
        Dim companyAddress As String = CompanySettingsManager.Instance.GetSettingString("Address", "")
        Dim companyPhone As String = CompanySettingsManager.Instance.GetSettingString("Phone", "")
        Dim companyTIN As String = CompanySettingsManager.Instance.GetSettingString("TIN", "")
        Dim birAuthNumber As String = CompanySettingsManager.Instance.GetSettingString("BIRAuthNumber", "ATP-2024-000001")
        Dim ptuNumber As String = CompanySettingsManager.Instance.GetSettingString("PTUNumber", "PTU-2024-001")
        Dim footer As String = CompanySettingsManager.Instance.GetSettingString("ReceiptFooter", "Thank you for your business!")

        sb.AppendLine("========================================")
        sb.AppendLine(companyName)
        sb.AppendLine("Dental Supply Management")
        If Not String.IsNullOrEmpty(companyTIN) Then sb.AppendLine($"TIN: {companyTIN} (VAT Registered)")
        If Not String.IsNullOrEmpty(companyPhone) Then sb.AppendLine($"Tel: {companyPhone}")
        If Not String.IsNullOrEmpty(companyAddress) Then sb.AppendLine(companyAddress)
        sb.AppendLine("========================================")
        sb.AppendLine("SALES INVOICE")
        sb.AppendLine($"Receipt #: {saleRecord("SaleID")}")
        sb.AppendLine($"Date: {Convert.ToDateTime(saleRecord("SaleDate")):MM/dd/yyyy HH:mm:ss}")
        sb.AppendLine($"Cashier: {saleRecord("Cashier")}")
        sb.AppendLine("----------------------------------------")
        sb.AppendLine("Customer Details:")
        sb.AppendLine($"Name: {If(String.IsNullOrWhiteSpace(Convert.ToString(saleRecord("CustomerName"))), "________________", Convert.ToString(saleRecord("CustomerName")))}")
        sb.AppendLine($"TIN: {If(String.IsNullOrWhiteSpace(Convert.ToString(saleRecord("CustomerTIN"))), "________________", Convert.ToString(saleRecord("CustomerTIN")))}")
        sb.AppendLine("----------------------------------------")

        Dim subtotalVatInc As Decimal = 0D
        For Each it In saleItems
            Dim qty As Integer = Convert.ToInt32(it("Quantity"))
            Dim unit As Decimal = Convert.ToDecimal(it("UnitPrice"))
            Dim lineTotal As Decimal = Math.Round(qty * unit, 2)
            subtotalVatInc += lineTotal

            sb.AppendLine($"{qty}x {it("ProductName")}")
            sb.AppendLine($"  @ ₱{unit:F2}      ₱{lineTotal:F2}")
        Next

        sb.AppendLine("----------------------------------------")

        Dim discountAmt As Decimal = 0D
        Dim discountTypeText As String = ""
        Dim vatAmt As Decimal = 0D
        Dim vatableNet As Decimal = 0D

        Dim salesDataJson As String = Convert.ToString(saleRecord("SalesData"))
        If Not String.IsNullOrEmpty(salesDataJson) Then
            Try
                Dim j = Newtonsoft.Json.Linq.JObject.Parse(salesDataJson)
                If j.SelectToken("payment.discount.amount") IsNot Nothing Then
                    discountAmt = j.SelectToken("payment.discount.amount").ToObject(Of Decimal)()
                End If
                If j.SelectToken("payment.discount.type") IsNot Nothing Then
                    discountTypeText = j.SelectToken("payment.discount.type").ToString()
                End If
            Catch
            End Try
        End If

        Dim remainingVatInc As Decimal = Math.Max(0D, subtotalVatInc - discountAmt)
        vatAmt = Math.Round(remainingVatInc * (0.12D / 1.12D), 2)
        vatableNet = Math.Round(remainingVatInc - vatAmt, 2)

        sb.AppendLine($"SUBTOTAL (VAT-INC): ₱{subtotalVatInc:F2}")
        If discountAmt > 0D Then
            Dim dLabel As String = If(String.IsNullOrWhiteSpace(discountTypeText), "Discount", $"Discount ({discountTypeText})")
            sb.AppendLine($"Less: {dLabel}: -₱{discountAmt:F2}")
        End If
        sb.AppendLine($"VATABLE SALES (NET): ₱{vatableNet:F2}")
        sb.AppendLine($"VAT (12%): ₱{vatAmt:F2}")
        sb.AppendLine("========================================")
        sb.AppendLine($"TOTAL AMOUNT DUE: ₱{Convert.ToDecimal(saleRecord("TotalAmount")):F2}")

        sb.AppendLine("PAYMENT INFORMATION")
        sb.AppendLine($"Payment Method: {If(String.IsNullOrWhiteSpace(Convert.ToString(saleRecord("PaymentMethod"))), "N/A", Convert.ToString(saleRecord("PaymentMethod")))}")
        If Not String.IsNullOrWhiteSpace(Convert.ToString(saleRecord("Reference"))) Then
            sb.AppendLine($"Reference: {saleRecord("Reference")}")
        End If
        sb.AppendLine($"Amount Received: ₱{Convert.ToDecimal(saleRecord("AmountPaid")):F2}")
        sb.AppendLine($"Change: ₱{(Convert.ToDecimal(saleRecord("AmountPaid")) - Convert.ToDecimal(saleRecord("TotalAmount"))):F2}")

        sb.AppendLine("----------------------------------------")
        sb.AppendLine($"BIR Authority to Print No.: {birAuthNumber}")
        sb.AppendLine($"PTU No.: {ptuNumber}")
        sb.AppendLine("========================================")
        sb.AppendLine(footer)
        sb.AppendLine("========================================")

        txtReceipt.Text = sb.ToString()
        Me.Text = $"Receipt - Sale #{saleId}"
    End Sub
End Class