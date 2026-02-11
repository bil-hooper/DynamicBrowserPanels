using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    public class OnlineItemDialog : Form
    {
        private TabControl tabInput;
        private TabPage tabUrl;
        private TabPage tabEmbed;
        private TextBox txtUrl;
        private TextBox txtEmbedCode;
        private TextBox txtDisplayName;
        private ComboBox cmbMediaType;
        private TextBox txtDescription;
        private ListBox lstItems;
        private Button btnAdd;
        private Button btnRemove;
        private Button btnOK;
        private Button btnCancel;

        public List<OnlineMediaItem> Items { get; private set; } = new List<OnlineMediaItem>();

        public OnlineItemDialog()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Add Online Media Items";
            this.Size = new Size(600, 550);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Tab control for URL vs Embed Code input
            tabInput = new TabControl
            {
                Location = new Point(20, 20),
                Size = new Size(550, 160)
            };

            // URL Tab
            tabUrl = new TabPage("URL");
            var lblUrl = new Label { Text = "URL:", Location = new Point(10, 15), AutoSize = true };
            txtUrl = new TextBox { Location = new Point(100, 12), Size = new Size(420, 23) };
            txtUrl.PlaceholderText = "https://www.youtube.com/watch?v=...";
            tabUrl.Controls.AddRange(new Control[] { lblUrl, txtUrl });

            // Embed Code Tab
            tabEmbed = new TabPage("Embed Code");
            var lblEmbed = new Label 
            { 
                Text = "Paste iframe embed code:", 
                Location = new Point(10, 10), 
                AutoSize = true 
            };
            txtEmbedCode = new TextBox
            {
                Location = new Point(10, 35),
                Size = new Size(510, 80),
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 8.5f)
            };
            txtEmbedCode.PlaceholderText = 
                "<iframe width=\"560\" height=\"315\" src=\"https://www.youtube.com/embed/...\" ...></iframe>";
            tabEmbed.Controls.AddRange(new Control[] { lblEmbed, txtEmbedCode });

            tabInput.TabPages.Add(tabUrl);
            tabInput.TabPages.Add(tabEmbed);

            // Display name input
            var lblName = new Label { Text = "Display Name:", Location = new Point(20, 195), AutoSize = true };
            txtDisplayName = new TextBox { Location = new Point(120, 192), Size = new Size(450, 23) };
            txtDisplayName.PlaceholderText = "My Video Title";

            // Media type selector
            var lblType = new Label { Text = "Type:", Location = new Point(20, 230), AutoSize = true };
            cmbMediaType = new ComboBox 
            { 
                Location = new Point(120, 227), 
                Size = new Size(200, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbMediaType.Items.AddRange(new object[] 
            { 
                "Auto-Detect",
                "YouTube", 
                "Vimeo", 
                "Dropbox", 
                "SoundCloud", 
                "Direct Stream",
                "Generic Embed" 
            });
            cmbMediaType.SelectedIndex = 0;

            // Description
            var lblDesc = new Label { Text = "Description:", Location = new Point(20, 265), AutoSize = true };
            txtDescription = new TextBox 
            { 
                Location = new Point(120, 262), 
                Size = new Size(450, 60),
                Multiline = true
            };
            txtDescription.PlaceholderText = "Optional description...";

            // Add button
            btnAdd = new Button { Text = "Add to List", Location = new Point(470, 330), Size = new Size(100, 30) };
            btnAdd.Click += BtnAdd_Click;

            // Items list
            var lblList = new Label { Text = "Items:", Location = new Point(20, 335), AutoSize = true };
            lstItems = new ListBox 
            { 
                Location = new Point(20, 360), 
                Size = new Size(440, 120),
                Font = new Font("Consolas", 9)
            };

            // Remove button
            btnRemove = new Button { Text = "Remove", Location = new Point(470, 360), Size = new Size(100, 30) };
            btnRemove.Click += BtnRemove_Click;

            // OK/Cancel buttons
            btnOK = new Button 
            { 
                Text = "OK", 
                Location = new Point(400, 495), 
                Size = new Size(80, 30),
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button 
            { 
                Text = "Cancel", 
                Location = new Point(490, 495), 
                Size = new Size(80, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[]
            {
                tabInput,
                lblName, txtDisplayName,
                lblType, cmbMediaType,
                lblDesc, txtDescription,
                btnAdd,
                lblList, lstItems,
                btnRemove,
                btnOK, btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            string url = null;

            // Determine which tab is active and extract URL accordingly
            if (tabInput.SelectedTab == tabUrl)
            {
                if (string.IsNullOrWhiteSpace(txtUrl.Text))
                {
                    MessageBox.Show("Please enter a URL.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                url = txtUrl.Text.Trim();
            }
            else if (tabInput.SelectedTab == tabEmbed)
            {
                if (string.IsNullOrWhiteSpace(txtEmbedCode.Text))
                {
                    MessageBox.Show("Please paste an embed code.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Extract URL from iframe embed code
                url = ExtractUrlFromIframe(txtEmbedCode.Text);
                if (string.IsNullOrEmpty(url))
                {
                    MessageBox.Show(
                        "Could not extract a valid URL from the embed code.\n\n" +
                        "Please make sure the embed code contains an iframe with a src attribute.",
                        "Invalid Embed Code",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                    return;
                }
            }

            var mediaType = cmbMediaType.SelectedIndex switch
            {
                1 => OnlineMediaType.YouTube,
                2 => OnlineMediaType.Vimeo,
                3 => OnlineMediaType.Dropbox,
                4 => OnlineMediaType.SoundCloud,
                5 => OnlineMediaType.DirectStream,
                6 => OnlineMediaType.Embed,
                _ => OnlineMediaType.Unknown
            };

            var item = new OnlineMediaItem(
                url,
                string.IsNullOrWhiteSpace(txtDisplayName.Text) ? null : txtDisplayName.Text.Trim(),
                mediaType
            );

            if (!string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                item.Description = txtDescription.Text.Trim();
            }

            Items.Add(item);
            lstItems.Items.Add($"{item.DisplayName} ({item.MediaType})");

            // Clear inputs for next item
            txtUrl.Clear();
            txtEmbedCode.Clear();
            txtDisplayName.Clear();
            txtDescription.Clear();
            cmbMediaType.SelectedIndex = 0;
            
            // Focus back to the active tab's input
            if (tabInput.SelectedTab == tabUrl)
                txtUrl.Focus();
            else
                txtEmbedCode.Focus();
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (lstItems.SelectedIndex >= 0)
            {
                Items.RemoveAt(lstItems.SelectedIndex);
                lstItems.Items.RemoveAt(lstItems.SelectedIndex);
            }
        }

        /// <summary>
        /// Extracts the URL from an iframe embed code
        /// </summary>
        private string ExtractUrlFromIframe(string embedCode)
        {
            if (string.IsNullOrWhiteSpace(embedCode))
                return null;

            // Try to extract src attribute from iframe tag
            // Supports both single and double quotes, with or without spaces
            var patterns = new[]
            {
                @"<iframe[^>]+src\s*=\s*[""']([^""']+)[""']", // src="url" or src='url'
                @"<iframe[^>]+src\s*=\s*([^\s>]+)",           // src=url (no quotes)
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(embedCode, pattern, RegexOptions.IgnoreCase);
                if (match.Success && match.Groups.Count > 1)
                {
                    var url = match.Groups[1].Value.Trim();
                    
                    // Validate that it's a proper URL
                    if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && 
                        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                    {
                        return url;
                    }
                }
            }

            return null;
        }
    }
}   