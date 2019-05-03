
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using roxority.Shared;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

using SystemWebPart = System.Web.UI.WebControls.WebParts.WebPart;
using TahoeWebPart = Microsoft.SharePoint.WebPartPages.WebPart;

namespace roxority_UploadZen {

	public class UploadZenMenuItem : ZenMenuItem {

		internal static readonly string cssIncludes = "<link rel=\"stylesheet\" type=\"text/css\" href=\"{0}/_layouts/roxority_UploadZen/roxority.tl.css?v={1}\"/><link rel=\"stylesheet\" type=\"text/css\" href=\"{0}/_layouts/roxority_UploadZen/roxority_UploadZen.css?v={1}\"/>";

		private static readonly Dictionary<Guid, string []> blockedExts = new Dictionary<Guid, string []> ();
		private static readonly Random rnd = new Random ();

		internal roxority_UploadWebPart uploadWebPart = null;
		internal readonly LiteralControl cssLink = new LiteralControl ("<div id=\"rox_content\">");

		private readonly Dictionary<string, UploadControl> uploaders = new Dictionary<string, UploadControl> ();
		private readonly Panel panel = new Panel ();

		private List<string> versions = null;
		private int ctlPos = 0;

		private static string Gb (IDictionary inst, string name, bool def) {
			return GetBool (inst, name, def) ? "1" : "0";
		}

		internal static string CompleteUrl (SPWeb web, string preparedUrl, bool escapeForJS) {
			string userName = string.Empty;
			try {
				userName = ProductPage.LoginName (web.CurrentUser.LoginName);
			} catch {
			}
			return preparedUrl.Substring (0, preparedUrl.IndexOf ("&u=") + "&u=".Length) + userName.Replace ("\\", escapeForJS ? "\\\\" : "\\") + "&be=" + string.Join (",", GetBlockedExtensions (web)) + "&r=" + rnd.Next ();
		}

		internal static string GetFolderPath (SPContext ctx, HttpContext context, SPList list) {
#if SP12
			string rootFolder = context.Request.QueryString ["RootFolder"];
#else
			string rootFolder = ((ctx == null) ? context.Request.QueryString ["RootFolder"] : ctx.RootFolderUrl);
#endif
			if ((rootFolder == null) || "*".Equals (rootFolder))
				rootFolder = string.Empty;
			if (rootFolder.StartsWith (SPContext.Current.Web.Url, StringComparison.InvariantCultureIgnoreCase))
				rootFolder = rootFolder.Substring (SPContext.Current.Web.Url.Length);
			if (rootFolder.StartsWith (SPContext.Current.Web.ServerRelativeUrl, StringComparison.InvariantCultureIgnoreCase))
				rootFolder = rootFolder.Substring (SPContext.Current.Web.ServerRelativeUrl.Length);
			return (rootFolder.Trim ('/').StartsWith (list.RootFolder.Url.Trim ('/'), StringComparison.InvariantCultureIgnoreCase) ? rootFolder.Trim ('/').Substring (list.RootFolder.Url.Trim ('/').Length) : rootFolder).Trim ('/');
		}

		internal static string PrepareUrl (SPContext ctx, HttpContext context, IDictionary action, SPWeb web, SPList list) {
			ProductPage.LicInfo li = ProductPage.LicInfo.Get (null);
			return "/_layouts/roxority_UploadZen/help/app/roxUp.application?cl=" + Gb (action, "cl", true) + "&w=" + ((SPSecurity.AuthenticationMode == AuthenticationMode.Windows) ? 1 : 0) + "&cf=" + Gb (action, "cf", true) + "&z=" + action ["uz"] + "&c=" + Gb (action, "sc", true) + "&d=" + (GetBool (action, "rp", true) ? 0 : 1) + "&l=" + context.Server.UrlEncode (list.RootFolder.Url) + "&f=" + context.Server.UrlEncode (GetFolderPath (ctx, context, list)) + "&s=" + context.Server.UrlEncode (web.Url) + "&t=" + context.Server.UrlEncode (web.Title) + "&hl=" + (((!GetBool (action, "sl", true)) && ProductPage.LicEdition (SPContext.Current, li, 2)) ? 1 : 0) + "&hh=" + (((!GetBool (action, "sh", true)) && ProductPage.LicEdition (SPContext.Current, li, 2)) ? 1 : 0) + "&ht=" + (GetBool (action, "st", true) ? 0 : 1) + "&li=" + (li.expired ? "x" : (ProductPage.LicEdition (SPContext.Current, li, 4) ? "u" : (ProductPage.LicEdition (SPContext.Current, li, 2) ? "b" : "l"))) + "&hn=" + (ProductPage.Config<bool> (SPContext.Current, "HideNote") ? 1 : 0) + "&u=";
		}

		internal static string ReplaceUrl (string url, string baseName) {
			string one = "/_layouts/" + baseName + ".aspx?", two = "/_layouts/roxority_UploadZen/" + baseName + (ProductPage.Is14 ? 14 : 12) + ".aspx?";
			int pos = url.IndexOf (one, StringComparison.InvariantCultureIgnoreCase);
			if (url.IndexOf ("MultipleUpload=1", StringComparison.InvariantCultureIgnoreCase) > 0)
				return url;
			if (pos > 0)
				return url.Substring (0, pos) + two + url.Substring (pos + one.Length);
			else if (pos == 0)
				return two + url.Substring (pos + one.Length);
			else
				return url;
		}

		public static string [] GetBlockedExtensions (SPWeb web) {
			string [] exts = null;
			Guid siteID = web.Site.ID;
			blockedExts.TryGetValue (web.Site.ID, out exts);
			if (exts == null)
				try {
					web.Site.CatchAccessDeniedException = false;
					SPSecurity.CatchAccessDeniedException = false;
					ProductPage.Elevate (delegate () {
						using (SPSite site = new SPSite (siteID))
							site.WebApplication.BlockedFileExtensions.CopyTo (exts = new string [site.WebApplication.BlockedFileExtensions.Count], 0);
					}, true, false);
					blockedExts [siteID] = exts;
				} catch {
				}
			return ((exts == null) ? new string [0] : exts);
		}

		public static bool GetBool (IDictionary inst, string name, bool def) {
			object obj = inst [name];
			return ((obj is bool) ? ((bool) obj) : def);
		}

		public static string GetUrl (SPContext ctx, HttpContext context, IDictionary action, SPWeb web, SPList list, bool escapeForJS) {
			return CompleteUrl (web, PrepareUrl (ctx, context, action, web, list), escapeForJS);
		}

		public static string ReplaceUrl (string url) {
			return ReplaceUrl (url, (ProductPage.Is14 && (url.IndexOf ("/_layouts/uploadex.aspx", StringComparison.InvariantCultureIgnoreCase) >= 0)) ? "uploadex" : "upload");
		}

		public UploadZenMenuItem () {
			actionPropPrefix = "UploadAction_";
		}

		protected override void CreateChildControls () {
			Page.ClientScript.RegisterClientScriptInclude (ProductPage.AssemblyName + "_plup", SpContext.Web.ServerRelativeUrl.TrimEnd ('/') + "/_layouts/" + ProductPage.AssemblyName + "/plup.js?v=" + ProductPage.Version);
			base.CreateChildControls ();
		}

		protected override string GetActionUrl (IDictionary inst, SPWeb thisWeb, bool useView, bool includeFilters, List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters, string fj, bool flag) {
			return GetActionUrl (inst, thisWeb, useView, includeFilters, effectiveFilters, fj, false, false);
		}

		protected override string GetClickScript (string siteUrl, string clickScript, IDictionary inst, SPWeb thisWeb, bool useView, bool includeFilters, List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters, string fj, bool flag) {
			string id = string.Empty;
			UploadControl uploader = null;
			if ("c".Equals (inst ["m"]))
				return string.Empty;
			if (WebPart != null) {
				if ((!uploaders.TryGetValue (id = inst ["id"] + string.Empty, out uploader)) && ((uploader = Page.LoadControl ("~/_controltemplates/roxority_UploadZen/Uploader.ascx") as UploadControl) != null)) {
					uploaders [id] = uploader;
					uploader.ID = id;
					uploader.action = inst;
					uploader.menuItem = this;
				}
				if (uploader != null) {
					if (!panel.Controls.Contains (cssLink))
						try {
							panel.Controls.Add (cssLink);
							if (!Context.Items.Contains ("roxUplCss")) {
								cssLink.Text = string.Format (cssIncludes, siteUrl, ProductPage.Version) + cssLink.Text;
								Context.Items.Add ("roxUplCss", new object ());
							}
							panel.Controls.Add (new LiteralControl ("</div>"));
						} catch {
						}
					if (!panel.Controls.Contains (uploader))
						try {
							panel.Controls.AddAt (++ctlPos, uploader);
						} catch {
						}
					if (!WebPart.Controls.Contains (panel))
						try {
							WebPart.Controls.Add (panel);
						} catch {
						}
				}
			}
			return ((uploader == null) ? string.Empty : "roxUps['" + uploader.ClientID + "'].show();");
		}

		protected override void OnActionsCreated (int cmdCount) {
			Control ctl;
			if (((ProductPage.Config (null, "HideAction") == "always") || ((ProductPage.Config (null, "HideAction") == "auto") && (cmdCount > 0))) && ((ctl = MenuTemplateControl.FindControl ("MultipleUpload")) != null)) {
				ctl.Visible = false;
				Page.ClientScript.RegisterClientScriptBlock (typeof (ClientScriptManager), ProductPage.AssemblyName + "_HideAction", " GetMultipleUploadEnabled = function() { return false; }; ", true);
			}
		}

		public string GetActionUrl (IDictionary inst, SPWeb thisWeb, bool useView, bool includeFilters, List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters, string fj, bool forceClickOnce, bool escapeForJS) {
			if (forceClickOnce || "c".Equals (inst ["m"]))
				return IsLic (4) ? GetUrl (SpContext, Context, inst, thisWeb, List, escapeForJS) : "/_layouts/roxority_UploadZen/default.aspx?cfg=lic";
			else
				return string.Empty;
		}

		public bool HasVersion {
			get {
				foreach (string v in Versions)
					if (new Version (v).Major >= 2)
						return true;
				return false;
			}
		}

		public override SPList List {
			get {
				if ((list == null) && (uploadWebPart != null))
					list = uploadWebPart.Library;
				return base.List;
			}
		}

		public override SPFolder ListFolder {
			get {
				if ((uploadWebPart != null) && (uploadWebPart.Library != null) && (uploadWebPart.folder != null))
					return uploadWebPart.folder;
				return base.ListFolder;
			}
		}

		public override string RootFolderUrl {
			get {
				if ((uploadWebPart != null) && (uploadWebPart.Library != null) && (uploadWebPart.folder != null))
					return uploadWebPart.folder.Url;
				return base.RootFolderUrl;
			}
		}

		public override bool LoadScript {
			get {
				return true;
			}
		}

		public override string RibbonGroup {
			get {
				return "New";
			}
		}

		public override string RibbonPath {
			get {
				return "Documents";
			}
		}

		public List<string> Versions {
			get {
				if (versions == null) {
					versions = new List<string> (Context.Request.UserAgent.Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries));
					for (int i = 0; i < versions.Count; i++)
						if ((versions [i] = versions [i].Trim ()).StartsWith (".NET CLR"))
							versions [i] = versions [i].Substring (9, 3);
						else {
							versions.RemoveAt (i);
							i--;
						}
				}
				return versions;
			}
		}

		public override SPView View {
			get {
				SPView view;
				if ((uploadWebPart != null) && (uploadWebPart.Library != null) && (!Guid.Empty.Equals (uploadWebPart.viewID)) && ((view = uploadWebPart.Library.Views [uploadWebPart.viewID]) != null))
					return view;
				return base.View;
			}
		}

		public override Guid ViewID {
			get {
				if ((uploadWebPart != null) && (uploadWebPart.Library != null) && !Guid.Empty.Equals (uploadWebPart.viewID))
					return uploadWebPart.viewID;
				return base.ViewID;
			}
		}

		public override SystemWebPart WebPart {
			get {
				if ((webPart == null) && (uploadWebPart != null) && (uploadWebPart.Library != null) && (uploadWebPart.libWebPart != null))
					return uploadWebPart.libWebPart;
				return base.WebPart;
			}
		}

	}

}
