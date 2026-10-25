<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class TerminalSetup
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

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        LabelTitle = New Label()
        lblSubtitle = New Label()
        btnBack = New Guna.UI2.WinForms.Guna2Button()
        NavigationPanel = New Guna.UI2.WinForms.Guna2Panel()
        PictureBox9 = New PictureBox()
        Guna2Panel1 = New Guna.UI2.WinForms.Guna2Panel()
        TermsGrid = New Guna.UI2.WinForms.Guna2DataGridView()
        lblInfo = New Label()
        btnRefresh = New Guna.UI2.WinForms.Guna2Button()
        NavigationPanel.SuspendLayout()
        CType(PictureBox9, ComponentModel.ISupportInitialize).BeginInit()
        Guna2Panel1.SuspendLayout()
        CType(TermsGrid, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        '
        'LabelTitle
        '
        LabelTitle.AutoSize = True
        LabelTitle.Font = New Font("Poppins Medium", 16.2F, FontStyle.Regular, GraphicsUnit.Point, CByte(0))
        LabelTitle.ForeColor = Color.FromArgb(CByte(42), CByte(42), CByte(42))
        LabelTitle.Location = New Point(310, 20)
        LabelTitle.Name = "LabelTitle"
        LabelTitle.Size = New Size(196, 50)
        LabelTitle.TabIndex = 0
        LabelTitle.Text = "PC Terminals"
        '
        'lblSubtitle
        '
        lblSubtitle.AutoSize = True
        lblSubtitle.Font = New Font("Poppins", 9.5F)
        lblSubtitle.ForeColor = Color.FromArgb(CByte(119), CByte(119), CByte(119))
        lblSubtitle.Location = New Point(312, 68)
        lblSubtitle.Name = "lblSubtitle"
        lblSubtitle.Size = New Size(560, 28)
        lblSubtitle.TabIndex = 1
        lblSubtitle.Text = "Name the computers connected to the clinic system and control their access"
        '
        'btnBack
        '
        btnBack.BorderColor = Color.FromArgb(CByte(220), CByte(220), CByte(220))
        btnBack.BorderRadius = 10
        btnBack.BorderThickness = 1
        btnBack.FillColor = Color.White
        btnBack.Font = New Font("Segoe UI", 18F, FontStyle.Regular)
        btnBack.ForeColor = Color.FromArgb(CByte(61), CByte(65), CByte(66))
        btnBack.Location = New Point(246, 24)
        btnBack.Name = "btnBack"
        btnBack.ShadowDecoration.Depth = 0
        btnBack.ShadowDecoration.Enabled = False
        btnBack.Size = New Size(48, 44)
        btnBack.TabIndex = 2
        btnBack.Text = "<"
        btnBack.TextOffset = New Point(0, -2)
        '
        'NavigationPanel
        '
        NavigationPanel.BorderColor = Color.FromArgb(CByte(232), CByte(232), CByte(232))
        NavigationPanel.BorderRadius = 10
        NavigationPanel.BorderThickness = 2
        NavigationPanel.Controls.Add(PictureBox9)
        NavigationPanel.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        NavigationPanel.FillColor = Color.White
        NavigationPanel.Location = New Point(-10, 5)
        NavigationPanel.Name = "NavigationPanel"
        NavigationPanel.ShadowDecoration.Depth = 0
        NavigationPanel.ShadowDecoration.Enabled = False
        NavigationPanel.Size = New Size(236, 1016)
        NavigationPanel.TabIndex = 6
        '
        'PictureBox9
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
        'Guna2Panel1
        '
        Guna2Panel1.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        Guna2Panel1.AutoScroll = True
        Guna2Panel1.BackColor = Color.Transparent
        Guna2Panel1.BorderColor = Color.FromArgb(CByte(232), CByte(232), CByte(232))
        Guna2Panel1.BorderRadius = 10
        Guna2Panel1.BorderThickness = 2
        Guna2Panel1.Controls.Add(TermsGrid)
        Guna2Panel1.Controls.Add(lblInfo)
        Guna2Panel1.Controls.Add(btnRefresh)
        Guna2Panel1.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        Guna2Panel1.FillColor = Color.White
        Guna2Panel1.Location = New Point(240, 113)
        Guna2Panel1.Name = "Guna2Panel1"
        Guna2Panel1.ShadowDecoration.Depth = 0
        Guna2Panel1.ShadowDecoration.Enabled = False
        Guna2Panel1.Size = New Size(1650, 908)
        Guna2Panel1.TabIndex = 2
        '
        'TermsGrid
        '
        TermsGrid.AllowUserToAddRows = False
        TermsGrid.AllowUserToDeleteRows = False
        TermsGrid.AllowUserToResizeColumns = False
        TermsGrid.AllowUserToResizeRows = False
        TermsGrid.Anchor = AnchorStyles.Top Or AnchorStyles.Left
        TermsGrid.BackgroundColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        TermsGrid.BorderStyle = System.Windows.Forms.BorderStyle.None
        TermsGrid.ColumnHeadersHeight = 44
        TermsGrid.GridColor = Color.FromArgb(CByte(220), CByte(220), CByte(220))
        TermsGrid.Location = New Point(8, 72)
        TermsGrid.Name = "TermsGrid"
        TermsGrid.RowHeadersVisible = False
        TermsGrid.RowTemplate.Height = 50
        TermsGrid.Size = New Size(1641, 801)
        TermsGrid.TabIndex = 41
        TermsGrid.ThemeStyle.BackColor = Color.White
        TermsGrid.ThemeStyle.HeaderStyle.BackColor = Color.FromArgb(CByte(250), CByte(249), CByte(246))
        TermsGrid.ThemeStyle.HeaderStyle.ForeColor = Color.FromArgb(CByte(68), CByte(68), CByte(68))
        TermsGrid.ThemeStyle.ReadOnly = True
        TermsGrid.ThemeStyle.RowsStyle.BackColor = Color.White
        TermsGrid.ThemeStyle.RowsStyle.ForeColor = Color.FromArgb(CByte(51), CByte(51), CByte(51))
        TermsGrid.ThemeStyle.RowsStyle.Height = 50
        '
        'lblInfo
        '
        lblInfo.AutoSize = True
        lblInfo.Font = New Font("Poppins", 10F, FontStyle.Bold)
        lblInfo.ForeColor = Color.FromArgb(CByte(190), CByte(154), CByte(48))
        lblInfo.Location = New Point(30, 26)
        lblInfo.Name = "lblInfo"
        lblInfo.Size = New Size(300, 28)
        lblInfo.TabIndex = 40
        lblInfo.Text = "Registered computers"
        '
        'btnRefresh
        '
        btnRefresh.BorderRadius = 8
        btnRefresh.CustomizableEdges = New Guna.UI2.WinForms.Suite.CustomizableEdges()
        btnRefresh.FillColor = Color.FromArgb(CByte(190), CByte(154), CByte(48))
        btnRefresh.Font = New Font("Poppins", 9F)
        btnRefresh.ForeColor = Color.White
        btnRefresh.Location = New Point(1458, 14)
        btnRefresh.Name = "btnRefresh"
        btnRefresh.ShadowDecoration.Depth = 0
        btnRefresh.ShadowDecoration.Enabled = False
        btnRefresh.Size = New Size(182, 42)
        btnRefresh.TabIndex = 66
        btnRefresh.Text = "Refresh"
        '
        'TerminalSetup
        '
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        AutoScroll = True
        BackColor = Color.FromArgb(CByte(248), CByte(248), CByte(247))
        ClientSize = New Size(1900, 1080)
        Controls.Add(lblSubtitle)
        Controls.Add(btnBack)
        Controls.Add(NavigationPanel)
        Controls.Add(LabelTitle)
        Controls.Add(Guna2Panel1)
        Name = "TerminalSetup"
        SizeGripStyle = SizeGripStyle.Hide
        StartPosition = FormStartPosition.CenterScreen
        Text = "PC Terminals - Jade Clinic"
        NavigationPanel.ResumeLayout(False)
        CType(PictureBox9, ComponentModel.ISupportInitialize).EndInit()
        Guna2Panel1.ResumeLayout(False)
        Guna2Panel1.PerformLayout()
        CType(TermsGrid, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents LabelTitle As Label
    Friend WithEvents lblSubtitle As Label
    Friend WithEvents btnBack As Guna.UI2.WinForms.Guna2Button
    Friend WithEvents NavigationPanel As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents PictureBox9 As PictureBox
    Friend WithEvents Guna2Panel1 As Guna.UI2.WinForms.Guna2Panel
    Friend WithEvents TermsGrid As Guna.UI2.WinForms.Guna2DataGridView
    Friend WithEvents lblInfo As Label
    Friend WithEvents btnRefresh As Guna.UI2.WinForms.Guna2Button
End Class
