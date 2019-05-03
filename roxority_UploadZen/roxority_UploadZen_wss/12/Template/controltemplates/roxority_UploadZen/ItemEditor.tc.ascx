<%@ Control Language="C#" AutoEventWireup="false" EnableViewState="false" %>
<%@ Assembly Name="Microsoft.SharePoint, Version=12.0.0.0, Culture=neutral, PublicKeyToken=71e9bce111e9429c" %>
<%@ Assembly Name="System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" %>
<%@ Assembly Name="roxority_UploadZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01" %>
<%@ Import Namespace="roxority.Data" %>
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

	private JsonSchemaManager farmSchema = null, jsonSchema = null;	
	private string asmName = null, filePath = null, schema = string.Empty;
	private Guid saveGuid = Guid.NewGuid ();
	private bool saved = false;

	public override void Dispose () {
		if ((jsonSchema != farmSchema) && (jsonSchema != null))
			jsonSchema.Dispose ();
		if (farmSchema != null)
			farmSchema.Dispose ();
		base.Dispose ();
	}

	public IEnumerable<IDictionary> GetInstances (bool prependNew) {
		object def;
		IDictionary od;
		if (prependNew) {
			od = new OrderedDictionary ();
			od ["id"] = "new";
			foreach (JsonSchemaManager.Property prop in Schema.Properties)
				if ((def = JsonSchemaManager.GetDisplayValue (prop.DefaultValue)) != null)
					od [prop.Name] = def;
			od ["name"] = ProductPage.GetResource ("Tool_ItemEditor_NewName", ProductPage.GetProductResource ("Tool_" + SchemaName + "_TitleSingular"));
			if ((SchemaName != "DataSources") && (SchemaName != "DataFieldFormats"))
				od ["desc"] = ProductPage.GetResource ("Tool_ItemEditor_NewDesc", ProductPage.GetProductResource ("Tool_" + SchemaName + "_TitleSingular"));
			yield return od;
		}
		if (Schema.Instances != null)
			foreach (IDictionary dic in Schema.Instances)
				yield return dic;
	}

	public string AsmName {
		get {
			return asmName;
		}
		set {
			asmName = value;
		}
	}

	public JsonSchemaManager.Schema FarmSchema {
		get {
			return FarmSchemaManager.AllSchemas [SchemaName];
		}
	}

	public JsonSchemaManager FarmSchemaManager {
		get {
			if (farmSchema == null)
				farmSchema = new JsonSchemaManager (ProdPage, PhysicalFilePath, false, asmName);
			return farmSchema;
		}
	}

	public string FilePath {
		get {
			return filePath;
		}
		set {
			filePath = value;
		}
	}

	public string this [IDictionary inst] {
		get {
			return JsonSchemaManager.GetDisplayName (inst, SchemaName, true);
		}
	}

	public string this [string name, params object [] args] {
		get {
			return ProductPage.GetProductResource (name, args);
		}
	}

	public string PhysicalFilePath {
		get {
			string fp = FilePath;
			if ((fp != null) && fp.StartsWith ("/", StringComparison.InvariantCultureIgnoreCase))
				fp = Server.MapPath (fp);
			return fp;
		}
	}

	public ProductPage ProdPage {
		get {
			return Page as ProductPage;
		}
	}

	public JsonSchemaManager.Schema Schema {
		get {
			return SchemaManager.AllSchemas [SchemaName];
		}
	}

	public JsonSchemaManager SchemaManager {
		get {
			if (jsonSchema == null)
				if (ProdPage.IsAdminSite)
					jsonSchema = FarmSchemaManager;
				else
					jsonSchema = new JsonSchemaManager (ProdPage, PhysicalFilePath, true, asmName);
			return jsonSchema;
		}
	}

	public string SchemaName {
		get {
			return schema;
		}
		set {
			jsonSchema = null;
			schema = value;
		}
	}

</script>
<%
	string cssPath = ProductPage.Is14 ? "Themable/layouts.css" : "core.css";
	List<int> lcidsDone = new List<int> (new int [] { 1033 });
	SPContext ctx = SPContext.Current;
	DataSource theDs = null;
	Exception theErr = null;
%>
<link rel="stylesheet" type="text/css" href="/_layouts/1033/styles/<%= cssPath%>"/>
<%
	foreach (int lcid in ProductPage.WssInstalledCultures)
		if (!lcidsDone.Contains (lcid)) {
			lcidsDone.Add (lcid);
%>
<link rel="stylesheet" type="text/css" href="/_layouts/<%= lcid%>/styles/<%= cssPath%>"/>
<%
		}
	try {
		if ((ctx.Web.RegionalSettings != null) && !lcidsDone.Contains ((int) ctx.Web.RegionalSettings.LocaleId)) {
			lcidsDone.Add ((int) ctx.Web.RegionalSettings.LocaleId);
%>
<link rel="stylesheet" type="text/css" href="/_layouts/<%= ctx.Web.RegionalSettings.LocaleId%>/styles/<%= cssPath%>"/>
<%
		} else if ((ctx.Web.Locale != null) && !lcidsDone.Contains (ctx.Web.Locale.LCID)) {
			lcidsDone.Add (ctx.Web.Locale.LCID);
%>
<link rel="stylesheet" type="text/css" href="/_layouts/<%= ctx.Web.Locale.LCID%>/styles/<%= cssPath%>"/>
<%
		}
	} catch {
	}
%>
<style type="text/css">
table.rox-rollupitems td table.ms-summarystandardbody td.ms-vb2
{
vertical-align: top;
padding: 4px 7px 4px 2px;
}
</style>
<script language="JavaScript" type="text/javascript">
	var roxDefTab = '', roxAllTabs = [], roxSelTabs = {}, roxHasChanges = false, roxShowIfs = {}, roxDataTypePrefixes = {}, roxItemIDs = [], roxCurItemID = 'default', roxEditBoxes = {};

<%
	foreach (Type t in DataSource.KnownProviderTypes)
		if ((theDs = DataSource.GetStatic (t, null, ref theErr)) != null) {
%>
	roxDataTypePrefixes['<%= t.Name%>'] = '<%= theDs.SchemaPropNamePrefix%>';
<%
		}
%>

	function roxAutoHideProps(itemID) {
		var allTrue, vals, visCount;
		for (var p in roxShowIfs) {
			allTrue = true;
			for (var sp in roxShowIfs[p]) {
				vals = jQuery.isArray(roxShowIfs[p][sp]) ? roxShowIfs[p][sp] : [roxShowIfs[p][sp]];
				if (jQuery.inArray(roxGetCtlVal(itemID, sp), vals) < 0) {
					allTrue = false;
					break;
				}
			}
			jQuery('#roxinstprop_' + itemID + '_' + p + '.rox-iteminst-edit-' + roxSelTabs[itemID])[(allTrue ? 'show' : 'hide')]();
		}
		setTimeout(roxAutoHideTabs, 50);
	}

	function roxAutoHideTabs() {
		var pos, tp, tt;
		for (var i = 0; i < roxAllTabs.length; i++)
			if ((pos = roxAllTabs[i].indexOf('__')) > 0) {
				tp = roxAllTabs[i].substr(0, pos);
				tt = roxAllTabs[i].substr(pos + 2);
				for (var j = 0; j < roxItemIDs.length; j++)
					jQuery('a#roxiteminsttab_' + roxItemIDs[j] + '_' + roxAllTabs[i]).css({ "display": ((tt == roxGetCtlVal(roxItemIDs[j], tp)) ? "inline-block" : "none") });
			}
	}

	function roxGetCtlVal(itemID, sp) {
		var curVal = jQuery('#' + itemID + '_' + sp).val(), theSel;
		if (((!curVal) || !curVal.length) && (theSel = document.getElementById(itemID + '_' + sp)))
			if (theSel['options'])
				curVal = theSel.options[theSel.selectedIndex].value;
			else if (theSel['value'])
				curVal = theSel.value;
			else
				curVal = jQuery(theSel).html() || theSel.innerHTML;
		return curVal;
	}

	function roxGetDynInst(dsid, noEnc) {
		var pn, pos, prefix = roxDataTypePrefixes[roxGetCtlVal(dsid, 't')], dyn = {};
		<%
			foreach (JsonSchemaManager.Property prop in Schema.Properties)
				if (prop.PropertyType.IsBool && !JsonSchemaManager.Property.Type.String.IsPassword (prop.RawSchema)) {
		%>
		if (((pos = (pn = '<%= prop.Name%>').indexOf('_')) <= 0) || (pn.substr(0, pos) == prefix))
			dyn['<%= prop.Name%>'] = (jQuery('#roxiteminstchk_' + dsid + '_<%= prop.Name%>').attr('checked') ? true : false);
		<%
				} else if (prop.Name != "p") {
		%>
		if ((((pos = (pn = '<%= prop.Name%>').indexOf('_')) <= 0) || (pn.substr(0, pos) == prefix)) && ((val = roxGetCtlVal(dsid, '<%= prop.Name%>')) !== undefined))
			dyn['<%= prop.Name%>'] = val;
		<%
				}
		%>
		return noEnc ? dyn : roxSlimEncode(JSON.stringify(dyn));
	}

	function roxGetFieldSel(ctlID) {
		var sel = document.getElementById(ctlID), selIndex = sel.selectedIndex, opt = sel.options[selIndex];
		return (((selIndex > 0) && (selIndex < (sel.options.length - 1))) ? opt.value : '');
	}

	function roxItemAction(action, itemID) {
		if ((action == 'roxitemdelete') && !confirm('<%= this ["Tool_ItemEditor_DelPrompt", this ["Tool_" + SchemaName + "_TitleSingular"]]%>'))
			return;
		jQuery('#roxitemhasnew').val('');
		jQuery('#' + action).val(itemID);
		roxItemSubmit();
	}

	function roxItemClearUnload() {
		window.onbeforeunload = null;
		if (window.detachEvent)
			window.detachEvent('onbeforeunload', roxItemUnload);
	}

	function roxItemUnload() {
		return '<%= this ["Tool_ItemEditor_Prompt", this ["Tool_" + SchemaName + "_Title"]]%>';
	}

	function roxItemSubmit() {
		jQuery('#roxitemsave').val('<%= saveGuid%>');
		roxItemClearUnload();
		setTimeout("jQuery('#aspnetForm').submit();", 10);
	}

	function roxHasChanged() {
		if (!roxHasChanges) {
			roxHasChanges = true;
			jQuery('#roxitemunsavedchanges').css({ 'visibility': 'visible' });
			window.onbeforeunload = roxItemUnload;
		}
		setTimeout(roxAutoHideTabs, 25);
		if (roxCurItemID)
			setTimeout(function() { roxAutoHideProps(roxCurItemID); }, 50);
	}

	function roxRefreshFieldList(ctlID) {
		var val, pn, pos, dsid = ctlID.substr(0, ctlID.indexOf('_')), prefix = roxDataTypePrefixes[roxGetCtlVal(dsid, 't')], $sel = jQuery('select.rox-iteminst-fieldsel-' + dsid), url = '<%= SPContext.Current.Web.Url%>/_layouts/roxority_UploadZen/mash.tl.aspx?op=rf&ss=<%= (Schema.Owner.SiteScope ? 1 : 0)%>&dsid=' + dsid, onFinish = function() {
			jQuery('.rox-iteminst-fieldsel-' + dsid).attr('disabled', false).removeAttr('disabled');
		};
		$sel.each(function(index, selElem) {
			selElem.selectedIndex = 0;
		});
		jQuery('.rox-iteminst-fieldsel-' + dsid).attr('disabled', 'disabled');
		<%
			foreach (JsonSchemaManager.Property prop in Schema.Properties)
				if (prop.PropertyType.IsBool) {
			%>
		if (((pos = (pn = '<%= prop.Name%>').indexOf('_')) <= 0) || (pn.substr(0, pos) == prefix))
			url += ('&<%= prop.Name%>=' + (jQuery('#roxiteminstchk_' + dsid + '_<%= prop.Name%>').attr('checked') ? 1 : 0));
			<%
				} else {
			%>
		if ((((pos = (pn = '<%= prop.Name%>').indexOf('_')) <= 0) || (pn.substr(0, pos) == prefix)) && ((val = roxGetCtlVal(dsid, '<%= prop.Name%>')) !== undefined)) {
			//alert(roxSlimEncode(val));
			url += ('&<%= prop.Name%>=' + roxSlimEncode(val));
		}
			<%
				}
		%>
		url += ('&rr=' + Math.random());
		if (url.length > 2083)
			url = (url = url.substr(0, 2083)).substr(0, url.lastIndexOf('&'));
		jQuery.ajax({ 'url': url, success: function(data, status) {
			if (!data)
				alert('Unexpected empty server response -- please contact ROXORITY technical support!\nStatus: ' + status + '\nURL: ' + url);
			else if (data['___roxerr'])
				alert(data['___roxerr']);
			else
				$sel.each(function(index, sel) {
					var small = jQuery(sel).hasClass('rox-iteminst-fieldsel-small'), opt = jQuery(sel.options[0]), html = '<option value="' + opt.val() + '">' + opt.html() + '</option>', isSmall;
					for (var p in data)
						html += ('<option value="' + p + '">' + ((isSmall = (small || (p == data[p]))) ? '' : '&quot;') + data[p] + (isSmall ? '' : '&quot;') + (isSmall ? '' : (' &mdash; [' + p + ']')) + '</option>');
					opt = jQuery(sel.options[sel.options.length - 1]);
					html += ('<option value="' + opt.val() + '">' + opt.html() + '</option>');
					jQuery(sel).html(html);
				});
			onFinish();
		}, error: function(xhr, status, err) {
			var msg = 'Status: ' + status + '\nError: ' + err, tmp;
			onFinish();
			if (xhr)
				for (var p in xhr)
					if (xhr[p] && (typeof(xhr[p]) != 'function') && (tmp = xhr[p] + '') && (tmp.indexOf('\n') < 0))
						msg += ('\nxhr.' + p + ': ' + tmp);
			url += '\n\nURL: ' + url;
			if (confirm(msg + '\n\nRetry?'))
				roxRefreshFieldList(ctlID);
		}, processData: false, global: false, dataType: 'json' });
	}

	function roxShowItemEditor(itemID, noChange) {
		roxCurItemID = itemID;
		if (itemID == 'new') {
			<%
				if (Schema.CanAddNew) {
			%>
			jQuery('#roxitemhasnew').val('1');
			jQuery('#roxitemnewinst').css({ 'visibility': 'hidden' });
			jQuery('#roxiteminst_new').show();
			if (!noChange)
				roxHasChanged();
			<%
				} else {
			%>
			alert('<%= SPEncode.ScriptEncode (this ["NoAdmin"])%>\n<%= SPEncode.ScriptEncode (this ["NopeEd", this ["Tool_ItemEditor_Add", this ["Tool_" + SchemaName + "_TitleSingular"]], "Basic"])%>');
			return;
			<%
				}
			%>
		}
		jQuery('.rox-iteminst-edit-box').slideUp();
		jQuery('.rox-iteminst-row').slideDown();
		if (itemID) {
			jQuery('#roxiteminstrow_' + itemID).slideUp();
			jQuery('#roxiteminsteditbox_' + itemID).slideDown();
			roxSwitchTab(itemID, roxDefTab);
		}
	}

	function roxSwitchTab(itemID, tabID) {
		roxSelTabs[itemID] = tabID;
		jQuery('.rox-iteminst-edit-tab').removeClass('rox-iteminst-edit-tab-active');
		jQuery('.rox-iteminst-edit-prop').slideUp();
		jQuery('#roxiteminsteditbox_' + itemID + ' .rox-iteminst-edit-' + tabID).slideDown(100);
		setTimeout(function() { jQuery('#roxiteminsttab_' + itemID + '_' + tabID).addClass('rox-iteminst-edit-tab-active')[0].blur(); }, 25);
		setTimeout(function() { roxAutoHideProps(itemID); }, 120);
	}

	function roxToggleItemHelp(itemID, propName) {
		jQuery('#roxiteminstdesc_' + itemID + '_' + propName).toggle();
	}

	function roxToggleListPicker(itemID, propName) {
	}

	function roxToggleListSel(itemID, propName) {
		var inner = jQuery('#roxlistsetinner_' + itemID + '_' + propName), outer = jQuery('#roxlistsetouter_' + itemID + '_' + propName);
		if (inner.css('display') == 'none') {
			inner.show();
			outer.addClass('rox-iteminst-edit-ListSet-top');
		} else {
			inner.hide();
			outer.removeClass('rox-iteminst-edit-ListSet-top');
		}
	}

	function roxValidateJson(itemID, propName, defVal) {
		var $el = jQuery('#' + itemID + '_' + propName), val = jQuery.trim($el.val()), obj, ret, msg, isEmpty = true;
		if (!defVal)
			defVal = '';
		if (!val)
			ret = '';
		else
			try {
				ret = ((obj = JSON.parse(val)) ? val : '');
				if (obj)
					if (!jQuery.isArray(obj))
						for (var p in obj) {
							isEmpty = false;
							break;
						}
					else
						isEmpty = (obj.length == 0);
				if (isEmpty)
					ret = '';
			} catch(err) {
				msg = (err['message'] ? err['message'] : err) + '';
				if (confirm('<%= this ["Tool_ItemEditor_ValidateJSON1"]%>\n\n' + msg + '\n\n<%= this ["Tool_ItemEditor_ValidateJSON2"]%>'))
					ret = null;
				else
					ret = defVal;
			}
		return ret;
	}

	function roxValidateNonEmpty(itemID, propName, defVal) {
		var $el = jQuery('#' + itemID + '_' + propName), val = jQuery.trim($el.val());
		return val ? val : defVal;
	}

	function roxValidateNumeric(itemID, propName, defVal) {
		var $el = jQuery('#' + itemID + '_' + propName), val = $el.val(), num = parseFloat(val);
		return isNaN(num) ? defVal : num;
	}

	jQuery(document).ready(function() {
<%
	if (saved = Schema.Saved) {
%>
		//if (!<%= Schema.SavedSilent.ToString ().ToLowerInvariant ()%>)
		//	alert('<%= this ["Tool_ItemEditor_Saved"]%>');
		location.replace(location.href.replace('r=<%= Request ["r"]%>', 'r=<%= ProdPage.Rnd.Next ()%>'));
<%
	} else if (Schema.SaveError != null) {
%>
		alert('<%= SPEncode.ScriptEncode (Schema.SaveError.Message)%>');
<%
	}
%>
		jQuery('#aspnetForm').submit(roxItemClearUnload);
	});
</script>
<span style="display: <%= saved ? "none" : "inline"%>;">
<div class="roxsub" style="color: #303030;">
	<%= ProductPage.GetResource ("Tool_ItemEditor_Info" + (ProdPage.IsAdminSite ? "_Farm" : string.Empty), this ["Tool_" + SchemaName + "_Title"], Context.Items ["roxsitetitle"], FarmSchema.InstanceCount, ProdPage.AdminSite.Url)%>
</div>
<div class="rox-itemtoolbar">
	<a class="rox-itemtoolbar" href="#noop<%= ProdPage.Rnd.Next ()%>" onclick="roxShowItemEditor('new');" id="roxitemnewinst" style="float: left; background-image: url('<%= this ["Tool_" + SchemaName + "_Icon"]%>');"><%= this ["Tool_ItemEditor_Add", this ["Tool_" + SchemaName + "_TitleSingular"]]%></a>
	<b id="roxitemnewlabel" style="background-image: url('<%= this ["Tool_" + SchemaName + "_Icon"]%>'); display: none;"><%= this ["Tool_ItemEditor_Add", this ["Tool_" + SchemaName + "_TitleSingular"]]%>:</b>
	<span class="rox-itemtoolbar" style="float: right; display: inline-block;"><span id="roxitemunsavedchanges" style="visibility: hidden;"><%= this ["Tool_ItemEditor_Unsaved"]%></span>&nbsp;<a onclick="<%= (ProdPage.IsApplicableAdmin ? "roxItemSubmit();" : ("alert('" + this ["NoAdmin" + (ProdPage.IsAdminSite ? "Farm" : string.Empty)] + "');"))%>" href="#noop<%= ProdPage.Rnd.Next ()%>" style="background-image: url('/_layouts/images/saveitem.gif');"> <%= this ["Tool_ItemEditor_Save"]%></a></span>
	&nbsp;
	<input type="hidden" name="roxitemhasnew" id="roxitemhasnew" value=""/>
	<input type="hidden" name="roxitemdelete" id="roxitemdelete" value=""/>
	<input type="hidden" name="roxitemmoveup" id="roxitemmoveup" value=""/>
	<input type="hidden" name="roxitemmovedn" id="roxitemmovedn" value=""/>
	<input type="hidden" name="roxitemsave" id="roxitemsave" value=""/>
	<input type="hidden" name="roxitemsaveschema" id="roxitemsaveschema" value="<%= SchemaName%>"/>
</div>
<%
	string tmp, lastTab = string.Empty, firstID = string.Empty;
	bool isBool;
	object val;
	int instCount = 0, index = 0;
	foreach (JsonSchemaManager.Property prop in Schema.Properties)
		if (prop.RawSchema.Contains ("show_if")) {
%>
<script type="text/javascript" language="javascript">
	roxShowIfs['<%= prop.Name%>'] = <%= JSON.JsonEncode (prop.RawSchema ["show_if"])%>;
</script>
<%
		}
	foreach (IDictionary inst in GetInstances (true)) {
		if (string.IsNullOrEmpty (firstID) && !"new".Equals (inst ["id"]))
			firstID = inst ["id"] + string.Empty;
		instCount++;
%>
<script type="text/javascript" language="JavaScript">
	roxItemIDs.push('<%= inst ["id"]%>');
</script>
<div class="rox-iteminst" id="roxiteminst_<%= inst ["id"]%>" style="display: <%= ("new".Equals (inst ["id"]) ? "none" : "block")%>;">
	<div class="rox-iteminst-row" id="roxiteminstrow_<%= inst ["id"]%>">
		<a href="#noop<%= ProdPage.Rnd.Next ()%>" onclick="roxShowItemEditor('<%= inst ["id"]%>');" class="rox-iteminst-title"><%= this [inst]%></a>
		<div class="rox-iteminst-desc">
			<%
				if (ProdPage.IsApplicableAdmin) {
			%>
			<span>
				<a href="#noop<%= ProdPage.Rnd.Next ()%>" onclick="roxItemAction('roxitemmovedn', '<%= inst ["id"]%>');" style="<%= (((index >= Schema.InstanceCount) || "new".Equals (inst ["id"])) ? "visibility: hidden;" : string.Empty) %>"><img alt="<%= this ["Tool_ItemEditor_ActionDn"]%>" title="<%= this ["Tool_ItemEditor_ActionDn"]%>" src="/_layouts/images/arrdowni.gif" /></a>
				<a href="#noop<%= ProdPage.Rnd.Next ()%>" onclick="roxItemAction('roxitemmoveup', '<%= inst ["id"]%>');" style="<%= (((index <= 1) || "new".Equals (inst ["id"])) ? "visibility: hidden;" : string.Empty) %>"><img alt="<%= this ["Tool_ItemEditor_ActionUp"]%>" title="<%= this ["Tool_ItemEditor_ActionUp"]%>" src="/_layouts/images/arrupi.gif" /></a>
				<a href="#noop<%= ProdPage.Rnd.Next ()%>" onclick="roxItemAction('roxitemdelete', '<%= inst ["id"]%>');" style="<%= (("default".Equals (inst ["id"])) ? "visibility: hidden;" : string.Empty) %>"><img alt="<%= this ["Tool_ItemEditor_ActionDel"]%>" title="<%= this ["Tool_ItemEditor_ActionDel"]%>" src="/_layouts/images/delete.gif" /></a>
			</span>
			<%
				}
			%>
			<%= Schema.GetInstanceDescription (inst)%>
		</div>
		<div class="rox-iteminst-more">
		<%
			foreach (JsonSchemaManager.Property prop in Schema.Properties)
				if (prop.ShowInSummary && (!("title".Equals (prop.Name) || "name".Equals (prop.Name) || "desc".Equals (prop.Name))) && ((val = inst [prop.Name]) != null) && !string.IsNullOrEmpty (tmp = prop.PropertyType.ToString (prop, val))) {
			%>
			<span style="color: #c0c0c0;">&nbsp;&nbsp;&mdash;&nbsp;&nbsp;</span><span class="rox-iteminst-more-prop"><%= tmp%></span>
			<%
				}
		%>
		</div>
	</div>
	<div class="rox-iteminst-edit-box" style="display: none;" id="roxiteminsteditbox_<%= inst ["id"]%>">
		<div class="rox-iteminst-edit-head">
		<%
			if (Schema.PropTabs.Count > 0)
				foreach (KeyValuePair<string, string> tab in Schema.PropTabs) {
			%>
			<script language="JavaScript" type="text/javascript">
				if (!roxDefTab)
					roxDefTab = '<%= tab.Key%>';
				if (!roxSelTabs['<%= inst ["id"]%>'])
					roxSelTabs['<%= inst ["id"]%>'] = '<%= tab.Key%>';
				if (jQuery.inArray('<%= tab.Key%>', roxAllTabs) < 0)
					roxAllTabs.push('<%= tab.Key%>');
			</script>
			<a class="rox-iteminst-edit-tab" id="roxiteminsttab_<%= inst ["id"]%>_<%= tab.Key%>" href="#noop<%= ProdPage.Rnd.Next ()%>" onclick="if(roxSelTabs['<%= inst ["id"]%>']!='<%= tab.Key%>'){roxSwitchTab('<%= inst ["id"]%>', '<%= tab.Key%>');}" class="rox-iteminst-edit-tab"><%= tab.Value%></a>
			<%
				}
		%>
			<a class="rox-iteminst-edit-close" href="#noop<%= ProdPage.Rnd.Next ()%>" onclick="roxShowItemEditor();" title="<%= this ["Tool_ItemEditor_Close"]%>">&times;</a>
			&nbsp;
		</div>
		<%
			lastTab = string.Empty;
			foreach (JsonSchemaManager.Property prop in Schema.Properties) {
				isBool = (prop.PropertyType.IsBool);
		%>
		<div class="rox-iteminst-edit-prop rox-iteminst-edit-<%= prop.Tab%>" id="roxinstprop_<%= inst["id"]%>_<%= prop.Name%>" style="display: none; <%= (lastTab != prop.Tab) ? "padding-top: 12px;" : string.Empty%>" onmouseover="jQuery('#roxiteminsthelp_<%= inst ["id"]%>_<%= prop.Name%>').css({ 'visibility': 'visible' });" onmouseout="jQuery('#roxiteminsthelp_<%= inst ["id"]%>_<%= prop.Name%>').css({ 'visibility': 'hidden' });">
			<span class="rox-iteminst-edit-prop-name">
				<span style="display: inline-block; float: left;">
					<input onclick="roxHasChanged();" type="checkbox" id="roxiteminstchk_<%= inst ["id"]%>_<%= prop.Name%>" name="roxiteminstchk_<%= inst ["id"]%>_<%= prop.Name%>" style="visibility: <%= (isBool ? "visible" : "hidden")%>;" <%= ((isBool && (inst [prop.Name] is bool) && ((bool) inst [prop.Name])) ? " checked=\"checked\"" : string.Empty)%> <%= (prop.Editable ? "" : "disabled=\"disabled\"")%>/>
					<label for="roxiteminstchk_<%= inst ["id"]%>_<%= prop.Name%>"<%= prop.Editable ? string.Empty : " disabled=\"disabled\""%>><%= prop.ToString () + ((isBool || (prop.Name == "webs")) ? string.Empty : ":")%></label>
				</span>
				<a class="rox-iteminst-edit-prop-help" style="visibility: hidden; display: <%= prop.AlwaysShowHelp ? "none" : "inline-block;"%>;" id="roxiteminsthelp_<%= inst ["id"]%>_<%= prop.Name%>" href="#noop" onclick="roxToggleItemHelp('<%= inst ["id"]%>', '<%= prop.Name%>');"><img src="/_layouts/images/hhelp.gif"/></a>
				&nbsp;
			</span>
			<%
				if (!string.IsNullOrEmpty (tmp = prop.PropertyType.RenderValueForEdit (prop, inst, !prop.Editable, !prop.Editable))) {
			%>
			<span class="rox-iteminst-edit-prop-val"><%= tmp%></span>
			<%
				}
			%>
			<span style="display: <%= prop.AlwaysShowHelp ? "block" : "none"%>;" class="rox-iteminst-edit-prop-desc" id="roxiteminstdesc_<%= inst ["id"]%>_<%= prop.Name%>">
				<%= prop.Description%>
				<%
					if ((!prop.Editable) && ProdPage.IsApplicableAdmin) {
				%>
				<br /><br />
				<b><%= prop.EditHint%></b>
				<%
					}
				%>
			</span>
		</div>
		<%
				lastTab = prop.Tab;
			}
		%>
		<div class="rox-iteminst-edit-buttons">
			<span style="display: inline-block; float: left; padding-top: 4px;"><%= this ["Tool_ItemEditor_Optional"]%></span>
			<span style="display: inline-block; float: right;">
				<input onclick="roxItemSubmit();" type="button" value="<%= this ["Tool_ItemEditor_Save"]%>" <%= (ProdPage.IsApplicableAdmin ? string.Empty : "disabled=\"disabled\"")%>/>
				&nbsp;
				<input type="button" value="<%= this ["Tool_ItemEditor_Close"]%>" onclick="roxShowItemEditor();" />
			</span>
			&nbsp;
		</div>
	</div>
</div>
<script language="JavaScript" type="text/javascript">
	roxSwitchTab('<%= inst ["id"]%>', roxSelTabs['<%= inst ["id"]%>']);
</script>
<%
		index++;
	}

	if (!saved)
		if (instCount <= 1) {
%>
<script language="JavaScript" type="text/javascript">
jQuery('#roxitemnewinst').hide();
jQuery('#roxitemnewlabel').show();
jQuery('#roxitemunsavedchanges').css({ "visibility": "visible" });
roxShowItemEditor('new', true);
</script>
<%
		} else if ((instCount == 2) && !string.IsNullOrEmpty (firstID)) {
%>
<script language="JavaScript" type="text/javascript">
roxShowItemEditor('<%= firstID%>');
</script>
<%
		}
%>
</span>
