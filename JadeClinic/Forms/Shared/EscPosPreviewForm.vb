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
    Private _scrollPanel As Panel
    Private _preview As PictureBox
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
        Me.BackColor = Color.White
        Me.KeyPreview = True

        ' Scrollable receipt preview: the receipt is rendered onto a bitmap at
        ' its full natural height, and shown in a scrollable picture. The layout
        ' stays fixed and nothing is ever cut off, even with many line items.
        _scrollPanel = New Panel() With {
            .Dock = DockStyle.Fill,
            .AutoScroll = True,
            .BackColor = Color.White
        }
        Me.Controls.Add(_scrollPanel)
        _scrollPanel.BringToFront()

        _preview = New PictureBox() With {
            .SizeMode = PictureBoxSizeMode.Normal,
            .Location = New Point(0, 0),
            .BackColor = Color.White
        }
        _scrollPanel.Controls.Add(_preview)
        AddHandler _scrollPanel.Resize, AddressOf RebuildReceipt

        Dim bottomPanel As New Panel() With {
            .Dock = DockStyle.Bottom,
            .Height = 60,
            .BackColor = Color.FromArgb(249, 249, 249)
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
        btnClose.BackColor = Color.FromArgb(240, 240, 240)
        btnClose.ForeColor = Color.FromArgb(51, 51, 51)
        btnClose.FlatStyle = FlatStyle.Flat
        btnClose.Font = New Font("Segoe UI", 10.0F)
        bottomPanel.Controls.Add(btnClose)

        Me.AcceptButton = btnPrint
        AddHandler Me.KeyDown, AddressOf OnPreviewFormKeyDown

        ' Render the receipt at the full width of the window so there is no
        ' empty space, laid out at natural size and scrollable when tall.
        RebuildReceipt(_scrollPanel, EventArgs.Empty)
    End Sub

    Private Sub RebuildReceipt(sender As Object, e As EventArgs)
        If _scrollPanel Is Nothing OrElse _data Is Nothing Then Return
        ' Use the full width of the panel (with a small inset so it still reads
        ' as a paper slip), capping so it never collapses below a sane width.
        Dim paperWidth As Integer = Math.Max(200, _scrollPanel.ClientSize.Width - 8)
        Try
            Dim bmp As Bitmap = ReceiptRenderer.RenderReceiptToBitmap(_data, paperWidth)
            If _preview.Image IsNot Nothing Then _preview.Image.Dispose()
            _preview.Image = bmp
            _preview.Size = bmp.Size
            _preview.Left = 4
            _preview.Top = 4
        Catch ex As Exception
            Console.WriteLine($"Receipt preview error: {ex.Message}")
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
