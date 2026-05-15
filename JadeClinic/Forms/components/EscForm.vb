Public Class EscForm
    Public Shared Function ConfirmExit(owner As IWin32Window) As DialogResult
        Dim overlay As Form = Nothing
        Try
            Dim ownerForm = TryCast(owner, Form)
            If ownerForm IsNot Nothing Then
                overlay = New Form() With {
                    .FormBorderStyle = FormBorderStyle.None,
                    .ShowInTaskbar = False,
                    .StartPosition = FormStartPosition.Manual,
                    .BackColor = Color.Black,
                    .Opacity = 0.55,
                    .TopMost = True
                }
                overlay.Bounds = ownerForm.Bounds
                overlay.Owner = ownerForm
                overlay.Show()
            End If

            Using dialog As New EscForm()
                dialog.StartPosition = FormStartPosition.CenterScreen
                dialog.TopMost = True
                Return dialog.ShowDialog(If(overlay, owner))
            End Using
        Finally
            If overlay IsNot Nothing Then
                overlay.Close()
                overlay.Dispose()
            End If
        End Try
    End Function

    Private Sub EscForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        btnCancel.Focus()
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