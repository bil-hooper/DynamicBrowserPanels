using System;
using System.Drawing;
using System.Media;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Manages countdown timer functionality with titlebar display and alerts
    /// </summary>
    public class TimerManager : IDisposable
    {
        private readonly Form _parentForm;
        private Timer _countdownTimer;
        private Timer _alertTimer;
        private Timer _flashTimer;
        private DateTime _endTime;
        private string _originalTitle;
        private bool _isTimerRunning;
        private bool _isAlertPlaying;
        private Color _originalBackColor;
        private bool _flashState;
        private NativeWindow _messageFilter;
        private TimeSpan _lastTimerDuration; // Store the last timer duration for auto-repeat
        private bool _autoRepeat; // Auto-repeat setting

        /// <summary>
        /// Raised when the timer elapses (reaches zero)
        /// </summary>
        public event EventHandler TimerElapsed;

        /// <summary>
        /// Gets or sets whether the timer should automatically repeat
        /// </summary>
        public bool AutoRepeat
        {
            get => _autoRepeat;
            set => _autoRepeat = value;
        }

        public TimerManager(Form parentForm)
        {
            _parentForm = parentForm ?? throw new ArgumentNullException(nameof(parentForm));
            _originalTitle = _parentForm.Text;
            _originalBackColor = _parentForm.BackColor;
            
            // Setup form event handlers for stopping alert
            _parentForm.KeyDown += ParentForm_KeyDown;
            _parentForm.Activated += ParentForm_Activated;
            
            // Install message filter to catch all mouse clicks
            _messageFilter = new TimerMessageFilter(this);
            _messageFilter.AssignHandle(_parentForm.Handle);
        }

        /// <summary>
        /// Starts a countdown timer with the specified duration
        /// </summary>
        public void StartTimer(TimeSpan duration)
        {
            StopTimer();
            StopAlert();

            _lastTimerDuration = duration; // Store for auto-repeat
            _endTime = DateTime.Now.Add(duration);
            _isTimerRunning = true;
            _originalTitle = GetBaseTitle();

            _countdownTimer = new Timer { Interval = 1000 }; // Update every second
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();

            UpdateTitlebar();
        }

        /// <summary>
        /// Stops the countdown timer
        /// </summary>
        public void StopTimer()
        {
            if (_countdownTimer != null)
            {
                _countdownTimer.Stop();
                _countdownTimer.Dispose();
                _countdownTimer = null;
            }

            _isTimerRunning = false;
            RestoreOriginalTitle();
        }

        /// <summary>
        /// Gets the original title without timer suffix
        /// </summary>
        private string GetBaseTitle()
        {
            string currentTitle = _parentForm.Text;
            
            // Remove any existing timer suffix (format: " ⏱ HH:MM:SS" or " ⏱ MM:SS")
            int timerIndex = currentTitle.LastIndexOf(" ⏱");
            if (timerIndex > 0)
            {
                return currentTitle.Substring(0, timerIndex);
            }
            
            return currentTitle;
        }

        /// <summary>
        /// Updates the titlebar with remaining time
        /// </summary>
        private void UpdateTitlebar()
        {
            if (!_isTimerRunning)
                return;

            TimeSpan remaining = _endTime - DateTime.Now;

            if (remaining.TotalSeconds <= 0)
            {
                // Timer finished
                StopTimer();
                
                // Start alert (alarm)
                StartAlert();
                
                // Auto-repeat: restart timer immediately while alarm is playing
                if (_autoRepeat && _lastTimerDuration.TotalSeconds > 0)
                {
                    RestartTimerAfterDelay();
                }
            }
            else
            {
                string timeStr = FormatTimeSpan(remaining);
                string autoRepeatIndicator = _autoRepeat ? " 🔄" : "";
                _parentForm.Text = $"{_originalTitle} ⏱ {timeStr}{autoRepeatIndicator}";
            }
        }

        /// <summary>
        /// Restarts the timer after a brief delay (allows alert to start)
        /// </summary>
        private void RestartTimerAfterDelay()
        {
            // Restart the timer immediately (runs concurrently with the alarm)
            _endTime = DateTime.Now.Add(_lastTimerDuration);
            _isTimerRunning = true;

            _countdownTimer = new Timer { Interval = 1000 };
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();
        }

        /// <summary>
        /// Formats a TimeSpan for display
        /// </summary>
        private string FormatTimeSpan(TimeSpan time)
        {
            if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours:D2}:{time.Minutes:D2}:{time.Seconds:D2}";
            }
            else
            {
                return $"{time.Minutes:D2}:{time.Seconds:D2}";
            }
        }

        /// <summary>
        /// Handles countdown timer tick
        /// </summary>
        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            UpdateTitlebar();
        }

        /// <summary>
        /// Starts the alert (sound + flashing)
        /// </summary>
        private void StartAlert()
        {
            if (_isAlertPlaying)
                return;

            _isAlertPlaying = true;

            // Raise event to pause all media playback
            TimerElapsed?.Invoke(this, EventArgs.Empty);

            // Start flashing titlebar
            _flashTimer = new Timer { Interval = 500 }; // Flash every 500ms
            _flashTimer.Tick += FlashTimer_Tick;
            _flashTimer.Start();

            // Start alert sound timer (plays for 60 seconds)
            _alertTimer = new Timer { Interval = 60000 }; // 60 seconds
            _alertTimer.Tick += (s, e) => StopAlert();
            _alertTimer.Start();

            // Play system alert sound repeatedly
            Task.Run(() => PlayAlertSoundLoop());

            // Update title to show alert (with timer info if auto-repeat is active)
            string alertMessage = "⏰ TIME'S UP! (Click anywhere to dismiss)";
            if (_autoRepeat && _isTimerRunning)
            {
                TimeSpan remaining = _endTime - DateTime.Now;
                if (remaining.TotalSeconds > 0)
                {
                    string timeStr = FormatTimeSpan(remaining);
                    alertMessage = $"⏰ TIME'S UP! (Next: {timeStr}) (Click to dismiss)";
                }
            }
            _parentForm.Text = $"{_originalTitle} {alertMessage}";
        }

        /// <summary>
        /// Plays alert sound in a loop
        /// </summary>
        private async void PlayAlertSoundLoop()
        {
            while (_isAlertPlaying)
            {
                try
                {
                    SystemSounds.Exclamation.Play();
                    await Task.Delay(2000); // Play every 2 seconds
                }
                catch
                {
                    // Ignore errors
                }
            }
        }

        /// <summary>
        /// Stops the alert
        /// </summary>
        private void StopAlert()
        {
            if (!_isAlertPlaying)
                return;

            _isAlertPlaying = false;

            if (_flashTimer != null)
            {
                _flashTimer.Stop();
                _flashTimer.Dispose();
                _flashTimer = null;
            }

            if (_alertTimer != null)
            {
                _alertTimer.Stop();
                _alertTimer.Dispose();
                _alertTimer = null;
            }

            // Restore original form color
            if (_parentForm.InvokeRequired)
            {
                _parentForm.Invoke(new Action(() => _parentForm.BackColor = _originalBackColor));
            }
            else
            {
                _parentForm.BackColor = _originalBackColor;
            }

            // If timer is still running (auto-repeat), update the titlebar
            if (_isTimerRunning)
            {
                UpdateTitlebar();
            }
            else
            {
                RestoreOriginalTitle();
            }
        }

        /// <summary>
        /// Handles titlebar flashing
        /// </summary>
        private void FlashTimer_Tick(object sender, EventArgs e)
        {
            _flashState = !_flashState;
            
            if (_parentForm.InvokeRequired)
            {
                _parentForm.Invoke(new Action(() => 
                {
                    _parentForm.BackColor = _flashState ? Color.Red : _originalBackColor;
                }));
            }
            else
            {
                _parentForm.BackColor = _flashState ? Color.Red : _originalBackColor;
            }
        }

        /// <summary>
        /// Restores the original titlebar text
        /// </summary>
        private void RestoreOriginalTitle()
        {
            _parentForm.Text = _originalTitle;
        }

        /// <summary>
        /// Updates the original title (call this when form title changes)
        /// </summary>
        public void UpdateOriginalTitle(string newTitle)
        {
            if (!_isTimerRunning && !_isAlertPlaying)
            {
                _originalTitle = newTitle;
            }
        }

        /// <summary>
        /// Public property to check if alert is playing (for message filter)
        /// </summary>
        internal bool IsAlertPlaying => _isAlertPlaying;

        /// <summary>
        /// Handles key press to dismiss alert
        /// </summary>
        private void ParentForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (_isAlertPlaying)
            {
                StopAlert();
            }
        }

        /// <summary>
        /// Handles form activation to dismiss alert
        /// </summary>
        private void ParentForm_Activated(object sender, EventArgs e)
        {
            if (_isAlertPlaying)
            {
                StopAlert();
            }
        }

        /// <summary>
        /// Disposes resources
        /// </summary>
        public void Dispose()
        {
            StopTimer();
            StopAlert();

            if (_parentForm != null)
            {
                _parentForm.KeyDown -= ParentForm_KeyDown;
                _parentForm.Activated -= ParentForm_Activated;
            }

            if (_messageFilter != null)
            {
                _messageFilter.ReleaseHandle();
                _messageFilter = null;
            }
        }

        /// <summary>
        /// Native window message filter to catch all mouse clicks
        /// </summary>
        private class TimerMessageFilter : NativeWindow
        {
            private const int WM_LBUTTONDOWN = 0x0201;
            private const int WM_RBUTTONDOWN = 0x0204;
            private const int WM_MBUTTONDOWN = 0x0207;
            private const int WM_NCLBUTTONDOWN = 0x00A1; // Non-client area left button (titlebar, borders)
            private const int WM_NCRBUTTONDOWN = 0x00A4; // Non-client area right button

            private readonly TimerManager _timerManager;

            public TimerMessageFilter(TimerManager timerManager)
            {
                _timerManager = timerManager;
            }

            protected override void WndProc(ref Message m)
            {
                // Check for any mouse button down message
                if (_timerManager.IsAlertPlaying)
                {
                    if (m.Msg == WM_LBUTTONDOWN || 
                        m.Msg == WM_RBUTTONDOWN || 
                        m.Msg == WM_MBUTTONDOWN ||
                        m.Msg == WM_NCLBUTTONDOWN ||
                        m.Msg == WM_NCRBUTTONDOWN)
                    {
                        _timerManager.StopAlert();
                    }
                }

                base.WndProc(ref m);
            }
        }
    }
}