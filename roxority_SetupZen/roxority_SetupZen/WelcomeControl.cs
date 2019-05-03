/**********************************************************************/
/*                                                                    */
/*                   SharePoint Solution Installer                    */
/*             http://www.codeplex.com/sharepointinstaller            */
/*                                                                    */
/*               (c) Copyright 2007 Lars Fastrup Nielsen.             */
/*                                                                    */
/*  This source is subject to the Microsoft Permissive License.       */
/*  http://www.codeplex.com/sharepointinstaller/Project/License.aspx  */
/*                                                                    */
/**********************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace roxority_SetupZen {

	public partial class WelcomeControl : InstallerControl {

		private bool init = false;

		public WelcomeControl () {
			InitializeComponent ();
			if (CultureInfo.CurrentUICulture.Name.StartsWith ("de", StringComparison.InvariantCultureIgnoreCase))
				langGermanRadioButton.Checked = true;
			if (CultureInfo.CurrentUICulture.Name.StartsWith ("fr", StringComparison.InvariantCultureIgnoreCase))
				langFrenchRadioButton.Checked = true;
			init = true;
			messageLabel.Text = InstallConfiguration.FormatString (messageLabel.Text);
			label2.Text = InstallConfiguration.FormatString (label2.Text);
		}

		private void langRadioButton_CheckedChanged (object sender, EventArgs e) {
			if (init && ((RadioButton) sender).Checked) {
				init = false;
				langEnglishRadioButton.Enabled = langGermanRadioButton.Enabled = false;
				try {
					Process.Start (Application.ExecutablePath, ((RadioButton) sender).Tag as string);
					Application.Exit ();
				} catch (Exception ex) {
					MessageBox.Show (ex.Message, ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

	}

}
