namespace DynamicBrowserPanels
{
    /// <summary>
    /// Represents the state of a panel (either a browser or a split container)
    /// </summary>
    public class PanelState
    {
        public PanelState()
        {
        }

        /// <summary>
        /// URL if this is a browser panel (legacy - for single tab)
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Tabs state for multi-tab browser
        /// </summary>
        public TabsStateData TabsState { get; set; }

        /// <summary>
        /// Whether this panel is split
        /// </summary>
        public bool IsSplit { get; set; }

        /// <summary>
        /// Orientation of the split (Horizontal or Vertical)
        /// </summary>
        public string SplitOrientation { get; set; }

        /// <summary>
        /// Splitter distance
        /// </summary>
        public int SplitterDistance { get; set; }

        /// <summary>
        /// Panel size (for calculating splitter distance)
        /// </summary>
        public int PanelSize { get; set; }

        /// <summary>
        /// First panel state (top or left)
        /// </summary>
        public PanelState Panel1 { get; set; }

        /// <summary>
        /// Second panel state (bottom or right)
        /// </summary>
        public PanelState Panel2 { get; set; }
    }
}
