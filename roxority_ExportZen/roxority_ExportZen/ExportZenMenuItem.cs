
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
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

using SystemWebPart = System.Web.UI.WebControls.WebParts.WebPart;
using TahoeWebPart = Microsoft.SharePoint.WebPartPages.WebPart;

namespace roxority_ExportZen {

	public class ExportZenMenuItem : ZenMenuItem {

		private Dictionary<object, bool> unixes = new Dictionary<object, bool> ();
		private Dictionary<object, string> seps = new Dictionary<object, string> ();

		public ExportZenMenuItem () {
			actionPropPrefix = "ExportAction_";
			baseSequence = "200";
		}

		protected override string GetActionUrl (IDictionary inst, SPWeb thisWeb, bool useView, bool includeFilters, List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters, string fj, bool flag) {
			return ("/_layouts/" + ProductPage.AssemblyName + "/expo.aspx?sep=" + Context.Server.UrlEncode (seps [inst ["id"]]) + "&unix=" + (unixes [inst ["id"]] ? 1 : 0) + "&rule=" + inst ["id"] + "&exportlist=" + List.ID + (useView ? ("&lv=" + Context.Server.UrlEncode (MenuButton.ViewId + string.Empty)) : string.Empty) + (((!includeFilters) || (effectiveFilters.Count == 0)) ? string.Empty : ("&f=" + Context.Server.UrlEncode (JSON.JsonEncode (effectiveFilters)) + "&fj=" + Context.Server.UrlEncode (fj))) + "&dt=" + DateTime.Now.Ticks);
		}

		protected override void OnActionsCreated (int cmdCount) {
			Control ctl;
			if (((ProductPage.Config (null, "HideAction") == "always") || ((ProductPage.Config (null, "HideAction") == "auto") && (cmdCount > 0))) && ((ctl = MenuTemplateControl.FindControl ("ExportToSpreadsheet")) != null))
				ctl.Visible = false;
		}

		protected override void ValidateInstance (IDictionary inst, ref string clickScript) {
			bool unix = false;
			string acsep, sep = ",";
			if (inst ["unix"] != null)
				try {
					unix = (bool) inst ["unix"];
				} catch {
					unix = false;
				}
			if (unix && !IsLic (2)) {
				unix = false;
				clickScript = "alert(\'" + SPEncode.ScriptEncode (ProductPage.GetResource ("NopeEd", ProductPage.GetProductResource ("PC_" + SchemaName + "_unix"), "Basic")) + "\');";
			}
			if (!string.IsNullOrEmpty (acsep = inst ["sep"] + string.Empty)) {
				if (acsep == "s")
					sep = ";";
				else if (acsep == "t")
					sep = "\t";
			} else if (inst ["excel"] != null)
				try {
					if ((bool) inst ["excel"])
						sep = ";";
				} catch {
				}
			if ((sep != ",") && !IsLic (2)) {
				sep = ",";
				clickScript = "alert(\'" + SPEncode.ScriptEncode (ProductPage.GetResource ("NopeEd", ProductPage.GetProductResource ("PC_" + SchemaName + "_sep"), "Basic")) + "\');";
			}
			seps [inst ["id"]] = sep;
			unixes [inst ["id"]] = unix;
		}

		public string GetPeopleClickScript (IDictionary inst, string webPageUrl, TahoeWebPart webPart, List<object []> filters, List<string> andFilters, Dictionary<string, string> oobFilterPairs) {
			return GetRollupClickScript (inst, webPageUrl, webPart, filters, andFilters, oobFilterPairs);
		}

		public string GetRollupClickScript (IDictionary inst, string webPageUrl, TahoeWebPart webPart, List<object []> filters, List<string> andFilters, Dictionary<string, string> oobFilterPairs) {
			string pageUrl = string.Empty, query = string.Empty, jop = "&rpzopt=' + encodeURI(JSON.stringify(roxLastOps['" + webPart.ID + "'][1]))";
			SortedDictionary<string, string> qs = new SortedDictionary<string, string> ();
			qs ["rule"] = inst ["id"] + string.Empty;
			qs ["exportlist"] = webPart.ID;
			qs ["View"] = HttpUtility.UrlEncode (webPageUrl);
			qs ["t"] = HttpUtility.UrlEncode (webPart.Title);
			qs ["r"] = DateTime.Now.Ticks.ToString ();
			foreach (KeyValuePair<string, string> kvp in qs)
				query += ((string.IsNullOrEmpty (query) ? "?" : "&") + kvp.Key + "=" + kvp.Value);
			pageUrl = SPContext.Current.Web.Url.TrimEnd ('/') + "/_layouts/" + ProductPage.AssemblyName + "/expo.aspx" + query;
			return (IsLic (2) ? 2 : 0) + "location.href='" + pageUrl + jop + ";";
		}

	}

	public class ExportZenActionsMenu : ActionsMenu {

		public static void RemoveLegacyTemplate () {
			try {
				ProductPage.Elevate (delegate () {
					string ctlPath = null;
					SPSecurity.CatchAccessDeniedException = false;
					ProductPage.GetSite (ProductPage.GetContext ());
					try {
						ctlPath = HttpContext.Current.Server.MapPath ("/_controltemplates/roxority_ExportZen_MenuItem.ascx");
					} catch {
					}
					if (string.IsNullOrEmpty (ctlPath))
						ctlPath = Path.Combine (ProductPage.HivePath, @"TEMPLATE\CONTROLTEMPLATES\roxority_ExportZen_MenuItem.ascx");
					if (File.Exists (ctlPath))
						File.Delete (ctlPath);
				}, true, true);
			} catch {
			}
		}

		public ExportZenActionsMenu () {
		}

	}

}
