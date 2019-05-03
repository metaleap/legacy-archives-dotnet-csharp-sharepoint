<%@ Control Language="C#" AutoEventWireup="false" EnableViewState="false" %>
<%@ Assembly Name="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Assembly Name="System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Assembly Name="roxority_PeopleZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %>
<%@ Import Namespace="roxority.Shared" %>
<%@ Import Namespace="roxority.SharePoint" %>
<%@ Import Namespace="Microsoft.SharePoint" %>
<%@ Import Namespace="Microsoft.SharePoint.Administration" %>
<%@ Import Namespace="Microsoft.SharePoint.Utilities" %>
<%@ Import Namespace="System.Collections.Generic" %>
<%@ Import Namespace="System.Globalization" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="System.Reflection" %>
<%@ Import Namespace="System.Resources" %>
<script runat="server">
	
	private Dictionary<string, SPUser> dict = null;

	public Dictionary<string, SPUser> Users {
		get {
			if (dict == null)
				dict = ProductPage.GetUsersDict (ProductPage.GetContext ());
			return dict;
		}
	}
	
</script>
<%= ProductPage.GetResource ("Tool_SiteUsers_Info", Users.Count, Context.Items ["roxsitetitle"])%>
<ul>
	<%
		foreach (KeyValuePair<string, SPUser> kvp in Users) {
	%>
	<li><a href="/_layouts/userdisp.aspx?Force=1&ID=<%= kvp.Value.ID%>&Source=<%= Server.UrlEncode (Request.Url.ToString ())%>"><%= Server.HtmlEncode (ProductPage.LoginName(kvp.Value.LoginName))%> &mdash; <%= Server.HtmlEncode (kvp.Value.Name)%></a></li>
	<%
		}
	%>
</ul>