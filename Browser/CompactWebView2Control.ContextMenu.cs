using System;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    public partial class CompactWebView2Control
    {
        /// <summary>
        /// Creates and configures the context menu
        /// </summary>
        private void CreateContextMenu()
        {
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

            mnuPlaylistControls = new ToolStripMenuItem("🎵 Playlist");
            mnuPlaylistControls.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("📂 Open Playlist...", null, (s, e) => OpenPlaylist()),
                new ToolStripMenuItem("➕ Add Songs...", null, (s, e) => AddSongsToPlaylist()),
                new ToolStripMenuItem("🗑️ Remove Current Song", null, (s, e) => RemoveCurrentSong()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("📋 Show Playlist...", null, (s, e) => ShowPlaylistViewer()),
                new ToolStripMenuItem("💾 Save Playlist...", null, (s, e) => SavePlaylist())
            });

            separator1b = new ToolStripSeparator();
            
            mnuOpenNotepad = new ToolStripMenuItem("📝 Open Notepad");
            mnuOpenNotepad.Click += (s, e) => OpenNotepad();
            
            new ToolStripSeparator();
            
            mnuNewTab = new ToolStripMenuItem("+ New Tab");
            mnuNewTab.Click += async (s, e) => await AddNewTab();
            
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
                mnuPlaylistControls,
                separator1b,
                mnuOpenNotepad,
                new ToolStripSeparator(),
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
        }

        /// <summary>
        /// Handles context menu opening event
        /// </summary>
        private void ContextMenu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            UpdateContextMenuButtons();
        }

        /// <summary>
        /// Updates the enabled/visible state of context menu items
        /// </summary>
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

        /// <summary>
        /// Opens the password manager
        /// </summary>
        private void OpenPasswordManager()
        {
            NavigateToUrl("https://passwords.google.com");
        }

        /// <summary>
        /// Opens the notepad in the current tab
        /// </summary>
        private void OpenNotepad()
        {
            try
            {
                var currentTab = GetCurrentTab();
                if (currentTab == null) return;
                
                // Get the next unique instance number
                int instanceNumber = NotepadHelper.GetNextNotepadInstance();
                
                // Store the instance number in the tab
                currentTab.NotepadInstance = instanceNumber;
                
                // Load saved notepad content for this instance
                var notepadData = NotepadManager.LoadNotepad(instanceNumber);
                
                // Create HTML file with the content
                var htmlPath = NotepadHelper.CreateNotepadHtml(notepadData.Content, instanceNumber);
                
                // Navigate to the notepad
                var url = LocalMediaHelper.FilePathToUrl(htmlPath);
                NavigateToUrl(url);
                
                // Set custom tab name
                int selectedIndex = tabControl.SelectedIndex;
                if (selectedIndex >= 0 && selectedIndex < _tabCustomNames.Count)
                {
                    _tabCustomNames[selectedIndex] = $"Notepad #{instanceNumber}";
                    currentTab.CustomName = $"Notepad #{instanceNumber}";
                    tabControl.TabPages[selectedIndex].Text = $"Notepad #{instanceNumber}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open notepad: {ex.Message}",
                    "Notepad Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }
    }
}