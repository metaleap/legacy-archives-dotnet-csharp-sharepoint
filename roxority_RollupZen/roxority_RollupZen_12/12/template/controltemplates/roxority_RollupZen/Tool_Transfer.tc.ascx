<%@ Control Language="C#" AutoEventWireup="false" EnableViewState="false" %>
<%@ Assembly Name="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Assembly Name="System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Assembly Name="roxority_RollupZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %>
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

	internal ResXResourceSet resSet = null;

	private List<Dictionary<string, string>> configSettings = null;

	public string Html (string val) {
		return Server.HtmlEncode (val).Replace ("\r\n", "<br/>").Replace ("\r", "<br/>").Replace ("\n", "<br/>");
	}

	public override void Dispose () {
		if (resSet != null) {
			resSet.Dispose ();
			resSet = null;
		}
		base.Dispose ();
	}

	public string GetName (string name, bool isCfg, bool pure) {
		int pos;
		string prefix;
		if (isCfg)
			name = (pure ? this ["CfgSettingTitle_" + name] : Server.HtmlEncode (this ["CfgSettingTitle_" + name]));
		else {
			if ((pos = name.IndexOf ("__")) > 0) {
				name = (pure ? name.Substring (pos + 2) : ("<span style=\"font-size: 11px; color: #c0c0c0;\">[" + Server.HtmlEncode (this ["Tool_Localizer_" + (((prefix = name.Substring (0, pos)) == "roxority_Shared") ? "Studio" : prefix.Replace (ProductPage.AssemblyName + "_", string.Empty)), string.Empty].Substring ((prefix == "roxority_Shared") ? 0 : 1)) + "]</span><br/>&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;" + Server.HtmlEncode (name.Substring (pos + 2))));
				if ((pos = name.IndexOf ("__")) > 0)
					name = name.Substring (0, pos);
			}
		}
		return name;
	}

	public string GetRes (string name) {
		string res = this [name], path, basePath;
		CultureInfo farmCulture;
		if (string.IsNullOrEmpty (res)) {
			if ((resSet == null) && ((farmCulture = ProductPage.GetFarmCulture (SPContext.Current)) != null))
				resSet = new ResXResourceSet (((!File.Exists (path = (basePath = Path.Combine (new DirectoryInfo (Server.MapPath ("/_layouts")).Parent.Parent.FullName, "Resources\\" + ProductPage.AssemblyName)) + "." + farmCulture.Name + ".resx")) && (farmCulture.Parent != null) && (farmCulture.Parent.LCID != 127) && !File.Exists (path = basePath + "." + farmCulture.Parent.Name + ".resx")) ? (basePath + ".resx") : path);
			res = resSet.GetString (name);
		}
		return res;
	}

	public List<Dictionary<string, string>> ConfigSettings {
		get {
			if (configSettings == null)
				configSettings = new List<Dictionary<string, string>> (ProdPage.ConfigSettings);
			return configSettings;
		}
	}

	public string this [string name, params object [] args] {
		get {
			return ProductPage.GetProductResource (name, args);
		}
	}

	public ProductPage ProdPage {
		get {
			return Page as ProductPage;
		}
	}

</script>
<script type="text/javascript" language="JavaScript">
	var roxImps = {};
	function roxRefreshImps() {
		var hasChecked = false;
		jQuery('input:checkbox').each(function(i, checkbox) {
			if (roxImps[checkbox.id] = checkbox.checked)
				hasChecked = true;
		});
		jQuery('#JsonTextBox').text(JSON.stringify(roxImps));
		document.getElementById('importButton').disabled = !hasChecked;
	}
</script>
<%
	string siteTitle, key, val, cfgKey, cfgVal, tmp;
	bool isCfgFarm = false, isCfgSite = false, isLoc = false, isFjs = false, isSjs = false;
	int maxLen = 64, pos;
	CultureInfo loc = null;
	Exception error = null;
	Hashtable import = null, temp, temp2, fjss = new Hashtable (), sjss = new Hashtable (), jsht;
	JsonSchemaManager jsm = null;
	JsonSchemaManager.Schema schema;
	Converter<KeyValuePair<string, bool>, JsonSchemaManager> getSchemaMan = delegate (KeyValuePair<string, bool> what) {
		KeyValuePair<JsonSchemaManager, JsonSchemaManager> kvp;
		if (!fjss.ContainsKey (what.Key)) {
			kvp = JsonSchemaManager.TryGet (ProdPage, Context.Server.MapPath ("/_layouts/" + ProductPage.AssemblyName + "/" + what.Key), true, true);
			fjss [what.Key] = kvp.Key;
			sjss [what.Key] = kvp.Value;
		} else
			kvp = new KeyValuePair<JsonSchemaManager, JsonSchemaManager> (fjss [what.Key] as JsonSchemaManager, sjss [what.Key] as JsonSchemaManager);
		return what.Value ? kvp.Key : kvp.Value;
	};
	if ((Request.Files != null) && (Request.Files.Count > 0))
		try {
			using (StreamReader sr = new StreamReader (Request.Files [0].InputStream, true))
				import = JSON.JsonDecode (sr.ReadToEnd ()) as Hashtable;
			if ((import == null) || (import.Count == 0)) {
				import = null;
				throw new Exception (this ["Tool_Transfer_ImportError"]);
			}
		} catch (Exception ex) {
			error = ex;
		}
	if (!string.IsNullOrEmpty (Request ["JsonTextBox"]))
		try {
			if ((temp2 = JSON.JsonDecode (Request ["JsonTextBox"]) as Hashtable) == null)
				throw new Exception ("JSON_PARSING_ERROR");
			import = new Hashtable ();
			foreach (DictionaryEntry entry in temp2)
				if ((bool) entry.Value) {
					if ((temp = import [key = (key = entry.Key + string.Empty).Substring (0, pos = key.IndexOf ('-'))] as Hashtable) == null)
						import [key] = temp = new Hashtable ();
					temp [(key = entry.Key + string.Empty).Substring (pos + 1, key.IndexOf ('-', pos + 1) - (pos + 1))] = key.Substring (key.IndexOf ('-', pos + 1) + 1);
				}
		} catch (Exception ex) {
			error = ex;
		}
	if (error != null) {
%>
<div style="background-image: url(/_layouts/images/exclaim.gif)" class="rox-info"><b><%= Server.HtmlEncode (error.Message)%></b></div>
<%
	}
	if ((!string.IsNullOrEmpty (Request ["JsonTextBox"])) && (import != null)) {
	%>
	<br />
	<h3><%= this ["Tool_Transfer_Results"]%></h3>
	<table class="roxtable">
	<%
		foreach (DictionaryEntry entry in import) {
			isCfgFarm = "farm".Equals (entry.Key);
			isCfgSite = "site".Equals (entry.Key);
			isFjs = "fjs".Equals (entry.Key);
			isSjs = "sjs".Equals (entry.Key);
			isLoc = !(isCfgSite || isCfgFarm || isFjs || isSjs);
			foreach (DictionaryEntry e2 in ((Hashtable) entry.Value))
				try {
		%>
		<tr><td><b><%= ((isFjs || isCfgFarm) ? "[Farm] " : ((isSjs || isCfgSite) ? "[Site] " : string.Empty)) + e2.Key%></b></td>
		<%
			if (!ProductPage.LicEdition (SPContext.Current, (IDictionary) null, ProductPage.IsWhiteLabel ? 0 : 4))
				throw new Exception (this ["NopeEd", this ["Tool_Transfer_Title"], "Ultimate"]);
			else if (isLoc)
				ProductPage.Loc (e2.Key + string.Empty, null, null, e2.Value + string.Empty);
			else if (isCfgFarm || isCfgSite)
				ProductPage.Config<string> (SPContext.Current, e2.Key + string.Empty, e2.Value + string.Empty, isCfgSite);
			else if ((isFjs || isSjs) && ((pos = (tmp = e2.Key + string.Empty).IndexOf (':')) > 0) && ((jsm = getSchemaMan (new KeyValuePair<string, bool> (tmp.Substring (0, pos), isFjs))) != null) && jsm.AllSchemas.TryGetValue (tmp.Substring (pos + 1), out schema))
				schema.Import (e2.Value + string.Empty);
		%>
		<td><%= this ["Tool_Transfer_Success"]%></td>
		<%
		} catch (Exception ex) {
		%>
		<td><%= Server.HtmlEncode (ex.Message)%></td>
		<%
		} finally {
		%>
		</tr>
		<%
		}
		}
	%>
	</table>
	<%
	} else if (import == null) {
		try {
			SPContext.Current.Site.CatchAccessDeniedException = false;
			siteTitle = SPContext.Current.Site.RootWeb.Title;
		} catch {
			siteTitle = SPContext.Current.Site.Url;
		}
%>
<a href="?r=<%= ProdPage.Rnd.Next ()%>&file=<%= ProductPage.AssemblyName%>.export.rox" style="display: block; padding: 16px 0px 0px 8px; font-size: 20px;"><b><%= this ["Tool_Transfer_Export"]%></b></a>
<ul>
	<li><%= this ["Tool_Transfer_Export1", ProdPage.GetLink (null, "tool", "Tool_Localizer")]%></li>
	<li><%= this ["Tool_Transfer_Export2", ProdPage.GetLink ("cfg"), ProductPage.GetProductResource ("Tool_Transfer_Export_Extra")]%></li>
	<li><%= this ["Tool_Transfer_Export3", Server.HtmlEncode (siteTitle), ProductPage.GetProductResource ("Tool_Transfer_Export_Extra")]%></li>
	<li><%= this ["Tool_Transfer_Export4"]%></li>
</ul>
<h3><%= this ["Tool_Transfer_Import"]%></h3>
<%= this ["Tool_Transfer_Import1", ProdPage.ProductName]%>
<br />
<asp:FileUpload ID="FileUpload1" runat="server" />
<%= this ["Tool_Transfer_Import2", ProdPage.IsApplicableAdmin ? string.Empty : "disabled=\"disabled\""]%>
<%
	} else {
		temp = new Hashtable ();
%>
<br />
<h3><%= this ["Tool_Transfer_Import3"]%></h3>
<%= this ["Tool_Transfer_Import4"]%>
<table class="roxtable">
	<tr>
		<th style="white-space: nowrap;"><a href="#noop" onclick="jQuery('input:checkbox').attr('checked',function(i,a){return(!this.disabled);});roxRefreshImps();"><img alt="<%= this ["Tool_Transfer_CheckAll"]%>" title="<%= this ["Tool_Transfer_CheckAll"]%>" src="/_layouts/images/checkall.gif" /></a> <a href="#noop" onclick="jQuery('input:checkbox').attr('checked', false);roxRefreshImps();"><img alt="<%= this ["Tool_Transfer_UncheckAll"]%>" title="<%= this ["Tool_Transfer_UncheckAll"]%>" src="/_layouts/images/unchecka.gif" /></a> <%= this ["Tool_Transfer_ImpHead1"]%></th>
		<th style="white-space: nowrap; text-align: center;"><%= this ["Tool_Transfer_ImpHead2"]%></th>
		<th style="white-space: nowrap; text-align: right;"><%= this ["Tool_Transfer_ImpHead3"]%></th>
	</tr>
	<%
	foreach (DictionaryEntry ht in import) {
		isCfgFarm = "farm".Equals (ht.Key);
		isCfgSite = "site".Equals (ht.Key);
		isFjs = "fjs".Equals (ht.Key);
		isSjs = "sjs".Equals (ht.Key);
		if (isCfgSite || isCfgFarm || isFjs || isSjs)
			loc = null;
		else
			try {
				loc = new CultureInfo (ht.Key + string.Empty);
			} catch {
				loc = null;
			}
		if ((isLoc = (loc != null)) || isCfgSite || isCfgFarm || isFjs || isSjs) {
		%>
		<tr class="roxgrouprow">
			<td colspan="3" class="roxcfggroup" style="cursor: default; background-image: none; font-size: 18px;"><b><%= (isCfgFarm ? this ["Tool_Transfer_CfgFarm"] : (isCfgSite ? this ["Tool_Transfer_CfgSite"] : (isFjs ? this ["Tool_Transfer_FarmSchema"] : (isSjs ? this ["Tool_Transfer_SiteSchema"] : loc.DisplayName))))%></b></td>
		</tr>
		<%
	foreach (DictionaryEntry entry in ((Hashtable) ht.Value)) {
		key = entry.Key + string.Empty;
		val = entry.Value + string.Empty;
		jsht = entry.Value as Hashtable;
		cfgKey = ht.Key + "-" + key + "-" + ((isFjs || isSjs) ? JSON.JsonEncode (jsht) : val);
		%>
		<tr>
		<%
	if (isLoc && !string.IsNullOrEmpty (cfgVal = GetRes (GetName (key, false, true)))) {
		temp [cfgKey] = false;
		%>
			<td style="white-space: nowrap;"><input onclick="roxRefreshImps();" id="<%= HttpUtility.HtmlAttributeEncode (cfgKey)%>" title="<%= key%>" type="checkbox"<%= ((ProdPage.IsAdminSite || isCfgSite) ? string.Empty : " disabled=\"disabled\"")%>/> <%= GetName (key, isCfgFarm || isCfgSite, false)%></td>
			<td><%= ((cfgVal.Length < maxLen) ? Server.HtmlEncode (cfgVal) : (Server.HtmlEncode (cfgVal.Substring (0, maxLen)) + "&hellip; <a href=\"#noop\" onclick=\"alert('" + SPEncode.ScriptEncode (cfgVal) + "');\">" + this ["Tool_Transfer_More"] + "</a>"))%>&nbsp;</td>
			<td><%= (string.IsNullOrEmpty (val) ? ("<i style=\"font-size: 11px; color: #c0c0c0;\">" + this ["Tool_Transfer_Fallback1"] + "</i>") : ((val.Length < maxLen) ? Server.HtmlEncode (val) : (Server.HtmlEncode (val.Substring (0, maxLen)) + "&hellip; <a href=\"#noop\" onclick=\"alert('" + SPEncode.ScriptEncode (val) + "');\">" + this ["Tool_Transfer_More"] + "</a>")))%>&nbsp;</td>
		<%
	} else if ((!isLoc) && ConfigSettings.Exists (delegate (Dictionary<string, string> dict) {
		return (dict ["name"] == key);
	})) {
		temp [cfgKey] = false;
		cfgVal = ProductPage.Config (SPContext.Current, (isCfgFarm ? "farm:" : "site:") + key);
		%>
			<td><input onclick="roxRefreshImps();" id="<%= HttpUtility.HtmlAttributeEncode (cfgKey)%>" title="<%= key%>" type="checkbox"<%= ((ProdPage.IsAdminSite || isCfgSite) ? string.Empty : " disabled=\"disabled\"")%>/> <%= GetName (key, isCfgFarm || isCfgSite, false)%></td>
			<td><%= (string.IsNullOrEmpty (cfgVal) ? ("<i style=\"font-size: 11px; color: #c0c0c0;\">" + this ["Tool_Transfer_Fallback2"] + "</i>") : Html (cfgVal))%>&nbsp;</td>
			<td><%= (string.IsNullOrEmpty (val) ? ("<i style=\"font-size: 11px; color: #c0c0c0;\">" + this ["Tool_Transfer_Fallback3"] + "</i>") : Html (val))%>&nbsp;</td>
		<%
			} else if ((isFjs || isSjs) && (jsht != null) && (jsht.Count > 0) && ((pos = key.IndexOf (':')) > 0) && ((jsm = getSchemaMan (new KeyValuePair<string, bool> (key.Substring (0, pos), isFjs))) != null) && jsm.AllSchemas.TryGetValue (key.Substring (pos + 1), out schema)) {
				temp [cfgKey] = false;
	%>
			<td><input onclick="roxRefreshImps();" id="<%= HttpUtility.HtmlAttributeEncode (cfgKey)%>" title="<%= key%>" type="checkbox"<%= (((ProdPage.IsAdminSite && isFjs) || (isSjs && !ProdPage.IsAdminSite)) ? string.Empty : " disabled=\"disabled\"")%>/> <%= this ["Tool_" + schema.Name + "_Title"]%></td>
			<td style="text-align: center;"><%= this ["Tool_Transfer_Current", schema.InstanceCount]%></td>
			<td style="text-align: center;"><%= this ["Tool_Transfer_Importing", jsht.Count]%></td>
	<%
	}
		%>
		</tr>
		<%
	}
		}
	}
	%>
</table>
<script type="text/javascript" language="JavaScript">
	roxImps = <%= JSON.JsonEncode (temp)%>;
</script>
<asp:TextBox ID="JsonTextBox" runat="server" TextMode="MultiLine" style="width: 600px; height: 40px; display: none;" />
<%
	}
%>
