Imports System.IO
Imports System.Collections.Generic
Imports Microsoft.Data.Sqlite
Imports Newtonsoft.Json

' Wipes the demo tables and reseeds realistic dental-clinic demo data.
' Keeps the admin user, company settings, and image files intact.
' Seeds demo users (manager, 3 staff, 1 inactive admin), ~40 products,
' ~3 years of sales history, matching inventory logs, and audit logs.
Public Class DemoDataSeeder

    Private Class DemoUser
        Public Id As Integer
        Public Username As String
        Public FullName As String
        Public Role As String
        Public IsActive As Boolean
        Public CreatedAt As DateTime
    End Class

    Private Class DemoProduct
        Public Id As Integer
        Public Code As String
        Public Name As String
        Public Category As String
        Public Unit As String
        Public Cost As Decimal
        Public SellingPrice As Decimal
        Public Wholesale As Decimal
        Public ReorderLevel As Integer
        Public SupplierID As Integer
        Public RunningStock As Integer
        Public CreatedAt As DateTime
    End Class

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

                    Dim stats As New Dictionary(Of String, Integer)()

                    Dim adminUserId As Integer = GetAdminUserId(conn, tran)
                    Dim users As List(Of DemoUser) = SeedUsers(conn, tran, adminUserId)
                    stats("Users") = users.Count

                    Dim supplierIds As New Dictionary(Of String, Integer)()
                    SeedSuppliers(conn, tran, supplierIds)
                    stats("Suppliers") = supplierIds.Count

                    Dim products As List(Of DemoProduct) = SeedProducts(conn, tran, supplierIds, adminUserId)
                    stats("Products") = products.Count

                    SeedSales(conn, tran, products, users, stats, adminUserId)
                    stats("AuditLogs") = SeedAuditLogs(conn, tran, products, users, adminUserId)

                    tran.Commit()

                    Dim summary As String = "Demo data loaded successfully:" & vbCrLf &
                        $"   Users: {stats("Users")}" & vbCrLf &
                        $"   Suppliers: {stats("Suppliers")}" & vbCrLf &
                        $"   Products: {stats("Products")}" & vbCrLf &
                        $"   Sales: {stats("Sales")}" & vbCrLf &
                        $"   Sale items: {stats("SaleItems")}" & vbCrLf &
                        $"   Inventory logs: {stats("InventoryLogs")}" & vbCrLf &
                        $"   Audit logs: {stats("AuditLogs")}" & vbCrLf &
                        "   Sales history: ~3 years"
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
        Using cmd As New SqliteCommand("SELECT UserID FROM Users WHERE Username = 'admin' LIMIT 1", conn, tran)
            Dim result As Object = cmd.ExecuteScalar()
            If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                Return Convert.ToInt32(result)
            End If
        End Using

        Using cmd As New SqliteCommand("SELECT UserID FROM Users ORDER BY UserID LIMIT 1", conn, tran)
            Dim result As Object = cmd.ExecuteScalar()
            If result Is Nothing OrElse IsDBNull(result) Then
                Throw New Exception("No user found in the Users table. Please create at least one user first.")
            End If
            Return Convert.ToInt32(result)
        End Using
    End Function

    Private Shared Function SeedUsers(conn As SqliteConnection, tran As SqliteTransaction,
                                      adminUserId As Integer) As List(Of DemoUser)
        Dim startDate As DateTime = DateTime.Today.AddYears(-3)

        ' Username, Password, FullName, Email, Phone, Role, IsActive, Pin, CreatedAt
        Dim definitions As (Username As String, Password As String, FullName As String, Email As String,
                            Phone As String, Role As String, IsActive As Boolean, Pin As Integer,
                            CreatedAt As DateTime)() = {
            ("manager", "manager123", "Maria Santos", "manager@jadeclinic.com", "0917-555-0102", "Manager", True, 5678, startDate.AddDays(30)),
            ("inactiveadmin", "admin123", "Juan Dela Cruz", "juan.delacruz@jadeclinic.com", "0917-555-0103", "Admin", False, 0, startDate.AddDays(60)),
            ("staff1", "staff123", "Carla Mendoza", "carla@jadeclinic.com", "0917-555-0104", "Staff", True, 1111, startDate.AddDays(90)),
            ("staff2", "staff123", "Pedro Garcia", "pedro@jadeclinic.com", "0917-555-0105", "Staff", True, 2222, startDate.AddDays(150)),
            ("staff3", "staff123", "Ana Reyes", "ana@jadeclinic.com", "0917-555-0106", "Staff", True, 3333, startDate.AddDays(210))
        }

        For Each def In definitions
            Dim exists As Boolean = False
            Using chk As New SqliteCommand("SELECT COUNT(*) FROM Users WHERE Username = @u", conn, tran)
                chk.Parameters.AddWithValue("@u", def.Username)
                exists = Convert.ToInt32(chk.ExecuteScalar()) > 0
            End Using

            If Not exists Then
                Dim hashed As String = frmLoginvb.HashPassword(def.Password)

                Using cmd As New SqliteCommand(
                    "INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, UserRole, pin, IsActive, CreatedAt, UpdatedAt) " &
                    "VALUES (@u, @p, @fn, @em, @ph, @role, @pin, @act, @created, @created); SELECT last_insert_rowid()", conn, tran)
                    cmd.Parameters.AddWithValue("@u", def.Username)
                    cmd.Parameters.AddWithValue("@p", hashed)
                    cmd.Parameters.AddWithValue("@fn", def.FullName)
                    cmd.Parameters.AddWithValue("@em", def.Email)
                    cmd.Parameters.AddWithValue("@ph", def.Phone)
                    cmd.Parameters.AddWithValue("@role", def.Role)
                    cmd.Parameters.AddWithValue("@pin", def.Pin)
                    cmd.Parameters.AddWithValue("@act", If(def.IsActive, 1, 0))
                    cmd.Parameters.AddWithValue("@created", def.CreatedAt)

                    Dim newId As Integer = Convert.ToInt32(cmd.ExecuteScalar())

                    Using upd As New SqliteCommand(
                        "UPDATE Users SET QRCode = @qr, Passkeys = @pk WHERE UserID = @id", conn, tran)
                        upd.Parameters.AddWithValue("@qr", $"User-{newId:D4}-{DateTime.Now:yyyyMMddHHmmssfff}")
                        upd.Parameters.AddWithValue("@pk", String.Join(",", DatabaseInitializer.GenerateRandomPasskeys(3)))
                        upd.Parameters.AddWithValue("@id", newId)
                        upd.ExecuteNonQuery()
                    End Using
                End Using
            End If
        Next

        ' Build the full user list (includes any users that already existed)
        Dim users As New List(Of DemoUser)()
        Using cmd As New SqliteCommand(
            "SELECT UserID, Username, FullName, UserRole, IsActive FROM Users", conn, tran)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    users.Add(New DemoUser With {
                        .Id = Convert.ToInt32(reader("UserID")),
                        .Username = reader("Username").ToString(),
                        .FullName = If(IsDBNull(reader("FullName")), "", reader("FullName").ToString()),
                        .Role = If(IsDBNull(reader("UserRole")), "Staff", reader("UserRole").ToString()),
                        .IsActive = Not IsDBNull(reader("IsActive")) AndAlso Convert.ToBoolean(reader("IsActive")),
                        .CreatedAt = DateTime.Today.AddYears(-3)
                    })
                End While
            End Using
        End Using
        Return users
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

    Private Shared Function SeedProducts(conn As SqliteConnection, tran As SqliteTransaction,
                                         supplierIds As Dictionary(Of String, Integer),
                                         adminUserId As Integer) As List(Of DemoProduct)
        ' Category, Name, Unit, Cost, Sell, Wholesale, OpeningStock, Reorder
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
            {"COSMETIC", "Air Polishing Powder", "BTL", 210, 400, 300, 19, 5},
            {"RESTO", "Alginate Impression Powder", "BAG", 140, 300, 210, 45, 10},
            {"RESTO", "Silicone Impression Material (Base)", "TUBE", 380, 720, 550, 18, 5},
            {"RESTO", "Matrix Bands (Assorted)", "BOX", 160, 340, 240, 40, 10},
            {"ORTHO", "Orthodontic Pliers Set", "SET", 550, 950, 750, 15, 4},
            {"RESTO", "Bite Registration Wax", "BOX", 95, 200, 140, 60, 12},
            {"CONSUMABLES", "Dental X-Ray Film", "BOX", 220, 420, 320, 30, 8},
            {"SURGERY", "Sterilization Pouches", "BOX", 110, 240, 170, 85, 15},
            {"COSMETIC", "Prophy Paste (Jar)", "JAR", 130, 280, 190, 38, 8},
            {"SURGERY", "Ultrasonic Scaler Tips", "SET", 320, 620, 470, 12, 3},
            {"SURGERY", "Contra-Angle Handpiece", "PCS", 1450, 2400, 1900, 8, 2}
        }

        Dim supplierCodes As String() = {"SUP001", "SUP002", "SUP003", "SUP004", "SUP005", "SUP006", "SUP007", "SUP008"}
        Dim startDate As DateTime = DateTime.Today.AddYears(-3)
        Dim result As New List(Of DemoProduct)()

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

            Dim createdAt As DateTime = startDate.AddDays(i * 3)

            Using cmd As New SqliteCommand(
                "INSERT INTO Products (ProductCode, ProductName, Category, Unit, CurrentStock, ReorderLevel, " &
                "CostPrice, SellingPrice, WholesalePrice, SupplierID, IsActive, Created, UpdatedAt) " &
                "VALUES (@code, @name, @category, @unit, @stock, @reorder, @cost, @sell, @wholesale, @supplier, 1, @created, @created); SELECT last_insert_rowid()", conn, tran)
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
                cmd.Parameters.AddWithValue("@created", createdAt)

                Dim productId As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                Dim productCode As String = Utilities.GenerateProductCode(productId)

                Using cmdUpdate As New SqliteCommand(
                    "UPDATE Products SET ProductCode = @code WHERE ProductID = @id", conn, tran)
                    cmdUpdate.Parameters.AddWithValue("@code", productCode)
                    cmdUpdate.Parameters.AddWithValue("@id", productId)
                    cmdUpdate.ExecuteNonQuery()
                End Using

                ' Opening stock IN log
                If stock > 0 Then
                    Using logCmd As New SqliteCommand(
                        "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, " &
                        "BatchNumber, SupplierID, UserID, Reference, Notes, CreatedAt) " &
                        "VALUES (@pid, 'IN', @qty, 0, @newstock, @batch, @supplier, @user, 'Initial Stock', 'Seeded demo opening stock', @created)", conn, tran)
                        logCmd.Parameters.AddWithValue("@pid", productId)
                        logCmd.Parameters.AddWithValue("@qty", stock)
                        logCmd.Parameters.AddWithValue("@newstock", stock)
                        logCmd.Parameters.AddWithValue("@batch", $"BATCH-{productId:D4}-OPEN")
                        logCmd.Parameters.AddWithValue("@supplier", supplierId)
                        logCmd.Parameters.AddWithValue("@user", adminUserId)
                        logCmd.Parameters.AddWithValue("@created", createdAt)
                        logCmd.ExecuteNonQuery()
                    End Using
                End If

                result.Add(New DemoProduct With {
                    .Id = productId,
                    .Code = productCode,
                    .Name = name,
                    .Category = category,
                    .Unit = unit,
                    .Cost = cost,
                    .SellingPrice = sell,
                    .Wholesale = wholesale,
                    .ReorderLevel = reorder,
                    .SupplierID = If(IsDBNull(supplierId), 0, Convert.ToInt32(supplierId)),
                    .RunningStock = stock,
                    .CreatedAt = createdAt
                })
            End Using
        Next
        Return result
    End Function

    Private Shared Sub SeedSales(conn As SqliteConnection, tran As SqliteTransaction,
                                 products As List(Of DemoProduct), users As List(Of DemoUser),
                                 stats As Dictionary(Of String, Integer), adminUserId As Integer)
        If products.Count = 0 Then
            stats("Sales") = 0
            stats("SaleItems") = 0
            stats("InventoryLogs") = 0
            Return
        End If

        Dim rng As New Random(2026)
        Dim activeUsers As List(Of DemoUser) = users.Where(Function(u) u.IsActive).ToList()
        If activeUsers.Count = 0 Then activeUsers.AddRange(users)

        Dim startDate As DateTime = DateTime.Today.AddYears(-3)
        Dim endDate As DateTime = DateTime.Today
        Dim totalDays As Integer = (endDate - startDate).Days

        Dim paymentMethods As String() = {"Cash", "GCash", "Maya", "Card"}
        Dim customerPool As String() = {
            "Maria Clara", "Jose Rizal", "Emilio Aguinaldo", "Andres Bonifacio",
            "Gabriela Silang", "Apolinario Mabini", "Melchora Aquino", "Lapu-Lapu",
            "Dona Teodora", "Antonio Luna", "Josefa Rizal", "Gregorio del Pilar"
        }

        Dim saleCount As Integer = 0
        Dim itemCount As Integer = 0
        Dim logCount As Integer = 0

        For dayOffset As Integer = 0 To totalDays
            Dim day As DateTime = startDate.AddDays(dayOffset)
            Dim progress As Double = dayOffset / totalDays
            Dim isSunday As Boolean = (day.DayOfWeek = DayOfWeek.Sunday)
            Dim base As Integer = 3 + CInt(8 * progress)
            Dim maxSales As Integer = base + rng.Next(0, 4)
            If isSunday Then maxSales = Math.Max(1, maxSales - 3)

            For s As Integer = 0 To maxSales - 1
                Dim saleUser As DemoUser = PickSalesUser(activeUsers, rng)
                Dim saleDateTime As DateTime = day.Date.AddHours(rng.Next(8, 19)).AddMinutes(rng.Next(0, 60))
                If saleDateTime > DateTime.Now Then saleDateTime = DateTime.Now

                Dim itemsPerSale As Integer = rng.Next(1, 4)
                Dim selected As New List(Of DemoProduct)()
                For k As Integer = 0 To itemsPerSale - 1
                    Dim p As DemoProduct = products(rng.Next(products.Count))
                    EnsureStock(conn, tran, p, rng, saleDateTime, users, adminUserId, logCount)
                    selected.Add(p)
                Next

                Dim method As String = PickPaymentMethod(paymentMethods, rng)
                Dim reference As String = If(method = "Cash", "", $"{method}-{rng.Next(100000, 999999)}")
                Dim customerName As String = PickCustomer(customerPool, rng)

                Dim orderItems As New List(Of Dictionary(Of String, Object))()
                Dim total As Decimal = 0D
                For Each p As DemoProduct In selected
                    Dim qty As Integer = rng.Next(1, 3)
                    EnsureStockQty(conn, tran, p, qty, rng, saleDateTime, users, adminUserId, logCount)
                    Dim lineTotal As Decimal = p.SellingPrice * qty
                    total += lineTotal
                    orderItems.Add(New Dictionary(Of String, Object)() From {
                        {"ProductID", p.Id},
                        {"ProductName", p.Name},
                        {"Price", p.SellingPrice},
                        {"ProductCode", p.Code},
                        {"Category", p.Category},
                        {"Quantity", qty}
                    })
                Next

                Dim paid As Decimal = total
                If method = "Cash" Then
                    Dim roundedUp As Decimal = Math.Ceiling(total / 10D) * 10
                    If rng.Next(0, 3) = 0 Then paid = roundedUp
                    If paid < total Then paid = total
                End If

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

                Dim saleNumber As String = $"SALE-{saleDateTime:yyyyMMdd}-{(saleCount + 1).ToString("D5")}"

                Using cmd As New SqliteCommand(
                    "INSERT INTO Sales (SaleNumber, SaleDate, CustomerName, CustomerTIN, UserID, TotalAmount, " &
                    "AmountPaid, PaymentMethod, IsVoid, Reference, SalesData, Status, DiscountType, DiscountAmount) " &
                    "VALUES (@salenum, @saledate, @customer, @tin, @user, @total, @paid, @method, 0, @reference, " &
                    "@salesdata, 'Completed', NULL, 0); SELECT last_insert_rowid()", conn, tran)
                    cmd.Parameters.AddWithValue("@salenum", saleNumber)
                    cmd.Parameters.AddWithValue("@saledate", saleDateTime)
                    cmd.Parameters.AddWithValue("@customer", If(String.IsNullOrWhiteSpace(customerName), CType(DBNull.Value, Object), CType(customerName, Object)))
                    cmd.Parameters.AddWithValue("@tin", DBNull.Value)
                    cmd.Parameters.AddWithValue("@user", saleUser.Id)
                    cmd.Parameters.AddWithValue("@total", total)
                    cmd.Parameters.AddWithValue("@paid", paid)
                    cmd.Parameters.AddWithValue("@method", method)
                    cmd.Parameters.AddWithValue("@reference", If(String.IsNullOrWhiteSpace(reference), CType(DBNull.Value, Object), CType(reference, Object)))
                    cmd.Parameters.AddWithValue("@salesdata", salesDataJson)

                    Dim saleId As Integer = Convert.ToInt32(cmd.ExecuteScalar())
                    saleCount += 1

                    For Each item As Dictionary(Of String, Object) In orderItems
                        Dim pid As Integer = Convert.ToInt32(item("ProductID"))
                        Dim qty As Integer = Convert.ToInt32(item("Quantity"))
                        Dim price As Decimal = Convert.ToDecimal(item("Price"))
                        Dim subTotal As Decimal = price * qty

                        Using itemCmd As New SqliteCommand(
                            "INSERT INTO SaleItems (SaleID, ProductID, Quantity, UnitPrice, OriginalUnitPrice, SubTotal) " &
                            "VALUES (@saleid, @pid, @qty, @price, @price, @subtotal)", conn, tran)
                            itemCmd.Parameters.AddWithValue("@saleid", saleId)
                            itemCmd.Parameters.AddWithValue("@pid", pid)
                            itemCmd.Parameters.AddWithValue("@qty", qty)
                            itemCmd.Parameters.AddWithValue("@price", price)
                            itemCmd.Parameters.AddWithValue("@subtotal", subTotal)
                            itemCmd.ExecuteNonQuery()
                        End Using
                        itemCount += 1

                        ' Stock OUT log (deducts running stock, keeps numbers consistent)
                        Dim p As DemoProduct = products.FirstOrDefault(Function(x) x.Id = pid)
                        Dim prevStock As Integer = p.RunningStock
                        Dim newStock As Integer = Math.Max(0, prevStock - qty)
                        p.RunningStock = newStock

                        Using logCmd As New SqliteCommand(
                            "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, " &
                            "SupplierID, UserID, Reference, Notes, CreatedAt) " &
                            "VALUES (@pid, 'OUT', @qty, @prev, @new, @supplier, @user, @salenum, 'Sale', @created)", conn, tran)
                            logCmd.Parameters.AddWithValue("@pid", pid)
                            logCmd.Parameters.AddWithValue("@qty", qty)
                            logCmd.Parameters.AddWithValue("@prev", prevStock)
                            logCmd.Parameters.AddWithValue("@new", newStock)
                            logCmd.Parameters.AddWithValue("@supplier", p.SupplierID)
                            logCmd.Parameters.AddWithValue("@user", saleUser.Id)
                            logCmd.Parameters.AddWithValue("@salenum", saleNumber)
                            logCmd.Parameters.AddWithValue("@created", saleDateTime)
                            logCmd.ExecuteNonQuery()
                        End Using
                        logCount += 1
                    Next
                End Using
            Next

            ' Occasional stock write-offs (damaged / expired)
            If rng.Next(0, 8) = 0 AndAlso products.Count > 0 Then
                Dim p As DemoProduct = products(rng.Next(products.Count))
                Dim adjQty As Integer = rng.Next(1, 3)
                If p.RunningStock >= adjQty Then
                    Dim prevStock As Integer = p.RunningStock
                    p.RunningStock -= adjQty
                    Dim adjUser As Integer = PickRestockUser(users, adminUserId, rng)
                    Using logCmd As New SqliteCommand(
                        "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, " &
                        "UserID, Reference, Notes, CreatedAt) " &
                        "VALUES (@pid, 'ADJUST', @qty, @prev, @new, @user, 'Stock Adjustment', 'Damaged/expired stock write-off', @created)", conn, tran)
                        logCmd.Parameters.AddWithValue("@pid", p.Id)
                        logCmd.Parameters.AddWithValue("@qty", adjQty)
                        logCmd.Parameters.AddWithValue("@prev", prevStock)
                        logCmd.Parameters.AddWithValue("@new", p.RunningStock)
                        logCmd.Parameters.AddWithValue("@user", adjUser)
                        logCmd.Parameters.AddWithValue("@created", day.Date.AddHours(9).AddMinutes(rng.Next(0, 60)))
                        logCmd.ExecuteNonQuery()
                    End Using
                    logCount += 1
                End If
            End If
        Next

        ' Sync final running stock into Products.CurrentStock
        For Each p As DemoProduct In products
            Using upCmd As New SqliteCommand(
                "UPDATE Products SET CurrentStock = @new, UpdatedAt = @now WHERE ProductID = @pid", conn, tran)
                upCmd.Parameters.AddWithValue("@new", p.RunningStock)
                upCmd.Parameters.AddWithValue("@now", DateTime.Now)
                upCmd.Parameters.AddWithValue("@pid", p.Id)
                upCmd.ExecuteNonQuery()
            End Using
        Next

        stats("Sales") = saleCount
        stats("SaleItems") = itemCount
        stats("InventoryLogs") = logCount
    End Sub

    Private Shared Sub EnsureStock(conn As SqliteConnection, tran As SqliteTransaction,
                                   p As DemoProduct, rng As Random, atTime As DateTime,
                                   users As List(Of DemoUser), adminUserId As Integer,
                                   ByRef logCount As Integer)
        If p.RunningStock <= p.ReorderLevel Then
            DoRestock(conn, tran, p, rng, atTime, users, adminUserId, logCount)
        End If
    End Sub

    Private Shared Sub EnsureStockQty(conn As SqliteConnection, tran As SqliteTransaction,
                                      p As DemoProduct, qty As Integer, rng As Random, atTime As DateTime,
                                      users As List(Of DemoUser), adminUserId As Integer,
                                      ByRef logCount As Integer)
        While p.RunningStock < qty
            DoRestock(conn, tran, p, rng, atTime, users, adminUserId, logCount)
        End While
    End Sub

    Private Shared Sub DoRestock(conn As SqliteConnection, tran As SqliteTransaction,
                                 p As DemoProduct, rng As Random, atTime As DateTime,
                                 users As List(Of DemoUser), adminUserId As Integer,
                                 ByRef logCount As Integer)
        Dim qty As Integer = Math.Max(4, p.ReorderLevel * rng.Next(2, 6))
        Dim prevStock As Integer = p.RunningStock
        p.RunningStock += qty

        Dim userId As Integer = PickRestockUser(users, adminUserId, rng)
        Dim batch As String = $"BATCH-{p.Id:D4}-{atTime:yyyyMM}"
        Dim expiry As Object = GetExpiryFor(p, atTime, rng)

        Using logCmd As New SqliteCommand(
            "INSERT INTO InventoryLog (ProductID, TransactionType, Quantity, PreviousStock, NewStock, " &
            "BatchNumber, ExpiryDate, SupplierID, UserID, Reference, Notes, CreatedAt) " &
            "VALUES (@pid, 'IN', @qty, @prev, @new, @batch, @expiry, @supplier, @user, 'Restock', 'Restocked to maintain inventory levels', @created)", conn, tran)
            logCmd.Parameters.AddWithValue("@pid", p.Id)
            logCmd.Parameters.AddWithValue("@qty", qty)
            logCmd.Parameters.AddWithValue("@prev", prevStock)
            logCmd.Parameters.AddWithValue("@new", p.RunningStock)
            logCmd.Parameters.AddWithValue("@batch", batch)
            logCmd.Parameters.AddWithValue("@expiry", expiry)
            logCmd.Parameters.AddWithValue("@supplier", p.SupplierID)
            logCmd.Parameters.AddWithValue("@user", userId)
            logCmd.Parameters.AddWithValue("@created", atTime)
            logCmd.ExecuteNonQuery()
        End Using
        logCount += 1
    End Sub

    Private Shared Function GetExpiryFor(p As DemoProduct, atTime As DateTime, rng As Random) As Object
        Select Case p.Category.ToUpper()
            Case "CONSUMABLES", "SURGERY", "RESTO", "ENDO", "COSMETIC"
                Return atTime.AddMonths(rng.Next(6, 19)).ToString("yyyy-MM-dd")
            Case Else
                Return DBNull.Value
        End Select
    End Function

    Private Shared Function PickSalesUser(active As List(Of DemoUser), rng As Random) As DemoUser
        Dim admins As List(Of DemoUser) = active.Where(Function(u) u.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase)).ToList()
        Dim managers As List(Of DemoUser) = active.Where(Function(u) u.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase)).ToList()
        Dim staff As List(Of DemoUser) = active.Where(Function(u) u.Role.Equals("Staff", StringComparison.OrdinalIgnoreCase)).ToList()

        Dim roll As Integer = rng.Next(1, 101)
        If roll <= 15 AndAlso admins.Count > 0 Then Return admins(rng.Next(admins.Count))
        If roll <= 40 AndAlso managers.Count > 0 Then Return managers(rng.Next(managers.Count))
        If staff.Count > 0 Then Return staff(rng.Next(staff.Count))
        Return active(rng.Next(active.Count))
    End Function

    Private Shared Function PickRestockUser(users As List(Of DemoUser), adminUserId As Integer, rng As Random) As Integer
        Dim mgmt As List(Of DemoUser) = users.Where(Function(u) u.IsActive AndAlso
                (u.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase) OrElse
                 u.Role.Equals("Manager", StringComparison.OrdinalIgnoreCase))).ToList()
        If mgmt.Count = 0 Then Return adminUserId
        Return mgmt(rng.Next(mgmt.Count)).Id
    End Function

    Private Shared Function PickPaymentMethod(methods As String(), rng As Random) As String
        Dim roll As Integer = rng.Next(1, 101)
        If roll <= 50 Then Return "Cash"
        If roll <= 70 Then Return "GCash"
        If roll <= 85 Then Return "Maya"
        Return "Card"
    End Function

    Private Shared Function PickCustomer(pool As String(), rng As Random) As String
        If rng.Next(0, 5) < 3 Then Return ""   ' 60% walk-in
        Return pool(rng.Next(pool.Length))
    End Function

    Private Shared Function SeedAuditLogs(conn As SqliteConnection, tran As SqliteTransaction,
                                          products As List(Of DemoProduct), users As List(Of DemoUser),
                                          adminUserId As Integer) As Integer
        Dim rng As New Random(2026)
        Dim events As New List(Of (At As DateTime, ByUser As Integer, Action As String, Details As String))()
        Dim active As List(Of DemoUser) = users.Where(Function(u) u.IsActive).ToList()
        If active.Count = 0 Then active.AddRange(users)

        Dim startDate As DateTime = DateTime.Today.AddYears(-3)
        Dim endDate As DateTime = DateTime.Today

        ' Product added events
        For Each p As DemoProduct In products
            events.Add((p.CreatedAt, adminUserId, "Product Added", $"New product added: {p.Name} ({p.Category})"))
        Next

        ' Staff / manager added events
        For Each u As DemoUser In users.Where(Function(x) Not x.Username.Equals("admin", StringComparison.OrdinalIgnoreCase))
            events.Add((u.CreatedAt, adminUserId, "Staff Added", $"New staff member added: {u.FullName} ({u.Role})"))
        Next

        ' Inactive admin deactivated
        Dim inactive As DemoUser = users.FirstOrDefault(Function(u) Not u.IsActive)
        If inactive IsNot Nothing Then
            events.Add((inactive.CreatedAt.AddYears(1), adminUserId, "Account Deactivated", $"User account deactivated: {inactive.Username}"))
        End If

        ' Weekly activity across the 3 years
        Dim totalDays As Integer = (endDate - startDate).Days
        For week As Integer = 0 To totalDays \ 7
            Dim dt As DateTime = startDate.AddDays(week * 7)
            If dt > endDate Then Exit For

            Dim numLogins As Integer = rng.Next(1, 3)
            For i As Integer = 0 To numLogins - 1
                Dim u As DemoUser = active(rng.Next(active.Count))
                events.Add((dt.Date.AddHours(rng.Next(8, 19)).AddMinutes(rng.Next(0, 60)), u.Id, "Login", "User logged in successfully"))
            Next

            If rng.Next(0, 3) = 0 Then
                Dim u As DemoUser = active(rng.Next(active.Count))
                events.Add((dt.Date.AddHours(rng.Next(17, 20)).AddMinutes(rng.Next(0, 60)), u.Id, "Logout", "User logged out"))
            End If

            If rng.Next(0, 6) = 0 AndAlso products.Count > 0 Then
                Dim p As DemoProduct = products(rng.Next(products.Count))
                events.Add((dt.Date.AddHours(10).AddMinutes(rng.Next(0, 60)), adminUserId, "Price Updated", $"Updated selling price for {p.Name}"))
            End If

            If rng.Next(0, 10) = 0 AndAlso products.Count > 0 Then
                Dim p As DemoProduct = products(rng.Next(products.Count))
                events.Add((dt.Date.AddHours(9), adminUserId, "Stock Adjustment", $"Manual stock adjustment for {p.Name}"))
            End If
        Next

        ' Final seeding event
        events.Add((DateTime.Now, adminUserId, "Demo Data Seeded", "Demo data was loaded via System Settings"))

        events.Sort(Function(a, b) a.At.CompareTo(b.At))

        Dim count As Integer = 0
        For Each ev In events
            Using cmd As New SqliteCommand(
                "INSERT INTO AuditLog (UserID, Action, Details, ActionTime) VALUES (@user, @action, @details, @time)", conn, tran)
                cmd.Parameters.AddWithValue("@user", ev.ByUser)
                cmd.Parameters.AddWithValue("@action", ev.Action)
                cmd.Parameters.AddWithValue("@details", ev.Details)
                cmd.Parameters.AddWithValue("@time", ev.At)
                cmd.ExecuteNonQuery()
            End Using
            count += 1
        Next
        Return count
    End Function

End Class
