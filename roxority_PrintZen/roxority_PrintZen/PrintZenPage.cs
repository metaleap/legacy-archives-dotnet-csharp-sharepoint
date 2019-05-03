
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using roxority.Shared;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml;

using SystemWebPart = System.Web.UI.WebControls.WebParts.WebPart;
using TahoeWebPart = Microsoft.SharePoint.WebPartPages.WebPart;

namespace roxority_PrintZen {

	public class PrintZenPage : Page {

		private static readonly Reflector reflector = new Reflector (typeof (SPView).Assembly);

		private static Assembly exAsm = null;
		private static bool exAsmTried = false;
		private static Reflector exRefl = null;
		private static Type exType = null;

		public readonly Dictionary<string, string> PhVals = new Dictionary<string, string> ();

		public Label theLit = null;
		public Panel FooterWebParts, HeaderWebParts, MainWebPart;
		public bool AutoPrint = false, Placeholders = false, ShowPlaceholders = false, ShowToolbar = true;
		public List<Exception> Errors = new List<Exception> ();

		private ICollection<IDictionary> actions = null;
		private SPSite site = null, site2 = null;
		private SPWeb web = null, web2 = null;
		private SPLimitedWebPartManager wpMan = null, wpMan2 = null;
		private SPContext ctx = null;

		internal static SPQuery BuildQuery (SPView view, Dictionary<string, string> viewAttributes, IEnumerable fields) {
			SPQuery query;
			XmlDocument doc = new XmlDocument ();
			string viewFieldFormat = ProductPage.FORMAT_CAML_VIEWFIELD, viewFields = string.Empty;
			query = new SPQuery (view);
			foreach (string f in fields)
				if (f != "Attachments")
					viewFields += string.Format (viewFieldFormat, f);
			query.ViewFields = viewFields;
			query.AutoHyperlink = query.ItemIdQuery = false;
			query.ExpandUserField = query.ExpandRecurrence = query.IndividualProperties = query.IncludePermissions = query.IncludeMandatoryColumns = query.IncludeAttachmentVersion = query.IncludeAttachmentUrls = query.IncludeAllUserPermissions = query.RecurrenceOrderBy = true;
			if (viewAttributes.Count > 0) {
				doc.LoadXml ("<View " + query.ViewAttributes + "/>");
				foreach (XmlAttribute att in doc.DocumentElement.Attributes)
					if (!viewAttributes.ContainsKey (att.LocalName))
						viewAttributes [att.LocalName] = att.Value;
				doc.LoadXml ("<View />");
				foreach (KeyValuePair<string, string> kvp in viewAttributes)
					doc.DocumentElement.Attributes.SetNamedItem (doc.CreateAttribute (kvp.Key)).Value = kvp.Value;
				query.ViewAttributes = doc.DocumentElement.OuterXml.Substring (5, doc.DocumentElement.OuterXml.Length - 8).Trim ();
			}
			return query;
		}

		public static string PhName (string phName) {
			return "{$PrintZen_" + phName + "$}";
		}

		public static string [] CssUrls {
			get {
				return ProductPage.Config (ProductPage.GetContext (), "CssLinks").Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			}
		}

		internal ICollection<IDictionary> Actions {
			get {
				if (actions == null)
					actions = JsonSchemaManager.GetInstances (Server.MapPath ("/_layouts/roxority_PrintZen/schemas.json"), "PrintActions");
				return actions;
			}
		}

		internal void AddListFormWebPart (IDictionary action, int itemID) {
			SPList list;
			SPListItem item;
			ListFormWebPart lfwp;
			try {
				list = web.Lists [new Guid (Request.QueryString ["l"])];
				PhVals ["List_Title"] = list.Title;
				if (string.IsNullOrEmpty (Title))
					Title = list.Title;
				item = list.GetItemById (itemID);
				string itemTitle = ProductPage.GetListItemTitle (item, false);
				PhVals ["Context_Title"] = PhVals ["Item_Title"] = itemTitle;
				if ((!string.IsNullOrEmpty (itemTitle)) && (itemTitle != Title))
					Title = ((string.IsNullOrEmpty (Title) ? string.Empty : (Title + " - ")) + (string.IsNullOrEmpty (itemTitle) ? ("#" + item.ID) : itemTitle));
				InitWebPart (lfwp = new ListFormWebPart ());
				lfwp.ControlMode = SPControlMode.Display;
				lfwp.FormType = 4;
				lfwp.ItemContext = SPContext.GetContext (Context, itemID, list.ID, web);
				lfwp.ListName = ProductPage.GuidBracedUpper (list.ID);
				lfwp.ListItemId = itemID;
				lfwp.Toolbar = "None";
				MainWebPart.Controls.Add (lfwp);
				PhVals ["WebPart_Title"] = lfwp.DisplayTitle;
			} catch (Exception ex) {
				Errors.Add (ex);
			}
		}

		internal void AddListViewWebPart (IDictionary action, Guid viewID, ArrayList filters, Hashtable fht) {
			string viewXml, paging, sortProp = string.Empty, sortOrder = string.Empty;
			bool expGrps = false;
			int itemID;
			SPList list;
			SPView view = null;
			SPQuery query;
			SPListItem item;
			SPField sortField;
			XmlNode node;
			XmlDocument doc = new XmlDocument ();
			XmlAttribute att;
			SPFolder folder = null;
			Dictionary<string, string> viewAtts = new Dictionary<string, string> ();
			ViewToolBar viewToolBar;
			ListViewWebPart lvwp;
			try {
				list = web.Lists [new Guid (Request.QueryString ["l"])];
				PhVals ["List_Title"] = list.Title;
				if (string.IsNullOrEmpty (Title))
					Title = list.Title;
				try {
					view = list.Views [viewID];
				} catch {
				}
				if (view == null)
					view = list.DefaultView;
				PhVals ["View_Title"] = view.Title;
				PhVals ["Context_Title"] = list.Title + ((string.IsNullOrEmpty (list.Title) || string.IsNullOrEmpty (view.Title)) ? string.Empty : " - ") + view.Title;
				if ((!string.IsNullOrEmpty (view.Title)) && (view.Title != Title))
					Title = Title + ((string.IsNullOrEmpty (Title) || string.IsNullOrEmpty (view.Title)) ? string.Empty : " - ") + view.Title;
				if ((filters != null) && (filters.Count == 0))
					filters = null;
				if (ProductPage.Is14) {
					reflector.Set (view, "InlineEdit", "false");
					reflector.Set (view, "TabularView", false);
				}
				viewAtts ["FailIfEmpty"] = "FALSE";
				viewAtts ["PageType"] = "DIALOGVIEW";
				viewAtts ["RequiresClientIntegration"] = "FALSE";
				viewAtts ["Threaded"] = "FALSE";
				viewAtts ["Scope"] = action ["f"] + string.Empty;
				query = BuildQuery (view, viewAtts, view.ViewFields);
				if (!string.IsNullOrEmpty (Request.QueryString ["RootFolder"]))
					try {
						folder = list.RootFolder.SubFolders [Request.QueryString ["RootFolder"]];
					} catch {
					}
				query.Folder = ((folder == null) ? list.RootFolder : folder);
				sortProp = Request.QueryString ["SortField"] + string.Empty;
				sortOrder = Request.QueryString ["SortDir"] + string.Empty;
				if (!string.IsNullOrEmpty (sortProp)) {
					doc.LoadXml ("<Query>" + query.Query + "</Query>");
					if ((node = doc.DocumentElement.SelectSingleNode ("OrderBy")) == null)
						node = doc.DocumentElement.AppendChild (doc.CreateElement ("OrderBy"));
					node.InnerXml = "<FieldRef Ascending=\"" + ("desc".Equals (sortOrder, StringComparison.InvariantCultureIgnoreCase) ? "FALSE" : "TRUE") + "\" Name=\"" + sortProp + "\"/>";
					query.Query = doc.DocumentElement.InnerXml;
				}
				if ((string.IsNullOrEmpty (sortProp) || string.IsNullOrEmpty (sortOrder)) && !string.IsNullOrEmpty (query.Query)) {
					doc.LoadXml ("<Query>" + query.Query + "</Query>");
					if ((node = doc.SelectSingleNode ("/Query/OrderBy/FieldRef")) != null) {
						if (string.IsNullOrEmpty (sortProp) && ((att = node.Attributes.GetNamedItem ("Name") as XmlAttribute) != null))
							sortProp = att.Value;
						if (string.IsNullOrEmpty (sortOrder) && ((att = node.Attributes.GetNamedItem ("Ascending") as XmlAttribute) != null))
							sortProp = ("true".Equals (att.Value, StringComparison.InvariantCultureIgnoreCase) ? "asc" : "desc");
					}
				}
				if (string.IsNullOrEmpty (sortProp))
					sortProp = "Order";
				if (string.IsNullOrEmpty (sortOrder))
					sortOrder = "asc";
				sortField = ProductPage.GetField (list, sortProp);
				if (PrintZenMenuItem.GetBool (action, "p") && !string.IsNullOrEmpty (paging = string.Join ("&", new List<string> (PrintZenMenuItem.GetAllPageParams (Context, true, "p_" + sortProp)).ConvertAll<string> ((pname) => {
					try {
						if (pname.Equals ("p_" + sortProp, StringComparison.InvariantCultureIgnoreCase) && string.IsNullOrEmpty (Request.QueryString [pname]) && (sortField != null) && int.TryParse (Request.QueryString ["p_ID"], out itemID) && ((item = list.GetItemById (itemID)) != null))
							return pname + "=" + HttpUtility.UrlEncode (item [sortField.Id] + string.Empty);
					} catch {
					}
					return pname + "=" + HttpUtility.UrlEncode (Request.QueryString [pname]);
				}).ToArray ()).Trim ())) {
					query.RowLimit = view.RowLimit;
					query.ListItemCollectionPosition = new SPListItemCollectionPosition (paging);
				} else
					query.RowLimit = 0;
				if (filters != null) {
					doc.LoadXml (viewXml = view.SchemaXml);
					if (!string.IsNullOrEmpty (viewXml = ProductPage.ApplyCore (list, viewXml, doc, filters, ref expGrps, false, fht, null))) {
						doc.LoadXml (viewXml);
						if ((node = doc.DocumentElement.SelectSingleNode ("Query")) != null)
							query.Query = node.InnerXml;
					}
				}
				if (ProductPage.Is14)
					reflector.Set (query, "ViewFieldsOnly", true);
				InitWebPart (lvwp = new ListViewWebPart ());
				lvwp.ID = "ListViewWebPart";
				lvwp.WebId = web.ID;
				reflector.Set (lvwp, "web", web);
				lvwp.ListName = ProductPage.GuidBracedUpper (list.ID);
				reflector.Set (lvwp, "list", list);
				lvwp.ViewFlag = 9;
				lvwp.ViewContentTypeId = view.ContentTypeId.ToString ();
				lvwp.ViewType = (ViewType) Enum.Parse (typeof (ViewType), view.Type, true);
				lvwp.ViewGuid = ProductPage.GuidBracedUpper (view.ID);
				reflector.Set (lvwp, "view", view);
				lvwp.ListViewXml = query.ViewXml;
				if (!string.IsNullOrEmpty (Request.QueryString ["RootFolder"]))
					reflector.Set (lvwp, "rootFolder", Request.QueryString ["RootFolder"]);
				if (!string.IsNullOrEmpty (Request.QueryString ["FolderCTID"]))
					reflector.Set (lvwp, "folderCtId", Request.QueryString ["FolderCTID"]);
				MainWebPart.Controls.Add (lvwp);
				PhVals ["WebPart_Title"] = lvwp.DisplayTitle;
				if ((viewToolBar = reflector.Get (lvwp, "ToolbarControl") as ViewToolBar) != null)
					viewToolBar.Visible = false;
			} catch {
			}
		}

		internal void AddOtherWebPart (string pageUrl, string wpID) {
			Guid wpGuid = ProductPage.GetGuid (wpID.Replace ('_', '-').Substring (2));
			SystemWebPart wp = null;
			wpMan2 = web.GetLimitedWebPartManager (pageUrl, PersonalizationScope.Shared);
			try {
				wp = wpMan2.WebParts [wpID];
			} catch (Exception ex) {
				if (Guid.Empty.Equals (wpGuid))
					Errors.Add (ex);
				else
					try {
						wp = wpMan2.WebParts [wpGuid];
					} catch (Exception ex2) {
						Errors.Add (ex2);
					}
			}
			if (wp != null) {
				if (string.IsNullOrEmpty (Title))
					Title = wp.Title;
				InitWebPart (wp);
				MainWebPart.Controls.Add (wp);
				PhVals ["Context_Title"] = PhVals ["WebPart_Title"] = wp.DisplayTitle;
			}
		}

		internal void AddPeopleWebPart (IDictionary action, string pageUrl, ArrayList filters, Hashtable fht) {
			string wpID = Request.QueryString ["l"], jop = Request.QueryString ["o"];
			Hashtable opt;
			Guid wpGuid = ProductPage.GetGuid (wpID.Replace ('_', '-').Substring (2));
			SystemWebPart wp = null;
			wpMan2 = web.GetLimitedWebPartManager (pageUrl.StartsWith ("/", StringComparison.InvariantCultureIgnoreCase) ? pageUrl : (web.Url.TrimEnd ('/') + '/' + pageUrl.TrimStart ('/')), PersonalizationScope.Shared);
			try {
				wp = wpMan2.WebParts [wpID];
			} catch (Exception ex) {
				if (Guid.Empty.Equals (wpGuid))
					Errors.Add (ex);
				else
					try {
						wp = wpMan2.WebParts [wpGuid];
					} catch (Exception ex2) {
						Errors.Add (ex2);
					}
			}
			if ((wp != null) && (bool) (new Reflector (wp.GetType ().Assembly).Get (wp, "IsB"))) {
				if (string.IsNullOrEmpty (Title))
					Title = wp.Title;
				if (!string.IsNullOrEmpty (jop))
					opt = JSON.JsonDecode (jop) as Hashtable;
				InitWebPart (wp);
				MainWebPart.Controls.Add (wp);
				PhVals ["Context_Title"] = PhVals ["WebPart_Title"] = wp.DisplayTitle;
			}
		}

		internal void InitWebPart (SystemWebPart webPart) {
			TahoeWebPart twp = webPart as TahoeWebPart;
			webPart.ChromeType = PartChromeType.None;
			webPart.AllowClose = webPart.AllowConnect = webPart.AllowEdit = webPart.AllowHide = webPart.AllowMinimize = webPart.AllowZoneChange = false;
			if (twp != null)
				twp.UseDefaultStyles = true;
		}

		internal bool Replace (XmlNode elem) {
			string oldInner = elem.InnerXml, newInner = Replace (oldInner);
			if (newInner != oldInner)
				try {
					elem.InnerXml = newInner;
					return true;
				} catch {
					return false;
				} else
				return false;
		}

		internal string Replace (string val) {
			foreach (KeyValuePair<string, string> kvp in PhVals)
				if (val.Contains (PhName (kvp.Key)))
					val = val.Replace (PhName (kvp.Key), kvp.Value);
			return val;
		}

		protected override void CreateChildControls () {
			string wpPageUrl, otherPartID = Request.QueryString ["wpid"], viewRawID, tmpCssClass = "c" + ProductPage.GuidLower (Guid.NewGuid (), false), propVal, nuPropVal, srcPageUrl = string.Empty;
			ProductPage.LicInfo li = ProductPage.LicInfo.Get (null);
			bool hasInvalidParams = false, isInvalidParam, isB = ProductPage.LicEdition (ctx, li, 2);
			int itemID = 0;
			object sm;
			XmlElement elem;
			Panel panel = HeaderWebParts;
			ArrayList filters = null;
			Hashtable fht = null;
			Guid viewID = Guid.Empty;
			SPLimitedWebPartManager wpMan = null;
			List<SystemWebPart> parts = new List<SystemWebPart> ();
			List<string> queryString = new List<string> ();
			TitleBarWebPart tbwp;
			SystemWebPart pwp;
			ctx = ProductPage.GetContext ();
			foreach (string k in Request.QueryString.AllKeys)
				if (!string.IsNullOrEmpty (k)) {
					if (!(isInvalidParam = (k.StartsWith ("FilterField", StringComparison.InvariantCultureIgnoreCase) || k.StartsWith ("FilterValue", StringComparison.InvariantCultureIgnoreCase))))
						if (!isB)
							foreach (string [] arr in new string [] [] { PrintZenMenuItem.CalendarParams, PrintZenMenuItem.FolderParams, PrintZenMenuItem.PageParams, PrintZenMenuItem.SortParams })
								if (isInvalidParam = (Array.IndexOf<string> (arr, k) >= 0))
									break;
					if (isInvalidParam)
						hasInvalidParams = true;
					else {
						queryString.Add (k + "=" + Server.UrlEncode (Request.QueryString [k]));
						if ((!"rpzopt".Equals (k, StringComparison.InvariantCultureIgnoreCase)) && !"r".Equals (k, StringComparison.InvariantCultureIgnoreCase))
							PhVals ["QueryString_" + k] = Request.QueryString [k];
					}
				}
			if ((!string.IsNullOrEmpty (Request.QueryString ["t"])) && (Title != Request.QueryString ["t"]))
				Title = Request.QueryString ["t"];
			if (hasInvalidParams)
				Response.Redirect (Request.RawUrl.Substring (0, Request.RawUrl.IndexOf ('?')) + "?" + string.Join ("&", queryString.ToArray ()), true);
			else if (!ProductPage.isEnabled)
				MainWebPart.Controls.Add (new LiteralControl (ProductPage.GetResource ("NotEnabled", "", ProductPage.GetTitle ())));
			else {
				if ((!string.IsNullOrEmpty (viewRawID = Request.QueryString ["View"])) && !int.TryParse (viewRawID, out itemID))
					viewID = ProductPage.GetGuid (viewRawID);
				if (ProductPage.LicEdition (ctx, li, 4)) {
					if (!string.IsNullOrEmpty (Request.QueryString ["fs"]))
						filters = JSON.JsonDecode (Request.QueryString ["fs"]) as ArrayList;
					if (!string.IsNullOrEmpty (Request.QueryString ["fj"]))
						fht = JSON.JsonDecode (Request.QueryString ["fj"]) as Hashtable;
				}
				foreach (IDictionary action in Actions)
					if (Request.QueryString ["a"].Equals (action ["id"]))
						try {
							if ((action ["sm"] is bool) && (bool) action ["sm"]) {
								if (!exAsmTried) {
									exAsmTried = true;
									try {
										exRefl = new Reflector (exAsm = Assembly.Load ("System.Web.Extensions, Version=3.5.0.0, Culture=Neutral, PublicKeyToken=31bf3856ad364e35"));
										exType = exAsm.GetType ("System.Web.UI.ScriptManager", false, true);
									} catch {
									}
								}
								if ((exRefl != null) && (exType != null) && (Form != null) && ((sm = exRefl.Call (exType, "GetCurrent", this)) == null))
									theLit.Controls.AddAt (0, (sm = exRefl.New (exType.FullName)) as Control);
							}
							PhVals ["List_Title"] = string.Empty;
							PhVals ["Item_Title"] = string.Empty;
							PhVals ["View_Title"] = string.Empty;
							PhVals ["Context_Title"] = string.Empty;
							foreach (DictionaryEntry prop in action)
								if (prop.Value is string)
									PhVals ["PrintAction_" + prop.Key] = prop.Value + string.Empty;
							AutoPrint = PrintZenMenuItem.GetBool (action, "dp");
							ShowPlaceholders = PrintZenMenuItem.GetBool (action, "sp");
							ShowToolbar = PrintZenMenuItem.GetBool (action, "tb");
							site = new SPSite (ctx.Site.ID);
							web = site.OpenWeb (ctx.Web.ID);
							if ((!string.IsNullOrEmpty (wpPageUrl = (action ["wp"] + string.Empty))) && ProductPage.LicEdition (ctx, li, 4)) {
								try {
									if ((wpPageUrl.StartsWith ("http:", StringComparison.InvariantCultureIgnoreCase) || wpPageUrl.StartsWith ("https:", StringComparison.InvariantCultureIgnoreCase)) && !wpPageUrl.StartsWith (web.Url.TrimEnd () + '/', StringComparison.InvariantCultureIgnoreCase)) {
										site2 = new SPSite (wpPageUrl);
										web2 = site2.OpenWeb ();
									}
									if ((wpMan = ((web2 == null) ? web : web2).GetLimitedWebPartManager (wpPageUrl, PersonalizationScope.Shared)) == null)
										throw new Exception ("xyz");
								} catch (Exception ex) {
									Errors.Add (new Exception (ProductPage.GetProductResource ("WebPartPageNotFound", wpPageUrl.ToUpperInvariant ()), "xyz".Equals (ex.Message) ? null : ex));
								}
							}
							if (!isB)
								panel.Controls.Add (new LiteralControl ("<div class=\"" + tmpCssClass + "\">Powered by <b>SharePoint-Tools.net/PrintZen</b></div><style type=\"text/css\"> ." + tmpCssClass + " { text-align: center; font-size: " + new Random ().Next (24, 37) + "px; display: none; padding-bottom: 12px; } @media print { div." + tmpCssClass + " { display: block !important; } } </style>"));
							if (wpMan == null) {
								if (!string.IsNullOrEmpty (otherPartID))
									AddOtherWebPart (Request.QueryString ["l"], otherPartID);
								else if (itemID > 0)
									AddListFormWebPart (action, itemID);
								else if (Guid.Empty.Equals (viewID) && !string.IsNullOrEmpty (viewRawID)) {
									if (isB)
										AddPeopleWebPart (action, viewRawID, filters, fht);
								} else
									AddListViewWebPart (action, viewID, filters, fht);
							} else {
								Placeholders = true;
								foreach (SystemWebPart wp in ProductPage.TryEach<SystemWebPart> (wpMan.WebParts, false, delegate (Exception ex) {
									Errors.Add (ex);
								}, true))
									if ((tbwp = wp as TitleBarWebPart) == null)
										parts.Add (wp);
									else {
										PhVals ["TitleBar_Caption"] = tbwp.HeaderCaption;
										PhVals ["TitleBar_Description"] = tbwp.HeaderDescription;
										PhVals ["TitleBar_Image"] = tbwp.Image;
										PhVals ["TitleBar_Title"] = tbwp.HeaderTitle;
										if (PrintZenMenuItem.GetBool (action, "st"))
											parts.Add (tbwp);
									}
								parts.Sort ((wp1, wp2) => {
									return wp1.ZoneIndex.CompareTo (wp2.ZoneIndex);
								});
								foreach (SystemWebPart wp in parts)
									if (("PRINTZEN_LIST_VIEW".Equals (wp.Title.Trim (), StringComparison.InvariantCultureIgnoreCase)) || ("PRINTZEN_TARGET".Equals (wp.Title.Trim (), StringComparison.InvariantCultureIgnoreCase))) {
										if (!string.IsNullOrEmpty (otherPartID))
											AddOtherWebPart (Request.QueryString ["l"], otherPartID);
										else if (itemID > 0)
											AddListFormWebPart (action, itemID);
										else if (Guid.Empty.Equals (viewID) && !string.IsNullOrEmpty (viewRawID)) {
											if (isB)
												AddPeopleWebPart (action, viewRawID, filters, fht);
										} else
											AddListViewWebPart (action, viewID, filters, fht);
										panel = FooterWebParts;
									} else
										try {
											InitWebPart (wp);
											panel.Controls.Add (wp);
										} catch (Exception ex) {
											Errors.Add (ex);
										}
								foreach (Panel p in new Panel [] { HeaderWebParts, MainWebPart, FooterWebParts })
									foreach (Control ctl in p.Controls)
										if ((pwp = ctl as SystemWebPart) != null)
											foreach (PropertyInfo propInfo in pwp.GetType ().GetProperties ())
												if(propInfo.CanRead && propInfo.CanWrite)
												if (propInfo.PropertyType == typeof (string)) {
													try {
														propVal = propInfo.GetValue (pwp, null) as string;
													} catch {
														propVal = null;
													}
													if ((!string.IsNullOrEmpty (propVal)) && (propVal != (nuPropVal = Replace (propVal))))
														propInfo.SetValue (pwp, nuPropVal, null);
												} else if (propInfo.PropertyType == typeof (XmlElement)) {
													try {
														elem = propInfo.GetValue (pwp, null) as XmlElement;
													} catch {
														elem = null;
													}
													if ((elem != null) && Replace (elem))
														try {
															propInfo.SetValue (pwp, elem, null);
														} catch {
														}
												}
							}
							break;
						} catch (Exception ex) {
							Errors.Add (ex);
							break;
						} finally {
							base.CreateChildControls ();
						}
			}
		}

		public override void Dispose () {
			if (wpMan != null)
				wpMan.Dispose ();
			if (wpMan2 != null)
				wpMan2.Dispose ();
			if (web != null)
				web.Dispose ();
			if (web2 != null)
				web2.Dispose ();
			if (site != null)
				site.Dispose ();
			if (site2 != null)
				site2.Dispose ();
			base.Dispose ();
		}

		public string Res (string name, params object [] args) {
			return ProductPage.GetProductResource (name, args);
		}

	}

}
