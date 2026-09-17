Imports System.Drawing
Imports System.IO
Imports System.Threading.Tasks
Imports System.Windows.Forms

' Shown while the app downloads and applies a new version. It is deliberately
' non-dismissable: the update must finish once started. The download runs on a
' background thread and reports progress back to this form before the app
' restarts itself into the new build.
Public Class UpdateProgressForm
    Inherits Form

    Private ReadOnly _remoteVersion As Version
    Private ReadOnly _updatePath As String

    Private _bar As ProgressBar
    Private _lblStatus As Label
    Private _lblHeadline As Label

    Public Sub New(remoteVersion As Version, updatePath As String)
        _remoteVersion = remoteVersion
        _updatePath = updatePath
        BuildUi()
    End Sub

    Private Sub BuildUi()
        Me.Text = "Updating JadeClinic"
        Me.Size = New Size(480, 240)
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ControlBox = False
        Me.ShowInTaskbar = False
        Me.TopMost = True
        Me.BackColor = Color.White

        Dim lblTitle As New Label() With {
            .Text = "Updating JadeClinic",
            .Font = New Font("Segoe UI", 15, FontStyle.Bold),
            .ForeColor = Color.FromArgb(190, 154, 48),
            .BackColor = Color.Transparent,
            .AutoSize = True,
            .Location = New Point(28, 22)
        }
        Me.Controls.Add(lblTitle)

        _lblHeadline = New Label() With {
            .Text = "Installing version " & _remoteVersion.ToString() & " — please wait.",
            .Font = New Font("Segoe UI", 10, FontStyle.Regular),
            .ForeColor = Color.FromArgb(80, 80, 80),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(420, 24),
            .Location = New Point(30, 58)
        }
        Me.Controls.Add(_lblHeadline)

        _bar = New ProgressBar() With {
            .Minimum = 0,
            .Maximum = 100,
            .Value = 0,
            .Style = ProgressBarStyle.Continuous,
            .Size = New Size(420, 20),
            .Location = New Point(30, 96)
        }
        Me.Controls.Add(_bar)

        _lblStatus = New Label() With {
            .Text = "Preparing update...",
            .Font = New Font("Segoe UI", 9, FontStyle.Regular),
            .ForeColor = Color.FromArgb(120, 120, 120),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(420, 40),
            .Location = New Point(30, 124),
            .TextAlign = ContentAlignment.TopLeft
        }
        Me.Controls.Add(_lblStatus)

        Dim lblNote As New Label() With {
            .Text = "The application will restart automatically. Do not close this window.",
            .Font = New Font("Segoe UI", 8.5F, FontStyle.Italic),
            .ForeColor = Color.FromArgb(150, 150, 150),
            .BackColor = Color.Transparent,
            .AutoSize = False,
            .Size = New Size(420, 24),
            .Location = New Point(30, 172),
            .TextAlign = ContentAlignment.MiddleLeft
        }
        Me.Controls.Add(lblNote)
    End Sub

    Protected Overrides Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        Task.Run(Sub() DoUpdate())
    End Sub

    Private Sub DoUpdate()
        Try
            Dim updateDir As String = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "JadeClinic", "Update")
            If Directory.Exists(updateDir) Then Directory.Delete(updateDir, True)

            AutoUpdater.DownloadUpdate(_updatePath, updateDir,
                Sub(pct, text) ReportProgress(pct, text))

            ReportProgress(100, "Restarting to install the update...")
            System.Threading.Thread.Sleep(800)
            Dim appDir As String = AppDomain.CurrentDomain.BaseDirectory
            Me.Invoke(Sub() AutoUpdater.LaunchUpdater(updateDir, appDir))
        Catch ex As Exception
            Me.Invoke(Sub()
                          ReportProgress(_bar.Value, "Update failed: " & ex.Message)
                          MessageBox.Show("Update failed: " & ex.Message, "Update Error",
                                          MessageBoxButtons.OK, MessageBoxIcon.Error)
                          Me.Close()
                      End Sub)
        End Try
    End Sub

    Private Sub ReportProgress(pct As Integer, text As String)
        If Me.IsDisposed OrElse Not Me.IsHandleCreated Then Return
        Me.BeginInvoke(Sub()
                           If Me.IsDisposed Then Return
                           _bar.Value = Math.Max(_bar.Minimum, Math.Min(_bar.Maximum, pct))
                           _lblStatus.Text = text
                       End Sub)
    End Sub
End Class
