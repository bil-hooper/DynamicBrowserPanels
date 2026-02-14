using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages saving and loading URL lists
    /// </summary>
    public static class UrlPadManager
    {
        private static readonly string UrlPadDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels",
            "UrlPad"
        );

        /// <summary>
        /// Gets the path to the UrlPad directory
        /// </summary>
        public static string GetUrlPadDirectoryPath()
        {
            return UrlPadDirectory;
        }

        /// <summary>
        /// Saves a URL list
        /// </summary>
        public static bool SaveUrlList(int urlListNumber, UrlList urlList)
        {
            try
            {
                if (urlList == null)
                    return false;

                // Ensure directory exists
                if (!Directory.Exists(UrlPadDirectory))
                {
                    Directory.CreateDirectory(UrlPadDirectory);
                }

                string filePath = GetUrlListFilePath(urlListNumber);
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(urlList, options);
                File.WriteAllText(filePath, json);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save URL list {urlListNumber}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads a URL list
        /// </summary>
        public static UrlList LoadUrlList(int urlListNumber)
        {
            try
            {
                string filePath = GetUrlListFilePath(urlListNumber);

                if (!File.Exists(filePath))
                    return null;

                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<UrlList>(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load URL list {urlListNumber}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Exports URL list to a file
        /// </summary>
        public static bool ExportUrlList(int urlListNumber, string filePath)
        {
            try
            {
                string sourceFilePath = GetUrlListFilePath(urlListNumber);

                if (!File.Exists(sourceFilePath))
                    return false;

                File.Copy(sourceFilePath, filePath, overwrite: true);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to export URL list {urlListNumber}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deletes a URL list
        /// </summary>
        public static bool DeleteUrlList(int urlListNumber)
        {
            try
            {
                string filePath = GetUrlListFilePath(urlListNumber);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete URL list {urlListNumber}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the file path for a URL list number
        /// </summary>
        private static string GetUrlListFilePath(int urlListNumber)
        {
            return Path.Combine(UrlPadDirectory, $"UrlPad_{urlListNumber:D4}.json");
        }

        /// <summary>
        /// Checks if a URL list exists
        /// </summary>
        public static bool UrlListExists(int urlListNumber)
        {
            string filePath = GetUrlListFilePath(urlListNumber);
            return File.Exists(filePath);
        }
    }

    /// <summary>
    /// Represents a list of URLs with a title
    /// </summary>
    public class UrlList
    {
        public string Title { get; set; } = string.Empty;
        public List<UrlItem> Urls { get; set; } = new List<UrlItem>();
    }

    /// <summary>
    /// Represents a single URL item
    /// </summary>
    public class UrlItem
    {
        public string Url { get; set; } = string.Empty;
        public string DisplayText { get; set; } = string.Empty;
    }
}