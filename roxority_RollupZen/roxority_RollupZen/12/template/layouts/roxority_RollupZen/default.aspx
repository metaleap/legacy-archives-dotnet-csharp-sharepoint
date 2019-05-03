<%@ Page Language="C#" Inherits="roxority.SharePoint.ProductPage, roxority_RollupZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" EnableEventValidation="false" ValidateRequest="false" AutoEventWireup="false" %>
<%@ Register TagPrefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Import Namespace="Microsoft.SharePoint.Administration" %>
<%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %>
<%@ Import Namespace="Microsoft.SharePoint.Utilities" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="System.IO" %><script runat="server">
	IDictionary sd = null, dic = null;
	Random rnd = new Random ();
	Dictionary<string, int> wssDict = null;
	TimeSpan timeSpan;
	SPSite lsite;
	KeyValuePair<string, object> userkvp = new KeyValuePair<string, object> ();
	SPWebApplication webApp = null;
	DateTime expDate = DateTime.MinValue;
	string [] configVals, homeHelpTopics = GetProductResource ("_HomeHelpTopics").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
	string exmsg, siteTitle, cfgGroup = string.Empty, lastCfgGroup = string.Empty, ctlPath, tmp, cfgWidthCss = "width: 49% !importan;", cfgWidth="width=\"49%\"";
	bool cfgHasSiteVal, hasThisSite = false, hasHomeTopics, isCheckEnabled = true, isLicEd = false, isChoiceSelected;
	int hiddenTopics = 0, wssItemCount, realWssCount, navCount;
</script><!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Strict//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd">
<html>
<%
	sd = Status;
	dic = In (sd) as IDictionary;
%>
	<head>
		<meta http-equiv="imagetoolbar" content="no" />
		<link rel="stylesheet" type="text/css" href="roxsite.tl.css?v=<%= Version%>" />
		<link rel="stylesheet" type="text/css" href="roxority.tl.css?v=<%= Version%>" />
		<link rel="stylesheet" type="text/css" href="magpop.css?v=<%= Version%>" />
		<link rel="shortcut icon" href="img/favicon.ico" type="image/x-icon" />
		<title><%= this ["Tab_" + ((IsDocTopic) ? "Help" : "Info")]%> - <%= this ["SiteTitle", ProductName]%></title>
		<script type="text/javascript" language="JavaScript" src="json2.tl.js?v=<%= Version%>"></script>
		<script language="javascript" type="text/javascript" src="jQuery.js?v=<%= Version%>"></script>
		<script language="javascript" type="text/javascript" src="magpop.js?v=<%= Version%>"></script>
		<script type="text/javascript" language="JavaScript">
			var roxBaseUrl = '<%= GetProductResource ("_WhiteLabelUrl")%>';
		</script>
		<script language="javascript" type="text/javascript" src="roxority.tl.js?v=<%= Version%>"></script>
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
							inner += ('<tr><td><table class="ms-navitem' + (('<%= id %>' == '<%= DocTopic %>') ? ' ms-selectednav' : '') + '" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a href="?doc=<%= id %>&r=<%= rnd.Next () %>" class="' + (('<%= id %>' == '<%= DocTopic %>') ? ' ms-selectednav' : '') + '" style="border-style: none !important; font-size: 1em;"><%= GetHelpTitle (id) %></a></td></tr></table></td></tr>');
						<%
						} else
							hiddenTopics++;
				if (hiddenTopics > 0) {
				%>
					inner += ('<tr><td><table class="ms-navitem" width="100%" border="0" cellpadding="0" cellspacing="0"><tr><td style="width: 100%;"><a href="<%= SPContext.Current.Site.Url %>/_layouts/AccessDenied.aspx?loginasanotheruser=true&Source=<%= Server.UrlEncode (SPContext.Current.Site.Url + "/_layouts/roxority_RollupZen/default.aspx?doc=intro") %>&ReturnUrl=<%= Server.UrlEncode (SPContext.Current.Site.Url + "/_layouts/roxority_RollupZen/default.aspx?doc=intro") %>" style="border-style: none !important; font-size: 1em;"><%= GetResource ("AdminHelpTopic") %></a></td></tr></table></td></tr>');
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
				var tmp, pos = location.href.lastIndexOf('#');
				jQuery('.roxcfgrow-').show(0, roxClearCssFilter);
				if (pos > 0) {
					jQuery('.roxcfgrow-' + location.href.substr(pos + 5)).click();
					if ((tmp = jQuery('#tr_' + location.href.substr(pos + 5)).prevAll('tr.roxgrouprow')) && tmp.length)
						jQuery(tmp[0]).children('.roxcfggroup').click();
				}
			});
			jQuery(window).load(function() {
				jQuery('body').removeClass();
				jQuery(document.body).removeClass();
				var tmp = jQuery('.rox-takeout').html();
				if (tmp) {
					if (jQuery('.rox-takeout .rox-logitem-full').length > 0)
						tmp = jQuery('.rox-takeout .rox-logitem-full').html();
					jQuery('.rox-takeout').html('');
					jQuery('.rox-takeaway').html(tmp);
				}
			});
			function roxCheckAdminSite() {
				var isAdminSite = <%= IsAdminSite.ToString ().ToLower ()%>;
				if (!isAdminSite) {
					alert('<%= SPEncode.ScriptEncode (GetResource ("IsNoAdminSite"))%>');
					try {
						event.cancelBubble = true;
						event.returnValue = false;
					} catch(e) {
					}
				}
				return isAdminSite;
			}
		</script>
		<SharePoint:ScriptLink language="javascript" name="core.js" runat="server" />
		<script language="javascript" type="text/javascript" src="RollupZen.tl.js?v=<%= Version%>"></script>
		<link type="text/css" rel="stylesheet" href="RollupZen.tl.css?v=<%= Version%>" />
	</head>
	<body>
		<div id="canvasWrapper" class="roxdiv">
			<div id="canvas">
				<div id="pageHeaderWrapper">
					<div id="pageHeader">
						<div id="navigationTop">
							<div class="horizontalNavigationBar">
								<div class="rox-topcontrols">
									<div style="white-space: nowrap">
										<nobr>
											<span class="active-module"><%= this ["SiteTitle", ProductName]%></span>
											<%= this ["LicSite"]%>
											<select onchange="roxGoSite(this.options[this.selectedIndex].value);"<%= (((!IsAnyAdmin) || (SPContext.Current.Site.WebApplication.Sites.Count > 16)) ? " disabled=\"disabed\"" : string.Empty) %>>
											<%
												Context.Items ["roxsitetitle"] = siteTitle = roxority.SharePoint.ProductPage.GetSiteTitle (SPContext.Current);
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
								</div>
								<br class="clearer" />
							</div>
						</div>
						<div id="bannerAreaWrapper">
							<div id="bannerArea">
								<div id="bannerWrapper">
									<%
										if (!IsWhiteLabel) {
									%>
									<a href="<%= GetProductResource ("_WhiteLabelUrl")%>"><img id="banner" src="help/res/roxority.tlhr.png" alt="ROXORITY" title="ROXORITY"/></a>
									<%
										} else {
									%>
									<a href="<%= GetProductResource ("_WhiteLabelUrl")%>"><img id="banner" src="help/res/yukka.png" height="80" alt="Yukka" title="Yukka"/></a>
									<%
										}
									%>
								</div>
							</div>
						</div>
						<div id="navigationBottom">
							<div class="horizontalNavigationBar">
								<ul class="content-navigation">
									<li class="module<%= IsDocTopic ? string.Empty : " active-module"%>">
										<div><a href="?r=<%= rnd.Next () %>"><span><%= this ["Tab_Info"] %></span></a></div>
									</li>
									<li class="module<%= IsDocTopic ? " active-module" : string.Empty%>">
										<div><a href="?doc=<%= DefaultTopicID %>&r=<%= rnd.Next () %>"><span><%= this ["Tab_Help"] %></span></a></div>
									</li>
								</ul>
								<br class="clearer" />
							</div>
						</div>
					</div>
				</div>
				<div id="pageBodyWrapper">
					<div id="pageBody">
						<div id="sidebar1Wrapper" class="verticalNavigationBarWrapper">
							<div class="iw-vnb1">
								<div class="iw-vnb2">
									<div class="iw-vnb3">
										<div class="iw-vnb4">
											<div id="sidebar1" class="verticalNavigationBar">
												<%
													if (!IsApplicableAdmin) {
												%>
												<div class="sectionWrapper"><div class="iw-s1"><div class="iw-s2"><div class="iw-s3"><div class="iw-s4"><div class="section">
													<div class="caption">Hi, <%= SPContext.Current.Web.CurrentUser.Name%>!</div>
													<div class="content-passthrough"><div class="widget-wrapper widget-type-links"><div><ul class="link-group-list-pt"><li><ul>
														<li>
															<div class="roxcaptioninfo"><%= this ["SignInfo", SPContext.Current.Site.Url.TrimEnd (), AdminSite.Url.TrimEnd ('/')]%></div>
															<div class="title"><a href="/_layouts/AccessDenied.aspx?loginasanotheruser=true&Source=<%= Server.UrlEncode (Request.RawUrl)%>&ReturnUrl=<%= Server.UrlEncode (Request.RawUrl)%>"><%= this ["SignIn"]%></a></div>
														</li>
													</ul></li></ul></div></div></div>
												</div></div></div></div></div></div>
												<%
													}
													if (HasUpdate ()) {
												%>
												<div class="sectionWrapper"><div class="iw-s1"><div class="iw-s2"><div class="iw-s3"><div class="iw-s4"><div class="section">
													<div class="caption"><%= this["NavUpdates"]%></div>
													<div class="content-passthrough"><div class="widget-wrapper widget-type-links"><div><ul class="link-group-list-pt"><li><ul>
														<li>
															<div class="title"><a><%= this ["NavUpdates_YourVersion", DisplayVersion]%></a></div>
															<div class="description"></div>
														</li>
														<li>
															<div class="title"><a href="<%= GetProductResource ("_WhiteLabelUrl")%><%= ProductName.ToLower ()%>-documentation/release-notes.html" target="_blank"><%= this ["NavUpdates_NewVersion", WebVersion]%></a></div>
															<div class="description"></div>
														</li>
														<li>
															<div class="title"><a style="font-weight: bold;" href="http://roxority.s3.amazonaws.com/<%= AssemblyName%>.zip"><%= this ["NavUpdates_Download"]%></a></div>
															<div class="description"></div>
														</li>
													</ul></li></ul></div></div></div>
												</div></div></div></div></div></div>
												<%
													}
													if (IsDocTopic) {
												%>
												<div class="sectionWrapper"><div class="iw-s1"><div class="iw-s2"><div class="iw-s3"><div class="iw-s4"><div class="section">
													<div class="caption"><%= this ["Tab_Help"]%></div>
													<div class="content-passthrough"><div class="widget-wrapper widget-type-links"><div><ul class="link-group-list-pt"><li><ul>
													<%
													foreach (string htid in HelpTopicIDs) {
														%>
														<li>
															<div class="title"><a class="<%= ((htid == Request ["doc"]) || ((htid == DefaultTopicID) && string.IsNullOrEmpty (Request ["doc"]))) ? "rox-active" : string.Empty %>" href="?doc=<%= htid %>&r=<%= rnd.Next () %>"><%= GetHelpTitle (htid)%></a></div>
															<div class="description"></div>
														</li>
														<%
													}
													%>
													</ul></li></ul></div></div></div>
												</div></div></div></div></div></div>
												<%
													} else {
												%>
												<div class="sectionWrapper"><div class="iw-s1"><div class="iw-s2"><div class="iw-s3"><div class="iw-s4"><div class="section">
													<div class="caption"><%= this ["Tab_Info"]%></div>
													<div class="content-passthrough"><div class="widget-wrapper widget-type-links"><div><ul class="link-group-list-pt"><li><ul>
														<%
															if (isEnabled) {
														%>
														<li>
															<div class="title"><a id="roxnavliclink" class="<%= ((Request ["cfg"] == "lic") ? "rox-active" : string.Empty) %>" href="?cfg=lic&r=<%= rnd.Next () %>"><%= this ["LicFrame"]%>: <%= GetLicStatus (dic, sd, true, out expDate) %></a></div>
															<div class="description"></div>
														</li>
														<%
															}
														%>
														<%
													if (IsAnyAdmin) {
														realWssCount = GetNavCount ("wss");
														navCount = GetNavCount ("cfg");
														%>
														<li>
															<div class="title"><a class="<%= ((Request ["cfg"] == "cfg") ? "rox-active" : string.Empty) %>" href="?cfg=cfg&r=<%= rnd.Next () %>"><%= this ["CfgFrame"]%> (<%= navCount%>)</a></div>
															<div class="description"></div>
														</li>
														<li>
															<div class="title"><a class="<%= ((Request ["cfg"] == "wss") ? "rox-active" : string.Empty) %>" href="?cfg=wss&r=<%= rnd.Next () %>"><%= this ["WssFrame"]%> (<%= realWssCount%>)</a></div>
															<div class="description"></div>
														</li>
														<%
													}
														%>
														<%
														%>
													</ul></li></ul></div></div></div>
												</div></div></div></div></div></div>
												<%
													if (!string.IsNullOrEmpty (GetProductResource ("_Tools"))) {
												%>
												<div class="sectionWrapper"><div class="iw-s1"><div class="iw-s2"><div class="iw-s3"><div class="iw-s4"><div class="section">
													<div class="caption"><%= this ["ToolsFrame"]%></div>
													<div class="content-passthrough"><div class="widget-wrapper widget-type-links"><div><ul class="link-group-list-pt"><li><ul>
													<%
													foreach (string tool in GetProductResource ("_Tools").Split (',')) {
														navCount = GetNavCount (tool);
													%>
														<li>
															<div class="title"><a class="<%= (((Request ["cfg"] == "tools") && (Request ["tool"] == tool)) ? "rox-active" : string.Empty) %>" href="?cfg=tools&tool=<%= tool%>&r=<%= rnd.Next () %>" title="<%= GetProductResource (tool + "_Desc", ProductName).Replace ("<br/>", " &mdash; ")%>"><%= GetProductResource (tool + "_Title") + ((navCount < 0) ? string.Empty : ("&nbsp;&nbsp;<b>(" + navCount + ")</b>"))%></a></div>
															<div class="description"></div>
														</li>
													<%
													}
													%>
													</ul></li></ul></div></div></div>
												</div></div></div></div></div></div>
												<%
													}
													}
												%>
												<div class="sectionWrapper"><div class="iw-s1"><div class="iw-s2"><div class="iw-s3"><div class="iw-s4"><div class="section">
													<div class="caption"><%= this ["NavFeedback"]%></div>
													<div class="content-passthrough"><div class="widget-wrapper widget-type-links"><div><ul class="link-group-list-pt"><li><ul>
														<li>
															<div class="title"><a target="_blank" href="<%= GetProductResource ("_WhiteLabelUrl")%><%= ProductName.ToLower ()%>-forum/"><%= this ["NavForum", ProductName]%></a></div>
															<div class="description"></div>
														</li>
														<li>
															<div class="title"><a target="_blank" href="<%= GetProductResource ("_WhiteLabelUrl")%><%= ProductName.ToLower ()%>-support/" style="white-space: nowrap;"><%= GetProductResource ("_WhiteLabelEmail")%></a></div>
															<div class="description"></div>
														</li>
														<li>
															<div class="roxcaptioninfo"><br />Build Version: <b><%= Version%></b></div>
														</li>
													</ul></li></ul></div></div></div>
												</div></div></div></div></div></div>
												<%
													if (IsApplicableAdmin) {
												%>
												<div class="sectionWrapper"><div class="iw-s1"><div class="iw-s2"><div class="iw-s3"><div class="iw-s4"><div class="section">
													<div class="caption"><%= roxority.SharePoint.ProductPage.LoginName(SPContext.Current.Web.CurrentUser.LoginName)%></div>
													<div class="content-passthrough"><div class="widget-wrapper widget-type-links"><div><ul class="link-group-list-pt"><li><ul>
														<li>
															<div class="title"><a href="/_layouts/AccessDenied.aspx?loginasanotheruser=true&Source=<%= Server.UrlEncode (Request.RawUrl)%>&ReturnUrl=<%= Server.UrlEncode (Request.RawUrl)%>"><%= this ["SignIn"]%></a></div>
														</li>
													</ul></li></ul></div></div></div>
												</div></div></div></div></div></div>
												<%
													}
												%>
											</div>
										</div>
									</div>
								</div>
							</div>
						</div>
						<div id="contentWrapper">
							<div id="content">
								<div class="header">
									<h2 class="document-title">
										<span class="rox-pagedesc">
										<%
											pageTitle = (IsDocTopic ? GetHelpTitle (DocTopic) : GetProductResource ((("tools".Equals (CfgTopic, StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty (Request.QueryString ["tool"])) ? (Request.QueryString ["tool"] + "_Title") : (string.IsNullOrEmpty (CfgTopic) ? "Home" : (CfgTopic.Substring (0, 1).ToUpperInvariant () + CfgTopic.Substring (1).ToLowerInvariant ())) + "Frame")));
											foreach (KeyValuePair<string, string> kvp in Breadcrumb) {
										%>
										<a href="<%= kvp.Key%>"><%= kvp.Value%></a>
										<span style="font-size: 10px;">&nbsp;&gt;&nbsp;</span>
										<%
											}
										%>
										</span>
										<%= pageTitle%>
									</h2>
								</div>
								<div id="rox_content">
<form id="aspnetForm" enctype="multipart/form-data" runat="server">
<SharePoint:FormDigest runat="server" />
<%
	if ((Request ["cfg"] == "enable") && AdminSite.ID.Equals (SPContext.Current.Site.ID)) {
		if (isEnabled) {
%>
<script type="text/javascript" language="javascript">
alert('<%= SPEncode.ScriptEncode (this ["NotEnabledSuccess", ProductName]) %>');
</script>
<%
		} else {
%>
<div class="rox-info" style="background-image: url('/_layouts/images/servicenotinstalled.gif');"><%= this ["NotEnabledError", SPContext.Current.Web.Url.TrimEnd ('/'), Server.UrlEncode (Request.RawUrl), Server.HtmlEncode (errorMessage), ProductName]%></div>
<%
		}
	} else if (!isEnabled) {
%>
<div class="rox-info" style="background-image: url('/_layouts/images/servicenotinstalled.gif');"><%= this ["NotEnabled", MergeUrlPaths (AdminSite.Url, "/_layouts/roxority_RollupZen/default.aspx?cfg=enable"), ProductName]%></div>
<%
	} else if (!string.IsNullOrEmpty (errorMessage)) {
%>
<div class="rox-info" style="background-image: url('/_layouts/images/servicenotinstalled.gif');"><%= errorMessage%></div>
<%
	}
	if (Request.UserAgent.Contains ("MSIE 6.0") || Request.UserAgent.Contains ("MSIE 5.5") || Request.UserAgent.Contains ("MSIE 5.0") || Request.UserAgent.Contains ("MSIE 4.0")) {
%>
<link rel="stylesheet" href="ie6.tl.css" type="text/css" />
<div class="rox-info" style="background-image: url('img/ie6.gif'); font-size: 11px;"><%= this ["IE6", DateTime.Now.Year - 2004]%></div>
<%
	}
	if (IsDocTopic) {
%>
<div id="rox_page_help">
	<%= TopicContent%>
</div>
<%
	} else {
%>
<div id="rox_page_info">
	<%
		if (!string.IsNullOrEmpty (Request ["em"])) {
	%>
	<div class="rox-info" style="background-image: url('/_layouts/images/exclaim.gif');">
		<%= this ["FarmAdminError", Server.HtmlEncode (Request ["em"])] + (Request ["em"].Contains ("EXECUTE") ? this ["FarmAdminErrorNoServer"] : string.Empty)%>
	</div>
	<%
		} else if (postEx != null) {
	%>
	<div class="rox-info" style="background-image: url('/_layouts/images/exclaim.gif');">
		<%= Server.HtmlEncode (postEx.ToString ()).Replace ("\r\n", "<br/>").Replace ("\r", "<br/>").Replace ("\n", "<br/>")%>
	</div>
	<%
		}
		if (CfgTopic == "wss") {
	%>
	<div class="rox-tabcontainer">
		<table class="ms-propertysheet" cellspacing="0" cellpadding="0" border="0" width="100%">
			<tr>
				<td class="ms-descriptiontext" colspan="2">
					<div><%= this ["WssInfo", ProductName]%></div>
				</td>
			</tr>
			<%
				foreach (Dictionary<string, string> dict in WssItems) {
			%>
			<tr>
				<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
			</tr>
			<tr>
				<td class="ms-descriptiontext" valign="top" style="background: url('/_layouts/images/<%= dict ["icon"]%>') no-repeat 2px 6px;">
					<div style="padding: 0px 20px 0px 40px;"><<%= dict.ContainsKey ("link") ? "a" : "span"%> target="<%= dict.ContainsKey ("link") ? "_blank" : "_self"%>" href="<%= dict.ContainsKey ("link") ? dict ["link"] : "#"%>" style="text-decoration: none !important;"><b><%= dict ["title"]%></b></<%= dict.ContainsKey ("link") ? "a" : "span"%>><br /><i><%= this ["WssItem_" + dict ["type"]]%></i>
					<%
						if (realWssCount > 3) {
					%>
					<a href="#noop" onclick="jQuery('#roxwssdesc_<%= dict ["id"]%>').toggle();"><img align="top" border="0" src="/_layouts/images/hhelp.gif"/></a>
					<%
						}
					%>
					</div>
				</td>
				<td class="ms-authoringcontrols" valign="top" align="left" nowrap="nowrap">
					<%= dict ["info"]%>
				</td>
			</tr>
			<tr id="roxwssdesc_<%= dict ["id"]%>" style="<%= ((realWssCount > 3) ? "display: none;" : string.Empty) %>">
				<td class="ms-descriptiontext" valign="top" colspan="2">
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
	<%
		} else if (CfgTopic == "tools") {
	%>
	<div class="rox-tabcontainer">
		<table class="ms-propertysheet" cellspacing="0" cellpadding="0" border="0" width="100%">
			<%
				if (string.IsNullOrEmpty (Request ["tool"])) {
			%>
			<tr>
				<td class="ms-descriptiontext">
					<div><%= this ["ToolsInfo", ProductName]%></div>
				</td>
			</tr>
			<%
				foreach (string tool in GetProductResource ("_Tools").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries)) {
			%>
			<tr>
				<td class="ms-sectionline" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
			</tr>
			<tr>
				<td class="ms-descriptiontext" valign="top">
					<div style="background: url('<%= GetProductResource (tool + "_Icon")%>') no-repeat 0px 4px; font-size: 16px; font-weight: bold; padding: 0px 0px 0px 24px;"><a href="?cfg=tools&tool=<%= tool%>&r=<%= rnd.Next () %>"><%= GetProductResource (tool + "_Title")%></a></div>
					<div style="padding: 4px 4px 4px 24px;"><%= GetProductResource (tool + "_Desc", ProductName)%></div>
					<%
					%></td></tr><%
				}
			%>
			<%
				} else {
			%>
			<tr>
				<td class="ms-descriptiontext" style="padding-bottom: 0px;">
					<div style="border-bottom: #c0c0a0 1px dotted; border-top: #c0c0a0 1px dotted; padding-bottom: 4px; background-color: infobackground; padding-left: 4px; padding-right: 4px; color: infotext; font-size: 11px; padding-top: 4px;"><%= GetProductResource (Request ["tool"] + "_Desc", ProductName)%></div>
				</td>
			</tr>
			<tr>
				<td class="ms-descriptiontext" valign="top" style="padding-top: 0px; padding-left: 12px;">
				<%
				foreach (string tool in GetProductResource ("_Tools").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
					if (Request ["tool"] == tool) {
						if (!File.Exists (Server.MapPath (ctlPath = "~/_controltemplates/roxority_RollupZen/" + tool + ".ascx")))
							ctlPath = "/_controltemplates/roxority_RollupZen/" + tool + ".tc.ascx";
						using (Control ctl = LoadControl (ctlPath))
							ctl.RenderControl (__w);
						break;
					}
				%>
				</td>
			</tr>
			<%
				}
			%>
		</table>
		<div class="rox-takeaway"></div>
	</div>
	<%
		} else if (CfgTopic == "cfg") {
			isLicEd = LicEdition (GetContext (), dic, 2);
			if (IsAdminSite)
				cfgWidthCss = cfgWidth = "";
	%>
	<div class="rox-tabcontainer">
		<table class="ms-propertysheet" cellspacing="0" cellpadding="0" border="0" width="100%">
			<tr>
				<td class="ms-descriptiontext" colspan="2" style="padding-bottom: 4px;">
					<div><%= this ["CfgInfo"]%></div>
				</td>
			</tr>
			<tr>
				<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
			</tr>
			<tr>
				<td class="ms-descriptiontext" valign="top" align="left" <%= cfgWidth %> style="padding-bottom: 4px; <%= cfgWidthCss%>">
				<%= this ["CfgSettingFarm"]%>
				</td>
				<td class="ms-descriptiontext" valign="top" align="left" <%= cfgWidth %> style="padding-bottom: 4px; <%= cfgWidthCss%>">
				<% if (!IsAdminSite) { %>
				<%= this ["CfgSettingSite", SPContext.Current.Site.RootWeb.Title]%>
				<% } %>
				</td>
			</tr>
			<%
				if (IsFarmError) {
			%>
			<tr>
				<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
			</tr>
			<tr>
				<td colspan="2" class="ms-descriptiontext" valign="top" align="center" style="font-size: 11px; text-align: center; padding: 4px 4px 4px 4px; color: InfoText; background-color: InfoBackground;">
					<%= this ["CfgFarmHint", MergeUrlPaths (AdminSite.Url, "/_layouts/roxority_RollupZen/default.aspx?cfg=cfg")]%>
				</td>
			</tr>
			<%
				}
		foreach (Dictionary<string, string> dict in ConfigSettings) {
			cfgHasSiteVal = ConfigHasSiteValue (GetContext (), null, dict ["name"]);
			if (ConfigGroups.TryGetValue (dict ["name"], out cfgGroup)) {
				lastCfgGroup = cfgGroup;
				%>
				<tr>
					<td class="ms-sectionline" colspan="2" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
				</tr>
				<tr class="roxgrouprow">
					<td colspan="2" onclick="jQuery('.roxcfgrow').hide(0, function() { jQuery('.roxcfgrow-').show(0, roxClearCssFilter); jQuery('.roxcfgrow-<%= cfgGroup %>').show(0, roxClearCssFilter); }); jQuery('.roxcfggroup').css({ 'background-image': 'url(/_layouts/images/collapseplus.gif)' }); jQuery('.roxcfggroup-<%= cfgGroup %>').css({ 'background-image': 'url(/_layouts/images/collapseminus.gif)' });" class="roxcfggroup roxcfggroup-<%= cfgGroup %>"><span class="ms-sitetitle" style="font-weight: 700 !important;" href="#noop"><%= Server.HtmlEncode (GetProductResource ("CfgSettingGroup_" + cfgGroup))%></span></td>
				</tr>
				<%
		} else
				cfgGroup = lastCfgGroup;
			%>
			<tr class="roxcfgrow roxcfgrow-<%= cfgGroup %>" id="tr_<%= dict ["name"] %>" style="display: none;">
				<td class="ms-sectionline" colspan="2" height="1"><a name="cfg_<%= dict ["name"] %>"><img height="1" src="/_layouts/images/blank.gif" width="1" /></a></td>
			</tr>
			<tr class="roxcfgrow roxcfgrow-<%= cfgGroup %>" style="display: none">
				<td class="ms-descriptiontext" valign="top" colspan="2" style="padding: 12px 0px 12px 0px;">
					<div class="roxcfgheadlink"><a class="roxcfgheadlink" onclick="jQuery('#cfgdesc_<%= dict ["name"]%>').toggle();return(event.returnValue=!(event.cancelBubble=true));" href="#cfg_<%= dict ["name"]%>"><%= dict [(dict ["type"] == "bool") ? "caption" : "title"]%></a></div>
				</td>
			</tr>
			<tr class="roxcfgrow roxcfgrow-<%= cfgGroup %> roxcfgrow-<%= dict ["name"]%>" style="display: none;">
				<%
		foreach (string scope in new string [] { "farm", "site" }) {
				%>
				<td class="ms-authoringcontrols ms-halfinputformcontrols" valign="top" align="left" <%= cfgWidth %> style="padding: 4px 16px 24px 16px; border-right: <%= ((scope == "farm") ? 1 : 0) %>px dotted #cccccc; <%= cfgWidthCss%>">
					<% if ((scope == "farm") || !IsAdminSite) { %>
					<div style="padding-bottom: 4px; height: 32px; white-space: nowrap;">
						<input onclick="document.getElementById('label_<%= scope %>_<%= dict ["name"] %>').style.textDecoration=(this.checked?'none':'line-through');if(this.checked){roxGoReset('<%= dict ["name"] %>', '<%= scope %>');}else{document.getElementById('content_<%= scope %>_<%= dict ["name"]%>').style.visibility='visible';}" name="chk_<%= scope %>_<%= dict ["name"] %>" id="chk_<%= scope %>_<%= dict ["name"] %>" type="checkbox" <%= (cfgHasSiteVal ? string.Empty : "checked=\"checked\"") %> <%= ((scope == "farm") ? "style=\"display: none;\"" : string.Empty) %> <%= ((isCheckEnabled = (IsSiteAdmin && (isLicEd || (dict ["name"] [0] == '_')) && !IsFarmOnlySetting (dict ["name"]))) ? string.Empty : "disabled=\"disabled\"") %> />
						<label id="label_<%= scope %>_<%= dict ["name"] %>" for="chk_<%= scope %>_<%= dict ["name"] %>" style="white-space: nowrap; text-decoration: <%= (((scope != "farm") && cfgHasSiteVal) ? "line-through" : "none") %>;<%= ((isCheckEnabled || (scope != "site")) ? string.Empty : " color: GrayText;")%>"><%= this [((scope == "farm") ? "CfgFarmSetting" : "CfgUseFarm")]%></label>
					</div>
					<div id="content_<%= scope %>_<%= dict ["name"]%>" style="visibility: <%= (((scope != "site") || cfgHasSiteVal) ? "visible" : "hidden") %>;">
					<%
		if (dict ["type"] == "bool") {
					%>
					<input onchange="roxGoSave('<%= dict ["name"] %>', '<%= scope %>');" onclick="roxGoSave('<%= dict ["name"] %>', '<%= scope %>');" type="checkbox" name="cfg_<%= scope %>_<%= dict ["name"] %>" id="cfg_<%= scope %>_<%= dict ["name"] %>"<%= Config<bool> (SPContext.Current, scope + ":" + dict ["name"]) ? " checked=\"checked\"" : "" %> <%= ((((scope == "farm") && !IsFullFarmAdmin) || ((dict ["name"] [0] != '_') && !isLicEd) || ((scope == "site") && !IsSiteAdmin)) ? "disabled=\"disabled\"" : "") %> />
					<label for="cfg_<%= scope %>_<%= dict ["name"] %>"><%= dict ["title"]%></label>
					<%
		} else if (dict ["type"] == "text") {
					%>
					<textarea style="width: 297px;" rows="10" wrap="off" onchange="document.getElementById('reset_<%= scope %>_<%= dict ["name"]%>').style.display='<%= ((dict ["default"] == null) ? "none" : "inline-block") %>';document.getElementById('save_<%= scope %>_<%= dict ["name"]%>').style.display='inline-block';" class="ms-input" name="cfg_<%= scope %>_<%= dict ["name"] %>" id="cfg_<%= scope %>_<%= dict ["name"] %>" <%= ((((scope == "farm") && !IsFullFarmAdmin) || ((dict ["name"] [0] != '_') && !isLicEd) || ((scope == "site") && !IsSiteAdmin)) ? "disabled=\"disabled\"" : "") %>><%= Config (SPContext.Current, scope + ":" + dict ["name"])%></textarea>
					<div style="text-align: <%= ((scope == "site") ? "right" : "left") %>; display: <%= ((((scope == "farm") && !IsFullFarmAdmin) || ((scope == "site") && !IsSiteAdmin)) ? "none" : "block") %>;">
					<a id="save_<%= scope %>_<%= dict ["name"]%>" style="display: none; padding: 0px 0px 0px 20px; background: url('/_layouts/images/saveitem.gif') left center no-repeat;" href="javascript:roxGoSave('<%= dict ["name"] %>', '<%= scope %>');"><%= this ["CfgSave"]%></a>
					<a id="reset_<%= scope %>_<%= dict ["name"] %>" style="visibility: <%= ((scope == "farm") ? "visible" : "hidden") %>; display: <%= ((Config (SPContext.Current, dict ["name"]) == dict ["default"]) ? "none" : "inline-block") %>; padding: 0px 0px 0px 20px; background: url('/_layouts/images/undo.gif') left center no-repeat;" href="javascript:roxGoReset('<%= dict ["name"] %>', '<%= scope %>');"><%= this ["CfgReset"]%></a>
					</div>
					<%
		} else if ((dict ["type"] == "string") || (dict ["type"] == "password")) {
					%>
					<input style="width: 290px !important;" onchange="document.getElementById('reset_<%= scope %>_<%= dict ["name"]%>').style.display='<%= ((dict ["default"] == null) ? "none" : "inline-block") %>';document.getElementById('save_<%= scope %>_<%= dict ["name"]%>').style.display='inline-block';" class="ms-input" type="<%= ((dict ["type"] == "password") ? "password" : "text")%>" name="cfg_<%= scope %>_<%= dict ["name"] %>" id="cfg_<%= scope %>_<%= dict ["name"] %>" value="<%= Server.HtmlEncode (Config (SPContext.Current, scope + ":" + dict ["name"]))%>" <%= ((((scope == "farm") && !IsFullFarmAdmin) || ((dict ["name"] [0] != '_') && !isLicEd) || ((scope == "site") && !IsSiteAdmin)) ? "disabled=\"disabled\"" : "") %>/>
					<div style="text-align: <%= ((scope == "site") ? "right" : "left") %>; display: <%= ((((scope == "farm") && !IsFullFarmAdmin) || ((scope == "site") && !IsSiteAdmin)) ? "none" : "block") %>;">
					<a id="save_<%= scope %>_<%= dict ["name"]%>" style="display: none; padding: 0px 0px 0px 20px; background: url('/_layouts/images/saveitem.gif') left center no-repeat;" href="javascript:roxGoSave('<%= dict ["name"] %>', '<%= scope %>');"><%= this ["CfgSave"]%></a>
					<a id="reset_<%= scope %>_<%= dict ["name"] %>" style="visibility: <%= ((scope == "farm") ? "visible" : "hidden") %>; display: <%= ((Config (SPContext.Current, dict ["name"]) == dict ["default"]) ? "none" : "inline-block") %>; padding: 0px 0px 0px 20px; background: url('/_layouts/images/undo.gif') left center no-repeat;" href="javascript:roxGoReset('<%= dict ["name"] %>', '<%= scope %>');"><%= this ["CfgReset"]%></a>
					</div>
					<%
		} else {
					%>
					<select style="width: 99%;" <%= dict.ContainsKey ("multiSel") ? "onblur" : "onchange"%>="roxGoSave('<%= dict ["name"] %>', '<%= scope %>');" name="cfg_<%= scope %>_<%= dict ["name"] %>" id="cfg_<%= scope %>_<%= dict ["name"] %>" <%= ((((scope == "farm") && !IsFullFarmAdmin) || ((dict ["name"] [0] != '_') && !isLicEd) || ((scope == "site") && !IsSiteAdmin)) ? "disabled=\"disabled\"" : "") %> <%= (dict.ContainsKey ("multiSel") ? (" size=\"" + dict ["type"].Trim ('[', ']').Split (new char [] { '|' }, StringSplitOptions.RemoveEmptyEntries).Length + "\" multiple=\"multiple\"") : "")%>>
						<%
		configVals = Config (SPContext.Current, scope + ":" + dict ["name"]).Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string pair in dict ["type"].Trim ('[', ']').Split (new char [] { '|' }, StringSplitOptions.RemoveEmptyEntries)) {
						%>
						<option value="<%= pair.Substring (0, pair.IndexOf ('='))%>"<%= (Array.Exists<string> (configVals, delegate (string vv) { return vv == pair.Substring (0, pair.IndexOf ('=')); })) ? " selected=\"selected\" style=\"font-weight: bold; font-style: italic;\"" : "" %>><%= pair.Substring (pair.IndexOf ('=') + 1)%></option>
						<%
		}
						%>
					</select>
					<%
		}
					%>
					</div>
					<% } %>
				</td>
				<%
		}
				%>
			</tr>
			<tr class="roxcfgrow roxcfgrow-<%= cfgGroup %>" style="display: none">
				<td class="ms-descriptiontext" valign="top" colspan="2">
					<div class="roxConfigDesc" style="display: none;" id="cfgdesc_<%= dict ["name"]%>">
						<%= dict ["desc"]%>
						<%
							if ((!(isLicEd || dict ["name"].StartsWith ("_", StringComparison.InvariantCultureIgnoreCase))) && isEnabled) {
						%>
						<br />
						<b><%= this ["NoLiteConfig" + (IsWhiteLabel ? "_White" : string.Empty)]%></b>
						<%
							}
						%>
					</div>
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
	<%
		} else if (CfgTopic == "lic") {
	%>
	<div class="rox-tabcontainer">
		<table class="ms-propertysheet" cellspacing="0" cellpadding="0" border="0" width="100%">
			<tr>
				<td class="ms-descriptiontext">
					<div style="text-align: center; font-size: 11px;"><%= GetProductResource ("_Copyright")%> | <a href="?doc=eula&r=<%= rnd.Next () %>"><%= this ["LicInfoLinkText"]%></a>
					</div>
					<%
						if (IsFullFarmAdmin) {
					%>
					<div class="rox-wssinfo" style="text-align: center;"><%= this ["LicUpdateInfo"]%></div>
					<%
						}
					%>
				</td>
			</tr>
			<%
				if (((dic == null) && (sd.Count > 3)) || ((dic != null) && (sd.Count > 4))) {
			%>
			<tr>
				<td class="ms-sectionline" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
			</tr>
			<tr>
				<td class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left">
					<h2><%= this ["LicOther"]%></h2>
					<%= this ["LicOtherHtml", siteTitle]%>
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
					lsite.CatchAccessDeniedException = false;
					lsite.Url.ToString ();
					if (lsite.RootWeb != null)
						lsite.RootWeb.Title.ToString ();
				} catch (Exception ex) {
					if (lsite != null)
						lsite.Dispose ();
					lsite = null;
					exmsg = ex.Message;
				}
				%>
				<li>
				<%
				if (lsite == null) {
							%>
							<%= k%>
							<%
		} else {
							%>
							<a style="background-color: #bbffbb; color: #006600" href="<%= lsite.Url %>/_layouts/<%= AssemblyName%>/default.aspx?cfg=lic"><%= string.Format ("{0} ( {1} )", (lsite.RootWeb == null) ? k : lsite.RootWeb.Title, lsite.Url)%></a>
							<%
		}
				if (IsFarmAdmin) {
						%>
						&mdash; <a onclick="return(<%= SPContext.Current.Site.WebApplication.IsAdministrationWebApplication ? ("confirm('" + this ["LicRemoveFastPrompt"] + "')") : ("alert('" + this ["SwitchToAdminApp"] + "')") %>);" href="<%= SPContext.Current.Site.WebApplication.IsAdministrationWebApplication ? ("?cfg=lic&licremove=" + k + "&r=" + rnd.Next ()) : "#" %>"><img border="0" src="/_layouts/images/delete.gif" align="top" style="margin-right: 2px;" /><%= this ["LicRemoveFast"]%></a>
						<%
							}
				%>
				</li>
				<%
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
				<td class="ms-sectionline" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
			</tr>
			<tr>
				<td class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left">
					<h2><%= this ["LicStatus"]%></h2>
					<%
		if (sd.Count == 0) {
					%>
					<div id="licdiv">
						<b class="lichead" style="background: #c00;"><%= this ["LicExpiry"]%></b>
						<div><%= this [IsWhiteLabel ? "LicExpiryHtml_White" : "LicExpiryHtml", this ["LicExpiryVague", l1], ProductName.ToLowerInvariant ()]%></div>
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
								<span class="lichead" style="color: #000; background: gold;"><%= this ["LicLiteHere", userkvp.Value]%></span>
								<div><%= this ["LicUsersError", userkvp.Key, userkvp.Value, siteTitle]%></div>
							</div>
						<%
						} else {
							timeSpan = new TimeSpan ((long) sd ["is"]);
							if (((int) Math.Round (timeSpan.TotalDays)) >= l1) {
								if (sd.Count > 3) {
								%>
								<div id="Div1">
									<b class="lichead" style="background: green;"><%= this[0]%></b>
								</div>
								<%
							} else {
							%>
							<div id="licdiv">
								<b class="lichead" style="color: #000; background: gold;"><%= this ["LicExpiryOn", ((DateTime) sd ["ed"]).ToShortDateString ()]%></b>
								<div><%= this [IsWhiteLabel ? "LicExpiryHtml_White" : "LicExpiryHtml", timeSpan.Days, ProductName.ToLowerInvariant ()]%></div>
							</div>
							<%
							}
						} else {
							%>
							<div id="licdiv">
								<b class="lichead" style="background: green;"><%= (IsWhiteLabel ? GetProductResource ("_WhiteLabelUnlicensed") : this ["LicTrial"])%></b>
								<div><%= this ["LicTrialHtml" + (IsWhiteLabel ? "_White" : string.Empty), ((DateTime) sd ["ed"]).ToShortDateString (), ProductName.ToLowerInvariant ()]%></div>
							</div>
							<%
						}
			}
		} else {
					%>
					<div id="licdiv">
						<span class="lichead" style="<%= (((expDate > DateTime.MinValue) && (expDate <= DateTime.Now)) ? "background: gold; color: #000000;" : "background: green;") %>"><%= (tmp = GetLicStatus (dic, sd, false, out expDate))%> &mdash; <%= dic ["c"]%></span>
						<%
							if (expDate > DateTime.MinValue)
								if (expDate <= DateTime.Now) {
							%>
							<%= this ["LicEvalExpired", expDate.ToShortDateString ()]%>
							<%
								} else {
							%>
							<%= this ["LicEvalExpires", expDate.ToShortDateString ()]%>
							<%
								}
							if ((dic != null) && ((dic ["f3"] + string.Empty) != "6") && tmp.Contains (GetEdition (-1)) && HasMicro) {
							%>
							<%= this ["LicMicroHint"]%>
							<%
							}
						%>
					</div>
					<%
						}
					%>
					<div onclick="jQuery('#rox_lictargetids').toggle();" style="font-size: 11px; text-align: center;">Farm ID: <%= GetFarm (SPContext.Current).Id + (IsWhiteLabel ? string.Empty : (" | Site ID: " + SPContext.Current.Site.ID))%></div>
					<div id="rox_lictargetids" class="rox-wssinfo" style="text-align: center; display: none;"><%= this ["LicTargetIDs", siteTitle]%></div>
				</td>
			</tr>
			<%
		if (((dic == null) || IsTheThing (dic)) && IsFullFarmAdmin && !IsFarmError) {
			%>
			<tr class="rox-licrow">
				<td class="ms-sectionline" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
			</tr>
			<tr class="rox-licrow">
				<td class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left">
					<h2><%= this ["LicUpdate"]%></h2>
					<div>
						<%= this ["LicSerialHtml"]%>
						<input id="licfile" name="licfile" class="ms-input" type="file" />
					</div>
				</td>
			</tr>
			<%
				} else if (dic != null) {
			if (!IsTheThing (dic)) {
			%>
			<tr class="rox-licrow">
				<td class="ms-sectionline" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
			</tr>
			<tr class="rox-licrow">
				<td class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left">
					<h2><%= this ["LicTarget"]%></h2>
					<div style="font-size: 12px; font-weight: <%= ("0".Equals (dic ["f1"])) ? "bold" : "normal" %>;">
						<input type="radio" disabled="disabled"<%= ("0".Equals (dic ["f1"])) ? " checked=\"checked\"" : "" %>/>
						<%= this ["LicTargetSite", (SPContext.Current.Site.RootWeb == null) ? SPContext.Current.Site.ID.ToString () : SPContext.Current.Site.RootWeb.Title, SPContext.Current.Site.Url]%>, <%= this ["LicUsers" + ("0".Equals (dic ["f2"]) ? 0 : 1), dic ["f2"], GetUsers (SPContext.Current)]%>
					</div>
					<div style="font-size: 12px; font-weight: <%= ("1".Equals (dic ["f1"])) ? "bold" : "normal" %>;">
						<input type="radio" disabled="disabled"<%= ("1".Equals (dic ["f1"])) ? " checked=\"checked\"" : "" %>/>
						<%= this ["LicTargetFarm"]%>, <%= this ["LicUsers" + ("0".Equals (dic ["f2"]) ? 0 : 1), dic ["f2"], GetUsers (SPContext.Current)]%>
					</div>
					<div style="font-size: 12px; font-weight: <%= ("2".Equals (dic ["f1"])) ? "bold" : "normal" %>;">
						<input type="radio" disabled="disabled"<%= ("2".Equals (dic ["f1"])) ? " checked=\"checked\"" : "" %>/>
						<%= this ["LicTargetFarms"]%>, <%= this ["LicUsers" + ("0".Equals (dic ["f2"]) ? 0 : 1), dic ["f2"], GetUsers (SPContext.Current)]%>
					</div>
				</td>
			</tr>
			<%
				}
		if (Editions.Count > 1) {
			%>
			<tr class="rox-licrow">
				<td class="ms-sectionline" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
			</tr>
			<tr class="rox-licrow">
				<td class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="left">
					<h2><%= this ["LicEditions"]%></h2>
					<%
		foreach (KeyValuePair<int, string> kvp in Editions) {
					%>
					<div style="white-space: nowrap; font-weight: <%= LicEdition (SPContext.Current, dic, kvp.Key) ? "bold" : "normal" %>;">
						<input type="checkbox" disabled="disabled"<%= LicEdition (SPContext.Current, dic, kvp.Key) ? " checked=\"checked\"" : "" %>/>
						<label style="text-decoration: <%= (((expDate > DateTime.MinValue) && (expDate <= DateTime.Now) && (kvp.Key != 0)) ? "line-through" : "none") %>;"><%= kvp.Value%></label>
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
				<td class="ms-authoringcontrols ms-inputformcontrols" valign="top" align="center">
					<%
						if (IsFarmError) {
					%>
					<div class="rox-wssinfo" style="text-align: center;"><%= this ["LicFullAdminHtml", MergeUrlPaths (AdminSite.Url, "/_layouts/roxority_RollupZen/default.aspx?cfg=lic")]%></div>
					<%
						} else if (!IsFullFarmAdmin) {
					%>
					<div class="rox-wssinfo" style="text-align: center;"><%= this ["LicFarmAdminHtml", SPContext.Current.Web.Url.TrimEnd ('/'), Server.UrlEncode (Request.RawUrl)]%></div>
					<%
				} else
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
			%>
			<tr>
				<td class="ms-sectionline" height="1"><img height="1" src="/_layouts/images/blank.gif" width="1" /></td>
			</tr>
			<tr class="rox-licrow">
				<td class="ms-authoringcontrols ms-inputformcontrols" valign="top">
					<div><%= this ["LicInfoText"]%></div>
				</td>
			</tr>
		</table>
	</div>
	<%
		} else {
	%>
	<div class="rox-tabcontainer">
		<table class="ms-propertysheet" cellspacing="0" cellpadding="0" border="0" width="100%">
			<tr>
				<td class="ms-descriptiontext" colspan="2">
					<div><%= this ["HomeInfo", ProductName]%></div>
				</td>
			</tr>
			<tr>
				<td style="background-image: url('/_layouts/images/menulistsettings.gif');" class="ms-authoringcontrols rox-homecell" valign="top" align="left">
					<h3><a href="?cfg=tools&r=<%= rnd.Next () %>"><%= this ["ToolsFrame"]%></a></h3>
					<div><%= this ["ToolsInfo", ProductName]%></div>
					<ul style="list-style-type: none; margin-left: 0px; padding-left: 0px;">
					<%
						foreach (string tool in GetProductResource ("_Tools").Split (',')) {
						%>
						<li style="padding-left: 20px; margin-left: 0px; background: url('<%= GetProductResource (tool + "_Icon")%>') no-repeat 0px 5px;"><a href="?cfg=tools&tool=<%= tool%>&r=<%= rnd.Next () %>" title="<%= GetProductResource (tool + "_Desc", ProductName).Replace ("<br/>", " &mdash; ")%>"><%= GetProductResource (tool + "_Title")%></a></li>
						<%
						}
					%>
					</ul>
				</td>
				<td style="background-image: url('/_layouts/images/lg_ICHLP.gif');" class="ms-descriptiontext rox-homecell" valign="top" align="left">
					<h3><a href="?doc=intro&r=<%= rnd.Next () %>"><%= this ["Tab_Help"]%></a></h3>
					<ul style="float: left; padding-left: 16px; margin-left: 0px;">
					<%
						wssItemCount = 0;
						hasHomeTopics = ((homeHelpTopics != null) && (homeHelpTopics.Length > 0));
						foreach (string topic in HelpTopicIDs)
							if ((!hasHomeTopics) || (Array.IndexOf<string> (homeHelpTopics, topic) >= 0)) {
						%>
						<li><a href="?doc=<%= topic%>&r=<%= rnd.Next () %>"><%= GetHelpTitle (topic)%></a></li>
						<%
						wssItemCount++;
						if ((!hasHomeTopics) && ((wssItemCount % ((HelpTopicIDs.Length / ((HelpTopicIDs.Length > 8) ? 3 : 2)) + 1)) == 0)) {
					%>
					</ul>
					<ul style="float: left; padding-left: 16px; margin-left: 0px;">
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
			<%
				if (IsAnyAdmin) {
			%>
			<tr>
				<td style="background-image: url('/_layouts/images/WssBasicWebPartsFeature.gif');" class="ms-descriptiontext rox-homecell" valign="top">
					<h3><a href="?cfg=wss&r=<%= rnd.Next () %>"><%= this ["WssFrame"]%></a></h3>
					<div><%= this ["WssInfo", ProductName]%></div>
					<ul style="padding-left: 16px; margin-left: 0px;">
					<%
				wssDict = new Dictionary<string, int> ();
				foreach (Dictionary<string, string> dict in WssItems)
					wssDict [dict ["type"]] = (wssDict.TryGetValue (dict ["type"], out wssItemCount) ? (wssItemCount + 1) : 1);
				foreach (KeyValuePair<string, int> kvp in wssDict) {
						%>
						<li><a href="?cfg=wss&r=<%= rnd.Next () %>"><%= kvp.Value%> <%= this ["WssItem_" + kvp.Key + ((kvp.Value == 1) ? string.Empty : "s")]%></a></li>
						<%
				}
					%>
					</ul>
				</td>
				<td style="background-image: url('/_layouts/<%= AssemblyName%>/img/completeallwftasks.gif');" class="ms-authoringcontrols rox-homecell" valign="top">
					<h3><a href="?cfg=cfg&r=<%= rnd.Next () %>"><%= this ["CfgFrame"]%></a></h3>
					<div><%= this ["CfgInfo2", ProductName, ConfigSettingsCount]%></div>
					<%
					if (ConfigGroups.Count > 0) {
					%>
					<ul style="padding-left: 16px; margin: 0px;">
					<%
					foreach (KeyValuePair<string, string> kvp in ConfigGroups) {
						%>
						<li><a href="?cfg=cfg&r=<%= rnd.Next () %>#cfg_<%= Server.HtmlEncode(kvp.Key)%>"><%= this ["Customize", Server.HtmlEncode (GetProductResource ("CfgSettingGroup_" + kvp.Value))]%></a></li>
						<%
					}
					%>
					</ul>
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
	<%
		}
	%>
</div>
<%
	}
%>
</form>
								</div>
							</div>
						</div>
					</div>
				</div>
				<div id="pageFooterWrapper">
					<div id="pageFooter">
						<div class="rox-footerbig"><%= this ["Studio1", this ["HelpTitle", ProductName]]%></div>
						<div class="rox-footermedium"><%= this ["Studio2"]%></div>
						<ul>
							<li><%= this ["Studio3", DefaultTopicID]%></li>
							<li><%= this ["Studio4"]%></li>
							<%
								if (IsAnyAdmin) {
							%>
							<li><%= this ["Studio5"]%></li>
							<li><%= this ["Studio6"]%></li>
							<%
								}
								if (IsFarmAdmin && (!string.IsNullOrEmpty (GetProductResource ("_Tools")))) {
							%>
							<li><%= this ["Studio7"]%></li>
							<%
								}
							%>
						</ul>
						<div><%= this ["Studio8", this ["HelpTitle", ProductName]]%></div>
						<div class="rox-footerinfo" style="color: #888b86; margin-top: 24px; border-top: 1px solid #c0c0c0;"><b><%= GetProductResource ("_Copyright")%></b> <%= ProductName%> Version: <%= Version%></div>
						<a name="down"></a>
					</div>
				</div>
			</div>
		</div>
		<div id="rox_topright" class="roxdiv">
			<a href="#down"><%= this ["StudioLink", ProductName]%></a>
		</div>
		<div id="rox_sitehint" style="visibility: hidden;">&nbsp;</div>
		<div id="rox_prodlogo">&nbsp;</div>
	</body>
</html>
