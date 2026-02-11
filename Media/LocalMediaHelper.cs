using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Helper class for handling local media files in WebView2
    /// </summary>
    public static class LocalMediaHelper
    {
        // Supported video formats
        private static readonly string[] VideoFormats = 
        { 
            ".mp4", ".webm", ".ogv", ".ogg" 
        };

        // Supported audio formats
        private static readonly string[] AudioFormats = 
        { 
            ".mp3", ".wav", ".aac", ".m4a", ".opus", ".flac", ".ogg" 
        };

        // Common unsupported formats that users might try
        private static readonly string[] UnsupportedFormats = 
        { 
            ".avi", ".wmv", ".mov", ".mkv", ".flv", ".mpg", ".mpeg", ".3gp" 
        };

        /// <summary>
        /// Checks if a file format is supported for playback
        /// </summary>
        public static bool IsMediaSupported(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return VideoFormats.Contains(ext) || AudioFormats.Contains(ext);
        }

        /// <summary>
        /// Checks if a file is a video format
        /// </summary>
        public static bool IsVideoFormat(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return VideoFormats.Contains(ext);
        }

        /// <summary>
        /// Checks if a file is an audio format
        /// </summary>
        public static bool IsAudioFormat(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return AudioFormats.Contains(ext);
        }

        /// <summary>
        /// Converts a local file path to a file:/// URL
        /// </summary>
        public static string FilePathToUrl(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return null;

            // Check if file exists
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Media file not found", filePath);

            // Convert to URI (handles encoding automatically)
            var uri = new Uri(filePath);
            return uri.AbsoluteUri;
        }

        /// <summary>
        /// Creates an HTML page for playing media with custom controls
        /// </summary>
        public static string CreateMediaPlayerHtml(string mediaFilePath, bool autoplay = false, bool loop = false)
        {
            var mediaUrl = FilePathToUrl(mediaFilePath);
            var fileName = Path.GetFileName(mediaFilePath);
            var isVideo = IsVideoFormat(mediaFilePath);
            var mediaTag = isVideo ? "video" : "audio";
            var mimeType = GetMimeType(mediaFilePath);

            var autoplayAttr = autoplay ? "autoplay" : "";
            var loopAttr = loop ? "loop" : "";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>{fileName}</title>
    <!-- MEDIA_SOURCE_PATH: {mediaFilePath} -->
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            background: #000;
            display: flex;
            flex-direction: column;
            justify-content: center;
            align-items: center;
            height: 100vh;
            font-family: 'Segoe UI', Arial, sans-serif;
            color: #fff;
        }}
        .container {{
            max-width: 90%;
            max-height: 90%;
            text-align: center;
        }}
        .title {{
            margin-bottom: 20px;
            font-size: 18px;
            color: #ccc;
        }}
        {mediaTag} {{
            max-width: 100%;
            max-height: 80vh;
            border-radius: 8px;
            box-shadow: 0 4px 20px rgba(0,0,0,0.5);
        }}
        audio {{
            width: 100%;
            max-width: 500px;
        }}
        .info {{
            margin-top: 20px;
            font-size: 14px;
            color: #999;
        }}
        .error {{
            color: #ff6b6b;
            font-size: 16px;
            padding: 20px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='title'>{fileName}</div>
        <{mediaTag} id='mediaPlayer' controls {autoplayAttr} {loopAttr}>
            <source src='{mediaUrl}' type='{mimeType}'>
            <p class='error'>Your browser does not support this media format.</p>
        </{mediaTag}>
        <div class='info' id='info'></div>
    </div>
    
    <script>
        const player = document.getElementById('mediaPlayer');
        const info = document.getElementById('info');
        
        player.addEventListener('loadedmetadata', function() {{
            const duration = Math.floor(player.duration);
            const minutes = Math.floor(duration / 60);
            const seconds = duration % 60;
            info.textContent = `Duration: ${{minutes}}:${{seconds.toString().padStart(2, '0')}}`;
        }});
        
        player.addEventListener('error', function(e) {{
            info.innerHTML = '<span class=""error"">Error loading media file</span>';
            console.error('Media error:', e);
        }});
    </script>
</body>
</html>";
        }

        /// <summary>
        /// Saves the HTML player to a temporary file and returns the path
        /// </summary>
        public static string CreateTemporaryPlayerFile(string mediaFilePath, bool autoplay = false, bool loop = false)
        {
            var html = CreateMediaPlayerHtml(mediaFilePath, autoplay, loop);
            var tempPath = Path.Combine(Path.GetTempPath(), $"webview_media_{Guid.NewGuid()}.html");
            File.WriteAllText(tempPath, html);
            return tempPath;
        }

        /// <summary>
        /// Extracts the original media file path from a temporary HTML player file
        /// </summary>
        public static string ExtractMediaPathFromHtml(string htmlFilePath)
        {
            try
            {
                if (!File.Exists(htmlFilePath))
                    return null;

                var htmlContent = File.ReadAllText(htmlFilePath);
                
                // Look for the comment containing the media source path
                var marker = "<!-- MEDIA_SOURCE_PATH: ";
                var startIndex = htmlContent.IndexOf(marker);
                if (startIndex >= 0)
                {
                    startIndex += marker.Length;
                    var endIndex = htmlContent.IndexOf(" -->", startIndex);
                    if (endIndex > startIndex)
                    {
                        return htmlContent.Substring(startIndex, endIndex - startIndex);
                    }
                }
            }
            catch
            {
                // If extraction fails, return null
            }
            return null;
        }

        /// <summary>
        /// Checks if a URL points to a temporary media player HTML file
        /// </summary>
        public static bool IsTempMediaPlayerUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            try
            {
                var uri = new Uri(url);
                if (uri.Scheme == "file")
                {
                    var path = uri.LocalPath;
                    return path.Contains("webview_media_") && path.EndsWith(".html");
                }
            }
            catch
            {
                // Invalid URL
            }
            return false;
        }

        /// <summary>
        /// Converts a temp player URL back to the original media file path
        /// </summary>
        public static string GetOriginalMediaPath(string playerUrl)
        {
            if (!IsTempMediaPlayerUrl(playerUrl))
                return null;

            try
            {
                var uri = new Uri(playerUrl);
                var htmlPath = uri.LocalPath;
                return ExtractMediaPathFromHtml(htmlPath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the MIME type for a media file
        /// </summary>
        public static string GetMimeType(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            return ext switch
            {
                // Video formats
                ".mp4" => "video/mp4",
                ".webm" => "video/webm",
                ".ogv" => "video/ogg",
                ".ogg" => "video/ogg",
                
                // Audio formats
                ".mp3" => "audio/mpeg",
                ".wav" => "audio/wav",
                ".aac" => "audio/aac",
                ".m4a" => "audio/mp4",
                ".opus" => "audio/opus",
                ".flac" => "audio/flac",
                
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Validates a media file and shows appropriate error messages
        /// </summary>
        public static bool ValidateMediaFile(string filePath, out string errorMessage)
        {
            errorMessage = null;

            // Check if path is provided
            if (string.IsNullOrWhiteSpace(filePath))
            {
                errorMessage = "No file path provided.";
                return false;
            }

            // Check if file exists
            if (!File.Exists(filePath))
            {
                errorMessage = $"File not found:\n{filePath}";
                return false;
            }

            // Check file size (warn if > 2GB)
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > 2L * 1024 * 1024 * 1024)
            {
                errorMessage = $"Warning: File is very large ({FormatFileSize(fileInfo.Length)}).\nPlayback may be slow.";
                // Return true but with warning
            }

            // Check if format is supported
            var ext = Path.GetExtension(filePath).ToLower();
            if (UnsupportedFormats.Contains(ext))
            {
                errorMessage = $"Format '{ext}' is not supported.\n\n" +
                             $"Supported formats:\n" +
                             $"Video: {string.Join(", ", VideoFormats)}\n" +
                             $"Audio: {string.Join(", ", AudioFormats)}\n\n" +
                             $"Please convert your file to MP4 (recommended) or WebM.";
                return false;
            }

            if (!IsMediaSupported(filePath))
            {
                errorMessage = $"Unrecognized format '{ext}'.\n\n" +
                             $"Supported formats:\n" +
                             $"Video: {string.Join(", ", VideoFormats)}\n" +
                             $"Audio: {string.Join(", ", AudioFormats)}";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Opens a file dialog to select a media file
        /// </summary>
        public static string OpenMediaFileDialog()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Media File";
                openFileDialog.Filter = 
                    "All Media Files|*.mp4;*.webm;*.ogv;*.ogg;*.mp3;*.wav;*.aac;*.m4a;*.opus;*.flac|" +
                    "Video Files (*.mp4;*.webm;*.ogv)|*.mp4;*.webm;*.ogv;*.ogg|" +
                    "Audio Files (*.mp3;*.wav;*.aac;*.m4a;*.opus;*.flac)|*.mp3;*.wav;*.aac;*.m4a;*.opus;*.flac;*.ogg|" +
                    "MP4 Videos (*.mp4)|*.mp4|" +
                    "WebM Videos (*.webm)|*.webm|" +
                    "All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
            }
            return null;
        }

        /// <summary>
        /// Formats file size for display
        /// </summary>
        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        /// <summary>
        /// Gets file information for display
        /// </summary>
        public static string GetMediaFileInfo(string filePath)
        {
            if (!File.Exists(filePath))
                return "File not found";

            var fileInfo = new FileInfo(filePath);
            var size = FormatFileSize(fileInfo.Length);
            var ext = Path.GetExtension(filePath).ToLower();
            var type = IsVideoFormat(filePath) ? "Video" : "Audio";
            var mimeType = GetMimeType(filePath);

            return $"File: {Path.GetFileName(filePath)}\n" +
                   $"Type: {type} ({ext})\n" +
                   $"Size: {size}\n" +
                   $"MIME: {mimeType}\n" +
                   $"Path: {filePath}";
        }

        /// <summary>
        /// Suggests conversion if format is unsupported
        /// </summary>
        public static string GetConversionSuggestion(string filePath)
        {
            var ext = Path.GetExtension(filePath).ToLower();
            
            if (!UnsupportedFormats.Contains(ext))
                return null;

            return $"The format '{ext}' is not supported.\n\n" +
                   $"To play this file, please convert it to MP4 using:\n" +
                   $"• HandBrake (free, user-friendly)\n" +
                   $"• VLC Media Player (free, can convert)\n" +
                   $"• FFmpeg (free, command-line)\n" +
                   $"• Online converters (e.g., cloudconvert.com)\n\n" +
                   $"Recommended settings:\n" +
                   $"• Format: MP4\n" +
                   $"• Video Codec: H.264\n" +
                   $"• Audio Codec: AAC";
        }
    }
}
