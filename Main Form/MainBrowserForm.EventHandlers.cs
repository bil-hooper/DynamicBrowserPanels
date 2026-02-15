using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    partial class MainBrowserForm
    {
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

            // Re-wire events
            browser.SplitRequested -= Browser_SplitRequested;
            browser.ResetLayoutRequested -= Browser_ResetLayoutRequested;
            browser.SaveLayoutDirectRequested -= Browser_SaveLayoutDirectRequested;
            browser.SaveLayoutAsRequested -= Browser_SaveLayoutAsRequested;
            browser.LoadLayoutRequested -= Browser_LoadLayoutRequested;
            browser.TimerRequested -= Browser_TimerRequested;
            browser.TimerStopRequested -= Browser_TimerStopRequested;
            browser.TimerAutoRepeatRequested -= Browser_TimerAutoRepeatRequested;

            WireUpBrowserEvents(browser);
            panel1.Controls.Add(browser);

            // Create a new browser in panel2
            var newBrowser = new CompactWebView2Control
            {
                Dock = DockStyle.Fill,
                HomeUrl = currentUrl ?? GlobalConstants.DEFAULT_URL
            };

            WireUpBrowserEvents(newBrowser);
            panel2.Controls.Add(newBrowser);

            // Ensure the new browser has at least one tab
            await newBrowser.EnsureTabExists();

            // Add the split container to the parent panel
            parentPanel.Controls.Add(splitContainer);
        }

        /// <summary>
        /// Handles timer request from browser control
        /// /// </summary>
        private void Browser_TimerRequested(object sender, TimeSpan duration)
        {
            _timerManager?.StartTimer(duration);
        }

        /// <summary>
        /// Handles timer stop request from browser control
        /// </summary>
        private void Browser_TimerStopRequested(object sender, EventArgs e)
        {
            _timerManager?.StopTimer();
        }

        /// <summary>
        /// Handles auto-repeat timer toggle request from browser control
        /// </summary>
        private void Browser_TimerAutoRepeatRequested(object sender, bool enabled)
        {
            if (_timerManager != null)
            {
                _timerManager.AutoRepeat = enabled;
            }
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
                          $"Template: {System.IO.Path.GetFileName(BrowserStateManager.SessionFilePath)}\n\n" +
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
                // Clear the last loaded template
                AppConfiguration.LastLoadedTemplatePath = string.Empty;

                // If in command-line mode or session mode, end the session first
                if (BrowserStateManager.IsCommandLineMode)
                {
                    BrowserStateManager.EndCommandLineSession();

                    // Update window title to show we're back in normal mode
                    this.Text = "Dynamic Browser Panels";
                    _timerManager.UpdateOriginalTitle(this.Text);
                }
                else if (BrowserStateManager.IsSessionMode)
                {
                    BrowserStateManager.EndSessionMode();
                    
                    // Update window title
                    this.Text = "Dynamic Browser Panels";
                    _timerManager.UpdateOriginalTitle(this.Text);
                }

                ResetLayout();
            }
        }

        /// <summary>
        /// Handles save layout direct request (saves to loaded session file)
        /// </summary>
        private void Browser_SaveLayoutDirectRequested(object sender, EventArgs e)
        {
            SaveDirectToSessionFile();
        }

        /// <summary>
        /// Handles save layout as request
        /// </summary>
        private void Browser_SaveLayoutAsRequested(object sender, EventArgs e)
        {
            SaveCurrentLayoutAs();
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
                _lastLoadedFileName = System.IO.Path.GetFileName(loadedFilePath);

                // Save this as the last loaded template for future sessions
                AppConfiguration.LastLoadedTemplatePath = loadedFilePath;

                this.Text = $"{System.IO.Path.GetFileName(_lastLoadedFileName)} - Dynamic Browser Panels";
                _timerManager.UpdateOriginalTitle(this.Text);

                // Apply the loaded state
                await ApplyLoadedState(state);
            }
        }
    }
}
