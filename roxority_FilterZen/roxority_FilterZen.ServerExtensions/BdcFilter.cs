
using Microsoft.Office.Server.ApplicationRegistry.MetadataModel;
using Microsoft.Office.Server.ApplicationRegistry.Runtime;
using Microsoft.SharePoint.Portal.WebControls;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using roxority.SharePoint;
using System;
using System.Collections.Generic;
using System.Data;
using System.Runtime.Serialization;
using System.Web.UI;
using System.Web.UI.WebControls;

using BdcView = Microsoft.Office.Server.ApplicationRegistry.MetadataModel.View;
using CamlOp = roxority.SharePoint.CamlOperator;

namespace roxority_FilterZen.ServerExtensions {

	[Serializable]
	public class BdcFilter : FilterBase.Interactive, IDisposable {

		private const string SEPARATOR = "{C453BF77-8CC4-4e1a-A50E-8A60B293CE94}";

		private static readonly string scriptCheckDefault = SCRIPT_CHECK_DEFAULT.Replace (PLACEHOLDER_LISTID, "BdcInstanceID");

		private string bdcDisplayField = string.Empty, bdcEntity = string.Empty, bdcInstanceID = CHOICE_EMPTY, bdcValueField = string.Empty;
		private bool sendNull = false;
		private LobSystemInstance lobInstance = null;
		private Entity entity = null;
		private BdcView view = null;
		private Field valueField = null, dispField = null;

		public static void Apply (roxority_FilterWebPart filterWebPart, DataFormWebPart bdcListWebPart) {
			BdcDataSource dataSource = bdcListWebPart.DataSource as BdcDataSource;
			BusinessDataParameter param;
			FilterBase filter;
			List<FilterBase> filters = filterWebPart.GetFilters ();
			List<KeyValuePair<string, List<roxority_FilterWebPart.FilterPair>>> filterValues = new List<KeyValuePair<string, List<roxority_FilterWebPart.FilterPair>>> ();
			Dictionary<string, FilterBase> allFilters = new Dictionary<string, FilterBase> ();
			KeyValuePair<string, List<roxority_FilterWebPart.FilterPair>> tmp;
			foreach (FilterBase fb in filters)
				allFilters [fb.Name] = fb;
			foreach (KeyValuePair<string, roxority_FilterWebPart.FilterPair> kvp in filterWebPart.PartFilters) {
				tmp = new KeyValuePair<string, List<roxority_FilterWebPart.FilterPair>> (kvp.Key, new List<roxority_FilterWebPart.FilterPair> ());
				if (filterValues.Count > 0)
					tmp = filterValues.Find (delegate (KeyValuePair<string, List<roxority_FilterWebPart.FilterPair>> test) {
						return test.Key.Equals (kvp.Key);
					});
				if (string.IsNullOrEmpty (tmp.Key) || (kvp.Value == null))
					tmp = new KeyValuePair<string, List<roxority_FilterWebPart.FilterPair>> (kvp.Key, new List<roxority_FilterWebPart.FilterPair> ());
				filterValues.Remove (tmp);
				tmp.Value.Add (kvp.Value);
				filterValues.Add (tmp);
			}
			foreach (KeyValuePair<string, List<roxority_FilterWebPart.FilterPair>> kvp in filterValues)
				foreach (roxority_FilterWebPart.FilterPair pair in kvp.Value) {
					param = new BusinessDataParameter ();
					if (allFilters.TryGetValue (pair.Key, out filter))
						param.ConvertEmptyStringToNull = !filter.SendEmpty;
					param.Name = pair.Key;
					param.DefaultValue = pair.Value;
					param.Direction = ParameterDirection.Input;
					param.Operator = ConvertCamlOperator (pair.CamlOperator);
					dataSource.FilterParameters.Add (param);
				}
		}

		public static bool CanApply (roxority_FilterWebPart filterWebPart, DataFormWebPart bdcListWebPart) {
			return bdcListWebPart.DataSource is BdcDataSource;
		}

		public static FilterOperator ConvertCamlOperator (CamlOp op) {
			if (op == CamlOp.Contains)
				return FilterOperator.Contains;
			else if (op == CamlOp.BeginsWith)
				return FilterOperator.StartsWith;
			else if (op == CamlOp.Neq)
				return FilterOperator.NotEquals;
			else if (op == CamlOp.Gt)
				return FilterOperator.Greater;
			else if (op == CamlOp.Lt)
				return FilterOperator.Less;
			else if (op == CamlOp.Geq)
				return FilterOperator.GreaterEq;
			else if (op == CamlOp.Leq)
				return FilterOperator.LessEq;
			else
				return FilterOperator.Equals;
		}

		public BdcFilter () {
			pickerSemantics = true;
			defaultIfEmpty = true;
			reqEd = 4;
		}

		public BdcFilter (SerializationInfo info, StreamingContext context)
			: base (info, context) {
			reqEd = 4;
			pickerSemantics = true;
			try {
				BdcDisplayField = info.GetString ("BdcDisplayField");
				BdcEntity = info.GetString ("BdcEntity");
				BdcInstanceID = info.GetString ("BdcInstanceID");
				BdcValueField = info.GetString ("BdcValueField");
				SendNull = info.GetBoolean ("SendNull");
				DefaultIfEmpty = info.GetBoolean ("DefaultIfEmpty");
			} catch {
			}
		}

		public void Dispose () {
			try {
				if ((LobInstance != null) && (LobInstance.CurrentConnection != null))
					LobInstance.CloseConnection ();
			} catch {
			}
		}

		public override void GetObjectData (SerializationInfo info, StreamingContext context) {
			info.AddValue ("BdcDisplayField", BdcDisplayField);
			info.AddValue ("BdcEntity", BdcEntity);
			info.AddValue ("BdcInstanceID", BdcInstanceID);
			info.AddValue ("BdcValueField", BdcValueField);
			info.AddValue ("SendNull", SendNull);
			base.GetObjectData (info, context);
		}

		public override void Render (System.Web.UI.HtmlTextWriter output, bool isUpperBound) {
			string options = string.Empty, valueID;
			int index = 0, limit = 150;
			bool checkStyle = Get<bool> ("CheckStyle");
			List<string> filvals = GetFilterValues (PREFIX_FIELDNAME + ID, Get<string> ("BdcInstanceID"));
			if (filvals.Contains (string.Empty) || filvals.Contains (CHOICE_EMPTY))
				filvals.Clear ();
			if ((int.TryParse (ProductPage.Config (null, "PickerLimit"), out limit)) && (limit != 0))
				limit = Math.Abs (limit);
			if (!Le (4, true)) {
				output.WriteLine (ProductPage.GetResource ("NopeEd", GetFilterTypeTitle (GetType ()), "Ultimate"));
				base.Render (output, isUpperBound);
				return;
			}
			try {
				if (Get<bool> ("DefaultIfEmpty")) {
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\"> roxMultiMins['filterval_" + ID + "'] = '" + CHOICE_EMPTY + "'; </script>");
					if (!checkStyle)
						options = string.Format (FORMAT_LISTOPTION, CHOICE_EMPTY, this ["Empty" + (Get<bool> ("SendEmpty") ? "None" : "All")], ((filvals.Count == 0) || filvals.Contains (string.Empty) || filvals.Contains (CHOICE_EMPTY)) ? " selected=\"selected\"" : string.Empty);
					else
						options = string.Format ("<span><input name=\"" + PREFIX_FIELDNAME + ID + "\" type=\"" + (AllowMultiEnter ? "checkbox" : "radio") + "\" id=\"empty_" + PREFIX_FIELDNAME + ID + "\" value=\"{1}\" {3}" + (string.IsNullOrEmpty (HtmlOnChangeAttr) ? (" onclick=\"jQuery(\'.chk-" + ID + "\').attr(\'checked\', false);\"") : HtmlOnChangeAttr.Replace ("onchange=\"", "onclick=\"jQuery('.chk-" + ID + "').attr('checked', false);")) + "/><label for=\"empty_" + PREFIX_FIELDNAME + ID + "\">{2}</label></span>", ProductPage.GuidLower (Guid.NewGuid ()), CHOICE_EMPTY, this ["Empty" + (Get<bool> ("SendEmpty") ? "None" : "All")], ((filvals.Count == 0) || filvals.Contains (string.Empty) || filvals.Contains (CHOICE_EMPTY)) ? " checked=\"checked\"" : string.Empty);
				}
				if ((LobInstance != null) && (Entity != null) && (View != null) && (ValueField != null) && (DisplayField != null))
					using (IEntityInstanceEnumerator values = Entity.FindFiltered (new FilterCollection (), LobInstance))
						while (values.MoveNext ()) {
							if ((values.Current != null) && !string.IsNullOrEmpty (valueID = ProductPage.Serialize<object> (values.Current.GetIdentifierValues ()))) {
								index++;
								if (checkStyle)
									options += string.Format ("<span><input class=\"chk-" + ID + "\" name=\"" + PREFIX_FIELDNAME + ID + "\" type=\"" + (AllowMultiEnter ? "checkbox" : "radio") + "\" id=\"x{0}\" value=\"{1}\" {3}" + ((string.IsNullOrEmpty (HtmlOnChangeAttr) && Get<bool> ("DefaultIfEmpty")) ? (" onclick=\"document.getElementById('empty_" + PREFIX_FIELDNAME + ID + "').checked=false;\"") : HtmlOnChangeAttr.Replace ("onchange=\"", "onclick=\"" + (Get<bool> ("DefaultIfEmpty") ? ("document.getElementById('empty_" + PREFIX_FIELDNAME + ID + "').checked=false;") : string.Empty))) + "/><label for=\"x{0}\">{2}</label></span>", ProductPage.GuidLower (Guid.NewGuid ()), valueID, GetDisplayValue (ProductPage.Trim (values.Current [DisplayField])), filvals.Contains (valueID.ToString ()) ? " checked=\"checked\"" : string.Empty);
								else
									options += string.Format (FORMAT_LISTOPTION, valueID, GetDisplayValue (ProductPage.Trim (values.Current [DisplayField])), filvals.Contains (valueID.ToString ()) ? " selected=\"selected\"" : string.Empty);
							}
							if ((limit != 0) && (index >= limit))
								break;
						}
				if (options.Length > 0)
					if (checkStyle)
						output.Write ("<div>" + options + "</div>");
					else
						output.Write ("<select" + (AllowMultiEnter ? " size=\"1\" multiple=\"multiple\" class=\"rox-multiselect ms-input\"" : " class=\"ms-input\"") + " name=\"{0}\" id=\"{0}\"{1}>" + options + "</select>", PREFIX_FIELDNAME + ID, AllowMultiEnter ? HtmlOnChangeMultiAttr : HtmlOnChangeAttr);
			} catch (Exception ex) {
				Report (ex);
			}
			base.Render (output, isUpperBound);
		}

		public override void UpdatePanel (Panel panel) {
			string baseOptions = string.Format (FORMAT_LISTOPTION, string.Empty, string.Empty, string.Empty), options = baseOptions, tmp;
			bool isSel = false;
			string valueID, formatDisabledTextBox = FORMAT_TEXTBOX.Replace ("<input ", "<input disabled=\"disabled\" "), formatDisabledList = FORMAT_LIST.Replace ("<select ", "<select disabled=\"disabled\" "), formatDisabledCheckBox = FORMAT_CHECKBOX.Replace ("<input ", "<input disabled=\"disabled\" "), formatDisabledTextArea = FORMAT_TEXTAREA.Replace ("<textarea ", "<textarea disabled=\"disabled\" ");
			panel.Controls.Add (new LiteralControl ("<div class=\"roxsectionlink\"><a onclick=\"jQuery('#roxfilterspecial').slideToggle();\" href=\"#noop\">" + this ["FilterProps", GetFilterTypeTitle (GetType ())] + "</a></div><fieldset style=\"padding: 4px; background-color: InfoBackground; color: InfoText;\" id=\"roxfilterspecial\" style=\"display: none;\">"));
			if (parentWebPart != null) {
				try {
					try {
						foreach (KeyValuePair<string, LobSystemInstance> kvpLob in ApplicationRegistry.GetLobSystemInstances ())
							try {
								foreach (KeyValuePair<string, Entity> kvpEntity in kvpLob.Value.GetEntities ())
									try {
										if ((kvpEntity.Value.GetIdentifierCount () == 1) && kvpEntity.Value.HasSpecificFinder ()) {
											options += string.Format (FORMAT_LISTOPTION, tmp = kvpLob.Key + SEPARATOR + kvpEntity.Key, (kvpLob.Value.ContainsLocalizedDisplayName () ? kvpLob.Value.GetLocalizedDisplayName () : kvpLob.Value.GetDefaultDisplayName ()) + ": " + (kvpEntity.Value.ContainsLocalizedDisplayName () ? kvpEntity.Value.GetLocalizedDisplayName () : kvpEntity.Value.GetDefaultDisplayName ()), (isSel = tmp.Equals (Get<string> ("BdcEntity"))) ? " selected=\"selected\"" : string.Empty);
											if (isSel) {
												lobInstance = kvpLob.Value;
												entity = kvpEntity.Value;
												view = entity.GetSpecificFinderView ();
											}
										}
									} catch (Exception ex) {
										Report (ex);
									}
							} catch (Exception ex) {
								Report (ex);
							}
					} catch (Exception ex) {
						Report (ex);
					}
					panel.Controls.Add (CreateControl (Le (4, false) ? FORMAT_LIST : formatDisabledList, "BdcEntity", " onchange=\"roxRefreshFilters();\"", options));
					options = baseOptions;
					if (view != null)
						try {
							foreach (Field field in view.Fields)
								try {
									options += string.Format (FORMAT_LISTOPTION, field.Name, field.ContainsLocalizedDisplayName ? field.LocalizedDisplayName : field.DefaultDisplayName, (isSel = field.Name.Equals (Get<string> ("BdcValueField"))) ? " selected=\"selected\"" : string.Empty);
									if (isSel)
										valueField = field;
								} catch (Exception ex) {
									Report (ex);
								}
						} catch (Exception ex) {
							Report (ex);
						}
					panel.Controls.Add (CreateControl (Le (4, false) ? FORMAT_LIST : formatDisabledList, "BdcValueField", " onchange=\"roxRefreshFilters();\"", options));
					options = baseOptions;
					if (view != null)
						try {
							foreach (Field field in view.Fields)
								try {
									options += string.Format (FORMAT_LISTOPTION, field.Name, field.ContainsLocalizedDisplayName ? field.LocalizedDisplayName : field.DefaultDisplayName, (isSel = field.Name.Equals (Get<string> ("BdcDisplayField"))) ? " selected=\"selected\"" : string.Empty);
									if (isSel)
										dispField = field;
								} catch (Exception ex) {
									Report (ex);
								}
						} catch (Exception ex) {
							Report (ex);
						}
					panel.Controls.Add (CreateControl (Le (4, false) ? FORMAT_LIST : formatDisabledList, "BdcDisplayField", " onchange=\"roxRefreshFilters();\"", options));
					options = string.Format (FORMAT_LISTOPTION, string.Empty, this ["Empty"], string.Empty);
					if ((entity != null) && (view != null)) {
						if (valueField == null)
							valueField = view.Fields [0];
						if (valueField != null) {
							if (dispField == null)
								dispField = valueField;
							using (IEntityInstanceEnumerator values = entity.FindFiltered (new FilterCollection (), lobInstance))
								while (values.MoveNext ())
									if ((values.Current != null) && !string.IsNullOrEmpty (valueID = ProductPage.Serialize<object> (values.Current.GetIdentifierValues ())))
										options += string.Format (FORMAT_LISTOPTION, valueID, values.Current [dispField], valueID.ToString ().Equals (Get<string> ("BdcInstanceID")) ? " selected=\"selected\"" : string.Empty);
						}
					}
					panel.Controls.Add (CreateControl (Le (4, false) ? FORMAT_LIST : formatDisabledList, "BdcInstanceID", " onchange=\"" + scriptCheckDefault + "\"", options));
				} catch (Exception ex) {
					Report (ex);
				} finally {
					try {
						if ((lobInstance != null) && (lobInstance.CurrentConnection != null))
							lobInstance.CloseConnection ();
					} catch (Exception ex) {
						Report (ex);
					}
				}
			}
			panel.Controls.Add (new LiteralControl ("</fieldset>"));
			try {
				panel.Controls.Add (CreateControl (Le (4, false) ? FORMAT_CHECKBOX : formatDisabledCheckBox, "SendNull", GetChecked (Get<bool> ("SendNull"))));
				base.UpdatePanel (panel);
				panel.Controls.Add (CreateScript (scriptCheckDefault));
			} catch (Exception ex) {
				Report (ex);
			}
		}

		public override void UpdateProperties (Panel panel) {
			BdcDisplayField = Get<string> ("BdcDisplayField");
			BdcEntity = Get<string> ("BdcEntity");
			BdcInstanceID = Get<string> ("BdcInstanceID");
			BdcValueField = Get<string> ("BdcValueField");
			SendNull = Get<bool> ("SendNull");
			base.UpdateProperties (panel);
		}

		protected override IEnumerable<string> AllPickableValues {
			get {
				object val;
				if ((LobInstance != null) && (Entity != null) && (View != null) && (ValueField != null))
					using (IEntityInstanceEnumerator values = Entity.FindFiltered (new FilterCollection (), LobInstance))
						while (values.MoveNext ())
							if ((values.Current != null) && ((val = values.Current [ValueField]) != null))
								yield return val.ToString ();
			}
		}

		protected override IEnumerable<KeyValuePair<string, string>> FilterPairs {
			get {
				IEnumerable<IEntityInstance> instances = null;
				Field valueField = null;
				object val = null;
				KeyValuePair<string, string> resultKvp;
				if (!roxority_BusinessDataItemBuilderWebPart.IsEd)
					throw new Exception (ProductPage.GetResource ("NopeEd", GetFilterTypeTitle (GetType ()), "Ultimate"));
				else {
					try {
						valueField = ValueField;
						instances = EntityInstances;
					} catch (Exception ex) {
						Report (ex);
					}
					if (instances != null)
						foreach (IEntityInstance inst in instances) {
							resultKvp = new KeyValuePair<string, string> (string.Empty, string.Empty);
							try {
								if ((inst == null) && SendNull)
									resultKvp = new KeyValuePair<string, string> (Name, string.Empty);
								else if ((inst != null) && (valueField != null))
									resultKvp = new KeyValuePair<string, string> (Name, ((val = inst [valueField]) == null) ? string.Empty : val.ToString ());
							} catch (Exception ex) {
								Report (ex);
							}
							if (!string.IsNullOrEmpty (resultKvp.Key))
								yield return (CHOICE_EMPTY.Equals (resultKvp.Value) ? new KeyValuePair<string, string> (resultKvp.Key, string.Empty) : resultKvp);
						}
				}
			}
		}

		public string BdcDisplayField {
			get {
				return Le (4, true) ? bdcDisplayField : string.Empty;
			}
			set {
				bdcDisplayField = Le (4, true) ? ProductPage.Trim (value) : string.Empty;
			}
		}

		public string BdcEntity {
			get {
				return Le (4, true) ? bdcEntity : string.Empty;
			}
			set {
				bdcEntity = Le (4, true) ? ProductPage.Trim (value) : string.Empty;
			}
		}

		public string BdcInstanceID {
			get {
				return Le (4, true) ? bdcInstanceID : string.Empty;
			}
			set {
				bdcInstanceID = Le (4, true) ? ProductPage.Trim (value) : string.Empty;
			}
		}

		public string BdcValueField {
			get {
				return Le (4, true) ? bdcValueField : string.Empty;
			}
			set {
				bdcValueField = Le (4, true) ? ProductPage.Trim (value) : string.Empty;
			}
		}

		public override bool DefaultIfEmpty {
			get {
				string defChoice = Get<string> ("BdcInstanceID");
				return (base.DefaultIfEmpty || string.IsNullOrEmpty (defChoice) || CHOICE_EMPTY.Equals (defChoice));
			}
			set {
				string defChoice = Get<string> ("BdcInstanceID");
				base.DefaultIfEmpty = (value || string.IsNullOrEmpty (defChoice) || CHOICE_EMPTY.Equals (defChoice));
			}
		}

		public Field DisplayField {
			get {
				string bdcDisplayField = Get<string> ("BdcDisplayField");
				if ((dispField == null) && (View != null))
					try {
						if (string.IsNullOrEmpty (bdcDisplayField))
							dispField = ValueField;
						else
							foreach (Field f in View.Fields)
								if (bdcDisplayField.Equals (f.Name)) {
									dispField = f;
									break;
								}
						if (dispField == null)
							throw new Exception (this ["BdcFailed", bdcDisplayField]);
					} catch {
						throw new Exception (this ["BdcFailed", bdcDisplayField]);
					}
				return dispField;
			}
		}

		public Entity Entity {
			get {
				string bdcEntity = Get<string> ("BdcEntity");
				int pos = ((bdcEntity == null) ? -1 : bdcEntity.IndexOf (SEPARATOR));
				if ((entity == null) && (LobInstance != null) && (pos > 0))
					try {
						if ((entity = lobInstance.GetEntities () [bdcEntity.Substring (pos + SEPARATOR.Length)]) == null)
							throw new Exception (this ["BdcFailed", bdcEntity]);
					} catch {
						throw new Exception (this ["BdcFailed", bdcEntity]);
					}
				return entity;
			}
		}

		public IEnumerable<IEntityInstance> EntityInstances {
			get {
				List<string> bdcInstanceIDs = GetFilterValues (PREFIX_FIELDNAME + ID, Get<string> ("BdcInstanceID"));
				IEnumerable<object> idValsEnum;
				IEntityInstance inst;
				foreach (string bdcInstanceID in bdcInstanceIDs)
					if ((!string.IsNullOrEmpty (bdcInstanceID)) && (!CHOICE_EMPTY.Equals (bdcInstanceID)) && ((idValsEnum = ProductPage.Deserialize<object> (bdcInstanceID, null)) != null))
						foreach (object idVal in idValsEnum) {
							inst = null;
							try {
								inst = Entity.FindSpecific (idVal, LobInstance);
							} catch (Exception ex) {
								Report (ex);
							}
							if (inst != null)
								yield return inst;
						}
			}
		}

		public LobSystemInstance LobInstance {
			get {
				string bdcEntity = Get<string> ("BdcEntity");
				int pos = ((bdcEntity == null) ? -1 : bdcEntity.IndexOf (SEPARATOR));
				if ((lobInstance == null) && (pos > 0))
					try {
						if ((lobInstance = ApplicationRegistry.GetLobSystemInstanceByName (bdcEntity.Substring (0, pos))) == null)
							throw new Exception (this ["BdcFailed", bdcEntity]);
					} catch {
						throw new Exception (this ["BdcFailed", bdcEntity]);
					}
				return lobInstance;
			}
		}

		public bool SendNull {
			get {
				return Le(4,true)&& sendNull;
			}
			set {
				sendNull = Le (4, true) && value;
			}
		}

		public Field ValueField {
			get {
				string bdcValueField = Get<string> ("BdcValueField");
				if ((valueField == null) && (View != null))
					try {
						foreach (Field f in View.Fields)
							if (string.IsNullOrEmpty (bdcValueField) || bdcValueField.Equals (f.Name)) {
								valueField = f;
								break;
							}
						if (valueField == null)
							throw new Exception (this ["BdcFailed", bdcValueField]);
					} catch {
						throw new Exception (this ["BdcFailed", bdcValueField]);
					}
				return valueField;
			}
		}

		public BdcView View {
			get {
				if ((view == null) && (Entity != null))
					view = Entity.GetSpecificFinderView ();
				return view;
			}
		}

	}

}
