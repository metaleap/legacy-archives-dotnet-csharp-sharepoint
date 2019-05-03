
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using roxority.Data;
using roxority.Shared;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using System.Xml;

namespace roxority_ExportZen {

	public class ExportZenPage : Page {

		internal const string WEIRD_CHAR = "​"; // not the empty string, just looks like it

		internal static readonly Regex htmlRegex = new Regex (@"(?></?\w+)(?>(?:[^>'""]+|'[^']*'|""[^""]*"")*)>", RegexOptions.Singleline);
		internal static Reflector refl = null;

		internal static string CsvEscape (string value, string sep, bool unix) {
			while (value.StartsWith (sep, StringComparison.InvariantCultureIgnoreCase))
				value = value.Substring (1);
			while (value.EndsWith (sep, StringComparison.InvariantCultureIgnoreCase))
				value = value.Substring (0, value.Length - 1);
			if (unix)
				value = value.Replace ("\\", "\\\\").Replace (sep, "\\" + sep).Replace ("\r", "\\r").Replace ("\n", "\\n");
			else {
				value = value.Replace ("\"", "\"\"");
				if ((value.IndexOf (sep) > 0) || (value.IndexOf ('\r') > 0) || (value.IndexOf ('\n') > 0))
					value = "\"" + value + "\"";
			}
			return value.Trim ();
		}

		internal static void ExportRollup (TextWriter writer, IDictionary action, Hashtable opt, SPWeb web, Hashtable fht, string [] listColumns, string locale, Action<string, string> csvWrite, string sep) {
			string tmp, fieldName, colVal, mulSep = ProductPage.Config (ProductPage.GetContext (), "MultiSep");
			object val, ds;
			int pos;
			bool filt = ((action ["filter"] is bool) && (bool) action ["filter"]);
			if (refl == null)
				refl = new Reflector (Assembly.Load ("roxority_PeopleZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01"));
			foreach (string k in new ArrayList (opt.Keys))
				if (opt [k] is string)
					opt [k] = HttpUtility.UrlDecode (opt [k] + string.Empty);
			if (filt && (fht == null) && opt.ContainsKey ("f")) {
				fht = new Hashtable ();
				fht ["f"] = JSON.JsonDecode (HttpUtility.UrlDecode (opt ["f"] + string.Empty));
				if (opt.ContainsKey ("fa"))
					fht ["fa"] = JSON.JsonDecode (HttpUtility.UrlDecode (opt ["fa"] + string.Empty));
			}
			using (IDisposable consumer = refl.New ("roxority.Data.DataSourceConsumer", JsonSchemaManager.Bool (action ["nopg"], false) ? 0 : (int) opt ["ps"], (int) opt ["p"], "1".Equals (opt ["dty"]), "1".Equals (opt ["did"]), opt ["pr"] + string.Empty, (object) (tmp = (opt ["spn"] + string.Empty)), string.IsNullOrEmpty (tmp) ? null : (object) "1".Equals (opt ["sd"]), JsonSchemaManager.Bool (action ["notb"], false) ? string.Empty : (opt ["tpn"] + string.Empty), JsonSchemaManager.Bool (action ["notb"], false) ? string.Empty : opt ["tv"], (object) (tmp = (opt ["gpn"] + string.Empty)), string.IsNullOrEmpty (tmp) ? null : (object) "1".Equals (opt ["gd"]), "1".Equals (opt ["gb"]), "1".Equals (opt ["gs"]), web, opt ["dsid"] + string.Empty, null, filt ? fht : null, null, new List<Exception> ()) as IDisposable) {
				ds = refl.Get (consumer, "DataSource");
				foreach (object crec in refl.Get (consumer, "List") as IEnumerable)
					for (int i = 0; i < listColumns.Length; i++) {
						fieldName = ((pos = listColumns [i].IndexOf (':')) <= 0) ? (listColumns [i]) : (listColumns [i].Substring (0, pos));
						val = refl.Call (crec, "Get", new Type [] { typeof (string), typeof (string), ds.GetType ().BaseType }, new object [] { fieldName, string.Empty, ds });
						colVal = GetExportValue (web, val, null, locale, ((fieldName.ToLowerInvariant ().Contains ("birthday") || fieldName.ToLowerInvariant ().Contains ("xxxhiredate")) && !string.IsNullOrEmpty (val + string.Empty)), mulSep);
						csvWrite (colVal, ((i == listColumns.Length - 1) ? "\r\n" : sep));
					}
			}
		}

		internal static string GetExportValue (SPWeb exportWeb, object val, SPField field, string locale, string mulSep) {
			return GetExportValue (exportWeb, val, field, locale, false, mulSep);
		}

		internal static string GetExportValue (SPWeb exportWeb, object val, SPField field, string locale, bool forceDate, string mulSep) {
			string colVal;
			int pos;
			DateTime dtVal;
			SPFieldDateTime dtField = field as SPFieldDateTime;
			SPFieldLookup pgField = field as SPFieldLookup;
			SPFieldMultiLineText rtField = field as SPFieldMultiLineText;
			SPFieldMultiChoiceValue mcv;
			XmlDocument doc;
			if (field is SPFieldCalculated)
				val = ((SPFieldCalculated) field).GetFieldValueAsText (val);
			if (field is SPFieldUrl)
				val = ((SPFieldUrl) field).GetFieldValue (val + string.Empty);
			if (forceDate && (DateTime.TryParse (val + string.Empty, out dtVal) || DateTime.TryParse (val + string.Empty, JsonSchemaManager.Property.Type.LocaleChoice.GetCulture (locale), DateTimeStyles.AllowWhiteSpaces, out dtVal)))
				val = dtVal;
			if ((val is DateTime) && ((dtField != null) || forceDate))
				colVal = ((DateTime) val).ToString ((dtField == null) ? "m" : ((dtField.DisplayFormat == SPDateTimeFieldFormatType.DateOnly) ? "d" : "g"), JsonSchemaManager.Property.Type.LocaleChoice.GetCulture (locale));
			else if (val is SPFieldUrlValue)
				colVal = ((SPFieldUrlValue) val).Url;
			else if (val is SPFieldLookupValueCollection)
				colVal = string.Join (mulSep, ((SPFieldLookupValueCollection) val).ConvertAll<string> ((lv) => {
					return lv.LookupValue;
				}).ToArray ());
			else if (val is SPFieldLookupValue)
				colVal = ((SPFieldLookupValue) val).LookupValue;
			else if ((mcv = val as SPFieldMultiChoiceValue) != null) {
				colVal = string.Empty;
				for (int i = 0; i < mcv.Count; i++)
					colVal += (mulSep + mcv [i]);
				if (colVal.Length > mulSep.Length)
					colVal = colVal.Substring (mulSep.Length);
			} else if (((pgField = field as SPFieldLookup) != null) || (((rtField = field as SPFieldMultiLineText) != null) && rtField.RichText)) {
				colVal = val + string.Empty;
				if (pgField != null) {
					if ((pos = colVal.IndexOf (";#", StringComparison.InvariantCultureIgnoreCase)) >= 0)
						colVal = colVal.Substring (pos + 2);
					if ((pos = colVal.IndexOf (";#")) > 0)
						colVal = colVal.Substring (0, pos);
				}
				colVal = htmlRegex.Replace (colVal.Replace (WEIRD_CHAR, string.Empty).Replace ("<br>", "\r\n").Replace ("<BR>", "\r\n").Replace ("<br/>", "\r\n").Replace ("<BR/>", "\r\n").Replace ("<br />", "\r\n").Replace ("<BR />", "\r\n"), string.Empty);
			} else
				colVal = val + string.Empty;
			if (colVal.StartsWith ("<roxhtml/>")) {
				colVal = htmlRegex.Replace (colVal.Substring ("<roxhtml/>".Length).Replace (WEIRD_CHAR, string.Empty).Replace ("<br>", "\r\n").Replace ("<BR>", "\r\n").Replace ("<br/>", "\r\n").Replace ("<BR/>", "\r\n").Replace ("<br />", "\r\n").Replace ("<BR />", "\r\n"), string.Empty);
				try {
					doc = new XmlDocument ();
					doc.LoadXml (colVal);
					colVal = doc.DocumentElement.InnerText;
				} catch {
				}
				colVal = colVal.Replace ('\r', ' ').Replace ('\n', ' ').Trim ();
			}
			return colVal;
		}

		internal static string SafeName (string name) {
			int pos1, pos2;
			while (((pos1 = name.IndexOf ("{$", StringComparison.InvariantCultureIgnoreCase)) > 0) && ((pos2 = name.IndexOf ("$}", pos1 + 2, StringComparison.InvariantCultureIgnoreCase)) > pos1))
				name = name.Substring (0, pos1) + name.Substring (pos2 + 2);
			for (int i = 0; i < name.Length; i++)
				if (!char.IsLetterOrDigit (name, i))
					name = name.Replace (name [i], '_');
			return name;
		}

		public static void Export (TextWriter writer, ICollection<IDictionary> allActions, string webUrl, string exportListID, string ruleID, string listViewID, string separator, string unixFlag, string fs, int min, string fj) {
			string locale = string.Empty, viewXml, sep = ",", versioning, colVal, subVersion, colName, verVal, newVerVal, tmp, fieldName, rollJson = null, mulSep = ProductPage.Config (ProductPage.GetContext (), "MultiSep"), acsep;
			string [] listColumns;
			int pos, verIndex = -1, theEnc;
			bool expGrps = false, versionRows = false, skipRow = false, isRoll, is2, is4, isMin;
			object val;
			HttpContext context = null;
			Encoding encoding = Encoding.Default;
			SPList exportList = null;
			SPListItemCollection listItems = null;
			SPView listView = null;
			SPField field;
			SPQuery query;
			List<SPListItemVersion> versions;
			Dictionary<string, string> verVals;
			ProductPage.LicInfo li = ProductPage.LicInfo.Get (null);
			ArrayList filters = null;
			SPView defView;
			Hashtable fht = null, rollOpt = null;
			XmlDocument doc = new XmlDocument ();
			XmlNode node;
			Action<string, string> csvWrite = null;
			IDictionary action = null;
			Guid ruleGuid, instGuid;
			List<string> originalViewFields;
			is2 = ProductPage.LicEdition (ProductPage.GetContext (), li, 2);
			is4 = ProductPage.LicEdition (ProductPage.GetContext (), li, 4);
			isMin = ProductPage.LicEdition (ProductPage.GetContext (), li, min);
			try {
				context = HttpContext.Current;
			} catch {
			}
			if (isRoll = ((context != null) && (context.Request != null) && (context.Request.QueryString != null) && !string.IsNullOrEmpty (rollJson = context.Request.QueryString ["rpzopt"])))
				rollOpt = JSON.JsonDecode (rollJson) as Hashtable;
			if (li.expired)
				throw new SPException (ProductPage.GetResource ("LicExpiry"));
			if (!isMin)
				throw new SPException (ProductPage.GetResource ("NopeEd", "ExportZen.exe", "Ultimate"));
			using (SPSite site = new SPSite (webUrl))
			using (SPWeb exportWeb = site.OpenWeb ()) {
				if ((!isRoll) && (string.IsNullOrEmpty (exportListID) || ((exportList = exportWeb.Lists [new Guid (exportListID)]) == null)))
					throw new SPException (ProductPage.GetProductResource ("Old_NoExportList"));
				foreach (IDictionary inst in allActions)
					if (ruleID.Equals (inst ["id"] + string.Empty, StringComparison.InvariantCultureIgnoreCase) || ruleID.Equals (inst ["name"] + string.Empty, StringComparison.InvariantCultureIgnoreCase) || ((!Guid.Empty.Equals (instGuid = ProductPage.GetGuid (inst ["id"] + string.Empty))) && (!Guid.Empty.Equals (ruleGuid = ProductPage.GetGuid (ruleID))) && ruleGuid.Equals (instGuid))) {
						action = inst;
						break;
					}
				if (action == null)
					throw new SPException (ProductPage.GetProductResource ("Old_NoRuleItem", ruleID, allActions.Count, ProductPage.LoginName(exportWeb.CurrentUser.LoginName), ProductPage.Config (ProductPage.GetContext (), "_lang")));
				if (is4 && (exportList != null) && !string.IsNullOrEmpty (listViewID))
					try {
						listView = exportList.Views [new Guid (listViewID)];
					} catch {
					}
				if (((listColumns = (action ["cols"] + string.Empty).Split (new string [] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)) == null) || (listColumns.Length == 0)) {
					if (isRoll)
						listColumns = HttpUtility.UrlDecode (((((int) rollOpt ["nm"]) != 0) ? (DataSource.SCHEMAPROP_PREFIX + DataSource.SCHEMAPROP_TITLEFIELD + ":" + ProductPage.GetResource ("Name") + "\r\n") : string.Empty) + rollOpt ["pr"] + "").Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					else if (exportList != null) {
						if ((defView = listView) == null)
							defView = exportList.DefaultView;
						originalViewFields = new List<string> (defView.ViewFields.Count);
						foreach (string fn in defView.ViewFields)
							if ((Array.IndexOf<string> (ProductPage.SPOddFieldNames, fn) < 0) || fn.StartsWith ("Link"))
								originalViewFields.Add (fn + (((field = ProductPage.GetField (exportList, fn)) == null) ? string.Empty : (":" + field.Title)));
						listColumns = originalViewFields.ToArray ();
					}
				}
				if ((listColumns.Length > 3) && !is2)
					listColumns = new string [] { listColumns [0], listColumns [1], listColumns [2] };
				versionRows = (((val = action ["vhrows"]) is bool) && (bool) val);
				if (!string.IsNullOrEmpty (versioning = (action ["vhcol"] + string.Empty).Trim ())) {
					for (int i = 0; i < listColumns.Length; i++)
						if ("roxVersion".Equals ((string.Empty + listColumns [i]).Trim (), StringComparison.InvariantCultureIgnoreCase)) {
							verIndex = i;
							break;
						}
					if (verIndex < 0) {
						Array.Resize<string> (ref listColumns, listColumns.Length + 1);
						listColumns [verIndex = (listColumns.Length - 1)] = versioning;
					} else
						listColumns [verIndex] = versioning;
				}
				try {
					locale = action ["loc"] + string.Empty;
				} catch {
				}
				try {
					if (!string.IsNullOrEmpty (tmp = action ["enc"] + string.Empty))
						if (int.TryParse (tmp, out theEnc))
							encoding = Encoding.GetEncoding (theEnc);
						else
							encoding = Encoding.GetEncoding (tmp);
				} catch {
				}
				if (context != null) {
					context.Response.ContentEncoding = encoding;
					context.Response.ContentType = "text/csv; charset=" + encoding.WebName;
					context.Response.AddHeader ("Content-Disposition", "attachment;filename=\"" + SafeName (((exportList == null) ? context.Request.QueryString ["t"] : exportList.Title) + " " + JsonSchemaManager.GetDisplayName (action, "ExportActions", false)) + "_" + DateTime.Now.Ticks + ".csv" + "\"");
				}
				try {
					if ((action ["bom"] is bool) && (bool) action ["bom"])
						writer.Write ("\uFEFF");
				} catch {
				}
				if (is2) {
					acsep = action ["sep"] + string.Empty;
					if (!string.IsNullOrEmpty (separator))
						sep = separator;
					else if ((action ["excel"] is bool) && ((bool) action ["excel"]) && (string.IsNullOrEmpty (acsep) || acsep == "s"))
						sep = ";";
					else if (acsep == "t")
						sep = "\t";
				}
				if (!JsonSchemaManager.Bool (action ["nf"], false))
					for (int i = 0; i < listColumns.Length; i++) {
						fieldName = (((pos = listColumns [i].IndexOf (':')) <= 0) ? (listColumns [i]) : (listColumns [i].Substring (pos + 1))).Trim ();
						if ((fieldName == "ID") && (i == 0))
							fieldName = "\"ID\"";
						writer.Write (fieldName + ((i == listColumns.Length - 1) ? ("\r\n") : (sep)));
					}
				if (is2 && !string.IsNullOrEmpty (fs))
					filters = JSON.JsonDecode (fs) as ArrayList;
				if (is2 && !string.IsNullOrEmpty (fj))
					fht = JSON.JsonDecode (fj) as Hashtable;
				if ((filters != null) && (filters.Count == 0))
					filters = null;
				if (exportList != null)
					if ((listView == null) && (filters == null))
						listItems = exportList.Items;
					else {
						query = ((listView != null) ? new SPQuery (listView) : new SPQuery ());
						if (listView == null) {
							query.Folder = exportList.RootFolder;
							viewXml = "<View><Query/></View>";
						} else
							viewXml = listView.SchemaXml;
						query.AutoHyperlink = query.ExpandUserField = query.ItemIdQuery = false;
						query.ExpandRecurrence = query.IndividualProperties = query.IncludePermissions = query.IncludeMandatoryColumns = query.IncludeAttachmentVersion = query.IncludeAttachmentUrls = query.IncludeAllUserPermissions = query.RecurrenceOrderBy = true;
						query.RowLimit = 0;
						query.ViewFields = string.Empty;
						foreach (SPField f in ProductPage.TryEach<SPField> (exportList.Fields))
							query.ViewFields += string.Format (ProductPage.FORMAT_CAML_VIEWFIELD, f.InternalName);
						if (filters != null) {
							doc.LoadXml (viewXml);
							if (!string.IsNullOrEmpty (viewXml = ProductPage.ApplyCore (exportList, viewXml, doc, filters, ref expGrps, false, fht, null))) {
								doc.LoadXml (viewXml);
								if ((node = doc.DocumentElement.SelectSingleNode ("Query")) != null)
									query.Query = node.InnerXml;
							}
						}
						listItems = exportList.GetItems (query);
					}
				csvWrite = delegate (string csvVal, string suffix) {
					writer.Write (CsvEscape (csvVal, sep, is2 && "1".Equals (unixFlag)) + suffix);
				};
				if (isRoll)
					ExportRollup (writer, action, rollOpt, exportWeb, fht, listColumns, locale, csvWrite, sep);
				else
					if (listItems != null)
						foreach (SPListItem item in ProductPage.TryEach<SPListItem> (listItems))
							for (int i = 0; i < listColumns.Length; i++) {
								skipRow = false;
								if ((!string.IsNullOrEmpty (versioning)) && (i == verIndex)) {
									versions = new List<SPListItemVersion> ();
									verVals = new Dictionary<string, string> ();
									colVal = string.Empty;
									foreach (SPListItemVersion v in item.Versions)
										versions.Add (v);
									versions.Sort (delegate (SPListItemVersion one, SPListItemVersion two) {
										return one.VersionLabel.CompareTo (two.VersionLabel);
									});
									if (versionRows && (versions.Count > 1)) {
										skipRow = true;
										csvWrite (string.Empty, "\r\n");
										for (int v = versions.Count - 1; v >= 0; v--) {
											for (int vc = 0; vc < listColumns.Length; vc++)
												if (vc == verIndex)
													csvWrite (versions [v].VersionLabel, ((vc == (listColumns.Length - 1)) ? "\r\n" : sep));
												else {
													colVal = string.Empty;
													if (!"ID".Equals (fieldName = ((pos = listColumns [vc].IndexOf (':')) <= 0) ? (listColumns [vc]) : (listColumns [vc].Substring (0, pos)))) {
														field = ProductPage.GetField (item, fieldName);
														val = string.Empty;
														try {
															val = versions [v] [fieldName];
														} catch {
														}
														colVal = GetExportValue (exportWeb, val, field, locale, mulSep);
													}
													csvWrite (colVal, ((vc == (listColumns.Length - 1)) ? "\r\n" : sep));
												}
										}
									} else {
										for (int v = 0; v < versions.Count; v++) {
											subVersion = versions [v].VersionLabel + " " + versions [v].Created.ToLocalTime () + "\r\n" + versions [v].CreatedBy.LookupValue;
											for (int lc = 0; lc < listColumns.Length; lc++)
												if (lc != verIndex)
													try {
														colName = ((pos = listColumns [lc].IndexOf (':')) <= 0) ? (listColumns [lc]) : (listColumns [lc].Substring (0, pos).Trim ());
														if ((field = ProductPage.GetField (item, colName)) != null)
															colName = field.InternalName;
														try {
															val = versions [v] [colName];
														} catch {
															val = null;
														}
														newVerVal = GetExportValue (exportWeb, val, field, locale, mulSep);
														if ((!verVals.TryGetValue (colName, out verVal)) || (verVal != newVerVal)) {
															verVals [colName] = newVerVal;
															subVersion += ("\r\n" + (((pos = listColumns [lc].IndexOf (':')) <= 0) ? (listColumns [lc]) : (listColumns [lc].Substring (pos + 1).Trim ())) + ": " + newVerVal);
														}
													} catch {
													}
											colVal = subVersion + "\r\n\r\n" + colVal;
										}
									}
								} else {
									if ((field = ProductPage.GetField (item, (fieldName = ((pos = listColumns [i].IndexOf (':')) <= 0) ? (listColumns [i]) : (listColumns [i].Substring (0, pos))))) != null)
										val = item [field.Id];
									else
										try {
											val = item [fieldName];
										} catch {
											val = null;
										}
									colVal = GetExportValue (exportWeb, val, field, locale, mulSep);
								}
								if (!skipRow)
									csvWrite (colVal, ((i == listColumns.Length - 1) ? "\r\n" : sep));
							}
			}
		}

		public static ICollection<IDictionary> GetActions (string fpath) {
			return JsonSchemaManager.GetInstances (fpath, "ExportActions");
		}

		protected override void Render (HtmlTextWriter writer) {
			string webUrl = string.Empty;
			Dictionary<string, string> req = new Dictionary<string, string> ();
			try {
				webUrl = SPContext.Current.Web.Url;
			} catch {
				try {
					webUrl = HttpContext.Current.Request.RawUrl;
				} catch (Exception ex) {
					throw new Exception ("Could not determine current SPWeb URL: " + ex.Message);
				}
			}
			foreach (string p in new string [] { "exportlist", "rule", "lv", "sep", "unix", "f", "fj" })
				try {
					req [p] = Request [p] + string.Empty;
				} catch (Exception ex) {
					throw new Exception ("Could not obtain GET argument " + p + ": " + ex.Message);
				}
			Export (writer, GetActions (Server.MapPath ("/_layouts/roxority_ExportZen/schemas.json")), webUrl.TrimEnd ('/'), req ["exportlist"], req ["rule"], req ["lv"], req ["sep"], req ["unix"], req ["f"], 0, req ["fj"]);
			base.Render (writer);
		}

	}

}
