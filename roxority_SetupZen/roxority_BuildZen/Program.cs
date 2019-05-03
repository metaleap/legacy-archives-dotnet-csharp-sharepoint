
using Ionic.Zip;
using roxority.Shared.IO;
using roxority.Shared.Xml;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Xml;

namespace roxority_BuildZen {

	using cfg = Properties.Settings;
	using res = Properties.Resources;

	public class Program {

		internal static readonly cfg config = cfg.Default;

		public static string ProjectName, ProjectShortName, ProjectShortNameLower, ProjectTitle;

		internal static string [] helpTopics = null;

		internal static void Build14 (string projPath, string buildPath, string path) {
			string fp12, cloneName;
			string [] cloneVals;
			List<string> list;
			XmlDocument mergeSrc = null, mergeTarget = null;
			DirectoryInfo dir14 = new DirectoryInfo (Path.Combine (projPath, path));
			foreach (DirectoryInfo dir in dir14.GetDirectories ("*", SearchOption.TopDirectoryOnly))
				Build14 (projPath, buildPath, path.TrimEnd ('\\') + '\\' + dir.Name);
			foreach (FileInfo file in dir14.GetFiles ("*", SearchOption.TopDirectoryOnly))
				if (!File.Exists (fp12 = file.FullName.Replace ("\\_14\\", "\\12\\").Replace (projPath, buildPath)))
					file.CopyTo (fp12, true);
				else {
					try {
						mergeSrc = new XmlDocument ();
						mergeTarget = new XmlDocument ();
						mergeSrc.Load (file.FullName);
						mergeTarget.Load (fp12);
					} catch {
						mergeSrc = null;
						mergeTarget = null;
					}
					if ((mergeSrc != null) && (mergeTarget != null)) {
						foreach (XmlNode mergeNode in mergeSrc.DocumentElement.ChildNodes)
							if (mergeNode.NodeType == XmlNodeType.Element)
								foreach (XmlNode targetNode in mergeTarget.SelectNodes (XmlUtil.Attribute (mergeNode, "Path")))
									if (mergeNode.LocalName == "AppendNode") {
										targetNode.AppendChild (mergeTarget.ImportNode (mergeNode.FirstChild, true));
										if ((!string.IsNullOrEmpty (cloneName = XmlUtil.Attribute (mergeNode, "CloneName"))) && ((cloneVals = XmlUtil.Attribute (mergeNode, "CloneValues").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (cloneVals.Length > 0)) {
											list = new List<string> ();
											foreach (string cv in cloneVals)
												if (!cv.Contains ("-"))
													list.Add (cv);
												else
													for (int i = int.Parse (cv.Substring (0, cv.IndexOf ('-'))); i <= int.Parse (cv.Substring (cv.IndexOf ('-') + 1)); i++)
														list.Add (i.ToString ());
											cloneVals = list.ToArray ();
											foreach (string attVal in cloneVals)
												XmlUtil.EnsureAttribute (targetNode.AppendChild (mergeTarget.ImportNode (mergeNode.FirstChild, true)), cloneName, attVal, true);
										}
									} else if (mergeNode.LocalName == "SetAtt")
										XmlUtil.EnsureAttribute (targetNode, XmlUtil.Attribute (mergeNode, "Name"), XmlUtil.Attribute (mergeNode, "Value"), true);
						mergeTarget.Save (fp12);
					}
				}
		}

		internal static IEnumerable<string> FindNext (string value, string startsWith, string endsWith, string endsWith2, string alsoYield) {
			int pos = -1, pos2, pos3;
			if (!string.IsNullOrEmpty (alsoYield))
				yield return alsoYield;
			while (((pos = value.IndexOf (startsWith, pos + 1)) > -1) && (((pos2 = value.IndexOf (endsWith, pos + 1)) > -1) | ((pos3 = value.IndexOf (endsWith2, pos + 1)) > -1)))
				yield return value.Substring (pos + startsWith.Length, (((pos3 > pos2) && (pos2 >= 0)) ? pos2 : pos3) - pos - startsWith.Length);
		}

		internal static string FixHtml (string html) {
			return html.Replace ("—", "&mdash;").Replace ("Ä", "&Auml;").Replace ("ä", "&auml;").Replace ("Ü", "&Uuml;").Replace ("ü", "&uuml;").Replace ("Ö", "&Ouml;").Replace ("ö", "&ouml;").Replace ("ß", "&szlig;").Replace ("<script ", "<!--script ").Replace ("</script>", "</script-->");
		}

		internal static ZipFile InitZip (ZipFile zipFile) {
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

		internal static string ProcessHtml (StringBuilder allHtml, XmlDocument doc, string outDir, string name, string html, string header, List<string> files, out string topicTitle) {
			bool isit, repeat = true, onlyw = false;
			IEnumerable<string> matches = FindNext (html, "/_layouts/images/", "\'", "\"", "star.gif");
			int pos, nextStart = 0, index = 0, cw, cwo;
			string aname, fixedHtml, title, guid = Guid.NewGuid ().ToString ();
			if (matches != null)
				foreach (string img in matches)
					try {
						if (files == null) {
							TryCopyFile (Path.Combine (config.Hive12 + "template\\images", img.TrimStart ('\\')), Path.Combine (outDir, "storage-images\\" + img));
							html = html.Replace ("/_layouts/images/" + img, "/storage/images/" + img);
						} else {
							TryCopyFile (Path.Combine (config.Hive12 + "template\\images", img.TrimStart ('\\')), Path.Combine (outDir, img.TrimStart ('\\')));
							if (!files.Contains (img.ToLowerInvariant ()))
								files.Add (img.ToLowerInvariant ());
							html = html.Replace ("/_layouts/images/" + img, img);
						}
					} catch {
					}
			html = html.Replace ("help/de/res/", "help/res/");
			if ((allHtml == null) && ((matches = FindNext (html, "help/res/", "\'", "\"", null)) != null))
				foreach (string img in matches) {
					try {
						TryCopyFile (Path.Combine (header, "res\\" + img), Path.Combine (outDir, "storage-images-" + ProjectName + "\\" + img));
					} catch {
					}
					html = html.Replace ("help/res/" + img, "/storage/images/" + ProjectName + "/" + img);
				}
			while (repeat) {
				repeat = false;
				cw = cwo = 0;
				if ((matches = FindNext (html, "?doc=", "\'", "\"", null)) != null)
					foreach (string href in matches) {
						if (isit = (!(onlyw || href.Contains ("#s")))) {
							cwo++;
							html = html.Replace ("?doc=" + href + "#s", guid);
							html = html.Replace ("?doc=" + href, href + ((allHtml == null) ? string.Empty : ".html"));
						} else {
							cw++;
							if (onlyw)
								html = html.Replace ("?doc=" + href, href.Substring (0, pos = href.IndexOf ('#')) + ((allHtml == null) ? string.Empty : ".html") + href.Substring (pos));
						}
						if (isit = isit && (html.IndexOf (guid) >= 0))
							html = html.Replace (guid, "?doc=" + href + "#s");
						repeat |= isit;
					}
				if (onlyw)
					repeat = onlyw = false;
				else if ((cwo == 0) && (cw > 0))
					repeat = onlyw = true;
			}
			if ((matches = FindNext (html, "?file=", "\'", "\"", null)) != null)
				foreach (string href in matches)
					html = html.Replace ("?file=" + href, "http://roxority.com/storage/sharepoint/" + ProjectShortNameLower + "/" + href);
			while (((pos = html.IndexOf ("</h3>", nextStart))) > 0) {
				html = html.Substring (0, pos) + (aname = ("<a name=\"s" + index + "\"></a>")) + html.Substring (pos);
				index++;
				nextStart = pos + aname.Length + 1;
			}
			topicTitle = FixHtml (doc.SelectSingleNode ("//data[@name='HelpTopic_" + name + "']/value").InnerText);
			if (allHtml == null)
				return FixHtml (html);
			allHtml.AppendFormat ("<h1>{0}</h1>", title = topicTitle);
			allHtml.Append (fixedHtml = FixHtml (html));
			return res.HtmlTemplateFile.Replace ("{$HELP_HEADER$}", header).Replace ("{$HELP_TITLE$}", title).Replace ("{$HELP_BODY$}", fixedHtml);
		}

		internal static void RewriteFile (string sourceFileName, string destFileName) {
			RewriteFile (sourceFileName, destFileName, null);
		}

		internal static void RewriteFile (string sourceFileName, string destFileName, string projectName, params string [] repls) {
			bool hasRepls = ((repls != null) && (repls.Length >= 2));
			if (File.Exists (sourceFileName) && Directory.Exists (Path.GetDirectoryName (destFileName)))
				if (sourceFileName.EndsWith (".png", StringComparison.InvariantCultureIgnoreCase) || sourceFileName.EndsWith (".jpg", StringComparison.InvariantCultureIgnoreCase))
					TryCopyFile (sourceFileName, destFileName);
				else
					using (StreamReader sr = File.OpenText (sourceFileName))
					using (StreamWriter sw = File.CreateText (destFileName))
						if (string.IsNullOrEmpty (projectName))
							sw.Write (sr.ReadToEnd ().Replace (hasRepls ? repls [0] : ProjectName, hasRepls ? repls [1] : "$$$PROJECTNAME$$$"));
						else
							sw.Write (sr.ReadToEnd ().Replace (hasRepls ? repls [1] : "$$$PROJECTNAME$$$", hasRepls ? repls [0] : projectName));
		}

		internal static void TryCopyFile (string sourceFileName, string destFileName) {
			string destDirPath = Path.GetDirectoryName (destFileName);
			if (File.Exists (sourceFileName)) {
				try {
					if (!Directory.Exists (destDirPath))
						Directory.CreateDirectory (destDirPath);
				} catch {
				}
				try {
					File.Copy (sourceFileName, destFileName, true);
				} catch {
				}
			}
		}

		public static void Main (string [] args) {
			ProjectName = ((args.Length > 1) ? args [1] : string.Empty);
			ProjectShortNameLower = (ProjectShortName = ProjectName.Substring (ProjectName.IndexOf ('_') + 1)).ToLowerInvariant ();
			if (args.Length > 2)
				ProjectTitle = string.Join (" ", args, 2, args.Length - 2);
			else
				ProjectTitle = (ProjectName.StartsWith ("Yukka_") ? "Yukka " : "ROXORITY ") + ProjectShortName;
			if (args [0] == "BuildDoc") {
				BuildDoc (Path.Combine (config.ProjectsRootPath + ProjectName, "chmdoc\\en"), config.ProjectsRootPath + ProjectName + "\\" + ProjectName + "\\12\\template\\layouts\\" + ProjectName + "\\help", "help\\res", config.ProjectsRootPath + ProjectName + "\\" + ProjectName + "\\Properties\\Resources.resx", string.Format (res.Title, ProjectTitle), "Yukka_GreenBox".Equals (ProjectName) ? res.WhiteHeadLine : res.HeadLine, ProjectShortName + res.NameSuffix, res.Lang);
				if ("Yukka_GreenBox".Equals (ProjectName))
					BuildDoc (Path.Combine (config.ProjectsRootPath + ProjectName, "chmdoc\\de"), config.ProjectsRootPath + ProjectName + "\\" + ProjectName + "\\12\\template\\layouts\\" + ProjectName + "\\help\\de", "help\\res", config.ProjectsRootPath + ProjectName + "\\" + ProjectName + "\\Properties\\Resources.de.resx", string.Format (res.Title_de, ProjectTitle), res.WhiteHeadLine_de, ProjectShortName + res.NameSuffix_de, res.Lang_de);
			} else if (args [0] == "CopyFiles")
				CopyFiles ();
			else if (args [0] == "PackFiles")
				PackFiles ("Debug".Equals (args [2]));
			else if (args [0] == "ClonePads")
				ClonePads ();
			else if (args [0] == "CheckRes")
				CheckRes ();
			else if ((args [0] == "Build14") && ProductPage.Is14)
				Build14 (args [2]);
		}

//        public static void Build12 () {
//#if DEBUG
//            bool debug = true;
//#else
//            bool debug = false;
//#endif
//            Dictionary<string, string> args = new Dictionary<string, string> ();
//            ProcessStartInfo procStart;
//            args ["12Path"] = Path.Combine (config.ProjectsRootPath, ProjectName + "\\" + ProjectName + "\\12");
//            args ["BuildCAS"] = "false";
//            args ["DeploymentTarget"] = "GAC";
//            args ["GACPath"] = Path.Combine (config.ProjectsRootPath, ProjectName + "\\" + ProjectName + "_12\\bin\\" + (debug ? "debug" : "release"));
//            args ["Outputpath"] = Path.Combine (config.ProjectsRootPath, ProjectName + "\\" + config.SetupPath);
//            args ["SolutionPath"] = Path.Combine (config.ProjectsRootPath, ProjectName + "\\" + ProjectName);
//            args ["WSPName"] = ProjectName + "_xii.wsp";
//            (procStart = new ProcessStartInfo (config.WspBuilderPath, ((Converter<Dictionary<string, string>, string>) delegate (Dictionary<string, string> dict) {
//                string all = string.Empty;
//                foreach (KeyValuePair<string, string> kvp in dict)
//                    all += string.Format (" -{0} {1}", kvp.Key, (kvp.Value.StartsWith ("C:\\", StringComparison.InvariantCultureIgnoreCase) || kvp.Value.EndsWith (".wsp", StringComparison.InvariantCultureIgnoreCase)) ? ("\"" + kvp.Value + "\"") : kvp.Value);
//                return all.Substring (1);
//            }) (args))).CreateNoWindow = procStart.UseShellExecute = true;
//            procStart.ErrorDialog = false;
//            procStart.WindowStyle = ProcessWindowStyle.Hidden;
//            using (Process proc = new Process ()) {
//                proc.StartInfo = procStart;
//                proc.Start ();
//                proc.WaitForExit ();
//            }
//        }

		public static void Build14 (string outDir) {
			string tmp, pCoreName = ((ProjectName.EndsWith ("_wss") || ProjectName.EndsWith ("_sbx")) ? ProjectName.Substring (0, ProjectName.Length - 4) : ProjectName), projPath = Path.Combine (config.ProjectsRootPath, pCoreName + "\\" + ProjectName), buildPath = Path.Combine (config.ProjectsRootPath, pCoreName + config.BuildPath + DateTime.Now.Ticks), wspPath = Path.Combine (config.ProjectsRootPath, pCoreName + config.SetupPath) + "\\" + pCoreName + "_xiv" + (ProjectName.EndsWith ("_wss") ? "_wss" : (ProjectName.EndsWith ("_sbx") ? "_sbx" : string.Empty)) + ".wsp";
			ProcessStartInfo procStart = new ProcessStartInfo (config.WspBuilderPath, string.Format ("-SolutionPath \"{0}\" -ProjectPath \"{0}\" -Outputpath \"{1}\" -WSPName \"{2}\"", buildPath, Path.GetDirectoryName (wspPath), Path.GetFileName (wspPath)));
			foreach (string dp in Directory.GetDirectories (Directory.GetParent (Path.Combine (config.ProjectsRootPath, pCoreName + config.BuildPath)).FullName, "wsp*", SearchOption.TopDirectoryOnly))
				try {
					Directory.Delete (dp, true);
				} catch {
				}
			Directory.CreateDirectory (buildPath);
			Directory.CreateDirectory (Path.Combine (buildPath, "bin"));
			Directory.CreateDirectory (tmp = Path.Combine (buildPath, outDir));
			IOUtil.CopyFiles (Path.Combine (projPath, outDir), tmp, "*", "*", "*", "*");
			IOUtil.CopyFiles (Path.Combine (projPath, "12"), Path.Combine (buildPath, "12"), "*", "*", "*", "*");
			File.Copy (Path.Combine (projPath, "WSPBuilder.exe.config"), Path.Combine (buildPath, "WSPBuilder.exe.config"), true);
			File.Copy (Path.Combine (projPath, "solutionid.txt"), Path.Combine (buildPath, "solutionid.txt"), true);
			if (Directory.Exists (tmp = Path.Combine (projPath, "_14")))
				Build14 (projPath, buildPath, "_14");
			procStart.CreateNoWindow = true;
			procStart.ErrorDialog = false;
			procStart.UseShellExecute = true;
			procStart.WindowStyle = ProcessWindowStyle.Hidden;
			Environment.CurrentDirectory = buildPath;
			using (Process proc = new Process ()) {
				proc.StartInfo = procStart;
				proc.Start ();
				proc.WaitForExit ();
			}
			Thread.Sleep (150);
			try {
				Directory.Delete (buildPath, true);
			} catch {
			}
		}

		public static void BuildDoc (string outDir, string htmlDir, string resDir, string resxFilePath, string title, string headerFormat, string chmOutName, string chmOutLang) {
			XmlDocument doc = new XmlDocument ();
			string toc = "", htmlTocList = string.Empty, htmlTocListItemFormat = "<li><a target=\"roxdoc\" href=\"_html/{0}.html\">{1}</a></li>", htmlFilePath, content, tempPath, topicTitle, tempHtml;
			int pos;
			StringBuilder allHtml = new StringBuilder ();
			List<string> files = new List<string> (new string [] { "help.tlhr.css", "jQuery.js" });
			doc.Load (resxFilePath);
			//foreach (string dirName in new string [] { "top_up", "top_up\\dashboard", "top_up\\jquery" })
			//    foreach (string fp in Directory.GetFiles (Path.Combine ((htmlDir.TrimEnd ('\\').EndsWith ("\\de") ? htmlDir.Replace ("\\help\\de", string.Empty) : htmlDir.Substring (0, htmlDir.LastIndexOf ('\\'))) + "\\img", dirName), "*", SearchOption.TopDirectoryOnly)) {
			//        TryCopyFile (fp, Path.Combine (Path.Combine (outDir, (resDir.TrimEnd ('\\') + "\\" + dirName).TrimStart ('\\')), Path.GetFileName (fp)));
			//        files.Add (resDir + "\\" + dirName + "\\" + Path.GetFileName (fp));
			//    }
			foreach (string resFilePath in Directory.GetFiles (Path.Combine (htmlDir, "res"), "*.*", SearchOption.TopDirectoryOnly)) {
				TryCopyFile (resFilePath, Path.Combine (Path.Combine (outDir, resDir.TrimStart ('\\')), Path.GetFileName (resFilePath)));
				files.Add (resDir + "\\" + Path.GetFileName (resFilePath));
			}
			TryCopyFile (Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\layouts\\roxority_Shared\\help\\res\\help.tlhr.css"), Path.Combine (outDir, "help.tlhr.css"));
			TryCopyFile (Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\layouts\\roxority_Shared\\jQuery.js"), Path.Combine (outDir, "jQuery.js"));
			if (helpTopics == null)
				helpTopics = doc.SelectSingleNode ("//data[@name='_HelpTopics']/value").InnerText.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			try {
				if (Directory.Exists (Path.Combine (outDir, "web")))
					Directory.Delete (Path.Combine (outDir, "web"), true);
				Directory.CreateDirectory (Path.Combine (outDir, "web"));
				Directory.CreateDirectory (Path.Combine (outDir, "web\\storage-images"));
				Directory.CreateDirectory (Path.Combine (outDir, "web\\storage-images-" + ProjectName));
			} catch {
			}
			foreach (string ht in helpTopics) {
				using (StreamReader reader = File.OpenText (htmlFilePath = Path.Combine (htmlDir, ht.TrimStart ('\\') + ".html")))
					content = reader.ReadToEnd ();
				using (StreamWriter writer = File.CreateText (tempPath = Path.Combine (outDir, Path.GetFileName (htmlFilePath)))) {
					files.Add (Path.GetFileName (htmlFilePath));
					writer.Write (ProcessHtml (allHtml, doc, outDir, Path.GetFileNameWithoutExtension (htmlFilePath), content, string.Format (headerFormat, title), files, out topicTitle));
				}
				using (StreamWriter writer = File.CreateText (htmlFilePath = Path.Combine (outDir, "web\\" + ht + ".html")))
					writer.Write (ProcessHtml (null, doc, Path.Combine (outDir, "web"), Path.GetFileNameWithoutExtension (htmlFilePath), content, htmlDir, null, out topicTitle));
				htmlTocList += string.Format (htmlTocListItemFormat, outDir.EndsWith ("\\de") ? ("de/" + ht) : ht, topicTitle);
			}
			foreach (string topic in helpTopics)
				toc += string.Format (res.TocItem, topic, ((topic == "intro") ? string.Format (outDir.EndsWith ("\\de") ? res.TocTitle_de : res.TocTitle, ProjectShortName) : FixHtml (doc.SelectSingleNode ("//data[@name='HelpTopic_" + topic + "']/value").InnerText.Replace ("\"", "\'"))));
			using (StreamWriter writer = File.CreateText (Path.Combine (outDir, ProjectName + ".hhk")))
				writer.Write (res.IndexFile);
			using (StreamWriter writer = File.CreateText (Path.Combine (outDir, ProjectName + ".hhc")))
				writer.Write (res.TocFile.Replace ("{$TOC$}", toc));
			using (StreamWriter writer = File.CreateText (Path.Combine (outDir, ProjectName + ".hhp")))
				writer.Write (res.ProjectFile.Replace ("{$OUTNAME$}", chmOutName).Replace ("{$NAME$}", ProjectName).Replace ("{$TOPIC$}", helpTopics [0]).Replace ("{$LANG$}", chmOutLang).Replace ("{$TITLE$}", title).Replace ("{$FILES$}", string.Join ("\r\n", files.ToArray ())));
			using (StreamWriter writer = File.CreateText (Path.Combine (outDir, ProjectName + ".html"))) {
				tempHtml = FixHtml (res.HtmlTemplateFile).Replace ("#f0f0f0", "#ffffff").Replace ("#303030", "#000000").Replace ("{$HELP_HEADER$}", "").Replace ("<h1>{$HELP_TITLE$}</h1>", "").Replace ("<title>{$HELP_TITLE$}</title>", "<title>" + string.Format (outDir.EndsWith ("\\de") ? res.Title_de : res.Title, ProjectTitle) + "</title><script type=\"text/javascript\" language=\"JavaScript\"> alert('For printing purposes only. Links have been disabled.\\nNur zum Ausdrucken geeignet. Links wurden deaktiviert.'); </script>").Replace ("{$HELP_BODY$}", allHtml.ToString ()).Replace ("<a\r\n", "<a ").Replace ("<a\r", "<a ").Replace ("<a\n", "<a ").Replace ("<a ", "<b style=\"font-style: italic; text-decoration: underline;\" ").Replace ("</a>", "</b>").Replace ("<h1>", "<h1 style=\"page-break-before: always;\">");
				if ((pos = tempHtml.IndexOf ("<h1 style=\"page-break-before: always;\">")) > 0)
					tempHtml = tempHtml.Substring (0, pos) + "<h1>" + tempHtml.Substring (pos + 39);
				writer.Write (tempHtml);
			}
			using (StreamWriter writer = File.CreateText (Path.Combine (config.ProjectsRootPath, ProjectName + config.SetupPath + "\\docs\\" + ProjectShortName + (outDir.EndsWith ("\\de") ? res.NameSuffix_de : res.NameSuffix) + ".html")))
				writer.Write (res.HtmlIndexFile.Replace ("{$HELP_TITLE$}", title).Replace ("{$HELP_TOCLIST$}", htmlTocList).Replace ("{$HELP_INTROPATH$}", outDir.EndsWith ("\\de") ? "_html/de/intro.html" : "_html/intro.html"));
			if (Directory.Exists (tempPath = Path.Combine (config.ProjectsRootPath, ProjectName + config.SetupPath + (outDir.EndsWith ("\\de") ? "\\docs\\_html\\de" : "\\docs\\_html"))))
				try {
					Directory.Delete (tempPath, true);
				} catch {
					try {
						Directory.Delete (tempPath, true);
					} catch {
					}
				}
			Directory.CreateDirectory (tempPath);
			Directory.CreateDirectory (Path.Combine (tempPath, "help"));
			foreach (string dirName in new string [] { "help\\res"/*, "help\\res\\top_up", "help\\res\\top_up\\dashboard", "help\\res\\top_up\\jquery"*/ }) {
				Directory.CreateDirectory (Path.Combine (tempPath, dirName));
				foreach (string fp in Directory.GetFiles (Path.Combine (outDir, dirName), "*", SearchOption.TopDirectoryOnly))
					TryCopyFile (fp, Path.Combine (tempPath, dirName + "\\" + Path.GetFileName (fp)));
			}
			foreach (string filter in new string [] { "*.html", "*.gif", "*.js", "*.css" })
				foreach (string fp in Directory.GetFiles (outDir, filter, SearchOption.TopDirectoryOnly))
					if (fp.EndsWith (".html", StringComparison.InvariantCultureIgnoreCase))
						RewriteFile (fp, Path.Combine (tempPath, Path.GetFileName (fp)), string.Empty, new string [] { "'help/res/top_up/'", "'_html/help/res/top_up/'" });
					else
						TryCopyFile (fp, Path.Combine (tempPath, Path.GetFileName (fp)));
		}

		public static void CheckRes () {
			string [] exts = { ".resx", ".de.resx"};
			List<string> resxPaths = new List<string> ();
			Dictionary<string, string> pairs = new Dictionary<string, string> (), pairsde = new Dictionary<string, string> (), pairsfr = new Dictionary<string, string> ();
			XmlDocument doc = new XmlDocument ();
			resxPaths.Add (Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Properties\\roxority_Shared"));
			resxPaths.Add (Path.Combine (config.ProjectsRootPath, ProjectName + "\\" + ProjectName + "\\Properties\\Resources"));
			resxPaths.Add (Path.Combine (config.ProjectsRootPath, ProjectName + "\\" + ProjectName + "\\12\\resources\\" + ProjectName));
			using (StreamWriter sw = File.CreateText (@"c:\temp\res_" + ProjectName + ".log"))
				foreach (string rp in resxPaths) {
					sw.WriteLine (rp.ToUpperInvariant ());
					pairs.Clear ();
					pairsde.Clear ();
					pairsfr.Clear ();
					doc.Load (rp + exts [0]);
					foreach (XmlNode node in doc.SelectNodes ("/root/data"))
						pairs [node.Attributes.GetNamedItem ("name").Value] = node.SelectSingleNode ("value").InnerText;
					doc.Load (rp + exts [1]);
					foreach (XmlNode node in doc.SelectNodes ("/root/data"))
						pairsde [node.Attributes.GetNamedItem ("name").Value] = node.SelectSingleNode ("value").InnerText;
					if (File.Exists (rp + exts [2])) {
						doc.Load (rp + exts [2]);
						foreach (XmlNode node in doc.SelectNodes ("/root/data"))
							pairsfr [node.Attributes.GetNamedItem ("name").Value] = node.SelectSingleNode ("value").InnerText;
					}
					foreach (string key in pairs.Keys) {
						if (!pairsde.ContainsKey (key))
							sw.WriteLine ("de missing " + key + "\t\t" + pairs [key].Replace ("\r", " ").Replace ("\n", " "));
						else if (pairsde [key] == pairs [key])
							sw.WriteLine ("de equals en for " + key + "\t\t" + pairsde [key].Replace ("\r", " ").Replace ("\n", " "));
						if (pairsfr.Count > 0)
							if (!pairsfr.ContainsKey (key))
								sw.WriteLine ("fr missing " + key + "\t\t" + pairs [key].Replace ("\r", " ").Replace ("\n", " "));
							else if (pairsfr [key] == pairs [key])
								sw.WriteLine ("fr equals en for " + key + "\t\t" + pairsfr [key].Replace ("\r", " ").Replace ("\n", " "));
							else if (pairsde.ContainsKey (key) && (pairsfr [key] == pairsde [key]))
								sw.WriteLine ("fr equals de for " + key + "\t\t" + pairsfr [key].Replace ("\r", " ").Replace ("\n", " "));
					}
					sw.WriteLine ();
				}
		}

		public static void ClonePads () {
			string srcPath, t, u, pu = string.Empty, pru = string.Empty, sn, fn = string.Empty, ln, adDirPath, padDirPath = @"E:\_r\My Dropbox\roxority-com\design2010\pad", allPads = string.Empty;
			string [] words,
				titlePaths = { "Site/Site_Site_Title", "Program_Info/Program_Name" },
				urlPaths = { "Site/Site_Site_URL", "Web_Info/Application_URLs/Application_Info_URL" },
				reUrlPaths = { "Web_Info/Download_URLs/Primary_Download_URL", "Web_Info/Application_URLs/Application_Order_URL" };
			int c = 0;
			ZipFile zip = null;
			XmlDocument doc;
			XmlNode node;
			if (Directory.Exists (padDirPath)) {
				foreach (string fp in Directory.GetFiles (padDirPath, "*", SearchOption.TopDirectoryOnly))
					try {
						File.Delete (fp);
					} catch {
					}
				foreach (string prod in config.Projects.Split (','))
					if (File.Exists (srcPath = config.PublishPath + (sn = prod.Substring (prod.IndexOf ('_') + 1)) + "\\" + prod + ".xml")) {
						allPads = ("http://roxority.com/storage/" + sn.ToLowerInvariant () + "/" + Path.GetFileName (srcPath) + "\r\n") + allPads;
						doc = new XmlDocument ();
						doc.Load (srcPath);
						if (doc.DocumentElement.SelectSingleNode ("PADmap") == null) {
							doc.DocumentElement.InnerXml += "<PADmap><PADmap_FORM>Y</PADmap_FORM><PADmap_DESCRIPTION>Link to plain text file containing all your PAD URLs from current host</PADmap_DESCRIPTION><PADmap_VERSION>1.0</PADmap_VERSION><PADmap_URL>http://www.padmaps.org/padmap.htm</PADmap_URL><PADmap_SCOPE>Company</PADmap_SCOPE><PADmap_Location>http://www.roxority.com/storage/pad/padmap.txt</PADmap_Location></PADmap>";
							doc.Save (srcPath);
						}
						if (Directory.Exists (adDirPath = @"E:\_r\My Dropbox\roxority-com\adwords\" + sn + "\\out"))
							foreach (string fp in Directory.GetFiles (adDirPath, "*.txt", SearchOption.TopDirectoryOnly))
								using (StreamReader sr = File.OpenText (fp))
									while (!string.IsNullOrEmpty (ln = sr.ReadLine ()))
										if (((words = ln.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (words.Length > 3)) {
											Console.Title = (++c) + string.Empty;
											doc = new XmlDocument ();
											doc.Load (srcPath);
											if ((node = doc.DocumentElement.SelectSingleNode ("RoboSoft")) != null)
												doc.DocumentElement.RemoveChild (node);
											t = string.Join (" ", words);
											u = "http://SharePoint-Tools.net/" + sn + "?" + (fn = string.Join ("-", words));
											pru = pu;
											allPads += ((pu = "http://roxority.com/storage/pad/" + fn.ToLowerInvariant () + ".xml") + "\r\n");
											if (!string.IsNullOrEmpty (pru)) {
												if ((node = doc.DocumentElement.SelectSingleNode ("PADRING")) == null)
													node = doc.DocumentElement.AppendChild (doc.CreateElement ("PADRING"));
												node.InnerText = pru;
											}
											if ((node = doc.DocumentElement.SelectSingleNode ("Web_Info/Application_URLs/Application_XML_File_URL")) != null)
												node.InnerText = pu;
											if ((node = doc.DocumentElement.SelectSingleNode ("MASTER_PAD_VERSION_INFO/MASTER_PAD_EDITOR")) != null)
												node.InnerText = "SharePoint-Tools.net";
											foreach (string xp in titlePaths)
												if ((node = doc.DocumentElement.SelectSingleNode (xp)) != null)
													node.InnerText = t;
											foreach (string xp in urlPaths)
												if ((node = doc.DocumentElement.SelectSingleNode (xp)) != null)
													node.InnerText = u;
											foreach (string xp in reUrlPaths)
												if ((node = doc.DocumentElement.SelectSingleNode (xp)) != null)
													node.InnerText = node.InnerText.Trim () + u.Substring (u.IndexOf ('?'));
											doc.Save (Path.Combine (padDirPath, fn.ToLowerInvariant () + ".xml"));
										}
					}
				using (StreamWriter sw = File.CreateText (Path.Combine (padDirPath, "padmap.txt")))
					sw.Write (allPads);
			}
			Console.Title = fn;
			c = 0;
			foreach (string fp in Directory.GetFiles (padDirPath, "*.xml")) {
				if ((c == 0) || ((c % 99) == 0)) {
					if (zip != null) {
						zip.Save (Path.Combine (padDirPath, "pads" + c + ".zip"));
						Console.WriteLine (Path.Combine (padDirPath, "pads" + c + ".zip"));
						zip.Dispose ();
					}
					zip = InitZip (null);
				}
				zip.AddFile (fp, string.Empty);
				c++;
			}
			if (zip != null) {
				zip.AddFile (Path.Combine (padDirPath, "padmap.txt"), string.Empty);
				zip.Save (Path.Combine (padDirPath, "pads" + c + ".zip"));
				zip.Dispose ();
			}
			Console.ReadLine ();
		}

		public static void CopyFiles () {
			string [] extenders = { "_12", "_12_wss", "_wss", "_sbx" }, noWssFeatures = { "roxority_BdcWebPart", "roxority_Cqwp" };
			string solutionPath = Path.Combine (config.ProjectsRootPath, ProjectName), projectPath = Path.Combine (solutionPath, ProjectName), tmp;
			//	copy resx and wsp files
			TryCopyFile (Path.Combine (projectPath, "12\\resources\\" + ProjectName + ".resx"), Path.Combine (projectPath, "12\\resources\\" + ProjectName + ".en-US.resx"));
			TryCopyFile (Path.Combine (projectPath, "12\\resources\\" + ProjectName + ".de.resx"), Path.Combine (projectPath, "12\\resources\\" + ProjectName + ".de-DE.resx"));
			TryCopyFile (Path.Combine (projectPath, "12\\resources\\" + ProjectName + ".de.resx"), Path.Combine (projectPath, "12\\resources\\" + ProjectName + ".de-AT.resx"));
			TryCopyFile (Path.Combine (projectPath, "12\\resources\\" + ProjectName + ".de.resx"), Path.Combine (projectPath, "12\\resources\\" + ProjectName + ".de-CH.resx"));
			if (ProjectName != "foo") {
				foreach (string extend in extenders)
					if (Directory.Exists (Path.Combine (solutionPath, ProjectName + extend))) {
						tmp = Path.Combine (solutionPath, ProjectName + extend + "\\12");
						try {
							Directory.Delete (tmp, true);
						} catch {
						}
						IOUtil.CopyFiles (Path.Combine (projectPath, "12"), tmp, "*", "*", "*", "*");
						if (!extend.Contains ("12"))
							IOUtil.CopyFiles (Path.Combine (projectPath, "_14"), Path.Combine (solutionPath, ProjectName + extend + "\\_14"), "*", "*", "*", "*");
						IOUtil.CopyFiles (projectPath, Path.Combine (solutionPath, ProjectName + extend), "*.txt", null, null, null);
						IOUtil.CopyFiles (projectPath, Path.Combine (solutionPath, ProjectName + extend), "*.config", null, null, null);
						if ((extend == "_12_wss") || (extend == "_wss"))
							foreach (string block in noWssFeatures)
								while (Directory.Exists (Path.Combine (tmp + @"\template\features", block)))
									try {
										Directory.Delete (Path.Combine (tmp + @"\template\features", block), true);
									} catch {
									} finally {
										Thread.Sleep (100);
									}
						if (File.Exists (tmp = Path.Combine (solutionPath, ProjectName + extend + "\\" + ProjectName + extend + ".wsp")))
							File.Copy (tmp, Path.Combine (solutionPath, config.SetupPath.TrimStart ('\\') + "\\" + ProjectName + ((extend == "_wss") ? "_xiv_wss" : extend).Replace ("_12", "_xii") + ".wsp"), true);
					}
			} else
				TryCopyFile (Path.Combine (projectPath, ProjectName + ".wsp"), Path.Combine (solutionPath, config.SetupPath.TrimStart ('\\') + "\\" + ProjectName + "_xii.wsp"));
			//	copy common files from project to Shared
			foreach (string fileName in Directory.GetFiles (Path.Combine (projectPath, "12\\template\\controltemplates\\" + ProjectName), "*.tc.*", SearchOption.TopDirectoryOnly))
				RewriteFile (fileName, Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\controltemplates\\roxority_Shared\\" + Path.GetFileName (fileName)));
			foreach (string fileName in Directory.GetFiles (Path.Combine (projectPath, "12\\template\\layouts\\" + ProjectName), "*.tl.*", SearchOption.TopDirectoryOnly))
				RewriteFile (fileName, Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\layouts\\roxority_Shared\\" + Path.GetFileName (fileName)));
			foreach (string fileName in Directory.GetFiles (Path.Combine (projectPath, "12\\template\\layouts\\" + ProjectName + "\\help\\res"), "*.tlhr.*", SearchOption.TopDirectoryOnly))
				RewriteFile (fileName, Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\layouts\\roxority_Shared\\help\\res\\" + Path.GetFileName (fileName)));
			RewriteFile (Path.Combine (projectPath, "12\\template\\layouts\\" + ProjectName + "\\default.aspx"), Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\layouts\\roxority_Shared\\default.aspx"));
			foreach (string dirName in new string [] { "img" /*, "img\\top_up", "img\\top_up\\dashboard", "img\\top_up\\jquery"*/ })
				foreach (string fileName in Directory.GetFiles (Path.Combine (projectPath, "12\\template\\layouts\\" + ProjectName + "\\" + dirName + "\\"), "*", SearchOption.TopDirectoryOnly))
					TryCopyFile (fileName, Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\layouts\\roxority_Shared\\" + dirName + "\\" + Path.GetFileName (fileName)));
			//	copy common files from Shared to all other projects
			foreach (string projectName in config.Projects.Split (','))
				if (projectName != ProjectName) {
					foreach (string fileName in Directory.GetFiles (Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\controltemplates\\roxority_Shared"), "*.tc.*", SearchOption.TopDirectoryOnly))
						RewriteFile (fileName, Path.Combine (config.ProjectsRootPath, projectName + "\\" + projectName + "\\12\\Template\\controltemplates\\" + projectName + "\\" + Path.GetFileName (fileName)), projectName);
					foreach (string fileName in Directory.GetFiles (Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\layouts\\roxority_Shared"), "*.tl.*", SearchOption.TopDirectoryOnly))
						RewriteFile (fileName, Path.Combine (config.ProjectsRootPath, projectName + "\\" + projectName + "\\12\\Template\\layouts\\" + projectName + "\\" + Path.GetFileName (fileName)), projectName);
					foreach (string fileName in Directory.GetFiles (Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\layouts\\roxority_Shared\\help\\res"), "*.tlhr.*", SearchOption.TopDirectoryOnly))
						RewriteFile (fileName, Path.Combine (config.ProjectsRootPath, projectName + "\\" + projectName + "\\12\\Template\\layouts\\" + projectName + "\\help\\res\\" + Path.GetFileName (fileName)), projectName);
					if (Directory.Exists (tmp = Path.Combine (config.ProjectsRootPath, projectName + "\\" + projectName + "\\12\\Template\\layouts\\" + projectName))) {
						RewriteFile (Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\layouts\\roxority_Shared\\default.aspx"), Path.Combine (tmp, "default.aspx"), projectName);
						foreach (string dirName in new string [] { "img" /*, "img\\top_up", "img\\top_up\\dashboard", "img\\top_up\\jquery"*/ })
							foreach (string fileName in Directory.GetFiles (Path.Combine (config.ProjectsRootPath, "roxority_Shared\\Templates\\layouts\\roxority_Shared\\" + dirName), "*", SearchOption.TopDirectoryOnly))
								TryCopyFile (fileName, Path.Combine (Path.Combine (tmp, dirName), Path.GetFileName (fileName)));
					}
				}
		}

		public static void PackFiles (bool isDebug) {
			bool doAtts = false;
			string publishPath = Path.Combine (config.PublishPath, ProjectShortNameLower), setupPath = Path.Combine (config.ProjectsRootPath, ProjectName + config.SetupPath), temp;
			FileAttributes atts = FileAttributes.Normal;
			ProcessStartInfo procStart = new ProcessStartInfo (Path.Combine (config.ProjectsRootPath, @"roxority_SetupZen\roxority_S3Sync\bin\Release\roxority_S3Sync.exe"), ProjectName);
			using (ZipFile zipFile = InitZip (null)) {
				zipFile.AddDirectory (Path.Combine (setupPath, "de"), ProjectName + "\\de");
				zipFile.AddDirectory (Path.Combine (setupPath, "docs"), ProjectName + "\\docs");
				if (Directory.Exists (temp = Path.Combine (setupPath, "files")))
					zipFile.AddDirectory (temp, ProjectName + "\\files");
				foreach (string file in Directory.GetFiles (setupPath, "*", SearchOption.TopDirectoryOnly)) {
					if (doAtts = Path.GetExtension (file).ToLowerInvariant ().EndsWith ("config"))
						File.SetAttributes (file, (atts = File.GetAttributes (file)) | FileAttributes.Hidden);
					zipFile.AddFile (file, ProjectName);
					if (doAtts)
						File.SetAttributes (file, atts);
				}
				zipFile.Save (temp = Path.Combine (publishPath, ProjectName + ".zip"));
				if (!isDebug) {
					File.Copy (temp, Path.Combine (@"c:\s3sync", Path.GetFileName (temp)), true);
					procStart.CreateNoWindow = false;
					procStart.ErrorDialog = procStart.UseShellExecute = true;
					procStart.WindowStyle = ProcessWindowStyle.Normal;
					using (Process proc = new Process ()) {
						proc.StartInfo = procStart;
						proc.Start ();
					}
				}
			}
		}

	}

}
