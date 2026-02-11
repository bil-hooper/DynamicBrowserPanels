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
            private static readonly string NotepadDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels",
            "Notepads"
        );

        /// <summary>
        /// Gets the data file path for a specific notepad instance
        /// </summary>
        private static string GetNotepadDataPath(int instanceNumber)
        {
            return Path.Combine(NotepadDataDirectory, $"Notepad_{instanceNumber}.json");
        }

        /// <summary>
        /// Loads the saved notepad content for a specific instance
        /// </summary>
        public static NotepadData LoadNotepad(int instanceNumber)
        {
            try
            {
                var notepadDataPath = GetNotepadDataPath(instanceNumber);
                
                if (File.Exists(notepadDataPath))
                {
                    var json = File.ReadAllText(notepadDataPath);
                    var data = JsonSerializer.Deserialize<NotepadData>(json) ?? new NotepadData();
                    return data;
                }
            }
            catch (Exception ex)
            {
                // Do nothing
            }

            return new NotepadData();
        }

        /// <summary>
        /// Saves the notepad content for a specific instance
        /// </summary>
        public static void SaveNotepad(NotepadData data, int instanceNumber)
        {
            try
            {
                var notepadDataPath = GetNotepadDataPath(instanceNumber);
               
                // Ensure directory exists
                if (!Directory.Exists(NotepadDataDirectory))
                {
                    Directory.CreateDirectory(NotepadDataDirectory);
                }

                data.LastModified = DateTime.Now;
                data.HasUnsavedChanges = false;

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(data, options);
                
                File.WriteAllText(notepadDataPath, json);
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
        public static bool ExportToFile(string content, string filePath, int instanceNumber)
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