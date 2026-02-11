using System;
using System.Drawing;
using System.Windows.Forms;

namespace DynamicBrowserPanels
{
    /// <summary>
    /// Dialog for verifying password for protected templates
    /// </summary>
    public class PasswordVerificationDialog : Form
    {
        private TextBox txtPassword;
        private Button btnOk;
        private Button btnCancel;
        
        public string Password { get; private set; }
        
        public PasswordVerificationDialog(string title = "Enter Password")
        {
            InitializeUI(title);
        }
        
        private void InitializeUI(string title)
        {
            this.Text = title;
            this.Size = new Size(400, 160);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            var lblPassword = new Label
            {
                Text = "Password:",
                Location = new Point(20, 25),
                Size = new Size(80, 20)
            };
            
            txtPassword = new TextBox
            {
                Location = new Point(100, 23),
                Size = new Size(260, 25),
                UseSystemPasswordChar = true
            };
            
            btnOk = new Button
            {
                Text = "OK",
                Location = new Point(200, 75),
                Size = new Size(75, 30),
                DialogResult = DialogResult.OK
            };
            btnOk.Click += BtnOk_Click;
            
            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(285, 75),
                Size = new Size(75, 30),
                DialogResult = DialogResult.Cancel
            };
            
            this.Controls.AddRange(new Control[]
            {
                lblPassword, txtPassword,
                btnOk, btnCancel
            });
            
            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;
        }
        
        private void BtnOk_Click(object sender, EventArgs e)
        {
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
            
            Password = txtPassword.Text;
        }
        
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            txtPassword.Focus();
        }
    }
}