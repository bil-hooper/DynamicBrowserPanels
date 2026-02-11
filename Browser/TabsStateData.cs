using System.Collections.Generic;

namespace DynamicBrowserPanels{
    /// <summary>
    /// Represents the state of tabs in a browser control
    /// </summary>
    public class TabsStateData
    {
        public int SelectedTabIndex { get; set; }
        public List<string> TabUrls { get; set; } = new List<string>();
        public List<string> TabCustomNames { get; set; } = new List<string>(); // Add this
    }
}
