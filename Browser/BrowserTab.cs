using System;
using System.Diagnostics;
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
        public event EventHandler MuteStateChanged;
        
        // Add field
        private PlaylistManager _playlist;
        private OnlineMediaPlaylist _onlinePlaylist;
        private bool _isMuted = false;

        public WebView2 WebView => _webView;
        public bool IsInitialized => _isInitialized;
        public string CurrentUrl => _currentUrl ?? _webView?.Source?.ToString() ?? _pendingUrl ?? "";
        public bool CanGoBack => _webView?.CoreWebView2?.CanGoBack ?? false;
        public bool CanGoForward => _webView?.CoreWebView2?.CanGoForward ?? false;

        /// <summary>
        /// Indicates whether this tab is in incognito mode (no history, no template save)
        /// </summary>
        public bool IsIncognito { get; set; }

        /// <summary>
        /// Gets or sets whether this tab is muted
        /// </summary>
        public bool IsMuted 
        { 
            get => _isMuted;
            set
            {
                if (_isMuted != value)
                {
                    _isMuted = value;
                    ApplyMuteState();
                    MuteStateChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

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

        // Add property for online playlist
        public OnlineMediaPlaylist OnlinePlaylist
        {
            get
            {
                if (_onlinePlaylist == null)
                {
                    _onlinePlaylist = new OnlineMediaPlaylist();
                }
                return _onlinePlaylist;
            }
        }

        /// <summary>
        /// Custom name for the tab (suppresses automatic title updates when set)
        /// </summary>
        public string CustomName { get; set; }

        public BrowserTab(CoreWebView2Environment environment, bool isIncognito = false)
        {
            _environment = environment;
            _webView = new WebView2();
            IsIncognito = isIncognito;
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

                // Apply mute state if already set
                ApplyMuteState();

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
        /// Applies the mute state to the WebView2
        /// </summary>
        private void ApplyMuteState()
        {
            if (!_isInitialized || _webView?.CoreWebView2 == null)
                return;

            try
            {
                // Use the native WebView2 IsMuted property for proper audio control
                // This requires Microsoft.Web.WebView2 SDK version 1.0.1661.34 or later
                _webView.CoreWebView2.IsMuted = _isMuted;
            }
            catch (Exception ex)
            {
                // If IsMuted is not available (older WebView2 runtime), fall back to JavaScript
                System.Diagnostics.Debug.WriteLine($"Failed to set native mute state: {ex.Message}");
                ApplyMuteStateViaJavaScript();
            }
        }

        /// <summary>
        /// Fallback method to apply mute state via JavaScript (less reliable)
        /// </summary>
        private async void ApplyMuteStateViaJavaScript()
        {
            if (!_isInitialized || _webView?.CoreWebView2 == null)
                return;

            try
            {
                // Set audio mute state using JavaScript
                string script = _isMuted 
                    ? @"
                        (function() {
                            document.querySelectorAll('video, audio').forEach(function(el) {
                                el.muted = true;
                            });
                        })();
                      "
                    : @"
                        (function() {
                            document.querySelectorAll('video, audio').forEach(function(el) {
                                el.muted = false;
                            });
                        })();
                      ";

                await _webView.CoreWebView2.ExecuteScriptAsync(script);
            }
            catch
            {
                // Silently ignore script execution errors
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
            
            // Only record URL in history if NOT in incognito mode
            if (!IsIncognito)
            {
                UrlHistoryManager.RecordUrl(e.Uri);
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _currentUrl = _webView.Source?.ToString() ?? _currentUrl;
            UrlChanged?.Invoke(this, _webView.Source?.ToString() ?? "");
            
            // Signal that navigation is complete
            _navigationCompletionSource?.TrySetResult(false);

            // Reapply mute state after navigation
            ApplyMuteState();
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
                        
                    }
                    else if (json.Contains("\"loadNotepad\""))
                    {
                        // Handle loading a different notepad
                        
                    }
                    else if (json.Contains("\"exportNotepad\""))
                    {
                        // Handle notepad export
                        
                    }
                    else if (json.Contains("\"openExternalUrl\""))
                    {
                        // Handle opening URL in external browser
                        HandleOpenExternalUrl(json);
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
                    else if (json.Contains("\"loadNote\""))
                    {
                        HandleLoadNote(json);
                    }
                    else if (json.Contains("\"saveNote\""))
                    {
                        HandleSaveNote(json);
                    }
                    else if (json.Contains("\"exportNote\""))
                    {
                        HandleExportNote(json);
                    }
                    else if (json.Contains("\"loadImage\""))
                    {
                        HandleLoadImage(json);
                    }
                    else if (json.Contains("\"saveImage\""))
                    {
                        HandleSaveImage(json);
                    }
                    else if (json.Contains("\"deleteImage\""))
                    {
                        HandleDeleteImage(json);
                    }
                    else if (json.Contains("\"exportImage\""))
                    {
                        HandleExportImage(json);
                    }
                    else if (json.Contains("\"loadUrlList\""))
                    {
                        HandleLoadUrlList(json);
                    }
                    else if (json.Contains("\"saveUrlList\""))
                    {
                        HandleSaveUrlList(json);
                    }
                    else if (json.Contains("\"exportUrlList\""))
                    {
                        HandleExportUrlList(json);
                    }
                }
            }
            catch
            {
                // Ignore message parsing errors
            }
        }

        /// <summary>
        /// Handles opening a URL in an external browser
        /// </summary>
        private void HandleOpenExternalUrl(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    if (document.RootElement.TryGetProperty("url", out JsonElement urlElement))
                    {
                        var url = urlElement.GetString();
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            // Open URL in default browser
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = url,
                                UseShellExecute = true
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to open URL: {ex.Message}",
                    "Error",
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
        /// Handles loading a note from JavaScript
        /// </summary>
        private async void HandleLoadNote(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int noteNumber = 1;
                    if (document.RootElement.TryGetProperty("noteNumber", out JsonElement noteElement))
                    {
                        noteNumber = noteElement.GetInt32();
                    }
                    
                    // Load from disk
                    var noteData = NotepadManager.LoadNote(noteNumber);
                    var content = noteData.Content ?? string.Empty;
                    
                    // Escape for JavaScript
                    var escaped = NotepadHelper.EscapeForJavaScript(content);
                    
                    // Call loadNote function directly
                    var script = $"if (typeof loadNote === 'function') loadNote(\"{escaped}\");";
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading note: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles saving a note from JavaScript
        /// </summary>
        private void HandleSaveNote(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int noteNumber = 1;
                    if (document.RootElement.TryGetProperty("noteNumber", out JsonElement noteElement))
                    {
                        noteNumber = noteElement.GetInt32();
                    }
                    
                    if (document.RootElement.TryGetProperty("content", out JsonElement contentElement))
                    {
                        var content = contentElement.GetString() ?? string.Empty;
                        NotepadManager.SaveNote(noteNumber, content);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving note: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles exporting a note from JavaScript
        /// </summary>
        private void HandleExportNote(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int noteNumber = 1;
                    if (document.RootElement.TryGetProperty("noteNumber", out JsonElement noteElement))
                    {
                        noteNumber = noteElement.GetInt32();
                    }
                    
                    if (document.RootElement.TryGetProperty("content", out JsonElement contentElement))
                    {
                        var content = contentElement.GetString() ?? string.Empty;
                        
                        using (var dialog = new SaveFileDialog())
                        {
                            dialog.Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*";
                            dialog.DefaultExt = "txt";
                            dialog.FileName = $"Note_{noteNumber}_{DateTime.Now:yyyy-MM-dd_HHmmss}.txt";
                            dialog.Title = "Export Note";
                            
                            if (dialog.ShowDialog() == DialogResult.OK)
                            {
                                if (NotepadManager.ExportNote(content, dialog.FileName))
                                {
                                    MessageBox.Show(
                                        $"Note exported successfully to:\n{dialog.FileName}",
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
                    $"Failed to export note: {ex.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Handles loading an image from JavaScript
        /// </summary>
        private async void HandleLoadImage(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int imageNumber = 1;
                    if (document.RootElement.TryGetProperty("imageNumber", out JsonElement imageElement))
                    {
                        imageNumber = imageElement.GetInt32();
                    }
                    
                    // Load from disk
                    var base64Data = ImagePadManager.LoadImage(imageNumber);
                    
                    // Escape for JavaScript (handle null case)
                    var escaped = base64Data != null 
                        ? base64Data.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r")
                        : "null";
                    
                    // Call loadImage function - pass null if no image exists
                    var script = base64Data != null
                        ? $"if (typeof loadImage === 'function') loadImage(\"{escaped}\");"
                        : "if (typeof loadImage === 'function') loadImage(null);";
                    
                    await _webView.CoreWebView2.ExecuteScriptAsync(script);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading image: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles saving an image from JavaScript
        /// </summary>
        private void HandleSaveImage(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int imageNumber = 1;
                    if (document.RootElement.TryGetProperty("imageNumber", out JsonElement imageElement))
                    {
                        imageNumber = imageElement.GetInt32();
                    }
                    
                    if (document.RootElement.TryGetProperty("base64Data", out JsonElement dataElement))
                    {
                        var base64Data = dataElement.GetString() ?? string.Empty;
                        ImagePadManager.SaveImage(imageNumber, base64Data);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving image: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles deleting an image from JavaScript
        /// </summary>
        private void HandleDeleteImage(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int imageNumber = 1;
                    if (document.RootElement.TryGetProperty("imageNumber", out JsonElement imageElement))
                    {
                        imageNumber = imageElement.GetInt32();
                    }
                    
                    ImagePadManager.DeleteImage(imageNumber);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting image: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles exporting an image from JavaScript
        /// </summary>
        private void HandleExportImage(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int imageNumber = 1;
                    if (document.RootElement.TryGetProperty("imageNumber", out JsonElement imageElement))
                    {
                        imageNumber = imageElement.GetInt32();
                    }
                    
                    if (!ImagePadManager.ImageExists(imageNumber))
                    {
                        // Marshal to UI thread for MessageBox
                        if (_webView.InvokeRequired)
                        {
                            _webView.BeginInvoke(new Action(() =>
                            {
                                MessageBox.Show(
                                    "No image to export.",
                                    "Export Image",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information
                                );
                            }));
                        }
                        else
                        {
                            MessageBox.Show(
                                "No image to export.",
                                "Export Image",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                        }
                        return;
                    }
                    
                    // CRITICAL FIX: Use a timer to defer the dialog show until after WebView2 message processing completes
                    // This prevents COM threading issues that cause native crashes
                    var timer = new System.Windows.Forms.Timer();
                    timer.Interval = 10; // Very short delay, just enough to let WebView2 finish processing
                    timer.Tick += (s, e) =>
                    {
                        timer.Stop();
                        timer.Dispose();
                        ShowExportDialog(imageNumber);
                    };
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in HandleExportImage: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // Marshal error message to UI thread
                if (_webView.InvokeRequired)
                {
                    _webView.BeginInvoke(new Action(() =>
                    {
                        MessageBox.Show(
                            $"Failed to export image: {ex.Message}",
                            "Export Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }));
                }
                else
                {
                    MessageBox.Show(
                        $"Failed to export image: {ex.Message}",
                        "Export Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
        }

        /// <summary>
        /// Shows the export dialog (must be called on UI thread)
        /// </summary>
        private void ShowExportDialog(int imageNumber)
        {
            SaveFileDialog dialog = null;
            try
            {
                System.Diagnostics.Debug.WriteLine($"ShowExportDialog: Starting for image {imageNumber}");
                
                dialog = new SaveFileDialog();
                dialog.Filter = "PNG Image (*.png)|*.png|" +
                              "JPEG Image (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                              "BMP Image (*.bmp)|*.bmp|" +
                              "All Files (*.*)|*.*";
                dialog.DefaultExt = "png";
                dialog.FileName = $"Image_{imageNumber}_{DateTime.Now:yyyy-MM-dd_HHmmss}";
                dialog.Title = "Export Image";
                
                try
                {
                    dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to set InitialDirectory: {ex.Message}");
                    // Continue without initial directory
                }
                
                System.Diagnostics.Debug.WriteLine("ShowExportDialog: About to show dialog");
                
                DialogResult result;
                try
                {
                    // Get the top-level form that contains this WebView2
                    var parentForm = _webView.FindForm();
                    
                    if (parentForm != null)     
                    {
                        result = dialog.ShowDialog(parentForm);
                    }
                    else
                    {
                        // Fallback if we can't find the parent form
                        result = dialog.ShowDialog();
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"ShowExportDialog: Dialog closed with result: {result}");
                }
                catch (Exception dialogEx)
                {
                    System.Diagnostics.Debug.WriteLine($"EXCEPTION in ShowDialog: {dialogEx.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"Message: {dialogEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {dialogEx.StackTrace}");
                    
                    MessageBox.Show(
                        $"Error showing file dialog:\n{dialogEx.GetType().Name}\n{dialogEx.Message}",
                        "Dialog Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    return;
                }
                
                if (result == DialogResult.OK)
                {
                    try
                    {
                        System.Diagnostics.Debug.WriteLine($"ShowExportDialog: User selected: {dialog.FileName}");
                        
                        // Determine format from filter index
                        System.Drawing.Imaging.ImageFormat format = dialog.FilterIndex switch
                        {
                            1 => System.Drawing.Imaging.ImageFormat.Png,
                            2 => System.Drawing.Imaging.ImageFormat.Jpeg,
                            3 => System.Drawing.Imaging.ImageFormat.Bmp,
                            _ => System.Drawing.Imaging.ImageFormat.Png
                        };
                        
                        System.Diagnostics.Debug.WriteLine($"ShowExportDialog: Exporting as format: {format}");
                        
                        if (ImagePadManager.ExportImage(imageNumber, dialog.FileName, format))
                        {
                            MessageBox.Show(
                                $"Image exported successfully to:\n{dialog.FileName}",
                                "Export Complete",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information
                            );
                        }
                        else
                        {
                            MessageBox.Show(
                                "Failed to export image.",
                                "Export Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Export failed: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        MessageBox.Show(
                            $"Failed to export image:\n{ex.Message}",
                            "Export Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ShowExportDialog: User cancelled");
                }
            }
            catch (Exception outerEx)
            {
                System.Diagnostics.Debug.WriteLine($"OUTER EXCEPTION in ShowExportDialog: {outerEx.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Message: {outerEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {outerEx.StackTrace}");
                
                MessageBox.Show(
                    $"Unexpected error in export dialog:\n{outerEx.GetType().Name}\n{outerEx.Message}",
                    "Unexpected Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
            finally
            {
                dialog?.Dispose();
                System.Diagnostics.Debug.WriteLine("ShowExportDialog: Completed");
            }
        }

        /// <summary>
        /// Handles loading a URL list from JavaScript
        /// </summary>
        private async void HandleLoadUrlList(string json)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"HandleLoadUrlList: Received JSON: {json}");
                
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int urlListNumber = 1;
                    if (document.RootElement.TryGetProperty("urlListNumber", out JsonElement listElement))
                    {
                        urlListNumber = listElement.GetInt32();
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"HandleLoadUrlList: Loading URL list number: {urlListNumber}");
                    
                    // Load from disk
                    var urlList = UrlPadManager.LoadUrlList(urlListNumber);
                    
                    if (urlList != null)
                    {
                        System.Diagnostics.Debug.WriteLine($"HandleLoadUrlList: Loaded - Title: {urlList.Title}, URL count: {urlList.Urls?.Count ?? 0}");
                        
                        // Serialize to JSON for JavaScript with camelCase property names
                        var jsonOptions = new JsonSerializerOptions 
                        { 
                            WriteIndented = false,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };
                        var urlListJson = JsonSerializer.Serialize(urlList, jsonOptions);
                        
                        System.Diagnostics.Debug.WriteLine($"HandleLoadUrlList: Serialized JSON (camelCase): {urlListJson}");
                        
                        // Use JsonSerializer to create a properly escaped string for JavaScript
                        var escapedJson = JsonSerializer.Serialize(urlListJson);
                        
                        // Call loadUrlList function - escapedJson is already a quoted, escaped string
                        var script = $"if (typeof loadUrlList === 'function') loadUrlList(JSON.parse({escapedJson}));";
                        
                        System.Diagnostics.Debug.WriteLine($"HandleLoadUrlList: Executing script: {script}");
                        
                        await _webView.CoreWebView2.ExecuteScriptAsync(script);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"HandleLoadUrlList: No saved data found for list #{urlListNumber}, sending null");
                        
                        // No saved data - send null
                        var script = "if (typeof loadUrlList === 'function') loadUrlList(null);";
                        await _webView.CoreWebView2.ExecuteScriptAsync(script);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading URL list: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Handles saving a URL list from JavaScript
        /// </summary>
        private void HandleSaveUrlList(string json)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"HandleSaveUrlList: Received JSON: {json}");
                
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int urlListNumber = 1;
                    if (document.RootElement.TryGetProperty("urlListNumber", out JsonElement listElement))
                    {
                        urlListNumber = listElement.GetInt32();
                    }
                    
                    System.Diagnostics.Debug.WriteLine($"HandleSaveUrlList: URL list number: {urlListNumber}");
                    
                    if (document.RootElement.TryGetProperty("urlList", out JsonElement urlListElement))
                    {
                        var urlListJson = urlListElement.GetRawText();
                        System.Diagnostics.Debug.WriteLine($"HandleSaveUrlList: URL list JSON: {urlListJson}");
                        
                        // Configure deserializer to handle camelCase from JavaScript
                        var deserializeOptions = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var urlList = JsonSerializer.Deserialize<UrlList>(urlListJson, deserializeOptions);
                        
                        if (urlList != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"HandleSaveUrlList: Deserialized - Title: {urlList.Title}, URL count: {urlList.Urls?.Count ?? 0}");
                            
                            if (urlList.Urls != null)
                            {
                                foreach (var url in urlList.Urls)
                                {
                                    System.Diagnostics.Debug.WriteLine($"  URL: {url.Url}, Display: {url.DisplayText}");
                                }
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("HandleSaveUrlList: Deserialized urlList is NULL");
                        }
                        
                        bool saveResult = UrlPadManager.SaveUrlList(urlListNumber, urlList);
                        System.Diagnostics.Debug.WriteLine($"HandleSaveUrlList: Save result: {saveResult}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("HandleSaveUrlList: No 'urlList' property found in JSON");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving URL list: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Handles exporting a URL list from JavaScript
        /// </summary>
        private void HandleExportUrlList(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    int urlListNumber = 1;
                    if (document.RootElement.TryGetProperty("urlListNumber", out JsonElement listElement))
                    {
                        urlListNumber = listElement.GetInt32();
                    }
                    
                    if (!UrlPadManager.UrlListExists(urlListNumber))
                    {
                        MessageBox.Show(
                            "No URLs to export.",
                            "Export URLs",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information
                        );
                        return;
                    }
                    
                    using (var dialog = new SaveFileDialog())
                    {
                        dialog.Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*";
                        dialog.DefaultExt = "json";
                        dialog.FileName = $"UrlPad_{urlListNumber:D4}_{DateTime.Now:yyyy-MM-dd_HHmmss}.json";
                        dialog.Title = "Export URLs";
                        
                        if (dialog.ShowDialog() == DialogResult.OK)
                        {
                            if (UrlPadManager.ExportUrlList(urlListNumber, dialog.FileName))
                            {
                                MessageBox.Show(
                                    $"URLs exported successfully to:\n{dialog.FileName}",
                                    "Export Complete",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Information
                                );
                            }
                            else
                            {
                                MessageBox.Show(
                                    "Failed to export URLs.",
                                    "Export Error",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Error
                                );
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to export URLs: {ex.Message}",
                    "Export Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Gets the type of playlist currently active (if any)
        /// </summary>
        public PlaylistType CurrentPlaylistType
        {
            get
            {
                if (_playlist != null && _playlist.Count > 0)
                    return PlaylistType.Local;
                if (_onlinePlaylist != null && _onlinePlaylist.Count > 0)
                    return PlaylistType.Online;
                return PlaylistType.None;
            }
        }

        public void Dispose()
        {
            _webView?.Dispose();
        }
    }

    public enum PlaylistType
    {
        None,
        Local,
        Online
    }
}
