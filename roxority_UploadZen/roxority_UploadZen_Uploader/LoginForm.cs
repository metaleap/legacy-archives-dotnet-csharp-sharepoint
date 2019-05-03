
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace roxUp {

	public partial class LoginForm : Form {

		public LoginForm (bool allowSavePassword, bool wasStored) {
			InitializeComponent ();
			checkBox.Checked = wasStored;
			checkBox.Enabled = allowSavePassword;
		}

	}

}
