using System;
using System.Windows.Forms;
using static Dropbox.Api.TeamLog.EventCategory;
using static Dropbox.Api.TeamLog.GroupJoinPolicy;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.StartPanel;
using System.IO;

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
            
            mnuOpenMedia = new ToolStripMenuItem("📁 Open Media File...");
            mnuOpenMedia.Click += (s, e) => OpenMediaFile();
            
            // Add new menu item for loop mode
            mnuOpenMediaLoop = new ToolStripMenuItem("🔁 Open Media File in Auto-Loop...");
            mnuOpenMediaLoop.Click += (s, e) => OpenMediaFileInLoop();

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

            mnuOpenImagePad = new ToolStripMenuItem("🖼️ Open Image Pad");
            mnuOpenImagePad.Click += (s, e) => OpenImagePad();
            
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
                CreateAutoRepeatMenuItem(), // Add auto-repeat toggle
                new ToolStripSeparator(),
                new ToolStripMenuItem("⏹ Stop Timer", null, (s, e) => StopTimer())
            });
            
            // Keep Awake menu item
            mnuKeepAwake = new ToolStripMenuItem("☕ Keep Awake");
            mnuKeepAwake.CheckOnClick = true;
            mnuKeepAwake.Click += (s, e) => ToggleKeepAwake();
            
            // History menu
            mnuHistory = new ToolStripMenuItem("📜 History");
            mnuHistory.DropDownItems.AddRange(new ToolStripItem[]
            {
                new ToolStripMenuItem("📂 Open History Folder", null, (s, e) => OpenHistoryFolder()),
                new ToolStripMenuItem("🗑️ Clear History", null, (s, e) => ClearHistory())
            });
            
            mnuNewTab = new ToolStripMenuItem("+ New Tab");
            mnuNewTab.Click += async (s, e) => await AddNewTab();

            var mnuNewIncognitoTab = new ToolStripMenuItem("🕶️ New Incognito Tab");
            mnuNewIncognitoTab.Click += async (s, e) => await AddNewIncognitoTab();
            
            mnuCloseTab = new ToolStripMenuItem("✕ Close Tab");
            mnuCloseTab.Click += (s, e) => CloseCurrentTab();
            
            mnuRenameTab = new ToolStripMenuItem("✎ Rename Tab...");
            mnuRenameTab.Click += (s, e) => RenameCurrentTab();

            mnuMuteTab = new ToolStripMenuItem("🔇 Mute Tab");
            mnuMuteTab.Click += (s, e) => MuteCurrentTab();

            mnuUnmuteTab = new ToolStripMenuItem("🔊 Unmute Tab");
            mnuUnmuteTab.Click += (s, e) => UnmuteCurrentTab();

            // Add tab privacy lock menu items
            mnuLockTab = new ToolStripMenuItem("🔒 Lock Tab");
            mnuLockTab.Click += (s, e) => LockCurrentTab();
            
            mnuUnlockTab = new ToolStripMenuItem("🔓 Unlock Tab");
            mnuUnlockTab.Click += (s, e) => UnlockCurrentTab();
            
            mnuMoveTabLeft = new ToolStripMenuItem("← Move Tab Left");
            mnuMoveTabLeft.Click += (s, e) => MoveTabLeft();
            
            mnuMoveTabRight = new ToolStripMenuItem("Move Tab Right →");
            mnuMoveTabRight.Click += (s, e) => MoveTabRight();
            
            mnuMoveTabToStart = new ToolStripMenuItem("⇤ Move Tab to Start");
            mnuMoveTabToStart.Click += (s, e) => MoveTabToStart();
            
            mnuMoveTabToEnd = new ToolStripMenuItem("Move Tab to End ⇥");
            mnuMoveTabToEnd.Click += (s, e) => MoveTabToEnd();
            
            mnuSplitHorizontal = new ToolStripMenuItem("Split Horizontal ⬌");
            mnuSplitHorizontal.Click += (s, e) => OnSplitRequested(Orientation.Horizontal);
            
            mnuSplitVertical = new ToolStripMenuItem("Split Vertical ⬍");
            mnuSplitVertical.Click += (s, e) => OnSplitRequested(Orientation.Vertical);
            
            mnuSaveLayoutDirect = new ToolStripMenuItem("💾 Save Layout");
            mnuSaveLayoutDirect.Click += (s, e) => SaveLayoutDirectRequested?.Invoke(this, EventArgs.Empty);
            
            mnuSaveLayoutAs = new ToolStripMenuItem("💾 Save Layout As...");
            mnuSaveLayoutAs.Click += (s, e) => SaveLayoutAsRequested?.Invoke(this, EventArgs.Empty);
            
            // Add password-protected template menu items
            mnuSaveProtectedTemplate = new ToolStripMenuItem("🔐 Save Password-Protected Template...");
            mnuSaveProtectedTemplate.Click += (s, e) => SaveProtectedTemplateRequested?.Invoke(this, EventArgs.Empty);
            
            mnuRemoveTemplateProtection = new ToolStripMenuItem("🔓 Remove Template Protection...");
            mnuRemoveTemplateProtection.Click += (s, e) => RemoveTemplateProtectionRequested?.Invoke(this, EventArgs.Empty);
            
            mnuLoadLayout = new ToolStripMenuItem("📂 Load Layout...");
            mnuLoadLayout.Click += (s, e) => LoadLayoutRequested?.Invoke(this, EventArgs.Empty);
            
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
                new ToolStripSeparator(),
                mnuSaveLayoutDirect, 
                mnuSaveLayoutAs, 
                mnuSaveProtectedTemplate,
                mnuRemoveTemplateProtection,
                mnuLoadLayout,
                mnuResetLayout,
                new ToolStripSeparator(),
                mnuNewTab,
                mnuNewIncognitoTab,
                mnuCloseTab, 
                mnuRenameTab,
                mnuLockTab,
                mnuUnlockTab,
                mnuMuteTab,
                mnuUnmuteTab,
                mnuMoveTabLeft, 
                mnuMoveTabRight,
                mnuMoveTabToStart, 
                mnuMoveTabToEnd,
                new ToolStripSeparator(),
                mnuSplitHorizontal, 
                mnuSplitVertical,
                new ToolStripSeparator(),
                mnuHistory,
                new ToolStripSeparator(),
                mnuOpenNotepad,
                mnuOpenImagePad,
                new ToolStripSeparator(),
                mnuOpenMedia,
                mnuOpenMediaLoop,
                mnuPlaylistControls,
                new ToolStripSeparator(),
                mnuTimer,
                mnuKeepAwake,
                new ToolStripSeparator(),
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

            bool tabIsMuted = currentTab?.IsMuted ?? false;
            mnuMuteTab.Visible = !tabIsMuted;
            mnuUnmuteTab.Visible = tabIsMuted;

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
            
            // Update tab lock/unlock menu items based on locked state
            int selectedIndex = tabControl.SelectedIndex;
            bool tabIsLocked = selectedIndex >= 0 && _lockedTabs.Contains(selectedIndex);
            mnuLockTab.Visible = !tabIsLocked;
            mnuUnlockTab.Visible = tabIsLocked;
            
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
            
            // Enable "Remove Template Protection" only when in session mode
            mnuRemoveTemplateProtection.Enabled = isSessionMode;
            
            // Update text to show filename if in session mode
            if (isSessionMode && !string.IsNullOrEmpty(BrowserStateManager.SessionFileName))
            {
                mnuSaveLayoutDirect.Text = $"💾 Save Layout ({BrowserStateManager.SessionFileName})";
            }
            else
            {
                mnuSaveLayoutDirect.Text = "💾 Save Layout";
            }

            // Update Keep Awake checked state
            if (mnuKeepAwake != null)
            {
                mnuKeepAwake.Checked = KeepAwakeManager.IsEnabled;
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

        /// <summary>
        /// Opens the image pad
        /// </summary>
        private void OpenImagePad()
        {
            try
            {
                var currentTab = GetCurrentTab();
                if (currentTab == null) return;
                
                // Create the image pad HTML file
                var htmlPath = ImagePadHelper.CreateImagePadHtml();
                
                // Navigate to image #1 by default
                var url = LocalMediaHelper.FilePathToUrl(htmlPath) + "?image=1";
                NavigateToUrl(url);
                
                // Set custom tab name
                int selectedIndex = tabControl.SelectedIndex;
                if (selectedIndex >= 0 && selectedIndex < _tabCustomNames.Count)
                {
                    _tabCustomNames[selectedIndex] = "Image Pad";
                    currentTab.CustomName = "Image Pad";
                    tabControl.TabPages[selectedIndex].Text = "Image Pad";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open image pad: {ex.Message}",
                    "Image Pad Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Opens the URL history folder in Windows Explorer
        /// </summary>
        private void OpenHistoryFolder()
        {
            try
            {
                string historyPath = UrlHistoryManager.GetHistoryFolderPath();
                
                // Ensure folder exists before opening
                if (!Directory.Exists(historyPath))
                {
                    Directory.CreateDirectory(historyPath);
                }
                
                System.Diagnostics.Process.Start("explorer.exe", historyPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open history folder: {ex.Message}", "Error", 
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Clears the URL history with confirmation
        /// </summary>
        private void ClearHistory()
        {
            var result = MessageBox.Show(
                "Are you sure you want to clear all URL history?\n\n" +
                "Click Yes if you want to delete all history for all time,\n\n" +
                " or click No if you want to delete only today's history.\n\n",
                "Clear History",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                UrlHistoryManager.ClearHistory();
                MessageBox.Show("History cleared successfully.", "History", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (result == DialogResult.No)
            {
                UrlHistoryManager.ClearTodaysHistory();
                MessageBox.Show("Today's history cleared successfully.", "History",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}