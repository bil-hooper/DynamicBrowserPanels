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
    }
}
