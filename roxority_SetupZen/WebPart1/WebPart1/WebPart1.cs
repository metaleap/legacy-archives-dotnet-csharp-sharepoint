using System;
using System.Runtime.InteropServices;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Xml.Serialization;

using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;

namespace WebPart1 {
	[Guid ("c3bd9af3-8df1-49bd-835b-36f5d64b060c")]
	public class WebPart1 : System.Web.UI.WebControls.WebParts.WebPart {
		public WebPart1 () {
		}

		protected override void CreateChildControls () {
			base.CreateChildControls ();

			// TODO: add custom rendering code here.
			// Label label = new Label();
			// label.Text = "Hello World";
			// this.Controls.Add(label);
		}
	}
}
