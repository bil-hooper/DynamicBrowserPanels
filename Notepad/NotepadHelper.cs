using System;
using System.IO;
using System.Text;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Helper class to generate the notepad HTML page
    /// </summary>
    public static class NotepadHelper
    {
        /// <summary>
        /// Creates a temporary HTML file for the notepad with the given content
        /// </summary>
        public static string CreateNotepadHtml(string content = "")
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"notepad_{Guid.NewGuid()}.html");

            // Escape content for JavaScript
            var escapedContent = EscapeForJavaScript(content);

            var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Notepad</title>
    <style>
        * {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}

        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: #1e1e1e;
            color: #d4d4d4;
            height: 100vh;
            display: flex;
            flex-direction: column;
        }}

        .toolbar {{
            background: #2d2d30;
            padding: 8px 12px;
            display: flex;
            gap: 8px;
            align-items: center;
            border-bottom: 1px solid #3e3e42;
            flex-shrink: 0;
        }}

        .toolbar button {{
            background: #0e639c;
            color: white;
            border: none;
            padding: 6px 12px;
            border-radius: 3px;
            cursor: pointer;
            font-size: 13px;
            transition: background 0.2s;
        }}

        .toolbar button:hover {{
            background: #1177bb;
        }}

        .toolbar button:active {{
            background: #0d5a8f;
        }}

        .toolbar button:disabled {{
            background: #555;
            cursor: not-allowed;
            opacity: 0.5;
        }}

        .toolbar .separator {{
            width: 1px;
            height: 20px;
            background: #3e3e42;
            margin: 0 4px;
        }}

        .toolbar .info {{
            margin-left: auto;
            font-size: 12px;
            color: #858585;
        }}

        .toolbar .save-status {{
            color: #4ec9b0;
            font-size: 12px;
        }}

        .toolbar .save-status.modified {{
            color: #ce9178;
        }}

        #notepad {{
            flex: 1;
            width: 100%;
            padding: 16px;
            background: #1e1e1e;
            color: #d4d4d4;
            border: none;
            outline: none;
            font-family: 'Consolas', 'Courier New', monospace;
            font-size: 14px;
            line-height: 1.6;
            resize: none;
            overflow-y: auto;
        }}

        #notepad::selection {{
            background: #264f78;
        }}

        #notepad::-webkit-scrollbar {{
            width: 12px;
        }}

        #notepad::-webkit-scrollbar-track {{
            background: #1e1e1e;
        }}

        #notepad::-webkit-scrollbar-thumb {{
            background: #424242;
            border-radius: 6px;
        }}

        #notepad::-webkit-scrollbar-thumb:hover {{
            background: #4e4e4e;
        }}
    </style>
</head>
<body>
    <div class=""toolbar"">
        <button id=""btnUndo"" title=""Undo (Ctrl+Z)"">⟲ Undo</button>
        <button id=""btnRedo"" title=""Redo (Ctrl+Y)"">⟳ Redo</button>
        <div class=""separator""></div>
        <button id=""btnSave"" title=""Save Now (Ctrl+S)"">💾 Save</button>
        <button id=""btnExport"" title=""Export to File"">📄 Export</button>
        <div class=""separator""></div>
        <button id=""btnClear"" title=""Clear All"">🗑️ Clear</button>
        <span class=""save-status"" id=""saveStatus"">Auto-saved</span>
        <div class=""info"">
            <span id=""charCount"">0 characters</span> | 
            <span id=""lineCount"">1 line</span>
        </div>
    </div>
    <textarea id=""notepad"" placeholder=""Start typing your notes here...&#10;&#10;• Plain text notes&#10;• URLs and links&#10;• Code snippets&#10;• To-do lists&#10;• Anything you want to remember&#10;&#10;Your notes are automatically saved every 5 minutes."">{escapedContent}</textarea>

    <script>
        const notepad = document.getElementById('notepad');
        const btnUndo = document.getElementById('btnUndo');
        const btnRedo = document.getElementById('btnRedo');
        const btnSave = document.getElementById('btnSave');
        const btnExport = document.getElementById('btnExport');
        const btnClear = document.getElementById('btnClear');
        const saveStatus = document.getElementById('saveStatus');
        const charCount = document.getElementById('charCount');
        const lineCount = document.getElementById('lineCount');

        let hasChanges = false;
        let autoSaveTimer = null;
        const AUTO_SAVE_INTERVAL = 5 * 60 * 1000; // 5 minutes

        // Initialize
        updateStats();
        updateUndoRedoButtons();

        // Update character and line count
        function updateStats() {{
            const text = notepad.value;
            const chars = text.length;
            const lines = text.split('\n').length;
            
            charCount.textContent = `${{chars}} character${{chars !== 1 ? 's' : ''}}`;
            lineCount.textContent = `${{lines}} line${{lines !== 1 ? 's' : ''}}`;
        }}

        // Update undo/redo button states
        function updateUndoRedoButtons() {{
            btnUndo.disabled = !document.queryCommandEnabled('undo');
            btnRedo.disabled = !document.queryCommandEnabled('redo');
        }}

        // Mark as modified
        function markModified() {{
            if (!hasChanges) {{
                hasChanges = true;
                saveStatus.textContent = 'Unsaved changes';
                saveStatus.classList.add('modified');
                
                // Schedule autosave
                scheduleAutoSave();
            }}
        }}

        // Schedule autosave
        function scheduleAutoSave() {{
            if (autoSaveTimer) {{
                clearTimeout(autoSaveTimer);
            }}
            
            autoSaveTimer = setTimeout(() => {{
                if (hasChanges) {{
                    saveNotepad();
                }}
            }}, AUTO_SAVE_INTERVAL);
        }}

        // Save notepad content
        function saveNotepad() {{
            const content = notepad.value;
            
            // Send message to host application
            window.chrome.webview.postMessage({{
                action: 'saveNotepad',
                content: content
            }});
            
            hasChanges = false;
            saveStatus.textContent = 'Saved at ' + new Date().toLocaleTimeString();
            saveStatus.classList.remove('modified');
        }}

        // Export to file
        function exportNotepad() {{
            const content = notepad.value;
            
            window.chrome.webview.postMessage({{
                action: 'exportNotepad',
                content: content
            }});
        }}

        // Clear notepad
        function clearNotepad() {{
            if (notepad.value.trim() === '') {{
                return;
            }}
            
            if (confirm('Are you sure you want to clear all notes?\\n\\nThis cannot be undone.')) {{
                notepad.value = '';
                hasChanges = true;
                saveNotepad();
                updateStats();
            }}
        }}

        // Event listeners
        notepad.addEventListener('input', () => {{
            markModified();
            updateStats();
            updateUndoRedoButtons();
        }});

        btnUndo.addEventListener('click', () => {{
            document.execCommand('undo');
            updateUndoRedoButtons();
            updateStats();
        }});

        btnRedo.addEventListener('click', () => {{
            document.execCommand('redo');
            updateUndoRedoButtons();
            updateStats();
        }});

        btnSave.addEventListener('click', () => {{
            saveNotepad();
        }});

        btnExport.addEventListener('click', () => {{
            exportNotepad();
        }});

        btnClear.addEventListener('click', () => {{
            clearNotepad();
        }});

        // Keyboard shortcuts
        notepad.addEventListener('keydown', (e) => {{
            if (e.ctrlKey && e.key === 's') {{
                e.preventDefault();
                saveNotepad();
            }}
            else if (e.ctrlKey && e.key === 'z') {{
                setTimeout(updateUndoRedoButtons, 0);
            }}
            else if (e.ctrlKey && e.key === 'y') {{
                setTimeout(updateUndoRedoButtons, 0);
            }}
        }});

        // Save before unload if there are unsaved changes
        window.addEventListener('beforeunload', (e) => {{
            if (hasChanges) {{
                saveNotepad();
            }}
        }});

        // Focus notepad on load
        notepad.focus();
    </script>
</body>
</html>";

            File.WriteAllText(tempPath, html);
            return tempPath;
        }

        /// <summary>
        /// Escapes content for safe inclusion in JavaScript
        /// </summary>
        private static string EscapeForJavaScript(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            var sb = new StringBuilder(content.Length);
            foreach (char c in content)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\'':
                        sb.Append("\\'");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }
    }
}