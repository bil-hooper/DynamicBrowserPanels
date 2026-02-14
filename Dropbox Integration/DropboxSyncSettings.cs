using System;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Settings for Dropbox synchronization
    /// </summary>
    public class DropboxSyncSettings
    {
        /// <summary>
        /// Dropbox access token
        /// </summary>
        public string AccessToken { get; set; } = string.Empty;

        /// <summary>
        /// Dropbox refresh token (for OAuth 2.0)
        /// </summary>
        public string RefreshToken { get; set; } = string.Empty;

        /// <summary>
        /// App key for Dropbox OAuth
        /// </summary>
        public string AppKey { get; set; } = string.Empty;

        /// <summary>
        /// App secret for Dropbox OAuth
        /// </summary>
        public string AppSecret { get; set; } = string.Empty;

        /// <summary>
        /// Whether synchronization is enabled
        /// </summary>
        public bool SyncEnabled { get; set; } = false;

        /// <summary>
        /// Whether to automatically sync on application startup
        /// </summary>
        public bool SyncOnStartup { get; set; } = true;

        /// <summary>
        /// Whether to automatically sync on application shutdown
        /// </summary>
        public bool SyncOnShutdown { get; set; } = true;

        /// <summary>
        /// Whether to sync Notes folder
        /// </summary>
        public bool SyncNotes { get; set; } = false;

        /// <summary>
        /// Whether to sync Playlists folder
        /// </summary>
        public bool SyncPlaylists { get; set; } = false;

        /// <summary>
        /// Whether to sync Templates folder
        /// </summary>
        public bool SyncTemplates { get; set; } = false;

        /// <summary>
        /// Whether to sync History folder
        /// </summary>
        public bool SyncHistory { get; set; } = false;

        /// <summary>
        /// Whether to sync Images folder
        /// </summary>
        public bool SyncImages { get; set; } = false;

        /// <summary>
        /// Whether to sync Bookmarks folder
        /// </summary>
        public bool SyncBookmarks { get; set; } = false;

        /// <summary>
        /// Whether to sync UrlPad folder
        /// </summary>
        public bool SyncUrlPad { get; set; } = false;

        /// <summary>
        /// Last successful sync timestamp (for display purposes)
        /// </summary>
        public DateTime? LastSyncTime { get; set; } = null;

        /// <summary>
        /// Last successful push (upload) timestamp
        /// </summary>
        public DateTime? LastPushTime { get; set; } = null;

        /// <summary>
        /// Last successful pull (download) timestamp
        /// </summary>
        public DateTime? LastPullTime { get; set; } = null;

        /// <summary>
        /// Whether the user is authenticated with Dropbox
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);
    }
}