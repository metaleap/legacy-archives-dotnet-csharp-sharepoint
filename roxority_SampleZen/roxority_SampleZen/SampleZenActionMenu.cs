
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

namespace roxority_SampleZen {

	public class SampleZenActionMenu : WebControl {

		protected override void CreateChildControls () {
			string menuItemImageUrl = "/_layouts/images/PrintList/Print_34.png";
			string webUrl = SPContext.Current.Web.Url;
			string menuItemNavigateUrl = "javascript:void window.open('{0}/_layouts/roxority_SampleZen/lprnt.aspx{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}')";

			ListViewWebPart oListView = FindListView (Parent);

			if (oListView == null)
				return;

			//List Parameters
			string listIDString = oListView.ListName != string.Empty ? "?ListID=" + oListView.ListName : string.Empty;
			string viewIDString = oListView.ViewGuid != string.Empty ? "&ViewID=" + oListView.ViewGuid : string.Empty;
			string filterString = oListView.FilterString != string.Empty ? "&" + oListView.FilterString.Replace ("\\", "_BACKSLASH_") : string.Empty;
			string folderString = Page.Request.QueryString ["RootFolder"] != null ? "&RootFolder=" + Page.Request.QueryString ["RootFolder"].ToString () : string.Empty;

			//Calendar Paramaters
			string calendarDateString = Page.Request.QueryString ["CalendarDate"] != null ? "&CalendarDate=" + Page.Request.QueryString ["CalendarDate"].ToString () : string.Empty;
			string calendarPeriodString = Page.Request.QueryString ["CalendarPeriod"] != null ? "&CalendarPeriod=" + Page.Request.QueryString ["CalendarPeriod"].ToString () : string.Empty;

			//Paging Parameteres
			string pagedString = Page.Request.QueryString ["Paged"] != null ? "&Paged=" + Page.Request.QueryString ["Paged"].ToString () : string.Empty;
			string pagedPrevString = Page.Request.QueryString ["PagedPrev"] != null ? "&PagedPrev=" + Page.Request.QueryString ["PagedPrev"].ToString () : string.Empty;
			string p_File_x0020_TypeString = Page.Request.QueryString ["p_File_x0020_Type"] != null ? "&p_File_x0020_Type=" + Page.Request.QueryString ["p_File_x0020_Type"].ToString () : string.Empty;
			string p_FileLeafRefString = Page.Request.QueryString ["p_FileLeafRef"] != null ? "&p_FileLeafRef=" + Page.Request.QueryString ["p_FileLeafRef"].ToString () : string.Empty;
			string p_FSObjTypeString = Page.Request.QueryString ["p_FSObjType"] != null ? "&p_FSObjType=" + Page.Request.QueryString ["p_FSObjType"].ToString () : string.Empty;
			string p_IDString = Page.Request.QueryString ["p_ID"] != null ? "&p_ID=" + Page.Request.QueryString ["p_ID"].ToString () : string.Empty;
			string pageFirstRowString = Page.Request.QueryString ["PageFirstRow"] != null ? "&PageFirstRow=" + Page.Request.QueryString ["PageFirstRow"].ToString () : string.Empty;
			string pageLastRowString = Page.Request.QueryString ["PageLastRow"] != null ? "&PageLastRow=" + Page.Request.QueryString ["PageLastRow"].ToString () : string.Empty;

			MenuItemTemplate menu = new MenuItemTemplate ();

			string navigateUrl = string.Format (menuItemNavigateUrl, webUrl, listIDString, viewIDString, folderString, filterString, calendarDateString, calendarPeriodString, pagedString, pagedPrevString, p_File_x0020_TypeString, p_FSObjTypeString, p_FileLeafRefString, p_IDString, pageFirstRowString, pageLastRowString);

			menu.Text = "Print List";
			menu.Description = "Print the current view of this list.";
			menu.ClientOnClickNavigateUrl = navigateUrl;
			menu.ImageUrl = menuItemImageUrl;
			Controls.Add (menu);

			base.CreateChildControls ();
		}

		protected ListViewWebPart FindListView (Control oParent) {
			if (oParent is ListViewWebPart)
				return oParent as ListViewWebPart;
			if (oParent.Parent == null)
				return null;
			return FindListView (oParent.Parent);
		}

		protected override void OnLoad (EventArgs e) {
			EnsureChildControls ();
			base.OnLoad (e);
		}

	}

}
