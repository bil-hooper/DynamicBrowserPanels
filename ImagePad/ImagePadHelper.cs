using System;
using System.IO;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Helper to create the image pad HTML file
    /// </summary>
    public static class ImagePadHelper
    {
        private static readonly string ImagePadDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels",
            "ImagePads"
        );

        private static readonly string ImagePadHtmlPath = Path.Combine(
            ImagePadDirectory,
            "DynamicBrowserPanels_ImagePad.html"
        );

        /// <summary>
        /// Gets the image pad HTML file path
        /// </summary>
        public static string GetImagePadHtmlPath()
        {
            return ImagePadHtmlPath;
        }

        /// <summary>
        /// Creates the image pad HTML file
        /// </summary>
        public static string CreateImagePadHtml()
        {
            var html = @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Image Pad</title>
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
            background: #9370DB;
            color: white;
            border: none;
            padding: 6px 12px;
            border-radius: 3px;
            cursor: pointer;
            font-size: 13px;
        }

        .toolbar button:hover {
            background: #8A2BE2;
        }

        .toolbar button:disabled {
            background: #3e3e42;
            color: #858585;
            cursor: not-allowed;
        }

        .toolbar .separator {
            width: 1px;
            height: 20px;
            background: #3e3e42;
            margin: 0 4px;
        }

        .toolbar .image-selector {
            display: flex;
            align-items: center;
            gap: 6px;
            background: #3e3e42;
            padding: 4px 8px;
            border-radius: 3px;
        }

        .toolbar .image-selector label {
            font-size: 11px;
            color: #858585;
        }

        .toolbar .image-selector input {
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

        .content-area {
            flex: 1;
            display: flex;
            align-items: center;
            justify-content: center;
            overflow: hidden;
            position: relative;
            background: #252526;
        }

        #imageContainer {
            max-width: 100%;
            max-height: 100%;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }

        #imageDisplay {
            max-width: 100%;
            max-height: 100%;
            object-fit: contain;
            display: none;
        }

        #emptyMessage {
            color: #858585;
            font-size: 16px;
            text-align: center;
            font-style: italic;
        }
    </style>
</head>
<body>
    <div class=""toolbar"">
        <button id=""btnCopy"" title=""Copy to Clipboard"">Copy</button>
        <button id=""btnPaste"" title=""Paste from Clipboard (Ctrl+V)"">Paste</button>
        <button id=""btnExport"" title=""Export Image"">Export</button>        
        <div class=""separator""></div>
        <button id=""btnRotateLeft"" title=""Rotate Left 90°"">⟲ Rotate Left</button>
        <button id=""btnRotateRight"" title=""Rotate Right 90°"">⟳ Rotate Right</button>
        <button id=""btnInvert"" title=""Invert Colors"">Invert</button>
        <div class=""separator""></div>
        <button id=""btnClear"" title=""Delete Image"">Clear</button>
        <div class=""separator""></div>
        <div class=""image-selector"">
            <label for=""imageNumber"">Image #:</label>
            <input type=""number"" id=""imageNumber"" min=""1"" max=""2147483647"" value=""1"">
        </div>
        <span class=""status"" id=""status"">Ready</span>
    </div>
    <div class=""content-area"">
        <div id=""imageContainer"">
            <img id=""imageDisplay"" alt=""Image"">
            <div id=""emptyMessage"">No image. Press Paste or Ctrl+V to add one.</div>
        </div>
    </div>

    <script>
        const imageDisplay = document.getElementById('imageDisplay');
        const emptyMessage = document.getElementById('emptyMessage');
        const imageNumber = document.getElementById('imageNumber');
        const btnCopy = document.getElementById('btnCopy');
        const btnPaste = document.getElementById('btnPaste');
        const btnRotateLeft = document.getElementById('btnRotateLeft');
        const btnRotateRight = document.getElementById('btnRotateRight');
        const btnInvert = document.getElementById('btnInvert');
        const btnClear = document.getElementById('btnClear');
        const btnExport = document.getElementById('btnExport');
        const status = document.getElementById('status');

        let currentImageNumber = 1;
        let currentRotation = 0;
        let isInverted = false;

        // Get image number from URL
        function getImageNumberFromUrl() {
            const urlParams = new URLSearchParams(window.location.search);
            const img = parseInt(urlParams.get('image'));
            return (img >= 1 && img <= 2147483647) ? img : 1;
        }

        // Update URL
        function updateUrl(imgNum) {
            const newUrl = window.location.pathname + '?image=' + imgNum;
            window.history.replaceState({ image: imgNum }, '', newUrl);
            document.title = 'Image #' + imgNum;
        }

        // Update button states function to include export
        function updateButtonStates() {
            const hasImage = imageDisplay.style.display !== 'none';
            btnCopy.disabled = !hasImage;
            btnRotateLeft.disabled = !hasImage;
            btnRotateRight.disabled = !hasImage;
            btnInvert.disabled = !hasImage;
            btnClear.disabled = !hasImage;
            btnExport.disabled = !hasImage;
        }

        // Apply transformations
        function applyTransformations() {
            let transform = `rotate(${currentRotation}deg)`;
            let filter = isInverted ? 'invert(1)' : 'invert(0)';
            imageDisplay.style.transform = transform;
            imageDisplay.style.filter = filter;
        }

        // Show image
        function showImage(base64Data) {
            if (!base64Data) {
                imageDisplay.style.display = 'none';
                emptyMessage.style.display = 'block';
                currentRotation = 0;
                isInverted = false;
                updateButtonStates();
                return;
            }

            imageDisplay.src = base64Data;
            imageDisplay.style.display = 'block';
            emptyMessage.style.display = 'none';
            currentRotation = 0;
            isInverted = false;
            applyTransformations();
            updateButtonStates();
            status.textContent = 'Image #' + currentImageNumber + ' loaded';
        }

        // Load image (called by C#)
        function loadImage(base64Data) {
            showImage(base64Data);
        }

        // Request load from C#
        function requestLoad(imgNum) {
            currentImageNumber = imgNum;
            imageNumber.value = imgNum;
            updateUrl(imgNum);
            status.textContent = 'Loading...';
            
            window.chrome.webview.postMessage({
                action: 'loadImage',
                imageNumber: imgNum
            });
        }

        // Copy to clipboard
        async function copyToClipboard() {
            if (imageDisplay.style.display === 'none') return;

            try {
                status.textContent = 'Copying...';
                
                // Create a canvas to apply transformations
                const canvas = document.createElement('canvas');
                const ctx = canvas.getContext('2d');
                
                // Account for rotation
                const isRotated90 = currentRotation % 180 !== 0;
                canvas.width = isRotated90 ? imageDisplay.naturalHeight : imageDisplay.naturalWidth;
                canvas.height = isRotated90 ? imageDisplay.naturalWidth : imageDisplay.naturalHeight;
                
                ctx.save();
                ctx.translate(canvas.width / 2, canvas.height / 2);
                ctx.rotate((currentRotation * Math.PI) / 180);
                
                if (isInverted) {
                    ctx.filter = 'invert(1)';
                }
                
                ctx.drawImage(imageDisplay, -imageDisplay.naturalWidth / 2, -imageDisplay.naturalHeight / 2);
                ctx.restore();
                
                canvas.toBlob(async (blob) => {
                    try {
                        await navigator.clipboard.write([
                            new ClipboardItem({ 'image/png': blob })
                        ]);
                        status.textContent = 'Copied to clipboard';
                    } catch (err) {
                        status.textContent = 'Copy failed: ' + err.message;
                    }
                });
            } catch (err) {
                status.textContent = 'Copy failed: ' + err.message;
            }
        }

        // Paste from clipboard
        async function pasteFromClipboard() {
            try {
                status.textContent = 'Pasting...';
                const clipboardItems = await navigator.clipboard.read();
                
                for (const item of clipboardItems) {
                    if (item.types.includes('image/png')) {
                        const blob = await item.getType('image/png');
                        const reader = new FileReader();
                        
                        reader.onload = (e) => {
                            const base64 = e.target.result;
                            
                            // Send to C# to save
                            window.chrome.webview.postMessage({
                                action: 'saveImage',
                                imageNumber: currentImageNumber,
                                base64Data: base64
                            });
                            
                            // Display immediately
                            showImage(base64);
                            status.textContent = 'Image pasted';
                        };
                        
                        reader.readAsDataURL(blob);
                        return;
                    }
                }
                
                status.textContent = 'No image in clipboard';
            } catch (err) {
                status.textContent = 'Paste failed: ' + err.message;
            }
        }

        // Rotate left
        function rotateLeft() {
            currentRotation = (currentRotation - 90) % 360;
            if (currentRotation < 0) currentRotation += 360;
            applyTransformations();
            
            // Save the modified image
            saveCurrentImage();
        }

        // Rotate right
        function rotateRight() {
            currentRotation = (currentRotation + 90) % 360;
            applyTransformations();
            
            // Save the modified image
            saveCurrentImage();
        }

        // Invert colors
        function invertColors() {
            isInverted = !isInverted;
            applyTransformations();
            
            // Save the modified image
            saveCurrentImage();
        }

        // Save current image with transformations
        function saveCurrentImage() {
            if (imageDisplay.style.display === 'none') return;

            status.textContent = 'Saving...';
            
            // Create a canvas to apply transformations
            const canvas = document.createElement('canvas');
            const ctx = canvas.getContext('2d');
            
            // Account for rotation
            const isRotated90 = currentRotation % 180 !== 0;
            canvas.width = isRotated90 ? imageDisplay.naturalHeight : imageDisplay.naturalWidth;
            canvas.height = isRotated90 ? imageDisplay.naturalWidth : imageDisplay.naturalHeight;
            
            ctx.save();
            ctx.translate(canvas.width / 2, canvas.height / 2);
            ctx.rotate((currentRotation * Math.PI) / 180);
            
            if (isInverted) {
                ctx.filter = 'invert(1)';
            }
            
            ctx.drawImage(imageDisplay, -imageDisplay.naturalWidth / 2, -imageDisplay.naturalHeight / 2);
            ctx.restore();
            
            const base64 = canvas.toDataURL('image/png');
            
            // Send to C# to save
            window.chrome.webview.postMessage({
                action: 'saveImage',
                imageNumber: currentImageNumber,
                base64Data: base64
            });
            
            // Reset transformations since we saved them
            currentRotation = 0;
            isInverted = false;
            imageDisplay.src = base64;
            applyTransformations();
            
            status.textContent = 'Saved at ' + new Date().toLocaleTimeString();
        }

        // Export image
        function exportImage() {
            if (imageDisplay.style.display === 'none') return;

            status.textContent = 'Exporting...';
            
            window.chrome.webview.postMessage({
                action: 'exportImage',
                imageNumber: currentImageNumber
            });
            
            status.textContent = 'Ready';
        }

        // Clear image
        function clearImage() {
            if (imageDisplay.style.display === 'none') return;

            if (confirm('Delete Image #' + currentImageNumber + '?\\n\\nThis cannot be undone.')) {
                window.chrome.webview.postMessage({
                    action: 'deleteImage',
                    imageNumber: currentImageNumber
                });
                
                showImage(null);
                status.textContent = 'Image deleted';
            }
        }

        // Switch images
        imageNumber.addEventListener('change', () => {
            const newNum = parseInt(imageNumber.value);
            if (isNaN(newNum) || newNum < 1) {
                imageNumber.value = currentImageNumber;
                return;
            }
            
            if (newNum === currentImageNumber) return;
            
            requestLoad(newNum);
        });

        // Buttons
        btnCopy.addEventListener('click', copyToClipboard);
        btnPaste.addEventListener('click', pasteFromClipboard);
        btnRotateLeft.addEventListener('click', rotateLeft);
        btnRotateRight.addEventListener('click', rotateRight);
        btnInvert.addEventListener('click', invertColors);
        btnClear.addEventListener('click', clearImage);
        btnExport.addEventListener('click', exportImage);

        // Keyboard shortcuts
        document.addEventListener('keydown', (e) => {
            if (e.ctrlKey && e.key === 'v') {
                e.preventDefault();
                pasteFromClipboard();
            } else if (e.ctrlKey && e.key === 'c') {
                e.preventDefault();
                copyToClipboard();
            }
        });

        // Browser back/forward
        window.addEventListener('popstate', (event) => {
            if (event.state && event.state.image) {
                const imgNum = event.state.image;
                if (imgNum !== currentImageNumber) {
                    requestLoad(imgNum);
                }
            }
        });

        // Initialize
        currentImageNumber = getImageNumberFromUrl();
        requestLoad(currentImageNumber);
        updateButtonStates();
    </script>
</body>
</html>";

            // Ensure directory exists
            if (!Directory.Exists(ImagePadDirectory))
            {
                Directory.CreateDirectory(ImagePadDirectory);
            }

            // Use UTF8 encoding when writing the file
            File.WriteAllText(ImagePadHtmlPath, html, System.Text.Encoding.UTF8);
            return ImagePadHtmlPath;
        }
    }
}