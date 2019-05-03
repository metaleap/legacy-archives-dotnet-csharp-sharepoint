
using roxority.Shared;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web;

using StrTuple = System.Collections.Generic.KeyValuePair<string, string>;
using SysType = System.Type;

namespace roxority.SharePoint {

	public class JsonSchemaManager : IDisposable {

		#region ISchemaExtender Interface

		public interface ISchemaExtender {

			void InitSchema (Schema owner);

		}

		#endregion

		#region Property Class

		public class Property {

			#region Type Class

			public abstract class Type {

				#region Boolean Class

				public class Boolean : Type {

					public override object FromPostedValue (Property prop, string value, Schema owner) {
						return !string.IsNullOrEmpty (value);
					}

					public override string GetFormKey (string id, Property prop) {
						return "roxiteminstchk_" + base.GetFormKey (id, prop);
					}

					public override string ToString (Property prop, object val) {
						return string.Empty;
					}

					public override bool IsBool {
						get {
							return true;
						}
					}

				}

				#endregion

				#region Choice Class

				public class Choice : Type {

					private readonly IEnumerable choices;

					public Choice (IEnumerable choices) {
						this.choices = choices;
					}

					protected internal string GetChoiceString (string ownerName, string propName, object choice, string prefix) {
						int pos;
						string r = ProductPage.GetProductResource (prefix + ownerName + "_" + propName + "_" + (("PD_".Equals (prefix) && "UserDatabase".Equals (choice)) ? "Ado" : choice));
						return ((string.IsNullOrEmpty (r) && ((pos = propName.IndexOf ('_')) > 0)) ? GetChoiceString (ownerName, propName.Substring (pos + 1), choice, prefix) : r);
					}

					public virtual string GetChoiceDesc (Property prop, object choice) {
						return GetChoiceString (prop.Owner.Name, prop.Name, choice, "PD_");
					}

					public virtual IEnumerable GetChoices (IDictionary rawSchema) {
						return choices;
					}

					public virtual string GetChoiceTitle (Property prop, object choice) {
						return GetChoiceString (prop.Owner.Name, prop.Name, choice, "PC_");
					}

					public override object GetDefaultValue (IDictionary rawSchema) {
						object def = base.GetDefaultValue (rawSchema);
						if (def == null)
							foreach (object c in GetChoices (rawSchema)) {
								def = c;
								break;
							}
						return def;
					}

					public override string RenderValueForEdit (Property prop, IDictionary instance, bool disabled, bool readOnly) {
						string options = "", desc;
						foreach (object choice in GetChoices (prop.RawSchema))
							options += string.Format ("<option value=\"{0}\"{2}>{1}</option>", choice, GetChoiceTitle (prop, choice) + (string.IsNullOrEmpty (desc = GetChoiceDesc (prop, choice)) ? string.Empty : (" &mdash; " + desc)), (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + (choice.Equals (instance [prop.Name]) ? " selected=\"selected\"" : string.Empty));
						return string.Format ("<select onchange=\"roxHasChanged();\" class=\"{2}\" id=\"{0}\" name=\"{0}\"" + (readOnly ? DISABLED : string.Empty) + ">{1}</select>", instance ["id"] + "_" + prop.Name, options, CssClass);
					}

					public override string RenderValueForDisplay (Property prop, object val) {
						return GetChoiceTitle (prop, val);
					}

				}

				#endregion

				#region ConfigChoice Class

				public class ConfigChoice : DictChoice {

					public ConfigChoice ()
						: base (new OrderedDictionary ()) {
					}

					internal void ResetChoices (IDictionary rawSchema) {
						string cfgName, cfgVal;
						string [] lines;
						int pos;
						if ((Choices.Count == 0) && (!string.IsNullOrEmpty (cfgName = rawSchema ["config"] + string.Empty)) && (!string.IsNullOrEmpty (cfgVal = ProductPage.Config (ProductPage.GetContext (), cfgName))) && ((lines = cfgVal.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Length > 0))
							foreach (string ln in lines)
								if ((pos = ln.IndexOf (':')) > 0)
									Choices [ln.Substring (pos + 1).Trim ()] = ln.Substring (0, pos).Trim ();
								else
									Choices [ln] = ln;
					}

					public override object GetDefaultValue (IDictionary rawSchema) {
						ResetChoices (rawSchema);
						return base.GetDefaultValue (rawSchema);
					}

					public override string RenderValueForEdit (Property prop, IDictionary instance, bool disabled, bool readOnly) {
						ResetChoices (prop.RawSchema);
						return base.RenderValueForEdit (prop, instance, disabled, readOnly);
					}

					public override string RenderValueForDisplay (Property prop, object val) {
						ResetChoices (prop.RawSchema);
						return string.IsNullOrEmpty (val + string.Empty) ? string.Empty : base.RenderValueForDisplay (prop, val);
					}

				}

				#endregion

				#region DictChoice Class

				public class DictChoice : Type {

					public readonly IDictionary Choices;

					private string formKey = null;

					public DictChoice (IDictionary choices) {
						Choices = choices;
					}

					protected internal bool IsMulti (IDictionary rawSchema) {
						return Bool (rawSchema ["multi"], false);
					}

					public override object GetDefaultValue (IDictionary rawSchema) {
						object def = null;
						if ((!IsMulti (rawSchema)) && ((def = base.GetDefaultValue (rawSchema)) == null))
							foreach (DictionaryEntry entry in Choices) {
								def = entry.Key;
								break;
							}
						return def;
					}

					public override string RenderValueForEdit (Property prop, IDictionary instance, bool disabled, bool readOnly) {
						string options = "";
						string [] vals = (instance [prop.Name] + string.Empty).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						foreach (DictionaryEntry choice in Choices)
							options += string.Format ("<option value=\"{0}\"{2}>{1}</option>", choice.Key, HttpUtility.HtmlEncode (choice.Value + string.Empty) + ((this is ConfigChoice) ? (" &mdash; [ " + HttpUtility.HtmlEncode (choice.Key + string.Empty) + " ]") : string.Empty), (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ((Array.IndexOf<string> (vals, choice.Key + string.Empty) >= 0) ? " selected=\"selected\"" : string.Empty));
						return string.Format ("<select " + (IsMulti (prop.RawSchema) ? "multiple=\"multiple\" size=\"" + Choices.Count + "\" " : string.Empty) + "onchange=\"roxHasChanged();\" class=\"{2}\" id=\"{0}\" name=\"{0}\">{1}</select>", instance ["id"] + "_" + prop.Name, options, CssClass);
					}

					public override string RenderValueForDisplay (Property prop, object val) {
						string [] vals = (val + string.Empty).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						List<string> list = new List<string> ();
						foreach (DictionaryEntry entry in Choices)
							if (Array.IndexOf<string> (vals, entry.Key + string.Empty) >= 0)
								list.Add (entry.Value + string.Empty);
						return ((list.Count == 0) ? (val + string.Empty) : string.Join (" · ", list.ToArray ()));
					}

					public override object FromPostedValue (Property prop, string value, Schema owner) {
						string [] vals = null;
						if (formKey != null) {
							try {
								vals = HttpContext.Current.Request.Form.GetValues (formKey);
							} catch {
							}
							if (vals != null)
								return string.Join ("\r\n", vals);
						}
						return base.FromPostedValue (prop, value, owner);
					}

					public override void Update (IDictionary inst, Property prop, HttpContext context, string formKey) {
						this.formKey = formKey;
						base.Update (inst, prop, context, formKey);
					}

					public override string CssClass {
						get {
							return "rox-iteminst-edit-control rox-iteminst-edit-" + typeof (Choice).Name;
						}
					}

				}

				#endregion

				#region EncodingChoice Class

				public class EncodingChoice : DictChoice {

					internal static IDictionary GetChoices () {
						int c = -1;
						StrTuple item;
						IDictionary dict = new OrderedDictionary ();
						List<StrTuple> items = new List<StrTuple> ();
						foreach (EncodingInfo encoding in Encoding.GetEncodings ())
							items.Add (new StrTuple (encoding.CodePage.ToString (), encoding.DisplayName));
						items.Sort (delegate (StrTuple one, StrTuple two) {
							int r = one.Value.CompareTo (two.Value);
							return ((r == 0) ? one.Key.CompareTo (two.Key) : r);
						});
						for (int i = 0; i < items.Count; i++)
							if (items [i].Key.StartsWith ("unicode") || items [i].Key.StartsWith ("utf-") || items [i].Key.EndsWith ("ascii")) {
								item = new StrTuple (items [i].Key, items [i].Value);
								items.RemoveAt (i);
								items.Insert (++c, item);
							}
						for (int i = 0; i < items.Count; i++)
							if ("utf-8".Equals (items [i].Key)) {
								item = new StrTuple (items [i].Key, items [i].Value);
								items.RemoveAt (i);
								items.Insert (0, item);
								break;
							}
						dict.Add (string.Empty, ProductPage.GetProductResource ("DefaultEncoding", Encoding.Default.EncodingName));
						foreach (StrTuple kvp in items)
							if (dict.Contains (kvp.Key))
								dict [kvp.Key] = kvp.Value;
							else
								dict.Add (kvp.Key, kvp.Value);
						return dict;
					}

					public EncodingChoice ()
						: base (GetChoices ()) {
					}

					public override string RenderValueForDisplay (Property prop, object val) {
						return string.IsNullOrEmpty (val + string.Empty) ? string.Empty : base.RenderValueForDisplay (prop, val);
					}

				}

				#endregion

				#region EnumChoice Class

				public class EnumChoice : Choice {

					private SysType enumType = null;
					private string enumTypeName = null;
					private string [] exclude = null;

					public EnumChoice ()
						: base (new ArrayList ()) {
					}

					internal void Init (IDictionary rawSchema) {
						if (enumTypeName == null)
							enumTypeName = rawSchema ["enumtype"] + string.Empty;
						if (exclude == null)
							exclude = (rawSchema ["exclude"] + string.Empty).Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					}

					public override string GetChoiceDesc (Property prop, object choice) {
						Init (prop.RawSchema);
						return ProductPage.GetResource ("PD_" + EnumType.Name + "_" + choice);
					}

					public override IEnumerable GetChoices (IDictionary rawSchema) {
						string tmp;
						List<string> names;
						Array arr;
						Init (rawSchema);
						if (((arr = Enum.GetValues (EnumType)) != null) && (arr.Length > 0)) {
							names = new List<string> (arr.Length);
							foreach (object o in arr)
								if ((!names.Contains (tmp = Enum.GetName (EnumType, o))) && (Array.IndexOf<string> (exclude, tmp) < 0))
									names.Insert ((tmp.Equals (rawSchema ["default"])) ? 0 : names.Count, tmp);
							foreach (string n in names)
								yield return n;
						}
					}

					public override string GetChoiceTitle (Property prop, object choice) {
						string c;
						Init (prop.RawSchema);
						return ((string.IsNullOrEmpty (c = ProductPage.GetResource ("PC_" + EnumType.Name + "_" + choice))) ? choice.ToString () : c);
					}

					public override string CssClass {
						get {
							return "rox-iteminst-edit-control rox-iteminst-edit-" + typeof (Choice).Name;
						}
					}

					public SysType EnumType {
						get {
							if (enumType == null)
								enumType = SysType.GetType (enumTypeName, false, true);
							return enumType;
						}
					}

				}

				#endregion

				#region LibSet Class

				public class LibSet : ListSet {

					public static readonly SPListTemplateType [] SupportedTemplateTypes = { SPListTemplateType.DataConnectionLibrary, SPListTemplateType.DataSources, SPListTemplateType.DocumentLibrary, SPListTemplateType.HomePageLibrary, SPListTemplateType.NoCodeWorkflows, SPListTemplateType.PictureLibrary, SPListTemplateType.WebPageLibrary, SPListTemplateType.XMLForm, SPListTemplateType.ListTemplateCatalog, SPListTemplateType.WebTemplateCatalog, SPListTemplateType.MasterPageCatalog };
					public static readonly int [] SupportedTemplateTypes2 = { 851, 10102, 2100, 122, 117, 433 };

					internal override bool IsSupported (SPList list) {
						return (list is SPDocumentLibrary);
					}

					internal override bool IsSupported (SPBaseType type) {
						return (type == SPBaseType.DocumentLibrary);
					}

					internal override bool IsSupported (SPListTemplateType type) {
						return ((Array.IndexOf<SPListTemplateType> (SupportedTemplateTypes, type) >= 0) || (Array.IndexOf<int> (SupportedTemplateTypes2, (int) type) >= 0));
					}

					public override string CssClass {
						get {
							return "rox-iteminst-edit-control rox-iteminst-edit-" + typeof (ListSet).Name;
						}
					}

					public override bool IgnoreBaseType {
						get {
							return true;
						}
					}

				}

				#endregion

				#region ListSet Class

				public class ListSet : Type {

					#region Config Class

					public class Config {

						public readonly ArrayList List;
						public readonly IDictionary ListSet, ViewSet;

						public Config (object val) {
							if ((List = val as ArrayList) == null)
								List = new ArrayList ();
							if ((List.Count < 1) || ((ListSet = List [0] as IDictionary) == null))
								List.Insert (0, ListSet = new OrderedDictionary ());
							if ((List.Count < 2) || ((ViewSet = List [1] as IDictionary) == null))
								List.Insert (1, ViewSet = new OrderedDictionary ());
						}

						public bool IsChecked (SPListTemplateType val) {
							return (Array.IndexOf<string> (ListTemplateTypes, val.ToString ()) >= 0);
						}

						public bool IsChecked (SPListTemplate val) {
							return (Array.IndexOf<string> (ListBaseTemplates, val.InternalName) >= 0);
						}

						public bool IsChecked (SPBaseType val) {
							return (Array.IndexOf<string> (ListBaseTypes, val.ToString ()) >= 0);
						}

						public bool IsMatch (SPList list, JsonSchemaManager schemaManager) {
							bool isException = false;
							string [] listNames, listBaseTemplates, listBaseTypes, listTemplateTypes;
							SPListTemplate lt;
							if (list == null)
								return true;
							if ((!isException) && ((listNames = ListNames) != null) && (listNames.Length > 0))
								foreach (string ln in listNames)
									if (isException = ((list.DefaultViewUrl.IndexOf (ln.Trim () + "/", StringComparison.InvariantCultureIgnoreCase) >= 0) || ln.Trim ().Equals (list.Title.Trim (), StringComparison.InvariantCultureIgnoreCase) || list.ID.Equals (ProductPage.GetGuid (ln.Trim (), true))))
										break;
							if ((!isException) && ((listBaseTypes = ListBaseTypes) != null) && (listBaseTypes.Length > 0))
								foreach (string lbt in listBaseTypes)
									if (isException = lbt.Equals (list.BaseType.ToString (), StringComparison.InvariantCultureIgnoreCase))
										break;
							if ((!isException) && ((listTemplateTypes = ListTemplateTypes) != null) && (listTemplateTypes.Length > 0))
								foreach (string ltt in listTemplateTypes)
									if (isException = ltt.Equals (list.BaseTemplate.ToString (), StringComparison.InvariantCultureIgnoreCase))
										break;
							if ((!isException) && ((listBaseTemplates = ListBaseTemplates) != null) && (listBaseTemplates.Length > 0))
								foreach (string lbt in listBaseTemplates)
									if (isException = (((lt = schemaManager.GetListTemplate (lbt)) != null) && (list.TemplateFeatureId.Equals (lt.FeatureId))))
										break;
							return ((ListNone && isException) || ((!ListNone) && (!isException)));
						}

						public bool IsMatch (SPView view) {
							bool isException = false;
							string [] viewNames;
							if (view == null)
								return true;
							if ((!isException) && ViewDefault && view.DefaultView)
								isException = true;
							if ((!isException) && ViewHidden && view.Hidden)
								isException = true;
							if ((!isException) && ViewPersonal && view.PersonalView)
								isException = true;
							if ((!isException) && ViewNoTitle && string.IsNullOrEmpty (view.Title))
								isException = true;
							if ((!isException) && ((viewNames = ViewNames) != null) && (viewNames.Length > 0))
								foreach (string vn in viewNames)
									if (isException = ((view.Url.IndexOf (vn.Trim (), StringComparison.InvariantCultureIgnoreCase) >= 0) || vn.Trim ().Equals (view.Title.Trim (), StringComparison.InvariantCultureIgnoreCase) || view.ID.Equals (ProductPage.GetGuid (vn.Trim (), true))))
										break;
							return ((ViewNone && isException) || ((!ViewNone) && (!isException)));
						}

						public string [] ListBaseTemplates {
							get {
								ArrayList list = ListSet ["bt"] as ArrayList;
								if (list == null)
									list = new ArrayList ();
								return list.ToArray (typeof (string)) as string [];
							}
							set {
								ArrayList list = new ArrayList ((value == null) ? 0 : value.Length);
								foreach (string bt in value)
									list.Add (bt);
								if (list.Count == 0)
									ListSet.Remove ("bt");
								else
									ListSet ["bt"] = list;
							}
						}

						public string [] ListBaseTypes {
							get {
								ArrayList list = ListSet ["b"] as ArrayList;
								if (list == null)
									list = new ArrayList ();
								return list.ToArray (typeof (string)) as string [];
							}
							set {
								ArrayList list = new ArrayList ((value == null) ? 0 : value.Length);
								foreach (string bt in value)
									list.Add (bt);
								if (list.Count == 0)
									ListSet.Remove ("b");
								else
									ListSet ["b"] = list;
							}
						}

						public string [] ListTemplateTypes {
							get {
								ArrayList list = ListSet ["t"] as ArrayList;
								if (list == null)
									list = new ArrayList ();
								return list.ToArray (typeof (string)) as string [];
							}
							set {
								ArrayList list = new ArrayList ((value == null) ? 0 : value.Length);
								foreach (string bt in value)
									list.Add (bt);
								if (list.Count == 0)
									ListSet.Remove ("t");
								else
									ListSet ["t"] = list;
							}
						}

						public string [] ListNames {
							get {
								ArrayList list = ListSet ["i"] as ArrayList;
								if (list == null)
									return new string [0];
								else
									return list.ToArray (typeof (string)) as string [];
							}
							set {
								List<string> list = ((value == null) ? new List<string> () : new List<string> (value)).ConvertAll<string> ((s) => {
									return s.Trim ();
								});
								ProductPage.RemoveDuplicates<string> (list);
								if (list.Count == 0)
									ListSet.Remove ("i");
								else
									ListSet ["i"] = new ArrayList (list);
							}
						}

						public bool ListNone {
							get {
								return "1".Equals (ListSet ["n"]);
							}
							set {
								ListSet ["n"] = (value ? "1" : "0");
							}
						}

						public bool ViewDefault {
							get {
								return "1".Equals (ViewSet ["d"]);
							}
							set {
								if (value)
									ViewSet ["d"] = "1";
								else
									ViewSet.Remove ("d");
							}
						}

						public bool ViewHidden {
							get {
								return "1".Equals (ViewSet ["h"]);
							}
							set {
								if (value)
									ViewSet ["h"] = "1";
								else
									ViewSet.Remove ("h");
							}
						}

						public string [] ViewNames {
							get {
								ArrayList list = ViewSet ["i"] as ArrayList;
								if (list == null)
									return new string [0];
								else
									return list.ToArray (typeof (string)) as string [];
							}
							set {
								List<string> list = ((value == null) ? new List<string> () : new List<string> (value)).ConvertAll<string> ((s) => {
									return s.Trim ();
								});
								ProductPage.RemoveDuplicates<string> (list);
								if (list.Count == 0)
									ViewSet.Remove ("i");
								else
									ViewSet ["i"] = new ArrayList (list);
							}
						}

						public bool ViewNone {
							get {
								return "1".Equals (ViewSet ["n"]);
							}
							set {
								ViewSet ["n"] = (value ? "1" : "0");
							}
						}

						public bool ViewNoTitle {
							get {
								return "1".Equals (ViewSet ["w"]);
							}
							set {
								if (value)
									ViewSet ["w"] = "1";
								else
									ViewSet.Remove ("w");
							}
						}

						public bool ViewPersonal {
							get {
								return "1".Equals (ViewSet ["p"]);
							}
							set {
								if (value)
									ViewSet ["p"] = "1";
								else
									ViewSet.Remove ("p");
							}
						}

					}

					#endregion

					internal virtual bool IsSupported (SPList list) {
						return true;
					}

					internal virtual bool IsSupported (SPBaseType type) {
						int i = (int) type;
						return ((i != 2) && (i >= 0));
					}

					internal virtual bool IsSupported (SPListTemplateType type) {
						return (((int) type) > 0);
					}

					internal virtual bool IsSupported (SPListTemplate lt) {
						return ((lt != null) && IsSupported (lt.BaseType));
					}

					public override string RenderValueForEdit (Property prop, IDictionary instance, bool disabled, bool readOnly) {
						//	[ { "n": "0", "i": ["", ""], "b": ["", ""], "bt": ["", ""], "t": ["", ""] },
						//	  { "n": "0", "i": ["", ""], "d": true/false, "w": true/false } ]
						string html = string.Empty, id = GetFormKey (instance, prop);
						Config cfg = new Config (instance [prop.Name]);
						html += "<div id=\"roxlistsetouter_" + id + "\" class=\"" + CssClass + "\"><select onchange=\"roxHasChanged();\" id=\"" + id + "_l_n\" name=\"" + id + "_l_n\"><option value=\"0\"" + ((!cfg.ListNone) ? " selected=\"selected\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ">" + this ["Tool_ItemEditor_ListSet_All", this ["Tool_ItemEditor_" + GetType ().Name + "_Sel"]] + "</option><option value=\"1\"" + (cfg.ListNone ? " selected=\"selected\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ">" + this ["Tool_ItemEditor_ListSet_None", this ["Tool_ItemEditor_" + GetType ().Name + "_Sel"]] + "</option></select> <a href=\"#noop\" onclick=\"roxToggleListSel('" + instance ["id"] + "', '" + prop.Name + "');\">" + this ["Tool_ItemEditor_ListSet_Except", (cfg.ListSet.Count > 1) ? (cfg.ListSet.Count - 1) : 0] + "</a>&nbsp;&nbsp;&nbsp;&mdash;&nbsp;&nbsp;&nbsp;<select onchange=\"roxHasChanged();\" id=\"" + id + "_v_n\" name=\"" + id + "_v_n\"><option value=\"0\"" + ((!cfg.ViewNone) ? " selected=\"selected\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ">" + this ["Tool_ItemEditor_ListSet_All", this ["Tool_ItemEditor_ListSet_Views"]] + "</option><option value=\"1\"" + (cfg.ViewNone ? " selected=\"selected\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ">" + this ["Tool_ItemEditor_ListSet_None", this ["Tool_ItemEditor_ListSet_Views"]] + "</option></select> <a href=\"#noop\" onclick=\"roxToggleListSel('" + instance ["id"] + "', '" + prop.Name + "');\">" + this ["Tool_ItemEditor_ListSet_Except", (cfg.ViewSet.Count > 1) ? (cfg.ViewSet.Count - 1) : 0] + "</a></div>";
						html += "<div id=\"roxlistsetinner_" + id + "\" style=\"display: none;\" class=\"" + CssClass + "-inner\">";
						//html += "<div class=\"" + CssClass + "-picker\" style=\"display: none;\"><div class=\"" + CssClass + "-exbox\" style=\"float: left;\">";
						//html += "Select a List:<br/><select size=\"6\">";
						//foreach (SPWeb web in ProductPage.TryEach<SPWeb> (prop.Owner.Owner.Site.AllWebs, false, null, true))
						//    foreach (SPList lst in ProductPage.TryEach<SPList> (web.Lists, false, null, true))
						//        if (IsSupported (lst))
						//            html += string.Format ("<option>" + lst.DefaultViewUrl.Substring (0, ((pos = lst.DefaultViewUrl.LastIndexOf ("/Forms/", StringComparison.InvariantCultureIgnoreCase)) > 0) ? pos : lst.DefaultViewUrl.LastIndexOf ('/')) + "</option>");
						//html += "</select></div><div class=\"" + CssClass + "-exbox\" style=\"float: right;\">view</div><br style=\"clear: both;\"/></div>";
						html += "<div class=\"" + CssClass + "-exbox\" style=\"float: left; border-right: #909090 1px dotted;\">";
						html += "<div>" + this ["Tool_ItemEditor_ListSet_ExceptBaseType"] + "</div><select onchange=\"roxHasChanged();\" size=\"" + (IgnoreBaseType ? "2\" disabled=\"disabled" : "4") + "\" id=\"" + id + "_l_b\" name=\"" + id + "_l_b\" multiple=\"multiple\">";
						foreach (SPBaseType val in Enum.GetValues (typeof (SPBaseType)))
							if (IsSupported (val))
								html += "<option value=\"" + val + "\"" + (cfg.IsChecked (val) ? " selected=\"selected\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ">" + val + "</option>";
						html += "</select>";
						html += "<div>" + this ["Tool_ItemEditor_ListSet_ExceptTemplateType"] + "</div><select onchange=\"roxHasChanged();\" size=\"4\" multiple=\"multiple\" id=\"" + id + "_l_t\" name=\"" + id + "_l_t\" >";
						foreach (SPListTemplateType val in Enum.GetValues (typeof (SPListTemplateType)))
							if (IsSupported (val))
								html += "<option value=\"" + val + "\"" + (cfg.IsChecked (val) ? " selected=\"selected\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ">" + ((int) val) + " &mdash; " + val + "</option>";
						html += "</select>";
						html += "<div>" + this ["Tool_ItemEditor_ListSet_ExceptBaseTemplate"] + "</div><select onchange=\"roxHasChanged();\" size=\"4\" multiple=\"multiple\" id=\"" + id + "_l_bt\" name=\"" + id + "_l_bt\">";
						foreach (SPListTemplate lt in prop.Owner.Owner.ListTemplates)
							if (IsSupported (lt))
								html += "<option value=\"" + lt.InternalName + "\"" + (cfg.IsChecked (lt) ? " selected=\"selected\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ">" + HttpUtility.HtmlEncode (lt.Name) + "</option>";
						html += "</select>";
						html += "<div>" + this ["Tool_ItemEditor_ListSet_ExceptListName"] + "</div><textarea onchange=\"roxHasChanged();\" rows=\"2\" class=\"rox-iteminst-edit-String" + ((disabled || readOnly) ? " rox-iteminst-edit-String-readonly" : string.Empty) + "\" id=\"" + id + "_l_i\" name=\"" + id + "_l_i\"" + ((disabled || readOnly) ? READONLY : string.Empty) + ">" + string.Join ("\n", cfg.ListNames) + "</textarea>";
						html += "</div><div class=\"" + CssClass + "-exbox\" style=\"float: right;\">";
						html += "<div>" + this ["Tool_ItemEditor_ListSet_ExceptView"] + "<br/><input onclick=\"roxHasChanged();\" id=\"" + id + "_v_d\" name=\"" + id + "_v_d\" type=\"checkbox\"" + (cfg.ViewDefault ? " checked=\"checked\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + " /> <label for=\"" + id + "_v_d\">" + this ["Tool_ItemEditor_ListSet_ExceptDefaultView"] + "</label></div>";
						html += "<div><input onclick=\"roxHasChanged();\" id=\"" + id + "_v_h\" name=\"" + id + "_v_h\" type=\"checkbox\"" + (cfg.ViewHidden ? " checked=\"checked\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + " /> <label for=\"" + id + "_v_h\">" + this ["Tool_ItemEditor_ListSet_ExceptViewHidden"] + "</label></div>";
						html += "<div><input onclick=\"roxHasChanged();\" id=\"" + id + "_v_p\" name=\"" + id + "_v_p\" type=\"checkbox\"" + (cfg.ViewPersonal ? " checked=\"checked\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + " /> <label for=\"" + id + "_v_p\">" + this ["Tool_ItemEditor_ListSet_ExceptViewPersonal"] + "</label></div>";
						html += "<div><input onclick=\"roxHasChanged();\" id=\"" + id + "_v_w\" name=\"" + id + "_v_w\" type=\"checkbox\"" + (cfg.ViewNoTitle ? " checked=\"checked\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + " /> <label for=\"" + id + "_v_w\">" + this ["Tool_ItemEditor_ListSet_ExceptViewWebPart"] + "</label></div>";
						html += "<div>" + this ["Tool_ItemEditor_ListSet_ExceptViewName"] + "<a class=\"rox-iteminst-edit-Pick\" style=\"display: none;\" href=\"#\"/><img src=\"/_layouts/images/blank.gif\" width=\"16\" height=\"16\"/></a></div><textarea onchange=\"roxHasChanged();\" rows=\"" + (IgnoreBaseType ? 9 : 11) + "\" class=\"rox-iteminst-edit-String" + ((disabled || readOnly) ? " rox-iteminst-edit-String-readonly" : string.Empty) + "\" id=\"" + id + "_v_i\" name=\"" + id + "_v_i\"" + ((disabled || readOnly) ? READONLY : string.Empty) + ">" + string.Join ("\n", cfg.ViewNames) + "</textarea>";
						html += "</div>";
						html += "<br style=\"clear: both;\"/>&nbsp;</div>";
						return html;
					}

					public override string ToString (Property prop, object val) {
						return string.Empty;
					}

					public override void Update (IDictionary inst, Property prop, HttpContext context, string id) {
						Config cfg = new Config (inst [prop.Name]);
						Converter<string, string> f = delegate (string k) {
							return context.Request.Form [id + k] + string.Empty;
						};
						string viewNone = f ("_v_n"), viewDef = f ("_v_d"), viewHidden = f ("_v_h"), viewPersonal = f ("_v_p"), viewNoTitle = f ("_v_w"), viewNames = f ("_v_i"), listNone = f ("_l_n"), listNames = f ("_l_i"), listBaseTypes = f ("_l_b"), listTemplateTypes = f ("_l_t"), listBaseTemplates = f ("_l_bt");
						cfg.ViewNone = "1".Equals (viewNone);
						cfg.ViewDefault = "on".Equals (viewDef, StringComparison.InvariantCultureIgnoreCase);
						cfg.ViewHidden = "on".Equals (viewHidden, StringComparison.InvariantCultureIgnoreCase);
						cfg.ViewPersonal = "on".Equals (viewPersonal, StringComparison.InvariantCultureIgnoreCase);
						cfg.ViewNoTitle = "on".Equals (viewNoTitle, StringComparison.InvariantCultureIgnoreCase);
						cfg.ViewNames = viewNames.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						cfg.ListNone = "1".Equals (listNone);
						cfg.ListNames = listNames.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						cfg.ListBaseTemplates = listBaseTemplates.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
						cfg.ListBaseTypes = listBaseTypes.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
						cfg.ListTemplateTypes = listTemplateTypes.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
						inst [prop.Name] = cfg.List;
					}

					public virtual bool IgnoreBaseType {
						get {
							return false;
						}
					}

				}

				#endregion

				#region LocaleChoice Class

				public class LocaleChoice : DictChoice {

					internal static readonly List<StrTuple> staticLocales = new List<StrTuple> ();
					internal static readonly Dictionary<string, CultureInfo> cachedCultures = new Dictionary<string, CultureInfo> ();

					public static IDictionary GetChoices () {
						IDictionary dict = new OrderedDictionary ();
						List<StrTuple> list = new List<StrTuple> ();
						foreach (CultureInfo culture in ProductPage.AllSpecificCultures)
							list.Add (new StrTuple (culture.Name, ToString (culture)));
						list.Sort (delegate (StrTuple one, StrTuple two) {
							int r = one.Value.CompareTo (two.Value);
							return ((r == 0) ? one.Key.CompareTo (two.Key) : r);
						});
						list.InsertRange (0, StaticLocales);
						foreach (StrTuple kvp in list)
							dict.Add (kvp.Key, string.Format (kvp.Value, ToString (GetCulture (kvp.Key))));
						return dict;
					}

					public static CultureInfo GetCulture (string staticName) {
						CultureInfo culture = null;
						if (!cachedCultures.TryGetValue (staticName, out culture)) {
							if (staticName == "System")
								culture = CultureInfo.InstalledUICulture;
							else if (staticName == "Web")
								try {
									ProductPage.Elevate (delegate () {
										culture = new CultureInfo ((int) SPContext.Current.Web.RegionalSettings.LocaleId);
									}, true);
								} catch {
								} else if (staticName == "User")
								try {
									ProductPage.Elevate (delegate () {
										if (SPContext.Current.Web.CurrentUser.RegionalSettings != null)
											culture = new CultureInfo ((int) SPContext.Current.Web.CurrentUser.RegionalSettings.LocaleId);
										else
											culture = new CultureInfo ((int) SPContext.Current.Web.RegionalSettings.LocaleId);
									}, true);
								} catch {
								} else if (staticName == "Browser")
								try {
									foreach (string loc in HttpContext.Current.Request.UserLanguages) {
										try {
											culture = new CultureInfo (loc);
										} catch {
										}
										if (culture != null)
											break;
									}
								} catch {
								} else if ((staticName != "Current") && !string.IsNullOrEmpty (staticName))
								try {
									culture = new CultureInfo (staticName);
								} catch {
								}
							cachedCultures [staticName] = culture;
						}
						return (((culture == null) || (culture.LCID == CultureInfo.InvariantCulture.LCID)) ? CultureInfo.CurrentUICulture : culture);
					}

					public static string ToString (CultureInfo culture) {
						return culture.DisplayName + (culture.DisplayName.Equals (culture.NativeName) ? string.Empty : (" / " + culture.NativeName));
					}

					public static List<StrTuple> StaticLocales {
						get {
							lock (staticLocales) {
								if (staticLocales.Count == 0)
									foreach (string s in new string [] { "Current", "Web", "User", "System", "Browser" })
										staticLocales.Add (new StrTuple (s, HttpUtility.HtmlDecode ("&rarr; ") + ProductPage.GetProductResource ("Locale" + s)));
							}
							return staticLocales;
						}
					}

					public LocaleChoice ()
						: base (GetChoices ()) {
					}

					public override string RenderValueForDisplay (Property prop, object val) {
						return "Current".Equals (val) ? string.Empty : base.RenderValueForDisplay (prop, val);
					}

				}

				#endregion

				#region String Class

				public class String : Type {

					public static bool IsPassword (IDictionary rawSchema) {
						return ((rawSchema ["is_password"] is bool) && (bool) rawSchema ["is_password"]);
					}

					public override object FromPostedValue (Property prop, string value, Schema owner) {
						return base.FromPostedValue (prop, (ProductPage.GetResource ("Tool_ItemEditor_NewDesc", ProductPage.GetProductResource ("Tool_" + owner.Name + "_TitleSingular")).Equals (value)) ? string.Empty : value, owner);
					}

					public override string RenderValueForDisplay (Property prop, object val) {
						object lines;
						int l;
						bool ispwd = IsPassword (prop.RawSchema);
						string delim, baseVal = ispwd ? "**(password set)**" : base.RenderValueForDisplay (prop, val);
						if ((!ispwd) && int.TryParse ((lines = prop.RawSchema ["lines"]) + string.Empty, out l) && (l > 1))
							return string.Join (string.IsNullOrEmpty (delim = prop.RawSchema ["multiline_summary_delimiter"] + string.Empty) ? ", " : delim, baseVal.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
						else
							return baseVal;
					}

					public override string RenderValueForEdit (Property prop, IDictionary instance, bool disabled, bool readOnly) {
						int l;
						object lines = prop.RawSchema ["lines"];
						string val = prop.RawSchema ["validator"] + string.Empty, valScript = string.Empty, ctlID = instance ["id"] + "_" + prop.Name;
						if (!string.IsNullOrEmpty (val))
							valScript = "var oval=jQuery('#" + SPEncode.ScriptEncode (ctlID) + "').val(),val=" + val + "('" + SPEncode.ScriptEncode (instance ["id"] + string.Empty) + "', '" + SPEncode.ScriptEncode (prop.Name) + "', '" + SPEncode.ScriptEncode (prop.DefaultValue + string.Empty) + "');if(val!=oval)jQuery('#roxiteminstdesc_" + instance ["id"] + "_" + prop.Name + "').show();if(val===null)setTimeout('jQuery(\\\"#" + SPEncode.ScriptEncode (ctlID) + "\\\").focus();',250);else jQuery('#" + SPEncode.ScriptEncode (ctlID) + "').val(val);";
						valScript += "roxHasChanged();";
						if (int.TryParse (lines + string.Empty, out l) && (l > 1))
							return string.Format ("<textarea onchange=\"" + HttpUtility.HtmlAttributeEncode (valScript) + "\" class=\"{2}\" rows=\"{3}\" id=\"{0}\" name=\"{0}\"{4}>{1}</textarea>", ctlID, HttpUtility.HtmlEncode (instance [prop.Name] + string.Empty), CssClass + ((disabled || readOnly) ? " rox-iteminst-edit-String-readonly" : string.Empty), l, ((disabled || readOnly) ? READONLY : string.Empty));
						else
							return string.Format ("<input onchange=\"" + HttpUtility.HtmlAttributeEncode (valScript) + "\" class=\"{2}\" type=\"" + (IsPassword (prop.RawSchema) ? "password" : "text") + "\" id=\"{0}\" name=\"{0}\" value=\"{1}\"{3}/>", ctlID, HttpUtility.HtmlEncode (instance [prop.Name] + string.Empty), CssClass + ((disabled || readOnly) ? " rox-iteminst-edit-String-readonly" : string.Empty), ((disabled || readOnly) ? READONLY : string.Empty));
					}

				}

				#endregion

				#region WebSet Class

				public class WebSet : Type {

					#region Config Class

					public class Config {

						public readonly IDictionary WebSet;

						public Config (object val) {
							if ((WebSet = val as IDictionary) == null)
								WebSet = new OrderedDictionary ();
						}

						public bool IsChecked (WebTemplateType val) {
							return (Array.IndexOf<string> (WebTemplates, val.ToString ()) >= 0);
						}

						public bool IsMatch (SPWeb web) {
							bool isException = false;
							string [] webNames, webTemps;
							if (web == null)
								return true;
							if ((!isException) && WebRoot && web.IsRootWeb)
								isException = true;
							if ((!isException) && ((webNames = WebNames) != null) && (webNames.Length > 0))
								foreach (string wn in webNames)
									if (isException = ((web.Url.IndexOf (wn.Trim (), StringComparison.InvariantCultureIgnoreCase) >= 0) || wn.Trim ().Equals (web.Title.Trim (), StringComparison.InvariantCultureIgnoreCase) || web.ID.Equals (ProductPage.GetGuid (wn.Trim (), true))))
										break;
							if ((!isException) && ((webTemps = WebTemplates) != null) && (webTemps.Length > 0))
								foreach (string wt in webTemps)
									if (isException = wt.Equals (web.WebTemplate, StringComparison.InvariantCultureIgnoreCase))
										break;
							return ((WebNone && isException) || ((!WebNone) && (!isException)));
						}

						public string [] WebTemplates {
							get {
								ArrayList list = WebSet ["t"] as ArrayList;
								if (list == null)
									list = new ArrayList ();
								return list.ToArray (typeof (string)) as string [];
							}
							set {
								ArrayList list = new ArrayList ((value == null) ? 0 : value.Length);
								foreach (string bt in value)
									list.Add (bt);
								if (list.Count == 0)
									WebSet.Remove ("t");
								else
									WebSet ["t"] = list;
							}
						}


						public string [] WebNames {
							get {
								ArrayList list = WebSet ["i"] as ArrayList;
								if (list == null)
									return new string [0];
								else
									return list.ToArray (typeof (string)) as string [];
							}
							set {
								List<string> list = ((value == null) ? new List<string> () : new List<string> (value)).ConvertAll<string> ((s) => {
									return s.Trim ();
								});
								ProductPage.RemoveDuplicates<string> (list);
								if (list.Count == 0)
									WebSet.Remove ("i");
								else
									WebSet ["i"] = new ArrayList (list);
							}
						}

						public bool WebNone {
							get {
								return "1".Equals (WebSet ["n"]);
							}
							set {
								WebSet ["n"] = (value ? "1" : "0");
							}
						}

						public bool WebRoot {
							get {
								return "1".Equals (WebSet ["d"]);
							}
							set {
								if (value)
									WebSet ["d"] = "1";
								else
									WebSet.Remove ("d");
							}
						}

					}

					#endregion

					#region WebTemplateType Enumeration

					public enum WebTemplateType {

						GLOBAL = 0,
						STS = 1,
						MPS = 2,
						CENTRALADMIN = 3,
						WIKI = 4,
						BDR = 7,
						BLOG = 9,
						SPSPERS = 21,
						SPSMSITE = 22,
						SPSNHOME = 33,
						SPSSITES = 34,
						SPSREPORTCENTER = 38,
						CMSPUBLISHING = 39,
						OSRV = 40,
						SPSPORTAL = 47,
						SRCHCEN = 50,
						PROFILES = 51,
						BLANKINTERNETCONTAINER = 52,
						BLANKINTERNET = 53,
						SPSMSITEHOST = 54,
						SRCHCENTERLITE = 90,
						PWA = 6221,
						PWS = 6215,
						OFFILE = 14483,
						AccSvr = -1,
						BICenterSite = -2,
						ENTERWIKI = -3,
						PPSMASite = -4,
						PUBLISHING = -5,
						SGS = -6,
						SRCHCENTERFAST = -7,
						TenantAdmin = -8,
						VISPR = -9,
						VISPRUS = -10,
						WebManifest = -11

					}

					#endregion

					internal virtual bool IsSupported (WebTemplateType template) {
						return true;
					}

					public override string RenderValueForEdit (Property prop, IDictionary instance, bool disabled, bool readOnly) {
						//	{ "n": "0", "i": ["", ""], "t": ["", ""], "d": true/false }
						string html = string.Empty, id = GetFormKey (instance, prop), tmp;
						Config cfg = new Config (instance [prop.Name]);
						html += "<div id=\"roxlistsetouter_" + id + "\" class=\"" + CssClass + "\"><select onchange=\"roxHasChanged();\" id=\"" + id + "_w_n\" name=\"" + id + "_w_n\"><option value=\"0\"" + ((!cfg.WebNone) ? " selected=\"selected\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ">" + this ["Tool_ItemEditor_WebSet_All", prop.Owner.Owner.SiteScope ? ("&quot;" + HttpUtility.HtmlEncode (prop.Owner.Owner.Web.Title) + "&quot;") : string.Empty] + "</option><option value=\"1\"" + (cfg.WebNone ? " selected=\"selected\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ">" + this ["Tool_ItemEditor_WebSet_None", prop.Owner.Owner.SiteScope ? ("&quot;" + HttpUtility.HtmlEncode (prop.Owner.Owner.Web.Title) + "&quot;") : string.Empty] + "</option></select> <a href=\"#noop\" onclick=\"roxToggleListSel('" + instance ["id"] + "', '" + prop.Name + "');\">" + this ["Tool_ItemEditor_ListSet_Except", (cfg.WebSet.Count > 1) ? (cfg.WebSet.Count - 1) : 0] + "</a></div>";
						html += "<div id=\"roxlistsetinner_" + id + "\" style=\"display: none;\" class=\"" + CssClass + "-inner\">";
						html += "<div class=\"" + CssClass + "-exbox\" style=\"float: left; width: 54%;\">";
						html += "<div>" + this ["Tool_ItemEditor_ListSet_ExceptView"] + "<br/><input onclick=\"roxHasChanged();\" id=\"" + id + "_w_d\" name=\"" + id + "_w_d\" type=\"checkbox\"" + (cfg.WebRoot ? " checked=\"checked\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + " /> <label for=\"" + id + "_w_d\">" + this ["Tool_ItemEditor_WebSet_ExceptRootWeb"] + "</label></div>";
						html += "<div>" + this ["Tool_ItemEditor_WebSet_ExceptTemplate"] + "</div><select onchange=\"roxHasChanged();\" size=\"10\" id=\"" + id + "_w_t\" name=\"" + id + "_w_t\" multiple=\"multiple\">";
						foreach (WebTemplateType val in Enum.GetValues (typeof (WebTemplateType)))
							if (IsSupported (val))
								html += "<option value=\"" + val + "\"" + (cfg.IsChecked (val) ? " selected=\"selected\"" : string.Empty) + (disabled ? DISABLED : string.Empty) + (readOnly ? READONLY : string.Empty) + ">" + ((((int) val) >= 0) ? (((int) val) + " &mdash; ") : string.Empty) + val + (string.IsNullOrEmpty (tmp = this ["Tool_ItemEditor_WebSet_Temp_" + val]) ? tmp : (" (" + tmp + ")")) + "</option>";
						html += "</select>";
						html += "</div><div class=\"" + CssClass + "-exbox\" style=\"float: right; width: 40%;\">";
						html += "<div><br/>" + this ["Tool_ItemEditor_WebSet_ExceptWebName"] + "<a class=\"rox-iteminst-edit-Pick\" style=\"display: none;\" href=\"#\"/><img src=\"/_layouts/images/blank.gif\" width=\"16\" height=\"16\"/></a></div><textarea onchange=\"roxHasChanged();\" rows=\"10\" class=\"rox-iteminst-edit-String" + ((disabled || readOnly) ? " rox-iteminst-edit-String-readonly" : string.Empty) + "\" id=\"" + id + "_w_i\" name=\"" + id + "_w_i\"" + ((disabled || readOnly) ? READONLY : string.Empty) + ">" + string.Join ("\n", cfg.WebNames) + "</textarea>";
						html += "</div>";
						html += "&nbsp;<br style=\"clear: both;\"/></div>";
						return html;
					}

					public override string ToString (Property prop, object val) {
						return string.Empty;
					}

					public override void Update (IDictionary inst, Property prop, HttpContext context, string id) {
						Config cfg = new Config (inst [prop.Name]);
						Converter<string, string> f = delegate (string k) {
							return context.Request.Form [id + k] + string.Empty;
						};
						string webNone = f ("_w_n"), webDef = f ("_w_d"), webNames = f ("_w_i"), webTemplates = f ("_w_t");
						cfg.WebNone = "1".Equals (webNone);
						cfg.WebRoot = "on".Equals (webDef, StringComparison.InvariantCultureIgnoreCase);
						cfg.WebNames = webNames.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						cfg.WebTemplates = webTemplates.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
						inst [prop.Name] = cfg.WebSet;
					}

					public override string CssClass {
						get {
							return "rox-iteminst-edit-control rox-iteminst-edit-" + typeof (ListSet).Name;
						}
					}

				}

				#endregion

				internal const string DISABLED = " disabled=\"disabled\" ",
					READONLY = " readonly=\"readonly\" ";

				public virtual object FromPostedValue (Property prop, string value, Schema owner) {
					return value;
				}

				public static Type FromSchema (object typeSpec) {
					SysType type;
					if (typeSpec is string) {
						if ((type = ProductPage.Assembly.GetType (typeof (Type).FullName + "+" + typeSpec, false, true)) == null)
							type = ProductPage.Assembly.GetType ("roxority.SharePoint.JsonSchemaPropertyTypes." + typeSpec, true, true);
						return type.GetConstructor (new SysType [0]).Invoke (null) as Type;
					} else if (typeSpec is IEnumerable)
						return new Choice (typeSpec as IEnumerable);
					return null;
				}

				public virtual object GetDefaultValue (IDictionary rawSchema) {
					return rawSchema ["default"];
				}

				public virtual string GetFormKey (string id, Property prop) {
					return id + "_" + prop.Name;
				}

				public string GetFormKey (IDictionary inst, Property prop) {
					return GetFormKey (inst ["id"] + string.Empty, prop);
				}

				public virtual string RenderValueForEdit (Property prop, IDictionary instance, bool disabled, bool readOnly) {
					return string.Empty;
				}

				public virtual string RenderValueForDisplay (Property prop, object val) {
					return val + string.Empty;
				}

				public override string ToString () {
					return GetType ().Name;
				}

				public virtual string ToString (Property prop, object val) {
					string v = RenderValueForDisplay (prop, val), p = prop.ToString ();
					int pos = p.IndexOf ('(');
					if(pos> 0)
						p=p.Substring(0,pos);
					return string.IsNullOrEmpty (v.Trim ()) ? string.Empty : (p + ": <b>" + v + "</b>");
				}

				public void Update (IDictionary inst, Property prop, HttpContext context) {
					Update (inst, prop, context, GetFormKey (inst, prop));
				}

				public virtual void Update (IDictionary inst, Property prop, HttpContext context, string formKey) {
					inst [prop.Name] = (prop.Le ? FromPostedValue (prop, context.Request.Form [formKey], prop.Owner) : prop.DefaultValue);
				}

				public virtual string CssClass {
					get {
						return "rox-iteminst-edit-control rox-iteminst-edit-" + GetType ().Name;
					}
				}

				public virtual bool IsBool {
					get {
						return false;
					}
				}

				public string this [string name, params object [] args] {
					get {
						return ProductPage.GetResource (name, args);
					}
				}

				public virtual bool ShowInSummary {
					get {
						return true;
					}
				}

			}

			#endregion

			public readonly string Name;
			public readonly IDictionary RawSchema;
			public readonly Schema Owner;

			internal readonly int le;
			private bool? isLe = null;

			public Property (Schema schema, string name, IDictionary rawSchema) {
				Name = name;
				Owner = schema;
				RawSchema = rawSchema;
				if (Array.IndexOf<string> (schema.lp0, name) >= 0)
					le = 0;
				else if (Array.IndexOf<string> (schema.lp4, name) >= 0)
					le = 4;
				else
					le = 2;
			}

			internal bool Le {
				get {
					if ((isLe == null) || !isLe.HasValue)
						isLe = IsLe (le);
					return isLe.Value;
				}
			}

			internal bool IsLe (int l) {
				return ProductPage.LicEdition (ProductPage.GetContext (), Li, le);
			}

			public override string ToString () {
				string resKey = RawSchema ["res_title"] + string.Empty, title = ProductPage.GetProductResource (string.IsNullOrEmpty (resKey) ? ("PC_" + Owner.Name + "_" + Name) : resKey);
				if (string.IsNullOrEmpty (title))
					title = ProductPage.GetResource ("PC_ItemEditor_" + Name, ProductPage.GetProductResource ("Tool_" + Owner.Name + "_TitleSingular"));
				return string.IsNullOrEmpty (title) ? Name : title;
			}

			public bool AlwaysShowHelp {
				get {
					return ((RawSchema ["always_show_help"] is bool) && (bool) RawSchema ["always_show_help"]);
				}
			}

			public object DefaultValue {
				get {
					return PropertyType.GetDefaultValue (RawSchema);
				}
			}

			public string Description {
				get {
					string resKey = RawSchema ["res_desc"] + string.Empty, desc = ProductPage.GetProductResource (string.IsNullOrEmpty (resKey) ? (resKey = "PD_" + Owner.Name + "_" + Name) : resKey), sharedDesc = ProductPage.GetResource (resKey.Substring (0, resKey.Length - 1) + "*");
					if (string.IsNullOrEmpty (desc))
						desc = ProductPage.GetResource ("PD_ItemEditor_" + Name, ProductPage.GetProductResource ("Tool_" + Owner.Name + "_TitleSingular"));
					return desc.StartsWith ("*") ? desc.Substring (1) : (string.IsNullOrEmpty (sharedDesc) ? desc : string.Format (sharedDesc, desc));
				}
			}

			public bool Disabled {
				get {
					return ((RawSchema ["disabled"] is bool) && (bool) RawSchema ["disabled"]);
				}
			}

			public bool Editable {
				get {
					return (!Disabled) && (!ReadOnly) && Owner.Owner.ProdPage.IsApplicableAdmin && Le;
				}
			}

			public string EditHint {
				get {
					return (ReadOnly ? ProductPage.GetResource ("Tool_ItemEditor_ReadOnly") : (Disabled ? ProductPage.GetResource ("Tool_ItemEditor_Disabled", ProductPage.GetTitle ()) : ProductPage.GetResource ("NopeEd", ToString (), (le == 4) ? "Ultimate" : ((le == 2) ? "Basic" : "Lite"))));
				}
			}

			public Type PropertyType {
				get {
					return Type.FromSchema (RawSchema ["type"]);
				}
			}

			public bool ReadOnly {
				get {
					return ((RawSchema ["readonly"] is bool) && (bool) RawSchema ["readonly"]);
				}
			}

			public bool ShowInSummary {
				get {
					object sis = RawSchema ["show_in_summary"];
					return (!Property.Type.String.IsPassword (RawSchema)) && PropertyType.ShowInSummary && ((!(sis is bool)) || (bool) sis);
				}
			}

			public string Tab {
				get {
					return RawSchema ["tab"] + string.Empty;
				}
			}

		}

		#endregion

		#region Schema Class

		public class Schema {

			public readonly string Name;
			public readonly List<Property> Properties = new List<Property> ();
			public readonly SortedDictionary<string, string> PropTabs = new SortedDictionary<string, string> ();
			public readonly IDictionary RawSchema;
			public readonly JsonSchemaManager Owner;
			public readonly bool Saved = false;
			public readonly Exception SaveError = null;
			public bool SavedSilent = false;

			internal readonly string [] lp0, lp2, lp4;
			internal IDictionary instDict = null;
			internal Converter<KeyValuePair<IDictionary, JsonSchemaManager.Property>, bool> ShouldSerialize;

			private Property descProp = null;

			public Schema (JsonSchemaManager schemaManager, string name, IDictionary rawSchema) {
				string firstTab = string.Empty, key, tmp;
				int pos;
				Property nuProp;
				HttpContext context = null;
				IDictionary propSchema = new OrderedDictionary ();
				ISchemaExtender dynSchema = null;
				Name = name;
				RawSchema = rawSchema;
				Owner = schemaManager;
				if (string.IsNullOrEmpty (tmp = ProductPage.GetProductResource ("PL_" + name + "_0")))
					lp0 = new string [0];
				else
					lp0 = tmp.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (string.IsNullOrEmpty (tmp = ProductPage.GetProductResource ("PL_" + name + "_2")))
					lp2 = new string [0];
				else
					lp2 = tmp.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				if (string.IsNullOrEmpty (tmp = ProductPage.GetProductResource ("PL_" + name + "_4")))
					lp4 = new string [0];
				else
					lp4 = tmp.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				foreach (DictionaryEntry entry in RawSchema)
					if (!(key = entry.Key + string.Empty).StartsWith ("_")) {
						Properties.Add (nuProp = new Property (this, key, entry.Value as IDictionary));
						if (string.IsNullOrEmpty (firstTab))
							firstTab = nuProp.Tab;
						if (!PropTabs.ContainsKey (nuProp.Tab))
							PropTabs [nuProp.Tab] = ((pos = nuProp.Tab.IndexOf ("__")) > 0) ? ProductPage.GetResource ("PC_" + Name + "_" + nuProp.Tab.Substring (0, pos) + "_" + nuProp.Tab.Substring (pos + 2)) : ProductPage.GetProductResource ("PT_" + Name + "_" + nuProp.Tab);
					} else if ("_dyn".Equals (key) && !string.IsNullOrEmpty (tmp = entry.Value + string.Empty)) {
						try {
							dynSchema = null;
							dynSchema = Reflector.Current.New (tmp) as ISchemaExtender;
						} catch {
						}
						if (dynSchema != null)
							dynSchema.InitSchema (this);
					}
				propSchema ["type"] = "String";
				propSchema ["tab"] = firstTab;
				Properties.Insert (0, new Property (this, "name", propSchema));
				propSchema = new OrderedDictionary ();
				propSchema ["type"] = "WebSet";
				propSchema ["tab"] = firstTab;
				if (HasWebs)
					Properties.Insert (1, new Property (this, "webs", propSchema));
				try {
					context = HttpContext.Current;
				} catch {
				}
				if ((!noSave) && (context != null) && (context.Request != null) && "POST".Equals (context.Request.HttpMethod, StringComparison.InvariantCultureIgnoreCase) && (!string.IsNullOrEmpty (context.Request.Form ["roxitemsave"])) && Name.Equals (context.Request.Form ["roxitemsaveschema"]) && !context.Items.Contains (context.Request.Form ["roxitemsave"]))
					try {
						context.Items [context.Request.Form ["roxitemsave"]] = new object ();
						SaveChanges (context);
						Saved = true;
					} catch (Exception ex) {
						SaveError = ex;
					}
			}

			internal List<Property> GetPropertiesNoDuplicates () {
				string lastTab = null;
				int pos;
				List<string> tabs = new List<string> (), dups = new List<string> ();
				List<Property> props = new List<Property> (), tmpProps = new List<Property> (Properties);
				tmpProps.Sort (delegate (Property one, Property two) {
					int c = ((one.Tab.Contains ("_") && two.Tab.Contains ("_")) ? 0 : one.Tab.CompareTo (two.Tab));
					return ((c == 0) ? Properties.IndexOf (one).CompareTo (Properties.IndexOf (two)) : c);
				});
				foreach (Property prop in tmpProps) {
					if (prop.Tab != lastTab) {
						if ((!string.IsNullOrEmpty (lastTab)) && !tabs.Contains (lastTab))
							tabs.Add (lastTab);
						lastTab = prop.Tab;
					}
					if (!tabs.Contains (prop.Tab)) {
						if (((pos = prop.Name.IndexOf ('_')) < 0) || (!dups.Contains (prop.Tab + prop.Name.Substring (pos)))) {
							props.Add (prop);
							if (pos > 0)
								dups.Add (prop.Tab + prop.Name.Substring (pos));
						}
					}
				}
				return props;
			}

			public IDictionary CreateDefaultInstance () {
				IDictionary inst = new OrderedDictionary ();
				inst ["id"] = "default";
				foreach (Property prop in Properties)
					inst [prop.Name] = GetDisplayValue (prop.DefaultValue);
				inst ["name"] = ProductPage.GetResource ("Tool_ItemEditor_DefaultName", ProductPage.GetProductResource ("Tool_" + Name + "_TitleSingular"));
				if ((Name != "DataSources") && (Name != "DataFieldFormats"))
					inst ["desc"] = ProductPage.GetResource ("Tool_ItemEditor_DefaultDesc", ProductPage.GetProductResource ("Tool_" + Name + "_TitleSingular"), Owner.ProdPage.ProductName, ProductPage.GetProductResource ("Tool_" + Name + "_Title"), ProductPage.GetSiteTitle (ProductPage.GetContext ()));
				return inst;
			}

			public string GetInstanceDescription (IDictionary inst) {
				if (descProp == null) {
					foreach (Property prop in Properties)
						if ((prop.RawSchema ["is_desc"] is bool) && (bool) prop.RawSchema ["is_desc"]) {
							descProp = prop;
							break;
						} else if ((prop.Name == "desc") && (descProp == null))
							descProp = prop;
				}
				return ((descProp == null) ? string.Empty : descProp.PropertyType.RenderValueForDisplay (descProp, inst [descProp.Name]));
			}

			public IEnumerable<IDictionary> GetInstances (SPWeb web, SPList list, SPView view) {
				Property listSetProp = null;
				IEnumerable<IDictionary> insts = Instances;
				Property.Type.ListSet.Config cfg;
				if (insts != null) {
					if ((list != null) || (view != null))
						foreach (Property p in Properties)
							if (p.PropertyType is Property.Type.ListSet) {
								listSetProp = p;
								break;
							}
					foreach (IDictionary inst in insts) {
						if ((!HasWebs) || (new Property.Type.WebSet.Config (inst ["webs"]).IsMatch (web) && ((listSetProp == null) || ((cfg = new Property.Type.ListSet.Config (inst [listSetProp.Name])).IsMatch (list, Owner) && cfg.IsMatch (view)))))
							yield return inst;
					}
				}
			}

			public void Import (string json) {
				Import (JSON.JsonDecode (json) as IDictionary);
			}

			public void Import (IDictionary imp) {
				IDictionary dict = InstDict;
				if ((imp != null) && (imp.Count > 0)) {
					foreach (DictionaryEntry e in imp)
						dict [e.Key] = e.Value;
					SaveChanges (null);
				}
			}

			public void SaveChanges (HttpContext context) {
				string tmp;
				object item = null;
				OrderedDictionary clone, dict = InstDict as OrderedDictionary, nuInst;
				if (context != null) {
					foreach (DictionaryEntry inst in dict)
						foreach (Property prop in Properties)
							prop.PropertyType.Update (inst.Value as IDictionary, prop, context);
					if ("1".Equals (context.Request.Form ["roxitemhasnew"]) && !"new".Equals (context.Request.Form ["roxitemdelete"])) {
						if (!Li2)
							throw new Exception (ProductPage.GetResource ("NopeEd", ProductPage.GetResource ("Tool_ItemEditor_Add", ProductPage.GetProductResource ("Tool_" + Name + "_TitleSingular")), "Basic"));
						nuInst = new OrderedDictionary ();
						nuInst ["id"] = ProductPage.GuidLower (Guid.NewGuid (), false);
						foreach (Property prop in Properties)
							prop.PropertyType.Update (nuInst, prop, context, prop.PropertyType.GetFormKey ("new", prop));
						dict.Insert (0, nuInst ["id"], nuInst);
					} else if (!string.IsNullOrEmpty (tmp = context.Request.Form ["roxitemdelete"])) {
						dict.Remove (tmp);
						SavedSilent = true;
					} else if (!string.IsNullOrEmpty (tmp = context.Request.Form ["roxitemmoveup"])) {
						clone = new OrderedDictionary (dict.Count);
						foreach (DictionaryEntry entry in dict)
							if ((tmp.Equals (entry.Key)) && (clone.Count > 0))
								clone.Insert (clone.Count - 1, entry.Key, entry.Value);
							else
								clone [entry.Key] = entry.Value;
						dict = clone;
						SavedSilent = true;
					} else if (!string.IsNullOrEmpty (tmp = context.Request.Form ["roxitemmovedn"])) {
						clone = new OrderedDictionary (dict.Count);
						foreach (DictionaryEntry entry in dict)
							if (tmp.Equals (entry.Key))
								item = entry.Value;
							else {
								clone [entry.Key] = entry.Value;
								if (item != null) {
									clone [tmp] = item;
									item = null;
								}
							}
						if ((item != null) && (clone.Count < dict.Count))
							clone [tmp] = item;
						dict = clone;
						SavedSilent = true;
					}
				}
				Owner.Update (this, Key, instDict = dict);
			}

			private bool Li2 {
				get {
					if ((li2 == null) || !li2.HasValue)
						li2 = ProductPage.LicEdition (ProductPage.GetContext (), Li, 2);
					return li2.Value;
				}
			}

			private bool Li4 {
				get {
					if ((li4 == null) || !li4.HasValue)
						li4 = ProductPage.LicEdition (ProductPage.GetContext (), Li, 4);
					return li4.Value;
				}
			}

			internal IDictionary InstDict {
				get {
					bool hadDef = false, loaded = false, noDef = ProductPage.Config<bool> (ProductPage.GetContext (), "NoDefaultInst");
					string tmpVal;
					IDictionary def, tmp;
					OrderedDictionary ordDict = null;
					if (instDict == null)
						if ((instDict = JSON.JsonDecode (Owner.Storage [Key] + string.Empty, typeof (OrderedDictionary)) as IDictionary) == null)
							instDict = new OrderedDictionary ();
						else
							loaded = true;
					if (Owner.SiteScope && noDef)
						instDict.Remove ("default");
					foreach (DictionaryEntry entry in instDict) {
						if ("default".Equals (entry.Key))
							hadDef = true;
						if ((tmp = entry.Value as IDictionary) != null)
							foreach (Property p in Properties) {
								if (string.IsNullOrEmpty (tmpVal = tmp [p.Name] + string.Empty) && (p.RawSchema ["default_if_empty"] is bool) && (bool) p.RawSchema ["default_if_empty"])
									tmpVal = (tmp [p.Name] = p.DefaultValue) + string.Empty;
								if ((!(Li4 || p.Le)) || p.ReadOnly)
									tmp [p.Name] = p.DefaultValue;
								else if (loaded && Property.Type.String.IsPassword (p.RawSchema) && !string.IsNullOrEmpty (tmpVal))
									try {
										tmp [p.Name] = Encoding.Unicode.GetString (ProtectedData.Unprotect (Convert.FromBase64String (tmpVal), ProductPage.Assembly.GetName ().GetPublicKeyToken (), DataProtectionScope.LocalMachine));
									} catch {
										tmp [p.Name] = string.Empty;
									}
							}
					}
					if ((!hadDef) && this ["has_default", false] && Owner.SiteScope && !noDef) {
						def = CreateDefaultInstance ();
						if ((ordDict = instDict as OrderedDictionary) != null)
							ordDict.Insert (0, "default", def);
						else
							instDict ["default"] = def;
					}
					if ((!Li2) && (instDict.Count > 1))
						if (hadDef || ((ordDict == null) && ((ordDict = instDict as OrderedDictionary) == null))) {
							ordDict = new OrderedDictionary ();
							if (hadDef)
								ordDict ["default"] = instDict ["default"];
							else
								foreach (DictionaryEntry entry in instDict) {
									ordDict [entry.Key] = entry.Value;
									break;
								}
							instDict = ordDict;
						} else
							for (int i = 1; i < ordDict.Count; i++)
								ordDict.RemoveAt (i);
					return instDict;
				}
			}

			public bool CanAddNew {
				get {
					return Li2 && Owner.ProdPage.IsApplicableAdmin;
				}
			}

			public bool HasWebs {
				get {
					return this ["has_webs", true];
				}
			}

			public int InstanceCount {
				get {
					return InstDict.Count;
				}
			}

			public IEnumerable<IDictionary> Instances {
				get {
					foreach (DictionaryEntry entry in InstDict)
						yield return entry.Value as IDictionary;
				}
			}

			public Property this [string propName] {
				get {
					return Properties.Find ((prop) => {
						return prop.Name == propName;
					});
				}
			}

			public bool this [string configKey, bool defVal] {
				get {
					object obj = null;
					IDictionary cfgDic = RawSchema ["_config"] as IDictionary;
					if ((cfgDic != null) && cfgDic.Contains (configKey))
						obj = cfgDic [configKey];
					return ((obj is bool) ? ((bool) obj) : defVal);
				}
			}

			public string this [string configKey, string defVal] {
				get {
					object obj = null;
					IDictionary cfgDic = RawSchema ["_config"] as IDictionary;
					if ((cfgDic != null) && cfgDic.Contains (configKey))
						obj = cfgDic [configKey];
					return ((obj == null) ? defVal : obj.ToString ());
				}
			}

			public string Key {
				get {
					return Owner.assemblyName + "_" + Name;
				}
			}

		}

		#endregion

		[ThreadStatic]
		internal static bool noSave = false;

		public readonly Dictionary<string, Schema> AllSchemas = new Dictionary<string, Schema> ();
		public readonly bool Elevated;
		public readonly ProductPage ProdPage;
		public readonly IDictionary RawSchema;
		public readonly string Source;

		internal string assemblyName = ProductPage.AssemblyName;

		private bool siteScope = false;
		private SPWeb web = null;
		private SPSite dispSite = null, site = null;
		private SPFarm farm = null;
		private SPWebApplication webApp = null;
		private List<SPListTemplate> templates = null;
		private Hashtable store = null;

		[ThreadStatic]
		private static ProductPage.LicInfo li = null;
		[ThreadStatic]
		private static bool? li2 = null;
		[ThreadStatic]
		private static bool? li4 = null;

		internal static bool Bool (object val, bool defVal) {
			return ((val is bool) ? (bool) val : defVal);
		}

		internal static IDictionary CloneDictionary (Schema schema, IDictionary dic, int level) {
			string val;
			IDictionary nuDic = new OrderedDictionary (), tmp;
			Property prop;
			Converter<KeyValuePair<IDictionary, JsonSchemaManager.Property>, bool> should = schema.ShouldSerialize;
			//if ((should == null) && "DataSources".Equals (schema.Name, StringComparison.InvariantCultureIgnoreCase))
			//    should = roxority.Data.DataSource.ShouldSerialize;
			foreach (DictionaryEntry entry in dic)
				if ((tmp = entry.Value as IDictionary) != null)
					nuDic [entry.Key] = CloneDictionary (schema, tmp, level + 1);
				else if ((level == 1) && ((prop = schema [entry.Key + string.Empty]) != null)) {
					if ((should == null) || should (new KeyValuePair<IDictionary, Property> (dic, prop)))
						if (Property.Type.String.IsPassword (prop.RawSchema) && !string.IsNullOrEmpty (val = entry.Value + string.Empty))
							nuDic [entry.Key] = Convert.ToBase64String (ProtectedData.Protect (Encoding.Unicode.GetBytes (val), ProductPage.Assembly.GetName ().GetPublicKeyToken (), DataProtectionScope.LocalMachine), Base64FormattingOptions.None);
						else
							nuDic [entry.Key] = entry.Value;
				} else
					nuDic [entry.Key] = entry.Value;
			return nuDic;
		}

		public static string [] DiscoverSchemaFiles (HttpContext context) {
			string [] arr = null;
			try {
				arr = Directory.GetFiles (context.Server.MapPath ("/_layouts/" + ProductPage.AssemblyName + "/"), "*.json", SearchOption.TopDirectoryOnly);
			} catch {
			}
			return ((arr == null) ? new string [0] : arr);
		}

		public static string GetDisplayName (IDictionary inst, string schemaName, bool mergeNameAndTitle) {
			string name = inst ["name"] + string.Empty, title = inst ["title"] + string.Empty, ret = ProductPage.GetResource ("Tool_ItemEditor_Untitled", ProductPage.GetProductResource ("Tool_" + schemaName + "_TitleSingular"), inst ["id"]);
			if (mergeNameAndTitle && (!string.IsNullOrEmpty (name)) && !string.IsNullOrEmpty (title))
				ret = name + " [\"" + title + "\"]";
			else if (!string.IsNullOrEmpty (title))
				ret = title;
			else if (!string.IsNullOrEmpty (name))
				ret = name;
			return ret.Contains ("$Resources:") ? (GetDisplayValue (ret) + string.Empty) : ret;
		}

		public static ICollection<IDictionary> GetInstances (string fpath, string schemaName) {
			return GetInstances (fpath, schemaName, null);
		}

		public static ICollection<IDictionary> GetInstances (string fpath, string schemaName, string asmName) {
			Dictionary<object, IDictionary> actions = new Dictionary<object, IDictionary> ();
			try {
				ProductPage.Elevate (delegate () {
					IEnumerable<IDictionary> en;
					using (ProductPage prodPage = new ProductPage ())
						if ((en = GetInstances (prodPage, fpath, schemaName, null, null, null, true, true, true, asmName)) != null)
							foreach (IDictionary inst in en)
								actions [inst ["id"]] = inst;
				}, true, true);
			} catch {
			}
			return actions.Values;
		}

		public static IEnumerable<IDictionary> GetInstances (ProductPage prodPage, string filePath, string schemaName, SPWeb web, SPList list, SPView view, bool farmScoped, bool siteScoped, bool reThrow) {
			return GetInstances (prodPage, filePath, schemaName, web, list, view, farmScoped, siteScoped, reThrow, null);
		}

		public static IEnumerable<IDictionary> GetInstances (ProductPage prodPage, string filePath, string schemaName, SPWeb web, SPList list, SPView view, bool farmScoped, bool siteScoped, bool reThrow, string asmName) {
			List<IDictionary> results = new List<IDictionary> ();
			if (farmScoped || siteScoped)
				ProductPage.Elevate (delegate () {
					bool elev = ProductPage.Elevated;
					JsonSchemaManager schemaMan = null;
					Schema schema = null;
					IEnumerable<IDictionary> insts;
					results.Clear ();
					try {
						try {
							schemaMan = new JsonSchemaManager (prodPage, filePath, !farmScoped, asmName);
							if (!schemaMan.AllSchemas.TryGetValue (schemaName, out schema))
								schemaMan = null;
						} catch (UnauthorizedAccessException) {
							if (!ProductPage.Elevated)
								throw;
						} catch {
							if (reThrow)
								throw;
						}
						if (schema != null) {
							if ((insts = schema.GetInstances (web, list, view)) != null)
								foreach (IDictionary inst in insts)
									results.Add (inst);
							if (farmScoped && siteScoped) {
								schemaMan.SiteScope = true;
								if ((insts = schema.GetInstances (web, list, view)) != null)
									foreach (IDictionary inst in insts)
										results.Add (inst);
							}
						}
					} finally {
						if (schemaMan != null)
							schemaMan.Dispose ();
					}
				}, true);
			return results;
		}

		public static object GetDisplayValue (object value) {
			int pos1, pos2;
			string v = value as string, sub, p = "$Resources:";
			if (!string.IsNullOrEmpty (v)) {
				while ((pos1 = v.IndexOf (p, StringComparison.InvariantCultureIgnoreCase)) >= 0) {
					if ((pos2 = v.IndexOf (' ', pos1 + p.Length)) < 0)
						sub = v.Substring (pos1 + p.Length);
					else
						sub = v.Substring (pos1, pos2 - p.Length - pos1);
					v = v.Replace (p + sub, ProductPage.GetProductResource (sub));
				}
				value = v;
			}
			return value;
		}

		public static JsonSchemaManager TryGet (ProductPage prodPage, string filePath, bool siteScope, string asmName) {
			KeyValuePair<JsonSchemaManager, JsonSchemaManager> kvp = TryGet (prodPage, filePath, !siteScope, siteScope, asmName);
			return siteScope ? kvp.Value : kvp.Key;
		}

		public static KeyValuePair<JsonSchemaManager, JsonSchemaManager> TryGet (ProductPage prodPage, string filePath, bool farmScope, bool siteScope, string asmName) {
			JsonSchemaManager fjs = null, sjs = null;
			if (farmScope)
				try {
					fjs = new JsonSchemaManager (prodPage, filePath, false, asmName);
				} catch {
				}
			if (siteScope)
				try {
					sjs = new JsonSchemaManager (prodPage, filePath, true, asmName);
				} catch {
				}
			return new KeyValuePair<JsonSchemaManager, JsonSchemaManager> (fjs, sjs);
		}

		internal static ProductPage.LicInfo Li {
			get {
				if (li == null)
					li = ProductPage.LicInfo.Get (null);
				return li;
			}
		}

		public JsonSchemaManager (ProductPage prodPage, string filePath, bool siteScope, string asmName) {
			Elevated = ProductPage.Elevated;
			if (!string.IsNullOrEmpty (asmName))
				assemblyName = asmName;
			ProdPage = prodPage;
			this.siteScope = siteScope;
			if (string.IsNullOrEmpty (filePath) && (prodPage != null) && (prodPage.Server != null) && !File.Exists (filePath = prodPage.Server.MapPath ("/_layouts/" + ProductPage.AssemblyName + "/schemas.json")))
				filePath = prodPage.Server.MapPath ("/_layouts/" + ProductPage.AssemblyName + "/schemas.tl.json");
			using (StreamReader sr = File.OpenText (filePath)) {
				sr.ReadLine ();
				RawSchema = JSON.JsonDecode (Source = sr.ReadToEnd (), typeof (OrderedDictionary)) as IDictionary;
				foreach (DictionaryEntry entry in RawSchema)
					AllSchemas [entry.Key + string.Empty] = new Schema (this, entry.Key + string.Empty, entry.Value as IDictionary);
			}
		}

		~JsonSchemaManager () {
			Dispose ();
		}

		internal SPListTemplate GetListTemplate (string name) {
			foreach (SPListTemplate lt in ListTemplates)
				if (lt.InternalName == name)
					return lt;
			return null;
		}

		internal void Update (Schema schema, string key, IDictionary val) {
			IDictionary dic = new OrderedDictionary ();
			try {
				Web.AllowUnsafeUpdates = Site.AllowUnsafeUpdates = true;
			} catch {
			}
			Storage [key] = JSON.JsonEncode (CloneDictionary (schema, val, 0));
			if (SiteScope)
				web.Update ();
			else
				WebApp.Update (true);
		}

		public void Dispose () {
			if (dispSite != null) {
				dispSite.Dispose ();
				dispSite = null;
			}
		}

		public SPFarm Farm {
			get {
				if (farm == null)
					farm = ProductPage.GetFarm (ProductPage.GetContext ());
				return farm;
			}
		}

		public List<SPListTemplate> ListTemplates {
			get {
				if (templates == null) {
					templates = new List<SPListTemplate> ();
					foreach (SPListTemplate lt in ProductPage.TryEach<SPListTemplate> (Web.ListTemplates))
						templates.Add (lt);
					foreach (SPListTemplate lt in ProductPage.TryEach<SPListTemplate> (Site.GetCustomListTemplates (Web)))
						templates.Add (lt);
				}
				return templates;
			}
		}

		public SPSite Site {
			get {
				if (site == null)
					site = SiteScope ? (Elevated ? (dispSite = ProductPage.OpenSite (ProductPage.GetContext ())) : ProductPage.GetSite (ProductPage.GetContext ())) : ProdPage.AdminSite;
				if (site != null)
					site.CatchAccessDeniedException = false;
				return site;
			}
		}

		public bool SiteScope {
			get {
				return siteScope;
			}
			set {
				if (value != siteScope) {
					siteScope = value;
					web = null;
					webApp = null;
					farm = null;
					store = null;
					site = null;
					foreach (KeyValuePair<string, Schema> kvp in AllSchemas)
						kvp.Value.instDict = null;
				}
			}
		}

		public Hashtable Storage {
			get {
				Guid webID;
				Guid siteID;
				if (store == null)
					try {
						store = (SiteScope ? Web.AllProperties : Site.WebApplication.Properties);
					} catch (UnauthorizedAccessException) {
						if (!ProductPage.Elevated) {
							webID = Web.ID;
							siteID = Site.ID;
							ProductPage.Elevate (delegate () {
								using (SPSite elSite = new SPSite (siteID))
								using (SPWeb elWeb = elSite.OpenWeb (webID))
									store = (SiteScope ? elWeb.AllProperties : elSite.WebApplication.Properties);
							}, false, false);
						}
					}
				return store;
			}
		}

		public SPWeb Web {
			get {
				if (web == null)
					web = Site.RootWeb;
				return web;
			}
		}

		public SPWebApplication WebApp {
			get {
				if (webApp == null)
					webApp = Site.WebApplication;
				return webApp;
			}
		}

	}

}
