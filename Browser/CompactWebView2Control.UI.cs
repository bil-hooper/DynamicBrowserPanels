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
            
            // URL TextBox
            txtUrl.Dock = DockStyle.Top;
            txtUrl.Location = new Point(0, 0);
            txtUrl.Name = "txtUrl";
            txtUrl.Size = new Size(800, 25);
            txtUrl.TabIndex = 0;
            txtUrl.KeyDown += TxtUrl_KeyDown;
            txtUrl.Enter += TxtUrl_Enter;
            
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

            // Define alternating colors - subtle tones
            Color evenColor = Color.FromArgb(252, 252, 255);      // Even lighter
            Color oddColor = Color.FromArgb(250, 248, 252);       // Even lighter
            Color selectedEvenColor = Color.FromArgb(230, 230, 245); // Slightly darker blue-white
            Color selectedOddColor = Color.FromArgb(235, 225, 245);  // Slightly darker purple-white

            // Determine if this tab is selected
            bool isSelected = (e.Index == tabControl.SelectedIndex);

            // Choose color based on index (even/odd) and selection state
            Color backColor;
            if (isSelected)
            {
                backColor = (e.Index % 2 == 0) ? selectedEvenColor : selectedOddColor;
            }
            else
            {
                backColor = (e.Index % 2 == 0) ? evenColor : oddColor;
            }

            // Fill the tab background
            using (SolidBrush brush = new SolidBrush(backColor))
            {
                g.FillRectangle(brush, tabBounds);
            }

            // Draw border around selected tab
            if (isSelected)
            {
                using (Pen pen = new Pen(Color.FromArgb(180, 180, 220), 2))
                {
                    g.DrawRectangle(pen, tabBounds.X, tabBounds.Y, tabBounds.Width - 1, tabBounds.Height - 1);
                }
            }
            else
            {
                // Draw subtle border for non-selected tabs
                using (Pen pen = new Pen(Color.FromArgb(220, 220, 230), 1))
                {
                    g.DrawRectangle(pen, tabBounds.X, tabBounds.Y, tabBounds.Width - 1, tabBounds.Height - 1);
                }
            }

            // Draw the tab text
            StringFormat stringFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            using (SolidBrush textBrush = new SolidBrush(Color.Black))
            {
                g.DrawString(tabPage.Text, tabControl.Font, textBrush, tabBounds, stringFormat);
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