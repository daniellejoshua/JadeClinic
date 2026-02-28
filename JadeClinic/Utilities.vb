Imports Microsoft.Data.SqlClient
Imports System.Data
Imports System.Security.Cryptography
Imports System.Text

Module Utilities

    ''' <summary>
    ''' Utility class for managing user passkeys.
    ''' Updated to store passkeys in a single comma-separated column named "Passkeys".
    ''' </summary>
    Public Class PasskeyManager

        ''' <summary>
        ''' Generate new passkeys for a user and store them as a single comma-separated string in Users.Passkeys
        ''' </summary>
        Public Shared Function GeneratePasskeys(userId As Integer) As String()
            Try
                Dim passkeys As String() = DatabaseInitializer.GenerateRandomPasskeys(3)
                Dim combined As String = String.Join(",", passkeys)

                Dim updateQuery As String = "UPDATE Users SET Passkeys = @Passkeys WHERE UserID = @UserID"
                Dim parameters() As SqlParameter = {
                New SqlParameter("@Passkeys", combined),
                New SqlParameter("@UserID", userId)
            }

                Utilities.ExecuteNonQuery(updateQuery, parameters)

                Utilities.LogAudit("", "Passkeys Generated", $"Generated new passkeys for user ID: {userId}", userId)

                Return passkeys
            Catch ex As Exception
                Throw New Exception($"Error generating passkeys: {ex.Message}")
            End Try
        End Function

        ''' <summary>
        ''' Validate a passkey for password reset (checks within comma-separated Passkeys column)
        ''' Returns UserID if valid, -1 if invalid
        ''' </summary>
        Public Shared Function ValidatePasskey(passkeyCode As String) As Integer
            Try
                Dim cleanPasskey As String = passkeyCode.Trim().ToUpper()

                Dim query As String =
                "SELECT UserID FROM Users " &
                "WHERE IsActive = 1 AND (',' + ISNULL(Passkeys, '') + ',') LIKE '%,' + @Passkey + ',%'"

                Dim parameters() As SqlParameter = {
                New SqlParameter("@Passkey", cleanPasskey)
            }

                Dim result = Utilities.ExecuteScalar(query, parameters)

                If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                    Return Convert.ToInt32(result)
                Else
                    Return -1
                End If
            Catch ex As Exception
                Console.WriteLine($"Error validating passkey: {ex.Message}")
                Return -1
            End Try
        End Function

        ''' <summary>
        ''' Clear a used passkey for a given user by removing it from the comma-separated Passkeys value.
        ''' </summary>
        Public Shared Sub ClearUsedPasskey(passkeyCode As String, userId As Integer)
            Try
                Dim cleanPasskey As String = passkeyCode.Trim().ToUpper()

                ' Read existing passkeys
                Dim queryRead As String = "SELECT Passkeys FROM Users WHERE UserID = @UserID"
                Dim paramRead() As SqlParameter = {New SqlParameter("@UserID", userId)}
                Dim existing As Object = Utilities.ExecuteScalar(queryRead, paramRead)

                If existing Is Nothing OrElse IsDBNull(existing) Then
                    Return
                End If

                Dim passkeysStr As String = existing.ToString()
                Dim parts As New List(Of String)(passkeysStr.Split(","c).Select(Function(p) p.Trim().ToUpper()).Where(Function(p) Not String.IsNullOrEmpty(p)))

                ' Remove matching passkey(s)
                parts = parts.Where(Function(p) p <> cleanPasskey).ToList()

                Dim newValue As Object = DBNull.Value
                If parts.Count > 0 Then
                    newValue = String.Join(",", parts)
                End If

                Dim updateQuery As String = "UPDATE Users SET Passkeys = @Passkeys WHERE UserID = @UserID"
                Dim parameters() As SqlParameter = {
                New SqlParameter("@Passkeys", If(newValue Is DBNull.Value, CType(DBNull.Value, Object), newValue)),
                New SqlParameter("@UserID", userId)
            }
                Utilities.ExecuteNonQuery(updateQuery, parameters)

                Utilities.LogAudit("", "Passkey Used", $"Passkey used for password reset. Code: {cleanPasskey}", userId)
            Catch ex As Exception
                Console.WriteLine($"Error clearing used passkey: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Get user's available passkeys (non-empty) from the single Passkeys column.
        ''' </summary>
        Public Shared Function GetAvailablePasskeys(userId As Integer) As String()
            Try
                Dim query As String = "SELECT Passkeys FROM Users WHERE UserID = @UserID AND IsActive = 1"
                Dim parameters() As SqlParameter = {New SqlParameter("@UserID", userId)}
                Dim result = Utilities.ExecuteScalar(query, parameters)

                If result Is Nothing OrElse IsDBNull(result) Then
                    Return New String() {}
                End If

                Dim passkeys As String() = result.ToString().Split(","c).Select(Function(p) p.Trim()).Where(Function(p) Not String.IsNullOrEmpty(p)).ToArray()
                Return passkeys
            Catch ex As Exception
                Console.WriteLine($"Error getting available passkeys: {ex.Message}")
                Return New String() {}
            End Try
        End Function

        ''' <summary>
        ''' Get how many passkeys a user has (non-empty entries in the comma-separated column).
        ''' </summary>
        Public Shared Function GetPasskeyCount(userId As Integer) As Integer
            Try
                Dim arr As String() = GetAvailablePasskeys(userId)
                Return arr.Length
            Catch ex As Exception
                Console.WriteLine($"Error getting passkey count: {ex.Message}")
                Return 0
            End Try
        End Function

        ''' <summary>
        ''' Clear all passkeys for a user by setting the Passkeys column to NULL.
        ''' </summary>
        Public Shared Sub ClearAllPasskeys(userId As Integer)
            Try
                Dim query As String = "UPDATE Users SET Passkeys = NULL WHERE UserID = @UserID"
                Dim parameters() As SqlParameter = {New SqlParameter("@UserID", userId)}
                Utilities.ExecuteNonQuery(query, parameters)
            Catch ex As Exception
                Console.WriteLine($"Error clearing all passkeys: {ex.Message}")
            End Try
        End Sub

        ''' <summary>
        ''' Validate passkey format (should be exactly 6 letters)
        ''' </summary>
        Public Shared Function IsValidPasskeyFormat(passkey As String) As Boolean
            If String.IsNullOrWhiteSpace(passkey) Then Return False

            Dim cleaned As String = passkey.Trim().ToUpper()
            If cleaned.Length <> 6 Then Return False

            For Each c As Char In cleaned
                If Not Char.IsLetter(c) Then Return False
            Next

            Return True
        End Function

    End Class
    ' Execute a SELECT query and return a SqlDataReader
    Public Function ExecuteReader(query As String, ParamArray parameters As SqlParameter()) As SqlDataReader
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Dim conn As New SqlConnection(connStr)
            conn.Open()

            Dim cmd As New SqlCommand(query, conn)
            If parameters IsNot Nothing Then
                cmd.Parameters.AddRange(parameters)
            End If

            Return cmd.ExecuteReader(CommandBehavior.CloseConnection)
        Catch ex As Exception
            Throw ex
        End Try
    End Function

    ' Execute INSERT, UPDATE, DELETE queries
    Public Function ExecuteNonQuery(query As String, ParamArray parameters As SqlParameter()) As Integer
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using cmd As New SqlCommand(query, conn)
                    If parameters IsNot Nothing Then
                        cmd.Parameters.AddRange(parameters)
                    End If
                    Return cmd.ExecuteNonQuery()
                End Using
            End Using
        Catch ex As Exception
            Throw ex
        End Try
    End Function

    ' Execute a query that returns a single value (like COUNT, MAX, etc.)
    Public Function ExecuteScalar(query As String, ParamArray parameters As SqlParameter()) As Object
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()
                Using cmd As New SqlCommand(query, conn)
                    If parameters IsNot Nothing Then
                        cmd.Parameters.AddRange(parameters)
                    End If
                    Return cmd.ExecuteScalar()
                End Using
            End Using
        Catch ex As Exception
            Throw ex
        End Try
    End Function

    ' Generate a random salt for password hashing
    Public Function GenerateSalt() As String
        Try
            Dim saltBytes(31) As Byte
            Using rng As New RNGCryptoServiceProvider()
                rng.GetBytes(saltBytes)
            End Using
            Return Convert.ToBase64String(saltBytes)
        Catch ex As Exception
            ' Fallback to GUID-based salt if crypto fails
            Return Guid.NewGuid().ToString()
        End Try
    End Function

    ' Hash a password with salt
    Public Function HashPassword(password As String, salt As String) As String
        Try
            Dim saltBytes As Byte() = Convert.FromBase64String(salt)
            Dim passwordBytes As Byte() = Encoding.UTF8.GetBytes(password)

            ' Combine password and salt
            Dim combined(passwordBytes.Length + saltBytes.Length - 1) As Byte
            Array.Copy(passwordBytes, 0, combined, 0, passwordBytes.Length)
            Array.Copy(saltBytes, 0, combined, passwordBytes.Length, saltBytes.Length)

            ' Hash the combined bytes
            Using sha256 As SHA256 = SHA256.Create()
                Dim hashBytes As Byte() = sha256.ComputeHash(combined)
                Return Convert.ToBase64String(hashBytes)
            End Using
        Catch ex As Exception
            Throw New Exception($"Password hashing failed: {ex.Message}")
        End Try
    End Function

    ' Verify password against hash
    Public Function VerifyPassword(password As String, salt As String, hash As String) As Boolean
        Try
            Dim computedHash As String = HashPassword(password, salt)
            Return computedHash = hash
        Catch ex As Exception
            Return False
        End Try
    End Function

    ' Log audit trail - Updated for new AuditLog structure (UserID only, no Username column)
    Public Sub LogAudit(username As String, action As String, details As String, Optional userID As Integer? = Nothing)
        Try
            ' If no UserID provided, try to get it from username
            Dim finalUserID As Integer? = userID

            If Not finalUserID.HasValue AndAlso Not String.IsNullOrEmpty(username) Then
                Try
                    Dim userQuery As String = "SELECT UserID FROM Users WHERE Username = @Username"
                    Dim userParams As SqlParameter() = {New SqlParameter("@Username", username)}
                    Dim result = ExecuteScalar(userQuery, userParams)
                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        finalUserID = Convert.ToInt32(result)
                    End If
                Catch ex As Exception
                    ' If username lookup fails, we'll log without UserID
                    Console.WriteLine($"Username lookup failed: {ex.Message}")
                End Try
            End If

            ' Insert audit log with UserID only (no Username column)
            Dim query As String = "INSERT INTO AuditLog (UserID, Action, Details, ActionTime) VALUES (@UserID, @Action, @Details, @ActionTime)"
            Dim parameters As SqlParameter() = {
                New SqlParameter("@UserID", If(finalUserID.HasValue, finalUserID.Value, DBNull.Value)),
                New SqlParameter("@Action", action),
                New SqlParameter("@Details", details),
                New SqlParameter("@ActionTime", DateTime.Now)
            }
            ExecuteNonQuery(query, parameters)
        Catch ex As Exception
            ' Silent fail for audit logging to prevent disrupting main functionality
            Console.WriteLine($"Audit logging failed: {ex.Message}")
        End Try
    End Sub

    ' Generate product code without dashes - format: P[ID]YYYYMMDDHHMMSS
    Public Function GenerateProductCode(productId As Integer) As String
        Try
            ' Generate product code without dashes: P[ID]YYYYMMDDHHMMSS
            Dim timestamp As String = DateTime.Now.ToString("yyyyMMddHHmmss")
            Return $"P{productId.ToString("D5")}{timestamp}"
        Catch ex As Exception
            ' Fallback to simple format if timestamp fails
            Return $"P{productId.ToString("D8")}"
        End Try
    End Function

    ' Generate SHA-256 hash for image content
    Public Function GenerateImageHash(imageBytes As Byte()) As String
        Try
            Using sha256 As SHA256 = SHA256.Create()
                Dim hashBytes As Byte() = sha256.ComputeHash(imageBytes)
                Return Convert.ToBase64String(hashBytes)
            End Using
        Catch ex As Exception
            ' Fallback to simple hash if SHA256 fails
            Return imageBytes.Length.ToString() & DateTime.Now.Ticks.ToString()
        End Try
    End Function

    ' Check if image with same hash already exists and return existing ID
    Public Function GetExistingImageId(imageHash As String) As Integer?
        Try
            Dim connStr As String = Connection.GetConnectionString()
            Using conn As New SqlConnection(connStr)
                conn.Open()

                Dim query As String = "SELECT TOP 1 ImageID FROM ProductImages WHERE ImageHash = @ImageHash"
                Using cmd As New SqlCommand(query, conn)
                    cmd.Parameters.AddWithValue("@ImageHash", imageHash)
                    Dim result = cmd.ExecuteScalar()

                    If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                        Return Convert.ToInt32(result)
                    Else
                        Return Nothing
                    End If
                End Using
            End Using
        Catch ex As Exception
            Return Nothing ' If error occurs, proceed with saving new image
        End Try
    End Function
End Module