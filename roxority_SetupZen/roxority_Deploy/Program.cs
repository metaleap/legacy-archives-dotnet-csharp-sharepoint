
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;

using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using roxority.SharePoint;

namespace roxority_Deploy {

	using res = Properties.Resources;

	class Program {

		static void Main (string [] args) {
			//Thread.CurrentThread.CurrentUICulture= new System.Globalization.CultureInfo ("de");
			Console.BackgroundColor = ConsoleColor.Gray;
			Console.ForegroundColor = ConsoleColor.Black;
			Console.Clear ();
			string caUrlHost = "http://" + Environment.MachineName;
			string caUrlPath = "_admin/solutions.aspx?";
			string cfgMossAsmCheck, cfgSolutionName, cfgSolutionTitle;
			Guid cfgSolutionId = Guid.Empty, cfgFeatureId;
			try {
				Console.Title = "ROXORITY " + res.Title;
				Console.WriteLine ("==================");
				Console.WriteLine (res.DontQuit);
				Console.WriteLine ("==================\n");
				Console.TreatControlCAsInput = true;

				Console.WriteLine (res.Step1);
				NameValueCollection cfg = ConfigurationManager.AppSettings;
				if (string.IsNullOrEmpty (cfgSolutionName = cfg ["SolutionName"]) || string.IsNullOrEmpty (cfg ["SolutionId"]))
					throw new Exception (res.Step1Fail);
				if (string.IsNullOrEmpty (cfgSolutionTitle = cfg ["SolutionTitle"]))
					cfgSolutionTitle = cfgSolutionName;
				cfgSolutionId = new Guid (cfg ["SolutionId"]);
				cfgFeatureId = new Guid (cfg ["FeatureId"]);
				caUrlPath = "_layouts/" + cfgSolutionName + "/default.aspx?doc=intro&postsetup=1";
				Console.Title = cfgSolutionTitle + " " + res.Title;

				Console.WriteLine (res.Step2);
				SPFarm farm = SPFarm.Local;
				if (farm == null)
					throw new Exception (res.Step2Fail);
				bool is14 = farm.BuildVersion.Major > 12;
				cfgMossAsmCheck = cfg ["Asm" + (is14 ? "14" : "12")];

				Console.WriteLine (res.Step3);
				SPSolution solution = farm.Solutions [cfgSolutionId];
				if (solution != null) {
					if (solution.Deployed) {
						Console.WriteLine ("\t" + res.Step3Retract);
						writeWait ();
						solution.Retract (DateTime.Now, solution.DeployedWebApplications);
						solution.Update (true);
						while (solution.Deployed || solution.DeploymentState != SPSolutionDeploymentState.NotDeployed) {
							Thread.Sleep (500);
						}
						solution.Update (true);
					}
					Console.WriteLine ("\t" + res.Step3Delete);
					solution.Delete ();
					solution = null;
				}
				writeColor ("\n" + cfgSolutionTitle + " " + res.Step3Uninstalled, ConsoleColor.DarkGreen);
				bool useWss = false;
				if (!string.IsNullOrEmpty (cfgMossAsmCheck))
					try {
						Assembly a1 = Assembly.Load (cfgMossAsmCheck + ", Culture=Neutral, Version=" + farm.BuildVersion.Major + ".0.0.0, PublicKeyToken=71e9bce111e9429c");
						Assembly a2 = Assembly.Load (cfgMossAsmCheck + ", Culture=Neutral, Version=14.0.0.0, PublicKeyToken=71e9bce111e9429c");
						if ((a1 == null) && (a2 == null)) {
							useWss = true;
						}
					} catch {
						useWss = true;
					}
				string wspFilePath = Path.Combine (System.AppDomain.CurrentDomain.BaseDirectory, cfgSolutionName + "_" + (is14 ? "xiv" : "xii") + ".wsp");
				if (useWss)
					wspFilePath = wspFilePath.Substring (0, wspFilePath.LastIndexOf ('.')) + "_wss.wsp";
				clearKeys ();
				Console.ForegroundColor = ConsoleColor.DarkCyan;
				Console.WriteLine ("\n" + res.Step4Prompt1 + Path.GetFileName (wspFilePath) + res.Step4Prompt2);
				Console.ForegroundColor = ConsoleColor.Black;
				Console.Write ("\t" + res.Step4Prompt3);
				if (Console.ReadKey (true).Key == ConsoleKey.Enter) {
					Console.WriteLine ("\n\n" + res.Step4);
					solution = farm.Solutions.Add (wspFilePath);
					farm.Solutions.Ensure (solution);
					solution.Update (true);
					enable (cfgSolutionId);
					farm.Update (true);

					Collection<SPWebApplication> webApps = new Collection<SPWebApplication> (), cWebApps = new Collection<SPWebApplication> ();
					foreach (SPWebApplication wapp in SPWebService.ContentService.WebApplications) {
						webApps.Add (wapp);
						cWebApps.Add (wapp);
						foreach (SPAlternateUrl url in wapp.AlternateUrls) {
							if (!string.IsNullOrEmpty (caUrlHost = url.Uri.ToString ()))
								break;
						}
					}
					foreach (SPWebApplication wapp in SPWebService.AdministrationService.WebApplications) {
						webApps.Add (wapp);
						foreach (SPAlternateUrl url in wapp.AlternateUrls) {
							if (!string.IsNullOrEmpty (caUrlHost = url.Uri.ToString ()))
								break;
						}
					}
					Console.WriteLine ("\t" + res.Step4Deploy);
					Thread.Sleep (TimeSpan.FromSeconds (webApps.Count * 2));
					writeWait ();
					solution.Deploy (DateTime.Now, true, webApps, true);
					solution.Update (true);
					do {
						Thread.Sleep (500);
					} while ((!solution.Deployed) || (solution.DeploymentState != SPSolutionDeploymentState.GlobalAndWebApplicationDeployed));
					solution.Synchronize ();
					foreach (SPWebApplication wapp in cWebApps) {
						try {
							for (int i = 0; i < wapp.Sites.Count; i++)
								try {
									SPSite site = wapp.Sites [i];
									try {
										site.AllowUnsafeUpdates = true;
									} catch {
									}
									try {
										site.Features.Add (cfgFeatureId, true);
									} catch {
									}
								} catch(Exception ex) {
									Console.WriteLine (ex.Message);
								}
						} catch {
						}
					}
					writeColor ("\n" + res.Success, ConsoleColor.DarkGreen);
				} else {
					caUrlHost = "";
					writeColor (res.Cancelled, ConsoleColor.DarkRed);
				}
			} catch (Exception ex) {
				caUrlHost = "";
				writeColor (res.Error + "\n" + ex.ToString (), ConsoleColor.DarkRed);
			} finally {
				enable (cfgSolutionId);
				clearKeys ();
				Console.WriteLine ("\n" + res.ExitPrompt);
				Console.ReadKey (true);
				Console.WriteLine (res.Exiting);
				if (!string.IsNullOrEmpty (caUrlHost))
					try {
						ProcessStartInfo proc = new ProcessStartInfo ("iexplore", caUrlHost.TrimEnd ('/') + "/" + caUrlPath.TrimStart ('/'));
						proc.ErrorDialog = false;
						proc.UseShellExecute = true;
						proc.WindowStyle = ProcessWindowStyle.Maximized;
						Process.Start (proc);
					} catch {
					}
			}
		}

		private static void clearKeys () {
			while (Console.KeyAvailable)
				Console.ReadKey (true);
			Console.Beep ();
		}

		private static void enable (Guid sid) {
			if (sid != Guid.Empty)
				try {
					Dictionary<string, object> obj = new Dictionary<string, object> ();
					obj ["t"] = DateTime.Now.Ticks;
					ProductPage.UpdateStatus (obj, true, true, false, res.ResourceManager.GetString ("map_" + sid.ToString ("N")), new List<string> (res.ResourceManager.GetString ("pid_" + sid.ToString ("N")).Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries)).ConvertAll<byte> (delegate (string s) {
						return byte.Parse (s.Trim ());
					}).ToArray ());
				} catch (Exception ex) {
					writeColor (ex.Message, ConsoleColor.DarkGray);
				}
		}

		private static void writeColor (string msg, ConsoleColor col) {
			Console.ForegroundColor = col;
			Console.WriteLine (msg);
			Console.ForegroundColor = ConsoleColor.Black;
		}

		private static void writeWait () {
			writeColor ("\t" + res.Wait, ConsoleColor.DarkCyan);
		}

	}

}
