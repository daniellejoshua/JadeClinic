Imports System.IO
Imports System.Diagnostics
Imports System.Reflection

Public Class AutoUpdater
    Public Shared Function GetCurrentVersion() As Version
        Return Assembly.GetExecutingAssembly().GetName().Version
    End Function

    Public Shared Function CheckForUpdate(updatePath As String) As Version
        Dim versionFile As String = Path.Combine(updatePath, "version.txt")
        If Not File.Exists(versionFile) Then Return Nothing
        Dim remoteVersionStr As String = File.ReadAllText(versionFile).Trim()
        Dim remoteVersion As Version
        If Not Version.TryParse(remoteVersionStr, remoteVersion) Then Return Nothing
        If remoteVersion <= GetCurrentVersion() Then Return Nothing
        Return remoteVersion
    End Function

    Public Shared Sub DownloadUpdate(updatePath As String, destDir As String,
                                     Optional progress As Action(Of Integer, String) = Nothing)
        Directory.CreateDirectory(destDir)
        Report(progress, 0, "Preparing update...")

        ' The main executable is the bulk of the download (self-contained
        ' single file), so map it to 0-90% and the rest to 90-100%.
        CopyFileWithProgress(Path.Combine(updatePath, "JadeClinic.exe"),
                             Path.Combine(destDir, "JadeClinic.exe"),
                             Sub(pct) Report(progress, CInt(pct * 0.9), $"Downloading application... {pct}%"))

        Report(progress, 92, "Downloading settings...")
        Dim configSrc As String = Path.Combine(updatePath, "JadeClinic.dll.config")
        If File.Exists(configSrc) Then
            CopyFile(configSrc, Path.Combine(destDir, "JadeClinic.dll.config"))
        End If

        Report(progress, 96, "Downloading fonts...")
        Dim fontsSrc As String = Path.Combine(updatePath, "LatoFont")
        If Directory.Exists(fontsSrc) Then
            CopyDirectory(fontsSrc, Path.Combine(destDir, "LatoFont"))
        End If

        Report(progress, 100, "Ready to install")
    End Sub

    Private Shared Sub Report(progress As Action(Of Integer, String), pct As Integer, text As String)
        If progress IsNot Nothing Then progress(Math.Max(0, Math.Min(100, pct)), text)
    End Sub

    Private Shared Sub CopyFileWithProgress(src As String, dst As String, onProgress As Action(Of Integer))
        If File.Exists(dst) Then File.Delete(dst)
        Using input As New FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read)
            Using output As New FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None)
                Dim buffer(81919) As Byte ' 80 KB
                Dim total As Long = input.Length
                Dim copied As Long = 0L
                Dim lastPct As Integer = -1
                While True
                    Dim read As Integer = input.Read(buffer, 0, buffer.Length)
                    If read <= 0 Then Exit While
                    output.Write(buffer, 0, read)
                    copied += read
                    Dim pct As Integer = If(total > 0L, CInt(copied * 100L \ total), 0)
                    If pct <> lastPct Then
                        lastPct = pct
                        If onProgress IsNot Nothing Then onProgress(pct)
                    End If
                End While
            End Using
        End Using
    End Sub

    Public Shared Sub ApplyUpdateAndRestart(updateDir As String, appDir As String)
        Dim psScript As String = $"
$retry = 0
while ($retry -lt 30) {{
    try {{
        $proc = Get-Process -Id {Process.GetCurrentProcess().Id} -ErrorAction Stop
        Start-Sleep -Seconds 1
        $retry++
    }} catch {{
        break
    }}
}}
Start-Sleep -Seconds 1
Copy-Item '{updateDir}\JadeClinic.exe' '{appDir}\JadeClinic.exe' -Force
if (Test-Path '{updateDir}\JadeClinic.dll.config') {{
    Copy-Item '{updateDir}\JadeClinic.dll.config' '{appDir}\JadeClinic.dll.config' -Force
}}
if (Test-Path '{updateDir}\LatoFont') {{
    Copy-Item '{updateDir}\LatoFont\*' '{appDir}\LatoFont\' -Recurse -Force
}}
Start-Process '{appDir}\JadeClinic.exe'
Remove-Item '{updateDir}' -Recurse -Force
"
        Dim psPath As String = Path.Combine(Path.GetTempPath(), "JadeUpdater.ps1")
        File.WriteAllText(psPath, psScript)
        Process.Start(New ProcessStartInfo("powershell.exe", $"-ExecutionPolicy Bypass -File ""{psPath}""") With {
            .WindowStyle = ProcessWindowStyle.Hidden,
            .CreateNoWindow = True
        })
        Application.Exit()
    End Sub

    Private Shared Sub CopyFile(src As String, dst As String)
        If File.Exists(dst) Then File.Delete(dst)
        File.Copy(src, dst)
    End Sub

    Private Shared Sub CopyDirectory(src As String, dst As String)
        Directory.CreateDirectory(dst)
        For Each f In Directory.GetFiles(src)
            CopyFile(f, Path.Combine(dst, Path.GetFileName(f)))
        Next
        For Each d In Directory.GetDirectories(src)
            CopyDirectory(d, Path.Combine(dst, New DirectoryInfo(d).Name))
        Next
    End Sub
End Class
