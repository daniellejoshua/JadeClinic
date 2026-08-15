' Order Summary Grid
' ==================
' A fixed, non-resizable DataGridView that replaces the hand-built Guna2Panel
' rows inside orderSummaryPanel. Columns: # | Product | Qty | Amount.
'
' Palette (user spec):
'   Table bg        #FFFFFF
'   Header bg       #FBF7EC  (very light warm gold)
'   Header text     #222222
'   Row bg          #FFFFFF
'   Row divider     #E8E8E8
'   Product text    #333333
'   Qty / # text    #666666
'   Selected/hover  #FBF7EC
'   Amount text     #BE9A30

Imports System.Collections.Generic
Imports System.Drawing
Imports System.Windows.Forms

Public Module OrderSummaryGridBuilder

    Public Const GridHeaderHeight As Integer = 40
    Public Const GridRowHeight As Integer = 48

    Private ReadOnly GridHeaderBg As Color = Color.FromArgb(251, 247, 236)     ' #FBF7EC
    Private ReadOnly GridHeaderText As Color = Color.FromArgb(34, 34, 34)      ' #222222
    Private ReadOnly GridRowBg As Color = Color.FromArgb(255, 255, 255)        ' #FFFFFF
    Private ReadOnly GridDivider As Color = Color.FromArgb(232, 232, 232)      ' #E8E8E8
    Private ReadOnly GridProductText As Color = Color.FromArgb(51, 51, 51)     ' #333333
    Private ReadOnly GridMutedText As Color = Color.FromArgb(102, 102, 102)    ' #666666
    Private ReadOnly GridHighlightText As Color = Color.FromArgb(190, 154, 48) ' #BE9A30

    ' One pre-formatted display row handed to PopulateGrid.
    Public Class OrderSummaryRowInfo
        Public Number As String
        Public DisplayName As String
        Public FullName As String
        Public Qty As String
        Public LineTotal As String
        Public AmountTooltip As String
    End Class

    ' Configure the grid once. All sizing/layout is locked down so the user
    ' can never resize columns, rows, or the header.
    Public Function BuildGrid() As DataGridView
        Dim grid As New DataGridView()

        ' Global layout - fixed
        grid.Dock = DockStyle.Fill
        grid.RowHeadersVisible = False
        grid.AllowUserToAddRows = False
        grid.AllowUserToDeleteRows = False
        grid.AllowUserToResizeColumns = False
        grid.AllowUserToResizeRows = False
        grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        grid.RowTemplate.Resizable = DataGridViewTriState.False
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
        grid.ColumnHeadersHeight = GridHeaderHeight
        grid.RowTemplate.Height = GridRowHeight

        ' Behavior
        grid.ReadOnly = True
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        grid.MultiSelect = False
        grid.ScrollBars = ScrollBars.Vertical
        grid.EnableHeadersVisualStyles = False

        ' Painting
        grid.BackgroundColor = GridRowBg
        grid.BorderStyle = BorderStyle.None
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        grid.GridColor = GridDivider

        ' Cell style (rows)
        grid.DefaultCellStyle.BackColor = GridRowBg
        grid.DefaultCellStyle.ForeColor = GridMutedText
        grid.DefaultCellStyle.SelectionBackColor = GridHeaderBg
        grid.DefaultCellStyle.Font = New Font(ResolveFontFamily({"Poppins", "Segoe UI"}), 9.0F, FontStyle.Regular)
        grid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter
        grid.DefaultCellStyle.Padding = New Padding(5, 2, 5, 2)
        grid.DefaultCellStyle.NullValue = ""

        ' Header style
        grid.ColumnHeadersDefaultCellStyle.BackColor = GridHeaderBg
        grid.ColumnHeadersDefaultCellStyle.ForeColor = GridHeaderText
        grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = GridHeaderBg
        grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = GridHeaderText
        grid.ColumnHeadersDefaultCellStyle.Font = New Font(ResolveFontFamily({"Poppins SemiBold", "Poppins", "Segoe UI"}), 10.0F, FontStyle.Regular)
        grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter

        ' # column
        Dim colNo As New DataGridViewTextBoxColumn()
        colNo.Name = "colNo"
        colNo.HeaderText = "#"
        colNo.Width = 50
        colNo.SortMode = DataGridViewColumnSortMode.NotSortable
        colNo.Resizable = DataGridViewTriState.False
        colNo.DefaultCellStyle.ForeColor = GridMutedText
        colNo.DefaultCellStyle.SelectionForeColor = GridMutedText
        grid.Columns.Add(colNo)

        ' Product column (absorbs remaining width)
        Dim colProduct As New DataGridViewTextBoxColumn()
        colProduct.Name = "colProduct"
        colProduct.HeaderText = "Product"
        colProduct.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        colProduct.SortMode = DataGridViewColumnSortMode.NotSortable
        colProduct.Resizable = DataGridViewTriState.False
        colProduct.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft
        colProduct.DefaultCellStyle.ForeColor = GridProductText
        colProduct.DefaultCellStyle.SelectionForeColor = GridProductText
        grid.Columns.Add(colProduct)

        ' Qty column
        Dim colQty As New DataGridViewTextBoxColumn()
        colQty.Name = "colQty"
        colQty.HeaderText = "Qty"
        colQty.Width = 70
        colQty.SortMode = DataGridViewColumnSortMode.NotSortable
        colQty.Resizable = DataGridViewTriState.False
        colQty.DefaultCellStyle.ForeColor = GridMutedText
        colQty.DefaultCellStyle.SelectionForeColor = GridMutedText
        grid.Columns.Add(colQty)

        ' Amount column
        Dim colAmount As New DataGridViewTextBoxColumn()
        colAmount.Name = "colAmount"
        colAmount.HeaderText = "Amount"
        colAmount.Width = 110
        colAmount.SortMode = DataGridViewColumnSortMode.NotSortable
        colAmount.Resizable = DataGridViewTriState.False
        colAmount.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight
        colAmount.DefaultCellStyle.ForeColor = GridHighlightText
        colAmount.DefaultCellStyle.SelectionForeColor = GridHighlightText
        colAmount.DefaultCellStyle.Font = New Font(ResolveFontFamily({"Poppins SemiBold", "Poppins", "Segoe UI"}), 9.0F, FontStyle.Regular)
        grid.Columns.Add(colAmount)

        Return grid
    End Function

    ' Replace the grid contents with the given display rows. Rows are added in
    ' order, so the DataGridView row index always matches the currentOrderList
    ' index (that is what ReduceItemQuantity expects).
    Public Sub PopulateGrid(grid As DataGridView, rows As List(Of OrderSummaryRowInfo))
        If grid Is Nothing Then Return
        grid.Rows.Clear()
        If rows Is Nothing Then Return

        For Each r In rows
            Dim rowIndex As Integer = grid.Rows.Add()
            Dim row = grid.Rows(rowIndex)
            row.Cells(0).Value = r.Number
            row.Cells(1).Value = r.DisplayName
            row.Cells(2).Value = r.Qty
            row.Cells(3).Value = r.LineTotal
            If Not String.IsNullOrEmpty(r.FullName) AndAlso r.FullName <> r.DisplayName Then
                row.Cells(1).ToolTipText = r.FullName
            End If
            If Not String.IsNullOrEmpty(r.AmountTooltip) Then
                row.Cells(3).ToolTipText = r.AmountTooltip
            End If
        Next

        ' No row should appear selected until the user actually hovers/selects one
        grid.ClearSelection()
    End Sub

    Private Function ResolveFontFamily(priorityNames As String()) As String
        Try
            Dim installed As New HashSet(Of String)()
            For Each family As FontFamily In System.Drawing.FontFamily.Families
                installed.Add(family.Name)
            Next
            For Each name As String In priorityNames
                If installed.Contains(name) Then Return name
            Next
        Catch
        End Try
        Return "Segoe UI"
    End Function
End Module
