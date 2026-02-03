using System;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Event args for split request
    /// </summary>
    public class SplitRequestedEventArgs : EventArgs
    {
        public Orientation Orientation { get; }

        public SplitRequestedEventArgs(Orientation orientation)
        {
            Orientation = orientation;
        }
    }
}
