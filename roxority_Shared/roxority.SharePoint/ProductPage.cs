
using Microsoft.SharePoint;
using Microsoft.SharePoint.Administration;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.WebPartPages.Communication;
using Microsoft.Win32;
using roxority.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Security.Cryptography;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls.WebParts;
using System.Xml;

namespace roxority.SharePoint {

	// FILTER_BEGIN

	public delegate T Func<T> ();

	#region CamlOperator Enumeration

	public enum CamlOperator {

		Eq = 0,
		Neq = 1,
		Gt = 2,
		Lt = 3,
		Geq = 4,
		Leq = 5,
		BeginsWith = 6,
		Contains = 7,
		Member = 8,
		RangeGeLe = 9,
		RangeGeLt = 10,
		RangeGtLe = 11,
		RangeGtLt = 12,
		Me = 13,
		NotMe = 14

	}

	#endregion

	#region ConnectionConsumer Class

	public class ConnectionConsumer : ConsumerConnectionPoint {

		public ConnectionConsumer (MethodInfo callbackMethod, Type interfaceType, Type controlType, string displayName, string id, bool allowsMultipleConnections)
			: base (callbackMethod, interfaceType, controlType, ProductPage.GetProductResource (displayName), id, allowsMultipleConnections) {
		}

	}

	#endregion

	#region ConnectionProvider Class

	public class ConnectionProvider : ProviderConnectionPoint {

		public ConnectionProvider (MethodInfo callbackMethod, Type interfaceType, Type controlType, string displayName, string id, bool allowsMultipleConnections)
			: base (callbackMethod, interfaceType, controlType, ProductPage.GetProductResource (displayName), id, allowsMultipleConnections) {
		}

	}

	#endregion

	#region FeatureReceiver Class

	public class FeatureReceiver : SPFeatureReceiver {

		public override void FeatureActivated (SPFeatureReceiverProperties properties) {
		}

		public override void FeatureDeactivating (SPFeatureReceiverProperties properties) {
			SPSite site = null;
			SPList cat = null;
			List<string> webParts = new List<string> ();
			bool hasUpdates = false, repeat = true;
			string tmp;
			try {
				foreach (SPElementDefinition def in properties.Definition.GetElementDefinitions (CultureInfo.InvariantCulture))
					if (def.XmlDefinition.HasChildNodes)
						foreach (XmlNode childNode in def.XmlDefinition.ChildNodes)
							foreach (XmlAttribute att in childNode.Attributes)
								if (att.LocalName == "Url") {
									if (!webParts.Contains (tmp = att.Value.ToLowerInvariant ()))
										webParts.Add (tmp);
									break;
								}
			} catch {
			}
			try {
				if ((webParts.Count > 0) && ((site = properties.Feature.Parent as SPSite) != null) && ((cat = site.GetCatalog (SPListTemplateType.WebPartCatalog)) != null))
					while (repeat) {
						repeat = false;
						foreach (SPListItem item in ProductPage.TryEach<SPListItem> (cat.Items))
							if (webParts.Contains (item.Name.ToLowerInvariant ())) {
								item.Delete ();
								repeat = hasUpdates = true;
								break;
							}
					}
			} catch {
			}
			try {
				if (hasUpdates) {
					site.AllowUnsafeUpdates = true;
					cat.Update ();
				}
			} catch {
			}
		}

		public override void FeatureInstalled (SPFeatureReceiverProperties properties) {
		}

		public override void FeatureUninstalling (SPFeatureReceiverProperties properties) {
		}

	}

	#endregion

	#region Reflector Class

	public class Reflector {

		private static Reflector current = null;

		public readonly Assembly Assembly;

		private Dictionary<string, Type> types = new Dictionary<string, Type> ();
		private Dictionary<Type, Dictionary<string, MemberInfo>> memberInfos = new Dictionary<Type, Dictionary<string, MemberInfo>> ();

		public static Reflector Current {
			get {
				if (current == null)
					current = new Reflector (ProductPage.Assembly);
				return current;
			}
		}

		public Reflector (Assembly assembly) {
			Assembly = assembly;
		}

		public object Call (Type type, string name, params object [] args) {
			MemberInfo member;
			MethodInfo method;
			Dictionary<string, MemberInfo> dict;
			if ((!MemberInfos.TryGetValue (type, out dict)) || (dict == null))
				MemberInfos [type] = dict = new Dictionary<string, MemberInfo> ();
			if (!dict.TryGetValue (name, out member))
				dict [name] = member = type.GetMethod (name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if ((method = member as MethodInfo) != null)
				return method.Invoke (null, args);
			return null;
		}

		public object Call (object obj, string name, Type [] argTypes, object [] args) {
			Type type;
			Type [] types = ((argTypes != null) ? argTypes : new Type [(args == null) ? 0 : args.Length]);
			MemberInfo member;
			MethodInfo method;
			Dictionary<string, MemberInfo> dict;
			if (obj != null) {
				if (argTypes == null)
					if ((args == null) || (args.Length == 0))
						argTypes = Type.EmptyTypes;
					else
						for (int i = 0; i < args.Length; i++)
							types [i] = ((args [i] == null) ? typeof (object) : args [i].GetType ());
				if ((!MemberInfos.TryGetValue (type = obj.GetType (), out dict)) || (dict == null))
					MemberInfos [type] = dict = new Dictionary<string, MemberInfo> ();
				if (!dict.TryGetValue (name, out member))
					dict [name] = member = type.GetMethod (name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, types, null);
				if ((method = member as MethodInfo) != null)
					return method.Invoke (obj, args);
			}
			return null;
		}

		public object Get (object obj, string name, params object [] args) {
			Type type;
			MemberInfo [] mems;
			MemberInfo member;
			PropertyInfo prop;
			FieldInfo field;
			Dictionary<string, MemberInfo> dict;
			if (obj != null) {
				if ((!MemberInfos.TryGetValue (type = obj.GetType (), out dict)) || (dict == null))
					MemberInfos [type] = dict = new Dictionary<string, MemberInfo> ();
				if ((!dict.TryGetValue (name, out member)) && ((mems = type.GetMember (name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) != null))
					foreach (MemberInfo mi in mems) {
						dict [name] = member = mi;
						break;
					}
				if ((field = member as FieldInfo) != null)
					return field.GetValue (obj);
				else if ((prop = member as PropertyInfo) != null)
					return prop.GetValue (obj, ((args != null) && (args.Length == 0)) ? null : args);
			}
			return null;
		}

		public Type GetType (string typeName) {
			Type type;
			if (!types.TryGetValue (typeName.ToLowerInvariant (), out type))
				types [typeName.ToLowerInvariant ()] = type = Assembly.GetType (typeName, false, true);
			return type;
		}

		public object New (string typeName, params object [] args) {
			Type type = GetType (typeName);
			if (type != null)
				return Activator.CreateInstance (type, args);
			return null;
		}

		public void Set (object obj, string name, object value, params object [] args) {
			Type type;
			MemberInfo [] mems;
			MemberInfo member;
			PropertyInfo prop;
			FieldInfo field;
			Dictionary<string, MemberInfo> dict;
			if (obj != null) {
				if ((!MemberInfos.TryGetValue (type = obj.GetType (), out dict)) || (dict == null))
					MemberInfos [type] = dict = new Dictionary<string, MemberInfo> ();
				if ((!dict.TryGetValue (name, out member)) && ((mems = type.GetMember (name, MemberTypes.Property | MemberTypes.Field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)) != null))
					foreach (MemberInfo mi in mems) {
						dict [name] = member = mi;
						break;
					}
				if ((field = member as FieldInfo) != null)
					field.SetValue (obj, value);
				else if ((prop = member as PropertyInfo) != null)
					prop.SetValue (obj, value, ((args != null) && (args.Length == 0)) ? null : args);
			}
		}

		internal Dictionary<Type, Dictionary<string, MemberInfo>> MemberInfos {
			get {
				if (memberInfos == null)
					memberInfos = new Dictionary<Type, Dictionary<string, MemberInfo>> ();
				return memberInfos;
			}
		}

	}

	#endregion

	#region RollupWebPart Class

#if !SETUPZEN
	public class RollupWebPart : roxority_RollupZen.RollupWebPart {
	}
#endif
	
	#endregion

	#region Serializables<> Class

	[Serializable]
	public class Serializables<T> : IEnumerable<T>, ISerializable {

		public readonly T [] Values;

		public Serializables (IEnumerable<T> values) {
			Values = ((values == null) ? new List<T> () : new List<T> (values)).ToArray ();
		}

		public Serializables (params T [] values) {
			Values = values;
		}

		public Serializables (SerializationInfo info, StreamingContext context) {
			int count = 0;
			List<T> vals;
			object val;
			try {
				count = info.GetInt32 ("Count");
			} catch {
			}
			vals = new List<T> (count);
			for (int i = 0; i < count; i++)
				try {
					if ((val = info.GetValue ("Item_" + i, typeof (T))) is T)
						vals.Add ((T) val);
				} catch {
				}
			Values = vals.ToArray ();
		}

		IEnumerator IEnumerable.GetEnumerator () {
			return Values.GetEnumerator ();
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator () {
			return new List<T> (Values).GetEnumerator ();
		}

		void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context) {
			info.AddValue ("Count", Values.Length);
			for (int i = 0; i < Values.Length; i++)
				try {
					info.AddValue ("Item_" + i, Values [i]);
				} catch {
				}
		}

	}

	#endregion

	#region SPWrap<> Class

	internal class SPWrap<T> : IDisposable where T : class {

		public readonly SPSite Site;
		public readonly SPWeb Web;
		public readonly T Value = null;

		private readonly bool dispose = true;

		public static SPWrap<T> Create (string fullUrl, Converter<SPWeb, T> converter) {
			SPSite site;
			if (string.IsNullOrEmpty (fullUrl))
				throw new ArgumentNullException ("fullUrl");
			else if ((!fullUrl.StartsWith ("http://")) && (!fullUrl.StartsWith ("https://")))
				fullUrl = ProductPage.MergeUrlPaths (fullUrl.StartsWith ("/") ? SPContext.Current.Site.Url : SPContext.Current.Web.Url, fullUrl);
			return new SPWrap<T> (site = new SPSite (fullUrl), site.OpenWeb (), converter);
		}

		public SPWrap (SPSite site, SPWeb web, T value, bool dispose) {
			Site = site;
			Web = web;
			Value = value;
			this.dispose = dispose;
		}

		public SPWrap (SPSite site, SPWeb web, Converter<SPWeb, T> converter) {
			Site = site;
			Web = web;
			if (converter != null)
				Value = converter (Web);
		}

		void IDisposable.Dispose () {
			if (dispose) {
				if (Web != null)
					Web.Dispose ();
				if (Site != null)
					Site.Dispose ();
			}
		}

	}

	#endregion

	#region SPElevator Class

	internal class SPElevator : IDisposable {

		private const int LOGON32_LOGON_NETWORK = 3;
		private const int LOGON32_PROVIDER_DEFAULT = 0;

		private WindowsImpersonationContext context = null;
		private WindowsIdentity identity = null;
		private IntPtr handle = IntPtr.Zero;

		[DllImport ("kernel32.dll", CharSet = CharSet.Auto)]
		internal static extern bool CloseHandle (IntPtr handle);

		[DllImport ("advapi32.dll", SetLastError = true)]
		internal static extern bool LogonUser (string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, ref IntPtr phToken);

		public SPElevator (string domain, string username, string password) {
			int pos = username.IndexOf ('\\');
			if (string.IsNullOrEmpty (domain)) {
				if (pos > 0) {
					domain = username.Substring (0, pos);
					username = username.Substring (pos + 1);
				} else
					domain = Environment.UserDomainName;
			}
			if (!(LogonUser (username, domain, password, LOGON32_LOGON_NETWORK, LOGON32_PROVIDER_DEFAULT, ref handle)))
				throw new UnauthorizedAccessException (ProductPage.GetResource ("SPElevator_Error", domain + "\\" + username, Marshal.GetLastWin32Error ()));
			context = (identity = new WindowsIdentity (handle)).Impersonate ();
		}

		public void Dispose () {
			try {
				context.Undo ();
			} catch {
			}
			try {
				identity.Dispose ();
			} catch {
			}
			try {
				context.Dispose ();
			} catch {
			}
			try {
				if (handle != IntPtr.Zero)
					CloseHandle (handle);
			} catch {
			}
		}

	}

	#endregion

	#region WebPartBase Class

	public abstract class WebPartBase : Microsoft.SharePoint.WebPartPages.WebPart {

		protected internal string urlPropertyPrefix = "prop_";

		internal bool expiredTitles = false;

		private ProductPage.LicInfo licInfo = null;
		private bool ex = false, ec = false, urlSettings = false;
		private int jQuery = 0;

		public static SPSite OffSite {
			get {
				return ProductPage.currentSite;
			}
			set {
				ProductPage.currentSite = value;
			}
		}

		internal bool LicEd (int edition) {
			return TwilightZone || ProductPage.LicEdition (ProductPage.GetContext (), Lic, edition);
		}

		protected internal object GetPropValue (string name, object value) {
			string paramName = urlPropertyPrefix + name, queryVal = ((Context == null) ? null : Context.Request.QueryString [paramName]);
			bool inQuery = UrlSettings && (Context != null) && (Array.IndexOf<string> (Context.Request.QueryString.AllKeys, paramName) >= 0);
			int intVal;
			double doubleVal;
			long longVal;
			if (inQuery) {
				if (value is bool)
					value = ((queryVal == "1") ? true : ((queryVal == "0") ? false : value));
				else if (value is int)
					value = int.TryParse (queryVal, out intVal) ? intVal : value;
				else if (value is double)
					value = double.TryParse (queryVal, out doubleVal) ? doubleVal : value;
				else if (value is long)
					value = long.TryParse (queryVal, out longVal) ? longVal : value;
				else
					value = queryVal;
			}
			return value;
		}

		protected internal T GetProp<T> (string name, T value) {
			return (T) GetPropValue (name, value);
		}

		//protected override bool RequiresWebPartClientScript () {
		//    return true;
		//}

		public override ConnectionRunAt CanRunAt () {
			return ConnectionRunAt.Server;
		}

		internal ProductPage.LicInfo Lic {
			get {
				if ((licInfo == null) && !TwilightZone)
					licInfo = ProductPage.LicInfo.Get (null);
				return licInfo;
			}
		}

		protected override HttpContext Context {
			get {
				try {
					return base.Context;
				} catch {
					return null;
				}
			}
		}

		protected internal virtual bool CanRun {
			get {
				try {
					return ((Context != null) && (SPContext.Current != null) && (SPContext.Current.Site != null));
				} catch {
					return false;
				}
			}
		}

		internal virtual bool Exed {
			get {
				if ((!ec) && !TwilightZone) {
					ec = true;
					ex = Lic.expired;
				}
				return ex;
			}
		}

		public string this [string resKey, params object [] args] {
			get {
				return ProductPage.GetProductResource (resKey, args);
			}
		}

		public bool EffectiveJquery {
			get {
				return ((JQuery == 1) || ((JQuery == 0) && !ProductPage.Config<bool> (ProductPage.GetContext (), "_nojquery")));
			}
		}

		public string ExpiredMessage {
			get {
				return ProductPage.GetResource ("LicExpiry");
			}
		}

		public virtual bool IsConnected {
			get {
				return false;
			}
		}

		public bool IsDesign {
			get {
				return DesignMode || (WebPartManager == null) || ((WebPartManager.DisplayMode != null) && WebPartManager.DisplayMode.AllowPageDesign);
			}
		}

		public bool IsFrontPage {
			get {
				return ((Context != null) && (((!string.IsNullOrEmpty (Context.Request.UserAgent)) && (Context.Request.UserAgent.ToLowerInvariant ().Contains ("msfrontpage"))) || Context.Request.Url.ToString ().ToLowerInvariant ().Contains ("/_layouts/toolpane.aspx")));
			}
		}

		public bool IsPreview {
			get {
				return Context.Request.Url.ToString ().ToLowerInvariant ().Contains ("/_layouts/wpprevw.aspx?");
			}
		}

		public virtual bool IsViewPage {
			get {
				return ((Page is Microsoft.SharePoint.WebPartPages.WebPartPage) && (Page.GetType ().FullName.StartsWith ("ASP.VIEWPAGE_ASPX__", StringComparison.InvariantCultureIgnoreCase)));
			}
		}

		[Personalizable]
		public int JQuery {
			get {
				return GetProp<int> ("JQuery", jQuery);
			}
			set {
				jQuery = ((value < 0) ? 0 : ((value > 2) ? 2 : value));
			}
		}

		[Personalizable]
		public override string Title {
			get {
				return ((Exed && CanRun && expiredTitles) ? ExpiredMessage : base.Title);
			}
			set {
				base.Title = ((Exed && CanRun && expiredTitles) ? ExpiredMessage : value.Replace (ExpiredMessage, string.Empty));
			}
		}

		[Personalizable]
		public override string TitleUrl {
			get {
				return ((Exed && CanRun && expiredTitles) ? ProductPage.GetProductResource ("_WhiteLabelUrl") + ProductPage.GetTitle ().ToLowerInvariant () + "-license/" : base.TitleUrl);
			}
			set {
				base.TitleUrl = ((Exed && CanRun && expiredTitles) ? ProductPage.GetProductResource ("_WhiteLabelUrl") + ProductPage.GetTitle ().ToLowerInvariant () + "-license/" : value.Replace (ProductPage.GetProductResource ("_WhiteLabelUrl") + ProductPage.GetTitle ().ToLowerInvariant () + "-license/", ""));
			}
		}

		private bool TwilightZone {
			get {
				return ((HttpContext.Current != null) && ((Page == null) || (Parent == null)));
			}
		}

		[Personalizable]
		public virtual bool UrlSettings {
			get {
				return urlSettings;
			}
			set {
				urlSettings = value;
			}
		}

	}

	#endregion
	// FILTER_END

	public class ProductPage
		// FILTER_BEGIN
		: Page
		// FILTER_END
	{
		// FILTER_BEGIN
		#region li Class

		internal class LicInfo {

			internal bool broken = false;
			internal bool enabled = false;
			internal bool expired = false;
			internal DateTime expiry = DateTime.MinValue;
			internal bool userBroken = false;
			internal string name = "";
			internal int maxUsers = -1;
			internal TimeSpan installSpan = TimeSpan.MinValue;
			internal int siteUsers = -1;
			internal Exception error = null;

			internal readonly IDictionary dic, sd;

			internal static LicInfo Get (IDictionary dic) {
				LicInfo li = null;
				HttpContext ht = null;
				try {
					ht = HttpContext.Current;
				} catch {
				}
				if ((dic == null) && (ht != null)) {
					li = ht.Items ["___" + AssemblyName + "_li"] as LicInfo;
					if (li == null)
						ht.Items ["___" + AssemblyName + "_li"] = li = new LicInfo ();
				}
				return ((li == null) ? new LicInfo (null, dic) : li);
			}

			private LicInfo ()
				: this (null, null) {
			}

			private LicInfo (SPContext context)
				: this (context, null) {
			}

			private LicInfo (SPContext context, IDictionary dic) {
				long ev;
				IDictionary sd = null;
				if (context == null)
					context = ProductPage.GetContext ();
				if (context != null)
					Elevate (delegate () {
						bool isUnltd = false;
						try {
							if (!(broken = (((sd = GetStatus<Dictionary<string, object>> (context)) != null) && (sd.Count == 0)))) {
								if (sd == null) {
									sd = new Dictionary<string, object> ();
									sd ["is"] = (long) 0;
									sd ["ed"] = DateTime.Today.AddDays (l1);
								}
								installSpan = new TimeSpan ((long) sd ["is"]);
								expired = ((DateTime.Now >= (expiry = (DateTime) sd ["ed"])) && (sd.Count <= 3));
							} else {
								if (sd == null)
									sd = new Dictionary<string, object> ();
								sd ["is"] = TimeSpan.FromDays (l1 + 1).Ticks;
								sd ["ed"] = DateTime.Now.Subtract (TimeSpan.FromDays (1));
								installSpan = new TimeSpan ((long) sd ["is"]);
								expired = true;
							}
							if ((sd != null) && (sd.Count > 3)) {
								foreach (string k in sd.Keys)
									if (isUnltd = (ProductPage.IsGuid (k) && Guid.Empty.Equals (ProductPage.GetGuid (k, true))))
										break;
								if (!isUnltd)
									expiry = DateTime.MinValue;
							}
							if (dic == null)
								dic = LicObject (context);
							if (dic != null) {
								expired = false;
								if (dic.Contains ("f4") && long.TryParse (dic ["f4"] + "", out ev) && (ev > 0))
									expiry = new DateTime (ev);
								if (IsTheThing (dic)) {
									userBroken = true;
									foreach (object o in dic)
										if (o is DictionaryEntry) {
											maxUsers = int.Parse (((DictionaryEntry) o).Key as string);
											siteUsers = (int) ((DictionaryEntry) o).Value;
										} else if (o is KeyValuePair<string, object>) {
											maxUsers = int.Parse (((KeyValuePair<string, object>) o).Key);
											siteUsers = (int) ((KeyValuePair<string, object>) o).Value;
										}
								} else {
									userBroken = (((siteUsers = GetUsers (context, Elevated, null)) > (maxUsers = int.Parse (dic ["f2"] as string))) && (maxUsers > 0));
									name = ProductPage.LicName (context, dic);
								}
							}
							if (!expired)
								expired = ((((dic == null) || string.IsNullOrEmpty (name)) && (expired || broken || userBroken)) || ((dic != null) && string.IsNullOrEmpty (name)));
						} catch (Exception ex) {
							error = ex;
							broken = true;
							expired = true;
						}
					}, true);
				this.dic = dic;
				this.sd = sd;
			}

		}

		#endregion

		public const string FORMAT_CAML_VIEWFIELD = "<FieldRef Name=\"{0}\"/>";
		public const string SEPARATOR = ";#";

		internal const string FILTERZEN_TYPENAME = "roxority_FilterZen.roxority_FilterWebPart, roxority_FilterZen, Version=1.0.0.0, Culture=neutral, PublicKeyToken=68349fdcd3484f01";

		public static string GoogSrc = null;

		public static readonly Assembly Assembly = typeof (ProductPage).Assembly;
		public static readonly string AssemblyName = (Assembly.ToString ().Contains (",") ? Assembly.ToString ().Substring (0, Assembly.ToString ().IndexOf (',')) : Assembly.ToString ());
		public static readonly string [] HelpTopicIDs = new string [] { "intro", "eula" };
		public static readonly string [] AdminHelpTopicIDs = new string [0];
		public static readonly string [] SPOddFieldNames = { "LinkDiscussionTitle", "LinkDiscussionTitleNoMenu", "LinkTitle", "LinkTitleNoMenu", "Attachments", "BaseName", "EncodedAbsUrl", "Edit", "SelectTitle", "PermMask", "UniqueId", "ScopeId", "_EditMenuTableStart", "_EditMenuTableEnd", "LinkFilenameNoMenu", "LinkFilename", "ServerUrl", "FileLeafRef", "ParentLeafName", "DocIcon" };
		public static readonly string DefaultTopicID = "intro";
		public static readonly ResourceManager ProductResources = new ResourceManager (AssemblyName + ".Properties.Resources", Assembly);
		public static readonly ResourceManager Resources = new ResourceManager (AssemblyName + ".Properties.roxority_Shared", Assembly);
		internal static readonly Dictionary<string, ResourceManager> resMans = new Dictionary<string, ResourceManager> ();
		private static bool farmMethodTried = false;
		private static MethodInfo farmMethod = null;
		private static bool? is14 = null;
		private static bool? is15 = null;
		// FILTER_END
		private static readonly Regex regexGuidPattern = new Regex (@"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$", RegexOptions.Compiled);
		// FILTER_BEGIN

		protected internal static readonly string tk = "t";
		protected internal static readonly int l1 = 45, l2 = 43, l3 = 60, l4 = 8;

		internal static readonly BinaryFormatter Formatter = new BinaryFormatter (null, new StreamingContext (StreamingContextStates.Persistence));

		public static bool isEnabled = true;

		[ThreadStatic]
		protected internal static string errorMessage = string.Empty;

		protected internal static byte [] os = new List<string> (GetProductResource ("_ProductID").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries)).ConvertAll<byte> (delegate (string value) {
			return byte.Parse (value.Trim ());
		}).ToArray ();

		[ThreadStatic]
		public static SPSite currentSite = null;

		[ThreadStatic]
		public static bool Elevated = false;

		internal static Dictionary<string, string> cfgGroups = null;

		private static readonly BinaryFormatter formatter = new BinaryFormatter (null, new StreamingContext (StreamingContextStates.Remoting));
		private static string mapkey = null, name = null, pname = null, wlabel = null;
		private static readonly List<KeyValuePair<int, string>> editions = new List<KeyValuePair<int, string>> ();
		private static readonly Dictionary<string, object> usersDic = new Dictionary<string, object> ();

		private static List<CultureInfo> allCultures = null, allSpecificCultures = null;

		public readonly Random Rnd = new Random ();
		public bool IsFarmError = false;
		public string pageTitle = string.Empty;

		public Exception postEx = null;
		protected internal string alertMessage = string.Empty;

		private SPWebApplication adminApp;
		private SPSite adminSite;
		private SPFarm farm;
		private bool? isAnyAdmin = null, isFarmAdmin = null, isSiteAdmin = null, isAppAdmin = null;

		static ProductPage () {
			HelpTopicIDs = GetProductResource ("_HelpTopics").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			AdminHelpTopicIDs = GetProductResource ("_AdminHelpTopics").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			DefaultTopicID = ((HelpTopicIDs.Length > 0) ? HelpTopicIDs [0] : string.Empty);
			formatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
		}

		internal static CultureInfo farmCulture {
			get {
				return HttpContext.Current.Items ["farmCulture"] as CultureInfo;
			}
			set {
				if (HttpContext.Current != null)
					HttpContext.Current.Items ["farmCulture"] = value;
			}
		}

		internal static CultureInfo siteCulture {
			get {
				return HttpContext.Current.Items ["siteCulture"] as CultureInfo;
			}
			set {
				if (HttpContext.Current != null)
					HttpContext.Current.Items ["siteCulture"] = value;
			}
		}

		public static List<CultureInfo> AllCultures {
			get {
				if (allCultures == null) {
					allCultures = new List<CultureInfo> (CultureInfo.GetCultures (CultureTypes.AllCultures));
					allCultures.Sort (delegate (CultureInfo one, CultureInfo two) {
						return one.DisplayName.CompareTo (two.DisplayName);
					});
				}
				return allCultures;
			}
		}

		public static List<CultureInfo> AllSpecificCultures {
			get {
				if (allSpecificCultures == null) {
					allSpecificCultures = new List<CultureInfo> (CultureInfo.GetCultures (CultureTypes.SpecificCultures));
					allSpecificCultures.Sort (delegate (CultureInfo one, CultureInfo two) {
						return one.DisplayName.CompareTo (two.DisplayName);
					});
				}
				return allSpecificCultures;
			}
		}

		public static bool HasMicro {
			get {
				return string.IsNullOrEmpty (GetProductResource ("_NoMicro"));
			}
		}

		public static bool IsWhiteLabel {
			get {
				return !"roxority_".Equals (GetProductResource ("_WhiteLabel"));
			}
		}

		public static IEnumerable<int> WssInstalledCultures {
			get {
				string [] names = { @"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\12.0\InstalledLanguages", @"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\14.0\InstalledLanguages", @"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\15.0\InstalledLanguages" };
				int i;
				RegistryKey key;
				foreach (string name in names) {
					key = null;
					try {
						key = Registry.LocalMachine.OpenSubKey (name);
					} catch {
					}
					if (key != null)
						using (key)
							foreach (string lcid in key.GetValueNames ())
								if ((!string.IsNullOrEmpty (lcid)) && int.TryParse (lcid, out i))
									yield return i;
				}
			}
		}

		private static bool Deep (object one, object two) {
			ICollection col = one as ICollection, col2 = two as ICollection;
			object [] arr, arr2;
			IDictionary dict = one as IDictionary, dict2 = two as IDictionary;
			if (((one == null) && (two == null)) || ReferenceEquals (one, two))
				return true;
			if ((one == null) || (two == null) || (one.GetType () != two.GetType ()))
				return false;
			if (dict != null) {
				if (dict.Count != dict2.Count)
					return false;
				foreach (string k in dict.Keys) {
					if (!dict2.Contains (k))
						return false;
					if (!Deep (dict [k], dict2 [k]))
						return false;
				}
				return true;
			}
			if (col != null) {
				if (col.Count != col2.Count)
					return false;
				arr = new object [col.Count];
				arr2 = new object [col2.Count];
				col.CopyTo (arr, 0);
				col2.CopyTo (arr2, 0);
				for (int c = 0; c < col.Count; c++)
					if (!Deep (arr [c], arr2 [c]))
						return false;
				return true;
			}
			return one.Equals (two);
		}

		private static IEnumerable<SPPersistedObject> EnumeratePersisteds (SPContext context) {
			return EnumeratePersisteds (context, false);
		}

		private static IEnumerable<SPPersistedObject> EnumeratePersisteds (SPContext context, bool contentAppsOnly) {
			if (!contentAppsOnly)
				yield return GetFarm (context);
			foreach (SPWebService webService in new SPWebService [] { SPWebService.AdministrationService, SPWebService.ContentService }) {
				if (!contentAppsOnly)
					yield return webService;
				if ((webService == SPWebService.ContentService) || (!contentAppsOnly))
					foreach (SPWebApplication webApp in TryEach<SPWebApplication> (webService.WebApplications))
						yield return webApp;
			}
		}

		private static List<KeyValuePair<int, string>> GetEditions () {
			int last = -1;
			List<KeyValuePair<int, string>> lst;
			if (editions.Count == 0)
				lock (editions) {
					lst = new List<KeyValuePair<int, string>> ();
					foreach (string k in GetProductResource ("_Editions").Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries))
						lst.Add (new KeyValuePair<int, string> (last = ((last < 0) ? 0 : ((last == 0) ? 2 : (last * 2))), k.Trim ()));
					if (lst.Count == 0)
						lst.Add (new KeyValuePair<int, string> (0, GetTitle ()));
					editions.Clear ();
					editions.AddRange (lst);
				}
			return editions;
		}

		private static string GetMapping () {
			if (mapkey == null)
				mapkey = GetProductResource ("_MappingKey");
			return mapkey;
		}

		private static string GetName () {
			if (pname == null)
				pname = AssemblyName;
			return pname;
		}

		private static T [] GetRange<T> (T [] arr, int startIndex, int length) {
			T [] t = new T [length];
			Array.Copy (arr, startIndex, t, 0, length);
			return t;
		}

		internal static T GetStatus<T> (SPContext context) where T : class {
			List<object> datas = new List<object> ();
			List<Dictionary<string, object>> sds = new List<Dictionary<string, object>> ();
			byte [] bytes;
			string m = GetMapping ();
			object status = null;
			bool enable = false;
			Guid lastStatusID = Guid.Empty;
			Dictionary<string, object> thesd;
			Dictionary<string, object> sdo = new Dictionary<string, object> ();
			SPWebApplication adminApp = GetAdminApplication ();
			SPSite site = OpenSite ((context == null) ? (context = GetContext ()) : context);
			Converter<SPPersistedObject, byte []> getData = delegate (SPPersistedObject input) {
				object obj;
				if (!input.Properties.ContainsKey (input.GetType ().Name.Replace ("SP", string.Empty) + m))
					return null;
				else if ((obj = input.Properties [input.GetType ().Name.Replace ("SP", string.Empty) + m]) is byte [])
					return obj as byte [];
				else if (obj != null)
					return new byte [0];
				return null;
			};
			if (IsWhiteLabel && (adminApp != null) && (site == null))
				site = GetAdminSite ();
			try {
				enable = (HttpContext.Current.Request ["cfg"] == "enable");
			} catch {
			}
			try {
				if (((adminApp != null)) && (site != null) && ((status == null) || (lastStatusID != site.ID))) {
					lastStatusID = site.ID;
					if ((bytes = getData (adminApp)) != null)
						if (bytes.Length == 0)
							return sdo as T;
						else
							datas.Add (bytes);
					try {
						using (SymmetricAlgorithm rm = NewAlgo ())
						using (ICryptoTransform rmct = rm.CreateDecryptor (GetRange<byte> (os, os.Length - 24, 24), GetRange<byte> (os, 0, 16)))
							foreach (byte [] b in datas)
								using (MemoryStream ms = new MemoryStream (Trans (rmct, b), false))
									sds.Add ((Dictionary<string, object>) formatter.Deserialize (ms));
					} catch {
						return sdo as T;
					}
					thesd = new Dictionary<string, object> ();
					if (sds.Count > 0) {
						for (int d = 0; d < sds.Count; d++)
							if ((d == 0) || (((long) sds [d] [tk]) < ((long) thesd [tk])))
								thesd [tk] = sds [d] [tk];
						sdo [tk] = thesd [tk];
						UpdateInfo (sdo);
						for (int d = 0; d < sds.Count; d++)
							if (d == 0) {
								foreach (string k in sds [d].Keys)
									if (k != tk)
										thesd [k] = sds [d] [k];
							} else if (!Deep (thesd, sds [d]))
								return sdo as T;
					} else if (datas.Count > 0)
						return sdo as T;
					else {
						thesd [tk] = DateTime.Today.Ticks;
						isEnabled = true;
						if (site.WebApplication.Id.Equals (adminApp.Id) && (enable || IsFarmAdministrator (adminApp.Farm)))
							try {
								UpdateStatus (thesd, false);
							} catch (Exception ex) {
								errorMessage = ex.Message;
								isEnabled = false;
							}
						else {
							errorMessage = "Please notify ROXORITY of this message--thanks!";
							isEnabled = false;
						}
					}
					status = thesd;
				}
				UpdateInfo (status as Dictionary<string, object>);
			} finally {
				if (site != null)
					site.Dispose ();
			}
			return status as T;
		}

		private static object In (SPContext context, IDictionary value, Guid id) {
			long f4;
			Dictionary<string, object> dic = null;
			SPSite site = Elevated ? OpenSite (context) : GetSite (context);
			string gid = Guid.Empty.ToString (), fid = GetFarm (context).Id.ToString (), sid = site.ID.ToString (), l, c, f1, f2, f3;
			try {
				if ((value is Dictionary<string, object>) && value.Contains ("l") && value.Contains ("f1") && value.Contains ("f2") && value.Contains ("f3") && value.Contains ("c"))
					dic = value as Dictionary<string, object>;
				else {
					if (!(value.Contains (sid) || value.Contains (fid) || value.Contains (gid)))
						return null;
					if (value.Contains (fid)) {
						dic = value [fid] as Dictionary<string, object>;
						id = new Guid (fid);
					}
					if ((dic == null) && (value.Contains (sid))) {
						dic = value [sid] as Dictionary<string, object>;
						id = new Guid (sid);
					}
					if ((dic == null) && (value.Contains (gid))) {
						dic = value [gid] as Dictionary<string, object>;
						id = Guid.Empty;
					}
				}
				if (dic != null) {
					if (!(dic.ContainsKey ("l") && (!string.IsNullOrEmpty (l = dic ["l"] as string)) && dic.ContainsKey ("f1") && (!string.IsNullOrEmpty (f1 = dic ["f1"] as string)) && dic.ContainsKey ("f2") && (!string.IsNullOrEmpty (f2 = dic ["f2"] as string)) && dic.ContainsKey ("f3") && (!string.IsNullOrEmpty (f3 = dic ["f3"] as string)) && dic.ContainsKey ("c") && (!string.IsNullOrEmpty (c = dic ["c"] as string))))
						return null;
					using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider ()) {
						rsa.ImportCspBlob (os);
						if ((f1 == "0") && (id.Equals (new Guid (fid)) || id.Equals (Guid.Empty)))
							return null;
						else if (Verify (rsa, Convert.FromBase64String (l + "=="), id, c, int.Parse (f1), int.Parse (f2), int.Parse (f3), (dic.ContainsKey ("f4") && long.TryParse (dic ["f4"] + "", out f4)) ? f4 : 0) != 128)
							return null;
						else if ((int.Parse (f2) > 0) && (GetUsers (context) > int.Parse (f2))) {
							usersDic.Clear ();
							usersDic [f2] = GetUsers (context);
							return usersDic;
						}
					}
				}
				return dic;
			} finally {
				if (Elevated && (site != null))
					site.Dispose ();
			}
		}

		private static SymmetricAlgorithm NewAlgo () {
			SymmetricAlgorithm rm = new TripleDESCryptoServiceProvider ();
			return rm;
		}

		private static byte [] ToByteArray (Guid id, string value, int flag1, int flag2, int flag3, long flag4) {
			List<byte> bytes = new List<byte> ();
			StringBuilder buffer = new StringBuilder ();
			bytes.AddRange (id.ToByteArray ());
			bytes.Add ((byte) flag1);
			using (MemoryStream ms = new MemoryStream ())
			using (BinaryWriter bw = new BinaryWriter (ms)) {
				bw.Write (flag2);
				bytes.AddRange (ms.ToArray ());
			}
			bytes.Add ((byte) flag3);
			if (flag4 > 0)
				using (MemoryStream ms = new MemoryStream ())
				using (BinaryWriter bw = new BinaryWriter (ms)) {
					bw.Write (flag4);
					bytes.AddRange (ms.ToArray ());
				}
			while (value.Contains ("  "))
				value = value.Replace ("  ", " ");
			buffer.Append (value.ToLowerInvariant ().Trim ());
			for (int c = 0; c < buffer.Length; c++)
				if (!char.IsLetterOrDigit (buffer [c]))
					buffer [c] = '_';
			bytes.AddRange (Encoding.Unicode.GetBytes (buffer.ToString ()));
			return bytes.ToArray ();
		}

		private static byte [] Trans (ICryptoTransform ct, byte [] sb) {
			using (MemoryStream ms = new MemoryStream ()) {
				using (CryptoStream cs = new CryptoStream (ms, ct, CryptoStreamMode.Write)) {
					cs.Write (sb, 0, sb.Length);
					cs.FlushFinalBlock ();
				}
				return ms.ToArray ();
			}
		}

		internal static string ApplyCore (SPList list, string viewXml, XmlDocument doc, ArrayList flist, ref bool expandGroups, bool outerOr, Hashtable filterHierarchy, string [] camlSourceFilters) {
			string myFilters = string.Empty, fname, tname = string.Empty, nuval, tmp, tmp2, valKey;
			bool isAnd, isDate, isNum;
			int c, groupID = -1;
			decimal dec;
			XmlDocument doc2 = new XmlDocument ();
			Dictionary<string, CamlOperator> fvals, fNuVals;
			Dictionary<string, string> subCamls = new Dictionary<string, string> ();
			XmlNode queryNode, whereNode, origNode, lastNode = null, lastSubNode = null, filterNode = null, tmpNode;
			XmlAttribute attNode;
			SPField field;
			SPFieldCalculated calcField;
			SPFieldNumber numField;
			SPGroupCollection [] groups;
			Converter<DictionaryEntry, XmlNode> createFromHierarchy = null;
			createFromHierarchy = delegate (DictionaryEntry entry) {
				string [] subs = { string.Empty, string.Empty };
				string v;
				ArrayList vals = (ArrayList) entry.Value;
				Hashtable ht;
				XmlNode node = doc.CreateElement ("OR".Equals (entry.Key) ? "Or" : "And"), subNode;
				for (int i = 0; i < 2; i++)
					if ((!string.IsNullOrEmpty (v = vals [i] as string)) && subCamls.ContainsKey (v))
						subs [i] = subCamls [vals [i] as string];
					else if (((ht = vals [i] as Hashtable) != null) && (ht.Count == 1))
						foreach (DictionaryEntry subEntry in ht)
							if (((subNode = createFromHierarchy (subEntry)) != null) && (subNode.HasChildNodes))
								subs [i] = ((subNode.ChildNodes.Count == 1) ? subNode.FirstChild : subNode).OuterXml;
				node.InnerXml = subs [0] + subs [1];
				return ((!node.HasChildNodes) ? null : ((node.ChildNodes.Count == 1) ? node.FirstChild : node));
			};
			if (doc.DocumentElement == null)
				doc.LoadXml ("<View/>");
			if ((queryNode = doc.DocumentElement.SelectSingleNode ("Query")) == null)
				queryNode = doc.DocumentElement.AppendChild (doc.CreateElement ("Query"));
			if ((whereNode = queryNode.SelectSingleNode ("Where")) == null)
				whereNode = queryNode.AppendChild (doc.CreateElement ("Where"));
			origNode = lastNode = whereNode.FirstChild;
			foreach (Hashtable ht in flist) {
				filterNode = null;
				c = 0;
				fname = ht ["k"] + string.Empty;
				fNuVals = new Dictionary<string, CamlOperator> ();
				fvals = new Dictionary<string, CamlOperator> ();
				isAnd = (bool) ((Hashtable) ht ["v"]) ["v"];
				foreach (Hashtable ht2 in ((ArrayList) ((Hashtable) ht ["v"]) ["k"]))
					if (fvals.ContainsKey (ht2 ["k"] + string.Empty))
						fvals [ht2 ["k"] + string.Empty] = CamlOperator.Eq;
					else
						fvals [ht2 ["k"] + string.Empty] = (CamlOperator) Enum.Parse (typeof (CamlOperator), ht2 ["v"] + string.Empty, true);
				foreach (KeyValuePair<string, CamlOperator> kvp in fvals) {
					if (((int) kvp.Value) > 7)
						if (kvp.Value == CamlOperator.RangeGeLe)
							fNuVals [kvp.Key] = ((fvals.Count == 1) ? CamlOperator.Eq : ((c == 0) ? CamlOperator.Geq : CamlOperator.Leq));
						else if (kvp.Value == CamlOperator.RangeGeLt)
							fNuVals [kvp.Key] = ((fvals.Count == 1) ? CamlOperator.Eq : ((c == 0) ? CamlOperator.Geq : CamlOperator.Lt));
						else if (kvp.Value == CamlOperator.RangeGtLe)
							fNuVals [kvp.Key] = ((fvals.Count == 1) ? CamlOperator.Eq : ((c == 0) ? CamlOperator.Gt : CamlOperator.Leq));
						else if (kvp.Value == CamlOperator.RangeGtLt)
							fNuVals [kvp.Key] = ((fvals.Count == 1) ? CamlOperator.Eq : ((c == 0) ? CamlOperator.Gt : CamlOperator.Lt));
					c++;
				}
				foreach (KeyValuePair<string, CamlOperator> kvp in fNuVals)
					fvals [kvp.Key] = kvp.Value;
				try {
					if ((field = ProductPage.GetField (list, fname)) != null)
						fname = field.InternalName;
				} catch {
					field = null;
				}
				foreach (KeyValuePair<string, CamlOperator> val in fvals) {
					nuval = null;
					isDate = false;
					if ((camlSourceFilters != null) && (Array.IndexOf<string> (camlSourceFilters, fname) >= 0)) {
						doc2.LoadXml (val.Key);
						lastSubNode = doc.ImportNode (doc2.DocumentElement, true);
					} else {
						lastSubNode = doc.CreateElement ((val.Value == CamlOperator.Member) ? "Membership" : val.Value.ToString ());
						if (val.Value == CamlOperator.Member) {
							lastSubNode.Attributes.Append (lastSubNode.OwnerDocument.CreateAttribute ("Type")).Value = "SPGroup";
							if (!int.TryParse (valKey = val.Key, out groupID)) {
								groups = new SPGroupCollection [2];
								try {
									groups [0] = SPContext.Current.Web.Groups;
								} catch {
								}
								try {
									groups [1] = SPContext.Current.Web.SiteGroups;
								} catch {
								}
								foreach (SPGroupCollection groupCol in groups)
									if (groupCol != null)
										foreach (SPGroup group in ProductPage.TryEach<SPGroup> (groupCol))
											if (group.Name == valKey) {
												groupID = group.ID;
												break;
											}
							}
							lastSubNode.Attributes.Append (lastSubNode.OwnerDocument.CreateAttribute ("ID")).Value = groupID.ToString ();
						}
						lastSubNode.AppendChild (doc.CreateElement ("FieldRef")).Attributes.SetNamedItem (doc.CreateAttribute ("Name")).Value = fname;
						if (val.Value != CamlOperator.Member)
							lastSubNode.AppendChild (doc.CreateElement ("Value")).Attributes.SetNamedItem (doc.CreateAttribute ("Type")).Value = ((field == null) ? "Text" : field.TypeAsString);
						if (field != null) {
							calcField = field as SPFieldCalculated;
							dec = -1;
							if (isNum = (((((numField = field as SPFieldNumber) != null) && numField.ShowAsPercentage) || (((calcField) != null) && (calcField.OutputType == SPFieldType.Number) && calcField.ShowAsPercentage)) && (decimal.TryParse (val.Key, out dec) || (val.Key.EndsWith ("%") && decimal.TryParse (val.Key, out dec)))))
								if (dec > 1)
									nuval = (dec / 100).ToString ();
								else
									nuval = ConvertNumberForCaml (dec.ToString ());
							if ((field.Type == SPFieldType.Invalid) && (field.FieldTypeDefinition != null) && !string.IsNullOrEmpty (field.FieldTypeDefinition.BaseRenderingTypeName))
								lastSubNode.LastChild.Attributes.GetNamedItem ("Type").Value = tname = field.FieldTypeDefinition.BaseRenderingTypeName;
							if ((field.Type == SPFieldType.Boolean) && string.IsNullOrEmpty (val.Key)) {
								tmp = lastSubNode.FirstChild.OuterXml;
								tmp2 = lastSubNode.OuterXml;
								lastSubNode = lastSubNode.OwnerDocument.CreateElement ("Or");
								lastSubNode.AppendChild (lastSubNode.OwnerDocument.CreateElement ("IsNull"));
								lastSubNode.FirstChild.InnerXml = tmp;
								lastSubNode.InnerXml += tmp2;
							}
							if (isDate = ((tname == "DateTime") || (field.Type == SPFieldType.DateTime) || ((calcField != null) && (calcField.OutputType == SPFieldType.DateTime)))) {
								if (string.IsNullOrEmpty (val.Key)) {
									tmp = lastSubNode.FirstChild.OuterXml;
									lastSubNode = lastSubNode.OwnerDocument.CreateElement ("IsNull");
									lastSubNode.InnerXml = tmp;
								} else {
									nuval = SPUtility.CreateISO8601DateTimeFromSystemDateTime (ConvertStringToDate (val.Key));
									if ((attNode = lastSubNode.LastChild.Attributes.GetNamedItem ("IncludeTimeValue") as XmlAttribute) == null)
										attNode = lastSubNode.LastChild.Attributes.Append (doc.CreateAttribute ("IncludeTimeValue"));
									attNode.Value = "FALSE";
								}
								if (calcField != null)
									lastSubNode.LastChild.Attributes.GetNamedItem ("Type").Value = calcField.OutputType.ToString ();
							}
							if ((!isDate) && (!isNum) && ((calcField != null) || (tname == "Calculated")) && (val.Value != CamlOperator.Eq) && (val.Value != CamlOperator.Neq))
								if ((val.Value == CamlOperator.BeginsWith) || (val.Value == CamlOperator.Contains))
									lastSubNode.LastChild.Attributes.GetNamedItem ("Type").Value = "Text";
								else
									lastSubNode.LastChild.Attributes.GetNamedItem ("Type").Value = ((calcField == null) ? "Number" : calcField.OutputType.ToString ());
						} else
							lastSubNode.LastChild.InnerText = val.Key;
						if ((val.Value != CamlOperator.Member) && (field != null) && (!((field.Type == SPFieldType.Boolean) && string.IsNullOrEmpty (val.Key))))
							lastSubNode.LastChild.InnerText = string.IsNullOrEmpty (nuval) ? val.Key : nuval;
						if (((attNode = lastSubNode.FirstChild.Attributes.GetNamedItem ("Name") as XmlAttribute) != null) && (Array.IndexOf<string> (new string [] { "LinkFilename", "LinkFilenameNoMenu" }, attNode.Value) >= 0))
							attNode.Value = "FileLeafRef";
					}
					if (filterNode == null)
						filterNode = lastSubNode;
					else {
						tmpNode = filterNode;
						filterNode = doc.CreateElement (isAnd ? "And" : "Or");
						filterNode.AppendChild (tmpNode);
						filterNode.AppendChild (lastSubNode);
					}
				}
				if (filterNode != null) {
					subCamls [fname] = filterNode.OuterXml;
					if (lastNode == null)
						lastNode = filterNode;
					else {
						tmpNode = lastNode;
						lastNode = doc.CreateElement (outerOr ? "Or" : "And");
						lastNode.AppendChild (tmpNode);
						lastNode.AppendChild (filterNode);
					}
				}
			}
			if ((filterHierarchy != null) && (filterHierarchy.Count == 1))
				foreach (DictionaryEntry entry in filterHierarchy)
					lastNode = createFromHierarchy (entry);
			if (lastNode != null) {
				whereNode.RemoveAll ();
				whereNode.AppendChild (lastNode);
			} else if (whereNode != null)
				queryNode.RemoveChild (whereNode);
			if (expandGroups) {
				expandGroups = false;
				if (((lastNode = doc.DocumentElement.SelectSingleNode ("Query/GroupBy")) != null) && ((attNode = lastNode.Attributes.GetNamedItem ("Collapse") as XmlAttribute) != null) && attNode.Value.Equals ("true", StringComparison.InvariantCultureIgnoreCase)) {
					attNode.Value = "FALSE";
					expandGroups = true;
				}
			}
			if (list == null)
				if ((lastNode = doc.SelectSingleNode ("//Where")) != null)
					doc.LoadXml (lastNode.OuterXml);
				else
					return string.Empty;
			return doc.DocumentElement.OuterXml;
		}

		internal static void Check (string cap, bool noTrial) {
			LicInfo li = LicInfo.Get (null);
			if (noTrial && string.IsNullOrEmpty (li.name))
				throw new Exception (GetResource ("NopeTrial", GetProductResource ("Cap_" + cap)));
			else if (li.expired)
				throw new Exception (GetResource ("NopeExpired", GetProductResource ("Cap_" + cap)));
		}

		internal static string ConvertDateIf (string val, bool convert) {
			return convert ? ConvertDateToString (ConvertStringToDate (val)) : val;
		}

		internal static string ConvertDateNoTimeIf (string val, bool convertDate, bool stripTime) {
			int pos;
			return (((pos = (val = (convertDate ? ConvertDateToString (ConvertStringToDate (val)) : val)).IndexOf (' ')) > 0) && stripTime) ? val.Substring (0, pos) : val;
		}

		internal static string ConvertDateToString (DateTime value) {
			return ConvertDateToString (value, string.Empty, null);
		}

		internal static string ConvertDateToString (DateTime value, string format, CultureInfo culture) {
			if (culture == null)
				culture = CultureInfo.CurrentCulture;
			if (string.IsNullOrEmpty (format))
				format = culture.DateTimeFormat.ShortDatePattern;
			if (value.TimeOfDay.Ticks != 0L)
				format += @" HH\:mm\:ss";
			return value.ToString (format, culture);
		}

		internal static string ConvertNumberForCaml (string value) {
			string nuVal = string.Empty;
			foreach (char c in value)
				if (char.IsNumber (c))
					nuVal += c;
				else if (CultureInfo.CurrentUICulture.NumberFormat.NumberGroupSeparator.Equals (c.ToString ()))
					nuVal += CultureInfo.InvariantCulture.NumberFormat.NumberGroupSeparator;
				else if (CultureInfo.CurrentUICulture.NumberFormat.NumberDecimalSeparator.Equals (c.ToString ()))
					nuVal += CultureInfo.InvariantCulture.NumberFormat.NumberDecimalSeparator;
			return nuVal;
		}

		internal static DateTime ConvertStringToDate (string value) {
			return ConvertStringToDate (value, null);
		}

		internal static DateTime ConvertStringToDate (string value, CultureInfo culture) {
			DateTime dt = DateTime.MaxValue;
			if (culture == null)
				culture = CultureInfo.CurrentCulture;
			if (value != null)
				DateTime.TryParse (value, culture, DateTimeStyles.AllowWhiteSpaces, out dt);
			return dt;
		}

		internal static void CreateLicControls (ControlCollection controls, string prefixControl, string suffixControl) {
			controls.Add (new LiteralControl (string.Format ("<span id=\"roxlicsection\">" + prefixControl, string.Empty, "<div class=\"rox-wplicdiv\"><a target=\"_blank\" href=\"http://SharePoint-Tools.net\"><img border=\"0\" align=\"right\" src=\"/_layouts/" + AssemblyName + "/help/res/roxority.tlhr.png\" width=\"96\"/></a>" + GetResource ("HelpText") + " <a target=\"_blank\" href=\"" + SPContext.Current.Site.Url + "/_layouts/" + GetProductResource ("_WhiteLabel") + GetTitle () + ".aspx\">" + GetResource ("HelpTitle", ProductPage.GetTitle ()) + "</a>.</div>" + GetLicHtml (), "")));
			controls.Add (new LiteralControl (suffixControl + "<a name=\"roxtooltop\"></a></span>"));
		}

		internal static string CreateSimpleCamlNode (string outerNodeName, string innerNodeName, string fieldRefName, string fieldRefType, string value) {
			XmlDocument doc = new XmlDocument ();
			XmlNode elem;
			XmlAttribute att;
			if ((fieldRefType == "BusinessData") || (fieldRefType == "Computed") || (fieldRefType == "Calculated"))
				fieldRefType = "Text";
			doc.LoadXml (string.Format (string.IsNullOrEmpty (outerNodeName) ? "<{1}><FieldRef Name=\"\"/><Value Type=\"{2}\"></Value></{1}>" : "<{0}><{1}><FieldRef Name=\"\"/><Value Type=\"{2}\"></Value></{1}></{0}>", outerNodeName, innerNodeName, fieldRefType));
			(elem = (string.IsNullOrEmpty (outerNodeName) ? doc.DocumentElement : doc.DocumentElement.FirstChild)).FirstChild.Attributes [0].Value = fieldRefName;
			if (fieldRefType == "Number")
				value = ConvertNumberForCaml (value);
			else if (fieldRefType == "DateTime")
				try {
					value = SPUtility.CreateISO8601DateTimeFromSystemDateTime (ConvertStringToDate (value));
					if ((att = elem.LastChild.Attributes.GetNamedItem ("IncludeTimeValue") as XmlAttribute) == null)
						att = elem.LastChild.Attributes.Append (doc.CreateAttribute ("IncludeTimeValue"));
					att.Value = "FALSE";
					//value = ConvertDateToString (ConvertStringToDate (value), CultureInfo.CurrentUICulture.DateTimeFormat.ShortDatePattern, CultureInfo.CurrentUICulture);
				} catch {
				}
			elem.LastChild.InnerText = value;
			return doc.DocumentElement.OuterXml;
		}

		internal static Control FindControl (ControlCollection controls, string id) {
			Control c;
			foreach (Control ctl in controls)
				if (id.Equals (ctl.ID))
					return ctl;
				else if ((c = FindControl (ctl.Controls, id)) != null)
					return c;
			return null;
		}

		public static string FindControlClientID (ControlCollection controls, string id) {
			Control ctl = FindControl (controls, id);
			return ((ctl == null) ? null : ctl.ClientID);
		}

		public static CultureInfo GetFarmCulture (SPContext context) {
			SPSite site = Elevated ? OpenSite (context) : GetSite (context);
			SPFarm farm = GetFarm (context);
			CultureInfo theCulture = null;
			string langConfig;
			if (site != null) {
				if (siteCulture == null)
					if (string.IsNullOrEmpty (langConfig = Config (context, "_lang")))
						siteCulture = Thread.CurrentThread.CurrentUICulture;
					else
						siteCulture = new CultureInfo (langConfig);
				theCulture = siteCulture;
			} else {
				if (farmCulture == null)
					if (string.IsNullOrEmpty (langConfig = Config (context, "_lang")))
						farmCulture = Thread.CurrentThread.CurrentUICulture;
					else
						farmCulture = new CultureInfo (langConfig);
				theCulture = farmCulture;
			}
			if (Elevated)
				site.Dispose ();
			return theCulture;
		}

		internal static object GetFieldProp (SPField field, string name) {
			LocalDataStoreSlot slot = Thread.GetNamedDataSlot (field.GetType ().Name + "_" + name);
			object val = ((slot == null) ? null : Thread.GetData (slot));
			return ((val == null) ? field.GetCustomProperty (name) : val);
		}

		public static string GetEdition (int key) {
			int max = -1;
			foreach (KeyValuePair<int, string> kvp in GetEditions ()) {
				if (kvp.Key > max)
					max = kvp.Key;
				if (kvp.Key == key)
					return (((kvp.Key == 0) || !IsWhiteLabel) ? kvp.Value : GetProductResource ("_WhiteLabelLicensed"));
			}
			return (((key < 0) && (max >= 0)) ? GetEdition (max) : (IsWhiteLabel ? GetProductResource ("_WhiteLabelLicensed") : "UNKNOWN"));
		}

		public static MailAddress GetEmailAddress (string value) {
			int pos = value.IndexOf ('@');
			MailAddress addr = null;
			if ((pos > 0) && (value.LastIndexOf ('.') > (pos + 2)))
				try {
					addr = new MailAddress (value);
				} catch {
				}
			return addr;
		}

		public static void ImportFarmSettings (string fileName) {
			bool isCfgFarm, isCfgSite, isFjs, isSjs, isLoc;
			string fileContent, key, prodName = AssemblyName, tmp;
			int pos;
			CultureInfo loc;
			SPFarm farm = SPFarm.Local;
			SPSite spAdminSite = ProductPage.GetAdminSite ();
			SPWeb spAdminWeb = spAdminSite.RootWeb;
			SPContext spCtx = SPContext.GetContext (spAdminWeb);
			HttpContext context = HttpContext.Current;
			ProductPage prodPage = new ProductPage ();
			Hashtable temp, tmp2, import, spProps = farm.Properties, fjss = new Hashtable (), sjss = new Hashtable (), jsht;
			JsonSchemaManager jsm = null;
			JsonSchemaManager.Schema schema;
			Converter<KeyValuePair<string, bool>, JsonSchemaManager> getSchemaMan = delegate (KeyValuePair<string, bool> what) {
				KeyValuePair<JsonSchemaManager, JsonSchemaManager> kvp;
				if (!fjss.ContainsKey (what.Key)) {
					kvp = JsonSchemaManager.TryGet (prodPage, SPUtility.GetGenericSetupPath ("template/layouts/" + prodName + "/" + what.Key), true, true, what.Key.EndsWith ("schemas.tl.json") ? "roxority_Shared" : null);
					fjss [what.Key] = kvp.Key;
					sjss [what.Key] = kvp.Value;
				} else
					kvp = new KeyValuePair<JsonSchemaManager, JsonSchemaManager> (fjss [what.Key] as JsonSchemaManager, sjss [what.Key] as JsonSchemaManager);
				return what.Value ? kvp.Key : kvp.Value;
			};
			if (File.Exists (fileName = prodName + ".export.rox") && (!string.IsNullOrEmpty (fileContent = File.ReadAllText (fileName))) && ((tmp2 = JSON.JsonDecode (fileContent) as Hashtable) != null) && (tmp2.Count > 0)) {
				import = tmp2;
				foreach (DictionaryEntry entry in import) {
					isCfgFarm = "farm".Equals (entry.Key);
					isCfgSite = "site".Equals (entry.Key);
					isFjs = "fjs".Equals (entry.Key);
					isSjs = "sjs".Equals (entry.Key);
					isLoc = !(isCfgSite || isCfgFarm || isFjs || isSjs);
					foreach (DictionaryEntry e2 in ((Hashtable) entry.Value))
						try {
							if (isLoc)
								ProductPage.Loc (null, farm, spProps, e2.Key + string.Empty, null, null, e2.Value + string.Empty);
							else if (isCfgFarm /*|| isCfgSite*/)
								ProductPage.Config<string> (spCtx, e2.Key + string.Empty, e2.Value + string.Empty, isCfgSite);
							else if ((isFjs /*|| isSjs*/) && ((pos = (tmp = e2.Key + string.Empty).IndexOf (':')) > 0) && ((jsm = getSchemaMan (new KeyValuePair<string, bool> (tmp.Substring (0, pos), isFjs))) != null) && jsm.AllSchemas.TryGetValue (tmp.Substring (pos + 1), out schema))
								if (e2.Value is IDictionary)
									schema.Import (e2.Value as IDictionary);
								else
									schema.Import (e2.Value + string.Empty);
						} catch {
						}
				}
			}
		}

		public static bool IsFarmAdministrator (SPFarm farm) {
			if (Is14 && (farmMethod == null) && !farmMethodTried)
				try {
					farmMethodTried = true;
					farmMethod = farm.GetType ().GetMethod ("CurrentUserIsAdministrator", BindingFlags.Public | BindingFlags.Instance, null, new Type [] { typeof (bool) }, null);
				} catch {
				}
			if (farmMethod != null)
				try {
					return (bool) farmMethod.Invoke (farm, new object [] { true });
				} catch {
				}
			return farm.CurrentUserIsAdministrator ();
		}

		public static bool IsFarmOnlySetting (string config) {
			string cfg = GetProductResource ("_SettingsFarmOnly");
			string [] cfgs;
			if ((!string.IsNullOrEmpty (cfg)) && ((cfgs = cfg.Split (new char [] { ',' }, StringSplitOptions.RemoveEmptyEntries)) != null))
				return (Array.IndexOf<string> (cfgs, config) >= 0);
			return false;
		}
		// FILTER_END

		public static SPField GetField (SPList list, Guid fieldID) {
			if (list == null)
				return null;
			else
				return GetField (list.Fields, fieldID);
		}

		public static SPField GetField (SPList list, string fieldName) {
			return ((list == null) ? null : GetField (list.Fields, fieldName));
		}

		public static SPField GetField (SPListItem item, string fieldName) {
			return ((item == null) ? null : GetField (item.Fields, fieldName));
		}

		internal static SPField GetField (SPFieldCollection fields, Guid id) {
			foreach (SPField f in TryEach<SPField> (fields))
				if (id.Equals (f.Id))
					return f;
			return null;
		}

		internal static SPField GetField (SPFieldCollection fields, string fieldName) {
			SPField namedField = null;
			SPField titledField = null;
			if (fields != null) {
#if !DEBUG
				try {
					namedField = fields.GetFieldByInternalName (fieldName);
				} catch {
				}
				if (namedField == null)
#endif
				foreach (SPField field in TryEach<SPField> (fields))
					if (field.InternalName == fieldName)
						return field;
					else if ((!string.IsNullOrEmpty (field.InternalName)) && (field.InternalName.Equals (fieldName, StringComparison.InvariantCultureIgnoreCase)))
						namedField = field;
					else if ((!string.IsNullOrEmpty (field.Title)) && (field.Title.Trim ().Equals (fieldName.Trim (), StringComparison.InvariantCultureIgnoreCase)))
						titledField = field;
			}
			return (namedField != null) ? namedField : titledField;
		}

		public static object GetFieldVal (SPListItem item, SPField field, bool returnException) {
			try {
				if (Guid.Empty.Equals (field.Id))
					return item [field.InternalName];
				else
					return item [field.Id];
			} catch (Exception ex) {
				return returnException ? ex : null;
			}
		}

		public static Guid GetGuid (string value) {
			return GetGuid (value, false);
		}

		public static Guid GetGuid (string value, bool force) {
			if (!force)
				return IsGuid (value) ? new Guid (value) : Guid.Empty;
			else
				try {
					return new Guid (value);
				} catch {
					return Guid.Empty;
				}
		}

		// FILTER_BEGIN
		internal static string GetLicHtml () {
			string script = string.Empty, diag = string.Empty;
			LicInfo lic = LicInfo.Get (null);
			SPSite site;
			DateTime dt;
			if (!lic.expired) {
				if (string.IsNullOrEmpty (lic.name))
					return string.Format ("<a target=\"_blank\" href=\"" + MergeUrlPaths (SPContext.Current.Web.Url, "_layouts/" + AssemblyName + ".aspx?cfg=lic") + "\" class=\"rox-licinfo\" style=\"background: green;\">{0}</a>", GetResource (DateTime.MinValue.Equals (lic.expiry) ? "LicLite" : "LicTrial"));
				else
					return string.Format ("<a target=\"_blank\" href=\"" + MergeUrlPaths (SPContext.Current.Web.Url, "_layouts/" + AssemblyName + ".aspx?cfg=lic") + "\" class=\"rox-licinfo\" style=\"background: green;\">{0}</a>", GetLicStatus (lic.dic, lic.sd, false, out dt).Replace ("b>", "i>") + GetResource ("LicTo", lic.name));
			} else {
				site = Elevated ? ProductPage.OpenSite (null) : ProductPage.GetSite (null);
				diag = string.Format ("b: {0} ed: {1} ey: {2} is: {3} s: {8} mu: {4} n: {5} su: {6} ub: {7}", lic.broken, lic.expired, lic.expiry, lic.installSpan, lic.maxUsers, lic.name, lic.siteUsers, lic.userBroken, ((site == null) ? string.Empty : site.ID.ToString ()));
				if ((site != null) && Elevated)
					site.Dispose ();
				script = "<script type=\"text/javascript\" language=\"JavaScript\"> var roxLicError = '" + SPEncode.ScriptEncode (diag) + "\\n'; ";
				if (lic.error != null)
					script += "roxLicError += '\\n" + SPEncode.ScriptEncode (lic.error.ToString ()) + "'; ";
				script += "</script>";
				if (lic.userBroken)
					return string.Format (script + "<a target=\"_blank\" href=\"" + MergeUrlPaths (SPContext.Current.Web.Url, "_layouts/" + AssemblyName + ".aspx?cfg=lic") + "\" class=\"rox-licinfo\" style=\"background: gold; color: #000;\">{0}</a><div style=\"text-align: right; font-size: 9px;\"><span onclick=\"alert(roxLicError);\">:</span></div>", GetResource ("LicExpiryUsers"));
				else
					return string.Format (script + "<a target=\"_blank\" href=\"" + MergeUrlPaths (SPContext.Current.Web.Url, "_layouts/" + AssemblyName + ".aspx?cfg=lic") + "\" class=\"rox-licinfo\" style=\"background: gold; color: #000;\">{0}</a><div style=\"text-align: right; font-size: 9px;\"><span onclick=\"alert(roxLicError);\">:</span></div>", GetResource ("LicExpiry"));
			}
		}
		// FILTER_END

		//private static SPList GetList (string listUrl) {
		//    using (SPSite site = new SPSite (listUrl))
		//    using (SPWeb web = site.OpenWeb ())
		//        return GetList (web, listUrl);
		//}

		public static SPList GetList (SPWeb web, Guid id) {
			SPListCollection lists = null;
			try {
				lists = web.Lists;
			} catch {
			}
			if (lists != null)
				foreach (SPList list in TryEach<SPList> (lists))
					if (id.Equals (list.ID))
						return list;
			return null;
		}

		public static SPList GetList (SPWeb web, string url) {
			SPList list = null;
			url = MergeUrlPaths (web.Url, url);
			try {
				if ((list = web.GetList (url)) != null)
					return list;
				throw new Exception ();
			} catch {
				try {
					if ((list = web.GetListFromUrl (url)) != null)
						return list;
					throw new Exception ();
				} catch {
					try {
						return web.GetListFromWebPartPageUrl (url);
					} catch {
						return null;
					}
				}
			}
		}

		public static SPListItem GetDocument (SPList list, string fileName) {
			int pos = fileName.LastIndexOf ('/');
			SPQuery query = new SPQuery ();
			if (pos >= 0)
				fileName = fileName.Substring (pos + 1);
			try {
				query.Folder = list.RootFolder;
				query.AutoHyperlink = query.ExpandRecurrence = query.ExpandUserField = query.IncludeAttachmentVersion = query.IncludeAttachmentUrls = query.ItemIdQuery = false;
				query.IncludeAllUserPermissions = query.IncludeMandatoryColumns = query.IncludePermissions = query.IndividualProperties = true;
				query.ViewAttributes = "FailIfEmpty=\"FALSE\" RequiresClientIntegration=\"FALSE\" Threaded=\"FALSE\" Scope=\"Recursive\"";
				query.ViewFields = "<FieldRef Name=\"ID\"/><FieldRef Name=\"Title\"/><FieldRef Name=\"FileLeafRef\"/><FieldRef Name=\"LinkFilename\"/><FieldRef Name=\"LinkFilenameNoMenu\"/><FieldRef Name=\"LinkFilename\"/><FieldRef Name=\"DocIcon\"/><FieldRef Name=\"ParentLeafName\"/>";
				query.Query = "<Where><Eq><FieldRef Name=\"FileLeafRef\" /><Value Type=\"File\">" + fileName + "</Value></Eq></Where>";
				foreach (SPListItem item in list.GetItems (query))
					return item;
				foreach (SPListItem item in TryEach<SPListItem> (list.Items))
					if (GetListItemTitle (item, true).Equals (fileName, StringComparison.InvariantCultureIgnoreCase))
						return item;
			} catch {
			}
			return null;
		}

		public static string GetListItemTitle (SPListItem item, bool forceName) {
			SPField field;
			string s;
			if (!forceName) {
				try {
					s = item.Title;
				} catch {
					s = "";
				}
				if (!string.IsNullOrEmpty (s))
					return s;
				foreach (string fn in new string [] { "Title", "LinkTitle", "LinkTitleNoMenu", "LinkDiscussionTitle", "LinkDiscussionTitleNoMenu" })
					try {
						if (((field = GetField (item, fn)) != null) && (!string.IsNullOrEmpty (s = item [field.Id] + string.Empty)))
							return s;
					} catch {
					}
			}
			try {
				s = item.Name;
			} catch {
				s = "";
			}
			if (!string.IsNullOrEmpty (s))
				return s;
			foreach (string fn in new string [] { "LinkFilenameNoMenu", "LinkFilename", "FileLeafRef", "ParentLeafName", "BaseName" })
				try {
					if (!string.IsNullOrEmpty (s = item [GetField (item, fn).Id] + string.Empty))
						return s;
				} catch {
				}
			return "#" + item.ID;
		}

		internal static SPWeb GetRootWeb (SPWeb web) {
			SPSecurity.CatchAccessDeniedException = false;
			if (web != null) {
				web.Site.CatchAccessDeniedException = false;
				try {
					return (web.IsRootWeb ? web : GetRootWeb (web.ParentWeb));
				} catch {
					try {
						return web.Site.RootWeb;
					} catch {
					}
				}
			}
			return null;
		}

		// FILTER_BEGIN
		internal static string GetTitle () {
			if (name == null)
				name = AssemblyName.Substring (AssemblyName.LastIndexOf ('_') + 1);
			return name;
		}

		internal static string GuidBracedUpper (Guid guid) {
			return "{" + guid.ToString ().ToUpperInvariant ().Replace ("{", string.Empty).Replace ("}", string.Empty) + "}";
		}

		public static string GuidLower (Guid guid) {
			return GuidLower (guid, false);
		}

		public static string GuidLower (Guid guid, bool dashes) {
			return GuidBracedUpper (guid).Substring (1, 36).Replace ("-", dashes ? "-" : string.Empty).ToLowerInvariant ();
		}

		internal static DateTimeControl InitializeDateTimePicker (DateTimeControl datePicker) {
			SPContext context = GetContext ();
			SPWeb web = ((context == null) ? null : SPContext.Current.Web);
			SPRegionalSettings regionalSettings = ((web == null) ? null : ((web.CurrentUser == null) ? web.RegionalSettings : web.CurrentUser.RegionalSettings));
			if ((web != null) && (regionalSettings == null))
				regionalSettings = web.RegionalSettings;
			if (regionalSettings != null) {
				datePicker.LocaleId = (int) regionalSettings.LocaleId;
				datePicker.Calendar = (SPCalendarType) regionalSettings.CalendarType;
				datePicker.DateOnly = true;
				datePicker.FirstDayOfWeek = (int) regionalSettings.FirstDayOfWeek;
				datePicker.FirstWeekOfYear = regionalSettings.FirstWeekOfYear;
				datePicker.HijriAdjustment = regionalSettings.AdjustHijriDays;
				datePicker.HoursMode24 = regionalSettings.Time24;
				//datePicker.ShowWeekNumber = true;
				datePicker.TimeZoneID = regionalSettings.TimeZone.ID;
				datePicker.UseTimeZoneAdjustment = false;
			}
			return datePicker;
		}

		internal static void InitField (SPField field, params string [] customProperties) {
			InitField (field, true, customProperties);
		}

		internal static void InitField (SPField field, bool fallbackToSchema, params string [] customProperties) {
			XmlDocument doc = (fallbackToSchema ? new XmlDocument () : null);
			XmlAttribute att;
			PropertyInfo prop;
			object obj;
			int intVal;
			if (doc != null)
				doc.LoadXml (field.SchemaXml);
			foreach (string s in customProperties)
				if ((prop = field.GetType ().GetProperty (s)) != null)
					if ((obj = GetFieldProp (field, s)) != null)
						prop.SetValue (field, obj, null);
					else if ((doc != null) && ((att = doc.DocumentElement.Attributes [s]) != null))
						if (prop.PropertyType != typeof (int))
							prop.SetValue (field, ((prop.PropertyType == typeof (bool)) ? (object) ParseBool (att.Value) : att.Value), null);
						else if (int.TryParse (att.Value, out intVal))
							prop.SetValue (field, intVal, null);
		}
		// FILTER_END

		internal static bool IsGuid (string value) {
			return ((!string.IsNullOrEmpty (value)) && regexGuidPattern.IsMatch (value));
		}

		// FILTER_BEGIN
		internal static int LicEditions (SPContext context) {
			return LicEditions (context, null);
		}

		internal static int LicEditions (SPContext context, IDictionary lic) {
			int def = 0;
			foreach (KeyValuePair<int, string> kvp in GetEditions ())
				def |= kvp.Key;
			return LicInt (context, lic, "f3", def);
		}

		internal static int LicInt (SPContext context, string key, int def) {
			return LicInt (context, null, key, def);
		}

		internal static int LicInt (SPContext context, IDictionary lic, string key, int def) {
			if (lic == null)
				lic = LicObject (context);
			if (lic != null)
				return int.Parse (lic [key] as string);
			return def;
		}

		internal static string LicName (SPContext context) {
			return LicName (context, null);
		}

		internal static string LicName (SPContext context, IDictionary lic) {
			if (lic == null)
				lic = LicObject (context);
			return ((lic == null) ? null : lic ["c"] as string);
		}

		internal static IDictionary LicObject (SPContext context) {
			string id;
			IDictionary sd = GetStatus<IDictionary> (context);
			SPSite site = Elevated ? OpenSite (context) : GetSite (context);
			try {
				if ((sd != null) && (sd.Contains (id = site.ID.ToString ()) || sd.Contains (id = GetFarm (context).Id.ToString ()) || sd.Contains (id = Guid.Empty.ToString ())))
					return sd [id] as IDictionary;
				return null;
			} finally {
				if ((site != null) && Elevated)
					site.Dispose ();
			}
		}

		internal static int LicTargetType (SPContext context) {
			return LicTargetType (context, null);
		}

		internal static int LicTargetType (SPContext context, IDictionary lic) {
			return LicInt (context, lic, "f1", 0);
		}

		internal static int LicUsers (SPContext context) {
			return LicUsers (context, null);
		}

		internal static int LicUsers (SPContext context, IDictionary lic) {
			return LicInt (context, lic, "f2", 0);
		}

		public static string GetLicStatus (IDictionary dic, IDictionary sd, bool detailed, out DateTime expiry) {
			int users = 0, scope = 0, range = 0;
			long exp = 0, timeLeft;
			string defStatus = "", prev = string.Empty;
			TimeSpan ts;
			LicInfo lic = LicInfo.Get (dic);
			expiry = DateTime.MinValue;
			if (dic != null) {
				if (dic.Contains ("f1"))
					int.TryParse (dic ["f1"] + string.Empty, out range);
				if ((dic.Contains ("f4")) && long.TryParse (dic ["f4"] + string.Empty, out exp) && (exp > 0))
					expiry = new DateTime (exp);
				if (dic.Contains ("f2"))
					int.TryParse (dic ["f2"] + string.Empty, out users);
				if (dic.Contains ("f3"))
					int.TryParse (dic ["f3"] + string.Empty, out scope);
				if ((range > 1) && (scope > 0) && (sd != null) && (expiry == DateTime.MinValue) && (sd.Contains ("ed")) && (sd ["ed"] is DateTime)) {
					exp = (expiry = (DateTime) sd ["ed"]).Ticks;
					if (expiry >= DateTime.Now)
						scope = 0;
				}
				if (scope == 6)
					defStatus = ProductPage.GetEdition (4);
				else
					defStatus = ProductPage.GetEdition (scope);
			} else if (sd != null) {
				if (sd.Contains ("ed") && (sd ["ed"] is DateTime))
					exp = (expiry = (DateTime) sd ["ed"]).Ticks;
				if (ProductPage.GetEdition (0).Equals (defStatus = ((expiry > DateTime.Now) ? (IsWhiteLabel ? GetProductResource ("_WhiteLabelUnlicensed") : GetEdition (-1)) : ((sd.Count > 3) ? ProductPage.GetEdition (0) : ""))))
					prev = ProductPage.GetEdition (-1);
			} else
				defStatus = GetResource ("LicStatusBroken");
			if (exp <= 0) {
				if ((lic.siteUsers <= 10) && HasMicro)
					defStatus = "<b>" + ProductPage.GetEdition (-1) + "</b>";
				else if (IsTheThing (dic))
					defStatus = "<b>" + ProductPage.GetEdition (0) + "</b> " + (detailed ? GetResource ("LicStatusDowned") : string.Empty);
				else
					defStatus = "<b>" + defStatus + "</b>";
			} else if ((timeLeft = exp - DateTime.Now.Ticks) > 0)
				defStatus = "<b>" + defStatus + "</b> " + (detailed ? GetResource ("LicStatusExpires", (((ts = new TimeSpan (Math.Abs (timeLeft))).Days == 0) ? 1 : ts.Days)) : string.Empty);
			else
				defStatus = ("<b>" + (string.IsNullOrEmpty (defStatus) ? GetResource ("None") : ProductPage.GetEdition (0)) + "</b> " + (detailed ? GetResource ("LicStatusExpired", (((ts = new TimeSpan (Math.Abs (timeLeft))).Days == 0) ? 1 : ts.Days), string.IsNullOrEmpty (prev) ? defStatus : prev) : string.Empty));
			return defStatus;
		}
		// FILTER_END

		internal static void RemoveDuplicates<T> (List<T> list) {
			for (int i = list.Count - 1; i >= 0; i--)
				if (list.IndexOf (list [i]) < i)
					list.RemoveAt (i);
		}

		internal static void RemoveDuplicates<TKey, TValue> (List<KeyValuePair<TKey, TValue>> list) {
			for (int i = list.Count - 1; i >= 0; i--)
				if (list.FindIndex (delegate (KeyValuePair<TKey, TValue> val) {
					return (Equals (val.Key, list [i].Key) && Equals (val.Value, list [i].Value));
				}) < i)
					list.RemoveAt (i);
		}

		// FILTER_BEGIN
		internal static void SetFieldProp (SPField field, string name, object val) {
			LocalDataStoreSlot slot = Thread.GetNamedDataSlot (field.GetType ().Name + "_" + name);
			field.SetCustomProperty (name, val);
			if (slot != null)
				Thread.SetData (slot, val);
		}

		internal static string StripID (string value) {
			int pos = value.IndexOf (SEPARATOR);
			return ((pos >= 0) ? value.Substring (pos + SEPARATOR.Length) : value);
		}

		internal static void UpdateField (SPField field, params string [] customProperties) {
			PropertyInfo prop;
			foreach (string s in customProperties)
				if ((prop = field.GetType ().GetProperty (s)) != null)
					SetFieldProp (field, s, prop.GetValue (field, null));
		}

		public static bool LicEdition (SPContext context, IDictionary dic, int edition) {
			return LicEdition (context, LicInfo.Get (dic), edition);
		}

		internal static bool LicEdition (SPContext context, LicInfo licInfo, int edition) {
			if (!isEnabled)
				return false;
			if (licInfo == null)
				licInfo = LicInfo.Get (null);
			if (licInfo.expired || licInfo.broken)
				return false;
			if (licInfo.userBroken)
				return (edition == 0);
			if (string.IsNullOrEmpty (licInfo.name) && ((licInfo.sd == null) || (licInfo.sd.Count <= 3)))
				return !licInfo.expired;
			if ((licInfo.siteUsers > 0) && (licInfo.siteUsers <= 10) && HasMicro)
				return true;
			if (((licInfo.expiry > DateTime.MinValue) && (licInfo.expiry <= DateTime.Now)) || ((licInfo.maxUsers > 0) && (licInfo.siteUsers > 0) && (licInfo.siteUsers > licInfo.maxUsers)))
				return (edition == 0);
			return ((edition == 0) || ((licInfo.dic != null) && ((LicEditions (context, licInfo.dic) & edition) == edition)) || ((licInfo.dic == null) && (licInfo.installSpan.Days < l1)));
		}

		private static void UpdateInfo (IDictionary value) {
			DateTime dt;
			if (value != null) {
				value ["is"] = DateTime.Today.Subtract (dt = new DateTime ((long) value [tk])).Ticks;
				value ["ed"] = dt.AddDays (l1);
			}
		}

		private static bool UpdateStatus (object d, bool ignoreErrors) {
			return UpdateStatus (d, ignoreErrors, false, true, GetMapping (), os);
		}

		internal static bool UpdateStatus (object d, bool ignoreErrors, bool unlessExists, bool web, string m, byte [] os) {
			byte [] b;
			object old = null;
			bool farmError = false;
			string k;
			SPPersistedObject pobj = GetAdminApplication ();
			using (MemoryStream ms = new MemoryStream ()) {
				formatter.Serialize (ms, d);
				b = ms.ToArray ();
			}
			using (SymmetricAlgorithm rm = NewAlgo ())
			using (ICryptoTransform rmct = rm.CreateEncryptor (GetRange<byte> (os, os.Length - 24, 24), GetRange<byte> (os, 0, 16)))
				b = Trans (rmct, b);
			if (web) {
				SPContext.Current.Web.AllowUnsafeUpdates = SPContext.Current.Site.AllowUnsafeUpdates = true;
				SPContext.Current.Site.CatchAccessDeniedException = false;
			}
			SPSecurity.CatchAccessDeniedException = false;
			old = pobj.Properties [k = pobj.GetType ().Name.Replace ("SP", string.Empty) + m];
			if (unlessExists && (old != null) && (!string.IsNullOrEmpty (old.ToString ())))
				return false;
			pobj.Properties [k] = b;
			if (ignoreErrors)
				try {
					try {
						pobj.Update (true);
					} catch (SqlException) {
						farmError = true;
						throw;
					}
				} catch {
					try {
						SPSecurity.RunWithElevatedPrivileges (delegate () {
							pobj.Update (true);
						});
					} catch {
					}
				} else
				try {
					pobj.Update (true);
				} catch (SqlException ex) {
					try {
						pobj.Properties [k] = old;
						pobj.Update (true);
					} catch {
					}
					throw new Exception (GetResource ("FarmAdminError", HttpContext.Current.Server.HtmlEncode (ex.Message)) + (ex.Message.Contains ("EXECUTE") ? GetResource ("FarmAdminErrorNoServer") : string.Empty));
				} catch {
					try {
						pobj.Properties [k] = old;
						pobj.Update (true);
					} catch {
					}
					throw;
				}
			return farmError;
		}

		private static int Verify (RSACryptoServiceProvider rsa, byte [] license, Guid id, string value, int flag1, int flag2, int flag3, long flag4) {
			byte [] salt = new byte [] { 109, 0, 115, 0, 104, 0, 97, 0, 100, 0 }, data = ToByteArray (id, value, flag1, flag2, flag3, flag4);
			List<byte> bytes = new List<byte> (license);
			return ((rsa.VerifyData (data, Encoding.Unicode.GetString (salt, 0, 2) + Encoding.Unicode.GetString (salt, 8, 2) + 5.ToString (), bytes.GetRange (128, 128).ToArray ()) && rsa.VerifyData (data, Encoding.Unicode.GetString (salt, 2, 6) + 1.ToString (), bytes.GetRange (0, 128).ToArray ())) ? 128 : 256);
		}

		internal static string GetSrpUrl () {
			string sspWebUrl = string.Empty;
#if !WSS
			if (Is14)
				using (SPSite site = GetAdminSite ())
					sspWebUrl = site.Url.TrimEnd ('/');
			else
				Elevate (delegate () {
					foreach (SPWebApplication app in TryEach<SPWebApplication> (SPWebService.ContentService.WebApplications)) {
						if ((app.Properties != null) && (app.Properties.ContainsKey ("Microsoft.Office.Server.SharedResourceProvider")))
							try {
								foreach (SPSite site in app.Sites)
									try {
										if (site.Url.ToLowerInvariant ().TrimEnd ('/').EndsWith ("/ssp/admin")) {
											sspWebUrl = site.Url.TrimEnd ('/');
											break;
										}
									} catch {
									}
							} catch {
							}
						if (!string.IsNullOrEmpty (sspWebUrl))
							break;
					}
				}, true);
#endif
			return sspWebUrl;
		}

		public static bool Is14 {
			get {
				SPFarm farm;
				if ((is14 == null) || !is14.HasValue) {
					try {
						if ((farm = GetFarm (GetContext ())) != null)
							is14 = (farm.BuildVersion.Major > 12);
					} catch {
					}
					is14 = (typeof (SPWeb).Assembly.FullName.IndexOf ("Version=14") > 0);
				}
				return (is14.HasValue && is14.Value) || Is15;
			}
		}

		public static bool Is15 {
			get {
				SPFarm farm;
				if ((is15 == null) || !is15.HasValue) {
					try {
						if ((farm = GetFarm (GetContext ())) != null)
							is15 = (farm.BuildVersion.Major > 14);
					} catch {
					}
					is15 = (typeof (SPWeb).Assembly.FullName.IndexOf ("Version=15") > 0);
				}
				return is15.HasValue && is15.Value;
			}
		}

		public static string HivePath {
			get {
				string hive = @"C:\Program Files\Common Files\Microsoft Shared\Web Server Extensions\" + (Is15 ? "15\\" : (Is14 ? "14\\" : "12\\"));
				HttpContext context = null;
				try {
					if (((context = HttpContext.Current) != null) && (context.Server == null))
						context = null;
				} catch {
				}
				if (context != null)
					try {
						return new DirectoryInfo (context.Server.MapPath ("/_layouts")).Parent.Parent.FullName.TrimEnd ('\\') + "\\";
					} catch {
					}
				try {
					using (RegistryKey regKey = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\Shared Tools\Web Server Extensions\" + (Is15 ? "15" : (Is14 ? "14" : "12")) + ".0", false))
						hive = regKey.GetValue ("Location", hive, RegistryValueOptions.None) + string.Empty;
				} catch {
				}
				return hive;
			}
		}

		internal static SPSite GetSite (SPContext context) {
			SPSite site = (((context == null) && ((context = GetContext ()) == null)) ? currentSite : context.Site);
			SPWebApplication webApp;
			HttpContext httpContext = null;
			try {
				httpContext = HttpContext.Current;
			} catch {
			}
			try {
				if ((site == null) && (httpContext != null)) {
					if (((webApp = SPWebApplication.Lookup (httpContext.Request.Url)) == null) || (webApp.Sites.Count == 0))
						throw new SPException (GetResource ("NoSite", AssemblyName, GetTitle ()));
					else
						using (SPSite firstSite = webApp.Sites [0])
							httpContext.Response.Redirect (firstSite.Url.TrimEnd ('/') + httpContext.Request.Url.ToString ().Substring (httpContext.Request.Url.ToString ().ToLowerInvariant ().IndexOf ("/_layouts/" + AssemblyName.ToLowerInvariant () + "/default.aspx")), true);
				}
			} catch {
			}
			if (site != null)
				site.CatchAccessDeniedException = false;
			return site;
		}

		public static string GetSiteTitle (SPContext context) {
			SPSite site = GetSite (context);
			SPSecurity.CatchAccessDeniedException = site.CatchAccessDeniedException = false;
			string title = string.Empty;
			try {
				Elevate (delegate () {
					if (string.IsNullOrEmpty (title))
						title = site.RootWeb.Title;
				}, true, false);
			} catch {
			}
			return string.IsNullOrEmpty (title) ? site.Url : title;
		}

		internal static SPSite OpenSite (SPContext context) {
			SPSite site = (((context == null) && ((context = GetContext ()) == null)) ? ((currentSite == null) ? null : new SPSite (currentSite.ID)) : new SPSite (context.Site.ID));
			SPWebApplication webApp;
			HttpContext httpContext = null;
			try {
				httpContext = HttpContext.Current;
			} catch {
			}
			try {
				if ((site == null) && (httpContext != null)) {
					if (((webApp = SPWebApplication.Lookup (httpContext.Request.Url)) == null) || (webApp.Sites.Count == 0))
						throw new SPException (GetResource ("NoSite", AssemblyName, GetTitle ()));
					else
						using (SPSite firstSite = webApp.Sites [0])
							httpContext.Response.Redirect (firstSite.Url.TrimEnd ('/') + httpContext.Request.Url.ToString ().Substring (httpContext.Request.Url.ToString ().ToLowerInvariant ().IndexOf ("/_layouts/" + AssemblyName.ToLowerInvariant () + "/default.aspx")), true);
				}
			} catch {
			}
			if (site != null)
				site.CatchAccessDeniedException = false;
			return site;
		}

		public static SPWebApplication GetAdminApplication () {
			foreach (SPWebApplication app in TryEach<SPWebApplication> (SPWebService.AdministrationService.WebApplications))
				if (app.IsAdministrationWebApplication)
					return app;
			return null;
		}

		public static SPSite GetAdminSite () {
			SPWebApplication app = GetAdminApplication ();
			SPSite site = null;
			if ((app != null) && (app.Sites.Count > 0) && ((site = app.Sites ["/"]) == null))
				site = app.Sites [0];
			site.CatchAccessDeniedException = false;
			return site;
		}

		public static SPContext GetContext () {
			try {
				return SPContext.Current;
			} catch {
				return null;
			}
		}

		public static SPFarm GetFarm (SPContext context) {
			SPPersistedObject obj;
			SPFarm farm = SPFarm.Local;
			SPSite site = null;
			if ((farm == null) && (context == null))
				context = GetContext ();
			if (farm == null)
				try {
					farm = GetSite (context).WebApplication.Farm;
				} catch {
				}
			if ((farm == null) && ((obj = (site = Elevated ? OpenSite (context) : GetSite (context)).WebApplication) != null)) {
				while ((obj != null) && (!(obj is SPFarm)) && (obj != obj.Parent))
					obj = obj.Parent;
				farm = obj as SPFarm;
			}
			if (Elevated && (site != null))
				site.Dispose ();
			return farm;
		}

		protected internal static string GetResource (ResourceManager res, string resKey, params object [] args) {
			string resVal = string.Empty, locKey = "roxority_Shared";
			CultureInfo farmCulture = null;
			if (res == null)
				res = Resources;
			else
				locKey = AssemblyName + "_Runtime";
			try {
				if ((Array.IndexOf<string> (new string [] { "_HelpTopics", "_AdminHelpTopics", "_ProductID" }, resKey) < 0) && ((GetContext () != null) || (currentSite != null)))
					farmCulture = GetFarmCulture (GetContext ());
				if ((farmCulture == null) || (farmCulture == CultureInfo.InvariantCulture))
					resVal = res.GetString (resKey);
				else if (string.IsNullOrEmpty (resVal = Loc (locKey, resKey, farmCulture)))
					resVal = res.GetString (resKey, farmCulture);
				if (resVal == null)
					resVal = string.Empty;
			} catch {
			}
			resVal = resVal.Replace ("{PROD}", ProductPage.GetTitle ());
			return (((args == null) || (args.Length == 0)) ? resVal : string.Format (resVal, args));
		}

		protected internal static int GetUsers (SPContext context) {
			return GetUsers (context, false, null);
		}

		protected internal static int GetUsers (SPContext context, bool elevated, Dictionary<string, SPUser> list) {
			int count = -1, tmp;
			bool wasEl = Elevated;
			SPSite contextSite = ((elevated = elevated || Elevated) ? OpenSite (context) : GetSite (context));
			contextSite.CatchAccessDeniedException = false;
			try {
				if ((tmp = contextSite.RootWeb.SiteUsers.Count) > count)
					count = tmp;
				if (list != null)
					foreach (SPUser user in contextSite.RootWeb.SiteUsers)
						if (!list.ContainsKey (ProductPage.LoginName(user.LoginName).ToLowerInvariant ()))
							list [ProductPage.LoginName(user.LoginName).ToLowerInvariant ()] = user;
			} catch {
			}
			try {
				if ((tmp = contextSite.RootWeb.Users.Count) > count)
					count = tmp;
				if (list != null)
					foreach (SPUser user in contextSite.RootWeb.Users)
						if (!list.ContainsKey (ProductPage.LoginName(user.LoginName).ToLowerInvariant ()))
							list [ProductPage.LoginName(user.LoginName).ToLowerInvariant ()] = user;
			} catch {
			}
			try {
				if ((tmp = contextSite.RootWeb.AllUsers.Count) > count)
					count = tmp;
				if (list != null)
					foreach (SPUser user in contextSite.RootWeb.AllUsers)
						if (!list.ContainsKey (ProductPage.LoginName(user.LoginName).ToLowerInvariant ()))
							list [ProductPage.LoginName(user.LoginName).ToLowerInvariant ()] = user;
			} catch {
			}
			if (elevated)
				contextSite.Dispose ();
			else
				try {
					Elevated = true;
					SPSecurity.RunWithElevatedPrivileges (delegate () {
						if ((tmp = GetUsers (context, true, list)) > count)
							count = tmp;
					});
				} catch {
				} finally {
					Elevated = wasEl;
				}
			return count;
		}

		protected internal static bool IsTheThing (IDictionary dic) {
			return (dic == usersDic);
		}

		public static string CheckVersion (SPContext context) {
			return null;
		}

		public static string Config (SPContext context, string key) {
			string scope = ConfigScope (ref key), k = AssemblyName + "_" + key, val = ((key == "_lang") ? string.Empty : GetProductResource ("CfgSettingDef_" + key));
			bool isLower = false;
			if (context == null)
				context = GetContext ();
			if ((key [0] == '_') || LicEdition (context, (LicInfo) null, 2))
				Elevate (delegate () {
					SPFarm farm = GetFarm (context);
					SPWeb rootWeb = null;
					SPSite site = Elevated ? OpenSite (context) : GetSite (context);
					SPSecurity.CatchAccessDeniedException = false;
					if (site != null)
						try {
							site.CatchAccessDeniedException = false;
							if (scope != "farm")
								rootWeb = site.OpenWeb (site.RootWeb.ID);
						} catch (UnauthorizedAccessException) {
							if (!Elevated)
								throw;
						} catch {
						}
					if (rootWeb != null) {
						using (rootWeb)
							if ((rootWeb.AllProperties != null) && (rootWeb.AllProperties.ContainsKey (k) || (isLower = rootWeb.AllProperties.ContainsKey (k.ToLowerInvariant ()))))
								val = rootWeb.AllProperties [isLower ? k.ToLowerInvariant () : k] + string.Empty;
							else if ((scope != "site") && (farm != null) && (farm.Properties != null) && (farm.Properties.ContainsKey (k) || (isLower = farm.Properties.ContainsKey (k.ToLowerInvariant ()))))
								val = farm.Properties [isLower ? k.ToLowerInvariant () : k] + string.Empty;
					} else if ((scope != "site") && (farm != null) && (farm.Properties != null) && (farm.Properties.ContainsKey (k) || (isLower = farm.Properties.ContainsKey (k.ToLowerInvariant ()))))
						val = farm.Properties [isLower ? k.ToLowerInvariant () : k] + string.Empty;
					if (Elevated && (site != null))
						site.Dispose ();
				}, true);
			return ((val == null) ? string.Empty : val);
		}

		public static T Config<T> (SPContext context, string key) where T : struct {
			string scope = ConfigScope (ref key), k = AssemblyName + "_" + key;
			bool isLower = false;
			T val = new T ();
			if (context == null)
				context = GetContext ();
			if ((key [0] == '_') || LicEdition (context, (LicInfo) null, 2))
				Elevate (delegate () {
					object obj = null;
					string sobj = null;
					SPFarm farm = GetFarm (context);
					SPSite site = Elevated ? OpenSite (context) : GetSite (context);
					SPWeb rootWeb = null;
					SPSecurity.CatchAccessDeniedException = false;
					if (site != null)
						try {
							site.CatchAccessDeniedException = false;
							if (scope != "farm")
								rootWeb = site.OpenWeb (site.RootWeb.ID);
						} catch(UnauthorizedAccessException) {
							if (!Elevated)
								throw;
						} catch {
						}
					if (rootWeb != null) {
						using (rootWeb)
							if ((rootWeb.AllProperties != null) && ((rootWeb.AllProperties.ContainsKey (k) && !string.IsNullOrEmpty (sobj = rootWeb.AllProperties [k] + string.Empty)) || (isLower = (rootWeb.AllProperties.ContainsKey (k.ToLowerInvariant ()) && !string.IsNullOrEmpty (sobj = rootWeb.AllProperties [k.ToLowerInvariant ()] + string.Empty)))))
								try {
									val = (T) Convert.ChangeType (sobj, typeof (T));
								} catch {
								} else if ((farm != null) && (farm.Properties != null) && ((farm.Properties.ContainsKey (k) && ((obj = farm.Properties [k]) != null)) || (isLower = (farm.Properties.ContainsKey (k.ToLowerInvariant ()) && ((obj = farm.Properties [k.ToLowerInvariant ()]) != null)))))
								try {
									val = ((obj is T) ? ((T) obj) : ((T) Convert.ChangeType (obj, typeof (T))));
								} catch {
								}
					} else if ((farm != null) && (farm.Properties != null) && ((farm.Properties.ContainsKey (k) && ((obj = farm.Properties [k]) != null)) || (isLower = (farm.Properties.ContainsKey (k.ToLowerInvariant ()) && ((obj = farm.Properties [k.ToLowerInvariant ()]) != null)))))
						try {
							val = ((obj is T) ? ((T) obj) : ((T) Convert.ChangeType (obj, typeof (T))));
						} catch {
						}
					if (Elevated && (site != null))
						site.Dispose ();
				}, true);
			return val;
		}

		public static void Config<T> (SPContext context, string key, T value, bool siteUpdate) {
			string k = ((AssemblyName == "Deploy") ? "Yukka_GreenBox" : AssemblyName) + "_" + key;
			if (key == "_lang")
				siteCulture = farmCulture = null;
			Elevate (delegate () {
				if (context == null)
					context = GetContext ();
				SPSite site = Elevated ? OpenSite (context) : GetSite (context);
				SPFarm farm = GetFarm (context);
				SPSecurity.CatchAccessDeniedException = false;
				SPWeb rootWeb;
				if (site != null) {
					site.AllowUnsafeUpdates = true;
					site.CatchAccessDeniedException = false;
				}
				if (context != null)
					try {
						context.Web.AllowUnsafeUpdates = true;
					} catch {
					}
				if ((!siteUpdate) && (farm != null) && (farm.Properties != null))
					try {
						farm.Properties [k] = value;
						farm.Update (true);
					} catch (SqlException) {
						throw;
					} catch (SecurityException) {
						throw;
					} catch {
					} else if ((siteUpdate) && (site != null) && ((rootWeb = site.RootWeb) != null) && (rootWeb.AllProperties != null))
					try {
						rootWeb.AllowUnsafeUpdates = true;
						rootWeb.AllProperties [k] = value.ToString ();
						rootWeb.Update ();
					} catch {
					}
				if ((site != null) && Elevated)
					site.Dispose ();
			}, true);
		}

		public static object ConvertCalcFieldValue (object val, SPFieldType typeHint, bool dateNoYear) {
			int pos;
			double dval;
			string sval = string.Empty + val, prefix = string.Empty;
			DateTime dtVal;
			if (((typeHint == SPFieldType.Boolean) && (val is bool)) ||
				((typeHint == SPFieldType.Currency) && ((val is decimal) || (val is int) || (val is long) || (val is float) || (val is double))) ||
				((typeHint == SPFieldType.DateTime) && (val is DateTime)) ||
				((typeHint == SPFieldType.Number) && ((val is decimal) || (val is int) || (val is long) || (val is float) || (val is double))))
				return val;
			if ((pos = sval.IndexOf (";#", StringComparison.InvariantCultureIgnoreCase)) > 0) {
				prefix = sval.Substring (0, pos);
				sval = sval.Substring (pos + 2);
			}
			if ((prefix == "float") && (typeHint == SPFieldType.DateTime) && double.TryParse (sval, out dval))
				return DateTime.FromOADate (dval);
			if ((prefix == "datetime") && DateTime.TryParse (sval, out dtVal))
				return dtVal;
			return string.IsNullOrEmpty (prefix) ? val : sval;
		}

		public static int Loc (string key, CultureInfo lang) {
			int c = 0, pos;
			if (lang == null) {
				string propKey;
				SPContext context = GetContext ();
				SPFarm farm = GetFarm (context);
				SPSecurity.CatchAccessDeniedException = false;
				if (farm.Properties != null)
					foreach (DictionaryEntry entry in farm.Properties) {
						propKey = entry.Key + string.Empty;
						if ((propKey.Contains ("roxority_Shared__") || propKey.Contains (AssemblyName + "_")) && ((pos = propKey.IndexOf ("__", StringComparison.InvariantCultureIgnoreCase)) > 0) && (propKey.LastIndexOf ("__", StringComparison.InvariantCultureIgnoreCase) > pos))
							c++;
					}
			}
			if (lang != null)
				Elevate (delegate () {
					string propKey;
					SPContext context = GetContext ();
					SPFarm farm = GetFarm (context);
					SPSecurity.CatchAccessDeniedException = false;
					if (farm.Properties != null)
						foreach (DictionaryEntry entry in farm.Properties)
							if ((propKey = entry.Key + string.Empty).EndsWith ("__" + lang.Name) && (string.IsNullOrEmpty (key) ? (propKey.StartsWith ("roxority_Shared__") || propKey.StartsWith (AssemblyName + "_")) : propKey.StartsWith (key + "__")))
								c++;
				}, true);
			return c;
		}

		public static string Loc (string key, string name, CultureInfo lang) {
			string res = null;
			Elevate (delegate () {
				int pos = lang.Name.IndexOf ('-');
				string propKey = key + "__" + name + "__" + lang.Name.Substring (0, (pos > 0) ? pos : lang.Name.Length);
				SPContext context = GetContext ();
				SPFarm farm = GetFarm (context);
				SPSecurity.CatchAccessDeniedException = false;
				if (farm.Properties != null)
					res = farm.Properties [propKey] as string;
			}, true);
			return res;
		}

		public static bool Loc (string key, string name, CultureInfo lang, string value) {
			SPContext ctx = ProductPage.GetContext ();
			SPFarm farm = ProductPage.GetFarm (ctx);
			return Loc (ctx, farm, farm.Properties, key, name, lang, value);
		}

		public static bool Loc (SPContext context, SPFarm farm, Hashtable props, string key, string name, CultureInfo lang, string value) {
			bool hasUpdate = false;
			string propKey = key + ((string.IsNullOrEmpty (name) && (lang == null)) ? string.Empty : ("__" + name + "__" + lang.Name));
			SPSecurity.CatchAccessDeniedException = false;
			if (!key.StartsWith ("PL_"))
				if (!ProductPage.LicEdition (context, (IDictionary) null, IsWhiteLabel ? 0 : 4))
					throw new Exception (GetResource ("NopeEd", GetResource ("Tool_Localizer_Title"), "Ultimate"));
			if (context != null)
				try {
					context.Web.AllowUnsafeUpdates = true;
				} catch {
				}
			if (props != null)
				try {
					if (string.IsNullOrEmpty (value) && props.ContainsKey (propKey)) {
						hasUpdate = true;
						props.Remove (propKey);
					} else if (!(string.IsNullOrEmpty (value) || value.Equals (props [propKey]))) {
						hasUpdate = true;
						props [propKey] = value;
					}
				} catch (SqlException) {
					throw;
				} catch (SecurityException) {
					throw;
				}
			return hasUpdate;
		}

		public static bool ConfigHasSiteValue (SPContext context, SPSite site, string key) {
			string k = AssemblyName + "_" + key;
			bool isLower = false, wasNull = (site == null);
			SPFarm farm;
			if (wasNull)
				site = Elevated ? OpenSite (context) : GetSite (context);
			if ((site != null) && (site.RootWeb == null))
				site = null;
			if (site == null)
				return false;
			farm = GetFarm (context);
			try {
				using (SPWeb rootWeb = site.OpenWeb (site.RootWeb.ID)) {
					if ((rootWeb.AllProperties == null) || !(rootWeb.AllProperties.ContainsKey (k) || (isLower = rootWeb.AllProperties.ContainsKey (k.ToLowerInvariant ()))))
						return false;
					return !rootWeb.AllProperties [isLower ? k.ToLowerInvariant () : k].Equals (farm.Properties [farm.Properties.ContainsKey (k.ToLowerInvariant ()) ? k.ToLowerInvariant () : k]);
				}
			} finally {
				if (wasNull && Elevated && (site != null))
					site.Dispose ();
			}
		}

		public static void ConfigReset (SPContext context, string key, bool siteReset) {
			string k = AssemblyName + "_" + key;
			if (key == "_lang")
				siteCulture = farmCulture = null;
			Elevate (delegate () {
				SPFarm farm = GetFarm (context);
				SPSite site = (Elevated ? OpenSite (context) : GetSite (context));
				site.AllowUnsafeUpdates = true;
				site.CatchAccessDeniedException = false;
				if (context != null)
					context.Web.AllowUnsafeUpdates = true;
				if (!siteReset) {
					if (farm.Properties.ContainsKey (k.ToLowerInvariant ()))
						farm.Properties.Remove (k.ToLowerInvariant ());
					if (farm.Properties.ContainsKey (k))
						farm.Properties.Remove (k);
					farm.Update (true);
				} else
					using (SPWeb rootWeb = site.OpenWeb (site.RootWeb.ID)) {
						rootWeb.AllowUnsafeUpdates = true;
						if (rootWeb.AllProperties.ContainsKey (k))
							rootWeb.AllProperties.Remove (k);
						if (rootWeb.AllProperties.ContainsKey (k.ToLowerInvariant ()))
							rootWeb.AllProperties.Remove (k.ToLowerInvariant ());
						rootWeb.Update ();
					}
				if (Elevated && (site != null))
					site.Dispose ();
			}, true);
		}

		public static string ConfigScope (ref string key) {
			int pos = key.IndexOf (':');
			string scope = ((pos <= 0) ? string.Empty : key.Substring (0, pos));
			if (pos >= 0)
				key = key.Substring (pos + 1);
			return scope;
		}

		public static IEnumerable<T> Deserialize<T> (string value, Action<T> action) where T : class {
			object obj;
			T t;
			IEnumerable<T> ts;
			using (MemoryStream ms = new MemoryStream (Convert.FromBase64String (value)))
				obj = Formatter.Deserialize (ms);
			if ((typeof (T) != typeof (object)) && ((t = obj as T) != null)) {
				if (action != null)
					action (t);
				yield return t;
			} else if ((ts = obj as IEnumerable<T>) != null)
				foreach (T tt in ts) {
					if (action != null)
						action (tt);
					yield return tt;
				}
		}

		private static string GetOtherResource (string resName, string resKey, params object [] args) {
			ResourceManager resMan;
			if (!resMans.TryGetValue (resName, out resMan))
				resMans [resName] = resMan = new ResourceManager (AssemblyName + ".Properties." + resName, Assembly);
			return GetResource (resMan, resKey, args);
		}

		public static string GetProductResource (string resKey, params object [] args) {
			string r = GetResource (ProductResources, resKey, args);
			return string.IsNullOrEmpty (r) ? GetResource (resKey, args) : r;
		}

		public static string GetResource (string resKey, params object [] args) {
			return GetResource (null, resKey, args);
		}
		// FILTER_END

		public static void Elevate (SPSecurity.CodeToRunElevated code, bool tryUnelevatedFirst) {
			Elevate (code, tryUnelevatedFirst, false);
		}

		public static void Elevate (SPSecurity.CodeToRunElevated code, bool tryUnelevatedFirst, bool forceBoth) {
			bool catchEx = SPSecurity.CatchAccessDeniedException;
			SPSecurity.CatchAccessDeniedException = false;
			try {
				if (SPContext.Current != null)
					SPContext.Current.Site.CatchAccessDeniedException = false;
			} catch {
			}
			if (tryUnelevatedFirst && !forceBoth)
				try {
					code ();
				} catch {
					Elevated = true;
					SPSecurity.RunWithElevatedPrivileges (code);
				} finally {
					SPSecurity.CatchAccessDeniedException = catchEx;
					Elevated = false;
				} else {
				if (forceBoth)
					try {
						code ();
					} catch {
					}
				try {
					Elevated = true;
					SPSecurity.RunWithElevatedPrivileges (code);
				} finally {
					SPSecurity.CatchAccessDeniedException = catchEx;
					Elevated = false;
				}
			}
		}

		public static Dictionary<string, SPUser> GetUsersDict (SPContext context) {
			Dictionary<string, SPUser> dict = new Dictionary<string, SPUser> ();
			GetUsers (context, false, dict);
			return dict;
		}

		public static string MergeUrlPaths (string absoluteUrl, string relativeUrl) {
			int index = 0, count;
			string absPath;
			Uri absUri;
			List<string> absParts, relParts;
			if (string.IsNullOrEmpty (absoluteUrl))
				return relativeUrl;
			else if (string.IsNullOrEmpty (relativeUrl))
				return absoluteUrl;
			else if (relativeUrl.StartsWith (absoluteUrl, StringComparison.InvariantCultureIgnoreCase))
				return relativeUrl;
			absPath = (absUri = new Uri (absoluteUrl)).GetLeftPart (UriPartial.Query).Substring (absUri.GetLeftPart (UriPartial.Authority).Length);
			absoluteUrl = absoluteUrl.TrimEnd ('/');
			relativeUrl = relativeUrl.Trim ('/');
			absParts = new List<string> (absPath.Split (new char [] { '/' }, StringSplitOptions.RemoveEmptyEntries));
			relParts = new List<string> (relativeUrl.Split (new char [] { '/' }, StringSplitOptions.RemoveEmptyEntries));
			for (int a = absParts.Count - 1; (a >= 0) && (index < relParts.Count); a--)
				if (((index + (count = absParts.Count - a)) <= relParts.Count) && (string.Join ("/", absParts.ToArray (), a, count).Equals (string.Join ("/", relParts.ToArray (), index, count), StringComparison.InvariantCultureIgnoreCase))) {
					for (int i = 0; i < count; i++) {
						absParts.RemoveAt (a);
						index++;
					}
				}
			return absUri.GetLeftPart (UriPartial.Authority).Trim ('/') + '/' + (absParts.Count == 0 ? "" : (string.Join ("/", absParts.ToArray ()) + '/')) + string.Join ("/", relParts.ToArray ());
		}

		public static string Normalize (string val) {
			bool? donorm = null;
			try {
				donorm = (bool) HttpContext.Current.Items ["roxdonormalize"];
			} catch {
			}
			if ((donorm == null) || !donorm.HasValue)
				try {
					HttpContext.Current.Items ["roxdonormalize"] = (donorm = ProductPage.Config<bool> (ProductPage.GetContext (), "DoNormalize")).Value;
				} catch {
				}
			return ((donorm != null) && donorm.Value) ? Regex.Replace (val.Normalize (NormalizationForm.FormD), @"[^\t\n\u001E-\u007F]", string.Empty) : val;
		}

		public static bool ParseBool (string value) {
			return ("1".Equals (value, StringComparison.InvariantCultureIgnoreCase) || "true".Equals (value, StringComparison.InvariantCultureIgnoreCase) || "yes".Equals (value, StringComparison.InvariantCultureIgnoreCase));
		}

		// FILTER_BEGIN
		public static string Serialize<T> (IEnumerable<T> values) {
			using (MemoryStream ms = new MemoryStream ()) {
				Formatter.Serialize (ms, new Serializables<T> (values));
				return Convert.ToBase64String (ms.ToArray ());
			}
		}
		// FILTER_END

		public static string Trim (object value, params char [] trimChars) {
			return ((value == null) ? string.Empty : (((trimChars == null) || (trimChars.Length == 0)) ? value.ToString ().Trim () : value.ToString ().Trim (trimChars)));
		}

		public static IEnumerable<T> TryEach<T> (IEnumerable collection) where T : class {
			return TryEach<T> (collection, false, null, false);
		}

		public static IEnumerable<T> TryEach<T> (IEnumerable collection, bool yieldNulls, Action<Exception> handler, bool elevate) where T : class {
			T obj;
			bool error = false, doCatch = SPSecurity.CatchAccessDeniedException;
			IEnumerator en = null;
			SPSite site;
			List<T> list = new List<T> ();
			SPSecurity.CatchAccessDeniedException = false;
			if (SPContext.Current != null)
				SPContext.Current.Site.CatchAccessDeniedException = false;
			SPSecurity.CodeToRunElevated code = delegate () {
				try {
					en = collection.GetEnumerator ();
				} catch (Exception ex) {
					if (handler != null)
						handler (ex);
				}
				if (en != null)
					while (en.MoveNext ()) {
						try {
							error = false;
							obj = en.Current as T;
							if ((site = obj as SPSite) != null) {
								site.CatchAccessDeniedException = false;
								site.ID.ToString ();
								site.Url.ToString ();
								site.RootWeb.ToString ();
								site.RootWeb.Title.ToString ();
							}
						} catch (Exception ex) {
							error = true;
							obj = null;
							if (handler != null)
								handler (ex);
						}
						if ((!error) && ((obj != null) || yieldNulls))
							list.Add (obj);
					}
			};
			if (elevate && !Elevated)
				try {
					Elevated = true;
					SPSecurity.RunWithElevatedPrivileges (code);
				} finally {
					Elevated = false;
				}
			else
				code ();
			SPSecurity.CatchAccessDeniedException = doCatch;
			return list;
		}

		// FILTER_BEGIN
		public static string DisplayVersion {
			get {
				Version version = Version;
				return string.Format ("{0}.{1}", version.Major, version.Minor);
				//return GetProductResource ("_Version");
			}
		}

		public static Version Version {
			get {
				return new Version (((AssemblyFileVersionAttribute) (ProductPage.Assembly.GetCustomAttributes (typeof (AssemblyFileVersionAttribute), true) [0])).Version);
			}
		}

		public bool HasUpdate () {
			string wv, dv;
			int w, d, wp, dp;
			try {
				wv = WebVersion;
				dv = DisplayVersion;
				if (wv.LastIndexOf ('.') > wv.IndexOf ('.'))
					wv = wv.Substring (0, wv.IndexOf ('.', wv.IndexOf ('.') + 1));
				if ((!(string.IsNullOrEmpty (wv) || string.IsNullOrEmpty (dv))) && ((wp = wv.IndexOf ('.')) > 0) && ((dp = dv.IndexOf ('.')) > 0) && int.TryParse (wv.Substring (0, wp), out w) && int.TryParse (dv.Substring (0, dp), out d))
					if (w > d)
						return true;
					else if (w == d)
						return (int.Parse (wv.Substring (wp + 1)) > int.Parse (dv.Substring (dp + 1)));
			} catch {
			}
			return false;
		}

		public static string WebVersion {
			get {
				long lastCheck = DateTime.MinValue.Ticks;
				string lc;
				XmlDocument doc=new XmlDocument ();
				XmlNode node;
				if (string.IsNullOrEmpty (HttpContext.Current.Application [GetTitle () + "_wv"] as string))
					HttpContext.Current.Application [GetTitle () + "_wv"] = DisplayVersion;
				if (string.IsNullOrEmpty (lc = HttpContext.Current.Application [GetTitle () + "_lc"] as string) || !long.TryParse (lc, out lastCheck) || (TimeSpan.FromDays (1).Ticks < (DateTime.Now.Ticks - lastCheck)))
					try {
						HttpContext.Current.Application [GetTitle () + "_lc"] = DateTime.Now.Ticks.ToString ();
						SPSecurity.CatchAccessDeniedException = false;
						Elevate (delegate () {
							try {
								new Thread (delegate (object appState) {
									string xml;
									HttpApplicationState app = appState as HttpApplicationState;
									if (app != null)
										try {
											using (WebClient wc = new WebClient ())
												if ((!string.IsNullOrEmpty (xml = wc.DownloadString ("http://roxority.com/storage/sharepoint/" + GetTitle ().ToLowerInvariant () + "/" + GetProductResource ("_WhiteLabel") + GetTitle () + ".xml"))) && xml.Contains ("<")) {
													while (!xml.StartsWith ("<", StringComparison.InvariantCultureIgnoreCase))
														xml = xml.Substring (xml.IndexOf ('<'));
													doc.LoadXml (xml);
												}
											if ((node = doc.SelectSingleNode ("//Program_Version")) != null)
												app [GetTitle () + "_wv"] = node.InnerText;
										} catch {
										}
								}).Start (HttpContext.Current.Application);
							} catch {
							}
						}, true);
					} catch {
					}
				return HttpContext.Current.Application [GetTitle () + "_wv"] as string;
			}
		}

		protected internal IEnumerable<SPWebApplication> GetWebApps (SPContext context, bool contentAppsOnly) {
			IEnumerable<SPPersistedObject> ep = EnumeratePersisteds (context, contentAppsOnly);
			if (ep != null)
				foreach (SPPersistedObject app in ep)
					if (app is SPWebApplication)
						yield return app as SPWebApplication;
		}

		protected internal object In (IDictionary value) {
			string sid = null, locKey = "roxority_Shared";
			bool isFarm = false, isOtherSite = false;
			CultureInfo culture = null;
			Dictionary<string, object> nu = new Dictionary<string, object> ();
			Guid id = Guid.Empty;
			XmlDocument doc;
			XmlNode flagNode;
			List<string> rk = new List<string> ();
			List<KeyValuePair<string, string>> theLocs;
			IDictionary dic, tmp = null;
			Hashtable locs;
			KeyValuePair<string, object> userkvp = new KeyValuePair<string, object> ();
			if (((Request.RequestType.ToLowerInvariant () == "post") || (!string.IsNullOrEmpty (Request ["licremove"]))) && (IsSiteAdmin || IsFarmAdmin))
				try {
					if (("on".Equals (Request ["licremove"], StringComparison.InvariantCultureIgnoreCase)) && (value.Contains (sid = SPContext.Current.Site.ID.ToString ()) || value.Contains (sid = GetFarm (SPContext.Current).Id.ToString ()) || value.Contains (sid = Guid.Empty.ToString ()))) {
						value.Remove (sid);
						UpdateStatus (value, false);
					} else if ((!string.IsNullOrEmpty (Request ["licremove"])) && value.Contains (sid = Request ["licremove"])) {
						value.Remove (sid);
						UpdateStatus (value, false);
					} else if ((Request.Files.Count > 0) && (Request ["cfg"] == "lic")) {
						doc = new XmlDocument ();
						doc.Load (Request.Files [0].InputStream);
						nu ["l"] = doc.DocumentElement.SelectSingleNode ("l").FirstChild.Value.Substring (l2, (l4 * l2) - 2);
						sid = (id = new Guid (doc.DocumentElement.SelectSingleNode ("i").InnerText)).ToString ();
						nu ["c"] = doc.DocumentElement.SelectSingleNode ("c").FirstChild.Value;
						for (int f = 1; f <= 4; f++)
							if ((flagNode = doc.DocumentElement.SelectSingleNode ("f" + f)) != null)
								nu ["f" + f] = flagNode.InnerText;
						if (!(isFarm = ((!"0".Equals (nu ["f1"])) && (id.Equals (GetFarm (SPContext.Current).Id) || id.Equals (Guid.Empty)))))
							isOtherSite = (id != SPContext.Current.Site.ID);
						if ((tmp = In (SPContext.Current, nu, id) as IDictionary) == null) {
							if (tmp == usersDic) {
								foreach (object thekvp in usersDic)
									if (thekvp is DictionaryEntry)
										userkvp = new KeyValuePair<string, object> (((DictionaryEntry) thekvp).Key as string, ((DictionaryEntry) thekvp).Value);
									else if (thekvp is KeyValuePair<string, object>)
										userkvp = ((KeyValuePair<string, object>) thekvp);
								throw new Exception (this ["LicUsersError", userkvp.Key, userkvp.Value]);
							} else
								throw new Exception (this ["LicNoUpload"]);
						} else {
							if (isFarm) {
								foreach (string k in value.Keys)
									if (value [k] is IDictionary)
										rk.Add (k);
								foreach (string k in rk)
									value.Remove (k);
							} else
								foreach (object o in value.Values)
									if (((dic = o as IDictionary) != null) && dic.Contains ("f1") && !"0".Equals (dic ["f1"]))
										throw new Exception (this ["LicNoSiteIfFarm"]);
							if (value.Contains (sid) && ((dic = value [sid] as IDictionary) != null))
								foreach (string k in nu.Keys)
									dic [k] = nu [k];
							value [sid] = nu;
							UpdateStatus (value, false);
						}
					} else if ((Request ["tool"] == "Tool_Localizer") && ((locs = JSON.JsonDecode (Request ["roxLocAllVals"]) as Hashtable) != null)) {
						try {
							culture = new CultureInfo (Request ["lang"]);
						} catch {
						}
						if (culture != null) {
							if (Request ["tab"] != "Studio")
								locKey = AssemblyName + "_" + Request ["tab"];
							Elevate (delegate () {
								SPContext ctx = GetContext ();
								SPFarm farm = GetFarm (ctx);
								Hashtable props = farm.Properties;
								bool hasUpdate = false;
								foreach (DictionaryEntry loc in locs)
									hasUpdate = hasUpdate | Loc (ctx, farm, props, locKey, loc.Key + string.Empty, culture, loc.Value + string.Empty);
								if (hasUpdate)
									try {
										farm.Update (true);
									} catch {
										farm.Update (false);
									}
							}, true);
						}
						Response.Redirect (GetLink (Request ["cfg"], "tab", Request ["tab"], "lang", Request ["lang"]), true);
					}
				} catch (Exception ex) {
					postEx = ((ex.InnerException != null) ? ex.InnerException : ex);
				}
			return In (SPContext.Current, value, id);
		}

		protected override void OnInit (EventArgs e) {
			SPWeb web;
			string msg = null;
			Form.Enctype = "multipart/form-data";
			Form.SubmitDisabledControls = true;
			if (Request.RawUrl.Contains ("%00") || Request.RawUrl.ToLowerInvariant ().Contains ("%00".ToLowerInvariant ()) || Request.RawUrl.ToLowerInvariant ().Contains (HttpUtility.UrlDecode ("%00").ToLowerInvariant ()) || Request.RawUrl.ToLowerInvariant ().Contains (HttpUtility.UrlDecode ("%0023c3e").ToLowerInvariant ()) || Request.RawUrl.Contains (HttpUtility.UrlDecode ("%00")) || Request.RawUrl.Contains (HttpUtility.UrlDecode ("%0023c3e")))
				Response.Redirect ("default.aspx", true);
			if (((Request ["cfg"] == "reset") || (Request ["cfg"] == "save")) && (IsSiteAdmin || IsFarmAdmin)) {
				SPContext.Current.Site.CatchAccessDeniedException = false;
				try {
					if (Request ["cfg"] == "reset")
						ConfigReset (SPContext.Current, Request ["name"], (Request ["scope"] == "site"));
					else if ((Request ["value"] == "true") || (Request ["value"] == "false"))
						Config<bool> (SPContext.Current, Request ["name"], bool.Parse (Request ["value"]), (Request ["scope"] == "site"));
					else {
						Config<string> (SPContext.Current, Request ["name"], Request ["value"], (Request ["scope"] == "site"));
					}
				} catch (SqlException ex) {
					msg = ex.Message;
				} catch {
				}
				if (msg == null)
					Response.Redirect (SPContext.Current.Site.Url + "/_layouts/" + AssemblyName + "/default.aspx?cfg=cfg&s=" + DateTime.Now.Ticks.ToString () + "#cfg_" + Request ["name"], true);
				else
					Response.Redirect (SPContext.Current.Site.Url + "/_layouts/" + AssemblyName + "/default.aspx?cfg=cfg&em=" + HttpContext.Current.Server.UrlEncode (msg.Replace (",", "").Replace (";", "")).Replace ("+", "%20").Replace ("\'", "%27") + "&s=" + DateTime.Now.Ticks.ToString (), true);
			} else if (!string.IsNullOrEmpty (Request ["roxgosite"]))
				using (SPSite site = new SPSite (Request ["roxgosite"]))
					Response.Redirect (site.Url + "/_layouts/" + AssemblyName + "/default.aspx?r=" + Rnd.Next () + "&" + (IsDocTopic ? ("doc=" + DocTopic) : (IsCfgTopic ? ("cfg=" + CfgTopic + "&tool=" + Request ["tool"]) : "")), true);
			else if (!string.IsNullOrEmpty (Request ["roxgoweb"])) {
				if ((web = GetSite (GetContext ()).OpenWeb (new Guid (Request ["roxgoweb"]))) != null)
					using (web)
						Response.Redirect (web.Url.TrimEnd ('/') + "/_layouts/" + AssemblyName + "/default.aspx" + Request.Url.Query.Replace ("roxgoweb=" + Request ["roxgoweb"], string.Empty), true);
			} else if ((!Request.RawUrl.ToLowerInvariant ().Contains ("default.aspx")) || ((!string.IsNullOrEmpty (Request.Headers ["VTI_SCRIPT_NAME"])) && (!Request.Headers ["VTI_SCRIPT_NAME"].ToLowerInvariant ().Contains ("default.aspx"))) || ((!string.IsNullOrEmpty (Request.ServerVariables ["HTTP_VTI_SCRIPT_NAME"])) && (!Request.ServerVariables ["HTTP_VTI_SCRIPT_NAME"].ToLowerInvariant ().Contains ("default.aspx"))))
				Response.Redirect (Request.RawUrl.Substring (0, Request.RawUrl.LastIndexOf ('/')) + "/default.aspx" + Request.Url.Query, true);
			else
				base.OnInit (e);
		}

		protected override void OnLoad (EventArgs e) {
			base.OnLoad (e);
			Context.Response.Cache.SetCacheability (HttpCacheability.NoCache);
		}

		protected override void OnPreRenderComplete (EventArgs e) {
			string temp, asm, jsonEnc;
			Hashtable export, tmp1, tmp2, fjs, sjs;
			SPContext context;
			SPFarm farm;
			JsonSchemaManager farmSchema = null, siteSchema = null;
			base.OnPreRenderComplete (e);
			if ((!string.IsNullOrEmpty (Request ["file"])) && !Request ["file"].EndsWith (".aspx", StringComparison.InvariantCultureIgnoreCase))
				try {
					Response.Clear ();
					Response.ContentType = "application/octet-stream";
					Response.AddHeader ("Content-Disposition", "attachment;filename=\"" + Request ["file"] + "\"");
					if (Request ["file"] == (AssemblyName + ".export.rox"))
						try {
							Response.ContentType = "text/plain";
							export = new Hashtable ();
							farm = GetFarm (context = GetContext ());
							foreach (CultureInfo culture in AllCultures)
								if ((!string.IsNullOrEmpty (culture.Name)) && (!culture.Name.Contains ("-")) && (culture.Name != "de") && (culture.Name != "en") /*&& (culture.Name != "fr")*/ && (Loc (null, culture) > 0)) {
									tmp1 = new Hashtable ();
									foreach (DictionaryEntry entry in farm.Properties)
										if ((temp = entry.Key + string.Empty).EndsWith ("__" + culture.Name) && (temp.StartsWith ("roxority_Shared__") || temp.StartsWith (AssemblyName + "_")))
											tmp1 [temp] = entry.Value;
									export [culture.Name] = tmp1;
								}
							tmp1 = new Hashtable ();
							tmp2 = new Hashtable ();
							foreach (Dictionary<string, string> cfgDict in ConfigSettings) {
								tmp1 [cfgDict ["name"]] = Config (context, "farm:" + cfgDict ["name"]);
								tmp2 [cfgDict ["name"]] = Config (context, "site:" + cfgDict ["name"]);
							}
							fjs = new Hashtable ();
							sjs = new Hashtable ();
							foreach (string fp in JsonSchemaManager.DiscoverSchemaFiles (Context)) {
								farmSchema = siteSchema = null;
								asm = (fp.EndsWith ("schemas.tl.json", StringComparison.InvariantCultureIgnoreCase)) ? "roxority_Shared" : null;
								try {
									farmSchema = new JsonSchemaManager (this, fp, false, asm);
									if (!IsAdminSite)
										siteSchema = new JsonSchemaManager (this, fp, true, asm);
								} catch {
								}
								if (farmSchema != null)
									foreach (KeyValuePair<string, JsonSchemaManager.Schema> kvp in farmSchema.AllSchemas)
										fjs [Path.GetFileName (fp) + ":" + kvp.Key] = kvp.Value.InstDict;
								if (siteSchema != null)
									foreach (KeyValuePair<string, JsonSchemaManager.Schema> kvp in siteSchema.AllSchemas)
										sjs [Path.GetFileName (fp) + ":" + kvp.Key] = kvp.Value.InstDict;
							}
							export ["farm"] = tmp1;
							export ["site"] = tmp2;
							if (fjs.Count > 0)
								export ["fjs"] = fjs;
							if (sjs.Count > 0)
								export ["sjs"] = sjs;
							jsonEnc = JSON.JsonEncode (export);
							Response.Write (jsonEnc);
						} catch (Exception ex) {
							Response.Write (ex.ToString ());
						}
					else
						Response.WriteFile (Server.MapPath (Request ["file"]));
					Response.End ();
				} finally {
					if ((siteSchema != farmSchema) && (siteSchema != null))
						siteSchema.Dispose ();
					if (farmSchema != null)
						farmSchema.Dispose ();
				}
		}

		public override void Dispose () {
			if (adminSite != null) {
				adminSite.Dispose ();
				adminSite = null;
			}
			base.Dispose ();
		}

		public string GetHelpTitle (string topicID) {
			string title = GetProductResource ("HelpTopic_" + topicID);
			if (string.IsNullOrEmpty (title) && topicID.StartsWith ("itemref_"))
				title = GetResource ("Ref", GetProductResource ("Tool_" + topicID.Substring ("itemref_".Length) + "_Title"));
			return title;
		}

		public string GetLink (string cfg, params string [] queryArgs) {
			string url = "default.aspx?";
			if (string.IsNullOrEmpty (cfg))
				cfg = Request ["cfg"];
			url += ("cfg=" + cfg);
			if ((!string.IsNullOrEmpty (cfg = Request ["tool"])) && (Array.IndexOf<string> (queryArgs, "tool") < 0))
				url += ("&tool=" + cfg);
			if ((queryArgs != null) && (queryArgs.Length > 0))
				for (int i = 1; i < queryArgs.Length; i += 2)
					url += ("&" + queryArgs [i - 1] + "=" + Server.UrlEncode (queryArgs [i]));
			url += ("&r=" + Rnd.Next ());
			return url;
		}

		public int GetNavCount (string item) {
			bool noSave = JsonSchemaManager.noSave;
			KeyValuePair<JsonSchemaManager, JsonSchemaManager> kvp;
			JsonSchemaManager jsm;
			JsonSchemaManager.Schema schema;
			JsonSchemaManager.noSave = true;
			try {
				if (item == "wss")
					return GetProductResource ("_WssItems").Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries).Length;
				if (item == "cfg")
					return ConfigSettingsCount;
				if (item == "Tool_SiteUsers")
					return ProductPage.GetUsersDict (ProductPage.GetContext ()).Count;
				if (item == "Tool_Localizer")
					return Loc (string.Empty, null);
				if (item.StartsWith ("Tool_") && (item != "Tool_Transfer"))
					foreach (string fp in JsonSchemaManager.DiscoverSchemaFiles (Context)) {
						kvp = JsonSchemaManager.TryGet (this, fp, IsAdminSite, !IsAdminSite, item.StartsWith ("Tool_Data") ? "roxority_Shared" : null);
						try {
							if (((jsm = (IsAdminSite ? kvp.Key : kvp.Value)) != null) && jsm.AllSchemas.TryGetValue (item.Substring (5), out schema))
								return schema.InstanceCount;
						} finally {
							if ((kvp.Key != null) && (kvp.Key != kvp.Value))
								kvp.Key.Dispose ();
							if (kvp.Value != null)
								kvp.Value.Dispose ();
						}
					}
				return -1;
			} finally {
				JsonSchemaManager.noSave = noSave;
			}
		}

		public bool IsAnyAdmin {
			get {
				if ((isAnyAdmin == null) || !isAnyAdmin.HasValue)
					isAnyAdmin = IsFarmAdmin || IsSiteAdmin;
				return isAnyAdmin.Value;
			}
		}

		public bool IsApplicableAdmin {
			get {
				if ((isAppAdmin == null) || !isAppAdmin.HasValue)
					isAppAdmin = (IsAdminSite ? IsFarmAdmin : IsSiteAdmin);
				return isAppAdmin.Value;
			}
		}

		public bool IsFarmAdmin {
			get {
				SPFarm farm;
				SPContext context;
				SPSite site;
				if ((isFarmAdmin == null) || !isFarmAdmin.HasValue)
					try {
						if ((bool) (isFarmAdmin = IsFarmAdministrator (farm = GetFarm (context = GetContext ())))) {
							site = (Elevated ? OpenSite (context) : GetSite (context));
							IsFarmError = !site.WebApplication.IsAdministrationWebApplication;
							if (Elevated)
								site.Dispose ();
						}
						//try {
						//    SPContext.Current.Web.AllowUnsafeUpdates = SPContext.Current.Site.AllowUnsafeUpdates = true;
						//    SPSecurity.CatchAccessDeniedException = SPContext.Current.Site.CatchAccessDeniedException = false;
						//    farm.Properties ["roxtmp"] = "roxtmp";
						//    farm.Update (true);
						//    try {
						//        farm.Properties.Remove ("roxtmp");
						//        farm.Update (true);
						//    } catch {
						//    }
						//} catch (SqlException) {
						//    IsFarmError = true;
						//}
					} catch {
						isFarmAdmin = false;
					}
				return isFarmAdmin.Value;
			}
		}

		public bool IsFullFarmAdmin {
			get {
				return IsFarmAdmin && !IsFarmError;
			}
		}

		public bool IsFullAdmin {
			get {
				return IsFarmAdmin && IsSiteAdmin;
			}
		}

		public bool IsSiteAdmin {
			get {
				SPContext context;
				SPSite site = null;
				if ((isSiteAdmin == null) || !isSiteAdmin.HasValue) {
					isSiteAdmin = false;
					SPSecurity.CatchAccessDeniedException = false;
					try {
						if (((context = GetContext ()) != null) && ((site = (Elevated ? OpenSite (context) : GetSite (context))) != null)) {
							site.CatchAccessDeniedException = false;
							isSiteAdmin = site.RootWeb.UserIsSiteAdmin;
						}
					} catch {
					} finally {
						if (Elevated && (site != null))
							site.Dispose ();
					}
				}
				return isSiteAdmin.Value;
			}
		}

		protected internal IDictionary Status {
			get {
				return GetStatus<Dictionary<string, object>> (SPContext.Current);
			}
		}

		public SPWebApplication AdminApp {
			get {
				if (adminApp == null)
					adminApp = GetAdminApplication ();
				return adminApp;
			}
		}

		public SPSite AdminSite {
			get {
				if (adminSite == null)
					adminSite = GetAdminSite ();
				return adminSite;
			}
		}

		public IEnumerable<KeyValuePair<string, string>> Breadcrumb {
			get {
				List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>> ();
				SPWeb web = SPContext.Current.Web;
				Guid lastID = web.ID;
				if ("tools".Equals (Request.QueryString ["cfg"], StringComparison.InvariantCultureIgnoreCase) && !string.IsNullOrEmpty (Request.QueryString ["tool"]))
					list.Insert (0, new KeyValuePair<string, string> ("?cfg=tools&r=" + Rnd.Next (), GetResource ("ToolsFrame")));
				if (IsDocTopic)
					list.Insert (0, new KeyValuePair<string, string> ("?doc=intro&r=" + Rnd.Next (), GetResource ("Tab_Help")));
				else
					list.Insert (0, new KeyValuePair<string, string> ("?r=" + Rnd.Next (), GetResource ("Tab_Info")));
				list.Insert (0, new KeyValuePair<string, string> (Request.RawUrl.Substring (0, (Request.RawUrl.IndexOf ('?') > 0) ? Request.RawUrl.IndexOf ('?') : Request.RawUrl.Length) + "?r=" + Rnd.Next (), this ["HelpTitle", ProductName]));
				list.Insert (0, new KeyValuePair<string, string> (Request.RawUrl.Substring (0, Request.RawUrl.IndexOf ("/_layouts/", StringComparison.InvariantCultureIgnoreCase)) + "/_layouts/settings.aspx", "Site Settings"));
				while (web != null) {
					list.Insert (0, new KeyValuePair<string, string> (web.Url, web.Title));
					try {
						web = web.ParentWeb;
					} catch {
						try {
							web = web.Site.RootWeb;
						} catch {
							web = null;
						}
					}
					if (web != null)
						if (web.ID == lastID)
							web = null;
						else
							lastID = web.ID;
				}
				return list;
			}
		}

		public string CfgTopic {
			get {
				return Request ["cfg"];
			}
		}

		public Dictionary<string, string> ConfigGroups {
			get {
				string tmp;
				if (cfgGroups == null) {
					cfgGroups = new Dictionary<string, string> ();
					if (!string.IsNullOrEmpty (tmp = GetProductResource ("_SettingGroups")))
						foreach (string s in tmp.Split (new char [] { '|' }, StringSplitOptions.RemoveEmptyEntries))
							cfgGroups [s.Substring (s.IndexOf (':') + 1)] = s.Substring (0, s.IndexOf (':'));
				}
				return cfgGroups;
			}
		}

		public IEnumerable<Dictionary<string, string>> ConfigSettings {
			get {
				bool multiSel;
				string choiceTitle, settings = GetProductResource ("_Settings"), defVal, val, valTemp = "[=" + this ["Auto"] + "|en-US=English|de-DE=Deutsch"/*|fr-FR=Français*/;
				string [] pairs;
				Dictionary<string, string> dict = new Dictionary<string, string> ();
				dict ["name"] = "_lang";
				foreach (CultureInfo culture in AllCultures)
					if ((!string.IsNullOrEmpty (culture.Name)) && (!culture.Name.Contains ("-")) && (culture.Name != "de") && (culture.Name != "en") /*&& (culture.Name != "fr")*/ && (Loc (string.Empty, culture) > 0))
						valTemp += ("|" + culture.Name + "=" + culture.DisplayName);
				dict ["type"] = valTemp + "]";
				dict ["title"] = this ["CfgSettingTitle__lang"];
				dict ["desc"] = this ["CfgSettingDesc__lang", ProductName];
				yield return dict;
				if ((!string.IsNullOrEmpty (settings)) && ((pairs = settings.Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries)) != null) && (pairs.Length > 0))
					foreach (string p in pairs) {
						dict = new Dictionary<string, string> ();
						dict ["name"] = p.Substring (0, p.IndexOf (':'));
						dict ["type"] = p.Substring (p.IndexOf (':') + 1);
						dict ["title"] = GetProductResource ("CfgSettingTitle_" + dict ["name"]);
						dict ["caption"] = GetProductResource ("CfgSettingCaption_" + dict ["name"]);
						dict ["desc"] = GetProductResource ("CfgSettingDesc_" + dict ["name"]);
						dict ["default"] = ((string.IsNullOrEmpty (defVal = GetProductResource ("CfgSettingDef_" + dict ["name"]))) ? string.Empty : defVal);
						if (dict ["type"].StartsWith ("[") && dict ["type"].EndsWith ("]")) {
							if (multiSel = (valTemp = dict ["type"].Substring (1, dict ["type"].Length - 2)).StartsWith ("*", StringComparison.InvariantCultureIgnoreCase))
								valTemp = valTemp.Substring (1);
							val = string.Empty;
							foreach (string s in valTemp.Split (new char [] { '|' }, StringSplitOptions.RemoveEmptyEntries))
								if (s.IndexOf ('=') >= 0)
									val += (s + '|');
								else {
									choiceTitle = GetProductResource ("CfgSettingChoice_" + dict ["name"] + "_" + s);
									val += (s + '=' + (string.IsNullOrEmpty (choiceTitle) ? s : choiceTitle) + '|');
								}
							dict ["type"] = '[' + val.Substring (0, val.Length - 1) + ']';
							if (multiSel)
								dict ["multiSel"] = string.Empty;
						}
						yield return dict;
					}
				if (!string.IsNullOrEmpty (GetProductResource ("_hasjquery"))) {
					dict ["name"] = "_nojquery";
					dict ["type"] = "bool";
					dict ["caption"] = this ["CfgSettingCaption__nojquery"];
					dict ["title"] = this ["CfgSettingTitle__nojquery"];
					dict ["desc"] = this ["CfgSettingDesc__nojquery", ProductName];
					yield return dict;
				}
			}
		}

		public int ConfigSettingsCount {
			get {
				string settings = GetProductResource ("_Settings");
				string [] pairs;
				return (string.IsNullOrEmpty (GetProductResource ("_hasjquery")) ? 1 : 2) + (((!string.IsNullOrEmpty (settings)) && ((pairs = settings.Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries)) != null)) ? pairs.Length : 0);
			}
		}

		public static string LoginName (string name) {
			int pos = name.IndexOf ('|');
			return (pos >= 0) ? name.Substring (pos + 1) : name;
		}

		public string DocTopic {
			get {
				return Request ["doc"];
			}
		}

		public List<KeyValuePair<int, string>> Editions {
			get {
				return GetEditions ();
			}
		}

		public SPFarm Farm {
			get {
				if (farm == null)
					farm = GetFarm (SPContext.Current);
				return farm;
			}
		}

		public bool IsAdminSite {
			get {
				try {
					return SPContext.Current.Site.ID.Equals (AdminSite.ID);
				} catch {
					return false;
				}
			}
		}

		public bool IsCfgTopic {
			get {
				return (!string.IsNullOrEmpty (CfgTopic));
			}
		}

		public bool IsDocTopic {
			get {
				return (!string.IsNullOrEmpty (DocTopic));
			}
		}

		public string this [int key] {
			get {
				return GetEdition (key);
			}
		}

		public string this [string resKey, params object [] args] {
			get {
				return GetResource (Resources, resKey, args);
			}
		}

		public string ProductName {
			get {
				return GetTitle();
			}
		}

		public string TopicContent {
			get {
				try {
					int pos = -1, c = 0;
					string defaultValue, path = string.Empty, langInset = string.Empty, lastTab = string.Empty, tmp;
					string [] pairs;
					JsonSchemaManager.Schema schema;
					JsonSchemaManager.Property.Type.Choice choice;
					CultureInfo farmCulture = GetFarmCulture (SPContext.Current), theCulture = farmCulture;
					StringBuilder content = new StringBuilder ();
					if (farmCulture != null)
						if (File.Exists (path = Server.MapPath ("help/" + farmCulture.Name + "/" + DocTopic + ".html")))
							langInset = farmCulture.Name + "\\";
						else if ((farmCulture.Parent != null) && File.Exists (path = Server.MapPath ("help/" + farmCulture.Parent.Name + "/" + DocTopic + ".html")))
							langInset = (theCulture = farmCulture.Parent).Name + "\\";
					if (string.IsNullOrEmpty (path) || !File.Exists (path)) {
						theCulture = null;
						path = Server.MapPath ("help/" + DocTopic + ".html");
					}
					using (StreamReader reader = File.OpenText (path))
						content.Append (reader.ReadToEnd ());
					if (content.Length == 0)
						if (DocTopic == "farm_site_config") {
							using (StreamWriter writer = File.CreateText (path)) {
								writer.WriteLine (GetResource ("ConfigHelp_Intro", ProductName, AssemblyName));
								foreach (Dictionary<string, string> config in ConfigSettings) {
									writer.WriteLine ("<h3>{0}</h3>", Server.HtmlEncode (config ["title"]).Replace ("'", "&#39;"));
									writer.WriteLine ("<p>{0}</p>", config ["desc"].Replace ("'", "&#39;").Replace (this ["ConfigHelp_ReplOld"], this ["ConfigHelp_ReplNew", ProductName]).Replace ("Ä", "&Auml;").Replace ("Ö", "&Ouml;").Replace ("Ü", "&Uuml;").Replace ("ä", "&auml;").Replace ("ö", "&ouml;").Replace ("ü", "&uuml;").Replace ("ß", "&szlig;"));
									if (config ["type"].StartsWith ("[") && config ["type"].EndsWith ("]") && ((pairs = config ["type"].Substring (1, config ["type"].Length - 2).Split ('|')) != null) && (pairs.Length > 0)) {
										writer.WriteLine (GetResource ("ConfigHelp_Choice", ProductName));
										writer.WriteLine ("<ul>");
										foreach (string pair in pairs)
											writer.WriteLine ("<li><i>{0}</i></li>", Server.HtmlEncode (pair.Substring (pair.IndexOf ('=') + 1)).Replace ("'", "&#39;"));
										writer.WriteLine ("</ul>");
									}
									if (!string.IsNullOrEmpty (defaultValue = GetProductResource ("CfgSettingDef_" + config ["name"]))) {
										writer.WriteLine (GetResource ("ConfigHelp_Default", GetResource ("CfgReset"), ProductName));
										writer.WriteLine ("<pre>{0}</pre>", HttpUtility.HtmlEncode (defaultValue).Replace ("'", "&#39;").Replace ("\r", "<br/>").Replace ("\n", "<br/>").Replace ("<br/><br/>", "<br/>"));
									}
								}
							}
							File.Copy (path, @"C:\Users\roxor\Documents\Visual Studio 2010\Projects\" + AssemblyName + "\\" + AssemblyName + @"\12\Template\layouts\" + AssemblyName + @"\help\" + langInset + "farm_site_config.html", true);
							return TopicContent;
						} else if (DocTopic.StartsWith ("itemref_")) {
							using (JsonSchemaManager schemaMan = new JsonSchemaManager (this, null, true, "itemref_DataSources".Equals (DocTopic) ? "roxority_Shared" : null)) {
								schema = schemaMan.AllSchemas [DocTopic.Substring ("itemref_".Length)];
								using (StreamWriter writer = File.CreateText (path)) {
									writer.WriteLine (GetResource ("ConfigHelp_ItemIntro", ProductName, GetProductResource ("Tool_" + schema.Name + "_Title"), GetProductResource ("Tool_" + schema.Name + "_TitleSingular")) + "<span>");
									foreach (JsonSchemaManager.Property prop in schema.GetPropertiesNoDuplicates ()) {
										if ((prop.Tab != lastTab) && (schema.PropTabs.Count > 1))
											writer.WriteLine ("</span><h4 class=\"rox-h4 rox-doctab rox-doctab-" + (lastTab = prop.Tab) + "\"><a href=\"#\" onclick=\"roxTogDocTab('" + prop.Tab + "');\">{0}</a></h4><span class=\"rox-doctab rox-doctab-" + prop.Tab + "\">", Server.HtmlEncode (this ["ConfigHelp_ItemTab", schema.PropTabs [prop.Tab]]).Replace ("'", "&#39;"));
										writer.WriteLine ("<h3>{0}</h3>", Server.HtmlEncode (prop.ToString ()).Replace ("'", "&#39;"));
										if (!string.IsNullOrEmpty (tmp = this ["ConfigHelp_ItemType" + prop.PropertyType.GetType ().Name]))
											writer.Write ("<p><b>(" + Server.HtmlEncode (tmp) + ")</b></p>");
										writer.WriteLine ("<p>{0}</p>", prop.Description.Replace ("'", "&#39;").Replace ("Ä", "&Auml;").Replace ("Ö", "&Ouml;").Replace ("Ü", "&Uuml;").Replace ("ä", "&auml;").Replace ("ö", "&ouml;").Replace ("ü", "&uuml;").Replace ("ß", "&szlig;"));
										if ((!string.IsNullOrEmpty (tmp = this ["ConfigHelp_Item" + prop.PropertyType.GetType ().Name])) || !string.IsNullOrEmpty (tmp = this ["ConfigHelp_Item" + prop.PropertyType.GetType ().BaseType.Name]))
											writer.WriteLine ("<p>{0}</p>", tmp.Replace ("'", "&#39;").Replace ("Ä", "&Auml;").Replace ("Ö", "&Ouml;").Replace ("Ü", "&Uuml;").Replace ("ä", "&auml;").Replace ("ö", "&ouml;").Replace ("ü", "&uuml;").Replace ("ß", "&szlig;"));
										if ((choice = prop.PropertyType as JsonSchemaManager.Property.Type.Choice) != null) {
											writer.WriteLine ("<ul>");
											foreach (object ch in choice.GetChoices (prop.RawSchema))
												if (string.IsNullOrEmpty (tmp = choice.GetChoiceDesc (prop, ch)))
													writer.WriteLine ("<li>" + HttpUtility.HtmlEncode (choice.GetChoiceTitle (prop, ch)) + "</li>");
												else
													writer.WriteLine ("<li><b>" + HttpUtility.HtmlEncode (choice.GetChoiceTitle (prop, ch)) + "</b><br/>&mdash; <i>" + HttpUtility.HtmlEncode (tmp) + "</i></li>");
											writer.WriteLine ("</ul>");
										}
#if !SETUPZEN
										if ((!prop.PropertyType.IsBool) && (!(prop.PropertyType is JsonSchemaManager.Property.Type.Choice)) && (!(prop.PropertyType is JsonSchemaManager.Property.Type.DictChoice)) && (!(prop.PropertyType is roxority.SharePoint.JsonSchemaPropertyTypes.DataFields)) && (!string.IsNullOrEmpty (prop.DefaultValue + string.Empty))) {
											writer.WriteLine ("<p>" + HttpUtility.HtmlEncode (GetResource ("ConfigHelp_ItemDef", GetProductResource ("Tool_" + DocTopic.Substring (DocTopic.IndexOf ('_') + 1) + "_Title"))) + "</p>");
											writer.WriteLine ("<pre>" + HttpUtility.HtmlEncode (prop.DefaultValue + string.Empty).Replace ("\r\n", "<br/>").Replace ("\r", "<br/>").Replace ("\n", "<br/>") + "</pre>");
										}
#endif
									}
									writer.WriteLine ("</span>");
									writer.Flush ();
									writer.Close ();
								}
							}
							File.Copy (path, @"C:\Users\roxor\Documents\Visual Studio 2010\Projects\" + AssemblyName + "\\" + AssemblyName + @"\12\Template\layouts\" + AssemblyName + @"\help\" + langInset + DocTopic + ".html", true);
							return TopicContent;
						}
					while ((pos = content.ToString ().IndexOf ("<h3>", pos + 1, StringComparison.InvariantCultureIgnoreCase)) >= 0)
						content.Insert (pos + 4, string.Format ("<a name=\"s{0}\"></a>", c++));
					return content.ToString ();
				} catch (Exception ex) {
					return "<div class=\"ms-error\">" + ex.Message + "</div>";
				}
			}
		}

		public string WhiteLabel {
			get {
				if (wlabel == null)
					wlabel = GetProductResource ("_WhiteLabel");
				return wlabel;
			}
		}

		public IEnumerable<Dictionary<string, string>> WssItems {
			get {
				string items = GetProductResource ("_WssItems"), iconUrl, allWebsSelect = null, ullis, fieldDesc;
				string [] splits = items.Split (new char [] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				int pos;
				SPContentType ctype, ctemp;
				SPContentTypeId ctypeID;
				Dictionary<string, string> dict;
				Dictionary<SPList, List<SPField>> listFields;
				List<SPField> fieldList;
				SPField field, lf;
				SPFieldTypeDefinition fieldType;
				Guid featureID;
				SPFeature feature;
				SPFeatureDefinition featureDef;
				SPContext context = GetContext ();
				SPSite site = null;
				SPFarm farm;
				CultureInfo culture;
				SPWeb web = null;
				bool active = false, isSite, isWeb, hasc;
				Converter<SPWeb, string> createAllWebsSelect = null;
				createAllWebsSelect = delegate (SPWeb w) {
					SPWeb pw = w;
					string options = string.Empty;
					int distance = 0;
					if (w.IsRootWeb)
						options += "<select style=\"font-size: 10px;\" onchange=\"roxGoWeb(this.options[this.selectedIndex].value);\">";
					else
						do
							distance++;
						while (!(pw = pw.ParentWeb).IsRootWeb);
					options += ("<option value=\"" + w.ID + "\"" + (w.ID.Equals (web.ID) ? " selected=\"selected\"" : string.Empty) + ">");
					for (int i = 0; i < distance; i++)
						options += ((i == (distance - 1)) ? "-&nbsp;" : "&nbsp;&nbsp;");
					options += (Context.Server.HtmlEncode (w.Title) + "</option>");
					foreach (SPWeb sw in TryEach<SPWeb> (w.Webs))
						using (sw)
							options += createAllWebsSelect (sw);
					if (w.IsRootWeb)
						options += "</select>";
					return options;
				};
				if ((context != null) && ((farm = GetFarm (context)) != null) && ((site = (Elevated ? OpenSite (context) : GetSite (context))) != null) && ((web = context.Web) != null) && ((culture = GetFarmCulture (context)) != null))
					foreach (string pair in splits) {
						ullis = string.Empty;
						isWeb = false;
						dict = new Dictionary<string, string> ();
						dict ["type"] = pair.Substring (0, pos = pair.IndexOf (':'));
						dict ["title"] = dict ["id"] = pair.Substring (pos + 1);
						dict ["desc"] = this ["WssInfoNope"];
						dict ["icon"] = "genericfeature.gif";
						dict ["info"] = string.Empty;
						if ((dict ["type"] == "FieldType") && ((fieldType = web.FieldTypeDefinitionCollection [dict ["id"]]) != null)) {
							dict ["icon"] = "menuaddcolumn.gif";
							if (string.IsNullOrEmpty (fieldDesc = GetProductResource ("WssField_" + dict ["id"]))) {
								dict ["title"] = fieldType.TypeDisplayName;
								dict ["desc"] = fieldType.TypeShortDescription;
							} else {
								dict ["title"] = fieldType.TypeShortDescription;
								dict ["desc"] = fieldDesc;
							}
							listFields = new Dictionary<SPList, List<SPField>> ();
							foreach (SPList wl in TryEach<SPList> (web.Lists))
								foreach (SPField f in TryEach<SPField> (wl.Fields))
									if (f.TypeAsString == dict ["id"]) {
										if (!listFields.TryGetValue (wl, out fieldList))
											listFields [wl] = fieldList = new List<SPField> ();
										fieldList.Add (f);
									}
							fieldList = new List<SPField> ();
							foreach (SPField sf in TryEach<SPField> (web.Fields))
								if (sf.TypeAsString == dict ["id"])
									fieldList.Add (sf);
							foreach (KeyValuePair<SPList, List<SPField>> kvp in listFields) {
								ullis += ("<li><a target=\"_blank\" href=\"" + MergeUrlPaths (web.Url, "/_layouts/listedit.aspx?List=" + Context.Server.UrlEncode (kvp.Key.ID.ToString ())) + "#1400\">" + Context.Server.HtmlEncode (kvp.Key.Title) + "</a>: ");
								foreach (SPField f in kvp.Value)
									ullis += ("<a target=\"_blank\" href=\"" + MergeUrlPaths (web.Url, "/_layouts/FldEditEx.aspx?List=" + Context.Server.UrlEncode (kvp.Key.ID.ToString ()) + "&Field=" + Context.Server.UrlEncode (f.InternalName)) + "\">" + Context.Server.HtmlEncode (f.Title) + "</a>, ");
								ullis = ullis.Substring (0, ullis.Length - 2) + "</li>";
							}
							if (fieldList.Count > 0) {
								ullis += ("<li>" + this ["WssInfoType_SiteColumns"] + ": ");
								foreach (SPField f in fieldList)
									ullis += ("<a target=\"_blank\" href=\"" + MergeUrlPaths (web.Url, "/_layouts/FldEditEx.aspx?Field=" + Context.Server.UrlEncode (f.InternalName)) + "\">" + Context.Server.HtmlEncode (f.Title) + "</a>, ");
								ullis = ullis.Substring (0, ullis.Length - 2) + "</li>";
							}
							if (string.IsNullOrEmpty (allWebsSelect))
								allWebsSelect = createAllWebsSelect (site.RootWeb);
							dict ["info"] = this ["WssInfoType", allWebsSelect] + ("<ul style=\"font-size: 10px;\">" + (string.IsNullOrEmpty (ullis) ? this ["WssInfoNone"] : ullis) + "</ul>");
						} else if ((dict ["type"] == "SiteColumn") && ((field = GetField (site.RootWeb.Fields, dict ["id"])) != null)) {
							dict ["link"] = string.Format (MergeUrlPaths (site.RootWeb.Url, "/_layouts/FldEditEx.aspx?Field={0}"), dict ["id"]);
							dict ["title"] = field.Title;
							//	mngfield.aspx?i=1&Cmd=DeleteField&List=id&Field=name
							dict ["desc"] = field.Description;
							dict ["icon"] = "desfield.gif";
							foreach (SPList wl in TryEach<SPList> (web.Lists))
								if ((lf = GetField (wl, dict ["id"])) != null) // && (lf.TypeAsString == "Yukka_GreenBox_LogField"))
									ullis += ("<li><a target=\"_blank\" href=\"" + MergeUrlPaths (web.Url, "/_layouts/FldEditEx.aspx?List=" + Context.Server.UrlEncode (wl.ID.ToString ()) + "&Field=" + Context.Server.UrlEncode (dict ["id"])) + "\">" + Context.Server.HtmlEncode (wl.Title) + "</a></li>");
							if (string.IsNullOrEmpty (allWebsSelect))
								allWebsSelect = createAllWebsSelect (site.RootWeb);
							dict ["info"] = this ["WssInfoUsed", allWebsSelect] + ("<ul style=\"font-size: 10px;\">" + (string.IsNullOrEmpty (ullis) ? this ["WssInfoNone"] : ullis) + "</ul>");
						} else if ((dict ["type"] == "ContentType") && ((ctype = site.RootWeb.ContentTypes [ctypeID = new SPContentTypeId (dict ["id"])]) != null)) {
							dict ["title"] = ctype.Name;
							dict ["desc"] = ctype.Description;
							dict ["icon"] = "WssTeamAndCollabInfrastruct.gif";
							dict ["link"] = string.Format (MergeUrlPaths (site.RootWeb.Url, "/_layouts/ManageContentType.aspx?ctype={0}"), dict ["id"]);
							foreach (SPList wl in TryEach<SPList> (web.Lists))
								if (wl.ContentTypesEnabled && (wl.ContentTypes != null))
									foreach (SPContentType lc in TryEach<SPContentType> (wl.ContentTypes)) {
										ctemp = lc;
										hasc = false;
										do {
											hasc |= ctemp.Id.Equals (ctypeID);
										} while ((!hasc) && (ctemp.Parent != null) && (!ctemp.Parent.Id.Equals (ctemp.Id)) && ((ctemp = ctemp.Parent) != null));
										if (hasc)
											ullis += ("<li><a target=\"_blank\" href=\"" + MergeUrlPaths (web.Url, "/_layouts/listedit.aspx?List=" + Context.Server.UrlEncode (wl.ID.ToString ())) + "#1400\">" + Context.Server.HtmlEncode (wl.Title) + "</a></li>");
									}
							if (string.IsNullOrEmpty (allWebsSelect))
								allWebsSelect = createAllWebsSelect (site.RootWeb);
							dict ["info"] = this ["WssInfoAttached", allWebsSelect] + ("<ul style=\"font-size: 10px;\">" + (string.IsNullOrEmpty (ullis) ? this ["WssInfoNone"] : ullis) + "</ul>");
						} else if (!Guid.Empty.Equals (featureID = GetGuid (dict ["id"], true))) {
							if (((featureDef = farm.FeatureDefinitions [featureID]) != null) || ((((feature = site.WebApplication.Features [featureID]) != null) || ((feature = site.Features [featureID]) != null) || ((feature = web.Features [featureID]) != null)) && ((featureDef = feature.Definition) != null))) {
								dict ["title"] = featureDef.GetTitle (culture);
								dict ["desc"] = featureDef.GetDescription (culture);
								if (!string.IsNullOrEmpty (iconUrl = featureDef.GetImageUrl (culture)))
									dict ["icon"] = iconUrl;
							}
							if (dict ["type"] == "FarmFeature") {
								dict ["link"] = string.Format (MergeUrlPaths (AdminSite.Url, "/_admin/ManageFarmFeatures.aspx#{0}"), dict ["id"]);
								active = ((SPWebService.AdministrationService.Features [featureID] != null) || (SPWebService.ContentService.Features [featureID] != null));
							} else if (dict ["type"] == "AppFeature") {
								dict ["link"] = string.Format (MergeUrlPaths (AdminSite.Url, "/_admin/ManageWebAppFeatures.aspx?WebApplicationId={1}#{0}"), dict ["id"], site.WebApplication.Id);
								active = (site.WebApplication.Features [featureID] != null);
							} else {
								dict ["link"] = string.Format (MergeUrlPaths (((isSite = (!(isWeb = (dict ["type"] == "WebFeature")))) ? site.RootWeb : web).Url, "/_layouts/ManageFeatures.aspx?Scope={1}#{0}"), dict ["id"], isSite ? "Site" : "Web");
								if (isWeb && string.IsNullOrEmpty (allWebsSelect))
									allWebsSelect = createAllWebsSelect (site.RootWeb);
								active = ((isSite ? site.Features : web.Features) [featureID] != null);
							}
							dict ["info"] = this ["WssInfo" + (isWeb ? "Web" : "Other") + "Feature", isWeb ? allWebsSelect : string.Empty] + " " + this ["WssInfo" + (active ? "A" : "Ina") + "ctive", dict ["link"]];
						}
						dict ["title"] = dict ["title"].Substring (dict ["title"].IndexOf (']') + 1);
						//if (dict ["title"].EndsWith (")") && ((pos = dict ["title"].IndexOf (" (")) > 0) && (pos < (dict ["title"].Length - 3)))
						//    dict ["title"] = dict ["title"].Substring (0, pos) + "<br/>" + dict ["title"].Substring (pos + 1);
						if (!((dict ["title"] == dict ["id"]) && (dict ["desc"] == this ["WssInfoNope"])))
							yield return dict;
					}
				if ((site != null) && Elevated)
					site.Dispose ();
			}
		}

		// FILTER_END

	}

}
