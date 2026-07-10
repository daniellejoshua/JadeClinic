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

    Public Shared Sub DownloadUpdate(updatePath As String, destDir As String)
        Directory.CreateDirectory(destDir)
        CopyFile(Path.Combine(updatePath, "JadeClinic.exe"), Path.Combine(destDir, "JadeClinic.exe"))
        Dim configSrc As String = Path.Combine(updatePath, "JadeClinic.dll.config")
        If File.Exists(configSrc) Then
            CopyFile(configSrc, Path.Combine(destDir, "JadeClinic.dll.config"))
        End If
        Dim fontsSrc As String = Path.Combine(updatePath, "LatoFont")
        If Directory.Exists(fontsSrc) Then
            CopyDirectory(fontsSrc, Path.Combine(destDir, "LatoFont"))
        End If
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
