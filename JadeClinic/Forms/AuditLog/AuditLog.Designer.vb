<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class AuditLog
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
        Dim CustomizableEdges12 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges13 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges14 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim DataGridViewCellStyle4 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim DataGridViewCellStyle5 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim DataGridViewCellStyle6 As DataGridViewCellStyle = New DataGridViewCellStyle()
        Dim CustomizableEdges15 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges16 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges17 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges18 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges19 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges20 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges21 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Dim CustomizableEdges22 As Guna.UI2.WinForms.Suite.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        LabelTitle = New Label()
        DashboardPanel = New Guna.UI2.WinForms.Guna2Panel()
        PictureBox9 = New PictureBox()
        Guna2CirclePictureBox5 = New Guna.UI2.WinForms.Guna2CirclePictureBox()
        InventoryLogDataGrid = New Guna.UI2.WinForms.Guna2DataGridView()
        Exportbtn = New Guna.UI2.WinForms.Guna2Button()
        lblUsername = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Guna2HtmlLabel4 = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Guna2DateTimePicker1 = New Guna.UI2.WinForms.Guna2DateTimePicker()
        Guna2HtmlLabel3 = New Guna.UI2.WinForms.Guna2HtmlLabel()
        Guna2HtmlLabel2 = New Guna.UI2.WinForms.Guna2HtmlLabel()
        filtertype = New Guna.UI2.WinForms.Guna2ComboBox()
        cmbAccounts = New Guna.UI2.WinForms.Guna2ComboBox()
        DashboardPanel.SuspendLayout()
        CType(PictureBox9, ComponentModel.ISupportInitialize).BeginInit()
        CType(Guna2CirclePictureBox5, ComponentModel.ISupportInitialize).BeginInit()
        CType(InventoryLogDataGrid, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' LabelTitle
        ' 
        LabelTitle.AutoSize = True
        LabelTitle.Font = New Font("Poppins Medium", 16.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelTitle.ForeColor = Color.White
        LabelTitle.Location = New Point(262, 20)
        LabelTitle.Name = "LabelTitle"
        LabelTitle.Size = New Size(153, 50)
        LabelTitle.TabIndex = 0
        LabelTitle.Text = "Audit Log"
        ' 
        ' DashboardPanel
        ' 
        DashboardPanel.BorderRadius = 30
        DashboardPanel.Controls.Add(PictureBox9)
        DashboardPanel.CustomizableEdges = CustomizableEdges12
        DashboardPanel.FillColor = Color.FromArgb(CByte(41), CByte(44), CByte(45))
        DashboardPanel.Location = New Point(-33, 5)
        DashboardPanel.Name = "DashboardPanel"
        DashboardPanel.ShadowDecoration.CustomizableEdges = CustomizableEdges13
        DashboardPanel.Size = New Size(236, 885)
        DashboardPanel.TabIndex = 6
        ' 
        ' PictureBox9
        ' 
        PictureBox9.BackColor = Color.FromArgb(CByte(61), CByte(65), CByte(66))
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
        Guna2CirclePictureBox5.ImageRotate = 0F
        Guna2CirclePictureBox5.Location = New Point(1440, 20)
        Guna2CirclePictureBox5.Name = "Guna2CirclePictureBox5"
        Guna2CirclePictureBox5.ShadowDecoration.CustomizableEdges = CustomizableEdges14
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
        DataGridViewCellStyle4.BackColor = Color.White
        InventoryLogDataGrid.AlternatingRowsDefaultCellStyle = DataGridViewCellStyle4
        InventoryLogDataGrid.BackgroundColor = Color.FromArgb(CByte(61), CByte(65), CByte(65))
        DataGridViewCellStyle5.Alignment = DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle5.BackColor = Color.FromArgb(CByte(100), CByte(88), CByte(255))
        DataGridViewCellStyle5.Font = New Font("Segoe UI", 9F)
        DataGridViewCellStyle5.ForeColor = Color.White
        DataGridViewCellStyle5.SelectionBackColor = SystemColors.Highlight
        DataGridViewCellStyle5.SelectionForeColor = SystemColors.HighlightText
        DataGridViewCellStyle5.WrapMode = DataGridViewTriState.True
        InventoryLogDataGrid.ColumnHeadersDefaultCellStyle = DataGridViewCellStyle5
        InventoryLogDataGrid.ColumnHeadersHeight = 4
        InventoryLogDataGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing
        DataGridViewCellStyle6.Alignment = DataGridViewContentAlignment.MiddleLeft
        DataGridViewCellStyle6.BackColor = Color.White
        DataGridViewCellStyle6.Font = New Font("Segoe UI", 9F)
        DataGridViewCellStyle6.ForeColor = Color.FromArgb(CByte(71), CByte(69), CByte(94))
        DataGridViewCellStyle6.SelectionBackColor = Color.FromArgb(CByte(231), CByte(229), CByte(255))
        DataGridViewCellStyle6.SelectionForeColor = Color.FromArgb(CByte(71), CByte(69), CByte(94))
        DataGridViewCellStyle6.WrapMode = DataGridViewTriState.False
        InventoryLogDataGrid.DefaultCellStyle = DataGridViewCellStyle6
        InventoryLogDataGrid.GridColor = Color.FromArgb(CByte(231), CByte(229), CByte(255))
        InventoryLogDataGrid.Location = New Point(235, 160)
        InventoryLogDataGrid.Name = "InventoryLogDataGrid"
        InventoryLogDataGrid.RowHeadersVisible = False
        InventoryLogDataGrid.RowHeadersWidth = 51
        InventoryLogDataGrid.Size = New Size(1362, 688)
        InventoryLogDataGrid.TabIndex = 41
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.BackColor = Color.White
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.Font = Nothing
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.ForeColor = Color.Empty
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.SelectionBackColor = Color.Empty
        InventoryLogDataGrid.ThemeStyle.AlternatingRowsStyle.SelectionForeColor = Color.Empty
        InventoryLogDataGrid.ThemeStyle.BackColor = Color.FromArgb(CByte(61), CByte(65), CByte(65))
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
        ' Exportbtn
        ' 
        Exportbtn.BorderRadius = 10
        Exportbtn.CustomizableEdges = CustomizableEdges15
        Exportbtn.DisabledState.BorderColor = Color.DarkGray
        Exportbtn.DisabledState.CustomBorderColor = Color.DarkGray
        Exportbtn.DisabledState.FillColor = Color.FromArgb(CByte(169), CByte(169), CByte(169))
        Exportbtn.DisabledState.ForeColor = Color.FromArgb(CByte(141), CByte(141), CByte(141))
        Exportbtn.FillColor = Color.FromArgb(CByte(255), CByte(204), CByte(77))
        Exportbtn.Font = New Font("Poppins Medium", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Exportbtn.ForeColor = Color.Black
        Exportbtn.Location = New Point(1463, 102)
        Exportbtn.Name = "Exportbtn"
        Exportbtn.ShadowDecoration.CustomizableEdges = CustomizableEdges16
        Exportbtn.Size = New Size(110, 40)
        Exportbtn.TabIndex = 66
        Exportbtn.Text = "Export"
        ' 
        ' lblUsername
        ' 
        lblUsername.BackColor = Color.Transparent
        lblUsername.Font = New Font("Poppins Light", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        lblUsername.ForeColor = Color.White
        lblUsername.Location = New Point(1477, 20)
        lblUsername.Name = "lblUsername"
        lblUsername.Size = New Size(65, 28)
        lblUsername.TabIndex = 75
        lblUsername.Text = "20 Items"
        ' 
        ' Guna2HtmlLabel4
        ' 
        Guna2HtmlLabel4.BackColor = Color.Transparent
        Guna2HtmlLabel4.Font = New Font("Poppins", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2HtmlLabel4.ForeColor = Color.White
        Guna2HtmlLabel4.Location = New Point(241, 110)
        Guna2HtmlLabel4.Name = "Guna2HtmlLabel4"
        Guna2HtmlLabel4.Size = New Size(116, 32)
        Guna2HtmlLabel4.TabIndex = 83
        Guna2HtmlLabel4.Text = "Filter by Date:"
        ' 
        ' Guna2DateTimePicker1
        ' 
        Guna2DateTimePicker1.BorderRadius = 10
        Guna2DateTimePicker1.Checked = True
        Guna2DateTimePicker1.CustomizableEdges = CustomizableEdges17
        Guna2DateTimePicker1.FillColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        Guna2DateTimePicker1.Font = New Font("Poppins", 9F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2DateTimePicker1.ForeColor = Color.White
        Guna2DateTimePicker1.Format = DateTimePickerFormat.Short
        Guna2DateTimePicker1.Location = New Point(363, 102)
        Guna2DateTimePicker1.MaxDate = New Date(9998, 12, 31, 0, 0, 0, 0)
        Guna2DateTimePicker1.MinDate = New Date(1753, 1, 1, 0, 0, 0, 0)
        Guna2DateTimePicker1.Name = "Guna2DateTimePicker1"
        Guna2DateTimePicker1.ShadowDecoration.CustomizableEdges = CustomizableEdges18
        Guna2DateTimePicker1.Size = New Size(148, 40)
        Guna2DateTimePicker1.TabIndex = 82
        Guna2DateTimePicker1.Value = New Date(2025, 9, 27, 23, 48, 46, 373)
        ' 
        ' Guna2HtmlLabel3
        ' 
        Guna2HtmlLabel3.BackColor = Color.Transparent
        Guna2HtmlLabel3.Font = New Font("Poppins", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2HtmlLabel3.ForeColor = Color.White
        Guna2HtmlLabel3.Location = New Point(548, 104)
        Guna2HtmlLabel3.Name = "Guna2HtmlLabel3"
        Guna2HtmlLabel3.Size = New Size(65, 32)
        Guna2HtmlLabel3.TabIndex = 81
        Guna2HtmlLabel3.Text = "Sort by:"
        ' 
        ' Guna2HtmlLabel2
        ' 
        Guna2HtmlLabel2.BackColor = Color.Transparent
        Guna2HtmlLabel2.Font = New Font("Poppins", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        Guna2HtmlLabel2.ForeColor = Color.White
        Guna2HtmlLabel2.Location = New Point(964, 104)
        Guna2HtmlLabel2.Name = "Guna2HtmlLabel2"
        Guna2HtmlLabel2.Size = New Size(129, 32)
        Guna2HtmlLabel2.TabIndex = 78
        Guna2HtmlLabel2.Text = "Filter by Group:"
        ' 
        ' filtertype
        ' 
        filtertype.BackColor = Color.Transparent
        filtertype.BorderRadius = 10
        filtertype.BorderThickness = 0
        filtertype.CustomizableEdges = CustomizableEdges19
        filtertype.DrawMode = DrawMode.OwnerDrawFixed
        filtertype.DropDownStyle = ComboBoxStyle.DropDownList
        filtertype.FillColor = Color.FromArgb(CByte(61), CByte(65), CByte(66))
        filtertype.FocusedColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        filtertype.FocusedState.BorderColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        filtertype.Font = New Font("Poppins Light", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        filtertype.ForeColor = Color.White
        filtertype.ItemHeight = 40
        filtertype.Location = New Point(1108, 96)
        filtertype.Name = "filtertype"
        filtertype.ShadowDecoration.CustomizableEdges = CustomizableEdges20
        filtertype.Size = New Size(309, 46)
        filtertype.TabIndex = 80
        ' 
        ' cmbAccounts
        ' 
        cmbAccounts.BackColor = Color.Transparent
        cmbAccounts.BorderRadius = 10
        cmbAccounts.BorderThickness = 0
        cmbAccounts.CustomizableEdges = CustomizableEdges21
        cmbAccounts.DrawMode = DrawMode.OwnerDrawFixed
        cmbAccounts.DropDownStyle = ComboBoxStyle.DropDownList
        cmbAccounts.FillColor = Color.FromArgb(CByte(61), CByte(65), CByte(66))
        cmbAccounts.FocusedColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        cmbAccounts.FocusedState.BorderColor = Color.FromArgb(CByte(94), CByte(148), CByte(255))
        cmbAccounts.Font = New Font("Poppins Light", 10.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        cmbAccounts.ForeColor = Color.White
        cmbAccounts.ItemHeight = 40
        cmbAccounts.Location = New Point(628, 96)
        cmbAccounts.Name = "cmbAccounts"
        cmbAccounts.ShadowDecoration.CustomizableEdges = CustomizableEdges22
        cmbAccounts.Size = New Size(309, 46)
        cmbAccounts.TabIndex = 79
        ' 
        ' AuditLog
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        BackColor = Color.FromArgb(CByte(30), CByte(30), CByte(30))
        ClientSize = New Size(1609, 875)
        Controls.Add(Guna2HtmlLabel4)
        Controls.Add(Guna2DateTimePicker1)
        Controls.Add(Guna2HtmlLabel3)
        Controls.Add(Guna2HtmlLabel2)
        Controls.Add(filtertype)
        Controls.Add(cmbAccounts)
        Controls.Add(lblUsername)
        Controls.Add(Exportbtn)
        Controls.Add(InventoryLogDataGrid)
        Controls.Add(Guna2CirclePictureBox5)
        Controls.Add(DashboardPanel)
        Controls.Add(LabelTitle)
        Name = "AuditLog"
        SizeGripStyle = SizeGripStyle.Hide
        StartPosition = FormStartPosition.CenterScreen
        Text = "Orders"
        DashboardPanel.ResumeLayout(False)
        CType(PictureBox9, ComponentModel.ISupportInitialize).EndInit()
        CType(Guna2CirclePictureBox5, ComponentModel.ISupportInitialize).EndInit()
        CType(InventoryLogDataGrid, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents LabelTitle As Label
    Friend WithEvents DashboardPanel As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents PictureBox9 As PictureBox
    Friend WithEvents Guna2CirclePictureBox5 As Guna.UI2.WinForms.Guna2CirclePictureBox
    Friend WithEvents InventoryLogDataGrid As Guna.UI2.WinForms.Guna2DataGridView
    Friend WithEvents Exportbtn As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents filtertype As Guna.UI2.WinForms.Guna2ComboBox
    Friend WithEvents Guna2HtmlLabel2 As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents lblUsername As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents Guna2HtmlLabel4 As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents Guna2DateTimePicker1 As Guna.UI2.WinForms.Guna2DateTimePicker
    Friend WithEvents Guna2HtmlLabel3 As Guna.UI2.WinForms.Guna2HtmlLabel
    Friend WithEvents cmbAccounts As Guna.UI2.WinForms.Guna2ComboBox
End Class
