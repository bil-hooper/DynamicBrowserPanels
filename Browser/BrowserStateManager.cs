using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages saving and loading of the browser layout state
    /// </summary>
    public class BrowserStateManager
    {
        private static readonly string StateDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels"
        );

        private static readonly string BackupDirectory = Path.Combine(AppContext.BaseDirectory, "Backups");
        private static readonly string BackupConfigFileName = "backup.dat";

        private static readonly string CurrentLayoutPath = Path.Combine(
            StateDirectory,
            "Current Layout.frm"
        );

        // Cache for the source backup directory path
        private static string _sourceBackupDirectory = null;
        private static bool _sourceBackupDirectoryChecked = false;

        /// <summary>
        /// Shared JSON serializer options for consistent serialization
        /// </summary>
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never, // Include null values
            IncludeFields = false,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Gets or sets the file path for the current session (for command-line mode)
        /// </summary>
        public static string SessionFilePath { get; set; }

        /// <summary>
        /// Gets whether the application is in command-line mode (opened with a specific file)
        /// </summary>
        public static bool IsCommandLineMode => !string.IsNullOrEmpty(SessionFilePath);

        /// <summary>
        /// Gets the source backup directory path from backup.dat if it exists
        /// </summary>
        private static string GetSourceBackupDirectory()
        {
            if (_sourceBackupDirectoryChecked)
            {
                return _sourceBackupDirectory;
            }

            _sourceBackupDirectoryChecked = true;

            try
            {
                string backupConfigPath = Path.Combine(AppContext.BaseDirectory, BackupConfigFileName);
                
                if (File.Exists(backupConfigPath))
                {
                    string sourceBackupPath = File.ReadAllText(backupConfigPath).Trim();
                    
                    // Verify the directory exists
                    if (!string.IsNullOrWhiteSpace(sourceBackupPath) && Directory.Exists(sourceBackupPath))
                    {
                        _sourceBackupDirectory = sourceBackupPath;
                    }
                }
            }
            catch
            {
                // If we can't read the config, just continue without source backup
            }

            return _sourceBackupDirectory;
        }

        /// <summary>
        /// Ends the command-line session and returns to normal mode
        /// </summary>
        public static void EndCommandLineSession()
        {
            SessionFilePath = null;
        }

        /// <summary>
        /// Synchronizes files from backup directory to state directory on startup
        /// </summary>
        public static void SynchronizeFromBackup()
        {
            try
            {
                // If backup directory doesn't exist, nothing to sync
                if (!Directory.Exists(BackupDirectory))
                {
                    return;
                }

                // Ensure state directory exists
                if (!Directory.Exists(StateDirectory))
                {
                    Directory.CreateDirectory(StateDirectory);
                }

                // Get all files in backup directory (including subdirectories)
                var backupFiles = Directory.GetFiles(BackupDirectory, "*.*", SearchOption.AllDirectories);

                foreach (var backupFile in backupFiles)
                {
                    try
                    {
                        // Calculate relative path from backup directory
                        var relativePath = Path.GetRelativePath(BackupDirectory, backupFile);

                        // Calculate corresponding path in state directory
                        var stateFile = Path.Combine(StateDirectory, relativePath);

                        // Only copy if file doesn't exist in state directory
                        if (!File.Exists(stateFile))
                        {
                            // Ensure target directory exists
                            var targetDirectory = Path.GetDirectoryName(stateFile);
                            if (!Directory.Exists(targetDirectory))
                            {
                                Directory.CreateDirectory(targetDirectory);
                            }

                            // Copy file from backup to state directory
                            File.Copy(backupFile, stateFile, false);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to synchronize from backup:\n{ex.Message}",
                    "Synchronization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        /// <summary>
        /// Saves the current state (only in normal mode, not in command-line mode)
        /// In command-line mode, the session file is treated as read-only
        /// </summary>
        public static void SaveCurrentLayout(BrowserState state)
        {
            if (IsCommandLineMode)
            {
                // In command-line mode, do NOT save to the session file
                // The file acts as a read-only template
                return;
            }
            else
            {
                // Normal mode, save to Current Layout.frm
                SaveState(state, CurrentLayoutPath);
            }
        }

        /// <summary>
        /// Loads the current layout from the session file
        /// </summary>
        public static BrowserState LoadCurrentLayout()
        {
            if (IsCommandLineMode)
            {
                // In command-line mode, load from the specified file
                return LoadState(SessionFilePath);
            }
            else
            {
                // Normal mode, load from Current Layout.frm
                return LoadState(CurrentLayoutPath);
            }
        }

        /// <summary>
        /// Saves the current state to a named file chosen by the user
        /// </summary>
        public static bool SaveLayoutAs(BrowserState state, string suggestedFileName = null)
        {
            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Title = "Save Layout As";
                saveFileDialog.Filter = "Layout Files (*.frm)|*.frm|All Files (*.*)|*.*";
                saveFileDialog.DefaultExt = "frm";
                saveFileDialog.InitialDirectory = StateDirectory;
                
                // Use suggested file name if provided, otherwise use default
                saveFileDialog.FileName = !string.IsNullOrWhiteSpace(suggestedFileName) 
                    ? suggestedFileName 
                    : "My Layout";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SaveState(state, saveFileDialog.FileName);
                    MessageBox.Show(
                        $"Layout saved successfully to:\n{saveFileDialog.FileName}",
                        "Layout Saved",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Loads a layout from a file chosen by the user and returns the file path
        /// </summary>
        public static string LoadLayoutFrom(out BrowserState state)
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Load Layout";
                openFileDialog.Filter = "Layout Files (*.frm)|*.frm|All Files (*.*)|*.*";
                openFileDialog.DefaultExt = "frm";
                openFileDialog.InitialDirectory = StateDirectory;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        state = LoadState(openFileDialog.FileName);
                        MessageBox.Show(
                            $"Layout loaded successfully from:\n{openFileDialog.FileName}",
                            "Layout Loaded",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        return openFileDialog.FileName;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to load layout:\n{ex.Message}",
                            "Load Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        state = null;
                        return null;
                    }
                }
            }
            
            state = null;
            return null;
        }

        /// <summary>
        /// Saves the current state to a specific file path and creates a backup copy
        /// </summary>
        private static void SaveState(BrowserState state, string filePath)
        {
            try
            {
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(state, JsonOptions);
 
                File.WriteAllText(filePath, json);

                // Create backup copy if file is in state directory
                if (filePath.StartsWith(StateDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    CreateBackupCopy(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save state to {filePath}:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        /// <summary>
        /// Creates a backup copy of a file from state directory to backup directories
        /// If backup.dat exists, also backs up to the source backup directory
        /// </summary>
        private static void CreateBackupCopy(string stateFilePath)
        {
            try
            {
                // Calculate relative path from state directory
                var relativePath = Path.GetRelativePath(StateDirectory, stateFilePath);
                
                // Backup to local backup directory (in installation folder or executable folder)
                var localBackupFilePath = Path.Combine(BackupDirectory, relativePath);
                CreateSingleBackup(stateFilePath, localBackupFilePath);

                // If backup.dat exists, also backup to source directory
                string sourceBackupDir = GetSourceBackupDirectory();
                if (!string.IsNullOrEmpty(sourceBackupDir))
                {
                    var sourceBackupFilePath = Path.Combine(sourceBackupDir, relativePath);
                    CreateSingleBackup(stateFilePath, sourceBackupFilePath);
                }
            }
            catch (Exception ex)
            {
                // Don't show error to user for backup failures, just log silently
            }
        }

        /// <summary>
        /// Creates a single backup copy to a specific destination
        /// </summary>
        private static void CreateSingleBackup(string sourceFilePath, string destinationFilePath)
        {
            try
            {
                // Ensure backup directory structure exists
                var backupFileDirectory = Path.GetDirectoryName(destinationFilePath);
                if (!Directory.Exists(backupFileDirectory))
                {
                    Directory.CreateDirectory(backupFileDirectory);
                }

                // Copy file to backup location
                File.Copy(sourceFilePath, destinationFilePath, true);
            }
            catch (Exception ex)
            {
                // Don't show error to user for backup failures, just log silently
            }
        }

        /// <summary>
        /// Loads the state from a specific file path
        /// </summary>
        private static BrowserState LoadState(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return CreateDefaultState();
                }

                var json = File.ReadAllText(filePath);
                var state = JsonSerializer.Deserialize<BrowserState>(json, JsonOptions);
                
                // DEBUG: Log what was deserialized
                if (state?.RootPanel?.TabsState?.TabPlaylists != null)
                {
                    for (int i = 0; i < state.RootPanel.TabsState.TabPlaylists.Count; i++)
                    {
                        var pl = state.RootPanel.TabsState.TabPlaylists[i];
                    }
                }
                
                return state ?? CreateDefaultState();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load state from {filePath}:\n{ex.Message}\nUsing default state.",
                    "Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return CreateDefaultState();
            }
        }

        /// <summary>
        /// Deletes the current layout file (only in normal mode)
        /// In command-line mode, this method does nothing
        /// </summary>
        public static void DeleteCurrentLayout()
        {
            try
            {
                // Only delete Current Layout.frm in normal mode
                // In command-line mode, we don't delete the session file
                if (!IsCommandLineMode && File.Exists(CurrentLayoutPath))
                {
                    File.Delete(CurrentLayoutPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to delete current layout:\n{ex.Message}",
                    "Delete Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
            }
        }

        /// <summary>
        /// Creates a default state
        /// </summary>
        private static BrowserState CreateDefaultState()
        {
            return new BrowserState
            {
                FormWidth = 1200,
                FormHeight = 800,
                FormX = -1, // Will be centered
                FormY = -1,
                RootPanel = new PanelState
                {
                    Url = GlobalConstants.DEFAULT_URL
                }
            };
        }
    }
}
