using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Represents a single browser tab with its own WebView2 instance
    /// </summary>
    internal class BrowserTab : IDisposable
    {
        private WebView2 _webView;
        private CoreWebView2Environment _environment;
        private bool _isInitialized = false;
        private string _pendingUrl;
        private string _currentUrl;
        private TaskCompletionSource<bool> _navigationCompletionSource;

        public event EventHandler<string> UrlChanged;
        public event EventHandler<string> TitleChanged;

        public WebView2 WebView => _webView;
        public bool IsInitialized => _isInitialized;
        public string CurrentUrl => _currentUrl ?? _webView?.Source?.ToString() ?? _pendingUrl ?? "";
        public bool CanGoBack => _webView?.CoreWebView2?.CanGoBack ?? false;
        public bool CanGoForward => _webView?.CoreWebView2?.CanGoForward ?? false;
        
        /// <summary>
        /// Custom name for the tab (suppresses automatic title updates when set)
        /// </summary>
        public string CustomName { get; set; }

        public BrowserTab(CoreWebView2Environment environment)
        {
            _environment = environment;
            _webView = new WebView2();
        }

        public async Task Initialize(string initialUrl)
        {
            try
            {
                await _webView.EnsureCoreWebView2Async(_environment);

                // Configure settings
                _webView.CoreWebView2.Settings.IsScriptEnabled = true;
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
                
                // Enable password autosave (requires WebView2 Runtime 1.0.1185.39 or later)
                _webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;
                
                // Enable general autofill for forms
                _webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;

                // Wire up events
                _webView.NavigationStarting += WebView_NavigationStarting;
                _webView.NavigationCompleted += WebView_NavigationCompleted;
                _webView.CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;

                _isInitialized = true;

                // Navigate to initial URL
                if (!string.IsNullOrWhiteSpace(initialUrl))
                {
                    await NavigateToUrl(initialUrl);
                }
                else if (!string.IsNullOrWhiteSpace(_pendingUrl))
                {
                    await NavigateToUrl(_pendingUrl);
                    _pendingUrl = null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to initialize browser tab: {ex.Message}",
                    "Initialization Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Navigates to a URL and waits for navigation to complete
        /// </summary>
        public async Task NavigateToUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return;

            // Store the URL we're navigating to
            _currentUrl = url;

            if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://") && !url.StartsWith("edge://"))
            {
                url = "https://" + url;
                _currentUrl = url;
            }

            if (_isInitialized && _webView?.CoreWebView2 != null)
            {
                // Create a task completion source to wait for navigation
                var navigationTcs = new TaskCompletionSource<bool>();
                _navigationCompletionSource = navigationTcs;

                try
                {
                    _webView.Source = new Uri(url);
                    
                    var completedTask = await Task.WhenAny(
                        navigationTcs.Task,
                        Task.Delay(GlobalConstants.DELAY_FOR_DOCUMENT_LOAD_MS)
                    );

                    if (completedTask != navigationTcs.Task)
                    {
                        // Timeout occurred
                    }
                }
                catch
                {
                    // Navigation failed, but URL is still tracked
                }
                finally
                {
                    _navigationCompletionSource = null;
                }
            }
            else
            {
                _pendingUrl = url;
            }
        }

        public void GoBack()
        {
            if (_webView?.CoreWebView2 != null && _webView.CoreWebView2.CanGoBack)
            {
                _webView.CoreWebView2.GoBack();
            }
        }

        public void GoForward()
        {
            if (_webView?.CoreWebView2 != null && _webView.CoreWebView2.CanGoForward)
            {
                _webView.CoreWebView2.GoForward();
            }
        }

        public void Refresh()
        {
            _webView?.CoreWebView2?.Reload();
        }

        private void WebView_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            _currentUrl = e.Uri;
            UrlChanged?.Invoke(this, e.Uri);
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _currentUrl = _webView.Source?.ToString() ?? _currentUrl;
            UrlChanged?.Invoke(this, _webView.Source?.ToString() ?? "");
            
            // Signal that navigation is complete
            _navigationCompletionSource?.TrySetResult(true);
        }

        private void CoreWebView2_DocumentTitleChanged(object sender, object e)
        {
            // Only fire TitleChanged event if there's no custom name set
            if (string.IsNullOrWhiteSpace(CustomName))
            {
                TitleChanged?.Invoke(this, _webView.CoreWebView2.DocumentTitle);
            }
            // Otherwise, silently ignore the title change - custom name takes precedence
        }

        public void Dispose()
        {
            _webView?.Dispose();
        }
    }
}
