Imports Microsoft.Data.SqlClient
Imports System.Configuration

Public Class FormFirstRun
    Private Sub FormFirstRun_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' For standalone deployment, simplify the interface
        ' Hide network options since this is now a standalone app
        rbNetwork.Visible = False
        txtServer.Visible = False

        ' Always select local and disable the option
        rbLocal.Checked = True
        rbLocal.Text = "Standalone Database (LocalDB)"
        rbLocal.Enabled = False

        ' Update labels
        lblComputerName.Text = $"Computer: {Environment.MachineName}"
        lblStatus.Text = "Ready to initialize standalone database"
        lblStatus.ForeColor = Color.Blue

        btnSave.Enabled = True
        btnTest.Text = "Test Database"
        btnSave.Text = "Initialize Database"
    End Sub

    Private Sub btnTest_Click(sender As Object, e As EventArgs) Handles btnTest.Click
        TestDatabaseConnection()
    End Sub

    Private Sub btnSave_Click(sender As Object, e As EventArgs) Handles btnSave.Click
        Try
            ' Initialize the database
            If Connection.InitializeDatabase() Then
                Connection.SaveConnectionString()

                MessageBox.Show($"Standalone database initialized successfully!{vbCrLf}" &
                              $"Location: {Application.StartupPath}\App_Data{vbCrLf}" &
                              $"Database: JadeDentalSupply.mdf",
                              "Success", MessageBoxButtons.OK, MessageBoxIcon.Information)

                Me.DialogResult = DialogResult.OK
                Me.Close()
            Else
                MessageBox.Show("Failed to initialize database. Please check the error details.",
                              "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If

        Catch ex As Exception
            MessageBox.Show($"Error initializing database: {ex.Message}",
                          "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub btnCancel_Click(sender As Object, e As EventArgs) Handles btnCancel.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub TestDatabaseConnection()
        Try
            If Connection.TestConnection() Then
                lblStatus.Text = "✅ Database connection successful!"
                lblStatus.ForeColor = Color.Green
                btnSave.Enabled = True
            Else
                lblStatus.Text = "❌ Database connection failed"
                lblStatus.ForeColor = Color.Red
                btnSave.Enabled = False
            End If

        Catch ex As Exception
            lblStatus.Text = $"❌ Error: {ex.Message}"
            lblStatus.ForeColor = Color.Red
            btnSave.Enabled = False
        End Try
    End Sub
End Class