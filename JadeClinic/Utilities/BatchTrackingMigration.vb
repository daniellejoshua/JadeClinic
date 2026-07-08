Imports System.Data.Common
Imports Microsoft.Data.Sqlite

Public Class BatchTrackingMigration
    ''' <summary>
    ''' Updates existing databases to support batch tracking for ENDO products
    ''' This should be called during application startup to ensure database compatibility
    ''' </summary>
    Public Shared Sub UpdateDatabaseForBatchTracking()
        Try
            Console.WriteLine("?? Checking database for batch tracking support...")
            
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As DbConnection = DbProvider.CreateConnection(connStr)
                conn.Open()

                ' Check if BatchNumber column exists in InventoryLog table (SQLite uses pragma_table_info)
                Dim checkBatchColumnQuery As String = "SELECT COUNT(*) FROM pragma_table_info('InventoryLog') WHERE name = 'BatchNumber'"

                Dim batchColumnExists As Integer = Convert.ToInt32(DatabaseHelper.ExecuteScalar(checkBatchColumnQuery, Nothing))

                ' Check if ExpiryDate column exists in InventoryLog table
                Dim checkExpiryColumnQuery As String = "SELECT COUNT(*) FROM pragma_table_info('InventoryLog') WHERE name = 'ExpiryDate'"

                Dim expiryColumnExists As Integer = Convert.ToInt32(DatabaseHelper.ExecuteScalar(checkExpiryColumnQuery, Nothing))

                Dim needsUpdate As Boolean = False

                ' Add BatchNumber column if it doesn't exist
                If batchColumnExists = 0 Then
                    Console.WriteLine("?? Adding BatchNumber column to InventoryLog table...")
                    Dim addBatchQuery As String = "ALTER TABLE InventoryLog ADD COLUMN BatchNumber TEXT NULL"
                    DatabaseHelper.ExecuteNonQuery(addBatchQuery, Nothing)
                    needsUpdate = True
                End If

                ' Add ExpiryDate column if it doesn't exist
                If expiryColumnExists = 0 Then
                    Console.WriteLine("?? Adding ExpiryDate column to InventoryLog table...")
                    Dim addExpiryQuery As String = "ALTER TABLE InventoryLog ADD COLUMN ExpiryDate TEXT NULL"
                    DatabaseHelper.ExecuteNonQuery(addExpiryQuery, Nothing)
                    needsUpdate = True
                End If

                ' Create indexes if columns were added
                If needsUpdate Then
                    CreateBatchTrackingIndexes(conn)
                    Console.WriteLine("? Database updated successfully for batch tracking!")
                Else
                    Console.WriteLine("? Database already supports batch tracking!")
                End If

            End Using
            
        Catch ex As Exception
            Console.WriteLine($"?? Warning: Could not update database for batch tracking: {ex.Message}")
            ' Don't throw the error - the application should still work without batch tracking
        End Try
    End Sub
    
    Private Shared Sub CreateBatchTrackingIndexes(conn As DbConnection)
        Try
            ' Create index for BatchNumber
            Dim batchIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_InventoryLog_Batch ON InventoryLog (BatchNumber)"
            DatabaseHelper.ExecuteNonQuery(batchIndexQuery, Nothing)
            
            ' Create index for ExpiryDate
            Dim expiryIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_InventoryLog_Expiry ON InventoryLog (ExpiryDate)"
            DatabaseHelper.ExecuteNonQuery(expiryIndexQuery, Nothing)
            
            Console.WriteLine("?? Created batch tracking indexes successfully!")
            
        Catch ex As Exception
            Console.WriteLine($"?? Warning: Could not create batch tracking indexes: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Generates sample batch data for testing purposes
    ''' This method is for development/testing only
    ''' </summary>
    Public Shared Sub GenerateSampleBatchData()
        Try
            Console.WriteLine("?? Generating sample batch data for testing...")
            
            Dim connStr As String = Connection.GetConnectionString()
            ' Use DatabaseHelper and provider-agnostic readers
            Dim findEndoQuery As String = "SELECT ProductID, ProductName FROM Products WHERE Category = 'ENDO' AND IsActive = 1 LIMIT 3"
            Using reader As DbDataReader = DatabaseHelper.ExecuteReader(findEndoQuery, Nothing)
                Dim endoProducts As New List(Of (ProductID As Integer, ProductName As String))
                While reader.Read()
                    endoProducts.Add((Convert.ToInt32(reader("ProductID")), reader("ProductName").ToString()))
                End While
                reader.Close()

                ' Create sample batch entries for each ENDO product
                For Each product In endoProducts
                    ' open a connection per entry to reuse existing helper methods
                    Dim insertQuery As String = "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, BatchNumber, ExpiryDate, UserID, Notes, CreatedAt) VALUES (@ProductID, 'IN', 10, 0, 10, @BatchNumber, @ExpiryDate, 1, @Notes, datetime('now'))"
                    Dim parameters() As SqlParameter = {
                        New SqlParameter("@ProductID", product.ProductID),
                        New SqlParameter("@BatchNumber", $"ENDO-BATCH-{DateTime.Now:yyyyMMdd}-001"),
                        New SqlParameter("@ExpiryDate", DateTime.Now.AddYears(2).ToString("yyyy-MM-dd")),
                        New SqlParameter("@Notes", $"Sample batch entry for {product.ProductName} - Testing batch tracking system")
                    }
                    DatabaseHelper.ExecuteNonQuery(insertQuery, parameters)

                    Dim updateStockQuery As String = "UPDATE Products SET CurrentStock = 10 WHERE ProductID = @ProductID"
                    Dim updateParams() As SqlParameter = {New SqlParameter("@ProductID", product.ProductID)}
                    DatabaseHelper.ExecuteNonQuery(updateStockQuery, updateParams)
                Next
            End Using
            
            Console.WriteLine("? Sample batch data generated successfully!")
            
        Catch ex As Exception
            Console.WriteLine($"?? Error generating sample batch data: {ex.Message}")
        End Try
    End Sub
    
    Private Shared Sub CreateSampleBatchEntry(conn As SqliteConnection, productId As Integer, productName As String)
        Try
            ' Create a sample inventory log entry with batch information
            Dim batchNumber As String = $"ENDO-BATCH-{DateTime.Now:yyyyMMdd}-001"
            Dim expiryDate As DateTime = DateTime.Now.AddYears(2) ' 2 years from now
            
            Dim insertQuery As String = "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, " &
                "BatchNumber, ExpiryDate, UserID, Notes, CreatedAt) " &
                "VALUES (@ProductID, 'IN', 10, 0, 10, @BatchNumber, @ExpiryDate, 1, @Notes, datetime('now'))"
            
            Using cmd As New SqliteCommand(insertQuery, conn)
                cmd.Parameters.AddWithValue("@ProductID", productId)
                cmd.Parameters.AddWithValue("@BatchNumber", batchNumber)
                cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate)
                cmd.Parameters.AddWithValue("@Notes", $"Sample batch entry for {productName} - Testing batch tracking system")
                
                cmd.ExecuteNonQuery()
            End Using
            
            ' Update product stock
            Dim updateStockQuery As String = "UPDATE Products SET CurrentStock = 10 WHERE ProductID = @ProductID"
            Using cmd As New SqliteCommand(updateStockQuery, conn)
                cmd.Parameters.AddWithValue("@ProductID", productId)
                cmd.ExecuteNonQuery()
            End Using
            
            Console.WriteLine($"?? Created sample batch for {productName}: {batchNumber}")
            
        Catch ex As Exception
            Console.WriteLine($"?? Error creating sample batch for product {productId}: {ex.Message}")
        End Try
    End Sub
    
    ''' <summary>
    ''' Gets batch information for expiry tracking reports
    ''' </summary>
    Public Shared Function GetExpiringBatches(daysAhead As Integer) As DataTable
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As DbConnection = DbProvider.CreateConnection(connStr)
                conn.Open()

                Dim query As String = "SELECT il.BatchNumber, p.ProductName, p.Category, il.ExpiryDate, " &
                    "CAST(julianday(il.ExpiryDate) - julianday('now') AS INTEGER) AS DaysUntilExpiry, " &
                    "il.Quantity, il.CreatedAt " &
                    "FROM InventoryLog il " &
                    "INNER JOIN Products p ON il.ProductID = p.ProductID " &
                    "WHERE il.ExpiryDate IS NOT NULL " &
                    "AND il.ExpiryDate BETWEEN datetime('now') AND datetime('now', '+' || @DaysAhead || ' days') " &
                    "ORDER BY il.ExpiryDate ASC"

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = query
                    Dim param As DbParameter = cmd.CreateParameter()
                    param.ParameterName = "@DaysAhead"
                    param.Value = daysAhead
                    cmd.Parameters.Add(param)

                    Dim table As New DataTable()
                    Using reader As DbDataReader = cmd.ExecuteReader()
                        table.Load(reader)
                    End Using

                    Return table
                End Using
            End Using

        Catch ex As Exception
            Console.WriteLine($"?? Error getting expiring batches: {ex.Message}")
            Return New DataTable() ' Return empty table on error
        End Try
    End Function
End Class