using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    partial class MainBrowserForm
    {
        /// <summary>
        /// Handles form load event - syncs from Dropbox then loads state
        /// </summary>
        private async void MainBrowserForm_Load(object sender, EventArgs e)
        {
            // Show loading overlay
            _loadingOverlay.SetStatus("Initializing...");
            _loadingOverlay.Show(this);

            // Sync from Dropbox FIRST (if enabled) - wait for it to complete
            bool syncCompleted = false;
            try
            {
                _loadingOverlay.SetStatus("Syncing templates from Dropbox...");
                await DropboxAutoSync.SyncOnStartupAsync();
                syncCompleted = true;
            }
            catch
            {
                // Silent fail - continue with local files
            }

            if (syncCompleted)
            {
                _loadingOverlay.SetStatus("Sync complete");
                await Task.Delay(300); // Brief pause to show status
            }

            // Check if we should prompt to restore last template BEFORE loading state
            // Now we know we have the latest version from Dropbox
            bool shouldPromptForTemplate = !BrowserStateManager.IsCommandLineMode && 
                AppConfiguration.PromptRestoreLastTemplate &&
                !string.IsNullOrEmpty(AppConfiguration.LastLoadedTemplatePath) &&
                File.Exists(AppConfiguration.LastLoadedTemplatePath);

            if (shouldPromptForTemplate)
            {
                // Hide loading for prompt
                _loadingOverlay.Hide();

                var templateName = Path.GetFileName(AppConfiguration.LastLoadedTemplatePath);
                
                var result = MessageBox.Show(
                    $"Would you like to restore your last session?\n\n" +
                    $"Template: {templateName}",
                    "Restore Last Session",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2 // Default to No for faster startup
                );

                // Show loading again
                _loadingOverlay.SetStatus("Loading...");
                _loadingOverlay.Show(this);

                if (result == DialogResult.Yes)
                {
                    // Load the last template instead of current layout - use password-aware method
                    var state = BrowserStateManager.LoadLayoutFrom(AppConfiguration.LastLoadedTemplatePath, out string _);
                    if (state != null)
                    {
                        _lastLoadedFileName = templateName;
                        this.Text = $"{templateName} - Dynamic Browser Panels";
                        _timerManager.UpdateOriginalTitle(this.Text);

                        // Restore form size with validation
                        int width = state.FormWidth > 0 ? state.FormWidth : 1184;
                        int height = state.FormHeight > 0 ? state.FormHeight : 761;
                        this.Size = new Size(width, height);

                        // Restore form position if saved
                        if (state.FormX >= 0 && state.FormY >= 0)
                        {
                            this.StartPosition = FormStartPosition.Manual;
                            this.Location = new Point(state.FormX, state.FormY);
                            EnsureWindowVisible();
                        }
                        else
                        {
                            this.StartPosition = FormStartPosition.CenterScreen;
                        }

                        // Restore panel layout with tabs
                        if (state.RootPanel != null)
                        {
                            await RestorePanelStateAsync(rootPanel, state.RootPanel);
                        }
                        else
                        {
                            await CreateDefaultBrowser();
                        }

                        // Hide loading overlay
                        _loadingOverlay.Hide();

                        return; // Exit early, don't load current layout
                    }
                }
            }
            else
            {
                _loadingOverlay.SetStatus("Loading...");
            }

            // Load current layout (only if we didn't load a template above)
            await LoadStateAsync();

            // Hide loading overlay
            _loadingOverlay.Hide();
            
            // Start non-critical background tasks after loading
            //StartBackgroundTasks();
        }

        /// <summary>
        /// Starts non-critical background tasks after form load
        /// </summary>
        private void StartBackgroundTasks()
        {
            // Add any background initialization here
            // For example:
            // - Preload resources
            // - Check for updates
            // - Initialize analytics
            // - Warm up caches
            
            // Currently this can be empty if you don't need background tasks
        }

        /// <summary>
        /// Handles save password-protected template request
        /// </summary>
        private void Browser_SaveProtectedTemplateRequested(object sender, EventArgs e)
        {
            SavePasswordProtectedTemplate();
        }

        /// <summary>
        /// Handles remove template protection request
        /// </summary>
        private void Browser_RemoveTemplateProtectionRequested(object sender, EventArgs e)
        {
            RemoveTemplateProtection();
        }

        /// <summary>
        /// Saves a password-protected template
        /// </summary>
        private void SavePasswordProtectedTemplate()
        {
            var bounds = GetActualWindowBounds();
            var state = CreateBrowserState(bounds);

            if (BrowserStateManager.SavePasswordProtectedTemplate(state, _lastLoadedFileName))
            {
                // Update the window title with the new filename
                _lastLoadedFileName = BrowserStateManager.SessionFileName;
                this.Text = $"{_lastLoadedFileName} - Dynamic Browser Panels";
                _timerManager.UpdateOriginalTitle(this.Text);
                
                // Save as last loaded template
                AppConfiguration.LastLoadedTemplatePath = BrowserStateManager.LoadedSessionFilePath;
            }
        }

        /// <summary>
        /// Removes password protection from the current template
        /// </summary>
        private void RemoveTemplateProtection()
        {
            var bounds = GetActualWindowBounds();
            var state = CreateBrowserState(bounds);
            BrowserStateManager.SaveWithoutPasswordProtection(state);
        }

        /// <summary>
        /// Saves the current state when closing (normal mode) and syncs to Dropbox
        /// </summary>
        private async void MainBrowserForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Hide form immediately for instant close feel
            this.Hide();

            // Show loading overlay during shutdown
            _loadingOverlay.SetStatus("Closing...");
            _loadingOverlay.Show(this);

            // Flush any pending configuration saves immediately
            AppConfiguration.FlushPendingSaves();
            
            // Check if we need to sync to Dropbox
            var syncSettings = AppConfiguration.DropboxSyncSettings;
            bool shouldSync = syncSettings.SyncEnabled && syncSettings.IsAuthenticated;

            if (shouldSync)
            {
                // Cancel the initial close
                e.Cancel = true;

                _loadingOverlay.SetStatus("Syncing to Dropbox...");

                try
                {
                    // Perform shutdown sync (push only, incremental)
                    await DropboxAutoSync.SyncOnShutdownAsync();
                }
                catch
                {
                    // Continue closing even if sync fails
                }

                // Save the layout before closing
                if (!BrowserStateManager.IsCommandLineMode)
                {
                    _loadingOverlay.SetStatus("Saving layout...");
                    SaveCurrentStateSync();
                }

                // Unsubscribe from the FormClosing event to prevent recursion
                FormClosing -= MainBrowserForm_FormClosing;
                
                // Now actually close the form
                Close();
                return;
            }

            // In command-line mode, the file is read-only - don't save on exit
            if (!BrowserStateManager.IsCommandLineMode)
            {
                _loadingOverlay.SetStatus("Saving layout...");
                SaveCurrentStateSync();
            }
        }
    }
}
