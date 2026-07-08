Imports System.Data

Public Class SqlParameter
    Public Property ParameterName As String
    Public Property Value As Object
    Public Property DbType As DbType

    Public Sub New(name As String, value As Object)
        Me.ParameterName = If(name.StartsWith("@"), name, "@" & name)
        Me.Value = value
    End Sub

    Public Sub New()
    End Sub
End Class
