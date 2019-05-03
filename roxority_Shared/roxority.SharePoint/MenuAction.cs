
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using roxority.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml;

using SystemWebPart = System.Web.UI.WebControls.WebParts.WebPart;

namespace roxority.SharePoint {

	public class ZenMenuItem : WebControl {

		#region RibbonItem Class

		private class RibbonItem {

			public readonly MenuItemTemplate Item;
			public readonly IDictionary Inst;
			public readonly string Caption, ClickScript, ClickUrl, Desc, ID, Img;
			public readonly int CmdNo;

			public RibbonItem (MenuItemTemplate item, IDictionary inst, string id, string caption, string img, string desc, string clickUrl, string clickScript, int cmdNo) {
				Item = item;
				Inst = inst;
				ID = id;
				Caption = caption;
				Desc = desc;
				Img = img;
				ClickScript = clickScript;
				ClickUrl = clickUrl;
				CmdNo = cmdNo;
			}

		}

		#endregion

		private static readonly Reflector refl = new Reflector (typeof (FormButton).Assembly);

		private static MethodInfo getConnPartMethod = null;
		private static PropertyInfo camlAndProp = null, partFiltersProp = null, partJsonProp = null, kvpValProp = null, fpKeyProp = null, fpValProp = null, fpOpProp = null;
		private static string iconUrl = null, iconUrlSmall = null, schemaName = null;
		[ThreadStatic]
		private static ProductPage.LicInfo l = null;

		public readonly Dictionary<string, string> PhVals = new Dictionary<string, string> ();

		internal string actionPropPrefix = string.Empty, baseSequence = "100";
		internal SystemWebPart webPart = null;
		internal SPList list = null;

		private LiteralControl literal = new LiteralControl ();
		private SPFolder folder = null;
		private SPListItem item = null;
		private MenuTemplate menuTemplate = null;
		private ToolBarMenuButton menuButton = null;
		private SPView view = null;
		private SPContext spCtx = null;
		private string pn = null, tri = null;
		private bool? isDisp = null;
		private List<RibbonItem> ribbons = new List<RibbonItem> ();

		internal static T FindParent<T> (Control parent) where T : class {
			if (parent is T)
				return parent as T;
			if (parent != null)
				return FindParent<T> (parent.Parent);
			return null;
		}

		internal static bool IsLic (int e) {
			return ProductPage.LicEdition (ProductPage.GetContext (), Lic, e);
		}

		internal static ProductPage.LicInfo Lic {
			get {
				if (l == null)
					l = ProductPage.LicInfo.Get (null);
				return l;
			}
		}

		public ZenMenuItem () {
		}

		internal void AddFormButton (string id, string caption, string img, string desc, string clickUrl, string clickScript, int cmdNo) {
			FormButton item = new FormButton ();
			if (cmdNo > 1)
				Controls.Add (new LiteralControl ("</span></td><td class=\"ms-separator\">&nbsp;</td><td class=\"ms-toolbar\" noWrap=\"nowrap\"><span>"));
			item.ID = id;
			item.ToolTip = desc;
			if (string.IsNullOrEmpty (clickScript))
				item.NavigateUrl = clickUrl;
			else
				item.OnClientClick = clickScript + "return(event && (event.returnValue=!(event.cancelBubble=true)));";
			item.ImageUrl = img;
			item.CssClass = "roxlistformbutton";
			item.Text = caption;
			item.ControlMode = SPControlMode.Display;
			item.PermissionContext = PermContext;
			item.Permissions = Perms;
			Controls.Add (item);
			if (ProductPage.Is14) {
				Page.ClientScript.RegisterClientScriptBlock (typeof (ClientScriptManager), ProductPage.AssemblyName + "_Script_" + cmdNo, " " + Prod + "Buttons['" + id + "'] = '" + item.ClientID + "'; ", true);
				AddRibbonButton (id, caption, img, desc, clickUrl, clickScript, cmdNo, item);
			}
		}

		internal void AddItem (IDictionary inst, string id, string caption, string img, string desc, string clickUrl, string clickScript, int cmdNo) {
			foreach (KeyValuePair<string, string> ph in PhVals) {
				caption = caption.Replace (PhName (ph.Key), ph.Value);
				desc = desc.Replace (PhName (ph.Key), ph.Value);
			}
			if ((!DispForm) && !IsDispFormOnly (inst))
				AddMenuItem (inst, id, caption, img, desc, clickUrl, clickScript, cmdNo);
			else if (DispForm && IsDispFormSupported (inst))
				AddFormButton (id, caption, img, desc, clickUrl, clickScript, cmdNo);
		}

		internal void AddMenuItem (IDictionary inst, string id, string caption, string img, string desc, string clickUrl, string clickScript, int cmdNo) {
			MenuItemTemplate item = new MenuItemTemplate (caption, img);
			item.ID = id;
			item.Description = desc;
			item.PermissionContext = PermContext;
			item.Permissions = Perms;
			if (string.IsNullOrEmpty (clickScript))
				item.ClientOnClickNavigateUrl = SPEncode.ScriptEncode (clickUrl);
			else
				item.ClientOnClickScript = clickScript;
			Controls.Add (item);
			if (ProductPage.Is14) {
				ribbons.Add (new RibbonItem (item, inst, ClientID + id, caption, img, desc, clickUrl, clickScript, cmdNo));
				WebPartIDs.ToString ();
			}
		}

		internal void AddRibbonButton (IDictionary inst, string id, string caption, string img, string desc, string clickUrl, string clickScript, int cmdNo, MenuItemTemplate btn) {
			object ribbon = Page.Items [refl.GetType ("Microsoft.SharePoint.WebControls.SPRibbon")];
			XmlDocument doc = new XmlDocument ();
			//if (((ribbons.Count > 1) || (WebPartIDs.Count > 1)) && JsonSchemaManager.GetDisplayName (inst, SchemaName, false).Equals (caption))
			//    caption += (" [" + PhVals ["Context_Title"] + "]");
			if (ribbon != null) {
				doc.LoadXml ("<Button Id=\"Ribbon." + RibbonPath + "." + RibbonGroup + ".Controls." + ProdName + "Action_" + id + "\" Sequence=\"" + baseSequence + cmdNo + "\" Command=\"" + ProdName + "Action\" Image16by16=\"" + SmallIconUrl + "\" Image32by32=\"" + IconUrl + "\" LabelText=\"" + HttpUtility.HtmlEncode (caption) + "\" Description=\"" + HttpUtility.HtmlEncode (desc) + "\" ToolTipDescription=\"" + HttpUtility.HtmlEncode (desc) + "\" ToolTipTitle=\"" + HttpUtility.HtmlEncode (caption + "\r\n[" + PhVals ["Context_Title"] + "]") + "\" Alt=\"\" TemplateAlias=\"o2\" DisplayMode=\"Small\" />");
				refl.Call (ribbon, "RegisterDataExtension", null, new object [] { doc.DocumentElement, "Ribbon." + RibbonPath + "." + RibbonGroup + ".Controls._children" });
			}
		}

		internal void AddRibbonButton (string id, string caption, string img, string desc, string clickUrl, string clickScript, int cmdNo, FormButton btn) {
			object ribbon = Page.Items [refl.GetType ("Microsoft.SharePoint.WebControls.SPRibbon")];
			XmlDocument doc = new XmlDocument ();
			if (ribbon != null) {
				doc.LoadXml ("<Button Id=\"Ribbon." + RibbonPath + "." + RibbonGroup + ".Controls." + ProdName + "Action_" + id + "\" Sequence=\"100" + baseSequence + cmdNo + "\" Command=\"" + ProdName + "Action\" Image16by16=\"" + SmallIconUrl + "\" Image32by32=\"" + IconUrl + "\" LabelText=\"" + HttpUtility.HtmlEncode (caption) + "\" Description=\"" + HttpUtility.HtmlEncode (desc) + "\" ToolTipDescription=\"" + HttpUtility.HtmlEncode (desc) + "\" ToolTipTitle=\"" + HttpUtility.HtmlEncode (caption) + "\" Alt=\"\" TemplateAlias=\"o" + ((cmdNo == 1) ? 1 : 2) + "\" />");
				refl.Call (ribbon, "RegisterDataExtension", null, new object [] { doc.DocumentElement, "Ribbon." + RibbonPath + "." + RibbonGroup + ".Controls._children" });
			}
		}

		internal string PhName(string name) {
			return "{$" + ProdName + "_" + name + "$}";
		}

		public static void GetFilterInfo (IDictionary inst, string schemaName, ref string clickScript, SystemWebPart webPart, Page page, ref bool includeFilters, ref string fj, ref List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters) {
			int i1, i2;
			object val;
			SPWebPartManager wpMan = null;
			SystemWebPart filterPart = null;
			IList flist = null;
			List<string> andFilters = new List<string> ();
			List<object []> filters = new List<object []> ();
			KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>> kvp;
			includeFilters = false;
			if (inst ["filter"] != null)
				try {
					includeFilters = (bool) inst ["filter"];
				} catch {
					includeFilters = false;
				}
			if (includeFilters && !IsLic (2)) {
				includeFilters = false;
				clickScript = "alert(\'" + SPEncode.ScriptEncode (ProductPage.GetResource ("NopeEd", ProductPage.GetProductResource ("PC_" + schemaName + "_filter"), "Basic")) + "\');";
			}
			if (includeFilters && (effectiveFilters == null)) {
				effectiveFilters = new List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> ();
				try {
					wpMan = SPWebPartManager.GetCurrentWebPartManager (page) as SPWebPartManager;
				} catch {
				}
				if ((webPart != null) && (wpMan != null))
					foreach (SystemWebPart wp in ProductPage.TryEach<SystemWebPart> (wpMan.WebParts)) {
						if (wp.GetType ().AssemblyQualifiedName == ProductPage.FILTERZEN_TYPENAME)
							foreach (SystemWebPart cwp in (((getConnPartMethod == null) ? (getConnPartMethod = wp.GetType ().GetMethod ("GetConnectedParts", BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null)) : getConnPartMethod).Invoke (wp, null) as IEnumerable<SystemWebPart>))
								if (cwp.ID.Equals (webPart.ID)) {
									filterPart = wp;
									break;
								}
						if (filterPart != null)
							break;
					}
				if (filterPart != null)
					try {
						if (!((bool) filterPart.GetType ().GetMethod ("LicEd", BindingFlags.NonPublic | BindingFlags.Instance).Invoke (filterPart, new object [] { 2 }))) {
							clickScript = "alert(\'" + SPEncode.ScriptEncode (ProductPage.GetProductResource ("Old_NoFilterZenEnt")) + "\');";
							filterPart = null;
						}
					} catch {
						clickScript = "alert(\'" + SPEncode.ScriptEncode (ProductPage.GetProductResource ("Old_NoFilterZenEnt")) + "\');";
						filterPart = null;
					}
				fj = string.Empty;
				if (filterPart != null) {
					try {
						flist = ((partFiltersProp == null) ? (partFiltersProp = filterPart.GetType ().GetProperty ("PartFilters", BindingFlags.Instance | BindingFlags.Public)) : partFiltersProp).GetValue (filterPart, null) as IList;
						fj = ((partJsonProp == null) ? (partJsonProp = filterPart.GetType ().GetProperty ("JsonFilters", BindingFlags.Instance | BindingFlags.Public)) : partJsonProp).GetValue (filterPart, null) as string;
						andFilters.AddRange (((string) ((camlAndProp == null) ? (camlAndProp = filterPart.GetType ().GetProperty ("CamlFiltersAndCombined", BindingFlags.Instance | BindingFlags.Public)) : camlAndProp).GetValue (filterPart, null)).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
					} catch {
					}
					if (flist != null)
						foreach (object kv in flist) {
							if (kvpValProp == null)
								kvpValProp = kv.GetType ().GetProperty ("Value", BindingFlags.Instance | BindingFlags.Public);
							if ((val = kvpValProp.GetValue (kv, null)) != null) {
								if (fpKeyProp == null)
									fpKeyProp = val.GetType ().GetProperty ("Key", BindingFlags.Public | BindingFlags.Instance);
								if (fpValProp == null)
									fpValProp = val.GetType ().GetProperty ("Value", BindingFlags.Public | BindingFlags.Instance);
								if (fpOpProp == null)
									fpOpProp = val.GetType ().GetProperty ("CamlOperator", BindingFlags.Public | BindingFlags.Instance);
								filters.Add (new object [] { fpKeyProp.GetValue (val, null), fpValProp.GetValue (val, null), (CamlOperator) Enum.Parse (typeof (CamlOperator), fpOpProp.GetValue (val, null).ToString (), true) });
							}
						}
					foreach (object [] f in filters) {
						kvp = new KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>> (f [0] as string, new KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool> (new List<KeyValuePair<string, CamlOperator>> (), andFilters.Contains (f [0] as string)));
						i1 = i2 = -1;
						foreach (KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>> k in effectiveFilters) {
							i2++;
							if (k.Key == kvp.Key) {
								i1 = i2;
								kvp = k;
								break;
							}
						}
						kvp.Value.Key.Add (new KeyValuePair<string, CamlOperator> (f [1] as string, (CamlOperator) f [2]));
						if (i1 >= 0)
							effectiveFilters [i1] = kvp;
						else
							effectiveFilters.Add (kvp);
					}
				}
			}
		}

		protected override void CreateChildControls () {
			string fj = string.Empty, listIDs = string.Empty, clickScript, temp, clickUrl = string.Empty, itemCaption, itemDesc, contentListUrl = string.Empty, siteUrl;
			bool includeFilters = false, useView = false;
			int cmdCount = 0;
			Hashtable customProps = new Hashtable ();
			List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters = null;
			IEnumerable<IDictionary> actions;
			SPContext ctx = SPContext.Current;
			ctx.Site.CatchAccessDeniedException = SPSecurity.CatchAccessDeniedException = false;
			if ((Page != null) && (Page.Header != null) && (List != null) && (Context != null) && (Context.Request != null) && (Context.Request.Url != null) && !Context.Request.Url.AbsolutePath.ToLowerInvariant ().Contains ("/_layouts/roxority_"))
				using (ProductPage prodPage = new ProductPage ())
				using (SPSite site = new SPSite (ctx.Site.ID)) {
					site.CatchAccessDeniedException = false;
					using (SPWeb thisWeb = site.OpenWeb (ctx.Web.ID))
						if ((actions = JsonSchemaManager.GetInstances (prodPage, null, SchemaName, thisWeb, List, DispForm ? null : View, true, true, false)) != null) {
							siteUrl = thisWeb.ServerRelativeUrl.TrimEnd ('/');
							PhVals ["View_Title"] = string.Empty;
							if (WebPart == null)
								PhVals ["WebPart_Title"] = string.Empty;
							PhVals ["TitleBar_Title"] = Page.Title;
							if (LoadScript) {
								if (!(ProductPage.Config<bool> (null, "_nojquery") || Page.Items.Contains ("jquery"))) {
									Page.Items ["jquery"] = new object ();
									Page.ClientScript.RegisterClientScriptInclude ("jquery", siteUrl + "/_layouts/" + ProductPage.AssemblyName + "/jQuery.js?v=" + ProductPage.Version);
								}
								Page.ClientScript.RegisterClientScriptInclude (ProductPage.AssemblyName, siteUrl + "/_layouts/" + ProductPage.AssemblyName + "/" + ProductPage.AssemblyName + ".js?v=" + ProductPage.Version);
							}
							if (ProductPage.Is14)
								Page.ClientScript.RegisterClientScriptBlock (typeof (ClientScriptManager), ProductPage.AssemblyName + "_Script", ProductPage.GetResource ("__RibbonScript", "{", "}", Prod, ProdName), true);
							foreach (IDictionary inst in actions)
								if (IsActionSupported (inst)) {
									foreach (DictionaryEntry prop in inst)
										PhVals [actionPropPrefix + prop.Key] = prop.Value + string.Empty;
									PhVals ["QueryString_a"] = inst ["id"] + string.Empty;
									clickScript = string.Empty;
									if (!ProductPage.isEnabled)
										using (SPSite adminSite = ProductPage.GetAdminSite ())
											clickScript = "if(confirm('" + (SPEncode.ScriptEncode (ProductPage.GetResource ("NotEnabledPlain", temp = ProductPage.MergeUrlPaths (adminSite.Url, "/_layouts/" + ProductPage.AssemblyName + "/default.aspx?cfg=enable&r=" + prodPage.Rnd.Next ()), ProductPage.GetTitle ())) + "\\n\\n" + SPEncode.ScriptEncode (ProductPage.GetResource ("NotEnabledPrompt"))) + "'))location.href='" + temp + "';";
									else {
										if (!Page.Items.Contains (Prod + "zenlistids"))
											Page.Items [Prod + "zenlistids"] = listIDs = "," + List.ID + ",";
										else if (!(listIDs = Page.Items [Prod + "zenlistids"] + string.Empty).Contains ("," + List.ID + ","))
											Page.Items [Prod + "zenlistids"] = listIDs = listIDs + "," + List.ID + ",";
										if (!Page.Items.Contains (Prod + "zencmdcount"))
											Page.Items [Prod + "zencmdcount"] = cmdCount = 1;
										else
											Page.Items [Prod + "zencmdcount"] = cmdCount = ((int) Page.Items [Prod + "zencmdcount"]) + 1;
										ValidateInstance (inst, ref clickScript);
										useView = false;
										if (inst ["view"] != null)
											try {
												useView = (bool) inst ["view"];
											} catch {
												useView = false;
											}
										if (useView && !IsLic (Vl)) {
											useView = false;
											clickScript = "alert(\'" + SPEncode.ScriptEncode (ProductPage.GetResource ("NopeEd", ProductPage.GetProductResource ("PC_" + SchemaName + "_view"), ((Vl == 2) ? "Basic" : ((Vl == 0) ? "Lite" : "Ultimate")))) + "\');";
										}
										GetFilterInfo (inst, SchemaName, ref clickScript, WebPart, Page, ref includeFilters, ref fj, ref effectiveFilters);
									}

									if (Lic.expired)
										itemCaption = ProductPage.GetResource ("LicExpiry");
									else
										itemCaption = JsonSchemaManager.GetDisplayName (inst, SchemaName, false);
									itemDesc = Lic.expired ? ProductPage.GetResource ("LicStudio", ProductPage.GetTitle ()) : (IsLic (2) ? (inst ["desc"] + "") : ("SharePoint-Tools.net/" + ProdName));

									if (string.IsNullOrEmpty (clickScript) && string.IsNullOrEmpty (clickScript = GetClickScript (siteUrl, clickScript, inst, thisWeb, useView, includeFilters, effectiveFilters, fj, GetFlag (inst))))
										AddItem (inst, inst ["id"] + string.Empty, itemCaption, siteUrl + IconUrl, itemDesc, clickUrl = siteUrl + (Lic.expired ? ("/_layouts/" + ProductPage.AssemblyName + "/default.aspx?cfg=lic&r=" + prodPage.Rnd.Next ()) : GetActionUrl (inst, thisWeb, useView, includeFilters, effectiveFilters, fj, GetFlag (inst))), string.Empty, cmdCount);
									else
										AddItem (inst, inst ["id"] + string.Empty, itemCaption, siteUrl + IconUrl, itemDesc, Lic.expired ? (siteUrl + "/_layouts/" + ProductPage.AssemblyName + "/default.aspx?cfg=lic&r=" + prodPage.Rnd.Next ()) : string.Empty, Lic.expired ? string.Empty : clickScript, cmdCount);

									if (string.IsNullOrEmpty (contentListUrl))
										try {
											contentListUrl = siteUrl + "/_layouts/" + ProductPage.AssemblyName + "/default.aspx?cfg=tools&tool=Tool_" + SchemaName + "&r=" + prodPage.Rnd.Next ();
										} catch {
										}
									if ((cmdCount >= 1) && !IsLic (2))
										break;
								}
							if (ribbons.Count > 0)
								foreach (RibbonItem rib in ribbons) {
									Page.ClientScript.RegisterStartupScript (typeof (ClientScriptManager), ProductPage.AssemblyName + "_Script_" + rib.CmdNo, " " + Prod + (string.IsNullOrEmpty (rib.ClickScript) ? "Urls" : "Commands") + "['" + rib.ID + "'] = '" + SPEncode.ScriptEncode (string.IsNullOrEmpty (rib.ClickScript) ? rib.ClickUrl : rib.ClickScript) + "'; ", true);
									AddRibbonButton (rib.Inst, rib.ID, rib.Caption, rib.Img, rib.Desc, rib.ClickUrl, rib.ClickScript, rib.CmdNo, rib.Item);
								}
							OnActionsCreated (cmdCount);
						}
				}
			base.CreateChildControls ();
		}

		protected virtual string GetActionUrl (IDictionary inst, SPWeb thisWeb, bool useView, bool includeFilters, List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters, string fj, bool flag) {
			return "/_layouts/" + ProductPage.AssemblyName + "/default.aspx";
		}

		protected virtual string GetClickScript (string siteUrl, string clickScript, IDictionary inst, SPWeb thisWeb, bool useView, bool includeFilters, List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters, string fj, bool flag) {
			return clickScript;
		}

		protected virtual bool GetFlag (IDictionary inst) {
			return false;
		}

		protected virtual bool IsActionSupported (IDictionary inst) {
			return true;
		}

		protected virtual bool IsDispFormOnly (IDictionary inst) {
			return false;
		}

		protected virtual bool IsDispFormSupported (IDictionary inst) {
			return DispFormSupported;
		}

		protected virtual void OnActionsCreated (int cmdCount) {
		}

		protected override void OnLoad (EventArgs e) {
			EnsureChildControls ();
			base.OnLoad (e);
		}

		protected virtual void PrepareChildControls (IDictionary propBag) {
		}

		protected virtual void ValidateInstance (IDictionary inst, ref string clickScript) {
		}

		internal string this [string resKey, params object [] args] {
			get {
				return ProductPage.GetProductResource (resKey, args);
			}
		}

		internal virtual int Vl {
			get {
				return 4;
			}
		}

		public virtual string IconUrl {
			get {
				if (string.IsNullOrEmpty (iconUrl))
					iconUrl = ProductPage.GetProductResource ("__MenuActionIcon");
				return iconUrl;
			}
		}

		public bool DispForm {
			get {
				return (DispFormSupported && (MenuButton == null) && (ListItem != null));
			}
		}

		public virtual bool DispFormSupported {
			get {
				return false;
			}
		}

		public bool IsCalendar {
			get {
				return ((List != null) && (list.BaseTemplate == SPListTemplateType.Events));
			}
		}

		public bool IsDispForm {
			get {
				if ((isDisp == null) || !isDisp.HasValue)
					isDisp = ((WebPart == null) ? ((!string.IsNullOrEmpty (Context.Request.QueryString ["ID"])) && (Context.Request.RawUrl.ToLowerInvariant ().Contains ("/dispform.aspx?") || Context.Request.RawUrl.ToLowerInvariant ().Contains ("/formdisp.aspx?"))) : (WebPart is ListFormWebPart));
				return isDisp.Value;
			}
		}

		public virtual SPList List {
			get {
				if (list == null) {
					if (MenuButton != null)
						list = menuButton.List;
					if ((list == null) && (ListItem != null))
						list = item.ParentList;
					if (list != null) {
						PhVals ["List_Title"] = list.Title;
						PhVals ["QueryString_l"] = ProductPage.GuidBracedUpper (list.ID);
						if ((ListItem == null) && (WebPart == null) && (View == null))
							PhVals ["Context_Title"] = list.Title;
					}
				}
				return list;
			}
		}

		public virtual SPFolder ListFolder {
			get {
				ListViewWebPart lvwp;
				DataFormWebPart dfwp;
				if ((folder == null) && (MenuButton!=null)) {
				}
				return folder;
			}
		}

		public SPListItem ListItem {
			get {
				if (DispFormSupported && IsDispForm && (item == null) && ((item = SpContext.ListItem) != null)) {
					PhVals ["Context_Title"] = PhVals ["Item_Title"] = ProductPage.GetListItemTitle (item, false);
					PhVals ["QueryString_View"] = item.ID.ToString ();
				}
				return item;
			}
		}

		public virtual bool LoadScript {
			get {
				return ProductPage.Is14;
			}
		}

		public ToolBarMenuButton MenuButton {
			get {
				if (menuButton == null)
					menuButton = FindParent<ToolBarMenuButton> (Parent);
				return menuButton;
			}
		}

		public MenuTemplate MenuTemplateControl {
			get {
				if (menuTemplate == null)
					menuTemplate = FindParent<MenuTemplate> (Parent);
				return menuTemplate;
			}
		}

		public virtual PermissionContext PermContext {
			get {
				return DispForm ? PermissionContext.CurrentItem : PermissionContext.CurrentFolder;
			}
		}

		public virtual SPBasePermissions Perms {
			get {
				string configPerms = ProductPage.Config (SpContext, "MenuPerms");
				string [] cfgPerms;
				SPBasePermissions basePerm = SPBasePermissions.ViewListItems;
				if ((!string.IsNullOrEmpty (configPerms)) && ((cfgPerms = configPerms.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (cfgPerms.Length > 0))
					foreach (string perm in cfgPerms)
						basePerm |= (SPBasePermissions) Enum.Parse (typeof (SPBasePermissions), perm, true);
				return basePerm;
			}
		}

		public string Prod {
			get {
				if (tri == null)
					tri = "rox" + ProdName.Substring (0, 3);
				return tri;
			}
		}

		public string ProdName {
			get {
				if (pn == null)
					pn = ProductPage.AssemblyName.Substring (ProductPage.AssemblyName.IndexOf ('_') + 1);
				return pn;
			}
		}

		public virtual string RibbonGroup {
			get {
				return "Actions";
			}
		}

		public virtual string RibbonPath {
			get {
				return (DispForm ? "ListForm.Display" : ((List is SPDocumentLibrary) ? "Library" : (IsCalendar ? "Calendar.Calendar" : "List")));
			}
		}

		public virtual string RootFolderUrl {
			get {
#if SP12
				string rf, tmp;
				if ((ProductPage.Is14) && !string.IsNullOrEmpty (tmp = Reflector.Current.Get (SpContext, "RootFolderUrl") + string.Empty))
					return tmp;
				Guid viewID = ((View == null) ? Guid.Empty : View.ID), urlViewID = ProductPage.GetGuid (Context.Request.QueryString ["View"], true);
				if ((viewID != Guid.Empty) && (viewID == urlViewID) && (!string.IsNullOrEmpty (rf = Context.Request.QueryString ["RootFolder"])) && (rf != "*"))
					return rf;
				return List.RootFolder.ServerRelativeUrl;
#else
				return SpContext.RootFolderUrl;
#endif
			}
		}

		public string SchemaName {
			get {
				if (string.IsNullOrEmpty (schemaName))
					schemaName = ProductPage.GetProductResource ("__MenuActionDefaultSchema");
				return schemaName;
			}
		}

		public virtual string SmallIconUrl {
			get {
				if (string.IsNullOrEmpty (iconUrlSmall))
					iconUrlSmall = ProductPage.GetProductResource ("__MenuActionIconSmall");
				return iconUrlSmall;
			}
		}

		public SPContext SpContext {
			get {
				return ((MenuButton != null) ? MenuButton.RenderContext : SPContext.Current);
			}
		}

		public virtual SPView View {
			get {
				if (view == null) {
					if (MenuButton != null)
						view = menuButton.View;
					if (view != null) {
						PhVals ["QueryString_View"] = ProductPage.GuidBracedUpper (view.ID);
						if ((List != null) && (ListItem == null))
							PhVals ["Context_Title"] = list.Title + ((string.IsNullOrEmpty (list.Title) || string.IsNullOrEmpty (view.Title)) ? string.Empty : " - ") + view.Title;
						PhVals ["View_Title"] = view.Title;
					}
				}
				return view;
			}
		}

		public virtual Guid ViewID {
			get {
				if ((MenuButton != null) && !Guid.Empty.Equals (menuButton.ViewId)) {
					PhVals ["QueryString_View"] = ProductPage.GuidBracedUpper (menuButton.ViewId);
					return menuButton.ViewId;
				}
				if (View != null)
					return view.ID;
				return Guid.Empty;
			}
		}

		public virtual SystemWebPart WebPart {
			get {
				if ((webPart == null) && ((webPart = FindParent<SystemWebPart> (Parent)) != null)) {
					PhVals ["WebPart_Title"] = webPart.DisplayTitle;
					if ((ListItem == null) && (View == null))
						PhVals ["Context_Title"] = webPart.DisplayTitle;
				}
				return webPart;
			}
		}

		public List<string> WebPartIDs {
			get {
				string key = ProductPage.AssemblyName + "_ribbonwpids";
				List<string> list = Context.Items [key] as List<string>;
				if (list == null)
					Context.Items [key] = list = new List<string> ();
				if ((WebPart != null) && !list.Contains (WebPart.ID))
					list.Add (WebPart.ID);
				return list;
			}
		}

	}

}
