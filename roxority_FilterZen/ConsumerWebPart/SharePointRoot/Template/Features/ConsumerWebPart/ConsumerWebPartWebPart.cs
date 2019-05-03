
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;

namespace ConsumerWebPart.UI.WebControls.WebParts {

	[Guid ("e8feb9ce-b396-44cb-9c4e-90b325390ed1")]
	public class ConsumerWebPartWebPart : Microsoft.SharePoint.WebPartPages.WebPart {

		IWebPartField field = null;
		IWebPartRow row = null;
		IWebPartTable table = null;
		IWebPartParameters param = null;
		object fieldVal = null, rowVal = null;
		ICollection tableVal = null;
		IDictionary paramsVal = null;

		public ConsumerWebPartWebPart () {
			this.ExportMode = WebPartExportMode.All;
		}

		internal void GetConsumedData () {
			if ((fieldVal == null) && (field != null))
				field.GetFieldValue (delegate (object v) {
					fieldVal = v;
				});
			if ((tableVal == null) && (table != null))
				table.GetTableData (delegate (ICollection v) {
					tableVal = v;
				});
			if ((rowVal == null) && (row != null))
				row.GetRowData (delegate (object v) {
					rowVal = v;
				});
			if ((paramsVal == null) && (param != null))
				param.GetParametersData (delegate (IDictionary v) {
					paramsVal = v;
				});
		}

		protected override void OnLoad (EventArgs e) {
			GetConsumedData ();
			base.OnLoad (e);
		}

		[ConnectionConsumer("Field Value", "fieldconsumer", AllowsMultipleConnections = true)]
		public void SetConnectionInterface (IWebPartField provider) {
			field = provider;
		}

		[ConnectionConsumer ("Parameters", "paramsconsumer", AllowsMultipleConnections = true)]
		public void SetConnectionInterface (IWebPartParameters provider) {
			param = provider;
		}

		[ConnectionConsumer ("Row Data", "rowconsumer", AllowsMultipleConnections = true)]
		public void SetConnectionInterface (IWebPartRow provider) {
			row = provider;
		}

		[ConnectionConsumer ("Table Data", "tableconsumer", AllowsMultipleConnections = true)]
		public void SetConnectionInterface (IWebPartTable provider) {
			table = provider;
		}

		protected override void RenderContents (HtmlTextWriter writer) {
			GetConsumedData ();
			writer.WriteLine ("<div>Field value: {0}</div>", fieldVal);
			writer.WriteLine ("<div>Row values: {0}</div>", rowVal);
			writer.WriteLine ("<div>Table values: {0}</div>", tableVal);
			writer.WriteLine ("<div>Param values: {0}</div>", paramsVal);
			base.RenderContents (writer);
		}

	}

}
