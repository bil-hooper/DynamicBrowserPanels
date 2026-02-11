using System;
using System.Collections.Generic; 
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

        private static readonly string TemplatesDirectory = Path.Combine(
            StateDirectory,
            "Templates"
        );

        private static readonly string CurrentLayoutPath = Path.Combine(
            StateDirectory,
            "Current Layout.frm"
        );

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
        /// Gets or sets the file path for the loaded session file (non-command-line mode)
        /// </summary>
        public static string LoadedSessionFilePath { get; set; }

        /// <summary>
        /// Gets whether the application is in command-line mode (opened with a specific file)
        /// </summary>
        public static bool IsCommandLineMode => !string.IsNullOrEmpty(SessionFilePath);

        /// <summary>
        /// Gets whether the application is in session mode (a layout file has been loaded)
        /// </summary>
        public static bool IsSessionMode => !string.IsNullOrEmpty(LoadedSessionFilePath);

        /// <summary>
        /// Gets the filename of the current session (without path)
        /// </summary>
        public static string SessionFileName
        {
            get
            {
                if (!string.IsNullOrEmpty(LoadedSessionFilePath))
                    return Path.GetFileName(LoadedSessionFilePath);
                return null;
            }
        }

        /// <summary>
        /// Ends the command-line session and returns to normal mode
        /// </summary>
        public static void EndCommandLineSession()
        {
            SessionFilePath = null;
            LoadedSessionFilePath = null;
        }

        /// <summary>
        /// Ends the session mode (clears loaded file path)
        /// </summary>
        public static void EndSessionMode()
        {
            LoadedSessionFilePath = null;
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
                
                // DO NOT clean up temp files for Current Layout
                // Only clean up when saving templates
            }
        }

        /// <summary>
        /// Saves directly to the loaded session file
        /// </summary>
        public static bool SaveToSessionFile(BrowserState state)
        {
            if (!IsSessionMode)
                return false;

            try
            {
                SaveState(state, LoadedSessionFilePath);
                
                // Clean up unused temp files for this template
                CleanupUnusedTempFilesForTemplate(LoadedSessionFilePath, state);
                
                MessageBox.Show(
                    $"Layout saved successfully to:\n{LoadedSessionFilePath}",
                    "Layout Saved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save layout:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
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
                saveFileDialog.InitialDirectory = TemplatesDirectory;
                
                // Use suggested file name if provided, otherwise use default
                saveFileDialog.FileName = !string.IsNullOrWhiteSpace(suggestedFileName) 
                    ? suggestedFileName 
                    : "My Layout";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SaveState(state, saveFileDialog.FileName);
                    
                    // Clean up unused temp files for this template
                    CleanupUnusedTempFilesForTemplate(saveFileDialog.FileName, state);
                    
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
        /// Cleans up unused temp files for a specific template
        /// </summary>
        private static void CleanupUnusedTempFilesForTemplate(string templatePath, BrowserState state)
        {
            try
            {
                // Collect all active URLs from the state
                var activeUrls = new HashSet<string>();
                CollectActiveUrls(state.RootPanel, activeUrls);
                
                // Clean up temp files not in use
                LocalMediaHelper.CleanupUnusedTempFiles(templatePath, activeUrls);
            }
            catch
            {
                // Silently fail - cleanup is not critical
            }
        }

        /// <summary>
        /// Recursively collects all active URLs from panel state
        /// </summary>
        private static void CollectActiveUrls(PanelState panelState, HashSet<string> activeUrls)
        {
            if (panelState == null)
                return;

            if (panelState.IsSplit)
            {
                // Recurse into child panels
                CollectActiveUrls(panelState.Panel1, activeUrls);
                CollectActiveUrls(panelState.Panel2, activeUrls);
            }
            else
            {
                // Collect URLs from tabs
                if (panelState.TabsState?.TabUrls != null)
                {
                    foreach (var url in panelState.TabsState.TabUrls)
                    {
                        if (!string.IsNullOrEmpty(url) && LocalMediaHelper.IsTempMediaPlayerUrl(url))
                        {
                            activeUrls.Add(url);
                        }
                    }
                }
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
        /// Saves the current state to a specific file path
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

        /// <summary>
        /// Gets the path to the current layout file being used
        /// </summary>
        public static string GetCurrentLayoutPath()
        {
            if (IsCommandLineMode)
            {
                return SessionFilePath;
            }
            else if (IsSessionMode)
            {
                return LoadedSessionFilePath;
            }
            else
            {
                return CurrentLayoutPath;
            }
        }

        /// <summary>
        /// Loads a layout from a user-selected file and sets it as the active session
        /// </summary>
        public static string LoadLayoutFrom(out BrowserState state)
        {
            state = null;
            
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Load Layout";
                openFileDialog.Filter = "Layout Files (*.frm)|*.frm|All Files (*.*)|*.*";
                openFileDialog.DefaultExt = "frm";
                openFileDialog.InitialDirectory = TemplatesDirectory;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    state = LoadState(openFileDialog.FileName);
                    
                    if (state != null)
                    {
                        // Set this as the loaded session file (not command-line mode)
                        LoadedSessionFilePath = openFileDialog.FileName;
                        
                        return openFileDialog.FileName;
                    }
                }
            }
            
            return null;
        }
    }
}
