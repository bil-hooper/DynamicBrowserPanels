using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Form displaying Dropbox setup instructions
    /// /// </summary>
    public class DropboxSetupInstructions : Form
    {
        public DropboxSetupInstructions()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "Dropbox Setup Instructions";
            Size = new Size(650, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Title
            var titleLabel = new Label
            {
                Text = "How to Set Up Dropbox Synchronization",
                Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(600, 30),
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Instructions text
            var instructionsText = new RichTextBox
            {
                Location = new Point(20, 60),
                Size = new Size(600, 400),
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f),
                BackColor = SystemColors.Window,
                Text = @"Follow these steps to enable Dropbox synchronization:

1. Create a Dropbox App
   • Go to https://www.dropbox.com/developers/apps
   • Click ""Create app""
   • Choose ""Scoped access""
   • Choose ""App folder"" (recommended - only accesses its own folder)
   • Give your app a name (e.g., ""Dynamic Browser Panels"")
   • Click ""Create app""

2. Configure Permissions
   • On the app settings page, click the ""Permissions"" tab
   • Enable the following permissions:
     ✓ files.metadata.write
     ✓ files.metadata.read
     ✓ files.content.write
     ✓ files.content.read
   • Click ""Submit"" at the bottom to save permissions
   • Without these permissions, Sync will fail

3. Configure OAuth Redirect URI (IMPORTANT!)
   • Go back to the ""Settings"" tab
   • Scroll to ""OAuth 2""
   • Under ""Redirect URIs"", click ""Add""
   • Enter exactly: https://localhost
   • Click ""Add"" to save it
   • This step is required for authentication to work!

4. Get Your Credentials
   • On the same settings page, locate the ""App key"" section
   • Copy the ""App key""
   • Copy the ""App secret""
   • Keep these credentials secure!

5. Configure in Dynamic Browser Panels
   • Right-click in the browser → ""☁️ Dropbox Sync..."":
     • Enable ""Enable Dropbox Synchronization""
     • Paste your App Key and App Secret
     • Click ""Authenticate with Dropbox""
     • Sign in and authorize the app

6. Select Folders to Sync
   • Check the folders you want to sync:
     □ Notes - Your notepad files
     □ Playlists - Your M3U playlists
     □ Templates - Your saved layouts (.frm files)

7. Start Syncing
   • Click ""Sync Now"" to manually sync
   • Or click ""Save"" - sync happens automatically at:
     • Application startup (pulls remote changes)
     • Application shutdown (pushes local changes)

Your data will be stored in Dropbox at:
/Apps/Dynamic Browser Panels/
├── Notes/
├── Playlists/
└── Templates/

TROUBLESHOOTING:
• If you get ""missing_scope"" error: You added permissions AFTER authenticating.
  Click ""Revoke Access"" then ""Authenticate with Dropbox"" again.
• If you get ""files.metadata.read"" error: You forgot to enable permissions.
  Go to Permissions tab, enable all 4 file permissions, then re-authenticate.

Note: App folder access means this app can ONLY access its own folder,
not your entire Dropbox, to ensure your personal security."
            };

            // Open Dropbox Developers button
            var openDropboxButton = new Button
            {
                Text = "Open Dropbox Developers Page",
                Location = new Point(20, 475),
                Size = new Size(220, 35),
                BackColor = Color.FromArgb(0, 97, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            openDropboxButton.FlatAppearance.BorderSize = 0;
            openDropboxButton.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://www.dropbox.com/developers/apps",
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to open browser:\n{ex.Message}\n\nPlease visit:\nhttps://www.dropbox.com/developers/apps",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            };

            // Close button
            var closeButton = new Button
            {
                Text = "Close",
                Location = new Point(530, 475),
                Size = new Size(90, 35),
                DialogResult = DialogResult.OK
            };

            AcceptButton = closeButton;

            Controls.AddRange(new Control[] {
                titleLabel,
                instructionsText,
                openDropboxButton,
                closeButton
            });
        }
    }
}