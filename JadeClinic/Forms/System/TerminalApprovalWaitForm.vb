Imports System.Drawing
Imports System.Windows.Forms

' Shown on a computer that is trying to open the app while it is still
' waiting for the administrator's approval. It polls the shared Terminals
' table every 15 seconds and closes automatically once the PC is approved.
' Returns DialogResult.OK when access is granted, anything else on exit.
Public Class TerminalApprovalWaitForm
    Inherits Form

    Private WithEvents _pollTimer As New Timer() With {.Interval = 15000}
    Private _lblStatus As Label
    Private _btnCheck As Button
    Private _closedByPoll As Boolean = False

    Public Sub New()
        BuildUi()
    End Sub

    Private Sub BuildUi()
        Me.Text = "Access Approval Required"
        Me.Size = New Size(620, 430)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ControlBox = False
        Me.BackColor = Color.White
        Me.ShowInTaskbar = False

        Dim marginL As Integer = 60

        Dim lblIcon As New Label() With {
            .Text = "🔐",
            .Font = New Font("Segoe UI", 30, FontStyle.Regular),
            .AutoSize = True
        }
        Dim lblTitle As New Label() With {
            .Text = "Access Approval Required",
            .Font = New Font("Segoe UI", 17, FontStyle.Bold),
            .ForeColor = Color.FromArgb(254, 191, 16),
            .AutoSize = True,
            .BackColor = Color.Transparent
        }
        Me.Controls.Add(lblIcon)
        Me.Controls.Add(lblTitle)
        AddHandler Me.Load, Sub()
                                lblIcon.Location = New Point((Me.ClientSize.Width - lblIcon.Width) \ 2, 28)
                                lblTitle.Location = New Point((Me.ClientSize.Width - lblTitle.Width) \ 2, 90)
                            End Sub

        Dim machineName As String = Environment.MachineName
        Dim lblInstruction As New Label() With {
            .Text = "This computer (" & machineName & ") is not yet approved to use the clinic system." & vbCrLf &
                    "The administrator will confirm access from their computer." & vbCrLf &
                    "This window will continue automatically once the PC is approved.",
            .Font = New Font("Segoe UI", 11, FontStyle.Regular),
            .ForeColor = Color.FromArgb(51, 51, 51),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(500, 90),
            .Location = New Point(marginL, 135),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(lblInstruction)

        _lblStatus = New Label() With {
            .Text = "Checking with the administrator...",
            .Font = New Font("Segoe UI", 10, FontStyle.Regular),
            .ForeColor = Color.FromArgb(120, 120, 120),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(500, 30),
            .Location = New Point(marginL, 235),
            .TextAlign = ContentAlignment.MiddleCenter
        }
        Me.Controls.Add(_lblStatus)

        _btnCheck = New Button() With {
            .Text = "Check Again",
            .Font = New Font("Segoe UI", 11, FontStyle.Bold),
            .BackColor = Color.FromArgb(254, 191, 16),
            .ForeColor = Color.Black,
            .FlatStyle = FlatStyle.Flat,
            .Size = New Size(150, 42),
            .Cursor = Cursors.Hand
        }
        _btnCheck.FlatAppearance.BorderSize = 0
        _btnCheck.Location = New Point((Me.ClientSize.Width - 330) \ 2, 300)
        AddHandler _btnCheck.Click, Sub() CheckAccessNow()
        Me.Controls.Add(_btnCheck)

        Dim btnExit As New Button() With {
            .Text = "Exit",
            .Font = New Font("Segoe UI", 11, FontStyle.Regular),
            .BackColor = Color.FromArgb(255, 71, 87),
            .ForeColor = Color.White,
            .FlatStyle = FlatStyle.Flat,
            .Size = New Size(150, 42),
            .Cursor = Cursors.Hand
        }
        btnExit.FlatAppearance.BorderSize = 0
        btnExit.Location = New Point(_btnCheck.Right + 30, 300)
        AddHandler btnExit.Click, Sub() Me.Close()
        Me.Controls.Add(btnExit)

        Utilities.EnableEscCloseModal(Me)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        _pollTimer.Start()
        CheckAccessNow()
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        _pollTimer.Stop()
        MyBase.OnFormClosing(e)
    End Sub

    Private Sub _pollTimer_Tick(sender As Object, e As EventArgs) Handles _pollTimer.Tick
        CheckAccessNow()
    End Sub

    Private Sub CheckAccessNow()
        Try
            Select Case TerminalManager.GetTerminalAccessStatus()
                Case TerminalManager.TerminalAccessStatus.Allowed
                    _pollTimer.Stop()
                    _closedByPoll = True
                    Me.DialogResult = DialogResult.OK
                    Me.Close()
                Case TerminalManager.TerminalAccessStatus.Blocked
                    _pollTimer.Stop()
                    _closedByPoll = True
                    MessageBox.Show(
                        "This computer has been blocked by the administrator." & vbCrLf & vbCrLf &
                        "Contact your administrator to enable access for this PC in the PC Terminals page.",
                        "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Me.DialogResult = DialogResult.Cancel
                    Me.Close()
                Case Else
                    ' Still pending - keep the last-seen time fresh so the admin
                    ' page shows this PC as online while it is waiting.
                    TerminalManager.RegisterCurrentTerminal()
                    _lblStatus.Text = "Last checked at " & DateTime.Now.ToString("HH:mm:ss") &
                                      " - still waiting for the administrator..."
            End Select
        Catch ex As Exception
            Console.WriteLine($"TerminalApprovalWaitForm.CheckAccessNow error: {ex.Message}")
        End Try
    End Sub

End Class