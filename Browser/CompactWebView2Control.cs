using Microsoft.Web.WebView2.WinForms;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Compact WebView2 browser control with tab support
    /// </summary>
    public partial class CompactWebView2Control : UserControl
    {
        // UI Components
        private TabControl tabControl;
        private TextBox txtUrl;
        private ContextMenuStrip contextMenu;

        // Context menu items
        private ToolStripMenuItem mnuBack;
        private ToolStripMenuItem mnuForward;
        private ToolStripMenuItem mnuRefresh;
        private ToolStripMenuItem mnuHome;
        private ToolStripSeparator separator1;
        private ToolStripMenuItem mnuOpenMedia;
        private ToolStripMenuItem mnuPlaylistControls;
        private ToolStripSeparator separator1b;
        private ToolStripMenuItem mnuOpenNotepad;
        private ToolStripMenuItem mnuTimer;
        private ToolStripMenuItem mnuNewTab;
        private ToolStripMenuItem mnuCloseTab;
        private ToolStripMenuItem mnuRenameTab;
        private ToolStripMenuItem mnuMoveTabLeft;
        private ToolStripMenuItem mnuMoveTabRight;
        private ToolStripMenuItem mnuMoveTabToStart;
        private ToolStripMenuItem mnuMoveTabToEnd;
        private ToolStripSeparator separator2;
        private ToolStripMenuItem mnuSplitHorizontal;
        private ToolStripMenuItem mnuSplitVertical;
        private ToolStripSeparator separator3;
        private ToolStripMenuItem mnuSaveLayoutDirect;
        private ToolStripMenuItem mnuSaveLayoutAs;
        private ToolStripMenuItem mnuLoadLayout;
        private ToolStripSeparator separator4;
        private ToolStripMenuItem mnuResetLayout;
        private ToolStripMenuItem mnuManagePasswords;
        private ToolStripMenuItem mnuInstall;
        private ToolStripMenuItem mnuUninstall;

        // Tab management
        private List<BrowserTab> _browserTabs = new List<BrowserTab>();
        private List<string> _tabCustomNames = new List<string>();

        // Shared WebView2 environment
        private static CoreWebView2Environment _sharedEnvironment;

        // Properties
        public string HomeUrl { get; set; } = GlobalConstants.DEFAULT_URL;
        public string CurrentUrl => GetCurrentTab()?.CurrentUrl ?? HomeUrl;

        // Events
        public event EventHandler<SplitRequestedEventArgs> SplitRequested;
        public event EventHandler ResetLayoutRequested;
        public event EventHandler SaveLayoutDirectRequested;
        public event EventHandler SaveLayoutAsRequested;
        public event EventHandler LoadLayoutRequested;
        public event EventHandler<TimeSpan> TimerRequested;
        public event EventHandler TimerStopRequested;

        public CompactWebView2Control()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Navigates the current tab to the specified URL
        /// </summary>
        public void NavigateToUrl(string url)
        {
            var currentTab = GetCurrentTab();
            if (currentTab != null)
            {
                _ = currentTab.NavigateToUrl(url);
            }
        }

        /// <summary>
        /// Goes back in navigation history for current tab
        /// </summary>
        public void GoBack()
        {
            GetCurrentTab()?.GoBack();
        }

        /// <summary>
        /// Goes forward in navigation history for current tab
        /// </summary>
        public void GoForward()
        {
            GetCurrentTab()?.GoForward();
        }

        /// <summary>
        /// Refreshes the current tab
        /// </summary>
        public void Refresh()
        {
            GetCurrentTab()?.Refresh();
        }

        /// <summary>
        /// Navigates current tab to the home URL
        /// </summary>
        public void GoHome()
        {
            NavigateToUrl(HomeUrl);
        }

        /// <summary>
        /// Gets the currently active browser tab
        /// </summary>
        private BrowserTab GetCurrentTab()
        {
            int selectedIndex = tabControl.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex < _browserTabs.Count)
            {
                return _browserTabs[selectedIndex];
            }
            return null;
        }

        /// <summary>
        /// Gets or creates the shared WebView2 environment
        /// </summary>
        private static async Task<CoreWebView2Environment> GetSharedEnvironment()
        {
            if (_sharedEnvironment == null)
            {
                var userDataFolder = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DynamicBrowserPanels",
                    "WebView2Data"
                );

                var options = new CoreWebView2EnvironmentOptions
                {
                    AdditionalBrowserArguments = "--enable-features=msWebView2EnableEdgeInternalSchemes"
                };

                _sharedEnvironment = await CoreWebView2Environment.CreateAsync(
                    null, // browserExecutableFolder
                    userDataFolder,
                    options
                );
            }
            return _sharedEnvironment;
        }

        /// <summary>
        /// Raises the split requested event
        /// </summary>
        private void OnSplitRequested(Orientation orientation)
        {
            SplitRequested?.Invoke(this, new SplitRequestedEventArgs(orientation));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var tab in _browserTabs)
                {
                    tab?.Dispose();
                }
                _browserTabs.Clear();

                tabControl?.Dispose();
                contextMenu?.Dispose();
                txtUrl?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
