using System;
using System.IO;
using System.Text;  

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Helper class for generating password generator HTML interface
    /// </summary>
    public static class PasswordGeneratorHelper
    {
        private static readonly string AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels"
        );

        /// <summary>
        /// Creates the password generator HTML file
        /// </summary>
        /// <returns>Path to the created HTML file</returns>
        public static string CreatePasswordGeneratorHtml()
        {
            string htmlPath = Path.Combine(AppDataDirectory, "PasswordGenerator.html");
            
            // Ensure directory exists
            Directory.CreateDirectory(AppDataDirectory);
            
            // Generate HTML content
            string html = GenerateHtml();
            
            // Write to file with UTF8 encoding
            File.WriteAllText(htmlPath, html, System.Text.Encoding.UTF8);
            
            return htmlPath;
        }

        /// <summary>
        /// Generates the complete HTML code for the password generator interface
        /// </summary>
        /// <returns>HTML string for the password generator page</returns>
        private static string GenerateHtml()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Password Generator</title>
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
            padding: 20px;
        }

        .container {
            background: #2d2d30;
            border-radius: 8px;
            padding: 30px;
            max-width: 800px;
            margin: 0 auto;
            width: 100%;
            border: 1px solid #3e3e42;
        }

        h1 {
            color: #BA98E8;
            margin-bottom: 30px;
            font-size: 28px;
        }

        .password-display {
            position: relative;
            margin-bottom: 30px;
        }

        #passwordOutput {
            width: 100%;
            padding: 15px;
            background: #1e1e1e;
            color: #BA98E8;
            border: 1px solid #3e3e42;
            border-radius: 4px;
            font-size: 18px;
            font-family: 'Courier New', Consolas, monospace;
            font-weight: bold;
        }

        #passwordOutput::placeholder {
            color: #858585;
            font-weight: normal;
        }

        .copy-feedback {
            position: absolute;
            top: 50%;
            right: 15px;
            transform: translateY(-50%);
            background: #BA98E8;
            color: #1e1e1e;
            padding: 6px 12px;
            border-radius: 3px;
            font-size: 12px;
            font-weight: bold;
            opacity: 0;
            transition: opacity 0.3s;
            pointer-events: none;
        }

        .copy-feedback.show {
            opacity: 1;
        }

        .settings-group {
            margin-bottom: 25px;
        }

        label {
            display: block;
            margin-bottom: 8px;
            color: #858585;
            font-weight: 500;
            font-size: 13px;
        }

        input[type=""number""] {
            width: 100%;
            padding: 10px;
            background: #1e1e1e;
            color: #d4d4d4;
            border: 1px solid #3e3e42;
            border-radius: 4px;
            font-size: 16px;
        }

        input[type=""text""].exclude-chars {
            width: 100%;
            padding: 10px;
            background: #1e1e1e;
            color: #d4d4d4;
            border: 1px solid #3e3e42;
            border-radius: 4px;
            font-size: 14px;
        }

        input[type=""text""].exclude-chars::placeholder {
            color: #858585;
        }

        .checkbox-group {
            display: flex;
            flex-wrap: wrap;
            gap: 20px;
            margin-bottom: 25px;
        }

        .checkbox-item {
            display: flex;
            align-items: center;
            gap: 8px;
        }

        .checkbox-item input[type=""checkbox""] {
            width: 18px;
            height: 18px;
            cursor: pointer;
            accent-color: #9370DB;
        }

        .checkbox-item label {
            margin: 0;
            cursor: pointer;
            color: #d4d4d4;
            font-size: 14px;
        }

        .button-group {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
            gap: 10px;
            margin-top: 30px;
        }

        button {
            padding: 12px 20px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
            font-weight: 500;
            transition: background-color 0.2s, transform 0.1s;
        }

        button:hover {
            transform: translateY(-1px);
        }

        button:active {
            transform: translateY(0);
        }

        .btn-primary {
            background-color: #9370DB;
            color: white;
        }

        .btn-primary:hover {
            background-color: #8A2BE2;
        }

        .btn-secondary {
            background-color: #3e3e42;
            color: #d4d4d4;
        }

        .btn-secondary:hover {
            background-color: #505050;
        }

        .btn-copy {
            background-color: #BA98E8;
            color: #1e1e1e;
            grid-column: 1 / -1;
            font-weight: bold;
        }

        .btn-copy:hover {
            background-color: #C8ACF0;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🔐 Password Generator</h1>
        
        <div class=""password-display"">
            <input type=""text"" id=""passwordOutput"" readonly placeholder=""Generated password will appear here..."">
            <span id=""copyFeedback"" class=""copy-feedback"">Copied!</span>
        </div>

        <div class=""settings-group"">
            <label for=""passwordLength"">Password Length:</label>
            <input type=""number"" id=""passwordLength"" min=""8"" max=""128"" value=""32"">
        </div>

        <div class=""checkbox-group"">
            <div class=""checkbox-item"">
                <input type=""checkbox"" id=""includeSymbols"">
                <label for=""includeSymbols"">Include Symbols</label>
            </div>
            <div class=""checkbox-item"">
                <input type=""checkbox"" id=""addExclamation"" checked>
                <label for=""addExclamation"">Add Exclamation Suffix (!)</label>
            </div>
            <div class=""checkbox-item"">
                <input type=""checkbox"" id=""excludeChars"">
                <label for=""excludeChars"">Exclude Characters</label>
            </div>
        </div>

        <div class=""settings-group"" id=""excludeCharsGroup"" style=""display: none;"">
            <label for=""excludeCharacters"">Characters to Exclude:</label>
            <input type=""text"" class=""exclude-chars"" id=""excludeCharacters"" placeholder=""e.g., 0OIl1"">
        </div>

        <div class=""button-group"">
            <button class=""btn-primary"" onclick=""generateRandomPassword()"">Random Password</button>
            <button class=""btn-secondary"" onclick=""generateApplePassword()"">Cell Phone Password</button>
            <button class=""btn-secondary"" onclick=""generateAppleStylePassword()"">Apple-Style Password</button>
            <button class=""btn-secondary"" onclick=""generateXKCDPassword()"">XKCD Password</button>
            <button class=""btn-copy"" onclick=""copyPassword()"">📋 Copy to Clipboard</button>
        </div>
    </div>

    <script>
        // Show/hide exclude characters input
        document.getElementById('excludeChars').addEventListener('change', function() {
            document.getElementById('excludeCharsGroup').style.display = this.checked ? 'block' : 'none';
        });

        function generateRandomPassword() {
            const length = parseInt(document.getElementById('passwordLength').value);
            const includeSymbols = document.getElementById('includeSymbols').checked;
            const addExclamation = document.getElementById('addExclamation').checked;
            const excludeChars = document.getElementById('excludeChars').checked 
                ? document.getElementById('excludeCharacters').value 
                : '';

            window.chrome.webview.postMessage({
                action: 'generatePassword',
                length: length,
                includeSymbols: includeSymbols,
                excludeCharacters: excludeChars,
                appleStyle: false,
                addExclamationSuffix: addExclamation
            });
        }

        function generateApplePassword() {
            const length = parseInt(document.getElementById('passwordLength').value);
            const includeSymbols = document.getElementById('includeSymbols').checked;
            const addExclamation = document.getElementById('addExclamation').checked;
            const excludeChars = document.getElementById('excludeChars').checked 
                ? document.getElementById('excludeCharacters').value 
                : '';

            window.chrome.webview.postMessage({
                action: 'generateApplePassword',
                length: length,
                includeSymbols: includeSymbols,
                excludeCharacters: excludeChars,
                appleStyle: true,
                addExclamationSuffix: addExclamation
            });
        }

        function generateAppleStylePassword() {
            const length = parseInt(document.getElementById('passwordLength').value);
            const includeSymbols = document.getElementById('includeSymbols').checked;
            const addExclamation = document.getElementById('addExclamation').checked;
            const excludeChars = document.getElementById('excludeChars').checked 
                ? document.getElementById('excludeCharacters').value 
                : '';

            window.chrome.webview.postMessage({
                action: 'generatePassword',
                length: length,
                includeSymbols: includeSymbols,
                excludeCharacters: excludeChars,
                appleStyle: true,
                addExclamationSuffix: addExclamation
            });
        }

        function generateXKCDPassword() {
            window.chrome.webview.postMessage({
                action: 'generateXKCDPassword',
                wordCount: 4
            });
        }

        function copyPassword() {
            const passwordField = document.getElementById('passwordOutput');
            passwordField.select();
            document.execCommand('copy');
            
            const feedback = document.getElementById('copyFeedback');
            feedback.classList.add('show');
            setTimeout(() => feedback.classList.remove('show'), 2000);
        }

        // Listen for password generation results
        window.chrome.webview.addEventListener('message', event => {
            if (event.data.type === 'passwordGenerated') {
                document.getElementById('passwordOutput').value = event.data.password;
            }
        });
    </script>
</body>
</html>";
        }
    }
}