<%@ Control Language="C#" AutoEventWireup="false" EnableViewState="false" %>
<%@ Assembly Name="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Assembly Name="System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Assembly Name="roxority_RollupZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %>
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

	public class DictComparer : IComparer<string> {

		public static readonly List<string> UiResources = new List<string> (ProductPage.GetProductResource ("_UiResources").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries));

		public int Compare (string x, string y) {
			if ((UiResources.Contains (x) || UiResources.Contains (y)) && !(UiResources.Contains (x) && UiResources.Contains (y))) {
				if (UiResources.Contains (x))
					return "a".CompareTo ("b");
				else
					return "b".CompareTo ("a");
			}
			return x.CompareTo (y);
		}

	}

	internal readonly SPFarm farm = ProductPage.GetFarm (ProductPage.GetContext ());

	private CultureInfo culture = null;
	private SortedDictionary<string, string> resources = null;
	private List<CultureInfo> cultures = new List<CultureInfo> ();
	private string siteLanguage = null;

	public int GetTotalCount (string tab) {
		int tc = 0;
		string tmp;
		ResourceSet resSet = null;
		if (tab == SelectedTab) {
			if (ResKeys != null)
				foreach (string s in ResKeys)
					break;
			tc = ((resources == null) ? 0 : resources.Count);
		} else {
			if (tab == "Templates")
				resSet = new ResXResourceSet (Path.Combine (new DirectoryInfo (Server.MapPath ("/_layouts/roxority_RollupZen")).Parent.Parent.Parent.FullName, @"resources\roxority_RollupZen.resx"));
			else if (tab == "Studio")
				resSet = ProductPage.Resources.GetResourceSet (CultureInfo.CurrentCulture, false, true);
			else if (tab == "Runtime")
				resSet = ProductPage.ProductResources.GetResourceSet (CultureInfo.CurrentCulture, false, true);
			if (resSet != null)
				try {
					foreach (DictionaryEntry entry in resSet)
						if (!((tmp = entry.Key + string.Empty).StartsWith ("_", StringComparison.InvariantCultureIgnoreCase) || tmp.StartsWith ("Lic", StringComparison.InvariantCultureIgnoreCase) || tmp.StartsWith ("HelpTopic_", StringComparison.InvariantCultureIgnoreCase)))
							tc++;
				} catch {
				}
		}
		return tc;
	}

	public List<CultureInfo> Cultures {
		get {
			if (cultures.Count == 0)
				lock (cultures) {
					foreach (CultureInfo culture in ProductPage.AllCultures)
						if ((!string.IsNullOrEmpty (culture.Name)) && (!culture.Name.Contains ("-")) /*&& (culture.Name != "de") && (culture.Name != "en") && (culture.Name != "fr")*/)
							cultures.Add (culture);
					cultures.Sort (delegate (CultureInfo one, CultureInfo two) {
						int i1 = ProductPage.Loc (LocKey, one), i2 = ProductPage.Loc (LocKey, two), r = i2.CompareTo (i1);
						return ((r == 0) ? one.EnglishName.CompareTo (two.EnglishName) : r);
					});
				}
			return cultures;
		}
	}

	public string this [string name, params object [] args] {
		get {
			return ProductPage.GetResource (name, args);
		}
	}

	public string LocKey {
		get {
			return ((SelectedTab == "Studio") ? "roxority_Shared" : ("roxority_RollupZen_" + SelectedTab));
		}
	}

	public ProductPage ProdPage {
		get {
			return Page as ProductPage;
		}
	}

	public IEnumerable<string> ResKeys {
		get {
			bool disp = false;
			string tmp;
			CultureInfo culture = ProductPage.GetFarmCulture (ProductPage.GetContext ());
			ResourceSet resSet = null;
			if (culture == null)
				culture = CultureInfo.CurrentCulture;
			if (resources == null) {
				resources = new SortedDictionary<string, string> (new DictComparer ());
				if (disp = (SelectedTab == "Templates"))
					resSet = new ResXResourceSet (Path.Combine (new DirectoryInfo (Server.MapPath ("/_layouts/roxority_RollupZen")).Parent.Parent.Parent.FullName, @"resources\roxority_RollupZen" + ((SiteLanguage == "de") ? ".de.resx" : ((SiteLanguage == "fr") ? ".resx" : ".resx"))));
				else if (SelectedTab == "Studio")
					resSet = ProductPage.Resources.GetResourceSet (culture, true, true);
				else if (SelectedTab == "Runtime")
					resSet = ProductPage.ProductResources.GetResourceSet (culture, true, true);
				if (resSet != null)
					try {
						foreach (DictionaryEntry entry in resSet)
							if (!((tmp = entry.Key + string.Empty).StartsWith ("_", StringComparison.InvariantCultureIgnoreCase) || tmp.StartsWith ("Lic", StringComparison.InvariantCultureIgnoreCase) || tmp.StartsWith ("HelpTopic_", StringComparison.InvariantCultureIgnoreCase) || tmp.StartsWith ("Data_", StringComparison.InvariantCultureIgnoreCase) || tmp.StartsWith ("DataSourceProvider_", StringComparison.InvariantCultureIgnoreCase) || tmp.StartsWith ("DataTool_", StringComparison.InvariantCultureIgnoreCase) || tmp.StartsWith ("Tool_DataSources_", StringComparison.InvariantCultureIgnoreCase) || tmp.StartsWith ("CfgSetting")))
								resources [tmp] = entry.Value + string.Empty;
					} finally {
						if (disp)
							resSet.Dispose ();
					}
			}
			if (resources != null)
				foreach (KeyValuePair<string, string> kvp in resources)
					yield return kvp.Key;
		}
	}

	public CultureInfo SelectedCulture {
		get {
			if ((culture == null) && !string.IsNullOrEmpty (SelectedLanguage))
				try {
					culture = new CultureInfo (SelectedLanguage);
				} catch {
				}
			return culture;
		}
	}

	public string SelectedLanguage {
		get {
			string selLang = string.Empty;
			foreach (CultureInfo lang in Cultures)
				if (lang.Name.Equals (Request ["lang"], StringComparison.InvariantCultureIgnoreCase)) {
					selLang = lang.Name;
					break;
				}
			return selLang;
		}
	}

	public string SelectedTab {
		get {
			string defTab = null, selTab = null;
			foreach (string tab in Tabs) {
				if (defTab == null)
					defTab = tab;
				if (tab.Equals (Request ["tab"], StringComparison.InvariantCultureIgnoreCase))
					selTab = tab;
			}
			return (string.IsNullOrEmpty (selTab) ? defTab : selTab);
		}
	}

	public string SiteLanguage {
		get {
			CultureInfo fc;
			if (siteLanguage == null) {
				siteLanguage = string.Empty;
				if ((fc = ProductPage.GetFarmCulture (ProductPage.GetContext ())) != null) {
					if ((fc.Parent != null) && (fc.Parent.LCID != 127))
						fc = fc.Parent;
					siteLanguage = fc.Name;
				}
				if ((siteLanguage != "de") && (siteLanguage != "fr"))
					siteLanguage = "en";
			}
			return siteLanguage;
		}
	}

	public IEnumerable<string> Tabs {
		get {
			yield return "Runtime";
			yield return "Templates";
			yield return "Studio";
		}
	}

</script>
<script type="text/javascript" language="JavaScript">
var roxHasChanges = false, roxGoog = false, locLengths = {}, locs = {};

function roxAddLoc(id) {
<%
	if (ProdPage.IsAdminSite) {
%>
	jQuery('#div_' + id).css({ 'display': 'block' });
	jQuery('#clearlink_' + id).css({ 'display': 'inline' });
	jQuery('#googlink_' + id).css({ 'display': 'inline' });
	jQuery('#addlink_' + id).css({ 'display': 'none' });
<%
	} else {
%>
	alert('<%= this ["Tool_NoAdmin"]%> <%= this ["Tool_NoAdminLink"]%>');
<%
	}
%>
}

function roxClearLoc(id) {
<%
	if (ProdPage.IsAdminSite) {
%>
	locs[id] = '';
	jQuery('#localize_' + id).html('');
	jQuery('#div_' + id).css({ 'display': 'none' });
	jQuery('#clearlink_' + id).css({ 'display': 'none' });
	jQuery('#googlink_' + id).css({ 'display': 'none' });
	jQuery('#addlink_' + id).css({ 'display': 'inline' });
	if (locLengths[id])
		roxHasChanged();
<%
	} else {
%>
	alert('<%= this ["Tool_NoAdmin"]%> <%= this ["Tool_NoAdminLink"]%>');
<%
	}
%>
}

function roxGoogLoc(id) {
<%
	if (ProdPage.IsAdminSite) {
%>
	if (!roxGoog)
		alert('<%= this ["Tool_Localizer_GoogNone"]%>');
	else {
		jQuery('#googlink_' + id)[0].disabled = true;
		google.language.translate(jQuery('#pre_' + id).text(), '<%= SiteLanguage%>', '<%= SelectedLanguage%>', function(result) {
			jQuery('#googlink_' + id)[0].disabled = false;
			if (result.error)
				alert(result.error['message'] ? result.error.message : JSON.stringify(result.error));
			else {
				jQuery('#localize_' + id).text(locs[id] = result.translation);
				roxHasChanged();
			}
		});
	}
<%
	} else {
%>
	alert('<%= this ["Tool_NoAdmin"]%> <%= this ["Tool_NoAdminLink"]%>');
<%
	}
%>
}

function roxHasChanged() {
	if (!roxHasChanges) {
		roxHasChanges = true;
		jQuery('#roxlocchanges').show();
		window.onbeforeunload = roxLocUnload;
	}
}

function roxLocClearUnload() {
	window.onbeforeunload = null;
	if (window.detachEvent)
		window.detachEvent('onbeforeunload', roxLocUnload);
}

function roxLocUnload() {
	return '<%= this ["Tool_Localizer_Prompt", ProdPage.ProductName]%>';
}

function roxLocSubmit() {
	jQuery('#roxLocAllVals').val(JSON.stringify(locs));
	roxLocClearUnload();
	setTimeout("jQuery('#aspnetForm').submit();", 100);
}

jQuery(document).ready(function() {
	jQuery('#aspnetForm').submit(roxLocClearUnload);
});
</script>
<%
	bool hasLoc;
	string curLoc;
%>
<div style="display: none;">
	<asp:TextBox runat="server" ID="roxLocAllVals" style="width: 99%;" />
</div>
<div style="position: fixed; top: 136px; left: 707px; width: 175px; text-align: center; background-color: InfoBackground; color: InfoText; border: 1px solid #909090; padding: 0px 8px 0px 8px; display: <%= (ProdPage.IsAdminSite ? "none" : "block")%>; <%= ProdPage.IsAdminSite ? string.Empty : "font-size: 11px; line-height: 1.3em; padding: 4px"%>" id="roxlocchanges">
	<%= (this [ProdPage.IsAdminSite ? "Tool_Localizer_Unsaved" : "Tool_NoAdmin"])%>
	<%
		if (ProdPage.IsAdminSite) {
	%>
	<a href="#noop" onclick="roxLocSubmit();" style="padding-left: 20px; background: url('/_layouts/images/save.gif') no-repeat left center; white-space: nowrap;"><b><%= this["Tool_Localizer_SaveNow"]%></b></a>
	<%
		} else {
	%>
	<a href="<%= ProductPage.MergeUrlPaths (ProdPage.AdminSite.Url, "_layouts/roxority_RollupZen/" + ProdPage.GetLink (null))%>"><b><%= this ["Tool_NoAdminLink"]%></b></a>
	<%
		}
	%>
</div>
<div style="padding: 8px; margin: 16px; border-bottom: 1px dotted #c4c4c4;">
	<div>
		<%= this ["Tool_Localizer_Select"]%>
		<select onchange="location.replace('<%= ProdPage.GetLink (null, "tab", SelectedTab)%>&lang=' + this.options[this.selectedIndex].value);">
			<option value=""></option>
			<%
				foreach (CultureInfo ci in Cultures) {
			%>
			<option value="<%= ci.Name%>"<%= ((SelectedLanguage == ci.Name) ? " selected=\"selected\"" : string.Empty)%>>
				[<%= ProductPage.Loc (string.Empty, ci)%>] <%= (ci.DisplayName.Equals (ci.EnglishName, StringComparison.InvariantCultureIgnoreCase) ? ci.DisplayName : (ci.EnglishName + " / " + ci.DisplayName))%><%= ((ci.NativeName.Equals (ci.DisplayName, StringComparison.InvariantCultureIgnoreCase) || ci.NativeName.Equals (ci.EnglishName, StringComparison.InvariantCultureIgnoreCase)) ? string.Empty : (" / " + ci.NativeName))%>
			</option>
			<%
				}
			%>
		</select>
	</div>
	<div class="roxsubinfo">
		<%= this ["Tool_Localizer_SelectHint"]%>
	</div>
</div>
<table cellpadding="0" cellspacing="0">
	<tr>
		<%
			foreach (string tab in Tabs) {
		%>
		<td valign="top" style="padding: 8px; <%= ((tab == SelectedTab) ? "background: #f0f0f0; border-left: 1px solid #c0c0c0; border-right: 1px solid #c0c0c0;" : string.Empty)%>">
			<<%= ((tab == SelectedTab) ? "b" : "a")%> class="roxcfghead" href="<%= ProdPage.GetLink (null, "tab", tab, "lang", SelectedLanguage)%>">
				<%= Server.HtmlEncode (this ["Tool_Localizer_" + tab, ProdPage.ProductName])%> <span style="font-weight: normal; font-size: 13px;">[<%= ProductPage.Loc ((tab == "Studio") ? "roxority_Shared" : (ProductPage.AssemblyName + "_" + tab), SelectedCulture)%>/<%= GetTotalCount (tab)%>]</span></<%= ((tab == SelectedTab) ? "b" : "a")%>>
			<div class="roxsub" style="line-height: 1.1em;">
				<%= Server.HtmlEncode (this ["Tool_Localizer_" + tab + "_Desc", ProdPage.ProductName])%>
			</div>
		</td>
		<%
			}
		%>
	</tr>
	<%
		if (SelectedCulture == null) {
	%>
	<tr>
		<td colspan="3" style="padding: 8px; background: #f0f0f0; border-left: 1px solid #c0c0c0; border-right: 1px solid #c0c0c0; border-bottom: 1px solid #c0c0c0;">
			<p><b><%= this ["Tool_Localizer_StartInfo"]%></b></p>
			<p><%= this ["Tool_Localizer_StartInfoHint", ProdPage.GetLink ("cfg")]%></p>
		</td>
	</tr>
	<%
		} else {
	%>
	<tr>
		<td colspan="3" style="padding: 8px; background: #f0f0f0; border-left: 1px solid #c0c0c0; border-right: 1px solid #c0c0c0; border-bottom: 1px solid #c0c0c0;">
			<p><b><%= this ["Tool_Localizer_Info"]%></b><br /><%= ((DictComparer.UiResources.Count == 0) ? string.Empty : this ["Tool_Localizer_Info2"])%></p>
			<%
				if (SelectedTab == "Templates") {
			%>
			<p style="font-size: 11px; line-height: 1.3em;"><%= this ["Tool_Localizer_Info3"]%></p>
			<%
				}
			%>
			<p class="roxsub" style="line-height: 1.3em;"><%= this ["Tool_Localizer_InfoHint", ProdPage.ProductName]%></p>
		</td>
	</tr>
	<%
			foreach (string key in ResKeys) {
				hasLoc = !string.IsNullOrEmpty (curLoc = (ProductPage.Loc (LocKey, key, SelectedCulture) + string.Empty).Trim ());
	%>
	<tr>
		<td colspan="3" class="rox-tabcell">
			<b style="display: block; padding-left: 20px; background: url('/_layouts/roxority_RollupZen/img/flag/<%= SiteLanguage%>.png') no-repeat left center;"><%= (DictComparer.UiResources.Contains (key) ? "[UI] " : string.Empty) + Server.HtmlEncode (key)%>:</b>
			<pre id="pre_<%= key%>" style="padding: 0px 4px; margin-top: 8px; margin-bottom: 8px;">
				<%= Server.HtmlEncode (resources [key])%>
			</pre>
			<b style="display: block; padding-left: 20px; background: url('/_layouts/roxority_RollupZen/img/flag/<%= SelectedLanguage%>.png') no-repeat left center;"><%= SelectedCulture.DisplayName%>: <a id="addlink_<%= key%>" href="#noop" onclick="roxAddLoc('<%= key%>');" style="display: <%= (hasLoc ? "none" : "inline")%>;"><%= this ["Tool_Localizer_Add"]%></a><a id="clearlink_<%= key%>" href="#noop" onclick="roxClearLoc('<%= key%>');" style="display: <%= (hasLoc ? "inline" : "none")%>;"><%= this ["Tool_Localizer_Clear"]%></a>&nbsp;&nbsp;<a id="googlink_<%= key%>" href="#noop" onclick="roxGoogLoc('<%= key%>');" style="font-weight: normal; display: <%= (hasLoc ? "inline" : "none")%>;">Google Translate</a></b>
			<div id="div_<%= key%>" style="display: <%= (hasLoc ? "block" : "none")%>;">
				<textarea onchange="locs['<%= key%>']=jQuery(this).text();roxHasChanged();" id="localize_<%= key%>" style="margin-left: 20px; width: 600px; height: 40px;"><%= Server.HtmlEncode (curLoc)%></textarea>
			</div>
			<script type="text/javascript" language="javascript">
				locLengths['<%= key%>'] = <%= curLoc.Length%>;
				locs['<%= key%>'] = '<%= SPEncode.ScriptEncode (curLoc)%>';
			</script>
		</td>
	</tr>
	<%
			}
		}
	%>
</table>
<script type="text/javascript" language="JavaScript" src="<%= Request.IsSecureConnection ? "https" : "http"%>"://www.google.com/jsapi">
</script>
<script type="text/javascript" language="JavaScript">
try {
	google.load('language', '1');
	roxGoog = true;
} catch(e) {
}
</script>
