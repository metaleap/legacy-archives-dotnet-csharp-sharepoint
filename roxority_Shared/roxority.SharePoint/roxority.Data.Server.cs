
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
#if !WSS
using Microsoft.Office.Server;
using Microsoft.Office.Server.UserProfiles;
#endif
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Web;

namespace roxority.Data.Providers {

	#region Bdc Class

#if PEOPLEZEN || (WSS && SP12)
#else
	public class Bdc : DataSource {

		public override string SchemaPropNamePrefix {
			get {
				return "bd";
			}
		}

	}
#endif

	#endregion

	#region UserProfiles Class

#if !WSS
	public class UserProfiles : UserDataSource {

		internal static Guid appID = Guid.Empty;
		private static readonly Dictionary<KnownProperty, string> knownMap = new Dictionary<KnownProperty, string> ();

		public readonly ServerContext SC;
		public readonly UserProfileManager Manager;
		public readonly IEnumerator Enum;

		private RecordPropertyCollection propCol = null;
		private Record curProf = null;
		private IDbConnection conn = null;

		static UserProfiles () {
#if PEOPLEZEN
			knownMap [KnownProperty.Email] = "WorkEmail";
			knownMap [KnownProperty.FirstName] = "FirstName";
#endif
			knownMap [KnownProperty.FriendlyName] = "PreferredName";
#if PEOPLEZEN
			knownMap [KnownProperty.LastName] = "LastName";
			knownMap [KnownProperty.LoginName] = "AccountName";
#endif
			knownMap [KnownProperty.Picture] = "PictureURL";
		}

		public static ServerContext GetServerContext (SPWeb site) {
			string sspName;
			ServerContext ctx = null;
			if (site == null)
				site = SPContext.Current.Web;
			SPSecurity.CatchAccessDeniedException = false;
			site.Site.CatchAccessDeniedException = false;
#if SP12
			if ((!ProductPage.Is14) && (!string.IsNullOrEmpty (sspName = ProductPage.Config (null, "SspName"))))
				ctx = ServerContext.GetContext (sspName);
#endif
			if ((ctx == null) && ((ctx = ServerContext.GetContext (site.Site)) == null) && ((ctx = ServerContext.Current) == null))
				throw new Exception (ProductPage.GetResource ("NoServerContext"));
			if (ctx.Status != SPObjectStatus.Online)
				throw new Exception (ProductPage.GetResource ("NoServerContextStatus", ctx.Status));
			return ctx;
		}

		public UserProfiles ()
			: this (GetServerContext (null)) {
		}

		public UserProfiles (SPWeb site)
			: this (GetServerContext (site)) {
		}

		public UserProfiles (ServerContext sc) {
			object tmpObj;
			if ((SC = sc) != null) {
				try {
					Manager = new UserProfileManager (sc, true, true);
				} catch (UnauthorizedAccessException) {
					Manager = new UserProfileManager (sc, false, true);
				}
				try {
					Enum = Manager.GetEnumerator ();
				} catch (Exception ex) {
					throw new Exception (ex.Message, ex);
				}
			}
			if (!ProductPage.Is14) {
				if (Guid.Empty.Equals (appID))
					try {
						appID = (Guid) Reflector.Current.Get (Reflector.Current.Get (sc, "SharedResourceProvider"), "ApplicationId");
					} catch {
						appID = new Guid ("df0bd2b0-1c72-497e-a86b-0783eb216993");
					}
				try {
					typeof (UserProfileManager).GetField ("m_bIsSiteAdminInit", BindingFlags.NonPublic | BindingFlags.Instance).SetValue (Manager, true);
					typeof (UserProfileManager).GetField ("m_bIsSiteAdmin", BindingFlags.NonPublic | BindingFlags.Instance).SetValue (Manager, true);
				} catch {
				}
			} else if (Guid.Empty.Equals (appID))
				try {
					appID = (Guid) (tmpObj = typeof (UserProfileManager).BaseType.GetField ("m_userProfileApplicationProxy", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (Manager)).GetType ().GetProperty ("AppID", BindingFlags.NonPublic | BindingFlags.Instance).GetValue (tmpObj, null);
				} catch {
				}
		}

		protected override string GetDefVal (string schemaProp) {
			string bv = base.GetDefVal (schemaProp);
			if (schemaProp == "vc")
				bv += @"
TITLE:[" + RewritePropertyName ("Title") + @"]";
			return bv;
		}

		public override bool AllowDateParsing (RecordProperty prop) {
			Property p = prop.Property as Property;
			Hashtable ht = prop.Property as Hashtable;
			return (((p != null) && ("date".Equals (p.Type, StringComparison.InvariantCultureIgnoreCase) || "datenoyear".Equals (p.Type, StringComparison.InvariantCultureIgnoreCase))) || ((ht != null) && ("12".Equals (ht ["DataTypeID"] + string.Empty) || "14".Equals (ht ["DataTypeID"] + string.Empty))) || base.AllowDateParsing (prop));
		}

		public override void Dispose () {
			if (!noDispose) {
				if (conn != null) {
					conn.Dispose ();
					conn = null;
				}
				base.Dispose ();
			}
		}

		public override string GetFieldInfoUrl (string webUrl, Guid contextID) {
			return ProductPage.MergeUrlPaths (webUrl, "_layouts/MgrProperty.aspx?ProfileType=User&ApplicationID=" + contextID);
		}

		public override string GetKnownPropName (KnownProperty kp) {
			return knownMap.ContainsKey (kp) ? knownMap [kp] : base.GetKnownPropName (kp);
		}

		public override string GetPropertyDisplayName (RecordProperty prop) {
			string locName = null;
			SPContext ctx;
			SPWeb web = null;
			CultureInfo culture;
			Property up = prop.Property as Property;
			Hashtable ht = prop.Property as Hashtable;
			if (ht != null) {
				ctx = ProductPage.GetContext ();
				if (ctx != null)
					web = ctx.Web;
				if ((web != null) && (web.CurrentUser != null) && (web.CurrentUser.RegionalSettings != null))
					locName = ht ["c_" + web.CurrentUser.RegionalSettings.LocaleId] + string.Empty;
				if (string.IsNullOrEmpty (locName) && (web != null) && (web.RegionalSettings != null))
					locName = ht ["c_" + web.RegionalSettings.LocaleId] + string.Empty;
				if (string.IsNullOrEmpty (locName) && (web != null) && (web.Locale != null))
					locName = ht ["c_" + web.Locale.LCID] + string.Empty;
				if (string.IsNullOrEmpty (locName))
					foreach (int lcid in ProductPage.TryEach<object> (ProductPage.WssInstalledCultures))
						if (!string.IsNullOrEmpty (locName = ht ["c_" + lcid] + string.Empty))
							break;
				if (string.IsNullOrEmpty (locName) && ((culture = ProductPage.GetFarmCulture (ctx)) != null))
					locName = ht ["c_" + culture.LCID] + string.Empty;
				if (!string.IsNullOrEmpty (locName))
					return locName;
				else
					foreach (string k in ht.Keys)
						if (k.StartsWith ("c_"))
							return ht [k] + string.Empty;
			}
			return ((up != null) ? up.DisplayName : base.GetPropertyDisplayName (prop));
		}

		public override RecordPropertyValueCollection GetPropertyValues (Record rec, RecordProperty prop) {
			string name;
			bool isHtml = false;
			UserProfile prof = rec.MainItem as UserProfile;
			UserProfileValueCollection vals;
			List<object> groups = null;
			SPUser user = null;
			RecordProperty nameProp;
			RecordPropertyValueCollection nameVal;
			Property origProp = prop.Property as Property;
			if (origProp != null)
				isHtml = (origProp.Type == "HTML");
			if (prop.Name == FIELDNAME_VCARDEXPORT)
				return new RecordPropertyValueCollection (this, rec, prop, new object [] { GetVcardExport (rec) }, null, null, null);
			else if (prop.Name == FIELDNAME_SITEGROUPS) {
				groups = new List<object> ();
				if (((nameProp = rec.DataSource.Properties.GetPropertyByName ("AccountName")) != null) && ((nameVal = GetPropertyValues (rec, nameProp)) != null) && (!string.IsNullOrEmpty (name = nameVal.Value + string.Empty))) {
					try {
						user = SPContext.Current.Web.AllUsers [name];
					} catch {
					}
					if (user != null)
						foreach (SPGroup group in ProductPage.TryEach<SPGroup> (user.Groups))
							try {
								groups.Add (group.Name);
							} catch {
							}
				}
				return new RecordPropertyValueCollection (this, rec, prop, groups.ToArray (), null, null, null);
			} else if (prop.Name == "roxUserPersonalUrl")
				return new RecordPropertyValueCollection (this, rec, prop, new object [] { GetPersonalUrl (rec) }, null, null, null);
			else if (prop.Name == "roxUserPublicUrl")
				return new RecordPropertyValueCollection (this, rec, prop, new object [] { GetPublicUrl (rec) }, null, null, null);
			else {
				vals = prof [prop.Name];
				return new RecordPropertyValueCollection (this, rec, prop, null, isHtml ? Array.ConvertAll<object, string> (new ArrayList (vals).ToArray (), delegate (object o) {
					return Record.HTML_PREFIX + o;
				}).GetEnumerator () : vals.GetEnumerator (), delegate () {
					return vals.Count;
				}, delegate () {
					if (isHtml)
						return Record.HTML_PREFIX + vals.Value;
					else
						return vals.Value;
				});
			}
		}

		public override Record GetRecord (long recID) {
			UserProfile prof = Manager.GetUserProfile (recID);
			return ((prof == null) ? null : new Record (this, prof, null, prof.ID, recID));
		}

		public override void InitSchema (JsonSchemaManager.Schema owner) {
			base.InitSchema (owner);
			InitSchemaForCaching (owner, true, false);
		}

		public override bool MoveNext () {
			curProf = null;
			return Enum.MoveNext ();
		}

		public override void Reset () {
			curProf = null;
			Enum.Reset ();
		}

		public bool AdoMode {
			get {
				return "s".Equals (this ["m", string.Empty]);
			}
		}

		public IDbConnection AdoConnection {
			get {
				if (conn == null) {
					conn = new System.Data.SqlClient.SqlConnection ("Data Source=ROXORITY\\SHAREPOINT; Initial Catalog=User Profile Service Application_ProfileDB_8f84974dfdac4fd1bcab512fe4dd11ea; Integrated Security=SSPI");
					conn.Open ();
				}
				return conn;
			}
		}

		public override Guid ContextID {
			get {
				return appID;
			}
		}

		public override long Count {
			get {
				return Manager.Count;
			}
		}

		public override object Current {
			get {
				object cur = Enum.Current;
				UserProfile prof = cur as UserProfile;
				if ((curProf == null) && (prof != null))
					curProf = new Record (this, prof, null, prof.ID, prof.RecordId);
				return curProf;
			}
		}

		public override RecordPropertyCollection Properties {
			get {
				bool isDesc;
				string tmp;
				Hashtable ht;
				Dictionary<string, Hashtable> dbProps;
				if (propCol == null) {
					propCol = new RecordPropertyCollection (this);
					if (AdoMode) {
						dbProps = new Dictionary<string, Hashtable> ();
						using (IDbCommand cmd = AdoConnection.CreateCommand ()) {
							cmd.CommandText = "SELECT PropertyID, PropertyName, DataTypeID, IsMultiValue FROM PropertyList";
							using (IDataReader r = cmd.ExecuteReader ())
								while (r.Read ()) {
									dbProps [r ["PropertyID"] + string.Empty]=ht = new Hashtable (r.FieldCount + 2);
									for (int i = 0; i < r.FieldCount; i++)
										ht [r.GetName (i)] = r [i] + string.Empty;
								}
							cmd.CommandText = "SELECT PropertyId, PropertyField, Lcid, Text FROM PropertyListLoc";
							using (IDataReader r = cmd.ExecuteReader ())
								while (r.Read ())
									if (dbProps.TryGetValue (r ["PropertyId"] + string.Empty, out ht) && ((isDesc = "2".Equals ((tmp = r ["PropertyField"] + string.Empty))) || "1".Equals (tmp)))
										ht [(isDesc ? "d_" : "c_") + r ["Lcid"]] = r ["Text"] + string.Empty;
						}
						foreach (KeyValuePair<string, Hashtable> kvp in dbProps)
							propCol.Props.Add (new RecordProperty (this, kvp.Value ["PropertyName"] + string.Empty, kvp.Value));
					} else
						foreach (Property prop in Manager.Properties)
							propCol.Props.Add (new RecordProperty (this, prop.Name, prop));
					EnsureUserFields (propCol);
					propCol.Props.Add (new RecordProperty (this, "roxUserPersonalUrl", null));
					propCol.Props.Add (new RecordProperty (this, "roxUserPublicUrl", null));
				}
				return propCol;
			}
		}

		public override bool RequireCaching {
			get {
				return !AdoMode;
			}
		}

		public override string SchemaPropNamePrefix {
			get {
				return "up";
			}
		}

	}
#endif

	#endregion

}
