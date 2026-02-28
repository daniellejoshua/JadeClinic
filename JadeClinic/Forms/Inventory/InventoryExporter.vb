Imports System.IO
Imports Microsoft.Data.SqlClient
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.Linq

Public Class InventoryExporter

    Shared Sub New()
        ' Initialize QuestPDF
        QuestPDF.Settings.License = LicenseType.Community
    End Sub

    Public Shared Sub ExportInventoryReport(products As List(Of Dictionary(Of String, Object)), filterDescription As String)

        ' Convert the dictionary list to InventoryReportData list
        Dim inventoryDataList As New List(Of InventoryReportData)()
        For Each product In products
            Dim stockQty As Integer = 0
            Dim reorder As Integer = 0
            Dim selling As Decimal = 0D
            Dim cost As Decimal = 0D

            If product.ContainsKey("StockQty") Then Integer.TryParse(product("StockQty").ToString(), stockQty)
            If product.ContainsKey("ReorderLevel") Then Integer.TryParse(product("ReorderLevel").ToString(), reorder)

            If product.ContainsKey("Price") Then selling = ParseDecimal(product("Price"))
            If product.ContainsKey("SellingPrice") AndAlso selling = 0D Then selling = ParseDecimal(product("SellingPrice"))
            If product.ContainsKey("CostPrice") Then cost = ParseDecimal(product("CostPrice"))

            ' Status: prefer explicit status or IsActive flag; otherwise use stock/reorder rules
            ' Determine status: no "IN STOCK" — use OUT OF STOCK, B.R.L (<= reorder), A.R.L (> reorder), INACTIVE, DISCONTINUED
            Dim statusText As String = String.Empty

            Dim explicitStatus As String = String.Empty
            If product.ContainsKey("Status") AndAlso product("Status") IsNot Nothing Then
                explicitStatus = product("Status").ToString().Trim()
            End If

            If Not String.IsNullOrEmpty(explicitStatus) Then
                Dim sLower = explicitStatus.ToLowerInvariant()
                If sLower.Contains("inactive") Then
                    statusText = "INACTIVE"
                ElseIf sLower.Contains("discontinued") Then
                    statusText = "DISCONTINUED"
                ElseIf sLower.Contains("out") OrElse sLower.Contains("out of stock") Then
                    statusText = "OUT OF STOCK"
                ElseIf sLower.Contains("low") OrElse sLower.Contains("below") OrElse sLower.Contains("b.r.l") OrElse sLower.Contains("brl") Then
                    If stockQty = 0 Then
                        statusText = "OUT OF STOCK"
                    Else
                        statusText = "B.R.L"
                    End If
                ElseIf sLower.Contains("above") OrElse sLower.Contains("a.r.l") OrElse sLower.Contains("arl") Then
                    statusText = "A.R.L"
                Else
                    statusText = explicitStatus.ToUpperInvariant()
                End If
            ElseIf product.ContainsKey("IsActive") Then
                Dim isActiveObj = product("IsActive")
                Dim isActiveBool As Boolean = True
                If Boolean.TryParse(isActiveObj.ToString(), isActiveBool) Then
                    If Not isActiveBool Then
                        statusText = "INACTIVE"
                    Else
                        If stockQty = 0 Then
                            statusText = "OUT OF STOCK"
                        ElseIf reorder > 0 Then
                            If stockQty <= reorder Then
                                statusText = "B.R.L"
                            Else
                                statusText = "A.R.L"
                            End If
                        Else
                            statusText = "A.R.L"
                        End If
                    End If
                Else
                    ' Fallback to stock/reorder logic
                    If stockQty = 0 Then
                        statusText = "OUT OF STOCK"
                    ElseIf reorder > 0 Then
                        If stockQty <= reorder Then
                            statusText = "B.R.L"
                        Else
                            statusText = "A.R.L"
                        End If
                    Else
                        statusText = "A.R.L"
                    End If
                End If
            Else
                ' Default derived from stock & reorder (no "IN STOCK")
                If stockQty = 0 Then
                    statusText = "OUT OF STOCK"
                ElseIf reorder > 0 Then
                    If stockQty <= reorder Then
                        statusText = "B.R.L"
                    Else
                        statusText = "A.R.L"
                    End If
                Else
                    statusText = "A.R.L"
                End If
            End If

            inventoryDataList.Add(New InventoryReportData() With {
                .ProductID = If(product.ContainsKey("ProductID"), Convert.ToInt32(product("ProductID")), 0),
                .ProductName = If(product.ContainsKey("ProductName"), product("ProductName").ToString(), String.Empty),
                .Category = If(product.ContainsKey("Category"), product("Category").ToString(), String.Empty),
                .Sizing = If(product.ContainsKey("Sizing"), product("Sizing").ToString(), If(product.ContainsKey("Unit"), product("Unit").ToString(), String.Empty)),
                .Description = If(product.ContainsKey("Description"), product("Description").ToString(), String.Empty),
                .Price = selling,
                .CostPrice = cost,
                .StockQty = stockQty,
                .ReorderLevel = reorder,
                .Status = statusText
            })
        Next

        ' Generate filename and call the PDF creation logic
        Dim inventoryReportsPath As String = Path.Combine(Application.StartupPath, "inventoryreports")
        If Not Directory.Exists(inventoryReportsPath) Then
            Directory.CreateDirectory(inventoryReportsPath)
        End If
        Dim fileName As String = $"Inventory_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
        Dim fullPath As String = Path.Combine(inventoryReportsPath, fileName)
        CreateQuestPDFInventoryReport(inventoryDataList, fullPath, filterDescription)

        ' Open PDF, show message, etc.
        If Not File.Exists(fullPath) Then
            Throw New Exception("PDF file was not created successfully.")
        End If

        If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
            Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Inventory Report Exported", $"Exported: {fileName} Filter: {filterDescription}")
        End If

        MessageBox.Show($"Inventory report exported successfully!{vbCrLf}Opening PDF now...",
                      "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Try
            Process.Start(New ProcessStartInfo(fullPath) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show($"PDF created successfully but couldn't open automatically.{vbCrLf}File location: {fullPath}",
                          "PDF Created", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End Try
    End Sub

    ' Professional QuestPDF inventory report creation with company settings integration
    Private Shared Sub CreateQuestPDFInventoryReport(inventoryDataList As List(Of InventoryReportData), filePath As String, filterDescription As String)
        Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "LOKAL RECIPE POS System")
        Dim companyTagline As String = CompanySettingsManager.Instance.GetSettingString("CompanyTagline", "Professional Inventory Management & Reporting Solutions")
        Dim logoImg As System.Drawing.Image = Nothing
        Try
            logoImg = CompanySettingsManager.Instance.GetCompanyLogo()
        Catch
            logoImg = Nothing
        End Try

        ' Refresh product authoritative values from database where ProductID is present
        Try
            For Each p In inventoryDataList
                If p.ProductID > 0 Then
                    Try
                        Dim sql As String = "SELECT CurrentStock, ReorderLevel, SellingPrice, CostPrice, IsActive, Unit, Category, ProductName FROM Products WHERE ProductID = @ProductID"
                        Using rdr As SqlDataReader = DatabaseHelper.ExecuteReader(sql, New SqlParameter() {New SqlParameter("@ProductID", p.ProductID)})
                            If rdr.Read() Then
                                If Not IsDBNull(rdr("CurrentStock")) Then p.StockQty = Convert.ToInt32(rdr("CurrentStock"))
                                If Not IsDBNull(rdr("ReorderLevel")) Then p.ReorderLevel = Convert.ToInt32(rdr("ReorderLevel"))
                                If Not IsDBNull(rdr("SellingPrice")) Then p.Price = Convert.ToDecimal(rdr("SellingPrice"))
                                If Not IsDBNull(rdr("CostPrice")) Then p.CostPrice = Convert.ToDecimal(rdr("CostPrice"))
                                If Not IsDBNull(rdr("IsActive")) Then
                                    Dim isActive = Convert.ToBoolean(rdr("IsActive"))
                                    If Not isActive Then
                                        p.Status = "INACTIVE"
                                    Else
                                        If p.StockQty = 0 Then
                                            p.Status = "OUT OF STOCK"
                                        ElseIf p.ReorderLevel > 0 Then
                                            If p.StockQty <= p.ReorderLevel Then
                                                p.Status = "B.R.L"
                                            Else
                                                p.Status = "A.R.L"
                                            End If
                                        Else
                                            p.Status = "A.R.L"
                                        End If
                                    End If
                                End If
                                If Not IsDBNull(rdr("Unit")) Then p.Sizing = rdr("Unit").ToString()
                                If Not IsDBNull(rdr("Category")) Then p.Category = rdr("Category").ToString()
                                If Not IsDBNull(rdr("ProductName")) Then p.ProductName = rdr("ProductName").ToString()
                            End If
                        End Using
                    Catch ex As Exception
                        ' Log and continue with existing values if DB check fails
                        Console.WriteLine($"Warning: could not refresh product {p.ProductID} from DB: {ex.Message}")
                    End Try
                End If
            Next
        Catch ex As Exception
            Console.WriteLine($"Error refreshing product values: {ex.Message}")
        End Try

        Dim tempLogoPath As String = Nothing
        If logoImg IsNot Nothing Then
            Try
                Dim tempDir = Path.GetDirectoryName(filePath)
                If String.IsNullOrEmpty(tempDir) Then tempDir = Application.StartupPath
                tempLogoPath = Path.Combine(tempDir, "report_logo.png")
                logoImg.Save(tempLogoPath, System.Drawing.Imaging.ImageFormat.Png)
            Catch
                tempLogoPath = Nothing
            End Try
        End If

        Try
            Document.Create(Sub(container)
                                container.Page(Sub(page)
                                                   page.Size(PageSizes.A4)
                                                   page.Margin(2, Unit.Centimetre)
                                                   page.PageColor(Colors.White)
                                                   ' Make table text slightly smaller across document
                                                   page.DefaultTextStyle(Function(x) x.FontSize(9))

                                                   ' Header
                                                   page.Header().Row(Sub(row)
                                                                         row.RelativeItem().Column(Sub(column)
                                                                                                       column.Item().Row(Sub(headerRow)
                                                                                                                             Dim logoPathToUse As String = Nothing
                                                                                                                             If Not String.IsNullOrEmpty(tempLogoPath) AndAlso File.Exists(tempLogoPath) Then
                                                                                                                                 logoPathToUse = tempLogoPath
                                                                                                                             Else
                                                                                                                                 Dim fallback = Path.Combine(Application.StartupPath, "Resources", "logoPrint.png")
                                                                                                                                 If File.Exists(fallback) Then
                                                                                                                                     logoPathToUse = fallback
                                                                                                                                 End If
                                                                                                                             End If

                                                                                                                             If Not String.IsNullOrEmpty(logoPathToUse) Then
                                                                                                                                 headerRow.ConstantItem(50).Height(35).Image(logoPathToUse)
                                                                                                                             Else
                                                                                                                                 headerRow.ConstantItem(50).Height(35).Text("")
                                                                                                                             End If

                                                                                                                             headerRow.RelativeItem().Padding(10, 0).Column(Sub(companyColumn)
                                                                                                                                                                                companyColumn.Item().Text(companyName).FontSize(16).SemiBold().FontColor(Colors.Orange.Medium)
                                                                                                                                                                                companyColumn.Item().Text(companyTagline).FontSize(9).Italic().FontColor(Colors.Grey.Medium)
                                                                                                                                                                            End Sub)
                                                                                                                         End Sub)

                                                                                                       column.Item().PaddingTop(15).Text("INVENTORY & PRODUCT MANAGEMENT REPORT").FontSize(18).SemiBold().AlignCenter().FontColor(Colors.Grey.Darken3)

                                                                                                       column.Item().PaddingTop(10).Row(Sub(infoRow)
                                                                                                                                            infoRow.RelativeItem().Text($"Generated: {DateTime.Now:dddd, MMMM dd, yyyy} at {DateTime.Now:hh:mm tt} by {frmLoginvb.LoggedInUsername}").FontSize(8).FontColor(Colors.Grey.Medium)
                                                                                                                                            infoRow.RelativeItem().AlignRight().Text("BUSINESS CONFIDENTIAL").FontSize(10).SemiBold().FontColor(Colors.Red.Medium)
                                                                                                                                        End Sub)
                                                                                                   End Sub)
                                                                     End Sub)

                                                   ' Content and table
                                                   page.Content().PaddingTop(20).Column(Sub(column)
                                                                                            column.Item().Text("PRODUCT INVENTORY LISTING").FontSize(12).SemiBold().FontColor(Colors.Grey.Darken2)
                                                                                            column.Item().Text("Filter Applied: " & filterDescription).FontSize(9).Italic().FontColor(Colors.Grey.Darken2)

                                                                                            column.Item().PaddingTop(10).Table(Sub(table)
                                                                                                                                   table.ColumnsDefinition(Sub(columns)
                                                                                                                                                               columns.ConstantColumn(30)   ' ID
                                                                                                                                                               columns.RelativeColumn(3)    ' Product Name
                                                                                                                                                               columns.RelativeColumn(2)    ' Category
                                                                                                                                                               columns.RelativeColumn(1)    ' Units
                                                                                                                                                               columns.RelativeColumn(1)    ' Cost
                                                                                                                                                               columns.RelativeColumn(1)    ' Price
                                                                                                                                                               columns.RelativeColumn(1)    ' Reorder
                                                                                                                                                               columns.RelativeColumn(1)    ' Stock
                                                                                                                                                               columns.RelativeColumn(2)    ' Status
                                                                                                                                                           End Sub)

                                                                                                                                   ' Headers (smaller)
                                                                                                                                   table.Header(Sub(header)
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("ID").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("PRODUCT NAME").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("CATEGORY").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("UNITS").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("COST").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("PRICE").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("REORDER").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("STOCK").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("STATUS").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                End Sub)

                                                                                                                                   ' Data rows (use smaller font, no color dependency)
                                                                                                                                   For Each product In inventoryDataList
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.ProductID.ToString()).FontSize(7).AlignCenter()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.ProductName).FontSize(7).SemiBold()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.Category).FontSize(7).AlignCenter()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.Sizing).FontSize(7).AlignCenter()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"₱{product.CostPrice:F2}").FontSize(7).AlignCenter()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"₱{product.Price:F2}").FontSize(7).AlignCenter().SemiBold()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.ReorderLevel.ToString()).FontSize(7).AlignCenter()

                                                                                                                                       ' Replace the two table cells for STOCK and STATUS with colored text
                                                                                                                                       Dim stockColor = GetStockColor(product.StockQty, product.ReorderLevel, product.Status)
                                                                                                                                       Dim statusColor = GetStatusColor(product.Status)
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.StockQty.ToString()).FontSize(7).AlignCenter().SemiBold().FontColor(stockColor)
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.Status).FontSize(7).AlignCenter().SemiBold().FontColor(statusColor)
                                                                                                                                   Next
                                                                                                                               End Sub)

                                                                                            ' Summary: compute using numeric checks and separate selling vs cost inventory values
                                                                                            Dim totalProducts As Integer = inventoryDataList.Count
                                                                                            ' Monetary totals use Decimal arithmetic with DB-refreshed values
                                                                                            Dim totalValueSelling As Decimal = inventoryDataList.Sum(Function(p) p.Price * CDec(p.StockQty))
                                                                                            Dim totalValueCost As Decimal = inventoryDataList.Sum(Function(p) p.CostPrice * CDec(p.StockQty))
                                                                                            Dim totalStockQty As Integer = inventoryDataList.Sum(Function(p) p.StockQty)
                                                                                            ' Inactive products counted separately
                                                                                            Dim inactiveProducts As Integer = inventoryDataList.Where(Function(p) Not String.IsNullOrEmpty(p.Status) AndAlso p.Status.ToLower().Contains("inactive")).Count()
                                                                                            ' Out of stock excludes inactive
                                                                                            Dim outOfStockProducts As Integer = inventoryDataList.Where(Function(p) p.StockQty = 0 AndAlso Not (Not String.IsNullOrEmpty(p.Status) AndAlso p.Status.ToLower().Contains("inactive"))).Count()
                                                                                            ' Below reorder excludes zero-stock and inactive
                                                                                            Dim belowReorderProducts As Integer = inventoryDataList.Where(Function(p) p.ReorderLevel > 0 AndAlso p.StockQty > 0 AndAlso p.StockQty <= p.ReorderLevel AndAlso Not (Not String.IsNullOrEmpty(p.Status) AndAlso p.Status.ToLower().Contains("inactive"))).Count()
                                                                                            ' Above reorder (explicitly above) excludes inactive
                                                                                            Dim aboveReorderProducts As Integer = inventoryDataList.Where(Function(p) p.ReorderLevel > 0 AndAlso p.StockQty > p.ReorderLevel AndAlso Not (Not String.IsNullOrEmpty(p.Status) AndAlso p.Status.ToLower().Contains("inactive"))).Count()

                                                                                            column.Item().PaddingTop(20).Border(2).BorderColor(Colors.Orange.Medium).Background(Colors.Grey.Lighten5).Padding(15).Column(Sub(summaryColumn)
                                                                                                                                                                                                                             summaryColumn.Item().Text("INVENTORY SUMMARY & ANALYTICS").FontSize(12).SemiBold().FontColor(Colors.Grey.Darken2)
                                                                                                                                                                                                                             summaryColumn.Item().PaddingTop(10).Row(Sub(summaryRow)
                                                                                                                                                                                                                                                                         summaryRow.RelativeItem().Column(Sub(statsColumn)
                                                                                                                                                                                                                                                                                                              statsColumn.Item().Text($"Total Products: {totalProducts} items").FontSize(9)
                                                                                                                                                                                                                                                                                                              statsColumn.Item().Text($"Total Inventory Value (Selling): ₱{totalValueSelling:N2}").FontSize(9).SemiBold()
                                                                                                                                                                                                                                                                                                              statsColumn.Item().Text($"Total Inventory Cost: ₱{totalValueCost:N2}").FontSize(9)
                                                                                                                                                                                                                                                                                                              statsColumn.Item().Text($"Total Stock Quantity: {totalStockQty} units").FontSize(9)
                                                                                                                                                                                                                                                                                                              statsColumn.Item().Text($"Above Reorder Level: {aboveReorderProducts} products").FontSize(9).FontColor(Colors.Green.Lighten2)
                                                                                                                                                                                                                                                                                                              statsColumn.Item().Text($"Inactive: {inactiveProducts} products").FontSize(9).FontColor(Colors.Grey.Medium)
                                                                                                                                                                                                                                                                                                          End Sub)
                                                                                                                                                                                                                                                                         summaryRow.RelativeItem().Column(Sub(statusColumn)
                                                                                                                                                                                                                                                                                                              statusColumn.Item().Text($"Out of Stock: {outOfStockProducts} products").FontSize(9).FontColor(Colors.Red.Medium)
                                                                                                                                                                                                                                                                                                              statusColumn.Item().Text($"Below Reorder Level: {belowReorderProducts} products").FontSize(9).FontColor(Colors.Orange.Medium)
                                                                                                                                                                                                                                                                                                              statusColumn.Item().PaddingTop(10).Text("Report Generated").FontSize(9).SemiBold()
                                                                                                                                                                                                                                                                                                              statusColumn.Item().Text($"{DateTime.Now:MM/dd/yyyy} at {DateTime.Now:hh:mm tt}").FontSize(9)
                                                                                                                                                                                                                                                                                                              statusColumn.Item().Text(companyName).FontSize(9)
                                                                                                                                                                                                                                                                                                          End Sub)
                                                                                                                                                                                                                                                                     End Sub)
                                                                                                                                                                                                                         End Sub)
                                                                                        End Sub)
                                               End Sub)
                            End Sub).GeneratePdf(filePath)

        Finally
            If Not String.IsNullOrEmpty(tempLogoPath) AndAlso File.Exists(tempLogoPath) Then
                Try
                    File.Delete(tempLogoPath)
                Catch
                End Try
            End If
        End Try
    End Sub

    ' Robust decimal parser that trims currency symbols and tries common cultures
    Private Shared Function ParseDecimal(value As Object) As Decimal
        If value Is Nothing Then
            Return 0D
        End If

        Dim s = value.ToString().Trim()
        If String.IsNullOrEmpty(s) Then
            Return 0D
        End If

        ' Keep digits, decimal separators, negative sign
        Dim cleaned = New String(s.Where(Function(c) Char.IsDigit(c) OrElse c = "."c OrElse c = ","c OrElse c = "-"c).ToArray())

        Dim result As Decimal
        If Decimal.TryParse(cleaned, NumberStyles.Number Or NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, result) Then
            Return result
        End If
        If Decimal.TryParse(cleaned, NumberStyles.Number Or NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, result) Then
            Return result
        End If

        ' Fallback: remove grouping commas and try invariant
        cleaned = cleaned.Replace(",", "")
        If Decimal.TryParse(cleaned, NumberStyles.Number Or NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, result) Then
            Return result
        End If

        Return 0D
    End Function

    ' Helper function to get status color
    Private Shared Function GetStatusColor(status As String) As QuestPDF.Infrastructure.Color
        If String.IsNullOrEmpty(status) Then
            Return Colors.Grey.Darken1
        End If

        Select Case status.ToLower().Trim()
            Case "a.r.l", "arl", "above reorder", "above re order level"
                Return Colors.Green.Medium
            Case "b.r.l", "brl", "below reorder", "below reorder level", "low on stock", "low stock"
                Return Colors.Orange.Medium
            Case "out of stock", "out", "outofstock"
                Return Colors.Red.Medium
            Case "discontinued"
                Return Colors.Red.Darken1
            Case "inactive"
                Return Colors.Grey.Medium
            Case Else
                Return Colors.Grey.Darken1
        End Select
    End Function

    ' Helper function to get stock quantity color
    ' Rules:
    '  - INACTIVE -> Gray
    '  - OUT OF STOCK -> Red
    '  - B.R.L / below reorder (<= reorder) -> Orange
    '  - A.R.L / above reorder -> Green
    Private Shared Function GetStockColor(stockQty As Integer, reorderLevel As Integer, status As String) As QuestPDF.Infrastructure.Color
        If Not String.IsNullOrEmpty(status) AndAlso status.ToLower().Contains("inactive") Then
            Return Colors.Grey.Medium
        End If

        If stockQty = 0 Then
            Return Colors.Red.Medium
        End If

        If reorderLevel > 0 AndAlso stockQty <= reorderLevel Then
            Return Colors.Orange.Medium
        End If

        Return Colors.Green.Medium
    End Function
End Class

' Data class for inventory report
Public Class InventoryReportData
    Public Property ProductID As Integer
    Public Property ProductName As String
    Public Property Category As String
    Public Property Sizing As String
    Public Property Description As String
    Public Property Price As Decimal
    Public Property CostPrice As Decimal
    Public Property StockQty As Integer
    Public Property ReorderLevel As Integer
    Public Property Status As String
End Class