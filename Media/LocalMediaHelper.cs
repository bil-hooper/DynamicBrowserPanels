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
        public static string CreateMediaPlayerHtml(string mediaFilePath, bool autoplay = false, bool loop = false, bool hasPlaylist = false)
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
        .playlist-controls {{
            position: fixed;
            bottom: 80px;
            left: 50%;
            transform: translateX(-50%);
            background: rgba(40, 40, 40, 0.95);
            padding: 12px 20px;
            border-radius: 8px;
            display: {(hasPlaylist ? "flex" : "none")};
            gap: 15px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.4);
        }}
        .playlist-btn {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #fff;
            border: none;
            padding: 10px 18px;
            cursor: pointer;
            border-radius: 6px;
            font-size: 14px;
            font-weight: 600;
            transition: all 0.3s ease;
            box-shadow: 0 2px 8px rgba(102, 126, 234, 0.3);
        }}
        .playlist-btn:hover {{
            transform: translateY(-2px);
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.5);
        }}
        .playlist-btn:active {{
            transform: translateY(0);
        }}
        .playlist-btn:disabled {{
            background: #555;
            cursor: not-allowed;
            opacity: 0.5;
        }}
        .autoplay-notice {{
            position: fixed;
            top: 20px;
            left: 50%;
            transform: translateX(-50%);
            background: rgba(255, 152, 0, 0.95);
            color: #000;
            padding: 12px 24px;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 600;
            box-shadow: 0 4px 12px rgba(0,0,0,0.3);
            display: none;
            cursor: pointer;
            z-index: 1000;
            animation: pulse 2s infinite;
        }}
        .autoplay-notice:hover {{
            background: rgba(255, 152, 0, 1);
        }}
        @keyframes pulse {{
            0%, 100% {{ opacity: 1; }}
            50% {{ opacity: 0.8; }}
        }}
    </style>
</head>
<body>
    <div class='autoplay-notice' id='autoplayNotice' onclick='tryPlayAgain()'>
        ⚠️ Click here to start playback
    </div>
    
    <div class='container'>
        <div class='title'>{fileName}</div>
        <{mediaTag} id='mediaPlayer' controls {autoplayAttr} {loopAttr} preload='auto'>
            <source src='{mediaUrl}' type='{mimeType}'>
            <p class='error'>Your browser does not support this media format.</p>
        </{mediaTag}>
        <div class='info' id='info'></div>
    </div>
    
    <div class='playlist-controls'>
        <button class='playlist-btn' id='prevBtn' onclick='previousTrack()'>⏮️ Previous</button>
        <button class='playlist-btn' id='nextBtn' onclick='nextTrack()'>⏭️ Next</button>
    </div>
    
    <script>
        const player = document.getElementById('mediaPlayer');
        const info = document.getElementById('info');
        const autoplayNotice = document.getElementById('autoplayNotice');
        const hasPlaylist = {(hasPlaylist ? "true" : "false")};
        const shouldAutoplay = {(autoplay ? "true" : "false")};
        let autoplayAttempted = false;
        
        // Aggressive autoplay strategy
        function attemptAutoplay() {{
            if (autoplayAttempted) return;
            autoplayAttempted = true;
            
            console.log('Attempting autoplay...');
            
            // Strategy: Try immediate play with promise handling
            const playPromise = player.play();
            
            if (playPromise !== undefined) {{
                playPromise
                    .then(() => {{
                        console.log('✓ Autoplay succeeded');
                        autoplayNotice.style.display = 'none';
                    }})
                    .catch((error) => {{
                        console.warn('✗ Autoplay blocked:', error.name, error.message);
                        // Show user prompt to start playback
                        autoplayNotice.style.display = 'block';
                        
                        // Auto-retry after a short delay (sometimes helps)
                        setTimeout(() => {{
                            player.play().catch(() => {{
                                console.log('Retry also failed, waiting for user interaction');
                            }});
                        }}, 500);
                    }});
            }}
        }}
        
        // Try to play again when user clicks the notice
        function tryPlayAgain() {{
            console.log('User clicked play notice');
            player.play()
                .then(() => {{
                    console.log('✓ Manual play succeeded');
                    autoplayNotice.style.display = 'none';
                }})
                .catch((error) => {{
                    console.error('✗ Manual play failed:', error);
                    alert('Unable to play media: ' + error.message);
                }});
        }}
        
        // Multiple autoplay triggers
        if (shouldAutoplay) {{
            console.log('Autoplay is enabled, setting up triggers...');
            
            // Trigger 1: Immediate attempt when DOM is ready
            if (document.readyState === 'complete') {{
                attemptAutoplay();
            }} else {{
                window.addEventListener('load', () => {{
                    console.log('Window loaded, attempting autoplay');
                    attemptAutoplay();
                }});
            }}
            
            // Trigger 2: On canplay event
            player.addEventListener('canplay', () => {{
                console.log('canplay event fired');
                if (!autoplayAttempted) {{
                    attemptAutoplay();
                }}
            }}, {{ once: true }});
            
            // Trigger 3: On loadeddata event
            player.addEventListener('loadeddata', () => {{
                console.log('loadeddata event fired');
                if (!autoplayAttempted) {{
                    attemptAutoplay();
                }}
            }}, {{ once: true }});
            
            // Trigger 4: Delayed fallback (100ms)
            setTimeout(() => {{
                if (!autoplayAttempted && player.paused) {{
                    console.log('Delayed fallback trigger');
                    attemptAutoplay();
                }}
            }}, 100);
            
            // Trigger 5: Extra delayed fallback (500ms)
            setTimeout(() => {{
                if (player.paused && player.readyState >= 2) {{
                    console.log('Extra delayed fallback trigger');
                    const promise = player.play();
                    if (promise) {{
                        promise.catch(() => {{
                            console.log('Extra fallback also blocked');
                            autoplayNotice.style.display = 'block';
                        }});
                    }}
                }}
            }}, 500);
        }}
        
        // Click anywhere on the video/audio to dismiss notice and play
        player.addEventListener('click', () => {{
            if (player.paused) {{
                player.play();
            }}
            autoplayNotice.style.display = 'none';
        }});
        
        player.addEventListener('loadedmetadata', function() {{
            const duration = Math.floor(player.duration);
            const minutes = Math.floor(duration / 60);
            const seconds = duration % 60;
            info.textContent = `Duration: ${{minutes}}:${{seconds.toString().padStart(2, '0')}}`;
        }});
        
        player.addEventListener('error', function(e) {{
            info.innerHTML = '<span class=""error"">Error loading media file</span>';
            console.error('Media error:', e);
            autoplayNotice.style.display = 'none';
        }});
        
        // Auto-advance to next track when current one ends
        player.addEventListener('ended', function() {{
            console.log('Media ended');
            if (hasPlaylist) {{
                nextTrack();
            }}
        }});
        
        // Hide autoplay notice when playback starts
        player.addEventListener('play', function() {{
            console.log('Playback started');
            autoplayNotice.style.display = 'none';
        }});
        
        // Show notice again if playback is paused unexpectedly
        player.addEventListener('pause', function() {{
            console.log('Playback paused');
        }});
        
        function previousTrack() {{
            if (window.chrome && window.chrome.webview) {{
                window.chrome.webview.postMessage({{ action: 'previous' }});
            }}
        }}
        
        function nextTrack() {{
            if (window.chrome && window.chrome.webview) {{
                window.chrome.webview.postMessage({{ action: 'next' }});
            }}
        }}
        
        // Keyboard shortcuts
        document.addEventListener('keydown', function(e) {{
            if (hasPlaylist) {{
                if (e.ctrlKey && e.key === 'ArrowLeft') {{
                    e.preventDefault();
                    previousTrack();
                }} else if (e.ctrlKey && e.key === 'ArrowRight') {{
                    e.preventDefault();
                    nextTrack();
                }}
            }}
            
            // Space bar to play/pause
            if (e.code === 'Space' && e.target === document.body) {{
                e.preventDefault();
                if (player.paused) {{
                    player.play();
                }} else {{
                    player.pause();
                }}
            }}
        }});
        
        console.log('Media player initialized. Autoplay:', shouldAutoplay, 'Playlist:', hasPlaylist);
    </script>
</body>
</html>";
        }

        /// <summary>
        /// Saves the HTML player to a temporary file and returns the path
        /// If a playlist is provided, creates the full playlist player view
        /// </summary>
        public static string CreateTemporaryPlayerFile(string mediaFilePath, bool autoplay = false, bool loop = false, List<string> playlistFiles = null, int currentIndex = 0)
        {
            // If we have a playlist with multiple files, use the full playlist player
            if (playlistFiles != null && playlistFiles.Count > 1)
            {
                return CreateTemporaryPlaylistPlayerFile(playlistFiles, currentIndex, shuffle: false, repeat: false);
            }
            
            // Otherwise use the simple single-track player
            var html = CreateMediaPlayerHtml(mediaFilePath, autoplay, loop, hasPlaylist: playlistFiles != null && playlistFiles.Count > 0);
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
                    // Check for both single-track player (webview_media_) and playlist player (webview_playlist_)
                    return (path.Contains("webview_media_") || path.Contains("webview_playlist_")) && path.EndsWith(".html");
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
        /// Opens a dialog to select M3U playlist or folder
        /// </summary>
        public static string OpenPlaylistDialog(out bool isFolder)
        {
            isFolder = false;

            var result = MessageBox.Show(
                "Load playlist from:\n\n" +
                "YES - M3U Playlist File\n" +
                "NO - Folder (all media files)\n" +
                "CANCEL - Cancel",
                "Open Playlist",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                // Load M3U file
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select M3U Playlist";
                    openFileDialog.Filter = "M3U Playlist (*.m3u;*.m3u8)|*.m3u;*.m3u8|All Files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        return openFileDialog.FileName;
                    }
                }
            }
            else if (result == DialogResult.No)
            {
                // Load from folder
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select folder containing media files";
                    folderDialog.ShowNewFolderButton = false;

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        isFolder = true;
                        return folderDialog.SelectedPath;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets all supported media files from a folder
        /// </summary>
        public static List<string> GetMediaFilesFromFolder(string folderPath)
        {
            var mediaFiles = new List<string>();

            if (!Directory.Exists(folderPath))
                return mediaFiles;

            try
            {
                var allExtensions = VideoFormats.Concat(AudioFormats).ToArray();
                
                foreach (var ext in allExtensions)
                {
                    mediaFiles.AddRange(Directory.GetFiles(folderPath, $"*{ext}", SearchOption.TopDirectoryOnly));
                }

                // Sort alphabetically
                mediaFiles.Sort(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                // Ignore errors
            }

            return mediaFiles;
        }

        /// <summary>
        /// Loads playlist from M3U file
        /// </summary>
        public static List<string> LoadM3UPlaylist(string m3uFilePath)
        {
            var playlist = new List<string>();

            if (!File.Exists(m3uFilePath))
                return playlist;

            try
            {
                string baseDir = Path.GetDirectoryName(m3uFilePath);
                
                foreach (var line in File.ReadAllLines(m3uFilePath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string mediaPath = line.Trim();
                    
                    // Handle relative paths
                    if (!Path.IsPathRooted(mediaPath))
                    {
                        mediaPath = Path.Combine(baseDir, mediaPath);
                    }

                    // Validate file exists and is supported
                    if (File.Exists(mediaPath) && IsMediaSupported(mediaPath))
                    {
                        playlist.Add(mediaPath);
                    }
                }
            }
            catch
            {
                // Ignore errors
            }

            return playlist;
        }

        /// <summary>
        /// Saves playlist to M3U file
        /// </summary>
        public static bool SaveM3UPlaylist(string m3uFilePath, List<string> mediaFiles, bool useRelativePaths = true)
        {
            try
            {
                for (int i = 0; i < mediaFiles.Count; i++)
                {
                    var file = mediaFiles[i];

                    // If the file doesn't exist, show a warning and remove from playlist
                    if (!File.Exists(file))
                    {
                        MessageBox.Show($"Media file not found: {file}\n\nIt may have been moved or deleted.", "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        mediaFiles.RemoveAt(i);
                        i--; // Adjust index after removal
                    }
                    else if (!IsMediaSupported(file))
                    {
                        // If the file is not supported, suggest removal
                        var result = MessageBox.Show(
                            $"The file '{Path.GetFileName(file)}' is not in a supported format and may not play correctly.\n\n" +
                            "Remove from playlist?",
                            "Unsupported Format",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question
                        );

                        if (result == DialogResult.Yes)
                        {
                            mediaFiles.RemoveAt(i);
                            i--; // Adjust index after removal
                        }
                    }
                }

                if (mediaFiles.Count == 0)
                {
                    MessageBox.Show("The playlist is empty. No valid media files found.", "Empty Playlist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return false;
                }

                var lines = new List<string> { "#EXTM3U" };
                string baseDir = Path.GetDirectoryName(m3uFilePath);

                foreach (var file in mediaFiles)
                {
                    string pathToWrite = file;

                    if (useRelativePaths && !string.IsNullOrEmpty(baseDir))
                    {
                        // Try to make path relative
                        try
                        {
                            pathToWrite = GetRelativePath(baseDir, file);
                        }
                        catch
                        {
                            // If relative path fails, use absolute
                            pathToWrite = file;
                        }
                    }

                    lines.Add(pathToWrite);
                }

                File.WriteAllLines(m3uFilePath, lines);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets relative path from one path to another
        /// </summary>
        private static string GetRelativePath(string fromPath, string toPath)
        {
            if (string.IsNullOrEmpty(fromPath)) return toPath;
            if (string.IsNullOrEmpty(toPath)) return toPath;

            Uri fromUri = new Uri(fromPath.EndsWith("\\") ? fromPath : fromPath + "\\");
            Uri toUri = new Uri(toPath);

            if (fromUri.Scheme != toUri.Scheme) return toPath;

            Uri relativeUri = fromUri.MakeRelativeUri(toUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            return relativePath.Replace('/', '\\');
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

        /// <summary>
        /// Creates an HTML page for playing a full playlist with embedded UI
        /// </summary>
        public static string CreatePlaylistPlayerHtml(List<string> mediaFiles, int currentIndex = 0, bool shuffle = false, bool repeat = false)
        {
            if (mediaFiles == null || mediaFiles.Count == 0)
                return CreateMediaPlayerHtml("", false, false, false);

            // Build JavaScript array of media files
            var filesJson = string.Join(",\n        ", mediaFiles.Select(f => 
                $"{{ path: {System.Text.Json.JsonSerializer.Serialize(f)}, " +
                $"name: {System.Text.Json.JsonSerializer.Serialize(Path.GetFileName(f))}, " +
                $"url: {System.Text.Json.JsonSerializer.Serialize(FilePathToUrl(f))}, " +
                $"type: '{GetMimeType(f)}', " +
                $"isVideo: {(IsVideoFormat(f) ? "true" : "false")} }}"
            ));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <title>Playlist Player</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        body {{
            background: linear-gradient(135deg, #1e1e2e 0%, #2d2d44 100%);
            font-family: 'Segoe UI', Arial, sans-serif;
            color: #fff;
            height: 100vh;
            display: flex;
            overflow: hidden;
        }}
        .player-container {{
            flex: 1;
            display: flex;
            flex-direction: column;
            padding: 20px;
            gap: 15px;
        }}
        .title-bar {{
            font-size: 20px;
            font-weight: 600;
            color: #a8b3ff;
            text-align: center;
            padding: 10px;
            background: rgba(0,0,0,0.3);
            border-radius: 8px;
        }}
        .media-wrapper {{
            flex: 1;
            display: flex;
            justify-content: center;
            align-items: center;
            background: #000;
            border-radius: 12px;
            overflow: hidden;
            box-shadow: 0 8px 32px rgba(0,0,0,0.5);
        }}
        #mediaPlayer {{
            max-width: 100%;
            max-height: 100%;
            width: auto;
            height: auto;
        }}
        audio#mediaPlayer {{
            width: 90%;
        }}
        .controls {{
            display: flex;
            gap: 10px;
            justify-content: center;
            align-items: center;
            padding: 15px;
            background: rgba(0,0,0,0.3);
            border-radius: 8px;
            flex-wrap: wrap;
        }}
        .btn {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: #fff;
            border: none;
            padding: 12px 20px;
            cursor: pointer;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 600;
            transition: all 0.3s ease;
            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
        }}
        .btn:hover {{
            transform: translateY(-2px);
            box-shadow: 0 6px 20px rgba(102, 126, 234, 0.6);
        }}
        .btn:active {{
            transform: translateY(0);
        }}
        .btn:disabled {{
            background: #555;
            cursor: not-allowed;
            opacity: 0.5;
            box-shadow: none;
        }}
        .btn.active {{
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
        }}
        .info {{
            text-align: center;
            font-size: 14px;
            color: #ccc;
        }}
        .playlist-sidebar {{
            width: 350px;
            background: rgba(0,0,0,0.4);
            border-left: 1px solid rgba(255,255,255,0.1);
            display: flex;
            flex-direction: column;
        }}
        .playlist-header {{
            padding: 15px;
            background: rgba(0,0,0,0.5);
            border-bottom: 1px solid rgba(255,255,255,0.1);
            font-weight: 600;
            font-size: 16px;
        }}
        .playlist {{
            flex: 1;
            overflow-y: auto;
            padding: 10px;
        }}
        .playlist-item {{
            padding: 12px;
            margin-bottom: 8px;
            background: rgba(255,255,255,0.05);
            border-radius: 6px;
            cursor: pointer;
            transition: all 0.2s ease;
            border: 2px solid transparent;
        }}
        .playlist-item:hover {{
            background: rgba(255,255,255,0.1);
            transform: translateX(5px);
        }}
        .playlist-item.active {{
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            border-color: #a8b3ff;
            box-shadow: 0 4px 12px rgba(102, 126, 234, 0.5);
        }}
        .playlist-item .name {{
            font-weight: 500;
            margin-bottom: 4px;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
        }}
        .playlist-item .index {{
            font-size: 11px;
            color: #999;
        }}
        .playlist::-webkit-scrollbar {{
            width: 8px;
        }}
        .playlist::-webkit-scrollbar-track {{
            background: rgba(0,0,0,0.2);
        }}
        .playlist::-webkit-scrollbar-thumb {{
            background: rgba(168, 179, 255, 0.3);
            border-radius: 4px;
        }}
        .playlist::-webkit-scrollbar-thumb:hover {{
            background: rgba(168, 179, 255, 0.5);
        }}
    </style>
</head>
<body>
    <div class='player-container'>
        <div class='title-bar' id='titleBar'>Playlist Player</div>
        
        <div class='media-wrapper'>
            <video id='mediaPlayer' controls preload='auto' style='display:none;'></video>
            <audio id='mediaPlayer_audio' controls preload='auto' style='display:none;'></audio>
        </div>
        
        <div class='info' id='info'>Loading...</div>
        
        <div class='controls'>
            <button class='btn' onclick='playPrevious()'>⏮️ Previous</button>
            <button class='btn' onclick='togglePlayPause()' id='playPauseBtn'>▶️ Play</button>
            <button class='btn' onclick='playNext()'>⏭️ Next</button>
            <button class='btn' onclick='toggleShuffle()' id='shuffleBtn'>🔀 Shuffle</button>
            <button class='btn' onclick='toggleRepeat()' id='repeatBtn'>🔁 Repeat</button>
        </div>
    </div>
    
    <div class='playlist-sidebar'>
        <div class='playlist-header'>
            Playlist ({mediaFiles.Count} tracks)
        </div>
        <div class='playlist' id='playlist'></div>
    </div>
    
    <script>
        // Playlist data
        const mediaFiles = [
        {filesJson}
        ];
        
        let currentPlaylistIndex = {currentIndex}; // Index in the play order
        let shuffle = {(shuffle ? "true" : "false")};
        let repeat = {(repeat ? "true" : "false")};
        let isPlaying = false;
        let currentPlayer = null;
        let playOrder = []; // Array of indices defining play order
        
        const videoPlayer = document.getElementById('mediaPlayer');
        const audioPlayer = document.getElementById('mediaPlayer_audio');
        const titleBar = document.getElementById('titleBar');
        const info = document.getElementById('info');
        const playlistEl = document.getElementById('playlist');
        const playPauseBtn = document.getElementById('playPauseBtn');
        const shuffleBtn = document.getElementById('shuffleBtn');
        const repeatBtn = document.getElementById('repeatBtn');
        
        // Initialize
        function init() {{
            generatePlayOrder();
            renderPlaylist();
            updateButtons();
            loadTrack(currentPlaylistIndex, true); // Autoplay immediately
        }}
        
        // Generate play order (normal or shuffled)
        function generatePlayOrder() {{
            playOrder = [];
            for (let i = 0; i < mediaFiles.length; i++) {{
                playOrder.push(i);
            }}
            
            if (shuffle) {{
                // Fisher-Yates shuffle algorithm
                for (let i = playOrder.length - 1; i > 0; i--) {{
                    const j = Math.floor(Math.random() * (i + 1));
                    [playOrder[i], playOrder[j]] = [playOrder[j], playOrder[i]];
                }}
                console.log('Shuffled play order:', playOrder);
            }} else {{
                console.log('Sequential play order');
            }}
        }}
        
        function renderPlaylist() {{
            playlistEl.innerHTML = '';
            const currentFileIndex = playOrder[currentPlaylistIndex];
            
            mediaFiles.forEach((file, index) => {{
                const item = document.createElement('div');
                item.className = 'playlist-item' + (index === currentFileIndex ? ' active' : '');
                item.innerHTML = `
                    <div class='name'>${{file.isVideo ? '🎬' : '🎵'}} ${{file.name}}</div>
                    <div class='index'>Track ${{index + 1}} of ${{mediaFiles.length}}</div>
                `;
                item.onclick = () => playTrackByFileIndex(index);
                playlistEl.appendChild(item);
            }});
            
            // Scroll active item into view
            const activeItem = playlistEl.querySelector('.active');
            if (activeItem) {{
                activeItem.scrollIntoView({{ block: 'center', behavior: 'smooth' }});
            }}
        }}
        
        function loadTrack(playlistIndex, autoplay = false) {{
            if (playlistIndex < 0 || playlistIndex >= playOrder.length) return;
            
            currentPlaylistIndex = playlistIndex;
            const fileIndex = playOrder[currentPlaylistIndex];
            const file = mediaFiles[fileIndex];
            
            console.log(`Loading track: playlist index ${{playlistIndex}}, file index ${{fileIndex}}, name: ${{file.name}}`);
            
            // Hide both players
            videoPlayer.style.display = 'none';
            audioPlayer.style.display = 'none';
            
            // Select appropriate player
            currentPlayer = file.isVideo ? videoPlayer : audioPlayer;
            currentPlayer.style.display = 'block';
            
            // Set source
            currentPlayer.src = file.url;
            currentPlayer.load();
            
            // Update UI
            titleBar.textContent = file.name;
            info.textContent = `Track ${{fileIndex + 1}} of ${{mediaFiles.length}} • Position ${{playlistIndex + 1}} in queue`;
            renderPlaylist();
            
            // Setup events
            currentPlayer.onloadedmetadata = () => {{
                const duration = Math.floor(currentPlayer.duration);
                const minutes = Math.floor(duration / 60);
                const seconds = duration % 60;
                info.textContent = `Track ${{fileIndex + 1}} of ${{mediaFiles.length}} • Duration: ${{minutes}}:${{seconds.toString().padStart(2, '0')}}`;
            }};
            
            currentPlayer.onended = () => {{
                playNext();
            }};
            
            currentPlayer.onplay = () => {{
                isPlaying = true;
                playPauseBtn.innerHTML = '⏸️ Pause';
            }};
            
            currentPlayer.onpause = () => {{
                isPlaying = false;
                playPauseBtn.innerHTML = '▶️ Play';
            }};
            
            currentPlayer.onerror = () => {{
                info.textContent = '❌ Error loading media';
                playNext(); // Auto-skip on error
            }};
            
            // Autoplay if requested
            if (autoplay) {{
                currentPlayer.play().catch(err => {{
                    console.error('Autoplay failed:', err);
                    info.textContent = 'Click Play to start';
                }});
            }}
        }}
        
        function playTrackByFileIndex(fileIndex) {{
            // Find this file's position in the play order
            const playlistIndex = playOrder.indexOf(fileIndex);
            if (playlistIndex !== -1) {{
                loadTrack(playlistIndex, true);
            }}
        }}
        
        function playNext() {{
            let nextIndex = currentPlaylistIndex + 1;
            
            if (nextIndex >= playOrder.length) {{
                if (repeat) {{
                    nextIndex = 0;
                    console.log('Playlist finished, repeating from start');
                }} else {{
                    currentPlayer?.pause();
                    info.textContent = '✓ Playlist finished';
                    console.log('Playlist finished');
                    return;
                }}
            }}
            
            loadTrack(nextIndex, true);
        }}
        
        function playPrevious() {{
            let prevIndex = currentPlaylistIndex - 1;
            
            if (prevIndex < 0) {{
                if (repeat) {{
                    prevIndex = playOrder.length - 1;
                    console.log('At start, jumping to end (repeat enabled)');
                }} else {{
                    prevIndex = 0;
                }}
            }}
            
            loadTrack(prevIndex, true);
        }}
        
        function togglePlayPause() {{
            if (!currentPlayer) {{
                loadTrack(currentPlaylistIndex, true);
                return;
            }}
            
            if (currentPlayer.paused) {{
                currentPlayer.play();
            }} else {{
                currentPlayer.pause();
            }}
        }}
        
        function toggleShuffle() {{
            shuffle = !shuffle;
            console.log('Shuffle toggled:', shuffle);
            
            // Remember current file
            const currentFileIndex = playOrder[currentPlaylistIndex];
            
            // Regenerate play order
            generatePlayOrder();
            
            // Find where the current file is in the new order
            currentPlaylistIndex = playOrder.indexOf(currentFileIndex);
            if (currentPlaylistIndex === -1) {{
                currentPlaylistIndex = 0; // Fallback
            }}
            
            updateButtons();
            renderPlaylist();
        }}
        
        function toggleRepeat() {{
            repeat = !repeat;
            console.log('Repeat toggled:', repeat);
            updateButtons();
        }}
        
        function updateButtons() {{
            shuffleBtn.className = shuffle ? 'btn active' : 'btn';
            repeatBtn.className = repeat ? 'btn active' : 'btn';
        }}
        
        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {{
            if (e.code === 'Space' && e.target === document.body) {{
                e.preventDefault();
                togglePlayPause();
            }} else if (e.key === 'ArrowLeft' && e.ctrlKey) {{
                e.preventDefault();
                playPrevious();
            }} else if (e.key === 'ArrowRight' && e.ctrlKey) {{
                e.preventDefault();
                playNext();
            }}
        }});
        
        // Notify C# of state changes
        function notifyStateChange() {{
            if (window.chrome && window.chrome.webview) {{
                window.chrome.webview.postMessage({{
                    action: 'stateChanged',
                    currentIndex: playOrder[currentPlaylistIndex],
                    shuffle: shuffle,
                    repeat: repeat,
                    isPlaying: isPlaying
                }});
            }}
        }}
        
        // Initialize on load
        init();
    </script>
</body>
</html>";
        }

        /// <summary>
        /// Creates temporary HTML file for playlist player
        /// </summary>
        public static string CreateTemporaryPlaylistPlayerFile(List<string> mediaFiles, int currentIndex = 0, bool shuffle = false, bool repeat = false)
        {
            var html = CreatePlaylistPlayerHtml(mediaFiles, currentIndex, shuffle, repeat);
            var tempPath = Path.Combine(Path.GetTempPath(), $"webview_playlist_{Guid.NewGuid()}.html");
            File.WriteAllText(tempPath, html);
            return tempPath;
        }
    }
}
