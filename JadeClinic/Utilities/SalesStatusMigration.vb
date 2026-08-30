Imports System.Data.Common

Public Class SalesStatusMigration
    ''' <summary>
    ''' Migrates existing databases to the Sales status model:
    '''   - Adds ApprovedBy and AbortReason columns (used for Aborted sale records)
    '''   - Drops the obsolete IsVoid column (it was never written as 1; Status is now the source of truth)
    ''' Runs automatically at startup; safe to call repeatedly.
    ''' </summary>
    Public Shared Sub UpdateDatabaseForSalesStatus()
        Try
            Console.WriteLine("Checking database for Sales status columns...")

            Dim connStr As String = Connection.GetConnectionString()
            Using conn As DbConnection = DbProvider.CreateConnection(connStr)
                conn.Open()

                Dim colNames As HashSet(Of String)

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.CommandText = "SELECT name FROM pragma_table_info('Sales')"
                    colNames = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
                    Using reader As DbDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            colNames.Add(Convert.ToString(reader("name")))
                        End While
                    End Using
                End Using

                ' Add ApprovedBy if missing
                If Not colNames.Contains("ApprovedBy") Then
                    Console.WriteLine("Adding ApprovedBy column to Sales table...")
                    Using cmd As DbCommand = conn.CreateCommand()
                        cmd.CommandText = "ALTER TABLE Sales ADD COLUMN ApprovedBy TEXT NULL"
                        cmd.ExecuteNonQuery()
                    End Using
                End If

                ' Add AbortReason if missing
                If Not colNames.Contains("AbortReason") Then
                    Console.WriteLine("Adding AbortReason column to Sales table...")
                    Using cmd As DbCommand = conn.CreateCommand()
                        cmd.CommandText = "ALTER TABLE Sales ADD COLUMN AbortReason TEXT NULL"
                        cmd.ExecuteNonQuery()
                    End Using
                End If

                ' Drop obsolete IsVoid column if present (SQLite >= 3.35 supports DROP COLUMN).
                ' For older SQLite, rebuild the table without the column as a fallback.
                If colNames.Contains("IsVoid") Then
                    Console.WriteLine("Removing obsolete IsVoid column from Sales table...")
                    Try
                        Using cmd As DbCommand = conn.CreateCommand()
                            cmd.CommandText = "ALTER TABLE Sales DROP COLUMN IsVoid"
                            cmd.ExecuteNonQuery()
                        End Using
                    Catch
                        RebuildSalesWithoutIsVoid(conn)
                    End Try
                End If

                Console.WriteLine("Sales status columns migration complete.")
            End Using
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not update database for Sales status: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub RebuildSalesWithoutIsVoid(conn As DbConnection)
        ' Legacy path for SQLite builds older than 3.35: recreate the Sales table
        ' without the IsVoid column and copy existing data across.
        Using tx As DbTransaction = conn.BeginTransaction()
            Try
                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.Transaction = tx
                    cmd.CommandText = "ALTER TABLE Sales RENAME TO Sales_old"
                    cmd.ExecuteNonQuery()
                End Using

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.Transaction = tx
                    cmd.CommandText =
                        "CREATE TABLE Sales (" &
                        "SaleID INTEGER PRIMARY KEY AUTOINCREMENT, " &
                        "SaleNumber TEXT, " &
                        "SaleDate DATETIME DEFAULT (CURRENT_TIMESTAMP), " &
                        "CustomerName TEXT NULL, " &
                        "CustomerTIN TEXT NULL, " &
                        "UserID INTEGER NULL, " &
                        "TotalAmount REAL DEFAULT 0, " &
                        "AmountPaid REAL DEFAULT 0, " &
                        "PaymentMethod TEXT DEFAULT 'Cash', " &
                        "Reference TEXT NULL, " &
                        "SalesData TEXT NOT NULL, " &
                        "Status TEXT DEFAULT 'Completed', " &
                        "ApprovedBy TEXT NULL, " &
                        "AbortReason TEXT NULL, " &
                        "DiscountType TEXT NULL, " &
                        "DiscountAmount REAL NOT NULL DEFAULT 0, " &
                        "FOREIGN KEY (UserID) REFERENCES Users(UserID))"
                    cmd.ExecuteNonQuery()
                End Using

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.Transaction = tx
                    cmd.CommandText =
                        "INSERT INTO Sales (SaleID, SaleNumber, SaleDate, CustomerName, CustomerTIN, UserID, TotalAmount, " &
                        "AmountPaid, PaymentMethod, Reference, SalesData, Status, DiscountType, DiscountAmount) " &
                        "SELECT SaleID, SaleNumber, SaleDate, CustomerName, CustomerTIN, UserID, TotalAmount, " &
                        "AmountPaid, PaymentMethod, Reference, SalesData, Status, DiscountType, DiscountAmount FROM Sales_old"
                    cmd.ExecuteNonQuery()
                End Using

                Using cmd As DbCommand = conn.CreateCommand()
                    cmd.Transaction = tx
                    cmd.CommandText = "DROP TABLE Sales_old"
                    cmd.ExecuteNonQuery()
                End Using

                tx.Commit()
            Catch
                Try
                    tx.Rollback()
                Catch
                End Try
                Throw
            End Try
        End Using
    End Sub
End Class
