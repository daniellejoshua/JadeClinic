Imports System.Reflection
Imports System.Drawing
Imports Guna.UI2.WinForms
Imports Microsoft.Data.SqlClient

Public NotInheritable Class NavigationBuilder
    Private Sub New()
    End Sub

    Public Shared Sub Build(dashboardPanel As Guna2Panel, owner As Form, activeItem As String)
        Try
            ' Remove all children except the PictureBox9 logo
            For i = dashboardPanel.Controls.Count - 1 To 0 Step -1
                Dim control As Control = dashboardPanel.Controls(i)
                If TypeOf control IsNot PictureBox Then
                    dashboardPanel.Controls.Remove(control)
                    control.Dispose()
                End If
            Next

            dashboardPanel.FillColor = Color.FromArgb(61, 65, 66)

            ' Render company logo into existing PictureBox9 if present
            Dim logoCtrl() As Control = dashboardPanel.Controls.Find("PictureBox9", True)
            If logoCtrl IsNot Nothing AndAlso logoCtrl.Length > 0 Then
                Dim pb As PictureBox = TryCast(logoCtrl(0), PictureBox)
                If pb IsNot Nothing Then
                    Try
                        Dim logoImg As System.Drawing.Image = CompanySettingsManager.Instance.GetCompanyLogo()
                        If logoImg IsNot Nothing Then
                            pb.Image = logoImg
                            pb.Location = New Point(81, 15)
                        End If
                    Catch
                        ' ignore
                    End Try
                    pb.BringToFront()
                End If
            End If

            Dim availableWidth As Integer = Math.Max(200, dashboardPanel.Width - 40)
            Dim startY As Integer = 250
            Dim buttonHeight As Integer = 50
            Dim buttonSpacing As Integer = 15
            Dim buttonWidth As Integer = Math.Max(120, availableWidth - 5)
            Dim buttonIndex As Integer = 0

            Dim companyName As String = CompanySettingsManager.Instance.GetSettingString("CompanyName", "JADE CLINIC")

            Dim titleLabel As New Label() With {
                .Text = companyName,
                .Font = New Font("Poppins", 14, FontStyle.Bold),
                .ForeColor = Color.FromArgb(254, 191, 16),
                .BackColor = Color.Transparent,
                .AutoSize = False,
                .Size = New Size(availableWidth, 30),
                .Location = New Point(20, 110),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            dashboardPanel.Controls.Add(titleLabel)

            Dim subtitleLabel As New Label() With {
                .Text = "Dental Supply Management",
                .Font = New Font("Poppins", 10, FontStyle.Regular),
                .ForeColor = Color.FromArgb(225, 229, 233),
                .BackColor = Color.Transparent,
                .AutoSize = False,
                .Size = New Size(availableWidth, 25),
                .Location = New Point(20, 145),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            dashboardPanel.Controls.Add(subtitleLabel)

            Dim separator1 As New Panel() With {
                .BackColor = Color.FromArgb(50, 50, 50),
                .Size = New Size(availableWidth - 20, 2),
                .Location = New Point(30, 190)
            }
            dashboardPanel.Controls.Add(separator1)

            Dim navLabel As New Label() With {
                .Text = "NAVIGATION",
                .Font = New Font("Poppins", 10, FontStyle.Bold),
                .ForeColor = Color.FromArgb(225, 229, 233),
                .BackColor = Color.Transparent,
                .AutoSize = False,
                .Size = New Size(availableWidth, 25),
                .Location = New Point(20, 205),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            dashboardPanel.Controls.Add(navLabel)

            Dim currentRole As String = If(frmLoginvb.LoggedInRole, "Staff").ToString().ToUpper()

            Dim createBtn As Action(Of String, Type, Integer, Action) = Sub(text, targetType, yPos, clickAction)
                                                                            Dim isActiveBtn As Boolean = String.Equals(activeItem, targetType.Name, StringComparison.OrdinalIgnoreCase) OrElse String.Equals(owner.GetType().Name, targetType.Name, StringComparison.OrdinalIgnoreCase)
                                                                            Dim btn As New Guna2Button()
                                                                            btn.Text = text
                                                                            btn.Size = New Size(buttonWidth, buttonHeight)
                                                                            btn.Location = New Point(20, yPos)
                                                                            btn.BorderRadius = 12
                                                                            btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                                                            btn.TextAlign = HorizontalAlignment.Left
                                                                            btn.FillColor = If(isActiveBtn, Color.FromArgb(254, 191, 16), Color.Transparent)
                                                                            btn.ForeColor = If(isActiveBtn, Color.FromArgb(26, 29, 31), Color.White)
                                                                            btn.BorderThickness = If(isActiveBtn, 0, 1)
                                                                            btn.BorderColor = If(isActiveBtn, Color.Transparent, Color.FromArgb(80, 80, 80))
                                                                            btn.BackColor = Color.Transparent
                                                                            btn.Cursor = Cursors.Hand
                                                                            btn.ShadowDecoration.Enabled = True
                                                                            btn.ShadowDecoration.Color = Color.FromArgb(30, 30, 30)
                                                                            btn.ShadowDecoration.Depth = 4
                                                                            AddHandler btn.Click, Sub(s, e)
                                                                                                      Try
                                                                                                          If isActiveBtn Then
                                                                                                              ' Try to refresh the current form via common refresh methods if available
                                                                                                              Dim refreshCandidates As String() = {"Refresh", "Reload", "LoadProducts", "LoadDashboardData", "LoadChartData", "RefreshData"}
                                                                                                              For Each mName In refreshCandidates
                                                                                                                  Dim mi = owner.GetType().GetMethod(mName, BindingFlags.Instance Or BindingFlags.NonPublic Or BindingFlags.Public)
                                                                                                                  If mi IsNot Nothing Then
                                                                                                                      mi.Invoke(owner, Nothing)
                                                                                                                      Exit For
                                                                                                                  End If
                                                                                                              Next
                                                                                                          Else
                                                                                                              clickAction()
                                                                                                          End If
                                                                                                      Catch
                                                                                                      End Try
                                                                                                  End Sub
                                                                            AddHandler btn.MouseEnter, Sub()
                                                                                                           If Not isActiveBtn Then
                                                                                                               btn.FillColor = Color.FromArgb(48, 52, 54)
                                                                                                               btn.BorderColor = Color.FromArgb(254, 191, 16)
                                                                                                               btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                                                                                           End If
                                                                                                       End Sub
                                                                            AddHandler btn.MouseLeave, Sub()
                                                                                                           If Not isActiveBtn Then
                                                                                                               btn.FillColor = Color.Transparent
                                                                                                               btn.BorderColor = Color.FromArgb(80, 80, 80)
                                                                                                               btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                                                                                           End If
                                                                                                       End Sub
                                                                            dashboardPanel.Controls.Add(btn)
                                                                        End Sub

            ' Navigation buttons (emoji icons preserved) - use target types so active state is computed automatically
            createBtn("🏠 Dashboard", GetType(Dashboard), startY + buttonIndex * (buttonHeight + buttonSpacing), Sub()
                                                                                                                     NavigateToForm(owner, GetType(Dashboard))
                                                                                                                 End Sub)
            buttonIndex += 1

            createBtn("🛒 POS / Sales", GetType(Sales), startY + buttonIndex * (buttonHeight + buttonSpacing), Sub()
                                                                                                                   NavigateToForm(owner, GetType(Sales))
                                                                                                               End Sub)
            buttonIndex += 1

            ' Inventory
            createBtn("📦 Inventory", GetType(Inventory), startY + buttonIndex * (buttonHeight + buttonSpacing), Sub()
                                                                                                                     NavigateToForm(owner, GetType(Inventory))
                                                                                                                 End Sub)
            buttonIndex += 1

            createBtn("📊 Sales Records", GetType(SalesRecord), startY + buttonIndex * (buttonHeight + buttonSpacing), Sub()
                                                                                                                           NavigateToForm(owner, GetType(SalesRecord))
                                                                                                                       End Sub)
            buttonIndex += 1

            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                createBtn("👥 Staff", GetType(Staff), startY + buttonIndex * (buttonHeight + buttonSpacing), Sub()
                                                                                                                 NavigateToForm(owner, GetType(Staff))
                                                                                                             End Sub)
                buttonIndex += 1
            End If

            createBtn("📋 Inventory Logs", GetType(InventoryLog), startY + buttonIndex * (buttonHeight + buttonSpacing), Sub()
                                                                                                                             NavigateToForm(owner, GetType(InventoryLog))
                                                                                                                         End Sub)
            buttonIndex += 1

            createBtn("🏷️ Suppliers", GetType(Supplier), startY + buttonIndex * (buttonHeight + buttonSpacing), Sub()
                                                                                                                     NavigateToForm(owner, GetType(Supplier))
                                                                                                                 End Sub)
            buttonIndex += 1

            createBtn("🔍 Audit Logs", GetType(AuditLog), startY + buttonIndex * (buttonHeight + buttonSpacing), Sub()
                                                                                                                     NavigateToForm(owner, GetType(AuditLog))
                                                                                                                 End Sub)
            buttonIndex += 1

            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                createBtn("⚙️ System", GetType(Sys), startY + buttonIndex * (buttonHeight + buttonSpacing), Sub()
                                                                                                                NavigateToForm(owner, GetType(Sys))
                                                                                                            End Sub)
                buttonIndex += 1
            End If

        Catch ex As Exception
            Console.WriteLine($"NavigationBuilder error: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub NavigateToForm(owner As Form, targetType As Type)
        Try
            ' Try to set private isNavigating flag via reflection to avoid close confirmation
            Try
                Dim fld = owner.GetType().GetField("isNavigating", BindingFlags.Instance Or BindingFlags.NonPublic)
                If fld IsNot Nothing Then fld.SetValue(owner, True)
            Catch
                ' ignore
            End Try

            ' Instantiate and show the new form, then close the owner. Capital enforcement for Sales is handled within the Sales form's Shown handler to ensure the form is visible before any modal dialog.
            Dim frm As Form = CType(Activator.CreateInstance(targetType), Form)
            Try
                frm.StartPosition = FormStartPosition.CenterScreen
                frm.TopMost = True
                frm.WindowState = FormWindowState.Normal
                frm.Bounds = Screen.PrimaryScreen.Bounds
                frm.WindowState = FormWindowState.Maximized
            Catch
                ' ignore layout errors for forms that customize their own layout
            End Try

            frm.Show()
            Try
                frm.BringToFront()
                frm.Activate()
                Application.DoEvents()
            Catch
                ' ignore activation errors
            End Try

            Try
                owner.Close()
            Catch
                ' ignore close errors
            End Try

        Catch ex As Exception
            ' fallback: try to just show the form without fullscreen changes
            Try
                Dim frm2 As Form = CType(Activator.CreateInstance(targetType), Form)
                frm2.Show()
            Catch
                ' ignore
            End Try
        End Try
    End Sub
End Class
