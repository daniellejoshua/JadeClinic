Imports System.IO
Imports Microsoft.Data.SqlClient
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure
Imports System.Drawing.Imaging

Public Class InventoryExporter

    Shared Sub New()
        ' Initialize QuestPDF
        QuestPDF.Settings.License = LicenseType.Community
    End Sub

    Public Shared Sub ExportInventoryReport(products As List(Of Dictionary(Of String, Object)), filterDescription As String)

        ' Convert the dictionary list to InventoryReportData list
        Dim inventoryDataList As New List(Of InventoryReportData)()
        For Each product In products
            inventoryDataList.Add(New InventoryReportData() With {
                .ProductID = Convert.ToInt32(product("ProductID")),
                .ProductName = product("ProductName").ToString(),
                .Category = product("Category").ToString(),
                .Sizing = product("Sizing").ToString(),
                .Description = product("Description").ToString(),
                .Price = Convert.ToDecimal(product("Price")),
                .StockQty = Convert.ToInt32(product("StockQty")),
                .Status = product("Status").ToString(),
                .Color = product("Color").ToString()
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
        ' Pull company settings (name, tagline, logo) so reports match site settings
        Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "LOKAL RECIPE POS System")
        Dim companyTagline As String = CompanySettingsManager.Instance.GetSettingString("CompanyTagline", "Professional Inventory Management & Reporting Solutions")
        Dim logoImg As System.Drawing.Image = Nothing
        Try
            logoImg = CompanySettingsManager.Instance.GetCompanyLogo()
        Catch ex As Exception
            Console.WriteLine($"Unable to load company logo from settings: {ex.Message}")
            logoImg = Nothing
        End Try

        Dim tempLogoPath As String = Nothing
        If logoImg IsNot Nothing Then
            Try
                Dim tempDir = Path.GetDirectoryName(filePath)
                If String.IsNullOrEmpty(tempDir) Then tempDir = Application.StartupPath
                tempLogoPath = Path.Combine(tempDir, "report_logo.png")
                ' Overwrite if exists
                ' Replace the ambiguous ImageFormat usage with fully-qualified type
                logoImg.Save(tempLogoPath, System.Drawing.Imaging.ImageFormat.Png)
            Catch ex As Exception
                Console.WriteLine($"Failed to save temporary logo file: {ex.Message}")
                tempLogoPath = Nothing
            End Try
        End If

        Try
            Document.Create(Sub(container)
                                container.Page(Sub(page)
                                                   page.Size(PageSizes.A4)
                                                   page.Margin(2, Unit.Centimetre)
                                                   page.PageColor(Colors.White)
                                                   page.DefaultTextStyle(Function(x) x.FontSize(10))

                                                   ' Header
                                                   page.Header().Row(Sub(row)
                                                                         row.RelativeItem().Column(Sub(column)
                                                                                                       ' Company logo and header
                                                                                                       column.Item().Row(Sub(headerRow)
                                                                                                                             ' Logo section - prefer company setting logo, fallback to resource file
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
                                                                                                                                 ' If no logo, reserve the space
                                                                                                                                 headerRow.ConstantItem(50).Height(35).Text("")
                                                                                                                             End If

                                                                                                                             ' Company info
                                                                                                                             headerRow.RelativeItem().Padding(10, 0).Column(Sub(companyColumn)
                                                                                                                                                                                companyColumn.Item().Text(companyName).FontSize(16).SemiBold().FontColor(Colors.Orange.Medium)
                                                                                                                                                                                companyColumn.Item().Text(companyTagline).FontSize(9).Italic().FontColor(Colors.Grey.Medium)
                                                                                                                                                                            End Sub)
                                                                                                                         End Sub)

                                                                                                       ' Report title
                                                                                                       column.Item().PaddingTop(15).Text("INVENTORY & PRODUCT MANAGEMENT REPORT").FontSize(18).SemiBold().AlignCenter().FontColor(Colors.Grey.Darken3)

                                                                                                       ' Generation info
                                                                                                       column.Item().PaddingTop(10).Row(Sub(infoRow)
                                                                                                                                            infoRow.RelativeItem().Text($"Generated: {DateTime.Now:dddd, MMMM dd, yyyy} at {DateTime.Now:hh:mm tt} by {frmLoginvb.LoggedInUsername}").FontSize(8).FontColor(Colors.Grey.Medium)
                                                                                                                                            infoRow.RelativeItem().AlignRight().Text("BUSINESS CONFIDENTIAL").FontSize(10).SemiBold().FontColor(Colors.Red.Medium)
                                                                                                                                        End Sub)
                                                                                                   End Sub)
                                                                     End Sub)

                                                   ' Content
                                                   page.Content().PaddingTop(20).Column(Sub(column)
                                                                                            ' Section title
                                                                                            column.Item().Text("PRODUCT INVENTORY LISTING").FontSize(12).SemiBold().FontColor(Colors.Grey.Darken2)
                                                                                            column.Item().Text("Filter Applied: " & filterDescription).FontSize(10).Italic().FontColor(Colors.Grey.Darken2)
                                                                                            ' Inventory table
                                                                                            column.Item().PaddingTop(10).Table(Sub(table)
                                                                                                                                   table.ColumnsDefinition(Sub(columns)
                                                                                                                                                               columns.ConstantColumn(30)   ' ID
                                                                                                                                                               columns.RelativeColumn(3)    ' Product Name
                                                                                                                                                               columns.RelativeColumn(2)    ' Category
                                                                                                                                                               columns.RelativeColumn(1)    ' Size
                                                                                                                                                               columns.RelativeColumn(1)    ' Color
                                                                                                                                                               columns.RelativeColumn(1)    ' Price
                                                                                                                                                               columns.RelativeColumn(1)    ' Stock
                                                                                                                                                               columns.RelativeColumn(2)    ' Status
                                                                                                                                                           End Sub)

                                                                                                                                   ' Headers
                                                                                                                                   table.Header(Sub(header)
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("ID").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("PRODUCT NAME").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("CATEGORY").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("SIZE").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("COLOR").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("PRICE").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("STOCK").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(6).Text("STATUS").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                                                End Sub)

                                                                                                                                   ' Data rows
                                                                                                                                   For Each product In inventoryDataList
                                                                                                                                       Dim statusColor = GetStatusColor(product.Status)
                                                                                                                                       Dim stockColor = GetStockColor(product.StockQty)

                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.ProductID.ToString()).FontSize(8).AlignCenter()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.ProductName).FontSize(8).SemiBold()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.Category).FontSize(8).AlignCenter().FontColor(Colors.Orange.Medium)
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.Sizing).FontSize(8).AlignCenter()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.Color).FontSize(8).AlignCenter()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text($"₱{product.Price:F2}").FontSize(8).AlignCenter().SemiBold()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.StockQty.ToString()).FontSize(8).AlignCenter().SemiBold().FontColor(stockColor)
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(4).Text(product.Status.ToUpper()).FontSize(8).AlignCenter().SemiBold().FontColor(statusColor)
                                                                                                                                   Next
                                                                                                                               End Sub)

                                                                                            ' Summary section
                                                                                            Dim totalProducts As Integer = inventoryDataList.Count
                                                                                            Dim totalValue As Decimal = inventoryDataList.Sum(Function(p) p.Price * p.StockQty)
                                                                                            Dim inStockProducts As Integer = inventoryDataList.Where(Function(p) p.Status.ToLower() = "instock").Count()
                                                                                            Dim outOfStockProducts As Integer = inventoryDataList.Where(Function(p) p.Status.ToLower().Contains("out of stock")).Count()
                                                                                            Dim lowStockProducts As Integer = inventoryDataList.Where(Function(p) p.Status.ToLower().Contains("low")).Count()
                                                                                            Dim totalStockQty As Integer = inventoryDataList.Sum(Function(p) p.StockQty)

                                                                                            column.Item().PaddingTop(20).Border(2).BorderColor(Colors.Orange.Medium).Background(Colors.Grey.Lighten5).Padding(15).Column(Sub(summaryColumn)
                                                                                                                                                                                                                             summaryColumn.Item().Text("INVENTORY SUMMARY & ANALYTICS").FontSize(12).SemiBold().FontColor(Colors.Grey.Darken2)
                                                                                                                                                                                                                             summaryColumn.Item().PaddingTop(10).Row(Sub(summaryRow)
                                                                                                                                                                                                                                                                         summaryRow.RelativeItem().Column(Sub(statsColumn)
                                                                                                                                                                                                                                                                                                              statsColumn.Item().Text($"Total Products: {totalProducts} items").FontSize(10)
                                                                                                                                                                                                                                                                                                              statsColumn.Item().Text($"Total Inventory Value: ₱{totalValue:N2}").FontSize(10).SemiBold()
                                                                                                                                                                                                                                                                                                              statsColumn.Item().Text($"Total Stock Quantity: {totalStockQty} units").FontSize(10)
                                                                                                                                                                                                                                                                                                              statsColumn.Item().Text($"In Stock: {inStockProducts} products").FontSize(10).FontColor(Colors.Green.Medium)
                                                                                                                                                                                                                                                                                                          End Sub)
                                                                                                                                                                                                                                                                         summaryRow.RelativeItem().Column(Sub(statusColumn)
                                                                                                                                                                                                                                                                                                              statusColumn.Item().Text($"Out of Stock: {outOfStockProducts} products").FontSize(10).FontColor(Colors.Red.Medium)
                                                                                                                                                                                                                                                                                                              statusColumn.Item().Text($"Low Stock: {lowStockProducts} products").FontSize(10).FontColor(Colors.Orange.Medium)
                                                                                                                                                                                                                                                                                                              statusColumn.Item().PaddingTop(10).Text("Report Generated").FontSize(10).SemiBold()
                                                                                                                                                                                                                                                                                                              statusColumn.Item().Text($"{DateTime.Now:MM/dd/yyyy} at {DateTime.Now:hh:mm tt}").FontSize(9)
                                                                                                                                                                                                                                                                                                              statusColumn.Item().Text(companyName).FontSize(9)
                                                                                                                                                                                                                                                                                                          End Sub)
                                                                                                                                                                                                                                                                     End Sub)
                                                                                                                                                                                                                         End Sub)
                                                                                        End Sub)

                                                   ' Footer
                                                   page.Footer().Row(Sub(row)
                                                                         row.RelativeItem().Column(Sub(column)
                                                                                                       ' Top border for footer
                                                                                                       column.Item().BorderTop(1).BorderColor(Colors.Grey.Lighten2).PaddingTop(5).Row(Sub(footerRow)
                                                                                                                                                                                          footerRow.RelativeItem().Background(Colors.Orange.Medium).Padding(8).AlignCenter().Text("BUSINESS CONFIDENTIAL - INTERNAL USE ONLY").FontColor(Colors.White).SemiBold().FontSize(10)
                                                                                                                                                                                      End Sub)

                                                                                                       ' Footer content with page numbers
                                                                                                       column.Item().PaddingTop(5).Row(Sub(pageRow)
                                                                                                                                           ' Left-aligned: Date and title
                                                                                                                                           pageRow.RelativeItem().AlignLeft().Text($"{DateTime.Now:MM/dd/yyyy} | {companyName}").FontSize(8).FontColor(Colors.Grey.Medium)

                                                                                                                                           ' Center-aligned: Page numbers
                                                                                                                                           pageRow.RelativeItem().AlignCenter().Text(Sub(text)
                                                                                                                                                                                         text.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium)
                                                                                                                                                                                         text.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium)
                                                                                                                                                                                         text.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium)
                                                                                                                                                                                         text.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium)
                                                                                                                                                                                     End Sub)

                                                                                                                                           ' Right-aligned: Document ID
                                                                                                                                           pageRow.RelativeItem().AlignRight().Text($"Doc ID: INV-{DateTime.Now:yyyyMMddHHmmss}").FontSize(8).FontColor(Colors.Grey.Medium)
                                                                                                                                       End Sub)
                                                                                                   End Sub)
                                                                     End Sub)
                                               End Sub)
                            End Sub).GeneratePdf(filePath)

        Finally
            ' Remove temporary logo file if it was created
            If Not String.IsNullOrEmpty(tempLogoPath) AndAlso File.Exists(tempLogoPath) Then
                Try
                    File.Delete(tempLogoPath)
                Catch ex As Exception
                    Console.WriteLine($"Failed to delete temp logo: {ex.Message}")
                End Try
            End If
        End Try
    End Sub

    ' Helper function to get status color
    Private Shared Function GetStatusColor(status As String) As QuestPDF.Infrastructure.Color
        Select Case status.ToLower()
            Case "instock", "active"
                Return Colors.Green.Medium
            Case "out of stock"
                Return Colors.Red.Medium
            Case "low on stock", "low stock"
                Return Colors.Orange.Medium
            Case "discontinued"
                Return Colors.Red.Darken1
            Case "inactive"
                Return Colors.Grey.Medium
            Case Else
                Return Colors.Grey.Darken1
        End Select
    End Function

    ' Helper function to get stock quantity color
    Private Shared Function GetStockColor(stockQty As Integer) As QuestPDF.Infrastructure.Color
        If stockQty = 0 Then
            Return Colors.Red.Medium
        ElseIf stockQty <= 10 Then
            Return Colors.Orange.Medium
        Else
            Return Colors.Green.Medium
        End If
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
    Public Property StockQty As Integer
    Public Property Status As String
    Public Property Color As String
End Class