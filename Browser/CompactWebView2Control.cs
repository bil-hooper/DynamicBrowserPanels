using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Compact WebView2 control with tabbed browsing support
    /// </summary>
    public class CompactWebView2Control : UserControl
    {
        private TabControl tabControl;
        private TextBox txtUrl;
        private ContextMenuStrip contextMenu;
        private ToolStripMenuItem mnuBack;
        private ToolStripMenuItem mnuForward;
        private ToolStripMenuItem mnuRefresh;
        private ToolStripMenuItem mnuHome;
        private ToolStripSeparator separator1;
        private ToolStripMenuItem mnuOpenMedia;
        private ToolStripSeparator separator1b;
        private ToolStripMenuItem mnuNewTab;
        private ToolStripMenuItem mnuCloseTab;
        private ToolStripMenuItem mnuRenameTab;
        private ToolStripMenuItem mnuMoveTabLeft;
        private ToolStripMenuItem mnuMoveTabRight;
        private ToolStripSeparator separator2;
        private ToolStripMenuItem mnuSplitHorizontal;
        private ToolStripMenuItem mnuSplitVertical;
        private ToolStripSeparator separator3;
        private ToolStripMenuItem mnuSaveLayout;
        private ToolStripMenuItem mnuLoadLayout;
        private ToolStripSeparator separator4;
        private ToolStripMenuItem mnuResetLayout;
        private ToolStripMenuItem mnuManagePasswords;
        private ToolStripMenuItem mnuInstall;
        private ToolStripMenuItem mnuUninstall;
        
        private string _homeUrl = GlobalConstants.DEFAULT_URL;
        private List<BrowserTab> _browserTabs = new List<BrowserTab>();
        private static CoreWebView2Environment _sharedEnvironment;

        // Track custom names for serialization
        private List<string> _tabCustomNames = new List<string>();

        public event EventHandler<SplitRequestedEventArgs> SplitRequested;
        public event EventHandler ResetLayoutRequested;
        public event EventHandler SaveLayoutRequested;
        public event EventHandler LoadLayoutRequested;

        public CompactWebView2Control()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the home URL
        /// </summary>
        public string HomeUrl
        {
            get => _homeUrl;
            set => _homeUrl = value;
        }

        /// <summary>
        /// Gets the current URL of the active tab
        /// </summary>
        public string CurrentUrl => GetCurrentTab()?.CurrentUrl ?? _homeUrl;

        /// <summary>
        /// Gets all tab URLs and the selected tab index for state saving
        /// </summary>
        public TabsStateData GetTabsState()
        {
            var tabUrls = new List<string>();
            
            foreach (var tab in _browserTabs)
            {
                var url = tab.CurrentUrl;
                
                if (string.IsNullOrWhiteSpace(url))
                {
                    url = _homeUrl;
                }
                
                if (LocalMediaHelper.IsTempMediaPlayerUrl(url))
                {
                    var originalPath = LocalMediaHelper.GetOriginalMediaPath(url);
                    if (!string.IsNullOrEmpty(originalPath))
                    {
                        url = "media:///" + originalPath;
                    }
                }
                
                tabUrls.Add(url);
            }
            
            // Create the result object
            var result = new TabsStateData
            {
                SelectedTabIndex = tabControl.SelectedIndex,
                TabUrls = tabUrls,
                TabCustomNames = new List<string>(_tabCustomNames)
            };
                       
            return result;
        }

        /// <summary>
        /// Restores tabs from saved state
        /// </summary>
        public async Task RestoreTabsState(TabsStateData state)
        {
            if (state == null || state.TabUrls == null || state.TabUrls.Count == 0)
            {
                // No state to restore, create default tab if needed
                if (_browserTabs.Count == 0)
                {
                    await AddNewTab(_homeUrl);
                }
                return;
            }

            // Snapshot current tabs for disposal
            var tabsToDispose = _browserTabs.ToList();
            
            // Clear collections immediately
            _browserTabs.Clear();
            tabControl.TabPages.Clear();
            _tabCustomNames.Clear();
            
            // Dispose old tabs asynchronously (after UI is cleared)
            _ = Task.Run(async () =>
            {
                await Task.Delay(GlobalConstants.TAB_DISPOSAL_DELAY_MS);
                foreach (var tab in tabsToDispose)
                {
                    try
                    {
                        tab.Dispose();
                    }
                    catch
                    {
                        // Ignore disposal errors
                    }
                }
            });

            // Create tabs from state - manually without using AddNewTab() to avoid race conditions
            for (int i = 0; i < state.TabUrls.Count; i++)
            {
                string customName = null;
                if (state.TabCustomNames != null && i < state.TabCustomNames.Count)
                {
                    customName = state.TabCustomNames[i];
                }
                
                bool hasCustomName = !string.IsNullOrWhiteSpace(customName);
                
                var tabPage = new TabPage(hasCustomName ? customName : $"Tab {i + 1}");
                var browserTab = new BrowserTab(await GetSharedEnvironment());
                
                // Set custom name BEFORE any events can fire - this prevents title overwrites
                browserTab.CustomName = customName;
                
                browserTab.WebView.Dock = DockStyle.Fill;
                browserTab.UrlChanged += BrowserTab_UrlChanged;
                browserTab.TitleChanged += BrowserTab_TitleChanged;
                
                tabPage.Controls.Add(browserTab.WebView);
                tabControl.TabPages.Add(tabPage);
                
                _browserTabs.Add(browserTab);
                _tabCustomNames.Add(customName);
                
                await browserTab.Initialize(null);
                await NavigateTabToUrl(browserTab, state.TabUrls[i]);
            }
    
            // Select the previously selected tab
            if (state.SelectedTabIndex >= 0 && state.SelectedTabIndex < tabControl.TabPages.Count)
            {
                tabControl.SelectedIndex = state.SelectedTabIndex;
            }
        }

        /// <summary>
        /// Ensures at least one tab exists (creates a default tab if needed)
        /// </summary>
        public async Task EnsureTabExists()
        {
            if (_browserTabs.Count == 0)
            {
                await AddNewTab(_homeUrl);
            }
        }

        /// <summary>
        /// Navigates a tab to a URL, handling media:// URLs specially
        /// </summary>
        private async Task NavigateTabToUrl(BrowserTab tab, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                await tab.NavigateToUrl(_homeUrl);
                return;
            }

            // Check if this is a media file URL
            if (url.StartsWith("media:///"))
            {
                // Extract the file path
                var filePath = url.Substring(9); // Remove "media:///"
                
                // Check if file still exists
                if (File.Exists(filePath))
                {
                    try
                    {
                        // Recreate the HTML player
                        var tempHtmlPath = LocalMediaHelper.CreateTemporaryPlayerFile(filePath);
                        var playerUrl = LocalMediaHelper.FilePathToUrl(tempHtmlPath);
                        await tab.NavigateToUrl(playerUrl);
                    }
                    catch
                    {
                        // If recreation fails, just navigate to home
                        await tab.NavigateToUrl(_homeUrl);
                    }
                }
                else
                {
                    // File no longer exists, show error in tab
                    MessageBox.Show(
                        $"Media file not found:\n{filePath}\n\nThis tab will load the home page instead.",
                        "Media File Missing",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    await tab.NavigateToUrl(_homeUrl);
                }
            }
            else
            {
                // Regular URL
                await tab.NavigateToUrl(url);
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
            NavigateToUrl(_homeUrl);
        }

        /// <summary>
        /// Opens a file dialog to select and play a media file
        /// </summary>
        private void OpenMediaFile()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Media File";
                openFileDialog.Filter = 
                    "All Media Files|*.mp4;*.webm;*.ogv;*.ogg;*.mp3;*.wav;*.aac;*.m4a;*.opus;*.flac|" +
                    "Video Files|*.mp4;*.webm;*.ogv;*.ogg|" +
                    "Audio Files|*.mp3;*.wav;*.aac;*.m4a;*.opus;*.flac;*.ogg|" +
                    "All Files|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var filePath = openFileDialog.FileName;
                    
                    // Validate the media file
                    if (!LocalMediaHelper.ValidateMediaFile(filePath, out string errorMessage))
                    {
                        MessageBox.Show(
                            errorMessage,
                            "Cannot Open Media File",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        
                        // Suggest conversion if format is unsupported
                        var suggestion = LocalMediaHelper.GetConversionSuggestion(filePath);
                        if (suggestion != null)
                        {
                            MessageBox.Show(
                                suggestion,
                                "Conversion Required",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                        }
                        return;
                    }

                    // Show warning for large files
                    if (errorMessage != null)
                    {
                        var result = MessageBox.Show(
                            errorMessage + "\n\nContinue anyway?",
                            "Warning",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Warning
                        );
                        
                        if (result != DialogResult.Yes)
                            return;
                    }

                    try
                    {
                        // Create HTML player page with the media file
                        var tempHtmlPath = LocalMediaHelper.CreateTemporaryPlayerFile(filePath);
                        var url = LocalMediaHelper.FilePathToUrl(tempHtmlPath);
                        
                        // Navigate to the player
                        NavigateToUrl(url);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error opening media file:\n{ex.Message}",
                            "Media Playback Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // URL TextBox
            txtUrl = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 9F)
            };
            txtUrl.KeyDown += TxtUrl_KeyDown;

            // Tab Control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            // Context Menu
            contextMenu = new ContextMenuStrip();
            
            mnuBack = new ToolStripMenuItem("← Back");
            mnuBack.Click += (s, e) => GoBack();
            
            mnuForward = new ToolStripMenuItem("→ Forward");
            mnuForward.Click += (s, e) => GoForward();
            
            mnuRefresh = new ToolStripMenuItem("⟳ Refresh");
            mnuRefresh.Click += (s, e) => Refresh();
            
            mnuHome = new ToolStripMenuItem("⌂ Home");
            mnuHome.Click += (s, e) => GoHome();
            
            separator1 = new ToolStripSeparator();
            
            mnuOpenMedia = new ToolStripMenuItem("📁 Open Media File...");
            mnuOpenMedia.Click += (s, e) => OpenMediaFile();
            
            separator1b = new ToolStripSeparator();
            
            mnuNewTab = new ToolStripMenuItem("+ New Tab");
            mnuNewTab.Click += async (s, e) => await AddNewTab(_homeUrl);
            
            mnuCloseTab = new ToolStripMenuItem("✕ Close Tab");
            mnuCloseTab.Click += (s, e) => CloseCurrentTab();
            
            mnuRenameTab = new ToolStripMenuItem("✎ Rename Tab...");
            mnuRenameTab.Click += (s, e) => RenameCurrentTab();
            
            mnuMoveTabLeft = new ToolStripMenuItem("← Move Tab Left");
            mnuMoveTabLeft.Click += (s, e) => MoveTabLeft();
            
            mnuMoveTabRight = new ToolStripMenuItem("Move Tab Right →");
            mnuMoveTabRight.Click += (s, e) => MoveTabRight();
            
            separator2 = new ToolStripSeparator();
            
            mnuSplitHorizontal = new ToolStripMenuItem("Split Horizontal ⬌");
            mnuSplitHorizontal.Click += (s, e) => OnSplitRequested(Orientation.Horizontal);
            
            mnuSplitVertical = new ToolStripMenuItem("Split Vertical ⬍");
            mnuSplitVertical.Click += (s, e) => OnSplitRequested(Orientation.Vertical);
            
            separator3 = new ToolStripSeparator();
            
            mnuSaveLayout = new ToolStripMenuItem("💾 Save Layout As...");
            mnuSaveLayout.Click += (s, e) => SaveLayoutRequested?.Invoke(this, EventArgs.Empty);
            
            mnuLoadLayout = new ToolStripMenuItem("📂 Load Layout...");
            mnuLoadLayout.Click += (s, e) => LoadLayoutRequested?.Invoke(this, EventArgs.Empty);
            
            separator4 = new ToolStripSeparator();
            
            mnuResetLayout = new ToolStripMenuItem("Reset Layout");
            mnuResetLayout.Click += (s, e) => ResetLayoutRequested?.Invoke(this, EventArgs.Empty);

            // Add password management option
            mnuManagePasswords = new ToolStripMenuItem("🔑 Manage Passwords");
            mnuManagePasswords.Click += (s, e) => OpenPasswordManager();

            // Add installation options
            mnuInstall = new ToolStripMenuItem("⚙️ Install Application...");
            mnuInstall.Click += (s, e) => InstallationManager.Install();
            
            mnuUninstall = new ToolStripMenuItem("🗑️ Uninstall Application...");
            mnuUninstall.Click += (s, e) => InstallationManager.Uninstall();

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                mnuBack, mnuForward, mnuRefresh, mnuHome,
                separator1,
                mnuOpenMedia,
                separator1b,
                mnuNewTab, mnuCloseTab, mnuRenameTab,
                new ToolStripSeparator(),
                mnuMoveTabLeft, mnuMoveTabRight,
                separator2,
                mnuSplitHorizontal, mnuSplitVertical,
                separator3,
                mnuSaveLayout, mnuLoadLayout,
                separator4,
                mnuResetLayout,
                new ToolStripSeparator(),
                mnuManagePasswords,
                new ToolStripSeparator(),
                mnuInstall,
                mnuUninstall
            });

            contextMenu.Opening += ContextMenu_Opening;

            // Add controls
            this.Controls.Add(tabControl);
            this.Controls.Add(txtUrl);
            
            // Attach context menu to URL textbox instead of WebView2
            txtUrl.ContextMenuStrip = contextMenu;

            this.ResumeLayout(false);
        }

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

        private void OpenPasswordManager()
        {
            NavigateToUrl("https://passwords.google.com");
        }

        private async Task<BrowserTab> AddNewTab(string url = null)
        {
            var tabPage = new TabPage($"Tab {_browserTabs.Count + 1}");
            var browserTab = new BrowserTab(await GetSharedEnvironment());
            
            browserTab.WebView.Dock = DockStyle.Fill;
            browserTab.UrlChanged += BrowserTab_UrlChanged;
            browserTab.TitleChanged += BrowserTab_TitleChanged;
            
            tabPage.Controls.Add(browserTab.WebView);
            tabControl.TabPages.Add(tabPage);
            
            _browserTabs.Add(browserTab);
            _tabCustomNames.Add(null); // No custom name for new tabs
            
            await browserTab.Initialize(null);
            
            if (!string.IsNullOrWhiteSpace(url))
            {
                await browserTab.NavigateToUrl(url);
            }
            
            return browserTab;
        }

        private void CloseCurrentTab()
        {
            if (tabControl.SelectedIndex >= 0 && _browserTabs.Count > 1)
            {
                CloseTab(tabControl.SelectedIndex);
            }
            else
            {
                MessageBox.Show(
                    "Cannot close the last tab. At least one tab must remain open.",
                    "Close Tab",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        private void CloseTab(int index)
        {
            if (index < 0 || index >= _browserTabs.Count)
                return;

            if (_browserTabs.Count <= 1)
                return;

            var tab = _browserTabs[index];
            tab.Dispose();
            
            _browserTabs.RemoveAt(index);
            tabControl.TabPages.RemoveAt(index);
            _tabCustomNames.RemoveAt(index);
        }

        private BrowserTab GetCurrentTab()
        {
            int selectedIndex = tabControl.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < _browserTabs.Count)
            {
                return _browserTabs[selectedIndex];
            }
            return null;
        }

        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var currentTab = GetCurrentTab();
            if (currentTab != null)
            {
                txtUrl.Text = currentTab.CurrentUrl;
            }
            UpdateContextMenuButtons();
        }

        private void BrowserTab_UrlChanged(object sender, string url)
        {
            var currentTab = GetCurrentTab();
            if (sender == currentTab)
            {
                if (!txtUrl.Focused)
                {
                    txtUrl.Text = url;
                }
            }
        }

        private void BrowserTab_TitleChanged(object sender, string title)
        {
            var tab = sender as BrowserTab;
            if (tab == null) return;
            
            int index = _browserTabs.IndexOf(tab);
            if (index < 0 || index >= tabControl.TabPages.Count) return;
            
            // The event won't fire if tab has CustomName (handled in BrowserTab.cs)
            // So if we're here, we should update the title
            string displayTitle = title ?? $"Tab {index + 1}";
            if (displayTitle.Length > GlobalConstants.MAX_TAB_TITLE_LENGTH)
            {
                displayTitle = displayTitle.Substring(0, GlobalConstants.TITLE_TRUNCATE_LENGTH) + "...";
            }
            tabControl.TabPages[index].Text = displayTitle;
        }

        private void TxtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                NavigateToUrl(txtUrl.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateContextMenuButtons();
        }

        private void UpdateContextMenuButtons()
        {
            var currentTab = GetCurrentTab();

            if (currentTab?.IsInitialized == true)
            {
                mnuBack.Enabled = currentTab.CanGoBack;
                mnuForward.Enabled = currentTab.CanGoForward;
            }
            else
            {
                mnuBack.Enabled = false;
                mnuForward.Enabled = false;
            }

            // Can only close tab if more than one tab exists
            mnuCloseTab.Enabled = _browserTabs.Count > 1;
            
            // Enable rename if at least one tab exists
            mnuRenameTab.Enabled = _browserTabs.Count > 0;
            
            // Enable move left if not at the leftmost position
            mnuMoveTabLeft.Enabled = tabControl.SelectedIndex > 0;
            
            // Enable move right if not at the rightmost position
            mnuMoveTabRight.Enabled = tabControl.SelectedIndex >= 0 && 
                                      tabControl.SelectedIndex < _browserTabs.Count - 1;

            // Show Install or Uninstall based on installation status
            bool isInstalled = InstallationManager.IsInstalled();
            mnuInstall.Visible = !isInstalled;
            mnuUninstall.Visible = isInstalled;
        }

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
                
                tabControl?.Dispose();
                contextMenu?.Dispose();
                txtUrl?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Renames the current tab
        /// </summary>
        private void RenameCurrentTab()
        {
            int selectedIndex = tabControl.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= tabControl.TabPages.Count)
                return;

            var currentTabPage = tabControl.TabPages[selectedIndex];
            var currentTab = GetCurrentTab();
            if (currentTab == null) return;

            string currentCustomName = selectedIndex < _tabCustomNames.Count ? _tabCustomNames[selectedIndex] : null;

            using (var inputForm = new Form())
            {
                inputForm.Text = "Rename Tab";
                inputForm.Size = new Size(400, 180);
                inputForm.StartPosition = FormStartPosition.CenterParent;
                inputForm.FormBorderStyle = FormBorderStyle.FixedDialog;
                inputForm.MaximizeBox = false;
                inputForm.MinimizeBox = false;

                var label = new Label
                {
                    Text = "Enter new tab name (leave empty to use page title):",
                    Location = new Point(20, 20),
                    Size = new Size(360, 40)
                };

                var textBox = new TextBox
                {
                    Text = currentCustomName ?? "",
                    Location = new Point(20, 65),
                    Width = 340
                };
                textBox.SelectAll();

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Location = new Point(200, 110),
                    Width = 75
                };

                var cancelButton = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(285, 110),
                    Width = 75
                };

                inputForm.Controls.AddRange(new Control[] { label, textBox, okButton, cancelButton });
                inputForm.AcceptButton = okButton;
                inputForm.CancelButton = cancelButton;

                if (inputForm.ShowDialog() == DialogResult.OK)
                {
                    string newName = textBox.Text.Trim();

                    if (string.IsNullOrWhiteSpace(newName))
                    {
                        // Clear custom name
                        _tabCustomNames[selectedIndex] = null;
                        currentTab.CustomName = null;

                        // IMPORTANT: Manually trigger the title update by directly calling the handler
                        // This simulates what would happen if the page title changed
                        if (currentTab.IsInitialized && currentTab.WebView?.CoreWebView2 != null)
                        {
                            string pageTitle = currentTab.WebView.CoreWebView2.DocumentTitle;
                            if (!string.IsNullOrWhiteSpace(pageTitle))
                            {
                                // Manually invoke the title changed handler
                                BrowserTab_TitleChanged(currentTab, pageTitle);
                            }
                            else
                            {
                                currentTabPage.Text = $"Tab {selectedIndex + 1}";
                            }
                        }
                        else
                        {
                            currentTabPage.Text = $"Tab {selectedIndex + 1}";
                        }
                    }
                    else
                    {
                        // Set custom name
                        _tabCustomNames[selectedIndex] = newName;
                        currentTab.CustomName = newName;
                        currentTabPage.Text = newName;
                    }
                }
            }
        }

        /// <summary>
        /// Moves the current tab to the left
        /// </summary>
        private void MoveTabLeft()
        {
            int selectedIndex = tabControl.SelectedIndex;
            if (selectedIndex <= 0) return;

            var tabPage = tabControl.TabPages[selectedIndex];
            var browserTab = _browserTabs[selectedIndex];
            var customName = _tabCustomNames[selectedIndex];

            tabControl.TabPages.RemoveAt(selectedIndex);
            _browserTabs.RemoveAt(selectedIndex);
            _tabCustomNames.RemoveAt(selectedIndex);

            int newIndex = selectedIndex - 1;
            tabControl.TabPages.Insert(newIndex, tabPage);
            _browserTabs.Insert(newIndex, browserTab);
            _tabCustomNames.Insert(newIndex, customName);

            tabControl.SelectedIndex = newIndex;
        }

        /// <summary>
        /// Moves the current tab to the right
        /// </summary>
        private void MoveTabRight()
        {
            int selectedIndex = tabControl.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= tabControl.TabPages.Count - 1) return;

            var tabPage = tabControl.TabPages[selectedIndex];
            var browserTab = _browserTabs[selectedIndex];
            var customName = _tabCustomNames[selectedIndex];

            tabControl.TabPages.RemoveAt(selectedIndex);
            _browserTabs.RemoveAt(selectedIndex);
            _tabCustomNames.RemoveAt(selectedIndex);

            int newIndex = selectedIndex + 1;
            tabControl.TabPages.Insert(newIndex, tabPage);
            _browserTabs.Insert(newIndex, browserTab);
            _tabCustomNames.Insert(newIndex, customName);

            tabControl.SelectedIndex = newIndex;
        }
    }
}
