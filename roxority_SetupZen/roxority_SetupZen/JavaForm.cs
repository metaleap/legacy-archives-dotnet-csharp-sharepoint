
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace roxority_SetupZen {

	public partial class JavaForm : Form {

		public readonly string ProcArgs, ProcPath;

		public JavaForm (string filePath, string args) {
			ProcArgs = args;
			ProcPath = filePath;
			InitializeComponent ();
		}

		internal void CoreInstall () {
			ProcessStartInfo procStart;
			if (Debugger.IsAttached && InvokeRequired) {
				Invoke (new ThreadStart (CoreInstall));
				return;
			}
			try {
				(procStart = new ProcessStartInfo (ProcPath, ProcArgs)).CreateNoWindow = true;
				procStart.ErrorDialog = true;
				procStart.ErrorDialogParentHandle = Handle;
				procStart.UseShellExecute = true;
				procStart.WindowStyle = ProcessWindowStyle.Hidden;
				procStart.WorkingDirectory = Application.StartupPath;
				using (Process proc = new Process ()) {
					proc.StartInfo = procStart;
					if (proc.Start ())
						try {
							proc.WaitForExit ();
						} catch {
						}
				}
			} catch (Exception ex) {
				MessageBox.Show (this, ex.Message, ProcPath, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			Close ();
		}

		protected override void OnLoad (EventArgs e) {
			base.OnLoad (e);
			Show ();
			Application.DoEvents ();
			Focus ();
			Application.DoEvents ();
			try {
				new Thread (CoreInstall).Start ();
			} catch {
			}
		}

	}

}
