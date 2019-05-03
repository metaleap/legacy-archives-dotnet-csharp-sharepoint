<%-- _lcid="1033" _version="14.0.4758" _dal="1" --%>
<%-- _LocalBinding --%>
<%@ Assembly Name="Microsoft.Office.Policy.Pages, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%> <%@ Assembly Name="roxority_UploadZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %>
<%@ Assembly Name="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%> <%@ Page Language="C#" DynamicMasterPageFile="~masterurl/default.master" Inherits="roxority_UploadZen.UploadExPage"       %> <%@ Import Namespace="Microsoft.SharePoint.WebControls" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Import Namespace="Microsoft.SharePoint" %> <%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="~/_controltemplates/InputFormSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="~/_controltemplates/InputFormControl.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="~/_controltemplates/ButtonSection.ascx" %>
<asp:Content ContentPlaceHolderId="PlaceHolderPageDescription" runat="server">
		<asp:PlaceHolder id="MultipleUploadWarning" Visible="false" runat="server">
			 <TR> <TD height="8px"><IMG SRC="/_layouts/images/blank.gif" width=1 height=8 alt=""></TD></TR>
			 <TR><TD colspan=2>
							 <table cellpadding=2 cellspacing=1 width=100% class="ms-informationbar" style="margin-bottom: 5px;" border=0>
									 <tr><td width=10 valign=center style="padding: 4px">
									<img id="Img1" src="/_layouts/images/exclaim.gif" alt="<%$Resources:wss,exclaim_icon%>" runat="server"/></td><TD class="ms-descriptiontext"><SharePoint:EncodedLiteral ID="EncodedLiteral1" runat="server" text="<%$Resources:wss,upload_document_required_multiple%>" EncodeMethod='HtmlEncode'/>
							</td></tr></table>
			  </TR></TD>
	   </asp:PlaceHolder>
</asp:Content>
<%@ Register Tagprefix="Publishing" Namespace="Microsoft.SharePoint.Publishing.Internal.WebControls" Assembly="Microsoft.SharePoint.Publishing, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<asp:Content contentplaceholderid="PlaceHolderPageTitle" runat="server">
	<% if (IsDocumentLibrary && EnforceRouting) { %>
	<asp:Literal runat="server" Text='<%$Resources:dlcpolicy,uploadex_submit_title%>' />
	<% } else if (IsTemplateCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral2" runat="server" text="<%$Resources:wss,upload_pagetitle_template%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsWebPartCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral3" runat="server" text="<%$Resources:wss,upload_pagetitle_webpart%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsMasterPageCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral4" runat="server" text="<%$Resources:wss,upload_pagetitle_masterpage%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsPictureLibrary) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral5" runat="server" text="<%$Resources:wss,upload_pagetitle_imglib%>" EncodeMethod='HtmlEncode'/>
	<% } else {
		if (MultipleItemSection.Visible) { %>
		<SharePoint:EncodedLiteral ID="EncodedLiteral6" runat="server" text="<%$Resources:wss,upload_pagetitle_multiple%>" EncodeMethod='HtmlEncode'/>
		<% } else { %>
		<SharePoint:EncodedLiteral ID="EncodedLiteral7" runat="server" text="<%$Resources:wss,upload_pagetitle%>" EncodeMethod='HtmlEncode'/>
		<% }
	 } %>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderPageTitleInTitleArea" runat="server">
	<asp:HyperLink id="ListTitle" runat="server"/>
	<% if (IsDocumentLibrary && EnforceRouting) { %>
	: <asp:Literal runat="server" Text='<%$Resources:dlcpolicy,uploadex_submit_title%>' />
	<% } else if (IsTemplateCatalog) { %>
	: <SharePoint:EncodedLiteral ID="EncodedLiteral8" runat="server" text="<%$Resources:wss,upload_pagetitle_template%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsWebPartCatalog) { %>
	: <SharePoint:EncodedLiteral ID="EncodedLiteral9" runat="server" text="<%$Resources:wss,upload_pagetitle_webpart%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsMasterPageCatalog) { %>
	: <SharePoint:EncodedLiteral ID="EncodedLiteral10" runat="server" text="<%$Resources:wss,upload_pagetitle_masterpage%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsPictureLibrary) { %>
	: <SharePoint:EncodedLiteral ID="EncodedLiteral11" runat="server" text="<%$Resources:wss,upload_pagetitle_imglib%>" EncodeMethod='HtmlEncode'/>
	<% } else {
		if (MultipleItemSection.Visible) { %>
		: <SharePoint:EncodedLiteral ID="EncodedLiteral12" runat="server" text="<%$Resources:wss,upload_pagetitle_multiple%>" EncodeMethod='HtmlEncode'/>
		<% } else { %>
		: <SharePoint:EncodedLiteral ID="EncodedLiteral13" runat="server" text="<%$Resources:wss,upload_pagetitle%>" EncodeMethod='HtmlEncode'/>
		<% }
	 } %>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderAdditionalPageHead" runat="server">
<% if (IsDocumentLibrary && !EnforceRouting && !InsideDocSet && ListHasFolders) { %>
<SharePoint:ScriptLink ID="ScriptLink1" language="javascript" name="PickerTreeDialog.js" runat="server" />
<% } %>
<% if (IsPictureLibrary) { %>
<script type="text/javascript" src="imglib.js"></script>
<% } %>
<script type="text/javascript">
// <![CDATA[
function ULSfGQ(){var o=new Object;o.ULSTeamName="DLC Server";o.ULSFileName="UploadEx.aspx";return o;}
function _spBodyOnLoad()
{ULSfGQ:;
	var controlId;
	controlId = "<%= InputFile.ClientID %>";
	var c = document.getElementById(controlId);
	if (c != null)
		c.focus();
<% if (MultipleItemSection.Visible) { %>
	MultipleUploadView();
<% } else { %>
	var hideLink = true;
	if (browseris.ie5up && !browseris.mac)
	{
		try
		{
<% if (IsPictureLibrary) { %>
			if (new ActiveXObject("OISCTRL.OISClientLauncher") ||
				new ActiveXObject("STSUpld.UploadCtl"))
			{
				hideLink = false;
			}
<% } else { %>
			if (new ActiveXObject("STSUpld.UploadCtl"))
			{
				hideLink = false;
			}
<% } %>
		}
		catch(e)
		{
		}
	}
	if (hideLink)
	{
		var linkName = "<%=UploadMultipleLink.ClientID%>";
	 var linkElement = document.getElementById(linkName);
	 if( linkElement != null)
			linkElement.style.display = "none";
	}
<% } %>
}
<% if (MultipleItemSection.Visible) { %>
function _spFormOnSubmit()
{ULSfGQ:;
	var putoptsElement = document.getElementById("putopts");
	var overwriteElement = document.getElementById("<%= OverwriteMultiple.ClientID %>");
	putoptsElement.value = overwriteElement.checked ? "true" : "false";
	document.getElementById("idUploadCtl").MultipleUpload();
	return false;
}
function EnsureUploadCtl()
{ULSfGQ:;
	return browseris.ie5up && !browseris.mac;
}
function MultipleUploadView()
{ULSfGQ:;
	if (EnsureUploadCtl())
	{
		treeColor = GetTreeColor();
		document.getElementById("idUploadCtl").SetTreeViewColor(treeColor);
	}
}
function GetTreeColor()
{ULSfGQ:;
	var bkColor="";
	if(null != document.getElementById("onetidNavBar"))
		bkColor = document.getElementById("onetidNavBar").currentStyle.backgroundColor;
	if(bkColor=="")
	{
		var numStyleSheets = document.styleSheets.length;
		var uploadRule = null;
		for (i = numStyleSheets-1; i >= 0; i--)
		{
			var numRules = document.styleSheets(i).rules.length;
			for (var ruleIndex = numRules-1; ruleIndex >= 0; ruleIndex--)
			{
				if(document.styleSheets[i].rules.item(ruleIndex).selectorText==".ms-uploadcontrol")
					uploadRule = document.styleSheets[i].rules.item(ruleIndex);
			}
		}
		if (uploadRule)
			bkColor = uploadRule.style.backgroundColor;
	}
	return(bkColor);
}
<% } else { %>
function LaunchPictureLibraryApp()
{ULSfGQ:;
<% if (IsPictureLibrary) { %>
	vCurrentListID = "<%= CurrentList.ID %>";
	vCurrentListUrlAsHTML = "<%= Web.Url + "/" + (CurrentList.RootFolder.Url.Length > 0 ? CurrentList.RootFolder.Url + "/" : "") %>";
	vCurrentWebUrl = "<%= Web.Url %>";
	if (PLMultipleUploadView())
		return true;
	return false;
<% } %>
}
<% } %>
function VerifyCommentLength()
{ULSfGQ:;
<% if (VersionCommentSection.Visible) { %>
	var commentId = "<%= CheckInComment.ClientID %>";
	if (document.getElementById(commentId).value.length > 1023)
	{
		window.alert("<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,upload_comment_sizelimitexceeded_err%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>");
		document.getElementById(commentId).focus();
		return false;
	}
<% } %>
	return true;
}
<% if (IsDocumentLibrary && !EnforceRouting && !InsideDocSet && ListHasFolders) { %>
var updateCalledOnce = false;
function updateDestination()
{ULSfGQ:;
	if(!updateCalledOnce)
		updateCalledOnce = true;
	else
		return;
	var hiddenDestination = document.getElementById("destination");
	var hiddenRedirectUrl = document.getElementById("Confirmation-URL");
	var uploadLocation = document.getElementById(uplocid).value;
	if(uploadLocation != null && uploadLocation != "")
	{
		hiddenDestination.value = hiddenDestination.value + uploadLocation;
		if(!isPopUI)
			hiddenRedirectUrl.value = hiddenRedirectUrl.value + hiddenDestination.value;
	}
}
<% } %>
function ForceRestoreToOriginalFormAction()
{ULSfGQ:;
	if (_spOriginalFormAction != null)
	{
		document.forms[0].action = _spOriginalFormAction;
		document.forms[0]._initialAction = document.forms[0].action;
		_spOriginalFormAction = null;
		_spEscapedFormAction = null;
	}
}
function LaunchTargetPicker()
{ULSfGQ:;
	var callback = function(dest)
	{ULSfGQ:;
		if (dest != null && dest != undefined && dest[3] != null)
		{
			document.getElementById(uplocid).value = dest[3];
			currSelectionId = dest[0];
		}
	};
	libraryRelUrl = document.getElementById(uplocid).value;
	if(libraryRelUrl == null || libraryRelUrl == "" || libraryRelUrl == "/")
	{
		libraryRelUrl = "";
	}
	else if(libraryRelUrl.charAt(0) != '/')
	{
		libraryRelUrl = "/" + libraryRelUrl;
	}
	currSelectionUrl = libraryUrl + libraryRelUrl;
	LaunchPickerTreeDialogSelectUrl('CbqPickerSelectFolderTitle', 'CbqPickerSelectFolderText', 'websListsFolders', currAnchor, webUrl, currSelectionUrl, '', '', '/_layouts/images/smt_icon.gif', '', callback, 'true', '');
}
var supportedExtensions = new Array(".docx",".docm",".dotx",".dotm",".pptx",".pptm",".potx",".ppsx",".ppsm",".xlsx",".xlsb",".xlsm",".xltx",".xltm",".gif",".jpeg",".jpg",".jpe",".jfif",".bmp",".dib",".png",".tif",".tiff",".ico",".wdp",".hdp",".one",".onetoc2");
var silverlightExtensions;
function GetSilverLightExtensions()
{ULSfGQ:;
	if(silverlightExtensions == null)
	{
		silverlightExtensions = new Array();
		<% foreach (string ext in Microsoft.SharePoint.Publishing.WebControls.MediaWebPart.SilverlightMediaFileExtensions) { %>
			var tempExtension = <%= "\"" + ext.ToLower() + "\"" %>;
			if(tempExtension.charAt(0) != '.')
				tempExtension = '.' + tempExtension;
			silverlightExtensions.push(tempExtension);
		<% } %>
		supportedExtensions = supportedExtensions.concat(silverlightExtensions);
	}
}
function CheckAssetLibMediaExtension()
{ULSfGQ:;
	<% if(CheckAssetLibraryForFileExtension) { %>
	var fileDiv = document.getElementById("<%= InputFile.ClientID %>");
	var showWarning = fileDiv != null;
	if (showWarning)
	{
		var filePath = fileDiv.value;
		var fileExtension = filePath.substr(filePath.lastIndexOf('.')).toLowerCase();
		GetSilverLightExtensions();
		for (var i = 0; i < supportedExtensions.length; i++)
		{
			if (fileExtension == supportedExtensions[i])
			{
				showWarning = false;
				break;
			 }
		}
	}
	var warningDiv = document.getElementById("AssetLibMediaWarning");
	if (warningDiv)
	{
		warningDiv.style.display = showWarning ? "" : "none";
	}
	<% } %>
}
// ]]>
</script>
<style type="text/css">
	.routing_warning {
		color: black;
		background: rgb(255,251,231);
		margin: .5em .5em .5em .5em;
		padding: .3em .3em .3em .3em;
		border-color: black;
		border-style: solid;
		border-width: thin;
		border-color: rgb(255,239,181);
	}
</style>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderMain" runat="server">
	<input type="hidden" name="destination" id="destination" value="<asp:Literal ID='Destination' runat='server'/>" />
	<SharePoint:SPPageStatusSetter id="PageStatus" runat="server"/>
	<% if(CheckAssetLibraryForFileExtension) { %>
	<SharePoint:UIVersionedContent ID="UIVersionedContent1" UIVersion="4" runat="server">
		<ContentTemplate>
			<div id="AssetLibMediaWarning" style="display:none;" class="s4-status-s3" >
		</ContentTemplate>
	</SharePoint:UIVersionedContent>
	<SharePoint:UIVersionedContent ID="UIVersionedContent2" UIVersion="3" runat="server">
		<ContentTemplate>
			<div id="AssetLibMediaWarning" style="display:none;" class="routing_warning" >
		</ContentTemplate>
	</SharePoint:UIVersionedContent>
	<asp:Literal runat="server" Text='<%$Resources:dlcpolicy,uploadex_file_assetlib_warning%>' />
	</div>
	<% } %>
	<table border="0" width="100%" cellspacing="0" cellpadding="0" class="ms-descriptiontext">
	<Control id="SingleItemSection" runat="server">
	<wssuc:InputFormSection runat="server"
		id="UploadDocumentSection"
		Title="<%$Resources:wss,upload_document_title%>">
		<Template_Description>
		<% if (IsPictureLibrary) { %>
			<SharePoint:EncodedLiteral ID="EncodedLiteral14" runat="server" text="<%$Resources:wss,upload_picture_description%>" EncodeMethod='HtmlEncode'/>
		<% } else if(EnforceRouting) { %>
			<asp:Literal runat="server" Text='<%$Resources:dlcpolicy,uploadex_submit_description%>' />
		<% } else { %>
			<SharePoint:EncodedLiteral ID="EncodedLiteral15" runat="server" text="<%$Resources:wss,upload_document_description%>" EncodeMethod='HtmlEncode'/>
		<% } %>
		</Template_Description>
		<Template_InputFormControls>
			<wssuc:InputFormControl runat="server"  LabelText="<%$Resources:wss,multipages_Name%>" LabelAssociatedControlId="InputFile">
			<Template_Control>
			   <table class="ms-authoringcontrols" width="100%">
				<tr><td>
					 <span dir="ltr">
						<input type="file" id="InputFile" runat="server" class="ms-fileinput" size="35" onchange="CheckAssetLibMediaExtension()"/>
					 </span>
				</td></tr>
				<tr><td>
				<wssawc:InputFormRequiredFieldValidator ID="InputFormRequiredFieldValidator1" ControlToValidate="InputFile"
					Display="Dynamic" ErrorMessage="<%$Resources:wss,upload_document_file_missing%>" Runat="server"
					BreakBefore="false" BreakAfter="false" />
				<asp:CustomValidator ControlToValidate="InputFile"
					Display = "Dynamic"
					ErrorMessage = "<%$Resources:wss,upload_document_file_invalid%>"
					OnServerValidate="ValidateFile"
					runat="server"/>
					</td></tr><tr><td>
					<% RenderUploadZenActions (__w); %>
					<asp:HyperLink id="UploadMultipleLink" runat="server"  ACCESSKEY=U Text="<%$SPHtmlEncodedResources:wss,upload_document_upload_multiple%>" onclick="javascript:return !LaunchPictureLibraryApp();"/>
				</td></tr>
				<% if ((IsDocumentLibrary && !EnforceRouting) || !IsDocumentLibrary) { %>
				<tr><td><asp:CheckBox id="OverwriteSingle" Checked="true" Text="<%$Resources:wss,upload_document_overwrite_file%>" runat="server" /></td></tr>
				<% } %>
				</table>
			</Template_Control>
			</wssuc:InputFormControl>
		</Template_InputFormControls>
	</wssuc:InputFormSection>
	<% if (IsDocumentLibrary && !EnforceRouting && !InsideDocSet && ListHasFolders) { %>
	<wssuc:InputFormSection
		Title="<%$Resources:dlcpolicy,uploadex_target_location_title%>"
		runat="server">
		<Template_Description>
		<asp:Literal runat="server" Text='<%$Resources:dlcpolicy,uploadex_target_location_description%>' />
		</Template_Description>
		<Template_InputFormControls>
			<wssuc:InputFormControl runat="server" LabelText="<%$Resources:wss,WrkStat_Folder%>" />
			<wssuc:InputFormControl runat="server">
			<Template_Control>
				<table class="ms-authoringcontrols" width="100%">
				<tr><td  valign="top">
					<span dir="ltr">
						<wssawc:InputFormTextBox runat="server" id="uploadLocation" FieldName="RoutingTargetPath"/>
					</span>
				</td><td valign="top">
					<asp:Button class="ms-input" runat="server" Text="<%$Resources:dlcpolicy,uploadex_choose_folder%>" id="selectTargetButton" onclientclick="LaunchTargetPicker();return false;" />
				</td></tr></table>
			</Template_Control>
			</wssuc:InputFormControl>
		</Template_InputFormControls>
	</wssuc:InputFormSection>
	<% } %>
	<wssuc:InputFormSection runat="server"
		id="VersionCommentSection"
		Title="<%$Resources:wss,upload_version_title%>">
		<Template_Description>
			<SharePoint:EncodedLiteral ID="EncodedLiteral16" runat="server" text="<%$Resources:wss,upload_version_description%>" EncodeMethod='HtmlEncode'/>
		</Template_Description>
		<Template_InputFormControls>
			<wssuc:InputFormControl runat="server"  LabelText="<%$Resources:wss,upload_version_comments%>" LabelAssociatedControlId="CheckInComment">
			<Template_Control>
				<asp:TextBox TextMode="MultiLine" Rows="5" Columns="45" id="CheckInComment" class="ms-long" runat="server" />
			</Template_Control>
			</wssuc:InputFormControl>
		</Template_InputFormControls>
	</wssuc:InputFormSection>
	</Control>
	<Control id="MultipleItemSection" runat="server" Visible="false">
	<wssuc:InputFormSection runat="server"
		Title="">
		<Template_Description>
		</Template_Description>
		<Template_InputFormControls>
			<wssuc:InputFormControl runat="server">
			<Template_Control>
				<asp:CheckBox id="OverwriteMultiple" Checked="true" Text="<%$Resources:wss,upload_document_overwrite_version%>" runat="server" />
			</Template_Control>
			</wssuc:InputFormControl>
		</Template_InputFormControls>
	</wssuc:InputFormSection>
	<% if (IsDocumentLibrary && !EnforceRouting && !InsideDocSet && ListHasFolders) { %>
	<wssuc:InputFormSection
		id="LocationPickerSectionMultiple"
		Title="<%$Resources:dlcpolicy,uploadex_target_location_title%>"
		runat="server">
		<Template_Description>
		<asp:Literal runat="server" Text='<%$Resources:dlcpolicy,uploadex_target_location_description%>' />
		</Template_Description>
		<Template_InputFormControls>
			<wssuc:InputFormControl runat="server" LabelText="<%$Resources:wss,WrkStat_Folder%>" />
			<wssuc:InputFormControl runat="server">
			<Template_Control>
				<table><tr><td valign="top">
				<wssawc:InputFormTextBox runat="server" id="uploadLocationMultiple" FieldName="RoutingTargetPath"/>
				</td><td valign="top">
					<asp:Button class="ms-input" runat="server" Text="<%$Resources:dlcpolicy,uploadex_choose_folder%>" id="selectTargetButtonMultiple" onclientclick="LaunchTargetPicker();return false;" />
				</td></tr></table>
			</Template_Control>
			</wssuc:InputFormControl>
		</Template_InputFormControls>
	</wssuc:InputFormSection>
	<% } %>
	<tr id=trUploadCtl><td width="100%" colspan="3">
		<table cellpadding="0" cellspacing="0" width="100%" height="100%" border="0">
			<tr><td id="idUploadTD" name="idUploadTD" class="ms-uploadborder" width="100%" height="100%">
				<script type="text/javascript">
// <![CDATA[
				RenderActiveX("<object id='idUploadCtl' name='idUploadCtl' classid='CLSID:07B06095-5687-4d13-9E32-12B4259C9813' width='638px' height='261px'></object>");
// ]]>
				</script>
		</table>
		<input type="hidden" name="PostURL" id="PostURL" value="<asp:Literal ID='PostURL' runat='server'/>" />
		<input type="hidden" name="Confirmation-URL" id="Confirmation-URL" value="<asp:Literal ID='ConfirmationURL' runat='server'/>" />
		<input type="hidden" name="putopts" id="putopts" value="true"/>
		 <!--webbot bot="FileUpload" S-Label-Fields="TRUE" B-Reverse-Chronology="FALSE" S-Date-Format="%A, %B %d, %Y" S-Time-Format="%I:%M:%S %p" S-Form-Fields="file destination" S-Format="HTML/UL" B-Dynamic-Redirect="TRUE" B-Process-Metainfo="TRUE" startspan -->
		<input type="hidden" name="VTI-GROUP" value="0"/>
		<input type="hidden" name="Cmd" value="Save"/>
		<!--webbot bot="FileUpload" endspan -->
	</td></tr>
	</Control>
	<wssuc:ButtonSection runat="server">
		<Template_Buttons>
			<input id="btnOK" runat="server" Type="button" AccessKey="<%$Resources:wss,multipages_okbutton_accesskey%>" class="ms-ButtonHeightWidth" Value="<%$Resources:wss,multipages_okbutton_text%>" onclick="javascript:if (!VerifyCommentLength()) return false;" OnServerClick="OnSubmit" />
		</Template_Buttons>
	</wssuc:ButtonSection>
	</table>
</asp:Content>
