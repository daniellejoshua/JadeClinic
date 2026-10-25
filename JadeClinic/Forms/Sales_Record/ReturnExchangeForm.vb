Imports System.Diagnostics
Imports System.Globalization
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks
Imports Npgsql
Imports Guna.UI2.WinForms

' Mutable per-line state shared by the grid and transaction processor.
Public Class ReturnLineState
    Public OriginalSaleItemID As Integer
    Public ProductID As Integer
    Public ProductName As String = ""
    Public ProductCode As String = ""
    Public Category As String = ""
    Public UnitPrice As Decimal
    Public NetUnitPrice As Decimal
    Public NetLineAmount As Decimal
    Public PriorReturnCredit As Decimal
    Public SellingPrice As Decimal
    Public SoldQty As Integer
    Public AlreadyReturned As Integer
    Public Condition As String = "Good"
    Public Disposition As String = "Restock"
    Public ReturnQty As Integer
    Public RefundQty As Integer
    Public ExchangeQty As Integer
    Public ReplacementProductID As Integer
    Public ReplacementProductName As String = ""
    Public ReplacementUnitPrice As Decimal

    Public ReadOnly Property MaxReturnable As Integer
        Get
            Return Math.Max(0, SoldQty - AlreadyReturned)
        End Get
    End Property

    Public ReadOnly Property HasReturn As Boolean
        Get
            Return ReturnQty > 0
        End Get
    End Property
End Class

Friend Class ReplacementProductOption
    Public ProductID As Integer
    Public ProductName As String = ""
    Public ProductCode As String = ""
    Public SellingPrice As Decimal
    Public CurrentStock As Integer

    Public Overrides Function ToString() As String
        Dim code As String = If(String.IsNullOrWhiteSpace(ProductCode), "", ProductCode & " - ")
        Return $"{code}{ProductName} | {ChrW(&H20B1)}{SellingPrice:N2} | Stock {CurrentStock}"
    End Function
End Class

' Lightweight Return / Exchange dialog opened from Sale Details.
'
' The grid uses one action and quantity per item. Mixed refund/exchange
' allocations remain supported through the Split action's allocation editor.
' Database and inventory processing intentionally remain unchanged.
Public Class ReturnExchangeForm
    Inherits Form

    Private Const ActionNone As String = "None"
    Private Const ActionRefund As String = "Refund"
    Private Const ActionExchange As String = "Exchange"
    Private Const DispositionRestock As String = "Restock"
    Private Const DispositionWriteOff As String = "Write Off"
    Private Const DispositionReturnToVendor As String = "Return to Vendor"

    Private ReadOnly _saleId As Integer
    Private ReadOnly _lines As New List(Of ReturnLineState)()
    Private ReadOnly _loadCancellation As New CancellationTokenSource()
    Private ReadOnly _constructionMilliseconds As Long

    Private _saleNumber As String = ""
    Private _customer As String = ""
    Private _loadStarted As Boolean
    Private _loading As Boolean
    Private _processing As Boolean
    Private _completed As Boolean
    Private _suppressGridEvents As Boolean
    Private _userInteracted As Boolean

    Private gridItems As DataGridView
    Private lblSubtitle As Label
    Private lblGridStatus As Label
    Private cmbReason As Guna2ComboBox
    Private txtCustomReason As Guna2TextBox
    Private customReasonRowStyle As RowStyle
    Private txtNotes As Guna2TextBox
    Private lblCharCount As Label
    Private lblItemsCount As Label
    Private lblSettlement As Label
    Private lblValidation As Label
    Private btnProcess As Guna2Button
    Private btnCancel As Guna2Button

    Public Sub New(saleId As Integer)
        Dim timer As Stopwatch = Stopwatch.StartNew()
        _saleId = saleId
        BuildUi()
        timer.Stop()
        _constructionMilliseconds = timer.ElapsedMilliseconds
    End Sub

    Protected Overrides Sub OnLoad(e As EventArgs)
        ' Finalize size and location before the first paint. The previous form
        ' resized in OnShown, which exposed it at the top-left before moving it.
        Dim targetScreen As Screen = If(Owner IsNot Nothing, Screen.FromControl(Owner), Screen.FromControl(Me))
        Dim workArea As Rectangle = targetScreen.WorkingArea
        Size = New Size(Math.Min(Width, workArea.Width - 24), Math.Min(Height, workArea.Height - 24))

        Dim ownerBounds As Rectangle = workArea
        If Owner IsNot Nothing Then
            ownerBounds = Owner.RectangleToScreen(Owner.ClientRectangle)
        End If
        Location = New Point(
            Math.Max(workArea.Left, Math.Min(workArea.Right - Width, ownerBounds.Left + (ownerBounds.Width - Width) \ 2)),
            Math.Max(workArea.Top, Math.Min(workArea.Bottom - Height, ownerBounds.Top + (ownerBounds.Height - Height) \ 2)))
        StartPosition = FormStartPosition.Manual

        MyBase.OnLoad(e)
    End Sub

    Protected Overrides Async Sub OnShown(e As EventArgs)
        MyBase.OnShown(e)
        If _loadStarted Then Return
        _loadStarted = True

        ' Yield once so the centered modal paints before database I/O starts.
        Await Task.Yield()
        Await LoadDataAsync(_loadCancellation.Token)
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        If _processing AndAlso Not _completed Then
            e.Cancel = True
            Return
        End If
        If Not _loadCancellation.IsCancellationRequested Then
            _loadCancellation.Cancel()
        End If
        MyBase.OnFormClosing(e)
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing Then
            _loadCancellation.Dispose()
        End If
        MyBase.Dispose(disposing)
    End Sub

    ' ======================================================================
    ' UI
    ' ======================================================================
    Private Sub BuildUi()
        SuspendLayout()

        Text = "Return / Exchange"
        StartPosition = FormStartPosition.CenterParent
        FormBorderStyle = FormBorderStyle.Sizable
        Size = New Size(1050, 620)
        MinimumSize = New Size(960, 580)
        MaximizeBox = False
        MinimizeBox = False
        ShowInTaskbar = False
        KeyPreview = True
        BackColor = Color.White
        AutoScaleMode = AutoScaleMode.Dpi
        DoubleBuffered = True

        Dim root As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 3,
            .BackColor = Color.White,
            .Margin = New Padding(0),
            .Padding = New Padding(0)
        }
        root.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 60))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 64))
        Controls.Add(root)

        BuildHeader(root)
        BuildMainContent(root)
        BuildFooter(root)

        AddHandler KeyDown,
            Sub(sender, args)
                If args.KeyCode = Keys.Escape AndAlso Not _processing Then
                    DialogResult = DialogResult.Cancel
                    Close()
                End If
            End Sub

        ResumeLayout(True)
    End Sub

    Private Sub BuildHeader(root As TableLayoutPanel)
        Dim header As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Margin = New Padding(0)
        }
        root.Controls.Add(header, 0, 0)

        Dim lblTitle As New Label With {
            .Text = "Return / Exchange",
            .Font = ReturnExchangeTheme.MakeFont(16, FontStyle.Bold),
            .ForeColor = ReturnExchangeTheme.PrimaryText,
            .AutoSize = True,
            .Location = New Point(16, 8)
        }
        header.Controls.Add(lblTitle)

        lblSubtitle = New Label With {
            .Text = "Loading transaction items...",
            .Font = ReturnExchangeTheme.MakeFont(9.5),
            .ForeColor = ReturnExchangeTheme.SecondaryText,
            .AutoSize = True,
            .Location = New Point(18, 35)
        }
        header.Controls.Add(lblSubtitle)

        Dim btnClose As New Button With {
            .Text = "×",
            .Font = New Font("Segoe UI", 14, FontStyle.Regular),
            .FlatStyle = FlatStyle.Flat,
            .ForeColor = ReturnExchangeTheme.SecondaryText,
            .BackColor = Color.White,
            .Dock = DockStyle.Right,
            .Width = 42,
            .TabStop = False,
            .Cursor = Cursors.Hand
        }
        btnClose.FlatAppearance.BorderSize = 0
        AddHandler btnClose.Click,
            Sub()
                If Not _processing Then
                    DialogResult = DialogResult.Cancel
                    Close()
                End If
            End Sub
        header.Controls.Add(btnClose)

        header.Controls.Add(New Panel With {
            .Dock = DockStyle.Bottom,
            .Height = 1,
            .BackColor = ReturnExchangeTheme.BorderColor
        })
    End Sub

    Private Sub BuildMainContent(root As TableLayoutPanel)
        Dim content As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .BackColor = Color.White,
            .Padding = New Padding(12),
            .Margin = New Padding(0)
        }
        content.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 70))
        content.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 30))
        content.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        root.Controls.Add(content, 0, 1)

        Dim gridHost As New Panel With {
            .Dock = DockStyle.Fill,
            .BackColor = Color.White,
            .Margin = New Padding(0, 0, 6, 0),
            .Padding = New Padding(0)
        }
        content.Controls.Add(gridHost, 0, 0)

        gridItems = CreateItemsGrid()
        gridHost.Controls.Add(gridItems)

        lblGridStatus = New Label With {
            .Dock = DockStyle.Fill,
            .Text = "Loading transaction items...",
            .TextAlign = ContentAlignment.MiddleCenter,
            .Font = ReturnExchangeTheme.MakeFont(10),
            .ForeColor = ReturnExchangeTheme.SecondaryText,
            .BackColor = Color.White
        }
        gridHost.Controls.Add(lblGridStatus)
        lblGridStatus.BringToFront()

        Dim rightPanel As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 1,
            .RowCount = 11,
            .BackColor = ReturnExchangeTheme.SecondaryBg,
            .Padding = New Padding(12),
            .Margin = New Padding(6, 0, 0, 0)
        }
        rightPanel.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        rightPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 38))
        customReasonRowStyle = New RowStyle(SizeType.Absolute, 0)
        rightPanel.RowStyles.Add(customReasonRowStyle)
        rightPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightPanel.RowStyles.Add(New RowStyle(SizeType.Absolute, 90))
        rightPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightPanel.RowStyles.Add(New RowStyle(SizeType.AutoSize))
        rightPanel.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        content.Controls.Add(rightPanel, 1, 0)

        Dim lblReason As New Label With {
            .Text = "Return reason",
            .AutoSize = True,
            .Font = ReturnExchangeTheme.MakeFont(9, FontStyle.Bold),
            .ForeColor = ReturnExchangeTheme.PrimaryText,
            .Margin = New Padding(0, 0, 0, 4)
        }
        rightPanel.Controls.Add(lblReason, 0, 0)

        cmbReason = New Guna2ComboBox With {
            .Dock = DockStyle.Fill,
            .DropDownStyle = ComboBoxStyle.DropDownList,
            .BorderRadius = 4,
            .BorderColor = ReturnExchangeTheme.BorderColor,
            .FillColor = Color.White,
            .ForeColor = ReturnExchangeTheme.PrimaryText,
            .Font = ReturnExchangeTheme.MakeFont(9),
            .ItemHeight = 26,
            .Margin = New Padding(0)
        }
        cmbReason.Items.AddRange(New Object() {"Damaged / Defective", "Wrong Item", "Customer Request", "Other"})
        cmbReason.SelectedIndex = -1
        rightPanel.Controls.Add(cmbReason, 0, 1)

        txtCustomReason = New Guna2TextBox With {
            .Dock = DockStyle.Fill,
            .Visible = False,
            .MaxLength = 120,
            .PlaceholderText = "Enter custom return reason",
            .BorderRadius = 4,
            .BorderColor = ReturnExchangeTheme.BorderColor,
            .FillColor = Color.White,
            .ForeColor = ReturnExchangeTheme.PrimaryText,
            .Font = ReturnExchangeTheme.MakeFont(9),
            .Margin = New Padding(0, 6, 0, 0)
        }
        AddHandler txtCustomReason.TextChanged,
            Sub()
                If txtCustomReason.Visible Then
                    _userInteracted = True
                    UpdateValidation(True)
                End If
            End Sub
        rightPanel.Controls.Add(txtCustomReason, 0, 2)

        Dim notesHeader As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .Margin = New Padding(0, 10, 0, 4)
        }
        notesHeader.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        notesHeader.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        notesHeader.Controls.Add(New Label With {
            .Text = "Notes (optional)",
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .Font = ReturnExchangeTheme.MakeFont(9, FontStyle.Bold),
            .ForeColor = ReturnExchangeTheme.PrimaryText
        }, 0, 0)
        lblCharCount = New Label With {
            .Text = "0/300",
            .AutoSize = True,
            .Anchor = AnchorStyles.Right,
            .Font = ReturnExchangeTheme.MakeFont(8),
            .ForeColor = ReturnExchangeTheme.MutedText
        }
        notesHeader.Controls.Add(lblCharCount, 1, 0)
        rightPanel.Controls.Add(notesHeader, 0, 3)

        txtNotes = New Guna2TextBox With {
            .Dock = DockStyle.Fill,
            .Multiline = True,
            .MaxLength = 300,
            .ScrollBars = ScrollBars.Vertical,
            .PlaceholderText = "Additional details",
            .BorderRadius = 4,
            .BorderColor = ReturnExchangeTheme.BorderColor,
            .FillColor = Color.White,
            .ForeColor = ReturnExchangeTheme.PrimaryText,
            .Font = ReturnExchangeTheme.MakeFont(9),
            .Margin = New Padding(0)
        }
        AddHandler txtNotes.TextChanged,
            Sub() lblCharCount.Text = txtNotes.TextLength.ToString(CultureInfo.InvariantCulture) & "/300"
        rightPanel.Controls.Add(txtNotes, 0, 4)

        rightPanel.Controls.Add(New Label With {
            .Text = "Summary",
            .AutoSize = True,
            .Font = ReturnExchangeTheme.MakeFont(10, FontStyle.Bold),
            .ForeColor = ReturnExchangeTheme.PrimaryText,
            .Margin = New Padding(0, 14, 0, 4)
        }, 0, 5)

        lblItemsCount = AddSummaryLine(rightPanel, "Return quantity", 6)
        lblSettlement = New Label With {
            .AutoSize = True,
            .Margin = New Padding(0, 10, 0, 0),
            .Font = ReturnExchangeTheme.MakeFont(10, FontStyle.Bold),
            .ForeColor = ReturnExchangeTheme.PrimaryText
        }
        rightPanel.Controls.Add(lblSettlement, 0, 7)

        rightPanel.Controls.Add(New Label With {
            .Text = "Good returns restock. Damaged -> Write Off. Use Return to Vendor if supplier takes it back.",
            .AutoSize = True,
            .Font = ReturnExchangeTheme.MakeFont(8.2),
            .ForeColor = ReturnExchangeTheme.SecondaryText,
            .Margin = New Padding(0, 12, 0, 0),
            .MaximumSize = New Size(240, 0)
        }, 0, 8)

        AddHandler cmbReason.SelectedIndexChanged, AddressOf Reason_SelectedIndexChanged
    End Sub

    Private Sub Reason_SelectedIndexChanged(sender As Object, e As EventArgs)
        If txtCustomReason Is Nothing OrElse customReasonRowStyle Is Nothing Then Return

        Dim useCustomReason As Boolean = String.Equals(Convert.ToString(cmbReason.SelectedItem), "Other", StringComparison.OrdinalIgnoreCase)
        customReasonRowStyle.Height = If(useCustomReason, 42.0F, 0.0F)
        txtCustomReason.Visible = useCustomReason
        If Not useCustomReason Then txtCustomReason.Clear()

        _userInteracted = True
        UpdateValidation(True)
        If useCustomReason Then txtCustomReason.Focus()
    End Sub

    Private Function CreateItemsGrid() As DataGridView
        Dim grid As New DataGridView With {
            .Dock = DockStyle.Fill,
            .AutoGenerateColumns = False,
            .AllowUserToAddRows = False,
            .AllowUserToDeleteRows = False,
            .AllowUserToResizeRows = False,
            .AllowUserToOrderColumns = False,
            .MultiSelect = False,
            .RowHeadersVisible = False,
            .BackgroundColor = Color.White,
            .BorderStyle = BorderStyle.FixedSingle,
            .CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            .ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single,
            .ColumnHeadersHeight = 38,
            .ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
            .RowTemplate = New DataGridViewRow With {.Height = 42},
            .SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            .EditMode = DataGridViewEditMode.EditOnEnter,
            .EnableHeadersVisualStyles = False,
            .AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
            .AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            .ScrollBars = ScrollBars.Vertical,
            .TabIndex = 0
        }

        grid.ColumnHeadersDefaultCellStyle = New DataGridViewCellStyle With {
            .BackColor = ReturnExchangeTheme.SecondaryBg,
            .ForeColor = ReturnExchangeTheme.HeaderTextColor,
            .Font = ReturnExchangeTheme.MakeFont(8.5, FontStyle.Bold),
            .Alignment = DataGridViewContentAlignment.MiddleLeft,
            .Padding = New Padding(6, 0, 6, 0),
            .SelectionBackColor = ReturnExchangeTheme.SecondaryBg,
            .SelectionForeColor = ReturnExchangeTheme.HeaderTextColor
        }
        grid.DefaultCellStyle = New DataGridViewCellStyle With {
            .BackColor = Color.White,
            .ForeColor = ReturnExchangeTheme.PrimaryText,
            .Font = ReturnExchangeTheme.MakeFont(9),
            .Alignment = DataGridViewContentAlignment.MiddleLeft,
            .Padding = New Padding(6, 0, 6, 0),
            .SelectionBackColor = ReturnExchangeTheme.LightGoldBg,
            .SelectionForeColor = ReturnExchangeTheme.PrimaryText
        }
        grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(253, 253, 252)

        grid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Item",
            .HeaderText = "ITEM / SKU",
            .ReadOnly = True,
            .FillWeight = 32
        })

        Dim actionColumn As New DataGridViewComboBoxColumn With {
            .Name = "Action",
            .HeaderText = "ACTION",
            .FlatStyle = FlatStyle.Flat,
            .DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            .FillWeight = 12
        }
        actionColumn.Items.AddRange(ActionNone, ActionRefund, ActionExchange)
        grid.Columns.Add(actionColumn)

        grid.Columns.Add(New DataGridViewTextBoxColumn With {
            .Name = "Quantity",
            .HeaderText = "QTY",
            .FillWeight = 8,
            .DefaultCellStyle = New DataGridViewCellStyle With {
                .Alignment = DataGridViewContentAlignment.MiddleCenter,
                .NullValue = "0"
            }
        })

        Dim conditionColumn As New DataGridViewComboBoxColumn With {
            .Name = "Condition",
            .HeaderText = "CONDITION",
            .FlatStyle = FlatStyle.Flat,
            .DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            .FillWeight = 12
        }
        conditionColumn.Items.AddRange("Good", "Damaged")
        grid.Columns.Add(conditionColumn)

        Dim dispositionColumn As New DataGridViewComboBoxColumn With {
            .Name = "Disposition",
            .HeaderText = "DISPOSITION",
            .FlatStyle = FlatStyle.Flat,
            .DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            .FillWeight = 16
        }
        dispositionColumn.Items.AddRange(DispositionRestock, DispositionWriteOff, DispositionReturnToVendor)
        grid.Columns.Add(dispositionColumn)

        grid.Columns.Add(New DataGridViewButtonColumn With {
            .Name = "Replacement",
            .HeaderText = "REPLACEMENT",
            .FlatStyle = FlatStyle.Flat,
            .FillWeight = 20,
            .UseColumnTextForButtonValue = False
        })

        AddHandler grid.CurrentCellDirtyStateChanged, AddressOf Grid_CurrentCellDirtyStateChanged
        AddHandler grid.CellValueChanged, AddressOf Grid_CellValueChanged
        AddHandler grid.CellEndEdit, AddressOf Grid_CellEndEdit
        AddHandler grid.CellContentClick, AddressOf Grid_CellContentClick
        AddHandler grid.EditingControlShowing, AddressOf Grid_EditingControlShowing
        AddHandler grid.DataError, Sub(sender, args) args.ThrowException = False

        Return grid
    End Function

    Private Function AddSummaryLine(parent As TableLayoutPanel, caption As String, row As Integer) As Label
        Dim line As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .AutoSize = True,
            .ColumnCount = 2,
            .RowCount = 1,
            .Margin = New Padding(0, 4, 0, 0)
        }
        line.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        line.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        line.Controls.Add(New Label With {
            .Text = caption,
            .AutoSize = True,
            .Anchor = AnchorStyles.Left,
            .Font = ReturnExchangeTheme.MakeFont(9),
            .ForeColor = ReturnExchangeTheme.SecondaryText
        }, 0, 0)

        Dim value As New Label With {
            .Text = If(caption = "Return quantity", "0", FormatPeso(0D)),
            .AutoSize = True,
            .Anchor = AnchorStyles.Right,
            .Font = ReturnExchangeTheme.MakeFont(10, FontStyle.Bold),
            .ForeColor = ReturnExchangeTheme.PrimaryText
        }
        line.Controls.Add(value, 1, 0)
        parent.Controls.Add(line, 0, row)
        Return value
    End Function

    Private Sub BuildFooter(root As TableLayoutPanel)
        Dim footer As New TableLayoutPanel With {
            .Dock = DockStyle.Fill,
            .ColumnCount = 2,
            .RowCount = 1,
            .BackColor = Color.White,
            .Padding = New Padding(12, 8, 12, 8),
            .Margin = New Padding(0)
        }
        footer.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 100))
        footer.ColumnStyles.Add(New ColumnStyle(SizeType.AutoSize))
        root.Controls.Add(footer, 0, 2)

        lblValidation = New Label With {
            .Text = "",
            .AutoEllipsis = True,
            .Dock = DockStyle.Fill,
            .TextAlign = ContentAlignment.MiddleLeft,
            .Font = ReturnExchangeTheme.MakeFont(9),
            .ForeColor = ReturnExchangeTheme.ErrorRed,
            .Visible = False,
            .Margin = New Padding(4, 0, 10, 0)
        }
        footer.Controls.Add(lblValidation, 0, 0)

        Dim buttons As New FlowLayoutPanel With {
            .AutoSize = True,
            .AutoSizeMode = AutoSizeMode.GrowAndShrink,
            .FlowDirection = FlowDirection.LeftToRight,
            .WrapContents = False,
            .Anchor = AnchorStyles.Right,
            .Margin = New Padding(0)
        }
        footer.Controls.Add(buttons, 1, 0)

        btnCancel = New Guna2Button With {
            .Text = "Cancel",
            .Size = New Size(100, 38),
            .BorderRadius = 5,
            .FillColor = ReturnExchangeTheme.NeutralFill,
            .ForeColor = ReturnExchangeTheme.PrimaryText,
            .Font = ReturnExchangeTheme.MakeFont(9.5),
            .Margin = New Padding(0, 0, 8, 0),
            .Cursor = Cursors.Hand
        }
        btnCancel.ShadowDecoration.Enabled = False
        AddHandler btnCancel.Click,
            Sub()
                If Not _processing Then
                    DialogResult = DialogResult.Cancel
                    Close()
                End If
            End Sub
        buttons.Controls.Add(btnCancel)

        btnProcess = New Guna2Button With {
            .Text = "Authorize",
            .Size = New Size(135, 38),
            .BorderRadius = 5,
            .FillColor = ReturnExchangeTheme.PrimaryGold,
            .ForeColor = Color.White,
            .Font = ReturnExchangeTheme.MakeFont(9.5, FontStyle.Bold),
            .Enabled = False,
            .Margin = New Padding(0),
            .Cursor = Cursors.Hand
        }
        btnProcess.HoverState.FillColor = ReturnExchangeTheme.GoldHover
        btnProcess.ShadowDecoration.Enabled = False
        AddHandler btnProcess.Click, AddressOf ProcessReturn
        buttons.Controls.Add(btnProcess)
    End Sub

    ' ======================================================================
    ' Asynchronous data loading
    ' ======================================================================
    Private Async Function LoadDataAsync(cancellationToken As CancellationToken) As Task
        _loading = True
        btnProcess.Enabled = False
        Dim totalTimer As Stopwatch = Stopwatch.StartNew()
        Dim queryTimer As Stopwatch = Stopwatch.StartNew()

        Try
            Dim loadedLines As New List(Of ReturnLineState)()
            Dim saleNumber As String = ""
            Dim customer As String = ""

            Const sql As String =
                "SELECT COALESCE(s.SaleNumber,'') AS SaleNumber, COALESCE(s.CustomerName,'') AS CustomerName, " &
                "si.SaleItemID, si.ProductID, COALESCE(p.ProductName,'Unknown') AS ProductName, " &
                "COALESCE(p.ProductCode,'') AS ProductCode, COALESCE(p.Category,'') AS Category, " &
                "si.Quantity AS SoldQty, si.UnitPrice, COALESCE(si.LineDiscountAmount,0) AS LineDiscountAmount, " &
                "COALESCE(p.SellingPrice, si.UnitPrice) AS SellingPrice, " &
                "COALESCE(ret.ReturnedQty, 0) AS AlreadyReturned, COALESCE(ret.ReturnedCredit, 0) AS PriorReturnCredit " &
                "FROM Sales s " &
                "LEFT JOIN SaleItems si ON si.SaleID = s.SaleID " &
                "LEFT JOIN Products p ON p.ProductID = si.ProductID " &
                "LEFT JOIN (" &
                "  SELECT sri.OriginalSaleItemID, SUM(sri.Quantity) AS ReturnedQty, SUM(sri.ReturnCreditAmount) AS ReturnedCredit " &
                "  FROM SalesReturnItems sri " &
                "  JOIN SalesReturns sr ON sr.ReturnID = sri.ReturnID " &
                "  WHERE sr.SaleID = @SaleID AND sr.Status = 'Completed' AND sri.OriginalSaleItemID IS NOT NULL " &
                "  GROUP BY sri.OriginalSaleItemID" &
                ") ret ON ret.OriginalSaleItemID = si.SaleItemID " &
                "WHERE s.SaleID = @SaleID AND s.Status = 'Completed' " &
                "ORDER BY si.SaleItemID"

            Using conn As New NpgsqlConnection(Connection.GetConnectionString())
                Await conn.OpenAsync(cancellationToken)
                Using cmd As New NpgsqlCommand(sql, conn)
                    cmd.Parameters.AddWithValue("@SaleID", _saleId)
                    Using reader As NpgsqlDataReader = Await cmd.ExecuteReaderAsync(cancellationToken)
                        While Await reader.ReadAsync(cancellationToken)
                            saleNumber = Convert.ToString(reader("SaleNumber"))
                            customer = Convert.ToString(reader("CustomerName"))

                            If reader.IsDBNull(reader.GetOrdinal("SaleItemID")) Then Continue While

                            Dim sold As Integer = Convert.ToInt32(reader("SoldQty"))
                            Dim alreadyReturned As Integer = Convert.ToInt32(reader("AlreadyReturned"))
                            If sold - alreadyReturned <= 0 Then Continue While
                            Dim grossUnitPrice As Decimal = Convert.ToDecimal(reader("UnitPrice"))
                            Dim lineDiscount As Decimal = Convert.ToDecimal(reader("LineDiscountAmount"))
                            Dim netLineAmount As Decimal = Math.Max(0D, (grossUnitPrice * sold) - lineDiscount)
                            Dim netUnitPrice As Decimal = Math.Round(netLineAmount / sold, 2, MidpointRounding.AwayFromZero)
                            Dim sellingPrice As Decimal = Convert.ToDecimal(reader("SellingPrice"))
                            Dim productId As Integer = Convert.ToInt32(reader("ProductID"))
                            Dim productName As String = Convert.ToString(reader("ProductName"))

                            loadedLines.Add(New ReturnLineState With {
                                .OriginalSaleItemID = Convert.ToInt32(reader("SaleItemID")),
                                .ProductID = productId,
                                .ProductName = productName,
                                .ProductCode = Convert.ToString(reader("ProductCode")),
                                .Category = Convert.ToString(reader("Category")),
                                .SoldQty = sold,
                                .AlreadyReturned = alreadyReturned,
                                .UnitPrice = grossUnitPrice,
                                .NetUnitPrice = netUnitPrice,
                                .NetLineAmount = Math.Round(netLineAmount, 2, MidpointRounding.AwayFromZero),
                                .PriorReturnCredit = Convert.ToDecimal(reader("PriorReturnCredit")),
                                .SellingPrice = sellingPrice,
                                .Condition = "Good",
                                .ReturnQty = 0,
                                .RefundQty = 0,
                                .ExchangeQty = 0,
                                .ReplacementProductID = productId,
                                .ReplacementProductName = productName,
                                .ReplacementUnitPrice = sellingPrice
                            })
                        End While
                    End Using
                End Using
            End Using

            queryTimer.Stop()
            cancellationToken.ThrowIfCancellationRequested()

            _saleNumber = If(String.IsNullOrWhiteSpace(saleNumber), _saleId.ToString(CultureInfo.InvariantCulture), saleNumber)
            _customer = customer
            _lines.Clear()
            _lines.AddRange(loadedLines)

            Dim gridTimer As Stopwatch = Stopwatch.StartNew()
            PopulateGrid()
            gridTimer.Stop()

            lblSubtitle.Text = $"Sale #{_saleNumber}" & If(String.IsNullOrWhiteSpace(_customer), "", $"  ·  {_customer}")
            lblGridStatus.Visible = _lines.Count = 0
            If _lines.Count = 0 Then
                lblGridStatus.Text = "This sale has no returnable items."
                lblGridStatus.BringToFront()
            Else
                gridItems.BringToFront()
            End If

            _loading = False
            RefreshSummary()
            UpdateValidation(False)
            totalTimer.Stop()

            Console.WriteLine(
                $"ReturnExchange performance: construct={_constructionMilliseconds}ms; " &
                $"query={queryTimer.ElapsedMilliseconds}ms; grid={gridTimer.ElapsedMilliseconds}ms; " &
                $"usable={_constructionMilliseconds + totalTimer.ElapsedMilliseconds}ms; items={_lines.Count}; controls={CountControls(Me)}")
        Catch ex As OperationCanceledException
            ' The modal was closed while loading.
        Catch ex As Exception
            queryTimer.Stop()
            lblGridStatus.Text = "Unable to load transaction items."
            lblGridStatus.Visible = True
            lblGridStatus.BringToFront()
            SetValidation("Unable to load sale: " & ex.Message, True)
            MessageBox.Show($"Unable to load sale for return: {ex.Message}", "Return / Exchange", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            _loading = False
        End Try
    End Function

    Private Sub PopulateGrid()
        _suppressGridEvents = True
        gridItems.SuspendLayout()
        Try
            gridItems.Rows.Clear()
            For Each line As ReturnLineState In _lines
                Dim itemText As String = line.ProductName
                If Not String.IsNullOrWhiteSpace(line.ProductCode) Then itemText &= "  ·  " & line.ProductCode
                If line.AlreadyReturned > 0 Then itemText &= $"  ({line.AlreadyReturned} returned)"

                Dim defaultDisposition As String = If(String.Equals(line.Condition, "Damaged", StringComparison.OrdinalIgnoreCase), DispositionWriteOff, DispositionRestock)
                line.Disposition = defaultDisposition
                Dim rowIndex As Integer = gridItems.Rows.Add(itemText, ActionNone, 0, "Good", defaultDisposition, line.ReplacementProductName)
                Dim row As DataGridViewRow = gridItems.Rows(rowIndex)
                row.Tag = line
                row.Cells("Quantity").ToolTipText = $"Maximum returnable: {line.MaxReturnable}"
                row.Cells("Item").ToolTipText = $"Purchased: {line.SoldQty}; returnable: {line.MaxReturnable}"
            Next
            gridItems.ClearSelection()
        Finally
            gridItems.ResumeLayout()
            _suppressGridEvents = False
        End Try
    End Sub

    Private Shared Function CountControls(parent As Control) As Integer
        Dim total As Integer = parent.Controls.Count
        For Each child As Control In parent.Controls
            total += CountControls(child)
        Next
        Return total
    End Function

    ' ======================================================================
    ' Grid editing and mixed allocation
    ' ======================================================================
    Private Sub Grid_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs)
        If gridItems.IsCurrentCellDirty AndAlso TypeOf gridItems.CurrentCell Is DataGridViewComboBoxCell Then
            gridItems.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If
    End Sub

    Private Sub Grid_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs)
        If _suppressGridEvents OrElse e.RowIndex < 0 Then Return
        Dim columnName As String = gridItems.Columns(e.ColumnIndex).Name
        If columnName = "Action" OrElse columnName = "Condition" OrElse columnName = "Disposition" Then
            _userInteracted = True
            ApplyGridRow(gridItems.Rows(e.RowIndex))
        End If
    End Sub

    Private Sub Grid_CellEndEdit(sender As Object, e As DataGridViewCellEventArgs)
        If _suppressGridEvents OrElse e.RowIndex < 0 Then Return
        If gridItems.Columns(e.ColumnIndex).Name = "Quantity" Then
            _userInteracted = True
            ApplyGridRow(gridItems.Rows(e.RowIndex))
        End If
    End Sub

    Private Sub Grid_CellContentClick(sender As Object, e As DataGridViewCellEventArgs)
        If e.RowIndex < 0 Then Return
        Dim columnName As String = gridItems.Columns(e.ColumnIndex).Name
        If columnName <> "Replacement" Then Return
        gridItems.EndEdit()

        Dim row As DataGridViewRow = gridItems.Rows(e.RowIndex)
        Dim line As ReturnLineState = DirectCast(row.Tag, ReturnLineState)

        ApplyGridRow(row)
        If line.ExchangeQty <= 0 Then
            SetValidation("Choose Exchange and enter a quantity before selecting a replacement.", True)
            Return
        End If
        If ShowReplacementDialog(line) Then
            row.Cells("Replacement").Value = line.ReplacementProductName
            RefreshSummary()
            UpdateValidation(True)
        End If
    End Sub

    Private Sub Grid_EditingControlShowing(sender As Object, e As DataGridViewEditingControlShowingEventArgs)
        Dim textBox As TextBox = TryCast(e.Control, TextBox)
        If textBox Is Nothing Then Return
        RemoveHandler textBox.KeyPress, AddressOf Quantity_KeyPress
        If gridItems.CurrentCell IsNot Nothing AndAlso gridItems.Columns(gridItems.CurrentCell.ColumnIndex).Name = "Quantity" Then
            AddHandler textBox.KeyPress, AddressOf Quantity_KeyPress
        End If
    End Sub

    Private Sub Quantity_KeyPress(sender As Object, e As KeyPressEventArgs)
        If Not Char.IsControl(e.KeyChar) AndAlso Not Char.IsDigit(e.KeyChar) Then
            e.Handled = True
        End If
    End Sub

    Private Sub ApplyGridRow(row As DataGridViewRow)
        Dim line As ReturnLineState = TryCast(row.Tag, ReturnLineState)
        If line Is Nothing Then Return

        row.ErrorText = ""
        row.Cells("Quantity").ErrorText = ""

        Dim quantity As Integer
        If Not Integer.TryParse(Convert.ToString(row.Cells("Quantity").Value), quantity) Then quantity = 0
        If quantity < 0 OrElse quantity > line.MaxReturnable Then
            row.Cells("Quantity").ErrorText = $"Quantity must be between 0 and {line.MaxReturnable}."
            SetValidation(row.Cells("Quantity").ErrorText, True)
            btnProcess.Enabled = False
            Return
        End If

        Dim condition As String = Convert.ToString(row.Cells("Condition").Value)
        If condition <> "Damaged" Then condition = "Good"
        Dim action As String = Convert.ToString(row.Cells("Action").Value)
        If String.IsNullOrWhiteSpace(action) OrElse (action <> ActionRefund AndAlso action <> ActionExchange) Then action = ActionNone

        If condition = "Damaged" AndAlso action <> ActionNone AndAlso action <> ActionExchange Then
            _suppressGridEvents = True
            row.Cells("Action").Value = ActionExchange
            _suppressGridEvents = False
            action = ActionExchange
            SetValidation("Damaged items are exchange only.", True)
        End If

        line.Condition = condition
        If condition = "Damaged" AndAlso action = ActionRefund Then
            _suppressGridEvents = True
            row.Cells("Action").Value = ActionExchange
            _suppressGridEvents = False
            action = ActionExchange
            SetValidation("Damaged items are exchange only.", True)
        End If

        Dim disposition As String = Convert.ToString(row.Cells("Disposition").Value)
        If String.IsNullOrWhiteSpace(disposition) Then
            disposition = If(condition = "Damaged", DispositionWriteOff, DispositionRestock)
        End If
        If condition = "Damaged" AndAlso String.Equals(disposition, DispositionRestock, StringComparison.OrdinalIgnoreCase) Then
            disposition = DispositionWriteOff
            _suppressGridEvents = True
            row.Cells("Disposition").Value = disposition
            _suppressGridEvents = False
        End If
        If disposition <> DispositionRestock AndAlso disposition <> DispositionWriteOff AndAlso disposition <> DispositionReturnToVendor Then
            disposition = If(condition = "Damaged", DispositionWriteOff, DispositionRestock)
        End If
        If condition = "Good" AndAlso String.Equals(disposition, DispositionWriteOff, StringComparison.OrdinalIgnoreCase) Then
            ' keep as-is only if user explicitly chose return to vendor is handled via DispositionReturnToVendor; write off good is unusual, leave as user chose or enforce via vendor
        End If
        line.Disposition = disposition
        If Not _suppressGridEvents Then row.Cells("Disposition").Value = disposition

        Select Case action
            Case ActionRefund
                line.ReturnQty = quantity
                line.RefundQty = quantity
                line.ExchangeQty = 0
            Case ActionExchange
                line.ReturnQty = quantity
                line.RefundQty = 0
                line.ExchangeQty = quantity
            Case Else
                line.ReturnQty = 0
                line.RefundQty = 0
                line.ExchangeQty = 0
        End Select

        RefreshSummary()
        UpdateValidation(True)
    End Sub

    Private Function ShowSplitAllocationDialog(line As ReturnLineState, totalQuantity As Integer) As Boolean
        Using dialog As New Form With {
            .Text = "Split Allocation",
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .ClientSize = New Size(360, 190),
            .MaximizeBox = False,
            .MinimizeBox = False,
            .ShowInTaskbar = False,
            .BackColor = Color.White
        }
            Dim layout As New TableLayoutPanel With {
                .Dock = DockStyle.Fill,
                .ColumnCount = 2,
                .RowCount = 5,
                .Padding = New Padding(16)
            }
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 55))
            layout.ColumnStyles.Add(New ColumnStyle(SizeType.Percent, 45))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 34))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 38))
            dialog.Controls.Add(layout)

            layout.Controls.Add(New Label With {
                .Text = $"Allocate {totalQuantity} × {line.ProductName}",
                .AutoSize = True,
                .Font = ReturnExchangeTheme.MakeFont(10, FontStyle.Bold),
                .ForeColor = ReturnExchangeTheme.PrimaryText,
                .Margin = New Padding(0, 0, 0, 8)
            }, 0, 0)
            layout.SetColumnSpan(layout.GetControlFromPosition(0, 0), 2)

            layout.Controls.Add(New Label With {
                .Text = "Refund quantity",
                .AutoSize = True,
                .Anchor = AnchorStyles.Left,
                .Font = ReturnExchangeTheme.MakeFont(9),
                .ForeColor = ReturnExchangeTheme.SecondaryText
            }, 0, 1)

            Dim initialRefund As Integer = line.RefundQty
            If initialRefund <= 0 OrElse initialRefund >= totalQuantity Then initialRefund = Math.Max(1, totalQuantity \ 2)
            Dim refundEditor As New NumericUpDown With {
                .Minimum = 1,
                .Maximum = totalQuantity - 1,
                .Value = initialRefund,
                .Dock = DockStyle.Fill,
                .Font = ReturnExchangeTheme.MakeFont(10),
                .TextAlign = HorizontalAlignment.Center,
                .Margin = New Padding(0, 4, 0, 4)
            }
            layout.Controls.Add(refundEditor, 1, 1)

            Dim lblExchangeQty As New Label With {
                .Text = $"Exchange quantity: {totalQuantity - initialRefund}",
                .AutoSize = True,
                .Font = ReturnExchangeTheme.MakeFont(9),
                .ForeColor = ReturnExchangeTheme.SecondaryText,
                .Margin = New Padding(0, 6, 0, 0)
            }
            layout.Controls.Add(lblExchangeQty, 0, 2)
            layout.SetColumnSpan(lblExchangeQty, 2)
            AddHandler refundEditor.ValueChanged,
                Sub() lblExchangeQty.Text = $"Exchange quantity: {totalQuantity - CInt(refundEditor.Value)}"

            Dim buttonHost As New FlowLayoutPanel With {
                .Dock = DockStyle.Fill,
                .FlowDirection = FlowDirection.RightToLeft,
                .WrapContents = False,
                .Margin = New Padding(0)
            }
            layout.Controls.Add(buttonHost, 0, 4)
            layout.SetColumnSpan(buttonHost, 2)

            Dim okButton As New Button With {
                .Text = "Apply",
                .DialogResult = DialogResult.OK,
                .Size = New Size(90, 30),
                .BackColor = ReturnExchangeTheme.PrimaryGold,
                .ForeColor = Color.White,
                .FlatStyle = FlatStyle.Flat
            }
            okButton.FlatAppearance.BorderSize = 0
            Dim cancelButton As New Button With {
                .Text = "Cancel",
                .DialogResult = DialogResult.Cancel,
                .Size = New Size(80, 30),
                .FlatStyle = FlatStyle.Flat,
                .Margin = New Padding(0, 0, 8, 0)
            }
            buttonHost.Controls.Add(okButton)
            buttonHost.Controls.Add(cancelButton)
            dialog.AcceptButton = okButton
            dialog.CancelButton = cancelButton

            If dialog.ShowDialog(Me) <> DialogResult.OK Then Return False

            line.ReturnQty = totalQuantity
            line.RefundQty = CInt(refundEditor.Value)
            line.ExchangeQty = totalQuantity - line.RefundQty
            Return True
        End Using
    End Function

    Private Function ShowReplacementDialog(line As ReturnLineState) As Boolean
        Dim products As New List(Of ReplacementProductOption)()
        Const sql As String =
            "SELECT ProductID, COALESCE(ProductCode,''), COALESCE(ProductName,''), " &
            "COALESCE(SellingPrice,0), COALESCE(CurrentStock,0) " &
            "FROM Products WHERE IsActive = 1 ORDER BY ProductName, ProductCode"
        Using conn As New NpgsqlConnection(Connection.GetConnectionString())
            conn.Open()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        products.Add(New ReplacementProductOption With {
                            .ProductID = Convert.ToInt32(reader.GetValue(0)),
                            .ProductCode = reader.GetString(1),
                            .ProductName = reader.GetString(2),
                            .SellingPrice = Convert.ToDecimal(reader.GetValue(3)),
                            .CurrentStock = Convert.ToInt32(reader.GetValue(4))
                        })
                    End While
                End Using
            End Using
        End Using

        Using dialog As New Form With {
            .Text = "Select Replacement Product",
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .ClientSize = New Size(540, 170),
            .MaximizeBox = False,
            .MinimizeBox = False,
            .ShowInTaskbar = False,
            .BackColor = Color.White
        }
            Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .RowCount = 4, .ColumnCount = 1, .Padding = New Padding(16)}
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 44))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 38))
            dialog.Controls.Add(layout)
            layout.Controls.Add(New Label With {
                .Text = $"Replacement for {line.ExchangeQty} x {line.ProductName}",
                .AutoSize = True,
                .Font = ReturnExchangeTheme.MakeFont(10, FontStyle.Bold),
                .ForeColor = ReturnExchangeTheme.PrimaryText
            }, 0, 0)

            Dim picker As New ComboBox With {
                .Dock = DockStyle.Fill,
                .DropDownStyle = ComboBoxStyle.DropDownList,
                .Font = ReturnExchangeTheme.MakeFont(9),
                .DataSource = products,
                .Margin = New Padding(0, 6, 0, 0)
            }
            layout.Controls.Add(picker, 0, 1)
            Dim selectedIndex As Integer = products.FindIndex(Function(product) product.ProductID = line.ReplacementProductID)
            If selectedIndex >= 0 Then
                AddHandler dialog.Shown,
                    Sub()
                        If selectedIndex < picker.Items.Count Then picker.SelectedIndex = selectedIndex
                    End Sub
            End If

            Dim hint As New Label With {
                .Text = "Only sellable stock is available for exchange; returned goods remain in quarantine.",
                .AutoSize = True,
                .ForeColor = ReturnExchangeTheme.SecondaryText,
                .Font = ReturnExchangeTheme.MakeFont(8.5)
            }
            layout.Controls.Add(hint, 0, 2)

            Dim buttons As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft, .WrapContents = False}
            Dim applyButton As New Button With {.Text = "Select", .Size = New Size(90, 30), .BackColor = ReturnExchangeTheme.PrimaryGold, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            applyButton.FlatAppearance.BorderSize = 0
            Dim cancelButton As New Button With {.Text = "Cancel", .Size = New Size(80, 30), .DialogResult = DialogResult.Cancel}
            buttons.Controls.Add(applyButton)
            buttons.Controls.Add(cancelButton)
            layout.Controls.Add(buttons, 0, 3)
            dialog.CancelButton = cancelButton

            AddHandler applyButton.Click,
                Sub()
                    Dim selected As ReplacementProductOption = TryCast(picker.SelectedItem, ReplacementProductOption)
                    If selected Is Nothing Then
                        MessageBox.Show("Select a replacement product.", "Replacement", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If
                    If selected.CurrentStock < line.ExchangeQty Then
                        MessageBox.Show($"Only {selected.CurrentStock} unit(s) are available for replacement.", "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If
                    line.ReplacementProductID = selected.ProductID
                    line.ReplacementProductName = selected.ProductName
                    line.ReplacementUnitPrice = selected.SellingPrice
                    dialog.DialogResult = DialogResult.OK
                    dialog.Close()
                End Sub

            Return dialog.ShowDialog(Me) = DialogResult.OK
        End Using
    End Function

    Private Shared Function AllocationText(line As ReturnLineState, Optional action As String = "") As String
        Select Case action
            Case ActionRefund
                Return If(line.ReturnQty > 0, $"{line.ReturnQty} refund", "Automatic")
            Case ActionExchange
                Return If(line.ReturnQty > 0, $"{line.ReturnQty} exchange", "Automatic")
            Case Else
                Return "—"
        End Select
    End Function

    ' ======================================================================
    ' Summary and validation
    ' ======================================================================
    Private Sub RefreshSummary()
        Dim totalQuantity As Integer = 0
        Dim selected As List(Of ReturnLineState) = _lines.Where(Function(line) line.HasReturn).ToList()
        For Each line As ReturnLineState In selected
            totalQuantity += line.ReturnQty
        Next
        Dim settlement As SettlementInfo = BuildExpectedSettlement(selected)

        lblItemsCount.Text = totalQuantity.ToString(CultureInfo.InvariantCulture)
        If settlement.RefundAmount > 0D Then
            lblSettlement.Text = "Customer gets " & FormatPeso(settlement.RefundAmount) & " back"
        ElseIf settlement.AdditionalPayment > 0D Then
            lblSettlement.Text = "Customer owes " & FormatPeso(settlement.AdditionalPayment)
        Else
            lblSettlement.Text = "Even exchange"
        End If
    End Sub

    Private Shared Function FormatPeso(value As Decimal) As String
        Return ChrW(&H20B1) & value.ToString("N2", CultureInfo.GetCultureInfo("en-PH"))
    End Function

    Private Sub UpdateValidation(showFeedback As Boolean)
        Dim message As String = ""
        Dim valid As Boolean = ValidateSelections(message)

        btnProcess.Enabled = valid AndAlso Not _loading AndAlso Not _processing
        If valid Then
            ClearValidation()
        ElseIf showFeedback AndAlso _userInteracted Then
            SetValidation(message, True)
        Else
            ClearValidation()
        End If
    End Sub

    Private Function ValidateSelections(ByRef message As String) As Boolean
        If _loading Then
            message = "Transaction items are still loading."
            Return False
        End If

        If cmbReason.SelectedItem Is Nothing OrElse String.IsNullOrWhiteSpace(Convert.ToString(cmbReason.SelectedItem)) Then
            message = "Select a return reason."
            Return False
        End If
        If String.Equals(Convert.ToString(cmbReason.SelectedItem), "Other", StringComparison.OrdinalIgnoreCase) AndAlso
           String.IsNullOrWhiteSpace(txtCustomReason.Text) Then
            message = "Enter the custom return reason."
            Return False
        End If

        Dim hasSelection As Boolean = False
        Dim damagedReason As Boolean = String.Equals(Convert.ToString(cmbReason.SelectedItem), "Damaged / Defective", StringComparison.OrdinalIgnoreCase)
        For Each line As ReturnLineState In _lines
            If Not line.HasReturn Then Continue For
            hasSelection = True

            If line.ReturnQty < 0 OrElse line.ReturnQty > line.MaxReturnable Then
                message = $"{line.ProductName}: quantity must be between 0 and {line.MaxReturnable}."
                Return False
            End If
            If line.Condition = "Damaged" AndAlso line.RefundQty > 0 Then
                message = $"{line.ProductName}: damaged items are exchange only."
                Return False
            End If
            If damagedReason AndAlso line.Condition <> "Damaged" Then
                message = $"{line.ProductName}: mark damaged/defective returns as Damaged."
                Return False
            End If
        If line.RefundQty + line.ExchangeQty <> line.ReturnQty Then
            message = $"{line.ProductName}: choose Refund or Exchange for the returned quantity."
            Return False
        End If
        If line.ExchangeQty > 0 AndAlso line.ReplacementProductID <= 0 Then
            message = $"{line.ProductName}: select a replacement product."
            Return False
        End If
        If String.IsNullOrWhiteSpace(line.Disposition) Then
            message = $"{line.ProductName}: select a disposition."
            Return False
        End If
        If String.Equals(line.Condition, "Damaged", StringComparison.OrdinalIgnoreCase) AndAlso String.Equals(line.Disposition, "Restock", StringComparison.OrdinalIgnoreCase) Then
            message = $"{line.ProductName}: damaged items cannot be restocked."
            Return False
        End If
        Next

        If Not hasSelection Then
            message = "Choose an action and quantity for at least one item."
            Return False
        End If

        Return True
    End Function

    Private Sub SetValidation(message As String, isError As Boolean)
        lblValidation.Text = message
        lblValidation.ForeColor = If(isError, ReturnExchangeTheme.ErrorRed, ReturnExchangeTheme.SuccessGreen)
        lblValidation.Visible = Not String.IsNullOrWhiteSpace(message)
    End Sub

    Private Sub ClearValidation()
        lblValidation.Text = ""
        lblValidation.Visible = False
    End Sub

    Private Function SyncGridToLines() As Boolean
        gridItems.EndEdit()
        For Each row As DataGridViewRow In gridItems.Rows
            ApplyGridRow(row)
            If row.ErrorText <> "" OrElse row.Cells("Quantity").ErrorText <> "" Then Return False
        Next
        Return True
    End Function

    ' ======================================================================
    ' Authorization and transaction processing
    ' ======================================================================
    Private Sub ProcessReturn(sender As Object, e As EventArgs)
        If _processing OrElse _loading Then Return

        _userInteracted = True
            If Not SyncGridToLines() Then Return

            Dim validationMessage As String = ""
            If Not ValidateSelections(validationMessage) Then
                SetValidation(validationMessage, True)
                btnProcess.Enabled = False
                Return
            End If

            Dim selected As List(Of ReturnLineState) = _lines.Where(Function(line) line.HasReturn).ToList()
            Dim settlement As SettlementInfo = BuildExpectedSettlement(selected)
            If Not ShowSettlementDialog(settlement) Then
                SetValidation("Settlement was cancelled. The return was not processed.", True)
                Return
            End If
            _processing = True
            btnProcess.Enabled = False
            btnCancel.Enabled = False
            btnProcess.Text = "Authorizing..."

            Try
                Dim subtitle As String = $"Sale #{_saleNumber}" & If(String.IsNullOrWhiteSpace(_customer), "", $"  ·  {_customer}")
                Dim authorizationDetail As String = String.Join("; ", selected.Select(
                    Function(line) $"{line.ProductName} x{line.ReturnQty} [{line.Condition} / {line.Disposition}]" &
                        If(line.ExchangeQty > 0, $" -> {line.ReplacementProductName} x{line.ExchangeQty}", "")))
                Dim selectedReason As String = If(String.Equals(Convert.ToString(cmbReason.SelectedItem), "Other", StringComparison.OrdinalIgnoreCase), txtCustomReason.Text.Trim(), Convert.ToString(cmbReason.SelectedItem))
                Dim authorizationWarning As String = If(settlement.RefundAmount > 0D,
                    $"Refund {FormatPeso(settlement.RefundAmount)} via {settlement.Method}",
                    If(settlement.AdditionalPayment > 0D, $"Collect {FormatPeso(settlement.AdditionalPayment)} via {settlement.Method}", "Even exchange")) &
                    $" | Reason: {selectedReason}"
                Dim approver As String = ApprovalDialogs.AuthorizeElevatedUser(
                    Me, "Return Authorization", subtitle, authorizationDetail, authorizationWarning,
                    If(frmLoginvb.LoggedInUsername, ""))
                If String.IsNullOrWhiteSpace(approver) Then
                    SetValidation("Authorization was cancelled. The return was not processed.", True)
                    Return
                End If

                btnProcess.Text = "Processing..."
                Dim result As ReturnResult = SaveReturn(selected, approver, settlement)

                SyncQueue.Instance.MarkDataChanged()

                MessageBox.Show($"Return processed successfully.{vbCrLf}{vbCrLf}Reference: {result.ReturnNumber}" &
                                If(result.RefundAmount > 0, $"{vbCrLf}Refund: {FormatPeso(result.RefundAmount)}", "") &
                                If(result.AdditionalPayment > 0, $"{vbCrLf}Collected: {FormatPeso(result.AdditionalPayment)}", ""),
                                "Return Completed", MessageBoxButtons.OK, MessageBoxIcon.Information)

                _completed = True
                DialogResult = DialogResult.OK
                Close()
        Catch ex As Exception
            SetValidation("Could not process: " & ex.Message, True)
            MessageBox.Show($"Could not process the return: {ex.Message}", "Return / Exchange", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Finally
            _processing = False
            If Not IsDisposed Then
                btnCancel.Enabled = True
                btnProcess.Text = "Authorize"
                UpdateValidation(False)
            End If
        End Try
    End Sub

    Private Structure ReturnResult
        Public ReturnNumber As String
        Public ReturnType As String
        Public RefundAmount As Decimal
        Public AdditionalPayment As Decimal
    End Structure

    Private Structure SettlementInfo
        Public ReturnCredit As Decimal
        Public ReplacementValue As Decimal
        Public RefundAmount As Decimal
        Public AdditionalPayment As Decimal
        Public Method As String
        Public Reference As String
    End Structure

    Private Function BuildExpectedSettlement(lines As List(Of ReturnLineState)) As SettlementInfo
        Dim value As New SettlementInfo()
        Dim settlementCredit As Decimal = 0D
        For Each line As ReturnLineState In lines
            Dim lineCredit As Decimal = CalculateExpectedCredit(line)
            Dim replacementValue As Decimal = Math.Round(line.ReplacementUnitPrice * line.ExchangeQty, 2, MidpointRounding.AwayFromZero)
            value.ReturnCredit += lineCredit
            value.ReplacementValue += replacementValue
            settlementCredit += If(line.Condition = "Damaged", Math.Min(lineCredit, replacementValue), lineCredit)
        Next
        value.ReturnCredit = Math.Round(value.ReturnCredit, 2, MidpointRounding.AwayFromZero)
        value.ReplacementValue = Math.Round(value.ReplacementValue, 2, MidpointRounding.AwayFromZero)
        Dim net As Decimal = Math.Round(settlementCredit, 2, MidpointRounding.AwayFromZero) - value.ReplacementValue
        value.RefundAmount = Math.Max(0D, net)
        value.AdditionalPayment = Math.Max(0D, -net)
        Return value
    End Function

    Private Shared Function CalculateExpectedCredit(line As ReturnLineState) As Decimal
        Dim remainingCredit As Decimal = Math.Max(0D, line.NetLineAmount - line.PriorReturnCredit)
        If line.ReturnQty = line.MaxReturnable Then Return remainingCredit
        Return Math.Min(remainingCredit,
            Math.Round((line.NetLineAmount / line.SoldQty) * line.ReturnQty, 2, MidpointRounding.AwayFromZero))
    End Function

    Private Function ShowSettlementDialog(ByRef settlement As SettlementInfo) As Boolean
        If settlement.RefundAmount = 0D AndAlso settlement.AdditionalPayment = 0D Then
            settlement.Method = "Even Exchange"
            settlement.Reference = ""
            Return True
        End If

        Using dialog As New Form With {
            .Text = "Return Settlement",
            .StartPosition = FormStartPosition.CenterParent,
            .FormBorderStyle = FormBorderStyle.FixedDialog,
            .ClientSize = New Size(430, 245),
            .MaximizeBox = False,
            .MinimizeBox = False,
            .ShowInTaskbar = False,
            .BackColor = Color.White
        }
            Dim layout As New TableLayoutPanel With {.Dock = DockStyle.Fill, .ColumnCount = 1, .RowCount = 6, .Padding = New Padding(18)}
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))
            layout.RowStyles.Add(New RowStyle(SizeType.AutoSize))
            layout.RowStyles.Add(New RowStyle(SizeType.Absolute, 42))
            layout.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
            dialog.Controls.Add(layout)

            Dim amountText As String = If(settlement.RefundAmount > 0D,
                "Refund customer " & FormatPeso(settlement.RefundAmount),
                "Collect from customer " & FormatPeso(settlement.AdditionalPayment))
            layout.Controls.Add(New Label With {.Text = amountText, .AutoSize = True, .Font = ReturnExchangeTheme.MakeFont(13, FontStyle.Bold), .ForeColor = ReturnExchangeTheme.PrimaryText}, 0, 0)
            layout.Controls.Add(New Label With {.Text = "Settlement method", .AutoSize = True, .Font = ReturnExchangeTheme.MakeFont(8.5), .ForeColor = ReturnExchangeTheme.SecondaryText, .Margin = New Padding(0, 10, 0, 2)}, 0, 1)
            Dim methodPicker As New ComboBox With {.Dock = DockStyle.Fill, .DropDownStyle = ComboBoxStyle.DropDownList, .Font = ReturnExchangeTheme.MakeFont(9)}
            methodPicker.Items.AddRange(New Object() {"Cash", "Card", "GCash", "Maya", "Bank Transfer"})
            methodPicker.SelectedIndex = 0
            layout.Controls.Add(methodPicker, 0, 2)
            layout.Controls.Add(New Label With {.Text = "Reference (required for non-cash)", .AutoSize = True, .Font = ReturnExchangeTheme.MakeFont(8.5), .ForeColor = ReturnExchangeTheme.SecondaryText}, 0, 3)
            Dim referenceBox As New TextBox With {.Dock = DockStyle.Fill, .Font = ReturnExchangeTheme.MakeFont(9), .MaxLength = 100}
            layout.Controls.Add(referenceBox, 0, 4)
            Dim buttons As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft, .WrapContents = False}
            Dim continueButton As New Button With {.Text = "Continue", .Size = New Size(100, 32), .BackColor = ReturnExchangeTheme.PrimaryGold, .ForeColor = Color.White, .FlatStyle = FlatStyle.Flat}
            continueButton.FlatAppearance.BorderSize = 0
            Dim cancelButton As New Button With {.Text = "Cancel", .Size = New Size(80, 32), .DialogResult = DialogResult.Cancel}
            buttons.Controls.Add(continueButton)
            buttons.Controls.Add(cancelButton)
            layout.Controls.Add(buttons, 0, 5)
            dialog.CancelButton = cancelButton
            Dim chosenMethod As String = ""
            Dim chosenReference As String = ""

            AddHandler continueButton.Click,
                Sub()
                    Dim method As String = Convert.ToString(methodPicker.SelectedItem)
                    If method <> "Cash" AndAlso String.IsNullOrWhiteSpace(referenceBox.Text) Then
                        MessageBox.Show("Enter the payment or refund reference.", "Settlement Reference", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                        Return
                    End If
                    chosenMethod = method
                    chosenReference = referenceBox.Text.Trim()
                    dialog.DialogResult = DialogResult.OK
                    dialog.Close()
                End Sub
            Dim accepted As Boolean = dialog.ShowDialog(Me) = DialogResult.OK
            If accepted Then
                settlement.Method = chosenMethod
                settlement.Reference = chosenReference
            End If
            Return accepted
        End Using
    End Function

    Private Function SaveReturn(lines As List(Of ReturnLineState), approver As String, expectedSettlement As SettlementInfo) As ReturnResult
        Dim userId As Integer = frmLoginvb.LoggedInUserID
        If userId <= 0 Then Throw New InvalidOperationException("A signed-in user is required to initiate a return.")
        Dim reason As String = Convert.ToString(cmbReason.SelectedItem)
        If String.Equals(reason, "Other", StringComparison.OrdinalIgnoreCase) Then
            reason = txtCustomReason.Text.Trim()
        End If
        Dim notes As String = txtNotes.Text.Trim()

        Using conn As New NpgsqlConnection(Connection.GetConnectionString())
            conn.Open()
            Using tran As NpgsqlTransaction = conn.BeginTransaction(System.Data.IsolationLevel.Serializable)
                Try
                    Using cmd As New NpgsqlCommand("SELECT Status FROM Sales WHERE SaleID = @SaleID FOR UPDATE", conn, tran)
                        cmd.Parameters.AddWithValue("@SaleID", _saleId)
                        Dim status As Object = cmd.ExecuteScalar()
                        If status Is Nothing OrElse IsDBNull(status) Then Throw New InvalidOperationException("The original sale no longer exists.")
                        If Not String.Equals(Convert.ToString(status), "Completed", StringComparison.OrdinalIgnoreCase) Then
                            Throw New InvalidOperationException("Only completed sales can be returned or exchanged.")
                        End If
                    End Using

                    Dim approverUserId As Integer
                    Using cmd As New NpgsqlCommand("SELECT UserID, UserRole, IsActive FROM Users WHERE Username = @Username FOR UPDATE", conn, tran)
                        cmd.Parameters.AddWithValue("@Username", approver)
                        Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                            If Not reader.Read() Then Throw New InvalidOperationException("The approving account no longer exists.")
                            approverUserId = Convert.ToInt32(reader("UserID"))
                            Dim active As Boolean = Convert.ToBoolean(reader("IsActive"))
                            Dim role As String = Convert.ToString(reader("UserRole"))
                            If Not active OrElse Not Rbac.IsElevated(role) Then Throw New InvalidOperationException("The approving account is no longer authorized.")
                        End Using
                    End Using
                    If approverUserId = userId Then Throw New InvalidOperationException("A different manager/admin must approve the return.")

                    Dim lineCredits As New Dictionary(Of Integer, Decimal)()
                    Dim lineRefunds As New Dictionary(Of Integer, Decimal)()
                    Dim replacementPrices As New Dictionary(Of Integer, Decimal)()
                    Dim returnCreditTotal As Decimal = 0D
                    Dim settlementCreditTotal As Decimal = 0D
                    Dim replacementTotal As Decimal = 0D
                    Dim hasRefund As Boolean = False
                    Dim hasExchange As Boolean = False

                    For Each line As ReturnLineState In lines.OrderBy(Function(item) item.OriginalSaleItemID)
                        Dim soldQty As Integer
                        Dim alreadyReturned As Integer
                        Dim originalProductId As Integer
                        Dim grossUnitPrice As Decimal
                        Dim lineDiscount As Decimal
                        Dim priorCredit As Decimal
                        Using cmd As New NpgsqlCommand(
                            "SELECT si.SaleID, si.ProductID, si.Quantity, si.UnitPrice, COALESCE(si.LineDiscountAmount,0) AS LineDiscountAmount, " &
                            "COALESCE((SELECT SUM(sri.Quantity) FROM SalesReturnItems sri JOIN SalesReturns sr ON sr.ReturnID = sri.ReturnID WHERE sri.OriginalSaleItemID = si.SaleItemID AND sr.Status = 'Completed'),0) AS AlreadyReturned, " &
                            "COALESCE((SELECT SUM(sri.ReturnCreditAmount) FROM SalesReturnItems sri JOIN SalesReturns sr ON sr.ReturnID = sri.ReturnID WHERE sri.OriginalSaleItemID = si.SaleItemID AND sr.Status = 'Completed'),0) AS PriorCredit " &
                            "FROM SaleItems si WHERE si.SaleItemID = @SaleItemID FOR UPDATE", conn, tran)
                            cmd.Parameters.AddWithValue("@SaleItemID", line.OriginalSaleItemID)
                            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                                If Not reader.Read() Then Throw New InvalidOperationException($"Original sale item for {line.ProductName} no longer exists.")
                                If Convert.ToInt32(reader("SaleID")) <> _saleId Then Throw New InvalidOperationException("A selected item does not belong to this sale.")
                                originalProductId = Convert.ToInt32(reader("ProductID"))
                                soldQty = Convert.ToInt32(reader("Quantity"))
                                grossUnitPrice = Convert.ToDecimal(reader("UnitPrice"))
                                lineDiscount = Convert.ToDecimal(reader("LineDiscountAmount"))
                                alreadyReturned = Convert.ToInt32(reader("AlreadyReturned"))
                                priorCredit = Convert.ToDecimal(reader("PriorCredit"))
                            End Using
                        End Using

                        If originalProductId <> line.ProductID Then Throw New InvalidOperationException("The original product changed; reopen the return.")
                        Dim remainingQty As Integer = soldQty - alreadyReturned
                        If line.ReturnQty <= 0 OrElse line.ReturnQty > remainingQty Then
                            Throw New InvalidOperationException($"Only {Math.Max(0, remainingQty)} unit(s) of {line.ProductName} remain returnable.")
                        End If

                        Dim netLineAmount As Decimal = Math.Round(Math.Max(0D, (grossUnitPrice * soldQty) - lineDiscount), 2, MidpointRounding.AwayFromZero)
                        Dim remainingCredit As Decimal = Math.Max(0D, netLineAmount - priorCredit)
                        Dim lineCredit As Decimal
                        If line.ReturnQty = remainingQty Then
                            lineCredit = remainingCredit
                        Else
                            lineCredit = Math.Min(remainingCredit, Math.Round((netLineAmount / soldQty) * line.ReturnQty, 2, MidpointRounding.AwayFromZero))
                        End If
                        Dim lineRefund As Decimal = If(line.RefundQty > 0,
                            Math.Round(lineCredit * line.RefundQty / line.ReturnQty, 2, MidpointRounding.AwayFromZero), 0D)
                        lineCredits(line.OriginalSaleItemID) = lineCredit
                        lineRefunds(line.OriginalSaleItemID) = lineRefund
                        returnCreditTotal += lineCredit
                        hasRefund = hasRefund OrElse line.RefundQty > 0
                        hasExchange = hasExchange OrElse line.ExchangeQty > 0

                        If line.Condition = "Damaged" AndAlso line.RefundQty > 0 Then
                            Throw New InvalidOperationException("Damaged items cannot be refunded.")
                        End If

                        Dim lineReplacementValue As Decimal = 0D
                        If line.ExchangeQty > 0 Then
                            Using cmd As New NpgsqlCommand("SELECT SellingPrice, IsActive FROM Products WHERE ProductID = @PID FOR UPDATE", conn, tran)
                                cmd.Parameters.AddWithValue("@PID", line.ReplacementProductID)
                                Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                                    If Not reader.Read() OrElse Not Convert.ToBoolean(reader("IsActive")) Then
                                        Throw New InvalidOperationException($"The replacement for {line.ProductName} is no longer available.")
                                    End If
                                    Dim currentPrice As Decimal = Convert.ToDecimal(reader("SellingPrice"))
                                    If currentPrice <> line.ReplacementUnitPrice Then
                                        Throw New InvalidOperationException($"The price of replacement item {line.ReplacementProductName} changed. Select it again and review the settlement.")
                                    End If
                                    replacementPrices(line.OriginalSaleItemID) = currentPrice
                                    lineReplacementValue = Math.Round(currentPrice * line.ExchangeQty, 2, MidpointRounding.AwayFromZero)
                                    replacementTotal += lineReplacementValue
                                End Using
                            End Using
                        End If
                        settlementCreditTotal += If(line.Condition = "Damaged", Math.Min(lineCredit, lineReplacementValue), lineCredit)
                    Next

                    returnCreditTotal = Math.Round(returnCreditTotal, 2, MidpointRounding.AwayFromZero)
                    replacementTotal = Math.Round(replacementTotal, 2, MidpointRounding.AwayFromZero)
                    settlementCreditTotal = Math.Round(settlementCreditTotal, 2, MidpointRounding.AwayFromZero)
                    Dim netSettlement As Decimal = settlementCreditTotal - replacementTotal
                    Dim refundTotal As Decimal = Math.Max(0D, netSettlement)
                    Dim additionalPayment As Decimal = Math.Max(0D, -netSettlement)
                    If refundTotal <> expectedSettlement.RefundAmount OrElse additionalPayment <> expectedSettlement.AdditionalPayment Then
                        Throw New InvalidOperationException("The return value changed while processing. Review and authorize the updated settlement.")
                    End If

                    Dim returnType As String = If(hasRefund AndAlso hasExchange, "Mixed", If(hasRefund, "Refund", "Exchange"))
                    Dim result As New ReturnResult With {.ReturnType = returnType, .RefundAmount = refundTotal, .AdditionalPayment = additionalPayment}
                    Dim returnId As Integer
                    Using cmd As New NpgsqlCommand(
                        "INSERT INTO SalesReturns (SaleID, ReturnType, Reason, RefundAmount, AdditionalPaymentAmount, SettlementMethod, SettlementReference, ApprovedBy, ApprovedByUserID, ProcessedBy, InitiatedBy, Notes, Status, ReturnDate) " &
                        "VALUES (@SaleID, @ReturnType, @Reason, @RefundAmount, @AdditionalPayment, @SettlementMethod, @SettlementReference, @ApprovedBy, @ApprovedByUserID, @ProcessedBy, @InitiatedBy, @Notes, 'Completed', now()) RETURNING ReturnID", conn, tran)
                        cmd.Parameters.AddWithValue("@SaleID", _saleId)
                        cmd.Parameters.AddWithValue("@ReturnType", returnType)
                        cmd.Parameters.AddWithValue("@Reason", If(String.IsNullOrWhiteSpace(reason), "Other", reason))
                        cmd.Parameters.AddWithValue("@RefundAmount", refundTotal)
                        cmd.Parameters.AddWithValue("@AdditionalPayment", additionalPayment)
                        cmd.Parameters.AddWithValue("@SettlementMethod", expectedSettlement.Method)
                        cmd.Parameters.AddWithValue("@SettlementReference", If(String.IsNullOrWhiteSpace(expectedSettlement.Reference), CObj(DBNull.Value), expectedSettlement.Reference))
                        cmd.Parameters.AddWithValue("@ApprovedBy", approver)
                        cmd.Parameters.AddWithValue("@ApprovedByUserID", approverUserId)
                        cmd.Parameters.AddWithValue("@ProcessedBy", userId)
                        cmd.Parameters.AddWithValue("@InitiatedBy", userId)
                        cmd.Parameters.AddWithValue("@Notes", If(String.IsNullOrWhiteSpace(notes), CObj(DBNull.Value), notes))
                        returnId = Convert.ToInt32(cmd.ExecuteScalar())
                    End Using

                    Dim returnNumber As String = Utilities.FormatReturnNumber(returnId)
                    result.ReturnNumber = returnNumber
                    Using cmd As New NpgsqlCommand("UPDATE SalesReturns SET ReturnNumber = @Number WHERE ReturnID = @ID", conn, tran)
                        cmd.Parameters.AddWithValue("@Number", returnNumber)
                        cmd.Parameters.AddWithValue("@ID", returnId)
                        cmd.ExecuteNonQuery()
                    End Using

                    For Each line As ReturnLineState In lines.OrderBy(Function(item) item.OriginalSaleItemID)
                        Dim productId As Integer = line.ProductID
                        Dim productName As String = line.ProductName
                        Dim qty As Integer = line.ReturnQty
                        Dim condition As String = If(String.Equals(line.Condition, "Damaged", StringComparison.OrdinalIgnoreCase), "Damaged", "Good")
                        Dim saleItemId As Integer = line.OriginalSaleItemID

                        Dim damaged As Boolean = (condition = "Damaged")
                        Dim disposition As String = If(String.IsNullOrWhiteSpace(line.Disposition), If(damaged, "Write Off", "Restock"), line.Disposition)
                        If damaged AndAlso String.Equals(disposition, "Restock", StringComparison.OrdinalIgnoreCase) Then disposition = "Write Off"
                        If disposition <> "Restock" AndAlso disposition <> "Write Off" AndAlso disposition <> "Return to Vendor" Then
                            disposition = If(damaged, "Write Off", "Restock")
                        End If
                        If String.Equals(disposition, "Restock", StringComparison.OrdinalIgnoreCase) AndAlso damaged Then
                            Throw New InvalidOperationException($"{productName}: damaged items cannot be restocked.")
                        End If

                        Dim lineCredit As Decimal = lineCredits(saleItemId)
                        Dim lineRefund As Decimal = lineRefunds(saleItemId)
                        Dim replacementProductId As Integer = line.ReplacementProductID
                        Dim replacementQty As Integer = line.ExchangeQty
                        Dim replacementPrice As Object = DBNull.Value
                        If replacementQty > 0 Then
                            replacementPrice = replacementPrices(saleItemId)
                        End If

                        Dim returnItemId As Integer
                        Using cmd As New NpgsqlCommand(
                            "INSERT INTO SalesReturnItems (ReturnID, OriginalSaleItemID, ProductID, Quantity, UnitPrice, Condition, Disposition, LineRefundAmount, ReturnCreditAmount, ReplacementProductID, ReplacementQuantity, ReplacementUnitPrice) " &
                            "VALUES (@ReturnID, @SaleItemID, @ProductID, @Qty, @UnitPrice, @Condition, @Disposition, @LineRefund, @ReturnCredit, @RepPID, @RepQty, @RepPrice) RETURNING ReturnItemID", conn, tran)
                            cmd.Parameters.AddWithValue("@ReturnID", returnId)
                            cmd.Parameters.AddWithValue("@SaleItemID", saleItemId)
                            cmd.Parameters.AddWithValue("@ProductID", productId)
                            cmd.Parameters.AddWithValue("@Qty", qty)
                            cmd.Parameters.AddWithValue("@UnitPrice", Math.Round(lineCredit / qty, 2, MidpointRounding.AwayFromZero))
                            cmd.Parameters.AddWithValue("@Condition", condition)
                            cmd.Parameters.AddWithValue("@Disposition", disposition)
                            cmd.Parameters.AddWithValue("@LineRefund", lineRefund)
                            cmd.Parameters.AddWithValue("@ReturnCredit", lineCredit)
                            cmd.Parameters.AddWithValue("@RepPID", If(replacementQty > 0, CObj(replacementProductId), CObj(DBNull.Value)))
                            cmd.Parameters.AddWithValue("@RepQty", If(replacementQty > 0, CObj(replacementQty), CObj(DBNull.Value)))
                            cmd.Parameters.AddWithValue("@RepPrice", replacementPrice)
                            returnItemId = Convert.ToInt32(cmd.ExecuteScalar())
                        End Using

                        If String.Equals(disposition, "Restock", StringComparison.OrdinalIgnoreCase) Then
                            Using cmd As New NpgsqlCommand("UPDATE Products SET CurrentStock = CurrentStock + @Qty, UpdatedAt = (now() AT TIME ZONE 'UTC') WHERE ProductID = @PID", conn, tran)
                                cmd.Parameters.AddWithValue("@Qty", qty)
                                cmd.Parameters.AddWithValue("@PID", productId)
                                If cmd.ExecuteNonQuery() = 0 Then
                                    Throw New Exception($"Unable to restock returned item ""{productName}"".")
                                End If
                            End Using
                            Dim currentAfter As Integer = GetCurrentStock(conn, tran, productId)
                            WriteInventoryLog(conn, tran, productId, "IN", qty,
                                              currentAfter - qty, currentAfter,
                                              $"Return {returnNumber}", $"Good return restocked - {productName}", userId)
                        ElseIf String.Equals(disposition, "Write Off", StringComparison.OrdinalIgnoreCase) Then
                            Dim currentStock As Integer = GetCurrentStock(conn, tran, productId)
                            WriteInventoryLog(conn, tran, productId, "OUT", qty,
                                              currentStock, currentStock,
                                              $"Return {returnNumber}", $"Damaged return written off - {productName}", userId)
                        ElseIf String.Equals(disposition, "Return to Vendor", StringComparison.OrdinalIgnoreCase) Then
                            Dim currentStock As Integer = GetCurrentStock(conn, tran, productId)
                            WriteInventoryLog(conn, tran, productId, "OUT", qty,
                                              currentStock, currentStock,
                                              $"Return {returnNumber}", $"Returned to Vendor - {productName}", userId)
                        End If

                        If replacementQty > 0 Then
                            Using cmd As New NpgsqlCommand("UPDATE Products SET CurrentStock = CurrentStock - @Qty, UpdatedAt = (now() AT TIME ZONE 'UTC') WHERE ProductID = @PID AND CurrentStock >= @Qty", conn, tran)
                                cmd.Parameters.AddWithValue("@Qty", replacementQty)
                                cmd.Parameters.AddWithValue("@PID", replacementProductId)
                                If cmd.ExecuteNonQuery() = 0 Then
                                    Throw New Exception($"Insufficient stock for replacement ""{GetProductName(conn, tran, replacementProductId)}"".")
                                End If
                            End Using
                            Dim newStock As Integer = GetCurrentStock(conn, tran, replacementProductId)
                            WriteInventoryLog(conn, tran, replacementProductId, "OUT", replacementQty,
                                              newStock + replacementQty, newStock,
                                              $"Return {returnNumber}", $"Replacement issued for exchange - {productName}", userId)
                        End If
                    Next

                    Dim auditDetails As String = Newtonsoft.Json.JsonConvert.SerializeObject(New With {
                        .returnNumber = returnNumber,
                        .saleId = _saleId,
                        .saleNumber = _saleNumber,
                        .type = returnType,
                        .reason = reason,
                        .refundAmount = refundTotal,
                        .additionalPayment = additionalPayment,
                        .settlementMethod = expectedSettlement.Method,
                        .settlementReference = expectedSettlement.Reference,
                        .initiatedBy = frmLoginvb.LoggedInUsername,
                        .approvedBy = approver,
                        .items = lines.Select(Function(item) New With {
                            .saleItemId = item.OriginalSaleItemID,
                            .productId = item.ProductID,
                            .quantity = item.ReturnQty,
                            .condition = item.Condition,
                            .refundQuantity = item.RefundQty,
                            .exchangeQuantity = item.ExchangeQty,
                            .replacementProductId = If(item.ExchangeQty > 0, CType(item.ReplacementProductID, Integer?), Nothing)
                        }).ToArray()
                    })
                    Using cmd As New NpgsqlCommand("INSERT INTO AuditLog (UserID, Action, Details, ActionTime) VALUES (@UserID, 'Sale Return Processed', @Details, now())", conn, tran)
                        cmd.Parameters.AddWithValue("@UserID", userId)
                        cmd.Parameters.AddWithValue("@Details", auditDetails)
                        cmd.ExecuteNonQuery()
                    End Using

                    tran.Commit()
                    Return result
                Catch
                    Try
                        tran.Rollback()
                    Catch rollbackEx As Exception
                        Console.WriteLine($"Return rollback failed: {rollbackEx.Message}")
                    End Try
                    Throw
                End Try
            End Using
        End Using
    End Function

    Private Function GetProductName(conn As NpgsqlConnection, tran As NpgsqlTransaction, productId As Integer) As String
        Using cmd As New NpgsqlCommand("SELECT COALESCE(ProductName,'') FROM Products WHERE ProductID = @PID", conn, tran)
            cmd.Parameters.AddWithValue("@PID", productId)
            Dim value As Object = cmd.ExecuteScalar()
            If value Is Nothing OrElse IsDBNull(value) Then Return productId.ToString(CultureInfo.InvariantCulture)
            Return Convert.ToString(value)
        End Using
    End Function

    Private Function GetCurrentStock(conn As NpgsqlConnection, tran As NpgsqlTransaction, productId As Integer) As Integer
        Using cmd As New NpgsqlCommand("SELECT CurrentStock FROM Products WHERE ProductID = @PID", conn, tran)
            cmd.Parameters.AddWithValue("@PID", productId)
            Dim value As Object = cmd.ExecuteScalar()
            If value Is Nothing OrElse IsDBNull(value) Then Return 0
            Return Convert.ToInt32(value)
        End Using
    End Function

    Private Sub WriteInventoryLog(conn As NpgsqlConnection, tran As NpgsqlTransaction, productId As Integer,
                                  txType As String, qty As Integer, previousStock As Integer, newStock As Integer,
                                  reference As String, notes As String, userId As Integer)
        Using cmd As New NpgsqlCommand(
            "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, UserID, Reference, Notes, CreatedAt) " &
            "VALUES (@ProductID, @Type, @Qty, @Prev, @New, @UserID, @Reference, @Notes, (now() AT TIME ZONE 'UTC'))", conn, tran)
            cmd.Parameters.AddWithValue("@ProductID", productId)
            cmd.Parameters.AddWithValue("@Type", txType)
            cmd.Parameters.AddWithValue("@Qty", Math.Max(1, qty))
            cmd.Parameters.AddWithValue("@Prev", previousStock)
            cmd.Parameters.AddWithValue("@New", newStock)
            cmd.Parameters.AddWithValue("@UserID", If(userId > 0, userId, CObj(DBNull.Value)))
            cmd.Parameters.AddWithValue("@Reference", reference)
            cmd.Parameters.AddWithValue("@Notes", notes)
            cmd.ExecuteNonQuery()
        End Using
    End Sub
End Class
