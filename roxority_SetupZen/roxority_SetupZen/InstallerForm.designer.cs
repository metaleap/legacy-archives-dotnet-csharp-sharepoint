namespace roxority_SetupZen
{
  partial class InstallerForm
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

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager (typeof (InstallerForm));
		this.contentPanel = new System.Windows.Forms.Panel ();
		this.buttonPanel = new System.Windows.Forms.TableLayoutPanel ();
		this.cancelButton = new System.Windows.Forms.Button ();
		this.prevButton = new System.Windows.Forms.Button ();
		this.nextButton = new System.Windows.Forms.Button ();
		this.vendorLabel = new System.Windows.Forms.Label ();
		this.topSeparatorPanel = new System.Windows.Forms.Panel ();
		this.bottomSeparatorPanel = new System.Windows.Forms.Panel ();
		this.titlePanel = new System.Windows.Forms.Panel ();
		this.logoPicture = new System.Windows.Forms.PictureBox ();
		this.titleLabel = new System.Windows.Forms.Label ();
		this.subTitleLabel = new System.Windows.Forms.Label ();
		this.buttonPanel.SuspendLayout ();
		this.titlePanel.SuspendLayout ();
		((System.ComponentModel.ISupportInitialize) (this.logoPicture)).BeginInit ();
		this.SuspendLayout ();
		// 
		// contentPanel
		// 
		this.contentPanel.AccessibleDescription = null;
		this.contentPanel.AccessibleName = null;
		resources.ApplyResources (this.contentPanel, "contentPanel");
		this.contentPanel.BackgroundImage = null;
		this.contentPanel.Font = null;
		this.contentPanel.Name = "contentPanel";
		// 
		// buttonPanel
		// 
		this.buttonPanel.AccessibleDescription = null;
		this.buttonPanel.AccessibleName = null;
		resources.ApplyResources (this.buttonPanel, "buttonPanel");
		this.buttonPanel.BackgroundImage = null;
		this.buttonPanel.Controls.Add (this.cancelButton, 3, 0);
		this.buttonPanel.Controls.Add (this.prevButton, 1, 0);
		this.buttonPanel.Controls.Add (this.nextButton, 2, 0);
		this.buttonPanel.Controls.Add (this.vendorLabel, 0, 0);
		this.buttonPanel.Font = null;
		this.buttonPanel.Name = "buttonPanel";
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
		this.cancelButton.Click += new System.EventHandler (this.cancelButton_Click);
		// 
		// prevButton
		// 
		this.prevButton.AccessibleDescription = null;
		this.prevButton.AccessibleName = null;
		resources.ApplyResources (this.prevButton, "prevButton");
		this.prevButton.BackgroundImage = null;
		this.prevButton.Font = null;
		this.prevButton.Name = "prevButton";
		this.prevButton.UseVisualStyleBackColor = true;
		this.prevButton.Click += new System.EventHandler (this.prevButton_Click);
		// 
		// nextButton
		// 
		this.nextButton.AccessibleDescription = null;
		this.nextButton.AccessibleName = null;
		resources.ApplyResources (this.nextButton, "nextButton");
		this.nextButton.BackgroundImage = null;
		this.nextButton.Font = null;
		this.nextButton.Name = "nextButton";
		this.nextButton.UseVisualStyleBackColor = true;
		this.nextButton.Click += new System.EventHandler (this.nextButton_Click);
		this.nextButton.EnabledChanged += new System.EventHandler (this.nextButton_EnabledChanged);
		// 
		// vendorLabel
		// 
		this.vendorLabel.AccessibleDescription = null;
		this.vendorLabel.AccessibleName = null;
		resources.ApplyResources (this.vendorLabel, "vendorLabel");
		this.vendorLabel.Font = null;
		this.vendorLabel.ForeColor = System.Drawing.SystemColors.GrayText;
		this.vendorLabel.Name = "vendorLabel";
		// 
		// topSeparatorPanel
		// 
		this.topSeparatorPanel.AccessibleDescription = null;
		this.topSeparatorPanel.AccessibleName = null;
		resources.ApplyResources (this.topSeparatorPanel, "topSeparatorPanel");
		this.topSeparatorPanel.BackColor = System.Drawing.SystemColors.ControlDark;
		this.topSeparatorPanel.BackgroundImage = null;
		this.topSeparatorPanel.Font = null;
		this.topSeparatorPanel.Name = "topSeparatorPanel";
		// 
		// bottomSeparatorPanel
		// 
		this.bottomSeparatorPanel.AccessibleDescription = null;
		this.bottomSeparatorPanel.AccessibleName = null;
		resources.ApplyResources (this.bottomSeparatorPanel, "bottomSeparatorPanel");
		this.bottomSeparatorPanel.BackColor = System.Drawing.SystemColors.ControlDark;
		this.bottomSeparatorPanel.BackgroundImage = null;
		this.bottomSeparatorPanel.Font = null;
		this.bottomSeparatorPanel.Name = "bottomSeparatorPanel";
		// 
		// titlePanel
		// 
		this.titlePanel.AccessibleDescription = null;
		this.titlePanel.AccessibleName = null;
		resources.ApplyResources (this.titlePanel, "titlePanel");
		this.titlePanel.BackColor = System.Drawing.Color.White;
		this.titlePanel.BackgroundImage = null;
		this.titlePanel.Controls.Add (this.logoPicture);
		this.titlePanel.Controls.Add (this.titleLabel);
		this.titlePanel.Controls.Add (this.subTitleLabel);
		this.titlePanel.Font = null;
		this.titlePanel.Name = "titlePanel";
		// 
		// logoPicture
		// 
		this.logoPicture.AccessibleDescription = null;
		this.logoPicture.AccessibleName = null;
		resources.ApplyResources (this.logoPicture, "logoPicture");
		this.logoPicture.BackColor = System.Drawing.Color.Transparent;
		this.logoPicture.BackgroundImage = null;
		this.logoPicture.Font = null;
		this.logoPicture.ImageLocation = null;
		this.logoPicture.Name = "logoPicture";
		this.logoPicture.TabStop = false;
		// 
		// titleLabel
		// 
		this.titleLabel.AccessibleDescription = null;
		this.titleLabel.AccessibleName = null;
		resources.ApplyResources (this.titleLabel, "titleLabel");
		this.titleLabel.BackColor = System.Drawing.Color.Transparent;
		this.titleLabel.ForeColor = System.Drawing.SystemColors.ControlDark;
		this.titleLabel.Name = "titleLabel";
		// 
		// subTitleLabel
		// 
		this.subTitleLabel.AccessibleDescription = null;
		this.subTitleLabel.AccessibleName = null;
		resources.ApplyResources (this.subTitleLabel, "subTitleLabel");
		this.subTitleLabel.BackColor = System.Drawing.Color.Transparent;
		this.subTitleLabel.ForeColor = System.Drawing.SystemColors.GrayText;
		this.subTitleLabel.Name = "subTitleLabel";
		// 
		// InstallerForm
		// 
		this.AccessibleDescription = null;
		this.AccessibleName = null;
		resources.ApplyResources (this, "$this");
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.BackgroundImage = null;
		this.CancelButton = this.cancelButton;
		this.ControlBox = false;
		this.Controls.Add (this.topSeparatorPanel);
		this.Controls.Add (this.bottomSeparatorPanel);
		this.Controls.Add (this.contentPanel);
		this.Controls.Add (this.titlePanel);
		this.Controls.Add (this.buttonPanel);
		this.Font = null;
		this.MaximizeBox = false;
		this.MinimizeBox = false;
		this.Name = "InstallerForm";
		this.buttonPanel.ResumeLayout (false);
		this.buttonPanel.PerformLayout ();
		this.titlePanel.ResumeLayout (false);
		((System.ComponentModel.ISupportInitialize) (this.logoPicture)).EndInit ();
		this.ResumeLayout (false);

    }

    #endregion

    private System.Windows.Forms.Panel titlePanel;
    private System.Windows.Forms.Panel contentPanel;
    private System.Windows.Forms.Panel topSeparatorPanel;
    private System.Windows.Forms.Panel bottomSeparatorPanel;
    private System.Windows.Forms.Button nextButton;
    private System.Windows.Forms.Button cancelButton;
    private System.Windows.Forms.Button prevButton;
    private System.Windows.Forms.Label titleLabel;
    private System.Windows.Forms.Label subTitleLabel;
    private System.Windows.Forms.TableLayoutPanel buttonPanel;
    private System.Windows.Forms.PictureBox logoPicture;
    private System.Windows.Forms.Label vendorLabel;
  }
}