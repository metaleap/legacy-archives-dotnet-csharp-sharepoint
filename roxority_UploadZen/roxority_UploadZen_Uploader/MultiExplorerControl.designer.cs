namespace roxUp {
	partial class MultiExplorerControl {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose (bool disposing) {
			if (MainForm != null)
				MainForm.statusInfoLabel.BackgroundImage = null;
			infoBackgroundImage.Dispose ();
			if (disposing && (components != null)) {
				components.Dispose ();
			}
			base.Dispose (disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent () {
			this.components = new System.ComponentModel.Container ();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (MultiExplorerControl));
			this.splitContainer = new System.Windows.Forms.SplitContainer ();
			this.browseGroupBox = new System.Windows.Forms.GroupBox ();
			this.browseListView = new System.Windows.Forms.ListView ();
			this.browseNameColumnHeader = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.browseDescriptionColumnHeader = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.browseContextMenuStrip = new System.Windows.Forms.ContextMenuStrip (this.components);
			this.contextAddItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.contextAddZipItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.contextFolderItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.contextSep2Item = new System.Windows.Forms.ToolStripSeparator ();
			this.contextOpenItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.contextOpenWithItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.contextRefreshItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.contextSep3Item = new System.Windows.Forms.ToolStripSeparator ();
			this.contextDeleteItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.contextPropertiesItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.fileIconList = new System.Windows.Forms.ImageList (this.components);
			this.browseToolStrip = new System.Windows.Forms.ToolStrip ();
			this.browseBackToolStripButton = new System.Windows.Forms.ToolStripSplitButton ();
			this.browseUpToolStripButton = new System.Windows.Forms.ToolStripButton ();
			this.browseFoldersToolStripButton = new System.Windows.Forms.ToolStripButton ();
			this.browseFiltersToolStripButton = new System.Windows.Forms.ToolStripDropDownButton ();
			this.browseFiltersDocumentsToolStripItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.browseFiltersImagesToolStripItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.browseFiltersWebPartsToolStripItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.browseFiltersTemplatesToolStripItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.browseFiltersMastersToolStripItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.browseFiltersArchivesToolStripItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.browseSeparatorToolStripMenuItem = new System.Windows.Forms.ToolStripSeparator ();
			this.browseFiltersNoneToolStripItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.browseSeparatorToolStripItem = new System.Windows.Forms.ToolStripSeparator ();
			this.browseHiddenToolStripItem = new System.Windows.Forms.ToolStripDropDownButton ();
			this.uploadGroupBox = new System.Windows.Forms.GroupBox ();
			this.uploadWebBrowser = new System.Windows.Forms.WebBrowser ();
			this.uploadListView = new System.Windows.Forms.ListView ();
			this.uploadNameColumnHeader = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.uploadDescriptionColumnHeader = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.iconList = new System.Windows.Forms.ImageList (this.components);
			this.zipPanel = new System.Windows.Forms.Panel ();
			this.zipLabel = new System.Windows.Forms.Label ();
			this.zipCheckBox = new System.Windows.Forms.CheckBox ();
			this.uploadSplitContainer = new System.Windows.Forms.SplitContainer ();
			this.metaGroupBox = new System.Windows.Forms.GroupBox ();
			this.metaSplitContainer = new System.Windows.Forms.SplitContainer ();
			this.metaListView = new System.Windows.Forms.ListView ();
			this.metaTitleColumn = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.metaNameColumn = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.metaTypeColumn = ((System.Windows.Forms.ColumnHeader) (new System.Windows.Forms.ColumnHeader ()));
			this.propertyGrid = new System.Windows.Forms.PropertyGrid ();
			this.toolStrip = new System.Windows.Forms.ToolStrip ();
			this.toolStripDriveItem = new System.Windows.Forms.ToolStripSplitButton ();
			this.toolStripPathButton = new System.Windows.Forms.ToolStripSplitButton ();
			this.toolStripAddFolderItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripFolderSeparatorItem = new System.Windows.Forms.ToolStripSeparator ();
			this.toolStripSelectAllItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripCancelButton = new System.Windows.Forms.ToolStripButton ();
			this.toolStripUploadButton = new System.Windows.Forms.ToolStripSplitButton ();
			this.toolStripCheckinItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripCheckInNoneItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripCheckInSepItem = new System.Windows.Forms.ToolStripSeparator ();
			this.toolStripCheckInMinorItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripCheckInMajorItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripCheckInOverwriteItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripCheckOutItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripUploadSepItem = new System.Windows.Forms.ToolStripSeparator ();
			this.toolStripUploadSaveItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripUploadOpenItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripUploadRecentItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripUserSeparator = new System.Windows.Forms.ToolStripSeparator ();
			this.toolStripUserItem = new System.Windows.Forms.ToolStripDropDownButton ();
			this.toolStripUserForgetItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.toolStripRightSeparator = new System.Windows.Forms.ToolStripSeparator ();
			this.toolStripLeftSeparator = new System.Windows.Forms.ToolStripSeparator ();
			this.toolStripRefreshButton = new System.Windows.Forms.ToolStripButton ();
			this.toolStripRemoveButton = new System.Windows.Forms.ToolStripButton ();
			this.toolStripMetaDataButton = new System.Windows.Forms.ToolStripButton ();
			this.toolStripAddButton = new System.Windows.Forms.ToolStripSplitButton ();
			this.toolStripAddZipItem = new System.Windows.Forms.ToolStripMenuItem ();
			this.uploadWorker = new System.ComponentModel.BackgroundWorker ();
			this.fileSystemWatcher = new System.IO.FileSystemWatcher ();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog ();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog ();
			this.splitContainer.Panel1.SuspendLayout ();
			this.splitContainer.Panel2.SuspendLayout ();
			this.splitContainer.SuspendLayout ();
			this.browseGroupBox.SuspendLayout ();
			this.browseContextMenuStrip.SuspendLayout ();
			this.browseToolStrip.SuspendLayout ();
			this.uploadGroupBox.SuspendLayout ();
			this.zipPanel.SuspendLayout ();
			this.uploadSplitContainer.Panel1.SuspendLayout ();
			this.uploadSplitContainer.Panel2.SuspendLayout ();
			this.uploadSplitContainer.SuspendLayout ();
			this.metaGroupBox.SuspendLayout ();
			this.metaSplitContainer.Panel1.SuspendLayout ();
			this.metaSplitContainer.Panel2.SuspendLayout ();
			this.metaSplitContainer.SuspendLayout ();
			this.toolStrip.SuspendLayout ();
			((System.ComponentModel.ISupportInitialize) (this.fileSystemWatcher)).BeginInit ();
			this.SuspendLayout ();
			// 
			// splitContainer
			// 
			resources.ApplyResources (this.splitContainer, "splitContainer");
			this.splitContainer.Name = "splitContainer";
			// 
			// splitContainer.Panel1
			// 
			resources.ApplyResources (this.splitContainer.Panel1, "splitContainer.Panel1");
			this.splitContainer.Panel1.Controls.Add (this.browseGroupBox);
			// 
			// splitContainer.Panel2
			// 
			resources.ApplyResources (this.splitContainer.Panel2, "splitContainer.Panel2");
			this.splitContainer.Panel2.Controls.Add (this.uploadGroupBox);
			// 
			// browseGroupBox
			// 
			resources.ApplyResources (this.browseGroupBox, "browseGroupBox");
			this.browseGroupBox.Controls.Add (this.browseListView);
			this.browseGroupBox.Controls.Add (this.browseToolStrip);
			this.browseGroupBox.Name = "browseGroupBox";
			this.browseGroupBox.TabStop = false;
			// 
			// browseListView
			// 
			resources.ApplyResources (this.browseListView, "browseListView");
			this.browseListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.browseListView.Columns.AddRange (new System.Windows.Forms.ColumnHeader [] {
            this.browseNameColumnHeader,
            this.browseDescriptionColumnHeader});
			this.browseListView.ContextMenuStrip = this.browseContextMenuStrip;
			this.browseListView.HideSelection = false;
			this.browseListView.LargeImageList = this.fileIconList;
			this.browseListView.Name = "browseListView";
			this.browseListView.SmallImageList = this.fileIconList;
			this.browseListView.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.browseListView.UseCompatibleStateImageBehavior = false;
			this.browseListView.View = System.Windows.Forms.View.Tile;
			this.browseListView.ItemActivate += new System.EventHandler (this.browseListView_ItemActivate);
			this.browseListView.SelectedIndexChanged += new System.EventHandler (this.browseListView_SelectedIndexChanged);
			// 
			// browseNameColumnHeader
			// 
			resources.ApplyResources (this.browseNameColumnHeader, "browseNameColumnHeader");
			// 
			// browseDescriptionColumnHeader
			// 
			resources.ApplyResources (this.browseDescriptionColumnHeader, "browseDescriptionColumnHeader");
			// 
			// browseContextMenuStrip
			// 
			resources.ApplyResources (this.browseContextMenuStrip, "browseContextMenuStrip");
			this.browseContextMenuStrip.Items.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.contextAddItem,
            this.contextAddZipItem,
            this.contextFolderItem,
            this.contextSep2Item,
            this.contextOpenItem,
            this.contextOpenWithItem,
            this.contextRefreshItem,
            this.contextSep3Item,
            this.contextDeleteItem,
            this.contextPropertiesItem});
			this.browseContextMenuStrip.Name = "browseFileContextMenuStrip";
			// 
			// contextAddItem
			// 
			resources.ApplyResources (this.contextAddItem, "contextAddItem");
			this.contextAddItem.Image = global::roxUp.Properties.Resources.DoubleRightArrowHS;
			this.contextAddItem.Name = "contextAddItem";
			this.contextAddItem.Click += new System.EventHandler (this.contextMenuItem_Click);
			// 
			// contextAddZipItem
			// 
			resources.ApplyResources (this.contextAddZipItem, "contextAddZipItem");
			this.contextAddZipItem.Name = "contextAddZipItem";
			this.contextAddZipItem.Click += new System.EventHandler (this.contextMenuItem_Click);
			// 
			// contextFolderItem
			// 
			resources.ApplyResources (this.contextFolderItem, "contextFolderItem");
			this.contextFolderItem.Image = global::roxUp.Properties.Resources.MoveToFolderHS;
			this.contextFolderItem.Name = "contextFolderItem";
			this.contextFolderItem.Click += new System.EventHandler (this.contextMenuItem_Click);
			// 
			// contextSep2Item
			// 
			resources.ApplyResources (this.contextSep2Item, "contextSep2Item");
			this.contextSep2Item.Name = "contextSep2Item";
			// 
			// contextOpenItem
			// 
			resources.ApplyResources (this.contextOpenItem, "contextOpenItem");
			this.contextOpenItem.Image = global::roxUp.Properties.Resources.openfolderHS;
			this.contextOpenItem.Name = "contextOpenItem";
			this.contextOpenItem.Click += new System.EventHandler (this.contextMenuItem_Click);
			// 
			// contextOpenWithItem
			// 
			resources.ApplyResources (this.contextOpenWithItem, "contextOpenWithItem");
			this.contextOpenWithItem.Name = "contextOpenWithItem";
			this.contextOpenWithItem.Click += new System.EventHandler (this.contextMenuItem_Click);
			// 
			// contextRefreshItem
			// 
			resources.ApplyResources (this.contextRefreshItem, "contextRefreshItem");
			this.contextRefreshItem.Image = global::roxUp.Properties.Resources.REFRESH;
			this.contextRefreshItem.Name = "contextRefreshItem";
			this.contextRefreshItem.Click += new System.EventHandler (this.contextMenuItem_Click);
			// 
			// contextSep3Item
			// 
			resources.ApplyResources (this.contextSep3Item, "contextSep3Item");
			this.contextSep3Item.Name = "contextSep3Item";
			// 
			// contextDeleteItem
			// 
			resources.ApplyResources (this.contextDeleteItem, "contextDeleteItem");
			this.contextDeleteItem.Image = global::roxUp.Properties.Resources.DeleteHS;
			this.contextDeleteItem.Name = "contextDeleteItem";
			this.contextDeleteItem.Click += new System.EventHandler (this.contextMenuItem_Click);
			// 
			// contextPropertiesItem
			// 
			resources.ApplyResources (this.contextPropertiesItem, "contextPropertiesItem");
			this.contextPropertiesItem.Image = global::roxUp.Properties.Resources.PropertiesHS;
			this.contextPropertiesItem.Name = "contextPropertiesItem";
			this.contextPropertiesItem.Click += new System.EventHandler (this.contextMenuItem_Click);
			// 
			// fileIconList
			// 
			this.fileIconList.ImageStream = ((System.Windows.Forms.ImageListStreamer) (resources.GetObject ("fileIconList.ImageStream")));
			this.fileIconList.TransparentColor = System.Drawing.Color.Transparent;
			this.fileIconList.Images.SetKeyName (0, "folder_open.ico");
			// 
			// browseToolStrip
			// 
			resources.ApplyResources (this.browseToolStrip, "browseToolStrip");
			this.browseToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.browseToolStrip.Items.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.browseBackToolStripButton,
            this.browseUpToolStripButton,
            this.browseFoldersToolStripButton,
            this.browseFiltersToolStripButton,
            this.browseSeparatorToolStripItem,
            this.browseHiddenToolStripItem});
			this.browseToolStrip.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
			this.browseToolStrip.Name = "browseToolStrip";
			// 
			// browseBackToolStripButton
			// 
			resources.ApplyResources (this.browseBackToolStripButton, "browseBackToolStripButton");
			this.browseBackToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.browseBackToolStripButton.Image = global::roxUp.Properties.Resources.NavBack;
			this.browseBackToolStripButton.Name = "browseBackToolStripButton";
			this.browseBackToolStripButton.ButtonClick += new System.EventHandler (this.browseHistoryMenuItem_Click);
			this.browseBackToolStripButton.DropDownOpening += new System.EventHandler (this.browseHistoryToolStripItem_DropDownOpening);
			// 
			// browseUpToolStripButton
			// 
			resources.ApplyResources (this.browseUpToolStripButton, "browseUpToolStripButton");
			this.browseUpToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.browseUpToolStripButton.Image = global::roxUp.Properties.Resources.UPFOLDER;
			this.browseUpToolStripButton.Name = "browseUpToolStripButton";
			this.browseUpToolStripButton.Click += new System.EventHandler (this.browseUpToolStripButton_Click);
			// 
			// browseFoldersToolStripButton
			// 
			resources.ApplyResources (this.browseFoldersToolStripButton, "browseFoldersToolStripButton");
			this.browseFoldersToolStripButton.CheckOnClick = true;
			this.browseFoldersToolStripButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.browseFoldersToolStripButton.Image = global::roxUp.Properties.Resources.MoveFolderHS;
			this.browseFoldersToolStripButton.Name = "browseFoldersToolStripButton";
			this.browseFoldersToolStripButton.Click += new System.EventHandler (this.browseFoldersToolStripButton_Click);
			// 
			// browseFiltersToolStripButton
			// 
			resources.ApplyResources (this.browseFiltersToolStripButton, "browseFiltersToolStripButton");
			this.browseFiltersToolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.browseFiltersToolStripButton.AutoToolTip = false;
			this.browseFiltersToolStripButton.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.browseFiltersDocumentsToolStripItem,
            this.browseFiltersImagesToolStripItem,
            this.browseFiltersWebPartsToolStripItem,
            this.browseFiltersTemplatesToolStripItem,
            this.browseFiltersMastersToolStripItem,
            this.browseFiltersArchivesToolStripItem,
            this.browseSeparatorToolStripMenuItem,
            this.browseFiltersNoneToolStripItem});
			this.browseFiltersToolStripButton.Image = global::roxUp.Properties.Resources.Filter2HS;
			this.browseFiltersToolStripButton.Name = "browseFiltersToolStripButton";
			this.browseFiltersToolStripButton.Tag = "*";
			// 
			// browseFiltersDocumentsToolStripItem
			// 
			resources.ApplyResources (this.browseFiltersDocumentsToolStripItem, "browseFiltersDocumentsToolStripItem");
			this.browseFiltersDocumentsToolStripItem.Image = global::roxUp.Properties.Resources.ICDOCX;
			this.browseFiltersDocumentsToolStripItem.Name = "browseFiltersDocumentsToolStripItem";
			this.browseFiltersDocumentsToolStripItem.Tag = resources.GetString ("browseFiltersDocumentsToolStripItem.Tag");
			this.browseFiltersDocumentsToolStripItem.Click += new System.EventHandler (this.toolStripFilterItem_Click);
			// 
			// browseFiltersImagesToolStripItem
			// 
			resources.ApplyResources (this.browseFiltersImagesToolStripItem, "browseFiltersImagesToolStripItem");
			this.browseFiltersImagesToolStripItem.Image = global::roxUp.Properties.Resources.ICJPEG;
			this.browseFiltersImagesToolStripItem.Name = "browseFiltersImagesToolStripItem";
			this.browseFiltersImagesToolStripItem.Tag = resources.GetString ("browseFiltersImagesToolStripItem.Tag");
			this.browseFiltersImagesToolStripItem.Click += new System.EventHandler (this.toolStripFilterItem_Click);
			// 
			// browseFiltersWebPartsToolStripItem
			// 
			resources.ApplyResources (this.browseFiltersWebPartsToolStripItem, "browseFiltersWebPartsToolStripItem");
			this.browseFiltersWebPartsToolStripItem.Image = global::roxUp.Properties.Resources.WEBPART;
			this.browseFiltersWebPartsToolStripItem.Name = "browseFiltersWebPartsToolStripItem";
			this.browseFiltersWebPartsToolStripItem.Tag = "*.dwp;*.webpart";
			this.browseFiltersWebPartsToolStripItem.Click += new System.EventHandler (this.toolStripFilterItem_Click);
			// 
			// browseFiltersTemplatesToolStripItem
			// 
			resources.ApplyResources (this.browseFiltersTemplatesToolStripItem, "browseFiltersTemplatesToolStripItem");
			this.browseFiltersTemplatesToolStripItem.Image = global::roxUp.Properties.Resources.ICSTP;
			this.browseFiltersTemplatesToolStripItem.Name = "browseFiltersTemplatesToolStripItem";
			this.browseFiltersTemplatesToolStripItem.Tag = "*.stp;*.cab;*.wsp";
			this.browseFiltersTemplatesToolStripItem.Click += new System.EventHandler (this.toolStripFilterItem_Click);
			// 
			// browseFiltersMastersToolStripItem
			// 
			resources.ApplyResources (this.browseFiltersMastersToolStripItem, "browseFiltersMastersToolStripItem");
			this.browseFiltersMastersToolStripItem.Image = global::roxUp.Properties.Resources.ICMASTER;
			this.browseFiltersMastersToolStripItem.Name = "browseFiltersMastersToolStripItem";
			this.browseFiltersMastersToolStripItem.Tag = "*.master";
			this.browseFiltersMastersToolStripItem.Click += new System.EventHandler (this.toolStripFilterItem_Click);
			// 
			// browseFiltersArchivesToolStripItem
			// 
			resources.ApplyResources (this.browseFiltersArchivesToolStripItem, "browseFiltersArchivesToolStripItem");
			this.browseFiltersArchivesToolStripItem.Name = "browseFiltersArchivesToolStripItem";
			this.browseFiltersArchivesToolStripItem.Tag = "*.zip;*.rar;*.wsp;*.stp;*.cab;*.arj;*.lzh;*.ace;*.tar;*.gz;*.uue;*.bz2;*.jar;*.is" +
				"o;*.z;*.7z";
			this.browseFiltersArchivesToolStripItem.Click += new System.EventHandler (this.toolStripFilterItem_Click);
			// 
			// browseSeparatorToolStripMenuItem
			// 
			resources.ApplyResources (this.browseSeparatorToolStripMenuItem, "browseSeparatorToolStripMenuItem");
			this.browseSeparatorToolStripMenuItem.Name = "browseSeparatorToolStripMenuItem";
			// 
			// browseFiltersNoneToolStripItem
			// 
			resources.ApplyResources (this.browseFiltersNoneToolStripItem, "browseFiltersNoneToolStripItem");
			this.browseFiltersNoneToolStripItem.Checked = true;
			this.browseFiltersNoneToolStripItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.browseFiltersNoneToolStripItem.Image = global::roxUp.Properties.Resources.DeleteHS;
			this.browseFiltersNoneToolStripItem.Name = "browseFiltersNoneToolStripItem";
			this.browseFiltersNoneToolStripItem.Tag = "*";
			this.browseFiltersNoneToolStripItem.Click += new System.EventHandler (this.toolStripFilterItem_Click);
			// 
			// browseSeparatorToolStripItem
			// 
			resources.ApplyResources (this.browseSeparatorToolStripItem, "browseSeparatorToolStripItem");
			this.browseSeparatorToolStripItem.Name = "browseSeparatorToolStripItem";
			// 
			// browseHiddenToolStripItem
			// 
			resources.ApplyResources (this.browseHiddenToolStripItem, "browseHiddenToolStripItem");
			this.browseHiddenToolStripItem.AutoToolTip = false;
			this.browseHiddenToolStripItem.ForeColor = System.Drawing.SystemColors.GrayText;
			this.browseHiddenToolStripItem.Name = "browseHiddenToolStripItem";
			this.browseHiddenToolStripItem.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			this.browseHiddenToolStripItem.ShowDropDownArrow = false;
			// 
			// uploadGroupBox
			// 
			resources.ApplyResources (this.uploadGroupBox, "uploadGroupBox");
			this.uploadGroupBox.Controls.Add (this.uploadWebBrowser);
			this.uploadGroupBox.Controls.Add (this.uploadListView);
			this.uploadGroupBox.Controls.Add (this.zipPanel);
			this.uploadGroupBox.Name = "uploadGroupBox";
			this.uploadGroupBox.TabStop = false;
			// 
			// uploadWebBrowser
			// 
			resources.ApplyResources (this.uploadWebBrowser, "uploadWebBrowser");
			this.uploadWebBrowser.AllowWebBrowserDrop = false;
			this.uploadWebBrowser.IsWebBrowserContextMenuEnabled = false;
			this.uploadWebBrowser.MinimumSize = new System.Drawing.Size (20, 22);
			this.uploadWebBrowser.Name = "uploadWebBrowser";
			this.uploadWebBrowser.ScriptErrorsSuppressed = true;
			// 
			// uploadListView
			// 
			resources.ApplyResources (this.uploadListView, "uploadListView");
			this.uploadListView.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.uploadListView.Columns.AddRange (new System.Windows.Forms.ColumnHeader [] {
            this.uploadNameColumnHeader,
            this.uploadDescriptionColumnHeader});
			this.uploadListView.FullRowSelect = true;
			this.uploadListView.GridLines = true;
			this.uploadListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.uploadListView.HideSelection = false;
			this.uploadListView.LargeImageList = this.iconList;
			this.uploadListView.Name = "uploadListView";
			this.uploadListView.SmallImageList = this.iconList;
			this.uploadListView.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.uploadListView.UseCompatibleStateImageBehavior = false;
			this.uploadListView.View = System.Windows.Forms.View.Tile;
			this.uploadListView.ItemActivate += new System.EventHandler (this.uploadListView_ItemActivate);
			this.uploadListView.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler (this.uploadListView_ItemCheck);
			this.uploadListView.SelectedIndexChanged += new System.EventHandler (this.uploadListView_SelectedIndexChanged);
			// 
			// uploadNameColumnHeader
			// 
			resources.ApplyResources (this.uploadNameColumnHeader, "uploadNameColumnHeader");
			// 
			// uploadDescriptionColumnHeader
			// 
			resources.ApplyResources (this.uploadDescriptionColumnHeader, "uploadDescriptionColumnHeader");
			// 
			// iconList
			// 
			this.iconList.ImageStream = ((System.Windows.Forms.ImageListStreamer) (resources.GetObject ("iconList.ImageStream")));
			this.iconList.TransparentColor = System.Drawing.Color.Transparent;
			this.iconList.Images.SetKeyName (0, "None");
			this.iconList.Images.SetKeyName (1, "Success");
			this.iconList.Images.SetKeyName (2, "Uploading");
			this.iconList.Images.SetKeyName (3, "Failed");
			// 
			// zipPanel
			// 
			resources.ApplyResources (this.zipPanel, "zipPanel");
			this.zipPanel.BackColor = System.Drawing.SystemColors.Info;
			this.zipPanel.Controls.Add (this.zipLabel);
			this.zipPanel.Controls.Add (this.zipCheckBox);
			this.zipPanel.ForeColor = System.Drawing.SystemColors.InfoText;
			this.zipPanel.Name = "zipPanel";
			// 
			// zipLabel
			// 
			resources.ApplyResources (this.zipLabel, "zipLabel");
			this.zipLabel.AutoEllipsis = true;
			this.zipLabel.Name = "zipLabel";
			// 
			// zipCheckBox
			// 
			resources.ApplyResources (this.zipCheckBox, "zipCheckBox");
			this.zipCheckBox.AutoEllipsis = true;
			this.zipCheckBox.Name = "zipCheckBox";
			this.zipCheckBox.UseVisualStyleBackColor = true;
			// 
			// uploadSplitContainer
			// 
			resources.ApplyResources (this.uploadSplitContainer, "uploadSplitContainer");
			this.uploadSplitContainer.Name = "uploadSplitContainer";
			// 
			// uploadSplitContainer.Panel1
			// 
			resources.ApplyResources (this.uploadSplitContainer.Panel1, "uploadSplitContainer.Panel1");
			this.uploadSplitContainer.Panel1.Controls.Add (this.splitContainer);
			// 
			// uploadSplitContainer.Panel2
			// 
			resources.ApplyResources (this.uploadSplitContainer.Panel2, "uploadSplitContainer.Panel2");
			this.uploadSplitContainer.Panel2.Controls.Add (this.metaGroupBox);
			this.uploadSplitContainer.Panel2Collapsed = true;
			// 
			// metaGroupBox
			// 
			resources.ApplyResources (this.metaGroupBox, "metaGroupBox");
			this.metaGroupBox.Controls.Add (this.metaSplitContainer);
			this.metaGroupBox.Name = "metaGroupBox";
			this.metaGroupBox.TabStop = false;
			// 
			// metaSplitContainer
			// 
			resources.ApplyResources (this.metaSplitContainer, "metaSplitContainer");
			this.metaSplitContainer.Name = "metaSplitContainer";
			// 
			// metaSplitContainer.Panel1
			// 
			resources.ApplyResources (this.metaSplitContainer.Panel1, "metaSplitContainer.Panel1");
			this.metaSplitContainer.Panel1.Controls.Add (this.metaListView);
			// 
			// metaSplitContainer.Panel2
			// 
			resources.ApplyResources (this.metaSplitContainer.Panel2, "metaSplitContainer.Panel2");
			this.metaSplitContainer.Panel2.Controls.Add (this.propertyGrid);
			// 
			// metaListView
			// 
			resources.ApplyResources (this.metaListView, "metaListView");
			this.metaListView.CheckBoxes = true;
			this.metaListView.Columns.AddRange (new System.Windows.Forms.ColumnHeader [] {
            this.metaTitleColumn,
            this.metaNameColumn,
            this.metaTypeColumn});
			this.metaListView.FullRowSelect = true;
			this.metaListView.GridLines = true;
			this.metaListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.metaListView.HideSelection = false;
			this.metaListView.Name = "metaListView";
			this.metaListView.ShowGroups = false;
			this.metaListView.UseCompatibleStateImageBehavior = false;
			this.metaListView.View = System.Windows.Forms.View.Details;
			this.metaListView.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler (this.metaListView_ItemCheck);
			// 
			// metaTitleColumn
			// 
			resources.ApplyResources (this.metaTitleColumn, "metaTitleColumn");
			// 
			// metaNameColumn
			// 
			resources.ApplyResources (this.metaNameColumn, "metaNameColumn");
			// 
			// metaTypeColumn
			// 
			resources.ApplyResources (this.metaTypeColumn, "metaTypeColumn");
			// 
			// propertyGrid
			// 
			resources.ApplyResources (this.propertyGrid, "propertyGrid");
			this.propertyGrid.HelpBackColor = System.Drawing.SystemColors.Info;
			this.propertyGrid.HelpForeColor = System.Drawing.SystemColors.InfoText;
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.ToolbarVisible = false;
			// 
			// toolStrip
			// 
			resources.ApplyResources (this.toolStrip, "toolStrip");
			this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip.Items.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.toolStripDriveItem,
            this.toolStripPathButton,
            this.toolStripCancelButton,
            this.toolStripUploadButton,
            this.toolStripUserSeparator,
            this.toolStripUserItem,
            this.toolStripRightSeparator,
            this.toolStripLeftSeparator,
            this.toolStripRefreshButton,
            this.toolStripRemoveButton,
            this.toolStripMetaDataButton,
            this.toolStripAddButton});
			this.toolStrip.Name = "toolStrip";
			// 
			// toolStripDriveItem
			// 
			resources.ApplyResources (this.toolStripDriveItem, "toolStripDriveItem");
			this.toolStripDriveItem.AutoToolTip = false;
			this.toolStripDriveItem.Image = global::roxUp.Properties.Resources.drive;
			this.toolStripDriveItem.Name = "toolStripDriveItem";
			this.toolStripDriveItem.Tag = "C:";
			this.toolStripDriveItem.ButtonClick += new System.EventHandler (this.toolStripDriveItem_ButtonClick);
			this.toolStripDriveItem.DropDownOpening += new System.EventHandler (this.toolStripDriveItem_DropDownOpening);
			// 
			// toolStripPathButton
			// 
			resources.ApplyResources (this.toolStripPathButton, "toolStripPathButton");
			this.toolStripPathButton.AutoToolTip = false;
			this.toolStripPathButton.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.toolStripAddFolderItem,
            this.toolStripFolderSeparatorItem,
            this.toolStripSelectAllItem});
			this.toolStripPathButton.Image = global::roxUp.Properties.Resources.openfolderHS;
			this.toolStripPathButton.Name = "toolStripPathButton";
			this.toolStripPathButton.ButtonClick += new System.EventHandler (this.toolStripPathButton_Click);
			this.toolStripPathButton.DropDownOpening += new System.EventHandler (this.toolStripPathButton_DropDownOpening);
			// 
			// toolStripAddFolderItem
			// 
			resources.ApplyResources (this.toolStripAddFolderItem, "toolStripAddFolderItem");
			this.toolStripAddFolderItem.Image = global::roxUp.Properties.Resources.MoveFolderHS;
			this.toolStripAddFolderItem.Name = "toolStripAddFolderItem";
			this.toolStripAddFolderItem.Click += new System.EventHandler (this.toolStripAddButton_Click);
			// 
			// toolStripFolderSeparatorItem
			// 
			resources.ApplyResources (this.toolStripFolderSeparatorItem, "toolStripFolderSeparatorItem");
			this.toolStripFolderSeparatorItem.Name = "toolStripFolderSeparatorItem";
			// 
			// toolStripSelectAllItem
			// 
			resources.ApplyResources (this.toolStripSelectAllItem, "toolStripSelectAllItem");
			this.toolStripSelectAllItem.Name = "toolStripSelectAllItem";
			this.toolStripSelectAllItem.Click += new System.EventHandler (this.toolStripSelectAllItem_Click);
			// 
			// toolStripCancelButton
			// 
			resources.ApplyResources (this.toolStripCancelButton, "toolStripCancelButton");
			this.toolStripCancelButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripCancelButton.AutoToolTip = false;
			this.toolStripCancelButton.Image = global::roxUp.Properties.Resources.RightsRestrictedHS;
			this.toolStripCancelButton.Name = "toolStripCancelButton";
			this.toolStripCancelButton.Click += new System.EventHandler (this.toolStripCancelButton_Click);
			// 
			// toolStripUploadButton
			// 
			resources.ApplyResources (this.toolStripUploadButton, "toolStripUploadButton");
			this.toolStripUploadButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripUploadButton.AutoToolTip = false;
			this.toolStripUploadButton.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.toolStripCheckinItem,
            this.toolStripCheckOutItem,
            this.toolStripUploadSepItem,
            this.toolStripUploadSaveItem,
            this.toolStripUploadOpenItem,
            this.toolStripUploadRecentItem});
			this.toolStripUploadButton.Image = global::roxUp.Properties.Resources.PublishPlanHS;
			this.toolStripUploadButton.Name = "toolStripUploadButton";
			this.toolStripUploadButton.ButtonClick += new System.EventHandler (this.toolStripUploadButton_Click);
			// 
			// toolStripCheckinItem
			// 
			resources.ApplyResources (this.toolStripCheckinItem, "toolStripCheckinItem");
			this.toolStripCheckinItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripCheckinItem.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.toolStripCheckInNoneItem,
            this.toolStripCheckInSepItem,
            this.toolStripCheckInMinorItem,
            this.toolStripCheckInMajorItem,
            this.toolStripCheckInOverwriteItem});
			this.toolStripCheckinItem.Image = global::roxUp.Properties.Resources.CHECKIN;
			this.toolStripCheckinItem.Name = "toolStripCheckinItem";
			// 
			// toolStripCheckInNoneItem
			// 
			resources.ApplyResources (this.toolStripCheckInNoneItem, "toolStripCheckInNoneItem");
			this.toolStripCheckInNoneItem.Checked = true;
			this.toolStripCheckInNoneItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.toolStripCheckInNoneItem.Image = global::roxUp.Properties.Resources.DeleteHS;
			this.toolStripCheckInNoneItem.Name = "toolStripCheckInNoneItem";
			this.toolStripCheckInNoneItem.Click += new System.EventHandler (this.toolStripCheckInItem_Click);
			// 
			// toolStripCheckInSepItem
			// 
			resources.ApplyResources (this.toolStripCheckInSepItem, "toolStripCheckInSepItem");
			this.toolStripCheckInSepItem.Name = "toolStripCheckInSepItem";
			// 
			// toolStripCheckInMinorItem
			// 
			resources.ApplyResources (this.toolStripCheckInMinorItem, "toolStripCheckInMinorItem");
			this.toolStripCheckInMinorItem.Name = "toolStripCheckInMinorItem";
			this.toolStripCheckInMinorItem.Tag = "MinorCheckIn";
			this.toolStripCheckInMinorItem.Click += new System.EventHandler (this.toolStripCheckInItem_Click);
			// 
			// toolStripCheckInMajorItem
			// 
			resources.ApplyResources (this.toolStripCheckInMajorItem, "toolStripCheckInMajorItem");
			this.toolStripCheckInMajorItem.Name = "toolStripCheckInMajorItem";
			this.toolStripCheckInMajorItem.Tag = "MajorCheckIn";
			this.toolStripCheckInMajorItem.Click += new System.EventHandler (this.toolStripCheckInItem_Click);
			// 
			// toolStripCheckInOverwriteItem
			// 
			resources.ApplyResources (this.toolStripCheckInOverwriteItem, "toolStripCheckInOverwriteItem");
			this.toolStripCheckInOverwriteItem.Name = "toolStripCheckInOverwriteItem";
			this.toolStripCheckInOverwriteItem.Tag = "OverwriteCheckIn";
			this.toolStripCheckInOverwriteItem.Click += new System.EventHandler (this.toolStripCheckInItem_Click);
			// 
			// toolStripCheckOutItem
			// 
			resources.ApplyResources (this.toolStripCheckOutItem, "toolStripCheckOutItem");
			this.toolStripCheckOutItem.CheckOnClick = true;
			this.toolStripCheckOutItem.Image = global::roxUp.Properties.Resources.CHECKOUT;
			this.toolStripCheckOutItem.Name = "toolStripCheckOutItem";
			// 
			// toolStripUploadSepItem
			// 
			resources.ApplyResources (this.toolStripUploadSepItem, "toolStripUploadSepItem");
			this.toolStripUploadSepItem.Name = "toolStripUploadSepItem";
			// 
			// toolStripUploadSaveItem
			// 
			resources.ApplyResources (this.toolStripUploadSaveItem, "toolStripUploadSaveItem");
			this.toolStripUploadSaveItem.Image = global::roxUp.Properties.Resources.saveHS;
			this.toolStripUploadSaveItem.Name = "toolStripUploadSaveItem";
			this.toolStripUploadSaveItem.Click += new System.EventHandler (this.toolStripUploadSaveItem_Click);
			// 
			// toolStripUploadOpenItem
			// 
			resources.ApplyResources (this.toolStripUploadOpenItem, "toolStripUploadOpenItem");
			this.toolStripUploadOpenItem.Image = global::roxUp.Properties.Resources.openfolderHS;
			this.toolStripUploadOpenItem.Name = "toolStripUploadOpenItem";
			this.toolStripUploadOpenItem.Click += new System.EventHandler (this.toolStripUploadOpenItem_Click);
			// 
			// toolStripUploadRecentItem
			// 
			resources.ApplyResources (this.toolStripUploadRecentItem, "toolStripUploadRecentItem");
			this.toolStripUploadRecentItem.Name = "toolStripUploadRecentItem";
			// 
			// toolStripUserSeparator
			// 
			resources.ApplyResources (this.toolStripUserSeparator, "toolStripUserSeparator");
			this.toolStripUserSeparator.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripUserSeparator.Name = "toolStripUserSeparator";
			// 
			// toolStripUserItem
			// 
			resources.ApplyResources (this.toolStripUserItem, "toolStripUserItem");
			this.toolStripUserItem.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripUserItem.AutoToolTip = false;
			this.toolStripUserItem.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.toolStripUserForgetItem});
			this.toolStripUserItem.Name = "toolStripUserItem";
			this.toolStripUserItem.ShowDropDownArrow = false;
			// 
			// toolStripUserForgetItem
			// 
			resources.ApplyResources (this.toolStripUserForgetItem, "toolStripUserForgetItem");
			this.toolStripUserForgetItem.Image = global::roxUp.Properties.Resources.DeleteHS;
			this.toolStripUserForgetItem.Name = "toolStripUserForgetItem";
			this.toolStripUserForgetItem.Click += new System.EventHandler (this.toolStripUserForgetItem_Click);
			// 
			// toolStripRightSeparator
			// 
			resources.ApplyResources (this.toolStripRightSeparator, "toolStripRightSeparator");
			this.toolStripRightSeparator.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripRightSeparator.Name = "toolStripRightSeparator";
			// 
			// toolStripLeftSeparator
			// 
			resources.ApplyResources (this.toolStripLeftSeparator, "toolStripLeftSeparator");
			this.toolStripLeftSeparator.Name = "toolStripLeftSeparator";
			// 
			// toolStripRefreshButton
			// 
			resources.ApplyResources (this.toolStripRefreshButton, "toolStripRefreshButton");
			this.toolStripRefreshButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripRefreshButton.Image = global::roxUp.Properties.Resources.REFRESH;
			this.toolStripRefreshButton.Name = "toolStripRefreshButton";
			this.toolStripRefreshButton.Click += new System.EventHandler (this.toolStripRefreshButton_Click);
			// 
			// toolStripRemoveButton
			// 
			resources.ApplyResources (this.toolStripRemoveButton, "toolStripRemoveButton");
			this.toolStripRemoveButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripRemoveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripRemoveButton.Image = global::roxUp.Properties.Resources.DoubleLeftArrowHS;
			this.toolStripRemoveButton.Name = "toolStripRemoveButton";
			this.toolStripRemoveButton.Click += new System.EventHandler (this.toolStripRemoveButton_Click);
			// 
			// toolStripMetaDataButton
			// 
			resources.ApplyResources (this.toolStripMetaDataButton, "toolStripMetaDataButton");
			this.toolStripMetaDataButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripMetaDataButton.Image = global::roxUp.Properties.Resources.EDITGRID;
			this.toolStripMetaDataButton.Name = "toolStripMetaDataButton";
			this.toolStripMetaDataButton.Click += new System.EventHandler (this.toolStripMetaDataButton_Click);
			// 
			// toolStripAddButton
			// 
			resources.ApplyResources (this.toolStripAddButton, "toolStripAddButton");
			this.toolStripAddButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripAddButton.DropDownItems.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.toolStripAddZipItem});
			this.toolStripAddButton.Image = global::roxUp.Properties.Resources.DoubleRightArrowHS;
			this.toolStripAddButton.Name = "toolStripAddButton";
			this.toolStripAddButton.ButtonClick += new System.EventHandler (this.toolStripAddButton_Click);
			// 
			// toolStripAddZipItem
			// 
			resources.ApplyResources (this.toolStripAddZipItem, "toolStripAddZipItem");
			this.toolStripAddZipItem.Name = "toolStripAddZipItem";
			this.toolStripAddZipItem.Click += new System.EventHandler (this.toolStripAddButton_Click);
			// 
			// uploadWorker
			// 
			this.uploadWorker.WorkerReportsProgress = true;
			this.uploadWorker.WorkerSupportsCancellation = true;
			this.uploadWorker.DoWork += new System.ComponentModel.DoWorkEventHandler (this.uploadWorker_DoWork);
			this.uploadWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler (this.uploadWorker_RunWorkerCompleted);
			// 
			// fileSystemWatcher
			// 
			this.fileSystemWatcher.EnableRaisingEvents = true;
			this.fileSystemWatcher.Filter = "*";
			this.fileSystemWatcher.NotifyFilter = ((System.IO.NotifyFilters) ((((((((System.IO.NotifyFilters.FileName | System.IO.NotifyFilters.DirectoryName)
						| System.IO.NotifyFilters.Attributes)
						| System.IO.NotifyFilters.Size)
						| System.IO.NotifyFilters.LastWrite)
						| System.IO.NotifyFilters.LastAccess)
						| System.IO.NotifyFilters.CreationTime)
						| System.IO.NotifyFilters.Security)));
			this.fileSystemWatcher.SynchronizingObject = this;
			this.fileSystemWatcher.Changed += new System.IO.FileSystemEventHandler (this.fileSystemWatcher_Event);
			this.fileSystemWatcher.Created += new System.IO.FileSystemEventHandler (this.fileSystemWatcher_Event);
			this.fileSystemWatcher.Deleted += new System.IO.FileSystemEventHandler (this.fileSystemWatcher_Event);
			this.fileSystemWatcher.Renamed += new System.IO.RenamedEventHandler (this.fileSystemWatcher_Renamed);
			// 
			// openFileDialog
			// 
			this.openFileDialog.DefaultExt = "rox";
			resources.ApplyResources (this.openFileDialog, "openFileDialog");
			this.openFileDialog.SupportMultiDottedExtensions = true;
			// 
			// saveFileDialog
			// 
			this.saveFileDialog.DefaultExt = "rox";
			resources.ApplyResources (this.saveFileDialog, "saveFileDialog");
			this.saveFileDialog.SupportMultiDottedExtensions = true;
			// 
			// MultiExplorerControl
			// 
			resources.ApplyResources (this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add (this.uploadSplitContainer);
			this.Controls.Add (this.toolStrip);
			this.DoubleBuffered = true;
			this.Name = "MultiExplorerControl";
			this.splitContainer.Panel1.ResumeLayout (false);
			this.splitContainer.Panel2.ResumeLayout (false);
			this.splitContainer.ResumeLayout (false);
			this.browseGroupBox.ResumeLayout (false);
			this.browseGroupBox.PerformLayout ();
			this.browseContextMenuStrip.ResumeLayout (false);
			this.browseToolStrip.ResumeLayout (false);
			this.browseToolStrip.PerformLayout ();
			this.uploadGroupBox.ResumeLayout (false);
			this.zipPanel.ResumeLayout (false);
			this.uploadSplitContainer.Panel1.ResumeLayout (false);
			this.uploadSplitContainer.Panel2.ResumeLayout (false);
			this.uploadSplitContainer.ResumeLayout (false);
			this.metaGroupBox.ResumeLayout (false);
			this.metaSplitContainer.Panel1.ResumeLayout (false);
			this.metaSplitContainer.Panel2.ResumeLayout (false);
			this.metaSplitContainer.ResumeLayout (false);
			this.toolStrip.ResumeLayout (false);
			this.toolStrip.PerformLayout ();
			((System.ComponentModel.ISupportInitialize) (this.fileSystemWatcher)).EndInit ();
			this.ResumeLayout (false);
			this.PerformLayout ();

		}

		#endregion

		internal System.Windows.Forms.SplitContainer splitContainer;
		internal System.Windows.Forms.GroupBox browseGroupBox;
		internal System.Windows.Forms.GroupBox uploadGroupBox;
		internal System.Windows.Forms.ToolStrip toolStrip;
		internal System.Windows.Forms.ToolStripSeparator toolStripRightSeparator;
		internal System.Windows.Forms.ToolStripButton toolStripRemoveButton;
		internal System.Windows.Forms.ToolStripSeparator toolStripLeftSeparator;
		internal System.Windows.Forms.ListView uploadListView;
		internal System.Windows.Forms.ToolStripButton toolStripRefreshButton;
		internal System.Windows.Forms.ImageList iconList;
		internal System.Windows.Forms.ColumnHeader uploadNameColumnHeader;
		internal System.Windows.Forms.ColumnHeader uploadDescriptionColumnHeader;
		internal System.ComponentModel.BackgroundWorker uploadWorker;
		internal System.Windows.Forms.ToolStripButton toolStripCancelButton;
		internal System.Windows.Forms.ToolStripSplitButton toolStripPathButton;
		internal System.Windows.Forms.ToolStripMenuItem toolStripAddFolderItem;
		internal System.Windows.Forms.Panel zipPanel;
		internal System.Windows.Forms.Label zipLabel;
		internal System.Windows.Forms.CheckBox zipCheckBox;
		internal System.Windows.Forms.ToolStripSplitButton toolStripAddButton;
		internal System.Windows.Forms.ToolStripMenuItem toolStripAddZipItem;
		internal System.Windows.Forms.ToolStripDropDownButton toolStripUserItem;
		internal System.Windows.Forms.ToolStripSeparator toolStripUserSeparator;
		internal System.Windows.Forms.ToolStripMenuItem toolStripUserForgetItem;
		internal System.Windows.Forms.SplitContainer uploadSplitContainer;
		internal System.Windows.Forms.GroupBox metaGroupBox;
		internal System.Windows.Forms.SplitContainer metaSplitContainer;
		internal System.Windows.Forms.PropertyGrid propertyGrid;
		internal System.Windows.Forms.ToolStripButton toolStripMetaDataButton;
		private System.Windows.Forms.ColumnHeader metaTitleColumn;
		private System.Windows.Forms.ColumnHeader metaNameColumn;
		private System.Windows.Forms.ColumnHeader metaTypeColumn;
		internal System.Windows.Forms.ListView metaListView;
		private System.Windows.Forms.ToolStrip browseToolStrip;
		private System.Windows.Forms.ToolStripButton browseUpToolStripButton;
		private System.Windows.Forms.ToolStripButton browseFoldersToolStripButton;
		private System.Windows.Forms.ToolStripDropDownButton browseFiltersToolStripButton;
		private System.Windows.Forms.ToolStripMenuItem browseFiltersTemplatesToolStripItem;
		private System.Windows.Forms.ToolStripMenuItem browseFiltersWebPartsToolStripItem;
		private System.Windows.Forms.ToolStripMenuItem browseFiltersImagesToolStripItem;
		private System.Windows.Forms.ToolStripSeparator browseSeparatorToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem browseFiltersNoneToolStripItem;
		private System.Windows.Forms.ContextMenuStrip browseContextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem contextAddItem;
		internal System.Windows.Forms.ToolStripMenuItem contextAddZipItem;
		internal System.Windows.Forms.ToolStripMenuItem contextFolderItem;
		private System.Windows.Forms.ToolStripSeparator contextSep2Item;
		private System.Windows.Forms.ToolStripSeparator contextSep3Item;
		private System.Windows.Forms.ToolStripMenuItem contextOpenItem;
		private System.Windows.Forms.ToolStripMenuItem contextDeleteItem;
		private System.Windows.Forms.ToolStripMenuItem contextRefreshItem;
		private System.Windows.Forms.ToolStripMenuItem contextPropertiesItem;
		internal System.IO.FileSystemWatcher fileSystemWatcher;
		private System.Windows.Forms.ToolStripSplitButton browseBackToolStripButton;
		private System.Windows.Forms.ToolStripMenuItem contextOpenWithItem;
		private System.Windows.Forms.ToolStripMenuItem browseFiltersDocumentsToolStripItem;
		internal System.Windows.Forms.ListView browseListView;
		internal System.Windows.Forms.ColumnHeader browseNameColumnHeader;
		internal System.Windows.Forms.ColumnHeader browseDescriptionColumnHeader;
		private System.Windows.Forms.ToolStripMenuItem browseFiltersMastersToolStripItem;
		private System.Windows.Forms.ToolStripSeparator browseSeparatorToolStripItem;
		private System.Windows.Forms.ToolStripDropDownButton browseHiddenToolStripItem;
		private System.Windows.Forms.ToolStripMenuItem browseFiltersArchivesToolStripItem;
		internal System.Windows.Forms.ImageList fileIconList;
		internal System.Windows.Forms.ToolStripSplitButton toolStripUploadButton;
		private System.Windows.Forms.ToolStripMenuItem toolStripUploadSaveItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripUploadOpenItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripUploadRecentItem;
		private System.Windows.Forms.ToolStripSeparator toolStripFolderSeparatorItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripSelectAllItem;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.WebBrowser uploadWebBrowser;
		private System.Windows.Forms.ToolStripMenuItem toolStripCheckinItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripCheckInNoneItem;
		private System.Windows.Forms.ToolStripSeparator toolStripCheckInSepItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripCheckInMajorItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripCheckInMinorItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripCheckInOverwriteItem;
		private System.Windows.Forms.ToolStripSeparator toolStripUploadSepItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripCheckOutItem;
		private System.Windows.Forms.ToolStripSplitButton toolStripDriveItem;

	}
}
