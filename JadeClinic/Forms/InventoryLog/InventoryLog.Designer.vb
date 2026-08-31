<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class InventoryLog
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim CustomizableEdges1 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges2 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim DataGridViewCellStyle1 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim DataGridViewCellStyle3 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim CustomizableEdges3 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges4 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges5 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges6 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges7 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges8 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges9 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges10 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges12 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges13 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges11 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges14 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        LabelTitle = New Label()
        DashboardPanel = New Guna.UI2.WinForms.Guna2Panel()
        PictureBox9 = New PictureBox()
        InventoryLogDataGrid = New Guna.UI2.WinForms.Guna2DataGridView()
        SortBy = New Guna.UI2.WinForms.Guna2ComboBox()
        Exportbtn = New Guna.UI2.WinForms.Guna2Button()
        Guna2HtmlLabel3 = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Guna2DateTimePicker1 = New Guna.UI2.WinForms.Guna2DateTimePicker()
        Guna2HtmlLabel4 = New Guna.UI2.WinForms.Guna2HtmlLabel()
        AddInventoryLog = New Guna.UI2.WinForms.Guna2Button()
        Guna2Panel1 = New Guna.UI2.WinForms.Guna2Panel()
        TxtSearch = New Guna.UI2.WinForms.Guna2TextBox()
        PaginationControl1 = New PaginationControl()
        lblSubtitle = New Label()
        DashboardPanel.SuspendLayout()
        CType(PictureBox9, ComponentModel.ISupportInitialize).BeginInit()
        CType(InventoryLogDataGrid, ComponentModel.ISupportInitialize).BeginInit()
        Guna2Panel1.SuspendLayout()
        SuspendLayout()
        ' 
        ' LabelTitle
        ' 
        LabelTitle.AutoSize = True
        LabelTitle.Font = New Font("Poppins Medium", 16.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelTitle.ForeColor = Color.FromArgb(CByte(42), CByte(42), CByte(42))
        LabelTitle.Location = New Point(262, 20)
        LabelTitle.Name = "LabelTitle"
        LabelTitle.Size = New Size(211, 50)
        LabelTitle.TabIndex = 0
        LabelTitle.Text = "Inventory Log"
        ' 
        ' DashboardPanel
        ' 
        DashboardPanel.BorderColor = Color.FromArgb(CByte(232), CByte(232), CByte(232))
        DashboardPanel.BorderRadius = 10
        DashboardPanel.BorderThickness = 2
        DashboardPanel.Controls.Add(PictureBox9)
        DashboardPanel.CustomizableEdges = CustomizableEdges1
        DashboardPanel.FillColor = Color.White
        DashboardPanel.Location = New Point(-10, 5)
        DashboardPanel.Name = "DashboardPanel"
        DashboardPanel.ShadowDecoration.CustomizableEdges = CustomizableEdges2
        DashboardPanel.Size = New Size(236, 1016)
        DashboardPanel.TabIndex = 6
        ' 
        ' PictureBox9
        ' 
        PictureBox9.BackColor = Color.White
        PictureBox9.Image = My.Resources.Resources.FinalLogoOfJAde
        PictureBox9.Location = New Point(63, 7)
        PictureBox9.Name = "PictureBox9"
        PictureBox9.Size = New Size(103, 85)
        PictureBox9.SizeMode = PictureBoxSizeMode.StretchImage
        PictureBox9.TabIndex = 39
        PictureBox9.TabStop = False
        ' 
        ' InventoryLogDataGrid
        ' 
        InventoryLogDataGrid.AllowUserToAddRows = False
        InventoryLogDataGrid.AllowUserToDeleteRows = False
        InventoryLogDataGrid.AllowUserToResizeColumns = False
        InventoryLogDataGrid.AllowUserToResizeRows = False
        DataGridViewCellStyle1.BackColor = Color.White
        InventoryLogDataGrid.AlternatingRowsDefaultCellStyle = DataGridViewCellStyle1
        InventoryLogDataGrid.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        InventoryLogDataGrid.BackgroundColor = Color.FromArgb(CByte(255), CByte(255), CByte(255))
        DataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle2.BackColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        DataGridViewCellStyle2.Font = New Font("Poppins", 8.5F, FontStyle.Bold)
        DataGridViewCellStyle2.ForeColor = Color.FromArgb(CByte(68), CByte(68), CByte(68))
        DataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        DataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(CByte(68), CByte(68), CByte(68))
        DataGridViewCellStyle2.WrapMode = DataGridViewTriState.True
        InventoryLogDataGrid.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle2
        InventoryLogDataGrid.ColumnHeadersHeight = 44
        InventoryLogDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        DataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle3.BackColor = Color.White
        DataGridViewCellStyle3.Font = New Font("Poppins", 9.5F)
        DataGridViewCellStyle3.ForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        DataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(CByte(235), CByte(228), CByte(200))
        DataGridViewCellStyle3.SelectionForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        DataGridViewCellStyle3.WrapMode = DataGridViewTriState.False
        InventoryLogDataGrid.DefaultCellStyle = DataGridViewCellStyle3
        InventoryLogDataGrid.GridColor = Color.FromArgb(CByte(238), CByte(236), CByte(236))
        InventoryLogDataGrid.Location = New Point(8, 72)
        InventoryLogDataGrid.Name = "InventoryLogDataGrid"
        InventoryLogDataGrid.ReadOnly = True
        InventoryLogDataGrid.RowHeadersVisible = False
        InventoryLogDataGrid.RowHeadersWidth = 51
        InventoryLogDataGrid.RowTemplate.Height = 50
        InventoryLogDataGrid.Size = New Size(1641, 768)
        InventoryLogDataGrid.TabIndex = 41
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.Font = Nothing
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty
        InventoryLogDataGrid.ThemeStyle.BackColor = Color.FromArgb(CByte(255), CByte(255), CByte(255))
        InventoryLogDataGrid.ThemeStyle.GridColor = Color.FromArgb(CByte(238), CByte(236), CByte(236))
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.Font = New Font("Poppins", 8.5F, FontStyle.Bold)
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.ForeColor = Color.FromArgb(CByte(68), CByte(68), CByte(68))
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.Height = 44
        InventoryLogDataGrid.ThemeStyle.ReadOnly = True
        InventoryLogDataGrid.ThemeStyle.RowsStyle.BackColor = Color.White
        InventoryLogDataGrid.ThemeStyle.RowsStyle.BorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        InventoryLogDataGrid.ThemeStyle.RowsStyle.Font = New Font("Poppins", 9.5F)
        InventoryLogDataGrid.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        InventoryLogDataGrid.ThemeStyle.RowsStyle.Height = 50
        InventoryLogDataGrid.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(CByte(235), CByte(228), CByte(200))
        InventoryLogDataGrid.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        ' 
        ' SortBy
        ' 
        SortBy.BackColor = Color.Transparent
        SortBy.BorderColor = Color.FromArgb(CByte(232), CByte(232), CByte(232))
        SortBy.BorderRadius = 10
        SortBy.CustomizableEdges = CustomizableEdges3
        SortBy.DrawMode = DrawMode.OwnerDrawFixed
        SortBy.DropDownStyle = ComboBoxStyle.DropDownList
        SortBy.FocusedColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        SortBy.FocusedState.BorderColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        SortBy.Font = New Font("Poppins Light", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        SortBy.ForeColor = Color.Black
        SortBy.ItemHeight = 40
        SortBy.Location = New Point(409, 11)
        SortBy.Name = "SortBy"
        SortBy.ShadowDecoration.CustomizableEdges = CustomizableEdges4
        SortBy.Size = New Size(176, 46)
        SortBy.TabIndex = 65
        ' 
        ' Exportbtn
        ' 
        Exportbtn.BorderColor = Color.Gainsboro
        Exportbtn.BorderRadius = 10
        Exportbtn.BorderThickness = 1
        Exportbtn.CustomizableEdges = CustomizableEdges5
        Exportbtn.DisabledState.BorderColor = Color.DarkGray
        Exportbtn.DisabledState.CustomBorderColor = Color.DarkGray
        Exportbtn.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        Exportbtn.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        Exportbtn.FillColor = Color.White
        Exportbtn.Font = New Font("Poppins Medium", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Exportbtn.ForeColor = Color.FromArgb(CByte(80), CByte(80), CByte(80))
        Exportbtn.Location = New Point(1505, 13)
        Exportbtn.Name = "Exportbtn"
        Exportbtn.ShadowDecoration.CustomizableEdges = CustomizableEdges6
        Exportbtn.Size = New Size(130, 42)
        Exportbtn.TabIndex = 66
        Exportbtn.Text = "📥 Export"
        ' 
        ' Guna2HtmlLabel3
        ' 
        Guna2HtmlLabel3.BackColor = Color.Transparent
        Guna2HtmlLabel3.Font = New Font("Poppins", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2HtmlLabel3.ForeColor = Color.FromArgb(CByte(49), CByte(49), CByte(49))
        Guna2HtmlLabel3.Location = New Point(329, 19)
        Guna2HtmlLabel3.Name = "Guna2HtmlLabel3"
        Guna2HtmlLabel3.Size = New Size(65, 32)
        Guna2HtmlLabel3.TabIndex = 68
        Guna2HtmlLabel3.Text = "Sort by:"
        ' 
        ' Guna2DateTimePicker1
        ' 
        Guna2DateTimePicker1.BorderColor = Color.FromArgb(CByte(232), CByte(232), CByte(232))
        Guna2DateTimePicker1.BorderRadius = 10
        Guna2DateTimePicker1.BorderThickness = 1
        Guna2DateTimePicker1.Checked = True
        Guna2DateTimePicker1.CustomizableEdges = CustomizableEdges7
        Guna2DateTimePicker1.FillColor = Color.White
        Guna2DateTimePicker1.Font = New Font("Poppins", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2DateTimePicker1.ForeColor = Color.Black
        Guna2DateTimePicker1.Format = DateTimePickerFormat.Short
        Guna2DateTimePicker1.Location = New Point(144, 17)
        Guna2DateTimePicker1.MaxDate = New Date(9998, 12, 31, 0, 0, 0, 0)
        Guna2DateTimePicker1.MinDate = New Date(1753, 1, 1, 0, 0, 0, 0)
        Guna2DateTimePicker1.Name = "Guna2DateTimePicker1"
        Guna2DateTimePicker1.ShadowDecoration.CustomizableEdges = CustomizableEdges8
        Guna2DateTimePicker1.Size = New Size(148, 40)
        Guna2DateTimePicker1.TabIndex = 76
        Guna2DateTimePicker1.Value = New Date(2025, 9, 27, 23, 48, 46, 373)
        ' 
        ' Guna2HtmlLabel4
        ' 
        Guna2HtmlLabel4.BackColor = Color.Transparent
        Guna2HtmlLabel4.Font = New Font("Poppins", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2HtmlLabel4.ForeColor = Color.FromArgb(CByte(49), CByte(49), CByte(49))
        Guna2HtmlLabel4.Location = New Point(22, 25)
        Guna2HtmlLabel4.Name = "Guna2HtmlLabel4"
        Guna2HtmlLabel4.Size = New Size(116, 32)
        Guna2HtmlLabel4.TabIndex = 77
        Guna2HtmlLabel4.Text = "Filter by Date:"
        ' 
        ' AddInventoryLog
        ' 
        AddInventoryLog.BorderRadius = 10
        AddInventoryLog.CustomizableEdges = CustomizableEdges9
        AddInventoryLog.DisabledState.BorderColor = Color.DarkGray
        AddInventoryLog.DisabledState.CustomBorderColor = Color.DarkGray
        AddInventoryLog.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        AddInventoryLog.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        AddInventoryLog.FillColor = Color.FromArgb(CByte(191), CByte(155), CByte(48))
        AddInventoryLog.Font = New Font("Poppins Medium", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        AddInventoryLog.ForeColor = Color.White
        AddInventoryLog.Location = New Point(1284, 13)
        AddInventoryLog.Name = "AddInventoryLog"
        AddInventoryLog.ShadowDecoration.CustomizableEdges = CustomizableEdges10
        AddInventoryLog.Size = New Size(206, 44)
        AddInventoryLog.TabIndex = 78
        AddInventoryLog.Text = " ＋ Add Inventory Log"
        AddInventoryLog.Visible = False
        ' 
        ' Guna2Panel1
        ' 
        Guna2Panel1.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Guna2Panel1.BackColor = Color.Transparent
        Guna2Panel1.BorderColor = Color.FromArgb(CByte(232), CByte(232), CByte(232))
        Guna2Panel1.BorderRadius = 10
        Guna2Panel1.BorderThickness = 2
        Guna2Panel1.Controls.Add(PaginationControl1)
        Guna2Panel1.Controls.Add(TxtSearch)
        Guna2Panel1.Controls.Add(InventoryLogDataGrid)
        Guna2Panel1.Controls.Add(SortBy)
        Guna2Panel1.Controls.Add(AddInventoryLog)
        Guna2Panel1.Controls.Add(Guna2HtmlLabel3)
        Guna2Panel1.Controls.Add(Exportbtn)
        Guna2Panel1.Controls.Add(Guna2HtmlLabel4)
        Guna2Panel1.Controls.Add(Guna2DateTimePicker1)
        Guna2Panel1.CustomizableEdges = CustomizableEdges12
        Guna2Panel1.FillColor = Color.White
        Guna2Panel1.Location = New Point(240, 113)
        Guna2Panel1.Name = "Guna2Panel1"
        Guna2Panel1.Padding = New Padding(0)
        Guna2Panel1.ShadowDecoration.CustomizableEdges = CustomizableEdges13
        Guna2Panel1.ShadowDecoration.Depth = 0
        Guna2Panel1.ShadowDecoration.Shadow = New Padding(0)
        Guna2Panel1.Size = New Size(1650, 908)
        Guna2Panel1.TabIndex = 79
        ' 
        ' TxtSearch
        ' 
        TxtSearch.BorderColor = Color.FromArgb(CByte(220), CByte(220), CByte(220))
        TxtSearch.BorderRadius = 8
        TxtSearch.CustomizableEdges = CustomizableEdges11
        TxtSearch.DefaultText = ""
        TxtSearch.DisabledState.BorderColor = Color.FromArgb(CByte(208), CByte(208), CByte(208))
        TxtSearch.DisabledState.FillColor = Color.FromArgb(CByte(226), CByte(226), CByte(226))
        TxtSearch.DisabledState.ForeColor = Color.FromArgb(CByte(138), CByte(138), CByte(138))
        TxtSearch.DisabledState.PlaceholderForeColor = Color.FromArgb(CByte(138), CByte(138), CByte(138))
        TxtSearch.FocusedState.BorderColor = Color.FromArgb(CByte(196), CByte(154), CByte(44))
        TxtSearch.Font = New Font("Poppins", 9.5F)
        TxtSearch.ForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        TxtSearch.HoverState.BorderColor = Color.FromArgb(CByte(196), CByte(154), CByte(44))
        TxtSearch.Location = New Point(779, 13)
        TxtSearch.Margin = New Padding(3, 4, 3, 4)
        TxtSearch.Name = "TxtSearch"
        TxtSearch.PlaceholderText = "🔍  Search by ID or Reference..."
        TxtSearch.SelectedText = ""
        TxtSearch.ShadowDecoration.CustomizableEdges = CustomizableEdges11
        TxtSearch.Size = New Size(446, 42)
        TxtSearch.TabIndex = 85
        ' 
        ' lblSubtitle
        ' 
        lblSubtitle.AutoSize = True
        lblSubtitle.Font = New Font("Poppins", 9.5F)
        lblSubtitle.ForeColor = Color.FromArgb(CByte(119), CByte(119), CByte(119))
        lblSubtitle.Location = New Point(264, 68)
        lblSubtitle.Name = "lblSubtitle"
        lblSubtitle.Size = New Size(339, 28)
        lblSubtitle.TabIndex = 81
        lblSubtitle.Text = "View and track all inventory transactions." & vbCrLf
        ' 
        ' PaginationControl1
        ' 
        PaginationControl1.BackColor = Color.White
        PaginationControl1.Location = New Point(8, 846)
        PaginationControl1.Name = "PaginationControl1"
        PaginationControl1.Size = New Size(1641, 48)
        PaginationControl1.TabIndex = 86
        ' 
        ' InventoryLog
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(245), CByte(243), CByte(239))
        ClientSize = New Size(1902, 1033)
        Controls.Add(lblSubtitle)
        Controls.Add(Guna2Panel1)
        Controls.Add(DashboardPanel)
        Controls.Add(LabelTitle)
        Name = "InventoryLog"
        SizeGripStyle = SizeGripStyle.Hide
        StartPosition = FormStartPosition.CenterScreen
        Text = "Orders"
        DashboardPanel.ResumeLayout(False)
        CType(PictureBox9, ComponentModel.ISupportInitialize).EndInit()
        CType(InventoryLogDataGrid, ComponentModel.ISupportInitialize).EndInit()
        Guna2Panel1.ResumeLayout(False)
        Guna2Panel1.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents LabelTitle As Label
    Friend WithEvents DashboardPanel As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents PictureBox9 As PictureBox
    Friend WithEvents InventoryLogDataGrid As Guna.UI2.WinForms.Guna2DataGridView
    Friend WithEvents SortBy As Guna.UI2.WinForms.Guna2ComboBox
    Friend WithEvents Exportbtn As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents filtertype As Guna.UI2.WinForms.Guna2ComboBox
    Friend WithEvents Guna2HtmlLabel2 As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents Guna2HtmlLabel3 As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents Guna2DateTimePicker1 As Guna.UI2.WinForms.Guna2DateTimePicker
    Friend WithEvents Guna2HtmlLabel4 As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents AddInventoryLog As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents Guna2Panel1 As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents TxtSearch As Guna.UI2.WinForms.Guna2TextBox
    Friend WithEvents lblSubtitle As Label
    Friend WithEvents PaginationControl1 As PaginationControl
End Class
