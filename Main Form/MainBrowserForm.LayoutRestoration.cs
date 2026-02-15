using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    partial class MainBrowserForm
    {
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
                // Center if no saved position
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
        /// Applies a loaded state to the form
        /// </summary>
        private async Task ApplyLoadedState(BrowserState state)
        {
            // Show loading
            _loadingOverlay.SetStatus("Loading...");
            _loadingOverlay.Show(this);

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
                EnsureWindowVisible();
            }

            // Restore panel layout
            if (state.RootPanel != null)
            {
                await RestorePanelStateAsync(rootPanel, state.RootPanel);
            }
            else
            {
                await CreateDefaultBrowser();
            }

            // Hide loading
            _loadingOverlay.Hide();
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

                WireUpBrowserEvents(browser);
                parentPanel.Controls.Add(browser);

                // Restore tabs state
                if (state.TabsState != null && state.TabsState.TabUrls != null && state.TabsState.TabUrls.Count > 0)
                {
                    await browser.RestoreTabsState(state.TabsState);
                }
                else
                {
                    // Create default tab (lightweight operation)
                    await browser.RestoreTabsState(new TabsStateData
                    {
                        TabUrls = new List<string> { state.Url ?? GlobalConstants.DEFAULT_URL },
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

                // Force layout to occur so splitContainer gets its actual size
                splitContainer.PerformLayout();
                Application.DoEvents();

                // Calculate and set splitter distance
                splitContainer.SplitterDistance = CalculateSplitterDistance(
                    splitContainer,
                    state.SplitterDistance,
                    state.PanelSize,
                    orientation
                );

                // Create panels first (synchronous, fast)
                var panel1 = new Panel { Dock = DockStyle.Fill };
                var panel2 = new Panel { Dock = DockStyle.Fill };
                splitContainer.Panel1.Controls.Add(panel1);
                splitContainer.Panel2.Controls.Add(panel2);

                // Restore child panels in parallel
                var tasks = new List<Task>();
                
                if (state.Panel1 != null)
                {
                    tasks.Add(RestorePanelStateAsync(panel1, state.Panel1));
                }

                if (state.Panel2 != null)
                {
                    tasks.Add(RestorePanelStateAsync(panel2, state.Panel2));
                }

                // Wait for both panels to complete restoration
                await Task.WhenAll(tasks);
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

            // Ensure we have a valid size
            if (currentSize <= 0)
            {
                return 100; // Return a safe default
            }

            int preMinDistance = splitContainer.Panel1MinSize;
            int preMaxDistance = currentSize - splitContainer.Panel2MinSize;

            // Additional safety check
            if (preMaxDistance <= preMinDistance)
            {
                return Math.Max(25, currentSize / 2); // Safe fallback
            }

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

            WireUpBrowserEvents(browser);
            rootPanel.Controls.Add(browser);

            // Ensure at least one tab exists
            await browser.EnsureTabExists();
        }

        /// <summary>
        /// Wires up all event handlers for a browser control
        /// </summary>
        private void WireUpBrowserEvents(CompactWebView2Control browser)
        {
            browser.SplitRequested += Browser_SplitRequested;
            browser.ResetLayoutRequested += Browser_ResetLayoutRequested;
            browser.SaveLayoutDirectRequested += Browser_SaveLayoutDirectRequested;
            browser.SaveLayoutAsRequested += Browser_SaveLayoutAsRequested;
            browser.SaveProtectedTemplateRequested += Browser_SaveProtectedTemplateRequested;
            browser.RemoveTemplateProtectionRequested += Browser_RemoveTemplateProtectionRequested;
            browser.LoadLayoutRequested += Browser_LoadLayoutRequested;
            browser.TimerRequested += Browser_TimerRequested;
            browser.TimerStopRequested += Browser_TimerStopRequested;
            browser.TimerAutoRepeatRequested += Browser_TimerAutoRepeatRequested;
        }
    }
}
