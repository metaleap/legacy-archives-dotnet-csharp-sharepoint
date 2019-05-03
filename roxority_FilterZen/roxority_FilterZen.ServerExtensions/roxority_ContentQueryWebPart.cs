
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.WebPartPages.Communication;
using Microsoft.SharePoint.Publishing.WebControls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;

namespace roxority_FilterZen.ServerExtensions {

	[Guid ("40da97dd-4f0c-4c54-9bd8-08c799ee0e56")]
	public class roxority_Cqwp : ContentByQueryWebPart, IFilterConsumer {

		public event FilterConsumerInitEventHandler FilterConsumerInit;

		public roxority_Cqwp () {
			this.ExportMode = WebPartExportMode.All;
		}

		protected override void CreateChildControls () {
			if (!string.IsNullOrEmpty (CustomQuery))
				QueryOverride = CustomQuery;
			base.CreateChildControls ();
			QueryOverride = string.Empty;
			CommonViewFields = string.Empty;
		}

		public override ConnectionRunAt CanRunAt () {
			return ConnectionRunAt.Server;
		}

		public override void EnsureInterfaces () {
			base.EnsureInterfaces ();
			RegisterInterface ("roxContentQuery", "IFilterConsumer", 1, ConnectionRunAt.Server, this, "", "menu label", "menu desc", true);
		}

		public override void PartCommunicationConnect (string interfaceName, Microsoft.SharePoint.WebPartPages.WebPart connectedPart, string connectedInterfaceName, ConnectionRunAt runAt) {
			roxority_FilterWebPart wp = connectedPart as roxority_FilterWebPart;
			if ((wp != null) && !wp.CamlFilters)
				wp.additionalWarningsErrors.Add (wp ["CqwpCaml"]);
		}

		public override void PartCommunicationInit () {
		}

		public override void PartCommunicationMain () {
		}

		public void ClearFilter (object sender, EventArgs e) {
		}

		public void NoFilter (object sender, EventArgs e) {
		}

		public void SetFilter (object sender, SetFilterEventArgs e) {
		}

		public string CustomQuery {
			get;
			set;
		}

	}

}
