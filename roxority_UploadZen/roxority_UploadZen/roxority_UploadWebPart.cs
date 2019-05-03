
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.WebPartPages.Communication;
using roxority.Shared;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;

using TahoeWebPart = Microsoft.SharePoint.WebPartPages.WebPart;
using SystemWebPart = System.Web.UI.WebControls.WebParts.WebPart;

namespace roxority_UploadZen {

	public class roxority_UploadWebPart : WebPartBase, ICellConsumer {

		public event CellConsumerInitEventHandler CellConsumerInit;

		internal SystemWebPart libWebPart = null;
		internal Guid viewID = Guid.Empty;
		internal SPFolder folder = null;
		internal Dictionary<SystemWebPart, SPDocumentLibrary> libParts = null;
		internal SPSite extSite = null;
		internal SPWeb extWeb = null, web = SPContext.Current.Web;
		internal SPList library = null;

		private UploadControl uploader = null;
		private bool allowSelectFolder = false, allowSelectLibPage = false, allowSelectLibWeb = false, allowSelectLibSite = false, allowSelectAction = false;
		private string cellLibFolder = null, excludeFolderNames = "Forms", uploadAction = string.Empty, targetFolder = string.Empty;

		private List<IDictionary> instances = null;
		private ProductPage prodPage = null;
		private JsonSchemaManager jsonFarmMan = null, jsonSiteMan = null;

		public static Guid GetListID (ListViewWebPart lvwp) {
#if SP12
			return ProductPage.GetGuid (lvwp.ListName, true);
#else
			return lvwp.ListId;
#endif
		}

		public static Guid GetListID (DataFormWebPart dfwp) {
#if SP12
			return ProductPage.GetGuid (dfwp.ListName, true);
#else
			return dfwp.ListId;
#endif
		}

		public roxority_UploadWebPart () {
			urlPropertyPrefix = "upload_";
		}

		void ICellConsumer.CellProviderInit (object sender, CellProviderInitEventArgs cellProviderInitArgs) {
		}

		void ICellConsumer.CellReady (object sender, CellReadyEventArgs cellReadyArgs) {
			cellLibFolder = cellReadyArgs.Cell + string.Empty;
		}

		protected override void CreateChildControls () {
			IDictionary action = null, defAction = null;
			LiteralControl ctl = new LiteralControl ("<div id=\"rox_content\">");
			foreach (IDictionary dict in Instances)
				if ("default".Equals (dict ["id"]))
					defAction = dict;
				else if (uploadAction.Equals (dict ["id"]))
					action = dict;
			if ((action != null) || (defAction != null)) {
				uploader = Page.LoadControl ("~/_controltemplates/roxority_UploadZen/Uploader.ascx") as UploadControl;
				uploader.action = ((action == null) ? defAction : action);
				uploader.menuItem = new UploadZenMenuItem () {
					uploadWebPart = this
				};
				uploader.webPart = this;
				if (!Context.Items.Contains ("roxUplCss")) {
					Context.Items.Add ("roxUplCss", new object ());
					ctl.Text = string.Format (UploadZenMenuItem.cssIncludes, uploader.WebUrl, ProductPage.Version) + ctl.Text;
				}
				if (!(ProductPage.Config<bool> (null, "_nojquery") || Page.Items.Contains ("jquery"))) {
					Page.Items ["jquery"] = new object ();
					Page.ClientScript.RegisterClientScriptInclude ("jquery", uploader.WebUrl + "/_layouts/" + ProductPage.AssemblyName + "/jQuery.js?v=" + ProductPage.Version);
				}
				Page.ClientScript.RegisterClientScriptInclude (ProductPage.AssemblyName + "_plup", uploader.WebUrl + "/_layouts/" + ProductPage.AssemblyName + "/plup.js?v=" + ProductPage.Version);
				Page.ClientScript.RegisterClientScriptInclude (ProductPage.AssemblyName, uploader.WebUrl + "/_layouts/" + ProductPage.AssemblyName + "/" + ProductPage.AssemblyName + ".js?v=" + ProductPage.Version);
				Controls.Add (ctl);
				Controls.Add (uploader);
				Controls.Add (new LiteralControl ("</div>"));
			}
		}

		public override void Dispose () {
			if (jsonFarmMan != null) {
				jsonFarmMan.Dispose ();
				jsonFarmMan = null;
			}
			if (jsonSiteMan != null) {
				jsonSiteMan.Dispose ();
				jsonSiteMan = null;
			}
			if (prodPage != null) {
				prodPage.Dispose ();
				prodPage = null;
			}
			if (extWeb != null) {
				extWeb.Dispose ();
				extWeb = null;
			}
			if (extSite != null) {
				extSite.Dispose ();
				extSite = null;
			}
			base.Dispose ();
		}

		public override void EnsureInterfaces () {
			try {
				RegisterInterface ("roxorityUploadZenTargetFolderConsumer_", "ICellConsumer", 1, ConnectionRunAt.Server, this, "roxorityUploadZenTargetFolderConsumer_Client_", ProductPage.GetProductResource ("PD_UploadWebPart_TargetFolderGet"), ProductPage.GetProductResource ("PD_UploadWebPart_TargetFolderGet"), false);
			} catch {
			}
		}

		public override InitEventArgs GetInitEventArgs (string InterfaceName) {
			if ((string.IsNullOrEmpty (InterfaceName)) || "roxorityUploadZenTargetFolderConsumer_".Equals (InterfaceName))
				return new CellConsumerInitEventArgs () {
					FieldName = "TargetFolder", FieldDisplayName = this ["PD_UploadWebPart_TargetFolder"]
				};
			return base.GetInitEventArgs (InterfaceName);
		}

		public override string LoadResource (string id) {
			return ProductPage.GetProductResource (id);
		}

		public override void PartCommunicationConnect (string interfaceName, TahoeWebPart connectedPart, string connectedInterfaceName, ConnectionRunAt runAt) {
		}

		public override void PartCommunicationInit () {
			if (CellConsumerInit != null)
				CellConsumerInit (this, GetInitEventArgs(string.Empty) as CellConsumerInitEventArgs );
		}

		public override void PartCommunicationMain () {
		}

		protected override void RenderWebPart (HtmlTextWriter output) {
			if ((uploader != null) && (Library == null))
				output.Write ("<div>" + this ["NoTargetFolder"] + "</div>");
			else
				base.RenderWebPart (output);
		}

		internal IEnumerable<IDictionary> Instances {
			get {
				if (instances == null) {
					instances = new List<IDictionary> ();
					foreach (IDictionary dict in JsonSiteSchema.Instances)
						if (!"c".Equals (dict ["m"]))
							if ("default".Equals (dict ["id"]))
								instances.Insert (0, dict);
							else
								instances.Add (dict);
					foreach (IDictionary dict in JsonFarmSchema.Instances)
						if (!"c".Equals (dict ["m"]))
							instances.Add (dict);
				}
				return instances;
			}
		}

		internal JsonSchemaManager JsonFarmMan {
			get {
				if (jsonFarmMan == null)
					jsonFarmMan = new JsonSchemaManager (ProdPage, null, false, null);
				return jsonFarmMan;
			}
		}

		internal JsonSchemaManager JsonSiteMan {
			get {
				if (jsonSiteMan == null)
					jsonSiteMan = new JsonSchemaManager (ProdPage, null, true, null);
				return jsonSiteMan;
			}
		}

		internal JsonSchemaManager.Schema JsonFarmSchema {
			get {
				return JsonFarmMan.AllSchemas ["UploadActions"];
			}
		}

		internal JsonSchemaManager.Schema JsonSiteSchema {
			get {
				return JsonSiteMan.AllSchemas ["UploadActions"];
			}
		}

		internal ProductPage ProdPage {
			get {
				if (prodPage == null)
					prodPage = new ProductPage ();
				return prodPage;
			}
		}

		[Personalizable, WebBrowsable, WebPartStorage (Storage.Shared), SPWebCategoryName ("[ROXORITY™] UploadZen"), Resources ("PC_UploadWebPart_AllowSelectFolder", "", "")]
		public bool AllowSelectFolder {
			get {
				return (!HasExtList) && allowSelectFolder;
			}
			set {
				allowSelectFolder = value;
			}
		}

		[Personalizable, WebBrowsable, WebPartStorage (Storage.Shared), SPWebCategoryName ("[ROXORITY™] UploadZen"), Resources ("PC_UploadWebPart_AllowSelectLibPage", "", "")]
		public bool AllowSelectLibPage {
			get {
				return (!HasExtList) && allowSelectLibPage;
			}
			set {
				allowSelectLibPage = value;
			}
		}

		[Personalizable, WebBrowsable, WebPartStorage (Storage.Shared), SPWebCategoryName ("[ROXORITY™] UploadZen"), Resources ("PC_UploadWebPart_AllowSelectLibWeb", "", "")]
		public bool AllowSelectLibWeb {
			get {
				return (!HasExtList) && allowSelectLibWeb;
			}
			set {
				allowSelectLibWeb = value;
			}
		}

		[Personalizable, WebBrowsable, WebPartStorage (Storage.Shared), SPWebCategoryName ("[ROXORITY™] UploadZen"), Resources ("PC_UploadWebPart_AllowSelectLibSite", "", "")]
		public bool AllowSelectLibSite {
			get {
				return (!HasExtList) && allowSelectLibSite;
			}
			set {
				allowSelectLibSite = value;
			}
		}

		[Personalizable, WebBrowsable, WebPartStorage (Storage.Shared), SPWebCategoryName ("[ROXORITY™] UploadZen"), Resources ("PC_UploadWebPart_AllowSelectAction", "", "")]
		public bool AllowSelectAction {
			get {
				return allowSelectAction;
			}
			set {
				allowSelectAction = value;
			}
		}

		public string EffectiveFolder {
			get {
				int pos;
				string f = null;
				if (uploader != null)
					if ((uploader.FolderDropDown != null) && (uploader.FolderDropDown.Items.Count > 0) && !string.IsNullOrEmpty (uploader.FolderDropDown.SelectedValue))
						f = HttpUtility.UrlDecode (uploader.FolderDropDown.SelectedValue).TrimStart ('/');
					else if ((uploader.LibDropDown != null) && (uploader.LibDropDown.Items.Count > 0) && !string.IsNullOrEmpty (uploader.LibDropDown.SelectedValue))
						f = uploader.LibDropDown.SelectedValue;
				if ((f == null) && (f = string.IsNullOrEmpty (cellLibFolder) ? targetFolder : cellLibFolder).EndsWith (".aspx", StringComparison.InvariantCultureIgnoreCase) && (((pos = f.IndexOf ("/forms/", StringComparison.InvariantCultureIgnoreCase)) > 0) || ((pos = f.LastIndexOf ('/')) > 0)))
					f = f.Substring (0, pos);
				return f;
			}
		}

		[Personalizable, WebBrowsable, WebPartStorage (Storage.Shared), SPWebCategoryName ("[ROXORITY™] UploadZen"), Resources ("PC_UploadWebPart_ExcludeFolderNames", "", "")]
		public string ExcludeFolderNames {
			get {
				return excludeFolderNames;
			}
			set {
				excludeFolderNames = value;
			}
		}

		public bool HasExtList {
			get {
				return (extSite != null);
			}
		}

		internal SPList TryGetList (SPListCollection lists, Guid id) {
			try {
				return lists [id];
			} catch {
			}
			return null;
		}

		public SPList Library {
			get {
				int pos;
				string rootFolder = Context.Request.QueryString ["RootFolder"], tf, fp;
				ListViewWebPart lvwp;
				DataFormWebPart dfwp;
				SystemWebPart firstPart = null;
				SPDocumentLibrary lib = null;
				JsonSchemaManager.Property.Type.LibSet.Config libSet = null;
				if (library == null) {
					if (uploader != null)
						if ((uploader.WebDropDown != null) && (uploader.WebDropDown.Items.Count > 0))
							try {
								web = SPContext.Current.Site.AllWebs [ProductPage.GetGuid (uploader.WebDropDown.SelectedValue, true)];
							} catch {
							} else if ((uploader.MenuItem != null) && (uploader.MenuItem.SpContext != null))
							web = uploader.MenuItem.SpContext.Web;
					if (!string.IsNullOrEmpty (fp = EffectiveFolder)) {
						if (ProductPage.IsGuid (fp))
							lib = TryGetList (SPContext.Current.Web.Lists, ProductPage.GetGuid (fp, true)) as SPDocumentLibrary;
						else
							try {
								if (fp.StartsWith ("http:", StringComparison.InvariantCultureIgnoreCase) || fp.StartsWith ("https:", StringComparison.InvariantCultureIgnoreCase))
									extSite = new SPSite (fp);
								if (extSite != null)
									extWeb = extSite.OpenWeb ();
								else if ((tf = fp).StartsWith ("/", StringComparison.InvariantCultureIgnoreCase))
									while ((((extWeb = SPContext.Current.Site.OpenWeb (tf)) == null) || !extWeb.Exists) && ((pos = tf.LastIndexOf ('/')) > 0))
										tf = tf.Substring (0, pos);
								if (extWeb != null)
									web = extWeb;
								else if (("/" + fp).StartsWith (web.ServerRelativeUrl))
									fp = fp.Substring (web.ServerRelativeUrl.Length);
								folder = web.GetFolder ((extWeb == null) ? fp : fp.Substring (fp.StartsWith (extWeb.Url, StringComparison.InvariantCultureIgnoreCase) ? (extWeb.Url.TrimEnd ('/') + "/").Length : 0));
								if (!Guid.Empty.Equals (folder.ParentListId))
									lib = TryGetList (folder.ParentWeb.Lists, folder.ParentListId) as SPDocumentLibrary;
							} catch {
							}
						library = lib;
					}
					if (Guid.Empty.Equals (viewID) && !string.IsNullOrEmpty (Context.Request.QueryString ["View"]))
						viewID = ProductPage.GetGuid (Context.Request.QueryString ["View"], true);
					libParts = new Dictionary<SystemWebPart, SPDocumentLibrary> ();
					if ((uploader != null) && (uploader.Action != null))
						libSet = new JsonSchemaManager.Property.Type.ListSet.Config (uploader.Action);
					foreach (SystemWebPart wp in ProductPage.TryEach<SystemWebPart> (WebPartManager.GetCurrentWebPartManager (Page).WebParts))
						if (((lvwp = wp as ListViewWebPart) != null) && ((lib = TryGetList (SPContext.Current.Web.Lists, GetListID (lvwp)) as SPDocumentLibrary) != null) && ((libSet == null) || libSet.IsMatch (lib, JsonFarmMan) || libSet.IsMatch (lib, JsonSiteMan))) {
							libParts [lvwp] = lib;
							if ((firstPart == null) || ((!Guid.Empty.Equals (viewID)) && viewID.Equals (ProductPage.GetGuid (lvwp.ViewGuid, true))))
								firstPart = lvwp;
						} else if (((dfwp = wp as DataFormWebPart) != null) && ((lib = TryGetList (SPContext.Current.Web.Lists, GetListID (dfwp)) as SPDocumentLibrary) != null) && ((libSet == null) || libSet.IsMatch (lib, JsonFarmMan) || libSet.IsMatch (lib, JsonSiteMan))) {
							libParts [dfwp] = lib;
							if ((firstPart == null) || ((!Guid.Empty.Equals (viewID)) && viewID.Equals (dfwp.StorageKey)))
								firstPart = dfwp;
						}
					if ((library == null) && ((library = ((firstPart == null) ? SPContext.Current.List : libParts [firstPart])) != null))
						folder = string.IsNullOrEmpty (rootFolder) ? library.RootFolder : library.ParentWeb.GetFolder (rootFolder);
					if ((libWebPart == null) || (firstPart != null))
						libWebPart = firstPart;
				}
				return library;
			}
		}

		[Personalizable, WebBrowsable, WebPartStorage (Storage.Shared), SPWebCategoryName ("[ROXORITY™] UploadZen"), Resources ("PC_UploadWebPart_UploadAction", "", "")]
		public string UploadAction {
			get {
				return uploadAction;
			}
			set {
				uploadAction = value;
			}
		}

		[Personalizable, WebBrowsable, WebPartStorage (Storage.Shared), SPWebCategoryName ("[ROXORITY™] UploadZen"), Resources ("PC_UploadWebPart_TargetFolder", "", "")]
		public string TargetFolder {
			get {
				return targetFolder;
			}
			set {
				targetFolder = value;
			}
		}

		public SPWeb TargetWeb {
			get {
				return web;
			}
		}

	}

}
