
using Ionic.Zip;
using Microsoft.SharePoint;
using Microsoft.SharePoint.WebControls;
using roxority.Shared;
using roxority.Shared.IO;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Services;

namespace roxority_UploadZen {

	[WebService (Namespace = "http://xmlns.roxority.com/uploadzen/"), WebServiceBinding (ConformsTo = WsiProfiles.BasicProfile1_1)]
	public class Files : WebService {

		#region ChunkStream Class

		public class ChunkStream : Stream {

			public readonly int Chunks;
			public readonly string DirPath, FileBaseName;

			private int chunkIndex = 0;
			private long length = -1, pos = 0;
			private Stream fs = null;

			public ChunkStream (string dirPath, string baseName, int chunks) {
				DirPath = dirPath;
				FileBaseName = baseName;
				Chunks = chunks;
			}

			protected override void Dispose (bool disposing) {
				if (fs != null) {
					fs.Close ();
					fs.Dispose ();
					fs = null;
				}
				for (int i = 0; i < Chunks; i++)
					IOUtil.TryDeleteFile (Path.Combine (DirPath, FileBaseName + "__" + i), 10);
				base.Dispose (disposing);
			}

			public override void Flush () {
			}

			public override int Read (byte [] buffer, int offset, int count) {
				int len = -1;
				if ((fs == null) && (chunkIndex == 0))
					fs = File.Open (Path.Combine (DirPath, FileBaseName + "__" + chunkIndex), FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite);
				if ((fs != null) && ((len = fs.Read (buffer, offset, count)) < count)) {
					fs.Close ();
					fs = null;
					if (chunkIndex < (Chunks - 1)) {
						fs = File.OpenRead (Path.Combine (DirPath, FileBaseName + "__" + (++chunkIndex)));
						if (len <= 0)
							len = fs.Read (buffer, offset, count);
					}
				}
				return ((len == 0) ? -1 : len);
			}

			public override long Seek (long offset, SeekOrigin origin) {
				return -1;
			}

			public override void SetLength (long value) {
			}

			public override void Write (byte [] buffer, int offset, int count) {
			}

			public override bool CanRead {
				get {
					return true;
				}
			}

			public override bool CanSeek {
				get {
					return false;
				}
			}

			public override bool CanWrite {
				get {
					return false;
				}
			}

			public override long Length {
				get {
					if (length < 0) {
						length = 0;
						for (int i = 0; i < Chunks; i++)
							using (FileStream fs = File.Open (Path.Combine (DirPath, FileBaseName + "__" + i), FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.ReadWrite)) {
								length += fs.Length;
								fs.Close ();
							}
					}
					return length;
				}
			}

			public override long Position {
				get {
					return pos;
				}
				set {
				}
			}

		}

		#endregion

		private static readonly Dictionary<string, int> counts = new Dictionary<string, int> (), unzipCounts = new Dictionary<string, int> ();

		[ThreadStatic]
		private static bool? errorDetails;

		[ThreadStatic]
		private static string chunkTempDirPath = null;

		internal int delay = 0;

		public static bool ErrorDetails {
			get {
				if ((errorDetails == null) || !errorDetails.HasValue)
					errorDetails = ProductPage.Config<bool> (ProductPage.GetContext (), "ErrorDetails");
				return errorDetails.Value;
			}
		}

		public static string GetChunkTempDirPath (HttpContext context) {
			if ((chunkTempDirPath == null) && string.IsNullOrEmpty (chunkTempDirPath = ProductPage.Config (ProductPage.GetContext (), "ChunkPath")))
				chunkTempDirPath = context.Server.MapPath ("/_layouts/roxority_UploadZen/fc");
			return chunkTempDirPath;
		}

		public static string Upload () {
			byte [] file = null;
			bool hasAllChunks = false;
			long len;
			int chunk = -1, chunks = 0;
			string dirPath, fileName = string.Empty, safeBaseName, userName, webUrl = string.Empty;
			HttpContext context;
			Stream inStream;
			try {
				context = HttpContext.Current;
				if (!int.TryParse (context.Request ["chunk"], out chunk))
					chunk = -1;
				if (!int.TryParse (context.Request ["chunks"], out chunks))
					chunks = 0;
				if (string.IsNullOrEmpty (fileName = context.Request ["fn"]))
					fileName = context.Request ["name"];
				if ((chunks > 1) && (chunk < chunks)) {
					try {
						webUrl = SPContext.Current.Web.Url.TrimEnd ('/').ToLowerInvariant ();
					} catch {
					}
					try {
						if (string.IsNullOrEmpty (userName = context.Request.LogonUserIdentity.Name))
							throw new Exception ();
					} catch {
						try {
							if (string.IsNullOrEmpty (userName = ProductPage.LoginName (SPContext.Current.Web.CurrentUser.LoginName)))
								throw new Exception ();
						} catch {
							userName = Environment.UserDomainName + "\\" + Environment.UserName;
						}
					}
					safeBaseName = SharedUtil.GetSafeString (userName + "_" + webUrl + "_" + context.Request.QueryString ["cid"] + "_" + context.Request.QueryString ["dn"] + "_" + fileName);
					if (context.Request.Files.Count > 0) {
						inStream = context.Request.Files [0].InputStream;
						fileName = context.Request.Files [0].FileName;
					} else
						inStream = context.Request.InputStream;
					using (FileStream fs = File.Open (Path.Combine (dirPath = GetChunkTempDirPath (context), safeBaseName + "__" + chunk), FileMode.Create, FileAccess.Write, FileShare.Delete | FileShare.ReadWrite)) {
						IOUtil.CopyStream (inStream, fs);
						fs.Close ();
					}
					for (int i = 0; i < chunks; i++)
						if (!(hasAllChunks = File.Exists (Path.Combine (dirPath, safeBaseName + "__" + i))))
							break;
					if (hasAllChunks)
						using (ChunkStream cs = new ChunkStream (dirPath, safeBaseName, chunks))
						using (Files files = new roxority_UploadZen.Files ())
							return files.UploadDocument (fileName, cs, context.Request ["dn"] + ("2".Equals (context.Request ["uz"]) ? ("/" + fileName) : string.Empty), "1".Equals (context.Request ["uz"]) || "2".Equals (context.Request ["uz"]), "1".Equals (context.Request ["ow"]), context.Request ["ci"], "1".Equals (context.Request ["co"]), context.Request ["rp"], null, "1".Equals (context.Request ["hh"]));
					return string.Empty;
				} else {
					if (context.Request.Files.Count > 0) {
						context.Request.Files [0].InputStream.Read (file = new byte [len = context.Request.Files [0].InputStream.Length], 0, (int) len);
						fileName = context.Request.Files [0].FileName;
					} else if ((!"1".Equals (context.Request.QueryString ["html"])) && (context.Request.InputStream.Length > 0))
						context.Request.InputStream.Read (file = new byte [len = context.Request.InputStream.Length], 0, (int) len);
					if (file == null)
						throw new Exception (ProductPage.GetProductResource ("NoFilesUploaded"));
					using (Files files = new roxority_UploadZen.Files ())
						return files.UploadDocument (fileName, file, context.Request ["dn"] + ("2".Equals (context.Request ["uz"]) ? ("/" + fileName) : string.Empty), "1".Equals (context.Request ["uz"]) || "2".Equals (context.Request ["uz"]), "1".Equals (context.Request ["ow"]), context.Request ["ci"], "1".Equals (context.Request ["co"]), context.Request ["rp"], null, "1".Equals (context.Request ["hh"]));
				}
			} catch (Exception ex) {
				return ErrorDetails ? ex.ToString () : ex.Message;
			}
		}

		internal void CheckInOut (bool checkIn, ProductPage.LicInfo li, SPList list, SPFolder folder, ref SPFile file, string fixedFileName, bool autoCheckOut, string autoCheckIn, string helpResName) {
			if (checkIn && list.ForceCheckout && !string.IsNullOrEmpty (autoCheckIn)) {
				if (!ProductPage.LicEdition (SPContext.Current, li.dic, 2))
					throw new Exception (ProductPage.GetProductResource ("LicCheckInOut") + " " + ProductPage.GetProductResource (helpResName));
				else
					file.CheckIn ("Auto Check-In by UploadZen", (SPCheckinType) Enum.Parse (typeof (SPCheckinType), autoCheckIn, true));
			} else if ((!checkIn) && autoCheckOut && list.ForceCheckout) {
				try {
					if (file == null)
						file = folder.Files [fixedFileName];
				} catch {
				}
				if (!ProductPage.LicEdition (SPContext.Current, li.dic, 2))
					throw new Exception (ProductPage.GetProductResource ("LicCheckInOut") + " " + ProductPage.GetProductResource (helpResName));
				else if ((file != null) && file.Exists && ((file.CheckOutStatus == SPFile.SPCheckOutStatus.None) || (file.CheckedOutBy == null) || (file.CheckedOutBy.ID != SPContext.Current.Web.CurrentUser.ID) || (ProductPage.LoginName (file.CheckedOutBy.LoginName) != ProductPage.LoginName (SPContext.Current.Web.CurrentUser.LoginName))))
						file.CheckOut ();
			}
			if (delay > 0)
				Thread.Sleep (TimeSpan.FromSeconds (delay));
		}

		internal string FixName (string name, string replace) {
			foreach (char c in "~#%&*:<>?{|}/\\\"")
				name = name.Replace (c.ToString (), replace);
			return name;
		}

		internal void FixName (ref string name) {
			while (name.StartsWith ("."))
				name = name.Substring (1);
			while (name.EndsWith ("."))
				name = name.Substring (0, name.Length - 1);
			while (name.Contains (".."))
				name = name.Replace ("..", ".");
		}

		internal SPFolder GetFolder (SPWeb web, string folderPath, string replace) {
			return GetFolder (web, folderPath.Replace ('\\', '/').Split (new char [] { '/' }, StringSplitOptions.RemoveEmptyEntries), replace);
		}

		internal SPFolder GetFolder (SPWeb web, List<string> folderPath, string replace) {
			return GetFolder (web, folderPath.ToArray (), replace);
		}

		internal SPFolder GetFolder (SPWeb web, string [] folderPath, string replace) {
			SPFolder folder = null, tempFolder;
			for (int i = 0; i < folderPath.Length; i++) {
				FixName (ref folderPath [i]);
				folderPath [i] = FixName (folderPath [i], replace);
				if (folderPath [i].Length > 128)
					folderPath [i] = folderPath [i].Substring (0, 128);
			}
			for (int i = 0; i < folderPath.Length; i++) {
				try {
					tempFolder = ((folder == null) ? web.GetFolder (folderPath [i]) : folder.SubFolders [folderPath [i]]);
				} catch {
					tempFolder = null;
				}
				if (tempFolder != null)
					folder = tempFolder;
				else if (folder != null) {
					folder = folder.SubFolders.Add (folderPath [i]);
					if (delay > 0)
						Thread.Sleep (TimeSpan.FromSeconds (delay));
				} else
					break;
			}
			return folder;
		}

		internal void SetMetaData (SPListItem item, Dictionary<string, string> metaData, ref string messages) {
			SPField field;
			SPUser match;
			foreach (KeyValuePair<string, string> kvp in metaData)
				try {
					field = null;
					foreach (SPField f in ProductPage.TryEach<SPField> (item.Fields))
						if (f.InternalName.Equals (kvp.Key))
							field = f;
					if (field != null)
						if (field.Type == SPFieldType.Boolean)
							item [field.Id] = bool.Parse (kvp.Value);
						else if (field.Type == SPFieldType.DateTime)
							item [field.Id] = DateTime.Parse (kvp.Value);
						else if (field.Type == SPFieldType.Guid)
							item [field.Id] = new Guid (kvp.Value);
						else if (field.Type == SPFieldType.Integer)
							item [field.Id] = int.Parse (kvp.Value);
						else if (field.Type == SPFieldType.Number)
							item [field.Id] = decimal.Parse (kvp.Value);
						else if (field.Type == SPFieldType.User) {
							match = null;
							foreach (SPUserCollection users in new SPUserCollection [] { SPContext.Current.Web.AllUsers, SPContext.Current.Web.SiteUsers }) {
								if (match != null)
									break;
								else
									foreach (SPUser user in ProductPage.TryEach<SPUser> (users)) {
										if (ProductPage.LoginName (user.LoginName).Equals (kvp.Value, StringComparison.InvariantCultureIgnoreCase)) {
											match = user;
											break;
										}
									}
							}
							if (match != null)
								item [field.Id] = new SPFieldUserValue (SPContext.Current.Web, match.ID, ProductPage.LoginName (match.LoginName));
							else
								item [field.Id] = kvp.Value;
						} else
							item [field.Id] = kvp.Value;
				} catch (Exception ex) {
					messages += ("[METADATA " + kvp.Key + "] " + (ErrorDetails ? ex.ToString () : ex.Message) + "\r\n\r\n");
				}
			item.UpdateOverwriteVersion ();
		}

		[WebMethod]
		public string UploadDocument (string fileName, byte [] fileData, string folderName, bool unzip, bool overwrite, string autoCheckIn, bool autoCheckOut, string replace, byte [] metaDataRaw, bool hasHelp) {
			using (MemoryStream ms = new MemoryStream (fileData, false))
				return UploadDocument (fileName, ms, folderName, unzip, overwrite, autoCheckIn, autoCheckOut, replace, metaDataRaw, hasHelp);
		}

		[WebMethod]
		public string UploadDocument (string fileName, Stream fileData, string folderName, bool unzip, bool overwrite, string autoCheckIn, bool autoCheckOut, string replace, byte [] metaDataRaw, bool hasHelp) {
			SPFolder folder, subFolder;
			SPFile file;
			ProductPage.LicInfo li = ProductPage.LicInfo.Get (null);
			Dictionary<string, Dictionary<string, string>> metaData = null;
			Dictionary<string, string> metaBag;
			SPList list;
			string [] folderPath = folderName.Split (new char [] { '/' }, StringSplitOptions.RemoveEmptyEntries);
			string fullPath, messages = string.Empty, ext = (fileName.Contains (".") ? fileName.Substring (fileName.LastIndexOf ('.')) : string.Empty), helpResName = hasHelp ? "LicHelp" : "LicNoHelp";
			int diff;
			for (int i = 0; i < folderPath.Length; i++)
				if (folderPath [i].Length > 128)
					folderPath [i] = folderPath [i].Substring (0, 128);
			folderName = string.Join ("/", folderPath);
			if (!int.TryParse (ProductPage.Config (ProductPage.GetContext (), "UploadDelay"), out delay))
				delay = 0;
			else
				delay = Math.Abs (delay);
			if ((metaDataRaw != null) && (metaDataRaw.Length > 0))
				metaData = roxority.Shared.SharedUtil.DeserializeBinary (metaDataRaw) as Dictionary<string, Dictionary<string, string>>;
			if (!ProductPage.isEnabled)
				using (SPSite adminSite = ProductPage.GetAdminSite ())
					throw new Exception (ProductPage.GetResource ("NotEnabledPlain", ProductPage.MergeUrlPaths (adminSite.Url, "/_layouts/roxority_UploadZen.aspx?cfg=enable"), "UploadZen"));
			if (li.userBroken)
				throw new Exception (ProductPage	.GetResource ("LicExpiryUsers") + " " + ProductPage.GetProductResource (helpResName));
			else if (li.expired)
				throw new Exception (ProductPage.GetResource ("LicExpiry") + " " + ProductPage.GetProductResource (helpResName));
			//else if (string.IsNullOrEmpty (li.name) || (li.maxUsers < 0) || ((!string.IsNullOrEmpty (li.name)) && (li.maxUsers > 0) && (li.maxUsers <= 20))) {
			//    if (unzip) {
			//        unzipCounts [Context.Request.UserHostAddress] = unzipCounts.TryGetValue (Context.Request.UserHostAddress, out count) ? (++count) : (count = 1);
			//        if (count > ProductPage.l2)
			//            throw new Exception (ProductPage.GetProductResource ("LicTrial", ProductPage.l3, ProductPage.l2) + " " + ProductPage.GetProductResource (helpResName));
			//    } else {
			//        counts [Context.Request.UserHostAddress] = counts.TryGetValue (Context.Request.UserHostAddress, out count) ? (++count) : (count = 1);
			//        if (count > ProductPage.l3)
			//            throw new Exception (ProductPage.GetProductResource ("LicTrial", ProductPage.l3, ProductPage.l2) + " " + ProductPage.GetProductResource (helpResName));
			//    }
			//}
			if (unzip && !ProductPage.LicEdition (SPContext.Current, li.dic, 4))
				throw new Exception (ProductPage.GetProductResource ("LicUnzip") + " " + ProductPage.GetProductResource (helpResName));
			if ((!(Context.Request.LogonUserIdentity is WindowsIdentity)) && !ProductPage.LicEdition (SPContext.Current, li.sd, 4))
				throw new Exception (ProductPage.GetProductResource ("LicForms") + " " + ProductPage.GetProductResource (helpResName));
			if ((metaData != null) && (metaData.Count > 0) && !ProductPage.LicEdition (SPContext.Current, li.sd, 4))
				throw new Exception (ProductPage.GetProductResource ("LicMetaData") + " " + ProductPage.GetProductResource (helpResName));
			FixName (ref fileName);
			fileName = FixName (fileName, replace);
			if (fileName.Length > 128)
				fileName = fileName.Substring (0, 128 - ext.Length) + ext;
			using (SPWeb web = SPControl.GetContextWeb (HttpContext.Current)) {
				web.AllowUnsafeUpdates = true;
				fullPath = ('/' + folderName.Trim ('/') + '/' + fileName);	//ProductPage.MergeUrlPaths (web.Url, folderName.Trim ('/') + '/' + fileName);
				if (((diff = 260 - fullPath.Length) < 0) && ((diff = Math.Abs (diff)) < (fileName.Length - ext.Length)))
					fileName = fileName.Substring (0, ext.Length + diff) + ext;
				//if ((('/' + folderName.Trim ('/') + '/').Length < 260) && ((diff = 260 - ('/' + folderName.Trim ('/') + '/' + fileName).Length) < 0))
				//    fileName = fileName.Substring (0, ext.Length + Math.Abs (diff)) + ext;
				if (((folder = GetFolder (web, folderPath, replace)) == null) || ((list = web.Lists [folder.ParentListId]) == null))
					throw new FileNotFoundException (new FileNotFoundException ().Message, string.Join ("/", folderPath));
				if (!unzip) {
					file = null;
					CheckInOut (false, li, list, folder, ref file, fileName, autoCheckOut, autoCheckIn, helpResName);
					file = folder.Files.Add (fileName, fileData, overwrite || (list.ForceCheckout && autoCheckOut));
					if (delay > 0)
						Thread.Sleep (TimeSpan.FromSeconds (delay));
					CheckInOut (true, li, list, folder, ref file, fileName, autoCheckOut, autoCheckIn, helpResName);
					if (metaData != null)
						foreach (KeyValuePair<string, Dictionary<string, string>> kvp in metaData) {
							SetMetaData (file.Item, kvp.Value, ref messages);
							break;
						}
				} else
					using (ZipFile zipFile = ZipFile.Read (fileData))
						foreach (ZipEntry zipEntry in zipFile)
							try {
								folderPath = zipEntry.FileName.Replace ('\\', '/').Split (new char [] { '/' }, StringSplitOptions.RemoveEmptyEntries);
								if (zipEntry.IsDirectory) {
									subFolder = GetFolder (web, folderName.Trim ('/') + '/' + string.Join ("/", folderPath), replace);
									if ((metaData != null) && metaData.TryGetValue (zipEntry.FileName, out metaBag))
										SetMetaData (subFolder.Item, metaBag, ref messages);
								} else
									using (MemoryStream ms = new MemoryStream ()) {
										file = null;
										zipEntry.Extract (ms);
										fileName = zipEntry.FileName.Substring (zipEntry.FileName.LastIndexOf ('/') + 1);
										FixName (ref fileName);
										fileName = FixName (fileName, replace);
										ext = (fileName.Contains (".") ? fileName.Substring (fileName.LastIndexOf ('.')) : string.Empty);
										if (fileName.Length > 128)
											fileName = fileName.Substring (0, 128 - ext.Length) + ext;
										fullPath = folderName.Trim ('/') + '/' + zipEntry.FileName.TrimStart ('/');	//ProductPage.MergeUrlPaths (web.Url, folderName.Trim ('/') + '/' + fileName);
										if (((diff = 260 - fullPath.Length) < 0) && ((diff = Math.Abs (diff)) < (fileName.Length - ext.Length)))
											fileName = fileName.Substring (0, ext.Length + diff) + ext;
										CheckInOut (false, li, list, folder, ref file, fileName, autoCheckOut, autoCheckIn, helpResName);
										file = ((folderPath.Length == 1) ? folder : GetFolder (web, folderName.Trim ('/') + '/' + zipEntry.FileName.Substring (0, zipEntry.FileName.LastIndexOf ('/')).Trim ('/'), replace)).Files.Add (fileName, ms.ToArray (), overwrite || list.ForceCheckout);
										if (delay > 0)
											Thread.Sleep (TimeSpan.FromSeconds (delay));
										CheckInOut (true, li, list, folder, ref file, fileName, autoCheckOut, autoCheckIn, helpResName);
										if ((metaData != null) && metaData.TryGetValue (zipEntry.FileName, out metaBag))
											SetMetaData (file.Item, metaBag, ref messages);
									}
							} catch (Exception ex) {
								messages += (zipEntry.FileName + ": " + (ErrorDetails ? ex.ToString () : ex.Message) + "\r\n\r\n");
							}
			}
			return messages;
		}

	}

}
