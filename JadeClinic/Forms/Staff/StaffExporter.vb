Imports System.IO
Imports System.Linq
Imports System.Data.Common
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Public Class StaffExporter

    Shared Sub New()
        QuestPDF.Settings.License = LicenseType.Community
    End Sub

    Public Shared Sub ExportStaffReport(Optional sortOrder As String = "", Optional filterType As String = "All Staff", Optional filterDate As DateTime? = Nothing)
        Try
            Dim staffReportsPath As String = Path.Combine(Application.StartupPath, "staffreports")
            If Not Directory.Exists(staffReportsPath) Then
                Directory.CreateDirectory(staffReportsPath)
            End If

            Dim query As String = "SELECT UserID, Username, FullName, Email, Phone, UserRole, IsActive, CreatedAt FROM Users"
            Dim whereClauses As New List(Of String)()

            Select Case filterType
                Case "Active Staff Only"
                    whereClauses.Add("IsActive = 1")
                Case "Inactive Staff Only"
                    whereClauses.Add("IsActive = 0")
                Case "Admin Only"
                    whereClauses.Add("UserRole = 'Admin'")
                Case "Manager Only"
                    whereClauses.Add("UserRole = 'Manager'")
                Case "Staff Only"
                    whereClauses.Add("UserRole = 'Staff'")
                Case "Recently Added (Last 30 Days)"
                    whereClauses.Add("CreatedAt >= datetime('now', '-30 days')")
                Case Else
                    ' All Staff
            End Select

            If filterDate.HasValue Then
                whereClauses.Add("DATE(CreatedAt) = @FilterDate")
            End If

            If whereClauses.Count > 0 Then
                query &= " WHERE " & String.Join(" AND ", whereClauses)
            End If

            Select Case sortOrder
                Case "Username (A-Z)"
                    query &= " ORDER BY Username ASC"
                Case "Username (Z-A)"
                    query &= " ORDER BY Username DESC"
                Case "User ID (Ascending)"
                    query &= " ORDER BY UserID ASC"
                Case "User ID (Descending)"
                    query &= " ORDER BY UserID DESC"
                Case "Full Name (A-Z)"
                    query &= " ORDER BY FullName ASC"
                Case "Full Name (Z-A)"
                    query &= " ORDER BY FullName DESC"
                Case "Role (A-Z)"
                    query &= " ORDER BY UserRole ASC"
                Case "Role (Z-A)"
                    query &= " ORDER BY UserRole DESC"
                Case "Date Added (Newest First)"
                    query &= " ORDER BY CreatedAt DESC"
                Case "Date Added (Oldest First)"
                    query &= " ORDER BY CreatedAt ASC"
                Case Else
                    query &= " ORDER BY UserID ASC"
            End Select

            Dim staffDataList As New List(Of StaffReportData)()
            Dim parameters As New List(Of SqlParameter)()
            If filterDate.HasValue Then
                parameters.Add(New SqlParameter("@FilterDate", filterDate.Value.Date))
            End If

            Using reader As DbDataReader = Utilities.ExecuteReader(query, parameters.ToArray())
                While reader.Read()
                    Dim staffData As New StaffReportData() With {
                        .UserID = If(IsDBNull(reader("UserID")), 0, Convert.ToInt32(reader("UserID"))),
                        .Username = If(IsDBNull(reader("Username")), "", reader("Username").ToString()),
                        .FullName = If(IsDBNull(reader("FullName")), "", reader("FullName").ToString()),
                        .Email = If(IsDBNull(reader("Email")), "", reader("Email").ToString()),
                        .Phone = If(IsDBNull(reader("Phone")), "", reader("Phone").ToString()),
                        .UserRole = If(IsDBNull(reader("UserRole")), "Staff", reader("UserRole").ToString()),
                        .IsActive = If(IsDBNull(reader("IsActive")), True, Convert.ToBoolean(reader("IsActive"))),
                        .CreatedAt = If(IsDBNull(reader("CreatedAt")), DateTime.MinValue, Convert.ToDateTime(reader("CreatedAt")))
                    }

                    If staffData.UserID > 0 Then
                        staffDataList.Add(staffData)
                    End If
                End While
            End Using

            If staffDataList.Count = 0 Then
                MessageBox.Show("No staff records found to export.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim dateSuffix As String = If(filterDate.HasValue, $"_Date_{filterDate.Value:yyyyMMdd}", "")
            Dim fileName As String = $"Staff_Report{dateSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            Dim fullPath As String = Path.Combine(staffReportsPath, fileName)

            CreateQuestPDFStaffReport(staffDataList, fullPath, filterType, filterDate)

            If Not File.Exists(fullPath) Then
                Throw New Exception("PDF file was not created successfully.")
            End If

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Dim dateFilterInfo As String = If(filterDate.HasValue, $", Date: {filterDate.Value:yyyy-MM-dd}", ", Date: All dates")
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Staff Report Exported", $"Filter: {filterType}{dateFilterInfo}, Records: {staffDataList.Count}")
            End If

            Dim dateFilterMessage As String = If(filterDate.HasValue, $"{vbCrLf}Date Filter: {filterDate.Value:yyyy-MM-dd}", $"{vbCrLf}Date Filter: All dates")
            MessageBox.Show($"Staff report exported successfully!{vbCrLf}Filter Applied: {filterType}{dateFilterMessage}{vbCrLf}Records Exported: {staffDataList.Count}{vbCrLf}Opening PDF now...",
                            "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Try
                Process.Start(New ProcessStartInfo(fullPath) With {.UseShellExecute = True})
            Catch
                MessageBox.Show($"PDF created successfully but couldn't open automatically.{vbCrLf}File location: {fullPath}",
                                "PDF Created", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try

        Catch ex As Exception
            Dim errorMessage As String = $"Error exporting staff report: {ex.Message}"
            If ex.InnerException IsNot Nothing Then
                errorMessage += $"{vbCrLf}Details: {ex.InnerException.Message}"
            End If

            MessageBox.Show(errorMessage, "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Staff Report Export Failed", $"Error: {ex.Message}")
            End If
        End Try
    End Sub

    Private Shared Sub CreateQuestPDFStaffReport(staffDataList As List(Of StaffReportData), filePath As String, filterType As String, Optional filterDate As DateTime? = Nothing)
        Dim tempLogoPath As String = Nothing

        Try
            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")
            Dim companyTagline As String = CompanySettingsManager.Instance.GetSettingString("CompanyTagline", "Professional Staff Management & Human Resources")
            Dim companyPhone As String = CompanySettingsManager.Instance.GetSettingString("Phone", "")
            Dim companyAddress As String = CompanySettingsManager.Instance.GetSettingString("Address", "")
            Dim companyTIN As String = CompanySettingsManager.Instance.GetSettingString("TIN", "")

            ' Save logo to a temporary file for QuestPDF image usage
            Try
                Dim logoImg As System.Drawing.Image = CompanySettingsManager.Instance.GetCompanyLogo()
                If logoImg IsNot Nothing Then
                    Dim outDir = Path.GetDirectoryName(filePath)
                    If String.IsNullOrEmpty(outDir) Then outDir = Application.StartupPath
                    tempLogoPath = Path.Combine(outDir, "staff_report_logo.png")
                    logoImg.Save(tempLogoPath, System.Drawing.Imaging.ImageFormat.Png)
                End If
            Catch
                tempLogoPath = Nothing
            End Try

            ' Analytics
            Dim totalStaff As Integer = staffDataList.Count
            Dim activeStaff As Integer = staffDataList.Where(Function(s) s.IsActive).Count()
            Dim inactiveStaff As Integer = totalStaff - activeStaff
            Dim adminCount As Integer = staffDataList.Where(Function(s) s.UserRole.Equals("Admin", StringComparison.OrdinalIgnoreCase)).Count()
            Dim managerCount As Integer = staffDataList.Where(Function(s) s.UserRole.Equals("Manager", StringComparison.OrdinalIgnoreCase)).Count()
            Dim staffCount As Integer = staffDataList.Where(Function(s) s.UserRole.Equals("Staff", StringComparison.OrdinalIgnoreCase)).Count()

            Document.Create(Sub(container)
                                container.Page(Sub(page)
                                                   page.Size(PageSizes.A4.Portrait())
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

                                                                            h.Item().PaddingTop(12).AlignCenter().Text("STAFF MANAGEMENT REPORT").FontSize(18).SemiBold().FontColor(Colors.Grey.Darken3)

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
                                                                                            ' Summary Section
                                                                                            column.Item().Border(2).BorderColor(Colors.Orange.Medium).Background(Colors.Grey.Lighten5).Padding(10).Column(Sub(summary)
                                                                                                                                                                                                              summary.Item().Text("STAFF SUMMARY").FontSize(11).SemiBold().FontColor(Colors.Grey.Darken3)
                                                                                                                                                                                                              summary.Item().PaddingTop(4).Row(Sub(sRow)
                                                                                                                                                                                                                                                   sRow.RelativeItem().Column(Sub(left)
                                                                                                                                                                                                                                                                                  left.Item().Text($"Total Staff: {totalStaff}").FontSize(9).SemiBold()
                                                                                                                                                                                                                                                                                  left.Item().Text($"Active Staff: {activeStaff}").FontSize(9).FontColor(Colors.Green.Medium)
                                                                                                                                                                                                                                                                                  left.Item().Text($"Inactive Staff: {inactiveStaff}").FontSize(9).FontColor(Colors.Red.Medium)
                                                                                                                                                                                                                                                                              End Sub)
                                                                                                                                                                                                                                                   sRow.RelativeItem().Column(Sub(right)
                                                                                                                                                                                                                                                                                  right.Item().Text($"Administrators: {adminCount}").FontSize(9).FontColor(Colors.Blue.Medium)
                                                                                                                                                                                                                                                                                  right.Item().Text($"Managers: {managerCount}").FontSize(9).FontColor(Colors.Purple.Medium)
                                                                                                                                                                                                                                                                                  right.Item().Text($"Staff Members: {staffCount}").FontSize(9).FontColor(Colors.Orange.Medium)
                                                                                                                                                                                                                                                                              End Sub)
                                                                                                                                                                                                                                               End Sub)
                                                                                                                                                                                                          End Sub)

                                                                                            column.Item().PaddingTop(12).Table(Sub(table)
                                                                                                                                   table.ColumnsDefinition(Sub(c)
                                                                                                                                                               c.ConstantColumn(30)
                                                                                                                                                               c.RelativeColumn(2.5F)
                                                                                                                                                               c.RelativeColumn(3)
                                                                                                                                                               c.RelativeColumn(3.5F)
                                                                                                                                                               c.RelativeColumn(2.5F)
                                                                                                                                                               c.RelativeColumn(1.8F)
                                                                                                                                                               c.RelativeColumn(1.2F)
                                                                                                                                                               c.RelativeColumn(2)
                                                                                                                                                           End Sub)

                                                                                                                                   table.Header(Sub(header)
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("ID").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("USERNAME").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("FULL NAME").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("EMAIL").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("PHONE").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("ROLE").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("STATUS").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                    header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("DATE ADDED").FontColor(Colors.White).SemiBold().FontSize(7).AlignCenter()
                                                                                                                                                End Sub)

                                                                                                                                   For Each staff In staffDataList
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(staff.UserID.ToString()).FontSize(7).AlignCenter()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(staff.Username).FontSize(7)
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(staff.FullName).FontSize(7)
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(staff.Email).FontSize(7)
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(staff.Phone).FontSize(7)

                                                                                                                                       ' Role with color coding
                                                                                                                                       Dim roleColor = Colors.Grey.Medium
                                                                                                                                       Select Case staff.UserRole.ToUpper()
                                                                                                                                           Case "ADMIN", "ADMINISTRATOR"
                                                                                                                                               roleColor = Colors.Blue.Medium
                                                                                                                                           Case "MANAGER"
                                                                                                                                               roleColor = Colors.Purple.Medium
                                                                                                                                           Case "STAFF"
                                                                                                                                               roleColor = Colors.Orange.Medium
                                                                                                                                       End Select
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(staff.UserRole).FontSize(7).FontColor(roleColor).AlignCenter()

                                                                                                                                       ' Status with color coding
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(If(staff.IsActive, "Active", "Inactive")).FontSize(7).FontColor(If(staff.IsActive, Colors.Green.Medium, Colors.Red.Medium)).AlignCenter()
                                                                                                                                       table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(staff.CreatedAt.ToString("MM/dd/yyyy")).FontSize(7).AlignCenter()
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
            Throw New Exception($"QuestPDF Staff Report Creation Error: {ex.Message}", ex)
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

Public Class StaffReportData
    Public Property UserID As Integer
    Public Property Username As String
    Public Property FullName As String
    Public Property Email As String
    Public Property Phone As String
    Public Property UserRole As String
    Public Property IsActive As Boolean
    Public Property CreatedAt As DateTime
End Class