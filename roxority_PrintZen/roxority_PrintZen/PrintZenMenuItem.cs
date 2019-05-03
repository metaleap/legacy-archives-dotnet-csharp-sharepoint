
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using roxority.Shared;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

using SystemWebPart = System.Web.UI.WebControls.WebParts.WebPart;
using TahoeWebPart = Microsoft.SharePoint.WebPartPages.WebPart;

namespace roxority_PrintZen {

	public class PrintZenMenuItem : ZenMenuItem {

		public static readonly string [] CalendarParams = { "CalendarDate", "CalendarPeriod" };
		public static readonly string [] FolderParams = { "RootFolder", "FolderCTID" };
		public static readonly string [] SortParams = { "SortField", "SortDir" };
		public static readonly string [] PageParams = { "Paged", "PagedPrev", "PageFirstRow", "PageLastRow" };

		internal static bool GetBool (IDictionary inst, string name) {
			object obj = inst [name];
			return ((obj is bool) && (bool) obj);
		}

		internal static IEnumerable<string> GetAllPageParams (HttpContext context, bool excludeEmpties, string include) {
			List<string> list = new List<string> ();
			foreach (string p in PageParams)
				if (((!excludeEmpties) || !string.IsNullOrEmpty (context.Request.QueryString [p])) && !list.Contains (p))
					list.Add (p);
			foreach (string p in context.Request.QueryString.AllKeys)
				if ((p != null) && p.StartsWith ("p_") && ((!excludeEmpties) || !string.IsNullOrEmpty (context.Request.QueryString [p])) && !list.Contains (p))
					list.Add (p);
			if ((!string.IsNullOrEmpty (include)) && !list.Contains (include))
				list.Add (include);
			return list;
		}

		public static string GetActionUrl (HttpContext context, IDictionary inst, SPWeb thisWeb, bool useView, bool includeFilters, List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters, string fj, bool dispForm, SPList list, SPListItem listItem, Guid viewID, IEnumerable<string> allPageParams, Control parent, bool origPage) {
			string query = string.Empty;
			SortedDictionary<string, string> qs = new SortedDictionary<string, string> ();
			SystemWebPart wp = null;
			WebPartManager wpMan;
			Converter<KeyValuePair<string, int>, string> trySubstr = delegate (KeyValuePair<string, int> kvp) {
				try {
					return kvp.Key.Substring (kvp.Value);
				} catch {
				}
				return kvp.Key;
			};
			if (origPage || (((parent is WebPartZoneBase) || (parent is Page)) && Guid.Empty.Equals (viewID)))
				return trySubstr (new KeyValuePair<string, int> (context.Request.Url.ToString (), SPContext.Current.Web.Url.TrimEnd ('/').Length)) + (string.IsNullOrEmpty (context.Request.Url.Query) ? "?" : "&") + "roxPrintZen=" + ((parent is WebPartZoneBase) ? parent.ID : (Guid.Empty.Equals (viewID) ? "__roxPage" : ("g_" + ProductPage.GuidLower (viewID, true).Replace ('-', '_'))));
			qs ["a"] = inst ["id"] + string.Empty;
			qs ["l"] = ((list == null) ? HttpUtility.UrlEncode (context.Request.Url.ToString ()) : ProductPage.GuidLower (list.ID, true));
			if (list == null) {
				qs ["wpid"] = "g_" + ProductPage.GuidLower (viewID, true).Replace ('-', '_');
				if ((parent != null) && ((wpMan = WebPartManager.GetCurrentWebPartManager (parent.Page)) != null))
					wp = wpMan.WebParts [qs ["wpid"]];
			}
			if (dispForm)
				qs ["View"] = listItem.ID.ToString ();
			else if (useView)
				qs ["View"] = ProductPage.GuidBracedUpper (viewID);
			if (includeFilters && (effectiveFilters.Count > 0))
				qs ["fs"] = context.Server.UrlEncode (JSON.JsonEncode (effectiveFilters));
			if (!string.IsNullOrEmpty (fj))
				qs ["fj"] = context.Server.UrlEncode (fj);
			if (!(inst ["f"] + string.Empty).StartsWith ("Recursive"))
				foreach (string pname in FolderParams)
					if (!string.IsNullOrEmpty (context.Request.QueryString [pname]))
						qs [pname] = context.Server.UrlEncode (context.Request.QueryString [pname]);
			if (GetBool (inst, "s"))
				foreach (string pname in SortParams)
					if (!string.IsNullOrEmpty (context.Request.QueryString [pname]))
						qs [pname] = context.Server.UrlEncode (context.Request.QueryString [pname]);
			foreach (string pname in CalendarParams)
				if (!string.IsNullOrEmpty (context.Request.QueryString [pname]))
					qs [pname] = context.Server.UrlEncode (context.Request.QueryString [pname]);
			if (GetBool (inst, "p"))
				foreach (string pname in allPageParams)
					qs [pname] = context.Server.UrlEncode (context.Request.QueryString [pname]);
			qs ["r"] = DateTime.Now.Ticks.ToString ();
			if ((!dispForm) && ((wp != null) || ((wp = FindParent<SystemWebPart> (parent)) != null)) && !string.IsNullOrEmpty (wp.DisplayTitle))
				qs ["t"] = HttpUtility.UrlEncode (wp.DisplayTitle);
			foreach (KeyValuePair<string, string> kvp in qs)
				query += ((string.IsNullOrEmpty (query) ? "?" : "&") + kvp.Key + "=" + kvp.Value);
			return ("/_layouts/" + ProductPage.AssemblyName + "/prnt.aspx" + query);
		}

		public static string GetClickScript (HttpContext context, string callerID, string siteUrl, string clickScript, IDictionary inst, SPWeb thisWeb, bool useView, bool includeFilters, List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters, string fj, bool forceScript, bool dispForm, SPList list, SPListItem listItem, Guid viewID, IEnumerable<string>allPageParams, Control parent, bool origPage) {
			string pageUrl;
			pageUrl = siteUrl + GetActionUrl (context, inst, thisWeb, useView, includeFilters, effectiveFilters, fj, dispForm, list, listItem, viewID, allPageParams, parent, origPage);
			if (ProductPage.Is14 && "o".Equals (inst ["t"]))
				return "OpenPopUpPage('" + pageUrl + "&roxDlg=1&roxDlgShow=" + (JsonSchemaManager.Bool (inst ["dp"], true) ? "1" : "0") + "');";
			if (!"w".Equals (inst ["t"]))
				return "window.open('" + pageUrl + "&roxDlgShow=" + (JsonSchemaManager.Bool (inst ["dp"], true) ? "1" : "0") + "', '" + callerID + "', '" + SPEncode.ScriptEncode (ProductPage.Config (ProductPage.GetContext (), "Popup")) + "');";
			return forceScript ? ("location.href='" + pageUrl + "&roxDlgShow=" + (JsonSchemaManager.Bool (inst ["dp"], true) ? "1" : "0") + "';") : string.Empty;
		}

		public static string GetRollupClickScriptStatic (IDictionary inst, string webPageUrl, TahoeWebPart webPart, List<object []> filters, List<string> andFilters, Dictionary<string, string> oobFilterPairs) {
			string pageUrl = string.Empty, query = string.Empty, jop = "&roxDlgShow=" + (JsonSchemaManager.Bool (inst ["dp"], true) ? "1" : "0") + "&rpzopt=' + (JSON.stringify(roxLastOps['" + webPart.ID + "'][1]))";
			SortedDictionary<string, string> qs = new SortedDictionary<string, string> ();
			qs ["a"] = inst ["id"] + string.Empty;
			qs ["l"] = webPart.ID;
			qs ["View"] = HttpUtility.UrlEncode (webPageUrl);
			qs ["r"] = DateTime.Now.Ticks.ToString ();
			qs ["t"] = HttpUtility.UrlEncode (webPart.Title);
			foreach (KeyValuePair<string, string> kvp in qs)
				query += ((string.IsNullOrEmpty (query) ? "?" : "&") + kvp.Key + "=" + kvp.Value);
			pageUrl = SPContext.Current.Web.Url.TrimEnd ('/') + "/_layouts/" + ProductPage.AssemblyName + "/prnt.aspx" + query;
			if (ProductPage.Is14 && "o".Equals (inst ["t"]))
				return (IsLic (2) ? 2 : 0) + "OpenPopUpPage('" + pageUrl + jop + " + '&roxDlg=1');";
			if (!"w".Equals (inst ["t"]))
				return (IsLic (2) ? 2 : 0) + "window.open('" + pageUrl + jop + ", '" + webPart.ID + "', '" + SPEncode.ScriptEncode (ProductPage.Config (ProductPage.GetContext (), "Popup")) + "');";
			return (IsLic (2) ? 2 : 0) + "location.href='" + pageUrl + jop + ";";
		}

		public PrintZenMenuItem () {
			actionPropPrefix = "PrintAction_";
		}

		protected override string GetActionUrl (IDictionary inst, SPWeb thisWeb, bool useView, bool includeFilters, List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters, string fj, bool origPage) {
			return GetActionUrl (Context, inst, thisWeb, useView, includeFilters, effectiveFilters, fj, DispForm, List, ListItem, ViewID, AllPageParams, Parent, origPage);
		}

		protected override string GetClickScript (string siteUrl, string clickScript, IDictionary inst, SPWeb thisWeb, bool useView, bool includeFilters, List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters, string fj, bool origPage) {
			return GetClickScript (Context, ID, siteUrl, clickScript, inst, thisWeb, useView, includeFilters, effectiveFilters, fj, false, DispForm, List, ListItem, ViewID, AllPageParams, Parent, origPage);
		}

		protected override bool GetFlag (IDictionary inst) {
			bool origPage;
			PrintZenSiteMenuItem.IsPartSupported (inst, WebPart, false, IsDispForm ? null : List, out origPage);
			return origPage;
		}

		protected override bool IsActionSupported (IDictionary inst) {
			bool tmp;
			return PrintZenSiteMenuItem.IsPartSupported (inst, WebPart, false, List, out tmp);
		}

		protected override bool IsDispFormOnly (IDictionary inst) {
			string tmp = ((inst == null) ? string.Empty : (inst ["m"] + string.Empty));
			return base.IsDispFormSupported (inst) && "d".Equals (tmp, StringComparison.InvariantCultureIgnoreCase);
		}

		protected override bool IsDispFormSupported (System.Collections.IDictionary inst) {
			string tmp = ((inst == null) ? string.Empty : (inst ["m"] + string.Empty));
			return base.IsDispFormSupported (inst) && (!string.IsNullOrEmpty (tmp)) && !"v".Equals (tmp, StringComparison.InvariantCultureIgnoreCase);
		}

		public string GetPeopleClickScript (IDictionary inst, string webPageUrl, TahoeWebPart webPart, List<object []> filters, List<string> andFilters, Dictionary<string, string> oobFilterPairs) {
			return GetRollupClickScript (inst, webPageUrl, webPart, filters, andFilters, oobFilterPairs);
		}

		public string GetRollupClickScript (IDictionary inst, string webPageUrl, TahoeWebPart webPart, List<object []> filters, List<string> andFilters, Dictionary<string, string> oobFilterPairs) {
			return GetRollupClickScriptStatic (inst, webPageUrl, webPart, filters, andFilters, oobFilterPairs);
		}

		internal override int Vl {
			get {
				return 0;
			}
		}

		public IEnumerable<string> AllPageParams {
			get {
				return GetAllPageParams (Context, true, null);
			}
		}

		public override bool DispFormSupported {
			get {
				return IsLic (4);
			}
		}

		public override string RibbonGroup {
			get {
				return (DispForm ? "Actions" : (IsCalendar ? "CustomViews" : "ViewFormat"));
			}
		}

	}

}
