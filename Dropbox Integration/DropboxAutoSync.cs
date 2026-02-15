using System;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Handles automatic Dropbox synchronization on startup and shutdown
    /// </summary>
    public static class DropboxAutoSync
    {
        /// <summary>
        /// Syncs on startup - prioritizes .frm template files for immediate use
        /// </summary>
        public static async Task SyncOnStartupAsync()
        {
            var settings = AppConfiguration.DropboxSyncSettings;

            // Only sync if enabled, authenticated, AND SyncOnStartup is true
            if (!settings.SyncEnabled || !settings.IsAuthenticated || !settings.SyncOnStartup)
            {
                return;
            }

            try
            {
                using (var manager = new DropboxSyncManager(settings.AccessToken))
                {
                    // PHASE 1: Sync .frm template files FIRST (high priority)
                    if (settings.SyncTemplates)
                    {
                        var templatesDir = Path.Combine(
                            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                            "DynamicBrowserPanels",
                            "Templates"
                        );

                        // Pull templates first (download any updates from Dropbox)
                        await manager.PullFolderAsync("/Templates", templatesDir, "*.frm");
                    }

                    // PHASE 2: Sync other files in background (lower priority)
                    var baseDir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "DynamicBrowserPanels"
                    );

                    var tasks = new List<Task>();

                    // Sync notes
                    if (settings.SyncNotes)
                    {
                        var notesDir = Path.Combine(baseDir, "Notes");
                        tasks.Add(manager.SyncFolderAsync("/Notes", notesDir));
                    }

                    // Sync playlists
                    if (settings.SyncPlaylists)
                    {
                        var playlistsDir = Path.Combine(baseDir, "Playlists");
                        tasks.Add(manager.SyncFolderAsync("/Playlists", playlistsDir));
                    }

                    // Sync history
                    if (settings.SyncHistory)
                    {
                        var historyDir = Path.Combine(baseDir, "History");
                        tasks.Add(manager.SyncFolderAsync("/History", historyDir));
                    }

                    // Sync images
                    if (settings.SyncImages)
                    {
                        var imagesDir = Path.Combine(baseDir, "Images");
                        tasks.Add(manager.SyncFolderAsync("/Images", imagesDir));
                    }

                    // Sync URL pads
                    if (settings.SyncUrlPad)
                    {
                        var urlPadDir = Path.Combine(baseDir, "UrlPad");
                        tasks.Add(manager.SyncFolderAsync("/UrlPad", urlPadDir));
                    }

                    // Wait for all background syncs to complete
                    await Task.WhenAll(tasks);

                    // Update last sync time
                    settings.LastSyncTime = DateTime.UtcNow;
                    settings.LastPullTime = DateTime.UtcNow;
                    AppConfiguration.DropboxSyncSettings = settings;
                }
            }
            catch
            {
                // Silent fail on startup
            }
        }

        /// <summary>
        /// Performs sync on application shutdown (Push only, Incremental)
        /// </summary>
        public static async Task SyncOnShutdownAsync()
        {
            var settings = AppConfiguration.DropboxSyncSettings;

            // Only sync if enabled, authenticated, AND SyncOnShutdown is true
            if (!settings.SyncEnabled || !settings.IsAuthenticated || !settings.SyncOnShutdown)
                return;

            try
            {
                // On shutdown: Push to Dropbox only (incremental)
                // This uploads any changes made during this session
                var result = await DropboxSyncManagerStatic.SynchronizeAsync(
                    settings,
                    progress: null,
                    direction: DropboxSyncManagerStatic.SyncDirection.PushOnly,
                    mode: DropboxSyncManagerStatic.SyncMode.Incremental
                );

                // Add safer nullable access
                if (result?.Success == true && result.SyncTime.HasValue)
                {
                    settings.LastPushTime = result.SyncTime.Value;
                    settings.LastSyncTime = result.SyncTime.Value;
                    AppConfiguration.DropboxSyncSettings = settings;
                }
            }
            catch
            {
                // Silently fail - don't block application shutdown
            }
        }
    }
}