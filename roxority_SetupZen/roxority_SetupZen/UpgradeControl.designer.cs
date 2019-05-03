namespace roxority_SetupZen
{
  partial class UpgradeControl
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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (UpgradeControl));
		this.upgradeRadioButton = new System.Windows.Forms.RadioButton ();
		this.removeRadioButton = new System.Windows.Forms.RadioButton ();
		this.messageLabel = new System.Windows.Forms.Label ();
		this.hintLabel = new System.Windows.Forms.Label ();
		this.upgradeDescriptionLabel = new System.Windows.Forms.Label ();
		this.removeDescriptionLabel = new System.Windows.Forms.Label ();
		this.removeHintLinkLabel = new System.Windows.Forms.LinkLabel ();
		this.SuspendLayout ();
		// 
		// upgradeRadioButton
		// 
		this.upgradeRadioButton.AccessibleDescription = null;
		this.upgradeRadioButton.AccessibleName = null;
		resources.ApplyResources (this.upgradeRadioButton, "upgradeRadioButton");
		this.upgradeRadioButton.BackgroundImage = null;
		this.upgradeRadioButton.Checked = true;
		this.upgradeRadioButton.Name = "upgradeRadioButton";
		this.upgradeRadioButton.TabStop = true;
		this.upgradeRadioButton.UseVisualStyleBackColor = true;
		this.upgradeRadioButton.CheckedChanged += new System.EventHandler (this.upgradeRadioButton_CheckedChanged);
		// 
		// removeRadioButton
		// 
		this.removeRadioButton.AccessibleDescription = null;
		this.removeRadioButton.AccessibleName = null;
		resources.ApplyResources (this.removeRadioButton, "removeRadioButton");
		this.removeRadioButton.BackgroundImage = null;
		this.removeRadioButton.Name = "removeRadioButton";
		this.removeRadioButton.UseVisualStyleBackColor = true;
		this.removeRadioButton.CheckedChanged += new System.EventHandler (this.removeRadioButton_CheckedChanged);
		// 
		// messageLabel
		// 
		this.messageLabel.AccessibleDescription = null;
		this.messageLabel.AccessibleName = null;
		resources.ApplyResources (this.messageLabel, "messageLabel");
		this.messageLabel.Font = null;
		this.messageLabel.Name = "messageLabel";
		// 
		// hintLabel
		// 
		this.hintLabel.AccessibleDescription = null;
		this.hintLabel.AccessibleName = null;
		resources.ApplyResources (this.hintLabel, "hintLabel");
		this.hintLabel.Font = null;
		this.hintLabel.Name = "hintLabel";
		// 
		// upgradeDescriptionLabel
		// 
		this.upgradeDescriptionLabel.AccessibleDescription = null;
		this.upgradeDescriptionLabel.AccessibleName = null;
		resources.ApplyResources (this.upgradeDescriptionLabel, "upgradeDescriptionLabel");
		this.upgradeDescriptionLabel.Font = null;
		this.upgradeDescriptionLabel.Name = "upgradeDescriptionLabel";
		// 
		// removeDescriptionLabel
		// 
		this.removeDescriptionLabel.AccessibleDescription = null;
		this.removeDescriptionLabel.AccessibleName = null;
		resources.ApplyResources (this.removeDescriptionLabel, "removeDescriptionLabel");
		this.removeDescriptionLabel.Font = null;
		this.removeDescriptionLabel.Name = "removeDescriptionLabel";
		// 
		// removeHintLinkLabel
		// 
		this.removeHintLinkLabel.AccessibleDescription = null;
		this.removeHintLinkLabel.AccessibleName = null;
		resources.ApplyResources (this.removeHintLinkLabel, "removeHintLinkLabel");
		this.removeHintLinkLabel.AutoEllipsis = true;
		this.removeHintLinkLabel.BackColor = System.Drawing.SystemColors.Info;
		this.removeHintLinkLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
		this.removeHintLinkLabel.Font = null;
		this.removeHintLinkLabel.ForeColor = System.Drawing.SystemColors.InfoText;
		this.removeHintLinkLabel.Name = "removeHintLinkLabel";
		this.removeHintLinkLabel.TabStop = true;
		this.removeHintLinkLabel.UseCompatibleTextRendering = true;
		this.removeHintLinkLabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler (this.removeHintLinkLabel_LinkClicked);
		// 
		// UpgradeControl
		// 
		this.AccessibleDescription = null;
		this.AccessibleName = null;
		resources.ApplyResources (this, "$this");
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackgroundImage = null;
		this.Controls.Add (this.removeHintLinkLabel);
		this.Controls.Add (this.removeDescriptionLabel);
		this.Controls.Add (this.upgradeDescriptionLabel);
		this.Controls.Add (this.hintLabel);
		this.Controls.Add (this.messageLabel);
		this.Controls.Add (this.removeRadioButton);
		this.Controls.Add (this.upgradeRadioButton);
		this.Font = null;
		this.Name = "UpgradeControl";
		this.ResumeLayout (false);
		this.PerformLayout ();

    }

    #endregion

    private System.Windows.Forms.RadioButton upgradeRadioButton;
    private System.Windows.Forms.RadioButton removeRadioButton;
    private System.Windows.Forms.Label messageLabel;
    private System.Windows.Forms.Label hintLabel;
    private System.Windows.Forms.Label upgradeDescriptionLabel;
    private System.Windows.Forms.Label removeDescriptionLabel;
	private System.Windows.Forms.LinkLabel removeHintLinkLabel;
  }
}
