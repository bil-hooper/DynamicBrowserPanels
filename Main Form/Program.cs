using System;
using System.IO;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Main program entry point
    /// </summary>
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Synchronize from backup directory on startup
            BrowserStateManager.SynchronizeFromBackup();
            
            // Check if a .frm file was passed as a command-line argument
            if (args.Length > 0)
            {
                string filePath = args[0];
                
                // Verify the file exists and has .frm extension
                if (File.Exists(filePath) && Path.GetExtension(filePath).Equals(".frm", StringComparison.OrdinalIgnoreCase))
                {
                    // Set the session file path for command-line mode
                    BrowserStateManager.SessionFilePath = filePath;
                    
                    // Launch the application
                    Application.Run(new MainBrowserForm());
                }
                else
                {
                    MessageBox.Show(
                        $"Invalid file: {filePath}\n\n" +
                        "Please specify a valid .frm layout file.",
                        "Invalid File",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                }
            }
            else
            {
                // Normal mode - no command-line arguments
                Application.Run(new MainBrowserForm());
            }
        }
    }
}   
