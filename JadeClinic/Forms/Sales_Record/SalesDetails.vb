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

        sb.AppendLine("========================================")
        sb.AppendLine($"                {companyName}")
        If Not String.IsNullOrEmpty(companyAddress) Then sb.AppendLine($"        {companyAddress}")
        If Not String.IsNullOrEmpty(companyPhone) Then sb.AppendLine($"        Tel: {companyPhone}")
        If Not String.IsNullOrEmpty(companyTIN) Then sb.AppendLine($"        TIN: {companyTIN}")
        sb.AppendLine("========================================")
        sb.AppendLine($"RECEIPT #: {saleRecord("SaleID")}")
        sb.AppendLine($"Date: {Convert.ToDateTime(saleRecord("SaleDate")):MM/dd/yyyy HH:mm}")
        sb.AppendLine($"Cashier: {saleRecord("Cashier")}")
        sb.AppendLine($"Customer: {saleRecord("CustomerName")}")
        If Not String.IsNullOrEmpty(Convert.ToString(saleRecord("CustomerTIN"))) Then
            sb.AppendLine($"Customer TIN: {saleRecord("CustomerTIN")}")
        End If
        If Not String.IsNullOrEmpty(Convert.ToString(saleRecord("Reference"))) Then
            sb.AppendLine($"Reference: {saleRecord("Reference")}")
        End If
        sb.AppendLine("----------------------------------------")
        sb.AppendLine("QTY  ITEM                          AMOUNT")
        sb.AppendLine("----------------------------------------")

        Dim subtotal As Decimal = 0D
        For Each it In saleItems
            Dim qty As Integer = Convert.ToInt32(it("Quantity"))
            Dim unit As Decimal = Convert.ToDecimal(it("UnitPrice"))
            Dim lineTotal As Decimal = Math.Round(qty * unit, 2)
            subtotal += lineTotal

            Dim name As String = it("ProductName").ToString()
            If name.Length > 26 Then name = name.Substring(0, 26) & "…"
            sb.AppendFormat("{0,-4} {1,-26} {2,8}", qty.ToString(), name, $"₱{lineTotal:F2}")
            sb.AppendLine()
        Next

        sb.AppendLine("----------------------------------------")
        sb.AppendFormat("{0,-32} {1,8}", "SUBTOTAL:", $"₱{subtotal:F2}")
        sb.AppendLine()
        ' Try to parse salesdata JSON for discount/tax/other info if present
        Dim salesDataJson As String = Convert.ToString(saleRecord("SalesData"))
        Dim discountLine As String = ""
        Dim taxLine As String = ""
        If Not String.IsNullOrEmpty(salesDataJson) Then
            Try
                Dim j = Newtonsoft.Json.Linq.JObject.Parse(salesDataJson)
                If j.SelectToken("payment.discount.amount") IsNot Nothing Then
                    Dim dAmt As Decimal = j.SelectToken("payment.discount.amount").ToObject(Of Decimal)()
                    Dim dType As String = If(j.SelectToken("payment.discount.type") IsNot Nothing, j.SelectToken("payment.discount.type").ToString(), "")
                    discountLine = $"{If(String.IsNullOrEmpty(dType), "Discount", $"Discount ({dType})")}: -₱{dAmt:F2}"
                    sb.AppendFormat("{0,-32} {1,8}", If(String.IsNullOrEmpty(dType), "Discount:", $"Discount ({dType}):"), $"-₱{dAmt:F2}")
                    sb.AppendLine()
                    subtotal = Math.Max(0D, subtotal - dAmt)
                End If
                If j.SelectToken("payment.tax") IsNot Nothing Then
                    Dim tax As Decimal = j.SelectToken("payment.tax").ToObject(Of Decimal)()
                    sb.AppendFormat("{0,-32} {1,8}", "VAT (12%):", $"₱{tax:F2}")
                    sb.AppendLine()
                End If
            Catch
                ' ignore JSON parse errors
            End Try
        End If

        sb.AppendFormat("{0,-32} {1,8}", "TOTAL:", $"₱{Convert.ToDecimal(saleRecord("TotalAmount")):F2}")
        sb.AppendLine()
        sb.AppendFormat("{0,-32} {1,8}", "Amount Paid:", $"₱{Convert.ToDecimal(saleRecord("AmountPaid")):F2}")
        sb.AppendLine()
        Dim changeVal = Convert.ToDecimal(saleRecord("AmountPaid")) - Convert.ToDecimal(saleRecord("TotalAmount"))
        sb.AppendFormat("{0,-32} {1,8}", "Change:", $"₱{changeVal:F2}")
        sb.AppendLine()
        sb.AppendLine("========================================")
        Dim footer As String = CompanySettingsManager.Instance.GetSettingString("ReceiptFooter", "Thank you for your business!")
        sb.AppendLine(footer)
        sb.AppendLine("========================================")

        txtReceipt.Text = sb.ToString()
        Me.Text = $"Receipt - Sale #{saleId}"
    End Sub
End Class