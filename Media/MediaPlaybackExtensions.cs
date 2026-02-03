using System;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Example extension methods for CompactWebView2Control
    /// </summary>
    public static class MediaPlaybackExtensions
    {
        /// <summary>
        /// Plays a local media file with validation
        /// </summary>
        public static void PlayMediaFile(this CompactWebView2Control browser, string filePath)
        {
            // Validate file
            if (!LocalMediaHelper.ValidateMediaFile(filePath, out string errorMessage))
            {
                MessageBox.Show(
                    errorMessage,
                    "Cannot Play Media",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                
                // Suggest conversion if format is unsupported
                var suggestion = LocalMediaHelper.GetConversionSuggestion(filePath);
                if (suggestion != null)
                {
                    MessageBox.Show(
                        suggestion,
                        "Conversion Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                return;
            }

            // Show warning for large files
            if (errorMessage != null)
            {
                var result = MessageBox.Show(
                    errorMessage + "\n\nContinue anyway?",
                    "Warning",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning
                );
                
                if (result != DialogResult.Yes)
                    return;
            }

            try
            {
                // Create HTML player page
                var tempHtmlPath = LocalMediaHelper.CreateTemporaryPlayerFile(filePath);
                var url = LocalMediaHelper.FilePathToUrl(tempHtmlPath);
                
                // Navigate to player
                browser.NavigateToUrl(url);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error playing media file:\n{ex.Message}",
                    "Playback Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Opens a dialog to select and play a media file
        /// </summary>
        public static void OpenAndPlayMediaFile(this CompactWebView2Control browser)
        {
            var filePath = LocalMediaHelper.OpenMediaFileDialog();
            if (!string.IsNullOrEmpty(filePath))
            {
                browser.PlayMediaFile(filePath);
            }
        }
    }
}
