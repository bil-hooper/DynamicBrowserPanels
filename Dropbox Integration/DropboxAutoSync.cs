using System;
using System.Threading.Tasks;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Handles automatic Dropbox synchronization at startup and shutdown
    /// </summary>
    public static class DropboxAutoSync
    {
        /// <summary>
        /// Performs a sync at application startup (pull remote changes)
        /// </summary>
        public static async Task<SyncResult> SyncOnStartupAsync()
        {
            var settings = AppConfiguration.DropboxSyncSettings;

            if (!settings.SyncEnabled || !settings.IsAuthenticated)
            {
                return new SyncResult { Success = false, Message = "Sync disabled or not authenticated" };
            }

            try
            {
                // Use incremental sync - only pull files changed since last sync
                var result = await DropboxSyncManager.SynchronizeAsync(
                    settings,
                    direction: DropboxSyncManager.SyncDirection.Both,
                    mode: DropboxSyncManager.SyncMode.Incremental
                );
                
                if (result.Success)
                {
                    settings.LastSyncTime = result.SyncTime;
                    AppConfiguration.DropboxSyncSettings = settings;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new SyncResult
                {
                    Success = false,
                    Message = $"Startup sync failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Performs a sync before application shutdown (push local changes)
        /// </summary>
        public static async Task<SyncResult> SyncOnShutdownAsync()
        {
            var settings = AppConfiguration.DropboxSyncSettings;

            if (!settings.SyncEnabled || !settings.IsAuthenticated)
            {
                return new SyncResult { Success = false, Message = "Sync disabled or not authenticated" };
            }

            try
            {
                // Use incremental sync - only push files changed since last sync
                var result = await DropboxSyncManager.SynchronizeAsync(
                    settings,
                    direction: DropboxSyncManager.SyncDirection.Both,
                    mode: DropboxSyncManager.SyncMode.Incremental
                );
                
                if (result.Success)
                {
                    settings.LastSyncTime = result.SyncTime;
                    AppConfiguration.DropboxSyncSettings = settings;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new SyncResult
                {
                    Success = false,
                    Message = $"Shutdown sync failed: {ex.Message}"
                };
            }
        }
    }
}