using System;
using System.Collections.Generic;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Represents the saved state of a notepad
    /// </summary>
    public class NotepadData
    {
        /// <summary>
        /// The text content of the notepad
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Last modified timestamp
        /// </summary>
        public DateTime LastModified { get; set; } = DateTime.Now;

        /// <summary>
        /// Whether the notepad has unsaved changes
        /// </summary>
        public bool HasUnsavedChanges { get; set; } = false;
    }
}