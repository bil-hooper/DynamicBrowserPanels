using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    public partial class CompactWebView2Control
    {
        /// <summary>
        /// Initializes the UI components
        /// </summary>
        private void InitializeComponent()
        {
            this.SuspendLayout();

            // URL TextBox
            txtUrl = new TextBox
            {
                Dock = DockStyle.Top,
                Height = 25,
                Font = new Font("Segoe UI", 9F)
            };
            txtUrl.KeyDown += TxtUrl_KeyDown;

            // Tab Control
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            // Create context menu
            CreateContextMenu();

            // Add controls
            this.Controls.Add(tabControl);
            this.Controls.Add(txtUrl);
            
            // Attach context menu to URL textbox instead of WebView2
            txtUrl.ContextMenuStrip = contextMenu;

            this.ResumeLayout(false);
        }

        /// <summary>
        /// Handles URL textbox key down events
        /// </summary>
        private void TxtUrl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                NavigateToUrl(txtUrl.Text);
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }

        /// <summary>
        /// Handles tab control selection changes
        /// </summary>
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var currentTab = GetCurrentTab();
            if (currentTab != null)
            {
                txtUrl.Text = currentTab.CurrentUrl;
            }
            UpdateContextMenuButtons();
        }

        /// <summary>
        /// Handles URL changes from browser tabs
        /// </summary>
        private void BrowserTab_UrlChanged(object sender, string url)
        {
            var currentTab = GetCurrentTab();
            if (sender == currentTab)
            {
                if (!txtUrl.Focused)
                {
                    txtUrl.Text = url;
                }
            }
        }

        /// <summary>
        /// Handles title changes from browser tabs
        /// </summary>
        private void BrowserTab_TitleChanged(object sender, string title)
        {
            var tab = sender as BrowserTab;
            if (tab == null) return;
            
            int index = _browserTabs.IndexOf(tab);
            if (index < 0 || index >= tabControl.TabPages.Count) return;
            
            // The event won't fire if tab has CustomName (handled in BrowserTab.cs)
            // So if we're here, we should update the title
            string displayTitle = title ?? $"Tab {index + 1}";
            if (displayTitle.Length > GlobalConstants.MAX_TAB_TITLE_LENGTH)
            {
                displayTitle = displayTitle.Substring(0, GlobalConstants.TITLE_TRUNCATE_LENGTH) + "...";
            }
            tabControl.TabPages[index].Text = displayTitle;
        }
    }
}