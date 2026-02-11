using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    public partial class CompactWebView2Control
    {
        /// <summary>
        /// Gets all tab URLs and the selected tab index for state saving
        /// </summary>
        public TabsStateData GetTabsState()
        {
            var tabUrls = new List<string>();
            var tabPlaylists = new List<PlaylistStateData>();
            
            foreach (var tab in _browserTabs)
            {
                var url = tab.CurrentUrl;
                
                if (string.IsNullOrWhiteSpace(url))
                {
                    url = HomeUrl;
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
                
                // Save playlist state for each tab (MachineName auto-set if playlist has files)
                if (tab.Playlist != null && tab.Playlist.Count > 0)
                {
                    var playlistState = tab.Playlist.GetState();
                    tabPlaylists.Add(playlistState);
                }
                else
                {
                    tabPlaylists.Add(null); // No playlist for this tab
                }
            }
            
            // Create the result object
            var result = new TabsStateData
            {
                SelectedTabIndex = tabControl.SelectedIndex,
                TabUrls = tabUrls,
                TabCustomNames = new List<string>(_tabCustomNames),
                TabPlaylists = tabPlaylists
            };
            
            return result;
        }

        /// <summary>
        /// Restores tabs from saved state, filtering machine-specific playlist tabs
        /// </summary>
        public async Task RestoreTabsState(TabsStateData state)
        {
            if (state == null || state.TabUrls == null || state.TabUrls.Count == 0)
            {
                // No state to restore, create default tab if needed
                if (_browserTabs.Count == 0)
                {
                    await AddNewTab(HomeUrl);
                }
                return;
            }

            // Get current machine name
            string currentMachine = Environment.MachineName;

            // Filter tabs based on machine-specific playlists
            var filteredUrls = new List<string>();
            var filteredCustomNames = new List<string>();
            var filteredPlaylists = new List<PlaylistStateData>();
            int originalSelectedIndex = state.SelectedTabIndex;
            int newSelectedIndex = -1;

            for (int i = 0; i < state.TabUrls.Count; i++)
            {
                PlaylistStateData playlist = null;
                if (state.TabPlaylists != null && i < state.TabPlaylists.Count)
                {
                    playlist = state.TabPlaylists[i];
                }

                // Check if this tab should be shown on this machine
                bool showTab = true;
                if (playlist != null && !string.IsNullOrEmpty(playlist.MachineName))
                {
                    // This is a machine-specific playlist - only show if it matches current machine
                    showTab = playlist.MachineName.Equals(currentMachine, StringComparison.OrdinalIgnoreCase);
                }

                if (showTab)
                {
                    filteredUrls.Add(state.TabUrls[i]);
                    filteredCustomNames.Add(state.TabCustomNames != null && i < state.TabCustomNames.Count 
                        ? state.TabCustomNames[i] 
                        : null);
                    filteredPlaylists.Add(playlist);

                    // Track the selected index
                    if (i == originalSelectedIndex)
                    {
                        newSelectedIndex = filteredUrls.Count - 1;
                    }
                }
            }

            // If no tabs passed the filter, create a default tab
            if (filteredUrls.Count == 0)
            {
                if (_browserTabs.Count == 0)
                {
                    await AddNewTab(HomeUrl);
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

            // Create tabs from filtered state
            for (int i = 0; i < filteredUrls.Count; i++)
            {
                string customName = filteredCustomNames[i];
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
                
                // Restore playlist state for this tab BEFORE navigating
                if (filteredPlaylists[i] != null)
                {
                    browserTab.Playlist.RestoreState(filteredPlaylists[i]);
                }
                
                await NavigateTabToUrl(browserTab, filteredUrls[i]);
            }

            // Select the appropriate tab
            if (newSelectedIndex >= 0 && newSelectedIndex < tabControl.TabPages.Count)
            {
                tabControl.SelectedIndex = newSelectedIndex;
            }
            else if (tabControl.TabPages.Count > 0)
            {
                // If the originally selected tab was filtered out, select the first tab
                tabControl.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Navigates a tab to a URL, handling media:// URLs specially
        /// </summary>
        private async Task NavigateTabToUrl(BrowserTab tab, string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                await tab.NavigateToUrl(HomeUrl);
                return;
            }

            // Check if this is a media file URL
            if (url.StartsWith("media:///"))
            {
                // Extract the file path
                var filePath = url.Substring(9); // Remove "media:///
                
                // Check if file still exists
                if (File.Exists(filePath))
                {
                    try
                    {
                        // Get playlist information if available
                        List<string> playlistFiles = null;
                        int currentIndex = 0;
                        
                        if (tab?.Playlist != null && tab.Playlist.Count > 0)
                        {
                            playlistFiles = tab.Playlist.MediaFiles;
                            // Find the index of the current file in the playlist
                            currentIndex = playlistFiles.IndexOf(filePath);
                            if (currentIndex < 0) currentIndex = 0;
                        }
                        
                        // Recreate the HTML player - use full playlist player if we have multiple files
                        var tempHtmlPath = LocalMediaHelper.CreateTemporaryPlayerFile(
                            filePath,
                            autoplay: true,  // Enable autoplay when restoring media from saved state
                            loop: false,
                            playlistFiles: playlistFiles,
                            currentIndex: currentIndex
                        );
                        var playerUrl = LocalMediaHelper.FilePathToUrl(tempHtmlPath);
                        await tab.NavigateToUrl(playerUrl);
                    }
                    catch
                    {
                        // If recreation fails, just navigate to home
                        await tab.NavigateToUrl(HomeUrl);
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
                    await tab.NavigateToUrl(HomeUrl);
                }
            }
            else
            {
                // Regular URL
                await tab.NavigateToUrl(url);
            }
        }
    }
}