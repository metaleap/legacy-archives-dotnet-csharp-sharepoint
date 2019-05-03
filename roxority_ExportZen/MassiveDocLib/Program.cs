
using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MassiveDocLib {

	public class Program {

		public static void Main (string [] args) {
			bool hasUpdate = false;
			string [] files = Directory.GetFiles ("E:\\Dropbox\\roxority ltd", "*.*", SearchOption.TopDirectoryOnly);
			string srcFilePath;
			Random rnd = new Random ();
			SPDocumentLibrary lib;
			SPField field;
			SPFile file;
			SPListItem item;
			using (SPSite site = new SPSite ("http://roxwin7"))
			using (SPWeb web = site.OpenWeb ()) {
				lib = web.Lists ["ManyManyDocs"] as SPDocumentLibrary;
				//	FIELDS
				for (int i = 1; i <= 56; i++) {
					field = null;
					try {
						field = lib.Fields ["Field" + i];
					} catch {
					}
					if (field == null) {
						lib.Fields.Add ("Field" + i, SPFieldType.Text, false, false, null);
						hasUpdate = true;
					}
				}
				if (hasUpdate)
					lib.Update ();
				//	DOCS
				if (lib.Items.Count == 0) {
					hasUpdate = true;
					for (int i = 0; i <= 850; i++)
						using (FileStream fs = File.Open (srcFilePath = files [rnd.Next (0, files.Length)], FileMode.Open, FileAccess.Read, FileShare.ReadWrite)) {
							lib.RootFolder.Files.Add ("f__" + i + "__" + Path.GetFileName (srcFilePath), fs, true);
							Console.Title = srcFilePath;
						}
				}
				if (hasUpdate)
					lib.Update ();
				//	FIELD VALS
				for (int v = 0; v < 35; v++)
					for (int i = 0; i <= 850; i++) {
						file = lib.RootFolder.Files [i];
						Console.Title = file.Name;
						item = file.Item;
						for (int j = 1; j <= 56; j++)
							item ["Field" + j] = ((rnd.Next (0, 2) == 1) ? "Rand Val " : string.Empty) + rnd.Next ();
						item.Update ();
					}
			}
		}

	}

}
