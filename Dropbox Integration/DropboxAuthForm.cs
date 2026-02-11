using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Form for Dropbox OAuth authentication
    /// </summary>
    public class DropboxAuthForm : Form
    {
        private WebView2 webView;
        private string appKey;
        private const string RedirectUri = "https://localhost";

        public string AccessToken { get; private set; }

        public DropboxAuthForm(string appKey)
        {
            this.appKey = appKey;
            InitializeComponent();
            InitializeAsync();
        }

        private void InitializeComponent()
        {
            Text = "Dropbox Authentication";
            Size = new Size(800, 600);
            StartPosition = FormStartPosition.CenterParent;

            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };

            Controls.Add(webView);
        }

        private async void InitializeAsync()
        {
            try
            {
                await webView.EnsureCoreWebView2Async(null);

                webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

                // Dropbox OAuth URL with token response type
                string authorizeUrl = $"https://www.dropbox.com/oauth2/authorize?client_id={appKey}&response_type=token&redirect_uri={Uri.EscapeDataString(RedirectUri)}";

                webView.CoreWebView2.Navigate(authorizeUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize authentication:\n{ex.Message}",
                    "Authentication Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        private void CoreWebView2_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            // Check if we got redirected with the access token
            if (e.Uri.StartsWith(RedirectUri))
            {
                e.Cancel = true;

                // Extract access token from URL fragment
                var uri = new Uri(e.Uri.Replace("#", "?"));
                var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
                AccessToken = query["access_token"];

                if (!string.IsNullOrEmpty(AccessToken))
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
                else if (e.Uri.Contains("error="))
                {
                    var error = query["error"];
                    var errorDescription = query["error_description"];
                    MessageBox.Show(
                        $"Authentication failed:\n{errorDescription ?? error}",
                        "Authentication Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
                else
                {
                    MessageBox.Show(
                        "Failed to retrieve access token",
                        "Authentication Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            }
        }
    }
}