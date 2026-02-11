using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Compact WebView2 browser control with tab support
    /// </summary>
    public partial class CompactWebView2Control : UserControl
    {
        // UI Components
        private TabControl tabControl;
        private TextBox txtUrl;
        private ContextMenuStrip contextMenu;

        // Context menu items
        private ToolStripMenuItem mnuBack;
        private ToolStripMenuItem mnuForward;
        private ToolStripMenuItem mnuRefresh;
        private ToolStripMenuItem mnuHome;
        private ToolStripMenuItem mnuOpenMedia;
        private ToolStripMenuItem mnuHistory;
        private ToolStripMenuItem mnuPlaylistControls;
        private ToolStripMenuItem mnuOpenNotepad;
        private ToolStripMenuItem mnuTimer;
        private ToolStripMenuItem mnuNewTab;
        private ToolStripMenuItem mnuCloseTab;
        private ToolStripMenuItem mnuRenameTab;
        private ToolStripMenuItem mnuLockTab;
        private ToolStripMenuItem mnuUnlockTab;
        private ToolStripMenuItem mnuMoveTabLeft;
        private ToolStripMenuItem mnuMoveTabRight;
        private ToolStripMenuItem mnuMoveTabToStart;
        private ToolStripMenuItem mnuMoveTabToEnd;
        private ToolStripMenuItem mnuSplitHorizontal;
        private ToolStripMenuItem mnuSplitVertical;
        private ToolStripMenuItem mnuSaveLayoutDirect;
        private ToolStripMenuItem mnuSaveLayoutAs;
        private ToolStripMenuItem mnuLoadLayout;
        private ToolStripMenuItem mnuResetLayout;
        private ToolStripMenuItem mnuManagePasswords;
        private ToolStripMenuItem mnuDropboxSync;
        private ToolStripMenuItem mnuInstall;
        private ToolStripMenuItem mnuUninstall;
        private ToolStripMenuItem mnuSaveProtectedTemplate;
        private ToolStripMenuItem mnuRemoveTemplateProtection;

        // Tab management
        private List<BrowserTab> _browserTabs = new List<BrowserTab>();
        private List<string> _tabCustomNames = new List<string>();
        
        // Tab privacy lock - stores locked tab indices and their overlay panels (in memory only)
        private HashSet<int> _lockedTabs = new HashSet<int>();
        private Dictionary<int, Panel> _lockOverlays = new Dictionary<int, Panel>();
        private Panel _urlBarOverlay; // Overlay for URL bar when locked tab is selected

        // Shared WebView2 environment
        private static CoreWebView2Environment _sharedEnvironment;

        // Properties
        public string HomeUrl { get; set; } = GlobalConstants.DEFAULT_URL;
        public string CurrentUrl => GetCurrentTab()?.CurrentUrl ?? HomeUrl;

        // Events
        public event EventHandler<SplitRequestedEventArgs> SplitRequested;
        public event EventHandler ResetLayoutRequested;
        public event EventHandler SaveLayoutDirectRequested;
        public event EventHandler SaveLayoutAsRequested;
        public event EventHandler LoadLayoutRequested;
        public event EventHandler<TimeSpan> TimerRequested;
        public event EventHandler TimerStopRequested;
        public event EventHandler SaveProtectedTemplateRequested;
        public event EventHandler RemoveTemplateProtectionRequested;

        public CompactWebView2Control()
        {
            InitializeComponent();
            
            // Subscribe to tab selection changes to handle locked tabs
            tabControl.Selecting += TabControl_Selecting;
            tabControl.Selected += TabControl_Selected;
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem_LockOverlay;
        }

        /// <summary>
        /// Locks the current tab using the app-level privacy PIN
        /// </summary>
        private void LockCurrentTab()
        {
            int selectedIndex = tabControl.SelectedIndex;
            if (selectedIndex < 0)
                return;

            // Check if privacy lock is configured
            if (!PrivacyLockManager.Instance.IsEnabled)
            {
                var result = MessageBox.Show(
                    "Privacy lock is not configured. Would you like to set it up now?\n\n" +
                    "The same PIN will be used for both app-level and tab-level locking.",
                    "Privacy Lock Setup Required",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    using (var settings = new PrivacyLockSettingsForm())
                    {
                        if (settings.ShowDialog() == DialogResult.OK)
                        {
                            // PIN is now configured, proceed with locking
                            LockTabAtIndex(selectedIndex);
                        }
                    }
                }
                return;
            }

            // PIN is configured, lock the tab silently
            LockTabAtIndex(selectedIndex);
        }

        /// <summary>
        /// Locks the tab at the specified index
        /// </summary>
        private void LockTabAtIndex(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabControl.TabPages.Count)
                return;

            // Add to locked tabs set
            _lockedTabs.Add(tabIndex);
            
            // Create and show the overlay
            ObfuscateTab(tabIndex);
            
            // Redraw the tab to show lock overlay
            tabControl.Invalidate();
            
            // If this is the currently selected tab, show URL bar overlay
            if (tabIndex == tabControl.SelectedIndex)
            {
                ShowUrlBarOverlay();
            }
        }

        /// <summary>
        /// Obfuscates a locked tab by adding a semi-transparent overlay
        /// </summary>
        private void ObfuscateTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabControl.TabPages.Count)
                return;

            // Don't create duplicate overlays
            if (_lockOverlays.ContainsKey(tabIndex))
                return;

            var tabPage = tabControl.TabPages[tabIndex];
            
            // Create a solid black overlay panel to completely obscure content
            var overlay = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 0, 0, 0), // Almost fully opaque black
                Cursor = Cursors.No
            };

            // Add a lock icon label in the center
            var lockLabel = new Label
            {
                Text = "🔒",
                Font = new Font("Segoe UI", 48, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 220), // Light gray
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Center the lock icon
            lockLabel.Location = new Point(
                (overlay.Width - lockLabel.Width) / 2,
                (overlay.Height - lockLabel.Height) / 2
            );

            overlay.Resize += (s, e) =>
            {
                lockLabel.Location = new Point(
                    (overlay.Width - lockLabel.Width) / 2,
                    (overlay.Height - lockLabel.Height) / 2
                );
            };

            overlay.Controls.Add(lockLabel);
            
            // Add right-click event to unlock the tab
            overlay.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    UnlockTabAtIndex(tabIndex);
                }
            };
            
            // Also add right-click to the lock label
            lockLabel.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    UnlockTabAtIndex(tabIndex);
                }
            };
            
            // Store the overlay
            _lockOverlays[tabIndex] = overlay;
            
            // Add overlay to the tab page (brings it to front)
            tabPage.Controls.Add(overlay);
            overlay.BringToFront();
        }

        /// <summary>
        /// Shows an overlay on the URL bar for locked tabs
        /// </summary>
        private void ShowUrlBarOverlay()
        {
            // Remove existing overlay if present
            HideUrlBarOverlay();
            
            _urlBarOverlay = new Panel
            {
                Size = txtUrl.Size,
                Location = txtUrl.Location,
                BackColor = Color.FromArgb(240, 30, 30, 30), // Almost fully opaque dark gray
                Cursor = Cursors.No
            };

            var lockLabel = new Label
            {
                Text = "🔒",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 220, 220), // Light gray
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Center the lock icon vertically
            lockLabel.Location = new Point(
                5,
                (_urlBarOverlay.Height - lockLabel.Height) / 2
            );

            _urlBarOverlay.Controls.Add(lockLabel);
            this.Controls.Add(_urlBarOverlay);
            _urlBarOverlay.BringToFront();
        }

        /// <summary>
        /// Hides the URL bar overlay
        /// </summary>
        private void HideUrlBarOverlay()
        {
            if (_urlBarOverlay != null)
            {
                this.Controls.Remove(_urlBarOverlay);
                _urlBarOverlay.Dispose();
                _urlBarOverlay = null;
            }
        }

        /// <summary>
        /// Custom draw handler to overlay lock icon on locked tabs
        /// </summary>
        private void TabControl_DrawItem_LockOverlay(object sender, DrawItemEventArgs e)
        {
            // First, call the existing draw method if you have one
            TabControl_DrawItem(sender, e);
            
            // Check if this tab is locked
            if (_lockedTabs.Contains(e.Index))
            {
                // Draw a solid black overlay on the tab header to obscure text
                using (var brush = new SolidBrush(Color.FromArgb(240, 0, 0, 0))) // Almost fully opaque black
                {
                    e.Graphics.FillRectangle(brush, e.Bounds);
                }
                
                // Draw lock icon on the tab
                string lockIcon = "🔒";
                using (var font = new Font("Segoe UI", 9, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.FromArgb(220, 220, 220))) // Light gray
                {
                    var textSize = e.Graphics.MeasureString(lockIcon, font);
                    var textX = e.Bounds.Left + (e.Bounds.Width - textSize.Width) / 2;
                    var textY = e.Bounds.Top + (e.Bounds.Height - textSize.Height) / 2;
                    e.Graphics.DrawString(lockIcon, font, textBrush, textX, textY);
                }
            }
        }

        /// <summary>
        /// Restores a tab's original appearance after unlocking
        /// </summary>
        private void RestoreTab(int tabIndex)
        {
            if (tabIndex < 0 || tabIndex >= tabControl.TabPages.Count)
                return;

            // Remove and dispose the overlay if it exists
            if (_lockOverlays.TryGetValue(tabIndex, out var overlay))
            {
                var tabPage = tabControl.TabPages[tabIndex];
                tabPage.Controls.Remove(overlay);
                overlay.Dispose();
                _lockOverlays.Remove(tabIndex);
            }
            
            // Redraw the tab to remove lock overlay
            tabControl.Invalidate();
            
            // If this is the currently selected tab, hide URL bar overlay
            if (tabIndex == tabControl.SelectedIndex)
            {
                HideUrlBarOverlay();
            }
        }

        /// <summary>
        /// Unlocks the current tab with PIN verification
        /// </summary>
        private void UnlockCurrentTab()
        {
            int selectedIndex = tabControl.SelectedIndex;
            if (selectedIndex < 0 || !_lockedTabs.Contains(selectedIndex))
                return;

            UnlockTabAtIndex(selectedIndex);
        }

        /// <summary>
        /// Unlocks a specific tab with PIN verification
        /// </summary>
        private void UnlockTabAtIndex(int tabIndex)
        {
            if (tabIndex < 0 || !_lockedTabs.Contains(tabIndex))
                return;

            using (var pinDialog = new TabPinDialog(isUnlocking: true))
            {
                if (pinDialog.ShowDialog() == DialogResult.OK)
                {
                    // Verify the PIN using the privacy lock manager
                    if (PrivacyLockManager.Instance.VerifyPin(pinDialog.EnteredPin))
                    {
                        // Remove from locked tabs set
                        _lockedTabs.Remove(tabIndex);
                        
                        // Restore the tab's original appearance
                        RestoreTab(tabIndex);
                    }
                    else
                    {
                        MessageBox.Show(
                            "Incorrect PIN. Tab remains locked.",
                            "Unlock Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                    }
                }
            }
        }

        /// <summary>
        /// Handles tab selection changes to enforce locked tab behavior
        /// </summary>
        private void TabControl_Selecting(object sender, TabControlCancelEventArgs e)
        {
            // Ensure locked tabs have their overlay visible
            if (_lockedTabs.Contains(e.TabPageIndex))
            {
                // Make sure overlay exists
                if (!_lockOverlays.ContainsKey(e.TabPageIndex))
                {
                    ObfuscateTab(e.TabPageIndex);
                }
            }
        }

        /// <summary>
        /// Handles tab selection completed to show/hide URL bar overlay
        /// </summary>
        private void TabControl_Selected(object sender, TabControlEventArgs e)
        {
            // Show URL bar overlay if locked tab is selected
            if (_lockedTabs.Contains(e.TabPageIndex))
            {
                ShowUrlBarOverlay();
            }
            else
            {
                HideUrlBarOverlay();
            }
        }

        /// <summary>
        /// Navigates the current tab to the specified URL
        /// </summary>
        public void NavigateToUrl(string url)
        {
            var currentTab = GetCurrentTab();
            if (currentTab != null)
            {
                _ = currentTab.NavigateToUrl(url);
            }
        }

        /// <summary>
        /// Goes back in navigation history for current tab
        /// </summary>
        public void GoBack()
        {
            GetCurrentTab()?.GoBack();
        }

        /// <summary>
        /// Goes forward in navigation history for current tab
        /// </summary>
        public void GoForward()
        {
            GetCurrentTab()?.GoForward();
        }

        /// <summary>
        /// Refreshes the current tab
        /// </summary>
        public void Refresh()
        {
            GetCurrentTab()?.Refresh();
        }

        /// <summary>
        /// Navigates current tab to the home URL
        /// </summary>
        public void GoHome()
        {
            NavigateToUrl(HomeUrl);
        }

        /// <summary>
        /// Gets the currently active browser tab
        /// </summary>
        private BrowserTab GetCurrentTab()
        {
            int selectedIndex = tabControl.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < _browserTabs.Count)
            {
                return _browserTabs[selectedIndex];
            }
            return null;
        }

        /// <summary>
        /// Gets or creates the shared WebView2 environment
        /// </summary>
        private static async Task<CoreWebView2Environment> GetSharedEnvironment()
        {
            if (_sharedEnvironment == null)
            {
                var userDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DynamicBrowserPanels",
                    "WebView2Data"
                );

                var options = new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--enable-features=msWebView2EnableEdgeInternalSchemes"
                };

                _sharedEnvironment = await CoreWebView2Environment.CreateAsync(
                    null, // browserExecutableFolder
                    userDataFolder,
                    options
                );
            }
            return _sharedEnvironment;
        }

        /// <summary>
        /// Raises the split requested event
        /// </summary>
        private void OnSplitRequested(Orientation orientation)
        {
            SplitRequested?.Invoke(this, new SplitRequestedEventArgs(orientation));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var tab in _browserTabs)
                {
                    tab?.Dispose();
                }
                _browserTabs.Clear();
                _lockedTabs.Clear();
                
                // Dispose all overlays
                foreach (var overlay in _lockOverlays.Values)
                {
                    overlay?.Dispose();
                }
                _lockOverlays.Clear();
                
                // Dispose URL bar overlay
                _urlBarOverlay?.Dispose();

                tabControl?.Dispose();
                contextMenu?.Dispose();
                txtUrl?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
