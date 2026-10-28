Imports System.Data.Common
Imports System.Text.RegularExpressions
Imports Npgsql
Imports Guna.UI2.WinForms

' Shared manager/admin authorization used by POS voids and returns/exchanges.
' The visual layout is the original POS void-authorization design.
Public Module ApprovalDialogs

    Private ReadOnly JadeOlive As Color = Color.FromArgb(190, 154, 48)
    Private ReadOnly GoldenYellow As Color = Color.FromArgb(254, 191, 16)
    Private ReadOnly OffWhite As Color = Color.FromArgb(249, 249, 249)
    Private ReadOnly PureWhite As Color = Color.FromArgb(255, 255, 255)
    Private ReadOnly DarkText As Color = Color.FromArgb(51, 51, 51)
    Private ReadOnly MediumText As Color = Color.FromArgb(120, 120, 120)
    Private ReadOnly BorderGray As Color = Color.FromArgb(228, 228, 228)
    Private ReadOnly SuccessGreen As Color = Color.FromArgb(16, 200, 100)
    Private ReadOnly AlertRed As Color = Color.FromArgb(235, 80, 80)

    ' Returns the approving username, or an empty string when cancelled/failed.
    Public Function AuthorizeElevatedUser(owner As IWin32Window,
                                          title As String,
                                          subtitle As String,
                                          Optional detail As String = "",
                                          Optional warning As String = "",
                                          Optional excludedUsername As String = "") As String
        Dim dlg As New Form With {
            .Text = title,
            .Size = New Size(560, 500),
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .MaximizeBox = False,
            .MinimizeBox = False,
            .BackColor = OffWhite,
            .KeyPreview = True,
            .ShowInTaskbar = False
        }

        Dim lblTitle As New Label With {
            .Text = title.ToUpperInvariant(),
            .Font = New Font("Poppins", 16, FontStyle.Bold),
            .ForeColor = Color.Black,
            .AutoSize = False,
            .Size = New Size(520, 34),
            .Location = New Point(20, 16),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        dlg.Controls.Add(lblTitle)

        Dim lblSubtitle As New Label With {
            .Text = subtitle,
            .Font = New Font("Poppins", 11, FontStyle.Bold),
            .ForeColor = GoldenYellow,
            .AutoSize = False,
            .Size = New Size(520, 24),
            .Location = New Point(20, 56),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        dlg.Controls.Add(lblSubtitle)

        Dim lblDetail As New Label With {
            .Text = detail,
            .Font = New Font("Poppins", 9, FontStyle.Regular),
            .ForeColor = MediumText,
            .AutoSize = False,
            .Size = New Size(520, 20),
            .Location = New Point(20, 82),
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoEllipsis = True,
            .Visible = Not String.IsNullOrWhiteSpace(detail)
        }
        dlg.Controls.Add(lblDetail)

        Dim lblWarning As New Label With {
            .Text = warning,
            .Font = New Font("Poppins", 9, FontStyle.Bold),
            .ForeColor = AlertRed,
            .AutoSize = False,
            .Size = New Size(520, 20),
            .Location = New Point(20, 104),
            .TextAlign = ContentAlignment.MiddleCenter,
            .AutoEllipsis = True,
            .Visible = Not String.IsNullOrWhiteSpace(warning)
        }
        dlg.Controls.Add(lblWarning)
        Dim approvalToolTip As New ToolTip()
        approvalToolTip.SetToolTip(lblDetail, detail)
        approvalToolTip.SetToolTip(lblWarning, warning)

        Dim btnQrMode As New Guna2Button With {
            .Text = "QR",
            .Size = New Size(240, 42),
            .Location = New Point(30, 126),
            .BorderRadius = 10,
            .FillColor = JadeOlive,
            .ForeColor = PureWhite,
            .Font = New Font("Poppins", 11, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }
        btnQrMode.ShadowDecoration.Enabled = False

        Dim btnPassMode As New Guna2Button With {
            .Text = "User/Pass",
            .Size = New Size(240, 42),
            .Location = New Point(290, 126),
            .BorderRadius = 10,
            .FillColor = BorderGray,
            .ForeColor = PureWhite,
            .Font = New Font("Poppins", 11, FontStyle.Bold),
            .Cursor = Cursors.Hand
        }
        btnPassMode.ShadowDecoration.Enabled = False
        dlg.Controls.Add(btnQrMode)
        dlg.Controls.Add(btnPassMode)

        Dim lblScanInstruction As New Label With {
            .Text = "Scan manager/admin QR now",
            .Font = New Font("Poppins", 10, FontStyle.Italic),
            .ForeColor = MediumText,
            .AutoSize = False,
            .Size = New Size(500, 24),
            .Location = New Point(30, 212),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        dlg.Controls.Add(lblScanInstruction)

        Dim txtQr As New Guna2TextBox With {
            .Size = New Size(1, 1),
            .Location = New Point(dlg.ClientSize.Width - 6, dlg.ClientSize.Height - 6),
            .BorderThickness = 0,
            .FillColor = OffWhite,
            .ForeColor = OffWhite,
            .PlaceholderText = "",
            .TabStop = False
        }
        dlg.Controls.Add(txtQr)

        Dim pnlPass As New Panel With {
            .Location = New Point(30, 178),
            .Size = New Size(500, 178),
            .BackColor = Color.Transparent,
            .Visible = False
        }
        Dim lblUser As New Label With {
            .Text = "Username",
            .ForeColor = MediumText,
            .Font = New Font("Poppins", 8),
            .AutoSize = True,
            .Location = New Point(0, 0)
        }
        Dim txtUser As New Guna2TextBox With {
            .Size = New Size(500, 40),
            .Location = New Point(0, 26),
            .BorderRadius = 8,
            .FillColor = PureWhite,
            .ForeColor = DarkText,
            .Font = New Font("Poppins", 9.5),
            .BorderColor = BorderGray
        }
        Dim lblPass As New Label With {
            .Text = "Password",
            .ForeColor = MediumText,
            .Font = New Font("Poppins", 8),
            .AutoSize = True,
            .Location = New Point(0, 80)
        }
        Dim txtPass As New Guna2TextBox With {
            .Size = New Size(500, 40),
            .Location = New Point(0, 108),
            .BorderRadius = 8,
            .FillColor = PureWhite,
            .ForeColor = DarkText,
            .Font = New Font("Poppins", 9.5),
            .UseSystemPasswordChar = True,
            .BorderColor = BorderGray
        }
        pnlPass.Controls.Add(lblUser)
        pnlPass.Controls.Add(txtUser)
        pnlPass.Controls.Add(lblPass)
        pnlPass.Controls.Add(txtPass)
        dlg.Controls.Add(pnlPass)

        Dim lblStatus As New Label With {
            .Text = "Waiting for manager/admin authorization...",
            .ForeColor = MediumText,
            .Font = New Font("Poppins", 9, FontStyle.Italic),
            .AutoSize = False,
            .Size = New Size(500, 20),
            .Location = New Point(30, 366),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        dlg.Controls.Add(lblStatus)

        Dim btnAuthorize As New Guna2Button With {
            .Text = "Authorize",
            .Size = New Size(180, 44),
            .Location = New Point(290, 405),
            .BorderRadius = 10,
            .FillColor = SuccessGreen,
            .ForeColor = DarkText,
            .Font = New Font("Poppins", 10, FontStyle.Bold),
            .Visible = False,
            .Cursor = Cursors.Hand
        }
        btnAuthorize.ShadowDecoration.Enabled = False

        Dim btnCancel As New Guna2Button With {
            .Text = "Cancel",
            .Size = New Size(140, 44),
            .Location = New Point(110, 405),
            .BorderRadius = 10,
            .FillColor = AlertRed,
            .ForeColor = PureWhite,
            .Font = New Font("Poppins", 10),
            .Cursor = Cursors.Hand
        }
        btnCancel.ShadowDecoration.Enabled = False
        dlg.Controls.Add(btnAuthorize)
        dlg.Controls.Add(btnCancel)

        Dim authorized As Boolean = False
        Dim approver As String = ""
        Dim qrMode As Boolean = True
        Dim isAuthorizingQr As Boolean = False
        Dim qrTimer As New Timer With {.Interval = 150}

        Dim tryAuthorizeQr As Action =
            Sub()
                If Not qrMode OrElse isAuthorizingQr OrElse authorized Then Return
                Dim qrRaw As String = txtQr.Text.Trim()
                If String.IsNullOrWhiteSpace(qrRaw) Then Return

                isAuthorizingQr = True
                Try
                    lblStatus.Text = "Processing QR authorization..."
                    lblStatus.ForeColor = MediumText
                    Dim approvedBy As String = ""
                    If TryAuthorizeByQr(qrRaw, approvedBy) Then
                        approver = approvedBy
                        authorized = True
                        dlg.DialogResult = DialogResult.OK
                        dlg.Close()
                    Else
                        lblStatus.Text = "Authorization failed. Manager/Admin required."
                        lblStatus.ForeColor = AlertRed
                        MessageBox.Show("Invalid QR code or insufficient role. Manager/Admin authorization is required.", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        txtQr.Clear()
                        txtQr.Focus()
                    End If
                Finally
                    isAuthorizingQr = False
                End Try
            End Sub

        AddHandler btnQrMode.Click,
            Sub()
                qrMode = True
                btnQrMode.FillColor = JadeOlive
                btnPassMode.FillColor = BorderGray
                pnlPass.Visible = False
                lblScanInstruction.Visible = True
                btnAuthorize.Visible = False
                txtQr.Clear()
                lblStatus.Text = "Waiting for manager/admin authorization..."
                lblStatus.ForeColor = MediumText
                txtQr.Focus()
            End Sub

        AddHandler btnPassMode.Click,
            Sub()
                qrMode = False
                btnPassMode.FillColor = JadeOlive
                btnQrMode.FillColor = BorderGray
                pnlPass.Visible = True
                lblScanInstruction.Visible = False
                btnAuthorize.Visible = True
                txtUser.Focus()
            End Sub

        AddHandler btnCancel.Click,
            Sub()
                dlg.DialogResult = DialogResult.Cancel
                dlg.Close()
            End Sub

        AddHandler qrTimer.Tick,
            Sub()
                qrTimer.Stop()
                tryAuthorizeQr()
            End Sub
        AddHandler txtQr.TextChanged,
            Sub()
                If Not qrMode Then Return
                qrTimer.Stop()
                If txtQr.TextLength > 0 Then qrTimer.Start()
            End Sub

        AddHandler btnAuthorize.Click,
            Sub()
                Dim approvedBy As String = ""
                If TryAuthorizeByPassword(txtUser.Text.Trim(), txtPass.Text, approvedBy) Then
                    approver = approvedBy
                    authorized = True
                    dlg.DialogResult = DialogResult.OK
                    dlg.Close()
                Else
                    lblStatus.Text = "Authorization failed. Manager/Admin required."
                    lblStatus.ForeColor = AlertRed
                    MessageBox.Show("Invalid credentials or insufficient role. Manager/Admin authorization is required.", "Authorization Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                End If
            End Sub

        AddHandler txtQr.KeyDown,
            Sub(sender, args)
                If args.KeyCode = Keys.Enter Then
                    qrTimer.Stop()
                    tryAuthorizeQr()
                    args.Handled = True
                End If
            End Sub
        AddHandler txtPass.KeyDown,
            Sub(sender, args)
                If args.KeyCode = Keys.Enter Then
                    btnAuthorize.PerformClick()
                    args.Handled = True
                End If
            End Sub
        AddHandler dlg.KeyDown,
            Sub(sender, args)
                If args.KeyCode = Keys.Escape Then
                    btnCancel.PerformClick()
                    args.Handled = True
                End If
            End Sub
        AddHandler dlg.FormClosed,
            Sub()
                qrTimer.Stop()
                qrTimer.Dispose()
            End Sub
        AddHandler dlg.Shown,
            Sub()
                If qrMode Then
                    dlg.BeginInvoke(New Action(Sub() txtQr.Focus()))
                End If
            End Sub

        btnQrMode.PerformClick()
        dlg.ShowDialog(owner)
        Return If(authorized, approver, "")
    End Function

    Private Function TryAuthorizeByQr(qrRaw As String, ByRef approverUsername As String) As Boolean
        approverUsername = ""
        If String.IsNullOrWhiteSpace(qrRaw) Then Return False

        Dim token As String = ExtractManagerQrToken(qrRaw)
        If String.IsNullOrWhiteSpace(token) Then Return False

        Dim sql As String = "SELECT Username, UserRole, IsActive FROM Users WHERE QRCode = @QRCode LIMIT 1"
        Using reader As DbDataReader = Utilities.ExecuteReader(sql, New NpgsqlParameter("@QRCode", token))
            If reader.Read() Then
                Dim userName As String = If(IsDBNull(reader("Username")), "", reader("Username").ToString())
                Dim role As String = If(IsDBNull(reader("UserRole")), "", reader("UserRole").ToString())
                Dim active As Boolean = If(IsDBNull(reader("IsActive")), False, Convert.ToBoolean(reader("IsActive")))
                If active AndAlso IsElevatedRole(role) Then
                    approverUsername = userName
                    Return True
                End If
            End If
        End Using
        Return False
    End Function

    Private Function TryAuthorizeByPassword(managerUsername As String, managerPassword As String, ByRef approverUsername As String) As Boolean
        approverUsername = ""
        If String.IsNullOrWhiteSpace(managerUsername) OrElse String.IsNullOrWhiteSpace(managerPassword) Then Return False

        Dim sql As String = "SELECT Username, UserRole, IsActive, PasswordHash FROM Users WHERE Username = @Username LIMIT 1"
        Using reader As DbDataReader = Utilities.ExecuteReader(sql, New NpgsqlParameter("@Username", managerUsername))
            If reader.Read() Then
                Dim role As String = If(IsDBNull(reader("UserRole")), "", reader("UserRole").ToString())
                Dim active As Boolean = If(IsDBNull(reader("IsActive")), False, Convert.ToBoolean(reader("IsActive")))
                Dim hash As String = If(IsDBNull(reader("PasswordHash")), "", reader("PasswordHash").ToString())
                If Not active OrElse Not IsElevatedRole(role) Then Return False
                If Not VerifyStoredPassword(managerPassword, hash) Then Return False
                approverUsername = If(IsDBNull(reader("Username")), managerUsername, reader("Username").ToString())
                Return True
            End If
        End Using
        Return False
    End Function

    Private Function ExtractManagerQrToken(rawInput As String) As String
        If String.IsNullOrWhiteSpace(rawInput) Then Return ""

        Dim match = Regex.Match(rawInput, "User-\d{5}", RegexOptions.IgnoreCase)
        If match.Success Then
            Return match.Value
        End If

        If rawInput.StartsWith("User-", StringComparison.OrdinalIgnoreCase) Then
            Return rawInput.Trim()
        End If

        Return ""
    End Function

    Public Function IsElevatedRole(role As String) As Boolean
        Return Rbac.IsElevated(role)
    End Function

    Private Function VerifyStoredPassword(inputPassword As String, storedPasswordHash As String) As Boolean
        If String.IsNullOrWhiteSpace(storedPasswordHash) Then Return False
        If storedPasswordHash.StartsWith("$2a$") OrElse storedPasswordHash.StartsWith("$2b$") OrElse storedPasswordHash.StartsWith("$2y$") Then
            Return BCrypt.Net.BCrypt.Verify(inputPassword, storedPasswordHash)
        End If
        Return String.Equals(ComputeSha256Base64(inputPassword), storedPasswordHash, StringComparison.Ordinal)
    End Function

    Private Function ComputeSha256Base64(value As String) As String
        Using sha As System.Security.Cryptography.SHA256 = System.Security.Cryptography.SHA256.Create()
            Dim bytes = System.Text.Encoding.UTF8.GetBytes(value)
            Return Convert.ToBase64String(sha.ComputeHash(bytes))
        End Using
    End Function
End Module
