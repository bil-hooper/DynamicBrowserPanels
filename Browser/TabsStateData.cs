using System.Collections.Generic;

namespace DynamicBrowserPanels
{
    public class TabsStateData
    {
        public int SelectedTabIndex { get; set; }
        public List<string> TabUrls { get; set; } = new List<string>();
        public List<string> TabCustomNames { get; set; } = new List<string>(); 
        public List<PlaylistStateData> TabPlaylists { get; set; } = new List<PlaylistStateData>(); 
    }
}
