using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Dialog for adding multiple URLs at once via multiline text input
    /// </summary>
    public class BulkUrlDialog : Form
    {
        private TextBox txtUrls;
        private ComboBox cmbDefaultType;
        private CheckBox chkAutoDetect;
        private Label lblInstructions;
        private Label lblCount;
        private Button btnOK;
        private Button btnCancel;

        public List<OnlineMediaItem> Items { get; private set; } = new List<OnlineMediaItem>();

        public BulkUrlDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Add Multiple URLs";
            this.Size = new Size(700, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Instructions
            lblInstructions = new Label
            {
                Text = "Enter URLs (one per line). Each URL will be automatically detected or use the default type below:",
                Location = new Point(20, 20),
                Size = new Size(640, 40),
                AutoSize = false
            };

            // Multiline URL input
            txtUrls = new TextBox
            {
                Location = new Point(20, 70),
                Size = new Size(640, 300),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9),
                AcceptsReturn = true
            };
            txtUrls.PlaceholderText = 
                "https://www.youtube.com/watch?v=...\n" +
                "https://vimeo.com/...\n" +
                "https://www.dropbox.com/...\n" +
                "https://soundcloud.com/...";
            txtUrls.TextChanged += TxtUrls_TextChanged;

            // Auto-detect checkbox
            chkAutoDetect = new CheckBox
            {
                Text = "Auto-detect media type for each URL",
                Location = new Point(20, 385),
                AutoSize = true,
                Checked = true
            };
            chkAutoDetect.CheckedChanged += ChkAutoDetect_CheckedChanged;

            // Default type selector (disabled when auto-detect is on)
            var lblDefaultType = new Label
            {
                Text = "Default Type:",
                Location = new Point(20, 420),
                AutoSize = true
            };
            cmbDefaultType = new ComboBox
            {
                Location = new Point(120, 417),
                Size = new Size(200, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Enabled = false
            };
            cmbDefaultType.Items.AddRange(new object[]
            {
                "YouTube",
                "Vimeo",
                "Dropbox",
                "SoundCloud",
                "Direct Stream",
                "Generic Embed"
            });
            cmbDefaultType.SelectedIndex = 0;

            // Count label
            lblCount = new Label
            {
                Text = "URLs: 0",
                Location = new Point(340, 420),
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold)
            };

            // OK/Cancel buttons
            btnOK = new Button
            {
                Text = "Add to Playlist",
                Location = new Point(480, 470),
                Size = new Size(120, 35),
                DialogResult = DialogResult.OK
            };
            btnOK.Click += BtnOK_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(610, 470),
                Size = new Size(70, 35),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[]
            {
                lblInstructions,
                txtUrls,
                chkAutoDetect,
                lblDefaultType,
                cmbDefaultType,
                lblCount,
                btnOK,
                btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void ChkAutoDetect_CheckedChanged(object sender, EventArgs e)
        {
            cmbDefaultType.Enabled = !chkAutoDetect.Checked;
        }

        private void TxtUrls_TextChanged(object sender, EventArgs e)
        {
            // Count non-empty lines
            var lines = txtUrls.Lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            lblCount.Text = $"URLs: {lines.Length}";
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            Items.Clear();

            var lines = txtUrls.Lines
                .Select(l => l.Trim())
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .ToArray();

            if (lines.Length == 0)
            {
                MessageBox.Show(
                    "Please enter at least one URL.",
                    "No URLs",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                this.DialogResult = DialogResult.None;
                return;
            }

            // Determine default media type if not auto-detecting
            OnlineMediaType defaultType = OnlineMediaType.Unknown;
            if (!chkAutoDetect.Checked)
            {
                defaultType = cmbDefaultType.SelectedIndex switch
                {
                    0 => OnlineMediaType.YouTube,
                    1 => OnlineMediaType.Vimeo,
                    2 => OnlineMediaType.Dropbox,
                    3 => OnlineMediaType.SoundCloud,
                    4 => OnlineMediaType.DirectStream,
                    5 => OnlineMediaType.Embed,
                    _ => OnlineMediaType.Unknown
                };
            }

            var invalidUrls = new List<string>();
            int urlNumber = 1;

            foreach (var line in lines)
            {
                // Validate URL format
                if (!line.StartsWith("http://") && !line.StartsWith("https://"))
                {
                    invalidUrls.Add($"Line {urlNumber}: {line}");
                    urlNumber++;
                    continue;
                }

                try
                {
                    var uri = new Uri(line);
                    
                    // Create the media item
                    var mediaType = chkAutoDetect.Checked ? OnlineMediaType.Unknown : defaultType;
                    var item = new OnlineMediaItem(line, null, mediaType);
                    Items.Add(item);
                }
                catch
                {
                    invalidUrls.Add($"Line {urlNumber}: {line}");
                }

                urlNumber++;
            }

            // Show warning if some URLs were invalid
            if (invalidUrls.Count > 0)
            {
                var message = $"The following {invalidUrls.Count} URL(s) are invalid and will be skipped:\n\n";
                message += string.Join("\n", invalidUrls.Take(10));
                if (invalidUrls.Count > 10)
                {
                    message += $"\n... and {invalidUrls.Count - 10} more";
                }

                var result = MessageBox.Show(
                    message + $"\n\nContinue with {Items.Count} valid URL(s)?",
                    "Invalid URLs",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );

                if (result != DialogResult.Yes)
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }
            }

            if (Items.Count == 0)
            {
                MessageBox.Show(
                    "No valid URLs to add.",
                    "No Valid URLs",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                this.DialogResult = DialogResult.None;
            }
        }
    }
}