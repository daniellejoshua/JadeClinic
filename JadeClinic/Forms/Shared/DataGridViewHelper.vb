Public Module DataGridViewHelper
    Private noRecordsLabel As Label = Nothing

    ''' <summary>
    ''' Shows a centered "No records found" message in a DataGridView using an overlay label
    ''' This ensures perfect centering regardless of column widths
    ''' </summary>
    Public Sub ShowNoRecordsMessage(dataGrid As Guna.UI2.WinForms.Guna2DataGridView, message As String)
        If dataGrid Is Nothing Then Return

        ' Clear existing rows
        dataGrid.Rows.Clear()

        ' Create or reuse the overlay label
        If noRecordsLabel Is Nothing Then
            noRecordsLabel = New Label() With {
                .AutoSize = True,
                .Font = New Font("Poppins", 10.0F, FontStyle.Italic),
                .ForeColor = Color.LightGray,
                .BackColor = System.Drawing.Color.FromArgb(41, 44, 45),
                .TextAlign = ContentAlignment.MiddleCenter
            }
            noRecordsLabel.Name = "noRecordsLabel"
        End If

        noRecordsLabel.Text = message

        ' Remove from any previous parent
        If noRecordsLabel.Parent IsNot Nothing Then
            noRecordsLabel.Parent.Controls.Remove(noRecordsLabel)
        End If

        ' Position at the center of the DataGridView
        Dim parentControl = dataGrid.Parent
        If parentControl IsNot Nothing Then
            ' Add label to the same parent as the DataGridView
            parentControl.Controls.Add(noRecordsLabel)

            ' Center the label in the DataGridView
            Dim labelWidth = noRecordsLabel.Width
            Dim labelHeight = noRecordsLabel.Height
            Dim gridLeft = dataGrid.Left
            Dim gridTop = dataGrid.Top
            Dim gridWidth = dataGrid.Width
            Dim gridHeight = dataGrid.Height

            noRecordsLabel.Location = New Point(
                gridLeft + (gridWidth \ 2) - (labelWidth \ 2),
                gridTop + (gridHeight \ 2) - (labelHeight \ 2)
            )

            ' Set the label's background to match the DataGridView's background
            noRecordsLabel.BackColor = System.Drawing.Color.FromArgb(41, 44, 45)


            ' Ensure label is above the grid but below any other overlays
            dataGrid.BringToFront()
            noRecordsLabel.BringToFront()
        End If

        ' Clear selection
        dataGrid.ClearSelection()
        If dataGrid.Columns.Count > 0 Then
            dataGrid.CurrentCell = Nothing
        End If
    End Sub

    ''' <summary>
    ''' Hides the "No records found" message
    ''' </summary>
    Public Sub HideNoRecordsMessage()
        If noRecordsLabel IsNot Nothing Then
            Try
                If noRecordsLabel.Parent IsNot Nothing Then
                    noRecordsLabel.Parent.Controls.Remove(noRecordsLabel)
                End If
                noRecordsLabel.Dispose()
            Catch
            End Try
            noRecordsLabel = Nothing
        End If
    End Sub
End Module