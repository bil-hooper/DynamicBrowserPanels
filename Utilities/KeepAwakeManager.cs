using System;
using System.Runtime.InteropServices;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages keeping the computer awake to prevent sleep during streaming
    /// </summary>
    public static class KeepAwakeManager
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern EXECUTION_STATE SetThreadExecutionState(EXECUTION_STATE esFlags);

        [Flags]
        private enum EXECUTION_STATE : uint
        {
            ES_AWAYMODE_REQUIRED = 0x00000040,
            ES_CONTINUOUS = 0x80000000,
            ES_DISPLAY_REQUIRED = 0x00000002,
            ES_SYSTEM_REQUIRED = 0x00000001
        }

        private static bool _isEnabled = false;

        /// <summary>
        /// Gets whether keep awake is currently enabled
        /// </summary>
        public static bool IsEnabled => _isEnabled;

        /// <summary>
        /// Enables keep awake mode - prevents system and display from sleeping
        /// </summary>
        public static void Enable()
        {
            if (_isEnabled)
                return;

            // Prevent system sleep and display sleep
            SetThreadExecutionState(
                EXECUTION_STATE.ES_CONTINUOUS |
                EXECUTION_STATE.ES_SYSTEM_REQUIRED |
                EXECUTION_STATE.ES_DISPLAY_REQUIRED);

            _isEnabled = true;
        }

        /// <summary>
        /// Disables keep awake mode - allows normal sleep behavior
        /// </summary>
        public static void Disable()
        {
            if (!_isEnabled)
                return;

            // Return to normal power management
            SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);

            _isEnabled = false;
        }

        /// <summary>
        /// Toggles keep awake mode on/off
        /// </summary>
        /// <returns>True if keep awake is now enabled, false if disabled</returns>
        public static bool Toggle()
        {
            if (_isEnabled)
            {
                Disable();
                return false;
            }
            else
            {
                Enable();
                return true;
            }
        }
    }
}