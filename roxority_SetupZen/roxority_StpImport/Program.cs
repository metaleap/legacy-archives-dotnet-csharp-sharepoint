
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using roxority.Shared.Xml;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace roxority_StpImport {

	public class Program {

		public static void ImportLics () {
			string tmp;
			long i = 0;
			bool isEval = false;
			List<Hashtable> lics = new List<Hashtable> ();
			Hashtable ht;
			XmlDocument doc = new XmlDocument ();
			SPList custList, licList;
			SPListItem custItem, licItem;
			SPQuery query;
			Dictionary<string, KeyValuePair<int, string>> prods = new Dictionary<string, KeyValuePair<int, string>> ();
			List<string> newCusts = new List<string> ();
			prods ["filterzen"] = new KeyValuePair<int, string> (1, "FilterZen");
			prods ["peoplezen"] = new KeyValuePair<int, string> (3, "PeopleZen");
			prods ["uploadzen"] = new KeyValuePair<int, string> (5, "UploadZen");
			prods ["exportzen"] = new KeyValuePair<int, string> (7, "ExportZen");
			prods ["greenbox"] = new KeyValuePair<int, string> (9, "GreenBox");
			prods ["duplexzen"] = new KeyValuePair<int, string> (10, "DuplexZen");
			prods ["printzen"] = new KeyValuePair<int, string> (11, "PrintZen");
			prods ["rollupzen"] = new KeyValuePair<int, string> (12, "RollupZen");
			foreach (string fp in Directory.GetFiles (@"E:\_r\My Dropbox\stps\lics-2010-05-10-08-03\licfiles", "*.*", SearchOption.TopDirectoryOnly)) {
				doc.Load (fp);
				ht = new Hashtable ();
				ht ["date"] = File.GetLastWriteTime (fp);
				ht ["name"] = doc.DocumentElement.SelectSingleNode("c").InnerText;
				ht ["id"] = doc.DocumentElement.SelectSingleNode ("i").InnerText;
				ht ["type"] = doc.DocumentElement.SelectSingleNode ("f1").InnerText;
				ht ["users"] = doc.DocumentElement.SelectSingleNode ("f2").InnerText;
				ht ["edition"] = doc.DocumentElement.SelectSingleNode ("f3").InnerText;
				ht ["expiry"] = doc.DocumentElement.SelectSingleNode ("f4").InnerText;
				ht ["product"] = (tmp = Path.GetFileNameWithoutExtension (fp)).Substring (0, tmp.IndexOf ('-'));
				lics.Add (ht);
			}
			lics.Sort (delegate (Hashtable ht1, Hashtable ht2) {
				return ((DateTime) ht1 ["date"]).CompareTo ((DateTime) ht2 ["date"]);
			});
			using (SPSite site = new SPSite ("http://roxwin7/roxority"))
			using (SPWeb web = site.OpenWeb ()) {
				web.AllowUnsafeUpdates = site.AllowUnsafeUpdates = true;
				custList = web.Lists ["Users"];
				licList = web.Lists ["Licenses"];
				foreach (Hashtable lic in lics) {
					query = new SPQuery (custList.DefaultView);
					query.AutoHyperlink = query.DatesInUtc = query.ExpandRecurrence = query.ExpandUserField = query.IncludeAllUserPermissions = query.IncludeAttachmentUrls = query.IncludeAttachmentVersion = query.IncludePermissions = query.ItemIdQuery = false;
					query.IncludeMandatoryColumns = query.IndividualProperties = query.RecurrenceOrderBy = true;
					query.Query = "<Where><Eq><FieldRef Name=\"Title\" /><Value Type=\"Text\">" + (tmp = lic ["name"] + string.Empty) + "</Value></Eq></Where>";
					custItem = null;
					foreach (SPListItem item in custList.GetItems (query)) {
						custItem = item;
						break;
					}
					if ((custItem == null) && !newCusts.Contains (tmp)) {
						custItem = custList.Items.Add ();
						custItem ["Title"] = tmp;
						custItem.Update ();
						newCusts.Add (tmp);
					}
					licItem = licList.Items.Add ();
					licItem ["Title"] = lic ["id"];
					licItem ["Licensee"] = new SPFieldLookupValue (custItem.ID, tmp);
					if ((!string.IsNullOrEmpty (tmp = lic ["product"] + string.Empty)) && prods.ContainsKey (tmp))
						licItem ["Product"] = new SPFieldLookupValue (prods [tmp].Key, prods [tmp].Value);
					if ((!string.IsNullOrEmpty (tmp = lic ["users"] + string.Empty)) && long.TryParse (tmp, out i))
						licItem ["Users"] = i;
					licItem ["Date"] = lic ["date"];
					if ("0".Equals (lic ["type"]))
						licItem ["Scope"] = new SPFieldLookupValue (3, "Site Coll.");
					else if ("1".Equals (lic ["type"]))
						licItem ["Scope"] = new SPFieldLookupValue (4, "Server Farm");
					else if ("2".Equals (lic ["type"]))
						licItem ["Scope"] = new SPFieldLookupValue (4, "Unltd. Farms");
					if (isEval = ((!string.IsNullOrEmpty (tmp = lic ["expiry"] + string.Empty)) && long.TryParse (tmp, out i) && (i > 0)))
						licItem ["Expiry Date"] = new DateTime (i);
					if (isEval)
						licItem ["Edition"] = new SPFieldLookupValue (7, "Evaluation");
					else if ("2".Equals (lic ["edition"]))
						licItem ["Edition"] = new SPFieldLookupValue (1, "Basic");
					else if ("6".Equals (lic ["edition"]))
						licItem ["Edition"] = new SPFieldLookupValue (2, "Ultimate");
					else
						licItem ["Edition"] = new SPFieldLookupValue (3, "Lite");
					licItem.Update ();
					Console.WriteLine (licItem.ID);
				}
				custList.Update ();
				licList.Update ();
			}
		}

		public static void ImportStps () {
			int nextID;
			object val;
			bool err;
			Dictionary<string, string []> schema = new Dictionary<string, string []> ();
			XmlDocument doc = new XmlDocument ();
			XmlNode node;
			SPField field = null;
			SPList list;
			SPListItem item;
			schema ["Countries"] = new string [] { "Title" };
			schema ["Processes"] = new string [] { "Title", "Placeholder", "Template", "Subject" };
			schema ["Users"] = new string [] { "Title", "URL", "Country", "Primary_x0020_Contact", "Address", "Partner", "Related", "Products", "Secondary_x0020_Contact" };
			schema ["Licenses"] = new string [] { "Title", "Customer", "Buyer", "Referrer", "Product", "Edition", "License_x0020_Type", "Users", "Order_x0020_Date", "Fee", "Fee0", "Refund", "Net", "Upgrade_x0020_from" };
			doc.Load (@"E:\_r\My Dropbox\stps\roxstp\manifest.xml");
			using (SPSite site = new SPSite ("http://roxwin7/roxority"))
			using (SPWeb web = site.OpenWeb ()) {
				web.AllowUnsafeUpdates = site.AllowUnsafeUpdates = true;
				foreach (string ln in new string [] { "Countries", "Processes", "Users", "Licenses" })
					if (((list = web.Lists [ln]) != null) && ((node = doc.DocumentElement.SelectSingleNode ("/Web/UserLists/List[@Title='" + ln + "']")) != null)) {
						nextID = 1;
						foreach (XmlNode rowNode in node.SelectNodes ("Data/Rows/Row")) {
							while (nextID != int.Parse (rowNode.SelectSingleNode ("Field[@Name='ID']").InnerText)) {
								(item = list.Items.Add ()) ["Title"] = nextID;
								item.Update ();
								nextID++;
							}
							item = list.Items.Add ();
							foreach (string cn in schema [ln])
								if (((node = rowNode.SelectSingleNode ("Field[@Name='" + cn + "']")) != null) && ((field = list.Fields.GetFieldByInternalName (cn)) != null)) {
									val = null;
									err = false;
									try {
										val = node.InnerText;
									} catch {
										err = true;
									}
									if (!(err || (field == null)))
										item [field.Id] = val;
								}
							item.Update ();
							nextID++;
						}
						list.Update ();
					}
			}
		}

		public static void Main (string [] args) {
			ImportLics ();
			Console.ReadLine ();
		}

	}

}
