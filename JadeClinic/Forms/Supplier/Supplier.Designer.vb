<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Supplier
    Inherits System.Windows.Forms.Form

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

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Dim CustomizableEdges1 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges2 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges3 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim DataGridViewCellStyle1 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim DataGridViewCellStyle3 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim CustomizableEdges11 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges4 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges5 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges6 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges7 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges8 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges9 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges10 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(Supplier))
        LabelTitle = New Label()
        lblSubtitle = New Label()
        NavigationPanel = New Guna.UI2.WinForms.Guna2Panel()
        PictureBox9 = New PictureBox()
        Guna2CirclePictureBox5 = New Guna.UI2.WinForms.Guna2CirclePictureBox()
        InventoryLogDataGrid = New Guna.UI2.WinForms.Guna2DataGridView()
        Guna2Panel1 = New Guna.UI2.WinForms.Guna2Panel()
        SortBy = New Guna.UI2.WinForms.Guna2ComboBox()
        Exportbtn = New Guna.UI2.WinForms.Guna2Button()
        Guna2Button1 = New Guna.UI2.WinForms.Guna2Button()
        TxtSearch = New Guna.UI2.WinForms.Guna2TextBox()
        lblUsername = New Guna.UI2.WinForms.Guna2HtmlLabel()
        NavigationPanel.SuspendLayout()
        CType(PictureBox9, ComponentModel.ISupportInitialize).BeginInit()
        CType(Guna2CirclePictureBox5, ComponentModel.ISupportInitialize).BeginInit()
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
        LabelTitle.Size = New Size(151, 50)
        LabelTitle.TabIndex = 0
        LabelTitle.Text = "Suppliers"
        ' 
        ' lblSubtitle
        ' 
        lblSubtitle.AutoSize = True
        lblSubtitle.Font = New Font("Poppins", 9.5F)
        lblSubtitle.ForeColor = Color.FromArgb(CByte(119), CByte(119), CByte(119))
        lblSubtitle.Location = New Point(264, 68)
        lblSubtitle.Name = "lblSubtitle"
        lblSubtitle.Size = New Size(502, 28)
        lblSubtitle.TabIndex = 1
        lblSubtitle.Text = "Manage your supplier information and inventory relationships"
        ' 
        ' NavigationPanel
        ' 
        NavigationPanel.BorderColor = Color.FromArgb(CByte(232), CByte(232), CByte(232))
        NavigationPanel.BorderRadius = 10
        NavigationPanel.BorderThickness = 2
        NavigationPanel.Controls.Add(PictureBox9)
        NavigationPanel.CustomizableEdges = CustomizableEdges1
        NavigationPanel.FillColor = Color.White
        NavigationPanel.Location = New Point(-10, 5)
        NavigationPanel.Name = "NavigationPanel"
        NavigationPanel.ShadowDecoration.CustomizableEdges = CustomizableEdges2
        NavigationPanel.ShadowDecoration.Depth = 0
        NavigationPanel.ShadowDecoration.Shadow = New Padding(0, 0, 4, 4)
        NavigationPanel.Size = New Size(236, 1016)
        NavigationPanel.TabIndex = 6
        ' 
        ' PictureBox9
        ' 
        PictureBox9.BackColor = Color.White
        PictureBox9.Image = My.Resources.Resources.Jade_Dental_Logo
        PictureBox9.Location = New Point(63, 7)
        PictureBox9.Name = "PictureBox9"
        PictureBox9.Size = New Size(103, 85)
        PictureBox9.SizeMode = PictureBoxSizeMode.StretchImage
        PictureBox9.TabIndex = 39
        PictureBox9.TabStop = False
        ' 
        ' Guna2CirclePictureBox5
        ' 
        Guna2CirclePictureBox5.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Guna2CirclePictureBox5.ImageRotate = 0F
        Guna2CirclePictureBox5.Location = New Point(1755, 20)
        Guna2CirclePictureBox5.Name = "Guna2CirclePictureBox5"
        Guna2CirclePictureBox5.ShadowDecoration.CustomizableEdges = CustomizableEdges3
        Guna2CirclePictureBox5.ShadowDecoration.Mode = Guna.UI2.WinForms.Enums.ShadowMode.Circle
        Guna2CirclePictureBox5.Size = New Size(31, 28)
        Guna2CirclePictureBox5.TabIndex = 40
        Guna2CirclePictureBox5.TabStop = False
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
        InventoryLogDataGrid.Size = New Size(1641, 801)
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
        ' Guna2Panel1
        ' 
        Guna2Panel1.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Guna2Panel1.BackColor = Color.Transparent
        Guna2Panel1.BorderColor = Color.FromArgb(CByte(232), CByte(232), CByte(232))
        Guna2Panel1.BorderRadius = 10
        Guna2Panel1.BorderThickness = 2
        Guna2Panel1.Controls.Add(InventoryLogDataGrid)
        Guna2Panel1.Controls.Add(SortBy)
        Guna2Panel1.Controls.Add(Exportbtn)
        Guna2Panel1.Controls.Add(Guna2Button1)
        Guna2Panel1.Controls.Add(TxtSearch)
        Guna2Panel1.CustomizableEdges = CustomizableEdges10
        Guna2Panel1.FillColor = Color.White
        Guna2Panel1.Location = New Point(240, 113)
        Guna2Panel1.Name = "Guna2Panel1"
        Guna2Panel1.ShadowDecoration.CustomizableEdges = CustomizableEdges11
        Guna2Panel1.ShadowDecoration.Depth = 0
        Guna2Panel1.ShadowDecoration.Shadow = New Padding(0, 0, 4, 4)
        Guna2Panel1.Size = New Size(1650, 908)
        Guna2Panel1.TabIndex = 80
        ' 
        ' SortBy
        ' 
        SortBy.BackColor = Color.Transparent
        SortBy.BorderColor = Color.FromArgb(CByte(220), CByte(220), CByte(220))
        SortBy.BorderRadius = 8
        SortBy.CustomizableEdges = CustomizableEdges4
        SortBy.DrawMode = DrawMode.OwnerDrawFixed
        SortBy.DropDownStyle = ComboBoxStyle.DropDownList
        SortBy.FocusedColor = Color.FromArgb(CByte(196), CByte(154), CByte(44))
        SortBy.FocusedState.BorderColor = Color.FromArgb(CByte(196), CByte(154), CByte(44))
        SortBy.Font = New Font("Poppins", 9.5F)
        SortBy.ForeColor = Color.FromArgb(CByte(102), CByte(102), CByte(102))
        SortBy.ItemHeight = 36
        SortBy.Location = New Point(362, 14)
        SortBy.Name = "SortBy"
        SortBy.ShadowDecoration.CustomizableEdges = CustomizableEdges5
        SortBy.Size = New Size(160, 42)
        SortBy.TabIndex = 65
        ' 
        ' Exportbtn
        ' 
        Exportbtn.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Exportbtn.BorderColor = Color.FromArgb(CByte(220), CByte(220), CByte(220))
        Exportbtn.BorderRadius = 8
        Exportbtn.BorderThickness = 1
        Exportbtn.CustomizableEdges = CustomizableEdges6
        Exportbtn.DisabledState.BorderColor = Color.DarkGray
        Exportbtn.DisabledState.CustomBorderColor = Color.DarkGray
        Exportbtn.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        Exportbtn.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        Exportbtn.FillColor = Color.White
        Exportbtn.Font = New Font("Poppins", 9F)
        Exportbtn.ForeColor = Color.FromArgb(CByte(80), CByte(80), CByte(80))
        Exportbtn.Location = New Point(1312, 14)
        Exportbtn.Name = "Exportbtn"
        Exportbtn.ShadowDecoration.CustomizableEdges = CustomizableEdges7
        Exportbtn.Size = New Size(130, 42)
        Exportbtn.TabIndex = 66
        Exportbtn.Text = "📥 Export"
        ' 
        ' Guna2Button1
        ' 
        Guna2Button1.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        Guna2Button1.BorderRadius = 8
        Guna2Button1.CustomizableEdges = CustomizableEdges8
        Guna2Button1.DisabledState.BorderColor = Color.DarkGray
        Guna2Button1.DisabledState.CustomBorderColor = Color.DarkGray
        Guna2Button1.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        Guna2Button1.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        Guna2Button1.FillColor = Color.FromArgb(CByte(190), CByte(154), CByte(48))
        Guna2Button1.Font = New Font("Poppins", 9F)
        Guna2Button1.ForeColor = Color.White
        Guna2Button1.Location = New Point(1458, 14)
        Guna2Button1.Name = "Guna2Button1"
        Guna2Button1.ShadowDecoration.CustomizableEdges = CustomizableEdges9
        Guna2Button1.Size = New Size(182, 42)
        Guna2Button1.TabIndex = 85
        Guna2Button1.Text = "＋ Add Supplier"
        ' 
        ' TxtSearch
        ' 
        TxtSearch.BorderColor = Color.FromArgb(CByte(220), CByte(220), CByte(220))
        TxtSearch.BorderRadius = 8
        TxtSearch.CustomizableEdges = CustomizableEdges10
        TxtSearch.DefaultText = ""
        TxtSearch.DisabledState.BorderColor = Color.FromArgb(CByte(208), CByte(208), CByte(208))
        TxtSearch.DisabledState.FillColor = Color.FromArgb(CByte(226), CByte(226), CByte(226))
        TxtSearch.DisabledState.ForeColor = Color.FromArgb(CByte(138), CByte(138), CByte(138))
        TxtSearch.DisabledState.PlaceholderForeColor = Color.FromArgb(CByte(138), CByte(138), CByte(138))
        TxtSearch.FocusedState.BorderColor = Color.FromArgb(CByte(196), CByte(154), CByte(44))
        TxtSearch.Font = New Font("Poppins", 9.5F)
        TxtSearch.ForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        TxtSearch.HoverState.BorderColor = Color.FromArgb(CByte(196), CByte(154), CByte(44))
        TxtSearch.Location = New Point(16, 14)
        TxtSearch.Margin = New Padding(3, 4, 3, 4)
        TxtSearch.Name = "TxtSearch"
        TxtSearch.PlaceholderText = "🔍  Search suppliers..."
        TxtSearch.SelectedText = ""
        TxtSearch.ShadowDecoration.CustomizableEdges = CustomizableEdges10
        TxtSearch.Size = New Size(330, 42)
        TxtSearch.TabIndex = 84
        ' 
        ' lblUsername
        ' 
        lblUsername.Anchor = AnchorStyles.Top Or AnchorStyles.Right
        lblUsername.BackColor = Color.Transparent
        lblUsername.Font = New Font("Poppins Light", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblUsername.ForeColor = Color.FromArgb(CByte(59), CByte(59), CByte(59))
        lblUsername.Location = New Point(1793, 20)
        lblUsername.Name = "lblUsername"
        lblUsername.Size = New Size(65, 28)
        lblUsername.TabIndex = 75
        lblUsername.Text = "20 Items"
        ' 
        ' Supplier
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        AutoScroll = True
        BackColor = Color.FromArgb(CByte(245), CByte(243), CByte(239))
        ClientSize = New Size(1902, 1033)
        Controls.Add(lblSubtitle)
        Controls.Add(lblUsername)
        Controls.Add(Guna2CirclePictureBox5)
        Controls.Add(NavigationPanel)
        Controls.Add(LabelTitle)
        Controls.Add(Guna2Panel1)
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        Name = "Supplier"
        SizeGripStyle = SizeGripStyle.Hide
        StartPosition = FormStartPosition.CenterScreen
        Text = "Orders"
        NavigationPanel.ResumeLayout(False)
        CType(PictureBox9, ComponentModel.ISupportInitialize).EndInit()
        CType(Guna2CirclePictureBox5, ComponentModel.ISupportInitialize).EndInit()
        CType(InventoryLogDataGrid, ComponentModel.ISupportInitialize).EndInit()
        Guna2Panel1.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents LabelTitle As Label
    Friend WithEvents lblSubtitle As Label
    Friend WithEvents NavigationPanel As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents PictureBox9 As PictureBox
    Friend WithEvents Guna2CirclePictureBox5 As Guna.UI2.WinForms.Guna2CirclePictureBox
    Friend WithEvents InventoryLogDataGrid As Guna.UI2.WinForms.Guna2DataGridView
    Friend WithEvents SortBy As Guna.UI2.WinForms.Guna2ComboBox
    Friend WithEvents Exportbtn As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents lblUsername As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents Guna2Panel1 As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents TxtSearch As Guna.UI2.WinForms.Guna2TextBox
    Friend WithEvents Guna2Button1 As Guna.UI2.WinForms.Guna2Button
End Class
