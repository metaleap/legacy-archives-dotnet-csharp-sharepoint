
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using roxority.SharePoint;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.UI;

namespace roxority_UploadZen {

	public class MultiUploadMenu : UploadMenu {

		//protected override void OnLoad (EventArgs e) {
		//    string temp;
		//    EnsureChildControls ();
		//    base.OnLoad (e);
		//    if (template == null)
		//        template = MenuTemplateControl.FindControl ("uploadzen_" + List.DefaultViewUrl) as MenuItemTemplate;
		//    if ((template != null) && (template.Visible = (!ProductPage.Config<bool> (SPContext.Current, "HideAllItems")))) {
		//        if (ProductPage.isEnabled) {
		//            template.ClientOnClickScript = "window.location = \'" + (template.ClientOnClickNavigateUrl = temp = CompleteUrl (Web, template.ClientOnClickNavigateUrl, true)) + "\';";
		//            template.ClientOnClickNavigateUrl = temp;
		//        } else
		//            using (SPSite adminSite = ProductPage.GetAdminSite ())
		//                template.ClientOnClickScript = "if(confirm('" + (SPEncode.ScriptEncode (ProductPage.GetResource ("NotEnabledPlain", temp = ProductPage.MergeUrlPaths (adminSite.Url, "/_layouts/roxority_UploadZen.aspx?cfg=enable"), "UploadZen")) + "\\n\\n" + SPEncode.ScriptEncode (ProductPage.GetResource ("NotEnabledPrompt"))) + "'))location.href='" + temp + "';";
		//        if (enabled = HasVersion)
		//            template.Attributes.Remove ("disabled");
		//        else
		//            template.Attributes ["disabled"] = "disabled";
		//        if ((!enabled) && ProductPage.Config<bool> (SPContext.Current, "HideDisabledItems"))
		//            template.Visible = false;
		//    }
		//}

		//protected override void Render (System.Web.UI.HtmlTextWriter output) {
		//    if (enabled && !ProductPage.Config<bool> (SPContext.Current, "DontHideUploadItem"))
		//        output.Write ("<script type=\"text/javascript\" language=\"JavaScript\"> GetMultipleUploadEnabled = function() { return false; }; </script>");
		//    base.Render (output);
		//}

	}

}
