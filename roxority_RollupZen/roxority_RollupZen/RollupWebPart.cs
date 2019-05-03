
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.WebPartPages.Communication;
using roxority.Data;
using roxority.Data.Providers;
using roxority.Shared;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

using TahoeWebPart = Microsoft.SharePoint.WebPartPages.WebPart;

namespace roxority_RollupZen {

	[Guid ("6B7BE95D-059D-4c35-91EB-BF8823C1040C")]
	public class RollupWebPart : WebPartBase, IFilterConsumer {

#if PEOPLEZEN
#if SP12
		internal const string DEFAULT_PICTUREURL = "/_layouts/images/roxority_PeopleZen/no_pic.gif";
#else
		internal const string DEFAULT_PICTUREURL = "/_layouts/images/roxority_PeopleZen/person.gif";
#endif
#else
		internal const string DEFAULT_PICTUREURL = "/_layouts/images/blank.gif";
#endif

		public static readonly bool IsPeopleZen =
#if PEOPLEZEN
 true;
#else
 false;
#endif

		public static readonly bool IsRollupZen =
#if ROLLUPZEN
 true;
#else
 false;
#endif

		internal static readonly Random rnd = new Random ();
		internal static PropertyInfo fzHasHiddenProp = null;

		private static string sspWebUrl = null;

		[ThreadStatic]
		private static ProductPage.LicInfo l = null;

		public event FilterConsumerInitEventHandler FilterConsumerInit;

		public TextBox textArea = new TextBox ();

		internal string groupProp = string.Empty, loaderAnim = "k", message = string.Empty, props = null, sortProp = string.Empty, tabProp = string.Empty, tileWidth = "180px";
		internal bool dateAgo = true, dateForth = false, forceReload = false, groupByCounts = false, groupDesc = false, groupInteractive = false, groupInteractiveDir = false, groupShowCounts = false, sortDesc = false, tabInteractive = false;
		internal double dateInterval = 56;
		internal string dataSourceID = "default", dateProp = string.Empty, exportAction = string.Empty, printAction = string.Empty;
		internal bool allowSort = false, allowView = false, curUser = false, dateIgnoreDay = false, dateThisYear = false, filterLive = true, isConnected = false, listStyle = false, presence = false, vcard = false;
		internal int imageHeight = 0, nameMode = 1, pageMode = 1, pageSkipMode = 0, pageStepMode = 1, pageSize = 6, pictMode = IsPeopleZen ? 1 : 0, rowSize = 2;
		internal System.Web.UI.WebControls.WebParts.WebPart connectedWebPart = null;
		internal List<string> andFilters = new List<string> ();
		internal List<object []> filters = new List<object []> ();
		internal Dictionary<string, string> oobFilterPairs = new Dictionary<string, string> ();
		internal List<Exception> sortErrors = null;
		internal WebPartVerb exportVerb = null, printVerb = null;
		internal IDictionary dataInst = null, expInst = null, printInst = null;

		private DataSource dataSource = null;
		private string dsPath = null, ezPath = null, pzPath = null;
		private bool? noAjax = null;
		private string webUrl = null;

		private static ProductPage.LicInfo L {
			get {
				if (l == null)
					l = ProductPage.LicInfo.Get (null);
				return l;
			}
		}

		internal static string GetPictureUrl (HttpContext context, DataSource ds, CachedRecord cachedRecord, string siteUrl) {
			string url = (L.expired || L.broken || L.userBroken) ? (siteUrl + "/_layouts/images/" + ProductPage.AssemblyName + "/" + ProductPage.AssemblyName + ".png") : cachedRecord [DataSource.SCHEMAPROP_PREFIX + DataSource.SCHEMAPROP_PICTUREFIELD, siteUrl + DEFAULT_PICTUREURL, ds];
			if (context.Request.IsSecureConnection && url.StartsWith ("http:", StringComparison.InvariantCultureIgnoreCase) && url.StartsWith (siteUrl.TrimEnd ('/') + '/', StringComparison.InvariantCultureIgnoreCase))
				url = "https" + url.Substring (url.IndexOf (':'));
			return url;
		}

		internal static string GetProfileUrl (CachedRecord profile) {
			if (L.expired || L.broken || L.userBroken)
				return "/_layouts/" + ProductPage.AssemblyName + "/default.aspx?cfg=lic&r=" + rnd.Next ();
			else
				return profile.Url;
		}

		internal static string GetReloadScript (string opName, string textAreaID, string id, int pageSize, int pageStart, int pageMode, int pageStepMode, int pageSkipMode, bool dateThisYear, bool dateIgnoreDay, bool filterLive, string properties, bool listStyle, bool allowView, bool allowSort, string sortPropName, bool sortPropDesc, string tabPropName, string tabPropOrig, string tabValue, string groupPropName, bool groupPropDesc, bool groupByCounts, bool groupShowCounts, bool groupInt, bool groupIntDir, int rowSize, string tileWidth, int nameMode, int pictMode, bool presence, bool vcard, int imageHeight, string loaderAnim, bool tabInt, Hashtable fht, string dsid, object dynInst) {
			HttpContext context = HttpContext.Current;
			List<object []> filters = null;
			List<string> andFilters = null;
			if (fht != null) {
				filters = fht ["f"] as List<object []>;
				andFilters = fht ["fa"] as List<string>;
			}
			return opName + "(\'" + textAreaID + "\', \'" + id + "\', { \"ps\": " + pageSize + ", \"p\": " + pageStart + ", \"la\": \"" + loaderAnim + "\", \"pmo\": " + pageMode + ", \"pst\": " + pageStepMode + ", \"psk\": " + pageSkipMode + ", \"dty\": \"" + (dateThisYear ? 1 : 0) + "\", \"did\": \"" + (dateIgnoreDay ? 1 : 0) + "\", \"dsid\": \"" + SPEncode.ScriptEncode (dsid) + "\", \"fl\": \"" + (filterLive ? 1 : 0) + "\", \"pr\": \"" + SPEncode.ScriptEncode (context.Server.UrlEncode (properties)) + "\", \"ls\": \"" + (listStyle ? 1 : 0) + "\", \"v\": \"" + (allowView ? 1 : 0) + "\", \"s\": \"" + (allowSort ? 1 : 0) + "\", \"spn\": \"" + SPEncode.ScriptEncode (sortPropName) + "\", \"sd\": \"" + (sortPropDesc ? 1 : 0) + "\", \"tpn\": \"" + tabPropName + "\", \"tpo\": \"" + tabPropOrig + "\", " + ((tabValue == null) ? string.Empty : ("\"tv\": " + "\"" + SPEncode.ScriptEncode (context.Server.UrlEncode (tabValue)) + "\"" + ", ")) + "\"gpn\": \"" + groupPropName + "\", \"gd\": \"" + (groupPropDesc ? 1 : 0) + "\", \"gb\": \"" + (groupByCounts ? 1 : 0) + "\", \"gs\": \"" + (groupShowCounts ? 1 : 0) + "\", \"gi\": \"" + (groupInt ? 1 : 0) + "\", \"ti\": \"" + (tabInt ? 1 : 0) + "\", \"gid\": \"" + (groupIntDir ? 1 : 0) + "\", \"rs\": " + rowSize + ", \"t\": \"" + SPEncode.ScriptEncode (context.Server.UrlEncode (tileWidth)) + "\", \"nm\": " + nameMode + ", \"pm\": " + pictMode + ", \"on\": \"" + (presence ? 1 : 0) + "\", \"vc\": \"" + (vcard ? 1 : 0) + "\", \"ih\": " + imageHeight + ", \"f\": \"" + SPEncode.ScriptEncode (context.Server.UrlEncode (JSON.JsonEncode (filters))) + "\", \"webUrl\": \"" + SPEncode.ScriptEncode (SPContext.Current.Web.Url.TrimEnd ('/')) + "\", \"fa\": \"" + SPEncode.ScriptEncode (context.Server.UrlEncode (JSON.JsonEncode (andFilters))) + "\", \"dyn\": " + ((dynInst == null) ? "null" : ("\"" + SPEncode.ScriptEncode (context.Server.UrlEncode ((dynInst is string) ? dynInst.ToString () : JSON.JsonEncode (dynInst))) + "\"")) + " });";
		}

		internal static string Noop {
			get {
				return "#noop" + rnd.Next (0, 999999999); // + ProductPage.GuidLower (Guid.NewGuid ());
			}
		}

		internal static string SspWebUrl {
			get {
				if (sspWebUrl == null)
					sspWebUrl = ProductPage.GetSrpUrl ();
				return sspWebUrl;
			}
		}

		public static void Render (RollupWebPart wp, TextWriter tw, string textAreaID, string id, int pageSize, int pageStart, int pageMode, int pageStepMode, int pageSkipMode, bool dateThisYear, bool dateIgnoreDay, bool filterLive, string properties, bool listStyle, bool allowView, bool allowSort, string sortPropName, bool sortDesc, string tabPropName, string tabPropOrig, string tabValue, string groupPropName, bool groupDesc, bool groupByCounts, bool groupShowCounts, bool groupInt, bool groupIntDir, int rowSize, string tileWidth, int nameMode, int pictMode, bool presence, bool vcard, int imageHeight, string loaderAnim, bool tabInt, Hashtable fht, object l, string dsid, IDictionary dynInst) {
			SPContext ctx = SPContext.Current;
			Guid siteID = ctx.Site.ID;
			Guid webID = ctx.Web.ID;
			int pc = 0, lastTab = -1, thisTab, pos;
			bool isEmpty, isPaging = false, isDate;
			char theChar;
			string [] pair;
			string nameCaption = ProductPage.Config (ctx, "AltNameCaption"), linkTarget = ProductPage.Config (ctx, "LinkTarget"), propName, picUrl, profUrl, picProfUrl, trClass = "ms-alternating", temp, sec = null, secDom = string.Empty, secUser = null, secPass = null, webUrl = string.Empty;
			StringBuilder buffer = new StringBuilder (), navBuffer = new StringBuilder ();
			DateTime dtVal;
			DataSource ds = ((wp == null) ? null : wp.DataSource);
			Converter<int, string> getPagingScript = null;
			Converter<string, string> getRegroupScript = null, getRetabScript = null, getSortScript = null, getTabScript = null;
			Converter<bool, string> getGroupScript = null, getViewScript = null;
			List<object []> filters;
			List<string> friendlyProperties = new List<string> ();
			HttpContext context = HttpContext.Current;
			SPSecurity.CodeToRunElevated code;
			SortedDictionary<string, string> knownProps = null;
			List<Exception> sortErrors = new List<Exception> ();
			try {
				webUrl = ctx.Web.Url.TrimEnd ('/');
			} catch {
			}
			if (!ProductPage.isEnabled) {
				using (SPSite adminSite = ProductPage.GetAdminSite ())
					tw.Write ("<div class=\"rox-error\">" + ProductPage.GetResource ("NotEnabled", ProductPage.MergeUrlPaths (adminSite.Url, "/_layouts/" + ProductPage.AssemblyName + "/default.aspx?cfg=enable&r=" + rnd.Next ()), ProductPage.GetTitle ()) + "</div>");
				return;
			}
			if (!(l is ProductPage.LicInfo))
				l = L;
			if (fht != null) {
				if (fht ["f"] is ArrayList) {
					filters = new List<object []> ();
					foreach (object al in (ArrayList) fht ["f"])
						filters.Add ((al is ArrayList) ? (((ArrayList) al).ToArray ()) : (object []) al);
					fht ["f"] = filters;
				}
				if (fht ["fa"] is ArrayList)
					fht ["fa"] = new List<string> (((ArrayList) fht ["fa"]).ToArray (typeof (string)) as string []);
			}
			if (!ProductPage.LicEdition (ctx, L, 2)) {
				presence = listStyle = false;
				sortPropName = groupPropName = tabValue = tabPropOrig = tabPropName = string.Empty;
			}
			if (!ProductPage.LicEdition (ctx, L, 4))
				vcard = groupByCounts = groupShowCounts = groupInt = groupIntDir = tabInt = allowView = false;
			try {
				if (ds == null)
					ds = DataSource.FromID (dsid, true, true, (dynInst == null) ? null : (dynInst ["t"] as string));
				if ((wp != null) && (wp.dataSource == null))
					wp.dataSource = ds;
				code = delegate () {
					int pageCount, skipped = 0, pcount = 0;
					string lastGroupVal = null, groupVal, groupID = string.Empty, groupTitle = null, options = string.Empty, tabOpts = string.Empty, tmp;
					MailAddress addr;
					SPContext spCtx = ProductPage.GetContext ();
					buffer.Remove (0, buffer.Length);
					if (allowSort)
						if (string.IsNullOrEmpty (sortPropName))
							sortPropName = null;
					if (string.IsNullOrEmpty (groupPropName))
						groupPropName = null;
					if (string.IsNullOrEmpty (tabPropName))
						tabPropName = string.IsNullOrEmpty (tabPropOrig) ? null : tabPropOrig;
					getRegroupScript = delegate (string groupProp) {
						return GetReloadScript ("roxReloadRollup", textAreaID, id, pageSize, pageStart, pageMode, pageStepMode, pageSkipMode, dateThisYear, dateIgnoreDay, filterLive, properties, listStyle, allowView, allowSort, sortPropName, sortDesc, tabPropName, tabPropOrig, tabValue, groupProp, groupDesc, groupByCounts, groupShowCounts, groupInt, groupIntDir, rowSize, tileWidth, nameMode, pictMode, presence, vcard, imageHeight, loaderAnim, tabInt, fht, dsid, dynInst);
					};
					getRetabScript = delegate (string tabProp) {
						return GetReloadScript ("roxReloadRollup", textAreaID, id, pageSize, 0, pageMode, pageStepMode, pageSkipMode, dateThisYear, dateIgnoreDay, filterLive, properties, listStyle, allowView, allowSort, sortPropName, sortDesc, tabProp, tabPropOrig, null, groupPropName, groupDesc, groupByCounts, groupShowCounts, groupInt, groupIntDir, rowSize, tileWidth, nameMode, pictMode, presence, vcard, imageHeight, loaderAnim, tabInt, fht, dsid, dynInst);
					};
					getGroupScript = delegate (bool desc) {
						return GetReloadScript ("roxReloadRollup", textAreaID, id, pageSize, pageStart, pageMode, pageStepMode, pageSkipMode, dateThisYear, dateIgnoreDay, filterLive, properties, listStyle, allowView, allowSort, sortPropName, sortDesc, tabPropName, tabPropOrig, tabValue, groupPropName, desc, groupByCounts, groupShowCounts, groupInt, groupIntDir, rowSize, tileWidth, nameMode, pictMode, presence, vcard, imageHeight, loaderAnim, tabInt, fht, dsid, dynInst);
					};
					getPagingScript = delegate (int ps) {
						return GetReloadScript ("roxReloadRollup", textAreaID, id, pageSize, ps, pageMode, pageStepMode, pageSkipMode, dateThisYear, dateIgnoreDay, filterLive, properties, listStyle, allowView, allowSort, sortPropName, sortDesc, tabPropName, tabPropOrig, tabValue, groupPropName, groupDesc, groupByCounts, groupShowCounts, groupInt, groupIntDir, rowSize, tileWidth, nameMode, pictMode, presence, vcard, imageHeight, loaderAnim, tabInt, fht, dsid, dynInst);
					};
					getSortScript = delegate (string spn) {
						return GetReloadScript ("roxReloadRollup", textAreaID, id, pageSize, pageStart, pageMode, pageStepMode, pageSkipMode, dateThisYear, dateIgnoreDay, filterLive, properties, listStyle, allowView, allowSort, spn.Substring (1), spn [0] == '-', tabPropName, tabPropOrig, tabValue, groupPropName, groupDesc, groupByCounts, groupShowCounts, groupInt, groupIntDir, rowSize, tileWidth, nameMode, pictMode, presence, vcard, imageHeight, loaderAnim, tabInt, fht, dsid, dynInst);
					};
					getTabScript = delegate (string tv) {
						return GetReloadScript ("roxReloadRollup", textAreaID, id, pageSize, 0, pageMode, pageStepMode, pageSkipMode, dateThisYear, dateIgnoreDay, filterLive, properties, listStyle, allowView, allowSort, sortPropName, sortDesc, tabPropName, tabPropOrig, tv, groupPropName, groupDesc, groupByCounts, groupShowCounts, groupInt, groupIntDir, rowSize, tileWidth, nameMode, pictMode, presence, vcard, imageHeight, loaderAnim, tabInt, fht, dsid, dynInst);
					};
					getViewScript = delegate (bool ls) {
						return GetReloadScript ("roxReloadRollup", textAreaID, id, pageSize, pageStart, pageMode, pageStepMode, pageSkipMode, dateThisYear, dateIgnoreDay, filterLive, properties, ls, allowView, allowSort, sortPropName, sortDesc, tabPropName, tabPropOrig, tabValue, groupPropName, groupDesc, groupByCounts, groupShowCounts, groupInt, groupIntDir, rowSize, tileWidth, nameMode, pictMode, presence, vcard, imageHeight, loaderAnim, tabInt, fht, dsid, dynInst);
					};
					using (StringWriter writer = new StringWriter (buffer))
					using (SPSite site = new SPSite (siteID))
					using (SPWeb web = site.OpenWeb (webID))
					using (DataSourceConsumer consumer = new DataSourceConsumer (pageSize, pageStart, dateThisYear, dateIgnoreDay, properties, sortPropName, string.IsNullOrEmpty (sortPropName) ? null : (object) sortDesc, tabPropName, tabValue, groupPropName, string.IsNullOrEmpty (groupPropName) ? null : (object) groupDesc, groupByCounts, groupShowCounts, web, dsid, dynInst, fht, l, sortErrors)) {
						if (ds == null)
							ds = consumer.DataSource;
						if (wp != null)
							wp.dataSource = consumer.DataSource;
						knownProps = new SortedDictionary<string, string> ();
						foreach (RecordProperty rp in consumer.DataSource.Properties)
							knownProps [rp.Name] = rp.DisplayName;
						foreach (KeyValuePair<string, string> kvp in knownProps) {
							if (((kvp.Key == tabPropName) || (kvp.Key == tabPropOrig) || (kvp.Key == groupPropName)) && !friendlyProperties.Contains (kvp.Key))
								friendlyProperties.Add (kvp.Key);
							if (groupInt)
								options += ("<option value=\"" + context.Server.HtmlEncode (kvp.Key) + "\"" + (kvp.Key.Equals (groupPropName) ? " selected=\"selected\"" : string.Empty) + ">" + context.Server.HtmlEncode (kvp.Value) + "</option>");
						}
						foreach (string propLine in properties.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
							if (((pair = propLine.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pair.Length >= 1) && (!friendlyProperties.Contains (pair [0])) && !friendlyProperties.Contains (consumer.DataSource.RewritePropertyName (pair [0])))
								friendlyProperties.Add (pair [0]);
						writer.Write ("<div class=\"rox-rollupitems\">");
#if !WSS
						if ((consumer.DataSource is UserProfiles) && (consumer.totalCount == 0))
							throw new Exception (ProductPage.GetProductResource ("NoProfiles", SspWebUrl, ProductPage.Is14 ? ("ManageUserProfileServiceApplication.aspx?ApplicationID=" + consumer.DataSource.ContextID) : "ProfMain.aspx"));
#endif
						if (consumer.tabs != null) {
							writer.Write ("<div class=\"rox-tabstrip\">");
							if (tabInt && !string.IsNullOrEmpty (tabPropName)) {
								writer.Write ("<div class=\"rox-tabprop\"><select onchange=\"" + HttpUtility.HtmlAttributeEncode (getRetabScript ("\" + this.options[this.selectedIndex].value + \"")) + "\">");
								foreach (string fp in friendlyProperties)
									writer.Write ("<option value=\"" + context.Server.HtmlEncode (fp) + "\"" + (fp.Equals (tabPropName) ? " selected=\"selected\"" : string.Empty) + ">" + context.Server.HtmlEncode ((knownProps.ContainsKey (fp) ? knownProps [fp] : fp) + (fp.Equals (tabPropName) ? ":" : string.Empty)) + "</option>");
								writer.Write ("</select></div>");
							}
							if (ProductPage.Config<bool> (null, "FilterTabShowAll"))
								writer.Write ("<div class=\"ms-templatepicker" + ((consumer.tabValue == null) ? string.Empty : "un") + "selected\"><div><a class=\"rollajaxlnk\" href=\"" + Noop + "\" onclick=\"" + HttpUtility.HtmlAttributeEncode (getTabScript (null)) + "\">" + Res ("All") + "</a></div></div>");
							foreach (string tab in consumer.tabs)
								if (!string.IsNullOrEmpty (tab)) {
									if (consumer.tabsReduced) {
										if (lastTab < 0)
											lastTab = (int.TryParse (tab, out lastTab) ? ((int) '0') : ((int) 'A')) - 1;
										if ((((thisTab = (int) tab [0]) - lastTab) > 1) && (lastTab >= 0))
											for (int i = (lastTab + 1); i < thisTab; i++)
												if (char.IsLetterOrDigit (theChar = (char) i))
													writer.Write ("<div class=\"ms-templatepickerunselected ms-templatepickerdisabled\"><div><span>" + theChar + "</span></div></div>");
										lastTab = thisTab;
									}
									writer.Write ("<div class=\"ms-templatepicker" + ((((consumer.tabValue == null) && (consumer.tabs.IndexOf (tab) == 0) && !ProductPage.Config<bool> (null, "FilterTabShowAll")) || (consumer.tabValue == tab)) ? string.Empty : "un") + "selected\"><div><a class=\"rollajaxlnk\" href=\"" + Noop + "\" onclick=\"" + HttpUtility.HtmlAttributeEncode (getTabScript (tab)) + "\">" + (string.IsNullOrEmpty (tab) ? "&mdash;" : context.Server.HtmlEncode (tab)) + "</a></div></div>");
								}
							if (lastTab > 0)
								if (int.TryParse (((char) lastTab).ToString (), out thisTab)) {
									if (thisTab < 9)
										for (int i = (thisTab + 1); i <= 9; i++)
											writer.Write ("<div class=\"ms-templatepickerunselected ms-templatepickerdisabled\"><div><span>" + i + "</span></div></div>");
								} else if (lastTab < ((int) 'Z'))
									for (int i = (lastTab + 1); i <= ((int) 'Z'); i++)
										if (char.IsLetterOrDigit (theChar = (char) i))
											writer.Write ("<div class=\"ms-templatepickerunselected ms-templatepickerdisabled\"><div><span>" + theChar + "</span></div></div>");
							if ((consumer.tabs.Count > 0) && string.Empty.Equals (consumer.tabs [consumer.tabs.Count - 1]))
								writer.Write ("<div class=\"ms-templatepicker" + (string.Empty.Equals (consumer.tabValue) ? string.Empty : "un") + "selected\"><div><a href=\"" + Noop + "\" class=\"rollajaxlnk\" onclick=\"" + HttpUtility.HtmlAttributeEncode (getTabScript (string.Empty)) + "\">" + "&mdash;</a></div></div>");
							writer.Write ("<div class=\"rox-tabstrip-fill\">&nbsp;</div></div>");
						}
						if ((isPaging = ((pageSize > 0) && (consumer.List.Count > 0) && ((pageStart > 0) || ((pageStart + pageSize) < consumer.recCount)) && ((pageMode > 0) || (pageStepMode > 0) || (pageSkipMode > 0)))) || (consumer.List.Count == 0) || allowView)
							using (StringWriter sw = new StringWriter (navBuffer)) {
								sw.Write ("<div id=\"rox_pager_" + id + "\" class=\"rox-rollup-paging" + (allowView ? " rox-rollup-switcher" : string.Empty) + ((consumer.tabs == null) ? string.Empty : " rox-rollup-hastabs") + "\">");
								if (allowView) {
									sw.Write (listStyle ? "<span style=\"float: right;\">" + Res ("ShowAs") + " <a class=\"rollajaxlnk\" href=\"" + Noop + "\" onclick=\"" + HttpUtility.HtmlAttributeEncode (getViewScript (false)) + "\">" + Res ("StyleClassicCaption") + "</a>&nbsp;" : "<span style=\"float: right;\">" + Res ("ShowAs") + "<b>" + Res ("StyleClassicCaption") + "</b>&nbsp;");
									sw.Write (listStyle ? "&nbsp;<b>" + Res ("StyleListCaption") + "</b></span>" : "&nbsp;<a class=\"rollajaxlnk\" href=\"" + Noop + "\" onclick=\"" + HttpUtility.HtmlAttributeEncode (getViewScript (true)) + "\">" + Res ("StyleListCaption") + "</a></span>");
								}
								if (consumer.List.Count == 0)
									sw.Write ("<span class=\"rox-pz-zero rox-rz-zero\">" + Res ("ZeroRecs" + (IsRollupZen ? "R" : string.Empty)) + "</span>");
								else if (isPaging) {
									pageCount = (consumer.recCount / pageSize) + (((consumer.recCount % pageSize) == 0) ? 0 : 1);
									if (pageStart > 0) {
										if (pageSkipMode == 1)
											sw.Write ("<a class=\"rollajaxlnk\" href=\"" + Noop + "\" onclick=\"{0}\">" + Res ("First") + "</a>&nbsp;&nbsp;" + ((pageStepMode == 0) ? "|&nbsp;" : "&nbsp;"), HttpUtility.HtmlAttributeEncode (getPagingScript (0)));
										else if (pageSkipMode == 2)
											sw.Write ("<a class=\"rox-pagestep rox-pageskip rollajaxlnk\" href=\"" + Noop + "\" onclick=\"{0}\" style=\"background-image: url('" + webUrl + "/_layouts/images/marrrtl.gif'); padding-left: 1px;\">|&nbsp;&nbsp;</a>&nbsp;", HttpUtility.HtmlAttributeEncode (getPagingScript (0)));
										if (pageStepMode == 1)
											sw.Write ("<a class=\"rollajaxlnk\" href=\"" + Noop + "\" onclick=\"{0}\">" + Res ("Prev") + "</a>&nbsp;&nbsp;|&nbsp;", HttpUtility.HtmlAttributeEncode (getPagingScript (pageStart - pageSize)));
										else if (pageStepMode == 2)
											sw.Write ("<a class=\"rox-pagestep rollajaxlnk\" href=\"" + Noop + "\" onclick=\"{0}\" style=\"background-image: url('" + webUrl + "/_layouts/images/marrrtl.gif');\">&nbsp;</a>&nbsp;", HttpUtility.HtmlAttributeEncode (getPagingScript (pageStart - pageSize)));
									}
									if (pageMode == 1)
										sw.Write ("&nbsp;" + Res ("xtoyofz", pageStart + 1, ((pageStart + pageSize) > consumer.recCount) ? (consumer.recCount) : (pageStart + pageSize), consumer.recCount) + "&nbsp;");
									else if (pageMode == 2)
										sw.Write ("&nbsp;" + Res ("xofy", (pageStart / pageSize) + 1, pageCount) + "&nbsp;");
									else if (pageMode == 3)
										for (int p = 0; p < pageCount; p++) {
											if (pageStart == (p * pageSize)) {
												if (skipped > 0)
													sw.Write ("&hellip;");
												skipped = 0;
												sw.Write ("<b class=\"rox-pageno\">" + (p + 1) + "</b>");
											} else if ((p == 0) || (p == 1) || (p == (pageCount - 1)) || (p == (pageCount - 2)) || (p == ((pageStart / pageSize) - 1)) || (p == ((pageStart / pageSize) + 1))) {
												if (skipped > 0)
													sw.Write ("&hellip;");
												skipped = 0;
												sw.Write ("<a class=\"rox-pageno rollajaxlnk\" href=\"" + Noop + "\" onclick=\"{0}\">" + (p + 1) + "</a>", HttpUtility.HtmlAttributeEncode (getPagingScript (p * pageSize)));
											} else
												skipped++;
										}
									if ((pageStart + pageSize) < consumer.recCount) {
										if (pageStepMode == 1)
											sw.Write ("&nbsp;|&nbsp;&nbsp;<a class=\"rollajaxlnk\" href=\"" + Noop + "\" onclick=\"{0}\">" + Res ("Next") + "</a>", HttpUtility.HtmlAttributeEncode (getPagingScript (pageStart + pageSize)));
										else if (pageStepMode == 2)
											sw.Write ("&nbsp;<a class=\"rox-pagestep rollajaxlnk\" href=\"" + Noop + "\" onclick=\"{0}\" style=\"background-image: url('" + webUrl + "/_layouts/images/marr.gif');\">&nbsp;</a>", HttpUtility.HtmlAttributeEncode (getPagingScript (pageStart + pageSize)));
										if (pageSkipMode == 1)
											sw.Write (((pageStepMode == 0) ? "&nbsp;|&nbsp;" : "&nbsp;&nbsp;") + "&nbsp;<a class=\"rollajaxlnk\" href=\"" + Noop + "\" onclick=\"{0}\">" + Res ("Last") + "</a>", HttpUtility.HtmlAttributeEncode (getPagingScript ((pageCount - 1) * pageSize)));
										else if (pageSkipMode == 2)
											sw.Write ("&nbsp;<a class=\"rox-pagestep rox-pageskip rollajaxlnk\" href=\"" + Noop + "\" onclick=\"{0}\" style=\"background-image: url('" + webUrl + "/_layouts/images/marr.gif');\">&nbsp;&nbsp;|</a>", HttpUtility.HtmlAttributeEncode (getPagingScript ((pageCount - 1) * pageSize)));
									}
								} else
									sw.Write ("&nbsp;");
								sw.Write ("</div>");
							}
						if (ShowNavTop)
							writer.Write ("<div class=\"rox-rollup-topnav\">" + navBuffer.ToString (0, navBuffer.Length) + "</div>");
						if (!string.IsNullOrEmpty (groupPropName)) {
							foreach (string propLine in properties.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
								if (((pair = propLine.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pair.Length >= 1) && (pair [0].Trim ().Equals (groupPropName, StringComparison.InvariantCultureIgnoreCase)))
									if ((pair.Length < 2) || ("___".Equals (pair [1].Trim (), StringComparison.InvariantCultureIgnoreCase)) || string.IsNullOrEmpty (pair [1].Trim ()))
										groupTitle = string.Empty;
									else
										groupTitle = pair [1].Trim ();
							if ((groupTitle == null) && (knownProps.ContainsKey (groupPropName)))
								groupTitle = knownProps [groupPropName];
						}
						if (consumer.List.Count > 0)
							if (listStyle) {
								writer.Write ("<table class=\"rox-rollupitems\" width=\"99%\" border=\"0\" cellSpacing=\"0\" cellPadding=\"0\"><tr><td><table width=\"100%\" class=\"ms-summarystandardbody\" border=\"0\" cellSpacing=\"0\" cellPadding=\"1\" summary=\"Tasks\">");
								writer.Write ("<tr class=\"ms-viewheadertr\" vAlign=\"top\">");
								pcount = 0;
								foreach (string propLine in (((pictMode > 0) ? "_rox_Picture:___\r\n" : string.Empty) + ((nameMode == 0) ? string.Empty : ("_rox_Name:" + nameCaption + "\r\n")) + properties).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
									if (((pair = propLine.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pair.Length >= 1) && ((pair.Length < 2) || (!string.IsNullOrEmpty (pair [1].Trim ())))) {
										if (pair.Length < 2)
											pair = new string [] { pair [0], consumer.DataSource.GetPropertyDisplayName (pair [0]) };
										propName = pair [0].Trim ();
										writer.Write ("<th class=\"ms-vh2\" noWrap=\"nowrap\"><div style=\"position: relative; width: 100%; top: 0px; left: 0px;\"><table height=\"100%\" class=\"ms-unselectedtitle\" style=\"width: 100%;\" cellSpacing=\"1\" cellPadding=\"0\"><tr><td width=\"100%\" class=\"ms-vb\" noWrap=\"nowrap\">" + ("___".Equals (pair [1].Trim ()) ? string.Empty : (allowSort ? ("<a title=\"" + context.Server.HtmlEncode (Res ("SortBy", pair [1])) + "\" href=\"" + Noop + "\" onclick=\"" + HttpUtility.HtmlAttributeEncode (getSortScript (((((sortPropName == null) && (propName.Equals ("_rox_Name"))) || propName.Equals (sortPropName)) && !sortDesc) ? ('-' + propName) : ('+' + propName))) + "\">" + context.Server.HtmlEncode (pair [1].Trim ()) + "</a><img src=\"" + webUrl + "/_layouts/images/" + (propName.Equals (sortPropName) ? (sortDesc ? "rsort" : "sort") : "blank") + ".gif\" border=\"0\" />") : context.Server.HtmlEncode (pair [1].Trim ()))) + "</td><td style=\"position: absolute;\"><img width=\"13\" style=\"visibility: hidden;\" src=\"" + webUrl + "/_layouts/images/blank.gif\" /></td></tr></table></div></th>");
									}
									pcount++;
									if ((!ProductPage.LicEdition (spCtx, l as ProductPage.LicInfo, 2)) && (pcount >= (2 + ((pictMode > 0) ? 1 : 0) + ((nameMode > 0) ? 1 : 0))))
										break;
								}
								writer.Write ("</tr>");
								foreach (CachedRecord crec in consumer.List) {
									profUrl = (nameMode == 1) ? GetProfileUrl (crec) : string.Empty;
									picProfUrl = (pictMode == 1) ? GetProfileUrl (crec) : string.Empty;
									if ((!string.IsNullOrEmpty (groupPropName)) && ((groupVal = crec [groupPropName, null, consumer.DataSource]) != lastGroupVal)) {
										lastGroupVal = groupVal;
										writer.Write ("<tr><td id=\"" + (groupID = "g" + ProductPage.GuidLower (Guid.NewGuid ()).Replace ('-', '_')) + "\" class=\"ms-gb\" nowrap=\"nowrap\" colspan=\"100\" onmouseover=\"jQuery(this).addClass('ms-gbhover" + (groupInt ? string.Empty : "x") + "');jQuery('a.rox-rollgroupdirlink').hide();jQuery('select.rox-rollgroupprefix').hide();jQuery('span.rox-rollgroupprefix').show();roxNoMouseOut=false;jQuery('#dir_" + groupID + "').show();jQuery('#grp_" + groupID + "').show();jQuery('#pref_" + groupID + "').hide();\" onmouseout=\"if(!roxNoMouseOut){jQuery(this).removeClass('ms-gbhover');jQuery('a.rox-rollgroupdirlink').hide();jQuery('select.rox-rollgroupprefix').hide();jQuery('span.rox-rollgroupprefix').show();}\">" + ((!string.IsNullOrEmpty (groupTitle)) ? ("<span id=\"pref_" + (groupInt ? groupID : string.Empty) + "\" class=\"rox-rollgroupprefix\">" + context.Server.HtmlEncode (groupTitle) + ":</span> ") : string.Empty) + (groupInt ? ("<select onchange=\"" + HttpUtility.HtmlAttributeEncode (getRegroupScript ("\" + this.options[this.selectedIndex].value + \"")) + "\" onfocus=\"roxNoMouseOut=true;\" onblur=\"roxNoMouseOut=false;\" onchange=\"\" class=\"rox-rollgroupprefix\" id=\"grp_" + groupID + "\" style=\"display: none;\">" + options + "</select> ") : string.Empty) + "<span class=\"rox-rollgroup\">" + (string.IsNullOrEmpty (groupVal) ? "&mdash;" : context.Server.HtmlEncode (groupVal)) + "</span>" + (groupShowCounts ? (" <span class=\"rox-rollgroupcount\">(" + consumer.groupCounts [groupVal + string.Empty] + ")</span>") : string.Empty) + (groupIntDir ? (" <a class=\"rox-rollgroupdirlink rollajaxlnk\" id=\"dir_" + groupID + "\" style=\"display: none;\" href=\"" + Noop + "\" onclick=\"" + HttpUtility.HtmlAttributeEncode (getGroupScript (!groupDesc)) + "\"><img src=\"" + webUrl + "/_layouts/images/" + (groupDesc ? "rsort" : "sort") + ".gif\" border=\"0\" align=\"baseline\" /></a>") : string.Empty) + "</td></tr>");
									}
									writer.Write ("<tr class=\"" + (trClass = (string.IsNullOrEmpty (trClass) ? "ms-alternating" : string.Empty)) + "\">");
									if (pictMode > 0)
										writer.Write ("<td width=\"1%\" class=\"ms-vb2\"><div class=\"rox-rollupitem-picture\"><" + (string.IsNullOrEmpty (picProfUrl) ? "span" : "a") + ((string.IsNullOrEmpty (picUrl = GetPictureUrl (context, consumer.DataSource, crec, site.Url)) || picUrl.ToLowerInvariant ().Contains (DEFAULT_PICTUREURL.ToLowerInvariant ()) || picUrl.ToLowerInvariant ().Contains ("person.gif") || picUrl.ToLowerInvariant ().Contains ("no_pic")) ? " style=\"background: none !important; border: 0px none transparent !important; padding: 3px !important;\" " : string.Empty) + (((linkTarget == "_modal") || (linkTarget == "_popup")) ? (" onclick=\"roxPopup(\'" + SPEncode.ScriptEncode (picProfUrl) + "\', " + (linkTarget == "_popup").ToString ().ToLowerInvariant () + ");\" ") : string.Empty) + " href=\"" + (((linkTarget == "_modal") || (linkTarget == "_popup")) ? Noop : picProfUrl) + "\" target=\"" + (((linkTarget == "_modal") || (linkTarget == "_popup")) ? "_self" : linkTarget) + "\"\"><img border=\"0\" onerror=\"roxImageError(this,'" + webUrl + DEFAULT_PICTUREURL + "');\" src=\"" + picUrl + "\" title=\"" + HttpUtility.HtmlAttributeEncode (DataSourceConsumer.GetTitle (consumer, crec)) + "\" " + ((imageHeight == 0) ? string.Empty : ("style=\"height: " + imageHeight + "px;\" ")) + "/></" + (string.IsNullOrEmpty (picProfUrl) ? "span" : "a") + "></div></td>");
									if (nameMode > 0) {
										writer.Write ("<td nowrap=\"nowrap\" class=\"ms-vb2 rox-rollupitem-fullname\"><" + (string.IsNullOrEmpty (profUrl) ? "b" : "a") + (((linkTarget == "_modal") || (linkTarget == "_popup")) ? " onclick=\"roxPopup(\'" + SPEncode.ScriptEncode (profUrl) + "\', " + (linkTarget == "_popup").ToString ().ToLowerInvariant () + ");\"" : string.Empty) + " href=\"" + (((linkTarget == "_modal") || (linkTarget == "_popup")) ? Noop : profUrl) + "\" target=\"" + (((linkTarget == "_modal") || (linkTarget == "_popup")) ? "_self" : linkTarget) + "\">" + DataSourceConsumer.GetTitle (consumer, crec) + "</" + (string.IsNullOrEmpty (profUrl) ? "b" : "a") + ">");
										if (presence && !string.IsNullOrEmpty (tmp = crec [DataSource.SCHEMAPROP_PREFIX + UserDataSource.SCHEMAPROP_MAILFIELD, string.Empty, consumer.DataSource]))
											writer.Write ("<span style=\"padding: 0px 5px 0px 5px;\"><img border=\"0\" height=\"12\" width=\"12\" src=\"" + webUrl + "/_layouts/images/imnunk.png\" onload=\"IMNRC('" + SPEncode.ScriptEncode (tmp) + "')\" name=\"imnmark\" id=\"IMID" + ProductPage.GuidLower (Guid.NewGuid ()) + "\" ShowOfflinePawn=\"1\"/></span>");
										if (vcard && !string.IsNullOrEmpty (tmp = crec [UserDataSource.FIELDNAME_VCARDEXPORT, string.Empty, consumer.DataSource]))
											writer.Write (tmp);
										writer.Write ("</td>");
									}
									pcount = 0;
									foreach (string propLine in properties.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
										if (((pair = propLine.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pair.Length >= 1) && ((pair.Length < 2) || (!string.IsNullOrEmpty (pair [1].Trim ())))) {
											writer.Write ("<td class=\"ms-vb2\">");
											writer.Write ((temp = (((isDate = DateTime.TryParse (crec [propName = pair [0].Trim (), string.Empty, consumer.DataSource], out dtVal))) && (propName.Equals ("SPS-Birthday", StringComparison.InvariantCultureIgnoreCase) || propName.Equals ("BDay", StringComparison.InvariantCultureIgnoreCase))) ? dtVal.ToString ("m", ProductPage.GetFarmCulture (spCtx)) : crec [propName, string.Empty, consumer.DataSource]).ToLowerInvariant ().Contains ("<div>") ? temp : (isDate ? ("<nobr>" + context.Server.HtmlEncode (temp) + "</nobr>") : (((addr = ProductPage.GetEmailAddress (temp)) != null) ? (string.Format (context.Server.HtmlDecode (ProductPage.Config (spCtx, "MailPropFormat")), context.Server.HtmlEncode (addr.Address), '{', '}')) : ((UserDataSource.FIELDNAME_VCARDEXPORT.Equals (propName) || temp.StartsWith (Record.HTML_PREFIX)) ? temp : context.Server.HtmlEncode (temp)))));
											writer.Write ("</td>");
										}
										pcount++;
										if ((!ProductPage.LicEdition (spCtx, l as ProductPage.LicInfo, 2)) && (pcount >= 2))
											break;
									}
									writer.Write ("</tr>");
								}
								writer.Write ("</table></td></tr></table>");
							} else {
								writer.Write ("<div class=\"rox-rollupitems-all\">");
								if (rowSize > 0)
									writer.WriteLine ("<table border=\"0\" cellspacing=\"0\" cellpadding=\"0\">");
								foreach (CachedRecord crec in consumer.List) {
									profUrl = (nameMode == 1) ? GetProfileUrl (crec) : string.Empty;
									picProfUrl = (pictMode == 1) ? GetProfileUrl (crec) : string.Empty;
									if ((rowSize > 0) && (!string.IsNullOrEmpty (groupPropName)) && ((groupVal = crec [groupPropName, null, consumer.DataSource]) != lastGroupVal)) {
										lastGroupVal = groupVal;
										writer.Write ("<tr><td id=\"" + (groupID = "g" + ProductPage.GuidLower (Guid.NewGuid ()).Replace ('-', '_')) + "\" class=\"ms-gb\" nowrap=\"nowrap\" colspan=\"100\" onmouseover=\"jQuery(this).addClass('ms-gbhover" + (groupInt ? string.Empty : "x") + "');jQuery('a.rox-rollgroupdirlink').hide();jQuery('select.rox-rollgroupprefix').hide();jQuery('span.rox-rollgroupprefix').show();roxNoMouseOut=false;jQuery('#dir_" + groupID + "').show();jQuery('#grp_" + groupID + "').show();jQuery('#pref_" + groupID + "').hide();\" onmouseout=\"if(!roxNoMouseOut){jQuery(this).removeClass('ms-gbhover');jQuery('a.rox-rollgroupdirlink').hide();jQuery('select.rox-rollgroupprefix').hide();jQuery('span.rox-rollgroupprefix').show();}\">" + ((!string.IsNullOrEmpty (groupTitle)) ? ("<span id=\"pref_" + (groupInt ? groupID : string.Empty) + "\" class=\"rox-rollgroupprefix\">" + context.Server.HtmlEncode (groupTitle) + ":</span> ") : string.Empty) + (groupInt ? ("<select onchange=\"" + HttpUtility.HtmlAttributeEncode (getRegroupScript ("\" + this.options[this.selectedIndex].value + \"")) + "\" onfocus=\"roxNoMouseOut=true;\" onblur=\"roxNoMouseOut=false;\" onchange=\"\" class=\"rox-rollgroupprefix\" id=\"grp_" + groupID + "\" style=\"display: none;\">" + options + "</select> ") : string.Empty) + "<span class=\"rox-rollgroup\">" + (string.IsNullOrEmpty (groupVal) ? "&mdash;" : context.Server.HtmlEncode (groupVal)) + "</span>" + (groupShowCounts ? (" <span class=\"rox-rollgroupcount\">(" + consumer.groupCounts [groupVal + string.Empty] + ")</span>") : string.Empty) + (groupIntDir ? (" <a class=\"rox-rollgroupdirlink rollajaxlnk\" id=\"dir_" + groupID + "\" style=\"display: none;\" href=\"" + Noop + "\" onclick=\"" + HttpUtility.HtmlAttributeEncode (getGroupScript (!groupDesc)) + "\"><img src=\"" + webUrl + "/_layouts/images/" + (groupDesc ? "rsort" : "sort") + ".gif\" border=\"0\" align=\"baseline\" /></a>") : string.Empty) + "</td></tr>");
									}
									if ((rowSize > 0) && ((pc == 0) || ((pc % rowSize) == 0)))
										writer.WriteLine ("<tr>");
									if (rowSize > 0)
										writer.WriteLine ("<td valign=\"top\">");
									writer.WriteLine (@"
<div class=""rox-rollupitem"" style=""width: " + tileWidth + @";"">" + ((pictMode > 0) ? @"
	<div class=""rox-rollupitem-picture"">
		<{10} style=""{3}"" href=""{6}"" target=""{4}""{8}><img alt=""{1}"" border=""0"" onerror=""roxImageError(this,'" + webUrl + DEFAULT_PICTUREURL + @"');"" src=""{0}"" title=""{1}"" " + ((imageHeight == 0) ? string.Empty : ("style=\"height: " + imageHeight + "px;\" ")) + @"/></{10}>
	</div>
" : string.Empty) + ((nameMode <= 0) ? string.Empty : @"
	<div class=""rox-rollupitem-fullname"">
		{9}
		<{5} href=""{2}"" target=""{4}"" class=""rox-rollup-item""{7}>{1}</{5}>
		{11}
	</div>"),
	   picUrl = GetPictureUrl (context, consumer.DataSource, crec, site.Url),
	   DataSourceConsumer.GetTitle (consumer, crec),
	   (((linkTarget == "_modal") || (linkTarget == "_popup")) ? Noop : profUrl),
	   (string.IsNullOrEmpty (picUrl) || picUrl.ToLowerInvariant ().Contains (DEFAULT_PICTUREURL.ToLowerInvariant ()) || picUrl.ToLowerInvariant ().Contains ("person.gif") || picUrl.ToLowerInvariant ().Contains ("no_pic")) ? "background: none !important; border: 0px none transparent !important; padding: 3px !important;" : string.Empty,
	   (((linkTarget == "_modal") || (linkTarget == "_popup")) ? "_self" : linkTarget),
	   string.IsNullOrEmpty (profUrl) ? "span" : "a",
	   (((linkTarget == "_modal") || (linkTarget == "_popup")) ? Noop : picProfUrl),
	   (((linkTarget == "_modal") || (linkTarget == "_popup")) ? " onclick=\"roxPopup(\'" + SPEncode.ScriptEncode (profUrl) + "\', " + (linkTarget == "_popup").ToString ().ToLowerInvariant () + ");\"" : string.Empty),
	   (((linkTarget == "_modal") || (linkTarget == "_popup")) ? " onclick=\"roxPopup(\'" + SPEncode.ScriptEncode (picProfUrl) + "\', " + (linkTarget == "_popup").ToString ().ToLowerInvariant () + ");\"" : string.Empty),
	   (presence && !string.IsNullOrEmpty (tmp = crec [DataSource.SCHEMAPROP_PREFIX + UserDataSource.SCHEMAPROP_MAILFIELD, string.Empty, consumer.DataSource])) ? ("<span style=\"padding: 0px 5px 0px 5px;\"><img border=\"0\" height=\"12\" width=\"12\" src=\"" + webUrl + "/_layouts/images/imnunk.png\" onload=\"IMNRC('" + SPEncode.ScriptEncode (tmp) + "');\" name=\"imnmark\" id=\"IMID" + ProductPage.GuidLower (Guid.NewGuid ()) + "\" ShowOfflinePawn=\"1\"/></span>") : string.Empty,
	   string.IsNullOrEmpty (picProfUrl) ? "span" : "a",
	   vcard ? crec [UserDataSource.FIELDNAME_VCARDEXPORT, string.Empty, consumer.DataSource] : string.Empty);
									pcount = 0;
									foreach (string propLine in properties.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
										if (((pair = propLine.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pair.Length >= 1) && (!string.IsNullOrEmpty (propName = pair [0].Trim ())) && ((pair.Length < 2) || (!string.IsNullOrEmpty (pair [1].Trim ()))) && ((!(isEmpty = string.IsNullOrEmpty (crec [propName, string.Empty, consumer.DataSource]))) || (!ProductPage.Config<bool> (spCtx, "SkipUnknownProps"))))
											try {
												if (pair.Length < 2)
													pair = new string [] { pair [0], consumer.DataSource.GetPropertyDisplayName (pair [0]) };
												writer.WriteLine ("<div class=\"rox-rollupitem-{0} rox-rollup-item\">{1}</div>", propName.ToLowerInvariant (), string.Format (context.Server.HtmlDecode (ProductPage.Config (spCtx, (isEmpty ? "Unknown" : "Known") + "PropFormat")), context.Server.HtmlEncode (pair [1].Trim ()), isEmpty ? Res ("Unknown") : ((propName.Equals ("SPS-Birthday", StringComparison.InvariantCultureIgnoreCase) && DateTime.TryParse (crec [propName, string.Empty, consumer.DataSource], out dtVal)) ? dtVal.ToString ("m", ProductPage.GetFarmCulture (spCtx)) : (((addr = ProductPage.GetEmailAddress (crec [propName, string.Empty, consumer.DataSource])) != null) ? string.Format (context.Server.HtmlDecode (ProductPage.Config (spCtx, "MailPropFormat")), context.Server.HtmlEncode (addr.Address), '{', '}') : (((temp = crec [propName, string.Empty, consumer.DataSource]).StartsWith (Record.HTML_PREFIX) || UserDataSource.FIELDNAME_VCARDEXPORT.Equals (propName)) ? temp : context.Server.HtmlEncode (temp)))), propName, '{', '}').Replace ("___: ", string.Empty).Replace ("___ ", string.Empty));
											} catch (Exception ex) {
												writer.WriteLine ("<div style=\"background-color: #ff9999;\" class=\"rox-rollupitem-{0} rox-rollup-item\">{1}</div>", propName.ToLowerInvariant (), context.Server.HtmlEncode (ex.Message));
											}
										pcount++;
										if ((!ProductPage.LicEdition (spCtx, l as ProductPage.LicInfo, 2)) && (pcount >= 2))
											break;
									}
									writer.Write ("</div>");
									if (rowSize > 0)
										writer.WriteLine ("</td>");
									pc++;
									if ((rowSize > 0) && ((pc % rowSize) == 0))
										writer.WriteLine ("</tr>");
								}
								if (rowSize > 0)
									writer.WriteLine ("</table>");
								writer.Write ("</div>");
							}
						if (ShowNavBottom && (navBuffer.Length > 0)) {
							writer.Write ("<div class=\"rox-rollup-bottomnav\">");
							writer.Write (navBuffer.ToString ());
							writer.Write ("</div>");
						}
						writer.Write ("<div style=\"clear: both;\"></div></div>");
						writer.Flush ();
					}
				};
				if ((ds != null) && (ds.inst != null))
					sec = ds.inst ["s"] as string;
				if ((dynInst != null) && dynInst.Contains ("s"))
					sec = dynInst ["s"] + string.Empty;
				if ((ds != null) && (ds.inst != null))
					secUser = ds.inst ["su"] as string;
				if ((dynInst != null) && dynInst.Contains ("su"))
					secUser = dynInst ["su"] + string.Empty;
				if ((ds != null) && (ds.inst != null))
					secPass = ds.inst ["sp"] as string;
				if ((dynInst != null) && dynInst.Contains ("sp"))
					secPass = dynInst ["sp"] + string.Empty;
				if ((!string.IsNullOrEmpty (secUser)) && ((pos = secUser.IndexOf ('\\')) > 0)) {
					secDom = secUser.Substring (0, pos);
					secUser = secUser.Substring (pos + 1);
				}
				if (string.IsNullOrEmpty (sec) || (sec == "b") || (sec == "e"))
					ProductPage.Elevate (code, (sec != "e"));
				else if ((sec == "i") && (!string.IsNullOrEmpty (secUser)) && !string.IsNullOrEmpty (secPass))
					using (SPElevator impersonate = new SPElevator (secDom, secUser, secPass))
						code ();
				else
					code ();
				tw.Write (buffer.ToString ());
			} catch (Exception err) {
				if ((err is TargetInvocationException) && (err.InnerException != null))
					err = err.InnerException;
				tw.Write ("<div class=\"rox-error\">" + HttpUtility.HtmlEncode (err.ToString ()).Replace ("\r", "<br/>").Replace ("\n", "<br/>").Replace ("\t", "&nbsp;&nbsp;&nbsp;&nbsp;") + "</div>");
			} finally {
				if ((wp != null) && (wp.sortErrors == null))
					wp.sortErrors = sortErrors;
			}
		}

		internal static string Res (string name, params object [] args) {
			return ProductPage.GetProductResource (name, args);
		}

		public static bool ShowNavBottom {
			get {
				string cfg = ProductPage.Config (ProductPage.GetContext (), "PagingAlign");
				return "bottom".Equals (cfg, StringComparison.InvariantCultureIgnoreCase) || "both".Equals (cfg, StringComparison.InvariantCultureIgnoreCase);
			}
		}

		public static bool ShowNavTop {
			get {
				return !"bottom".Equals (ProductPage.Config (ProductPage.GetContext (), "PagingAlign"), StringComparison.InvariantCultureIgnoreCase);
			}
		}

		public RollupWebPart () {
			urlPropertyPrefix = "rollup_";
			this.ExportMode = WebPartExportMode.All;
		}

		void IFilterConsumer.ClearFilter (object sender, EventArgs e) {
			oobFilterPairs.Clear ();
		}


		void IFilterConsumer.NoFilter (object sender, EventArgs e) {
		}

		void IFilterConsumer.SetFilter (object sender, SetFilterEventArgs setFilterEventArgs) {
			int pos;
			List<string> names = new List<string> (), vals = new List<string> ();
			if ((setFilterEventArgs != null) && !string.IsNullOrEmpty (setFilterEventArgs.FilterExpression))
				foreach (string pair in setFilterEventArgs.FilterExpression.Split (new char [] { '&' }, StringSplitOptions.RemoveEmptyEntries))
					if ((pos = pair.IndexOf ('=')) > 0)
						((pair.Substring (0, pos).StartsWith ("FilterField")) ? names : vals).Add (pair.Substring (pos + 1));
			if (names.Count <= vals.Count)
				for (int i = 0; i < names.Count; i++)
					oobFilterPairs [names [i]] = vals [i];
		}

		internal string Replace (string v, IDictionary inst, string pa, string pq) {
			int pos1, pos2;
			string ph, phName, phVal;
			while (((pos1 = v.IndexOf ("{$", StringComparison.InvariantCultureIgnoreCase)) >= 0) && ((pos2 = v.IndexOf ("$}", pos1 + 2, StringComparison.InvariantCultureIgnoreCase)) > pos1)) {
				ph = v.Substring (pos1, pos2 - pos1 + 2);
				phName = ph.Substring (2, ph.Length - 4);
				if (phName.StartsWith (pa, StringComparison.InvariantCultureIgnoreCase))
					phVal = inst [phName.Substring (pa.Length)] + string.Empty;
				else if (phName.StartsWith (pq, StringComparison.InvariantCultureIgnoreCase))
					phVal = Context.Request.QueryString [phName.Substring (pa.Length)] + string.Empty;
				else
					phVal = EffectiveTitle;
				v = v.Replace (ph, phVal);
			}
			return v;
		}

		protected override void CreateChildControls () {
			base.CreateChildControls ();
			textArea.ID = "textArea";
			textArea.Wrap = false;
			textArea.Rows = 8;
			textArea.Style ["display"] = "none";
			textArea.Style ["width"] = "98%";
			textArea.TextMode = TextBoxMode.MultiLine;
			Controls.Add (textArea);
		}

		protected virtual void OnFilterConsumerInit (FilterConsumerInitEventArgs e) {
			if (FilterConsumerInit != null)
				FilterConsumerInit (this, e);
		}

		protected override void Render (HtmlTextWriter writer) {
			int pageStart = 0;
			bool hasTv;
			string webUrl;
			Hashtable fht = new Hashtable (), jop;
			NameValueCollection qs = null;
			List<int> lcidsDone = new List<int> (new int [] { 1033 });
			SPContext ctx = ProductPage.GetContext ();
			Guid contextID = Guid.Empty;
			IDictionary dynInst;
			fht ["f"] = Filters;
			fht ["fa"] = CurUser ? new List<string> () : andFilters;
			EnsureChildControls ();
			if (EffectiveJquery && !Page.Items.Contains ("jquery")) {
				Page.Items ["jquery"] = new object ();
				writer.Write ("<script language=\"JavaScript\" type=\"text/javascript\" src=\"" + WebUrl + "/_layouts/" + ProductPage.AssemblyName + "/jQuery.js?v=" + ProductPage.Version + "\"></script>");
			}
			if (!Page.Items.Contains ("roxority")) {
				Page.Items ["roxority"] = new object ();
				writer.Write ("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + WebUrl + "/_layouts/" + ProductPage.AssemblyName + "/roxority.tl.css?v=" + ProductPage.Version + "\"/>");
				writer.Write ("<script language=\"JavaScript\" type=\"text/javascript\" src=\"" + WebUrl + "/_layouts/" + ProductPage.AssemblyName + "/json2.tl.js?v=" + ProductPage.Version + "\"></script>");
				writer.Write ("<script language=\"JavaScript\" type=\"text/javascript\" src=\"" + WebUrl + "/_layouts/" + ProductPage.AssemblyName + "/roxority.tl.js?v=" + ProductPage.Version + "\"></script>");
			}
			if (!Page.Items.Contains ("roxrollupcss")) {
				Page.Items ["roxrollupcss"] = new object ();
				writer.Write ("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + WebUrl + "/_layouts/" + ProductPage.AssemblyName + "/RollupZen.tl.css?v=" + ProductPage.Version + "\"/>");
				writer.Write ("<script language=\"JavaScript\" type=\"text/javascript\" src=\"" + WebUrl + "/_layouts/" + ProductPage.AssemblyName + "/RollupZen.tl.js?v=" + ProductPage.Version + "\"></script>");
				if (NoAjax)
					writer.Write ("<script language=\"JavaScript\" type=\"text/javascript\"> roxRollNoAjax = true; </script>");
				writer.Write ("<script language=\"JavaScript\" type=\"text/javascript\"> roxEmbedMode = '" + ProductPage.Config (ctx, "EmbedFilters") + "' || 'merge'; " + (IsDesign ? "roxEditMode = true;" : string.Empty) + " </script>");
				if (ProductPage.Is14) {
					writer.Write ("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + WebUrl + "/_layouts/1033/styles/Themable/layouts.css\"/>");
					foreach (int lcid in ProductPage.WssInstalledCultures)
						if (!lcidsDone.Contains (lcid)) {
							lcidsDone.Add (lcid);
							writer.Write ("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + WebUrl + "/_layouts/" + lcid + "/styles/Themable/layouts.css\"/>");
						}
					try {
						if ((ctx.Web.RegionalSettings != null) && !lcidsDone.Contains ((int) ctx.Web.RegionalSettings.LocaleId)) {
							lcidsDone.Add ((int) ctx.Web.RegionalSettings.LocaleId);
							writer.Write ("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + WebUrl + "/_layouts/" + ctx.Web.RegionalSettings.LocaleId + "/styles/Themable/layouts.css\"/>");
						} else if ((ctx.Web.Locale != null) && !lcidsDone.Contains (ctx.Web.Locale.LCID)) {
							lcidsDone.Add (ctx.Web.Locale.LCID);
							writer.Write ("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + WebUrl + "/_layouts/" + ctx.Web.Locale.LCID + "/styles/Themable/layouts.css\"/>");
						}
					} catch {
					}
				}
			}
			writer.Write ("<script language=\"JavaScript\" type=\"text/javascript\"> jQuery(document).ready(function() { roxRewriteFilterFunc(); " + (((ConnectedWebPart != null) && EffectiveFilterLive) ? ("roxRollupConns['" + ID + "'] = '" + ConnectedWebPart.ID + "'; ") : string.Empty) + GetReloadScript ("roxRefreshRollup", textArea.ClientID, ID, PageSize, pageStart, PageMode, PageStepMode, PageSkipMode, DateThisYear, DateIgnoreDay, EffectiveFilterLive, Properties, ListStyle, AllowView, AllowSort, SortProp, SortDesc, TabProp, TabProp, null, GroupProp, GroupDesc, GroupByCounts, GroupShowCounts, GroupInteractive, GroupInteractiveDir, RowSize, TileWidth, NameMode, PictMode, Presence, Vcard, ImageHeight, LoaderAnim, TabInteractive, fht, DataSourceID, null) + " }); </script>");
			if (isConnected)
				if ((ConnectedWebPart == null) && !LicEd (4))
					writer.Write ("<div class=\"rox-error\">" + this ["Old_NoFilterZen"] + "</div>");
				else if (ConnectedWebPart != null)
					if (!(IsFilterOobConnection || IsFilterZenConnection))
						writer.Write ("<div class=\"rox-error\">" + ProductPage.GetResource ("NopeEd", ProductPage.GetProductResource (IsFilterZenConnection ? "Old_GetFiltersFrom_FilterZen" : "Old_GetFiltersFrom_Other"), IsFilterZenConnection ? "Basic" : "Ultimate") + "</div>");
					else if (!LicEd (IsFilterZenConnection ? 2 : 4))
						writer.Write ("<div class=\"rox-error\">" + ProductPage.GetResource ("NopeEd", ProductPage.GetProductResource (IsFilterZenConnection ? "Old_GetFiltersFrom_FilterZen" : "Old_GetFiltersFrom_Other"), IsFilterZenConnection ? "Basic" : "Ultimate") + "</div>");
					else if ((!LicEd (4)) && !IsFilterZenConnection)
						writer.Write ("<div class=\"rox-error\">" + this ["Old_NoFilterZen"] + "</div>");
					else if (IsFilterZenConnection && (!((bool) ConnectedWebPart.GetType ().GetMethod ("LicEd", BindingFlags.Instance | BindingFlags.NonPublic).Invoke (ConnectedWebPart, new object [] { 2 }))))
						writer.Write ("<div class=\"rox-error\">" + this ["Old_NoFilterZenEnt"] + "</div>");
			if (!string.IsNullOrEmpty (message))
				writer.Write ("<div class=\"rox-error\">" + message + "</div>");
			if (base.IsDesign && !EffectiveJquery)
				writer.Write ("<div class=\"rox-error\">" + this ["JqueryNone"] + "</div>");

			writer.Write ("<div class=\"rox-loader\" id=\"rox_loader_" + ID + "\" style=\"background-image: url('" + WebUrl + "/_layouts/images/" + ProductPage.AssemblyName + "/" + LoaderAnim + ".gif');\">&nbsp;</div>");
			writer.Write ("<div class=\"rox-rollup\" id=\"rox_rollup_" + ID + "\"></div>");
			if (string.IsNullOrEmpty (Context.Request.QueryString ["rpzopt"]) && (((string.IsNullOrEmpty (textArea.Text) || forceReload) && !ProductPage.Config<bool> (ctx, "AjaxFirst")) || ((string.IsNullOrEmpty (textArea.Text) || !textArea.Text.StartsWith ("roxrollnoajax::")) && (ConnectedWebPart != null) && !EffectiveFilterLive))) {
				using (StringWriter sw = new StringWriter ()) {
					RollupWebPart.Render (this, sw, textArea.ClientID, ID, PageSize, pageStart, PageMode, PageStepMode, PageSkipMode, DateThisYear, DateIgnoreDay, EffectiveFilterLive, Properties, ListStyle, AllowView, AllowSort, SortProp, SortDesc, TabProp, TabProp, null, GroupProp, GroupDesc, GroupByCounts, GroupShowCounts, GroupInteractive, GroupInteractiveDir, RowSize, TileWidth, NameMode, PictMode, Presence, Vcard, ImageHeight, LoaderAnim, TabInteractive, fht, Lic, DataSourceID, null);
					sw.Flush ();
					textArea.Text = sw.GetStringBuilder ().ToString ();
				}
			} else if (textArea.Text.StartsWith ("roxrollnoajax::") || !string.IsNullOrEmpty (Context.Request.QueryString ["rpzopt"])) {
				if (textArea.Text.StartsWith ("roxrollnoajax::"))
					qs = HttpUtility.ParseQueryString (textArea.Text.Substring (textArea.Text.IndexOf ('?') + 1));
				else if ((!string.IsNullOrEmpty (Context.Request.QueryString ["rpzopt"])) && ((jop = JSON.JsonDecode (Context.Request.QueryString ["rpzopt"]) as Hashtable) != null) && (jop.Count > 0)) {
					qs = new NameValueCollection (jop.Count);
					foreach (DictionaryEntry kvp in jop)
						qs [kvp.Key + string.Empty] = kvp.Value + string.Empty;
				}
				if (qs != null) {
					hasTv = (Array.IndexOf<string> (qs.AllKeys, "tv") >= 0);
					fht ["f"] = JSON.JsonDecode (qs ["f"]);
					fht ["fa"] = JSON.JsonDecode (qs ["fa"]);
					dynInst = JSON.JsonDecode (qs ["dyn"]) as IDictionary;
					using (StringWriter sw = new StringWriter ()) {
						RollupWebPart.Render (this, sw, string.IsNullOrEmpty (qs ["tid"]) ? textArea.ClientID : qs ["tid"], string.IsNullOrEmpty (qs ["id"]) ? ID : qs ["id"], int.Parse (qs ["ps"]), int.Parse (qs ["p"]), int.Parse (qs ["pmo"]), int.Parse (qs ["pst"]), int.Parse (qs ["psk"]), "1".Equals (qs ["dty"]), "1".Equals (qs ["did"]), "1".Equals (qs ["fl"]), qs ["pr"], "1".Equals (qs ["ls"]), "1".Equals (qs ["v"]), "1".Equals (qs ["s"]), qs ["spn"], "1".Equals (qs ["sd"]), qs ["tpn"], qs ["tpo"], hasTv ? qs ["tv"] : null, qs ["gpn"], "1".Equals (qs ["gd"]), "1".Equals (qs ["gb"]), "1".Equals (qs ["gs"]), "1".Equals (qs ["gi"]), "1".Equals (qs ["gid"]), int.Parse (qs ["rs"]), qs ["t"], int.Parse (qs ["nm"]), int.Parse (qs ["pm"]), "1".Equals (qs ["on"]), "1".Equals (qs ["vc"]), int.Parse (qs ["ih"]), qs ["la"], "1".Equals (qs ["ti"]), fht, null, qs ["dsid"], dynInst);
						sw.Flush ();
						textArea.Text = sw.GetStringBuilder ().ToString ();
					}
				}
			}
			if (string.IsNullOrEmpty (SspWebUrl) || !ServerContext)
				webUrl = WebUrl;
			else
				webUrl = sspWebUrl;
			if (DataSource != null)
				contextID = DataSource.ContextID;
#if !WSS
			else if (ServerContext)
				contextID = UserProfiles.appID;
#endif
			else
				try {
					contextID = UserAccounts.GetUserList (ctx.Web).ID;
				} catch {
				}
			if (DataSource != null)
				writer.Write ("<script type=\"text/javascript\" language=\"JavaScript\">jQuery(document).ready(function() { jQuery('#roxpzproplink" + ID + "').attr({ \"href\": \"" + DataSource.GetFieldInfoUrl (webUrl, contextID) + "\", \"target\": \"_blank\" }); });</script>");
			if ((sortErrors != null) && (sortErrors.Count > 0))
				foreach (Exception ex in sortErrors)
					writer.Write ("<div class=\"rox-error\">Comparison error details: " + Context.Server.HtmlEncode (ex.ToString ()).Replace ("\r\n", "<br/>").Replace ("\r", "<br/>").Replace ("\n", "<br/>") + "</div>");
			base.Render (writer);
		}

		public override void EnsureInterfaces () {
			try {
				base.EnsureInterfaces ();
				RegisterInterface ("roxorityFilterConsumerInterface", InterfaceTypes.IFilterConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, ConnectionRunAt.Server, this, string.Empty, this ["Old_GetFiltersFrom"], this ["Old_GetFiltersFrom"], false);
			} catch {
			}
		}

		public override InitEventArgs GetInitEventArgs (string InterfaceName) {
			Dictionary<string, string> filterNames = new Dictionary<string, string> ();
			List<string> usedNames = new List<string> ();
			FilterConsumerInitEventArgs args = new FilterConsumerInitEventArgs ();
			if (DataSource != null)
				foreach (RecordProperty rp in dataSource.Properties)
					if (!usedNames.Contains (rp.Name.ToLowerInvariant ())) {
						filterNames [rp.Name] = rp.DisplayName;
						usedNames.Add (rp.Name.ToLowerInvariant ());
					}
			args.FieldList = new string [filterNames.Count];
			args.FieldDisplayList = new string [filterNames.Count];
			filterNames.Keys.CopyTo (args.FieldList, 0);
			filterNames.Values.CopyTo (args.FieldDisplayList, 0);
			return args;
		}

		public WebPartVerb GetPrintVerb (IDictionary printAction) {
			object pzItem;
			string onclick;
			int l;
			Reflector refl = null;
			WebPartVerb pv = null;
			try {
				refl = new Reflector (Assembly.Load ("roxority_PrintZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01"));
			} catch {
			}
			if ((refl != null) && ((pzItem = refl.New ("roxority_PrintZen.PrintZenMenuItem")) != null) && !string.IsNullOrEmpty (onclick = refl.Call (pzItem, "GetRollupClickScript", new Type [] { typeof (IDictionary), typeof (string), typeof (TahoeWebPart), typeof (List<object []>), typeof (List<string>), typeof (Dictionary<string, string>) }, new object [] { printAction, Context.Request.RawUrl, this, filters, andFilters, oobFilterPairs }) as string)) {
				l = int.Parse (onclick.Substring (0, 1));
				pv = new WebPartVerb (ID + "_PrintVerb", onclick.Substring (1));
				pv.Description = ((l == 0) ? "SharePoint-Tools.net/PrintZen" : Replace (printAction ["desc"] + string.Empty, printAction, "PrintZen_PrintAction_", "PrintZen_QueryString_"));
				pv.Enabled = pv.Visible = true;
				pv.ImageUrl = WebUrl + "/_layouts/images/roxority_PrintZen/printer16.png";
				pv.Text = Replace (JsonSchemaManager.GetDisplayName (printAction, "PrintActions", false), printAction, "PrintZen_PrintAction_", "PrintZen_QueryString_");
			}
			return pv;
		}

		public override ToolPart [] GetToolParts () {
			List<ToolPart> toolparts = new List<ToolPart> (base.GetToolParts ());
			if (!IsFrontPage)
				toolparts.Insert (0, new RollupToolPart ());
			return toolparts.ToArray ();
		}

		public override void PartCommunicationConnect (string interfaceName, Microsoft.SharePoint.WebPartPages.WebPart connectedPart, string connectedInterfaceName, ConnectionRunAt runAt) {
			if (connectedPart != null)
				connectedWebPart = connectedPart;
			isConnected = true;
		}

		public override void PartCommunicationInit () {
			OnFilterConsumerInit ((FilterConsumerInitEventArgs) GetInitEventArgs ("roxorityFilterConsumerInterface"));
		}

		public override void PartCommunicationMain () {
			object val;
			bool licEd = false;
			IList list = null;
			PropertyInfo kvpKeyProp = null, kvpValProp = null, fpKeyProp = null, fpValProp = null, fpOpProp = null;
			filters.Clear ();
			andFilters.Clear ();
			try {
				licEd = ((bool) ConnectedWebPart.GetType ().GetMethod ("LicEd", BindingFlags.Instance | BindingFlags.NonPublic).Invoke (ConnectedWebPart, new object [] { 2 }));
			} catch {
				licEd = false;
			}
			if (LicEd (2) && licEd)
				try {
					list = ConnectedWebPart.GetType ().GetProperty ("PartFilters", BindingFlags.Instance | BindingFlags.Public).GetValue (ConnectedWebPart, null) as IList;
					andFilters.AddRange (((string) ConnectedWebPart.GetType ().GetProperty ("CamlFiltersAndCombined", BindingFlags.Instance | BindingFlags.Public).GetValue (ConnectedWebPart, null)).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
				} catch {
				}
			if ((list == null) && (oobFilterPairs.Count > 0) && IsFilterOobConnection && LicEd (4)) {
				list = new List<KeyValuePair<string, string>> ();
				foreach (KeyValuePair<string, string> nv in oobFilterPairs)
					list.Add (nv);
			}
			if (list != null)
				foreach (object kvp in list) {
					if (kvpValProp == null)
						kvpValProp = kvp.GetType ().GetProperty ("Value", BindingFlags.Instance | BindingFlags.Public);
					if (kvpKeyProp == null)
						kvpKeyProp = kvp.GetType ().GetProperty ("Key", BindingFlags.Instance | BindingFlags.Public);
					if ((val = kvpValProp.GetValue (kvp, null)) != null) {
						if (fpKeyProp == null)
							fpKeyProp = val.GetType ().GetProperty ("Key", BindingFlags.Public | BindingFlags.Instance);
						if (fpValProp == null)
							fpValProp = val.GetType ().GetProperty ("Value", BindingFlags.Public | BindingFlags.Instance);
						if (fpOpProp == null)
							fpOpProp = val.GetType ().GetProperty ("CamlOperator", BindingFlags.Public | BindingFlags.Instance);
						if ((fpKeyProp != null) && (fpValProp != null) && (fpOpProp != null))
							filters.Add (new object [] { fpKeyProp.GetValue (val, null), fpValProp.GetValue (val, null), (CamlOperator) Enum.Parse (typeof (CamlOperator), fpOpProp.GetValue (val, null).ToString (), true) });
						else if (kvpKeyProp != null)
							filters.Add (new object [] { kvpKeyProp.GetValue (kvp, null), val, CamlOperator.Eq });
					}
				}
		}

		internal IDictionary ExpInst {
			get {
				if ((expInst == null) && (!string.IsNullOrEmpty (ExportAction)) && !string.IsNullOrEmpty (EzPath))
					foreach (IDictionary inst in JsonSchemaManager.GetInstances (EzPath, "ExportActions", "roxority_ExportZen"))
						if (ExportAction.Equals (inst ["id"] + string.Empty, StringComparison.InvariantCultureIgnoreCase)) {
							expInst = inst;
							break;
						}
				return expInst;
			}
		}

		internal string EzPath {
			get {
				if (ezPath == null) {
					ezPath = string.Empty;
					try {
						ezPath = Context.Server.MapPath ("/_layouts/roxority_ExportZen/schemas.json");
					} catch {
					}
					if ((!string.IsNullOrEmpty (ezPath)) && !File.Exists (ezPath))
						ezPath = string.Empty;
				}
				return ezPath;
			}
		}

		internal string DataSourcePath {
			get {
				if (dsPath == null) {
					dsPath = string.Empty;
					try {
						dsPath = Context.Server.MapPath ("/_layouts/" + ProductPage.AssemblyName + "/schemas.tl.json");
					} catch {
					}
					if ((!string.IsNullOrEmpty (dsPath)) && !File.Exists (dsPath))
						dsPath = string.Empty;
				}
				return dsPath;
			}
		}

		internal IDictionary DataSourceInst {
			get {
				if ((dataInst == null) && (!string.IsNullOrEmpty (DataSourceID)) && !string.IsNullOrEmpty (DataSourcePath))
					foreach (IDictionary inst in JsonSchemaManager.GetInstances (DataSourcePath, "DataSource"))
						if (DataSourceID.Equals (inst ["id"] + string.Empty, StringComparison.InvariantCultureIgnoreCase)) {
							dataInst = inst;
							break;
						}
				return dataInst;
			}
		}

		internal IDictionary PrintInst {
			get {
				if ((printInst == null) && (!string.IsNullOrEmpty (PrintAction)) && !string.IsNullOrEmpty (PzPath))
					foreach (IDictionary inst in JsonSchemaManager.GetInstances (PzPath, "PrintActions", "roxority_PrintZen"))
						if (PrintAction.Equals (inst ["id"] + string.Empty, StringComparison.InvariantCultureIgnoreCase)) {
							printInst = inst;
							break;
						}
				return printInst;
			}
		}

		internal string PzPath {
			get {
				if (pzPath == null) {
					pzPath = string.Empty;
					try {
						pzPath = Context.Server.MapPath ("/_layouts/roxority_PrintZen/schemas.json");
					} catch {
					}
					if ((!string.IsNullOrEmpty (pzPath)) && !File.Exists (pzPath))
						pzPath = string.Empty;
				}
				return pzPath;
			}
		}

		[Personalizable]
		public bool AllowSort {
			get {
				return LicEd (2) && GetProp<bool> ("AllowSort", allowSort);
			}
			set {
				allowSort = LicEd (2) && value;
			}
		}

		[Personalizable]
		public bool AllowView {
			get {
				return LicEd (4) && GetProp<bool> ("AllowView", allowView);
			}
			set {
				allowView = LicEd (4) && value;
			}
		}

		public System.Web.UI.WebControls.WebParts.WebPart ConnectedWebPart {
			get {
				SPWebPartManager wpMan = WebPartManager as SPWebPartManager;
				if ((connectedWebPart == null) && (wpMan != null) && (wpMan.SPWebPartConnections != null) && (wpMan.SPWebPartConnections.Count > 0))
					foreach (SPWebPartConnection conn in wpMan.SPWebPartConnections)
						if ((conn.Consumer == this) && ((connectedWebPart = conn.Provider) != null))
							break;
				return connectedWebPart;
			}
		}

		[Personalizable]
		public bool CurUser {
			get {
				return GetProp<bool> ("CurUser", curUser);
			}
			set {
				curUser = value;
			}
		}

		public DataSource DataSource {
			get {
				if ((dataSource == null) && !string.IsNullOrEmpty (DataSourceID))
					try {
						dataSource = DataSource.FromID (DataSourceID, true, true, null);
					} catch {
					}
				return dataSource;
			}
		}

		[Personalizable]
		public string DataSourceID {
			get {
				return dataSourceID;
			}
			set {
				dataSourceID = value;
			}
		}

		[Personalizable]
		public bool DateIgnoreDay {
			get {
				return LicEd (2) && GetProp<bool> ("DateIgnoreDay", dateIgnoreDay);
			}
			set {
				dateIgnoreDay = LicEd (2) && value;
			}
		}

		[Personalizable]
		public bool DateThisYear {
			get {
				return LicEd (2) && GetProp<bool> ("DateThisYear", dateThisYear);
			}
			set {
				dateThisYear = LicEd (2) && value;
			}
		}

		public bool EffectiveFilterLive {
			get {
				if (NoAjax)
					return false;
				if (!(LicEd (2) && IsFilterZenConnection))
					return false;
				if ((ConnectedWebPart != null) && ((fzHasHiddenProp != null) || ((fzHasHiddenProp = ConnectedWebPart.GetType ().GetProperty ("HasHiddenFilter", BindingFlags.Public | BindingFlags.Instance)) != null)) && (bool) fzHasHiddenProp.GetValue (ConnectedWebPart, null))
					return false;
				if (Context.Request.UserAgent.Contains ("Gecko/") || Context.Request.UserAgent.Contains ("Firefox"))
					return false;
				return GetProp<bool> ("FilterLive", filterLive);
			}
		}

		[Personalizable]
		public string ExportAction {
			get {
				return LicEd (2) ? exportAction : string.Empty;
			}
			set {
				exportAction = LicEd (2) ? value : string.Empty;
			}
		}

		public WebPartVerb ExportVerb {
			get {
				object ezItem;
				string onclick;
				int l;
				Reflector refl = null;
				if ((exportVerb == null) && (ExpInst != null)) {
					try {
						refl = new Reflector (Assembly.Load ("roxority_ExportZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01"));
					} catch {
					}
					if ((refl != null) && ((ezItem = refl.New ("roxority_ExportZen.ExportZenMenuItem")) != null) && !string.IsNullOrEmpty (onclick = refl.Call (ezItem, "GetRollupClickScript", new Type [] { typeof (IDictionary), typeof (string), typeof (TahoeWebPart), typeof (List<object []>), typeof (List<string>), typeof (Dictionary<string, string>) }, new object [] { expInst, Context.Request.RawUrl, this, filters, andFilters, oobFilterPairs }) as string)) {
						l = int.Parse (onclick.Substring (0, 1));
						exportVerb = new WebPartVerb (ID + "_ExportVerb", onclick.Substring (1));
						exportVerb.Description = ((l == 0) ? "SharePoint-Tools.net/ExportZen" : Replace (expInst ["desc"] + string.Empty, expInst, "ExportZen_ExportAction_", "ExportZen_QueryString_"));
						exportVerb.Enabled = exportVerb.Visible = true;
						exportVerb.ImageUrl = WebUrl + "/_layouts/images/roxority_ExportZen/icon16.png";
						exportVerb.Text = Replace (JsonSchemaManager.GetDisplayName (expInst, "ExportActions", false), expInst, "ExportZen_ExportAction_", "ExportZen_QueryString_");
					}
				}
				return exportVerb;
			}
		}

		[Personalizable]
		public bool FilterLive {
			get {
				return LicEd (2) && filterLive;
			}
			set {
				filterLive = LicEd (2) && value;
			}
		}

		public List<object []> Filters {
			get {
#if PEOPLEZEN
				string pn;
				List<object []> list;
				if (CurUser) {
					list = new List<object []> ();
					if ((dataSource == null) || string.IsNullOrEmpty (pn = dataSource.GetKnownPropName (DataSource.KnownProperty.LoginName)))
						pn = "AccountName";
					list.Add (new object [] { pn, ((Context != null) && !string.IsNullOrEmpty (Context.Request [urlPropertyPrefix + "curuser"])) ? Context.Request [urlPropertyPrefix + "curuser"] : ProductPage.LoginName(SPContext.Current.Web.CurrentUser.LoginName), "Eq" });
					return list;
				} else
#endif
				return filters;
			}
		}

		[Personalizable]
		public bool GroupByCounts {
			get {
				return LicEd (4) && GetProp<bool> ("GroupByCounts", groupByCounts);
			}
			set {
				groupByCounts = LicEd (4) && value;
			}
		}

		[Personalizable]
		public bool GroupInteractive {
			get {
				return LicEd (4) && GetProp<bool> ("GroupInteractive", groupInteractive);
			}
			set {
				groupInteractive = LicEd (4) && value;
			}
		}

		[Personalizable]
		public bool GroupInteractiveDir {
			get {
				return LicEd (4) && GetProp<bool> ("GroupInteractiveDir", groupInteractiveDir);
			}
			set {
				groupInteractiveDir = LicEd (4) && value;
			}
		}

		[Personalizable]
		public bool GroupDesc {
			get {
				return LicEd (2) && GetProp<bool> ("GroupDesc", groupDesc);
			}
			set {
				groupDesc = LicEd (2) && value;
			}
		}

		[Personalizable]
		public bool GroupShowCounts {
			get {
				return LicEd (4) && GetProp<bool> ("GroupShowCounts", groupShowCounts);
			}
			set {
				groupShowCounts = LicEd (4) && value;
			}
		}

		[Personalizable]
		public string GroupProp {
			get {
				return LicEd (2) ? GetProp<string> ("GroupProp", groupProp) : string.Empty;
			}
			set {
				groupProp = LicEd (2) ? value : string.Empty;
			}
		}

		[Personalizable]
		public int ImageHeight {
			get {
				return GetProp<int> ("ImageHeight", imageHeight);
			}
			set {
				imageHeight = value;
			}
		}

		public bool IsB {
			get {
				return ProductPage.LicEdition (ProductPage.GetContext (), L, 2);
			}
		}

		public bool IsFilterOobConnection {
			get {
				object wp = ConnectedWebPart;
				return ((wp != null) && wp.GetType ().FullName.StartsWith ("Microsoft.SharePoint.") && wp.GetType ().Assembly.FullName.StartsWith ("Microsoft."));
			}
		}

		public bool IsFilterZenConnection {
			get {
				object wp = ConnectedWebPart;
				return ((wp != null) && wp.GetType ().AssemblyQualifiedName == ProductPage.FILTERZEN_TYPENAME);
			}
		}

		[Personalizable]
		public bool ListStyle {
			get {
				return LicEd (2) && GetProp<bool> ("ListStyle", listStyle);
			}
			set {
				listStyle = LicEd (2) && value;
			}
		}

		[Personalizable]
		public string LoaderAnim {
			get {
				return GetProp<string> ("LoaderAnim", loaderAnim);
			}
			set {
				value = (value + string.Empty).ToLowerInvariant ().Trim ();
				loaderAnim = (((value == "b") || (value == "l")) ? value : "k");
			}
		}

		[Personalizable]
		public int NameMode {
			get {
				return LicEd (2) ? GetProp<int> ("NameMode", nameMode) : 1;
			}
			set {
				nameMode = LicEd (2) ? value : 1;
			}
		}

		public bool NoAjax {
			get {
				if ((noAjax == null) || !noAjax.HasValue)
					noAjax = ProductPage.Config<bool> (ProductPage.GetContext (), "NoAjax");
				return noAjax.Value;
			}
		}

		[Personalizable]
		public int PageMode {
			get {
				return LicEd (4) ? GetProp<int> ("PageMode", pageMode) : 1;
			}
			set {
				pageMode = LicEd (4) ? value : 1;
			}
		}

		[Personalizable]
		public int PageSkipMode {
			get {
				return LicEd (4) ? GetProp<int> ("PageSkipMode", pageSkipMode) : 0;
			}
			set {
				pageSkipMode = LicEd (4) ? value : 0;
			}
		}

		[Personalizable]
		public int PageStepMode {
			get {
				return LicEd (4) ? GetProp<int> ("PageStepMode", pageStepMode) : 1;
			}
			set {
				pageStepMode = LicEd (4) ? value : 1;
			}
		}

		[Personalizable]
		public int PageSize {
			get {
				return (LicEd (2) ? GetProp<int> ("PageSize", pageSize) : 4);
			}
			set {
				pageSize = (LicEd (2) ? ((value < 1) ? 0 : value) : 4);
			}
		}

		[Personalizable]
		public int PictMode {
			get {
				return LicEd (2) ? GetProp<int> ("PictMode", pictMode) : 1;
			}
			set {
				pictMode = LicEd (2) ? value : 1;
			}
		}

		[Personalizable]
		public bool Presence {
			get {
				return LicEd (2) && GetProp<bool> ("Presence", presence);
			}
			set {
				presence = LicEd (2) && value;
			}
		}

		[Personalizable]
		public string PrintAction {
			get {
				return LicEd (2) ? printAction : string.Empty;
			}
			set {
				printAction = LicEd (2) ? value : string.Empty;
			}
		}

		public WebPartVerb PrintVerb {
			get {
				if ((printVerb == null) && (PrintInst != null) && !"n".Equals(printInst["mpz"]))
					printVerb = GetPrintVerb (PrintInst);
				return printVerb;
			}
		}

		[Personalizable]
		public string Properties {
			get {
				if ((props == null) && (DataSource != null))
					props = dataSource ["pd", string.Empty];
				if (props == null)
					props = string.Empty;
				return props;
			}
			set {
				props = (value + string.Empty).Trim ().Trim ('\r', '\n');
			}
		}

		[Personalizable]
		public int RowSize {
			get {
				return GetProp<int> ("RowSize", rowSize);
			}
			set {
				rowSize = ((value < 1) ? 0 : value);
			}
		}

		public bool ServerContext {
			get {
#if WSS
				return false;
#else
				return ((DataSource is UserProfiles) || ((DataSourceInst != null) && typeof (UserProfiles).Name.Equals (DataSourceInst ["t"])));
#endif
			}
		}

		[Personalizable]
		public bool SortDesc {
			get {
				return LicEd (2) && GetProp<bool> ("SortDesc", sortDesc);
			}
			set {
				sortDesc = LicEd (2) && value;
			}
		}

		[Personalizable]
		public string SortProp {
			get {
				return LicEd (2) ? GetProp<string> ("SortProp", sortProp) : string.Empty;
			}
			set {
				sortProp = LicEd (2) ? value : string.Empty;
			}
		}

		[Personalizable]
		public bool TabInteractive {
			get {
				return LicEd (4) && GetProp<bool> ("TabInteractive", tabInteractive);
			}
			set {
				tabInteractive = LicEd (4) && value;
			}
		}

		[Personalizable]
		public string TabProp {
			get {
				return LicEd (2) ? GetProp<string> ("TabProp", tabProp) : string.Empty;
			}
			set {
				tabProp = LicEd (2) ? value : string.Empty;
			}
		}

		[Personalizable]
		public string TileWidth {
			get {
				return GetProp<string> ("TileWidth", tileWidth);
			}
			set {
				tileWidth = value;
			}
		}

		[Personalizable]
		public override bool UrlSettings {
			get {
				return LicEd (4) && base.UrlSettings;
			}
			set {
				base.UrlSettings = LicEd (4) && value;
			}
		}

		[Personalizable]
		public bool Vcard {
			get {
				return LicEd (4) && GetProp<bool> ("Vcard", vcard);
			}
			set {
				vcard = LicEd (4) && value;
			}
		}

		public override WebPartVerbCollection Verbs {
			get {
				WebPartVerbCollection baseVerbs = base.Verbs;
				WebPartVerb expVerb = ExportVerb, printVerb = PrintVerb;
				List<WebPartVerb> verbs = new List<WebPartVerb> ();
				if (baseVerbs != null)
					foreach (WebPartVerb v in baseVerbs)
						verbs.Add (v);
				if (printVerb != null)
					verbs.Add (printVerb);
				if (expVerb != null)
					verbs.Add (expVerb);
				return new WebPartVerbCollection (verbs);
			}
		}

		public string WebUrl {
			get {
				if (webUrl == null)
					try {
						webUrl = SPContext.Current.Web.Url.TrimEnd ('/');
					} catch {
						webUrl = string.Empty;
					}
				return webUrl;
			}
		}

	}

}
