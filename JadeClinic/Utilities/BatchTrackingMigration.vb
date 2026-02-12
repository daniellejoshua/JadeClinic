Imports Microsoft.Data.SqlClient

Public Class BatchTrackingMigration
    ''' <summary>
    ''' Updates existing databases to support batch tracking for ENDO products
    ''' This should be called during application startup to ensure database compatibility
    ''' </summary>
    Public Shared Sub UpdateDatabaseForBatchTracking()
        Try
            Console.WriteLine("?? Checking database for batch tracking support...")
            
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()
                
                ' Check if BatchNumber column exists in InventoryLog table
                Dim checkBatchColumnQuery As String = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS " &
                    "WHERE TABLE_NAME = 'InventoryLog' AND COLUMN_NAME = 'BatchNumber'"
                
                Dim batchColumnExists As Integer
                Using cmd As New SqlCommand(checkBatchColumnQuery, conn)
                    batchColumnExists = Convert.ToInt32(cmd.ExecuteScalar())
                End Using
                
                ' Check if ExpiryDate column exists in InventoryLog table
                Dim checkExpiryColumnQuery As String = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS " &
                    "WHERE TABLE_NAME = 'InventoryLog' AND COLUMN_NAME = 'ExpiryDate'"
                
                Dim expiryColumnExists As Integer
                Using cmd As New SqlCommand(checkExpiryColumnQuery, conn)
                    expiryColumnExists = Convert.ToInt32(cmd.ExecuteScalar())
                End Using
                
                Dim needsUpdate As Boolean = False
                
                ' Add BatchNumber column if it doesn't exist
                If batchColumnExists = 0 Then
                    Console.WriteLine("?? Adding BatchNumber column to InventoryLog table...")
                    Dim addBatchQuery As String = "ALTER TABLE InventoryLog ADD BatchNumber nvarchar(50) NULL"
                    Using cmd As New SqlCommand(addBatchQuery, conn)
                        cmd.ExecuteNonQuery()
                    End Using
                    needsUpdate = True
                End If
                
                ' Add ExpiryDate column if it doesn't exist
                If expiryColumnExists = 0 Then
                    Console.WriteLine("?? Adding ExpiryDate column to InventoryLog table...")
                    Dim addExpiryQuery As String = "ALTER TABLE InventoryLog ADD ExpiryDate date NULL"
                    Using cmd As New SqlCommand(addExpiryQuery, conn)
                        cmd.ExecuteNonQuery()
                    End Using
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
    
    Private Shared Sub CreateBatchTrackingIndexes(conn As SqlConnection)
        Try
            ' Create index for BatchNumber
            Dim batchIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryLog_Batch') " &
                "CREATE NONCLUSTERED INDEX IX_InventoryLog_Batch ON InventoryLog (BatchNumber ASC) WHERE BatchNumber IS NOT NULL"
            
            Using cmd As New SqlCommand(batchIndexQuery, conn)
                cmd.ExecuteNonQuery()
            End Using
            
            ' Create index for ExpiryDate
            Dim expiryIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryLog_Expiry') " &
                "CREATE NONCLUSTERED INDEX IX_InventoryLog_Expiry ON InventoryLog (ExpiryDate ASC) WHERE ExpiryDate IS NOT NULL"
            
            Using cmd As New SqlCommand(expiryIndexQuery, conn)
                cmd.ExecuteNonQuery()
            End Using
            
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
            Using conn As New SqlConnection(connStr)
                conn.Open()
                
                ' Find ENDO products to create sample batch data
                Dim findEndoQuery As String = "SELECT TOP 3 ProductID, ProductName FROM Products WHERE Category = 'ENDO' AND IsActive = 1"
                
                Using cmd As New SqlCommand(findEndoQuery, conn)
                    Using reader As SqlDataReader = cmd.ExecuteReader()
                        Dim endoProducts As New List(Of (ProductID As Integer, ProductName As String))
                        
                        While reader.Read()
                            endoProducts.Add((reader("ProductID"), reader("ProductName").ToString()))
                        End While
                        
                        reader.Close()
                        
                        ' Create sample batch entries for each ENDO product
                        For Each product In endoProducts
                            CreateSampleBatchEntry(conn, product.ProductID, product.ProductName)
                        Next
                    End Using
                End Using
            End Using
            
            Console.WriteLine("? Sample batch data generated successfully!")
            
        Catch ex As Exception
            Console.WriteLine($"?? Error generating sample batch data: {ex.Message}")
        End Try
    End Sub
    
    Private Shared Sub CreateSampleBatchEntry(conn As SqlConnection, productId As Integer, productName As String)
        Try
            ' Create a sample inventory log entry with batch information
            Dim batchNumber As String = $"ENDO-BATCH-{DateTime.Now:yyyyMMdd}-001"
            Dim expiryDate As DateTime = DateTime.Now.AddYears(2) ' 2 years from now
            
            Dim insertQuery As String = "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, " &
                "BatchNumber, ExpiryDate, UserID, Notes, CreatedAt) " &
                "VALUES (@ProductID, 'IN', 10, 0, 10, @BatchNumber, @ExpiryDate, 1, @Notes, GETDATE())"
            
            Using cmd As New SqlCommand(insertQuery, conn)
                cmd.Parameters.AddWithValue("@ProductID", productId)
                cmd.Parameters.AddWithValue("@BatchNumber", batchNumber)
                cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate)
                cmd.Parameters.AddWithValue("@Notes", $"Sample batch entry for {productName} - Testing batch tracking system")
                
                cmd.ExecuteNonQuery()
            End Using
            
            ' Update product stock
            Dim updateStockQuery As String = "UPDATE Products SET CurrentStock = 10 WHERE ProductID = @ProductID"
            Using cmd As New SqlCommand(updateStockQuery, conn)
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
            Using conn As New SqlConnection(connStr)
                conn.Open()
                
                Dim query As String = "SELECT il.BatchNumber, p.ProductName, p.Category, il.ExpiryDate, " &
                    "DATEDIFF(day, GETDATE(), il.ExpiryDate) AS DaysUntilExpiry, " &
                    "il.Quantity, il.CreatedAt " &
                    "FROM InventoryLog il " &
                    "INNER JOIN Products p ON il.ProductID = p.ProductID " &
                    "WHERE il.ExpiryDate IS NOT NULL " &
                    "AND il.ExpiryDate BETWEEN GETDATE() AND DATEADD(day, @DaysAhead, GETDATE()) " &
                    "ORDER BY il.ExpiryDate ASC"
                
                Using cmd As New SqlCommand(query, conn)
                    cmd.Parameters.AddWithValue("@DaysAhead", daysAhead)
                    
                    Dim adapter As New SqlDataAdapter(cmd)
                    Dim table As New DataTable()
                    adapter.Fill(table)
                    
                    Return table
                End Using
            End Using
            
        Catch ex As Exception
            Console.WriteLine($"?? Error getting expiring batches: {ex.Message}")
            Return New DataTable() ' Return empty table on error
        End Try
    End Function
End Class