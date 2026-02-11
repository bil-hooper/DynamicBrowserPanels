using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Main browser form with dynamic split panel support
    /// </summary>
    public class MainBrowserForm : Form
    {
        private Panel rootPanel;
        private string _lastLoadedFileName; // Store it here at the form level

        public MainBrowserForm()
        {
            InitializeComponent();
            
            // Runtime initialization - not in InitializeComponent()
            if (!DesignMode)
            {
                InitializeRuntime();
            }
        }

        /// <summary>
        /// Initializes runtime-specific behavior
        /// </summary>
        private void InitializeRuntime()
        {
            // Set window title based on mode
            if (BrowserStateManager.IsCommandLineMode)
            {
                this.Text = $"{Path.GetFileName(BrowserStateManager.SessionFilePath)} (Read-Only) - Dynamic Browser Panels";
            }
            
            // Wire up event handlers
            FormClosing += MainBrowserForm_FormClosing;
            
            // Load state and cleanup
            _ = LoadStateAsync(); // Fire and forget, but properly async
            CleanupTempFiles();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainBrowserForm));
            rootPanel = new Panel();
            rootPanel.Dock = DockStyle.Fill;
            SuspendLayout();
            // 
            // rootPanel
            // 
            rootPanel.Location = new Point(0, 0);
            rootPanel.Name = "rootPanel";
            rootPanel.Size = new Size(200, 100);
            rootPanel.TabIndex = 0;
            // 
            // MainBrowserForm
            // 
            ClientSize = new Size(1184, 761);
            Controls.Add(rootPanel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainBrowserForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Dynamic Browser Panels";
            ResumeLayout(false);
        }

        /// <summary>
        /// Loads the saved state and restores the layout
        /// </summary>
        private async Task LoadStateAsync()
        {
            var state = BrowserStateManager.LoadCurrentLayout();

            // Restore form size - always apply with validation
            int width = state.FormWidth > 0 ? state.FormWidth : 1184;
            int height = state.FormHeight > 0 ? state.FormHeight : 761;
            this.Size = new Size(width, height);
            
            // Restore form position if saved
            if (state.FormX >= 0 && state.FormY >= 0)
            {
                this.StartPosition = FormStartPosition.Manual;
                this.Location = new Point(state.FormX, state.FormY);
                
                // Ensure the window is visible on screen
                EnsureWindowVisible();
            }
            else
            {
                // Center if no position saved
                this.StartPosition = FormStartPosition.CenterScreen;
            }

            // Restore panel layout
            if (state.RootPanel != null)
            {
                await RestorePanelStateAsync(rootPanel, state.RootPanel);
            }
            else
            {
                // Create default browser
                await CreateDefaultBrowser();
            }
        }

        /// <summary>
        /// Ensures the window is visible on at least one screen
        /// </summary>
        private void EnsureWindowVisible()
        {
            // Check if the window is on a visible screen
            bool isVisible = false;
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(new Rectangle(this.Location, this.Size)))
                {
                    isVisible = true;
                    break;
                }
            }

            // If not visible, center on primary screen
            if (!isVisible)
            {
                this.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        /// <summary>
        /// Creates the default single browser view
        /// </summary>
        private async Task CreateDefaultBrowser()
        {
            rootPanel.Controls.Clear();

            var browser = new CompactWebView2Control
            {
                Dock = DockStyle.Fill,
                HomeUrl = GlobalConstants.DEFAULT_URL
            };

            browser.SplitRequested += Browser_SplitRequested;
            browser.ResetLayoutRequested += Browser_ResetLayoutRequested;
            browser.SaveLayoutRequested += Browser_SaveLayoutRequested;
            browser.LoadLayoutRequested += Browser_LoadLayoutRequested;

            rootPanel.Controls.Add(browser);
            
            // Ensure at least one tab exists
            await browser.EnsureTabExists();
        }

        /// <summary>
        /// Restores a panel state recursively
        /// </summary>
        private async Task RestorePanelStateAsync(Panel parentPanel, PanelState state)
        {
            parentPanel.Controls.Clear();

            if (!state.IsSplit)
            {
                // Create a browser control
                var browser = new CompactWebView2Control
                {
                    Dock = DockStyle.Fill,
                    HomeUrl = state.Url ?? GlobalConstants.DEFAULT_URL
                };

                browser.SplitRequested += Browser_SplitRequested;
                browser.ResetLayoutRequested += Browser_ResetLayoutRequested;
                browser.SaveLayoutRequested += Browser_SaveLayoutRequested;
                browser.LoadLayoutRequested += Browser_LoadLayoutRequested;

                parentPanel.Controls.Add(browser);

                // Restore tabs state if available
                if (state.TabsState != null && state.TabsState.TabUrls != null && state.TabsState.TabUrls.Count > 0)
                {
                    await browser.RestoreTabsState(state.TabsState);
                }
                // Otherwise navigate to legacy single URL if available, or create default tab
                else
                {
                    // This will create a tab with the URL or home URL
                    await browser.RestoreTabsState(new TabsStateData 
                    { 
                        TabUrls = new List<string>{ state.Url ?? GlobalConstants.DEFAULT_URL },
                        SelectedTabIndex = 0
                    });
                }
            }
            else
            {
                // Create a split container
                var orientation = state.SplitOrientation == "Horizontal" 
                    ? Orientation.Horizontal 
                    : Orientation.Vertical;

                var splitContainer = new SplitContainer
                {
                    Dock = DockStyle.Fill,
                    Orientation = orientation
                };

                // Add the split container to parent first so it has a size
                parentPanel.Controls.Add(splitContainer);

                // Calculate and set splitter distance
                splitContainer.SplitterDistance = CalculateSplitterDistance(
                    splitContainer, 
                    state.SplitterDistance, 
                    state.PanelSize,
                    orientation
                );

                // Restore child panels - AWAIT these calls
                var panel1Task = Task.CompletedTask;
                var panel2Task = Task.CompletedTask;

                if (state.Panel1 != null)
                {
                    var panel1 = new Panel { Dock = DockStyle.Fill };
                    splitContainer.Panel1.Controls.Add(panel1);
                    panel1Task = RestorePanelStateAsync(panel1, state.Panel1);
                }

                if (state.Panel2 != null)
                {
                    var panel2 = new Panel { Dock = DockStyle.Fill };
                    splitContainer.Panel2.Controls.Add(panel2);
                    panel2Task = RestorePanelStateAsync(panel2, state.Panel2);
                }

                // Wait for both panels to complete restoration
                await Task.WhenAll(panel1Task, panel2Task);
            }
        }

        /// <summary>
        /// Calculates the appropriate splitter distance based on saved values
        /// </summary>
        private int CalculateSplitterDistance(SplitContainer splitContainer, int savedDistance, int savedPanelSize, Orientation orientation)
        {
            // Get the current size of the split container
            int currentSize = orientation == Orientation.Vertical 
                ? splitContainer.Width 
                : splitContainer.Height;

            // If we have saved values, try to maintain the same ratio
            if (savedDistance > 0 && savedPanelSize > 0)
            {
                // Calculate the ratio of the saved splitter position
                double ratio = (double)savedDistance / (double)savedPanelSize;
                
                // Apply the same ratio to the current size
                int newDistance = (int)(currentSize * ratio);
                
                // Ensure the distance is within valid bounds
                int minDistance = splitContainer.Panel1MinSize;
                int maxDistance = currentSize - splitContainer.Panel2MinSize;
                
                // Clamp the value to valid range
                newDistance = Math.Max(minDistance, Math.Min(newDistance, maxDistance));
                
                return newDistance;
            }
            
            // Default to middle if no saved values
            return currentSize / 2;
        }

        /// <summary>
        /// Handles split request from a browser control
        /// </summary>
        private async void Browser_SplitRequested(object sender, SplitRequestedEventArgs e)
        {
            var browser = sender as CompactWebView2Control;
            if (browser == null) return;

            // Find the parent panel of this browser
            var parentPanel = browser.Parent as Panel;
            if (parentPanel == null) return;

            // Get the current URL before removing the browser
            var currentUrl = browser.CurrentUrl;

            // Remove the browser from the panel
            parentPanel.Controls.Remove(browser);

            // Create a split container
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = e.Orientation
            };

            // Set splitter distance to middle
            if (e.Orientation == Orientation.Vertical)
            {
                splitContainer.SplitterDistance = parentPanel.Width / 2;
            }
            else
            {
                splitContainer.SplitterDistance = parentPanel.Height / 2;
            }

            // Create panel wrappers
            var panel1 = new Panel { Dock = DockStyle.Fill };
            var panel2 = new Panel { Dock = DockStyle.Fill };

            splitContainer.Panel1.Controls.Add(panel1);
            splitContainer.Panel2.Controls.Add(panel2);

            // Move the existing browser to panel1
            browser.Parent = null; // Detach first
            browser.Dock = DockStyle.Fill;
            
            // Re-wire events (they should still be connected, but ensure)
            browser.SplitRequested -= Browser_SplitRequested;
            browser.ResetLayoutRequested -= Browser_ResetLayoutRequested;
            browser.SaveLayoutRequested -= Browser_SaveLayoutRequested;
            browser.LoadLayoutRequested -= Browser_LoadLayoutRequested;
            
            browser.SplitRequested += Browser_SplitRequested;
            browser.ResetLayoutRequested += Browser_ResetLayoutRequested;
            browser.SaveLayoutRequested += Browser_SaveLayoutRequested;
            browser.LoadLayoutRequested += Browser_LoadLayoutRequested;
            
            panel1.Controls.Add(browser);

            // Create a new browser in panel2
            var newBrowser = new CompactWebView2Control
            {
                Dock = DockStyle.Fill,
                HomeUrl = currentUrl ?? GlobalConstants.DEFAULT_URL
            };

            newBrowser.SplitRequested += Browser_SplitRequested;
            newBrowser.ResetLayoutRequested += Browser_ResetLayoutRequested;
            newBrowser.SaveLayoutRequested += Browser_SaveLayoutRequested;
            newBrowser.LoadLayoutRequested += Browser_LoadLayoutRequested;

            panel2.Controls.Add(newBrowser);
            
            // Ensure the new browser has at least one tab
            await newBrowser.EnsureTabExists();

            // Add the split container to the parent panel
            parentPanel.Controls.Add(splitContainer);
        }

        /// <summary>
        /// Handles reset layout request
        /// </summary>
        private void Browser_ResetLayoutRequested(object sender, EventArgs e)
        {
            string message;
            
            if (BrowserStateManager.IsCommandLineMode)
            {
                message = $"Reset layout and end session?\n\n" +
                          $"Template: {Path.GetFileName(BrowserStateManager.SessionFilePath)}\n\n" +
                          $"This will:\n" +
                          $"• Clear the current layout\n" +
                          $"• Close this template session\n" +
                          $"• Return to normal mode\n\n" +
                          $"Note: Template file will NOT be modified.";
            }
            else
            {
                message = "Are you sure you want to reset the layout?\n" +
                          "This will remove all split panels and reset to a single browser.";
            }
            
            var result = MessageBox.Show(
                message,
                "Reset Layout",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                // If in command-line mode, end the session first
                if (BrowserStateManager.IsCommandLineMode)
                {
                    BrowserStateManager.EndCommandLineSession();
                    
                    // Update window title to show we're back in normal mode
                    this.Text = "Dynamic Browser Panels";
                }
                
                ResetLayout();
            }
        }

        /// <summary>
        /// Handles save layout request
        /// </summary>
        private void Browser_SaveLayoutRequested(object sender, EventArgs e)
        {
            SaveCurrentLayout();
        }

        /// <summary>
        /// Saves the current layout state
        /// </summary>
        private void SaveCurrentLayout()
        {
            // Get the actual window bounds (not maximized bounds)
            Rectangle bounds;
            if (this.WindowState == FormWindowState.Normal)
            {
                bounds = new Rectangle(this.Location, this.Size);
            }
            else
            {
                // Use RestoreBounds when maximized or minimized
                bounds = this.RestoreBounds != Rectangle.Empty 
                    ? this.RestoreBounds 
                    : new Rectangle(this.Location, this.Size);
            }

            var state = new BrowserState
            {
                FormWidth = bounds.Width,
                FormHeight = bounds.Height,
                FormX = bounds.X,
                FormY = bounds.Y,
                RootPanel = CapturePanelState(rootPanel)
            };

            // Pass the last loaded file name to the save dialog
            BrowserStateManager.SaveLayoutAs(state, _lastLoadedFileName);
        }

        /// <summary>
        /// Handles load layout request
        /// </summary>
        private async void Browser_LoadLayoutRequested(object sender, EventArgs e)
        {
            var loadedFilePath = BrowserStateManager.LoadLayoutFrom(out var state);
            
            if (state != null && !string.IsNullOrEmpty(loadedFilePath))
            {
                // Store the loaded file name at the form level
                _lastLoadedFileName = Path.GetFileName(loadedFilePath);

                this.Text = $"{Path.GetFileName(_lastLoadedFileName)} (Read-Only) - Dynamic Browser Panels";

                // Reset the current layout first
                DisposeAllControls(rootPanel);
                rootPanel.Controls.Clear();

                // Restore form size with validation
                int width = state.FormWidth > 0 ? state.FormWidth : 1184;
                int height = state.FormHeight > 0 ? state.FormHeight : 761;
                this.Size = new Size(width, height);
                
                // Restore form position if saved
                if (state.FormX >= 0 && state.FormY >= 0)
                {
                    this.Location = new Point(state.FormX, state.FormY);
                    
                    // Ensure the window is visible on screen
                    EnsureWindowVisible();
                }

                // Restore panel layout - AWAIT this
                if (state.RootPanel != null)
                {
                    await RestorePanelStateAsync(rootPanel, state.RootPanel);
                }
                else
                {
                    await CreateDefaultBrowser();
                }
            }
        }

        /// <summary>
        /// Resets the layout to a single browser
        /// </summary>
        private async void ResetLayout()
        {
            // Clear all controls
            DisposeAllControls(rootPanel);
            rootPanel.Controls.Clear();

            // Delete saved state
            BrowserStateManager.DeleteCurrentLayout();

            // Create default browser
            await CreateDefaultBrowser();
        }

        /// <summary>
        /// Recursively disposes all controls in a panel
        /// </summary>
        private void DisposeAllControls(Control control)
        {
            try
            {
                foreach (Control child in control.Controls)
                {
                    try
                    {
                        DisposeAllControls(child);
                    }
                    catch
                    {
                        // Do nothing.
                    }
                }
                
                // Check if THIS control is a split container (not children)
                if (control is SplitContainer splitContainer)
                {
                    try
                    {
                        DisposeAllControls(splitContainer.Panel1);
                    }
                    catch
                    {
                        // Do nothing.
                    }
                    try
                    {
                        DisposeAllControls(splitContainer.Panel2);
                    }
                    catch
                    {
                        // Do nothing.
                    }
                }
            }
            catch
            {
                // Do nothing.
            }
        }

        /// <summary>
        /// Saves the current state when closing (only in normal mode)
        /// </summary>
        private void MainBrowserForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // In command-line mode, the file is read-only - don't save on exit
            if (BrowserStateManager.IsCommandLineMode)
            {
                // Just close without saving
                return;
            }

            // Normal mode: save the current layout
            // Get the actual window bounds (not maximized bounds)
            Rectangle bounds;
            if (this.WindowState == FormWindowState.Normal)
            {
                bounds = new Rectangle(this.Location, this.Size);
            }
            else
            {
                // Use RestoreBounds when maximized or minimized
                bounds = this.RestoreBounds != Rectangle.Empty 
                    ? this.RestoreBounds 
                    : new Rectangle(this.Location, this.Size);
            }

            var state = new BrowserState
            {
                FormWidth = bounds.Width,
                FormHeight = bounds.Height,
                FormX = bounds.X,
                FormY = bounds.Y,
                RootPanel = CapturePanelState(rootPanel)
            };

            BrowserStateManager.SaveCurrentLayout(state);
        }

        /// <summary>
        /// Captures the state of a panel recursively
        /// </summary>
        private PanelState CapturePanelState(Panel panel)
        {
            var state = new PanelState();

            // Check if the panel contains a browser or a split container
            foreach (Control control in panel.Controls)
            {
                if (control is CompactWebView2Control browser)
                {
                    // Capture tabs state
                    var tabsState = browser.GetTabsState();
                    state.TabsState = new TabsStateData
                    {
                        SelectedTabIndex = tabsState.SelectedTabIndex,
                        TabUrls = tabsState.TabUrls,
                        TabCustomNames = tabsState.TabCustomNames
                    };
                    
                    // Also save current URL for backward compatibility
                    state.Url = browser.CurrentUrl;
                    state.IsSplit = false;
                    return state;
                }
                else if (control is SplitContainer splitContainer)
                {
                    state.IsSplit = true;
                    state.SplitOrientation = splitContainer.Orientation.ToString();
                    state.SplitterDistance = splitContainer.SplitterDistance;
                    state.PanelSize = splitContainer.Orientation == Orientation.Vertical 
                        ? splitContainer.Width 
                        : splitContainer.Height;

                    // Capture child panels
                    var panel1 = FindFirstPanel(splitContainer.Panel1);
                    var panel2 = FindFirstPanel(splitContainer.Panel2);

                    if (panel1 != null)
                        state.Panel1 = CapturePanelState(panel1);
                    
                    if (panel2 != null)
                        state.Panel2 = CapturePanelState(panel2);

                    return state;
                }
            }

            // Default empty state
            return state;
        }

        /// <summary>
        /// Finds the first Panel control in a container
        /// </summary>
        private Panel FindFirstPanel(Control container)
        {
            foreach (Control control in container.Controls)
            {
                if (control is Panel panel)
                    return panel;
            }
            return null;
        }

        /// <summary>
        /// Cleanup temporary HTML files created for media playback and error pages
        /// </summary>
        public static void CleanupTempFiles()
        {
            try
            {
                var tempPath = Path.GetTempPath();
                
                // Clean up media player HTML files
                var mediaFiles = Directory.GetFiles(tempPath, "webview_media_*.html");
                foreach (var file in mediaFiles)
                {
                    try { File.Delete(file); } catch { }
                }
                
                // Clean up media error HTML files
                var errorFiles = Directory.GetFiles(tempPath, "media_error_*.html");
                foreach (var file in errorFiles)
                {
                    try { File.Delete(file); } catch { }
                }
            }
            catch { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeAllControls(rootPanel);
            }
            base.Dispose(disposing);
        }
    }
}
