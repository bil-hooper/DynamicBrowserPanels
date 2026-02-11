using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Form for configuring Dropbox synchronization
    /// </summary>
    public class DropboxSyncForm : Form
    {
        private CheckBox enableSyncCheckBox;
        private CheckBox syncNotesCheckBox;
        private CheckBox syncPlaylistsCheckBox;
        private CheckBox syncTemplatesCheckBox;
        private TextBox accessTokenTextBox;
        private TextBox appKeyTextBox;
        private TextBox appSecretTextBox;
        private Button authenticateButton;
        private Button testConnectionButton;
        private Button revokeAccessButton;
        private Button syncNowButton;
        private Button pushButton;
        private Button pullButton;
        private Button helpButton;
        private Button saveButton;
        private Button cancelButton;
        private Label statusLabel;
        private Label lastSyncLabel;
        private ProgressBar syncProgressBar;

        private DropboxSyncSettings settings;

        public DropboxSyncForm()
        {
            InitializeComponent();
            LoadSettings();
            UpdateUIState();
        }

        private void InitializeComponent()
        {
            Text = "Dropbox Synchronization Settings";
            Size = new Size(600, 620);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Help button (top-right, like a "?" button)
            helpButton = new Button
            {
                Text = "?",
                Location = new Point(540, 10),
                Size = new Size(30, 30),
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            helpButton.FlatAppearance.BorderColor = Color.FromArgb(0, 97, 255);
            helpButton.Click += HelpButton_Click;

            // Enable Sync Checkbox
            enableSyncCheckBox = new CheckBox
            {
                Text = "Enable Dropbox Synchronization",
                Location = new Point(20, 20),
                Size = new Size(250, 24),
                Font = new Font(Font, FontStyle.Bold)
            };
            enableSyncCheckBox.CheckedChanged += EnableSyncCheckBox_CheckedChanged;

            // Sync Options Group
            var syncOptionsGroup = new GroupBox
            {
                Text = "Sync Folders",
                Location = new Point(20, 50),
                Size = new Size(540, 100)
            };

            syncNotesCheckBox = new CheckBox
            {
                Text = "Notes",
                Location = new Point(20, 25),
                Size = new Size(150, 24)
            };

            syncPlaylistsCheckBox = new CheckBox
            {
                Text = "Playlists",
                Location = new Point(20, 55),
                Size = new Size(150, 24)
            };

            syncTemplatesCheckBox = new CheckBox
            {
                Text = "Templates",
                Location = new Point(200, 25),
                Size = new Size(150, 24)
            };

            syncOptionsGroup.Controls.AddRange(new Control[] { syncNotesCheckBox, syncPlaylistsCheckBox, syncTemplatesCheckBox });

            // Authentication Group
            var authGroup = new GroupBox
            {
                Text = "Dropbox Authentication",
                Location = new Point(20, 160),
                Size = new Size(540, 180)
            };

            var appKeyLabel = new Label
            {
                Text = "App Key:",
                Location = new Point(20, 25),
                Size = new Size(80, 20)
            };

            appKeyTextBox = new TextBox
            {
                Location = new Point(110, 23),
                Size = new Size(400, 23),
                PlaceholderText = "Enter your Dropbox App Key"
            };

            var appSecretLabel = new Label
            {
                Text = "App Secret:",
                Location = new Point(20, 55),
                Size = new Size(80, 20)
            };

            appSecretTextBox = new TextBox
            {
                Location = new Point(110, 53),
                Size = new Size(400, 23),
                PasswordChar = '•',
                PlaceholderText = "Enter your Dropbox App Secret"
            };

            var accessTokenLabel = new Label
            {
                Text = "Access Token:",
                Location = new Point(20, 85),
                Size = new Size(80, 20)
            };

            accessTokenTextBox = new TextBox
            {
                Location = new Point(110, 83),
                Size = new Size(400, 23),
                PasswordChar = '•',
                PlaceholderText = "Generated after authentication",
                ReadOnly = true
            };

            authenticateButton = new Button
            {
                Text = "Authenticate with Dropbox",
                Location = new Point(20, 120),
                Size = new Size(180, 30)
            };
            authenticateButton.Click += AuthenticateButton_Click;

            testConnectionButton = new Button
            {
                Text = "Test Connection",
                Location = new Point(210, 120),
                Size = new Size(140, 30)
            };
            testConnectionButton.Click += TestConnectionButton_Click;

            revokeAccessButton = new Button
            {
                Text = "Revoke Access",
                Location = new Point(360, 120),
                Size = new Size(140, 30),
                ForeColor = Color.DarkRed
            };
            revokeAccessButton.Click += RevokeAccessButton_Click;

            authGroup.Controls.AddRange(new Control[] {
                appKeyLabel, appKeyTextBox,
                appSecretLabel, appSecretTextBox,
                accessTokenLabel, accessTokenTextBox,
                authenticateButton, testConnectionButton, revokeAccessButton
            });

            // Sync Status Group (increased height for new buttons)
            var statusGroup = new GroupBox
            {
                Text = "Synchronization Status",
                Location = new Point(20, 350),
                Size = new Size(540, 130)
            };

            lastSyncLabel = new Label
            {
                Text = "Last sync: Never",
                Location = new Point(20, 25),
                Size = new Size(500, 20)
            };

            syncProgressBar = new ProgressBar
            {
                Location = new Point(20, 50),
                Size = new Size(500, 23),
                Style = ProgressBarStyle.Continuous
            };

            // Manual sync buttons row
            syncNowButton = new Button
            {
                Text = "Sync Now (Push + Pull)",
                Location = new Point(20, 85),
                Size = new Size(160, 30)
            };
            syncNowButton.Click += SyncNowButton_Click;

            pushButton = new Button
            {
                Text = "↑ Push to Dropbox",
                Location = new Point(190, 85),
                Size = new Size(160, 30),
                BackColor = Color.FromArgb(230, 247, 255),
                FlatStyle = FlatStyle.Flat
            };
            pushButton.FlatAppearance.BorderColor = Color.FromArgb(0, 97, 255);
            pushButton.Click += PushButton_Click;

            pullButton = new Button
            {
                Text = "↓ Pull from Dropbox",
                Location = new Point(360, 85),
                Size = new Size(160, 30),
                BackColor = Color.FromArgb(255, 247, 230),
                FlatStyle = FlatStyle.Flat
            };
            pullButton.FlatAppearance.BorderColor = Color.FromArgb(255, 153, 0);
            pullButton.Click += PullButton_Click;

            statusGroup.Controls.AddRange(new Control[] { 
                lastSyncLabel, 
                syncProgressBar, 
                syncNowButton, 
                pushButton, 
                pullButton 
            });

            // Status Label
            statusLabel = new Label
            {
                Location = new Point(20, 490),
                Size = new Size(540, 20),
                ForeColor = Color.Blue
            };

            // Info label
            var infoLabel = new Label
            {
                Text = "💡 Need help setting up? Click the ? button above",
                Location = new Point(20, 515),
                Size = new Size(400, 20),
                ForeColor = Color.Gray,
                Font = new Font(Font.FontFamily, 8.5f, FontStyle.Italic)
            };

            // Action Buttons
            saveButton = new Button
            {
                Text = "Save",
                Location = new Point(390, 550),
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };
            saveButton.Click += SaveButton_Click;

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(480, 550),
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            AcceptButton = saveButton;
            CancelButton = cancelButton;

            Controls.AddRange(new Control[] {
                helpButton,
                enableSyncCheckBox,
                syncOptionsGroup,
                authGroup,
                statusGroup,
                statusLabel,
                infoLabel,
                saveButton,
                cancelButton
            });
        }

        private void HelpButton_Click(object sender, EventArgs e)
        {
            using (var helpForm = new DropboxSetupInstructions())
            {
                helpForm.ShowDialog(this);
            }
        }

        private void LoadSettings()
        {
            settings = AppConfiguration.DropboxSyncSettings;

            enableSyncCheckBox.Checked = settings.SyncEnabled;
            syncNotesCheckBox.Checked = settings.SyncNotes;
            syncPlaylistsCheckBox.Checked = settings.SyncPlaylists;
            syncTemplatesCheckBox.Checked = settings.SyncTemplates;
            appKeyTextBox.Text = settings.AppKey;
            appSecretTextBox.Text = settings.AppSecret;
            accessTokenTextBox.Text = settings.AccessToken;

            if (settings.LastSyncTime.HasValue)
            {
                lastSyncLabel.Text = $"Last sync: {settings.LastSyncTime.Value:yyyy-MM-dd HH:mm:ss}";
            }
        }

        private void SaveButton_Click(object sender, EventArgs e)
        {
            settings.SyncEnabled = enableSyncCheckBox.Checked;
            settings.SyncNotes = syncNotesCheckBox.Checked;
            settings.SyncPlaylists = syncPlaylistsCheckBox.Checked;
            settings.SyncTemplates = syncTemplatesCheckBox.Checked;
            settings.AppKey = appKeyTextBox.Text.Trim();
            settings.AppSecret = appSecretTextBox.Text.Trim();
            settings.AccessToken = accessTokenTextBox.Text.Trim();

            AppConfiguration.DropboxSyncSettings = settings;

            statusLabel.Text = "Settings saved successfully";
            statusLabel.ForeColor = Color.Green;
        }

        private void EnableSyncCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            UpdateUIState();
        }

        private void UpdateUIState()
        {
            bool enabled = enableSyncCheckBox.Checked;
            bool authenticated = !string.IsNullOrEmpty(accessTokenTextBox.Text);

            syncNotesCheckBox.Enabled = enabled;
            syncPlaylistsCheckBox.Enabled = enabled;
            syncTemplatesCheckBox.Enabled = enabled;
            appKeyTextBox.Enabled = enabled;
            appSecretTextBox.Enabled = enabled;
            authenticateButton.Enabled = enabled && !string.IsNullOrEmpty(appKeyTextBox.Text) && !string.IsNullOrEmpty(appSecretTextBox.Text);
            testConnectionButton.Enabled = enabled && authenticated;
            revokeAccessButton.Enabled = enabled && authenticated;
            syncNowButton.Enabled = enabled && authenticated;
            pushButton.Enabled = enabled && authenticated;
            pullButton.Enabled = enabled && authenticated;
        }

        private async void AuthenticateButton_Click(object sender, EventArgs e)
        {
            statusLabel.Text = "Opening Dropbox authentication...";
            statusLabel.ForeColor = Color.Blue;

            try
            {
                var authForm = new DropboxAuthForm(appKeyTextBox.Text.Trim());
                if (authForm.ShowDialog() == DialogResult.OK)
                {
                    accessTokenTextBox.Text = authForm.AccessToken;
                    statusLabel.Text = "Authentication successful!";
                    statusLabel.ForeColor = Color.Green;
                    UpdateUIState();
                }
                else
                {
                    statusLabel.Text = "Authentication cancelled";
                    statusLabel.ForeColor = Color.Orange;
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Authentication failed: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
        }

        private async void TestConnectionButton_Click(object sender, EventArgs e)
        {
            statusLabel.Text = "Testing connection...";
            statusLabel.ForeColor = Color.Blue;
            testConnectionButton.Enabled = false;

            try
            {
                bool success = await DropboxSyncManager.TestConnectionAsync(accessTokenTextBox.Text.Trim());
                
                if (success)
                {
                    statusLabel.Text = "Connection successful!";
                    statusLabel.ForeColor = Color.Green;
                }
                else
                {
                    statusLabel.Text = "Connection failed - Invalid token";
                    statusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Connection failed: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
            finally
            {
                testConnectionButton.Enabled = true;
            }
        }

        private async void RevokeAccessButton_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Are you sure you want to revoke Dropbox access?\n\nThis will disconnect the application from your Dropbox account.",
                "Revoke Access",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning
            );

            if (result != DialogResult.Yes)
                return;

            statusLabel.Text = "Revoking access...";
            statusLabel.ForeColor = Color.Blue;
            revokeAccessButton.Enabled = false;

            try
            {
                bool success = await DropboxSyncManager.RevokeAccessAsync(accessTokenTextBox.Text.Trim());
                
                if (success)
                {
                    accessTokenTextBox.Text = string.Empty;
                    statusLabel.Text = "Access revoked successfully";
                    statusLabel.ForeColor = Color.Green;
                    UpdateUIState();
                }
                else
                {
                    statusLabel.Text = "Failed to revoke access";
                    statusLabel.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"Error: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
            finally
            {
                revokeAccessButton.Enabled = true;
            }
        }

        private async void SyncNowButton_Click(object sender, EventArgs e)
        {
            // Manual sync - sync everything (Full mode)
            await PerformSyncAsync(DropboxSyncManager.SyncDirection.Both, DropboxSyncManager.SyncMode.Full);
        }

        private async void PushButton_Click(object sender, EventArgs e)
        {
            // Manual push - push everything (Full mode)
            await PerformSyncAsync(DropboxSyncManager.SyncDirection.PushOnly, DropboxSyncManager.SyncMode.Full);
        }

        private async void PullButton_Click(object sender, EventArgs e)
        {
            // Manual pull - pull everything (Full mode)
            await PerformSyncAsync(DropboxSyncManager.SyncDirection.PullOnly, DropboxSyncManager.SyncMode.Full);
        }

        private async Task PerformSyncAsync(DropboxSyncManager.SyncDirection direction, DropboxSyncManager.SyncMode mode)
        {
            string action = direction switch
            {
                DropboxSyncManager.SyncDirection.PushOnly => "Pushing to Dropbox",
                DropboxSyncManager.SyncDirection.PullOnly => "Pulling from Dropbox",
                _ => "Synchronizing"
            };

            statusLabel.Text = $"{action}...";
            statusLabel.ForeColor = Color.Blue;
            
            // Disable all sync buttons during operation
            syncNowButton.Enabled = false;
            pushButton.Enabled = false;
            pullButton.Enabled = false;
            syncProgressBar.Style = ProgressBarStyle.Marquee;

            try
            {
                var progress = new Progress<string>(message =>
                {
                    statusLabel.Text = message;
                });

                // Create temporary settings from current UI
                var tempSettings = new DropboxSyncSettings
                {
                    SyncEnabled = enableSyncCheckBox.Checked,
                    SyncNotes = syncNotesCheckBox.Checked,
                    SyncPlaylists = syncPlaylistsCheckBox.Checked,
                    SyncTemplates = syncTemplatesCheckBox.Checked,
                    AccessToken = accessTokenTextBox.Text.Trim(),
                    AppKey = appKeyTextBox.Text.Trim(),
                    AppSecret = appSecretTextBox.Text.Trim()
                };

                var result = await DropboxSyncManager.SynchronizeAsync(tempSettings, progress, direction, mode);

                if (result.Success)
                {
                    statusLabel.Text = result.Message;
                    statusLabel.ForeColor = Color.Green;
                    lastSyncLabel.Text = $"Last sync: {result.SyncTime:yyyy-MM-dd HH:mm:ss}";
                    settings.LastSyncTime = result.SyncTime;
                }
                else
                {
                    statusLabel.Text = result.Message;
                    statusLabel.ForeColor = Color.Red;
                }

                // After sync completes
                if (!result.Success && !string.IsNullOrEmpty(result.DetailedError))
                {
                    var errorForm = new Form
                    {
                        Text = "Sync Error Details",
                        Width = 600,
                        Height = 400,
                        StartPosition = FormStartPosition.CenterParent
                    };
                    
                    var textBox = new TextBox
                    {
                        Multiline = true,
                        ScrollBars = ScrollBars.Both,
                        Dock = DockStyle.Fill,
                        ReadOnly = true,
                        Text = result.DetailedError,
                        Font = new Font("Consolas", 9)
                    };
                    
                    errorForm.Controls.Add(textBox);
                    errorForm.ShowDialog(this);
                }
            }
            catch (Exception ex)
            {
                statusLabel.Text = $"{action} failed: {ex.Message}";
                statusLabel.ForeColor = Color.Red;
            }
            finally
            {
                syncProgressBar.Style = ProgressBarStyle.Continuous;
                UpdateUIState(); // Re-enable buttons
            }
        }
    }
}