Imports Microsoft.Data.SqlClient
Imports System.Text

''' <summary>
''' Utility class for managing user passkeys (recovery keys for forgot password)
''' Stores passkeys directly in Users table as Passkey1, Passkey2, Passkey3
''' </summary>
Public Class PasskeyManager

    ''' <summary>
    ''' Generate new passkeys for a user
    ''' </summary>
    ''' <param name="userId">User ID</param>
    ''' <returns>Array of 3 generated passkeys (6 letters each)</returns>
    Public Shared Function GeneratePasskeys(userId As Integer) As String()
        Try
            ' Generate new passkeys (3 words of 6 letters each)
            Dim passkeys As String() = DatabaseInitializer.GenerateRandomPasskeys(3)
            
            ' Save to Users table
            Dim updateQuery As String = "UPDATE Users SET Passkey1 = @Passkey1, Passkey2 = @Passkey2, Passkey3 = @Passkey3 WHERE UserID = @UserID"
            
            Dim parameters() As SqlParameter = {
                New SqlParameter("@UserID", userId),
                New SqlParameter("@Passkey1", passkeys(0)),
                New SqlParameter("@Passkey2", passkeys(1)),
                New SqlParameter("@Passkey3", passkeys(2))
            }
            
            Utilities.ExecuteNonQuery(updateQuery, parameters)
            
            ' Log the action
            Utilities.LogAudit("", "Passkeys Generated", $"Generated new passkeys for user ID: {userId}", userId)
            
            Return passkeys
            
        Catch ex As Exception
            Throw New Exception($"Error generating passkeys: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Validate a passkey for password reset
    ''' </summary>
    ''' <param name="passkeyCode">The passkey code to validate (6 letters)</param>
    ''' <returns>UserID if valid, -1 if invalid</returns>
    Public Shared Function ValidatePasskey(passkeyCode As String) As Integer
        Try
            Dim cleanPasskey As String = passkeyCode.Trim().ToUpper()
            
            ' Check if passkey matches any of the 3 passkey fields
            Dim query As String = "SELECT UserID FROM Users WHERE (Passkey1 = @Passkey OR Passkey2 = @Passkey OR Passkey3 = @Passkey) AND IsActive = 1"
            
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
    ''' Clear a used passkey after successful password reset
    ''' </summary>
    ''' <param name="passkeyCode">The passkey code that was used</param>
    ''' <param name="userId">User ID for security verification</param>
    Public Shared Sub ClearUsedPasskey(passkeyCode As String, userId As Integer)
        Try
            Dim cleanPasskey As String = passkeyCode.Trim().ToUpper()
            
            ' Clear the specific passkey that was used
            Dim query As String = "UPDATE Users SET " +
                                "Passkey1 = CASE WHEN Passkey1 = @Passkey THEN NULL ELSE Passkey1 END, " +
                                "Passkey2 = CASE WHEN Passkey2 = @Passkey THEN NULL ELSE Passkey2 END, " +
                                "Passkey3 = CASE WHEN Passkey3 = @Passkey THEN NULL ELSE Passkey3 END " +
                                "WHERE UserID = @UserID"
            
            Dim parameters() As SqlParameter = {
                New SqlParameter("@Passkey", cleanPasskey),
                New SqlParameter("@UserID", userId)
            }
            
            Utilities.ExecuteNonQuery(query, parameters)
            
            ' Log the action
            Utilities.LogAudit("", "Passkey Used", $"Passkey used for password reset. Code: {cleanPasskey}", userId)
            
        Catch ex As Exception
            Console.WriteLine($"Error clearing used passkey: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Get user's available passkeys (non-null)
    ''' </summary>
    ''' <param name="userId">User ID</param>
    ''' <returns>List of available passkey codes</returns>
    Public Shared Function GetAvailablePasskeys(userId As Integer) As String()
        Try
            Dim query As String = "SELECT Passkey1, Passkey2, Passkey3 FROM Users WHERE UserID = @UserID AND IsActive = 1"
            
            Dim parameters() As SqlParameter = {
                New SqlParameter("@UserID", userId)
            }
            
            Dim passkeys As New List(Of String)()
            
            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                If reader.Read() Then
                    For i As Integer = 0 To 2
                        Dim fieldName As String = $"Passkey{i + 1}"
                        If Not IsDBNull(reader(fieldName)) AndAlso Not String.IsNullOrEmpty(reader(fieldName).ToString()) Then
                            passkeys.Add(reader(fieldName).ToString())
                        End If
                    Next
                End If
            End Using
            
            Return passkeys.ToArray()
            
        Catch ex As Exception
            Console.WriteLine($"Error getting available passkeys: {ex.Message}")
            Return New String() {}
        End Try
    End Function

    ''' <summary>
    ''' Get user information by username for passkey operations
    ''' </summary>
    ''' <param name="username">Username</param>
    ''' <returns>UserID and FullName if found</returns>
    Public Shared Function GetUserByUsername(username As String) As (UserId As Integer, FullName As String, Email As String)
        Try
            Dim query As String = "SELECT UserID, FullName, Email FROM Users WHERE Username = @Username AND IsActive = 1"
            
            Dim parameters() As SqlParameter = {
                New SqlParameter("@Username", username.Trim())
            }
            
            Using reader As SqlDataReader = Utilities.ExecuteReader(query, parameters)
                If reader.Read() Then
                    Dim emailValue As String = ""
                    If Not IsDBNull(reader("Email")) Then
                        emailValue = reader("Email").ToString()
                    End If
                    
                    Return (
                        UserId:=Convert.ToInt32(reader("UserID")),
                        FullName:=reader("FullName").ToString(),
                        Email:=emailValue
                    )
                Else
                    Return (UserId:=-1, FullName:="", Email:="")
                End If
            End Using
            
        Catch ex As Exception
            Console.WriteLine($"Error getting user by username: {ex.Message}")
            Return (UserId:=-1, FullName:="", Email:="")
        End Try
    End Function

    ''' <summary>
    ''' Check how many passkeys a user has available
    ''' </summary>
    ''' <param name="userId">User ID</param>
    ''' <returns>Number of available passkeys</returns>
    Public Shared Function GetPasskeyCount(userId As Integer) As Integer
        Try
            Dim query As String = "SELECT " +
                                "(CASE WHEN Passkey1 IS NOT NULL AND Passkey1 <> '' THEN 1 ELSE 0 END) + " +
                                "(CASE WHEN Passkey2 IS NOT NULL AND Passkey2 <> '' THEN 1 ELSE 0 END) + " +
                                "(CASE WHEN Passkey3 IS NOT NULL AND Passkey3 <> '' THEN 1 ELSE 0 END) AS PasskeyCount " +
                                "FROM Users WHERE UserID = @UserID"
            
            Dim parameters() As SqlParameter = {
                New SqlParameter("@UserID", userId)
            }
            
            Dim result = Utilities.ExecuteScalar(query, parameters)
            
            If result IsNot Nothing AndAlso Not IsDBNull(result) Then
                Return Convert.ToInt32(result)
            Else
                Return 0
            End If
            
        Catch ex As Exception
            Console.WriteLine($"Error getting passkey count: {ex.Message}")
            Return 0
        End Try
    End Function

    ''' <summary>
    ''' Clear all passkeys for a user (when generating new ones)
    ''' </summary>
    ''' <param name="userId">User ID</param>
    Public Shared Sub ClearAllPasskeys(userId As Integer)
        Try
            Dim query As String = "UPDATE Users SET Passkey1 = NULL, Passkey2 = NULL, Passkey3 = NULL WHERE UserID = @UserID"
            
            Dim parameters() As SqlParameter = {
                New SqlParameter("@UserID", userId)
            }
            
            Utilities.ExecuteNonQuery(query, parameters)
            
        Catch ex As Exception
            Console.WriteLine($"Error clearing all passkeys: {ex.Message}")
        End Try
    End Sub

    ''' <summary>
    ''' Validate passkey format (should be exactly 6 letters)
    ''' </summary>
    ''' <param name="passkey">Passkey to validate</param>
    ''' <returns>True if format is valid</returns>
    Public Shared Function IsValidPasskeyFormat(passkey As String) As Boolean
        If String.IsNullOrWhiteSpace(passkey) Then Return False
        
        ' Should be exactly 6 characters
        Dim cleaned As String = passkey.Trim().ToUpper()
        
        If cleaned.Length <> 6 Then Return False
        
        ' Check if all characters are letters
        For Each c As Char In cleaned
            If Not Char.IsLetter(c) Then Return False
        Next
        
        Return True
    End Function

End Class