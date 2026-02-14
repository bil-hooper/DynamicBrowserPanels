using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages saving and loading images for the ImagePad
    /// </summary>
    public static class ImagePadManager
    {
        private static readonly string ImagesDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DynamicBrowserPanels",
            "Images"
        );

        /// <summary>
        /// Gets the path to the images directory
        /// </summary>
        public static string GetImagesDirectoryPath()
        {
            return ImagesDirectory;
        }

        /// <summary>
        /// Saves an image from base64 data
        /// </summary>
        public static bool SaveImage(int imageNumber, string base64Data)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(base64Data))
                    return false;

                // Ensure directory exists
                if (!Directory.Exists(ImagesDirectory))
                {
                    Directory.CreateDirectory(ImagesDirectory);
                }

                // Extract base64 content (remove data:image/png;base64, prefix if present)
                string base64Content = base64Data;
                if (base64Data.Contains(","))
                {
                    base64Content = base64Data.Substring(base64Data.IndexOf(",") + 1);
                }

                // Convert to bytes
                byte[] imageBytes = Convert.FromBase64String(base64Content);

                // Save to file
                string filePath = GetImageFilePath(imageNumber);
                File.WriteAllBytes(filePath, imageBytes);

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save image {imageNumber}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Loads an image as base64 data
        /// </summary>
        public static string LoadImage(int imageNumber)
        {
            try
            {
                string filePath = GetImageFilePath(imageNumber);

                if (!File.Exists(filePath))
                    return null;

                byte[] imageBytes = File.ReadAllBytes(filePath);
                string base64 = Convert.ToBase64String(imageBytes);
                
                // Return as data URL
                return $"data:image/png;base64,{base64}";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load image {imageNumber}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Exports an image to a file in the specified format
        /// </summary>
        public static bool ExportImage(int imageNumber, string filePath, ImageFormat format)
        {
            string sourceFilePath = GetImageFilePath(imageNumber);

            try
            {
                if (!File.Exists(sourceFilePath))
                {
                    System.Diagnostics.Debug.WriteLine($"Source file not found: {sourceFilePath}");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine($"Exporting image {imageNumber} to {filePath} in format {format}");

                // For PNG, just copy the file directly (most efficient and safest)
                if (format.Equals(ImageFormat.Png))
                {
                    File.Copy(sourceFilePath, filePath, overwrite: true);
                    System.Diagnostics.Debug.WriteLine("PNG copied successfully");
                    return true;
                }

                // For other formats, we need to convert
                // Use a non-locking file read
                byte[] imageBytes = File.ReadAllBytes(sourceFilePath);
                System.Diagnostics.Debug.WriteLine($"Read {imageBytes.Length} bytes from source");
                
                // Create image from bytes with minimal validation
                using (var ms = new MemoryStream(imageBytes))
                {
                    // Don't use 'using' on the image yet - need to create bitmap first for some formats
                    Image sourceImage = null;
                    try
                    {
                        ms.Position = 0;
                        sourceImage = Image.FromStream(ms, useEmbeddedColorManagement: false, validateImageData: false);
                        System.Diagnostics.Debug.WriteLine($"Image loaded: {sourceImage.Width}x{sourceImage.Height}");
                        
                        // For JPEG and BMP, direct save usually works
                        if (format.Equals(ImageFormat.Jpeg) || format.Equals(ImageFormat.Bmp))
                        {
                            sourceImage.Save(filePath, format);
                            System.Diagnostics.Debug.WriteLine($"{format} saved successfully");
                        }
                        else
                        {
                            // For any other format, convert through bitmap first
                            using (var bitmap = new Bitmap(sourceImage))
                            {
                                bitmap.Save(filePath, format);
                                System.Diagnostics.Debug.WriteLine($"{format} (via Bitmap) saved successfully");
                            }
                        }
                    }
                    finally
                    {
                        sourceImage?.Dispose();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to export image {imageNumber}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Exception type: {ex.GetType().Name}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                throw; // Re-throw to show user-friendly error
            }
        }

        /// <summary>
        /// Deletes an image
        /// </summary>
        public static bool DeleteImage(int imageNumber)
        {
            try
            {
                string filePath = GetImageFilePath(imageNumber);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to delete image {imageNumber}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gets the file path for an image number
        /// </summary>
        private static string GetImageFilePath(int imageNumber)
        {
            return Path.Combine(ImagesDirectory, $"Image_{imageNumber}.png");
        }

        /// <summary>
        /// Checks if an image exists
        /// </summary>
        public static bool ImageExists(int imageNumber)
        {
            string filePath = GetImageFilePath(imageNumber);
            return File.Exists(filePath);
        }
    }
}