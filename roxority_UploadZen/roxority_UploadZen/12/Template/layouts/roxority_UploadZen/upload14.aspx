<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%> <%@ Assembly Name="roxority_UploadZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %> <%@ Page Language="C#" DynamicMasterPageFile="~masterurl/default.master" Inherits="roxority_UploadZen.UploadPage" %> <%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Import Namespace="Microsoft.SharePoint" %> <%@ Assembly Name="Microsoft.Web.CommandUI, Version=14.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
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
<asp:Content contentplaceholderid="PlaceHolderPageTitle" runat="server">
	<%= strListTitle %>
	<% if (IsTemplateCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral2" runat="server" text="<%$Resources:wss,upload_pagetitle_template%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsWebPartCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral3" runat="server" text="<%$Resources:wss,upload_pagetitle_webpart%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsMasterPageCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral4" runat="server" text="<%$Resources:wss,upload_pagetitle_masterpage%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsPictureLibrary) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral5" runat="server" text="<%$Resources:wss,upload_pagetitle_imglib%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsSolutionCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral6" runat="server" text="<%$Resources:wss,upload_pagetitle_solutiongallery%>" EncodeMethod='HtmlEncode'/>
	<% } else {
		if (MultipleItemSection.Visible) { %>
		<SharePoint:EncodedLiteral ID="EncodedLiteral7" runat="server" text="<%$Resources:wss,upload_pagetitle_multiple%>" EncodeMethod='HtmlEncode'/>
		<% } else { %>
		<SharePoint:EncodedLiteral ID="EncodedLiteral8" runat="server" text="<%$Resources:wss,upload_pagetitle%>" EncodeMethod='HtmlEncode'/>
		<% }
	 } %>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderPageTitleInTitleArea" runat="server">
	<asp:HyperLink id="ListTitle" runat="server"/>
	<% if (IsTemplateCatalog) { %>
	: <SharePoint:EncodedLiteral ID="EncodedLiteral9" runat="server" text="<%$Resources:wss,upload_pagetitle_template%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsWebPartCatalog) { %>
	: <SharePoint:EncodedLiteral ID="EncodedLiteral10" runat="server" text="<%$Resources:wss,upload_pagetitle_webpart%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsMasterPageCatalog) { %>
	: <SharePoint:EncodedLiteral ID="EncodedLiteral11" runat="server" text="<%$Resources:wss,upload_pagetitle_masterpage%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsPictureLibrary) { %>
	: <SharePoint:EncodedLiteral ID="EncodedLiteral12" runat="server" text="<%$Resources:wss,upload_pagetitle_imglib%>" EncodeMethod='HtmlEncode'/>
	<% } else {
		if (MultipleItemSection.Visible) { %>
		: <SharePoint:EncodedLiteral ID="EncodedLiteral13" runat="server" text="<%$Resources:wss,upload_pagetitle_multiple%>" EncodeMethod='HtmlEncode'/>
		<% } else { %>
		: <SharePoint:EncodedLiteral ID="EncodedLiteral14" runat="server" text="<%$Resources:wss,upload_pagetitle%>" EncodeMethod='HtmlEncode'/>
		<% }
	 } %>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderAdditionalPageHead" runat="server">
<% if (IsPictureLibrary) { %>
<script type="text/javascript" src="/_layouts/<%=System.Threading.Thread.CurrentThread.CurrentUICulture.LCID%>/imglib.js"></script>
<% } %>
<script type="text/javascript">
// <![CDATA[
function ULSk4d(){var o=new Object;o.ULSTeamName="Microsoft SharePoint Foundation";o.ULSFileName="Upload.aspx";return o;}
function _spBodyOnLoad()
{ULSk4d:;
	var controlId;
<% if (MultipleItemSection.Visible) { %>
	controlId = "<%= OverwriteMultiple.ClientID %>";
<% } else { %>
	controlId = "<%= InputFile.ClientID %>";
<% } %>
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
<% } else if (IsSolutionCatalog) { %>
			hideLink = true;
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
{ULSk4d:;
	var putoptsElement = document.getElementById("putopts");
	var overwriteElement = document.getElementById("<%= OverwriteMultiple.ClientID %>");
	putoptsElement.value = overwriteElement.checked ? "true" : "false";
	document.getElementById("idUploadCtl").MultipleUpload();
	return false;
}
function EnsureUploadCtl()
{ULSk4d:;
	return browseris.ie5up && !browseris.mac;
}
function MultipleUploadView()
{ULSk4d:;
	if (EnsureUploadCtl())
	{
		treeColor = GetTreeColor();
		document.getElementById("idUploadCtl").SetTreeViewColor(treeColor);
	}
}
function GetTreeColor()
{ULSk4d:;
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
{ULSk4d:;
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
{ULSk4d:;
<% if (VersionCommentSection.Visible) { %>
	var commentId = "<%= CheckInComment.ClientID %>";
	var verComment = document.getElementById(commentId).value;
	if (escapeProperly(verComment).length > 1023)
	{
		window.alert("<SharePoint:EncodedLiteral runat='server' text='<%$Resources:wss,upload_comment_sizelimitexceeded_err%>' EncodeMethod='EcmaScriptStringLiteralEncode'/>");
		document.getElementById(commentId).focus();
		return false;
	}
<% } %>
	return true;
}
function btnDisabled(set)
{ULSk4d:;
	if(set)
		document.getElementById("<%= btnOK.ClientID %>").disabled = true;
	else
		document.getElementById("<%= btnOK.ClientID %>").disabled = false;
}
function processInput()
{ULSk4d:;
	if(!VerifyCommentLength())
		return false;
	if(!document.getElementById("<%= InputFile.ClientID %>"))
		return true;
	if(document.getElementById("<%= InputFile.ClientID %>").value == "")
		return false;
	btnDisabled(true);
}
function ResetSpFormOnSubmitCalled()
{ULSk4d:;
	_spFormOnSubmitCalled = false;
	return true;
}
// ]]>
</script>
</asp:Content>
<asp:Content ContentPlaceHolderId="PlaceHolderMain" runat="server">
	<input type="hidden" name="destination" id="destination" value="<asp:Literal ID='Destination' runat='server'/>" />
	<table border="0" width="100%" cellspacing="0" cellpadding="0" class="ms-descriptiontext">
	<Control id="SingleItemSection" runat="server">
	<wssuc:InputFormSection runat="server">
		<Template_Title>
			<% if (IsSolutionCatalog) { %>
				<SharePoint:EncodedLiteral ID="EncodedLiteral15" runat="server" text="<%$Resources:wss,upload_solution_title%>" EncodeMethod='HtmlEncode'/>
			<% } else { %>
				<SharePoint:EncodedLiteral ID="EncodedLiteral16" runat="server" text="<%$Resources:wss,upload_document_title%>" EncodeMethod='HtmlEncode'/>
			<% } %>
		</Template_Title>
		<Template_Description>
			<% if (IsPictureLibrary) { %>
				<SharePoint:EncodedLiteral ID="EncodedLiteral17" runat="server" text="<%$Resources:wss,upload_picture_description%>" EncodeMethod='HtmlEncode'/>
			<% } else if (IsSolutionCatalog) { %>
				<SharePoint:EncodedLiteral ID="EncodedLiteral18" runat="server" text="<%$Resources:wss,upload_solution_description%>" EncodeMethod='HtmlEncode'/>
			<% } else { %>
				<SharePoint:EncodedLiteral ID="EncodedLiteral19" runat="server" text="<%$Resources:wss,upload_document_description%>" EncodeMethod='HtmlEncode'/>
			<% } %>
		</Template_Description>
		<Template_InputFormControls>
			<wssuc:InputFormControl runat="server"  LabelText="<%$Resources:wss,multipages_Name%>" LabelAssociatedControlId="InputFile">
			<Template_Control>
			   <table class="ms-authoringcontrols" width="100%">
				<tr><td>
					 <span dir="ltr">
						<input type="file" id="InputFile" runat="server" class="ms-fileinput" size="35" onfocus="ResetSpFormOnSubmitCalled();" />
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
				<tr><td><asp:CheckBox id="OverwriteSingle" Checked="true" Text="<%$Resources:wss,upload_document_overwrite_file%>" runat="server" CssClass="ms-upload-overwrite-cb" /></td></tr></table>
			</Template_Control>
			</wssuc:InputFormControl>
		</Template_InputFormControls>
	</wssuc:InputFormSection>
	<wssuc:InputFormSection runat="server"
	    id="VersionCommentSection"
		Title="<%$Resources:wss,upload_version_title%>">
		<Template_Description>
			<SharePoint:EncodedLiteral ID="EncodedLiteral20" runat="server" text="<%$Resources:wss,upload_version_description%>" EncodeMethod='HtmlEncode'/>
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
	<tr id="trUploadCtl"><td width="100%" colspan="3">
		<table cellpadding="0" cellspacing="0" width="100%" height="100%" border="0">
			<tr><td class="ms-sectionline" height="1"><img src="/_layouts/images/blank.gif" width='1' height='1' alt="" /></td></tr>
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
	</Control>
	<wssuc:ButtonSection runat="server">
		<Template_Buttons>
			<input id="btnOK" runat="server" type="button" accesskey="<%$Resources:wss,multipages_okbutton_accesskey%>" class="ms-ButtonHeightWidth" value="<%$Resources:wss,multipages_okbutton_text%>" onclick="processInput();" onserverclick="OnSubmit" />
		</Template_Buttons>
	</wssuc:ButtonSection>
	</table>
</asp:Content>
<%@ Register TagPrefix="wssuc" TagName="TopNavBar" src="~/_controltemplates/TopNavBar.ascx" %>
<asp:Content contentplaceholderid="PlaceHolderTopNavBar" runat="server">
  <wssuc:TopNavBar id="IdTopNavBar" runat="server" Version="4" ShouldUseExtra="true"/>
</asp:Content>
<asp:Content contentplaceholderid="PlaceHolderHorizontalNav" runat="server"/>
<asp:Content contentplaceholderid="PlaceHolderSearchArea" runat="server"/>
<asp:Content contentplaceholderid="PlaceHolderTitleBreadcrumb" runat="server">
  <SharePoint:UIVersionedContent ID="UIVersionedContent1" UIVersion="3" runat="server"><ContentTemplate>
	<asp:SiteMapPath
		SiteMapProvider="SPXmlContentMapProvider"
		id="ContentMap"
		SkipLinkText=""
		NodeStyle-CssClass="ms-sitemapdirectional"
		RootNodeStyle-CssClass="s4-die"
		PathSeparator="&#160;&gt; "
		PathSeparatorStyle-CssClass = "s4-bcsep"
		runat="server" />
  </ContentTemplate></SharePoint:UIVersionedContent>
  <SharePoint:UIVersionedContent ID="UIVersionedContent2" UIVersion="4" runat="server"><ContentTemplate>
	<SharePoint:ListSiteMapPath
		runat="server"
		SiteMapProviders="SPSiteMapProvider,SPXmlContentMapProvider"
		RenderCurrentNodeAsLink="false"
		PathSeparator=""
		CssClass="s4-breadcrumb"
		NodeStyle-CssClass="s4-breadcrumbNode"
		CurrentNodeStyle-CssClass="s4-breadcrumbCurrentNode"
		RootNodeStyle-CssClass="s4-breadcrumbRootNode"
		HideInteriorRootNodes="true"
		SkipLinkText="" />
  </ContentTemplate></SharePoint:UIVersionedContent>
</asp:Content>
