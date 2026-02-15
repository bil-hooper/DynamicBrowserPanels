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
            tabControl = new TabControl();
            txtUrl = new TextBox();
            
            SuspendLayout();
            
            // TabControl
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 25);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(800, 575);
            tabControl.TabIndex = 1;
            
            // Enable owner-draw for custom tab colors
            tabControl.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged; 
            
            // URL TextBox
            txtUrl.Dock = DockStyle.Top;
            txtUrl.Location = new Point(0, 0);
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(800, 25);
            txtUrl.TabIndex = 0;
            txtUrl.KeyDown += TxtUrl_KeyDown;
            txtUrl.Enter += TxtUrl_Enter;
            
            // Create the context menu BEFORE setting it
            CreateContextMenu();
            
            // Context menu for the URL bar
            txtUrl.ContextMenuStrip = contextMenu;
            
            // CompactWebView2Control
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            Controls.Add(tabControl);
            Controls.Add(txtUrl);
            Name = "CompactWebView2Control";
            Size = new Size(800, 600);
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>
        /// Handles custom drawing of tabs with alternating colors
        /// </summary>
        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;
            TabPage tabPage = tabControl.TabPages[e.Index];
            Rectangle tabBounds = tabControl.GetTabRect(e.Index);

            // Determine if this tab is selected
            bool isSelected = (e.Index == tabControl.SelectedIndex);

            // Choose color based on index (even/odd) and selection state using AppColors
            Color backColor;
            if (isSelected)
            {
                backColor = (e.Index % 2 == 0) ? AppColors.TabSelectedEven : AppColors.TabSelectedOdd;
            }
            else
            {
                backColor = (e.Index % 2 == 0) ? AppColors.TabEven : AppColors.TabOdd;
            }

            // Fill the tab background
            using (SolidBrush brush = new SolidBrush(backColor))
            {
                g.FillRectangle(brush, tabBounds);
            }

            // Draw border around selected tab
            if (isSelected)
            {
                using (Pen pen = new Pen(AppColors.TabBorderSelected, 2))
                {
                    g.DrawRectangle(pen, tabBounds.X, tabBounds.Y, tabBounds.Width - 1, tabBounds.Height - 1);
                }
            }
            else
            {
                // Draw subtle border for non-selected tabs
                using (Pen pen = new Pen(AppColors.TabBorderNormal, 1))
                {
                    g.DrawRectangle(pen, tabBounds.X, tabBounds.Y, tabBounds.Width - 1, tabBounds.Height - 1);
                }
            }

            // Draw the tab text with strikethrough if muted
            StringFormat stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            // Check if tab is muted
            bool isMuted = e.Index >= 0 && e.Index < _browserTabs.Count && _browserTabs[e.Index].IsMuted;
            Font textFont = isMuted 
                ? new Font(tabControl.Font, FontStyle.Strikeout) 
                : tabControl.Font;

            using (SolidBrush textBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(tabPage.Text, textFont, textBrush, tabBounds, stringFormat);
            }

            if (isMuted)
            {
                textFont.Dispose();
            }
        }

        /// <summary>
        /// Handles tab selection changes
        /// </summary>
        private void TabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            var currentTab = GetCurrentTab();
            if (currentTab != null)
            {
                // Update URL bar to show the current tab's URL
                txtUrl.Text = currentTab.CurrentUrl;
            }
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
        /// Selects all text in the URL textbox when it receives focus
        /// </summary>
        private void TxtUrl_Enter(object sender, System.EventArgs e)
        {
            txtUrl.SelectAll();
        }

        /// <summary>
        /// Handles URL changes from a browser tab
        /// </summary>
        private void BrowserTab_UrlChanged(object sender, string url)
        {
            var tab = sender as BrowserTab;
            if (tab == null) return;

            // Only update URL bar if this is the currently selected tab
            int tabIndex = _browserTabs.IndexOf(tab);
            if (tabIndex >= 0 && tabIndex == tabControl.SelectedIndex)
            {
                txtUrl.Text = url;
            }
        }

        /// <summary>
        /// Handles title changes from a browser tab
        /// </summary>
        private void BrowserTab_TitleChanged(object sender, string title)
        {
            var tab = sender as BrowserTab;
            if (tab == null) return;

            // Find the tab index
            int tabIndex = _browserTabs.IndexOf(tab);
            if (tabIndex < 0 || tabIndex >= tabControl.TabPages.Count)
                return;

            // Only update the tab text if there's no custom name set
            if (string.IsNullOrWhiteSpace(tab.CustomName))
            {
                var tabPage = tabControl.TabPages[tabIndex];
                
                // Truncate long titles
                string displayTitle = title;
                if (displayTitle.Length > GlobalConstants.MAX_TAB_TITLE_LENGTH)
                {
                    displayTitle = displayTitle.Substring(0, GlobalConstants.TITLE_TRUNCATE_LENGTH) + "...";
                }
                
                // Preserve incognito prefix if tab is incognito
                string incognitoPrefix = tab.IsIncognito ? "🕶️ " : "";
                tabPage.Text = incognitoPrefix + displayTitle;
            }
            // If custom name is set, do nothing - the custom name takes precedence
        }
    }
}