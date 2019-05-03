<%@ Assembly Name="roxority_UploadZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %> <%@ Control Language="C#" Debug="false" CompilationMode="Auto" AutoEventWireup="false" %>
<%
	string url = roxority_UploadZen.UploadZenMenuItem.ReplaceUrl (Request.RawUrl);
	if (!url.Equals (Request.RawUrl, StringComparison.InvariantCultureIgnoreCase))
		Response.Redirect (url, true);
%>
