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
        Me.ContentPanel = New Panel()
        Me.SuspendLayout()
        '
        'ContentPanel
        '
        Me.ContentPanel.Dock = DockStyle.Fill
        Me.ContentPanel.Location = New Point(0, 0)
        Me.ContentPanel.Name = "ContentPanel"
        Me.ContentPanel.Size = New Size(1280, 720)
        Me.ContentPanel.TabIndex = 0
        '
        'MainShell
        '
        Me.AutoScaleDimensions = New SizeF(7.0!, 15.0!)
        Me.AutoScaleMode = AutoScaleMode.Font
        Me.ClientSize = New Size(1280, 720)
        Me.Controls.Add(Me.ContentPanel)
        Me.FormBorderStyle = FormBorderStyle.None
        Me.Name = "MainShell"
        Me.StartPosition = FormStartPosition.CenterScreen
        Me.Text = "Jade Clinic"
        Me.WindowState = FormWindowState.Maximized
        Me.ResumeLayout(False)
    End Sub
End Class
