
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.WebPartPages.Communication;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Serialization;

namespace roxority_FilterZen {

	public class FilterToolPart : ToolPart {

		public const string FORMAT_INFO_CONTROL = "<div class=\"rox-infobox\" id=\"{2}\" style=\"background-image: url(\'/_layouts/images/{1}\');\">{0}</div>";
		public const string FORMAT_PREFIX_CONTROL = "<div class=\"rox-usersection\"><table><tr><td><div class='rox-UserSectionTitle'>{0}</div></td></tr></table><div><table width=\"100%\"><tr><td><div class='rox-UserSectionHead'><label for='{2}'>{1}</label></div><div class='rox-UserSectionBody'><div class='rox-UserControlGroup'>";
		public const string FORMAT_SUFFIX_CONTROL = "</div></div><div class='rox-UserDottedLine'> </div></td></tr></table></div></div>";
		public const string FORMAT_TOOL_BUTTON = "<a id=\"{0}\" href=\"#noop\" onclick=\"{1}\" class=\"rox-toolbutton\" style=\"visibility: hidden;\"><img alt=\"{3}\" border=\"0\" src=\"/_layouts/images/{2}\" title=\"{3}\"/></a>";
		public const string MAPPING_SEPARATOR = "=>";

		internal TextBox acSecFieldsTextBox = new TextBox (), ajax14TextBox = new TextBox (), camlTextBox = new TextBox (), editorNameTextBox = new TextBox (), groupsTextBox = new TextBox (), htmlTextBox = new TextBox (), jsonTextBox = new TextBox (), maxTextBox = new TextBox (), wizardTextBox = new TextBox (), hiddenTextBox = new TextBox ();
		internal DropDownList disableDropDownList = new DropDownList (), dynDropDownList = new DropDownList (), filterDropDownList = new DropDownList (), folderDropDownList = new DropDownList (), htmlDropDownList = new DropDownList (), htmlTempDropDownList = new DropDownList (), multiDropDownList = new DropDownList (), nameDropDownList = new DropDownList (), wizardFieldDropDownList = new DropDownList (), wizardListDropDownList = new DropDownList (), jqueryDropDownList = new DropDownList ();
		internal RadioButton camlNoRadioButton = new RadioButton (), camlYesRadioButton = new RadioButton (), debugOnRadioButton = new RadioButton (), debugOffRadioButton = new RadioButton (), errorOnRadioButton = new RadioButton (), errorOffRadioButton = new RadioButton ();
		internal Button wizardButton = new Button ();
		internal Label wizardLabel = new Label ();
		internal HiddenField filterEditorHidden = new HiddenField (), wizardHidden = new HiddenField (), wizardHidden2 = new HiddenField ();
		internal ListBox filterListBox = new ListBox ();
		internal CheckBox ajax14CheckBox = new CheckBox (), ajax14FocusCheckBox = new CheckBox (), autoCheckBox = new CheckBox (), cascadedCheckBox = new CheckBox (), defaultOrCheckBox = new CheckBox (), editorEnabledCheckBox = new CheckBox (), highlightCheckBox = new CheckBox (), recollapseGroupsCheckBox = new CheckBox (), rememberCheckBox = new CheckBox (), respectCheckBox = new CheckBox (), searchCheckBox = new CheckBox (), sendOnChangeCheckBox = new CheckBox (), embedFiltersCheckBox = new CheckBox (), showClearCheckBox = new CheckBox (), suppressCheckBox = new CheckBox (), toolSpaceCheckBox = new CheckBox (), toolStyleCheckBox = new CheckBox (), urlParamCheckBox = new CheckBox (), urlSettingsCheckBox = new CheckBox ();
		internal Panel editorPanel = new Panel (), htmlPanel = new Panel ();

		private string bigAnnoyance = null, webUrl = null;
		private List<FilterBase> filters = null;
		private roxority_FilterWebPart webPart = null;
		private FilterBase editFilter = null;

		public FilterToolPart (string title) {
			Title = title;
			ChromeState = System.Web.UI.WebControls.WebParts.PartChromeState.Normal;
			Visible = true;
		}

		protected override void CreateChildControls () {
			base.CreateChildControls ();
			if ((WebPart != null) && WebPart.CanRun) {
				WebPart.EnsureChildControls2 ();
				wizardHidden.ID = "roxListWizardHidden";
				Controls.Add (wizardHidden);
				wizardHidden2.ID = "roxListWizardHidden2";
				Controls.Add (wizardHidden2);
				ProductPage.CreateLicControls (Controls, FORMAT_PREFIX_CONTROL.Replace ("UserSectionTitle\"", "UserSectionTitle\" style=\"font-weight: normal;\""), FORMAT_SUFFIX_CONTROL);
				Controls.Add (new LiteralControl ("<input type=\"hidden\" name=\"roxfilteraction\" id=\"roxfilteraction\" value=\"\"/>"));
				hiddenTextBox.CssClass = "ms-input";
				hiddenTextBox.TextMode = TextBoxMode.MultiLine;
				hiddenTextBox.Rows = 4;
				hiddenTextBox.Style [HtmlTextWriterStyle.Display] = "none";
				hiddenTextBox.Width = new Unit (200, UnitType.Pixel);
				hiddenTextBox.Wrap = true;
				Controls.Add (hiddenTextBox);
				Controls.Add (new LiteralControl ("<span id=\"roxfiltereditor\" style=\"display: none;\">"));
				Controls.Add (new LiteralControl (string.Format (FORMAT_PREFIX_CONTROL, "<span id=\"roxeditortitle\">Filter Editor</span>", this ["SectionDesc_FilterEditor"], null)));
				Controls.Add (filterEditorHidden);
				{
					Controls.Add (new LiteralControl ("<div class=\"rox-infobox\" id=\"roxListWizard\" style=\"display: none;\">" + this ["WizardInfo"] + "<fieldset><legend>" + this ["WizardTitle"] + "</legend>"));
					wizardTextBox.CssClass = "ms-input";
					wizardTextBox.Width = new Unit (85, UnitType.Percentage);
					Controls.Add (wizardTextBox);
					wizardButton.ID = "wizardButton";
					wizardButton.Style ["font-size"] = "xx-small";
					wizardButton.Text = this ["WizardGo"];
					wizardButton.UseSubmitBehavior = false;
					wizardButton.CausesValidation = false;
					wizardButton.Width = new Unit (15, UnitType.Percentage);
					Controls.Add (wizardButton);
					Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> var roxHiddenFieldID = '" + wizardHidden.ClientID + "', roxHidden2FieldID = '" + wizardHidden2.ClientID + "'; configInitPage('" + wizardButton.ClientID + "'); </script>"));
					wizardListDropDownList.AutoPostBack = true;
					wizardListDropDownList.ID = "wizardListDropDownList";
					wizardListDropDownList.Width = new Unit (98, UnitType.Percentage);
					Controls.Add (wizardListDropDownList);
					wizardFieldDropDownList.ID = "wizardFieldDropDownList";
					wizardFieldDropDownList.Width = new Unit (98, UnitType.Percentage);
					Controls.Add (wizardFieldDropDownList);
					wizardLabel.Style ["padding"] = "2px 2px 2px 20px";
					wizardLabel.Style ["display"] = "block";
					wizardLabel.Style ["background"] = "url('" + WebUrl + "/_layouts/images/forward.gif') 2px 2px no-repeat";
					wizardLabel.Text = this ["WizardLabel"];
					Controls.Add (wizardLabel);
					wizardFieldDropDownList.Attributes ["onchange"] = "jQuery('#" + wizardLabel.ClientID + "').html(this.options[this.selectedIndex].value ? '" + this ["WizardName"].Substring (0, this ["WizardName"].IndexOf ("<b>")).Replace ("\'", "\\\'") + "<b>' + this.options[this.selectedIndex].value + '</b>" + this ["WizardName"].Substring (this ["WizardName"].IndexOf ("</b>") + 4).Replace ("\'", "\\\'") + "' : (roxListViewUrl ? roxListViewUrl : '" + this ["WizardLabel"].Replace ("\'", "\\\'") + "'));";
					Controls.Add (new LiteralControl ("</fieldset>" + this ["WizardInfo2"] + "</div>"));
				}
				Controls.Add (new LiteralControl ("<div class=\"rox-prop rox-proplimited\"><span style=\"display: inline-block; float: left;\">" + this ["Edit_T_Name"] + ":</span>"));
				Controls.Add (new LiteralControl ("<a id=\"roxListWizardLink\" onclick=\"toggleListWizard();\" href=\"#noop\">" + this ["WizardLink"] + "</a></div><div class=\"rox-prop\">"));
				editorNameTextBox.CssClass = "ms-input rox-proplimited";
				editorNameTextBox.ID = "editorNameTextBox";
				editorNameTextBox.Style ["clear"] = "both";
				editorNameTextBox.Width = new Unit (98, UnitType.Percentage);
				Controls.Add (editorNameTextBox);
				nameDropDownList.ID = "nameDropDownList";
				nameDropDownList.Width = new Unit (96, UnitType.Percentage);
				nameDropDownList.Attributes ["onblur"] = "jQuery('#roxfilternames').hide();";
				nameDropDownList.Attributes ["onfocus"] = "setTimeout(\"jQuery('#roxfilternames').show();\", 50);setTimeout(\"document.getElementById('roxfilternames').focus();\", 100);";
				Controls.Add (new LiteralControl ("<div style=\"display: none;\" id=\"roxfilternames\">"));
				Controls.Add (nameDropDownList);
				Controls.Add (new LiteralControl ("</div><div class=\"rox-proplimited\">"));
				editorEnabledCheckBox.ID = "editorEnabledCheckBox";
				editorEnabledCheckBox.Text = this ["Edit_T_Enabled"];
				Controls.Add (editorEnabledCheckBox);
				Controls.Add (new LiteralControl ("</div>"));
				editorEnabledCheckBox.LabelAttributes ["id"] = "label_editorEnabledCheckBox";
				nameDropDownList.Attributes ["onchange"] = "if(this.selectedIndex>0){document.getElementById(nameTextBoxID).value=this.options[this.selectedIndex].value;document.getElementById(nameTextBoxID).focus();setTimeout(\"jQuery('#roxfilternames').hide();\", 50);this.selectedIndex=0;}";
				editorNameTextBox.Attributes ["onblur"] = "jQuery('#roxfilternames').hide();";
				editorNameTextBox.Attributes ["onfocus"] = "if(document.getElementById('" + nameDropDownList.ClientID + "').options.length>1)jQuery('#roxfilternames').show();";
				editorNameTextBox.Attributes ["onchange"] = editorNameTextBox.Attributes ["onkeyup"] = "roxUpdatePreview();";
				editorPanel.ID = "editorPanel";
				Controls.Add (editorPanel);
				Controls.Add (new LiteralControl ("</div>"));
				Controls.Add (new LiteralControl ("<div style=\"float: right; white-space: nowrap;\"><button style=\"background-image: url('" + WebUrl + "/_layouts/images/saveitem.gif');\" onclick=\"toolFilterAction('editsave');\" class=\"UserButton\" id=\"roxfiltappbtn\">" + this ["BtnApply"] + "</button><button style=\"background-image: url('" + WebUrl + "/_layouts/images/delete.gif');\" onclick=\"toolFilterAction('editstop');\" class=\"UserButton\" id=\"roxfiltdiscbtn\">" + this ["BtnDiscard"] + "</button></div><br style=\"clear: both;\"/>"));
				Controls.Add (new LiteralControl (FORMAT_SUFFIX_CONTROL));
				Controls.Add (new LiteralControl ("</span><span id=\"roxfilterlist\">"));
				Controls.Add (new LiteralControl (string.Format (FORMAT_PREFIX_CONTROL, this ["Section_Filters"], this ["SectionDesc_Filters"], null)));
				Controls.Add (new LiteralControl ("<div class=\"rox-toolbar\">"));
				filterDropDownList.Items.Clear ();
				Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\">\n"));
				Controls.Add (new LiteralControl ("roxFilterDescs[''] = '" + this ["FilterDesc_None"] + "';\n"));
				filterDropDownList.Items.Add (new ListItem (this ["FilterType_None"], string.Empty));
				foreach (Type t in FilterBase.FilterTypes) {
					filterDropDownList.Items.Add (new ListItem (FilterBase.GetFilterTypeTitle (t), t.AssemblyQualifiedName));
					filterDropDownList.Items [filterDropDownList.Items.Count - 1].Attributes ["style"] = "font-weight: bold;";
					Controls.Add (new LiteralControl ("roxFilterDescs['" + t.AssemblyQualifiedName + "'] = '" + SPEncode.ScriptEncode (FilterBase.GetFilterTypeDesc (t)) + "';\n"));
				}
				Controls.Add (new LiteralControl ("</script>\n"));
				filterDropDownList.Attributes ["onchange"] = "document.getElementById('roxFilterDesc').innerText=roxFilterDescs[this.options[this.selectedIndex].value];jQuery('#roxFilterAddButton').css({'visibility':((this.selectedIndex>0)?'visible':'hidden')});";
				filterDropDownList.CssClass = "ms-input";
				filterDropDownList.Style ["width"] = "180px";
				filterDropDownList.ID = "filterDropDownList";
				Controls.Add (filterDropDownList);
				Controls.Add (new LiteralControl (string.Format (FORMAT_TOOL_BUTTON, "roxFilterAddButton", "toolFilterAction('add');", "collapseplus.gif", this ["ToolButton_AddFilter"])));
				Controls.Add (new LiteralControl (string.Format (FORMAT_TOOL_BUTTON, "roxFilterUpButton", "toolFilterAction('up');", "arrupi.gif", this ["ToolButton_FilterUp"])));
				Controls.Add (new LiteralControl (string.Format (FORMAT_TOOL_BUTTON, "roxFilterDownButton", "toolFilterAction('down');", "arrdowni.gif", this ["ToolButton_FilterDown"])));
				Controls.Add (new LiteralControl (string.Format (FORMAT_TOOL_BUTTON, "roxFilterEditButton", "toolFilterAction('edit');", "edit.gif", this ["ToolButton_EditFilter"])));
				Controls.Add (new LiteralControl (string.Format (FORMAT_TOOL_BUTTON, "roxFilterRemoveButton", "toolFilterAction('remove', '" + this ["ToolPrompt_RemoveFilter"] + "');", "filteroff.gif", this ["ToolButton_RemoveFilter"])));
				Controls.Add (new LiteralControl ("</div>"));
				filterListBox.CssClass = "ms-input";
				filterListBox.Width = new Unit (98, UnitType.Percentage);
				filterListBox.ID = "filterListBox";
				Controls.Add (filterListBox);
				Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> wizardFieldDropDownListID = '" + wizardFieldDropDownList.ClientID + "'; nameTextBoxID = '" + editorNameTextBox.ClientID + "'; filterListBoxID = '" + filterListBox.ClientID + "'; </script>"));
				Controls.Add (new LiteralControl (string.Format (FORMAT_INFO_CONTROL, this ["FilterDesc_None"], "blank.gif", "roxFilterDesc") + FORMAT_SUFFIX_CONTROL));
				Controls.Add (new LiteralControl ("<span id=\"roxifilters\" style=\"display: none;\">"));
				Controls.Add (new LiteralControl (string.Format (FORMAT_PREFIX_CONTROL, this ["Section_Interactive"], this ["SectionDesc_Interactive"], "") + "<div><br/>"));
				toolStyleCheckBox.Checked = WebPart.ApplyToolbarStylings;
				toolStyleCheckBox.Text = this ["ApplyToolStylings"];
				Controls.Add (toolStyleCheckBox);
				Controls.Add (new LiteralControl ("</div><div style=\"display: " + (ProductPage.Is14 ? "none" : "block") + ";\">"));
				toolSpaceCheckBox.Checked = WebPart.SuppressSpacing;
				toolSpaceCheckBox.Text = this ["SuppressSpacing"];
				Controls.Add (toolSpaceCheckBox);
				Controls.Add (new LiteralControl ("</div>" + ProductPage.GetResource ("Css", ProductPage.AssemblyName) + "<br/><div>"));
				cascadedCheckBox.Checked = WebPart.Cascaded;
				cascadedCheckBox.Enabled = WebPart.LicEd (4);
				cascadedCheckBox.Text = this ["Cascaded"];
				Controls.Add (cascadedCheckBox);
				Controls.Add (new LiteralControl ("</div><div>"));
				rememberCheckBox.Checked = WebPart.RememberFilterValues;
				rememberCheckBox.Enabled = WebPart.LicEd (2);
				rememberCheckBox.Text = this ["RememberFilterValues"];
				Controls.Add (rememberCheckBox);
				Controls.Add (new LiteralControl ("</div><div>"));
				sendOnChangeCheckBox.Checked = WebPart.AutoRepost;
				sendOnChangeCheckBox.Text = this ["Prop_RepostOnChange"];
				Controls.Add (sendOnChangeCheckBox);
				if (ProductPage.Is14) {
					Controls.Add (new LiteralControl ("</div><div>"));
					embedFiltersCheckBox.Checked = WebPart.EmbedFilters;
					embedFiltersCheckBox.Text = this ["Prop_EmbedFilters"];
					Controls.Add (embedFiltersCheckBox);
				}
				Controls.Add (new LiteralControl ("</div><br/><div>" + this ["HtmlPre"] + "</div>"));
				for (int i = 0; i < 3; i++)
					htmlDropDownList.Items.Add (new ListItem (this ["Html" + i], i.ToString ()));
				htmlDropDownList.CssClass = "ms-input";
				htmlDropDownList.ID = "htmlDropDownList";
				htmlDropDownList.Width = new Unit (98, UnitType.Percentage);
				htmlDropDownList.SelectedIndex = WebPart.HtmlMode;
				Controls.Add (htmlDropDownList);
				Controls.Add (new LiteralControl (this ["Html"]));
				htmlTextBox.CssClass = "ms-input";
				htmlTextBox.ID = "htmlTextBox";
				htmlTextBox.TextMode = TextBoxMode.MultiLine;
				htmlTextBox.Rows = 6;
				htmlTextBox.Width = new Unit (98, UnitType.Percentage);
				htmlTextBox.Text = WebPart.HtmlEmbed;
				Controls.Add (htmlTextBox);
				Controls.Add (new LiteralControl (this ["HtmlTemp"] + " "));
				htmlTempDropDownList.CssClass = "ms-input";
				htmlTempDropDownList.Items.Add (new ListItem ("", ""));
				for (int i = 0; i < int.Parse (this ["HtmlTempCount"]); i++)
					htmlTempDropDownList.Items.Add (new ListItem (this ["HtmlTitle" + i], this ["HtmlTemp" + i]));
				Controls.Add (htmlTempDropDownList);
				Controls.Add (new LiteralControl ("<fieldset><legend>" + this ["Preview"] + "</legend>"));
				htmlPanel.CssClass = "ms-WPBody rox-preview";
				htmlPanel.ID = "htmlPanel";
				htmlPanel.Style [HtmlTextWriterStyle.TextAlign] = ((htmlDropDownList.SelectedIndex == 2) ? "right" : ((htmlDropDownList.SelectedIndex == 1) ? "left" : "center"));
				Controls.Add (htmlPanel);
				Controls.Add (new LiteralControl ("</fieldset><script type=\"text/javascript\" language=\"JavaScript\"> setInterval(function() { jQuery('#" + htmlPanel.ClientID + "').html(jQuery('#" + htmlTextBox.ClientID + "').text()); }, 1000); </script>"));
				htmlTempDropDownList.Attributes ["onchange"] = "document.getElementById('" + htmlTextBox.ClientID + "').innerText=this.options[this.selectedIndex].value;jQuery('#" + htmlPanel.ClientID + "').html(jQuery('#" + htmlTextBox.ClientID + "').text());";
				htmlDropDownList.Attributes ["onchange"] = "document.getElementById('" + htmlPanel.ClientID + "').style.textAlign=((this.selectedIndex==2)?'right':((this.selectedIndex==1)?'left':'center'));";
				Controls.Add (new LiteralControl ("<div><br/>"));
				showClearCheckBox.Text = this ["ShowClearButtons"];
				showClearCheckBox.Checked = (showClearCheckBox.Enabled = webPart.LicEd (4)) && WebPart.showClearButtons;
				Controls.Add (showClearCheckBox);
				Controls.Add (new LiteralControl ("</div><div><br/>" + this ["MaxFiltersPerRow"]));
				maxTextBox.CssClass = "ms-input";
				maxTextBox.Width = new Unit (98, UnitType.Percentage);
				maxTextBox.Text = WebPart.MaxFiltersPerRow.ToString ();
				Controls.Add (maxTextBox);
				Controls.Add (new LiteralControl ("</div><br/><div>"));
				highlightCheckBox.Text = this ["Highlight"];
				highlightCheckBox.Checked = (highlightCheckBox.Enabled = webPart.LicEd (4)) && WebPart.highlight;
				Controls.Add (highlightCheckBox);
				Controls.Add (new LiteralControl ("</div><div>"));
				searchCheckBox.Text = this ["SearchBehavior"];
				searchCheckBox.Checked = (searchCheckBox.Enabled = webPart.LicEd (4)) && WebPart.searchBehaviour;
				Controls.Add (searchCheckBox);
				Controls.Add (new LiteralControl ("</div><div>"));
				urlParamCheckBox.Text = this ["UrlParams"];
				urlParamCheckBox.Checked = (urlParamCheckBox.Enabled = webPart.LicEd (4)) && WebPart.urlParams;
				Controls.Add (urlParamCheckBox);
				Controls.Add (new LiteralControl ("</div>" + FORMAT_SUFFIX_CONTROL + "</span>"));
				Controls.Add (new LiteralControl (string.Format (FORMAT_PREFIX_CONTROL, this ["FilteringMode"], this ["CamlFilters"], "")));
				Controls.Add (new LiteralControl ("<br/><div>"));
				camlNoRadioButton.Enabled = camlYesRadioButton.Enabled = webPart.LicEd (4);
				camlNoRadioButton.GroupName = camlYesRadioButton.GroupName = "CamlFilters";
				camlNoRadioButton.Checked = true;
				if (camlYesRadioButton.Checked = WebPart.CamlFilters)
					camlNoRadioButton.Checked = false;
				camlYesRadioButton.Text = this ["CamlFiltersYes"];
				camlNoRadioButton.Text = this ["CamlFiltersNo"];
				Controls.Add (camlNoRadioButton);
				Controls.Add (new LiteralControl ("</div><br/><div>"));
				Controls.Add (camlYesRadioButton);
				Controls.Add (new LiteralControl ("</div><br/><div style=\"padding: 4px; background: InfoBackground; color: InfoText; border: 1px solid #c0c0c0;\">" + this ["CamlListView"] + "<br/><br/><div>" + this ["DisableFilters1"] + " "));
				disableDropDownList.ID = "disableDropDownList";
				Controls.Add (disableDropDownList);
				Controls.Add (new LiteralControl (" " + this ["DisableFilters2"] + "</div><div>"));
				respectCheckBox.Checked = WebPart.respectFilters;
				respectCheckBox.Text = this ["RespectFilters"];
				Controls.Add (respectCheckBox);
				Controls.Add (new LiteralControl ("</div><div>"));
				recollapseGroupsCheckBox.Checked = WebPart.recollapseGroups;
				recollapseGroupsCheckBox.Text = this ["RecollapseGroups"];
				Controls.Add (recollapseGroupsCheckBox);
				Controls.Add (new LiteralControl ("</div><div>"));
				defaultOrCheckBox.Checked = WebPart.defaultToOr;
				defaultOrCheckBox.Text = this ["DefaultToOr"];
				Controls.Add (defaultOrCheckBox);
				Controls.Add (new LiteralControl ("</div><div>" + this ["NoListFolders"] + "<br/>"));
				folderDropDownList.Items.Add (new ListItem (this ["FolderScope_"], string.Empty));
				foreach (object v in Enum.GetValues (typeof (SPViewScope)))
					folderDropDownList.Items.Add (new ListItem (this ["FolderScope_" + v], v + string.Empty));
				folderDropDownList.CssClass = "ms-input";
				folderDropDownList.ID = "folderDropDownList";
				folderDropDownList.Width = new Unit (98, UnitType.Percentage);
				if (folderDropDownList.Enabled = WebPart.LicEd (4))
					folderDropDownList.SelectedValue = WebPart.FolderScope;
				Controls.Add (folderDropDownList);
				Controls.Add (new LiteralControl ("</div><br/><div>" + this ["CamlAnd"]));
				camlTextBox.CssClass = "ms-input";
				camlTextBox.Enabled = WebPart.LicEd (4);
				camlTextBox.Text = WebPart.CamlFiltersAndCombined;
				camlTextBox.TextMode = TextBoxMode.MultiLine;
				camlTextBox.Wrap = false;
				camlTextBox.Width = new Unit (98, UnitType.Percentage);
				Controls.Add (camlTextBox);
				Controls.Add (new LiteralControl ("</div><br/><div>" + this ["JsonFilters"]));
				jsonTextBox.CssClass = "ms-input";
				jsonTextBox.Enabled = WebPart.LicEd (4);
				jsonTextBox.Text = WebPart.JsonFilters;
				jsonTextBox.TextMode = TextBoxMode.MultiLine;
				jsonTextBox.Wrap = false;
				jsonTextBox.Width = new Unit (98, UnitType.Percentage);
				Controls.Add (jsonTextBox);
				Controls.Add (new LiteralControl ("<br/></div></div>"));
				Controls.Add (new LiteralControl (FORMAT_SUFFIX_CONTROL));
				Controls.Add (new LiteralControl (string.Format (FORMAT_PREFIX_CONTROL, this ["Advanced"], this ["AdvancedDesc"], "")));
				if (ProductPage.Is14) {
					Controls.Add (new LiteralControl ("<div><br/>"));
					autoCheckBox.Text = this ["AutoConnect"];
					autoCheckBox.Checked = WebPart.AutoConnect;
					Controls.Add (autoCheckBox);
					Controls.Add (new LiteralControl ("<br/></div>"));
				}
				Controls.Add (new LiteralControl ("<div>"));
				suppressCheckBox.Checked = WebPart.SuppressUnknownFilters;
				suppressCheckBox.Enabled = WebPart.LicEd (4);
				suppressCheckBox.Text = this ["SuppressUnknownFilters"];
				Controls.Add (suppressCheckBox);
				Controls.Add (new LiteralControl ("</div><div style=\"display: none;\"><br/>"));
				if (this ["DynamicInteractiveFilters"].EndsWith ("{0}"))
					Controls.Add (new LiteralControl (this ["DynamicInteractiveFilters"].Substring (0, this ["DynamicInteractiveFilters"].Length - 3)));
				for (int i = 0; i < 3; i++)
					dynDropDownList.Items.Add (new ListItem (this ["Dyn" + i], i.ToString ()));
				dynDropDownList.AutoPostBack = true;
				dynDropDownList.CssClass = "ms-input";
				dynDropDownList.Enabled = webPart.LicEd (4);
				dynDropDownList.SelectedIndex = WebPart.DynamicInteractiveFilters;
				Controls.Add (dynDropDownList);
				if (this ["DynamicInteractiveFilters"].StartsWith ("{0}"))
					Controls.Add (new LiteralControl (this ["DynamicInteractiveFilters"].Substring (3)));
				Controls.Add (new LiteralControl ("<br/></div><div>"));
				urlSettingsCheckBox.Checked = WebPart.UrlSettings;
				urlSettingsCheckBox.Enabled = WebPart.LicEd (4);
				urlSettingsCheckBox.Text = ProductPage.GetResource ("UrlSettings");
				Controls.Add (urlSettingsCheckBox);
				if (ProductPage.Is14) {
					Controls.Add (new LiteralControl ("</div><br/><b>" + this ["Ajax14"] + "</b><ul><li>" + this ["Ajax14Interval"] + " "));
					ajax14TextBox.Style [HtmlTextWriterStyle.Width] = "40px";
					ajax14TextBox.CssClass = "ms-input";
					ajax14TextBox.Text = WebPart.ajax14Interval.ToString ();
					Controls.Add (ajax14TextBox);
					Controls.Add (new LiteralControl (" " + this ["Ajax14Interval2"] + "</li><li>"));
					ajax14FocusCheckBox.Text = this ["Ajax14Focus"];
					ajax14FocusCheckBox.Checked = WebPart.ajax14focus;
					Controls.Add (ajax14FocusCheckBox);
					Controls.Add (new LiteralControl ("</li><li>"));
					ajax14CheckBox.Text = this ["Ajax14Hide"];
					ajax14CheckBox.Checked = WebPart.ajax14hide;
					Controls.Add (ajax14CheckBox);
					Controls.Add (new LiteralControl ("</li></ul><div>"));
				}
				Controls.Add (new LiteralControl ("</div><div><br/><span id=\"roxmultilabel\">" + this ["MultiValueFilter"] + "</span>"));
				multiDropDownList.AutoPostBack = false;
				multiDropDownList.CssClass = "ms-input";
				multiDropDownList.Enabled = webPart.LicEd (4);
				multiDropDownList.ID = "multiDropDownList";
				multiDropDownList.Width = new Unit (98, UnitType.Percentage);
				Controls.Add (multiDropDownList);
				filterListBox.Attributes ["onchange"] = "jQuery('#roxFilterUpButton').css({'visibility':((this.selectedIndex>0)?'visible':'hidden')});jQuery('#roxFilterDownButton').css({'visibility':((this.selectedIndex<(this.options.length-1))?'visible':'hidden')});jQuery('#roxFilterRemoveButton').css({'visibility':(((this.selectedIndex>=0)&&(!" + (WebPart._rowConnected ? "true" : "false") + ")&&(jQuery(document.getElementById('" + multiDropDownList.ClientID + "').options[document.getElementById('" + multiDropDownList.ClientID + "').selectedIndex]).text()!=jQuery(document.getElementById('" + filterListBox.ClientID + "').options[document.getElementById('" + filterListBox.ClientID + "').selectedIndex]).text()))?'visible':'hidden')});jQuery('#roxFilterEditButton').css({'visibility':((this.selectedIndex>=0)?'visible':'hidden')});";
				Controls.Add (new LiteralControl ("</div><br/><div>" + ProductPage.GetProductResource ("AcSecFields") + "<br/>"));
				acSecFieldsTextBox.CssClass = "ms-input";
				acSecFieldsTextBox.TextMode = TextBoxMode.MultiLine;
				acSecFieldsTextBox.Rows = 2;
				acSecFieldsTextBox.Width = new Unit (98, UnitType.Percentage);
				acSecFieldsTextBox.Wrap = false;
				acSecFieldsTextBox.Text = WebPart.AcSecFields;
				Controls.Add (acSecFieldsTextBox);
				Controls.Add (new LiteralControl ("</div><br/><div>" + ProductPage.GetResource ("Jquery") + "<br/>"));
				jqueryDropDownList.AutoPostBack = false;
				jqueryDropDownList.CssClass = "ms-input";
				jqueryDropDownList.Style ["width"] = "99%";
				for (int i = 0; i < 3; i++)
					jqueryDropDownList.Items.Add (new ListItem (ProductPage.GetResource ("Jquery_" + i), i.ToString ()));
				jqueryDropDownList.SelectedIndex = WebPart.JQuery;
				Controls.Add (jqueryDropDownList);
				Controls.Add (new LiteralControl ("</div>" + FORMAT_SUFFIX_CONTROL));
				Controls.Add (new LiteralControl (string.Format (FORMAT_PREFIX_CONTROL, this ["Groups"], this ["GroupsDesc"], "")));
				groupsTextBox.CssClass = "ms-input";
				groupsTextBox.Enabled = webPart.LicEd (4);
				groupsTextBox.TextMode = TextBoxMode.MultiLine;
				groupsTextBox.Rows = (groupsTextBox.Text = WebPart.Groups).Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length + 2;
				groupsTextBox.Wrap = false;
				groupsTextBox.Width = new Unit (98, UnitType.Percentage);
				Controls.Add (groupsTextBox);
				Controls.Add (new LiteralControl (FORMAT_SUFFIX_CONTROL));
				Controls.Add (new LiteralControl (string.Format (FORMAT_PREFIX_CONTROL, this ["DebugMode"], this ["DebugModeDesc"], "")));
				debugOnRadioButton.Text = this ["DebugModeOn"];
				debugOffRadioButton.Text = this ["DebugModeOff"];
				debugOffRadioButton.GroupName = debugOnRadioButton.GroupName = "DebugMode";
				debugOffRadioButton.Checked = (!(debugOnRadioButton.Checked = WebPart.DebugMode));
				debugOnRadioButton.Enabled = debugOffRadioButton.Enabled = errorOnRadioButton.Enabled = errorOffRadioButton.Enabled = filterListBox.Enabled = filterDropDownList.Enabled = !webPart.Exed;
				Controls.Add (debugOnRadioButton);
				Controls.Add (new LiteralControl ("<br/>"));
				Controls.Add (debugOffRadioButton);
				Controls.Add (new LiteralControl (FORMAT_SUFFIX_CONTROL));
				Controls.Add (new LiteralControl (string.Format (FORMAT_PREFIX_CONTROL, this ["ErrorMode"], this ["ErrorModeDesc"], "")));
				errorOnRadioButton.Text = this ["ErrorModeOn"];
				errorOffRadioButton.Text = this ["ErrorModeOff"];
				errorOffRadioButton.GroupName = errorOnRadioButton.GroupName = "ErrorMode";
				errorOffRadioButton.Checked = (!(errorOnRadioButton.Checked = WebPart.ErrorMode));
				Controls.Add (errorOnRadioButton);
				Controls.Add (new LiteralControl ("<br/>"));
				Controls.Add (errorOffRadioButton);
				Controls.Add (new LiteralControl (FORMAT_SUFFIX_CONTROL + "</span>"));
			}
		}

		protected override void OnLoad (EventArgs e) {
			string listVal, fieldVal;
			int tmp = -1, selIndex = filterListBox.SelectedIndex;
			bool editorVisible = false;
			FilterBase filter;
			SPList parentLib = null;
			if ((WebPart != null) && (WebPart.CanRun)) {
				if (disableDropDownList.Items.Count == 0) {
					disableDropDownList.Items.Add (new ListItem (this ["DisableFiltersAll"], "0"));
					disableDropDownList.Items.Add (new ListItem (this ["DisableFiltersSome"], "1"));
					disableDropDownList.Items.Add (new ListItem (this ["DisableFiltersNone"], "2"));
					disableDropDownList.SelectedIndex = ((!WebPart.DisableFilters) ? 2 : (WebPart.DisableFiltersSome ? 1 : 0));
				}
				foreach (FilterBase f in Filters)
					f.isEditMode = false;
				if (filterDropDownList.SelectedIndex > 0)
					Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery('#roxFilterAddButton').css({ 'visibility': 'visible' }); </script>"));
				if ((!string.IsNullOrEmpty (Context.Request ["roxfilteraction"])) && (((selIndex >= 0) && (selIndex < filterListBox.Items.Count)) || (Context.Request ["roxfilteraction"] == "add"))) {
					if ((Context.Request ["roxfilteraction"] == "add") && (!string.IsNullOrEmpty (bigAnnoyance = Context.Request [filterDropDownList.ClientID.Replace ("ctl00_", "ctl00$").Replace ("MSO_ContentDiv_", "MSO_ContentDiv$").Replace ("MSOTlPn_EditorZone_", "MSOTlPn_EditorZone$").Replace ("Zone_Edit", "Zone$Edit").Replace ("_filterDropDownList", "$filterDropDownList")])) && (!bigAnnoyance.StartsWith ("____")) && (!bigAnnoyance.StartsWith ("_$$_"))) {
						Filters.Add (FilterBase.Create (bigAnnoyance));
						Filters [Filters.Count - 1].isEditMode = true;
						Filters [Filters.Count - 1].parentWebPart = WebPart;
						hiddenTextBox.Text = FilterBase.Serialize (Filters);
						selIndex = Filters.Count - 1;
						editorVisible = true;
						filterEditorHidden.Value = "1";
					} else if (Context.Request ["roxfilteraction"] == "up") {
						filter = Filters [selIndex];
						Filters.RemoveAt (selIndex);
						Filters.Insert (--selIndex, filter);
					} else if (Context.Request ["roxfilteraction"] == "down") {
						filter = Filters [selIndex];
						Filters.RemoveAt (selIndex);
						Filters.Insert (++selIndex, filter);
					} else if (Context.Request ["roxfilteraction"] == "remove") {
						Filters.RemoveAt (selIndex);
						hiddenTextBox.Text = FilterBase.Serialize (Filters);
					} else if (Context.Request ["roxfilteraction"] == "edit") {
						editorVisible = true;
						filterEditorHidden.Value = "1";
						Filters [selIndex].isEditMode = true;
					} else if ((Context.Request ["roxfilteraction"] == "editstop") || (Context.Request ["roxfilteraction"] == "editsave")) {
						editorVisible = false;
						filterEditorHidden.Value = "";
						if (Context.Request ["roxfilteraction"] == "editsave") {
							Filters [selIndex].Enabled = editorEnabledCheckBox.Checked;
							Filters [selIndex].Name = editorNameTextBox.Text;
							Filters [selIndex].isEditMode = true;
							Filters [selIndex].resolve = false;
							Filters [selIndex].UpdateProperties (editorPanel);
							Filters [selIndex].resolve = true;
							Filters [selIndex].isEditMode = false;
							hiddenTextBox.Text = FilterBase.Serialize (Filters);
						}
					}
					hiddenTextBox.Text = FilterBase.Serialize (Filters);
					if (Array.IndexOf<string> (new string [] { "up", "down", "remove", "add" }, Context.Request ["roxfilteraction"]) >= 0)
						Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery(document).ready(function() { toolFilterAction(''); }); </script>"));
				}
				if (editorVisible |= (filterEditorHidden.Value == "1")) {
					if (selIndex < 0)
						selIndex = Filters.Count - 1;
					if ((selIndex >= 0) && (selIndex < Filters.Count)) {
						if (Filters [selIndex].Enabled)
							Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> document.getElementById('" + editorEnabledCheckBox.ClientID + "').checked = true; </script>"));
						editorNameTextBox.Text = Filters [selIndex].Name;
						Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> showFilterEditor('" + ((filterListBox.SelectedItem == null) ? "" : SPEncode.ScriptEncode (filterListBox.SelectedItem.Text.Replace (this ["Disabled"], string.Empty))) + "'); </script>"));
						Filters [selIndex].isEditMode = true;
						foreach (FilterBase fb in WebPart.GetFilters (false, false))
							if (fb.ID.Equals (Filters [selIndex].ID))
								fb.isEditMode = true;
						Filters [selIndex].resolve = false;
						Filters [selIndex].UpdatePanel (editorPanel);
						editFilter = Filters [selIndex];
						Filters [selIndex].resolve = true;
					}
				}
				filterListBox.Items.Clear ();
				foreach (FilterBase f in Filters)
					filterListBox.Items.Add (new ListItem (f.ToString (), (++tmp).ToString ()));
				try {
					filterListBox.SelectedIndex = selIndex;
				} catch {
				}
				if ((filterListBox.SelectedIndex >= 0) && (filterListBox.SelectedIndex < filterListBox.Items.Count)) {
					Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery('#roxFilterRemoveButton').css({ 'visibility': ((jQuery(document.getElementById('" + multiDropDownList.ClientID + "').options[document.getElementById('" + multiDropDownList.ClientID + "').selectedIndex]).text() == '" + filterListBox.SelectedItem.Text.Replace ("\'", "\\\'") + "') ? 'hidden' : '" + (WebPart._rowConnected ? "hidden" : "visible") + "') }); jQuery('#roxFilterEditButton').css({ 'visibility': 'visible' }); </script>"));
					if (filterListBox.SelectedIndex > 0)
						Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery('#roxFilterUpButton').css({ 'visibility': 'visible' }); </script>"));
					if (filterListBox.SelectedIndex < (filterListBox.Items.Count - 1))
						Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery('#roxFilterDownButton').css({ 'visibility': 'visible' }); </script>"));
				}
				if (e != null)
					base.OnLoad (e);
				groupsTextBox.Rows = groupsTextBox.Text.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length + 2;
				listVal = ((multiDropDownList.Items.Count == 0) ? WebPart.MultiValueFilterID : multiDropDownList.SelectedValue);
				multiDropDownList.Items.Clear ();
				foreach (FilterBase fb in Filters)
					if (fb.Enabled && fb.SupportsMultipleValues && !(fb is FilterBase.Multi)) {
						if (multiDropDownList.Items.Count == 0)
							multiDropDownList.Items.Add (new ListItem (this ["FilterNone", "'" + fb.Name + "'"], string.Empty));
						multiDropDownList.Items.Add (new ListItem (fb.ToString (), fb.ID));
					}
				if (multiDropDownList.Items.Count == 0)
					multiDropDownList.Items.Add (new ListItem (this ["FilterNone", this ["FilterNoneNone"]], string.Empty));
				try {
					multiDropDownList.SelectedValue = listVal;
				} catch {
					multiDropDownList.SelectedIndex = 0;
				}
				ProductPage.Elevate (delegate () {
					listVal = wizardListDropDownList.SelectedValue;
					fieldVal = wizardFieldDropDownList.SelectedValue;
					wizardListDropDownList.Items.Clear ();
					wizardFieldDropDownList.Items.Clear ();
					wizardListDropDownList.Items.Add (new ListItem (this ["WizardList"], ""));
					wizardFieldDropDownList.Items.Add (new ListItem (this ["WizardField"], ""));
					wizardListDropDownList.SelectedIndex = wizardFieldDropDownList.SelectedIndex = 0;
					if (string.IsNullOrEmpty (wizardTextBox.Text.Trim ()))
						wizardTextBox.Text = SPContext.Current.Web.Url;
					try {
						using (SPSite site = new SPSite (wizardTextBox.Text.Trim ()))
						using (SPWeb web = site.OpenWeb ()) {
							foreach (SPList list in ProductPage.TryEach<SPList> (web.Lists))
								wizardListDropDownList.Items.Add (new ListItem (list.Title, list.ID.ToString ()));
							if (!string.IsNullOrEmpty (listVal))
								try {
									wizardListDropDownList.SelectedValue = listVal;
								} catch {
									wizardListDropDownList.SelectedIndex = 0;
								} else if ((selIndex >= 0) && (selIndex < Filters.Count) && (Filters [selIndex] is FilterBase.PageField))
								try {
									using (SPWrap<SPList> wrap = ((FilterBase.Interactive) Filters [selIndex]).GetList ("ListUrl", true))
										if ((parentLib = wrap.Value) != null)
											wizardListDropDownList.SelectedValue = parentLib.ID.ToString ();
								} catch {
								}
							if (wizardListDropDownList.SelectedIndex > 0)
								foreach (SPField field in ProductPage.TryEach<SPField> (web.Lists [new Guid (wizardListDropDownList.SelectedValue)].Fields))
									wizardFieldDropDownList.Items.Add (new ListItem (field.Title, field.InternalName));
							if (!string.IsNullOrEmpty (fieldVal))
								try {
									wizardFieldDropDownList.SelectedValue = fieldVal;
								} catch {
									wizardFieldDropDownList.SelectedIndex = 0;
								}
							wizardLabel.Text = (string.IsNullOrEmpty (wizardFieldDropDownList.SelectedValue) ? (string.IsNullOrEmpty (wizardListDropDownList.SelectedValue) ? this ["WizardLabel"] : this ["WizardUrl", ProductPage.MergeUrlPaths (web.Url, web.Lists [new Guid (wizardListDropDownList.SelectedValue)].DefaultViewUrl)]) : string.Format (this ["WizardName"], wizardFieldDropDownList.SelectedValue, wizardFieldDropDownList.ClientID));
						}
					} catch (Exception ex) {
						wizardLabel.Text = ex.Message;
					}
				}, true);
			} else
				base.OnLoad (e);
		}

		protected override void RenderToolPart (HtmlTextWriter output) {
			int selIndex = nameDropDownList.SelectedIndex;
			Dictionary<KeyValuePair<string, string>, FilterBase.Interactive> knownFilters = new Dictionary<KeyValuePair<string, string>, FilterBase.Interactive> ();
			if (!ProductPage.isEnabled) {
				using (SPSite adminSite = ProductPage.GetAdminSite ())
					output.Write (FilterToolPart.FORMAT_INFO_CONTROL, ProductPage.GetResource ("NotEnabled", ProductPage.MergeUrlPaths (adminSite.Url, "/_layouts/roxority_FilterZen/default.aspx?cfg=enable"), "FilterZen"), "servicenotinstalled.gif", "noid");
				return;
			}
			if (WebPart != null) {
				WebPart.RenderScripts (output, WebUrl);
				if (!(dynDropDownList.Enabled = suppressCheckBox.Enabled = (WebPart.LicEd (4) && (WebPart.validFilterNames.Count > 0)))) {
					suppressCheckBox.Checked = false;
					dynDropDownList.SelectedIndex = 0;
				}
				if (1.Equals (filterListBox.Rows = filterListBox.Items.Count + 1))
					filterListBox.Style ["display"] = "none";
				else
					filterListBox.Style.Remove ("display");
				if (InteractiveCount != 0)
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery(document).ready(function() { document.getElementById('roxifilters').style.display = 'block'; }); </script>");
				nameDropDownList.Items.Clear ();
				nameDropDownList.Items.Add (new ListItem (this ["NameSelector"], string.Empty));
				foreach (KeyValuePair<string, string> kvp in WebPart.validFilterNames) {
					nameDropDownList.Items.Add (new ListItem (kvp.Value, kvp.Key));
					if (!Filters.Exists (delegate (FilterBase fb) {
						return (fb.Name == kvp.Key);
					}))
						knownFilters [kvp] = WebPart.CreateDynamicInteractiveFilter (kvp);
				}
				try {
					nameDropDownList.SelectedIndex = selIndex;
				} catch {
				}
				selIndex = 0;
				Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\">\n"));
				foreach (Type t in FilterBase.FilterTypes) {
					selIndex++;
					foreach (KeyValuePair<KeyValuePair<string, string>, FilterBase.Interactive> pair in knownFilters)
						if ((pair.Value != null) && (t == pair.Value.GetType ())) {
							selIndex++;
							filterDropDownList.Items.Insert (selIndex, new ListItem (this ["FilterFly" + (pair.Key.Key.Equals (pair.Key.Value) ? "1" : string.Empty), pair.Key.Value, pair.Key.Key], "____" + pair.Key.Key));
							filterDropDownList.Items [selIndex].Attributes ["style"] = "color: #666;";
							Controls.Add (new LiteralControl ("roxFilterDescs['____" + pair.Key.Key + "'] = '" + SPEncode.ScriptEncode (this ["FilterFlyDesc", FilterBase.GetFilterTypeTitle (t)]) + "';\n"));
						} else if ((t == typeof (FilterBase.Lookup)) && (webPart.connectedList != null)) {
							selIndex++;
							filterDropDownList.Items.Insert (selIndex, new ListItem (this ["FilterFly" + (pair.Key.Key.Equals (pair.Key.Value) ? "1" : string.Empty), pair.Key.Value, pair.Key.Key], "_$$_" + pair.Key.Key));
							filterDropDownList.Items [selIndex].Attributes ["style"] = "color: #666;";
							Controls.Add (new LiteralControl ("roxFilterDescs['_$$_" + pair.Key.Key + "'] = '" + SPEncode.ScriptEncode (this ["FilterFlyDesc", FilterBase.GetFilterTypeTitle (t)]) + "';\n"));
						}
				}
				Controls.Add (new LiteralControl ("</script>\n"));
				if ((bigAnnoyance != null) && (bigAnnoyance.StartsWith ("____") || bigAnnoyance.StartsWith ("_$$_"))) {
					foreach (FilterBase fb in Filters)
						fb.isEditMode = false;
					Filters.Add (WebPart.CreateDynamicInteractiveFilter (bigAnnoyance.Substring (4), bigAnnoyance.StartsWith ("_$$_")));
					Filters [Filters.Count - 1].isEditMode = true;
					Filters [Filters.Count - 1].parentWebPart = WebPart;
					hiddenTextBox.Text = FilterBase.Serialize (Filters);
					filterEditorHidden.Value = "1";
					hiddenTextBox.Text = FilterBase.Serialize (Filters);
					filterListBox.Items.Add (new ListItem (Filters [Filters.Count - 1].ToString (), (Filters.Count - 1).ToString ()));
					filterListBox.SelectedIndex = filterListBox.Items.Count - 1;
					Controls.Add (new LiteralControl ("<script type=\"text/javascript\" language=\"JavaScript\"> showFilterEditor('" + ((filterListBox.Items.Count == 0) ? "" : SPEncode.ScriptEncode (filterListBox.Items [filterListBox.Items.Count - 1].Text.Replace (this ["Disabled"], string.Empty))) + "'); </script>"));
					Filters [Filters.Count - 1].isEditMode = true;
					foreach (FilterBase fb in WebPart.GetFilters (false, false))
						if (fb.ID.Equals (Filters [Filters.Count - 1].ID))
							fb.isEditMode = true;
					Filters [Filters.Count - 1].resolve = false;
					Filters [Filters.Count - 1].UpdatePanel (editorPanel);
					editFilter = null;
					Filters [Filters.Count - 1].resolve = true;
				}
				if ((editFilter != null) && editFilter.requirePostLoadRendering) {
					editFilter.isEditMode = true;
					editFilter.parentWebPart = WebPart;
					editFilter.resolve = false;
					editorPanel.Controls.Clear ();
					editFilter.UpdatePanel (editorPanel);
					editFilter.resolve = true;
					editFilter.isEditMode = false;
				}
				if (WebPart._rowConnected) {
					output.Write ("<style type=\"text/css\"> .rox-proplimited { display: none !important; } </style>");
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery(document).ready(function() { jQuery('#roxFilterRemoveButton').css({ 'visibility': 'hidden' }); }); </script>");
				}
				if (WebPart.transform != null) {
					output.Write ("<script type=\"text/javascript\" language=\"JavaScript\"> jQuery(document).ready(function() { document.getElementById('" + multiDropDownList.ClientID + "').style.display = 'none'; jQuery('#roxmultilabel').html(jQuery('#roxmultilabel').html() + '&nbsp;<b>" + WebPart.transform.Filter.ToString () + "</b>'); }); </script>");
					if (!WebPart._rowConnected)
						foreach (FilterBase fb in Filters) {
							if (fb.isEditMode && fb.ID.Equals (WebPart.transform.Filter.ID))
								output.Write ("<style type=\"text/css\"> .rox-proplimited { display: none !important; } </style>");
							break;
						}
				}
			}
			output.Write ("<div class=\"rox-toolpart\">");
			base.RenderToolPart (output);
			output.Write ("</div>");
		}

		public override void ApplyChanges () {
			int maxFiltersPerRow, tmpInt;
			//Guid wpID = Guid.Empty;
			//SPWebPartManager wpMan;
			base.ApplyChanges ();
			ProductPage.Check ("ApplyChanges", false);
			EnsureChildControls ();
			if ((WebPart != null) && WebPart.CanRun) {
				//if (!string.IsNullOrEmpty (webPart.ID))
				//    try {
				//        wpID = ProductPage.GetGuid (webPart.ID.Substring (2).Replace ("_", "-"), false);
				//    } catch {
				//    }
				WebPart.ApplyToolbarStylings = toolStyleCheckBox.Checked;
				WebPart.SuppressSpacing = toolSpaceCheckBox.Checked;
				WebPart.MultiValueFilterID = multiDropDownList.SelectedValue;
				WebPart.AutoRepost = sendOnChangeCheckBox.Checked;
				WebPart.EmbedFilters = embedFiltersCheckBox.Checked;
				WebPart.DebugMode = debugOnRadioButton.Checked;
				WebPart.ErrorMode = errorOnRadioButton.Checked;
				WebPart.FiltersList = Filters;
				WebPart.HtmlEmbed = htmlTextBox.Text;
				WebPart.HtmlMode = htmlDropDownList.SelectedIndex;
				WebPart.JQuery = jqueryDropDownList.SelectedIndex;
				if (int.TryParse (maxTextBox.Text, out maxFiltersPerRow))
					WebPart.MaxFiltersPerRow = maxFiltersPerRow;
				else {
					WebPart.MaxFiltersPerRow = 0;
					maxTextBox.Text = "0";
				}
				WebPart.DynamicInteractiveFilters = dynDropDownList.SelectedIndex;
				WebPart.CamlFilters = camlYesRadioButton.Checked;
				WebPart.AutoConnect = autoCheckBox.Checked;
				WebPart.CamlFiltersAndCombined = camlTextBox.Text;
				WebPart.JsonFilters = jsonTextBox.Text;
				WebPart.SuppressUnknownFilters = suppressCheckBox.Checked;
				WebPart.RememberFilterValues = rememberCheckBox.Checked;
				WebPart.Cascaded = cascadedCheckBox.Checked;
				WebPart.FolderScope = folderDropDownList.SelectedValue;
				WebPart.RecollapseGroups = recollapseGroupsCheckBox.Checked;
				WebPart.DisableFilters = (disableDropDownList.SelectedIndex < 2);
				WebPart.DisableFiltersSome = (disableDropDownList.SelectedIndex == 1);
				WebPart.RespectFilters = respectCheckBox.Checked;
				WebPart.DefaultToOr = defaultOrCheckBox.Checked;
				WebPart.UrlSettings = urlSettingsCheckBox.Checked;
				WebPart.Ajax14hide = ajax14CheckBox.Checked;
				WebPart.Ajax14focus = ajax14FocusCheckBox.Checked;
				WebPart.Ajax14Interval = int.TryParse (ajax14TextBox.Text, out tmpInt) ? tmpInt : 0;
				WebPart.Groups = groupsTextBox.Text;
				webPart.forceReload = true;
				webPart.UrlParams = urlParamCheckBox.Checked;
				webPart.SearchBehaviour = searchCheckBox.Checked;
				webPart.Highlight = highlightCheckBox.Checked;
				webPart.ShowClearButtons = showClearCheckBox.Checked;
				webPart.AcSecFields = acSecFieldsTextBox.Text;
			}
			base.ApplyChanges ();
			//try {
			//    if (ProductPage.Is14 && ((wpMan = WebPartManager as SPWebPartManager) != null) && (!Guid.Empty.Equals (wpID)) && WebPart.IsViewPageCore)
			//        ProductPage.Elevate (delegate () {
			//            wpMan.SaveChanges (wpID);
			//        }, true);
			//} catch {
			//}
		}

		public override void CancelChanges () {
			if (WebPart != null)
				WebPart.toolPart = null;
			base.CancelChanges ();
		}

		public List<FilterBase> Filters {
			get {
				if (filters == null)
					if (!string.IsNullOrEmpty (hiddenTextBox.Text))
						filters = FilterBase.Deserialize (WebPart, hiddenTextBox.Text);
					else if (WebPart != null) {
						filters = WebPart.GetFilters (false, false);
						hiddenTextBox.Text = FilterBase.Serialize (filters);
					}
				return filters;
			}
		}

		public int InteractiveCount {
			get {
				FilterBase.Interactive ifilter;
				int count = ((dynDropDownList.Enabled && (dynDropDownList.SelectedIndex > 0)) ? -1 : 0);
				if (count == 0)
					foreach (FilterBase filter in (string.IsNullOrEmpty (hiddenTextBox.Text) ? WebPart.GetFilters (false, false) : Filters))
						if (((ifilter = filter as FilterBase.Interactive) != null) && (ifilter.Enabled) && (ifilter.IsInteractive))
							count++;
				return count;
			}
		}

		public string this [string resKey, params object [] args] {
			get {
				return ProductPage.GetProductResource (resKey, args);
			}
		}

		public int ListPartCount {
			get {
				int count = 0;
				foreach (Microsoft.SharePoint.WebPartPages.WebPart wp in WebPart.connectedParts)
					if (wp is ListViewWebPart)
						count++;
				return count;
			}
		}

		public roxority_FilterWebPart WebPart {
			get {
				if ((webPart == null) && (ParentToolPane != null))
					webPart = ParentToolPane.SelectedWebPart as roxority_FilterWebPart;
				return webPart;
			}
		}

		public string WebUrl {
			get {
				if (webUrl == null)
					try {
						webUrl = SPContext.Current.Web.Url.TrimEnd ('/');
					} catch {
						webUrl = string.Empty;
					}
				return webUrl;
			}
		}

	}

}
