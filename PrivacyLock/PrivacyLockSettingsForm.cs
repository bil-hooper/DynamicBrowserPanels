using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    public partial class PrivacyLockSettingsForm : Form
    {
        private CheckBox _enabledCheckBox;
        private TextBox _pinTextBox;
        private TextBox _confirmPinTextBox;
        private TextBox _currentPinTextBox;
        private Panel _pinPanel;
        private Panel _changePinPanel;
        private Button _saveButton;
        private Button _cancelButton;
        
        public PrivacyLockSettingsForm()
        {
            InitializeComponent();
            SetupUI();
            LoadSettings();
        }
        
        private void InitializeComponent()
        {
            this.Text = "Privacy Lock Settings";
            this.Size = new Size(450, 400);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.BackColor = Color.White;
        }
        
        private void SetupUI()
        {
            var titleLabel = new Label
            {
                Text = "Privacy Lock Configuration",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(400, 30)
            };
            
            _enabledCheckBox = new CheckBox
            {
                Text = "Enable Privacy Lock",
                Location = new Point(20, 70),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 10)
            };
            _enabledCheckBox.CheckedChanged += OnEnabledChanged;
            
            // Initial PIN setup panel
            _pinPanel = new Panel
            {
                Location = new Point(20, 110),
                Size = new Size(400, 120),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            
            var pinLabel = new Label
            {
                Text = "Set PIN (4-8 digits):",
                Location = new Point(10, 15),
                Size = new Size(150, 20)
            };
            
            _pinTextBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(200, 25),
                UseSystemPasswordChar = true,
                MaxLength = 8
            };
            
            var confirmLabel = new Label
            {
                Text = "Confirm PIN:",
                Location = new Point(10, 70),
                Size = new Size(150, 20)
            };
            
            _confirmPinTextBox = new TextBox
            {
                Location = new Point(10, 95),
                Size = new Size(200, 25),
                UseSystemPasswordChar = true,
                MaxLength = 8
            };
            
            _pinPanel.Controls.AddRange(new Control[] 
            { 
                pinLabel, 
                _pinTextBox, 
                confirmLabel, 
                _confirmPinTextBox 
            });
            
            // Change PIN panel (shown when already configured)
            _changePinPanel = new Panel
            {
                Location = new Point(20, 110),
                Size = new Size(400, 160),
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false
            };
            
            var currentLabel = new Label
            {
                Text = "Current PIN:",
                Location = new Point(10, 15),
                Size = new Size(150, 20)
            };
            
            _currentPinTextBox = new TextBox
            {
                Location = new Point(10, 40),
                Size = new Size(200, 25),
                UseSystemPasswordChar = true,
                MaxLength = 8
            };
            
            var newPinLabel = new Label
            {
                Text = "New PIN (leave blank to keep):",
                Location = new Point(10, 70),
                Size = new Size(200, 20)
            };
            
            var newPinTextBox = new TextBox
            {
                Location = new Point(10, 95),
                Size = new Size(200, 25),
                UseSystemPasswordChar = true,
                MaxLength = 8,
                Name = "newPin"
            };
            
            var confirmNewLabel = new Label
            {
                Text = "Confirm New PIN:",
                Location = new Point(10, 125),
                Size = new Size(150, 20)
            };
            
            var confirmNewTextBox = new TextBox
            {
                Location = new Point(10, 150),
                Size = new Size(200, 25),
                UseSystemPasswordChar = true,
                MaxLength = 8,
                Name = "confirmNewPin"
            };
            
            _changePinPanel.Controls.AddRange(new Control[]
            {
                currentLabel,
                _currentPinTextBox,
                newPinLabel,
                newPinTextBox,
                confirmNewLabel,
                confirmNewTextBox
            });
            
            // Buttons
            _saveButton = new Button
            {
                Text = "Save",
                Location = new Point(250, 320),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            _saveButton.Click += OnSaveClick;
            
            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(340, 320),
                Size = new Size(80, 30),
                FlatStyle = FlatStyle.Flat
            };
            _cancelButton.Click += (s, e) => this.Close();
            
            this.Controls.AddRange(new Control[]
            {
                titleLabel,
                _enabledCheckBox,
                _pinPanel,
                _changePinPanel,
                _saveButton,
                _cancelButton
            });
        }
        
        private void LoadSettings()
        {
            var manager = PrivacyLockManager.Instance;
            _enabledCheckBox.Checked = manager.IsEnabled;
            
            if (manager.IsEnabled)
            {
                _changePinPanel.Visible = true;
                _pinPanel.Visible = false;
            }
        }
        
        private void OnEnabledChanged(object sender, EventArgs e)
        {
            var isEnabled = _enabledCheckBox.Checked;
            var manager = PrivacyLockManager.Instance;
            
            if (isEnabled && !manager.IsEnabled)
            {
                // First time setup
                _pinPanel.Visible = true;
                _changePinPanel.Visible = false;
            }
            else if (isEnabled && manager.IsEnabled)
            {
                // Already configured
                _pinPanel.Visible = false;
                _changePinPanel.Visible = true;
            }
            else
            {
                // Disabling
                _pinPanel.Visible = false;
                _changePinPanel.Visible = false;
            }
        }
        
        private void OnSaveClick(object sender, EventArgs e)
        {
            var manager = PrivacyLockManager.Instance;
            
            if (!_enabledCheckBox.Checked)
            {
                manager.Disable();
                MessageBox.Show("Privacy lock disabled.", "Success", 
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
                return;
            }
            
            // First time setup
            if (!manager.IsEnabled && _pinPanel.Visible)
            {
                if (_pinTextBox.Text != _confirmPinTextBox.Text)
                {
                    MessageBox.Show("PINs do not match!", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                try
                {
                    manager.Initialize(_pinTextBox.Text);
                    MessageBox.Show("Privacy lock enabled successfully!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                
                return;
            }
            
            // Changing PIN
            if (manager.IsEnabled && _changePinPanel.Visible)
            {
                var newPinTextBox = _changePinPanel.Controls["newPin"] as TextBox;
                var confirmNewTextBox = _changePinPanel.Controls["confirmNewPin"] as TextBox;
                
                // If no new PIN, just verify current and close
                if (string.IsNullOrWhiteSpace(newPinTextBox.Text))
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                    return;
                }
                
                if (newPinTextBox.Text != confirmNewTextBox.Text)
                {
                    MessageBox.Show("New PINs do not match!", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                
                if (manager.ChangePin(_currentPinTextBox.Text, newPinTextBox.Text))
                {
                    MessageBox.Show("PIN changed successfully!", "Success", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Current PIN is incorrect or new PIN is invalid!", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}