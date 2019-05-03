
using Microsoft.Office.Server;
using Microsoft.Office.Server.UserProfiles;
using Microsoft.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Drawing;
using System.IO;
using System.Text;

namespace roxority_PeopleZen_PictureImport {

	using res = Properties.Resources;
	using System.Security.Principal;

	public class Program {

		internal const string FORBIDDEN_CHARS = "/\\\"':*<>?!|~#%&{}";
		internal const bool DUMMY = false;

		internal static readonly Properties.Settings cfg = Properties.Settings.Default;
		internal static readonly string dummyCopyPath = @"C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\14\TEMPLATE\IMAGES\actionscreate.jpg";

		public static void Main (string [] args) {
			AppDomain.CurrentDomain.SetPrincipalPolicy (PrincipalPolicy.WindowsPrincipal);
			try {
				string userName, dirPath = null, fileName, imgUrl;
				byte [] data = null;
				int pos;
				bool hasData;
				SPDocumentLibrary lib = null;
				ServerContext sc = null;
				UserProfileManager man = null;
				UserProfile user = null;
				IEnumerator userEnum = null;
				DirectoryEntry entry = null;
				DirectorySearcher search = null;
				SearchResult result = null;
				PropertyValueCollection prop = null;
				ResultPropertyValueCollection rprop = null;
				SPFile spf;
				Predicate<IEnumerator> hasNext = delegate (IEnumerator o) {
					bool b;
					user = null;
					try {
						if (b = o.MoveNext ())
							user = o.Current as UserProfile;
						return b;
					} catch (Exception ex) {
						Console.WriteLine ("\r\n" + ex + "\r\n");
						return false;
					}
				};
				using (SPSite site = new SPSite (cfg.ImportPath))
				using (SPWeb web = site.OpenWeb ()) {
					if ((pos = cfg.ImportPath.IndexOf ("/_layouts/images/", StringComparison.InvariantCultureIgnoreCase)) > 0)
						dirPath = Path.Combine (@"C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\14\TEMPLATE\IMAGES", cfg.ImportPath.Substring (pos + "/_layouts/images/".Length).Replace ("/", "\\"));
					else if ((pos = cfg.ImportPath.IndexOf ("/_layouts/", StringComparison.InvariantCultureIgnoreCase)) > 0)
						dirPath = Path.Combine (@"C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\14\TEMPLATE\LAYOUTS", cfg.ImportPath.Substring (pos + "/_layouts/images/".Length).Replace ("/", "\\"));
					else
						try {
							if ((lib = web.GetList (cfg.ImportPath) as SPDocumentLibrary) == null)
								throw new Exception (res.NoLib + " " + cfg.ImportPath);
						} catch (Exception ex) {
							Console.WriteLine ("\r\n" + ex + "\r\n");
						}
					if ((lib != null) || ((dirPath != null) && Directory.Exists (dirPath)))
						try {
							if ((sc = ServerContext.GetContext (site)) == null)
								throw new Exception ("ServerContext.GetContext");
						} catch (Exception ex) {
							Console.WriteLine (res.InitErr, typeof (ServerContext).Name);
							Console.WriteLine ("\r\n" + ex + "\r\n");
						}
					if (sc != null) {
						try {
							man = new UserProfileManager (sc, cfg.SSPIgnorePrivacy, cfg.SSPBackwardCompatible);
							if ((userEnum = man.GetEnumerator ()) == null)
								throw new Exception ("UserProfileManager.GetEnumerator");
						} catch (Exception ex) {
							Console.WriteLine (res.InitErr, typeof (UserProfileManager).Name);
							Console.WriteLine ("\r\n" + ex + "\r\n");
						}
						if ((man != null) && (userEnum != null)) {
							while (hasNext (userEnum))
								if (user != null)
									try {
										Console.WriteLine ("=== " + (userName = user [cfg.SSPNameProp] + string.Empty).ToUpperInvariant () + " ===");
										if (!DUMMY)
											if (string.IsNullOrEmpty (cfg.ConnSearch))
												try {
													entry = NewDirectoryEntry (cfg.Conn.Replace ("$USERNAME$", userName), cfg.ConnUser, cfg.ConnPass, cfg.ConnAuth);
												} catch (Exception ex) {
													Console.WriteLine (res.InitErr, typeof (DirectoryEntry).Name);
													Console.WriteLine ("\r\n" + ex + "\r\n");
												} else
												try {
													entry = NewDirectoryEntry (cfg.Conn, cfg.ConnUser, cfg.ConnPass, cfg.ConnAuth);
													search = new DirectorySearcher (entry);
													search.Filter = cfg.ConnSearch.Replace ("$USERNAME$", userName);
													if ((result = search.FindOne ()) == null)
														throw new Exception ("DirectorySearcher.FindOne");
												} catch (Exception ex) {
													Console.WriteLine (res.InitErr, typeof (SearchResult).Name);
													Console.WriteLine ("\r\n" + ex + "\r\n");
												}
										if (((result != null) && (result.Properties == null)) || ((result == null) && (entry != null) && (entry.Properties == null)))
											Console.WriteLine (res.InitErr, typeof (System.DirectoryServices.PropertyCollection).Name);
										else if (DUMMY || ((entry != null) && (entry.Properties != null)) || ((result != null) && (result.Properties != null))) {
											rprop = null;
											prop = null;
											data = null;
											if (!DUMMY)
												try {
													if ((result != null) && (result.Properties != null))
														rprop = result.Properties [cfg.ConnProp];
													if ((entry != null) && (entry.Properties != null))
														prop = entry.Properties [cfg.ConnProp];
													if ((rprop == null) && (prop == null))
														throw new Exception ((result != null) ? "SearchResult.Properties" : "DirectoryEntry.Properties");
												} catch (Exception ex) {
													Console.WriteLine (res.InitErr, typeof (PropertyValueCollection).Name);
													Console.WriteLine ("\r\n" + ex + "\r\n");
												}
											hasData = true;
											if ((prop != null) || (rprop != null) || DUMMY) {
												if ((rprop != null) && ((rprop.Count == 0) || ((data = rprop [0] as byte []) == null))) {
													hasData = false;
													Console.WriteLine ("Picture data is " + ((rprop.Count == 0) ? "empty" : ((rprop [0] == null) ? "null" : rprop [0].GetType ().FullName)));
												}
												if (hasData && (data.Length == 0) && (prop != null) && ((data = prop.Value as byte []) == null)) {
													hasData = false;
													Console.WriteLine ("Picture data is " + ((prop.Value == null) ? "null" : prop.Value.GetType ().FullName));
												}
												if (DUMMY)
													using (FileStream fs = File.OpenRead (dummyCopyPath)) {
														data = new byte [fs.Length];
														fs.Read (data, 0, data.Length);
													}
												try {
													fileName = userName.ToLowerInvariant () + cfg.ImportExt;
													foreach (char c in FORBIDDEN_CHARS)
														fileName = fileName.Replace (c, '_');
													imgUrl = cfg.ImportPath.TrimEnd ('/') + '/' + fileName;
													if (hasData) {
														Console.WriteLine (res.Saving, imgUrl);
														if (lib != null) {
															spf = lib.RootFolder.Files.Add (fileName, data, true);
															imgUrl = web.Url.TrimEnd ('/') + '/' + spf.Url.TrimStart ('/');
														} else
															using (FileStream fs = File.Create (Path.Combine (dirPath, fileName))) {
																fs.Write (data, 0, data.Length);
																fs.Flush ();
																fs.Close ();
															}
														Console.WriteLine (res.Setting, imgUrl);
														user [cfg.SSPProp].Value = imgUrl;
													} else if (cfg.ClearEmpty) {
														Console.WriteLine (res.Removing, imgUrl);
														try {
															if (lib != null)
																lib.RootFolder.Files.Delete (fileName);
															else
																if (File.Exists (Path.Combine (dirPath, fileName)))
																	File.Delete (Path.Combine (dirPath, fileName));
														} catch {
														}
														Console.WriteLine (res.Setting, imgUrl);
														user [cfg.SSPProp].Value = String.Empty;
													}
													user.Commit ();
												} catch (Exception ex) {
													Console.WriteLine ("\r\n" + ex + "\r\n");
												}
											}
										}
									} finally {
										if (search != null) {
											search.Dispose ();
											search = null;
										}
									}
							if (lib != null)
								lib.Update ();
						}
					}
				}
				Console.WriteLine (res.Done);
				if (Environment.UserInteractive)
					Console.ReadLine ();
			} catch (Exception ex) {
				Console.WriteLine (ex.Message);
				Console.WriteLine (ex.StackTrace);
				throw ex;
			}
		}

		private static DirectoryEntry NewDirectoryEntry (string path, string username, string password, AuthenticationTypes authenticationType) {
			DirectoryEntry de;
			if (!String.IsNullOrEmpty (username)) {
				de = new DirectoryEntry (path, username, password, authenticationType);
			} else {
				de = new DirectoryEntry (path);
				de.AuthenticationType = authenticationType;
			}

			return de;
		}
	}
}
