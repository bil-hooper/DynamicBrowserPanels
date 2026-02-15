using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Dropbox.Api;
using Dropbox.Api.Files;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Instance-based Dropbox manager for individual operations
    /// </summary>
    public class DropboxSyncManager : IDisposable
    {
        private readonly DropboxClient _client;

        public DropboxSyncManager(string accessToken)
        {
            _client = new DropboxClient(accessToken);
        }

        /// <summary>
        /// Pulls (downloads) files from Dropbox to local folder, optionally filtering by pattern
        /// </summary>
        public async Task PullFolderAsync(string dropboxPath, string localPath, string filePattern = "*")
        {
            if (_client == null)
            {
                throw new InvalidOperationException("Not authenticated.");
            }

            // Ensure local directory exists
            Directory.CreateDirectory(localPath);

            try
            {
                // List files in Dropbox folder
                var listResult = await _client.Files.ListFolderAsync(dropboxPath);

                do
                {
                    foreach (var entry in listResult.Entries)
                    {
                        if (entry.IsFile)
                        {
                            var fileMetadata = entry.AsFile;
                            var fileName = Path.GetFileName(entry.Name);

                            // Check if file matches pattern
                            if (!MatchesPattern(fileName, filePattern))
                            {
                                continue;
                            }

                            var localFilePath = Path.Combine(localPath, fileName);

                            // Check if file exists locally and compare timestamps
                            bool shouldDownload = true;
                            if (File.Exists(localFilePath))
                            {
                                var localModified = File.GetLastWriteTimeUtc(localFilePath);
                                var dropboxModified = fileMetadata.ServerModified;

                                // Only download if Dropbox version is newer
                                shouldDownload = dropboxModified > localModified;
                            }

                            if (shouldDownload)
                            {
                                // Download file from Dropbox
                                using (var response = await _client.Files.DownloadAsync(entry.PathDisplay))
                                {
                                    var content = await response.GetContentAsByteArrayAsync();
                                    await File.WriteAllBytesAsync(localFilePath, content);

                                    // Set local file timestamp to match Dropbox
                                    File.SetLastWriteTimeUtc(localFilePath, fileMetadata.ServerModified);
                                }
                            }
                        }
                    }

                    // Handle pagination if there are more files
                    if (listResult.HasMore)
                    {
                        listResult = await _client.Files.ListFolderContinueAsync(listResult.Cursor);
                    }
                    else
                    {
                        break;
                    }
                }
                while (true);
            }
            catch (ApiException<Dropbox.Api.Files.ListFolderError> ex)
            {
                // Handle case where folder doesn't exist on Dropbox
                if (ex.ErrorResponse.IsPath)
                {
                    // Folder doesn't exist on Dropbox yet - this is okay, just return
                    return;
                }
                throw;
            }
        }

        /// <summary>
        /// Syncs a local folder with Dropbox
        /// </summary>
        public async Task SyncFolderAsync(string dropboxPath, string localPath)
        {
            await DropboxSyncManagerStatic.SyncFolderAsync(_client, localPath, dropboxPath);
        }

        /// <summary>
        /// Simple wildcard pattern matching for file filtering
        /// </summary>
        private bool MatchesPattern(string fileName, string pattern)
        {
            if (string.IsNullOrEmpty(pattern) || pattern == "*")
                return true;

            // Handle *.extension pattern
            if (pattern.StartsWith("*."))
            {
                var extension = pattern.Substring(1); // Get ".frm" from "*.frm"
                return fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase);
            }

            // Handle exact match
            return fileName.Equals(pattern, StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _client?.Dispose();
        }
    }

    /// <summary>
    /// Static methods for Dropbox synchronization
    /// </summary>
    public static class DropboxSyncManagerStatic
    {
        // When using "App folder" access, the root is automatically /Apps/[AppName]/
        // We don't need to specify it - Dropbox SDK handles this automatically
        private const string NotesFolder = "/Notes";
        private const string PlaylistsFolder = "/Playlists";
        private const string TemplatesFolder = "/Templates";
        private const string HistoryFolder = "/History";
        private const string ImagesFolder = "/Images";
        private const string UrlPadFolder = "/UrlPad";

        private static readonly string AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels"
        );

        private static readonly string NotesDirectory = Path.Combine(AppDataDirectory, "Notes");
        private static readonly string PlaylistsDirectory = Path.Combine(AppDataDirectory, "Playlists");
        private static readonly string TemplatesDirectory = Path.Combine(AppDataDirectory, "Templates");
        private static readonly string HistoryDirectory = Path.Combine(AppDataDirectory, "History");
        private static readonly string ImagesDirectory = Path.Combine(AppDataDirectory, "Images");
        private static readonly string UrlPadDirectory = Path.Combine(AppDataDirectory, "UrlPad");

        /// <summary>
        /// Sync direction options
        /// </summary>
        public enum SyncDirection
        {
            Both,      // Push then Pull (default)
            PushOnly,  // Upload to Dropbox only
            PullOnly   // Download from Dropbox only
        }

        /// <summary>
        /// Sync mode - determines whether to sync all files or only changed files
        /// </summary>
        public enum SyncMode
        {
            Full,         // Sync all files (manual sync)
            Incremental   // Sync only files changed since last sync (automatic sync)
        }

        /// <summary>
        /// Performs a full synchronization based on current settings
        /// </summary>
        public static async Task<SyncResult> SynchronizeAsync(
            DropboxSyncSettings settings,
            IProgress<string> progress = null,
            SyncDirection direction = SyncDirection.Both,
            SyncMode mode = SyncMode.Full)
        {
            if (!settings.SyncEnabled)
            {
                return new SyncResult { Success = false, Message = "Synchronization is disabled" };
            }

            if (!settings.IsAuthenticated)
            {
                return new SyncResult { Success = false, Message = "Not authenticated with Dropbox" };
            }

            var result = new SyncResult { Success = true };

            try
            {
                using (var dbx = new DropboxClient(settings.AccessToken))
                {
                    // Determine the cutoff date for incremental sync based on direction
                    DateTime? sinceDate = null;
                    
                    if (mode == SyncMode.Incremental)
                    {
                        sinceDate = direction switch
                        {
                            SyncDirection.PushOnly => settings.LastPushTime,
                            SyncDirection.PullOnly => settings.LastPullTime,
                            _ => settings.LastSyncTime // For Both, use the older of the two
                        };
                    }

                    // Sync each enabled folder
                    if (settings.SyncNotes)
                    {
                        string action = direction switch
                        {
                            SyncDirection.PushOnly => "Pushing Notes to Dropbox",
                            SyncDirection.PullOnly => "Pulling Notes from Dropbox",
                            _ => "Synchronizing Notes"
                        };
                        progress?.Report(action + "...");
                        await SyncFolderAsync(dbx, NotesDirectory, NotesFolder, direction, sinceDate);
                        result.NotesCount++;
                    }

                    if (settings.SyncPlaylists)
                    {
                        string action = direction switch
                        {
                            SyncDirection.PushOnly => "Pushing Playlists to Dropbox",
                            SyncDirection.PullOnly => "Pulling Playlists from Dropbox",
                            _ => "Synchronizing Playlists"
                        };
                        progress?.Report(action + "...");
                        await SyncFolderAsync(dbx, PlaylistsDirectory, PlaylistsFolder, direction, sinceDate);
                        result.PlaylistsCount++;
                    }

                    if (settings.SyncTemplates)
                    {
                        string action = direction switch
                        {
                            SyncDirection.PushOnly => "Pushing Templates to Dropbox",
                            SyncDirection.PullOnly => "Pulling Templates from Dropbox",
                            _ => "Synchronizing Templates"
                        };
                        progress?.Report(action + "...");
                        await SyncFolderAsync(dbx, TemplatesDirectory, TemplatesFolder, direction, sinceDate);
                        result.TemplatesCount++;
                    }

                    if (settings.SyncHistory)
                    {
                        string action = direction switch
                        {
                            SyncDirection.PushOnly => "Pushing History to Dropbox",
                            SyncDirection.PullOnly => "Pulling History from Dropbox",
                            _ => "Synchronizing History"
                        };
                        progress?.Report(action + "...");
                        await SyncFolderAsync(dbx, HistoryDirectory, HistoryFolder, direction, sinceDate);
                        result.HistoryCount++;
                    }

                    if (settings.SyncImages)
                    {
                        string action = direction switch
                        {
                            SyncDirection.PushOnly => "Pushing Images to Dropbox",
                            SyncDirection.PullOnly => "Pulling Images from Dropbox",
                            _ => "Synchronizing Images"
                        };
                        progress?.Report(action + "...");
                        await SyncFolderAsync(dbx, ImagesDirectory, ImagesFolder, direction, sinceDate);
                        result.ImagesCount++;
                    }

                    if (settings.SyncUrlPad)
                    {
                        string action = direction switch
                        {
                            SyncDirection.PushOnly => "Pushing UrlPad to Dropbox",
                            SyncDirection.PullOnly => "Pulling UrlPad from Dropbox",
                            _ => "Synchronizing UrlPad"
                        };
                        progress?.Report(action + "...");
                        await SyncFolderAsync(dbx, UrlPadDirectory, UrlPadFolder, direction, sinceDate);
                        result.UrlPadCount++;
                    }

                    result.Message = direction switch
                    {
                        SyncDirection.PushOnly => "Push to Dropbox completed successfully",
                        SyncDirection.PullOnly => "Pull from Dropbox completed successfully",
                        _ => "Synchronization completed successfully"
                    };
                    result.SyncTime = DateTime.Now;
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Synchronization failed: {ex.Message}";
                result.DetailedError = $"Error Type: {ex.GetType().Name}\n" +
                                      $"Message: {ex.Message}\n" +
                                      $"Stack Trace:\n{ex.StackTrace}";
                result.Exception = ex;

                // Also log inner exceptions if they exist
                if (ex.InnerException != null)
                {
                    result.DetailedError += $"\n\nInner Exception: {ex.InnerException.Message}\n" +
                                           $"Inner Stack Trace:\n{ex.InnerException.StackTrace}";
                }
            }

            return result;
        }

        /// <summary>
        /// Syncs a local folder with Dropbox based on sync direction
        /// </summary>
        public static async Task SyncFolderAsync(
            DropboxClient dbx,
            string localFolder,
            string dropboxFolder,
            SyncDirection direction = SyncDirection.Both,
            DateTime? sinceDate = null)
        {
            // Ensure local folder exists
            if (!Directory.Exists(localFolder))
            {
                Directory.CreateDirectory(localFolder);
            }

            // Ensure Dropbox folder exists
            await EnsureFolderExistsAsync(dbx, dropboxFolder);

            // Execute based on direction
            switch (direction)
            {
                case SyncDirection.PushOnly:
                    await PushLocalFilesAsync(dbx, localFolder, dropboxFolder, sinceDate);
                    break;

                case SyncDirection.PullOnly:
                    await PullDropboxFilesAsync(dbx, localFolder, dropboxFolder, sinceDate);
                    break;

                case SyncDirection.Both:
                default:
                    // Phase 1: Push local files to Dropbox
                    await PushLocalFilesAsync(dbx, localFolder, dropboxFolder, sinceDate);
                    // Phase 2: Pull Dropbox files to local
                    await PullDropboxFilesAsync(dbx, localFolder, dropboxFolder, sinceDate);
                    break;
            }
        }

        /// <summary>
        /// Pushes local files to Dropbox
        /// </summary>
        private static async Task PushLocalFilesAsync(
            DropboxClient dbx, 
            string localFolder, 
            string dropboxFolder, 
            DateTime? sinceDate = null)
        {
            if (!Directory.Exists(localFolder))
                return;

            var localFiles = Directory.GetFiles(localFolder, "*.*", SearchOption.AllDirectories);

            foreach (var localFile in localFiles)
            {
                try
                {
                    var localModified = File.GetLastWriteTimeUtc(localFile);

                    // Skip files that haven't been modified since last sync (incremental mode)
                    if (sinceDate.HasValue && localModified <= sinceDate.Value.ToUniversalTime())
                    {
                        continue;
                    }

                    var relativePath = Path.GetRelativePath(localFolder, localFile);
                    var dropboxPath = $"{dropboxFolder}/{relativePath.Replace("\\", "/")}";

                    // Check if file exists in Dropbox and compare timestamps
                    bool shouldUpload = true;

                    try
                    {
                        var metadata = await dbx.Files.GetMetadataAsync(dropboxPath);
                        if (metadata.IsFile)
                        {
                            var fileMetadata = metadata.AsFile;
                            var dropboxModified = fileMetadata.ServerModified.ToUniversalTime();

                            // Only upload if local is newer
                            shouldUpload = localModified > dropboxModified;
                        }
                    }
                    catch (ApiException<GetMetadataError>)
                    {
                        // File doesn't exist in Dropbox, upload it
                        shouldUpload = true;
                    }

                    if (shouldUpload)
                    {
                        using (var fileStream = File.OpenRead(localFile))
                        {
                            await dbx.Files.UploadAsync(
                                dropboxPath,
                                WriteMode.Overwrite.Instance,
                                body: fileStream
                            );
                        }
                    }
                }
                catch
                {
                    // Skip files that fail to upload
                    continue;
                }
            }
        }

        /// <summary>
        /// Pulls Dropbox files to local folder
        /// </summary>
        private static async Task PullDropboxFilesAsync(
            DropboxClient dbx, 
            string localFolder, 
            string dropboxFolder, 
            DateTime? sinceDate = null)
        {
            try
            {
                var listResult = await dbx.Files.ListFolderAsync(dropboxFolder, recursive: true);

                do
                {
                    // Add null check for listResult.Entries
                    if (listResult?.Entries != null)
                    {
                        foreach (var entry in listResult.Entries)
                        {
                            if (entry.IsFile)
                            {
                                var fileEntry = entry.AsFile;
                                var dropboxModified = fileEntry.ServerModified.ToUniversalTime();

                                // Skip files that haven't been modified since last sync (incremental mode)
                                if (sinceDate.HasValue && dropboxModified <= sinceDate.Value.ToUniversalTime())
                                {
                                    continue;
                                }

                                var relativePath = entry.PathDisplay.Substring(dropboxFolder.Length + 1);
                                var localPath = Path.Combine(localFolder, relativePath.Replace("/", "\\"));

                                try
                                {
                                    // Check if we should download
                                    bool shouldDownload = true;

                                    if (File.Exists(localPath))
                                    {
                                        var localModified = File.GetLastWriteTimeUtc(localPath);

                                        // Only download if Dropbox is newer
                                        shouldDownload = dropboxModified > localModified;
                                    }

                                    if (shouldDownload)
                                    {
                                        // Ensure directory exists
                                        // Add null check in PullDropboxFilesAsync for directory path
                                        var directory = Path.GetDirectoryName(localPath);
                                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                                        {
                                            Directory.CreateDirectory(directory);
                                        }

                                        // Download file
                                        using (var response = await dbx.Files.DownloadAsync(entry.PathDisplay))
                                        {
                                            var content = await response.GetContentAsByteArrayAsync();
                                            File.WriteAllBytes(localPath, content);

                                            // Set the last write time to match Dropbox
                                            File.SetLastWriteTimeUtc(localPath, dropboxModified);
                                        }
                                    }
                                }
                                catch
                                {
                                    // Skip files that fail to download
                                    continue;
                                }
                            }
                        }
                    }

                    if (listResult.HasMore)
                    {
                        listResult = await dbx.Files.ListFolderContinueAsync(listResult.Cursor);
                    }
                    else
                    {
                        break;
                    }
                } while (true);
            }
            catch (ApiException<ListFolderError>)
            {
                // Folder doesn't exist, nothing to pull
            }
        }

        /// <summary>
        /// Ensures a folder exists in Dropbox
        /// </summary>
        private static async Task EnsureFolderExistsAsync(DropboxClient dbx, string folderPath)
        {
            try
            {
                await dbx.Files.GetMetadataAsync(folderPath);
            }
            catch (ApiException<GetMetadataError>)
            {
                // Folder doesn't exist, create it
                try
                {
                    await dbx.Files.CreateFolderV2Async(folderPath);
                }
                catch (ApiException<CreateFolderError>)
                {
                    // Folder might have been created between check and create
                }
            }
        }

        /// <summary>
        /// Revokes Dropbox access
        /// </summary>
        public static async Task<bool> RevokeAccessAsync(string accessToken)
        {
            try
            {
                using (var dbx = new DropboxClient(accessToken))
                {
                    await dbx.Auth.TokenRevokeAsync();
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tests the connection to Dropbox
        /// </summary>
        public static async Task<bool> TestConnectionAsync(string accessToken)
        {
            try
            {
                using (var dbx = new DropboxClient(accessToken))
                {
                    var account = await dbx.Users.GetCurrentAccountAsync();
                    return account != null;
                }
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Result of a synchronization operation
    /// </summary>
    public class SyncResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime? SyncTime { get; set; }
        public int NotesCount { get; set; }
        public int PlaylistsCount { get; set; }
        public int TemplatesCount { get; set; }
        public int HistoryCount { get; set; }
        public int ImagesCount { get; set; }
        public int UrlPadCount { get; set; }
        public string DetailedError { get; set; }
        public Exception Exception { get; set; }
    }
}