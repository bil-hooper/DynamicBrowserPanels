using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Semi-transparent loading overlay with animated spinner
    /// </summary>
    public class LoadingOverlay : Panel
    {
        private Timer _animationTimer;
        private int _rotationAngle = 0;
        private Label _statusLabel;
        private const int SpinnerSize = 48;

        public LoadingOverlay()
        {
            // Semi-transparent background
            BackColor = Color.FromArgb(200, 30, 30, 30);
            Dock = DockStyle.Fill;

            // Status label
            _statusLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(135, 206, 250), // Light Sky Blue
                Font = new Font("Segoe UI", 12F, FontStyle.Regular),
                Text = "Loading...",
                Dock = DockStyle.Bottom,
                Height = 50
            };
            Controls.Add(_statusLabel);

            // Animation timer (60 FPS)
            _animationTimer = new Timer { Interval = 16 };
            _animationTimer.Tick += (s, e) =>
            {
                _rotationAngle = (_rotationAngle + 8) % 360;
                Invalidate();
            };

            // Double buffering for smooth animation
            DoubleBuffered = true;
        }

        /// <summary>
        /// Updates the status message
        /// </summary>
        public void SetStatus(string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => _statusLabel.Text = message));
            }
            else
            {
                _statusLabel.Text = message;
            }
        }

        /// <summary>
        /// Shows the loading overlay
        /// </summary>
        public void Show(Control parent)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => Show(parent)));
                return;
            }

            if (!parent.Controls.Contains(this))
            {
                parent.Controls.Add(this);
            }

            BringToFront();
            Visible = true;
            _animationTimer.Start();
        }

        /// <summary>
        /// Hides the loading overlay
        /// </summary>
        public new void Hide()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(Hide));
                return;
            }

            _animationTimer.Stop();
            Visible = false;
        }

        /// <summary>
        /// Draws the spinning loading indicator
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Calculate center position
            int centerX = Width / 2;
            int centerY = (Height - _statusLabel.Height) / 2;

            // Draw rotating arc (modern spinner) - using purple theme
            using (var pen = new Pen(Color.FromArgb(255, 147, 112, 219), 4)) // Medium Purple
            {
                pen.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                pen.EndCap = System.Drawing.Drawing2D.LineCap.Round;

                var rect = new Rectangle(
                    centerX - SpinnerSize / 2,
                    centerY - SpinnerSize / 2,
                    SpinnerSize,
                    SpinnerSize
                );

                e.Graphics.DrawArc(pen, rect, _rotationAngle, 270);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer?.Dispose();
                _statusLabel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}