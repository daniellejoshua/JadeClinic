Imports System.Drawing
Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms

' Shown by the freshly downloaded build (launched with --apply-update) while it
' swaps the old files for the new ones and relaunches the app. This is the
' "install" phase that used to be a hidden PowerShell script with no UI, which
' looked like the app had hung. It is deliberately non-dismissable.
Public Class ApplyingUpdateForm
    Inherits Form

    Private ReadOnly _updateDir As String
    Private ReadOnly _appDir As String
    Private ReadOnly _oldPid As Integer

    Private _bar As ProgressBar
    Private _lblStatus As Label
    Private _allowClose As Boolean = False

    Public Sub New(updateDir As String, appDir As String, oldPid As Integer)
        _updateDir = updateDir
        _appDir = appDir
        _oldPid = oldPid
        BuildUi()
    End Sub

    Private Sub BuildUi()
        Me.Text = "Installing JadeClinic Update"
        Me.Size = New Size(420, 190)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ControlBox = False
        Me.ShowInTaskbar = True
        Me.TopMost = True
        Me.BackColor = Color.White

        Dim lblTitle As New Label() With {
            .Text = "Installing update",
            .Font = New Font("Segoe UI", 15, FontStyle.Bold),
            .ForeColor = Color.FromArgb(190, 154, 48),
            .BackColor = Color.Transparent,
            .AutoSize = True,
            .Location = New Point(28, 22)
        }
        Me.Controls.Add(lblTitle)

        _bar = New ProgressBar() With {
            .Style = ProgressBarStyle.Marquee,
            .MarqueeAnimationSpeed = 45,
            .Size = New Size(364, 20),
            .Location = New Point(28, 70)
        }
        Me.Controls.Add(_bar)

        _lblStatus = New Label() With {
            .Text = "Starting...",
            .Font = New Font("Segoe UI", 9.5F, FontStyle.Regular),
            .ForeColor = Color.FromArgb(90, 90, 90),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(364, 26),
            .Location = New Point(28, 98),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        Me.Controls.Add(_lblStatus)

        Dim lblNote As New Label() With {
            .Text = "Please wait. JadeClinic will restart automatically. Do not close this window.",
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Italic),
            .ForeColor = Color.FromArgb(150, 150, 150),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(364, 34),
            .Location = New Point(28, 128),
            .TextAlign = ContentAlignment.TopLeft
        }
        Me.Controls.Add(lblNote)
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If Not _allowClose Then
            e.Cancel = True
        End If
        MyBase.OnFormClosing(e)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Task.Run(Sub() DoInstall())
    End Sub

    Private Sub DoInstall()
        Try
            AutoUpdater.InstallUpdate(_updateDir, _appDir, _oldPid,
                Sub(status) SetStatus(status))
            _allowClose = True
            CloseSafe()
        Catch ex As Exception
            _allowClose = True
            CloseSafe(Sub()
                          MessageBox.Show(
                              "The update could not be installed: " & ex.Message & vbCrLf & vbCrLf &
                              "JadeClinic will stay on the current version. Please reopen it and try again.",
                              "Update Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                      End Sub)
        End Try
    End Sub

    Private Sub SetStatus(text As String)
        If Me.IsDisposed OrElse Not Me.IsHandleCreated Then Return
        Try
            Me.BeginInvoke(Sub()
                               If Not Me.IsDisposed Then _lblStatus.Text = text
                           End Sub)
        Catch
        End Try
    End Sub

    Private Sub CloseSafe(Optional afterClose As Action = Nothing)
        If Me.IsDisposed OrElse Not Me.IsHandleCreated Then Return
        Try
            Me.BeginInvoke(Sub()
                               If Not Me.IsDisposed Then
                                   Me.Close()
                                   If afterClose IsNot Nothing Then afterClose()
                               End If
                           End Sub)
        Catch
        End Try
    End Sub
End Class
