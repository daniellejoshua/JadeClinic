Imports System.Reflection
Imports System.Drawing
Imports Guna.UI2.WinForms

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

            ' Helper to determine if a navigation button should be marked active
            Dim isActiveFor As Func(Of String, Boolean) = Function(name)
                                                              Return String.Equals(activeItem, name, StringComparison.OrdinalIgnoreCase)
                                                          End Function

            Dim createBtn As Action(Of String, Integer, Boolean, Action) = Sub(text, yPos, isActive, clickAction)
                                                                            Dim btn As New Guna2Button()
                                                                            btn.Text = text
                                                                            btn.Size = New Size(buttonWidth, buttonHeight)
                                                                            btn.Location = New Point(20, yPos)
                                                                            btn.BorderRadius = 12
                                                                            btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                                                            btn.TextAlign = HorizontalAlignment.Left
                                                                            btn.FillColor = If(isActive, Color.FromArgb(254, 191, 16), Color.Transparent)
                                                                            btn.ForeColor = If(isActive, Color.FromArgb(26, 29, 31), Color.White)
                                                                            btn.BorderThickness = If(isActive, 0, 1)
                                                                            btn.BorderColor = If(isActive, Color.Transparent, Color.FromArgb(80, 80, 80))
                                                                            btn.BackColor = Color.Transparent
                                                                            btn.Cursor = Cursors.Hand
                                                                            btn.ShadowDecoration.Enabled = True
                                                                            btn.ShadowDecoration.Color = Color.FromArgb(30, 30, 30)
                                                                            btn.ShadowDecoration.Depth = 4
                                                                            AddHandler btn.Click, Sub(s, e)
                                                                                                     Try
                                                                                                         clickAction()
                                                                                                     Catch
                                                                                                     End Try
                                                                                                 End Sub
                                                                            AddHandler btn.MouseEnter, Sub()
                                                                                                         If Not isActive Then
                                                                                                             btn.FillColor = Color.FromArgb(48, 52, 54)
                                                                                                             btn.BorderColor = Color.FromArgb(254, 191, 16)
                                                                                                             btn.Font = New Font("Poppins", 9, FontStyle.Bold)
                                                                                                         End If
                                                                                                     End Sub
                                                                            AddHandler btn.MouseLeave, Sub()
                                                                                                         If Not isActive Then
                                                                                                             btn.FillColor = Color.Transparent
                                                                                                             btn.BorderColor = Color.FromArgb(80, 80, 80)
                                                                                                             btn.Font = New Font("Poppins", 10, FontStyle.Regular)
                                                                                                         End If
                                                                                                     End Sub
                                                                            dashboardPanel.Controls.Add(btn)
                                                                        End Sub

            ' Navigation buttons (emoji icons preserved) — isActive determined from activeItem
            createBtn("🏠 Dashboard", startY + buttonIndex * (buttonHeight + buttonSpacing), isActiveFor("Dashboard"), Sub()
                                                                                                    NavigateToForm(owner, GetType(Dashboard))
                                                                                                End Sub)
            buttonIndex += 1

            createBtn("🛒 POS / Sales", startY + buttonIndex * (buttonHeight + buttonSpacing), isActiveFor("Sales"), Sub()
                                                                                                        NavigateToForm(owner, GetType(Sales))
                                                                                                    End Sub)
            buttonIndex += 1

            ' Inventory (active depending on activeItem)
            createBtn("📦 Inventory", startY + buttonIndex * (buttonHeight + buttonSpacing), isActiveFor("Inventory"), Sub()
                                                                                                          ' If we're already on Inventory, try to refresh via reflection
                                                                                                          Try
                                                                                                              If owner.GetType().Name = "Inventory" Then
                                                                                                                  Dim mi = owner.GetType().GetMethod("LoadProducts", BindingFlags.Instance Or BindingFlags.NonPublic Or BindingFlags.Public)
                                                                                                                  If mi IsNot Nothing Then mi.Invoke(owner, Nothing)
                                                                                                              Else
                                                                                                                  NavigateToForm(owner, GetType(Inventory))
                                                                                                              End If
                                                                                                          Catch
                                                                                                          End Try
                                                                                                      End Sub)
            buttonIndex += 1

            createBtn("📊 Sales Records", startY + buttonIndex * (buttonHeight + buttonSpacing), isActiveFor("SalesRecord"), Sub()
                                                                                                        NavigateToForm(owner, GetType(SalesRecord))
                                                                                                    End Sub)
            buttonIndex += 1

            If currentRole = "MANAGER" Or currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                createBtn("👥 Staff", startY + buttonIndex * (buttonHeight + buttonSpacing), isActiveFor("Staff"), Sub()
                                                                                                        NavigateToForm(owner, GetType(Staff))
                                                                                                    End Sub)
                buttonIndex += 1
            End If

            createBtn("📋 Inventory Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), isActiveFor("InventoryLog"), Sub()
                                                                                                            NavigateToForm(owner, GetType(InventoryLog))
                                                                                                        End Sub)
            buttonIndex += 1

            createBtn("🏷️ Suppliers", startY + buttonIndex * (buttonHeight + buttonSpacing), isActiveFor("Supplier"), Sub()
                                                                                                      NavigateToForm(owner, GetType(Supplier))
                                                                                                  End Sub)
            buttonIndex += 1

            createBtn("🔍 Audit Logs", startY + buttonIndex * (buttonHeight + buttonSpacing), isActiveFor("AuditLog"), Sub()
                                                                                                       NavigateToForm(owner, GetType(AuditLog))
                                                                                                   End Sub)
            buttonIndex += 1

            If currentRole = "ADMIN" Or currentRole = "ADMINISTRATOR" Then
                createBtn("⚙️ System", startY + buttonIndex * (buttonHeight + buttonSpacing), isActiveFor("Sys"), Sub()
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

            Dim frm As Form = CType(Activator.CreateInstance(targetType), Form)
            frm.StartPosition = FormStartPosition.CenterScreen
            frm.Show()
            owner.Close()
        Catch ex As Exception
            ' fallback: try to just show the form
            Try
                Dim frm As Form = CType(Activator.CreateInstance(targetType), Form)
                frm.Show()
            Catch
            End Try
        End Try
    End Sub
End Class
