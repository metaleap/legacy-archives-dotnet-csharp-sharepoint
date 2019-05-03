using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

namespace roxority_SetupZen {

	using res = Properties.Resources;

	public partial class FinishedControl : roxority_SetupZen.InstallerControl {
		#region Constants

		public const int DefaultLinkHeight = 28;

		#endregion

		#region Member Variables

		private bool initialized = false;

		#endregion

		#region Constructor

		public FinishedControl () {
			InitializeComponent ();
		}

		#endregion

		#region Overrides

		protected internal override void Open (InstallOptions options) {
			if (!initialized) {
				initialized = true;
				if (Form.Operation == InstallOperation.Install) {
					AddLinks (options);
				}
			}
			Form.AbortButton.Enabled = true;
		}

		#endregion

		#region Private Methods

		private void AddLinks (InstallOptions options) {
			// Show a documentation Url if one is configured
			if (!String.IsNullOrEmpty (InstallConfiguration.DocumentationUrl)) {
				string linkText = InstallConfiguration.FormatString (res.ProductDocumentation);
				AddLink (linkText, 0, linkText.Length, InstallConfiguration.DocumentationUrl);
			}
			// Add the for each target
			if (((InstallConfiguration.FeatureScope == SPFeatureScope.Site) || InstallConfiguration.RequireDeploymentToAllContentWebApplications) &&
			  !String.IsNullOrEmpty (InstallConfiguration.SiteCollectionRelativeConfigLink)) {
				// Add site collection links
				AddSiteCollectionLinks (options.SiteCollectionTargets, FormatRelativeLink (InstallConfiguration.SiteCollectionRelativeConfigLink));
			} else if (((InstallConfiguration.FeatureScope == SPFeatureScope.Farm) || InstallConfiguration.RequireDeploymentToCentralAdminWebApplication) &&
			  !String.IsNullOrEmpty (InstallConfiguration.SSPRelativeConfigLink)) {
				// Add Shared Service Provider links
				// Note that thes are really Shared Resource Provider links - we just wish we knew how to only show links for a SSP and not SRPs
				AddSspLinks (options.Targets, FormatRelativeLink (InstallConfiguration.SSPRelativeConfigLink));
			}
		}

		private string FormatRelativeLink (string relativeLink) {
			if (!relativeLink.StartsWith ("/"))
				relativeLink = "/" + relativeLink;
			return relativeLink;
		}

		private void AddLink (string linkText, int linkStart, int linkLength, string url) {
			LinkLabel linkLabel = new LinkLabel ();
			linkLabel.Text = linkText;
			linkLabel.LinkArea = new LinkArea (linkStart, linkLength);
			linkLabel.Links [0].LinkData = url;
			linkLabel.LinkClicked += new LinkLabelLinkClickedEventHandler (linkLabel_LinkClicked);
			linkLabel.Width = tableLayoutPanel.Width - 10;
			linkLabel.Height = DefaultLinkHeight;
			tableLayoutPanel.Controls.Add (linkLabel);
		}

		private void AddSspLinks (IList webApplicationTargets, string relativeLink) {
			SPWebApplication webApp;
			string linkText;
			foreach (object webAppObj in webApplicationTargets)
				if ((webApp = webAppObj as SPWebApplication) != null)
					if (webApp.Sites.Count > 0)
						foreach (SPSite site in webApp.Sites)
							try {
								AddLink (linkText = InstallConfiguration.FormatString (res.ConfigButton, site.Url, InstallConfiguration.SolutionShortName), 0, linkText.Length, site.Url + relativeLink);
								break;
							} catch {
							} else if (webApp.AlternateUrls.Count > 0)
						foreach (SPAlternateUrl url in webApp.AlternateUrls)
							if (!string.IsNullOrEmpty (url.IncomingUrl))
								AddLink (linkText = InstallConfiguration.FormatString (res.ConfigButton, url, InstallConfiguration.SolutionShortName), 0, linkText.Length, url + relativeLink);
		}

		private void AddSiteCollectionLinks (List<SPSite> siteCollectionTargets, string relativeLink) {
			foreach (SPSite siteCollection in siteCollectionTargets) {
				string linkText = InstallConfiguration.FormatString (res.ConfigButton, siteCollection.Url, InstallConfiguration.SolutionShortName);
				AddLink (linkText, 0, linkText.Length, siteCollection.Url + relativeLink);
			}
		}

		#endregion

		#region Event Handlers

		void linkLabel_LinkClicked (object sender, LinkLabelLinkClickedEventArgs e) {
			string target = e.Link.LinkData as string;
			try {
				System.Diagnostics.Process.Start (target);
			} catch (Exception ex) {
				MessageBox.Show (string.Format (res.ProcessingError, target) + "\r\n\r\n" + ex.Message);
			}
		}

		#endregion

	}

}
