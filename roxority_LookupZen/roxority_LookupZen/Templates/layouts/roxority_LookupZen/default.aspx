<%@ Page Language="C#" Inherits="roxority.SharePoint.ProductPage, roxority_LookupZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" MasterPageFile="~/_layouts/simple.master" EnableEventValidation="false" ValidateRequest="false" AutoEventWireup="false" %>
<%@ Register TagPrefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" Src="~/_controltemplates/ButtonSection.ascx" %>
<%@ Register TagPrefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Import Namespace="Microsoft.SharePoint.Administration" %>
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %>
<%@ Import Namespace="System.Collections.Generic" %>
<script runat="server">
	IDictionary sd = null, dic = null;
	Dictionary<string, int> wssDict = null;
	TimeSpan timeSpan;
	SPSite lsite;
	KeyValuePair<string, object> userkvp = new KeyValuePair<string, object> ();
	SPWebApplication webApp = null;
	string [] homeHelpTopics = GetProductResource ("_HomeHelpTopics").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
	string exmsg, siteTitle, cfgGroup = string.Empty, lastCfgGroup = string.Empty;
	bool cfgHasSiteVal, hasThisSite = false, hasHomeTopics;
	int hiddenTopics = 0, wssItemCount;
</script>
<asp:Content ID="PageTitleContent" ContentPlaceHolderID="PlaceHolderPageTitle" runat="server">
	<%= this ["Tab_" + ((IsDocTopic) ? "Help" : "Info")]%> - <%= this ["SiteTitle", ProductName]%>
</asp:Content>
<asp:Content ID="PageTitleInTitleAreaContent" ContentPlaceHolderID="PlaceHolderPageTitleInTitleArea" runat="server">
	<%= (IsDocTopic ? GetProductResource ("HelpTopic_" + DocTopic) : this ["WelcomeTitle", ProductName, DisplayVersion])%>
</asp:Content>
<asp:Content ID="AdditionalPageHeadContent" ContentPlaceHolderID="PlaceHolderAdditionalPageHead" runat="server">
	<meta http-equiv="imagetoolbar" content="no" />
	<link rel="stylesheet" type="text/css" href="help/res/help.css" />
	<style type="text/css">
	td#GlobalTitleAreaImage 
	{
		background-image: url('<%= GetProductResource ("_LogoImageUrl") %>');
	}
	</style>
	<script language="javascript" type="text/javascript" src="jQuery.js">
	</script>
	<script type="text/javascript" language="JavaScript">
		var roxBaseUrl = '<%= GetProductResource ("_WhiteLabelUrl")%>';
	</script>
	<script language="javascript" type="text/javascript" src="roxbase.js">
	</script>
	<script language="javascript" type="text/javascript" src="roxority.js">
	</script>
	<script language="javascript" type="text/javascript">
		roxCfgPageTitle = '<%= this ["CfgPage"]%>';
		prodName = '<%= ProductName %>';
		prodNameLower = '<%= ProductName.ToLowerInvariant () %>';
		<%
			if (IsDocTopic)
				foreach (string id in HelpTopicIDs)
					if (id != DefaultTopicID)
						if (IsAnyAdmin || (Array.IndexOf<string> (AdminHelpTopicIDs, id) < 0)) {
					%>
						inner += ('<tr><td><table class="ms-navitem' + (('<%= id %>' == '<%= DocTopic %>') ? ' ms-selectednav' : '') + '" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a href="?doc=<%= id %>" class="' + (('<%= id %>' == '<%= DocTopic %>') ? ' ms-selectednav' : '') + '" style="border-style: none !important; font-size: 1em;"><%= GetProductResource ("HelpTopic_" + id) %></a></td></tr></table></td></tr>');
					<%
					} else
						hiddenTopics++;
			if (hiddenTopics > 0) {
			%>
				inner += ('<tr><td><table class="ms-navitem" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a href="<%= SPContext.Current.Site.Url %>/_layouts/AccessDenied.aspx?loginasanotheruser=true&Source=<%= Server.UrlEncode (SPContext.Current.Site.Url + "/_layouts/" + WhiteLabel + ProductName + "/default.aspx?doc=intro") %>&ReturnUrl=<%= Server.UrlEncode (SPContext.Current.Site.Url + "/_layouts/" + WhiteLabel + ProductName + "/default.aspx?doc=intro") %>" style="border-style: none !important; font-size: 1em;"><%= GetResource ("AdminHelpTopic") %></a></td></tr></table></td></tr>');
			<%
			}
			if (!IsAnyAdmin) {
		%>
		var ids = ['roxsavebutton', 'licremove', 'licfile'];
		jQuery(document).ready(function() {
			var elem;
			for (var i = 0; i < ids.length; i++) if ((elem = document.getElementById(ids[i])) != null) elem.disabled = true;
		});
		<%
			} else {
		%>
		jQuery(document).ready(function() {
			jQuery('.roxadmintopic').css({ 'display': 'inline' });
		});
		<%
			}
		%>
		jQuery(document).ready(function() {
			var tmp, pos = location.href.lastIndexOf('#') + 5;
			jQuery('.roxcfgrow-').show();
			if (pos) {
				jQuery('.roxcfgrow-' + location.href.substr(pos)).click();
				if ((tmp = jQuery('#tr_' + location.href.substr(pos)).prevAll('tr.roxgrouprow')) && tmp.length)
					jQuery(tmp[0]).children('.roxcfggroup').click();
			}
		});
	</script>
</asp:Content>
<asp:Content ID="GlobalNavigationContent" ContentPlaceHolderID="PlaceHolderGlobalNavigation" runat="server">
	<!-- hides default rendering of wss help button -->
</asp:Content>
<asp:Content ID="SiteNameContent" ContentPlaceHolderID="PlaceHolderSiteName" runat="server">
	<h2 class="ms-sitetitle"><%= this ["SiteTitle", ProductName]%></h2>
</asp:Content>
<asp:Content ID="SearchAreaContent" ContentPlaceHolderID="PlaceHolderSearchArea" runat="server">
	<div style="white-space: nowrap">
		<nobr>
			<%= this ["LicSite"]%>
			<select onchange="roxGoSite(this.options[this.selectedIndex].value);"<%= (((!IsAnyAdmin) || (SPContext.Current.Site.WebApplication.Sites.Count > 16)) ? " disabled=\"disabed\"" : string.Empty) %>>
			<%
				SPSecurity.CatchAccessDeniedException = false;
				try {
					SPContext.Current.Site.CatchAccessDeniedException = false;
					siteTitle = SPContext.Current.Site.RootWeb.Title;
				} catch {
					siteTitle = SPContext.Current.Site.Url;
				}
				try {
					webApp = SPContext.Current.Site.WebApplication;
				} catch {
				}
				if ((webApp==null)|| (webApp.Sites.Count > 16) || (!IsAnyAdmin)) {
			%>
			<option value="<%= SPContext.Current.Site.Url %>" selected="selected"><%= siteTitle %></option>
			<%
				} else
					foreach (SPSite sc in TryEach<SPSite> (webApp.Sites))
						using (sc) {
							if (SPContext.Current.Site.ID == sc.ID)
								hasThisSite = true;
				%>
				<option value="<%= sc.Url %>"<%= ((SPContext.Current.Site.ID == sc.ID) ? " selected=\"selected\"" : "") %>><%= (sc.RootWeb == null) ? sc.Url : sc.RootWeb.Title%></option>
				<%
						}
				if (!hasThisSite) {
			%>
			<option value="<%= SPContext.Current.Site.Url %>" selected="selected"><%= siteTitle %></option>
			<%
				}
				%>
			</select>
			<%
				if (IsAnyAdmin && (webApp != null)) {
			%>
			<%= this ["WebApp"]%>
			<select onchange="roxGoSite(this.options[this.selectedIndex].value);">
			<%
				IEnumerable<SPWebApplication> webApps = GetWebApps (SPContext.Current, false);
				if (webApps != null)
					foreach (SPWebApplication app in webApps)
						try {
							if (app.Sites.Count > 0)
								using (SPSite appSite = app.Sites [0]) {
									appSite.CatchAccessDeniedException = false;
								%>
								<option value="<%= appSite.Url %>"<%= ((webApp.Id == app.Id) ? " selected=\"selected\"" : "") %>><%= (string.IsNullOrEmpty (app.DisplayName) ? (string.IsNullOrEmpty (app.Name) ? ("[ " + appSite.Url + " ]") : app.Name) : app.DisplayName)%></option>
								<%
								}
						} catch {
						}
			%>
			</select>
			<%
				}
			%>
			&nbsp;
		</nobr>
	</div>
</asp:Content>
<asp:Content ID="TopNavBarContent" ContentPlaceHolderID="PlaceHolderTopNavBar" runat="server">
	<table class="ms-bannerframe" border="0" cellspacing="0" cellpadding="0" width="100%">
		<tr>
			<td nowrap="nowrap" valign="middle"></td>
			<td class="ms-banner" width="99%" nowrap="nowrap">
				<table id="zz1_TopNavigationMenu" class="ms-topNavContainer zz1_TopNavigationMenu_5 zz1_TopNavigationMenu_2" cellpadding="0" cellspacing="0" border="0">
					<tr>
						<td>
							<table class="ms-topnav" cellpadding="0" cellspacing="0" border="0" width="100%">
								<tr>
									<td style="white-space:nowrap;"><a class="<%= (IsDocTopic) ? "ms-topnav" : "ms-topnavselected" %>" href="?" style="border-style: none; font-size: 1em;"><%= this [IsAnyAdmin ? "Tab_Info" : "Tab_Home"] %></a></td>
								</tr>
							</table>
						</td>
						<td>
							<table class="ms-topnav" cellpadding="0" cellspacing="0" border="0" width="100%">
								<tr>
									<td style="white-space:nowrap;"><a class="<%= (IsDocTopic) ? "ms-topnavselected" : "ms-topnav" %>" href="?doc=<%= DefaultTopicID %>" style="border-style: none; font-size: 1em;"><%= this ["Tab_Help"] %></a></td>
								</tr>
							</table>
						</td>
						<td width="99%">
							<div style="text-align: right; white-space: nowrap;">by <a target="_blank" style="color: #42A00C;" href="<%= GetProductResource ("_WhiteLabelUrl")%>"><b><%= GetProductResource ("_WhiteLabelTitle")%></b></a></div>
						</td>
						<td style="width:0px;"></td>
					</tr>
				</table>
			</td>
			<td class="ms-banner">&nbsp;&nbsp;</td>
			<td></td>
		</tr>
	</table>
</asp:Content>
<asp:Content ID="PageImageContent" ContentPlaceHolderID="PlaceHolderPageImage" runat="server">
<!--img id="ctl00_PlaceHolderPageImage_ctl00" src="/_layouts/images/<%= IsDocTopic ? "POSTS.PNG" : "SURVEY.PNG" %>" alt="" /-->
</asp:Content>
<asp:Content ID="TitleBreadcrumbContent" ContentPlaceHolderID="PlaceHolderTitleBreadcrumb" runat="server">
	<%= this ["Tab_" + ((IsDocTopic) ? "Help" : "Info")]%>
</asp:Content>
<asp:Content ID="LeftNavBarContent" ContentPlaceHolderID="PlaceHolderLeftNavBar" runat="server">
	<div class="ms-quicklaunchouter">
		<div class="ms-quicklaunch" style="width: 100%;">
			<div>
				<div>
					<table class="ms-navsubmenu1" border="0" cellpadding="0" cellspacing="0">
						<%
							if (WebVersion != DisplayVersion) {
						%>
						<tr>
							<td>
								<table class="ms-navheader" border="0" cellpadding="0" cellspacing="0" style="width: 100%;">
									<tr><td style="width: 100%;"><b class="ms-navheader" style="font-size: 1em; border-style: none !important"><%= this["NavUpdates"]%></b></td></tr>
								</table>
							</td>
						</tr>
						<tr>
							<td>
								<table id="Table2" class="ms-navSubMenu2" width="100%" border="0" cellpadding="0" cellspacing="0">
									<tr><td><table class="ms-navitem" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><%= this ["NavUpdates_YourVersion", DisplayVersion]%></td></tr></table></td></tr>
									<tr><td><table class="ms-navitem" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a href="<%= GetProductResource ("_WhiteLabelUrl")%><%= ProductName.ToLower ()%>-documentation/release-notes.html" style="border-style: none !important; font-size: 1em;" target="_blank"><%= this ["NavUpdates_NewVersion", WebVersion]%></a></td></tr></table></td></tr>
									<tr><td><table class="ms-navitem" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a href="<%= GetProductResource ("_WhiteLabelUrl")%>storage/sharepoint/<%= ProductName.ToLower ()%>/<%= WhiteLabel + ProductName %>.zip" style="border-style: none !important; font-size: 1em;"><%= this ["NavUpdates_Download"]%></a></td></tr></table></td></tr>
								</table>
							</td>
						</tr>
						<%
							}
						%>
						<tr>
							<td>
								<table class="ms-navheader<%= (IsDocTopic ? "" : (string.IsNullOrEmpty (Request ["cfg"]) ? " ms-selectednavheader" : string.Empty)) %>" border="0" cellpadding="0" cellspacing="0" style="width: 100%;">
									<tr><td style="width: 100%;"><a class="ms-navheader<%= (IsDocTopic ? "" : (string.IsNullOrEmpty (Request ["cfg"]) ? " ms-selectednavheader" : string.Empty)) %>" href="?" style="font-size: 1em; border-style: none !important"><%= this [IsAnyAdmin ? "Tab_Info" : "Tab_Home"]%></a></td></tr>
								</table>
							</td>
						</tr>
						<%
							if (!IsDocTopic) {
						%>
						<tr>
							<td>
								<table class="ms-navSubMenu2" width="100%" border="0" cellpadding="0" cellspacing="0">
									<%
										if (IsFarmAdmin && (!string.IsNullOrEmpty (GetProductResource ("_Tools")))) {
									%>
									<tr><td><table class="ms-navitem<%= ((Request ["cfg"] == "tools") ? " ms-selectednav" : string.Empty) %>" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a href="?cfg=tools"><%= this ["ToolsFrame"]%></a></td></tr></table></td></tr>
									<%
										}
										if (IsAnyAdmin) {
									%>
									<tr><td><table class="ms-navitem<%= ((Request ["cfg"] == "wss") ? " ms-selectednav" : string.Empty) %>" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a href="?cfg=wss"><%= this ["WssFrame"]%></a></td></tr></table></td></tr>
									<tr><td><table class="ms-navitem<%= ((Request ["cfg"] == "cfg") ? " ms-selectednav" : string.Empty) %>" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a href="?cfg=cfg"><%= this ["CfgFrame"]%></a></td></tr></table></td></tr>
									<tr><td><table class="ms-navitem<%= ((Request ["cfg"] == "lic") ? " ms-selectednav" : string.Empty) %>" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a href="?cfg=lic"><%= this ["LicFrame"]%></a></td></tr></table></td></tr>
									<%
										}
									%>
								</table>
							</td>
						</tr>
						<%
							}
						%>
						<tr>
							<td>
								<table class="ms-navheader<%= ((DocTopic == DefaultTopicID) ? " ms-selectednavheader" : "") %>" border="0" cellpadding="0" cellspacing="0" style="width: 100%;">
									<tr><td style="width: 100%;"><a class="ms-navheader<%= ((DocTopic == DefaultTopicID) ? " ms-selectednavheader" : "") %>" href="?doc=<%= DefaultTopicID %>" style="font-size: 1em; border-style: none !important"><%= this ["Tab_Help"]%></a></td></tr>
								</table>
							</td>
						</tr>
						<%
							if (IsDocTopic) {
						%>
						<tr>
							<td>
								<table id="roxDocToc" class="ms-navSubMenu2" width="100%" border="0" cellpadding="0" cellspacing="0">
								</table>
							</td>
						</tr>
						<%
							}
						%>
						<tr>
							<td>
								<table class="ms-navheader" border="0" cellpadding="0" cellspacing="0" style="width: 100%;">
									<tr><td style="width: 100%;"><b class="ms-navheader" style="font-size: 1em; border-style: none !important"><%= this ["NavFeedback"]%></b></td></tr>
								</table>
							</td>
						</tr>
						<tr>
							<td>
								<table id="Table1" class="ms-navSubMenu2" width="100%" border="0" cellpadding="0" cellspacing="0">
									<tr><td><table class="ms-navitem" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a target="_blank" href="<%= GetProductResource ("_WhiteLabelUrl")%><%= ProductName.ToLower ()%>-forum/" style="border-style: none !important; font-size: 1em;"><%= this ["NavForum", ProductName]%></a></td></tr></table></td></tr>
									<tr><td><table class="ms-navitem" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a target="_blank" href="<%= GetProductResource ("_WhiteLabelUrl")%><%= ProductName.ToLower ()%>-support/" style="border-style: none !important; font-size: 1em;"><%= this ["NavSupport"]%></a></td></tr></table></td></tr>
									<tr><td><table class="ms-navitem" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a target="_blank" href="<%= GetProductResource ("_WhiteLabelUrl")%>" style="border-style: none !important; font-size: 1em;"><%= GetProductResource ("_WhiteLabelTitle")%></a><br /><%= GetProductResource ("_WhiteLabelAddress")%><br /><a target="_blank" href="<%= GetProductResource ("_WhiteLabelUrl")%><%= GetProductResource ("_WhiteLabelUrlContact")%>" style="border-style: none !important; font-size: 1em;"><%= GetProductResource ("_WhiteLabelEmail")%></a></td></tr></table></td></tr>
								</table>
							</td>
						</tr>
					</table>
				</div>
			</div>
		</div>
	</div>
</asp:Content>
<asp:Content ID="MainContent" ContentPlaceHolderID="PlaceHolderMain" runat="server">
	<table width="100%" class="ms-propertysheet" style="background: #fff url('/_layouts/images/settingsgraphic.jpg') right top repeat-x" cellspacing="0" cellpadding="0" border="0">
		<tr>
			<td class="ms-descriptionText">
				<%
					if (IsDocTopic) {
				%>
				<div id="rox_page_help">
					<%= TopicContent%>
				</div>
				<%
					} else {
						sd = Status;
						dic = (sd.Count > 0) ? (In (sd) as IDictionary) : null;
				%>
				<div id="rox_page_info">
					<%
						if (postEx != null) {
					%>
					<div class="rox-info" style="background-image: url('/_layouts/images/exclaim.gif');">
						<%= postEx.Message%>
					</div>
					<%
						} else {
					%>
					<div class="ms-descriptiontext">
						<%= (string.IsNullOrEmpty (GetProductResource ("WelcomeMessage")) ? this ["WelcomeMessage", ProductName, ProductName.ToLowerInvariant ()] : GetProductResource ("WelcomeMessage", ProductName, ProductName.ToLowerInvariant ()))%>
					</div>
					<%
						}
						if (CfgTopic == "wss") {
					%>
					<br />
					<br />
					<div class="rox-tabrow">
						<div class="ms-templatepickerunselected"><div><a href="?"><%= this ["HomeFrame"]%></a></div></div>
						<%
							if (IsFarmAdmin && (!string.IsNullOrEmpty (GetProductResource ("_Tools")))) {
						%>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=tools"><%= this ["ToolsFrame"]%></a></div></div>
						<%
							}
							if (IsAnyAdmin) {
						%>
						<div class="ms-templatepickerselected"><div><a href="?cfg=wss"><%= this ["WssFrame"]%></a></div></div>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=cfg"><%= this ["CfgFrame"]%></a></div></div>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=lic"><%= this ["LicFrame"]%></a></div></div>
						<%
							}
						%>
						<div class="rox-templatepickerspacer">&nbsp;<%= this ["NoAdmin"]%>&nbsp;</div>
					</div>
					<div class="rox-tabcontainer">
						<table class="ms-propertysheet" cellspacing="0" cellpadding="0" border="0" width="100%">
							<tr>
								<td class="ms-descriptiontext" colspan="3">
									<div><%= this ["WssInfo", ProductName]%></div>
								</td>
							</tr>
							<%
								foreach (Dictionary<string, string> dict in WssItems) {
							%>
							<tr>
								<td class="ms-sectionline" colspan="3" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr>
								<td class="ms-descriptiontext" valign="top" style="background: url('/_layouts/images/<%= dict ["icon"]%>') no-repeat 6px 14px;">
									<div style="white-space: nowrap; padding-left: 36px;"><%= this ["WssItem_" + dict ["type"]]%>:<br /><<%= dict.ContainsKey ("link") ? "a" : "span"%> target="<%= dict.ContainsKey ("link") ? "_blank" : "_self"%>" href="<%= dict.ContainsKey ("link") ? dict ["link"] : "#"%>" style="text-decoration: none !important;"><b><%= dict ["title"]%></b></<%= dict.ContainsKey ("link") ? "a" : "span"%>></div>
								</td>
								<td class="ms-authoringcontrols" valign="top" align="left">
										<%= dict ["info"]%>
								</td>
								<td class="ms-authoringcontrols" valign="top" align="left">
									<div class="rox-wssinfo">
										<%= dict ["desc"]%>
									</div>
								</td>
							</tr>
							<%
								}
							%>
						</table>
					</div>
					<br />
					<br />
					<%
						} else if (CfgTopic == "tools") {
					%>
					<br />
					<br />
					<div class="rox-tabrow">
						<div class="ms-templatepickerunselected"><div><a href="?"><%= this ["HomeFrame"]%></a></div></div>
						<%
							if (IsFarmAdmin && (!string.IsNullOrEmpty (GetProductResource ("_Tools")))) {
						%>
						<div class="ms-templatepickerselected"><div><a href="?cfg=tools"><%= this ["ToolsFrame"]%></a></div></div>
						<%
						}
							if (IsAnyAdmin) {
						%>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=wss"><%= this ["WssFrame"]%></a></div></div>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=cfg"><%= this ["CfgFrame"]%></a></div></div>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=lic"><%= this ["LicFrame"]%></a></div></div>
						<%
							}
						%>
						<div class="rox-templatepickerspacer">&nbsp;<%= this ["NoAdmin"]%>&nbsp;</div>
					</div>
					<div class="rox-tabcontainer">
						<table class="ms-propertysheet" cellspacing="0" cellpadding="0" border="0" width="100%">
							<tr>
								<td class="ms-descriptiontext" colspan="2">
									<div><%= (string.IsNullOrEmpty (Request ["tool"]) ? this ["ToolsInfo", ProductName] : GetProductResource (Request ["tool"] + "_Desc"))%></div>
								</td>
							</tr>
							<%
								foreach (string tool in GetProductResource ("_Tools").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
									if (string.IsNullOrEmpty (Request ["tool"]) || (Request ["tool"] == tool)) {
							%>
							<tr>
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr>
								<td class="ms-descriptiontext" colspan="<%= ((Request ["tool"] == tool) ? 2 : 1) %>" valign="top" style="<%= ((Request ["tool"] == tool) ? "background: InfoBackground; color: InfoText;" : string.Empty)%>">
									<div style="background: url('<%= GetProductResource (tool + "_Icon")%>') no-repeat left top; padding: 0px 0px 0px 20px; white-space: nowrap;"><<%= ((Request ["tool"] == tool) ? "b" : "a") %> href="?cfg=tools&tool=<%= tool%>"><%= GetProductResource (tool + "_Title")%></<%= ((Request ["tool"] == tool) ? "b" : "a") %>></div>
								</td>
								<%
								if (Request ["tool"] == tool) {
							%>
							</tr>
							<tr>
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr>
							<%
								}
								%>
								<td class="ms-authoringcontrols" colspan="<%= ((Request ["tool"] == tool) ? 2 : 1) %>" valign="top" align="left">
									<%
								if (Request ["tool"] == tool)
									using (Control ctl = LoadControl ("/_controltemplates/" + WhiteLabel + ProductName + "/" + tool + ".ascx"))
										ctl.RenderControl (__w);
								else {
									%>
									<%= GetProductResource (tool + "_Desc")%>
									<%
								}
									%>
								</td>
							</tr>
							<%
								}
							%>
						</table>
					</div>
					<br />
					<br />
					<%
						} else if (CfgTopic == "cfg") {
					%>
					<br />
					<br />
					<div class="rox-tabrow">
						<div class="ms-templatepickerunselected"><div><a href="?"><%= this ["HomeFrame"]%></a></div></div>
						<%
							if (IsFarmAdmin && (!string.IsNullOrEmpty (GetProductResource ("_Tools")))) {
						%>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=tools"><%= this ["ToolsFrame"]%></a></div></div>
						<%
						}
							if (IsAnyAdmin) {
						%>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=wss"><%= this ["WssFrame"]%></a></div></div>
						<div class="ms-templatepickerselected"><div><a href="?cfg=cfg"><%= this ["CfgFrame"]%></a></div></div>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=lic"><%= this ["LicFrame"]%></a></div></div>
						<%
							}
						%>
						<div class="rox-templatepickerspacer">&nbsp;<%= this ["NoAdmin"]%>&nbsp;</div>
					</div>
					<div class="rox-tabcontainer">
						<table class="ms-propertysheet" cellspacing="0" cellpadding="0" border="0" width="100%">
							<tr>
								<td class="ms-descriptiontext" colspan="3">
									<div><%= this ["CfgInfo"]%></div>
								</td>
							</tr>
							<tr>
								<td class="ms-sectionline" colspan="3" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr>
								<td class="ms-descriptiontext" valign="top" align="left" width="40%" style="width: 40% !important; background: url('/_layouts/images/hhelp.gif') no-repeat 6px 24px;">
								<b>&nbsp;</b><br />&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;<%= this ["CfgHelp"]%>
								</td>
								<td class="ms-descriptiontext" valign="top" align="left" width="30%" style="width: 30% !important;">
								<%= this ["CfgSettingFarm"]%>
								</td>
								<td class="ms-descriptiontext" valign="top" align="left" width="30%" style="width: 30% !important;">
								<%= this ["CfgSettingSite", SPContext.Current.Site.RootWeb.Title]%>
								</td>
							</tr>
							<%
						foreach (Dictionary<string, string> dict in ConfigSettings) {
							cfgHasSiteVal = ConfigHasSiteValue (GetContext (), null, dict ["name"]);
							if (ConfigGroups.TryGetValue (dict ["name"], out cfgGroup)) {
								lastCfgGroup = cfgGroup;
								%>
								<tr>
									<td class="ms-sectionline" colspan="3" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
								</tr>
								<tr class="roxgrouprow">
									<td colspan="3" onclick="jQuery('.roxcfgrow').hide(function() { jQuery('.roxcfgrow-').show(); jQuery('.roxcfgrow-<%= cfgGroup %>').show(); }); jQuery('.roxcfggroup').css({ 'background-image': 'url(/_layouts/images/collapseplus.gif)' }); jQuery('.roxcfggroup-<%= cfgGroup %>').css({ 'background-image': 'url(/_layouts/images/collapseminus.gif)' }); jQuery('.roxConfigDesc').hide(); jQuery('#cfgdesc_<%= dict ["name"]%>').show();" class="roxcfggroup roxcfggroup-<%= cfgGroup %>"><span class="ms-sitetitle" style="font-weight: 700 !important;" href="#noop"><%= GetProductResource ("CfgSettingGroup_" + cfgGroup)%></span></td>
								</tr>
								<%
						} else
								cfgGroup = lastCfgGroup;
							%>
							<tr class="roxcfgrow roxcfgrow-<%= cfgGroup %>" id="tr_<%= dict ["name"] %>" style="display: none">
								<td class="ms-sectionline" colspan="3" height="1"><a name="cfg_<%= dict ["name"] %>"><img height="1" src="/_layouts/images/blank.gif" width="1" /></a></td>
							</tr>
							<tr class="roxcfgrow roxcfgrow-<%= cfgGroup %> roxcfgrow-<%= dict ["name"]%>" style="display: none" onclick="jQuery('.roxConfigDesc').hide(); jQuery('#cfgdesc_<%= dict ["name"]%>').show();">
								<td class="ms-descriptiontext" valign="top" width="40%" style="width: 40% !important;">
									<div style="height: 32px;"><b><%= ((dict ["type"] == "bool") ? "&nbsp;" : dict ["title"])%></b></div>
									<div class="roxConfigDesc" style="display: none;" id="cfgdesc_<%= dict ["name"]%>"><%= dict ["desc"]%></div>
								</td>
								<%
						foreach (string scope in new string [] { "farm", "site" }) {
								%>
								<td class="ms-authoringcontrols ms-halfinputformcontrols" valign="top" align="left" width="30%" style="width: 30% !important;">
									<div style="padding-bottom: 4px; height: 32px; white-space: nowrap; visibility: <%= ((scope == "farm") ? "hidden" : "visible") %>;">
										<input onclick="document.getElementById('label_<%= scope %>_<%= dict ["name"] %>').style.textDecoration=(this.checked?'none':'line-through');if(this.checked){roxGoReset('<%= dict ["name"] %>', '<%= scope %>');}else{document.getElementById('content_<%= scope %>_<%= dict ["name"]%>').style.visibility='visible';}" name="chk_<%= scope %>_<%= dict ["name"] %>" id="chk_<%= scope %>_<%= dict ["name"] %>" type="checkbox" <%= (cfgHasSiteVal ? "" : "checked=\"checked\"") %> <%= ((IsSiteAdmin && !IsFarmOnlySetting (dict ["name"])) ? "" : "disabled=\"disabled\"") %> />
										<label id="label_<%= scope %>_<%= dict ["name"] %>" for="chk_<%= scope %>_<%= dict ["name"] %>" style="white-space: nowrap; text-decoration: <%= (cfgHasSiteVal ? "line-through" : "none") %>;"><%= this ["CfgUseFarm"]%></label>
									</div>
									<div id="content_<%= scope %>_<%= dict ["name"]%>" style="visibility: <%= (((scope != "site") || cfgHasSiteVal) ? "visible" : "hidden") %>;">
									<%
						if (dict ["type"] == "bool") {
									%>
									<input onchange="roxGoSave('<%= dict ["name"] %>', '<%= scope %>');" onclick="roxGoSave('<%= dict ["name"] %>', '<%= scope %>');" type="checkbox" name="cfg_<%= scope %>_<%= dict ["name"] %>" id="cfg_<%= scope %>_<%= dict ["name"] %>"<%= Config<bool> (SPContext.Current, scope + ":" + dict ["name"]) ? " checked=\"checked\"" : "" %> <%= ((((scope == "farm") && !IsFarmAdmin) || ((scope == "site") && !IsSiteAdmin)) ? "disabled=\"disabled\"" : "") %> />
									<label for="cfg_<%= scope %>_<%= dict ["name"] %>"><%= dict ["title"]%></label>
									<%
						} else if (dict ["type"] == "text") {
									%>
									<textarea style="width: 99%;" rows="10" wrap="off" onchange="document.getElementById('reset_<%= scope %>_<%= dict ["name"]%>').style.display='<%= ((dict ["default"] == null) ? "none" : "inline-block") %>';document.getElementById('save_<%= scope %>_<%= dict ["name"]%>').style.display='inline-block';" class="ms-input" name="cfg_<%= scope %>_<%= dict ["name"] %>" id="cfg_<%= scope %>_<%= dict ["name"] %>" <%= ((((scope == "farm") && !IsFarmAdmin) || ((scope == "site") && !IsSiteAdmin)) ? "disabled=\"disabled\"" : "") %>><%= Config (SPContext.Current, scope + ":" + dict ["name"])%></textarea>
									<div style="text-align: <%= ((scope == "site") ? "right" : "left") %>; display: <%= ((((scope == "farm") && !IsFarmAdmin) || ((scope == "site") && !IsSiteAdmin)) ? "none" : "block") %>;">
									<a id="save_<%= scope %>_<%= dict ["name"]%>" style="display: none; height: 24px; padding: 0px 0px 0px 20px; background: url('/_layouts/images/saveitem.gif') 0px 0px no-repeat;" href="javascript:roxGoSave('<%= dict ["name"] %>', '<%= scope %>');"><%= this ["CfgSave"]%></a>
									<a id="reset_<%= scope %>_<%= dict ["name"] %>" style="visibility: <%= ((scope == "farm") ? "visible" : "hidden") %>; display: <%= ((Config (SPContext.Current, dict ["name"]) == dict ["default"]) ? "none" : "inline-block") %>; height: 24px; padding: 0px 0px 0px 20px; background: url('/_layouts/images/undo.gif') 0px 0px no-repeat;" href="javascript:roxGoReset('<%= dict ["name"] %>', '<%= scope %>');"><%= this ["CfgReset"]%></a>
									</div>
									<%
						} else if ((dict ["type"] == "string") || (dict ["type"] == "password")) {
									%>
									<input style="width: 99% !important;" onchange="document.getElementById('reset_<%= scope %>_<%= dict ["name"]%>').style.display='<%= ((dict ["default"] == null) ? "none" : "inline-block") %>';document.getElementById('save_<%= scope %>_<%= dict ["name"]%>').style.display='inline-block';" class="ms-input" type="<%= ((dict ["type"] == "password") ? "password" : "text")%>" name="cfg_<%= scope %>_<%= dict ["name"] %>" id="cfg_<%= scope %>_<%= dict ["name"] %>" value="<%= Config (SPContext.Current, scope + ":" + dict ["name"])%>" <%= ((((scope == "farm") && !IsFarmAdmin) || ((scope == "site") && !IsSiteAdmin)) ? "disabled=\"disabled\"" : "") %>/>
									<div style="text-align: <%= ((scope == "site") ? "right" : "left") %>; display: <%= ((((scope == "farm") && !IsFarmAdmin) || ((scope == "site") && !IsSiteAdmin)) ? "none" : "block") %>;">
									<a id="save_<%= scope %>_<%= dict ["name"]%>" style="display: none; height: 24px; padding: 0px 0px 0px 20px; background: url('/_layouts/images/saveitem.gif') 0px 0px no-repeat;" href="javascript:roxGoSave('<%= dict ["name"] %>', '<%= scope %>');"><%= this ["CfgSave"]%></a>
									<a id="reset_<%= scope %>_<%= dict ["name"] %>" style="visibility: <%= ((scope == "farm") ? "visible" : "hidden") %>; display: <%= ((Config (SPContext.Current, dict ["name"]) == dict ["default"]) ? "none" : "inline-block") %>; height: 24px; padding: 0px 0px 0px 20px; background: url('/_layouts/images/undo.gif') 0px 0px no-repeat;" href="javascript:roxGoReset('<%= dict ["name"] %>', '<%= scope %>');"><%= this ["CfgReset"]%></a>
									</div>
									<%
						} else {
									%>
									<select style="width: 99%;" onchange="roxGoSave('<%= dict ["name"] %>', '<%= scope %>');" name="cfg_<%= scope %>_<%= dict ["name"] %>" id="cfg_<%= scope %>_<%= dict ["name"] %>" <%= ((((scope == "farm") && !IsFarmAdmin) || ((scope == "site") && !IsSiteAdmin)) ? "disabled=\"disabled\"" : "") %>>
										<%
						foreach (string pair in dict ["type"].Trim ('[', ']').Split (new char [] { '|' }, StringSplitOptions.RemoveEmptyEntries)) {
										%>
										<option value="<%= pair.Substring (0, pair.IndexOf ('='))%>"<%= (Config (SPContext.Current, scope + ":" + dict ["name"]) == pair.Substring (0, pair.IndexOf ('='))) ? " selected=\"selected\"" : "" %>><%= pair.Substring (pair.IndexOf ('=') + 1)%></option>
										<%
						}
										%>
									</select>
									<%
						}
									%>
									</div>
								</td>
								<%
						}
								%>
							</tr>
							<%
						}
							%>
							<tr>
								<td class="ms-sectionline" colspan="3" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
						</table>
					</div>
					<br />
					<br />
					<%
						} else if (CfgTopic == "lic") {
					%>
					<br />
					<br />
					<div class="rox-tabrow">
						<div class="ms-templatepickerunselected"><div><a href="?"><%= this ["HomeFrame"]%></a></div></div>
						<%
							if (IsFarmAdmin && (!string.IsNullOrEmpty (GetProductResource ("_Tools")))) {
						%>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=tools"><%= this ["ToolsFrame"]%></a></div></div>
						<%
						}
						if (IsAnyAdmin) {
						%>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=wss"><%= this ["WssFrame"]%></a></div></div>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=cfg"><%= this ["CfgFrame"]%></a></div></div>
						<div class="ms-templatepickerselected"><div><a href="?cfg=lic"><%= this ["LicFrame"]%></a></div></div>
						<%
						}
						%>
						<div class="rox-templatepickerspacer">&nbsp;<%= this ["NoAdmin"]%>&nbsp;</div>
					</div>
					<div class="rox-tabcontainer">
						<table class="ms-propertysheet" cellspacing="0" cellpadding="0" border="0" width="100%">
							<tr>
								<td class="ms-descriptiontext" colspan="2">
									<div><%= this ["LicInfoText"]%><br /><a href="?doc=eula"><%= this ["LicInfoLinkText"]%></a> <a href="?doc=release_notes"><%= this ["ReleaseLinkText"]%></a></div>
									<div><%= GetProductResource ("_Copyright")%></div>
								</td>
							</tr>
							<tr>
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr>
								<td width="40%" class="ms-descriptiontext" valign="top" style="width: 40% !important;">
									<%= this ["LicStatus"]%>
								</td>
								<td width="60%" class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left" style="width: 60% !important;">
									<%
						if (sd.Count == 0) {
									%>
									<div id="licdiv">
										<b class="lichead" style="background: #c00;"><%= this ["LicExpiry"]%></b>
										<div><%= this ["LicExpiryHtml", this ["LicExpiryVague", l1], ProductName.ToLowerInvariant ()]%></div>
									</div>
									<%
						} else if ((dic == null) || IsTheThing (dic)) {
							if (IsTheThing (dic)) {
								foreach (object thekvp in dic)
									if (thekvp is DictionaryEntry)
										userkvp = new KeyValuePair<string, object> (((DictionaryEntry) thekvp).Key as string, ((DictionaryEntry) thekvp).Value);
									else if (thekvp is KeyValuePair<string, object>)
										userkvp = ((KeyValuePair<string, object>) thekvp);
										%>
											<div id="licdiv">
												<b class="lichead" style="background: #c00;"><%= this ["LicExpiryUsers"]%></b>
												<div><%= this ["LicUsersError", userkvp.Key, userkvp.Value]%></div>
											</div>
										<%
						} else {
								timeSpan = new TimeSpan ((long) sd ["is"]);
								if (timeSpan.Days > l1) {
											%>
											<div id="licdiv">
												<b class="lichead" style="background: #c00;"><%= this ["LicExpiryOn", ((DateTime) sd ["ed"]).ToShortDateString ()]%></b>
												<div><%= this ["LicExpiryHtml", timeSpan.Days, ProductName.ToLowerInvariant ()]%></div>
											</div>
											<%
						} else {
											%>
											<div id="licdiv">
												<b class="lichead" style="color: #000; background: gold;"><%= this ["LicTrial"]%></b>
												<div><%= this ["LicTrialHtml", ((DateTime) sd ["ed"]).ToShortDateString (), ProductName.ToLowerInvariant ()]%></div>
											</div>
											<%
						}
							}
						} else {
									%>
									<div id="licdiv">
										<b class="lichead" style="background: green;"><%= this ["LicFull"]%></b>
									</div>
									<%
						}
						if ((sd.Count == 0) || (dic == null)) {
									%>
									<div id="licdiv2">
										<b class="lichead" style="background: green;"><%= this ["LicMicro"]%></b>
										<b><%= this ["LicMicroHtml" + (IsAnyAdmin ? 2 : 3), SPContext.Current.Site.Url, Server.UrlEncode (SPContext.Current.Site.Url + "/_layouts/" + WhiteLabel + ProductName + "/default.aspx?cfg=lic")]%></b>
										<div><%= this ["LicMicroHtml", l1]%></div>
									</div>
									<%
						}
									%>
								</td>
							</tr>
							<%
						if (((dic == null) || IsTheThing (dic)) && IsAnyAdmin) {
							%>
							<tr class="rox-licrow">
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr class="rox-licrow">
								<td width="40%" class="ms-descriptiontext" valign="top" style="width: 40% !important;">
									<%= this ["LicSerial"]%>
								</td>
								<td width="60%" class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left" style="width: 60% !important;">
									<div><%= this ["LicSerialHtml"]%></div>
									<input id="licfile" name="licfile" class="ms-input" type="file" />
									<div><strong><%= this ["LicTargetIDs"]%></strong></div>
									<div><%= this ["LicTargetFarmID"]%></div>
									<div class="rox-input"><%= GetFarm (SPContext.Current).Id%></div>
									<div><%= this ["LicTargetSiteID", (SPContext.Current.Site.RootWeb == null) ? SPContext.Current.Site.ID.ToString () : SPContext.Current.Site.RootWeb.Title, SPContext.Current.Site.Url]%></div>
									<div class="rox-input"><%= SPContext.Current.Site.ID%></div>
									<div><%= this ["LicSiteHtml", ProductName, (SPContext.Current.Site.RootWeb == null) ? SPContext.Current.Site.ID.ToString () : SPContext.Current.Site.RootWeb.Title]%></div>
								</td>
							</tr>
							<%
						} else if (dic != null) {
							%>
							<tr class="rox-licrow">
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr class="rox-licrow">
								<td width="40%" class="ms-descriptiontext" valign="top" style="width: 40% !important;">
									<%= this ["LicName"]%>
								</td>
								<td width="60%" class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left" style="width: 60% !important;">
									<div class="rox-input"><%= dic ["c"]%></div>
								</td>
							</tr>
							<tr class="rox-licrow">
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr class="rox-licrow">
								<td width="40%" class="ms-descriptiontext" valign="top" style="width: 40% !important;">
									<%= this ["LicTarget"]%>
								</td>
								<td width="60%" class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left" style="width: 60% !important;">
									<div style="white-space: nowrap; font-weight: <%= ("0".Equals (dic ["f1"])) ? "bold" : "normal" %>;">
										<input type="radio" disabled="disabled"<%= ("0".Equals (dic ["f1"])) ? " checked=\"checked\"" : "" %>/>
										<%= this ["LicTargetSite", (SPContext.Current.Site.RootWeb == null) ? SPContext.Current.Site.ID.ToString () : SPContext.Current.Site.RootWeb.Title, SPContext.Current.Site.Url]%>, <%= this ["LicUsers" + ("0".Equals (dic ["f2"]) ? 0 : 1), dic ["f2"], GetUsers (SPContext.Current)]%>
									</div>
									<div style="white-space: nowrap; font-weight: <%= ("1".Equals (dic ["f1"])) ? "bold" : "normal" %>;">
										<input type="radio" disabled="disabled"<%= ("1".Equals (dic ["f1"])) ? " checked=\"checked\"" : "" %>/>
										<%= this ["LicTargetFarm"]%>, <%= this ["LicUsers" + ("0".Equals (dic ["f2"]) ? 0 : 1), dic ["f2"], GetUsers (SPContext.Current)]%>
									</div>
									<div style="white-space: nowrap; font-weight: <%= ("2".Equals (dic ["f1"])) ? "bold" : "normal" %>;">
										<input type="radio" disabled="disabled"<%= ("2".Equals (dic ["f1"])) ? " checked=\"checked\"" : "" %>/>
										<%= this ["LicTargetFarms"]%>, <%= this ["LicUsers" + ("0".Equals (dic ["f2"]) ? 0 : 1), dic ["f2"], GetUsers (SPContext.Current)]%>
									</div>
								</td>
							</tr>
							<%
						if (Editions.Count > 1) {
							%>
							<tr class="rox-licrow">
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr class="rox-licrow">
								<td width="40%" class="ms-descriptiontext" valign="top" style="width: 40% !important;">
									<%= this ["LicEditions"]%>
								</td>
								<td width="60%" class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left" style="width: 60% !important;">
									<%
						foreach (KeyValuePair<int, string> kvp in Editions) {
									%>
									<div style="white-space: nowrap; font-weight: <%= LicEdition (SPContext.Current, dic, kvp.Key) ? "bold" : "normal" %>;">
										<input type="checkbox" disabled="disabled"<%= LicEdition (SPContext.Current, dic, kvp.Key) ? " checked=\"checked\"" : "" %>/>
										<%= kvp.Value%>
									</div>
									<%
						}
									%>
								</td>
							</tr>
							<%
						}
						}
						if (IsAnyAdmin) {
							%>
							<tr class="rox-licrow">
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr class="rox-licrow">
								<td width="40%" class="ms-descriptiontext" valign="top" style="width: 40% !important;">
									<%= this ["LicUpdate"]%>
								</td>
								<td width="60%" class="ms-authoringcontrols ms-inputformcontrols" valign="top" style="width: 60% !important;">
									<%
						if (dic != null) {
									%>
									<div>
										<input type="checkbox" id="licremove" name="licremove" />
										<label for="licremove"><%= this ["LicRemove"]%></label>
									</div>
									<div style="text-align: right;">
										<input class="ms-ButtonHeightWidth" id="roxsavebutton" type="submit" value="OK"/>
									</div>
									<%
						} else {
									%>
									<%= this ["LicUpdateHtml"]%>
									<%
						}
									%>
								</td>
							</tr>
							<%
						}
						if (((dic == null) && (sd.Count > 3)) || ((dic != null) && (sd.Count > 4))) {
							%>
							<tr>
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr>
								<td width="40%" class="ms-descriptiontext" valign="top" style="width: 40% !important;">
									<%= this ["LicOther"]%>
								</td>
								<td width="60%" class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left" style="width: 60% !important;">
									<%= this ["LicOtherHtml"]%>
									<ul>
									<%
						foreach (string k in sd.Keys)
							if ((!k.Equals (SPContext.Current.Site.ID.ToString (), StringComparison.InvariantCultureIgnoreCase)) && (sd [k] is IDictionary)) {
								exmsg = string.Empty;
								if (lsite != null) {
									lsite.Dispose ();
									lsite = null;
								}
								try {
									lsite = new SPSite (new Guid (k));
								} catch (Exception ex) {
									exmsg = ex.Message;
								}
								if (lsite == null) {
											%>
											<li><%= k%></li>
											<%
						} else {
											%>
											<li><a href="<%= lsite.Url %>/_layouts/<%= AssemblyName%>/default.aspx?cfg=lic"><%= string.Format ("{0} ( {1} )", (lsite.RootWeb == null) ? k : lsite.RootWeb.Title, lsite.Url)%></a></li>
											<%
						}
							}
						if (lsite != null) {
							lsite.Dispose ();
							lsite = null;
						}
									%>
									</ul>
								</td>
							</tr>
							<%
						}
							%>
							<tr>
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
						</table>
					</div>
					<br />
					<br />
					<%
						} else {
					%>
					<br />
					<br />
					<div class="rox-tabrow">
						<div class="ms-templatepickerselected"><div><a href="?"><%= this ["HomeFrame"]%></a></div></div>
						<%
							if (IsFarmAdmin && (!string.IsNullOrEmpty (GetProductResource ("_Tools")))) {
						%>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=tools"><%= this ["ToolsFrame"]%></a></div></div>
						<%
						}
							if (IsAnyAdmin) {
						%>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=wss"><%= this ["WssFrame"]%></a></div></div>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=cfg"><%= this ["CfgFrame"]%></a></div></div>
						<div class="ms-templatepickerunselected"><div><a href="?cfg=lic"><%= this ["LicFrame"]%></a></div></div>
						<%
							}
						%>
						<div class="rox-templatepickerspacer">&nbsp;<%= this ["NoAdmin"]%>&nbsp;</div>
					</div>
					<div class="rox-tabcontainer">
						<table class="ms-propertysheet" cellspacing="0" cellpadding="0" border="0" width="100%">
							<tr>
								<td class="ms-descriptiontext" colspan="2">
									<div><%= this ["HomeInfo", ProductName]%></div>
								</td>
							</tr>
							<tr>
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<tr>
								<%
									if (IsFarmAdmin && !string.IsNullOrEmpty (GetProductResource ("_Tools"))) {
								%>
								<td style="background-image: url('/_layouts/images/menulistsettings.gif');" class="ms-authoringcontrols rox-homecell" valign="top" align="left">
									<h3><a href="?cfg=tools"><%= this ["ToolsFrame"]%></a></h3>
									<div><%= this ["ToolsInfo", ProductName]%></div>
									<ul>
									<%
										foreach (string tool in GetProductResource ("_Tools").Split (',')) {
										%>
										<li><a href="?cfg=tools&tool=<%= tool%>" title="<%= GetProductResource (tool + "_Desc").Replace ("<br/>", " &mdash; ")%>"><%= GetProductResource (tool + "_Title")%></a></li>
										<%
										}
									%>
									</ul>
								</td>
								<%
									}
								%>
								<td style="background-image: url('/_layouts/images/lg_ICHLP.gif');" colspan="<%= (IsFarmAdmin && !string.IsNullOrEmpty (GetProductResource ("_Tools"))) ? 1 : 2 %>" class="ms-descriptiontext rox-homecell" valign="top" align="left">
									<h3><a href="?doc=intro"><%= this ["Tab_Help"]%></a></h3>
									<ul style="float: left; margin: 8px;">
									<%
										wssItemCount = 0;
										hasHomeTopics = ((homeHelpTopics != null) && (homeHelpTopics.Length > 0));
										foreach (string topic in HelpTopicIDs)
											if ((!hasHomeTopics) || (Array.IndexOf<string> (homeHelpTopics, topic) >= 0)) {
										%>
										<li><a style="white-space: nowrap;" href="?doc=<%= topic%>"><%= GetProductResource ("HelpTopic_" + topic)%></a></li>
										<%
										wssItemCount++;
										if ((!hasHomeTopics) && ((wssItemCount % ((HelpTopicIDs.Length / ((HelpTopicIDs.Length > 8) ? 3 : 2)) + 1)) == 0)) {
									%>
									</ul>
									<ul style="float: left; margin: 8px;">
									<%
										}
											}
										if (hasHomeTopics) {
									%>
									<li><%= this ["HomeHelpTopics", HelpTopicIDs.Length - homeHelpTopics.Length]%></li>
									<%
										}
									%>
									</ul>
								</td>
							</tr>
							<tr>
								<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
							</tr>
							<%
								if (IsAnyAdmin) {
							%>
							<tr>
								<td style="background-image: url('/_layouts/images/WssBasicWebPartsFeature.gif');" class="ms-descriptiontext rox-homecell" valign="top">
									<h3><a href="?cfg=wss"><%= this ["WssFrame"]%></a></h3>
									<div><%= this ["WssInfo", ProductName]%></div>
									<ul>
									<%
								wssDict = new Dictionary<string, int> ();
								foreach (Dictionary<string, string> dict in WssItems)
									wssDict [dict ["type"]] = (wssDict.TryGetValue (dict ["type"], out wssItemCount) ? (wssItemCount + 1) : 1);
								foreach (KeyValuePair<string, int> kvp in wssDict) {
										%>
										<li><a href="?cfg=wss"><%= kvp.Value%> <%= this ["WssItem_" + kvp.Key + ((kvp.Value == 1) ? string.Empty : "s")]%></a></li>
										<%
								}
									%>
									</ul>
								</td>
								<%
									if (ConfigSettingsCount > 3) {
								%>
								<td style="background-image: url('/_layouts/images/<%= AssemblyName%>/completeallwftasks.gif');" class="ms-authoringcontrols rox-homecell" valign="top">
									<h3><a href="?cfg=cfg"><%= this ["CfgFrame"]%></a></h3>
									<div><%= this ["CfgInfo2", ProductName, ConfigSettingsCount]%></div>
									<%
									if (ConfigGroups.Count > 0) {
									%>
									<ul>
									<%
									foreach (KeyValuePair<string, string> kvp in ConfigGroups) {
										%>
										<li><a href="?cfg=cfg#cfg_<%= Server.HtmlEncode(kvp.Key)%>"><%= this ["Customize"]%> <%= Server.HtmlEncode (GetProductResource ("CfgSettingGroup_" + kvp.Value))%>&hellip;</a></li>
										<%
									}
									%>
									</ul>
									<%
									}
									%>
								</td>
								<%
									}
								%>
							</tr>
							<%
								}
							%>
						</table>
					</div>
					<br />
					<br />
					<%
						}
					%>
				</div>
				<%
					}
				%>
			</td>
		</tr>
		<tr>
			<td height="10px" class="ms-descriptiontext">
				<img src="/_layouts/images/blank.gif" width="1" height="10" alt="">
			</td>
		</tr>
	</table>
</asp:Content>
