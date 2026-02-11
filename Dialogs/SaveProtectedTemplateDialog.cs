using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Dialog for saving a password-protected template
    /// </summary>
    public class SaveProtectedTemplateDialog : Form
    {
        private TextBox txtFileName;
        private TextBox txtPassword;
        private TextBox txtConfirmPassword;
        private Button btnOk;
        private Button btnCancel;
        
        public string FileName { get; private set; }
        public string Password { get; private set; }
        
        public SaveProtectedTemplateDialog(string currentFileName = null)
        {
            InitializeUI(currentFileName);
        }
        
        private void InitializeUI(string currentFileName)
        {
            this.Text = "Save Password-Protected Template";
            this.Size = new Size(450, 250);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            var lblFileName = new Label
            {
                Text = "Template filename:",
                Location = new Point(20, 20),
                Size = new Size(120, 20)
            };
            
            txtFileName = new TextBox
            {
                Location = new Point(140, 18),
                Size = new Size(270, 25)
            };
            
            // Prepopulate filename if provided
            if (!string.IsNullOrEmpty(currentFileName))
            {
                txtFileName.Text = Path.GetFileNameWithoutExtension(currentFileName);
            }
            
            var lblPassword = new Label
            {
                Text = "Password:",
                Location = new Point(20, 60),
                Size = new Size(120, 20)
            };
            
            txtPassword = new TextBox
            {
                Location = new Point(140, 58),
                Size = new Size(270, 25),
                UseSystemPasswordChar = true
            };
            
            var lblConfirmPassword = new Label
            {
                Text = "Confirm Password:",
                Location = new Point(20, 100),
                Size = new Size(120, 20)
            };
            
            txtConfirmPassword = new TextBox
            {
                Location = new Point(140, 98),
                Size = new Size(270, 25),
                UseSystemPasswordChar = true
            };
            
            var lblNote = new Label
            {
                Text = "Note: URLs will be encrypted. This template can be shared\nbetween machines with the password.",
                Location = new Point(20, 135),
                Size = new Size(390, 40),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            
            btnOk = new Button
            {
                Text = "Save",
                Location = new Point(250, 180),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            btnOk.Click += BtnOk_Click;
            
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(335, 180),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };
            
            this.Controls.AddRange(new Control[]
            {
                lblFileName, txtFileName,
                lblPassword, txtPassword,
                lblConfirmPassword, txtConfirmPassword,
                lblNote,
                btnOk, btnCancel
            });
            
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
        
        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Validate filename
            if (string.IsNullOrWhiteSpace(txtFileName.Text))
            {
                MessageBox.Show(
                    "Please enter a filename.",
                    "Filename Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtFileName.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }
            
            // Validate password
            if (string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                MessageBox.Show(
                    "Please enter a password.",
                    "Password Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtPassword.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }
            
            if (txtPassword.Text.Length < 4)
            {
                MessageBox.Show(
                    "Password must be at least 4 characters.",
                    "Password Too Short",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtPassword.Focus();
                this.DialogResult = DialogResult.None;
                return;
            }
            
            // Validate password confirmation
            if (txtPassword.Text != txtConfirmPassword.Text)
            {
                MessageBox.Show(
                    "Passwords do not match.",
                    "Password Mismatch",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                txtConfirmPassword.Focus();
                txtConfirmPassword.SelectAll();
                this.DialogResult = DialogResult.None;
                return;
            }
            
            FileName = txtFileName.Text.Trim();
            Password = txtPassword.Text;
            
            // Ensure .frm extension
            if (!FileName.EndsWith(".frm", StringComparison.OrdinalIgnoreCase))
            {
                FileName += ".frm";
            }
        }
    }
}