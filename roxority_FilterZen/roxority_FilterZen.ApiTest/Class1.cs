[ToolboxData("<{0}:ComboListFilter runat=server></{0}:ComboListFilter>"), XmlRoot(Namespace="http://www.KWizCom.com/ComboListFilter")]
public class ComboListFilter : WebPartBase, IFilterProvider, IWebPartTable_TableViewer
{
    // Fields
    private AdvancedOptionsToolPart _AdvancedOptionsToolPart;
    private bool _clearFilterClicked;
    private List<string> _ColumnsToHide;
    public bool _connected;
    public ArrayList _ConnectedWebParts;
    private string _connectedWebPartTitle;
    private Web _CurrentWeb;
    public bool _dataformwebpart;
    private DropDownFilterControl _DropDownFilterControl;
    private string _filterExpression;
    private bool _noFilterClicked;
    private bool _registrationErrorOccurred;
    private bool _setFilterClicked;
    public bool _usequery;
    private ComboListFilterProperties _WProperties;
    private string cancelButtonText;
    private string checkAllText;
    private string clearButtonText;
    private string connectedWebPart;
    private int controlsInRow;
    private int controlWidth;
    private string cssFile;
    public DataTableExternalFilterDelegate DataTableExternalFilter;
    private const string defaulCancelButtonText = "res:DropDown_CancelButton_Caption";
    private const string defaulCheckAllText = "res:DropDown_SelectAll_Caption";
    private const string defaulClearButtonText = "res:WebPart_ClearFilter_Caption";
    private const string defaulConnectedWebPart = "";
    private const int defaulcontrolsInRow = 0;
    private const int defaulControlWidth = 0;
    private const string defaulOkButtonText = "res:DropDown_OkButton_Caption";
    private const bool defaulShowCheckAll = true;
    private const bool defaulShowClearButtonForEachField = true;
    private const bool defaulShowOkCancelButtons = true;
    private const bool defaulShowSubmitClearButtons = false;
    private const string defaulSubmitButtonText = "res:WebPart_ApplyFilter_Caption";
    private const string defaultCSS_File = "";
    private const DropDownFilterControl.DropDownCaptionPositions defaultDropDownCaptionPosition = DropDownFilterControl.DropDownCaptionPositions.Left;
    private const string defaultDropDownClass = "ms-RadioText";
    private const DropDownFilterControl.DropDownSkins defaultDropDownSkin = DropDownFilterControl.DropDownSkins.Default;
    private const string defaultemptyRowCaption = "res:DropDown_EmptyRow_Caption";
    private const string defaultFieldCaptionClass = "ms-vb";
    private const bool defaultOverwriteOriginalFilter = false;
    private const bool defaultShowAllWhenNotFiltered = true;
    private const string defaultTemplateUrl = "/_layouts/KWizCom_DropDownFilter/DropDownFilter.ascx";
    private const bool defaultUseCookieToSaveTheOldFilterChoice = true;
    public Dictionary<string, string> DefaultValues;
    private const bool defaulUseConnectedListAsDataSource = true;
    private DropDownFilterControl.DropDownCaptionPositions dropDownCaptionPosition;
    private string dropDownClass;
    private DropDownFilterControl.DropDownSkins dropDownSkin;
    private string emptyRowCaption;
    protected ArrayList ErrorsCollection;
    private string fieldCaptionClass;
    private string FieldLabel;
    private string fieldXML;
    private string okButtonText;
    private bool overwriteOriginalFilter;
    private bool showAllWhenNotFiltered;
    private bool showCheckAll;
    private bool showClearButtonForEachField;
    private bool showOkCancelButtons;
    private bool showSubmitClearButtons;
    private string siteList;
    private string siteURL;
    private string sourceWebPart;
    private string submitButtonText;
    private string targetList;
    private string templateUrl;
    private bool useConnectedListAsDataSource;
    private bool useCookieToSaveTheOldFilterChoice;
    private string ValueLabel;
    private ITableViewerProperties ViewerProperties;

    // Events
    public event ClearFilterEventHandler ClearFilter;

    public event NoFilterEventHandler NoFilter;

    public event SetFilterEventHandler SetFilter;

    // Methods
    public ComboListFilter() : base("KWizCom.SharePoint.WebParts.ComboListFilter.Strings", Assembly.GetExecutingAssembly())
    {
        this.ErrorsCollection = new ArrayList();
        this._ConnectedWebParts = new ArrayList();
        this.targetList = string.Empty;
        this.sourceWebPart = string.Empty;
        this.useConnectedListAsDataSource = true;
        this.connectedWebPart = "";
        this.useCookieToSaveTheOldFilterChoice = true;
        this.emptyRowCaption = "res:DropDown_EmptyRow_Caption";
        this.showAllWhenNotFiltered = true;
        this.submitButtonText = "res:WebPart_ApplyFilter_Caption";
        this.showClearButtonForEachField = true;
        this.clearButtonText = "res:WebPart_ClearFilter_Caption";
        this.showCheckAll = true;
        this.checkAllText = "res:DropDown_SelectAll_Caption";
        this.showOkCancelButtons = true;
        this.okButtonText = "res:DropDown_OkButton_Caption";
        this.cancelButtonText = "res:DropDown_CancelButton_Caption";
        this.templateUrl = "/_layouts/KWizCom_DropDownFilter/DropDownFilter.ascx";
        this.cssFile = "";
        this.fieldCaptionClass = "ms-vb";
        this.dropDownCaptionPosition = DropDownFilterControl.DropDownCaptionPositions.Left;
        this.dropDownSkin = DropDownFilterControl.DropDownSkins.Default;
        this._connectedWebPartTitle = string.Empty;
        this._filterExpression = string.Empty;
        this.FieldLabel = "FilterField";
        this.ValueLabel = "FilterValue";
        this._ColumnsToHide = new List<string>();
        base._Constants = new Constants();
        base.Init += new EventHandler(this.ComboListFilter_Init);
        base.PreRender += new EventHandler(this.ComboListFilter_PreRender);
    }

    public void AddError(string errorMsg)
    {
        if (errorMsg != null)
        {
            base.AddError(new Exception(errorMsg));
        }
    }

    public void AddErrorEx(Exception errorMsg)
    {
        if (errorMsg != null)
        {
            base.AddError(errorMsg);
        }
    }

    private string BuildFullQuery(string listId, string listViewXml, bool isRetrieveWhereXml, bool useSelectedValues, out string whereXml, bool isForDropDowns)
    {
        whereXml = string.Empty;
        if (this._CurrentWeb == null)
        {
            return listViewXml;
        }
        if ((this._DropDownFilterControl.FilterFieldNames == null) || (this._DropDownFilterControl.FilterFieldNames.Length == 0))
        {
            return listViewXml;
        }
        if ((this._DropDownFilterControl.FilterValues == null) || (this._DropDownFilterControl.FilterValues.Length == 0))
        {
            return listViewXml;
        }
        string queryXML = this.GetQueryXML(listId, useSelectedValues);
        if (string.IsNullOrEmpty(queryXML) && !isRetrieveWhereXml)
        {
            return listViewXml;
        }
        XmlDocument document = new XmlDocument();
        document.LoadXml(listViewXml);
        XmlNode newChild = null;
        XmlNode node2 = null;
        if (!string.IsNullOrEmpty(queryXML))
        {
            this._setFilterClicked = true;
            newChild = document.SelectSingleNode(".//Query");
            if (newChild == null)
            {
                XmlNode node3 = document.SelectSingleNode(".//View");
                if (node3 == null)
                {
                    return listViewXml;
                }
                newChild = document.CreateElement("Query");
                node3.AppendChild(newChild);
            }
            node2 = newChild.SelectSingleNode("Where");
            if (node2 == null)
            {
                node2 = document.CreateElement("Where");
                newChild.AppendChild(node2);
            }
            else
            {
                whereXml = node2.InnerXml;
            }
            if (string.IsNullOrEmpty(whereXml) || this.OverwriteOriginalFilter)
            {
                whereXml = queryXML;
            }
            else
            {
                whereXml = "<And>" + whereXml + queryXML + "</And>";
            }
            node2.InnerXml = whereXml;
        }
        else
        {
            newChild = document.SelectSingleNode(".//Query");
            if (newChild != null)
            {
                node2 = newChild.SelectSingleNode("Where");
            }
            if (isForDropDowns && this.OverwriteOriginalFilter)
            {
                node2 = null;
            }
        }
        if ((node2 == null) || string.IsNullOrEmpty(node2.InnerXml))
        {
            whereXml = string.Empty;
            return listViewXml;
        }
        whereXml = node2.OuterXml;
        return document.OuterXml;
    }

    private string BuildQueryFromValue(List<string> values, SPFieldType fldType, string fldInternalName, SPField fld, bool isExcluded)
    {
        string str = string.Empty;
        string str2 = "Or";
        for (int i = 0; i < values.Count; i++)
        {
            DateTime time2;
            StringBuilder builder;
            string s = values[i];
            if ((s == null) || (s.Trim() == ""))
            {
                continue;
            }
            string localName = isExcluded ? "Neq" : "Eq";
            string str5 = "Text";
            string text = s;
            switch (fldType)
            {
                case SPFieldType.Note:
                    str5 = "Note";
                    if (!s.StartsWith("*"))
                    {
                        break;
                    }
                    localName = "Contains";
                    goto Label_00E8;

                case SPFieldType.DateTime:
                    DateTime time;
                    if (values.Count != 1)
                    {
                        goto Label_01D2;
                    }
                    str5 = "DateTime";
                    if (DateTime.TryParse(s, out time))
                    {
                        text = time.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    }
                    goto Label_039E;

                case SPFieldType.Counter:
                    str5 = "Counter";
                    goto Label_039E;

                case SPFieldType.Lookup:
                    str5 = "Text";
                    goto Label_039E;

                case SPFieldType.Boolean:
                    str5 = "Boolean";
                    text = "1";
                    if (!(s.ToLower() == Strings.DropDown_Bool_Yes.ToLower()))
                    {
                        goto Label_017F;
                    }
                    localName = isExcluded ? "Neq" : "Eq";
                    goto Label_039E;

                case SPFieldType.Number:
                    str5 = "Number";
                    goto Label_039E;

                case SPFieldType.Currency:
                    str5 = "Currency";
                    goto Label_039E;

                case SPFieldType.Calculated:
                {
                    text = s;
                    int index = text.IndexOf(";#");
                    string str7 = "string";
                    if (index > 0)
                    {
                        text = s.Substring(index + 2);
                        str7 = s.Substring(0, index);
                    }
                    string str9 = str7;
                    if (str9 == null)
                    {
                        goto Label_02D8;
                    }
                    if (!(str9 == "string"))
                    {
                        if (str9 == "float")
                        {
                            goto Label_02B4;
                        }
                        if (str9 == "datetime")
                        {
                            goto Label_02C0;
                        }
                        if (str9 == "boolean")
                        {
                            goto Label_02CC;
                        }
                        goto Label_02D8;
                    }
                    str5 = "Text";
                    goto Label_039E;
                }
                case SPFieldType.Attachments:
                    str5 = "Attachments";
                    goto Label_039E;

                case SPFieldType.User:
                    str5 = "User";
                    text = KWizCom.SharePoint.Utilities.Utilities.GetUserNameWithTokens(text.Trim());
                    goto Label_039E;

                case SPFieldType.ModStat:
                    str5 = "ModStat";
                    goto Label_039E;

                default:
                    text = s;
                    if ((fld != null) && ((fld.TypeAsString == "Tag") || fld.ParentList.RootFolder.Url.ToLower().Contains("/kwizcomtags")))
                    {
                        text = text.Trim(new char[] { '*' });
                        text = "*" + text;
                    }
                    if (text.StartsWith("*"))
                    {
                        localName = "Contains";
                    }
                    else if (text.EndsWith("*"))
                    {
                        localName = "BeginsWith";
                    }
                    text = s.Trim(new char[] { '*' });
                    str5 = "Text";
                    goto Label_039E;
            }
            if (s.EndsWith("*"))
            {
                localName = "BeginsWith";
            }
        Label_00E8:;
            text = s.Trim(new char[] { '*' });
            goto Label_039E;
        Label_017F:
            localName = isExcluded ? "Eq" : "Neq";
            goto Label_039E;
        Label_01D2:
            str2 = "And";
            if (isExcluded)
            {
                localName = (i == 0) ? "Leq" : "Geq";
            }
            else
            {
                localName = (i == 0) ? "Geq" : "Leq";
            }
            str5 = "DateTime";
            if (DateTime.TryParse(s, out time2))
            {
                text = time2.ToString("yyyy-MM-ddTHH:mm:ssZ");
            }
            goto Label_039E;
        Label_02B4:
            str5 = "Number";
            goto Label_039E;
        Label_02C0:
            str5 = "DateTime";
            goto Label_039E;
        Label_02CC:
            str5 = "Boolean";
            goto Label_039E;
        Label_02D8:
            str5 = "Text";
        Label_039E:
            builder = new StringBuilder();
            StringWriter w = new StringWriter(builder);
            XmlTextWriter writer2 = new XmlTextWriter(w);
            writer2.WriteStartElement(localName);
            writer2.WriteStartElement("FieldRef");
            writer2.WriteAttributeString("Name", fldInternalName);
            writer2.WriteEndElement();
            writer2.WriteStartElement("Value");
            writer2.WriteAttributeString("Type", str5);
            writer2.WriteString(text);
            writer2.WriteEndElement();
            writer2.WriteEndElement();
            string str8 = builder.ToString();
            if (string.IsNullOrEmpty(str))
            {
                str = str8;
            }
            else
            {
                str = ("<" + str2 + ">") + str + str8 + ("</" + str2 + ">");
            }
        }
        return str;
    }

    public override ConnectionRunAt CanRunAt()
    {
        return ConnectionRunAt.Server;
    }

    private void ComboListFilter_Init(object sender, EventArgs e)
    {
        this._CurrentWeb = new Web(this.Context);
    }

    private void ComboListFilter_PreRender(object sender, EventArgs e)
    {
        base.RegisterClientJScriptFile("/_layouts/KWizCom_Files/KWizCom_JSutility.js");
        base.RegisterClientJScriptFile("/_layouts/KWizCom_DropDownFilter/KWizCom_DropDownFilter.js");
        if (!string.IsNullOrEmpty(this.CSS_File))
        {
            base.RegisterClientStyleSheetFile(this.CSS_File);
        }
    }

    protected override void CreateChildControls()
    {
        this._DropDownFilterControl = (DropDownFilterControl) this.Page.LoadControl(base.ReplaceTokens(this.TemplateUrl));
        if (base.DesignMode || base.BrowserDesignMode)
        {
            this._DropDownFilterControl.EnableViewState = false;
        }
        this._DropDownFilterControl.Qualifier = base.Qualifier;
        this._DropDownFilterControl.FieldXML = this.FieldXML;
        this._DropDownFilterControl.DefaultValues = this.DefaultValues;
        this._DropDownFilterControl.EmptyRowCaption = this.EmptyRowCaptionFromResource;
        this._DropDownFilterControl.DropDownSkin = this.DropDownSkin;
        this._DropDownFilterControl.ControlWidth = this.ControlWidth;
        this._DropDownFilterControl.ControlsInRow = this.ControlsInRow;
        this._DropDownFilterControl.DropDownCaptionPosition = this.DropDownCaptionPosition;
        this._DropDownFilterControl.ShowSubmitClearButtons = this.ShowSubmitClearButtons;
        this._DropDownFilterControl.SubmitButtonText = this.SubmitButtonTextFromResource;
        this._DropDownFilterControl.ClearButtonText = this.ClearButtonTextFromResource;
        this._DropDownFilterControl.ShowCheckAll = this.ShowCheckAll;
        this._DropDownFilterControl.CheckAllText = this.CheckAllTextFromResource;
        this._DropDownFilterControl.ShowOkCancelButtons = this.ShowOkCancelButtons;
        this._DropDownFilterControl.OkButtonText = this.OkButtonTextFromResource;
        this._DropDownFilterControl.CancelButtonText = this.CancelButtonTextFromResource;
        this._DropDownFilterControl.CurrentWeb = this._CurrentWeb;
        this._DropDownFilterControl.ParentWebPart = this;
        this._DropDownFilterControl.ShowClearButtonForEachField = this.ShowClearButtonForEachField;
        this._DropDownFilterControl.UseCookieToSaveTheOldFilterChoice = this.UseCookieToSaveTheOldFilterChoice;
        this.Controls.Add(this._DropDownFilterControl);
        this._DropDownFilterControl.EnsureCreated();
    }

    public override void EnsureInterfaces()
    {
        try
        {
            base.RegisterInterface("ComboListFilter", "IFilterProvider", -1, ConnectionRunAt.Server, this, "", "Provide Filter To", "Provides a Filter to a consumer Web Part.", true);
        }
        catch (SecurityException)
        {
            this._registrationErrorOccurred = true;
        }
        catch
        {
            this._registrationErrorOccurred = true;
        }
    }

    public DataTable ExecuteQuery()
    {
        using (Web web = new Web(this.Context, this.WProperties.WebUrl))
        {
            DataTable siteData = null;
            SPSiteDataQuery sPSiteDataQuery = this.GetSPSiteDataQuery();
            if (sPSiteDataQuery != null)
            {
                siteData = web.WebSite.GetSiteData(sPSiteDataQuery);
            }
            else
            {
                siteData = new DataTable();
            }
            return siteData;
        }
    }

    private void FillControlsData()
    {
        if (!this._connected && (!this._DropDownFilterControl.DropDownsFilled || (this._DropDownFilterControl.ClearFilter == "true")))
        {
            this._DropDownFilterControl.FillValuesArray(this.ID);
            this._DropDownFilterControl.FillDropDownsData(this.ID, this.UseConnectedListAsDataSource, null, null);
        }
    }

    public void FilterConsumerInit(object sender, FilterConsumerInitEventArgs filterConsumerInitEventArgs)
    {
    }

    public void FindSourceWebPart(string webPartId, out string listName, out string listViewXml)
    {
        listName = "";
        listViewXml = "";
        if (!string.IsNullOrEmpty(webPartId))
        {
            foreach (WebPart part in base.WebPartManager.WebParts)
            {
                if (part.ID == webPartId)
                {
                    ListViewWebPart part2 = part as ListViewWebPart;
                    if (part2 != null)
                    {
                        listName = part2.ListName;
                        listViewXml = part2.ListViewXml;
                        break;
                    }
                    DataFormWebPart part3 = part as DataFormWebPart;
                    if (part3 != null)
                    {
                        SPDataSource dataSource = part3.DataSource as SPDataSource;
                        listName = dataSource.List.ID.ToString();
                        listViewXml = dataSource.SelectCommand;
                        break;
                    }
                }
            }
        }
    }

    [ConnectionProvider("DataTable", typeof(DataTableProviderConnectionPoint), AllowsMultipleConnections=true)]
    public IWebPartTable_TableViewer GetConnectionInterface()
    {
        return this;
    }

    public string GetEmptyDataSourceMessage()
    {
        return "Please select a filter to view items";
    }

    private string GetHtmlFromControl(Control htmlControl)
    {
        StringWriter writer = new StringWriter();
        HtmlTextWriter writer2 = new HtmlTextWriter(writer);
        htmlControl.RenderControl(writer2);
        return writer.ToString();
    }

    private string GetListsTypesToUse()
    {
        if (this.WProperties.ListsTypeToUse == ListsTypesToUse.Base_Type)
        {
            return ("<Lists MaxListLimit=\"0\" BaseType=\"" + ((int) this.WProperties.BaseType) + "\" />");
        }
        if (this.WProperties.ListsTypeToUse == ListsTypesToUse.Custom_List_Template)
        {
            return ("<Lists MaxListLimit=\"0\" ServerTemplate=\"" + this.WProperties.CustomListTemplateType + "\" />");
        }
        return ("<Lists MaxListLimit=\"0\" ServerTemplate=\"" + ((int) this.WProperties.ListTemplateType) + "\" />");
    }

    private string GetOnlyQuery()
    {
        string queryXML = this.GetQueryXML(null, true);
        if (!string.IsNullOrEmpty(queryXML))
        {
            return ("<Where>" + queryXML + "</Where>");
        }
        return "";
    }

    private string GetQueryScope()
    {
        switch (this.WProperties.QueryScope)
        {
            case QueryScopes.Entire_site_collection:
                return "<Webs Scope=\"SiteCollection\" />";

            case QueryScopes.Selected_site_and_subsites:
                return "<Webs Scope=\"Recursive\" />";
        }
        return "";
    }

    private string GetQueryXML(string listId, bool useSelectedValues)
    {
        List<string> list = new List<string>();
        SPList list2 = null;
        if (!string.IsNullOrEmpty(listId))
        {
            list2 = this._CurrentWeb.WebSite.Lists[new Guid(listId)];
        }
        if (useSelectedValues && (this._DropDownFilterControl.FilterFieldNames != null))
        {
            for (int i = 0; i < this._DropDownFilterControl.FilterFieldNames.Length; i++)
            {
                if (this._DropDownFilterControl.FilterValues[i] != null)
                {
                    SPField fld = null;
                    bool flag = false;
                    try
                    {
                        if (list2 != null)
                        {
                            fld = list2.Fields.GetFieldByInternalName(this._DropDownFilterControl.FilterFieldNames[i]);
                            flag = true;
                        }
                        else
                        {
                            fld = this._DropDownFilterControl.GetSPFieldByIndex(i);
                        }
                    }
                    catch
                    {
                        fld = null;
                    }
                    string sPFieldInternalNameByIndex = this._DropDownFilterControl.GetSPFieldInternalNameByIndex(i);
                    SPFieldType sPFieldTypeByIndex = this._DropDownFilterControl.GetSPFieldTypeByIndex(i);
                    bool isExcludedByIndex = this._DropDownFilterControl.GetIsExcludedByIndex(i);
                    if ((fld != null) && flag)
                    {
                        sPFieldTypeByIndex = fld.Type;
                        sPFieldInternalNameByIndex = fld.InternalName;
                    }
                    list.Add(this.BuildQueryFromValue(this._DropDownFilterControl.FilterValues[i], sPFieldTypeByIndex, sPFieldInternalNameByIndex, fld, isExcludedByIndex));
                }
            }
        }
        string str2 = string.Empty;
        foreach (string str3 in list)
        {
            if (!string.IsNullOrEmpty(str3))
            {
                if (string.IsNullOrEmpty(str2))
                {
                    str2 = str3;
                }
                else
                {
                    str2 = "<And>" + str2 + str3 + "</And>";
                }
            }
        }
        return str2;
    }

    private SPSiteDataQuery GetSPSiteDataQuery()
    {
        SPSiteDataQuery query = new SPSiteDataQuery();
        query.Query = this.GetOnlyQuery();
        if (string.IsNullOrEmpty(query.Query))
        {
            return null;
        }
        query.ViewFields = this.GetViewFields();
        query.Webs = this.GetQueryScope();
        query.Lists = this.GetListsTypesToUse();
        return query;
    }

    public override ToolPart[] GetToolParts()
    {
        try
        {
            new WPToolParts(this.WProperties, this);
            ToolPart[] partArray = base.RenderToolpartsAsPopup("Table Viewer Properties", "Table Viewer Properties");
            ToolPart[] partArray2 = new ToolPart[partArray.Length + 2];
            ListConnectionToolPart part = new ListConnectionToolPart();
            this._AdvancedOptionsToolPart = new AdvancedOptionsToolPart();
            partArray2[0] = part;
            partArray2[1] = this._AdvancedOptionsToolPart;
            for (int i = 0; i < partArray.Length; i++)
            {
                partArray2[i + 2] = partArray[i];
            }
            return partArray2;
        }
        catch (Exception exception)
        {
            base.AddError(exception);
        }
        return null;
    }

    private string GetUrlByDataRow(DataRow row)
    {
        string str4;
        Guid gWebId = new Guid(row["WebId"].ToString());
        Guid guid2 = new Guid(row["ListId"].ToString());
        int id = int.Parse(row["ID"].ToString());
        using (Web web = new Web(this.Context, this.WProperties.WebUrl))
        {
            using (SPWeb web2 = web.Site.OpenWeb(gWebId))
            {
                SPList list = web2.Lists[guid2];
                SPListItem itemById = list.GetItemById(id);
                if (this.WProperties.OpenDocumentInLibrary && (list.BaseType == SPBaseType.DocumentLibrary))
                {
                    return itemById["ServerUrl"].ToString();
                }
                if (this.WProperties.OpenListItemAlternateFieldname.Trim() != "")
                {
                    try
                    {
                        string str = "";
                        foreach (string str2 in this.WProperties.OpenListItemAlternateFieldname.Split(new char[] { '/' }))
                        {
                            if (str != "")
                            {
                                str = str + "/";
                            }
                            str = str + itemById[str2].ToString();
                        }
                        str = str.Replace("//", "/").Replace(":/", "://");
                        if (str.Trim() != "")
                        {
                            return str;
                        }
                    }
                    catch
                    {
                    }
                }
                string serverRelativeUrl = web2.ServerRelativeUrl;
                if (!serverRelativeUrl.EndsWith("/"))
                {
                    serverRelativeUrl = serverRelativeUrl + "/";
                }
                str4 = string.Concat(new object[] { serverRelativeUrl, list.Forms[PAGETYPE.PAGE_DISPLAYFORM].Url, "?ID=", id, "&Source=", this.Page.Request.Url.ToString() });
            }
        }
        return str4;
    }

    private string GetViewFields()
    {
        string str = "";
        foreach (ComboListFilterProperties.FieldSetting setting in this.WProperties.GetDisplayFieldSettingsDictionary().Values)
        {
            str = str + "<FieldRef Name=\"" + setting.FieldInternalName + "\" Nullable=\"TRUE\" />";
        }
        return str;
    }

    public bool IsDisplayColumn(string ColumnName)
    {
        return !this._ColumnsToHide.Contains(ColumnName);
    }

    private bool IsExpired(object o)
    {
        bool flag = false;
        if (DateTime.Today > Constants.ExiredDate)
        {
            flag = true;
        }
        return flag;
    }

    private bool IsWikiSearch()
    {
        if (SPContext.Current.Web.WebTemplateId != 0x12c00)
        {
            if ((SPContext.Current.List != null) && (SPContext.Current.List.BaseTemplate == ((SPListTemplateType) 0x9ca4)))
            {
                return true;
            }
            if (!(this.WProperties.CustomListTemplateType == "40100") && !(this.WProperties.CustomListTemplateType == "40102"))
            {
                return false;
            }
        }
        return true;
    }

    void IWebPartTable_TableViewer.DeleteRow(DataRow row)
    {
    }

    void IWebPartTable_TableViewer.EditRow(DataRow row)
    {
    }

    void IWebPartTable_TableViewer.GetDataTable(DataTableCallback callback)
    {
        this.EnsureChildControls();
        this.FillControlsData();
        DataTable dataTable = this.ExecuteQuery();
        if (dataTable != null)
        {
            this._ColumnsToHide = new List<string>();
            Dictionary<string, ComboListFilterProperties.FieldSetting> displayFieldSettingsDictionary = this.WProperties.GetDisplayFieldSettingsDictionary();
            for (int i = 0; i < dataTable.Columns.Count; i++)
            {
                try
                {
                    string columnName = dataTable.Columns[i].ColumnName;
                    if (displayFieldSettingsDictionary.ContainsKey(columnName))
                    {
                        dataTable.Columns[i].Caption = displayFieldSettingsDictionary[columnName].FieldCaption;
                    }
                    else
                    {
                        this._ColumnsToHide.Add(columnName);
                    }
                }
                catch
                {
                }
            }
            foreach (DataRow row in dataTable.Rows)
            {
                for (int j = 0; j < dataTable.Columns.Count; j++)
                {
                    string item = dataTable.Columns[j].ColumnName;
                    if ((this._ColumnsToHide.Contains(item) || (row[item] == null)) || string.IsNullOrEmpty(row[item].ToString()))
                    {
                        continue;
                    }
                    switch (displayFieldSettingsDictionary[item].FieldFormat)
                    {
                        case FieldFormatDataTypes.DateTime:
                        {
                            row[item] = DateTime.Parse(row[item].ToString()).ToString(SPContext.Current.Web.Locale.DateTimeFormat);
                            continue;
                        }
                        case FieldFormatDataTypes.Boolean:
                        {
                            if (!(row[item].ToString() == "1") && !(row[item].ToString().ToLower() == Strings.DropDown_Bool_Yes.ToLower()))
                            {
                                break;
                            }
                            row[item] = Strings.DropDown_Bool_Yes;
                            continue;
                        }
                        default:
                            goto Label_01C8;
                    }
                    row[item] = Strings.DropDown_Bool_No;
                    continue;
                Label_01C8:;
                    string str3 = row[item].ToString().Replace(";#", "; ").Trim(new char[] { ';', ' ' });
                    row[item] = str3;
                }
            }
            if (this.DataTableExternalFilter != null)
            {
                dataTable = this.DataTableExternalFilter(dataTable);
            }
            callback(dataTable);
        }
    }

    void IWebPartTable_TableViewer.GetViewerProperties(ref ITableViewerProperties ViewerProperties)
    {
        this.ViewerProperties = ViewerProperties;
    }

    void IWebPartTable_TableViewer.SelectRow(DataRow row)
    {
        string urlByDataRow = this.GetUrlByDataRow(row);
        if (!string.IsNullOrEmpty(urlByDataRow))
        {
            this.Page.Response.Redirect(urlByDataRow);
        }
    }

    protected override void OnPreRender(EventArgs e)
    {
        base.OnPreRender(e);
        this.FillControlsData();
    }

    public override void PartCommunicationConnect(string interfaceName, WebPart connectedPart, string connectedInterfaceName, ConnectionRunAt runAt)
    {
        if ((interfaceName == "ComboListFilter") && (connectedPart != null))
        {
            this._connected = true;
            this._connectedWebPartTitle = SPEncode.HtmlEncode(connectedPart.Title);
            this.EnsureChildControls();
            ListViewWebPart part = connectedPart as ListViewWebPart;
            if (part == null)
            {
                this._usequery = false;
            }
            else
            {
                this._usequery = true;
                this._ConnectedWebParts.Add(part);
                string listName = "";
                string listViewXml = "";
                this.FindSourceWebPart(this.SourceWebPart, out listName, out listViewXml);
                if (string.IsNullOrEmpty(listName))
                {
                    listName = part.ListName;
                    listViewXml = part.ListViewXml;
                }
                if (string.IsNullOrEmpty(this.TargetList))
                {
                    this.TargetList = part.ListName;
                }
                string whereXml = string.Empty;
                string str4 = string.Empty;
                if (!this._DropDownFilterControl.DropDownsFilled)
                {
                    this._DropDownFilterControl.FillValuesArray(this.ID);
                    this.BuildFullQuery(listName, listViewXml, this.UseConnectedListAsDataSource, true, out str4, true);
                    this._DropDownFilterControl.FillDropDownsData(this.ID, this.UseConnectedListAsDataSource, listName, str4);
                }
                part.ListViewXml = this.BuildFullQuery(part.ListName, part.ListViewXml, this.UseConnectedListAsDataSource, true, out whereXml, false);
            }
        }
        else
        {
            foreach (SPWebPartConnection connection in ((SPWebPartManager) base.WebPartManager).SPWebPartConnections)
            {
                if ((connection.Provider is KWizCom.SharePoint.WebParts.ComboListFilter.ComboListFilter) && (connection.Consumer is DataFormWebPart))
                {
                    KWizCom.SharePoint.WebParts.ComboListFilter.ComboListFilter provider = (KWizCom.SharePoint.WebParts.ComboListFilter.ComboListFilter) connection.Provider;
                    if (provider.ConnectionID == base.ConnectionID)
                    {
                        DataFormWebPart consumer = connection.Consumer as DataFormWebPart;
                        if (consumer.DataSource is SPDataSource)
                        {
                            this._ConnectedWebParts.Add(consumer);
                            this._connected = true;
                            this._dataformwebpart = true;
                            this._connectedWebPartTitle = SPEncode.HtmlEncode(consumer.Title);
                            this.EnsureChildControls();
                            this._usequery = true;
                            SPDataSource dataSource = consumer.DataSource as SPDataSource;
                            string str5 = "";
                            string selectCommand = "";
                            this.FindSourceWebPart(this.SourceWebPart, out str5, out selectCommand);
                            if (string.IsNullOrEmpty(str5))
                            {
                                str5 = dataSource.List.ID.ToString();
                                selectCommand = dataSource.SelectCommand;
                            }
                            if (string.IsNullOrEmpty(this.TargetList))
                            {
                                this.TargetList = str5;
                            }
                            string str7 = string.Empty;
                            string str8 = string.Empty;
                            if (!this._DropDownFilterControl.DropDownsFilled)
                            {
                                this._DropDownFilterControl.FillValuesArray(this.ID);
                                this.BuildFullQuery(str5, selectCommand, this.UseConnectedListAsDataSource, true, out str8, true);
                                this._DropDownFilterControl.FillDropDownsData(this.ID, this.UseConnectedListAsDataSource, str5, str8);
                            }
                            dataSource.SelectCommand = this.BuildFullQuery(str5, dataSource.SelectCommand, this.UseConnectedListAsDataSource, true, out str7, false);
                        }
                    }
                }
            }
            if (this._ConnectedWebParts.Count == 0)
            {
                this._usequery = false;
            }
        }
    }

    public override void PartCommunicationMain()
    {
        if (this._connected)
        {
            if (this._usequery)
            {
                if (this._setFilterClicked)
                {
                    if (this.NoFilter != null)
                    {
                        this.NoFilter(this, new EventArgs());
                    }
                    return;
                }
                if (!this.ShowAllWhenNotFiltered && !this._dataformwebpart)
                {
                    SetFilterEventArgs e = new SetFilterEventArgs();
                    e.FilterExpression = this.FieldLabel + "1=ID&" + this.ValueLabel + "1=0";
                    this.SetFilter(this, e);
                    return;
                }
            }
            if (this.NoFilter != null)
            {
                this.NoFilter(this, new EventArgs());
            }
        }
    }

    private string ReadResourcesString(string resID)
    {
        string str = base.LoadResource(resID.Replace("res:", ""));
        if (string.IsNullOrEmpty(str))
        {
            return resID;
        }
        return str;
    }

    internal string RemoveUrlKey(string url, string keyName)
    {
        if (url == null)
        {
            return null;
        }
        int index = url.IndexOf("&" + keyName + "=", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
        {
            index = url.IndexOf("?" + keyName + "=", StringComparison.OrdinalIgnoreCase);
        }
        if (index < 0)
        {
            return url;
        }
        int num2 = url.IndexOf('&', index + 1);
        if (num2 >= 0)
        {
            return url.Remove(index + 1, num2 - index);
        }
        return url.Substring(0, index);
    }

    private void RenderNotConnectedMsg(HtmlTextWriter output)
    {
        Table htmlControl = new Table();
        TableRow child = new TableRow();
        TableCell cell = new TableCell();
        cell.VerticalAlign = VerticalAlign.Top;
        Image image = new Image();
        image.ImageUrl = base.ReplaceTokens("_WPR_/FilterWarning.gif");
        image.AlternateText = "Warning";
        cell.Controls.Add(image);
        child.Controls.Add(cell);
        cell = new TableCell();
        cell.Width = Unit.Percentage(100.0);
        cell.Text = Constants.Message_WebPartDoesNotConnected;
        child.Controls.Add(cell);
        htmlControl.Controls.Add(child);
        output.Write(this.GetHtmlFromControl(htmlControl));
    }

    protected override void RenderWebPart(HtmlTextWriter output)
    {
        try
        {
            if (!this.IsWikiSearch() && this.IsExpired(output))
            {
                return;
            }
            this.EnsureChildControls();
            if (!this._connected && ((this._DropDownFilterControl.FilterFieldNames == null) || (this._DropDownFilterControl.FilterFieldNames.Length < 1)))
            {
                this.Controls.Clear();
                this.RenderNotConnectedMsg(output);
                return;
            }
            if (this.FieldXML.Length < 60)
            {
                LiteralControl htmlControl = new LiteralControl();
                htmlControl.Text = string.Format("<a href=\"javascript:MSOTlPn_ShowToolPane('1','{0}');\" target=\"_self\">Open the tool pane</a> to configure this filter.", this.ID);
                output.Write(this.GetHtmlFromControl(htmlControl));
                return;
            }
            this.RenderChildren(output);
        }
        catch (Exception exception)
        {
            this.Page.Trace.Write("Error", exception.ToString());
        }
        base.RenderErrorsAsList(output);
    }

    public bool ShouldSerializeWProperties()
    {
        return true;
    }

    // Properties
    [Browsable(false), DefaultValue("res:DropDown_CancelButton_Caption"), WebPartStorage(Storage.Personal)]
    public string CancelButtonText
    {
        get
        {
            return this.cancelButtonText;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "res:DropDown_CancelButton_Caption";
            }
            this.cancelButtonText = value;
        }
    }

    private string CancelButtonTextFromResource
    {
        get
        {
            return this.ReadResourcesString(this.CancelButtonText);
        }
    }

    [DefaultValue("res:DropDown_SelectAll_Caption"), Browsable(false), WebPartStorage(Storage.Personal)]
    public string CheckAllText
    {
        get
        {
            return this.checkAllText;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "res:DropDown_SelectAll_Caption";
            }
            this.checkAllText = value;
        }
    }

    private string CheckAllTextFromResource
    {
        get
        {
            return this.ReadResourcesString(this.CheckAllText);
        }
    }

    [DefaultValue("res:WebPart_ClearFilter_Caption"), Browsable(false), WebPartStorage(Storage.Personal)]
    public string ClearButtonText
    {
        get
        {
            return this.clearButtonText;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "res:WebPart_ClearFilter_Caption";
            }
            this.clearButtonText = value;
        }
    }

    private string ClearButtonTextFromResource
    {
        get
        {
            return this.ReadResourcesString(this.ClearButtonText);
        }
    }

    [Browsable(false), DefaultValue(""), WebPartStorage(Storage.Personal)]
    public string ConnectedWebPart
    {
        get
        {
            return this.connectedWebPart;
        }
        set
        {
            this.connectedWebPart = value;
        }
    }

    public bool ConnectionPointEnabled
    {
        get
        {
            return (!string.IsNullOrEmpty(this.WProperties.WebUrl) && (this.WProperties.GetDisplayFieldSettingsDictionary().Count > 0));
        }
    }

    [Browsable(false), WebPartStorage(Storage.Personal), DefaultValue(0)]
    public int ControlsInRow
    {
        get
        {
            return this.controlsInRow;
        }
        set
        {
            this.controlsInRow = value;
        }
    }

    [WebPartStorage(Storage.Personal), Browsable(false), DefaultValue(0)]
    public int ControlWidth
    {
        get
        {
            return this.controlWidth;
        }
        set
        {
            this.controlWidth = value;
        }
    }

    [Browsable(true), Description("url to an alternate CSS file."), Category("Advanced Display Options"), WebPartStorage(Storage.Personal), FriendlyName("CSS file")]
    public string CSS_File
    {
        get
        {
            return this.cssFile;
        }
        set
        {
            this.cssFile = value;
        }
    }

    [Category("Advanced Display Options"), DefaultValue(1), WebPartStorage(Storage.Personal), Browsable(true), FriendlyName("Control caption location")]
    public DropDownFilterControl.DropDownCaptionPositions DropDownCaptionPosition
    {
        get
        {
            return this.dropDownCaptionPosition;
        }
        set
        {
            this.dropDownCaptionPosition = value;
        }
    }

    [WebPartStorage(Storage.Personal), Category("Advanced Display Options"), FriendlyName("Drop down class"), DefaultValue("ms-RadioText"), Browsable(false)]
    public string DropDownClass
    {
        get
        {
            return this.dropDownClass;
        }
        set
        {
            this.dropDownClass = value;
        }
    }

    [WebPartStorage(Storage.Personal), FriendlyName("Drop down skin"), Category("Advanced Display Options"), DefaultValue(5), Browsable(true)]
    public DropDownFilterControl.DropDownSkins DropDownSkin
    {
        get
        {
            return this.dropDownSkin;
        }
        set
        {
            this.dropDownSkin = value;
        }
    }

    [WebPartStorage(Storage.Personal), DefaultValue("res:DropDown_EmptyRow_Caption"), Browsable(false)]
    public string EmptyRowCaption
    {
        get
        {
            return this.emptyRowCaption;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "res:DropDown_EmptyRow_Caption";
            }
            this.emptyRowCaption = value;
        }
    }

    private string EmptyRowCaptionFromResource
    {
        get
        {
            return this.ReadResourcesString(this.EmptyRowCaption);
        }
    }

    [DefaultValue("ms-vb"), Category("Advanced Display Options"), WebPartStorage(Storage.Personal), FriendlyName("Field Caption Class"), Browsable(true)]
    public string FieldCaptionClass
    {
        get
        {
            return this.fieldCaptionClass;
        }
        set
        {
            this.fieldCaptionClass = value;
        }
    }

    [Browsable(false)]
    public string FieldXML
    {
        get
        {
            return this.fieldXML;
        }
        set
        {
            this.fieldXML = value;
        }
    }

    [WebPartStorage(Storage.Personal), Browsable(false), DefaultValue("res:DropDown_OkButton_Caption")]
    public string OkButtonText
    {
        get
        {
            return this.okButtonText;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "res:DropDown_OkButton_Caption";
            }
            this.okButtonText = value;
        }
    }

    private string OkButtonTextFromResource
    {
        get
        {
            return this.ReadResourcesString(this.OkButtonText);
        }
    }

    [DefaultValue(false), WebPartStorage(Storage.Personal), Browsable(false)]
    public bool OverwriteOriginalFilter
    {
        get
        {
            return this.overwriteOriginalFilter;
        }
        set
        {
            this.overwriteOriginalFilter = value;
        }
    }

    [Browsable(false), DefaultValue(true), WebPartStorage(Storage.Personal)]
    public bool ShowAllWhenNotFiltered
    {
        get
        {
            return this.showAllWhenNotFiltered;
        }
        set
        {
            this.showAllWhenNotFiltered = value;
        }
    }

    [WebPartStorage(Storage.Personal), DefaultValue(true), Browsable(false)]
    public bool ShowCheckAll
    {
        get
        {
            return this.showCheckAll;
        }
        set
        {
            this.showCheckAll = value;
        }
    }

    [DefaultValue(true), WebPartStorage(Storage.Personal), Browsable(false)]
    public bool ShowClearButtonForEachField
    {
        get
        {
            return this.showClearButtonForEachField;
        }
        set
        {
            this.showClearButtonForEachField = value;
        }
    }

    [WebPartStorage(Storage.Personal), Browsable(false), DefaultValue(true)]
    public bool ShowOkCancelButtons
    {
        get
        {
            return this.showOkCancelButtons;
        }
        set
        {
            this.showOkCancelButtons = value;
        }
    }

    [Browsable(false), WebPartStorage(Storage.Personal), DefaultValue(false)]
    public bool ShowSubmitClearButtons
    {
        get
        {
            return this.showSubmitClearButtons;
        }
        set
        {
            this.showSubmitClearButtons = value;
        }
    }

    [Browsable(false)]
    public string SiteList
    {
        get
        {
            return this.siteList;
        }
        set
        {
            this.siteList = value;
        }
    }

    [Browsable(false)]
    public string SiteURL
    {
        get
        {
            return this.siteURL;
        }
        set
        {
            this.siteURL = value;
        }
    }

    [Browsable(false)]
    public string SourceWebPart
    {
        get
        {
            return this.sourceWebPart;
        }
        set
        {
            this.sourceWebPart = value;
        }
    }

    [WebPartStorage(Storage.Personal), Browsable(false), DefaultValue("res:WebPart_ApplyFilter_Caption")]
    public string SubmitButtonText
    {
        get
        {
            return this.submitButtonText;
        }
        set
        {
            if (string.IsNullOrEmpty(value))
            {
                value = "res:WebPart_ApplyFilter_Caption";
            }
            this.submitButtonText = value;
        }
    }

    private string SubmitButtonTextFromResource
    {
        get
        {
            return this.ReadResourcesString(this.SubmitButtonText);
        }
    }

    [Browsable(false)]
    public string TargetList
    {
        get
        {
            return this.targetList;
        }
        set
        {
            this.targetList = value;
        }
    }

    [Description("URL to User Control (ASCX) file which is used for rendering the web part."), FriendlyName("Template Url"), Category("Advanced Display Options"), WebPartStorage(Storage.Personal), DefaultValue("/_layouts/KWizCom_DropDownFilter/DropDownFilter.ascx"), Browsable(true)]
    public string TemplateUrl
    {
        get
        {
            return this.templateUrl;
        }
        set
        {
            if (string.IsNullOrEmpty(this.templateUrl))
            {
                value = "/_layouts/KWizCom_DropDownFilter/DropDownFilter.ascx";
            }
            this.templateUrl = value;
        }
    }

    [DefaultValue(true), WebPartStorage(Storage.Personal), Browsable(false)]
    public bool UseConnectedListAsDataSource
    {
        get
        {
            return this.useConnectedListAsDataSource;
        }
        set
        {
            this.useConnectedListAsDataSource = value;
        }
    }

    [FriendlyName("Alwais remember previous selected filter"), DefaultValue(true), Description("Use cookie to sremember previous selected filter."), Browsable(false), WebPartStorage(Storage.Personal)]
    public bool UseCookieToSaveTheOldFilterChoice
    {
        get
        {
            return this.useCookieToSaveTheOldFilterChoice;
        }
        set
        {
            this.useCookieToSaveTheOldFilterChoice = value;
        }
    }

    [WebPartStorage(Storage.Personal), Browsable(false)]
    public ComboListFilterProperties WProperties
    {
        get
        {
            if (this._WProperties == null)
            {
                this._WProperties = new ComboListFilterProperties();
            }
            return this._WProperties;
        }
        set
        {
            this._WProperties = value;
        }
    }

    // Nested Types
    public delegate DataTable DataTableExternalFilterDelegate(DataTable dataTable);

    public class DataTableProviderConnectionPoint : ProviderConnectionPoint
    {
        // Methods
        public DataTableProviderConnectionPoint(MethodInfo callbackMethod, Type interfaceType, Type controlType, string name, string id, bool allowsMultipleConnections) : base(callbackMethod, interfaceType, controlType, name, id, allowsMultipleConnections)
        {
        }

        public override bool GetEnabled(Control control)
        {
            return ((KWizCom.SharePoint.WebParts.ComboListFilter.ComboListFilter) control).ConnectionPointEnabled;
        }
    }
}

