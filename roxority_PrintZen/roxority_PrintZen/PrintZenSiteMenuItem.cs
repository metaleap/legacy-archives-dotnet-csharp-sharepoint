
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using roxority.Shared;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

using SystemWebPart = System.Web.UI.WebControls.WebParts.WebPart;
using TahoeWebPart = Microsoft.SharePoint.WebPartPages.WebPart;

namespace roxority_PrintZen {

	public class PrintZenSiteMenuItem : WebControl {

		public LiteralControl Literal = null;

		internal static readonly Reflector webRefl = new Reflector (typeof (WebPartVerbCollection).Assembly);

		internal static IEnumerable<Control> GetControls (ControlCollection ctls, bool allRecursive, params Type [] blockTypes) {
			bool isBlocked;
			IEnumerable<Control> subs;
			foreach (Control ctl in ctls) {
				isBlocked = false;
				foreach (Type bt in blockTypes)
					if (isBlocked = bt.IsAssignableFrom (ctl.GetType ()))
						break;
				if (!isBlocked) {
					yield return ctl;
					if (allRecursive && ((subs = GetControls (ctl.Controls, true)) != null))
						foreach (Control sub in subs)
							yield return sub;
				}
			}
		}

		internal static string GetPartDesc (SystemWebPart part, Reflector refl) {
			SPView view;
			if (!string.IsNullOrEmpty (part.Description))
				return part.Description;
			if ((view = refl.Get (part, (part is ListViewWebPart) ? "View" : "ContextView") as SPView) != null)
				return (view.ParentList.Title.Equals (part.DisplayTitle) ? string.Empty : (view.ParentList.Title + HttpUtility.HtmlDecode (" &mdash; "))) + (string.IsNullOrEmpty (view.Title) ? ProductPage.GetProductResource ("CustomView") : view.Title);
			return part.GetType ().Name;
		}

		internal static string GetZoneDesc (WebPartZoneBase zone) {
			SystemWebPart [] parts = new SystemWebPart [zone.WebParts.Count];
			zone.WebParts.CopyTo (parts, 0);
			Array.Sort<SystemWebPart> (parts, delegate (SystemWebPart one, SystemWebPart two) {
				int c = one.DisplayTitle.Length.CompareTo (two.DisplayTitle.Length);
				return ((c == 0) ? one.DisplayTitle.CompareTo (two.DisplayTitle) : c);
			});
			return string.Join (" • ", Array.ConvertAll<SystemWebPart, string> (parts, (wp) => {
				return wp.DisplayTitle;
			}));
		}

		internal static bool IsPartSupported (IDictionary action, SystemWebPart wp, bool isPeop, SPList list, out bool origPage) {
			string flag;
			Type wpType = null;
			try {
				wpType = wp.GetType ();
			} catch {
			}
			if ((wpType != null) && ("Microsoft.SharePoint.WebPartPages.XsltListFormWebPart".Equals (wpType.FullName) || (wp is ListFormWebPart))) {
				origPage = "o".Equals (flag = action ["mlf"] + string.Empty);
				return !"n".Equals (flag);
			}
			if (list != null) {
				origPage = "o".Equals (flag = action [(list.BaseTemplate == SPListTemplateType.Events) ? "mcv" : "mlv"] + string.Empty);
				return !"n".Equals (flag);
			}
			if (isPeop) {
				origPage = "o".Equals (flag = action ["mpz"] + string.Empty);
				return !"n".Equals (flag);
			}
			origPage = "o".Equals (flag = action ["mwp"] + string.Empty);
			return (!(wp is TitleBarWebPart)) && !"n".Equals (flag);
		}

		internal static OrderedDictionary MakeSubControl (string id, string title, string desc, string click, string img, IDictionary items) {
			bool hasL = ProductPage.LicEdition (ProductPage.GetContext (), (ProductPage.LicInfo) null, 0);
			OrderedDictionary ctl = new OrderedDictionary ();
			ctl ["title"] = hasL ? title : ProductPage.GetResource ("LicExpiry");
			ctl ["id"] = id;
			ctl ["desc"] = hasL ? (ProductPage.LicEdition (ProductPage.GetContext (), (ProductPage.LicInfo) null, 2) ? desc : "SharePoint-Tools.net/PrintZen") : ProductPage.GetResource ("LicStudio", ProductPage.GetTitle ());
			ctl ["click"] = hasL ? click : ("location.href='" + SPContext.Current.Web.Url.TrimEnd ('/') + "/_layouts/" + ProductPage.AssemblyName + "/default.aspx?cfg=lic&r=" + new Random ().Next () + "';");
			ctl ["items"] = items;
			ctl ["img"] = img;
			return ctl;
		}

		public static IDictionary GetControls (HttpContext context, Page page, string ownerID, string check, string siteUrl, Control parent) {
			string thisID, clickScript, fj = string.Empty;
			bool isSep1, isSep2, hasSep1, hasSep2, includeFilters = false, isPeop, origPage, doZones;
			int zoneCount;
			List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters = null;
			Type [] types = new Type [] { typeof (WebPartVerb) }, rollTypes = new Type [] { typeof (IDictionary) };
			Type wpType;
			OrderedDictionary dict = new OrderedDictionary (), ctl, items = null, actionItem, actionItems = null;
			KeyValuePair<JsonSchemaManager, JsonSchemaManager> jsonMans;
			JsonSchemaManager.Schema ctlSchema, actSchema;
			WebPartManager wpMan = WebPartManager.GetCurrentWebPartManager (page);
			Reflector refl;
			SPList list;
			List<SystemWebPart> parts;
			IEnumerable<SystemWebPart> partsEnum;
			WebPartVerb wpVerb;
			using (ProductPage ppage = new ProductPage ()) {
				jsonMans = JsonSchemaManager.TryGet (ppage, null, true, true, null);
				foreach (JsonSchemaManager jman in new JsonSchemaManager [] { jsonMans.Key, jsonMans.Value })
					if (jman != null) {
						if (((ctlSchema = jman.AllSchemas ["PrintControls"]) != null) && ((actSchema = jman.AllSchemas ["PrintActions"]) != null))
							foreach (IDictionary launcher in ctlSchema.Instances) if (launcher != null) {
									if (JsonSchemaManager.Bool (launcher [check], true)) {
										ctl = MakeSubControl (ownerID + "_" + launcher ["id"], JsonSchemaManager.GetDisplayName (launcher, ctlSchema.Name, false), ctlSchema.GetInstanceDescription (launcher), string.Empty, siteUrl + "/_layouts/images/roxority_PrintZen/printer32.png", items = new OrderedDictionary ());
										foreach (IDictionary action in actSchema.Instances) if (action != null) {
											items [thisID = (ctl ["id"] + "_" + action ["id"])] = actionItem = MakeSubControl (thisID, JsonSchemaManager.GetDisplayName (action, "PrintActions", false), ctlSchema.GetInstanceDescription (action), string.Empty, siteUrl + "/_layouts/images/lg_icxps.gif", actionItems = new OrderedDictionary ());
												hasSep1 = hasSep2 = isSep1 = isSep2 = false;
												zoneCount = -1;
												if (wpMan != null) {
													if (hasSep2 = hasSep1 = (JsonSchemaManager.Bool (launcher ["pp"], true) && !"n".Equals (action ["mpp"])))
														actionItems [thisID + "_pp"] = MakeSubControl (thisID + "_pp", ProductPage.GetProductResource ("PrintPageContent"), ProductPage.GetProductResource ("PrintPageContentDesc"), clickScript = PrintZenMenuItem.GetClickScript (context, ownerID, siteUrl, string.Empty, action, SPContext.Current.Web, false, false, null, null, true, false, null, null, Guid.Empty, null, page, false), siteUrl + "/_layouts/images/lg_icgen.gif", null);
													if (JsonSchemaManager.Bool (launcher ["pw"], true) && ((partsEnum = ProductPage.TryEach<SystemWebPart> (wpMan.WebParts)) != null)) {
														parts = new List<SystemWebPart> (partsEnum);
														parts.Sort ((wp1, wp2) => {
															WebPartZoneBase zone1, zone2;
															if ((wp1 == null) && (wp2 == null))
																return 0;
															if (wp1 == null)
																return -1;
															if (wp2 == null)
																return 1;
															if (((zone1 = wp1.Zone) == null) || ((zone2 = wp2.Zone) == null))
																return ((wp1.ZoneIndex == wp2.ZoneIndex) ? wp1.TabIndex.CompareTo (wp2.TabIndex) : wp1.ZoneIndex.CompareTo (wp2.ZoneIndex));
															return ((zone1.TabIndex == zone2.TabIndex) ? wp1.ZoneIndex.CompareTo (wp2.ZoneIndex) : zone1.TabIndex.CompareTo (zone2.TabIndex));
														});
														foreach (SystemWebPart wp in parts) {
															refl = new Reflector ((wpType = wp.GetType ()).Assembly);
															list = (("Microsoft.SharePoint.WebPartPages.XsltListViewWebPart".Equals (wpType.FullName) || (wp is ListViewWebPart)) ? ((wp is ListViewWebPart) ? (refl.Get (wp, "List") as SPList) : ((SPView) refl.Get (wp, "ContextView")).ParentList) : null);
															isPeop = ((wpType.FullName == "roxority_RollupZen.RollupWebPart") || (wpType.FullName == "roxority_PeopleZen.roxority_UserListWebPart"));
															if (IsPartSupported (action, wp, isPeop, list, out origPage)) {
																if (hasSep1 && !isSep1) {
																	isSep1 = true;
																	actionItems [thisID + "_sep1"] = null;
																}
																hasSep2 = true;
																if (isPeop && (!origPage) && ((wpVerb = refl.Call (wp, "GetPrintVerb", rollTypes, new object [] { action }) as WebPartVerb) != null))
																	clickScript = wpVerb.ClientClickHandler + string.Empty;
																else if ((list != null) && !origPage) {
																	clickScript = string.Empty;
																	PrintZenMenuItem.GetFilterInfo (action, "PrintActions", ref clickScript, wp, page, ref includeFilters, ref fj, ref effectiveFilters);
																	if (string.IsNullOrEmpty (clickScript))
																		clickScript = PrintZenMenuItem.GetClickScript (context, ownerID, siteUrl, string.Empty, action, SPContext.Current.Web, JsonSchemaManager.Bool (action ["view"], true), includeFilters, effectiveFilters, fj, true, false, list, null, ProductPage.GetGuid (refl.Get (wp, "ViewGuid") + string.Empty), PrintZenMenuItem.GetAllPageParams (context, true, null), parent, origPage);
																} else
																	clickScript = PrintZenMenuItem.GetClickScript (context, ownerID, siteUrl, string.Empty, action, SPContext.Current.Web, false, false, null, null, true, false, null, null, ProductPage.GetGuid (wp.ID.Substring (2).Replace ('_', '-')), PrintZenMenuItem.GetAllPageParams (context, true, null), parent, origPage);
																actionItems [thisID + "_" + wp.ID] = MakeSubControl (thisID + "_" + wp.ID, wp.DisplayTitle, GetPartDesc (wp, refl), clickScript, siteUrl + "/_layouts/roxority_PrintZen/mash.tl.aspx?op=imgoverlay&backimg=" + HttpUtility.UrlEncode ("/_layouts/images/roxority_PrintZen/printer32.png") + "&overlay=" + HttpUtility.UrlEncode (string.IsNullOrEmpty (wp.TitleIconImageUrl) ? (string.IsNullOrEmpty (wp.CatalogIconImageUrl) ? "/_layouts/images/itobject.gif" : wp.CatalogIconImageUrl) : wp.TitleIconImageUrl) + "&r=" + ppage.Rnd.Next (), null);
															}
														}
													}
													if (doZones = (JsonSchemaManager.Bool (launcher ["pz"], true) && (!"n".Equals (action ["mwz"])) && ((!JsonSchemaManager.Bool (launcher ["pp"], true)) || (((zoneCount = wpMan.Zones.Count) > 1)))))
														if (zoneCount > 0) {
															zoneCount = 0;
															foreach (WebPartZoneBase zone in ProductPage.TryEach<WebPartZoneBase> (wpMan.Zones))
																if (zone.WebParts.Count > (JsonSchemaManager.Bool (launcher ["pw"], true) ? 1 : 0))
																	zoneCount++;
														}
													if (doZones && ((zoneCount < 0) || (zoneCount > 1)))
														foreach (WebPartZoneBase zone in ProductPage.TryEach<WebPartZoneBase> (wpMan.Zones))
															if (zone.WebParts.Count > (JsonSchemaManager.Bool (launcher ["pw"], true) ? 1 : 0)) {
																if (hasSep2 && !isSep2) {
																	isSep2 = true;
																	actionItems [thisID + "_sep2"] = null;
																}
																clickScript = PrintZenMenuItem.GetClickScript (context, ownerID, siteUrl, string.Empty, action, SPContext.Current.Web, false, false, null, null, true, false, null, null, Guid.Empty, null, zone, false);
																actionItems [thisID + "_" + zone.ID] = MakeSubControl (thisID + "_" + zone.ID, ProductPage.GetProductResource ("WebPartZone", zone.DisplayTitle), GetZoneDesc (zone), clickScript, siteUrl + "/_layouts/images/lg_icgen.gif", null);
															}
												}
												if (actionItems.Count == 0)
													items.Remove (thisID);
											}
										if (items.Count > 0)
											dict [launcher ["id"]] = ctl;
									}
									if ((items != null) && (items.Count == 1)) {
										foreach (DictionaryEntry entry in actionItems)
											items [entry.Key] = entry.Value;
										items.RemoveAt (0);
									}
								}
					}
			}
			return dict;
		}

		internal void AddMenuItems (ControlCollection controls, IDictionary ctls) {
			string tmp, img;
			IDictionary ctl, subCtls;
			MenuItemTemplate menuItem;
			SubMenuTemplate subMenu;
			foreach (DictionaryEntry entry in ctls)
				if ((ctl = entry.Value as IDictionary) != null) {
					if (((subCtls = ctl ["items"] as IDictionary) != null) && (subCtls.Count > 0)) {
						subMenu = new SubMenuTemplate () {
							ID = entry.Key + string.Empty,
							Description = ctl ["desc"] + string.Empty,
							Text = ctl ["title"] + string.Empty
						};
						if (!string.IsNullOrEmpty (img = ctl ["img"] + string.Empty))
							subMenu.ImageUrl = img;
						AddMenuItems (subMenu.Controls, subCtls);
						controls.Add (subMenu);
					} else if (!string.IsNullOrEmpty (tmp = ctl ["click"] + string.Empty)) {
						menuItem = new MenuItemTemplate (ctl ["title"] + string.Empty) {
							Description = ctl ["desc"] + string.Empty,
							ID = entry.Key + string.Empty
						};
						if (!string.IsNullOrEmpty (img = ctl ["img"] + string.Empty))
							menuItem.ImageUrl = img;
						menuItem.ClientOnClickScript = tmp;
						controls.Add (menuItem);
					}
				} else
					controls.Add (new MenuSeparatorTemplate () {
						ID = entry.Key + string.Empty
					});
		}

		protected override void CreateChildControls () {
			string siteUrl = SPContext.Current.Web.Url.TrimEnd ('/'), printID;
			bool hasPart;
			WebPartManager wpMan;
			if (string.IsNullOrEmpty (Context.Request.QueryString ["roxPrintZen"]))
				AddMenuItems (Controls, GetControls (Context, Page, ID, "sam", siteUrl, Parent));
			else {
				//Page.ClientScript.RegisterClientScriptBlock (typeof (PrintZenSiteMenuItem), "roxPrintZenCss", "<link type=\"text/css\" rel=\"stylesheet\" href=\"" + siteUrl + "/_layouts/" + ProductPage.AssemblyName + "/" + ProductPage.AssemblyName + ".css?r=" + "&v=" + ProductPage.Version + "\"/>" + (Context.Request.UserAgent.Contains ("MSIE") ? "<style type=\"text/css\"> div.rox-prz-toolbar button { text-align: left; padding-left: 18px; } </style><script type=\"text/javascript\" language=\"JavaScript\"> roxIsPrintPage = true; roxDlgOnLoad = " + "1".Equals (Context.Request.QueryString ["roxDlgShow"]).ToString ().ToLowerInvariant () + "; " + (hasL ? string.Empty : "setInterval(function() { jQuery('div.ms-bodyareacell').hide(); }, 250);") + " </script>" : string.Empty), false);
				//if (hasL && ((ct = ProductPage.FindControl (Page.Controls, "MSO_ContentDiv") as HtmlGenericControl) != null)) {
				//    foreach (Control subCt in ct.Controls)
				//        if (done = ((lc = subCt as LiteralControl) != null)) {
				//            lc.Text = html + lc.Text;
				//            break;
				//        }
				//    if (!done)
				//        ct.Controls.AddAt (0, new LiteralControl (html));
				//} else
				//    foreach (Control ctl in GetControls (Page.Form.Controls, !hasL))
				//        if ((!hasL) && ((wc = ctl as WebControl) != null)) {
				//            wc.Style [HtmlTextWriterStyle.Display] = "none";
				//            wc.Style [HtmlTextWriterStyle.Visibility] = "hidden";
				//            wc.Style [HtmlTextWriterStyle.Height] = "1px";
				//            wc.Style [HtmlTextWriterStyle.Overflow] = "hidden";
				//        } else if (hasL && ((lc = ctl as LiteralControl) != null)) {
				//            lc.Text = html + lc.Text;
				//            break;
				//        }
				if ((!"__roxPage".Equals (printID = Context.Request.QueryString ["roxPrintZen"], StringComparison.InvariantCultureIgnoreCase)) && ((wpMan = WebPartManager.GetCurrentWebPartManager (Page)) != null))
					foreach (WebPartZoneBase zone in wpMan.Zones)
						if (printID.StartsWith ("g_")) {
							hasPart = false;
							foreach (SystemWebPart wp in ProductPage.TryEach<SystemWebPart> (zone.WebParts))
								if (hasPart = wp.ID.Equals (printID, StringComparison.InvariantCultureIgnoreCase))
									break;
							if (!hasPart) {
								zone.Visible = false;
							} else
								foreach (SystemWebPart wp in ProductPage.TryEach<SystemWebPart> (zone.WebParts))
									if (!wp.ID.Equals (printID, StringComparison.InvariantCultureIgnoreCase))
										wp.Visible = false;
									else
										Page.ClientScript.RegisterClientScriptBlock (typeof (PrintZenSiteMenuItem), "roxPrintPagePartTitle", "jQuery(document).ready(function() { document.title = '" + SPEncode.ScriptEncode (wp.DisplayTitle) + "'; });", true);
						} else if (zone.ID != printID)
							zone.Visible = false;
			}
			base.CreateChildControls ();
		}

		protected override void OnInit (EventArgs e) {
			string tmp = string.Empty, siteUrl = SPContext.Current.Web.Url.TrimEnd ('/'), html, tmpCssClass = "c" + ProductPage.GuidLower (Guid.NewGuid (), false);
			bool hasL = ProductPage.LicEdition (ProductPage.GetContext (), (ProductPage.LicInfo) null, 0), is14Popup = "1".Equals (Context.Request.QueryString ["roxDlg"]);
			Random rnd = new Random ();
			if (!(ProductPage.Config<bool> (null, "_nojquery") || Page.Items.Contains ("jquery"))) {
				Page.Items ["jquery"] = new object ();
				Page.ClientScript.RegisterClientScriptInclude ("jquery", siteUrl + "/_layouts/" + ProductPage.AssemblyName + "/jQuery.js?v=" + ProductPage.Version);
			}
			Page.ClientScript.RegisterClientScriptInclude (ProductPage.AssemblyName, siteUrl + "/_layouts/" + ProductPage.AssemblyName + "/" + ProductPage.AssemblyName + ".js?r=" + "&v=" + ProductPage.Version);
			base.OnInit (e);
			if (!string.IsNullOrEmpty (Context.Request.QueryString ["roxPrintZen"])) {
				html = "<div class=\"rox-prz-toolbar\"><button type=\"button\" onclick=\"roxDoPrint();\">" + (is14Popup ? "&nbsp;&nbsp;" : string.Empty) + ProductPage.GetProductResource ("ToolBar_PrintNow" + (is14Popup ? "Ctx" : string.Empty)) + "</button>&nbsp;<span>" + ProductPage.GetProductResource ("ToolBar_PrintPreview" + (is14Popup ? "Ctx" : string.Empty));
				if (is14Popup)
					html += (" | Zoom: <select id=\"roxpzzoomer\" onchange=\"roxPrintZoom(this.options[this.selectedIndex].value);\"><option value=\"100\">100%</option><option value=\"120\">120%</option><option value=\"150\">150%</option><option value=\"200\">200% " + ProductPage.GetResource ("Recommended") + "</option></select>");
				html += "</span></div>";
				if (!ProductPage.LicEdition (ProductPage.GetContext (), (ProductPage.LicInfo) null, 2))
					html += ("<div class=\"" + tmpCssClass + "\">Powered by <b>SharePoint-Tools.net/PrintZen</b></div><style type=\"text/css\"> ." + tmpCssClass + " { text-align: center; font-size: " + rnd.Next (24, 37) + "px; padding-bottom: 12px; display: none; } @media print { div." + tmpCssClass + " { display: block !important; } } </style>");
				tmp += "<link type=\"text/css\" rel=\"stylesheet\" href=\"" + siteUrl + "/_layouts/" + ProductPage.AssemblyName + "/" + ProductPage.AssemblyName + ".css?r=" + "&v=" + ProductPage.Version + "\"/>";
				tmp += "<style type=\"text/css\"> div.rox-prz-toolbar button { padding-left: 18px; padding-right: 18px; } </style>";
				tmp += "<script> roxPrintPage = '" + SPEncode.ScriptEncode (html) + "'; roxDlgOnLoad = " + "1".Equals (Context.Request.QueryString ["roxDlgShow"]).ToString ().ToLowerInvariant () + "; " + (hasL ? string.Empty : "setInterval(function() { jQuery('div.ms-bodyareacell').hide(); }, 250);") + " </script>";
				Page.ClientScript.RegisterClientScriptInclude (ProductPage.AssemblyName + "2", siteUrl + "/_layouts/" + ProductPage.AssemblyName + "/mash.tl.aspx?op=r&r=" + rnd.Next (100, 1000) + "&o=" + Context.Server.UrlEncode (tmp));
			}
		}

	}

}

namespace roxority.SharePoint.JsonSchemaPropertyTypes {

}
