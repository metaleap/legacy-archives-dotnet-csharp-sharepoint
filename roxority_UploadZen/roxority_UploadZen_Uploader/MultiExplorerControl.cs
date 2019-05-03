
using Ionic.Zip;
using Microsoft.VisualBasic.FileIO;
using roxority.Shared;
using roxority.Shared.ComponentModel;
using roxority.Shared.Drawing;
using roxority.Shared.IO;
using roxority.Shared.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Windows.Forms;
using System.Xml;
using WssAuth = roxUp.WssAuthentication;

using SharedCustomTypeDescriptor = roxority.Shared.ComponentModel.CustomTypeDescriptor;
using SystemTypeDescriptor = System.ComponentModel.TypeDescriptor;

namespace roxUp {

	using res = Properties.Resources;

	public partial class MultiExplorerControl : UserControl {

		#region Status Enumeration

		public enum Status {

			None,
			DynZip,
			Failed,
			Success,
			Uploading

		}

		#endregion

		public event EventHandler PropertyChanged, UploadBusy, UploadNonBusy;

		internal static readonly Properties.Settings config = Properties.Settings.Default;
		internal static readonly Dictionary<string, Status> statuses = new Dictionary<string, Status> ();
		internal static readonly string [] nonZipExts = { "zip", "rar", "zip", "rar", "arj", "lzh", "ace", "tar", "gz", "uue", "bz2", "jar", "iso", "z", "7z", "gif", "jfif", "jif", "jpe", "jpeg", "jpg", "png" };

		internal readonly Dictionary<string, ToolStripMenuItem> preFilters = new Dictionary<string, ToolStripMenuItem> ();
		internal readonly List<string> blockedExtensions = new List<string> (new string [] { "ade", "adp", "app", "asa", "ashx", "asmx", "asp", "bas", "bat", "cdx", "cer", "chm", "class", "cmd", "cnt", "com", "config", "cpl", "crt", "csh", "der", "dll", "exe", "fxp", "gadget", "hlp", "hpj", "hta", "htr", "htw", "ida", "idc", "idq", "ins", "isp", "its", "jse", "ksh", "lnk", "mad", "maf", "mag", "mam", "maq", "mar", "mas", "mat", "mau", "mav", "maw", "mda", "mdb", "mde", "mdt", "mdw", "mdz", "msc", "msh", "msh1", "msh1xml", "msh2", "msh2xml", "mshxml", "msi", "msp", "mst", "ops", "pcd", "pif", "prf", "prg", "printer", "pst", "reg", "rem", "scf", "scr", "sct", "shb", "shs", "shtm", "shtml", "soap", "stm", "url", "vb", "vbe", "vbs", "ws", "wsc", "wsf", "wsh" });

		internal Cookie lastCookie = null;
		internal bool allowFolderChange = true, allowListChange = true, allowSavePassword = true, browseFolders = RegistryUtil.GetBoolean ("ShowFolders", true), checkBusy = false, firstAttempt = true, forceLogin = false, hideHelp = false, hideLink = false, hideNote = false, hideTutorial = false, isFormsAuth = false, isUploadBusy = false, isUserLoginStored, noHistory = false;
		internal string autoCheckIn = string.Empty, filter = RegistryUtil.GetString ("filter", config.DefaultFilter), li = "x", listName = RegistryUtil.GetString ("listname", config.ListName), folderName = config.FolderName, logonDomain = RegistryUtil.GetString ("ld", string.Empty), logonUser = RegistryUtil.GetString ("lu", string.Empty), logonPass, url = RegistryUtil.GetString ("url", config.roxority_UploadZen_Uploader_roxority_UploadZen_Files), zipHandling = "prompt";
		internal Stack<string> pathHistory = new Stack<string> ();

		private readonly Image infoBackgroundImage;

		private bool refreshUploadTitleLastAfterSuccess = false;
		private int uploadPending = 0, uploadPercent = 0, uploadTotal = 0, uploadCompleted = 0, uploadFailed = 0;

		public MultiExplorerControl () {
			Font baseFont;
			isUserLoginStored = !string.IsNullOrEmpty (logonPass = RegistryUtil.GetString ("lp", string.Empty, string.IsNullOrEmpty (logonDomain) ? logonUser : logonDomain + '\\' + logonUser));
			InitializeComponent ();
			browseGroupBox.Font = uploadGroupBox.Font = new Font (toolStrip.Font, FontStyle.Bold);
			foreach (GroupBox groupBox in new GroupBox [] { browseGroupBox, uploadGroupBox })
				foreach (Control ctl in groupBox.Controls)
					ctl.Font = Font;
			preFilters ["101"] = browseFiltersDocumentsToolStripItem;
			preFilters ["114"] = preFilters ["111"] = browseFiltersTemplatesToolStripItem;
			preFilters ["116"] = browseFiltersMastersToolStripItem;
			preFilters ["109"] = browseFiltersImagesToolStripItem;
			preFilters ["113"] = browseFiltersWebPartsToolStripItem;
			browseFoldersToolStripButton.Checked = ShowFolders;
			Filter = filter;
			foreach (ToolStripItem item in browseFiltersToolStripButton.DropDownItems)
				if ((!(item is ToolStripSeparator)) && (filter.Equals (item.Tag + "")))
					toolStripFilterItem_Click (item, EventArgs.Empty);
			toolStripUploadButton.Font = new Font (baseFont = toolStripUploadButton.Font, FontStyle.Bold);
			foreach (ToolStripItem subItem in toolStripUploadButton.DropDownItems)
				subItem.Font = baseFont;
			using (Graphics gfx = Graphics.FromImage (infoBackgroundImage = new Bitmap (4, 4, PixelFormat.Format24bppRgb))) {
				gfx.Clear (SystemColors.Info);
				gfx.Flush ();
			}
		}

		private void browseFoldersToolStripButton_Click (object sender, EventArgs e) {
			ShowFolders = browseFoldersToolStripButton.Checked;
		}

		private void browseHistoryMenuItem_Click (object sender, EventArgs e) {
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			ToolStripSplitButton button = ((item == null) ? (sender as ToolStripSplitButton) : item.OwnerItem as ToolStripSplitButton);
			if (item == null)
				item = button.DropDownItems [button.DropDownItems.Count - 1] as ToolStripMenuItem;
			noHistory = true;
			ChangeDirectory (item.Text, true);
			for (int i = 0; ((i < (int) item.Tag) && (pathHistory.Count > 0)); i++)
				pathHistory.Pop ();
			noHistory = false;
			RefreshHistory ();
		}

		private void browseHistoryToolStripItem_DropDownOpening (object sender, EventArgs e) {
			ToolStripSplitButton button = sender as ToolStripSplitButton;
			ToolStripMenuItem item;
			Stack<string> stack = pathHistory;
			int distance = 0;
			button.DropDownItems.Clear ();
			foreach (string s in stack) {
				button.DropDownItems.Insert (0, item = new ToolStripMenuItem (s, null, browseHistoryMenuItem_Click));
				item.Tag = ++distance;
				item.Enabled = Directory.Exists (s);
			}
		}

		private void browseListView_ItemActivate (object sender, EventArgs e) {
			List<string> dirs = new List<string> (), files = new List<string> ();
			string path;
			foreach (ListViewItem item in browseListView.SelectedItems)
				if (Directory.Exists (path = Path.Combine (browseListView.Tag + "", item.Text)))
					dirs.Add (path);
				else if (File.Exists (path))
					files.Add (path);
			toolStripAddButton_Click (null, EventArgs.Empty);
			if (dirs.Count == 1)
				ChangeDirectory (dirs [0], true);
		}

		private void browseListView_SelectedIndexChanged (object sender, EventArgs e) {
			bool hasDir = false, hasFile = false;
			foreach (ListViewItem item in browseListView.SelectedItems)
				if (File.Exists (Path.Combine (browseListView.Tag + "", item.Text)))
					hasFile = true;
				else if (Directory.Exists (Path.Combine (browseListView.Tag + "", item.Text)))
					hasDir = true;
			toolStripAddZipItem.Enabled = ((toolStripAddButton.Enabled = (hasFile && !hasDir) || (hasDir && (li == "u"))) && hasFile && (li == "u"));
			contextAddZipItem.Enabled = ((contextOpenWithItem.Enabled = contextAddItem.Enabled = hasFile) && (li == "u"));
			contextFolderItem.Enabled = ((hasDir || (browseListView.SelectedItems.Count == 0)) && (li == "u"));
			contextRefreshItem.Enabled = !(contextDeleteItem.Enabled = hasFile || hasDir);
			RefreshStatus ();
		}

		private void browseUpToolStripButton_Click (object sender, EventArgs e) {
			if (Path.GetPathRoot (browseListView.Tag + string.Empty).TrimEnd ('\\').Equals ((browseListView.Tag + string.Empty).TrimEnd ('\\'), StringComparison.InvariantCultureIgnoreCase))
				toolStripPathButton_Click (toolStripPathButton, EventArgs.Empty);
			else
				ChangeDirectory (Directory.GetParent (browseListView.Tag as string).FullName, true);
		}

		private void contextMenuItem_Click (object sender, EventArgs e) {
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			string temp = browseListView.Tag + "", path;
			List<string> dirPaths = new List<string> ();
			try {
				if (item == contextAddItem)
					toolStripAddButton_Click (null, EventArgs.Empty);
				else if (item == contextAddZipItem)
					toolStripAddButton_Click (toolStripAddZipItem, EventArgs.Empty);
				else if (item == contextFolderItem) {
					if (browseListView.SelectedItems.Count == 0)
						toolStripAddButton_Click (toolStripAddFolderItem, EventArgs.Empty);
					else
						foreach (ListViewItem listItem in browseListView.SelectedItems)
							if (Directory.Exists (path = Path.Combine (temp, listItem.Text))) {
								browseListView.Tag = path;
								toolStripAddButton_Click (toolStripAddFolderItem, EventArgs.Empty);
								browseListView.Tag = temp;
							}
					RefreshBrowseListView ();
				} else if ((item == contextDeleteItem) && (MessageBox.Show (this, res.DelPrompt, Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)) {
					foreach (ListViewItem listItem in browseListView.SelectedItems)
						if (Directory.Exists (path = Path.Combine (temp, listItem.Text)))
							FileSystem.DeleteDirectory (path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
						else if (File.Exists (path))
							FileSystem.DeleteFile (path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin, UICancelOption.DoNothing);
					RefreshBrowseListView ();
				} else if (item == contextOpenItem) {
					if (browseListView.SelectedItems.Count == 0)
						using (ShellInfo shell = new ShellInfo (browseListView.Tag + ""))
							shell.Open (Handle, true);
					else
						foreach (ListViewItem listItem in browseListView.SelectedItems)
							using (ShellInfo shell = new ShellInfo (Path.Combine (browseListView.Tag + "", listItem.Text)))
								shell.Open (Handle, true);
				} else if (item == contextPropertiesItem) {
					if (browseListView.SelectedItems.Count == 0)
						ShellInfo.ShowProperties (temp);
					else
						foreach (ListViewItem listItem in browseListView.SelectedItems)
							ShellInfo.ShowProperties (Path.Combine (temp, listItem.Text));
				} else if (item == contextRefreshItem)
					RefreshBrowseListView ();
				else if (item == contextOpenWithItem)
					foreach (ListViewItem listItem in browseListView.SelectedItems)
						if (File.Exists (path = Path.Combine (temp, listItem.Text)))
							ShellInfo.ShowOpenWith (path);
			} catch (Exception ex) {
				MessageBox.Show (this, ex.Message, ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			} finally {
				CheckForZips ();
			}
		}

		private void fileSystemWatcher_Event (object sender, FileSystemEventArgs e) {
			RefreshBrowseListView ();
		}

		private void fileSystemWatcher_Renamed (object sender, RenamedEventArgs e) {
			RefreshBrowseListView ();
		}

		private void metaListView_ItemCheck (object sender, ItemCheckEventArgs e) {
			e.NewValue = e.CurrentValue;
		}

		private void toolStripAddButton_Click (object sender, EventArgs e) {
			string filePath, tmp;
			ToolStripMenuItem senderItem = sender as ToolStripMenuItem;
			ListViewItem listViewItem;
			List<ListViewItem> newItems = new List<ListViewItem> (), folderItems = new List<ListViewItem> ();
			string tempZip = "|" + Guid.NewGuid ().ToString () + ".zip", temp;
			uploadListView.BeginUpdate ();
			if (senderItem == toolStripAddZipItem) {
				filePath = browseListView.Tag + "";
				foreach (ListViewItem item in browseListView.SelectedItems)
					if (File.Exists (tmp = Path.Combine (browseListView.Tag + "", item.Text).ToLowerInvariant ())) {
						statuses [tmp] = Status.DynZip;
						filePath += ('|' + item.Text);
					} else
						folderItems.Add (item);
				filePath += tempZip;
				if (filePath != (browseListView.Tag + "")) {
					if (!(uploadListView.Items.ContainsKey (filePath))) {
						(listViewItem = uploadListView.Items.Add (new ListViewItem (new string [] { ("[ZIP] " + filePath.Replace (tempZip, string.Empty).Substring (filePath.IndexOf ('|') + 1).Replace ("|", ", ")).Replace (" ", HttpUtility.HtmlDecode ("&nbsp;")), string.Empty }, string.Empty, GetListViewGroup (uploadListView, browseGroupBox.Text)))).Name = filePath;
						SetStatus (listViewItem, Status.None);
						uploadPending++;
					}
					newItems.Add (uploadListView.Items [filePath]);
				}
			} else if (senderItem == toolStripAddFolderItem) {
				filePath = browseListView.Tag as string;
				listViewItem = uploadListView.Items [filePath];
				if (senderItem.Checked && (listViewItem != null)) {
					foreach (ListViewItem item in uploadListView.Items)
						if (item.Selected = (item == listViewItem)) {
							newItems.Add (item);
							item.EnsureVisible ();
						}
					toolStripRemoveButton_Click (toolStripRemoveButton, EventArgs.Empty);
				} else if (!senderItem.Checked) {
					if (listViewItem == null) {
						(listViewItem = uploadListView.Items.Add (new ListViewItem (new string [] { GetFriendlyFolderName (filePath).Replace (" ", HttpUtility.HtmlDecode ("&nbsp;")), string.Empty }, string.Empty, GetListViewGroup (uploadListView, res.AddFolderGroup)))).Name = filePath;
						SetStatus (listViewItem, Status.None);
						uploadPending++;
					} else
						listViewItem.Group = GetListViewGroup (uploadListView, res.AddFolderGroup);
					newItems.Add (listViewItem);
				}
			} else
				foreach (ListViewItem item in browseListView.SelectedItems)
					if (Directory.Exists (filePath = Path.Combine (browseListView.Tag as string, item.Text))) {
						if (sender != null) {
							temp = browseListView.Tag as string;
							browseListView.Tag = filePath;
							toolStripAddButton_Click (toolStripAddFolderItem, null);
							browseListView.Tag = temp;
						}
						folderItems.Add (item);
					} else if (File.Exists (filePath)) {
						if (!(uploadListView.Items.ContainsKey (filePath))) {
							(listViewItem = uploadListView.Items.Add (new ListViewItem (new string [] { Path.GetFileName (filePath).Replace (" ", HttpUtility.HtmlDecode ("&nbsp;")), string.Empty }, string.Empty, GetListViewGroup (uploadListView, browseGroupBox.Text)))).Name = filePath;
							SetStatus (listViewItem, Status.None);
							uploadPending++;
						}
						newItems.Add (uploadListView.Items [filePath]);
					}
			if (e != null) {
				CheckForZips ();
				browseListView.SelectedItems.Clear ();
				foreach (ListViewItem item in folderItems) {
					item.Selected = true;
					item.EnsureVisible ();
				}
				foreach (ListViewItem item in uploadListView.Items)
					if (item.Selected = newItems.Contains (item))
						item.EnsureVisible ();
				RefreshListViewColors (true);
			}
			uploadListView.EndUpdate ();
			RefreshUploadTitle ();
		}

		private void toolStripCancelButton_Click (object sender, EventArgs e) {
			if (MessageBox.Show (this, res.CancelPrompt, Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes) {
				toolStripCancelButton.Enabled = false;
				uploadWorker.CancelAsync ();
			}
		}

		private void toolStripCheckInItem_Click (object sender, EventArgs e) {
			ToolStripMenuItem menuItem;
			toolStripCheckinItem.Checked = (sender != toolStripCheckInNoneItem);
			foreach (ToolStripItem item in toolStripCheckinItem.DropDownItems)
				if ((menuItem = item as ToolStripMenuItem) != null)
					if (menuItem.Checked = (menuItem == sender))
						autoCheckIn = menuItem.Tag + string.Empty;
		}

		private void toolStripDriveItem_ButtonClick (object sender, EventArgs e) {
			ToolStripItem item = sender as ToolStripItem;
			if ((item != null) && Directory.Exists (item.Tag + string.Empty))
				ChangeDirectory (item.Tag + "\\", true);
		}

		private void toolStripDriveItem_DropDownOpening (object sender, EventArgs e) {
			string text;
			ToolStripMenuItem item;
			toolStripDriveItem.DropDownItems.Clear ();
			try {
				foreach (string d in Directory.GetLogicalDrives ())
					try {
						if (Directory.Exists (d)) {
							item = toolStripDriveItem.DropDownItems.Add (text = d.TrimEnd ('\\'), toolStripDriveItem.Image, toolStripDriveItem_ButtonClick) as ToolStripMenuItem;
							item.Tag = text;
							item.Checked = item.Text.Equals (toolStripDriveItem.Text, StringComparison.InvariantCultureIgnoreCase);
						}
					} catch {
					}
			} catch {
			}
		}

		private void toolStripFilterItem_Click (object sender, EventArgs e) {
			ToolStripMenuItem item = sender as ToolStripMenuItem;
			string filter;
			if (item != null) {
				browseFiltersToolStripButton.Text = item.Text;
				browseFiltersToolStripButton.BackColor = ("*".Equals ((browseFiltersToolStripButton.Tag = filter = item.Tag + "") + "") ? Color.Transparent : ControlPaint.LightLight (SystemColors.Highlight));
				if (e != null)
					Filter = filter;
			}
		}

		private void toolStripMetaDataButton_Click (object sender, EventArgs e) {
			uploadSplitContainer.Panel2Collapsed = !(toolStripMetaDataButton.Checked = !toolStripMetaDataButton.Checked);
		}

		private void toolStripPathButton_Click (object sender, EventArgs e) {
			using (FolderBrowserDialog dialog = new FolderBrowserDialog ()) {
				dialog.Description = res.FolderDialog;
				dialog.RootFolder = Environment.SpecialFolder.Desktop;
				dialog.SelectedPath = browseListView.Tag as string;
				dialog.ShowNewFolderButton = true;
				if (dialog.ShowDialog (this) == DialogResult.OK)
					ChangeDirectory (dialog.SelectedPath, true);
			}
			browseListView_SelectedIndexChanged (browseListView, EventArgs.Empty);
			uploadListView_SelectedIndexChanged (uploadListView, EventArgs.Empty);
		}

		private void toolStripPathButton_DropDownOpening (object sender, EventArgs e) {
			toolStripAddFolderItem.Checked = false;
			foreach (ListViewItem item in uploadListView.Items)
				if ((item.Name.Equals (browseListView.Tag + "", StringComparison.InvariantCultureIgnoreCase)) && (item.Group.Name == res.AddFolderGroup))
					toolStripAddFolderItem.Checked = true;
		}

		private void toolStripRefreshButton_Click (object sender, EventArgs e) {
			RefreshBrowseListView ();
		}

		private void toolStripRemoveButton_Click (object sender, EventArgs e) {
			ListViewItem [] items = new ListViewItem [uploadListView.SelectedItems.Count];
			string [] splits;
			Status status;
			uploadListView.SelectedItems.CopyTo (items, 0);
			foreach (ListViewItem item in items) {
				uploadListView.Items.Remove (item);
				if (e != null)
					uploadPending--;
				if ((e != null) && ((!statuses.TryGetValue (item.Name.ToLowerInvariant (), out status)) || (status == Status.None))) {
					statuses.Remove (item.Name.ToLowerInvariant ());
					if (((splits = item.Name.Split (new char [] { '|' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (splits.Length > 1))
						for (int i = 1; i < splits.Length; i++)
							statuses.Remove (Path.Combine (splits [0], splits [i]).ToLowerInvariant ());
				} else if (((splits = item.Name.Split (new char [] { '|' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (splits.Length > 1))
					for (int i = 1; i < splits.Length; i++)
						statuses [Path.Combine (splits [0], splits [i]).ToLowerInvariant ()] = Status.Success;
			}
			RefreshUploadTitle ();
			CheckForZips ();
			RefreshListViewColors (true);
		}

		private void toolStripSelectAllItem_Click (object sender, EventArgs e) {
			foreach (ListView lv in new ListView [] { browseListView, uploadListView })
				foreach (ListViewItem item in lv.Items)
					item.Selected = true;
		}

		private void toolStripUploadButton_Click (object sender, EventArgs e) {
			OnUploadBusy (true);
			toolStripUploadButton.Visible = false;
			toolStripCancelButton.Visible = true;
			uploadWorker.RunWorkerAsync ();
		}

		private void toolStripUploadOpenItem_Click (object sender, EventArgs e) {
			XmlDocument doc = new XmlDocument ();
			ListViewItem listViewItem;
			ToolStripMenuItem menuItem;
			string val, itemName, itemText, filePath;
			int count;
			bool itemChecked;
			Converter<string, string> getAtt = delegate (string name) {
				XmlAttribute attNode = doc.DocumentElement.Attributes.GetNamedItem (name) as XmlAttribute;
				return ((attNode == null) ? null : attNode.Value);
			};
			if ((sender!=toolStripUploadOpenItem)|| (openFileDialog.ShowDialog (this) == DialogResult.OK))
				try {
					doc.Load (filePath = ((sender == toolStripUploadOpenItem) ? openFileDialog.FileName : ((ToolStripMenuItem) sender).Text));
					toolStripCheckOutItem.Checked = "1".Equals (getAtt ("AutoCheckOut"));
					toolStripCheckinItem.Checked = !string.IsNullOrEmpty (autoCheckIn = getAtt ("AutoCheckIn"));
					foreach (ToolStripItem item in toolStripCheckinItem.DropDownItems)
						if ((menuItem = item as ToolStripMenuItem) != null)
							menuItem.Checked = autoCheckIn.Equals (menuItem.Tag + string.Empty);
					if (!string.IsNullOrEmpty (val = getAtt ("Path")))
						ChangeDirectory (val, true);
					if (!string.IsNullOrEmpty (val = getAtt ("Url")))
						Url = val;
					if (!string.IsNullOrEmpty (val = getAtt ("ListName")))
						ListName = val;
					if (!string.IsNullOrEmpty (val = getAtt ("FolderName")))
						FolderName = val;
					if ((!string.IsNullOrEmpty (val = getAtt ("ItemCount"))) && int.TryParse (val, out count) && (count > 0)) {
						toolStripSelectAllItem_Click (toolStripSelectAllItem, e);
						toolStripRemoveButton_Click (toolStripRemoveButton, e);
						for (int i = 0; i < count; i++)
							if ((!string.IsNullOrEmpty (itemText = getAtt ("Item_" + i + "_Text"))) && (!string.IsNullOrEmpty (itemName = getAtt ("Item_" + i + "_Name")))) {
								(listViewItem = uploadListView.Items.Add (new ListViewItem (new string [] { itemText, string.Empty }, string.Empty, GetListViewGroup (uploadListView, getAtt ("Item_" + i + "_Group"))))).Name = itemName;
								if ((!string.IsNullOrEmpty (val = getAtt ("Item_" + i + "_Checked"))) && bool.TryParse (val, out itemChecked))
									listViewItem.Checked = itemChecked;
								SetStatus (listViewItem, Status.None);
							}
					}
					AddToHistory (filePath);
				} catch (Exception ex) {
					MessageBox.Show (this, ex.Message, ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				} finally {
					Application.DoEvents ();
					RefreshUploadTitle ();
					Application.DoEvents ();
					CheckForZips ();
					Application.DoEvents ();
					System.Threading.Thread.Sleep (111);
					Application.DoEvents ();
					if ((doc != null) && (doc.DocumentElement != null) && (doc.DocumentElement.Attributes.Count > 0) && (!string.IsNullOrEmpty (val = getAtt ("Filter"))))
						Filter = val;
				}
		}

		private void toolStripUploadSaveItem_Click (object sender, EventArgs e) {
			XmlDocument doc = new XmlDocument ();
			int index = 0;
			Action<string, string> addAtt = delegate (string name, string value) {
				doc.DocumentElement.Attributes.Append (doc.CreateAttribute (name)).Value = value;
			};
			if (saveFileDialog.ShowDialog (this) == DialogResult.OK)
				try {
					doc.LoadXml ("<UploadZen/>");
					addAtt ("AutoCheckIn", autoCheckIn);
					addAtt ("AutoCheckOut", toolStripCheckOutItem.Checked ? "1" : "0");
					addAtt ("Path", browseListView.Tag + "");
					addAtt ("Filter", Filter);
					addAtt ("Url", Url);
					addAtt ("ListName", ListName);
					addAtt ("FolderName", FolderName);
					addAtt ("ItemCount", uploadListView.Items.Count.ToString ());
					foreach (ListViewItem item in uploadListView.Items) {
						addAtt ("Item_" + index + "_Checked", item.Checked.ToString ());
						addAtt ("Item_" + index + "_Group", item.Group.Header);
						addAtt ("Item_" + index + "_Name", item.Name);
						addAtt ("Item_" + index + "_Text", item.Text);
						index++;
					}
					doc.Save (saveFileDialog.FileName);
					AddToHistory (saveFileDialog.FileName);
				} catch (Exception ex) {
					MessageBox.Show (this, ex.Message, ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				}
		}

		private void toolStripUserForgetItem_Click (object sender, EventArgs e) {
			LogonUser = LogonPass = LogonDomain = string.Empty;
			lastCookie = null;
			forceLogin = true;
			isUserLoginStored = false;
			UpdateUserItem ();
		}

		private void uploadListView_ItemActivate (object sender, EventArgs e) {
			string msg = uploadListView.SelectedItems [0].SubItems [1].Text;
			if (msg.StartsWith ("[") && msg.EndsWith ("]"))
				msg = msg.Substring (1, msg.Length - 2);
			MessageBox.Show (this, msg.Trim (), uploadListView.SelectedItems [0].Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			CheckForZips ();
		}

		private void uploadListView_ItemCheck (object sender, ItemCheckEventArgs e) {
			ListViewItem item = null;
			if (isUploadBusy)
				e.NewValue = e.CurrentValue;
			else if ((e.Index >= 0) && (e.Index < uploadListView.Items.Count) && ((item = uploadListView.Items [e.Index]) != null))
				if (!item.Name.EndsWith (".zip", StringComparison.InvariantCultureIgnoreCase))
					e.NewValue = CheckState.Indeterminate;
				else if (zipHandling == "zipped")
					e.NewValue = CheckState.Unchecked;
				else if (zipHandling == "unzip")
					e.NewValue = CheckState.Checked;
			CheckForZips ();
		}

		private void uploadListView_SelectedIndexChanged (object sender, EventArgs e) {
			toolStripRemoveButton.Enabled = ((uploadListView.SelectedItems.Count > 0) /*&& !isUploadBusy*/);
			RefreshUploadTitle ();
		}

		private void uploadWorker_DoWork (object sender, DoWorkEventArgs e) {
			ListViewItem item = null;
			Nullable<bool> result;
			CookieCollection cookies;
			Cookie cookie;
			DialogResult prompt;
			WssAuth.LoginResult authResult;
			bool authError = false;
			int pos;
			if (uploadListView.InvokeRequired)
				uploadListView.Invoke (new DoWorkEventHandler (uploadWorker_DoWork), sender, e);
			else if (!(e.Cancel))
				try {
					uploadPending = uploadTotal = uploadListView.Items.Count;
					uploadCompleted = uploadFailed = uploadPercent = 0;
					RefreshStatus ();
					using (Uploader uploader = new Uploader (false)) {
						uploader.Url = Url;
						if (isFormsAuth)
							using (WssAuth.Authentication auth = new WssAuth.Authentication ())
								try {
									auth.Url = Url.Replace ("_layouts/roxority_UploadZen/Files.asmx", "_vti_bin/Authentication.asmx");
									auth.CookieContainer = new CookieContainer ();
									if (((cookie = lastCookie) != null) || ((!(authError = (((authResult = auth.Login (LogonUser, LogonPass)) == null) || (authResult.ErrorCode != WssAuth.LoginErrorCode.NoError)))) && (!string.IsNullOrEmpty (authResult.CookieName)) && ((cookies = auth.CookieContainer.GetCookies (new Uri (auth.Url))) != null) && ((cookie = cookies [authResult.CookieName]) != null))) {
										uploader.CookieContainer = new CookieContainer ();
										uploader.CookieContainer.Add (lastCookie = cookie);
									} else {
										lastCookie = null;
										if (authResult == null)
											throw new UnauthorizedAccessException ();
										else if (authResult.ErrorCode == WssAuth.LoginErrorCode.NotInFormsAuthenticationMode) {
											isFormsAuth = false;
											uploader.PreAuthenticate = true;
											uploader.Credentials = ((string.IsNullOrEmpty (LogonUser) && !forceLogin) ? CredentialCache.DefaultNetworkCredentials : (string.IsNullOrEmpty (LogonDomain) ? new NetworkCredential (LogonUser, LogonPass) : new NetworkCredential (LogonUser, LogonPass, LogonDomain)));
										} else if (authResult.ErrorCode != WssAuth.LoginErrorCode.NoError)
											throw new UnauthorizedAccessException (res.AuthError_PasswordNotMatch);
									}
								} catch (Exception ex) {
									authError = true;
									lastCookie = null;
									MessageBox.Show (this, ex.Message, ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
								} else {
							uploader.PreAuthenticate = true;
							uploader.Credentials = ((string.IsNullOrEmpty (LogonUser) && !forceLogin) ? CredentialCache.DefaultNetworkCredentials : (string.IsNullOrEmpty (LogonDomain) ? new NetworkCredential (LogonUser, LogonPass) : new NetworkCredential (LogonUser, LogonPass, LogonDomain)));
						}
						for (int i = 0; i < uploadListView.Items.Count; i++) {
							uploadPercent = SharedUtil.Percent (i, uploadTotal = uploadListView.Items.Count);
							RefreshStatus ();
							SetStatus (item = uploadListView.Items [i], Status.Uploading);
							foreach (ListViewItem upItem in uploadListView.Items)
								if (upItem != item) {
									upItem.BackColor = SystemColors.Window;
									upItem.ForeColor = SystemColors.WindowText;
								}
							item.BackColor = SystemColors.Info;
							item.ForeColor = SystemColors.InfoText;
							if (!(uploadListView.Focused || uploadListView.ContainsFocus))
								item.EnsureVisible ();
							Application.DoEvents ();
							if (authError) {
								authError = false;
								using (LoginForm loginForm = new LoginForm (allowSavePassword, isUserLoginStored)) {
									if ((!isFormsAuth) && string.IsNullOrEmpty (logonPass) && string.IsNullOrEmpty (logonDomain) && string.IsNullOrEmpty (logonUser)) {
										logonDomain = Environment.UserDomainName;
										logonUser = Environment.UserName;
									}
									loginForm.userNameTextBox.Text = (string.IsNullOrEmpty (logonDomain) ? logonUser : (logonDomain + '\\' + logonUser));
									loginForm.passwordTextBox.Text = logonPass;
									prompt = ((isFormsAuth && firstAttempt && (!string.IsNullOrEmpty (logonPass)) && (!string.IsNullOrEmpty (logonUser))) ? DialogResult.OK : loginForm.ShowDialog (this));
									firstAttempt = false;
									if (prompt != DialogResult.OK) {
										SetStatus (item, Status.None);
										e.Cancel = true;
										break;
									} else {
										logonDomain = (((pos = loginForm.userNameTextBox.Text.IndexOf ('\\')) > 0) ? loginForm.userNameTextBox.Text.Substring (0, pos) : string.Empty);
										logonUser = loginForm.userNameTextBox.Text.Substring ((pos >= 0) ? (pos + 1) : 0);
										logonPass = loginForm.passwordTextBox.Text;
										SaveOrForgetUserData (loginForm.checkBox.Checked);
										if (isFormsAuth)
											using (WssAuth.Authentication auth = new WssAuth.Authentication ())
												try {
													auth.Url = Url.Replace ("_layouts/roxority_UploadZen/Files.asmx", "_vti_bin/Authentication.asmx");
													auth.CookieContainer = new CookieContainer ();
													if (((cookie = lastCookie) != null) || ((!(authError = (((authResult = auth.Login (LogonUser, LogonPass)) == null) || (authResult.ErrorCode != WssAuth.LoginErrorCode.NoError)))) && (!string.IsNullOrEmpty (authResult.CookieName)) && ((cookies = auth.CookieContainer.GetCookies (new Uri (auth.Url))) != null) && ((cookie = cookies [authResult.CookieName]) != null))) {
														uploader.CookieContainer = new CookieContainer ();
														uploader.CookieContainer.Add (lastCookie = cookie);
													} else {
														lastCookie = null;
														if (authResult == null)
															throw new UnauthorizedAccessException ();
														else if (authResult.ErrorCode == WssAuth.LoginErrorCode.NotInFormsAuthenticationMode) {
															isFormsAuth = false;
															uploader.PreAuthenticate = true;
															uploader.Credentials = ((string.IsNullOrEmpty (LogonUser) && !forceLogin) ? CredentialCache.DefaultNetworkCredentials : (string.IsNullOrEmpty (LogonDomain) ? new NetworkCredential (LogonUser, LogonPass) : new NetworkCredential (LogonUser, LogonPass, LogonDomain)));
														} else if (authResult.ErrorCode != WssAuth.LoginErrorCode.NoError)
															throw new UnauthorizedAccessException (res.AuthError_PasswordNotMatch);
													}
												} catch (Exception ex) {
													lastCookie = null;
													MessageBox.Show (this, ex.Message, ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
												} else {
											uploader.PreAuthenticate = true;
											uploader.Credentials = ((string.IsNullOrEmpty (LogonUser) && !forceLogin) ? CredentialCache.DefaultNetworkCredentials : (string.IsNullOrEmpty (LogonDomain) ? new NetworkCredential (LogonUser, LogonPass) : new NetworkCredential (LogonUser, LogonPass, LogonDomain)));
										}
									}
								}
							}
							if (((result = DoUpload (uploader, item, ref authError)) != null) && (result.HasValue)) {
								SetStatus (item, result.Value ? Status.Success : Status.None);
								if (result.Value) {
									uploadPending--;
									uploadCompleted++;
									RefreshStatus ();
								}
							}
							if (authError)
								i--;
							else if ((uploadWorker.CancellationPending) || (e.Cancel)) {
								SetStatus (item, Status.None);
								e.Cancel = true;
								break;
							}
							UpdateUserItem ();
						}
					}
				} catch (Exception ex) {
					if (item != null) {
						uploadPending--;
						uploadFailed++;
						RefreshStatus ();
						SetStatus (item, Status.Failed, ex.ToString ());
					} else
						MessageBox.Show (this, ex.ToString (), ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
		}

		private void uploadWorker_RunWorkerCompleted (object sender, RunWorkerCompletedEventArgs e) {
			uploadPercent = 100;
			toolStripCancelButton.Visible = false;
			toolStripCancelButton.Enabled = toolStripUploadButton.Visible = true;
			uploadListView.BeginUpdate ();
			foreach (ListViewItem item in uploadListView.Items) {
				item.BackColor = SystemColors.Window;
				item.ForeColor = SystemColors.WindowText;
				if (item.Selected = Status.Success.Equals (item.Tag))
					item.EnsureVisible ();
			}
			uploadListView.EndUpdate ();
			toolStripRemoveButton_Click (sender, null);
			RefreshStatus ();
			OnUploadBusy (false);
		}

		internal void AddToHistory (string filePath) {
			List<string> history = History;
			if ((!string.IsNullOrEmpty (filePath)) && File.Exists (filePath) && (!history.Contains (filePath))) {
				history.Add (filePath);
				History = history;
			}
			if (toolStripUploadRecentItem.Enabled = (history.Count > 0)) {
				toolStripUploadRecentItem.DropDownItems.Clear ();
				foreach (string path in history)
					toolStripUploadRecentItem.DropDownItems.Add (path, null, toolStripUploadOpenItem_Click);
			}
		}

		internal void ChangeDirectory (string filePath, bool beginEndUpdate) {
			string [] subItems, selfIconExts = { "exe", "ico" }, selfImageExts = {"emf","wmf", "exif","bmp", "gif", "png", "jfif", "jif", "jpe", "jpeg", "jpg", "tif", "tiff" };
			List<string> allFilePaths = new List<string> ();
			Image selfImage;
			SizeF selfImageSize;
			bool isSelfIcon = false, isSelfImage = false;
			int hiddenDirCount = 0, hiddenDirCountNoFolders = 0, hiddenDirCountSettings = 0, hiddenFileCount = 0, hiddenFileCountFilters = 0, hiddenFileCountSettings = 0, selfImageCounts = 0;
			if (MainForm == null)
				return;
			fileSystemWatcher.EnableRaisingEvents = false;
			if (!(Directory.Exists (filePath)))
				filePath = Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments);
			toolStripDriveItem.Tag = toolStripDriveItem.Text = Path.GetPathRoot (filePath).TrimEnd ('\\');
			toolStripAddFolderItem.Text = string.Format (res.AddFolder, new DirectoryInfo (filePath).Name, (ListName + '/' + FolderName).Trim ('/'), SharedUtil.ReplaceCharacters (new DirectoryInfo (filePath).Name, Settings.SPInvalidFileNameCharacters, MainForm.Settings.Replace));
			RegistryUtil.SetString ("lastdir", filePath);
			if ((!noHistory) && (!(string.IsNullOrEmpty (browseListView.Tag as string) || filePath.Equals (browseListView.Tag as string, StringComparison.InvariantCultureIgnoreCase))))
				pathHistory.Push (browseListView.Tag as string);
			browseListView.Tag = fileSystemWatcher.Path = filePath;
			browseGroupBox.Text = GetFriendlyFolderName (filePath);
			browseListView.Items.Clear ();
			for (int i = 0; i < fileIconList.Images.Count; i++)
				if (fileIconList.Images.Keys [i].StartsWith ("--__--")) {
					fileIconList.Images.RemoveAt (i);
					i--;
				}
			try {
				if (beginEndUpdate)
					browseListView.BeginUpdate ();
				hiddenDirCountNoFolders = hiddenDirCount = Directory.GetDirectories (filePath).Length;
				if (ShowFolders) {
					hiddenDirCountNoFolders = 0;
					foreach (string dir in Directory.GetDirectories (filePath)) {
						if (IsIncluded ("Browse", dir))
							try {
								if ((((subItems = Directory.GetFileSystemEntries (dir)) != null) && (subItems.Length > 0)) || MainForm.Settings.BrowseEmptyDirs) {
									browseListView.Items.Add (new ListViewItem (new string [] { Path.GetFileName (dir), ((subItems == null) || (subItems.Length == 0)) ? res.Empty : string.Format (res.Count, subItems.Length) }, GetListViewGroup (browseListView, res.Folder))).ImageIndex = 0;
									hiddenDirCount--;
								} else
									hiddenDirCountSettings++;
							} catch (Exception ex) {
								if (MainForm.Settings.BrowseErrorDirs) {
									browseListView.Items.Add (new ListViewItem (new string [] { Path.GetFileName (dir), "[ " + ex.Message.Replace (dir, string.Empty) + " ]" }, GetListViewGroup (browseListView, res.Folder))).ImageIndex = 4;
									hiddenDirCount--;
								} else
									hiddenDirCountSettings++;
							} else
							hiddenDirCountSettings++;
					}
				}
				hiddenFileCountFilters = hiddenFileCountSettings = hiddenFileCount = Directory.GetFiles (filePath).Length;
				if (MainForm.Settings.PreviewThreshold > 0)
					foreach (string ext in selfImageExts)
						selfImageCounts += Directory.GetFiles (filePath, "*." + ext).Length;
				foreach (string filter in Filter.Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries))
					foreach (string file in Directory.GetFiles (filePath, filter, System.IO.SearchOption.TopDirectoryOnly)) {
						hiddenFileCountFilters--;
						if (IsIncluded ("Browse", file) && !allFilePaths.Contains (file.ToLowerInvariant ()))
							try {
								allFilePaths.Add (file.ToLowerInvariant ());
								using (ShellInfo shell = new ShellInfo (file)) {
									if ((isSelfIcon = (Array.IndexOf<string> (selfIconExts, shell.DotLessExtension) >= 0)) || (isSelfImage = ((MainForm.Settings.PreviewThreshold > 0) && (selfImageCounts <= MainForm.Settings.PreviewThreshold) && (Array.IndexOf<string> (selfImageExts, shell.DotLessExtension) >= 0))) || (!fileIconList.Images.ContainsKey (shell.DotLessExtension)))
										if (isSelfImage) {
											try {
												using (Image img = Image.FromFile (file))
												using (Graphics gfx = Graphics.FromImage (selfImage = new Bitmap (fileIconList.ImageSize.Width, fileIconList.ImageSize.Height, PixelFormat.Format32bppArgb))) {
													gfx.Clear (Color.Transparent);
													gfx.CompositingQuality = CompositingQuality.HighQuality;
													gfx.InterpolationMode = InterpolationMode.High;
													gfx.SmoothingMode = SmoothingMode.HighQuality;
													gfx.FillRectangle (SystemBrushes.Window, -2, -2, selfImage.Width + 8, selfImage.Height + 8);
													gfx.DrawRectangle (SystemPens.Control, 0, 0, selfImage.Width - 1, selfImage.Height - 1);
													selfImageSize = new SizeF ((float) img.Width, (float) img.Height);
													if ((img.Width > selfImage.Width - 4) || (img.Height > selfImage.Height - 4))
														if (img.Width > img.Height)
															selfImageSize = DrawingUtil.ScaleToWidth (selfImageSize, (float) selfImage.Width - 4);
														else
															selfImageSize = DrawingUtil.ScaleToHeight (selfImageSize, (float) selfImage.Height - 4);
													gfx.DrawImage (img, new RectangleF (2f + ((selfImage.Width - 4 - selfImageSize.Width) / 2), 1f + ((selfImage.Height - 4 - selfImageSize.Height) / 2), selfImageSize.Width, selfImageSize.Height));
													gfx.Flush ();
												}
											} catch {
												selfImage = shell.Image;
											}
											fileIconList.Images.Add ("--__--" + file, selfImage);
										} else
											fileIconList.Images.Add (isSelfIcon ? file : shell.DotLessExtension, shell.Image);
									using (FileStream stream = File.OpenRead (file))
										if ((stream.Length > 0) || MainForm.Settings.BrowseEmptyFiles) {
											browseListView.Items.Add (new ListViewItem (new string [] { Path.GetFileName (file), "[ " + IOUtil.FormatFileSize (IOUtil.GetFileSize (stream.Length)) + ", " + File.GetAttributes (file) + " ]" }, GetListViewGroup (browseListView, shell.FileType))).ImageKey = (isSelfImage ? ("--__--" + file) : (isSelfIcon ? file : shell.DotLessExtension));
											hiddenFileCount--;
											hiddenFileCountSettings--;
										}
								}
							} catch (Exception ex) {
								if (MainForm.Settings.BrowseErrorFiles) {
									browseListView.Items.Add (new ListViewItem (new string [] { Path.GetFileName (file), "[ " + ex.GetType ().FullName + " ]" }, GetListViewGroup (browseListView, "[ " + ex.Message.Replace (" \'" + file + "\'", string.Empty).Replace (" \"" + file + "\"", string.Empty).Replace (file, string.Empty) + " ]")));
									hiddenFileCount--;
									hiddenFileCountSettings--;
								}
							}
					}
				hiddenFileCountSettings -= hiddenFileCountFilters;
			} catch (Exception ex) {
				MessageBox.Show (this, ex.Message, ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			} finally {
				RefreshListViewColors (false);
				if (beginEndUpdate)
					browseListView.EndUpdate ();
			}
			if (browseHiddenToolStripItem.Visible = !string.IsNullOrEmpty (browseHiddenToolStripItem.Text = (((hiddenDirCount == 0) && (hiddenFileCount == 0)) ? string.Empty : (res.Hidden + " ") + ((hiddenDirCount == 0) ? string.Empty : string.Format (res.HiddenDirs, hiddenDirCount)) + (((hiddenDirCount != 0) && (hiddenFileCount != 0)) ? ", " : string.Empty) + ((hiddenFileCount == 0) ? string.Empty : string.Format (res.HiddenFiles, hiddenFileCount))))) {
				browseHiddenToolStripItem.DropDownItems.Clear ();
				if (hiddenDirCountNoFolders > 0)
					browseHiddenToolStripItem.DropDownItems.Add (res.HiddenDirsNoFolders, res.MoveFolderHS, browseFoldersToolStripButton_Click);
				if (hiddenFileCountFilters > 0)
					browseHiddenToolStripItem.DropDownItems.Add (string.Format (res.HiddenFilesFilters, hiddenFileCountFilters), res.Filter2HS, delegate (object sender, EventArgs e) {
						MainForm.tabControl.SelectedIndex = 0;
						if (browseFiltersToolStripButton.IsOnOverflow)
							browseToolStrip.OverflowButton.ShowDropDown ();
						browseFiltersToolStripButton.ShowDropDown ();
					});
				if ((hiddenDirCountSettings > 0) || (hiddenFileCountSettings > 0))
					browseHiddenToolStripItem.DropDownItems.Add (string.Format (res.HiddenDirsSettings, hiddenFileCountSettings, hiddenDirCountSettings), res.PropertiesHS, delegate (object sender, EventArgs e) {
						MainForm.tabControl.SelectedTab = MainForm.prepTabPage;
					}).Enabled = MainForm.tabControl.TabPages.Contains (MainForm.prepTabPage);
			}
			RefreshHistory ();
			RefreshStatus ();
			fileSystemWatcher.EnableRaisingEvents = !(filePath.StartsWith (Directory.GetParent (Environment.GetFolderPath (Environment.SpecialFolder.System)).FullName.TrimEnd ('\\', '/'), StringComparison.InvariantCultureIgnoreCase) || (Directory.GetFiles (filePath).Length > 2000));
		}

		internal void CheckForZips () {
			bool hasZip = false;
			List<ListViewItem> nonZipItems = new List<ListViewItem> ();
			Dictionary<ListViewItem, ListViewGroup> groups = new Dictionary<ListViewItem, ListViewGroup> ();
			if (!checkBusy) {
				checkBusy = true;
				foreach (ListViewItem item in uploadListView.Items)
					if (!item.Name.EndsWith (".zip", StringComparison.InvariantCultureIgnoreCase))
						item.StateImageIndex = -1;
					else {
						hasZip = true;
						if (zipHandling == "unzip")
							item.Checked = true;
					}
				if (zipPanel.Visible = (hasZip && (li == "u"))) {
					uploadListView.View = View.Details;
					uploadListView.CheckBoxes = true;
					uploadListView.AutoResizeColumns (ColumnHeaderAutoResizeStyle.ColumnContent);
				} else {
					uploadListView.CheckBoxes = false;
					uploadListView.View = View.Tile;
				}
				checkBusy = false;
			}
		}

		internal Nullable<bool> DoUpload (Uploader uploader, ListViewItem item, ref bool authError) {
			byte [] data = null;
			string msg, filePath = item.Name, dynFileName = null, messages = string.Empty;
			string [] pathItems;
			int pos, pos2;
			bool unzip = filePath.EndsWith (".zip", StringComparison.InvariantCultureIgnoreCase) && item.Checked, isZippedFolder = ((!filePath.Contains ("|")) && (!File.Exists (filePath)) && Directory.Exists (filePath)), zipAll = (MainForm.Settings.ZipUploadThreshold > 0);
			bool? create = null;
			DialogResult result;
			try {
				if (isZippedFolder) {
					create = true;
					if (zipAll && (zipAll = (IOUtil.GetDirectorySize (filePath, 0, MainForm.Settings.ZipUploadThreshold * 1024 * 1024) < (MainForm.Settings.ZipUploadThreshold * 1024 * 1024)))) {
						using (MemoryStream ms = new MemoryStream ())
						using (ZipForm zipForm = new ZipForm (this, item.Text, delegate (ZipFile zipFile) {
							zipFile.AddDirectory (filePath, string.Empty);
							zipFile.Save (ms);
							data = ms.ToArray ();
						}))
							if ((result = zipForm.ShowDialog (this)) != DialogResult.OK) {
								if (result == DialogResult.Abort)
									uploadWorker.CancelAsync ();
								return false;
							}
						unzip = true;
					}
				} else if (filePath.Contains ("|")) {
					pathItems = filePath.Split (new char [] { '|' }, StringSplitOptions.RemoveEmptyEntries);
					using (MemoryStream ms = new MemoryStream ())
					using (ZipForm zipForm = new ZipForm (this, item.Text, delegate (ZipFile zipFile) {
						if (pathItems.Length > 2) {
							for (int i = 1; i < (pathItems.Length - 1); i++) {
								dynFileName += (", " + pathItems [i]);
								zipFile.AddFile (Path.Combine (pathItems [0], pathItems [i]), string.Empty);
							}
							dynFileName = dynFileName.Substring (2);
						}
						zipFile.Save (ms);
						data = ms.ToArray ();
					}))
						if ((result = zipForm.ShowDialog (this)) != DialogResult.OK) {
							if (result == DialogResult.Abort)
								uploadWorker.CancelAsync ();
							return false;
						}
					unzip = item.Checked;
				} else
					using (FileStream stream = File.OpenRead (filePath)) {
						data = new byte [stream.Length];
						stream.Read (data, 0, data.Length);
					}
				Application.DoEvents ();
				if (uploadWorker.CancellationPending)
					return false;
				if (!Upload (uploader, item, ListName, FolderName, data, ((!string.IsNullOrEmpty (dynFileName)) ? (dynFileName + ".zip") : (isZippedFolder ? (unzip ? (new DirectoryInfo (filePath).Name + ".zip") : filePath) : item.Text)), unzip, create)) {
					uploadWorker.CancelAsync ();
					return false;
				}
				Application.DoEvents ();
			} catch (Exception ex) {
				WebException webEx = ex as WebException;
				HttpWebResponse resp;
				if ((webEx != null) && ((resp = webEx.Response as HttpWebResponse) != null) && ((resp.StatusCode == HttpStatusCode.Unauthorized) || (resp.StatusCode == HttpStatusCode.ProxyAuthenticationRequired) || (resp.StatusCode == HttpStatusCode.Forbidden))) {
					authError = true;
					lastCookie = null;
				}
				msg = (((pos = ex.Message.LastIndexOf (" ---> ")) >= 0) ? (ex.Message.Substring (pos + " ---> ".Length).Trim ()) : (ex.Message));
				while (((pos = msg.IndexOf ("<nativehr>", StringComparison.InvariantCultureIgnoreCase)) >= 0) && ((pos2 = msg.IndexOf ("</nativestack>", pos + 1, StringComparison.InvariantCultureIgnoreCase)) > pos))
					msg = ((pos == 0) ? string.Empty : msg.Substring (0, pos)) + msg.Substring (pos2 + "</nativestack>".Length);
				if (msg.ToLowerInvariant ().Contains ("check"))
					msg += ("\r\n\r\n" + res.HintCheckInOut);
				else if (msg.ToLowerInvariant ().Contains ("exist") && MainForm.tabControl.TabPages.Contains (MainForm.prepTabPage))
					msg += ("\r\n\r\n" + res.HintExists);
				SetStatus (item, Status.Failed, ex.Message);
				uploadPending--;
				uploadFailed++;
				RefreshStatus ();
				return null;
			}
			return true;
		}

		internal string GetFriendlyFolderName (string path) {
			DirectoryInfo parent = null;
			try {
				if ((parent = Directory.GetParent (path)) != null)
					return string.Format (res.FriendlyFolderName, new DirectoryInfo (path).Name, parent.FullName);
			} catch {
			}
			return path;
		}

		internal ListViewGroup GetListViewGroup (ListView listView, string groupName) {
			ListViewGroup listGroup;
			foreach (ListViewGroup group in listView.Groups)
				if (group.Header == groupName)
					return group;
			listGroup = new ListViewGroup (groupName, groupName);
			if ((groupName.StartsWith ("(")) || (groupName.StartsWith ("[")))
				listView.Groups.Insert (0, listGroup);
			else
				listView.Groups.Add (listGroup);
			return listGroup;
		}

		internal string GetStatus (Status status, params object [] args) {
			if ((args != null) && (args.Length > 0))
				return string.Format (res.Status, args);
			return string.Format (res.Status, res.ResourceManager.GetString ("Status_" + status));
		}

		internal bool IsIncluded (string type, string path) {
			FileAttributes atts = FileAttributes.Normal;
			bool included = true;
			string ext = Path.GetExtension (path);
			try {
				atts = File.GetAttributes (path);
			} catch {
			}
			if (File.Exists (path) && (!string.IsNullOrEmpty (ext)) && blockedExtensions.Contains (ext.ToLowerInvariant ().Trim ('.')) && (!IsIncluded (type, path, "Blocked")))
				return false;
			if (atts != FileAttributes.Normal)
				foreach (FileAttributes att in Enum.GetValues (typeof (FileAttributes)))
					if (((atts & att) == att) && (!(included &= IsIncluded (type, path, att.ToString ()))))
						return false;
			return true;
		}

		internal bool IsIncluded (string type, string path, string att) {
			PropertyInfo prop;
			if ((prop = typeof (Settings).GetProperty (type + att + (File.Exists (path) ? "Files" : "Dirs"))) != null)
				return (bool) prop.GetValue (MainForm.Settings, null);
			return true;
		}

		internal void RefreshBrowseListView () {
			List<string> selNames = new List<string> ();
			if (MainForm != null)
				try {
					browseListView.BeginUpdate ();
					foreach (ListViewItem browseItem in browseListView.SelectedItems)
						selNames.Add (Path.Combine (browseListView.Tag as string, browseItem.Text).ToLowerInvariant ());
					ChangeDirectory (browseListView.Tag as string, false);
					foreach (ListViewItem browseItem in browseListView.Items)
						if (browseItem.Selected = selNames.Contains (Path.Combine (browseListView.Tag as string, browseItem.Text).ToLowerInvariant ()))
							browseItem.EnsureVisible ();
				} finally {
					browseListView.EndUpdate ();
				}
		}

		internal void RefreshHistory () {
			browseHistoryToolStripItem_DropDownOpening (browseBackToolStripButton, EventArgs.Empty);
			browseBackToolStripButton.Enabled = (pathHistory.Count > 0);
		}

		internal void RefreshListViewColors (bool beginEndUpdate) {
			Status status;
			string folderPath;
			DirectoryInfo tmp;
			foreach (ListView lv in new ListView [] { browseListView, uploadListView })
				try {
					if (beginEndUpdate)
						lv.BeginUpdate ();
					foreach (ListViewItem item in lv.Items) {
						item.SubItems [1].ForeColor = SystemColors.GrayText;
						if (lv == browseListView)
							if (!statuses.TryGetValue (Path.Combine (folderPath = browseListView.Tag as string, item.Text).ToLowerInvariant (), out status)) {
								item.SubItems [0].BackColor = SystemColors.Window;
								do
									if (statuses.TryGetValue (folderPath.ToLowerInvariant (), out status))
										item.SubItems [0].BackColor = ColorTranslator.FromHtml ((status == Status.Failed) ? "#FF9999" : ((status == Status.Success) ? "#99FF99" : ((status == Status.Uploading) ? "#FFFF99" : "#9999FF")));
								while (((tmp = Directory.GetParent (folderPath)) != null) && (folderPath != (folderPath = tmp.FullName)));
							} else if (status == Status.Failed)
								item.SubItems [0].BackColor = ColorTranslator.FromHtml ("#FF9999");
							else if (status == Status.Success)
								item.SubItems [0].BackColor = ColorTranslator.FromHtml ("#99FF99");
							else if (status == Status.DynZip)
								item.SubItems [0].BackColor = ColorTranslator.FromHtml ("#9999FF");
							else
								item.SubItems [0].BackColor = ColorTranslator.FromHtml ("#FFFF99");
					}
				} finally {
					if (beginEndUpdate)
						lv.EndUpdate ();
				}
		}

		internal void RefreshStatus () {
			if (MainForm != null) {
				MainForm.statusInfoLabel.Text = ((isUploadBusy || ((uploadPercent > 0) && (browseListView.SelectedItems.Count == 0))) ? string.Format (res.StatusProgress, uploadPercent, uploadPending, uploadTotal, uploadCompleted, uploadFailed, uploadTotal - uploadPending) : string.Format (res.StatusInfo, browseListView.Items.Count, browseListView.SelectedItems.Count));
				if ((uploadPercent > 0) && (uploadPercent < 100)) {
					MainForm.statusInfoLabel.BackgroundImage = infoBackgroundImage;
					MainForm.statusInfoLabel.ForeColor = SystemColors.InfoText;
				} else {
					MainForm.statusInfoLabel.BackgroundImage = null;
					MainForm.statusInfoLabel.ForeColor = SystemColors.GrayText;
				}
			}
		}

		internal void RefreshUploadTitle () {
			bool afterSuccess = (statuses.Count > 0);
			string baseTitle = uploadGroupBox.Text.Substring (0, uploadGroupBox.Text.LastIndexOf ('[')), newContent;
			if (afterSuccess)
				foreach (KeyValuePair<string, Status> kvp in statuses)
					if (!(afterSuccess = (kvp.Value == Status.Success)))
						break;
			toolStripUploadButton.Enabled = (uploadListView.Items.Count > 0);
			uploadGroupBox.Text = baseTitle + '[' + ((uploadListView.SelectedItems.Count == 0) ? (uploadListView.Items.Count + "") : (uploadListView.SelectedItems.Count + "/" + uploadListView.Items.Count)) + ']';
			if ((uploadWebBrowser.Visible = !(uploadListView.Visible = (hideTutorial || (uploadListView.Items.Count > 0)))) && (string.IsNullOrEmpty (uploadWebBrowser.DocumentText) || (afterSuccess != refreshUploadTitleLastAfterSuccess)) && (!string.IsNullOrEmpty (Url))) {
				newContent = res.welcome.Replace ("%%BASE_URL%%", new Uri (Url).GetLeftPart (UriPartial.Authority).TrimEnd ('/')).Replace ("%%DISPLAY_NOTE%%", hideNote ? "none" : "block").Replace ("%%DISPLAY_WELCOME%%", afterSuccess ? "none" : "block").Replace ("%%DISPLAY_SUCCESS%%", afterSuccess ? "block" : "none");
				if (uploadWebBrowser.Document == null)
					uploadWebBrowser.DocumentText = newContent;
				else
					uploadWebBrowser.Document.OpenNew (true).Write (newContent);
				refreshUploadTitleLastAfterSuccess = afterSuccess;
			}
		}

		internal void SaveOrForgetUserData (bool save) {
			if (isUserLoginStored = save) {
				LogonDomain = logonDomain;
				LogonUser = logonUser;
				LogonPass = logonPass;
			} else {
				RegistryUtil.SetString ("ld", string.Empty);
				RegistryUtil.SetString ("lu", string.Empty);
				RegistryUtil.SetString ("lp", string.Empty, string.Empty);
			}
		}

		internal void SetFilterByTemplate (string template) {
			ToolStripMenuItem filterItem = browseFiltersNoneToolStripItem;
			if (((template == "101") && MainForm.Settings.SuggestFiltersDocLibs) || ((template != "101") && MainForm.Settings.SuggestFiltersOtherLibs)) {
				preFilters.TryGetValue (template, out filterItem);
				toolStripFilterItem_Click (filterItem, EventArgs.Empty);
				//MainForm.tabControl.SelectedIndex = 0;
				//if (browseFiltersToolStripButton.IsOnOverflow)
				//    browseToolStrip.OverflowButton.ShowDropDown ();
				//browseFiltersToolStripButton.ShowDropDown ();
			}
		}

		internal ListViewItem SetStatus (ListViewItem item, Status status, params object [] args) {
			Status s;
			item.Tag = status;
			item.SubItems [1].Text = GetStatus (status, args);
			item.ImageKey = status.ToString ();
			if ((!statuses.TryGetValue (item.Name.ToLowerInvariant (), out s)) || (((s != Status.Failed) || (status == Status.Success)) && ((s != Status.Success) || (status == Status.Failed))))
				statuses [item.Name.ToLowerInvariant ()] = status;
			RefreshListViewColors (true);
			Application.DoEvents ();
			return item;
		}

		internal void ShowMessages (string title, string messages) {
			MessageControl ctl = new MessageControl ();
			TabPage tabPage = new TabPage (title);
			ctl.toolStripButton.Click += delegate (object sender, EventArgs e) {
				MainForm.tabControl.TabPages.Remove (tabPage);
			};
			ctl.textBox.Text = messages;
			ctl.textBox.SelectionStart = ctl.textBox.SelectionLength = 0;
			tabPage.Controls.Add (ctl);
			ctl.Dock = DockStyle.Fill;
			MainForm.tabControl.TabPages.Insert (1, tabPage);
			MainForm.tabControl.SelectedTab = tabPage;
		}

		internal void UpdateUserItem () {
			toolStripUserItem.Text = (((!isFormsAuth) && (!isUserLoginStored) && string.IsNullOrEmpty (LogonUser)) ? ((string.IsNullOrEmpty (Environment.UserDomainName) ? Environment.UserName : (Environment.UserDomainName + '\\' + Environment.UserName))) : (string.IsNullOrEmpty (logonDomain) ? logonUser : (logonDomain + '\\' + logonUser)));
			toolStripUserSeparator.Visible = toolStripUserItem.Visible = !string.IsNullOrEmpty (toolStripUserItem.Text);
			toolStripUserItem.Enabled = isFormsAuth || isUserLoginStored;
		}

		internal bool Upload (Uploader uploader, ListViewItem item, string listName, string folderName, byte [] data, string fileName) {
			return Upload (uploader, item, listName, folderName, data, fileName, false, null);
		}

		internal bool Upload (Uploader uploader, ListViewItem item, string listName, string folderName, byte [] data, string fileName, bool unzip, bool? createFolder) {
			Exception ex = null;
			IEnumerable<string> allFiles;
			List<string> allFilePaths;
			int completedSubFiles = 0, pos1, pos2;
			string targetPath = (listName.Trim ('/') + "/" + folderName.Trim ('/')).Trim ('/'), messages = string.Empty;
			bool completed = false, cancelled = false;
			object state = new object (), subState = new object ();
			roxority_UploadZen.UploadDocumentCompletedEventHandler onUpload = null, onSubUpload = null;
			onUpload = delegate (object sender, roxority_UploadZen.UploadDocumentCompletedEventArgs e) {
				uploader.Remove (onUpload);
				if ((ex = e.Error) != null)
					if (item != null)
						SetStatus (item, Status.Failed, ex.ToString ());
					else
						MessageBox.Show (this, ex.ToString (), ex.GetType ().FullName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				completed = !(cancelled = e.Cancelled);
				if ((item != null) && (ex == null) && !string.IsNullOrEmpty (messages = ((e.Result.IndexOf (" ---> ") < 0) ? e.Result : e.Result.Substring (e.Result.LastIndexOf (" ---> ") + " ---> ".Length)))) {
					while (((pos1 = messages.IndexOf ("<nativehr>", StringComparison.InvariantCultureIgnoreCase)) >= 0) && ((pos2 = messages.IndexOf ("</nativestack>", pos1 + 1, StringComparison.InvariantCultureIgnoreCase)) > pos1))
						messages = ((pos1 == 0) ? string.Empty : messages.Substring (0, pos1)) + messages.Substring (pos2 + "</nativestack>".Length);
					if ((messages.Length > 1000) || messages.IndexOf ('\n') != messages.LastIndexOf ('\n'))
						ShowMessages (Directory.Exists (item.Name) ? string.Format (res.FriendlyFolderName, Path.GetFileName (item.Name), Path.GetDirectoryName (item.Name)) : fileName, messages);
					else
						MessageBox.Show (this, messages, Directory.Exists (item.Name) ? string.Format (res.FriendlyFolderName, Path.GetFileName (item.Name), Path.GetDirectoryName (item.Name)) : fileName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			};
			onSubUpload = delegate (object sender, roxority_UploadZen.UploadDocumentCompletedEventArgs e) {
				uploader.Remove (onSubUpload);
				if ((ex = e.Error) != null)
					messages += (((ex.Message.IndexOf (" ---> ") < 0) ? ex.Message : ex.Message.Substring (ex.Message.LastIndexOf (" ---> ") + " ---> ".Length)) + "\r\n");
				if ((completed = !(cancelled = e.Cancelled)) && (ex == null))
					completedSubFiles++;
				if ((ex == null) && !string.IsNullOrEmpty (e.Result))
					messages += (((e.Result.IndexOf (" ---> ") < 0) ? e.Result : e.Result.Substring (e.Result.LastIndexOf (" ---> ") + " ---> ".Length)) + "\r\n");
			};
			if (!createFolder.HasValue)
				createFolder = zipCheckBox.Checked;
			if (File.Exists (item.Name) && (fileName.EndsWith (".zip", StringComparison.InvariantCultureIgnoreCase)))
				if ((!unzip) && (!(unzip = (zipHandling == "unzip"))) && (zipHandling == "prompt"))
					foreach (ListViewItem theItem in uploadListView.Items)
						if (theItem.Text.Equals (fileName, StringComparison.InvariantCultureIgnoreCase)) {
							unzip = theItem.Checked;
							break;
						}
			if (data != null) {
				uploader.Add (onUpload);
				uploader.UploadDocumentAsync (fileName, data, (unzip && createFolder.Value) ? (targetPath.Trim ('/') + '/' + fileName.Substring (0, fileName.Length - ".zip".Length)) : targetPath, unzip, MainForm.Settings.Overwrite, autoCheckIn, toolStripCheckOutItem.Checked, MainForm.Settings.Replace, null, !hideHelp, state);
				while (!(completed || cancelled || (ex != null))) {
					Application.DoEvents ();
					if (uploadWorker.CancellationPending) {
						uploader.Remove (onUpload);
						uploader.CancelAsync (state);
						uploader.Abort ();
						cancelled = true;
					}
				}
				if (ex != null)
					throw ex;
				return completed;
			} else if (((allFiles = IOUtil.GetAllFiles (fileName, delegate (string fp) {
				bool inc = false;
				try {
					using (Stream fs = File.OpenRead (fp))
						if ((fs.Length == 0) && !MainForm.Settings.ZipEmptyFiles)
							return false;
					inc = IsIncluded ("Zip", fp);
				} catch {
					if (!MainForm.Settings.ZipErrorFiles)
						return false;
				}
				return inc;
			}, delegate (string dp) {
				bool inc = false;
				try {
					if ((Directory.GetDirectories (dp).Length == 0) && (Directory.GetFiles (dp).Length == 0) && !MainForm.Settings.ZipEmptyDirs)
						return false;
					inc = IsIncluded ("Zip", dp);
				} catch {
					if (!MainForm.Settings.ZipErrorDirs)
						return false;
				}
				return inc;
			})) != null) && ((allFilePaths = new List<string> (allFiles)).Count > 0)) {
				foreach (string sourcePath in allFilePaths) {
					if ((uploadWorker.CancellationPending) || cancelled) {
						if (!uploadWorker.CancellationPending)
							uploadWorker.CancelAsync ();
						cancelled = true;
						break;
					}
					data = null;
					try {
						using (FileStream fs = File.OpenRead (sourcePath))
						using (MemoryStream ms = new MemoryStream ()) {
							SetStatus (item, Status.Uploading, string.Format ("{0}% — {1} {2}", SharedUtil.Percent (allFilePaths.IndexOf (sourcePath) + 1, allFilePaths.Count), (unzip = ((fs.Length >= 1024 * 1024) && (((ulong) fs.Length) < (Settings.ComputerInfo.TotalPhysicalMemory / 4)) && (Array.IndexOf<string> (nonZipExts, Path.GetExtension (sourcePath).ToLowerInvariant ().Trim ('.')) < 0))) ? res.MiniStatus_Zip : res.MiniStatus_Load, Path.GetFileName (sourcePath)));
							if (!unzip)
								cancelled = !IOUtil.CopyStream (fs, ms, delegate (int pos) {
									Application.DoEvents ();
									return cancelled || uploadWorker.CancellationPending;
								});
							else
								cancelled = !SharedUtil.RunThread (delegate () {
									using (ZipFile zipFile = ZipForm.CreateZip ()) {
										zipFile.AddFileStream (Path.GetFileName (sourcePath), string.Empty, fs);
										zipFile.Save (ms);
									}
								}, delegate () {
									Application.DoEvents ();
									return cancelled || uploadWorker.CancellationPending;
								});
							data = ms.ToArray ();
						}
					} catch (Exception exception) {
						if (MainForm.Settings.ZipErrorFiles && (MessageBox.Show (this, exception.Message, string.Format (res.FriendlyFolderName, sourcePath.Replace (fileName.TrimEnd ('\\'), "."), fileName.TrimEnd ('\\')), MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)) {
							cancelled = true;
							uploadWorker.CancelAsync ();
							break;
						}
					}
					if ((uploadWorker.CancellationPending) || cancelled) {
						if (!uploadWorker.CancellationPending)
							uploadWorker.CancelAsync ();
						cancelled = true;
						break;
					}
					if (data != null) {
						uploader.Add (onSubUpload);
						completed = false;
						ex = null;
						SetStatus (item, Status.Uploading, string.Format ("{0}% — {1} {2}", SharedUtil.Percent (allFilePaths.IndexOf (sourcePath) + 1, allFilePaths.Count), res.MiniStatus_Up, Path.GetFileName (sourcePath)));
						uploader.UploadDocumentAsync (Path.GetFileName (sourcePath), data, (targetPath.Trim ('/') + '/' + Path.GetDirectoryName (sourcePath).Substring (new DirectoryInfo (fileName).Parent.FullName.Length).Replace ('\\', '/').Trim ('/')).Trim ('/'), unzip, MainForm.Settings.Overwrite, autoCheckIn, toolStripCheckOutItem.Checked, MainForm.Settings.Replace, null, !hideHelp, subState);
						while (!(completed || cancelled || (ex != null))) {
							Application.DoEvents ();
							if (uploadWorker.CancellationPending) {
								uploader.Remove (onSubUpload);
								uploader.CancelAsync (subState);
								uploader.Abort ();
								cancelled = true;
							}
						}
					}
				}
				if (!string.IsNullOrEmpty (messages))
					ShowMessages (string.Format (res.FriendlyFolderName, Path.GetFileName (fileName), Path.GetDirectoryName (fileName)), messages);
				return !cancelled;
			} else
				return true;
		}

		protected override void OnLoad (EventArgs e) {
			List<string> history = History;
			base.OnLoad (e);
			try {	
				using (ShellInfo shell = new ShellInfo ("temp.zip")) {
					if (!(iconList.Images.ContainsKey (shell.DotLessExtension)))
						iconList.Images.Add (shell.DotLessExtension, shell.SmallImage);
					if (!(fileIconList.Images.ContainsKey (shell.DotLessExtension)))
						fileIconList.Images.Add (shell.DotLessExtension, shell.Image);
				}
			} catch {
			}
			if (iconList.Images.ContainsKey ("zip"))
				browseFiltersArchivesToolStripItem.Image = contextAddZipItem.Image = toolStripAddZipItem.Image = iconList.Images ["zip"];
			UpdateUserItem ();
			ChangeDirectory (RegistryUtil.GetString ("lastdir", Environment.GetFolderPath (Environment.SpecialFolder.MyDocuments)), true);
			browseListView_SelectedIndexChanged (browseListView, EventArgs.Empty);
			if (toolStripUploadRecentItem.Enabled = (history.Count > 0))
				foreach (string path in history)
					toolStripUploadRecentItem.DropDownItems.Add (path,null,toolStripUploadOpenItem_Click);
		}

		protected internal virtual void OnPropertyChanged (EventArgs e) {
			try {
				toolStripCheckinItem.Enabled = toolStripCheckOutItem.Enabled = toolStripAddFolderItem.Enabled = ((li == "b") || (li == "u"));
				toolStripUploadRecentItem.Enabled = toolStripUploadSaveItem.Enabled = toolStripUploadOpenItem.Enabled = ((li == "b") || (li == "u"));
				toolStripAddFolderItem.Text = string.Format (res.AddFolder, new DirectoryInfo (browseListView.Tag as string).Name, (ListName + '/' + FolderName).Trim ('/'), SharedUtil.ReplaceCharacters (new DirectoryInfo (browseListView.Tag as string).Name, Settings.SPInvalidFileNameCharacters, MainForm.Settings.Replace));
			} catch {
			}
			if (PropertyChanged != null)
				PropertyChanged (this, e);
		}

		protected internal virtual void OnUploadBusy (bool busy) {
			if ((toolStripUploadOpenItem.Enabled = toolStripUploadSaveItem.Enabled = zipCheckBox.Enabled = !(isUploadBusy = busy)) && (UploadNonBusy != null))
				UploadNonBusy (this, EventArgs.Empty);
			else if (busy && (UploadBusy != null))
				UploadBusy (this, EventArgs.Empty);
			browseListView_SelectedIndexChanged (browseListView, EventArgs.Empty);
			uploadListView_SelectedIndexChanged (uploadListView, EventArgs.Empty);
		}

		[DefaultValue ("*"), DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public string Filter {
			get {
				return filter;
			}
			set {
				bool exists = false;
				ToolStripMenuItem newItem;
				RegistryUtil.SetString ("filter", filter = (string.IsNullOrEmpty (value) ? config.DefaultFilter : value));
				browseFiltersToolStripButton.DropDown.Close ();
				browseToolStrip.OverflowButton.DropDown.Close ();
				foreach (ToolStripItem subItem in browseFiltersToolStripButton.DropDownItems)
					if (subItem is ToolStripMenuItem)
						if (((ToolStripMenuItem) subItem).Checked = filter.Equals (subItem.Tag + "")) {
							exists = true;
							toolStripFilterItem_Click (subItem, null);
						}
				if (!exists) {
					browseFiltersToolStripButton.DropDownItems.Insert (0, newItem = new ToolStripMenuItem (string.Format (res.OtherFilter, filter.Replace (";", "; ")), res.Filter2HS, toolStripFilterItem_Click));
					newItem.Tag = filter;
					toolStripFilterItem_Click (newItem, null);
				}
				RefreshBrowseListView ();
			}
		}

		[DefaultValue (""), DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public string FolderName {
			get {
				return folderName;
			}
			set {
				folderName = value;
				OnPropertyChanged (EventArgs.Empty);
			}
		}

		[DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public List<string> History {
			get {
				List<string> list = new List<string> (RegistryUtil.GetStrings ("History", new string [0]));
				list.RemoveAll (delegate (string path) {
					return !File.Exists (path);
				});
				return list;
			}
			set {
				if (value == null)
					value = new List<string> ();
				SharedUtil.RemoveDuplicates<string> (value);
				RegistryUtil.SetStrings ("History", value.ToArray ());
			}
		}

		[DefaultValue (""), DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public string LogonDomain {
			get {
				return logonDomain;
			}
			set {
				RegistryUtil.SetString ("ld", logonDomain = value);
				UpdateUserItem ();
			}
		}

		[DefaultValue (""), DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden), PasswordPropertyText (true)]
		public string LogonPass {
			get {
				return logonPass;
			}
			set {
				RegistryUtil.SetString ("lp", logonPass = value, string.IsNullOrEmpty (logonDomain) ? logonUser : logonDomain + '\\' + logonUser);
			}
		}

		[DefaultValue (""), DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public string LogonUser {
			get {
				return logonUser;
			}
			set {
				RegistryUtil.SetString ("lu", logonUser = value);
				UpdateUserItem ();
			}
		}

		public MainForm MainForm {
			get {
				return ParentForm as MainForm;
			}
		}

		[DefaultValue (true), DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public bool ShowFolders {
			get {
				return browseFolders;
			}
			set {
				RegistryUtil.SetBoolean ("ShowFolders", browseFolders = value);
				RefreshBrowseListView ();
			}
		}

		[DefaultValue ("Shared Documents"), DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public string ListName {
			get {
				return listName;
			}
			set {
				RegistryUtil.SetString ("listname", listName = value);
				OnPropertyChanged (EventArgs.Empty);
			}
		}

		[DefaultValue ("http://roxority/_layouts/roxority_UploadZen/Files.asmx"), DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
		public string Url {
			get {
				return url;
			}
			set {
				RegistryUtil.SetString ("url", url = value);
				OnPropertyChanged (EventArgs.Empty);
				RefreshUploadTitle ();
			}
		}

	}

}
