# ?? IdleTimeoutManager Usage Guide

## ?? **Overview**
The `IdleTimeoutManager` provides automatic session timeout functionality for your JadeClinic application. After 30 seconds of user inactivity, it displays a password re-authentication dialog.

## ?? **Features**
- ? **Automatic Detection** - Monitors mouse movements, clicks, and key presses
- ? **Password Re-authentication** - Uses existing user password (BCrypt or SHA256)
- ? **Session Continuation** - Allows users to continue working after authentication
- ? **Audit Logging** - Records session timeouts and continuations
- ? **Logout Option** - Users can choose to logout instead of re-authenticating

## ?? **Current Settings**
- **Timeout Duration:** 30 seconds (for testing)
- **Target:** Change to 300 seconds (5 minutes) for production

## ??? **Implementation**

### **1. Add to Form Load Event**
```vb
Private Sub YourForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
    ' Your existing initialization code...
    
    ' Start idle timeout monitoring
    IdleTimeoutManager.Instance.StartMonitoring(Me)
End Sub
```

### **2. Add to Form Closing Event**
```vb
Private Sub YourForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
    ' Stop idle timeout monitoring
    IdleTimeoutManager.Instance.StopMonitoring(Me)
    
    ' Your existing closing logic...
End Sub
```

## ?? **Forms Already Updated**
- ? **Dashboard.vb** - Main dashboard with statistics
- ? **Sales.vb** - POS/Sales form
- ? **Staff.vb** - Staff management form

## ?? **Forms to Update**
- ? **Inventory.vb** - Add idle timeout monitoring
- ? **InventoryLog.vb** - Add idle timeout monitoring
- ? **Other forms** - Add as needed

## ?? **Dialog Appearance**
```
???????????????????????????????????????
? ?? Session Timeout                  ?
???????????????????????????????????????
?                                     ?
? Your session has timed out due to   ?
? inactivity. Please enter your       ?
? password to continue.               ?
?                                     ?
? User: admin                         ?
? [••••••••••••••••••••••••]         ?
?                                     ?
?           [Continue] [Logout]       ?
???????????????????????????????????????
```

## ?? **Customization Options**

### **Change Timeout Duration**
```vb
' In IdleTimeoutManager.vb, modify this line:
Private ReadOnly IDLE_TIMEOUT_SECONDS As Integer = 300 ' 5 minutes for production
```

### **Temporarily Disable Timer**
```vb
' Before showing dialogs or long operations
IdleTimeoutManager.Instance.DisableTimer()

' Show your dialog...

' Re-enable after operation
IdleTimeoutManager.Instance.EnableTimer()
```

### **Manual Reset (if needed)**
```vb
' Reset the timer manually (automatically done on user activity)
IdleTimeoutManager.Instance.ResetIdleTimer()
```

## ?? **Audit Logging**
The system automatically logs:
- ?? **"Session Timeout Logout"** - User logged out due to timeout
- ?? **"Session Continued"** - User re-authenticated successfully

## ?? **Production Deployment**
1. Change `IDLE_TIMEOUT_SECONDS` from 30 to 300 (5 minutes)
2. Test on all forms where implemented
3. Ensure all database connections work correctly
4. Verify audit logging functionality

## ?? **Important Notes**
- The manager is a **singleton** - one instance handles all forms
- Automatically detects user activity on ALL controls recursively
- Blocks interaction with form during password prompt
- Uses existing authentication system (same as login)
- Gracefully handles form closing and cleanup

## ?? **Testing Process**
1. Login to application normally
2. Open any form (Dashboard, Sales, Staff)
3. Wait 30 seconds without touching mouse/keyboard
4. Password dialog should appear automatically
5. Enter correct password to continue OR click Logout
6. Verify session continues or returns to login

## ?? **Required Dependencies**
- ? BCrypt.Net (for password verification)
- ? Microsoft.Data.SqlClient (for database queries)
- ? Utilities.vb (for logging and database operations)
- ? frmLoginvb.vb (for user session management)

**Perfect for maintaining application security while providing user-friendly session management!** ??