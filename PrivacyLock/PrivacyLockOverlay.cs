using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    public partial class PrivacyLockOverlay : Form
    {
        private TextBox _pinTextBox;
        private Button _unlockButton;
        private Label _statusLabel;
        private Label _titleLabel;
        private int _failedAttempts;
        
        public PrivacyLockOverlay()
        {
            InitializeComponent();
            SetupUI();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Form settings
            this.AutoScaleDimensions = new SizeF(8F, 16F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.BackColor = Color.FromArgb(30, 30, 30);
            this.ClientSize = new Size(400, 250);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Name = "PrivacyLockOverlay";
            this.Opacity = 0.98;
            
            this.ResumeLayout(false);
        }
        
        private void SetupUI()
        {
            var panel = new Panel
            {
                BackColor = Color.White,
                Size = new Size(350, 200),
                Location = new Point(25, 25)
            };
            
            _titleLabel = new Label
            {
                Text = "🔒 Privacy Lock",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(310, 40),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(30, 30, 30)
            };
            
            var instructionLabel = new Label
            {
                Text = "Enter your PIN to unlock",
                Location = new Point(20, 70),
                Size = new Size(310, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(60, 60, 60)
            };
            
            _pinTextBox = new TextBox
            {
                Location = new Point(75, 100),
                Size = new Size(200, 30),
                Font = new Font("Segoe UI", 12),
                UseSystemPasswordChar = true,
                MaxLength = 8,
                TextAlign = HorizontalAlignment.Center
            };
            _pinTextBox.KeyPress += OnPinKeyPress;
            
            _unlockButton = new Button
            {
                Text = "Unlock",
                Location = new Point(125, 140),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _unlockButton.FlatAppearance.BorderSize = 0;
            _unlockButton.Click += OnUnlockClick;
            
            _statusLabel = new Label
            {
                Text = "",
                Location = new Point(20, 180),
                Size = new Size(310, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(200, 50, 50),
                Font = new Font("Segoe UI", 9)
            };
            
            panel.Controls.AddRange(new Control[] 
            { 
                _titleLabel, 
                instructionLabel, 
                _pinTextBox, 
                _unlockButton, 
                _statusLabel 
            });
            
            this.Controls.Add(panel);
        }
        
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _pinTextBox.Focus();
        }
        
        private void OnPinKeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true;
                AttemptUnlock();
            }
        }
        
        private void OnUnlockClick(object sender, EventArgs e)
        {
            AttemptUnlock();
        }
        
        private void AttemptUnlock()
        {
            var pin = _pinTextBox.Text;
            
            if (string.IsNullOrWhiteSpace(pin))
            {
                _statusLabel.Text = "Please enter your PIN";
                return;
            }
            
            if (PrivacyLockManager.Instance.Unlock(pin))
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                _failedAttempts++;
                _statusLabel.Text = $"Incorrect PIN (Attempt {_failedAttempts})";
                _pinTextBox.Clear();
                _pinTextBox.Focus();
                
                // Optional: Add delay after multiple failures
                if (_failedAttempts >= 3)
                {
                    _statusLabel.Text = "Too many failed attempts";
                }
            }
        }
        
        protected override bool ProcessDialogKey(Keys keyData)
        {
            // Prevent closing with Escape or Alt+F4
            if (keyData == Keys.Escape || keyData == (Keys.Alt | Keys.F4))
                return true;
                
            return base.ProcessDialogKey(keyData);
        }
        
        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ClassStyle |= 0x20000; // CS_DROPSHADOW
                return cp;
            }
        }
    }
}