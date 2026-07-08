Imports System.IO
Imports System.Linq
Imports System.Data.Common
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Public Class InventoryLogExporter

    Shared Sub New()
        QuestPDF.Settings.License = LicenseType.Community
    End Sub

    Public Shared Sub ExportInventoryLogsReport(Optional sortOrder As String = "", Optional filterType As String = "All Logs", Optional filterDate As DateTime? = Nothing)
        Try
            Dim inventoryReportsPath As String = Path.Combine(Application.StartupPath, "inventoryreports")
            If Not Directory.Exists(inventoryReportsPath) Then
                Directory.CreateDirectory(inventoryReportsPath)
            End If

            Dim query As String = "SELECT il.LogID, il.ProductID, p.ProductName, il.TransactionType, " &
                                  "il.Quantity, il.PreviousStock, il.NewStock, s.SupplierName, " &
                                  "il.Reference, il.Notes, il.BatchNumber, il.ExpiryDate, p.Category, il.CreatedAt, " &
                                  "u.Username " &
                                  "FROM InventoryLog il " &
                                  "INNER JOIN Products p ON il.ProductID = p.ProductID " &
                                  "LEFT JOIN Suppliers s ON il.SupplierID = s.SupplierID " &
                                  "LEFT JOIN Users u ON il.UserID = u.UserID"

            Dim whereClauses As New List(Of String)()

            Select Case filterType
                Case "Today's Logs"
                    whereClauses.Add("DATE(il.CreatedAt) = date('now')")
                Case "This Week's Logs"
                    whereClauses.Add("il.CreatedAt >= datetime('now', 'start of week')")
                Case "This Month's Logs"
                    whereClauses.Add("CAST(strftime('%m', il.CreatedAt) AS INTEGER) = CAST(strftime('%m', 'now') AS INTEGER) AND CAST(strftime('%Y', il.CreatedAt) AS INTEGER) = CAST(strftime('%Y', 'now') AS INTEGER)")
                Case "Stock In Only"
                    whereClauses.Add("LOWER(il.TransactionType) IN ('stock in', 'in', 'stock_in', 'inbound')")
                Case "Stock Out Only"
                    whereClauses.Add("LOWER(il.TransactionType) IN ('stock out', 'out', 'stock_out', 'outbound', 'sold')")

                Case "High Quantity (50+)"
                    whereClauses.Add("ABS(il.Quantity) >= 50")
                Case "Low Quantity (<10)"
                    whereClauses.Add("ABS(il.Quantity) < 10")
                Case Else
                    ' All Logs
            End Select

            If filterDate.HasValue Then
                whereClauses.Add("DATE(il.CreatedAt) = @FilterDate")
            End If

            If whereClauses.Count > 0 Then
                query &= " WHERE " & String.Join(" AND ", whereClauses)
            End If

            Select Case sortOrder
                Case "Date (Newest First)"
                    query &= " ORDER BY il.CreatedAt DESC"
                Case "Date (Oldest First)"
                    query &= " ORDER BY il.CreatedAt ASC"
                Case "Product (A-Z)"
                    query &= " ORDER BY p.ProductName ASC, il.CreatedAt DESC"
                Case "Product (Z-A)"
                    query &= " ORDER BY p.ProductName DESC, il.CreatedAt DESC"
                Case "Transaction Type"
                    query &= " ORDER BY il.TransactionType ASC, il.CreatedAt DESC"
                Case "Quantity (High to Low)"
                    query &= " ORDER BY ABS(il.Quantity) DESC, il.CreatedAt DESC"
                Case "Quantity (Low to High)"
                    query &= " ORDER BY ABS(il.Quantity) ASC, il.CreatedAt DESC"
                Case Else
                    query &= " ORDER BY il.CreatedAt DESC"
            End Select

            Dim inventoryDataList As New List(Of InventoryLogReportData)()
            Dim parameters As New List(Of SqlParameter)()
            If filterDate.HasValue Then
                parameters.Add(New SqlParameter("@FilterDate", filterDate.Value.Date))
            End If

            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters.ToArray())
                While reader.Read()
                    Dim inventoryData As New InventoryLogReportData() With {
                        .LogID = If(IsDBNull(reader("LogID")), 0, Convert.ToInt32(reader("LogID"))),
                        .ProductName = If(IsDBNull(reader("ProductName")), "Unknown", reader("ProductName").ToString()),
                        .TransactionType = If(IsDBNull(reader("TransactionType")), "N/A", reader("TransactionType").ToString()),
                        .Quantity = If(IsDBNull(reader("Quantity")), 0, Convert.ToInt32(reader("Quantity"))),
                        .PreviousStock = If(IsDBNull(reader("PreviousStock")), 0, Convert.ToInt32(reader("PreviousStock"))),
                        .NewStock = If(IsDBNull(reader("NewStock")), 0, Convert.ToInt32(reader("NewStock"))),
                        .SupplierName = If(IsDBNull(reader("SupplierName")), "N/A", reader("SupplierName").ToString()),
                        .Reference = If(IsDBNull(reader("Reference")), "", reader("Reference").ToString()),
                        .Notes = If(IsDBNull(reader("Notes")), "", reader("Notes").ToString()),
                        .BatchNumber = If(IsDBNull(reader("BatchNumber")), "", reader("BatchNumber").ToString()),
                        .ExpiryDate = If(IsDBNull(reader("ExpiryDate")), Nothing, Convert.ToDateTime(reader("ExpiryDate"))),
                        .Category = If(IsDBNull(reader("Category")), "N/A", reader("Category").ToString()),
                        .CreatedAt = If(IsDBNull(reader("CreatedAt")), DateTime.MinValue, Convert.ToDateTime(reader("CreatedAt"))),
                        .CreatedBy = If(IsDBNull(reader("Username")), "System", reader("Username").ToString())
                    }

                    If inventoryData.LogID > 0 AndAlso inventoryData.CreatedAt <> DateTime.MinValue Then
                        inventoryDataList.Add(inventoryData)
                    End If
                End While
            End Using

            If inventoryDataList.Count = 0 Then
                MessageBox.Show("No inventory logs found to export.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim dateSuffix As String = If(filterDate.HasValue, $"_Date_{filterDate.Value:yyyyMMdd}", "")
            Dim fileName As String = $"Inventory_Logs_Report{dateSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            Dim fullPath As String = Path.Combine(inventoryReportsPath, fileName)

            CreateQuestPDFInventoryLogsReport(inventoryDataList, fullPath, filterType, filterDate)

            If Not File.Exists(fullPath) Then
                Throw New Exception("PDF file was not created successfully.")
            End If

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Dim dateFilterInfo As String = If(filterDate.HasValue, $", Date: {filterDate.Value:yyyy-MM-dd}", ", Date: All dates")
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Inventory Logs Report Exported", $"Filter: {filterType}{dateFilterInfo}, Records: {inventoryDataList.Count}")
            End If

            Dim dateFilterMessage As String = If(filterDate.HasValue, $"{vbCrLf}Date Filter: {filterDate.Value:yyyy-MM-dd}", $"{vbCrLf}Date Filter: All dates")
            MessageBox.Show($"Inventory logs report exported successfully!{vbCrLf}Filter Applied: {filterType}{dateFilterMessage}{vbCrLf}Records Exported: {inventoryDataList.Count}{vbCrLf}Opening PDF now...",
                            "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Try
                Process.Start(New ProcessStartInfo(fullPath) With {.UseShellExecute = True})
            Catch
                MessageBox.Show($"PDF created successfully but couldn't open automatically.{vbCrLf}File location: {fullPath}",
                                "PDF Created", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try

        Catch ex As Exception
            Dim errorMessage As String = $"Error exporting inventory logs report: {ex.Message}"
            If ex.InnerException IsNot Nothing Then
                errorMessage += $"{vbCrLf}Details: {ex.InnerException.Message}"
            End If

            MessageBox.Show(errorMessage, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Inventory Logs Report Export Failed", $"Error: {ex.Message}")
            End If
        End Try
    End Sub

    Private Shared Sub CreateQuestPDFInventoryLogsReport(inventoryDataList As List(Of InventoryLogReportData), filePath As String, filterType As String, Optional filterDate As DateTime? = Nothing)
        Dim tempLogoPath As String = Nothing

        Try
            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")
            Dim companyTagline As String = CompanySettingsManager.Instance.GetSettingString("CompanyTagline", "Professional Inventory Management & Tracking")
            Dim companyPhone As String = CompanySettingsManager.Instance.GetSettingString("Phone", "")
            Dim companyAddress As String = CompanySettingsManager.Instance.GetSettingString("Address", "")
            Dim companyTIN As String = CompanySettingsManager.Instance.GetSettingString("TIN", "")

            ' Save logo to a temporary file for QuestPDF image usage
            Try
                Dim logoImg As System.Drawing.Image = CompanySettingsManager.Instance.GetCompanyLogo()
                If logoImg IsNot Nothing Then
                    Dim outDir = Path.GetDirectoryName(filePath)
                    If String.IsNullOrEmpty(outDir) Then outDir = Application.StartupPath
                    tempLogoPath = Path.Combine(outDir, "inventory_report_logo.png")
                    logoImg.Save(tempLogoPath, System.Drawing.Imaging.ImageFormat.Png)
                End If
            Catch
                tempLogoPath = Nothing
            End Try

            ' Analytics by transaction type
            Dim stockInLogs = inventoryDataList.Where(Function(x) x.TransactionType.ToLowerInvariant().Contains("in")).ToList()
            Dim stockOutLogs = inventoryDataList.Where(Function(x) x.TransactionType.ToLowerInvariant().Contains("out")).ToList()
            Dim adjustmentLogs = inventoryDataList.Where(Function(x) x.TransactionType.ToLowerInvariant().Contains("adj")).ToList()

            Dim totalStockIn = stockInLogs.Sum(Function(x) Math.Abs(x.Quantity))
            Dim totalStockOut = stockOutLogs.Sum(Function(x) Math.Abs(x.Quantity))
            Dim totalAdjustments = adjustmentLogs.Sum(Function(x) Math.Abs(x.Quantity))

            ' Analytics by user
            Dim perUser = inventoryDataList.
                GroupBy(Function(x) If(String.IsNullOrWhiteSpace(x.CreatedBy), "System", x.CreatedBy)).
                Select(Function(g) New With {
                    .UserName = g.Key,
                    .Counter = g.Count(),
                    .StockInCount = g.Where(Function(x) x.TransactionType.ToLowerInvariant().Contains("in")).Count(),
                    .StockOutCount = g.Where(Function(x) x.TransactionType.ToLowerInvariant().Contains("out")).Count(),
                    .AdjustmentCount = g.Where(Function(x) x.TransactionType.ToLowerInvariant().Contains("adj")).Count()
                }).
                OrderByDescending(Function(x) x.Counter).
                ToList()

            ' General analytics
            Dim totalLogs As Integer = inventoryDataList.Count
            Dim uniqueProducts As Integer = inventoryDataList.Select(Function(x) x.ProductName).Distinct().Count()
            Dim highestQuantity As Integer = If(totalLogs > 0, inventoryDataList.Max(Function(x) Math.Abs(x.Quantity)), 0)
            Dim lowestQuantity As Integer = If(totalLogs > 0, inventoryDataList.Min(Function(x) Math.Abs(x.Quantity)), 0)

            Document.Create(Sub(container)
                                container.Page(Sub(page)
                                                   page.Size(PageSizes.A4.Landscape())
                                                   page.Margin(1.5F, Unit.Centimetre)
                                                   page.PageColor(Colors.White)
                                                   page.DefaultTextStyle(Function(x) x.FontSize(9))

                                                   ' Header
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

                                                                            h.Item().PaddingTop(12).AlignCenter().Text("INVENTORY LOGS & TRANSACTION REPORT").FontSize(18).SemiBold().FontColor(Colors.Grey.Darken3)

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

                                                   ' Content
                                                   page.Content().PaddingTop(12).Column(Sub(column)
                                                                                            column.Item().Table(Sub(table)
                                                                                                                    table.ColumnsDefinition(Sub(c)
                                                                                                                                                c.ConstantColumn(40)
                                                                                                                                                c.RelativeColumn(2.5F)
                                                                                                                                                c.RelativeColumn(1.2F)
                                                                                                                                                c.RelativeColumn(1.0F)
                                                                                                                                                c.RelativeColumn(1.0F)
                                                                                                                                                c.RelativeColumn(1.0F)
                                                                                                                                                c.RelativeColumn(1.8F)
                                                                                                                                                c.RelativeColumn(1.5F)
                                                                                                                                                c.RelativeColumn(2.0F)
                                                                                                                                            End Sub)

                                                                                                                    table.Header(Sub(header)
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("ID").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("PRODUCT").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("TYPE").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("QTY").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("PREV").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("NEW").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("SUPPLIER").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("REFERENCE").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("DATE & TIME").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                 End Sub)

                                                                                                                    For Each log In inventoryDataList
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(log.LogID.ToString()).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(log.ProductName).FontSize(7)
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(log.TransactionType).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(log.Quantity.ToString()).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(log.PreviousStock.ToString()).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(log.NewStock.ToString()).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(log.SupplierName).FontSize(6)
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(If(String.IsNullOrWhiteSpace(log.Reference), "N/A", log.Reference)).FontSize(6)
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(log.CreatedAt.ToString("MM/dd/yy HH:mm")).FontSize(7).AlignCenter()
                                                                                                                    Next
                                                                                                                End Sub)

                                                                                            ' Summary section
                                                                                            column.Item().PaddingTop(12).Border(2).BorderColor(Colors.Orange.Medium).Background(Colors.Grey.Lighten5).Padding(10).Column(Sub(summary)
                                                                                                                                                                                                                             summary.Item().Text("INVENTORY LOGS SUMMARY & ANALYTICS").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken3)
                                                                                                                                                                                                                             summary.Item().PaddingTop(4).Row(Sub(sRow)
                                                                                                                                                                                                                                                                  sRow.RelativeItem().Column(Sub(left)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Total Logs: {totalLogs}").FontSize(9)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Unique Products: {uniqueProducts}").FontSize(9).SemiBold().FontColor(Colors.Blue.Medium)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Stock In Logs: {stockInLogs.Count}").FontSize(9).FontColor(Colors.Green.Medium)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Stock Out Logs: {stockOutLogs.Count}").FontSize(9).FontColor(Colors.Red.Medium)
                                                                                                                                                                                                                                                                                             End Sub)
                                                                                                                                                                                                                                                                  sRow.RelativeItem().Column(Sub(right)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"Total Stock In Quantity: {totalStockIn}").FontSize(9).SemiBold().FontColor(Colors.Green.Medium)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"Total Stock Out Quantity: {totalStockOut}").FontSize(9).SemiBold().FontColor(Colors.Red.Medium)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"Highest Quantity: {highestQuantity}").FontSize(9)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"Lowest Quantity: {lowestQuantity}").FontSize(9)
                                                                                                                                                                                                                                                                                             End Sub)
                                                                                                                                                                                                                                                              End Sub)
                                                                                                                                                                                                                         End Sub)

                                                                                            ' User activity table
                                                                                            column.Item().PaddingTop(10).Text("USER ACTIVITY BREAKDOWN").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken3)
                                                                                            column.Item().PaddingTop(4).Table(Sub(userTable)
                                                                                                                                  userTable.ColumnsDefinition(Sub(c)
                                                                                                                                                                  c.RelativeColumn(3)
                                                                                                                                                                  c.RelativeColumn(1)
                                                                                                                                                                  c.RelativeColumn(1)
                                                                                                                                                                  c.RelativeColumn(1)
                                                                                                                                                                  c.RelativeColumn(1)
                                                                                                                                                              End Sub)
                                                                                                                                  userTable.Header(Sub(h)
                                                                                                                                                       h.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("USER").FontColor(Colors.White).SemiBold().FontSize(8)
                                                                                                                                                       h.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("TOTAL").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                       h.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("IN").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                       h.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("OUT").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                   End Sub)
                                                                                                                                  For Each u In perUser
                                                                                                                                      userTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.UserName).FontSize(8)
                                                                                                                                      userTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.Counter.ToString()).FontSize(8).AlignCenter()
                                                                                                                                      userTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.StockInCount.ToString()).FontSize(8).AlignCenter().FontColor(Colors.Green.Medium)
                                                                                                                                      userTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.StockOutCount.ToString()).FontSize(8).AlignCenter().FontColor(Colors.Red.Medium)
                                                                                                                                  Next
                                                                                                                              End Sub)
                                                                                        End Sub)

                                                   ' Footer
                                                   page.Footer().AlignCenter().Text(Sub(t)
                                                                                        t.Span("Page ").FontSize(7).FontColor(Colors.Grey.Medium)
                                                                                        t.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium)
                                                                                        t.Span(" of ").FontSize(7).FontColor(Colors.Grey.Medium)
                                                                                        t.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium)
                                                                                    End Sub)
                                               End Sub)
                            End Sub).GeneratePdf(filePath)

        Catch ex As Exception
            Throw New Exception($"QuestPDF Inventory Logs Report Creation Error: {ex.Message}", ex)
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

Public Class InventoryLogReportData
    Public Property LogID As Integer
    Public Property ProductName As String
    Public Property TransactionType As String
    Public Property Quantity As Integer
    Public Property PreviousStock As Integer
    Public Property NewStock As Integer
    Public Property SupplierName As String
    Public Property Reference As String
    Public Property Notes As String
    Public Property BatchNumber As String
    Public Property ExpiryDate As DateTime?
    Public Property Category As String
    Public Property CreatedAt As DateTime
    Public Property CreatedBy As String
End Class