Imports System.Data.Common
Imports System.Text
Imports System.Drawing.Printing

Imports Npgsql

Public Class SalesDetails
    Private ReadOnly _saleId As Integer
    Private txtReceipt As TextBox
    Private receiptScrollPanel As Panel
    Private receiptPreview As PictureBox

    Private saleRecord As Dictionary(Of String, Object) = Nothing
    Private saleItems As New List(Of Dictionary(Of String, Object))()

    Private returnFooter As Panel
    Private btnReturn As Guna.UI2.WinForms.Guna2Button
    Private lblReturnsSummary As Label

    Private ReadOnly JadeOlive As Color = Color.FromArgb(190, 154, 48)
    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)
    Private ReadOnly OffWhite As Color = Color.FromArgb(247, 246, 243)
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)
    Private ReadOnly DarkText As Color = Color.FromArgb(45, 47, 49)
    Private ReadOnly MediumText As Color = Color.FromArgb(120, 120, 120)
    Private ReadOnly BorderGray As Color = Color.FromArgb(228, 226, 221)
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 190, 96)

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
            BuildReturnFooter()
            RefreshReturnsSummary()
            Dim isAborted As Boolean = String.Equals(Convert.ToString(saleRecord("Status")), "Aborted", StringComparison.OrdinalIgnoreCase)
            Me.Text = $"{(If(isAborted, "Aborted Sale", "Receipt"))} - Sale #{DisplaySaleNumber()}"
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

        Dim saleQuery As String = "SELECT s.SaleID, COALESCE(s.SaleNumber, '') AS SaleNumber, s.SaleDate, s.CustomerName, s.CustomerTIN, s.TotalAmount, s.AmountPaid, s.PaymentMethod, s.Reference, s.SalesData, COALESCE(s.Status, 'Completed') AS Status, s.ApprovedBy, s.AbortReason, COALESCE(u.EmployeeCode, '') AS Cashier " &
                                  "FROM Sales s LEFT JOIN Users u ON s.UserID = u.UserID WHERE s.SaleID = @SaleID"
        Using reader As DbDataReader = Utilities.ExecuteReader(saleQuery, New NpgsqlParameter("@SaleID", saleId))
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
                    {"Status", If(IsDBNull(reader("Status")), "Completed", reader("Status").ToString())},
                    {"ApprovedBy", If(IsDBNull(reader("ApprovedBy")), "", reader("ApprovedBy").ToString())},
                    {"AbortReason", If(IsDBNull(reader("AbortReason")), "", reader("AbortReason").ToString())},
                    {"Cashier", If(IsDBNull(reader("Cashier")), "", reader("Cashier").ToString())}
                }
            End If
        End Using

        If saleRecord Is Nothing Then Return

        Dim itemsQuery As String = "SELECT si.SaleItemID, si.ProductID, COALESCE(p.ProductName, 'Unknown') AS ProductName, si.Quantity, si.UnitPrice " &
                                   "FROM SaleItems si LEFT JOIN Products p ON si.ProductID = p.ProductID WHERE si.SaleID = @SaleID ORDER BY si.SaleItemID"
        Using reader As DbDataReader = Utilities.ExecuteReader(itemsQuery, New NpgsqlParameter("@SaleID", saleId))
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

        If receiptScrollPanel Is Nothing Then
            receiptScrollPanel = New Panel() With {
                .Dock = DockStyle.Fill,
                .AutoScroll = True
            }
            Me.Controls.Add(receiptScrollPanel)
            receiptScrollPanel.BringToFront()
        End If

        If receiptPreview Is Nothing Then
            receiptPreview = New PictureBox() With {
                .SizeMode = PictureBoxSizeMode.Normal,
                .Location = New Point(0, 0),
                .BackColor = Color.White
            }
            receiptScrollPanel.Controls.Add(receiptPreview)
            AddHandler receiptScrollPanel.Resize, AddressOf RebuildReceipt
        End If

        ' Render the full-width, full-natural-height receipt so the preview
        ' scrolls instead of shrinking or clipping the footer, even with many
        ' line items.
        RebuildReceipt(receiptScrollPanel, EventArgs.Empty)
    End Sub

    Private Sub RebuildReceipt(sender As Object, e As EventArgs)
        If receiptScrollPanel Is Nothing OrElse saleRecord Is Nothing Then Return
        Dim panelWidth As Integer = Math.Max(200, receiptScrollPanel.ClientSize.Width - 8)
        Try
            ' Keep the full width even if the panel was previously sized.
            Dim bmp As Bitmap = ReceiptRenderer.RenderReceiptToBitmap(BuildReceiptData(), panelWidth)
            If receiptPreview.Image IsNot Nothing Then receiptPreview.Image.Dispose()
            receiptPreview.Image = bmp
            receiptPreview.Size = bmp.Size
            receiptPreview.Left = 4
            receiptPreview.Top = 4
        Catch ex As Exception
            MessageBox.Show($"Error rendering receipt: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
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

        ' Internal view: surface the voided-items trail and abort metadata.
        ' The printed customer receipt keeps ShowVoidedItems = False, so voided
        ' lines never appear on the customer copy.
        data.ShowVoidedItems = True
        data.IsAborted = String.Equals(Convert.ToString(saleRecord("Status")), "Aborted", StringComparison.OrdinalIgnoreCase)
        PopulateVoidedItemsFromSalesData(data, salesDataJson)

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

    ' Reads the buffered voided lines from the SalesData JSON (voidedItems array)
    ' and exposes them as internal-only receipt line items for the audit trail.
    Private Sub PopulateVoidedItemsFromSalesData(data As ReceiptData, salesDataJson As String)
        Try
            If String.IsNullOrWhiteSpace(salesDataJson) Then Return
            Dim j = Newtonsoft.Json.Linq.JObject.Parse(salesDataJson)
            Dim arr = TryCast(j.SelectToken("voidedItems"), Newtonsoft.Json.Linq.JArray)
            If arr Is Nothing Then Return

            For Each token In arr
                Dim nameTok = token.SelectToken("ProductName")
                Dim name As String = If(nameTok IsNot Nothing, nameTok.ToString(), "Unknown")
                Dim qtyTok = token.SelectToken("Quantity")
                Dim qty As Integer = 0
                If qtyTok IsNot Nothing Then Integer.TryParse(qtyTok.ToString(), qty)
                Dim priceTok = token.SelectToken("UnitPrice")
                Dim unitPrice As Decimal = 0D
                If priceTok IsNot Nothing Then Decimal.TryParse(priceTok.ToString(), unitPrice)
                Dim appTok = token.SelectToken("ApprovedBy")
                Dim approvedBy As String = If(appTok IsNot Nothing, appTok.ToString(), "")
                If Not String.IsNullOrWhiteSpace(approvedBy) Then
                    name &= $" (by {approvedBy})"
                End If

                data.VoidedItems.Add(New ReceiptLineItem() With {
                    .ProductName = name,
                    .Quantity = qty,
                    .UnitVatInc = unitPrice,
                    .LineTotal = Math.Round(unitPrice * qty, 2)
                })
            Next
        Catch
            ' Malformed/legacy SalesData without a voidedItems array: ignore.
        End Try
    End Sub

    ' -----------------------------------------------------------------------
    '  Returns / Exchanges footer
    ' -----------------------------------------------------------------------
    Private Sub BuildReturnFooter()
        If returnFooter IsNot Nothing Then Return

        returnFooter = New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 84,
            .BackColor = OffWhite,
            .Padding = New Padding(14, 12, 14, 12)
        }

        Dim summaryCard As New Panel() With {
            .Dock = DockStyle.Fill,
            .BackColor = PureWhite,
            .Padding = New Padding(14, 8, 14, 8)
        }
        AddHandler summaryCard.Paint, Sub(s, e)
                                          Using p As New Pen(BorderGray, 1)
                                              e.Graphics.DrawRectangle(p, 0, 0, summaryCard.ClientSize.Width - 1, summaryCard.ClientSize.Height - 1)
                                          End Using
                                      End Sub

        Dim lblCaption As New Label() With {
            .Text = "RETURNS",
            .Font = New Font("Poppins", 7.5F, FontStyle.Bold),
            .ForeColor = JadeOlive,
            .AutoSize = True,
            .Location = New Point(14, 8)
        }
        lblReturnsSummary = New Label() With {
            .Text = "Checking...",
            .Font = New Font("Poppins", 9.0F, FontStyle.Regular),
            .ForeColor = DarkText,
            .AutoSize = False,
            .Size = New Size(220, 34),
            .Location = New Point(14, 26)
        }
        summaryCard.Controls.Add(lblCaption)
        summaryCard.Controls.Add(lblReturnsSummary)

        Dim canReturn As Boolean = String.Equals(Convert.ToString(saleRecord("Status")), "Completed", StringComparison.OrdinalIgnoreCase) AndAlso
                                   frmLoginvb.LoggedInUserID > 0

        btnReturn = New Guna.UI2.WinForms.Guna2Button() With {
            .Text = "Return/Exchange",
            .Size = New Size(210, 44),
            .Dock = DockStyle.Right,
            .BorderRadius = 8,
            .FillColor = JadeOlive,
            .ForeColor = PureWhite,
            .Font = New Font("Poppins", 9.0F, FontStyle.Bold),
            .TextAlign = HorizontalAlignment.Center,
            .Padding = New Padding(0),
            .Cursor = Cursors.Hand,
            .Visible = canReturn
        }
        btnReturn.ShadowDecoration.Enabled = False
        AddHandler btnReturn.Click, AddressOf OpenReturnDialog

        ' Right-docked button must be added before the fill card.
        returnFooter.Controls.Add(summaryCard)
        returnFooter.Controls.Add(btnReturn)
        Me.Controls.Add(returnFooter)
    End Sub

    Private Sub RefreshReturnsSummary()
        If lblReturnsSummary Is Nothing Then Return
        Try
            Dim count As Integer = 0
            Dim refund As Decimal = 0D
            Dim collected As Decimal = 0D
            Dim exchanged As Integer = 0
            Dim remaining As Integer = 0
            Dim sql As String =
                "SELECT " &
                "(SELECT COUNT(*) FROM SalesReturns WHERE SaleID = @SaleID AND Status = 'Completed') AS Cnt, " &
                "(SELECT COALESCE(SUM(RefundAmount),0) FROM SalesReturns WHERE SaleID = @SaleID AND Status = 'Completed') AS Refund, " &
                "(SELECT COALESCE(SUM(AdditionalPaymentAmount),0) FROM SalesReturns WHERE SaleID = @SaleID AND Status = 'Completed') AS Collected, " &
                "(SELECT COALESCE(SUM(sri.ReplacementQuantity),0) FROM SalesReturnItems sri JOIN SalesReturns sr ON sr.ReturnID = sri.ReturnID WHERE sr.SaleID = @SaleID AND sr.Status = 'Completed') AS Exchanged, " &
                "(SELECT GREATEST(0, COALESCE(SUM(si.Quantity),0) - COALESCE((SELECT SUM(sri.Quantity) FROM SalesReturnItems sri JOIN SalesReturns sr ON sr.ReturnID = sri.ReturnID WHERE sr.SaleID = @SaleID AND sr.Status = 'Completed'),0)) FROM SaleItems si WHERE si.SaleID = @SaleID) AS Remaining"
            Using reader As DbDataReader = Utilities.ExecuteReader(sql, New NpgsqlParameter("@SaleID", _saleId))
                If reader.Read() Then
                    count = Convert.ToInt32(reader("Cnt"))
                    refund = Convert.ToDecimal(reader("Refund"))
                    collected = Convert.ToDecimal(reader("Collected"))
                    exchanged = Convert.ToInt32(reader("Exchanged"))
                    remaining = Convert.ToInt32(reader("Remaining"))
                End If
            End Using

            If btnReturn IsNot Nothing Then
                btnReturn.Enabled = remaining > 0
                btnReturn.Text = If(remaining > 0, "Return/Exchange", "Fully Returned")
            End If

            If count = 0 Then
                lblReturnsSummary.Text = "No returns recorded for this sale."
                lblReturnsSummary.ForeColor = MediumText
            Else
                Dim refundText As String = If(refund > 0, $" | Refunded {ChrW(&H20B1)}{refund:N2}", "")
                Dim collectedText As String = If(collected > 0, $" | Collected {ChrW(&H20B1)}{collected:N2}", "")
                Dim exchangeText As String = If(exchanged > 0, $" | {exchanged} exchanged", "")
                lblReturnsSummary.Text = $"{count} return(s){refundText}{collectedText}{exchangeText}"
                lblReturnsSummary.ForeColor = DarkText
            End If
        Catch ex As Exception
            lblReturnsSummary.Text = "Unable to read returns."
            lblReturnsSummary.ForeColor = MediumText
        End Try
    End Sub

    Private Sub OpenReturnDialog(sender As Object, e As EventArgs)
        Try
            Using dlg As New ReturnExchangeForm(_saleId)
                Utilities.EnableEscCloseModal(dlg)
                If dlg.ShowDialog(Me) = DialogResult.OK Then
                    LoadSaleData(_saleId)
                    RefreshReturnsSummary()
                    If receiptScrollPanel IsNot Nothing Then
                        RebuildReceipt(receiptScrollPanel, EventArgs.Empty)
                    End If
                End If
            End Using
        Catch ex As Exception
            MessageBox.Show($"Unable to open the return window: {ex.Message}", "Return / Exchange", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub
End Class
