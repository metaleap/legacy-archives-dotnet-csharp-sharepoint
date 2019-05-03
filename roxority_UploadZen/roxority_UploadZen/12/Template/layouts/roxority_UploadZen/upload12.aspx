<%@ Assembly Name="Microsoft.SharePoint.ApplicationPages, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c"%> <%@ Assembly Name="roxority_UploadZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %>  <%@ Page Language="C#" Inherits="roxority_UploadZen.UploadPage" MasterPageFile="~/_layouts/application.master"      %> <%@ Import Namespace="Microsoft.SharePoint.ApplicationPages" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="Utilities" Namespace="Microsoft.SharePoint.Utilities" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Register Tagprefix="wssawc" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %> <%@ Register Tagprefix="SharePoint" Namespace="Microsoft.SharePoint.WebControls" Assembly="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormSection" src="~/_controltemplates/InputFormSection.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="InputFormControl" src="~/_controltemplates/InputFormControl.ascx" %>
<%@ Register TagPrefix="wssuc" TagName="ButtonSection" src="~/_controltemplates/ButtonSection.ascx" %>
<asp:Content ID="Content1" contentplaceholderid="PlaceHolderPageTitle" runat="server">
	<% if (IsTemplateCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral1" runat="server" text="<%$Resources:wss,upload_pagetitle_template%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsWebPartCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral2" runat="server" text="<%$Resources:wss,upload_pagetitle_webpart%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsMasterPageCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral3" runat="server" text="<%$Resources:wss,upload_pagetitle_masterpage%>" EncodeMethod='HtmlEncode'/>
	<% } else if (IsPictureLibrary) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral4" runat="server" text="<%$Resources:wss,upload_pagetitle_imglib%>" EncodeMethod='HtmlEncode'/>
	<% } else { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral5" runat="server" text="<%$Resources:wss,upload_pagetitle%>" EncodeMethod='HtmlEncode'/>
	<% } %>
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderId="PlaceHolderPageTitleInTitleArea" runat="server">
	<% if (IsTemplateCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral6" runat="server" text="<%$Resources:wss,upload_pagetitle_template%>" EncodeMethod='HtmlEncode'/>:
	<% } else if (IsWebPartCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral7" runat="server" text="<%$Resources:wss,upload_pagetitle_webpart%>" EncodeMethod='HtmlEncode'/>:
	<% } else if (IsMasterPageCatalog) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral8" runat="server" text="<%$Resources:wss,upload_pagetitle_masterpage%>" EncodeMethod='HtmlEncode'/>:
	<% } else if (IsPictureLibrary) { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral9" runat="server" text="<%$Resources:wss,upload_pagetitle_imglib%>" EncodeMethod='HtmlEncode'/>:
	<% } else { %>
	<SharePoint:EncodedLiteral ID="EncodedLiteral10" runat="server" text="<%$Resources:wss,upload_pagetitle%>" EncodeMethod='HtmlEncode'/>:
	<% } %>
	<asp:HyperLink id="ListTitle" runat="server"/>
</asp:Content>
<asp:Content ID="Content3" ContentPlaceHolderId="PlaceHolderAdditionalPageHead" runat="server">
<% if (IsPictureLibrary) { %>
<script src="/_layouts/<%=System.Threading.Thread.CurrentThread.CurrentUICulture.LCID%>/imglib.js"></script>
<% } %>
<SCRIPT Language="Javascript">
function _spBodyOnLoad()
{
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
{
	var putoptsElement = document.getElementById("putopts");
	var overwriteElement = document.getElementById("<%= OverwriteMultiple.ClientID %>");
	putoptsElement.value = overwriteElement.checked ? "true" : "false";
	document.getElementById("idUploadCtl").MultipleUpload();
	return false;
}
function EnsureUploadCtl()
{
	return browseris.ie5up && !browseris.mac;
}
function MultipleUploadView()
{
	if (EnsureUploadCtl())
	{
		treeColor = GetTreeColor();
		document.getElementById("idUploadCtl").SetTreeViewColor(treeColor);
	}
}
function GetTreeColor()
{
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
{
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
{
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
</script>
</asp:Content>
<asp:Content ID="Content4" ContentPlaceHolderId="PlaceHolderPageDescription" runat="server">
		<asp:PlaceHolder id="MultipleUploadWarning" Visible="false" runat="server">
			 <TR> <TD height="8px"><IMG SRC="/_layouts/images/blank.gif" width=1 height=8 alt=""></TD></TR>
			 <TR><TD colspan=2>
							 <table cellpadding=2 cellspacing=1 width=100% class="ms-informationbar" style="margin-bottom: 5px;" border=0>
									 <tr><td width=10 valign=center style="padding: 4px">
									<img id="Img1" src="/_layouts/images/exclaim.gif" alt="<%$Resources:wss,exclaim_icon%>" runat="server"/></td><TD class="ms-descriptiontext"><SharePoint:EncodedLiteral ID="EncodedLiteral11" runat="server" text="<%$Resources:wss,upload_document_required_multiple%>" EncodeMethod='HtmlEncode'/>
							</td></tr></table>
			  </TR></TD>
	   </asp:PlaceHolder>
</asp:Content>
<asp:Content ID="Content5" ContentPlaceHolderId="PlaceHolderMain" runat="server">
	<INPUT TYPE=hidden NAME="destination" id="destination" VALUE="<asp:Literal ID='Destination' runat='server'/>" />
	<TABLE border="0" width="100%" cellspacing="0" cellpadding="0" class="ms-descriptiontext">
	<Control id="SingleItemSection" runat="server">
	<wssuc:InputFormSection runat="server"
		Title="<%$Resources:wss,upload_document_title%>">
		<Template_Description>
			 <% if (IsPictureLibrary) { %>
			<SharePoint:EncodedLiteral ID="EncodedLiteral12" runat="server" text="<%$Resources:wss,upload_picture_description%>" EncodeMethod='HtmlEncode'/>
		<% } else { %>
			<SharePoint:EncodedLiteral ID="EncodedLiteral13" runat="server" text="<%$Resources:wss,upload_document_description%>" EncodeMethod='HtmlEncode'/>
		<% } %>
		</Template_Description>
		<Template_InputFormControls>
			<wssuc:InputFormControl runat="server"  LabelText="<%$Resources:wss,multipages_Name%>" LabelAssociatedControlId="InputFile">
			<Template_Control>
			   <TABLE class="ms-authoringcontrols" width="100%">
				<TR><TD>
					 <SPAN dir="ltr">
						<input type="file" id="InputFile" runat="server" class="ms-fileinput" size="35" />
					 </SPAN>
				</TD></TR>
				<TR><TD>
				<wssawc:InputFormRequiredFieldValidator ID="InputFormRequiredFieldValidator1" ControlToValidate="InputFile"
					Display="Dynamic" ErrorMessage="<%$Resources:wss,upload_document_file_missing%>" Runat="server"
					BreakBefore="false" BreakAfter="false" />
				<asp:CustomValidator ID="CustomValidator1" ControlToValidate="InputFile"
					Display = "Dynamic"
					ErrorMessage = "<%$Resources:wss,upload_document_file_invalid%>"
					OnServerValidate="ValidateFile"
					runat="server"/>
					</TD></TR><TR><TD>
					<asp:HyperLink id="UploadMultipleLink" runat="server"  ACCESSKEY=U Text="<%$SPHtmlEncodedResources:wss,upload_document_upload_multiple%>" onclick="javascript:return !LaunchPictureLibraryApp();"/>
				</TD></TR>
				<TR><TD><asp:CheckBox id="OverwriteSingle" Checked="true" Text="<%$Resources:wss,upload_document_overwrite_file%>" runat="server" /></TD></TR></TABLE>
			</Template_Control>
			</wssuc:InputFormControl>
		</Template_InputFormControls>
	</wssuc:InputFormSection>
	<wssuc:InputFormSection runat="server"
	    id="VersionCommentSection"
		Title="<%$Resources:wss,upload_version_title%>">
		<Template_Description>
			<SharePoint:EncodedLiteral ID="EncodedLiteral14" runat="server" text="<%$Resources:wss,upload_version_description%>" EncodeMethod='HtmlEncode'/>
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
		Title="<%$Resources:wss,upload_document_title%>">
		<Template_Description>
			 <% if (IsPictureLibrary) { %>
			<SharePoint:EncodedLiteral ID="EncodedLiteral15" runat="server" text="<%$Resources:wss,upload_picture_description%>" EncodeMethod='HtmlEncode'/>
		<% } else { %>
			<SharePoint:EncodedLiteral ID="EncodedLiteral16" runat="server" text="<%$Resources:wss,upload_document_description%>" EncodeMethod='HtmlEncode'/>
		<% } %>
		</Template_Description>
		<Template_InputFormControls>
			<wssuc:InputFormControl runat="server">
			<Template_Control>
				<asp:CheckBox id="OverwriteMultiple" Checked="true" Text="<%$Resources:wss,upload_document_overwrite_version%>" runat="server" />
			</Template_Control>
			</wssuc:InputFormControl>
		</Template_InputFormControls>
	</wssuc:InputFormSection>
	<TR id=trUploadCtl><TD WIDTH="100%" colspan="3">
		<TABLE CELLPADDING=0 CELLSPACING=0 WIDTH="100%" HEIGHT="100%" BORDER=0>
			<TR><TD id=idUploadTD name=idUploadTD class="ms-uploadborder" WIDTH="100%" HEIGHT="100%">
				<script>RenderActiveX("<OBJECT id=idUploadCtl name=idUploadCtl CLASSID=CLSID:07B06095-5687-4d13-9E32-12B4259C9813 WIDTH='100%' HEIGHT='350px'></OBJECT>");</script>
		</TABLE>
		<INPUT TYPE=hidden NAME="PostURL" id=PostURL VALUE="<asp:Literal ID='PostURL' runat='server'/>" />
		<INPUT TYPE=hidden NAME="Confirmation-URL" id="Confirmation-URL" VALUE="<asp:Literal ID='ConfirmationURL' runat='server'/>" />
		<INPUT type=hidden name="putopts" id=putopts value="true">
		 <!--webbot bot="FileUpload" S-Label-Fields="TRUE" B-Reverse-Chronology="FALSE" S-Date-Format="%A, %B %d, %Y" S-Time-Format="%I:%M:%S %p" S-Form-Fields="file destination" S-Format="HTML/UL" B-Dynamic-Redirect="TRUE" B-Process-Metainfo="TRUE" startspan -->
		<input TYPE="hidden" NAME="VTI-GROUP" VALUE="0">
		<input TYPE="hidden" NAME="Cmd" VALUE="Save">
		<!--webbot bot="FileUpload" endspan -->
	</TD></TR>
	</Control>
	<wssuc:ButtonSection runat="server">
		<Template_Buttons>
			<INPUT id="btnOK" runat="server" Type="button" AccessKey="<%$Resources:wss,multipages_okbutton_accesskey%>" class="ms-ButtonHeightWidth" Value="<%$Resources:wss,multipages_okbutton_text%>" onclick="javascript:if (!VerifyCommentLength()) return false;" OnServerClick="OnSubmit" />
		</Template_Buttons>
	</wssuc:ButtonSection>
	</TABLE>
</asp:Content>
