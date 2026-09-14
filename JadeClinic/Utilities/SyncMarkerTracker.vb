''' <summary>
''' Tracks the shared SyncMarker value for a page so it can cheaply detect
''' stock changes made by ANY PC sharing the database (via the Products
''' triggers created by SyncMarkerMigration). Each page keeps its own
''' instance and calls Reset() after it (re)loads data, then polls
''' HasChanged() on a timer to refresh silently in place.
''' </summary>
Public Class SyncMarkerTracker

    Private _lastSeen As String = ""

    ''' <summary>Sets the baseline to the current marker value.</summary>
    Public Sub Reset()
        _lastSeen = ReadMarker()
    End Sub

    ''' <summary>
    ''' True when the shared marker moved since the last Reset/HasChanged call.
    ''' Advances the internal baseline so a subsequent call returns False again.
    ''' </summary>
    Public Function HasChanged() As Boolean
        Dim current As String = ReadMarker()
        If current <> _lastSeen Then
            _lastSeen = current
            Return True
        End If
        Return False
    End Function

    Private Shared Function ReadMarker() As String
        Try
            Dim value As Object = Utilities.ExecuteScalar(
                "SELECT LastChange FROM SyncMarker WHERE MarkerID = 1", Nothing)
            If value Is Nothing OrElse IsDBNull(value) Then Return ""
            Return Convert.ToString(value)
        Catch
            ' Marker table may be missing on very old installs or when the
            ' migration failed; fall back to "no change" and keep current behavior.
            Return ""
        End Try
    End Function

End Class