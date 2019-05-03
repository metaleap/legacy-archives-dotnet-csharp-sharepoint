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
/* KML: Minor fix to configuration property name                      */
/* KML: Added capabality around DefaultDeployToSRP                    */
/*                                                                    */
/**********************************************************************/
using Microsoft.SharePoint.Administration;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

namespace roxority_SetupZen {

	using res = Properties.Resources;

	public partial class DeploymentTargetsControl : InstallerControl {

		public DeploymentTargetsControl () {
			InitializeComponent ();

			webApplicationsCheckedListBox.ItemCheck += new ItemCheckEventHandler (webApplicationsCheckedListBox_ItemCheck);

			this.Load += new EventHandler (Control_Load);
		}

		private void Control_Load (object sender, EventArgs e) {
			ConfigureWebApplicationList ();
		}

		private void webApplicationsCheckedListBox_ItemCheck (object sender, ItemCheckEventArgs e) {
			WebApplicationInfo info = (WebApplicationInfo) webApplicationsCheckedListBox.Items [e.Index];
			if (info.Required)
				e.NewValue = CheckState.Indeterminate;
		}

		protected internal override void Close (InstallOptions options) {
			WebApplicationInfo appInfo;
			if (InstallConfiguration.RequireDeploymentToCentralAdminWebApplication)
				for (int i = 0; i < webApplicationsCheckedListBox.Items.Count; i++)
					if (((appInfo = webApplicationsCheckedListBox.Items [i] as WebApplicationInfo) != null) && (appInfo.IsSRP || ((appInfo.Application != null) && appInfo.Application.IsAdministrationWebApplication)))
						webApplicationsCheckedListBox.SetItemChecked (i, true);
			foreach (WebApplicationInfo info in webApplicationsCheckedListBox.CheckedItems)
				options.Targets.Add (info.Application);
		}

		private void webApplicationsCheckedListBox_SelectedIndexChanged (object sender, EventArgs e) {
			Form.NextButton.Enabled = (webApplicationsCheckedListBox.CheckedItems.Count > 0);
		}

		private void ConfigureWebApplicationList () {
			bool defaultDeployToSRP = InstallConfiguration.DefaultDeployToSRP;
			bool required = InstallConfiguration.RequireDeploymentToAllContentWebApplications;
			CheckState defaultCheckState = defaultDeployToSRP ? CheckState.Unchecked : CheckState.Checked;
			Collection<SPWebApplication> applications = new Collection<SPWebApplication> ();
			foreach (SPWebApplication application in SPWebService.ContentService.WebApplications) {
				WebApplicationInfo webAppInfo = new WebApplicationInfo (application, required);
				if (required)
					webApplicationsCheckedListBox.Items.Add (webAppInfo, CheckState.Indeterminate);
				else
					webApplicationsCheckedListBox.Items.Add (webAppInfo, InstallConfiguration.SuggestDeploymentToAllContentWebApplications ? CheckState.Checked : ((defaultDeployToSRP && !webAppInfo.IsSRP) ? CheckState.Unchecked : CheckState.Checked));
			}
			required = InstallConfiguration.RequireDeploymentToCentralAdminWebApplication;
			foreach (SPWebApplication application in SPWebService.AdministrationService.WebApplications) {
				WebApplicationInfo webAppInfo = new WebApplicationInfo (application, required);
				if (required)
					webApplicationsCheckedListBox.Items.Add (webAppInfo, CheckState.Indeterminate);
				else
					webApplicationsCheckedListBox.Items.Add (webAppInfo, (InstallConfiguration.SuggestDeploymentToCentralAdminWebApplication && webAppInfo.Application.IsAdministrationWebApplication) ? CheckState.Checked : (webAppInfo.Application.IsAdministrationWebApplication ? CheckState.Unchecked : defaultCheckState));
			}
		}

		private class WebApplicationInfo {

			private readonly SPWebApplication application;
			private readonly bool required;

			internal WebApplicationInfo (SPWebApplication application, bool required) {
				this.application = application;
				this.required = required;
			}

			internal SPWebApplication Application {
				get {
					return application;
				}
			}

			public bool Required {
				get {
					return required;
				}
			}

			public bool IsSRP {
				get {
					return application.Properties.ContainsKey ("Microsoft.Office.Server.SharedResourceProvider");
				}
			}

			public override string ToString () {
				string str = application.GetResponseUri (SPUrlZone.Default).ToString ();

				if (application.IsAdministrationWebApplication) {
					str += ("     " + res.CentralAdmin);
				} else if (IsSRP) {
					str += ("     " + res.SharedResourceProvider);
				} else if (!String.IsNullOrEmpty (application.DisplayName)) {
					str += "     (" + application.DisplayName + ")";
				} else if (!String.IsNullOrEmpty (application.Name)) {
					str += "     (" + application.Name + ")";
				}

				return str;
			}

		}

	}

}
