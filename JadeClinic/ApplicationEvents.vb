Imports Microsoft.VisualBasic.ApplicationServices

Namespace My
    Partial Friend Class MyApplication
        Private Sub MyApplication_Startup(sender As Object, e As StartupEventArgs) Handles Me.Startup
            ' Simply show loginform
            Dim loginForm As New frmLoginvb()
            loginForm.Show()
        End Sub
    End Class
End Namespace