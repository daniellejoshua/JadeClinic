Imports Npgsql

''' <summary>
''' Seeds the initial Super Admin account.
'''
''' The Super Admin is the highest role in the system and is the only role that
''' can manage terminals, restore the database, load demo data, force a full
''' cloud sync, and create/promote Admin accounts.
'''
''' This seeder is idempotent: it creates the account only if no Super Admin
''' already exists, so it is safe to call on every startup.
''' </summary>
Public Module SuperAdminSeeder

    ' Default first-run credentials. These should be changed after first login.
    Public Const DefaultUsername As String = "superadmin"
    Public Const DefaultPassword As String = "superadmin123"
    Private Const DefaultPin As Integer = 2468

    Public Sub EnsureSuperAdmin()
        Try
            Dim checkSql As String =
                "SELECT COUNT(*) FROM Users " &
                "WHERE UPPER(REPLACE(REPLACE(UserRole, ' ', ''), '_', '')) " &
                "IN ('SUPERADMIN', 'SUPERADMINISTRATOR')"
            Dim existing As Integer = CInt(DatabaseHelper.ExecuteScalar(checkSql, Nothing))
            If existing > 0 Then
                Return
            End If

            ' If the username is already taken for some reason, don't clobber it.
            Dim nameCheckSql As String = "SELECT COUNT(*) FROM Users WHERE Username = @Username"
            Dim nameTaken As Integer = CInt(DatabaseHelper.ExecuteScalar(
                nameCheckSql, New NpgsqlParameter() {New NpgsqlParameter("@Username", DefaultUsername)}))
            If nameTaken > 0 Then
                Console.WriteLine($"SuperAdminSeeder: username '{DefaultUsername}' already exists but has no Super Admin role; skipping seed.")
                Return
            End If

            Dim hashedPassword As String = frmLoginvb.HashPassword(DefaultPassword)

            Dim insertSql As String =
                "INSERT INTO Users (Username, PasswordHash, FullName, Email, Phone, UserRole, IsActive, pin, CreatedAt, UpdatedAt) " &
                "VALUES (@Username, @PasswordHash, @FullName, @Email, @Phone, @UserRole, 1, @Pin, now(), now()) " &
                "RETURNING UserID"

            Dim parameters() As NpgsqlParameter = {
                New NpgsqlParameter("@Username", DefaultUsername),
                New NpgsqlParameter("@PasswordHash", hashedPassword),
                New NpgsqlParameter("@FullName", "Super Administrator"),
                New NpgsqlParameter("@Email", "superadmin@jadeclinic.com"),
                New NpgsqlParameter("@Phone", "(02) 8123-4567"),
                New NpgsqlParameter("@UserRole", Rbac.SuperAdminName),
                New NpgsqlParameter("@Pin", DefaultPin)
            }

            Dim userId As Integer = CInt(DatabaseHelper.ExecuteScalar(insertSql, parameters))

            ' Assign a unique QR code and employee code derived from the new ID.
            Try
                DatabaseHelper.ExecuteNonQuery(
                    "UPDATE Users SET QRCode = 'User-' || lpad(UserID::TEXT, 5, '0'), " &
                    "EmployeeCode = UserID::TEXT || to_char((CreatedAt AT TIME ZONE 'UTC'), 'YYYYMMDDHH24MISS') " &
                    "WHERE UserID = @UserID",
                    New NpgsqlParameter() {
                        New NpgsqlParameter("@UserID", userId)
                    })

                DatabaseHelper.ExecuteNonQuery(
                    "UPDATE Users SET Passkeys = @Passkeys WHERE UserID = @UserID",
                    New NpgsqlParameter() {
                        New NpgsqlParameter("@UserID", userId),
                        New NpgsqlParameter("@Passkeys", String.Join(",", DatabaseInitializer.GenerateRandomPasskeys(3)))
                    })
            Catch ex As Exception
                Console.WriteLine($"SuperAdminSeeder: could not finalize Super Admin identifiers: {ex.Message}")
            End Try

            Console.WriteLine($"SuperAdminSeeder: default Super Admin created (username: {DefaultUsername}, password: {DefaultPassword}, PIN: {DefaultPin})")
        Catch ex As Exception
            Console.WriteLine($"SuperAdminSeeder: failed to seed Super Admin: {ex.Message}")
        End Try
    End Sub

End Module
