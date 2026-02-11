using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    public partial class CompactWebView2Control
    {
        /// <summary>
        /// Opens an online playlist (from JSON/M3U file or manual entry)
        /// </summary>
        private void OpenOnlinePlaylist()
        {
            var currentTab = GetCurrentTab();
            if (currentTab == null) return;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Online Playlist";
                openFileDialog.Filter = "JSON Playlist (*.json)|*.json|M3U Playlist (*.m3u;*.m3u8)|*.m3u;*.m3u8|All Files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.InitialDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DynamicBrowserPanels",
                    "Playlists"
                );
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    bool loaded = false;

                    if (openFileDialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        // Load from JSON
                        loaded = currentTab.OnlinePlaylist.LoadFromFile(openFileDialog.FileName);
                    }
                    else
                    {
                        // Load from M3U
                        loaded = LoadOnlinePlaylistFromM3U(openFileDialog.FileName);
                    }

                    if (loaded)
                    {
                        MessageBox.Show(
                            $"Loaded {currentTab.OnlinePlaylist.Count} online media items",
                            "Online Playlist Loaded",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        ShowOnlinePlaylistPlayer();
                    }
                }
            }
        }

        /// <summary>
        /// Loads online playlist from M3U file
        /// </summary>
        private bool LoadOnlinePlaylistFromM3U(string filePath)
        {
            var currentTab = GetCurrentTab();
            if (currentTab == null) return false;

            try
            {
                var lines = File.ReadAllLines(filePath);
                var items = new List<OnlineMediaItem>();
                string currentTitle = null;

                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.StartsWith("#EXTM3U"))
                        continue;

                    if (line.StartsWith("#EXTINF:"))
                    {
                        // Extract title from EXTINF line
                        var parts = line.Split(',');
                        if (parts.Length > 1)
                        {
                            currentTitle = parts[1].Trim();
                        }
                    }
                    else if (line.StartsWith("http://") || line.StartsWith("https://"))
                    {
                        // This is a URL
                        var item = new OnlineMediaItem(line, currentTitle);
                        items.Add(item);
                        currentTitle = null; // Reset for next item
                    }
                }

                if (items.Count > 0)
                {
                    currentTab.OnlinePlaylist.Clear();
                    currentTab.OnlinePlaylist.AddItems(items.ToArray());
                    currentTab.OnlinePlaylist.PlaylistName = Path.GetFileNameWithoutExtension(filePath);
                    return true;
                }
                else
                {
                    MessageBox.Show(
                        "No valid URLs found in the M3U file.",
                        "No URLs Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error loading online playlist:\n{ex.Message}",
                    "Load Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }

            return false;
        }

        /// <summary>
        /// Shows dialog to add a single online item to playlist (detailed entry)
        /// </summary>
        private void AddSingleOnlineItem()
        {
            var currentTab = GetCurrentTab();
            if (currentTab == null) return;

            using (var dialog = new OnlineItemDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK && dialog.Items.Count > 0)
                {
                    bool wasEmpty = currentTab.OnlinePlaylist.Count == 0;
                    
                    currentTab.OnlinePlaylist.AddItems(dialog.Items.ToArray());

                    MessageBox.Show(
                        $"Added {dialog.Items.Count} item(s) to online playlist.\n\n" +
                        $"Total items: {currentTab.OnlinePlaylist.Count}",
                        "Items Added",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    // If playlist was empty before, show the player
                    if (wasEmpty)
                    {
                        ShowOnlinePlaylistPlayer();
                    }
                }
            }
        }

        /// <summary>
        /// Shows dialog to add multiple URLs at once via bulk text input
        /// </summary>
        private void AddBulkOnlineUrls()
        {
            var currentTab = GetCurrentTab();
            if (currentTab == null) return;

            using (var dialog = new BulkUrlDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK && dialog.Items.Count > 0)
                {
                    bool wasEmpty = currentTab.OnlinePlaylist.Count == 0;
                    
                    currentTab.OnlinePlaylist.AddItems(dialog.Items.ToArray());

                    MessageBox.Show(
                        $"Added {dialog.Items.Count} URL(s) to online playlist.\n\n" +
                        $"Total items: {currentTab.OnlinePlaylist.Count}",
                        "URLs Added",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );

                    // If playlist was empty before, show the player
                    if (wasEmpty)
                    {
                        ShowOnlinePlaylistPlayer();
                    }
                }
            }
        }

        /// <summary>
        /// Shows the online playlist player
        /// </summary>
        private void ShowOnlinePlaylistPlayer()
        {
            var currentTab = GetCurrentTab();
            if (currentTab?.OnlinePlaylist == null || currentTab.OnlinePlaylist.Count == 0)
                return;

            try
            {
                var templatePath = BrowserStateManager.GetCurrentLayoutPath();
                var tempHtmlPath = LocalMediaHelper.CreateTemporaryOnlinePlaylistPlayerFile(
                    currentTab.OnlinePlaylist.MediaItems,
                    currentTab.OnlinePlaylist.CurrentIndex,
                    currentTab.OnlinePlaylist.Shuffle,
                    currentTab.OnlinePlaylist.Repeat,
                    templatePath
                );
                var playerUrl = LocalMediaHelper.FilePathToUrl(tempHtmlPath);
                NavigateToUrl(playerUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error showing online playlist:\n{ex.Message}",
                    "Playlist Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}