
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using roxority.Shared;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using TahoeWebPart = Microsoft.SharePoint.WebPartPages.WebPart;
using SystemWebPart = System.Web.UI.WebControls.WebParts.WebPart;

namespace roxority_UploadZen {

	public class UploadControl : UserControl {

		private static Dictionary<string, string> fileIcons = null;

		public readonly Random Rnd = new Random ();

		public DropDownList ActionDropDown, FolderDropDown, LibDropDown, WebDropDown;

		internal roxority_UploadWebPart webPart = null;
		internal IDictionary action = null, altAction = null;
		internal UploadZenMenuItem menuItem;

		private bool eventsSet = false;
		private IEnumerable<IDictionary> instances = null;
		private Hashtable ht = null;
		private ArrayList ff = null;
		private int numInputs = 0;
		private string listID = null;
		private string targetWebUrl = null, webUrl = null;

		internal static IEnumerable<KeyValuePair<string, string>> GetFolders (SPFolder owner, bool includeRoot, int indent, string [] excludeNames) {
			string prefix = string.Empty;
			int len = (indent + (includeRoot ? 1 : 0));
			IEnumerable<KeyValuePair<string, string>> subs;
			if (includeRoot)
				yield return new KeyValuePair<string, string> (owner.ServerRelativeUrl, ProductPage.GetProductResource ("Uploader_RootFolder", owner.Name));
			for (int i = 0; i < len; i++)
				prefix += "- ";
			foreach (SPFolder folder in ProductPage.TryEach<SPFolder> (owner.SubFolders))
				if (Array.IndexOf<string> (excludeNames, folder.Name) < 0) {
					yield return new KeyValuePair<string, string> (folder.ServerRelativeUrl, prefix + folder.Name);
					if ((subs = GetFolders (folder, false, indent + (includeRoot ? 2 : 1), excludeNames)) != null)
						foreach (KeyValuePair<string, string> kvp in subs)
							yield return kvp;
				}
		}

		internal static IEnumerable<KeyValuePair<Guid, string>> GetWebs (SPWeb owner, bool includeRoot, int indent, JsonSchemaManager.Property.Type.WebSet.Config webSet) {
			string prefix = string.Empty;
			int len = (indent + (includeRoot ? 1 : 0));
			IEnumerable<KeyValuePair<Guid, string>> subs;
			if (includeRoot)
				yield return new KeyValuePair<Guid, string> (owner.ID, owner.Title);
			for (int i = 0; i < len; i++)
				prefix += "- ";
			foreach (SPWeb web in ProductPage.TryEach<SPWeb> (owner.Webs)) {
				if (webSet.IsMatch (web))
					yield return new KeyValuePair<Guid, string> (web.ID, prefix + web.Title);
				if ((subs = GetWebs (web, false, indent + (includeRoot ? 2 : 1), webSet)) != null)
					foreach (KeyValuePair<Guid, string> kvp in subs)
						yield return kvp;
			}
		}

		internal void FolderDropDown_SelectedIndexChanged (object sender, EventArgs e) {
			if (webPart != null)
				webPart.library = null;
		}

		internal void LibDropDown_SelectedIndexChanged (object sender, EventArgs e) {
			if (webPart != null)
				webPart.library = null;
			if (FolderDropDown != null) {
				FolderDropDown.Items.Clear ();
				RefreshControls ();
			}
		}

		internal void WebDropDown_SelectedIndexChanged (object sender, EventArgs e) {
			bool hasLib = (LibDropDown != null), hasFolder = (FolderDropDown != null);
			if (webPart != null)
				webPart.library = null;
			if (hasLib)
				LibDropDown.Items.Clear ();
			if (hasFolder)
				FolderDropDown.Items.Clear ();
			if (hasLib || hasFolder)
				RefreshControls ();
		}

		protected override void CreateChildControls () {
			RefreshControls ();
			base.CreateChildControls ();
		}

		protected string GetUplScript () {
			return ProductPage.LicEdition (ProductPage.GetContext (), (IDictionary) null, 4) ? ("roxUps['" + ClientID + "'].doUpload();") : ("alert('" + SPEncode.ScriptEncode (ProductPage.GetResource ("NopeEd", "UploadZen Web Part", "Ultimate")) + "');");
		}

		public bool Bool (string name, bool def) {
			return UploadZenMenuItem.GetBool (Action, name, def);
		}

		public void RefreshControls () {
			object tmp;
			string key;
			Guid webID, listID;
			ListItem item;
			SPList list = null;
			JsonSchemaManager.Property.Type.LibSet.Config libSet;
			if (Part != null) {
				tmp = Part.Library;
				if (Part.AllowSelectAction && (ActionDropDown != null) && (ActionDropDown.Items.Count == 0))
					foreach (IDictionary inst in Instances)
						ActionDropDown.Items.Add (new ListItem (JsonSchemaManager.GetDisplayName (inst, "UploadActions", false) + ":", inst ["id"] + string.Empty));
				if (Part.AllowSelectLibSite && (WebDropDown != null) && (WebDropDown.Items.Count == 0))
					foreach (KeyValuePair<Guid, string> kvp in GetWebs (SPContext.Current.Site.RootWeb, true, 0, new JsonSchemaManager.Property.Type.WebSet.Config (Action ["webs"]))) {
						WebDropDown.Items.Add (item = new ListItem (kvp.Value, ProductPage.GuidLower (kvp.Key, true)));
						if (kvp.Key.Equals (((webPart.extWeb == null) ? SPContext.Current.Web : webPart.extWeb).ID))
							item.Selected = true;
					}
				if ((Part.AllowSelectLibWeb || Part.AllowSelectLibSite || Part.AllowSelectLibPage) && (LibDropDown != null) && (LibDropDown.Items.Count == 0)) {
					if (Part.AllowSelectLibWeb || Part.AllowSelectLibSite) {
						libSet = new JsonSchemaManager.Property.Type.LibSet.Config (Action ["show"]);
						using (SPWeb web = SPContext.Current.Site.OpenWeb (((WebDropDown != null) && (!Guid.Empty.Equals (webID = ProductPage.GetGuid (WebDropDown.SelectedValue, true)))) ? webID : SPContext.Current.Web.ID))
							foreach (SPList lib in ProductPage.TryEach<SPList> (web.Lists))
								if ((lib is SPDocumentLibrary) && (libSet.IsMatch (lib, webPart.JsonFarmMan) || libSet.IsMatch (lib, webPart.JsonSiteMan)))
									LibDropDown.Items.Add (new ListItem (lib.Title, ProductPage.GuidLower (lib.ID, true)));
					} else if (Part.Library != null)
						foreach (KeyValuePair<SystemWebPart, SPDocumentLibrary> kvp in Part.libParts)
							if (LibDropDown.Items.FindByValue (key = ProductPage.GuidLower (kvp.Value.ID, true)) == null)
								LibDropDown.Items.Add (new ListItem (kvp.Value.Title, key));
					if (Part.Library != null)
						try {
							LibDropDown.SelectedValue = ProductPage.GuidLower (Part.Library.ID, true);
						} catch {
						}
				}
				if (Part.AllowSelectFolder && (FolderDropDown != null) && (FolderDropDown.Items.Count == 0)) {
					if ((Part.AllowSelectLibWeb || Part.AllowSelectLibSite || Part.AllowSelectLibPage) && (LibDropDown != null) && !Guid.Empty.Equals (listID = ProductPage.GetGuid (LibDropDown.SelectedValue, true)))
						using (SPWeb web = SPContext.Current.Site.OpenWeb (((WebDropDown != null) && (!Guid.Empty.Equals (webID = ProductPage.GetGuid (WebDropDown.SelectedValue, true)))) ? webID : SPContext.Current.Web.ID))
							list = web.Lists [listID];
					else if (Part.Library != null)
						list = Part.Library;
					if (list != null)
						foreach (KeyValuePair<string, string> kvp in GetFolders (list.RootFolder, true, 0, Part.ExcludeFolderNames.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)))
							FolderDropDown.Items.Add (item = new ListItem (kvp.Value, kvp.Key));
					if (Part.folder != null)
						try {
							FolderDropDown.SelectedValue = Part.folder.ServerRelativeUrl;
						} catch {
						}
				}
				if (!eventsSet) {
					eventsSet = true;
					if ((FolderDropDown != null) && (FolderDropDown.Items.Count > 0))
						FolderDropDown.SelectedIndexChanged += FolderDropDown_SelectedIndexChanged;
					if ((LibDropDown != null) && (LibDropDown.Items.Count > 0))
						LibDropDown.SelectedIndexChanged += LibDropDown_SelectedIndexChanged;
					if ((WebDropDown != null) && (WebDropDown.Items.Count > 0))
						WebDropDown.SelectedIndexChanged += WebDropDown_SelectedIndexChanged;
				}
			}
		}

		public IDictionary Action {
			get {
				string actionID;
				IDictionary inst = null, defInst = null;
				if ((Part != null) && ((altAction == null) || ((ActionDropDown != null) && (ActionDropDown.SelectedValue != (altAction ["id"] + string.Empty))))) {
					if (Part.AllowSelectAction && (ActionDropDown != null) && !string.IsNullOrEmpty (ActionDropDown.SelectedValue))
						actionID = ActionDropDown.SelectedValue;
					else
						actionID = Part.UploadAction;
					foreach (IDictionary dict in Instances)
						if ("default".Equals (dict ["id"]))
							defInst = dict;
						else if (actionID.Equals (dict ["id"]))
							inst = dict;
					if ((inst == null) && (Part.UploadAction != actionID) && !string.IsNullOrEmpty (Part.UploadAction))
						foreach (IDictionary dict in Instances)
							if (Part.UploadAction.Equals (dict ["id"]))
								inst = dict;
					altAction = ((inst == null) ? defInst : inst);
				}
				return ((altAction == null) ? action : altAction);
			}
		}

		public bool CanDragDrop {
			get {
				return Bool ("nsd", false) && !"f".Equals (Action ["np"] + string.Empty, StringComparison.InvariantCultureIgnoreCase);
			}
		}

		public string ClickOnceUrl {
			get {
				return MenuItem.GetActionUrl (Action, SPContext.Current.Web, false, false, null, null, true, false);
			}
		}

		public ArrayList ExtFilters {
			get {
				string [] f, s;
				int pos, pos2;
				Hashtable ht;
				if (ff == null) {
					ff = new ArrayList ();
					f = ProductPage.Config (ProductPage.GetContext (), "FileFilters").Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					s = (Action ["nff"] + string.Empty).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					foreach (string v in s)
						if (((pos = Array.FindIndex<string> (f, (sv) => {
							return sv.EndsWith (v);
						})) >= 0) && ((pos2 = f [pos].IndexOf (':')) > 0)) {
							ht = new Hashtable ();
							ht ["title"] = f [pos].Substring (0, pos2).Trim ();
							ht ["extensions"] = v;
							ff.Add (ht);
						}
				}
				return ((ff.Count == 0) ? null : ff);
			}
		}

		public Dictionary<string, string> FileIcons {
			get {
				string imgDir;
				Dictionary<string, string> icons;
				HttpContext context;
				if (((fileIcons == null) || (fileIcons.Count == 0)) && ((context = HttpContext.Current) != null) && Directory.Exists (imgDir = context.Server.MapPath ("/_layouts/images"))) {
					icons = new Dictionary<string, string> ();
					foreach (string ext in new string [] { "gif", "png" })
						foreach (string fn in Directory.GetFiles (imgDir, "ic*." + ext, SearchOption.AllDirectories))
							icons [Path.GetFileNameWithoutExtension (fn).Substring (2).ToLowerInvariant ()] = "/_layouts/images" + fn.Substring (imgDir.TrimEnd ('\\').Length).Replace ('\\', '/');
					fileIcons = icons;
				}
				return fileIcons;
			}
		}

		public int Flags {
			get {
				return UploadZenMenuItem.IsLic (2) ? 0 : 8;
			}
		}

		public bool HideCheckin {
			get {
				return ProductPage.Config<bool> (ProductPage.GetContext (), "HideNoCheckIn");
			}
		}

		public override string ID {
			get {
				if (Part != null)
					return Action ["id"] + string.Empty;
				return base.ID;
			}
			set {
				base.ID = value;
			}
		}

		public Hashtable ImageResize {
			get {
				string [] nums;
				int [] vals;
				if (ht == null) {
					ht = new Hashtable (3);
					vals = new int [3];
					if (((nums = (Action ["nri"] + string.Empty).Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries)).Length >= 2) && int.TryParse (nums [0], out vals [0]) && int.TryParse (nums [1], out vals [1]) && (vals [0] > 0) && (vals [1] > 0)) {
						vals [2] = 100;
						if ((nums.Length >= 3) && (int.TryParse (nums [2], out vals [2]))) {
							if (vals [2] <= 0)
								vals [2] = 1;
							if (vals [2] > 100)
								vals [2] = 100;
						}
						ht ["width"] = vals [0];
						ht ["height"] = vals [1];
						ht ["quality"] = vals [2];
					}
				}
				return ((ht.Count == 0) ? null : ht);
			}
		}

		public int InputCount {
			get {
				string str;
				if ((numInputs == 0) && (string.IsNullOrEmpty (str = ProductPage.Config (ProductPage.GetContext (), "HtmlNumInputs")) || (!int.TryParse (str, out numInputs)) || (numInputs < 10) || (numInputs > 200)))
					numInputs = 40;
				return numInputs;
			}
		}

		public IEnumerable<IDictionary> Instances {
			get {
				return ((webPart == null) ? null : webPart.Instances);
			}
		}

		public bool IsChrome {
			get {
				return Request.UserAgent.ToLowerInvariant ().Contains ("chrome");
			}
		}

		public bool IsPart {
			get {
				return (webPart != null);
			}
		}

		public string this [string name, params object [] args] {
			get {
				return ProductPage.GetProductResource (name, args);
			}
		}

		public string ListID {
			get {
				if ((listID == null) && (MenuItem != null) && (MenuItem.List != null))
					try {
						listID = MenuItem.List.ID.ToString ();
					} catch {
					}
				return listID;
			}
		}

		public UploadZenMenuItem MenuItem {
			get {
				return menuItem;
			}
		}

		public roxority_UploadWebPart Part {
			get {
				return webPart;
			}
		}

		public string RootFolderUrl {
			get {
				string fp = MenuItem.RootFolderUrl;
				Guid guid;
				SPList lib;
				if ((webPart != null) && (webPart.Library != null) && !string.IsNullOrEmpty (webPart.EffectiveFolder))
					fp = webPart.EffectiveFolder;
				if (ProductPage.IsGuid (fp) && (!Guid.Empty.Equals (guid = ProductPage.GetGuid (fp, true))) && ((lib = TargetWeb.Lists [guid]) != null))
					fp = lib.RootFolder.Url;
				return HttpUtility.UrlDecode (fp).TrimStart ('/');
			}
		}

		public bool RuntimeBoth {
			get {
				string rt = Action ["np"] + string.Empty;
				return string.IsNullOrEmpty (rt) || (rt.Contains ("s") && rt.Contains ("f"));
			}
		}

		public bool RuntimeFlash {
			get {
				string rt = Action ["np"] + string.Empty;
				return rt.Contains ("f");
			}
		}

		public bool RuntimeSilver {
			get {
				string rt = Action ["np"] + string.Empty;
				return rt.Contains ("s");
			}
		}

		public bool ShowClickOnce {
			get {
				//	Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET4.0C; .NET4.0E; .NET CLR 3.5.30729; .NET CLR 3.0.30729)
				return (Bool ("noc", true) && (Request.UserAgent.Contains (".NET") || Request.UserAgent.Contains ("MSIE")) && Request.UserAgent.Contains ("Windows NT") /* && (!Request.UserAgent.Contains ("Windows NT 6")) && (!Request.UserAgent.Contains ("Windows NT 7")) && (!Request.UserAgent.Contains ("Windows NT 8")) && (!Request.UserAgent.Contains ("Windows NT 9")) && (!Request.UserAgent.Contains ("Windows NT 10")) */ );
			}
		}

		public bool ShowCredits {
			get {
				return Bool ("sl", false) || !ProductPage.LicEdition (ProductPage.GetContext (), (IDictionary) null, 2);
			}
		}

		public string TargetTitle {
			get {
				return MenuItem.List.Title;
			}
		}

		public SPWeb TargetWeb {
			get {
				if ((Part != null) && (Part.Library != null))
					return Part.TargetWeb;
				return MenuItem.SpContext.Web;
			}
		}

		public string TargetWebUrl {
			get {
				if (targetWebUrl == null)
					try {
						targetWebUrl = (((Part != null) && (Part.Library != null)) ? Part.TargetWeb.Url.TrimEnd ('/') : WebUrl);
					} catch {
					}
				return ((targetWebUrl == null) ? string.Empty : targetWebUrl);
			}
		}

		public string WebUrl {
			get {
				if (webUrl == null)
					try {
						webUrl = MenuItem.SpContext.Web.Url.TrimEnd ('/');
					} catch {
					}
				return ((webUrl == null) ? string.Empty : webUrl);
			}
		}

	}

}
