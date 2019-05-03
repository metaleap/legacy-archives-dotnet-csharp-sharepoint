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
/*  KML: added conditional hook to FinishedControl                    */
/*                                                                    */
/**********************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace roxority_SetupZen {

	using res = Properties.Resources;

	public partial class CompletionControl : InstallerControl {

		public CompletionControl () {
			InitializeComponent ();

			this.Load += new EventHandler (CompletionControl_Load);
		}

		void CompletionControl_Load (object sender, EventArgs e) {
			// Conditionally show the FinishedControl
			if (InstallConfiguration.ShowFinishedControl && Form.Operation == InstallOperation.Install) {
				FinishedControl finishedControl = new FinishedControl ();
				finishedControl.Title = res.Finished;
				finishedControl.SubTitle = InstallConfiguration.FormatString (res.Finished2);
				Form.ContentControls.Add (finishedControl);

				Form.NextButton.Enabled = true;
			} else
				Form.AbortButton.Enabled = true;
		}

		public string Details {
			get {
				return detailsTextBox.Text;
			}
			set {
				detailsTextBox.Text = value;
			}
		}

		protected internal override void Open (InstallOptions options) {
			Form.PrevButton.Enabled = false;
			if (InstallConfiguration.ShowFinishedControl && Form.Operation == InstallOperation.Install) {
				Form.AbortButton.Enabled = false;
			} else
				Form.AbortButton.Enabled = true;
			if (string.IsNullOrEmpty (Details) && Form.NextButton.Enabled)
				Form.NextButton.PerformClick ();
		}

	}

}
