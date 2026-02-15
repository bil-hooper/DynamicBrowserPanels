using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    partial class MainBrowserForm
    {
        /// <summary>
        /// Saves directly to the loaded session file
        /// </summary>
        private void SaveDirectToSessionFile()
        {
            var bounds = GetActualWindowBounds();
            var state = CreateBrowserState(bounds);
            BrowserStateManager.SaveToSessionFile(state);
        }

        /// <summary>
        /// Saves the current layout state with "Save As" dialog
        /// </summary>
        private void SaveCurrentLayoutAs()
        {
            var bounds = GetActualWindowBounds();
            var state = CreateBrowserState(bounds);

            // Pass the last loaded file name to the save dialog
            BrowserStateManager.SaveLayoutAs(state, _lastLoadedFileName);
        }

        /// <summary>
        /// Synchronously saves the current state (for shutdown)
        /// </summary>
        private void SaveCurrentStateSync()
        {
            var bounds = GetActualWindowBounds();
            var state = CreateBrowserState(bounds);
            BrowserStateManager.SaveCurrentLayout(state);
        }

        /// <summary>
        /// Gets the actual window bounds (not maximized bounds)
        /// </summary>
        private Rectangle GetActualWindowBounds()
        {
            if (this.WindowState == FormWindowState.Normal)
            {
                return new Rectangle(this.Location, this.Size);
            }
            else
            {
                // Use RestoreBounds when maximized or minimized
                return this.RestoreBounds != Rectangle.Empty
                    ? this.RestoreBounds
                    : new Rectangle(this.Location, this.Size);
            }
        }

        /// <summary>
        /// Creates a BrowserState from window bounds and panel state
        /// </summary>
        private BrowserState CreateBrowserState(Rectangle bounds)
        {
            return new BrowserState
            {
                FormWidth = bounds.Width,
                FormHeight = bounds.Height,
                FormX = bounds.X,
                FormY = bounds.Y,
                RootPanel = CapturePanelState(rootPanel)
            };
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
                        TabCustomNames = tabsState.TabCustomNames,
                        TabPlaylists = tabsState.TabPlaylists
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
    }
}
