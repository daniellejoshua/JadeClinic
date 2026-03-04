Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports Microsoft.Data.SqlClient
Imports QuestPDF.Fluent
Imports QuestPDF.Helpers
Imports QuestPDF.Infrastructure

Public Class AuditExporter

    Shared Sub New()
        QuestPDF.Settings.License = LicenseType.Community
    End Sub

    Public Shared Sub ExportAuditLogsReport(Optional sortOrder As String = "Newest First",
                                            Optional filterType As String = "All Logs",
                                            Optional filterDate As DateTime? = Nothing,
                                            Optional selectedUser As String = "All Accounts")
        Try
            Dim reportsPath As String = Path.Combine(Application.StartupPath, "auditreports")
            If Not Directory.Exists(reportsPath) Then
                Directory.CreateDirectory(reportsPath)
            End If

            Dim query As String = "SELECT a.AuditID, ISNULL(u.Username, '') AS Username, ISNULL(a.Action, '') AS Action, ISNULL(a.Details, '') AS Details, a.ActionTime " &
                                  "FROM AuditLog a LEFT JOIN Users u ON a.UserID = u.UserID"

            Dim whereClauses As New List(Of String)()
            Dim parameters As New List(Of SqlParameter)()

            Dim ft As String = If(filterType, "").Trim().ToLowerInvariant()
            Select Case ft
                Case "authentication events"
                    whereClauses.Add("(LOWER(a.Action) LIKE '%log%' OR LOWER(a.Action) LIKE '%logged%')")
                Case "navigation & access"
                    whereClauses.Add("(LOWER(a.Action) LIKE '%navigation%' OR LOWER(a.Action) LIKE '%navigate%' OR LOWER(a.Action) LIKE '%access%')")
                Case "data creation"
                    whereClauses.Add("(LOWER(a.Action) LIKE '%add%' OR LOWER(a.Action) LIKE '%create%' OR LOWER(a.Action) LIKE '%added%' OR LOWER(a.Action) LIKE '%created%')")
                Case "data updates"
                    whereClauses.Add("(LOWER(a.Action) LIKE '%update%' OR LOWER(a.Action) LIKE '%modify%' OR LOWER(a.Action) LIKE '%edit%' OR LOWER(a.Action) LIKE '%edited%')")
                Case "data deletion"
                    whereClauses.Add("(LOWER(a.Action) LIKE '%delete%' OR LOWER(a.Action) LIKE '%remove%' OR LOWER(a.Action) LIKE '%deleted%')")
                Case "export activities"
                    whereClauses.Add("(LOWER(a.Action) LIKE '%export%' OR LOWER(a.Action) LIKE '%report%')")
                Case "session management"
                    whereClauses.Add("(LOWER(a.Action) LIKE '%session%' OR LOWER(a.Action) LIKE '%pin%' OR LOWER(a.Action) LIKE '%timeout%')")
                Case "system errors"
                    whereClauses.Add("(LOWER(a.Action) LIKE '%error%' OR LOWER(a.Action) LIKE '%failed%' OR LOWER(a.Action) LIKE '%exception%')")
                Case "information events"
                    whereClauses.Add("LOWER(a.Action) NOT LIKE '%log%' AND LOWER(a.Action) NOT LIKE '%logout%' AND LOWER(a.Action) NOT LIKE '%error%' AND LOWER(a.Action) NOT LIKE '%failed%' AND LOWER(a.Action) NOT LIKE '%navigation%' AND LOWER(a.Action) NOT LIKE '%add%' AND LOWER(a.Action) NOT LIKE '%create%' AND LOWER(a.Action) NOT LIKE '%update%' AND LOWER(a.Action) NOT LIKE '%delete%' AND LOWER(a.Action) NOT LIKE '%export%'")
                Case "all logs", ""
                    ' no filter
                Case Else
                    If ft.StartsWith("authentication") Then
                        whereClauses.Add("(LOWER(a.Action) LIKE '%log%' OR LOWER(a.Action) LIKE '%logged%')")
                    End If
            End Select

            If filterDate.HasValue Then
                whereClauses.Add("CAST(a.ActionTime AS DATE) = @FilterDate")
                parameters.Add(New SqlParameter("@FilterDate", System.Data.SqlDbType.Date) With {.Value = filterDate.Value.Date})
            End If

            If Not String.IsNullOrWhiteSpace(selectedUser) AndAlso Not selectedUser.Equals("All Accounts", StringComparison.OrdinalIgnoreCase) Then
                whereClauses.Add("u.Username = @Username")
                parameters.Add(New SqlParameter("@Username", selectedUser.Trim()))
            End If

            If whereClauses.Count > 0 Then
                query &= " WHERE " & String.Join(" AND ", whereClauses)
            End If

            If sortOrder IsNot Nothing AndAlso sortOrder.ToLowerInvariant().Contains("oldest") Then
                query &= " ORDER BY a.ActionTime ASC"
            Else
                query &= " ORDER BY a.ActionTime DESC"
            End If

            Dim logs As New List(Of AuditLogReportData)()
            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters.ToArray())
                While reader.Read()
                    logs.Add(New AuditLogReportData With {
                        .AuditID = If(IsDBNull(reader("AuditID")), 0, Convert.ToInt32(reader("AuditID"))),
                        .Username = If(IsDBNull(reader("Username")) OrElse String.IsNullOrWhiteSpace(reader("Username").ToString()), "Unknown", reader("Username").ToString()),
                        .Action = If(IsDBNull(reader("Action")), "", reader("Action").ToString()),
                        .Details = If(IsDBNull(reader("Details")), "", reader("Details").ToString()),
                        .ActionTime = If(IsDBNull(reader("ActionTime")), DateTime.MinValue, Convert.ToDateTime(reader("ActionTime")))
                    })
                End While
            End Using

            If logs.Count = 0 Then
                MessageBox.Show("No audit logs found for the selected filters.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return
            End If

            Dim dateSuffix As String = If(filterDate.HasValue, $"_Date_{filterDate.Value:yyyyMMdd}", "")
            Dim userSuffix As String = If(Not String.IsNullOrWhiteSpace(selectedUser) AndAlso selectedUser <> "All Accounts", $"_User_{selectedUser.Replace(" ", "_")}", "")
            Dim fileName As String = $"Audit_Logs_Report{dateSuffix}{userSuffix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            Dim fullPath As String = Path.Combine(reportsPath, fileName)

            CreateQuestPDFAuditLogsReport(logs, fullPath, filterType, filterDate, selectedUser)

            If Not File.Exists(fullPath) Then
                Throw New Exception("PDF file was not created successfully.")
            End If

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername,
                                  "Audit Logs Report Exported",
                                  $"Filter: {filterType}, Date: {If(filterDate.HasValue, filterDate.Value.ToString("yyyy-MM-dd"), "All")}, User: {selectedUser}, Records: {logs.Count}")
            End If

            MessageBox.Show($"Audit logs report exported successfully!{vbCrLf}Filter: {filterType}{vbCrLf}Date: {If(filterDate.HasValue, filterDate.Value.ToString("yyyy-MM-dd"), "All dates")}{vbCrLf}User: {selectedUser}{vbCrLf}Records: {logs.Count}",
                            "Export Successful", MessageBoxButtons.OK, MessageBoxIcon.Information)

            Try
                Process.Start(New ProcessStartInfo(fullPath) With {.UseShellExecute = True})
            Catch
                MessageBox.Show($"PDF created but could not open automatically.{vbCrLf}File location: {fullPath}", "PDF Created", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End Try

        Catch ex As Exception
            MessageBox.Show($"Error exporting audit logs report: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error)

            If Not String.IsNullOrEmpty(frmLoginvb.LoggedInUsername) Then
                Utilities.LogAudit(frmLoginvb.LoggedInUsername, "Audit Logs Report Export Failed", $"Error: {ex.Message}")
            End If
        End Try
    End Sub

    Private Shared Sub CreateQuestPDFAuditLogsReport(logs As List(Of AuditLogReportData),
                                                      filePath As String,
                                                      filterType As String,
                                                      filterDate As DateTime?,
                                                      selectedUser As String)
        Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")
        Dim companyPhone As String = CompanySettingsManager.Instance.GetSettingString("Phone", "")
        Dim companyAddress As String = CompanySettingsManager.Instance.GetSettingString("Address", "")

        Dim totalCount As Integer = logs.Count
        Dim authCount As Integer = logs.Where(Function(x) GetActionTypeLabel(x.Action) = "AUTH").Count()
        Dim navCount As Integer = logs.Where(Function(x) GetActionTypeLabel(x.Action) = "NAV").Count()
        Dim createCount As Integer = logs.Where(Function(x) GetActionTypeLabel(x.Action) = "CREATE").Count()
        Dim updateCount As Integer = logs.Where(Function(x) GetActionTypeLabel(x.Action) = "UPDATE").Count()
        Dim deleteCount As Integer = logs.Where(Function(x) GetActionTypeLabel(x.Action) = "DELETE").Count()
        Dim errorCount As Integer = logs.Where(Function(x) GetActionTypeLabel(x.Action) = "ERROR").Count()

        Document.Create(Sub(container)
                            container.Page(Sub(page)
                                               page.Size(PageSizes.A4.Landscape())
                                               page.Margin(1.5F, Unit.Centimetre)
                                               page.PageColor(Colors.White)
                                               page.DefaultTextStyle(Function(x) x.FontSize(9))

                                               page.Header().Column(Sub(h)
                                                                        h.Item().Text(companyName).FontSize(16).SemiBold().FontColor(Colors.Orange.Medium)
                                                                        h.Item().Text("AUDIT LOGS REPORT").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken3)
                                                                        h.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd hh:mm tt} by {frmLoginvb.LoggedInUsername}").FontSize(8).FontColor(Colors.Grey.Medium)
                                                                        h.Item().Text($"Filter Type: {filterType} | Date: {If(filterDate.HasValue, filterDate.Value.ToString("yyyy-MM-dd"), "All dates")} | User: {selectedUser}").FontSize(8).FontColor(Colors.Grey.Medium)
                                                                        If Not String.IsNullOrWhiteSpace(companyPhone) OrElse Not String.IsNullOrWhiteSpace(companyAddress) Then
                                                                            h.Item().Text($"{companyPhone} {If(String.IsNullOrWhiteSpace(companyAddress), "", "| " & companyAddress)}").FontSize(7).FontColor(Colors.Grey.Medium)
                                                                        End If
                                                                    End Sub)

                                               page.Content().PaddingTop(10).Column(Sub(c)
                                                                                        c.Item().Table(Sub(table)
                                                                                                           table.ColumnsDefinition(Sub(col)
                                                                                                                                       col.ConstantColumn(45)
                                                                                                                                       col.RelativeColumn(1.3F)
                                                                                                                                       col.RelativeColumn(2.2F)
                                                                                                                                       col.RelativeColumn(3.2F)
                                                                                                                                       col.RelativeColumn(1.5F)
                                                                                                                                       col.RelativeColumn(1.0F)
                                                                                                                                   End Sub)

                                                                                                           table.Header(Sub(header)
                                                                                                                            header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("ID").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                            header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("USERNAME").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                            header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("ACTION").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                            header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("DETAILS").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                            header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("DATE & TIME").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                            header.Cell().Background(Colors.Grey.Darken3).Padding(4).Text("TYPE").FontColor(Colors.White).SemiBold().FontSize(8).AlignCenter()
                                                                                                                        End Sub)

                                                                                                           For Each item In logs
                                                                                                               Dim typeLabel As String = GetActionTypeLabel(item.Action)
                                                                                                               Dim typeColor As String = GetActionTypeColor(typeLabel)
                                                                                                               table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.AuditID.ToString()).FontSize(7).AlignCenter()
                                                                                                               table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.Username).FontSize(7)
                                                                                                               table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.Action).FontSize(7)
                                                                                                               table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.Details).FontSize(7)
                                                                                                               table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(item.ActionTime.ToString("MM/dd/yyyy HH:mm")).FontSize(7).AlignCenter()
                                                                                                               table.Cell().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(typeLabel).FontSize(7).SemiBold().FontColor(typeColor).AlignCenter()
                                                                                                           Next
                                                                                                       End Sub)

                                                                                        c.Item().PaddingTop(10).Border(1).BorderColor(Colors.Grey.Lighten2).Background(Colors.Grey.Lighten5).Padding(8).Column(Sub(summary)
                                                                                                                                                                                                                   summary.Item().Text("AUDIT SUMMARY").SemiBold().FontSize(10).FontColor(Colors.Grey.Darken3)
                                                                                                                                                                                                                   summary.Item().Text($"Total Records: {totalCount}").FontSize(9)
                                                                                                                                                                                                                   summary.Item().Text($"AUTH: {authCount} | NAV: {navCount} | CREATE: {createCount} | UPDATE: {updateCount} | DELETE: {deleteCount} | ERROR: {errorCount}").FontSize(9)
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
    End Sub

    Private Shared Function GetActionTypeLabel(action As String) As String
        Dim a As String = If(action, "").ToLowerInvariant()
        If a.Contains("login") OrElse a.Contains("logout") OrElse a.Contains("logged") Then
            Return "AUTH"
        ElseIf a.Contains("navigation") OrElse a.Contains("access") OrElse a.Contains("view") Then
            Return "NAV"
        ElseIf a.Contains("add") OrElse a.Contains("create") OrElse a.Contains("added") OrElse a.Contains("created") Then
            Return "CREATE"
        ElseIf a.Contains("update") OrElse a.Contains("modify") OrElse a.Contains("edit") OrElse a.Contains("edited") Then
            Return "UPDATE"
        ElseIf a.Contains("delete") OrElse a.Contains("remove") OrElse a.Contains("deleted") Then
            Return "DELETE"
        ElseIf a.Contains("export") OrElse a.Contains("report") Then
            Return "EXPORT"
        ElseIf a.Contains("error") OrElse a.Contains("failed") Then
            Return "ERROR"
        ElseIf a.Contains("security") OrElse a.Contains("unauthorized") Then
            Return "SECURITY"
        ElseIf a.Contains("product") OrElse a.Contains("inventory") Then
            Return "INVENTORY"
        ElseIf a.Contains("session") OrElse a.Contains("pin") Then
            Return "SESSION"
        Else
            Return "INFO"
        End If
    End Function

    Private Shared Function GetActionTypeColor(typeLabel As String) As String
        Select Case If(typeLabel, "").ToUpperInvariant()
            Case "AUTH"
                Return Colors.Blue.Medium
            Case "NAV"
                Return Colors.Teal.Medium
            Case "CREATE"
                Return Colors.Green.Medium
            Case "UPDATE"
                Return Colors.Orange.Medium
            Case "DELETE"
                Return Colors.Red.Medium
            Case "EXPORT"
                Return Colors.Indigo.Medium
            Case "ERROR"
                Return Colors.Red.Darken2
            Case "SECURITY"
                Return Colors.Purple.Medium
            Case "INVENTORY"
                Return Colors.Brown.Medium
            Case "SESSION"
                Return Colors.Cyan.Darken2
            Case Else
                Return Colors.Grey.Darken2
        End Select
    End Function

End Class

Public Class AuditLogReportData
    Public Property AuditID As Integer
    Public Property Username As String
    Public Property Action As String
    Public Property Details As String
    Public Property ActionTime As DateTime
End Class