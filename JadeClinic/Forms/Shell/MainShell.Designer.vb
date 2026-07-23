<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainShell
    Inherits Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer
    Friend WithEvents ContentPanel As Panel

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        ContentPanel = New Panel()
        SuspendLayout()
        ' 
        ' ContentPanel
        ' 
        ContentPanel.Dock = DockStyle.Fill
        ContentPanel.Location = New Point(0, 0)
        ContentPanel.Margin = New Padding(3, 4, 3, 4)
        ContentPanel.Name = "ContentPanel"
        ContentPanel.Size = New Size(1463, 960)
        ContentPanel.TabIndex = 0
        ' 
        ' MainShell
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(1463, 960)
        Controls.Add(ContentPanel)
        FormBorderStyle = FormBorderStyle.None
        MinimumSize = New Size(1024, 600)
        Margin = New Padding(3, 4, 3, 4)
        Name = "MainShell"
        StartPosition = FormStartPosition.CenterScreen
        Text = "Jade Clinic"
        WindowState = FormWindowState.Maximized
        ResumeLayout(False)
    End Sub
End Class
