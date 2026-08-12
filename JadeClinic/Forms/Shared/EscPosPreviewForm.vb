Imports System.Text
Imports System.Drawing
Imports System.Collections.Generic

' Preview dialog for the ESC/POS receipt. Shows the exact 58mm/80mm thermal
' layout (monospace, 32/42 columns) so what you see is what the printer prints.
' Print sends the raw ESC/POS stream to the thermal printer; Close prints nothing.
Public Class EscPosPreviewForm
    Inherits Form

    Private ReadOnly _printerName As String
    Private ReadOnly _lines As List(Of EscPosPrinter.EscLine)
    Private txtReceipt As TextBox
    Private WithEvents btnPrint As Button
    Private WithEvents btnClose As Button

    Public Sub New(printerName As String, lines As List(Of EscPosPrinter.EscLine))
        _printerName = printerName
        _lines = lines
        InitializeUi()
        RenderReceipt()
    End Sub

    Private Sub InitializeUi()
        Me.Text = "Receipt Preview"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False
        Me.ClientSize = New Size(360, 640)
        Me.BackColor = Color.FromArgb(26, 29, 31)

        txtReceipt = New TextBox()
        txtReceipt.Multiline = True
        txtReceipt.ReadOnly = True
        txtReceipt.ScrollBars = ScrollBars.Vertical
        txtReceipt.WordWrap = False
        txtReceipt.Font = New Font("Courier New", 9.0F)
        txtReceipt.BackColor = Color.White
        txtReceipt.ForeColor = Color.Black
        txtReceipt.Location = New Point(20, 20)
        txtReceipt.Size = New Size(320, 540)
        Me.Controls.Add(txtReceipt)

        btnPrint = New Button()
        btnPrint.Text = "Print"
        btnPrint.Location = New Point(120, 578)
        btnPrint.Size = New Size(100, 38)
        btnPrint.BackColor = Color.FromArgb(254, 191, 16)
        btnPrint.ForeColor = Color.FromArgb(26, 29, 31)
        btnPrint.FlatStyle = FlatStyle.Flat
        btnPrint.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        Me.Controls.Add(btnPrint)

        btnClose = New Button()
        btnClose.Text = "Close"
        btnClose.Location = New Point(230, 578)
        btnClose.Size = New Size(110, 38)
        btnClose.BackColor = Color.FromArgb(61, 65, 69)
        btnClose.ForeColor = Color.White
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.Font = New Font("Segoe UI", 10.0F)
        Me.Controls.Add(btnClose)
    End Sub

    Private Sub RenderReceipt()
        Dim sb As New StringBuilder()
        For Each line As EscPosPrinter.EscLine In _lines
            sb.AppendLine(EscPosPrinter.FormatForDisplay(line))
        Next
        txtReceipt.Text = sb.ToString()
    End Sub

    Private Sub btnPrint_Click(sender As Object, e As EventArgs) Handles btnPrint.Click
        Cursor = Cursors.WaitCursor
        Try
            If EscPosPrinter.PrintReceipt(_printerName, _lines) Then
                Me.DialogResult = DialogResult.OK
                Me.Close()
            Else
                MessageBox.Show("Could not send the receipt to the printer. Check that the printer is connected and is the selected receipt printer.",
                                "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
            End If
        Catch ex As Exception
            MessageBox.Show($"Print failed: {ex.Message}", "Print Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub btnClose_Click(sender As Object, e As EventArgs) Handles btnClose.Click
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

End Class
