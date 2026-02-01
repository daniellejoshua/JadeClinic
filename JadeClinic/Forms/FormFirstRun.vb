Imports Microsoft.Data.SqlClient
Imports System.Configuration

Public Class FormFirstRun
    Private Sub FormFirstRun_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Set default selection
        rbLocal.Checked = True
        txtServer.Enabled = False
        btnSave.Enabled = False

        ' Show computer name
        lblComputerName.Text = $"Computer: {Environment.MachineName}"
        txtServer.Text = Environment.MachineName
    End Sub

    Private Sub rbNetwork_CheckedChanged(sender As Object, e As EventArgs) Handles rbNetwork.CheckedChanged
        txtServer.Enabled = rbNetwork.Checked
    End Sub

    Private Sub btnTest_Click(sender As Object, e As EventArgs) Handles btnTest.Click
        Dim server As String = GetSelectedServer()
        TestDatabaseConnection(server)
    End Sub

    Private Function GetSelectedServer() As String
        If rbLocal.Checked Then
            Return "localhost"
        Else
            Return If(String.IsNullOrEmpty(txtServer.Text), "localhost", txtServer.Text)
        End If
    End Function

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Dim server As String = GetSelectedServer()
        Connection.SaveConnectionString(server)

        MessageBox.Show($"Configuration saved!{vbCrLf}Server: {server}",
                      "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub TestDatabaseConnection(server As String)
        Try
            Dim connStr As String = Connection.BuildConnectionString(server)  ' Uses the function from Connection module

            Using conn As New SqlConnection(connStr)
                conn.Open()

                Using cmd As New SqlCommand("SELECT @@SERVERNAME, DB_NAME()", conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        If reader.Read() Then
                            lblStatus.Text = $"✅ Connected!{vbCrLf}" &
                                           $"Server: {reader.GetString(0)}{vbCrLf}" &
                                           $"Database: {reader.GetString(1)}"
                            lblStatus.ForeColor = Color.Green
                            btnSave.Enabled = True
                        End If
                    End Using
                End Using
            End Using

        Catch ex As Exception
            lblStatus.Text = $"❌ Error: {ex.Message}"
            lblStatus.ForeColor = Color.Red
            btnSave.Enabled = False
        End Try
    End Sub

End Class