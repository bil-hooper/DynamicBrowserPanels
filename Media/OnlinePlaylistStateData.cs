using System.Collections.Generic;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// State data for online media playlists
    /// </summary>
    public class OnlinePlaylistStateData
    {
        public List<OnlineMediaItem> MediaItems { get; set; }
        public int CurrentIndex { get; set; }
        public bool Shuffle { get; set; }
        public bool Repeat { get; set; }
        public string PlaylistName { get; set; }
        
        /// <summary>
        /// Machine name where this playlist was created (automatically set for online playlists)
        /// </summary>
        public string MachineName { get; set; }
    }
}