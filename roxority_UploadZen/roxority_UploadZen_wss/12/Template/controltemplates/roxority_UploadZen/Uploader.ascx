<%@ Control Language="C#" AutoEventWireup="false" Inherits="roxority_UploadZen.UploadControl, roxority_UploadZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %>
<%@ Import Namespace="roxority.Shared" %>
<%@ Import Namespace="roxority.SharePoint" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Import Namespace="Microsoft.SharePoint.Utilities" %>
<%@ Import Namespace="System.Collections.Generic" %>
<div class="roxdiv rox-uploader" id="<%= ClientID%>" style="display: <%= (IsPart ? "block" : "none")%>;">
	<div class="rox-itemtoolbar" style="margin-top: 0px; font-size: 9pt;">
		<table border="0" width="100%" cellpadding="0" cellspacing="0"><tr><td>
		<span style="display: inline-block; padding: 2px 8px 2px 0px;">
		<%
			if ((Part != null) && Part.AllowSelectAction) {
		%>
		<asp:DropDownList runat="server" ID="ActionDropDown" AutoPostBack="true">
		</asp:DropDownList>
		<%
			} else {
		%>
		<%= JsonSchemaManager.GetDisplayName (Action, "UploadActions", false)%>:
		<%
			}
		%>
		</span>
		<a style="background-image: url('<%= WebUrl%>/_layouts/images/open.gif'); visibility: hidden;" id="<%= ClientID%>_roxnew"
			class="rox-itemtoolbar roxup-notonupload" href="#noop<%= Rnd.Next ()%>">
			<%= Bool ("nsd", false) ? ("&nbsp;" + this ["Uploader_AddFilesDrop"] + "&nbsp;") : this ["Uploader_AddFiles"]%></a>
		</td><td align="right">
		<span style="display: inline-block;" class="rox-itemtoolbar">
				<span style="visibility: hidden;" id="<%= ClientID%>_upload">
				<a style="background-image: url('<%= WebUrl%>/_layouts/images/roxority_UploadZen/upload.png'); font-weight: bold; color: #003759;"
					onclick="<%= GetUplScript()%>" href="#noop<%= Rnd.Next ()%>" class="roxup-notonupload">
					<%= this ["Uploader_Upload", ClientID]%></a>
				</span><span style="<%= (IsPart ? "display: none;" : string.Empty)%>"><a style="background-image: url('<%= WebUrl%>/_layouts/images/roxority_UploadZen/cross-white.png')"
						id="<%= ClientID%>_close" onclick="roxUps['<%= ClientID%>'].close();" href="#noop<%= Rnd.Next ()%>">
						<%= this ["Tool_ItemEditor_Close"]%></a></span>
						<a style="background-image: url('<%= WebUrl%>/_layouts/images/crit_16.gif'); display: none;"
						id="<%= ClientID%>_cancel" onclick="roxUps['<%= ClientID%>'].cancel();" href="#noop<%= Rnd.Next ()%>">
						<%= this ["Uploader_Cancel"]%></a> </span>
		</td></tr></table>
	</div>
	<div class="rox-uploadbox">
		<table border="0" cellpadding="0" cellspacing="0" width="100%">
			<tr>
				<td valign="top">
					<fieldset class="roxup-notonupload" id="<%= ClientID%>_fieldset">
						<legend><%= this ["Uploader_UploadTo"]%>
						<%
							if ((Part != null) && (Part.AllowSelectFolder || Part.AllowSelectLibPage || Part.AllowSelectLibSite || Part.AllowSelectLibWeb)) {
								if (Part.AllowSelectLibSite) {
							%>
							<asp:DropDownList runat="server" ID="WebDropDown" AutoPostBack="true">
							</asp:DropDownList>
							<%
								}
								if (Part.AllowSelectLibWeb || Part.AllowSelectLibPage || Part.AllowSelectLibSite) {
							%>
							<asp:DropDownList runat="server" ID="LibDropDown" AutoPostBack="true">
							</asp:DropDownList>
							<%
								}
								if (Part.AllowSelectFolder) {
							%>
							<asp:DropDownList runat="server" ID="FolderDropDown" AutoPostBack="true">
							</asp:DropDownList>
							<%
								}
							} else {
						%>
						<b><%= RootFolderUrl.Substring ((MenuItem.SpContext.Web.ServerRelativeUrl.Length > 1) && ("/" + RootFolderUrl).StartsWith (MenuItem.SpContext.Web.ServerRelativeUrl) ? MenuItem.SpContext.Web.ServerRelativeUrl.Length : 0).TrimStart ('/')%>:</b>
						<%
							}
						%>
						</legend>
						<div class="rox-uploadfieldset rox-uploadfieldset-files">
							<div id="<%= ClientID%>_nofiles" style="display: block; color: #808080; visibility: hidden;">
								<%= this ["Uploader_NoFiles"]%>
							</div>
							<div id="<%= ClientID%>_noruntime" style="display: block; color: #808080;">
								<%= this ["Uploader_Plugin1"] + (RuntimeBoth ? this ["Uploader_Plugin2"] : string.Empty) + (RuntimeSilver ? this ["Uploader_PluginSilver"] : string.Empty) + (RuntimeBoth ? this ["Uploader_Plugin3"] : string.Empty) + (RuntimeFlash ? this ["Uploader_PluginFlash"] : string.Empty)%>.
							</div>
							<div id="<%= ClientID%>_files" style="display: none;">
								<table id="<%= ClientID%>_files_table" class="rox-uptable" border="0" cellpadding="0" cellspacing="0" width="100%">
								</table>
								<%
									if (Bool ("st", true)) {
								%>
								<ul>
									<%
										if (ImageResize != null) {
									%>
									<li><%= this ["Uploader_NoteResize", ImageResize ["width"], ImageResize ["height"]]%></li>
									<%
										}
										if (!string.IsNullOrEmpty (Action ["nms"] + string.Empty)) {
									%>
									<li><%= this ["Uploader_NoteMaxSize", (Action ["nms"] + string.Empty).ToUpperInvariant ()]%></li>
									<%
										}
										if (ExtFilters != null) {
											string t;
											List<string> titles = new List<string> ();
											foreach (Hashtable ht in ExtFilters)
												if ((!string.IsNullOrEmpty (t = (ht ["title"] + string.Empty).Trim ())) && !titles.Contains (t))
													titles.Add (t);
									%>
									<li><%= this ["Uploader_NoteFilters", string.Join (" · ", titles.ToArray ())]%></li>
									<%
										}
										if ((MenuItem.List != null) && (MenuItem.List.ForceCheckout)) {
									%>
									<li><%= this ["Uploader_NoteCheckout"]%></li>
									<%
										} else {
									%>
									<li><%= this ["Uploader_NoteOverwrite", this ["Uploader_NoteOverwrite" + (Bool ("now", false) ? 1 : 2)]]%></li>
									<%
										}
									%>
									<li><%= this ["Uploader_NoteBlocked" + (Bool ("nrb", false) ? "1" : "2")]%></li>
									<li><%= this ["Uploader_NoteUpload"]%></li>
								</ul>
								<%
									}
								%>
							</div>
						</div>
					</fieldset>
				</td>
			</tr>
		</table>
	</div>
	<%
		if (ShowClickOnce || Bool ("sl", true) || Bool ("sh", true)) {
	%>
	<div class="rox-itemtoolbar" style="margin-top: 0px; background-color: #f4f4f4; font-size: 8pt;">
		<table border="0" width="100%" cellpadding="0" cellspacing="0"><tr><td>
		<%
			if (ShowClickOnce) {
		%>
		<a style="background-image: url('<%= WebUrl%>/_layouts/images/roxority_UploadZen/win16.png');" class="rox-itemtoolbar" href="<%= (WebUrl + ClickOnceUrl)%>">
			<%= this ["Uploader_Windows"]%>
		</a>
		<%
			} else if (Bool ("sl", true)) {
		%>
		Powered by
		<a class="roxzenlnk" target="_blank" href="http://SharePoint-Tools.net/UploadZen/">
			<strong>UploadZen</strong> <%= ProductPage.DisplayVersion%>
		</a>
		<%
			}
		%>
		</td><td align="right">
		<span style="display: inline-block;" class="rox-itemtoolbar">
			<%
				if (Bool ("sl", true) && ShowClickOnce) {
			%>
			Powered by
			<a class="roxzenlnk" target="_blank" href="http://SharePoint-Tools.net/UploadZen/">
				<strong>UploadZen</strong> <%= ProductPage.DisplayVersion%>
			</a>
			<%
				}
				if (Bool ("sh", true)) {
			%>
			<a style="background-image: url('<%= WebUrl%>/_layouts/images/hhelp.gif')" target="_blank" href="<%= WebUrl%>/_layouts/roxority_UploadZen/default.aspx?doc=intro">
				<%= this ["Uploader_Help"]%>
			</a>
			<%
				}
			%>
		</span>
		</td></tr></table>
	</div>
	<%
		}
	%>
</div>
<% if (HideCheckin) { %>
<style type="text/css">
.roxupcheckintd { display: none; }
</style>
<% } %>
<script language="JavaScript" type="text/javascript">
	var roxUpHideCheckIn = false;
	roxNewUpper('<%= ID%>', '<%= ClientID%>', '<%= SPEncode.ScriptEncode (TargetWebUrl)%>', <%= ((MenuItem.List != null) && MenuItem.List.ForceCheckout).ToString ().ToLowerInvariant ()%>, <%= Bool ("sh", true).ToString ().ToLowerInvariant ()%>, '<%= SPEncode.ScriptEncode (HttpUtility.UrlEncode (Action ["nrc"] + string.Empty))%>', '<%= SPEncode.ScriptEncode (HttpUtility.UrlEncode (RootFolderUrl.Substring (("/" + RootFolderUrl).StartsWith (TargetWeb.ServerRelativeUrl) ? (TargetWeb.ServerRelativeUrl.Length - 1) : 0)))%>', <%= InputCount%>, <%= IsChrome.ToString ().ToLowerInvariant ()%>, '<%= SPEncode.ScriptEncode (TargetWeb.ServerRelativeUrl.TrimEnd ('/') + "/_layouts/" + ProductPage.AssemblyName)%>', '<%= Action ["np"]%>', '<%= (Bool ("nsd", false) ? SPEncode.ScriptEncode (this ["Uploader_AddFilesDrop"]) : string.Empty)%>', '<%= SPEncode.ScriptEncode (this ["Uploader_AddFiles"])%>', <%= Bool ("nrb", false).ToString ().ToLowerInvariant ()%>, <%= JSON.JsonEncode (ExtFilters)%>, '<%= ListID%>', <%= Bool ("nuz", true).ToString ().ToLowerInvariant ()%>, <%= Bool ("nun", false).ToString ().ToLowerInvariant ()%>, <%= Bool ("now", false).ToString ().ToLowerInvariant ()%>, '<%= SPEncode.ScriptEncode ((Action ["nms"] + string.Empty).Trim ().ToLowerInvariant ())%>', <%= JSON.JsonEncode (ImageResize)%>, '<%= SPEncode.ScriptEncode ((Action ["ncs"] + string.Empty).Trim ().ToLowerInvariant ())%>', '<%= ("None".Equals (Action ["nci"]) ? string.Empty : (Action ["nci"] + string.Empty))%>', <%= Bool ("nca", true).ToString ().ToLowerInvariant ()%>);
	if (!roxExtIconsDone) {
		roxExtIconsDone = true;
		<%
			foreach (KeyValuePair<string, string> kvp in FileIcons) {
		%>
		roxExtIcons['<%= SPEncode.ScriptEncode (kvp.Key)%>'] = '<%= SPEncode.ScriptEncode (kvp.Value)%>';
		<%
			}
		%>
	}
	if (!roxLocDone) {
		roxLocDone = true;
		<%
			foreach (string s in new string [] { "BlockInfo", "SetAllZip", "SetAll", "ZipExtract0", "ZipExtract1", "ZipExtract2", "Messages", "FlashWarn", "StatusNone", "StatusQueue", "StatusDone", "StatusFail", "StatusCancel", "CheckIn", "CheckIn0", "CheckIn1", "CheckIn2", "CheckIn3", "File", "Status" }) {
		%>
		roxUpLoc['<%= SPEncode.ScriptEncode (s)%>'] = '<%= SPEncode.ScriptEncode (this ["Uploader_" + s])%>';
		<%
			}
		%>
	}
	roxUpFlags = <%= Flags%>;
	if (roxBlocked.length == 0) {
		<%
			foreach (string ext in roxority_UploadZen.UploadZenMenuItem.GetBlockedExtensions (TargetWeb)) {
		%>
		roxBlocked.push('<%= SPEncode.ScriptEncode (ext)%>');
		<%
			}
		%>
	}
	<%
		if (!ProductPage.Is14) {
	%>
	jQuery.fx.off = true;
	<%
		}
		if ((Request.QueryString ["roxuplshow"] == ClientID) || (Request.QueryString ["roxuplshow"] == ID)) {
	%>
	jQuery('#<%= ClientID%>').slideDown();
	<%
		}
	%>
</script>
<%
	if (!ProductPage.Is14) {
%>
<style type="text/css">
	div.rox-uploader * { font-size: 8pt !important; }
	div.rox-uploader { width: 98% !important; }
</style>
<%
	}
%>
