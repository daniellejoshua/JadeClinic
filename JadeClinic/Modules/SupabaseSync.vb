Imports System.Configuration
Imports System.Globalization
Imports System.IO
Imports System.Collections.Generic
Imports System.Threading.Tasks
Imports Microsoft.Data.Sqlite
Imports Npgsql
Imports NpgsqlTypes
Imports Amazon.S3
Imports Amazon.S3.Model
Imports Amazon.Runtime

' Syncs the central LAN SQLite database to Supabase (Postgres).
' Read the connection string from (in order):
'   1. Environment variable JADECLINIC_SUPABASE_DSN
'   2. %LocalAppData%\JadeClinic\supabase.config.json  (never committed)
'   3. app.config -> SupabaseConnectionString (placeholder)
'
' Credentials are NEVER committed. See PLAN.md section 9.
Public Module SupabaseSync

    Private Const ConfigFileName As String = "supabase.config.json"

    ' ------------------------------------------------------------
    ' Configuration
    ' ------------------------------------------------------------
    Public Function GetConfigPath() As String
        Return Path.Combine(Connection.GetDatabaseFolder(), ConfigFileName)
    End Function

    Public Function GetSupabaseConnectionString() As String
        ' 1. Environment variable override
        Dim envDsn As String = Environment.GetEnvironmentVariable("JADECLINIC_SUPABASE_DSN")
        If Not String.IsNullOrWhiteSpace(envDsn) Then
            Return envDsn
        End If

        ' 2. Local config file next to the database
        Try
            Dim configPath As String = GetConfigPath()
            If File.Exists(configPath) Then
                Dim json As String = File.ReadAllText(configPath)
                Dim doc As Newtonsoft.Json.Linq.JObject = Newtonsoft.Json.Linq.JObject.Parse(json)
                Dim cs As String = doc("supabaseConnectionString")?.ToString()
                If Not String.IsNullOrWhiteSpace(cs) Then
                    Return cs
                End If
            End If
        Catch ex As Exception
            Console.WriteLine($"Note: Could not read {ConfigFileName}: {ex.Message}")
        End Try

        ' 3. app.config fallback (placeholder, normally empty)
        Dim configCs As String = ConfigurationManager.AppSettings("SupabaseConnectionString")
        If Not String.IsNullOrWhiteSpace(configCs) Then
            Return configCs
        End If

        Throw New InvalidOperationException(
            "Supabase is not configured. Set the JADECLINIC_SUPABASE_DSN environment variable " &
            "or create the file: " & GetConfigPath())
    End Function

    ' ------------------------------------------------------------
    ' S3 image upload (Supabase Storage, S3-compatible API)
    ' ------------------------------------------------------------
    Private Function GetS3Config() As Dictionary(Of String, String)
        Try
            Dim configPath As String = GetConfigPath()
            If Not File.Exists(configPath) Then Return Nothing
            Dim json As String = File.ReadAllText(configPath)
            Dim doc As Newtonsoft.Json.Linq.JObject = Newtonsoft.Json.Linq.JObject.Parse(json)
            Dim s3 As Newtonsoft.Json.Linq.JObject = doc("s3")
            If s3 Is Nothing Then Return Nothing
            Dim cfg As New Dictionary(Of String, String)
            cfg("endpoint") = s3("endpoint")?.ToString()
            cfg("accessKeyId") = s3("accessKeyId")?.ToString()
            cfg("secretAccessKey") = s3("secretAccessKey")?.ToString()
            cfg("region") = s3("region")?.ToString()
            cfg("bucket") = s3("bucket")?.ToString()
            If String.IsNullOrWhiteSpace(cfg("bucket")) Then Return Nothing
            Return cfg
        Catch ex As Exception
            Console.WriteLine($"Note: Could not read S3 config: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Private Function GetContentType(path As String) As String
        Dim ext As String = Path.GetExtension(path).ToLowerInvariant()
        Select Case ext
            Case ".jpg", ".jpeg" : Return "image/jpeg"
            Case ".png" : Return "image/png"
            Case ".gif" : Return "image/gif"
            Case ".webp" : Return "image/webp"
            Case ".bmp" : Return "image/bmp"
            Case ".ico" : Return "image/x-icon"
            Case Else : Return "application/octet-stream"
        End Select
    End Function

    Private Function BuildPublicUrl(endpoint As String, bucket As String, key As String) As String
        ' endpoint: https://<ref>.storage.supabase.co/storage/v1/s3
        ' public:   https://<ref>.supabase.co/storage/v1/object/public/<bucket>/<key>
        Dim publicBase As String = endpoint.Replace(".storage.supabase.co/storage/v1/s3",
                                                    ".supabase.co/storage/v1/object/public")
        Return $"{publicBase}/{bucket}/{key}"
    End Function

    Private Function UploadImageToBucket(localFile As String, remoteSubfolder As String, remoteName As String) As String
        ' remoteSubfolder: "avatar", "product", "company"  -> stored under images/<subfolder>/
        ' Returns public URL, or Nothing when skipped/failed (sync continues without images).
        If String.IsNullOrWhiteSpace(localFile) OrElse Not File.Exists(localFile) Then
            Return Nothing
        End If

        Dim cfg As Dictionary(Of String, String) = GetS3Config()
        If cfg Is Nothing Then Return Nothing

        Dim accessKey As String = cfg("accessKeyId")
        Dim secretKey As String = cfg("secretAccessKey")
        If String.IsNullOrWhiteSpace(accessKey) OrElse String.IsNullOrWhiteSpace(secretKey) Then
            Console.WriteLine("Note: S3 keys not configured - skipping image upload.")
            Return Nothing
        End If

        Dim key As String = $"images/{remoteSubfolder}/{remoteName}"

        Try
            Dim s3Config As New AmazonS3Config()
            s3Config.ServiceURL = cfg("endpoint")
            s3Config.ForcePathStyle = True
            s3Config.AuthenticationRegion = cfg("region")

            Using client As New AmazonS3Client(accessKey, secretKey, s3Config)
                Dim req As New PutObjectRequest()
                req.BucketName = cfg("bucket")
                req.Key = key
                req.FilePath = localFile
                req.ContentType = GetContentType(localFile)
                client.PutObject(req)
            End Using

            Return BuildPublicUrl(cfg("endpoint"), cfg("bucket"), key)
        Catch ex As Exception
            Console.WriteLine($"Image upload failed for {key}: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Public Function TestConnection() As Boolean
        Dim cs As String = GetSupabaseConnectionString()
        Using conn As New NpgsqlConnection(cs)
            conn.Open()
            Using cmd As New NpgsqlCommand("SELECT 1", conn)
                cmd.ExecuteScalar()
            End Using
        End Using
        Return True
    End Function

    ' ------------------------------------------------------------
    ' Full sync (all 8 data tables, in FK-safe order)
    ' ------------------------------------------------------------
    Public Function RunFullSync() As SyncResult
        Dim result As New SyncResult()
        Try
            Dim cs As String = GetSupabaseConnectionString()
            Dim localConnStr As String = Connection.GetConnectionString()

            Using pg As New NpgsqlConnection(cs)
                pg.Open()

                Using local As New SqliteConnection(localConnStr)
                    local.Open()

                    Dim supplierIdMap As New Dictionary(Of Integer, Integer)()   ' LAN SupplierID -> supabase id
                    Dim userIdMap As New Dictionary(Of Integer, Integer)()       ' LAN UserID -> supabase id
                    Dim productIdMap As New Dictionary(Of Integer, Integer)()    ' LAN ProductID -> supabase id
                    Dim saleIdMap As New Dictionary(Of Integer, Integer)()       ' LAN SaleID -> supabase id

                    result.Summary.Add($"suppliers: {SyncSuppliers(local, pg, supplierIdMap)}")
                    result.Summary.Add($"users: {SyncUsers(local, pg, userIdMap)}")
                    result.Summary.Add($"products: {SyncProducts(local, pg, supplierIdMap, productIdMap)}")
                    result.Summary.Add($"sales: {SyncSales(local, pg, userIdMap, saleIdMap)}")
                    result.Summary.Add($"sale_items: {SyncSaleItems(local, pg, saleIdMap, productIdMap)}")
                    result.Summary.Add($"inventory_logs: {SyncInventoryLogs(local, pg, productIdMap, supplierIdMap, userIdMap)}")
                    result.Summary.Add($"audit_logs: {SyncAuditLogs(local, pg, userIdMap)}")
                    result.Summary.Add($"company_settings: {SyncCompanySettings(local, pg)}")
                End Using
            End Using

            result.Success = True
        Catch ex As Exception
            result.ErrorMessage = ex.Message
            Console.WriteLine($"Sync error: {ex}")
        End Try
        Return result
    End Function

    ' ------------------------------------------------------------
    ' Table syncs
    ' ------------------------------------------------------------
    Private Function SyncSuppliers(local As SqliteConnection, pg As NpgsqlConnection,
                                   idMap As Dictionary(Of Integer, Integer)) As Integer
        Dim count As Integer = 0
        Using cmd As New SqliteCommand(
            "SELECT SupplierID, SupplierCode, SupplierName, ContactPerson, Phone, Email, IsActive FROM Suppliers", local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("SupplierID"))

                    Using pgCmd As New NpgsqlCommand(
                        "INSERT INTO suppliers (local_id, supplier_code, supplier_name, contact_person, phone, email, is_active, created_at, updated_at, synced_at) " &
                        "VALUES (@lid, @code, @name, @contact, @phone, @email, @active, COALESCE(@created, NOW()), COALESCE(@updated, NOW()), NOW()) " &
                        "ON CONFLICT (local_id) DO UPDATE SET " &
                        "supplier_code = EXCLUDED.supplier_code, supplier_name = EXCLUDED.supplier_name, " &
                        "contact_person = EXCLUDED.contact_person, phone = EXCLUDED.phone, email = EXCLUDED.email, " &
                        "is_active = EXCLUDED.is_active, updated_at = EXCLUDED.updated_at, synced_at = NOW() " &
                        "RETURNING id", pg)

                        AddParam(pgCmd, "@lid", localId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@code", GetStr(reader, "SupplierCode"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@name", GetStr(reader, "SupplierName"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@contact", GetStr(reader, "ContactPerson"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@phone", GetStr(reader, "Phone"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@email", GetStr(reader, "Email"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@active", GetBoolDb(reader, "IsActive"), NpgsqlDbType.Boolean)
                        AddParam(pgCmd, "@created", GetDateDb(reader, "CreatedAt"), NpgsqlDbType.TimestampTz)
                        AddParam(pgCmd, "@updated", GetDateDb(reader, "UpdatedAt"), NpgsqlDbType.TimestampTz)

                        idMap(localId) = Convert.ToInt32(pgCmd.ExecuteScalar())
                        count += 1
                    End Using
                End While
            End Using
        End Using
        Return count
    End Function

    Private Function SyncUsers(local As SqliteConnection, pg As NpgsqlConnection,
                               idMap As Dictionary(Of Integer, Integer)) As Integer
        Dim count As Integer = 0
        ' IMPORTANT: PasswordHash, pin, QRCode, Passkeys are intentionally NOT synced.
        Using cmd As New SqliteCommand(
            "SELECT UserID, Username, FullName, UserRole, IsActive, Email, Phone, PhotoPath, CreatedAt, UpdatedAt FROM Users", local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("UserID"))

                    Dim photoUrl As Object = DBNull.Value
                    Dim rawPhoto = GetStr(reader, "PhotoPath")
                    If Not IsDBNull(rawPhoto) Then
                        Dim photoFileName As String = rawPhoto.ToString()
                        If Not String.IsNullOrWhiteSpace(photoFileName) Then
                            Dim localPhoto As String = Path.Combine(Connection.GetImagesFolder("users"), photoFileName)
                            Dim url = UploadImageToBucket(localPhoto, "avatar", photoFileName)
                            If url IsNot Nothing Then photoUrl = url
                        End If
                    End If

                    Using pgCmd As New NpgsqlCommand(
                        "INSERT INTO users (local_id, username, full_name, user_role, is_active, email, phone, photo_url, created_at, updated_at, synced_at) " &
                        "VALUES (@lid, @username, @fullname, @role, @active, @email, @phone, @photourl, COALESCE(@created, NOW()), COALESCE(@updated, NOW()), NOW()) " &
                        "ON CONFLICT (local_id) DO UPDATE SET " &
                        "username = EXCLUDED.username, full_name = EXCLUDED.full_name, user_role = EXCLUDED.user_role, " &
                        "is_active = EXCLUDED.is_active, email = EXCLUDED.email, phone = EXCLUDED.phone, " &
                        "photo_url = COALESCE(EXCLUDED.photo_url, users.photo_url), " &
                        "updated_at = EXCLUDED.updated_at, synced_at = NOW() " &
                        "RETURNING id", pg)

                        AddParam(pgCmd, "@lid", localId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@username", GetStr(reader, "Username"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@fullname", GetStr(reader, "FullName"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@role", GetStr(reader, "UserRole"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@active", GetBoolDb(reader, "IsActive"), NpgsqlDbType.Boolean)
                        AddParam(pgCmd, "@email", GetStr(reader, "Email"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@phone", GetStr(reader, "Phone"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@photourl", photoUrl, NpgsqlDbType.Text)
                        AddParam(pgCmd, "@created", GetDateDb(reader, "CreatedAt"), NpgsqlDbType.TimestampTz)
                        AddParam(pgCmd, "@updated", GetDateDb(reader, "UpdatedAt"), NpgsqlDbType.TimestampTz)

                        idMap(localId) = Convert.ToInt32(pgCmd.ExecuteScalar())
                        count += 1
                    End Using
                End While
            End Using
        End Using
        Return count
    End Function

    Private Function SyncProducts(local As SqliteConnection, pg As NpgsqlConnection,
                                  supplierIdMap As Dictionary(Of Integer, Integer),
                                  idMap As Dictionary(Of Integer, Integer)) As Integer
        Dim count As Integer = 0
        Using cmd As New SqliteCommand(
            "SELECT ProductID, ProductCode, ProductName, Category, Unit, CurrentStock, ReorderLevel, CostPrice, " &
            "SellingPrice, WholesalePrice, SupplierID, IsActive, Created, UpdatedAt FROM Products", local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("ProductID"))

                    ' Supplier FK: resolve LAN SupplierID -> supabase id (handle schema variants safely)
                    Dim supplierId As Object = DBNull.Value
                    Dim rawSupplier = GetValue(reader, "SupplierID")
                    If Not IsDBNull(rawSupplier) Then
                        Dim lanSupplierId As Integer = Convert.ToInt32(rawSupplier)
                        If supplierIdMap.ContainsKey(lanSupplierId) Then
                            supplierId = supplierIdMap(lanSupplierId)
                        End If
                    End If

                    ' HasExpiry / ExpiryDate only exist on some LAN schemas
                    Dim hasExpiry As Object = GetBoolDb(reader, "HasExpiry")
                    Dim expiryDate As Object = GetStr(reader, "ExpiryDate")
                    If IsDBNull(hasExpiry) Then hasExpiry = False

                    ' First product image (from ProductImages via mapping)
                    Dim imageUrl As Object = DBNull.Value
                    Dim firstImagePath As String = GetFirstProductImagePath(local, localId)
                    If Not String.IsNullOrWhiteSpace(firstImagePath) Then
                        Dim localImage As String = Path.Combine(Connection.GetImagesFolder("products"), firstImagePath)
                        Dim url = UploadImageToBucket(localImage, "product", firstImagePath)
                        If url IsNot Nothing Then imageUrl = url
                    End If

                    Using pgCmd As New NpgsqlCommand(
                        "INSERT INTO products (local_id, product_code, product_name, category, unit, current_stock, reorder_level, " &
                        "has_expiry, expiry_date, cost_price, selling_price, wholesale_price, supplier_id, image_url, is_active, " &
                        "created_at, updated_at, synced_at) " &
                        "VALUES (@lid, @code, @name, @category, @unit, COALESCE(@stock, 0), COALESCE(@reorder, 0), COALESCE(@has_expiry, FALSE), @expiry, " &
                        "@cost, @sell, @wholesale, @supplier, @imageurl, @active, COALESCE(@created, NOW()), COALESCE(@updated, NOW()), NOW()) " &
                        "ON CONFLICT (local_id) DO UPDATE SET " &
                        "product_code = EXCLUDED.product_code, product_name = EXCLUDED.product_name, category = EXCLUDED.category, " &
                        "unit = EXCLUDED.unit, current_stock = EXCLUDED.current_stock, reorder_level = EXCLUDED.reorder_level, " &
                        "has_expiry = EXCLUDED.has_expiry, expiry_date = EXCLUDED.expiry_date, cost_price = EXCLUDED.cost_price, " &
                        "selling_price = EXCLUDED.selling_price, wholesale_price = EXCLUDED.wholesale_price, " &
                        "supplier_id = EXCLUDED.supplier_id, is_active = EXCLUDED.is_active, " &
                        "image_url = COALESCE(EXCLUDED.image_url, products.image_url), " &
                        "updated_at = EXCLUDED.updated_at, synced_at = NOW() " &
                        "RETURNING id", pg)

                        AddParam(pgCmd, "@lid", localId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@code", GetStr(reader, "ProductCode"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@name", GetStr(reader, "ProductName"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@category", GetStr(reader, "Category"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@unit", GetStr(reader, "Unit"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@stock", GetIntDb(reader, "CurrentStock"), NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@reorder", GetIntDb(reader, "ReorderLevel"), NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@has_expiry", hasExpiry, NpgsqlDbType.Boolean)
                        AddParam(pgCmd, "@expiry", expiryDate, NpgsqlDbType.Text)
                        AddParam(pgCmd, "@cost", GetDecimalDb(reader, "CostPrice"), NpgsqlDbType.Numeric)
                        AddParam(pgCmd, "@sell", GetDecimalDb(reader, "SellingPrice"), NpgsqlDbType.Numeric)
                        AddParam(pgCmd, "@wholesale", GetDecimalDb(reader, "WholesalePrice"), NpgsqlDbType.Numeric)
                        AddParam(pgCmd, "@supplier", supplierId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@imageurl", imageUrl, NpgsqlDbType.Text)
                        AddParam(pgCmd, "@active", GetBoolDb(reader, "IsActive"), NpgsqlDbType.Boolean)
                        AddParam(pgCmd, "@created", GetDateDb(reader, "Created"), NpgsqlDbType.TimestampTz)
                        AddParam(pgCmd, "@updated", GetDateDb(reader, "UpdatedAt"), NpgsqlDbType.TimestampTz)

                        idMap(localId) = Convert.ToInt32(pgCmd.ExecuteScalar())
                        count += 1
                    End Using
                End While
            End Using
        End Using
        Return count
    End Function

    Private Function SyncSales(local As SqliteConnection, pg As NpgsqlConnection,
                               userIdMap As Dictionary(Of Integer, Integer),
                               idMap As Dictionary(Of Integer, Integer)) As Integer
        Dim count As Integer = 0
        Using cmd As New SqliteCommand(
            "SELECT SaleID, SaleNumber, SaleDate, CustomerName, CustomerTIN, UserID, TotalAmount, AmountPaid, PaymentMethod, " &
            "IsVoid, Status, DiscountType, DiscountAmount, Reference, SalesData FROM Sales", local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("SaleID"))

                    Dim userId As Object = DBNull.Value
                    Dim rawUser = GetValue(reader, "UserID")
                    If Not IsDBNull(rawUser) Then
                        Dim lanUserId As Integer = Convert.ToInt32(rawUser)
                        If userIdMap.ContainsKey(lanUserId) Then
                            userId = userIdMap(lanUserId)
                        End If
                    End If

                    Dim salesData As Object = GetStr(reader, "SalesData")

                    Using pgCmd As New NpgsqlCommand(
                        "INSERT INTO sales (local_id, sale_number, sale_date, customer_name, customer_tin, user_id, total_amount, " &
                        "amount_paid, payment_method, reference, is_void, status, discount_type, discount_amount, sales_data, created_at, synced_at) " &
                        "VALUES (@lid, @salenum, COALESCE(@saledate, NOW()), @customer, @tin, @user, COALESCE(@total, 0), COALESCE(@paid, 0), " &
                        "COALESCE(@method, 'Cash'), @reference, COALESCE(@isvoid, FALSE), COALESCE(@status, 'Completed'), @disctype, COALESCE(@discamt, 0), " &
                        "@salesdata, COALESCE(@saledate, NOW()), NOW()) " &
                        "ON CONFLICT (local_id) DO UPDATE SET " &
                        "sale_number = EXCLUDED.sale_number, sale_date = EXCLUDED.sale_date, customer_name = EXCLUDED.customer_name, " &
                        "customer_tin = EXCLUDED.customer_tin, user_id = EXCLUDED.user_id, total_amount = EXCLUDED.total_amount, " &
                        "amount_paid = EXCLUDED.amount_paid, payment_method = EXCLUDED.payment_method, reference = EXCLUDED.reference, " &
                        "is_void = EXCLUDED.is_void, status = EXCLUDED.status, discount_type = EXCLUDED.discount_type, " &
                        "discount_amount = EXCLUDED.discount_amount, sales_data = EXCLUDED.sales_data, synced_at = NOW() " &
                        "RETURNING id", pg)

                        AddParam(pgCmd, "@lid", localId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@salenum", GetStr(reader, "SaleNumber"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@saledate", GetDateDb(reader, "SaleDate"), NpgsqlDbType.TimestampTz)
                        AddParam(pgCmd, "@customer", GetStr(reader, "CustomerName"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@tin", GetStr(reader, "CustomerTIN"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@user", userId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@total", GetDecimalDb(reader, "TotalAmount"), NpgsqlDbType.Numeric)
                        AddParam(pgCmd, "@paid", GetDecimalDb(reader, "AmountPaid"), NpgsqlDbType.Numeric)
                        AddParam(pgCmd, "@method", GetStr(reader, "PaymentMethod"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@reference", GetStr(reader, "Reference"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@isvoid", GetBoolDb(reader, "IsVoid"), NpgsqlDbType.Boolean)
                        AddParam(pgCmd, "@status", GetStr(reader, "Status"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@disctype", GetStr(reader, "DiscountType"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@discamt", GetDecimalDb(reader, "DiscountAmount"), NpgsqlDbType.Numeric)
                        AddParam(pgCmd, "@salesdata", salesData, NpgsqlDbType.Jsonb)

                        idMap(localId) = Convert.ToInt32(pgCmd.ExecuteScalar())
                        count += 1
                    End Using
                End While
            End Using
        End Using
        Return count
    End Function

    Private Function SyncSaleItems(local As SqliteConnection, pg As NpgsqlConnection,
                                   saleIdMap As Dictionary(Of Integer, Integer),
                                   productIdMap As Dictionary(Of Integer, Integer)) As Integer
        Dim count As Integer = 0
        Using cmd As New SqliteCommand(
            "SELECT SaleItemID, SaleID, ProductID, Quantity, UnitPrice, OriginalUnitPrice, LineDiscountAmount, SubTotal FROM SaleItems", local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("SaleItemID"))

                    ' sale_id is NOT NULL in supabase — skip orphans
                    Dim lanSaleId As Integer = Convert.ToInt32(reader("SaleID"))
                    If Not saleIdMap.ContainsKey(lanSaleId) Then
                        Continue While
                    End If
                    Dim saleId As Object = saleIdMap(lanSaleId)

                    Dim productId As Object = DBNull.Value
                    Dim rawProduct = GetValue(reader, "ProductID")
                    If Not IsDBNull(rawProduct) Then
                        Dim lanProductId As Integer = Convert.ToInt32(rawProduct)
                        If productIdMap.ContainsKey(lanProductId) Then
                            productId = productIdMap(lanProductId)
                        End If
                    End If

                    Using pgCmd As New NpgsqlCommand(
                        "INSERT INTO sale_items (local_id, sale_id, product_id, quantity, unit_price, original_unit_price, line_discount, sub_total, created_at, synced_at) " &
                        "VALUES (@lid, @saleid, @productid, @qty, @unitprice, @originalprice, COALESCE(@linedisc, 0), @subtotal, NOW(), NOW()) " &
                        "ON CONFLICT (local_id) DO UPDATE SET " &
                        "sale_id = EXCLUDED.sale_id, product_id = EXCLUDED.product_id, quantity = EXCLUDED.quantity, " &
                        "unit_price = EXCLUDED.unit_price, original_unit_price = EXCLUDED.original_unit_price, " &
                        "line_discount = EXCLUDED.line_discount, sub_total = EXCLUDED.sub_total, synced_at = NOW() " &
                        "RETURNING id", pg)

                        AddParam(pgCmd, "@lid", localId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@saleid", saleId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@productid", productId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@qty", GetIntDb(reader, "Quantity"), NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@unitprice", GetDecimalDb(reader, "UnitPrice"), NpgsqlDbType.Numeric)
                        AddParam(pgCmd, "@originalprice", GetDecimalDb(reader, "OriginalUnitPrice"), NpgsqlDbType.Numeric)
                        AddParam(pgCmd, "@linedisc", GetDecimalDb(reader, "LineDiscountAmount"), NpgsqlDbType.Numeric)
                        AddParam(pgCmd, "@subtotal", GetDecimalDb(reader, "SubTotal"), NpgsqlDbType.Numeric)

                        idMap(localId) = Convert.ToInt32(pgCmd.ExecuteScalar())
                        count += 1
                    End Using
                End While
            End Using
        End Using
        Return count
    End Function

    Private Function SyncInventoryLogs(local As SqliteConnection, pg As NpgsqlConnection,
                                       productIdMap As Dictionary(Of Integer, Integer),
                                       supplierIdMap As Dictionary(Of Integer, Integer),
                                       userIdMap As Dictionary(Of Integer, Integer)) As Integer
        Dim count As Integer = 0
        Using cmd As New SqliteCommand(
            "SELECT LogID, ProductID, TransactionType, Quantity, PreviousStock, NewStock, BatchNumber, ExpiryDate, " &
            "SupplierID, UserID, Reference, Notes, CreatedAt FROM InventoryLog", local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("LogID"))

                    Dim productId As Object = ResolveMapId(reader, "ProductID", productIdMap)
                    Dim supplierId As Object = ResolveMapId(reader, "SupplierID", supplierIdMap)
                    Dim userId As Object = ResolveMapId(reader, "UserID", userIdMap)

                    Using pgCmd As New NpgsqlCommand(
                        "INSERT INTO inventory_logs (local_id, product_id, transaction_type, quantity, previous_stock, new_stock, " &
                        "batch_number, expiry_date, supplier_id, user_id, reference, notes, created_at, synced_at) " &
                        "VALUES (@lid, @productid, @type, @qty, @prevstock, @newstock, @batch, @expiry, @supplier, @user, @ref, @notes, " &
                        "COALESCE(@created, NOW()), NOW()) " &
                        "ON CONFLICT (local_id) DO UPDATE SET " &
                        "product_id = EXCLUDED.product_id, transaction_type = EXCLUDED.transaction_type, quantity = EXCLUDED.quantity, " &
                        "previous_stock = EXCLUDED.previous_stock, new_stock = EXCLUDED.new_stock, batch_number = EXCLUDED.batch_number, " &
                        "expiry_date = EXCLUDED.expiry_date, supplier_id = EXCLUDED.supplier_id, user_id = EXCLUDED.user_id, " &
                        "reference = EXCLUDED.reference, notes = EXCLUDED.notes, synced_at = NOW() " &
                        "RETURNING id", pg)

                        AddParam(pgCmd, "@lid", localId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@productid", productId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@type", GetStr(reader, "TransactionType"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@qty", GetIntDb(reader, "Quantity"), NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@prevstock", GetIntDb(reader, "PreviousStock"), NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@newstock", GetIntDb(reader, "NewStock"), NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@batch", GetStr(reader, "BatchNumber"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@expiry", GetStr(reader, "ExpiryDate"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@supplier", supplierId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@user", userId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@ref", GetStr(reader, "Reference"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@notes", GetStr(reader, "Notes"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@created", GetDateDb(reader, "CreatedAt"), NpgsqlDbType.TimestampTz)

                        idMap(localId) = Convert.ToInt32(pgCmd.ExecuteScalar())
                        count += 1
                    End Using
                End While
            End Using
        End Using
        Return count
    End Function

    Private Function SyncAuditLogs(local As SqliteConnection, pg As NpgsqlConnection,
                                   userIdMap As Dictionary(Of Integer, Integer)) As Integer
        Dim count As Integer = 0
        Using cmd As New SqliteCommand(
            "SELECT AuditID, Action, Details, ActionTime, UserID FROM AuditLog", local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("AuditID"))
                    Dim userId As Object = ResolveMapId(reader, "UserID", userIdMap)

                    Using pgCmd As New NpgsqlCommand(
                        "INSERT INTO audit_logs (local_id, action, details, user_id, action_time, synced_at) " &
                        "VALUES (@lid, @action, @details, @user, COALESCE(@time, NOW()), NOW()) " &
                        "ON CONFLICT (local_id) DO UPDATE SET " &
                        "action = EXCLUDED.action, details = EXCLUDED.details, user_id = EXCLUDED.user_id, " &
                        "action_time = EXCLUDED.action_time, synced_at = NOW() " &
                        "RETURNING id", pg)

                        AddParam(pgCmd, "@lid", localId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@action", GetStr(reader, "Action"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@details", GetStr(reader, "Details"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@user", userId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@time", GetDateDb(reader, "ActionTime"), NpgsqlDbType.TimestampTz)

                        pgCmd.ExecuteNonQuery()
                        count += 1
                    End Using
                End While
            End Using
        End Using
        Return count
    End Function

    Private Function SyncCompanySettings(local As SqliteConnection, pg As NpgsqlConnection) As Integer
        Dim count As Integer = 0
        ' Only the active settings row is synced (SettingID becomes local_id)
        Using cmd As New SqliteCommand(
            "SELECT SettingID, CompanyName, TIN, Address, Phone, Email, Website, LogoPath, BIRAuthNumber, PTUNumber, " &
            "ValidityYears, ReceiptFooter, CompanyHours, IsActive, DateCreated, LastModified " &
            "FROM CompanySettings WHERE IsActive = 1 ORDER BY DateCreated DESC LIMIT 1", local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("SettingID"))

                    Dim logoUrl As Object = DBNull.Value
                    Dim rawLogo = GetStr(reader, "LogoPath")
                    If Not IsDBNull(rawLogo) Then
                        Dim logoFileName As String = rawLogo.ToString()
                        If Not String.IsNullOrWhiteSpace(logoFileName) Then
                            Dim localLogo As String = Path.Combine(Connection.GetImagesFolder("company"), logoFileName)
                            Dim url = UploadImageToBucket(localLogo, "company", logoFileName)
                            If url IsNot Nothing Then logoUrl = url
                        End If
                    End If

                    Using pgCmd As New NpgsqlCommand(
                        "INSERT INTO company_settings (local_id, company_name, tin, address, phone, email, website, logo_url, " &
                        "bir_auth_number, ptu_number, validity_years, receipt_footer, company_hours, is_active, created_at, updated_at, synced_at) " &
                        "VALUES (@lid, @name, @tin, @address, @phone, @email, @website, @logourl, @bir, @ptu, COALESCE(@validity, 5), " &
                        "@footer, @hours, @active, COALESCE(@created, NOW()), COALESCE(@updated, NOW()), NOW()) " &
                        "ON CONFLICT (local_id) DO UPDATE SET " &
                        "company_name = EXCLUDED.company_name, tin = EXCLUDED.tin, address = EXCLUDED.address, phone = EXCLUDED.phone, " &
                        "email = EXCLUDED.email, website = EXCLUDED.website, bir_auth_number = EXCLUDED.bir_auth_number, " &
                        "ptu_number = EXCLUDED.ptu_number, validity_years = EXCLUDED.validity_years, " &
                        "receipt_footer = EXCLUDED.receipt_footer, company_hours = EXCLUDED.company_hours, " &
                        "is_active = EXCLUDED.is_active, logo_url = COALESCE(EXCLUDED.logo_url, company_settings.logo_url), " &
                        "updated_at = EXCLUDED.updated_at, synced_at = NOW() " &
                        "RETURNING id", pg)

                        AddParam(pgCmd, "@lid", localId, NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@name", GetStr(reader, "CompanyName"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@tin", GetStr(reader, "TIN"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@address", GetStr(reader, "Address"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@phone", GetStr(reader, "Phone"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@email", GetStr(reader, "Email"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@website", GetStr(reader, "Website"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@logourl", logoUrl, NpgsqlDbType.Text)
                        AddParam(pgCmd, "@bir", GetStr(reader, "BIRAuthNumber"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@ptu", GetStr(reader, "PTUNumber"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@validity", GetIntDb(reader, "ValidityYears"), NpgsqlDbType.Integer)
                        AddParam(pgCmd, "@footer", GetStr(reader, "ReceiptFooter"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@hours", GetStr(reader, "CompanyHours"), NpgsqlDbType.Text)
                        AddParam(pgCmd, "@active", GetBoolDb(reader, "IsActive"), NpgsqlDbType.Boolean)
                        AddParam(pgCmd, "@created", GetDateDb(reader, "DateCreated"), NpgsqlDbType.TimestampTz)
                        AddParam(pgCmd, "@updated", GetDateDb(reader, "LastModified"), NpgsqlDbType.TimestampTz)

                        pgCmd.ExecuteNonQuery()
                        count += 1
                    End Using
                End While
            End Using
        End Using
        Return count
    End Function

    ' ------------------------------------------------------------
    ' Helpers
    ' ------------------------------------------------------------
    Private Function GetFirstProductImagePath(local As SqliteConnection, productId As Integer) As String
        Try
            Using cmd As New SqliteCommand(
                "SELECT pi.FilePath FROM ProductImages pi " &
                "INNER JOIN ProductImageMapping pim ON pim.ImageID = pi.ImageID " &
                "WHERE pim.ProductID = @pid ORDER BY pi.ImageID LIMIT 1", local)
                cmd.Parameters.AddWithValue("@pid", productId)
                Dim raw As Object = cmd.ExecuteScalar()
                If raw Is Nothing OrElse IsDBNull(raw) Then Return ""
                Return raw.ToString()
            End Using
        Catch ex As Exception
            Console.WriteLine($"Note: Could not read product image path: {ex.Message}")
            Return ""
        End Try
    End Function

    Private Function ResolveMapId(reader As SqliteDataReader, column As String,
                                  idMap As Dictionary(Of Integer, Integer)) As Object
        Dim raw = GetValue(reader, column)
        If IsDBNull(raw) Then
            Return DBNull.Value
        End If
        Dim lanId As Integer = Convert.ToInt32(raw)
        If idMap.ContainsKey(lanId) Then
            Return CObj(idMap(lanId))
        End If
        Return DBNull.Value
    End Function

    Private Sub AddParam(cmd As NpgsqlCommand, name As String, value As Object, dbType As NpgsqlDbType)
        Dim p As NpgsqlParameter = cmd.Parameters.Add(name, dbType)
        p.Value = If(value Is Nothing OrElse IsDBNull(value), DBNull.Value, value)
    End Sub

    Private Function GetValue(reader As SqliteDataReader, name As String) As Object
        Try
            If reader.GetOrdinal(name) >= 0 Then
                Return reader(name)
            End If
        Catch
        End Try
        Return DBNull.Value
    End Function

    Private Function GetStr(reader As SqliteDataReader, name As String) As Object
        Dim v As Object = GetValue(reader, name)
        If v Is Nothing OrElse IsDBNull(v) Then
            Return DBNull.Value
        End If
        Dim s As String = v.ToString()
        If String.IsNullOrWhiteSpace(s) Then
            Return DBNull.Value
        End If
        Return CObj(s)
    End Function

    Private Function GetIntDb(reader As SqliteDataReader, name As String) As Object
        Dim v As Object = GetValue(reader, name)
        If v Is Nothing OrElse IsDBNull(v) Then
            Return DBNull.Value
        End If
        Return CObj(Convert.ToInt32(v))
    End Function

    Private Function GetDecimalDb(reader As SqliteDataReader, name As String) As Object
        Dim v As Object = GetValue(reader, name)
        If v Is Nothing OrElse IsDBNull(v) Then
            Return DBNull.Value
        End If
        Return CObj(Convert.ToDecimal(v))
    End Function

    Private Function GetBoolDb(reader As SqliteDataReader, name As String) As Object
        Dim v As Object = GetValue(reader, name)
        If v Is Nothing OrElse IsDBNull(v) Then
            Return DBNull.Value
        End If
        Return CObj(Convert.ToInt32(v) <> 0)
    End Function

    Private Function GetDateDb(reader As SqliteDataReader, name As String) As Object
        Dim v As Object = GetValue(reader, name)
        If v Is Nothing OrElse IsDBNull(v) Then
            Return DBNull.Value
        End If
        If TypeOf v Is DateTime Then
            Return v
        End If
        Dim s As String = v.ToString()
        Dim dt As DateTime
        If DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, dt) Then
            Return dt
        End If
        Return DBNull.Value
    End Function

End Module

Public Class SyncResult
    Public Property Success As Boolean = False
    Public Property Summary As List(Of String) = New List(Of String)()
    Public Property ErrorMessage As String = ""

    Public Overrides Function ToString() As String
        If Not Success Then
            Return $"Sync failed: {ErrorMessage}"
        End If
        Return String.Join(vbCrLf, Summary)
    End Function
End Class
