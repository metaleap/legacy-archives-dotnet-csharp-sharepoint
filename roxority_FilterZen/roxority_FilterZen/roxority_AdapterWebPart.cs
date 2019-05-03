
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

	[Guid ("cdbec42f-2b0c-4c1c-8984-1a38f425e8f6")]
	public class roxority_AdapterWebPart : WebPartBase, ICellConsumer, IFilterConsumer, IListConsumer, IParametersInConsumer, IParametersOutConsumer, IRowConsumer, ICellProvider, IFilterProvider, IListProvider, IParametersInProvider, IParametersOutProvider, IRowProvider {

		#region ConnPoint Class

		public class ConnPoint : ITransformableFilterValues, IWebPartField, IWebPartParameters, IWebPartRow, IWebPartTable {

			public readonly roxority_AdapterWebPart Part;

			public ConnPoint (roxority_AdapterWebPart part) {
				Part = part;
			}

			bool ITransformableFilterValues.AllowAllValue {
				get {
					return true;
				}
			}

			bool ITransformableFilterValues.AllowEmptyValue {
				get {
					return true;
				}
			}

			bool ITransformableFilterValues.AllowMultipleValues {
				get {
					return true;
				}
			}

			string ITransformableFilterValues.ParameterName {
				get {
					if (Part.cellFieldName != null)
						return Part.cellFieldName;
					foreach (KeyValuePair<string, string> kvp in Part.fields)
						return kvp.Key;
					return "ITransformableFilterValues";
				}
			}

			ReadOnlyCollection<string> ITransformableFilterValues.ParameterValues {
				get {
					string [] ret = Part.Provide (ProviderPreference.Values) as string [];
					return new ReadOnlyCollection<string> (new List<string> ((ret == null) ? new string [0] : ret));
				}
			}

			void IWebPartField.GetFieldValue (FieldCallback callback) {
				callback (Part.Provide (ProviderPreference.SingleValue));
			}

			PropertyDescriptor IWebPartField.Schema {
				get {
					if (Part.cellFieldName != null)
						return new CustomPropertyDescriptor (Part.cellFieldName, new Attribute [0], null, null);
					foreach (KeyValuePair<string, string> kvp in Part.fields)
						return new CustomPropertyDescriptor (kvp.Key, new Attribute [0], null, null);
					return new CustomPropertyDescriptor ("IWebPartField", new Attribute [0], null, null);
				}
			}

			void IWebPartParameters.GetParametersData (ParametersCallback callback) {
				DataTable dt = Part.Provide (ProviderPreference.Table) as DataTable;
				Hashtable ht = new Hashtable ();
				if ((dt != null) && (dt.Rows.Count > 0) && (dt.Columns.Count > 0)) {
					ht = new Hashtable ();
					foreach (DataColumn col in dt.Columns)
						ht [col.ColumnName] = dt.Rows [0] [col];
					callback (ht);
				}
			}

			PropertyDescriptorCollection IWebPartParameters.Schema {
				get {
					return GetSchema ("IWebPartParameters");
				}
			}

			void IWebPartParameters.SetConsumerSchema (PropertyDescriptorCollection schema) {
			}

			void IWebPartRow.GetRowData (RowCallback callback) {
				DataTable dt = Part.Provide (ProviderPreference.Table) as DataTable;
				if ((dt != null) && (dt.Rows.Count > 0))
					callback (dt.Rows [0]);
			}

			PropertyDescriptorCollection IWebPartRow.Schema {
				get {
					return GetSchema ("IWebPartRow");
				}
			}

			void IWebPartTable.GetTableData (TableCallback callback) {
				DataTable dt = Part.Provide (ProviderPreference.Table) as DataTable;
				if ((dt != null) && (dt.Rows.Count > 0))
					callback (dt.Rows);
			}

			PropertyDescriptorCollection IWebPartTable.Schema {
				get {
					return GetSchema ("IWebPartTable");
				}
			}

			internal PropertyDescriptorCollection GetSchema (string name) {
				PropertyDescriptorCollection props = new PropertyDescriptorCollection (new PropertyDescriptor [0], false);
				if (Part.cellFieldName != null)
					props.Add (new CustomPropertyDescriptor (Part.cellFieldName, new Attribute [0], null, null));
				foreach (KeyValuePair<string, string> kvp in Part.fields)
					props.Add (new CustomPropertyDescriptor (kvp.Key, new Attribute [0], null, null));
				if (props.Count == 0)
					props.Add (new CustomPropertyDescriptor (name, new Attribute [0], null, null));
				return props;
			}

		}

		#endregion

		#region ProviderPreference Enumeration

		public enum ProviderPreference {

			FilterString,
			Rows,
			SingleValue,
			Table,
			Values

		}

		#endregion

		public event CellConsumerInitEventHandler CellConsumerInit;
		public event CellProviderInitEventHandler CellProviderInit;
		public event CellReadyEventHandler CellReady;
		public event ClearFilterEventHandler ClearFilter;
		public event FilterConsumerInitEventHandler FilterConsumerInit;
		public event ListProviderInitEventHandler ListProviderInit;
		public event ListReadyEventHandler ListReady;
		public event NoFilterEventHandler NoFilter;
		public event NoParametersInEventHandler NoParametersIn;
		public event NoParametersOutEventHandler NoParametersOut;
		public event ParametersInConsumerInitEventHandler ParametersInConsumerInit;
		public event ParametersInReadyEventHandler ParametersInReady;
		public event ParametersOutProviderInitEventHandler ParametersOutProviderInit;
		public event ParametersOutReadyEventHandler ParametersOutReady;
		public event PartialListReadyEventHandler PartialListReady;
		public event RowProviderInitEventHandler RowProviderInit;
		public event RowReadyEventHandler RowReady;
		public event SetFilterEventHandler SetFilter;

		internal Dictionary<ITransformableFilterValues, Duo<string, ReadOnlyCollection<string>>> filterProviders = new Dictionary<ITransformableFilterValues, Duo<string, ReadOnlyCollection<string>>> ();
		internal Dictionary<IWebPartField, object> fieldProviders = new Dictionary<IWebPartField, object> ();
		internal Dictionary<IWebPartParameters, IDictionary> paramProviders = new Dictionary<IWebPartParameters, IDictionary> ();
		internal Dictionary<IWebPartRow, object> rowProviders = new Dictionary<IWebPartRow, object> ();
		internal Dictionary<IWebPartTable, ICollection> tableProviders = new Dictionary<IWebPartTable, ICollection> ();
		internal SortedDictionary<string, string> filterValues = new SortedDictionary<string, string> ();
		internal SortedDictionary<string, string> fields = new SortedDictionary<string, string> ();

		internal bool filterClear = false, filterNone = false, listPartial = false, paramInNone = false, paramOutNone = false;
		internal string cellFieldName = null, cellFieldTitle = null, rowSel = "Standard";
		internal object cellValue = null;
		internal int consumersWaiting = 0;
		internal DataRow [] rows = null;
		internal DataTable list = null;
		internal ParameterOutProperty [] paramOutProps = null;
		internal ParameterInProperty [] paramInProps = null;
		internal string [] paramInValues = null, paramOutValues = null;

		public roxority_AdapterWebPart () {
			ChromeType = PartChromeType.None;
		}

		void ICellConsumer.CellProviderInit (object sender, CellProviderInitEventArgs e) {
			if (cellFieldName == null)
				cellFieldName = e.FieldName;
			else
				e.FieldName = cellFieldName;
			if (cellFieldTitle == null)
				cellFieldTitle = e.FieldDisplayName;
			else
				e.FieldDisplayName = cellFieldTitle;
		}

		void ICellConsumer.CellReady (object sender, CellReadyEventArgs e) {
			cellValue = e.Cell;
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		void ICellProvider.CellConsumerInit (object sender, CellConsumerInitEventArgs e) {
			if (cellFieldName == null)
				cellFieldName = e.FieldName;
			else
				e.FieldName = cellFieldName;
			if (cellFieldTitle == null)
				cellFieldTitle = e.FieldDisplayName;
			else
				e.FieldDisplayName = cellFieldTitle;
		}

		void IFilterConsumer.ClearFilter (object sender, EventArgs e) {
			filterClear = true;
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		void IFilterConsumer.NoFilter (object sender, EventArgs e) {
			filterNone = true;
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		void IFilterConsumer.SetFilter (object sender, SetFilterEventArgs e) {
			string lastName = null;
			string [] arr;
			List<string> names = new List<string> ();
			foreach (string pair in e.FilterExpression.Split (new char [] { '&' }, StringSplitOptions.RemoveEmptyEntries))
				if (((arr = pair.Split (new char [] { '=' }, StringSplitOptions.RemoveEmptyEntries))).Length > 1)
					if (lastName == null)
						names.Add (lastName = string.Join ("=", arr, 1, arr.Length - 1));
					else {
						filterValues [lastName] = string.Join ("=", arr, 1, arr.Length - 1);
						lastName = null;
					}
			Fields = new Duo<string [], string []> (names.ToArray (), names.ToArray ());
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		void IFilterProvider.FilterConsumerInit (object sender, FilterConsumerInitEventArgs e) {
			Fields = new Duo<string [], string []> (e.FieldList, e.FieldDisplayList);
		}

		void IListConsumer.ListProviderInit (object sender, ListProviderInitEventArgs e) {
			Fields = new Duo<string [], string []> (e.FieldList, e.FieldDisplayList);
		}

		void IListConsumer.ListReady (object sender, ListReadyEventArgs e) {
			listPartial = false;
			list = e.List;
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		void IListConsumer.PartialListReady (object sender, PartialListReadyEventArgs e) {
			if (listPartial = ((list == null) && (e.List != null)))
				list = e.List;
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		void IParametersInConsumer.NoParametersIn (object sender, EventArgs e) {
			paramInNone = true;
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		void IParametersInConsumer.ParametersInReady (object sender, ParametersInReadyEventArgs e) {
			paramInNone = false;
			paramInValues = e.ParameterValues;
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		void IParametersInProvider.ParametersInConsumerInit (object sender, ParametersInConsumerInitEventArgs e) {
			if ((paramInProps = e.ParameterInProperties) != null)
				Fields = new Duo<string [], string []> (Array.ConvertAll<ParameterInProperty, string> (paramInProps, (pip) => {
					return pip.ParameterName;
				}), Array.ConvertAll<ParameterInProperty, string> (paramInProps, (pip) => {
					return pip.ParameterDisplayName;
				}));
		}

		void IParametersOutConsumer.NoParametersOut (object sender, EventArgs e) {
			paramOutNone = true;
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		void IParametersOutConsumer.ParametersOutProviderInit (object sender, ParametersOutProviderInitEventArgs e) {
			if ((paramOutProps = e.ParameterOutProperties) != null)
				Fields = new Duo<string [], string []> (Array.ConvertAll<ParameterOutProperty, string> (paramOutProps, (pop) => {
					return pop.ParameterName;
				}), Array.ConvertAll<ParameterOutProperty, string> (paramOutProps, (pop) => {
					return pop.ParameterDisplayName;
				}));
		}

		void IParametersOutConsumer.ParametersOutReady (object sender, ParametersOutReadyEventArgs e) {
			paramOutNone = false;
			paramOutValues = e.ParameterValues;
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		void IRowConsumer.RowProviderInit (object sender, RowProviderInitEventArgs e) {
			Fields = new Duo<string [], string []> (e.FieldList, e.FieldDisplayList);
		}

		void IRowConsumer.RowReady (object sender, RowReadyEventArgs e) {
			if (!string.IsNullOrEmpty (e.SelectionStatus))
				rowSel = e.SelectionStatus;
			rows = e.Rows;
			if ((--consumersWaiting) == 0)
				PartCommunicationMain ();
		}

		internal Duo<string [], string []> Fields {
			get {
				string [] keys = new string [fields.Count], vals = new string [fields.Count];
				fields.Keys.CopyTo (keys, 0);
				fields.Values.CopyTo (vals, 0);
				return new Duo<string [], string []> (keys, vals);
			}
			set {
				if ((value != null) && (value.Value1 != null) && (value.Value2 != null))
					for (int i = 0; i < value.Value1.Length; i++)
						fields [value.Value1 [i]] = ((value.Value2.Length > i) ? value.Value2 [i] : value.Value1 [i]);
			}
		}

		internal object Provide (ProviderPreference pref) {
			string fs;
			int j = 1;
			bool dtNu = false;
			DataRow [] dtRows;
			DataRow dtRow;
			List<string> values;
			List<DataRow> allRows = new List<DataRow> ();
			SortedDictionary<string, string> fvals;
			DataTable dt = null;
			if (rows != null)
				foreach (DataRow r in rows)
					allRows.Add (r);
			if (list != null)
				foreach (DataRow r in list.Rows)
					allRows.Add (r);
			if (pref == ProviderPreference.FilterString) {
				fs = string.Empty;
				fvals = new SortedDictionary<string, string> ();
				if (filterValues != null)
					foreach (KeyValuePair<string, string> kvp in filterValues)
						fvals [kvp.Key] = kvp.Value;
				if ((cellValue != null) && (cellFieldName != null))
					fvals [cellFieldName] = cellValue.ToString ();
				if ((paramInProps != null) && (paramInValues != null))
					for (int i = 0; i < Math.Min (paramInProps.Length, paramInValues.Length); i++)
						fvals [paramInProps [i].ParameterName] = paramInValues [i];
				if ((paramOutProps != null) && (paramOutValues != null))
					for (int i = 0; i < Math.Min (paramOutProps.Length, paramOutValues.Length); i++)
						fvals [paramOutProps [i].ParameterName] = paramOutValues [i];
				if (allRows.Count > 0)
					foreach (DataColumn col in allRows [0].Table.Columns)
						fvals [col.ColumnName] = allRows [0] [col] + string.Empty;
				foreach (KeyValuePair<string, string> kvp in fvals)
					fs += string.Format ("FilterField{0}={1}&FilterValue{0}={2}&", j++, kvp.Key.Replace ("&", "%26").Replace ("=", "%3D"), kvp.Value.Replace ("&", "%26").Replace ("=", "%3D"));
				return string.IsNullOrEmpty (fs) ? string.Empty : fs.Substring (0, fs.Length - 1);
			} else if ((pref == ProviderPreference.Rows) || (pref == ProviderPreference.Table)) {
				dt = new DataTable ();
				foreach (KeyValuePair<string, string> kvp in fields)
					if (!dt.Columns.Contains (kvp.Key))
						dt.Columns.Add (kvp.Key);
				if ((cellFieldName != null) && !dt.Columns.Contains (cellFieldName))
					dt.Columns.Add (cellFieldName);
				foreach (DataRow row in allRows)
					dt.Rows.Add (row);
				{
					dtRow = dt.NewRow ();
					if ((cellValue != null) && (cellFieldName != null))
						dtRow [cellFieldName] = cellValue;
					if (filterValues != null)
						foreach (KeyValuePair<string, string> kvp in filterValues)
							dtRow [kvp.Key] = kvp.Value;
					if ((paramInProps != null) && (paramInValues != null))
						for (int i = 0; i < Math.Min (paramInProps.Length, paramInValues.Length); i++)
							dtRow [paramInProps [i].ParameterName] = paramInValues [i];
					if ((paramOutProps != null) && (paramOutValues != null))
						for (int i = 0; i < Math.Min (paramOutProps.Length, paramOutValues.Length); i++)
							dtRow [paramOutProps [i].ParameterName] = paramOutValues [i];
					dt.Rows.Add (dtRow);
				}
				if (pref == ProviderPreference.Rows) {
					dtRows = new DataRow [dt.Rows.Count];
					dt.Rows.CopyTo (dtRows, 0);
					return dtRows;
				}
				return dt;
			} else if (pref == ProviderPreference.SingleValue) {
				if (cellValue != null)
					return cellValue;
				if (paramInValues != null)
					foreach (string v in paramInValues)
						return v;
				if (paramOutValues != null)
					foreach (string v in paramOutValues)
						return v;
				if (filterValues.Count > 0)
					foreach (string v in filterValues.Values)
						return v;
				foreach (DataRow row in allRows)
					foreach (DataColumn col in row.Table.Columns)
						return row [col];
			} else if (pref == ProviderPreference.Values) {
				values = new List<string> ();
				if (paramInValues != null)
					values.AddRange (paramInValues);
				if (paramOutValues != null)
					values.AddRange (paramOutValues);
				if (cellValue != null)
					values.Add (cellValue.ToString ());
				if (filterValues.Count > 0)
					values.AddRange (filterValues.Values);
				if (allRows.Count > 0)
					foreach (DataColumn col in allRows [0].Table.Columns)
						values.Add (allRows [0] [col] + string.Empty);
				return values.ToArray ();
			}
			return null;
		}

		public override ConnectionRunAt CanRunAt () {
			return ConnectionRunAt.Server;
		}

		public override void EnsureInterfaces () {
			RegisterInterface ("ICellConsumer", InterfaceTypes.ICellConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "ICellConsumer (Get Cell From)", "ICellConsumer", true);
			RegisterInterface ("IFilterConsumer", InterfaceTypes.IFilterConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "IFilterConsumer (Get Filters From)", "IFilterConsumer", true);
			RegisterInterface ("IListConsumer", InterfaceTypes.IListConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "IListConsumer (Get List From)", "IListConsumer", true);
			RegisterInterface ("IParametersInConsumer", InterfaceTypes.IParametersInConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "IParametersInConsumer (Get Input From)", "IParametersInConsumer", true);
			RegisterInterface ("IParametersOutConsumer", InterfaceTypes.IParametersOutConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "IParametersOutConsumer (Get Output From)", "IParametersOutConsumer", true);
			RegisterInterface ("IRowConsumer", InterfaceTypes.IRowConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "IRowConsumer (Get Row From)", "IRowConsumer", true);
			RegisterInterface ("ICellProvider", InterfaceTypes.ICellProvider, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "ICellProvider (Send Cell To)", "ICellProvider", true);
			RegisterInterface ("IFilterProvider", InterfaceTypes.IFilterProvider, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "IFilterProvider (Send Filters To)", "IFilterProvider", true);
			RegisterInterface ("IListProvider", InterfaceTypes.IListProvider, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "IListProvider (Send List To)", "IListProvider", true);
			RegisterInterface ("IParametersInProvider", InterfaceTypes.IParametersInProvider, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "IParametersInProvider (Send Input To)", "IParametersInProvider", true);
			RegisterInterface ("IParametersOutProvider", InterfaceTypes.IParametersOutProvider, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "IParametersOutProvider (Send Output To)", "IParametersOutProvider", true);
			RegisterInterface ("IRowProvider", InterfaceTypes.IRowProvider, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, "IRowProvider (Send Row To)", "IRowProvider", true);
		}

		public override InitEventArgs GetInitEventArgs (string interfaceName) {
			Duo<string [], string []> fields = Fields;
			ParameterInProperty [] pips;
			ParameterOutProperty [] pops;
			if ((fields == null) || (fields.Value1 == null) || (fields.Value1.Length == 0))
				fields = new Duo<string [], string []> (new string [] { interfaceName }, new string [] { interfaceName });
			if (interfaceName == "ICellConsumer")
				return new CellConsumerInitEventArgs () {
					FieldDisplayName = "ICellConsumer", FieldName = "ICellConsumer"
				};
			if (interfaceName == "IFilterConsumer")
				return new FilterConsumerInitEventArgs () {
					FieldList = fields.Value1, FieldDisplayList = fields.Value2
				};
			if (interfaceName == "IParametersInConsumer") {
				pips = new ParameterInProperty [fields.Value1.Length];
				for (int i = 0; i < pips.Length; i++)
					pips [i] = new ParameterInProperty () {
						Description = ((fields.Value2.Length > i) ? fields.Value2 [i] : fields.Value1 [i]), ParameterDisplayName = ((fields.Value2.Length > i) ? fields.Value2 [i] : fields.Value1 [i]), ParameterName = fields.Value1 [i], Required = false
					};
				return new ParametersInConsumerInitEventArgs () {
					ParameterInProperties = pips
				};
			}
			if (interfaceName == "ICellProvider")
				return new CellProviderInitEventArgs () {
					FieldName = "ICellProvider", FieldDisplayName = "ICellProvider"
				};
			if (interfaceName == "IListProvider")
				return new ListProviderInitEventArgs () {
					FieldList = fields.Value1, FieldDisplayList = fields.Value2
				};
			if (interfaceName == "IParametersOutProvider") {
				pops = new ParameterOutProperty [fields.Value1.Length];
				for (int i = 0; i < pops.Length; i++)
					pops [i] = new ParameterOutProperty () {
						Description = ((fields.Value2.Length > i) ? fields.Value2 [i] : fields.Value1 [i]), ParameterDisplayName = ((fields.Value2.Length > i) ? fields.Value2 [i] : fields.Value1 [i]), ParameterName = fields.Value1 [i]
					};
				return new ParametersOutProviderInitEventArgs () {
					ParameterOutProperties = pops
				};
			}
			if (interfaceName == "IRowProvider")
				return new RowProviderInitEventArgs () {
					FieldList = fields.Value1, FieldDisplayList = fields.Value2
				};
			return base.GetInitEventArgs (interfaceName);
		}

		public override void PartCommunicationConnect (string interfaceName, WssWebPart connectedPart, string connectedInterfaceName, ConnectionRunAt runAt) {
		}

		public override void PartCommunicationInit () {
			if (CellConsumerInit != null) {
				consumersWaiting++;
				CellConsumerInit (this, GetInitEventArgs ("ICellConsumer") as CellConsumerInitEventArgs);
			}
			if (FilterConsumerInit != null) {
				consumersWaiting++;
				FilterConsumerInit (this, GetInitEventArgs ("IFilterConsumer") as FilterConsumerInitEventArgs);
			}
			if (ParametersInConsumerInit != null) {
				consumersWaiting++;
				ParametersInConsumerInit (this, GetInitEventArgs ("IParametersInConsumer") as ParametersInConsumerInitEventArgs);
			}
			if (CellProviderInit != null)
				CellProviderInit (this, GetInitEventArgs ("ICellProvider") as CellProviderInitEventArgs);
			if (ListProviderInit != null)
				ListProviderInit (this, GetInitEventArgs ("IListProvider") as ListProviderInitEventArgs);
			if (RowProviderInit != null)
				RowProviderInit (this, GetInitEventArgs ("IRowProvider") as RowProviderInitEventArgs);
		}

		public override void PartCommunicationMain () {
			if (consumersWaiting == 0) {
				if (CellReady != null)
					CellReady (this, new CellReadyEventArgs () {
						Cell = Provide (ProviderPreference.SingleValue)
					});
				if (filterClear && (ClearFilter != null))
					ClearFilter (this, EventArgs.Empty);
				else if (filterNone && (NoFilter != null))
					NoFilter (this, EventArgs.Empty);
				else if (SetFilter != null)
					SetFilter (this, new SetFilterEventArgs () {
						FilterExpression = Provide (ProviderPreference.FilterString) as string
					});
				if (listPartial && (PartialListReady != null))
					PartialListReady (this, new PartialListReadyEventArgs () {
						List = Provide (ProviderPreference.Table) as DataTable
					});
				else if (ListReady != null)
					ListReady (this, new ListReadyEventArgs () {
						List = Provide (ProviderPreference.Table) as DataTable
					});
				if (paramInNone && (NoParametersIn != null))
					NoParametersIn (this, EventArgs.Empty);
				else if (ParametersInReady != null)
					ParametersInReady (this, new ParametersInReadyEventArgs () {
						ParameterValues = Provide (ProviderPreference.Values) as string []
					});
				if (paramOutNone && (NoParametersOut != null))
					NoParametersOut (this, EventArgs.Empty);
				else if (ParametersOutReady != null)
					ParametersOutReady (this, new ParametersOutReadyEventArgs () {
						ParameterValues = Provide (ProviderPreference.Values) as string []
					});
				if (RowReady != null)
					try {
						RowReady (this, new RowReadyEventArgs () {
							SelectionStatus = rowSel, Rows = Provide (ProviderPreference.Rows) as DataRow []
						});
					} catch {
					}
			}
		}

	}

}
