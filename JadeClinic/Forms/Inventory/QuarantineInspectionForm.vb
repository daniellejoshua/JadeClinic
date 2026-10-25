Imports Npgsql

Public Class QuarantineInspectionForm
    Inherits Form

    Private ReadOnly _grid As New DataGridView()
    Private ReadOnly _status As New Label()
    Private ReadOnly _btnRestock As New Button()
    Private ReadOnly _btnWriteOff As New Button()
    Private ReadOnly _btnVendor As New Button()

    Public Sub New()
        Text = "Returned Goods Quarantine"
        StartPosition = FormStartPosition.CenterParent
        FormBorderStyle = FormBorderStyle.Sizable
        MinimumSize = New Size(850, 480)
        Size = New Size(980, 560)
        BackColor = Color.White
        ShowInTaskbar = False

        Dim root As New TableLayoutPanel With {.Dock = DockStyle.Fill, .RowCount = 3, .ColumnCount = 1, .Padding = New Padding(14)}
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 54))
        root.RowStyles.Add(New RowStyle(SizeType.Percent, 100))
        root.RowStyles.Add(New RowStyle(SizeType.Absolute, 52))
        Controls.Add(root)

        Dim header As New Panel With {.Dock = DockStyle.Fill}
        header.Controls.Add(New Label With {
            .Text = "Returned Goods Quarantine",
            .AutoSize = True,
            .Font = ReturnExchangeTheme.MakeFont(16, FontStyle.Bold),
            .ForeColor = ReturnExchangeTheme.PrimaryText,
            .Location = New Point(0, 0)
        })
        _status.Text = "Pending inspection"
        _status.AutoSize = True
        _status.Font = ReturnExchangeTheme.MakeFont(9)
        _status.ForeColor = ReturnExchangeTheme.SecondaryText
        _status.Location = New Point(2, 31)
        header.Controls.Add(_status)
        root.Controls.Add(header, 0, 0)

        ConfigureGrid()
        root.Controls.Add(_grid, 0, 1)

        Dim footer As New FlowLayoutPanel With {.Dock = DockStyle.Fill, .FlowDirection = FlowDirection.RightToLeft, .WrapContents = False, .Padding = New Padding(0, 8, 0, 0)}
        ConfigureButton(_btnRestock, "Approve Restock", Color.FromArgb(16, 160, 90), AddressOf RestockSelected)
        ConfigureButton(_btnWriteOff, "Write Off", Color.FromArgb(210, 70, 70), AddressOf WriteOffSelected)
        ConfigureButton(_btnVendor, "Return to Vendor", Color.FromArgb(90, 105, 120), AddressOf ReturnToVendorSelected)
        footer.Controls.Add(_btnRestock)
        footer.Controls.Add(_btnWriteOff)
        footer.Controls.Add(_btnVendor)
        root.Controls.Add(footer, 0, 2)

        AddHandler Shown, Sub() LoadPending()
        AddHandler _grid.SelectionChanged, Sub() UpdateButtons()
    End Sub

    Private Sub ConfigureGrid()
        _grid.Dock = DockStyle.Fill
        _grid.ReadOnly = True
        _grid.AllowUserToAddRows = False
        _grid.AllowUserToDeleteRows = False
        _grid.AllowUserToResizeRows = False
        _grid.AutoGenerateColumns = False
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect
        _grid.MultiSelect = False
        _grid.RowHeadersVisible = False
        _grid.BackgroundColor = Color.White
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "QuarantineID", .Visible = False})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Reference", .HeaderText = "RETURN", .FillWeight = 14})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Product", .HeaderText = "PRODUCT", .FillWeight = 34})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Quantity", .HeaderText = "QTY", .FillWeight = 8})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Condition", .HeaderText = "CONDITION", .FillWeight = 12})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Received", .HeaderText = "RECEIVED", .FillWeight = 18})
        _grid.Columns.Add(New DataGridViewTextBoxColumn With {.Name = "Notes", .HeaderText = "NOTES", .FillWeight = 28})
    End Sub

    Private Shared Sub ConfigureButton(button As Button, text As String, fill As Color, handler As EventHandler)
        button.Text = text
        button.Size = New Size(145, 34)
        button.BackColor = fill
        button.ForeColor = Color.White
        button.FlatStyle = FlatStyle.Flat
        button.FlatAppearance.BorderSize = 0
        button.Cursor = Cursors.Hand
        button.Margin = New Padding(8, 0, 0, 0)
        AddHandler button.Click, handler
    End Sub

    Private Sub LoadPending()
        _grid.Rows.Clear()
        Const sql As String =
            "SELECT q.QuarantineID, COALESCE(sr.ReturnNumber, sr.ReturnID::text) AS ReturnNumber, " &
            "COALESCE(p.ProductName,'Unknown') AS ProductName, q.Quantity, q.ItemCondition, q.ReceivedAt, COALESCE(q.Notes,'') AS Notes " &
            "FROM ReturnQuarantine q JOIN SalesReturnItems sri ON sri.ReturnItemID = q.ReturnItemID " &
            "JOIN SalesReturns sr ON sr.ReturnID = sri.ReturnID LEFT JOIN Products p ON p.ProductID = q.ProductID " &
            "WHERE q.Status = 'Pending' ORDER BY q.ReceivedAt, q.QuarantineID"
        Using conn As New NpgsqlConnection(Connection.GetConnectionString())
            conn.Open()
            Using cmd As New NpgsqlCommand(sql, conn)
                Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                    While reader.Read()
                        _grid.Rows.Add(Convert.ToInt32(reader("QuarantineID")), Convert.ToString(reader("ReturnNumber")),
                                       Convert.ToString(reader("ProductName")), Convert.ToInt32(reader("Quantity")),
                                       Convert.ToString(reader("ItemCondition")), Convert.ToDateTime(reader("ReceivedAt")).ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                                       Convert.ToString(reader("Notes")))
                    End While
                End Using
            End Using
        End Using
        _status.Text = $"{_grid.Rows.Count} item(s) awaiting inspection"
        If _grid.Rows.Count > 0 Then _grid.Rows(0).Selected = True
        UpdateButtons()
    End Sub

    Private Sub RestockSelected(sender As Object, e As EventArgs)
        Try
            ResolveSelected("Restocked", "Restock")
        Catch ex As Exception
            MessageBox.Show("Could not restock the returned item: " & ex.Message, "Quarantine", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub WriteOffSelected(sender As Object, e As EventArgs)
        Try
            ResolveSelected("WrittenOff", "WriteOff")
        Catch ex As Exception
            MessageBox.Show("Could not write off the returned item: " & ex.Message, "Quarantine", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ReturnToVendorSelected(sender As Object, e As EventArgs)
        Try
            ResolveSelected("ReturnedToVendor", "ReturnToVendor")
        Catch ex As Exception
            MessageBox.Show("Could not return the item to the vendor: " & ex.Message, "Quarantine", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ResolveSelected(status As String, disposition As String)
        If Not Rbac.IsManagerOrAbove() Then
            MessageBox.Show("Manager access is required to resolve quarantined returns.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If
        If _grid.SelectedRows.Count = 0 Then Return
        Dim quarantineId As Integer = Convert.ToInt32(_grid.SelectedRows(0).Cells("QuarantineID").Value)
        Dim actionText As String = If(status = "Restocked", "release this item to sellable stock", If(status = "WrittenOff", "write off this item", "return this item to the vendor"))
        If MessageBox.Show($"Confirm that you want to {actionText}?", "Resolve Quarantine", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Return

        Using conn As New NpgsqlConnection(Connection.GetConnectionString())
            conn.Open()
            Using tran As NpgsqlTransaction = conn.BeginTransaction(System.Data.IsolationLevel.Serializable)
                Try
                    Dim returnItemId As Integer
                    Dim productId As Integer
                    Dim quantity As Integer
                    Dim productName As String
                    Dim returnNumber As String
                    Using cmd As New NpgsqlCommand(
                        "SELECT q.ReturnItemID, q.ProductID, q.Quantity, q.Status, COALESCE(p.ProductName,'Unknown') AS ProductName, COALESCE(sr.ReturnNumber,sr.ReturnID::text) AS ReturnNumber " &
                        "FROM ReturnQuarantine q JOIN SalesReturnItems sri ON sri.ReturnItemID = q.ReturnItemID " &
                        "JOIN SalesReturns sr ON sr.ReturnID = sri.ReturnID JOIN Products p ON p.ProductID = q.ProductID " &
                        "WHERE q.QuarantineID = @ID FOR UPDATE OF q, sri, p", conn, tran)
                        cmd.Parameters.AddWithValue("@ID", quarantineId)
                        Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                            If Not reader.Read() Then Throw New InvalidOperationException("The quarantine item no longer exists.")
                            If Convert.ToString(reader("Status")) <> "Pending" Then Throw New InvalidOperationException("This item was already resolved.")
                            returnItemId = Convert.ToInt32(reader("ReturnItemID"))
                            productId = Convert.ToInt32(reader("ProductID"))
                            quantity = Convert.ToInt32(reader("Quantity"))
                            productName = Convert.ToString(reader("ProductName"))
                            returnNumber = Convert.ToString(reader("ReturnNumber"))
                        End Using
                    End Using

                    If status = "Restocked" Then
                        Dim previousStock As Integer
                        Using cmd As New NpgsqlCommand("UPDATE Products SET CurrentStock = CurrentStock + @Qty, UpdatedAt = now() WHERE ProductID = @PID RETURNING CurrentStock - @Qty", conn, tran)
                            cmd.Parameters.AddWithValue("@Qty", quantity)
                            cmd.Parameters.AddWithValue("@PID", productId)
                            previousStock = Convert.ToInt32(cmd.ExecuteScalar())
                        End Using
                        Using cmd As New NpgsqlCommand(
                            "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, UserID, Reference, Notes, CreatedAt) " &
                            "VALUES (@PID, 'IN', @Qty, @Previous, @New, @UserID, @Reference, @Notes, now())", conn, tran)
                            cmd.Parameters.AddWithValue("@PID", productId)
                            cmd.Parameters.AddWithValue("@Qty", quantity)
                            cmd.Parameters.AddWithValue("@Previous", previousStock)
                            cmd.Parameters.AddWithValue("@New", previousStock + quantity)
                            cmd.Parameters.AddWithValue("@UserID", frmLoginvb.LoggedInUserID)
                            cmd.Parameters.AddWithValue("@Reference", $"Return {returnNumber}")
                            cmd.Parameters.AddWithValue("@Notes", $"Quarantine inspection approved restock - {productName}")
                            cmd.ExecuteNonQuery()
                        End Using
                    End If

                    Using cmd As New NpgsqlCommand("UPDATE ReturnQuarantine SET Status = @Status, ResolvedAt = now(), ResolvedBy = @UserID WHERE QuarantineID = @ID AND Status = 'Pending'", conn, tran)
                        cmd.Parameters.AddWithValue("@Status", status)
                        cmd.Parameters.AddWithValue("@UserID", frmLoginvb.LoggedInUserID)
                        cmd.Parameters.AddWithValue("@ID", quarantineId)
                        If cmd.ExecuteNonQuery() <> 1 Then Throw New InvalidOperationException("This quarantine item was resolved by another user.")
                    End Using
                    Using cmd As New NpgsqlCommand("UPDATE SalesReturnItems SET Disposition = @Disposition WHERE ReturnItemID = @ReturnItemID", conn, tran)
                        cmd.Parameters.AddWithValue("@Disposition", disposition)
                        cmd.Parameters.AddWithValue("@ReturnItemID", returnItemId)
                        cmd.ExecuteNonQuery()
                    End Using
                    Using cmd As New NpgsqlCommand("INSERT INTO AuditLog (UserID, Action, Details, ActionTime) VALUES (@UserID, 'Return Quarantine Resolved', @Details, now())", conn, tran)
                        cmd.Parameters.AddWithValue("@UserID", frmLoginvb.LoggedInUserID)
                        cmd.Parameters.AddWithValue("@Details", $"Return {returnNumber}; ProductID={productId}; Quantity={quantity}; Resolution={status}; Product={productName}")
                        cmd.ExecuteNonQuery()
                    End Using
                    tran.Commit()
                Catch
                    Try
                        tran.Rollback()
                    Catch
                    End Try
                    Throw
                End Try
            End Using
        End Using

        SyncQueue.Instance.MarkDataChanged()
        LoadPending()
    End Sub

    Private Sub UpdateButtons()
        Dim enabled As Boolean = _grid.SelectedRows.Count > 0 AndAlso Rbac.IsManagerOrAbove()
        _btnRestock.Enabled = enabled
        _btnWriteOff.Enabled = enabled
        _btnVendor.Enabled = enabled
    End Sub
End Class
