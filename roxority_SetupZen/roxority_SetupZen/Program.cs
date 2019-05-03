
using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace roxority_SetupZen {

	using res = Properties.Resources;

	public static class Program {

		internal static InstallerControl CreateDeploymentTargetsControl () {
			InstallerControl control = null;
			SPFeatureScope featureScope = InstallConfiguration.FeatureScope;
			if (featureScope == SPFeatureScope.Farm) {
				control = new DeploymentTargetsControl ();
				control.Title = res.FarmDeploymentTargets;
				control.SubTitle = res.FarmDeploymentTargets2;
			} else if (featureScope == SPFeatureScope.Site) {
				control = new SiteCollectionDeploymentTargetsControl ();
				control.Title = res.SiteDeploymentTargets;
				control.SubTitle = res.SiteDeploymentTargets2;
			}
			return control;
		}

		internal static InstallerControl CreateEULAControl () {
			EULAControl control = new EULAControl ();
			control.Title = res.Eula;
			control.SubTitle = res.Eula2;
			return control;
		}

		internal static InstallerControl CreateSqlControl () {
			SqlControl control = new SqlControl ();
			control.Title = res.Sql;
			control.SubTitle = res.Sql2;
			return control;
		}

		internal static InstallProcessControl CreateProcessControl () {
			InstallProcessControl control = new InstallProcessControl ();
			control.Title = res.Installing;
			control.SubTitle = res.Installing2;
			return control;
		}

		//internal static InstallerControl CreateOptionsControl () {
		//    OptionsControl control = new OptionsControl ();
		//    control.Title = res.Options;
		//    control.SubTitle = "";
		//    return control;
		//}

		internal static InstallerControl CreateRepairControl (bool allowRepair) {
			RepairControl control = new RepairControl (allowRepair);
			control.Title = res.RepairRemove;
			control.SubTitle = res.RepairRemove2;
			return control;
		}

		internal static InstallerControl CreateSystemCheckControl () {
			SystemCheckControl control = new SystemCheckControl ();
			control.Title = res.Check;
			control.SubTitle = InstallConfiguration.FormatString (res.Check2);

			control.RequireMOSS = InstallConfiguration.RequireMoss;
			control.RequireSearchSKU = false;

			return control;
		}

		internal static InstallerControl CreateUpgradeControl (bool allowUpgrade) {
			UpgradeControl control = new UpgradeControl (allowUpgrade);
			control.Title = res.UpgradeRemove;
			control.SubTitle = res.RepairRemove2;
			return control;
		}

		internal static InstallerControl CreateWelcomeControl () {
			WelcomeControl control = new WelcomeControl ();
			control.Title = InstallConfiguration.FormatString (res.Welcome);
			control.SubTitle = InstallConfiguration.FormatString (res.Welcome2);
			return control;
		}

		[STAThread]
		public static void Main (string [] args) {
			byte [] data;
			string fp2;
			try {
				foreach (string fp in Directory.GetFiles (Path.Combine (Application.StartupPath, "docs"), "*.chm", SearchOption.TopDirectoryOnly))
					try {
						data = null;
						using (FileStream fs = File.OpenRead (fp))
							fs.Read (data = new byte [(int) fs.Length], 0, (int) fs.Length);
						File.Delete (fp);
						using (FileStream fs = File.Create (fp))
							fs.Write (data, 0, data.Length);
					} catch {
					}
			} catch {
			}
			try {
				if (File.Exists (fp2 = Path.Combine (Application.StartupPath, "files\\sqljdbc" + ((IntPtr.Size == 8) ? 64 : 32) + ".dll")))
					File.Copy (fp2, Path.Combine (Environment.GetFolderPath (Environment.SpecialFolder.System), "sqljdbc_auth.dll"), true);
			} catch {
			}

			if ((args != null) && (args.Length > 0))
				try {
					Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo (args [0]);
				} catch {
				}
			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault (false);

			InstallerForm form = new InstallerForm ();
			form.Text = InstallConfiguration.FormatString (res.SolutionTitle, InstallConfiguration.SolutionTitle, InstallConfiguration.SolutionVersion);

			form.ContentControls.Add (CreateWelcomeControl ());
			form.ContentControls.Add (CreateSystemCheckControl ());

			Application.Run (form);
		}

	}

}
