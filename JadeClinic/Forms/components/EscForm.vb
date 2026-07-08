Public Class EscForm
    Public Shared Function ConfirmExit(owner As IWin32Window) As DialogResult
        Dim overlay As Form = Nothing
        Try
            Dim ownerForm = TryCast(owner, Form)
            ' If the owner form is disabled (another modal dialog is open) do not show the Esc dialog
            If ownerForm IsNot Nothing AndAlso Not ownerForm.Enabled Then
                Return DialogResult.No
            End If

            If ownerForm IsNot Nothing Then
                overlay = New Form() With {
                    .FormBorderStyle = FormBorderStyle.None,
                    .ShowInTaskbar = False,
                    .StartPosition = FormStartPosition.Manual,
                    .BackColor = Color.Black,
                    .Opacity = 0.55,
                    .TopMost = True,
                    .Enabled = False
                }
                overlay.Bounds = ownerForm.Bounds
                overlay.Owner = ownerForm
                overlay.Show()
                overlay.BringToFront()
            End If

            Using dialog As New EscForm()
                dialog.StartPosition = FormStartPosition.CenterScreen
                dialog.TopMost = True
                dialog.KeyPreview = True
                ' Ensure dialog receives focus immediately; pass the real owner form (not the overlay) so modality works correctly
                Dim ownerWindow As IWin32Window = If(ownerForm IsNot Nothing, CType(ownerForm, IWin32Window), owner)
                Dim result As DialogResult = dialog.ShowDialog(ownerWindow)
                Return result
            End Using
        Finally
            If overlay IsNot Nothing Then
                Try
                    overlay.Close()
                    overlay.Dispose()
                Catch
                End Try
            End If
        End Try
    End Function

    Private Sub EscForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.KeyPreview = True
        btnCancel.Focus()
        ApplyRoundedRegion()
    End Sub

    Private Sub ApplyRoundedRegion()
        Dim radius As Integer = 18
        Dim path As New Drawing2D.GraphicsPath()
        path.AddArc(0, 0, radius, radius, 180, 90)
        path.AddArc(Me.Width - radius, 0, radius, radius, 270, 90)
        path.AddArc(Me.Width - radius, Me.Height - radius, radius, radius, 0, 90)
        path.AddArc(0, Me.Height - radius, radius, radius, 90, 90)
        path.CloseAllFigures()
        Me.Region = New Region(path)
    End Sub

    Private Sub btnExit_Click(sender As Object, e As EventArgs) Handles btnExit.Click
        DialogResult = DialogResult.Yes
        Close()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        DialogResult = DialogResult.No
        Close()
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        DialogResult = DialogResult.No
        Close()
    End Sub

    Private Sub EscForm_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyCode = Keys.Escape Then
            btnCancel.PerformClick()
        ElseIf e.KeyCode = Keys.Enter Then
            btnExit.PerformClick()
        End If
    End Sub
End Class