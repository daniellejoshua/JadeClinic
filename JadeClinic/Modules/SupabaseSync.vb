Imports System.Configuration
Imports System.Globalization
Imports System.IO
Imports System.Collections.Generic
Imports System.Threading.Tasks
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
    Private Const ChunkSize As Integer = 500

    ' ------------------------------------------------------------
    ' Chunked upsert SQL (multi-row INSERT ... ON CONFLICT)
    ' Placeholders ($1, $2, ...) are appended by UpsertChunk.
    ' ------------------------------------------------------------
    Private ReadOnly SuppliersInsert As String = "INSERT INTO suppliers (local_id, supplier_code, supplier_name, contact_person, phone, email, is_active, created_at, updated_at, synced_at)"
    Private ReadOnly SuppliersSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "supplier_code = EXCLUDED.supplier_code, supplier_name = EXCLUDED.supplier_name, " &
        "contact_person = EXCLUDED.contact_person, phone = EXCLUDED.phone, email = EXCLUDED.email, " &
        "is_active = EXCLUDED.is_active, updated_at = EXCLUDED.updated_at, synced_at = NOW() " &
        "RETURNING id, local_id"

    Private ReadOnly UsersInsert As String = "INSERT INTO users (local_id, username, full_name, user_role, is_active, email, phone, photo_url, employee_code, created_at, updated_at, synced_at)"
    Private ReadOnly UsersSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "username = EXCLUDED.username, full_name = EXCLUDED.full_name, user_role = EXCLUDED.user_role, " &
        "is_active = EXCLUDED.is_active, email = EXCLUDED.email, phone = EXCLUDED.phone, " &
        "photo_url = COALESCE(EXCLUDED.photo_url, users.photo_url), " &
        "employee_code = COALESCE(EXCLUDED.employee_code, users.employee_code), " &
        "updated_at = EXCLUDED.updated_at, synced_at = NOW() " &
        "RETURNING id, local_id"

    Private ReadOnly ProductsInsert As String = "INSERT INTO products (local_id, product_code, product_name, category, unit, current_stock, reorder_level, " &
        "has_expiry, expiry_date, cost_price, selling_price, wholesale_price, supplier_id, image_url, is_active, created_at, updated_at, synced_at)"
    Private ReadOnly ProductsSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "product_code = EXCLUDED.product_code, product_name = EXCLUDED.product_name, category = EXCLUDED.category, " &
        "unit = EXCLUDED.unit, current_stock = EXCLUDED.current_stock, reorder_level = EXCLUDED.reorder_level, " &
        "has_expiry = EXCLUDED.has_expiry, expiry_date = EXCLUDED.expiry_date, cost_price = EXCLUDED.cost_price, " &
        "selling_price = EXCLUDED.selling_price, wholesale_price = EXCLUDED.wholesale_price, " &
        "supplier_id = EXCLUDED.supplier_id, is_active = EXCLUDED.is_active, " &
        "image_url = COALESCE(EXCLUDED.image_url, products.image_url), " &
        "updated_at = EXCLUDED.updated_at, synced_at = NOW() " &
        "RETURNING id, local_id"

    Private ReadOnly SalesInsert As String = "INSERT INTO sales (local_id, sale_number, sale_date, customer_name, customer_tin, user_id, total_amount, " &
        "amount_paid, payment_method, reference, status, approved_by, abort_reason, discount_type, discount_amount, sales_data, created_at, synced_at)"
    Private ReadOnly SalesSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "sale_number = EXCLUDED.sale_number, sale_date = EXCLUDED.sale_date, customer_name = EXCLUDED.customer_name, " &
        "customer_tin = EXCLUDED.customer_tin, user_id = EXCLUDED.user_id, total_amount = EXCLUDED.total_amount, " &
        "amount_paid = EXCLUDED.amount_paid, payment_method = EXCLUDED.payment_method, reference = EXCLUDED.reference, " &
        "status = EXCLUDED.status, approved_by = EXCLUDED.approved_by, abort_reason = EXCLUDED.abort_reason, discount_type = EXCLUDED.discount_type, " &
        "discount_amount = EXCLUDED.discount_amount, sales_data = EXCLUDED.sales_data, synced_at = NOW() " &
        "RETURNING id, local_id"

    Private ReadOnly SaleItemsInsert As String = "INSERT INTO sale_items (local_id, sale_id, product_id, quantity, unit_price, original_unit_price, line_discount, sub_total, created_at, synced_at)"
    Private ReadOnly SaleItemsSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "sale_id = EXCLUDED.sale_id, product_id = EXCLUDED.product_id, quantity = EXCLUDED.quantity, " &
        "unit_price = EXCLUDED.unit_price, original_unit_price = EXCLUDED.original_unit_price, " &
        "line_discount = EXCLUDED.line_discount, sub_total = EXCLUDED.sub_total, synced_at = NOW() " &
        "RETURNING id, local_id"

    Private ReadOnly SalesReturnsInsert As String = "INSERT INTO sales_returns (local_id, sale_id, return_number, return_date, return_type, reason, " &
        "refund_amount, additional_payment_amount, settlement_method, settlement_reference, approved_by, approved_by_user_id, " &
        "processed_by, initiated_by, notes, status, synced_at)"
    Private ReadOnly SalesReturnsSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "sale_id = EXCLUDED.sale_id, return_number = EXCLUDED.return_number, return_date = EXCLUDED.return_date, " &
        "return_type = EXCLUDED.return_type, reason = EXCLUDED.reason, refund_amount = EXCLUDED.refund_amount, " &
        "additional_payment_amount = EXCLUDED.additional_payment_amount, settlement_method = EXCLUDED.settlement_method, " &
        "settlement_reference = EXCLUDED.settlement_reference, approved_by = EXCLUDED.approved_by, " &
        "approved_by_user_id = EXCLUDED.approved_by_user_id, processed_by = EXCLUDED.processed_by, initiated_by = EXCLUDED.initiated_by, notes = EXCLUDED.notes, " &
        "status = EXCLUDED.status, synced_at = NOW() " &
        "RETURNING id, local_id"

    Private ReadOnly SalesReturnItemsInsert As String = "INSERT INTO sales_return_items (local_id, return_id, original_sale_item_id, product_id, " &
        "quantity, unit_price, condition, disposition, line_refund_amount, return_credit_amount, replacement_product_id, replacement_quantity, replacement_unit_price, created_at, synced_at)"
    Private ReadOnly SalesReturnItemsSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "return_id = EXCLUDED.return_id, original_sale_item_id = EXCLUDED.original_sale_item_id, product_id = EXCLUDED.product_id, " &
        "quantity = EXCLUDED.quantity, unit_price = EXCLUDED.unit_price, condition = EXCLUDED.condition, " &
        "disposition = EXCLUDED.disposition, line_refund_amount = EXCLUDED.line_refund_amount, return_credit_amount = EXCLUDED.return_credit_amount, " &
        "replacement_product_id = EXCLUDED.replacement_product_id, replacement_quantity = EXCLUDED.replacement_quantity, " &
        "replacement_unit_price = EXCLUDED.replacement_unit_price, synced_at = NOW() " &
        "RETURNING id, local_id"

    Private ReadOnly ReturnQuarantineInsert As String = "INSERT INTO return_quarantine (local_id, return_item_id, product_id, quantity, item_condition, " &
        "status, received_at, resolved_at, resolved_by, notes, synced_at)"
    Private ReadOnly ReturnQuarantineSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "return_item_id = EXCLUDED.return_item_id, product_id = EXCLUDED.product_id, quantity = EXCLUDED.quantity, " &
        "item_condition = EXCLUDED.item_condition, status = EXCLUDED.status, received_at = EXCLUDED.received_at, " &
        "resolved_at = EXCLUDED.resolved_at, resolved_by = EXCLUDED.resolved_by, notes = EXCLUDED.notes, synced_at = NOW()"

    Private ReadOnly InventoryLogsInsert As String = "INSERT INTO inventory_logs (local_id, product_id, transaction_type, quantity, previous_stock, new_stock, " &
        "batch_number, expiry_date, supplier_id, user_id, reference, notes, created_at, synced_at)"
    Private ReadOnly InventoryLogsSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "product_id = EXCLUDED.product_id, transaction_type = EXCLUDED.transaction_type, quantity = EXCLUDED.quantity, " &
        "previous_stock = EXCLUDED.previous_stock, new_stock = EXCLUDED.new_stock, batch_number = EXCLUDED.batch_number, " &
        "expiry_date = EXCLUDED.expiry_date, supplier_id = EXCLUDED.supplier_id, user_id = EXCLUDED.user_id, " &
        "reference = EXCLUDED.reference, notes = EXCLUDED.notes, synced_at = NOW()"

    Private ReadOnly AuditLogsInsert As String = "INSERT INTO audit_logs (local_id, action, details, user_id, action_time, synced_at)"
    Private ReadOnly AuditLogsSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "action = EXCLUDED.action, details = EXCLUDED.details, user_id = EXCLUDED.user_id, " &
        "action_time = EXCLUDED.action_time, synced_at = NOW()"

    ' DDL to recreate cloud tables if they were dropped, so the sync self-heals.
    ' Mirrors database/supabase_schema.sql (CREATE TABLE IF NOT EXISTS is idempotent).
    Private ReadOnly EnsureSchemaStatements As String() = {
        "CREATE TABLE IF NOT EXISTS users (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "username TEXT NOT NULL, full_name TEXT NOT NULL, user_role TEXT DEFAULT 'Staff', " &
        "is_active BOOLEAN DEFAULT TRUE, email TEXT, phone TEXT, photo_url TEXT, employee_code TEXT, " &
         "created_at TIMESTAMPTZ DEFAULT NOW(), updated_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "ALTER TABLE users ADD COLUMN IF NOT EXISTS employee_code TEXT",
        "CREATE TABLE IF NOT EXISTS suppliers (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "supplier_code TEXT NOT NULL, supplier_name TEXT NOT NULL, contact_person TEXT, phone TEXT, email TEXT, " &
        "is_active BOOLEAN DEFAULT TRUE, created_at TIMESTAMPTZ DEFAULT NOW(), " &
        "updated_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS products (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "product_code TEXT NOT NULL, product_name TEXT NOT NULL, category TEXT, unit TEXT DEFAULT 'PCS', " &
        "current_stock INTEGER DEFAULT 0, reorder_level INTEGER DEFAULT 10, has_expiry BOOLEAN DEFAULT FALSE, " &
        "expiry_date TEXT, cost_price DECIMAL(10,2) NOT NULL, selling_price DECIMAL(10,2) NOT NULL, " &
        "wholesale_price DECIMAL(10,2), supplier_id INTEGER REFERENCES suppliers(id), image_url TEXT, " &
        "is_active BOOLEAN DEFAULT TRUE, created_at TIMESTAMPTZ DEFAULT NOW(), " &
        "updated_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS sales (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "sale_number TEXT, sale_date TIMESTAMPTZ DEFAULT NOW(), customer_name TEXT, customer_tin TEXT, " &
        "user_id INTEGER REFERENCES users(id), total_amount DECIMAL(10,2) DEFAULT 0, " &
        "amount_paid DECIMAL(10,2) DEFAULT 0, payment_method TEXT DEFAULT 'Cash', reference TEXT, " &
        "status TEXT DEFAULT 'Completed', approved_by TEXT, abort_reason TEXT, discount_type TEXT, " &
        "discount_amount DECIMAL(10,2) DEFAULT 0, sales_data JSONB, " &
        "created_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS sale_items (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "sale_id INTEGER REFERENCES sales(id) ON DELETE CASCADE, product_id INTEGER REFERENCES products(id), " &
        "quantity INTEGER NOT NULL, unit_price DECIMAL(10,2) NOT NULL, original_unit_price DECIMAL(10,2), " &
        "line_discount DECIMAL(10,2) DEFAULT 0, sub_total DECIMAL(10,2), " &
        "created_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS sales_returns (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "sale_id INTEGER REFERENCES sales(id), return_number TEXT, return_date TIMESTAMPTZ DEFAULT NOW(), " &
        "return_type TEXT, reason TEXT, refund_amount DECIMAL(10,2) DEFAULT 0, approved_by TEXT, " &
        "approved_by_user_id INTEGER REFERENCES users(id), processed_by INTEGER REFERENCES users(id), initiated_by INTEGER REFERENCES users(id), " &
        "additional_payment_amount DECIMAL(18,2) NOT NULL DEFAULT 0, settlement_method TEXT, settlement_reference TEXT, " &
        "notes TEXT, status TEXT DEFAULT 'Completed', " &
        "created_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS sales_return_items (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "return_id INTEGER REFERENCES sales_returns(id), original_sale_item_id INTEGER REFERENCES sale_items(id), product_id INTEGER REFERENCES products(id), " &
        "quantity INTEGER NOT NULL, unit_price DECIMAL(10,2) DEFAULT 0, condition TEXT, disposition TEXT, " &
        "line_refund_amount DECIMAL(10,2) DEFAULT 0, return_credit_amount DECIMAL(18,2) NOT NULL DEFAULT 0, " &
        "replacement_product_id INTEGER REFERENCES products(id), replacement_quantity INTEGER, " &
        "replacement_unit_price DECIMAL(10,2), created_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS return_quarantine (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "return_item_id INTEGER UNIQUE NOT NULL REFERENCES sales_return_items(id), product_id INTEGER NOT NULL REFERENCES products(id), " &
        "quantity INTEGER NOT NULL CHECK (quantity > 0), item_condition TEXT NOT NULL, " &
        "status TEXT NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending','Restocked','WrittenOff','ReturnedToVendor')), " &
        "received_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), resolved_at TIMESTAMPTZ, resolved_by INTEGER REFERENCES users(id), " &
        "notes TEXT, synced_at TIMESTAMPTZ)",
        "ALTER TABLE sales_returns ADD COLUMN IF NOT EXISTS approved_by_user_id INTEGER REFERENCES users(id)",
        "ALTER TABLE sales_returns ADD COLUMN IF NOT EXISTS initiated_by INTEGER REFERENCES users(id)",
        "ALTER TABLE sales_returns ADD COLUMN IF NOT EXISTS additional_payment_amount DECIMAL(18,2) NOT NULL DEFAULT 0",
        "ALTER TABLE sales_returns ADD COLUMN IF NOT EXISTS settlement_method TEXT",
        "ALTER TABLE sales_returns ADD COLUMN IF NOT EXISTS settlement_reference TEXT",
        "ALTER TABLE sales_return_items ADD COLUMN IF NOT EXISTS return_credit_amount DECIMAL(18,2) NOT NULL DEFAULT 0",
        "CREATE INDEX IF NOT EXISTS idx_sales_returns_approved_by_user ON sales_returns(approved_by_user_id)",
        "CREATE INDEX IF NOT EXISTS idx_sales_returns_initiated_by ON sales_returns(initiated_by)",
        "CREATE INDEX IF NOT EXISTS idx_return_quarantine_status ON return_quarantine(status, product_id)",
        "CREATE INDEX IF NOT EXISTS idx_return_quarantine_resolved_by ON return_quarantine(resolved_by)",
        "CREATE TABLE IF NOT EXISTS inventory_logs (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "product_id INTEGER REFERENCES products(id), transaction_type TEXT NOT NULL " &
        "CHECK (transaction_type IN ('IN', 'OUT', 'ADJUST')), quantity INTEGER NOT NULL, " &
        "previous_stock INTEGER, new_stock INTEGER, batch_number TEXT, expiry_date TEXT, " &
        "supplier_id INTEGER REFERENCES suppliers(id), user_id INTEGER REFERENCES users(id), " &
        "reference TEXT, notes TEXT, created_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS audit_logs (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "action TEXT NOT NULL, details TEXT, user_id INTEGER REFERENCES users(id), " &
        "action_time TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS sync_log (id SERIAL PRIMARY KEY, started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(), " &
        "completed_at TIMESTAMPTZ, rows_synced INTEGER DEFAULT 0, status TEXT DEFAULT 'running', error TEXT)"
    }

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
            Return WithDefaultTimeout(envDsn)
        End If

        ' 2. Local config file next to the database
        Try
            Dim configPath As String = GetConfigPath()
            If File.Exists(configPath) Then
                Dim json As String = File.ReadAllText(configPath)
                Dim doc As Newtonsoft.Json.Linq.JObject = Newtonsoft.Json.Linq.JObject.Parse(json)
                Dim cs As String = doc("supabaseConnectionString")?.ToString()
                If Not String.IsNullOrWhiteSpace(cs) Then
                    Return WithDefaultTimeout(cs)
                End If
            End If
        Catch ex As Exception
            Console.WriteLine($"Note: Could not read {ConfigFileName}: {ex.Message}")
        End Try

        ' 3. app.config fallback (placeholder, normally empty)
        Dim configCs As String = ConfigurationManager.AppSettings("SupabaseConnectionString")
        If Not String.IsNullOrWhiteSpace(configCs) Then
            Return WithDefaultTimeout(configCs)
        End If

        Throw New InvalidOperationException(
            "Supabase is not configured. Set the JADECLINIC_SUPABASE_DSN environment variable " &
            "or create the file: " & GetConfigPath())
    End Function

    Private Function WithDefaultTimeout(connString As String) As String
        Try
            Dim builder As New NpgsqlConnectionStringBuilder(connString)
            If builder.CommandTimeout < 300 Then
                builder.CommandTimeout = 300
            End If
            If builder.KeepAlive <= 0 Then
                builder.KeepAlive = 30
            End If
            If builder.Timeout < 60 Then
                builder.Timeout = 60
            End If
            Return builder.ConnectionString
        Catch ex As Exception
            Console.WriteLine($"Note: Could not parse connection string, keeping as-is: {ex.Message}")
            Return connString
        End Try
    End Function

    Private Function GetFullErrorMessage(ex As Exception) As String
        Dim parts As New List(Of String)()
        Dim cur As Exception = ex
        While cur IsNot Nothing
            Dim msg As String = cur.Message
            If Not String.IsNullOrWhiteSpace(msg) AndAlso Not parts.Contains(msg) Then
                parts.Add(msg)
            End If
            cur = cur.InnerException
        End While
        Return String.Join(" | ", parts)
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
        Dim ext As String = System.IO.Path.GetExtension(path).ToLowerInvariant()
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
            Using client As New AmazonS3Client(accessKey, secretKey, BuildS3Config(cfg))
                Dim req As New PutObjectRequest()
                req.BucketName = cfg("bucket")
                req.Key = key
                req.FilePath = localFile
                req.ContentType = GetContentType(localFile)
                client.PutObjectAsync(req).GetAwaiter().GetResult()
            End Using

            Return BuildPublicUrl(cfg("endpoint"), cfg("bucket"), key)
        Catch ex As Exception
            Console.WriteLine($"Image upload failed for {key}: {ex.Message}")
            Return Nothing
        End Try
    End Function

    Public Structure S3ObjectInfo
        Public Key As String
        Public LastModified As DateTime
    End Structure

    Private Function BuildS3Config(cfg As Dictionary(Of String, String)) As AmazonS3Config
        Dim s3Config As New AmazonS3Config()
        s3Config.ServiceURL = cfg("endpoint")
        s3Config.ForcePathStyle = True
        s3Config.AuthenticationRegion = cfg("region")
        Return s3Config
    End Function

    ' Generic S3 upload (used for snapshot/backup files, keyed under backups/...).
    Public Function UploadFileToBucket(localFile As String, remoteKey As String,
                                       Optional contentType As String = "application/octet-stream") As Boolean
        Dim cfg As Dictionary(Of String, String) = GetS3Config()
        If cfg Is Nothing Then Return False
        Dim accessKey As String = cfg("accessKeyId")
        Dim secretKey As String = cfg("secretAccessKey")
        If String.IsNullOrWhiteSpace(accessKey) OrElse String.IsNullOrWhiteSpace(secretKey) Then
            Console.WriteLine("Note: S3 keys not configured - skipping upload.")
            Return False
        End If
        Try
            Using client As New AmazonS3Client(accessKey, secretKey, BuildS3Config(cfg))
                Dim req As New PutObjectRequest()
                req.BucketName = cfg("bucket")
                req.Key = remoteKey
                req.FilePath = localFile
                req.ContentType = contentType
                client.PutObjectAsync(req).GetAwaiter().GetResult()
            End Using
            Return True
        Catch ex As Exception
            Console.WriteLine($"S3 upload failed for {remoteKey}: {ex.Message}")
            Return False
        End Try
    End Function

    Public Function ListBucketObjects(prefix As String) As List(Of S3ObjectInfo)
        Dim result As New List(Of S3ObjectInfo)()
        Dim cfg As Dictionary(Of String, String) = GetS3Config()
        If cfg Is Nothing Then Return result
        Dim accessKey As String = cfg("accessKeyId")
        Dim secretKey As String = cfg("secretAccessKey")
        If String.IsNullOrWhiteSpace(accessKey) OrElse String.IsNullOrWhiteSpace(secretKey) Then Return result
        Try
            Using client As New AmazonS3Client(accessKey, secretKey, BuildS3Config(cfg))
                Dim req As New ListObjectsV2Request()
                req.BucketName = cfg("bucket")
                req.Prefix = prefix
                Dim resp As ListObjectsV2Response = client.ListObjectsV2Async(req).GetAwaiter().GetResult()
                For Each obj As Amazon.S3.Model.S3Object In resp.S3Objects
                    result.Add(New S3ObjectInfo With {.Key = obj.Key, .LastModified = obj.LastModified})
                Next
            End Using
        Catch ex As Exception
            Console.WriteLine($"S3 list failed for {prefix}: {ex.Message}")
        End Try
        Return result
    End Function

    Public Function DeleteBucketObject(key As String) As Boolean
        Dim cfg As Dictionary(Of String, String) = GetS3Config()
        If cfg Is Nothing Then Return False
        Dim accessKey As String = cfg("accessKeyId")
        Dim secretKey As String = cfg("secretAccessKey")
        If String.IsNullOrWhiteSpace(accessKey) OrElse String.IsNullOrWhiteSpace(secretKey) Then Return False
        Try
            Using client As New AmazonS3Client(accessKey, secretKey, BuildS3Config(cfg))
                client.DeleteObjectAsync(cfg("bucket"), key).GetAwaiter().GetResult()
            End Using
            Return True
        Catch ex As Exception
            Console.WriteLine($"S3 delete failed for {key}: {ex.Message}")
            Return False
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
    ' Full sync (all 8 data tables, in FK-safe order).
    ' progress receives human-readable status messages on the calling
    ' thread (typically the background worker). When full = False, the
    ' append-only tables (sales, sale_items, inventory_logs, audit_logs)
    ' sync only NEW rows (delta) based on the max local_id already in the
    ' cloud. Products/Users/Suppliers always upsert fully
    ' so stock levels, prices and roles stay current.
    ' ------------------------------------------------------------
    Public Function RunFullSync(Optional progress As Action(Of String) = Nothing,
                                Optional full As Boolean = False,
                                Optional resolveBaseline As Boolean = False,
                                Optional reconcile As Boolean = True,
                                Optional mode As String = "auto") As SyncResult
        Dim result As New SyncResult()
        Dim pg As NpgsqlConnection = Nothing
        Dim logId As Integer = 0
        Dim localLogId As Integer = 0
        Dim totalRows As Integer = 0
        Dim deletedRows As Integer = 0
        Dim snapLocal As Boolean = False
        Dim snapCloud As Boolean = False
        Dim snapBytes As Long = 0
        Try
            Dim cs As String = GetSupabaseConnectionString()
            Dim localConnStr As String = Connection.GetConnectionString()

            ReportProgress(progress, "Connecting to Supabase...")
            pg = New NpgsqlConnection(cs)
            pg.Open()

            ReportProgress(progress, "Checking cloud schema...")
            EnsureCloudSchema(pg)

            ReconcileStaleSyncRuns(pg)

            ' Smart runs (manual "Sync Cloud" button without "Force full
            ' re-upload") upload everything only until the cloud has a
            ' baseline; once any sales rows exist they run as a delta sync
            ' so repeat clicks do not re-upload the whole history.
            If Not full AndAlso resolveBaseline Then
                Using countCmd As New NpgsqlCommand("SELECT COUNT(*) FROM sales", pg)
                    full = Convert.ToInt32(countCmd.ExecuteScalar()) = 0
                End Using
            End If

            ' Record the start of this sync run
            Using logCmd As New NpgsqlCommand(
                "INSERT INTO sync_log (started_at, status) VALUES (NOW(), 'running') RETURNING id", pg)
                logId = Convert.ToInt32(logCmd.ExecuteScalar())
            End Using

            Using local As New NpgsqlConnection(localConnStr)
                local.Open()

                ' Local audit trail for this run
                localLogId = LocalSyncLog.StartEvent("sync", mode, If(full, "full", mode), reconcile)

                ' Rolling snapshot + monthly permanent backup BEFORE the upload
                ' (best-effort - a snapshot failure never aborts the sync).
                Try
                    Dim snap As DatabaseSnapshot.SnapshotOutcome = DatabaseSnapshot.EnsureRollingSnapshot(False)
                    snapLocal = snap.CreatedLocal
                    snapCloud = snap.UploadedCloud
                    snapBytes = snap.SizeBytes
                    If Not String.IsNullOrEmpty(snap.FileName) Then
                        result.Summary.Add($"snapshot: {snap.FileName}")
                    End If
                    Dim monthly As DatabaseSnapshot.BackupOutcome = DatabaseSnapshot.CreateMonthlyBackupIfDue()
                    If monthly.CreatedLocal Then
                        result.Summary.Add($"monthly backup: {monthly.FileName}")
                    End If
                Catch snapEx As Exception
                    ReportProgress(progress, "Snapshot skipped: " & snapEx.Message)
                End Try

                Dim supplierIdMap As New Dictionary(Of Integer, Integer)()   ' LAN SupplierID -> supabase id
                Dim userIdMap As New Dictionary(Of Integer, Integer)()       ' LAN UserID -> supabase id
                Dim productIdMap As New Dictionary(Of Integer, Integer)()    ' LAN ProductID -> supabase id
                Dim saleIdMap As New Dictionary(Of Integer, Integer)()       ' LAN SaleID -> supabase id
                Dim saleItemIdMap As New Dictionary(Of Integer, Integer)()   ' LAN SaleItemID -> supabase id
                Dim returnIdMap As New Dictionary(Of Integer, Integer)()     ' LAN ReturnID -> supabase id
                Dim returnItemIdMap As New Dictionary(Of Integer, Integer)() ' LAN ReturnItemID -> supabase id

                If Not full Then
                    PopulateCloudIdMap(pg, "users", userIdMap)
                    PopulateCloudIdMap(pg, "products", productIdMap)
                    PopulateCloudIdMap(pg, "sales", saleIdMap)
                    PopulateCloudIdMap(pg, "sale_items", saleItemIdMap)
                    PopulateCloudIdMap(pg, "sales_returns", returnIdMap)
                    PopulateCloudIdMap(pg, "sales_return_items", returnItemIdMap)
                End If

                ReportProgress(progress, "Sync started (delta: " & (Not full).ToString() & ")")

                Dim nSuppliers As Integer = SyncStep(progress, "suppliers", Function() SyncSuppliers(local, pg, supplierIdMap, progress))
                Dim nUsers As Integer = SyncStep(progress, "users", Function() SyncUsers(local, pg, userIdMap, progress))
                Dim nProducts As Integer = SyncStep(progress, "products", Function() SyncProducts(local, pg, supplierIdMap, productIdMap, progress))
                Dim nSales As Integer = SyncStep(progress, "sales", Function() SyncSales(local, pg, userIdMap, saleIdMap, progress, full))
                Dim nSaleItems As Integer = SyncStep(progress, "sale_items", Function() SyncSaleItems(local, pg, saleIdMap, productIdMap, saleItemIdMap, progress, full))
                Dim nSalesReturns As Integer = SyncStep(progress, "sales_returns", Function() SyncSalesReturns(local, pg, saleIdMap, userIdMap, returnIdMap, progress, full))
                Dim nSalesReturnItems As Integer = SyncStep(progress, "sales_return_items", Function() SyncSalesReturnItems(local, pg, returnIdMap, saleItemIdMap, productIdMap, returnItemIdMap, progress, full))
                Dim nReturnQuarantine As Integer = SyncStep(progress, "return_quarantine", Function() SyncReturnQuarantine(local, pg, returnItemIdMap, productIdMap, userIdMap, progress, full))
                Dim nInvLogs As Integer = SyncStep(progress, "inventory_logs", Function() SyncInventoryLogs(local, pg, productIdMap, supplierIdMap, userIdMap, progress, full))
                Dim nAuditLogs As Integer = SyncStep(progress, "audit_logs", Function() SyncAuditLogs(local, pg, userIdMap, progress, full))
                Dim nWebAdmins As Integer = SyncStep(progress, "web_admins", Function() SyncAuthUsers(local, pg))
                totalRows = nSuppliers + nUsers + nProducts + nSales + nSaleItems + nSalesReturns + nSalesReturnItems + nReturnQuarantine + nInvLogs + nAuditLogs + nWebAdmins

                result.Summary.Add($"suppliers: {nSuppliers}")
                result.Summary.Add($"users: {nUsers}")
                result.Summary.Add($"products: {nProducts}")
                result.Summary.Add($"sales: {nSales}")
                result.Summary.Add($"sale_items: {nSaleItems}")
                result.Summary.Add($"sales_returns: {nSalesReturns}")
                result.Summary.Add($"sales_return_items: {nSalesReturnItems}")
                result.Summary.Add($"return_quarantine: {nReturnQuarantine}")
                result.Summary.Add($"inventory_logs: {nInvLogs}")
                result.Summary.Add($"audit_logs: {nAuditLogs}")
                result.Summary.Add($"web_admins: {nWebAdmins}")

                ' 1:1 mirror: delete any cloud rows that no longer exist locally
                ' (delete propagation). Silent - single syncing machine, exact copy.
                If reconcile Then
                    ReportProgress(progress, "Reconciling cloud to match local (deleting removed rows)...")
                    deletedRows = ReconcileCloudToLocal(local, pg, progress)
                    result.Summary.Add($"reconcile_deleted: {deletedRows}")
                End If

                ReportProgress(progress, "Sync finished (delta: " & (Not full).ToString() & ")")
            End Using

            ' Close the local event as successful
            If localLogId > 0 Then
                LocalSyncLog.CompleteEvent(localLogId, "success", totalRows, "", snapLocal, snapCloud, snapBytes, deletedRows)
            End If

            ' Mark the run as successful
            Using logCmd As New NpgsqlCommand(
                "UPDATE sync_log SET completed_at = NOW(), rows_synced = @rows, status = 'success' WHERE id = @id", pg)
                AddParam(logCmd, "@rows", totalRows, NpgsqlDbType.Integer)
                AddParam(logCmd, "@id", logId, NpgsqlDbType.Integer)
                logCmd.ExecuteNonQuery()
            End Using

            result.Summary.Add($"sync_log id: {logId}")
            result.Success = True
        Catch ex As Exception
            result.ErrorMessage = GetFullErrorMessage(ex)
            Console.WriteLine($"Sync error: {ex}")

            ' Close the local event as failed (best-effort, never masks the original error)
            If localLogId > 0 Then
                LocalSyncLog.CompleteEvent(localLogId, "failed", 0, GetFullErrorMessage(ex), snapLocal, snapCloud, snapBytes, deletedRows)
            End If

            ' Record the failure (best-effort, never masks the original error)
            If pg IsNot Nothing AndAlso pg.State = System.Data.ConnectionState.Open AndAlso logId > 0 Then
                Try
                    Using logCmd As New NpgsqlCommand(
                        "UPDATE sync_log SET completed_at = NOW(), status = 'failed', error = @err WHERE id = @id", pg)
                        AddParam(logCmd, "@err", GetFullErrorMessage(ex), NpgsqlDbType.Text)
                        AddParam(logCmd, "@id", logId, NpgsqlDbType.Integer)
                        logCmd.ExecuteNonQuery()
                    End Using
                Catch logEx As Exception
                    Console.WriteLine($"Could not update sync_log: {logEx.Message}")
                End Try
            End If
        Finally
            If pg IsNot Nothing Then
                pg.Dispose()
            End If
        End Try
        Return result
    End Function

    Public Function GetRecentSyncLogs(limit As Integer) As List(Of String)
        Dim lines As New List(Of String)()
        Try
            Dim cs As String = GetSupabaseConnectionString()
            Using pg As New NpgsqlConnection(cs)
                pg.Open()
                ReconcileStaleSyncRuns(pg)
                Using cmd As New NpgsqlCommand(
                    "SELECT id, started_at, completed_at, rows_synced, status, error " &
                    "FROM sync_log ORDER BY id DESC LIMIT @limit", pg)
                    AddParam(cmd, "@limit", limit, NpgsqlDbType.Integer)
                    Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                        While reader.Read()
                            Dim status As String = Convert.ToString(reader("status"))
                            Dim startedAtText As String = Convert.ToString(reader("started_at"))
                            Dim rows As Object = reader("rows_synced")
                            Dim rowText As String = If(IsDBNull(rows), "0", rows.ToString())
                            Dim statusIcon As String = If(status = "success", "[OK]", If(status = "failed", "[FAIL]", "[...]"))
                            Dim line As String = $"{statusIcon} {startedAtText}  {rowText} rows  {status}"
                            If status = "failed" AndAlso Not IsDBNull(reader("error")) Then
                                line &= "  - " & Convert.ToString(reader("error"))
                            End If
                            lines.Add(line)
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            lines.Add($"Could not load sync history: {ex.Message}")
        End Try
        Return lines
    End Function

    ' ------------------------------------------------------------
    ' Table syncs
    ' ------------------------------------------------------------
    Private Function SyncSuppliers(local As NpgsqlConnection, pg As NpgsqlConnection,
                                   idMap As Dictionary(Of Integer, Integer),
                                   progress As Action(Of String)) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Using cmd As New NpgsqlCommand(
            "SELECT SupplierID, SupplierCode, SupplierName, ContactPerson, Phone, Email, IsActive FROM Suppliers", local)
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(Convert.ToInt32(reader("SupplierID")), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "SupplierCode"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "SupplierName"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "ContactPerson"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Phone"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Email"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetBoolDb(reader, "IsActive"), NpgsqlDbType.Boolean))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "CreatedAt"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "UpdatedAt"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, SuppliersInsert, SuppliersSuffix,
                                    Sub(lanId, cloudId) idMap(lanId) = cloudId)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"suppliers synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, SuppliersInsert, SuppliersSuffix,
                        Sub(lanId, cloudId) idMap(lanId) = cloudId)
            count += chunk.Count
            ReportProgress(progress, $"suppliers synced: {count}")
        End If
        Return count
    End Function

    Private Function SyncUsers(local As NpgsqlConnection, pg As NpgsqlConnection,
                               idMap As Dictionary(Of Integer, Integer),
                               progress As Action(Of String)) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        ' IMPORTANT: PasswordHash, pin, QRCode, Passkeys are intentionally NOT synced.
        Using cmd As New NpgsqlCommand(
            "SELECT UserID, Username, FullName, UserRole, IsActive, Email, Phone, PhotoPath, EmployeeCode, CreatedAt, UpdatedAt FROM Users", local)
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
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

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "Username"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "FullName"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "UserRole"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetBoolDb(reader, "IsActive"), NpgsqlDbType.Boolean))
                    vals.Add(New SyncVal(GetStr(reader, "Email"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Phone"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(photoUrl, NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "EmployeeCode"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "CreatedAt"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "UpdatedAt"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, UsersInsert, UsersSuffix,
                                    Sub(lanId, cloudId) idMap(lanId) = cloudId)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"users synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, UsersInsert, UsersSuffix,
                        Sub(lanId, cloudId) idMap(lanId) = cloudId)
            count += chunk.Count
            ReportProgress(progress, $"users synced: {count}")
        End If
        Return count
    End Function

    ' ------------------------------------------------------------
    ' Web auth seeding (Supabase Auth / GoTrue auth.users)
    ' Only active Admin accounts with a BCrypt hash + email become web logins.
    ' PasswordHash is stored ONLY in auth.users.encrypted_password — never in
    ' the synced public.users table. OAuth links to the same account by email.
    ' ------------------------------------------------------------
    Private Function SyncAuthUsers(local As NpgsqlConnection, pg As NpgsqlConnection) As Integer
        Dim count As Integer = 0
        Dim activeLocalIds As New HashSet(Of Integer)()

        Using cmd As New NpgsqlCommand(
            "SELECT UserID, Username, FullName, UserRole, IsActive, Email, PasswordHash FROM Users", local)
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim role As String = If(IsDBNull(reader("UserRole")), "Staff", reader("UserRole").ToString())
                    If Not role.Equals("Admin", StringComparison.OrdinalIgnoreCase) Then Continue While

                    Dim active As Boolean = Convert.ToInt32(reader("IsActive")) <> 0
                    If Not active Then Continue While

                    Dim emailRaw = GetStr(reader, "Email")
                    If IsDBNull(emailRaw) Then Continue While
                    Dim email As String = emailRaw.ToString().Trim().ToLowerInvariant()
                    If String.IsNullOrWhiteSpace(email) Then Continue While

                    Dim pwhashRaw = GetStr(reader, "PasswordHash")
                    If IsDBNull(pwhashRaw) Then Continue While
                    Dim pwhash As String = pwhashRaw.ToString()
                    If Not IsBcryptHash(pwhash) Then Continue While

                    Dim fullName As Object = GetStr(reader, "FullName")
                    Dim localId As Integer = Convert.ToInt32(reader("UserID"))
                    activeLocalIds.Add(localId)

                    Dim appMeta As String = "{""provider"": ""email"", ""providers"": [""email""]}"
                    Dim userMeta As String = "{""full_name"": " & JsonStr(fullName) & ", ""role"": ""Admin"", ""local_id"": " & localId & "}"

                    ' Match by local_id first (stable across email changes), else by email
                    Dim uid As Object = FindAuthUserIdByLocalId(pg, localId)
                    If uid Is Nothing OrElse IsDBNull(uid) Then
                        uid = FindAuthUserIdByEmail(pg, email)
                    End If

                    If uid Is Nothing OrElse IsDBNull(uid) Then
                        uid = InsertAuthUser(pg, email, pwhash, appMeta, userMeta)
                    Else
                        UpdateAuthUser(pg, uid, email, pwhash, userMeta)
                    End If

                    If uid Is Nothing OrElse IsDBNull(uid) Then Continue While

                    SyncAuthIdentity(pg, uid, email)
                    count += 1
                End While
            End Using
        End Using

        ' Revoke web access for accounts whose POS user is no longer an active admin
        RevokeInactiveAuthUsers(pg, activeLocalIds)

        Return count
    End Function

    Private Function FindAuthUserIdByLocalId(pg As NpgsqlConnection, localId As Integer) As Object
        Using cmd As New NpgsqlCommand(
            "SELECT id FROM auth.users WHERE raw_user_meta_data ->> 'local_id' = @lid LIMIT 1", pg)
            AddParam(cmd, "@lid", localId.ToString(), NpgsqlDbType.Text)
            Dim res As Object = cmd.ExecuteScalar()
            If res Is Nothing Then Return DBNull.Value
            Return res
        End Using
    End Function

    Private Function FindAuthUserIdByEmail(pg As NpgsqlConnection, email As String) As Object
        Using cmd As New NpgsqlCommand(
            "SELECT id FROM auth.users WHERE email = @em AND is_sso_user = false LIMIT 1", pg)
            AddParam(cmd, "@em", email, NpgsqlDbType.Text)
            Dim res As Object = cmd.ExecuteScalar()
            If res Is Nothing Then Return DBNull.Value
            Return res
        End Using
    End Function

    Private Function InsertAuthUser(pg As NpgsqlConnection, email As String, pwhash As String,
                                    appMeta As String, userMeta As String) As Object
        Using pgCmd As New NpgsqlCommand(
            "INSERT INTO auth.users " &
            "(instance_id, id, aud, role, email, encrypted_password, email_confirmed_at, " &
            " raw_app_meta_data, raw_user_meta_data, created_at, updated_at, " &
            " confirmation_token, recovery_token, email_change_token_new, email_change, " &
            " phone_change, phone_change_token, email_change_token_current, email_change_confirm_status, " &
            " reauthentication_token, is_sso_user, is_anonymous) " &
            "VALUES ('00000000-0000-0000-0000-000000000000', gen_random_uuid(), 'authenticated', 'authenticated', @email, @hash, NOW(), " &
            " @appmeta::jsonb, @usermeta::jsonb, NOW(), NOW(), " &
            " '', '', '', '', '', '', '', 0, '', FALSE, FALSE) " &
            "ON CONFLICT (email) WHERE is_sso_user = false " &
            "DO UPDATE SET encrypted_password = EXCLUDED.encrypted_password, " &
            " raw_user_meta_data = EXCLUDED.raw_user_meta_data, banned_until = NULL, deleted_at = NULL, updated_at = NOW() " &
            "RETURNING id", pg)

            AddParam(pgCmd, "@email", email, NpgsqlDbType.Text)
            AddParam(pgCmd, "@hash", pwhash, NpgsqlDbType.Text)
            AddParam(pgCmd, "@appmeta", appMeta, NpgsqlDbType.Jsonb)
            AddParam(pgCmd, "@usermeta", userMeta, NpgsqlDbType.Jsonb)

            Dim res As Object = pgCmd.ExecuteScalar()
            If res Is Nothing Then Return DBNull.Value
            Return res
        End Using
    End Function

    Private Sub UpdateAuthUser(pg As NpgsqlConnection, uid As Object, email As String, pwhash As String,
                               userMeta As String)
        Using pgCmd As New NpgsqlCommand(
            "UPDATE auth.users SET email = @email, encrypted_password = @hash, " &
            " email_confirmed_at = COALESCE(email_confirmed_at, NOW()), " &
            " raw_user_meta_data = @usermeta, banned_until = NULL, deleted_at = NULL, updated_at = NOW() " &
            "WHERE id = @uid", pg)

            AddParam(pgCmd, "@email", email, NpgsqlDbType.Text)
            AddParam(pgCmd, "@hash", pwhash, NpgsqlDbType.Text)
            AddParam(pgCmd, "@usermeta", userMeta, NpgsqlDbType.Jsonb)
            AddParam(pgCmd, "@uid", uid, NpgsqlDbType.Uuid)
            pgCmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub SyncAuthIdentity(pg As NpgsqlConnection, uid As Object, email As String)
        Dim uidText As String = uid.ToString()

        ' Remove legacy email-keyed identities (old provider_id = email convention)
        Using delCmd As New NpgsqlCommand(
            "DELETE FROM auth.identities WHERE user_id = @uid AND provider = 'email' AND provider_id <> @pid", pg)
            AddParam(delCmd, "@uid", uid, NpgsqlDbType.Uuid)
            AddParam(delCmd, "@pid", uidText, NpgsqlDbType.Text)
            delCmd.ExecuteNonQuery()
        End Using

        Dim identityData As String = "{""sub"": " & JsonStr(uidText) & ", ""email"": " & JsonStr(email) & ", ""email_verified"": true, ""phone_verified"": false}"
        Using idCmd As New NpgsqlCommand(
            "INSERT INTO auth.identities (provider_id, user_id, identity_data, provider, last_sign_in_at, created_at, updated_at) " &
            "VALUES (@pid, @uid, @idata::jsonb, 'email', NOW(), NOW(), NOW()) " &
            "ON CONFLICT (provider_id, provider) DO UPDATE SET " &
            "identity_data = EXCLUDED.identity_data, updated_at = NOW()", pg)

            AddParam(idCmd, "@pid", uidText, NpgsqlDbType.Text)
            AddParam(idCmd, "@uid", uid, NpgsqlDbType.Uuid)
            AddParam(idCmd, "@idata", identityData, NpgsqlDbType.Jsonb)
            idCmd.ExecuteNonQuery()
        End Using
    End Sub

    Private Sub RevokeInactiveAuthUsers(pg As NpgsqlConnection, activeLocalIds As HashSet(Of Integer))
        Dim toBan As New List(Of Object)()
        Using cmd As New NpgsqlCommand(
            "SELECT id, raw_user_meta_data ->> 'local_id' AS lid FROM auth.users " &
            "WHERE raw_user_meta_data ->> 'local_id' IS NOT NULL", pg)
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim lidRaw = reader("lid")
                    If IsDBNull(lidRaw) Then Continue While
                    Dim lid As Integer
                    If Not Integer.TryParse(lidRaw.ToString(), lid) Then Continue While
                    If Not activeLocalIds.Contains(lid) Then
                        toBan.Add(reader("id"))
                    End If
                End While
            End Using
        End Using

        For Each id As Object In toBan
            Using up As New NpgsqlCommand(
                "UPDATE auth.users SET banned_until = NOW() + interval '100 years', updated_at = NOW() WHERE id = @uid", pg)
                AddParam(up, "@uid", id, NpgsqlDbType.Uuid)
                up.ExecuteNonQuery()
            End Using
        Next
    End Sub

    Private Function IsBcryptHash(value As String) As Boolean
        If String.IsNullOrWhiteSpace(value) Then Return False
        Return value.StartsWith("$2a$", StringComparison.Ordinal) OrElse
               value.StartsWith("$2b$", StringComparison.Ordinal)
    End Function

    Private Function JsonStr(value As Object) As String
        If value Is Nothing OrElse IsDBNull(value) Then
            Return "null"
        End If
        Dim s As String = value.ToString().Replace("\", "\\").Replace("""", "\""")
        Return """" & s & """"
    End Function

    Private Function SyncProducts(local As NpgsqlConnection, pg As NpgsqlConnection,
                                  supplierIdMap As Dictionary(Of Integer, Integer),
                                  idMap As Dictionary(Of Integer, Integer),
                                  progress As Action(Of String)) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Using cmd As New NpgsqlCommand(
            "SELECT ProductID, ProductCode, ProductName, Category, Unit, CurrentStock, ReorderLevel, CostPrice, " &
            "SellingPrice, WholesalePrice, SupplierID, IsActive, Created, UpdatedAt FROM Products", local)
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
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

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "ProductCode"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "ProductName"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Category"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Unit"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetIntDb(reader, "CurrentStock"), 0), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(Dcoalesce(GetIntDb(reader, "ReorderLevel"), 0), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(hasExpiry, NpgsqlDbType.Boolean))
                    vals.Add(New SyncVal(expiryDate, NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetDecimalDb(reader, "CostPrice"), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(GetDecimalDb(reader, "SellingPrice"), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(GetDecimalDb(reader, "WholesalePrice"), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(supplierId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(imageUrl, NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetBoolDb(reader, "IsActive"), NpgsqlDbType.Boolean))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "Created"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "UpdatedAt"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, ProductsInsert, ProductsSuffix,
                                    Sub(lanId, cloudId) idMap(lanId) = cloudId)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"products synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, ProductsInsert, ProductsSuffix,
                        Sub(lanId, cloudId) idMap(lanId) = cloudId)
            count += chunk.Count
            ReportProgress(progress, $"products synced: {count}")
        End If
        Return count
    End Function

    Private Function SyncSales(local As NpgsqlConnection, pg As NpgsqlConnection,
                               userIdMap As Dictionary(Of Integer, Integer),
                               idMap As Dictionary(Of Integer, Integer),
                               progress As Action(Of String),
                               full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim lastSyncedId As Long = 0
        Dim selectSql As String =
            "SELECT SaleID, SaleNumber, SaleDate, CustomerName, CustomerTIN, UserID, TotalAmount, AmountPaid, PaymentMethod, " &
            "Status, ApprovedBy, AbortReason, DiscountType, DiscountAmount, Reference, SalesData FROM Sales"
        If Not full Then
            lastSyncedId = GetCloudMaxLocalId(pg, "sales")
            selectSql &= " WHERE SaleID > @lastSyncedId"
        End If
        Using cmd As New NpgsqlCommand(selectSql, local)
            If Not full Then
                cmd.Parameters.AddWithValue("@lastSyncedId", lastSyncedId)
            End If
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
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
                    Dim saleDate As Object = Dcoalesce(GetDateDb(reader, "SaleDate"), Date.UtcNow)

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "SaleNumber"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(saleDate, NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(GetStr(reader, "CustomerName"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "CustomerTIN"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(userId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(Dcoalesce(GetDecimalDb(reader, "TotalAmount"), 0D), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(Dcoalesce(GetDecimalDb(reader, "AmountPaid"), 0D), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(Dcoalesce(GetStr(reader, "PaymentMethod"), "Cash"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Reference"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetStr(reader, "Status"), "Completed"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "ApprovedBy"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "AbortReason"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "DiscountType"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetDecimalDb(reader, "DiscountAmount"), 0D), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(salesData, NpgsqlDbType.Jsonb))
                    vals.Add(New SyncVal(saleDate, NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, SalesInsert, SalesSuffix,
                                    Sub(lanId, cloudId) idMap(lanId) = cloudId)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"sales synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, SalesInsert, SalesSuffix,
                        Sub(lanId, cloudId) idMap(lanId) = cloudId)
            count += chunk.Count
            ReportProgress(progress, $"sales synced: {count}")
        End If
        Return count
    End Function

    Private Function SyncSaleItems(local As NpgsqlConnection, pg As NpgsqlConnection,
                                   saleIdMap As Dictionary(Of Integer, Integer),
                                   productIdMap As Dictionary(Of Integer, Integer),
                                   saleItemIdMap As Dictionary(Of Integer, Integer),
                                   progress As Action(Of String),
                                   full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim lastSyncedId As Long = 0
        Dim selectSql As String =
            "SELECT SaleItemID, SaleID, ProductID, Quantity, UnitPrice, OriginalUnitPrice, LineDiscountAmount, SubTotal FROM SaleItems"
        If Not full Then
            lastSyncedId = GetCloudMaxLocalId(pg, "sale_items")
            selectSql &= " WHERE SaleItemID > @lastSyncedId"
        End If
        Using cmd As New NpgsqlCommand(selectSql, local)
            If Not full Then
                cmd.Parameters.AddWithValue("@lastSyncedId", lastSyncedId)
            End If
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("SaleItemID"))

                    ' sale_id is required; silently skipping would leave the cloud incomplete.
                    Dim lanSaleId As Integer = Convert.ToInt32(reader("SaleID"))
                    If Not saleIdMap.ContainsKey(lanSaleId) Then
                        Throw New InvalidOperationException(
                            $"Cannot sync sale_items local_id {localId}: required sales parent local_id {lanSaleId} has no cloud ID mapping.")
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

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(saleId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(productId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetIntDb(reader, "Quantity"), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetDecimalDb(reader, "UnitPrice"), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(GetDecimalDb(reader, "OriginalUnitPrice"), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(Dcoalesce(GetDecimalDb(reader, "LineDiscountAmount"), 0D), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(GetDecimalDb(reader, "SubTotal"), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, SaleItemsInsert, SaleItemsSuffix,
                                    Sub(lanId, cloudId) saleItemIdMap(lanId) = cloudId)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"sale_items synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, SaleItemsInsert, SaleItemsSuffix,
                        Sub(lanId, cloudId) saleItemIdMap(lanId) = cloudId)
            count += chunk.Count
            ReportProgress(progress, $"sale_items synced: {count}")
        End If
        Return count
    End Function

    Private Function SyncSalesReturns(local As NpgsqlConnection, pg As NpgsqlConnection,
                                      saleIdMap As Dictionary(Of Integer, Integer),
                                      userIdMap As Dictionary(Of Integer, Integer),
                                      returnIdMap As Dictionary(Of Integer, Integer),
                                      progress As Action(Of String),
                                      full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim selectSql As String =
            "SELECT ReturnID, SaleID, ReturnNumber, ReturnDate, ReturnType, Reason, RefundAmount, AdditionalPaymentAmount, " &
            "SettlementMethod, SettlementReference, ApprovedBy, ApprovedByUserID, ProcessedBy, InitiatedBy, Notes, Status FROM SalesReturns"
        Using cmd As New NpgsqlCommand(selectSql, local)
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("ReturnID"))

                    ' sale_id is required; silently skipping would lose the return.
                    Dim lanSaleId As Integer = Convert.ToInt32(reader("SaleID"))
                    If Not saleIdMap.ContainsKey(lanSaleId) Then
                        Throw New InvalidOperationException(
                            $"Cannot sync sales_returns local_id {localId}: required sales parent local_id {lanSaleId} has no cloud ID mapping.")
                    End If
                    Dim saleId As Object = saleIdMap(lanSaleId)

                    Dim approvedByUserId As Object = ResolveForeignKey(
                        reader, "ApprovedByUserID", userIdMap, "sales_returns", localId, "users", False)
                    Dim processedBy As Object = ResolveForeignKey(
                        reader, "ProcessedBy", userIdMap, "sales_returns", localId, "users", False)
                    Dim initiatedBy As Object = ResolveForeignKey(
                        reader, "InitiatedBy", userIdMap, "sales_returns", localId, "users", False)

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(saleId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "ReturnNumber"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "ReturnDate"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Dcoalesce(GetStr(reader, "ReturnType"), "Exchange"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Reason"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetDecimalDb(reader, "RefundAmount"), 0D), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(Dcoalesce(GetDecimalDb(reader, "AdditionalPaymentAmount"), 0D), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(GetStr(reader, "SettlementMethod"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "SettlementReference"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "ApprovedBy"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(approvedByUserId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(processedBy, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(initiatedBy, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "Notes"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetStr(reader, "Status"), "Completed"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, SalesReturnsInsert, SalesReturnsSuffix,
                                    Sub(lanId, cloudId) returnIdMap(lanId) = cloudId)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"sales_returns synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, SalesReturnsInsert, SalesReturnsSuffix,
                        Sub(lanId, cloudId) returnIdMap(lanId) = cloudId)
            count += chunk.Count
            ReportProgress(progress, $"sales_returns synced: {count}")
        End If
        Return count
    End Function

    Private Function SyncSalesReturnItems(local As NpgsqlConnection, pg As NpgsqlConnection,
                                          returnIdMap As Dictionary(Of Integer, Integer),
                                          saleItemIdMap As Dictionary(Of Integer, Integer),
                                          productIdMap As Dictionary(Of Integer, Integer),
                                          returnItemIdMap As Dictionary(Of Integer, Integer),
                                          progress As Action(Of String),
                                          full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim selectSql As String =
            "SELECT ReturnItemID, ReturnID, OriginalSaleItemID, ProductID, Quantity, UnitPrice, Condition, Disposition, " &
            "LineRefundAmount, ReturnCreditAmount, ReplacementProductID, ReplacementQuantity, ReplacementUnitPrice FROM SalesReturnItems"
        Using cmd As New NpgsqlCommand(selectSql, local)
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("ReturnItemID"))

                    ' return_id is required; silently skipping would lose the return item.
                    Dim lanReturnId As Integer = Convert.ToInt32(reader("ReturnID"))
                    If Not returnIdMap.ContainsKey(lanReturnId) Then
                        Throw New InvalidOperationException(
                            $"Cannot sync sales_return_items local_id {localId}: required sales_returns parent local_id {lanReturnId} has no cloud ID mapping.")
                    End If
                    Dim returnId As Object = returnIdMap(lanReturnId)

                    Dim originalSaleItemId As Object = DBNull.Value
                    Dim rawOriginalSaleItem = GetValue(reader, "OriginalSaleItemID")
                    If Not IsDBNull(rawOriginalSaleItem) AndAlso rawOriginalSaleItem IsNot Nothing Then
                        Dim lanOriginalSaleItemId As Integer = Convert.ToInt32(rawOriginalSaleItem)
                        If Not saleItemIdMap.ContainsKey(lanOriginalSaleItemId) Then
                            Throw New InvalidOperationException(
                                $"Cannot sync sales_return_items local_id {localId}: required sale_items parent local_id {lanOriginalSaleItemId} has no cloud ID mapping.")
                        End If
                        originalSaleItemId = saleItemIdMap(lanOriginalSaleItemId)
                    End If

                    Dim productId As Object = ResolveForeignKey(
                        reader, "ProductID", productIdMap, "sales_return_items", localId, "products", True)
                    Dim replacementProductId As Object = ResolveForeignKey(
                        reader, "ReplacementProductID", productIdMap, "sales_return_items", localId, "products", False)

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(returnId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(originalSaleItemId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(productId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetIntDb(reader, "Quantity"), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(Dcoalesce(GetDecimalDb(reader, "UnitPrice"), 0D), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(Dcoalesce(GetStr(reader, "Condition"), "Good"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetStr(reader, "Disposition"), "Quarantine"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetDecimalDb(reader, "LineRefundAmount"), 0D), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(Dcoalesce(GetDecimalDb(reader, "ReturnCreditAmount"), 0D), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(replacementProductId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetIntDb(reader, "ReplacementQuantity"), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetDecimalDb(reader, "ReplacementUnitPrice"), NpgsqlDbType.Numeric))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, SalesReturnItemsInsert, SalesReturnItemsSuffix,
                                    Sub(lanId, cloudId) returnItemIdMap(lanId) = cloudId)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"sales_return_items synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, SalesReturnItemsInsert, SalesReturnItemsSuffix,
                        Sub(lanId, cloudId) returnItemIdMap(lanId) = cloudId)
            count += chunk.Count
            ReportProgress(progress, $"sales_return_items synced: {count}")
        End If
        Return count
    End Function

    Private Function SyncReturnQuarantine(local As NpgsqlConnection, pg As NpgsqlConnection,
                                          returnItemIdMap As Dictionary(Of Integer, Integer),
                                          productIdMap As Dictionary(Of Integer, Integer),
                                          userIdMap As Dictionary(Of Integer, Integer),
                                          progress As Action(Of String),
                                          full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim selectSql As String =
            "SELECT QuarantineID, ReturnItemID, ProductID, Quantity, ItemCondition, Status, ReceivedAt, ResolvedAt, ResolvedBy, Notes FROM ReturnQuarantine"
        Using cmd As New NpgsqlCommand(selectSql, local)
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("QuarantineID"))
                    Dim returnItemId As Object = ResolveForeignKey(
                        reader, "ReturnItemID", returnItemIdMap, "return_quarantine", localId, "sales_return_items", True)
                    Dim productId As Object = ResolveForeignKey(
                        reader, "ProductID", productIdMap, "return_quarantine", localId, "products", True)
                    Dim resolvedBy As Object = ResolveForeignKey(
                        reader, "ResolvedBy", userIdMap, "return_quarantine", localId, "users", False)

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(returnItemId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(productId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetIntDb(reader, "Quantity"), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "ItemCondition"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetStr(reader, "Status"), "Pending"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "ReceivedAt"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(GetDateDb(reader, "ResolvedAt"), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(resolvedBy, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "Notes"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, ReturnQuarantineInsert, ReturnQuarantineSuffix, Nothing)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"return_quarantine synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, ReturnQuarantineInsert, ReturnQuarantineSuffix, Nothing)
            count += chunk.Count
            ReportProgress(progress, $"return_quarantine synced: {count}")
        End If
        Return count
    End Function

    Private Function SyncInventoryLogs(local As NpgsqlConnection, pg As NpgsqlConnection,
                                       productIdMap As Dictionary(Of Integer, Integer),
                                       supplierIdMap As Dictionary(Of Integer, Integer),
                                       userIdMap As Dictionary(Of Integer, Integer),
                                       progress As Action(Of String),
                                       full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim lastSyncedId As Long = 0
        Dim selectSql As String =
            "SELECT LogID, ProductID, TransactionType, Quantity, PreviousStock, NewStock, BatchNumber, ExpiryDate, " &
            "SupplierID, UserID, Reference, Notes, CreatedAt FROM InventoryLog"
        If Not full Then
            lastSyncedId = GetCloudMaxLocalId(pg, "inventory_logs")
            selectSql &= " WHERE LogID > @lastSyncedId"
        End If
        Using cmd As New NpgsqlCommand(selectSql, local)
            If Not full Then
                cmd.Parameters.AddWithValue("@lastSyncedId", lastSyncedId)
            End If
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("LogID"))

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(ResolveMapId(reader, "ProductID", productIdMap), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "TransactionType"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetIntDb(reader, "Quantity"), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetIntDb(reader, "PreviousStock"), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetIntDb(reader, "NewStock"), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "BatchNumber"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "ExpiryDate"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(ResolveMapId(reader, "SupplierID", supplierIdMap), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(ResolveMapId(reader, "UserID", userIdMap), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "Reference"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Notes"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "CreatedAt"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, InventoryLogsInsert, InventoryLogsSuffix, Nothing)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"inventory_logs synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, InventoryLogsInsert, InventoryLogsSuffix, Nothing)
            count += chunk.Count
            ReportProgress(progress, $"inventory_logs synced: {count}")
        End If
        Return count
    End Function

    Private Function SyncAuditLogs(local As NpgsqlConnection, pg As NpgsqlConnection,
                                   userIdMap As Dictionary(Of Integer, Integer),
                                   progress As Action(Of String),
                                   full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim lastSyncedId As Long = 0
        Dim selectSql As String = "SELECT AuditID, Action, Details, ActionTime, UserID FROM AuditLog"
        If Not full Then
            lastSyncedId = GetCloudMaxLocalId(pg, "audit_logs")
            selectSql &= " WHERE AuditID > @lastSyncedId"
        End If
        Using cmd As New NpgsqlCommand(selectSql, local)
            If Not full Then
                cmd.Parameters.AddWithValue("@lastSyncedId", lastSyncedId)
            End If
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    Dim localId As Integer = Convert.ToInt32(reader("AuditID"))

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "Action"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Details"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(ResolveMapId(reader, "UserID", userIdMap), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "ActionTime"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, AuditLogsInsert, AuditLogsSuffix, Nothing)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"audit_logs synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, AuditLogsInsert, AuditLogsSuffix, Nothing)
            count += chunk.Count
            ReportProgress(progress, $"audit_logs synced: {count}")
        End If
        Return count
    End Function

    ' ------------------------------------------------------------
    ' Helpers
    ' ------------------------------------------------------------

    ' A single typed column value for a chunked upsert row.
    Private Structure SyncVal
        Public Value As Object
        Public DbType As NpgsqlDbType
        Public Sub New(value As Object, dbType As NpgsqlDbType)
            Me.Value = value
            Me.DbType = dbType
        End Sub
    End Structure

    Private Function NormalizeValue(value As Object, dbType As NpgsqlDbType) As Object
        If value Is Nothing OrElse IsDBNull(value) Then
            Return DBNull.Value
        End If
        If dbType = NpgsqlDbType.TimestampTz AndAlso TypeOf value Is DateTime Then
            Dim dt As DateTime = CType(value, DateTime)
            If dt.Kind = DateTimeKind.Unspecified Then
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            ElseIf dt.Kind = DateTimeKind.Local Then
                dt = dt.ToUniversalTime()
            End If
            Return dt
        End If
        Return value
    End Function

    Private Function Dcoalesce(value As Object, fallback As Object) As Object
        If value Is Nothing OrElse IsDBNull(value) Then
            Return fallback
        End If
        Return value
    End Function

    Private Sub ReportProgress(progress As Action(Of String), message As String)
        If progress IsNot Nothing Then
            Try
                progress.Invoke(message)
            Catch
            End Try
        End If
    End Sub

    ' Mark any leftover 'running' sync_log rows as failed. Happens when the app
    ' is closed (or crashes) mid-sync, which leaves a row that never gets a
    ' final status. Called before each new run and before viewing history.
    Private Sub ReconcileStaleSyncRuns(pg As NpgsqlConnection)
        Try
            Using cmd As New NpgsqlCommand(
                "UPDATE sync_log SET completed_at = NOW(), status = 'failed', " &
                "error = 'Interrupted (app closed during sync)' WHERE status = 'running'", pg)
                cmd.ExecuteNonQuery()
            End Using
        Catch ex As Exception
            Console.WriteLine($"Could not reconcile stale sync_log: {ex.Message}")
        End Try
    End Sub

    ' ------------------------------------------------------------
    ' 1:1 mirror reconcile (delete propagation).
    ' Deletes cloud rows whose local_id no longer exists locally, so the cloud
    ' becomes an exact copy of the LAN database. Safe because only the admin
    ' PC (holding the DB) writes to the cloud. Runs silently on every sync.
    ' Order is child-first to satisfy FK constraints.
    ' ------------------------------------------------------------
    Private Function ReconcileCloudToLocal(local As NpgsqlConnection, pg As NpgsqlConnection,
                                           progress As Action(Of String)) As Integer
        Dim total As Integer = 0
        ReconcileSimpleTable(local, pg, "return_quarantine", "QuarantineID", progress, total, Nothing)
        ReconcileSimpleTable(local, pg, "sales_return_items", "ReturnItemID", progress, total, Nothing)
        ReconcileSimpleTable(local, pg, "sale_items", "SaleItemID", progress, total, Nothing)
        ReconcileSimpleTable(local, pg, "sales_returns", "ReturnID", progress, total, Nothing)
        ReconcileSimpleTable(local, pg, "inventory_logs", "LogID", progress, total, Nothing)
        ReconcileSimpleTable(local, pg, "audit_logs", "AuditID", progress, total, Nothing)
        ReconcileSimpleTable(local, pg, "sales", "SaleID", progress, total, Nothing)
        ReconcileSimpleTable(local, pg, "products", "ProductID", progress, total,
            New String() {"sale_items/product_id", "inventory_logs/product_id", "sales_return_items/product_id", "sales_return_items/replacement_product_id", "return_quarantine/product_id"})
        ReconcileSimpleTable(local, pg, "suppliers", "SupplierID", progress, total,
            New String() {"products/supplier_id"})
        ReconcileSimpleTable(local, pg, "users", "UserID", progress, total,
            New String() {"sales/user_id", "inventory_logs/user_id", "audit_logs/user_id", "sales_returns/approved_by_user_id", "sales_returns/processed_by", "sales_returns/initiated_by", "return_quarantine/resolved_by"})
        Return total
    End Function

    Private Sub ReconcileSimpleTable(local As NpgsqlConnection, pg As NpgsqlConnection,
                                     cloudTable As String, localIdColumn As String,
                                     progress As Action(Of String), ByRef total As Integer,
                                     guardRefs As String())
        Dim cloudIds As New HashSet(Of Integer)()
        Using cmd As New NpgsqlCommand("SELECT local_id FROM " & cloudTable, pg)
            Using rdr As NpgsqlDataReader = cmd.ExecuteReader()
                While rdr.Read()
                    If Not rdr.IsDBNull(0) Then cloudIds.Add(Convert.ToInt32(rdr(0)))
                End While
            End Using
        End Using
        If cloudIds.Count = 0 Then Return

        Dim localIds As New HashSet(Of Integer)()
        Using cmd As New NpgsqlCommand("SELECT " & localIdColumn & " FROM " & GetLocalTableName(cloudTable), local)
            Using rdr As NpgsqlDataReader = cmd.ExecuteReader()
                While rdr.Read()
                    If Not rdr.IsDBNull(0) Then localIds.Add(Convert.ToInt32(rdr(0)))
                End While
            End Using
        End Using

        Dim toDelete As New List(Of Integer)()
        For Each cid As Integer In cloudIds
            If Not localIds.Contains(cid) Then toDelete.Add(cid)
        Next
        If toDelete.Count = 0 Then Return

        ' FK guards: null-out references from surviving local rows so the
        ' parent can be deleted without a foreign key violation.
        If guardRefs IsNot Nothing Then
            For Each ref As String In guardRefs
                Dim parts As String() = ref.Split("/"c)
                NullOutReferences(pg, parts(0), parts(1), cloudTable, toDelete)
            Next
        End If

        Dim actuallyDeleted As Integer = 0
        For i As Integer = 0 To toDelete.Count - 1 Step ChunkSize
            Dim n As Integer = Math.Min(ChunkSize, toDelete.Count - i)
            Dim ids As Integer() = toDelete.GetRange(i, n).ToArray()
            Using cmd As New NpgsqlCommand("DELETE FROM " & cloudTable & " WHERE local_id = ANY(@ids)", pg)
                cmd.Parameters.Add("@ids", NpgsqlDbType.Array Or NpgsqlDbType.Integer).Value = ids
                actuallyDeleted += cmd.ExecuteNonQuery()
            End Using
        Next
        total += actuallyDeleted
        ReportProgress(progress, $"{cloudTable}: reconcile - deleted {actuallyDeleted} row(s) removed locally")
    End Sub

    Private Sub NullOutReferences(pg As NpgsqlConnection, childTable As String, fkColumn As String,
                                  parentTable As String, ids As List(Of Integer))
        For i As Integer = 0 To ids.Count - 1 Step ChunkSize
            Dim n As Integer = Math.Min(ChunkSize, ids.Count - i)
            Dim idArr As Integer() = ids.GetRange(i, n).ToArray()
            Try
                Using cmd As New NpgsqlCommand(
                    "UPDATE " & childTable & " SET " & fkColumn & " = NULL WHERE " & fkColumn &
                    " IN (SELECT id FROM " & parentTable & " WHERE local_id = ANY(@ids))", pg)
                    cmd.Parameters.Add("@ids", NpgsqlDbType.Array Or NpgsqlDbType.Integer).Value = idArr
                    cmd.ExecuteNonQuery()
                End Using
            Catch ex As Exception
                Console.WriteLine($"Note: could not null {childTable}.{fkColumn}: {ex.Message}")
            End Try
        Next
    End Sub

    Private Function GetLocalTableName(cloudTable As String) As String
        Select Case cloudTable
            Case "sale_items" : Return "SaleItems"
            Case "sales_returns" : Return "SalesReturns"
            Case "sales_return_items" : Return "SalesReturnItems"
            Case "return_quarantine" : Return "ReturnQuarantine"
            Case "inventory_logs" : Return "InventoryLog"
            Case "audit_logs" : Return "AuditLog"
            Case "sales" : Return "Sales"
            Case "products" : Return "Products"
            Case "suppliers" : Return "Suppliers"
            Case "users" : Return "Users"
            Case Else : Return "CompanySettings"
        End Select
    End Function

    ' Recreate any missing cloud tables so a wiped/dropped schema self-heals.
    ' Runs before each sync. Also grants the dashboard (authenticated role) read
    ' access to any table it (re)creates. GRANT is best-effort so it never
    ' aborts a sync against a healthy schema owned by another role.
    Private Sub EnsureCloudSchema(pg As NpgsqlConnection)
        For Each stmt As String In EnsureSchemaStatements
            Using cmd As New NpgsqlCommand(stmt, pg)
                cmd.ExecuteNonQuery()
            End Using
        Next

        Dim readOnlyTables As String() = {
            "users", "suppliers", "products", "sales", "sale_items",
            "sales_returns", "sales_return_items", "return_quarantine",
            "inventory_logs", "audit_logs"
        }
        For Each t As String In readOnlyTables
            Try
                Using cmd As New NpgsqlCommand("GRANT SELECT ON " & t & " TO authenticated", pg)
                    cmd.ExecuteNonQuery()
                End Using
            Catch ex As Exception
                Console.WriteLine($"Note: could not grant read on {t}: {ex.Message}")
            End Try
        Next
    End Sub

    ' Run one table sync with a start/end progress message and elapsed time, so
    ' the UI stays visibly alive while the worker is busy.
    Private Function SyncStep(progress As Action(Of String), name As String, work As Func(Of Integer)) As Integer
        Dim sw As New System.Diagnostics.Stopwatch()
        ReportProgress(progress, $"Syncing {name}...")
        sw.Start()
        Dim n As Integer = work()
        sw.Stop()
        ReportProgress(progress, $"{name}: {n} rows ({sw.Elapsed.TotalSeconds.ToString("0.0")}s)")
        Return n
    End Function

    ' Push one chunk of rows as a single multi-row INSERT ... ON CONFLICT upsert.
    ' When onReturning is supplied, the statement must end with "RETURNING id, local_id"
    ' and each (local_id, cloud id) pair is passed back to rebuild FK maps.
    Private Sub UpsertChunk(pg As NpgsqlConnection,
                            chunk As List(Of List(Of SyncVal)),
                            insertPrefix As String,
                            conflictSuffix As String,
                            onReturning As Action(Of Integer, Integer))
        Dim nCols As Integer = chunk(0).Count
        Dim sb As New System.Text.StringBuilder()
        sb.Append(insertPrefix)
        sb.Append(" VALUES ")
        Dim pIndex As Integer = 0
        For i As Integer = 0 To chunk.Count - 1
            If i > 0 Then sb.Append(", ")
            sb.Append("(")
            For j As Integer = 0 To nCols - 1
                If j > 0 Then sb.Append(", ")
                pIndex += 1
                sb.Append("@p").Append(pIndex)
            Next
            sb.Append(")")
        Next
        sb.Append(" ")
        sb.Append(conflictSuffix)

        Using cmd As New NpgsqlCommand(sb.ToString(), pg)
            pIndex = 0
            For Each row As List(Of SyncVal) In chunk
                For Each sv As SyncVal In row
                    pIndex += 1
                    cmd.Parameters.Add("@p" & pIndex, sv.DbType).Value = NormalizeValue(sv.Value, sv.DbType)
                Next
            Next

            If onReturning Is Nothing Then
                cmd.ExecuteNonQuery()
            Else
                Using rdr As NpgsqlDataReader = cmd.ExecuteReader()
                    While rdr.Read()
                        onReturning(Convert.ToInt32(rdr("local_id")), Convert.ToInt32(rdr("id")))
                    End While
                End Using
            End If
        End Using
    End Sub

    ' Read the highest local_id already uploaded to the cloud table. Used by
    ' delta syncs so we only push local rows created after the last sync.
    Private Function GetCloudMaxLocalId(pg As NpgsqlConnection, table As String) As Long
        Try
            Using cmd As New NpgsqlCommand("SELECT COALESCE(MAX(local_id), 0) FROM " & table, pg)
                Return Convert.ToInt64(cmd.ExecuteScalar())
            End Using
        Catch ex As Exception
            Console.WriteLine($"Could not read max local_id for {table}: {ex.Message}")
            Return 0
        End Try
    End Function

    Private Sub PopulateCloudIdMap(pg As NpgsqlConnection, table As String,
                                   idMap As Dictionary(Of Integer, Integer))
        Using cmd As New NpgsqlCommand("SELECT local_id, id FROM " & table, pg)
            Using reader As NpgsqlDataReader = cmd.ExecuteReader()
                While reader.Read()
                    idMap(Convert.ToInt32(reader("local_id"))) = Convert.ToInt32(reader("id"))
                End While
            End Using
        End Using
    End Sub

    Private Function GetFirstProductImagePath(local As NpgsqlConnection, productId As Integer) As String
        Try
            Using cmd As New NpgsqlCommand(
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

    Private Function ResolveMapId(reader As NpgsqlDataReader, column As String,
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

    Private Function ResolveForeignKey(reader As NpgsqlDataReader, column As String,
                                       idMap As Dictionary(Of Integer, Integer), childTable As String,
                                       childLocalId As Integer, parentTable As String, required As Boolean) As Object
        Dim raw As Object = GetValue(reader, column)
        If raw Is Nothing OrElse IsDBNull(raw) Then
            If required Then
                Throw New InvalidOperationException(
                    $"Cannot sync {childTable} local_id {childLocalId}: required {parentTable} parent in {column} is missing.")
            End If
            Return DBNull.Value
        End If

        Dim lanId As Integer = Convert.ToInt32(raw)
        If Not idMap.ContainsKey(lanId) Then
            Throw New InvalidOperationException(
                $"Cannot sync {childTable} local_id {childLocalId}: {column} references {parentTable} local_id {lanId}, which has no cloud ID mapping.")
        End If
        Return CObj(idMap(lanId))
    End Function

    Private Sub AddParam(cmd As NpgsqlCommand, name As String, value As Object, dbType As NpgsqlDbType)
        Dim p As NpgsqlParameter = cmd.Parameters.Add(name, dbType)
        If value Is Nothing OrElse IsDBNull(value) Then
            p.Value = DBNull.Value
            Return
        End If
        If dbType = NpgsqlDbType.TimestampTz AndAlso TypeOf value Is DateTime Then
            Dim dt As DateTime = CType(value, DateTime)
            If dt.Kind = DateTimeKind.Unspecified Then
                dt = DateTime.SpecifyKind(dt, DateTimeKind.Utc)
            ElseIf dt.Kind = DateTimeKind.Local Then
                dt = dt.ToUniversalTime()
            End If
            p.Value = dt
        Else
            p.Value = value
        End If
    End Sub

    Private Function GetValue(reader As NpgsqlDataReader, name As String) As Object
        Try
            If reader.GetOrdinal(name) >= 0 Then
                Return reader(name)
            End If
        Catch
        End Try
        Return DBNull.Value
    End Function

    Private Function GetStr(reader As NpgsqlDataReader, name As String) As Object
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

    Private Function GetIntDb(reader As NpgsqlDataReader, name As String) As Object
        Dim v As Object = GetValue(reader, name)
        If v Is Nothing OrElse IsDBNull(v) Then
            Return DBNull.Value
        End If
        Return CObj(Convert.ToInt32(v))
    End Function

    Private Function GetDecimalDb(reader As NpgsqlDataReader, name As String) As Object
        Dim v As Object = GetValue(reader, name)
        If v Is Nothing OrElse IsDBNull(v) Then
            Return DBNull.Value
        End If
        Return CObj(Convert.ToDecimal(v))
    End Function

    Private Function GetBoolDb(reader As NpgsqlDataReader, name As String) As Object
        Dim v As Object = GetValue(reader, name)
        If v Is Nothing OrElse IsDBNull(v) Then
            Return DBNull.Value
        End If
        Return CObj(Convert.ToInt32(v) <> 0)
    End Function

    Private Function GetDateDb(reader As NpgsqlDataReader, name As String) As Object
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
