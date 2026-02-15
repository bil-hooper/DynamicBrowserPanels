using System.Drawing;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Centralized color scheme for the application
    /// </summary>
    public static class AppColors
    {
        // Purple accent color scheme
        public static readonly Color PrimaryAccent = Color.FromArgb(147, 112, 219);      // Medium Purple
        public static readonly Color PrimaryAccentHover = Color.FromArgb(138, 43, 226);  // Blue Violet (hover)
        public static readonly Color PrimaryAccentLight = Color.FromArgb(230, 230, 250); // Lavender (light backgrounds)
        public static readonly Color PrimaryAccentBorder = Color.FromArgb(180, 180, 220); // Light purple border
        
        // Tab colors (alternating subtle purples)
        public static readonly Color TabEven = Color.FromArgb(252, 252, 255);            // Very light blue-white
        public static readonly Color TabOdd = Color.FromArgb(250, 248, 252);             // Very light purple-white
        public static readonly Color TabSelectedEven = Color.FromArgb(230, 230, 245);    // Light blue-white
        public static readonly Color TabSelectedOdd = Color.FromArgb(235, 225, 245);     // Light purple-white
        public static readonly Color TabBorderSelected = Color.FromArgb(180, 180, 220);  // Purple border
        public static readonly Color TabBorderNormal = Color.FromArgb(220, 220, 230);    // Subtle border
        
        // Button colors (for special actions)
        public static readonly Color ButtonPrimaryBg = Color.FromArgb(240, 235, 250);    // Light purple background
        public static readonly Color ButtonPrimaryBorder = PrimaryAccent;                // Purple border
        
        // Status colors
        public static readonly Color StatusSuccess = Color.FromArgb(107, 142, 35);       // Olive Drab (keep green)
        public static readonly Color StatusError = Color.FromArgb(178, 34, 34);          // Firebrick (keep red)
        public static readonly Color StatusInfo = PrimaryAccent;                         // Purple for info
        public static readonly Color StatusWarning = Color.FromArgb(255, 140, 0);        // Dark Orange (keep orange)
    }
}