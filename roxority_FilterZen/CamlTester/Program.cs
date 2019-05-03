
using Microsoft.SharePoint;
using System;
using System.Collections.Generic;
using System.Xml;

namespace CamlTester {

	public class Program {

		internal static void CollectNodes (List<XmlNode> nodes, XmlNode node) {
			if ((node.LocalName != "Where") && (node.LocalName != "Or") && (node.LocalName != "And"))
				nodes.Add (node);
			else
				foreach (XmlNode subNode in node.ChildNodes)
					if (subNode.NodeType == XmlNodeType.Element)
						CollectNodes (nodes, subNode);
		}

		public static void Main (string [] args) {
			XmlDocument doc = new XmlDocument ();
			List<XmlNode> nodes = new List<XmlNode> ();
			SPList tasks;
			SPQuery query;
			using (SPSite site = new SPSite ("http://roxority:16335/teamsite/"))
			using (SPWeb web = site.OpenWeb ()) {
				tasks = web.Lists ["Tasks"];
				doc.LoadXml (Properties.Resources.TextFile1);
				CollectNodes (nodes, doc.DocumentElement);
				foreach (XmlNode camlNode in nodes) {
					query = new SPQuery (tasks.DefaultView);
					query.IncludeAllUserPermissions = query.IncludeAttachmentUrls = query.IncludeAttachmentVersion = query.IncludeMandatoryColumns = query.IncludePermissions = query.IndividualProperties = query.ExpandUserField = query.ExpandRecurrence = query.AutoHyperlink = true;
					Console.Title = query.Query = "<Where>" + camlNode.OuterXml + "</Where>";
					try {
						foreach (SPListItem item in tasks.GetItems (query))
							break;
					} catch (Exception ex) {
						Console.WriteLine ();
						Console.WriteLine (camlNode.OuterXml);
						Console.WriteLine (ex.Message);
						Console.WriteLine ();
					}
				}
			}
			Console.ReadLine ();
		}

	}

}
