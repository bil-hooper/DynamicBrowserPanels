using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages application configuration settings with batched saves
    /// </summary>
    public static class AppConfiguration
    {
        private const string ConfigFileName = "AppConfig.json";

        private static readonly string ConfigFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels",
            ConfigFileName
        );

        private static AppSettings _settings;
        private static readonly object _lock = new object();
        private static Timer _saveTimer;
        private static bool _isDirty = false;
        private const int SAVE_DELAY_MS = 1000; // Batch saves within 1 second

        /// <summary>
        /// Marks settings as dirty and schedules a save
        /// </summary>
        private static void MarkDirtyAndScheduleSave()
        {
            lock (_lock)
            {
                _isDirty = true;
                
                // Reset timer - this batches multiple changes
                _saveTimer?.Dispose();
                _saveTimer = new Timer(_ => SaveSettingsIfDirty(), null, SAVE_DELAY_MS, Timeout.Infinite);
            }
        }

        /// <summary>
        /// Saves settings only if dirty
        /// </summary>
        private static void SaveSettingsIfDirty()
        {
            lock (_lock)
            {
                if (_isDirty)
                {
                    SaveSettings();
                    _isDirty = false;
                }
            }
        }

        /// <summary>
        /// Forces immediate save (call before app closes)
        /// </summary>
        public static void FlushPendingSaves()
        {
            lock (_lock)
            {
                _saveTimer?.Dispose();
                _saveTimer = null;
                
                if (_isDirty)
                {
                    SaveSettings();
                    _isDirty = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the last custom timer duration
        /// </summary>
        public static TimeSpan LastCustomTimerDuration
        {
            get
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    return _settings.LastCustomTimerDuration;
                }
            }
            set
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    if (_settings.LastCustomTimerDuration != value)
                    {
                        _settings.LastCustomTimerDuration = value;
                        MarkDirtyAndScheduleSave();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the Dropbox synchronization settings
        /// </summary>
        public static DropboxSyncSettings DropboxSyncSettings
        {
            get
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    return _settings.DropboxSyncSettings;
                }
            }
            set
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    _settings.DropboxSyncSettings = value;
                    MarkDirtyAndScheduleSave();
                }
            }
        }

        /// <summary>
        /// Gets or sets the last directory used for loading media files
        /// </summary>
        public static string LastMediaDirectory
        {
            get
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    return _settings.LastMediaDirectory;
                }
            }
            set
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    if (_settings.LastMediaDirectory != value)
                    {
                        _settings.LastMediaDirectory = value;
                        MarkDirtyAndScheduleSave();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the last loaded template path (for session restore on startup)
        /// </summary>
        public static string LastLoadedTemplatePath
        {
            get
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    return _settings.LastLoadedTemplatePath;
                }
            }
            set
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    if (_settings.LastLoadedTemplatePath != value)
                    {
                        _settings.LastLoadedTemplatePath = value;
                        MarkDirtyAndScheduleSave();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to prompt to restore last template on startup
        /// </summary>
        public static bool PromptRestoreLastTemplate
        {
            get
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    return _settings.PromptRestoreLastTemplate;
                }
            }
            set
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    if (_settings.PromptRestoreLastTemplate != value)
                    {
                        _settings.PromptRestoreLastTemplate = value;
                        MarkDirtyAndScheduleSave();
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the privacy lock settings
        /// </summary>
        public static PrivacyLockSettings PrivacyLockSettings
        {
            get
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    return _settings.PrivacyLockSettings;
                }
            }
            set
            {
                lock (_lock)
                {
                    EnsureSettingsLoaded();
                    _settings.PrivacyLockSettings = value;
                    MarkDirtyAndScheduleSave();
                }
            }
        }

        /// <summary>
        /// Ensures settings are loaded from disk
        /// </summary>
        private static void EnsureSettingsLoaded()
        {
            if (_settings == null)
            {
                _settings = LoadSettings();
            }
        }

        /// <summary>
        /// Loads settings from the configuration file
        /// </summary>
        private static AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch
            {
                // If loading fails, return default settings
            }

            return new AppSettings();
        }

        /// <summary>
        /// Saves settings to the configuration file
        /// </summary>
        private static void SaveSettings()
        {
            try
            {
                // Ensure directory exists
                string directory = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch
            {
                // Silently fail if we can't save settings
            }
        }

        /// <summary>
        /// Application settings data class
        /// </summary>
        private class AppSettings
        {
            public TimeSpan LastCustomTimerDuration { get; set; } = TimeSpan.FromMinutes(5);
            public DropboxSyncSettings DropboxSyncSettings { get; set; } = new DropboxSyncSettings();
            public string LastMediaDirectory { get; set; } = string.Empty;
            public string LastLoadedTemplatePath { get; set; } = string.Empty;
            public bool PromptRestoreLastTemplate { get; set; } = true;
            public PrivacyLockSettings PrivacyLockSettings { get; set; } = new PrivacyLockSettings();
        }
    }
}