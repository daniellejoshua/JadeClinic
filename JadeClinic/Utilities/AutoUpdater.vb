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

    ' Launches the freshly downloaded build in "apply update" mode. That process
    ' shows a visible progress window, waits for us to exit, swaps the files and
    ' relaunches JadeClinic. Called from the update dialog on the UI thread.
    Public Shared Sub LaunchUpdater(updateDir As String, appDir As String)
        Dim stagedExe As String = Path.Combine(updateDir, "JadeClinic.exe")
        If Not File.Exists(stagedExe) Then
            Throw New FileNotFoundException("The downloaded update file is missing.", stagedExe)
        End If

        Dim args As String = $"--apply-update ""{updateDir}"" ""{appDir}"" {Process.GetCurrentProcess().Id}"
        Process.Start(New ProcessStartInfo(stagedExe, args) With {
            .UseShellExecute = False,
            .WorkingDirectory = updateDir
        })
        Application.Exit()
    End Sub

    ' Runs inside the --apply-update process: waits for the previous instance to
    ' close, copies the new files over the running install, then starts it again.
    Public Shared Sub InstallUpdate(updateDir As String, appDir As String, oldPid As Integer,
                                    Optional status As Action(Of String) = Nothing)
        ReportStatus(status, "Waiting for JadeClinic to close...")
        WaitForProcessExit(oldPid, 60000)

        ReportStatus(status, "Copying application files...")
        CopyFileWithRetry(Path.Combine(updateDir, "JadeClinic.exe"),
                          Path.Combine(appDir, "JadeClinic.exe"), 40, 500)

        ReportStatus(status, "Updating settings...")
        Dim configSrc As String = Path.Combine(updateDir, "JadeClinic.dll.config")
        If File.Exists(configSrc) Then
            CopyFileWithRetry(configSrc, Path.Combine(appDir, "JadeClinic.dll.config"), 20, 500)
        End If

        ReportStatus(status, "Updating fonts...")
        Dim fontsSrc As String = Path.Combine(updateDir, "LatoFont")
        If Directory.Exists(fontsSrc) Then
            CopyDirectoryWithRetry(fontsSrc, Path.Combine(appDir, "LatoFont"), 20, 500)
        End If

        ReportStatus(status, "Restarting JadeClinic...")
        Process.Start(New ProcessStartInfo(Path.Combine(appDir, "JadeClinic.exe")) With {
            .UseShellExecute = False,
            .WorkingDirectory = appDir
        })

        ' Best-effort cleanup of the staging folder. The running updater exe is
        ' locked, so the folder may not fully delete; the next update clears it.
        Try
            Directory.Delete(updateDir, True)
        Catch
        End Try
    End Sub

    Private Shared Sub ReportStatus(status As Action(Of String), text As String)
        If status IsNot Nothing Then status(text)
    End Sub

    Private Shared Sub WaitForProcessExit(pid As Integer, timeoutMs As Integer)
        If pid <= 0 Then Return
        Dim sw As Stopwatch = Stopwatch.StartNew()
        While sw.ElapsedMilliseconds < timeoutMs
            Try
                Dim p As Process = Process.GetProcessById(pid)
                If p.HasExited Then Return
            Catch
                Return ' process no longer exists
            End Try
            System.Threading.Thread.Sleep(250)
        End While
    End Sub

    Private Shared Sub CopyFileWithRetry(src As String, dst As String, attempts As Integer, delayMs As Integer)
        Dim lastEx As Exception = Nothing
        For i As Integer = 1 To attempts
            Try
                If File.Exists(dst) Then File.Delete(dst)
                File.Copy(src, dst, True)
                Return
            Catch ex As Exception
                lastEx = ex
                System.Threading.Thread.Sleep(delayMs)
            End Try
        Next
        Throw New IOException($"Could not write ""{dst}"".", lastEx)
    End Sub

    Private Shared Sub CopyDirectoryWithRetry(src As String, dst As String, attempts As Integer, delayMs As Integer)
        Directory.CreateDirectory(dst)
        For Each f In Directory.GetFiles(src)
            CopyFileWithRetry(f, Path.Combine(dst, Path.GetFileName(f)), attempts, delayMs)
        Next
        For Each d In Directory.GetDirectories(src)
            CopyDirectoryWithRetry(d, Path.Combine(dst, New DirectoryInfo(d).Name), attempts, delayMs)
        Next
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
