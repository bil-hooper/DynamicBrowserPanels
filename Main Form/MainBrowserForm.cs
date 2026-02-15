using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Main browser form with dynamic split panel support
    /// </summary>
    public partial class MainBrowserForm : Form
    {
        private Panel rootPanel;
        private string _lastLoadedFileName;
        private TimerManager _timerManager;
        private LoadingOverlay _loadingOverlay;
        private PrivacyLockOverlay _privacyOverlay;

        public MainBrowserForm()
        {
            InitializeComponent();

            // Runtime initialization - not in InitializeComponent()
            if (!DesignMode)
            {
                InitializeRuntime();
            }
        }

        /// <summary>
        /// Initializes runtime-specific behavior
        /// </summary>
        private void InitializeRuntime()
        {
            // Initialize loading overlay
            _loadingOverlay = new LoadingOverlay();

            // Initialize timer manager
            _timerManager = new TimerManager(this);
            _timerManager.TimerElapsed += TimerManager_TimerElapsed;

            // Set window title based on mode
            if (BrowserStateManager.IsCommandLineMode)
            {
                this.Text = $"{Path.GetFileName(BrowserStateManager.SessionFilePath)} (Read-Only) - Dynamic Browser Panels";
                _timerManager.UpdateOriginalTitle(this.Text);
            }

            // Wire up event handlers
            FormClosing += MainBrowserForm_FormClosing;
            Load += MainBrowserForm_Load;

            // Cleanup temp files
            CleanupTempFiles();

            // Initialize privacy lock
            InitializePrivacyLock();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainBrowserForm));
            rootPanel = new Panel();
            rootPanel.Dock = DockStyle.Fill;
            SuspendLayout();
            // 
            // rootPanel
            // 
            rootPanel.Location = new Point(0, 0);
            rootPanel.Name = "rootPanel";
            rootPanel.Size = new Size(200, 100);
            rootPanel.TabIndex = 0;
            // 
            // MainBrowserForm
            // 
            ClientSize = new Size(1184, 761);
            Controls.Add(rootPanel);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "MainBrowserForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Dynamic Browser Panels";
            KeyPreview = true; // Enable form-level key events for timer dismiss
            ResumeLayout(false);
        }

        /// <summary>
        /// Ensures the window is visible on at least one screen
        /// </summary>
        private void EnsureWindowVisible()
        {
            // Check if the window is on a visible screen
            bool isVisible = false;
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.WorkingArea.IntersectsWith(new Rectangle(this.Location, this.Size)))
                {
                    isVisible = true;
                    break;
                }
            }

            // If not visible, center on primary screen
            if (!isVisible)
            {
                this.StartPosition = FormStartPosition.CenterScreen;
            }
        }

        /// <summary>
        /// Finds the first Panel control in a container
        /// </summary>
        private Panel FindFirstPanel(Control container)
        {
            foreach (Control control in container.Controls)
            {
                if (control is Panel panel)
                    return panel;
            }
            return null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Disable keep awake when closing
                KeepAwakeManager.Disable();
                
                DisposeAllControls(rootPanel);
                _timerManager?.Dispose();
                _loadingOverlay?.Dispose();
            }
            base.Dispose(disposing);
        }   
    }
}
