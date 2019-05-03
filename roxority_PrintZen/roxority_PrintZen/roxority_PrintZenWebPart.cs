
using Microsoft.SharePoint;
using roxority.SharePoint;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace roxority_PrintZen {

	[Guid("691f8f9e-376b-483b-93bf-9ee92bce00ba")]
	public class roxority_PrintZenWebPart : WebPartBase {

		public roxority_PrintZenWebPart () {
			urlPropertyPrefix = "print";
		}

		protected override void CreateChildControls () {
			base.CreateChildControls ();
		}

		protected override void OnLoad (EventArgs e) {
			EnsureChildControls ();
			base.OnLoad (e);
		}

	}

}
