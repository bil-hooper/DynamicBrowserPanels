namespace DynamicBrowserPanels
{
    /// <summary>
    /// Privacy lock configuration settings
    /// </summary>
    public class PrivacyLockSettings
    {
        /// <summary>
        /// Whether privacy lock is enabled
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Encrypted PIN hash (DPAPI-protected)
        /// </summary>
        public string PinHashEncrypted { get; set; } = string.Empty;
    }
}