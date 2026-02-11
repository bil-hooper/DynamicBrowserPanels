using System;
using System.Windows.Forms;
using static Dropbox.Api.TeamLog.EventCategory;
using static Dropbox.Api.TeamLog.GroupJoinPolicy;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;

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
                new ToolStripMenuItem("📂 Open Local Playlist...", null, (s, e) => OpenPlaylist()),
                new ToolStripMenuItem("➕ Add Local Songs...", null, (s, e) => AddSongsToPlaylist()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("🌐 Open Online Playlist...", null, (s, e) => OpenOnlinePlaylist()),
                new ToolStripMenuItem("➕ Add Online URL...", null, (s, e) => AddSingleOnlineItem()),
                new ToolStripMenuItem("➕ Add Multiple URLs...", null, (s, e) => AddBulkOnlineUrls()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("🗑️ Remove Current Song", null, (s, e) => RemoveCurrentSong()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("📋 Show Playlist...", null, (s, e) => ShowPlaylistViewer()),
                new ToolStripMenuItem("💾 Save Playlist...", null, (s, e) => SavePlaylist())
            });

            mnuOpenNotepad = new ToolStripMenuItem("📝 Open Notepad");
            mnuOpenNotepad.Click += (s, e) => OpenNotepad();

            separator1b = new ToolStripSeparator();
            
            // Timer menu
            mnuTimer = new ToolStripMenuItem("⏱ Timer");
            mnuTimer.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("⏱ 5 Minutes", null, (s, e) => SetTimer(TimeSpan.FromMinutes(5))),
                new ToolStripMenuItem("⏱ 10 Minutes", null, (s, e) => SetTimer(TimeSpan.FromMinutes(10))),
                new ToolStripMenuItem("⏱ 15 Minutes", null, (s, e) => SetTimer(TimeSpan.FromMinutes(15))),
                new ToolStripMenuItem("⏱ 20 Minutes", null, (s, e) => SetTimer(TimeSpan.FromMinutes(20))),
                new ToolStripMenuItem("⏱ 25 Minutes", null, (s, e) => SetTimer(TimeSpan.FromMinutes(25))),
                new ToolStripMenuItem("⏱ 30 Minutes", null, (s, e) => SetTimer(TimeSpan.FromMinutes(30))),
                new ToolStripMenuItem("⏱ 1 Hour", null, (s, e) => SetTimer(TimeSpan.FromHours(1))),
                new ToolStripSeparator(),
                new ToolStripMenuItem("⏱ Custom...", null, (s, e) => SetCustomTimer()),
                new ToolStripSeparator(),
                new ToolStripMenuItem("⏹ Stop Timer", null, (s, e) => StopTimer())
            });
            
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
            
            mnuMoveTabToStart = new ToolStripMenuItem("⇤ Move Tab to Start");
            mnuMoveTabToStart.Click += (s, e) => MoveTabToStart();
            
            mnuMoveTabToEnd = new ToolStripMenuItem("Move Tab to End ⇥");
            mnuMoveTabToEnd.Click += (s, e) => MoveTabToEnd();
            
            separator2 = new ToolStripSeparator();
            
            mnuSplitHorizontal = new ToolStripMenuItem("Split Horizontal ⬌");
            mnuSplitHorizontal.Click += (s, e) => OnSplitRequested(Orientation.Horizontal);
            
            mnuSplitVertical = new ToolStripMenuItem("Split Vertical ⬍");
            mnuSplitVertical.Click += (s, e) => OnSplitRequested(Orientation.Vertical);
            
            separator3 = new ToolStripSeparator();
            
            mnuSaveLayoutDirect = new ToolStripMenuItem("💾 Save Layout");
            mnuSaveLayoutDirect.Click += (s, e) => SaveLayoutDirectRequested?.Invoke(this, EventArgs.Empty);
            
            mnuSaveLayoutAs = new ToolStripMenuItem("💾 Save Layout As...");
            mnuSaveLayoutAs.Click += (s, e) => SaveLayoutAsRequested?.Invoke(this, EventArgs.Empty);
            
            mnuLoadLayout = new ToolStripMenuItem("📂 Load Layout...");
            mnuLoadLayout.Click += (s, e) => LoadLayoutRequested?.Invoke(this, EventArgs.Empty);
            
            separator4 = new ToolStripSeparator();
            
            mnuResetLayout = new ToolStripMenuItem("Reset Layout");
            mnuResetLayout.Click += (s, e) => ResetLayoutRequested?.Invoke(this, EventArgs.Empty);

            // Add password management option
            mnuManagePasswords = new ToolStripMenuItem("🔑 Manage Passwords");
            mnuManagePasswords.Click += (s, e) => OpenPasswordManager();

            // Add Dropbox sync option
            mnuDropboxSync = new ToolStripMenuItem("☁️ Dropbox Sync...");
            mnuDropboxSync.Click += (s, e) => OpenDropboxSync();

            // Add installation options
            mnuInstall = new ToolStripMenuItem("⚙️ Install Application...");
            mnuInstall.Click += (s, e) => InstallationManager.Install();
            
            mnuUninstall = new ToolStripMenuItem("🗑️ Uninstall Application...");
            mnuUninstall.Click += (s, e) => InstallationManager.Uninstall();

            contextMenu.Items.AddRange(new ToolStripItem[]
            {
                mnuBack, 
                mnuForward, 
                mnuRefresh, 
                mnuHome,
                separator1,
                mnuSaveLayoutDirect, 
                mnuSaveLayoutAs, 
                mnuLoadLayout,
                mnuResetLayout,
                separator4,
                mnuNewTab, 
                mnuCloseTab, 
                mnuRenameTab,
                mnuMoveTabLeft, 
                mnuMoveTabRight,
                mnuMoveTabToStart, 
                mnuMoveTabToEnd,
                new ToolStripSeparator(),
                mnuSplitHorizontal, 
                mnuSplitVertical,
                separator3,
                mnuOpenNotepad,
                separator1b,
                mnuOpenMedia,
                mnuPlaylistControls,
                new ToolStripSeparator(),
                mnuTimer,
                separator2,
                mnuManagePasswords,
                mnuDropboxSync,
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
            
            int selectedIndex = tabControl.SelectedIndex;
            int tabCount = _browserTabs.Count;
            
            // Enable move left if not at the leftmost position
            mnuMoveTabLeft.Enabled = selectedIndex > 0;
            
            // Enable move right if not at the rightmost position
            mnuMoveTabRight.Enabled = selectedIndex >= 0 && selectedIndex < tabCount - 1;
            
            // Enable move to start if not already at the start
            mnuMoveTabToStart.Enabled = selectedIndex > 0;
            
            // Enable move to end if not already at the end
            mnuMoveTabToEnd.Enabled = selectedIndex >= 0 && selectedIndex < tabCount - 1;

            // Enable "Save Layout" only when in session mode (file is loaded)
            bool isSessionMode = BrowserStateManager.IsSessionMode;
            mnuSaveLayoutDirect.Enabled = isSessionMode;
            
            // Update text to show filename if in session mode
            if (isSessionMode && !string.IsNullOrEmpty(BrowserStateManager.SessionFileName))
            {
                mnuSaveLayoutDirect.Text = $"💾 Save Layout ({BrowserStateManager.SessionFileName})";
            }
            else
            {
                mnuSaveLayoutDirect.Text = "💾 Save Layout";
            }

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
        /// Opens the Dropbox sync settings
        /// </summary>
        private void OpenDropboxSync()
        {
            try
            {
                using (var syncForm = new DropboxSyncForm())
                {
                    syncForm.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open Dropbox sync settings:\n{ex.Message}",
                    "Dropbox Sync Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Sets a timer with the specified duration
        /// </summary>
        private void SetTimer(TimeSpan duration)
        {
            TimerRequested?.Invoke(this, duration);
        }

        /// <summary>
        /// Shows a dialog to set a custom timer
        /// </summary>
        private void SetCustomTimer()
        {
            using (var dialog = new TimerInputDialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    SetTimer(dialog.TimerDuration);
                }
            }
        }

        /// <summary>
        /// Stops the active timer
        /// </summary>
        private void StopTimer()
        {
            TimerStopRequested?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Opens the notepad
        /// </summary>
        private void OpenNotepad()
        {
            try
            {
                var currentTab = GetCurrentTab();
                if (currentTab == null) return;
                
                // Create the notepad HTML file
                var htmlPath = NotepadHelper.CreateNotepadHtml();
                
                // Navigate to notepad #1 by default
                var url = LocalMediaHelper.FilePathToUrl(htmlPath) + "?note=1";
                NavigateToUrl(url);
                
                // Set custom tab name
                int selectedIndex = tabControl.SelectedIndex;
                if (selectedIndex >= 0 && selectedIndex < _tabCustomNames.Count)
                {
                    _tabCustomNames[selectedIndex] = "Notepad";
                    currentTab.CustomName = "Notepad";
                    tabControl.TabPages[selectedIndex].Text = "Notepad";
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