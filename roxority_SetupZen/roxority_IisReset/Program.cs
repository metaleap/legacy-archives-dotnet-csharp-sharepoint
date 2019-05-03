
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace roxority_IisReset {

	public class Program {

		public static void Main (string [] args) {
			ProcessStartInfo procStart = new ProcessStartInfo ("iisreset", "/stop");
			procStart.CreateNoWindow = true;
			procStart.ErrorDialog = false;
			procStart.UseShellExecute = false;
			procStart.WindowStyle = ProcessWindowStyle.Hidden;
			procStart.RedirectStandardOutput = procStart.RedirectStandardError = true;
			using (Process proc = new Process ()) {
				proc.StartInfo = procStart;
				proc.Start ();
				Console.Write (proc.StandardOutput.ReadToEnd ());
				Console.Write (proc.StandardError.ReadToEnd ());
				proc.WaitForExit ();
			}
			foreach (string f in new string [] { "Framework", "Framework64" })
				foreach (string v in new string [] { "v2.0.50727", "v4.0.30319" })
					foreach (string dirPath in Directory.GetDirectories (@"C:\Windows\Microsoft.NET\" + f + "\\" + v + @"\Temporary ASP.NET Files", "*", SearchOption.TopDirectoryOnly))
						try {
							Directory.Delete (dirPath, true);
						} catch {
							Thread.Sleep (250);
							try {
								Directory.Delete (dirPath, true);
							} catch (Exception ex) {
								Console.WriteLine (dirPath + "\t" + ex.Message);
							}
						}
			procStart.Arguments = string.Empty;
			using (Process proc = new Process ()) {
				proc.StartInfo = procStart;
				proc.Start ();
				Console.Write (proc.StandardOutput.ReadToEnd ());
				Console.Write (proc.StandardError.ReadToEnd ());
				proc.WaitForExit ();
			}
		}

	}

}
