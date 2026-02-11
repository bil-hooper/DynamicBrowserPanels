using System;
using System.IO;
using System.Text.Json;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages notepad persistence and autosave functionality
    /// </summary>
    public static class NotepadManager
    {
        private static readonly string NotepadDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels",
            "Notepad.json"
        );

        /// <summary>
        /// Loads the saved notepad content
        /// </summary>
        public static NotepadData LoadNotepad()
        {
            try
            {
                if (File.Exists(NotepadDataPath))
                {
                    var json = File.ReadAllText(NotepadDataPath);
                    return JsonSerializer.Deserialize<NotepadData>(json) ?? new NotepadData();
                }
            }
            catch
            {
                // If load fails, return empty notepad
            }

            return new NotepadData();
        }

        /// <summary>
        /// Saves the notepad content
        /// </summary>
        public static void SaveNotepad(NotepadData data)
        {
            try
            {
                var directory = Path.GetDirectoryName(NotepadDataPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                data.LastModified = DateTime.Now;
                data.HasUnsavedChanges = false;

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(NotepadDataPath, json);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Failed to save notepad: {ex.Message}",
                    "Save Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Exports notepad content to a text file
        /// </summary>
        public static bool ExportToFile(string content, string filePath)
        {
            try
            {
                File.WriteAllText(filePath, content);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Failed to export notepad: {ex.Message}",
                    "Export Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );
                return false;
            }
        }
    }
}