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
        LabelTitle = New Label()
        DashboardPanel = New Guna.UI2.WinForms.Guna2Panel()
        Guna2CirclePictureBox5 = New Guna.UI2.WinForms.Guna2CirclePictureBox()
        Guna2DataGridView1 = New Guna.UI2.WinForms.Guna2DataGridView()
        btnDiscount = New Guna.UI2.WinForms.Guna2Button()
        SortBy = New Guna.UI2.WinForms.Guna2ComboBox()
        Exportbtn = New Guna.UI2.WinForms.Guna2Button()
        lblUsername = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Guna2HtmlLabel3 = New Guna.UI2.WinForms.Guna2HtmlLabel()
        PictureBox9 = New PictureBox()
        DashboardPanel.SuspendLayout()
        CType(Guna2CirclePictureBox5, ComponentModel.ISupportInitialize).BeginInit()
        CType(Guna2DataGridView1, ComponentModel.ISupportInitialize).BeginInit()
        CType(PictureBox9, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' LabelTitle
        ' 
        LabelTitle.AutoSize = True
        LabelTitle.Font = New Font("Poppins Medium", 16.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelTitle.ForeColor = Color.White
        LabelTitle.Location = New Point(241, 20)
        LabelTitle.Name = "LabelTitle"
        LabelTitle.Size = New Size(286, 50)
        LabelTitle.TabIndex = 0
        LabelTitle.Text = "Staff Management"
        ' 
        ' DashboardPanel
        ' 
        DashboardPanel.BorderRadius = 30
        DashboardPanel.Controls.Add(PictureBox9)
        DashboardPanel.CustomizableEdges = CustomizableEdges1
        DashboardPanel.FillColor = Color.White
        DashboardPanel.Location = New Point(-33, 5)
        DashboardPanel.Name = "DashboardPanel"
        DashboardPanel.ShadowDecoration.CustomizableEdges = CustomizableEdges2
        DashboardPanel.Size = New Size(236, 885)
        DashboardPanel.TabIndex = 6
        ' 
        ' Guna2CirclePictureBox5
        ' 
        Guna2CirclePictureBox5.ImageRotate = 0F
        Guna2CirclePictureBox5.Location = New Point(1440, 20)
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
        Guna2DataGridView1.BackgroundColor = Color.FromArgb(CByte(61), CByte(65), CByte(65))
        DataGridViewCellStyle2.Alignment = DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle2.BackColor = Color.FromArgb(CByte(100), CByte(88), CByte(255))
        DataGridViewCellStyle2.Font = New Font("Segoe UI", 9.0F)
        DataGridViewCellStyle2.ForeColor = Color.White
        DataGridViewCellStyle2.SelectionBackColor = SystemColors.Highlight
        DataGridViewCellStyle2.SelectionForeColor = SystemColors.HighlightText
        DataGridViewCellStyle2.WrapMode = DataGridViewTriState.True
        Guna2DataGridView1.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle2
        Guna2DataGridView1.ColumnHeadersHeight = 4
        Guna2DataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing
        DataGridViewCellStyle3.Alignment = DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle3.BackColor = Color.White
        DataGridViewCellStyle3.Font = New Font("Segoe UI", 9.0F)
        DataGridViewCellStyle3.ForeColor = Color.FromArgb(CByte(71), CByte(69), CByte(94))
        DataGridViewCellStyle3.SelectionBackColor = Color.FromArgb(CByte(231), CByte(229), CByte(255))
        DataGridViewCellStyle3.SelectionForeColor = Color.FromArgb(CByte(71), CByte(69), CByte(94))
        DataGridViewCellStyle3.WrapMode = DataGridViewTriState.False
        Guna2DataGridView1.DefaultCellStyle = DataGridViewCellStyle3
        Guna2DataGridView1.GridColor = Color.FromArgb(CByte(231), CByte(229), CByte(255))
        Guna2DataGridView1.Location = New Point(235, 160)
        Guna2DataGridView1.Name = "Guna2DataGridView1"
        Guna2DataGridView1.RowHeadersVisible = False
        Guna2DataGridView1.RowHeadersWidth = 51
        Guna2DataGridView1.Size = New Size(1362, 688)
        Guna2DataGridView1.TabIndex = 41
        Guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White
        Guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.Font = Nothing
        Guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty
        Guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty
        Guna2DataGridView1.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty
        Guna2DataGridView1.ThemeStyle.BackColor = Color.FromArgb(CByte(61), CByte(65), CByte(65))
        Guna2DataGridView1.ThemeStyle.GridColor = Color.FromArgb(CByte(231), CByte(229), CByte(255))
        Guna2DataGridView1.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(CByte(100), CByte(88), CByte(255))
        Guna2DataGridView1.ThemeStyle.HeaderStyle.BorderStyle = DataGridViewHeaderBorderStyle.None
        Guna2DataGridView1.ThemeStyle.HeaderStyle.Font = New Font("Segoe UI", 9.0F)
        Guna2DataGridView1.ThemeStyle.HeaderStyle.ForeColor = Color.White
        Guna2DataGridView1.ThemeStyle.HeaderStyle.HeaightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing
        Guna2DataGridView1.ThemeStyle.HeaderStyle.Height = 4
        Guna2DataGridView1.ThemeStyle.ReadOnly = False
        Guna2DataGridView1.ThemeStyle.RowsStyle.BackColor = Color.White
        Guna2DataGridView1.ThemeStyle.RowsStyle.BorderStyle = DataGridViewCellBorderStyle.SingleHorizontal
        Guna2DataGridView1.ThemeStyle.RowsStyle.Font = New Font("Segoe UI", 9.0F)
        Guna2DataGridView1.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(CByte(71), CByte(69), CByte(94))
        Guna2DataGridView1.ThemeStyle.RowsStyle.Height = 29
        Guna2DataGridView1.ThemeStyle.RowsStyle.SelectionBackColor = Color.FromArgb(CByte(231), CByte(229), CByte(255))
        Guna2DataGridView1.ThemeStyle.RowsStyle.SelectionForeColor = Color.FromArgb(CByte(71), CByte(69), CByte(94))
        ' 
        ' btnDiscount
        ' 
        btnDiscount.BorderRadius = 10
        btnDiscount.CustomizableEdges = CustomizableEdges4
        btnDiscount.DisabledState.BorderColor = Color.DarkGray
        btnDiscount.DisabledState.CustomBorderColor = Color.DarkGray
        btnDiscount.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        btnDiscount.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        btnDiscount.FillColor = Color.FromArgb(CByte(255), CByte(204), CByte(77))
        btnDiscount.Font = New Font("Poppins Medium", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        btnDiscount.ForeColor = Color.Black
        btnDiscount.Location = New Point(1314, 100)
        btnDiscount.Name = "btnDiscount"
        btnDiscount.ShadowDecoration.CustomizableEdges = CustomizableEdges5
        btnDiscount.Size = New Size(147, 40)
        btnDiscount.TabIndex = 64
        btnDiscount.Text = "Add Staff"
        ' 
        ' SortBy
        ' 
        SortBy.BackColor = Color.Transparent
        SortBy.BorderRadius = 10
        SortBy.BorderThickness = 0
        SortBy.CustomizableEdges = CustomizableEdges6
        SortBy.DrawMode = DrawMode.OwnerDrawFixed
        SortBy.DropDownStyle = ComboBoxStyle.DropDownList
        SortBy.FillColor = Color.FromArgb(CByte(61), CByte(65), CByte(66))
        SortBy.FocusedColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        SortBy.FocusedState.BorderColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        SortBy.Font = New Font("Poppins Light", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        SortBy.ForeColor = Color.White
        SortBy.ItemHeight = 40
        SortBy.Location = New Point(330, 100)
        SortBy.Name = "SortBy"
        SortBy.ShadowDecoration.CustomizableEdges = CustomizableEdges7
        SortBy.Size = New Size(361, 46)
        SortBy.TabIndex = 65
        ' 
        ' Exportbtn
        ' 
        Exportbtn.BorderRadius = 10
        Exportbtn.CustomizableEdges = CustomizableEdges8
        Exportbtn.DisabledState.BorderColor = Color.DarkGray
        Exportbtn.DisabledState.CustomBorderColor = Color.DarkGray
        Exportbtn.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        Exportbtn.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        Exportbtn.FillColor = Color.FromArgb(CByte(255), CByte(204), CByte(77))
        Exportbtn.Font = New Font("Poppins Medium", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Exportbtn.ForeColor = Color.Black
        Exportbtn.Location = New Point(1501, 100)
        Exportbtn.Name = "Exportbtn"
        Exportbtn.ShadowDecoration.CustomizableEdges = CustomizableEdges9
        Exportbtn.Size = New Size(86, 40)
        Exportbtn.TabIndex = 66
        Exportbtn.Text = "Export"
        ' 
        ' lblUsername
        ' 
        lblUsername.BackColor = Color.Transparent
        lblUsername.Font = New Font("Poppins Light", 9.0F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblUsername.ForeColor = Color.White
        lblUsername.Location = New Point(1477, 20)
        lblUsername.Name = "lblUsername"
        lblUsername.Size = New Size(65, 28)
        lblUsername.TabIndex = 74
        lblUsername.Text = "20 Items"
        ' 
        ' Guna2HtmlLabel3
        ' 
        Guna2HtmlLabel3.BackColor = Color.Transparent
        Guna2HtmlLabel3.Font = New Font("Poppins", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2HtmlLabel3.ForeColor = Color.White
        Guna2HtmlLabel3.Location = New Point(241, 108)
        Guna2HtmlLabel3.Name = "Guna2HtmlLabel3"
        Guna2HtmlLabel3.Size = New Size(72, 32)
        Guna2HtmlLabel3.TabIndex = 75
        Guna2HtmlLabel3.Text = "Filter by:"
        ' 
        ' PictureBox9
        ' 
        PictureBox9.BackColor = Color.FromArgb(CByte(41), CByte(44), CByte(45))
        PictureBox9.ErrorImage = My.Resources.Resources.Jade_Dental_Logo
        PictureBox9.Image = My.Resources.Resources.Jade_Dental_Logo
        PictureBox9.Location = New Point(60, 3)
        PictureBox9.Name = "PictureBox9"
        PictureBox9.Size = New Size(121, 100)
        PictureBox9.SizeMode = PictureBoxSizeMode.StretchImage
        PictureBox9.TabIndex = 76
        PictureBox9.TabStop = False
        ' 
        ' Staff
        ' 
        AutoScaleDimensions = New SizeF(8.0F, 20.0F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        ClientSize = New Size(1609, 875)
        Controls.Add(Guna2HtmlLabel3)
        Controls.Add(lblUsername)
        Controls.Add(Exportbtn)
        Controls.Add(SortBy)
        Controls.Add(btnDiscount)
        Controls.Add(Guna2DataGridView1)
        Controls.Add(Guna2CirclePictureBox5)
        Controls.Add(DashboardPanel)
        Controls.Add(LabelTitle)
        Name = "Staff"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Orders"
        DashboardPanel.ResumeLayout(False)
        CType(Guna2CirclePictureBox5, ComponentModel.ISupportInitialize).EndInit()
        CType(Guna2DataGridView1, ComponentModel.ISupportInitialize).EndInit()
        CType(PictureBox9, ComponentModel.ISupportInitialize).EndInit()
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
End Class
