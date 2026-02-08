Imports Microsoft.Data.SqlClient
Imports System.Configuration

Public Class DatabaseInitializer

    ' Simplified database schema creation that matches your actual database
    Public Shared Sub CreateDatabaseSchema()
        Try
            Console.WriteLine("Creating database schema...")

            ' Create tables in the right order (no foreign key dependencies first)
            CreateUsersTableSimple()
            CreateSuppliersTableSimple()
            CreateCustomersTableSimple()
            CreateProductsTableSimple()
            CreateProductImagesTableSimple()
            CreateSalesTableSimple()
            CreateSaleItemsTableSimple()
            CreateInventoryLogTableSimple()
            CreateAuditLogTableSimple()

            ' Create initial data
            CreateInitialData()

            Console.WriteLine("✅ Database schema created successfully!")

        Catch ex As Exception
            Console.WriteLine($"❌ Error creating database schema: {ex.Message}")
            Throw New Exception($"Failed to create database schema: {ex.Message}")
        End Try
    End Sub

    Private Shared Sub CreateUsersTableSimple()
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
            "QRCode nvarchar(100) NULL)"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
    End Sub

    Private Shared Sub CreateSuppliersTableSimple()
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

    Private Shared Sub CreateCustomersTableSimple()
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

    Private Shared Sub CreateProductsTableSimple()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Products' AND xtype='U') " &
            "CREATE TABLE Products(" &
            "ProductID int IDENTITY(1,1) PRIMARY KEY, " &
            "ProductCode nvarchar(50) NOT NULL UNIQUE, " &
            "Barcode nvarchar(100) NOT NULL UNIQUE, " &
            "ProductName nvarchar(200) NOT NULL, " &
            "Category nvarchar(100) NULL, " &
            "Unit nvarchar(20) DEFAULT 'PCS', " &
            "CurrentStock int DEFAULT 0, " &
            "ReorderLevel int DEFAULT 10, " &
            "CostPrice decimal(18,2) NOT NULL, " &
            "SellingPrice decimal(18,2) NOT NULL, " &
            "HasExpiry bit DEFAULT 0, " &
            "ExpiryDate date NULL, " &
            "SupplierID int NULL, " &
            "IsActive bit DEFAULT 1, " &
            "Created datetime DEFAULT getdate(), " &
            "WholesalePrice decimal(18,2) NULL, " &
            "UpdatedAt datetime DEFAULT getdate())"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
    End Sub

    Private Shared Sub CreateProductImagesTableSimple()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProductImages' AND xtype='U') " &
            "CREATE TABLE ProductImages(" &
            "ImageID int IDENTITY(1,1) PRIMARY KEY, " &
            "ProductID int NOT NULL, " &
            "ImageType nvarchar(10) DEFAULT 'thumb', " &
            "ImageData varbinary(max) NOT NULL, " &
            "CreatedAt datetime DEFAULT getdate(), " &
            "UpdatedAt datetime DEFAULT getdate())"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
    End Sub

    Private Shared Sub CreateSalesTableSimple()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Sales' AND xtype='U') " &
            "CREATE TABLE Sales(" &
            "SaleID int IDENTITY(1,1) PRIMARY KEY, " &
            "SaleDate datetime DEFAULT getdate(), " &
            "CustomerID int NULL, " &
            "CustomerName nvarchar(200) NULL, " &
            "UserID int NULL, " &
            "TotalAmount decimal(18,2) DEFAULT 0, " &
            "AmountPaid decimal(18,2) DEFAULT 0, " &
            "PaymentMethod nvarchar(20) DEFAULT 'Cash', " &
            "IsVoid bit DEFAULT 0, " &
            "Reference nvarchar(100) NULL)"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
    End Sub

    Private Shared Sub CreateSaleItemsTableSimple()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='SaleItems' AND xtype='U') " &
            "CREATE TABLE SaleItems(" &
            "SaleItemID int IDENTITY(1,1) PRIMARY KEY, " &
            "SaleID int NOT NULL, " &
            "ProductID int NOT NULL, " &
            "Quantity int NOT NULL, " &
            "UnitPrice decimal(18,2) NOT NULL)"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
    End Sub

    Private Shared Sub CreateInventoryLogTableSimple()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='InventoryLog' AND xtype='U') " &
            "CREATE TABLE InventoryLog(" &
            "LogID int IDENTITY(1,1) PRIMARY KEY, " &
            "ProductID int NOT NULL, " &
            "TransactionType varchar(10) NOT NULL, " &
            "Quantity int NOT NULL, " &
            "PreviousStock int NULL, " &
            "NewStock int NULL, " &
            "SupplierID int NULL, " &
            "UserID int NOT NULL, " &
            "Reference varchar(100) NULL, " &
            "Notes nvarchar(500) NULL, " &
            "CreatedAt datetime DEFAULT getdate())"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
    End Sub

    Private Shared Sub CreateAuditLogTableSimple()
        Dim query As String = "IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AuditLog' AND xtype='U') " &
            "CREATE TABLE AuditLog(" &
            "AuditID int IDENTITY(1,1) PRIMARY KEY, " &
            "Action nvarchar(200) NOT NULL, " &
            "Details nvarchar(1000) NULL, " &
            "ActionTime datetime DEFAULT getdate(), " &
            "UserID int NULL)"

        DatabaseHelper.ExecuteNonQuery(query, Nothing)
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
                Dim hashedPassword As String = frmLoginvb.HashPassword("admin123")

                Dim insertQuery As String = "INSERT INTO Users (Username, PasswordHash, FullName, UserRole, IsActive, pin, QRCode) VALUES ('admin', @Password, 'System Administrator', 'Admin', 1, 1234, 'User-00001')"

                Dim parameters() As SqlParameter = {
                    New SqlParameter("@Password", hashedPassword)
                }

                DatabaseHelper.ExecuteNonQuery(insertQuery, parameters)
                Console.WriteLine("✅ Default admin user created (username: admin, password: admin123, PIN: 1234)")
            End If
        Catch ex As Exception
            Console.WriteLine($"Warning: Could not create default admin user: {ex.Message}")
        End Try
    End Sub

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