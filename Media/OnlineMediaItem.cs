using System;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Represents a single online media item (YouTube video, streaming URL, etc.)
    /// </summary>
    public class OnlineMediaItem
    {
        /// <summary>
        /// URL of the media (YouTube, Dropbox, direct media URL, etc.)
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Custom display name for the item
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Optional description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Type of media (YouTube, Dropbox, DirectStream, Embed, etc.)
        /// </summary>
        public OnlineMediaType MediaType { get; set; }

        /// <summary>
        /// Optional thumbnail URL
        /// </summary>
        public string ThumbnailUrl { get; set; }

        /// <summary>
        /// Duration in seconds (if known)
        /// </summary>
        public int? DurationSeconds { get; set; }

        /// <summary>
        /// Tags for organization
        /// </summary>
        public string[] Tags { get; set; }

        /// <summary>
        /// When this item was added to the playlist
        /// </summary>
        public DateTime DateAdded { get; set; } = DateTime.Now;

        public OnlineMediaItem()
        {
        }

        public OnlineMediaItem(string url, string displayName = null, OnlineMediaType mediaType = OnlineMediaType.Unknown)
        {
            Url = url;
            DisplayName = displayName ?? GetDefaultDisplayName(url);
            MediaType = mediaType != OnlineMediaType.Unknown ? mediaType : DetectMediaType(url);
        }

        private static string GetDefaultDisplayName(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return "Untitled";

            try
            {
                var uri = new Uri(url);
                
                // For YouTube, try to extract video ID
                if (uri.Host.Contains("youtube.com") || uri.Host.Contains("youtu.be"))
                {
                    return $"YouTube Video";
                }
                
                // For other URLs, use the path or host
                if (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
                {
                    var segments = uri.AbsolutePath.Split('/');
                    return segments[segments.Length - 1];
                }
                
                return uri.Host;
            }
            catch
            {
                return url.Length > 50 ? url.Substring(0, 50) + "..." : url;
            }
        }

        private static OnlineMediaType DetectMediaType(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return OnlineMediaType.Unknown;

            url = url.ToLower();

            if (url.Contains("youtube.com") || url.Contains("youtu.be"))
                return OnlineMediaType.YouTube;
            
            if (url.Contains("dropbox.com"))
                return OnlineMediaType.Dropbox;
            
            if (url.Contains("vimeo.com"))
                return OnlineMediaType.Vimeo;
            
            if (url.Contains("soundcloud.com"))
                return OnlineMediaType.SoundCloud;
            
            if (url.EndsWith(".mp4") || url.EndsWith(".webm") || url.EndsWith(".mp3") || 
                url.EndsWith(".wav") || url.EndsWith(".ogg"))
                return OnlineMediaType.DirectStream;

            return OnlineMediaType.Embed;
        }
    }

    /// <summary>
    /// Type of online media source
    /// </summary>
    public enum OnlineMediaType
    {
        Unknown,
        YouTube,
        Vimeo,
        Dropbox,
        SoundCloud,
        DirectStream,  // Direct MP4, MP3, etc. URL
        Embed          // Generic embed/iframe
    }
}