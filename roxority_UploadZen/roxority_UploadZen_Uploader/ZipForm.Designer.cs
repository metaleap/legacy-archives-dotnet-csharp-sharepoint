namespace roxUp {
	partial class ZipForm {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose (bool disposing) {
			Ionic.Zip.ZipEntry.ExceptionNotification -= zipEntry_ExceptionNotification;
			if (disposing && (components != null)) {
				components.Dispose ();
			}
			base.Dispose (disposing);
			ZipFile.Dispose ();
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent () {
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (ZipForm));
			this.label = new System.Windows.Forms.Label ();
			this.progressBar = new System.Windows.Forms.ProgressBar ();
			this.button1 = new System.Windows.Forms.Button ();
			this.button2 = new System.Windows.Forms.Button ();
			this.SuspendLayout ();
			// 
			// label
			// 
			this.label.AccessibleDescription = null;
			this.label.AccessibleName = null;
			resources.ApplyResources (this.label, "label");
			this.label.AutoEllipsis = true;
			this.label.Font = null;
			this.label.Name = "label";
			// 
			// progressBar
			// 
			this.progressBar.AccessibleDescription = null;
			this.progressBar.AccessibleName = null;
			resources.ApplyResources (this.progressBar, "progressBar");
			this.progressBar.BackgroundImage = null;
			this.progressBar.Font = null;
			this.progressBar.MarqueeAnimationSpeed = 33;
			this.progressBar.Name = "progressBar";
			this.progressBar.Step = 50;
			this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.progressBar.Value = 50;
			// 
			// button1
			// 
			this.button1.AccessibleDescription = null;
			this.button1.AccessibleName = null;
			resources.ApplyResources (this.button1, "button1");
			this.button1.BackgroundImage = null;
			this.button1.DialogResult = System.Windows.Forms.DialogResult.Abort;
			this.button1.Font = null;
			this.button1.Name = "button1";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler (this.cancelButton_Click);
			// 
			// button2
			// 
			this.button2.AccessibleDescription = null;
			this.button2.AccessibleName = null;
			resources.ApplyResources (this.button2, "button2");
			this.button2.BackgroundImage = null;
			this.button2.DialogResult = System.Windows.Forms.DialogResult.Ignore;
			this.button2.Font = null;
			this.button2.Name = "button2";
			this.button2.UseVisualStyleBackColor = true;
			this.button2.Click += new System.EventHandler (this.cancelButton_Click);
			// 
			// ZipForm
			// 
			this.AccessibleDescription = null;
			this.AccessibleName = null;
			resources.ApplyResources (this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImage = null;
			this.ControlBox = false;
			this.Controls.Add (this.button2);
			this.Controls.Add (this.button1);
			this.Controls.Add (this.progressBar);
			this.Controls.Add (this.label);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.Name = "ZipForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout (false);

		}

		#endregion

		private System.Windows.Forms.Label label;
		private System.Windows.Forms.ProgressBar progressBar;
		private System.Windows.Forms.Button button1;
		private System.Windows.Forms.Button button2;
	}
}