using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages online media playlists (YouTube, Dropbox, streaming URLs, etc.)
    /// </summary>
    public class OnlineMediaPlaylist
    {
        private List<OnlineMediaItem> _mediaItems = new List<OnlineMediaItem>();
        private List<int> _shuffleOrder = new List<int>();
        private int _currentIndex = 0;
        private bool _shuffle = false;
        private bool _repeat = false;
        private string _playlistName;

        public event EventHandler<OnlineMediaItem> MediaChanged;
        public event EventHandler PlaylistEnded;

        public int CurrentIndex => _shuffle ? _shuffleOrder[_currentIndex] : _currentIndex;
        public int Count => _mediaItems.Count;
        public bool HasNext => _repeat || (_currentIndex < _mediaItems.Count - 1);
        public bool HasPrevious => _repeat || (_currentIndex > 0);
        public OnlineMediaItem CurrentItem => _mediaItems.Count > 0 && _currentIndex < _mediaItems.Count 
            ? _mediaItems[CurrentIndex] 
            : null;
        public bool Shuffle { get => _shuffle; set { _shuffle = value; GenerateShuffleOrder(); } }
        public bool Repeat { get => _repeat; set => _repeat = value; }
        public string PlaylistName { get => _playlistName; set => _playlistName = value; }

        public List<OnlineMediaItem> MediaItems => new List<OnlineMediaItem>(_mediaItems);

        /// <summary>
        /// Load playlist from JSON file
        /// </summary>
        public bool LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var json = File.ReadAllText(filePath);
                var state = System.Text.Json.JsonSerializer.Deserialize<OnlinePlaylistStateData>(json);
                
                if (state != null)
                {
                    RestoreState(state);
                    return true;
                }
            }
            catch
            {
                // Ignore errors
            }
            return false;
        }

        /// <summary>
        /// Save playlist to JSON file
        /// </summary>
        public bool SaveToFile(string filePath)
        {
            try
            {
                var state = GetState();
                var json = System.Text.Json.JsonSerializer.Serialize(state, new System.Text.Json.JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                var directory = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(filePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Add a single item to the playlist
        /// </summary>
        public void AddItem(OnlineMediaItem item)
        {
            if (item != null && !string.IsNullOrWhiteSpace(item.Url))
            {
                _mediaItems.Add(item);
                GenerateShuffleOrder();
            }
        }

        /// <summary>
        /// Add multiple items to the playlist
        /// </summary>
        public void AddItems(params OnlineMediaItem[] items)
        {
            foreach (var item in items)
            {
                if (item != null && !string.IsNullOrWhiteSpace(item.Url))
                {
                    _mediaItems.Add(item);
                }
            }
            GenerateShuffleOrder();
        }

        /// <summary>
        /// Remove item from playlist
        /// </summary>
        public void RemoveItem(int index)
        {
            if (index >= 0 && index < _mediaItems.Count)
            {
                _mediaItems.RemoveAt(index);
                if (_currentIndex >= _mediaItems.Count)
                {
                    _currentIndex = Math.Max(0, _mediaItems.Count - 1);
                }
                GenerateShuffleOrder();
            }
        }

        /// <summary>
        /// Clear all items from playlist
        /// </summary>
        public void Clear()
        {
            _mediaItems.Clear();
            _currentIndex = 0;
            GenerateShuffleOrder();
        }

        /// <summary>
        /// Move to next track
        /// </summary>
        public OnlineMediaItem Next()
        {
            if (_mediaItems.Count == 0) return null;

            _currentIndex++;
            
            if (_currentIndex >= _mediaItems.Count)
            {
                if (_repeat)
                {
                    _currentIndex = 0;
                }
                else
                {
                    _currentIndex = _mediaItems.Count - 1;
                    PlaylistEnded?.Invoke(this, EventArgs.Empty);
                    return null;
                }
            }

            var item = CurrentItem;
            MediaChanged?.Invoke(this, item);
            return item;
        }

        /// <summary>
        /// Move to previous track
        /// </summary>
        public OnlineMediaItem Previous()
        {
            if (_mediaItems.Count == 0) return null;

            _currentIndex--;
            
            if (_currentIndex < 0)
            {
                if (_repeat)
                {
                    _currentIndex = _mediaItems.Count - 1;
                }
                else
                {
                    _currentIndex = 0;
                    return null;
                }
            }

            var item = CurrentItem;
            MediaChanged?.Invoke(this, item);
            return item;
        }

        /// <summary>
        /// Jump to specific index
        /// </summary>
        public OnlineMediaItem JumpTo(int index)
        {
            if (index >= 0 && index < _mediaItems.Count)
            {
                _currentIndex = index;
                var item = CurrentItem;
                MediaChanged?.Invoke(this, item);
                return item;
            }
            return null;
        }

        /// <summary>
        /// Get current state for saving
        /// </summary>
        public OnlinePlaylistStateData GetState()
        {
            return new OnlinePlaylistStateData
            {
                MediaItems = new List<OnlineMediaItem>(_mediaItems),
                CurrentIndex = _currentIndex,
                Shuffle = _shuffle,
                Repeat = _repeat,
                PlaylistName = _playlistName,
                MachineName = _mediaItems.Count > 0 ? Environment.MachineName : null
            };
        }

        /// <summary>
        /// Restore from saved state
        /// </summary>
        public void RestoreState(OnlinePlaylistStateData state)
        {
            if (state == null) return;

            _mediaItems = new List<OnlineMediaItem>(state.MediaItems ?? new List<OnlineMediaItem>());
            _currentIndex = state.CurrentIndex;
            _shuffle = state.Shuffle;
            _repeat = state.Repeat;
            _playlistName = state.PlaylistName;
            GenerateShuffleOrder();
        }

        private void GenerateShuffleOrder()
        {
            _shuffleOrder.Clear();
            for (int i = 0; i < _mediaItems.Count; i++)
            {
                _shuffleOrder.Add(i);
            }

            if (_shuffle)
            {
                var random = new Random();
                for (int i = _shuffleOrder.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    (_shuffleOrder[i], _shuffleOrder[j]) = (_shuffleOrder[j], _shuffleOrder[i]);
                }
            }
        }
    }
}