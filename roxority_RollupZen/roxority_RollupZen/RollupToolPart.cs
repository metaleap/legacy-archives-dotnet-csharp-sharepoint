
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebPartPages;
using Microsoft.SharePoint.WebPartPages.Communication;
using roxority.Data;
using roxority.Shared;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;

using TahoeWebPart = Microsoft.SharePoint.WebPartPages.WebPart;

namespace roxority_RollupZen {

	internal class RollupToolPart : ToolPart {

		internal const string SELEMPTY = "e58b8e83-784f-4fab-943a-8552e4aa7032";

		internal const string PREFIX_CONTROL = @"
<div>
<table><tr><td><div class=""rox-UserSectionTitle"">{0}</div></td></tr></table>
<div>
<table><tr><td>
<div class=""rox-UserSectionHead""><label for=""{2}"">{1}</label></div>
<div class=""rox-UserSectionBody""><div class=""rox-UserControlGroup"">
";

		internal const string PREFIX_CONTROL_HLP = @"
<div>
<table><tr><td><div class=""rox-UserSectionTitle"">{0}<a target=""_blank"" href=""/_layouts/{3}/default.aspx?doc={4}""><img src=""/_layouts/images/hhelp.gif""/></a></div></td></tr></table>
<div>
<table><tr><td>
<div class=""rox-UserSectionHead""><label for=""{2}"">{1}</label></div>
<div class=""rox-UserSectionBody""><div class=""rox-UserControlGroup"">
";

		internal const string SUFFIX_CONTROL = @"
</div></div>
<div class=""rox-UserDottedLine""> </div>
</td></tr></table>
</div>
</div>
";

		internal static readonly Random rnd = new Random ();

		internal TextBox dateIntervalTextBox = new TextBox (), datePropTextBox = new TextBox (), imageHeightTextBox = new TextBox (), pageSizeTextBox = new TextBox (), propsTextBox = new TextBox (), rowSizeTextBox = new TextBox (), tileTextBox = new TextBox ();
		internal CheckBox curUserCheckBox = new CheckBox (), dateNoDayCheckBox = new CheckBox (), dateThisYearCheckBox = new CheckBox (), filterLiveCheckBox = new CheckBox (), groupByCountsCheckBox = new CheckBox (), groupIntCheckBox = new CheckBox (), groupIntDirCheckBox = new CheckBox (), groupShowCountsCheckBox = new CheckBox (), presenceCheckBox = new CheckBox (), sortCheckBox = new CheckBox (), tabCheckBox = new CheckBox (), urlSettingsCheckBox = new CheckBox (), vcardCheckBox = new CheckBox (), viewCheckBox = new CheckBox ();
		internal RadioButton styleClassicRadioButton = new RadioButton (), styleListRadioButton = new RadioButton (), groupAscRadioButton = new RadioButton (), groupDescRadioButton = new RadioButton (), sortAscRadioButton = new RadioButton (), sortDescRadioButton = new RadioButton ();
		internal DropDownList animDropDownList = new DropDownList (), dataSourceDropDownList = new DropDownList (), expDropDownList = new DropDownList (), groupDropDownList = new DropDownList (), jqueryDropDownList = new DropDownList (), nameDropDownList = new DropDownList (), pageDropDownList = new DropDownList (), pageSkipDropDownList = new DropDownList (), pageStepDropDownList = new DropDownList (), pictDropDownList = new DropDownList (), printDropDownList = new DropDownList (), propDropDownList = new DropDownList (), sortDropDownList = new DropDownList (), tabDropDownList = new DropDownList ();

		private SortedDictionary<string, string> knownProps = null;
		private string knownPropID = null;

		internal static string Res (string name, params object [] args) {
			return ProductPage.GetProductResource (name, args);
		}

		public RollupToolPart () {
			Title = ProductPage.GetProductResource ("WebPart_DefaultTitle");
			ChromeState = PartChromeState.Normal;
			Visible = true;
		}

		internal bool NeedsRefresh (DropDownList propList, string extraProp) {
			int knownCount = KnownProps.Count + (string.IsNullOrEmpty (extraProp) ? 0 : 1);
			if ((propList.Items.Count == 0) || (propList.Items.Count != (knownCount + 1)))
				return true;
			for (int i = 1; i < propList.Items.Count; i++)
				if ((!KnownProps.ContainsKey (propList.Items [i].Value)) && (string.IsNullOrEmpty (extraProp) || (extraProp != propList.Items [i].Value)))
					return true;
			foreach (string k in KnownProps.Keys)
				if (propList.Items.FindByValue (k) == null)
					return true;
			return false;
		}

		protected override void CreateChildControls () {
			string [] pair;
			string tmp;
			List<string> cssClasses = new List<string> ();
			RollupWebPart webPart = ParentToolPane.SelectedWebPart as RollupWebPart;
			if (webPart != null)
				foreach (string propLine in webPart.Properties.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
					if (((pair = propLine.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pair.Length >= 2))
						cssClasses.Add ("rox-rollupitem-" + pair [0].Trim ().ToLowerInvariant ());
			base.CreateChildControls ();
			ProductPage.CreateLicControls (Controls, PREFIX_CONTROL.Replace ("UserSectionTitle\"", "UserSectionTitle\" style=\"font-weight: normal;\""), SUFFIX_CONTROL);
			Controls.Add (new LiteralControl (string.Format (PREFIX_CONTROL, Res ("DataSourceMode"), Res ("DataSourceModeDesc"), string.Empty)));
			Controls.Add (new LiteralControl ("<div>"));
			dataSourceDropDownList.AutoPostBack = true;
			Controls.Add (dataSourceDropDownList);
			Controls.Add (new LiteralControl ("</div><div><a target=\"_blank\" href=\"" + SPContext.Current.Web.Url + "/_layouts/" + ProductPage.AssemblyName + "/default.aspx?cfg=tools&tool=Tool_DataSources\">" + Res ("Tool_ItemEditor_DefaultDesc2") + string.Format ((tmp = Res ("Tool_ItemEditor_DefaultDesc")).Substring (tmp.IndexOf ('.') + 2), string.Empty, ProductPage.GetTitle (), Res ("Tool_DataSources_Title")) + "</a></div>"));
			Controls.Add (new LiteralControl (SUFFIX_CONTROL));
			Controls.Add (new LiteralControl (string.Format (PREFIX_CONTROL_HLP, Res ("Style"), Res ("StyleDesc"), string.Empty, ProductPage.AssemblyName, "user_profiles_web_part_display_style")));
			Controls.Add (new LiteralControl ("<div>"));
			styleClassicRadioButton.GroupName = "PeopleStyle";
			styleClassicRadioButton.ID = "styleClassicRadioButton";
			Controls.Add (styleClassicRadioButton);
			Controls.Add (new LiteralControl (string.Format ("<label for=\"{0}\">" + Res ("StyleClassic") + "</label></div>", styleClassicRadioButton.ClientID)));
			Controls.Add (new LiteralControl ("<div style=\"padding-left: 28px;\">" + Res ("RowSize1") + " "));
			rowSizeTextBox.CssClass = "ms-input";
			rowSizeTextBox.Attributes ["onchange"] = "if(!roxRowInfoShown){roxRowInfoShown=true;jQuery('#roxrowsizeinfo').show();}";
			rowSizeTextBox.Attributes ["onfocus"] = "jQuery('#roxpeoprowinfo').show();";
			rowSizeTextBox.Attributes ["onblur"] = "jQuery('#roxpeoprowinfo').hide();";
			rowSizeTextBox.Style ["width"] = "24px";
			rowSizeTextBox.Style ["text-align"] = "center";
			Controls.Add (rowSizeTextBox);
			Controls.Add (new LiteralControl (Res ("TileWidth1") + " "));
			tileTextBox.CssClass = "ms-input";
			tileTextBox.Style ["width"] = "40px";
			tileTextBox.Style ["text-align"] = "center";
			Controls.Add (tileTextBox);
			Controls.Add (new LiteralControl (" " + Res ("TileWidth2") + "<div id=\"roxpeoprowinfo\" style=\"display: none;\">" + Res ("RowSize2") + "</div></div><div style=\"display: none;\" id=\"roxrowsizeinfo\" class=\"rox-error\">" + Res ("RowSize3") + "</div>"));
			Controls.Add (new LiteralControl ("<div>"));
			styleListRadioButton.GroupName = "PeopleStyle";
			styleListRadioButton.ID = "styleListRadioButton";
			Controls.Add (styleListRadioButton);
			Controls.Add (new LiteralControl (string.Format ("<label for=\"{0}\">" + Res ("StyleList") + "</label></div>", styleListRadioButton.ClientID)));
			Controls.Add (new LiteralControl ("<div style=\"padding-left: 28px;\">"));
			Controls.Add (sortCheckBox);
			Controls.Add (new LiteralControl (string.Format ("<label for=\"{0}\">" + Res ("AllowSort") + "</label></div>", sortCheckBox.ClientID)));
			Controls.Add (new LiteralControl ("<div>"));
			Controls.Add (viewCheckBox);
			Controls.Add (new LiteralControl (string.Format ("<label for=\"{0}\">" + Res ("AllowView") + "</label></div>", viewCheckBox.ClientID)));
			Controls.Add (new LiteralControl (ProductPage.GetResource ("Css", ProductPage.AssemblyName)));
			Controls.Add (new LiteralControl ("<br/>" + ProductPage.GetProductResource ("HtmlInfo")));
			Controls.Add (new LiteralControl (SUFFIX_CONTROL));
			Controls.Add (new LiteralControl (string.Format (PREFIX_CONTROL_HLP, Res ("T_Prop11" + (RollupWebPart.IsRollupZen ? "R" : string.Empty)), Res ("D_Prop11" + (RollupWebPart.IsRollupZen ? "R" : string.Empty), webPart.ID), propsTextBox.ClientID, ProductPage.AssemblyName, "web_part_user_profile_properties")));
			propsTextBox.ID = "propsTextBox";
			propsTextBox.TextMode = TextBoxMode.MultiLine;
			propsTextBox.Rows = 2;
			propsTextBox.Wrap = false;
			propsTextBox.Width = new Unit (99, UnitType.Percentage);
			Controls.Add (propsTextBox);
			Controls.Add (new LiteralControl ("<div>" + ProductPage.GetResource ("Tool_ItemEditor_DataFields_LabelAdd") + " "));
			propDropDownList.Attributes ["onchange"] = "if(this.selectedIndex>0){document.getElementById('" + propsTextBox.ClientID + "').innerText+=('\\n'+this.options[this.selectedIndex].value+': '+this.options[this.selectedIndex].innerText);roxScrollEnd(document.getElementById('" + propsTextBox.ClientID + "'));}this.selectedIndex=0;";
			Controls.Add (propDropDownList);
			Controls.Add (new LiteralControl ("</select></div>"));
			Controls.Add (new LiteralControl ("<div>" + ProductPage.GetProductResource ("Old_SortInfo") + "</div>"));
			Controls.Add (new LiteralControl (SUFFIX_CONTROL));
			Controls.Add (new LiteralControl (string.Format (PREFIX_CONTROL_HLP, Res ("T_Prop5"), Res ("D_Prop5"), string.Empty, ProductPage.AssemblyName, "user_profiles_web_part_filtering_tabbing_search")));
			Controls.Add (new LiteralControl ("<br/><div>" + Res ("TabBy") + "<br/>"));
			Controls.Add (tabDropDownList);
			Controls.Add (new LiteralControl ("</div><div>"));
			tabCheckBox.Text = Res ("Retab");
			Controls.Add (tabCheckBox);
			Controls.Add (new LiteralControl ("</div>" + Res (RollupWebPart.IsRollupZen ? "PreFilterHint" : "FilterExclude") + "<br/><div>"));
#if PEOPLEZEN
			curUserCheckBox.Text = Res ("CurUser");
			Controls.Add (curUserCheckBox);
			Controls.Add (new LiteralControl ("</div><div>"));
#endif
			Controls.Add (dateThisYearCheckBox);
			Controls.Add (new LiteralControl (string.Format ("<label for=\"{0}\">" + Res ("T_Prop10") + "</label>", dateThisYearCheckBox.ClientID)));
			Controls.Add (new LiteralControl ("</div><div>"));
			Controls.Add (dateNoDayCheckBox);
			Controls.Add (new LiteralControl (string.Format ("<label for=\"{0}\">" + Res ("IgnoreDay") + "</label>", dateNoDayCheckBox.ClientID)));
			Controls.Add (new LiteralControl ("</div><div>"));
			Controls.Add (filterLiveCheckBox);
			Controls.Add (new LiteralControl (string.Format ("<label for=\"{0}\">" + Res ("FilterLive") + "</label></div>", filterLiveCheckBox.ClientID)));
			Controls.Add (new LiteralControl (SUFFIX_CONTROL));
			Controls.Add (new LiteralControl (string.Format (PREFIX_CONTROL_HLP, Res ("GroupSort"), Res ("GroupSortInfo" + (RollupWebPart.IsRollupZen ? "R" : string.Empty)), string.Empty, ProductPage.AssemblyName, "user_profiles_web_part_sorting_grouping")));
			Controls.Add (new LiteralControl ("<br/><div>" + Res ("Sort") + "<br/>"));
			Controls.Add (sortDropDownList);
			Controls.Add (new LiteralControl ("</div><div>"));
			sortAscRadioButton.Text = Res ("GroupSortAsc");
			sortAscRadioButton.GroupName = "sortDir";
			Controls.Add (sortAscRadioButton);
			sortDescRadioButton.Text = Res ("GroupSortDesc");
			sortDescRadioButton.GroupName = "sortDir";
			Controls.Add (sortDescRadioButton);
			Controls.Add (new LiteralControl ("</div><br/><div>" + Res ("GroupBy") + "<br/>"));
			Controls.Add (groupDropDownList);
			Controls.Add (new LiteralControl ("</div><div>"));
			groupAscRadioButton.Text = Res ("GroupSortAsc");
			groupAscRadioButton.GroupName = "groupDir";
			Controls.Add (groupAscRadioButton);
			groupDescRadioButton.Text = Res ("GroupSortDesc");
			groupDescRadioButton.GroupName = "groupDir";
			Controls.Add (groupDescRadioButton);
			Controls.Add (new LiteralControl ("</div><div>"));
			groupByCountsCheckBox.Text = Res ("GroupByCounts");
			Controls.Add (groupByCountsCheckBox);
			Controls.Add (new LiteralControl ("</div><div>"));
			groupShowCountsCheckBox.Text = Res ("GroupShowCounts");
			Controls.Add (groupShowCountsCheckBox);
			Controls.Add (new LiteralControl ("</div><div>"));
			groupIntCheckBox.Text = Res ("GroupInteractive");
			Controls.Add (groupIntCheckBox);
			Controls.Add (new LiteralControl ("</div><div>"));
			groupIntDirCheckBox.Text = Res ("GroupInteractiveDir");
			Controls.Add (groupIntDirCheckBox);
			Controls.Add (new LiteralControl ("</div>"));
			Controls.Add (new LiteralControl (SUFFIX_CONTROL));
			Controls.Add (new LiteralControl (string.Format (PREFIX_CONTROL_HLP, Res ("T_Prop4"), Res ("D_Prop4"), pageSizeTextBox.ClientID, ProductPage.AssemblyName, "user_profiles_web_part_paging")));
			Controls.Add (pageSizeTextBox);
			Controls.Add (new LiteralControl ("<br/><br/><div>" + Res ("PageMode") + "</div>"));
			for (int i = 0; i < 4; i++)
				pageDropDownList.Items.Add (new ListItem (Res ("PageMode_" + i), i.ToString ()));
			Controls.Add (pageDropDownList);
			Controls.Add (new LiteralControl ("<br/><br/><div>" + Res ("StepMode") + "</div>"));
			for (int i = 0; i < 3; i++)
				pageStepDropDownList.Items.Add (new ListItem (Res ("StepMode_" + i), i.ToString ()));
			Controls.Add (pageStepDropDownList);
			Controls.Add (new LiteralControl ("<br/><br/><div>" + Res ("SkipMode") + "</div>"));
			for (int i = 0; i < 3; i++)
				pageSkipDropDownList.Items.Add (new ListItem (Res ("StepMode_" + i), i.ToString ()));
			Controls.Add (pageSkipDropDownList);
			Controls.Add (new LiteralControl (SUFFIX_CONTROL));
			Controls.Add (new LiteralControl (string.Format (PREFIX_CONTROL_HLP, Res ("Misc"), Res ("ImageHeight" + (RollupWebPart.IsRollupZen ? "R" : string.Empty)), string.Empty, ProductPage.AssemblyName, "user_profiles_web_part_misc")));
			Controls.Add (imageHeightTextBox);
			Controls.Add (new LiteralControl ("<br/><br/><div>" + Res ("PrintAction") + "<br/>"));
			printDropDownList.AutoPostBack = false;
			printDropDownList.CssClass = "ms-input";
			Controls.Add (printDropDownList);
			Controls.Add (new LiteralControl ("<br/><br/><div>" + Res ("ExportAction") + "<br/>"));
			expDropDownList.AutoPostBack = false;
			expDropDownList.CssClass = "ms-input";
			Controls.Add (expDropDownList);
			Controls.Add (new LiteralControl ("</div><br/><div>" + Res ("Anim") + "<br/>"));
			animDropDownList.AutoPostBack = false;
			animDropDownList.CssClass = "ms-input";
			foreach (string s in new string [] { "b", "k", "l" })
				animDropDownList.Items.Add (new ListItem (Res ("Anim_" + s), s));
			Controls.Add (animDropDownList);
			Controls.Add (new LiteralControl ("</div><br/><div>" + ProductPage.GetResource ("Old_ShowPictures") + "<br/>"));
			pictDropDownList.AutoPostBack = false;
			pictDropDownList.CssClass = "ms-input";
			for (int i = 0; i < 3; i++)
				pictDropDownList.Items.Add (new ListItem (Res ("NameMode_" + i), i.ToString ()));
			Controls.Add (pictDropDownList);
			Controls.Add (new LiteralControl ("</div><br/><div>" + Res ("NameMode") + "<br/>"));
			nameDropDownList.AutoPostBack = false;
			nameDropDownList.CssClass = "ms-input";
			for (int i = 0; i < 3; i++)
				nameDropDownList.Items.Add (new ListItem (Res ("NameMode_" + i), i.ToString ()));
			Controls.Add (nameDropDownList);
#if PEOPLEZEN
			Controls.Add (new LiteralControl ("</div><br/><div>"));
			presenceCheckBox.Text = ProductPage.GetProductResource ("Presence");
			Controls.Add (presenceCheckBox);
			Controls.Add (new LiteralControl ("</div><br/><div>"));
			vcardCheckBox.Text = ProductPage.GetProductResource ("Vcard");
			Controls.Add (vcardCheckBox);
#endif
			Controls.Add (new LiteralControl ("</div><br/><div>"));
			urlSettingsCheckBox.Text = ProductPage.GetResource ("UrlSettings");
			Controls.Add (urlSettingsCheckBox);
			Controls.Add (new LiteralControl ("</div><br/><div>" + ProductPage.GetResource ("Jquery") + "<br/>"));
			jqueryDropDownList.AutoPostBack = false;
			jqueryDropDownList.CssClass = "ms-input";
			for (int i = 0; i < 3; i++)
				jqueryDropDownList.Items.Add (new ListItem (ProductPage.GetResource ("Jquery_" + i), i.ToString ()));
			Controls.Add (jqueryDropDownList);
			Controls.Add (new LiteralControl ("</div>"));
			Controls.Add (new LiteralControl (SUFFIX_CONTROL));
			if (propsTextBox.Enabled = (webPart != null)) {
				jqueryDropDownList.Enabled = animDropDownList.Enabled = pageSizeTextBox.Enabled = propsTextBox.Enabled = rowSizeTextBox.Enabled = tileTextBox.Enabled = webPart.LicEd (0);
				groupByCountsCheckBox.Enabled = groupShowCountsCheckBox.Enabled = groupIntCheckBox.Enabled = tabCheckBox.Enabled = groupIntDirCheckBox.Enabled = webPart.LicEd (4);
				groupAscRadioButton.Enabled = groupDescRadioButton.Enabled = webPart.LicEd (2);
				groupIntCheckBox.Checked = webPart.groupInteractive;
				tabCheckBox.Checked = webPart.tabInteractive;
				groupIntDirCheckBox.Checked = webPart.groupInteractiveDir;
				groupShowCountsCheckBox.Checked = webPart.groupShowCounts;
				groupByCountsCheckBox.Checked = webPart.groupByCounts;
#if PEOPLEZEN
				curUserCheckBox.Checked = webPart.curUser;
#endif
				groupAscRadioButton.Checked = (!(groupDescRadioButton.Checked = webPart.groupDesc && groupDescRadioButton.Enabled)) && groupAscRadioButton.Enabled;
				sortAscRadioButton.Enabled = sortDescRadioButton.Enabled = webPart.LicEd (2);
				sortAscRadioButton.Checked = (!(sortDescRadioButton.Checked = webPart.sortDesc && sortDescRadioButton.Enabled)) && sortAscRadioButton.Enabled;
				imageHeightTextBox.Text = webPart.imageHeight.ToString ();
				jqueryDropDownList.SelectedIndex = webPart.JQuery;
				animDropDownList.SelectedValue = webPart.loaderAnim;
				pageSizeTextBox.Enabled = nameDropDownList.Enabled = pictDropDownList.Enabled = webPart.LicEd (2);
				pageSkipDropDownList.Enabled = pageStepDropDownList.Enabled = pageDropDownList.Enabled = webPart.LicEd (4);
				nameDropDownList.SelectedIndex = webPart.LicEd (2) ? webPart.nameMode : webPart.NameMode;
				pictDropDownList.SelectedIndex = webPart.LicEd (2) ? webPart.pictMode : webPart.PictMode;
				urlSettingsCheckBox.Checked = (urlSettingsCheckBox.Enabled = webPart.LicEd (4)) && webPart.UrlSettings;
#if PEOPLEZEN
				presenceCheckBox.Checked = (presenceCheckBox.Enabled = webPart.LicEd (2)) && webPart.presence;
				vcardCheckBox.Checked = (vcardCheckBox.Enabled = webPart.LicEd (4)) && webPart.vcard;
#endif
				rowSizeTextBox.Text = webPart.rowSize.ToString ();
				tileTextBox.Text = webPart.TileWidth;
				sortCheckBox.Checked = (sortCheckBox.Enabled = webPart.LicEd (2)) && webPart.allowSort;
				viewCheckBox.Checked = (viewCheckBox.Enabled = webPart.LicEd (4)) && webPart.allowView;
				dateThisYearCheckBox.Checked = (dateThisYearCheckBox.Enabled = webPart.LicEd (2)) && webPart.dateThisYear;
				dateNoDayCheckBox.Checked = (dateNoDayCheckBox.Enabled = webPart.LicEd (2)) && webPart.dateIgnoreDay;
				filterLiveCheckBox.Checked = (filterLiveCheckBox.Enabled = webPart.LicEd (2)) && webPart.filterLive;
				styleClassicRadioButton.Checked = !(styleListRadioButton.Checked = (styleListRadioButton.Enabled = webPart.LicEd (2)) && webPart.listStyle);
				pageSizeTextBox.Text = webPart.LicEd (2) ? webPart.pageSize.ToString () : "4";
				propsTextBox.Text = webPart.Properties;
				pageDropDownList.SelectedIndex = webPart.LicEd (4) ? webPart.pageMode : webPart.PageMode;
				pageSkipDropDownList.SelectedIndex = webPart.LicEd (4) ? webPart.pageSkipMode : webPart.PageSkipMode;
				pageStepDropDownList.SelectedIndex = webPart.LicEd (4) ? webPart.pageStepMode : webPart.PageStepMode;
			}
		}

		internal void RefreshFieldDropDown (DropDownList dropDown, int le, RollupWebPart webPart, bool none, string propVal, string extraPropName, string extraPropTitle) {
			if (NeedsRefresh (dropDown, extraPropName)) {
				dropDown.Items.Clear ();
				dropDown.Items.Add (new ListItem (none ? ProductPage.GetResource ("None") : string.Empty, none ? string.Empty : string.Empty));
				if (!string.IsNullOrEmpty (extraPropName))
					dropDown.Items.Add (new ListItem (extraPropTitle, extraPropName));
				foreach (KeyValuePair<string, string> kvp in KnownProps)
					dropDown.Items.Add (new ListItem (kvp.Value, kvp.Key));
				if (string.IsNullOrEmpty (dropDown.SelectedValue))
					if (dropDown.Enabled = ((le <= 0) || webPart.LicEd (le)))
						try {
							dropDown.SelectedValue = propVal;
						} catch {
							dropDown.SelectedIndex = 0;
						} else
						dropDown.SelectedIndex = 0;
			}
		}

		protected override void OnLoad (EventArgs e) {
			RollupWebPart webPart = ParentToolPane.SelectedWebPart as RollupWebPart;
			EnsureChildControls ();
			base.OnLoad (e);
			expDropDownList.Enabled = printDropDownList.Enabled = ((webPart == null) || webPart.LicEd (2));
			if (dataSourceDropDownList.Items.Count == 0) {
				if ((webPart != null) && !string.IsNullOrEmpty (webPart.DataSourcePath)) {
					foreach (IDictionary inst in JsonSchemaManager.GetInstances (webPart.DataSourcePath, "DataSources", DataSource.SCHEMAPROP_ASMNAME))
						dataSourceDropDownList.Items.Add (new ListItem (JsonSchemaManager.GetDisplayName (inst, "DataSources", false) + " -- " + ProductPage.GetResource ("PC_DataSources_t_" + inst ["t"]), inst ["id"] + string.Empty));
					if (!string.IsNullOrEmpty (webPart.DataSourceID))
						try {
							dataSourceDropDownList.SelectedValue = webPart.DataSourceID;
						} catch {
						}
				}
			}
			RefreshFieldDropDown (groupDropDownList, 2, webPart, true, webPart.groupProp, null, null);
			RefreshFieldDropDown (tabDropDownList, 2, webPart, true, webPart.tabProp, null, null);
			RefreshFieldDropDown (sortDropDownList, 2, webPart, true, webPart.sortProp, "___roxRandomizedSort", ProductPage.GetResource ("Disp___roxRandomizedSort"));
			RefreshFieldDropDown (propDropDownList, 0, webPart, false, string.Empty, null, null);
			if (printDropDownList.Items.Count == 0) {
				printDropDownList.Items.Add (new ListItem (Res ("Anim_b"), string.Empty));
				if ((webPart != null) && printDropDownList.Enabled && !string.IsNullOrEmpty (webPart.PzPath)) {
					foreach (IDictionary inst in JsonSchemaManager.GetInstances (webPart.PzPath, "PrintActions", "roxority_PrintZen"))
						if (!"n".Equals (inst ["mpz"]))
							printDropDownList.Items.Add (new ListItem (JsonSchemaManager.GetDisplayName (inst, "PrintActions", false), inst ["id"] + string.Empty));
					if (!string.IsNullOrEmpty (webPart.PrintAction))
						try {
							printDropDownList.SelectedValue = webPart.PrintAction;
						} catch {
							printDropDownList.SelectedIndex = 0;
						}
				}
			}
			if (expDropDownList.Items.Count == 0) {
				expDropDownList.Items.Add (new ListItem (Res ("Anim_b"), string.Empty));
				if ((webPart != null) && expDropDownList.Enabled && !string.IsNullOrEmpty (webPart.EzPath)) {
					foreach (IDictionary inst in JsonSchemaManager.GetInstances (webPart.EzPath, "ExportActions", "roxority_ExportZen"))
						expDropDownList.Items.Add (new ListItem (JsonSchemaManager.GetDisplayName (inst, "ExportActions", false), inst ["id"] + string.Empty));
					if (!string.IsNullOrEmpty (webPart.ExportAction))
						try {
							expDropDownList.SelectedValue = webPart.ExportAction;
						} catch {
							expDropDownList.SelectedIndex = 0;
						}
				}
			}
			if ((webPart != null) && ((propsTextBox.Rows = webPart.Properties.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length + 1) < 3))
				propsTextBox.Rows = 3;
			dataSourceDropDownList.Attributes ["onchange"] = "alert('" + Res ("DataSourceChange") + "');";
		}

		protected override void RenderToolPart (HtmlTextWriter output) {
			if (!ProductPage.isEnabled) {
				using (SPSite adminSite = ProductPage.GetAdminSite ())
					output.Write ("<div class=\"rox-error\">" + ProductPage.GetResource ("NotEnabled", ProductPage.MergeUrlPaths (adminSite.Url, "/_layouts/" + ProductPage.AssemblyName + "/default.aspx?cfg=enable&r=" + rnd.Next ()), ProductPage.GetTitle ()) + "</div>");
				return;
			}
			output.Write ("<div class=\"rox-toolpart\">");
			base.RenderToolPart (output);
			output.Write ("</div>");
		}

		public override void ApplyChanges () {
			int rowSize;
			RollupWebPart webPart = ParentToolPane.SelectedWebPart as RollupWebPart;
			if (webPart != null) {
				webPart.forceReload = true;
				webPart.textArea.Text = string.Empty;
				if (int.TryParse (rowSizeTextBox.Text.Trim (), out rowSize))
					webPart.RowSize = rowSize;
				else
					webPart.RowSize = 2;
				webPart.GroupDesc = groupDescRadioButton.Checked;
#if PEOPLEZEN
				webPart.CurUser = curUserCheckBox.Checked;
#endif
				webPart.GroupByCounts = groupByCountsCheckBox.Checked;
				webPart.GroupShowCounts = groupShowCountsCheckBox.Checked;
				webPart.GroupInteractive = groupIntCheckBox.Checked;
				webPart.TabInteractive = tabCheckBox.Checked;
				webPart.GroupInteractiveDir = groupIntDirCheckBox.Checked;
				webPart.GroupProp = SELEMPTY.Equals (groupDropDownList.SelectedValue) ? string.Empty : groupDropDownList.SelectedValue;
				webPart.TabProp = SELEMPTY.Equals (tabDropDownList.SelectedValue) ? string.Empty : tabDropDownList.SelectedValue;
				webPart.SortDesc = sortDescRadioButton.Checked;
				webPart.SortProp = SELEMPTY.Equals (sortDropDownList.SelectedValue) ? string.Empty : sortDropDownList.SelectedValue;
				webPart.TileWidth = tileTextBox.Text;
				webPart.message = string.Empty;
				webPart.JQuery = jqueryDropDownList.SelectedIndex;
				webPart.DateThisYear = dateThisYearCheckBox.Checked;
				webPart.DateIgnoreDay = dateNoDayCheckBox.Checked;
				webPart.filterLive = filterLiveCheckBox.Enabled && filterLiveCheckBox.Checked;
				if (int.TryParse (pageSizeTextBox.Text, out rowSize))
					webPart.PageSize = rowSize;
				webPart.ImageHeight = (int.TryParse (imageHeightTextBox.Text, out rowSize)) ? rowSize : 0;
				webPart.Properties = propsTextBox.Text;
				webPart.PageMode = pageDropDownList.SelectedIndex;
				webPart.PageSkipMode = pageSkipDropDownList.SelectedIndex;
				webPart.PageStepMode = pageStepDropDownList.SelectedIndex;
				webPart.LoaderAnim = animDropDownList.SelectedValue;
				webPart.NameMode = nameDropDownList.SelectedIndex;
				webPart.PictMode = pictDropDownList.SelectedIndex;
				webPart.ListStyle = styleListRadioButton.Checked;
				webPart.AllowSort = sortCheckBox.Checked;
				webPart.AllowView = viewCheckBox.Checked;
				webPart.UrlSettings = urlSettingsCheckBox.Checked;
#if PEOPLEZEN
				webPart.Presence = presenceCheckBox.Checked;
				webPart.Vcard = vcardCheckBox.Checked;
#endif
				webPart.PrintAction = printDropDownList.SelectedValue + string.Empty;
				webPart.DataSourceID = dataSourceDropDownList.SelectedValue + string.Empty;
				webPart.ExportAction = expDropDownList.SelectedValue + string.Empty;

			}
			base.ApplyChanges ();
		}

		internal SortedDictionary<string, string> KnownProps {
			get {
				DataSource ds;
				if (knownProps == null)
					knownProps = new SortedDictionary<string, string> ();
				try {
					if (((knownProps.Count == 0) || (dataSourceDropDownList.SelectedValue != knownPropID)) && ((ds = DataSource.FromID (dataSourceDropDownList.SelectedValue, true, true, null)) != null)) {
						knownProps.Clear ();
						knownPropID = dataSourceDropDownList.SelectedValue;
						foreach (RecordProperty rp in ds.Properties)
							knownProps [rp.Name] = rp.DisplayName;
					}
				} catch {
				}
				return knownProps;
			}
		}

	}

}
