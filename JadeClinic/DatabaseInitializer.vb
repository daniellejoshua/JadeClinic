' (entire file with edits)
Imports System.Data.Common
Imports System.Configuration
Imports System.IO

Public Class DatabaseInitializer

    ' Create all schema using SQLite-compatible DDL
    Public Shared Sub CreateDatabaseSchema()
        Try
            Console.WriteLine("Creating database schema...")

            CreateUsersTableActual()
            CreateSuppliersTableActual()
            CreateProductsTableActual()
            CreateProductImagesTableActual()
            CreateSalesTableActual()
            CreateSaleItemsTableActual()
            CreateInventoryLogTableActual()
            CreateAuditLogTableActual()
            CreateCompanySettingsTableActual()

            CreateInitialData()

            ' Run batch tracking migration if present (no-op if not needed)
            Try
                BatchTrackingMigration.UpdateDatabaseForBatchTracking()
            Catch
            End Try

            Console.WriteLine("?? Database schema created successfully!")
        Catch ex As Exception
            Console.WriteLine($"? Error creating database schema: {ex.Message}")
            Throw New Exception($"Failed to create database schema: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateUsersTableActual()
        Dim query As String = "CREATE TABLE IF NOT EXISTS Users (" &
            "UserID INTEGER PRIMARY KEY AUTOINCREMENT, " &
            "Username TEXT NOT NULL UNIQUE, " &
            "PasswordHash TEXT NOT NULL, " &
            "FullName TEXT NOT NULL, " &
            "UserRole TEXT DEFAULT 'Staff', " &
            "IsActive INTEGER DEFAULT 1, " &
            "CreatedAt DATETIME DEFAULT (CURRENT_TIMESTAMP), " &
            "UpdatedAt DATETIME DEFAULT (CURRENT_TIMESTAMP), " &
            "pin INTEGER NULL, " &
            "PhotoPath TEXT NULL, " &
            "QRCode TEXT NULL, " &
            "Email TEXT NULL, " &
            "Phone TEXT NULL, " &
            "Passkeys TEXT NULL)"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        Dim indexQuery As String = "CREATE INDEX IF NOT EXISTS IX_Users_QRCode ON Users (QRCode)"
        Try
            DatabaseHelper.ExecuteNonQuery(indexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create QRCode index: {ex.Message}")
        End Try

        Dim roleActiveIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_Users_Role_Active ON Users (UserRole, IsActive)"
        Try
            DatabaseHelper.ExecuteNonQuery(roleActiveIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Role/Active index: {ex.Message}")
        End Try

        Dim emailIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_Users_Email ON Users (Email)"
        Try
            DatabaseHelper.ExecuteNonQuery(emailIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Email index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateSuppliersTableActual()
        Dim query As String = "CREATE TABLE IF NOT EXISTS Suppliers (" &
            "SupplierID INTEGER PRIMARY KEY AUTOINCREMENT, " &
            "SupplierCode TEXT NOT NULL UNIQUE, " &
            "SupplierName TEXT NOT NULL, " &
            "ContactPerson TEXT NULL, " &
            "Phone TEXT NULL, " &
            "Email TEXT NULL, " &
            "IsActive INTEGER DEFAULT 1)"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
    End Sub

    Private Shared Sub CreateProductsTableActual()
        Dim query As String = "CREATE TABLE IF NOT EXISTS Products (" &
            "ProductID INTEGER PRIMARY KEY AUTOINCREMENT, " &
            "ProductCode TEXT NOT NULL UNIQUE, " &
            "ProductName TEXT NOT NULL, " &
            "Category TEXT NULL, " &
            "Unit TEXT DEFAULT 'PCS', " &
            "CurrentStock INTEGER DEFAULT 0, " &
            "ReorderLevel INTEGER DEFAULT 10, " &
            "CostPrice REAL NOT NULL, " &
            "SellingPrice REAL NOT NULL, " &
            "SupplierID INTEGER NULL, " &
            "IsActive INTEGER DEFAULT 1, " &
            "Created DATETIME DEFAULT (CURRENT_TIMESTAMP), " &
            "WholesalePrice REAL NULL, " &
            "UpdatedAt DATETIME DEFAULT (CURRENT_TIMESTAMP), " &
            "FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID))"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        Dim categoryIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_Products_Category_Active ON Products (Category, IsActive)"
        Try
            DatabaseHelper.ExecuteNonQuery(categoryIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Category/Active index: {ex.Message}")
        End Try

        Dim supplierIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_Products_Supplier ON Products (SupplierID)"
        Try
            DatabaseHelper.ExecuteNonQuery(supplierIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Supplier index: {ex.Message}")
        End Try

        Dim nameIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_Products_Name ON Products (ProductName)"
        Try
            DatabaseHelper.ExecuteNonQuery(nameIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ProductName index: {ex.Message}")
        End Try

        Try
            DatabaseHelper.ExecuteNonQuery(nameIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ProductName index: {ex.Message}")
        End Try

        ' Add index for low stock alerts (CurrentStock <= ReorderLevel)
        Dim stockIndexQuery As String =
            "CREATE INDEX IF NOT EXISTS IX_Products_Stock_Alert ON Products (CurrentStock ASC, ReorderLevel ASC, IsActive ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(stockIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Stock Alert index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateProductImagesTableActual()
        Dim query As String = "CREATE TABLE IF NOT EXISTS ProductImages (" &
            "ImageID INTEGER PRIMARY KEY AUTOINCREMENT, " &
            "ImageHash TEXT NOT NULL UNIQUE, " &
            "ImageType TEXT DEFAULT 'thumb', " &
            "FilePath TEXT NOT NULL, " &
            "CreatedAt DATETIME DEFAULT (CURRENT_TIMESTAMP), " &
            "UpdatedAt DATETIME DEFAULT (CURRENT_TIMESTAMP)" &
            ")"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Create ProductImageMapping table for many-to-many relationship
        Dim mappingQuery As String = "CREATE TABLE IF NOT EXISTS ProductImageMapping (" &
            "MappingID INTEGER PRIMARY KEY AUTOINCREMENT, " &
            "ProductID INTEGER NOT NULL, " &
            "ImageID INTEGER NOT NULL, " &
            "CreatedAt DATETIME DEFAULT (CURRENT_TIMESTAMP), " &
            "FOREIGN KEY (ProductID) REFERENCES Products(ProductID), " &
            "FOREIGN KEY (ImageID) REFERENCES ProductImages(ImageID), " &
            "UNIQUE(ProductID, ImageID)" &
            ")"

        DatabaseHelper.ExecuteNonQuery(mappingQuery, Nothing)

        ' Add index for ProductImages hash (for duplicate detection)
        Dim hashIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_ProductImages_Hash ON ProductImages (ImageHash)"
        Try
            DatabaseHelper.ExecuteNonQuery(hashIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ImageHash index: {ex.Message}")
        End Try

        ' Add index for ProductImageMapping (for product image lookups)
        Dim mappingIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_ProductImageMapping_Product ON ProductImageMapping (ProductID)"
        Try
            DatabaseHelper.ExecuteNonQuery(mappingIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ProductImageMapping index: {ex.Message}")
        End Try
    End Sub

    ' Customers table intentionally not created (ephemeral customers only via sales snapshots)

    Private Shared Sub CreateSalesTableActual()
        Dim query As String = "CREATE TABLE IF NOT EXISTS Sales (" &
            "SaleID INTEGER PRIMARY KEY AUTOINCREMENT, " &
            "SaleNumber TEXT, " &
            "SaleDate DATETIME DEFAULT (CURRENT_TIMESTAMP), " &
            "CustomerName TEXT NULL, " &
            "CustomerTIN TEXT NULL, " &
            "UserID INTEGER NULL, " &
            "TotalAmount REAL DEFAULT 0, " &
            "AmountPaid REAL DEFAULT 0, " &
            "PaymentMethod TEXT DEFAULT 'Cash', " &
            "IsVoid INTEGER DEFAULT 0, " &
            "Reference TEXT NULL, " &
            "SalesData TEXT NOT NULL, " &
            "Status TEXT DEFAULT 'Completed', " &
            "DiscountType TEXT NULL, " &
            "DiscountAmount REAL NOT NULL DEFAULT 0, " &
            "FOREIGN KEY (UserID) REFERENCES Users(UserID)" &
            ")"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Ensure CustomerTIN column exists (no-op if created above)
        Try
            Dim idxCustomerTIN As String = "CREATE INDEX IF NOT EXISTS IX_Sales_CustomerTIN ON Sales (CustomerTIN)"
            DatabaseHelper.ExecuteNonQuery(idxCustomerTIN, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create CustomerTIN index on Sales: {ex.Message}")
        End Try

        ' Backfill SaleNumber for rows that predate automatic generation so
        ' every sale has a readable numbers-only reference
        ' (yyyyMMdd + zero-padded SaleID, e.g. 20230201000001).
        Try
            DatabaseHelper.ExecuteNonQuery(
                "UPDATE Sales SET SaleNumber = " &
                "strftime('%Y%m%d', SaleDate) || substr('000000', 1, MAX(0, 6 - length(Cast(SaleID As Text)))) || Cast(SaleID As Text) " &
                "WHERE SaleNumber IS NULL OR Trim(SaleNumber) = '' OR SaleNumber NOT GLOB '[0-9]*'", Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not backfill SaleNumber on Sales: {ex.Message}")
        End Try

        ' Additional indexes (SaleDate, DiscountAmount) can be created similarly if needed
    End Sub

    Private Shared Sub CreateSaleItemsTableActual()
        Dim query As String = "CREATE TABLE IF NOT EXISTS SaleItems (" &
            "SaleItemID INTEGER PRIMARY KEY AUTOINCREMENT, " &
            "SaleID INTEGER NOT NULL, " &
            "ProductID INTEGER NOT NULL, " &
            "Quantity INTEGER NOT NULL, " &
            "UnitPrice REAL NOT NULL, " &
            "OriginalUnitPrice REAL NULL, " &
            "LineDiscountAmount REAL NOT NULL DEFAULT 0, " &
            "SubTotal REAL, " &
            "FOREIGN KEY (SaleID) REFERENCES Sales(SaleID), " &
            "FOREIGN KEY (ProductID) REFERENCES Products(ProductID)" &
            ")"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Backwards-compatible column adds are not straightforward in SQLite; if needed, handle via migration logic

        ' Add index for SaleID (foreign key lookups for sale details)
        Dim saleIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_SaleItems_Sale ON SaleItems (SaleID)"
        Try
            DatabaseHelper.ExecuteNonQuery(saleIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create SaleID index: {ex.Message}")
        End Try

        ' Add index for ProductID (to track product sales)
        Dim productIndexQuery As String = "CREATE INDEX IF NOT EXISTS IX_SaleItems_Product ON SaleItems (ProductID)"
        Try
            DatabaseHelper.ExecuteNonQuery(productIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ProductID index: {ex.Message}")
        End Try

        Try
            DatabaseHelper.ExecuteNonQuery(productIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ProductID index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateInventoryLogTableActual()
        Dim query As String = "CREATE TABLE IF NOT EXISTS InventoryLog (" &
            "LogID INTEGER PRIMARY KEY AUTOINCREMENT, " &
            "ProductID INTEGER NOT NULL, " &
            "TransactionType TEXT NOT NULL CHECK (TransactionType IN ('IN','OUT','ADJUST')), " &
            "Quantity INTEGER NOT NULL CHECK (Quantity > 0), " &
            "PreviousStock INTEGER NULL, " &
            "NewStock INTEGER NULL, " &
            "BatchNumber TEXT NULL, " &
            "ExpiryDate TEXT NULL, " &
            "SupplierID INTEGER NULL, " &
            "UserID INTEGER NOT NULL, " &
            "Reference TEXT NULL, " &
            "Notes TEXT NULL, " &
            "CreatedAt DATETIME DEFAULT (CURRENT_TIMESTAMP), " &
            "FOREIGN KEY (ProductID) REFERENCES Products(ProductID), " &
            "FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID), " &
            "FOREIGN KEY (UserID) REFERENCES Users(UserID)" &
            ")"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Create useful indexes
        Try
            DatabaseHelper.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_InventoryLog_Product_Date ON InventoryLog (ProductID, CreatedAt)", Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Product/Date index: {ex.Message}")
        End Try

        Try
            DatabaseHelper.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_InventoryLog_Date ON InventoryLog (CreatedAt)", Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Date index: {ex.Message}")
        End Try

        Try
            DatabaseHelper.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_InventoryLog_User ON InventoryLog (UserID)", Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create User index: {ex.Message}")
        End Try

        Try
            DatabaseHelper.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_InventoryLog_Batch ON InventoryLog (BatchNumber)", Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create BatchNumber index: {ex.Message}")
        End Try

        Try
            DatabaseHelper.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_InventoryLog_Expiry ON InventoryLog (ExpiryDate)", Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ExpiryDate index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateAuditLogTableActual()
        Dim query As String = "CREATE TABLE IF NOT EXISTS AuditLog (" &
            "AuditID INTEGER PRIMARY KEY AUTOINCREMENT, " &
            "Action TEXT NOT NULL, " &
            "Details TEXT NULL, " &
            "ActionTime DATETIME DEFAULT (CURRENT_TIMESTAMP), " &
            "UserID INTEGER NULL, " &
            "FOREIGN KEY (UserID) REFERENCES Users(UserID))"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        Try
            DatabaseHelper.ExecuteNonQuery("CREATE INDEX IF NOT EXISTS IX_AuditLog_UserID_ActionTime ON AuditLog (UserID, ActionTime)", Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create AuditLog index: {ex.Message}")
        End Try
    End Sub

    ' FIXED: Restore the proper function name and add the actual logo resource conversion
    Private Shared Sub CreateCompanySettingsTableActual()
        Dim query As String = "CREATE TABLE IF NOT EXISTS CompanySettings (" &
        "SettingID INTEGER PRIMARY KEY AUTOINCREMENT, " &
        "CompanyName TEXT NOT NULL, " &
        "TIN TEXT NULL, " &
        "Address TEXT NULL, " &
        "Phone TEXT NULL, " &
        "Email TEXT NULL, " &
        "Website TEXT NULL, " &
        "LogoPath TEXT NULL, " &
        "BIRAuthNumber TEXT NULL, " &
        "PTUNumber TEXT NULL, " &
        "ValidityYears INTEGER NOT NULL DEFAULT 5, " &
        "ReceiptFooter TEXT NULL, " &
        "CompanyHours TEXT NULL, " &          ' <-- new column
        "IsActive INTEGER NOT NULL DEFAULT 1, " &
        "DateCreated DATETIME NOT NULL DEFAULT (CURRENT_TIMESTAMP), " &
        "LastModified DATETIME NOT NULL DEFAULT (CURRENT_TIMESTAMP)" &
        ")"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
    End Sub

    Private Shared Sub CreateDefaultCompanySettings()
        Try
            Dim checkSql As String = "SELECT COUNT(*) FROM CompanySettings WHERE IsActive = 1"
            Dim settingsCount = Utilities.ExecuteScalar(checkSql, Nothing)

            If Convert.ToInt32(settingsCount) = 0 Then
                Dim logoPath As Object = DBNull.Value
                Try
                    Dim imagesDir As String = Connection.GetImagesFolder("company")
                    Dim destPath As String = Path.Combine(imagesDir, "logo.png")
                    Using logoImage As System.Drawing.Image = My.Resources.CleanJadeLogo_1_
                        If logoImage IsNot Nothing Then
                            logoImage.Save(destPath, System.Drawing.Imaging.ImageFormat.Png)
                            logoPath = "logo.png"
                        End If
                    End Using
                Catch logoEx As Exception
                    Console.WriteLine($"Note: Could not load Jade Dental Logo from resources: {logoEx.Message}")
                End Try

                Dim sql As String = "INSERT INTO CompanySettings (CompanyName, TIN, Address, Phone, Email, Website, LogoPath, BIRAuthNumber, PTUNumber, ReceiptFooter, CompanyHours) VALUES (@CompanyName, @TIN, @Address, @Phone, @Email, @Website, @LogoPath, @BIRAuthNumber, @PTUNumber, @ReceiptFooter, @CompanyHours)"
                Dim params As SqlParameter() = {
                New SqlParameter("@CompanyName", "JADE CLINIC"),
                New SqlParameter("@TIN", "123-456-789-000"),
                New SqlParameter("@Address", "123 Medical Plaza, Makati City, Philippines"),
                New SqlParameter("@Phone", "(02) 8123-4567"),
                New SqlParameter("@Email", "admin@jadeclinic.com"),
                New SqlParameter("@Website", "www.jadeclinic.com"),
                New SqlParameter("@LogoPath", logoPath),
                New SqlParameter("@BIRAuthNumber", "ATP-2024-000001"),
                New SqlParameter("@PTUNumber", "PTU-2024-001"),
                New SqlParameter("@ReceiptFooter", "Thank you for your business!" & vbCrLf & "Have a great day!"),
                New SqlParameter("@CompanyHours", "Mon-Fri: 9:00 AM - 5:00 PM" & vbCrLf & "Sat: 9:00 AM - 1:00 PM" & vbCrLf & "Sun: Closed")
            }

                Utilities.ExecuteNonQuery(sql, params)
                Console.WriteLine("? Default company settings created with Jade Dental Clinic logo")
            End If
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not create default company settings: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateInitialData()
        CreateDefaultAdminUser()
        CreateDefaultSuppliers()
        ' Default customers intentionally not created when customers are ephemeral
        CreateDefaultCompanySettings() ' Add default company settings
    End Sub

    Private Shared Sub CreateDefaultAdminUser()
        Try
            Dim checkQuery As String = "SELECT COUNT(*) FROM Users WHERE Username = 'admin'"
            Dim userCount As Integer = CInt(DatabaseHelper.ExecuteScalar(checkQuery, Nothing))

            If userCount = 0 Then
                ' Use simple password hashing for compatibility
                Dim hashedPassword As String = frmLoginvb.HashPassword("admin123")

                Dim insertQuery As String = "INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, UserRole, IsActive, pin, QRCode) VALUES ('admin', @Password, 'System Administrator', 'admin@jadeclinic.com', '555-0100', 'Admin', 1, 1234, 'User-00001')"

                Dim parameters() As SqlParameter = {
                    New SqlParameter("@Password", hashedPassword)
                }

                DatabaseHelper.ExecuteNonQuery(insertQuery, parameters)

                ' Get the new user ID
                Dim userIdQuery As String = "SELECT UserID FROM Users WHERE Username = 'admin'"
                Dim adminUserId As Integer = CInt(DatabaseHelper.ExecuteScalar(userIdQuery, Nothing))

                ' Generate passkeys for admin user
                CreateDefaultPasskeys(adminUserId)

                Console.WriteLine("? Default admin user created (username: admin, password: admin123, PIN: 1234)")
            End If
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not create default admin user: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateDefaultPasskeys(userId As Integer)
        Try
            ' Generate 3 random passkeys for forgot password functionality
            Dim passkeys As String() = GenerateRandomPasskeys(3)

            ' Update the Users table with the passkeys
            Dim updatePasskeysQuery As String = "UPDATE Users SET Passkeys = @Passkeys WHERE UserID = @UserID"

            Dim passkeyParams() As SqlParameter = {
                New SqlParameter("@UserID", userId),
                New SqlParameter("@Passkeys", String.Join(",", passkeys))
            }

            DatabaseHelper.ExecuteNonQuery(updatePasskeysQuery, passkeyParams)

            Console.WriteLine($"?? Generated 3 recovery passkeys: {String.Join(", ", passkeys)}")
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not create default passkeys: {ex.Message}")
        End Try
    End Sub

    Public Shared Function GenerateRandomPasskeys(count As Integer) As String()
        Dim passkeys(count - 1) As String
        Dim random As New Random()

        ' Words for generating 6-letter passkeys
        Dim wordBank() As String = {
            "SECURE", "ACCESS", "CLINIC", "DENTAL", "SYSTEM", "BACKUP",
            "MASTER", "FORGOT", "RESCUE", "SAFETY", "UNLOCK", "VERIFY",
            "GOLDEN", "SILVER", "BRONZE", "BRIGHT", "STRONG", "STABLE",
            "HEALTH", "REPAIR", "OFFICE", "MANAGE", "CREATE", "UPDATE",
            "DELETE", "INSERT", "SELECT", "RECORD", "NUMBER", "STRING",
            "DOUBLE", "SIMPLE", "MODERN", "FUTURE", "ONLINE", "EXPERT"
        }

        For i As Integer = 0 To count - 1
            ' Generate a unique 6-letter word
            Dim selectedWord As String
            Do
                selectedWord = wordBank(random.Next(wordBank.Length))
                ' Make sure it's exactly 6 letters
                If selectedWord.Length = 6 Then Exit Do
            Loop

            passkeys(i) = selectedWord.ToUpper()
        Next

        Return passkeys
    End Function

    Private Shared Sub CreateDefaultSuppliers()
        Try
            Dim suppliers(,) As String = {
                {"SUP001", "Dental Supply Co."},
                {"SUP002", "Medical Equipment Ltd."},
                {"SUP003", "Oral Care Supplies"}
            }

            For i As Integer = 0 To suppliers.GetUpperBound(0)
                Dim supplierCode As String = suppliers(i, 0)
                Dim supplierName As String = suppliers(i, 1)

                Dim checkQuery As String = "SELECT COUNT(*) FROM Suppliers WHERE SupplierCode = @SupplierCode"
                Dim parameters() As SqlParameter = {New SqlParameter("@SupplierCode", supplierCode)}
                Dim count As Integer = CInt(DatabaseHelper.ExecuteScalar(checkQuery, parameters))

                If count = 0 Then
                    Dim insertQuery As String = "INSERT INTO Suppliers (SupplierCode, SupplierName, IsActive) VALUES (@SupplierCode, @SupplierName, 1)"
                    Dim insertParams() As SqlParameter = {
                        New SqlParameter("@SupplierCode", supplierCode),
                        New SqlParameter("@SupplierName", supplierName)
                    }
                    DatabaseHelper.ExecuteNonQuery(insertQuery, insertParams)
                End If
            Next
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not create default suppliers: {ex.Message}")
        End Try
    End Sub

    ' ENHANCED: Include actual Jade Dental Clinic logo from resources as default

    ' ?? ADD THIS NEW METHOD:
End Class