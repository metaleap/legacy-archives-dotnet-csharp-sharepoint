namespace LegacyMigration {
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
			this.splitContainer = new System.Windows.Forms.SplitContainer ();
			this.treeView = new System.Windows.Forms.TreeView ();
			this.textBox = new System.Windows.Forms.TextBox ();
			this.toolStrip = new System.Windows.Forms.ToolStrip ();
			this.toolStripScanButton = new System.Windows.Forms.ToolStripButton ();
			this.toolStripSaveButton = new System.Windows.Forms.ToolStripButton ();
			this.backgroundWorker = new System.ComponentModel.BackgroundWorker ();
			this.comboBox = new System.Windows.Forms.ComboBox ();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog ();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer ();
			this.richTextBox = new System.Windows.Forms.RichTextBox ();
			this.splitContainer.Panel1.SuspendLayout ();
			this.splitContainer.Panel2.SuspendLayout ();
			this.splitContainer.SuspendLayout ();
			this.toolStrip.SuspendLayout ();
			this.splitContainer1.Panel1.SuspendLayout ();
			this.splitContainer1.Panel2.SuspendLayout ();
			this.splitContainer1.SuspendLayout ();
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
			this.splitContainer.Panel1.Controls.Add (this.treeView);
			// 
			// splitContainer.Panel2
			// 
			resources.ApplyResources (this.splitContainer.Panel2, "splitContainer.Panel2");
			this.splitContainer.Panel2.Controls.Add (this.textBox);
			// 
			// treeView
			// 
			resources.ApplyResources (this.treeView, "treeView");
			this.treeView.FullRowSelect = true;
			this.treeView.HideSelection = false;
			this.treeView.HotTracking = true;
			this.treeView.Name = "treeView";
			this.treeView.ShowLines = false;
			this.treeView.ShowNodeToolTips = true;
			this.treeView.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler (this.treeView_NodeMouseDoubleClick);
			// 
			// textBox
			// 
			resources.ApplyResources (this.textBox, "textBox");
			this.textBox.Name = "textBox";
			this.textBox.ReadOnly = true;
			// 
			// toolStrip
			// 
			resources.ApplyResources (this.toolStrip, "toolStrip");
			this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip.Items.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.toolStripScanButton,
            this.toolStripSaveButton});
			this.toolStrip.Name = "toolStrip";
			// 
			// toolStripScanButton
			// 
			resources.ApplyResources (this.toolStripScanButton, "toolStripScanButton");
			this.toolStripScanButton.Image = global::LegacyMigration.Properties.Resources.arrow_rotate_clockwise;
			this.toolStripScanButton.Name = "toolStripScanButton";
			this.toolStripScanButton.Click += new System.EventHandler (this.toolStripScanButton_Click);
			// 
			// toolStripSaveButton
			// 
			resources.ApplyResources (this.toolStripSaveButton, "toolStripSaveButton");
			this.toolStripSaveButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.toolStripSaveButton.Image = global::LegacyMigration.Properties.Resources.disk;
			this.toolStripSaveButton.Name = "toolStripSaveButton";
			this.toolStripSaveButton.Click += new System.EventHandler (this.toolStripSaveButton_Click);
			// 
			// backgroundWorker
			// 
			this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler (this.backgroundWorker_DoWork);
			this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler (this.backgroundWorker_RunWorkerCompleted);
			// 
			// comboBox
			// 
			resources.ApplyResources (this.comboBox, "comboBox");
			this.comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.comboBox.FormattingEnabled = true;
			this.comboBox.Items.AddRange (new object [] {
            resources.GetString("comboBox.Items"),
            resources.GetString("comboBox.Items1")});
			this.comboBox.Name = "comboBox";
			// 
			// saveFileDialog
			// 
			this.saveFileDialog.AddExtension = false;
			this.saveFileDialog.DefaultExt = "rox";
			this.saveFileDialog.FileName = "roxority_ExportZen.export.rox";
			resources.ApplyResources (this.saveFileDialog, "saveFileDialog");
			this.saveFileDialog.OverwritePrompt = false;
			this.saveFileDialog.SupportMultiDottedExtensions = true;
			// 
			// splitContainer1
			// 
			resources.ApplyResources (this.splitContainer1, "splitContainer1");
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			resources.ApplyResources (this.splitContainer1.Panel1, "splitContainer1.Panel1");
			this.splitContainer1.Panel1.Controls.Add (this.richTextBox);
			// 
			// splitContainer1.Panel2
			// 
			resources.ApplyResources (this.splitContainer1.Panel2, "splitContainer1.Panel2");
			this.splitContainer1.Panel2.Controls.Add (this.splitContainer);
			this.splitContainer1.Panel2.Controls.Add (this.toolStrip);
			this.splitContainer1.Panel2.Controls.Add (this.comboBox);
			// 
			// richTextBox
			// 
			resources.ApplyResources (this.richTextBox, "richTextBox");
			this.richTextBox.BackColor = System.Drawing.SystemColors.Info;
			this.richTextBox.ForeColor = System.Drawing.SystemColors.InfoText;
			this.richTextBox.Name = "richTextBox";
			this.richTextBox.ReadOnly = true;
			// 
			// MainForm
			// 
			resources.ApplyResources (this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add (this.splitContainer1);
			this.Name = "MainForm";
			this.splitContainer.Panel1.ResumeLayout (false);
			this.splitContainer.Panel2.ResumeLayout (false);
			this.splitContainer.Panel2.PerformLayout ();
			this.splitContainer.ResumeLayout (false);
			this.toolStrip.ResumeLayout (false);
			this.toolStrip.PerformLayout ();
			this.splitContainer1.Panel1.ResumeLayout (false);
			this.splitContainer1.Panel2.ResumeLayout (false);
			this.splitContainer1.Panel2.PerformLayout ();
			this.splitContainer1.ResumeLayout (false);
			this.ResumeLayout (false);

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip;
		private System.Windows.Forms.ToolStripButton toolStripScanButton;
		private System.Windows.Forms.SplitContainer splitContainer;
		private System.Windows.Forms.TreeView treeView;
		private System.Windows.Forms.TextBox textBox;
		private System.Windows.Forms.ToolStripButton toolStripSaveButton;
		private System.ComponentModel.BackgroundWorker backgroundWorker;
		private System.Windows.Forms.ComboBox comboBox;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.RichTextBox richTextBox;
	}
}

