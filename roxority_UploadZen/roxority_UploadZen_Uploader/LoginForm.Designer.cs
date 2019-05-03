namespace roxUp {
	partial class LoginForm {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (LoginForm));
			this.label = new System.Windows.Forms.Label ();
			this.userNameLabel = new System.Windows.Forms.Label ();
			this.userNameTextBox = new System.Windows.Forms.TextBox ();
			this.passwordLabel = new System.Windows.Forms.Label ();
			this.passwordTextBox = new System.Windows.Forms.TextBox ();
			this.cancelButton = new System.Windows.Forms.Button ();
			this.okButton = new System.Windows.Forms.Button ();
			this.checkBox = new System.Windows.Forms.CheckBox ();
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
			// userNameLabel
			// 
			this.userNameLabel.AccessibleDescription = null;
			this.userNameLabel.AccessibleName = null;
			resources.ApplyResources (this.userNameLabel, "userNameLabel");
			this.userNameLabel.Font = null;
			this.userNameLabel.Name = "userNameLabel";
			// 
			// userNameTextBox
			// 
			this.userNameTextBox.AccessibleDescription = null;
			this.userNameTextBox.AccessibleName = null;
			resources.ApplyResources (this.userNameTextBox, "userNameTextBox");
			this.userNameTextBox.BackgroundImage = null;
			this.userNameTextBox.Font = null;
			this.userNameTextBox.Name = "userNameTextBox";
			// 
			// passwordLabel
			// 
			this.passwordLabel.AccessibleDescription = null;
			this.passwordLabel.AccessibleName = null;
			resources.ApplyResources (this.passwordLabel, "passwordLabel");
			this.passwordLabel.Font = null;
			this.passwordLabel.Name = "passwordLabel";
			// 
			// passwordTextBox
			// 
			this.passwordTextBox.AccessibleDescription = null;
			this.passwordTextBox.AccessibleName = null;
			resources.ApplyResources (this.passwordTextBox, "passwordTextBox");
			this.passwordTextBox.BackgroundImage = null;
			this.passwordTextBox.Font = null;
			this.passwordTextBox.Name = "passwordTextBox";
			this.passwordTextBox.UseSystemPasswordChar = true;
			// 
			// cancelButton
			// 
			this.cancelButton.AccessibleDescription = null;
			this.cancelButton.AccessibleName = null;
			resources.ApplyResources (this.cancelButton, "cancelButton");
			this.cancelButton.BackgroundImage = null;
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Font = null;
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// okButton
			// 
			this.okButton.AccessibleDescription = null;
			this.okButton.AccessibleName = null;
			resources.ApplyResources (this.okButton, "okButton");
			this.okButton.BackgroundImage = null;
			this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.okButton.Font = null;
			this.okButton.Name = "okButton";
			this.okButton.UseVisualStyleBackColor = true;
			// 
			// checkBox
			// 
			this.checkBox.AccessibleDescription = null;
			this.checkBox.AccessibleName = null;
			resources.ApplyResources (this.checkBox, "checkBox");
			this.checkBox.BackgroundImage = null;
			this.checkBox.Font = null;
			this.checkBox.Name = "checkBox";
			this.checkBox.UseVisualStyleBackColor = true;
			// 
			// LoginForm
			// 
			this.AcceptButton = this.okButton;
			this.AccessibleDescription = null;
			this.AccessibleName = null;
			resources.ApplyResources (this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImage = null;
			this.CancelButton = this.cancelButton;
			this.Controls.Add (this.checkBox);
			this.Controls.Add (this.okButton);
			this.Controls.Add (this.cancelButton);
			this.Controls.Add (this.passwordTextBox);
			this.Controls.Add (this.userNameTextBox);
			this.Controls.Add (this.passwordLabel);
			this.Controls.Add (this.userNameLabel);
			this.Controls.Add (this.label);
			this.DoubleBuffered = true;
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LoginForm";
			this.ShowInTaskbar = false;
			this.ResumeLayout (false);
			this.PerformLayout ();

		}

		#endregion

		private System.Windows.Forms.Label label;
		private System.Windows.Forms.Label userNameLabel;
		private System.Windows.Forms.Label passwordLabel;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button okButton;
		internal System.Windows.Forms.TextBox userNameTextBox;
		internal System.Windows.Forms.TextBox passwordTextBox;
		internal System.Windows.Forms.CheckBox checkBox;

	}
}