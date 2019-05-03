
using Microsoft.Office.Server.ApplicationRegistry.Infrastructure;
using Microsoft.Office.Server.ApplicationRegistry.MetadataModel;
using Microsoft.Office.Server.ApplicationRegistry.Runtime;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Portal;
using Microsoft.SharePoint.Portal.WebControls;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.WebPartPages.Communication;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;

using BdcView = Microsoft.Office.Server.ApplicationRegistry.MetadataModel.View;

namespace roxority_FilterZen.ServerExtensions {

	[Guid ("abd6282a-2ac6-4664-adce-49e93b3b6e2a")]
	public class roxority_BusinessDataItemBuilderWebPart : roxority.SharePoint.WebPartBase, ICellConsumer, IEntityInstanceProvider, IRowProvider {

		/*
SetConsumerSystemInstance
SetConsumerEntities
get SelectedConsumerEntity
GetEntityInstanceId
	get IdentifierValues
GetEntityInstance
	get IdentifierValues
		 */

		#region BdcConnectionProvider Class

		public class BdcConnectionProvider : ConnectionProvider {

			public BdcConnectionProvider (MethodInfo callbackMethod, Type interfaceType, Type controlType, string displayName, string id, bool allowsMultipleConnections)
				: base (callbackMethod, interfaceType, controlType, displayName, id, allowsMultipleConnections) {
			}

			public override bool GetEnabled (Control control) {
				return true;
			}

		}

		#endregion

		private static Dictionary<string, MethodInfo> bdcClientUtilMethods = new Dictionary<string, MethodInfo> ();
		private static MethodInfo licEdMethod = null;

		public event CellConsumerInitEventHandler CellConsumerInit;
		public event RowProviderInitEventHandler RowProviderInit;
		public event RowReadyEventHandler RowReady;

		private Entity entity = null;
		private IEntityInstance entityInstance = null;
		private LobSystemInstance systemInstance = null;
		private Exception regError = null;
		private bool _cellConnected = false, _rowConnected = false;
		private object cellValue = null;
		private string [] cell = null;
		private string cellFieldName = "ID";
		private DataTable rowTable = null;
		private RowProviderInitEventArgs rowArgs = null;

		public static T BdcClientUtil<T> (string methodName, params object [] args) {
			MethodInfo method;
			Type [] types = new Type [(args == null) ? 0 : args.Length];
			if (types.Length > 0)
				for (int i = 0; i < types.Length; i++)
					types [i] = ((args [i] == null) ? null : args [i].GetType ());
			if (!bdcClientUtilMethods.TryGetValue (methodName, out method))
				bdcClientUtilMethods [methodName] = method = roxority_FilterWebPart.BdcClientUtilType.GetMethod (methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly, null, types, null);
			return (T) method.Invoke (null, args);
		}

		internal static bool IsEd {
			get {
				try {
					if (licEdMethod == null)
						licEdMethod = Type.GetType ("roxority.SharePoint.ProductPage, roxority_FilterZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01", true, true).GetMethod ("LicEdition", BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Static, null, new Type [] { typeof (SPContext), typeof (IDictionary), typeof (int) }, null);
					return (bool) licEdMethod.Invoke (null, new object [] { SPContext.Current, null, 4 });
				} catch {
					return false;
				}
			}
		}

		public static bool BdcClientUtilAvailable {
			get {
				try {
					roxority_FilterWebPart.BdcClientUtilType.ToString ();
					return true;
				} catch {
					return false;
				}
			}
		}

		public roxority_BusinessDataItemBuilderWebPart () {
			ChromeType = PartChromeType.None;
			Description = this ["BdcWebPart_Desc"];
		}

		protected virtual void OnCellConsumerInit (CellConsumerInitEventArgs e) {
			if (CellConsumerInit != null)
				CellConsumerInit (this, e);
		}

		protected virtual void OnRowProviderInit (RowProviderInitEventArgs e) {
			if (RowProviderInit != null)
				RowProviderInit (this, e);
		}

		protected virtual void OnRowReady (RowReadyEventArgs e) {
			e.SelectionStatus = "Standard";
			if (RowReady != null)
				RowReady (this, e);
		}

		public override ConnectionRunAt CanRunAt () {
			return CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None;
		}

		public void CellProviderInit (object sender, CellProviderInitEventArgs cellProviderInitArgs) {
			cell = new string [] { cellProviderInitArgs.FieldName, cellProviderInitArgs.FieldDisplayName };
		}

		public void CellReady (object sender, CellReadyEventArgs cellReadyArgs) {
			RowReadyEventArgs rowArgs = new RowReadyEventArgs ();
			BdcView view = null;
			cellValue = cellReadyArgs.Cell;
			bool doit = IsEd || !Exed;
			if (_cellConnected && _rowConnected) {
				if (rowTable != null)
					rowTable.Dispose ();
				rowTable = new DataTable ();
				if (RowArgs.FieldList.Length > 0) {
					foreach (string f in RowArgs.FieldList)
						rowTable.Columns.Add (f);
					if (doit)
						try {
							view = entity.GetSpecificFinderView ();
							if (entityInstance == null)
								entityInstance = GetEntityInstance (view);
						} catch {
						}
					rowArgs.Rows = new DataRow [] { rowTable.Rows.Add (new List<string> (RowArgs.FieldList).ConvertAll<object> (delegate (string fieldName) {
						object val;
						if (!doit)
							return CellValue;
						else if ((entityInstance != null) && (view != null))
							try{
								foreach (Field f in view.Fields)
									if (f.Name == fieldName)
										return (((val = entityInstance [f]) == null) ? string.Empty : val.ToString ());
							} catch {
							}
						return string.Empty;
					}).ToArray ()) };
					OnRowReady (rowArgs);
				}
			}
		}

		public override void Dispose () {
			if (rowTable != null) {
				rowTable.Dispose ();
				rowTable = null;
			}
			base.Dispose ();
		}

		public override void EnsureInterfaces () {
			try {
				RegisterInterface ("roxorityCellConsumerInterface", InterfaceTypes.ICellConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, this ["GetIDFrom"], this ["GetIDFrom"], true);
				RegisterInterface ("roxorityRowProviderInterface", InterfaceTypes.IRowProvider, Microsoft.SharePoint.WebPartPages.WebPart.UnlimitedConnections, ConnectionRunAt.Server, this, "", this ["SendRowTo"], this ["SendsRowDesc"], true);
			} catch (Exception ex) {
				regError = ex;
			}
		}

		public IEntityInstance GetEntityInstance (BdcView desiredView) {
			if (!BdcClientUtilAvailable)
				throw new Exception (this ["BdcNotAvailable"]);
			if ((desiredView != null) && !string.Equals (desiredView.Name, entity.GetSpecificFinderView ().Name))
				throw new NotSupportedException ();
			if (entityInstance == null)
				entityInstance = BdcClientUtil<IEntityInstance> ("FindEntity", entity, IdentifierValues, systemInstance);
			return entityInstance;
		}

		public string GetEntityInstanceId () {
			return EntityInstanceIdEncoder.EncodeEntityInstanceId (IdentifierValues);
		}

		public override InitEventArgs GetInitEventArgs (string interfaceName) {
			CellConsumerInitEventArgs args = (interfaceName == "roxorityCellConsumerInterface") ? new CellConsumerInitEventArgs () : null;
			if (args != null)
				args.FieldDisplayName = args.FieldName = EffectiveCellFieldName;
			else if (interfaceName == "roxorityRowProviderInterface")
				return RowArgs;
			return args;
		}

		[ConnectionProvider ("BdcItem", "roxorityEntityProviderInterface", typeof (BdcConnectionProvider), AllowsMultipleConnections = true)]
		public IEntityInstanceProvider GetProvider () {
			return this;
		}

		public override void PartCommunicationConnect (string interfaceName, Microsoft.SharePoint.WebPartPages.WebPart connectedPart, string connectedInterfaceName, ConnectionRunAt runAt) {
			if (interfaceName == "roxorityCellConsumerInterface")
				_cellConnected = true;
			if (interfaceName == "roxorityRowProviderInterface")
				_rowConnected = true;
		}

		public override void PartCommunicationInit () {
			CellConsumerInitEventArgs args = new CellConsumerInitEventArgs ();
			if (IsConnected) {
				args.FieldDisplayName = args.FieldName = EffectiveCellFieldName;
				OnCellConsumerInit (args);
				if (_rowConnected)
					OnRowProviderInit (RowArgs);
			}
		}

		public override void PartCommunicationMain () {
		}

		protected override void RenderWebPart (HtmlTextWriter output) {
			try {
				if (!ProductPage.isEnabled)
					using (SPSite adminSite = ProductPage.GetAdminSite ())
						output.Write (FilterToolPart.FORMAT_INFO_CONTROL, ProductPage.GetResource ("NotEnabled", ProductPage.MergeUrlPaths (adminSite.Url, "/_layouts/roxority_FilterZen/default.aspx?cfg=enable"), "FilterZen"), "servicenotinstalled.gif", "noid");
				else if (CanRun && (DesignMode || WebPartManager.DisplayMode.AllowPageDesign || Exed || !IsEd))
					output.Write ("<div><b>{0}</b> = <i>{1}</i></div>", Context.Server.HtmlEncode (EffectiveCellFieldName), Context.Server.HtmlEncode (ProductPage.Trim (CellValue)));
				base.RenderWebPart (output);
			} catch {
			}
		}

		public void SetConsumerEntities (NamedEntityDictionary entities) {
			if (entities != null)
				using (Dictionary<string, Entity>.ValueCollection.Enumerator enumerator = entities.Values.GetEnumerator ())
					if (enumerator.MoveNext ())
						entity = enumerator.Current;
		}

		public void SetConsumerSystemInstance (LobSystemInstance lobSystemInstance) {
			systemInstance = lobSystemInstance;
		}

		protected internal object [] IdentifierValues {
			get {
				return new object [] { CellValue };
			}
		}

		protected override bool CanRun {
			get {
				return base.CanRun && BdcClientUtilAvailable && !IsPreview;
			}
		}

		protected internal RowProviderInitEventArgs RowArgs {
			get {
				List<KeyValuePair<string, string>> filterFields = new List<KeyValuePair<string, string>> ();
				BdcView view;
				if (rowArgs == null) {
					rowArgs = new RowProviderInitEventArgs ();
					try {
						if ((entity != null) && ((view = entity.GetSpecificFinderView ()) != null))
							foreach (Field f in view.Fields)
								filterFields.Add (new KeyValuePair<string, string> (f.Name, f.ContainsLocalizedDisplayName ? f.LocalizedDisplayName : f.DefaultDisplayName));
					} catch {
					}
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

		public object CellValue {
			get {
				return (IsEd ? cellValue : ProductPage.GetResource ("Nope" + (Exed ? "Expired" : "Ed"), DefaultTitle, "Ultimate"));
			}
		}

		public override string DefaultTitle {
			get {
				return this ["BdcWebPart_Title"];
			}
		}

		[Browsable (false), Category ("FilterZen"), DefaultValue ("ID"), FriendlyName ("Optionally specify the cell, column, field or filter name that the connected Web Part uses to provide the BDC Entity Instance ID to this Web Part over a non-configurable single column 'cell' connection. (Ignored if the connected Web Part provides the BDC Entity Instance ID to this Web Part via a configurable multi-column 'row' connection.)"), Personalizable (true), WebBrowsable (false), WebPartStorage (Storage.Shared)]
		public string CellFieldName {
			get {
				return cellFieldName;
			}
			set {
				cellFieldName = (string.IsNullOrEmpty (value = ProductPage.Trim (value)) ? "ID" : value);
			}
		}

		public string EffectiveCellFieldName {
			get {
				return ((cell == null) || (cell.Length == 0) || string.IsNullOrEmpty (cell [0])) ? CellFieldName : cell [0];
			}
		}

		public override bool IsConnected {
			get {
				return _cellConnected || base.IsConnected;
			}
		}

		public Entity SelectedConsumerEntity {
			get {
				return entity;
			}
		}

	}

}
