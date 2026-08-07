Imports System.IO
Imports System.Collections.Generic
Imports Microsoft.Data.Sqlite
Imports Newtonsoft.Json

' Wipes the demo tables and reseeds realistic dental-clinic demo data.
' Keeps the admin user, company settings, and image files intact.
Public Class DemoDataSeeder

    Public Shared Function SeedDemoData() As String
        Dim connStr As String = Connection.GetConnectionString()
        If String.IsNullOrEmpty(connStr) Then
            Throw New Exception("Database connection string is not configured.")
        End If

        Using conn As New SqliteConnection(connStr)
            conn.Open()
            Using tran As SqliteTransaction = conn.BeginTransaction()
                Try
                    WipeDemoTables(conn, tran)
                    ResetSequences(conn, tran)

                    Dim adminUserId As Integer = GetAdminUserId(conn, tran)
                    Dim supplierIds As New Dictionary(Of String, Integer)()
                    SeedSuppliers(conn, tran, supplierIds)
                    Dim productIds As New List(Of Integer)()
                    Dim initialLogs As Integer = 0
                    SeedProducts(conn, tran, supplierIds, productIds, adminUserId, initialLogs)

                    Dim stats As New Dictionary(Of String, Integer)() From {
                        {"Suppliers", supplierIds.Count},
                        {"Products", productIds.Count},
                        {"InitialLogs", initialLogs}
                    }

                    stats("Sales") = SeedSales(conn, tran, productIds, adminUserId, stats)
                    SeedAuditLog(conn, tran, adminUserId)

                    tran.Commit()

                    Dim summary As String = "Demo data loaded successfully:" & vbCrLf &
                        $"   Suppliers: {stats("Suppliers")}" & vbCrLf &
                        $"   Products: {stats("Products")}" & vbCrLf &
                        $"   Sales: {stats("Sales")}" & vbCrLf &
                        $"   Sale items: {stats("SaleItems")}" & vbCrLf &
                        $"   Inventory logs: {stats("InventoryLogs")}" & vbCrLf &
                        $"   Audit logs: 1"
                    Console.WriteLine(summary.Replace(vbCrLf, " | "))
                    Return summary
                Catch ex As Exception
                    Try
                        tran.Rollback()
                    Catch
                    End Try
                    Throw
                End Try
            End Using
        End Using
    End Function

    Private Shared Sub WipeDemoTables(conn As SqliteConnection, tran As SqliteTransaction)
        Dim tables As String() = {
            "SaleItems",
            "Sales",
            "InventoryLog",
            "AuditLog",
            "ProductImageMapping",
            "ProductImages",
            "Products",
            "Suppliers"
        }
        For Each t As String In tables
            Using cmd As New SqliteCommand($"DELETE FROM {t}", conn, tran)
                cmd.ExecuteNonQuery()
            End Using
        Next
    End Sub

    Private Shared Sub ResetSequences(conn As SqliteConnection, tran As SqliteTransaction)
        Dim tables As String() = {
            "SaleItems",
            "Sales",
            "InventoryLog",
            "AuditLog",
            "ProductImageMapping",
            "ProductImages",
            "Products",
            "Suppliers"
        }
        For Each t As String In tables
            Try
                Using cmd As New SqliteCommand("DELETE FROM sqlite_sequence WHERE name = @name", conn, tran)
                    cmd.Parameters.AddWithValue("@name", t)
                    cmd.ExecuteNonQuery()
                End Using
            Catch
                ' sqlite_sequence may not exist yet on a brand-new db
            End Try
        Next
    End Sub

    Private Shared Function GetAdminUserId(conn As SqliteConnection, tran As SqliteTransaction) As Integer
        Using cmd As New SqliteCommand("SELECT UserID FROM Users ORDER BY UserID LIMIT 1", conn, tran)
            Dim result As Object = cmd.ExecuteScalar()
            If result Is Nothing OrElse IsDBNull(result) Then
                Throw New Exception("No user found in the Users table. Please create at least one user first.")
            End If
            Return Convert.ToInt32(result)
        End Using
    End Function

    Private Shared Sub SeedSuppliers(conn As SqliteConnection, tran As SqliteTransaction,
                                     supplierIds As Dictionary(Of String, Integer))
        Dim suppliers As String() = {
            "Dental Supply Co.",
            "Medical Equipment Ltd.",
            "Oral Care Supplies",
            "Metro Dental Distributors",
            "Prime Ortho Systems",
            "MediPro Surgical Supply",
            "Global Endo Technologies",
            "Pearl White Cosmetic Solutions"
        }

        For i As Integer = 0 To suppliers.Length - 1
            Dim code As String = $"SUP{(i + 1).ToString("D3")}"
            Dim contact As String = ContactPersons(i Mod ContactPersons.Length)
            Dim phone As String = $"(02) 8{100 + i * 97}-{2000 + i * 863}"
            Dim email As String = $"sales@supplier{i + 1}.com"

            Using cmd As New SqliteCommand(
                "INSERT INTO Suppliers (SupplierCode, SupplierName, ContactPerson, Phone, Email, IsActive) " &
                "VALUES (@code, @name, @contact, @phone, @email, 1); SELECT last_insert_rowid()", conn, tran)
                cmd.Parameters.AddWithValue("@code", code)
                cmd.Parameters.AddWithValue("@name", suppliers(i))
                cmd.Parameters.AddWithValue("@contact", contact)
                cmd.Parameters.AddWithValue("@phone", phone)
                cmd.Parameters.AddWithValue("@email", email)
                supplierIds(code) = Convert.ToInt32(cmd.ExecuteScalar())
            End Using
        Next
    End Sub

    Private Shared ReadOnly ContactPersons As String() = {
        "Juan Dela Cruz",
        "Maria Santos",
        "Jose Ramirez",
        "Ana Reyes",
        "Pedro Garcia",
        "Liza Fernandez",
        "Carlos Mendoza",
        "Grace Torres"
    }

    Private Shared Sub SeedProducts(conn As SqliteConnection, tran As SqliteTransaction,
                                    supplierIds As Dictionary(Of String, Integer),
                                    productIds As List(Of Integer), adminUserId As Integer,
                                    ByRef initialLogs As Integer)
        ' Category, Name, Unit, Cost, Sell, Wholesale, Stock, Reorder
        Dim products(,) As Object = {
            {"ORTHO", "Braces Kit (Standard Metal)", "SET", 850, 1500, 1200, 25, 5},
            {"ORTHO", "Ligature Wire Spool", "PCS", 120, 250, 180, 60, 15},
            {"ORTHO", "Orthodontic Bands", "BOX", 400, 750, 600, 30, 8},
            {"ORTHO", "Archwire NiTi (Round)", "SET", 150, 300, 220, 45, 10},
            {"ORTHO", "Elastic Bands Pack", "BOX", 80, 180, 120, 100, 20},
            {"CONSUMABLES", "Latex Gloves (Box of 100)", "BOX", 180, 320, 250, 80, 15},
            {"CONSUMABLES", "Face Masks (Box of 50)", "BOX", 90, 200, 140, 120, 20},
            {"CONSUMABLES", "Dental Cotton Rolls", "BOX", 70, 150, 100, 90, 15},
            {"CONSUMABLES", "Disposable Syringes 5ml", "BOX", 60, 130, 95, 75, 12},
            {"CONSUMABLES", "Saliva Ejectors", "BOX", 50, 110, 80, 0, 15},
            {"SURGERY", "Scalpel Blades #15", "BOX", 200, 380, 300, 40, 10},
            {"SURGERY", "Surgical Sutures 3-0", "PCS", 75, 160, 110, 55, 10},
            {"SURGERY", "Sterile Gauze Pads", "BOX", 45, 95, 70, 110, 20},
            {"SURGERY", "Local Anesthetic 2% (Cartridge)", "BOX", 250, 480, 380, 35, 8},
            {"SURGERY", "Bone Graft Material (0.5g)", "PCS", 950, 1600, 1300, 12, 3},
            {"RESTO", "Composite Resin A2 (Syringe)", "TUBE", 450, 850, 650, 28, 6},
            {"RESTO", "Glass Ionomer Cement", "SET", 300, 580, 440, 22, 5},
            {"RESTO", "Bonding Agent (5ml)", "BTL", 350, 680, 520, 18, 4},
            {"RESTO", "Amalgam Capsules", "BOX", 220, 420, 330, 33, 8},
            {"RESTO", "Flowable Composite", "TUBE", 380, 720, 550, 20, 5},
            {"ENDO", "Endodontic Files K #25", "BOX", 280, 540, 410, 26, 6},
            {"ENDO", "Gutta Percha Points", "BOX", 130, 270, 190, 48, 10},
            {"ENDO", "Root Canal Sealer", "SET", 400, 760, 580, 16, 4},
            {"ENDO", "Rotary Endo Files (NiTi)", "BOX", 900, 1550, 1200, 14, 3},
            {"ENDO", "Paper Points", "BOX", 110, 230, 160, 52, 10},
            {"COSMETIC", "Teeth Whitening Gel", "TUBE", 260, 520, 390, 24, 6},
            {"COSMETIC", "Veneer Composite Kit", "SET", 1200, 2100, 1650, 10, 2},
            {"COSMETIC", "Bleaching Trays (Pair)", "SET", 150, 320, 230, 30, 8},
            {"COSMETIC", "Diamond Polishing Paste", "TUBE", 180, 360, 260, 27, 6},
            {"COSMETIC", "Air Polishing Powder", "BTL", 210, 400, 300, 19, 5}
        }

        Dim supplierCodes As String() = {"SUP001", "SUP002", "SUP003", "SUP004", "SUP005", "SUP006", "SUP007", "SUP008"}

        For i As Integer = 0 To products.GetUpperBound(0)
            Dim category As String = products(i, 0).ToString()
            Dim name As String = products(i, 1).ToString()
            Dim unit As String = products(i, 2).ToString()
            Dim cost As Decimal = Convert.ToDecimal(products(i, 3))
            Dim sell As Decimal = Convert.ToDecimal(products(i, 4))
            Dim wholesale As Decimal = Convert.ToDecimal(products(i, 5))
            Dim stock As Integer = Convert.ToInt32(products(i, 6))
            Dim reorder As Integer = Convert.ToInt32(products(i, 7))
            Dim supplierCode As String = supplierCodes(i Mod supplierCodes.Length)

            Dim supplierId As Object
            If supplierIds.ContainsKey(supplierCode) Then
                supplierId = supplierIds(supplierCode)
            Else
                supplierId = DBNull.Value
            End If

            Using cmd As New SqliteCommand(
                "INSERT INTO Products (ProductCode, ProductName, Category, Unit, CurrentStock, ReorderLevel, " &
                "CostPrice, SellingPrice, WholesalePrice, SupplierID, IsActive, Created, UpdatedAt) " &
                "VALUES (@code, @name, @category, @unit, @stock, @reorder, @cost, @sell, @wholesale, @supplier, 1, datetime('now'), datetime('now')); SELECT last_insert_rowid()", conn, tran)
                cmd.Parameters.AddWithValue("@code", "TEMP_CODE")
                cmd.Parameters.AddWithValue("@name", name)
                cmd.Parameters.AddWithValue("@category", category)
                cmd.Parameters.AddWithValue("@unit", unit)
                cmd.Parameters.AddWithValue("@stock", stock)
                cmd.Parameters.AddWithValue("@reorder", reorder)
                cmd.Parameters.AddWithValue("@cost", cost)
                cmd.Parameters.AddWithValue("@sell", sell)
                cmd.Parameters.AddWithValue("@wholesale", wholesale)
                cmd.Parameters.AddWithValue("@supplier", supplierId)

                Dim productId As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                productIds.Add(productId)

                Using cmdUpdate As New SqliteCommand(
                    "UPDATE Products SET ProductCode = @code WHERE ProductID = @id", conn, tran)
                    cmdUpdate.Parameters.AddWithValue("@code", Utilities.GenerateProductCode(productId))
                    cmdUpdate.Parameters.AddWithValue("@id", productId)
                    cmdUpdate.ExecuteNonQuery()
                End Using

                ' Initial stock IN log (skip when there is no opening stock)
                If stock > 0 Then
                    Using logCmd As New SqliteCommand(
                        "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, " &
                        "UserID, Reference, Notes, CreatedAt) " &
                        "VALUES (@pid, 'IN', @qty, 0, @newstock, @user, 'Initial Stock', 'Seeded demo opening stock', datetime('now'))", conn, tran)
                        logCmd.Parameters.AddWithValue("@pid", productId)
                        logCmd.Parameters.AddWithValue("@qty", stock)
                        logCmd.Parameters.AddWithValue("@newstock", stock)
                        logCmd.Parameters.AddWithValue("@user", adminUserId)
                        logCmd.ExecuteNonQuery()
                    End Using
                    initialLogs += 1
                End If
            End Using
        Next
    End Sub

    Private Shared Function SeedSales(conn As SqliteConnection, tran As SqliteTransaction,
                                      productIds As List(Of Integer), adminUserId As Integer,
                                      stats As Dictionary(Of String, Integer)) As Integer
        If productIds.Count = 0 Then Return 0

        Dim rng As New Random(2026)
        Dim paymentMethods As String() = {"Cash", "GCash", "Maya", "Card", "Cash"}
        Dim customerPool As String() = {
            "", "Maria Clara", "Jose Rizal", "Emilio Aguinaldo", "Andres Bonifacio",
            "Gabriela Silang", "Apolinario Mabini", "Melchora Aquino", "Lapu-Lapu", "Dona Teodora"
        }
        Dim saleCount As Integer = 0
        Dim itemCount As Integer = 0
        Dim logCount As Integer = 0

        For day As Integer = 30 To 1 Step -1
            ' 0-2 sales per day for a busy clinic
            Dim salesToday As Integer = rng.Next(0, 3)
            For s As Integer = 0 To salesToday - 1
                Dim itemsPerSale As Integer = rng.Next(1, 4)
                Dim selected As New List(Of Integer)()
                For k As Integer = 0 To itemsPerSale - 1
                    selected.Add(productIds(rng.Next(productIds.Count)))
                Next

                Dim saleDateTime As DateTime = DateTime.Today.AddDays(-day).AddHours(rng.Next(8, 18)).AddMinutes(rng.Next(0, 60))
                Dim customerName As String = customerPool(rng.Next(customerPool.Length))
                Dim method As String = paymentMethods(rng.Next(paymentMethods.Length))
                Dim reference As String = If(method = "Cash", "", $"REF-{rng.Next(100000, 999999)}")

                Dim orderItems As New List(Of Dictionary(Of String, Object))()
                Dim total As Decimal = 0D
                For Each pid As Integer In selected
                    Dim qty As Integer = rng.Next(1, 3)
                    Dim product As (id As Integer, name As String, code As String, category As String, price As Decimal) = GetProductSnapshot(conn, tran, pid)
                    Dim lineTotal As Decimal = product.price * qty
                    total += lineTotal
                    orderItems.Add(New Dictionary(Of String, Object)() From {
                        {"ProductID", pid},
                        {"ProductName", product.name},
                        {"Price", product.price},
                        {"ProductCode", product.code},
                        {"Category", product.category},
                        {"Quantity", qty}
                    })
                Next

                Dim paid As Decimal = If(total Mod 5 = 0, total + rng.Next(0, 5) * 10, total)
                If paid < total Then paid = total

                Dim salesData As New Dictionary(Of String, Object)()
                salesData("payment") = New Dictionary(Of String, Object)() From {
                    {"method", method},
                    {"reference", reference},
                    {"received", paid},
                    {"change", paid - total},
                    {"discount", New Dictionary(Of String, Object)() From {{"type", "None"}, {"amount", 0}}}
                }
                salesData("items") = orderItems
                Dim salesDataJson As String = JsonConvert.SerializeObject(salesData)

                Dim saleNumber As String = $"SALE-{saleDateTime:yyyyMMdd}-{rng.Next(1000, 9999)}"

                Using cmd As New SqliteCommand(
                    "INSERT INTO Sales (SaleNumber, SaleDate, CustomerName, CustomerTIN, UserID, TotalAmount, " &
                    "AmountPaid, PaymentMethod, IsVoid, Reference, SalesData, Status, DiscountType, DiscountAmount) " &
                    "VALUES (@salenum, @saledate, @customer, @tin, @user, @total, @paid, @method, 0, @reference, " &
                    "@salesdata, 'Completed', NULL, 0); SELECT last_insert_rowid()", conn, tran)
                    cmd.Parameters.AddWithValue("@salenum", saleNumber)
                    cmd.Parameters.AddWithValue("@saledate", saleDateTime)
                    cmd.Parameters.AddWithValue("@customer", If(String.IsNullOrWhiteSpace(customerName), CType(DBNull.Value, Object), CType(customerName, Object)))
                    cmd.Parameters.AddWithValue("@tin", DBNull.Value)
                    cmd.Parameters.AddWithValue("@user", adminUserId)
                    cmd.Parameters.AddWithValue("@total", total)
                    cmd.Parameters.AddWithValue("@paid", paid)
                    cmd.Parameters.AddWithValue("@method", method)
                    cmd.Parameters.AddWithValue("@reference", If(String.IsNullOrWhiteSpace(reference), CType(DBNull.Value, Object), CType(reference, Object)))
                    cmd.Parameters.AddWithValue("@salesdata", salesDataJson)

                    Dim saleId As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                    saleCount += 1

                    For Each item As Dictionary(Of String, Object) In orderItems
                        Dim pid2 As Integer = Convert.ToInt32(item("ProductID"))
                        Dim qty2 As Integer = Convert.ToInt32(item("Quantity"))
                        Dim price2 As Decimal = Convert.ToDecimal(item("Price"))
                        Dim subTotal As Decimal = price2 * qty2

                        Using itemCmd As New SqliteCommand(
                            "INSERT INTO SaleItems (SaleID, ProductID, Quantity, UnitPrice, OriginalUnitPrice, SubTotal) " &
                            "VALUES (@saleid, @pid, @qty, @price, @price, @subtotal)", conn, tran)
                            itemCmd.Parameters.AddWithValue("@saleid", saleId)
                            itemCmd.Parameters.AddWithValue("@pid", pid2)
                            itemCmd.Parameters.AddWithValue("@qty", qty2)
                            itemCmd.Parameters.AddWithValue("@price", price2)
                            itemCmd.Parameters.AddWithValue("@subtotal", subTotal)
                            itemCmd.ExecuteNonQuery()
                        End Using
                        itemCount += 1

                        ' Stock OUT log (deducts current stock, keeps numbers consistent)
                        Dim prevStock As Integer = GetCurrentStock(conn, tran, pid2)
                        Dim newStock As Integer = prevStock - qty2
                        If newStock < 0 Then newStock = 0
                        Using logCmd As New SqliteCommand(
                            "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, " &
                            "UserID, Reference, Notes, CreatedAt) " &
                            "VALUES (@pid, 'OUT', @qty, @prev, @new, @user, @salenum, 'Sale', @created)", conn, tran)
                            logCmd.Parameters.AddWithValue("@pid", pid2)
                            logCmd.Parameters.AddWithValue("@qty", qty2)
                            logCmd.Parameters.AddWithValue("@prev", prevStock)
                            logCmd.Parameters.AddWithValue("@new", newStock)
                            logCmd.Parameters.AddWithValue("@user", adminUserId)
                            logCmd.Parameters.AddWithValue("@salenum", saleNumber)
                            logCmd.Parameters.AddWithValue("@created", saleDateTime)
                            logCmd.ExecuteNonQuery()
                        End Using
                        logCount += 1

                        Using upCmd As New SqliteCommand(
                            "UPDATE Products SET CurrentStock = @new WHERE ProductID = @pid", conn, tran)
                            upCmd.Parameters.AddWithValue("@new", newStock)
                            upCmd.Parameters.AddWithValue("@pid", pid2)
                            upCmd.ExecuteNonQuery()
                        End Using
                    Next
                End Using
            Next
        Next

        stats("SaleItems") = itemCount
        stats("InventoryLogs") = logCount + stats("InitialLogs")
        Return saleCount
    End Function

    Private Shared Function GetProductSnapshot(conn As SqliteConnection, tran As SqliteTransaction, productId As Integer) As (id As Integer, name As String, code As String, category As String, price As Decimal)
        Using cmd As New SqliteCommand(
            "SELECT ProductID, ProductName, ProductCode, Category, SellingPrice FROM Products WHERE ProductID = @id", conn, tran)
            cmd.Parameters.AddWithValue("@id", productId)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
                If reader.Read() Then
                    Return (Convert.ToInt32(reader("ProductID")),
                            reader("ProductName").ToString(),
                            reader("ProductCode").ToString(),
                            If(IsDBNull(reader("Category")), "", reader("Category").ToString()),
                            Convert.ToDecimal(reader("SellingPrice")))
                End If
            End Using
        End Using
        Return (productId, "", "", "", 0D)
    End Function

    Private Shared Function GetCurrentStock(conn As SqliteConnection, tran As SqliteTransaction, productId As Integer) As Integer
        Using cmd As New SqliteCommand("SELECT IFNULL(CurrentStock, 0) FROM Products WHERE ProductID = @id", conn, tran)
            cmd.Parameters.AddWithValue("@id", productId)
            Dim result As Object = cmd.ExecuteScalar()
            If result Is Nothing OrElse IsDBNull(result) Then Return 0
            Return Convert.ToInt32(result)
        End Using
    End Function

    Private Shared Sub SeedAuditLog(conn As SqliteConnection, tran As SqliteTransaction, adminUserId As Integer)
        Using cmd As New SqliteCommand(
            "INSERT INTO AuditLog (Action, Details, ActionTime, UserID) VALUES ('Demo Data Seeded', 'Demo data was loaded via System Settings', datetime('now'), @user)", conn, tran)
            cmd.Parameters.AddWithValue("@user", adminUserId)
            cmd.ExecuteNonQuery()
        End Using
    End Sub

End Class
