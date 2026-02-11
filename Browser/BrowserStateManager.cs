using System;
using System.Collections.Generic; 
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using System.Threading.Tasks;

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
        /// Gets or sets the password for the currently loaded password-protected template
        /// </summary>
        private static string CurrentTemplatePassword { get; set; }

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
        /// Saves a password-protected template (establishes NEW password protection)
        /// </summary>
        public static bool SavePasswordProtectedTemplate(BrowserState state, string suggestedFileName = null)
        {
            using (var dialog = new SaveProtectedTemplateDialog(suggestedFileName))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Ensure templates directory exists
                        if (!Directory.Exists(TemplatesDirectory))
                        {
                            Directory.CreateDirectory(TemplatesDirectory);
                        }

                        var filePath = Path.Combine(TemplatesDirectory, dialog.FileName);

                        // Create a copy of the state with encrypted URLs
                        var encryptedState = EncryptStateUrls(state, dialog.Password);

                        // Set the password hash
                        encryptedState.PasswordHash = TemplateEncryption.HashPassword(dialog.Password);

                        // Save the encrypted state
                        var json = JsonSerializer.Serialize(encryptedState, JsonOptions);
                        File.WriteAllText(filePath, json);

                        // Clean up unused temp files
                        CleanupUnusedTempFilesForTemplate(filePath, state);

                        // Delete Current Layout.frm
                        DeleteCurrentLayout();

                        // Set as loaded session
                        LoadedSessionFilePath = filePath;

                        MessageBox.Show(
                            $"Password-protected template saved successfully to:\n{filePath}",
                            "Template Saved",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Failed to save password-protected template:\n{ex.Message}",
                            "Save Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        return false;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Removes password protection from a template
        /// </summary>
        public static bool SaveWithoutPasswordProtection(BrowserState state)
        {
            if (!IsSessionMode)
                return false;

            try
            {
                // Load the existing template to check if it's password protected
                var json = File.ReadAllText(LoadedSessionFilePath);
                var existingState = JsonSerializer.Deserialize<BrowserState>(json, JsonOptions);

                if (!string.IsNullOrEmpty(existingState?.PasswordHash))
                {
                    // Password protected - verify password first
                    using (var passwordDialog = new PasswordVerificationDialog("Enter Password to Remove Protection"))
                    {
                        if (passwordDialog.ShowDialog() == DialogResult.OK)
                        {
                            if (!TemplateEncryption.VerifyPassword(passwordDialog.Password, existingState.PasswordHash))
                            {
                                MessageBox.Show(
                                    "Incorrect password. Protection not removed.",
                                    "Authentication Failed",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                }

                // Save without password hash or encryption
                var unprotectedState = new BrowserState
                {
                    FormWidth = state.FormWidth,
                    FormHeight = state.FormHeight,
                    FormX = state.FormX,
                    FormY = state.FormY,
                    RootPanel = state.RootPanel,
                    PasswordHash = null // Remove password protection
                };

                json = JsonSerializer.Serialize(unprotectedState, JsonOptions);
                File.WriteAllText(LoadedSessionFilePath, json);

                // Clean up unused temp files
                CleanupUnusedTempFilesForTemplate(LoadedSessionFilePath, state);

                // Delete Current Layout.frm
                DeleteCurrentLayout();

                MessageBox.Show(
                    $"Password protection removed. Template saved to:\n{LoadedSessionFilePath}",
                    "Protection Removed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to save template:\n{ex.Message}",
                    "Save Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }
        }


        /// <summary>
        /// Encrypts all URLs in the state
        /// </summary>
        private static BrowserState EncryptStateUrls(BrowserState state, string password)
        {
            var encryptedState = new BrowserState
            {
                FormWidth = state.FormWidth,
                FormHeight = state.FormHeight,
                FormX = state.FormX,
                FormY = state.FormY,
                RootPanel = EncryptPanelUrls(state.RootPanel, password)
            };

            return encryptedState;
        }

        /// <summary>
        /// Recursively encrypts URLs in panel state
        /// </summary>
        private static PanelState EncryptPanelUrls(PanelState panel, string password)
        {
            if (panel == null)
                return null;

            var encryptedPanel = new PanelState
            {
                IsSplit = panel.IsSplit,
                SplitOrientation = panel.SplitOrientation,
                SplitterDistance = panel.SplitterDistance,
                PanelSize = panel.PanelSize
            };

            if (panel.IsSplit)
            {
                encryptedPanel.Panel1 = EncryptPanelUrls(panel.Panel1, password);
                encryptedPanel.Panel2 = EncryptPanelUrls(panel.Panel2, password);
            }
            else
            {
                // Encrypt the URL
                encryptedPanel.Url = TemplateEncryption.EncryptUrl(panel.Url, password);

                // Encrypt URLs in tabs
                if (panel.TabsState != null)
                {
                    encryptedPanel.TabsState = new TabsStateData
                    {
                        SelectedTabIndex = panel.TabsState.SelectedTabIndex,
                        TabCustomNames = panel.TabsState.TabCustomNames,
                        TabPlaylists = panel.TabsState.TabPlaylists,
                        TabUrls = new List<string>()
                    };

                    foreach (var url in panel.TabsState.TabUrls)
                    {
                        encryptedPanel.TabsState.TabUrls.Add(TemplateEncryption.EncryptUrl(url, password));
                    }
                }
            }

            return encryptedPanel;
        }

        /// <summary>
        /// Decrypts all URLs in the state
        /// </summary>
        private static BrowserState DecryptStateUrls(BrowserState state, string password)
        {
            var decryptedState = new BrowserState
            {
                FormWidth = state.FormWidth,
                FormHeight = state.FormHeight,
                FormX = state.FormX,
                FormY = state.FormY,
                PasswordHash = state.PasswordHash,
                RootPanel = DecryptPanelUrls(state.RootPanel, password)
            };

            return decryptedState;
        }

        /// <summary>
        /// Recursively decrypts URLs in panel state
        /// </summary>
        private static PanelState DecryptPanelUrls(PanelState panel, string password)
        {
            if (panel == null)
                return null;

            var decryptedPanel = new PanelState
            {
                IsSplit = panel.IsSplit,
                SplitOrientation = panel.SplitOrientation,
                SplitterDistance = panel.SplitterDistance,
                PanelSize = panel.PanelSize
            };

            if (panel.IsSplit)
            {
                decryptedPanel.Panel1 = DecryptPanelUrls(panel.Panel1, password);
                decryptedPanel.Panel2 = DecryptPanelUrls(panel.Panel2, password);
            }
            else
            {
                // Decrypt the URL
                decryptedPanel.Url = TemplateEncryption.DecryptUrl(panel.Url, password);

                // Decrypt URLs in tabs
                if (panel.TabsState != null)
                {
                    decryptedPanel.TabsState = new TabsStateData
                    {
                        SelectedTabIndex = panel.TabsState.SelectedTabIndex,
                        TabCustomNames = panel.TabsState.TabCustomNames,
                        TabPlaylists = panel.TabsState.TabPlaylists,
                        TabUrls = new List<string>()
                    };

                    foreach (var url in panel.TabsState.TabUrls)
                    {
                        decryptedPanel.TabsState.TabUrls.Add(TemplateEncryption.DecryptUrl(url, password));
                    }
                }
            }

            return decryptedPanel;
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
        /// Saves the current layout asynchronously
        /// </summary>
        public static async Task SaveCurrentLayoutAsync(BrowserState state)
        {
            if (IsCommandLineMode)
            {
                // In command-line mode, do NOT save to the session file
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(CurrentLayoutPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(state, JsonOptions);
                
                // Use async file write
                await File.WriteAllTextAsync(CurrentLayoutPath, json);
            }
            catch
            {
                // Silently fail
            }
        }

        /// <summary>
        /// Saves directly to the loaded session file
        /// Handles both password-protected and regular templates
        /// </summary>
        public static bool SaveToSessionFile(BrowserState state)
        {
            if (!IsSessionMode)
                return false;

            try
            {
                // Load the existing template to check if it's password protected
                var json = File.ReadAllText(LoadedSessionFilePath);
                var existingState = JsonSerializer.Deserialize<BrowserState>(json, JsonOptions);

                if (!string.IsNullOrEmpty(existingState?.PasswordHash))
                {
                    // Password protected - always prompt for password to save with same protection
                    using (var passwordDialog = new PasswordVerificationDialog("Enter Password to Save"))
                    {
                        if (passwordDialog.ShowDialog() == DialogResult.OK)
                        {
                            // Verify password
                            if (!TemplateEncryption.VerifyPassword(passwordDialog.Password, existingState.PasswordHash))
                            {
                                MessageBox.Show(
                                    "Incorrect password. Changes not saved.",
                                    "Authentication Failed",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                                return false;
                            }
                            
                            // Encrypt URLs and save with the same password hash
                            var encryptedState = EncryptStateUrls(state, passwordDialog.Password);
                            encryptedState.PasswordHash = existingState.PasswordHash;
                            
                            json = JsonSerializer.Serialize(encryptedState, JsonOptions);
                            File.WriteAllText(LoadedSessionFilePath, json);
                            
                            // Clean up unused temp files
                            CleanupUnusedTempFilesForTemplate(LoadedSessionFilePath, state);
                            
                            // Delete Current Layout.frm on successful save
                            DeleteCurrentLayout();
                            
                            MessageBox.Show(
                                $"Template saved successfully to:\n{LoadedSessionFilePath}",
                                "Template Saved",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                            
                            return true;
                        }
                        else
                        {
                            // User cancelled
                            return false;
                        }
                    }
                }
                
                // Not password protected - save normally
                SaveState(state, LoadedSessionFilePath);
                
                // Clean up unused temp files for this template
                CleanupUnusedTempFilesForTemplate(LoadedSessionFilePath, state);
                
                // Delete Current Layout.frm on successful save
                DeleteCurrentLayout();
                
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
        /// Loads the current layout asynchronously
        /// </summary>
        public static async Task<BrowserState> LoadCurrentLayoutAsync()
        {
            try
            {
                string filePath = IsCommandLineMode ? SessionFilePath : CurrentLayoutPath;

                if (File.Exists(filePath))
                {
                    // Use async file read
                    string json = await File.ReadAllTextAsync(filePath);
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
            }
            catch
            {
                // If loading fails, return default state
            }

            return CreateDefaultState();
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
        public static BrowserState LoadState(string filePath)
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
        /// Loads a layout from a specific file path and sets it as the active session
        /// Handles password-protected templates
        /// </summary>
        public static BrowserState LoadLayoutFrom(string filePath, out string password)
        {
            password = null;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            var state = LoadStateWithPasswordCheck(filePath, out password);

            if (state != null)
            {
                // Set this as the loaded session file
                LoadedSessionFilePath = filePath;

                // Delete Current Layout.frm on successful load
                DeleteCurrentLayout();
            }

            return state;
        }

        /// <summary>
        /// Loads a layout from a user-selected file and sets it as the active session
        /// Handles password-protected templates
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
                    state = LoadStateWithPasswordCheck(openFileDialog.FileName, out string password);

                    if (state != null)
                    {
                        // Set this as the loaded session file
                        LoadedSessionFilePath = openFileDialog.FileName;

                        // Delete Current Layout.frm on successful load
                        DeleteCurrentLayout();

                        return openFileDialog.FileName;
                    }
                }
            }

            return null;
        }


        /// <summary>
        /// Loads state from a file, prompting for password if needed
        /// </summary>
        private static BrowserState LoadStateWithPasswordCheck(string filePath, out string password)
        {
            password = null;

            try
            {
                if (!File.Exists(filePath))
                {
                    return CreateDefaultState();
                }

                var json = File.ReadAllText(filePath);
                var state = JsonSerializer.Deserialize<BrowserState>(json, JsonOptions);

                if (state == null)
                    return CreateDefaultState();

                // Check if password protected
                if (!string.IsNullOrEmpty(state.PasswordHash))
                {
                    // Prompt for password
                    using (var passwordDialog = new PasswordVerificationDialog("Enter Password to Load Template"))
                    {
                        if (passwordDialog.ShowDialog() == DialogResult.OK)
                        {
                            // Verify password
                            if (TemplateEncryption.VerifyPassword(passwordDialog.Password, state.PasswordHash))
                            {
                                password = passwordDialog.Password;
                                // Decrypt URLs before returning
                                return DecryptStateUrls(state, password);
                            }
                            else
                            {
                                MessageBox.Show(
                                    "Incorrect password. Template not loaded.",
                                    "Authentication Failed",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error);
                                return null;
                            }
                        }
                        else
                        {
                            // User cancelled
                            return null;
                        }
                    }
                }

                // Not password protected
                return state;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to load state from {filePath}:\n{ex.Message}",
                    "Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return null;
            }
        }
    }
}