
using Microsoft.SharePoint;
using roxority.SharePoint;
using roxority_ExportZen;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace roxority_ExportZen_Scheduler {

	public static class Program {

		[STAThread]
		public static void Main (string [] args) {
			/* "http://roxbook/Lists/Tasks" "idididid" "c:\temp\test export.csv" */
			string listUrl, exportItemID, exportFilePath, schemaFilePath = Path.Combine (Application.StartupPath, "schemas.json"), tmpEnc, sep = ",", acsep;
			bool unix = false;
			int tmp;
			SPList exportDataList;
			SPView exportDataView = null;
			ICollection<IDictionary> actions = null;
			Encoding enc = Encoding.Default;
			System.Diagnostics.Debugger.Break ();
			if ((args.Length < 3) || string.IsNullOrEmpty (listUrl = args [0].Trim ()) || string.IsNullOrEmpty (exportItemID = args [1].Trim ()) || string.IsNullOrEmpty (exportFilePath = args [2].Trim ()))
				Console.WriteLine ("Usage: ExportZen.exe \"List URL\" \"Export Action Name or ID\" \"Full .csv export file path\"");
			else {
				if (listUrl.Contains ("%20")) {
					listUrl = listUrl.Replace ("%20", " ");
					Console.WriteLine ("Warning: 'List URL' argument contained '%20'.\r\nI replaced this with a single white-space but DO only specify un-escaped URLs.");
				}
				using (SPSite site = new SPSite (listUrl))
				using (SPWeb web = site.OpenWeb ()) {
					exportDataList = ProductPage.GetList (web, listUrl);
					if (listUrl.ToLowerInvariant ().Contains (".aspx"))
						foreach (SPView view in exportDataList.Views)
							if (listUrl.EndsWith (view.Url, StringComparison.InvariantCultureIgnoreCase)) {
								exportDataView = view;
								break;
							}
					using (ProductPage prodPage = new ProductPage ()) {
						ProductPage.currentSite = site;
						if (!File.Exists (schemaFilePath))
							schemaFilePath = Path.Combine (ProductPage.HivePath, @"TEMPLATE\LAYOUTS\roxority_ExportZen\schemas.json");
						if ((actions = ExportZenPage.GetActions (schemaFilePath)) != null)
							foreach (IDictionary inst in actions)
								if (exportItemID.Equals (inst ["id"] + string.Empty, StringComparison.InvariantCultureIgnoreCase) || exportItemID.Equals (inst ["name"] + string.Empty, StringComparison.InvariantCultureIgnoreCase))
									try {
										if (!string.IsNullOrEmpty (acsep = inst ["sep"] + string.Empty)) {
											if (acsep == "t")
												sep = "\t";
											else if (acsep == "s")
												sep = ";";
										} else if ((inst ["excel"] is bool) && (bool) inst ["excel"])
											sep = ";";
										if (inst ["unix"] is bool)
											unix = (bool) inst ["unix"];
										if (int.TryParse ((tmpEnc = inst ["enc"] + string.Empty), out tmp))
											enc = Encoding.GetEncoding (tmp);
										else
											enc = Encoding.GetEncoding (tmpEnc);
									} catch {
									}
						using (FileStream fs = new FileStream (exportFilePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
						using (StreamWriter sw = new StreamWriter (fs, enc)) {
							ExportZenPage.Export (sw, actions, web.Url.TrimEnd ('/'), ProductPage.GuidLower (exportDataList.ID), exportItemID, (exportDataView == null) ? null : ProductPage.GuidLower (exportDataView.ID), sep, unix ? "1" : "0", null, 4, string.Empty);
							sw.Flush ();
							sw.Close ();
						}
					}
				}
			}
		}

	}

}
