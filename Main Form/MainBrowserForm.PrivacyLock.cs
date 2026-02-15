using System;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    partial class MainBrowserForm
    {
        /// <summary>
        /// Initializes privacy lock functionality
        /// </summary>
        private void InitializePrivacyLock()
        {
            PrivacyLockManager.Instance.LockRequested += OnPrivacyLockRequested;
            PrivacyLockManager.Instance.UnlockSuccessful += OnPrivacyUnlocked;
            
            // Add keyboard shortcut
            this.KeyPreview = true;
            this.KeyDown += OnKeyDown;
        }

        /// <summary>
        /// Handles keyboard shortcuts
        /// </summary>
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.L)
            {
                e.Handled = true;
                OnLockClick(sender, e);
            }
        }

        /// <summary>
        /// Handles lock button click
        /// </summary>
        private void OnLockClick(object sender, EventArgs e)
        {
            if (!PrivacyLockManager.Instance.IsEnabled)
            {
                var result = MessageBox.Show(
                    "Privacy lock is not configured. Would you like to set it up now?",
                    "Privacy Lock",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    using (var settings = new PrivacyLockSettingsForm())
                    {
                        settings.ShowDialog(this);
                    }
                }
                return;
            }
            
            PrivacyLockManager.Instance.Lock();
        }

        /// <summary>
        /// Handles privacy lock requested event
        /// </summary>
        private void OnPrivacyLockRequested(object sender, EventArgs e)
        {
            // Hide all browser controls
            HideAllBrowserControls(rootPanel);
            
            _privacyOverlay = new PrivacyLockOverlay();
            _privacyOverlay.FormClosed += (s, args) => OnPrivacyUnlocked(s, args);
            _privacyOverlay.ShowDialog(this);
        }

        /// <summary>
        /// Handles privacy unlock event
        /// </summary>
        private void OnPrivacyUnlocked(object sender, EventArgs e)
        {
            // Restore all browser controls
            ShowAllBrowserControls(rootPanel);
            
            if (_privacyOverlay != null)
            {
                _privacyOverlay.Dispose();
                _privacyOverlay = null;
            }
        }

        /// <summary>
        /// Recursively hides all CompactWebView2Control instances
        /// </summary>
        private void HideAllBrowserControls(Control control)
        {
            if (control is CompactWebView2Control browser)
            {
                browser.Visible = false;
                return;
            }

            if (control is SplitContainer splitContainer)
            {
                HideAllBrowserControls(splitContainer.Panel1);
                HideAllBrowserControls(splitContainer.Panel2);
                return;
            }

            foreach (Control child in control.Controls)
            {
                HideAllBrowserControls(child);
            }
        }

        /// <summary>
        /// Recursively shows all CompactWebView2Control instances
        /// </summary>
        private void ShowAllBrowserControls(Control control)
        {
            if (control is CompactWebView2Control browser)
            {
                browser.Visible = true;
                return;
            }

            if (control is SplitContainer splitContainer)
            {
                ShowAllBrowserControls(splitContainer.Panel1);
                ShowAllBrowserControls(splitContainer.Panel2);
                return;
            }

            foreach (Control child in control.Controls)
            {
                ShowAllBrowserControls(child);
            }
        }
    }
}
