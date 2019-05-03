
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;

#if !WSS
using Microsoft.Office.Server;
using Microsoft.Office.Server.UserProfiles;
#endif
using roxority.Data;
using roxority.Shared;
using roxority.SharePoint;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.DirectoryServices;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml;



namespace roxority.Data {

	namespace Providers {

		#region Ado Class

		public class Ado :
#if PEOPLEZEN
			UserDataSource {
#else
			DataSource {
#endif

			private List<Record> recs = new List<Record> ();
			private IEnumerator recEnum = null;
			private string adoProvider = null;
			private IDbCommand cmd = null;
			private IDbConnection conn = null;
			private IDataReader reader = null;
			private RecordPropertyCollection props;
			private Reflector refl = null;
			private Guid? contextID = null;

			public Ado () {
				props = new RecordPropertyCollection (this);
			}

			public override void Dispose () {
				if (!noDispose)
					try {
						if (reader != null) {
							reader.Dispose ();
							reader = null;
						}
						if (cmd != null) {
							cmd.Dispose ();
							cmd = null;
						}
						if (conn != null) {
							conn.Dispose ();
							conn = null;
						}
					} catch {
					} finally {
						base.Dispose ();
					}
			}

			public override RecordPropertyValueCollection GetPropertyValues (Record rec, RecordProperty prop) {
				IDictionary dic = rec.MainItem as IDictionary;
#if PEOPLEZEN
				string name;
				List<object> groups;
				SPUser user = null;
				if (prop.Name == FIELDNAME_VCARDEXPORT)
					return new RecordPropertyValueCollection (this, rec, prop, new object [] { GetVcardExport (rec) }, null, null, null);
				else if (prop.Name == FIELDNAME_SITEGROUPS) {
					groups = new List<object> ();
					if (!string.IsNullOrEmpty (name = Record.GetSpecialFieldValue (this, DataSource.SCHEMAPROP_PREFIX + UserDataSource.SCHEMAPROP_LOGINFIELD, delegate (string pn) {
						return rec [pn, string.Empty];
					}))) {
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
				}
#endif
				return new RecordPropertyValueCollection (this, rec, prop, ((dic == null) ? new object [0] : new object [] { dic [prop.Name] }), null, null, null);
			}

			public override Record GetRecord (long recID) {
				if (recs.Count == 0)
					foreach (Record r in this)
						if (r.RecordID == recID)
							return r;
				return ((recs.Count >= recID) ? recs [(int) recID - 1] : null);
			}

			public override void InitSchema (JsonSchemaManager.Schema owner) {
				IDictionary pmore = new OrderedDictionary ();
				base.InitSchema (owner);
				pmore ["config"] = "DataProviders";
				pmore ["always_show_help"] = true;
				AddSchemaProp (owner, "ac", "ConfigChoice", pmore);
				pmore = new OrderedDictionary ();
				pmore ["lines"] = 4;
				pmore ["always_show_help"] = true;
				AddSchemaProp (owner, "cs", "String", pmore);
				pmore = new OrderedDictionary ();
				pmore ["always_show_help"] = true;
				pmore ["is_password"] = true;
				AddSchemaProp (owner, "pw", "String", pmore);
				pmore = new OrderedDictionary ();
				pmore ["always_show_help"] = true;
				pmore ["lines"] = 8;
				AddSchemaProp (owner, "sq", "String", pmore);
				pmore = new OrderedDictionary ();
				AddSchemaProp (owner, SCHEMAPROP_DEFAULTFIELDS, "DataFields", "w", true, pmore);
				pmore = new OrderedDictionary ();
				AddSchemaProp (owner, SCHEMAPROP_TITLEFIELD, "DataFields", "w", true, pmore);
				pmore = new OrderedDictionary ();
				AddSchemaProp (owner, SCHEMAPROP_PICTUREFIELD, "DataFields", "w", true, pmore);
				pmore = new OrderedDictionary ();
				AddSchemaProp (owner, SCHEMAPROP_URLFIELD, "DataFields", "w", true, pmore);
				InitSchemaForCaching (owner, false, true);
			}

			public override bool MoveNext () {
				return ((recEnum != null) ? recEnum.MoveNext () : AdoReader.Read ());
			}

			public override void Reset () {
				if ((recEnum == null) && (recs.Count > 0))
					recEnum = recs.GetEnumerator ();
				if (recEnum != null)
					recEnum.Reset ();
			}

			public IDbCommand AdoCommand {
				get {
					if (cmd == null) {
						cmd = AdoConnection.CreateCommand ();
						cmd.CommandText = AdoQuery;
					}
					return cmd;
				}
			}

			public IDbConnection AdoConnection {
				get {
					if (conn == null)
						conn = AdoReflector.New (AdoProviderType, AdoConnectionString) as IDbConnection;
					if (conn == null)
						throw new Exception (ProductPage.GetResource ("Data_AdoConnError", AdoProviderName));
					return conn;
				}
			}

			public string AdoConnectionString {
				get {
					return this ["cs", string.Empty].Replace ("{$ROXPWD$}", this ["pw", string.Empty]);
				}
			}

			public string AdoProvider {
				get {
					if (adoProvider == null)
						adoProvider = this ["ac", string.Empty];
					return adoProvider;
				}
			}

			public string AdoProviderAssembly {
				get {
					return AdoProvider.Substring (AdoProvider.IndexOf (',') + 1).Trim ();
				}
			}

			public string AdoProviderName {
				get {
					JsonSchemaManager.Property.Type.ConfigChoice prop = JsonSchema [SchemaPropNamePrefix + "_ac"].PropertyType as JsonSchemaManager.Property.Type.ConfigChoice;
					return prop.Choices [AdoProvider] + string.Empty;
				}
			}

			public string AdoProviderType {
				get {
					return (AdoProvider.IndexOf (',') > 0) ? AdoProvider.Substring (0, AdoProvider.IndexOf (',')) : AdoProvider;
				}
			}

			public string AdoQuery {
				get {
					return this ["sq", string.Empty];
				}
			}

			public IDataReader AdoReader {
				get {
					if (reader == null) {
						recs.Clear ();
						AdoConnection.Open ();
						reader = AdoCommand.ExecuteReader ();
					}
					return reader;
				}
			}

			public Reflector AdoReflector {
				get {
					if (refl == null)
						refl = new Reflector (Assembly.LoadWithPartialName (AdoProviderAssembly));
					return refl;
				}
			}

			public override Guid ContextID {
				get {
					if (((contextID == null) || !contextID.HasValue) && (JsonInstance != null) && Guid.Empty.Equals (contextID = ProductPage.GetGuid (JsonInstance ["id"] + string.Empty, true)))
						contextID = new Guid ("ef0bd2b1-2c73-597f-b86c-1783eb216994");
					return contextID.HasValue ? contextID.Value : Guid.Empty;
				}
			}

			public override long Count {
				get {
					return (RequireCaching && recs.Count < 1) ? -1 : recs.Count;
				}
			}

			public override object Current {
				get {
					object val;
					string pname;
					IDictionary dic;
					Record rec;
					if (recEnum != null)
						return recEnum.Current;
					dic = new OrderedDictionary ();
					for (int i = 0; i < AdoReader.FieldCount; i++) {
						try {
							val = AdoReader.GetValue (i);
						} catch (Exception ex) {
							val = ex;
						}
						if (props.GetPropertyByName (pname = AdoReader.GetName (i)) == null)
							props.Props.Add (new RecordProperty (this, pname, AdoReader.GetFieldType (i)));
						dic.Add (pname, val);
					}
					recs.Add (rec = new Record (this, dic, null, Guid.Empty, recs.Count + 1));
					return rec;
				}
			}

			public override RecordPropertyCollection Properties {
				get {
					if (props.Props.Count == 0) {
						foreach (object rec in this)
							break;
#if PEOPLEZEN
						EnsureUserFields (props);
#endif
					}
					return props;
				}
			}

			public override string SchemaPropNamePrefix {
				get {
					return "db";
				}
			}

		}

		#endregion

		#region Directory Class

		public class Directory : UserDataSource {

			public static readonly Dictionary<string, string> PropMappings = new Dictionary<string, string> ();

			internal static Dictionary<Guid, Dictionary<long, string>> cacheHelpers = new Dictionary<Guid, Dictionary<long, string>> ();

			private static readonly Dictionary<KnownProperty, string> knownMap = new Dictionary<KnownProperty, string> ();

			private DirectoryEntry dirRoot = null;
			private DirectorySearcher dirSearch = null;
			private SearchResultCollection dirSearchResults = null;
			private IEnumerator dirEnum = null, recEnum = null;
			private List<IDisposable> disposables = new List<IDisposable> ();
			private List<Record> records = new List<Record> ();
			private RecordPropertyCollection props = null;
			private bool clearRecs = false;
			private AuthenticationTypes? dirAuth = null;
			private SearchScope? dirScope = null;
			private Guid? contextID = null;

			static Directory () {
#if PEOPLEZEN
				knownMap [KnownProperty.Email] = "mail";
				knownMap [KnownProperty.FirstName] = "givenName";
#endif
				knownMap [KnownProperty.FriendlyName] = "cn";
#if PEOPLEZEN
				knownMap [KnownProperty.LastName] = "sn";
				knownMap [KnownProperty.LoginName] = "userPrincipalName";
#endif
				knownMap [KnownProperty.Picture] = string.Empty;
#if PEOPLEZEN
				PropMappings ["AccountName"] = knownMap [KnownProperty.LoginName];
				PropMappings ["WorkEmail"] = knownMap [KnownProperty.Email];
#endif
				PropMappings ["PreferredName"] = knownMap [KnownProperty.FriendlyName];
				PropMappings ["PictureURL"] = string.Empty;
				PropMappings ["CellPhone"] = "telephoneNumber";
				PropMappings ["Department"] = "department";
				PropMappings ["Title"] = "title";
				PropMappings ["SPS-Location"] = "physicalDeliveryOfficeName";
				PropMappings ["AboutMe"] = "info";
				PropMappings ["UserName"] = "sAMAccountName";
			}

			internal DirectoryEntry MakeEntry (string path) {
				DirectoryEntry entry;
				if (string.IsNullOrEmpty (DirAuthUser) || string.IsNullOrEmpty (DirAuthPass))
					entry = new DirectoryEntry (path, null, null, DirAuth);
				else
					entry = new DirectoryEntry (path, DirAuthUser, DirAuthPass, DirAuth);
				return entry;
			}

			internal Record MakeRecord (object obj, bool addCacheHelper, long recID) {
				Dictionary<long, string> helper = null;
				DirectoryEntry entry;
				SearchResult result;
				entry = obj as DirectoryEntry;
				result = obj as SearchResult;
				if (obj == null)
					return null;
				if (props == null)
					props = new RecordPropertyCollection (this);
				if (result != null) {
					entry = result.GetDirectoryEntry ();
					foreach (string pn in result.Properties.PropertyNames)
						if (props.GetPropertyByName (pn) == null)
							props.Props.Add (new RecordProperty (this, pn, null));
				}
				if (entry != null) {
					if (addCacheHelper) {
						if ((helper == null) && ((!cacheHelpers.TryGetValue (ContextID, out helper)) || (helper == null)))
							cacheHelpers [ContextID] = helper = new Dictionary<long, string> ();
						helper [recID] = entry.Path;
					}
					disposables.Add (entry);
					foreach (string pn in entry.Properties.PropertyNames)
						if (props.GetPropertyByName (pn) == null)
							props.Props.Add (new RecordProperty (this, pn, null));
				}
				return new Record (this, entry, result, Guid.Empty, recID);
			}

			public override void Dispose () {
				if (!noDispose) {
					foreach (IDisposable disp in disposables)
						if (disp != null)
							disp.Dispose ();
					disposables.Clear ();
					dirEnum = null;
					recEnum = null;
					if (dirSearchResults != null) {
						dirSearchResults.Dispose ();
						dirSearchResults = null;
					}
					if (dirSearch != null) {
						dirSearch.Dispose ();
						dirSearch = null;
					}
					if (dirRoot != null) {
						dirRoot.Dispose ();
						dirRoot = null;
					}
					base.Dispose ();
				}
			}

			public override string GetKnownPropName (DataSource.KnownProperty kp) {
				return (knownMap.ContainsKey (kp) ? knownMap [kp] : base.GetKnownPropName (kp));
			}

			public override RecordPropertyValueCollection GetPropertyValues (Record rec, RecordProperty prop) {
				DirectoryEntry entry = rec.MainItem as DirectoryEntry;
				SearchResult result = rec.RelItem as SearchResult;
				ResultPropertyValueCollection rvals = ((result == null) ? null : result.Properties [prop.Name]);
				PropertyValueCollection pvals = ((entry == null) ? null : entry.Properties [prop.Name]);
				string name;
				IDictionary dic = rec.MainItem as IDictionary;
				List<object> groups;
				SPUser user = null;
				if (prop.Name == FIELDNAME_VCARDEXPORT)
					return new RecordPropertyValueCollection (this, rec, prop, new object [] { GetVcardExport (rec) }, null, null, null);
				else if (prop.Name == FIELDNAME_SITEGROUPS) {
					groups = new List<object> ();
					if (!string.IsNullOrEmpty (name = Record.GetSpecialFieldValue (this, DataSource.SCHEMAPROP_PREFIX + UserDataSource.SCHEMAPROP_LOGINFIELD, delegate (string pn) {
						return rec [pn, string.Empty];
					}))) {
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
				}
				if (rvals != null)
					return new RecordPropertyValueCollection (this, rec, prop, null, rvals.GetEnumerator (), delegate () {
						return rvals.Count;
					}, delegate () {
						foreach (object o in rvals)
							return o;
						return null;
					});
				if (pvals != null)
					return new RecordPropertyValueCollection (this, rec, prop, null, pvals.GetEnumerator (), delegate () {
						return pvals.Count;
					}, delegate () {
						return pvals.Value;
					});
				return null;
			}

			public override Record GetRecord (long recID) {
				string cp;
				Dictionary<long, string> helper;
				if (records.Count == 0) {
					if (RequireCaching && cacheHelpers.TryGetValue (ContextID, out helper) && (helper != null) && helper.TryGetValue (recID, out cp) && !string.IsNullOrEmpty (cp))
						return MakeRecord (MakeEntry (cp), false, recID);
					foreach (Record r in this)
						if (r.RecordID == recID)
							return r;
				}
				return ((recID > 0) && (recID < records.Count)) ? records [(int) recID] : null;
			}

			public override void InitSchema (JsonSchemaManager.Schema owner) {
				IDictionary pmore = new OrderedDictionary (), showIf = new OrderedDictionary ();
				base.InitSchema (owner);
				showIf [SchemaPropNamePrefix + "_au"] = new string [] { "Secure", "None", "" };
				pmore = new OrderedDictionary ();
				pmore ["default"] = "LDAP://OU=Departments,DC=global,DC=local"; // "WinNT://" + Environment.MachineName;
				AddSchemaProp (owner, "cs", "String", pmore);
				pmore = new OrderedDictionary ();
#if PEOPLEZEN
				pmore ["default"] = "(&(objectCategory=person)(objectClass=user))";
#endif
				AddSchemaProp (owner, "sq", "String", pmore);
				pmore = new OrderedDictionary ();
				pmore ["default"] = "Subtree";
				pmore ["enumtype"] = typeof (SearchScope).AssemblyQualifiedName;
				AddSchemaProp (owner, "ss", "EnumChoice", pmore);
				pmore = new OrderedDictionary ();
				pmore ["default"] = "None";
				pmore ["exclude"] = "Signing,Sealing,ServerBind,FastBind,ReadonlyServer";
				pmore ["enumtype"] = typeof (AuthenticationTypes).AssemblyQualifiedName;
				AddSchemaProp (owner, "au", "EnumChoice", pmore);
				pmore = new OrderedDictionary ();
				pmore ["show_if"] = showIf;
				AddSchemaProp (owner, "us", "String", pmore);
				pmore = new OrderedDictionary ();
				pmore ["is_password"] = true;
				pmore ["show_if"] = showIf;
				AddSchemaProp (owner, "pw", "String", pmore);
				InitSchemaForCaching (owner, false, true);
			}

			public override bool MoveNext () {
				if (recEnum != null)
					return recEnum.MoveNext ();
				clearRecs = true;
				return ((DirEnum != null) && DirEnum.MoveNext ());
			}

			public override void Reset () {
				if ((recEnum == null) && (records.Count > 0)) {
					recEnum = records.GetEnumerator ();
					recEnum.Reset ();
				}
			}

			public override string RewritePropertyName (string name) {
				return PropMappings.ContainsKey (name) ? PropMappings [name] : base.RewritePropertyName (name);
			}

			public override Guid ContextID {
				get {
					if (((contextID == null) || !contextID.HasValue) && (JsonInstance != null) && Guid.Empty.Equals (contextID = ProductPage.GetGuid (JsonInstance ["id"] + string.Empty, true)))
						contextID = new Guid ("ef0bd2b1-2c73-597f-b86c-1783eb216994");
					return contextID.HasValue ? contextID.Value : Guid.Empty;
				}
			}

			public override long Count {
				get {
					if ((recEnum == null) && (dirSearchResults != null))
						if (dirSearchResults.Count < 1)
							return -1;
						else
							return dirSearchResults.Count;
					return ((records.Count < 1) ? -1 : records.Count);
				}
			}

			public override object Current {
				get {
					Record rec;
					if (recEnum != null)
						return recEnum.Current;
					records.Add (rec = MakeRecord (DirEnum.Current, RequireCaching, records.Count + 1));
					return rec;
				}
			}

			public AuthenticationTypes DirAuth {
				get {
					if ((dirAuth == null) || !dirAuth.HasValue)
						try {
							dirAuth = (AuthenticationTypes) Enum.Parse (typeof (AuthenticationTypes), this ["au", "None"]);
						} catch {
							dirAuth = AuthenticationTypes.None;
						}
					return dirAuth.Value;
				}
			}

			public string DirAuthPass {
				get {
					return this ["pw", string.Empty];
				}
			}

			public string DirAuthUser {
				get {
					return this ["us", string.Empty];
				}
			}

			public string DirConn {
				get {
					return this ["cs", string.Empty];
				}
			}

			public IEnumerator DirEnum {
				get {
					if ((dirEnum == null) && (DirRoot != null)) {
						if (clearRecs) {
							records.Clear ();
							clearRecs = false;
						}
						if ((DirSearch != null) && ((dirSearchResults != null) || ((dirSearchResults = DirSearch.FindAll ()) != null)))
							dirEnum = dirSearchResults.GetEnumerator ();
						else
							dirEnum = DirRoot.Children.GetEnumerator ();
					}
					return dirEnum;
				}
			}

			public string DirQuery {
				get {
					return this ["sq", string.Empty];
				}
			}

			public DirectoryEntry DirRoot {
				get {
					if (dirRoot == null)
						dirRoot = MakeEntry (DirConn);
					return dirRoot;
				}
			}

			public SearchScope DirScope {
				get {
					if ((dirScope == null) || !dirScope.HasValue)
						try {
							dirScope = (SearchScope) Enum.Parse (typeof (SearchScope), this ["ss", "Subtree"]);
						} catch {
							dirScope = SearchScope.Subtree;
						}
					return dirScope.Value;
				}
			}

			public DirectorySearcher DirSearch {
				get {
					if ((dirSearch == null) && (!string.IsNullOrEmpty (DirQuery)) && (DirRoot != null))
						dirSearch = new DirectorySearcher (DirRoot, DirQuery, null, DirScope);
					return dirSearch;
				}
			}

			public override RecordPropertyCollection Properties {
				get {
					if (props == null) {
						props = new RecordPropertyCollection (this);
						foreach (object o in this)
							break;
					}
					EnsureUserFields (props);
					return props;
				}
			}

			public override string SchemaPropNamePrefix {
				get {
					return "ds";
				}
			}

		}

		#endregion

#if !PEOPLEZEN
		#region Dummy Class

		public class Dummy : DataSource {

			private RecordPropertyCollection propCol = null;
			private List<Record> recs = null;
			private IEnumerator enumerator = null;
			private ArrayList staticRecs = null;

			public override string GetPropertyDisplayName (RecordProperty prop) {
				string ln = prop.Property + string.Empty;
				string [] parts = ln.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				return ((parts.Length > 1) ? string.Join (":", parts, 1, parts.Length - 1) : ln);
			}

			public override RecordPropertyValueCollection GetPropertyValues (Record rec, RecordProperty prop) {
				string s = rec.Tags [prop.Name] + string.Empty;
				IDictionary obj;
				if (string.IsNullOrEmpty (s))
					if ((StaticRecs != null) && (StaticRecs.Count >= rec.RecordID)) {
						obj = StaticRecs [(int) rec.RecordID - 1] as IDictionary;
						rec.Tags [prop.Name] = s = ((obj == null) ? null : (obj [prop.Name] as string));
					} else
						rec.Tags [prop.Name] = s = ("Title".Equals (prop.Name) ? (ProductPage.GetResource ("PC_DataSources_t_" + GetType ().Name) + " #" + rec.RecordID) : (1.Equals (rnd.Next (0, 2)) ? ProductPage.GuidLower (Guid.NewGuid ()) : rnd.Next (1000, 1000 * 1000).ToString ()));
				return new RecordPropertyValueCollection (this, rec, prop, new object [] { s }, null, null, null);
			}

			public override Record GetRecord (long recID) {
				return Records [(int) recID - 1];
			}

			public override void InitSchema (JsonSchemaManager.Schema owner) {
				IDictionary pmore = new OrderedDictionary ();
				pmore ["lines"] = 4;
				pmore ["default"] = "ID\r\nTitle\r\nSampleColumn";
				pmore ["validator"] = "roxValidateNonEmpty";
				AddSchemaProp (owner, "f", "String", pmore);
				pmore = new OrderedDictionary ();
				pmore ["validator"] = "roxValidateNumeric";
				pmore ["default"] = "85";
				AddSchemaProp (owner, "c", "String", pmore);
				pmore = new OrderedDictionary ();
				pmore ["lines"] = "4";
				pmore ["validator"] = "roxValidateJson";
				AddSchemaProp (owner, "r", "String", pmore);
				pmore = new OrderedDictionary ();
				pmore ["default"] = "SampleColumn:" + ProductPage.GetResource ("PC_DataSources_t_" + GetType ().Name);
				AddSchemaProp (owner, SCHEMAPROP_DEFAULTFIELDS, "DataFields", "w", true, pmore);
				pmore = new OrderedDictionary ();
				pmore ["default"] = "[Title]";
				AddSchemaProp (owner, SCHEMAPROP_TITLEFIELD, "DataFields", "w", true, pmore);
				pmore = new OrderedDictionary ();
				AddSchemaProp (owner, SCHEMAPROP_PICTUREFIELD, "DataFields", "w", true, pmore);
				pmore = new OrderedDictionary ();
				AddSchemaProp (owner, SCHEMAPROP_URLFIELD, "DataFields", "w", true, pmore);
				InitSchemaForCaching (owner, false, false);
			}

			public override bool MoveNext () {
				return Enumerator.MoveNext ();
			}

			public override void Reset () {
				Enumerator.Reset ();
			}

			public override long Count {
				get {
					return Records.Count;
				}
			}

			public override object Current {
				get {
					return Enumerator.Current;
				}
			}

			public IEnumerator Enumerator {
				get {
					if (enumerator == null)
						enumerator = Records.GetEnumerator ();
					return enumerator;
				}
			}

			public override RecordPropertyCollection Properties {
				get {
					string fields;
					string [] parts;
					List<string> lines;
					if (propCol == null) {
						propCol = new RecordPropertyCollection (this);
						if (JsonInstance != null) {
							if (string.IsNullOrEmpty (fields = this ["f"] + string.Empty))
								for (int i = 0; i < rnd.Next (3, 8); i++)
									fields += ("\r\nField" + i + ":Field #" + (i + 1));
							if (((lines = new List<string> (fields.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))).Count > 0)) {
								for (int i = 0; i < lines.Count; i++)
									lines [i] = lines [i].Trim ();
								ProductPage.RemoveDuplicates<string> (lines);
								lines.Sort (StringComparer.InvariantCultureIgnoreCase);
								foreach (string ln in lines) {
									parts = ln.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries);
									propCol.Props.Add (new RecordProperty (this, (parts.Length > 1) ? parts [0] : ln, ln));
								}
							}
						}
					}
					return propCol;
				}
			}

			public List<Record> Records {
				get {
					int recCount;
					IDictionary dynRec;
					if (recs == null) {
						recs = new List<Record> ();
						if (JsonInstance != null) {
							if (StaticRecs != null)
								foreach (IDictionary dict in staticRecs)
									recs.Add (new Record (this, dict, null, Guid.Empty, recs.Count + 1));
							else {
								if ((!int.TryParse (this ["c", string.Empty], out recCount)) || (recCount <= 1))
									recCount = rnd.Next (2, 100);
								for (int i = 0; i < recCount; i++) {
									dynRec = new OrderedDictionary ();
									foreach (RecordProperty prop in Properties)
										dynRec [prop.Name] = prop.Name + " #" + (i + 1) + ": " + (1.Equals (rnd.Next (0, 2)) ? rnd.Next (2, int.MaxValue).ToString () : ProductPage.GuidLower (Guid.NewGuid ()));
									recs.Add (new Record (this, dynRec, null, Guid.Empty, recs.Count + 1));
								}
							}
						}
					}
					return recs;
				}
			}

			public override bool RequireCaching {
				get {
					return false;
				}
			}

			public override string SchemaPropNamePrefix {
				get {
					return "du";
				}
			}

			public ArrayList StaticRecs {
				get {
					string tmp;
					if (staticRecs == null) {
						staticRecs = new ArrayList ();
						if (JsonInstance != null)
							if (!string.IsNullOrEmpty (tmp = this ["r", string.Empty]))
								if ((staticRecs = JSON.JsonDecode (tmp) as ArrayList) == null)
									throw new FormatException ("Bad JSON syntax: " + ProductPage.GetResource ("PC_DataSources_" + SchemaPropNamePrefix + "_r"));
					}
					return ((staticRecs.Count == 0) ? null : staticRecs);
				}
			}

		}

		#endregion

		#region Hybrid Class

		public class Hybrid : DataSource {

			public override string SchemaPropNamePrefix {
				get {
					return "hy";
				}
			}

		}

		#endregion

		#region ListLocal Class

		public class ListLocal : DataSource {

			public override string SchemaPropNamePrefix {
				get {
					return "ll";
				}
			}

		}

		#endregion

		#region ListRemote Class

		public class ListRemote : DataSource {

			public override string SchemaPropNamePrefix {
				get {
					return "lr";
				}
			}

		}

		#endregion
#endif

		#region UserAccounts Class

		public class UserAccounts : UserDataSource {

			public static readonly Dictionary<string, string> PropMappings = new Dictionary<string, string> ();

			private static readonly Dictionary<DataSource.KnownProperty, string> knownMap = new Dictionary<DataSource.KnownProperty, string> ();

			public readonly SPSite SpSite;
			public readonly SPWeb Site;
			public readonly SPList Users;

			private readonly string [] hiddenFields = new string [] { "UserSelection", "NameWithPictureAndDetails", "NameWithPicture", "EditUser", "ContentTypeDisp" },
				includeFields = new string [] { "Title", "Name", "EMail", "MobilePhone", "Notes", "SipAddress", "Picture", "Department", "JobTitle" };
			private readonly Dictionary<string, SPField> fields = new Dictionary<string, SPField> ();

			private IEnumerator enumerator = null;
			private SPGroupCollection groupCol = null;
			private SPUserCollection userCol = null;
			private Record curRec = null;
			private RecordPropertyCollection propCol = null;
			private ArrayList combined = null;

			public static SPList GetUserList (SPWeb web) {
				SPList users = null;
				try {
					users = web.GetCatalog (SPListTemplateType.UserInformation);
				} catch {
				}
				if (users == null)
					users = web.Site.GetCatalog (SPListTemplateType.UserInformation);
				return users;
			}

			static UserAccounts () {
				PropMappings ["AccountName"] = "Name";
				PropMappings ["WorkEmail"] = "EMail";
				PropMappings ["PreferredName"] = "Title";
				PropMappings ["PictureURL"] = "Picture";
				PropMappings ["SPS-SipAddress"] = "SipAddress";
				PropMappings ["CellPhone"] = "MobilePhone";
				PropMappings ["Title"] = "JobTitle";
				PropMappings ["SPS-Location"] = "Office";
				PropMappings ["SPS-Responsibility"] = "SPSResponsibility";
				PropMappings ["AboutMe"] = "Notes";
#if PEOPLEZEN
				knownMap [DataSource.KnownProperty.Email] = "EMail";
				if (ProductPage.Is14)
					knownMap [DataSource.KnownProperty.FirstName] = "FirstName";
#endif
				knownMap [DataSource.KnownProperty.FriendlyName] = "Title";
#if PEOPLEZEN
				if (ProductPage.Is14)
					knownMap [DataSource.KnownProperty.LastName] = "LastName";
				knownMap [DataSource.KnownProperty.LoginName] = "Name";
#endif
				knownMap [DataSource.KnownProperty.Picture] = "Picture";
			}

			public UserAccounts ()
				: this (null) {
			}

			public UserAccounts (SPWeb site) {
				SPContext ctx;
				if ((site == null) && ((ctx = ProductPage.GetContext ()) != null))
					site = ctx.Web;
				SpSite = ((site == null) ? null : new SPSite (site.Site.ID));
				Site = ((SpSite == null) ? null : SpSite.OpenWeb (site.ID));
				if ((SpSite != null) && (Site != null)) {
					SpSite.CatchAccessDeniedException = SPSecurity.CatchAccessDeniedException = false;
					Users = GetUserList (Site);
					try {
						site.Site.CatchAccessDeniedException = false;
					} catch {
					}
				}
			}

			internal string Get (SPListItem item, string fieldName) {
				SPField field;
				if (!fields.TryGetValue (fieldName, out field))
					fields [fieldName] = field = ProductPage.GetField (Users, fieldName);
				return ((field != null) ? (string.Empty + item [field.InternalName]) : null);
			}

			internal bool Get (SPListItem item, string fieldName, bool defIfNull) {
				SPField field;
				if (!fields.TryGetValue (fieldName, out field))
					fields [fieldName] = field = ProductPage.GetField (Users, fieldName);
				return ((field != null) ? ((bool) item [field.InternalName]) : defIfNull);
			}

			public override bool AllowDateParsing (RecordProperty prop) {
				SPFieldCalculated calcField = prop.Property as SPFieldCalculated;
				return ((calcField == null) || (calcField.OutputType == SPFieldType.DateTime));
			}

			public override void Dispose () {
				if (!noDispose) {
					Site.Dispose ();
					SpSite.Dispose ();
					base.Dispose ();
				}
			}

			public override string GetFieldInfoUrl (string webUrl, Guid contextID) {
				return ProductPage.MergeUrlPaths (webUrl, "_layouts/listedit.aspx?List=" + contextID);
			}

			public override string GetKnownPropName (DataSource.KnownProperty kp) {
				return knownMap.ContainsKey (kp) ? knownMap [kp] : base.GetKnownPropName (kp);
			}

			public override string GetPropertyDisplayName (RecordProperty prop) {
				SPField field = prop.Property as SPField;
				return ((field != null) ? field.Title : base.GetPropertyDisplayName (prop));
			}

			public override RecordPropertyValueCollection GetPropertyValues (Record rec, RecordProperty prop) {
				object val;
				SPFieldUrl urlField;
				SPListItem listItem = rec.RelItem as SPListItem;
				SPField field = prop.Property as SPField;
				SPUser user = rec.MainItem as SPUser;
				List<object> vals;
				if (prop.Name == FIELDNAME_VCARDEXPORT)
					return new RecordPropertyValueCollection (this, rec, prop, new object [] { GetVcardExport (rec) }, null, null, null);
				else if ((prop.Name == FIELDNAME_SITEGROUPS) && (user != null)) {
					vals = new List<object> ();
					foreach (SPGroup group in ProductPage.TryEach<SPGroup> (user.Groups))
						try {
							vals.Add (group.Name);
						} catch {
						}
					return new RecordPropertyValueCollection (this, rec, prop, vals.ToArray (), null, null, null);
				} else if (prop.Name == "roxUserPersonalUrl")
					return new RecordPropertyValueCollection (this, rec, prop, new object [] { GetPersonalUrl (rec) }, null, null, null);
				else if (prop.Name == "roxUserPublicUrl")
					return new RecordPropertyValueCollection (this, rec, prop, new object [] { GetPublicUrl (rec) }, null, null, null);
				else
					if ((listItem != null) && (field != null)) {
						val = listItem [field.Id];
						if (val is DateTime)
							val = ((DateTime) val).ToUniversalTime ();
						if (((urlField = field as SPFieldUrl) != null) && (val is string))
							if (urlField.DisplayFormat == SPUrlFieldFormatType.Image)
								val = new SPFieldUrlValue (val as string).Url;
							else
								val = new SPFieldUrlValue (val as string);
						//if ((calcField = prop.Field as SPFieldCalculated) != null)
						//    val = ProductPage.ConvertCalcFieldValue (val, calcField.OutputType, false);
						return new RecordPropertyValueCollection (this, rec, prop, (val is object []) ? (val as object []) : ((val == null) ? new object [0] : new object [] { ((urlField != null) ? val : field.GetFieldValueAsHtml (val)) }), null, null, null);
					}
				return null;
			}

			public Record GetRecord (long recID, object obj) {
				SPListItem listItem = obj as SPListItem;
				SPUser user = obj as SPUser;
				SPGroup group = obj as SPGroup;
				if (recID <= 0)
					if (listItem != null)
						recID = listItem.ID;
					else if (user != null)
						recID = user.ID;
					else if (group != null)
						recID = group.ID;
				if ((recID > 0) && (listItem == null))
					try {
#if NET_3_0
						listItem = Users.GetItemById ((int) recID);
#else
						listItem = Users.GetItemByIdAllFields ((int) recID);
#endif
					} catch {
					}
				if ((recID > 0) && (user == null) && (UserCollection != null))
					try {
						user = UserCollection.GetByID ((int) recID);
					} catch {
					}
				if ((recID > 0) && (user == null) && (group == null) && (GroupCollection != null))
					try {
						group = GroupCollection.GetByID ((int) recID);
					} catch {
					}
				//if ((listItem != null) && ((Get (listItem, "ContentType") != "Person") || Get (listItem, "Deleted", false) || !Get (listItem, "IsActive", false)))
				//    listItem = null;
				if (user != null)
					return new Record (this, user, listItem, ((listItem != null) ? listItem.UniqueId : Guid.Empty), recID);
				if (group != null)
					return new Record (this, group, listItem, ((listItem != null) ? listItem.UniqueId : Guid.Empty), recID);
				return null;
			}

			public override Record GetRecord (long recID) {
				return GetRecord (recID, null);
			}

			public override void InitSchema (JsonSchemaManager.Schema owner) {
				string [] userChoices = new string [] { "a", "s", "u", "n" }, groupChoices = new string [] { "n", "g", "s" };
				base.InitSchema (owner);
				IDictionary pmore = new OrderedDictionary ();
				AddSchemaProp (owner, "u", new ArrayList (userChoices), pmore);
				AddSchemaProp (owner, "g", new ArrayList (groupChoices), pmore);
				InitSchemaForCaching (owner, false, true);
			}

			public override bool MoveNext () {
				curRec = null;
				return Enumerator.MoveNext ();
			}

			public override void Reset () {
				curRec = null;
				Enumerator.Reset ();
			}

			public override string RewritePropertyName (string name) {
				return PropMappings.ContainsKey (name) ? PropMappings [name] : base.RewritePropertyName (name);
			}

			public ArrayList CombinedCollection {
				get {
					if (combined == null) {
						combined = new ArrayList ();
						if (UserCollection != null)
							foreach (SPUser user in ProductPage.TryEach<SPUser> (UserCollection))
								combined.Add (user);
						if (GroupCollection != null)
							foreach (SPGroup group in ProductPage.TryEach<SPGroup> (GroupCollection))
								combined.Add (group);
					}
					return combined;
				}
			}

			public override Guid ContextID {
				get {
					return Users.ID;
				}
			}

			public override long Count {
				get {
					return (RequireCaching || (Users == null)) ? -1 : Users.ItemCount;
				}
			}

			public override object Current {
				get {
					object cur = Enumerator.Current;
					if ((curRec == null) && (cur != null))
						curRec = GetRecord (0, cur);
					return curRec;
				}
			}

			public bool EnumerateAllUsers {
				get {
					return ((inst == null) || (!inst.Contains (SchemaPropNamePrefix + "_u")) || "a".Equals (this ["u"]));
				}
			}

			public bool EnumerateSiteGroups {
				get {
					return "s".Equals (this ["g"]);
				}
			}

			public bool EnumerateSiteUsers {
				get {
					return "s".Equals (this ["u"]);
				}
			}

			public bool EnumerateWebGroups {
				get {
					return "g".Equals (this ["g"]);
				}
			}

			public bool EnumerateWebUsers {
				get {
					return "u".Equals (this ["u"]);
				}
			}

			public IEnumerator Enumerator {
				get {
					if (enumerator == null)
						enumerator = CombinedCollection.GetEnumerator ();
					return enumerator;
				}
			}

			public SPGroupCollection GroupCollection {
				get {
					if (groupCol == null) {
						if (EnumerateSiteGroups)
							groupCol = Site.SiteGroups;
						if (EnumerateWebGroups)
							groupCol = Site.Groups;
					}
					return groupCol;
				}
			}

			public override RecordPropertyCollection Properties {
				get {
					if (propCol == null) {
						propCol = new RecordPropertyCollection (this);
						foreach (SPField field in ProductPage.TryEach<SPField> (Users.Fields))
							if ((!(field.Hidden || field.FromBaseType || (Array.IndexOf<string> (hiddenFields, field.InternalName) >= 0))) || (Array.IndexOf<string> (includeFields, field.InternalName) >= 0))
								propCol.Props.Add (new RecordProperty (this, field.InternalName, field));
						EnsureUserFields (propCol);
						propCol.Props.Add (new RecordProperty (this, "roxUserPersonalUrl", null));
						propCol.Props.Add (new RecordProperty (this, "roxUserPublicUrl", null));
					}
					return propCol;
				}
			}

			public override string SchemaPropNamePrefix {
				get {
					return "ua";
				}
			}

			public SPUserCollection UserCollection {
				get {
					if (userCol == null) {
						if (EnumerateAllUsers)
							userCol = Site.AllUsers;
						if (EnumerateSiteUsers)
							userCol = Site.SiteUsers;
						if (EnumerateWebUsers)
							userCol = Site.Users;
					}
					return userCol;
				}
			}

		}

		#endregion

	}

	#region CachedRecord Class

	public class CachedRecord {

		public readonly Dictionary<string, List<object>> Values = new Dictionary<string, List<object>> ();
		public readonly Guid ID = Guid.Empty;
		public readonly long RecordID;
		public string Url;

		internal string friendlyName = string.Empty;

		public CachedRecord (Record rec, string [] props) {
			RecordID = rec.RecordID;
			ID = rec.ID;
			Resync (rec, props);
		}

		public string Get (string key, string defaultValue, DataSource ds) {
			string result = string.Join (DataSourceConsumer.cfgMultiPropJoin, DataSourceConsumer.Get (ds, this, null, key, defaultValue, false).ToArray ());
			return (ProductPage.LicEdition (ProductPage.GetContext (), DataSourceConsumer.L, 0) || (result == null) || (result.Length <= 3)) ? result : result.Substring (0, 3) + "...";
		}

		public void Resync (Record rec, string [] props) {
			string [] pair;
			string propName;
			RecordPropertyValueCollection vals;
			Exception err = null;
			List<string> propsDone = new List<string> (props);
			Url = rec.DataSource.GetRecordUri (rec);
			props = propsDone.ToArray ();
			propsDone.Clear ();
			foreach (string propLine in props)
				if (propLine != null) {
					err = null;
					vals = null;
					if (((pair = propLine.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pair.Length >= 1) && (!string.IsNullOrEmpty (propName = pair [0].Trim ())) && (!propsDone.Contains (propName))) {
						propsDone.Add (propName);
						Values [propName] = new List<object> ();
						try {
							vals = rec [propName];
						} catch (Exception ex) {
							err = ex;
						}
						if (err != null)
							Values [propName].Add (err.Message);
						else if (vals != null)
							if (vals.Count > 0)
								foreach (object obj in vals)
									Values [propName].Add (obj);
							else if (vals.Value != null)
								Values [propName].Add (vals.Value);
					}
				}
		}

		public string this [string key, string defaultValue, DataSource ds] {
			get {
				return Get (key, defaultValue, ds);
			}
		}

		public override string ToString () {
			return RecordID + ";#" + friendlyName;
		}

	}

	#endregion

	#region DataSource Class

	public abstract class DataSource : IEnumerable, IEnumerator, IDisposable {

		public enum KnownProperty {

#if PEOPLEZEN
			Email,
			FirstName,
#endif
			FriendlyName,
#if PEOPLEZEN
			LastName,
			LoginName,
#endif
			Picture

		}

		public const string SCHEMAPROP_ASMNAME = "roxority_Shared";
		public const string SCHEMAPROP_DEFAULTFIELDS = "pd";
		public const string SCHEMAPROP_PICTUREFIELD = "pp";
		public const string SCHEMAPROP_PREFIX = "rox___";
		public const string SCHEMAPROP_TITLEFIELD = "pt";
		public const string SCHEMAPROP_URLFIELD = "pu";

		protected internal static readonly Random rnd = new Random ();

		private static readonly Dictionary<Type, DataSource> statics = new Dictionary<Type, DataSource> ();

		protected internal bool noDispose = true;

		internal IDictionary inst;

		private Guid? contextID = null;
		private JsonSchemaManager.Schema schema;
		private HttpContext httpContext = null;

		public static DataSource FromID (string id, bool farmScope, bool siteScope, string typeName) {
			bool found = false;
			IDictionary inst = new OrderedDictionary ();
			KeyValuePair<JsonSchemaManager, JsonSchemaManager> kvp;
			if (string.IsNullOrEmpty (id))
				id = "default";
			inst ["id"] = id;
			using (ProductPage pp = new ProductPage ()) {
				kvp = JsonSchemaManager.TryGet (pp, null, farmScope, siteScope, SCHEMAPROP_ASMNAME);
				foreach (JsonSchemaManager schemaMan in new JsonSchemaManager [] { kvp.Value, kvp.Key })
					if (schemaMan != null)
						using (schemaMan)
							foreach (JsonSchemaManager.Schema schema in schemaMan.AllSchemas.Values)
								if ((schema != null) && (schema.Name == "DataSources")) {
									if ("new".Equals (id, StringComparison.InvariantCultureIgnoreCase))
										return DataSource.Load (schema, inst, typeName);
									foreach (IDictionary dict in schema.GetInstances (SPContext.Current.Web, null, null))
										if (found = ((dict != null) && (id.Equals (dict ["id"])))) {
											foreach (DictionaryEntry entry in dict)
												inst [entry.Key] = entry.Value;
											break;
										}
									if (found)
										return DataSource.Load (schema, inst, typeName);
								}
			}
			return null;
		}

		public static DataSource GetStatic (Type type, string typeName, ref Exception error) {
			DataSource dsp = null;
			if (type == null)
				try {
					type = ProductPage.Assembly.GetType ("roxority.Data.Providers." + typeName, false, true);
				} catch (Exception ex) {
					error = ex;
				}
			if ((type != null) && (!statics.TryGetValue (type, out dsp)) && ((dsp = New (type, ref error)) != null))
				statics [type] = dsp;
			return dsp;
		}

		public static DataSource Load (JsonSchemaManager.Schema schema, IDictionary inst, string typeName) {
			Type type = ProductPage.Assembly.GetType ("roxority.Data.Providers." + (string.IsNullOrEmpty (typeName) ? inst ["t"] : typeName), false, true);
			Exception ex = null;
			DataSource ds = ((type == null) ? null : New (type, ref ex));
			if (ds != null) {
				ds.schema = schema;
				ds.schema.ShouldSerialize = ShouldSerialize;
				ds.inst = inst;
			} else if (ex != null)
				throw ex;
			return ds;
		}

		public static DataSource New (Type type, ref Exception error) {
			try {
				return Reflector.Current.New (type.FullName) as DataSource;
			} catch (Exception ex) {
				error = ex;
				return null;
			}
		}

		public static bool ShouldSerialize (KeyValuePair<IDictionary, JsonSchemaManager.Property> kvp) {
			//string t;
			//int pos;
			//DataSource sds;
			//if (((pos = kvp.Value.Name.IndexOf ('_')) > 0) && (!string.IsNullOrEmpty (t = kvp.Key ["t"] + string.Empty)) && ((sds = GetStatic (null, t)) != null))
			//    return sds.SchemaPropNamePrefix.Equals (kvp.Value.Name.Substring (0, pos));
			return true;
		}

		public static IEnumerable<Type> KnownProviderTypes {
			get {
#if !PEOPLEZEN
				yield return typeof (Providers.Dummy);
				yield return typeof (Providers.ListLocal);
				yield return typeof (Providers.ListRemote);
#if WSS
#else
				yield return typeof (Providers.Bdc);
#endif
#endif
#if !WSS
				yield return typeof (Providers.UserProfiles);
#endif
				yield return typeof (Providers.UserAccounts);
#if PEOPLEZEN
#endif
				yield return typeof (Providers.Directory);
				yield return typeof (Providers.Ado);
#if !PEOPLEZEN
				yield return typeof (Providers.Hybrid);
#endif
			}
		}

		public static IEnumerable<string> SchemaProps {
			get {
				yield return DataSource.SCHEMAPROP_PICTUREFIELD;
				yield return DataSource.SCHEMAPROP_TITLEFIELD;
				yield return DataSource.SCHEMAPROP_URLFIELD;
				yield return UserDataSource.SCHEMAPROP_LOGINFIELD;
				yield return UserDataSource.SCHEMAPROP_MAILFIELD;
			}
		}

		protected IDictionary AddSchemaProp (JsonSchemaManager.Schema owner, string pname, object ptype, IDictionary pmore) {
			return AddSchemaProp (owner, pname, ptype, null, false, pmore);
		}

		protected IDictionary AddSchemaProp (JsonSchemaManager.Schema owner, string pname, object ptype, string tab, bool sharedRes, IDictionary pmore) {
			IDictionary ht = new OrderedDictionary (), showIf;
			if (string.IsNullOrEmpty (tab))
				tab = "t__" + GetType ().Name;
			ht ["type"] = ptype;
			ht ["tab"] = tab;
			ht ["show_in_summary"] = false;
			if ((showIf = pmore ["show_if"] as IDictionary) == null)
				showIf = new OrderedDictionary ();
			showIf ["t"] = GetType ().Name;
			ht ["show_if"] = showIf;
			if (sharedRes) {
				ht ["res_title"] = "PC_DataSources_" + pname;
				ht ["res_desc"] = "PD_DataSources_" + pname;
			}
			if (pmore != null)
				foreach (DictionaryEntry entry in pmore)
					ht [entry.Key] = entry.Value;
			owner.RawSchema [SchemaPropNamePrefix + "_" + pname] = ht;
			return ht;
		}

		public virtual bool AllowDateParsing (RecordProperty prop) {
			return false;
		}

		public virtual void Dispose () {
		}

		public void DoDispose () {
			noDispose = false;
			Dispose ();
		}

		public virtual string FixupTitle (string name) {
			return (DataSourceConsumer.L.expired || DataSourceConsumer.L.broken || DataSourceConsumer.L.userBroken) ? ProductPage.GetResource ("LicExpiry") : name;
		}

		public IEnumerator GetEnumerator () {
			return this;
		}

		public virtual string GetFieldInfoUrl (string webUrl, Guid contextID) {
			return ProductPage.MergeUrlPaths (webUrl, "_layouts/" + ProductPage.AssemblyName + "/default.aspx?cfg=tools&tool=Tool_DataSources&r=" + rnd.Next ());
		}

		public virtual string GetKnownPropName (DataSource.KnownProperty kp) {
			return string.Empty;
		}

		public string GetPropertyDisplayName (string name) {
			string predisp = ProductPage.GetResource ("Disp_" + name);
			RecordProperty rp = Properties.GetPropertyByName (name);
			return ((rp == null) ? (string.IsNullOrEmpty (predisp) ? name : predisp) : GetPropertyDisplayName (rp));
		}

		public virtual string GetPropertyDisplayName (RecordProperty prop) {
			string predisp = ProductPage.GetResource ("Disp_" + prop.Name);
			return string.IsNullOrEmpty (predisp) ? prop.Name : predisp;
		}

		public virtual RecordPropertyValueCollection GetPropertyValues (Record rec, RecordProperty prop) {
			return new RecordPropertyValueCollection (this, rec, prop, null, null, delegate () {
				return 0;
			}, delegate () {
				return null;
			});
		}

		public virtual Record GetRecord (long recID) {
			return null;
		}

		public virtual string GetRecordUri (Record rec) {
			return rec [SCHEMAPROP_PREFIX + DataSource.SCHEMAPROP_URLFIELD, string.Empty];
		}

		public virtual void InitSchema (JsonSchemaManager.Schema owner) {
			AddSchemaProp (owner, "s2", "String", "w", true, new OrderedDictionary ());
		}

		public virtual void InitSchemaForCaching (JsonSchemaManager.Schema owner, bool defVal, bool allowChange) {
			IDictionary pmore = new OrderedDictionary (), showIf = new OrderedDictionary ();
			showIf [SchemaPropNamePrefix + "_cc"] = "c";
			pmore ["default"] = defVal;
			pmore ["always_show_help"] = true;
			if (!allowChange)
				pmore ["readonly"] = true;
			AddSchemaProp (owner, "cc", new ArrayList (new string [] { defVal ? "c" : "n", defVal ? "n" : "c" }), "u", true, pmore);
			pmore.Clear ();
			pmore ["show_if"] = showIf;
			pmore ["validator"] = "roxValidateNumeric";
			pmore ["default"] = "10";
			pmore ["always_show_help"] = true;
			AddSchemaProp (owner, "cr", "String", "u", true, pmore);
			pmore.Clear ();
			pmore ["show_if"] = showIf;
			pmore ["always_show_help"] = true;
			AddSchemaProp (owner, "cn", "Boolean", "u", true, pmore);
			pmore.Clear ();
			pmore ["show_if"] = showIf;
			pmore ["always_show_help"] = true;
			AddSchemaProp (owner, "rc", "Boolean", "u", true, pmore);
			pmore.Clear ();
			pmore ["show_if"] = showIf;
			pmore ["always_show_help"] = true;
			AddSchemaProp (owner, "cl", "ClearCache", "u", true, pmore);
		}

		public virtual bool MoveNext () {
			return false;
		}

		public virtual void Reset () {
		}

		public virtual string RewritePropertyName (string name) {
			string kpn;
			foreach (string n in Enum.GetNames (typeof (DataSource.KnownProperty)))
				if ((n.Equals (name, StringComparison.InvariantCultureIgnoreCase)) && !string.IsNullOrEmpty (kpn = GetKnownPropName ((DataSource.KnownProperty) Enum.Parse (typeof (DataSource.KnownProperty), name, true))))
					return kpn;
			return name;
		}

		public int CacheRate {
			get {
				int cr;
				return (int.TryParse (this ["cr", string.Empty], out cr) && (cr >= 0)) ? cr : 10;
			}
		}

		public bool CacheRefresh {
			get {
				return this ["cn", false];
			}
		}

		public virtual Guid ContextID {
			get {
				if (((contextID == null) || !contextID.HasValue) && (JsonInstance != null))
					contextID = ProductPage.GetGuid (JsonInstance ["id"] + string.Empty, true);
				return contextID.Value;
			}
		}

		public virtual long Count {
			get {
				return RequireCaching ? -1 : 0;
			}
		}

		public virtual object Current {
			get {
				return null;
			}
		}

		public HttpContext HttpContext {
			get {
				if (httpContext == null)
					httpContext = HttpContext.Current;
				return httpContext;
			}
		}

		public bool IsReal {
			get {
				return (inst != null) && (schema != null);
			}
		}

		public object this [string name] {
			get {
				object val = null;
				string pname = SchemaPropNamePrefix + "_" + name;
				JsonSchemaManager.Property prop;
				if (inst != null)
					val = inst [pname];
				if ((val == null) && (schema != null) && ((prop = schema [pname]) != null))
					val = prop.DefaultValue;
				return val;
			}
		}

		public bool this [string name, bool defVal] {
			get {
				object obj = this [name];
				return ((obj is bool) ? (bool) obj : defVal);
			}
		}

		public string this [string name, string defVal] {
			get {
				object obj = this [name];
				return ((obj == null) ? defVal : obj.ToString ());
			}
		}

		public IDictionary JsonInstance {
			get {
				return inst;
			}
		}

		public JsonSchemaManager.Schema JsonSchema {
			get {
				return schema;
			}
		}

		public virtual RecordPropertyCollection Properties {
			get {
				return new RecordPropertyCollection (this);
			}
		}

		public bool Recache {
			get {
				return this ["rc", false];
			}
		}

		public virtual bool RequireCaching {
			get {
				return !"n".Equals (this ["cc", "n"]);
			}
		}

		public virtual string SchemaPropNamePrefix {
			get {
				Type t = GetType ();
				return (t == typeof (DataSource)) ? "dsp" : t.Name;
			}
		}

	}

	#endregion

	#region DataSourceCache Class

	public class DataSourceCache {

		internal bool caching = false;
		internal readonly List<CachedRecord> recordCache = new List<CachedRecord> ();
		internal int recacheHere = 0;
		internal readonly List<string> cachedProperties = new List<string> ();

		public void Clear () {
			recordCache.Clear ();
			recacheHere = 0;
			caching = false;
			cachedProperties.Clear ();
		}

	}

	#endregion

	#region DataSourceConsumer Class

	public class DataSourceConsumer : IDisposable {

		internal static string cfgMultiPropJoin = ProductPage.Config (ProductPage.GetContext (), "MultiPropJoin");
		internal static RecordPropertyCollection dataProps = null;
		internal static Dictionary<string, DataSourceCache> dsCaches = new Dictionary<string, DataSourceCache> ();

		[ThreadStatic]
		internal static ProductPage.LicInfo lic = null;

		public readonly bool DateThisYear = false, DateIgnoreDay = false;
		public readonly List<CachedRecord> List = new List<CachedRecord> ();
		public readonly int PageSize, PageStart;
		public readonly string Properties;
		public readonly DataSource DataSource;

		internal string [] cfgExcludePatterns = null;
		internal List<string> nameProps = null;
		internal readonly Dictionary<string, int> groupCounts = new Dictionary<string, int> ();
		internal int recCount = 0;
		internal long totalCount = 0;
		internal List<string> andFilters = new List<string> ();
		internal List<object []> filters = null;
		internal List<string> tabs = null;
		internal bool tabsReverse;
		internal string tabValue = null;
		internal List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> effectiveFilters = null;
		internal bool tabsReduced = false, tabsShowAll;
		internal DataSourceCache dsCache = null;

		private StringComparison comparison = StringComparison.InvariantCulture;

		internal static List<string> Get (DataSource ds, CachedRecord record, List<object> values, string propertyName, string defaultValue, bool longDateTime) {
			string result = defaultValue, tmp;
			DateTime dateValue;
			List<string> results;
			SPFieldUrlValue urlVal;
			RecordProperty dsProp = null;
			Record realRec;
			if (ds != null)
				dsProp = ds.Properties.GetPropertyByName (propertyName);
			if (values == null)
				record.Values.TryGetValue (propertyName, out values);
			if ((!propertyName.StartsWith (DataSource.SCHEMAPROP_PREFIX)) && (values == null) && (record.RecordID > 0) && ((realRec = ds.GetRecord (record.RecordID)) != null)) {
				record.Resync (realRec, new string [] { propertyName });
				record.Values.TryGetValue (propertyName, out values);
			}
			results = ((values == null) ? new List<string> () : new List<string> (values.Count));
			if (values != null)
				for (int i = 0; i < values.Count; i++)
					results.Add (((urlVal = values [i] as SPFieldUrlValue) != null) ? string.Format ("<a href=\"{0}\">{1}</a>", urlVal.Url, HttpUtility.HtmlEncode (urlVal.Description)) : (values [i] + string.Empty));
			if (!(propertyName.StartsWith ("{", StringComparison.InvariantCultureIgnoreCase) && propertyName.EndsWith ("}", StringComparison.InvariantCultureIgnoreCase) && propertyName.ToLowerInvariant ().Contains ("birthday")))
				if (longDateTime) {
					for (int i = 0; i < results.Count; i++)
						if (DateTime.TryParse (results [i], out dateValue))
							results [i] = dateValue.ToLongDateString ();
				} else if ((dsProp == null) || dsProp.AllowDateParsing)
					for (int i = 0; i < results.Count; i++)
						if (DateTime.TryParse (results [i], out dateValue))
							results [i] = dateValue.ToShortDateString ();
			if ((ds != null) && (dsProp == null) && propertyName.StartsWith (DataSource.SCHEMAPROP_PREFIX) && !string.IsNullOrEmpty (tmp = Record.GetSpecialFieldValue (ds, propertyName, delegate (string n) {
				return record [n, string.Empty, ds];
			})))
				results.Add (tmp);
			if ((results.Count == 0) && !string.IsNullOrEmpty (defaultValue))
				results.Add (defaultValue);
			return results;
		}

		internal static string GetTitle (DataSourceConsumer consumer, CachedRecord record) {
			string ln = string.Empty;
			if (string.IsNullOrEmpty (record.friendlyName) || consumer.DataSource.RequireCaching) {
				record.friendlyName = record [DataSource.SCHEMAPROP_PREFIX + DataSource.SCHEMAPROP_TITLEFIELD, string.Empty, consumer.DataSource];
				//if (consumer.nameProps == null) {
				//    string s = record [DataSource.SCHEMAPROP_PREFIX + DataSource.SCHEMAPROP_URLFIELD, string.Empty, consumer.DataSource];
				//    consumer.nameProps = new List<string> (ProductPage.Config (ProductPage.GetContext (), "NameOrder").Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
				//    for (int i = 0; i < consumer.nameProps.Count; i++)
				//        if ((string.IsNullOrEmpty ((consumer.nameProps [i] + string.Empty).Trim ())) || (consumer.nameProps.IndexOf (consumer.nameProps [i]) < i)) {
				//            consumer.nameProps.RemoveAt (i);
				//            i--;
				//        }
				//}
				//foreach (string line in consumer.nameProps) {
				//    record.friendlyName = string.Join (" ", new List<string> (line.Split (new char [] { ' ' }, StringSplitOptions.RemoveEmptyEntries)).ConvertAll<string> ((pn) => {
				//        return record [consumer.DataSource.RewritePropertyName (pn), string.Empty, consumer.DataSource];
				//    }).ToArray ()).Trim ();
				//    if (!string.IsNullOrEmpty (record.friendlyName))
				//        break;
				//}
			}
			return string.IsNullOrEmpty (record.friendlyName) ? ((record.RecordID > 0) ? ("#" + record.RecordID) : record.ID.ToString ()) : consumer.DataSource.FixupTitle (record.friendlyName);
		}

		internal static ProductPage.LicInfo L {
			get {
				if (lic == null)
					lic = ProductPage.LicInfo.Get (null);
				return lic;
			}
		}

		public DataSourceConsumer (int pageSize, int pageStart, bool dateThisYear, bool dateIgnoreDay, string properties, object sSortProp, object bSortDesc, string tabProp, string tv, object sGroupProp, object bGroupDesc, bool groupByCounts, bool groupShowCounts, SPWeb contextSite, string dsid, IDictionary dynInst, Hashtable fht, object l, List<Exception> sortErrors) {
			string tmp;
			UserDataSource uds;
			SPContext ctx = ProductPage.GetContext ();
			tabsReverse = ProductPage.Config<bool> (ctx, "FilterTabReverse");
			cfgMultiPropJoin = ProductPage.Config (ctx, "MultiPropJoin");
			DateThisYear = dateThisYear;
			DateIgnoreDay = dateIgnoreDay;
			PageSize = pageSize;
			PageStart = pageStart;
			Properties = properties;

			tabValue = tv;
			tabsShowAll = ProductPage.Config<bool> (ctx, "FilterTabShowAll");
			lic = l as ProductPage.LicInfo;
			if (fht != null) {
				if (((filters = fht ["f"] as List<object []>) == null) && (fht ["f"] is ICollection)) {
					filters = new List<object []> ();
					foreach (ArrayList x in (ICollection) fht ["f"])
						filters.Add (x.ToArray (typeof (object)) as object []);
				}
				if (fht ["fa"] is List<string>)
					andFilters = fht ["fa"] as List<string>;
				else if (fht ["fa"] is ICollection) {
					andFilters = new List<string> ();
					foreach (string x in (ICollection) fht ["fa"])
						andFilters.Add (x);
				}
			}
			if ((uds = (DataSource = DataSource.FromID (dsid, true, true, (dynInst == null) ? null : (dynInst ["t"] + string.Empty))) as UserDataSource) != null)
				cfgExcludePatterns = uds.ExcludePatterns;
			if (DataSource == null)
				throw new Exception (ProductPage.GetResource ("DataSourceNotFound", dsid));
			if ((dynInst != null) && (dynInst.Count > 0)) {
				if (DataSource.inst == null)
					DataSource.inst = new OrderedDictionary ();
				foreach (DictionaryEntry entry in dynInst)
					DataSource.inst [entry.Key] = entry.Value;
			}
			if ((!Guid.Empty.Equals (DataSource.ContextID)) && DataSource.RequireCaching && !dsCaches.TryGetValue (tmp = ProductPage.GuidLower (DataSource.ContextID, false), out dsCache))
				dsCaches [tmp] = dsCache = new DataSourceCache ();
			PopulateList (sSortProp, bSortDesc, sGroupProp, tabProp, bGroupDesc, groupByCounts, groupShowCounts, sortErrors);
		}

		internal bool GetDate (string dt, out DateTime dtVal) {
			bool parsed;
			if (parsed = DateTime.TryParse (dt, out dtVal))
				dtVal = GetDate (dtVal);
			return parsed;
		}

		internal DateTime GetDate (DateTime value) {
			if (DateThisYear && DateIgnoreDay)
				value = new DateTime (DateTime.Now.Year, value.Month, DateTime.Now.Day);
			else if (DateThisYear)
				value = new DateTime (DateTime.Now.Year, value.Month, value.Day);
			else if (DateIgnoreDay)
				value = new DateTime (value.Year, value.Month, DateTime.Now.Day);
			else
				value = new DateTime (value.Year, value.Month, value.Day);
			return value;
		}

		internal bool InFilter (DataSource ds, CachedRecord rec) {
			int i1, i2;
			string excl, accName = string.Empty;
			KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>> kvp;
			if (ds != null) {
				if ((rec != null) && (cfgExcludePatterns != null) && (cfgExcludePatterns.Length > 0)) {
					foreach (string v in DataSourceConsumer.Get (ds, rec, null, DataSource.SCHEMAPROP_PREFIX + UserDataSource.SCHEMAPROP_LOGINFIELD, string.Empty, false)) {
						accName = (string.Empty + v).ToLowerInvariant ();
						break;
					}
					if (string.IsNullOrEmpty (accName))
						foreach (string v in DataSourceConsumer.Get (ds, rec, null, ds.RewritePropertyName ("AccountName"), string.Empty, false)) {
							accName = (string.Empty + v).ToLowerInvariant ();
							break;
						}
					if (!string.IsNullOrEmpty (accName))
						foreach (string exclude in cfgExcludePatterns)
							if (!string.IsNullOrEmpty (exclude)) {
								excl = exclude.Trim ().ToLowerInvariant ();
								if (excl.StartsWith ("*") && excl.EndsWith ("*") && (accName.IndexOf (excl.Substring (1, excl.Length - 2)) >= 0))
									return false;
								else if (excl.StartsWith ("*") && accName.EndsWith (excl.Substring (1)))
									return false;
								else if (excl.EndsWith ("*") && accName.StartsWith (excl.Substring (0, excl.Length - 1)))
									return false;
								else if (accName.Equals (excl))
									return false;
							}
				}
				if ((filters != null) && (filters.Count > 0)) {
					if (effectiveFilters == null) {
						effectiveFilters = new List<KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>>> ();
						foreach (object [] f in filters)
							if ((f != null) && (f.Length >= 3)) {
								kvp = new KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>> (f [0] + string.Empty, new KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool> (new List<KeyValuePair<string, CamlOperator>> (), andFilters.Contains (f [0] + string.Empty)));
								i1 = i2 = -1;
								foreach (KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>> k in effectiveFilters) {
									i2++;
									if (string.Equals (k.Key, kvp.Key)) {
										i1 = i2;
										kvp = k;
										break;
									}
								}
								kvp.Value.Key.Add (new KeyValuePair<string, CamlOperator> (f [1] + string.Empty, (CamlOperator) ((f [2] is string) ? Enum.Parse (typeof (CamlOperator), f [2] + string.Empty, true) : f [2])));
								if (i1 >= 0)
									effectiveFilters [i1] = kvp;
								else
									effectiveFilters.Add (kvp);
							}
					}
					if (rec != null)
						foreach (KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>> filter in effectiveFilters)
							if ((filter.Value.Key != null) && (!string.IsNullOrEmpty (filter.Key)) && !IsMatch (DataSourceConsumer.Get (ds, rec, null, filter.Key, null, true), filter.Value.Key, filter.Value.Value))
								return false;
				}
			}
			return true;
		}

		internal bool InTab (CachedRecord rec, string tabProp, CamlOperator op, bool asDate) {
			int dv = 0;
			string tv = tabValue, tmp, tvn;
			DateTime dt;
			DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;
			if (tabs == null)
				return true;
			if (tv == null) {
				if (tabsShowAll)
					return true;
				tv = tabs [0];
				if (asDate) {
					if ((DateThisYear) && tabs.Contains (dtfi.MonthNames [DateTime.Now.Month - 1]))
						tv = dtfi.MonthNames [DateTime.Now.Month - 1];
				}
				tabValue = tv;
			}
			if (asDate && (tv.Length > 0))
				if (!DateThisYear)
					dv = int.Parse (tv);
				else
					foreach (string m in dtfi.MonthNames) {
						if (m.Equals (tv, StringComparison.InvariantCultureIgnoreCase))
							break;
						dv++;
					}
			if (rec.Values.ContainsKey (tabProp))
				if ((tv.Length == 0) && ((rec.Values [tabProp] == null) || (rec.Values [tabProp].Count == 0)))
					return true;
				else if (tv.Length > 0)
					foreach (object obj in rec.Values [tabProp]) {
						tmp = ProductPage.Normalize (obj + string.Empty);
						if (asDate) {
							if (!DateTime.TryParse (tmp, out dt))
								dt = DateTime.Parse (tmp, CultureInfo.CurrentUICulture, DateTimeStyles.AllowWhiteSpaces);
							if ((!DateThisYear) && dt.Year == dv)
								return true;
							else if (DateThisYear && dt.Month == (dv + 1))
								return true;
						} else {
							tvn = ProductPage.Normalize (tv);
							if ((op == CamlOperator.Eq) && tvn.Equals (tmp, StringComparison.InvariantCultureIgnoreCase))
								return true;
							else if ((op == CamlOperator.BeginsWith) && tmp.StartsWith (tvn, StringComparison.InvariantCultureIgnoreCase))
								return true;
							else if ((op == CamlOperator.Contains) && tmp.ToLowerInvariant ().Contains (tvn.ToLowerInvariant ()))
								return true;
							else if ((op == CamlOperator.Neq) && !tvn.Equals (tmp, StringComparison.InvariantCultureIgnoreCase))
								return true;
						}
					}
			return false;
		}

		internal bool IsMatch (List<string> pvals, List<KeyValuePair<string, CamlOperator>> vals, bool and) {
			bool match, wasMatch, between = (DateThisYear && (vals.Count == 2) && (((vals [0].Value == CamlOperator.Geq) || (vals [0].Value == CamlOperator.Gt)) && ((vals [1].Value == CamlOperator.Leq) || (vals [1].Value == CamlOperator.Lt))));
			List<bool> matches = new List<bool> ();
			decimal d1, d2;
			int index = 0;
			DateTime dt1, dt2, dt3, dt2Last = DateTime.MinValue;
			if (pvals.Count == 0)
				pvals.Add (string.Empty);
			foreach (string pval in pvals)
				if (pval != null) {
					wasMatch = false;
					foreach (KeyValuePair<string, CamlOperator> kvp in vals) {
						if ((DateTime.TryParse (pval, out dt1) || DateTime.TryParse (pval, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt1)) && (DateTime.TryParse (kvp.Key, out dt2) || DateTime.TryParse (kvp.Key, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt2))) {
							if (DateThisYear || DateIgnoreDay) {
								dt1 = new DateTime (DateThisYear ? (DateTime.Now.Year) : dt1.Year, dt1.Month, DateIgnoreDay ? DateTime.Now.Day : dt1.Day, dt1.Hour, dt1.Minute, dt1.Second, dt1.Millisecond);
								//if (between && (index == 0) && (DateTime.TryParse (vals [1].Key, out dt3) || DateTime.TryParse (vals [1].Key, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt3)) && (dt1 <= new DateTime (DateTime.Now.Year, dt3.Month, dt3.Day, dt3.Hour, dt3.Minute, dt3.Second, dt3.Millisecond)))
								//    dt1 = new DateTime (DateTime.Now.Year + 1, dt1.Month, dt1.Day, dt1.Hour, dt1.Minute, dt1.Second, dt1.Millisecond);
								dt2 = new DateTime (DateThisYear ? (DateTime.Now.Year) : dt2.Year, dt2.Month, DateIgnoreDay ? DateTime.Now.Day : dt2.Day, dt2.Hour, dt2.Minute, dt2.Second, dt2.Millisecond);
								//if (between && (index == 1) && (dt2 <= dt2Last))
								//    dt2 = new DateTime (DateTime.Now.Year + 1, dt2.Month, dt2.Day, dt2.Hour, dt2.Minute, dt2.Second, dt2.Millisecond);
								dt2Last = dt2;
							}
							if (kvp.Value == CamlOperator.Geq)
								match = (dt1 >= dt2);
							else if (kvp.Value == CamlOperator.Gt)
								match = (dt1 > dt2);
							else if (kvp.Value == CamlOperator.Leq)
								match = (dt1 <= dt2);
							else if (kvp.Value == CamlOperator.Lt)
								match = (dt1 < dt2);
							else if (kvp.Value == CamlOperator.Neq)
								match = (dt1 != dt2);
							else
								match = (dt1 == dt2);
						} else if ((decimal.TryParse (pval, out d1) || decimal.TryParse (pval, NumberStyles.Any, CultureInfo.InvariantCulture, out d1)) && (decimal.TryParse (kvp.Key, out d2) || decimal.TryParse (kvp.Key, NumberStyles.Any, CultureInfo.InvariantCulture, out d2))) {
							if (kvp.Value == CamlOperator.Geq)
								match = (d1 >= d2);
							else if (kvp.Value == CamlOperator.Gt)
								match = (d1 > d2);
							else if (kvp.Value == CamlOperator.Leq)
								match = (d1 <= d2);
							else if (kvp.Value == CamlOperator.Lt)
								match = (d1 < d2);
							else if (kvp.Value == CamlOperator.Neq)
								match = (d1 != d2);
							else
								match = (d1 == d2);
						} else if (kvp.Value == CamlOperator.BeginsWith)
							match = pval.StartsWith (kvp.Key, Comparison);
						else if (kvp.Value == CamlOperator.Contains)
							match = (pval.IndexOf (kvp.Key, Comparison) >= 0);
						else if (kvp.Value == CamlOperator.Neq)
							match = !pval.Equals (kvp.Key, Comparison);
						else
							match = pval.Equals (kvp.Key, Comparison);
						if (match)
							wasMatch = true;
						matches.Add (match);
						index++;
					}
					if (wasMatch)
						break;
				}
			return ((matches.Count > 0) && matches.Contains (true) && ((!and) || (!matches.Contains (false))));
		}

		internal void PopulateList (object sSortProp, object bSortDesc, object sGroupProp, string tabProp, object bGroupDesc, bool groupByCounts, bool groupShowCounts, List<Exception> sortErrors) {
			string [] pair, props, cacheProps;
			string groupProp = null, sortProp = null, propName, tmp = null;
			string sort2Prop = DataSource ["s2", ""];
			bool groupDesc = false, sortDesc = false, tabsAllNums = false, tabsAllDates = false, altEnumAddNulls = false, sort2Desc = (!string.IsNullOrEmpty (sort2Prop)) && sort2Prop.StartsWith ("-");
			long counter = 0;
			int tcount, gc, pos, cacheRate = DataSource.CacheRate, numTabs = 0, dtTabs = 0, tabThreshold = 16;
			decimal decTmp = -1;
			bool? isDt = null, isDec = null, isGrDt = null, isGrDec = null;
			Guid cid;
			DateTime recordDate = DateTime.MinValue;
			Record record;
			List<Guid> cachedIDs;
			List<string> uncachedProps = new List<string> (), tabsDone = null, zehProps = new List<string> ();
			List<CachedRecord> thisCache = null;
			CachedRecord crec;
			CamlOperator tabOp = CamlOperator.Eq;
			DateTimeFormatInfo dtfi = DateTimeFormatInfo.CurrentInfo;

			recCount = 0;
			totalCount = 0;

			if ((!int.TryParse (ProductPage.Config (null, "FilterTabThreshold"), out tabThreshold)) || (tabThreshold < 2) || (tabThreshold > 50))
				tabThreshold = 16;

			if (sSortProp is string)
				sortProp = sSortProp + string.Empty;
			if (sGroupProp is string)
				groupProp = sGroupProp + string.Empty;
			if (bSortDesc is bool)
				sortDesc = (bool) bSortDesc;
			if (bGroupDesc is bool)
				groupDesc = (bool) bGroupDesc;

			if (string.Equals (sortProp, groupProp, StringComparison.InvariantCultureIgnoreCase))
				sortProp = null;

			InFilter (DataSource, null);
			if (effectiveFilters != null)
				foreach (KeyValuePair<string, KeyValuePair<List<KeyValuePair<string, CamlOperator>>, bool>> kvp in effectiveFilters)
					zehProps.Add (kvp.Key);
			if ((!string.IsNullOrEmpty (sortProp)) && !"___roxRandomizedSort".Equals (sortProp, StringComparison.InvariantCultureIgnoreCase))
				zehProps.Add (sortProp);
			if (!string.IsNullOrEmpty (groupProp))
				zehProps.Add (groupProp);
			if (!string.IsNullOrEmpty (tabProp))
				zehProps.Add (tabProp);
			if ((DataSource is UserDataSource) && !string.IsNullOrEmpty (DataSource [UserDataSource.SCHEMAPROP_LOGINFIELD, string.Empty]))
				zehProps.Add (UserDataSource.FIELDNAME_SITEGROUPS);
#if PEOPLEZEN
			if (DataSource is UserDataSource)
				zehProps.Add (UserDataSource.FIELDNAME_VCARDEXPORT);
			if ((!string.IsNullOrEmpty (DataSource [UserDataSource.SCHEMAPROP_MAILFIELD, string.Empty])) && !string.IsNullOrEmpty (tmp = DataSource.GetKnownPropName (DataSource.KnownProperty.Email)))
				zehProps.Add (tmp);
			if (!string.IsNullOrEmpty (tmp = DataSource.GetKnownPropName (DataSource.KnownProperty.LoginName)))
				zehProps.Add (tmp);
#endif
			if (DataSource is UserDataSource)
				zehProps.Add (DataSource.RewritePropertyName ("FirstName"));
			if (DataSource is UserDataSource)
				zehProps.Add (DataSource.RewritePropertyName ("LastName"));
			foreach (string s in DataSource.SchemaProps)
				foreach (string n in RecordProperty.ExtractNames (s, false, DataSource))
					zehProps.Add (n);
			if (DataSource is UserDataSource)
				zehProps.Add (DataSource.RewritePropertyName ("UserName"));
			foreach (string p in Properties.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
				zehProps.Add (((pos = p.IndexOf (':')) > 0) ? p.Substring (0, pos) : p);
			tmp = string.Empty;
			ProductPage.RemoveDuplicates<string> (zehProps);
			props = zehProps.ToArray ();
			try {
				totalCount = DataSource.Count;
			} catch (UnauthorizedAccessException) {
				totalCount = -1;
			}
#if DEBUG
			//totalCount = 80;
#endif
			if ((!DataSource.RequireCaching) || (dsCache == null)) {
				thisCache = new List<CachedRecord> ();
				foreach (Record user in DataSource)
					if (user != null)
						thisCache.Add (new CachedRecord (user, props));
			} else if (!dsCache.caching)
				try {
					dsCache.caching = true;
					if ((((tcount = ((int) totalCount)) < 0) && ((dsCache.recordCache.Count == 0) || ((dsCache.recacheHere == 0) && DataSource.Recache))) || ((tcount >= 0) && (dsCache.recordCache.Count != tcount))) {
						Providers.Directory.cacheHelpers.Remove (DataSource.ContextID);
						dsCache.recordCache.Clear ();
						dsCache.recacheHere = 0;
						foreach (string propLine in props)
							if (((pair = propLine.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pair.Length >= 1) && (!dsCache.cachedProperties.Contains (propName = pair [0].Trim ())))
								dsCache.cachedProperties.Add (propName);
#if !WSS
						if ((DataSource is Providers.UserProfiles) && ProductPage.Config<bool> (null, "AltEnum") && (tcount >= 0)) {
							while (dsCache.recordCache.Count < tcount) {
								record = null;
								try {
									record = DataSource.GetRecord (counter);
								} catch (Exception ex) {
									if (("UserNotFoundException".Equals (ex.GetType ().Name)) && altEnumAddNulls)
										dsCache.recordCache.Add (null);
								}
								if (record != null)
									dsCache.recordCache.Add (new CachedRecord (record, props));
								if (counter >= int.MaxValue)
									break;
								counter++;
							}
						} else
#endif
						{
							cachedIDs = new List<Guid> ();
							foreach (Record up in DataSource)
								if ((up != null) && (!cachedIDs.Contains (cid = up.ID))) {
									cachedIDs.Add (cid);
									dsCache.recordCache.Add (new CachedRecord (up, props));
								}
							tcount = (int) (totalCount = dsCache.recordCache.Count);
						}
					} else {
						foreach (string propLine in props)
							if (((pair = propLine.Split (new char [] { ':' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pair.Length >= 1) && (!dsCache.cachedProperties.Contains (propName = pair [0].Trim ())) && !uncachedProps.Contains (propName))
								uncachedProps.Add (propName);

						if (filters != null)
							foreach (object [] obj in filters)
								if (!(dsCache.cachedProperties.Contains (propName = (obj [0] + string.Empty).Trim ()) || uncachedProps.Contains (propName)))
									uncachedProps.Add (propName);

						if (uncachedProps.Count > 0) {
							for (int cp = 0; cp < dsCache.recordCache.Count; cp++)
								if (dsCache.recordCache [cp] != null) {
									record = null;
									try {
										record = DataSource.GetRecord (dsCache.recordCache [cp].RecordID);
									} catch {
									}
									if (record != null)
										dsCache.recordCache [cp].Resync (record, uncachedProps.ToArray ());
								}
							dsCache.cachedProperties.AddRange (uncachedProps);
						} else if (cacheRate > 0) {
							cacheProps = dsCache.cachedProperties.ToArray ();
							for (int i = dsCache.recacheHere; ((i < (dsCache.recacheHere + cacheRate)) && (i < dsCache.recordCache.Count)); i++)
								if ((crec = dsCache.recordCache [i]) != null) {
									record = null;
									try {
										record = DataSource.GetRecord (crec.RecordID);
									} catch {
									}
									if (record != null)
										crec.Resync (record, cacheProps);
								}
							dsCache.recacheHere += cacheRate;
							if (dsCache.recacheHere >= dsCache.recordCache.Count)
								dsCache.recacheHere = 0;
						}
					}
				} finally {
					dsCache.caching = false;
				}
			if (!string.IsNullOrEmpty (tabProp)) {
				tabs = new List<string> ();
				tabsDone = new List<string> ();
				if ((!DataSource.RequireCaching) || (dsCache == null)) {
					for (int ri = 0; ri < thisCache.Count; ri++)
						if (!InFilter (DataSource, thisCache [ri])) {
							thisCache.RemoveAt (ri);
							ri--;
						} else
							PrepareTab (thisCache [ri], tabProp, tabsDone, ref dtTabs, ref numTabs, ref tmp, ref decTmp);
				} else {
					thisCache = new List<CachedRecord> ();
					for (int ri = 0; ri < dsCache.recordCache.Count; ri++)
						if (dsCache.recordCache [ri] != null) {
							try {
								if (InFilter (DataSource, dsCache.recordCache [ri])) {
									thisCache.Add (dsCache.recordCache [ri]);
									PrepareTab (dsCache.recordCache [ri], tabProp, tabsDone, ref dtTabs, ref numTabs, ref tmp, ref decTmp);
								}
							} catch {
							}
						}
				}
				tabsAllDates = ((dtTabs == tabs.Count) || ((dtTabs == (tabs.Count - 1)) && tabs.Contains (string.Empty)));
				tabsAllNums = ((numTabs == tabs.Count) || ((numTabs == (tabs.Count - 1)) && tabs.Contains (string.Empty)));
				if (tabs.Count > tabThreshold) {
					if (tabsAllDates) {
						for (int i = 0; i < tabs.Count; i++)
							if (tabs [i].Length > 0)
								if (DateThisYear)
									tabs [i] = DateTime.Parse (tabs [i], CultureInfo.CurrentUICulture, DateTimeStyles.AllowWhiteSpaces).Month + string.Empty;
								else
									tabs [i] = DateTime.Parse (tabs [i], CultureInfo.CurrentUICulture, DateTimeStyles.AllowWhiteSpaces).Year + string.Empty;
						for (int i = 0; i < tabs.Count; i++)
							if (tabs.IndexOf (tabs [i]) < i) {
								tabs.RemoveAt (i);
								i--;
							}
						tabs.Sort (delegate (string one, string two) {
							return ((one.Length == 0) ? two.CompareTo (one) : ((two.Length == 0) ? one.CompareTo (two) : long.Parse (one).CompareTo (long.Parse (two))));
						});
						if (tabsReverse)
							tabs.Reverse ();
						if (((pos = tabs.IndexOf (string.Empty)) >= 0) && (pos < (tabs.Count - 1))) {
							for (int i = pos; i < (tabs.Count - 1); i++)
								tabs [i] = tabs [i + 1];
							tabs [tabs.Count - 1] = string.Empty;
						}
						if (DateThisYear)
							for (int i = 0; i < tabs.Count; i++)
								if (tabs [i].Length > 0)
									tabs [i] = dtfi.MonthNames [int.Parse (tabs [i]) - 1];
					} else {
						tabsReduced = true;
						tabOp = CamlOperator.BeginsWith;
						for (int i = 0; i < tabs.Count; i++) {
							if (tabs [i].Length > 0)
								tabs [i] = tabs [i].Trim ().Substring (0, 1).ToUpperInvariant ();
							if (tabs.IndexOf (tabs [i]) < i) {
								tabs.RemoveAt (i);
								i--;
							}
						}
					}
				}
			}

			if ((tabs != null) && (tabs.Contains (string.Empty) && ProductPage.Config<bool> (null, "FilterTabHideEmpty")))
				tabs.Remove (string.Empty);

			if ((tabs != null) && (tabs.Count == 0))
				tabs = null;
			if (DataSource.RequireCaching && (dsCache != null) && ((tabs == null) || (thisCache == null)))
				thisCache = dsCache.recordCache;
			if ((tabs != null) && !tabsAllDates) {
				tabs.Sort ();
				if (tabsReverse)
					tabs.Reverse ();
				if (((pos = tabs.IndexOf (string.Empty)) >= 0) && (pos < (tabs.Count - 1))) {
					for (int i = pos; i < (tabs.Count - 1); i++)
						tabs [i] = tabs [i + 1];
					tabs [tabs.Count - 1] = string.Empty;
				}
			}

			for (int ri = 0; ri < thisCache.Count; ri++)
				if (thisCache [ri] != null)
					try {
						if ((dsCache == null) ? (string.IsNullOrEmpty (tabProp) ? InFilter (DataSource, thisCache [ri]) : InTab (thisCache [ri], tabProp, tabOp, tabsAllDates)) : ((thisCache != dsCache.recordCache) ? InTab (thisCache [ri], tabProp, tabOp, tabsAllDates) : InFilter (DataSource, thisCache [ri]))) {
							if ((!string.IsNullOrEmpty (sortProp)) || (!string.IsNullOrEmpty (groupProp)) || ((PageSize <= 0) || ((List.Count < PageSize) && (recCount >= PageStart)))) {
								List.Add (thisCache [ri]);
								if ((!string.IsNullOrEmpty (groupProp)) && (groupByCounts || groupShowCounts))
									groupCounts [thisCache [ri] [groupProp, string.Empty, DataSource]] = ((groupCounts.TryGetValue (thisCache [ri] [groupProp, string.Empty, DataSource], out gc)) ? (gc + 1) : 1);
							}
							recCount++;
						}
					} catch {
					}

			if (sort2Desc)
				sort2Prop = sort2Prop.Substring (1);
			while ((pos = List.IndexOf (null)) >= 0)
				List.RemoveAt (pos);

			if ("___roxRandomizedSort".Equals (sortProp))
				List.Sort (delegate (CachedRecord one, CachedRecord two) {
					if ((one == null) && (two == null))
						return 0;
					if (one == null)
						return int.MinValue;
					if (two == null)
						return int.MaxValue;
					if ((one.RecordID > 0) && (two.RecordID > 0) && one.RecordID.Equals (two.RecordID))
						return 0;
					if ((!Guid.Empty.Equals (one.ID)) && (!Guid.Empty.Equals (two.ID)) && one.ID.Equals (two.ID))
						return 0;
					return DataSource.rnd.Next (int.MinValue, int.MaxValue);
				});
			else
				List.Sort (delegate (CachedRecord one, CachedRecord two) {
					try {
						decimal d1, d2;
						int groupResult = 0, sortResult, temp;
						if ((one == null) && (two == null))
							return 0;
						if ((one != null) && (two == null))
							return 1;
						if ((one == null) && (two != null))
							return -1;
						if (DataSource == null)
							throw new ArgumentNullException ("Manager");
						string sortValueOne = ((string.IsNullOrEmpty (sortProp = (sortProp + string.Empty)) || (sortProp == "_rox_Name")) ? (GetTitle (this, one)) : (one [sortProp, string.Empty, DataSource])) + string.Empty;
						string sortValueTwo = ((string.IsNullOrEmpty (sortProp) || (sortProp == "_rox_Name")) ? (GetTitle (this, two)) : (two [sortProp, string.Empty, DataSource])) + string.Empty;
						string groupValueOne = string.IsNullOrEmpty (groupProp = (groupProp + string.Empty)) ? string.Empty : (one [groupProp, string.Empty, DataSource] + string.Empty);
						string groupValueTwo = string.IsNullOrEmpty (groupProp) ? string.Empty : (two [groupProp, string.Empty, DataSource] + string.Empty);
						DateTime dt1, dt2;
						if (sortValueOne == sortValueTwo) {
							if (string.IsNullOrEmpty (sort2Prop))
								sortResult = 0;
							else {
								sortValueOne = one [sort2Prop, string.Empty, DataSource] + string.Empty;
								sortValueTwo = two [sort2Prop, string.Empty, DataSource] + string.Empty;
								sortResult = sort2Desc ? sortValueTwo.CompareTo (sortValueOne) : sortValueOne.CompareTo (sortValueTwo);
							}
						} else {
							if (!(string.IsNullOrEmpty (sortValueOne) || string.IsNullOrEmpty (sortValueTwo))) {
								if ((isDt == null) || !isDt.HasValue)
									isDt = ((DateTime.TryParse (sortValueOne, out dt1) || DateTime.TryParse (sortValueOne, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt1)) && (DateTime.TryParse (sortValueTwo, out dt2) || DateTime.TryParse (sortValueTwo, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt2)));
								if ((isDec == null) || !isDec.HasValue)
									isDec = ((decimal.TryParse (sortValueOne, out d1) || decimal.TryParse (sortValueOne, NumberStyles.Any, CultureInfo.InvariantCulture, out d1)) && (decimal.TryParse (sortValueTwo, out d2) || decimal.TryParse (sortValueTwo, NumberStyles.Any, CultureInfo.InvariantCulture, out d2)));
							}
							if (((isDt == null) || (!isDt.HasValue) || isDt.Value) && (DateTime.TryParse (sortValueOne, out dt1) || DateTime.TryParse (sortValueOne, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt1)) && (DateTime.TryParse (sortValueTwo, out dt2) || DateTime.TryParse (sortValueTwo, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt2))) {
								isDt = true;
								sortResult = (sortDesc ? DateTime.Compare (GetDate (dt2), GetDate (dt1)) : DateTime.Compare (GetDate (dt1), GetDate (dt2)));
							} else if (((isDec == null) || (!isDec.HasValue) || isDec.Value) && (decimal.TryParse (sortValueOne, out d1) || decimal.TryParse (sortValueOne, NumberStyles.Any, CultureInfo.InvariantCulture, out d1)) && (decimal.TryParse (sortValueTwo, out d2) || decimal.TryParse (sortValueTwo, NumberStyles.Any, CultureInfo.InvariantCulture, out d2))) {
								isDec = true;
								sortResult = (sortDesc ? decimal.Compare (d2, d1) : decimal.Compare (d1, d2));
							} else {
								if ((isDt != null) && isDt.Value)
									isDt = string.IsNullOrEmpty (sortValueOne) || string.IsNullOrEmpty (sortValueTwo);
								if ((isDec != null) && isDec.Value)
									isDec = string.IsNullOrEmpty (sortValueOne) || string.IsNullOrEmpty (sortValueTwo);
								sortResult = ((sortDesc) ? (string.Compare (sortValueTwo, sortValueOne)) : (string.Compare (sortValueOne, sortValueTwo)));
							}
						}
						if (!string.IsNullOrEmpty (groupProp)) {
							if (groupValueTwo == groupValueOne)
								groupResult = 0;
							else {
								if (!(groupByCounts || string.IsNullOrEmpty (groupValueOne) || string.IsNullOrEmpty (groupValueTwo))) {
									if ((isGrDt == null) || !isGrDt.HasValue)
										isGrDt = ((DateTime.TryParse (groupValueOne, out dt1) || DateTime.TryParse (groupValueOne, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt1)) && (DateTime.TryParse (groupValueTwo, out dt2) || DateTime.TryParse (groupValueTwo, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt2)));
									if ((isGrDec == null) || !isGrDec.HasValue)
										isGrDec = ((decimal.TryParse (groupValueOne, out d1) || decimal.TryParse (groupValueOne, NumberStyles.Any, CultureInfo.InvariantCulture, out d1)) && (decimal.TryParse (groupValueTwo, out d2) || decimal.TryParse (groupValueTwo, NumberStyles.Any, CultureInfo.InvariantCulture, out d2)));
								}
								if (groupByCounts)
									groupResult = (groupCounts.TryGetValue (groupDesc ? groupValueTwo : groupValueOne, out temp) ? temp : 0).CompareTo (groupCounts.TryGetValue (groupDesc ? groupValueOne : groupValueTwo, out temp) ? temp : 0);
								else if (((isGrDt == null) || (!isGrDt.HasValue) || isGrDt.Value) && (DateTime.TryParse (groupValueOne, out dt1) || DateTime.TryParse (groupValueOne, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt1)) && (DateTime.TryParse (groupValueTwo, out dt2) || DateTime.TryParse (groupValueTwo, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dt2))) {
									isGrDt = true;
									groupResult = (groupDesc ? DateTime.Compare (GetDate (dt2), GetDate (dt1)) : DateTime.Compare (GetDate (dt1), GetDate (dt2)));
								} else if (((isGrDec == null) || (!isGrDec.HasValue) || isGrDec.Value) && (decimal.TryParse (groupValueOne, out d1) || decimal.TryParse (groupValueOne, NumberStyles.Any, CultureInfo.InvariantCulture, out d1)) && (decimal.TryParse (groupValueTwo, out d2) || decimal.TryParse (groupValueTwo, NumberStyles.Any, CultureInfo.InvariantCulture, out d2))) {
									isGrDec = true;
									groupResult = (groupDesc ? decimal.Compare (d2, d1) : decimal.Compare (d1, d2));
								} else {
									if ((isGrDt != null) && isGrDt.Value)
										isGrDt = string.IsNullOrEmpty (groupValueOne) || string.IsNullOrEmpty (groupValueTwo);
									if ((isGrDec != null) && isGrDec.Value)
										isGrDec = string.IsNullOrEmpty (groupValueOne) || string.IsNullOrEmpty (groupValueTwo);
									groupResult = ((groupDesc) ? (string.Compare (groupValueTwo, groupValueOne)) : (string.Compare (groupValueOne, groupValueTwo)));
								}
							}
						}
						return ((groupResult == 0) ? sortResult : groupResult);
					} catch (Exception ex) {
						sortErrors.Add (ex);
						throw;
					}
				});
			if ((PageSize > 0) && (List.Count > PageSize)) {
				if (PageStart > 0)
					List.RemoveRange (0, PageStart);
				if (List.Count > PageSize)
					List.RemoveRange (PageSize, List.Count - PageSize);
			}
			if (DataSource.CacheRefresh) {
				cacheProps = dsCache.cachedProperties.ToArray ();
				foreach (CachedRecord cachedRec in List) {
					try {
						record = DataSource.GetRecord (cachedRec.RecordID);
					} catch {
						record = null;
					}
					if (record != null)
						cachedRec.Resync (record, cacheProps);
				}
			}
		}

		internal void PrepareTab (CachedRecord rec, string tabProp, List<string> tabsDone, ref int dtTabs, ref int numTabs, ref string tmp, ref decimal decTmp) {
			bool isDt;
			DateTime dtTmp;
			if (rec.Values.ContainsKey (tabProp))
				if ((rec.Values [tabProp] == null) || (rec.Values [tabProp].Count == 0)) {
					if (!tabsDone.Contains (string.Empty)) {
						tabsDone.Add (string.Empty);
						tabs.Add (string.Empty);
					}
				} else
					for (int oc = 0; oc < rec.Values [tabProp].Count; oc++) {
						tmp = ProductPage.Normalize (rec.Values [tabProp] [oc] + string.Empty);
						if (isDt = (DateTime.TryParse (tmp, out dtTmp) || DateTime.TryParse (tmp, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeLocal, out dtTmp)))
							if (!(DateIgnoreDay || DateThisYear))
								tmp = dtTmp.ToShortDateString ();
							else
								tmp = dtTmp.ToString ((DateIgnoreDay && DateThisYear) ? "MMMM" : (DateIgnoreDay ? "MMMM yyyy" : "dd MMMM"));
						if (!tabsDone.Contains (tmp.ToLowerInvariant ())) {
							tabsDone.Add (tmp.ToLowerInvariant ());
							tabs.Add (tmp);
							if (isDt)
								dtTabs++;
							if (decimal.TryParse (tmp, out decTmp) || decimal.TryParse (tmp, NumberStyles.Any, CultureInfo.InvariantCulture, out decTmp))
								numTabs++;
						}
					}
		}

		public virtual void Dispose () {
			DataSource.DoDispose ();
		}

		public StringComparison Comparison {
			get {
				string cfg;
				if (comparison == StringComparison.InvariantCulture) {
					comparison = StringComparison.InvariantCultureIgnoreCase;
					if (!string.IsNullOrEmpty (cfg = ProductPage.Config (ProductPage.GetContext (), "Compare")))
						try {
							comparison = (StringComparison) Enum.Parse (typeof (StringComparison), cfg, true);
						} catch {
						}
				}
				return comparison;
			}
		}

	}

	#endregion

	#region DataSourceSchemaExtender Class

	public class DataSourceSchemaExtender : JsonSchemaManager.ISchemaExtender {

		public void InitSchema (JsonSchemaManager.Schema owner) {
			DataSource dsp;
			Exception ex = null;
			foreach (Type dspType in DataSource.KnownProviderTypes)
				if ((dsp = DataSource.GetStatic (dspType, null, ref ex)) != null)
					dsp.InitSchema (owner);
		}

	}

	#endregion

	#region Record Class

	public class Record {

		public const string HTML_PREFIX = "<roxhtml/>";

		public readonly DataSource DataSource;
		public readonly object MainItem, RelItem;
		public readonly IDictionary Tags = new OrderedDictionary ();
		public readonly Guid ID  = Guid.Empty;
		public readonly long RecordID = -1;

		private Dictionary<string, RecordPropertyValueCollection> propVals = new Dictionary<string, RecordPropertyValueCollection> ();

		public static string GetSpecialFieldValue (DataSource ds, string name, Converter<string,string>getSourceValue) {
			string tmp, theVal = null;
			string [] lines;
			int lastPos, endPos;
			List<string> names;
			if ((!string.IsNullOrEmpty (tmp = ds [name.Substring (DataSource.SCHEMAPROP_PREFIX.Length), string.Empty])) && ((lines = tmp.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Length > 0))
				foreach (string ln in lines) {
					lastPos = -1;
					tmp = ln;
					names = new List<string> ();
					while ((lastPos < (lastPos = ln.IndexOf ('[', lastPos + 1))) && ((endPos = ln.IndexOf (']', lastPos + 1)) > lastPos))
						names.Add (ln.Substring (lastPos + 1, endPos - lastPos - 1));
					foreach (string n in names)
						tmp = tmp.Replace ("[" + n + "]", getSourceValue (n));
					if (!string.IsNullOrEmpty (tmp = tmp.Trim ())) {
						theVal = tmp;
						break;
					}
				}
			return theVal;
		}

		public Record (DataSource dataSource, object item, object relItem, Guid guid, long recID) {
			string tmp;
			MainItem = item;
			RelItem = relItem;
			DataSource = dataSource;
			RecordID = recID;
			if (Guid.Empty.Equals (ID = guid))
				ID = ProductPage.GetGuid (new string ('0', 32 - (tmp = RecordID.ToString ()).Length) + tmp, true);
		}

		public RecordPropertyValueCollection this [string name] {
			get {
				string theVal, tmplContent = string.Empty, tmp;
				string [] templateNameParts;
				int pos, pos2;
				long l;
				bool found = false;
				Record rec;
				RecordPropertyValueCollection pv;
				RecordProperty rp;
				UserDataSource uds;
				IDictionary template = null;
				IEnumerable<IDictionary> templates;
				UserDataSource profDataSource = DataSource as UserDataSource;
				if (!propVals.TryGetValue (name, out pv)) {
					if (UserDataSource.FIELDNAME_VCARDEXPORT.Equals (name) && ((uds = DataSource as UserDataSource) != null))
						propVals [name] = pv = new RecordPropertyValueCollection (DataSource, this, null, new object [] { "<span class=\"rox-vcard\">" + string.Format (ProductPage.Config (ProductPage.GetContext (), "VcardPropFormat"), UserDataSource.GetVcardExport (this), '{', '}') + "</span>" }, null, null, null);
					else if (name.StartsWith ("{", StringComparison.InvariantCultureIgnoreCase) && name.EndsWith ("}", StringComparison.InvariantCultureIgnoreCase) && (DataSource != null) && (DataSource.JsonSchema != null) && (DataSource.JsonSchema.Owner != null) && ((templateNameParts = name.Substring (1, name.Length - 2).Trim ().Split (new string [] { ";#" }, StringSplitOptions.RemoveEmptyEntries)) != null) && (templateNameParts.Length >= 1) && ((templates = JsonSchemaManager.GetInstances (DataSource.JsonSchema.Owner.ProdPage, null, "DataFieldFormats", null, null, null, true, true, false, "roxority_Shared")) != null)) {
						foreach (IDictionary tmpl in templates)
							if ((tmpl != null) && (templateNameParts [0].Equals (tmpl ["name"]) || templateNameParts [0].Equals (tmpl ["id"]))) {
								template = tmpl;
								break;
							}
						if ((template == null) || string.IsNullOrEmpty (tmplContent = template ["t"] + string.Empty)) {
							if (templateNameParts [0].Contains ("_"))
								for (int i = 1; i < templateNameParts.Length; i++) {
									foreach (string fn in RecordProperty.ExtractNames (templateNameParts [i], true, DataSource))
										templateNameParts [i] = templateNameParts [i].Replace ("[" + fn + "]", this [fn].Value + string.Empty);
									while (((pos = templateNameParts [i].IndexOf ('{')) >= 0) && ((pos2 = templateNameParts [i].IndexOf ('}', pos + 1)) > pos))
										templateNameParts [i] = templateNameParts [i].Replace (tmp = templateNameParts [i].Substring (pos, pos2 - pos + 1), this [tmp].Value + string.Empty);
								}
							if ((!found) && (found = (templateNameParts [0] == "DateTime_FromBinary")) && (templateNameParts.Length > 1) && long.TryParse (templateNameParts [1], out l))
								tmplContent = DateTime.FromBinary (l).ToShortDateString ();
							else if ((!found) && (found = (templateNameParts [0] == "DateTime_FromDdMm")) && (templateNameParts.Length > 1) && (templateNameParts [1].Length == 4) && long.TryParse (templateNameParts [1], out l))
								tmplContent = new DateTime (DateTime.Today.Year, int.Parse (templateNameParts [1].Substring (2)), int.Parse (templateNameParts [1].Substring (0, 2))).ToString (((templateNameParts.Length > 2) ? templateNameParts [2] : "MMMM dd"), CultureInfo.CurrentUICulture);
							else if ((!found) && (found = (templateNameParts [0] == "DateTime_FromFileTime")) && (templateNameParts.Length > 1) && long.TryParse (templateNameParts [1], out l))
								tmplContent = DateTime.FromFileTime (l).ToShortDateString ();
							else if ((!found) && (found = (templateNameParts [0] == "DateTime_FromFileTimeUtc")) && (templateNameParts.Length > 1) && long.TryParse (templateNameParts [1], out l))
								tmplContent = DateTime.FromFileTimeUtc (l).ToShortDateString ();
							else if ((!found) && (found = (templateNameParts [0] == "DateTime_FromTicks")) && (templateNameParts.Length > 1) && long.TryParse (templateNameParts [1], out l))
								tmplContent = new DateTime (l).ToShortDateString ();
							else if ((!found) && (found = (templateNameParts [0] == "String_Replace")) && (templateNameParts.Length > 3))
								tmplContent = templateNameParts [1].Replace (templateNameParts [2], templateNameParts [3]);
							else if ((!found) && (found = (templateNameParts [0] == "UserProfiles_PropertyValue")) && (templateNameParts.Length > 3) && (profDataSource != null))
								tmplContent = profDataSource.GetRecordVal (templateNameParts [1], templateNameParts [2], templateNameParts [3]);
							else if ((!found) && (found = (templateNameParts [0] == "UserProfiles_CurrentUser")) && (profDataSource != null))
								tmplContent = ProductPage.LoginName (SPContext.Current.Web.CurrentUser.LoginName);
							if (!found)
								tmplContent = ProductPage.GetResource ("Tool_DataSources_UnFormat", templateNameParts [0]);
						} else {
							for (int i = 1; i < templateNameParts.Length; i++)
								tmplContent = tmplContent.Replace ("[" + i + "]", this [templateNameParts [i]].Value + string.Empty);
							foreach (string fn in RecordProperty.ExtractNames (tmplContent, true, DataSource))
								tmplContent = tmplContent.Replace ("[" + fn + "]", this [fn].Value + string.Empty);
							while (((pos = tmplContent.IndexOf ('{')) >= 0) && ((pos2 = tmplContent.IndexOf ('}', pos + 1)) > pos))
								tmplContent = tmplContent.Replace (tmp = tmplContent.Substring (pos, pos2 - pos + 1), this [tmp].Value + string.Empty);
							if ("h".Equals (template ["m"]))
								tmplContent = HTML_PREFIX + tmplContent;
						}
						propVals [name] = pv = new RecordPropertyValueCollection (DataSource, this, null, new object [] { tmplContent }, null, null, null);
					} else if (!name.StartsWith (DataSource.SCHEMAPROP_PREFIX)) {
						if ((rp = DataSource.Properties.GetPropertyByName (name)) == null)
							throw new Exception (ProductPage.GetResource ("Tool_DataSources_UnField", name, JsonSchemaManager.GetDisplayName (DataSource.JsonInstance, "DataSources", false)));
						else
							propVals [name] = pv = DataSource.GetPropertyValues (this, rp);
					} else {
						propVals [name] = pv = new RecordPropertyValueCollection (DataSource, this, null, ((theVal = GetSpecialFieldValue (DataSource, name, delegate (string n) {
							return this [n, string.Empty];
						})) == null) ? new object [0] : new object [] { theVal }, null, null, null);
					}
				}
				return pv;
			}
		}

		public string this [string name, string defVal] {
			get {
				RecordPropertyValueCollection pv = this [name];
				return ((pv == null) ? defVal : ((pv.Value == null) ? defVal : pv.Value.ToString ()));
			}
		}

	}

	#endregion

	#region RecordProperty Class

	public class RecordProperty {

		#region Comparer Class

		internal class Comparer : IComparer<string> {

			public readonly DataSource DataSource;

			public Comparer (DataSource dataSource) {
				DataSource = dataSource;
			}

			public int Compare (string x, string y) {
				string tmp;
				RecordProperty prop;
				if (x.Equals (y))
					return 0;
#if PEOPLEZEN
				else if (x == DataSource.GetKnownPropName (DataSource.KnownProperty.Email))
					return "a".CompareTo ("b");
				else if (y == DataSource.GetKnownPropName (DataSource.KnownProperty.Email))
					return "b".CompareTo ("a");
#endif
				//else if (manager != null)
				//    return managerProps.GetPropertyByName (x).DisplayName.CompareTo (managerProps.GetPropertyByName (y).DisplayName);
				if (!string.IsNullOrEmpty (tmp = ProductPage.GetResource ("Disp_" + x)))
					x = tmp;
				if (!string.IsNullOrEmpty (tmp = ProductPage.GetResource ("Disp_" + y)))
					y = tmp;
				if (DataSource != null) {
					if (((prop = DataSourceConsumer.dataProps.GetPropertyByName (x)) != null) && !string.IsNullOrEmpty (tmp = prop.DisplayName))
						x = tmp;
					if (((prop = DataSourceConsumer.dataProps.GetPropertyByName (y)) != null) && !string.IsNullOrEmpty (tmp = prop.DisplayName))
						y = tmp;
				}
				return x.CompareTo (y);
			}

		}

		#endregion

		public readonly DataSource DataSource;
		public readonly object Property = null;

		public readonly string Name = null;

		private bool? allowDateParsing = null;

		public static IEnumerable<string> ExtractNames (string name, bool isVal, DataSource ds) {
			string tmp;
			string [] lines;
			int lastPos, endPos;
			List<string> names = new List<string> ();
			if ((!string.IsNullOrEmpty (tmp = (isVal ? name : ds [name.Substring (name.StartsWith (DataSource.SCHEMAPROP_PREFIX) ? DataSource.SCHEMAPROP_PREFIX.Length : 0), string.Empty]))) && ((lines = tmp.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)).Length > 0))
				foreach (string ln in lines) {
					lastPos = -1;
					while ((lastPos < (lastPos = ln.IndexOf ('[', lastPos + 1))) && ((endPos = ln.IndexOf (']', lastPos + 1)) > lastPos))
						if (!names.Contains (tmp = ln.Substring (lastPos + 1, endPos - lastPos - 1)))
							names.Add (tmp);
				}
			return names;
		}

		public RecordProperty (DataSource dataSource, string name, object property) {
			DataSource = dataSource;
			Name = name;
			Property = property;
		}

		public bool AllowDateParsing {
			get {
				if ((allowDateParsing == null) || !allowDateParsing.HasValue)
					allowDateParsing = DataSource.AllowDateParsing (this);
				return allowDateParsing.Value;
			}
		}

		public string DisplayName {
			get {
				return DataSource.GetPropertyDisplayName (this);
			}
		}

	}

	#endregion

	#region RecordPropertyCollection Class

	public class RecordPropertyCollection : IEnumerable {

		public readonly DataSource DataSource;
		public readonly List<RecordProperty> Props = new List<RecordProperty> ();

		private SortedDictionary<string, string> dict = null;

		public RecordPropertyCollection (DataSource dataSource) {
			DataSource = dataSource;
		}

		public IEnumerator GetEnumerator () {
			return Props.GetEnumerator ();
		}

		public RecordProperty GetPropertyByName (string name) {
			string altName = DataSource.RewritePropertyName (name);
			RecordProperty p = Props.Find ((prop) => {
				return (prop.Name == name);
			});
			if (p == null)
				p = Props.Find ((prop) => {
					return (prop.Name == altName);
				});
			return p;
		}

		public SortedDictionary<string, string> Dict {
			get {
				string title;
				List<string> prevTitles;
				if (dict == null) {
					prevTitles = new List<string> ();
					dict = new SortedDictionary<string, string> ();
					foreach (RecordProperty prop in Props) {
						if (!prevTitles.Contains (title = prop.DisplayName))
							prevTitles.Add (prop.DisplayName);
						else if (prop.Name != title)
							title += (" [" + prop.Name + "]");
						dict [prop.Name] = title;
					}
				}
				return dict;
			}
		}

		public OrderedDictionary SortedByName {
			get {
				OrderedDictionary dict = new OrderedDictionary ();
				List<RecordProperty> props = new List<RecordProperty> (Props);
				props.Sort (delegate (RecordProperty one, RecordProperty two) {
					int c = one.Name.CompareTo (two.Name);
					return ((c == 0) ? one.DisplayName.CompareTo (two.DisplayName) : c);
				});
				foreach (RecordProperty rp in props)
					dict.Add (rp.Name, rp.DisplayName);
				return dict;
			}
		}

		public OrderedDictionary SortedByTitle {
			get {
				OrderedDictionary dict = new OrderedDictionary ();
				List<RecordProperty> props = new List<RecordProperty> (Props);
				props.Sort (delegate (RecordProperty one, RecordProperty two) {
					int c = one.DisplayName.CompareTo (two.DisplayName);
					return ((c == 0) ? one.Name.CompareTo (two.Name) : c);
				});
				foreach (RecordProperty rp in props)
					dict.Add (rp.Name, rp.DisplayName);
				return dict;
			}
		}

	}

	#endregion

	#region RecordPropertyValueCollection Class

	public class RecordPropertyValueCollection : IEnumerable {

		public roxority.SharePoint.Func<int> GetCountDelegate = null;
		public roxority.SharePoint.Func<object> GetValueDelegate = null;

		public IEnumerator Enum = null;
		public readonly object [] Values = null;

		public readonly DataSource DataSource;
		public readonly Record Record;
		public readonly RecordProperty RecordProperty;

		public RecordPropertyValueCollection (DataSource dataSource, Record record, RecordProperty prop, object [] values, IEnumerator enumerator, roxority.SharePoint.Func<int> getCount, roxority.SharePoint.Func<object> getValue) {
			DataSource = dataSource;
			Record = record;
			RecordProperty = prop;
			GetCountDelegate = getCount;
			GetValueDelegate = getValue;
			if ((Values = values) != null)
				Enum = Values.GetEnumerator ();
			else if (enumerator != null)
				Enum = enumerator;
		}

		public IEnumerator GetEnumerator () {
			return Enum;
		}

		public int Count {
			get {
				if (GetCountDelegate != null)
					return GetCountDelegate ();
				return ((Values == null) ? 0 : Values.Length);
			}
		}

		public object Value {
			get {
				if (GetValueDelegate != null)
					return GetValueDelegate ();
				return ((Values == null) ? null : ((Values.Length == 1) ? Values [0] : ((Values.Length == 0) ? null : Values)));
			}
		}

	}

	#endregion

	#region UserDataSource Class

	public abstract class UserDataSource : DataSource {

		public const string FIELDNAME_SITEGROUPS = "roxSiteGroups",
			FIELDNAME_VCARDEXPORT = "roxVcardExport",
			SCHEMAPROP_MAILFIELD = "pm",
			SCHEMAPROP_LOGINFIELD = "pl";

		private string [] exclPatterns = null;

		public static string GetVcardExport (string dsid, int recID, Guid guid) {
			string vcard = "BEGIN:VCARD\r\nVERSION:3.0\r\n", template, tmp, tmpVal;
			int lastPos, endPos;
			List<string> names = new List<string> ();
			Record user = null;
			using (DataSource ds = DataSource.FromID (dsid, true, true, null)) {
				if (recID > 0)
					user = ds.GetRecord (recID);
				if ((!Guid.Empty.Equals (guid)) && ((user == null) || (user.ID != guid)))
					foreach (Record r in ds)
						if (r.ID == guid) {
							user = r;
							break;
						}
				if ((user != null) && !string.IsNullOrEmpty (template = ds ["vc", string.Empty].Trim ('\r', '\n'))) {
					foreach (string ln in template.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) {
						lastPos = -1;
						tmp = ln;
						names.Clear ();
						while ((lastPos < (lastPos = ln.IndexOf ('[', lastPos + 1))) && ((endPos = ln.IndexOf (']', lastPos + 1)) > lastPos))
							names.Add (ln.Substring (lastPos + 1, endPos - lastPos - 1));
						foreach (string n in names) {
							try {
								tmpVal = user [n, string.Empty];
							} catch (Exception ex) {
								tmpVal = ex.Message;
							}
							if ((n == SCHEMAPROP_PREFIX + SCHEMAPROP_PICTUREFIELD) && (!tmpVal.StartsWith ("http:", StringComparison.InvariantCultureIgnoreCase)) && !tmpVal.StartsWith ("https:", StringComparison.InvariantCultureIgnoreCase))
								tmpVal = ProductPage.MergeUrlPaths (SPContext.Current.Web.Url, tmpVal);
							tmp = tmp.Replace ("[" + n + "]", tmpVal);
						}
						vcard += tmp + "\r\n";
					}
				}
			}
			vcard += "REV:" + DateTime.Now.ToString ("yyyyMMdd'T'HHmmss'Z'", CultureInfo.InvariantCulture) + "\r\nEND:VCARD";
			return vcard;
		}

		public static string GetVcardExport (Record user) {
			return ProductPage.MergeUrlPaths (SPContext.Current.Web.Url, "/_layouts/" + ProductPage.AssemblyName + "/mash.tl.aspx?op=vc&dsid=" + user.DataSource.JsonInstance ["id"] + "&r=" + user.RecordID + "&i=" + ProductPage.GuidLower (user.ID, false) + "&rr=" + rnd.Next () + "&fn=" + HttpUtility.UrlEncode (user [SCHEMAPROP_PREFIX + SCHEMAPROP_LOGINFIELD, ProductPage.AssemblyName].ToLowerInvariant () + "_" + user.RecordID) + ".vcf");
		}

		protected void EnsureUserFields (RecordPropertyCollection props) {
			string moreFields;
			string[]mfs;
			if ((props.GetPropertyByName (FIELDNAME_SITEGROUPS) == null) && !string.IsNullOrEmpty (this [SCHEMAPROP_LOGINFIELD, string.Empty]))
				props.Props.Add (new RecordProperty (this, FIELDNAME_SITEGROUPS, null));
			if ((!string.IsNullOrEmpty (moreFields = this ["mf", string.Empty])) && ((mfs = moreFields.Split (new char [] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (mfs.Length > 0))
				foreach (string fn in mfs)
					if (props.GetPropertyByName (fn) == null)
						props.Props.Add (new RecordProperty (this, fn, null));
		}

		protected virtual string GetDefVal (string schemaProp) {
			string tmp = string.Empty;
			ProductPage.LicInfo lic;
#if PEOPLEZEN
			if (!(this is Providers.Ado))
#endif
				if (schemaProp == SCHEMAPROP_TITLEFIELD)
					return (((!(this is Providers.UserAccounts)) || ProductPage.Is14) ? ("[" + RewritePropertyName ("FirstName") + "] [" + RewritePropertyName ("LastName") + "]\r\n") : string.Empty) + "[" + RewritePropertyName ("PreferredName") + "]\r\n[" + RewritePropertyName ("UserName") + "]\r\n[" + RewritePropertyName ("AccountName") + "]";
				else if (schemaProp == SCHEMAPROP_URLFIELD)
					return (this is Providers.Directory) ? string.Empty : "[roxUserPublicUrl]\r\n[roxUserPersonalUrl]";
				else if (schemaProp == SCHEMAPROP_DEFAULTFIELDS)
					return RewritePropertyName ("Department") + ":" + ProductPage.GetResource ("Department") + "\r\n" + RewritePropertyName ("WorkEmail") + ":" + ProductPage.GetResource ("WorkEmail");
				else if (schemaProp == SCHEMAPROP_MAILFIELD)
					return "[" + RewritePropertyName ("WorkEmail") + "]";
				else if (schemaProp == SCHEMAPROP_LOGINFIELD)
					return "[" + RewritePropertyName ("AccountName") + "]";
			if (schemaProp == "vc") {
				if (!(this is Providers.Ado))
					tmp += "N:" + (((!(this is Providers.UserAccounts)) || ProductPage.Is14) ? ("[" + RewritePropertyName ("LastName") + "];[" + RewritePropertyName ("FirstName") + "]") : ("[" + RewritePropertyName ("PreferredName") + "]")) + "\r\n";
				tmp += "FN:[" + SCHEMAPROP_PREFIX + SCHEMAPROP_TITLEFIELD + "]\r\nORG:" + ((((lic = ProductPage.LicInfo.Get (null)) == null) || string.IsNullOrEmpty (lic.name)) ? "ROXORITY Ltd." : lic.name.Replace ('[', '(').Replace (']', ')')) + "\r\nEMAIL:[" + SCHEMAPROP_PREFIX + SCHEMAPROP_MAILFIELD + "]\r\nPHOTO;VALUE=URL:[" + SCHEMAPROP_PREFIX + SCHEMAPROP_PICTUREFIELD + "]";
				if (!(this is Providers.Ado))
					tmp += "\r\nTEL:[" + RewritePropertyName ("CellPhone") + "]";
				return tmp;
			}
			if (schemaProp == SCHEMAPROP_PICTUREFIELD)
				return ((this is Providers.Directory) ? string.Empty : ("[" + RewritePropertyName ("PictureURL") + "]\r\n")) + "/_layouts/images/" + ProductPage.AssemblyName + "/" + (ProductPage.Is14 ? "person" : "no_pic") + ".gif";
			return string.Empty;
		}

		public override string FixupTitle (string name) {
			int pos;
			if (ExcludeDomain && ((pos = name.IndexOf ('\\')) > 0))
				name = name.Substring (pos + 1);
			return base.FixupTitle (name);
		}

		public override void InitSchema (JsonSchemaManager.Schema owner) {
			IDictionary pmore = new OrderedDictionary ();
#if PEOPLEZEN
			pmore ["lines"] = 4;
			pmore ["default"] = "system\\*\r\nnt authority\\*\r\nbuiltin\\*\r\nsharepoint\\*";
			AddSchemaProp (owner, "ex", "String", null, true, pmore);
			pmore = new OrderedDictionary ();
			AddSchemaProp (owner, "ed", "Boolean", null, true, pmore);
			pmore = new OrderedDictionary ();
#endif
			pmore ["default"] = GetDefVal (SCHEMAPROP_DEFAULTFIELDS);
			AddSchemaProp (owner, SCHEMAPROP_DEFAULTFIELDS, "DataFields", "w", true, pmore);
			pmore = new OrderedDictionary ();
			pmore ["default"] = GetDefVal (SCHEMAPROP_TITLEFIELD);
			AddSchemaProp (owner, SCHEMAPROP_TITLEFIELD, "DataFields", "w", true, pmore);
			pmore = new OrderedDictionary ();
			pmore ["default"] = GetDefVal (SCHEMAPROP_PICTUREFIELD);
			AddSchemaProp (owner, SCHEMAPROP_PICTUREFIELD, "DataFields", "w", true, pmore);
			pmore = new OrderedDictionary ();
			pmore ["default"] = GetDefVal (SCHEMAPROP_URLFIELD);
			AddSchemaProp (owner, SCHEMAPROP_URLFIELD, "DataFields", "w", true, pmore);
			pmore = new OrderedDictionary ();
			pmore ["default"] = GetDefVal (SCHEMAPROP_MAILFIELD);
			AddSchemaProp (owner, SCHEMAPROP_MAILFIELD, "DataFields", "w", true, pmore);
			pmore = new OrderedDictionary ();
			pmore ["default"] = GetDefVal (SCHEMAPROP_LOGINFIELD);
			AddSchemaProp (owner, SCHEMAPROP_LOGINFIELD, "DataFields", "w", true, pmore);
			pmore = new OrderedDictionary ();
			pmore ["default"] = GetDefVal ("vc");
			pmore ["default_if_empty"] = true;
			pmore ["lines"] = 6;
			AddSchemaProp (owner, "vc", "String", "w", true, pmore);
			pmore = new OrderedDictionary ();
			pmore ["lines"] = 6;
			AddSchemaProp (owner, "mf", "String", "w", true, pmore);
			base.InitSchema (owner);
		}

		public Uri GetPersonalUrl (Record user) {
			Uri prvUri = user.Tags ["prvUri "] as Uri;
			SPUser spUser = user.MainItem as SPUser;
			SPGroup spGroup = user.MainItem as SPGroup;
#if !WSS
			UserProfile prof = user.MainItem as UserProfile;
			if ((prvUri == null) && (prof != null))
				try {
					user.Tags ["prvUri "] = prvUri = prof.PersonalUrl;
				} catch {
				}
#endif
			if ((prvUri == null) && ((prvUri = user.Tags ["prvUri"] as Uri) == null))
				if (spUser != null)
					user.Tags ["prvUri "] = prvUri = new Uri (ProductPage.MergeUrlPaths (spUser.ParentWeb.Url.Replace ("http:", ((HttpContext != null) && HttpContext.Request.IsSecureConnection) ? "https:" : "http:"), "_layouts/userdisp.aspx") + "?Force=1&ID=" + spUser.ID + "&Source=" + ((HttpContext == null) ? string.Empty : HttpUtility.UrlEncode ((HttpContext.Request.Url.ToString ().ToLowerInvariant ().Contains ("_layouts/" + ProductPage.AssemblyName.ToLowerInvariant () + "/mash.tl.aspx?") ? HttpContext.Request.UrlReferrer : HttpContext.Request.Url).ToString ())));
				else if (spGroup != null)
					user.Tags ["prvUri "] = prvUri = new Uri (ProductPage.MergeUrlPaths (spGroup.ParentWeb.Url.Replace ("http:", ((HttpContext != null) && HttpContext.Request.IsSecureConnection) ? "https:" : "http:"), "_layouts/userdisp.aspx") + "?Force=1&ID=" + spGroup.ID + "&Source=" + ((HttpContext == null) ? string.Empty : HttpUtility.UrlEncode ((HttpContext.Request.Url.ToString ().ToLowerInvariant ().Contains ("_layouts/" + ProductPage.AssemblyName.ToLowerInvariant () + "/mash.tl.aspx?") ? HttpContext.Request.UrlReferrer : HttpContext.Request.Url).ToString ())));
			return prvUri;
		}

		public Uri GetPublicUrl (Record user) {
			Uri pubUri = user.Tags ["pubUri"] as Uri;
			SPUser spUser = user.MainItem as SPUser;
			SPGroup spGroup = user.MainItem as SPGroup;
#if !WSS
			UserProfile prof = user.MainItem as UserProfile;
			if ((pubUri == null) && (prof != null))
				try {
					user.Tags ["pubUri"] = pubUri = prof.PublicUrl;
				} catch {
				}
#endif
			if ((pubUri == null) && ((pubUri = user.Tags ["pubUri"] as Uri) == null))
				if (spUser != null)
					user.Tags ["pubUri"] = pubUri = new Uri (ProductPage.MergeUrlPaths (spUser.ParentWeb.Url.Replace ("http:", ((HttpContext != null) && HttpContext.Request.IsSecureConnection) ? "https:" : "http:"), "_layouts/userdisp.aspx") + "?ID=" + spUser.ID + "&Source=" + ((HttpContext == null) ? string.Empty : HttpUtility.UrlEncode ((HttpContext.Request.Url.ToString ().ToLowerInvariant ().Contains ("_layouts/" + ProductPage.AssemblyName.ToLowerInvariant () + "/mash.tl.aspx?") ? HttpContext.Request.UrlReferrer : HttpContext.Request.Url).ToString ())));
				else if (spGroup != null)
					user.Tags ["pubUri"] = pubUri = new Uri (ProductPage.MergeUrlPaths (spGroup.ParentWeb.Url.Replace ("http:", ((HttpContext != null) && HttpContext.Request.IsSecureConnection) ? "https:" : "http:"), "_layouts/userdisp.aspx") + "?ID=" + spGroup.ID + "&Source=" + ((HttpContext == null) ? string.Empty : HttpUtility.UrlEncode ((HttpContext.Request.Url.ToString ().ToLowerInvariant ().Contains ("_layouts/" + ProductPage.AssemblyName.ToLowerInvariant () + "/mash.tl.aspx?") ? HttpContext.Request.UrlReferrer : HttpContext.Request.Url).ToString ())));
			return pubUri;
		}

		public virtual string GetRecordVal (string prop, string val, string getProp) {
			DataSourceCache dsCache;
			CachedRecord crec = null;
			if (DataSourceConsumer.dsCaches.TryGetValue (ProductPage.GuidLower (ContextID, false), out dsCache))
				crec = dsCache.recordCache.Find (delegate (CachedRecord cachedRec) {
					return ((cachedRec != null) && val.Equals (cachedRec [prop, string.Empty, this], StringComparison.InvariantCultureIgnoreCase));
				});
			return ((crec == null) ? string.Empty : crec [getProp, string.Empty, this]);
		}

		public bool ExcludeDomain {
			get {
				return this ["ed", false];
			}
		}

		public string [] ExcludePatterns {
			get {
				if (exclPatterns == null) {
					exclPatterns = (this ["ex"] + string.Empty).Split (new char [] { '\r', '\n' });
				}
				return ((exclPatterns.Length == 0) ? null : exclPatterns);
			}
		}

	}

	#endregion

}

namespace roxority.SharePoint.JsonSchemaPropertyTypes {

	#region ClearCache Class

	public class ClearCache : JsonSchemaManager.Property.Type.Boolean {

		public override void Update (IDictionary inst, JsonSchemaManager.Property prop, HttpContext context, string formKey) {
			string id = inst ["id"] + string.Empty;
			DataSourceCache dsc;
			roxority.Data.DataSource ds;
			if ((!string.IsNullOrEmpty (context.Request [formKey])) && (DataSourceConsumer.dsCaches != null) && (DataSourceConsumer.dsCaches.TryGetValue (id, out dsc) || (((ds = roxority.Data.DataSource.FromID (id, true, true, null)) != null) && DataSourceConsumer.dsCaches.TryGetValue (ProductPage.GuidLower (ds.ContextID, false), out dsc))))
				dsc.Clear ();
		}

	}

	#endregion

	#region DataFields Class

	public class DataFields : JsonSchemaManager.Property.Type.String {

		public const string OPTION_REFRESH = "1b150b00-f653-4504-a47d-e7b59a4a12f6";

		internal static string RenderFieldDropDown (string label, bool labelInSelect, string ctlID, string buttonLabel, string onChange, bool divEnclose, bool namesOnly, bool enabled) {
			bool hasBtn = !string.IsNullOrEmpty (buttonLabel);
			string html = divEnclose ? "<div>" : string.Empty, dsid = ctlID.Substring (0, ctlID.IndexOf ('_'));
			html += ((labelInSelect ? string.Empty : ("<label>" + HttpUtility.HtmlEncode (label) + "</label> ")) + "<select " + (enabled ? string.Empty : "disabled=\"disabled\"") + " class=\"rox-iteminst-fieldsel-" + dsid + (namesOnly ? " rox-iteminst-fieldsel-small" : string.Empty) + "\" id=\"" + ctlID + "\" onchange=\"if(this.options[this.selectedIndex].value=='" + OPTION_REFRESH + "')roxRefreshFieldList('" + ctlID + "');else if(" + (!hasBtn).ToString ().ToLowerInvariant () + "){" + onChange + "}\"><option>" + (labelInSelect ? HttpUtility.HtmlEncode (label) : ProductPage.GetResource ("Tool_ItemEditor_DataFields_OptionSel")) + "</option><option value=\"" + OPTION_REFRESH + "\">" + ProductPage.GetResource ("Tool_ItemEditor_DataFields_OptionRef") + "</option></select>");
			if (hasBtn)
				html += ("<button " + (enabled ? string.Empty : "disabled=\"disabled\"") + " id=\"" + ctlID + "_btn\" class=\"rox-iteminst-fieldsel-" + dsid + "\" onclick=\"{" + HttpUtility.HtmlAttributeEncode (onChange) + "}\" type=\"button\">" + HttpUtility.HtmlEncode (buttonLabel) + "</button>");
			return html + (divEnclose ? "</div>" : string.Empty);
		}

		public override string RenderValueForDisplay (JsonSchemaManager.Property prop, object val) {
			prop.RawSchema ["lines"] = 3;
			return base.RenderValueForDisplay (prop, val);
		}

		public override string RenderValueForEdit (JsonSchemaManager.Property prop, IDictionary instance, bool disabled, bool readOnly) {
			bool isDef = prop.Name.EndsWith ("_pd");
			string html, ctlID = instance ["id"] + "_" + prop.Name;
			prop.RawSchema ["lines"] = 3;
			html = base.RenderValueForEdit (prop, instance, disabled, readOnly);
			if (JsonSchemaManager.Bool (prop.RawSchema ["allow_fieldsel"], true))
				html += RenderFieldDropDown (this ["Tool_ItemEditor_DataFields_LabelAdd"], false, ctlID + "_fields", string.Empty, "var tb=jQuery('#" + ctlID + "'),sel=jQuery('#" + ctlID + "_fields')[0];if((sel.selectedIndex>0)&&(sel.selectedIndex<(sel.options.length-1))){tb.val(tb.val()+(tb.val() ?'\\n':'')+'" + (isDef ? string.Empty : "[") + "'+sel.options[sel.selectedIndex].value+'" + (isDef ? string.Empty : "]") + "');roxScrollEnd(tb[0]);sel.selectedIndex=0;}", true, false, prop.Editable);
			return html;
		}

		public override string CssClass {
			get {
				return "rox-iteminst-edit-" + typeof (String).Name;
			}
		}

	}

	#endregion

#if !SETUPZEN

	#region DataPreview Class

	public class DataPreview : JsonSchemaManager.Property.Type {

		public override string RenderValueForEdit (JsonSchemaManager.Property prop, IDictionary instance, bool disabled, bool readOnly) {
			int rnd = new Random ().Next (int.MaxValue - 100, int.MaxValue), rnd2 = new Random ().Next (int.MaxValue - 200, int.MaxValue - 100);
			string tmpProps = ProductPage.GuidLower (Guid.NewGuid (), false), tmpTab = ProductPage.GuidLower (Guid.NewGuid (), false), tmpGroup = ProductPage.GuidLower (Guid.NewGuid (), false), tmpDyn = ProductPage.GuidLower (Guid.NewGuid (), false), instID = instance ["id"] + string.Empty, ctlID = instID + "_" + prop.Name, reloadScript = roxority_RollupZen.RollupWebPart.GetReloadScript ("roxReloadRollup", ctlID + "_tb", ctlID, rnd, 0, 1, 1, 1, false, false, false, tmpProps, true, true, true, string.Empty, false, tmpTab, string.Empty, string.Empty, tmpGroup, false, false, true, false, true, 2, "180px", 1, rnd2, false, false, 0, "k", false, null, instID, tmpDyn).Replace (rnd.ToString (), "parseInt(roxGetCtlVal('" + instID + "', 'pps'))").Replace (rnd2.ToString (), "(roxGetCtlVal('" + instID + "',roxDataTypePrefixes[roxGetCtlVal('" + instID + "', 't')]+'_pp')?1:0)").Replace ("\"" + tmpTab + "\"", "roxGetFieldSel('" + ctlID + "_tabby')").Replace ("\"" + tmpGroup + "\"", "roxGetFieldSel('" + ctlID + "_groupby')").Replace ("\"" + tmpDyn + "\"", "roxGetDynInst('" + instID + "')").Replace ("\"" + tmpProps + "\"", "roxSlimEncode(roxGetCtlVal('" + instID + "',roxDataTypePrefixes[roxGetCtlVal('" + instID + "', 't')]+'_pd'))");
			StringBuilder buffer = new StringBuilder ();
			buffer.Append ("<div class=\"rox-dataprev-tools\">");
			//buffer.Append ("<label>" + this ["Tool_ItemEditor_DataPreview_GroupBy"] + " </label><select><option>" + this ["None"] + "</option></select>");
			//buffer.Append ("<label>" + this ["Tool_ItemEditor_DataPreview_TabBy"] + " </label><select><option>" + this ["None"] + "</option></select>");
			buffer.Append (DataFields.RenderFieldDropDown (this ["Tool_ItemEditor_DataPreview_GroupBy"], true, ctlID + "_groupby", string.Empty, string.Empty, false, true, prop.IsLe (2)));
			buffer.Append (DataFields.RenderFieldDropDown (this ["Tool_ItemEditor_DataPreview_TabBy"], true, ctlID + "_tabby", string.Empty, string.Empty, false, true, prop.IsLe (4)));
			buffer.Append ("<label for=\"" + instID + "_pps\">" + this ["Tool_ItemEditor_DataPreview_PageSize"] + " </label><input " + (prop.IsLe (2) ? string.Empty : "disabled=\"disabled\"") + " style=\"width: 20px;\" id=\"" + instID + "_pps\" onchange=\"this.value=roxValidateNumeric('" + instID + "', 'pps', '4');\" value=\"4\" />");
			buffer.Append ("\n<script type=\"text/javascript\" language=\"JavaScript\">\nfunction rel" + tmpTab + "(){" + reloadScript + "}\n</script>\n");
			buffer.Append ("<input onclick=\"rel" + tmpTab + "();\" type=\"button\" value=\"" + this ["Tool_ItemEditor_DataPreview_Refresh"] + "\"/></div>");
			buffer.Append ("<textarea style=\"display: none; width: 98%;\" id=\"" + ctlID + "_tb\"></textarea><div class=\"rox-dataprev\" id=\"" + ctlID + "\"><div id=\"rox_rollup_" + ctlID + "\"><div class=\"rox-dataprev-hint rox-rollup-paging\" id=\"rox_pager_" + ctlID + "\">" + this ["Tool_ItemEditor_DataPreview_RefreshHint"] + "</div>");
			//using (StringWriter sw = new StringWriter (buffer))
			//    roxority_RollupZen.RollupWebPart.Render (null, sw, ctlID + "_tb", ctlID, 6, 0, 1, 1, 2, false, false, false, "ID", true, true, true, "ID", false, string.Empty, string.Empty, string.Empty, string.Empty, false, false, true, true, true, 2, "180px", 1, 1, true, 0, string.Empty, false, null, null, true);
			buffer.Append ("</div></div>");
			return buffer.ToString ();
		}

		public override bool ShowInSummary {
			get {
				return false;
			}
		}

	}

	#endregion

#endif

	#region DataProvider Class

	public class DataProvider : JsonSchemaManager.Property.Type.Choice {

		public DataProvider ()
			: base (null) {
		}

		public override IEnumerable GetChoices (IDictionary rawSchema) {
			foreach (Type t in roxority.Data.DataSource.KnownProviderTypes)
				yield return t.Name;
		}

		public override string CssClass {
			get {
				return "rox-iteminst-edit-control rox-iteminst-edit-" + typeof (Choice).Name;
			}
		}

	}

	#endregion

	#region PrintTargetMode Class

	public class PrintTargetMode : JsonSchemaManager.Property.Type.Choice {

		public PrintTargetMode ()
			: base (null) {
		}

		public override string GetChoiceDesc (JsonSchemaManager.Property prop, object choice) {
			return ProductPage.GetProductResource ("PD_PrintTargetMode_" + choice);
		}

		public override IEnumerable GetChoices (IDictionary rawSchema) {
			if (JsonSchemaManager.Bool (rawSchema ["support_printzenpage"], true))
				yield return "p";
			if (JsonSchemaManager.Bool (rawSchema ["support_origpage"], true))
				yield return "o";
			if (JsonSchemaManager.Bool (rawSchema ["allow_none"], true))
				yield return "n";
		}

		public override string GetChoiceTitle (JsonSchemaManager.Property prop, object choice) {
			return ProductPage.GetProductResource ("PC_PrintTargetMode_" + choice);
		}

		public override string CssClass {
			get {
				return "rox-iteminst-edit-control rox-iteminst-edit-" + typeof (Choice).Name;
			}
		}

	}

	#endregion

}
