namespace DynamicBrowserPanels
{
    /// <summary>
    /// Represents the complete state of the browser form
    /// </summary>
    public class BrowserState
    {
        public int FormWidth { get; set; }
        public int FormHeight { get; set; }
        public int FormX { get; set; }
        public int FormY { get; set; }
        public PanelState RootPanel { get; set; }

        // Password protection for templates
        public string PasswordHash { get; set; } // SHA256 hash of password (saved cleartext in JSON)

        public BrowserState()
        {
            FormWidth = 1184;
            FormHeight = 761;
            FormX = -1;
            FormY = -1;
            RootPanel = new PanelState();
            PasswordHash = null; // Null means no password protection
        }
    }
}
