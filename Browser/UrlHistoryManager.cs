using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages URL history by creating .url shortcut files in the History folder
    /// </summary>
    public static class UrlHistoryManager
    {
        private static readonly string HistoryFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels",
            "History"
        );

        // Cache to avoid re-parsing the same URL
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _filenameCache 
            = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();

        /// <summary>
        /// Creates a URL shortcut file for the given URL (fire-and-forget)
        /// </summary>
        public static void RecordUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            // Fire-and-forget - don't await
            _ = RecordUrlAsync(url);
        }

        /// <summary>
        /// Creates a URL shortcut file for the given URL asynchronously
        /// </summary>
        private static async Task RecordUrlAsync(string url)
        {
            try
            {
                // Skip special URLs
                if (url.StartsWith("about:", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("edge:", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("file:", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("media:", StringComparison.OrdinalIgnoreCase) ||
                    url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // Parse the URL to remove fragments
                Uri uri;
                try
                {
                    uri = new Uri(url);
                }
                catch
                {
                    // Invalid URL, skip
                    return;
                }

                // Build URL without fragment
                string urlWithoutFragment = uri.GetLeftPart(UriPartial.Query);

                // Check cache first
                if (!_filenameCache.TryGetValue(urlWithoutFragment, out string filename))
                {
                    // Generate filename from URL
                    filename = GenerateFilenameFromUrl(urlWithoutFragment);
                    
                    if (string.IsNullOrWhiteSpace(filename))
                        return;
                    
                    // Cache it
                    _filenameCache.TryAdd(urlWithoutFragment, filename);
                }

                // Check if file already exists (fast check before directory creation)
                string filePath = Path.Combine(HistoryFolderPath, filename + ".url");
                if (File.Exists(filePath))
                    return; // Already exists, skip

                // Ensure History folder exists
                await Task.Run(() =>
                {
                    if (!Directory.Exists(HistoryFolderPath))
                    {
                        Directory.CreateDirectory(HistoryFolderPath);
                    }
                });

                // Double-check after directory creation (race condition protection)
                if (!File.Exists(filePath))
                {
                    await CreateUrlShortcutAsync(filePath, urlWithoutFragment);
                }
            }
            catch
            {
                // Silently fail - this is a background operation
            }
        }

        /// <summary>
        /// Generates a safe filename from a URL
        /// </summary>
        private static string GenerateFilenameFromUrl(string url)
        {
            // Remove http:// and https:// prefixes
            string filename = Regex.Replace(url, @"^https?://", "", RegexOptions.IgnoreCase);

            // Remove query string (everything after ?)
            int queryIndex = filename.IndexOf('?');
            if (queryIndex >= 0)
            {
                filename = filename.Substring(0, queryIndex);
            }

            // Replace slashes with underscores
            filename = filename.Replace('/', '_').Replace('\\', '_');

            // Remove or replace other restricted characters
            char[] invalidChars = Path.GetInvalidFileNameChars();
            foreach (char c in invalidChars)
            {
                filename = filename.Replace(c, '_');
            }

            // Remove trailing dots and spaces (Windows restriction)
            filename = filename.TrimEnd('.', ' ', '_');

            // Limit length to avoid path length issues (Windows MAX_PATH)
            // Reserve space for ".url" extension and folder path
            const int maxFilenameLength = 200;
            if (filename.Length > maxFilenameLength)
            {
                filename = filename.Substring(0, maxFilenameLength);
            }

            return filename;
        }

        /// <summary>
        /// Creates a Windows .url shortcut file
        /// </summary>
        private static async Task CreateUrlShortcutAsync(string filePath, string url)
        {
            // Windows .url file format (INI-style)
            var content = new StringBuilder();
            content.AppendLine("[InternetShortcut]");
            content.AppendLine($"URL={url}");
            content.AppendLine($"IconIndex=0");

            await Task.Run(() => File.WriteAllText(filePath, content.ToString(), Encoding.ASCII));
        }

        /// <summary>
        /// Gets the path to the History folder
        /// </summary>
        public static string GetHistoryFolderPath()
        {
            return HistoryFolderPath;
        }

        /// <summary>
        /// Clears all URL history shortcuts
        /// </summary>
        public static void ClearHistory()
        {
            try
            {
                if (Directory.Exists(HistoryFolderPath))
                {
                    Directory.Delete(HistoryFolderPath, true);
                }
            }
            catch
            {
                // Silently fail
            }
        }

        internal static void ClearTodaysHistory()
        {
            try
            {
                if (!Directory.Exists(HistoryFolderPath))
                    return;

                // Get today's date at midnight for comparison
                DateTime today = DateTime.Today;
                DateTime tomorrow = today.AddDays(1);

                // Get all .url files in the history folder
                var urlFiles = Directory.GetFiles(HistoryFolderPath, "*.url");

                foreach (var file in urlFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);

                        // Check if file was created today
                        if (fileInfo.CreationTime >= today && fileInfo.CreationTime < tomorrow)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // Skip files we can't delete
                    }
                }
            }
            catch
            {
                // Silently fail
            }
        }
    }
}