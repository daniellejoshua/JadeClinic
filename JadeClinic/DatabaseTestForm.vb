Imports Microsoft.Data.Sqlite
Imports System.Data.Common

Public Class DatabaseTestForm
    Inherits Form

    Private Sub DatabaseTestForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Text = "Database Connection Test"
        Me.Size = New Size(600, 400)
        Me.StartPosition = FormStartPosition.CenterScreen

        ' Create test button
        Dim btnTest As New Button()
        btnTest.Text = "Test Database Connection"
        btnTest.Size = New Size(200, 50)
        btnTest.Location = New Point(200, 50)
        AddHandler btnTest.Click, AddressOf TestDatabaseConnection

        ' Create result textbox
        Dim txtResults As New TextBox()
        txtResults.Multiline = True
        txtResults.ScrollBars = ScrollBars.Vertical
        txtResults.Size = New Size(550, 250)
        txtResults.Location = New Point(25, 120)
        txtResults.Font = New Font("Consolas", 9)

        Me.Controls.AddRange({btnTest, txtResults})
        Me.Tag = txtResults ' Store reference for later use
    End Sub

    Private Sub TestDatabaseConnection(sender As Object, e As EventArgs)
        Dim txtResults As TextBox = CType(Me.Tag, TextBox)
        txtResults.Clear()

        Try
            txtResults.AppendText("=== Database Connection Test ===" & vbCrLf)
            txtResults.AppendText($"Time: {DateTime.Now}" & vbCrLf)
            txtResults.AppendText(vbCrLf)

            ' Test 1: Connection String
            txtResults.AppendText("1. Testing Connection String..." & vbCrLf)
            Dim connStr As String = Connection.GetConnectionString()
            txtResults.AppendText($"   Connection String: {connStr}" & vbCrLf)
            txtResults.AppendText(vbCrLf)

            ' Test 2: Basic Connection
            txtResults.AppendText("2. Testing Basic Connection..." & vbCrLf)
            If Connection.TestConnection() Then
                txtResults.AppendText("   ? Connection successful!" & vbCrLf)
            Else
                txtResults.AppendText("   ? Connection failed!" & vbCrLf)
            End If
            txtResults.AppendText(vbCrLf)

            ' Test 3: Database Info
            txtResults.AppendText("3. Database Information..." & vbCrLf)
            txtResults.AppendText($"   {Connection.GetDatabaseInfo()}" & vbCrLf)
            txtResults.AppendText(vbCrLf)

            ' Test 4: Initialize Database
            txtResults.AppendText("4. Testing Database Initialization..." & vbCrLf)
            If Connection.InitializeDatabase() Then
                txtResults.AppendText("   ? Database initialization successful!" & vbCrLf)
            Else
                txtResults.AppendText("   ? Database initialization failed!" & vbCrLf)
            End If
            txtResults.AppendText(vbCrLf)

            ' Test 5: Check Tables
            txtResults.AppendText("5. Checking Database Tables..." & vbCrLf)
            Try
                Using conn As New SqliteConnection(Connection.GetConnectionString())
                    conn.Open()

                    Dim query As String = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME"
                    Using cmd As New SqliteCommand(query, conn)
                        Using reader As DbDataReader = cmd.ExecuteReader()
                            Dim tableCount As Integer = 0
                            While reader.Read()
                                tableCount += 1
                                txtResults.AppendText($"   ? {reader("TABLE_NAME")}" & vbCrLf)
                            End While

                            If tableCount = 0 Then
                                txtResults.AppendText("   ?? No tables found - database might need initialization" & vbCrLf)
                            Else
                                txtResults.AppendText($"   ? Found {tableCount} tables" & vbCrLf)
                            End If
                        End Using
                    End Using
                End Using
            Catch ex As Exception
                txtResults.AppendText($"   ? Error checking tables: {ex.Message}" & vbCrLf)
            End Try

            txtResults.AppendText(vbCrLf)
            txtResults.AppendText("=== Test Complete ===" & vbCrLf)

        Catch ex As Exception
            txtResults.AppendText($"? Test failed with error: {ex.Message}" & vbCrLf)
            txtResults.AppendText($"Stack trace: {ex.StackTrace}" & vbCrLf)
        End Try

        txtResults.SelectionStart = 0
        txtResults.ScrollToCaret()
    End Sub
End Class
