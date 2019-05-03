
using roxority.Shared;
using roxority.SharePoint;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace LegacyMigration {

	using res = Properties.Resources;

	public partial class MainForm : Form {

		public enum FieldNames {

			Title,
			Description,
			ExportZenDefinition,
			ExportZenListName,
			ExportZenListViews,
			ExportZenColumns,
			ExportZenVersions,
			ExportZenVersionsRow,
			ExportZenExportView,
			ExportZenExportFilters,
			ExportZenExcelStyle,
			ExportZenUnixStyle,
			ExportZenLocale,
			ExportZenEncoding,
			ExportZenPrependBom

		}

		public static readonly Guid FeatureID = new Guid ("65a0b7a9-080a-4bf7-8fed-5dc9de3654a1");
		public static readonly Hashtable Webs;

		internal Hashtable fullExport = null;
		internal List<SPList> allLists = new List<SPList> ();
		internal int listCount = -1;

		static MainForm () {
			Webs = new Hashtable ();
			Webs ["n"] = "0";
		}

		public MainForm () {
			InitializeComponent ();
			comboBox.SelectedIndex = 0;
		}

		private void backgroundWorker_DoWork (object sender, DoWorkEventArgs e) {
			if (treeView.InvokeRequired)
				treeView.Invoke (new DoWorkEventHandler (backgroundWorker_DoWork), sender, e);
			else {
				bool elevate = (comboBox.SelectedIndex > 0);
				int nodeCount, subNodeCount, nodeSubCount;
				Hashtable ht_main = new Hashtable (), ht_fjs = new Hashtable (), ht_exp = new Hashtable ();
				TreeNode wsNode, waNode, siteNode, webNode, listNode, itemNode, curNode = null;
				Dictionary<FieldNames, SPField> fields = new Dictionary<FieldNames, SPField> ();
				Action<TreeNode> addNode = delegate (TreeNode node) {
					treeView.ExpandAll ();
					treeView.Update ();
					Application.DoEvents ();
				};
				Action<Exception> onError = delegate (Exception ex) {
					TreeNode node;
					if (curNode != null) {
						addNode (node = curNode.Nodes.Add (ex.Message));
						node.BackColor = Color.Gold;
						node.ToolTipText = res.Error + "\r\n\r\n" + ex.ToString ();
						node.Tag = ex.ToString ();
					}
				};
				listCount = 0;
				fullExport = null;
				allLists.Clear ();
				ht_main ["fjs"] = ht_fjs;
				ht_main ["sjs"] = ht_fjs;
				ht_fjs ["schemas.json:ExportActions"] = ht_exp;
				treeView.Nodes.Clear ();
				foreach (SPWebService ws in new SPWebService [] { SPWebService.AdministrationService, SPWebService.ContentService }) {
					addNode (curNode = wsNode = treeView.Nodes.Add (ws.TypeName));
					wsNode.ToolTipText = res.WebSvc;
					wsNode.ForeColor = SystemColors.GrayText;
					foreach (SPWebApplication wa in ProductPage.TryEach<SPWebApplication> (ws.WebApplications, false, onError, elevate)) {
						addNode (curNode = waNode = wsNode.Nodes.Add (wa.AlternateUrls.DisplayName));
						waNode.NodeFont = new Font (treeView.Font, FontStyle.Italic);
						waNode.ToolTipText = res.WebApp;
						foreach (SPSite site in ProductPage.TryEach<SPSite> (wa.Sites, false, onError, elevate)) {
							addNode (curNode = siteNode = waNode.Nodes.Add (site.Url));
							siteNode.ToolTipText = res.Site;
							foreach (SPWeb web in ProductPage.TryEach<SPWeb> (site.AllWebs, false, onError, elevate)) {
								addNode (curNode = webNode = siteNode.Nodes.Add (web.Url));
								webNode.NodeFont = new Font (treeView.Font, FontStyle.Underline);
								nodeCount = 0;
								nodeSubCount = 0;
								foreach (SPList list in ProductPage.TryEach<SPList> (web.Lists, false, onError, elevate))
									if (list.TemplateFeatureId == FeatureID) {
										listCount++;
										allLists.Add (list);
										fields.Clear ();
										foreach (FieldNames fn in Enum.GetValues (typeof (FieldNames)))
											fields [fn] = ProductPage.GetField (list, fn.ToString ());
										nodeCount++;
										addNode (curNode = listNode = webNode.Nodes.Add (list.Title));
											subNodeCount = 0;
										foreach (SPListItem item in ProductPage.TryEach<SPListItem> (list.Items, false, onError, elevate))
											try {
												addNode (curNode = itemNode = listNode.Nodes.Add ("#" + item.ID + ": " + item.Title));
												ht_exp [ProductPage.GuidLower (item.UniqueId, false)] = Convert (item, fields);
												subNodeCount++;
												nodeSubCount++;
											} catch (Exception ex) {
												onError (ex);
											} finally {
												curNode = listNode;
											}
										listNode.Text += (" (" + subNodeCount + ")");
										if (subNodeCount > 0) {
											listNode.ToolTipText = res.ListItems;
											listNode.NodeFont = new Font (treeView.Font, FontStyle.Bold);
										} else
											listNode.ToolTipText = res.ListNoItems;
									}
								if (nodeCount == 0)
									webNode.ToolTipText = res.WebNoLists;
								else if (nodeSubCount == 0)
									webNode.ToolTipText = res.WebNoItems;
								else
									webNode.ToolTipText = res.WebItems;
								if (nodeSubCount == 0)
									webNode.ForeColor = SystemColors.GrayText;
								else
									webNode.BackColor = Color.FromArgb (187, 255, 187);
								webNode.Text += (" (" + nodeCount + ")");
							}
						}
					}
				}
				curNode= treeView.Nodes.Add (res.Legend);
				(wsNode = curNode.Nodes.Add (res.WebSvc)).ForeColor = SystemColors.GrayText;
				(waNode = wsNode.Nodes.Add (res.WebApp)).NodeFont = new Font (treeView.Font, FontStyle.Italic);
				siteNode = waNode.Nodes.Add (res.Site);
				(webNode = siteNode.Nodes.Add (res.WebNoLists)).NodeFont = new Font (treeView.Font, FontStyle.Underline);
				webNode.ForeColor = SystemColors.GrayText;
				(webNode = siteNode.Nodes.Add (res.WebNoItems)).NodeFont = new Font (treeView.Font, FontStyle.Underline);
				listNode = webNode.Nodes.Add (res.ListNoItems);
				(webNode = siteNode.Nodes.Add (res.WebItems)).NodeFont = new Font (treeView.Font, FontStyle.Underline);
				webNode.BackColor = Color.FromArgb (187, 255, 187);
				(listNode = webNode.Nodes.Add (res.ListItems)).NodeFont = new Font (treeView.Font, FontStyle.Bold);
				listNode.Nodes.Add (res.Item);
				textBox.Text = JSON.JsonEncode (ht_exp);
				fullExport = ht_main;
				if (listCount == 0) {
					MessageBox.Show (this, res.NoLists, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
					textBox.Text = res.NoLists;
				}
			}
		}

		private void backgroundWorker_RunWorkerCompleted (object sender, RunWorkerCompletedEventArgs e) {
			toolStripScanButton.Enabled = comboBox.Enabled = true;
			toolStripSaveButton.Enabled = ((fullExport != null) && (listCount > 0));
			UseWaitCursor = false;
		}

		private void toolStripScanButton_Click (object sender, EventArgs e) {
			textBox.Text = string.Empty;
			toolStripScanButton.Enabled = toolStripSaveButton.Enabled = comboBox.Enabled = false;
			UseWaitCursor = true;
			backgroundWorker.RunWorkerAsync ();
		}

		private void toolStripSaveButton_Click (object sender, EventArgs e) {
			string json = string.Empty, msg = string.Empty;
			bool saved = false, elevate = (comboBox.SelectedIndex > 0);
			List<string> deleted = new List<string> ();
			Dictionary<SPList, Exception> failed = new Dictionary<SPList, Exception> ();
			try {
				json = JSON.JsonEncode (fullExport);
			} catch (Exception ex) {
				MessageBox.Show (this, res.Step1Failed + "\r\n\r\n" + ex, ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			if (!string.IsNullOrEmpty (json))
				if (saveFileDialog.ShowDialog (this) == System.Windows.Forms.DialogResult.OK)
					try {
						using (StreamWriter sw = File.CreateText (saveFileDialog.FileName)) {
							sw.Write (json);
							sw.Flush ();
							sw.Close ();
						}
						saved = true;
					} catch (Exception ex) {
						MessageBox.Show (this, string.Format (res.Step2Failed, ex), ex.Message, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					}
			if (saved && (allLists.Count > 0)) {
				msg = string.Format (res.Saved, saveFileDialog.FileName) + "\r\n\r\n";
				foreach (SPList list in allLists)
					try {
#if !DEBUG
						if (elevate)
							SPSecurity.RunWithElevatedPrivileges (delegate () {
								list.Delete ();
							});
						else
							list.Delete ();
#endif
						deleted.Add (list.ParentWeb.Url + " - " + list.Title);
					} catch (Exception ex) {
						failed [list] = ex;
					}
				if (deleted.Count > 0) {
					msg += res.Deleted + "\r\n";
					foreach (string list in deleted)
						msg += ("\t" + list + "\r\n");
					msg += "\r\n";
				}
				if (failed.Count > 0) {
					msg += res.Failed + "\r\n";
					foreach (KeyValuePair<SPList, Exception> kvp in failed)
						msg += ("\t" + kvp.Key.ParentWeb.Url + " - " + kvp.Key.Title + ":\r\n\t\t" + kvp.Value.Message + "\r\n");
					msg += "\r\n" + res.Failed2 + "\r\n";
				}
				textBox.Text = msg;
				MessageBox.Show (this, msg, Text, MessageBoxButtons.OK, (failed.Count > 0) ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
			}
		}

		private void treeView_NodeMouseDoubleClick (object sender, TreeNodeMouseClickEventArgs e) {
			if (e.Node.Tag != null)
				MessageBox.Show (this, e.Node.Tag + string.Empty, e.Node.Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		internal Hashtable Convert (SPListItem exportRule, Dictionary<FieldNames, SPField> fields) {
			string tmp, tmp2;
			ArrayList show = new ArrayList ();
			Hashtable ht = new Hashtable (), showLists = new Hashtable (), showViews = new Hashtable ();
			Converter<FieldNames, object> get = delegate (FieldNames fn) {
				try {
					return exportRule [fields [fn].Id];
				} catch {
					return null;
				}
			};
			Converter<FieldNames, string> gets = delegate (FieldNames fn) {
				object obj = get (fn);
				return ((obj == null) ? null : (obj + string.Empty));
			};
			show.Add (showLists);
			show.Add (showViews);
			ht ["id"] = ProductPage.GuidLower (exportRule.UniqueId, false);
			ht ["name"] =res.Migrated + " " + exportRule.Title;
			ht ["show"] = "";
			ht ["title"] = exportRule.Title;
			ht ["desc"] = gets (FieldNames.Description);
			ht ["cols"] = gets (FieldNames.ExportZenColumns);
			ht ["vhcol"] = gets (FieldNames.ExportZenVersions);
			ht ["vhrows"] = get (FieldNames.ExportZenVersionsRow);
			ht ["view"] = get (FieldNames.ExportZenExportView);
			ht ["filter"] = get (FieldNames.ExportZenExportFilters);
			ht ["unix"] = get (FieldNames.ExportZenUnixStyle);
			ht ["loc"] = gets (FieldNames.ExportZenLocale);
			if (!string.IsNullOrEmpty (tmp = gets (FieldNames.ExportZenEncoding)))
				try {
					ht ["enc"] = Encoding.GetEncoding (tmp).CodePage.ToString ();
				} catch {
				}
			if (!string.IsNullOrEmpty (tmp = gets (FieldNames.ExportZenListViews))) {
				showViews ["n"] = "1";
				showViews ["i"] = new ArrayList (tmp.Split (new string [] { ";#" }, StringSplitOptions.RemoveEmptyEntries));
			} else
				showViews ["n"] = "0";
			if ((!string.IsNullOrEmpty (tmp = gets (FieldNames.ExportZenDefinition))) | !string.IsNullOrEmpty (tmp2 = gets (FieldNames.ExportZenListName))) {
				showLists ["n"] = "1";
				if (!string.IsNullOrEmpty (tmp2))
					showLists ["i"] = new ArrayList (tmp2.Split (new string [] { ";#" }, StringSplitOptions.RemoveEmptyEntries));
				if (!string.IsNullOrEmpty (tmp))
					showLists ["bt"] = new ArrayList (new string [] { tmp });
			} else
				showLists ["n"] = "0";
			ht ["bom"] = get (FieldNames.ExportZenPrependBom);
			ht ["webs"] = Webs;
			ht ["show"] = show;
			return ht;
		}

	}

}
