
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace roxority_UploadZen {

	public class UploadPage : Microsoft.SharePoint.ApplicationPages.UploadPage {

		private ProductPage prodPage = null;
		private SPSite adminSite = null;

		public static void RenderUploadZenActions (HtmlTextWriter __w, HttpContext context, ref SPSite adminSite, ref ProductPage prodPage, SPWeb web, SPList list, bool multiUploadMode, HyperLink uploadMultipleLink) {
			string temp, srcUrl;
			int cmdCount = 0;
			IEnumerable<IDictionary> actionsEnum;
			SPContext ctx = ProductPage.GetContext ();
			bool hideAlways = ProductPage.Config (ctx, "HideLink").Equals ("always"), hideAuto = ProductPage.Config (ctx, "HideLink").Equals ("auto");
			HyperLink link;
			List<IDictionary> actions = (((actionsEnum = JsonSchemaManager.GetInstances (prodPage = new ProductPage (), null, "UploadActions", web, list, null, true, true, false)) == null) ? new List<IDictionary> () : new List<IDictionary> (actionsEnum));
			if ((!ProductPage.isEnabled) && (adminSite == null))
				adminSite = ProductPage.GetAdminSite ();
			if (multiUploadMode) {
			} else {
				foreach (IDictionary inst in actions)
					if (UploadZenMenuItem.GetBool (inst, "p", true)) {
						link = new HyperLink ();
						link.Text = JsonSchemaManager.GetDisplayName (inst, "UploadActions", false);
						link.ToolTip = inst ["desc"] + string.Empty;
						if ("c".Equals (inst ["m"]))
							link.NavigateUrl = ProductPage.isEnabled ? UploadZenMenuItem.GetUrl (SPContext.Current, context, inst, web, list, false) : ("javascript:if(confirm('" + (SPEncode.ScriptEncode (ProductPage.GetResource ("NotEnabledPlain", temp = ProductPage.MergeUrlPaths (adminSite.Url, "/_layouts/roxority_UploadZen.aspx?cfg=enable"), "UploadZen")) + "\\n\\n" + SPEncode.ScriptEncode (ProductPage.GetResource ("NotEnabledPrompt"))) + "'))location.href='" + temp + "';");
						else {
							if (string.IsNullOrEmpty (srcUrl = context.Request.QueryString ["Source"]))
								srcUrl = list.DefaultViewUrl + "?";
							else
								srcUrl += "&";
							link.NavigateUrl = srcUrl + "roxuplshow=" + inst ["id"];
						}
						link.Target = "_top";
						link.RenderControl (__w);
						__w.Write ("<br/>");
						cmdCount++;
					}
				if (hideAlways || (hideAuto && (cmdCount > 0)))
					uploadMultipleLink.Visible = false;
				if ((!uploadMultipleLink.NavigateUrl.StartsWith ("/")) && (!uploadMultipleLink.NavigateUrl.StartsWith ("http")))
					uploadMultipleLink.NavigateUrl = web.Url.TrimEnd ('/') + "/_layouts/" + uploadMultipleLink.NavigateUrl;
			}
		}

#if !SP12
		public UploadPage () {
			customPage = true;
		}

		protected override void OnPreInit (EventArgs e) {
			customPage = true;
			base.OnPreInit (e);
		}
#endif

		protected void RenderUploadZenActions (HtmlTextWriter __w) {
			RenderUploadZenActions (__w, Context, ref adminSite, ref prodPage, Web, CurrentList, MultipleUploadMode, UploadMultipleLink);
		}

		public override void Dispose () {
			if (prodPage != null)
				prodPage.Dispose ();
			if (adminSite != null)
				adminSite.Dispose ();
			base.Dispose ();
		}

		public string FolderMap {
			get {
				string folderPath = UploadZenMenuItem.GetFolderPath (SPContext.Current, Context, CurrentList), combinedPath = string.Empty, output = "";
				SPFolder lastFolder = CurrentList.RootFolder;
				foreach (string subPath in folderPath.Split (new char [] { '/' }, StringSplitOptions.RemoveEmptyEntries)) {
					combinedPath = (combinedPath + '/' + subPath).Trim ('/');
					if (lastFolder != null)
						try {
							lastFolder = lastFolder.SubFolders [subPath];
						} catch {
							lastFolder = null;
						}
					output += string.Format ("<span><a class=\"ms-sitemapdirectional\" href=\"{0}\">{1}</a></span><span> &gt; </span>", ProductPage.MergeUrlPaths (Web.Url, (lastFolder == null) ? CurrentList.RootFolder.Url.TrimEnd ('/') + '/' + combinedPath : lastFolder.Url), (lastFolder != null) ? ((lastFolder.Item != null) ? (string.IsNullOrEmpty (lastFolder.Item.Title) ? (string.IsNullOrEmpty (lastFolder.Item.Name) ? subPath : lastFolder.Item.Name) : lastFolder.Item.Title) : (string.IsNullOrEmpty (lastFolder.Name) ? subPath : lastFolder.Name)) : subPath);
				}
				return output;
			}
		}

#if !SP12
		public new bool IsSolutionCatalog {
			get {
				return base.IsSolutionCatalog;
			}
		}

		public new string strListTitle {
			get {
				return base.strListTitle;
			}
		}
#endif

	}

}
