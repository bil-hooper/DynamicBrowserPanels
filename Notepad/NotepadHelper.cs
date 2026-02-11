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
        private static int _notepadCounter = 0;
        private static readonly object _counterLock = new object();

        /// <summary>
        /// Gets the next unique notepad instance number
        /// </summary>
        public static int GetNextNotepadInstance()
        {
            lock (_counterLock)
            {
                return ++_notepadCounter;
            }
        }

        /// <summary>
        /// Gets the HTML file path for a specific notepad instance
        /// </summary>
        public static string GetNotepadHtmlPath(int instanceNumber)
        {
            return Path.Combine(
                Path.GetTempPath(),
                $"DynamicBrowserPanels_Notepad_{instanceNumber}.html"
            );
        }

        /// <summary>
        /// Creates a temporary HTML file for the notepad with the given content
        /// </summary>
        /// <param name="content">Initial content for the notepad</param>
        /// <param name="instanceNumber">Unique instance number for this notepad</param>
        public static string CreateNotepadHtml(string content = "", int instanceNumber = 0)
        {
            var htmlPath = GetNotepadHtmlPath(instanceNumber);
            
            // Escape content for JavaScript string (will be set via script)
            var escapedContent = EscapeForJavaScript(content);
            
            var html = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Notepad {instanceNumber}</title>
    <meta name=""notepad-instance"" content=""{instanceNumber}"">
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

        .toolbar .instance-badge {{
            background: #3e3e42;
            padding: 4px 8px;
            border-radius: 3px;
            font-size: 11px;
            color: #858585;
        }}

        .toolbar .mode-indicator {{
            background: #3e3e42;
            padding: 4px 8px;
            border-radius: 3px;
            font-size: 11px;
            color: #858585;
        }}

        .toolbar .mode-indicator.edit-mode {{
            background: #ce9178;
            color: #1e1e1e;
        }}

        .content-container {{
            flex: 1;
            position: relative;
            overflow: hidden;
        }}

        #notepad {{
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
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

        #renderedView {{
            position: absolute;
            top: 0;
            left: 0;
            width: 100%;
            height: 100%;
            padding: 16px;
            background: #1e1e1e;
            color: #d4d4d4;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            font-size: 14px;
            line-height: 1.6;
            overflow-y: auto;
            cursor: text;
            white-space: pre-wrap;
            word-wrap: break-word;
        }}

        #renderedView::-webkit-scrollbar {{
            width: 12px;
        }}

        #renderedView::-webkit-scrollbar-track {{
            background: #1e1e1e;
        }}

        #renderedView::-webkit-scrollbar-thumb {{
            background: #424242;
            border-radius: 6px;
        }}

        #renderedView::-webkit-scrollbar-thumb:hover {{
            background: #4e4e4e;
        }}

        #renderedView a {{
            color: #4a9eff;
            text-decoration: none;
        }}

        #renderedView a:hover {{
            text-decoration: underline;
        }}

        #renderedView a:visited {{
            color: #c586c0;
        }}

        .hidden {{
            display: none;
        }}
    </style>
</head>
<body>
    <div class=""toolbar"">
        <button id=""btnUndo"" title=""Undo (Ctrl+Z)"">⟲ Undo</button>
        <button id=""btnRedo"" title=""Redo (Ctrl+Y)"">⟳ Redo</button>
        <div class=""separator""></div>
        <button id=""btnSave"" title=""Save Now (Ctrl+S)"">💾 Save</button>
        <button id=""btnRefresh"" title=""Reload from Disk (Ctrl+R)"">🔄 Refresh</button>
        <button id=""btnExport"" title=""Export to File"">📄 Export</button>
        <div class=""separator""></div>
        <button id=""btnClear"" title=""Clear All"">🗑️ Clear</button>
        <span class=""instance-badge"">#{instanceNumber}</span>
        <span class=""mode-indicator"" id=""modeIndicator"">View Mode</span>
        <span class=""save-status"" id=""saveStatus"">Auto-saved</span>
        <div class=""info"">
            <span id=""charCount"">0 characters</span> | 
            <span id=""lineCount"">1 line</span>
        </div>
    </div>
    <div class=""content-container"">
        <textarea id=""notepad"" class=""hidden"" placeholder=""Start typing your notes here...&#10;&#10;• Plain text notes&#10;• URLs and links&#10;• Code snippets&#10;• To-do lists&#10;• Anything you want to remember&#10;&#10;Your notes are automatically saved every 5 minutes.&#10;&#10;Double-click to edit, click Save to render HTML links.""></textarea>
        <div id=""renderedView""></div>
    </div>

    <script>
        const notepad = document.getElementById('notepad');
        const renderedView = document.getElementById('renderedView');
        const btnUndo = document.getElementById('btnUndo');
        const btnRedo = document.getElementById('btnRedo');
        const btnSave = document.getElementById('btnSave');
        const btnRefresh = document.getElementById('btnRefresh');
        const btnExport = document.getElementById('btnExport');
        const btnClear = document.getElementById('btnClear');
        const saveStatus = document.getElementById('saveStatus');
        const modeIndicator = document.getElementById('modeIndicator');
        const charCount = document.getElementById('charCount');
        const lineCount = document.getElementById('lineCount');

        const INSTANCE_NUMBER = {instanceNumber};
        
        let hasChanges = false;
        let autoSaveTimer = null;
        let isEditMode = false;
        const AUTO_SAVE_INTERVAL = 5 * 60 * 1000; // 5 minutes

        // Set initial content (properly unescaped)
        notepad.value = ""{escapedContent}"";
        
        // Initialize in view mode
        updateRenderedView();
        updateStats();
        updateUndoRedoButtons();

        // Convert URLs to clickable links
        function linkify(text) {{
            const urlPattern = /(\b(https?|ftp):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])/gim;
            const wwwPattern = /(^|[^\/])(www\.[\S]+(\b|$))/gim;
            const emailPattern = /(([a-zA-Z0-9\-\_\.])+@[a-zA-Z\_]+?(\.[a-zA-Z]{{2,6}})+)/gim;

            let result = text.replace(/&/g, '&amp;')
                            .replace(/</g, '&lt;')
                            .replace(/>/g, '&gt;');

            result = result.replace(urlPattern, '<a href=""$1"" target=""_blank"">$1</a>');
            result = result.replace(wwwPattern, '$1<a href=""http://$2"" target=""_blank"">$2</a>');
            result = result.replace(emailPattern, '<a href=""mailto:$1"">$1</a>');

            return result;
        }}

        // Update the rendered view with linkified content
        function updateRenderedView() {{
            const content = notepad.value;
            if (content.trim() === '') {{
                renderedView.innerHTML = '<span style=""color: #858585; font-style: italic;"">Double-click to start editing...</span>';
            }} else {{
                renderedView.innerHTML = linkify(content);
            }}
        }}

        // Switch to edit mode
        function enterEditMode() {{
            isEditMode = true;
            notepad.classList.remove('hidden');
            renderedView.classList.add('hidden');
            modeIndicator.textContent = 'Edit Mode';
            modeIndicator.classList.add('edit-mode');
            btnUndo.disabled = false;
            btnRedo.disabled = false;
            notepad.focus();
            updateUndoRedoButtons();
        }}

        // Switch to view mode
        function enterViewMode() {{
            isEditMode = false;
            notepad.classList.add('hidden');
            renderedView.classList.remove('hidden');
            modeIndicator.textContent = 'View Mode';
            modeIndicator.classList.remove('edit-mode');
            btnUndo.disabled = true;
            btnRedo.disabled = true;
            updateRenderedView();
        }}

        // Listen for content updates from the host application
        window.chrome.webview.addEventListener('message', function(event) {{
            if (event.data && event.data.action === 'updateContent') {{
                const newContent = event.data.content || '';
                
                // Only update if content actually changed
                if (notepad.value !== newContent) {{
                    // Save cursor position
                    const selectionStart = notepad.selectionStart;
                    const selectionEnd = notepad.selectionEnd;
                    
                    // Update content
                    notepad.value = newContent;
                    
                    // Restore cursor position (if still valid)
                    if (selectionStart <= notepad.value.length) {{
                        notepad.setSelectionRange(
                            Math.min(selectionStart, notepad.value.length),
                            Math.min(selectionEnd, notepad.value.length)
                        );
                    }}
                    
                    // Update UI
                    updateStats();
                    updateRenderedView();
                    hasChanges = false;
                    saveStatus.textContent = 'Content refreshed at ' + new Date().toLocaleTimeString();
                    saveStatus.classList.remove('modified');
                }}
            }}
        }});

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
            if (isEditMode) {{
                btnUndo.disabled = !document.queryCommandEnabled('undo');
                btnRedo.disabled = !document.queryCommandEnabled('redo');
            }} else {{
                btnUndo.disabled = true;
                btnRedo.disabled = true;
            }}
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
            
            // Send message to host application with instance number
            window.chrome.webview.postMessage({{
                action: 'saveNotepad',
                instanceNumber: INSTANCE_NUMBER,
                content: content
            }});
            
            hasChanges = false;
            saveStatus.textContent = 'Saved at ' + new Date().toLocaleTimeString();
            saveStatus.classList.remove('modified');
            
            // Switch to view mode after saving
            enterViewMode();
        }}

        // Refresh content from disk
        function refreshNotepad() {{
            if (hasChanges) {{
                if (!confirm('You have unsaved changes. Reload from disk and lose changes?')) {{
                    return;
                }}
            }}
            
            // Request fresh content from host
            window.chrome.webview.postMessage({{
                action: 'refreshNotepad',
                instanceNumber: INSTANCE_NUMBER
            }});
        }}

        // Export to file
        function exportNotepad() {{
            const content = notepad.value;
            
            window.chrome.webview.postMessage({{
                action: 'exportNotepad',
                instanceNumber: INSTANCE_NUMBER,
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
                updateRenderedView();
                
                // Immediately save the empty content
                window.chrome.webview.postMessage({{
                    action: 'saveNotepad',
                    instanceNumber: INSTANCE_NUMBER,
                    content: ''
                }});
                
                hasChanges = false;
                saveStatus.textContent = 'Cleared and saved at ' + new Date().toLocaleTimeString();
                saveStatus.classList.remove('modified');
            }}
        }}

        // Event listeners
        notepad.addEventListener('input', () => {{
            markModified();
            updateStats();
            updateUndoRedoButtons();
        }});

        // Double-click on rendered view to enter edit mode
        renderedView.addEventListener('dblclick', () => {{
            enterEditMode();
        }});

        btnUndo.addEventListener('click', () => {{
            if (isEditMode) {{
                document.execCommand('undo');
                updateUndoRedoButtons();
                updateStats();
            }}
        }});

        btnRedo.addEventListener('click', () => {{
            if (isEditMode) {{
                document.execCommand('redo');
                updateUndoRedoButtons();
                updateStats();
            }}
        }});

        btnSave.addEventListener('click', () => {{
            saveNotepad();
        }});

        btnRefresh.addEventListener('click', () => {{
            refreshNotepad();
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
            else if (e.ctrlKey && e.key === 'r') {{
                e.preventDefault();
                refreshNotepad();
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

        // Auto-refresh when page becomes visible (tab switch)
        document.addEventListener('visibilitychange', () => {{
            if (!document.hidden) {{
                // Request fresh content when tab becomes visible
                window.chrome.webview.postMessage({{
                    action: 'requestCurrentContent',
                    instanceNumber: INSTANCE_NUMBER
                }});
            }}
        }});
    </script>
</body>
</html>";

            // Always overwrite the same file for this instance
            File.WriteAllText(htmlPath, html);
            
            return htmlPath;
        }

        /// <summary>
        /// Escapes content for safe inclusion in JavaScript string literal
        /// </summary>
        public static string EscapeForJavaScript(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            return content
                .Replace("\\", "\\\\")   // Backslash must be first
                .Replace("\"", "\\\"")   // Escape double quotes
                .Replace("\r\n", "\\n")  // Windows newlines
                .Replace("\n", "\\n")    // Unix newlines
                .Replace("\r", "\\n")    // Mac newlines
                .Replace("\t", "\\t")    // Tabs
                .Replace("</", "<\\/");  // Prevent script injection
        }
    }
}