using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    partial class MainBrowserForm
    {
        /// <summary>
        /// Handles timer elapsed event - pauses all media playback
        /// </summary>
        private async void TimerManager_TimerElapsed(object sender, EventArgs e)
        {
            await PauseAllMediaAsync();
        }

        /// <summary>
        /// Pauses all media (playlists and videos) across all browser panels
        /// </summary>
        private async Task PauseAllMediaAsync()
        {
            var browsers = new List<CompactWebView2Control>();
            CollectAllBrowserControls(rootPanel, browsers);

            // Pause all browsers concurrently
            var pauseTasks = new List<Task>();
            foreach (var browser in browsers)
            {
                pauseTasks.Add(browser.PauseAllMediaAsync());
            }

            await Task.WhenAll(pauseTasks);
        }

        /// <summary>
        /// Recursively collects all CompactWebView2Control instances from a panel
        /// </summary>
        private void CollectAllBrowserControls(Control control, List<CompactWebView2Control> browsers)
        {
            if (control is CompactWebView2Control browser)
            {
                browsers.Add(browser);
                return;
            }

            if (control is SplitContainer splitContainer)
            {
                CollectAllBrowserControls(splitContainer.Panel1, browsers);
                CollectAllBrowserControls(splitContainer.Panel2, browsers);
                return;
            }

            foreach (Control child in control.Controls)
            {
                CollectAllBrowserControls(child, browsers);
            }
        }

        /// <summary>
        /// Cleanup temporary HTML files created for media playback and error pages
        /// </summary>
        public static void CleanupTempFiles()
        {
            try
            {
                var tempPath = Path.GetTempPath();

                // Clean up old temp files from Windows temp folder (media player HTML files)
                // Only delete files older than 1 day
                var mediaFiles = Directory.GetFiles(tempPath, "webview_media_*.html");
                foreach (var file in mediaFiles)
                {
                    try 
                    { 
                        var fileInfo = new FileInfo(file);
                        if (DateTime.Now - fileInfo.LastWriteTime > TimeSpan.FromDays(1))
                        {
                            File.Delete(file);
                        }
                    } 
                    catch { }
                }

                // Clean up old media error HTML files
                var errorFiles = Directory.GetFiles(tempPath, "media_error_*.html");
                foreach (var file in errorFiles)
                {
                    try 
                    { 
                        var fileInfo = new FileInfo(file);
                        if (DateTime.Now - fileInfo.LastWriteTime > TimeSpan.FromDays(1))
                        {
                            File.Delete(file);
                        }
                    } 
                    catch { }
                }
            }
            catch { }
        }
    }
}
