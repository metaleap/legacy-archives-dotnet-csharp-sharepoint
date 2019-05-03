namespace roxority_SetupZen {
	partial class JavaForm {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (JavaForm));
			this.label = new System.Windows.Forms.Label ();
			this.progressBar = new System.Windows.Forms.ProgressBar ();
			this.SuspendLayout ();
			// 
			// label
			// 
			this.label.AccessibleDescription = null;
			this.label.AccessibleName = null;
			resources.ApplyResources (this.label, "label");
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
			this.progressBar.Name = "progressBar";
			this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
			this.progressBar.Value = 33;
			// 
			// JavaForm
			// 
			this.AccessibleDescription = null;
			this.AccessibleName = null;
			resources.ApplyResources (this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImage = null;
			this.ControlBox = false;
			this.Controls.Add (this.progressBar);
			this.Controls.Add (this.label);
			this.Font = null;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "JavaForm";
			this.ShowInTaskbar = false;
			this.ResumeLayout (false);

		}

		#endregion

		private System.Windows.Forms.Label label;
		private System.Windows.Forms.ProgressBar progressBar;
	}
}