
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace roxority_UploadZen {

	public class UploadExPage : Microsoft.Office.RecordsManagement.PolicyFeatures.ApplicationPages.UploadExPage {

		protected ProductPage prodPage = null;
		protected SPSite adminSite = null;

		protected void RenderUploadZenActions (HtmlTextWriter __w) {
			
			UploadPage.RenderUploadZenActions (__w, Context, ref adminSite, ref prodPage, Web, CurrentList, MultipleUploadMode, UploadMultipleLink);
		}

		public override void Dispose () {
			if (prodPage != null)
				prodPage.Dispose ();
			if (adminSite != null)
				adminSite.Dispose ();
			base.Dispose ();
		}

		public new SPList CurrentList {
			get {
				return base.CurrentList;
			}
		}

		public new bool MultipleUploadMode {
			get {
				return base.MultipleUploadMode;
			}
		}

		public new HyperLink UploadMultipleLink {
			get {
				return base.UploadMultipleLink;
			}
			set {
				base.UploadMultipleLink = value;
			}
		}

		public new SPWeb Web {
			get {
				return base.Web;
			}
		}

	}

}
