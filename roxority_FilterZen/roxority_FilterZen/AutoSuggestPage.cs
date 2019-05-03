
using Microsoft.SharePoint;
using roxority.SharePoint;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;

namespace roxority_FilterZen {

	public class AutoSuggestPage : Page {

		private static readonly string [] unsupportedFieldTypes = { "Boolean", "ContentTypeId", "Counter", "Error", "MaxItems", "PageSeparator" };
		private static readonly string [] nonTextFieldTypes = { "AllDayEvent", "Attachments", "BusinessData", "Currency", "DateTime", "Guid", "Integer", "Number", "Recurrence", "ThreadIndex", "Threading" };

		protected override void Render (HtmlTextWriter __w) {
			bool begins = "1".Equals (Request ["b"]), isNonSupported = false, isNonText = false, tryAsText = false, tryAll = false;
			int defLimit = 40, limit, count = 0, itemCount = -1, c = 0;
			object val;
			string fsep = Request ["fs"], sval, secVal, tmpVal, fieldType, urlQuery = (Request ["q"] + string.Empty).Trim (), op = (begins = begins || ((urlQuery.Length == 1) && ProductPage.Config<bool> (ProductPage.GetContext (), "Auto1Begins"))) ? "BeginsWith" : "Contains";
			string [] svals;
			Guid viewID;
			SPList list = null;
			SPView view = null;
			SPQuery query;
			SPListItemCollection items = null;
			SPField field, tmpField;
			SPFieldCalculated calcField;
			SPFieldLookup lookupField;
			SPFieldUrl urlField;
			SPFieldLookupValue lookVal;
			SPFieldLookupValueCollection lookVals;
			SPFieldUserValueCollection userVals;
			SPFieldUrlValue urlVal;
			List<string> allVals = new List<string> (), secVals = new List<string> ();
			List<SPField> secFields = new List<SPField> ();
			if ((!int.TryParse (ProductPage.Config (ProductPage.GetContext (), "AutoLimit"), out limit)) || (limit <= 1))
				limit = defLimit;
			if ((!int.TryParse (Request ["limit"], out limit)) || (limit <= 1))
				limit = defLimit;
			try {
				list = SPContext.Current.Web.Lists [ProductPage.GetGuid (Request ["l"])];
			} catch {
			}
			if (list == null) {
				__w.Write (ProductPage.GetProductResource ("AutoNoList", ProductPage.GetGuid (Request ["l"])));
			} else if (((field = ProductPage.GetField (list, Request ["f"])) == null) /*|| Guid.Empty.Equals (field.Id)*/) {
				__w.Write (ProductPage.GetProductResource ("AutoNoField", Request ["f"]));
			} else if (!string.IsNullOrEmpty (urlQuery)) {
				foreach (string sf in (Request ["sf"] + string.Empty).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
					if ((tmpField = ProductPage.GetField (list, sf)) != null)
						secFields.Add (tmpField);
				calcField = field as SPFieldCalculated;
				urlField = field as SPFieldUrl;
				lookupField = field as SPFieldLookup;
				if ((!string.IsNullOrEmpty (Request ["v"])) && !Guid.Empty.Equals (viewID = ProductPage.GetGuid (Request ["v"])))
					try {
						view = list.Views [viewID];
					} catch {
					}
				if (view == null)
					view = list.DefaultView;
			retry:
				query = new SPQuery (view);
				query.Folder = list.RootFolder;
				query.IncludeAllUserPermissions = query.IncludeMandatoryColumns = query.IncludePermissions = query.IndividualProperties = query.AutoHyperlink = query.ExpandRecurrence = query.ExpandUserField = query.IncludeAttachmentVersion = query.IncludeAttachmentUrls = query.ItemIdQuery = false;
				query.ViewAttributes = "FailIfEmpty=\"FALSE\" RequiresClientIntegration=\"FALSE\" Threaded=\"FALSE\" Scope=\"Recursive\"";
				query.ViewFields = "<FieldRef Name=\"" + field.InternalName + "\"/>";
				foreach (SPField spf in secFields)
					query.ViewFields += ("<FieldRef Name=\"" + spf.InternalName + "\"/>");
				fieldType = ((calcField != null) ? calcField.OutputType.ToString () : field.TypeAsString);
				isNonText = (Array.IndexOf<string> (nonTextFieldTypes, field.TypeAsString) >= 0);
				isNonSupported = (Array.IndexOf<string> (unsupportedFieldTypes, field.TypeAsString) >= 0);
				query.Query = (tryAll ? string.Empty : ("<Where><" + op + "><FieldRef Name=\"" + field.InternalName + "\" /><Value Type=\"" + (tryAsText ? "Text" : fieldType) + "\">" + urlQuery + "</Value></" + op + "></Where>")) + "<OrderBy><FieldRef Name=\"" + field.InternalName + "\"/></OrderBy>";
				query.RowLimit = (uint) limit;
				items = list.GetItems (query);
				if (items != null) {
					itemCount = 0;
					foreach (SPListItem item in ProductPage.TryEach<SPListItem> (items)) {
						secVal = string.Empty;
						tmpVal = string.Empty;
						foreach (SPField sf in secFields) {
							if ((sf != null) && !Guid.Empty.Equals (sf.Id))
								try {
									tmpVal = item [sf.Id] + string.Empty;
								} catch {
									tmpVal = string.Empty;
								}
							if (!string.IsNullOrEmpty (tmpVal))
								secVal += ("<div class=\"rox-acsecfield\"><i>" + Server.HtmlEncode (sf.Title) + ":</i> " + tmpVal.Replace ("\r\n", " ").Replace ("\r", " ").Replace ("\n", " ") + "</div>");
						}
						itemCount++;
						svals = null;
						val = ProductPage.GetFieldVal (item, field, true);
						if (!string.IsNullOrEmpty (sval = ((val == null) ? string.Empty : field.GetFieldValueAsText (val))))
							if (sval.Trim ().IndexOf (urlQuery, StringComparison.InvariantCultureIgnoreCase) < 0)
								sval = string.Empty;
							else if (lookupField != null) {
								if ((userVals = val as SPFieldUserValueCollection) != null) {
									sval = string.Empty;
									svals = userVals.ConvertAll<string> ((uv) => {
										return uv.LookupValue;
									}).ToArray ();
								} else if ((lookVals = val as SPFieldLookupValueCollection) != null) {
									sval = string.Empty;
									svals = lookVals.ConvertAll<string> ((lv) => {
										return lv.LookupValue;
									}).ToArray ();
								} else if ((lookVal = val as SPFieldLookupValue) != null)
									sval = lookVal.LookupValue;
							} else if ((urlField != null) && ((urlVal = val as SPFieldUrlValue) != null))
								sval = urlVal.Url;
							else if (field.Type == SPFieldType.MultiChoice)
								svals = (val + string.Empty).Split (new string [] { ";#" }, StringSplitOptions.RemoveEmptyEntries);
							else if (!string.IsNullOrEmpty (fsep))
								svals = (val + string.Empty).Split (new string [] { fsep }, StringSplitOptions.RemoveEmptyEntries);
						if (sval.Trim ().IndexOf (urlQuery, StringComparison.InvariantCultureIgnoreCase) < 0)
							sval = string.Empty;
						if (svals != null) {
							for (int i = 0; i < svals.Length; i++)
								if (svals [i].Trim ().IndexOf (urlQuery, StringComparison.InvariantCultureIgnoreCase) < 0)
									svals [i] = string.Empty;
						} else if (!string.IsNullOrEmpty (sval))
							svals = new string [] { sval };
						else
							svals = new string [0];
						if (svals.Length > 0) {
							count++;
							foreach (string sv in svals)
								if ((!string.IsNullOrEmpty (sv)) && (sv.Trim ().IndexOf (urlQuery, StringComparison.InvariantCultureIgnoreCase) >= 0) && !allVals.Contains (sv.Trim ())) {
									secVals.Add (secVal);
									allVals.Add (sv.Trim ());
								}
						}
						if (allVals.Count >= limit)
							break;
					}
				}
				if (itemCount < ((isNonText) ? 1 : 0)) {
					if (isNonText && !tryAsText) {
						tryAsText = true;
						goto retry;
					}
					if (!tryAll) {
						tryAll = true;
						goto retry;
					}
				}
			}
			allVals.Sort ();
			foreach (string sv in allVals) {
				string str = sv;
				foreach (string repl in new string [] { "mailto:", "http://", "www." }) {
					if (str.StartsWith (repl))
						str = str.Substring (repl.Length);
				}
				if (str.Length > 0)
					__w.Write (/*Server.HtmlEncode*/ (str.Replace ("\r\n", " ").Replace ("\r", " ").Replace ("\n", " ") + secVals [c++]) + "\r\n");
			}
		}

	}

}
