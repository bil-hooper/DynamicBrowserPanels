using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Dialog for entering custom timer duration
    /// </summary>
    public class TimerInputDialog : Form
    {
        private NumericUpDown numHours;
        private NumericUpDown numMinutes;
        private NumericUpDown numSeconds;
        private Button btnOk;
        private Button btnCancel;
        private Label lblHours;
        private Label lblMinutes;
        private Label lblSeconds;
        private Label lblTimeInfo;
        private Timer updateTimer;

        public TimeSpan TimerDuration { get; private set; }

        public TimerInputDialog()
        {
            InitializeComponent();
            LoadLastCustomTimer();
            SetupUpdateTimer();
            UpdateTimeInfo();
        }

        private void InitializeComponent()
        {
            this.Text = "Set Custom Timer";
            this.Size = new Size(360, 260);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            const int leftMargin = 20;
            const int labelWidth = 70;
            const int numericWidth = 80;
            const int controlHeight = 23;
            const int verticalSpacing = 35;
            const int labelNumericGap = 10;
            int currentY = 20;

            // Hours
            lblHours = new Label
            {
                Text = "Hours:",
                Location = new Point(leftMargin, currentY + 3),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            numHours = new NumericUpDown
            {
                Location = new Point(leftMargin + labelWidth + labelNumericGap, currentY),
                Size = new Size(numericWidth, controlHeight),
                Minimum = 0,
                Maximum = 23,
                Value = 0
            };
            numHours.ValueChanged += NumericUpDown_ValueChanged;

            currentY += verticalSpacing;

            // Minutes
            lblMinutes = new Label
            {
                Text = "Minutes:",
                Location = new Point(leftMargin, currentY + 3),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            numMinutes = new NumericUpDown
            {
                Location = new Point(leftMargin + labelWidth + labelNumericGap, currentY),
                Size = new Size(numericWidth, controlHeight),
                Minimum = 0,
                Maximum = 59,
                Value = 5
            };
            numMinutes.ValueChanged += NumericUpDown_ValueChanged;

            currentY += verticalSpacing;

            // Seconds
            lblSeconds = new Label
            {
                Text = "Seconds:",
                Location = new Point(leftMargin, currentY + 3),
                Size = new Size(labelWidth, 20),
                TextAlign = ContentAlignment.MiddleLeft
            };

            numSeconds = new NumericUpDown
            {
                Location = new Point(leftMargin + labelWidth + labelNumericGap, currentY),
                Size = new Size(numericWidth, controlHeight),
                Minimum = 0,
                Maximum = 59,
                Value = 0
            };
            numSeconds.ValueChanged += NumericUpDown_ValueChanged;

            currentY += verticalSpacing + 10;

            // Time info label
            lblTimeInfo = new Label
            {
                Location = new Point(leftMargin, currentY),
                Size = new Size(this.ClientSize.Width - (leftMargin * 2), 40),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(this.Font.FontFamily, 9, FontStyle.Regular),
                ForeColor = Color.FromArgb(64, 64, 64)
            };

            currentY += 50;

            // Buttons
            const int buttonWidth = 90;
            const int buttonHeight = 32;
            const int buttonSpacing = 10;
            int buttonY = currentY;
            int totalButtonWidth = (buttonWidth * 2) + buttonSpacing;
            int buttonStartX = (this.ClientSize.Width - totalButtonWidth) / 2;

            btnOk = new Button
            {
                Text = "OK",
                Location = new Point(buttonStartX, buttonY),
                Size = new Size(buttonWidth, buttonHeight),
                DialogResult = DialogResult.OK
            };
            btnOk.Click += BtnOk_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(buttonStartX + buttonWidth + buttonSpacing, buttonY),
                Size = new Size(buttonWidth, buttonHeight),
                DialogResult = DialogResult.Cancel
            };

            // Add controls
            Controls.AddRange(new Control[]
            {
                lblHours, numHours,
                lblMinutes, numMinutes,
                lblSeconds, numSeconds,
                lblTimeInfo,
                btnOk, btnCancel
            });

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        /// <summary>
        /// Sets up the timer to update time info every second
        /// </summary>
        private void SetupUpdateTimer()
        {
            updateTimer = new Timer
            {
                Interval = 1000 // Update every second
            };
            updateTimer.Tick += (s, e) => UpdateTimeInfo();
            updateTimer.Start();
        }

        /// <summary>
        /// Handles value changes in numeric up/down controls
        /// </summary>
        private void NumericUpDown_ValueChanged(object sender, EventArgs e)
        {
            UpdateTimeInfo();
        }

        /// <summary>
        /// Updates the time information label
        /// </summary>
        private void UpdateTimeInfo()
        {
            DateTime now = DateTime.Now;
            int hours = (int)numHours.Value;
            int minutes = (int)numMinutes.Value;
            int seconds = (int)numSeconds.Value;
            
            TimeSpan duration = new TimeSpan(hours, minutes, seconds);
            DateTime endTime = now.Add(duration);

            string currentTimeStr = now.ToString("h:mm:ss tt");
            string endTimeStr = endTime.ToString("h:mm:ss tt");

            lblTimeInfo.Text = $"Current: {currentTimeStr}\nTimer End: {endTimeStr}";
        }

        /// <summary>
        /// Loads the last custom timer value from configuration
        /// </summary>
        private void LoadLastCustomTimer()
        {
            try
            {
                TimeSpan lastDuration = AppConfiguration.LastCustomTimerDuration;
                
                numHours.Value = lastDuration.Hours;
                numMinutes.Value = lastDuration.Minutes;
                numSeconds.Value = lastDuration.Seconds;
            }
            catch
            {
                // If loading fails, keep default values
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            int hours = (int)numHours.Value;
            int minutes = (int)numMinutes.Value;
            int seconds = (int)numSeconds.Value;

            if (hours == 0 && minutes == 0 && seconds == 0)
            {
                MessageBox.Show(
                    "Timer duration must be greater than 0.",
                    "Invalid Duration",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                DialogResult = DialogResult.None;
                return;
            }

            TimerDuration = new TimeSpan(hours, minutes, seconds);
            
            // Save this duration for future use
            AppConfiguration.LastCustomTimerDuration = TimerDuration;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (updateTimer != null)
                {
                    updateTimer.Stop();
                    updateTimer.Dispose();
                    updateTimer = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}