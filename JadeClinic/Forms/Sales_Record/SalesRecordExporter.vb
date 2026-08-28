Imports System.IO
Imports System.Linq
Imports System.Data.Common
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Public Class SalesRecordExporter

    Shared Sub New()
        QuestPDF.Settings.License = LicenseType.Community
    End Sub

    Public Shared Sub ExportOrderRecordsReport(Optional sortOrder As String = "", Optional userFilter As String = Nothing, Optional filterDate As DateTime? = Nothing, Optional searchTerm As String = "")
        Try
            Dim orderReportsPath As String = Path.Combine(Application.StartupPath, "orderreports")
            If Not Directory.Exists(orderReportsPath) Then
                Directory.CreateDirectory(orderReportsPath)
            End If

            Dim query As String = "SELECT s.SaleID, u.Username, s.SaleDate, s.TotalAmount, s.AmountPaid, " &
                                  "(s.AmountPaid - s.TotalAmount) AS ChangeAmount, " &
                                  "IFNULL(s.PaymentMethod, '') AS PaymentMethod, " &
                                  "IFNULL(s.DiscountType, '') AS DiscountType, " &
                                  "IFNULL(s.DiscountAmount, 0) AS DiscountAmount, " &
                                   "IFNULL((SELECT SUM(si.Quantity * IFNULL(p.CostPrice, 0)) " &
                                   "        FROM SaleItems si " &
                                   "        LEFT JOIN Products p ON p.ProductID = si.ProductID " &
                                   "        WHERE si.SaleID = s.SaleID), 0) AS TotalCost, " &
                                   "(s.TotalAmount - IFNULL(s.DiscountAmount, 0)) - IFNULL((SELECT SUM(si.Quantity * IFNULL(p.CostPrice, 0)) " &
                                   "                                             FROM SaleItems si " &
                                   "                                             LEFT JOIN Products p ON p.ProductID = si.ProductID " &
                                   "                                             WHERE si.SaleID = s.SaleID), 0) AS ProfitAmount " &
                                  "FROM Sales s " &
                                  "LEFT JOIN Users u ON s.UserID = u.UserID"

            Dim whereClauses As New List(Of String)()
            Dim parameters As New List(Of SqlParameter)()

            If Not String.IsNullOrWhiteSpace(userFilter) AndAlso userFilter <> "All Users" Then
                whereClauses.Add("u.Username = @Username")
                parameters.Add(New SqlParameter("@Username", userFilter))
            End If

            If filterDate.HasValue Then
                whereClauses.Add("DATE(s.SaleDate) = @FilterDate")
                parameters.Add(New SqlParameter("@FilterDate", filterDate.Value.Date.ToString("yyyy-MM-dd")))
            End If

            If Not String.IsNullOrWhiteSpace(searchTerm) Then
                whereClauses.Add("(IFNULL(s.SaleNumber, '') LIKE @Search OR IFNULL(u.Username, '') LIKE @Search)")
                parameters.Add(New SqlParameter("@Search", "%" & searchTerm & "%"))
            End If

            If whereClauses.Count > 0 Then
                query &= " WHERE " & String.Join(" AND ", whereClauses)
            End If

            Select Case sortOrder
                Case "Sale Date (Newest First)", "Order Date (Newest First)"
                    query &= " ORDER BY s.SaleDate DESC"
                Case "Sale Date (Oldest First)", "Order Date (Oldest First)"
                    query &= " ORDER BY s.SaleDate ASC"
                Case "Sale ID (Ascending)", "Order ID (Ascending)"
                    query &= " ORDER BY s.SaleID ASC"
                Case "Sale ID (Descending)", "Order ID (Descending)"
                    query &= " ORDER BY s.SaleID DESC"
                Case "Total Amount (Highest First)"
                    query &= " ORDER BY s.TotalAmount DESC"
                Case "Total Amount (Lowest First)"
                    query &= " ORDER BY s.TotalAmount ASC"
                Case "Created By (A-Z)", "Cashier (A-Z)"
                    query &= " ORDER BY u.Username ASC"
                Case "Created By (Z-A)", "Cashier (Z-A)"
                    query &= " ORDER BY u.Username DESC"
                Case Else
                    query &= " ORDER BY s.SaleDate DESC"
            End Select

            Dim orderDataList As New List(Of OrderReportData)()

            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters.ToArray())
                While reader.Read()
                    Dim orderData As New OrderReportData() With {
                        .OrderID = If(IsDBNull(reader("SaleID")), 0, Convert.ToInt32(reader("SaleID"))),
                        .CreatedBy = If(IsDBNull(reader("Username")), "Unknown", reader("Username").ToString()),
                        .OrderDate = If(IsDBNull(reader("SaleDate")), DateTime.MinValue, Convert.ToDateTime(reader("SaleDate"))),
                        .TotalAmount = If(IsDBNull(reader("TotalAmount")), 0D, Convert.ToDecimal(reader("TotalAmount"))),
                        .TotalReceived = If(IsDBNull(reader("AmountPaid")), 0D, Convert.ToDecimal(reader("AmountPaid"))),
                        .Change = If(IsDBNull(reader("ChangeAmount")), 0D, Convert.ToDecimal(reader("ChangeAmount"))),
                        .PaymentMethod = If(IsDBNull(reader("PaymentMethod")), "", reader("PaymentMethod").ToString()),
                        .DiscountType = If(IsDBNull(reader("DiscountType")), "None", reader("DiscountType").ToString()),
                        .DiscountAmount = If(IsDBNull(reader("DiscountAmount")), 0D, Convert.ToDecimal(reader("DiscountAmount"))),
                        .TotalCost = If(IsDBNull(reader("TotalCost")), 0D, Convert.ToDecimal(reader("TotalCost"))),
                        .ProfitAmount = If(IsDBNull(reader("ProfitAmount")), 0D, Convert.ToDecimal(reader("ProfitAmount")))
                    }

                    If orderData.OrderID > 0 AndAlso orderData.OrderDate <> DateTime.MinValue Then
                        orderDataList.Add(orderData)
                    End If
                End While
            End Using

            If orderDataList.Count = 0 Then
                MessageBox.Show("No sales records found to export.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim reportCapital As Decimal = GetReportCapital(filterDate)
            Dim totalSales As Decimal = orderDataList.Sum(Function(o) o.TotalAmount)
            Dim expectedAmount As Decimal = reportCapital + totalSales

            Dim dateSuffix As String = If(filterDate.HasValue, $"_Date_{filterDate.Value:yyyyMMdd}", "")
            Dim fileName As String = $"Sales_Records_Report{dateSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            Dim fullPath As String = Path.Combine(orderReportsPath, fileName)

            Dim filterDesc As String = "All Sales"
            If Not String.IsNullOrWhiteSpace(userFilter) AndAlso userFilter <> "All Users" Then
                filterDesc = $"User: {userFilter}"
            End If
            If filterDate.HasValue Then
                filterDesc &= $", Date: {filterDate.Value:yyyy-MM-dd}"
            End If
            If Not String.IsNullOrWhiteSpace(searchTerm) Then
                filterDesc &= $", Search: ""{searchTerm}"""
            End If

            CreateQuestPDFOrderRecordsReport(orderDataList, fullPath, filterDesc, reportCapital, expectedAmount, filterDate)

            If Not File.Exists(fullPath) Then
                Throw New Exception("PDF file was not created successfully.")
            End If

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Sales Records Report Exported", $"Filter: {filterDesc}, Records: {orderDataList.Count}")
            End If

            Dim dateFilterMessage As String = If(filterDate.HasValue, $"{vbCrLf}Date Filter: {filterDate.Value:yyyy-MM-dd}", $"{vbCrLf}Date Filter: All dates")
            MessageBox.Show($"Sales records report exported successfully!{vbCrLf}Filter Applied: {filterDesc}{dateFilterMessage}{vbCrLf}Records Exported: {orderDataList.Count}{vbCrLf}Opening PDF now...",
                            "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Try
                Process.Start(New ProcessStartInfo(fullPath) With {.UseShellExecute = True})
            Catch
                MessageBox.Show($"PDF created successfully but couldn't open automatically.{vbCrLf}File location: {fullPath}",
                                "PDF Created", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try

        Catch ex As Exception
            Dim errorMessage As String = $"Error exporting sales records report: {ex.Message}"
            If ex.InnerException IsNot Nothing Then
                errorMessage += $"{vbCrLf}Details: {ex.InnerException.Message}"
            End If

            MessageBox.Show(errorMessage, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Sales Records Report Export Failed", $"Error: {ex.Message}")
            End If
        End Try
    End Sub

    Private Shared Function GetReportCapital(Optional filterDate As DateTime? = Nothing) As Decimal
        Try
            Dim targetDate As Date = If(filterDate.HasValue, filterDate.Value.Date, Date.Today)
            Dim capitalObj = Utilities.ExecuteScalar("SELECT OpeningAmount FROM DailyOpeningCapital WHERE CashDate = @CashDate", New SqlParameter("@CashDate", targetDate))
            If capitalObj Is Nothing OrElse capitalObj Is DBNull.Value Then
                Return 0D
            End If
            Return Convert.ToDecimal(capitalObj)
        Catch
            Return 0D
        End Try
    End Function

    Private Shared Sub CreateQuestPDFOrderRecordsReport(orderDataList As List(Of OrderReportData), filePath As String, filterType As String, reportCapital As Decimal, expectedAmount As Decimal, Optional filterDate As DateTime? = Nothing)
        Dim tempLogoPath As String = Nothing

        Try
            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")
            Dim companyTagline As String = CompanySettingsManager.Instance.GetSettingString("CompanyTagline", "Professional Order Management & Transaction Reporting")
            Dim companyPhone As String = CompanySettingsManager.Instance.GetSettingString("Phone", "")
            Dim companyAddress As String = CompanySettingsManager.Instance.GetSettingString("Address", "")
            Dim companyTIN As String = CompanySettingsManager.Instance.GetSettingString("TIN", "")

            ' Save logo to a temporary file for QuestPDF image usage
            Try
                Dim logoImg As System.Drawing.Image = CompanySettingsManager.Instance.GetCompanyLogo()
                If logoImg IsNot Nothing Then
                    Dim outDir = Path.GetDirectoryName(filePath)
                    If String.IsNullOrEmpty(outDir) Then outDir = Application.StartupPath
                    tempLogoPath = Path.Combine(outDir, "sales_report_logo.png")
                    logoImg.Save(tempLogoPath, System.Drawing.Imaging.ImageFormat.Png)
                End If
            Catch
                tempLogoPath = Nothing
            End Try

            Dim perUser = orderDataList.
                GroupBy(Function(x) If(String.IsNullOrWhiteSpace(x.CreatedBy), "Unknown", x.CreatedBy)).
                Select(Function(g) New With {
                    .UserName = g.Key,
                    .Counter = g.Count(),
                    .Amount = g.Sum(Function(x) x.TotalAmount),
                    .Average = If(g.Count() > 0, g.Sum(Function(x) x.TotalAmount) / g.Count(), 0D)
                }).
                OrderByDescending(Function(x) x.Counter).
                ToList()

            ' Expanded analytics
            Dim totalCounter As Integer = orderDataList.Count
            Dim totalRevenue As Decimal = orderDataList.Sum(Function(o) o.TotalAmount)
            Dim totalCost As Decimal = orderDataList.Sum(Function(o) o.TotalCost)
            Dim totalProfit As Decimal = orderDataList.Sum(Function(o) o.ProfitAmount)
            Dim totalReceived As Decimal = orderDataList.Sum(Function(o) o.TotalReceived)
            Dim totalChange As Decimal = orderDataList.Sum(Function(o) o.Change)
            Dim totalDiscounts As Decimal = orderDataList.Sum(Function(o) o.DiscountAmount)
            Dim ordersWithDiscount As Integer = orderDataList.Where(Function(o) o.DiscountAmount > 0D).Count()
            Dim discountRate As Decimal = If(totalCounter > 0, CDec(ordersWithDiscount) / CDec(totalCounter) * 100D, 0D)
            Dim highestSale As Decimal = If(totalCounter > 0, orderDataList.Max(Function(o) o.TotalAmount), 0D)
            Dim lowestSale As Decimal = If(totalCounter > 0, orderDataList.Min(Function(o) o.TotalAmount), 0D)

            Dim cashAmount As Decimal = orderDataList.Where(Function(o) o.PaymentMethod IsNot Nothing AndAlso o.PaymentMethod.Trim().Equals("Cash", StringComparison.OrdinalIgnoreCase)).Sum(Function(o) o.TotalReceived)
            Dim gcashAmount As Decimal = orderDataList.Where(Function(o) o.PaymentMethod IsNot Nothing AndAlso o.PaymentMethod.Trim().Equals("GCash", StringComparison.OrdinalIgnoreCase)).Sum(Function(o) o.TotalReceived)
            Dim cardAmount As Decimal = orderDataList.Where(Function(o) o.PaymentMethod IsNot Nothing AndAlso o.PaymentMethod.Trim().Equals("Card", StringComparison.OrdinalIgnoreCase)).Sum(Function(o) o.TotalReceived)
            Dim otherPaymentAmount As Decimal = orderDataList.Where(Function(o)
                                                                        Dim pm = If(o.PaymentMethod, "").Trim().ToLowerInvariant()
                                                                        Return pm <> "cash" AndAlso pm <> "gcash" AndAlso pm <> "card"
                                                                    End Function).Sum(Function(o) o.TotalReceived)

            Document.Create(Sub(container)
                                container.Page(Sub(page)
                                                   page.Size(PageSizes.A4.Landscape())
                                                   page.Margin(1.5F, Unit.Centimetre)
                                                   page.PageColor(Colors.White)
                                                   page.DefaultTextStyle(Function(x) x.FontSize(9))

                                                   ' Improved header style (logo + branding + metadata)
                                                   page.Header().Column(Sub(h)
                                                                            h.Item().Row(Sub(r)
                                                                                             If Not String.IsNullOrEmpty(tempLogoPath) AndAlso File.Exists(tempLogoPath) Then
                                                                                                 r.ConstantItem(58).Height(36).Image(tempLogoPath)
                                                                                             Else
                                                                                                 r.ConstantItem(58).Height(36).Text("")
                                                                                             End If

                                                                                             r.RelativeItem().PaddingLeft(8).Column(Sub(c)
                                                                                                                                        c.Item().Text(companyName).FontSize(16).SemiBold().FontColor(Colors.Orange.Medium)
                                                                                                                                        c.Item().Text(companyTagline).FontSize(9).Italic().FontColor(Colors.Grey.Medium)
                                                                                                                                    End Sub)
                                                                                         End Sub)

                                                                            h.Item().PaddingTop(12).AlignCenter().Text("SALES RECORDS & TRANSACTION REPORT").FontSize(18).SemiBold().FontColor(Colors.Grey.Darken3)

                                                                            h.Item().PaddingTop(8).Row(Sub(infoRow)
                                                                                                           infoRow.RelativeItem().Text($"Generated: {DateTime.Now:dddd, MMMM dd, yyyy} at {DateTime.Now:hh:mm tt} by {frmLoginvb.LoggedInUsername}").FontSize(8).FontColor(Colors.Grey.Medium)
                                                                                                           infoRow.RelativeItem().AlignRight().Text($"Filter: {filterType}").FontSize(8).SemiBold().FontColor(Colors.Orange.Medium)
                                                                                                       End Sub)

                                                                            h.Item().Row(Sub(metaRow)
                                                                                             metaRow.RelativeItem().Text($"Date: {If(filterDate.HasValue, filterDate.Value.ToString("yyyy-MM-dd"), "All dates")}").FontSize(8).FontColor(Colors.Grey.Medium)
                                                                                             metaRow.RelativeItem().AlignRight().Text($"Tel: {If(String.IsNullOrWhiteSpace(companyPhone), "N/A", companyPhone)} | TIN: {If(String.IsNullOrWhiteSpace(companyTIN), "N/A", companyTIN)}").FontSize(7).FontColor(Colors.Grey.Medium)
                                                                                         End Sub)

                                                                            If Not String.IsNullOrWhiteSpace(companyAddress) Then
                                                                                h.Item().Text(companyAddress).FontSize(7).FontColor(Colors.Grey.Medium)
                                                                            End If
                                                                        End Sub)

                                                   page.Content().PaddingTop(12).Column(Sub(column)
                                                                                            column.Item().Table(Sub(table)
                                                                                                                    table.ColumnsDefinition(Sub(c)
                                                                                                                                                c.ConstantColumn(40)
                                                                                                                                                c.RelativeColumn(2)
                                                                                                                                                c.RelativeColumn(2.2F)
                                                                                                                                                c.RelativeColumn(1.5F)
                                                                                                                                                c.RelativeColumn(1.8F)
                                                                                                                                                c.RelativeColumn(1.8F)
                                                                                                                                                c.RelativeColumn(1.6F)
                                                                                                                                                c.RelativeColumn(1.3F)
                                                                                                                                                c.RelativeColumn(1.4F)
                                                                                                                                                c.RelativeColumn(1.8F)
                                                                                                                                            End Sub)

                                                                                                                    table.Header(Sub(header)
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("ID").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("CASHIER").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("SALE DATE").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("METHOD").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("TOTAL").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("COST").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("PAID").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("CHANGE").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("DISC TYPE").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("DISC AMOUNT").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                 End Sub)

                                                                                                                    For Each sale In orderDataList
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(sale.OrderID.ToString()).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(sale.CreatedBy).FontSize(7)
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(sale.OrderDate.ToString("MM/dd/yy HH:mm")).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(If(String.IsNullOrWhiteSpace(sale.PaymentMethod), "N/A", sale.PaymentMethod)).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"₱{sale.TotalAmount:F2}").FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"₱{sale.TotalCost:F2}").FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"₱{sale.TotalReceived:F2}").FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"₱{sale.Change:F2}").FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(sale.DiscountType).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"₱{sale.DiscountAmount:F2}").FontSize(7).AlignCenter()
                                                                                                                    Next
                                                                                                                End Sub)

                                                                                            column.Item().PaddingTop(12).Border(2).BorderColor(Colors.Orange.Medium).Background(Colors.Grey.Lighten5).Padding(10).Column(Sub(summary)
                                                                                                                                                                                                                             summary.Item().Text("SALES RECORDS SUMMARY & ANALYTICS").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken3)
                                                                                                                                                                                                                             summary.Item().PaddingTop(4).Row(Sub(sRow)
                                                                                                                                                                                                                                                                  sRow.RelativeItem().Column(Sub(left)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Counter (Transactions): {totalCounter}").FontSize(9)
                                                                                                                                                                                                                                                                                                  left.Item().Text($"Total Revenue: ₱{totalRevenue:N2}").FontSize(9).SemiBold()
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Total Cost: ₱{totalCost:N2}").FontSize(9).SemiBold().FontColor(Colors.Red.Medium)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Total Profit (Less Discount): ₱{totalProfit:N2}").FontSize(9).SemiBold().FontColor(Colors.Green.Medium)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Highest Sale: ₱{highestSale:N2}").FontSize(9)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Lowest Sale: ₱{lowestSale:N2}").FontSize(9)
                                                                                                                                                                                                                                                                                                  left.Item().Text($"Orders with Discounts: {ordersWithDiscount} ({discountRate:N1}%)").FontSize(9)
                                                                                                                                                                                                                                                                                             End Sub)
                                                                                                                                                                                                                                                                  sRow.RelativeItem().Column(Sub(right)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"Total Amount Paid: ₱{totalReceived:N2}").FontSize(9)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"Total Change: ₱{totalChange:N2}").FontSize(9)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"Total Discounts Given: ₱{totalDiscounts:N2}").FontSize(9).FontColor(Colors.Red.Medium)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"Opening Capital: ₱{reportCapital:N2}").FontSize(9)
                                                                                                                                                                                                                                                                                                  right.Item().Text($"Expected Amount (Capital + Revenue): ₱{expectedAmount:N2}").FontSize(9).SemiBold()
                                                                                                                                                                                                                                                                                             End Sub)
                                                                                                                                                                                                                                                              End Sub)

                                                                                                                                                                                                                             summary.Item().PaddingTop(8).Row(Sub(pmRow)
                                                                                                                                                                                                                                                                   pmRow.RelativeItem().Text($"Cash Amount: ₱{cashAmount:N2}").FontSize(9).SemiBold()
                                                                                                                                                                                                                                                                   pmRow.RelativeItem().Text($"GCash Amount: ₱{gcashAmount:N2}").FontSize(9).SemiBold()
                                                                                                                                                                                                                                                                   pmRow.RelativeItem().Text($"Card Amount: ₱{cardAmount:N2}").FontSize(9).SemiBold()
                                                                                                                                                                                                                                                              End Sub)
                                                                                                                                                                                                                         End Sub)

                                                                                            column.Item().PaddingTop(10).Text("PER USER TRANSACTION").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken3)
                                                                                            column.Item().PaddingTop(4).Table(Sub(userTable)
                                                                                                                                  userTable.ColumnsDefinition(Sub(c)
                                                                                                                                                                  c.RelativeColumn(3)
                                                                                                                                                                  c.RelativeColumn(1)
                                                                                                                                                                  c.RelativeColumn(2)
                                                                                                                                                              End Sub)
                                                                                                                                  userTable.Header(Sub(h)
                                                                                                                                                       h.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("CASHIER").FontColor(Colors.White).SemiBold().FontSize(8)
                                                                                                                                                       h.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("COUNTER").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                       h.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("AMOUNT").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                   End Sub)
                                                                                                                                  For Each u In perUser
                                                                                                                                      userTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.UserName).FontSize(8)
                                                                                                                                      userTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.Counter.ToString()).FontSize(8).AlignCenter()
                                                                                                                                      userTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"₱{u.Amount:N2}").FontSize(8).AlignCenter()
                                                                                                                                  Next
                                                                                                                              End Sub)
                                                                                        End Sub)

                                                   page.Footer().AlignCenter().Text(Sub(t)
                                                                                        t.Span("Page ").FontSize(7).FontColor(Colors.Grey.Medium)
                                                                                        t.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium)
                                                                                        t.Span(" of ").FontSize(7).FontColor(Colors.Grey.Medium)
                                                                                        t.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium)
                                                                                    End Sub)
                                               End Sub)
                            End Sub).GeneratePdf(filePath)

        Catch ex As Exception
            Throw New Exception($"QuestPDF Sales Records Report Creation Error: {ex.Message}", ex)
        Finally
            If Not String.IsNullOrEmpty(tempLogoPath) AndAlso File.Exists(tempLogoPath) Then
                Try
                    File.Delete(tempLogoPath)
                Catch
                End Try
            End If
        End Try
    End Sub
End Class
Public Class OrderReportData
    Public Property OrderID As Integer
    Public Property CreatedBy As String
    Public Property OrderDate As DateTime
    Public Property TotalAmount As Decimal
    Public Property TotalReceived As Decimal
    Public Property Change As Decimal
    Public Property PaymentMethod As String
    Public Property DiscountType As String
    Public Property DiscountAmount As Decimal
    Public Property TotalCost As Decimal
    Public Property ProfitAmount As Decimal
End Class