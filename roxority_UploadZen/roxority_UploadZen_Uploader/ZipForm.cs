
using Ionic.Zip;
using roxority.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace roxUp {

	public partial class ZipForm : Form {

		public readonly Action<ZipFile> ZipAction;
		public readonly MultiExplorerControl MultiExplorerControl;
		public readonly Thread ZipThread;
		public readonly ZipFile ZipFile = CreateZip ();

		private bool closing = false;

		public static ZipFile CreateZip () {
			return InitZip (null);
		}

		public static ZipFile InitZip (ZipFile zipFile) {
			if (zipFile == null)
				zipFile = new ZipFile ();
			zipFile.CompressionLevel = Ionic.Zlib.CompressionLevel.BEST_COMPRESSION;
			zipFile.ForceNoCompression = false;
			zipFile.UseZip64WhenSaving = Zip64Option.Never;
			zipFile.WantCompression = delegate (string one, string two) {
				return true;
			};
			zipFile.WillReadTwiceOnInflation = delegate (long one, long two, string three) {
				return false;
			};
			return zipFile;
		}

		public ZipForm (MultiExplorerControl multiExplorerControl, string title, Action<ZipFile> zipAction) {
			MultiExplorerControl = multiExplorerControl;
			ZipEntry.ExceptionNotification += zipEntry_ExceptionNotification;
			ZipAction = zipAction;
			ZipThread = new Thread (Start);
			InitializeComponent ();
			Text = title;
		}

		private void cancelButton_Click (object sender, EventArgs e) {
			Button button = sender as Button;
			closing = true;
			try {
				ZipThread.Abort ();
			} catch {
			}
			DialogResult = button.DialogResult;
			Close ();
		}

		private void zipEntry_ExceptionNotification (object sender, EventArgs e) {
			if (MultiExplorerControl.MainForm.Settings.ZipErrorFiles && (!(closing || IsDisposed || Disposing))) {
				if (MessageBox.Show (this, sender.ToString (), sender.GetType ().FullName, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
					try {
						closing = true;
						cancelButton_Click (button2, e);
					} catch {
					}
			}
		}

		private void zipFile_DirectoryError (object sender, EventArgs e) {
			if (MultiExplorerControl.MainForm.Settings.ZipErrorDirs && (!(closing || IsDisposed || Disposing))) {
				if (MessageBox.Show (this, sender.ToString (), sender.GetType ().FullName, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
					try {
						closing = true;
						cancelButton_Click (button2, e);
					} catch {
					}
			}
		}

		private void zipFile_FileError (object sender, EventArgs e) {
			if (MultiExplorerControl.MainForm.Settings.ZipErrorFiles && (!(closing || IsDisposed || Disposing))) {
				if (MessageBox.Show (this, sender.ToString (), sender.GetType ().FullName, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
					try {
						closing = true;
						cancelButton_Click (button2, e);
					} catch {
					}
			}
		}

		private void zipFile_InclusionFilter (object sender, CancelEventArgs e) {
			string [] files, subDirs;
			string path = sender as string;
			if (Directory.Exists (path)) {
				if ((((files = Directory.GetFiles (path)) == null) || (files.Length == 0)) && (((subDirs = Directory.GetDirectories (path)) == null) || (subDirs.Length == 0)) && !MultiExplorerControl.MainForm.Settings.ZipEmptyDirs)
					e.Cancel = true;
			} else if (File.Exists (path)) {
				using (FileStream s = File.OpenRead (path))
					if ((s.Length == 0) && !MultiExplorerControl.MainForm.Settings.ZipEmptyFiles)
						e.Cancel = true;
			} else
				e.Cancel = true;
			if ((!e.Cancel) && !MultiExplorerControl.IsIncluded ("Zip", path))
				e.Cancel = true;
		}

		protected override void OnLoad (EventArgs e) {
			base.OnLoad (e);
			ZipThread.Start ();
		}

		public void Start () {
			if (InvokeRequired)
				Invoke (Delegate.CreateDelegate (typeof (Action), this, "Start"));
			else {
				ZipFile.DirectoryError += zipFile_DirectoryError;
				ZipFile.FileError += zipFile_FileError;
				ZipFile.InclusionFilter += zipFile_InclusionFilter;
				ZipAction (ZipFile);
				closing = true;
				DialogResult = DialogResult.OK;
				Close ();
			}
		}

	}

}
