
using roxority.Shared;
using roxority.SharePoint;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.OracleClient;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

using Operator = roxority.SharePoint.CamlOperator;

namespace roxority_FilterZen {

	using FilterPair = roxority_FilterWebPart.FilterPair;

	[Serializable]
	public abstract class FilterBase : ISerializable {

		#region Boolean Class

		[Serializable]
		public class Boolean : FilterBase.Interactive {

			private string boolValue = "", falseValue = "", trueValue = "1";

			public Boolean () {
				interactive = true;
				supportAllowMultiEnter = false;
				AllowMultiEnter = false;
				sendEmpty = true;
			}

			public Boolean (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				supportAllowMultiEnter = false;
				try {
					BoolValue = info.GetString ("BoolValue");
					FalseValue = info.GetString ("FalseValue");
					TrueValue = info.GetString ("TrueValue");
				} catch {
				}
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				info.AddValue ("BoolValue", BoolValue);
				info.AddValue ("FalseValue", FalseValue);
				info.AddValue ("TrueValue", TrueValue);
				base.GetObjectData (info, context);
			}

			public override void Render (HtmlTextWriter output, bool isUpperBound) {
				bool defTrue = Get<string> ("TrueValue").Equals (Get<string> ("BoolValue"));
				output.Write ("<input class=\"" + (defTrue ? "rox-check-default" : "rox-check-value") + "\" type=\"checkbox\" name=\"{0}\" id=\"{0}\"{1}{2}/><label for=\"{0}\">{3}</label>", PREFIX_FIELDNAME + ID, Equals (WebPartValue, Get<string> ("TrueValue")) ? " checked=\"checked\"" : string.Empty, HtmlOnChangeAttr.Replace ("onchange", "onclick"), Label);
				base.Render (output, isUpperBound);
			}

			public override void UpdatePanel (Panel panel) {
				if (!hiddenProperties.Contains ("DefaultIfEmpty"))
					hiddenProperties.Add ("DefaultIfEmpty");
				panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilterspecial').slideToggle();\" href=\"#noop\">" + this ["FilterProps", GetFilterTypeTitle (GetType ())] + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText;\" id=\"roxfilterspecial\" style=\"display: none;\">"));
				panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "FalseValue", Get<string> ("FalseValue")));
				panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "TrueValue", Get<string> ("TrueValue")));
				panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "BoolValue", Get<string> ("BoolValue")));
				panel.Controls.Add (new LiteralControl ("</fieldset>"));
				base.UpdatePanel (panel);
			}

			public override void UpdateProperties (Panel panel) {
				BoolValue = Get<string> ("BoolValue");
				FalseValue = Get<string> ("FalseValue");
				TrueValue = Get<string> ("TrueValue");
				base.UpdateProperties (panel);
			}

			protected internal override IEnumerable<KeyValuePair<string, string>> FilterPairs {
				get {
					yield return new KeyValuePair<string, string> (Name, WebPartValue);
				}
			}

			public string BoolValue {
				get {
					return boolValue;
				}
				set {
					boolValue = value;
				}
			}

			public string FalseValue {
				get {
					return falseValue;
				}
				set {
					falseValue = ProductPage.Trim (value);
				}
			}

			public string TrueValue {
				get {
					return trueValue;
				}
				set {
					trueValue = ProductPage.Trim (value);
				}
			}

			public override string WebPartValue {
				get {
					string filval = GetFilterValue (PREFIX_FIELDNAME + ID, "POST".Equals (HttpContext.Current.Request.HttpMethod, StringComparison.InvariantCultureIgnoreCase) ? string.Empty : Get<string> ("BoolValue"));
					return ("on".Equals (filval) ? Get<string> ("TrueValue") : ((string.IsNullOrEmpty (filval) || filval.Equals (Get<string> ("FalseValue"), StringComparison.InvariantCultureIgnoreCase)) ? Get<string> ("FalseValue") : Get<string> ("TrueValue")));
				}
			}

		}

		#endregion

		#region CamlDistinct Class

		[Serializable]
		internal class CamlDistinct : FilterBase {

			public CamlDistinct () {
			}

			public CamlDistinct (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				try {
				} catch {
				}
			}

			public override void UpdatePanel (Panel panel) {
				hiddenProperties.Add ("SendEmpty");
				panel.Controls.Add (new LiteralControl ("<div>" + this ["CamlDistinct"] + "</div>"));
				panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilterspecial').slideToggle();\" href=\"#noop\">" + this ["FilterProps", GetFilterTypeTitle (GetType ())] + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText; display: none; visibility: hidden;\" id=\"roxfilterspecial\">"));
				panel.Controls.Add (new LiteralControl ("</fieldset>"));
				base.UpdatePanel (panel);
				panel.Controls.Add (new LiteralControl ("<style type=\"text/css\"> fieldset#roxfilterspecial, fieldset#roxfilteradvanced, div.roxsectionlink { display: none; } </style>"));
			}

			protected internal override IEnumerable<KeyValuePair<string, string>> FilterPairs {
				get {
					return null;
				}
			}

		}

		#endregion

		#region CamlSource Class

		[Serializable]
		internal class CamlSource : Text {

			public CamlSource ()
				: base () {
				isCamlSource = true;
			}

			public CamlSource (SerializationInfo info, StreamingContext context)
				: base (info, context) {
					isCamlSource = true;
			}

		}

		#endregion

		#region CamlViewSwitch Class

		[Serializable]
		internal class CamlViewSwitch : Choice {

			private List<string> empty = new List<string> (), views = null;
			private string [] excludeViews = null;

			public CamlViewSwitch () {
				reqEd = 4;
				name = "_roxListView";
				label = this ["CamlViewSwitch"];
				interactive = true;
				supportAllowMultiEnter = false;
			}

			public CamlViewSwitch (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				try {
				} catch {
				}
				reqEd = 4;
				supportAllowMultiEnter = false;
			}

			public override void UpdatePanel (Panel panel) {
				hiddenProperties.AddRange (new string [] { "AllowMultiEnter", "Groups", "CamlOperator", "SuppressMode", "MultiFilterSeparator", "MultiValueSeparator", "SuppressMultiValues", "FallbackValue", "NumberFormat", "NumberCulture", "SendEmpty", "SendAllAsMultiValuesIfEmpty", "PostFilter" });
				panel.Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> roxEmpty = '" + SPEncode.ScriptEncode (this ["EmptyCur"]) + "'; </script>"));
				base.UpdatePanel (panel);
			}

			protected internal override List<string> AutoChoices {
				get {
					if ((views == null) && (parentWebPart != null) && (parentWebPart.connectedList != null)) {
						views = new List<string> ();
						foreach (SPView view in ProductPage.TryEach<SPView> (parentWebPart.connectedList.Views))
							if (!(string.IsNullOrEmpty (view.Title) || view.PersonalView || "assetLibTemp".Equals (view.Title) || Array.Exists<string> (ExcludeViews, delegate (string urlPart) {
								return (view.Url.IndexOf (urlPart, StringComparison.InvariantCultureIgnoreCase) > 0);
							})))
								views.Add (view.Url + ";#" + view.Title);
					}
					return ((views == null) ? empty : views);
				}
			}

			public override bool Cascade {
				get {
					return false;
				}
			}

			public string [] ExcludeViews {
				get {
					if (excludeViews == null)
						excludeViews = ProductPage.Config (ProductPage.GetContext (), "ExcludeViews").Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
					return excludeViews;
				}
			}

			protected internal override bool SupportsMultipleValues {
				get {
					return false;
				}
			}

		}

		#endregion

		#region Choice Class

		[Serializable]
		internal class Choice : Interactive {

			private const string SCRIPT_REPOPULATE = "repopulateList('filter_DefaultChoice', 'filter_Choices', '{0}', '" + CHOICE_EMPTY + "');repopulateList('filter_DefaultChoice2', 'filter_Choices', '{1}', '" + CHOICE_EMPTY + "');";

			private static readonly string scriptCheckDefault = SCRIPT_CHECK_DEFAULT.Replace (PLACEHOLDER_LISTID, "DefaultChoice");

			internal string [] choices = new string [0];
			internal string [] postChoices = null;

			internal bool choicesDisabled = false;

			private string defaultChoice = CHOICE_EMPTY, defaultChoice2 = CHOICE_EMPTY;

			public Choice () {
				pickerSemantics = defaultIfEmpty = supportRange = true;
			}

			public Choice (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				supportRange = pickerSemantics = true;
				try {
					DefaultChoice = info.GetString ("DefaultChoice");
					DefaultIfEmpty = info.GetBoolean ("DefaultIfEmpty");
					if (!(this is CamlViewSwitch))
						Choices = info.GetValue ("Choices", typeof (string [])) as string [];
					DefaultChoice2 = info.GetString ("DefaultChoice2");
				} catch {
				}
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				if (AutoChoices == null)
					info.AddValue ("Choices", choices, typeof (string []));
				info.AddValue ("DefaultChoice", DefaultChoice);
				info.AddValue ("DefaultChoice2", DefaultChoice2);
				base.GetObjectData (info, context);
			}

			public override void Render (HtmlTextWriter output, bool isUpperBound) {
				string options = string.Empty, choiceTitle, choiceValue, id = PREFIX_FIELDNAME + ID + (isUpperBound ? "2" : string.Empty), dcName = isUpperBound ? "DefaultChoice2" : "DefaultChoice";
				string [] choices = ((postChoices == null) ? ((List<string>) AllPickableValues).ToArray () : postChoices);
				int index = 0, pos;
				bool checkStyle = Get<bool> ("CheckStyle"), hasCondition = false, isInCondition = false;
				List<string> selIndices = GetFilterValues (id, Get<string> (dcName).ToString ()), autoChoices = AutoChoices;
				SPView view;
				if (autoChoices != null)
					choices = autoChoices.ToArray ();
				if ((selIndices.Count > 0) && (string.IsNullOrEmpty (selIndices [0]) || selIndices [0].Equals (CHOICE_EMPTY)))
					selIndices.Clear ();
				if (postChoices == null) {
					if (Get<bool> ("PostFilter"))
						postChoices = choices = PostFilterChoices (choices).Key;
					if (Cascade)
						postChoices = choices = PostFilterChoices (parentWebPart.connectedList, view = ((parentWebPart.connectedView == null) ? parentWebPart.connectedList.DefaultView : parentWebPart.connectedView), Name.StartsWith ("@") ? Name.Substring (1) : Name, choices, true).Key;
				}
				if (Get<bool> ("DefaultIfEmpty")) {
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\"> roxMultiMins['filterval_" + ID + "'] = '" + CHOICE_EMPTY + "'; </script>");
					if (!checkStyle)
						options += string.Format (FORMAT_LISTOPTION, CHOICE_EMPTY, this ["Empty" + ((this is CamlViewSwitch) ? "Cur" : (Get<bool> ("SendEmpty") ? "None" : "All"))], ((selIndices.Count == 0) || ((selIndices.Count == 1) && ((selIndices [0] == CHOICE_EMPTY) || (selIndices [0] == string.Empty)))) ? " selected=\"selected\"" : string.Empty);
					else
						options += string.Format ("<span><input class=\"rox-check-default\" name=\"" + id + "\" type=\"" + (AllowMultiEnter ? "checkbox" : "radio") + "\" id=\"empty_" + id + "\" value=\"{1}\" {3}" + (string.IsNullOrEmpty (HtmlOnChangeAttr) ? (" onclick=\"jQuery(\'.chk-" + ID + "\').attr(\'checked\', false);\"") : HtmlOnChangeAttr.Replace ("onchange=\"", "onclick=\"jQuery('.chk-" + ID + "').attr('checked', false);")) + "/><label for=\"empty_" + id + "\">{2}</label></span>", ProductPage.GuidLower (Guid.NewGuid ()), CHOICE_EMPTY, this ["Empty" + ((this is CamlViewSwitch) ? "Cur" : (Get<bool> ("SendEmpty") ? "None" : "All"))], ((selIndices.Count == 0) || ((selIndices.Count == 1) && ((selIndices [0] == CHOICE_EMPTY) || (selIndices [0] == string.Empty)))) ? " checked=\"checked\"" : string.Empty);
				}
				foreach (string c in choices) {
					index++;
					if ((pos = c.IndexOf (";#")) > 0) {
						choiceValue = c.Substring (0, pos);
						choiceTitle = c.Substring (pos + 2);
					} else
						choiceTitle = choiceValue = c;
					isInCondition = false;
					if (hasCondition = (((pos = choiceTitle.IndexOf ("[[")) > 0) && (choiceTitle.IndexOf ("]]") > (pos + 4) && (choiceTitle.IndexOf (':', pos) > (pos + 2)))))
						try {
							isInCondition = choiceTitle.Substring (choiceTitle.IndexOf (':', pos) + 1, choiceTitle.IndexOf ("]]") - choiceTitle.IndexOf (':', pos) - 1).Equals (ResolveValue (choiceTitle.Substring (pos + 2, choiceTitle.IndexOf (':', pos) - (pos + 2))));
							choiceTitle = choiceTitle.Substring (0, pos);
						} catch {
						}
					if ((!hasCondition) || isInCondition) {
						if (checkStyle)
							options += string.Format ("<span><input class=\"chk-" + ID + " rox-check-value\" name=\"" + id + "\" type=\"" + (AllowMultiEnter ? "checkbox" : "radio") + "\" id=\"x{0}\" value=\"{1}\" {3}" + ((string.IsNullOrEmpty (HtmlOnChangeAttr) && Get<bool> ("DefaultIfEmpty")) ? (" onclick=\"document.getElementById('empty_" + id + "').checked=false;\"") : HtmlOnChangeAttr.Replace ("onchange=\"", "onclick=\"" + (Get<bool> ("DefaultIfEmpty") ? ("document.getElementById('empty_" + id + "').checked=false;") : string.Empty))) + "/><label for=\"x{0}\">{2}</label></span>", ProductPage.GuidLower (Guid.NewGuid ()), c, GetDisplayValue (choiceTitle), selIndices.Contains (choiceValue) ? " checked=\"checked\"" : string.Empty);
						else
							options += string.Format (FORMAT_LISTOPTION, c, GetDisplayValue (choiceTitle), selIndices.Contains (c) ? " selected=\"selected\"" : string.Empty);
						if ((PickerLimit != 0) && (index >= PickerLimit))
							break;
					}
				}
				if (checkStyle)
					output.Write ("<div>" + options + "</div>");
				else
					output.Write ("<select" + (AllowMultiEnter ? (" size=\"1\" multiple=\"multiple\" class=\"rox-multiselect ms-input\"") : " class=\"ms-input\"") + " name=\"{0}\" id=\"{0}\"{1}>" + options + "</select>", id, AllowMultiEnter ? HtmlOnChangeMultiAttr : HtmlOnChangeAttr);
				base.Render (output, isUpperBound);
			}

			public override void UpdatePanel (Panel panel) {
				string postedChoices = Context.Request ["filter_Choices"];
				string [] choices = string.IsNullOrEmpty (postedChoices) ? Choices : postedChoices.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				List<string> autoChoices = AutoChoices;
				if (autoChoices != null)
					choices = autoChoices.ToArray ();
				if (IsRange)
					hiddenProperties.Remove ("DefaultChoice2");
				else
					hiddenProperties.Add ("DefaultChoice2");
				panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilterspecial').slideToggle();\" href=\"#noop\">" + this ["FilterProps", GetFilterTypeTitle (GetType ())] + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText;\" id=\"roxfilterspecial\" style=\"display: none;\">"));
				panel.Controls.Add (CreateControl ((autoChoices != null) ? FORMAT_TEXTAREA.Replace ("<textarea ", "<textarea disabled=\"disabled\" ") : FORMAT_TEXTAREA, "Choices", (choices.Length == 0) ? string.Empty : string.Join ("\n", choices), (choices.Length < 4) ? 4 : choices.Length, string.Format (SCRIPT_REPOPULATE, CHOICE_EMPTY, CHOICE_EMPTY)));
				panel.Controls.Add (CreateControl (FORMAT_LIST, "DefaultChoice", " onchange=\"" + scriptCheckDefault + "\"" + (Get<bool> ("AllowMultiEnter") ? (" multiple=\"multiple\" size=\"" + choices.Length + "\"") : string.Empty)));
				panel.Controls.Add (CreateControl (FORMAT_LIST, "DefaultChoice2", " onchange=\"" + scriptCheckDefault + "\"" + (Get<bool> ("AllowMultiEnter") ? (" multiple=\"multiple\" size=\"" + choices.Length + "\"") : string.Empty)));
				panel.Controls.Add (CreateScript (string.Format (SCRIPT_REPOPULATE, Get<string> ("DefaultChoice"), Get<string> ("DefaultChoice2")) + scriptCheckDefault));
				panel.Controls.Add (new LiteralControl ("</fieldset>"));
				base.UpdatePanel (panel);
			}

			public override void UpdateProperties (Panel panel) {
				if (AutoChoices == null)
					Choices = Get<string> ("Choices").Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				DefaultChoice = Get<string> ("DefaultChoice");
				DefaultChoice2 = Get<string> ("DefaultChoice2");
				base.UpdateProperties (panel);
			}

			protected internal override IEnumerable<string> AllPickableValues {
				get {
					string postedChoices = Context.Request ["filter_Choices"];
					List<string> autoChoices = AutoChoices;
					return new List<string> (string.IsNullOrEmpty (postedChoices) ? ((autoChoices == null) ? Choices : autoChoices.ToArray ()) : postedChoices.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
				}
			}

			protected internal virtual List<string> AutoChoices {
				get {
					return null;
				}
			}

			protected internal override IEnumerable<KeyValuePair<string, string>> FilterPairs {
				get {
					string [] choices = ((postChoices == null) ? ((List<string>) AllPickableValues).ToArray () : postChoices);
					int pos;
					List<string> autoChoices = AutoChoices;
					if (autoChoices != null)
						choices = autoChoices.ToArray ();
					//SPView view;
					//if (postChoices == null) {
					//    if (Get<bool> ("PostFilter"))
					//        postChoices = choices = PostFilterChoices (choices).Key;
					//    if (Cascade)
					//        postChoices = choices = PostFilterChoices (parentWebPart.connectedList, view = ((parentWebPart.connectedView == null) ? parentWebPart.connectedList.DefaultView : parentWebPart.connectedView), Name.StartsWith ("@") ? Name.Substring (1) : Name, choices, true).Key;
					//}
					foreach (string v in GetFilterValues (PREFIX_FIELDNAME + ID, Get<string> ("DefaultChoice")))
						if (string.IsNullOrEmpty (v) || v.Equals (CHOICE_EMPTY) || (Array.IndexOf<string> (choices, v) >= 0)) {
							yield return new KeyValuePair<string, string> (Name, CHOICE_EMPTY.Equals (v) ? string.Empty : (((pos = v.IndexOf (";#")) > 0) ? v.Substring (0, pos) : v));
							if (IsRange)
								break;
						}
					if (IsRange)
						foreach (string v in GetFilterValues (PREFIX_FIELDNAME + ID + 2, Get<string> ("DefaultChoice2")))
							if (string.IsNullOrEmpty (v) || v.Equals (CHOICE_EMPTY) || (Array.IndexOf<string> (choices, v) >= 0)) {
								yield return new KeyValuePair<string, string> (Name, CHOICE_EMPTY.Equals (v) ? string.Empty : (((pos = v.IndexOf (";#")) > 0) ? v.Substring (0, pos) : v));
								if (IsRange)
									break;
							}
				}
			}

			public string [] Choices {
				get {
					List<string> autoChoices = AutoChoices;
					return ((autoChoices == null) ? choices : autoChoices.ToArray ());
				}
				set {
					List<string> list = new List<string> ((value == null) ? new string [0] : value), autoChoices = AutoChoices;
					if (autoChoices == null) {
						list = list.ConvertAll<string> (delegate (string val) {
							return ProductPage.Trim (val);
						});
						while (list.IndexOf (string.Empty) >= 0)
							list.Remove (string.Empty);
						ProductPage.RemoveDuplicates<string> (list);
						choices = list.ToArray ();
					}
				}
			}

			public string DefaultChoice {
				get {
					return defaultChoice;
				}
				set {
					defaultChoice = value;
				}
			}

			public string DefaultChoice2 {
				get {
					return defaultChoice2;
				}
				set {
					defaultChoice2 = value;
				}
			}

			public override bool DefaultIfEmpty {
				get {
					string defChoice = Get<string> ("DefaultChoice");
					return (base.DefaultIfEmpty || string.IsNullOrEmpty (defChoice) || CHOICE_EMPTY.Equals (defChoice));
				}
				set {
					string defChoice = Get<string> ("DefaultChoice");
					base.DefaultIfEmpty = (value || string.IsNullOrEmpty (defChoice) || CHOICE_EMPTY.Equals (defChoice));
				}
			}

			public override string WebPartValue {
				get {
					string defaultChoice = Get<string> ("DefaultChoice"), postedChoices = Context.Request ["filter_Choices"];
					if (Array.IndexOf<string> (string.IsNullOrEmpty (postedChoices) ? Choices : postedChoices.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), defaultChoice) >= 0)
						return (CHOICE_EMPTY.Equals (defaultChoice) ? string.Empty : defaultChoice);
					return (CHOICE_EMPTY.Equals (base.WebPartValue) ? string.Empty : base.WebPartValue);
				}
			}

			public string WebPartValue2 {
				get {
					string defaultChoice = Get<string> ("DefaultChoice2"), postedChoices = Context.Request ["filter_Choices"];
					if (Array.IndexOf<string> (string.IsNullOrEmpty (postedChoices) ? Choices : postedChoices.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries), defaultChoice) >= 0)
						return (CHOICE_EMPTY.Equals (defaultChoice) ? string.Empty : defaultChoice);
					return (CHOICE_EMPTY.Equals (base.WebPartValue) ? string.Empty : base.WebPartValue);
				}
			}

		}

		#endregion

		#region Date Class

		[Serializable]
		internal sealed class Date : Interactive {

			private CultureInfo dtCulture = null;
			private long absoluteDefaultValue = 0, absoluteDefaultValue2 = 0;
			private string dateCulture = string.Empty, dateFormat = string.Empty, dateFilter = string.Empty, dateFilter2 = string.Empty;
			private int relativeOffset = 0;
			private bool relativeOffsetForDefaultOnly = true;

			public Date () {
				supportRange = true;
			}

			public Date (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				supportRange = true;
				try {
					AbsoluteDefaultValue = info.GetInt64 ("AbsoluteDefaultValue");
					RelativeOffset = info.GetInt32 ("RelativeOffset");
					DateCulture = info.GetString ("DateCulture");
					DateFormat = info.GetString ("DateFormat");
					RelativeOffsetForDefaultOnly = info.GetBoolean ("RelativeOffsetForDefaultOnly");
					dateFilter = info.GetString ("dateFilter");
					dateFilter2 = info.GetString ("dateFilter2");
					AbsoluteDefaultValue2 = info.GetInt64 ("AbsoluteDefaultValue2");
				} catch {
				}
			}

			internal string GetDateGetString (long absDefVal) {
				long diff = DateTime.MaxValue.Ticks - absDefVal;
				switch (diff) {
					case 6:
						return "nextmonth_lastday";
					case 5:
						return "nextmonth_firstday";
					case 4:
						return "thismonth_lastday";
					case 3:
						return "thismonth_firstday";
					case 2:
						return "lastmonth_lastday";
					case 1:
						return "lastmonth_firstday";
					default:
						return "today";
				}
			}

			internal string GetDateGetValue (bool is2) {
				if (Get<long> ("AbsoluteDefaultValue" + (is2 ? "2" : string.Empty)) == DateTime.MaxValue.Ticks)
					return ProductPage.ConvertDateToString (GetDateSpecialValue (0, false, false), DateFormat, EffectiveDateCulture);
				if (Get<long> ("AbsoluteDefaultValue" + (is2 ? "2" : string.Empty)) == DateTime.MaxValue.Ticks - 1)
					return ProductPage.ConvertDateToString (GetDateSpecialValue (-1, true, false), DateFormat, EffectiveDateCulture);
				if (Get<long> ("AbsoluteDefaultValue" + (is2 ? "2" : string.Empty)) == DateTime.MaxValue.Ticks - 2)
					return ProductPage.ConvertDateToString (GetDateSpecialValue (-1, false, true), DateFormat, EffectiveDateCulture);
				if (Get<long> ("AbsoluteDefaultValue" + (is2 ? "2" : string.Empty)) == DateTime.MaxValue.Ticks - 3)
					return ProductPage.ConvertDateToString (GetDateSpecialValue (0, true, false), DateFormat, EffectiveDateCulture);
				if (Get<long> ("AbsoluteDefaultValue" + (is2 ? "2" : string.Empty)) == DateTime.MaxValue.Ticks - 4)
					return ProductPage.ConvertDateToString (GetDateSpecialValue (0, false, true), DateFormat, EffectiveDateCulture);
				if (Get<long> ("AbsoluteDefaultValue" + (is2 ? "2" : string.Empty)) == DateTime.MaxValue.Ticks - 5)
					return ProductPage.ConvertDateToString (GetDateSpecialValue (1, true, false), DateFormat, EffectiveDateCulture);
				if (Get<long> ("AbsoluteDefaultValue" + (is2 ? "2" : string.Empty)) == DateTime.MaxValue.Ticks - 6)
					return ProductPage.ConvertDateToString (GetDateSpecialValue (1, false, true), DateFormat, EffectiveDateCulture);
				else if (Get<long> ("AbsoluteDefaultValue" + (is2 ? "2" : string.Empty)) == 0)
					return string.Empty;
				else if (Get<long> ("AbsoluteDefaultValue" + (is2 ? "2" : string.Empty)) == -1)
					return dateFilter;
				return is2 ? AbsoluteDateValue2 : AbsoluteDateValue;
			}

			internal long GetDateSetValue (string value) {
				DateTime dt;
				value = ProductPage.Trim (value);
				if (string.IsNullOrEmpty (value))
					return 0;
				else if (value.ToLowerInvariant ().Contains ("today"))
					return DateTime.MaxValue.Ticks;
				else if (value.ToLowerInvariant ().Contains ("lastmonth_firstday"))
					return DateTime.MaxValue.Ticks - 1;
				else if (value.ToLowerInvariant ().Contains ("lastmonth_lastday"))
					return DateTime.MaxValue.Ticks - 2;
				else if (value.ToLowerInvariant ().Contains ("thismonth_firstday"))
					return DateTime.MaxValue.Ticks - 3;
				else if (value.ToLowerInvariant ().Contains ("thismonth_lastday"))
					return DateTime.MaxValue.Ticks - 4;
				else if (value.ToLowerInvariant ().Contains ("nextmonth_firstday"))
					return DateTime.MaxValue.Ticks - 5;
				else if (value.ToLowerInvariant ().Contains ("nextmonth_lastday"))
					return DateTime.MaxValue.Ticks - 6;
				else if (value.StartsWith ("{$") && value.EndsWith ("$}")) {
					dateFilter = value;
					return -1;
				} else if ((dt = ProductPage.ConvertStringToDate (value, EffectiveDateCulture)).Equals (DateTime.MaxValue))
					return 0;
				else
					return dt.Ticks;
			}

			internal DateTime GetDateSpecialValue (int addMonths, bool first, bool last) {
				DateTime t = DateTime.Today, t2;
				if (first)
					return new DateTime ((t2 = t.AddMonths (addMonths)).Year, t2.Month, 1);
				else if (last)
					return new DateTime ((t2 = t.AddMonths (addMonths + 1)).Year, t2.Month, 1).AddDays (-1);
				return t;
			}

			internal string GetFilterValue (out bool isDefaultValue, bool isUpperBound) {
				string defVal = isUpperBound ? DefaultValue2 : DefaultValue;
				List<string> userVals = GetFilterValues ("$datePicker" + ID + (isUpperBound ? "2" : string.Empty) + "$datePicker" + ID + (isUpperBound ? "2" : string.Empty) + "Date", string.Empty);
				isDefaultValue = false;
				foreach (string k in Context.Request.Form.AllKeys)
					if (k.EndsWith ("$datePicker" + ID + (isUpperBound ? "2" : string.Empty) + "$datePicker" + ID + (isUpperBound ? "2" : string.Empty) + "Date"))
						return base.GetFilterValue (k, defVal);
				if (userVals != null)
					foreach (string val in userVals)
						if (!string.IsNullOrEmpty (val))
							return val;
				isDefaultValue = true;
				return defVal;
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				info.AddValue ("AbsoluteDefaultValue", AbsoluteDefaultValue);
				info.AddValue ("AbsoluteDefaultValue2", AbsoluteDefaultValue2);
				info.AddValue ("RelativeOffset", RelativeOffset);
				info.AddValue ("DateFormat", DateFormat);
				info.AddValue ("DateCulture", DateCulture);
				info.AddValue ("RelativeOffsetForDefaultOnly", RelativeOffsetForDefaultOnly);
				info.AddValue ("dateFilter", dateFilter);
				info.AddValue ("dateFilter2", dateFilter2);
				base.GetObjectData (info, context);
			}

			public override void Render (HtmlTextWriter output, bool isUpperBound) {
				int c = 0;
				DateTimeControl datePicker = new DateTimeControl ();
				ProductPage.InitializeDateTimePicker (datePicker);
				datePicker.AutoPostBack = (parentWebPart != null) && parentWebPart.AutoRepost;
				datePicker.ID = "datePicker" + ID + (isUpperBound ? "2" : string.Empty);
				foreach (KeyValuePair<string, string> kvp in FilterPairs) {
					if ((!string.IsNullOrEmpty (kvp.Value)) && ((!isUpperBound) || (c == 1))) {
						datePicker.SelectedDate = ProductPage.ConvertStringToDate (kvp.Value, EffectiveDateCulture);
						break;
					}
					c++;
				}
				output.Write ("<span class=\"rox-ifilter-datetime\">");
				if (parentWebPart != null)
					parentWebPart.Controls.Add (datePicker);
				datePicker.RenderControl (output);
				if (parentWebPart != null)
					parentWebPart.Controls.Remove (datePicker);
				output.Write ("</span>");
				base.Render (output, isUpperBound);
			}

			public override void UpdatePanel (Panel panel) {
				DateTimeControl datePicker = new DateTimeControl ();
				DateTimeControl datePicker2 = new DateTimeControl ();
				string postedDateTime = null, postedDateTime2 = null;
				//hiddenProperties.AddRange (new string [] { "DateCulture", "DateFormat" });
				ProductPage.InitializeDateTimePicker (datePicker);
				ProductPage.InitializeDateTimePicker (datePicker2);
				foreach (string k in Context.Request.Form.AllKeys)
					if (k.EndsWith (((parentWebPart == null) ? string.Empty : parentWebPart.ID) + "$datePicker$datePickerDate"))
						postedDateTime = Context.Request [k];
					else if (k.EndsWith (((parentWebPart == null) ? string.Empty : parentWebPart.ID) + "$datePicker2$datePicker2Date"))
						postedDateTime2 = Context.Request [k];
				datePicker.ID = "datePicker";
				datePicker2.ID = "datePicker2";
				if ((postedDateTime == null) && (AbsoluteDefaultValue > 0) && (AbsoluteDefaultValue != DateTime.MaxValue.Ticks))
					datePicker.SelectedDate = AbsoluteDate;
				if ((postedDateTime2 == null) && (AbsoluteDefaultValue2 > 0) && (AbsoluteDefaultValue2 != DateTime.MaxValue.Ticks))
					datePicker2.SelectedDate = AbsoluteDate2;
				panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilterspecial').slideToggle();\" href=\"#noop\">" + this ["FilterProps", GetFilterTypeTitle (GetType ())] + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText;\" id=\"roxfilterspecial\" style=\"display: none;\">"));
				panel.Controls.Add (CreateControl (FORMAT_GENERIC_PREFIX, "AbsoluteDateTime"));
				panel.Controls.Add (datePicker);
				if (IsRange)
					panel.Controls.Add (datePicker2);
				panel.Controls.Add (new LiteralControl (FORMAT_GENERIC_SUFFIX));
				panel.Controls.Add (new LiteralControl (string.Format ("<div id=\"div_{0}\"><div class=\"rox-prop\"><label id=\"label_{0}\" for=\"{0}\">{1}</label></div></div>", "filter_RelativeOffset", this ["Prop_RelativeOffset", string.Format ("<input style=\"width: 32px; text-align: center;\" class=\"ms-input\" type=\"text\" name=\"{0}\" id=\"{0}\" value=\"{1}\"/>", "filter_RelativeOffset", Get<int> ("RelativeOffset"))])));
				panel.Controls.Add (CreateControl (FORMAT_CHECKBOX, "RelativeOffsetForDefaultOnly", GetChecked (Get<bool> ("RelativeOffsetForDefaultOnly"))));
				if ((postedDateTime != null) || (AbsoluteDefaultValue >= (DateTime.MaxValue.Ticks - 6)) || (AbsoluteDefaultValue < 0))
					panel.Controls.Add (CreateScript ("jQuery('#" + datePicker.ClientID + "_datePickerDate').val('" + ((postedDateTime != null) ? postedDateTime : ((AbsoluteDefaultValue < 0) ? dateFilter : GetDateGetString (AbsoluteDefaultValue))) + "');"));
				if ((postedDateTime2 != null) || (AbsoluteDefaultValue2 >= (DateTime.MaxValue.Ticks - 6)) || (AbsoluteDefaultValue2 < 0))
					panel.Controls.Add (CreateScript ("jQuery('#" + datePicker2.ClientID + "_datePicker2Date').val('" + ((postedDateTime2 != null) ? postedDateTime2 : ((AbsoluteDefaultValue2 < 0) ? dateFilter2 : GetDateGetString (AbsoluteDefaultValue2))) + "');"));
				panel.Controls.Add (new LiteralControl ("<br/>"));
				panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "DateCulture", Get<string> ("DateCulture")));
				panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "DateFormat", Get<string> ("DateFormat")));
				panel.Controls.Add (new LiteralControl ("</fieldset>"));
				base.UpdatePanel (panel);
			}

			public override void UpdateProperties (Panel panel) {
				RelativeOffset = Get<int> ("RelativeOffset");
				RelativeOffsetForDefaultOnly = Get<bool> ("RelativeOffsetForDefaultOnly");
				foreach (string k in Context.Request.Form.AllKeys)
					if (k.EndsWith (((parentWebPart == null) ? string.Empty : parentWebPart.ID) + "$datePicker$datePickerDate"))
						AbsoluteDateValue = Context.Request [k];
					else if (k.EndsWith (((parentWebPart == null) ? string.Empty : parentWebPart.ID) + "$datePicker2$datePicker2Date"))
						AbsoluteDateValue2 = Context.Request [k];
				DateCulture = Get<string> ("DateCulture");
				DateFormat = Get<string> ("DateFormat");
				base.UpdateProperties (panel);
			}

			internal DateTime AbsoluteDate {
				get {
					long l = Get<long> ("AbsoluteDefaultValue");
					return ((l < 0) ? ProductPage.ConvertStringToDate (ResolveValue (DefaultValue.Substring (2, DefaultValue.Length - 4)), EffectiveDateCulture) : new DateTime (l));
				}
				set {
					AbsoluteDefaultValue = value.Ticks;
				}
			}

			internal DateTime AbsoluteDate2 {
				get {
					long l = Get<long> ("AbsoluteDefaultValue2");
					return ((l < 0) ? ProductPage.ConvertStringToDate (ResolveValue (DefaultValue2.Substring (2, DefaultValue2.Length - 4)), EffectiveDateCulture) : new DateTime (l));
				}
				set {
					AbsoluteDefaultValue2 = value.Ticks;
				}
			}

			internal string AbsoluteDateValue {
				get {
					return ProductPage.ConvertDateToString (AbsoluteDate, DateFormat, EffectiveDateCulture);
				}
				set {
					AbsoluteDefaultValue = GetDateSetValue (value);
				}
			}

			internal string AbsoluteDateValue2 {
				get {
					return ProductPage.ConvertDateToString (AbsoluteDate2, DateFormat, EffectiveDateCulture);
				}
				set {
					AbsoluteDefaultValue2=GetDateSetValue(value);
				}
			}

			internal string DefaultValue {
				get {
					return GetDateGetValue (false);
				}
			}

			internal string DefaultValue2 {
				get {
					return GetDateGetValue (true);
				}
			}

			internal CultureInfo EffectiveDateCulture {
				get {
					int i;
					string value = Get<string> ("DateCulture");
					if ((dtCulture == null) && !string.IsNullOrEmpty (value))
						try {
							if (int.TryParse (value, out i))
								dtCulture = new CultureInfo (i);
							else
								dtCulture = new CultureInfo (value);
						} catch {
							dtCulture = null;
						}
					return dtCulture;
				}
			}

			protected internal override IEnumerable<KeyValuePair<string, string>> FilterPairs {
				get {
					bool isDefault = false;
					string value = GetFilterValue (out isDefault, false);
					DateTime dtValue;
					if ((value.StartsWith ("{$") && value.EndsWith ("$}")) && ((value = ResolveValue (value.Substring (2, value.Length - 4))) == null))
						value = string.Empty;
					dtValue = ProductPage.ConvertStringToDate (value, EffectiveDateCulture);
					yield return new KeyValuePair<string, string> (Name, string.IsNullOrEmpty (value) ? value : ProductPage.ConvertDateToString (dtValue.AddDays (((!Get<bool> ("RelativeOffsetForDefaultOnly")) || isDefault) ? Get<int> ("RelativeOffset") : 0), DateFormat, EffectiveDateCulture));
					if (IsRange) {
						if (((value = GetFilterValue (out isDefault, true)).StartsWith ("{$") && value.EndsWith ("$}")) && ((value = ResolveValue (value.Substring (2, value.Length - 4))) == null))
							value = string.Empty;
						dtValue = ProductPage.ConvertStringToDate (value, EffectiveDateCulture);
						yield return new KeyValuePair<string, string> (Name, string.IsNullOrEmpty (value) ? value : ProductPage.ConvertDateToString (dtValue.AddDays (((!Get<bool> ("RelativeOffsetForDefaultOnly")) || isDefault) ? Get<int> ("RelativeOffset") : 0), DateFormat, EffectiveDateCulture));
					}
				}
			}

			public long AbsoluteDefaultValue {
				get {
					return absoluteDefaultValue;
				}
				set {
					absoluteDefaultValue = value;
				}
			}

			public long AbsoluteDefaultValue2 {
				get {
					return absoluteDefaultValue2;
				}
				set {
					absoluteDefaultValue2 = value;
				}
			}

			public string DateFormat {
				get {
					return dateFormat;
				}
				set {
					dateFormat = value;
				}
			}

			public string DateCulture {
				get {
					return dateCulture;
				}
				set {
					int i;
					dtCulture = null;
					value = ProductPage.Trim (value);
					try {
						if (int.TryParse (value, out i))
							new CultureInfo (i);
						else
							new CultureInfo (value);
						dateCulture = value;
					} catch {
						dateCulture = string.Empty;
					}
				}
			}

			public int RelativeOffset {
				get {
					return relativeOffset;
				}
				set {
					relativeOffset = value;
				}
			}

			public bool RelativeOffsetForDefaultOnly {
				get {
					return relativeOffsetForDefaultOnly;
				}
				set {
					relativeOffsetForDefaultOnly = value;
				}
			}

		}

		#endregion

		#region Favorites Class

		[Serializable]
		public class Favorites : Interactive {

			public Favorites () {
				Init ();
			}

			public Favorites (SerializationInfo info, StreamingContext ctx)
				: base (info, ctx) {
				Init ();
			}

			internal void Init () {
				supportAutoSuggest = supportRange = supportAllowMultiEnter = false;
				interactive = true;
			}

			protected internal override bool SupportsMultipleValues {
				get {
					return false;
				}
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				base.GetObjectData (info, context);
			}

			public override void Render (HtmlTextWriter output, bool isUpperBound) {
				output.Write ("<select></select>");
				base.Render (output, isUpperBound);
			}

			public override void UpdatePanel (Panel panel) {
				hiddenProperties.AddRange (new string [] { "IsInteractive", "SendEmpty", "DefaultIfEmpty" });
				base.UpdatePanel (panel);
				panel.Controls.Add (new LiteralControl ("<style type=\"text/css\"> fieldset#roxfilteradvanced, div.roxsectionlink { display: none; } </style>"));
			}

			public override void UpdateProperties (Panel panel) {
				base.UpdateProperties (panel);
			}

			public override bool IsInteractive {
				get {
					return true;
				}
				set {
					base.IsInteractive = true;
				}
			}

		}

		#endregion

		#region Interactive Class

		[Serializable]
		public abstract class Interactive : FilterBase {

			public const string CHOICE_EMPTY = "0478f8f9-fbdc-42f5-99ea-f6e8ec702606";

			protected internal const string PLACEHOLDER_LISTID = "%%PLACEHOLDER_LISTID%%";
			protected internal const string SCRIPT_CHECK_DEFAULT = "setTimeout('var roxtmp = document.getElementById(\\'filter_" + PLACEHOLDER_LISTID + "2\\'); if (document.getElementById(\\'filter_DefaultIfEmpty\\').disabled = ((document.getElementById(\\'filter_" + PLACEHOLDER_LISTID + "\\').selectedIndex == 0) || (roxtmp && (roxtmp.selectedIndex == 0)))) { document.getElementById(\\'label_filter_DefaultIfEmpty\\').style.textDecoration = \\'none\\'; document.getElementById(\\'filter_DefaultIfEmpty\\').checked = true; }', 150);";

			protected internal static readonly string [] baseViewFields = new string [] { "ID", "Title", "LinkTitle", "LinkTitleNoMenu", "FileLeafRef", "LinkFilenameNoMenu", "LinkFilename", "BaseName" };

			protected internal int reqEd = 0;
			protected internal bool defaultIfEmpty = false, doPostFilterNow = false, interactive = false, pickerSemantics = false, postFiltered = false, supportAllowMultiEnter = false, supportAutoSuggest = false, suppressInteractive = false;

			internal SPList postFilterList = null;
			internal SPView postFilterView = null;

			private int pickerLimit = -1;
			private bool allowMultiEnter = false, autoSuggest = false, postFilter = false, checkStyle = false, sendAllAsMultiValuesIfEmpty = false;
			internal string beginGroup = string.Empty, label = string.Empty, label2 = string.Empty, postFilterListViewUrl = string.Empty, postFilterFieldName = string.Empty;

			public Interactive () {
			}

			public Interactive (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				try {
					DefaultIfEmpty = info.GetBoolean ("DefaultIfEmpty");
					IsInteractive = info.GetBoolean ("IsInteractive");
					Label = info.GetString ("Label");
					PostFilter = info.GetBoolean ("PostFilter");
					PostFilterFieldName = info.GetString ("PostFilterFieldName");
					PostFilterListViewUrl = info.GetString ("PostFilterListViewUrl");
					SendAllAsMultiValuesIfEmpty = info.GetBoolean ("SendAllAsMultiValuesIfEmpty");
					AllowMultiEnter = info.GetBoolean ("AllowMultiEnter");
					CheckStyle = info.GetBoolean ("CheckStyle");
					if ((this is Text) || (this is Multi))
						AutoSuggest = info.GetBoolean ("AutoSuggest");
					Label2 = info.GetString ("Label2");
					BeginGroup = info.GetString ("BeginGroup");
				} catch {
				}
			}

			internal string CreateCamlViewFields (params string [] viewFields) {
				string queryViewFields = "";
				foreach (string viewField in viewFields)
					queryViewFields += string.Format (ProductPage.FORMAT_CAML_VIEWFIELD, viewField);
				return queryViewFields;
			}

			internal SPQuery CreateQuery (SPView view, string queryViewFields, string filterCaml) {
				SPQuery query = new SPQuery (view);
				query.AutoHyperlink = query.ExpandRecurrence = query.IndividualProperties = query.ExpandUserField = query.IncludePermissions = query.IncludeAllUserPermissions = query.IncludeAttachmentUrls = query.IncludeAttachmentVersion = query.IncludeMandatoryColumns = query.RecurrenceOrderBy = false;
				query.RowLimit = 0;
				if (!string.IsNullOrEmpty (queryViewFields))
					query.ViewFields = queryViewFields;
				if (!string.IsNullOrEmpty (filterCaml))
					query.Query = filterCaml;
				return query;
			}

			internal SPWrap<SPList> GetList (string listUrlParam, bool parentListOrNull) {
				bool isParent = (parentListOrNull || string.IsNullOrEmpty (Get<string> (listUrlParam)));
				string listUrl = isParent ? Context.Request.Url.ToString () : Get<string> (listUrlParam);
				return SPWrap<SPList>.Create (listUrl, delegate (SPWeb web) {
					List<SPList> lists = new List<SPList> ();
					string baseUrl = Context.Request.Url.ToString ().Substring (0, Context.Request.Url.ToString ().LastIndexOf ('/'));
					if (!isParent)
						return (listUrl.ToLowerInvariant ().Contains ("/_catalogs/users/") ? web.Site.GetCatalog (SPListTemplateType.UserInformation) : ProductPage.GetList (web, listUrl));
					foreach (SPList list in ProductPage.TryEach<SPList> (web.Lists))
						if (ProductPage.MergeUrlPaths (web.Url, list.DefaultViewUrl).StartsWith (baseUrl, StringComparison.InvariantCultureIgnoreCase))
							lists.Add (list);
					return ((lists.Count == 1) ? lists [0] : null);
				});
			}

			internal virtual int GetPageID (string listUrlParam) {
				SPField urlField;
				string urlVal;
				try {
					using (SPWrap<SPList> wrap = GetList (listUrlParam, true))
						if ((wrap.Value != null) && ((urlField = ProductPage.GetField (wrap.Value, "ServerUrl")) != null))
							foreach (SPListItem item in ProductPage.TryEach<SPListItem> (wrap.Value.Items))
								if ((!string.IsNullOrEmpty (urlVal = ProductPage.Trim (item [urlField.Id]))) && (Context.Request.Url.ToString ().ToLowerInvariant ().Contains (urlVal.ToLowerInvariant ())))
									return item.ID;
				} catch (Exception ex) {
					Report (ex);
				}
				return -1;
			}

			internal SPView GetView (SPWrap<SPList> wrap, string listUrl) {
				string tmp, tmp2;
				SPView view = wrap.Value.DefaultView;
				foreach (SPView v in ProductPage.TryEach<SPView> (wrap.Value.Views))
					if ((!string.IsNullOrEmpty (v.Url)) && ((tmp = ProductPage.MergeUrlPaths (wrap.Web.Url, v.Url)).Equals (tmp2 = ProductPage.MergeUrlPaths (wrap.Web.Url, listUrl.Replace ("%20", " ")), StringComparison.InvariantCultureIgnoreCase)) && (!string.IsNullOrEmpty (v.Query))) {
						view = v;
						break;
					}
				return view;
			}

			internal KeyValuePair<string [], string []> PostFilterChoices (string [] choices) {
				doPostFilterNow = false;
				return PostFilterChoices (postFilterList, postFilterView, Get<string> ("PostFilterFieldName"), choices, false);
			}

			internal KeyValuePair<string [], string []> PostFilterChoices (SPList postFilterList, SPView postFilterView, string postFilterFieldName, string [] choices, bool cascade) {
				string camlNode, outerCamlNode = string.Empty, camlAndFormat = "<And>{0}{1}</And>", camlOrFormat = "<Or>{0}{1}</Or>", priorCaml;
				bool doDispose = ((postFilterView == null) && (postFilterList == null));
				List<string> list, removed = new List<string> ();
				SPView view = null;
				SPField field = null, tmpField;
				Dictionary<string, SPField> cascadeFields = new Dictionary<string, SPField> ();
				List<string> viewFieldNames = new List<string> ();
				Dictionary<string, string> filterCamls = new Dictionary<string, string> ();
				SPListItemCollection results;
				SPWrap<SPList> wrap = null;
				try {
					if (choices != null) {
						list = new List<string> (choices);
						try {
							wrap = (doDispose ? GetList ("PostFilterListViewUrl", false) : new SPWrap<SPList> (postFilterList.ParentWeb.Site, postFilterList.ParentWeb, postFilterList, false));
							if ((wrap.Value != null) && ((field = ProductPage.GetField (wrap.Value, postFilterFieldName = (string.IsNullOrEmpty (postFilterFieldName) ? "Title" : postFilterFieldName))) != null) && ((view = ((postFilterView == null) ? GetView (wrap, Get<string> ("PostFilterListViewUrl")) : postFilterView)) != null)) {
								viewFieldNames.Add (field.InternalName);
								if (cascade) {
									foreach (KeyValuePair<string, FilterPair> kvp in parentWebPart.PartFilters)
										if ((kvp.Value.Key == Name) && parentWebPart.cascadeLtr)
											break;
										else if (kvp.Value.Key != Name)
											if ((!string.IsNullOrEmpty (kvp.Value.Value)) && ((tmpField = ProductPage.GetField (wrap.Value, kvp.Value.Key)) != null)) {
												if (!cascadeFields.ContainsKey (tmpField.InternalName))
													cascadeFields.Add (tmpField.InternalName, tmpField);
												camlNode = ProductPage.CreateSimpleCamlNode (string.Empty, "Eq", tmpField.InternalName, tmpField.TypeAsString, kvp.Value.Value);
												if (filterCamls.TryGetValue (kvp.Value.Key, out priorCaml))
													filterCamls [kvp.Value.Key] = string.Format (parentWebPart.EffectiveAndFilters.Contains (kvp.Value.Key) ? camlAndFormat : camlOrFormat, priorCaml, camlNode);
												else
													filterCamls [kvp.Value.Key] = camlNode;
											}
									foreach (KeyValuePair<string, string> kvp in filterCamls)
										if (string.IsNullOrEmpty (outerCamlNode))
											outerCamlNode = kvp.Value;
										else
											outerCamlNode = string.Format (camlAndFormat, outerCamlNode, kvp.Value);
								}
								//if (!string.IsNullOrEmpty (outerCamlNode))
								for (int i = 0; i < list.Count; i++) {
									camlNode = ProductPage.CreateSimpleCamlNode (string.Empty, "Eq", field.InternalName, field.TypeAsString, list [i]);
									if (!string.IsNullOrEmpty (outerCamlNode))
										camlNode = string.Format (camlAndFormat, outerCamlNode, camlNode);
									if (((results = wrap.Value.GetItems (CreateQuery (view, CreateCamlViewFields (viewFieldNames.ToArray ()), "<Where>" + camlNode + "</Where>"))) == null) || (results.Count == 0)) {
										removed.Add (list [i]);
										list.RemoveAt (i);
										i--;
									}
								}
							} else
								throw new Exception (this ["PostFilterFailed", (wrap.Value == null) ? Get<string> ("PostFilterListViewUrl") : wrap.Value.ToString (), (view == null) ? Get<string> ("PostFilterListViewUrl") : view.ToString (), (field == null) ? Get<string> ("PostFilterFieldName") : field.ToString ()]);
						} catch (Exception ex) {
							if ((!cascade) || (!parentWebPart.LicEd (4)))
								Report (ex);
						}
						choices = list.ToArray ();
					}
					doPostFilterNow = false;
					return new KeyValuePair<string [], string []> (choices, removed.ToArray ());
				} finally {
					if (doDispose && (wrap != null))
						((IDisposable) wrap).Dispose ();
				}
			}

			protected string GetDisplayValue (string value, string valSep, string nameSep, bool isNumeric) {
				string result = string.Empty;
				List<string> pairs = new List<string> ();
				int pos;
				if (!string.IsNullOrEmpty (valSep))
					if (string.IsNullOrEmpty (nameSep))
						return string.Join (this ["CamlOp_Or"], value.Split (new string [] { valSep }, StringSplitOptions.RemoveEmptyEntries));
					else {
						foreach (string pair in value.Split (new string [] { nameSep }, StringSplitOptions.RemoveEmptyEntries))
							if ((pos = pair.IndexOf (valSep)) > 0)
								pairs.Add (pair.Substring (0, pos) + ":\'" + pair.Substring (pos + valSep.Length) + "\'");
							else
								pairs.Add (Name + ":\'" + pair.Substring ((pos == 0) ? valSep.Length : 0) + "\'");
						return string.Join (this ["CamlOp_And"], pairs.ToArray ());
					}
				return isNumeric ? GetNumeric (value) : value;
			}

			protected string GetDisplayValue (string value) {
				return GetDisplayValue (value, Get<string> ("MultiValueSeparator"), Get<string> ("MultiFilterSeparator"), IsNumeric);
			}

			protected internal string GetFilterValue (string formKey, string defaultValue) {
				List<string> vals = GetFilterValues (formKey, defaultValue);
				return ((vals.Count == 0) ? defaultValue : vals [0]);
			}

			protected internal virtual List<string> GetFilterValues (string formKey, string defaultValue) {
				object obj;
				string [] rawVals;
				SPContext ctx = ProductPage.GetContext ();
				bool byCookies = (string.IsNullOrEmpty (ProductPage.Config (ctx, "RememberStorage")) || (ProductPage.Config (ctx, "RememberStorage") == "both") || (ProductPage.Config (ctx, "RememberStorage") == "cookies")), byWeb = (string.IsNullOrEmpty (ProductPage.Config (ctx, "RememberStorage")) || (ProductPage.Config (ctx, "RememberStorage") == "both") || (ProductPage.Config (ctx, "RememberStorage") == "spweb"));
				HttpContext context = Context;
				string cookey = "roxfz_" + (ProductPage.Config<bool> (ctx, "RememberByNameOnly") ? Name : (ID + "_" + ((parentWebPart == null) ? string.Empty : (parentWebPart.ID + "_")))), key = cookey + ((ctx.Web.CurrentUser == null) ? null : ctx.Web.CurrentUser.LoginName.ToLowerInvariant ()), json;
				List<string> vals = new List<string> ();
				Guid siteID = ctx.Site.ID, webID = ctx.Web.ID;
				if ((Array.IndexOf<string> (Context.Request.Form.AllKeys, formKey) >= 0) || ((this is Boolean) && (context != null) && (context.Request != null) && "POST".Equals (context.Request.HttpMethod, StringComparison.InvariantCultureIgnoreCase))) {
					try {
						if ((rawVals = Context.Request.Form.GetValues (formKey)) != null)
							vals.AddRange (rawVals);
						if (vals.Count == 0)
							throw new Exception ();
					} catch {
						vals.AddRange ((Context.Request.Form [formKey] + string.Empty).Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries));
					}
					if ((vals.Count == 1) && ((DefaultIfEmpty && !pickerSemantics) ? string.IsNullOrEmpty (vals [0]) : (vals [0] == null)))
						vals.Clear ();
					if ((vals.Count == 0) && (DefaultIfEmpty || pickerSemantics))
						vals.Add (defaultValue);
					if ((vals.Count > 0) && (parentWebPart != null) && parentWebPart.RememberFilterValues && (!string.IsNullOrEmpty (key)) && (byWeb || byCookies))
						try {
							json = JSON.JsonEncode (vals);
							if (byCookies && (context != null))
								context.Response.SetCookie (new HttpCookie (cookey, HttpUtility.UrlEncode (json)));
							if (byWeb)
								try {
									ProductPage.Elevate (delegate () {
										using (SPSite curSite = new SPSite (siteID))
										using (SPWeb curWeb = curSite.OpenWeb (webID)) {
											curSite.AllowUnsafeUpdates = curWeb.AllowUnsafeUpdates = true;
											curWeb.AllProperties [key] = json;
											curWeb.Update ();
										}
									}, true);
								} catch {
								}
						} catch {
						}
					return vals;
				}
				if ((parentWebPart != null) && string.IsNullOrEmpty (context.Request.Form ["roxact_" + parentWebPart.ID]) && parentWebPart.RememberFilterValues && (byCookies || byWeb)) {
					json = null;
					if ((context != null) && byCookies && (Array.IndexOf<string> (context.Request.Cookies.AllKeys, cookey) >= 0))
						json = HttpUtility.UrlDecode (context.Request.Cookies [cookey].Value);
					else if (byWeb && ctx.Web.AllProperties.ContainsKey (key))
						json = ctx.Web.AllProperties [key] + string.Empty;
					if ((!string.IsNullOrEmpty (json)) && ((obj = JSON.JsonDecode (json)) != null)) {
						vals.Clear ();
						if (obj is ArrayList)
							foreach (object o in ((ArrayList) obj))
								vals.Add (o + string.Empty);
						else
							vals.Add (obj + string.Empty);
					}
				}
				if (vals.Count == 0)
					if ((this is Choice) && AllowMultiEnter && !string.IsNullOrEmpty (defaultValue))
						vals.AddRange (defaultValue.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries));
					else
						vals.Add (defaultValue);
				return vals;
			}

			public string GetNumeric (string value) {
				long l;
				decimal d;
				string format = Get<string> ("NumberFormat");
				bool noCulture = (EffectiveNumberCulture == null);
				try {
					if (noCulture ? long.TryParse (value, out l) : long.TryParse (value, NumberStyles.Any, EffectiveNumberCulture, out l))
						return (noCulture ? l.ToString (format) : l.ToString (format, EffectiveNumberCulture));
					if (noCulture ? decimal.TryParse (value, out d) : decimal.TryParse (value, NumberStyles.Any, EffectiveNumberCulture, out d))
						return (noCulture ? d.ToString (format) : d.ToString (format, EffectiveNumberCulture));
				} catch (Exception ex) {
					if (parentWebPart != null)
						parentWebPart.additionalWarningsErrors.Add (ex.Message);
				}
				return value;
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				info.AddValue ("DefaultIfEmpty", DefaultIfEmpty);
				info.AddValue ("IsInteractive", IsInteractive);
				info.AddValue ("Label", Label);
				info.AddValue ("Label2", Label2);
				info.AddValue ("PostFilter", PostFilter);
				info.AddValue ("CheckStyle", CheckStyle);
				info.AddValue ("PostFilterFieldName", PostFilterFieldName);
				info.AddValue ("PostFilterListViewUrl", PostFilterListViewUrl);
				info.AddValue ("SendAllAsMultiValuesIfEmpty", SendAllAsMultiValuesIfEmpty);
				info.AddValue ("AllowMultiEnter", AllowMultiEnter);
				if (supportAutoSuggest)
					info.AddValue ("AutoSuggest", AutoSuggest);
				info.AddValue ("BeginGroup", BeginGroup);
				base.GetObjectData (info, context);
			}

			public virtual void Render (HtmlTextWriter output, bool isUpperBound) {
			}

			public override void UpdatePanel (Panel panel) {
				string formatDisabledTextBox = FORMAT_TEXTBOX.Replace ("<input ", "<input disabled=\"disabled\" "), formatDisabledList = FORMAT_LIST.Replace ("<select ", "<select disabled=\"disabled\" "), formatDisabledCheckBox = FORMAT_CHECKBOX.Replace ("<input ", "<input disabled=\"disabled\" "), formatDisabledTextArea = FORMAT_TEXTAREA.Replace ("<textarea ", "<textarea disabled=\"disabled\" ");
				if (suppressInteractive)
					hiddenProperties.Add ("Label");
				else
					hiddenProperties.Remove ("Label");
				if (IsRange && !suppressInteractive)
					hiddenProperties.Remove ("Label2");
				else
					hiddenProperties.Add ("Label2");
				if ((parentWebPart != null) && pickerSemantics && !(this is CamlViewSwitch)) {
					if (suppressInteractive)
						hiddenProperties.Add ("AllowMultiEnter");
					else
						hiddenProperties.Remove ("AllowMultiEnter");
					hiddenProperties.Remove ("SendAllAsMultiValuesIfEmpty");
				} else {
					if (suppressInteractive)
						hiddenProperties.Remove ("AllowMultiEnter");
					else if (!(this is FilterBase.Multi))
						hiddenProperties.Add ("AllowMultiEnter");
					hiddenProperties.Add ("SendAllAsMultiValuesIfEmpty");
				}
				if ((this is Boolean) || (this is Date))
					hiddenProperties.AddRange (new string [] { "NumberCulture", "NumberFormat" });
				if (parentWebPart != null) {
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (reqEd) ? FORMAT_CHECKBOX : formatDisabledCheckBox, suppressInteractive || hiddenProperties.Contains ("IsInteractive"), new object [] { "filter_IsInteractive", this ["Prop_IsInteractive" + (pickerSemantics ? "Picker" : "")], GetChecked (Get<bool> ("IsInteractive")), "jQuery('#roxboxia').css({'display':(this.checked?'block':'none')});" + (pickerSemantics ? "jQuery('#div_filter_PostFilter').css({'display':(this.checked?'block':'none')});jQuery('#roxboxpostfilter').css({'display':(this.checked&&document.getElementById('filter_PostFilter').checked?'block':'none')});" : string.Empty) }));
					panel.Controls.Add (new LiteralControl ("<fieldset id=\"roxboxia\" style=\"padding: 4px; background-color: InfoBackground; color: InfoText;\">"));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (reqEd) ? FORMAT_TEXTBOX : formatDisabledTextBox, "Label", Get<string> ("Label")));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (reqEd) ? FORMAT_TEXTBOX : formatDisabledTextBox, "Label2", Get<string> ("Label2")));
					if (supportAutoSuggest)
						panel.Controls.Add (CreateControl (parentWebPart.LicEd (4) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "AutoSuggest", GetChecked (Get<bool> ("AutoSuggest"))));
					panel.Controls.Add (CreateControl (FORMAT_CHECKBOX, suppressInteractive || hiddenProperties.Contains ("DefaultIfEmpty"), new object [] { "filter_DefaultIfEmpty", this ["Prop_" + (pickerSemantics ? "AllowEmpty" : "DefaultIfEmpty")], GetChecked (Get<bool> ("DefaultIfEmpty")) }));
					panel.Controls.Add (CreateControl ((parentWebPart.LicEd (reqEd) && !IsRange) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "AllowMultiEnter", GetChecked (Get<bool> ("AllowMultiEnter") && !IsRange), string.Empty));
					if (pickerSemantics && !suppressInteractive) {
						panel.Controls.Add (CreateControl (parentWebPart.LicEd (reqEd) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "PostFilter", GetChecked (Get<bool> ("PostFilter")), "document.getElementById('roxboxpostfilter').style.display=(this.checked?'block':'none');"));
						panel.Controls.Add (new LiteralControl ("<fieldset id=\"roxboxpostfilter\" style=\"padding: 4px; background-color: ButtonFace; color: ButtonText;\">"));
						panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "PostFilterListViewUrl", Get<string> ("PostFilterListViewUrl")));
						panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "PostFilterFieldName", Get<string> ("PostFilterFieldName")));
						panel.Controls.Add (new LiteralControl ("</fieldset>"));
						panel.Controls.Add (CreateControl ((parentWebPart.LicEd (reqEd) && !IsRange) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "CheckStyle", GetChecked (Get<bool> ("CheckStyle") && !IsRange), string.Empty));
					}
					panel.Controls.Add (CreateControl ((parentWebPart.LicEd (reqEd) && !IsRange) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "SendAllAsMultiValuesIfEmpty", GetChecked (Get<bool> ("SendAllAsMultiValuesIfEmpty") && !IsRange), string.Empty));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (reqEd) ? FORMAT_TEXTBOX : formatDisabledTextBox, "BeginGroup", Get<string> ("BeginGroup")));
					panel.Controls.Add (new LiteralControl ("</fieldset>"));
					if (!Get<bool> ("IsInteractive"))
						panel.Controls.Add (CreateScript ((pickerSemantics ? "document.getElementById('roxboxpostfilter').style.display = " : string.Empty) + "document.getElementById('roxboxia').style.display = 'none';"));
					if (pickerSemantics && !Get<bool> ("PostFilter"))
						panel.Controls.Add (CreateScript ("document.getElementById('roxboxpostfilter').style.display = 'none';"));
				}
				base.UpdatePanel (panel);
			}

			public override void UpdateProperties (Panel panel) {
				DefaultIfEmpty = Get<bool> ("DefaultIfEmpty");
				IsInteractive = Get<bool> ("IsInteractive");
				Label = Get<string> ("Label");
				Label2 = Get<string> ("Label2");
				CheckStyle = Get<bool> ("CheckStyle");
				PostFilter = Get<bool> ("PostFilter");
				PostFilterListViewUrl = Get<string> ("PostFilterListViewUrl");
				PostFilterFieldName = Get<string> ("PostFilterFieldName");
				SendAllAsMultiValuesIfEmpty = Get<bool> ("SendAllAsMultiValuesIfEmpty");
				AllowMultiEnter = Get<bool> ("AllowMultiEnter");
				if (supportAutoSuggest)
					AutoSuggest = Get<bool> ("AutoSuggest");
				BeginGroup = Get<string> ("BeginGroup");
				base.UpdateProperties (panel);
			}

			protected internal virtual IEnumerable<string> AllPickableValues {
				get {
					return null;
				}
			}

			protected internal string HtmlOnChangeAttr {
				get {
					return ((parentWebPart != null) && parentWebPart.AutoRepost) ? " onchange=\"roxRefreshFilters('" + parentWebPart.ID + "');\"" : string.Empty;
				}
			}

			protected internal string HtmlOnChangeMultiAttr {
				get {
					return " onchange=\"roxMultiSelect(this);" + (((parentWebPart != null) && parentWebPart.AutoRepost) ? "roxRefreshFilters('" + parentWebPart.ID + "');" : string.Empty) + "\"";
				}
			}

			public bool AllowMultiEnter {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) && allowMultiEnter && !IsRange;
				}
				set {
					allowMultiEnter = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) && value && !IsRange;
				}
			}

			public bool AutoSuggest {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (4)) && autoSuggest;
				}
				set {
					autoSuggest = ((parentWebPart == null) || parentWebPart.LicEd (4)) && value;
				}
			}

			public string BeginGroup {
				get {
					return beginGroup;
				}
				set {
					beginGroup = (value + string.Empty).Trim ();
				}
			}

			public virtual bool Cascade {
				get {
					return (pickerSemantics && (parentWebPart != null) && parentWebPart.Cascaded && parentWebPart.LicEd (4) && parentWebPart.CamlFilters && (parentWebPart.connectedList != null));
				}
			}

			public bool CheckStyle {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) && checkStyle;
				}
				set {
					checkStyle = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) && value;
				}
			}

			public virtual bool DefaultIfEmpty {
				get {
					return defaultIfEmpty;
				}
				set {
					defaultIfEmpty = value;
				}
			}

			public virtual bool IsInteractive {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) && interactive && !suppressInteractive;
				}
				set {
					interactive = suppressInteractive ? false : (((parentWebPart == null) || parentWebPart.LicEd (reqEd)) && value);
				}
			}

			public bool IsNumeric {
				get {
					return (!string.IsNullOrEmpty (Get<string> ("NumberFormat").Trim ()));
				}
			}

			public bool IsSet {
				get {
					List<string> vals = GetFilterValues (PREFIX_FIELDNAME + ID, string.Empty);
					return ((vals != null) && (vals.Count > 0) && (!string.IsNullOrEmpty (vals [0])) && (!CHOICE_EMPTY.Equals (vals [0], StringComparison.InvariantCultureIgnoreCase)) && ((!"0".Equals (vals [0])) || !(this is Lookup)));
				}
			}

			public virtual string Label {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? label : string.Empty;
				}
				set {
					label = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

			public virtual string Label2 {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? label2 : string.Empty;
				}
				set {
					label2 = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

			public int PickerLimit {
				get {
					if ((pickerLimit < 0) && (!int.TryParse (ProductPage.Config (null, "PickerLimit"), out pickerLimit)))
						pickerLimit = 150;
					return pickerLimit;
				}
			}

			public bool PostFilter {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) && postFilter;
				}
				set {
					postFilter = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) && value;
				}
			}

			public string PostFilterListViewUrl {
				get {
					return postFilterListViewUrl;
				}
				set {
					postFilterListViewUrl = ProductPage.Trim (value);
				}
			}

			public string PostFilterFieldName {
				get {
					return postFilterFieldName;
				}
				set {
					postFilterFieldName = ProductPage.Trim (value);
				}
			}

			public bool SendAllAsMultiValuesIfEmpty {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) && sendAllAsMultiValuesIfEmpty && !IsRange;
				}
				set {
					sendAllAsMultiValuesIfEmpty = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) && value && !IsRange;
				}
			}

		}

		#endregion

		#region Lookup Class

		[Serializable]
		internal class Lookup : Interactive {

			#region ItemSort Enumeration

			public enum ItemSort {

				None = 0,
				TitleAlphaAsc = 1,
				TitleAlphaDesc = 2,
				TitleCountAsc = 3,
				TitleCountDesc = 4,
				ValueAlphaAsc = 5,
				ValueAlphaDesc = 6,
				ValueCountAsc = 7,
				ValueCountDesc = 8

			}

			#endregion

			private static readonly string scriptCheckDefault = SCRIPT_CHECK_DEFAULT.Replace (PLACEHOLDER_LISTID, "ItemID");

			protected internal bool fallbackTitles = false, stripID = true;
			protected internal int itemID = 0;

			private string displayFieldName = string.Empty, filterCaml = string.Empty, valueFieldName = string.Empty, listUrl = string.Empty;
			private int itemSorting = 0;
			private bool removeDuplicateTitles = false, removeDuplicateValues = true;
			private List<KeyValuePair<string, string []>> items = null;

			public Lookup () {
				pickerSemantics = true;
				defaultIfEmpty = true;
			}

			public Lookup (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				pickerSemantics = true;
				try {
					DisplayFieldName = info.GetString ("DisplayFieldName");
					FilterCaml = info.GetString ("FilterCaml");
					ItemID = info.GetInt32 ("ItemID");
					ItemSorting = info.GetInt32 ("ItemSorting");
					ListUrl = info.GetString ("ListUrl");
					RemoveDuplicateTitles = info.GetBoolean ("RemoveDuplicateTitles");
					RemoveDuplicateValues = info.GetBoolean ("RemoveDuplicateValues");
					StripID = info.GetBoolean ("StripID");
					ValueFieldName = info.GetString ("ValueFieldName");
					DefaultIfEmpty = info.GetBoolean ("DefaultIfEmpty");
				} catch {
				}
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				info.AddValue ("DisplayFieldName", DisplayFieldName);
				info.AddValue ("FilterCaml", FilterCaml);
				info.AddValue ("ItemID", ItemID);
				info.AddValue ("ItemSorting", ItemSorting);
				info.AddValue ("ListUrl", ListUrl);
				info.AddValue ("RemoveDuplicateTitles", RemoveDuplicateTitles);
				info.AddValue ("RemoveDuplicateValues", RemoveDuplicateValues);
				info.AddValue ("StripID", StripID);
				info.AddValue ("ValueFieldName", ValueFieldName);
				base.GetObjectData (info, context);
			}

			public override void Render (HtmlTextWriter output, bool isUpperBound) {
				List<string> selIndices = GetFilterValues (PREFIX_FIELDNAME + ID, Get<int> ("ItemID").ToString ());
				string checkStart=string.Empty, options = "", nameSep = Get<string> ("MultiFilterSeparator"), valSep = Get<string> ("MultiValueSeparator"), dispVal;
				int index = 0, pos;
				bool hasSel = false, thisSel, isNumeric = IsNumeric, checkStyle = Get<bool> ("CheckStyle");
				if (selIndices.Contains ("0"))
					selIndices.Clear ();
				if (!Le (2, true)) {
					output.WriteLine (ProductPage.GetResource ("NopeEd", GetFilterTypeTitle (GetType ()), "Basic"));
					base.Render (output, isUpperBound);
					return;
				}
				if ((this is User) && (selIndices.Count == 1) && selIndices.Contains ("-1"))
					checkStart = GetPageID (string.Empty) + ";#";
				try {
					if (items == null) {
						if (!postFiltered)
							doPostFilterNow = postFiltered = true;
						Items.ToString ();
					} else if (Cascade && !postFiltered) {
						postFiltered = doPostFilterNow = true;
						Items.ToString ();
					}
					foreach (KeyValuePair<string, string []> kvp in items) {
						dispVal = GetDisplayValue (kvp.Value [0], valSep, nameSep, isNumeric).Replace (" ", "&nbsp;");
						if (checkStyle)
							options += string.Format ("<span><input class=\"chk-" + ID + " rox-check-value\" name=\"" + PREFIX_FIELDNAME + ID + "\" type=\"" + (AllowMultiEnter ? "checkbox" : "radio") + "\" id=\"x{0}\" value=\"{1}\" {3}" + ((string.IsNullOrEmpty (HtmlOnChangeAttr) && Get<bool> ("DefaultIfEmpty")) ? (" onclick=\"document.getElementById(\'empty_" + PREFIX_FIELDNAME + ID + "\').checked=false;\"") : HtmlOnChangeAttr.Replace ("onchange=\"", "onclick=\"" + (Get<bool> ("DefaultIfEmpty") ? ("document.getElementById('empty_" + PREFIX_FIELDNAME + ID + "').checked=false;") : string.Empty))) + "/><label for=\"x{0}\">{2}</label></span>", ProductPage.GuidLower (Guid.NewGuid ()), kvp.Key, dispVal, (thisSel = (selIndices.Contains (kvp.Key) || (((pos = kvp.Key.IndexOf (";#")) > 0) && selIndices.Contains (kvp.Key.Substring (0, pos))) || (selIndices.Contains ("-2") && (index == 0)) || (selIndices.Contains ("-3") && (index == (items.Count - 1))))) ? " checked=\"checked\"" : string.Empty);
						else
							options += string.Format (FORMAT_LISTOPTION, HttpUtility.HtmlAttributeEncode (kvp.Key), dispVal, (thisSel = (((!string.IsNullOrEmpty (checkStart)) && kvp.Key.StartsWith (checkStart, StringComparison.InvariantCultureIgnoreCase)) || selIndices.Contains (kvp.Key) || (((pos = kvp.Key.IndexOf (";#")) > 0) && selIndices.Contains (kvp.Key.Substring (0, pos))) || (selIndices.Contains ("-2") && (index == 0)) || (selIndices.Contains ("-3") && (index == (items.Count - 1))))) ? " selected=\"selected\"" : string.Empty);
						hasSel |= thisSel;
						if ((PickerLimit != 0) && (index >= PickerLimit))
							break;
						index++;
					}
					if (Get<bool> ("DefaultIfEmpty")) {
						output.Write ("<script type=\"text/javascript\" language=\"JavaScript\"> roxMultiMins['filterval_" + ID + "'] = '0'; </script>");
						if (!checkStyle)
							options = string.Format (FORMAT_LISTOPTION, "0", this ["Empty" + (Get<bool> ("SendEmpty") ? "None" : "All")], ((selIndices.Count == 0) || selIndices.Contains ("0")) ? " selected=\"selected\"" : string.Empty) + options;
						else
							options = string.Format ("<span><input class=\"rox-check-default\" name=\"" + PREFIX_FIELDNAME + ID + "\" type=\"" + (AllowMultiEnter ? "checkbox" : "radio") + "\" id=\"empty_" + PREFIX_FIELDNAME + ID + "\" value=\"{1}\" {3}" + (string.IsNullOrEmpty (HtmlOnChangeAttr) ? (" onclick=\"jQuery(\'.chk-" + ID + "\').attr(\'checked\', false);\"") : HtmlOnChangeAttr.Replace ("onchange=\"", "onclick=\"jQuery('.chk-" + ID + "').attr('checked', false);")) + "/><label for=\"empty_" + PREFIX_FIELDNAME + ID + "\">{2}</label></span>", ProductPage.GuidLower (Guid.NewGuid ()), 0, this ["Empty" + (Get<bool> ("SendEmpty") ? "None" : "All")], ((selIndices.Count == 0) || selIndices.Contains ("0")) ? " checked=\"checked\"" : string.Empty) + options;
					}
					if (options.Length > 0)
						if (checkStyle)
							output.Write ("<div>" + options + "</div>");
						else
							output.Write ("<select" + (AllowMultiEnter ? (" size=\"1\" multiple=\"multiple\" class=\"rox-multiselect ms-input\"") : " class=\"ms-input\"") + " name=\"{0}\" id=\"{0}\"{1}>" + options + "</select>", PREFIX_FIELDNAME + ID, AllowMultiEnter ? HtmlOnChangeMultiAttr : HtmlOnChangeAttr);
				} catch (Exception ex) {
					Report (ex);
				}
				base.Render (output, isUpperBound);
			}

			public override void UpdatePanel (Panel panel) {
				int itemID = Get<int> ("ItemID"), theID;
				string options = string.Format (FORMAT_LISTOPTION, "0", this ["Empty"], (itemID == 0) ? " selected=\"selected\"" : string.Empty) + string.Format (FORMAT_LISTOPTION, "-2", this ["ItemIDFirst"], (itemID == -2) ? " selected=\"selected\"" : string.Empty), formatDisabledTextBox = FORMAT_TEXTBOX.Replace ("<input ", "<input disabled=\"disabled\" "), formatDisabledList = FORMAT_LIST.Replace ("<select ", "<select disabled=\"disabled\" "), formatDisabledCheckBox = FORMAT_CHECKBOX.Replace ("<input ", "<input disabled=\"disabled\" "), formatDisabledTextArea = FORMAT_TEXTAREA.Replace ("<textarea ", "<textarea disabled=\"disabled\" ");
				LiteralControl lc;
				if (parentWebPart != null) {
					try {
						using (SPWrap<SPList> wrap = GetList ("ListUrl", true))
							if ((wrap.Value != null) && (string.IsNullOrEmpty (Get<string> ("ListUrl")) || Get<string> ("ListUrl").Equals (wrap.Value.DefaultViewUrl, StringComparison.InvariantCultureIgnoreCase)))
								options = options + string.Format (FORMAT_LISTOPTION, "-1", this ["ItemID"], (itemID == -1) ? " selected=\"selected\"" : string.Empty);
							else
								using (SPWrap<SPList> wrap2 = GetList ("ListUrl", false))
									if ((wrap2.Value != null) && (wrap2.Value.DefaultViewUrl.ToLowerInvariant ().Contains ("_catalogs/users/")))
										options = options + string.Format (FORMAT_LISTOPTION, "-1", this ["ItemUser"], (itemID == -1) ? " selected=\"selected\"" : string.Empty);
					} catch (Exception ex) {
						Report (ex);
					}
					(lc = CreateControl (parentWebPart.LicEd (2) ? FORMAT_TEXTBOX : formatDisabledTextBox, "ListUrl", Get<string> ("ListUrl"))).Text = lc.Text.Replace ("100%", "294px");
					panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilterspecial').slideToggle();\" href=\"#noop\">" + this ["FilterProps", GetFilterTypeTitle (GetType ())] + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText;\" id=\"roxfilterspecial\" style=\"display: none;\">"));
					panel.Controls.Add (lc);
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_TEXTBOX : formatDisabledTextBox, "ValueFieldName", Get<string> ("ValueFieldName")));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_TEXTBOX : formatDisabledTextBox, "DisplayFieldName", Get<string> ("DisplayFieldName")));
					try {
						foreach (KeyValuePair<string, string> kvp in Items)
							options += string.Format (FORMAT_LISTOPTION, kvp.Key, kvp.Value, ((int.TryParse (kvp.Key, out theID) || (kvp.Key.Contains (";#") && int.TryParse (kvp.Key.Substring (0, kvp.Key.IndexOf (";#")), out theID))) && (itemID == theID)) ? " selected=\"selected\"" : string.Empty);
					} catch (Exception ex) {
						Report (ex);
					}
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_LIST : formatDisabledList, "ItemID", " onchange=\"" + scriptCheckDefault + "\"", options + string.Format (FORMAT_LISTOPTION, "-3", this ["ItemIDLast"], (itemID == -3) ? " selected=\"selected\"" : string.Empty)));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "StripID", GetChecked (Get<bool> ("StripID"))));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "RemoveDuplicateValues", GetChecked (Get<bool> ("RemoveDuplicateValues"))));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "RemoveDuplicateTitles", GetChecked (Get<bool> ("RemoveDuplicateTitles"))));
					options = "";
					for (int i = 0; i < 9; i++)
						options += string.Format (FORMAT_LISTOPTION, i, this ["ItemSort_" + i], (i == Get<int> ("ItemSorting")) ? " selected=\"selected\"" : string.Empty);
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_LIST : formatDisabledList, "ItemSorting", "", options));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_TEXTAREA : formatDisabledTextArea, "FilterCaml", Get<string> ("FilterCaml")));
					panel.Controls.Add (CreateScript (scriptCheckDefault));
					panel.Controls.Add (new LiteralControl ("</fieldset>"));
				}
				base.UpdatePanel (panel);
			}

			public override void UpdateProperties (Panel panel) {
				FilterCaml = Get<string> ("FilterCaml");
				ListUrl = Get<string> ("ListUrl");
				ValueFieldName = Get<string> ("ValueFieldName");
				DisplayFieldName = Get<string> ("DisplayFieldName");
				ItemID = Get<int> ("ItemID");
				RemoveDuplicateTitles = Get<bool> ("RemoveDuplicateTitles");
				RemoveDuplicateValues = Get<bool> ("RemoveDuplicateValues");
				ItemSorting = Get<int> ("ItemSorting");
				StripID = Get<bool> ("StripID");
				base.UpdateProperties (panel);
			}

			internal IEnumerable<KeyValuePair<string, string>> Items {
				get {
					bool isDateColumn, isTaxDispColumn = false, isTaxValColumn = false;
					string valDelim = null, indHint = string.Empty, dispDelim = null, valColName, dispColName, displayTitle, itemValue, itemSort, queryViewFields = "", listUrl = string.IsNullOrEmpty (Get<string> ("ListUrl")) ? Context.Request.Url.ToString () : Get<string> ("ListUrl"), finalTitle, finalVal;
					string [] splits, tmpArr;
					int sortIndex, pos, restartIndex, nextIndex, tmpIndex;
					decimal dec;
					bool sortDesc, sortCount, stripID = Get<bool> ("StripID"), removeDuplicateTitles = Get<bool> ("RemoveDuplicateTitles"), removeDuplicateValues = Get<bool> ("RemoveDuplicateValues"), isLookup = false, isCalc = false;
					object ival;
					SPList list = null;
					SPListItemCollection listItems;
					SPView view = null;
					SPQuery query = null;
					SPUser user = null;
					SPField displayColumn = null, valueColumn = null;
					SPFieldCalculated calcField;
					Dictionary<string, int> [] countDicts = new Dictionary<string, int> [] { new Dictionary<string, int> (), new Dictionary<string, int> () };
					ItemSort itemSorting = (ItemSort) Get<int> ("ItemSorting");
					KeyValuePair<string [], string []> postFiltered;
					List<string> viewFields = new List<string> (baseViewFields), baseVals = new List<string> (), itemVals = (removeDuplicateValues ? new List<string> () : null), itemTitles = (removeDuplicateTitles ? new List<string> () : null);
					SPWrap<SPList> wrap1 = null, wrap2 = null;
					if (Name == "Company")
						new object ();
					try {
						if (items == null) {
							items = new List<KeyValuePair<string, string []>> ();
							if (parentWebPart.CamlFilters && (CamlOperator == ((int) (Operator.Contains)))) {
								dispDelim = valDelim = Get<string> ("MultiValueSeparator");
								indHint = ProductPage.Config (ProductPage.GetContext (), "IndentHint");
							}
							try {
								wrap1 = GetList ("ListUrl", false);
								if ((list = wrap1.Value) != null) {
#if !SP12
									if (list.HasExternalDataSource)
										viewFields.Clear ();
#endif
									view = GetView (wrap1, listUrl);
									if (string.IsNullOrEmpty (valColName = Get<string> ("ValueFieldName")))
										valColName = "Title";
									else if ((pos = (valColName).IndexOf ("::", StringComparison.InvariantCultureIgnoreCase)) > 0) {
										valDelim = valColName.Substring (pos + 2);
										valColName = valColName.Substring (0, pos);
									}
									if ((!string.IsNullOrEmpty (dispColName = Get<string> ("DisplayFieldName"))) && ((pos = dispColName.IndexOf ("::", StringComparison.InvariantCultureIgnoreCase)) > 0)) {
										dispDelim = dispColName.Substring (pos + 2);
										dispColName = dispColName.Substring (0, pos);
									}
									if ((valueColumn = ProductPage.GetField (list, valColName)) != null) {
										if (!viewFields.Contains (valueColumn.InternalName))
											viewFields.Add (valueColumn.InternalName);
										if (isTaxValColumn = ((valueColumn.TypeAsString == "TaxonomyFieldType") || (valueColumn.TypeAsString == "TaxonomyFieldTypeMulti")))
											valDelim = ";";
									}
									if ((displayColumn = string.IsNullOrEmpty (dispColName) ? valueColumn : ProductPage.GetField (list, dispColName)) != null) {
										if (!viewFields.Contains (displayColumn.InternalName))
											viewFields.Add (displayColumn.InternalName);
										if (isTaxDispColumn = ((displayColumn.TypeAsString == "TaxonomyFieldType") || (displayColumn.TypeAsString == "TaxonomyFieldTypeMulti")))
											dispDelim = ";";
									}
									//foreach (string fn in new string [] { dispColName, valColName })
									//    if ((!string.IsNullOrEmpty (fn)) && (!viewFields.Contains (fn)))
									//        viewFields.Add (fn);
									queryViewFields = CreateCamlViewFields (viewFields.ToArray ());
									query = CreateQuery (view, queryViewFields, Get<string> ("FilterCaml"));
									query.ViewAttributes = "Scope=\"RecursiveAll\"";
								}
							} catch (Exception ex) {
								Report (ex);
							}
							if ((list != null) && (displayColumn != null) && (valueColumn != null) && (query != null)) {
								isDateColumn = ((valueColumn.Type == SPFieldType.DateTime) || ((valueColumn.FieldTypeDefinition != null) && (valueColumn.FieldTypeDefinition.BaseRenderingTypeName == "DateTime")));
								if ((((calcField = valueColumn as SPFieldCalculated) != null) || ((calcField = displayColumn as SPFieldCalculated) != null)) && (calcField.OutputType == SPFieldType.DateTime))
									isDateColumn = true;
								isLookup = (((valueColumn.Type == SPFieldType.Lookup) || ((valueColumn.FieldTypeDefinition != null) && (valueColumn.FieldTypeDefinition.BaseRenderingTypeName == "Lookup"))) || ((valueColumn.Type == SPFieldType.User) || ((valueColumn.FieldTypeDefinition != null) && (valueColumn.FieldTypeDefinition.BaseRenderingTypeName == "User"))));
								isCalc = ((valueColumn.InternalName != "GroupLink") && ((valueColumn.Type == SPFieldType.Computed) || (valueColumn.Type == SPFieldType.Calculated)));
								if ((PickerLimit > 0) && (!(Cascade || doPostFilterNow)))
									query.RowLimit = (uint) PickerLimit;
								foreach (SPListItem item in list.GetItems (query)) {
									ival = ProductPage.GetFieldVal (item, displayColumn, false);
									if (string.IsNullOrEmpty (displayTitle = ProductPage.Trim (ival)) && fallbackTitles && string.IsNullOrEmpty (displayTitle = item.Title) && string.IsNullOrEmpty (displayTitle = item.Name))
										displayTitle = "[" + item.ID + "]";
									if ((this is User) && (valueColumn.InternalName == "GroupLink")) {
										displayTitle = itemValue = string.Empty;
										user = null;
										try {
											user = list.ParentWeb.Users [item ["Name"] + string.Empty];
										} catch {
											user = list.ParentWeb.SiteUsers [item ["Name"] + string.Empty];
										}
										if (user != null)
											foreach (SPGroup group in user.Groups) {
												itemValue += (";#" + group.ID + ";#" + group.Name);
												displayTitle += (", " + group.Name);
											}
										if (itemValue.StartsWith (";#"))
											itemValue = itemValue.Substring (2);
										if (displayTitle.StartsWith (", "))
											displayTitle = displayTitle.Substring (2);
									} else
										itemValue = ProductPage.GetFieldVal (item, valueColumn, false) + string.Empty;
									if (!string.IsNullOrEmpty (itemValue)) {
										baseVals.Clear ();
										if (((pos = itemValue.IndexOf (";#", StringComparison.InvariantCultureIgnoreCase)) > 0) && isCalc)
											itemValue = itemValue.Substring (pos + 2);
										if (itemValue.IndexOf (";#") >= 0) {
											if (itemValue.IndexOf (";#") != itemValue.LastIndexOf (";#")) {
												splits = itemValue.Split (new string [] { ";#" }, StringSplitOptions.None);
												for (int i = ((isLookup || (user != null)) ? 1 : 0); i < splits.Length; i = i + ((isLookup || (user != null)) ? 2 : 1))
													baseVals.Add ((isLookup || (user != null)) ? (splits [i - 1] + ";#" + splits [i]) : splits [i]);
											} else {
												if (isLookup || (user != null) || isCalc)
													baseVals.Add (itemValue);
												else
													baseVals.AddRange (itemValue.Split (new string [] { ";#" }, StringSplitOptions.RemoveEmptyEntries));
											}
										} else if ((!string.IsNullOrEmpty (valDelim)) && (itemValue.IndexOf (valDelim) >= 0)) {
											foreach (string spl in itemValue.Split (new string [] { valDelim }, StringSplitOptions.RemoveEmptyEntries))
												if (!baseVals.Contains (spl.Trim ()))
													baseVals.Add (spl.Trim ());
										}
										if (baseVals.Count > 1)
											displayTitle = null;
										while (baseVals.Contains (string.Empty))
											baseVals.Remove (string.Empty);
										if (baseVals.Count == 0)
											baseVals.Add (itemValue);
										if (isTaxValColumn)
											for (int i = 0; i < baseVals.Count; i++)
												if ((pos = baseVals [i].IndexOf ('|')) > 0)
													baseVals [i] = baseVals [i].Substring (0, pos);
										foreach (string bv in baseVals) {
											if ((displayColumn is SPFieldCalculated) && ((pos = displayTitle.IndexOf (";#", StringComparison.InvariantCultureIgnoreCase)) > 0))
												displayTitle = displayTitle.Substring (pos + 2);
											if (isTaxDispColumn && (displayTitle != null) && ((pos = displayTitle.IndexOf ('|')) > 0))
												displayTitle = displayTitle.Substring (0, pos);
											if ((!stripID) && (displayTitle == bv) && (bv.Contains (";#")) && ((splits = bv.Split (new string [] { ";#" }, StringSplitOptions.RemoveEmptyEntries)) != null) && (splits.Length >= 1))
												foreach (string val in splits) {
													finalTitle = ProductPage.ConvertDateNoTimeIf (val, false, isDateColumn);
													if ((displayColumn is SPFieldNumber) && ((SPFieldNumber) displayColumn).ShowAsPercentage && decimal.TryParse (finalTitle, out dec) && (dec <= 1))
														finalTitle = (dec * 100) + "%";
													finalVal = ProductPage.ConvertDateNoTimeIf (val, isDateColumn, isDateColumn);
													if (((itemTitles == null) || !itemTitles.Contains (finalTitle)) && ((itemVals == null) || !itemVals.Contains (finalVal))) {
														if ((itemTitles != null) && !itemTitles.Contains (finalTitle))
															itemTitles.Add (finalTitle);
														if ((itemVals != null) && !itemVals.Contains (finalVal))
															itemVals.Add (finalVal);
														items.Add (new KeyValuePair<string, string []> (item.ID + ";#" + finalVal, new string [] { finalTitle, finalVal }));
													}
												}
											else {
												finalTitle = ProductPage.ConvertDateNoTimeIf (ProductPage.StripID ((displayTitle == null) ? bv : displayTitle), isDateColumn, isDateColumn);
												if ((displayColumn is SPFieldNumber) && ((SPFieldNumber) displayColumn).ShowAsPercentage && decimal.TryParse (finalTitle, out dec) && (dec <= 1))
													finalTitle = ((int) (dec * 100)) + "%";
												finalVal = ProductPage.ConvertDateNoTimeIf (stripID ? ProductPage.StripID (bv) : bv, isDateColumn, isDateColumn);
												if (((itemTitles == null) || !itemTitles.Contains (finalTitle)) && ((itemVals == null) || !itemVals.Contains (finalVal))) {
													if ((itemTitles != null) && !itemTitles.Contains (finalTitle))
														itemTitles.Add (finalTitle);
													if ((itemVals != null) && !itemVals.Contains (finalVal))
														itemVals.Add (finalVal);
													items.Add (new KeyValuePair<string, string []> (item.ID + ";#" + finalVal, new string [] { finalTitle, finalVal }));
												}
											}
										}
										if ((PickerLimit > 0) && (itemSorting == ItemSort.None) && (items.Count >= PickerLimit) && !(removeDuplicateTitles || removeDuplicateValues))
											break;
									}
								}
							} else
								throw new Exception (this ["LookupFailed", (list == null) ? Get<string> ("ListUrl") : list.ToString (), (view == null) ? Get<string> ("ListUrl") : view.ToString (), (valueColumn == null) ? Get<string> ("ValueFieldName") : valueColumn.ToString (), (displayColumn == null) ? Get<string> ("DisplayFieldName") : displayColumn.ToString ()]);
							if (!Get<bool> ("DefaultIfEmpty"))
								items.RemoveAll (delegate (KeyValuePair<string, string []> kvp) {
									return string.IsNullOrEmpty (kvp.Value [1]);
								});
							if (itemSorting != ItemSort.None) {
								itemSort = itemSorting.ToString ();
								sortIndex = (itemSort.StartsWith ("Value") ? 1 : 0);
								sortDesc = itemSort.EndsWith ("Desc");
								sortCount = itemSort.Contains ("Count");
								items.Sort (delegate (KeyValuePair<string, string []> one, KeyValuePair<string, string []> two) {
									int result = 0;
									KeyValuePair<string, string []> one2 = one;
									if (sortDesc) {
										one = two;
										two = one2;
									}
									if (sortCount)
										result = countDicts [sortIndex] [one.Value [sortIndex]].CompareTo (countDicts [sortIndex] [two.Value [sortIndex]]);
									if (result == 0)
										result = one.Value [sortIndex].CompareTo (two.Value [sortIndex]);
									return ((result == 0) ? one.Key.CompareTo (two.Key) : result);
								});
							}
							if ((!string.IsNullOrEmpty (indHint)) && (items.TrueForAll ((kvp) => {
								return ((kvp.Value.Length > 0) && ((kvp.Value.Length < 2) || (string.Empty + kvp.Value [0]).Equals (string.Empty + kvp.Value [1])));
							}))) {
								nextIndex = restartIndex = items.Count;
								string tmpKey, subKey;
								for (int i = 0; i < restartIndex; i++) {
									subKey = (((tmpIndex = items [i].Key.IndexOf (";#")) > 0) ? items [i].Key.Substring (0, tmpIndex) : "0") + ";#";
									if ((tmpArr = items [i].Value [0].Split (new string [] { indHint }, StringSplitOptions.RemoveEmptyEntries)) != null)
										for (int t = 0; t < tmpArr.Length; t++) {
											tmpKey = string.Join (indHint, tmpArr, 0, t + 1);
											if (items.FindIndex (restartIndex, (kvp) => {
												string [] a = tmpArr;
												string key = kvp.Key;
												int p = key.IndexOf (";#");
												if (p > 0)
													key = key.Substring (p + 2);
												return key.Trim ().Equals (tmpKey.Trim ());
											}) < restartIndex)
												items.Insert (nextIndex++, new KeyValuePair<string, string []> (subKey + tmpKey, new string [] { new string (' ', t * 3) + tmpArr [t].Trim (), tmpKey }));
										}
									//if ((tmpArr = items [i].Value [0].Split (new string [] { indHint }, StringSplitOptions.RemoveEmptyEntries)) != null)
									//    for (int t = 0; t < tmpArr.Length; t++) {
									//        tmpStr = (tmpArr [t] + string.Empty).Trim ();
									//        if ((tmpIndex = items.FindIndex ((kvp) => {
									//            if (tmpStr == "Jaguar")
									//                tmpArr.ToString ();
									//            return tmpStr.Equals ((string.Empty + kvp.Value [0]).Trim ());
									//        })) >= 0) {
									//            nextIndex = tmpIndex + 1;
									//        } else {
									//            if (t == 0)
									//                nextIndex = items.Count;
									//            items.Insert (nextIndex, new KeyValuePair<string, string []> (items [i].Key.Contains (";#") ? (items [i].Key.Substring (0, items [i].Key.IndexOf (";#", StringComparison.InvariantCultureIgnoreCase) + 2) + tmpStr) : items [i].Key, new string [] { new string (' ', t * 3) + tmpStr, tmpStr }));
									//            nextIndex++;
									//        }
									//    }
								}
								for (int ii = 0; ii < restartIndex; ii++)
									items.RemoveAt (0);
							}
							if (Get<bool> ("PostFilter")) {
								postFiltered = PostFilterChoices (items.ConvertAll<string> (delegate (KeyValuePair<string, string []> kvp) {
									return kvp.Value [1];
								}).ToArray ());
								items.RemoveAll (delegate (KeyValuePair<string, string []> kvp) {
									return (Array.IndexOf<string> (postFiltered.Value, kvp.Value [1]) >= 0);
								});
							}
							if (Cascade && doPostFilterNow) {
								postFiltered = PostFilterChoices (list, view /*= ((parentWebPart.connectedView == null) ? parentWebPart.connectedList.DefaultView : parentWebPart.connectedView)*/, Name.StartsWith ("@") ? Name.Substring (1) : Name, items.ConvertAll<string> (delegate (KeyValuePair<string, string []> kvp) {
									return kvp.Value [1];
								}).ToArray (), true);
								items.RemoveAll (delegate (KeyValuePair<string, string []> kvp) {
									return (Array.IndexOf<string> (postFiltered.Value, kvp.Value [1]) >= 0);
								});
							}
						} else if (doPostFilterNow) {
							if ((list == null) || (view == null))
								wrap2 = GetList ("ListUrl", false);
							if ((list = wrap2.Value) != null)
								view = GetView (wrap2, listUrl);
							if ((list != null) && (view != null)) {
								postFiltered = PostFilterChoices (list, view /* = ((parentWebPart.connectedView == null) ? parentWebPart.connectedList.DefaultView : parentWebPart.connectedView)*/, Name.StartsWith ("@") ? Name.Substring (1) : Name, items.ConvertAll<string> (delegate (KeyValuePair<string, string []> kvp) {
									return kvp.Value [1];
								}).ToArray (), true);
								items.RemoveAll (delegate (KeyValuePair<string, string []> kvp) {
									return (Array.IndexOf<string> (postFiltered.Value, kvp.Value [1]) >= 0);
								});
							}
						}
						return items.ConvertAll<KeyValuePair<string, string>> (delegate (KeyValuePair<string, string []> kvp) {
							return new KeyValuePair<string, string> (kvp.Key, kvp.Value [0]);
						});
					} finally {
						if (wrap1 != null)
							((IDisposable) wrap1).Dispose ();
						if (wrap2 != null)
							((IDisposable) wrap2).Dispose ();
					}
				}
			}

			protected internal override IEnumerable<string> AllPickableValues {
				get {
					if (items == null)
						Items.ToString ();
					foreach (KeyValuePair<string, string []> item in items)
						yield return item.Value [1];
				}
			}

			protected internal override IEnumerable<KeyValuePair<string, string>> FilterPairs {
				get {
					int itemID = Get<int> ("ItemID");
					bool noYield = false;
					SPUser user = null;
					List<string> selItems = GetFilterValues (PREFIX_FIELDNAME + ID, itemID.ToString ());
					string filterValue = string.Empty;
					Converter<string, string> findValueById = delegate (string id) {
						KeyValuePair<string, string []> match = items.Find (delegate (KeyValuePair<string, string []> value) {
							int pos = value.Key.IndexOf (";#");
							return ((id == value.Key) || ((pos > 0) && id.Equals (value.Key.Substring (0, pos))));
						});
						return (((match.Value != null) && (match.Value.Length >= 2)) ? match.Value [1] : string.Empty);
					};
					if (!Le (2, true))
						throw new Exception (ProductPage.GetResource ("NopeEd", GetFilterTypeTitle (GetType ()), "Basic"));
					try {
						if (items == null)
							Items.ToString ();
					} catch (Exception ex) {
						Report (ex);
					}
					foreach (string id in selItems)
						if (int.TryParse (id, out itemID) || (((itemID = id.IndexOf (";#")) > 0) && int.TryParse (id.Substring (0, itemID), out itemID))) {
							try {
								if ((itemID > 0) || ((itemID == -1) && (!(this is User)) && ((itemID = GetPageID ("ListUrl")) > 0)))
									filterValue = findValueById (id);
							} catch (Exception ex) {
								Report (ex);
							}
							if ((items != null) && (items.Count > 0))
								if ((this is User) && (itemID == -1))
									try {
										user = SPContext.Current.Web.CurrentUser;
									} catch {
									}
							if (user != null)
								if (noYield = (valueFieldName == "GroupLink"))
									foreach (SPGroup group in SPContext.Current.Web.CurrentUser.Groups)
										yield return new KeyValuePair<string, string> (Name, Get<bool> ("StripID") ? group.Name : (group.ID + ";#" + group.Name));
								else
									filterValue = user.Name;
							if (itemID == -2)
								filterValue = items [0].Value [1];
							else if (itemID == -3)
								filterValue = items [items.Count - 1].Value [1];
							else if ((itemID == 0) && ((itemID = id.IndexOf (";#")) > 0))
								filterValue = id.Substring (itemID + 2);
							if (!noYield)
								yield return new KeyValuePair<string, string> (Name, filterValue);
						}
				}
			}

			public override bool Cascade {
				get {
					return (pickerSemantics && (parentWebPart != null) && parentWebPart.Cascaded && parentWebPart.LicEd (4) && parentWebPart.CamlFilters);
				}
			}

			public override bool DefaultIfEmpty {
				get {
					return (base.DefaultIfEmpty || (Get<int> ("ItemID") == 0));
				}
				set {
					base.DefaultIfEmpty = (value || (Get<int> ("ItemID") == 0));
				}
			}

			public string DisplayFieldName {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (2)) ? displayFieldName : string.Empty;
				}
				set {
					displayFieldName = ((parentWebPart == null) || parentWebPart.LicEd (2)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

			public string FilterCaml {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (2)) ? filterCaml : string.Empty;
				}
				set {
					filterCaml = ((parentWebPart == null) || parentWebPart.LicEd (2)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

			public int ItemID {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (2)) ? itemID : 0;
				}
				set {
					itemID = ((parentWebPart == null) || parentWebPart.LicEd (2)) ? ((value < -3) ? 0 : value) : 0;
				}
			}

			public int ItemSorting {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (2)) ? itemSorting : 0;
				}
				set {
					itemSorting = ((parentWebPart == null) || parentWebPart.LicEd (2)) ? value : 0;
				}
			}

			public virtual string ListUrl {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (2)) ? listUrl : string.Empty;
				}
				set {
					listUrl = ((parentWebPart == null) || parentWebPart.LicEd (2)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

			public bool RemoveDuplicateTitles {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (2)) ? removeDuplicateTitles : false;
				}
				set {
					removeDuplicateTitles = ((parentWebPart == null) || parentWebPart.LicEd (2)) ? value : false;
				}
			}

			public bool RemoveDuplicateValues {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (2)) ? removeDuplicateValues : true;
				}
				set {
					removeDuplicateValues = ((parentWebPart == null) || parentWebPart.LicEd (2)) ? value : true;
				}
			}

			public bool StripID {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (2)) ? stripID : false;
				}
				set {
					stripID = ((parentWebPart == null) || parentWebPart.LicEd (2)) ? value : false;
				}
			}

			public string ValueFieldName {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (2)) ? valueFieldName : string.Empty;
				}
				set {
					valueFieldName = ((parentWebPart == null) || parentWebPart.LicEd (2)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

		}

		#endregion

		#region Multi Class

		[Serializable]
		internal class Multi : FilterBase.Interactive {

			#region FieldCat Enumeration

			internal enum FieldCat {

				None = 0,
				Custom = 16,
				ListInherited = 4,	//	related
				ListOther = 2,	//	custom list fields
				ListSystem = 8,	//	all other
				ListView = 1,
				All = Custom | ListInherited | ListOther | ListSystem | ListView

			}

			#endregion

			#region FieldDesc Class

			internal class FieldDesc {

				public string Name = string.Empty;
				public string Title = string.Empty;
				public SPField SPField = null;
				public FieldCat Cat = FieldCat.Custom;

				public static int Compare (FieldDesc one, FieldDesc two) {
					if (one.Cat != two.Cat)
						return ((int) one.Cat).CompareTo ((int) two.Cat);
					if ((one.Title == "Title") && (two.Title != "Title"))
						return "a".CompareTo ("b");
					else if ((two.Title == "Title") && (one.Title != "Title"))
						return "b".CompareTo ("a");
					return (one.Title.Equals (two.Title) ? one.Name.CompareTo (two.Name) : one.Title.CompareTo (two.Title));
				}

			}

			#endregion

			internal static readonly string [] blockedFields = { "Attachments", "ContentTypeId", "Edit", "SelectTitle", "PermMask", "UniqueId", "ProgId", "ScopeId", "_EditMenuTableStart", "_EditMenuTableEnd", "LinkFilenameNoMenu", "LinkFilename", "ServerUrl", "EncodedAbsUrl", "BaseName", "FolderChildCount", "ItemChildCount", "Last_x0020_Modified", "Created_x0020_Date", "_UIVersionString", "WorkflowInstanceID", "WorkflowVersion", "_UIVersion", "SortBehavior", "MetaInfo", "owshiddenversion", "Order", "_Level", "FSObjType", "_IsCurrentVersion", "InstanceID", "HTML_x0020_File_x0020_Type", "_HasCopyDestinations", "GUID", "File_x0020_Type", "_CopySource", "SyncClientId", "ContentType", "_ModerationStatus", "DocIcon" },
				titleFields = { "Title", "LinkTitle", "LinkTitleNoMenu", "LinkTitle2", "LinkTitleNoMenu2" };

			internal static readonly string [] boolOpTypes = { "AllDayEvent", "Boolean" },
					numOpTypes = { "Counter", "Currency", "DateTime", "Integer", "MaxItems", "Number", "ThreadIndex" },
					strOpTypes = { "Computed", "Note", "Text", "Choice", "Lookup", "URL", "MultiChoice", "GridChoice", "ModStat" },
					userOpTypes = { "User" };

			internal static readonly Operator [] allOperators = { Operator.Eq, Operator.Neq, Operator.BeginsWith, Operator.Contains, Operator.Gt, Operator.Geq, Operator.Lt, Operator.Leq },
				anyOperators = { Operator.Eq, Operator.Neq },
				numberOperators = { Operator.Eq, Operator.Neq, Operator.Gt, Operator.Geq, Operator.Lt, Operator.Leq },
				stringOperators = { Operator.Eq, Operator.Neq, Operator.BeginsWith, Operator.Contains },
				userOperators = { Operator.Me, Operator.NotMe, Operator.Eq, Operator.Neq };

			private string [] customFieldNames = new string [0], listFieldNames = new string [0];
			private bool allowAllInheritedFields = true, allowAllListFields = true, allowAllOtherFields = false, allowAllViewFields = true, allowAnyField = false, allowAnyAllOps = false, anyIsAll = false, groupFields = true, indent = true;
			private List<FieldDesc> fields = null;
			private List<FilterPair> fps = null;
			private bool? hasPeop = null;

			public Multi () {
				supportAllowMultiEnter = supportAutoSuggest = requirePostLoadRendering = true;
				AllowMultiEnter = AutoSuggest = true;
			}

			public Multi (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				try {
					customFieldNames = info.GetValue ("CustomFieldNames", typeof (string [])) as string [];
					listFieldNames = info.GetValue ("ListFieldNames", typeof (string [])) as string [];
					AllowAllInheritedFields = info.GetBoolean ("AllowAllInheritedFields");
					AllowAllListFields = info.GetBoolean ("AllowAllListFields");
					AllowAllOtherFields = info.GetBoolean ("AllowAllOtherFields");
					AllowAllViewFields = info.GetBoolean ("AllowAllViewFields");
					AllowAnyField = info.GetBoolean ("AllowAnyField");
					GroupFields = info.GetBoolean ("GroupFields");
					AllowAnyAllOps = info.GetBoolean ("AllowAnyAllOps");
					Indent = info.GetBoolean ("Indent");
					AnyIsAll = info.GetBoolean ("AnyIsAll");
				} catch {
				}
				supportAllowMultiEnter = supportAutoSuggest = requirePostLoadRendering = true;
			}

			internal Operator GetDefaultOperator (string fieldType, bool safe, Operator defOp) {
				Operator [] ops = GetOperators (fieldType, safe);
				Operator op = ((Array.IndexOf<Operator> (ops, defOp) >= 0) ? defOp : ops [0]);
				return ((op == Operator.Me) || (op == Operator.NotMe)) ? defOp : op;
			}

			internal IEnumerable<FieldDesc> GetFields (FieldCat match) {
				string [] pair;
				bool hasTitle = false, hasPeop = HasPeop (), isTitle;
				List<string> names;
				SPField spf;
				FieldDesc field;
				if ((fields == null) && (parentWebPart != null)) {
					fields = new List<FieldDesc> ();
					names = new List<string> ();
					if (parentWebPart.connectedList != null) {
						if (parentWebPart.connectedView != null)
							foreach (string viewField in parentWebPart.connectedView.ViewFields)
								if (((!(isTitle = IsTitle (viewField))) || !hasTitle) && (!names.Contains (viewField.ToLowerInvariant ())) && IsAllowed (viewField)) {
									field = new FieldDesc () {
										Name = viewField, Title = viewField, Cat = FieldCat.ListView
									};
									if ((spf = ProductPage.GetField (parentWebPart.connectedList, viewField)) != null) {
										field.SPField = spf;
										field.Title = spf.Title;
									}
									if ((spf == null) || (spf.Type != SPFieldType.Computed)) {
										if (isTitle)
											hasTitle = true;
										names.Add (viewField.ToLowerInvariant ());
										fields.Add (field);
									}
								}
						foreach (SPField spField in ProductPage.TryEach<SPField> (parentWebPart.connectedList.Fields))
							if (((!(isTitle = IsTitle (spField.InternalName))) || !hasTitle) && (!names.Contains (spField.InternalName.ToLowerInvariant ())) && IsAllowed (spField.InternalName) && (spField.Type != SPFieldType.Computed)) {
								if (isTitle)
									hasTitle = true;
								names.Add (spField.InternalName.ToLowerInvariant ());
								fields.Add (field = new FieldDesc () {
									Cat = (spField.FromBaseType ? FieldCat.ListSystem : ((spField.UsedInWebContentTypes || (spField.SourceId == "http://schemas.microsoft.com/sharepoint/v3")) ? FieldCat.ListInherited : FieldCat.ListOther)), SPField = spField, Name = spField.InternalName, Title = spField.Title
								});
							}
					}
					foreach (KeyValuePair<string, string> kvp in parentWebPart.validFilterNames)
						if (((!hasPeop) || (kvp.Key != "Title")) && ((!(isTitle = IsTitle (kvp.Key))) || !hasTitle) && (!names.Contains (kvp.Key.ToLowerInvariant ())) && IsAllowed (kvp.Key) && (((spf = (((parentWebPart == null) || (parentWebPart.connectedList == null)) ? null : ProductPage.GetField (parentWebPart.connectedList, kvp.Key))) == null) || (spf.Type != SPFieldType.Computed))) {
							if (isTitle)
								hasTitle = true;
							names.Add (kvp.Key.ToLowerInvariant ());
							fields.Add (field = new FieldDesc () {
								Cat = FieldCat.ListOther, Name = kvp.Key, Title = (string.IsNullOrEmpty (kvp.Value) ? kvp.Key : kvp.Value)
							});
						}
					foreach (string cfn in CustomFieldNames)
						if ((!string.IsNullOrEmpty (cfn)) && ((pair = cfn.Split (new string [] { ";#" }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pair.Length > 0) && ((!(isTitle = IsTitle (pair [0]))) || !hasTitle) && (!names.Contains (pair [0].ToLowerInvariant ())) && IsAllowed (pair [0]) && (((spf = (((parentWebPart == null) || (parentWebPart.connectedList == null)) ? null : ProductPage.GetField (parentWebPart.connectedList, pair [0]))) == null) || (spf.Type != SPFieldType.Computed))) {
							if (isTitle)
								hasTitle = true;
							names.Add (pair [0].ToLowerInvariant ());
							fields.Add (field = new FieldDesc () {
								Cat = FieldCat.Custom, Name = pair [0], Title = pair [(pair.Length > 1) ? 1 : 0]
							});
						}
					if (fields.Count == 0)
						fields = null;
					else
						fields.Sort (FieldDesc.Compare);
				}
				return (((fields == null) || (match == FieldCat.All)) ? fields : fields.FindAll (delegate (FieldDesc fd) {
					return ((Array.IndexOf<string> (CustomFieldNames, fd.Name) >= 0) || (Array.IndexOf<string> (ListFieldNames, fd.Name) >= 0) || ((match != FieldCat.None) && (fd.Cat != FieldCat.None) && ((fd.Cat & match) == fd.Cat)) || Array.Exists<string> (CustomFieldNames, delegate (string cfn) {
						return cfn.StartsWith (fd.Name + ";#");
					}));
				}));
			}

			internal string GetJson () {
				int fc, c = 0;
				IEnumerable<KeyValuePair<string, string>> pairs;
				StringBuilder buf = new StringBuilder ();
				//	{ "AND": [ "f1", { "OR": [ "f2", { "AND": [ "f3", { "OR": [ "f4", "f5" ] } ] } ] } ] }
				if ((fps == null) && ((pairs = FilterPairs) != null))
					foreach (KeyValuePair<string, string> kvp in pairs)
						break;
				if ((fps != null) && ((fc = fps.Count) > 0)) {
					if (fc > 1) {
						c++;
						buf.Append ("{\"" + (fps [0].nextAnd ? "AND" : "OR") + "\":[");
					}
					for (int i = 0; i < fc; i++) {
						buf.Append (" \"" + fps [i].Key + "\" ");
						if (i < (fc - 1))
							buf.Append (",");
						if (i < (fc - 2)) {
							c++;
							buf.Append ("{\"" + (fps [i + 1].nextAnd ? "AND" : "OR") + "\":[");
						}
					}
					for (int i = 0; i < c; i++)
						buf.Append ("]}");
				}
				return buf.ToString ();
			}

			internal Operator GetOperator (string name, Operator defVal) {
				IEnumerable<KeyValuePair<string, string>> pairs;
				IEnumerable<FieldDesc> fields = this.GetFields (FieldCat.All);
				FieldDesc fd = null;
				if (fields != null)
					foreach (FieldDesc fdesc in fields)
						if ((fdesc.Name == name) && ((fd == null) || (fd.SPField == null)))
							fd = fdesc;
				if ((fps == null) && ((pairs = FilterPairs) != null))
					foreach (KeyValuePair<string, string> kvp in pairs)
						break;
				if (fps != null)
					foreach (FilterPair fp in fps)
						if (fp.Key == name)
							return (((fd == null) || (fd.SPField == null)) ? fp.CamlOperator : GetDefaultOperator (fd.SPField.TypeAsString, !AllowAnyAllOps, fp.CamlOperator));
				return defVal;
			}

			internal string GetOperatorOptions (Operator [] ops) {
				string options = string.Empty;
				foreach (Operator op in ops)
					options += ("<option value=\"" + op + "\">" + HttpUtility.HtmlEncode (this ["CamlOp_" + op]) + "</option>");
				return options;
			}

			internal string GetOperators (string fieldType) {
				Operator [] ops = GetOperators (fieldType, !AllowAnyAllOps);
				if (ops == numberOperators)
					return "roxMultiOpsNum";
				if (ops == stringOperators)
					return "roxMultiOpsStr";
				if (ops == userOperators)
					return "roxMultiOpsUser";
				if (ops == allOperators)
					return "roxMultiOpsAll";
				if (ops == anyOperators)
					return "roxMultiOpsAny";
				return "roxMultiOpsNone";
			}

			internal Operator [] GetOperators (string fieldType, bool safe) {
				if (Array.IndexOf<string> (numOpTypes, fieldType) >= 0)
					return numberOperators;
				if (Array.IndexOf<string> (strOpTypes, fieldType) >= 0)
					return stringOperators;
				if (Array.IndexOf<string> (userOpTypes, fieldType) >= 0)
					return userOperators;
				return ((safe || (Array.IndexOf<string> (boolOpTypes, fieldType) >= 0)) ? anyOperators : allOperators);
			}

			internal string GetFieldOptions (FieldCat match, bool optGroups, bool showFieldNames, bool showCustom, string [] selected, out int titlePos) {
				int c = 0;
				string options = string.Empty;
				bool hasPeop = HasPeop ();
				IEnumerable<FieldDesc> fieldsEnum;
				List<FieldDesc> fieldsList;
				FieldCat lastCat = FieldCat.None;
				titlePos = -1;
				if ((fieldsEnum = GetFields (match)) != null) {
					if (!optGroups) {
						fieldsList = new List<FieldDesc> (fieldsEnum);
						fieldsList.Sort (delegate (FieldDesc one, FieldDesc two) {
							if ((!hasPeop) && (one.Title == "Title") && (two.Title != "Title"))
								return "a".CompareTo ("b");
							else if ((!hasPeop) && (two.Title == "Title") && (one.Title != "Title"))
								return "b".CompareTo ("a");
							else
								return one.Title.CompareTo (two.Title);
						});
						fieldsEnum = fieldsList;
					}
					foreach (FieldDesc fd in fieldsEnum)
						if ((fd.Cat != FieldCat.Custom) || showCustom) {
							if (optGroups && (fd.Cat != lastCat)) {
								if (optGroups && (lastCat != FieldCat.None))
									options += "</optgroup>";
								options += ("<optgroup label=\"" + HttpUtility.HtmlEncode (this ["FieldCat_" + (lastCat = fd.Cat)]) + "\">");
								c++;
							}
							if ((!hasPeop) && (fd.Name == "Title"))
								titlePos = c;
							c++;
							options += string.Format (FORMAT_LISTOPTION, HttpUtility.HtmlEncode (fd.Name), HttpUtility.HtmlEncode (fd.Title) + ((fd.Name.Equals (fd.Title) || !showFieldNames) ? string.Empty : string.Format (" &mdash; [ " + HttpUtility.HtmlEncode (fd.Name) + " ]")), (Array.IndexOf<string> (selected, fd.Name) >= 0) ? " selected=\"selected\"" : string.Empty);
						}
				}
				if (optGroups && !string.IsNullOrEmpty (options))
					options += "</optgroup>";
				return options;
			}

			internal bool HasPeop () {
				if (((hasPeop == null) || !hasPeop.HasValue) && (parentWebPart != null) && (parentWebPart.connectedParts != null)) {
					hasPeop = false;
					foreach (WebPart wp in parentWebPart.connectedParts)
						if (wp.GetType ().Name == "roxority_UserListWebPart") {
							hasPeop = true;
							break;
						}
				}
				return ((hasPeop != null) && hasPeop.Value);
			}

			internal bool IsAnd (string name, bool defVal) {
				IEnumerable<KeyValuePair<string, string>> pairs;
				if ((fps == null) && ((pairs = FilterPairs) != null))
					foreach (KeyValuePair<string, string> kvp in pairs)
						break;
				if (fps != null)
					foreach (FilterPair fp in fps)
						if (fp.Key == name)
							return fp.nextAnd;
				return defVal;
			}

			internal bool IsAllowed (string name) {
				return (((parentWebPart != null) && (parentWebPart.connectedList != null)) ? ((ProductPage.GetField (parentWebPart.connectedList, name) != null) && (Array.IndexOf<string> (blockedFields, name) < 0) && ((!name.EndsWith ("2", StringComparison.InvariantCultureIgnoreCase)) || (Array.IndexOf<string> (blockedFields, name.Substring (0, name.Length - 1)) < 0))) : true);
			}

			internal bool IsTitle (string name) {
				return ((!HasPeop ()) && (Array.IndexOf<string> (titleFields, name) >= 0));
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				info.AddValue ("ListFieldNames", ListFieldNames, typeof (string []));
				info.AddValue ("CustomFieldNames", CustomFieldNames, typeof (string []));
				info.AddValue ("AllowAllInheritedFields", AllowAllInheritedFields);
				info.AddValue ("AllowAllListFields", AllowAllListFields);
				info.AddValue ("AllowAllOtherFields", AllowAllOtherFields);
				info.AddValue ("AllowAllViewFields", AllowAllViewFields);
				info.AddValue ("AllowAnyField", AllowAnyField);
				info.AddValue ("AllowAnyAllOps", AllowAnyAllOps);
				info.AddValue ("GroupFields", GroupFields);
				info.AddValue ("Indent", Indent);
				info.AddValue ("AnyIsAll", AnyIsAll);
				base.GetObjectData (info, context);
			}

			public override void Render (HtmlTextWriter output, bool isUpperBound) {
				int tp;
				string options = string.Empty, defField = AllowAnyField ? "*" : string.Empty;
				string [] lox = { "CamlOp_And", "CamlOp_Or", "MultiChecked" };
				bool autoSuggest = AutoSuggest && (parentWebPart.connectedList != null);
				int limit = 40;
				IEnumerable<FieldDesc> fields = GetFields (FieldMask);
				List<FieldDesc>fieldsList = fields as List<FieldDesc>;
				if ((!int.TryParse (ProductPage.Config (ProductPage.GetContext (), "AutoLimit"), out limit)) || (limit <= 1))
					limit = 40;
				if (string.IsNullOrEmpty (defField) && (fields != null))
					foreach (FieldDesc fd in fields) {
						defField = fd.Name;
						break;
					}
/*
output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery(document).ready(function() { jQuery('#" + cid + "').autocomplete('" + parentWebPart.connectedList.ParentWeb.Url.TrimEnd ('/') + "/_layouts/roxority_FilterZen/jqas.aspx?f=" + HttpUtility.UrlEncode (Name) + "&v=" + ((parentWebPart.connectedView == null) ? string.Empty : ProductPage.GuidLower (parentWebPart.connectedView.ID, true)) + "&b=" + ((CamlOperator == (int) (Operator.BeginsWith)) ? 1 : 0) + "&l=" + ProductPage.GuidLower (parentWebPart.connectedList.ID, true) + "', { \"max\": " + limit + ", \"delay\": 100, \"minChars\": 1, \"selectFirst\": false, \"multiple\": " + (string.IsNullOrEmpty (MultiValueSeparator) ? "false" : "true") + ", \"multipleSeparator\": \"" + MultiValueSeparator + "\", \"matchContains\": " + ((CamlOperator != (int) (Operator.Eq)) ? "true" : "false") + " }); }); </script>");
*/
				output.Write (@"
<script type=""text/javascript"" language=""JavaScript"">
roxMultiOpsAll = '{12}';
roxMultiOpsAny = '{13}';
roxMultiOpsNone = '{14}';
roxMultiOpsNum = '{15}';
roxMultiOpsStr = '{16}';
roxMultiOpsUser = '{22}';
roxMultis['{2}'] = {0}
	fieldOpts: '{11}',
	ctlID: '{3}',
	indent: {10},
	allowOps: {5},
	allowAnyAllOps: {6},
	defField: '{8}',
	defOp: '{4}',
	defLop: '{7}',
	isCaml : {9},
	cfg: null,
	fields: {17},
	fieldTypes: {18},
	allowMulti: {23},
	autoComplete: {0}
		active: {19},
		url: '{20}',
		opts: {0}
			max: {21},
			delay: 100,
			minChars: 1,
			selectFirst: false,
			multiple: false,
			cacheLength: 0,
			matchContains: true
		{1}
	{1}
{1};
roxMultiDateCounts['{2}'] = 0;
roxMultiUserCounts['{2}'] = 0;
roxMultiInit('{2}');
",
 "{", "}", ID, parentWebPart.MultiTextBox.ClientID, (Operator) CamlOperator, parentWebPart.CamlFilters.ToString ().ToLowerInvariant (), AllowAnyAllOps.ToString ().ToLowerInvariant (), parentWebPart.DefaultToOr ? "Or" : "And", SPEncode.ScriptEncode (defField), parentWebPart.CamlFilters.ToString ().ToLowerInvariant (), Indent.ToString ().ToLowerInvariant (),
		  SPEncode.ScriptEncode ((AllowAnyField ? ("<option value=\"*\">" + this ["FieldCat_Any"] + "</option>") : string.Empty) + GetFieldOptions (FieldMask, GroupFields, false, true, new string [0], out tp)),
		  SPEncode.ScriptEncode (GetOperatorOptions (allOperators)),
		  SPEncode.ScriptEncode (GetOperatorOptions (anyOperators)),
		  SPEncode.ScriptEncode (GetOperatorOptions (new Operator [] { Operator.Eq })),
		  SPEncode.ScriptEncode (GetOperatorOptions (numberOperators)),
		  SPEncode.ScriptEncode (GetOperatorOptions (stringOperators)),
		  ((fieldsList == null) ? "null" : ("{" + string.Join (",", fieldsList.ConvertAll<string> (delegate (FieldDesc fd) {
					return string.Format ("\"{0}\": {1}", fd.Name, GetOperators (((fd.SPField) == null) ? string.Empty : fd.SPField.TypeAsString));
				}).ToArray ()) + "}")),
		  ((fieldsList == null) ? "null" : ("{" + string.Join (",", fieldsList.ConvertAll<string> (delegate (FieldDesc fd) {
					return string.Format ("\"{0}\": \"{1}\"", fd.Name, ((fd.SPField == null) ? string.Empty : fd.SPField.TypeAsString));
				}).ToArray ()) + "}")),
		autoSuggest.ToString ().ToLowerInvariant (),
		autoSuggest ? SPEncode.ScriptEncode ((parentWebPart.connectedList.ParentWeb.Url.TrimEnd ('/') + "/_layouts/roxority_FilterZen/jqas.aspx?v=" + ((parentWebPart.connectedView == null) ? string.Empty : ProductPage.GuidLower (parentWebPart.connectedView.ID, true)) + "&l=" + ProductPage.GuidLower (parentWebPart.connectedList.ID, true) + "&sf=" + HttpUtility.UrlEncode (parentWebPart.AcSecFields))) : string.Empty,
		limit,
		SPEncode.ScriptEncode (GetOperatorOptions (userOperators)),
		Get<bool> ("AllowMultiEnter").ToString ().ToLowerInvariant ()
		  );
				foreach (string l in lox)
					output.Write ("roxLox['" + l + "'] = '" + SPEncode.ScriptEncode (this [l]) + "';");
				output.Write ("</script><div class=\"rox-multibox rox-multibox-" + ID + " rox-multibox-" + parentWebPart.ClientID + "\">");
				output.Write ("</div>");
				base.Render (output, isUpperBound);
			}

			public override void UpdatePanel (Panel panel) {
				int tp;
				string options = string.Empty, formatDisabledList = FORMAT_LIST.Replace ("<select ", "<select disabled=\"disabled\" "), formatDisabledCheckBox = FORMAT_CHECKBOX.Replace ("<input ", "<input disabled=\"disabled\" "), formatDisabledTextArea = FORMAT_TEXTAREA.Replace ("<textarea ", "<textarea disabled=\"disabled\" ");
				hiddenProperties.AddRange (new string [] { "Label", "DefaultIfEmpty", "IsInteractive", "SendEmpty", "SuppressMultiValues", "MultiFilterSeparator" });
				fields = null;
				panel.Controls.Add (new LiteralControl ("<div>" + this ["FilterDesc_Multi"] + "</div>"));
				panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilterspecial').slideToggle();\" href=\"#noop\">" + GetFilterTypeTitle (GetType ()) + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText;\" id=\"roxfilterspecial\">"));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "AllowAllViewFields", GetChecked (Get<bool> ("AllowAllViewFields"))));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "AllowAllListFields", GetChecked (Get<bool> ("AllowAllListFields"))));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "AllowAllInheritedFields", GetChecked (Get<bool> ("AllowAllInheritedFields"))));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "AllowAllOtherFields", GetChecked (Get<bool> ("AllowAllOtherFields"))));
				options = GetFieldOptions (FieldCat.All, true, true, false, ListFieldNames, out tp);
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_LIST : formatDisabledList, "ListFieldNames", " size=\"" + ((tp > 10) ? (tp + 2) : 12) + "\" multiple=\"multiple\"", options));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_TEXTAREA : formatDisabledTextArea, "CustomFieldNames", Get<string> ("CustomFieldNames")));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "AllowAnyField", GetChecked (Get<bool> ("AllowAnyField"))));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "AnyIsAll", GetChecked (Get<bool> ("AnyIsAll"))));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "AllowAnyAllOps", GetChecked (Get<bool> ("AllowAnyAllOps"))));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "GroupFields", GetChecked (Get<bool> ("GroupFields"))));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "Indent", GetChecked (Get<bool> ("Indent"))));
				panel.Controls.Add (new LiteralControl ("</fieldset><style type=\"text/css\"> div#div_filter_Label, div#div_filter_IsInteractive { display: none !important; visibility: hidden !important; } </style>"));
				base.UpdatePanel (panel);
				//panel.Controls.Add (new LiteralControl ("<style type=\"text/css\"> fieldset#roxfilterspecial, fieldset#roxfilteradvanced, div.roxsectionlink { display: none; } </style>"));
			}

			public override void UpdateProperties (Panel panel) {
				HttpContext context = Context;
				string cfn = ((context == null) ? Get<string> ("CustomFieldNames") : context.Request ["filter_CustomFieldNames"]), lfn = ((context == null) ? Get<string> ("ListFieldNames") : context.Request ["filter_ListFieldNames"]);
				if (cfn == null)
					cfn = string.Empty;
				if (lfn == null)
					lfn = string.Empty;
				CustomFieldNames = cfn.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				ListFieldNames = lfn.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
				AllowAllInheritedFields = Get<bool> ("AllowAllInheritedFields");
				AllowAllListFields = Get<bool> ("AllowAllListFields");
				AllowAllOtherFields = Get<bool> ("AllowAllOtherFields");
				AllowAllViewFields = Get<bool> ("AllowAllViewFields");
				AllowAnyField = Get<bool> ("AllowAnyField");
				AllowAnyAllOps = Get<bool> ("AllowAnyAllOps");
				GroupFields = Get<bool> ("GroupFields");
				Indent = Get<bool> ("Indent");
				AnyIsAll = Get<bool> ("AnyIsAll");
				base.UpdateProperties (panel);
			}

			protected internal override IEnumerable<KeyValuePair<string, string>> FilterPairs {
				get {
					string tmp;
					ArrayList filters;
					if ((fps == null) && (parentWebPart != null) && !string.IsNullOrEmpty (parentWebPart.MultiTextBox.Text))
						try {
							if ((filters = JSON.JsonDecode (parentWebPart.MultiTextBox.Text) as ArrayList) == null)
								throw new Exception (this ["JsonSyntax"] + " -- " + parentWebPart.MultiTextBox.Text);
							fps = new List<FilterPair> ();
							foreach (Hashtable ht in filters)
								if ("*".Equals (tmp = ht ["field"] + string.Empty, StringComparison.InvariantCultureIgnoreCase) || string.IsNullOrEmpty (tmp))
									foreach (FieldDesc fd in GetFields (AnyIsAll ? FieldCat.All : FieldMask)) {
										if (((fd.SPField == null) || (fd.SPField.Type != SPFieldType.Boolean)) && (!fps.Exists (delegate (FilterPair test) {
											return test.Key == fd.Name;
										})))
											fps.Add (new FilterPair (fd.Name, ht ["val"] + string.Empty, (Operator) Enum.Parse (typeof (Operator), ht ["op"] + string.Empty, true)) {
												nextAnd = false
											});
									} else if (!fps.Exists (delegate (FilterPair test) {
										return (test.Key == tmp) && (ht ["val"] + string.Empty).Equals (test.Value) && test.CamlOperator.Equals ((Operator) Enum.Parse (typeof (Operator), ht ["op"] + string.Empty, true)) && test.nextAnd.Equals (!"or".Equals (ht ["lop"] + string.Empty, StringComparison.InvariantCultureIgnoreCase));
								}))
									fps.Add (new FilterPair (tmp, ht ["val"] + string.Empty, (Operator) Enum.Parse (typeof (Operator), ht ["op"] + string.Empty, true)) {
										nextAnd = !"or".Equals (ht ["lop"] + string.Empty, StringComparison.InvariantCultureIgnoreCase)
									});
						} catch (Exception ex) {
							parentWebPart.warningsErrors.Add (new KeyValuePair<FilterBase, Exception> (this, ex));
						}
					if (fps != null)
						for (int i = 0; i < fps.Count; i++)
							if ((fps [i].CamlOperator == Operator.Me) || (fps [i].CamlOperator == Operator.NotMe)) {
								fps [i].Value = SPContext.Current.Web.CurrentUser.Name;
								fps [i].CamlOperator = ((fps [i].CamlOperator == Operator.NotMe) ? Operator.Neq : Operator.Eq);
							}
					return (((fps == null) || (fps.Count == 0)) ? null : fps.ConvertAll<KeyValuePair<string, string>> (delegate (FilterPair fp) {
						return new KeyValuePair<string, string> (fp.Key, fp.Value);
					}));
				}
			}

			public bool AllowAllInheritedFields {
				get {
					return allowAllInheritedFields;
				}
				set {
					allowAllInheritedFields = value;
				}
			}

			public bool AllowAllListFields {
				get {
					return allowAllListFields;
				}
				set {
					allowAllListFields = value;
				}
			}

			public bool AllowAllOtherFields {
				get {
					return allowAllOtherFields;
				}
				set {
					allowAllOtherFields = value;
				}
			}

			public bool AllowAllViewFields {
				get {
					return allowAllViewFields;
				}
				set {
					allowAllViewFields = value;
				}
			}

			public bool AllowAnyAllOps {
				get {
					return allowAnyAllOps;
				}
				set {
					allowAnyAllOps = value;
				}
			}

			public bool AllowAnyField {
				get {
					return allowAnyField;
				}
				set {
					allowAnyField = value;
				}
			}

			public bool AnyIsAll {
				get {
					return anyIsAll;
				}
				set {
					anyIsAll = value;
				}
			}

			public string [] CustomFieldNames {
				get {
					return customFieldNames;
				}
				set {
					customFieldNames = value;
				}
			}

			public FieldCat FieldMask {
				get {
					FieldCat cat = (AllowAllViewFields ? FieldCat.ListView : FieldCat.None);
					if (AllowAllListFields)
						cat |= FieldCat.ListOther;
					if (AllowAllInheritedFields)
						cat |= FieldCat.ListInherited;
					if (AllowAllOtherFields)
						cat |= FieldCat.ListSystem;
					if ((ListFieldNames.Length > 0) || (CustomFieldNames.Length > 0))
						cat |= FieldCat.Custom;
					return cat;
				}
			}

			public bool GroupFields {
				get {
					return groupFields;
				}
				set {
					groupFields = value;
				}
			}

			public bool Indent {
				get {
					return indent;
				}
				set {
					indent = value;
				}
			}

			public override bool IsInteractive {
				get {
					return true;
				}
				set {
				}
			}

			public override string Label {
				get {
					return string.Empty;
				}
				set {
				}
			}

			public string [] ListFieldNames {
				get {
					return listFieldNames;
				}
				set {
					listFieldNames = value;
				}
			}

		}

		#endregion

		#region PageField Class

		[Serializable]
		internal sealed class PageField : Lookup {

			public PageField () {
				Init ();
				defaultIfEmpty = false;
			}

			public PageField (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				Init ();
			}

			private void Init () {
				suppressInteractive = true;
				hiddenProperties.AddRange (new string [] { "ListUrl", "DisplayFieldName", "ItemID", "RemoveDuplicateValues", "RemoveDuplicateTitles", "ItemSorting", "FilterCaml" });
				itemID = -1;
			}

			public override void UpdatePanel (Panel panel) {
				using (SPWrap<SPList> wrap = (GetList ("ListUrl", true)))
					if (wrap.Value == null)
						Report (new Exception (this ["NoPageLibrary"]));
				base.UpdatePanel (panel);
			}

		}

		#endregion

		#region RequestParameter Class

		[Serializable]
		internal sealed class RequestParameter : FilterBase {

			private static List<string> blockedParams = null;

			private bool catchAll = false, sendNull = false;
			private int requestMode = 0;
			private string parameterName = string.Empty, subParameterName = string.Empty;

			internal static List<string> BlockedParams {
				get {
					if (blockedParams == null)
						blockedParams = new List<string> (ProductPage.Config (SPContext.Current, "BlockedParams").Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).ConvertAll<string> (delegate (string value) {
							return value.ToLowerInvariant ();
						});
					return blockedParams;
				}
			}

			public RequestParameter () {
			}

			public RequestParameter (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				try {
					CatchAll = info.GetBoolean ("CatchAll");
					ParameterName = info.GetString ("ParameterName");
					RequestMode = info.GetInt32 ("RequestMode");
					SendNull = info.GetBoolean ("SendNull");
					SubParameterName = info.GetString ("SubParameterName");
				} catch {
				}
			}

			internal string GetParameterName () {
				string name = Get<string> ("ParameterName");
				List<string> vals;
				if (string.IsNullOrEmpty (name))
					name = Name;
				if (name.StartsWith ("{{") && name.EndsWith ("}}") && (name.Length > 4) && ((vals = GetParameterValues (name.Substring (2, name.Length - 4), string.Empty)) != null) && (vals.Count > 0))
					name = vals [0];
				return name;
			}

			internal List<string> GetParameterValues (string paramName, string subParamName) {
				string tmp, paramValue;
				string [] vals;
				int pos, delCount;
				HttpCookie cookie = null;
				List<string> fragments = new List<string> (), paramValues = new List<string> ();
				NameValueCollection col = ((RequestMode == 0) ? Context.Request.QueryString : ((RequestMode == 1) ? Context.Request.Form : null));
				if (RequestMode == 3) {
					fragments.AddRange (Context.Request.Url.ToString ().Split ('/'));
					if ((!string.IsNullOrEmpty (paramName = ProductPage.Trim (paramName))) && ((pos = fragments.IndexOf (paramName)) >= 0) && (pos < (fragments.Count - 1)))
						paramValues.Add (fragments [pos + 1]);
				} else if (RequestMode == 2) {
					foreach (NameValueCollection nvc in new NameValueCollection [] { Context.Request.QueryString, Context.Request.Form, Context.Request.ServerVariables, ((Array.IndexOf<string> (Context.Request.Cookies.AllKeys, paramName) >= 0) && ((cookie = Context.Request.Cookies [paramName]) != null)) ? cookie.Values : null })
						if ((nvc != null) && ((vals = nvc.GetValues (paramName)) != null) && (vals.Length > 0))
							paramValues.AddRange (vals);
						else if ((nvc != null) && (cookie != null) && (nvc == cookie.Values))
							for (int i = 0; i < nvc.Count; i++)
								paramValues.Add (nvc [i]);
				} else if (((vals = col.GetValues (paramName)) != null) && (vals.Length > 0))
					paramValues.AddRange (vals);
				if (!string.IsNullOrEmpty (subParamName)) {
					delCount = paramValues.Count;
					foreach (string pv in paramValues)
						if ((pos = (paramValue = pv).IndexOf (tmp = subParamName + ":\"")) >= 0)
							paramValues.Add (((pos = (paramValue = paramValue.Substring (pos + tmp.Length)).IndexOf ('\"')) >= 0) ? paramValue.Substring (0, pos) : paramValue);
					paramValues.RemoveRange (0, delCount);
				}
				return paramValues;
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				info.AddValue ("CatchAll", CatchAll);
				info.AddValue ("ParameterName", ParameterName);
				info.AddValue ("RequestMode", RequestMode);
				info.AddValue ("SendNull", SendNull);
				info.AddValue ("SubParameterName", SubParameterName);
				base.GetObjectData (info, context);
			}

			public override void UpdatePanel (Panel panel) {
				string options = "";
				panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilterspecial').slideToggle();\" href=\"#noop\">" + this ["FilterProps", GetFilterTypeTitle (GetType ())] + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText;\" id=\"roxfilterspecial\" style=\"display: none;\">"));
				panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "ParameterName", GetParameterName (), " onchange=\"roxUpdatePreview();\" onkeyup=\"roxUpdatePreview();\""));
				panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "SubParameterName", Get<string> ("SubParameterName"), " onchange=\"roxUpdatePreview();\" onkeyup=\"roxUpdatePreview();\""));
				for (int i = 0; i <= 3; i++)
					options += string.Format (FORMAT_LISTOPTION, i, this ["RequestMode" + i], (i == Get<int> ("RequestMode")) ? " selected=\"selected\"" : string.Empty);
				panel.Controls.Add (CreateControl (FORMAT_LIST, "RequestMode", " onchange=\"roxUpdatePreview();\"", options));
				panel.Controls.Add (new LiteralControl (string.Format (FORMAT_GENERIC_PREFIX + "<span id=\"span_Preview\"></span>" + FORMAT_GENERIC_SUFFIX, "Preview", this ["Prop_Preview"])));
				panel.Controls.Add (CreateControl (FORMAT_CHECKBOX, "SendNull", GetChecked (Get<bool> ("SendNull"))));
				panel.Controls.Add (CreateControl (FORMAT_CHECKBOX, "CatchAll", string.Empty, "document.getElementById('div_filter_ParameterName').style.display=document.getElementById('div_filter_SubParameterName').style.display=document.getElementById('div_filter_SendNull').style.display=((this.checked)?'none':'block');roxUpdatePreview();"));
				panel.Controls.Add (CreateScript ("roxUpdatePreview = function() { if (document.getElementById('filter_RequestMode').selectedIndex == 3) jQuery('#span_Preview').html('<span style=\"color: GrayText;\">http://server/xyz/</span><b>' + jQuery.trim(jQuery('#filter_ParameterName').val()) + '</b>/<i>" + this ["JsValue"] + "</i><span style=\"color: GrayText;\">/xyz/</span>'); else jQuery('#span_Preview').html('<span style=\"color: GrayText;\">page.aspx?' + ((document.getElementById('filter_CatchAll').checked & (jQuery('#' + nameTextBoxID).val() == '?')) ? '</span><b>" + this ["JsFilter"] + "</b>=<i>" + this ["JsValue"] + "</i>&<b>" + this ["JsName"] + "</b>=<i>" + this ["JsValue"] + "</i>&<b>" + this ["JsColumn"] + "</b>=<i>" + this ["JsValue"] + "</i>' : ('x=y&</span><b>' + (((document.getElementById('filter_CatchAll').checked) || (jQuery.trim(jQuery('#filter_ParameterName').val()).length == 0)) ? jQuery('#' + nameTextBoxID).val() : jQuery('#filter_ParameterName').val()) + '</b>=' + (((!document.getElementById('filter_CatchAll').checked) && (jQuery.trim(jQuery('#filter_SubParameterName').val()).length == 0)) ? '<i>" + this ["JsFilterValue"] + "</i>' : (document.getElementById('filter_CatchAll').checked ? ('" + this ["JsFilter"] + "1:\"<i>" + this ["JsValue"] + "1</i>\" " + this ["JsColumn"] + "2:\"<i>" + this ["JsValue"] + "2</i>\" " + this ["JsName"] + "3:\"<i>" + this ["JsValue"] + "3</i>\"') : ('a:\"b\" <b>' + jQuery('#filter_SubParameterName').val() + '</b>:\"<i>" + this ["JsFilterValue"] + "</i>\" c:\"d\"'))) + '<span style=\"color: GrayText;\">&x=y')) + '</span>'); jQuery('#div_Preview').css({ 'display': ((document.getElementById('filter_RequestMode').selectedIndex == 1) ? 'none' : 'block') }); }; document.getElementById('div_filter_ParameterName').style.display=document.getElementById('div_filter_SubParameterName').style.display=document.getElementById('div_filter_SendNull').style.display=((document.getElementById('filter_CatchAll').checked=" + (Get<bool> ("CatchAll") ? "true" : "false") + ")?'none':'block'); jQuery(document).ready(function() { roxUpdatePreview(); });"));
				panel.Controls.Add (new LiteralControl ("</fieldset>"));
				base.UpdatePanel (panel);
			}

			public override void UpdateProperties (Panel panel) {
				CatchAll = Get<bool> ("CatchAll");
				ParameterName = Get<string> ("ParameterName");
				RequestMode = Get<int> ("RequestMode");
				SendNull = Get<bool> ("SendNull");
				SubParameterName = Get<string> ("SubParameterName");
				base.UpdateProperties (panel);
			}

			protected internal override IEnumerable<KeyValuePair<string, string>> FilterPairs {
				get {
					List<NameObjectCollectionBase> cols = new List<NameObjectCollectionBase> ();
					NameValueCollection valCol;
					bool catchAll = Get<bool> ("CatchAll");
					string paramName = GetParameterName (), subParamName = Get<string> ("SubParameterName"), pval;
					string [] multiVals;
					List<string> paramValues = ((catchAll && (Name == "?")) ? new List<string> () : GetParameterValues (catchAll ? Name : paramName, catchAll ? string.Empty : subParamName));
					if (catchAll) {
						if (Name == "?") {
							if (RequestMode != 1)
								cols.Add (Context.Request.QueryString);
							if (RequestMode != 0)
								cols.Add (Context.Request.Form);
							if ((RequestMode != 0) && (RequestMode != 1)) {
								cols.Add (Context.Request.Cookies);
								cols.Add (Context.Request.ServerVariables);
							}
							foreach (NameObjectCollectionBase col in cols) {
								valCol = col as NameValueCollection;
								foreach (string k in col.Keys)
									if (!string.IsNullOrEmpty (k))
										if (BlockedParams.Contains (k.ToLowerInvariant ())) {
											if (parentWebPart != null)
												parentWebPart.filtersNotSent.Add (new KeyValuePair<string, string> (k, "Blocked"));
										} else
											if ((valCol == null) || ((multiVals = valCol.GetValues (k)) == null) || (multiVals.Length == 0))
												yield return new KeyValuePair<string, string> (k, (valCol != null) ? valCol [k] : Context.Request.Cookies [k].Value);
											else
												foreach (string mv in multiVals)
													yield return new KeyValuePair<string, string> (k, mv);
							}
						} else {
							if ((paramValues.Count == 0) && Get<bool> ("SendNull"))
								yield return new KeyValuePair<string, string> (paramName, string.Empty);
							foreach (string pv in paramValues)
								foreach (string pair in pv.Split (new string [] { "\" " }, StringSplitOptions.RemoveEmptyEntries))
									yield return new KeyValuePair<string, string> (pair.Substring (0, pair.IndexOf (':')), (pval = pair.Substring (pair.IndexOf (":\"") + 2)).Substring (0, pval.Length - (pval.EndsWith ("\"") ? 1 : 0)));
						}
					} else {
						if ((paramValues.Count == 0) && Get<bool> ("SendNull"))
							yield return new KeyValuePair<string, string> (Name, string.Empty);
						foreach (string pv in paramValues)
							if ((!string.IsNullOrEmpty (pval = ProductPage.Trim (pv))) || Get<bool> ("SendNull"))
								yield return new KeyValuePair<string, string> (Name, pval);
					}
				}
			}

			public bool CatchAll {
				get {
					return catchAll;
				}
				set {
					catchAll = value;
				}
			}

			public string ParameterName {
				get {
					return parameterName;
				}
				set {
					parameterName = ProductPage.Trim (value);
				}
			}

			public int RequestMode {
				get {
					return requestMode;
				}
				set {
					requestMode = (((value >= 1) && (value <= 3)) ? value : 0);
				}
			}

			public bool SendNull {
				get {
					return sendNull;
				}
				set {
					sendNull = value;
				}
			}

			public string SubParameterName {
				get {
					return subParameterName;
				}
				set {
					subParameterName = ProductPage.Trim (value);
				}
			}

		}

		#endregion

		#region SqlData Class

		[Serializable]
		internal class SqlData : Interactive {

			public const string CHOICE_FIRST = "4aea04a6-787a-4135-81a0-195e5946db1f", CHOICE_LAST = "c3bd9af3-8df1-49bd-835b-36f5d64b060c";

			private static readonly string [] choices = new string [] { "Empty", "ResultFirst", "ResultLast" };
			private static readonly string scriptCheckDefault = SCRIPT_CHECK_DEFAULT.Replace (PLACEHOLDER_LISTID, "SqlChoice");

			private string adoConnectionString = string.Empty, adoDataProvider = string.Empty, displayColumnName = string.Empty, query = string.Empty, valueColumnName = string.Empty;
			private string choice = CHOICE_EMPTY;
			private List<KeyValuePair<int, string []>> items = null;

			public SqlData () {
				pickerSemantics = true;
				defaultIfEmpty = true;
				reqEd = 4;
			}

			public SqlData (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				reqEd = 4;
				pickerSemantics = true;
				try {
					AdoConnectionString = info.GetString ("AdoConnectionString");
					AdoDataProvider = info.GetString ("AdoDataProvider");
					DisplayColumnName = info.GetString ("DisplayColumnName");
					Query = info.GetString ("Query");
					ValueColumnName = info.GetString ("ValueColumnName");
					SqlChoice = info.GetString ("SqlChoice");
					DefaultIfEmpty = info.GetBoolean ("DefaultIfEmpty");
				} catch {
				}
			}

			internal IDbConnection CreateConnection () {
				string provider = Get<string> ("AdoDataProvider"), providerTypeName = provider.Substring (0, provider.IndexOf (',')), providerAssembly = provider.Substring (provider.IndexOf (',') + 1).Trim ();
				Type providerType;
				if (providerAssembly.Equals ("System.Data", StringComparison.InvariantCultureIgnoreCase))
					providerType = typeof (DataTable).Assembly.GetType (providerTypeName, true, true);
				else if (providerAssembly.Equals ("System.Data.OracleClient", StringComparison.InvariantCultureIgnoreCase))
					providerType = typeof (OracleConnection).Assembly.GetType (providerTypeName, true, true);
				else
					providerType = Type.GetType (provider, true, true);
				return providerType.GetConstructor (new Type [] { typeof (string) }).Invoke (new object [] { Get<string> ("AdoConnectionString") }) as IDbConnection;
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				base.GetObjectData (info, context);
				info.AddValue ("AdoConnectionString", AdoConnectionString);
				info.AddValue ("AdoDataProvider", AdoDataProvider);
				info.AddValue ("SqlChoice", SqlChoice);
				info.AddValue ("DisplayColumnName", DisplayColumnName);
				info.AddValue ("Query", Query);
				info.AddValue ("ValueColumnName", ValueColumnName);
			}

			public override void Render (HtmlTextWriter output, bool isUpperBound) {
				string options = "";
				int index = -1;
				bool checkStyle = Get<bool> ("CheckStyle");
				List<string> selIndices = GetFilterValues (PREFIX_FIELDNAME + ID, Get<string> ("SqlChoice").ToString ());
				if (selIndices.Contains (CHOICE_EMPTY))
					selIndices.Clear ();
				if ((selIndices.Count == 1) && (string.IsNullOrEmpty (selIndices [0]) || selIndices [0].Equals (CHOICE_EMPTY)))
					selIndices.Clear ();
				if (!Le (4, true)) {
					output.WriteLine (ProductPage.GetResource ("NopeEd", GetFilterTypeTitle (GetType ()), "Ultimate"));
					base.Render (output, isUpperBound);
					return;
				}
				try {
					if (items == null) {
						if (!postFiltered)
							doPostFilterNow = postFiltered = true;
						Items.ToString ();
					} else if (Cascade && !postFiltered) {
						postFiltered = doPostFilterNow = true;
						Items.ToString ();
					}
					if (Get<bool> ("DefaultIfEmpty")) {
						output.Write ("<script type=\"text/javascript\" language=\"JavaScript\"> roxMultiMins['filterval_" + ID + "'] = '" + CHOICE_EMPTY + "'; </script>");
						if (!checkStyle)
							options += string.Format (FORMAT_LISTOPTION, CHOICE_EMPTY, this ["Empty" + (Get<bool> ("SendEmpty") ? "None" : "All")], ((selIndices.Count == 0) || selIndices.Contains (CHOICE_EMPTY)) ? " selected=\"selected\"" : string.Empty);
						else
							options += string.Format ("<span><input class=\"rox-check-default\" name=\"" + PREFIX_FIELDNAME + ID + "\" type=\"" + (AllowMultiEnter ? "checkbox" : "radio") + "\" id=\"empty_" + PREFIX_FIELDNAME + ID + "\" value=\"{1}\" {3}" + (string.IsNullOrEmpty (HtmlOnChangeAttr) ? (" onclick=\"jQuery(\'.chk-" + ID + "\').attr(\'checked\', false);\"") : HtmlOnChangeAttr.Replace ("onchange=\"", "onclick=\"jQuery('.chk-" + ID + "').attr('checked', false);")) + "/><label for=\"empty_" + PREFIX_FIELDNAME + ID + "\">{2}</label></span>", ProductPage.GuidLower (Guid.NewGuid ()), CHOICE_EMPTY, this ["Empty" + (Get<bool> ("SendEmpty") ? "None" : "All")], ((selIndices.Count == 0) || selIndices.Contains (CHOICE_EMPTY)) ? " checked=\"checked\"" : string.Empty);
					}
					foreach (KeyValuePair<int, string []> kvp in items) {
						index++;
						if (checkStyle)
							options += string.Format ("<span><input class=\"chk-" + ID + " rox-check-value\" name=\"" + PREFIX_FIELDNAME + ID + "\" type=\"" + (AllowMultiEnter ? "checkbox" : "radio") + "\" id=\"x{0}\" value=\"{1}\" {3}" + ((string.IsNullOrEmpty (HtmlOnChangeAttr) && Get<bool> ("DefaultIfEmpty")) ? (" onclick=\"document.getElementById('empty_" + PREFIX_FIELDNAME + ID + "').checked=false;\"") : HtmlOnChangeAttr.Replace ("onchange=\"", "onclick=\"" + (Get<bool> ("DefaultIfEmpty") ? ("document.getElementById('empty_" + PREFIX_FIELDNAME + ID + "').checked=false;") : string.Empty))) + "/><label for=\"x{0}\">{2}</label></span>", ProductPage.GuidLower (Guid.NewGuid ()), kvp.Value [1], GetDisplayValue (kvp.Value [0]), (selIndices.Contains (kvp.Value [1]) || (selIndices.Contains (CHOICE_FIRST) && (index == 0)) || (selIndices.Contains (CHOICE_LAST) && (index == (items.Count - 1)))) ? " checked=\"checked\"" : string.Empty);
						else
							options += string.Format (FORMAT_LISTOPTION, kvp.Value [1], GetDisplayValue (kvp.Value [0]), (selIndices.Contains (kvp.Value [1]) || (selIndices.Contains (CHOICE_FIRST) && (index == 0)) || (selIndices.Contains (CHOICE_LAST) && (index == (items.Count - 1)))) ? " selected=\"selected\"" : string.Empty);
						if ((PickerLimit != 0) && ((index + 1) >= PickerLimit))
							break;
					}
					if (options.Length > 0)
						if (checkStyle)
							output.Write ("<div>" + options + "</div>");
						else
							output.Write ("<select" + (AllowMultiEnter ? (" size=\"1\" multiple=\"multiple\" class=\"rox-multiselect ms-input\"") : " class=\"ms-input\"") + " name=\"{0}\" id=\"{0}\"{1}>" + options + "</select>", PREFIX_FIELDNAME + ID, AllowMultiEnter ? HtmlOnChangeMultiAttr : HtmlOnChangeAttr);
				} catch (Exception ex) {
					Report (ex);
				}
				base.Render (output, isUpperBound);
			}

			public override void UpdatePanel (Panel panel) {
				string options = "", optVal, key, itemID = Get<string> ("SqlChoice"), formatDisabledTextBox = FORMAT_TEXTBOX.Replace ("<input ", "<input disabled=\"disabled\" "), formatDisabledList = FORMAT_LIST.Replace ("<select ", "<select disabled=\"disabled\" "), formatDisabledCheckBox = FORMAT_CHECKBOX.Replace ("<input ", "<input disabled=\"disabled\" "), formatDisabledTextArea = FORMAT_TEXTAREA.Replace ("<textarea ", "<textarea disabled=\"disabled\" ");
				int pos;
				if (parentWebPart != null) {
					foreach (string providerLine in ProductPage.Config (SPContext.Current, "DataProviders").Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
						if (((pos = providerLine.IndexOf (':')) <= 0) || (providerLine.LastIndexOf (',') < pos))
							Report (new Exception (this ["InvalidAdoProviderFormat", providerLine]));
						else
							options += string.Format (FORMAT_LISTOPTION, optVal = providerLine.Substring (pos + 1).Trim (), providerLine.Substring (0, pos), (optVal == Get<string> ("AdoDataProvider")) ? " selected=\"selected\"" : string.Empty);
					panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilterspecial').slideToggle();\" href=\"#noop\">" + this ["FilterProps", GetFilterTypeTitle (GetType ())] + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText;\" id=\"roxfilterspecial\" style=\"display: none;\">"));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (4) ? FORMAT_LIST : formatDisabledList, "AdoDataProvider", "", options));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (4) ? FORMAT_TEXTAREA : formatDisabledTextArea, "AdoConnectionString", Get<string> ("AdoConnectionString")));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (4) ? FORMAT_TEXTBOX : formatDisabledTextBox, "ValueColumnName", Get<string> ("ValueColumnName")));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (4) ? FORMAT_TEXTBOX : formatDisabledTextBox, "DisplayColumnName", Get<string> ("DisplayColumnName")));
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (4) ? FORMAT_TEXTAREA : formatDisabledTextArea, "Query", Get<string> ("Query")));
					options = "";
					for (int i = 0; i < choices.Length; i++)
						options += string.Format (FORMAT_LISTOPTION, key = ((i != 0) ? ((i == 1) ? CHOICE_FIRST : CHOICE_LAST) : CHOICE_EMPTY), this [choices [i]], (key == Get<string> ("SqlChoice")) ? " selected=\"selected\"" : string.Empty);
					panel.Controls.Add (CreateControl (parentWebPart.LicEd (4) ? FORMAT_LIST : formatDisabledList, "SqlChoice", " onchange=\"" + scriptCheckDefault + "\"", options));
					panel.Controls.Add (new LiteralControl ("</fieldset>"));
				}
				base.UpdatePanel (panel);
				if (parentWebPart != null)
					panel.Controls.Add (CreateScript (scriptCheckDefault));
			}

			public override void UpdateProperties (Panel panel) {
				AdoConnectionString = Get<string> ("AdoConnectionString");
				AdoDataProvider = Get<string> ("AdoDataProvider");
				SqlChoice = Get<string> ("SqlChoice");
				DisplayColumnName = Get<string> ("DisplayColumnName");
				Query = Get<string> ("Query");
				ValueColumnName = Get<string> ("ValueColumnName");
				base.UpdateProperties (panel);
			}

			internal IEnumerable<KeyValuePair<int, string>> Items {
				get {
					object val, disp;
					bool cascade = Cascade, postFilter = Get<bool> ("PostFilter"), defaultIfEmpty = Get<bool> ("DefaultIfEmpty");
					KeyValuePair<string [], string []> postFiltered;
					SPView view;
					if (items == null) {
						items = new List<KeyValuePair<int, string []>> ();
						using (IDbConnection conn = CreateConnection ())
						using (IDbCommand cmd = conn.CreateCommand ()) {
							cmd.CommandText = Get<string> ("Query");
							conn.Open ();
							using (IDataReader reader = cmd.ExecuteReader ())
								while (reader.Read ()) {
									val = reader [Get<string> ("ValueColumnName")];
									disp = reader [string.IsNullOrEmpty (Get<string> ("DisplayColumnName")) ? Get<string> ("ValueColumnName") : Get<string> ("DisplayColumnName")];
									items.Add (new KeyValuePair<int, string []> (items.Count + 1, new string [] { (disp == null) ? string.Empty : disp.ToString (), (val == null) ? string.Empty : val.ToString () }));
									if (defaultIfEmpty && (!postFilter) && (!cascade) && (PickerLimit != 0) && (items.Count >= PickerLimit))
										break;
								}
						}
						if (!defaultIfEmpty)
							items.RemoveAll (delegate (KeyValuePair<int, string []> kvp) {
								return string.IsNullOrEmpty (kvp.Value [1]);
							});
						if (postFilter) {
							postFiltered = PostFilterChoices (items.ConvertAll<string> (delegate (KeyValuePair<int, string []> kvp) {
								return kvp.Value [1];
							}).ToArray ());
							items.RemoveAll (delegate (KeyValuePair<int, string []> kvp) {
								return (Array.IndexOf<string> (postFiltered.Value, kvp.Value [1]) >= 0);
							});
						}
						if (cascade && doPostFilterNow) {
							postFiltered = PostFilterChoices (parentWebPart.connectedList, view = ((parentWebPart.connectedView == null) ? parentWebPart.connectedList.DefaultView : parentWebPart.connectedView), Name.StartsWith ("@") ? Name.Substring (1) : Name, items.ConvertAll<string> (delegate (KeyValuePair<int, string []> kvp) {
								return kvp.Value [1];
							}).ToArray (), true);
							items.RemoveAll (delegate (KeyValuePair<int, string []> kvp) {
								return (Array.IndexOf<string> (postFiltered.Value, kvp.Value [1]) >= 0);
							});
						}
					} else if (doPostFilterNow) {
						postFiltered = PostFilterChoices (parentWebPart.connectedList, view = ((parentWebPart.connectedView == null) ? parentWebPart.connectedList.DefaultView : parentWebPart.connectedView), Name.StartsWith ("@") ? Name.Substring (1) : Name, items.ConvertAll<string> (delegate (KeyValuePair<int, string []> kvp) {
							return kvp.Value [1];
						}).ToArray (), true);
						items.RemoveAll (delegate (KeyValuePair<int, string []> kvp) {
							return (Array.IndexOf<string> (postFiltered.Value, kvp.Value [1]) >= 0);
						});
					}
					return items.ConvertAll<KeyValuePair<int, string>> (delegate (KeyValuePair<int, string []> kvp) {
						return new KeyValuePair<int, string> (kvp.Key, kvp.Value [0]);
					});
				}
			}

			protected internal override IEnumerable<string> AllPickableValues {
				get {
					if (items == null)
						Items.ToString ();
					foreach (KeyValuePair<int, string []> item in items)
						yield return item.Value [1];
				}
			}

			protected internal override IEnumerable<KeyValuePair<string, string>> FilterPairs {
				get {
					string itemID = Get<string> ("SqlChoice");
					List<string> vals = GetFilterValues (PREFIX_FIELDNAME + ID, itemID.ToString ());
					string filterValue = string.Empty;
					Converter<string, string> findValueById = delegate (string id) {
						KeyValuePair<int, string []> match = new KeyValuePair<int, string []> ();
						if (CHOICE_EMPTY.Equals (id) || string.IsNullOrEmpty (id))
							return string.Empty;
						if (items.Count > 0)
							match = ((id == CHOICE_FIRST) ? items [0] : ((id == CHOICE_LAST) ? items [items.Count - 1] : items.Find (delegate (KeyValuePair<int, string []> value) {
								return value.Value [1] == id;
							})));
						return (((match.Value != null) && (match.Value.Length >= 2)) ? match.Value [1] : string.Empty);
					};
					if (!Le (4, true))
						throw new Exception (ProductPage.GetResource ("NopeEd", GetFilterTypeTitle (GetType ()), "Ultimate"));
					try {
						if (items == null)
							Items.ToString ();
					} catch (Exception ex) {
						Report (ex);
					}
					foreach (string val in vals) {
						try {
							if (CHOICE_EMPTY.Equals (val))
								filterValue = string.Empty;
							else if (CHOICE_FIRST.Equals (val) || CHOICE_LAST.Equals (val))
								filterValue = findValueById (itemID);
							else if (!string.IsNullOrEmpty (val))
								filterValue = val;
							else if (itemID != CHOICE_EMPTY)
								filterValue = findValueById (itemID);
							else
								filterValue = string.Empty;
						} catch (Exception ex) {
							Report (ex);
						}
						yield return new KeyValuePair<string, string> (Name, CHOICE_EMPTY.Equals (filterValue) ? string.Empty : filterValue);
					}
				}
			}

			public string AdoConnectionString {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? adoConnectionString : string.Empty;
				}
				set {
					adoConnectionString = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

			public string AdoDataProvider {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ?  adoDataProvider:string.Empty;
				}
				set {
					adoDataProvider = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

			public string SqlChoice {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? choice : CHOICE_EMPTY;
				}
				set {
					choice = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? value : CHOICE_EMPTY;
				}
			}

			public override bool DefaultIfEmpty {
				get {
					string defChoice = Get<string> ("SqlChoice");
					return (base.DefaultIfEmpty || string.IsNullOrEmpty (defChoice) || CHOICE_EMPTY.Equals (defChoice));
				}
				set {
					string defChoice = Get<string> ("SqlChoice");
					base.DefaultIfEmpty = (value || string.IsNullOrEmpty (defChoice) || CHOICE_EMPTY.Equals (defChoice));
				}
			}

			public string DisplayColumnName {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? displayColumnName : string.Empty;
				}
				set {
					displayColumnName = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

			public string Query {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? query : string.Empty;
				}
				set {
					query = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

			public string ValueColumnName {
				get {
					return ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? valueColumnName : string.Empty;
				}
				set {
					valueColumnName = ((parentWebPart == null) || parentWebPart.LicEd (reqEd)) ? ProductPage.Trim (value) : string.Empty;
				}
			}

		}

		#endregion

		#region Text Class

		[Serializable]
		internal class Text : Interactive {

			internal bool isCamlSource = false;

			private string defaultValue = string.Empty, defaultValue2 = string.Empty;

			public Text () {
				AutoSuggest = supportAutoSuggest = supportRange = true;
			}

			public Text (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				try {
					supportAutoSuggest = true;
					supportRange = true;
					DefaultValue = info.GetString ("Value");
					DefaultValue2 = info.GetString ("Value2");
				} catch {
				}
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				info.AddValue ("Value", DefaultValue);
				info.AddValue ("Value2", DefaultValue2);
				base.GetObjectData (info, context);
			}

			public override void Render (HtmlTextWriter output, bool isUpperBound) {
				string val = (isUpperBound ? WebPartValue2 : WebPartValue), cid;
				bool autoSuggest = AutoSuggest && (parentWebPart.connectedList != null);
				int limit = 40;
				if ((!int.TryParse (ProductPage.Config (ProductPage.GetContext (), "AutoLimit"), out limit)) || (limit <= 1))
					limit = 40;
				if (IsNumeric)
					val = GetNumeric (val);
				output.Write ("<input class=\"ms-input\" type=\"text\" name=\"{0}\" " + (autoSuggest ? string.Empty : "onkeyup=\"roxOnKey(event, '" + parentWebPart.ID + "');\"") + " id=\"{0}\" value=\"{1}\"{2} " + (autoSuggest ? " autocomplete=\"off\" " : string.Empty) + " />", cid = (PREFIX_FIELDNAME + ID + (isUpperBound ? "2" : string.Empty)), HttpUtility.HtmlEncode (val), HtmlOnChangeAttr);
				if (autoSuggest)
					output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery(document).ready(function() { jQuery('#" + cid + "').autocomplete('" + parentWebPart.connectedList.ParentWeb.Url.TrimEnd ('/') + "/_layouts/roxority_FilterZen/jqas.aspx?f=" + HttpUtility.UrlEncode (Name) + "&fs=" + (string.Empty + MultiValueSeparator) + "&v=" + ((parentWebPart.connectedView == null) ? string.Empty : ProductPage.GuidLower (parentWebPart.connectedView.ID, true)) + "&b=" + ((CamlOperator == (int) (Operator.BeginsWith)) ? 1 : 0) + "&l=" + ProductPage.GuidLower (parentWebPart.connectedList.ID, true) + "&sf=" + HttpUtility.UrlEncode (parentWebPart.AcSecFields) + "', { \"max\": " + limit + ", \"delay\": 100, \"minChars\": 1, \"selectFirst\": false, \"multiple\": " + (string.IsNullOrEmpty (MultiValueSeparator) ? "false" : "true") + ", \"multipleSeparator\": \"" + MultiValueSeparator + "\", \"matchContains\": " + ((CamlOperator != (int) (Operator.Eq)) ? "true" : "false") + " }); }); </script>");
				base.Render (output, isUpperBound);
			}

			public override void UpdatePanel (Panel panel) {
				string formatDisabledCheckBox = FORMAT_CHECKBOX.Replace ("<input ", "<input disabled=\"disabled\" ");
				if (IsRange)
					hiddenProperties.Remove ("DefaultValue2");
				else
					hiddenProperties.Add ("DefaultValue2");
				panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "DefaultValue", Get<string> ("DefaultValue")));
				panel.Controls.Add (CreateControl (FORMAT_TEXTBOX, "DefaultValue2", Get<string> ("DefaultValue2")));
				base.UpdatePanel (panel);
			}

			public override void UpdateProperties (Panel panel) {
				DefaultValue = Get<string> ("DefaultValue");
				DefaultValue2 = Get<string> ("DefaultValue2");
				base.UpdateProperties (panel);
			}

			protected internal override IEnumerable<KeyValuePair<string, string>> FilterPairs {
				get {
					yield return new KeyValuePair<string, string> (Name, WebPartValue);
					if (IsRange)
						yield return new KeyValuePair<string, string> (Name, WebPartValue2);
				}
			}

			public string DefaultValue {
				get {
					return defaultValue;
				}
				set {
					defaultValue = ProductPage.Trim (value);
				}
			}

			public string DefaultValue2 {
				get {
					return defaultValue2;
				}
				set {
					defaultValue2 = ProductPage.Trim (value);
				}
			}

			public override string WebPartValue {
				get {
					return GetFilterValue (PREFIX_FIELDNAME + ID, Get<string> ("DefaultValue"));
				}
			}

			public string WebPartValue2 {
				get {
					return GetFilterValue (PREFIX_FIELDNAME + ID + "2", Get<string> ("DefaultValue2"));
				}
			}

		}

		#endregion

		#region User Class

		[Serializable]
		internal sealed class User : Lookup {

			private SPList userList = null;
			private string userListUrl = null;

			public User () {
				Init ();
				defaultIfEmpty = true;
				itemID = -1;
				stripID = true;
			}

			public User (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				Init ();
			}

			private void Init () {
				hiddenProperties.AddRange (new string [] { "ListUrl" });
				base.ListUrl = UserListUrl;
			}

			internal override int GetPageID (string listUrlParam) {
				return ((SPContext.Current.Web.CurrentUser == null) ? 0 : SPContext.Current.Web.CurrentUser.ID);
			}

			protected internal override object GetValue (string name) {
				return ((name == "ListUrl") ? UserListUrl : base.GetValue (name));
			}

			public override void UpdatePanel (Panel panel) {
				base.UpdatePanel (panel);
				if (string.IsNullOrEmpty (Get<string> ("ValueFieldName")) && string.IsNullOrEmpty (Get<string> ("DisplayFieldName"))) {
					panel.Controls.Add (CreateScript ("function roxUserColumns(disp) { document.getElementById('div_filter_ValueFieldName').style.display = document.getElementById('div_filter_DisplayFieldName').style.display = disp; if (disp == 'block') { (function(){jQuery('#roxfilterspecial').slideDown();})(); document.getElementById('div_filter_ValueFieldName').style.backgroundColor = document.getElementById('div_filter_DisplayFieldName').style.backgroundColor = 'ButtonFace'; location.replace('#roxtooltop'); jQuery('#roxusercols').hide(); } } roxUserColumns('none'); "));
					panel.Controls.Add (new LiteralControl ("<div id=\"roxusercols\" class=\"rox-prop\"><a onclick=\"roxUserColumns('block');\" href=\"#noop\">" + this ["UserFilterColumnsLink"] + "</a></div>"));
				}
			}

			internal SPList UserList {
				get {
					if (userList == null)
						userList = SPContext.Current.Site.GetCatalog (SPListTemplateType.UserInformation);
					return userList;
				}
			}

			internal string UserListUrl {
				get {
					if (userListUrl == null)
						userListUrl = ProductPage.MergeUrlPaths (SPContext.Current.Site.Url, UserList.DefaultViewUrl);
					return userListUrl;
				}
			}

			public override string ListUrl {
				get {
					return UserListUrl;
				}
				set {
					base.ListUrl = UserListUrl;
				}
			}

		}

		#endregion

		#region WssContext Class

		[Serializable]
		internal sealed class WssContext : FilterBase {

			private static readonly string [] contextObjects = { "Site.WebApplication", "Site", "Web", "List", "ViewContext.View", "ListItem", "File" };
			private string contextObject = "Web", contextProperty = "ID";

			public WssContext () {
			}

			public WssContext (SerializationInfo info, StreamingContext context)
				: base (info, context) {
				try {
					ContextObject = info.GetString ("ContextObject");
					ContextProperty = info.GetString ("ContextProperty");
				} catch {
				}
			}

			internal object GetContextObject (string opath) {
				object obj = SPContext.Current, o = null;
				string [] splits = opath.Split (new char [] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				PropertyInfo prop = null;
				foreach (string s in splits) {
					try {
						prop = obj.GetType ().GetProperty (s);
					} catch {
						prop = null;
					}
					if (prop != null)
						try {
							o = prop.GetValue (obj, null);
						} catch {
							o = null;
						}
					if (o == null) {
						obj = null;
						break;
					} else
						obj = o;
				}
				return ((obj == SPContext.Current) ? null : obj);
			}

			internal Type GetContextObjectType (string opath) {
				Type type = typeof (SPContext);
				string [] splits = opath.Split (new char [] { '.' }, StringSplitOptions.RemoveEmptyEntries);
				PropertyInfo prop = null;
				foreach (string s in splits) {
					try {
						prop = type.GetProperty (s);
					} catch {
						prop = null;
					}
					if (prop != null)
						type = prop.PropertyType;
					else
						break;
				}
				return ((type == typeof (SPContext)) ? null : type);
			}

			internal object GetContextPropertyValue (object obj, string prop) {
				SPItem item=obj as SPItem;
				try {
					return (prop.StartsWith ("roxfld__") && (item != null)) ? GetContextPropertyValue (item, ProductPage.GetField (item.Fields, prop.Substring ("roxfld__".Length))) : GetContextPropertyValue (obj, obj.GetType ().GetProperty (prop));
				} catch (Exception ex) {
					return ex;
				}
			}

			internal object GetContextPropertyValue (SPItem obj, SPField prop) {
				object val = null;
				Exception err = null;
				if ((prop != null) && (obj != null))
					try {
						val = obj[prop.InternalName];
					} catch (Exception ex) {
						err = ex;
					}
				return ((err == null) ? val : err);
			}

			internal object GetContextPropertyValue (object obj, PropertyInfo prop) {
				object val = null;
				Exception err = null;
				if ((prop != null) && (obj != null))
					try {
						val = prop.GetValue (obj, null);
					} catch (Exception ex) {
						err = ex;
					}
				if (val != null) {
					if (val is Guid)
						val = val.ToString ();
					else if (val.GetType ().IsGenericParameter || val.GetType ().IsGenericType || val.GetType ().IsGenericTypeDefinition)
						val = new Exception ();
					else if (val.GetType ().IsClass)
						val = (val.ToString ().Equals (val.GetType ().FullName) ? (object) new Exception () : (object) val.ToString ());
				} else if (prop.PropertyType != typeof (string))
					val = new Exception ();
				else
					val = string.Empty;
				return ((err == null) ? val : err);
			}

			public override void GetObjectData (SerializationInfo info, StreamingContext context) {
				info.AddValue ("ContextObject", ContextObject);
				info.AddValue ("ContextProperty", ContextProperty);
				base.GetObjectData (info, context);
			}

			public List<PropertyInfo> GetProperties (Type ctype) {
				ParameterInfo [] args = null;
				List<PropertyInfo> props = new List<PropertyInfo> ();
				foreach (PropertyInfo prop in ctype.GetProperties (BindingFlags.Public | BindingFlags.Instance)) {
					try {
						args = prop.GetIndexParameters ();
					} catch {
					}
					if (((args == null) || (args.Length == 0)) && (!(prop.PropertyType.IsGenericParameter || prop.PropertyType.IsGenericType || prop.PropertyType.IsGenericTypeDefinition)))
						props.Add (prop);
				}
				props.Sort (delegate (PropertyInfo one, PropertyInfo two) {
					return one.Name.CompareTo (two.Name);
				});
				return props;
			}

			public override void UpdatePanel (Panel panel) {
				string options = "", formatDisabledList = FORMAT_LIST.Replace ("<select ", "<select disabled=\"disabled\" ");
				int selIndex = 0;
				bool isSel;
				object cobj, cval;
				SPItem ctxItem;
				Type ctype;
				panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilterspecial').slideToggle();\" href=\"#noop\">" + this ["FilterProps", GetFilterTypeTitle (GetType ())] + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText; display: none;\" id=\"roxfilterspecial\">"));
				for (int i = 0; i < contextObjects.Length; i++) {
					if (isSel = (contextObjects [i] == ContextObject))
						selIndex = i;
					options += string.Format (FORMAT_LISTOPTION, contextObjects [i], this ["ContextObject" + i], isSel ? " selected=\"selected\"" : string.Empty);
				}
				panel.Controls.Add (CreateControl (Le (2, true) ? FORMAT_LIST : formatDisabledList, "ContextObject", " onchange=\"jQuery('.rox-spctxprops').hide(); jQuery('#roxspctxprops_' + this.selectedIndex).show();\"", options));
				for (int i = 0; i < contextObjects.Length; i++) {
					options = string.Empty;
					panel.Controls.Add (new LiteralControl ("<div class=\"rox-spctxprops\" id=\"roxspctxprops_" + i + "\" style=\"display: " + ((i == selIndex) ? "block" : "none") + "\">"));
					if ((ctype = GetContextObjectType (contextObjects [i])) != null) {
						cobj = GetContextObject (contextObjects [i]);
						foreach (PropertyInfo prop in GetProperties (ctype))
							if (!((cval = GetContextPropertyValue (cobj, prop)) is Exception))
								options += string.Format (FORMAT_LISTOPTION, prop.Name, prop.Name + ((cobj == null) ? string.Empty : (" [" + Context.Server.HtmlEncode (JSON.JsonEncode (cval)) + "]")), (prop.Name == ContextProperty) ? " selected=\"selected\"" : string.Empty);
						if (((ctxItem = cobj as SPItem) != null) && (ctxItem.Fields != null))
							foreach (SPField fld in ProductPage.TryEach<SPField> (ctxItem.Fields))
								if (!((cval = GetContextPropertyValue (ctxItem, fld)) is Exception))
									options += string.Format (FORMAT_LISTOPTION, "roxfld__" + fld.InternalName, fld.Title + ((cobj == null) ? string.Empty : (" [" + Context.Server.HtmlEncode (JSON.JsonEncode (cval)) + "]")), (("roxfld__" + fld.InternalName) == ContextProperty) ? " selected=\"selected\"" : string.Empty);
					}
					panel.Controls.Add (CreateControl (Le (2, true) ? FORMAT_LIST : formatDisabledList, "ContextProperty___" + i, string.Empty, options));
					panel.Controls.Add (new LiteralControl ("</div>"));
				}
				panel.Controls.Add (new LiteralControl ("</fieldset>"));
				base.UpdatePanel (panel);
			}

			public override void UpdateProperties (Panel panel) {
				ContextObject = Get<string> ("ContextObject");
				ContextProperty = string.Empty;
				for (int i = 0; i < contextObjects.Length; i++)
					if (contextObjects [i] == ContextObject)
						ContextProperty = Get<string> ("ContextProperty___" + i);
				base.UpdateProperties (panel);
			}

			protected internal override IEnumerable<KeyValuePair<string, string>> FilterPairs {
				get {
					object cobj = GetContextObject (Get<string> ("ContextObject")), cval = ((cobj == null) ? new Exception () : GetContextPropertyValue (cobj, Get<string> ("ContextProperty")));
					if (!Le (2, true))
						throw new Exception (ProductPage.GetResource ("NopeEd", GetFilterTypeTitle (GetType ()), "Basic"));
					if (!(cval is Exception))
						yield return new KeyValuePair<string, string> (Name, cval + string.Empty);
				}
			}

			public string ContextObject {
				get {
					return Le (2, true) ? contextObject : string.Empty;
				}
				set {
					contextObject = Le (2, true) ? ProductPage.Trim (value) : string.Empty;
				}
			}

			public string ContextProperty {
				get {
					return Le (2, true) ? contextProperty : string.Empty;
				}
				set {
					contextProperty = Le (2, true) ? ProductPage.Trim (value) : string.Empty;
				}
			}

		}

		#endregion

		protected internal const string FORMAT_CHECKBOX = "<div id=\"div_{0}\" class=\"rox-prop\"><input onclick=\"{3}\" type=\"checkbox\" name=\"{0}\" id=\"{0}\"{2}/><label id=\"label_{0}\" for=\"{0}\">{1}</label></div>";
		protected internal const string FORMAT_GENERIC_PREFIX = "<div id=\"div_{0}\"><div class=\"rox-prop\"><label id=\"label_{0}\" for=\"{0}\">{1}</label></div>";
		protected internal const string FORMAT_GENERIC_SUFFIX = "</div>";
		protected internal const string FORMAT_LIST = FORMAT_GENERIC_PREFIX + "<select style=\"width: 98%;\" class=\"ms-input\" name=\"{0}\" id=\"{0}\"{2}>{3}</select>" + FORMAT_GENERIC_SUFFIX;
		protected internal const string FORMAT_LISTOPTION = "<option value=\"{0}\"{2}>{1}</option>";
		protected internal const string FORMAT_SCRIPT = "<script type=\"text/javascript\" language=\"JavaScript\">\n{0}\n</script>";
		protected internal const string FORMAT_TEXTAREA = FORMAT_GENERIC_PREFIX + "<textarea rows=\"{3}\" onchange=\"{4}\" style=\"width: 96%;\" class=\"ms-input\" name=\"{0}\" id=\"{0}\">{2}</textarea>" + FORMAT_GENERIC_SUFFIX;
		protected internal const string FORMAT_TEXTBOX = FORMAT_GENERIC_PREFIX + "<input style=\"width: 98%;\" class=\"ms-input\" type=\"text\" name=\"{0}\" id=\"{0}\" value=\"{2}\"{3}/>" + FORMAT_GENERIC_SUFFIX;
		protected internal const string PREFIX_FIELDNAME = "filterval_";

		internal static readonly List<string> invalidTypes = new List<string> ();
		internal static readonly string [] rangeRelevantProperties = { "DefaultValue", "DefaultValue2", "DefaultChoice", "DefaultChoice2" };

		private static readonly List<Type> filterTypes = new List<Type> ();
		private static readonly Dictionary<string, Type> externalFilterTypes = new Dictionary<string, Type> ();

		protected internal roxority_FilterWebPart parentWebPart = null;

		internal readonly List<string> groups = new List<string> (), hiddenProperties = new List<string> ();

		internal bool isEditMode = false, requirePostLoadRendering = false, resolve = true, supportRange = false;
		internal string id = string.Empty;
		internal string [] suppressValues = new string [0];

		private Dictionary<string, int> resolveLevels = new Dictionary<string, int> ();
		private bool enabled = true, sendEmpty = false, suppressIfInactive = false;
		private int camlOperator = 0, suppressMode = 0;
		private string fallbackValue = string.Empty, multiFilterSeparator = string.Empty, multiValueSeparator = string.Empty, name = string.Empty, numberCulture = string.Empty, numberFormat = string.Empty, suppressIfParam = string.Empty, suppressMultiValues = string.Empty;
		private CultureInfo culture = null;

		static FilterBase () {
			filterTypes.Add (typeof (Multi));
			filterTypes.Add (typeof (Choice));
			filterTypes.Add (typeof (Date));
			filterTypes.Add (typeof (Lookup));
			filterTypes.Add (typeof (PageField));
			filterTypes.Add (typeof (RequestParameter));
			filterTypes.Add (typeof (WssContext));
			filterTypes.Add (typeof (Text));
			filterTypes.Add (typeof (User));
			filterTypes.Add (typeof (SqlData));
			filterTypes.Add (typeof (Boolean));
			filterTypes.Add (typeof (CamlSource));
			filterTypes.Add (typeof (CamlDistinct));
			filterTypes.Add (typeof (CamlViewSwitch));
			//filterTypes.Add (typeof (Favorites));
		}

		public static FilterBase Create (string typeName) {
			return Type.GetType (typeName).GetConstructor (Type.EmptyTypes).Invoke (null) as FilterBase;
		}

		public static List<FilterBase> Deserialize (roxority_FilterWebPart webPart, string value) {
			IEnumerable<FilterBase> filters = ProductPage.Deserialize<FilterBase> (value, delegate (FilterBase fb) {
				fb.parentWebPart = webPart;
			});
			return ((filters == null) ? new List<FilterBase> () : new List<FilterBase> (filters));
		}

		public static string GetFilterTypeDesc (Type type) {
			string val = ProductPage.GetProductResource ("FilterDesc_" + type.Name);
			PropertyInfo prop;
			if (string.IsNullOrEmpty (val) && ((prop = type.GetProperty ("FilterTypeDescription", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)) != null))
				try {
					val = prop.GetValue (null, null) as string;
				} catch {
				}
			return string.IsNullOrEmpty (val) ? type.AssemblyQualifiedName : val;
		}

		public static string GetFilterTypeTitle (Type type) {
			string val = ProductPage.GetProductResource ("FilterType_" + type.Name);
			PropertyInfo prop;
			if (string.IsNullOrEmpty (val) && ((prop = type.GetProperty ("FilterTypeTitle", BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)) != null))
				try {
					val = prop.GetValue (null, null) as string;
				} catch {
				}
			return string.IsNullOrEmpty (val) ? type.Name : val;
		}

		public static string Serialize (List<FilterBase> values) {
			return ProductPage.Serialize<FilterBase> (values.ToArray ());
		}

		public static IEnumerable<Type> FilterTypes {
			get {
				List<string> externals = null;
				bool repeat = true;
				Type type;
				try {
					externals = new List<string> (ProductPage.Config (SPContext.Current, "FilterTypes").Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
				} catch {
				}
				foreach (Type ft in filterTypes)
					yield return ft;
				if ((roxority_FilterWebPart.BdcClientUtilType != null) && (roxority_FilterWebPart.BdcFilterType != null))
					yield return roxority_FilterWebPart.BdcFilterType;
				if (externals != null) {
					foreach (string x in externals)
						if (!externalFilterTypes.ContainsKey (x)) {
							if (((type = Type.GetType (x, false, true)) != null) && (!typeof (FilterBase).IsAssignableFrom (type)))
								type = null;
							externalFilterTypes.Add (x, type);
						}
					while (repeat) {
						repeat = false;
						foreach (KeyValuePair<string, Type> ext in externalFilterTypes)
							if (!externals.Contains (ext.Key)) {
								externalFilterTypes.Remove (ext.Key);
								invalidTypes.Remove (ext.Key);
								repeat = true;
								break;
							}
					}
				}
				foreach (KeyValuePair<string, Type> kvp in externalFilterTypes)
					if (kvp.Value != null)
						yield return kvp.Value;
					else if (!invalidTypes.Contains (kvp.Key))
						invalidTypes.Add (kvp.Key);
			}
		}

		public FilterBase () {
			id = Guid.NewGuid ().ToString ();
		}

		public FilterBase (SerializationInfo info, StreamingContext context) {
			try {
				id = info.GetString ("ID");
				Name = info.GetString ("Name");
				Enabled = info.GetBoolean ("Enabled");
				SendEmpty = info.GetBoolean ("SendEmpty");
				FallbackValue = info.GetString ("FallbackValue");
				SuppressMode = info.GetInt32 ("SuppressMode");
				suppressValues = (string []) info.GetValue ("SuppressValues", typeof (string []));
				MultiFilterSeparator = info.GetString ("MultiFilterSeparator");
				MultiValueSeparator = info.GetString ("MultiValueSeparator");
				CamlOperator = info.GetInt32 ("CamlOperator");
				SuppressMultiValues = info.GetString ("SuppressMultiValues");
				Groups = info.GetString ("Groups");
				NumberFormat = info.GetString ("NumberFormat");
				NumberCulture = info.GetString ("NumberCulture");
				SuppressIfInactive = info.GetBoolean ("SuppressIfInactive");
				SuppressIfParam = info.GetString ("SuppressIfParam");
			} catch {
			}
		}

		protected internal LiteralControl CreateControl (string format, bool hidden, params object [] args) {
			int minCount = 0;
			List<object> list = new List<object> (args);
			if (FORMAT_LIST.Equals (format))
				format = format.Replace ("\" for=\"", "\" for=\"nope_");
			for (int i = 0; i < 10; i++)
				if (format.IndexOf ("{" + i + "}") >= 0)
					minCount++;
				else
					break;
			if (list.Count < minCount)
				for (int c = list.Count; c < minCount; c++)
					list.Add (string.Empty);
			if (hidden)
				format = "<span style=\"display: none;\">" + format + "</span>";
			return new LiteralControl (string.Format (format, list.ToArray ()));
		}

		protected internal LiteralControl CreateControl (string format, string name, params object [] args) {
			List<object> list = new List<object> (args);
			Interactive ia = this as Interactive;
			bool isRangeName = (ia != null) && ia.IsRange && (Array.IndexOf<string> (rangeRelevantProperties, name) >= 0);
			string pureName = ((isRangeName && name.EndsWith ("2")) ? name.Substring (0, name.Length - 1) : name);
			list.Insert (0, "filter_" + name);
			if (isRangeName)
				list.Insert (1, (name == pureName) ? this ["Prop_" + name + "Lower"] : this ["Prop_" + pureName + "Upper"]);
			else
				list.Insert (1, this ["Prop_" + (((this is FilterBase.Multi) && "AllowMultiEnter".Equals (name, StringComparison.InvariantCultureIgnoreCase)) ? "AllowMultiCombo" : name)]);
			return CreateControl (format, hiddenProperties.Contains (name), list.ToArray ());
		}

		protected internal LiteralControl CreateScript (string script) {
			return CreateControl (FORMAT_SCRIPT, false, new object [] { script });
		}

		protected internal string GetChecked (bool value) {
			return (value ? " checked=\"checked\"" : string.Empty);
		}

		protected virtual internal object GetValue (string name) {
			int pos = name.IndexOf ("___");
			HttpContext context = Context;
			object myValue = GetType ().GetProperty ((pos > 0) ? name.Substring (0, pos) : name).GetValue (this, null);
			bool isEdit = isEditMode || ((context != null) && ("add".Equals (context.Request ["roxfilteraction"]) || "edit".Equals (context.Request ["roxfilteraction"]))), hasForm = ((context != null) && (Array.IndexOf<string> (context.Request.Form.AllKeys, UrlPropertyPrefix + name) >= 0)), hasQuerySpec = false, hasQuery = (parentWebPart != null) && parentWebPart.UrlSettings && (!isEdit) && ((context != null) && (((hasQuerySpec = (Array.IndexOf<string> (context.Request.QueryString.AllKeys, UrlPropertyPrefix + Name + "_" + name) >= 0)) || (Array.IndexOf<string> (context.Request.QueryString.AllKeys, UrlPropertyPrefix + name) >= 0))));
			string formVal = ((context == null) ? string.Empty : context.Request [(hasQuerySpec && (Array.IndexOf<string> (context.Request.Form.AllKeys, UrlPropertyPrefix + name) < 0)) ? (UrlPropertyPrefix + Name + "_" + name) : (UrlPropertyPrefix + name)]), resolveName, sv = null, rv;
			int intVal, pos1, pos2;
			long longVal;
			Converter<string, string> doResolve = delegate (string v) {
				int rlevel;
				if (!resolveLevels.TryGetValue (v, out rlevel))
					resolveLevels [v] = rlevel = 0;
				if (rlevel < 5) {
					resolveLevels [v] = rlevel + 1;
					return ResolveValue (v);
				}
				return null;
			};
			if (hasQuery || hasQuerySpec)
				new object ();
			if ((!hasQuery) && (!(resolve && (myValue is string))) && ((!isEditMode) || ((context != null) && (("add".Equals (context.Request ["roxfilteraction"])) || ("edit".Equals (context.Request ["roxfilteraction"]))))))
				return myValue;
			if (myValue is bool)
				return (hasQuery ? ((formVal == "1") ? true : ((formVal == "0") ? false : myValue)) : !string.IsNullOrEmpty (formVal));
			if ((hasForm || hasQuery) && (myValue is int))
				return (int.TryParse (formVal, out intVal) || (((pos = formVal.IndexOf (";#", StringComparison.InvariantCultureIgnoreCase)) > 0) && int.TryParse (formVal.Substring (0, pos), out intVal))) ? intVal : myValue;
			if ((hasForm || hasQuery) && (myValue is long))
				return (long.TryParse (formVal, out longVal)) ? longVal : myValue;
			if (resolve && (parentWebPart != null) && (((hasForm || hasQuery) && (!string.IsNullOrEmpty (sv = formVal))) || ((!(hasQuery || hasForm)) && (!string.IsNullOrEmpty (sv = myValue as string)))))
				while (((pos1 = sv.IndexOf ("{$")) >= 0) && ((pos2 = sv.IndexOf ("$}", pos1 + 2)) > (pos1 + 2)))
					sv = sv.Replace ("{$" + (resolveName = sv.Substring (pos1 + 2, pos2 - 2 - pos1)) + "$}", (string.IsNullOrEmpty (resolveName) || (Name.Equals (resolveName) && !parentWebPart.consumedRow.ContainsKey (resolveName))) ? string.Empty : (string.IsNullOrEmpty (rv = doResolve (resolveName)) ? Interactive.CHOICE_EMPTY : rv));
			return ((!string.IsNullOrEmpty (sv)) ? sv.Replace (Interactive.CHOICE_EMPTY, string.Empty) : (((hasForm || hasQuery)) ? formVal : myValue));
		}

		protected virtual internal T Get<T> (string name) {
			object obj = GetValue (name);
			return (((obj is string []) && (typeof (T) == typeof (string))) ? ((T) (object) string.Join ("\r\n", (string []) obj)) : ((T) GetValue (name)));
		}

		protected internal bool Le (int le, bool ifNull) {
			return ((parentWebPart == null) ? ifNull : parentWebPart.LicEd (le));
		}

		protected internal void Report (Exception ex) {
			string msg;
			if (parentWebPart != null) {
				while (ex.InnerException != null)
					ex = ex.InnerException;
				msg = (ex.Message.Contains (" ") ? ex.Message : string.Format (this ["ColumnFailed", ex.Message]));
				foreach (KeyValuePair<FilterBase, Exception> kvp in parentWebPart.warningsErrors)
					if ((kvp.Key.ID == this.ID) && (kvp.Value.Message == msg))
						return;
				parentWebPart.warningsErrors.Add (new KeyValuePair<FilterBase, Exception> (this, (ex.Message == msg) ? ex : new Exception (msg, ex)));
			}
		}

		protected internal string ResolveValue (string name) {
			IEnumerable<KeyValuePair<string, string>> filterPairs;
			List<FilterBase> filters;
			List<string> vals = new List<string> ();
			FilterBase resolvedFilter = null;
			int index;
			string suppMult;
			if (parentWebPart.LicEd (4)) {
				if ((vals.Count == 0) && parentWebPart.consumedRow.ContainsKey (name))
					vals.Add (parentWebPart.consumedRow [name]);
				if (vals.Count == 0) {
					if ((filters = parentWebPart.GetFilters (true, true).FindAll (delegate (FilterBase fb) {
						return fb.Name.Equals (name);
					})) == null)
						filters = new List<FilterBase> ();
					if (filters.Count == 0)
						filters = parentWebPart.GetFilters (true, false).FindAll (delegate (FilterBase fb) {
							return fb.Name.Equals (name, StringComparison.InvariantCultureIgnoreCase);
						});
					if (filters != null)
						foreach (FilterBase fb in filters)
							if ((filterPairs = fb.FilterPairs) != null) {
								resolvedFilter = fb;
								foreach (KeyValuePair<string, string> kvp in filterPairs)
									vals.Add (kvp.Value);
								break;
							}
				}
				if (vals.Count == 1)
					return vals [0];
				else if (vals.Count > 1)
					if ((suppMult = resolvedFilter.Get<string> ("SuppressMultiValues")).StartsWith ("[") && suppMult.EndsWith ("]") && int.TryParse (suppMult.Substring (1, suppMult.Length - 2), out index))
						return vals [(index < 1) ? (vals.Count - 1) : (index - 1)];
					else
						return string.Join (suppMult, vals.ToArray ());
			}
			return WebConfigurationManager.AppSettings[name];
		}

		public virtual void GetObjectData (SerializationInfo info, StreamingContext context) {
			info.AddValue ("CamlOperator", CamlOperator);
			info.AddValue ("FallbackValue", FallbackValue);
			info.AddValue ("ID", ID);
			info.AddValue ("Name", Name);
			info.AddValue ("Enabled", Enabled);
			info.AddValue ("SendEmpty", SendEmpty);
			info.AddValue ("SuppressMode", SuppressMode);
			info.AddValue ("SuppressValues", suppressValues, typeof (string []));
			info.AddValue ("MultiFilterSeparator", MultiFilterSeparator);
			info.AddValue ("MultiValueSeparator", MultiValueSeparator);
			info.AddValue ("SuppressMultiValues", SuppressMultiValues);
			info.AddValue ("Groups", Groups);
			info.AddValue ("NumberFormat", NumberFormat);
			info.AddValue ("NumberCulture", NumberCulture);
			info.AddValue ("SuppressIfInactive", SuppressIfInactive);
			info.AddValue ("SuppressIfParam", SuppressIfParam);
		}

		public void Set (string name, object value) {
			GetType ().GetProperty (name).SetValue (this, value, null);
		}

		public virtual void UpdatePanel (Panel panel) {
			List<string> partGroups = new List<string> ();
			string options = "", formatDisabledTextArea = FORMAT_TEXTAREA.Replace ("<textarea ", "<textarea disabled=\"disabled\" "), formatDisabledTextBox = FORMAT_TEXTBOX.Replace ("<input ", "<input disabled=\"disabled\" "), formatDisabledList = FORMAT_LIST.Replace ("<select ", "<select disabled=\"disabled\" ");
			FilterBase.Interactive ithis = this as FilterBase.Interactive;
			if ((parentWebPart != null) && (parentWebPart.toolPart != null)) {
				panel.Controls.Add (CreateControl (FORMAT_CHECKBOX, "SendEmpty", GetChecked (Get<bool> ("SendEmpty"))));
				panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilteradvanced').slideToggle();\" href=\"#noop\">" + this ["FilterAdvancedProps"] + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText; display: none;\" id=\"roxfilteradvanced\">"));
				options = "";
				for (int i = 0; i <= (supportRange ? 12 : 8); i++)
					options += string.Format (FORMAT_LISTOPTION, i, this ["CamlOp_" + ((CamlOperator) i).ToString ()], (i == Get<int> ("CamlOperator")) ? " selected=\"selected\"" : string.Empty);
				panel.Controls.Add (CreateControl ((parentWebPart.LicEd (4) && parentWebPart.toolPart.camlYesRadioButton.Checked) ? FORMAT_LIST : formatDisabledList, "CamlOperator", " onchange=\"if((" + (IsRange ? "true" : "false") + "&&(this.selectedIndex<=8))||(" + (IsRange ? "false" : "true") + "&&(this.selectedIndex>8)))roxRefreshFilters();\"", options));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_TEXTBOX : formatDisabledTextBox, "FallbackValue", Get<string> ("FallbackValue")));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_TEXTBOX : formatDisabledTextBox, "NumberFormat", Get<string> ("NumberFormat")));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_TEXTBOX : formatDisabledTextBox, "NumberCulture", Get<string> ("NumberCulture")));
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (4) ? FORMAT_TEXTBOX : formatDisabledTextBox, "SuppressMultiValues", Get<string> ("SuppressMultiValues")));
				panel.Controls.Add (CreateControl ((parentWebPart.LicEd (4) && !IsRange) ? FORMAT_TEXTBOX : formatDisabledTextBox, "MultiValueSeparator", Get<string> ("MultiValueSeparator")));
				panel.Controls.Add (CreateControl ((parentWebPart.LicEd (4) && !IsRange) ? FORMAT_TEXTBOX : formatDisabledTextBox, "MultiFilterSeparator", Get<string> ("MultiFilterSeparator")));
				options = "";
				for (int i = 0; i <= 4; i++)
					options += string.Format (FORMAT_LISTOPTION, i, this ["SuppressMode" + i], (i == Get<int> ("SuppressMode")) ? " selected=\"selected\"" : string.Empty);
				panel.Controls.Add (CreateControl (parentWebPart.LicEd (2) ? FORMAT_LIST : formatDisabledList, "SuppressMode", " onchange=\"document.getElementById('div_filter_SuppressValues').style.display=((this.selectedIndex==0)?'none':'block');\"", options));
				panel.Controls.Add (CreateControl (FORMAT_TEXTAREA, "SuppressValues", Get<string> ("SuppressValues"), 4));
				if (Get<int> ("SuppressMode") == 0)
					panel.Controls.Add (CreateScript ("document.getElementById('div_filter_SuppressValues').style.display = 'none';"));
				options = "";
				partGroups = parentWebPart.GetGroups ();
				foreach (string g in partGroups)
					options += string.Format (FORMAT_LISTOPTION, g, g.Replace (roxority_FilterWebPart.sep, ","), groups.Contains (g) ? " selected=\"selected\"" : string.Empty);
				panel.Controls.Add (CreateControl ((parentWebPart.LicEd (4) && (partGroups.Count > 1)) ? FORMAT_LIST : formatDisabledList, "Groups", " multiple=\"multiple\" size=\"" + (partGroups.Count + 1) + "\"", options));
				panel.Controls.Add (CreateControl (FORMAT_CHECKBOX, "SuppressIfInactive", GetChecked (Get<bool> ("SuppressIfInactive"))));
				panel.Controls.Add (CreateControl (FORMAT_TEXTAREA, "SuppressIfParam", Get<string> ("SuppressIfParam")));
				panel.Controls.Add (new LiteralControl ("</fieldset>"));
			}
		}

		public virtual void UpdateProperties (Panel panel) {
			CamlOperator = Get<int> ("CamlOperator");
			FallbackValue = Get<string> ("FallbackValue");
			SendEmpty = Get<bool> ("SendEmpty");
			SuppressMode = Get<int> ("SuppressMode");
			SuppressValues = Get<string> ("SuppressValues");
			MultiFilterSeparator = Get<string> ("MultiFilterSeparator");
			MultiValueSeparator = Get<string> ("MultiValueSeparator");
			SuppressMultiValues = Get<string> ("SuppressMultiValues");
			Groups = string.Join ("\r\n", Get<string> ("Groups").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries));
			NumberCulture = Get<string> ("NumberCulture");
			NumberFormat = Get<string> ("NumberFormat");
			SuppressIfInactive = Get<bool> ("SuppressIfInactive");
			SuppressIfParam = Get<string> ("SuppressIfParam");
		}

		public override string ToString () {
			FilterBase.Interactive ifilter = this as FilterBase.Interactive;
			string label = (((ifilter == null) || string.IsNullOrEmpty (ifilter.Label)) ? string.Empty : ((ifilter.Label.EndsWith (":")) ? ifilter.Label.Substring (0, ifilter.Label.Length - 1) : ifilter.Label));
			return (Enabled ? string.Empty : this ["Disabled"]) + this ["FilterType_" + GetType ().Name] + ": " + (string.IsNullOrEmpty (Name) ? this ["Edit_NoName"] : Name) + (((!string.IsNullOrEmpty (label)) && (label != Name)) ? string.Format (" (\"{0}\")", label) : string.Empty);
		}

		internal HttpContext Context {
			get {
				try {
					return HttpContext.Current;
				} catch {
				}
				return null;
			}
		}

		internal CultureInfo EffectiveNumberCulture {
			get {
				int i;
				string value = Get<string> ("NumberCulture");
				if (culture == null)
					try {
						if (int.TryParse (value, out i))
							culture = new CultureInfo (i);
						else
							culture = new CultureInfo (value);
					} catch {
						culture = null;
					}
				return culture;
			}
		}

		protected internal virtual IEnumerable<KeyValuePair<string, string>> FilterPairs {
			get {
				return null;
			}
		}

		protected internal string this [string resKey, params object [] args] {
			get {
				return ProductPage.GetProductResource (resKey, args);
			}
		}

		protected internal virtual bool SupportsMultipleValues {
			get {
				return true;
			}
		}

		public int CamlOperator {
			get {
				return (((parentWebPart == null) || parentWebPart.LicEd (4)) ? camlOperator : 0);
			}
			set {
				camlOperator = (((parentWebPart == null) || parentWebPart.LicEd (4)) ? value : 0);
			}
		}

		public bool Enabled {
			get {
				return enabled;
			}
			set {
				enabled = value;
			}
		}

		public string FallbackValue {
			get {
				return (((parentWebPart == null) || parentWebPart.LicEd (2)) ? fallbackValue : string.Empty);
			}
			set {
				fallbackValue = (((parentWebPart == null) || parentWebPart.LicEd (2)) ? ((value == null) ? string.Empty : value) : string.Empty);
			}
		}

		public string Groups {
			get {
				return (((parentWebPart == null) || parentWebPart.LicEd (4)) ? string.Join ("\r\n", groups.ConvertAll<string> (delegate (string v) {
					return v.Replace (roxority_FilterWebPart.sep, ",");
				}).ToArray ()) : string.Empty);
			}
			set {
				string [] arr = ProductPage.Trim (value).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				string t;
				groups.Clear ();
				if ((parentWebPart == null) || parentWebPart.LicEd (4))
					foreach (string v in arr)
						if ((!string.IsNullOrEmpty (t = v.Trim ().Replace (",", roxority_FilterWebPart.sep))) && (!groups.Contains (t)))
							groups.Add (t);
			}
		}

		public string ID {
			get {
				return id.ToString ();
			}
		}

		public bool IsMultiValueFilter {
			get {
				return (parentWebPart != null) && ID.Equals (parentWebPart.MultiValueFilterID);
			}
		}

		public bool IsRange {
			get {
				return (Get<int> ("CamlOperator") > 8);
			}
		}

		public string MultiFilterSeparator {
			get {
				return ((((parentWebPart == null) || parentWebPart.LicEd (4)) && !IsRange) ? multiFilterSeparator : string.Empty);
			}
			set {
				multiFilterSeparator = ((((parentWebPart == null) || parentWebPart.LicEd (4)) && !IsRange) ? ((value == null) ? string.Empty : value) : string.Empty);
			}
		}

		public string MultiValueSeparator {
			get {
				return ((((parentWebPart == null) || parentWebPart.LicEd (4)) && !IsRange) ? multiValueSeparator : string.Empty);
			}
			set {
				multiValueSeparator = ((((parentWebPart == null) || parentWebPart.LicEd (4)) && !IsRange) ? ((value == null) ? string.Empty : value) : string.Empty);
			}
		}

		public string Name {
			get {
				return name;
			}
			set {
				name = ProductPage.Trim (value);
			}
		}

		public string NumberFormat {
			get {
				return (((parentWebPart == null) || parentWebPart.LicEd (2)) ? numberFormat : string.Empty);
			}
			set {
				numberFormat = (((parentWebPart == null) || parentWebPart.LicEd (2)) ? (value + string.Empty).Trim () : string.Empty);
			}
		}

		public string NumberCulture {
			get {
				return (((parentWebPart == null) || parentWebPart.LicEd (2)) ? numberCulture : string.Empty);
			}
			set {
				int i;
				numberCulture = null;
				value = ProductPage.Trim (value);
				if ((parentWebPart == null) || parentWebPart.LicEd (2))
					try {
						if (int.TryParse (value, out i))
							new CultureInfo (i);
						else
							new CultureInfo (value);
						numberCulture = value;
					} catch {
						numberCulture = string.Empty;
					} else
					numberCulture = string.Empty;
			}
		}

		public bool SendEmpty {
			get {
				return sendEmpty;
			}
			set {
				sendEmpty = value;
			}
		}

		public bool SuppressIfInactive {
			get {
				return suppressIfInactive;
			}
			set {
				suppressIfInactive = value;
			}
		}

		public string SuppressIfParam {
			get {
				return suppressIfParam;
			}
			set {
				suppressIfParam = (value + string.Empty).Trim ();
			}
		}

		public int SuppressMode {
			get {
				return (((parentWebPart == null) || parentWebPart.LicEd (2)) ? suppressMode : 0);
			}
			set {
				suppressMode = (((parentWebPart == null) || parentWebPart.LicEd (2)) ? (((value < 0) || (value > 4)) ? 0 : value) : 0);
			}
		}

		public string SuppressMultiValues {
			get {
				return (((parentWebPart == null) || parentWebPart.LicEd (4)) ? suppressMultiValues : string.Empty);
			}
			set {
				suppressMultiValues = (((parentWebPart == null) || parentWebPart.LicEd (4)) ? ((value == null) ? string.Empty : value) : string.Empty);
			}
		}

		public string SuppressValues {
			get {
				return string.Join ("\n", suppressValues);
			}
			set {
				List<string> list = new List<string> (string.IsNullOrEmpty (value = ProductPage.Trim (value)) ? new string [0] : value.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
				list = list.ConvertAll<string> (delegate (string val) {
					return ProductPage.Trim (val);
				});
				while (list.IndexOf (string.Empty) >= 0)
					list.Remove (string.Empty);
				ProductPage.RemoveDuplicates<string> (list);
				suppressValues = list.ToArray ();
			}
		}

		public string UrlPropertyPrefix {
			get {
				return ((parentWebPart == null) ? "filter_" : parentWebPart.urlPropertyPrefix);
			}
		}

		public virtual string WebPartValue {
			get {
				return string.Empty;
			}
		}

	}

}
