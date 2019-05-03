namespace roxUp {
	partial class MessageControl {
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent () {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (MessageControl));
			this.toolStrip = new System.Windows.Forms.ToolStrip ();
			this.toolStripButton = new System.Windows.Forms.ToolStripButton ();
			this.toolStripLabel = new System.Windows.Forms.ToolStripLabel ();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator ();
			this.textBox = new System.Windows.Forms.RichTextBox ();
			this.toolStrip.SuspendLayout ();
			this.SuspendLayout ();
			// 
			// toolStrip
			// 
			this.toolStrip.AccessibleDescription = null;
			this.toolStrip.AccessibleName = null;
			resources.ApplyResources (this.toolStrip, "toolStrip");
			this.toolStrip.BackgroundImage = null;
			this.toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip.Items.AddRange (new System.Windows.Forms.ToolStripItem [] {
            this.toolStripButton,
            this.toolStripLabel,
            this.toolStripSeparator1});
			this.toolStrip.Name = "toolStrip";
			// 
			// toolStripButton
			// 
			this.toolStripButton.AccessibleDescription = null;
			this.toolStripButton.AccessibleName = null;
			this.toolStripButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			resources.ApplyResources (this.toolStripButton, "toolStripButton");
			this.toolStripButton.BackgroundImage = null;
			this.toolStripButton.Image = global::roxUp.Properties.Resources.DeleteHS;
			this.toolStripButton.Name = "toolStripButton";
			// 
			// toolStripLabel
			// 
			this.toolStripLabel.AccessibleDescription = null;
			this.toolStripLabel.AccessibleName = null;
			resources.ApplyResources (this.toolStripLabel, "toolStripLabel");
			this.toolStripLabel.BackgroundImage = null;
			this.toolStripLabel.ForeColor = System.Drawing.SystemColors.GrayText;
			this.toolStripLabel.Image = global::roxUp.Properties.Resources.ServiceNotInstalled;
			this.toolStripLabel.Name = "toolStripLabel";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.AccessibleDescription = null;
			this.toolStripSeparator1.AccessibleName = null;
			this.toolStripSeparator1.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			resources.ApplyResources (this.toolStripSeparator1, "toolStripSeparator1");
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			// 
			// textBox
			// 
			this.textBox.AccessibleDescription = null;
			this.textBox.AccessibleName = null;
			resources.ApplyResources (this.textBox, "textBox");
			this.textBox.AutoWordSelection = true;
			this.textBox.BackgroundImage = null;
			this.textBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBox.Font = null;
			this.textBox.Name = "textBox";
			this.textBox.ReadOnly = true;
			this.textBox.ShowSelectionMargin = true;
			// 
			// MessageControl
			// 
			this.AccessibleDescription = null;
			this.AccessibleName = null;
			resources.ApplyResources (this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImage = null;
			this.Controls.Add (this.textBox);
			this.Controls.Add (this.toolStrip);
			this.DoubleBuffered = true;
			this.Name = "MessageControl";
			this.toolStrip.ResumeLayout (false);
			this.toolStrip.PerformLayout ();
			this.ResumeLayout (false);
			this.PerformLayout ();

		}

		#endregion

		internal System.Windows.Forms.ToolStrip toolStrip;
		internal System.Windows.Forms.ToolStripButton toolStripButton;
		internal System.Windows.Forms.RichTextBox textBox;
		private System.Windows.Forms.ToolStripLabel toolStripLabel;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;

	}
}
