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
	public partial class RepairControl : InstallerControl {

		public readonly bool AllowRepair;
		private readonly InstallProcessControl processControl;

		public RepairControl (bool allowRepair) {
			AllowRepair = allowRepair;
			this.processControl = Program.CreateProcessControl ();
			InitializeComponent ();

			messageLabel.Text = InstallConfiguration.FormatString (messageLabel.Text);
		}

		protected internal override void Open (InstallOptions options) {
			if (!AllowRepair) {
				repairRadioButton.Checked = repairRadioButton.Enabled = false;
				repairDescriptionLabel.Text = Properties.Resources.NoUpgrade;
			}
			bool enable = repairRadioButton.Checked || removeRadioButton.Checked;
			Form.Operation = InstallOperation.Repair;
			Form.NextButton.Enabled = enable;
		}

		protected internal override void Close (InstallOptions options) {
			Form.ContentControls.Add (processControl);
		}

		private void repairRadioButton_CheckedChanged (object sender, EventArgs e) {
			if (repairRadioButton.Checked) {
				Form.Operation = InstallOperation.Repair;
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
