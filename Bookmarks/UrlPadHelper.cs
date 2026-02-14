using System;
using System.IO;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Helper to create the UrlPad HTML file
    /// </summary>
    public static class UrlPadHelper
    {
        private static readonly string UrlPadDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels",
            "UrlPad"
        );

        private static readonly string UrlPadHtmlPath = Path.Combine(
            UrlPadDirectory,
            "DynamicBrowserPanels_UrlPad.html"
        );

        /// <summary>
        /// Gets the UrlPad HTML file path
        /// </summary>
        public static string GetUrlPadHtmlPath()
        {
            return UrlPadHtmlPath;
        }

        /// <summary>
        /// Creates the UrlPad HTML file
        /// </summary>
        public static string CreateUrlPadHtml()
        {
            var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>UrlPad</title>
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
            flex-wrap: wrap;
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

        .toolbar button:disabled {
            background: #3e3e42;
            color: #858585;
            cursor: not-allowed;
        }

        .toolbar button.delete-btn {
            background: #c5302a;
        }

        .toolbar button.delete-btn:hover {
            background: #d9534f;
        }

        .toolbar .separator {
            width: 1px;
            height: 20px;
            background: #3e3e42;
            margin: 0 4px;
        }

        .toolbar .url-selector {
            display: flex;
            align-items: center;
            gap: 6px;
            background: #3e3e42;
            padding: 4px 8px;
            border-radius: 3px;
        }

        .toolbar .url-selector label {
            font-size: 11px;
            color: #858585;
        }

        .toolbar .url-selector input {
            background: #2d2d30;
            color: #d4d4d4;
            border: 1px solid #555;
            padding: 4px 8px;
            font-size: 12px;
            width: 110px;
            text-align: center;
            border-radius: 3px;
        }

        .toolbar .title-input {
            flex: 1;
            min-width: 200px;
            background: #2d2d30;
            color: #d4d4d4;
            border: 1px solid #555;
            padding: 6px 12px;
            font-size: 13px;
            border-radius: 3px;
        }

        .toolbar .title-input::placeholder {
            color: #858585;
        }

        .toolbar .status {
            font-size: 12px;
            color: #4ec9b0;
        }

        .toolbar .status.modified {
            color: #ce9178;
        }

        .content-area {
            flex: 1;
            display: flex;
            flex-direction: column;
            overflow: hidden;
            position: relative;
        }

        .urls-list {
            flex: 1;
            padding: 16px;
            overflow-y: auto;
        }

        .url-item {
            background: #2d2d30;
            border: 1px solid #3e3e42;
            border-radius: 4px;
            padding: 12px;
            margin-bottom: 8px;
            display: flex;
            align-items: center;
            gap: 12px;
            transition: background 0.2s;
        }

        .url-item:hover {
            background: #3e3e42;
        }

        .url-link {
            flex: 1;
            color: #4a9eff;
            text-decoration: none;
            font-size: 14px;
            overflow: hidden;
            text-overflow: ellipsis;
            white-space: nowrap;
        }

        .url-link:hover {
            text-decoration: underline;
        }

        .url-delete {
            background: #c5302a;
            color: white;
            border: none;
            padding: 4px 10px;
            border-radius: 3px;
            cursor: pointer;
            font-size: 12px;
            flex-shrink: 0;
        }

        .url-delete:hover {
            background: #d9534f;
        }

        .empty-message {
            text-align: center;
            color: #858585;
            font-style: italic;
            margin-top: 50px;
        }

        .paste-area {
            background: #2d2d30;
            border: 2px dashed #555;
            border-radius: 4px;
            padding: 24px;
            margin: 16px;
            text-align: center;
            color: #858585;
            cursor: text;
        }

        .paste-area:hover {
            border-color: #0e639c;
            background: #3e3e42;
        }

        .paste-area.focus {
            border-color: #4a9eff;
            background: #3e3e42;
        }
    </style>
</head>
<body>
    <div class=""toolbar"">
        <input type=""text"" class=""title-input"" id=""titleInput"" placeholder=""Enter list title..."">
        <div class=""separator""></div>
        <button id=""btnPaste"" title=""Paste URLs (Ctrl+V)"">📋 Paste</button>
        <button id=""btnSave"" title=""Save URLs (Ctrl+S)"">💾 Save</button>
        <button id=""btnExport"" title=""Export to File"">📤 Export</button>
        <button id=""btnClear"" class=""delete-btn"" title=""Clear All URLs"">🗑️ Clear All</button>
        <div class=""separator""></div>
        <div class=""url-selector"">
            <label for=""urlListNumber"">List #:</label>
            <input type=""number"" id=""urlListNumber"" min=""1"" max=""2147483647"" value=""1"">
        </div>
        <span class=""status"" id=""status"">Ready</span>
    </div>
    <div class=""content-area"">
        <div class=""paste-area"" id=""pasteArea"" tabindex=""0"">
            Click here and press Ctrl+V to paste URLs
        </div>
        <div class=""urls-list"" id=""urlsList"">
            <div class=""empty-message"">No URLs yet. Paste some URLs to get started!</div>
        </div>
    </div>

    <script>
        const titleInput = document.getElementById('titleInput');
        const pasteArea = document.getElementById('pasteArea');
        const urlsList = document.getElementById('urlsList');
        const urlListNumber = document.getElementById('urlListNumber');
        const btnPaste = document.getElementById('btnPaste');
        const btnSave = document.getElementById('btnSave');
        const btnExport = document.getElementById('btnExport');
        const btnClear = document.getElementById('btnClear');
        const status = document.getElementById('status');

        let currentUrlListNumber = 1;
        let currentUrls = [];
        let hasChanges = false;

        // Constants
        const MAX_LIST_NUMBER = 2147483647; // Int32.MaxValue

        // URL regex pattern
        const URL_PATTERN = /https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-A-Z0-9()@:%_\+.~#?&//=]*)/gi;

        // Get URL list number from URL
        function getUrlListNumberFromUrl() {
            const urlParams = new URLSearchParams(window.location.search);
            const list = parseInt(urlParams.get('list'));
            return (list >= 1 && list <= MAX_LIST_NUMBER) ? list : 1;
        }

        // Update URL
        function updateUrl(listNum) {
            const newUrl = window.location.pathname + '?list=' + listNum;
            window.history.replaceState({ list: listNum }, '', newUrl);
            document.title = 'UrlPad #' + listNum;
        }

        // Extract URLs from text
        function extractUrls(text) {
            const urls = text.match(URL_PATTERN);
            return urls ? [...new Set(urls)] : [];
        }

        // Add URL to list
        function addUrl(url, displayText = null) {
            const text = displayText || url;
            currentUrls.push({ url: url, displayText: text });
            markModified();
            renderUrls();
        }

        // Delete URL
        function deleteUrl(index) {
            currentUrls.splice(index, 1);
            markModified();
            renderUrls();
        }

        // Render URLs list
        function renderUrls() {
            if (currentUrls.length === 0) {
                urlsList.innerHTML = '<div class=""empty-message"">No URLs yet. Paste some URLs to get started!</div>';
                return;
            }

            let html = '';
            currentUrls.forEach((urlItem, index) => {
                html += `
                    <div class=""url-item"">
                        <a href=""${escapeHtml(urlItem.url)}"" class=""url-link"" target=""_blank"" title=""${escapeHtml(urlItem.url)}"">${escapeHtml(urlItem.displayText)}</a>
                        <button class=""url-delete"" onclick=""deleteUrlAt(${index})"">Delete</button>
                    </div>
                `;
            });
            urlsList.innerHTML = html;
        }

        // Delete URL at index (global function for onclick)
        window.deleteUrlAt = function(index) {
            deleteUrl(index);
        };

        // Escape HTML
        function escapeHtml(text) {
            const div = document.createElement('div');
            div.textContent = text;
            return div.innerHTML;
        }

        // Handle paste
        async function handlePaste() {
            try {
                const text = await navigator.clipboard.readText();
                const urls = extractUrls(text);
                
                if (urls.length === 0) {
                    status.textContent = 'No URLs found in clipboard';
                    status.style.color = '#ce9178';
                    return;
                }

                urls.forEach(url => addUrl(url));
                status.textContent = `Added ${urls.length} URL${urls.length > 1 ? 's' : ''}`;
                status.style.color = '#4ec9b0';
            } catch (err) {
                status.textContent = 'Failed to read clipboard';
                status.style.color = '#d9534f';
            }
        }

        // Mark modified
        function markModified() {
            if (!hasChanges) {
                hasChanges = true;
                status.textContent = 'Unsaved changes';
                status.style.color = '#ce9178';
            }
        }

        // Save URLs
        function saveUrls() {
            const urlList = {
                title: titleInput.value.trim() || `UrlPad #${currentUrlListNumber}`,
                urls: currentUrls
            };

            window.chrome.webview.postMessage({
                action: 'saveUrlList',
                urlListNumber: currentUrlListNumber,
                urlList: urlList
            });

            hasChanges = false;
            status.textContent = 'Saved at ' + new Date().toLocaleTimeString();
            status.style.color = '#4ec9b0';
        }

        // Load URLs (called by C# via executeScript)
        function loadUrlList(urlList) {
            if (urlList) {
                titleInput.value = urlList.title || '';
                currentUrls = urlList.urls || [];
            } else {
                titleInput.value = '';
                currentUrls = [];
            }
            
            hasChanges = false;
            renderUrls();
            status.textContent = urlList ? `Loaded ${currentUrls.length} URL${currentUrls.length !== 1 ? 's' : ''}` : 'Empty list';
            status.style.color = '#4ec9b0';
        }

        // Request load from C#
        function requestLoad(listNum) {
            currentUrlListNumber = listNum;
            urlListNumber.value = listNum;
            updateUrl(listNum);
            status.textContent = 'Loading...';
            
            window.chrome.webview.postMessage({
                action: 'loadUrlList',
                urlListNumber: listNum
            });
        }

        // Export URLs
        function exportUrls() { 
            window.chrome.webview.postMessage({
                action: 'exportUrlList',
                urlListNumber: currentUrlListNumber
            });
        }

        // Clear all URLs
        function clearUrls() {
            if (confirm(`Clear all URLs in list #${currentUrlListNumber}?\\n\\nThis cannot be undone.`)) {
                currentUrls = [];
                titleInput.value = '';
                hasChanges = true;
                renderUrls();
                saveUrls();
            }
        }

        // Event listeners
        titleInput.addEventListener('input', markModified);

        pasteArea.addEventListener('focus', () => {
            pasteArea.classList.add('focus');
        });

        pasteArea.addEventListener('blur', () => {
            pasteArea.classList.remove('focus');
        });

        pasteArea.addEventListener('click', () => {
            pasteArea.focus();
        });

        btnPaste.addEventListener('click', handlePaste);
        btnSave.addEventListener('click', saveUrls);
        btnExport.addEventListener('click', exportUrls);
        btnClear.addEventListener('click', clearUrls);

        urlListNumber.addEventListener('change', () => {
            const newNum = parseInt(urlListNumber.value);
            if (isNaN(newNum) || newNum < 1 || newNum > MAX_LIST_NUMBER) {
                urlListNumber.value = currentUrlListNumber;
                return;
            }
            
            if (newNum === currentUrlListNumber) return;
            
            if (hasChanges && !confirm('Unsaved changes. Switch anyway?')) {
                urlListNumber.value = currentUrlListNumber;
                return;
            }
            
            requestLoad(newNum);
        });

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey && e.key === 'v' && (document.activeElement === pasteArea || document.activeElement === document.body)) {
                e.preventDefault();
                handlePaste();
            } else if (e.ctrlKey && e.key === 's') {
                e.preventDefault();
                saveUrls();
            }
        });

        // Browser back/forward
        window.addEventListener('popstate', (event) => {
            if (event.state && event.state.list) {
                const listNum = event.state.list;
                if (listNum !== currentUrlListNumber) {
                    if (hasChanges && !confirm('Unsaved changes. Navigate anyway?')) {
                        updateUrl(currentUrlListNumber);
                        return;
                    }
                    requestLoad(listNum);
                }
            }
        });

        // Initialize
        currentUrlListNumber = getUrlListNumberFromUrl();
        requestLoad(currentUrlListNumber);
    </script>
</body>
</html>";

            // Ensure directory exists
            if (!Directory.Exists(UrlPadDirectory))
            {
                Directory.CreateDirectory(UrlPadDirectory);
            }

            // Use UTF8 encoding when writing the file
            File.WriteAllText(UrlPadHtmlPath, html, System.Text.Encoding.UTF8);
            return UrlPadHtmlPath;
        }
    }
}