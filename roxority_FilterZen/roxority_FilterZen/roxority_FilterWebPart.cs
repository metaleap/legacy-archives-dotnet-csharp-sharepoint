
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.WebPartPages.Communication;
using roxority.Shared;
using roxority.Shared.ComponentModel;
using roxority.Shared.Xml;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml;
using System.Xml.Serialization;

using SystemWebPart = System.Web.UI.WebControls.WebParts.WebPart;
using WssWebPart = Microsoft.SharePoint.WebPartPages.WebPart;

namespace roxority_FilterZen {

	[Guid ("cdbec42f-2b0c-4c1c-8984-1a38f425e8f5")]
	public class roxority_FilterWebPart : WebPartBase, ICellProvider, ICellConsumer, IRowConsumer, IRowProvider, IFilterProvider, IWebPartParameters {

		#region FilterPair Class

		public class FilterPair {

			internal bool nextAnd = true;
			private CamlOperator camlOperator;
			private string key, value;

			public FilterPair (string key, string value, int camlOperator)
				: this (key, value, (CamlOperator) camlOperator) {
			}

			public FilterPair (string key, string value, CamlOperator camlOperator) {
				Key = key;
				Value = value;
				CamlOperator = camlOperator;
			}

			public override bool Equals (object obj) {
				FilterPair fp = obj as FilterPair;
				return ((fp != null) && fp.CamlOperator.Equals (CamlOperator) && fp.Key.Equals (Key) && fp.Value.Equals (Value));
			}

			public override int GetHashCode () {
				return CamlOperator.GetHashCode () ^ Key.GetHashCode () ^ Value.GetHashCode () ^ nextAnd.GetHashCode ();
			}

			public CamlOperator CamlOperator {
				get {
					return camlOperator;
				}
				set {
					camlOperator = value;
				}
			}

			public string Key {
				get {
					return key;
				}
				set {
					key = ((value == null) ? string.Empty : value);
				}
			}

			public string Value {
				get {
					return value;
				}
				set {
					this.value = ((value == null) ? string.Empty : value);
				}
			}

		}

		#endregion

		#region Transform Class

		public class Transform : ITransformableFilterValues {

			#region Provider Class

			public class Provider : ProviderConnectionPoint {

				private readonly string cleanDisplayName, originalDisplayName;

				public Provider (MethodInfo callbackMethod, Type interfaceType, Type controlType, string displayName, string id, bool allowsMultipleConnections)
					: base (callbackMethod, interfaceType, controlType, ProductPage.GetProductResource (displayName).Replace ("'{0}'-", "").Replace ("'{0}'", ""), id, allowsMultipleConnections) {
					cleanDisplayName = DisplayName;
					originalDisplayName = displayName;
				}

				public override bool GetEnabled (Control control) {
					roxority_FilterWebPart part = control as roxority_FilterWebPart;
					FilterBase filter = null;
					bool enabled = ((part != null) && ((filter = part.GetFilters (false, false).Find (delegate (FilterBase value) {
						return (value.Enabled && value.SupportsMultipleValues && (string.IsNullOrEmpty (part.MultiValueFilterID) || value.ID.Equals (part.MultiValueFilterID)));
					})) != null));
					try {
						typeof (ConnectionPoint).GetField ("_displayName", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic).SetValue (this, enabled ? ProductPage.GetProductResource (part.HasPeople ? (originalDisplayName + "Alt") : originalDisplayName, filter.Name) : cleanDisplayName);
					} catch {
					}
					if (enabled)
						part._connected = true;
					return enabled;
				}

				public string Description {
					get {
						return ProductPage.GetProductResource ("TransformDesc");
					}
				}

			}

			#endregion

			public readonly FilterBase Filter;
			public readonly roxority_FilterWebPart WebPart;

			public Transform (roxority_FilterWebPart webPart, FilterBase filter) {
				WebPart = webPart;
				Filter = filter;
			}

			public bool AllowAllValue {
				get {
					return !Filter.Get<bool> ("SendEmpty");
				}
			}

			public bool AllowEmptyValue {
				get {
					return Filter.Get<bool> ("SendEmpty");
				}
			}

			public bool AllowMultipleValues {
				get {
					return Filter.SupportsMultipleValues && string.IsNullOrEmpty (Filter.SuppressMultiValues);
				}
			}

			public string ParameterName {
				get {
					return Filter.Name;
				}
			}

			public ReadOnlyCollection<string> ParameterValues {
				get {
					List<string> vals = new List<string> ();
					if (!WebPart.CamlFilters) {
						foreach (KeyValuePair<string, FilterPair> kvp in WebPart.PartFilters)
							if (kvp.Key == ParameterName)
								vals.Add (kvp.Value.Value);
						if (vals.Count > 0) {
							ProductPage.RemoveDuplicates<string> (vals);
							WebPart.eventOrderLog.Add (WebPart ["LogSent", WebPart ["Transformed", WebPart.FiltersList.Find (delegate (FilterBase fb) {
								return (fb.Enabled && fb.SupportsMultipleValues && (string.IsNullOrEmpty (WebPart.MultiValueFilterID) || WebPart.MultiValueFilterID.Equals (fb.ID)));
							}).Name]]);
						}
					}
					return ((vals.Count == 0) ? null : new ReadOnlyCollection<string> (vals));
				}
			}

		}

		#endregion

		internal static readonly string sep = "{DB02F8DE-30FE-47c3-BFE8-8E5BD525989B}";
		internal static readonly string xsltTypeName = "Microsoft.SharePoint.WebPartPages.XsltListViewWebPart";

		private static Type bdcClientUtilType = null, bdcFilterType = null;
		private static MethodInfo bdcFilterApplyMethod = null, bdcFilterCanApplyMethod = null;

		public event CellConsumerInitEventHandler CellConsumerInit;
		public event CellProviderInitEventHandler CellProviderInit;
		public event CellReadyEventHandler CellReady;
		public event ClearFilterEventHandler ClearFilter;
		public event SetFilterEventHandler SetFilter;
		public event NoFilterEventHandler NoFilter;
		public event RowProviderInitEventHandler RowProviderInit;
		public event RowReadyEventHandler RowReady;

		public TextBox MultiTextBox = new TextBox ();
		public Panel MultiPanel = new Panel ();
		public DateTimeControl [] DatePickers = new DateTimeControl [6];
		public PeopleEditor [] PeoplePickers = new PeopleEditor [6];

		protected internal readonly List<KeyValuePair<FilterBase, Exception>> warningsErrors = new List<KeyValuePair<FilterBase, Exception>> ();
		public readonly List<string> additionalWarningsErrors = new List<string> ();
		protected internal readonly Dictionary<string, string> consumedRow = new Dictionary<string, string> ();

		internal readonly List<SystemWebPart> connectedParts = new List<SystemWebPart> (), appliedParts = new List<SystemWebPart> ();
		internal readonly List<KeyValuePair<string, string>> filtersNotSent = new List<KeyValuePair<string, string>> (), validFilterNames = new List<KeyValuePair<string, string>> ();
		internal readonly List<string> eventOrderLog = new List<string> (), groups = new List<string> ();
		internal readonly Dictionary<SystemWebPart, Action<SystemWebPart>> deferredActions = new Dictionary<SystemWebPart, Action<SystemWebPart>> ();

		internal SPList connectedList = null;
		internal SPView connectedView = null;
		internal Transform transform = null;
		internal bool _cellConnected = false, ajax14hide = false, ajax14focus = true, applyToolbarStylings = !ProductPage.Is14, autoConnect = false, autoRepost = false, embedFilters = false, camlFiltered = false, camlFilters = false, cascaded = false, cascadeLtr = false, debugMode = false, defaultToOr = false, disableFilters = true, disableFiltersSome = true, errorMode = true, forceReload = false, highlight = false, _connected = false, _rowConnected = false, recollapseGroups = false, rememberFilterValues = false, respectFilters = true, searchBehaviour = false, showClearButtons = false, suppressSpacing = false, suppressUnknownFilters = false, urlParams = false, extraHide = false;
		private string generatedQuery = string.Empty, listViewUrl = null;
		internal FilterToolPart toolPart = null;
		internal int ajax14Interval = 0;

		private List<KeyValuePair<string, string>> debugFilters = new List<KeyValuePair<string, string>> ();
		private List<KeyValuePair<string, FilterPair>> partFilters = null;
		private List<DataFormWebPart> connectedDataParts = new List<DataFormWebPart> ();
		private List<FilterBase> filters = null, dynamicFilters = null;
		private List<Guid> listViews = new List<Guid> ();
		private string camlFiltersAndCombined = string.Empty, cellFieldName = "FilterGroup", folderScope = string.Empty, group = string.Empty, htmlEmbed = null, jsonFilters = string.Empty, multiValueFilterID = string.Empty, serializedFilters = string.Empty, finalJson = null;
		private Exception regError = null;
		private bool firstSkipped = true, isRowConsumer = false;
		private int htmlMode = 2, maxFiltersPerRow = 0, nullParts = 0, multiWidth = 240;
		private string [] acSecFields = null, cell = null, cellNames = new string [] { }, dataPartIDs = new string [0];
		private DataTable rowTable = null;
		private FilterBase cellFilter = null;
		private RowProviderInitEventArgs rowArgs = null;
		private CellReadyEventArgs cellArgs = null;
		private bool? isViewPage, hasDate, hasMulti, hasPeople;
		private SystemWebPart viewPart = null;
		private List<IDisposable> disps = new List<IDisposable> ();
		private Action deferredFilterAction1 = null, deferredFilterAction2 = null;

		public static Type BdcClientUtilType {
			get {
				if (bdcClientUtilType == null)
					try {
						bdcClientUtilType = Type.GetType ((ProductPage.Is14 ? "Microsoft.SharePoint.BdcClientUtil, Microsoft.SharePoint, Version=14.0.0.0" : "Microsoft.SharePoint.Portal.WebControls.BdcClientUtil, Microsoft.SharePoint.Portal, Version=12.0.0.0") + ", Culture=neutral, PublicKeyToken=71e9bce111e9429c", true, true);
					} catch {
					}
				return bdcClientUtilType;
			}
		}

		public static MethodInfo BdcFilterApplyMethod {
			get {
				if ((bdcFilterApplyMethod == null) && (BdcFilterType != null))
					bdcFilterApplyMethod = bdcFilterType.GetMethod ("Apply", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new Type [] { typeof (roxority_FilterWebPart), typeof (DataFormWebPart) }, null);
				return bdcFilterApplyMethod;
			}
		}

		public static MethodInfo BdcFilterCanApplyMethod {
			get {
				if ((bdcFilterCanApplyMethod == null) && (BdcFilterType != null))
					bdcFilterCanApplyMethod = bdcFilterType.GetMethod ("CanApply", BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly, null, new Type [] { typeof (roxority_FilterWebPart), typeof (DataFormWebPart) }, null);
				return bdcFilterCanApplyMethod;
			}
		}

		public static Type BdcFilterType {
			get {
				if (bdcFilterType == null)
					try {
						bdcFilterType = Type.GetType ("roxority_FilterZen.ServerExtensions.BdcFilter, roxority_FilterZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01", false, true);
					} catch {
					}
				return bdcFilterType;
			}
		}

		public roxority_FilterWebPart () {
			ExportMode = WebPartExportMode.All;
			ChromeType = PartChromeType.None;
			urlPropertyPrefix = "filter_";
		}

		void ICellProvider.CellConsumerInit (object sender, CellConsumerInitEventArgs cellConsumerInitEventArgs) {
			if ((cellConsumerInitEventArgs != null) && !string.IsNullOrEmpty (cellConsumerInitEventArgs.FieldName)) {
				cellNames = new string [] { cellConsumerInitEventArgs.FieldName, cellConsumerInitEventArgs.FieldDisplayName };
				if (!validFilterNames.Exists (delegate (KeyValuePair<string, string> value) {
					return value.Key.Equals (cellConsumerInitEventArgs.FieldName);
				}))
					validFilterNames.Add (new KeyValuePair<string, string> (cellConsumerInitEventArgs.FieldName, string.IsNullOrEmpty (cellConsumerInitEventArgs.FieldDisplayName) ? cellConsumerInitEventArgs.FieldName : cellConsumerInitEventArgs.FieldDisplayName));
			}
		}

		void ICellConsumer.CellProviderInit (object sender, CellProviderInitEventArgs cellProviderInitArgs) {
			cell = new string [] { cellProviderInitArgs.FieldName, cellProviderInitArgs.FieldDisplayName };
		}

		void ICellConsumer.CellReady (object sender, CellReadyEventArgs cellReadyArgs) {
			eventOrderLog.Add (this ["LogReceived", EffectiveCellFieldName, SelectedGroup = ((cellReadyArgs.Cell == null) ? string.Empty : cellReadyArgs.Cell.ToString ())]);
			partFilters = null;
		}

		void IFilterProvider.FilterConsumerInit (object sender, FilterConsumerInitEventArgs filterConsumerInitEventArgs) {
			string [] fieldList = null, fieldDisplayList = null;
			if (filterConsumerInitEventArgs != null) {
				fieldList = filterConsumerInitEventArgs.FieldList;
				fieldDisplayList = filterConsumerInitEventArgs.FieldDisplayList;
			}
			if ((fieldList != null) && (fieldList.Length > 0)) {
				for (int i = 0; i < fieldList.Length; i++)
					validFilterNames.Add (new KeyValuePair<string, string> (fieldList [i], ((fieldDisplayList != null) && (fieldDisplayList.Length > i)) ? fieldDisplayList [i] : string.Empty));
				ProductPage.RemoveDuplicates<KeyValuePair<string, string>> (validFilterNames);
			}
		}

		void IRowConsumer.RowProviderInit (object sender, RowProviderInitEventArgs e) {
			consumedRow.Clear ();
		}

		void IRowConsumer.RowReady (object sender, RowReadyEventArgs e) {
			string rowVal;
			if ((e != null) && (e.Rows != null))
				foreach (DataRow row in e.Rows) {
					foreach (DataColumn col in row.Table.Columns)
						if (!consumedRow.ContainsKey (col.ColumnName)) {
							rowVal = LicEd (4) ? (row [col.ColumnName] + string.Empty) : ProductPage.GetResource ("NopeEd", "{" + row [col.ColumnName] + string.Empty + "} via " + this ["GetValuesFrom"], "Ultimate");
							eventOrderLog.Add (this ["LogReceived", col.ColumnName, rowVal]);
							consumedRow [col.ColumnName] = rowVal;
						}
					break;
				}
			if (deferredFilterAction1 != null) {
				if (CamlFilters && !camlFiltered) {
					camlFiltered = true;
					if (!Exed) {
						Apply<ListViewWebPart> (GetConnectedParts<ListViewWebPart> (), true);
						Apply<DataFormWebPart> (GetConnectedParts<DataFormWebPart> (), true);
					}
				}
				deferredFilterAction1 ();
				deferredFilterAction1 = null;
				if (deferredFilterAction2 != null) {
					deferredFilterAction2 ();
					deferredFilterAction2 = null;
				}
			}
		}

		internal void AddViewFields (XmlDocument doc, SPList list, Hashtable ht) {
			string fn;
			SPField field;
			ht ["_list"] = list;
			foreach (XmlNode node in doc.DocumentElement.SelectNodes ("/View/ViewFields/FieldRef"))
				if ((field = ProductPage.GetField (list, fn = XmlUtil.GetAttributeValue (node, "Name", string.Empty))) != null)
					ht [fn] = field.Title;
		}

		internal void AddViewFields (XmlDocument doc, SPList list, string [] viewFields) {
			XmlNode node, fieldsNode = doc.DocumentElement.SelectSingleNode ("/View/ViewFields");
			if (fieldsNode != null)
				foreach (string vf in viewFields)
					if ((node = fieldsNode.SelectSingleNode ("FieldRef[@Name='" + vf + "']")) == null)
						fieldsNode.AppendChild (doc.CreateElement ("FieldRef")).Attributes.SetNamedItem (doc.CreateAttribute ("Name")).Value = vf;
		}

		internal void Apply (DataFormWebPart webPart, bool is14, Hashtable listAndViewFields) {
#if !SP12
			webPart.InitialAsyncDataFetch = false;
#endif
			string appKey, nuView = string.Empty, oldView = string.Empty;
			object appVal, ccc = Reflector.Current.Get (webPart, "ChildControlsCreated");
			bool expandGroups = RecollapseGroups; // xsltTypeName.Equals (webPart.GetType ().FullName) || "roxority_FilterZen.XsltListViewWebPart".Equals (webPart.GetType ().FullName);
			bool? created = ((ccc is bool) ? ((bool?) (bool) ccc) : null);
			SPDataSource dataSource = ((is14) ? null : webPart.DataSource as SPDataSource);
			SPList list;
			XmlDocument doc = new XmlDocument ();
			Hashtable urlFilters = new Hashtable ();
			HttpContext context = Context;
			Guid viewGuid, urlViewID;
			SPView nuSPView = null;
			if (DisableFilters && !DisableFiltersSome)
				Reflector.Current.Set (webPart, "_disableColumnFiltering", true);
			if (is14) {
				try {
					oldView = (string) webPart.GetType ().GetProperty ("XmlDefinition").GetValue (webPart, null);
				} catch {
				}
				if (string.IsNullOrEmpty (oldView)) {
					is14 = false;
					dataSource = webPart.DataSource as SPDataSource;
				}
			}
			if ((dataSource == null) && !is14) {
				if ((BdcFilterCanApplyMethod != null) && (BdcFilterApplyMethod != null) && ((bool) BdcFilterCanApplyMethod.Invoke (null, new object [] { this, webPart }))) {
					BdcFilterApplyMethod.Invoke (null, new object [] { this, webPart });
					eventOrderLog.Add (this ["LogCaml", webPart.GetType ().Name + ": &quot;" + webPart.EffectiveTitle + "&quot;", GeneratedQuery, (string.IsNullOrEmpty (GeneratedQuery) && string.IsNullOrEmpty (finalJson)) ? "none" : "inline", finalJson]);
				} else if (webPart.GetType ().GetProperty ("CustomQuery", BindingFlags.Public | BindingFlags.Instance) != null)
					Reflector.Current.Set (webPart, "CustomQuery", Apply (null, Reflector.Current.Get (webPart, "QueryOverride") + string.Empty, FolderScope, ref expandGroups, (((webPart.FilterValues == null) || (webPart.FilterValues.Collection == null) || (webPart.FilterValues.Collection.Count == 0)) ? urlFilters : webPart.FilterValues.Collection), null, null, ref nuSPView));
				else
					additionalWarningsErrors.Add (this ["DataFormNoCaml", webPart.DisplayTitle]);
			} else {
				if (RespectFilters && (context != null) && (!string.IsNullOrEmpty (context.Request ["View"])) && ((!string.IsNullOrEmpty (oldView)) || (dataSource != null)) && !string.IsNullOrEmpty (context.Request ["FilterField1"])) {
					doc.LoadXml (string.IsNullOrEmpty (oldView) ? dataSource.SelectCommand : oldView);
					if ((viewGuid = ProductPage.GetGuid (XmlUtil.GetAttributeValue (doc.DocumentElement, "Name", string.Empty))).Equals (urlViewID = ProductPage.GetGuid (context.Request ["View"])))
						for (int i = 1; i <= int.MaxValue; i++)
							if (string.IsNullOrEmpty (context.Request ["FilterField" + i]))
								break;
							else
								urlFilters [context.Request ["FilterField" + i]] = context.Request ["FilterValue" + i] + string.Empty;
				}
				doc.LoadXml (nuView = Apply (list = is14 ? (Reflector.Current.Get (webPart, "SPList") as SPList) : dataSource.List, is14 ? oldView : dataSource.SelectCommand, FolderScope, ref expandGroups, (((webPart.FilterValues == null) || (webPart.FilterValues.Collection == null) || (webPart.FilterValues.Collection.Count == 0)) ? urlFilters : webPart.FilterValues.Collection), Reflector.Current.Get (webPart, "ViewID"), null, ref nuSPView));
				//AddViewFields (doc, list, listAndViewFields);
				eventOrderLog.Add (this ["LogCaml", webPart.GetType ().Name + ": &quot;" + webPart.EffectiveTitle + "&quot;", GeneratedQuery, (string.IsNullOrEmpty (GeneratedQuery) && string.IsNullOrEmpty (finalJson)) ? "none" : "inline", finalJson]);
				if (((appVal = Context.Application [appKey = webPart.ID]) == null) || ((is14 ? oldView : dataSource.SelectCommand) != appVal.ToString ()) || (is14 ? oldView : dataSource.SelectCommand).Equals (Context.Application ["orig_" + appKey])) {
					Context.Application ["orig_" + appKey] = (is14 ? oldView : dataSource.SelectCommand);
					Context.Application [appKey] = nuView;
					if (!is14)
						dataSource.SelectCommand = nuView;
				} else if ((is14 ? oldView : dataSource.SelectCommand) != nuView) {
					Context.Application [appKey] = nuView;
					if (!is14)
						dataSource.SelectCommand = nuView;
				}
				if (!is14) {
					//if (nuSPView != null) {
					//    if (ProductPage.Is14 && (nuSPView != null)) {
					//        webPart.ViewContentTypeId = nuSPView.ContentTypeId.ToString ();
					//        Reflector.Current.Set (webPart, "view", nuSPView);
					//    }
					//}
					dataSource.DataBind ();
				} else
					try {
						foreach (Control ctl in webPart.Controls)
							if (ctl is Literal)
								((Literal) ctl).Text = "";
						//Reflector.Current.Call (webPart, "ResetXslCache");
						//Reflector.Current.Set (webPart, "_partContent", null);
						Reflector.Current.Set (webPart, "_viewXml", null);
						Reflector.Current.Set (webPart, "XmlDefinition", nuView);
						//Reflector.Current.Set (webPart, "_deferredXSLTBecauseOfConnections", true);
						//Reflector.Current.Set (webPart, "deferXsltTransform", true);
						//Reflector.Current.Set (webPart, "_requiresDataBinding", true);
						//Reflector.Current.Set (webPart, "_bypassXSLCache", true);
						//Reflector.Current.Set (webPart, "_dataTable", null);
						//Reflector.Current.Set (webPart, "_dataNavigator", null);
						Reflector.Current.Set (webPart, "_singleDataSource", null);
						Reflector.Current.Set (webPart, "_dataSource", null);
						Reflector.Current.Set (webPart, "_schemaXml", nuView);
						//Reflector.Current.Set (webPart, "view", null);
						//Reflector.Current.Set (webPart, "_hasOverrideSelectCommand", true);
						//Reflector.Current.Set (webPart, "ChildControlsCreated", false);
						//Reflector.Current.Set (webPart, "RequiresDataBinding", true);
						//Hashtable ht = new Hashtable ();
						//foreach (FieldInfo fi in webPart.GetType ().GetFields (BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
						//    ht [fi.Name] = fi.GetValue (webPart);
						if (ProductPage.Is14) {
							//Reflector.Current.Call (webPart, "ForceDataBind", new Type [] { typeof (bool) }, new object [] { true });
						} else {
							Reflector.Current.Call (webPart, "ForceTransformRerun", null, null);
							webPart.DataBind ();
							foreach (Control ctl in webPart.Controls)
								if (ctl is Literal)
									try {
										((Literal) ctl).Text.ToString ();
									} catch {
									}
						}
					} catch {
					}
				if ((!is14) && (webPart.GetType ().FullName != xsltTypeName) && webPart.FireInitialRow && (DesignMode || WebPartManager.DisplayMode.AllowPageDesign))
					additionalWarningsErrors.Add (this ["CamlDataFormFail", webPart.EffectiveTitle]);
				if (!appliedParts.Contains (webPart))
					appliedParts.Add (webPart);
				if (deferredActions.ContainsKey (webPart)) {
					deferredActions [webPart] (webPart);
					extraHide = true;
				}
			}
		}

		internal bool Apply (ListViewWebPart webPart, Hashtable listAndViewFields) {
			bool expandGroups = CamlFilters && RecollapseGroups;
			string lvx;
			SPList list = null;
			XmlDocument doc = new XmlDocument ();
			Hashtable jsonFilters = new Hashtable (), urlFilters = new Hashtable ();
			SPWeb web;
			HttpContext context = Context;
			Guid viewGuid = ProductPage.GetGuid (webPart.ViewGuid), urlViewID;
			SPView nuView = null;
			Reflector refl = new Reflector (typeof (ListViewWebPart).Assembly);
			ViewType nuViewType = ViewType.None;
			if (RespectFilters && (context != null) && (viewGuid != Guid.Empty) && (viewGuid == (urlViewID = ProductPage.GetGuid (context.Request ["View"]))))
				for (int i = 1; i <= int.MaxValue; i++)
					if (string.IsNullOrEmpty (context.Request ["FilterField" + i]))
						break;
					else
						urlFilters [context.Request ["FilterField" + i]] = context.Request ["FilterValue" + i] + string.Empty;
			disps.Add (web = SPContext.Current.Site.OpenWeb (Guid.Empty.Equals (webPart.WebId) ? SPContext.Current.Web.ID : webPart.WebId));
			try {
				list = web.Lists [new Guid (webPart.ListName)];
			} catch {
				try {
					list = web.Lists [webPart.ListName];
				} catch (Exception ex) {
					additionalWarningsErrors.Add (ex.ToString ());
				}
			}
			doc.LoadXml (lvx = Apply (list, webPart.ListViewXml, FolderScope, ref expandGroups, urlFilters, webPart.ViewGuid, null, ref nuView));
			AddViewFields (doc, list, listAndViewFields);
			if (nuView != null) {
				webPart.ViewGuid = ProductPage.GuidBracedUpper (nuView.ID);
				try {
					nuViewType = (ViewType) Enum.Parse (typeof (ViewType), nuView.Type, true);
				} catch {
				}
				if (nuViewType != ViewType.None)
					webPart.ViewType = nuViewType;
				refl.Set (webPart, "view", nuView);
			}
			try {
				webPart.ListViewXml = lvx;
			} catch (XmlException xex) {
				if (xex.Message.Contains ("EntityName")) {
					foreach (XmlNode node in doc.SelectNodes ("*"))
						foreach (XmlAttribute att in node.Attributes)
							att.Value = att.Value.Replace ("&", "&amp;");
					webPart.ListViewXml = doc.DocumentElement.OuterXml;
				} else
					throw;
			}
			eventOrderLog.Add (this ["LogCaml", webPart.GetType ().Name + ": &quot;" + webPart.EffectiveTitle + "&quot;", GeneratedQuery, (string.IsNullOrEmpty (GeneratedQuery) && string.IsNullOrEmpty (finalJson)) ? "none" : "inline", finalJson]);
			return expandGroups;
		}

		internal string Apply (SPList list, string viewXml, string folderScope, ref bool expandGroups, Hashtable moreFilters, object altViewID, string [] addViewFields, ref SPView nuView) {
			string result, cdVal, customJson = string.Empty, json;
			int jc = 1;
			object cjs;
			ArrayList flist = new ArrayList (), al;
			Hashtable ht, ht2, ht3, jsonFilters = null, custJson = null;
			List<KeyValuePair<string, List<FilterPair>>> filterValues = new List<KeyValuePair<string, List<FilterPair>>> ();
			KeyValuePair<string, List<FilterPair>> tmp;
			List<string> camlAndFilters = EffectiveAndFilters, addJsonFilters = new List<string> ();
			Guid viewID;
			XmlDocument doc = new XmlDocument (), doc2 = new XmlDocument ();
			XmlNode whereNode;
			XmlAttribute att;
			List<FilterBase.CamlDistinct> cdFilters = new List<FilterBase.CamlDistinct> ();
			List<int> cdItemIDs, cdItemIDs2;
			List<string> messages = new List<string> ();
			Dictionary<string, Dictionary<string, List<int>>> cdMaps;
			Dictionary<string, Dictionary<string, int>> cdIDs;
			Dictionary<int, Dictionary<string, string>> cdIDMaps;
			SPQuery cdQuery;
			FilterBase.Multi multi = null;
			connectedList = list;
			if (list != null)
				foreach (SPField fld in ProductPage.TryEach<SPField> (list.Fields))
					if (!validFilterNames.Exists ((kvp) => {
						return kvp.Key == fld.InternalName;
					}))
						validFilterNames.Add (new KeyValuePair<string, string> (fld.InternalName, fld.Title));
			foreach (FilterBase f in GetFilters (false, false, true))
				if (f.Enabled)
					if (f is FilterBase.CamlDistinct)
						cdFilters.Add (f as FilterBase.CamlDistinct);
					else if (f is FilterBase.Multi)
						customJson = (multi = f as FilterBase.Multi).GetJson ();
					else
						addJsonFilters.Add (f.Name);
			if (cdFilters.Count > 0) {
				cdIDs = new Dictionary<string, Dictionary<string, int>> ();
				cdMaps = new Dictionary<string, Dictionary<string, List<int>>> ();
				cdIDMaps = new Dictionary<int, Dictionary<string, string>> ();
				cdItemIDs = new List<int> ();
				cdItemIDs2 = new List<int> ();
				cdQuery = new SPQuery ();
				cdQuery.AutoHyperlink = cdQuery.ExpandRecurrence = cdQuery.ExpandUserField = cdQuery.IncludeAllUserPermissions = cdQuery.IncludeAttachmentUrls = cdQuery.IncludeAttachmentVersion = cdQuery.IncludeMandatoryColumns = cdQuery.IncludePermissions = cdQuery.IndividualProperties = cdQuery.ItemIdQuery = false;
				cdQuery.ViewAttributes = "FailIfEmpty=\"FALSE\" RequiresClientIntegration=\"FALSE\" Threaded=\"FALSE\" Scope=\"Recursive\"";
				cdQuery.ViewFields = "<FieldRef Name=\"ID\"/>";
				foreach (FilterBase.CamlDistinct cdf in cdFilters)
					if (!cdMaps.ContainsKey (cdf.Name)) {
						cdIDs [cdf.Name] = new Dictionary<string, int> ();
						cdMaps [cdf.Name] = new Dictionary<string, List<int>> ();
						cdQuery.ViewFields += "<FieldRef Name=\"" + cdf.Name + "\"/>";
					}
				foreach (SPListItem rec in ProductPage.TryEach<SPListItem> (list.GetItems (cdQuery))) {
					cdItemIDs.Add (rec.ID);
					cdIDMaps [rec.ID] = new Dictionary<string, string> ();
					foreach (KeyValuePair<string, Dictionary<string, List<int>>> kvp in cdMaps) {
						try {
							cdVal = rec [rec.Fields.GetFieldByInternalName (kvp.Key).Id] + string.Empty;
						} catch {
							cdVal = string.Empty;
						}
						cdIDMaps [rec.ID] [kvp.Key] = cdVal;
						if (!kvp.Value.ContainsKey (cdVal))
							kvp.Value [cdVal] = new List<int> ();
						kvp.Value [cdVal].Add (rec.ID);
					}
				}
				cdItemIDs.Sort (delegate (int id1, int id2) {
					int res = cdIDMaps [id2].Count.CompareTo (cdIDMaps [id1].Count);
					return ((res == 0) ? id1.CompareTo (id2) : res);
				});
				foreach (int itemID in cdItemIDs)
					foreach (KeyValuePair<string, string> cdPair in cdIDMaps [itemID])
						if (!cdIDs [cdPair.Key].ContainsKey (cdPair.Value)) {
							if (!cdItemIDs2.Contains (itemID))
								cdItemIDs2.Add (itemID);
							cdIDs [cdPair.Key] [cdPair.Value] = itemID;
						}
				if (cdItemIDs2.Count > 0) {
					flist.Add (ht = new Hashtable ());
					ht ["k"] = "ID";
					ht ["v"] = ht2 = new Hashtable ();
					ht2 ["v"] = false;
					ht2 ["k"] = al = new ArrayList ();
					foreach (int itemID in cdItemIDs2) {
						al.Add (ht3 = new Hashtable ());
						ht3 ["k"] = itemID.ToString ();
						ht3 ["v"] = "Eq";
					}
				}
			}
			if (moreFilters != null)
				foreach (DictionaryEntry entry in moreFilters) {
					tmp = new KeyValuePair<string, List<FilterPair>> (entry.Key.ToString (), new List<FilterPair> ());
					if (filterValues.Count > 0)
						tmp = filterValues.Find (delegate (KeyValuePair<string, List<FilterPair>> test) {
							return test.Key.Equals (entry.Key.ToString ());
						});
					if (string.IsNullOrEmpty (tmp.Key) || (tmp.Value == null))
						tmp = new KeyValuePair<string, List<FilterPair>> (entry.Key.ToString (), new List<FilterPair> ());
					filterValues.Remove (tmp);
					tmp.Value.Add (new FilterPair (entry.Key.ToString (), entry.Value + string.Empty, CamlOperator.Eq));
					filterValues.Add (tmp);
				}
			foreach (KeyValuePair<string, FilterPair> kvp in PartFilters) {
				tmp = new KeyValuePair<string, List<FilterPair>> (kvp.Key, new List<FilterPair> ());
				if (filterValues.Count > 0)
					tmp = filterValues.Find (delegate (KeyValuePair<string, List<FilterPair>> test) {
						return test.Key.Equals (kvp.Key);
					});
				if (string.IsNullOrEmpty (tmp.Key) || (kvp.Value == null))
					tmp = new KeyValuePair<string, List<FilterPair>> (kvp.Key, new List<FilterPair> ());
				filterValues.Remove (tmp);
				tmp.Value.Add (kvp.Value);
				filterValues.Add (tmp);
			}
			foreach (KeyValuePair<string, List<FilterPair>> kvp in filterValues) {
				flist.Add (ht = new Hashtable ());
				ht ["k"] = kvp.Key;
				ht ["v"] = ht2 = new Hashtable ();
				ht2 ["v"] = camlAndFilters.Contains (kvp.Key);
				ht2 ["k"] = al = new ArrayList ();
				foreach (FilterPair fp in kvp.Value) {
					al.Add (ht3 = new Hashtable ());
					ht3 ["k"] = fp.Value;
					ht3 ["v"] = GetOperator (fp).ToString ();
				}
			}
			if (!string.IsNullOrEmpty (ListViewUrl))
				foreach (SPView view in ProductPage.TryEach<SPView> (list.Views))
					if (ListViewUrl.Equals (view.Url, StringComparison.InvariantCultureIgnoreCase)) {
						nuView = view;
						altViewID = viewID = view.ID;
						connectedView = view;
						if (string.IsNullOrEmpty (viewXml))
							viewXml = view.SchemaXml;
						else {
							doc.LoadXml (view.SchemaXml);
							doc2.LoadXml (viewXml);
							doc2.DocumentElement.InnerXml = doc.DocumentElement.InnerXml;
							foreach (XmlAttribute attNode in doc.DocumentElement.Attributes)
								if ((att = doc2.DocumentElement.Attributes.GetNamedItem (attNode.LocalName, attNode.NamespaceURI) as XmlAttribute) != null)
									att.Value = attNode.Value;
								else if ("Scope".Equals (attNode.LocalName, StringComparison.InvariantCultureIgnoreCase)) {
									att = doc2.DocumentElement.Attributes.Append (doc2.CreateAttribute (attNode.Prefix, attNode.LocalName, attNode.NamespaceURI));
									att.Value = attNode.Value;
								}
							viewXml = doc2.DocumentElement.OuterXml;
						}
						break;
					}
			if (!string.IsNullOrEmpty (viewXml)) {
				doc.LoadXml (viewXml);
				if ((!string.IsNullOrEmpty (folderScope)) && ((flist.Count > 0) || !ProductPage.Config<bool> (ProductPage.GetContext (), "NoFoldersAlt"))) {
					doc.DocumentElement.SetAttribute ("Scope", folderScope);
					viewXml = doc.DocumentElement.OuterXml;
				}
				if (((connectedView == null)) && !Guid.Empty.Equals (viewID = ProductPage.GetGuid ((XmlUtil.Attribute (doc.DocumentElement, "Name", altViewID + string.Empty) == null) ? Guid.Empty.ToString () : XmlUtil.Attribute (doc.DocumentElement, "Name", altViewID + string.Empty))))
					foreach (SPView view in list.Views)
						if (view.ID.Equals (viewID)) {
							connectedView = view;
							break;
						}
			}
			if (string.IsNullOrEmpty (json = JsonFilters))
				if (addJsonFilters.Count == 0)
					json = customJson;
				else if (multi != null) {
					json = "{\"" + (DefaultToOr ? "OR" : "AND") + "\":[";
					for (int i = 0; i < addJsonFilters.Count; i++) {
						json += ("\"" + addJsonFilters [i] + "\",");
						if (i < (addJsonFilters.Count - 1)) {
							jc++;
							json += ("{\"" + (DefaultToOr ? "OR" : "AND") + "\":[");
						}
					}
					json += ("\"" + multi.Name + "\"");
					for (int i = 0; i < jc; i++)
						json += "]}";
				}
			if ((!string.IsNullOrEmpty (json)) && ((json != customJson) || json.StartsWith ("{"))) {
				if ((json != customJson) && !string.IsNullOrEmpty (customJson))
					try {
						cjs = JSON.JsonDecode (customJson);
						if (cjs is string)
							json = json.Replace ("\"" + multi.Name + "\"", "\"" + cjs + "\"");
						else
							custJson = cjs as Hashtable;
					} catch (Exception ex) {
						messages.Add (this ["JsonError"] + ex.Message);
					}
				try {
					if ((jsonFilters = JSON.JsonDecode (json) as Hashtable) == null)
						throw new Exception (this ["JsonSyntax"]);
				} catch (Exception ex) {
					messages.Add (this ["JsonError"] + ex.Message);
				}
				if (custJson != null)
					if (jsonFilters == null)
						jsonFilters = custJson;
					else
						try {
							MergeJson (jsonFilters, multi.Name, custJson);
						} catch (Exception ex) {
							messages.Add (ex.Message);
						}
				if (jsonFilters != null)
					ValidateJsonFilters (jsonFilters, flist, messages);
				if (messages.Count > 0)
					additionalWarningsErrors.Add (this ["JsonIssues"] + "<ul><li>" + string.Join ("</li><li>", messages.ConvertAll<string> (delegate (string v) {
						return HttpUtility.HtmlEncode (v);
					}).ToArray ()) + "</li></ul>");
			}
			finalJson = JSON.JsonEncode (jsonFilters);
			result = ProductPage.ApplyCore (list, viewXml, doc, flist, ref expandGroups, DefaultToOr, (messages.Count == 0) ? jsonFilters : null, CamlSourceFilters);
			if (DisableFilters && !DisableFiltersSome)
				result = result.Replace ("]]></HTML><GetVar Name=\"FilterDisable\" HTMLEncode=\"TRUE\" /><HTML><![CDATA[", "TRUE");
			if (!string.IsNullOrEmpty (result))
				doc.LoadXml (result);
			else if (list == null)
				doc.RemoveAll ();
			if (string.IsNullOrEmpty (ListViewUrl) && (addViewFields != null) && (addViewFields.Length > 0))
				AddViewFields (doc, list, addViewFields);
			result = ((doc.DocumentElement == null) ? string.Empty : doc.DocumentElement.OuterXml);
			if ((doc.DocumentElement != null) && ((whereNode = doc.DocumentElement.SelectSingleNode ("Query/Where")) != null))
				GeneratedQuery = HttpUtility.HtmlEncode (whereNode.OuterXml);
			foreach (KeyValuePair<string, List<FilterPair>> kvp in filterValues)
				if (kvp.Value == null)
					debugFilters.Add (new KeyValuePair<string, string> (kvp.Key, null));
				else
					foreach (FilterPair fp in kvp.Value)
						debugFilters.Add (new KeyValuePair<string, string> (fp.Key, fp.Value));
			return result;
		}

		internal void MergeJson (Hashtable ht, string name, Hashtable custJson) {
			ArrayList al;
			foreach (DictionaryEntry entry in ht) {
				al = entry.Value as ArrayList;
				if (name.Equals (al [1]))
					al [1] = custJson;
				else if (name.Equals (al [0])) {
					if (al [1] is string) {
						al [0] = al [1];
						al [1] = custJson;
					} else
						throw new Exception (this ["JsonCraze", FilterBase.GetFilterTypeTitle (typeof (FilterBase.Multi)), name]);
						//	{ "AND": [ "moo", { "OR": [ "f1", "f2" ] } ] }
				} else if (al [1] is Hashtable)
					MergeJson (al [1] as Hashtable, name, custJson);
			}
		}

		internal bool ValidateJsonFilters (Hashtable ht, ArrayList flist, List<string> messages) {
			string key, val;
			bool found;
			ArrayList list;
			Hashtable ht2;
			if (ht.Count > 1)
				messages.Add (this ["JsonOne"]);
			else
				foreach (DictionaryEntry entry in ht) {
					if (((key = entry.Key as string) == null) || (!("OR".Equals (key) || "AND".Equals (key))))
						messages.Add (this ["JsonOperator", entry.Key]);
					if (((list = entry.Value as ArrayList) == null) || (list.Count != 2))
						messages.Add (this ["JsonOperands", key]);
					else
						for (int i = 0; i < 2; i++)
							if (((ht2 = list [i] as Hashtable) == null) & ((val = list [i] as string) == null))
								messages.Add (this ["JsonOperandTypes", key]);
							else if (ht2 != null)
								ValidateJsonFilters (ht2, flist, messages);
							else if ((val != null) && (val != "*") && (!FiltersList.Exists (delegate (FilterBase test) {
								return val.Equals (test.Name);
							})) && !(found = ((connectedList != null) && (ProductPage.GetField (connectedList, val) != null)))) {
								foreach (Hashtable ht3 in flist)
									if (val.Equals (ht3 ["k"] + string.Empty, StringComparison.InvariantCultureIgnoreCase)) {
										found = true;
										break;
									}
								if (!found)
									messages.Add (this ["JsonUnknown"] + val);
							}
				}
			return (messages.Count == 0);
		}

		internal void Apply<T> (IEnumerable<T> webParts, bool isDfwp14) {
			DataFormWebPart dp;
			ListViewWebPart lp;
			if (webParts != null)
				foreach (T wp in webParts)
					if ((lp = wp as ListViewWebPart) != null) {
						if (Apply (lp, new Hashtable ()))
							Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery(document).ready(function() { roxCollapseGroups('" + lp.ID + "'); }); </script>"));
					} else if ((dp = wp as DataFormWebPart) != null)
						Apply (dp, isDfwp14, new Hashtable ());
		}

		internal FilterBase.Interactive CreateDynamicInteractiveFilter (string name, bool isLookupFilter) {
			foreach (KeyValuePair<string, string> kvp in validFilterNames)
				if (kvp.Key == name)
					return CreateDynamicInteractiveFilter (kvp, false, isLookupFilter);
			return null;
		}

		internal FilterBase.Interactive CreateDynamicInteractiveFilter (KeyValuePair<string, string> kvp) {
			return CreateDynamicInteractiveFilter (kvp, true, false);
		}

		internal FilterBase.Interactive CreateDynamicInteractiveFilter (KeyValuePair<string, string> kvp, bool dynID, bool isLookupFilter) {
			bool doTry2;
			FilterBase.Boolean boolFilter = null;
			FilterBase.Interactive dynFilter = null;
			FilterBase.Choice choiceFilter = null;
			FilterBase.Date dateFilter = null;
			FilterBase.Lookup lookupFilter = null;
			FilterBase.User userFilter = null;
			ListViewWebPart listPart, theListPart = null;
			DataFormWebPart dataPart;
			SPField field = null;
			SPFieldMultiChoice choiceField = null;
			SPFieldBoolean boolField = null;
			SPFieldDateTime dateField = null;
			SPFieldUser userField = null;
			SPFieldLookup lookupField = null;
			SPList spList = null;
			SPView spView = null;
			Guid viewID = Guid.Empty;
			SPDataSource dataSource;
			SPWrap<SPList> wrap = null;
			foreach (SystemWebPart wp in connectedParts)
				if (((dataPart = wp as DataFormWebPart) != null) && ((dataSource = dataPart.DataSource as SPDataSource) != null))
					try {
						wrap = new SPWrap<SPList> (SPContext.Current.Site, SPContext.Current.Web, delegate (SPWeb web) {
							return dataSource.List;
						});
						if ((spList = wrap.Value) != null) {
							field = ProductPage.GetField (wrap.Value, kvp.Key);
							break;
						}
					} catch {
					} else if ((listPart = wp as ListViewWebPart) != null)
					try {
						wrap = new SPWrap<SPList> (SPContext.Current.Site, SPContext.Current.Web, delegate (SPWeb web) {
							return web.Lists [new Guid (listPart.ListName)];
						});
						if ((spList = wrap.Value) != null) {
							field = ProductPage.GetField (wrap.Value, kvp.Key);
							theListPart = listPart;
							break;
						}
					} catch {
					}
			if ((connectedList == null) && (spList != null))
				connectedList = spList;
			else if ((spList == null) && (connectedList != null))
				spList = connectedList;
			if ((spList == null) && (connectedList == null) && isLookupFilter)
				return null;
			if (isLookupFilter)
				dynFilter = lookupFilter = new FilterBase.Lookup ();
			else if ((boolField = field as SPFieldBoolean) != null)
				dynFilter = boolFilter = new FilterBase.Boolean ();
			else if ((choiceField = field as SPFieldMultiChoice) != null)
				dynFilter = choiceFilter = new FilterBase.Choice ();
			else if ((dateField = field as SPFieldDateTime) != null)
				dynFilter = dateFilter = new FilterBase.Date ();
			else if ((userField = field as SPFieldUser) != null)
				dynFilter = userFilter = new FilterBase.User ();
			else if ((lookupField = field as SPFieldLookup) != null)
				dynFilter = lookupFilter = new FilterBase.Lookup ();
			if (dynFilter == null)
				dynFilter = new FilterBase.Text ();
			if (dynID)
				dynFilter.id = ID + kvp.Key;
			dynFilter.parentWebPart = this;
			dynFilter.Name = kvp.Key;
			dynFilter.DefaultIfEmpty = true;
			dynFilter.Enabled = true;
			dynFilter.IsInteractive = true;
			dynFilter.Label = kvp.Value + ":";
			dynFilter.SendEmpty = false;
			dynFilter.suppressInteractive = false;
			if (userFilter != null)
				userFilter.ItemID = 0;
			if (choiceFilter != null) {
				choiceFilter.choices = new string [choiceField.Choices.Count];
				choiceField.Choices.CopyTo (choiceFilter.choices, 0);
			}
			if (lookupFilter != null) {
				if (isLookupFilter) {
					lookupFilter.ListUrl = ProductPage.MergeUrlPaths (connectedList.ParentWeb.Url, connectedList.DefaultViewUrl);
					if (lookupFilter.ListUrl.ToLowerInvariant ().StartsWith (SPContext.Current.Web.Url.ToLowerInvariant ().TrimEnd ('/') + '/'))
						lookupFilter.ListUrl = lookupFilter.ListUrl.Substring (SPContext.Current.Web.Url.ToLowerInvariant ().TrimEnd ('/').Length + 1);
					lookupFilter.ItemID = 0;
					lookupFilter.ItemSorting = 1;
					lookupFilter.ValueFieldName = kvp.Key;
					lookupFilter.stripID = ((field is SPFieldLookup) || (field is SPFieldUser));
					lookupFilter.AllowMultiEnter = CamlFilters;
				} else {
					doTry2 = false;
					Action try1 = delegate () {
						object obj;
						PropertyInfo prop = lookupField.GetType ().GetProperty ("ViewUrl");
						if ((prop == null) || ((obj = prop.GetValue (lookupField, null)) == null))
							doTry2 = true;
						else
							lookupFilter.ListUrl = obj.ToString ();
					};
					Action try2 = delegate () {
						Guid guid = ProductPage.GetGuid (lookupField.LookupList);
						if (Guid.Empty.Equals (guid))
							dynFilter = lookupFilter = null;
						else
							using (SPWeb web = SPContext.Current.Site.OpenWeb (lookupField.LookupWebId))
								lookupFilter.ListUrl = ProductPage.MergeUrlPaths (web.Url, web.Lists [guid].DefaultViewUrl);
					};
					try {
						try1 ();
						if (doTry2)
							try2 ();
					} catch {
						if (doTry2)
							dynFilter = lookupFilter = null;
						else
							try {
								try2 ();
							} catch {
								dynFilter = lookupFilter = null;
							}
					} finally {
						if (lookupFilter != null) {
							lookupFilter.ItemID = 0;
							lookupFilter.ValueFieldName = lookupField.LookupField;
						}
					}
				}
			}
			try {
				if (dynID && (dynFilter != null) && dynFilter.pickerSemantics && (theListPart != null) && (spList != null) && (!string.IsNullOrEmpty (theListPart.ViewGuid)) && (!Guid.Empty.Equals (viewID = new Guid (theListPart.ViewGuid))) && ((spView = spList.Views [viewID]) != null) && (!string.IsNullOrEmpty (spView.Url))) {
					dynFilter.PostFilter = true;
					dynFilter.PostFilterListViewUrl = ProductPage.MergeUrlPaths (spList.ParentWeb.Url, spView.Url);
					dynFilter.PostFilterFieldName = kvp.Key;
					dynFilter.postFilterList = spList;
					dynFilter.postFilterView = spView;
				}
			} catch {
				dynFilter.PostFilter = false;
			}
			return dynFilter;
		}

		internal void EnsureChildControls2 () {
			EnsureChildControls ();
		}

		internal IEnumerable<T> GetConnectedParts<T> () where T : SystemWebPart {
			SPWebPartManager partMan = WebPartManager as SPWebPartManager;
			List<string> yieldedParts = new List<string> ();
			DataFormWebPart dfwp;
			T t;
			if ((partMan != null) && (partMan.SPWebPartConnections != null))
				foreach (SPWebPartConnection conn in partMan.SPWebPartConnections)
					if (((conn.Provider == null) && string.IsNullOrEmpty (conn.ProviderID)) || (conn.Consumer == null))
						additionalWarningsErrors.Add (this ["BrokenConnection", conn.ID]);
					else if (ID.Equals (conn.ProviderID)) {
						if (((dfwp = conn.Consumer as DataFormWebPart) != null) && (!connectedDataParts.Contains (dfwp))) {
							connectedDataParts.Add (dfwp);
							nullParts--;
						}
						if (!connectedParts.Contains (conn.Consumer))
							connectedParts.Add (conn.Consumer);
						if (((t = (conn.Consumer as T)) != null) && !yieldedParts.Contains (t.ID)) {
							yieldedParts.Add (t.ID);
							yield return t;
						}
					}
			if (typeof (T).IsAssignableFrom (typeof (DataFormWebPart)))
				foreach (DataFormWebPart dwp in connectedDataParts)
					if (!yieldedParts.Contains (dwp.ID)) {
						yieldedParts.Add (dwp.ID);
						yield return dwp as T;
					}
			foreach (SystemWebPart swp in connectedParts)
				if (((t = swp as T) != null) && !yieldedParts.Contains (t.ID)) {
					yieldedParts.Add (t.ID);
					yield return t;
				}
			if (yieldedParts.Count > 0)
				_connected = true;
		}

		[ConnectionProvider ("Parameters", "roxorityParametersProviderInterface", typeof (Transform.Provider), AllowsMultipleConnections = true)]
		public IWebPartParameters GetConnectionInterface () {
			return this;
		}

		public IEnumerable<SystemWebPart> GetConnectedParts () {
			return GetConnectedParts<SystemWebPart> ();
		}

		internal List<FilterBase> GetFilters (bool includeDynamicFilters, bool useGroups) {
			return GetFilters (includeDynamicFilters, false, useGroups);
		}

		internal List<FilterBase> GetFilters (bool includeDynamicFilters, bool suggestLookupFilters, bool useGroups) {
			List<FilterBase> list = new List<FilterBase> (), temp = FiltersList;
			FilterBase.Interactive dynFilter;
			string selGroup = string.Empty;
			if (useGroups && (GetGroups ().Count > 1)) {
				includeDynamicFilters = false;
				selGroup = SelectedGroup;
			}
			if (includeDynamicFilters && (dynamicFilters == null) && (DynamicInteractiveFilters > 0) && (validFilterNames.Count > 0)) {
				dynamicFilters = new List<FilterBase> ();
				foreach (KeyValuePair<string, string> kvp in validFilterNames) {
					if (!filters.Exists (delegate (FilterBase filter) {
						return filter.Name.Equals (kvp.Key);
					})) {
						if ((dynFilter = CreateDynamicInteractiveFilter (kvp)) != null)
							dynamicFilters.Add (dynFilter);
						if ((dynFilter = CreateDynamicInteractiveFilter (kvp, true, true)) != null)
							dynamicFilters.Add (dynFilter);
					}
				}
			}
			if (includeDynamicFilters && (DynamicInteractiveFilters == 1) && (dynamicFilters != null))
				list.AddRange (dynamicFilters);
			list.AddRange (filters);
			if (includeDynamicFilters && (DynamicInteractiveFilters == 2) && (dynamicFilters != null))
				list.AddRange (dynamicFilters);
			if (!string.IsNullOrEmpty (selGroup))
				list.RemoveAll (delegate (FilterBase fb) {
					return !fb.groups.Contains (selGroup);
				});
			return list;
		}

		internal string GetFilterValue (string val) {
			return (Exed ? ExpiredMessage : val);
		}

		internal List<string> GetGroups () {
			List<string> gs = new List<string> ();
			string t, g = ((toolPart != null) ? toolPart.groupsTextBox.Text : GetProp<string> ("Groups", Groups));
			if (LicEd (4)) {
				if (g == Groups)
					return groups;
				foreach (string v in ProductPage.Trim (g).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
					if ((!string.IsNullOrEmpty (t = v.Trim ().Replace (",", sep))) && (!gs.Contains (t)))
						gs.Add (t);
			}
			return gs;
		}

		internal bool GetLop (FilterPair fp) {
			bool and = fp.nextAnd;
			FilterBase.Multi camlFilter;
			foreach (FilterBase fb in GetFilters (false, false, true))
				if ((camlFilter = fb as FilterBase.Multi) != null) {
					and = camlFilter.IsAnd (fp.Key, and);
					break;
				}
			return and;
		}

		internal CamlOperator GetOperator (FilterPair fp) {
			CamlOperator op = fp.CamlOperator;
			FilterBase.Multi camlFilter;
			foreach (FilterBase fb in GetFilters (false, false, true))
				if ((camlFilter = fb as FilterBase.Multi) != null) {
					op = camlFilter.GetOperator (fp.Key, op);
					break;
				}
			return op;
		}

		internal void RenderScripts (HtmlTextWriter output, string webUrl) {
			string imgUrl;
			if (CanRun) {
				if (EffectiveJquery && !Page.Items.Contains ("jquery")) {
					Page.Items ["jquery"] = new object ();
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\" src=\"" + webUrl + "/_layouts/roxority_FilterZen/jQuery.js?v=" + ProductPage.Version + "\"></script>");
				}
				if (!Page.Items.Contains ("roxority")) {
					Page.Items ["roxority"] = new object ();
					output.Write ("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + webUrl + "/_layouts/roxority_FilterZen/roxority.tl.css?v=" + ProductPage.Version + "\"/>");
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\" src=\"" + webUrl + "/_layouts/roxority_FilterZen/roxority.tl.js?v=" + ProductPage.Version + "\"></script>");
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\" src=\"" + webUrl + "/_layouts/roxority_FilterZen/json2.tl.js?v=" + ProductPage.Version + "\"></script>");
				}
				if (!Context.Items.Contains ("roxfzscripts")) {
					if (string.IsNullOrEmpty (imgUrl = ProductPage.Config (ProductPage.GetContext (), "Anim").Trim ()))
						imgUrl = webUrl + "/_layouts/images/gears_an" + (ProductPage.Is14 ? "v4" : string.Empty) + ".gif";
					Context.Items ["roxfzscripts"] = new object ();
					if (HasMulti || HasDate)
						output.Write ("<script type=\"text/javascript\" language=\"JavaScript\" src=\"" + webUrl + "/_layouts/datepicker.js\"></script>");
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\" src=\"" + webUrl + "/_layouts/roxority_FilterZen/jqms/jquery.multiSelect.js?v=" + ProductPage.Version + "\"></script>");
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\">var roxEmpty = '" + this ["Empty"] + "', roxEmptyAll = '" + this ["EmptyAll"] + "', roxJqMs = jQuery().multiSelect, roxEmptyNone = '" + this ["EmptyNone"] + "'; </script>");
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\" src=\"" + webUrl + "/_layouts/roxority_FilterZen/roxority_FilterZen.js?v=" + ProductPage.Version + "\"></script>");
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\"> roxAnim = '" + SPEncode.ScriptEncode (imgUrl) + "'; </script>");
					if (!string.IsNullOrEmpty (imgUrl = ProductPage.Config (ProductPage.GetContext (), "AutoInfo").Trim ()))
						output.Write ("<script type=\"text/javascript\" language=\"JavaScript\"> roxAcHint = '" + SPEncode.ScriptEncode (HttpUtility.HtmlEncode (imgUrl)) + "'; </script>");
					output.Write ("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + webUrl + "/_layouts/roxority_FilterZen/jqms/jquery.multiSelect.css?v=" + ProductPage.Version + "\"/>");
					output.Write ("<link rel=\"stylesheet\" type=\"text/css\" href=\"" + webUrl + "/_layouts/roxority_FilterZen/roxority_FilterZen.css?v=" + ProductPage.Version + "\"/>");
				}
				output.WriteLine ("<style type=\"text/css\">");
				if (SuppressSpacing)
					output.WriteLine (" div.ms-PartSpacingVertical { display: none !important; } ");
				output.WriteLine (" .rox-ifilter-all-" + ID + " { border: " + ((ApplyToolbarStylings && SuppressSpacing) ? "0px none" : "1px solid") + " " + (ApplyToolbarStylings ? "#83b0ec" : "transparent") + "; } ");
				if (ApplyToolbarStylings)
					output.Write (" .rox-ifilter-all-" + ID + " { background: #d6e8ff url('" + webUrl + "/_layouts/images/toolgrad.gif') repeat-x; color: #003399; } ");
				output.Write (" div.rox-multibox-" + ClientID + " input.rox-multi-text-input { width: " + (MultiWidth - 6) + "px; } ");
				output.Write (" div.rox-multibox-" + ClientID + " span.rox-multi-check-input { width: " + MultiWidth + "px; } ");
				output.Write (" div.rox-multibox-" + ClientID + " table.rox-dtpickertable input.ms-input { width: " + (MultiWidth - 26) + "px; } ");
				if (ProductPage.Is14)
					output.Write (" span.rox-multi-datepicker, span.rox-multi-userpicker { overflow: hidden; } ");
				output.WriteLine ("</style>");
				output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery(document).ready(function() { jQuery('div#" + MultiPanel.ClientID + " > table').addClass('rox-dtpickertable').attr('id', function() { var tmpID; return (tmpID = jQuery(this).find('input.ms-input').attr('id')).substr(0, tmpID.lastIndexOf('_')); }); }); </script>");
				if (extraHide)
					output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> roxHideDatasheetRibbon = true; </script>");
				if (ajax14hide)
					output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> roxAjax14Hide = true; </script>");
				if (ajax14focus)
					output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> roxAjax14Focus = true; </script>");
				if (ajax14Interval > 0)
					output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> roxAjax14Interval = " + ajax14Interval + "; </script>");
			}
		}

		protected virtual void OnCellConsumerInit (CellConsumerInitEventArgs e) {
			if (CellConsumerInit != null)
				CellConsumerInit (this, e);
		}

		protected virtual void OnCellProviderInit (CellProviderInitEventArgs e) {
			if (CellProviderInit != null)
				CellProviderInit (this, e);
		}

		protected virtual void OnCellReady (CellReadyEventArgs e) {
			if (CellReady != null) {
				if (e.Cell != null)
					eventOrderLog.Add (this ["LogSent", this ["SendCellTo"]]);
				CellReady (this, e);
			}
		}

		protected override void OnLoad (EventArgs e) {
			base.OnLoad (e);
			Action<SystemWebPart> action;
			MenuItemTemplate gridButton;
			base.OnLoad (e);
			action = delegate (SystemWebPart wp) {
				if ((gridButton = ProductPage.FindControl (wp.Controls, "EditInGridButton") as MenuItemTemplate) != null) {
					gridButton.Visible = false;
					gridButton.Attributes ["class"] = "rox-hidehidehide";
					gridButton.Attributes ["style"] = "display: none !important;";
#if !SP12
					gridButton.RibbonCommand = string.Empty;
#endif
				}
			};
			if ("xxx_disable".Equals (ProductPage.Config (ProductPage.GetContext (), "Datasheet")))
				foreach (SystemWebPart webPart in GetConnectedParts ()) {
					if ((!ProductPage.Is14) || (!(webPart is DataFormWebPart)) || (!CamlFilters) || appliedParts.Contains (webPart))
						action (webPart);
					else
						deferredActions [webPart] = action;
				}
		}

		protected virtual void OnRowProviderInit (RowProviderInitEventArgs e) {
			if (RowProviderInit != null)
				RowProviderInit (this, e);
		}

		protected virtual void OnRowReady (RowReadyEventArgs e) {
			e.SelectionStatus = "Standard";
			if (RowReady != null) {
				if ((e.Rows != null) && (e.Rows.Length > 0))
					eventOrderLog.Add (this ["LogSent", this ["SendRowTo"]]);
				try {
					RowReady (this, e);
				} catch {
				}
			}
		}

		private bool initChecksPerformed = false;

		internal void PerformInitChecks () {
			ListViewWebPart lvwp;
			DataFormWebPart dfwp;
			Hashtable ht = new Hashtable ();
			if (!initChecksPerformed) {
				initChecksPerformed = true;
				bool prevCamlFilters = camlFilters, doCamlFilters = false;
				if (IsConnected && (CamlFilters || (doCamlFilters = (AutoConnect && IsViewPage && (viewPart != null) && LicEd (4)))) && (!camlFiltered))
					try {
						camlFilters = true;
						camlFiltered = true;
						if (viewPart == null)
							foreach (SystemWebPart wp in connectedParts)
								if ((wp is DataFormWebPart) || (wp is ListViewWebPart)) {
									viewPart = wp;
									break;
								}
						if ((lvwp = viewPart as ListViewWebPart) != null)
							Apply (lvwp, ht);
						else if (ProductPage.Is14 && ((dfwp = viewPart as DataFormWebPart) != null))
							Apply (dfwp, true, ht);
						foreach (DictionaryEntry entry in ht)
							if (entry.Value is string)
								validFilterNames.Add (new KeyValuePair<string, string> (entry.Key.ToString (), entry.Value.ToString ()));
					} finally {
						if (doCamlFilters)
							camlFilters = prevCamlFilters;
					}
			}
		}

		protected override void CreateChildControls () {
			PeopleEditor peoplePicker;
			SPContext context = ProductPage.GetContext ();
			PerformInitChecks ();
			base.CreateChildControls ();
			MultiPanel.ID = "MultiPanel";
			MultiPanel.Style [HtmlTextWriterStyle.Display] = "none";
			if (!Controls.Contains (MultiPanel))
				Controls.Add (MultiPanel);
			MultiTextBox.ID = "MultiTextBox";
			MultiTextBox.TextMode = TextBoxMode.MultiLine;
			MultiTextBox.Rows = 8;
			MultiTextBox.Width = new Unit (90, UnitType.Percentage);
			if (MultiPanel.Controls.Count == 0) {
				MultiPanel.Controls.Add (MultiTextBox);
				for (int i = 0; i < DatePickers.Length; i++)
					MultiPanel.Controls.Add (ProductPage.InitializeDateTimePicker (DatePickers [i] = new DateTimeControl () {
						ID = "DatePicker" + i, Visible = HasMulti
					}));
				for (int i = 0; i < PeoplePickers.Length; i++) {
					PeoplePickers [i] = peoplePicker = new PeopleEditor () {
						ID = "PeoplePicker" + i, AllUrlZones = true, PlaceButtonsUnderEntityEditor = false, CssClass = "ms-input", Rows = 1, ValidateResolvedEntity = true, ValidatorEnabled = true, MaximumEntities = 1, MultiSelect = false, Width = new Unit (MultiWidth, UnitType.Pixel), Visible = HasMulti
					};
					if (context != null)
						peoplePicker.WebApplicationId = context.Site.WebApplication.Id;
					MultiPanel.Controls.Add (peoplePicker);
				}
			}
		}

		protected override void RenderWebPart (HtmlTextWriter output) {
			//base.RenderWebPart (output);
			string buttonFormat = "<span class=\"rox-ifilter-button\" style=\"white-space: nowrap !important;\">{0}</span>", newQueryString = "?", webUrl = string.Empty;
			int urlFilterCount = 0, minus = 0, multCount = 0;
			bool addedNone = true, hasMultiValues = false, hasMultiSel = false, qsEq = true, designMode = false;
			string [] cfgRemoveParams = ProductPage.Config (ProductPage.GetContext (), "RemoveParams").Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			FilterBase.Interactive ifilter;
			List<KeyValuePair<string, string>> anotherBloodyTempList = new List<KeyValuePair<string, string>> ();
			List<FilterBase.Interactive> ifilters = new List<FilterBase.Interactive> ();
			List<int> removeUrlFields = new List<int> ();
			List<string> removeUrlParams = new List<string> ();
			Dictionary<string, string> newQuery = new Dictionary<string, string> ();
			Dictionary<string, bool> transWritten = new Dictionary<string, bool> ();
			Dictionary<string, int> counts = new Dictionary<string, int> ();
			StringBuilder buffer = new StringBuilder ();
			ListViewWebPart listPart;
			Guid viewID;
			IEnumerable<SystemWebPart> connPartsEnum = GetConnectedParts ();
			IEnumerable<KeyValuePair<string, string>> fpairs;
			try {
				webUrl = SPContext.Current.Web.Url.TrimEnd ('/');
			} catch {
			}
			try {
				designMode = DesignMode || WebPartManager.DisplayMode.AllowPageDesign;
			} catch {
			}
			Action<Guid> urlAction = delegate (Guid vid) {
				if (((viewID = string.IsNullOrEmpty (Context.Request.QueryString ["View"]) ? Guid.Empty : new Guid (Context.Request.QueryString ["View"])) != Guid.Empty) && viewID.Equals (vid)) {
					if ((!CamlFilters) || DisableFilters)
						for (int i = 1; i < int.MaxValue; i++)
							if (Array.IndexOf<string> (Context.Request.QueryString.AllKeys, "FilterField" + i) < 0)
								break;
							else {
								urlFilterCount = i;
								if ((ActiveFilters.IndexOf (Context.Request.QueryString ["FilterField" + i]) >= 0) && (!removeUrlFields.Contains (i)))
									removeUrlFields.Add (i);
							}
					if ("TRUE".Equals (Context.Request.QueryString ["Paged"], StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty (Context.Request.Form ["roxact_" + ID])) {
						removeUrlParams.Add ("paged");
						removeUrlParams.Add ("pagedprev");
						removeUrlParams.Add ("pagefirstrow");
						foreach (string qk in Context.Request.QueryString.AllKeys)
							if ((!string.IsNullOrEmpty (qk)) && qk.StartsWith ("p_", StringComparison.InvariantCultureIgnoreCase))
								removeUrlParams.Add (qk.ToLowerInvariant ());
					}
				}
			};
			foreach (string ln in cfgRemoveParams)
				removeUrlParams.Add ((ln + string.Empty).Trim ().ToLowerInvariant ());
			output.Write ("<input type=\"hidden\" name=\"roxact_" + ID + "\" id=\"roxact_" + ID + "\" value=\"\"/><input type=\"hidden\" name=\"roxact2_" + ID + "\" id=\"roxact2_" + ID + "\" value=\"" + Context.Request.Form ["roxact2_" + ID] + "\"/><span class=\"roxfilterouter\"><span class=\"roxfilterinner" + (ProductPage.Is14 ? " roxfilterinner14" : string.Empty) + "\" id=\"roxfilterinner_" + ID + "\">");
			RenderScripts (output, webUrl);
			if (!ProductPage.isEnabled) {
				using (SPSite adminSite = ProductPage.GetAdminSite ())
					output.Write (FilterToolPart.FORMAT_INFO_CONTROL, ProductPage.GetResource ("NotEnabled", ProductPage.MergeUrlPaths (adminSite.Url, "/_layouts/roxority_FilterZen/default.aspx?cfg=enable"), "FilterZen"), "servicenotinstalled.gif", "noid");
				base.RenderWebPart (output);
				return;
			} else if (Exed) {
				output.Write (FilterToolPart.FORMAT_INFO_CONTROL, "<a href=\"" + ProductPage.MergeUrlPaths (SPContext.Current.Web.Url, "/_layouts/" + ProductPage.AssemblyName + ".aspx?cfg=lic&r=" + new Random ().Next ()) + "\">" + ProductPage.GetResource ("LicExpiry") + "</a>", "servicenotinstalled.gif", "noid");
				base.RenderWebPart (output);
				return;
			} else if (!CanRun) {
				output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["NoCanRun"], "blank.gif", "noid");
				base.RenderWebPart (output);
			} else
				try {
					output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery(document).ready(function() { jQuery('#WebPart" + Qualifier + "').css('overflow', 'visible'); }); ");
					foreach (FilterBase fb in GetFilters (true, false)) {
						if ((fb is FilterBase.Multi) && fb.Enabled)
							multCount++;
						output.Write ("roxFilterNames['" + SPEncode.ScriptEncode (ID.ToLowerInvariant ()) + "_" + SPEncode.ScriptEncode (fb.ID.ToLowerInvariant ()) + "'] = '" + SPEncode.ScriptEncode (fb.Name) + "'; ");
						output.Write ("roxFilterCamlOps['" + SPEncode.ScriptEncode (ID.ToLowerInvariant ()) + "_" + SPEncode.ScriptEncode (fb.ID.ToLowerInvariant ()) + "'] = '" + SPEncode.ScriptEncode (((CamlOperator) fb.CamlOperator).ToString ()) + "'; ");
						if (fb.SendEmpty)
							output.Write ("roxFilterEmpties.push('" + SPEncode.ScriptEncode (ID.ToLowerInvariant ()) + "_" + SPEncode.ScriptEncode (fb.ID.ToLowerInvariant ()) + "'); ");
						if (!string.IsNullOrEmpty (fb.MultiValueSeparator))
							output.Write ("roxSeps.push('" + SPEncode.ScriptEncode (fb.MultiValueSeparator) + "');");
					}
					output.Write (" </script>");
					if ((listViews.Count > 0) || ((connectedView != null) && (connectedList != null)) || UrlParams) {
						output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\">");
						foreach (SystemWebPart part in connectedParts)
							if ((listPart = part as ListViewWebPart) != null) {
								output.WriteLine ("roxListViews[roxListViews.length] = { wpID: '" + ID + "', highlight: " + Highlight.ToString ().ToLowerInvariant () + ", disableFilters: " + (((!CamlFilters) || (DisableFilters && DisableFiltersSome))).ToString ().ToLowerInvariant () + ", embedFilters: " + EmbedFilters.ToString ().ToLowerInvariant () + ", listID: '" + ProductPage.GuidBracedUpper (new Guid (listPart.ListName)) + "', viewID: '" + ProductPage.GuidBracedUpper (new Guid (listPart.ViewGuid)) + "', filters: " + ((ActiveFilters.Count == 0) ? "[]" : ("['" + string.Join ("', '", ActiveFilters.ToArray ()) + "']")) + " };");
								urlAction (ProductPage.GetGuid (listPart.ViewGuid));
							}
						if ((connectedView != null) && (connectedList != null)) {
							output.WriteLine ("roxListViews[roxListViews.length] = { wpID: '" + ID + "', highlight: " + Highlight.ToString ().ToLowerInvariant () + ", disableFilters: " + (((!CamlFilters) || (DisableFilters && DisableFiltersSome))).ToString ().ToLowerInvariant () + ", embedFilters: " + EmbedFilters.ToString ().ToLowerInvariant () + ", listID: '" + ProductPage.GuidBracedUpper (connectedList.ID) + "', viewID: '" + ProductPage.GuidBracedUpper (connectedView.ID) + "', filters: " + ((ActiveFilters.Count == 0) ? "[]" : ("['" + string.Join ("', '", ActiveFilters.ToArray ()) + "']")) + " };");
							urlAction (connectedView.ID);
						}
						if ((removeUrlFields.Count > 0) || (removeUrlParams.Count > 0) || UrlParams) {
							foreach (string k in Context.Request.QueryString.AllKeys)
								if ((!string.IsNullOrEmpty (k)) && (!k.StartsWith ("FilterField")) && (!k.StartsWith ("fz-")) && (!k.StartsWith ("FilterValue")) && !removeUrlParams.Contains (k.ToLowerInvariant ()))
									newQuery [k] = Context.Request.QueryString [k];
							for (int i = 1; i <= urlFilterCount; i++)
								if (removeUrlFields.Contains (i))
									minus++;
								else {
									newQuery ["FilterField" + (i - minus)] = Context.Request.QueryString ["FilterField" + i];
									newQuery ["FilterValue" + (i - minus)] = Context.Request.QueryString ["FilterValue" + i];
								}
						}
						output.WriteLine ("</script>");
					}
					if (designMode) {
						if (!EffectiveJquery)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["JqueryNone"], "ServiceNotInstalled.gif", "noid");
						if (GetGroups ().Count > 1)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["SelectedGroup", SelectedGroup] + this ["SelectedGroup" + (_cellConnected ? "Conn" : "NoConn"), string.Empty, ID], "cat.gif", "noid");
						if (CamlFilters)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["FilterMode"], "ServiceInstalled.gif", "noid");
						if (toolPart != null) {
							if (transform != null)
								output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["MultiLabel", transform.Filter.Name, ClientID, '{', '}'], "checkall.gif", "noid");
							if (_rowConnected)
								output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["RowLabel", string.Empty, ClientID, '{', '}'], "checkall.gif", "noid");
						}
						if ((nullParts > 0) || (connectedParts.Count > 0) || (transform != null)) {
							foreach (SystemWebPart wp in connectedParts) {
								anotherBloodyTempList.Add (new KeyValuePair<string, string> (wp.GetType ().Name, wp.DisplayTitle));
								addedNone = false;
							}
							if (connPartsEnum != null)
								foreach (SystemWebPart wp in connPartsEnum)
									if (!anotherBloodyTempList.Exists (delegate (KeyValuePair<string, string> test) {
										return test.Value.Equals (wp.DisplayTitle);
									})) {
										addedNone = false;
										anotherBloodyTempList.Add (new KeyValuePair<string, string> (wp.GetType ().Name, wp.DisplayTitle));
									}
							if (addedNone && ((nullParts > 0) || ((transform != null) && !CamlFilters)))
								anotherBloodyTempList.Add (new KeyValuePair<string, string> (string.Empty, string.Empty));
							ProductPage.RemoveDuplicates<string, string> (anotherBloodyTempList);
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, string.Format (this ["WebPartConnected"], (((transform != null) && (anotherBloodyTempList.Count == 1)) ? 1 : ActiveFilters.Count), string.Join ("</li><li>", anotherBloodyTempList.ConvertAll<string> (delegate (KeyValuePair<string, string> pair) {
								return ((string.IsNullOrEmpty (pair.Key) && string.IsNullOrEmpty (pair.Value)) ? this ["WebPartConnectedUnknown"] : string.Format ("<i>{0}</i>: <b>{1}</b>", Context.Server.HtmlEncode (pair.Key), Context.Server.HtmlEncode (pair.Value)));
							}).ToArray ()), ((viewPart == null) || !ProductPage.Is14) ? string.Empty : this ["AutoConnected"]), "webpart.gif", "noid");
						}
						if (eventOrderLog.Count > 0)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["LogHtml", string.Join ("", eventOrderLog.ToArray ()), ID], "log16.gif", "noid");
						if (toolPart != null)
							if (validFilterNames.Count > 0)
								output.Write (FilterToolPart.FORMAT_INFO_CONTROL, string.Format (this ["ValidNames"], string.Join ("</li><li>", validFilterNames.ConvertAll<string> (delegate (KeyValuePair<string, string> pair) {
									IEnumerable<DataFormWebPart> parts;
									SPDataSource ds;
									string key = pair.Key;
									if (key.StartsWith ("@") && ((parts = GetConnectedParts<DataFormWebPart> ()) != null))
										foreach (DataFormWebPart wp in parts)
											if (((ds = wp.DataSource as SPDataSource) != null) && (ds.List != null)) {
												key = key.Substring (1);
												break;
											}
									return string.IsNullOrEmpty (pair.Value) ? ("<b>" + key + "</b>") : this ["ValidNamesFormat", Context.Server.HtmlEncode (pair.Value), Context.Server.HtmlEncode (key)];
								}).ToArray ()), '{', '}'), "checkall.gif", "noid");
							else
								output.Write ("<div id=\"roxvalidnames\" style=\"display: none; background-image: none;\" class=\"rox-infobox\">" + this ["ValidNamesNone"] + "</div>");
						if ((connectedDataParts.Count > 0) && (connectedDataParts.Count != connectedDataParts.FindAll (delegate (DataFormWebPart dfwp) {
							return (dfwp.GetType ().FullName == xsltTypeName);
						}).Count) && !CamlFilters)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["DataFormConnected"], "itdatash.gif", "noid");
						if ((nullParts > 0) && (nullParts != connectedParts.Count) && CamlFilters)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["CamlDataFormConnected"], "itdatash.gif", "noid");
					}
					if (DebugMode || designMode) {
						if ((debugFilters.Count > 0) || ((transform != null) && (PartFilters.Count > 0)))
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, string.Format (this ["DebugOutput"], string.Join ("</li><li>", new List<string> (ProductPage.TryEach<string> (PartFilters.ConvertAll<string> (delegate (KeyValuePair<string, FilterPair> pair) {
								int fc = 0;
								counts.TryGetValue (pair.Value.Key, out fc);
								counts [pair.Value.Key] = (++fc);
								if (fc > 1)
									hasMultiValues = true;
								if (((transform == null) || !pair.Key.Equals (transform.ParameterName)) && (!CamlFilters) && (!debugFilters.Exists (delegate (KeyValuePair<string, string> ouf) {
									return ouf.Key.Equals (pair.Key);
								})))
									return null;
								else if (CamlFilters || ((transform != null) && pair.Key.Equals (transform.ParameterName))) {
									if (transWritten.ContainsKey (pair.Key))
										return null;
									string ret = "<b>" + Context.Server.HtmlEncode (pair.Key) + "</b>: <ul>";
									foreach (KeyValuePair<string, FilterPair> yakvp in PartFilters)
										if (yakvp.Key.Equals (pair.Key))
											ret += ("<li>" + Context.Server.HtmlEncode (yakvp.Value.Value) + "</li>");
									transWritten [pair.Key] = true;
									return ret + "</ul>";
								} else
									return string.Format ("{0}: <b>{1}</b>", Context.Server.HtmlEncode (pair.Key), Context.Server.HtmlEncode (pair.Value.Value));
							}), false, null, false)).ToArray ())), "wpfilter.gif", "noid");
						if (hasMultiValues)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["MultiFilterHint"], "iccat.gif", "noid");
						ProductPage.RemoveDuplicates<KeyValuePair<string, string>> (filtersNotSent);
						if (filtersNotSent.Count > 0)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, string.Format (this ["FiltersNotSent"], string.Join ("</li><li>", filtersNotSent.ConvertAll<string> (delegate (KeyValuePair<string, string> pair) {
								return string.Format ("<b>{0}</b> &mdash; {2}: <i>{1}</i>", Context.Server.HtmlEncode (pair.Key), string.IsNullOrEmpty (this ["Reason" + pair.Value]) ? pair.Value : Context.Server.HtmlEncode (this ["Reason" + pair.Value]), this ["Reason"]);
							}).ToArray ()), '{', '}'), "filteroffdisabled.gif", "noid");
					}
					if (ErrorMode || designMode) {
						if (regError != null)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, regError.Message, "exclaim.gif", "noid");
						if (multCount > 1)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["MultiCount", FilterBase.GetFilterTypeTitle (typeof (FilterBase.Multi))], "exclaim.gif", "noid");
						if (Guid.Empty.Equals (ConnectionID) && !IsConnected)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["NotConnected"], "exclaim.gif", "noid");
						foreach (KeyValuePair<FilterBase, Exception> kvp in warningsErrors)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, "[" + kvp.Key.ToString () + "] &mdash; " + kvp.Value.Message, "servicenotinstalled.gif", "noid");
						foreach (string msg in additionalWarningsErrors)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, msg, "servicenotinstalled.gif", "noid");
						foreach (string invalidFilterTypeName in FilterBase.invalidTypes)
							if (!this ["CfgSettingDef_FilterTypes"].Equals (invalidFilterTypeName, StringComparison.InvariantCultureIgnoreCase))
								output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["InvalidFilterTypeName", invalidFilterTypeName], "exclaim.gif", "noid");
						warningsErrors.Clear ();
						additionalWarningsErrors.Clear ();
					}
					foreach (FilterBase filter in GetFilters (true, true)) {
						if (((ifilter = filter as FilterBase.Interactive) != null) && (ifilter.Enabled) && (ifilter.Get<bool> ("IsInteractive")))
							ifilters.Add (ifilter);
						if (filter.isEditMode)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, this ["InfoFilterPreview"], "filter.gif", "noid");
					}
					int thisFilter = 0;
					if (ifilters.Count > 0) {
						output.Write (" <span class=\"rox-ifilter-all rox-ifilter-all-" + ID + "\"> ");
						if ((HtmlMode == 1) && !string.IsNullOrEmpty (HtmlEmbed))
							output.Write (string.Format (buttonFormat, HtmlEmbed.Replace ("{0}", ID)));
						output.Write (" <span class=\"rox-ifilter-controls rox-ifilter-" + ID + "\"> ");
						foreach (FilterBase.Interactive filter in ifilters) {
							if (filter.AutoSuggest && !Context.Items.Contains ("roxjqas")) {
								Context.Items ["roxjqas"] = new object ();
								output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\" src=\"" + webUrl + "/_layouts/roxority_FilterZen/jqas/jquery.ajaxQueue.js\"></script>");
								output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\" src=\"" + webUrl + "/_layouts/roxority_FilterZen/jqas/jquery.bgiframe.min.js\"></script>");
								output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\" src=\"" + webUrl + "/_layouts/roxority_FilterZen/jqas/jquery.autocomplete.js\"></script>");
								output.WriteLine ("<link type=\"text/css\" rel=\"stylesheet\" href=\"" + webUrl + "/_layouts/roxority_FilterZen/jqas/jquery.autocomplete.css\"/>");
							}
							if (AutoRepost)
								output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> roxAutoReposts[roxAutoReposts.length] = 'rox_ifilter_" + filter.ID + "'; roxAutoReposts[roxAutoReposts.length] = 'rox_ifilter_control_" + filter.ID + "'; </script>");
							buffer.Remove (0, buffer.Length);
							using (StringWriter sw = new StringWriter (buffer))
							using (HtmlTextWriter writer = new HtmlTextWriter (sw)) {
								filter.Render (writer, false);
								if (filter.IsRange) {
									writer.Write ("&nbsp;</span><span id=\"roxifiltercontrol2_" + filter.ID + "\">");
									filter.Render (writer, true);
								}
								if (ShowClearButtons && !(filter is FilterBase.Multi))
									writer.Write ("<a class=\"rox-filter-icon-clear\" href=\"#noop\" onclick=\"roxClearFilters('" + ID + "', '" + filter.ID + "');\"></a>");
							}
							if (buffer.Length > 0) {
								if (!string.IsNullOrEmpty (filter.BeginGroup))
									output.Write ("<div class=\"rox-ifilter-header\">" + HttpUtility.HtmlEncode (filter.BeginGroup) + "</div>");
								output.Write (" <span class=\"rox-ifilter" + (filter.IsSet ? " rox-ifilter-active" : string.Empty) + "\" id=\"rox_ifilter_" + filter.ID + "\" style=\"" + ((filter is FilterBase.Multi) ? "display: block; clear: both;" : string.Empty) + "\"> <span class=\"rox-ifilter-label rox-ifilter-label-" + ID + "" + ((filter is FilterBase.Date) ? " rox-ifilter-label-datetime" : string.Empty) + "\" id=\"rox_ifilter_label_" + filter.ID + "\" style=\"white-space: nowrap;\"> " + ((filter is FilterBase.Boolean) ? string.Empty : filter.Get<string> ("Label")) + " </span>" + (filter.IsRange ? ("<span class=\"rox-ifilter-label rox-ifilter-label2 rox-ifilter-label-" + ID + "" + ((filter is FilterBase.Date) ? " rox-ifilter-label-datetime" : string.Empty) + "\" id=\"rox_ifilter_label2_" + filter.ID + "\" style=\"white-space: nowrap;\"> " + ((filter is FilterBase.Boolean) ? string.Empty : filter.Get<string> ("Label2")) + " </span>") : string.Empty) + (filter.CheckStyle ? string.Empty : "<br/>") + "<span class=\"rox-ifilter-control " + (filter.IsNumeric ? "rox-ifilter-control-numeric " : string.Empty) + "rox-ifilter-control-" + ID + "\" id=\"rox_ifilter_control_" + filter.ID + "\"> <span id=\"roxifiltercontrol1_" + filter.ID + "\"> ");
								output.Write (buffer.ToString ());
								output.Write (" </span> </span> </span> ");
								if ((maxFiltersPerRow == 1) || ((maxFiltersPerRow > 0) && (((++thisFilter) % maxFiltersPerRow) == 0)))
									output.Write ("<br style=\"clear: both;\"/>");
							}
							hasMultiSel |= filter.AllowMultiEnter;
							if (UrlParams && ((fpairs = filter.FilterPairs) != null))
								foreach (KeyValuePair<string, string> kvp in fpairs)
									if ((!string.IsNullOrEmpty (kvp.Value)) || filter.SendEmpty)
										newQuery ["fz-" + kvp.Key] = kvp.Value;
						}
						output.Write (" </span> ");
						if ((HtmlMode == 2) && !string.IsNullOrEmpty (HtmlEmbed))
							output.Write (string.Format (buttonFormat, HtmlEmbed.Replace ("{0}", ID)));
						output.Write (" </span> ");
						if (hasMultiSel)
							output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery(document).ready(function() { if (roxJqMs && !jQuery().multiSelect) jQuery.extend(jQuery.fn, { multiSelect: roxJqMs }); try { jQuery('select.rox-multiselect').multiSelect({ selectAll: false, noneSelected: '', oneOrMoreSelected: '*', selectAllText: '' }, roxMultiSelect); } catch(err) { jQuery('span#roxfilterinner_" + ID + "').prepend('<div class=\"rox-infobox\" style=\"background-image: url(" + webUrl + "/_layouts/images/servicenotinstalled.gif);\">" + this ["JqueryWarning"].Replace ("\'", "\\\'") + "</div> '); } }); </script>");
					}
					if (designMode) {
						foreach (KeyValuePair<FilterBase, Exception> kvp in warningsErrors)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, "[" + kvp.Key.ToString () + "] &mdash; " + kvp.Value.Message, "exclaim.gif", "noid");
						foreach (string msg in additionalWarningsErrors)
							output.Write (FilterToolPart.FORMAT_INFO_CONTROL, msg, "servicenotinstalled.gif", "noid");
					}
					warningsErrors.Clear ();
					additionalWarningsErrors.Clear ();

					if ("1".Equals (Context.Request.QueryString ["FilterClear"], StringComparison.InvariantCultureIgnoreCase))
						foreach (string qp in Context.Request.QueryString.AllKeys)
							if ((!string.IsNullOrEmpty (qp)) && (!"FilterClear".Equals (qp, StringComparison.InvariantCultureIgnoreCase)) && !newQuery.ContainsKey (qp))
								newQuery [qp] = Context.Request.QueryString [qp];

					foreach (KeyValuePair<string, string> kvp in newQuery)
						newQueryString += string.Format ("{0}={1}&", Context.Server.UrlEncode (kvp.Key), Context.Server.UrlEncode (kvp.Value));
					if (qsEq = (newQuery.Count == Context.Request.QueryString.Count))
						foreach (KeyValuePair<string, string> kvp in newQuery)
							if (!(qsEq = kvp.Value.Equals (Context.Request.QueryString [kvp.Key])))
								break;
					if (!qsEq)
						output.WriteLine ("<script type=\"text/javascript\" language=\"JavaScript\"> roxNewQueryString = '{0}'; </script>", newQueryString.Substring (0, newQueryString.Length - 1));

					base.RenderWebPart (output);
				} catch (Exception error) {
					output.Write (FilterToolPart.FORMAT_INFO_CONTROL, error.Message, "exclaim.gif", "noid");
				}
			output.Write ("</span></span>");
		}

		public override ConnectionRunAt CanRunAt () {
			return (CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None);
		}

		public override void Dispose () {
			IDisposable disp;
			if (rowTable != null) {
				rowTable.Dispose ();
				rowTable = null;
			}
			foreach (FilterBase fb in GetFilters (true, false))
				if ((disp = fb as IDisposable) != null)
					disp.Dispose ();
			foreach (IDisposable d in disps)
				d.Dispose ();
			base.Dispose ();
		}

		public override void EnsureInterfaces () {
			Guid vid;
			SPList list;
			ListViewWebPart lv;
			bool redir = false;
			try {
				RegisterInterface ("roxorityConsumeCell", InterfaceTypes.ICellConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, this ["GetGroupFrom"], this ["GetGroupFrom"], true);
				RegisterInterface ("roxorityConsumeRow", InterfaceTypes.IRowConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, this ["GetValuesFromRow"], this ["GetValuesFromRow"], true);
				RegisterInterface ("roxorityFilterProviderInterface", InterfaceTypes.IFilterProvider, WssWebPart.UnlimitedConnections, ConnectionRunAt.Server, this, "", this ["SendUrlFilterTo"], this ["SendsDesc"], true);
				RegisterInterface ("roxorityProvideRow", InterfaceTypes.IRowProvider, WssWebPart.UnlimitedConnections, ConnectionRunAt.Server, this, "", this [HasPeople ? "SendRowToAlt" : "SendRowTo"], this ["SendsRowDesc"], true);
				if (ProductPage.Config<bool> (SPContext.Current, "AllowCells"))
					RegisterInterface ("roxorityProvideCell", InterfaceTypes.ICellProvider, WssWebPart.UnlimitedConnections, ConnectionRunAt.Server, this, "", this ["SendCellTo"], this ["SendsCellDesc"], true);
				if ((Context.Request ["ShowInGrid"] == "True") && (!Guid.Empty.Equals (ConnectionID)) && !Guid.Empty.Equals (vid = ProductPage.GetGuid (Context.Request ["View"], false)))
					foreach (SystemWebPart wp in WebPartManager.WebParts)
						if (((lv = wp as ListViewWebPart) != null) && (!Guid.Empty.Equals (lv.ConnectionID)) && lv.Connections.Contains (ProductPage.GuidLower (lv.ConnectionID, true) + "," + ProductPage.GuidLower (ConnectionID, true) + ",") && (ProductPage.GetGuid (lv.ViewGuid, false).Equals (vid)) && ((list = ProductPage.GetList (SPContext.Current.Web, ProductPage.GetGuid (lv.ListName))) != null)) {
							if ("redirect".Equals (ProductPage.Config (ProductPage.GetContext (), "Datasheet")))
								foreach (SPView view in list.Views)
									if ((view.Type == "GRID") && !(view.Hidden || view.PersonalView)) {
										Context.Response.Redirect (ProductPage.MergeUrlPaths (SPContext.Current.Web.Url, view.Url), true);
										redir = true;
									}
							if (!redir)
								additionalWarningsErrors.Add (this ["Datasheet", SPContext.Current.Web.Url.TrimEnd ('/'), lv.ListName]);
							break;
						}
			} catch (Exception ex) {
				regError = ex;
			}
		}

		public List<FilterBase> GetFilters () {
			return FiltersList;
		}

		public override InitEventArgs GetInitEventArgs (string interfaceName) {
			CellConsumerInitEventArgs args = (interfaceName == "roxorityConsumeCell") ? new CellConsumerInitEventArgs () : null;
			if (args != null)
				args.FieldDisplayName = args.FieldName = EffectiveCellFieldName;
			return ((interfaceName == "roxorityProvideRow") ? (RowArgs as InitEventArgs) : args);
		}

		public override ToolPart [] GetToolParts () {
			List<ToolPart> parts = new List<ToolPart> (base.GetToolParts ());
			if (CanRun)
				parts.Insert (0, toolPart = new FilterToolPart (this ["FilterWebPart_Title"]));
			return parts.ToArray ();
		}

		public override void PartCommunicationConnect (string interfaceName, WssWebPart connectedPart, string connectedInterfaceName, ConnectionRunAt runAt) {
			DataFormWebPart dataFormPart;
			ListViewWebPart listViewPart;
			Guid viewID = Guid.Empty, listID;
			PerformInitChecks ();
			if (interfaceName == "roxorityConsumeCell")
				_cellConnected = true;
			if (interfaceName == "roxorityConsumeRow")
				isRowConsumer = true;
			if ((interfaceName == "roxorityFilterProviderInterface") || (interfaceName == "roxorityProvideCell") || (interfaceName == "roxorityProvideRow")) {
				_connected = true;
				if (interfaceName == "roxorityProvideRow")
					_rowConnected = true;
				if (connectedPart == null)
					nullParts++;
				else {
					if (!connectedParts.Contains (connectedPart)) {
						connectedParts.Add (connectedPart);
						if (forceReload && connectedPart.GetType ().FullName == "roxority_PeopleZen.roxority_UserListWebPart")
							try {
								connectedPart.GetType ().GetField ("forceReload", BindingFlags.NonPublic | BindingFlags.Instance).SetValue (connectedPart, true);
							} catch {
							}
					}
					if ((listViewPart = connectedPart as ListViewWebPart) != null) {
						if (connectedList == null)
							try {
								if (Guid.Empty.Equals (listID = ProductPage.GetGuid (listViewPart.ListName)))
									connectedList = SPContext.Current.Web.Lists [listViewPart.ListName];
								else
									connectedList = SPContext.Current.Web.Lists [listID];
							} catch {
							}
						if ((!string.IsNullOrEmpty (listViewPart.ViewGuid)) && (!Guid.Empty.Equals (viewID = new Guid (listViewPart.ViewGuid))) && (!listViews.Contains (viewID)))
							listViews.Add (viewID);
						if ((connectedList != null) && (connectedView == null) && !Guid.Empty.Equals (viewID))
							try {
								connectedView = connectedList.Views [viewID];
							} catch {
							}
					} else if (((dataFormPart = connectedPart as DataFormWebPart) != null) && (!connectedDataParts.Contains (dataFormPart)))
						connectedDataParts.Add (dataFormPart);
				}
			}
		}

		public override void PartCommunicationInit () {
			CellProviderInitEventArgs cellArgs = new CellProviderInitEventArgs ();
			CellConsumerInitEventArgs args = new CellConsumerInitEventArgs ();
			SPWebPartManager spMan = WebPartManager as SPWebPartManager;
			ListViewWebPart lvwp;
			DataFormWebPart dfwp;
			SPContext context = null;
			Guid viewID, listID;
			if ((spMan != null) && (spMan.SPWebPartConnections != null))
				foreach (SPWebPartConnection conn in ((SPWebPartManager) WebPartManager).SPWebPartConnections)
					if (conn.Provider == this) {
						if (context == null)
							try {
								context = SPContext.Current;
							} catch {
							}
						if (!connectedParts.Contains (conn.Consumer))
							connectedParts.Add (conn.Consumer);
						if ((lvwp = conn.Consumer as ListViewWebPart) != null) {
							if ((!Guid.Empty.Equals (viewID = ProductPage.GetGuid (lvwp.ViewGuid))) && !listViews.Contains (viewID))
								listViews.Add (viewID);
							if (context != null) {
								try {
									if (connectedList == null)
										if (Guid.Empty.Equals (listID = ProductPage.GetGuid (lvwp.ListName)))
											connectedList = context.Web.Lists [lvwp.ListName];
										else
											connectedList = context.Web.Lists [listID];
								} catch {
								}
								if ((connectedList != null) && (connectedView == null) && !Guid.Empty.Equals (viewID))
									try {
										connectedView = connectedList.Views [viewID];
									} catch {
									}
							}
						} else if ((dfwp = conn.Consumer as DataFormWebPart) != null) {
							if (!connectedDataParts.Contains (dfwp))
								connectedDataParts.Add (dfwp);
							if (connectedList == null)
								try {
									connectedList = Reflector.Current.Get (dfwp, "SPList") as SPList;
								} catch {
								}
							viewID = Guid.Empty;
							try {
								viewID = (Guid) Reflector.Current.Get (dfwp, "ViewID");
							} catch {
							}
							if (!Guid.Empty.Equals (viewID)) {
								if (!listViews.Contains (viewID))
									listViews.Add (viewID);
								if ((connectedList != null) && (connectedView == null))
									try {
										connectedView = connectedList.Views [viewID];
									} catch {
									}
							}
						}
					}
			if (_cellConnected) {
				args.FieldDisplayName = args.FieldName = EffectiveCellFieldName;
				OnCellConsumerInit (args);
			} else if (IsConnected) {
				if (_rowConnected)
					OnRowProviderInit (RowArgs);
				if ((cellNames != null) && (cellNames.Length > 0) && (GetFilters (true, false).Exists (delegate (FilterBase filter) {
					return filter.Enabled && filter.Name.Equals (cellNames [0]);
				}))) {
					cellArgs.FieldDisplayName = cellNames [(cellNames.Length > 1) ? 1 : 0];
					cellArgs.FieldName = cellNames [0];
					OnCellProviderInit (cellArgs);
				}
			}
			if (CamlFilters && !(camlFiltered || isRowConsumer)) {
				camlFiltered = true;
				if (!Exed) {
					Apply<ListViewWebPart> (GetConnectedParts<ListViewWebPart> (), false);
					Apply<DataFormWebPart> (GetConnectedParts<DataFormWebPart> (), ProductPage.Is14 && ((!string.IsNullOrEmpty (FolderScope)) || RecollapseGroups || !string.IsNullOrEmpty (ListViewUrl)));
				}
			}
		}

		public override void PartCommunicationMain () {
			int eIndex = 0;
			string filterExpression = string.Empty, val = string.Empty;
			bool revertToClear = false, rowSent = false;
			XmlDocument selXml = new XmlDocument ();
			Guid selId;
			SetFilterEventArgs setFilterEventArgs = new SetFilterEventArgs ();
			RowReadyEventArgs rowArgs = new RowReadyEventArgs ();
			SPDataSource spds;
			SPWeb web;
			if (_cellConnected && IsConnected && !firstSkipped) {
				firstSkipped = true;
				return;
			}
			debugFilters.Clear ();
			if (IsConnected) {
				if (CamlFilters && (connectedList == null)) {
					foreach (ListViewWebPart webPart in GetConnectedParts<ListViewWebPart> ())
						try {
							disps.Add (web = SPContext.Current.Site.OpenWeb (Guid.Empty.Equals (webPart.WebId) ? SPContext.Current.Web.ID : webPart.WebId));
							try {
								connectedList = web.Lists [new Guid (webPart.ListName)];
							} catch {
								try {
									connectedList = web.Lists [webPart.ListName];
								} catch {
								}
							}
							if (connectedList != null) {
								selXml.LoadXml (webPart.ListViewXml);
								break;
							}
						} catch {
						}
					if (connectedList == null)
						foreach (DataFormWebPart webPart in GetConnectedParts<DataFormWebPart> ())
							if (((spds = webPart.DataSource as SPDataSource) != null) && (spds.List != null)) {
								connectedList = spds.List;
								selXml.LoadXml (string.IsNullOrEmpty (Context.Application ["orig_" + webPart.ID] as string) ? spds.SelectCommand : Context.Application ["orig_" + webPart.ID] as string);
								break;
							}
					if ((connectedList != null) && (selXml.DocumentElement != null) && (selXml.DocumentElement.Attributes.GetNamedItem ("Name") != null) && !Guid.Empty.Equals (selId = ProductPage.GetGuid (selXml.DocumentElement.Attributes.GetNamedItem ("Name").Value)))
						foreach (SPView v in connectedList.Views)
							if (v.ID.Equals (selId)) {
								connectedView = v;
								break;
							}
				}
				if ((cellNames != null) && (cellNames.Length > 0) && ((cellFilter = GetFilters (true, true).Find (delegate (FilterBase filter) {
					return filter.Enabled && filter.Name.Equals (cellNames [0]);
				})) != null))
					cellArgs = new CellReadyEventArgs ();
				if (rowTable != null)
					rowTable.Dispose ();
				rowTable = new DataTable ();
				if (!(CamlFilters || isRowConsumer))
					foreach (KeyValuePair<string, FilterPair> kvp in PartFilters) {
						debugFilters.Add (new KeyValuePair<string, string> (kvp.Key, kvp.Value.Value));
						if (((val = GetFilterValue (kvp.Value.Value)).IndexOf ('&') >= 0) && ((connectedList != null) || (connectedDataParts.Count > 0))) {
							additionalWarningsErrors.Add (this ["Ampersand", kvp.Value.Key, val]);
							if (!ProductPage.Is14) {
								val = val.Replace ("&", string.Empty);
								additionalWarningsErrors.Add (this ["Ampersand12"]);
							}
						}
						filterExpression += string.Format ("FilterField{2}={0}&FilterValue{2}={1}&", kvp.Key, val, ++eIndex);
					}
				if (_rowConnected && (RowArgs.FieldList.Length > 0)) {
					foreach (string f in RowArgs.FieldList)
						if (!rowTable.Columns.Contains (f))
							rowTable.Columns.Add (f);
					rowArgs.Rows = Exed ? new DataRow [0] : new DataRow [] { rowTable.Rows.Add (new List<string> (RowArgs.FieldList).ConvertAll<object> (delegate (string fieldName) {
						KeyValuePair<string, FilterPair> result = PartFilters.Find (delegate (KeyValuePair<string, FilterPair> keyValuePair) {
							return (fieldName.Equals (keyValuePair.Key) || (fieldName.StartsWith ("@") && fieldName.Substring (1).Equals (keyValuePair.Key)));
						});
						return ((result.Value == null) ? string.Empty : result.Value.Value);
					}).ToArray ()) };
					//if (!CamlFilters)
					OnRowReady (rowArgs);
					//else if (!additionalWarningsErrors.Contains (this ["NoRowConnection"]))
					//    additionalWarningsErrors.Add (this ["NoRowConnection"]);
					rowSent = true;
				}
				deferredFilterAction1 = delegate () {
					if (isRowConsumer)
						foreach (KeyValuePair<string, FilterPair> kvp in PartFilters) {
							debugFilters.Add (new KeyValuePair<string, string> (kvp.Key, kvp.Value.Value));
							if (((val = GetFilterValue (kvp.Value.Value)).IndexOf ('&') >= 0) && ((connectedList != null) || (connectedDataParts.Count > 0))) {
								additionalWarningsErrors.Add (this ["Ampersand", kvp.Value.Key, val]);
								if (!ProductPage.Is14) {
									val = val.Replace ("&", string.Empty);
									additionalWarningsErrors.Add (this ["Ampersand12"]);
								}
							}
							filterExpression += string.Format ("FilterField{2}={0}&FilterValue{2}={1}&", kvp.Key, val, ++eIndex);
						}
					if (filterExpression.Length != 0)
						filterExpression = filterExpression.Substring (0, filterExpression.Length - 1);
					else
						revertToClear = true;
					if (SetFilter != null) {
						setFilterEventArgs.FilterExpression = ((CamlFilters || Exed) ? "" : filterExpression);
						try {
							SetFilter (this, setFilterEventArgs);
							if (!CamlFilters)
								eventOrderLog.Add (this ["LogSent", this ["SendUrlFilterTo"]]);
						} catch {
						}
					} else
						revertToClear = true;
				};
				if (!isRowConsumer) {
					deferredFilterAction1 ();
					deferredFilterAction1 = null;
				}
			}
			if (!CamlFilters) {
				deferredFilterAction2 = delegate () {
					if ((!IsConnected) || revertToClear) {
						if (IsConnected && revertToClear && (debugFilters.Count > 0) && (!_rowConnected && rowSent))
							debugFilters.Clear ();
						if (ClearFilter != null)
							ClearFilter (this, EventArgs.Empty);
						else if (NoFilter != null)
							NoFilter (this, EventArgs.Empty);
						foreach (DataFormWebPart dataPart in connectedDataParts)
							if (!xsltTypeName.Equals (dataPart.GetType ().FullName)) {
								dataPart.FilterValues.Collection.Clear ();
								dataPart.DataBind ();
							}
					} else if (!Exed) {
						foreach (DataFormWebPart dataPart in connectedDataParts)
							if (!xsltTypeName.Equals (dataPart.GetType ().FullName)) {
								foreach (KeyValuePair<string, string> kvp in debugFilters)
									dataPart.FilterValues.Set (kvp.Key, GetFilterValue (kvp.Value));
								dataPart.DataBind ();
							}
					}
				};
				if (!isRowConsumer) {
					deferredFilterAction2 ();
					deferredFilterAction2 = null;
				}
			} else if (!(camlFiltered || isRowConsumer)) {
				camlFiltered = true;
				if (!Exed) {
					Apply<ListViewWebPart> (GetConnectedParts<ListViewWebPart> (), false);
					Apply<DataFormWebPart> (GetConnectedParts<DataFormWebPart> (), false);
				}
			} else if (CamlFilters && (!camlFiltered) && (consumedRow.Count > 0)) {
				camlFiltered = true;
				if (!Exed) {
					Apply<ListViewWebPart> (GetConnectedParts<ListViewWebPart> (), true);
					Apply<DataFormWebPart> (GetConnectedParts<DataFormWebPart> (), true);
				}
				if (deferredFilterAction1 != null) {
					deferredFilterAction1 ();
					deferredFilterAction1 = null;
				}
				if (deferredFilterAction2 != null) {
					deferredFilterAction2 ();
					deferredFilterAction2 = null;
				}
			}
		}

		[ConnectionProvider ("Transformed", "roxorityTransformedFilterProviderInterface", typeof (Transform.Provider), AllowsMultipleConnections = true)]
		public ITransformableFilterValues SetConnectionInterface () {
			if (transform == null)
				foreach (FilterBase fb in GetFilters (true, false))
					if (fb.Enabled && fb.SupportsMultipleValues && (!(fb is FilterBase.Multi)) && (string.IsNullOrEmpty (MultiValueFilterID) || fb.ID.Equals (MultiValueFilterID)))
						return (transform = new Transform (this, fb));
			return transform;
		}

		internal List<string> ActiveFilters {
			get {
				List<string> names = new List<string> ();
				foreach (FilterBase fb in GetFilters (true, true))
					if (fb.Enabled)
						names.Add (fb.Name);
				return names;
			}
		}

		internal string ListViewUrl {
			get {
				if (listViewUrl == null)
					foreach (KeyValuePair<string, FilterPair> kvp in PartFilters)
						kvp.GetType ();
				return listViewUrl;
			}
		}

		internal StateBag State {
			get {
				return ViewState;
			}
		}

		protected internal override bool CanRun {
			get {
				return base.CanRun && (!IsFrontPage) && !IsPreview;
			}
		}

		[Personalizable (false), XmlIgnore]
		protected internal List<FilterBase> FiltersList {
			get {
				if (filters == null) {
					try {
						if (!string.IsNullOrEmpty (serializedFilters))
							filters = FilterBase.Deserialize (this, serializedFilters);
					} catch {
					}
					if (filters == null)
						serializedFilters = FilterBase.Serialize (filters = new List<FilterBase> ());
				}
				return filters;
			}
			set {
				if ((filters = value) == null)
					filters = new List<FilterBase> ();
				serializedFilters = FilterBase.Serialize (filters);
			}
		}

		protected internal RowProviderInitEventArgs RowArgs {
			get {
				List<KeyValuePair<string, string>> filterFields = new List<KeyValuePair<string, string>> ();
				FilterBase.Interactive ifb;
				if (rowArgs == null) {
					rowArgs = new RowProviderInitEventArgs ();
					foreach (FilterBase fb in GetFilters (false, false, false))
						if (fb.Enabled && !filterFields.Exists (delegate (KeyValuePair<string, string> test) {
							return fb.Name.Equals (test.Key);
						}))
							filterFields.Add (new KeyValuePair<string, string> (fb.Name, (((ifb = fb as FilterBase.Interactive) == null) || string.IsNullOrEmpty (ifb.Get<string> ("Label"))) ? fb.Name : (ifb.Get<string> ("Label").EndsWith (":") ? ifb.Get<string> ("Label").Substring (0, ifb.Get<string> ("Label").Length - 1) : ifb.Get<string> ("Label"))));
					rowArgs.FieldList = filterFields.ConvertAll<string> (delegate (KeyValuePair<string, string> value) {
						return value.Key;
					}).ToArray ();
					rowArgs.FieldDisplayList = filterFields.ConvertAll<string> (delegate (KeyValuePair<string, string> value) {
						return value.Value;
					}).ToArray ();
				}
				return rowArgs;
			}
		}

		[Personalizable]
		public string AcSecFields {
			get {
				return (acSecFields == null) ? string.Empty : string.Join ("\r\n", acSecFields).Trim ();
			}
			set {
				acSecFields = (value + string.Empty).Trim ().Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			}
		}

		public bool Activated {
			get {
				return !string.IsNullOrEmpty (Context.Request.Form ["roxact2_" + ID]);
			}
		}

		[Personalizable]
		public bool Ajax14hide {
			get {
				return ajax14hide;
			}
			set {
				ajax14hide = value;
			}
		}

		[Personalizable]
		public bool Ajax14focus {
			get {
				return ajax14focus;
			}
			set {
				ajax14focus = value;
			}
		}

		[Personalizable]
		public int Ajax14Interval {
			get {
				return ajax14Interval;
			}
			set {
				ajax14Interval = value;
			}
		}

		[Personalizable]
		public bool ApplyToolbarStylings {
			get {
				return GetProp<bool> ("ApplyToolbarStylings", applyToolbarStylings);
			}
			set {
				applyToolbarStylings = value;
			}
		}

		[Personalizable]
		public bool AutoConnect {
			get {
				return autoConnect;
			}
			set {
				autoConnect = value;
			}
		}

		[Personalizable]
		public bool AutoRepost {
			get {
				return GetProp<bool> ("AutoRepost", autoRepost) || EmbedFilters;
			}
			set {
				autoRepost = value;
			}
		}

		[Personalizable]
		public bool EmbedFilters {
			get {
				return GetProp<bool> ("EmbedFilters", embedFilters);
			}
			set {
				embedFilters = value;
			}
		}

		[Personalizable]
		public bool CamlFilters {
			get {
				return LicEd (4) && camlFilters;
			}
			set {
				camlFilters = LicEd (4) && value;
			}
		}

		[Personalizable]
		public string CamlFiltersAndCombined {
			get {
				return LicEd (4) ? camlFiltersAndCombined : string.Empty;
			}
			set {
				camlFiltersAndCombined = ProductPage.Trim (value);
			}
		}

		public string [] CamlSourceFilters {
			get {
				List<string> list = new List<string> ();
				foreach (FilterBase fb in GetFilters (false, false, true))
					if (fb.Enabled && (fb is FilterBase.CamlSource))
						list.Add (fb.Name);
				return list.ToArray ();
			}
		}

		[Personalizable]
		public bool Cascaded {
			get {
				return LicEd (4) && GetProp<bool> ("Cascade", cascaded) && (Activated || !SearchBehaviour);
			}
			set {
				cascaded = LicEd (4) && value;
			}
		}

		[Personalizable]
		public string CellFieldName {
			get {
				return cellFieldName;
			}
			set {
				cellFieldName = (string.IsNullOrEmpty (value = ProductPage.Trim (value)) ? "FilterGroup" : value);
			}
		}

		[Personalizable]
		public string DataPartIDsString {
			get {
				return string.Empty;
			}
			set {
			}
		}

		[Personalizable]
		public bool DebugMode {
			get {
				return GetProp<bool> ("DebugMode", debugMode);
			}
			set {
				debugMode = value;
			}
		}

		[Personalizable]
		public bool DefaultToOr {
			get {
				return LicEd(4) && GetProp<bool> ("DefaultToOr", defaultToOr);
			}
			set {
				defaultToOr = LicEd(4) && value;
			}
		}

		[Personalizable]
		public bool DisableFilters {
			get {
				return GetProp<bool> ("DisableFilters", disableFilters);
			}
			set {
				disableFilters = value;
			}
		}

		[Personalizable]
		public bool DisableFiltersSome {
			get {
				return GetProp<bool> ("DisableFiltersSome", disableFiltersSome);
			}
			set {
				disableFiltersSome = value;
			}
		}

		[Personalizable]
		public int DynamicInteractiveFilters {
			get {
				return 0;
			}
			set {
			}
		}

		public List<string> EffectiveAndFilters {
			get {
				List<string> ands = new List<string> ();
				FilterBase.Interactive ia;
				ands.AddRange (CamlFiltersAndCombined.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
				foreach (FilterBase fb in GetFilters (false, false, false))
					if (((ia = fb as FilterBase.Interactive) != null) && ia.IsRange && !ands.Contains (ia.Name))
						ands.Add (ia.Name);
				return ands;
			}
		}

		public string EffectiveCellFieldName {
			get {
				return ((cell == null) || (cell.Length == 0) || string.IsNullOrEmpty (cell [0])) ? CellFieldName : cell [0];
			}
		}

		[Personalizable]
		public bool ErrorMode {
			get {
				return GetProp<bool> ("ErrorMode", errorMode);
			}
			set {
				errorMode = value;
			}
		}

		[Personalizable]
		public string FolderScope {
			get {
				return LicEd (4) ? folderScope : string.Empty;
			}
			set {
				folderScope = LicEd (4) ? value : string.Empty;
			}
		}

		public string GeneratedQuery {
			get {
				return generatedQuery;
			}
			set {
				string tmp = HttpUtility.HtmlDecode (generatedQuery = value + string.Empty);
				if ((Context != null) && !string.IsNullOrEmpty (tmp))
					Context.Items ["roxFiltCaml"] = tmp;
				if ((Page != null) && !string.IsNullOrEmpty (tmp))
					Page.Items ["roxFiltCaml"] = tmp;
			}
		}

		[Personalizable]
		public string Groups {
			get {
				return LicEd (4) ? string.Join ("\r\n", groups.ConvertAll<string> (delegate (string v) {
					return v.Replace (sep, ",");
				}).ToArray ()) : string.Empty;
			}
			set {
				string [] arr = LicEd (4) ? ProductPage.Trim (value).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries) : new string [0];
				string t;
				groups.Clear ();
				foreach (string v in arr)
					if ((!string.IsNullOrEmpty (t = v.Trim ().Replace (",", sep))) && (!groups.Contains (t)))
						groups.Add (t);
			}
		}

		public bool HasDate {
			get {
				bool hd = false;
				List<FilterBase> filters;
				if (((hasDate == null) || !hasDate.HasValue) && ((filters = GetFilters (false, false, true)) != null)) {
					foreach (FilterBase fb in filters)
						if (hd = ((fb is FilterBase.Date) && fb.Enabled))
							break;
					hasDate = hd;
				}
				return (hasDate != null) && hasDate.HasValue && hasDate.Value;
			}
		}

		public bool HasHiddenFilter {
			get {
				foreach (FilterBase fb in GetFilters (false, false, true))
					if (fb.Enabled && ((fb is FilterBase.Multi) || (!(fb is FilterBase.Interactive)) || !((FilterBase.Interactive) fb).IsInteractive))
						return true;
				return false;
			}
		}

		public bool HasMulti {
			get {
				bool hm = false;
				List<FilterBase> filters;
				if (((hasMulti == null) || !hasMulti.HasValue) && ((filters = GetFilters (false, false, true)) != null)) {
					foreach (FilterBase fb in filters)
						if (hm = ((fb is FilterBase.Multi) && fb.Enabled))
							break;
					hasMulti = hm;
				}
				return (hasMulti != null) && hasMulti.HasValue && hasMulti.Value;
			}
		}

		public bool HasPeople {
			get {
				bool hasPeoplePart = false;
				WebPartCollection wps = null;
				if ((hasPeople == null) || !hasPeople.HasValue) {
					try {
						if (WebPartManager != null)
							wps = WebPartManager.WebParts;
						if ((wps != null) && (wps.Count == 0))
							wps = null;
					} catch {
					}
					if (wps != null) {
						foreach (SystemWebPart wp in ProductPage.TryEach<SystemWebPart> (wps))
							if ((wp != null) && (hasPeoplePart = (wp.GetType ().FullName == "roxority_PeopleZen.roxority_UserListWebPart")))
								break;
						hasPeople = hasPeoplePart;
					}
				}
				return (hasPeople != null) && hasPeople.HasValue && hasPeople.Value;
			}
		}

		[Personalizable]
		public bool Highlight {
			get {
				return LicEd(4) && GetProp<bool> ("Highlight", highlight);
			}
			set {
				highlight = LicEd (4) && value;
			}
		}

		[Personalizable]
		public string HtmlEmbed {
			get {
				if (htmlEmbed == null)
					htmlEmbed = this ["HtmlTemp1"];
				return htmlEmbed;
			}
			set {
				htmlEmbed = ((value == null) ? string.Empty : value.Trim ());
			}
		}

		[Personalizable]
		public int HtmlMode {
			get {
				return GetProp<int> ("HtmlMode", htmlMode);
			}
			set {
				htmlMode = value;
			}
		}

		public override bool IsConnected {
			get {
				if ((!_connected) && (connectedParts.Count == 0) && AutoConnect && IsViewPage && (viewPart != null)) {
					connectedParts.Add (viewPart);
					_connected = true;
				}
				return _connected || _rowConnected || (transform != null) || base.IsConnected;
			}
		}

		public override bool IsViewPage {
			get {
				int count = 0, lvCount = 0;
				SystemWebPart wpLast = null;
				if (((isViewPage == null) || !isViewPage.HasValue) && (WebPartManager != null)) {
					foreach (SystemWebPart wp in WebPartManager.WebParts) {
						count++;
						if ((wp is ListViewWebPart) || xsltTypeName.Equals (wp.GetType ().FullName) || "roxority_FilterZen.XsltListViewWebPart".Equals (wp.GetType ().FullName)) {
							wpLast = wp;
							lvCount++;
						}
					}
					if ((isViewPage = ((lvCount == 1) && (wpLast != null))).Value)
						viewPart = wpLast;
				}
				return ((isViewPage != null) && isViewPage.HasValue && isViewPage.Value);
			}
		}

		public bool IsViewPageCore {
			get {
				return base.IsViewPage;
			}
		}

		[Personalizable]
		public string JsonFilters {
			get {
				return LicEd (4) ? GetProp<string> ("JsonFilters", jsonFilters) : string.Empty;
			}
			set {
				jsonFilters = LicEd (4) ? value : string.Empty;
			}
		}

		[Personalizable]
		public int MaxFiltersPerRow {
			get {
				return maxFiltersPerRow;
			}
			set {
				maxFiltersPerRow = ((value < 0) ? 0 : value);
			}
		}

		[Personalizable]
		public string MultiValueFilterID {
			get {
				return LicEd (4) ? multiValueFilterID : string.Empty;
			}
			set {
				multiValueFilterID = LicEd (4) ? ProductPage.Trim (value) : string.Empty;
			}
		}

		public int MultiWidth {
			get {
				return multiWidth;
			}
			set {
				multiWidth = ((value < 60) ? 240 : value);
			}
		}

		[Personalizable]
		public bool NoListFolders {
			get {
				return false;
			}
			set {
			}
		}

		[Personalizable]
		public bool RecollapseGroups {
			get {
				return GetProp<bool> ("RecollapseGroups", recollapseGroups);
			}
			set {
				recollapseGroups = value;
			}
		}

		public List<KeyValuePair<string, FilterPair>> PartFilters {
			get {
				int fcount = 0, keepIndex = -1;
				bool noSend = false, doSend = false, doBreak = false;
				string [] pairSplit;
				string suppressMultiValues;
				KeyValuePair<string, string> pair;
				KeyValuePair<string, FilterPair> tmpPair;
				List<KeyValuePair<string, string>> pairs, filterPairs;
				IEnumerable<KeyValuePair<string, string>> fps;
				List<FilterBase> zehFiltas;
				FilterBase.Interactive iaFilter;
				FilterBase.Date dtFilter;
				if (partFilters == null) {
					listViewUrl = string.Empty;
					filtersNotSent.Clear ();
					partFilters = new List<KeyValuePair<string, FilterPair>> ();
					if ((zehFiltas = GetFilters (true, false)) != null)
						foreach (FilterBase filter in zehFiltas)
							if (doBreak)
								break;
							else if (filter != null)
								if (string.IsNullOrEmpty (filter.Name))
									filtersNotSent.Add (new KeyValuePair<string, string> (filter.ToString (), "NoName"));
								else if (!filter.Enabled)
									filtersNotSent.Add (new KeyValuePair<string, string> (filter.Name, "Disabled"));
								else if (filter.SuppressIfInactive && (!Activated))
									filtersNotSent.Add (new KeyValuePair<string, string> (filter.Name, "NonActive"));
								else if ((!string.IsNullOrEmpty (filter.SuppressIfParam)) && new List<string> (filter.SuppressIfParam.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Exists (delegate (string pn) {
									return (Context.Request.QueryString [pn] != null);
								}))
									filtersNotSent.Add (new KeyValuePair<string, string> (filter.Name, "HasParam"));
								else if ("_roxListView".Equals (filter.Name)) {
									if ((fps = filter.FilterPairs) != null)
										foreach (KeyValuePair<string, string> kvp in fps) {
											listViewUrl = kvp.Value;
											break;
										}
								} else if ((GetGroups ().Count > 1) && (!string.IsNullOrEmpty (SelectedGroup)) && (!filter.groups.Contains (SelectedGroup)))
									filtersNotSent.Add (new KeyValuePair<string, string> (filter.Name, "Group"));
								else if ((partFilters.Count > 0) && !LicEd (2))
									filtersNotSent.Add (new KeyValuePair<string, string> (filter.Name, ProductPage.GetResource ("NopeEd", this ["MultiFilters"], "Basic")));
								else {
									iaFilter = filter as FilterBase.Interactive;
									fcount = 0;
									if ((fps = filter.FilterPairs) == null)
										filterPairs = new List<KeyValuePair<string, string>> ();
									else
										try {
											filterPairs = new List<KeyValuePair<string, string>> (fps);
										} catch (Exception ex) {
											filtersNotSent.Add (new KeyValuePair<string, string> (filter.Name, ex.Message));
											filterPairs = new List<KeyValuePair<string, string>> ();
										}
									if (((dtFilter = filter as FilterBase.Date) != null) && ((dtFilter.EffectiveDateCulture != null) || !string.IsNullOrEmpty (dtFilter.Get<string> ("DateFormat"))))
										filterPairs = filterPairs.ConvertAll<KeyValuePair<string, string>> (delegate (KeyValuePair<string, string> kv) {
											if (!string.IsNullOrEmpty (kv.Value))
												return new KeyValuePair<string, string> (kv.Key, ProductPage.ConvertDateToString (ProductPage.ConvertStringToDate (kv.Value), dtFilter.Get<string> ("DateFormat"), dtFilter.EffectiveDateCulture));
											return kv;
										});
									if (filterPairs != null) {
										if ((iaFilter != null) && (iaFilter.IsNumeric))
											filterPairs = filterPairs.ConvertAll<KeyValuePair<string, string>> (delegate (KeyValuePair<string, string> thePair) {
												return new KeyValuePair<string, string> (thePair.Key, iaFilter.GetNumeric (thePair.Value));
											});
										if ((iaFilter != null) && iaFilter.pickerSemantics && iaFilter.SendAllAsMultiValuesIfEmpty && (filterPairs.Count == 1) && string.IsNullOrEmpty (filterPairs [0].Value))
											try {
												foreach (string v in iaFilter.AllPickableValues)
													filterPairs.Add (new KeyValuePair<string, string> (iaFilter.Name, v));
												if (filterPairs.Count > 1)
													filterPairs.RemoveAt (0);
											} catch (Exception ex) {
												for (int i = 1; i < filterPairs.Count; i++)
													filterPairs.RemoveAt (i);
												iaFilter.Report (ex);
											}
										ProductPage.RemoveDuplicates<string, string> (filterPairs);
										if ((filterPairs.Count > 0) && !string.IsNullOrEmpty (suppressMultiValues = filter.Get<string> ("SuppressMultiValues"))) {
											if (suppressMultiValues == "[-1]")
												keepIndex = filterPairs.Count - 1;
											else if (suppressMultiValues.StartsWith ("[") && suppressMultiValues.EndsWith ("]") && int.TryParse (suppressMultiValues, out keepIndex))
												keepIndex--;
											if ((keepIndex >= 0) && (keepIndex < filterPairs.Count))
												filterPairs = new List<KeyValuePair<string, string>> (new KeyValuePair<string, string> [] { filterPairs [keepIndex] });
											else
												filterPairs = new List<KeyValuePair<string, string>> (new KeyValuePair<string, string> [] { new KeyValuePair<string, string> (filter.Name, string.Join (suppressMultiValues, filterPairs.ConvertAll<string> (delegate (KeyValuePair<string, string> tuple) {
													return tuple.Value;
												}).ToArray ())) });
										}
										if ((filterPairs.Count == 0) && SearchBehaviour && !Activated)
											filterPairs.Add (new KeyValuePair<string, string> (filter.Name, string.Empty));
										foreach (KeyValuePair<string, string> kvp in filterPairs) {
											fcount++;
											pair = kvp;
											doSend = noSend = false;
											if (SearchBehaviour && !Activated) {
												doBreak = true;
												pair = new KeyValuePair<string, string> (string.IsNullOrEmpty (pair.Key) ? "Title" : pair.Key, "Emulating search behavior [" + pair.Key + "] -- " + Guid.NewGuid ());
											}
											if ((((filter.Get<int> ("SuppressMode") == 1) || (filter.Get<int> ("SuppressMode") == 2)) && (Array.IndexOf<string> (filter.suppressValues, pair.Value) >= 0)) || (((filter.Get<int> ("SuppressMode") == 3) || (filter.Get<int> ("SuppressMode") == 4)) && (Array.IndexOf<string> (filter.suppressValues, pair.Value) < 0)))
												if ((filter.Get<int> ("SuppressMode") == 1) || (filter.Get<int> ("SuppressMode") == 3))
													pair = new KeyValuePair<string, string> (pair.Key, string.Empty);
												else {
													noSend = true;
													filtersNotSent.Add (new KeyValuePair<string, string> (pair.Key, "Locked"));
												}
											if (!noSend) {
												if (SuppressUnknownFilters && (!validFilterNames.Exists (delegate (KeyValuePair<string, string> val) {
													return pair.Key.Equals (val.Key);
												})))
													filtersNotSent.Add (new KeyValuePair<string, string> (pair.Key, "Suppressed"));
												else if ((!string.IsNullOrEmpty (pair.Value)) || filter.Get<bool> ("SendEmpty") || !string.IsNullOrEmpty (filter.Get<string> ("FallbackValue"))) {
													doSend = true;
													partFilters.Add ((string.IsNullOrEmpty (pair.Value) && (!string.IsNullOrEmpty (filter.Get<string> ("FallbackValue")))) ? new KeyValuePair<string, FilterPair> (pair.Key, new FilterPair (pair.Key, filter.Get<string> ("FallbackValue"), filter.Get<int> ("CamlOperator"))) : new KeyValuePair<string, FilterPair> (pair.Key, new FilterPair (pair.Key, pair.Value, filter.Get<int> ("CamlOperator"))));
												} else
													filtersNotSent.Add (new KeyValuePair<string, string> (pair.Key, "Empty"));
											}
											if (doSend && !string.IsNullOrEmpty (filter.Get<string> ("MultiValueSeparator"))) {
												pairs = new List<KeyValuePair<string, string>> ();
												tmpPair = partFilters [partFilters.Count - 1];
												partFilters.RemoveAt (partFilters.Count - 1);
												if (string.IsNullOrEmpty (filter.Get<string> ("MultiFilterSeparator")))
													foreach (string s in tmpPair.Value.Value.Split (new string [] { filter.Get<string> ("MultiValueSeparator") }, StringSplitOptions.RemoveEmptyEntries))
														pairs.Add (new KeyValuePair<string, string> (tmpPair.Key, s));
												else
													foreach (string s in tmpPair.Value.Value.Split (new string [] { filter.Get<string> ("MultiFilterSeparator") }, StringSplitOptions.RemoveEmptyEntries))
														if (((pairSplit = s.Split (new string [] { filter.Get<string> ("MultiValueSeparator") }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pairSplit.Length > 0))
															pairs.Add (new KeyValuePair<string, string> ((pairSplit.Length == 1) ? tmpPair.Key : pairSplit [0], pairSplit [(pairSplit.Length == 1) ? 0 : 1]));
												foreach (KeyValuePair<string, string> kv in pairs)
													if (!((((filter.Get<int> ("SuppressMode") == 1) || (filter.Get<int> ("SuppressMode") == 2)) && (Array.IndexOf<string> (filter.suppressValues, kv.Value) >= 0)) || (((filter.Get<int> ("SuppressMode") == 3) || (filter.Get<int> ("SuppressMode") == 4)) && (Array.IndexOf<string> (filter.suppressValues, kv.Value) < 0))))
														partFilters.Add (new KeyValuePair<string, FilterPair> (kv.Key, new FilterPair (kv.Key, kv.Value, filter.Get<int> ("CamlOperator"))));
													else if ((filter.Get<int> ("SuppressMode") == 1) || (filter.Get<int> ("SuppressMode") == 3))
														partFilters.Add (new KeyValuePair<string, FilterPair> (kv.Key, new FilterPair (kv.Key, kv.Value, filter.Get<int> ("CamlOperator"))));
													else
														filtersNotSent.Add (new KeyValuePair<string, string> (kv.Key + " = " + kv.Value, "Locked"));
											}
											if (doSend && (cellArgs != null) && (cellFilter != null) && (filter.ID.Equals (cellFilter.ID))) {
												cellArgs.Cell = pair.Value;
												OnCellReady (cellArgs);
												cellArgs = null;
											}
											if (doBreak)
												break;
										}
									}
									if ((fcount == 0) && !(filter is FilterBase.CamlDistinct))
										filtersNotSent.Add (new KeyValuePair<string, string> (filter.Name, "Null"));
								}
					ProductPage.RemoveDuplicates<string, FilterPair> (partFilters);
				}
				return partFilters;
			}
		}

		[Personalizable]
		public bool RememberFilterValues {
			get {
				return LicEd (2) && GetProp<bool> ("RememberFilterValues", rememberFilterValues);
			}
			set {
				rememberFilterValues = LicEd (2) && value;
			}
		}

		[Personalizable]
		public bool RespectFilters {
			get {
				return GetProp<bool> ("RespectFilters", respectFilters);
			}
			set {
				respectFilters = value;
			}
		}

		[Personalizable (false)]
		public string SelectedGroup {
			get {
				SelectedGroup = group;
				return group;
			}
			set {
				List<string> gs = GetGroups ();
				group = ((gs.Count < 2) ? string.Empty : gs [0]);
				foreach (string g in GetGroups ())
					if (g.Equals (value)) {
						group = value;
						break;
					}
			}
		}

		[Personalizable]
		public string SerializedFilters {
			get {
				return serializedFilters;
			}
			set {
				serializedFilters = value;
			}
		}

		[Personalizable]
		public bool SearchBehaviour {
			get {
				return LicEd (4) && GetProp<bool> ("SearchBehaviour", searchBehaviour);
			}
			set {
				searchBehaviour = LicEd (4) && value;
			}
		}

		[Personalizable]
		public bool ShowClearButtons {
			get {
				return LicEd (4) && GetProp<bool> ("ShowClearButtons", showClearButtons);
			}
			set {
				showClearButtons = LicEd (4) && value;
			}
		}

		[Personalizable]
		public bool SuppressSpacing {
			get {
				return GetProp<bool> ("SuppressSpacing", suppressSpacing);
			}
			set {
				suppressSpacing = value;
			}
		}

		[Personalizable]
		public bool SuppressUnknownFilters {
			get {
				return LicEd (4) && GetProp<bool> ("SuppressUnknownFilters", suppressUnknownFilters);
			}
			set {
				suppressUnknownFilters = LicEd (4) && value;
			}
		}

		[Personalizable]
		public bool UrlParams {
			get {
				return LicEd (4) && urlParams;
			}
			set {
				urlParams = LicEd (4) && value;
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

		public void GetParametersData (ParametersCallback callback) {
			Hashtable ht = new Hashtable ();
			if (callback != null) {
				_connected = true;
				foreach (KeyValuePair<string, FilterPair> kvp in PartFilters) {
					ht [kvp.Value.Key] = kvp.Value.Value;
					eventOrderLog.Add (this ["LogSent", this ["Parameters", kvp.Value.Key]]);
				}
				callback (ht);
			}
		}

		public PropertyDescriptorCollection Schema {
			get {
				string name;
				List<CustomPropertyDescriptor> props = new List<CustomPropertyDescriptor> ();
				foreach (KeyValuePair<string, FilterPair> kvp in PartFilters)
					props.Add (new CustomPropertyDescriptor (name = kvp.Value.Key, null, new CustomPropertyHelper ("FilterZen", name, name), null));
				return new PropertyDescriptorCollection (props.ToArray ());
			}
		}

		public void SetConsumerSchema (PropertyDescriptorCollection schema) {
			if ((schema != null) && (schema.Count == 0))
				foreach (PropertyDescriptor pd in ((IWebPartParameters) this).Schema)
					schema.Add (pd);
		}

	}

}
