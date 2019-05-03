namespace roxority_SetupZen
{
  partial class WelcomeControl
  {
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (WelcomeControl));
		this.messageLabel = new System.Windows.Forms.Label ();
		this.label1 = new System.Windows.Forms.Label ();
		this.langEnglishRadioButton = new System.Windows.Forms.RadioButton ();
		this.langGermanRadioButton = new System.Windows.Forms.RadioButton ();
		this.label2 = new System.Windows.Forms.Label ();
		this.langFrenchRadioButton = new System.Windows.Forms.RadioButton ();
		this.SuspendLayout ();
		// 
		// messageLabel
		// 
		this.messageLabel.AccessibleDescription = null;
		this.messageLabel.AccessibleName = null;
		resources.ApplyResources (this.messageLabel, "messageLabel");
		this.messageLabel.Font = null;
		this.messageLabel.Name = "messageLabel";
		// 
		// label1
		// 
		this.label1.AccessibleDescription = null;
		this.label1.AccessibleName = null;
		resources.ApplyResources (this.label1, "label1");
		this.label1.Font = null;
		this.label1.ForeColor = System.Drawing.SystemColors.GrayText;
		this.label1.Name = "label1";
		// 
		// langEnglishRadioButton
		// 
		this.langEnglishRadioButton.AccessibleDescription = null;
		this.langEnglishRadioButton.AccessibleName = null;
		resources.ApplyResources (this.langEnglishRadioButton, "langEnglishRadioButton");
		this.langEnglishRadioButton.BackgroundImage = null;
		this.langEnglishRadioButton.Checked = true;
		this.langEnglishRadioButton.Font = null;
		this.langEnglishRadioButton.Image = global::roxority_SetupZen.Properties.Images.gb;
		this.langEnglishRadioButton.Name = "langEnglishRadioButton";
		this.langEnglishRadioButton.TabStop = true;
		this.langEnglishRadioButton.Tag = "en-US";
		this.langEnglishRadioButton.UseVisualStyleBackColor = true;
		this.langEnglishRadioButton.CheckedChanged += new System.EventHandler (this.langRadioButton_CheckedChanged);
		// 
		// langGermanRadioButton
		// 
		this.langGermanRadioButton.AccessibleDescription = null;
		this.langGermanRadioButton.AccessibleName = null;
		resources.ApplyResources (this.langGermanRadioButton, "langGermanRadioButton");
		this.langGermanRadioButton.BackgroundImage = null;
		this.langGermanRadioButton.Font = null;
		this.langGermanRadioButton.Image = global::roxority_SetupZen.Properties.Images.de;
		this.langGermanRadioButton.Name = "langGermanRadioButton";
		this.langGermanRadioButton.TabStop = true;
		this.langGermanRadioButton.Tag = "de-DE";
		this.langGermanRadioButton.UseVisualStyleBackColor = true;
		this.langGermanRadioButton.CheckedChanged += new System.EventHandler (this.langRadioButton_CheckedChanged);
		// 
		// label2
		// 
		this.label2.AccessibleDescription = null;
		this.label2.AccessibleName = null;
		resources.ApplyResources (this.label2, "label2");
		this.label2.Font = null;
		this.label2.Name = "label2";
		// 
		// langFrenchRadioButton
		// 
		this.langFrenchRadioButton.AccessibleDescription = null;
		this.langFrenchRadioButton.AccessibleName = null;
		resources.ApplyResources (this.langFrenchRadioButton, "langFrenchRadioButton");
		this.langFrenchRadioButton.BackgroundImage = null;
		this.langFrenchRadioButton.Font = null;
		this.langFrenchRadioButton.Image = global::roxority_SetupZen.Properties.Images.fr;
		this.langFrenchRadioButton.Name = "langFrenchRadioButton";
		this.langFrenchRadioButton.TabStop = true;
		this.langFrenchRadioButton.Tag = "fr-FR";
		this.langFrenchRadioButton.UseVisualStyleBackColor = true;
		this.langFrenchRadioButton.CheckedChanged += new System.EventHandler (this.langRadioButton_CheckedChanged);
		// 
		// WelcomeControl
		// 
		this.AccessibleDescription = null;
		this.AccessibleName = null;
		resources.ApplyResources (this, "$this");
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackgroundImage = null;
		this.Controls.Add (this.langFrenchRadioButton);
		this.Controls.Add (this.label2);
		this.Controls.Add (this.langGermanRadioButton);
		this.Controls.Add (this.langEnglishRadioButton);
		this.Controls.Add (this.label1);
		this.Controls.Add (this.messageLabel);
		this.Font = null;
		this.Name = "WelcomeControl";
		this.ResumeLayout (false);
		this.PerformLayout ();

    }

    #endregion

    private System.Windows.Forms.Label messageLabel;
	private System.Windows.Forms.Label label1;
	private System.Windows.Forms.RadioButton langEnglishRadioButton;
	private System.Windows.Forms.RadioButton langGermanRadioButton;
	private System.Windows.Forms.Label label2;
	private System.Windows.Forms.RadioButton langFrenchRadioButton;
  }
}
