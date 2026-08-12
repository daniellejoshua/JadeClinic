Imports System.IO
Imports System.Text
Imports System.Collections.Generic
Imports System.Runtime.InteropServices

' Native ESC/POS receipt printing for 58mm/80mm thermal printers (384 dots/line).
' GDI (PrintDocument) rendering is unreliable on cheap ESC/POS drivers - text
' gets clipped or misaligned. Sending raw ESC/POS gives a pixel-correct layout:
' 32 characters per line on the standard 12-dot Font A, 42 on the small Font B.
' Used automatically when a thermal/receipt printer is detected; GDI remains
' the fallback for regular printers.
Public Module EscPosPrinter

    Public Structure EscLine
        Public Text As String
        Public Align As Integer      ' 0 = left, 1 = center, 2 = right
        Public Bold As Boolean
        Public FontB As Boolean      ' small 9-dot font (42 chars/line)
        Public DoubleHeight As Boolean

        Public Sub New(text As String, Optional align As Integer = 0,
                       Optional bold As Boolean = False, Optional fontB As Boolean = False,
                       Optional doubleHeight As Boolean = False)
            Me.Text = text
            Me.Align = align
            Me.Bold = bold
            Me.FontB = fontB
            Me.DoubleHeight = doubleHeight
        End Sub
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)>
    Private Structure DOCINFOW
        <MarshalAs(UnmanagedType.LPWStr)> Public pDocName As String
        <MarshalAs(UnmanagedType.LPWStr)> Public pOutputFile As String
        <MarshalAs(UnmanagedType.LPWStr)> Public pDataType As String
    End Structure

    <DllImport("winspool.drv", EntryPoint:="OpenPrinterW", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Private Function OpenPrinter(ByVal szPrinter As String, ByRef hPrinter As IntPtr, ByVal pDefault As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", EntryPoint:="ClosePrinter", SetLastError:=True)>
    Private Function ClosePrinter(ByVal hPrinter As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", EntryPoint:="StartDocPrinterW", SetLastError:=True, CharSet:=CharSet.Unicode)>
    Private Function StartDocPrinter(ByVal hPrinter As IntPtr, ByVal level As Integer, ByRef di As DOCINFOW) As Boolean
    End Function

    <DllImport("winspool.drv", EntryPoint:="EndDocPrinter", SetLastError:=True)>
    Private Function EndDocPrinter(ByVal hPrinter As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", EntryPoint:="StartPagePrinter", SetLastError:=True)>
    Private Function StartPagePrinter(ByVal hPrinter As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", EntryPoint:="EndPagePrinter", SetLastError:=True)>
    Private Function EndPagePrinter(ByVal hPrinter As IntPtr) As Boolean
    End Function

    <DllImport("winspool.drv", EntryPoint:="WritePrinter", SetLastError:=True)>
    Private Function WritePrinter(ByVal hPrinter As IntPtr, ByVal pBytes As IntPtr, ByVal dwCount As Integer, ByRef dwWritten As Integer) As Boolean
    End Function

    ' Build the ESC/POS byte stream from the receipt lines and send it raw.
    ' Returns False on any failure so the caller can fall back to GDI.
    Public Function PrintReceipt(printerName As String, lines As List(Of EscLine)) As Boolean
        Try
            If String.IsNullOrWhiteSpace(printerName) OrElse lines Is Nothing Then Return False
            Using ms As New MemoryStream()
                ms.WriteByte(&H1B) : ms.WriteByte(&H40)       ' ESC @ initialize

                For Each line As EscLine In lines
                    ms.WriteByte(&H1B) : ms.WriteByte(&H61)   ' ESC a alignment
                    ms.WriteByte(CByte(line.Align And 3))

                    Dim style As Integer = 0
                    If line.FontB Then style = style Or 1
                    If line.Bold Then style = style Or 8
                    If line.DoubleHeight Then style = style Or 16
                    ms.WriteByte(&H1B) : ms.WriteByte(&H21)   ' ESC ! font/style
                    ms.WriteByte(CByte(style))

                    Dim width As Integer = If(line.FontB, 42, 32)
                    Dim tb As Byte() = EncodeText(line.Text, width)
                    ms.Write(tb, 0, tb.Length)

                    ms.WriteByte(&H0A)                        ' LF
                Next

                ms.WriteByte(&H1B) : ms.WriteByte(&H64) : ms.WriteByte(4)   ' ESC d 4 feed 4 lines
                ms.WriteByte(&H1D) : ms.WriteByte(&H56) : ms.WriteByte(1)   ' GS V 1 partial cut

                Return SendToPrinter(printerName, ms.ToArray())
            End Using
        Catch ex As Exception
            Console.WriteLine($"EscPosPrinter.PrintReceipt error: {ex.Message}")
            Return False
        End Try
    End Function

    Public Function SendToPrinter(printerName As String, bytes As Byte()) As Boolean
        If String.IsNullOrWhiteSpace(printerName) OrElse bytes Is Nothing OrElse bytes.Length = 0 Then Return False
        Dim hPrinter As IntPtr = IntPtr.Zero
        Try
            If Not OpenPrinter(printerName, hPrinter, IntPtr.Zero) Then
                Console.WriteLine($"OpenPrinter failed for '{printerName}': {Marshal.GetLastWin32Error()}")
                Return False
            End If

            Dim di As New DOCINFOW()
            di.pDocName = "JadeClinic Receipt"
            di.pDataType = "RAW"
            If Not StartDocPrinter(hPrinter, 1, di) Then
                Console.WriteLine($"StartDocPrinter failed: {Marshal.GetLastWin32Error()}")
                Return False
            End If
            If Not StartPagePrinter(hPrinter) Then
                Console.WriteLine($"StartPagePrinter failed: {Marshal.GetLastWin32Error()}")
                Return False
            End If

            Dim unmanagedBytes As IntPtr = Marshal.AllocCoTaskMem(bytes.Length)
            Try
                Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length)
                Dim dwWritten As Integer = 0
                Return WritePrinter(hPrinter, unmanagedBytes, bytes.Length, dwWritten)
            Finally
                Marshal.FreeCoTaskMem(unmanagedBytes)
            End Try
        Catch ex As Exception
            Console.WriteLine($"EscPosPrinter.SendToPrinter error: {ex.Message}")
            Return False
        Finally
            If hPrinter <> IntPtr.Zero Then
                Try : EndPagePrinter(hPrinter) : Catch : End Try
                Try : EndDocPrinter(hPrinter) : Catch : End Try
                Try : ClosePrinter(hPrinter) : Catch : End Try
            End If
        End Try
        Return False
    End Function

    ' Render a line the way the thermal printer will print it (same width and
    ' alignment), used by the on-screen receipt preview so it is WYSIWYG.
    Public Function FormatForDisplay(line As EscLine) As String
        Dim width As Integer = If(line.FontB, 42, 32)
        Dim s As String = Sanitize(line.Text)
        If s.Length > width Then s = s.Substring(0, width)
        Select Case line.Align
            Case 1 : s = s.PadLeft((width + s.Length) \ 2).PadRight(width)
            Case 2 : s = s.PadLeft(width)
            Case Else : s = s.PadRight(width)
        End Select
        Return s
    End Function

    ' Convert a line to printable ASCII bytes. The peso sign is not in the
    ' standard ESC/POS character set, so it is written as "P". Text wider than
    ' the column is truncated; unknown characters become "?".
    Private Function EncodeText(text As String, width As Integer) As Byte()
        Dim s As String = Sanitize(text)
        If s.Length > width Then s = s.Substring(0, width)
        Return Encoding.ASCII.GetBytes(s)
    End Function

    Private Function Sanitize(text As String) As String
        Dim sb As New StringBuilder()
        For Each ch As Char In text
            Dim c As Char = ch
            If c = ChrW(&H20B1) OrElse c = ChrW(&H20A6) Then
                sb.Append("P")
                Continue For
            End If
            Dim code As Integer = AscW(c)
            If code >= 32 AndAlso code <= 126 Then
                sb.Append(c)
            Else
                sb.Append("?"c)
            End If
        Next
        Return sb.ToString()
    End Function

End Module
