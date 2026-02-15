using System;
using System.Drawing;
using System.Linq;
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
            if (string.IsNullOrWhiteSpace(appKey))
            {
                throw new ArgumentException("App key cannot be null or empty", nameof(appKey));
            }

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

                if (webView?.CoreWebView2 == null)
                {
                    throw new InvalidOperationException("Failed to initialize WebView2");
                }

                webView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;

                // Dropbox OAuth URL with token response type
                string authorizeUrl = $"https://www.dropbox.com/oauth2/authorize?client_id={appKey}&response_type=token&redirect_uri={Uri.EscapeDataString(RedirectUri)}";

                webView.CoreWebView2.Navigate(authorizeUrl);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize authentication:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
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
            try
            {
                if (string.IsNullOrEmpty(e?.Uri))
                {
                    return;
                }

                // Check if we got redirected with the access token
                if (e.Uri.StartsWith(RedirectUri, StringComparison.OrdinalIgnoreCase))
                {
                    e.Cancel = true;

                    // Extract access token from URL fragment (after #)
                    string accessToken = null;
                    string error = null;
                    string errorDescription = null;

                    // Parse the fragment manually (more reliable than HttpUtility)
                    int hashIndex = e.Uri.IndexOf('#');
                    if (hashIndex >= 0 && hashIndex < e.Uri.Length - 1)
                    {
                        string fragment = e.Uri.Substring(hashIndex + 1);
                        var parameters = fragment.Split('&')
                            .Select(param => param.Split('='))
                            .Where(parts => parts.Length == 2)
                            .ToDictionary(
                                parts => Uri.UnescapeDataString(parts[0]),
                                parts => Uri.UnescapeDataString(parts[1]),
                                StringComparer.OrdinalIgnoreCase
                            );

                        parameters.TryGetValue("access_token", out accessToken);
                        parameters.TryGetValue("error", out error);
                        parameters.TryGetValue("error_description", out errorDescription);
                    }

                    if (!string.IsNullOrEmpty(accessToken))
                    {
                        AccessToken = accessToken;
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                    else if (!string.IsNullOrEmpty(error))
                    {
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
                            $"Failed to retrieve access token.\n\nRedirect URI: {e.Uri}",
                            "Authentication Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        DialogResult = DialogResult.Cancel;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error during authentication:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
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