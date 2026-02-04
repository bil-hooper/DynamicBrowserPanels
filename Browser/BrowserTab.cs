using System;
using System.Text.Json;
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
        
        // Add field
        private PlaylistManager _playlist;
        private int _notepadInstance = -1; // -1 means not a notepad

        public WebView2 WebView => _webView;
        public bool IsInitialized => _isInitialized;
        public string CurrentUrl => _currentUrl ?? _webView?.Source?.ToString() ?? _pendingUrl ?? "";
        public bool CanGoBack => _webView?.CoreWebView2?.CanGoBack ?? false;
        public bool CanGoForward => _webView?.CoreWebView2?.CanGoForward ?? false;

        // Add property
        public PlaylistManager Playlist
        {
            get
            {
                if (_playlist == null)
                {
                    _playlist = new PlaylistManager();
                    // No longer need MediaChanged event - playlist player handles everything
                }
                return _playlist;
            }
        }

        // Add this property
        public int NotepadInstance
        {
            get => _notepadInstance;
            set => _notepadInstance = value;
        }

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
                _webView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
                
                // Add permission handler for autoplay
                _webView.CoreWebView2.PermissionRequested += CoreWebView2_PermissionRequested;

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

        private void CoreWebView2_PermissionRequested(object sender, CoreWebView2PermissionRequestedEventArgs e)
        {
            // Auto-grant all permissions for local file:// URLs (our media player)
            if (e.Uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                e.State = CoreWebView2PermissionState.Allow;
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

        private async void CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                var json = e.WebMessageAsJson;
                
                // Parse the JSON to get the action
                if (json.Contains("\"action\""))
                {
                    if (json.Contains("\"saveNotepad\""))
                    {
                        // Handle notepad save
                        HandleNotepadSave(json);
                    }
                    else if (json.Contains("\"refreshNotepad\"") || json.Contains("\"requestCurrentContent\""))
                    {
                        // Handle notepad refresh/content request
                        HandleNotepadRefresh();
                    }
                    else if (json.Contains("\"exportNotepad\""))
                    {
                        // Handle notepad export
                        HandleNotepadExport(json);
                    }
                    else if (json.Contains("\"previous\""))
                    {
                        // Handle previous track
                        if (_playlist != null && _playlist.Count > 0)
                        {
                            var prevFile = _playlist.Previous();    
                            if (prevFile != null)
                            {
                                await NavigateToPlaylistTrack(prevFile);
                            }
                        }
                    }
                    else if (json.Contains("\"next\""))
                    {
                        // Handle next track
                        if (_playlist != null && _playlist.Count > 0)
                        {
                            var nextFile = _playlist.Next();
                            if (nextFile != null)
                            {
                                await NavigateToPlaylistTrack(nextFile);
                            }
                        }
                    }
                    else if (json.Contains("\"stateChanged\""))
                    {
                        // Future: Could extract and sync playlist state here
                        // For now, the HTML player manages its own state
                    }
                }
            }
            catch
            {
                // Ignore message parsing errors
            }
        }

        /// <summary>
        /// Handles saving notepad content from JavaScript
        /// </summary>
        private void HandleNotepadSave(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int instanceNumber = -1;
                    if (document.RootElement.TryGetProperty("instanceNumber", out JsonElement instanceElement))
                    {
                        instanceNumber = instanceElement.GetInt32();
                    }
            
                    if (document.RootElement.TryGetProperty("content", out JsonElement contentElement))
                    {
                        var content = contentElement.GetString() ?? string.Empty;
                        content = UnescapeJavaScriptString(content);
                
                        // Save to JSON file
                        var notepadData = new NotepadData
                        {
                            Content = content,
                            HasUnsavedChanges = false
                        };
                
                        NotepadManager.SaveNotepad(notepadData, instanceNumber);
                
                        // CRITICAL: Also regenerate the HTML file with the updated content
                        // This ensures when you switch tabs and come back, you see the latest content
                        NotepadHelper.CreateNotepadHtml(content, instanceNumber);
                    }
                }
            }
            catch (Exception ex)
            {
                // Do nothing.
            }
        }

        /// <summary>
        /// Handles exporting notepad content to file
        /// </summary>
        private void HandleNotepadExport(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int instanceNumber = -1;
                    if (document.RootElement.TryGetProperty("instanceNumber", out JsonElement instanceElement))
                    {
                        instanceNumber = instanceElement.GetInt32();
                    }
                    
                    if (document.RootElement.TryGetProperty("content", out JsonElement contentElement))
                    {
                        var content = contentElement.GetString() ?? string.Empty;
                        content = UnescapeJavaScriptString(content);
                        
                        using (var dialog = new SaveFileDialog())
                        {
                            dialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                            dialog.DefaultExt = "txt";
                            dialog.FileName = $"Notepad_{instanceNumber}_{DateTime.Now:yyyy-MM-dd_HHmmss}.txt";
                            dialog.Title = "Export Notepad";
                            
                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                if (NotepadManager.ExportToFile(content, dialog.FileName, instanceNumber))
                                {
                                    MessageBox.Show(
                                        $"Notes exported successfully to:\n{dialog.FileName}",
                                        "Export Complete",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Information
                                    );
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to export notepad: {ex.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Unescapes common JavaScript string escape sequences
        /// </summary>
        private string UnescapeJavaScriptString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\'", "'")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\"); // Must be last to avoid double-unescaping
        }

        /// <summary>
        /// Navigate to a specific track in the playlist (for single-file player with prev/next)
        /// </summary>
        private async Task NavigateToPlaylistTrack(string filePath)
        {
            try
            {
                // Create temporary player HTML with playlist support
                var tempHtmlPath = LocalMediaHelper.CreateTemporaryPlayerFile(
                    filePath,
                    autoplay: true,
                    loop: false,
                    playlistFiles: _playlist?.MediaFiles,
                    currentIndex: _playlist?.CurrentIndex ?? 0
                );
                var playerUrl = LocalMediaHelper.FilePathToUrl(tempHtmlPath);
                await NavigateToUrl(playerUrl);
            }
            catch
            {
                // Ignore navigation errors
            }
        }

        /// <summary>
        /// Handles refreshing notepad content from disk
        /// </summary>
        private async void HandleNotepadRefresh()
        {
            try
            {
                // Load current content from disk using the instance number
                var notepadData = NotepadManager.LoadNotepad(_notepadInstance);
                var content = notepadData.Content ?? string.Empty;

                // Escape for JSON transmission
                var escapedContent = NotepadHelper.EscapeForJavaScript(content);
                
                // Send updated content to the page
                var updateScript = $@"
                    window.dispatchEvent(new MessageEvent('message', {{
                        data: {{
                            action: 'updateContent',
                            content: ""{escapedContent}""
                        }}
                    }}));
                ";
                
                await _webView.CoreWebView2.ExecuteScriptAsync(updateScript);
            }
            catch (Exception ex)
            {
                // Do nothing
            }
        }

        public void Dispose()
        {
            _webView?.Dispose();
        }
    }
}
