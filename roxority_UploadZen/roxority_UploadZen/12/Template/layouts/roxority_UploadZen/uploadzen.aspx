<%@ Assembly Name="roxority_UploadZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %><%@ Page ContentType="text/plain" Language="C#" ValidateRequest="false" EnableEventValidation="false" EnableViewState="false" EnableViewStateMac="false" %><%@ Import Namespace="roxority_UploadZen" %><%@ Import Namespace="Microsoft.SharePoint" %><%@ Import Namespace="Microsoft.SharePoint.Utilities" %><script runat="server">
	protected override void Render (HtmlTextWriter writer) {
		//System.Threading.Thread.Sleep (1000);
		string msg = Files.Upload ();
		writer.Write ("1".Equals (Request ["js"]) ? SPEncode.ScriptEncode (msg) : msg);
	}
</script>