<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Staff
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
        Dim CustomizableEdges3 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim DataGridViewCellStyle1 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim DataGridViewCellStyle2 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim DataGridViewCellStyle3 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim CustomizableEdges4 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges5 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges6 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges7 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges8 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges9 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges10 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges11 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        LabelTitle = New Label()
        DashboardPanel = New Guna.UI2.WinForms.Guna2Panel()
        PictureBox9 = New PictureBox()
        Guna2CirclePictureBox5 = New Guna.UI2.WinForms.Guna2CirclePictureBox()
        Guna2DataGridView1 = New Guna.UI2.WinForms.Guna2DataGridView()
        btnDiscount = New Guna.UI2.WinForms.Guna2Button()
        SortBy = New Guna.UI2.WinForms.Guna2ComboBox()
        Exportbtn = New Guna.UI2.WinForms.Guna2Button()
        lblUsername = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Guna2HtmlLabel3 = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Guna2Panel1 = New Guna.UI2.WinForms.Guna2Panel()
        PaginationControl1 = New PaginationControl()
        DashboardPanel.SuspendLayout()
        CType(PictureBox9, ComponentModel.ISupportInitialize).BeginInit()
        CType(Guna2CirclePictureBox5, ComponentModel.ISupportInitialize).BeginInit()
        CType(Guna2DataGridView1, ComponentModel.ISupportInitialize).BeginInit()
        Guna2Panel1.SuspendLayout()
        SuspendLayout()
        ' 
        ' LabelTitle
        ' 
        LabelTitle.AutoSize = True
        LabelTitle.Font = New Font("Poppins Medium", 16.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelTitle.ForeColor = Color.FromArgb(CByte(42), CByte(42), CByte(42))
        LabelTitle.Location = New Point(241, 20)
        LabelTitle.Name = "LabelTitle"
        LabelTitle.Size = New Size(286, 50)
        LabelTitle.TabIndex = 0
        LabelTitle.Text = "Staff Management"
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
        PictureBox9.ErrorImage = My.Resources.Resources.Jade_Dental_Logo
        PictureBox9.Image = My.Resources.Resources.Jade_Dental_Logo
        PictureBox9.Location = New Point(60, 3)
        PictureBox9.Name = "PictureBox9"
        PictureBox9.Size = New Size(103, 85)
        PictureBox9.SizeMode = PictureBoxSizeMode.StretchImage
        PictureBox9.TabIndex = 76
        PictureBox9.TabStop = False
        ' 
        ' Guna2CirclePictureBox5
        ' 
        Guna2CirclePictureBox5.FillColor = Color.Transparent
        Guna2CirclePictureBox5.Image = My.Resources.Resources.avatar_default_svgrepo_com
        Guna2CirclePictureBox5.ImageRotate = 0F
        Guna2CirclePictureBox5.Location = New Point(1742, 26)
        Guna2CirclePictureBox5.Name = "Guna2CirclePictureBox5"
        Guna2CirclePictureBox5.ShadowDecoration.CustomizableEdges = CustomizableEdges3
        Guna2CirclePictureBox5.ShadowDecoration.Mode = Guna.UI2.WinForms.Enums.ShadowMode.Circle
        Guna2CirclePictureBox5.Size = New Size(31, 28)
        Guna2CirclePictureBox5.TabIndex = 40
        Guna2CirclePictureBox5.TabStop = False
        ' 
        ' Guna2DataGridView1
        ' 
        Guna2DataGridView1.AllowUserToAddRows = False
        Guna2DataGridView1.AllowUserToDeleteRows = False
        Guna2DataGridView1.AllowUserToResizeColumns = False
        Guna2DataGridView1.AllowUserToResizeRows = False
        DataGridViewCellStyle1.BackColor = Color.White
        Guna2DataGridView1.AlternatingRowsDefaultCellStyle = DataGridViewCellStyle1
        Guna2DataGridView1.Anchor = AnchorStyles.Top Or AnchorStyles.Bottom Or AnchorStyles.Left Or AnchorStyles.Right
        Guna2DataGridView1.BackgroundColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        DataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle2.BackColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        DataGridViewCellStyle2.Font = New Font("Poppins", 8.5F, FontStyle.Bold)
        DataGridViewCellStyle2.ForeColor = Color.FromArgb(CByte(68), CByte(68), CByte(68))
        DataGridViewCellStyle2.SelectionBackColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        DataGridViewCellStyle2.SelectionForeColor = Color.FromArgb(CByte(68), CByte(68), CByte(68))
        DataGridViewCellStyle2.WrapMode = DataGridViewTriState.True
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle2
        Guna2DataGridView1.ColumnHeadersHeight = 44
        Guna2DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        DataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleCenter
        DataGridViewCellStyle3.BackColor = Color.White
        DataGridViewCellStyle3.Font = New Font("Poppins", 9.5F)
        DataGridViewCellStyle3.ForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        DataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(CByte(235), CByte(228), CByte(200))
        DataGridViewCellStyle3.SelectionForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        DataGridViewCellStyle3.WrapMode = DataGridViewTriState.False
        Guna2DataGridView1.DefaultCellStyle = DataGridViewCellStyle3
        Guna2DataGridView1.GridColor = Color.FromArgb(CByte(238), CByte(236), CByte(236))
        Guna2DataGridView1.Location = New Point(8, 58)
        Guna2DataGridView1.Name = "Guna2DataGridView1"
        Guna2DataGridView1.RowHeadersVisible = False
        Guna2DataGridView1.RowHeadersWidth = 51
        Guna2DataGridView1.Size = New Size(1641, 800)
        Guna2DataGridView1.TabIndex = 41
        Guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White
        Guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.Font = Nothing
        Guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty
        Guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty
        Guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty
        Guna2DataGridView1.ThemeStyle.BackColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        Guna2DataGridView1.ThemeStyle.GridColor = Color.FromArgb(CByte(238), CByte(236), CByte(236))
        Guna2DataGridView1.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        Guna2DataGridView1.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None
        Guna2DataGridView1.ThemeStyle.HeaderStyle.Font = New Font("Poppins", 8.5F, FontStyle.Bold)
        Guna2DataGridView1.ThemeStyle.HeaderStyle.ForeColor = Color.FromArgb(CByte(68), CByte(68), CByte(68))
        Guna2DataGridView1.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
        Guna2DataGridView1.ThemeStyle.HeaderStyle.Height = 44
        Guna2DataGridView1.ThemeStyle.ReadOnly = False
        Guna2DataGridView1.ThemeStyle.RowsStyle.BackColor = Color.White
        Guna2DataGridView1.ThemeStyle.RowsStyle.BorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        Guna2DataGridView1.ThemeStyle.RowsStyle.Font = New Font("Poppins", 9.5F)
        Guna2DataGridView1.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        Guna2DataGridView1.ThemeStyle.RowsStyle.Height = 72
        Guna2DataGridView1.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(CByte(235), CByte(228), CByte(200))
        Guna2DataGridView1.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        ' 
        ' btnDiscount
        ' 
        btnDiscount.BorderRadius = 10
        btnDiscount.CustomizableEdges = CustomizableEdges4
        btnDiscount.DisabledState.BorderColor = Color.DarkGray
        btnDiscount.DisabledState.CustomBorderColor = Color.DarkGray
        btnDiscount.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        btnDiscount.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        btnDiscount.FillColor = Color.FromArgb(CByte(191), CByte(155), CByte(48))
        btnDiscount.Font = New Font("Poppins Medium", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        btnDiscount.ForeColor = Color.White
        btnDiscount.Location = New Point(1284, 13)
        btnDiscount.Name = "btnDiscount"
        btnDiscount.ShadowDecoration.CustomizableEdges = CustomizableEdges5
        btnDiscount.Size = New Size(206, 44)
        btnDiscount.TabIndex = 64
        btnDiscount.Text = "＋ Add Staff"
        ' 
        ' SortBy
        ' 
        SortBy.BackColor = Color.Transparent
        SortBy.BorderColor = Color.FromArgb(CByte(232), CByte(232), CByte(232))
        SortBy.BorderRadius = 10
        SortBy.CustomizableEdges = CustomizableEdges6
        SortBy.DrawMode = DrawMode.OwnerDrawFixed
        SortBy.DropDownStyle = ComboBoxStyle.DropDownList
        SortBy.FocusedColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        SortBy.FocusedState.BorderColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        SortBy.Font = New Font("Poppins Light", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        SortBy.ForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        SortBy.ItemHeight = 40
        SortBy.Location = New Point(115, 11)
        SortBy.Name = "SortBy"
        SortBy.ShadowDecoration.CustomizableEdges = CustomizableEdges7
        SortBy.Size = New Size(200, 46)
        SortBy.TabIndex = 65
        ' 
        ' Exportbtn
        ' 
        Exportbtn.BorderColor = Color.Gainsboro
        Exportbtn.BorderRadius = 10
        Exportbtn.BorderThickness = 1
        Exportbtn.CustomizableEdges = CustomizableEdges8
        Exportbtn.DisabledState.BorderColor = Color.DarkGray
        Exportbtn.DisabledState.CustomBorderColor = Color.DarkGray
        Exportbtn.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        Exportbtn.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        Exportbtn.FillColor = Color.White
        Exportbtn.Font = New Font("Poppins Medium", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Exportbtn.ForeColor = Color.FromArgb(CByte(80), CByte(80), CByte(80))
        Exportbtn.Location = New Point(1505, 13)
        Exportbtn.Name = "Exportbtn"
        Exportbtn.ShadowDecoration.CustomizableEdges = CustomizableEdges9
        Exportbtn.Size = New Size(130, 42)
        Exportbtn.TabIndex = 66
        Exportbtn.Text = "📥 Export"
        ' 
        ' lblUsername
        ' 
        lblUsername.BackColor = Color.Transparent
        lblUsername.Font = New Font("Poppins Light", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblUsername.ForeColor = Color.FromArgb(CByte(59), CByte(59), CByte(59))
        lblUsername.Location = New Point(1821, 20)
        lblUsername.Name = "lblUsername"
        lblUsername.Size = New Size(65, 28)
        lblUsername.TabIndex = 74
        lblUsername.Text = "20 Items"
        ' 
        ' Guna2HtmlLabel3
        ' 
        Guna2HtmlLabel3.BackColor = Color.Transparent
        Guna2HtmlLabel3.Font = New Font("Poppins", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2HtmlLabel3.ForeColor = Color.FromArgb(CByte(42), CByte(42), CByte(42))
        Guna2HtmlLabel3.Location = New Point(22, 19)
        Guna2HtmlLabel3.Name = "Guna2HtmlLabel3"
        Guna2HtmlLabel3.Size = New Size(72, 32)
        Guna2HtmlLabel3.TabIndex = 75
        Guna2HtmlLabel3.Text = "Filter by:"
        ' 
        ' Guna2Panel1
        ' 
        Guna2Panel1.Anchor = AnchorStyles.Top Or AnchorStyles.Left Or AnchorStyles.Right
        Guna2Panel1.BackColor = Color.Transparent
        Guna2Panel1.BorderColor = Color.FromArgb(CByte(232), CByte(232), CByte(232))
        Guna2Panel1.BorderRadius = 10
        Guna2Panel1.BorderThickness = 2
        Guna2Panel1.Controls.Add(Guna2DataGridView1)
        Guna2Panel1.Controls.Add(PaginationControl1)
        Guna2Panel1.Controls.Add(SortBy)
        Guna2Panel1.Controls.Add(Guna2HtmlLabel3)
        Guna2Panel1.Controls.Add(btnDiscount)
        Guna2Panel1.Controls.Add(Exportbtn)
        Guna2Panel1.CustomizableEdges = CustomizableEdges10
        Guna2Panel1.FillColor = Color.White
        Guna2Panel1.Location = New Point(240, 113)
        Guna2Panel1.Name = "Guna2Panel1"
        Guna2Panel1.ShadowDecoration.CustomizableEdges = CustomizableEdges11
        Guna2Panel1.ShadowDecoration.Depth = 0
        Guna2Panel1.ShadowDecoration.Shadow = New Padding(0)
        Guna2Panel1.Size = New Size(1650, 908)
        Guna2Panel1.TabIndex = 80
        ' 
        ' PaginationControl1
        ' 
        PaginationControl1.BackColor = Color.White
        PaginationControl1.CurrentPage = 1
        PaginationControl1.ItemsPerPage = 8
        PaginationControl1.Location = New Point(8, 858)
        PaginationControl1.MaximumSize = New Size(0, 62)
        PaginationControl1.MinimumSize = New Size(360, 62)
        PaginationControl1.Name = "PaginationControl1"
        PaginationControl1.Size = New Size(1641, 62)
        PaginationControl1.TabIndex = 86
        PaginationControl1.TotalItems = 0
        PaginationControl1.TotalPages = 1
        ' 
        ' Staff
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        AutoScroll = True
        BackColor = Color.FromArgb(CByte(245), CByte(243), CByte(239))
        ClientSize = New Size(1902, 1033)
        Controls.Add(Guna2Panel1)
        Controls.Add(lblUsername)
        Controls.Add(Guna2CirclePictureBox5)
        Controls.Add(DashboardPanel)
        Controls.Add(LabelTitle)
        Name = "Staff"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Orders"
        DashboardPanel.ResumeLayout(False)
        CType(PictureBox9, ComponentModel.ISupportInitialize).EndInit()
        CType(Guna2CirclePictureBox5, ComponentModel.ISupportInitialize).EndInit()
        CType(Guna2DataGridView1, ComponentModel.ISupportInitialize).EndInit()
        Guna2Panel1.ResumeLayout(False)
        Guna2Panel1.PerformLayout()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents LabelTitle As Label
    Friend WithEvents DashboardPanel As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents Guna2CirclePictureBox5 As Guna.UI2.WinForms.Guna2CirclePictureBox
    Friend WithEvents Guna2DataGridView1 As Guna.UI2.WinForms.Guna2DataGridView
    Friend WithEvents btnDiscount As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents SortBy As Guna.UI2.WinForms.Guna2ComboBox
    Friend WithEvents Exportbtn As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents lblUsername As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents Guna2HtmlLabel3 As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents PictureBox9 As PictureBox
    Friend WithEvents Guna2Panel1 As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents PaginationControl1 As PaginationControl
End Class
