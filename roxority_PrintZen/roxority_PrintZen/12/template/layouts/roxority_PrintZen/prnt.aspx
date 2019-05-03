<%@ Page Language="C#" AutoEventWireup="false" EnableEventValidation="false" EnableTheming="false" EnableViewState="false" ValidateRequest="false" Inherits="roxority_PrintZen.PrintZenPage, roxority_PrintZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %>
<%@ Register TagPrefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Import Namespace="roxority.SharePoint" %>
<%@ Import Namespace="System.Collections.Generic" %>
<script runat="server">

	private string webUrl = null;
	
	public bool Is14Popup {
		get {
			return "1".Equals (Request.QueryString ["roxDlg"]);
		}
	}

	public string WebUrl {
		get {
			if (webUrl == null)
				try {
					webUrl = SPContext.Current.Web.Url.TrimEnd ('/');
				} catch {
					webUrl = string.Empty;
				}
			return webUrl;
		}
	}
	
</script>
<html>
<head runat="server">
	<title runat="server"></title>
	<SharePoint:CssLink ID="CssLink" runat="server" />
	<SharePoint:Theme ID="Theme" runat="server" />
	<SharePoint:ScriptLink language="JavaScript" name="core.js" defer="true" runat="server"/>
	<SharePoint:CustomJSUrl runat="server"/>
	<script type="text/javascript" language="JavaScript" src="<%= WebUrl %>/_layouts/roxority_PrintZen/jQuery.js?v=<%= ProductPage.Version%>"></script>
	<script type="text/javascript" language="JavaScript" src="<%= WebUrl %>/_layouts/roxority_PrintZen/roxority_PrintZen.js?v=<%= ProductPage.Version%>"></script>
	<script type="text/javascript" language="JavaScript">
	roxPrintPage = '1';
	</script>
</head>
<body onload="if(<%= ((Errors.Count > 0) ? false : AutoPrint).ToString ().ToLowerInvariant ()%>){setTimeout(roxDoPrint,100);}">
<link rel="stylesheet" href="<%= WebUrl %>/_layouts/roxority_PrintZen/roxority_PrintZen.css?v=<%= ProductPage.Version%>" type="text/css" />
<style type="text/css">
	div.rox-prz-toolbar button { padding-left: 20px; padding-right: 20px; }
	div.rox-prz-toolbar span { height: 16px; overflow: hidden; text-overflow: ellipsis; }
</style>
<%
	foreach (string cu in CssUrls) {
%>
<link rel="stylesheet" href="<%= cu%>" type="text/css"/>
<%
	}
%>
<form runat="server" id="aspnetForm">
	<asp:Label runat="server" ID="theLit"></asp:Label>
	<%
		if (Errors.Count > 0)
			foreach (Exception ex in Errors) {
	%>
	<div class="rox-prz-toolbar" style="display: block; white-space: normal;">
		<%= Server.HtmlEncode (ex.ToString ().Replace (" ---> ", "\r\n ---> ")).Replace ("\r\n", "<br/>").Replace ("\r", "<br/>").Replace ("\n", "<br/>")%>
	</div>
	<%
			}
	%>
	<div class="rox-prz-toolbar" <%= ((ShowToolbar || (ShowPlaceholders && Placeholders)) ? string.Empty : "style=\"display: none;\"")%>>
		<%
			if (ShowToolbar) {	
		%>
		<button onclick="roxDoPrint();" type="button"><%= (Is14Popup ? "&nbsp;&nbsp;" : string.Empty) + Res ("ToolBar_PrintNow" + (Is14Popup ? "Ctx" : string.Empty))%></button>
		<span><%= Res ("ToolBar_PrintPreview" + (Is14Popup ? "Ctx" : string.Empty))%></span>
		<%
			}
			if (ShowPlaceholders && Placeholders) {
		%>
		<ul>
			<%
			foreach (KeyValuePair<string, string> kvp in PhVals) {
			%>
			<li><b><%= Server.HtmlEncode (roxority_PrintZen.PrintZenPage.PhName (kvp.Key))%></b> &mdash; <%= Server.HtmlEncode (kvp.Value)%></li>
			<%
			}
			%>
		</ul>
		<%
			}
		%>
	</div>
	<asp:Panel runat="server" ID="HeaderWebParts" CssClass="rox-printpart rox-printpart-misc rox-printpart-misc-header" />
	<asp:Panel runat="server" ID="MainWebPart" CssClass="ms-WPBody noindex ms-wpContentDivSpace rox-printpart rox-printpart-main" style="margin: 0px 0px 0px 0px;" />
	<asp:Panel runat="server" ID="FooterWebParts" CssClass="rox-printpart rox-printpart-misc rox-printpart-misc-footer" />
</form>
</body>
</html>
