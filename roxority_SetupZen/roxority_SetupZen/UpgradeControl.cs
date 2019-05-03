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
/* KML: Updated to use Operation now owned by InstallerForm           */
/*                                                                    */
/**********************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace roxority_SetupZen {
	public partial class UpgradeControl : InstallerControl {

		public readonly bool AllowUpgrade;
		private readonly InstallProcessControl processControl;

		public UpgradeControl (bool allowUpgrade) {
			AllowUpgrade = allowUpgrade;
			this.processControl = Program.CreateProcessControl ();
			InitializeComponent ();

			messageLabel.Text = InstallConfiguration.FormatString (messageLabel.Text);

			string upgradeDescription = InstallConfiguration.UpgradeDescription;
			if (!string.IsNullOrEmpty (upgradeDescription)) {
				upgradeDescriptionLabel.Text = upgradeDescription;
			}
		}

		protected internal override void Open (InstallOptions options) {
			if (!AllowUpgrade) {
				upgradeRadioButton.Checked = upgradeRadioButton.Enabled = false;
				upgradeDescriptionLabel.Text = Properties.Resources.NoUpgrade;
			}
			bool enable = upgradeRadioButton.Checked || removeRadioButton.Checked;
			Form.Operation = InstallOperation.Upgrade;
			Form.NextButton.Enabled = enable;
		}

		protected internal override void Close (InstallOptions options) {
			Form.ContentControls.Add (processControl);
		}

		private void upgradeRadioButton_CheckedChanged (object sender, EventArgs e) {
			if (upgradeRadioButton.Checked) {
				Form.Operation = InstallOperation.Upgrade;
				Form.NextButton.Enabled = true;
			}
		}

		private void removeRadioButton_CheckedChanged (object sender, EventArgs e) {
			if (removeHintLinkLabel.Visible = removeRadioButton.Checked) {
				Form.Operation = InstallOperation.Uninstall;
				Form.NextButton.Enabled = true;
			}
		}

		private void removeHintLinkLabel_LinkClicked (object sender, LinkLabelLinkClickedEventArgs e) {
			string url = string.Empty;
			try {
				Process.Start (url = InstallConfiguration.GetConfigLink ("wss"));
			} catch {
				if (!string.IsNullOrEmpty (url))
					MessageBox.Show ("Open: " + url);
			}
		}

	}

}
