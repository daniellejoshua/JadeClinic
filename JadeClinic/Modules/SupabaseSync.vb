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

    Private ReadOnly UsersInsert As String = "INSERT INTO users (local_id, username, full_name, user_role, is_active, email, phone, photo_url, created_at, updated_at, synced_at)"
    Private ReadOnly UsersSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "username = EXCLUDED.username, full_name = EXCLUDED.full_name, user_role = EXCLUDED.user_role, " &
        "is_active = EXCLUDED.is_active, email = EXCLUDED.email, phone = EXCLUDED.phone, " &
        "photo_url = COALESCE(EXCLUDED.photo_url, users.photo_url), " &
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
        "amount_paid, payment_method, reference, is_void, status, discount_type, discount_amount, sales_data, created_at, synced_at)"
    Private ReadOnly SalesSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "sale_number = EXCLUDED.sale_number, sale_date = EXCLUDED.sale_date, customer_name = EXCLUDED.customer_name, " &
        "customer_tin = EXCLUDED.customer_tin, user_id = EXCLUDED.user_id, total_amount = EXCLUDED.total_amount, " &
        "amount_paid = EXCLUDED.amount_paid, payment_method = EXCLUDED.payment_method, reference = EXCLUDED.reference, " &
        "is_void = EXCLUDED.is_void, status = EXCLUDED.status, discount_type = EXCLUDED.discount_type, " &
        "discount_amount = EXCLUDED.discount_amount, sales_data = EXCLUDED.sales_data, synced_at = NOW() " &
        "RETURNING id, local_id"

    Private ReadOnly SaleItemsInsert As String = "INSERT INTO sale_items (local_id, sale_id, product_id, quantity, unit_price, original_unit_price, line_discount, sub_total, created_at, synced_at)"
    Private ReadOnly SaleItemsSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "sale_id = EXCLUDED.sale_id, product_id = EXCLUDED.product_id, quantity = EXCLUDED.quantity, " &
        "unit_price = EXCLUDED.unit_price, original_unit_price = EXCLUDED.original_unit_price, " &
        "line_discount = EXCLUDED.line_discount, sub_total = EXCLUDED.sub_total, synced_at = NOW()"

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

    Private ReadOnly CompanySettingsInsert As String = "INSERT INTO company_settings (local_id, company_name, tin, address, phone, email, website, logo_url, " &
        "bir_auth_number, ptu_number, validity_years, receipt_footer, company_hours, is_active, created_at, updated_at, synced_at)"
    Private ReadOnly CompanySettingsSuffix As String = "ON CONFLICT (local_id) DO UPDATE SET " &
        "company_name = EXCLUDED.company_name, tin = EXCLUDED.tin, address = EXCLUDED.address, phone = EXCLUDED.phone, " &
        "email = EXCLUDED.email, website = EXCLUDED.website, bir_auth_number = EXCLUDED.bir_auth_number, " &
        "ptu_number = EXCLUDED.ptu_number, validity_years = EXCLUDED.validity_years, " &
        "receipt_footer = EXCLUDED.receipt_footer, company_hours = EXCLUDED.company_hours, " &
        "is_active = EXCLUDED.is_active, logo_url = COALESCE(EXCLUDED.logo_url, company_settings.logo_url), " &
        "updated_at = EXCLUDED.updated_at, synced_at = NOW()"

    ' DDL to recreate cloud tables if they were dropped, so the sync self-heals.
    ' Mirrors database/supabase_schema.sql (CREATE TABLE IF NOT EXISTS is idempotent).
    Private ReadOnly EnsureSchemaStatements As String() = {
        "CREATE TABLE IF NOT EXISTS users (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "username TEXT NOT NULL, full_name TEXT NOT NULL, user_role TEXT DEFAULT 'Staff', " &
        "is_active BOOLEAN DEFAULT TRUE, email TEXT, phone TEXT, photo_url TEXT, " &
        "created_at TIMESTAMPTZ DEFAULT NOW(), updated_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
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
        "is_void BOOLEAN DEFAULT FALSE, status TEXT DEFAULT 'Completed', discount_type TEXT, " &
        "discount_amount DECIMAL(10,2) DEFAULT 0, sales_data JSONB, " &
        "created_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS sale_items (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "sale_id INTEGER REFERENCES sales(id) ON DELETE CASCADE, product_id INTEGER REFERENCES products(id), " &
        "quantity INTEGER NOT NULL, unit_price DECIMAL(10,2) NOT NULL, original_unit_price DECIMAL(10,2), " &
        "line_discount DECIMAL(10,2) DEFAULT 0, sub_total DECIMAL(10,2), " &
        "created_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS inventory_logs (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "product_id INTEGER REFERENCES products(id), transaction_type TEXT NOT NULL " &
        "CHECK (transaction_type IN ('IN', 'OUT', 'ADJUST')), quantity INTEGER NOT NULL, " &
        "previous_stock INTEGER, new_stock INTEGER, batch_number TEXT, expiry_date TEXT, " &
        "supplier_id INTEGER REFERENCES suppliers(id), user_id INTEGER REFERENCES users(id), " &
        "reference TEXT, notes TEXT, created_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS audit_logs (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "action TEXT NOT NULL, details TEXT, user_id INTEGER REFERENCES users(id), " &
        "action_time TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
        "CREATE TABLE IF NOT EXISTS company_settings (id SERIAL PRIMARY KEY, local_id INTEGER UNIQUE NOT NULL, " &
        "company_name TEXT NOT NULL, tin TEXT, address TEXT, phone TEXT, email TEXT, website TEXT, logo_url TEXT, " &
        "bir_auth_number TEXT, ptu_number TEXT, validity_years INTEGER DEFAULT 5, receipt_footer TEXT, " &
        "company_hours TEXT, is_active BOOLEAN DEFAULT TRUE, created_at TIMESTAMPTZ DEFAULT NOW(), " &
        "updated_at TIMESTAMPTZ DEFAULT NOW(), synced_at TIMESTAMPTZ)",
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
                client.PutObjectAsync(req).GetAwaiter().GetResult()
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
    ' Full sync (all 8 data tables, in FK-safe order).
    ' progress receives human-readable status messages on the calling
    ' thread (typically the background worker). When full = False, the
    ' append-only tables (sales, sale_items, inventory_logs, audit_logs)
    ' sync only NEW rows (delta) based on the max local_id already in the
    ' cloud. Products/Users/Suppliers/CompanySettings always upsert fully
    ' so stock levels, prices and roles stay current.
    ' ------------------------------------------------------------
    Public Function RunFullSync(Optional progress As Action(Of String) = Nothing,
                                Optional full As Boolean = False) As SyncResult
        Dim result As New SyncResult()
        Dim pg As NpgsqlConnection = Nothing
        Dim logId As Integer = 0
        Dim totalRows As Integer = 0
        Try
            Dim cs As String = GetSupabaseConnectionString()
            Dim localConnStr As String = Connection.GetConnectionString()

            ReportProgress(progress, "Connecting to Supabase...")
            pg = New NpgsqlConnection(cs)
            pg.Open()

            ReportProgress(progress, "Checking cloud schema...")
            EnsureCloudSchema(pg)

            ReconcileStaleSyncRuns(pg)

            ' Record the start of this sync run
            Using logCmd As New NpgsqlCommand(
                "INSERT INTO sync_log (started_at, status) VALUES (NOW(), 'running') RETURNING id", pg)
                logId = Convert.ToInt32(logCmd.ExecuteScalar())
            End Using

            Using local As New SqliteConnection(localConnStr)
                local.Open()

                Dim supplierIdMap As New Dictionary(Of Integer, Integer)()   ' LAN SupplierID -> supabase id
                Dim userIdMap As New Dictionary(Of Integer, Integer)()       ' LAN UserID -> supabase id
                Dim productIdMap As New Dictionary(Of Integer, Integer)()    ' LAN ProductID -> supabase id
                Dim saleIdMap As New Dictionary(Of Integer, Integer)()       ' LAN SaleID -> supabase id

                ReportProgress(progress, "Sync started (delta: " & (Not full).ToString() & ")")

                Dim nSuppliers As Integer = SyncStep(progress, "suppliers", Function() SyncSuppliers(local, pg, supplierIdMap, progress))
                Dim nUsers As Integer = SyncStep(progress, "users", Function() SyncUsers(local, pg, userIdMap, progress))
                Dim nProducts As Integer = SyncStep(progress, "products", Function() SyncProducts(local, pg, supplierIdMap, productIdMap, progress))
                Dim nSales As Integer = SyncStep(progress, "sales", Function() SyncSales(local, pg, userIdMap, saleIdMap, progress, full))
                Dim nSaleItems As Integer = SyncStep(progress, "sale_items", Function() SyncSaleItems(local, pg, saleIdMap, productIdMap, progress, full))
                Dim nInvLogs As Integer = SyncStep(progress, "inventory_logs", Function() SyncInventoryLogs(local, pg, productIdMap, supplierIdMap, userIdMap, progress, full))
                Dim nAuditLogs As Integer = SyncStep(progress, "audit_logs", Function() SyncAuditLogs(local, pg, userIdMap, progress, full))
                Dim nCompanySettings As Integer = SyncStep(progress, "company_settings", Function() SyncCompanySettings(local, pg, progress))
                Dim nWebAdmins As Integer = SyncStep(progress, "web_admins", Function() SyncAuthUsers(local, pg))
                totalRows = nSuppliers + nUsers + nProducts + nSales + nSaleItems + nInvLogs + nAuditLogs + nCompanySettings + nWebAdmins

                result.Summary.Add($"suppliers: {nSuppliers}")
                result.Summary.Add($"users: {nUsers}")
                result.Summary.Add($"products: {nProducts}")
                result.Summary.Add($"sales: {nSales}")
                result.Summary.Add($"sale_items: {nSaleItems}")
                result.Summary.Add($"inventory_logs: {nInvLogs}")
                result.Summary.Add($"audit_logs: {nAuditLogs}")
                result.Summary.Add($"company_settings: {nCompanySettings}")
                result.Summary.Add($"web_admins: {nWebAdmins}")
                ReportProgress(progress, "Sync finished (delta: " & (Not full).ToString() & ")")
            End Using

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
    Private Function SyncSuppliers(local As SqliteConnection, pg As NpgsqlConnection,
                                   idMap As Dictionary(Of Integer, Integer),
                                   progress As Action(Of String)) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Using cmd As New SqliteCommand(
            "SELECT SupplierID, SupplierCode, SupplierName, ContactPerson, Phone, Email, IsActive FROM Suppliers", local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
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

    Private Function SyncUsers(local As SqliteConnection, pg As NpgsqlConnection,
                               idMap As Dictionary(Of Integer, Integer),
                               progress As Action(Of String)) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
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

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "Username"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "FullName"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "UserRole"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetBoolDb(reader, "IsActive"), NpgsqlDbType.Boolean))
                    vals.Add(New SyncVal(GetStr(reader, "Email"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Phone"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(photoUrl, NpgsqlDbType.Text))
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
    Private Function SyncAuthUsers(local As SqliteConnection, pg As NpgsqlConnection) As Integer
        Dim count As Integer = 0
        Dim activeLocalIds As New HashSet(Of Integer)()

        Using cmd As New SqliteCommand(
            "SELECT UserID, Username, FullName, UserRole, IsActive, Email, PasswordHash FROM Users", local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
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

    Private Function SyncProducts(local As SqliteConnection, pg As NpgsqlConnection,
                                  supplierIdMap As Dictionary(Of Integer, Integer),
                                  idMap As Dictionary(Of Integer, Integer),
                                  progress As Action(Of String)) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
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

    Private Function SyncSales(local As SqliteConnection, pg As NpgsqlConnection,
                               userIdMap As Dictionary(Of Integer, Integer),
                               idMap As Dictionary(Of Integer, Integer),
                               progress As Action(Of String),
                               full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim selectSql As String =
            "SELECT SaleID, SaleNumber, SaleDate, CustomerName, CustomerTIN, UserID, TotalAmount, AmountPaid, PaymentMethod, " &
            "IsVoid, Status, DiscountType, DiscountAmount, Reference, SalesData FROM Sales"
        If Not full Then
            selectSql &= " WHERE SaleID > COALESCE((SELECT MAX(local_id) FROM sales), 0)"
        End If
        Using cmd As New SqliteCommand(selectSql, local)
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
                    vals.Add(New SyncVal(Dcoalesce(GetBoolDb(reader, "IsVoid"), False), NpgsqlDbType.Boolean))
                    vals.Add(New SyncVal(Dcoalesce(GetStr(reader, "Status"), "Completed"), NpgsqlDbType.Text))
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

    Private Function SyncSaleItems(local As SqliteConnection, pg As NpgsqlConnection,
                                   saleIdMap As Dictionary(Of Integer, Integer),
                                   productIdMap As Dictionary(Of Integer, Integer),
                                   progress As Action(Of String),
                                   full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim selectSql As String =
            "SELECT SaleItemID, SaleID, ProductID, Quantity, UnitPrice, OriginalUnitPrice, LineDiscountAmount, SubTotal FROM SaleItems"
        If Not full Then
            selectSql &= " WHERE SaleItemID > COALESCE((SELECT MAX(local_id) FROM sale_items), 0)"
        End If
        Using cmd As New SqliteCommand(selectSql, local)
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
                        UpsertChunk(pg, chunk, SaleItemsInsert, SaleItemsSuffix, Nothing)
                        count += chunk.Count
                        chunk.Clear()
                        ReportProgress(progress, $"sale_items synced: {count}")
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, SaleItemsInsert, SaleItemsSuffix, Nothing)
            count += chunk.Count
            ReportProgress(progress, $"sale_items synced: {count}")
        End If
        Return count
    End Function

    Private Function SyncInventoryLogs(local As SqliteConnection, pg As NpgsqlConnection,
                                       productIdMap As Dictionary(Of Integer, Integer),
                                       supplierIdMap As Dictionary(Of Integer, Integer),
                                       userIdMap As Dictionary(Of Integer, Integer),
                                       progress As Action(Of String),
                                       full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim selectSql As String =
            "SELECT LogID, ProductID, TransactionType, Quantity, PreviousStock, NewStock, BatchNumber, ExpiryDate, " &
            "SupplierID, UserID, Reference, Notes, CreatedAt FROM InventoryLog"
        If Not full Then
            selectSql &= " WHERE LogID > COALESCE((SELECT MAX(local_id) FROM inventory_logs), 0)"
        End If
        Using cmd As New SqliteCommand(selectSql, local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
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

    Private Function SyncAuditLogs(local As SqliteConnection, pg As NpgsqlConnection,
                                   userIdMap As Dictionary(Of Integer, Integer),
                                   progress As Action(Of String),
                                   full As Boolean) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
        Dim selectSql As String = "SELECT AuditID, Action, Details, ActionTime, UserID FROM AuditLog"
        If Not full Then
            selectSql &= " WHERE AuditID > COALESCE((SELECT MAX(local_id) FROM audit_logs), 0)"
        End If
        Using cmd As New SqliteCommand(selectSql, local)
            Using reader As SqliteDataReader = cmd.ExecuteReader()
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

    Private Function SyncCompanySettings(local As SqliteConnection, pg As NpgsqlConnection,
                                         progress As Action(Of String)) As Integer
        Dim count As Integer = 0
        Dim chunk As New List(Of List(Of SyncVal))()
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

                    Dim vals As New List(Of SyncVal)()
                    vals.Add(New SyncVal(localId, NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "CompanyName"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "TIN"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Address"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Phone"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Email"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "Website"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(logoUrl, NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "BIRAuthNumber"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "PTUNumber"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(Dcoalesce(GetIntDb(reader, "ValidityYears"), 5), NpgsqlDbType.Integer))
                    vals.Add(New SyncVal(GetStr(reader, "ReceiptFooter"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetStr(reader, "CompanyHours"), NpgsqlDbType.Text))
                    vals.Add(New SyncVal(GetBoolDb(reader, "IsActive"), NpgsqlDbType.Boolean))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "DateCreated"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Dcoalesce(GetDateDb(reader, "LastModified"), Date.UtcNow), NpgsqlDbType.TimestampTz))
                    vals.Add(New SyncVal(Date.UtcNow, NpgsqlDbType.TimestampTz))
                    chunk.Add(vals)

                    If chunk.Count >= ChunkSize Then
                        UpsertChunk(pg, chunk, CompanySettingsInsert, CompanySettingsSuffix, Nothing)
                        count += chunk.Count
                        chunk.Clear()
                    End If
                End While
            End Using
        End Using
        If chunk.Count > 0 Then
            UpsertChunk(pg, chunk, CompanySettingsInsert, CompanySettingsSuffix, Nothing)
            count += chunk.Count
        End If
        ReportProgress(progress, $"company_settings synced: {count}")
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
            "inventory_logs", "audit_logs", "company_settings"
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
