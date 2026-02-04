using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages media playlists
    /// </summary>
    public class PlaylistManager
    {
        private List<string> _mediaFiles = new List<string>();
        private List<int> _shuffleOrder = new List<int>();
        private int _currentIndex = 0;
        private bool _shuffle = false;
        private bool _repeat = false;
        private string _playlistName;

        public event EventHandler<string> MediaChanged;
        public event EventHandler PlaylistEnded;

        public int CurrentIndex => _shuffle ? _shuffleOrder[_currentIndex] : _currentIndex;
        public int Count => _mediaFiles.Count;
        public bool HasNext => _repeat || (_currentIndex < _mediaFiles.Count - 1);
        public bool HasPrevious => _repeat || (_currentIndex > 0);
        public string CurrentFile => _mediaFiles.Count > 0 && _currentIndex < _mediaFiles.Count 
            ? _mediaFiles[CurrentIndex] 
            : null;
        public bool Shuffle { get => _shuffle; set { _shuffle = value; GenerateShuffleOrder(); } }
        public bool Repeat { get => _repeat; set => _repeat = value; }

        public List<string> MediaFiles => new List<string>(_mediaFiles);

        /// <summary>
        /// Load playlist from M3U file
        /// </summary>
        public bool LoadFromM3U(string filePath)
        {
            try
            {
                _mediaFiles.Clear();
                string baseDir = Path.GetDirectoryName(filePath);
                
                foreach (var line in File.ReadAllLines(filePath))
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    string mediaPath = line.Trim();
                    
                    // Handle relative paths
                    if (!Path.IsPathRooted(mediaPath))
                    {
                        mediaPath = Path.Combine(baseDir, mediaPath);
                    }

                    if (File.Exists(mediaPath))
                    {
                        _mediaFiles.Add(mediaPath);
                    }
                }

                _playlistName = Path.GetFileNameWithoutExtension(filePath);
                _currentIndex = 0;
                GenerateShuffleOrder();
                return _mediaFiles.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Load playlist from folder (all media files)
        /// </summary>
        public bool LoadFromFolder(string folderPath)
        {
            try
            {
                _mediaFiles.Clear();
                
                var extensions = new[] { ".mp4", ".webm", ".ogv", ".ogg", ".mp3", ".wav", ".aac", ".m4a", ".flac", ".opus" };
                
                foreach (var ext in extensions)
                {
                    _mediaFiles.AddRange(Directory.GetFiles(folderPath, $"*{ext}", SearchOption.TopDirectoryOnly));
                }

                _mediaFiles.Sort(); // Sort alphabetically
                _playlistName = Path.GetFileName(folderPath);
                _currentIndex = 0;
                GenerateShuffleOrder();
                return _mediaFiles.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Add files manually
        /// </summary>
        public void AddFiles(params string[] files)
        {
            foreach (var file in files)
            {
                if (File.Exists(file) && !_mediaFiles.Contains(file))
                {
                    _mediaFiles.Add(file);
                }
            }
            GenerateShuffleOrder();
        }

        /// <summary>
        /// Remove file from playlist
        /// </summary>
        public void RemoveFile(int index)
        {
            if (index >= 0 && index < _mediaFiles.Count)
            {
                _mediaFiles.RemoveAt(index);
                if (_currentIndex >= _mediaFiles.Count)
                {
                    _currentIndex = Math.Max(0, _mediaFiles.Count - 1);
                }
                GenerateShuffleOrder();
            }
        }

        /// <summary>
        /// Move to next track
        /// </summary>
        public string Next()
        {
            if (_mediaFiles.Count == 0) return null;

            _currentIndex++;
            
            if (_currentIndex >= _mediaFiles.Count)
            {
                if (_repeat)
                {
                    _currentIndex = 0;
                }
                else
                {
                    _currentIndex = _mediaFiles.Count - 1;
                    PlaylistEnded?.Invoke(this, EventArgs.Empty);
                    return null;
                }
            }

            var file = CurrentFile;
            MediaChanged?.Invoke(this, file);
            return file;
        }

        /// <summary>
        /// Move to previous track
        /// </summary>
        public string Previous()
        {
            if (_mediaFiles.Count == 0) return null;

            _currentIndex--;
            
            if (_currentIndex < 0)
            {
                if (_repeat)
                {
                    _currentIndex = _mediaFiles.Count - 1;
                }
                else
                {
                    _currentIndex = 0;
                    return null;
                }
            }

            var file = CurrentFile;
            MediaChanged?.Invoke(this, file);
            return file;
        }

        /// <summary>
        /// Jump to specific index
        /// </summary>
        public string JumpTo(int index)
        {
            if (index >= 0 && index < _mediaFiles.Count)
            {
                _currentIndex = index;
                var file = CurrentFile;
                MediaChanged?.Invoke(this, file);
                return file;
            }
            return null;
        }

        /// <summary>
        /// Get current state for saving
        /// </summary>
        public PlaylistStateData GetState()
        {
            return new PlaylistStateData
            {
                MediaFiles = new List<string>(_mediaFiles),
                CurrentIndex = _currentIndex,
                Shuffle = _shuffle,
                Repeat = _repeat,
                PlaylistName = _playlistName
            };
        }

        /// <summary>
        /// Restore from saved state
        /// </summary>
        public void RestoreState(PlaylistStateData state)
        {
            if (state == null) return;

            _mediaFiles = new List<string>(state.MediaFiles ?? new List<string>());
            _currentIndex = state.CurrentIndex;
            _shuffle = state.Shuffle;
            _repeat = state.Repeat;
            _playlistName = state.PlaylistName;
            GenerateShuffleOrder();
        }

        /// <summary>
        /// Save playlist to M3U file
        /// </summary>
        public void SaveToM3U(string filePath)
        {
            var lines = new List<string> { "#EXTM3U" };
            lines.AddRange(_mediaFiles);
            File.WriteAllLines(filePath, lines);
        }

        private void GenerateShuffleOrder()
        {
            _shuffleOrder.Clear();
            for (int i = 0; i < _mediaFiles.Count; i++)
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