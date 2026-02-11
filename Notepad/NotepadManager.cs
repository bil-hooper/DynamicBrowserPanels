using System;
using System.IO;
using System.Text.Json;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages notepad file persistence
    /// </summary>
    public static class NotepadManager
    {
        private static readonly string NotesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels",
            "Notes"
        );

        /// <summary>
        /// Loads a note from disk
        /// </summary>
        public static NotepadData LoadNote(int noteNumber)
        {
            try
            {
                var filePath = GetNoteFilePath(noteNumber);
                
                if (File.Exists(filePath))
                {
                    var json = File.ReadAllText(filePath);
                    return JsonSerializer.Deserialize<NotepadData>(json) ?? new NotepadData();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading note {noteNumber}: {ex.Message}");
            }

            return new NotepadData();
        }

        /// <summary>
        /// Saves a note to disk
        /// </summary>
        public static void SaveNote(int noteNumber, string content)
        {
            try
            {
                // Ensure directory exists
                if (!Directory.Exists(NotesDirectory))
                {
                    Directory.CreateDirectory(NotesDirectory);
                }

                var noteData = new NotepadData
                {
                    Content = content,
                    LastModified = DateTime.Now
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(noteData, options);
                
                var filePath = GetNoteFilePath(noteNumber);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Failed to save note: {ex.Message}",
                    "Save Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );
            }
        }

        /// <summary>
        /// Exports a note to a text file
        /// </summary>
        public static bool ExportNote(string content, string filePath)
        {
            try
            {
                File.WriteAllText(filePath, content);
                return true;
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    $"Failed to export note: {ex.Message}",
                    "Export Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error
                );
                return false;
            }
        }

        private static string GetNoteFilePath(int noteNumber)
        {
            return Path.Combine(NotesDirectory, $"Note_{noteNumber}.json");
        }
    }
}