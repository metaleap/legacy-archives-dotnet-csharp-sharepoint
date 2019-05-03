
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Printing;
using System.Windows.Forms;
using System.Windows.Xps.Packaging;

namespace roxority_PrintZen_PrintServer {

	using cfg = Properties.Settings;
	using Tim = System.Timers.Timer;

	public class Program : Form {

		static readonly cfg config = cfg.Default;

		private NotifyIcon icon;
		private Timer timer;
		private bool busy = false;

		#region Job Class

		public class Job {

			public string FilePath, Printer;
			public Duplexing? Duplex;
			public Stapling? Staple;
			public bool Hole;

			public Job (string filePath, string printer, Duplexing? duplex, bool hole, Stapling? staple) {
				FilePath = filePath;
				Printer = printer;
				Duplex = duplex;
				Staple = staple;
			}

		}

		#endregion

		internal static List<string> Convert (string id, string hivePath, string toolPath, List<string> sourceFiles, string printer, bool assemblyOnly, bool duplex, bool staple, bool hole, bool indiJobs, bool merge) {
			string outPath, tmp;
			List<string> outFiles = new List<string> ();
			foreach (string fileName in sourceFiles) {
				outPath = ConvertCore (hivePath, toolPath, !indiJobs, !indiJobs, indiJobs ? "xps" : "pdf", id, fileName);
				if (!outFiles.Contains (outPath.ToLowerInvariant ()))
					outFiles.Add (outPath.ToLowerInvariant ());
			}
			if (!(assemblyOnly || indiJobs)) {
				outFiles [0] = ConvertCore (hivePath, toolPath, false, true, "xps", id, Path.GetFileName (tmp = outFiles [0]));
				Delete (tmp);
			}
			return outFiles;
		}

		internal static string ConvertCore (string hivePath, string toolPath, bool append, bool useID, string ext, string id, string fileName) {
			bool retry;
			string outPath = null, tmp;
			ProcessStartInfo procStart;
			do {
				retry = false;
				procStart = new ProcessStartInfo (toolPath, "-d -D -p " + (append ? 2 : 0) + " -i \"" + Path.Combine (hivePath, fileName) + "\" -o \"" + (outPath = Path.Combine (hivePath, (useID ? id : fileName) + ".vaprintjob." + ext)) + "\"");
				procStart.CreateNoWindow = procStart.ErrorDialog = procStart.UseShellExecute = false;
				procStart.RedirectStandardError = procStart.RedirectStandardOutput = true;
				procStart.WindowStyle = ProcessWindowStyle.Normal;
				using (Process proc = new Process ()) {
					proc.StartInfo = procStart;
					proc.Start ();
					proc.WaitForExit ();
					if (!string.IsNullOrEmpty (tmp = proc.StandardError.ReadToEnd ()))
						retry = true;
					else if ((!string.IsNullOrEmpty (tmp = proc.StandardOutput.ReadToEnd ())) && tmp.Contains ("Result Code = 0"))
						retry = true;
				}
			} while (retry);
			return outPath;
		}

		internal static void Delete (params string [] filePaths) {
			foreach (string filePath in filePaths)
				while (File.Exists (filePath))
					try {
						File.Delete (filePath);
					} catch {
					}
		}

		internal static void Print (string filePath, PrintQueue printer, Duplexing? duplexing, bool hole, Stapling? stapling) {
			if ((duplexing != null) && duplexing.HasValue)
				printer.CurrentJobSettings.CurrentPrintTicket.Duplexing = duplexing;
			if ((stapling!=null)&&stapling.HasValue)
				printer.CurrentJobSettings.CurrentPrintTicket.Stapling = stapling;
			printer.AddJob (Path.GetFileName (filePath), filePath, false).Dispose ();
			Delete (filePath);
		}

		[STAThread]
		public static void Main (string [] args) {
			Application.Run (new Program ());
		}

		public Program () {
			ComponentResourceManager resources = new ComponentResourceManager (typeof (Program));
			icon = new NotifyIcon ();
			icon.Text = "ROXORITY PrintZen -- Print Server";
			icon.Icon = resources.GetObject ("$this.Icon") as Icon;
			icon.Visible = true;
			icon.ContextMenu = new ContextMenu (new MenuItem [] { new MenuItem ("Exit", delegate (object sender, EventArgs e) {
				timer.Stop ();
				Application.Exit ();
			}) });
			timer = new Timer ();
			timer.Tick += Timer_Tick;
			ForceHide ();
		}

		internal void Timer_Tick (object sender, EventArgs e) {
			bool a, d, s, h, j, m;
			string p;
			Job job;
			List<Job> jobs = new List<Job> ();
			//foreach (string fp in Directory.GetFiles (config.HivePath, "*", SearchOption.TopDirectoryOnly))
			//    if (!"readme.txt".Equals (Path.GetFileName (fp), StringComparison.InvariantCultureIgnoreCase))
			//        Delete (fp);
			string ln;
			List<string> fileNames = new List<string> (), outFilePaths;
			Dictionary<char, string> options = new Dictionary<char, string> ();
			if (!busy)
				try {
					busy = true;
					foreach (string filePath in Directory.GetFiles (HivePath, "va-print-*-.vaprintjob", SearchOption.TopDirectoryOnly))
						try {
							fileNames.Clear ();
							options.Clear ();
							foreach (string fp in Directory.GetFiles (HivePath, Path.GetFileNameWithoutExtension (filePath) + "*.*", SearchOption.TopDirectoryOnly))
								if ((!filePath.Equals (fp, StringComparison.InvariantCultureIgnoreCase)) && !fp.ToLowerInvariant ().Contains (".vaprintjob."))
									fileNames.Add (Path.GetFileName (fp));
							fileNames.Sort ();
							using (StreamReader sr = File.OpenText (filePath))
								while (!string.IsNullOrEmpty (ln = sr.ReadLine ()))
									options [ln [0]] = ln.Substring (2);
							outFilePaths = Convert (Path.GetFileNameWithoutExtension (filePath), HivePath, ToolPath, fileNames, p = options ['p'], a = "1".Equals (options ['a']), d = "1".Equals (options ['d']), s = "1".Equals (options ['s']), h = "1".Equals (options ['h']), j = "1".Equals (options ['j']), m = "1".Equals (options ['m']));
							Delete (fileNames.ToArray ());
							foreach (string ofp in outFilePaths)
								if (a)
									File.Move (ofp, ofp.Replace (".vaprintjob.pdf", ".vaprintjob.final.pdf"));
								else
									jobs.Add (new Job (ofp, p, d, h, s));
						} catch (Exception ex) {
							File.AppendAllText (filePath + ".error", ex.ToString ());
						} finally {
							Delete (filePath);
						}
					while (jobs.Count > 0)
						using (LocalPrintServer lps = new LocalPrintServer ()) {
							job = jobs [0];
							jobs.RemoveAt (0);
							try {
								using (PrintQueue pq = lps.GetPrintQueue (job.Printer))
									Print (job.FilePath, pq, job.Duplex, job.Hole, job.Staple);
							} catch (Exception ex) {
								File.AppendAllText (job.FilePath + ".error", ex.ToString ());
							}
						}
				} finally {
					busy = false;
				}
		}

		protected override void Dispose (bool disposing) {
			if (disposing) {
				timer.Stop ();
				timer.Dispose ();
				icon.Dispose ();
			}
			base.Dispose (disposing);
		}

		protected override void OnLoad (EventArgs e) {
			ForceHide ();
			base.OnLoad (e);
			ForceHide ();
			timer.Start ();
		}

		protected override void OnShown (EventArgs e) {
			ForceHide ();
			base.OnShown (e);
			ForceHide ();
		}

		protected override void OnVisibleChanged (EventArgs e) {
			ForceHide ();
			base.OnVisibleChanged (e);
			ForceHide ();
		}

		protected override void OnCreateControl () {
			ForceHide ();
			base.OnCreateControl ();
			ForceHide ();
		}

		public void ForceHide () {
			Visible = ShowInTaskbar = false;
			Hide ();
		}

		public Duplexing Duplex {
			get {
				return Duplexing.OneSided;
			}
		}

		public string HivePath {
			get {
				return "";
			}
		}

		public Stapling Stapling {
			get {
				return Stapling.None;
			}
		}

		public string ToolPath {
			get {
				return "";
			}
		}

	}

}
