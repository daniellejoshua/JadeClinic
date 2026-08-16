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
        Dim CustomizableEdges11 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges12 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges13 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        LabelTitle = New Label()
        DashboardPanel = New Guna.UI2.WinForms.Guna2Panel()
        PictureBox9 = New PictureBox()
        InventoryLogDataGrid = New Guna.UI2.WinForms.Guna2DataGridView()
        SortBy = New Guna.UI2.WinForms.Guna2ComboBox()
        Exportbtn = New Guna.UI2.WinForms.Guna2Button()
        Guna2HtmlLabel3 = New Guna.UI2.WinForms.Guna2HtmlLabel()
        lblUsername = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Guna2DateTimePicker1 = New Guna.UI2.WinForms.Guna2DateTimePicker()
        Guna2HtmlLabel4 = New Guna.UI2.WinForms.Guna2HtmlLabel()
        AddInventoryLog = New Guna.UI2.WinForms.Guna2Button()
        Guna2Panel1 = New Guna.UI2.WinForms.Guna2Panel()
        Guna2CirclePictureBox5 = New Guna.UI2.WinForms.Guna2CirclePictureBox()
        DashboardPanel.SuspendLayout()
        CType(PictureBox9, ComponentModel.ISupportInitialize).BeginInit()
        CType(InventoryLogDataGrid, ComponentModel.ISupportInitialize).BeginInit()
        Guna2Panel1.SuspendLayout()
        CType(Guna2CirclePictureBox5, ComponentModel.ISupportInitialize).BeginInit()
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
        DashboardPanel.BorderColor = Color.FromArgb(CByte(246), CByte(245), CByte(242))
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
        InventoryLogDataGrid.BackgroundColor = Color.FromArgb(CByte(246), CByte(245), CByte(242))
        DataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle2.BackColor = Color.FromArgb(CByte(100), CByte(88), CByte(255))
        DataGridViewCellStyle2.Font = New Font("Segoe UI", 9F)
        DataGridViewCellStyle2.ForeColor = Color.White
        DataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight
        DataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText
        DataGridViewCellStyle2.WrapMode = DataGridViewTriState.True
        InventoryLogDataGrid.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle2
        InventoryLogDataGrid.ColumnHeadersHeight = 4
        InventoryLogDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing
        DataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle3.BackColor = Color.White
        DataGridViewCellStyle3.Font = New Font("Segoe UI", 9F)
        DataGridViewCellStyle3.ForeColor = Color.FromArgb(CByte(71), CByte(69), CByte(94))
        DataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(CByte(231), CByte(229), CByte(255))
        DataGridViewCellStyle3.SelectionForeColor = Color.FromArgb(CByte(71), CByte(69), CByte(94))
        DataGridViewCellStyle3.WrapMode = DataGridViewTriState.False
        InventoryLogDataGrid.DefaultCellStyle = DataGridViewCellStyle3
        InventoryLogDataGrid.GridColor = Color.FromArgb(CByte(231), CByte(229), CByte(255))
        InventoryLogDataGrid.Location = New Point(6, 3)
        InventoryLogDataGrid.Name = "InventoryLogDataGrid"
        InventoryLogDataGrid.RowHeadersVisible = False
        InventoryLogDataGrid.RowHeadersWidth = 51
        InventoryLogDataGrid.Size = New Size(1651, 852)
        InventoryLogDataGrid.TabIndex = 41
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.Font = Nothing
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty
        InventoryLogDataGrid.ThemeStyle.BackColor = Color.FromArgb(CByte(246), CByte(245), CByte(242))
        InventoryLogDataGrid.ThemeStyle.GridColor = Color.FromArgb(CByte(231), CByte(229), CByte(255))
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(CByte(100), CByte(88), CByte(255))
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.Font = New Font("Segoe UI", 9F)
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.ForeColor = Color.White
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing
        InventoryLogDataGrid.ThemeStyle.HeaderStyle.Height = 4
        InventoryLogDataGrid.ThemeStyle.ReadOnly = False
        InventoryLogDataGrid.ThemeStyle.RowsStyle.BackColor = Color.White
        InventoryLogDataGrid.ThemeStyle.RowsStyle.BorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        InventoryLogDataGrid.ThemeStyle.RowsStyle.Font = New Font("Segoe UI", 9F)
        InventoryLogDataGrid.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(CByte(71), CByte(69), CByte(94))
        InventoryLogDataGrid.ThemeStyle.RowsStyle.Height = 29
        InventoryLogDataGrid.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(CByte(231), CByte(229), CByte(255))
        InventoryLogDataGrid.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(CByte(71), CByte(69), CByte(94))
        ' 
        ' SortBy
        ' 
        SortBy.BackColor = Color.Transparent
        SortBy.BorderColor = Color.FromArgb(CByte(253), CByte(198), CByte(44))
        SortBy.BorderRadius = 10
        SortBy.CustomizableEdges = CustomizableEdges3
        SortBy.DrawMode = DrawMode.OwnerDrawFixed
        SortBy.DropDownStyle = ComboBoxStyle.DropDownList
        SortBy.FocusedColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        SortBy.FocusedState.BorderColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        SortBy.Font = New Font("Poppins Light", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        SortBy.ForeColor = Color.Black
        SortBy.ItemHeight = 40
        SortBy.Location = New Point(621, 96)
        SortBy.Name = "SortBy"
        SortBy.ShadowDecoration.CustomizableEdges = CustomizableEdges4
        SortBy.Size = New Size(309, 46)
        SortBy.TabIndex = 65
        ' 
        ' Exportbtn
        ' 
        Exportbtn.BorderRadius = 10
        Exportbtn.CustomizableEdges = CustomizableEdges5
        Exportbtn.DisabledState.BorderColor = Color.DarkGray
        Exportbtn.DisabledState.CustomBorderColor = Color.DarkGray
        Exportbtn.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        Exportbtn.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        Exportbtn.BorderColor = Color.FromArgb(CByte(190), CByte(154), CByte(48))
        Exportbtn.BorderThickness = 1
        Exportbtn.FillColor = Color.White
        Exportbtn.HoverState.BorderColor = Color.FromArgb(CByte(238), CByte(188), CByte(27))
        Exportbtn.HoverState.FillColor = Color.FromArgb(CByte(251), CByte(247), CByte(236))
        Exportbtn.HoverState.ForeColor = Color.FromArgb(CByte(190), CByte(154), CByte(48))
        Exportbtn.PressedColor = Color.FromArgb(CByte(245), CByte(232), CByte(197))
        Exportbtn.Font = New Font("Poppins Medium", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Exportbtn.ForeColor = Color.FromArgb(CByte(190), CByte(154), CByte(48))
        Exportbtn.Location = New Point(1779, 102)
        Exportbtn.Name = "Exportbtn"
        Exportbtn.ShadowDecoration.CustomizableEdges = CustomizableEdges6
        Exportbtn.Size = New Size(110, 40)
        Exportbtn.TabIndex = 66
        Exportbtn.Text = "Export"
        ' 
        ' Guna2HtmlLabel3
        ' 
        Guna2HtmlLabel3.BackColor = Color.Transparent
        Guna2HtmlLabel3.Font = New Font("Poppins", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2HtmlLabel3.ForeColor = Color.FromArgb(CByte(49), CByte(49), CByte(49))
        Guna2HtmlLabel3.Location = New Point(541, 104)
        Guna2HtmlLabel3.Name = "Guna2HtmlLabel3"
        Guna2HtmlLabel3.Size = New Size(65, 32)
        Guna2HtmlLabel3.TabIndex = 68
        Guna2HtmlLabel3.Text = "Sort by:"
        ' 
        ' lblUsername
        ' 
        lblUsername.BackColor = Color.Transparent
        lblUsername.Font = New Font("Poppins Light", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblUsername.ForeColor = Color.FromArgb(CByte(59), CByte(59), CByte(59))
        lblUsername.Location = New Point(1793, 20)
        lblUsername.Name = "lblUsername"
        lblUsername.Size = New Size(65, 28)
        lblUsername.TabIndex = 75
        lblUsername.Text = "20 Items"
        ' 
        ' Guna2DateTimePicker1
        ' 
        Guna2DateTimePicker1.BorderColor = Color.FromArgb(CByte(253), CByte(198), CByte(44))
        Guna2DateTimePicker1.BorderRadius = 10
        Guna2DateTimePicker1.BorderThickness = 1
        Guna2DateTimePicker1.Checked = True
        Guna2DateTimePicker1.CustomizableEdges = CustomizableEdges7
        Guna2DateTimePicker1.FillColor = Color.White
        Guna2DateTimePicker1.Font = New Font("Poppins", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2DateTimePicker1.ForeColor = Color.Black
        Guna2DateTimePicker1.Format = DateTimePickerFormat.Short
        Guna2DateTimePicker1.Location = New Point(356, 102)
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
        Guna2HtmlLabel4.Location = New Point(234, 110)
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
        AddInventoryLog.FillColor = Color.FromArgb(CByte(238), CByte(188), CByte(27))
        AddInventoryLog.HoverState.FillColor = Color.FromArgb(CByte(223), CByte(175), CByte(22))
        AddInventoryLog.PressedColor = Color.FromArgb(CByte(190), CByte(154), CByte(48))
        AddInventoryLog.Font = New Font("Poppins Medium", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        AddInventoryLog.ForeColor = Color.White
        AddInventoryLog.Location = New Point(1578, 102)
        AddInventoryLog.Name = "AddInventoryLog"
        AddInventoryLog.ShadowDecoration.CustomizableEdges = CustomizableEdges10
        AddInventoryLog.Size = New Size(173, 40)
        AddInventoryLog.TabIndex = 78
        AddInventoryLog.Text = "Add Inventory Log"
        AddInventoryLog.Visible = False
        ' 
        ' Guna2Panel1
        ' 
        Guna2Panel1.BorderColor = Color.FromArgb(CByte(246), CByte(245), CByte(242))
        Guna2Panel1.BorderThickness = 2
        Guna2Panel1.Controls.Add(InventoryLogDataGrid)
        Guna2Panel1.CustomizableEdges = CustomizableEdges11
        Guna2Panel1.FillColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        Guna2Panel1.Location = New Point(235, 160)
        Guna2Panel1.Name = "Guna2Panel1"
        Guna2Panel1.ShadowDecoration.CustomizableEdges = CustomizableEdges12
        Guna2Panel1.Size = New Size(1657, 861)
        Guna2Panel1.TabIndex = 79
        ' 
        ' Guna2CirclePictureBox5
        ' 
        Guna2CirclePictureBox5.ImageRotate = 0F
        Guna2CirclePictureBox5.Location = New Point(1742, 26)
        Guna2CirclePictureBox5.Name = "Guna2CirclePictureBox5"
        Guna2CirclePictureBox5.ShadowDecoration.CustomizableEdges = CustomizableEdges13
        Guna2CirclePictureBox5.ShadowDecoration.Mode = Guna.UI2.WinForms.Enums.ShadowMode.Circle
        Guna2CirclePictureBox5.Size = New Size(31, 28)
        Guna2CirclePictureBox5.TabIndex = 80
        Guna2CirclePictureBox5.TabStop = False
        ' 
        ' InventoryLog
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        AutoScroll = True
        BackColor = Color.White
        ClientSize = New Size(1902, 1033)
        Controls.Add(Guna2CirclePictureBox5)
        Controls.Add(Guna2Panel1)
        Controls.Add(AddInventoryLog)
        Controls.Add(Guna2HtmlLabel4)
        Controls.Add(Guna2DateTimePicker1)
        Controls.Add(lblUsername)
        Controls.Add(Guna2HtmlLabel3)
        Controls.Add(Exportbtn)
        Controls.Add(SortBy)
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
        CType(Guna2CirclePictureBox5, ComponentModel.ISupportInitialize).EndInit()
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
    Friend WithEvents lblUsername As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents Guna2DateTimePicker1 As Guna.UI2.WinForms.Guna2DateTimePicker
    Friend WithEvents Guna2HtmlLabel4 As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents AddInventoryLog As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents Guna2Panel1 As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents Guna2CirclePictureBox5 As Guna.UI2.WinForms.Guna2CirclePictureBox
End Class
