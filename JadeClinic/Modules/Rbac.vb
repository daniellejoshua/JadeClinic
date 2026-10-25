' Central role-based access control helpers.
'
' Hierarchy (lowest to highest):
'     Staff  <  Manager  <  Admin  <  SuperAdmin
'
'   * Staff       - POS / daily operations only.
'   * Manager     - Staff + inventory, sales records, staff view, approvals.
'   * Admin       - Manager + staff accounts (up to Manager), audit & sync logs,
'                   backups and cloud sync.
'   * SuperAdmin  - Admin + database restore, demo data, terminals, forced
'                   full sync, and creating Admin/SuperAdmin accounts.
Public Module Rbac

    Public Enum AccessRole
        Staff = 1
        Manager = 2
        Admin = 3
        SuperAdmin = 4
    End Enum

    Public Const SuperAdminName As String = "SuperAdmin"
    Public Const AdminName As String = "Admin"
    Public Const ManagerName As String = "Manager"
    Public Const StaffName As String = "Staff"

    ''' <summary>Normalises a role string for comparison.</summary>
    Private Function Normalize(role As String) As String
        Return If(role, "").Trim().ToUpperInvariant().Replace(" ", "").Replace("_", "").Replace("-", "")
    End Function

    ''' <summary>Maps a stored/displayed role string to the access enum.</summary>
    Public Function ParseRole(role As String) As AccessRole
        Select Case Normalize(role)
            Case "SUPERADMIN", "SUPERADMINISTRATOR"
                Return AccessRole.SuperAdmin
            Case "ADMIN", "ADMINISTRATOR"
                Return AccessRole.Admin
            Case "MANAGER"
                Return AccessRole.Manager
            Case Else
                Return AccessRole.Staff
        End Select
    End Function

    ''' <summary>True when the supplied role string is elevated (Manager or above).</summary>
    Public Function IsElevated(role As String) As Boolean
        Return ParseRole(role) >= AccessRole.Manager
    End Function

    ''' <summary>True when the supplied role string is Admin or above.</summary>
    Public Function IsAdminRole(role As String) As Boolean
        Return ParseRole(role) >= AccessRole.Admin
    End Function

    ''' <summary>True when the supplied role string is SuperAdmin.</summary>
    Public Function IsSuperAdminRole(role As String) As Boolean
        Return ParseRole(role) = AccessRole.SuperAdmin
    End Function

    ' ---- Current session helpers -------------------------------------------

    Public Function CurrentRole() As AccessRole
        Try
            Return ParseRole(frmLoginvb.LoggedInRole)
        Catch
            Return AccessRole.Staff
        End Try
    End Function

    Public Function CurrentRoleName() As String
        Try
            Return If(String.IsNullOrWhiteSpace(frmLoginvb.LoggedInRole), StaffName, frmLoginvb.LoggedInRole)
        Catch
            Return StaffName
        End Try
    End Function

    Public Function IsAtLeast(required As AccessRole) As Boolean
        Return CurrentRole() >= required
    End Function

    Public Function IsStaff() As Boolean
        Return CurrentRole() = AccessRole.Staff
    End Function

    Public Function IsManagerOrAbove() As Boolean
        Return CurrentRole() >= AccessRole.Manager
    End Function

    Public Function IsAdminOrAbove() As Boolean
        Return CurrentRole() >= AccessRole.Admin
    End Function

    Public Function IsSuperAdmin() As Boolean
        Return CurrentRole() = AccessRole.SuperAdmin
    End Function

    ' ---- System Settings capabilities --------------------------------------

    Public Function CanCreateBackup() As Boolean
        Return IsAdminOrAbove()
    End Function

    Public Function CanRestoreDatabase() As Boolean
        Return IsSuperAdmin()
    End Function

    Public Function CanLoadDemoData() As Boolean
        Return IsSuperAdmin()
    End Function

    Public Function CanManageTerminals() As Boolean
        Return IsSuperAdmin()
    End Function

    Public Function CanForceFullSync() As Boolean
        Return IsSuperAdmin()
    End Function

    Public Function CanRunCloudSync() As Boolean
        Return IsAdminOrAbove()
    End Function

    Public Function CanViewSyncLogs() As Boolean
        Return IsAdminOrAbove()
    End Function

    Public Function CanViewAuditLogs() As Boolean
        Return IsAdminOrAbove()
    End Function

    Public Function CanManageStaff() As Boolean
        Return IsManagerOrAbove()
    End Function

    ''' <summary>Roles the current user is allowed to assign to another account.</summary>
    Public Function AssignableRoleNames() As String()
        If IsSuperAdmin() Then
            Return New String() {StaffName, ManagerName, AdminName, SuperAdminName}
        ElseIf IsAdminOrAbove() Then
            Return New String() {StaffName, ManagerName}
        Else
            Return New String() {}
        End If
    End Function

End Module
