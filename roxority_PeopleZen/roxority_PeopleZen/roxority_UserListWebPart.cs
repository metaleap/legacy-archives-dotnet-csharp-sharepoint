
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebPartPages;
using roxority.SharePoint;
using roxority_RollupZen;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace roxority_PeopleZen {

	[Guid ("14711c53-ede9-4c31-8bfb-4124428a4aa5")]
	public class roxority_UserListWebPart : roxority_RollupZen.RollupWebPart {

		public roxority_UserListWebPart () {
			urlPropertyPrefix = "people_";
		}

	}

}
