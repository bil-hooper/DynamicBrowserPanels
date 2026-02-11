using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Simple PIN dialog for locking/unlocking individual tabs
    /// </summary>
    public class TabPinDialog : Form
    {
        private TextBox _pinTextBox;
        private Button _okButton;
        private Button _cancelButton;
        private Label _messageLabel;
        private readonly bool _isUnlocking;

        public string EnteredPin { get; private set; }

        public TabPinDialog(bool isUnlocking = false)
        {
            _isUnlocking = isUnlocking;
            InitializeUI();
        }

        private void InitializeUI()
        {
            this.Text = _isUnlocking ? "Unlock Tab" : "Lock Tab";
            this.Size = new Size(350, 180);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            _messageLabel = new Label
            {
                Text = _isUnlocking ? "Enter PIN to unlock this tab:" : "Enter PIN to lock this tab:",
                Location = new Point(20, 20),
                Size = new Size(300, 20),
                Font = new Font("Segoe UI", 9)
            };

            _pinTextBox = new TextBox
            {
                Location = new Point(20, 50),
                Size = new Size(290, 25),
                Font = new Font("Segoe UI", 10),
                UseSystemPasswordChar = true,
                MaxLength = 8
            };
            _pinTextBox.KeyPress += OnPinKeyPress;

            _okButton = new Button
            {
                Text = "OK",
                Location = new Point(150, 95),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            _okButton.Click += OnOkClick;

            _cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(235, 95),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };

            this.Controls.AddRange(new Control[] 
            { 
                _messageLabel, 
                _pinTextBox, 
                _okButton, 
                _cancelButton 
            });

            this.AcceptButton = _okButton;
            this.CancelButton = _cancelButton;
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
                OnOkClick(sender, e);
            }
        }

        private void OnOkClick(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_pinTextBox.Text))
            {
                MessageBox.Show(
                    "Please enter a PIN.",
                    "PIN Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            EnteredPin = _pinTextBox.Text;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}