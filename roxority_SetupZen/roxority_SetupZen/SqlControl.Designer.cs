namespace roxority_SetupZen {
	partial class SqlControl {
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (SqlControl));
			this.sqlInfoLabel = new System.Windows.Forms.Label ();
			this.sqlDataSourceLabel = new System.Windows.Forms.Label ();
			this.sqlDataSourceComboBox = new System.Windows.Forms.ComboBox ();
			this.sqlDatabaseLabel = new System.Windows.Forms.Label ();
			this.sqlDataSourceStatusLabel = new System.Windows.Forms.Label ();
			this.sqlButton = new System.Windows.Forms.Button ();
			this.sqlDatabaseStatusLabel = new System.Windows.Forms.Label ();
			this.sqlDatabaseTextBox = new System.Windows.Forms.TextBox ();
			this.SuspendLayout ();
			// 
			// sqlInfoLabel
			// 
			this.sqlInfoLabel.AccessibleDescription = null;
			this.sqlInfoLabel.AccessibleName = null;
			resources.ApplyResources (this.sqlInfoLabel, "sqlInfoLabel");
			this.sqlInfoLabel.Font = null;
			this.sqlInfoLabel.Name = "sqlInfoLabel";
			// 
			// sqlDataSourceLabel
			// 
			this.sqlDataSourceLabel.AccessibleDescription = null;
			this.sqlDataSourceLabel.AccessibleName = null;
			resources.ApplyResources (this.sqlDataSourceLabel, "sqlDataSourceLabel");
			this.sqlDataSourceLabel.Font = null;
			this.sqlDataSourceLabel.Name = "sqlDataSourceLabel";
			// 
			// sqlDataSourceComboBox
			// 
			this.sqlDataSourceComboBox.AccessibleDescription = null;
			this.sqlDataSourceComboBox.AccessibleName = null;
			resources.ApplyResources (this.sqlDataSourceComboBox, "sqlDataSourceComboBox");
			this.sqlDataSourceComboBox.BackgroundImage = null;
			this.sqlDataSourceComboBox.Font = null;
			this.sqlDataSourceComboBox.FormattingEnabled = true;
			this.sqlDataSourceComboBox.Name = "sqlDataSourceComboBox";
			this.sqlDataSourceComboBox.TextChanged += new System.EventHandler (this.sqlDatabaseTextBox_TextChanged);
			// 
			// sqlDatabaseLabel
			// 
			this.sqlDatabaseLabel.AccessibleDescription = null;
			this.sqlDatabaseLabel.AccessibleName = null;
			resources.ApplyResources (this.sqlDatabaseLabel, "sqlDatabaseLabel");
			this.sqlDatabaseLabel.Font = null;
			this.sqlDatabaseLabel.Name = "sqlDatabaseLabel";
			// 
			// sqlDataSourceStatusLabel
			// 
			this.sqlDataSourceStatusLabel.AccessibleDescription = null;
			this.sqlDataSourceStatusLabel.AccessibleName = null;
			resources.ApplyResources (this.sqlDataSourceStatusLabel, "sqlDataSourceStatusLabel");
			this.sqlDataSourceStatusLabel.Font = null;
			this.sqlDataSourceStatusLabel.Name = "sqlDataSourceStatusLabel";
			// 
			// sqlButton
			// 
			this.sqlButton.AccessibleDescription = null;
			this.sqlButton.AccessibleName = null;
			resources.ApplyResources (this.sqlButton, "sqlButton");
			this.sqlButton.BackgroundImage = null;
			this.sqlButton.Font = null;
			this.sqlButton.Name = "sqlButton";
			this.sqlButton.UseVisualStyleBackColor = true;
			this.sqlButton.Click += new System.EventHandler (this.sqlButton_Click);
			// 
			// sqlDatabaseStatusLabel
			// 
			this.sqlDatabaseStatusLabel.AccessibleDescription = null;
			this.sqlDatabaseStatusLabel.AccessibleName = null;
			resources.ApplyResources (this.sqlDatabaseStatusLabel, "sqlDatabaseStatusLabel");
			this.sqlDatabaseStatusLabel.Font = null;
			this.sqlDatabaseStatusLabel.Name = "sqlDatabaseStatusLabel";
			// 
			// sqlDatabaseTextBox
			// 
			this.sqlDatabaseTextBox.AccessibleDescription = null;
			this.sqlDatabaseTextBox.AccessibleName = null;
			resources.ApplyResources (this.sqlDatabaseTextBox, "sqlDatabaseTextBox");
			this.sqlDatabaseTextBox.BackgroundImage = null;
			this.sqlDatabaseTextBox.Font = null;
			this.sqlDatabaseTextBox.Name = "sqlDatabaseTextBox";
			this.sqlDatabaseTextBox.TextChanged += new System.EventHandler (this.sqlDatabaseTextBox_TextChanged);
			// 
			// SqlControl
			// 
			this.AccessibleDescription = null;
			this.AccessibleName = null;
			resources.ApplyResources (this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.BackgroundImage = null;
			this.Controls.Add (this.sqlDatabaseTextBox);
			this.Controls.Add (this.sqlButton);
			this.Controls.Add (this.sqlDatabaseStatusLabel);
			this.Controls.Add (this.sqlDataSourceStatusLabel);
			this.Controls.Add (this.sqlDataSourceComboBox);
			this.Controls.Add (this.sqlDatabaseLabel);
			this.Controls.Add (this.sqlDataSourceLabel);
			this.Controls.Add (this.sqlInfoLabel);
			this.Font = null;
			this.Name = "SqlControl";
			this.ResumeLayout (false);
			this.PerformLayout ();

		}

		#endregion

		private System.Windows.Forms.Label sqlInfoLabel;
		private System.Windows.Forms.Label sqlDataSourceLabel;
		private System.Windows.Forms.ComboBox sqlDataSourceComboBox;
		private System.Windows.Forms.Label sqlDatabaseLabel;
		private System.Windows.Forms.Label sqlDataSourceStatusLabel;
		private System.Windows.Forms.Button sqlButton;
		private System.Windows.Forms.Label sqlDatabaseStatusLabel;
		private System.Windows.Forms.TextBox sqlDatabaseTextBox;
	}
}
