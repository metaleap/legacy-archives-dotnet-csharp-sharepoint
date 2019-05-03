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
/* KML: Minor fix to configuration property access                    */
/*                                                                    */
/**********************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace roxority_SetupZen {

	public partial class EULAControl : InstallerControl {

		public EULAControl () {
			InitializeComponent ();
		}

		private void acceptCheckBox_CheckedChanged (object sender, EventArgs e) {
			Form.NextButton.Enabled = acceptCheckBox.Checked;
		}

		private void languageComboBox_SelectedIndexChanged (object sender, EventArgs e) {
			string filename = InstallConfiguration.GetEula ((languageComboBox.SelectedIndex == 1) ? "de_" : string.Empty);
			if (!String.IsNullOrEmpty (filename)) {
				try {
					this.richTextBox.LoadFile (filename);
					acceptCheckBox.Enabled = true;
				} catch (IOException ex) {
					this.richTextBox.Lines = new string [] { filename + ":", ex.Message };
				}
			}
			richTextBox.SelectionLength = richTextBox.SelectionStart = 0;
			richTextBox.ScrollToCaret ();
		}

		private void richTextBox_LinkClicked (object sender, LinkClickedEventArgs e) {
			try {
				System.Diagnostics.Process.Start (e.LinkText);
			} catch (Exception ex) {
				MessageBox.Show (e.LinkText + ":\r\n\r\n" + ex.Message);
			}
		}

		protected override void OnLoad (EventArgs e) {
			base.OnLoad (e);
			if (CultureInfo.CurrentCulture.Name.StartsWith ("de") || CultureInfo.CurrentUICulture.Name.StartsWith ("de"))
				languageComboBox.SelectedIndex = 1;
			else
				languageComboBox.SelectedIndex = 0;
			languageComboBox_SelectedIndexChanged (languageComboBox, EventArgs.Empty);
		}

		protected internal override void Open (InstallOptions options) {
			Form.NextButton.Enabled = acceptCheckBox.Checked;
		}

	}

}
