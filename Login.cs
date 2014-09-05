using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WrekRip {
    public partial class Login : Form {
        public Login() {
            InitializeComponent();
        }

        private void btnOK_Click(object sender, EventArgs e) {
            AlbumInfo.UserID = txtUserID.Text;
            AlbumInfo.Password = txtPassword.Text;
        }
    }
}
