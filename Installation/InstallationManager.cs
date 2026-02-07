using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;
using Microsoft.Win32;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Handles installation and file association registration
    /// </summary>
    public static class InstallationManager
    {
        public static readonly string InstallDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            "DynamicBrowserPanels"
        );

        private static readonly string ExecutableName = "DynamicBrowserPanels.exe";
        private static readonly string BackupConfigFileName = "backup.dat";

        /// <summary>
        /// Checks if the application is running from the installed location
        /// </summary>
        public static bool IsInstalled()
        {
            string currentPath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule.FileName;
            string currentDir = Path.GetDirectoryName(currentPath);
            return currentDir.Equals(InstallDirectory, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks if the installed version of the app is currently running
        /// </summary>
        public static bool IsInstalledVersionRunning()
        {
            try
            {
                string installedExePath = Path.Combine(InstallDirectory, ExecutableName);
                
                if (!File.Exists(installedExePath))
                    return false;
                
                var currentProcess = Process.GetCurrentProcess();
                var processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(ExecutableName));
                
                foreach (var process in processes)
                {
                    try
                    {
                        // Skip if it's the current process
                        if (process.Id == currentProcess.Id)
                            continue;
                        
                        // Check if this process is running from the install directory
                        string processPath = process.MainModule?.FileName;
                        if (processPath != null && 
                            processPath.Equals(installedExePath, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        // Access denied or process already exited
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the current process has administrator privileges
        /// </summary>
        public static bool IsRunningAsAdmin()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Restarts the application with administrator privileges
        /// </summary>
        public static void RestartAsAdmin()
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule.FileName,
                    Verb = "runas"
                };

                Process.Start(startInfo);
                Application.Exit();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to restart with administrator privileges:\n{ex.Message}",
                    "Elevation Failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Performs the installation or update
        /// </summary>
        public static bool Install()
        {
            if (!IsRunningAsAdmin())
            {
                MessageBox.Show(
                    "Administrator privileges are required to install.\n\nThe application will now restart with elevated permissions.",
                    "Administrator Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                RestartAsAdmin();
                return false;
            }

            // Check if installed version is running
            if (IsInstalledVersionRunning())
            {
                MessageBox.Show(
                    "The installed version of Dynamic Browser Panels is currently running.\n\n" +
                    "Please close it before installing.",
                    "Installation Blocked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return false;
            }

            try
            {
                string currentExePath = Environment.ProcessPath ?? Process.GetCurrentProcess().MainModule.FileName;
                string currentDir = Path.GetDirectoryName(currentExePath);

                bool isUpdate = Directory.Exists(InstallDirectory);
                
                if (isUpdate)
                {
                    var result = MessageBox.Show(
                        $"Dynamic Browser Panels is already installed.\n\n" +
                        $"Do you want to update it with the current version?\n\n" +
                        $"Install Location: {InstallDirectory}",
                        "Update Installation",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );
                    
                    if (result != DialogResult.Yes)
                        return false;
                }

                // Create installation directory if needed
                if (!Directory.Exists(InstallDirectory))
                {
                    Directory.CreateDirectory(InstallDirectory);
                }
                
                // Copy all files from current directory to install directory
                CopyDirectory(currentDir, InstallDirectory);

                // Register file association
                RegisterFileAssociation();

                string message = isUpdate
                    ? $"Update successful!\n\n" +
                      $"Location: {InstallDirectory}\n\n" +
                      $"The application will now restart from the installed location."
                    : $"Installation successful!\n\n" +
                      $"Location: {InstallDirectory}\n\n" +
                      $".frm files are now associated with Dynamic Browser Panels.\n\n" +
                      $"The application will now restart from the installed location.";

                MessageBox.Show(
                    message,
                    isUpdate ? "Update Complete" : "Installation Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Restart from installed location
                string installedExePath = Path.Combine(InstallDirectory, ExecutableName);
                Process.Start(installedExePath);
                Application.Exit();

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Installation failed:\n{ex.Message}",
                    "Installation Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }
        }

        /// <summary>
        /// Uninstalls the application
        /// </summary>
        public static bool Uninstall()
        {
            if (!IsRunningAsAdmin())
            {
                MessageBox.Show(
                    "Administrator privileges are required to uninstall.\n\nThe application will now restart with elevated permissions.",
                    "Administrator Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                RestartAsAdmin();
                return false;
            }

            var result = MessageBox.Show(
                "Are you sure you want to uninstall Dynamic Browser Panels?\n\n" +
                "This will:\n" +
                "• Remove the application from Program Files\n" +
                "• Remove .frm file association\n" +
                "• Keep your user data and layouts (in AppData)\n\n" +
                "Continue with uninstall?",
                "Confirm Uninstall",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result != DialogResult.Yes)
                return false;

            try
            {
                // Unregister file association
                UnregisterFileAssociation();

                // We can't delete the directory while running from it
                // Create a batch file to delete after exit
                string batchPath = Path.Combine(Path.GetTempPath(), "uninstall_dbp.bat");
                string batchContent = $@"@echo off
timeout /t 2 /nobreak > nul
rmdir /s /q ""{InstallDirectory}""
del ""{batchPath}""
";
                File.WriteAllText(batchPath, batchContent);

                MessageBox.Show(
                    "Uninstall will complete after the application closes.\n\n" +
                    "Your layouts and user data in AppData will be preserved.",
                    "Uninstall Scheduled",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );

                // Start the batch file and exit
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = batchPath,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);

                Application.Exit();
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Uninstall failed:\n{ex.Message}",
                    "Uninstall Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                return false;
            }
        }

        /// <summary>
        /// Copies a directory and all its contents
        /// </summary>
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            // Create destination directory if it doesn't exist
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Copy all files
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string destFile = Path.Combine(destDir, Path.GetFileName(file));
                try
                {
                    File.Copy(file, destFile, true);
                }
                catch (IOException)
                {
                    // Skip files that are in use (like the current executable)
                    // They will be handled by a separate mechanism if needed
                }
            }

            // Copy all subdirectories
            foreach (string dir in Directory.GetDirectories(sourceDir))
            {
                string destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
                CopyDirectory(dir, destSubDir);
            }
        }

        /// <summary>
        /// Registers the .frm file association
        /// </summary>
        private static void RegisterFileAssociation()
        {
            string executablePath = Path.Combine(InstallDirectory, ExecutableName);
            string progId = "DynamicBrowserPanels.Layout";

            try
            {
                // Register the ProgID
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(progId))
                {
                    key.SetValue("", "Browser Layout File");
                    key.SetValue("FriendlyTypeName", "Dynamic Browser Panels Layout");
                }

                // Set the default icon
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey($@"{progId}\DefaultIcon"))
                {
                    key.SetValue("", $"{executablePath},0");
                }

                // Register the open command
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey($@"{progId}\shell\open\command"))
                {
                    key.SetValue("", $"\"{executablePath}\" \"%1\"");
                }

                // Associate .frm extension with the ProgID
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(".frm"))
                {
                    key.SetValue("", progId);
                }

                // Notify Windows of the file association change
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to register file association: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Unregisters the .frm file association
        /// </summary>
        private static void UnregisterFileAssociation()
        {
            string progId = "DynamicBrowserPanels.Layout";

            try
            {
                // Remove .frm extension association
                Registry.ClassesRoot.DeleteSubKeyTree(".frm", false);

                // Remove ProgID
                Registry.ClassesRoot.DeleteSubKeyTree(progId, false);

                // Notify Windows of the file association change
                SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to unregister file association: {ex.Message}", ex);
            }
        }

        // Windows API to notify shell of changes
        [System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}