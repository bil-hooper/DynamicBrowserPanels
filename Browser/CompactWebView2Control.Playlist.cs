using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    public partial class CompactWebView2Control
    {
        /// <summary>
        /// Pauses all media playback (playlists and videos) in the current tab
        /// </summary>
        public async Task PauseAllMediaAsync()
        {
            var currentTab = GetCurrentTab();
            if (currentTab?.IsInitialized != true || currentTab.WebView?.CoreWebView2 == null)
                return;

            try
            {
                // Execute JavaScript to pause all media elements (audio and video)
                string pauseScript = @"
                    (function() {
                        // Pause all video elements
                        var videos = document.querySelectorAll('video');
                        for (var i = 0; i < videos.length; i++) {
                            if (!videos[i].paused) {
                                videos[i].pause();
                            }
                        }
                        
                        // Pause all audio elements
                        var audios = document.querySelectorAll('audio');
                        for (var i = 0; i < audios.length; i++) {
                            if (!audios[i].paused) {
                                audios[i].pause();
                            }
                        }
                        
                        return true;
                    })();
                ";

                await currentTab.WebView.CoreWebView2.ExecuteScriptAsync(pauseScript);
            }
            catch
            {
                // Silently fail if script execution fails
            }
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
                
                // Use last media directory if available
                var lastDir = AppConfiguration.LastMediaDirectory;
                if (!string.IsNullOrEmpty(lastDir) && Directory.Exists(lastDir))
                {
                    openFileDialog.InitialDirectory = lastDir;
                }
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var filePath = openFileDialog.FileName;
                    
                    // Save the directory for next time
                    var selectedDir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(selectedDir))
                    {
                        AppConfiguration.LastMediaDirectory = selectedDir;
                    }
                    
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
                        var currentTab = GetCurrentTab();
                        
                        // Get playlist information if available
                        List<string> playlistFiles = null;
                        int currentIndex = 0;
                        
                        if (currentTab?.Playlist != null && currentTab.Playlist.Count > 0)
                        {
                            playlistFiles = currentTab.Playlist.MediaFiles;
                            currentIndex = playlistFiles.IndexOf(filePath);
                            if (currentIndex < 0)
                            {
                                // File not in playlist - add it
                                currentTab.Playlist.AddFiles(filePath);
                                playlistFiles = currentTab.Playlist.MediaFiles;
                                currentIndex = playlistFiles.IndexOf(filePath);
                            }
                        }
                        
                        // Get current template path
                        var templatePath = BrowserStateManager.GetCurrentLayoutPath();
                        
                        // Create HTML player page with the media file - use full playlist player if available
                        var tempHtmlPath = LocalMediaHelper.CreateTemporaryPlayerFile(
                            filePath,
                            autoplay: true,  // Enable autoplay for user-initiated media file opens
                            loop: false,
                            playlistFiles: playlistFiles,
                            currentIndex: currentIndex,
                            templatePath: templatePath  // ✅ Pass the template path
                        );
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

        /// <summary>
        /// Opens a file dialog to select and play a media file in loop mode
        /// </summary>
        private void OpenMediaFileInLoop()
        {
            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Select Media File for Auto-Loop";
                openFileDialog.Filter = 
                    "All Media Files|*.mp4;*.webm;*.ogv;*.ogg;*.mp3;*.wav;*.aac;*.m4a;*.opus;*.flac|" +
                    "Video Files|*.mp4;*.webm;*.ogv;*.ogg|" +
                    "Audio Files|*.mp3;*.wav;*.aac;*.m4a;*.opus;*.flac;*.ogg|" +
                    "All Files|*.*";
                openFileDialog.FilterIndex = 1;
                
                // Use last media directory if available
                var lastDir = AppConfiguration.LastMediaDirectory;
                if (!string.IsNullOrEmpty(lastDir) && Directory.Exists(lastDir))
                {
                    openFileDialog.InitialDirectory = lastDir;
                }
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var filePath = openFileDialog.FileName;
                    
                    // Save the directory for next time
                    var selectedDir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(selectedDir))
                    {
                        AppConfiguration.LastMediaDirectory = selectedDir;
                    }
                    
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
                        // Get current template path
                        var templatePath = BrowserStateManager.GetCurrentLayoutPath();
                        
                        // Create HTML player page with the media file in LOOP mode
                        // Note: passing null for playlistFiles forces single-file player with loop enabled
                        var tempHtmlPath = LocalMediaHelper.CreateTemporaryPlayerFile(
                            filePath,
                            autoplay: true,  // Enable autoplay for user-initiated media file opens
                            loop: true,      // ✅ ENABLE LOOP MODE
                            playlistFiles: null,  // No playlist - single file mode
                            currentIndex: 0,
                            templatePath: templatePath
                        );
                        var url = LocalMediaHelper.FilePathToUrl(tempHtmlPath);
                        
                        // Navigate to the player
                        NavigateToUrl(url);
                        
                        // Optionally set a custom tab name to indicate loop mode
                        var currentTab = GetCurrentTab();
                        int selectedIndex = tabControl.SelectedIndex;
                        if (currentTab != null && selectedIndex >= 0 && selectedIndex < _tabCustomNames.Count)
                        {
                            var fileName = Path.GetFileNameWithoutExtension(filePath);
                            _tabCustomNames[selectedIndex] = $"{fileName} (Loop)";
                            currentTab.CustomName = $"{fileName} (Loop)";
                            tabControl.TabPages[selectedIndex].Text = $"{fileName} (Loop)";
                        }
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

        /// <summary>
        /// Opens a playlist from file or folder
        /// </summary>
        private void OpenPlaylist()
        {
            var result = MessageBox.Show(
                "Load playlist from:\n\n" +
                "YES - M3U Playlist File\n" +
                "NO - Folder (all media files)\n" +
                "CANCEL - Cancel",
                "Open Playlist",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question
            );

            var currentTab = GetCurrentTab();
            if (currentTab == null) return;

            List<string> mediaFiles = null;
            
            if (result == DialogResult.Yes)
            {
                // Load M3U file (don't save any directory information)
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Select M3U Playlist";
                    openFileDialog.Filter = "M3U Playlist (*.m3u;*.m3u8)|*.m3u;*.m3u8|All Files (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.InitialDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "DynamicBrowserPanels",
                        "Playlists"
                    );
                    openFileDialog.RestoreDirectory = true;

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        if (currentTab.Playlist.LoadFromM3U(openFileDialog.FileName))
                        {
                            mediaFiles = currentTab.Playlist.MediaFiles;
                            // Don't save directory when loading M3U playlists
                        }
                    }
                }
            }
            else if (result == DialogResult.No)
            {
                // Load from folder
                using (var folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Select folder containing media files";
                    folderDialog.ShowNewFolderButton = false;
                    
                    // Use last media directory if available
                    var lastDir = AppConfiguration.LastMediaDirectory;
                    if (!string.IsNullOrEmpty(lastDir) && Directory.Exists(lastDir))
                    {
                        folderDialog.SelectedPath = lastDir;
                    }

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        if (currentTab.Playlist.LoadFromFolder(folderDialog.SelectedPath))
                        {
                            mediaFiles = currentTab.Playlist.MediaFiles;
                            
                            // Save the folder directory for next time
                            AppConfiguration.LastMediaDirectory = folderDialog.SelectedPath;
                        }
                    }
                }
            }

            // If we successfully loaded a playlist, show it with the full playlist player
            if (mediaFiles != null && mediaFiles.Count > 0)
            {
                MessageBox.Show(
                    $"Loaded {mediaFiles.Count} media files",
                    "Playlist Loaded",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Get current template path
                var templatePath = BrowserStateManager.GetCurrentLayoutPath();

                // Create the full playlist player view
                var firstFile = mediaFiles[0];
                var tempHtmlPath = LocalMediaHelper.CreateTemporaryPlayerFile(
                    firstFile,
                    autoplay: true,
                    loop: false,
                    playlistFiles: mediaFiles,
                    currentIndex: 0,
                    templatePath: templatePath
                );
                var playerUrl = LocalMediaHelper.FilePathToUrl(tempHtmlPath);
                NavigateToUrl(playerUrl);
            }
        }

        /// <summary>
        /// Adds songs to the current playlist
        /// </summary>
        private void AddSongsToPlaylist()
        {
            var currentTab = GetCurrentTab();
            if (currentTab == null) return;

            using (var openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Title = "Add Songs to Playlist";
                openFileDialog.Filter = 
                    "All Media Files|*.mp4;*.webm;*.ogv;*.ogg;*.mp3;*.wav;*.aac;*.m4a;*.opus;*.flac|" +
                    "Video Files|*.mp4;*.webm;*.ogv;*.ogg|" +
                    "Audio Files|*.mp3;*.wav;*.aac;*.m4a;*.opus;*.flac;*.ogg|" +
                    "All Files|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.Multiselect = true;
                
                // Use last media directory if available
                var lastDir = AppConfiguration.LastMediaDirectory;
                if (!string.IsNullOrEmpty(lastDir) && Directory.Exists(lastDir))
                {
                    openFileDialog.InitialDirectory = lastDir;
                }
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    var filePaths = openFileDialog.FileNames;
                    
                    // Save the directory for next time (from first file)
                    if (filePaths.Length > 0)
                    {
                        var selectedDir = Path.GetDirectoryName(filePaths[0]);
                        if (!string.IsNullOrEmpty(selectedDir))
                        {
                            AppConfiguration.LastMediaDirectory = selectedDir;
                        }
                    }
                    
                    // Filter to only supported media files
                    var validFiles = new List<string>();
                    var invalidFiles = new List<string>();

                    foreach (var file in filePaths)
                    {
                        if (LocalMediaHelper.IsMediaSupported(file))
                        {
                            validFiles.Add(file);
                        }
                        else
                        {
                            invalidFiles.Add(Path.GetFileName(file));
                        }
                    }

                    // Show warning if some files were invalid
                    if (invalidFiles.Count > 0)
                    {
                        var message = $"The following {invalidFiles.Count} file(s) are not supported and will be skipped:\n\n";
                        message += string.Join("\n", invalidFiles.Take(10));
                        if (invalidFiles.Count > 10)
                        {
                            message += $"\n... and {invalidFiles.Count - 10} more";
                        }

                        MessageBox.Show(
                            message,
                            "Unsupported Files",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                    }

                    // Add valid files to playlist
                    if (validFiles.Count > 0)
                    {
                        // Store the current playlist count BEFORE adding
                        int previousPlaylistCount = currentTab.Playlist.Count;
                        
                        // Add files to playlist FIRST
                        currentTab.Playlist.AddFiles(validFiles.ToArray());

                        MessageBox.Show(
                            $"Added {validFiles.Count} song(s) to playlist.\n\n" +
                            $"Total songs in playlist: {currentTab.Playlist.Count}",
                            "Songs Added",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );

                        // Check if we're currently on an ACTIVE media player page (not a restored media:/// URL)
                        bool isActivelyPlayingMedia = !string.IsNullOrEmpty(currentTab.CurrentUrl) && 
                                                       LocalMediaHelper.IsTempMediaPlayerUrl(currentTab.CurrentUrl);
                        
                        // Only reload the player if:
                        // 1. We're currently ACTIVELY playing media (temp player URL)
                        // OR
                        // 2. The playlist was empty before adding (new playlist scenario)
                        if (isActivelyPlayingMedia || previousPlaylistCount == 0)
                        {
                            // Get the current file
                            string fileToPlay = currentTab.Playlist.CurrentFile;
                            int indexToPlay = currentTab.Playlist.CurrentIndex;
                            
                            // If current file doesn't exist or is null AND we had an empty playlist, start from beginning
                            if ((string.IsNullOrEmpty(fileToPlay) || !File.Exists(fileToPlay)) && previousPlaylistCount == 0)
                            {
                                // Jump to first file only for NEW playlists
                                fileToPlay = currentTab.Playlist.JumpTo(0);
                                indexToPlay = 0;
                            }
                            else if (string.IsNullOrEmpty(fileToPlay) || !File.Exists(fileToPlay))
                            {
                                // For some reason current file is invalid - try first file
                                fileToPlay = currentTab.Playlist.JumpTo(0);
                                indexToPlay = 0;
                            }
                            
                            if (fileToPlay != null && File.Exists(fileToPlay))
                            {
                                // Determine if we should autoplay
                                // Autoplay if:
                                // - We're currently playing media (adding to active playlist)
                                // - OR we had an empty playlist (new playlist scenario)
                                bool shouldAutoplay = isActivelyPlayingMedia || previousPlaylistCount == 0;
                                
                                // Get current template path
                                var templatePath = BrowserStateManager.GetCurrentLayoutPath();
                                
                                // Recreate the playlist player with the updated playlist
                                var tempHtmlPath = LocalMediaHelper.CreateTemporaryPlayerFile(
                                    fileToPlay,
                                    autoplay: shouldAutoplay,
                                    loop: false,
                                    playlistFiles: currentTab.Playlist.MediaFiles,
                                    currentIndex: indexToPlay,
                                    templatePath: templatePath  // ✅ Pass the template path
                                );
                                var playerUrl = LocalMediaHelper.FilePathToUrl(tempHtmlPath);
                                NavigateToUrl(playerUrl);
                            }
                        }
                    }
                    else if (invalidFiles.Count > 0)
                    {
                        MessageBox.Show(
                            "No valid media files were selected.",
                            "No Files Added",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Removes the current song from the playlist (works with both local and online)
        /// </summary>
        private void RemoveCurrentSong()
        {
            var currentTab = GetCurrentTab();
            if (currentTab == null) return;

            var playlistType = currentTab.CurrentPlaylistType;

            if (playlistType == PlaylistType.Local)
            {
                RemoveCurrentLocalSong();
            }
            else if (playlistType == PlaylistType.Online)
            {
                RemoveCurrentOnlineSong();
            }
            else
            {
                MessageBox.Show(
                    "No playlist loaded or playlist is empty.",
                    "Remove Song",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        /// <summary>
        /// Shows the playlist viewer (works with both local and online)
        /// </summary>
        private void ShowPlaylistViewer()
        {
            var currentTab = GetCurrentTab();
            if (currentTab == null) return;

            var playlistType = currentTab.CurrentPlaylistType;

            if (playlistType == PlaylistType.Local)
            {
                ShowLocalPlaylistViewer();
            }
            else if (playlistType == PlaylistType.Online)
            {
                ShowOnlinePlaylistViewer();
            }
            else
            {
                MessageBox.Show(
                    "No playlist loaded",
                    "Playlist",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        /// <summary>
        /// Saves the current playlist (works with both local and online)
        /// </summary>
        private void SavePlaylist()
        {
            var currentTab = GetCurrentTab();
            if (currentTab == null) return;

            var playlistType = currentTab.CurrentPlaylistType;

            if (playlistType == PlaylistType.Local)
            {
                SaveLocalPlaylist();
            }
            else if (playlistType == PlaylistType.Online)
            {
                SaveOnlinePlaylist();
            }
            else
            {
                MessageBox.Show(
                    "No playlist to save",
                    "Playlist",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        // Rename existing methods to be specific
        private void RemoveCurrentLocalSong()
        {
            var currentTab = GetCurrentTab();
            if (currentTab?.Playlist == null || currentTab.Playlist.Count == 0)
            {
                MessageBox.Show(
                    "No playlist loaded or playlist is empty.",
                    "Remove Song",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            int currentIndex = currentTab.Playlist.CurrentIndex;
            
            if (currentIndex < 0 || currentIndex >= currentTab.Playlist.MediaFiles.Count)
            {
                MessageBox.Show(
                    "No song is currently selected.",
                    "Remove Song",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            var currentFile = currentTab.Playlist.MediaFiles[currentIndex];
            var fileName = Path.GetFileName(currentFile);

            if (currentTab.Playlist.Count == 1)
            {
                currentTab.Playlist.RemoveFile(currentIndex);
                NavigateTabToUrl(currentTab, HomeUrl);
                
                MessageBox.Show(
                    $"Removed: {fileName}\n\nPlaylist is now empty.",
                    "Song Removed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                string nextFile = null;
                
                if (currentIndex < currentTab.Playlist.MediaFiles.Count - 1)
                {
                    nextFile = currentTab.Playlist.MediaFiles[currentIndex + 1];
                }
                else
                {
                    if (currentTab.Playlist.Repeat)
                    {
                        nextFile = currentTab.Playlist.MediaFiles[0];
                    }
                    else if (currentIndex > 0)
                    {
                        nextFile = currentTab.Playlist.MediaFiles[currentIndex - 1];
                    }
                }

                currentTab.Playlist.RemoveFile(currentIndex);

                if (nextFile != null)
                {
                    int newIndex = currentTab.Playlist.MediaFiles.IndexOf(nextFile);
                    if (newIndex >= 0)
                    {
                        currentTab.Playlist.JumpTo(newIndex);
                        NavigateTabToUrl(currentTab, "media:///" + nextFile);
                    }
                }

                MessageBox.Show(
                    $"Removed: {fileName}\n\n" +
                    $"Remaining songs: {currentTab.Playlist.Count}",
                    "Song Removed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        private void RemoveCurrentOnlineSong()
        {
            var currentTab = GetCurrentTab();
            if (currentTab?.OnlinePlaylist == null || currentTab.OnlinePlaylist.Count == 0)
            {
                MessageBox.Show(
                    "No online playlist loaded or playlist is empty.",
                    "Remove Item",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            int currentIndex = currentTab.OnlinePlaylist.CurrentIndex;
            
            if (currentIndex < 0 || currentIndex >= currentTab.OnlinePlaylist.MediaItems.Count)
            {
                MessageBox.Show(
                    "No item is currently selected.",
                    "Remove Item",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            var currentItem = currentTab.OnlinePlaylist.MediaItems[currentIndex];

            if (currentTab.OnlinePlaylist.Count == 1)
            {
                currentTab.OnlinePlaylist.RemoveItem(currentIndex);
                NavigateTabToUrl(currentTab, HomeUrl);
                
                MessageBox.Show(
                    $"Removed: {currentItem.DisplayName}\n\nPlaylist is now empty.",
                    "Item Removed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
            else
            {
                currentTab.OnlinePlaylist.RemoveItem(currentIndex);

                // Reload the playlist player
                ShowOnlinePlaylistPlayer();

                MessageBox.Show(
                    $"Removed: {currentItem.DisplayName}\n\n" +
                    $"Remaining items: {currentTab.OnlinePlaylist.Count}",
                    "Item Removed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
            }
        }

        private void ShowLocalPlaylistViewer()
        {
            var currentTab = GetCurrentTab();
            if (currentTab?.Playlist == null || currentTab.Playlist.Count == 0)
            {
                MessageBox.Show("No playlist loaded", "Playlist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new Form())
            {
                form.Text = "Local Playlist";
                form.Size = new Size(600, 400);
                form.StartPosition = FormStartPosition.CenterParent;

                var listBox = new ListBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 10)
                };

                for (int i = 0; i < currentTab.Playlist.MediaFiles.Count; i++)
                {
                    var file = currentTab.Playlist.MediaFiles[i];
                    var prefix = i == currentTab.Playlist.CurrentIndex ? "▶ " : "   ";
                    listBox.Items.Add($"{prefix}{Path.GetFileName(file)}");
                }

                listBox.SelectedIndex = currentTab.Playlist.CurrentIndex;
                listBox.DoubleClick += (s, e) =>
                {
                    if (listBox.SelectedIndex >= 0)
                    {
                        var file = currentTab.Playlist.JumpTo(listBox.SelectedIndex);
                        if (file != null)
                        {
                            NavigateTabToUrl(currentTab, "media:///" + file);
                            form.Close();
                        }
                    }
                };

                form.Controls.Add(listBox);
                form.ShowDialog();
            }
        }

        private void ShowOnlinePlaylistViewer()
        {
            var currentTab = GetCurrentTab();
            if (currentTab?.OnlinePlaylist == null || currentTab.OnlinePlaylist.Count == 0)
            {
                MessageBox.Show("No online playlist loaded", "Playlist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var form = new Form())
            {
                form.Text = "Online Playlist";
                form.Size = new Size(600, 400);
                form.StartPosition = FormStartPosition.CenterParent;

                var listBox = new ListBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Consolas", 10)
                };

                for (int i = 0; i < currentTab.OnlinePlaylist.MediaItems.Count; i++)
                {
                    var item = currentTab.OnlinePlaylist.MediaItems[i];
                    var prefix = i == currentTab.OnlinePlaylist.CurrentIndex ? "▶ " : "   ";
                    listBox.Items.Add($"{prefix}{item.DisplayName} ({item.MediaType})");
                }

                listBox.SelectedIndex = currentTab.OnlinePlaylist.CurrentIndex;
                listBox.DoubleClick += (s, e) =>
                {
                    if (listBox.SelectedIndex >= 0)
                    {
                        currentTab.OnlinePlaylist.JumpTo(listBox.SelectedIndex);
                        ShowOnlinePlaylistPlayer();
                        form.Close();
                    }
                };

                form.Controls.Add(listBox);
                form.ShowDialog();
            }
        }

        private void SaveLocalPlaylist()
        {
            var currentTab = GetCurrentTab();
            if (currentTab?.Playlist == null || currentTab.Playlist.Count == 0)
            {
                MessageBox.Show("No playlist to save", "Playlist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "M3U Playlist (*.m3u)|*.m3u";
                saveFileDialog.FileName = "playlist.m3u";
                saveFileDialog.InitialDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DynamicBrowserPanels",
                    "Playlists"
                );
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    currentTab.Playlist.SaveToM3U(saveFileDialog.FileName);
                    MessageBox.Show("Playlist saved!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void SaveOnlinePlaylist()
        {
            var currentTab = GetCurrentTab();
            if (currentTab?.OnlinePlaylist == null || currentTab.OnlinePlaylist.Count == 0)
            {
                MessageBox.Show("No online playlist to save", "Playlist", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (var saveFileDialog = new SaveFileDialog())
            {
                // JSON is the recommended format for online playlists (machine-agnostic, syncs via Dropbox)
                // M3U format is also supported for compatibility
                saveFileDialog.Filter = "JSON Playlist (*.json)|*.json|M3U Playlist (*.m3u)|*.m3u";
                saveFileDialog.FilterIndex = 1; // Default to JSON
                saveFileDialog.FileName = "online_playlist.json";
                saveFileDialog.InitialDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DynamicBrowserPanels",
                    "Playlists"
                );
                saveFileDialog.RestoreDirectory = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (saveFileDialog.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        {
                            // Save as JSON (recommended for online playlists)
                            currentTab.OnlinePlaylist.SaveToFile(saveFileDialog.FileName);
                        }
                        else
                        {
                            // Save as M3U (compatibility format)
                            SaveOnlinePlaylistAsM3U(saveFileDialog.FileName);
                        }
                        
                        MessageBox.Show(
                            "Online playlist saved!\n\n" +
                            "JSON playlists are machine-agnostic and ideal for syncing via Dropbox.",
                            "Success",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(
                            $"Error saving playlist:\n{ex.Message}",
                            "Save Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
            }
        }

        private void SaveOnlinePlaylistAsM3U(string filePath)
        {
            var currentTab = GetCurrentTab();
            if (currentTab?.OnlinePlaylist == null) return;

            var lines = new List<string> { "#EXTM3U" };
            
            foreach (var item in currentTab.OnlinePlaylist.MediaItems)
            {
                // Add EXTINF line with duration and title
                var duration = item.DurationSeconds ?? -1;
                lines.Add($"#EXTINF:{duration},{item.DisplayName}");
                lines.Add(item.Url);
            }

            File.WriteAllLines(filePath, lines);
        }
    }
}