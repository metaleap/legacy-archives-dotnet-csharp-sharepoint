namespace roxUp {
	partial class MainForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose (bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose ();
			}
			base.Dispose (disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent () {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (MainForm));
			this.tabControl = new System.Windows.Forms.TabControl ();
			this.uploadTabPage = new System.Windows.Forms.TabPage ();
			this.multiExplorerControl = new roxUp.MultiExplorerControl ();
			this.prepTabPage = new System.Windows.Forms.TabPage ();
			this.propertyGrid = new System.Windows.Forms.PropertyGrid ();
			this.statusStrip = new System.Windows.Forms.StatusStrip ();
			this.statusSiteItem = new System.Windows.Forms.ToolStripStatusLabel ();
			this.statusListItem = new System.Windows.Forms.ToolStripDropDownButton ();
			this.statusFolderItem = new System.Windows.Forms.ToolStripDropDownButton ();
			this.statusInfoLabel = new System.Windows.Forms.ToolStripStatusLabel ();
			this.statusHelpLabel = new System.Windows.Forms.ToolStripStatusLabel ();
			this.statusHelpLinkLabel = new System.Windows.Forms.ToolStripStatusLabel ();
			this.tabControl.SuspendLayout ();
			this.uploadTabPage.SuspendLayout ();
			this.prepTabPage.SuspendLayout ();
			this.statusStrip.SuspendLayout ();
			this.SuspendLayout ();
			// 
			// tabControl
			// 
			resources.ApplyResources (this.tabControl, "tabControl");
			this.tabControl.Controls.Add (this.uploadTabPage);
			this.tabControl.Controls.Add (this.prepTabPage);
			this.tabControl.Name = "tabControl";
			this.tabControl.SelectedIndex = 0;
			// 
			// uploadTabPage
			// 
			resources.ApplyResources (this.uploadTabPage, "uploadTabPage");
			this.uploadTabPage.Controls.Add (this.multiExplorerControl);
			this.uploadTabPage.Name = "uploadTabPage";
			this.uploadTabPage.UseVisualStyleBackColor = true;
			// 
			// multiExplorerControl
			// 
			resources.ApplyResources (this.multiExplorerControl, "multiExplorerControl");
			this.multiExplorerControl.Name = "multiExplorerControl";
			this.multiExplorerControl.UploadBusy += new System.EventHandler (this.multiExplorerControl_UploadBusy);
			this.multiExplorerControl.UploadNonBusy += new System.EventHandler (this.multiExplorerControl_UploadNonBusy);
			// 
			// prepTabPage
			// 
			resources.ApplyResources (this.prepTabPage, "prepTabPage");
			this.prepTabPage.Controls.Add (this.propertyGrid);
			this.prepTabPage.Name = "prepTabPage";
			this.prepTabPage.UseVisualStyleBackColor = true;
			// 
			// propertyGrid
			// 
			resources.ApplyResources (this.propertyGrid, "propertyGrid");
			this.propertyGrid.HelpBackColor = System.Drawing.SystemColors.Info;
			this.propertyGrid.HelpForeColor = System.Drawing.SystemColors.InfoText;
			this.propertyGrid.Name = "propertyGrid";
			this.propertyGrid.ToolbarVisible = false;
			// 
			// statusStrip
			// 
			resources.ApplyResources (this.statusStrip, "statusStrip");
			this.statusStrip.Items.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.statusSiteItem,
            this.statusListItem,
            this.statusFolderItem,
            this.statusInfoLabel,
            this.statusHelpLabel,
            this.statusHelpLinkLabel});
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.statusStrip.ShowItemToolTips = true;
			// 
			// statusSiteItem
			// 
			resources.ApplyResources (this.statusSiteItem, "statusSiteItem");
			this.statusSiteItem.Image = global::roxUp.Properties.Resources.STSICON;
			this.statusSiteItem.Name = "statusSiteItem";
			// 
			// statusListItem
			// 
			resources.ApplyResources (this.statusListItem, "statusListItem");
			this.statusListItem.AutoToolTip = false;
			this.statusListItem.Image = global::roxUp.Properties.Resources.sts_list_genericlist16;
			this.statusListItem.Name = "statusListItem";
			this.statusListItem.ShowDropDownArrow = false;
			// 
			// statusFolderItem
			// 
			resources.ApplyResources (this.statusFolderItem, "statusFolderItem");
			this.statusFolderItem.AutoToolTip = false;
			this.statusFolderItem.Image = global::roxUp.Properties.Resources.FOLDER1;
			this.statusFolderItem.Name = "statusFolderItem";
			this.statusFolderItem.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			this.statusFolderItem.ShowDropDownArrow = false;
			// 
			// statusInfoLabel
			// 
			resources.ApplyResources (this.statusInfoLabel, "statusInfoLabel");
			this.statusInfoLabel.ForeColor = System.Drawing.SystemColors.GrayText;
			this.statusInfoLabel.Name = "statusInfoLabel";
			this.statusInfoLabel.Spring = true;
			// 
			// statusHelpLabel
			// 
			resources.ApplyResources (this.statusHelpLabel, "statusHelpLabel");
			this.statusHelpLabel.IsLink = true;
			this.statusHelpLabel.LinkColor = System.Drawing.SystemColors.ControlText;
			this.statusHelpLabel.Name = "statusHelpLabel";
			this.statusHelpLabel.Click += new System.EventHandler (this.statusHelpLabel_Click);
			// 
			// statusHelpLinkLabel
			// 
			resources.ApplyResources (this.statusHelpLinkLabel, "statusHelpLinkLabel");
			this.statusHelpLinkLabel.Image = global::roxUp.Properties.Resources.Help;
			this.statusHelpLinkLabel.IsLink = true;
			this.statusHelpLinkLabel.Name = "statusHelpLinkLabel";
			this.statusHelpLinkLabel.Click += new System.EventHandler (this.statusLinkLabel_Click);
			// 
			// MainForm
			// 
			resources.ApplyResources (this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add (this.tabControl);
			this.Controls.Add (this.statusStrip);
			this.DoubleBuffered = true;
			this.Name = "MainForm";
			this.tabControl.ResumeLayout (false);
			this.uploadTabPage.ResumeLayout (false);
			this.prepTabPage.ResumeLayout (false);
			this.statusStrip.ResumeLayout (false);
			this.statusStrip.PerformLayout ();
			this.ResumeLayout (false);
			this.PerformLayout ();

		}

		#endregion

		internal MultiExplorerControl multiExplorerControl;
		internal System.Windows.Forms.TabControl tabControl;
		internal System.Windows.Forms.TabPage prepTabPage;
		internal System.Windows.Forms.TabPage uploadTabPage;
		internal System.Windows.Forms.PropertyGrid propertyGrid;
		internal System.Windows.Forms.StatusStrip statusStrip;
		internal System.Windows.Forms.ToolStripStatusLabel statusHelpLabel;
		internal System.Windows.Forms.ToolStripStatusLabel statusHelpLinkLabel;
		internal System.Windows.Forms.ToolStripDropDownButton statusListItem;
		internal System.Windows.Forms.ToolStripDropDownButton statusFolderItem;
		internal System.Windows.Forms.ToolStripStatusLabel statusInfoLabel;
		internal System.Windows.Forms.ToolStripStatusLabel statusSiteItem;

	}
}

