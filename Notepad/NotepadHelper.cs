using System;
using System.IO;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Helper to create the notepad HTML file
    /// </summary>
    public static class NotepadHelper
    {
        private static readonly string NotepadHtmlPath = Path.Combine(
            Path.GetTempPath(),
            "DynamicBrowserPanels_Notepad.html"
        );

        /// <summary>
        /// Gets the notepad HTML file path
        /// </summary>
        public static string GetNotepadHtmlPath()
        {
            return NotepadHtmlPath;
        }

        /// <summary>
        /// Creates the notepad HTML file
        /// </summary>
        public static string CreateNotepadHtml()
        {
            var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Notepad</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: #1e1e1e;
            color: #d4d4d4;
            height: 100vh;
            display: flex;
            flex-direction: column;
        }

        .toolbar {
            background: #2d2d30;
            padding: 8px 12px;
            display: flex;
            gap: 8px;
            align-items: center;
            border-bottom: 1px solid #3e3e42;
            flex-shrink: 0;
        }

        .toolbar button {
            background: #0e639c;
            color: white;
            border: none;
            padding: 6px 12px;
            border-radius: 3px;
            cursor: pointer;
            font-size: 13px;
        }

        .toolbar button:hover {
            background: #1177bb;
        }

        .toolbar .separator {
            width: 1px;
            height: 20px;
            background: #3e3e42;
            margin: 0 4px;
        }

        .toolbar .note-selector {
            display: flex;
            align-items: center;
            gap: 6px;
            background: #3e3e42;
            padding: 4px 8px;
            border-radius: 3px;
        }

        .toolbar .note-selector label {
            font-size: 11px;
            color: #858585;
        }

        .toolbar .note-selector input {
            background: #2d2d30;
            color: #d4d4d4;
            border: 1px solid #555;
            padding: 4px 8px;
            font-size: 12px;
            width: 80px;
            text-align: center;
            border-radius: 3px;
        }

        .toolbar .status {
            margin-left: auto;
            font-size: 12px;
            color: #4ec9b0;
        }

        .toolbar .status.modified {
            color: #ce9178;
        }

        .toolbar .mode-indicator {
            background: #3e3e42;
            padding: 4px 8px;
            border-radius: 3px;
            font-size: 11px;
            color: #858585;
        }

        .toolbar .mode-indicator.edit-mode {
            background: #ce9178;
            color: #1e1e1e;
        }

        .content-area {
            flex: 1;
            display: flex;
            flex-direction: column;
            overflow: hidden;
            position: relative;
        }

        #notepad {
            flex: 1;
            padding: 16px;
            background: #1e1e1e;
            color: #d4d4d4;
            border: none;
            outline: none;
            font-family: 'Consolas', 'Courier New', monospace;
            font-size: 14px;
            line-height: 1.6;
            resize: none;
            display: none;
        }

        #notepad::selection {
            background: #264f78;
        }

        #renderedView {
            flex: 1;
            padding: 16px;
            background: #1e1e1e;
            color: #d4d4d4;
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            font-size: 14px;
            line-height: 1.6;
            overflow-y: auto;
            white-space: pre-wrap;
            word-wrap: break-word;
            cursor: text;
        }

        #renderedView a {
            color: #4a9eff;
            text-decoration: none;
        }

        #renderedView a:hover {
            text-decoration: underline;
        }

        #renderedView a:visited {
            color: #c586c0;
        }

        #renderedView.empty {
            color: #858585;
            font-style: italic;
        }
    </style>
</head>
<body>
    <div class=""toolbar"">
        <button id=""btnSave"" title=""Save and View (Ctrl+S)"">Save</button>
        <button id=""btnEdit"" title=""Edit Mode"">Edit</button>
        <button id=""btnExport"" title=""Export to File"">Export</button>
        <button id=""btnClear"" title=""Clear Note"">Clear</button>
        <div class=""separator""></div>
        <div class=""note-selector"">
            <label for=""noteNumber"">Note #:</label>
            <input type=""number"" id=""noteNumber"" min=""1"" max=""2147483647"" value=""1"">
        </div>
        <span class=""mode-indicator"" id=""modeIndicator"">View Mode</span>
        <span class=""status"" id=""status"">Ready</span>
    </div>
    <div class=""content-area"">
        <textarea id=""notepad"" placeholder=""Start typing your notes here...

• Plain text notes
• URLs and links (rendered as clickable links in view mode)
• Code snippets
• To-do lists
• Quick thoughts

Notes are saved per number - switch numbers to access different notes.
Auto-saves every 5 minutes.

Click 'Save' to switch to view mode with clickable links.
Double-click the view to edit again.""></textarea>
        <div id=""renderedView"" class=""empty"">Double-click to start editing...</div>
    </div>

    <script>
        const notepad = document.getElementById('notepad');
        const renderedView = document.getElementById('renderedView');
        const noteNumber = document.getElementById('noteNumber');
        const btnSave = document.getElementById('btnSave');
        const btnEdit = document.getElementById('btnEdit');
        const btnExport = document.getElementById('btnExport');
        const btnClear = document.getElementById('btnClear');
        const btnUndo = document.getElementById('btnUndo');
        const btnRedo = document.getElementById('btnRedo');
        const status = document.getElementById('status');
        const modeIndicator = document.getElementById('modeIndicator');

        let currentNoteNumber = 1;
        let hasChanges = false;
        let autoSaveTimer = null;
        let isEditMode = false;
        const AUTO_SAVE_INTERVAL = 5 * 60 * 1000; // 5 minutes

        // Undo/Redo state
        let undoStack = [];
        let redoStack = [];
        let lastValue = '';
        const MAX_UNDO_STACK = 50;

        // Get note number from URL
        function getNoteNumberFromUrl() {
            const urlParams = new URLSearchParams(window.location.search);
            const note = parseInt(urlParams.get('note'));
            return (note >= 1 && note <= 2147483647) ? note : 1;
        }

        // Update URL
        function updateUrl(noteNum) {
            const newUrl = window.location.pathname + '?note=' + noteNum;
            window.history.pushState({ note: noteNum }, '', newUrl);
            document.title = 'Note #' + noteNum;
        }

        // Convert URLs to clickable links
        function linkify(text) {
            const urlPattern = /(\b(https?|ftp):\/\/[-A-Z0-9+&@#\/%?=~_|!:,.;]*[-A-Z0-9+&@#\/%=~_|])/gim;
            const wwwPattern = /(^|[^\/])(www\.[\S]+(\b|$))/gim;
            const emailPattern = /(([a-zA-Z0-9\-\_\.])+@[a-zA-Z\_]+?(\.[a-zA-Z]{2,6})+)/gim;

            let result = text.replace(/&/g, '&amp;')
                            .replace(/</g, '&lt;')
                            .replace(/>/g, '&gt;');

            result = result.replace(urlPattern, '<a href=""$1"" target=""_blank"">$1</a>');
            result = result.replace(wwwPattern, '$1<a href=""http://$2"" target=""_blank"">$2</a>');
            result = result.replace(emailPattern, '<a href=""mailto:$1"">$1</a>');

            return result;
        }

        // Update rendered view
        function updateRenderedView() {
            const content = notepad.value;
            if (content.trim() === '') {
                renderedView.innerHTML = 'Note #' + currentNoteNumber + ' is empty. Double-click to start editing...';
                renderedView.classList.add('empty');
            } else {
                renderedView.innerHTML = linkify(content);
                renderedView.classList.remove('empty');
            }
        }

        // Update undo/redo button states
        function updateUndoRedoButtons() {
            btnUndo.disabled = undoStack.length === 0;
            btnRedo.disabled = redoStack.length === 0;
        }

        // Save state to undo stack
        function saveUndoState(value) {
            if (value !== lastValue) {
                undoStack.push(lastValue);
                if (undoStack.length > MAX_UNDO_STACK) {
                    undoStack.shift();
                }
                redoStack = []; // Clear redo stack on new change
                lastValue = value;
                updateUndoRedoButtons();
            }
        }

        // Undo
        function undo() {
            if (undoStack.length > 0) {
                redoStack.push(notepad.value);
                notepad.value = undoStack.pop();
                lastValue = notepad.value;
                updateUndoRedoButtons();
                markModified();
            }
        }

        // Redo
        function redo() {
            if (redoStack.length > 0) {
                undoStack.push(notepad.value);
                notepad.value = redoStack.pop();
                lastValue = notepad.value;
                updateUndoRedoButtons();
                markModified();
            }
        }

        // Switch to edit mode
        function enterEditMode() {
            isEditMode = true;
            notepad.style.display = 'block';
            renderedView.style.display = 'none';
            modeIndicator.textContent = 'Edit Mode';
            modeIndicator.classList.add('edit-mode');
            notepad.focus();
        }

        // Switch to view mode
        function enterViewMode() {
            isEditMode = false;
            notepad.style.display = 'none';
            renderedView.style.display = 'block';
            modeIndicator.textContent = 'View Mode';
            modeIndicator.classList.remove('edit-mode');
            updateRenderedView();
        }

        // Load note (called by C# via executeScript)
        function loadNote(content) {
            notepad.value = content || '';
            lastValue = notepad.value;
            undoStack = [];
            redoStack = [];
            updateUndoRedoButtons();
            hasChanges = false;
            status.textContent = content ? 'Loaded Note #' + currentNoteNumber : 'Note #' + currentNoteNumber + ' is empty';
            status.classList.remove('modified');
            updateRenderedView();
        }

        // Request load from C#
        function requestLoad(noteNum) {
            currentNoteNumber = noteNum;
            noteNumber.value = noteNum;
            updateUrl(noteNum);
            status.textContent = 'Loading...';
            
            window.chrome.webview.postMessage({
                action: 'loadNote',
                noteNumber: noteNum
            });
        }

        // Save note
        function saveNote() {
            window.chrome.webview.postMessage({
                action: 'saveNote',
                noteNumber: currentNoteNumber,
                content: notepad.value
            });
            
            hasChanges = false;
            status.textContent = 'Saved at ' + new Date().toLocaleTimeString();
            status.classList.remove('modified');
            
            // Switch to view mode after save
            enterViewMode();
        }

        // Export note
        function exportNote() {
            window.chrome.webview.postMessage({
                action: 'exportNote',
                noteNumber: currentNoteNumber,
                content: notepad.value
            });
        }

        // Clear note
        function clearNote() {
            if (confirm('Clear Note #' + currentNoteNumber + '?\\n\\nThis cannot be undone.')) {
                notepad.value = '';
                hasChanges = true;
                saveNote();
            }
        }

        // Mark modified
        function markModified() {
            if (!hasChanges) {
                hasChanges = true;
                status.textContent = 'Unsaved changes';
                status.classList.add('modified');
                
                // Schedule autosave
                if (autoSaveTimer) clearTimeout(autoSaveTimer);
                autoSaveTimer = setTimeout(() => {
                    if (hasChanges) saveNote();
                }, AUTO_SAVE_INTERVAL);
            }
        }

        // Track changes for undo/redo
        let inputTimeout;
        notepad.addEventListener('input', () => {
            markModified();
            
            // Debounce undo state saving (save after 500ms of no typing)
            clearTimeout(inputTimeout);
            inputTimeout = setTimeout(() => {
                saveUndoState(notepad.value);
            }, 500);
        });

        // Switch notes
        noteNumber.addEventListener('change', () => {
            const newNum = parseInt(noteNumber.value);
            if (isNaN(newNum) || newNum < 1) {
                noteNumber.value = currentNoteNumber;
                return;
            }
            
            if (newNum === currentNoteNumber) return;
            
            if (hasChanges && !confirm('Unsaved changes. Switch anyway?')) {
                noteNumber.value = currentNoteNumber;
                return;
            }
            
            enterViewMode();
            requestLoad(newNum);
        });

        // Double-click rendered view to edit
        renderedView.addEventListener('dblclick', () => {
            enterEditMode();
        });

        // Buttons
        btnSave.addEventListener('click', saveNote);
        btnEdit.addEventListener('click', enterEditMode);
        btnExport.addEventListener('click', exportNote);
        btnClear.addEventListener('click', clearNote);
        btnUndo.addEventListener('click', undo);
        btnRedo.addEventListener('click', redo);

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey && e.key === 's') {
                e.preventDefault();
                saveNote();
            } else if (e.ctrlKey && e.key === 'z' && !e.shiftKey) {
                e.preventDefault();
                undo();
            } else if (e.ctrlKey && (e.key === 'y' || (e.key === 'z' && e.shiftKey))) {
                e.preventDefault();
                redo();
            }
        });

        // Browser back/forward
        window.addEventListener('popstate', (event) => {
            if (event.state && event.state.note) {
                const noteNum = event.state.note;
                if (noteNum !== currentNoteNumber) {
                    if (hasChanges && !confirm('Unsaved changes. Navigate anyway?')) {
                        updateUrl(currentNoteNumber);
                        return;
                    }
                    enterViewMode();
                    requestLoad(noteNum);
                }
            }
        });

        // Initialize
        currentNoteNumber = getNoteNumberFromUrl();
        requestLoad(currentNoteNumber);
        enterViewMode();
        updateUndoRedoButtons();
    </script>
</body>
</html>";

            // Use UTF8 encoding when writing the file
            File.WriteAllText(NotepadHtmlPath, html, System.Text.Encoding.UTF8);
            return NotepadHtmlPath;
        }

        /// <summary>
        /// Escapes content for JavaScript
        /// </summary>
        public static string EscapeForJavaScript(string content)
        {
            if (string.IsNullOrEmpty(content))
                return string.Empty;

            return content
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r\n", "\\n")
                .Replace("\n", "\\n")
                .Replace("\r", "\\n")
                .Replace("\t", "\\t");
        }
    }
}