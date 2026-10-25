Imports Npgsql

' Versioned, additive schema for returns, exchanges, settlements, and quarantine.
Public Class ReturnMigration

    Public Shared Sub EnsureTables()
        Dim connStr As String = Connection.GetConnectionString()
        Using conn As New NpgsqlConnection(connStr)
            conn.Open()
            Using tran As NpgsqlTransaction = conn.BeginTransaction()
                Try
                    Execute(conn, tran,
                        "CREATE TABLE IF NOT EXISTS SalesReturns (" &
                        "ReturnID BIGSERIAL PRIMARY KEY, ReturnNumber TEXT NULL, SaleID INTEGER NOT NULL, " &
                        "ReturnDate TIMESTAMPTZ NOT NULL DEFAULT now(), ReturnType TEXT NOT NULL, Reason TEXT NULL, " &
                        "RefundAmount DOUBLE PRECISION NOT NULL DEFAULT 0, ApprovedBy TEXT NULL, ProcessedBy INTEGER NULL, " &
                        "Notes TEXT NULL, Status TEXT NOT NULL DEFAULT 'Completed', " &
                        "FOREIGN KEY (SaleID) REFERENCES Sales(SaleID))")

                    Execute(conn, tran,
                        "CREATE TABLE IF NOT EXISTS SalesReturnItems (" &
                        "ReturnItemID BIGSERIAL PRIMARY KEY, ReturnID INTEGER NOT NULL, OriginalSaleItemID INTEGER NULL, " &
                        "ProductID INTEGER NOT NULL, Quantity INTEGER NOT NULL, UnitPrice DOUBLE PRECISION NOT NULL DEFAULT 0, " &
                        "Condition TEXT NOT NULL DEFAULT 'Good', Disposition TEXT NOT NULL DEFAULT 'Quarantine', " &
                        "LineRefundAmount DOUBLE PRECISION NOT NULL DEFAULT 0, ReplacementProductID INTEGER NULL, " &
                        "ReplacementQuantity INTEGER NULL, ReplacementUnitPrice DOUBLE PRECISION NULL, " &
                        "FOREIGN KEY (ReturnID) REFERENCES SalesReturns(ReturnID))")

                    Execute(conn, tran, "ALTER TABLE SalesReturns ADD COLUMN IF NOT EXISTS ApprovedByUserID INTEGER NULL")
                    Execute(conn, tran, "ALTER TABLE SalesReturns ADD COLUMN IF NOT EXISTS InitiatedBy INTEGER NULL")
                    Execute(conn, tran, "ALTER TABLE SalesReturns ADD COLUMN IF NOT EXISTS AdditionalPaymentAmount NUMERIC(18,2) NOT NULL DEFAULT 0")
                    Execute(conn, tran, "ALTER TABLE SalesReturns ADD COLUMN IF NOT EXISTS SettlementMethod TEXT NULL")
                    Execute(conn, tran, "ALTER TABLE SalesReturns ADD COLUMN IF NOT EXISTS SettlementReference TEXT NULL")
                    Execute(conn, tran, "ALTER TABLE SaleItems ADD COLUMN IF NOT EXISTS LineDiscountAmount DOUBLE PRECISION NOT NULL DEFAULT 0")
                    Execute(conn, tran, "ALTER TABLE SaleItems ADD COLUMN IF NOT EXISTS SubTotal DOUBLE PRECISION NULL")
                    Execute(conn, tran, "ALTER TABLE SalesReturnItems ADD COLUMN IF NOT EXISTS ReturnCreditAmount NUMERIC(18,2) NOT NULL DEFAULT 0")

                    ' Older checkout versions stored only the sale-level discount. The POS
                    ' rule assigns that discount to the highest-value line, so backfill the
                    ' same allocation before any refundable amount is calculated.
                    Execute(conn, tran,
                        "WITH discounted_sales AS (" &
                        " SELECT s.SaleID, LEAST(s.DiscountAmount, ranked.GrossAmount) AS DiscountAmount, ranked.SaleItemID" &
                        " FROM Sales s" &
                        " CROSS JOIN LATERAL (SELECT si.SaleItemID, (si.UnitPrice * si.Quantity) AS GrossAmount" &
                        "   FROM SaleItems si WHERE si.SaleID = s.SaleID ORDER BY (si.UnitPrice * si.Quantity) DESC, si.SaleItemID LIMIT 1) ranked" &
                        " WHERE COALESCE(s.DiscountAmount,0) > 0" &
                        " AND NOT EXISTS (SELECT 1 FROM SaleItems existing WHERE existing.SaleID = s.SaleID AND COALESCE(existing.LineDiscountAmount,0) > 0)" &
                        ") UPDATE SaleItems si SET LineDiscountAmount = ds.DiscountAmount," &
                        " SubTotal = GREATEST(0, (si.UnitPrice * si.Quantity) - ds.DiscountAmount)" &
                        " FROM discounted_sales ds WHERE si.SaleItemID = ds.SaleItemID")

                    Execute(conn, tran,
                        "UPDATE SalesReturnItems sri SET ReturnCreditAmount = ROUND((" &
                        " GREATEST(0, (si.UnitPrice * si.Quantity) - COALESCE(si.LineDiscountAmount,0)) / NULLIF(si.Quantity,0)" &
                        ")::numeric * sri.Quantity, 2)" &
                        " FROM SaleItems si WHERE sri.OriginalSaleItemID = si.SaleItemID AND sri.ReturnCreditAmount = 0")

                    Execute(conn, tran,
                        "CREATE TABLE IF NOT EXISTS ReturnQuarantine (" &
                        "QuarantineID BIGSERIAL PRIMARY KEY, ReturnItemID INTEGER NOT NULL UNIQUE, ProductID INTEGER NOT NULL, " &
                        "Quantity INTEGER NOT NULL, ItemCondition TEXT NOT NULL, Status TEXT NOT NULL DEFAULT 'Pending', " &
                        "ReceivedAt TIMESTAMPTZ NOT NULL DEFAULT now(), ResolvedAt TIMESTAMPTZ NULL, ResolvedBy INTEGER NULL, Notes TEXT NULL, " &
                        "FOREIGN KEY (ReturnItemID) REFERENCES SalesReturnItems(ReturnItemID), " &
                        "FOREIGN KEY (ProductID) REFERENCES Products(ProductID))")

                    Execute(conn, tran, "CREATE INDEX IF NOT EXISTS IX_SalesReturns_Sale ON SalesReturns (SaleID)")
                    Execute(conn, tran, "CREATE INDEX IF NOT EXISTS IX_SalesReturns_Date ON SalesReturns (ReturnDate)")
                    Execute(conn, tran, "CREATE UNIQUE INDEX IF NOT EXISTS UX_SalesReturns_Number ON SalesReturns (ReturnNumber) WHERE ReturnNumber IS NOT NULL")
                    Execute(conn, tran, "CREATE INDEX IF NOT EXISTS IX_SalesReturnItems_Return ON SalesReturnItems (ReturnID)")
                    Execute(conn, tran, "CREATE INDEX IF NOT EXISTS IX_SalesReturnItems_Original ON SalesReturnItems (OriginalSaleItemID)")
                    Execute(conn, tran, "CREATE INDEX IF NOT EXISTS IX_ReturnQuarantine_Status ON ReturnQuarantine (Status, ProductID)")

                    AddConstraint(conn, tran, "SalesReturns", "CK_SalesReturns_Type", "ReturnType IN ('Refund','Exchange','Mixed')")
                    AddConstraint(conn, tran, "SalesReturns", "CK_SalesReturns_Status", "Status IN ('Completed','Cancelled','Reversed')")
                    AddConstraint(conn, tran, "SalesReturns", "CK_SalesReturns_Amounts", "RefundAmount >= 0 AND AdditionalPaymentAmount >= 0")
                    AddConstraint(conn, tran, "SalesReturnItems", "CK_SalesReturnItems_Quantity", "Quantity > 0")
                    AddConstraint(conn, tran, "SalesReturnItems", "CK_SalesReturnItems_Condition", "Condition IN ('Good','Damaged')")
                    ReplaceConstraint(conn, tran, "SalesReturnItems", "CK_SalesReturnItems_Disposition", "Disposition IN ('Quarantine','Restock','WriteOff','ReturnToVendor')")
                    AddConstraint(conn, tran, "SalesReturnItems", "CK_SalesReturnItems_Amounts", "LineRefundAmount >= 0 AND ReturnCreditAmount >= LineRefundAmount")
                    AddConstraint(conn, tran, "SalesReturnItems", "CK_SalesReturnItems_Damaged", "Condition <> 'Damaged' OR LineRefundAmount = 0")
                    AddConstraint(conn, tran, "SalesReturnItems", "CK_SalesReturnItems_Replacement", "(ReplacementProductID IS NULL AND ReplacementQuantity IS NULL) OR (ReplacementProductID IS NOT NULL AND ReplacementQuantity > 0 AND ReplacementUnitPrice >= 0)")
                    AddConstraint(conn, tran, "ReturnQuarantine", "CK_ReturnQuarantine_Quantity", "Quantity > 0")
                    AddConstraint(conn, tran, "ReturnQuarantine", "CK_ReturnQuarantine_Status", "Status IN ('Pending','Restocked','WrittenOff','ReturnedToVendor')")

                    AddForeignKey(conn, tran, "SalesReturns", "FK_SalesReturns_ApprovedByUser", "ApprovedByUserID", "Users", "UserID")
                    AddForeignKey(conn, tran, "SalesReturns", "FK_SalesReturns_InitiatedBy", "InitiatedBy", "Users", "UserID")
                    AddForeignKey(conn, tran, "SalesReturns", "FK_SalesReturns_ProcessedBy", "ProcessedBy", "Users", "UserID")
                    AddForeignKey(conn, tran, "SalesReturnItems", "FK_SalesReturnItems_Original", "OriginalSaleItemID", "SaleItems", "SaleItemID")
                    AddForeignKey(conn, tran, "SalesReturnItems", "FK_SalesReturnItems_Product", "ProductID", "Products", "ProductID")
                    AddForeignKey(conn, tran, "SalesReturnItems", "FK_SalesReturnItems_Replacement", "ReplacementProductID", "Products", "ProductID")

                    tran.Commit()
                    Console.WriteLine("Sales return schema migration complete.")
                Catch
                    Try
                        tran.Rollback()
                    Catch rollbackEx As Exception
                        Console.WriteLine($"Return migration rollback failed: {rollbackEx.Message}")
                    End Try
                    Throw
                End Try
            End Using
        End Using
    End Sub

    Private Shared Sub Execute(conn As NpgsqlConnection, tran As NpgsqlTransaction, sql As String)
        Using cmd As New NpgsqlCommand(sql, conn, tran)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Shared Sub AddConstraint(conn As NpgsqlConnection, tran As NpgsqlTransaction,
                                     tableName As String, constraintName As String, expression As String)
        Dim sql As String =
            $"DO $body$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = '{constraintName.ToLowerInvariant()}') THEN " &
            $"ALTER TABLE {tableName} ADD CONSTRAINT {constraintName} CHECK ({expression}) NOT VALID; END IF; END $body$;"
        Execute(conn, tran, sql)
    End Sub

    Private Shared Sub AddForeignKey(conn As NpgsqlConnection, tran As NpgsqlTransaction,
                                     tableName As String, constraintName As String, columnName As String,
                                     referencedTable As String, referencedColumn As String)
        Dim sql As String =
            $"DO $body$ BEGIN IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = '{constraintName.ToLowerInvariant()}') THEN " &
            $"ALTER TABLE {tableName} ADD CONSTRAINT {constraintName} FOREIGN KEY ({columnName}) REFERENCES {referencedTable}({referencedColumn}) NOT VALID; END IF; END $body$;"
        Execute(conn, tran, sql)
    End Sub

    Private Shared Sub ReplaceConstraint(conn As NpgsqlConnection, tran As NpgsqlTransaction,
                                         tableName As String, constraintName As String, expression As String)
        Execute(conn, tran, $"ALTER TABLE {tableName} DROP CONSTRAINT IF EXISTS {constraintName}")
        Execute(conn, tran, $"ALTER TABLE {tableName} ADD CONSTRAINT {constraintName} CHECK ({expression}) NOT VALID")
    End Sub
End Class
