/**********************************************************************/
/*                                                                    */
/*                   SharePoint Solution Installer                    */
/*             http://www.codeplex.com/sharepointinstaller            */
/*                                                                    */
/*               (c) Copyright 2007 Lars Fastrup Nielsen.             */
/*                                                                    */
/*  This source is subject to the Microsoft Permissive License.       */
/*  http://www.codeplex.com/sharepointinstaller/Project/License.aspx  */
/*                                                                    */
/* KML: Minor update to usage of EULA config property and error text  */
/*                                                                    */
/**********************************************************************/
using roxority.SharePoint;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Security;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Timers;
using System.Windows.Forms;

using Microsoft.Win32;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Security;
using System.Configuration;
using System.IO;


namespace roxority_SetupZen {

	using res = Properties.Resources;

	public partial class SystemCheckControl : InstallerControl {
		private static readonly ILog log = LogManager.GetLogger ();
		internal static bool? MossInstalled = null;
		private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer ();

		private bool requireMOSS;
		private bool requireSearchSKU;
		private SystemCheckList checks;
		private int nextCheckIndex;
		private int errors;

		#region Constructors

		public SystemCheckControl () {
			InitializeComponent ();

			this.Load += new EventHandler (SystemCheckControl_Load);
		}

		#endregion

		#region Public Properties

		public bool RequireMOSS {
			get {
				return requireMOSS;
			}
			set {
				requireMOSS = value;
			}
		}

		public bool RequireSearchSKU {
			get {
				return requireSearchSKU;
			}
			set {
				requireSearchSKU = value;
			}
		}

		#endregion

		#region Event Handlers

		private void SystemCheckControl_Load (object sender, EventArgs e) {
		}

		private void TimerEventProcessor (Object myObject, EventArgs myEventArgs) {
			timer.Stop ();

			if (nextCheckIndex < checks.Count) {
				if (ExecuteCheck (nextCheckIndex)) {
					nextCheckIndex++;
					timer.Start ();
					return;
				}
			}

			FinalizeChecks ();
		}

		#endregion

		#region Protected Methods

		protected internal override void Open (InstallOptions options) {
			if (checks == null) {
				Form.NextButton.Enabled = false;
				Form.PrevButton.Enabled = false;

				checks = new SystemCheckList ();
				InitializeChecks ();

				timer.Interval = 100;
				timer.Tick += new EventHandler (TimerEventProcessor);
				timer.Start ();
			}
		}

		protected internal override void Close (InstallOptions options) {
		}

		#endregion

		#region Private Methods

		private void InitializeChecks () {
			Guid guid;
			this.tableLayoutPanel.SuspendLayout ();

			//
			// WSS Installed Check
			//
			WSSInstalledCheck wssCheck = new WSSInstalledCheck ();
			wssCheck.QuestionText = res.CheckWss;
			wssCheck.OkText = res.CheckWssYes;
			wssCheck.ErrorText = res.CheckWssNo;
			AddCheck (wssCheck);

			//
			// MOSS Installed Check
			//
			if (requireMOSS) {
				MOSSInstalledCheck mossCheck = new MOSSInstalledCheck ();
				mossCheck.QuestionText = res.CheckMoss;
				mossCheck.OkText = res.CheckMossYes;
				mossCheck.ErrorText = res.CheckMossNo;
				AddCheck (mossCheck);
			}

			//
			// Admin Rights Check
			//
			AdminRightsCheck adminRightsCheck = new AdminRightsCheck ();
			adminRightsCheck.QuestionText = res.CheckAdmin;
			adminRightsCheck.OkText = res.CheckAdminYes;
			adminRightsCheck.ErrorText = res.CheckAdminNo;
			AddCheck (adminRightsCheck);

			//
			// Admin Service Check
			//
			AdminServiceCheck adminServiceCheck = new AdminServiceCheck ();
			adminServiceCheck.QuestionText = res.CheckService;
			adminServiceCheck.OkText = res.CheckServiceYes;
			adminServiceCheck.ErrorText = res.CheckServiceNo;
			AddCheck (adminServiceCheck);

			//
			// Timer Service Check
			//
			TimerServiceCheck timerServiceCheck = new TimerServiceCheck ();
			timerServiceCheck.QuestionText = res.CheckTimer;
			timerServiceCheck.OkText = res.CheckTimerYes;
			timerServiceCheck.ErrorText = res.CheckTimerNo;
			AddCheck (timerServiceCheck);

			//
			// Solution Package Check
			//
			SolutionFileCheck solutionFileCheck = new SolutionFileCheck ();
			solutionFileCheck.QuestionText = InstallConfiguration.FormatString (res.CheckFile);
			solutionFileCheck.OkText = InstallConfiguration.FormatString (res.CheckFileYes);
			solutionFileCheck.ErrorText = InstallConfiguration.FormatString (res.CheckFileNo);
			AddCheck (solutionFileCheck);

			//
			// Solution Check
			//
			SolutionCheck solutionCheck = new SolutionCheck ();
			solutionCheck.QuestionText = InstallConfiguration.FormatString (res.CheckSolution);
			solutionCheck.OkText = InstallConfiguration.FormatString (res.CheckSolutionYes);
			solutionCheck.ErrorText = InstallConfiguration.FormatString (res.CheckSolutionNo);
			AddCheck (solutionCheck);

			if (!string.IsNullOrEmpty (InstallConfiguration.Java)) {
				JavaInstalledCheck javaCheck = new JavaInstalledCheck ();
				javaCheck.QuestionText = string.Format (res.CheckJava, InstallConfiguration.Java);
				javaCheck.OkText = string.Format (res.CheckJavaYes, InstallConfiguration.Java);
				javaCheck.ErrorText = string.Format (res.CheckJavaNo, InstallConfiguration.Java);
				AddCheck (javaCheck);
			}

			if ((!string.IsNullOrEmpty (InstallConfiguration.LegacyLists)) && !Guid.Empty.Equals (guid = ProductPage.GetGuid (InstallConfiguration.LegacyLists, false))) {
				LegacyCheck legacyCheck = new LegacyCheck (guid);
				legacyCheck.ErrorText = res.LegacyFail;
				legacyCheck.QuestionText = res.Legacy;
				legacyCheck.OkText = res.LegacyOK;
				AddCheck (legacyCheck);
			}

			//
			// Add empty row that will eat up the rest of the 
			// row space in the layout table.
			//
			this.tableLayoutPanel.RowCount++;
			this.tableLayoutPanel.RowStyles.Add (new System.Windows.Forms.RowStyle (System.Windows.Forms.SizeType.Percent, 100F));

			this.tableLayoutPanel.ResumeLayout (false);
			this.tableLayoutPanel.PerformLayout ();
		}

		private bool ExecuteCheck (int index) {
			SystemCheck check = checks [index];
			string imageLabelName = "imageLabel" + index;
			string textLabelName = "textLabel" + index;
			Label imageLabel = (Label) tableLayoutPanel.Controls [imageLabelName];
			Label textLabel = (Label) tableLayoutPanel.Controls [textLabelName];

			try {
				SystemCheckResult result = check.Execute ();
				if (result == SystemCheckResult.Success) {
					imageLabel.Image = global::roxority_SetupZen.Properties.Images.CheckOk;
					textLabel.Text = check.OkText;
				} else if (result == SystemCheckResult.Error) {
					errors++;
					imageLabel.Image = global::roxority_SetupZen.Properties.Images.CheckFail;
					textLabel.Text = check.ErrorText;
				}

				//
				// Show play icon on next check that will run.
				//
				int nextIndex = index + 1;
				string nextImageLabelName = "imageLabel" + nextIndex;
				Label nextImageLabel = (Label) tableLayoutPanel.Controls [nextImageLabelName];
				if (nextImageLabel != null) {
					nextImageLabel.Image = global::roxority_SetupZen.Properties.Images.CheckPlay;
				}

				return true;
			} catch (InstallException ex) {
				errors++;
				imageLabel.Image = global::roxority_SetupZen.Properties.Images.CheckFail;
				textLabel.Text = ex.Message;
			}

			return false;
		}

		private void FinalizeChecks () {
			if (errors == 0) {
				ConfigureControls ();
				Form.NextButton.Enabled = true;
				messageLabel.Text = res.CheckAllYes;
			} else {
				messageLabel.Text = InstallConfiguration.FormatString (res.CheckAllNo);
			}

			Form.PrevButton.Enabled = true;
		}

		private void AddCheck (SystemCheck check) {
			int row = tableLayoutPanel.RowCount;

			Label imageLabel = new Label ();
			imageLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			imageLabel.Image = global::roxority_SetupZen.Properties.Images.CheckWait;
			imageLabel.Location = new System.Drawing.Point (3, 0);
			imageLabel.Name = "imageLabel" + row;
			imageLabel.Size = new System.Drawing.Size (24, (check is LegacyCheck) ? 80 : 20);
			if (check is LegacyCheck)
				imageLabel.ImageAlign = ContentAlignment.TopCenter;

			Label textLabel = new Label ();
			textLabel.AutoSize = !(check is LegacyCheck);
			textLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			textLabel.Location = new System.Drawing.Point (33, 0);
			if (check is LegacyCheck)
				textLabel.Padding = new System.Windows.Forms.Padding (0, 3, 0, 0);
			textLabel.Name = "textLabel" + row;
			textLabel.Size = new System.Drawing.Size (390, (check is LegacyCheck) ? 80 : 20);
			textLabel.Text = check.QuestionText;
			textLabel.TextAlign = (check is LegacyCheck) ? System.Drawing.ContentAlignment.TopLeft : System.Drawing.ContentAlignment.MiddleLeft;

			this.tableLayoutPanel.Controls.Add (imageLabel, 0, row);
			this.tableLayoutPanel.Controls.Add (textLabel, 1, row);
			this.tableLayoutPanel.RowStyles.Add (new System.Windows.Forms.RowStyle (System.Windows.Forms.SizeType.Absolute, (check is LegacyCheck) ? 80F : 20F));
			this.tableLayoutPanel.RowCount++;

			checks.Add (check);
		}

		private void ConfigureControls () {
			SolutionCheck check = (SolutionCheck) checks ["SolutionCheck"];
			SPSolution solution = check.Solution;

			if (solution == null) {
				AddInstallControls ();
			} else {
				Version installedVersion = InstallConfiguration.InstalledVersion;
				Version newVersion = InstallConfiguration.SolutionVersion;
				Version minVersion = string.IsNullOrEmpty (InstallConfiguration.UpdateMinVersion) ? null : new Version (InstallConfiguration.UpdateMinVersion);

				if (newVersion != installedVersion) {
					Form.ContentControls.Add (Program.CreateUpgradeControl ((minVersion == null) || (installedVersion == null) || (installedVersion >= minVersion)));
				} else {
					Form.ContentControls.Add (Program.CreateRepairControl ((minVersion == null) || (installedVersion == null) || (installedVersion >= minVersion)));
				}
			}
		}

		private void AddInstallControls () {
			//
			// Add EULA control if an EULA file was specified.
			//
			string filename = InstallConfiguration.GetEula (string.Empty);
			if (!String.IsNullOrEmpty (filename)) {
				Form.ContentControls.Add (Program.CreateEULAControl ());
			}

			if ((!string.IsNullOrEmpty (InstallConfiguration.Sql)) && (File.Exists (InstallConfiguration.Sql) || ((!InstallConfiguration.Sql.Contains ("\\")) && File.Exists (Path.Combine (Application.StartupPath, InstallConfiguration.Sql.TrimStart ('\\'))))))
				Form.ContentControls.Add (Program.CreateSqlControl ());

			Form.ContentControls.Add (Program.CreateDeploymentTargetsControl ());
			//Form.ContentControls.Add(Program.CreateOptionsControl());
			Form.ContentControls.Add (Program.CreateProcessControl ());
		}

		#endregion

		#region Check Classes

		internal enum SystemCheckResult {
			Inconclusive,
			Success,
			Error
		}

		/// <summary>
		/// Base class for all system checks.
		/// </summary>
		internal abstract class SystemCheck {
			private readonly string id;
			private string questionText;
			private string okText;
			private string errorText;

			protected SystemCheck (string id) {
				this.id = id;
			}

			public string Id {
				get {
					return id;
				}
			}

			public string QuestionText {
				get {
					return questionText;
				}
				set {
					questionText = value;
				}
			}

			public string OkText {
				get {
					return okText;
				}
				set {
					okText = value;
				}
			}

			public string ErrorText {
				get {
					return errorText;
				}
				set {
					errorText = value;
				}
			}

			internal SystemCheckResult Execute () {
				if (CanRun) {
					return DoExecute ();
				}
				return SystemCheckResult.Inconclusive;
			}

			protected abstract SystemCheckResult DoExecute ();

			protected virtual bool CanRun {
				get {
					return true;
				}
			}

			protected static bool IsWSSInstalled {
				get {
					bool is14 = ProductPage.Is14;
					string name = @"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\" + (is14 ? 14 : 12) + ".0";
					object val;
					try {
						RegistryKey key = Registry.LocalMachine.OpenSubKey (name);
						if (key != null)
							using (key) {
								val = key.GetValue ("SharePoint");
								if (val != null && val.Equals ("Installed"))
									return true;
							}
					} catch (SecurityException ex) {
						throw new InstallException (string.Format (res.ErrorRegistryAccess, name, ex.Message), ex);
					}
					return false;
				}
			}

			protected internal static bool IsMOSSInstalled {
				get {
					bool is14 = ProductPage.Is14;
					string versionStr, check = is14 ? InstallConfiguration.MossAssemblyCheck14 : InstallConfiguration.MossAssemblyCheck12, name = @"SOFTWARE\Microsoft\Office Server\" + (is14 ? 14 : 12) + ".0";
					Version buildVersion = null;
					if ((MossInstalled == null) || !MossInstalled.HasValue) {
						MossInstalled = false;
						try {
							using (RegistryKey key = Registry.LocalMachine.OpenSubKey (name))
								if (key != null)
									using (key)
										if (!string.IsNullOrEmpty (versionStr = key.GetValue ("BuildVersion") + string.Empty)) {
											buildVersion = new Version (versionStr);
											if (buildVersion.Major == (is14 ? 14 : 12))
												MossInstalled = true;
										}
						} catch {
						}
						if ((MossInstalled == null) || (!MossInstalled.HasValue) || MossInstalled.Value) {
							try {
								if (Assembly.Load ((string.IsNullOrEmpty (check) ? "Microsoft.Office.Server" : check) + ", Culture=Neutral, Version=" + (is14 ? 14 : 12) + ".0.0.0, PublicKeyToken=71e9bce111e9429c") == null)
									throw new FileLoadException ();
								else
									MossInstalled = true;
							} catch {
								MossInstalled = false;
							}
							if ((!string.IsNullOrEmpty (check)) && MessageBox.Show (InstallerForm.ActiveForm, string.Format (res.CheckMossPrompt, ((bool) MossInstalled) ? res.CheckMossPromptMoss : res.CheckMossPromptWss, ((bool) MossInstalled) ? res.CheckMossPromptIfMoss : res.CheckMossPromptIfWss, ((bool) MossInstalled) ? res.CheckMossPromptWss : res.CheckMossPromptMoss), res.CheckMossTitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
								MossInstalled = !MossInstalled;
						}
					}
					return MossInstalled.HasValue && MossInstalled.Value;
				}
			}
		}

		private class SystemCheckList : List<SystemCheck> {
			internal SystemCheck this [string id] {
				get {
					foreach (SystemCheck check in this) {
						if (check.Id == id)
							return check;
					}
					return null;
				}
			}
		}

		/// <summary>
		/// Checks if WSS 3.0 is installed.
		/// </summary>
		private class WSSInstalledCheck : SystemCheck {
			internal WSSInstalledCheck ()
				: base ("WSSInstalledCheck") {
			}

			protected override SystemCheckResult DoExecute () {
				if (IsWSSInstalled)
					return SystemCheckResult.Success;
				return SystemCheckResult.Error;
			}
		}

		/// <summary>
		/// Checks if Microsoft Office Server 2007 is installed.
		/// </summary>
		private class MOSSInstalledCheck : SystemCheck {
			internal MOSSInstalledCheck ()
				: base ("MOSSInstalledCheck") {
			}

			protected override SystemCheckResult DoExecute () {
				if (IsMOSSInstalled)
					return SystemCheckResult.Success;
				return SystemCheckResult.Error;
			}
		}

		private class JavaInstalledCheck : SystemCheck {

			internal JavaInstalledCheck ()
				: base ("JavaInstalledCheck") {
			}

			internal SystemCheckResult DoExecute (bool promptSetup) {
				SystemCheckResult res = SystemCheckResult.Error;
				RegistryKey regKey;
				try {
					bool found = false;
					int pos;
					Version minVersion = new Version (InstallConfiguration.Java);
					if ((regKey = Registry.LocalMachine.OpenSubKey (@"Software\JavaSoft\Java Runtime Environment", false)) != null)
						using (regKey)
							foreach (string s in regKey.GetSubKeyNames ()) {
								try {
									found = (new Version ((((pos = s.IndexOf ('-')) > 0) ? s.Substring (0, pos) : s).Replace ('_', '.')) >= minVersion);
								} catch {
								}
								if (found)
									break;
							}
					if (found)
						res = SystemCheckResult.Success;
				} catch {
				}
				if (res != SystemCheckResult.Success)
					if (promptSetup && MessageBox.Show (InstallerForm.ActiveForm, Properties.Resources.JavaNope, "Java", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
						using (JavaForm javaForm = new JavaForm (Path.Combine (Application.StartupPath, "files\\zupjava" + ((IntPtr.Size == 8) ? 64 : 32) + ".exe"), "/quiet /qn /norestart")) {
							javaForm.ShowDialog (InstallerForm.ActiveForm);
							return DoExecute (false);
						} else
						MessageBox.Show (InstallerForm.ActiveForm, string.Format (Properties.Resources.JavaNoInstall, (IntPtr.Size == 8) ? 64 : 32), "Java", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return res;
			}

			protected override SystemCheckResult DoExecute () {
				return DoExecute (true);
			}

		}

		/// <summary>
		/// Checks if the Windows SharePoint Services Administration service is started.
		/// </summary>
		private class AdminServiceCheck : SystemCheck {
			internal AdminServiceCheck ()
				: base ("AdminServiceCheck") {
			}

			protected override SystemCheckResult DoExecute () {
				try {
					ServiceController sc = new ServiceController (ProductPage.Is14 ? "SPAdminV4" : "SPAdmin");
					if (sc.Status != ServiceControllerStatus.Running) {
						sc.Start ();
						sc.WaitForStatus (ServiceControllerStatus.Running, new TimeSpan (0, 2, 0));
					}
					if (sc.Status == ServiceControllerStatus.Running)
						return SystemCheckResult.Success;
					return SystemCheckResult.Error;
				} catch (Win32Exception ex) {
					log.Error (ex.Message, ex);
				} catch (InvalidOperationException ex) {
					log.Error (ex.Message, ex);
				}

				return SystemCheckResult.Inconclusive;
			}

			protected override bool CanRun {
				get {
					return IsWSSInstalled;
				}
			}
		}

		/// <summary>
		/// Checks if the Windows SharePoint Services Timer service is started.
		/// </summary>
		private class TimerServiceCheck : SystemCheck {
			Exception err = null;

			internal TimerServiceCheck ()
				: base ("TimerServiceCheck") {
			}

			protected override SystemCheckResult DoExecute () {
				try {
					TimeSpan timeout = new TimeSpan (0, 2, 0);
					ServiceController sc = new ServiceController (ProductPage.Is14 ? "SPTimerV4" : "SPTimerV3");
					if (sc.Status != ServiceControllerStatus.Running) {
						try {
							sc.Stop ();
						} catch {
						}
						sc.WaitForStatus (ServiceControllerStatus.Stopped, timeout);
						if (sc.Status != ServiceControllerStatus.Running)
							sc.Start ();
						sc.WaitForStatus (ServiceControllerStatus.Running, timeout);
					}
					return SystemCheckResult.Success;
				} catch (Exception ex) {
					err = ex;
					log.Error (ex.Message, ex);
				}

				MessageBox.Show (InstallerForm.ActiveForm, res.NoTimerHint + ((err == null) ? string.Empty : ("\r\n\r\n" + err.ToString ())));
				return SystemCheckResult.Error;
			}

			protected override bool CanRun {
				get {
					return IsWSSInstalled;
				}
			}
		}

		/// <summary>
		/// Checks if the current user is an administrator.
		/// </summary>
		private class AdminRightsCheck : SystemCheck {
			internal AdminRightsCheck ()
				: base ("AdminRightsCheck") {
			}

			protected override SystemCheckResult DoExecute () {
				try {
					if (SPFarm.Local.CurrentUserIsAdministrator ())
						return SystemCheckResult.Success;
					else
						return SystemCheckResult.Error;
				} catch (NullReferenceException) {
					throw new InstallException (res.SpDbError);
				} catch (Exception ex) {
					throw new InstallException (ex.Message, ex);
				}
			}

			protected override bool CanRun {
				get {
					return IsWSSInstalled;
				}
			}
		}

		private class SolutionFileCheck : SystemCheck {
			internal SolutionFileCheck ()
				: base ("SolutionFileCheck") {
			}

			protected override SystemCheckResult DoExecute () {
				string filename = InstallConfiguration.SolutionFile;
				if (!String.IsNullOrEmpty (filename)) {
					FileInfo solutionFileInfo = new FileInfo (filename);
					if (!solutionFileInfo.Exists)
						throw new InstallException (string.Format (res.SolutionFileNotFound, filename));
				} else {
					throw new InstallException (res.ConfigErrorNoSolution);
				}

				return SystemCheckResult.Success;
			}
		}

		private class LegacyCheck : SystemCheck {

			public readonly Guid Guid;
			private bool found = false;

			internal LegacyCheck (Guid guid) : base ("LegacyCheck") {
				Guid = guid;
			}

			internal void DoCheck () {
				foreach (SPWebService ws in new SPWebService [] { SPWebService.AdministrationService, SPWebService.ContentService })
					foreach (SPWebApplication wa in ProductPage.TryEach<SPWebApplication> (ws.WebApplications)) {
						if (wa.IsAdministrationWebApplication && !wa.Properties.ContainsKey ("AdministrationWebApplicationSyncLock")) {
							found = false;
							return;
						}
						foreach (SPSite site in ProductPage.TryEach<SPSite> (wa.Sites))
							foreach (SPWeb web in ProductPage.TryEach<SPWeb> (site.AllWebs))
								foreach (SPList list in ProductPage.TryEach<SPList> (web.Lists))
									if (found = Guid.Equals (list.TemplateFeatureId))
										return;
					}
			}

			protected override SystemCheckResult DoExecute () {
				found = false;
				ProductPage.Elevate (DoCheck, true);
				return found ? SystemCheckResult.Error : SystemCheckResult.Success;
			}

		}

		private class SolutionCheck : SystemCheck {

			private SPSolution solution;

			internal SolutionCheck ()
				: base ("SolutionCheck") {
			}

			protected override SystemCheckResult DoExecute () {
				Guid solutionId = Guid.Empty;
				try {
					solutionId = InstallConfiguration.SolutionId;
				} catch (ArgumentNullException) {
					throw new InstallException (res.ConfigErrorNoSolutionID);
				} catch (FormatException) {
					throw new InstallException (res.ConfigErrorInvalidSolutionID);
				}

				try {
					solution = SPFarm.Local.Solutions [solutionId];
					if (solution != null) {
						this.OkText = InstallConfiguration.FormatString (res.SolutionInstalled);
					} else {
						this.OkText = InstallConfiguration.FormatString (res.SolutionNotInstalled);
					}
				} catch (NullReferenceException) {
					throw new InstallException (res.SpDbError);
				} catch (Exception ex) {
					throw new InstallException (ex.Message, ex);
				}

				return SystemCheckResult.Success;
			}

			protected override bool CanRun {
				get {
					return IsWSSInstalled;
				}
			}

			internal SPSolution Solution {
				get {
					return solution;
				}
			}
		}

		#endregion

	}

}
