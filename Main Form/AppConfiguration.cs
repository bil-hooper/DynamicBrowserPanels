using System;
using System.IO;
using System.Text.Json;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages application configuration settings
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
                    _settings.LastCustomTimerDuration = value;
                    SaveSettings();
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
                    SaveSettings();
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
                    _settings.LastMediaDirectory = value;
                    SaveSettings();
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
        }
    }
}