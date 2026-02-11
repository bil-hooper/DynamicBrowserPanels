using System.Collections.Generic;

namespace DynamicBrowserPanels
{
    public class PlaylistStateData
    {
        public List<string> MediaFiles { get; set; }
        public int CurrentIndex { get; set; }
        public bool Shuffle { get; set; }
        public bool Repeat { get; set; }
        public string PlaylistName { get; set; }
    }
}