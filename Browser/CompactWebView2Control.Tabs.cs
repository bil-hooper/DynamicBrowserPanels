using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    public partial class CompactWebView2Control
    {
        /// <summary>
        /// Ensures at least one tab exists (creates a default tab if needed)
        /// </summary>
        public async Task EnsureTabExists()
        {
            if (_browserTabs.Count == 0)
            {
                await AddNewTab();
            }
        }

        /// <summary>
        /// Adds a new tab with the specified URL
        /// </summary>
        private async Task<BrowserTab> AddNewTab(string url = null, bool isIncognito = false)
        {
            string tabPrefix = isIncognito ? "🕶️ " : "";
            var tabPage = new TabPage($"{tabPrefix}Tab {_browserTabs.Count + 1}");
            var browserTab = new BrowserTab(await GetSharedEnvironment(), isIncognito);
            
            browserTab.WebView.Dock = DockStyle.Fill;
            browserTab.UrlChanged += BrowserTab_UrlChanged;
            browserTab.TitleChanged += BrowserTab_TitleChanged;
            
            tabPage.Controls.Add(browserTab.WebView);
            tabControl.TabPages.Add(tabPage);
            
            _browserTabs.Add(browserTab);
            _tabCustomNames.Add(null); // No custom name for new tabs
            
            // Use the provided URL, or fallback to _homeUrl if not provided
            string targetUrl = url ?? GlobalConstants.DEFAULT_URL;
            
            // Pass the URL to Initialize so it navigates during initialization
            await browserTab.Initialize(targetUrl);
            
            return browserTab;
        }

        /// <summary>
        /// Adds a new incognito tab
        /// </summary>
        private async Task AddNewIncognitoTab()
        {
            await AddNewTab(null, isIncognito: true);
        }

        /// <summary>
        /// Closes the currently selected tab
        /// </summary>
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

        /// <summary>
        /// Closes the tab at the specified index
        /// </summary>
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
                    string incognitoPrefix = currentTab.IsIncognito ? "🕶️ " : "";

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
                                currentTabPage.Text = $"{incognitoPrefix}Tab {selectedIndex + 1}";
                            }
                        }
                        else
                        {
                            currentTabPage.Text = $"{incognitoPrefix}Tab {selectedIndex + 1}";
                        }
                    }
                    else
                    {
                        // Set custom name
                        _tabCustomNames[selectedIndex] = newName;
                        currentTab.CustomName = newName;
                        currentTabPage.Text = incognitoPrefix + newName;
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

            MoveTab(selectedIndex, selectedIndex - 1);
        }

        /// <summary>
        /// Moves the current tab to the right
        /// </summary>
        private void MoveTabRight()
        {
            int selectedIndex = tabControl.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= tabControl.TabPages.Count - 1) return;

            MoveTab(selectedIndex, selectedIndex + 1);
        }

        /// <summary>
        /// Moves the current tab all the way to the left (first position)
        /// </summary>
        private void MoveTabToStart()
        {
            int selectedIndex = tabControl.SelectedIndex;
            if (selectedIndex <= 0) return;

            MoveTab(selectedIndex, 0);
        }

        /// <summary>
        /// Moves the current tab all the way to the right (last position)
        /// </summary>
        private void MoveTabToEnd()
        {
            int selectedIndex = tabControl.SelectedIndex;
            int lastIndex = tabControl.TabPages.Count - 1;

            if (selectedIndex < 0 || selectedIndex >= lastIndex) return;

            MoveTab(selectedIndex, lastIndex);
        }

        /// <summary>
        /// Core method to move a tab from one index to another
        /// </summary>
        private void MoveTab(int fromIndex, int toIndex)
        {
            if (fromIndex == toIndex) return;

            // Map each TabPage to its lock state
            var tabLockStates = new Dictionary<TabPage, bool>();
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                tabLockStates[tabControl.TabPages[i]] = _lockedTabs.Contains(i);
            }

            // Move the tab and associated data
            var tabPage = tabControl.TabPages[fromIndex];
            var browserTab = _browserTabs[fromIndex];
            var customName = _tabCustomNames[fromIndex];

            tabControl.TabPages.RemoveAt(fromIndex);
            _browserTabs.RemoveAt(fromIndex);
            _tabCustomNames.RemoveAt(fromIndex);

            tabControl.TabPages.Insert(toIndex, tabPage);
            _browserTabs.Insert(toIndex, browserTab);
            _tabCustomNames.Insert(toIndex, customName);

            // Rebuild lock states based on TabPage objects
            _lockedTabs.Clear();
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                if (tabLockStates[tabControl.TabPages[i]])
                {
                    _lockedTabs.Add(i);
                }
            }

            tabControl.SelectedIndex = toIndex;

            // Rebuild overlays
            RebuildLockOverlays();

            // Redraw tabs
            tabControl.Invalidate();
        }

        /// <summary>
        /// Rebuilds the lock overlays dictionary to match current tab indices
        /// </summary>
        private void RebuildLockOverlays()
        {
            // Dispose old overlays but keep track of which tabs should have overlays
            var oldOverlays = new Dictionary<int, Panel>(_lockOverlays);
            _lockOverlays.Clear();

            // Dispose all old overlays
            foreach (var overlay in oldOverlays.Values)
            {
                if (overlay != null && overlay.Parent != null)
                {
                    overlay.Parent.Controls.Remove(overlay);
                }
                overlay?.Dispose();
            }

            // Recreate overlays for locked tabs at their new positions
            foreach (int lockedIndex in _lockedTabs)
            {
                ObfuscateTab(lockedIndex);
            }
        }
        }
}