Imports System.IO
Imports System.Linq
Imports System.Data.Common
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Public Class SupplierExporter

    Shared Sub New()
        QuestPDF.Settings.License = LicenseType.Community
    End Sub

    Public Shared Sub ExportOrderRecordsReport(Optional sortOrder As String = "", Optional filterType As String = "All Suppliers", Optional filterDate As DateTime? = Nothing)
        Try
            Dim reportsPath As String = Path.Combine(Application.StartupPath, "supplierreports")
            If Not Directory.Exists(reportsPath) Then
                Directory.CreateDirectory(reportsPath)
            End If

            Dim query As String = "SELECT s.SupplierID, s.SupplierCode, s.SupplierName, s.ContactPerson, s.Phone, s.Email, s.IsActive, " &
                                  "IFNULL((SELECT COUNT(1) FROM InventoryLog il WHERE il.SupplierID = s.SupplierID " &
                                  "AND (LOWER(il.TransactionType) = 'in' OR il.TransactionType IN ('IN','INBOUND','Stock In','stock in'))" &
                                  If(filterDate.HasValue, " AND DATE(il.CreatedAt) = @FilterDate", "") &
                                  "), 0) AS StockInCount, " &
                                  "(SELECT MAX(il.CreatedAt) FROM InventoryLog il WHERE il.SupplierID = s.SupplierID " &
                                  "AND (LOWER(il.TransactionType) = 'in' OR il.TransactionType IN ('IN','INBOUND','Stock In','stock in'))" &
                                  If(filterDate.HasValue, " AND DATE(il.CreatedAt) = @FilterDate", "") &
                                  ") AS LastStockInDate " &
                                  "FROM Suppliers s"

            Dim whereClauses As New List(Of String)()

            Select Case filterType
                Case "Active Suppliers"
                    whereClauses.Add("IFNULL(s.IsActive, 1) = 1")
                Case "Inactive Suppliers"
                    whereClauses.Add("IFNULL(s.IsActive, 1) = 0")
                Case "With Stock In"
                    whereClauses.Add("EXISTS (SELECT 1 FROM InventoryLog il WHERE il.SupplierID = s.SupplierID " &
                                     "AND (LOWER(il.TransactionType) = 'in' OR il.TransactionType IN ('IN','INBOUND','Stock In','stock in'))" &
                                     If(filterDate.HasValue, " AND DATE(il.CreatedAt) = @FilterDate", "") & ")")
                Case "Without Stock In"
                    whereClauses.Add("NOT EXISTS (SELECT 1 FROM InventoryLog il WHERE il.SupplierID = s.SupplierID " &
                                     "AND (LOWER(il.TransactionType) = 'in' OR il.TransactionType IN ('IN','INBOUND','Stock In','stock in'))" &
                                     If(filterDate.HasValue, " AND DATE(il.CreatedAt) = @FilterDate", "") & ")")
                Case Else
                    ' All Suppliers
            End Select

            If whereClauses.Count > 0 Then
                query &= " WHERE " & String.Join(" AND ", whereClauses)
            End If

            Select Case sortOrder
                Case "Name (A-Z)"
                    query &= " ORDER BY s.SupplierName ASC"
                Case "Name (Z-A)"
                    query &= " ORDER BY s.SupplierName DESC"
                Case "Code (Ascending)"
                    query &= " ORDER BY s.SupplierCode ASC"
                Case "Code (Descending)"
                    query &= " ORDER BY s.SupplierCode DESC"
                Case "Status (Active First)"
                    query &= " ORDER BY s.IsActive DESC, s.SupplierName ASC"
                Case Else
                    query &= " ORDER BY s.SupplierName ASC"
            End Select

            Dim supplierDataList As New List(Of SupplierReportData)()
            Dim parameters As New List(Of SqlParameter)()
            If filterDate.HasValue Then
                parameters.Add(New SqlParameter("@FilterDate", filterDate.Value.Date))
            End If

            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters.ToArray())
                While reader.Read()
                    Dim supplierData As New SupplierReportData() With {
                        .SupplierID = If(IsDBNull(reader("SupplierID")), 0, Convert.ToInt32(reader("SupplierID"))),
                        .SupplierCode = If(IsDBNull(reader("SupplierCode")), "", reader("SupplierCode").ToString()),
                        .SupplierName = If(IsDBNull(reader("SupplierName")), "", reader("SupplierName").ToString()),
                        .ContactPerson = If(IsDBNull(reader("ContactPerson")), "", reader("ContactPerson").ToString()),
                        .Phone = If(IsDBNull(reader("Phone")), "", reader("Phone").ToString()),
                        .Email = If(IsDBNull(reader("Email")), "", reader("Email").ToString()),
                        .IsActive = If(IsDBNull(reader("IsActive")), True, Convert.ToBoolean(reader("IsActive"))),
                        .StockInCount = If(IsDBNull(reader("StockInCount")), 0, Convert.ToInt32(reader("StockInCount"))),
                        .LastStockInDate = If(IsDBNull(reader("LastStockInDate")), CType(Nothing, DateTime?), Convert.ToDateTime(reader("LastStockInDate")))
                    }

                    If supplierData.SupplierID > 0 Then
                        supplierDataList.Add(supplierData)
                    End If
                End While
            End Using

            If supplierDataList.Count = 0 Then
                MessageBox.Show("No supplier records found to export.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim dateSuffix As String = If(filterDate.HasValue, $"_Date_{filterDate.Value:yyyyMMdd}", "")
            Dim fileName As String = $"Supplier_Report{dateSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            Dim fullPath As String = Path.Combine(reportsPath, fileName)

            CreateQuestPDFOrderRecordsReport(supplierDataList, fullPath, filterType, filterDate)

            If Not File.Exists(fullPath) Then
                Throw New Exception("PDF file was not created successfully.")
            End If

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Dim dateFilterInfo As String = If(filterDate.HasValue, $", Date: {filterDate.Value:yyyy-MM-dd}", ", Date: All dates")
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Supplier Report Exported", $"Filter: {filterType}{dateFilterInfo}, Records: {supplierDataList.Count}")
            End If

            Dim dateFilterMessage As String = If(filterDate.HasValue, $"{vbCrLf}Date Filter: {filterDate.Value:yyyy-MM-dd}", $"{vbCrLf}Date Filter: All dates")
            MessageBox.Show($"Supplier report exported successfully!{vbCrLf}Filter Applied: {filterType}{dateFilterMessage}{vbCrLf}Records Exported: {supplierDataList.Count}{vbCrLf}Opening PDF now...",
                            "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Try
                Process.Start(New ProcessStartInfo(fullPath) With {.UseShellExecute = True})
            Catch
                MessageBox.Show($"PDF created successfully but couldn't open automatically.{vbCrLf}File location: {fullPath}",
                                "PDF Created", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try

        Catch ex As Exception
            Dim errorMessage As String = $"Error exporting supplier report: {ex.Message}"
            If ex.InnerException IsNot Nothing Then
                errorMessage += $"{vbCrLf}Details: {ex.InnerException.Message}"
            End If

            MessageBox.Show(errorMessage, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Supplier Report Export Failed", $"Error: {ex.Message}")
            End If
        End Try
    End Sub

    Private Shared Sub CreateQuestPDFOrderRecordsReport(supplierDataList As List(Of SupplierReportData), filePath As String, filterType As String, Optional filterDate As DateTime? = Nothing)
        Dim tempLogoPath As String = Nothing

        Try
            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")
            Dim companyTagline As String = CompanySettingsManager.Instance.GetSettingString("CompanyTagline", "Professional Supplier Management & Reporting")
            Dim companyPhone As String = CompanySettingsManager.Instance.GetSettingString("Phone", "")
            Dim companyAddress As String = CompanySettingsManager.Instance.GetSettingString("Address", "")
            Dim companyTIN As String = CompanySettingsManager.Instance.GetSettingString("TIN", "")

            ' Save logo to a temporary file for QuestPDF image usage
            Try
                Dim logoImg As System.Drawing.Image = CompanySettingsManager.Instance.GetCompanyLogo()
                If logoImg IsNot Nothing Then
                    Dim outDir = Path.GetDirectoryName(filePath)
                    If String.IsNullOrEmpty(outDir) Then outDir = Application.StartupPath
                    tempLogoPath = Path.Combine(outDir, "supplier_report_logo.png")
                    logoImg.Save(tempLogoPath, System.Drawing.Imaging.ImageFormat.Png)
                End If
            Catch
                tempLogoPath = Nothing
            End Try

            Dim perSupplier = supplierDataList.
                OrderByDescending(Function(x) x.StockInCount).
                ThenBy(Function(x) x.SupplierName).
                Select(Function(x) New With {
                    .SupplierName = If(String.IsNullOrWhiteSpace(x.SupplierName), "Unknown", x.SupplierName),
                    .Counter = x.StockInCount,
                    .LastActivity = If(x.LastStockInDate.HasValue, x.LastStockInDate.Value.ToString("MM/dd/yyyy HH:mm"), "N/A")
                }).
                ToList()

            Dim totalSuppliers As Integer = supplierDataList.Count
            Dim activeSuppliers As Integer = supplierDataList.Where(Function(s) s.IsActive).Count()
            Dim inactiveSuppliers As Integer = totalSuppliers - activeSuppliers
            Dim totalStockIns As Integer = supplierDataList.Sum(Function(s) s.StockInCount)
            Dim suppliersWithStockIns As Integer = supplierDataList.Where(Function(s) s.StockInCount > 0).Count()
            Dim suppliersWithoutStockIns As Integer = totalSuppliers - suppliersWithStockIns
            Dim suppliersWithContact As Integer = supplierDataList.Where(Function(s) Not String.IsNullOrWhiteSpace(s.ContactPerson)).Count()
            Dim suppliersWithPhone As Integer = supplierDataList.Where(Function(s) Not String.IsNullOrWhiteSpace(s.Phone)).Count()
            Dim suppliersWithEmail As Integer = supplierDataList.Where(Function(s) Not String.IsNullOrWhiteSpace(s.Email)).Count()
            Dim topStockInCount As Integer = If(totalSuppliers > 0, supplierDataList.Max(Function(s) s.StockInCount), 0)

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

                                                                            h.Item().PaddingTop(12).AlignCenter().Text("SUPPLIER MASTERLIST & ANALYTICS REPORT").FontSize(18).SemiBold().FontColor(Colors.Grey.Darken3)

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
                                                                                                                                                c.RelativeColumn(1.3F)
                                                                                                                                                c.RelativeColumn(2.2F)
                                                                                                                                                c.RelativeColumn(1.7F)
                                                                                                                                                c.RelativeColumn(1.3F)
                                                                                                                                                c.RelativeColumn(2.0F)
                                                                                                                                                c.RelativeColumn(1.0F)
                                                                                                                                                c.RelativeColumn(1.2F)
                                                                                                                                                c.RelativeColumn(1.8F)
                                                                                                                                            End Sub)

                                                                                                                    table.Header(Sub(header)
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("ID").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("CODE").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("SUPPLIER NAME").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("CONTACT").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("PHONE").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("EMAIL").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("STOCK IN").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("STATUS").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                     header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("LAST STOCK-IN").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                 End Sub)

                                                                                                                    For Each s In supplierDataList
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(s.SupplierID.ToString()).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(If(String.IsNullOrWhiteSpace(s.SupplierCode), "N/A", s.SupplierCode)).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(If(String.IsNullOrWhiteSpace(s.SupplierName), "N/A", s.SupplierName)).FontSize(7)
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(If(String.IsNullOrWhiteSpace(s.ContactPerson), "N/A", s.ContactPerson)).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(If(String.IsNullOrWhiteSpace(s.Phone), "N/A", s.Phone)).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(If(String.IsNullOrWhiteSpace(s.Email), "N/A", s.Email)).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(s.StockInCount.ToString()).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(If(s.IsActive, "Active", "Inactive")).FontSize(7).AlignCenter()
                                                                                                                        table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(If(s.LastStockInDate.HasValue, s.LastStockInDate.Value.ToString("MM/dd/yy HH:mm"), "N/A")).FontSize(7).AlignCenter()
                                                                                                                    Next
                                                                                                                End Sub)

                                                                                            column.Item().PaddingTop(12).Border(2).BorderColor(Colors.Orange.Medium).Background(Colors.Grey.Lighten5).Padding(10).Column(Sub(summary)
                                                                                                                                                                                                                             summary.Item().Text("SUPPLIER SUMMARY & ANALYTICS").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken3)
                                                                                                                                                                                                                             summary.Item().PaddingTop(4).Row(Sub(sRow)
                                                                                                                                                                                                                                                                  sRow.RelativeItem().Column(Sub(left)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Total Suppliers: {totalSuppliers}").FontSize(9)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Active Suppliers: {activeSuppliers}").FontSize(9).SemiBold().FontColor(Colors.Green.Medium)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Inactive Suppliers: {inactiveSuppliers}").FontSize(9).FontColor(Colors.Red.Medium)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Suppliers With Stock-In: {suppliersWithStockIns}").FontSize(9)
                                                                                                                                                                                                                                                                                                 left.Item().Text($"Suppliers Without Stock-In: {suppliersWithoutStockIns}").FontSize(9)
                                                                                                                                                                                                                                                                                             End Sub)
                                                                                                                                                                                                                                                                  sRow.RelativeItem().Column(Sub(right)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"Total Stock-In Transactions: {totalStockIns:N0}").FontSize(9)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"Highest Stock-In Count (Single Supplier): {topStockInCount:N0}").FontSize(9)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"With Contact Person: {suppliersWithContact}").FontSize(9)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"With Phone: {suppliersWithPhone}").FontSize(9)
                                                                                                                                                                                                                                                                                                 right.Item().Text($"With Email: {suppliersWithEmail}").FontSize(9)
                                                                                                                                                                                                                                                                                             End Sub)
                                                                                                                                                                                                                                                              End Sub)
                                                                                                                                                                                                                         End Sub)

                                                                                            column.Item().PaddingTop(10).Text("SUPPLIER STOCK-IN ACTIVITY").FontSize(10).SemiBold().FontColor(Colors.Grey.Darken3)
                                                                                            column.Item().PaddingTop(4).Table(Sub(activityTable)
                                                                                                                                  activityTable.ColumnsDefinition(Sub(c)
                                                                                                                                                                      c.RelativeColumn(3)
                                                                                                                                                                      c.RelativeColumn(1)
                                                                                                                                                                      c.RelativeColumn(2)
                                                                                                                                                                  End Sub)
                                                                                                                                  activityTable.Header(Sub(h)
                                                                                                                                                           h.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("SUPPLIER").FontColor(Colors.White).SemiBold().FontSize(8)
                                                                                                                                                           h.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("COUNTER").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                           h.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("LAST ACTIVITY").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                       End Sub)
                                                                                                                                  If perSupplier.Count = 0 Then
                                                                                                                                      activityTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text("No supplier activity").FontSize(8)
                                                                                                                                      activityTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text("0").FontSize(8).AlignCenter()
                                                                                                                                      activityTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text("N/A").FontSize(8).AlignCenter()
                                                                                                                                  Else
                                                                                                                                      For Each u In perSupplier
                                                                                                                                          activityTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.SupplierName).FontSize(8)
                                                                                                                                          activityTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.Counter.ToString()).FontSize(8).AlignCenter()
                                                                                                                                          activityTable.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.LastActivity).FontSize(8).AlignCenter()
                                                                                                                                      Next
                                                                                                                                  End If
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
            Throw New Exception($"QuestPDF Supplier Report Creation Error: {ex.Message}", ex)
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

Public Class SupplierReportData
    Public Property SupplierID As Integer
    Public Property SupplierCode As String
    Public Property SupplierName As String
    Public Property ContactPerson As String
    Public Property Phone As String
    Public Property Email As String
    Public Property IsActive As Boolean
    Public Property StockInCount As Integer
    Public Property LastStockInDate As DateTime?
End Class