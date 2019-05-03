
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.WebPartPages;
using roxority.SharePoint;
using roxority_FilterZen;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Web.UI.WebControls.WebParts;

using SPWebPart = Microsoft.SharePoint.WebPartPages.WebPart;
using SystemWebPart = System.Web.UI.WebControls.WebParts.WebPart;

namespace roxority_FilterZen.ApiTest {

	using cfg = Properties.Settings;

	public class Program {

		internal static readonly cfg config = cfg.Default;

		internal static void BatchAddDocuments (bool anew) {
			string [] months = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
			Random rnd = new Random ();
			SPDocumentLibrary lib;
			SPListItem item;
			using (SPSite site = new SPSite ("http://roxority"))
			using (SPWeb web = site.OpenWeb ())
				if ((lib = web.Lists ["ThousandsDocs"] as SPDocumentLibrary) != null) {
					for (int i = 0; i < 8000; i++)
						if (anew)
							lib.RootFolder.Files.Add (Console.Title = "text_" + i + ".txt", Encoding.UTF8.GetBytes ("Text File #" + i));
						else {
							(item = lib.Items [i]) ["Auto No"] = i;
							item ["Auto Text"] = "Auto Text " + i;
							if (i > 2880)
								item ["Month"] = months [rnd.Next (0, months.Length)];
							item.Update ();
							Console.Title = i.ToString ();
						}
				}
		}

		internal static void Iterate (StringCollection webUrls, List<SPPersistedObject> webApps, Action<SPSite>sitePreAction, Action<SPWeb> webAction, Action<SPSite> sitePostAction) {
			if ((webUrls != null) && (webUrls.Count > 0))
				foreach (string url in webUrls)
					using (SPSite site = new SPSite (url))
					using (SPWeb web = site.OpenWeb ()) {
						if (sitePreAction != null)
							sitePreAction (site);
						if (webAction != null)
							webAction (web);
						if (sitePostAction != null)
							sitePostAction (site);
					}
			else foreach(SPPersistedObject obj in webApps)
				if(obj is SPWebApplication)
					foreach (SPSite site in ProductPage.TryEach<SPSite> (((SPWebApplication) obj).Sites)) {
						if (sitePreAction != null)
							sitePreAction (site);
						if (webAction != null)
							foreach (SPWeb web in ProductPage.TryEach<SPWeb> (site.AllWebs))
								webAction (web);
						if (sitePostAction != null)
							sitePostAction (site);
					}
		}

		internal static void PrintDataProviders () {
			System.Data.DataTable dt = System.Data.Common.DbProviderFactories.GetFactoryClasses ();
			foreach (System.Data.DataRow row in dt.Rows) {
				Console.WriteLine ();
				for (int i = 0; i < dt.Columns.Count; i++) {
					Console.WriteLine (row [i]);
				}
			}
		}

		internal static void RemoveFilterZen (bool flagsOnly, params string [] flags) {
			List<SPPersistedObject> objs = new List<SPPersistedObject> (new SPPersistedObject [] { SPWebService.AdministrationService, SPWebService.ContentService, SPWebService.AdministrationService, SPWebService.ContentService.Farm });
			string centralAdminUrl = "http://your_central_administration";
			Action<SPPersistedObject> removeFlags = null;
			Action<SPFolder> removeWebParts = null;
			Action<SPWeb> webAction = delegate (SPWeb web) {
				Console.Title = "... " + web.Title + " @ " + web.Url;
				web.AllowUnsafeUpdates = true;
				removeWebParts (web.RootFolder);
			};
			Action<SPSite> sitePreAction = delegate (SPSite site) {
				site.AllowUnsafeUpdates = true;
				Console.WriteLine ("Detecting Filter Web Parts throughout " + site.Url);
			}, sitePostAction = delegate (SPSite site) {
				Console.Title = "... " + site.RootWeb.Title + " @ " + site.Url;
				if (config.DeactivateFeature)
					foreach (SPFeature feature in ProductPage.TryEach<SPFeature> (site.Features))
						if (feature.Definition.DisplayName == "roxority_FilterWebPart")
							try {
								site.Features.Remove (feature.DefinitionId, true);
								Console.WriteLine ("Deactivated " + feature.Definition.DisplayName + " at " + site.Url);
							} catch {
								Console.WriteLine ("Site Collection Feature " + feature.Definition.DisplayName + " could not be deactivated at " + site.Url + ". Manually deactivate this feature before proceeding.");
							}
			};
			removeWebParts = delegate (SPFolder folder) {
				SPLimitedWebPartManager webParts = null;
				SystemWebPart wp;
				int wpCount;
				foreach (SPFile file in folder.Files)
					if (file.Url.EndsWith (".aspx", StringComparison.InvariantCultureIgnoreCase)) {
						wpCount = 0;
						try {
							webParts = file.GetLimitedWebPartManager (PersonalizationScope.Shared);
						} catch {
							webParts = null;
						}
						if (webParts != null)
							for (int i = 0; i < webParts.WebParts.Count; i++) {
								try {
									wp = webParts.WebParts [i];
								} catch {
									wp = null;
								}
								if ((wp != null) && wp.GetType ().FullName.ToLowerInvariant ().Contains ("roxority_filterwebpart"))
									try {
										webParts.DeleteWebPart (wp);
										wpCount++;
										i--;
									} catch {
										Console.WriteLine ("Filter Web Part(s) on " + ProductPage.MergeUrlPaths (folder.ParentWeb.Site.Url, file.Url) + " could not be deleted. Delete them manually before proceeding.");
									}
							}
						if (wpCount > 0)
							try {
								file.Update ();
								Console.WriteLine ("...deleted " + wpCount + " Filter Web Part(s) from " + file.Name);
							} catch {
								Console.WriteLine ("Filter Web Part(s) on " + ProductPage.MergeUrlPaths (folder.ParentWeb.Site.Url, file.Url) + " were deleted, but file could not be saved. Delete them manually before proceeding.");
							}
					}
				foreach (SPFolder subFolder in folder.SubFolders)
					removeWebParts (subFolder);
			};
#if DEBUG
			removeFlags = delegate (SPPersistedObject obj) {
				SPWebService service = obj as SPWebService;
				SPWebApplication app = obj as SPWebApplication;
				foreach (string f in flags) {
					obj.Properties [obj.GetType ().Name.Substring (2) + f] = null;
					obj.Properties.Remove (obj.GetType ().Name.Substring (2) + f);
				}
				obj.Update (true);
				if (service != null)
					foreach (SPWebApplication a in ProductPage.TryEach<SPWebApplication> (service.WebApplications))
						removeFlags (a);
			};
#endif
			Console.WriteLine ("Initialization...");
			if (!flagsOnly)
				foreach (SPPersistedObject obj in objs.ConvertAll<SPPersistedObject> (delegate (SPPersistedObject po) {
					return po;
				}))
					if (obj is SPWebService)
						foreach (SPWebApplication app in ProductPage.TryEach<SPWebApplication> (((SPWebService) obj).WebApplications)) {
							objs.Add (app);
							if (app.IsAdministrationWebApplication)
								centralAdminUrl = app.Sites [0].Url;
						}
#if DEBUG
			Console.WriteLine ("Removing product configuration flags from content database...");
			SPSecurity.RunWithElevatedPrivileges (delegate () {
				foreach (SPPersistedObject obj in objs)
					removeFlags (obj);
			});
#endif
			if (!flagsOnly)
				Iterate (config.WebUrls, objs, sitePreAction, webAction, sitePostAction);
			Console.Title = "Done.";
			Console.WriteLine ("\r\n\r\nThis tool has finished its jobs. Now scan the above messages for any instructions on manual actions required on your part. If any, perform them, THEN go to " + ProductPage.MergeUrlPaths (centralAdminUrl, "/_admin/Solutions.aspx") + " and manually first retract, THEN remove the FilterZen solution from the solution store, THEN press ENTER to exit.");
		}

		internal static void TestWebParts () {
			string url = "http://roxserver/sites/filterzen/default.aspx";
			SPSite site = new SPSite (url);
			SPWeb web = site.OpenWeb ();
			roxority_FilterWebPart.OffSite = site;
			roxority_FilterWebPart webPart = new roxority_FilterWebPart ();
			FilterBase pageRequestFilter = FilterBase.Create ("roxority_FilterZen.FilterBase+RequestParameter");
			webPart.GetFilters ().Add (pageRequestFilter);
			pageRequestFilter.Name = "Project";
			pageRequestFilter.Enabled = true;
			pageRequestFilter.Set ("ParameterName", "projects");
			pageRequestFilter.Set ("RequestMode", 3);
			webPart.SerializedFilters = FilterBase.Serialize (webPart.GetFilters ());
			webPart.DebugMode = true;
			webPart.ApplyToolbarStylings = false;
			webPart.AutoRepost = false;
			webPart.DynamicInteractiveFilters = 1;
			webPart.HtmlMode = 0;
			webPart.RememberFilterValues = false;
			webPart.Title = "foo";
			SPLimitedWebPartManager webPartManager = web.GetLimitedWebPartManager (url, PersonalizationScope.Shared);
			webPartManager.AddWebPart (webPart, "LeftZone", 0);
			webPartManager.CacheInvalidate (webPart, Storage.Shared);
			webPartManager.SaveChanges (webPart);
		}

		public static void Main (string [] args) {
			//SPSecurity.RunWithElevatedPrivileges (delegate () {
			//    RemoveFilterZen (true, "FormControls", "SafeVects", "Collation", "Migration", "RefLink", "ReplClust", "StorageFlags", "SyncLock");
			//});
			//TestWebParts ();
			//SPSecurity.RunWithElevatedPrivileges (delegate () {
			//    RemoveFilterZen (true, "StorageFlags");
			//});
			BatchAddDocuments (true);
			Console.ReadLine ();
		}

	}

}
