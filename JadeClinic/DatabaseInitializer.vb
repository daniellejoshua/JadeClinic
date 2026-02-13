Imports Microsoft.Data.SqlClient
Imports System.Configuration

Public Class DatabaseInitializer

    ' Database schema creation that matches your ACTUAL SQL script
    Public Shared Sub CreateDatabaseSchema()
        Try
            Console.WriteLine("Creating database schema...")

            ' Create tables in the right order (no foreign key dependencies first)
            CreateUsersTableActual() ' Match your real schema with passkeys in Users table
            CreateSuppliersTableActual()
            CreateCustomersTableActual()
            CreateProductsTableActual()
            CreateProductImagesTableActual()
            CreateSalesTableActual()
            CreateSaleItemsTableActual()
            CreateInventoryLogTableActual()
            CreateAuditLogTableActual()

            ' Create initial data
            CreateInitialData()

            ' Update database for batch tracking support (for existing databases)
            BatchTrackingMigration.UpdateDatabaseForBatchTracking()

            Console.WriteLine("🎉 Database schema created successfully!")

        Catch ex As Exception
            Console.WriteLine($"❌ Error creating database schema: {ex.Message}")
            Throw New Exception($"Failed to create database schema: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateUsersTableActual()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Users' AND xtype='U') " &
            "CREATE TABLE Users(" &
            "UserID int IDENTITY(1,1) PRIMARY KEY, " &
            "Username nvarchar(50) NOT NULL UNIQUE, " &
            "PasswordHash nvarchar(255) NOT NULL, " &
            "FullName nvarchar(100) NOT NULL, " &
            "UserRole nvarchar(20) DEFAULT 'Staff', " &
            "IsActive bit DEFAULT 1, " &
            "CreatedAt datetime DEFAULT getdate(), " &
            "UpdatedAt datetime DEFAULT getdate(), " &
            "pin int NULL, " &
            "Photo varbinary(max) NULL, " &
            "QRCode nvarchar(100) NULL, " &
            "Email varchar(255) NULL, " &
            "Phone varchar(20) NULL, " &
            "Passkeys nvarchar(max) NULL)"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Add index for QRCode (from your SQL script)
        Dim indexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_QRCode') " &
            "CREATE NONCLUSTERED INDEX IX_Users_QRCode ON Users (QRCode ASC) " &
            "WHERE QRCode IS NOT NULL"

        Try
            DatabaseHelper.ExecuteNonQuery(indexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create QRCode index: {ex.Message}")
        End Try

        ' Add index for UserRole and IsActive (frequently queried together)
        Dim roleActiveIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Role_Active') " &
            "CREATE NONCLUSTERED INDEX IX_Users_Role_Active ON Users (UserRole ASC, IsActive ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(roleActiveIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Role/Active index: {ex.Message}")
        End Try

        ' Add index for Email (for login by email scenarios)
        Dim emailIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Users_Email') " &
            "CREATE NONCLUSTERED INDEX IX_Users_Email ON Users (Email ASC) WHERE Email IS NOT NULL"

        Try
            DatabaseHelper.ExecuteNonQuery(emailIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Email index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateSuppliersTableActual()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Suppliers' AND xtype='U') " &
            "CREATE TABLE Suppliers(" &
            "SupplierID int IDENTITY(1,1) PRIMARY KEY, " &
            "SupplierCode nvarchar(50) NOT NULL UNIQUE, " &
            "SupplierName nvarchar(200) NOT NULL, " &
            "ContactPerson nvarchar(100) NULL, " &
            "Phone nvarchar(20) NULL, " &
            "Email nvarchar(100) NULL, " &
            "IsActive bit DEFAULT 1)"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
    End Sub

    Private Shared Sub CreateCustomersTableActual()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Customers' AND xtype='U') " &
            "CREATE TABLE Customers(" &
            "CustomerID int IDENTITY(1,1) PRIMARY KEY, " &
            "CustomerCode nvarchar(50) NOT NULL UNIQUE, " &
            "CustomerName nvarchar(200) NOT NULL, " &
            "ContactPerson nvarchar(100) NULL, " &
            "Phone nvarchar(20) NULL, " &
            "Email nvarchar(100) NULL, " &
            "CustomerType nvarchar(20) DEFAULT 'Dentist', " &
            "IsActive bit DEFAULT 1)"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
    End Sub

    Private Shared Sub CreateProductsTableActual()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' AND xtype='U') " &
            "CREATE TABLE Products(" &
            "ProductID int IDENTITY(1,1) PRIMARY KEY, " &
            "ProductCode nvarchar(50) NOT NULL UNIQUE, " &
            "ProductName nvarchar(200) NOT NULL, " &
            "Category nvarchar(100) NULL, " &
            "Unit nvarchar(20) DEFAULT 'PCS', " &
            "CurrentStock int DEFAULT 0, " &
            "ReorderLevel int DEFAULT 10, " &
            "CostPrice decimal(18,2) NOT NULL, " &
            "SellingPrice decimal(18,2) NOT NULL, " &
            "SupplierID int NULL, " &
            "IsActive bit DEFAULT 1, " &
            "Created datetime DEFAULT getdate(), " &
            "WholesalePrice decimal(18,2) NULL, " &
            "UpdatedAt datetime DEFAULT getdate(), " &
            "FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID))"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Add index for Category and IsActive (for product filtering)
        Dim categoryIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Category_Active') " &
            "CREATE NONCLUSTERED INDEX IX_Products_Category_Active ON Products (Category ASC, IsActive ASC) WHERE Category IS NOT NULL"

        Try
            DatabaseHelper.ExecuteNonQuery(categoryIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Category/Active index: {ex.Message}")
        End Try

        ' Add index for SupplierID (foreign key lookups)
        Dim supplierIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Supplier') " &
            "CREATE NONCLUSTERED INDEX IX_Products_Supplier ON Products (SupplierID ASC) WHERE SupplierID IS NOT NULL"

        Try
            DatabaseHelper.ExecuteNonQuery(supplierIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Supplier index: {ex.Message}")
        End Try

        ' Add index for ProductName (for search functionality)
        Dim nameIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Name') " &
            "CREATE NONCLUSTERED INDEX IX_Products_Name ON Products (ProductName ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(nameIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ProductName index: {ex.Message}")
        End Try

        ' Add index for low stock alerts (CurrentStock <= ReorderLevel)
        Dim stockIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Products_Stock_Alert') " &
            "CREATE NONCLUSTERED INDEX IX_Products_Stock_Alert ON Products (CurrentStock ASC, ReorderLevel ASC, IsActive ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(stockIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Stock Alert index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateProductImagesTableActual()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProductImages' AND xtype='U') " &
            "CREATE TABLE ProductImages(" &
            "ImageID int IDENTITY(1,1) PRIMARY KEY, " &
            "ImageHash nvarchar(255) NOT NULL UNIQUE, " &
            "ImageType nvarchar(10) DEFAULT 'thumb', " &
            "ImageData varbinary(max) NOT NULL, " &
            "CreatedAt datetime DEFAULT getdate(), " &
            "UpdatedAt datetime DEFAULT getdate())"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Create ProductImageMapping table for many-to-many relationship
        Dim mappingQuery As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProductImageMapping' AND xtype='U') " &
            "CREATE TABLE ProductImageMapping(" &
            "MappingID int IDENTITY(1,1) PRIMARY KEY, " &
            "ProductID int NOT NULL, " &
            "ImageID int NOT NULL, " &
            "CreatedAt datetime DEFAULT getdate(), " &
            "FOREIGN KEY (ProductID) REFERENCES Products(ProductID), " &
            "FOREIGN KEY (ImageID) REFERENCES ProductImages(ImageID), " &
            "UNIQUE(ProductID, ImageID))"

        DatabaseHelper.ExecuteNonQuery(mappingQuery, Nothing)

        ' Add index for ProductImages hash (for duplicate detection)
        Dim hashIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductImages_Hash') " &
            "CREATE NONCLUSTERED INDEX IX_ProductImages_Hash ON ProductImages (ImageHash ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(hashIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ImageHash index: {ex.Message}")
        End Try

        ' Add index for ProductImageMapping (for product image lookups)
        Dim mappingIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_ProductImageMapping_Product') " &
            "CREATE NONCLUSTERED INDEX IX_ProductImageMapping_Product ON ProductImageMapping (ProductID ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(mappingIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ProductImageMapping index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateSalesTableActual()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Sales' AND xtype='U') " &
            "CREATE TABLE Sales(" &
            "SaleID int IDENTITY(1,1) PRIMARY KEY, " &
            "SaleNumber AS ('SALE'+right('00000'+CONVERT(nvarchar(10),SaleID),5)) PERSISTED, " &
            "SaleDate datetime DEFAULT getdate(), " &
            "CustomerID int NULL, " &
            "CustomerName nvarchar(200) NULL, " &
            "UserID int NULL, " &
            "TotalAmount decimal(18,2) DEFAULT 0, " &
            "AmountPaid decimal(18,2) DEFAULT 0, " &
            "PaymentMethod nvarchar(20) DEFAULT 'Cash', " &
            "IsVoid bit DEFAULT 0, " &
            "Reference nvarchar(100) NULL, " &
            "SalesData nvarchar(max) not null," &
            "Status nvarchar(50) DEFAULT 'Completed', " &
            "FOREIGN KEY (CustomerID) REFERENCES Customers(CustomerID), " &
            "FOREIGN KEY (UserID) REFERENCES Users(UserID))"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Add index for SaleDate (for date range queries and reporting)
        Dim dateIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_Date') " &
            "CREATE NONCLUSTERED INDEX IX_Sales_Date ON Sales (SaleDate DESC)"

        Try
            DatabaseHelper.ExecuteNonQuery(dateIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create SaleDate index: {ex.Message}")
        End Try

        ' Add index for CustomerID (foreign key lookups)
        Dim customerIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_Customer') " &
            "CREATE NONCLUSTERED INDEX IX_Sales_Customer ON Sales (CustomerID ASC) WHERE CustomerID IS NOT NULL"

        Try
            DatabaseHelper.ExecuteNonQuery(customerIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Customer index: {ex.Message}")
        End Try

        ' Add index for UserID (to track sales by user)
        Dim userIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_User') " &
            "CREATE NONCLUSTERED INDEX IX_Sales_User ON Sales (UserID ASC) WHERE UserID IS NOT NULL"

        Try
            DatabaseHelper.ExecuteNonQuery(userIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create User index: {ex.Message}")
        End Try

        ' Add index for SaleNumber (for quick sale lookups)
        Dim saleNumberIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Sales_SaleNumber') " &
            "CREATE NONCLUSTERED INDEX IX_Sales_SaleNumber ON Sales (SaleNumber ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(saleNumberIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create SaleNumber index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateSaleItemsTableActual()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SaleItems' AND xtype='U') " &
            "CREATE TABLE SaleItems(" &
            "SaleItemID int IDENTITY(1,1) PRIMARY KEY, " &
            "SaleID int NOT NULL, " &
            "ProductID int NOT NULL, " &
            "Quantity int NOT NULL, " &
            "UnitPrice decimal(18,2) NOT NULL, " &
            "SubTotal AS (Quantity * UnitPrice), " &
            "FOREIGN KEY (SaleID) REFERENCES Sales(SaleID), " &
            "FOREIGN KEY (ProductID) REFERENCES Products(ProductID))"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Add index for SaleID (foreign key lookups for sale details)
        Dim saleIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_Sale') " &
            "CREATE NONCLUSTERED INDEX IX_SaleItems_Sale ON SaleItems (SaleID ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(saleIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create SaleID index: {ex.Message}")
        End Try

        ' Add index for ProductID (to track product sales)
        Dim productIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SaleItems_Product') " &
            "CREATE NONCLUSTERED INDEX IX_SaleItems_Product ON SaleItems (ProductID ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(productIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ProductID index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateInventoryLogTableActual()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='InventoryLog' AND xtype='U') " &
            "CREATE TABLE InventoryLog(" &
            "LogID int IDENTITY(1,1) PRIMARY KEY, " &
            "ProductID int NOT NULL, " &
            "TransactionType varchar(10) NOT NULL CHECK (TransactionType IN ('IN', 'OUT', 'ADJUST')), " &
            "Quantity int NOT NULL CHECK (Quantity > 0), " &
            "PreviousStock int NULL, " &
            "NewStock int NULL, " &
            "BatchNumber nvarchar(50) NULL, " &
            "ExpiryDate date NULL, " &
            "SupplierID int NULL, " &
            "UserID int NOT NULL, " &
            "Reference varchar(100) NULL, " &
            "Notes nvarchar(500) NULL, " &
            "CreatedAt datetime DEFAULT getdate(), " &
            "FOREIGN KEY (ProductID) REFERENCES Products(ProductID), " &
            "FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID), " &
            "FOREIGN KEY (UserID) REFERENCES Users(UserID))"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Add BatchNumber and ExpiryDate columns if they don't exist (for existing databases)
        Try
            Dim addBatchQuery As String = "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InventoryLog' AND COLUMN_NAME = 'BatchNumber') " &
                "ALTER TABLE InventoryLog ADD BatchNumber nvarchar(50) NULL"
            DatabaseHelper.ExecuteNonQuery(addBatchQuery, Nothing)

            Dim addExpiryQuery As String = "IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'InventoryLog' AND COLUMN_NAME = 'ExpiryDate') " &
                "ALTER TABLE InventoryLog ADD ExpiryDate date NULL"
            DatabaseHelper.ExecuteNonQuery(addExpiryQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not add batch/expiry columns: {ex.Message}")
        End Try

        ' Add index for ProductID and CreatedAt (for product history)
        Dim productDateIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryLog_Product_Date') " &
            "CREATE NONCLUSTERED INDEX IX_InventoryLog_Product_Date ON InventoryLog (ProductID ASC, CreatedAt DESC)"

        Try
            DatabaseHelper.ExecuteNonQuery(productDateIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Product/Date index: {ex.Message}")
        End Try

        ' Add index for CreatedAt (for date-based queries)
        Dim dateIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryLog_Date') " &
            "CREATE NONCLUSTERED INDEX IX_InventoryLog_Date ON InventoryLog (CreatedAt DESC)"

        Try
            DatabaseHelper.ExecuteNonQuery(dateIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create Date index: {ex.Message}")
        End Try

        ' Add index for UserID (to track who made changes)
        Dim userIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryLog_User') " &
            "CREATE NONCLUSTERED INDEX IX_InventoryLog_User ON InventoryLog (UserID ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(userIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create User index: {ex.Message}")
        End Try

        ' Add index for BatchNumber (for batch tracking queries)
        Dim batchIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryLog_Batch') " &
            "CREATE NONCLUSTERED INDEX IX_InventoryLog_Batch ON InventoryLog (BatchNumber ASC) WHERE BatchNumber IS NOT NULL"

        Try
            DatabaseHelper.ExecuteNonQuery(batchIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create BatchNumber index: {ex.Message}")
        End Try

        ' Add index for ExpiryDate (for expiry tracking)
        Dim expiryIndexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_InventoryLog_Expiry') " &
            "CREATE NONCLUSTERED INDEX IX_InventoryLog_Expiry ON InventoryLog (ExpiryDate ASC) WHERE ExpiryDate IS NOT NULL"

        Try
            DatabaseHelper.ExecuteNonQuery(expiryIndexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create ExpiryDate index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateAuditLogTableActual()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLog' AND xtype='U') " &
            "CREATE TABLE AuditLog(" &
            "AuditID int IDENTITY(1,1) PRIMARY KEY, " &
            "Action nvarchar(200) NOT NULL, " &
            "Details nvarchar(1000) NULL, " &
            "ActionTime datetime DEFAULT getdate(), " &
            "UserID int NULL, " &
            "FOREIGN KEY (UserID) REFERENCES Users(UserID))"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)

        ' Add index for AuditLog (from your SQL script)
        Dim indexQuery As String = "IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_AuditLog_UserID_ActionTime') " &
            "CREATE NONCLUSTERED INDEX IX_AuditLog_UserID_ActionTime ON AuditLog (UserID ASC, ActionTime ASC)"

        Try
            DatabaseHelper.ExecuteNonQuery(indexQuery, Nothing)
        Catch ex As Exception
            Console.WriteLine($"Note: Could not create AuditLog index: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateInitialData()
        CreateDefaultAdminUser()
        CreateDefaultSuppliers()
        CreateDefaultCustomers()
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

            Console.WriteLine($"? Generated 3 recovery passkeys: {String.Join(", ", passkeys)}")
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

    Private Shared Sub CreateDefaultCustomers()
        Try
            Dim customers(,) As String = {
                {"CUST001", "General Dentist"},
                {"CUST002", "Orthodontist Office"},
                {"CUST003", "Dental Clinic"}
            }

            For i As Integer = 0 To customers.GetUpperBound(0)
                Dim customerCode As String = customers(i, 0)
                Dim customerName As String = customers(i, 1)

                Dim checkQuery As String = "SELECT COUNT(*) FROM Customers WHERE CustomerCode = @CustomerCode"
                Dim parameters() As SqlParameter = {New SqlParameter("@CustomerCode", customerCode)}
                Dim count As Integer = CInt(DatabaseHelper.ExecuteScalar(checkQuery, parameters))

                If count = 0 Then
                    Dim insertQuery As String = "INSERT INTO Customers (CustomerCode, CustomerName, CustomerType, IsActive) VALUES (@CustomerCode, @CustomerName, 'Dentist', 1)"
                    Dim insertParams() As SqlParameter = {
                        New SqlParameter("@CustomerCode", customerCode),
                        New SqlParameter("@CustomerName", customerName)
                    }
                    DatabaseHelper.ExecuteNonQuery(insertQuery, insertParams)
                End If
            Next
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not create default customers: {ex.Message}")
        End Try
    End Sub
End Class