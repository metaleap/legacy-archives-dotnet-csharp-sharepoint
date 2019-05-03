
#if SP12
using Microsoft.Office.Server.ApplicationRegistry.Infrastructure;
using Microsoft.Office.Server.ApplicationRegistry.MetadataModel;
using Microsoft.Office.Server.ApplicationRegistry.Runtime;
#else
using Microsoft.BusinessData.Infrastructure;
using Microsoft.BusinessData.MetadataModel;
using Microsoft.BusinessData.Runtime;
using Microsoft.SharePoint.BusinessData.Administration;
using Microsoft.SharePoint.BusinessData.Infrastructure;
using Microsoft.SharePoint.BusinessData.MetadataModel;
#endif

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

#if SP12
using BdcEntity = Microsoft.Office.Server.ApplicationRegistry.MetadataModel.Entity;
using BdcLobSysInst = Microsoft.Office.Server.ApplicationRegistry.MetadataModel.LobSystemInstance;
using BdcView = Microsoft.Office.Server.ApplicationRegistry.MetadataModel.View;
#else
using BdcEntity = Microsoft.BusinessData.MetadataModel.IEntity;
using BdcLobSysInst = Microsoft.BusinessData.MetadataModel.ILobSystemInstance;
using BdcView = Microsoft.BusinessData.MetadataModel.IView;
#endif

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

		public event CellConsumerInitEventHandler CellConsumerInit;
		public event RowProviderInitEventHandler RowProviderInit;
		public event RowReadyEventHandler RowReady;

		private BdcEntity entity = null;
		private IEntityInstance entityInstance = null;
		private BdcLobSysInst systemInstance;
#if !SP12
		private Identity identity = null;
#endif

		private Exception regError = null;
		private bool _cellConnected = false, _rowConnected = false;
		private object cellValue = null;
		private string [] cell = null;
		private string cellFieldName = "ID";
		private DataTable rowTable = null;
		private RowProviderInitEventArgs rowArgs = null;

#if SP12
		public static BdcView GetView (BdcEntity entity) {
			return entity.GetSpecificFinderView ();
		}
#else
		public static BdcView GetView (BdcEntity entity) {
			return entity.GetDefaultSpecificFinderView ();
		}
#endif

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
			bool doit = LicEd (4) || !Exed;
			if (_cellConnected && _rowConnected) {
				if (rowTable != null)
					rowTable.Dispose ();
				rowTable = new DataTable ();
				if (RowArgs.FieldList.Length > 0) {
					foreach (string f in RowArgs.FieldList)
						rowTable.Columns.Add (f);
					if (doit)
						try {
							view = GetView (entity);
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
				RegisterInterface ("roxorityConsumeCell", InterfaceTypes.ICellConsumer, Microsoft.SharePoint.WebPartPages.WebPart.LimitOneConnection, CanRun ? ConnectionRunAt.Server : ConnectionRunAt.None, this, string.Empty, this ["GetIDFrom"], this ["GetIDFrom"], true);
				RegisterInterface ("roxorityProvideRow", InterfaceTypes.IRowProvider, Microsoft.SharePoint.WebPartPages.WebPart.UnlimitedConnections, ConnectionRunAt.Server, this, "", this ["SendRowTo"], this ["SendsRowDesc"], true);
			} catch (Exception ex) {
				regError = ex;
			}
		}

		public IEntityInstance GetEntityInstance (BdcView desiredView) {
			if (!BdcClientUtilAvailable)
				throw new Exception (this ["BdcNotAvailable"]);
			if ((desiredView != null) && !string.Equals (desiredView.Name, GetView (entity).Name))
				throw new NotSupportedException ();
			if (entityInstance == null)
#if SP12
				entityInstance = entity.FindSpecific (IdentifierValues, systemInstance);
#else
				if (Identity != null)
					try {
						entityInstance = entity.FindSpecific (identity, systemInstance);
					} catch (InvalidOperationException ex) {
						object [] arr;
						string s;
						Guid guid;
						if ((ex.Source == "Microsoft.SharePoint") && ex.Message.Contains ("'System.Guid'") && ex.Message.Contains ("'System.String'") && ((arr = IdentifierValues) != null) && (arr.Length > 0) && (!string.IsNullOrEmpty (s = arr [0] as string)) && !Guid.Empty.Equals (guid = ProductPage.GetGuid (s))) {
							arr [0] = guid;
							identity = new Identity (arr);
							entityInstance = entity.FindSpecific (identity, systemInstance);
						} else
							throw;
					}
#endif
			return entityInstance;
		}

		public string GetEntityInstanceId () {
			return EntityInstanceIdEncoder.EncodeEntityInstanceId (IdentifierValues);
		}

		public override InitEventArgs GetInitEventArgs (string interfaceName) {
			CellConsumerInitEventArgs args = (interfaceName == "roxorityConsumeCell") ? new CellConsumerInitEventArgs () : null;
			if (args != null)
				args.FieldDisplayName = args.FieldName = EffectiveCellFieldName;
			else if (interfaceName == "roxorityProvideRow")
				return RowArgs;
			return args;
		}

		[ConnectionProvider ("BdcItem", "roxorityEntityProviderInterface", typeof (BdcConnectionProvider), AllowsMultipleConnections = true)]
		public IEntityInstanceProvider GetProvider () {
			return this;
		}

		public override void PartCommunicationConnect (string interfaceName, Microsoft.SharePoint.WebPartPages.WebPart connectedPart, string connectedInterfaceName, ConnectionRunAt runAt) {
			if (interfaceName == "roxorityConsumeCell")
				_cellConnected = true;
			if (interfaceName == "roxorityProvideRow")
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
				else if (CanRun && (DesignMode || WebPartManager.DisplayMode.AllowPageDesign || Exed || !LicEd (4)))
					output.Write ("<div><b>{0}</b> = <i>{1}</i></div>", Context.Server.HtmlEncode (EffectiveCellFieldName), Context.Server.HtmlEncode (ProductPage.Trim (CellValue)));
				base.RenderWebPart (output);
			} catch {
			}
		}

		public void SetConsumerEntities (IEnumerator<BdcEntity> enumerator) {
			using (enumerator)
				if (enumerator.MoveNext ())
					entity = enumerator.Current;
		}

#if SP12
		public void SetConsumerEntities (NamedEntityDictionary entities) {
			if (entities != null)
				SetConsumerEntities (entities.Values.GetEnumerator ());
		}
#else
		public void SetConsumerEntities (IList<BdcEntity> entities) {
			if (entities != null)
				SetConsumerEntities (entities.GetEnumerator ());
		}
#endif

		public void SetConsumerSystemInstance (BdcLobSysInst lobSystemInstance) {
			systemInstance = lobSystemInstance;
		}

#if !SP12
		protected internal Identity Identity {
			get {
				object [] arr = IdentifierValues;
				if (arr != null)
					if (arr.Length == 0)
						arr = null;
					else
						foreach (object o in arr)
							if ((o == null) || ((o is string) && string.IsNullOrEmpty ((string) o))) {
								arr = null;
								break;
							}
				if ((identity == null) && (arr != null))
					identity = new Identity (IdentifierValues);
				return identity;
			}
		}
#endif

		protected internal object [] IdentifierValues {
			get {
				return new object [] { CellValue };
			}
		}

		protected internal override bool CanRun {
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
						if ((entity != null) && ((view = GetView (entity)) != null))
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
				return (LicEd (4) ? cellValue : ProductPage.GetResource ("Nope" + (Exed ? "Expired" : "Ed"), "Cell connections", "Ultimate"));
			}
		}

		[Browsable (false), Category ("FilterZen"), DefaultValue ("ID"), FriendlyName ("Optionally specify the cell, column, field or filter name that the connected Web Part uses to provide the BCS External Content Type / BDC Entity Instance ID to this Web Part over a non-configurable single column 'cell' connection. (Ignored if the connected Web Part provides the BCS/BDC Entity Instance ID to this Web Part via a configurable multi-column 'row' connection.)"), Personalizable (true), WebBrowsable (false), WebPartStorage (Storage.Shared)]
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

		public BdcEntity SelectedConsumerEntity {
			get {
				return entity;
			}
		}

	}

}
