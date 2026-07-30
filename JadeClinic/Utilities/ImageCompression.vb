Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Security.Cryptography

Public Class ImageCompression
    ''' <summary>
    ''' Compresses an image to a specified quality and maximum dimensions
    ''' </summary>
    ''' <param name="originalImage">The original image to compress</param>
    ''' <param name="quality">JPEG quality (1-100, higher = better quality)</param>
    ''' <param name="maxWidth">Maximum width in pixels</param>
    ''' <param name="maxHeight">Maximum height in pixels</param>
    ''' <returns>Compressed image as byte array</returns>
    Public Shared Function CompressImage(originalImage As Image, Optional quality As Long = 75, Optional maxWidth As Integer = 800, Optional maxHeight As Integer = 600) As Byte()
        Try
            ' Calculate new dimensions while maintaining aspect ratio
            Dim newSize As Size = CalculateNewSize(originalImage.Size, maxWidth, maxHeight)

            ' Create compressed image
            Using compressedImage As New Bitmap(newSize.Width, newSize.Height)
                Using graphics As Graphics = Graphics.FromImage(compressedImage)
                    ' Set high-quality resize settings
                    graphics.CompositingMode = CompositingMode.SourceCopy
                    graphics.CompositingQuality = CompositingQuality.HighQuality
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic
                    graphics.SmoothingMode = SmoothingMode.HighQuality
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality

                    ' Draw the resized image
                    graphics.DrawImage(originalImage, 0, 0, newSize.Width, newSize.Height)
                End Using

                ' Convert to JPEG with specified quality
                Return ConvertToJpegBytes(compressedImage, quality)
            End Using
        Catch ex As Exception
            Throw New Exception($"Image compression failed: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Compresses an image from a file path
    ''' </summary>
    ''' <param name="imagePath">Path to the image file</param>
    ''' <param name="quality">JPEG quality (1-100)</param>
    ''' <param name="maxWidth">Maximum width in pixels</param>
    ''' <param name="maxHeight">Maximum height in pixels</param>
    ''' <returns>Compressed image as byte array</returns>
    Public Shared Function CompressImageFromFile(imagePath As String, Optional quality As Long = 75, Optional maxWidth As Integer = 800, Optional maxHeight As Integer = 600) As Byte()
        Try
            Using originalImage As Image = Image.FromFile(imagePath)
                Return CompressImage(originalImage, quality, maxWidth, maxHeight)
            End Using
        Catch ex As Exception
            Throw New Exception($"Failed to compress image from file: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Compresses an image from a byte array
    ''' </summary>
    ''' <param name="imageBytes">Original image bytes</param>
    ''' <param name="quality">JPEG quality (1-100)</param>
    ''' <param name="maxWidth">Maximum width in pixels</param>
    ''' <param name="maxHeight">Maximum height in pixels</param>
    ''' <returns>Compressed image as byte array</returns>
    Public Shared Function CompressImageFromBytes(imageBytes As Byte(), Optional quality As Long = 75, Optional maxWidth As Integer = 800, Optional maxHeight As Integer = 600) As Byte()
        Try
            Using memoryStream As New MemoryStream(imageBytes)
                Using originalImage As Image = Image.FromStream(memoryStream)
                    Return CompressImage(originalImage, quality, maxWidth, maxHeight)
                End Using
            End Using
        Catch ex As Exception
            Throw New Exception($"Failed to compress image from bytes: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Creates a thumbnail version of an image
    ''' </summary>
    ''' <param name="originalImage">Original image</param>
    ''' <param name="thumbnailSize">Thumbnail size (square)</param>
    ''' <returns>Thumbnail as byte array</returns>
    Public Shared Function CreateThumbnail(originalImage As Image, Optional thumbnailSize As Integer = 150) As Byte()
        Try
            Return CompressImage(originalImage, 70, thumbnailSize, thumbnailSize)
        Catch ex As Exception
            Throw New Exception($"Thumbnail creation failed: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Converts an image to JPEG format with specified quality
    ''' </summary>
    ''' <param name="image">Image to convert</param>
    ''' <param name="quality">JPEG quality (1-100)</param>
    ''' <returns>JPEG image as byte array</returns>
    Private Shared Function ConvertToJpegBytes(image As Image, quality As Long) As Byte()
        Try
            ' Set up JPEG encoder parameters
            Dim jpegEncoder As ImageCodecInfo = GetEncoderInfo(ImageFormat.Jpeg)
            Dim encoderParams As New EncoderParameters(1)
            encoderParams.Param(0) = New EncoderParameter(Encoder.Quality, quality)

            Using memoryStream As New MemoryStream()
                image.Save(memoryStream, jpegEncoder, encoderParams)
                Return memoryStream.ToArray()
            End Using
        Catch ex As Exception
            Throw New Exception($"JPEG conversion failed: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Calculates new image size while maintaining aspect ratio
    ''' </summary>
    ''' <param name="originalSize">Original image size</param>
    ''' <param name="maxWidth">Maximum width</param>
    ''' <param name="maxHeight">Maximum height</param>
    ''' <returns>New calculated size</returns>
    Private Shared Function CalculateNewSize(originalSize As Size, maxWidth As Integer, maxHeight As Integer) As Size
        Dim widthRatio As Double = CDbl(maxWidth) / originalSize.Width
        Dim heightRatio As Double = CDbl(maxHeight) / originalSize.Height
        Dim ratio As Double = Math.Min(widthRatio, heightRatio)

        ' If image is already smaller than max dimensions, don't upscale
        If ratio > 1.0 Then ratio = 1.0

        Dim newWidth As Integer = CInt(originalSize.Width * ratio)
        Dim newHeight As Integer = CInt(originalSize.Height * ratio)

        Return New Size(newWidth, newHeight)
    End Function

    ''' <summary>
    ''' Gets the image codec info for a specific format
    ''' </summary>
    ''' <param name="format">Image format</param>
    ''' <returns>ImageCodecInfo for the format</returns>
    Private Shared Function GetEncoderInfo(format As ImageFormat) As ImageCodecInfo
        Dim codecs() As ImageCodecInfo = ImageCodecInfo.GetImageEncoders()
        For Each codec As ImageCodecInfo In codecs
            If codec.FormatID = format.Guid Then
                Return codec
            End If
        Next
        Throw New Exception($"No encoder found for format: {format}")
    End Function

    ''' <summary>
    ''' Validates if a file is a supported image format
    ''' </summary>
    ''' <param name="filePath">Path to the file</param>
    ''' <returns>True if the file is a supported image</returns>
    Public Shared Function IsValidImageFile(filePath As String) As Boolean
        Try
            Dim extension As String = Path.GetExtension(filePath).ToLower()
            Dim validExtensions() As String = {".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff", ".webp"}

            If Not validExtensions.Contains(extension) Then
                Return False
            End If

            ' Try to load the image to verify it's actually an image file
            Using image As Image = Image.FromFile(filePath)
                Return True
            End Using
        Catch
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Gets the size of an image file without loading the entire image
    ''' </summary>
    ''' <param name="filePath">Path to the image file</param>
    ''' <returns>Size of the image</returns>
    Public Shared Function GetImageSize(filePath As String) As Size
        Try
            Using image As Image = Image.FromFile(filePath)
                Return image.Size
            End Using
        Catch ex As Exception
            Throw New Exception($"Failed to get image size: {ex.Message}")
        End Try
    End Function

    ''' <summary>
    ''' Gets the file size in a human-readable format
    ''' </summary>
    ''' <param name="bytes">File size in bytes</param>
    ''' <returns>Formatted file size string</returns>
    Public Shared Function FormatFileSize(bytes As Long) As String
        Dim sizes() As String = {"B", "KB", "MB", "GB"}
        Dim order As Integer = 0
        Dim size As Double = bytes

        While size >= 1024 AndAlso order < sizes.Length - 1
            order += 1
            size /= 1024
        End While

        Return $"{size:F1} {sizes(order)}"
    End Function

    Public Shared Function ComputeHash(filePath As String) As String
        Try
            Using sha As SHA256 = SHA256.Create()
                Using fs As FileStream = File.OpenRead(filePath)
                    Dim hashBytes As Byte() = sha.ComputeHash(fs)
                    Dim sb As New System.Text.StringBuilder()
                    For Each b As Byte In hashBytes
                        sb.Append(b.ToString("x2"))
                    Next
                    Return sb.ToString()
                End Using
            End Using
        Catch ex As Exception
            Return Guid.NewGuid().ToString("N")
        End Try
    End Function

    Public Shared Sub CompressImageToFile(sourcePath As String, destPath As String, Optional quality As Integer = 75, Optional maxWidth As Integer = 800, Optional maxHeight As Integer = 600)
        Using originalImage As Image = Image.FromFile(sourcePath)
            Dim newSize As Size = CalculateNewSize(originalImage.Size, maxWidth, maxHeight)
            Using resizedImage As New Bitmap(newSize.Width, newSize.Height)
                Using g As Graphics = Graphics.FromImage(resizedImage)
                    g.CompositingMode = CompositingMode.SourceCopy
                    g.CompositingQuality = CompositingQuality.HighQuality
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic
                    g.SmoothingMode = SmoothingMode.HighQuality
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality
                    g.DrawImage(originalImage, 0, 0, newSize.Width, newSize.Height)
                End Using

                Dim jpegCodec As ImageCodecInfo = GetEncoderInfo(ImageFormat.Jpeg)
                Dim encoderParams As New EncoderParameters(1)
                encoderParams.Param(0) = New EncoderParameter(Encoder.Quality, CLng(quality))

                resizedImage.Save(destPath, jpegCodec, encoderParams)
            End Using
        End Using
    End Sub

    Public Shared Function CompressImage(imageBytes As Byte(), quality As Integer) As Byte()
        Try
            Using originalStream As New MemoryStream(imageBytes)
                Using originalImage As Image = Image.FromStream(originalStream)
                    
                    ' Get JPEG codec
                    Dim jpegCodec As ImageCodecInfo = GetEncoderInfo("image/jpeg")
                    If jpegCodec Is Nothing Then
                        Return imageBytes ' Return original if no JPEG codec found
                    End If
                    
                    ' Set quality parameters
                    Dim encoderParams As New EncoderParameters(1)
                    encoderParams.Param(0) = New EncoderParameter(Encoder.Quality, CLng(quality))
                    
                    ' Compress and return
                    Using compressedStream As New MemoryStream()
                        originalImage.Save(compressedStream, jpegCodec, encoderParams)
                        Return compressedStream.ToArray()
                    End Using
                End Using
            End Using
        Catch ex As Exception
            ' If compression fails, return original
            Console.WriteLine($"Image compression failed: {ex.Message}")
            Return imageBytes
        End Try
    End Function

    ' Get encoder info for specified MIME type
    Private Shared Function GetEncoderInfo(mimeType As String) As ImageCodecInfo
        Try
            Dim codecs As ImageCodecInfo() = ImageCodecInfo.GetImageEncoders()
            For Each codec In codecs
                If codec.MimeType = mimeType Then
                    Return codec
                End If
            Next
            Return Nothing
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Public Shared Function CompressImage(imageBytes As Byte(), quality As Integer, maxWidth As Integer, maxHeight As Integer) As Byte()
        Try
            Using originalStream As New MemoryStream(imageBytes)
                Using originalImage As Image = Image.FromStream(originalStream)
                    Return CompressImage(originalImage, quality, maxWidth, maxHeight)
                End Using
            End Using
        Catch ex As Exception
            Console.WriteLine($"Image compression failed: {ex.Message}")
            Return imageBytes
        End Try
    End Function

    ' Resize image to maximum dimensions while maintaining aspect ratio
    Public Shared Function ResizeImage(imageBytes As Byte(), maxWidth As Integer, maxHeight As Integer) As Byte()
        Try
            Using originalStream As New MemoryStream(imageBytes)
                Using originalImage As Image = Image.FromStream(originalStream)
                    
                    ' Calculate new dimensions
                    Dim ratio As Double = Math.Min(maxWidth / originalImage.Width, maxHeight / originalImage.Height)
                    Dim newWidth As Integer = CInt(originalImage.Width * ratio)
                    Dim newHeight As Integer = CInt(originalImage.Height * ratio)
                    
                    ' Create resized image
                    Using resizedImage As New Bitmap(newWidth, newHeight)
                        Using graphics As Graphics = Graphics.FromImage(resizedImage)
                            graphics.InterpolationMode = Drawing2D.InterpolationMode.HighQualityBicubic
                            graphics.DrawImage(originalImage, 0, 0, newWidth, newHeight)
                        End Using
                        
                        ' Convert back to bytes
                        Using resultStream As New MemoryStream()
                            resizedImage.Save(resultStream, ImageFormat.Jpeg)
                            Return resultStream.ToArray()
                        End Using
                    End Using
                End Using
            End Using
        Catch ex As Exception
            ' If resize fails, return original
            Console.WriteLine($"Image resize failed: {ex.Message}")
            Return imageBytes
        End Try
    End Function
End Class