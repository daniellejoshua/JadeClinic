Imports System.Drawing
Imports System.Drawing.Printing
Imports System.Collections.Generic

' Preview dialog for the ESC/POS receipt, styled like the Sales Record "eye"
' view (page-style PrintPreviewControl). Renders the exact 58mm/80mm thermal
' layout from the same EscLine list the printer receives, so the preview is
' WYSIWYG. Print sends the raw ESC/POS stream; pressing Enter also prints.
Public Class EscPosPreviewForm
    Inherits Form

    Private ReadOnly _printerName As String
    Private ReadOnly _lines As List(Of EscPosPrinter.EscLine)
    Private ReadOnly _data As ReceiptData
    Private _preview As PrintPreviewControl
    Private _doc As PrintDocument
    Private WithEvents btnPrint As Button
    Private WithEvents btnClose As Button

    Public Sub New(printerName As String, lines As List(Of EscPosPrinter.EscLine), data As ReceiptData)
        _printerName = printerName
        _lines = lines
        _data = data
        InitializeUi()
    End Sub

    Private Sub InitializeUi()
        Me.Text = "Receipt Preview"
        Me.StartPosition = FormStartPosition.CenterParent
        Me.FormBorderStyle = FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.ShowInTaskbar = False
        Me.ClientSize = New Size(380, 660)
        Me.BackColor = Color.FromArgb(26, 29, 31)
        Me.KeyPreview = True

        ' Page preview (same style as the Sales Record eye view)
        _preview = New PrintPreviewControl() With {
            .Dock = DockStyle.Fill,
            .AutoZoom = True,
            .Zoom = 1.0,
            .UseAntiAlias = True
        }
        Me.Controls.Add(_preview)
        _preview.BringToFront()

        Dim bottomPanel As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 60,
            .BackColor = Color.FromArgb(43, 47, 50)
        }
        Me.Controls.Add(bottomPanel)
        bottomPanel.BringToFront()

        btnPrint = New Button()
        btnPrint.Text = "Print"
        btnPrint.Location = New Point(100, 12)
        btnPrint.Size = New Size(110, 38)
        btnPrint.BackColor = Color.FromArgb(254, 191, 16)
        btnPrint.ForeColor = Color.FromArgb(26, 29, 31)
        btnPrint.FlatStyle = FlatStyle.Flat
        btnPrint.Font = New Font("Segoe UI", 10.0F, FontStyle.Bold)
        bottomPanel.Controls.Add(btnPrint)

        btnClose = New Button()
        btnClose.Text = "Close"
        btnClose.Location = New Point(220, 12)
        btnClose.Size = New Size(110, 38)
        btnClose.BackColor = Color.FromArgb(61, 65, 69)
        btnClose.ForeColor = Color.White
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.Font = New Font("Segoe UI", 10.0F)
        bottomPanel.Controls.Add(btnClose)

        Me.AcceptButton = btnPrint
        AddHandler Me.KeyDown, AddressOf OnPreviewFormKeyDown

        ' Build the receipt page the same way as the Sales Record eye view
        ' (300x700 GDI page), so the preview looks identical on both forms.
        Try
            _doc = New PrintDocument()
            _doc.DefaultPageSettings.PaperSize = New PaperSize("Receipt", 300, 700)
            _doc.DefaultPageSettings.Margins = New Margins(10, 10, 10, 10)
            AddHandler _doc.PrintPage, AddressOf RenderReceiptPage
            _preview.Document = _doc
            _preview.InvalidatePreview()
        Catch ex As Exception
            Console.WriteLine($"Receipt preview setup error: {ex.Message}")
        End Try
    End Sub

    ' Draw the receipt page with the shared GDI renderer (the exact same look
    ' as the Sales Record "eye" view).
    Private Sub RenderReceiptPage(sender As Object, e As PrintPageEventArgs)
        Try
            ReceiptRenderer.DrawReceipt(e.Graphics, e.MarginBounds, _data)
        Catch ex As Exception
            e.Graphics.DrawString($"Preview render error: {ex.Message}", New Font("Arial", 10), Brushes.Black, 10, 10)
        End Try
    End Sub

    Private Sub OnPreviewFormKeyDown(sender As Object, e As KeyEventArgs)
        If e.KeyCode = Keys.Enter Then
            e.SuppressKeyPress = True
            btnPrint.PerformClick()
        ElseIf e.KeyCode = Keys.Escape Then
            e.SuppressKeyPress = True
            btnClose.PerformClick()
        End If
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
