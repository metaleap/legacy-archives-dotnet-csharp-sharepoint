
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Design;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Xml;

namespace roxority.Shared {

	using Collections;

#if NET_3_0
	internal delegate void Action ();
	internal delegate void Action<T, U> (T t, U u);
	internal delegate void Action<T, U, V> (T t, U u, V v);
	internal delegate void Action<T, U, V, W> (T t, U u, V v, W w);
	public delegate T Func<T> ();
#endif
	internal delegate void ActionRef<T, U> (ref T t, ref U u);
	internal delegate bool AssortHandler (object element, params object [] args);
	internal delegate void CancelEventHandler<T> (object sender, CancelEventArgs<T> e);
	internal delegate IEnumerable<T> Enumerator<T> ();
	public delegate T Get<T> ();
	internal delegate void Handler (object sender, object e);
	internal delegate T Operation<T> (T value);
	public delegate TReturn Operation<TParam, TReturn> (TParam value);
	internal delegate TReturn Operation<TParam1, TParam2, TReturn>(TParam1 param1, TParam2 param2);
	internal delegate void Parameterized<T, U> (T t, params U [] u);
	internal delegate bool Predicate ();
	internal delegate bool Predicate<T, U> (T t, U u);

	internal class CancelEventArgs<T> : CancelEventArgs {
		// Fields
		internal readonly T Value;

		// Methods
		internal CancelEventArgs (T value, bool cancel)
			: base (cancel) {
			this.Value = value;
		}
	}

	[Serializable]
	internal class CancelledException : Exception {
		// Methods
		internal CancelledException () {
		}

		internal CancelledException (string message)
			: base (message) {
		}

		protected CancelledException (SerializationInfo info, StreamingContext context)
			: base (info, context) {
		}

		internal CancelledException (string message, Exception inner)
			: base (message, inner) {
		}
	}

	internal enum ComparisonResult {
		None,
		One,
		Two,
		Both
	}

	internal class Context<T> : IContext, IDisposable {
		// Fields
		internal readonly T ContextObject;
		internal readonly Action<T> DisposeAction;

		// Methods
		internal Context (T contextObject, Action<T> disposeAction) {
			this.ContextObject = contextObject;
			this.DisposeAction = disposeAction;
		}

		public void Dispose () {
			if ((this.DisposeAction != null) && (this.ContextObject != null)) {
				this.DisposeAction (this.ContextObject);
			} else if (this.ContextObject is IDisposable) {
				((IDisposable) this.ContextObject).Dispose ();
			}
		}

		// Properties
		object IContext.ContextObject {
			get {
				return this.ContextObject;
			}
		}
	}

	public abstract class ConvertibleBase<T> {
		// Fields
		private Get<T> handler;

		// Methods
		protected ConvertibleBase ()
			: this (null) {
		}

		protected ConvertibleBase (Get<T> handler) {
			this.Handler = handler;
		}

		// Properties
		internal virtual Get<T> Handler {
			get {
				return this.handler;
			}
			set {
				this.handler = value;
			}
		}
	}

	internal class Disposable<T> : IDisposable {
		// Fields
		internal readonly Action<T> DisposeAction;
		internal readonly T Value;

		// Methods
		internal Disposable (T value, Action<T> disposeAction) {
			this.DisposeAction = disposeAction;
			this.Value = value;
		}

		public void Dispose () {
			if (this.DisposeAction != null) {
				this.DisposeAction (this.Value);
			} else if (this.Value is IDisposable) {
				((IDisposable) this.Value).Dispose ();
			}
		}
	}

	[Serializable]
	internal class Duo<T> : Duo<T, T> {
		// Methods
		protected Duo (SerializationInfo info, StreamingContext context)
			: base (info, context) {
		}

		internal Duo (T item1, T item2)
			: base (item1, item2) {
		}

		internal virtual T [] GetArray () {
			return new T [] { base.Value1, base.Value2 };
		}
	}

	[Serializable]
	public class Duo<T, U> : MultipleBase, ISerializable {
		// Fields
		internal readonly T Value1;
		internal readonly U Value2;

		// Methods
		protected Duo (SerializationInfo info, StreamingContext context) {
			this.Value1 = (T) info.GetValue ("item1", typeof (T));
			this.Value2 = (U) info.GetValue ("item2", typeof (U));
		}

		internal Duo (T item1, U item2)
			: this (item1, item2, null) {
			this.Handler = new Get<string> (this.ToStringCore);
		}

		internal Duo (T item1, U item2, Get<string> handler)
			: base (handler) {
			this.Value1 = item1;
			this.Value2 = item2;
		}

		public override bool Equals (object obj) {
			Duo<T, U> objB = obj as Duo<T, U>;
			if (objB == null) {
				return false;
			}
			return (object.ReferenceEquals (this, objB) || (object.Equals (this.Value1, objB.Value1) && object.Equals (this.Value2, objB.Value2)));
		}

		public override int GetHashCode () {
			return SharedUtil.GetHashCode (new object [] { this.Value1, this.Value2 });
		}

		public virtual void GetObjectData (SerializationInfo info, StreamingContext context) {
			info.AddValue ("item1", this.Value1);
			info.AddValue ("item2", this.Value2);
		}

		public static bool operator == (Duo<T, U> one, Duo<T, U> two) {
			return ((object.ReferenceEquals (one, null) && object.ReferenceEquals (two, null)) || ((!object.ReferenceEquals (one, null) && !object.ReferenceEquals (two, null)) && one.Equals (two)));
		}

		public static bool operator != (Duo<T, U> one, Duo<T, U> two) {
			return !(one == two);
		}

		internal virtual Array ToArray () {
			return new object [] { this.Value1, this.Value2 };
		}

		protected virtual string ToStringCore () {
			return string.Format ("{0} ; {1}", this.Value1, this.Value2);
		}
	}

	internal class DuoList<T, U> : List<Duo<T, U>> {
		// Methods
		internal DuoList () {
		}

		internal DuoList (IEnumerable<T> values1, IEnumerable<U> values2)
			: base (SharedUtil.Enumerate<T, U> (values1, values2)) {
		}
	}

	internal class EventArgs<T> : EventArgs {
		// Fields
		private T value;

		// Methods
		internal EventArgs (T value) {
			this.value = value;
		}

		internal EventArgs<T> SetValue (T value) {
			this.Value = value;
			return (EventArgs<T>) this;
		}

		// Properties
		internal T Value {
			get {
				return this.value;
			}
			set {
				this.value = value;
			}
		}
	}

	internal class EventArgs<T, U> : EventArgs<T> {
		// Fields
		private U value2;

		// Methods
		internal EventArgs (T value, U value2)
			: base (value) {
			this.value2 = value2;
		}

		// Properties
		internal U Value2 {
			get {
				return this.value2;
			}
			set {
				this.value2 = value;
			}
		}
	}

	[Serializable]
	internal class Exception<T> : Exception {
		// Fields
		internal readonly T Value;

		// Methods
		internal Exception (T value) {
			this.Value = value;
		}

		protected Exception (SerializationInfo info, StreamingContext context)
			: base (info, context) {
			this.Value = (T) info.GetValue ("Value", typeof (T));
		}

		internal Exception (string message, T value)
			: base (message) {
			this.Value = value;
		}

		internal Exception (string message, T value, Exception inner)
			: base (message, inner) {
			this.Value = value;
		}

		public override void GetObjectData (SerializationInfo info, StreamingContext context) {
			info.AddValue ("Value", this.Value);
			base.GetObjectData (info, context);
		}
	}

	internal interface I<T> {
		// Methods
		void Get (out T value);
		void Set (T value);
	}

	internal interface IContext : IDisposable {
		// Properties
		object ContextObject {
			get;
		}
	}

	internal interface ISelfDescriptor {
		// Properties
		string Description {
			get;
		}
		Image Image {
			get;
		}
		string Title {
			get;
		}
	}

	public abstract class MultipleBase : ConvertibleBase<string> {
		// Methods
		protected MultipleBase ()
			: base (null) {
		}

		protected MultipleBase (Get<string> handler)
			: base (handler) {
		}

		public override string ToString () {
			return this.Handler ();
		}

		// Properties
		internal override Get<string> Handler {
			get {
				return base.Handler;
			}
			set {
				base.Handler = (value == null) ? new Get<string> (this.ToString) : value;
			}
		}
	}

	[AttributeUsage (AttributeTargets.Assembly)]
	internal sealed class ResourceBaseNameAttribute : Attribute {
		// Fields
		internal readonly string ResourceBaseName;

		// Methods
		internal ResourceBaseNameAttribute (string resourceBaseName) {
			this.ResourceBaseName = SharedUtil.IsEmpty (resourceBaseName) ? string.Empty : resourceBaseName.Trim ();
		}
	}

	[Serializable]
	public class Trio<T> : Trio<T, T, T> {
		// Methods
		protected Trio (SerializationInfo info, StreamingContext context)
			: base (info, context) {
		}

		internal Trio (T item1, T item2, T item3)
			: base (item1, item2, item3) {
		}

		internal virtual T [] GetArray () {
			return new T [] { base.Value1, base.Value2, base.Value3 };
		}
	}

	internal static class SharedUtil {

		internal const char CHAR_ELLIPSIS = '…';
		internal const string STRING_ELLIPSIS = "…";
		private const string DEFAULT_RESOURCES_BASENAME = "Properties.Resources";

		internal static readonly Random Random;
		private static HashTree resourceManagers;

		static SharedUtil () {
			Random = new Random ();
			resourceManagers = null;
		}

		internal static bool AnyFlag (Enum value, params Enum [] flags) {
			foreach (Enum enum2 in flags) {
				if ((((int) (object) value) & ((int) (object) enum2)) == ((int) (object) enum2)) {
					return true;
				}
			}
			return false;
		}

		internal static long ApproximateRemainingProgress (long progress, int percent) {
			if ((percent != 0) && (percent < 100)) {
				return (((long) ((100.0 / ((double) percent)) * progress)) - progress);
			}
			return 0L;
		}

		internal static void Assort (Array array, out Array assorted, out Array remainders, AssortHandler handler, params object [] args) {
			ThrowIfEmpty (handler, "handler");
			if (IsEmpty ((ICollection) array)) {
				assorted = null;
				remainders = null;
			} else {
				ArrayList list = new ArrayList (array.Length);
				ArrayList list2 = new ArrayList (array.Length);
				Type elementType = array.GetType ().GetElementType ();
				for (int i = 0; i < array.Length; i++) {
					object obj2;
					if (handler (obj2 = array.GetValue (i), args)) {
						list.Add (obj2);
					} else {
						list2.Add (obj2);
					}
				}
				assorted = list.ToArray (elementType);
				remainders = list2.ToArray (elementType);
			}
		}

		internal static U Batch<T, U> (Operation<T, U> operation, U determinant, U defaultValue, params T [] args) {
			foreach (T local2 in args) {
				U local;
				U local4 = local = operation (local2);
				if (local4.Equals (determinant)) {
					return local;
				}
			}
			return defaultValue;
		}

		internal static T Clone<T> (T value) where T : class, ICloneable {
			if (value != null) {
				return (value.Clone () as T);
			}
			return default (T);
		}

		internal static List<T> Combine<T> (params ICollection [] collections) {
			List<T> list = new List<T> ();
			foreach (ICollection is2 in collections) {
				if (!IsEmpty (is2)) {
					T [] array = new T [is2.Count];
					is2.CopyTo (array, 0);
					list.AddRange (array);
				}
			}
			return list;
		}

		internal static ComparisonResult Compare<T> (T one, T two, Predicate<T> match) {
			bool flag = match (one);
			bool flag2 = match (two);
			if (flag && flag2) {
				return ComparisonResult.Both;
			}
			if (flag) {
				return ComparisonResult.One;
			}
			if (!flag2) {
				return ComparisonResult.None;
			}
			return ComparisonResult.Two;
		}

		internal static bool Contains<TKey, TValue> (Dictionary<TKey, TValue> dictionary, Predicate<TKey> keyMatch, Predicate<TValue> valueMatch) {
			return Contains<TKey, TValue> (dictionary, keyMatch, valueMatch, delegate (Duo<bool> value) {
				if (value.Value1) {
					return value.Value2;
				}
				return false;
			});
		}

		internal static bool Contains<TKey, TValue> (Dictionary<TKey, TValue> dictionary, Predicate<TKey> keyMatch, Predicate<TValue> valueMatch, Predicate<Duo<bool>> combineMatch) {
			foreach (KeyValuePair<TKey, TValue> pair in dictionary) {
				bool flag = (keyMatch == null) || keyMatch (pair.Key);
				bool flag2 = (valueMatch == null) || valueMatch (pair.Value);
				if (combineMatch (new Duo<bool> (flag, flag2))) {
					return true;
				}
			}
			return false;
		}

		internal static void CopyHashtableTo (Hashtable source, Hashtable destination) {
			foreach (DictionaryEntry entry in source) {
				destination [entry.Key] = entry.Value;
			}
		}

		internal static T [] CreateArray<T> (IEnumerable<T> values, Predicate<T> match) {
			List<T> list = new List<T> ();
			if (values != null) {
				if (match == null) {
					list.AddRange (values);
				} else {
					foreach (T local in values) {
						if (match (local)) {
							list.Add (local);
						}
					}
				}
			}
			return list.ToArray ();
		}

		internal static TArray [] CreateArray<TArray, TSource> (IEnumerable<TSource> values, Operation<TSource, TArray> operation) {
			return CreateArray<TArray, TSource> (values, operation, false);
		}

		internal static TArray [] CreateArray<TArray, TSource> (IEnumerable<TSource> values, Operation<TSource, TArray> operation, bool nullIfEmpty) {
			List<TArray> list = new List<TArray> ();
			if (values != null) {
				foreach (TSource local in values) {
					list.Add (operation (local));
				}
			} else if (nullIfEmpty) {
				return null;
			}
			if (nullIfEmpty && (list.Count == 0)) {
				return null;
			}
			return list.ToArray ();
		}

		internal static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue> (params Duo<TKey, TValue> [] items) {
			Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue> ();
			foreach (Duo<TKey, TValue> duo in items) {
				dictionary [duo.Value1] = duo.Value2;
			}
			return dictionary;
		}

		internal static Dictionary<T, T> CreateDictionary<T> (params T [] keysValues) {
			List<Duo<T, T>> list = new List<Duo<T, T>> ();
			for (int i = 0; i < keysValues.Length; i += 2) {
				list.Add (new Duo<T, T> (keysValues [i], keysValues [i + 1]));
			}
			return CreateDictionary<T, T> (list.ToArray ());
		}

		internal static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue> (IEnumerable<TKey> keys, IEnumerable<TValue> values) {
			Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue> ();
			foreach (Duo<TKey, TValue> duo in Enumerate<TKey, TValue> (keys, values)) {
				dictionary [duo.Value1] = duo.Value2;
			}
			return dictionary;
		}

		internal static Hashtable CreateHashtable (IDataRecord record) {
			if (record == null) {
				return null;
			}
			Hashtable hashtable = new Hashtable ();
			for (int i = 0; i < record.FieldCount; i++) {
				hashtable [record.GetName (i)] = record [i];
			}
			return hashtable;
		}

		internal static object CreateInstance (Type type, params object [] args) {
			if (type.IsArray) {
				return Array.CreateInstance (type.GetElementType (), (int) args [0]);
			}
			ConstructorInfo constructor = type.GetConstructor (GetTypes (args));
			if (constructor == null) {
				return Activator.CreateInstance (type, args);
			}
			return constructor.Invoke (args);
		}

		internal static LinkArea CreateLinkArea (LinkLabel linkLabel, SeekOrigin seekOrigin, int length) {
			switch (seekOrigin) {
				case SeekOrigin.Begin:
					return new LinkArea (0, length);

				case SeekOrigin.End:
					return new LinkArea (linkLabel.Text.Length - length, length);
			}
			throw new ArgumentException (null, "seekOrigin");
		}

		internal static List<T> CreateList<T> (IEnumerable<T> source, bool nullIfEmpty) {
			List<T> list = (source == null) ? new List<T> () : new List<T> (source);
			if (nullIfEmpty && IsEmpty ((ICollection) list)) {
				return null;
			}
			return list;
		}

		internal static List<T> CreateList<T> (IEnumerable<T> source, Predicate<T> match) {
			List<T> list = new List<T> ();
			if (source != null) {
				foreach (T local in source) {
					if ((local != null) && ((match == null) || match (local))) {
						list.Add (local);
					}
				}
			}
			return list;


		}

		internal static string DefaultJoinOperation<T> (T value) where T : class {
			if (value != null) {
				return value.ToString ();
			}
			return null;
		}

		internal static string DefaultJoinStructOperation<T> (T value) where T : struct {
			return value.ToString ();
		}

		internal static T DescendentControl<T> (Control control) where T : Control {
			if ((control == null) || (control.Parent == null)) {
				return default (T);
			}
			if (control.Parent is T) {
				return (control.Parent as T);
			}
			return DescendentControl<T> (control.Parent);
		}

		internal static T Deserialize<T> (SerializationInfo info, string name, T defaultValue) {
			try {
				return (T) info.GetValue (name, typeof (T));
			} catch {
				return defaultValue;
			}
		}

		internal static object DeserializeBinary (byte [] value) {
			if (value != null) {
				using (MemoryStream stream = new MemoryStream (value, false)) {
					return new BinaryFormatter ().Deserialize (stream);
				}
			}
			return null;
		}

		internal static object DeserializeBinary (IDataObject value) {
			if ((value != null) && value.GetDataPresent (typeof (byte []))) {
				return DeserializeBinary (value.GetData (typeof (byte [])) as byte []);
			}
			return null;
		}

		internal static T Do<T> (T value, params Operation<T, T> [] actions) {
			foreach (Operation<T, T> operation in actions) {
				value = operation (value);
			}
			return value;
		}

		internal static IEnumerable<Duo<T, U>> Enumerate<T, U> (params object [] values) {
			throw new NotSupportedException ();
		}

		internal static IEnumerable<T> Enumerate<T> (params object [] values) {
			throw new NotSupportedException ();
		}

		internal static IEnumerable<T> Enumerate<T> (IEnumerable source) {
			throw new NotSupportedException ();
		}

		internal static IEnumerable<Duo<T, U>> Enumerate<T, U> (IEnumerable<T> t, IEnumerable<U> u) {
			throw new NotSupportedException ();
		}

		internal static IEnumerable<TValue> Enumerate<TTagged, TValue> (IEnumerable taggedObjects, Operation<TTagged, TValue> convertTag) {
			throw new NotSupportedException ();
		}

		internal static bool Equals<T> (T [] one, T [] two) {
			if ((one != null) || (two != null)) {
				if ((one == null) || (two == null)) {
					return false;
				}
				if (!object.ReferenceEquals (one, two)) {
					if (one.Length != two.Length) {
						return false;
					}
					for (int i = 0; i < one.Length; i++) {
						if (!Equals (one [i], two [i])) {
							return false;
						}
					}
				}
			}
			return true;
		}

		internal static new bool Equals (object one, object two) {
			if ((one != null) || (two != null)) {
				if ((one == null) || (two == null)) {
					return false;
				}
				if (!object.ReferenceEquals (one, two)) {
					return one.Equals (two);
				}
			}
			return true;
		}

		internal static IEnumerable<TReturn> FetchUnique<TReturn, TList> (IEnumerable<TList> values, Operation<TList, TReturn> getter) {
			List<TReturn> list = new List<TReturn> ();
			foreach (TList local2 in values) {
				TReturn local;
				if (((local = getter (local2)) != null) && !list.Contains (local)) {
					list.Add (local);
				}
			}
			return list;
		}

		internal static Duo<TKey, TValue> Find<TKey, TValue> (Dictionary<TKey, TValue> dictionary, Predicate<TValue> match) {
			foreach (KeyValuePair<TKey, TValue> pair in dictionary) {
				if (match (pair.Value)) {
					return new Duo<TKey, TValue> (pair.Key, pair.Value);
				}
			}
			return null;
		}

		internal static object FromBase64String (string value) {
			if (IsEmpty (value)) {
				return null;
			}
			using (MemoryStream stream = new MemoryStream (Convert.FromBase64String (value), false)) {
				return new BinaryFormatter ().Deserialize (stream);
			}
		}

		internal static bool GetConfig (string configName, bool defaultValue) {
			try {
				return bool.Parse (GetConfig (configName, defaultValue.ToString ()));
			} catch {
				return defaultValue;
			}
		}

		internal static IsolationLevel GetConfig (string configName, IsolationLevel defaultValue) {
			try {
				return (IsolationLevel) Enum.Parse (typeof (IsolationLevel), GetConfig (configName, defaultValue.ToString ()), true);
			} catch {
				return defaultValue;
			}
		}

		internal static string GetConfig (string configName, string defaultValue) {
			try {
				string str;
				return (IsEmpty (str = ConfigurationManager.AppSettings [configName]) ? defaultValue : str);
			} catch {
				return defaultValue;
			}
		}

		internal static string GetExceptionMessages (Exception ex) {
			StringBuilder buffer = new StringBuilder ();
			GetExceptionMessages (ex, buffer, 0);
			return buffer.ToString ();
		}

		internal static void GetExceptionMessages (Exception ex, StringBuilder buffer, int level) {
			if (level > 0) {
				buffer.Append ('\t', level);
			}
			buffer.Append (ex.Message);
			if (ex.InnerException != null) {
				buffer.AppendLine ();
				GetExceptionMessages (ex.InnerException, buffer, level + 1);
			}
		}

		internal static int GetHashCode (Array values) {
			object obj2;
			if (IsEmpty ((ICollection) values)) {
				return 0;
			}
			int num = ((obj2 = values.GetValue (0)) == null) ? 0 : obj2.GetHashCode ();
			if (values.Length > 1) {
				for (int i = 1; i < values.Length; i++) {
					obj2 = values.GetValue (i);
					if (obj2 is Array)
						num ^= GetHashCode (obj2 as Array);
					else if (obj2 != null)
						num ^= obj2.GetHashCode ();
				}
			}
			return num;
		}

		internal static int GetHashCode (params object [] values) {
			return GetHashCode ((Array) values);
		}

		internal static int GetIndex (string text, int line, int column, bool zeroBased) {
			int num = 0;
			if (!zeroBased) {
				return GetIndex (text, line - 1, column - 1, true);
			}
			for (int i = 0; i < line; i++) {
				int num2;
				if ((((num2 = text.IndexOf ("\r\n", (int) (num + 2))) < num) && ((num2 = text.IndexOf ("\n", (int) (num + 1))) < num)) && ((num2 = text.IndexOf ("\r", (int) (num + 1))) < num)) {
					break;
				}
				num = num2;
			}
			return (num + column);
		}

		internal static ResourceManager GetResourceManager (Assembly assembly) {
			return GetResourceManager (assembly, null);
		}

		internal static ResourceManager GetResourceManager (Assembly assembly, string baseName) {
			ResourceManager manager = null;
			bool flag = false;
			ThrowIfEmpty (assembly, "assembly");
			if (IsEmpty (baseName, true)) {
				ResourceBaseNameAttribute customAttribute = Attribute.GetCustomAttribute (assembly, typeof (ResourceBaseNameAttribute)) as ResourceBaseNameAttribute;
				if (customAttribute == null) {
					baseName = assembly.GetName ().Name + ".Properties.Resources";
				} else {
					baseName = customAttribute.ResourceBaseName;
				}
			}
			foreach (string str in assembly.GetManifestResourceNames ()) {
				if (flag = str == (baseName + ".resources")) {
					break;
				}
			}
			if (flag && ((manager = ResourceManagers [new object [] { assembly, baseName }] as ResourceManager) == null)) {
				lock (resourceManagers) {
					resourceManagers [new object [] { assembly, baseName }] = manager = new ResourceManager (baseName, assembly);
				}
			}
			return manager;
		}

		internal static string GetSafeString (string value) {
			return GetSafeString (new StringBuilder (value)).ToString ();
		}

		internal static StringBuilder GetSafeString (StringBuilder value) {
			for (int i = 0; i < value.Length; i++) {
				if (!IsCharSafe (value [i])) {
					value [i] = '_';
				}
			}
			return value;
		}

		internal static string GetString (string name, params object [] args) {
			string str = GetString (GetResourceManager (Assembly.GetCallingAssembly ()), name, args);
			if (!IsEmpty (str)) {
				return str;
			}
			return GetString (GetResourceManager (typeof (SharedUtil).Assembly), name, args);
		}

		internal static string GetString (ResourceManager resources, string name, params object [] args) {
			return GetString (resources, name, CultureInfo.CurrentUICulture, args);
		}

		internal static string GetString (string name, CultureInfo culture, params object [] args) {
			string str = GetString (GetResourceManager (Assembly.GetCallingAssembly ()), name, culture, args);
			if (!IsEmpty (str)) {
				return str;
			}
			return GetString (GetResourceManager (typeof (SharedUtil).Assembly), name, culture, args);
		}

		internal static string GetString (ResourceManager resources, string name, CultureInfo culture, params object [] args) {
			try {
				if (resources == null) {
					resources = GetResourceManager (typeof (SharedUtil).Assembly);
				}
				string format = resources.GetString (name, culture);
				if (format == null) {
					format = string.Empty;
				}
				format = format.Replace (@"\r", "\r").Replace (@"\n", "\n").Replace (@"\t", "\t");
				if ((args == null) || (args.Length == 0)) {
					return format;
				}
				return string.Format (format, args);
			} catch {
				return string.Empty;
			}
		}

		internal static string [] GetStrings (params string [] values) {
			return values;
		}

		internal static TReturn GetTag<TParam, TReturn> (TParam value)
			where TParam : Control
			where TReturn : class {
			if (value != null) {
				return (value.Tag as TReturn);
			}
			return default (TReturn);
		}

		internal static Predicate<TPredicate> GetTypeCheckPredicate<TCheck, TPredicate> () {
			return delegate (TPredicate value) {
				return (value is TCheck);
			};
		}

		internal static string GetTypeDescription (Type type) {
			string typeDescription = GetTypeDescription (Assembly.GetCallingAssembly (), type);
			if (IsEmpty (typeDescription)) {
				try {
					typeDescription = GetTypeDescription (type.Assembly, type);
				} catch {
				}
			}
			if (IsEmpty (typeDescription)) {
				typeDescription = GetTypeDescription ((ResourceManager) null, type);
			}
			return typeDescription;
		}

		internal static string GetTypeDescription (Assembly assembly, Type type) {
			return GetTypeDescription ((assembly == null) ? null : GetResourceManager (assembly), type);
		}

		internal static string GetTypeDescription (ResourceManager resources, Type type) {
			string str;
			if (type == null) {
				throw new ArgumentNullException ("type");
			}
			if (resources == null) {
				resources = GetResourceManager (typeof (SharedUtil).Assembly);
			}
			if (IsEmpty (str = GetString (resources, "T_Desc_" + type.Name, new object [0])) && IsEmpty (str = GetString (resources, type.FullName + ".Description", new object [0]))) {
				str = GetString (resources, type.Name + ".Description", new object [0]);
			}
			if (!IsEmpty (str)) {
				return str;
			}
			if (type.BaseType != null) {
				return GetTypeDescription (resources, type.BaseType);
			}
			return string.Empty;
		}

		internal static Image GetTypeImage (Type type) {
			return GetTypeImage (GetResourceManager (type.Assembly), type);
		}

		internal static Image GetTypeImage (ResourceManager resources, Type type) {
			Image image = null;
			while (((image == null) && (type != null)) && (type != typeof (object))) {
				image = resources.GetObject ("Image_" + type.Name) as Image;
				type = type.BaseType;
			}
			return image;
		}

		internal static Type [] GetTypes (params object [] args) {
			Type [] typeArray = new Type [(args == null) ? 0 : args.Length];
			for (int i = 0; i < typeArray.Length; i++) {
				typeArray [i] = (args [i] == null) ? null : args [i].GetType ();
			}
			return typeArray;
		}

		internal static Type [] GetTypes (Type type, int length) {
			Type [] typeArray = new Type [length];
			for (int i = 0; i < length; i++) {
				typeArray [i] = type;
			}
			return typeArray;
		}

		internal static string GetTypeTitle (Type type) {
			return GetTypeTitle (type, null);
		}

		internal static string GetTypeTitle (Assembly assembly, Type type) {
			return GetTypeTitle (assembly, type, null);
		}

		internal static string GetTypeTitle (ResourceManager resources, Type type) {
			return GetTypeTitle (resources, type, null);
		}

		internal static string GetTypeTitle (Type type, CultureInfo culture) {
			string str = string.Empty;
			try {
				str = GetTypeTitle (Assembly.GetCallingAssembly (), type, culture);
			} catch {
			}
			if (IsEmpty (str)) {
				try {
					str = GetTypeTitle (type.Assembly, type, culture);
				} catch {
				}
			}
			if (IsEmpty (str)) {
				str = GetTypeTitle ((ResourceManager) null, type, culture);
			}
			if (str != null) {
				return str;
			}
			return string.Empty;
		}

		internal static string GetTypeTitle (Assembly assembly, Type type, CultureInfo culture) {
			return GetTypeTitle ((assembly == null) ? null : GetResourceManager (assembly), type, culture);
		}

		internal static string GetTypeTitle (ResourceManager resources, Type type, CultureInfo culture) {
			string str;
			if (type == null) {
				throw new ArgumentNullException ("type");
			}
			if (resources == null) {
				resources = GetResourceManager (typeof (SharedUtil).Assembly);
			}
			if (culture == null) {
				if (IsEmpty (str = GetString (resources, "T_Title_" + type.Name, new object [0])) && IsEmpty (str = GetString (resources, type.Name + ".Title", new object [0]))) {
					str = GetString (resources, type.FullName + ".Title", new object [0]);
				}
			} else if (IsEmpty (str = GetString (resources, "T_Title_" + type.Name, culture, new object [0])) && IsEmpty (str = GetString (resources, type.Name + ".Title", culture, new object [0]))) {
				str = GetString (resources, type.FullName + ".Title", culture, new object [0]);
			}
			if (!IsEmpty (str)) {
				return str;
			}
			if (type.BaseType != null) {
				return GetTypeTitle (resources, type.BaseType, culture);
			}
			return string.Empty;
		}

		internal static string Hash (string value, HashAlgorithm provider, bool replace, bool base64) {
			if (IsEmpty (value))
				return string.Empty;
			value = (base64 ? Convert.ToBase64String (provider.ComputeHash (Encoding.UTF8.GetBytes (value)), Base64FormattingOptions.None) : Encoding.UTF8.GetString (provider.ComputeHash (Encoding.UTF8.GetBytes (value))));
			if (replace)
				value = ReplaceCharacters (value, delegate (char oldChar) {
					return !char.IsLetterOrDigit (oldChar);
				}, '_');
			return value;
		}

		internal static string Hash (string value, bool bothMd5AndSha) {
			string md5, sha = string.Empty;
			using (MD5CryptoServiceProvider provider = new MD5CryptoServiceProvider ())
				md5 = Hash (value, provider, true, false);
#if !NET_3_0
			if (bothMd5AndSha)
				using (SHA384CryptoServiceProvider provider = new SHA384CryptoServiceProvider ())
					sha = Hash (value, provider, true, false);
#endif
			return md5 + sha;
		}

		internal static bool In<T> (T value, IEnumerable<T> values) {
			if (values != null) {
				foreach (T local in values) {
					if (Equals (value, local)) {
						return true;
					}
				}
			}
			return false;
		}

		internal static bool In<T> (T value, params T [] values) {
			return (Array.IndexOf<T> (values, value) >= 0);
		}

		internal static int IndexOfOrLength (object value, char c) {
			return IndexOfOrLength ((value == null) ? string.Empty : value.ToString (), c);
		}

		internal static int IndexOfOrLength (string value, char c) {
			int index = value.IndexOf (c);
			if (index > 0) {
				return index;
			}
			return value.Length;
		}

		internal static string InsertSpaces (string value) {
			StringBuilder builder = new StringBuilder ();
			if ((value == null) || (value.Length == 0)) {
				return string.Empty;
			}
			for (int i = 0; i < value.Length; i++) {
				if (((char.IsUpper (value, i) && (i > 0)) && ((i == (value.Length - 1)) || char.IsLower (value, i + 1))) && !char.IsWhiteSpace (value, i - 1)) {
					builder.Append (new char [] { ' ', value [i] });
				} else if (((char.IsNumber (value, i) && (i > 0)) && ((i == (value.Length - 1)) || char.IsLetter (value, i + 1))) && !char.IsWhiteSpace (value, i - 1)) {
					builder.Append (new char [] { ' ', value [i] });
				} else {
					builder.Append (value, i, 1);
				}
			}
			return builder.ToString ();
		}

		internal static bool InvokeIfRequired<TReturn> (Control control, Action action, ref TReturn result) {
			if ((control != null) && control.InvokeRequired) {
				result = (TReturn) control.Invoke (action);
				return true;
			}
			return false;
		}

		internal static bool InvokeIfRequired<TReturn, TParam> (Control control, Action<TParam> action, TParam param, ref TReturn result) {
			if ((control != null) && control.InvokeRequired) {
				result = (TReturn) control.Invoke (action, new object [] { param });
				return true;
			}
			return false;
		}

		internal static bool InvokeIfRequired<TReturn, TParam1, TParam2> (Control control, Action<TParam1, TParam2> action, TParam1 param1, TParam2 param2, ref TReturn result) {
			if ((control != null) && control.InvokeRequired) {
				result = (TReturn) control.Invoke (action, new object [] { param1, param2 });
				return true;
			}
			return false;
		}

		internal static bool InvokeIfRequired<TReturn, TParam1, TParam2, TParam3> (Control control, Action<TParam1, TParam2, TParam3> action, TParam1 param1, TParam2 param2, TParam3 param3, ref TReturn result) {
			if ((control != null) && control.InvokeRequired) {
				result = (TReturn) control.Invoke (action, new object [] { param1, param2, param3 });
				return true;
			}
			return false;
		}

		internal static bool InvokeIfRequiredVoid (Control control, Action action) {
			if ((control != null) && control.InvokeRequired) {
				control.Invoke (action);
				return true;
			}
			return false;
		}

		internal static bool InvokeIfRequiredVoid<TParam> (Control control, Action<TParam> action, TParam param) {
			if ((control != null) && control.InvokeRequired) {
				control.Invoke (action, new object [] { param });
				return true;
			}
			return false;
		}

		internal static bool InvokeIfRequiredVoid<TParam1, TParam2> (Control control, Action<TParam1, TParam2> action, TParam1 param1, TParam2 param2) {
			if ((control != null) && control.InvokeRequired) {
				control.Invoke (action, new object [] { param1, param2 });
				return true;
			}
			return false;
		}

		internal static bool InvokeIfRequiredVoid<TParam1, TParam2, TParam3> (Control control, Action<TParam1, TParam2, TParam3> action, TParam1 param1, TParam2 param2, TParam3 param3) {
			if ((control != null) && control.InvokeRequired) {
				control.Invoke (action, new object [] { param1, param2, param3 });
				return true;
			}
			return false;
		}

		internal static bool IsCharSafe (char value) {
			return ((char.IsNumber (value) || ((value >= 'A') && (value <= 'Z'))) || ((value >= 'a') && (value <= 'z')));
		}

		internal static bool IsEmpty (ICollection value) {
			if (value != null) {
				return (value.Count == 0);
			}
			return true;
		}

		internal static bool IsEmpty (object value) {
			if (value is ICollection) {
				return IsEmpty ((ICollection) value);
			}
			if (value is string) {
				return IsEmpty ((string) value);
			}
			return (value == null);
		}

		internal static bool IsEmpty (string value) {
			return string.IsNullOrEmpty (value);
		}

		internal static bool IsEmpty (Uri uri) {
			if (uri != null) {
				return IsEmpty (uri.ToString ());
			}
			return true;
		}

		internal static bool IsEmpty<T, U> (Duo<T, U> tuple) {
			return ((tuple == null) || (IsEmpty (tuple.Value1) && IsEmpty (tuple.Value2)));
		}

		internal static bool IsEmpty (ref string value) {
			string str;
			value = str = Trim (value);
			return (str.Length == 0);
		}

		internal static bool IsEmpty (XmlNodeList list) {
			if (list != null) {
				return (list.Count == 0);
			}
			return true;
		}

		internal static bool IsEmpty (ICollection value, bool deep) {
			bool flag = (value == null) || (value.Count == 0);
			if (deep && !flag) {
				foreach (object obj2 in value) {
					if (flag |= IsEmpty (obj2)) {
						return flag;
					}
				}
			}
			return flag;
		}

		internal static bool IsEmpty (string value, bool trim) {
			if (trim) {
				if (value != null) {
					return (value.Trim ().Length == 0);
				}
				return true;
			}
			if (value != null) {
				return (value.Length == 0);
			}
			return true;
		}

		internal static bool IsEmptyEnumerable<T> (IEnumerable<T> values) {
			if (values != null) {
				return IsEmpty ((ICollection) new List<T> (values));
			}
			return true;
		}

		internal static bool IsOperator (MethodInfo method) {
			return (((method != null) && method.IsSpecialName) && method.Name.StartsWith ("op_"));
		}

		internal static string Join (string separator, params object [] values) {
			string [] strArray = new string [IsEmpty ((ICollection) values) ? 0 : values.Length];
			if (strArray.Length > 0) {
				for (int i = 0; i < values.Length; i++) {
					strArray [i] = (values [i] == null) ? string.Empty : values [i].ToString ();
				}
			}
			return string.Join (separator, strArray);
		}

		internal static string Join<T> (string separator, IEnumerable<T> values, Operation<T, string> operation) where T : class {
			if (operation == null) {
				operation = new Operation<T, string> (SharedUtil.DefaultJoinOperation<T>);
			}
			return string.Join (separator, CreateArray<string, T> (values, operation));
		}

		internal static string JoinStruct<T> (string separator, IEnumerable<T> values, Operation<T, string> operation) where T : struct {
			if (operation == null) {
				operation = new Operation<T, string> (SharedUtil.DefaultJoinStructOperation<T>);
			}
			return string.Join (separator, CreateArray<string, T> (values, operation));
		}

		internal static T Last<T> (List<T> list) {
			return list [list.Count - 1];
		}

		internal static bool MatchAll<T> (List<T> one, List<T> two, Predicate<Duo<T>> match) {
			if (one.Count != two.Count) {
				return false;
			}
			for (int i = 0; i < one.Count; i++) {
				if (!match (new Duo<T> (one [i], two [i]))) {
					return false;
				}
			}
			return true;
		}

		internal static int ParseInt (object value, int defaultValue) {
			int num;
			if (!int.TryParse (value.ToString (), out num)) {
				return defaultValue;
			}
			return num;
		}

		internal static double Percent (double value, double hundred) {
			return ((value * 100.0) / hundred);
		}

		internal static int Percent (int value, int hundred) {
			return (int) Percent ((double) value, (double) hundred);
		}

		internal static int Percent (long value, long hundred) {
			return (int) Percent ((double) value, (double) hundred);
		}

		internal static double PercentOf (double percent, double of) {
			return ((of / 100.0) * percent);
		}

		internal static int PercentOf (int percent, int of) {
			return (int) PercentOf ((double) percent, (double) of);
		}

		internal static long PercentOf (long percent, long of) {
			return (long) PercentOf ((double) percent, (double) of);
		}

		internal static void RemoveDuplicates<T> (List<T> list) {
			for (int i = 0; i < list.Count; i++) {
				int num;
				while ((num = list.IndexOf (list [i], i + 1)) > i) {
					list.RemoveAt (num);
				}
			}
		}

		internal static string RemoveSentences (string value, string sentenceClosingIndicator, int count, SeekOrigin seekOrigin) {
			if ((IsEmpty (value) || IsEmpty (sentenceClosingIndicator)) || ((count <= 0) || (seekOrigin == SeekOrigin.Current))) {
				return value;
			}
			List<string> list = new List<string> (value.Split (new string [] { sentenceClosingIndicator }, StringSplitOptions.RemoveEmptyEntries));
			if (count == list.Count) {
				list.Clear ();
			} else if (list.Count > count) {
				for (int i = 0; i < count; i++) {
					list.RemoveAt ((seekOrigin == SeekOrigin.Begin) ? 0 : (list.Count - 1));
				}
			}
			if (list.Count != 1) {
				return string.Join (sentenceClosingIndicator, list.ToArray ());
			}
			return (list [0] + sentenceClosingIndicator);
		}

		internal static string ReplaceCharacters (string value, string characters, string newValue) {
			foreach (char c in characters)
				value = value.Replace (c.ToString (), newValue);
			return value;
		}

		internal static string ReplaceCharacters (string value, Predicate<char> match, char newChar) {
			StringBuilder builder = new StringBuilder (value.Length);
			for (int i = 0; i < value.Length; i++) {
				builder.Append (match (value [i]) ? newChar : value [i]);
			}
			return builder.ToString ();
		}

		internal static string ReplaceRepeatedly (string value, params string [] replaceValues) {
			if ((replaceValues != null) && (replaceValues.Length > 1))
				for (int i = 0; i < (replaceValues.Length - 1); i++)
					while (value.IndexOf (replaceValues [i]) >= 0)
						value = value.Replace (replaceValues [i], replaceValues [replaceValues.Length - 1]);
			return value;
		}

		internal static string Reverse (string value) {
			char [] array = value.ToCharArray ();
			Array.Reverse (array);
			return new string (array);
		}

		internal static bool RunThread (Action action, Predicate doCancel) {
			bool aborted = false;
			Thread t = new Thread (delegate () {
				try {
					action ();
				} catch (ThreadAbortException) {
					aborted = true;
				}
			});
			try {
				t.Start ();
				while (t.ThreadState != System.Threading.ThreadState.Stopped)
					if (aborted = doCancel ()) {
						t.Abort ();
						break;
					}
			} catch (ThreadAbortException) {
				aborted = true;
			}
			return !aborted;
		}

		internal static void Serialize (SerializationInfo info, params object [] namesValues) {
			for (int i = 1; i < namesValues.Length; i += 2) {
				info.AddValue (namesValues [i - 1].ToString (), namesValues [i]);
			}
		}

		internal static byte [] SerializeBinary (object value) {
			using (MemoryStream stream = new MemoryStream ()) {
				new BinaryFormatter ().Serialize (stream, value);
				return stream.ToArray ();
			}
		}

		internal static T [] SubArray<T> (T [] source, int index, int length) {
			T [] destinationArray = new T [length];
			Array.Copy (source, index, destinationArray, 0, (length > source.Length) ? source.Length : length);
			return destinationArray;
		}

		internal static string Substring (string value, int startIndex, int length) {
			if ((IsEmpty (value) || (startIndex < 0)) || ((length < 0) || (startIndex >= value.Length))) {
				return string.Empty;
			}
			if ((startIndex + length) < value.Length) {
				return value.Substring (startIndex, length);
			}
			return value.Substring (startIndex);
		}

		internal static void ThrowIfEmpty (ICollection value, string paramName) {
			ThrowIfEmpty (value, false, paramName);
		}

		internal static void ThrowIfEmpty (object value, string paramName) {
			if (IsEmpty (value)) {
				throw new ArgumentNullException (paramName);
			}
		}

		internal static void ThrowIfEmpty (string value, string paramName) {
			if (IsEmpty (value)) {
				throw new ArgumentNullException (paramName);
			}
		}

		internal static void ThrowIfEmpty (ICollection value, bool deep, string paramName) {
			if (IsEmpty (value, deep)) {
				throw new ArgumentNullException (paramName);
			}
		}

		internal static void ThrowIfNull (object value, string paramName) {
			if (value == null) {
				throw new ArgumentNullException (paramName);
			}
		}

		internal static void ThrowIfNull (ref string value, string paramName) {
			if (IsEmpty (ref value)) {
				throw new ArgumentNullException (paramName);
			}
		}

		internal static string ToBase64String (object value) {
			if (value == null) {
				return string.Empty;
			}
			using (MemoryStream stream = new MemoryStream ()) {
				new BinaryFormatter ().Serialize (stream, value);
				return Convert.ToBase64String (stream.ToArray ());
			}
		}

		internal static Hashtable ToDictionary (params object [] values) {
			Hashtable dictionary = new Hashtable ((values == null) ? 0 : (values.Length / 2));
			ToDictionary (dictionary, values);
			return dictionary;
		}

		internal static void ToDictionary (IDictionary dictionary, params object [] values) {
			if (!IsEmpty ((ICollection) values) && ((values.Length % 2) == 0)) {
				for (int i = 1; i < values.Length; i += 2) {
					dictionary [values [i - 1]] = values [i];
				}
			}
		}

		internal static string ToString (object value, string ifNull, TypeConverter converter) {
			if ((converter != null) && converter.CanConvertTo (typeof (string))) {
				return (converter.ConvertTo (value, typeof (string)) as string);
			}
			if (value != null) {
				return value.ToString ();
			}
			return ifNull;
		}

		internal static string Trim (string value) {
			if (value != null) {
				return value.Trim ();
			}
			return string.Empty;
		}

		internal static string TrimLength (string value, int maxLength) {
			return TrimLength (value, maxLength, true);
		}

		internal static string TrimLength (string value, int maxLength, bool ellipsis) {
			if (value == null) {
				return null;
			}
			if (value.Length > maxLength) {
				return (value.Substring (0, maxLength) + (ellipsis ? "…" : string.Empty));
			}
			return value;
		}

		internal static bool TrueForAll<T> (IEnumerable<T> values, Predicate<T> match) {
			foreach (T local in values) {
				if (!match (local)) {
					return false;
				}
			}
			return true;
		}

		internal static bool TrueForAll<T> (IEnumerable values, Predicate<T> match) where T : class {
			foreach (object obj2 in values) {
				T local;
				if (((local = obj2 as T) != null) && !match (local)) {
					return false;
				}
			}
			return true;
		}

		internal static bool TrueForAll<T> (Predicate<T> match, params T [] values) {
			return TrueForAll<T> (values, match);
		}

		internal static bool TrueForAny<T> (IEnumerable<T> values, Predicate<T> match) {
			foreach (T local in values) {
				if (match (local)) {
					return true;
				}
			}
			return false;
		}

		internal static bool TrueForAny<T> (IEnumerable values, Predicate<T> match) where T : class {
			foreach (object obj2 in values) {
				T local;
				if (((local = obj2 as T) != null) && match (local)) {
					return true;
				}
			}
			return false;
		}

		internal static void Wait (TimeSpan duration, bool doEvents, params Action [] actions) {
			DateTime now = DateTime.Now;
			while ((DateTime.Now.Ticks - now.Ticks) < duration.Ticks) {
				if (doEvents) {
					Application.DoEvents ();
				}
			}
			if (actions != null) {
				foreach (Action action in actions) {
					if (action != null) {
						action ();
					}
				}
			}
		}

		internal static void Walk<T> (IEnumerable values, Action<T> action) where T : class {
			if (values != null) {
				foreach (object obj2 in values) {
					T local = obj2 as T;
					if (local != null) {
						action (local);
					}
				}
			}
		}

		private static HashTree ResourceManagers {
			get {
				if (resourceManagers == null) {
					resourceManagers = new HashTree ();
				}
				return resourceManagers;
			}
		}

	}

	[Serializable]
	public class Trio<T, U, V> : Duo<T, U> {
		// Fields
		internal readonly V Value3;

		// Methods
		protected Trio (SerializationInfo info, StreamingContext context)
			: base (info, context) {
			this.Value3 = (V) info.GetValue ("item3", typeof (V));
		}

		internal Trio (T item1, U item2, V item3)
			: this (item1, item2, item3, null) {
			this.Handler = new Get<string> (this.ToStringCore);
		}

		internal Trio (T item1, U item2, V item3, Get<string> handler)
			: base (item1, item2, handler) {
			this.Value3 = item3;
		}

		public override bool Equals (object obj) {
			Trio<T, U, V> trio = obj as Trio<T, U, V>;
			if (trio == null) {
				return false;
			}
			return (object.ReferenceEquals (this, obj) || ((object.Equals (base.Value1, trio.Value1) && object.Equals (base.Value2, trio.Value2)) && object.Equals (this.Value3, trio.Value3)));
		}

		public override int GetHashCode () {
			return SharedUtil.GetHashCode (new object [] { base.Value1, base.Value2, this.Value3 });
		}

		public override void GetObjectData (SerializationInfo info, StreamingContext context) {
			base.GetObjectData (info, context);
			info.AddValue ("item3", this.Value3);
		}

		internal override Array ToArray () {
			return new object [] { base.Value1, base.Value2, this.Value3 };
		}

		protected override string ToStringCore () {
			return string.Format ("{0} ; {1}; {2}", base.Value1, base.Value2, this.Value3);
		}
	}

	internal class TrioList<T, U, V> : List<Trio<T, U, V>> {
	}

	internal class UndoRedoManager {
		// Fields
		internal readonly string Original;
		private readonly Stack<Trio<Modification, int, string> []> redoStack = new Stack<Trio<Modification, int, string> []> ();
		private readonly Stack<Trio<Modification, int, string> []> undoStack = new Stack<Trio<Modification, int, string> []> ();

		// Methods
		internal UndoRedoManager (string original) {
			this.Original = (original == null) ? string.Empty : original;
		}

		internal void AddChange (string previousVersion, string currentVersion) {
			if (previousVersion == null) {
				previousVersion = string.Empty;
			}
			if (currentVersion == null) {
				currentVersion = string.Empty;
			}
			if (!previousVersion.Equals (currentVersion)) {
				int length = previousVersion.Length;
				int num2 = currentVersion.Length;
				this.redoStack.Clear ();
			}
		}

		internal string Modify (string currentVersion, bool revert, params Trio<Modification, int, string> [] operations) {
			for (int i = revert ? (operations.Length - 1) : 0; revert ? (i >= 0) : (i < operations.Length); i = revert ? (i - 1) : (i + 1)) {
				currentVersion = this.Modify (currentVersion, revert ? ((((Modification) operations [i].Value1) == Modification.Insert) ? Modification.Remove : Modification.Insert) : operations [i].Value1, operations [i].Value2, operations [i].Value3);
			}
			return currentVersion;
		}

		internal string Modify (string currentVersion, Modification modification, int position, string value) {
			StringBuilder builder = new StringBuilder (currentVersion);
			if (modification == Modification.Insert) {
				builder.Insert (position, value);
			} else {
				builder.Remove (position, value.Length);
			}
			return builder.ToString ();
		}

		internal string Redo (string currentVersion) {
			Trio<Modification, int, string> [] trioArray;
			if ((this.undoStack.Count != 0) && !SharedUtil.IsEmpty ((ICollection) (trioArray = this.undoStack.Pop ()))) {
				return this.Modify (currentVersion, false, trioArray);
			}
			return currentVersion;
		}

		internal string Undo (string currentVersion) {
			Trio<Modification, int, string> [] trioArray;
			if ((this.undoStack.Count != 0) && !SharedUtil.IsEmpty ((ICollection) (trioArray = this.undoStack.Pop ()))) {
				return this.Modify (currentVersion, true, trioArray);
			}
			return currentVersion;
		}

		// Properties
		internal bool CanRedo {
			get {
				return (this.redoStack.Count > 0);
			}
		}

		internal bool CanUndo {
			get {
				return (this.undoStack.Count > 0);
			}
		}

		// Nested Types
		internal enum Modification {
			Insert,
			Remove
		}
	}

	internal static class WebUtil {
		// Fields
		internal static readonly Hashtable entityMapping = new Hashtable ();

		// Methods
		static WebUtil () {
			entityMapping ["<"] = "&lt;";
			entityMapping [">"] = "&gt;";
			entityMapping ["\""] = "&quot;";
			entityMapping ["Œ"] = "&OElig;";
			entityMapping ["œ"] = "&oelig;";
			entityMapping ["Š"] = "&Scaron;";
			entityMapping ["š"] = "&scaron;";
			entityMapping ["Ÿ"] = "&Yuml;";
			entityMapping ["ˆ"] = "&circ;";
			entityMapping ["~"] = "&tilde;";
			entityMapping ["–"] = "&ndash;";
			entityMapping ["—"] = "&mdash;";
			entityMapping ["‘"] = "&lsquo;";
			entityMapping ["’"] = "&rsquo;";
			entityMapping ["‚"] = "&sbquo;";
			entityMapping ["“"] = "&ldquo;";
			entityMapping ["”"] = "&rdquo;";
			entityMapping ["„"] = "&bdquo;";
			entityMapping ["†"] = "&dagger;";
			entityMapping ["‡"] = "&Dagger;";
			entityMapping ["‰"] = "&permil;";
			entityMapping ["‹"] = "&lsaquo;";
			entityMapping ["›"] = "&rsaquo;";
			entityMapping ["ƒ"] = "&fnof;";
			entityMapping ["Α"] = "&Alpha;";
			entityMapping ["Β"] = "&Beta;";
			entityMapping ["Γ"] = "&Gamma;";
			entityMapping ["Δ"] = "&Delta;";
			entityMapping ["Ε"] = "&Epsilon;";
			entityMapping ["Ζ"] = "&Zeta;";
			entityMapping ["Η"] = "&Eta;";
			entityMapping ["Θ"] = "&Theta;";
			entityMapping ["Ι"] = "&Iota;";
			entityMapping ["Κ"] = "&Kappa;";
			entityMapping ["Λ"] = "&Lambda;";
			entityMapping ["Μ"] = "&Mu;";
			entityMapping ["Ν"] = "&Nu;";
			entityMapping ["Ξ"] = "&Xi;";
			entityMapping ["Ο"] = "&Omicron;";
			entityMapping ["Π"] = "&Pi;";
			entityMapping ["Ρ"] = "&Rho;";
			entityMapping ["Σ"] = "&Sigma;";
			entityMapping ["Τ"] = "&Tau;";
			entityMapping ["Υ"] = "&Upsilon;";
			entityMapping ["Φ"] = "&Phi;";
			entityMapping ["Χ"] = "&Chi;";
			entityMapping ["Ψ"] = "&Psi;";
			entityMapping ["Ω"] = "&Omega;";
			entityMapping ["α"] = "&alpha;";
			entityMapping ["β"] = "&beta;";
			entityMapping ["γ"] = "&gamma;";
			entityMapping ["δ"] = "&delta;";
			entityMapping ["ε"] = "&epsilon;";
			entityMapping ["ζ"] = "&zeta;";
			entityMapping ["η"] = "&eta;";
			entityMapping ["θ"] = "&theta;";
			entityMapping ["ι"] = "&iota;";
			entityMapping ["κ"] = "&kappa;";
			entityMapping ["λ"] = "&lambda;";
			entityMapping ["μ"] = "&mu;";
			entityMapping ["ν"] = "&nu;";
			entityMapping ["ξ"] = "&xi;";
			entityMapping ["ο"] = "&omicron;";
			entityMapping ["π"] = "&pi;";
			entityMapping ["ρ"] = "&rho;";
			entityMapping ["ς"] = "&sigmaf;";
			entityMapping ["σ"] = "&sigma;";
			entityMapping ["τ"] = "&tau;";
			entityMapping ["υ"] = "&upsilon;";
			entityMapping ["φ"] = "&phi;";
			entityMapping ["χ"] = "&chi;";
			entityMapping ["ψ"] = "&psi;";
			entityMapping ["ω"] = "&omega;";
			entityMapping ["ϑ"] = "&thetasym;";
			entityMapping ["ϒ"] = "&upsih;";
			entityMapping ["ϖ"] = "&piv;";
			entityMapping ["•"] = "&bull;";
			entityMapping ["…"] = "&hellip;";
			entityMapping ["′"] = "&prime;";
			entityMapping ["″"] = "&Prime;";
			entityMapping ["‾"] = "&oline;";
			entityMapping ["⁄"] = "&frasl;";
			entityMapping ["℘"] = "&weierp;";
			entityMapping ["ℑ"] = "&image;";
			entityMapping ["ℜ"] = "&real;";
			entityMapping ["™"] = "&trade;";
			entityMapping ["ℵ"] = "&alefsym;";
			entityMapping ["←"] = "&larr;";
			entityMapping ["↑"] = "&uarr;";
			entityMapping ["→"] = "&rarr;";
			entityMapping ["↓"] = "&darr;";
			entityMapping ["↔"] = "&harr;";
			entityMapping ["↵"] = "&crarr;";
			entityMapping ["⇐"] = "&lArr;";
			entityMapping ["⇑"] = "&uArr;";
			entityMapping ["⇒"] = "&rArr;";
			entityMapping ["⇓"] = "&dArr;";
			entityMapping ["⇔"] = "&hArr;";
			entityMapping ["∀"] = "&forall;";
			entityMapping ["∂"] = "&part;";
			entityMapping ["∃"] = "&exist;";
			entityMapping ["∅"] = "&empty;";
			entityMapping ["∇"] = "&nabla;";
			entityMapping ["∈"] = "&isin;";
			entityMapping ["∉"] = "&notin;";
			entityMapping ["∋"] = "&ni;";
			entityMapping ["∏"] = "&prod;";
			entityMapping ["−"] = "&sum;";
			entityMapping ["−"] = "&minus;";
			entityMapping ["∗"] = "&lowast;";
			entityMapping ["√"] = "&radic;";
			entityMapping ["∝"] = "&prop;";
			entityMapping ["∞"] = "&infin;";
			entityMapping ["∠"] = "&ang;";
			entityMapping ["⊥"] = "&and;";
			entityMapping ["⊦"] = "&or;";
			entityMapping ["∩"] = "&cap;";
			entityMapping ["∪"] = "&cup;";
			entityMapping ["∫"] = "&int;";
			entityMapping ["∴"] = "&there4;";
			entityMapping ["∼"] = "&sim;";
			entityMapping ["≅"] = "&cong;";
			entityMapping ["≅"] = "&asymp;";
			entityMapping ["≠"] = "&ne;";
			entityMapping ["≡"] = "&equiv;";
			entityMapping ["≤"] = "&le;";
			entityMapping ["≥"] = "&ge;";
			entityMapping ["⊂"] = "&sub;";
			entityMapping ["⊃"] = "&sup;";
			entityMapping ["⊄"] = "&nsub;";
			entityMapping ["⊆"] = "&sube;";
			entityMapping ["⊇"] = "&supe;";
			entityMapping ["⊕"] = "&oplus;";
			entityMapping ["⊗"] = "&otimes;";
			entityMapping ["⊥"] = "&perp;";
			entityMapping ["⋅"] = "&sdot;";
			entityMapping ["⌈"] = "&lceil;";
			entityMapping ["⌉"] = "&rceil;";
			entityMapping ["⌊"] = "&lfloor;";
			entityMapping ["⌋"] = "&rfloor;";
			entityMapping ["〈"] = "&lang;";
			entityMapping ["〉"] = "&rang;";
			entityMapping ["◊"] = "&loz;";
			entityMapping ["♠"] = "&spades;";
			entityMapping ["♣"] = "&clubs;";
			entityMapping ["♥"] = "&hearts;";
			entityMapping ["♦"] = "&diams;";
			entityMapping ["\x00a3"] = "&pound;";
			entityMapping ["\x00a1"] = "&iexcl;";
			entityMapping ["\x00a2"] = "&cent;";
			entityMapping ["€"] = "&euro;";
			entityMapping ["\x00a4"] = "&curren;";
			entityMapping ["\x00a5"] = "&yen;";
			entityMapping ["\x00a6"] = "&brvbar;";
			entityMapping ["\x00a7"] = "&sect;";
			entityMapping ["\x00a8"] = "&uml;";
			entityMapping ["\x00a9"] = "&copy;";
			entityMapping ["\x00aa"] = "&ordf;";
			entityMapping ["\x00ab"] = "&laquo;";
			entityMapping ["\x00ac"] = "&not;";
			entityMapping ["\x00ae"] = "&reg;";
			entityMapping ["\x00af"] = "&macr;";
			entityMapping ["\x00b0"] = "&deg;";
			entityMapping ["\x00b1"] = "&plusmn;";
			entityMapping ["\x00b2"] = "&sup2;";
			entityMapping ["\x00b3"] = "&sup3;";
			entityMapping ["\x00b4"] = "&acute;";
			entityMapping ["\x00b5"] = "&micro;";
			entityMapping ["\x00b6"] = "&para;";
			entityMapping ["\x00b7"] = "&middot;";
			entityMapping ["\x00b8"] = "&cedil;";
			entityMapping ["\x00b9"] = "&sup1;";
			entityMapping ["\x00ba"] = "&ordm;";
			entityMapping ["\x00bb"] = "&raquo;";
			entityMapping ["\x00bc"] = "&frac14;";
			entityMapping ["\x00bd"] = "&frac12;";
			entityMapping ["\x00be"] = "&frac34;";
			entityMapping ["\x00bf"] = "&iquest;";
			entityMapping ["\x00c0"] = "&Agrave;";
			entityMapping ["\x00c1"] = "&Aacute;";
			entityMapping ["\x00c2"] = "&Acirc;";
			entityMapping ["\x00c3"] = "&Atilde;";
			entityMapping ["\x00c4"] = "&Auml;";
			entityMapping ["\x00c5"] = "&Aring;";
			entityMapping ["\x00c6"] = "&AElig;";
			entityMapping ["\x00c7"] = "&Ccedil;";
			entityMapping ["\x00c8"] = "&Egrave;";
			entityMapping ["\x00c9"] = "&Eacute;";
			entityMapping ["\x00ca"] = "&Ecirc;";
			entityMapping ["\x00cb"] = "&Euml;";
			entityMapping ["\x00cc"] = "&Igrave;";
			entityMapping ["\x00cd"] = "&Iacute;";
			entityMapping ["\x00ce"] = "&Icirc;";
			entityMapping ["\x00cf"] = "&Iuml;";
			entityMapping ["\x00d0"] = "&ETH;";
			entityMapping ["\x00d1"] = "&Ntilde;";
			entityMapping ["\x00d2"] = "&Ograve;";
			entityMapping ["\x00d3"] = "&Oacute;";
			entityMapping ["\x00d4"] = "&Ocirc;";
			entityMapping ["\x00d5"] = "&Otilde;";
			entityMapping ["\x00d6"] = "&Ouml;";
			entityMapping ["\x00d7"] = "&times;";
			entityMapping ["\x00d8"] = "&Oslash;";
			entityMapping ["\x00d9"] = "&Ugrave;";
			entityMapping ["\x00da"] = "&Uacute;";
			entityMapping ["\x00db"] = "&Ucirc;";
			entityMapping ["\x00dc"] = "&Uuml;";
			entityMapping ["\x00dd"] = "&Yacute;";
			entityMapping ["\x00de"] = "&THORN;";
			entityMapping ["\x00df"] = "&szlig;";
			entityMapping ["\x00e0"] = "&agrave;";
			entityMapping ["\x00e1"] = "&aacute;";
			entityMapping ["\x00e2"] = "&acirc;";
			entityMapping ["\x00e3"] = "&atilde;";
			entityMapping ["\x00e4"] = "&auml;";
			entityMapping ["\x00e5"] = "&aring;";
			entityMapping ["\x00e6"] = "&aelig;";
			entityMapping ["\x00e7"] = "&ccedil;";
			entityMapping ["\x00e8"] = "&egrave;";
			entityMapping ["\x00e9"] = "&eacute;";
			entityMapping ["\x00ea"] = "&ecirc;";
			entityMapping ["\x00eb"] = "&euml;";
			entityMapping ["\x00ec"] = "&igrave;";
			entityMapping ["\x00ed"] = "&iacute;";
			entityMapping ["\x00ee"] = "&icirc;";
			entityMapping ["\x00ef"] = "&iuml;";
			entityMapping ["\x00f0"] = "&eth;";
			entityMapping ["\x00f1"] = "&ntilde;";
			entityMapping ["\x00f2"] = "&ograve;";
			entityMapping ["\x00f3"] = "&oacute;";
			entityMapping ["\x00f4"] = "&ocirc;";
			entityMapping ["\x00f5"] = "&otilde;";
			entityMapping ["\x00f6"] = "&ouml;";
			entityMapping ["\x00f7"] = "&divide;";
			entityMapping ["\x00f8"] = "&oslash;";
			entityMapping ["\x00f9"] = "&ugrave;";
			entityMapping ["\x00fa"] = "&uacute;";
			entityMapping ["\x00fb"] = "&ucirc;";
			entityMapping ["\x00fc"] = "&uuml;";
			entityMapping ["\x00fd"] = "&yacute;";
			entityMapping ["\x00fe"] = "&thorn;";
			entityMapping ["\x00ff"] = "&yuml;";
		}

		internal static string CompileMht (string title, params string [] files) {
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat (@"From: <Saved by Windows Internet Explorer 7>
Subject: {0}
Date: {1} +0200
MIME-Version: 1.0
Content-Type: multipart/related;
	type=""text/html"";
	boundary=""----=_NextPart_000_0000_01CA2B1E.5D5D76A0""
X-MimeOLE: Produced By Microsoft MimeOLE V6.00.3790.3959
X-roxority: not really, this is auto-generated but pretends to be IE-generated

This is a multi-part message in MIME format.

", title, DateTime.Now.ToUniversalTime ().ToString ("r").Substring (0, DateTime.Now.ToUniversalTime ().ToString ("r").LastIndexOf (' ')));
			return sb.ToString ();
		}

		internal static string [] GetQueryParameters (string queryString) {
			string [] strArray;
			ArrayList list = new ArrayList ();
			while (queryString.StartsWith ("?")) {
				queryString = queryString.Substring (1);
			}
			if (!SharedUtil.IsEmpty ((ICollection) (strArray = queryString.Split (new char [] { '&' })))) {
				foreach (string str in strArray) {
					string [] strArray2;
					if (!SharedUtil.IsEmpty ((ICollection) (strArray2 = str.Split (new char [] { '=' })))) {
						foreach (string str2 in strArray2) {
							list.Add (str2);
						}
					}
				}
			}
			return (list.ToArray (typeof (string)) as string []);
		}

		internal static string [] GetUserLanguages (params string [] supportedLanguages) {
			string empty = string.Empty;
			return GetUserLanguages (ref empty, supportedLanguages);
		}

		internal static string [] GetUserLanguages (ref string languagePreference, params string [] supportedLanguages) {
			ArrayList list = null;
			int index;
			string str4;
			bool flag = true;
			string str = languagePreference = SharedUtil.Trim (languagePreference).ToLower ();
			if (!SharedUtil.IsEmpty ((string) languagePreference) && ((index = languagePreference.IndexOf ('-')) > 0)) {
				str = languagePreference.Substring (0, index);
			}
			if (((HttpContext.Current == null) || (HttpContext.Current.Request == null)) || SharedUtil.IsEmpty ((ICollection) HttpContext.Current.Request.UserLanguages)) {
				if (!SharedUtil.IsEmpty ((ICollection) supportedLanguages)) {
					list = new ArrayList (supportedLanguages);
				} else {
					list = new ArrayList ();
				}
				goto Label_0204;
			}
			list = new ArrayList (HttpContext.Current.Request.UserLanguages.Clone () as ICollection);
		Label_017C:
			while (flag && (list.Count > 0)) {
				flag = false;
				for (int i = 0; i < list.Count; i++) {
					if (flag = (list [i] == null) || SharedUtil.IsEmpty (list [i] as string)) {
						list.Remove (i);
						goto Label_017C;
					}
					if (flag = (index = ((string) list [i]).IndexOf (';')) >= 0) {
						list [i] = ((string) list [i]).Substring (0, index);
						goto Label_017C;
					}
					if (flag = (index = ((string) list [i]).IndexOf ('-')) >= 0) {
						list [i] = ((string) list [i]).Substring (0, index);
						goto Label_017C;
					}
					list [i] = SharedUtil.Trim ((string) list [i]).ToLower ();
				}
			}
			if (SharedUtil.IsEmpty ((ICollection) supportedLanguages)) {
				goto Label_0204;
			}
			flag = true;
		Label_01E8:
			while (flag) {
				flag = false;
				foreach (string str2 in list) {
					if (Array.IndexOf<string> (supportedLanguages, str2) < 0) {
						flag = true;
						list.Remove (str2);
						goto Label_01E8;
					}
				}
			}
		Label_0204:
			if (SharedUtil.IsEmpty ((ICollection) list)) {
				list.AddRange (supportedLanguages);
			}
			if ((Array.IndexOf<string> (supportedLanguages, languagePreference) < 0) && (Array.IndexOf<string> (supportedLanguages, str) >= 0)) {
				languagePreference = str;
			}
			languagePreference = str4 = SharedUtil.Trim (languagePreference).ToLower ();
			if (!SharedUtil.IsEmpty (str4) && (Array.IndexOf<string> (supportedLanguages, languagePreference) >= 0)) {
				index = list.IndexOf (languagePreference);
				if (index >= 0) {
					list.RemoveAt (index);
				}
				list.Insert (0, languagePreference);
			} else if ((list.Count > 0) && ((languagePreference = list [0] as string) == null)) {
				languagePreference = string.Empty;
			}
			return (list.ToArray (typeof (string)) as string []);
		}

		internal static string HtmlEscape (string value) {
			StringBuilder builder = new StringBuilder (value);
			builder.Replace ("&", "&amp;");
			foreach (DictionaryEntry entry in entityMapping) {
				builder.Replace ((string) entry.Key, (string) entry.Value);
			}
			return builder.ToString ();
		}

		internal static string HtmlUnescape (string value) {
			StringBuilder builder = new StringBuilder (value);
			foreach (DictionaryEntry entry in entityMapping) {
				builder.Replace ((string) entry.Value, (string) entry.Key);
			}
			return builder.ToString ();
		}

		internal static string ModifyUrlParameter (string uri, string paramName, string paramValue) {
			return ModifyUrlParameter (new Uri (uri), paramName, paramValue).ToString ();
		}

		internal static Uri ModifyUrlParameter (Uri uri, string paramName, string paramValue) {
			int num;
			string query = uri.Query;
			if ((query.Length > 1) && ((num = query.IndexOf (paramName + "=")) >= 1)) {
				int index = query.IndexOf ('&', num + 1);
				string str2 = query.Substring (0, num);
				while (str2.EndsWith ("&")) {
					str2 = str2.Substring (0, str2.Length - 1);
				}
				if (index > num) {
					str2 = str2 + query.Substring (index);
				}
				query = str2 = str2 + string.Format ("&{0}={1}", paramName, paramValue);
			} else if (query.Length == 1) {
				query = query + string.Format ("{0}={1}", paramName, paramValue);
			} else if (query.Length == 0) {
				query = query + string.Format ("?{0}={1}", paramName, paramValue);
			} else {
				query = query + string.Format ("&{0}={1}", paramName, paramValue);
			}
			return new Uri (uri.GetLeftPart (UriPartial.Path) + query);
		}

		internal static string ModifyUrlParameters (string uri, params string [] paramNamesValues) {
			return ModifyUrlParameters (new Uri (uri), paramNamesValues).ToString ();
		}

		internal static Uri ModifyUrlParameters (Uri uri, params string [] paramNamesValues) {
			for (int i = 0; i < paramNamesValues.Length; i += 2) {
				uri = ModifyUrlParameter (uri, paramNamesValues [i], paramNamesValues [i + 1]);
			}
			return uri;
		}

		internal static string NamedToNumericEntities (string html) {
			StringBuilder builder = new StringBuilder (html);
			foreach (DictionaryEntry entry in entityMapping) {
				builder.Replace ((string) entry.Value, HttpUtility.HtmlEncode ((string) entry.Key));
			}
			return builder.Replace ("&nbsp;", "&#160;").ToString ();
		}

		// Nested Types
		internal sealed class RequestContext {
			// Fields
			internal readonly string ContextID;
			private object value;

			// Methods
			private RequestContext ()
				: this (null) {
			}

			private RequestContext (string contextID) {
				if (SharedUtil.IsEmpty (this.ContextID = contextID)) {
					this.ContextID = contextID = Guid.NewGuid ().ToString ();
				}
				try {
					if ((Items != null) && Items.Contains (this.ContextID)) {
						throw new ArgumentException (null, "contextID");
					}
				} catch {
				}
			}

			internal static WebUtil.RequestContext Add (object value) {
				return Add (null, value);
			}

			internal static WebUtil.RequestContext Add (string contextID, object value) {
				WebUtil.RequestContext context = new WebUtil.RequestContext (contextID);
				context.Value = value;
				return context;
			}

			internal void Remove () {
				try {
					if (Items != null) {
						Items.Remove (this.ContextID);
					}
				} catch {
				}
			}

			public override string ToString () {
				object obj2 = this.Value;
				if (obj2 != null) {
					return obj2.ToString ();
				}
				return null;
			}

			// Properties
			internal static IDictionary Items {
				get {
					if (HttpContext.Current != null) {
						return HttpContext.Current.Items;
					}
					return null;
				}
			}

			internal object Value {
				get {
					if (Items != null) {
						return Items [this.ContextID];
					}
					return this.value;
				}
				set {
					this.value = value;
					try {
						if (Items != null) {
							Items [this.ContextID] = value;
						}
					} catch {
					}
				}
			}
		}
	}

	[Serializable]
	internal class Wrap<T> : ISerializable where T : class {
		// Fields
		internal readonly T Value;

		// Methods
		internal Wrap (T value) {
			this.Value = value;
		}

		protected Wrap (SerializationInfo info, StreamingContext context) {
			this.Value = (T) info.GetValue ("Wrap_Value", typeof (T));
		}

		public override bool Equals (object obj) {
			if (!SharedUtil.Equals (this, obj)) {
				return SharedUtil.Equals (this.Value, obj);
			}
			return true;
		}

		public override int GetHashCode () {
			if (this.Value != null) {
				return this.Value.GetHashCode ();
			}
			return 0;
		}

		protected virtual void GetObjectData (SerializationInfo info, StreamingContext context) {
			info.AddValue ("Wrap_Value", this.Value);
		}

		public static implicit operator Wrap<T> (T value) {
			return new Wrap<T> (value);
		}

		public static implicit operator T (Wrap<T> value) {
			return value.Value;
		}

		void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context) {
			this.GetObjectData (info, context);
		}

		public override string ToString () {
			if (this.Value != null) {
				return this.Value.ToString ();
			}
			return null;
		}
	}

	#region Collections Namespace

	namespace Collections {

		internal abstract class DisposableDictionaryBase : DictionaryBase, IDisposable {
			// Methods
			protected DisposableDictionaryBase () {
			}

			public void Dispose () {
				foreach (DictionaryEntry entry in this) {
					IDisposable disposable = entry.Value as IDisposable;
					if (disposable != null) {
						disposable.Dispose ();
					}
				}
				this.Dispose (true);
			}

			protected virtual void Dispose (bool disposing) {
			}

		}

		internal class DisposableHashtable : DisposableDictionaryBase {
			// Methods
			internal virtual void Add (object key, object value) {
				base.Dictionary.Add (key, value);
			}

			internal bool Contains (object key) {
				return this.ContainsKey (key);
			}

			internal virtual bool ContainsKey (object key) {
				return base.Dictionary.Contains (key);
			}

			internal virtual bool ContainsValue (object value) {
				return base.InnerHashtable.ContainsValue (value);
			}

			internal virtual void Remove (object key) {
				base.Dictionary.Remove (key);
			}

			// Properties
			internal virtual object this [object key] {
				get {
					return base.Dictionary [key];
				}
				set {
					base.Dictionary [key] = value;
				}
			}

			internal virtual ICollection Keys {
				get {
					return base.Dictionary.Keys;
				}
			}

			internal virtual object SyncRoot {
				get {
					return base.Dictionary.SyncRoot;
				}
			}
		}

		internal sealed class HashTree : DictionaryBase {
			// Methods
			internal void Add (object value, params object [] keys) {
				object obj2;
				this.GetDeepHashtable (keys, 0, true, out obj2).InnerHashtable.Add (obj2, value);
			}

			internal bool Contains (params object [] keys) {
				object obj2;
				HashTree tree = this.GetDeepHashtable (keys, 0, false, out obj2);
				return (((tree != null) && (obj2 != null)) && tree.Dictionary.Contains (obj2));
			}

			internal ICollection GetAllValues () {
				ArrayList arrayList = new ArrayList (base.Count * 2);
				this.GetAllValues (arrayList);
				return arrayList;
			}

			private void GetAllValues (ArrayList arrayList) {
				foreach (object obj2 in base.InnerHashtable.Values) {
					HashTree tree = obj2 as HashTree;
					if (tree != null) {
						tree.GetAllValues (arrayList);
					} else {
						arrayList.Add (obj2);
					}
				}
			}

			private HashTree GetDeepHashtable (object [] keys, int index, bool create, out object key) {
				HashTree tree;
				if (SharedUtil.IsEmpty ((ICollection) keys)) {
					throw new ArgumentNullException ("keys");
				}
				key = keys [index];
				if (index == (keys.Length - 1)) {
					return this;
				}
				if (((tree = base.InnerHashtable [keys [index]] as HashTree) == null) && create) {
					base.InnerHashtable [keys [index]] = tree = new HashTree ();
				}
				if ((tree != null) && (tree != null)) {
					return tree.GetDeepHashtable (keys, index + 1, create, out key);
				}
				return null;
			}

			protected override void OnValidate (object key, object value) {
				if (value is HashTree) {
					throw new ArgumentException (null, "value");
				}
				base.OnValidate (key, value);
			}

			internal void Remove (params object [] keys) {
				object obj2;
				HashTree tree = this.GetDeepHashtable (keys, 0, false, out obj2);
				if ((tree != null) && (obj2 != null))
					tree.Dictionary.Remove (obj2);
			}

			// Properties
			internal object this [object [] keys] {
				get {
					object obj2;
					HashTree tree = this.GetDeepHashtable (keys, 0, false, out obj2);
					if ((tree != null) && (obj2 != null)) {
						return tree.InnerHashtable [obj2];
					}
					return null;
				}
				set {
					object obj2;
					this.GetDeepHashtable (keys, 0, true, out obj2).InnerHashtable [obj2] = value;
				}
			}
		}

		internal class LiveList<T> : List<T> {
			// Events
			internal event EventHandler<EventArgs<T>> AddedItem;

			internal event CancelEventHandler<T> AddingItem;

			internal event EventHandler<EventArgs<int>> ChangedItem;

			internal event CancelEventHandler<Duo<int, T>> ChangingItem;

			internal event EventHandler<EventArgs<T>> RemovedItem;

			internal event CancelEventHandler<T> RemovingItem;

			// Methods
			internal new void Add (T item) {
				CancelEventArgs<T> e = new CancelEventArgs<T> (item, false);
				this.OnAddingItem (e);
				if (!e.Cancel) {
					base.Add (item);
					this.OnAddedItem (new EventArgs<T> (item));
				}
			}

			protected virtual void OnAddedItem (EventArgs<T> e) {
				if (this.AddedItem != null) {
					this.AddedItem (this, e);
				}
			}

			protected virtual void OnAddingItem (CancelEventArgs<T> e) {
				if (this.AddingItem != null) {
					this.AddingItem (this, e);
				}
			}

			protected virtual void OnChangedItem (EventArgs<int> e) {
				if (this.ChangedItem != null) {
					this.ChangedItem (this, e);
				}
			}

			protected virtual void OnChangingItem (CancelEventArgs<Duo<int, T>> e) {
				if (this.ChangingItem != null) {
					this.ChangingItem (this, e);
				}
			}

			protected virtual void OnRemovedItem (EventArgs<T> e) {
				if (this.RemovedItem != null) {
					this.RemovedItem (this, e);
				}
			}

			protected virtual void OnRemovingItem (CancelEventArgs<T> e) {
				if (this.RemovingItem != null) {
					this.RemovingItem (this, e);
				}
			}

			internal new void Remove (T item) {
				CancelEventArgs<T> e = new CancelEventArgs<T> (item, false);
				this.OnRemovingItem (e);
				if (!e.Cancel) {
					base.Remove (item);
					this.OnRemovedItem (new EventArgs<T> (item));
				}
			}

			// Properties
			internal new T this [int index] {
				get {
					return base [index];
				}
				set {
					CancelEventArgs<Duo<int, T>> e = new CancelEventArgs<Duo<int, T>> (new Duo<int, T> (index, value), false);
					this.OnChangingItem (e);
					if (!e.Cancel) {
						base [index] = value;
						this.OnChangedItem (new EventArgs<int> (index));
					}
				}
			}
		}

	}

	#endregion

	#region ComponentModel Namespace

	namespace ComponentModel {

		using Drawing;
		using Win32;

		internal class BooleanTypeConverter : BooleanConverter {
			// Fields
			internal static string FalseString = "No";
			internal static string TrueString = "Yes";

			public BooleanTypeConverter () {
				new object ();
			}

			// Methods
			public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType) {
				if (sourceType != typeof (string)) {
					return base.CanConvertFrom (context, sourceType);
				}
				return true;
			}

			public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType) {
				if (destinationType != typeof (string)) {
					return base.CanConvertTo (context, destinationType);
				}
				return true;
			}

			public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value) {
				if (value == null) {
					return false;
				}
				if ((value is string) && TrueString.Equals ((string) value, StringComparison.InvariantCultureIgnoreCase)) {
					return true;
				}
				if ((value is string) && FalseString.Equals ((string) value, StringComparison.InvariantCultureIgnoreCase)) {
					return false;
				}
				return base.ConvertFrom (context, culture, value);
			}

			public override object ConvertTo (ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
				if (!(value is bool) || (destinationType != typeof (string))) {
					return base.ConvertTo (context, culture, value, destinationType);
				}
				if (!((bool) value)) {
					return FalseString;
				}
				return TrueString;
			}

			public override TypeConverter.StandardValuesCollection GetStandardValues (ITypeDescriptorContext context) {
				return new TypeConverter.StandardValuesCollection (new string [] { TrueString, FalseString });
			}

			public override bool GetStandardValuesExclusive (ITypeDescriptorContext context) {
				return true;
			}

			public override bool GetStandardValuesSupported (ITypeDescriptorContext context) {
				return true;
			}

			public override bool IsValid (ITypeDescriptorContext context, object value) {
				if (value != null) {
					bool flag = true;
					if (!flag.Equals (value)) {
						bool flag2 = false;
						if (!flag2.Equals (value)) {
							if (!(value is string)) {
								return false;
							}
							if (!TrueString.Equals ((string) value, StringComparison.InvariantCultureIgnoreCase)) {
								return FalseString.Equals ((string) value, StringComparison.InvariantCultureIgnoreCase);
							}
							return true;
						}
					}
				}
				return true;
			}
		}

		internal class BooleanTypeEditor : UITypeEditor {
			// Fields
			internal static Image FalseImage = null;
			internal static Image TrueImage = null;

			public BooleanTypeEditor () {
				new object ();
			}

			// Methods
			public override UITypeEditorEditStyle GetEditStyle (ITypeDescriptorContext context) {
				return UITypeEditorEditStyle.None;
			}

			public override bool GetPaintValueSupported (ITypeDescriptorContext context) {
				return true;
			}

			public override void PaintValue (PaintValueEventArgs e) {
				if (!(e.Value is bool))
					base.PaintValue (e);
				else if ((TrueImage != null) && (FalseImage != null))
					e.Graphics.DrawImage (((bool) e.Value) ? TrueImage : FalseImage, e.Bounds);
				else {
					Size size;
					Size size2 = size = CheckBoxRenderer.GetGlyphSize (e.Graphics, ((bool) e.Value) ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal);
					if (size2.Height <= e.Bounds.Height) {
						if ((bool) e.Value) {
							CheckBoxRenderer.DrawCheckBox (e.Graphics, new Point (e.Bounds.X + ((e.Bounds.Width - size.Width) / 2), e.Bounds.Y + ((e.Bounds.Height - size.Height) / 2)), Rectangle.Empty, string.Empty, null, false, CheckBoxState.CheckedNormal);
						} else {
							CheckBoxRenderer.DrawCheckBox (e.Graphics, new Point (e.Bounds.X + ((e.Bounds.Width - size.Width) / 2), e.Bounds.Y + ((e.Bounds.Height - size.Height) / 2)), Rectangle.Empty, string.Empty, null, false, CheckBoxState.UncheckedNormal);
						}
					}
				}
			}

			// Properties
			public override bool IsDropDownResizable {
				get {
					return false;
				}
			}
		}

		[ProvideProperty ("CommandKeyProvided", typeof (ToolStripItem)), ProvideProperty ("CommandKeyConsumed", typeof (ToolStripItem))]
		internal class CommandManager : Component, IExtenderProvider {
			// Fields
			protected readonly EventHandler consumerItem_Click;
			private readonly Dictionary<ToolStripItem, string> consumers = new Dictionary<ToolStripItem, string> ();
			internal static readonly Dictionary globalCommands = new Dictionary ();
			internal static readonly List<CommandManager> globalInstances = new List<CommandManager> ();
			private bool isGlobal;
			private readonly Dictionary localCommands = new Dictionary ();
			protected readonly EventHandler providerItem_CheckedChanged;
			protected readonly EventHandler providerItem_EnabledChanged;
			protected readonly EventHandler providerItem_TextChanged;

			// Methods
			internal CommandManager () {
				this.consumerItem_Click = new EventHandler (this.ConsumerItem_Click);
				this.providerItem_EnabledChanged = new EventHandler (this.ProviderItem_EnabledChanged);
				this.providerItem_CheckedChanged = new EventHandler (this.ProviderItem_CheckedChanged);
				this.providerItem_TextChanged = new EventHandler (this.ProviderItem_TextChanged);
				globalInstances.Add (this);
			}

			protected virtual void ConsumerItem_Click (object sender, EventArgs e) {
				string str;
				ToolStripSplitButton button;
				ToolStripItem item2;
				ToolStripItem commandItem = sender as ToolStripItem;
				if ((((button = commandItem as ToolStripSplitButton) == null) || !button.DropDownButtonPressed) && (((commandItem != null) && !SharedUtil.IsEmpty (str = this.GetCommandKeyConsumed (commandItem))) && ((item2 = this [str]) != null))) {
					item2.PerformClick ();
				}
			}

			protected override void Dispose (bool disposing) {
				globalInstances.Remove (this);
				base.Dispose (disposing);
			}

			[Localizable (false), DefaultValue (""), ExtenderProvidedProperty]
			internal string GetCommandKeyConsumed (ToolStripItem commandItem) {
				string str;
				if (!this.consumers.TryGetValue (commandItem, out str)) {
					return string.Empty;
				}
				return str;
			}

			[DefaultValue (""), Localizable (false), ExtenderProvidedProperty]
			internal string GetCommandKeyProvided (ToolStripItem commandItem) {
				return this [commandItem];
			}

			protected virtual void ProviderItem_CheckedChanged (object sender, EventArgs e) {
				string str;
				ToolStripItem providerToolStripItem = sender as ToolStripItem;
				if ((providerToolStripItem != null) && !SharedUtil.IsEmpty (str = this [providerToolStripItem])) {
					this.RefreshConsumers (providerToolStripItem, str);
				}
				if (e != null) {
					foreach (CommandManager manager in globalInstances) {
						if ((manager != this) && manager.isGlobal) {
							manager.ProviderItem_CheckedChanged (sender, null);
						}
					}
				}
			}

			protected virtual void ProviderItem_EnabledChanged (object sender, EventArgs e) {
				string str;
				ToolStripItem providerToolStripItem = sender as ToolStripItem;
				if ((providerToolStripItem != null) && !SharedUtil.IsEmpty (str = this [providerToolStripItem])) {
					this.RefreshConsumers (providerToolStripItem, str);
				}
				if (e != null) {
					foreach (CommandManager manager in globalInstances) {
						if ((manager != this) && manager.isGlobal) {
							manager.ProviderItem_EnabledChanged (sender, null);
						}
					}
				}
			}

			protected virtual void ProviderItem_TextChanged (object sender, EventArgs e) {
				string str;
				ToolStripItem providerToolStripItem = sender as ToolStripItem;
				if ((providerToolStripItem != null) && !SharedUtil.IsEmpty (str = this [providerToolStripItem])) {
					this.RefreshConsumers (providerToolStripItem, str);
				}
				if (e != null) {
					foreach (CommandManager manager in globalInstances) {
						if ((manager != this) && manager.isGlobal) {
							manager.ProviderItem_TextChanged (sender, null);
						}
					}
				}
			}

			internal void RefreshConsumers () {
				this.RefreshConsumers (null, null);
			}

			internal void RefreshConsumers (ToolStripItem providerToolStripItem, string providerCommandKey) {
				foreach (KeyValuePair<ToolStripItem, string> pair in this.consumers) {
					ToolStripItem item;
					ToolStripMenuItem item2;
					ToolStripMenuItem item3;
					if (((SharedUtil.IsEmpty (providerCommandKey) || (pair.Value != providerCommandKey)) || ((item = providerToolStripItem) == null)) && ((item = this [pair.Value]) == null)) {
						continue;
					}
					pair.Key.Enabled = item.Enabled;
					pair.Key.Image = item.Image;
					pair.Key.Text = item.Text;
					if (((item2 = pair.Key as ToolStripMenuItem) != null) && ((item3 = item as ToolStripMenuItem) != null)) {
						item2.Checked = item3.Checked;
						item2.ShortcutKeyDisplayString = item3.ShortcutKeyDisplayString;
						item2.ShortcutKeys = item3.ShortcutKeys;
						item2.ShowShortcutKeys = item3.ShowShortcutKeys;
					} else {
						ToolStripButton button;
						ToolStripButton button2;
						if (((item2 = pair.Key as ToolStripMenuItem) != null) && ((button2 = item as ToolStripButton) != null)) {
							item2.Checked = button2.Checked;
							continue;
						}
						if (((button = pair.Key as ToolStripButton) != null) && ((button2 = item as ToolStripButton) != null)) {
							button.Checked = button2.Checked;
							continue;
						}
						if (((button = pair.Key as ToolStripButton) != null) && ((item3 = item as ToolStripMenuItem) != null)) {
							button.Checked = item3.Checked;
						}
					}
				}
			}

			[ExtenderProvidedProperty, DefaultValue (""), Localizable (false)]
			internal void SetCommandKeyConsumed (ToolStripItem commandItem, string key) {
				if (SharedUtil.IsEmpty (key)) {
					this.consumers.Remove (commandItem);
					commandItem.Click -= this.consumerItem_Click;
				} else {
					this.consumers [commandItem] = key;
					commandItem.Click += this.consumerItem_Click;
				}
				this.RefreshConsumers ();
			}

			[DefaultValue (""), ExtenderProvidedProperty, Localizable (false)]
			internal void SetCommandKeyProvided (ToolStripItem commandItem, string key) {
				this [commandItem] = key;
				this.RefreshConsumers ();
			}

			bool IExtenderProvider.CanExtend (object extendee) {
				return (extendee is ToolStripItem);
			}

			// Properties
			[Browsable (false), DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
			internal Dictionary Commands {
				get {
					if (!this.isGlobal) {
						return this.localCommands;
					}
					return globalCommands;
				}
			}

			internal static Keys [] CommandShortcutKeys {
				get {
					List<Keys> list = new List<Keys> ();
					foreach (ToolStripItem item2 in globalCommands.Values) {
						ToolStripMenuItem item;
						if ((((item = item2 as ToolStripMenuItem) != null) && (item.ShortcutKeys != Keys.None)) && !list.Contains (item.ShortcutKeys)) {
							list.Add (item.ShortcutKeys);
						}
					}
					return list.ToArray ();
				}
			}

			internal static Shortcut [] CommandShortcuts {
				get {
					return SharedUtil.CreateArray<Shortcut, Keys> (CommandShortcutKeys, delegate (Keys value) {
						return (Shortcut) value;
					});
				}
			}

			[DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden), Browsable (false)]
			internal Dictionary GlobalCommands {
				get {
					return globalCommands;
				}
			}

			[DefaultValue (false), Localizable (false)]
			internal bool IsGlobal {
				get {
					return this.isGlobal;
				}
				set {
					if ((this.isGlobal != value) && (this.isGlobal = value)) {
						foreach (KeyValuePair<string, ToolStripItem> pair in this.localCommands) {
							globalCommands [pair.Key] = pair.Value;
						}
						this.localCommands.Clear ();
					}
					this.RefreshConsumers ();
				}
			}

			internal string this [ToolStripItem commandItem] {
				get {
					foreach (KeyValuePair<string, ToolStripItem> pair in this.Commands) {
						if (pair.Value == commandItem) {
							return pair.Key;
						}
					}
					return string.Empty;
				}
				set {
					string str = this [commandItem];
					if (SharedUtil.IsEmpty (value) && !SharedUtil.IsEmpty (str)) {
						if (commandItem is ToolStripMenuItem) {
							((ToolStripMenuItem) commandItem).CheckedChanged -= this.providerItem_CheckedChanged;
						} else if (commandItem is ToolStripButton) {
							((ToolStripButton) commandItem).CheckedChanged -= this.providerItem_CheckedChanged;
						}
						commandItem.EnabledChanged -= this.providerItem_EnabledChanged;
						commandItem.TextChanged -= this.providerItem_TextChanged;
						this.Commands.Remove (str);
					} else if (!SharedUtil.IsEmpty (value)) {
						if (!SharedUtil.IsEmpty (str)) {
							if (commandItem is ToolStripMenuItem) {
								((ToolStripMenuItem) commandItem).CheckedChanged -= this.providerItem_CheckedChanged;
							} else if (commandItem is ToolStripButton) {
								((ToolStripButton) commandItem).CheckedChanged -= this.providerItem_CheckedChanged;
							}
							commandItem.EnabledChanged -= this.providerItem_EnabledChanged;
							commandItem.TextChanged -= this.providerItem_TextChanged;
							this.Commands.Remove (str);
						} else {
							if (commandItem is ToolStripMenuItem) {
								((ToolStripMenuItem) commandItem).CheckedChanged += this.providerItem_CheckedChanged;
							} else if (commandItem is ToolStripButton) {
								((ToolStripButton) commandItem).CheckedChanged += this.providerItem_CheckedChanged;
							}
							commandItem.EnabledChanged += this.providerItem_EnabledChanged;
							commandItem.TextChanged += this.providerItem_TextChanged;
						}
						this.Commands [value] = commandItem;
					}
				}
			}

			internal ToolStripItem this [string key] {
				get {
					ToolStripItem item;
					if (!this.Commands.TryGetValue (key, out item)) {
						return null;
					}
					return item;
				}
			}

			[Browsable (false), DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden)]
			internal Dictionary LocalCommands {
				get {
					return this.localCommands;
				}
			}

			// Nested Types
			internal class Dictionary : Dictionary<string, ToolStripItem> {
			}
		}

		public class CustomPropertyDescriptor : PropertyDescriptor {

			public static bool AllowBoolEditor = true;

			public bool CatchSetValue;
			public Type DefaultComponentType;
			public Operation<object, object> DefaultGetValue;
			public bool DefaultIsReadOnly;
			public Type DefaultPropertyType;
			public object DefaultValue;
			public readonly PropertyDescriptor Descriptor;
			public readonly IPropertyHelper Helper;
			public bool? ReadOnly;
			private readonly ICustomTypeDescriptor typeDesc;

			public CustomPropertyDescriptor (PropertyDescriptor propertyDescriptor, IPropertyHelper helper, ICustomTypeDescriptor typeDesc)
				: base (propertyDescriptor) {
				List<Attribute> atts;
				this.CatchSetValue = true;
				this.ReadOnly = null;
				this.Helper = helper;
				this.typeDesc = typeDesc;
				this.Descriptor = propertyDescriptor;
				if (AllowBoolEditor && (PropertyType == typeof (bool))) {
					atts = new List<Attribute> (AttributeArray);
					atts.Add (new TypeConverterAttribute (typeof (BooleanTypeConverter)));
					AttributeArray = atts.ToArray ();
				}
			}

			public CustomPropertyDescriptor (string name, Attribute [] attributes, IPropertyHelper helper, ICustomTypeDescriptor typeDesc)
				: base (name, attributes) {
				this.CatchSetValue = true;
				this.ReadOnly = null;
				this.Helper = helper;
				this.typeDesc = typeDesc;
				this.Descriptor = null;
			}

			public override bool CanResetValue (object component) {
				return ((this.Descriptor != null) && this.Descriptor.CanResetValue (component));
			}

			public static string GetDescription (Enum value) {
				DescriptionAttribute attribute;
				string name = value.ToString ();
				object [] customAttributes = value.GetType ().GetField (name).GetCustomAttributes (typeof (DescriptionAttribute), true);
				if ((!SharedUtil.IsEmpty ((ICollection) customAttributes) && ((attribute = customAttributes [0] as DescriptionAttribute) != null)) && !SharedUtil.IsEmpty (attribute.Description)) {
					return attribute.Description;
				}
				return name;
			}

			public override object GetEditor (Type editorBaseType) {
				if (AllowBoolEditor && (this.PropertyType == typeof (bool))) {
					return new BooleanTypeEditor ();
				}
				return base.GetEditor (editorBaseType);
			}

			public override object GetValue (object component) {
				if (this.Descriptor != null) {
					return this.Descriptor.GetValue (component);
				}
				if (this.DefaultGetValue != null) {
					return this.DefaultGetValue (component);
				}
				return this.DefaultValue;
			}

			public override void ResetValue (object component) {
				if (this.Descriptor != null) {
					this.Descriptor.ResetValue (component);
				}
			}

			public override void SetValue (object component, object value) {
				string sval=value+string.Empty;
				bool isTrue = false;
				if (this.Descriptor != null) {
					if ((Descriptor.PropertyType == typeof (bool)) && (!string.IsNullOrEmpty (sval)) && (BooleanTypeConverter.FalseString.Equals (sval, StringComparison.InvariantCultureIgnoreCase) || (isTrue = BooleanTypeConverter.TrueString.Equals (sval, StringComparison.InvariantCultureIgnoreCase))))
						value = isTrue;
					if (!this.CatchSetValue) {
						this.Descriptor.SetValue (component, value);
					} else {
						try {
							this.Descriptor.SetValue (component, value);
						} catch {
						}
					}
				}
			}

			public override bool ShouldSerializeValue (object component) {
				if (this.Descriptor == null) {
					return false;
				}
				return this.Descriptor.ShouldSerializeValue (component);
			}

			public override string Category {
				get {
					if ((this.Helper != null) && (typeDesc != null)) {
						return this.Helper.GetCategory (this, this.typeDesc.GetPropertyOwner (this));
					}
					if (this.Descriptor != null) {
						return this.Descriptor.Category;
					}
					return base.Category;
				}
			}

			public override Type ComponentType {
				get {
					if (this.Descriptor != null) {
						return this.Descriptor.ComponentType;
					}
					return this.DefaultComponentType;
				}
			}

			public override string Description {
				get {
					if ((this.Helper != null) && (typeDesc != null)) {
						return this.Helper.GetDescription (this, this.typeDesc.GetPropertyOwner (this));
					}
					if (this.Descriptor != null) {
						return this.Descriptor.Description;
					}
					return base.Description;
				}
			}

			public override string DisplayName {
				get {
					if ((this.Helper != null) && (typeDesc != null)) {
						return this.Helper.GetDisplayName (this, this.typeDesc.GetPropertyOwner (this));
					}
					if (this.Descriptor != null) {
						return this.Descriptor.DisplayName;
					}
					return base.DisplayName;
				}
			}

			public override bool IsReadOnly {
				get {
					if (this.ReadOnly.HasValue && this.ReadOnly.HasValue) {
						return this.ReadOnly.Value;
					}
					if (this.Descriptor != null) {
						return this.Descriptor.IsReadOnly;
					}
					return this.DefaultIsReadOnly;
				}
			}

			public override Type PropertyType {
				get {
					if (this.Descriptor != null) {
						return this.Descriptor.PropertyType;
					}
					return this.DefaultPropertyType;
				}
			}

		}

		internal class CustomPropertyHelper : IPropertyHelper {
			// Fields
			internal readonly string Category;
			internal readonly string Description;
			internal readonly string DisplayName;

			// Methods
			internal CustomPropertyHelper (string category, string description, string displayName) {
				this.Category = SharedUtil.IsEmpty (category) ? string.Empty : category;
				this.Description = SharedUtil.IsEmpty (description) ? string.Empty : description;
				this.DisplayName = SharedUtil.IsEmpty (displayName) ? string.Empty : displayName;
			}

			string IPropertyHelper.GetCategory (CustomPropertyDescriptor property, object owner) {
				return this.Category;
			}

			string IPropertyHelper.GetDescription (CustomPropertyDescriptor property, object owner) {
				return this.Description;
			}

			string IPropertyHelper.GetDisplayName (CustomPropertyDescriptor property, object owner) {
				return this.DisplayName;
			}
		}

		public abstract class CustomTypeDescriptor : ICustomTypeDescriptor {
			// Methods
			protected CustomTypeDescriptor () {
			}

			public AttributeCollection GetAttributes () {
				return TypeDescriptor.GetAttributes (this, true);
			}

			public string GetClassName () {
				return TypeDescriptor.GetClassName (this, true);
			}

			public string GetComponentName () {
				return TypeDescriptor.GetComponentName (this, true);
			}

			public TypeConverter GetConverter () {
				return TypeDescriptor.GetConverter (this, true);
			}

			public EventDescriptor GetDefaultEvent () {
				return TypeDescriptor.GetDefaultEvent (this, true);
			}

			public PropertyDescriptor GetDefaultProperty () {
				return TypeDescriptor.GetDefaultProperty (this, true);
			}

			public object GetEditor (Type editorBaseType) {
				return TypeDescriptor.GetEditor (this, editorBaseType, true);
			}

			public EventDescriptorCollection GetEvents () {
				return TypeDescriptor.GetEvents (this, true);
			}

			public EventDescriptorCollection GetEvents (Attribute [] attributes) {
				return TypeDescriptor.GetEvents (this, attributes, true);
			}

			public virtual PropertyDescriptorCollection GetProperties () {
				return TypeDescriptor.GetProperties (this, true);
			}

			public virtual PropertyDescriptorCollection GetProperties (Attribute [] attributes) {
				return TypeDescriptor.GetProperties (this, attributes, true);
			}

			public virtual object GetPropertyOwner (PropertyDescriptor pd) {
				return this;
			}
		}

		internal class EnumTypeConverter : EnumConverter {
			// Fields
			private readonly Dictionary<string, string> translations;

			// Events
			internal static event EventHandler<EventArgs<Assembly, ResourceManager>> ResolveResourceManager;

			// Methods
			internal EnumTypeConverter (Type enumType)
				: base (enumType) {
				this.translations = new Dictionary<string, string> ();
				EventArgs<Assembly, ResourceManager> e = new EventArgs<Assembly, ResourceManager> (enumType.Assembly, null);
				if (ResolveResourceManager != null) {
					ResolveResourceManager (this, e);
				}
				foreach (string str in Enum.GetNames (base.EnumType)) {
					this.translations [str] = (e.Value2 == null) ? str : e.Value2.GetString (string.Format ("E_{0}_{1}", enumType.Name, str));
				}
			}

			public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType) {
				if (sourceType != typeof (string)) {
					return base.CanConvertFrom (context, sourceType);
				}
				return true;
			}

			public override bool CanConvertTo (ITypeDescriptorContext context, Type destinationType) {
				if (destinationType != typeof (string)) {
					return base.CanConvertTo (context, destinationType);
				}
				return true;
			}

			public override object ConvertFrom (ITypeDescriptorContext context, CultureInfo culture, object value) {
				if (value is string) {
					foreach (KeyValuePair<string, string> pair in this.translations) {
						if (pair.Value.Equals ((string) value, StringComparison.InvariantCultureIgnoreCase)) {
							return Enum.Parse (base.EnumType, pair.Key, true);
						}
					}
				}
				return base.ConvertFrom (context, culture, value);
			}

			public override object ConvertTo (ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
				if (destinationType == typeof (string)) {
					if (value is string) {
						foreach (KeyValuePair<string, string> pair in this.translations) {
							if (pair.Value.Equals ((string) value, StringComparison.InvariantCultureIgnoreCase)) {
								return pair.Key;
							}
						}
						return base.ConvertTo (context, culture, value, destinationType);
					}
					if (value != null) {
						return this.translations [value.ToString ()];
					}
				}
				return base.ConvertTo (context, culture, value, destinationType);
			}

			public override TypeConverter.StandardValuesCollection GetStandardValues (ITypeDescriptorContext context) {
				return new TypeConverter.StandardValuesCollection (this.translations.Values);
			}

			public override bool GetStandardValuesExclusive (ITypeDescriptorContext context) {
				return true;
			}

			public override bool GetStandardValuesSupported (ITypeDescriptorContext context) {
				return true;
			}

			public override bool IsValid (ITypeDescriptorContext context, object value) {
				return (((value is string) && (this.translations.ContainsKey ((string) value) || this.translations.ContainsValue ((string) value))) || base.IsValid (context, value));
			}
		}

		internal class FilePathTypeEditor<T> : UITypeEditor where T : FileDialog, new () {
			// Fields
			internal static Image NoFileImage = null;

			// Methods
			public override object EditValue (ITypeDescriptorContext context, IServiceProvider provider, object value) {
				using (T local = Activator.CreateInstance<T> ()) {
					local.FileName = value as string;
					local.Filter = ParameterAttribute.GetValue (context.PropertyDescriptor, "Filter", local.Filter);
					if (local.ShowDialog () == DialogResult.OK) {
						return local.FileName;
					}
				}
				return base.EditValue (context, provider, value);
			}

			public override UITypeEditorEditStyle GetEditStyle (ITypeDescriptorContext context) {
				return UITypeEditorEditStyle.Modal;
			}

			public override bool GetPaintValueSupported (ITypeDescriptorContext context) {
				return true;
			}

			public override void PaintValue (PaintValueEventArgs e) {
				bool flag;
				string str = e.Value as string;
				if ((flag = !SharedUtil.IsEmpty (str)) || (FilePathTypeEditor<T>.NoFileImage != null)) {
					e.Graphics.DrawImage (flag ? DrawingUtil.GetFileImage (str, false) : FilePathTypeEditor<T>.NoFileImage, new Rectangle (1, 2, e.Bounds.Width - 2, e.Bounds.Height - 2));
				}
				base.PaintValue (e);
			}
		}

		internal class FolderPathTypeEditor : UITypeEditor {
			// Fields
			internal static Image FolderImage = null;
			internal static Image NoFolderImage = null;

			// Methods
			public override object EditValue (ITypeDescriptorContext context, IServiceProvider provider, object value) {
				using (FolderBrowserDialog dialog = new FolderBrowserDialog ()) {
					dialog.Description = context.PropertyDescriptor.Description;
					dialog.RootFolder = Environment.SpecialFolder.Desktop;
					dialog.SelectedPath = value as string;
					dialog.ShowNewFolderButton = true;
					if (dialog.ShowDialog () == DialogResult.OK) {
						return dialog.SelectedPath;
					}
				}
				return base.EditValue (context, provider, value);
			}

			public override UITypeEditorEditStyle GetEditStyle (ITypeDescriptorContext context) {
				return UITypeEditorEditStyle.Modal;
			}

			public override bool GetPaintValueSupported (ITypeDescriptorContext context) {
				return true;
			}

			public override void PaintValue (PaintValueEventArgs e) {
				bool flag;
				string str = e.Value as string;
				if ((flag = ((FolderImage != null) && !SharedUtil.IsEmpty (str)) && Directory.Exists (str)) || (NoFolderImage != null)) {
					e.Graphics.DrawImage (flag ? FolderImage : NoFolderImage, new Rectangle (1, 2, e.Bounds.Width - 2, e.Bounds.Height - 2));
				}
				base.PaintValue (e);
			}
		}

		public interface IPropertyHelper {
			// Methods
			string GetCategory (CustomPropertyDescriptor property, object owner);
			string GetDescription (CustomPropertyDescriptor property, object owner);
			string GetDisplayName (CustomPropertyDescriptor property, object owner);
		}

		internal class ParameterAttribute : Attribute {
			// Fields
			private string name;
			internal const string PREFIX_RES = "res::";
			private string value;

			// Methods
			internal ParameterAttribute (string name, string value) {
				this.name = name;
				this.value = value;
			}

			internal static string GetValue (PropertyDescriptor propertyDescriptor, string name, string defaultValue) {
				ResourceManager manager;
				string str = defaultValue;
				if (propertyDescriptor.Attributes != null) {
					foreach (Attribute attribute2 in propertyDescriptor.Attributes) {
						ParameterAttribute attribute;
						if (((attribute = attribute2 as ParameterAttribute) != null) && (attribute.Name == name)) {
							str = attribute.Value;
							break;
						}
					}
				}
				if (((str != null) && str.StartsWith ("res::")) && ((manager = SharedUtil.GetResourceManager (propertyDescriptor.ComponentType.Assembly)) != null)) {
					str = manager.GetString (str.Substring ("res::".Length));
				}
				return str;
			}

			// Properties
			internal string Name {
				get {
					return this.name;
				}
				set {
					this.name = value;
				}
			}

			internal string Value {
				get {
					return this.value;
				}
				set {
					this.value = value;
				}
			}
		}

		[ProvideProperty ("SettingsMachineScope", typeof (IComponent)), ProvideProperty ("SettingsUserScope", typeof (IComponent)), ProvideProperty ("SettingsName", typeof (IComponent))]
		internal class SettingManager : Component, IExtenderProvider {
			// Fields
			private static readonly Dictionary<IComponent, string> componentNames = new Dictionary<IComponent, string> ();
			private readonly Dictionary<IComponent, string []> machineProviders = new Dictionary<IComponent, string []> ();
			private readonly Dictionary<IComponent, string []> userProviders = new Dictionary<IComponent, string []> ();

			// Methods
			internal static string GetName (IComponent c) {
				string str;
				Control control = c as Control;
				if (componentNames.TryGetValue (c, out str) && !SharedUtil.IsEmpty (str)) {
					return str;
				}
				if (((control = c as Control) != null) && !SharedUtil.IsEmpty (control.Name)) {
					return control.Name;
				}
				if (((c != null) && (c.Site != null)) && !SharedUtil.IsEmpty (c.Site.Name)) {
					return c.Site.Name;
				}
				if (c != null) {
					return c.GetType ().FullName.Replace ('.', '_').Replace ('+', '_');
				}
				return string.Empty;
			}

			[ExtenderProvidedProperty, Localizable (false), DefaultValue ((string) null)]
			internal string [] GetSettingsMachineScope (IComponent provider) {
				string [] strArray;
				if (!this.machineProviders.TryGetValue (provider, out strArray)) {
					return null;
				}
				return strArray;
			}

			[Localizable (false), DefaultValue ((string) null), ExtenderProvidedProperty]
			internal string GetSettingsName (IComponent provider) {
				string str;
				if (componentNames.TryGetValue (provider, out str) && !SharedUtil.IsEmpty (str)) {
					return str;
				}
				return null;
			}

			[DefaultValue ((string) null), Localizable (false), ExtenderProvidedProperty]
			internal string [] GetSettingsUserScope (IComponent provider) {
				string [] strArray;
				if (!this.userProviders.TryGetValue (provider, out strArray)) {
					return null;
				}
				return strArray;
			}

			internal void LoadSettings (string prefix) {
				foreach (Dictionary<IComponent, string []> dictionary in new Dictionary<IComponent, string []> [] { this.userProviders, this.machineProviders }) {
					foreach (KeyValuePair<IComponent, string []> pair in dictionary) {
						foreach (string str in pair.Value) {
							PropertyInfo info;
							object obj2;
							if (((info = pair.Key.GetType ().GetProperty (str)) != null) && ((obj2 = RegistryUtil.GetValue ((dictionary == this.userProviders) ? Application.UserAppDataRegistry : Application.CommonAppDataRegistry, prefix + "_" + GetName (pair.Key) + "_" + str, info.GetValue (pair.Key, null))) != null)) {
								try {
									info.SetValue (pair.Key, obj2, null);
								} catch {
								}
							}
						}
					}
				}
			}

			internal void SaveSettings (string prefix) {
				foreach (Dictionary<IComponent, string []> dictionary in new Dictionary<IComponent, string []> [] { this.userProviders, this.machineProviders }) {
					foreach (KeyValuePair<IComponent, string []> pair in dictionary) {
						foreach (string str in pair.Value) {
							try {
								PropertyInfo property = pair.Key.GetType ().GetProperty (str);
								if (property != null) {
									RegistryUtil.SetValue ((dictionary == this.userProviders) ? Application.UserAppDataRegistry : Application.CommonAppDataRegistry, prefix + "_" + GetName (pair.Key) + "_" + str, property.GetValue (pair.Key, null));
								}
							} catch {
							}
						}
					}
				}
			}

			[Localizable (false), DefaultValue ((string) null), ExtenderProvidedProperty]
			internal void SetSettingsMachineScope (IComponent provider, string [] settings) {
				if (SharedUtil.IsEmpty ((ICollection) settings)) {
					this.machineProviders.Remove (provider);
				} else {
					this.machineProviders [provider] = settings;
				}
			}

			[Localizable (false), ExtenderProvidedProperty, DefaultValue ((string) null)]
			internal void SetSettingsName (IComponent provider, string name) {
				if (SharedUtil.IsEmpty (name)) {
					componentNames.Remove (provider);
				} else {
					componentNames [provider] = name;
				}
			}

			[Localizable (false), DefaultValue ((string) null), ExtenderProvidedProperty]
			internal void SetSettingsUserScope (IComponent provider, string [] settings) {
				if (SharedUtil.IsEmpty ((ICollection) settings)) {
					this.userProviders.Remove (provider);
				} else {
					this.userProviders [provider] = settings;
				}
			}

			bool IExtenderProvider.CanExtend (object extendee) {
				return ((extendee != null) && (extendee != this));
			}
		}

		internal class SizeTypeConverter : SizeConverter {
			// Methods
			public override object ConvertTo (ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
				Size size;
				if (!(value is Size) || (destinationType != typeof (string))) {
					return base.ConvertTo (context, culture, value, destinationType);
				}
				Size size2 = size = (Size) value;
				return string.Format ("{0} \x00d7 {1}", size2.Width, size.Height);
			}

			public override bool GetPropertiesSupported (ITypeDescriptorContext context) {
				return false;
			}
		}

	}

	#endregion

	#region Design Namespace

	namespace Design {

		[AttributeUsage (AttributeTargets.Property, AllowMultiple = false)]
		internal sealed class DisplayNameAttribute : Attribute {
			// Fields
			private string displayName;
			private readonly string memberName;
			private const string RESOURCENAME_SUFFIX = "DisplayName";
			private readonly Type type;

			// Methods
			internal DisplayNameAttribute (string displayName) {
				this.displayName = SharedUtil.Trim (displayName);
				this.memberName = string.Empty;
				this.type = null;
			}

			internal DisplayNameAttribute (Type type, string memberName) {
				this.type = type;
				if (this.type == null) {
					throw new ArgumentNullException ("type");
				}
				SharedUtil.ThrowIfEmpty (this.memberName = memberName, "memberName");
			}

			internal static string GetDisplayName (PropertyDescriptor propDesc) {
				DisplayNameAttribute displayNameAttribute = GetDisplayNameAttribute (propDesc);
				if (displayNameAttribute != null) {
					return displayNameAttribute.DisplayName;
				}
				return propDesc.DisplayName;
			}

			internal static string GetDisplayName (MemberInfo memberInfo) {
				DisplayNameAttribute attribute = null;
				foreach (Attribute attribute2 in memberInfo.GetCustomAttributes (true)) {
					attribute = attribute2 as DisplayNameAttribute;
					if (attribute != null) {
						break;
					}
				}
				if (attribute != null) {
					return attribute.DisplayName;
				}
				return memberInfo.Name;
			}

			internal static DisplayNameAttribute GetDisplayNameAttribute (PropertyDescriptor propDesc) {
				DisplayNameAttribute attribute = null;
				foreach (Attribute attribute2 in propDesc.Attributes) {
					attribute = attribute2 as DisplayNameAttribute;
					if (attribute != null) {
						return attribute;
					}
				}
				return attribute;
			}

			private string [] GetResourceNames () {
				return new string [] { (this.memberName + '.' + "DisplayName"), string.Concat (new object [] { this.type.Name, '.', this.memberName, '.', "DisplayName" }), string.Concat (new object [] { this.type.FullName, '.', this.memberName, '.', "DisplayName" }), string.Concat (new object [] { this.type.FullName.Replace ('+', '.'), '.', this.memberName, '.', "DisplayName" }) };
			}

			// Properties
			internal string DisplayName {
				get {
					if ((this.displayName == null) && ((this.type != null) && !SharedUtil.IsEmpty (this.memberName))) {
						try {
							ResourceManager resourceManager = SharedUtil.GetResourceManager (this.type.Assembly);
							string [] resourceNames = this.GetResourceNames ();
							for (int i = 0; i < resourceNames.Length; i++) {
								if (!SharedUtil.IsEmpty (this.displayName = SharedUtil.Trim (SharedUtil.GetString (resourceManager, resourceNames [i], new object [0])))) {
									break;
								}
							}
							if (SharedUtil.IsEmpty (this.displayName)) {
								if ((this.type.BaseType == null) || (this.type.BaseType == typeof (object))) {
									this.displayName = this.memberName;
								} else {
									this.displayName = new DisplayNameAttribute (this.type.BaseType, this.memberName).DisplayName;
								}
							}
						} catch {
							this.displayName = this.memberName;
						}
					}
					return this.displayName;
				}
			}
		}

		[ProvideProperty ("FontStyle", typeof (Control))]
		internal class FontStyleProvider : Component, IExtenderProvider {
			// Fields
			private readonly Dictionary<Control, FontStyle> controls = new Dictionary<Control, FontStyle> ();

			// Methods
			internal static Font GetFont (Control control) {
				if (control.Font != null) {
					return control.Font;
				}
				if (control.Parent != null) {
					return GetFont (control.Parent);
				}
				return SystemFonts.DefaultFont;
			}

			[DefaultValue (0), ExtenderProvidedProperty, Localizable (false)]
			internal FontStyle GetFontStyle (Control control) {
				FontStyle style;
				if (!this.controls.TryGetValue (control, out style)) {
					return FontStyle.Regular;
				}
				return style;
			}

			internal void RefreshControlFonts () {
				foreach (KeyValuePair<Control, FontStyle> pair in this.controls) {
					if ((pair.Key.Font == null) || (pair.Key.Font.Style != ((FontStyle) pair.Value))) {
						pair.Key.Font = new Font (GetFont (pair.Key), pair.Value);
					}
				}
			}

			[ExtenderProvidedProperty, Localizable (false), DefaultValue (0)]
			internal void SetFontStyle (Control control, FontStyle fontStyle) {
				if (fontStyle == FontStyle.Regular) {
					this.controls.Remove (control);
				} else {
					this.controls [control] = fontStyle;
				}
			}

			bool IExtenderProvider.CanExtend (object extendee) {
				return (extendee is Control);
			}
		}

		internal interface IImageProvider {
			// Events
			event EventHandler ImageChanged;

			// Properties
			Image Image {
				get;
			}
		}

		[AttributeUsage (AttributeTargets.All)]
		internal sealed class LocalCategoryAttribute : CategoryAttribute {
			// Fields
			private string category;
			private bool loaded;
			private const string RESOURCENAME_SUFFIX = "Category";
			private readonly Type type;

			// Methods
			internal LocalCategoryAttribute (Type type, string category)
				: base (category) {
				this.category = string.Empty;
				this.category = SharedUtil.Trim (category);
				SharedUtil.ThrowIfNull (this.type = type, "type");
			}

			protected override string GetLocalizedString (string value) {
				string category = this.category;
				if (!this.loaded) {
					try {
						ResourceManager resourceManager = SharedUtil.GetResourceManager (this.type.Assembly);
						string [] resourceNames = this.GetResourceNames ();
						for (int i = 0; i < resourceNames.Length; i++) {
							if (!SharedUtil.IsEmpty (category = SharedUtil.Trim (SharedUtil.GetString (resourceManager, resourceNames [i], new object [0])))) {
								break;
							}
						}
						if ((SharedUtil.IsEmpty (category) && (this.type.BaseType != null)) && (this.type.BaseType != typeof (object))) {
							category = new LocalCategoryAttribute (this.type.BaseType, this.category).Category;
						}
					} catch {
					} finally {
						if (SharedUtil.IsEmpty (category)) {
							category = this.category;
						} else {
							this.category = category;
						}
						this.loaded = true;
					}
				}
				return category;
			}

			private string [] GetResourceNames () {
				return new string [] { (this.category + '.' + "Category"), ("Category" + '.' + this.category) };
			}
		}

		[AttributeUsage (AttributeTargets.All)]
		internal sealed class LocalDescriptionAttribute : DescriptionAttribute {
			// Fields
			private string description;
			private readonly string memberName;
			private const string RESOURCENAME_SUFFIX = "Description";
			private readonly Type type;

			// Methods
			internal LocalDescriptionAttribute (string description)
				: base (description) {
				this.description = string.Empty;
				this.description = SharedUtil.Trim (description);
				this.memberName = string.Empty;
				this.type = null;
			}

			internal LocalDescriptionAttribute (Type type, string memberName) {
				this.description = string.Empty;
				this.type = type;
				if (this.type == null) {
					throw new ArgumentNullException ("type");
				}
				this.memberName = SharedUtil.Trim (memberName);
			}

			internal static string GetLocalDescription (Enum member) {
				string description = new LocalDescriptionAttribute (member.GetType (), member.ToString ()).Description;
				if (!SharedUtil.IsEmpty (ref description)) {
					return description;
				}
				return member.ToString ();
			}

			private string [] GetResourceNames () {
				return new string [] { string.Concat (new object [] { this.type.FullName.Replace ('+', '.'), '.', this.memberName, '.', "Description" }), string.Concat (new object [] { this.type.Name, '.', this.memberName, '.', "Description" }), (this.memberName + '.' + "Description"), (this.type.FullName.Replace ('+', '.') + '.' + this.memberName), (this.type.Name + '.' + this.memberName), this.memberName };
			}

			// Properties
			public override string Description {
				get {
					if (SharedUtil.IsEmpty (this.description) && ((this.type != null) && !SharedUtil.IsEmpty (this.memberName))) {
						try {
							ResourceManager resourceManager = SharedUtil.GetResourceManager (this.type.Assembly);
							string [] resourceNames = this.GetResourceNames ();
							for (int i = 0; i < resourceNames.Length; i++) {
								if (!SharedUtil.IsEmpty (this.description = SharedUtil.Trim (SharedUtil.GetString (resourceManager, resourceNames [i], new object [0])))) {
									break;
								}
							}
							if (SharedUtil.IsEmpty (this.description)) {
								if ((this.type.BaseType == null) || (this.type.BaseType == typeof (object))) {
									this.description = this.memberName;
								} else {
									this.description = new LocalDescriptionAttribute (this.type.BaseType, this.memberName).Description;
								}
							}
						} catch {
							this.description = this.memberName;
						}
					}
					return this.description;
				}
			}
		}

	}

	#endregion

	#region Drawing Namespace

	namespace Drawing {

		internal static class DrawingUtil {

			private static readonly Dictionary<Duo<Color, Image>, Bitmap> disabledImages = new Dictionary<Duo<Color, Image>, Bitmap> ();
			private const int DTT_COMPOSITED = 0x2000;
			private const int DTT_GLOWSIZE = 0x800;
			private const int DTT_TEXTCOLOR = 1;
			private static ImageCodecInfo jpegCodec = null;
			private static readonly Hashtable knownColors;
			private static readonly Dictionary<int, LinearGradientBrush> lgBrushes = new Dictionary<int, LinearGradientBrush> ();
			private static readonly Dictionary<int, Pen> pens = new Dictionary<int, Pen> ();
			private static readonly Dictionary<int, SolidBrush> sBrushes = new Dictionary<int, SolidBrush> ();

			// Methods
			static DrawingUtil () {
				string [] names = Enum.GetNames (typeof (KnownColor));
				knownColors = new Hashtable (names.Length);
				foreach (string str in names) {
					knownColors [str.ToLower ()] = Color.Empty;
				}
			}

			[DllImport ("gdi32.dll")]
			private static extern bool BitBlt (IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);
			internal static void ClearAll () {
				foreach (KeyValuePair<int, LinearGradientBrush> pair in lgBrushes) {
					pair.Value.Dispose ();
				}
				lgBrushes.Clear ();
				foreach (KeyValuePair<int, SolidBrush> pair2 in sBrushes) {
					pair2.Value.Dispose ();
				}

				sBrushes.Clear ();
				foreach (KeyValuePair<int, Pen> pair3 in pens) {
					pair3.Value.Dispose ();
				}
				pens.Clear ();
				disabledImages.Clear ();
			}

			internal static int Compare (Size one, Size two) {
				if (one.IsEmpty && two.IsEmpty) {
					return 0;
				}
				int num = one.Width + one.Height;
				return num.CompareTo ((int) (two.Width + two.Height));
			}

			[DllImport ("gdi32.dll", SetLastError = true, ExactSpelling = true)]
			private static extern IntPtr CreateCompatibleDC (IntPtr hDC);
			[DllImport ("gdi32.dll")]
			private static extern IntPtr CreateDIBSection (IntPtr hdc, Win32BitmapInfo pbmi, uint iUsage, int ppvBits, IntPtr hSection, uint dwOffset);
			[DllImport ("gdi32.dll", SetLastError = true, ExactSpelling = true)]
			private static extern bool DeleteDC (IntPtr hdc);
			[DllImport ("gdi32.dll", SetLastError = true, ExactSpelling = true)]
			private static extern bool DeleteObject (IntPtr hObject);
			internal static void DrawGlowingText (Graphics graphics, string text, Font font, Rectangle bounds, Color color, TextFormatFlags flags) {
				IntPtr hdc = graphics.GetHdc ();
				IntPtr hDC = CreateCompatibleDC (hdc);
				Win32BitmapInfo structure = new Win32BitmapInfo ();
				structure.biSize = Marshal.SizeOf (structure);
				structure.biWidth = bounds.Width;
				structure.biHeight = -bounds.Height;
				structure.biPlanes = 1;
				structure.biBitCount = 0x20;
				structure.biCompression = 0;
				IntPtr hObject = CreateDIBSection (hdc, structure, 0, 0, IntPtr.Zero, 0);
				SelectObject (hDC, hObject);
				IntPtr ptr4 = font.ToHfont ();
				SelectObject (hDC, ptr4);
				VisualStyleRenderer renderer = new VisualStyleRenderer (VisualStyleElement.Window.Caption.Active);
				Win32DrawThemeOptions pOptions = new Win32DrawThemeOptions ();
				pOptions.dwSize = Marshal.SizeOf (typeof (Win32DrawThemeOptions));
				pOptions.dwFlags = 0x2801;
				pOptions.crText = ColorTranslator.ToWin32 (color);
				pOptions.iGlowSize = 5;
				Win32Rect pRect = new Win32Rect (0, 0, bounds.Right - bounds.Left, bounds.Bottom - bounds.Top);
				DrawThemeTextEx (renderer.Handle, hDC, 0, 0, text, -1, (int) flags, ref pRect, ref pOptions);
				BitBlt (hdc, bounds.Left, bounds.Top, bounds.Width, bounds.Height, hDC, 0, 0, 0xcc0020);
				DeleteObject (ptr4);
				DeleteObject (hObject);
				DeleteDC (hDC);
				graphics.ReleaseHdc (hdc);
			}

			[DllImport ("UxTheme.dll", CharSet = CharSet.Unicode)]
			private static extern int DrawThemeTextEx (IntPtr hTheme, IntPtr hdc, int iPartId, int iStateId, string text, int iCharCount, int dwFlags, ref Win32Rect pRect, ref Win32DrawThemeOptions pOptions);
			[DllImport ("dwmapi.dll")]
			private static extern void DwmExtendFrameIntoClientArea (IntPtr hWnd, ref Win32Margins pMargins);
			[DllImport ("dwmapi.dll")]
			private static extern void DwmIsCompositionEnabled (ref bool pfEnabled);
			internal static bool Equals (Bitmap one, Bitmap two) {
				if ((one != null) || (two != null)) {
					if ((one == null) || (two == null)) {
						return false;
					}
					if ((one.Width != two.Width) || (one.Height != two.Height)) {
						return false;
					}
					for (int i = 0; i < one.Width; i++) {
						for (int j = 0; j < two.Height; j++) {
							if (one.GetPixel (i, j).ToArgb () != two.GetPixel (i, j).ToArgb ()) {
								return false;
							}
						}
					}
				}
				return true;
			}

			internal static Color GetColor (string color) {
				if (!SharedUtil.IsEmpty (color)) {
					Color color2;
					while (color.StartsWith ("#")) {
						color = color.Substring (1);
					}
					if (!knownColors.ContainsKey (color = color.ToLower ())) {
						return Color.FromArgb (int.Parse ((color.Length == 6) ? ("FF" + color) : color, NumberStyles.HexNumber));
					}
					if ((color2 = (Color) knownColors [color]) == Color.Empty) {
						knownColors [color] = color2 = Color.FromName (color);
					}
					return color2;
				}
				return Color.Transparent;
			}

			internal static Image GetDisabledImage (Color backColor, Image image) {
				Duo<Color, Image> key = new Duo<Color, Image> (backColor, image);
				Bitmap bitmap = null;
				if ((image != null) && !disabledImages.TryGetValue (key, out bitmap)) {
					disabledImages [key] = bitmap = new Bitmap (image.Width, image.Height, PixelFormat.Format32bppArgb);
					using (Graphics graphics = Graphics.FromImage (bitmap)) {
						graphics.Clear (backColor);
						ControlPaint.DrawImageDisabled (graphics, image, 0, 0, backColor);
					}
				}
				return bitmap;
			}

			internal static Icon GetFileIcon (string filePath, bool large) {
				try {
					SHGFI shgfi;
					Win32FileInfo structure = new Win32FileInfo (true);
					int num = Marshal.SizeOf (structure);
					if (large) {
						shgfi = SHGFI.Icon | SHGFI.UseFileAttributes;
					} else {
						shgfi = SHGFI.Icon | SHGFI.UseFileAttributes | SHGFI.SmallIcon;
					}
					Win32FileInfo.SHGetFileInfo (filePath, 0x100, out structure, (uint) num, shgfi);
					return Icon.FromHandle (structure.hIcon);
				} catch {
					return null;
				}
			}

			internal static Image GetFileImage (string filePath, bool large) {
				return GetFileImage (filePath, large, Color.Transparent);
			}

			internal static Image GetFileImage (string filePath, bool large, Color backColor) {
				Icon fileIcon = GetFileIcon (filePath, large);
				Bitmap image = null;
				if (fileIcon != null) {
					image = new Bitmap (fileIcon.Width, fileIcon.Height, PixelFormat.Format32bppArgb);
					using (Graphics graphics = Graphics.FromImage (image)) {
						graphics.Clear (backColor);
						graphics.DrawIcon (fileIcon, new Rectangle (0, 0, fileIcon.Width, fileIcon.Height));
					}
				}
				return image;
			}

			internal static LinearGradientBrush GetLinearGradientBrush (Rectangle rect, Color one, Color two, LinearGradientMode mode) {
				LinearGradientBrush brush;
				int hashCode = SharedUtil.GetHashCode (new object [] { rect, one.ToArgb (), two.ToArgb (), mode });
				if (!lgBrushes.TryGetValue (hashCode, out brush)) {
					lgBrushes [hashCode] = brush = new LinearGradientBrush (rect, one, two, mode);
				}
				return brush;
			}

			internal static Pen GetPen (Brush brush, float width) {
				Pen pen;
				int hashCode = SharedUtil.GetHashCode (new object [] { brush, width });
				if (!pens.TryGetValue (hashCode, out pen)) {
					pens [hashCode] = pen = new Pen (brush, width);
				}
				return pen;
			}

			internal static Pen GetPen (Color color, float width) {
				Pen pen;
				int hashCode = SharedUtil.GetHashCode (new object [] { color.ToArgb (), width });
				if (!pens.TryGetValue (hashCode, out pen)) {
					pens [hashCode] = pen = new Pen (color, width);
				}
				return pen;
			}

			internal static Point [] GetPoints (params int [] xy) {
				List<Point> list = new List<Point> (xy.Length / 2);
				for (int i = 1; i < xy.Length; i += 2) {
					list.Add (new Point (xy [i - 1], xy [i]));
				}
				return list.ToArray ();
			}

			internal static RectangleF GetRectangle (Rectangle rect) {
				return new RectangleF ((float) rect.X, (float) rect.Y, (float) rect.Width, (float) rect.Height);
			}

			internal static Rectangle GetRectangle (RectangleF rect) {
				return new Rectangle ((int) rect.X, (int) rect.Y, (int) rect.Width, (int) rect.Height);
			}

			internal static SizeF GetSize (Size size) {
				return new SizeF ((float) size.Width, (float) size.Height);
			}

			internal static Size GetSize (SizeF size) {
				return new Size ((int) size.Width, (int) size.Height);
			}

			internal static SolidBrush GetSolidBrush (Color color) {
				SolidBrush brush;
				int key = color.ToArgb ();
				if (!sBrushes.TryGetValue (key, out brush)) {
					sBrushes [key] = brush = new SolidBrush (color);
				}
				return brush;
			}

			internal static void SaveImage (Image image, Stream stream, ImageFormat imageFormat, int jpegQuality) {
				SharedUtil.ThrowIfEmpty (image, "image");
				SharedUtil.ThrowIfEmpty (stream, "stream");
				SharedUtil.ThrowIfEmpty (imageFormat, "imageFormat");
				if (imageFormat == ImageFormat.Jpeg) {
					using (EncoderParameters parameters = new EncoderParameters (1)) {
						using (EncoderParameter parameter = new EncoderParameter (System.Drawing.Imaging.Encoder.Quality, (long) jpegQuality)) {
							parameters.Param [0] = parameter;
							image.Save (stream, JpegCodec, parameters);
							stream.Flush ();
						}
						return;
					}
				}
				using (MemoryStream stream2 = new MemoryStream ()) {
					image.Save (stream2, imageFormat);
					stream2.Seek (0L, SeekOrigin.Begin);
					stream2.WriteTo (stream);
					stream.Flush ();
					stream2.Flush ();
				}
			}

			internal static SizeF ScaleToHeight (SizeF size, float maxHeight) {
				float num = size.Height / maxHeight;
				if (num > 1f) {
					size = new SizeF (size.Width / num, maxHeight);
				}
				return size;
			}

			internal static SizeF ScaleToWidth (SizeF size, float maxWidth) {
				float num = size.Width / maxWidth;
				if (num > 1f) {
					size = new SizeF (maxWidth, size.Height / num);
				}
				return size;
			}

			[DllImport ("gdi32.dll", ExactSpelling = true)]
			private static extern IntPtr SelectObject (IntPtr hDC, IntPtr hObject);

			internal static ImageCodecInfo JpegCodec {
				get {
					ImageCodecInfo [] infoArray;
					if ((jpegCodec == null) && !SharedUtil.IsEmpty ((ICollection) (infoArray = ImageCodecInfo.GetImageEncoders ()))) {
						foreach (ImageCodecInfo info in infoArray) {
							if (info.MimeType == "image/jpeg") {
								jpegCodec = info;
								break;
							}
						}
					}
					return jpegCodec;
				}
			}

		}

		[Flags]
		internal enum SHGFI {
			AddOverlays = 0x20,
			Attr_Specified = 0x20000,
			Attributes = 0x800,
			DisplayName = 0x200,
			ExeType = 0x2000,
			Icon = 0x100,
			IconLocation = 0x1000,
			LargeIcon = 0,
			LinkOverlay = 0x8000,
			OpenIcon = 2,
			OverlayIndex = 0x40,
			PIDL = 8,
			Selected = 0x10000,
			ShellIconize = 4,
			SmallIcon = 1,
			SysIconIndex = 0x4000,
			TypeName = 0x400,
			UseFileAttributes = 0x10
		}

		[StructLayout (LayoutKind.Sequential)]
		internal class Win32BitmapInfo {
			internal int biSize;
			internal int biWidth;
			internal int biHeight;
			internal short biPlanes;
			internal short biBitCount;
			internal int biCompression;
			internal int biSizeImage;
			internal int biXPelsPerMeter;
			internal int biYPelsPerMeter;
			internal int biClrUsed;
			internal int biClrImportant;
			internal byte bmiColors_rgbBlue;
			internal byte bmiColors_rgbGreen;
			internal byte bmiColors_rgbRed;
			internal byte bmiColors_rgbReserved;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct Win32DrawThemeOptions {
			internal int dwSize;
			internal int dwFlags;
			internal int crText;
			internal int crBorder;
			internal int crShadow;
			internal int iTextShadowType;
			internal Win32Point ptShadowOffset;
			internal int iBorderSize;
			internal int iFontPropId;
			internal int iColorPropId;
			internal int iStateId;
			internal bool fApplyOverlay;
			internal int iGlowSize;
			internal int pfnDrawTextCallback;
			internal IntPtr lParam;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct Win32FileInfo {
			internal const int MAX_PATH = 260;
			internal const int MAX_TYPE = 80;
			internal IntPtr hIcon;
			internal int iIcon;
			internal uint dwAttributes;
			[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 260)]
			internal string szDisplayName;
			[MarshalAs (UnmanagedType.ByValTStr, SizeConst = 80)]
			internal string szTypeName;
			[DllImport ("shell32.dll", CharSet = CharSet.Auto)]
			internal static extern int SHGetFileInfo (string pszPath, int dwFileAttributes, out Win32FileInfo psfi, uint cbfileInfo, SHGFI uFlags);
			internal Win32FileInfo (bool b) {
				this.hIcon = IntPtr.Zero;
				this.iIcon = 0;
				this.dwAttributes = 0;
				this.szDisplayName = "";
				this.szTypeName = "";
			}
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct Win32Margins {
			internal int Left;
			internal int Right;
			internal int Top;
			internal int Bottom;
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct Win32Point {
			internal int x;
			internal int y;
			internal Win32Point (int x, int y) {
				this.x = x;
				this.y = y;
			}
		}

		[StructLayout (LayoutKind.Sequential)]
		internal struct Win32Rect {
			internal int Left;
			internal int Top;
			internal int Right;
			internal int Bottom;
			internal Win32Rect (int left, int top, int right, int bottom) {
				this.Left = left;
				this.Top = top;
				this.Right = right;
				this.Bottom = bottom;
			}

			internal Win32Rect (Rectangle rectangle) {
				this.Left = rectangle.X;
				this.Top = rectangle.Y;
				this.Right = rectangle.Right;
				this.Bottom = rectangle.Bottom;
			}

			internal Rectangle ToRectangle () {
				return new Rectangle (this.Left, this.Top, this.Right - this.Left, this.Bottom - this.Top);
			}
		}

	}

	#endregion

	#region IO Namespace

	namespace IO {

		using Drawing;

		internal enum FileSize : long {
			B = 0L,
			GB = 0x40000000L,
			KB = 0x400L,
			MB = 0x100000L,
			TB = 0x10000000000L
		}

		internal static class IOUtil {
			// Fields
			internal const int DEFAULT_BUFFERSIZE = 0x1000;

			// Methods
			internal static void CopyFiles (string containingDirectoryPath, string targetDirectoryPath, string filePattern, string subFilePattern, string directoryPattern, string subDirectoryPattern) {
				DirectoryInfo info = new DirectoryInfo (containingDirectoryPath);
				Action<FileInfo> action = delegate (FileInfo item) {
					try {
						item.CopyTo (Path.Combine (targetDirectoryPath, Path.GetFileName (item.FullName)), true);
					} catch {
					}
				};
				if (!Directory.Exists (targetDirectoryPath))
					CreateDirectory (new DirectoryInfo (targetDirectoryPath));
				foreach (FileInfo info2 in info.GetFiles (filePattern, SearchOption.TopDirectoryOnly))
					action (info2);
				if (!string.IsNullOrEmpty (directoryPattern))
					foreach (DirectoryInfo info3 in info.GetDirectories (directoryPattern, SearchOption.TopDirectoryOnly))
						CopyFiles (info3.FullName, Path.Combine (targetDirectoryPath, info3.Name), subFilePattern, subFilePattern, subDirectoryPattern, subDirectoryPattern);
			}

			internal static bool CopyStream (Stream source, Stream target) {
				return CopyStream (source, target, null);
			}

			internal static bool CopyStream (Stream source, Stream target, Operation<int, bool> shouldCancel) {
				SharedUtil.ThrowIfEmpty (source, "source");
				return CopyStream (source, target, 0, 0L, SeekOrigin.Current, source.Length, 0L, SeekOrigin.Begin, shouldCancel);
			}

			internal static bool CopyStream (Stream source, Stream target, int bufferSize, long sourceSeekOffset, SeekOrigin sourceSeekOrigin, long targetSetLength, long targetSeekOffset, SeekOrigin targetSeekOrigin, Operation<int, bool> shouldCancel) {
				SharedUtil.ThrowIfEmpty (source, "source");
				SharedUtil.ThrowIfEmpty (target, "target");
				if (!source.CanRead || !target.CanWrite) {
					throw new IOException ();
				}
				byte [] buffer = new byte [bufferSize = (bufferSize <= 0) ? 0x1000 : bufferSize];
				if (((sourceSeekOffset > 0L) || ((sourceSeekOffset == 0L) && (sourceSeekOrigin != SeekOrigin.Current))) && source.CanSeek) {
					source.Seek (sourceSeekOffset, sourceSeekOrigin);
				}
				if (target.CanSeek) {
					if (targetSetLength > 0L) {
						target.SetLength (targetSetLength);
					}
					target.Seek (targetSeekOffset, targetSeekOrigin);
				}
				while (source.Position < source.Length) {
					long num;
					if ((num = source.Length - source.Position) < bufferSize) {
						bufferSize = (int) num;
						buffer = new byte [bufferSize];
					}
					source.Read (buffer, 0, bufferSize);
					target.Write (buffer, 0, bufferSize);
					target.Flush ();
					if ((shouldCancel != null) && shouldCancel (SharedUtil.Percent (source.Position, source.Length))) {
						return false;
					}
				}
				return true;
			}

			internal static DirectoryInfo CreateDirectory (DirectoryInfo dir) {
				if (!dir.Exists) {
					if (dir.Parent != null) {
						CreateDirectory (dir.Parent);
					}
					Directory.CreateDirectory (dir.FullName);
				}
				return dir;
			}

			internal static string CreateDirectory (string path) {
				return CreateDirectory (new DirectoryInfo (path)).FullName;
			}

			internal static void DeleteFiles (string dirPath, SearchOption searchOption, int loops, params string [] filters) {
				string [] filePaths = null;
				if ((!string.IsNullOrEmpty (dirPath)) && Directory.Exists (dirPath))
					for (int i = 0; i < loops; i++)
						foreach (string f in filters) {
							if (!string.IsNullOrEmpty (f))
								try {
									filePaths = Directory.GetFiles (dirPath, f, searchOption);
								} catch {
									filePaths = null;
								}
							if (filePaths != null)
								foreach (string fp in filePaths)
									TryDeleteFile (fp, ((i == 0) ? 0 : 1));
						}
			}

			internal static string FormatFileSize (Duo<double, FileSize> fileSize) {
				return string.Format ("{0} {1}", fileSize.Value1.Equals ((double) ((long) fileSize.Value1)) ? ((long) fileSize.Value1).ToString () : fileSize.Value1.ToString ("N2"), fileSize.Value2);
			}

			internal static string FormatFileSize (long fileLength) {
				return FormatFileSize (GetFileSize (fileLength));
			}

			internal static IEnumerable<string> GetAllFiles (string dirPath, Predicate<string> includeFile, Predicate<string> includeDirectory) {
				IEnumerable<string> subFilePaths;
				foreach (string filePath in Directory.GetFiles (dirPath))
					if ((includeFile == null) || includeFile (filePath))
						yield return filePath;
				foreach (string subDirPath in Directory.GetDirectories (dirPath))
					if (((includeDirectory == null) || includeDirectory (subDirPath)) && ((subFilePaths = GetAllFiles (subDirPath, includeFile, includeDirectory)) != null))
						foreach (string subFilePath in subFilePaths)
							yield return subFilePath;
			}

			internal static ulong GetDirectorySize (string dirPath, ulong testAddSize, ulong testMaxSize) {
				ulong size = 0;
				string [] paths = new string [0];
				try {
					paths = Directory.GetFiles (dirPath);
				} catch {
				}
				foreach (string filePath in paths) {
					try {
						using (Stream fs = File.OpenRead (filePath))
							size += (ulong) fs.Length;
					} catch {
					}
					if ((size + testAddSize) >= testMaxSize)
						return size;
				}
				try {
					paths = new string [0];
					paths = Directory.GetDirectories (dirPath);
				} catch {
				}
				foreach (string subDirPath in paths)
					if ((size = size + GetDirectorySize (subDirPath, testAddSize + size, testMaxSize)) >= testMaxSize)
						return size;
				return size;
			}

			internal static Duo<double, FileSize> GetFileSize (long fileLength) {
				long num = 0x400L;
				long num2 = 0x100000L;
				long num3 = 0x40000000L;
				long num4 = 0x10000000000L;
				if (fileLength < num) {
					return new Duo<double, FileSize> ((double) fileLength, FileSize.B);
				}
				if (fileLength < num2) {
					return new Duo<double, FileSize> (((double) fileLength) / ((double) num), FileSize.KB);
				}
				if (fileLength < num3) {
					return new Duo<double, FileSize> (((double) fileLength) / ((double) num2), FileSize.MB);
				}
				if (fileLength < num4) {
					return new Duo<double, FileSize> (((double) fileLength) / ((double) num3), FileSize.GB);
				}
				return new Duo<double, FileSize> (((double) fileLength) / ((double) num4), FileSize.TB);
			}

			internal static string NormalizePath (string path) {
				path = (SharedUtil.IsEmpty (path) || (path == ".")) ? Environment.CurrentDirectory.Trim ().ToLower () : (path = path.Trim ().ToLower ());
				if (path.StartsWith ("file:")) {
					path = PathFromUri (path);
				}
				while ((path.Length > 0) && path.EndsWith (Path.DirectorySeparatorChar.ToString ())) {
					path = path.Substring (0, path.Length - 1);
				}
				return path;
			}

			internal static bool PathEquals (string one, string two) {
				if ((one != null) || (two != null)) {
					if ((one == null) || (two == null)) {
						return false;
					}
					if (!object.ReferenceEquals (one, two) && !one.Equals (two)) {
						return one.Trim ().Equals (two.Trim (), StringComparison.InvariantCultureIgnoreCase);
					}
				}
				return true;
			}

			internal static string PathFromUri (string uri) {
				return uri.Replace ("file:///", string.Empty).Replace ("file://", string.Empty).Replace ('/', '\\');
			}

			internal static void TryDeleteFile (string path) {
				TryDeleteFile (path, 0);
			}

			internal static void TryDeleteFile (string path, int sleep) {
				try {
					File.Delete (path);
				} catch {
				} finally {
					if (sleep > 0)
						Thread.Sleep (sleep);
				}
			}

		}

		internal class ShellInfo : IDisposable {
			// Fields
			private string contentType;
			internal readonly string DirectoryName;
			internal readonly string DotLessExtension;
			internal readonly string Extension;
			internal readonly string FileName;
			internal readonly string FilePath;
			private Icon icon;
			private Image image;
			internal readonly string Name;
			private Verb primaryVerb;
			private List<Verb> secondaryVerbs;
			private Icon smallIcon;
			private Image smallImage;

			// Methods
			internal ShellInfo (string filePath) {
				if (SharedUtil.IsEmpty (this.FilePath = SharedUtil.Trim (filePath))) {
					throw new ArgumentNullException ("filePath");
				}
				this.DirectoryName = Path.GetDirectoryName (this.FilePath);
				this.Extension = Path.GetExtension (this.FilePath).ToLower ();
				this.DotLessExtension = this.Extension.Replace (".", string.Empty);
				this.FileName = Path.GetFileName (this.FilePath);
				this.Name = Path.GetFileNameWithoutExtension (this.FilePath);
			}

			private static string AppDesc (ref string value) {
				try {
					RegistryKey key = Registry.CurrentUser.OpenSubKey (@"Software\Microsoft\Windows\ShellNoRoam\MUICache", false);
					if (key != null) {
						using (key) {
							string [] strArray;
							if (!SharedUtil.IsEmpty ((ICollection) (strArray = key.GetValueNames ()))) {
								foreach (string str in strArray) {
									if (value.ToLower ().IndexOf (str.ToLower ()) >= 0) {
										return SharedUtil.Trim (key.GetValue (str, value, RegistryValueOptions.None) as string);
									}
								}
								foreach (string str2 in strArray) {
									if (str2.ToLower ().IndexOf (value.ToLower ()) >= 0) {
										value = str2;
										return SharedUtil.Trim (key.GetValue (str2, value, RegistryValueOptions.None) as string);
									}
								}
							}
						}
					}
				} catch {
				}
				try {
					return ("(" + Path.GetFileNameWithoutExtension (Extract (value)) + ")");
				} catch {
					return ("(" + value + ")");
				}
			}

			public void Dispose () {
				if (this.icon != null) {
					this.icon.Dispose ();
					this.icon = null;
				}
				if (this.image != null) {
					this.image.Dispose ();
					this.image = null;
				}
				if (this.smallIcon != null) {
					this.smallIcon.Dispose ();
					this.smallIcon = null;
				}
				if (this.smallImage != null) {
					this.smallImage.Dispose ();
					this.smallImage = null;
				}
			}

			internal void ExecuteVerb (Verb verb, IntPtr errorDialogParentHandle, bool useShellExecute) {
				ExecuteVerb (verb, errorDialogParentHandle, useShellExecute, this.FilePath);
			}

			internal static void ExecuteVerb (Verb verb, IntPtr errorDialogParentHandle, bool useShellExecute, string filePath) {
				string args = null;
				int num;
				string newValue = Guid.NewGuid ().ToString ("N");
				string str4 = filePath.Replace (".exe", newValue);
				bool flag = true;
				if (verb == null) {
					Open (filePath, null, errorDialogParentHandle, useShellExecute);
					return;
				}
				string cmd = verb.Command.Replace ("\"%1\"", "\"" + str4 + "\"").Replace ("\"%l\"", "\"" + str4 + "\"");
				if (cmd.Contains ("rundll32")) {
					cmd = cmd.Replace ("%1", str4 ?? "").Replace ("%l", str4 ?? "");
				} else {
					cmd = cmd.Replace ("%1", "\"" + str4 + "\"").Replace ("%l", "\"" + str4 + "\"");
				}
				if (!cmd.Contains (str4)) {
					cmd = cmd + " \"" + str4 + "\"";
				}
			Label_0106:
				if (((num = cmd.ToLower ().IndexOf (".exe\"")) > 0) && ((num + 5) < cmd.Length)) {
					args = cmd.Substring (num + 5).Trim ();
					cmd = cmd.Substring (0, num + 5);
				} else if (((num = cmd.ToLower ().IndexOf (".exe")) > 0) && ((num + 4) < cmd.Length)) {
					args = cmd.Substring (num + 4).Trim ();
					cmd = cmd.Substring (0, num + 4);
				}
				cmd = cmd.Replace (newValue, ".exe");
				if (args != null) {
					args = args.Replace (newValue, ".exe");
				} else if (flag) {
					flag = false;
					goto Label_0106;
				}
				if (args == "%*") {
					args = null;
				}
				Open (cmd, args, errorDialogParentHandle, useShellExecute);
			}

			private static string Extract (string value) {
				if (!SharedUtil.IsEmpty (value)) {
					for (int i = 0; i < 20; i++) {
						value = value.Replace ("%" + i, string.Empty);
					}
					value = value.Replace ("\"\"", string.Empty).Trim ();
					int index = value.ToLower ().IndexOf (".exe");
					if (index > 0) {
						value = value.Substring (0, index + 4);
					}
					while (value.StartsWith ("\"") || value.StartsWith ("@")) {
						value = value.Substring (1);
					}
					while (value.EndsWith ("\"")) {
						value = value.Substring (0, value.Length - 1);
					}
				}
				return SharedUtil.Trim (value);
			}

			internal Image GetDisabledImage (Color backColor, bool large) {
				return DrawingUtil.GetDisabledImage (backColor, large ? this.Image : this.SmallImage);
			}

			internal void Open (IntPtr errorDialogParentHandle, bool useShellExecute) {
				Open (this.FilePath, null, errorDialogParentHandle, useShellExecute);
			}

			private static void Open (string cmd, string args, IntPtr errorDialogParentHandle, bool useShellExecute) {
				ProcessStartInfo startInfo = null;
				Process process = null;
				try {
					if ((startInfo = SharedUtil.IsEmpty (args) ? new ProcessStartInfo (cmd) : new ProcessStartInfo (cmd, args)).ErrorDialog = !IntPtr.Zero.Equals (errorDialogParentHandle))
						startInfo.ErrorDialogParentHandle = errorDialogParentHandle;
					startInfo.UseShellExecute = useShellExecute;
					process = Process.Start (startInfo);
				} finally {
					if (process != null) {
						process.Dispose ();
					}
				}
			}

			[DllImport ("shell32.dll", SetLastError = true)]
			private static extern bool ShellExecuteEx (ref ShellExecuteInfo lpExecInfo);

			internal static bool ShowOpenWith (string filePath) {
				ShellExecuteInfo structure = new ShellExecuteInfo ();
				structure.Size = Marshal.SizeOf (structure);
				structure.Verb = "openas";
				structure.File = filePath;
				structure.Show = 1;
				try {
					return ShellExecuteEx (ref structure);
				} catch {
					return false;
				}
			}

			internal static bool ShowProperties (string filePath) {
				ShellExecuteInfo structure = new ShellExecuteInfo ();
				structure.Size = Marshal.SizeOf (structure);
				structure.Verb = "properties";
				structure.File = filePath;
				structure.Show = 5;
				structure.Mask = 12;
				try {
					return ShellExecuteEx (ref structure);
				} catch {
					return false;
				}
			}

			// Properties
			internal string ContentType {
				get {
					if (this.contentType == null) {
						try {
							RegistryKey key = Registry.ClassesRoot.OpenSubKey (this.Extension, false);
							if (key != null) {
								using (key) {
									this.contentType = key.GetValue ("Content Type", string.Empty, RegistryValueOptions.None) as string;
								}
							}
						} catch {
							this.contentType = string.Empty;
						}
					}
					return this.contentType;
				}
			}

			internal bool Exists {
				get {
					return File.Exists (this.FilePath);
				}
			}

			internal string FileType {
				get {
					string dotLessExtension = this.DotLessExtension;
					try {
						RegistryKey key = Registry.ClassesRoot.OpenSubKey (this.Extension, false);
						if (key != null) {
							using (key) {
								dotLessExtension = key.GetValue (null, dotLessExtension, RegistryValueOptions.None) as string;
							}
						}
						key = Registry.ClassesRoot.OpenSubKey (dotLessExtension, false);
						if (key != null) {
							using (key) {
								dotLessExtension = key.GetValue (null, dotLessExtension, RegistryValueOptions.None) as string;
							}
						}
					} catch {
					}
					if (SharedUtil.IsEmpty (dotLessExtension)) {
						dotLessExtension = this.DotLessExtension;
					}
					return SharedUtil.Trim (dotLessExtension);
				}
			}

			internal Icon Icon {
				get {
					if (this.icon != null) {
						return this.icon;
					}
					return (this.icon = DrawingUtil.GetFileIcon (this.FilePath, true));
				}
			}

			internal Image Image {
				get {
					if (this.image != null) {
						return this.image;
					}
					return (this.image = DrawingUtil.GetFileImage (this.FilePath, true));
				}
			}

			internal Verb PrimaryVerb {
				get {
					RegistryKey key = null;
					RegistryKey key2 = null;
					RegistryKey key3 = null;
					string title = string.Empty;
					string str2 = string.Empty;
					Image fileImage = null;
					if (this.primaryVerb == null) {
						try {
							key = Registry.ClassesRoot.OpenSubKey (this.Extension, false);
							if (key != null) {
								string str3;
								if (!SharedUtil.IsEmpty (str3 = key.GetValue (null, string.Empty) as string) && (((key2 = Registry.ClassesRoot.OpenSubKey (str3, false)) == null) || ((key3 = key2.OpenSubKey (@"shell\open\command", false)) == null))) {
									key3 = key.OpenSubKey (@"shell\open\command", false);
								}
								if (((key3 == null) && (key2 != null)) && (((key3 = key2.OpenSubKey ("shell", false)) != null) && (key3.SubKeyCount > 0))) {
									key3 = key3.OpenSubKey (key3.GetSubKeyNames () [0] + @"\command", false);
								}
								if ((key3 != null) && !SharedUtil.IsEmpty (str3 = key3.GetValue (null, null, RegistryValueOptions.None) as string)) {
									str2 = str3;
									str3 = Extract ((this.Extension == ".exe") ? this.FilePath : str3);
									title = AppDesc (ref str3);
									string str4 = AppDesc (ref str2);
									if (str4.Contains (" ") || str4.Contains ("-")) {
										title = str4;
									}
									if (this.Extension != ".exe") {
										fileImage = DrawingUtil.GetFileImage (Extract (str3), false);
									}
								}
							}
						} catch {
						} finally {
							if (key3 != null) {
								key3.Close ();
							}
							if (key2 != null) {
								key2.Close ();
							}
							if (key != null) {
								key.Close ();
							}
							this.primaryVerb = new Verb (title, str2, fileImage);
						}
					}
					return this.primaryVerb;
				}
			}

			internal IEnumerable<Verb> SecondaryVerbs {
				get {
					RegistryKey key = null;
					RegistryKey key2 = null;
					RegistryKey key3 = null;
					string filePath = string.Empty;
					if (this.secondaryVerbs == null) {
						try {
							this.secondaryVerbs = new List<Verb> ();
							List<string> list = new List<string> ();
							key = Registry.ClassesRoot.OpenSubKey (this.Extension, false);
							if (key != null) {
								if (((key2 = Registry.ClassesRoot.OpenSubKey (key.GetValue (null, this.Extension, RegistryValueOptions.None) as string, false)) == null) || ((key3 = key2.OpenSubKey ("OpenWithList", false)) == null)) {
									key3 = key.OpenSubKey ("OpenWithList", false);
								}
								if (key3 != null) {
									list.AddRange (key3.GetSubKeyNames ());
									key3.Close ();
								}
								key.Close ();
								key3 = Registry.LocalMachine.OpenSubKey (@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths", false);
								if (key3 != null) {
									foreach (string str3 in list) {
										string str2;
										key = key3.OpenSubKey (str3, false);
										if (key != null) {
											str2 = SharedUtil.Trim (key.GetValue (null, null, RegistryValueOptions.None) as string);
											this.secondaryVerbs.Add (new Verb (AppDesc (ref str2), str2, (this.Extension == ".exe") ? null : DrawingUtil.GetFileImage (Extract (str2), false)));
											key.Close ();
											continue;
										}
										if ((this.Extension == ".exe") || (this.Extension == ".com")) {
											str2 = str3;
											filePath = this.FilePath;
											this.secondaryVerbs.Add (new Verb (AppDesc (ref filePath), str2, null));
											continue;
										}
										str2 = str3;
										this.secondaryVerbs.Add (new Verb (AppDesc (ref str2), str2, DrawingUtil.GetFileImage (Extract (str2), false)));
									}
								}
							}
						} catch {
							this.secondaryVerbs = null;
						} finally {
							if (key3 != null) {
								key3.Close ();
							}
							if (key2 != null) {
								key2.Close ();
							}
							if (key != null) {
								key.Close ();
							}
						}
					}
					return this.secondaryVerbs;
				}
			}

			internal Icon SmallIcon {
				get {
					if (this.smallIcon != null) {
						return this.smallIcon;
					}
					return (this.smallIcon = DrawingUtil.GetFileIcon (this.FilePath, false));
				}
			}

			internal Image SmallImage {
				get {
					if (this.smallImage != null) {
						return this.smallImage;
					}
					return (this.smallImage = DrawingUtil.GetFileImage (this.FilePath, false));
				}
			}

			// Nested Types
			[Serializable, StructLayout (LayoutKind.Sequential)]
			private struct ShellExecuteInfo {
				internal int Size;
				internal uint Mask;
				internal IntPtr hwnd;
				internal string Verb;
				internal string File;
				internal string Parameters;
				internal string Directory;
				internal uint Show;
				internal IntPtr InstApp;
				internal IntPtr IDList;
				internal string Class;
				internal IntPtr hkeyClass;
				internal uint HotKey;
				internal IntPtr Icon;
				internal IntPtr Monitor;
			}

			internal class Verb : Trio<string, string, Image> {
				// Methods
				internal Verb (string title, string command, Image image)
					: base (title, command, image) {
				}

				// Properties
				internal string Command {
					get {
						return base.Value2;
					}
				}

				internal Image Image {
					get {
						return base.Value3;
					}
				}

				internal string Title {
					get {
						return base.Value1;
					}
				}
			}

		}

	}

	#endregion

	#region Reflection Namespace

	namespace Reflection {

		using IO;

		internal static class AssemblyUtil {
			// Fields
			private static readonly List<Assembly> allAssemblies = new List<Assembly> ();
			private static Hashtable loadedAssemblies = null;
			private static Hashtable loadedTypes = null;

			// Methods
			internal static void CopyResource (Assembly assembly, string resourceName, Stream targetStream) {
				using (Stream stream = assembly.GetManifestResourceStream (resourceName)) {
					IOUtil.CopyStream (stream, targetStream);
				}
			}

			internal static void CopyResource (Assembly assembly, string resourceName, string targetFilePath) {
				using (FileStream stream = File.Create (targetFilePath)) {
					CopyResource (assembly, resourceName, stream);
				}
			}

			internal static List<Assembly> GetAllAssemblies () {
				return GetAllAssemblies (null);
			}

			internal static List<Assembly> GetAllAssemblies (ProgressBar progressBar) {
				if (allAssemblies.Count == 0) {
					Assembly [] assemblyArray;
					GetAllAssemblies (allAssemblies, Assembly.GetEntryAssembly ());
					if (!SharedUtil.IsEmpty ((ICollection) (assemblyArray = AppDomain.CurrentDomain.GetAssemblies ()))) {
						if (progressBar != null) {
							progressBar.Value = 0;
							progressBar.Maximum = assemblyArray.Length;
							Application.DoEvents ();
						}
						foreach (Assembly assembly in assemblyArray) {
							if (progressBar != null) {
								progressBar.Value++;
								Application.DoEvents ();
							}
							GetAllAssemblies (allAssemblies, assembly);
						}
					}
				}
				return allAssemblies;
			}

			private static void GetAllAssemblies (List<Assembly> list, Assembly asm) {
				if (((list != null) && (asm != null)) && !list.Contains (asm)) {
					list.Add (asm);
					foreach (AssemblyName name in asm.GetReferencedAssemblies ()) {
						try {
							GetAllAssemblies (list, Assembly.Load (name));
						} catch {
						}
					}
				}
			}

			internal static MethodInfo GetBaseDefinition (MethodInfo methodInfo) {
				Type type;
				PropertyInfo info;
				return GetBaseDefinition (methodInfo, false, out type, out info);
			}

			internal static MethodInfo GetBaseDefinition (MethodInfo methodInfo, bool findInterfaceProperty, out Type interfaceType, out PropertyInfo interfaceProperty) {
				MethodInfo info;
				Type [] typeArray;
				SharedUtil.ThrowIfNull (methodInfo, "methodInfo");
				interfaceType = null;
				interfaceProperty = null;
				if (((info = methodInfo.GetBaseDefinition ()) == methodInfo) && !SharedUtil.IsEmpty ((ICollection) (typeArray = methodInfo.DeclaringType.GetInterfaces ()))) {
					foreach (Type type in typeArray) {
						InterfaceMapping interfaceMap = methodInfo.DeclaringType.GetInterfaceMap (type);
						for (int i = 0; i < interfaceMap.InterfaceMethods.Length; i++) {
							if (interfaceMap.TargetMethods [i] != methodInfo) {
								continue;
							}
							if (findInterfaceProperty) {
								PropertyInfo [] infoArray;
								Type type2;
								interfaceType = type2 = type;
								if (!SharedUtil.IsEmpty ((ICollection) (infoArray = type2.GetProperties ()))) {
									foreach (PropertyInfo info2 in infoArray) {
										if ((info2.GetGetMethod () == interfaceMap.InterfaceMethods [i]) || (info2.GetSetMethod () == interfaceMap.InterfaceMethods [i])) {
											interfaceProperty = info2;
											break;
										}
									}
								}
							}
							return interfaceMap.InterfaceMethods [i];
						}
					}
				}
				return info;
			}

			internal static string GetCompany () {
				return GetCompany (Assembly.GetCallingAssembly ());
			}

			internal static string GetCompany (Assembly assembly) {
				AssemblyCompanyAttribute attribute = null;
				attribute = Attribute.GetCustomAttribute (assembly, typeof (AssemblyCompanyAttribute), true) as AssemblyCompanyAttribute;
				if (attribute == null) {
					return string.Empty;
				}
				return attribute.Company;
			}

			internal static string GetCopyright (Assembly assembly) {
				AssemblyCopyrightAttribute attribute = null;
				attribute = Attribute.GetCustomAttribute (assembly, typeof (AssemblyCopyrightAttribute), true) as AssemblyCopyrightAttribute;
				if (attribute == null) {
					return string.Empty;
				}
				return attribute.Copyright;
			}

			internal static string GetDescription (Assembly assembly) {
				AssemblyDescriptionAttribute attribute = null;
				attribute = Attribute.GetCustomAttribute (assembly, typeof (AssemblyDescriptionAttribute), true) as AssemblyDescriptionAttribute;
				if (attribute == null) {
					return string.Empty;
				}
				return attribute.Description;
			}

			internal static Type [] GetLoadedTypes (Type baseType, bool @abstract, ProgressBar progressBar) {
				return GetLoadedTypes (baseType, false, @abstract, progressBar);
			}

			internal static Type [] GetLoadedTypes (Type baseType, bool includeBaseType, bool @abstract, ProgressBar progressBar) {
				List<Assembly> allAssemblies = GetAllAssemblies (progressBar);
				ArrayList list2 = new ArrayList ();
				if (baseType == null) {
					baseType = typeof (object);
				}
				if (progressBar != null) {
					progressBar.Value = 0;
					progressBar.Maximum = allAssemblies.Count;
					Application.DoEvents ();
				}
				foreach (Assembly assembly in allAssemblies) {
					Type [] typeArray;
					if (progressBar != null) {
						progressBar.Value++;
						Application.DoEvents ();
					}
					if ((assembly != null) && !SharedUtil.IsEmpty ((ICollection) (typeArray = assembly.GetTypes ()))) {
						foreach (Type type in typeArray) {
							if ((((type != null) && baseType.IsAssignableFrom (type)) && (includeBaseType || (type != baseType))) && (@abstract || (!type.IsAbstract && !type.IsInterface))) {
								list2.Add (type);
							}
						}
					}
				}
				return (list2.ToArray (typeof (Type)) as Type []);
			}

			internal static string GetProduct () {
				return GetProduct (Assembly.GetCallingAssembly ());
			}

			internal static string GetProduct (Assembly assembly) {
				AssemblyProductAttribute attribute = null;
				attribute = Attribute.GetCustomAttribute (assembly, typeof (AssemblyProductAttribute), true) as AssemblyProductAttribute;
				if (attribute == null) {
					return string.Empty;
				}
				return attribute.Product;
			}

			internal static string GetTitle (Assembly assembly) {
				AssemblyTitleAttribute attribute = null;
				attribute = Attribute.GetCustomAttribute (assembly, typeof (AssemblyTitleAttribute), true) as AssemblyTitleAttribute;
				if (attribute == null) {
					return string.Empty;
				}
				return attribute.Title;
			}

			internal static string GetTrademark (Assembly assembly) {
				AssemblyTrademarkAttribute attribute = null;
				attribute = Attribute.GetCustomAttribute (assembly, typeof (AssemblyTrademarkAttribute), true) as AssemblyTrademarkAttribute;
				if (attribute == null) {
					return string.Empty;
				}
				return attribute.Trademark;
			}

			internal static Type GetType (TypeCode typeCode) {
				switch (typeCode) {
					case TypeCode.Object:
						return typeof (object);

					case TypeCode.Boolean:
						return typeof (bool);

					case TypeCode.Char:
						return typeof (char);

					case TypeCode.SByte:
						return typeof (sbyte);

					case TypeCode.Byte:
						return typeof (byte);

					case TypeCode.Int16:
						return typeof (short);

					case TypeCode.UInt16:
						return typeof (ushort);

					case TypeCode.Int32:
						return typeof (int);

					case TypeCode.UInt32:
						return typeof (uint);

					case TypeCode.Int64:
						return typeof (long);

					case TypeCode.UInt64:
						return typeof (ulong);

					case TypeCode.Single:
						return typeof (float);

					case TypeCode.Double:
						return typeof (double);

					case TypeCode.Decimal:
						return typeof (decimal);

					case TypeCode.DateTime:
						return typeof (DateTime);

					case TypeCode.String:
						return typeof (string);
				}
				return null;
			}

			internal static string GetTypeName (Type type) {
				return string.Format ("{0},{1}", type.FullName, type.Assembly.GetName ().Name);
			}

			internal static Type [] GetTypes (Assembly assembly) {
				Type [] typeArray;
				lock (LoadedTypes.SyncRoot) {
					typeArray = LoadedTypes [assembly] as Type [];
					if (typeArray == null) {
						LoadedTypes [assembly] = typeArray = assembly.GetTypes ();
					}
				}
				return typeArray;
			}

			internal static Version GetVersion () {
				return GetVersion (Assembly.GetCallingAssembly ());
			}

			internal static Version GetVersion (Assembly assembly) {
				return assembly.GetName ().Version;
			}

			internal static Assembly LoadAssembly (string filePath) {
				return LoadAssembly (filePath, false);
			}

			internal static Assembly LoadAssembly (string filePath, bool forceReload) {
				Assembly [] assemblyArray;
				if (!forceReload && !SharedUtil.IsEmpty ((ICollection) (assemblyArray = AppDomain.CurrentDomain.GetAssemblies ()))) {
					foreach (Assembly assembly in assemblyArray) {
						if (((assembly != null) && !(assembly is AssemblyBuilder)) && (!SharedUtil.IsEmpty (assembly.CodeBase) && IOUtil.PathEquals (assembly.CodeBase, filePath))) {
							return assembly;
						}
					}
				}
				lock (LoadedAssemblies.SyncRoot) {
					if (forceReload || !LoadedAssemblies.ContainsKey (filePath = IOUtil.NormalizePath (filePath))) {
						byte [] buffer;
						using (FileStream stream = new FileStream (filePath, FileMode.Open, FileAccess.Read, FileShare.Read)) {
							buffer = new byte [stream.Length];
							using (MemoryStream stream2 = new MemoryStream (buffer, true)) {
								IOUtil.CopyStream (stream, stream2);
							}
						}
						LoadedAssemblies [filePath] = Assembly.Load (buffer);
					}
					return (LoadedAssemblies [filePath] as Assembly);
				}
			}

			// Properties
			private static Hashtable LoadedAssemblies {
				get {
					if (loadedAssemblies == null) {
						loadedAssemblies = Hashtable.Synchronized (new Hashtable ());
					}
					return loadedAssemblies;
				}
			}

			private static Hashtable LoadedTypes {
				get {
					if (loadedTypes == null) {
						loadedTypes = Hashtable.Synchronized (new Hashtable ());
					}
					return loadedTypes;
				}
			}

		}

	}

	#endregion

	#region Win32 Namespace

	namespace Win32 {

		internal class IniFile {
			// Fields
			internal string Path;

			// Methods
			internal IniFile (string path) {
				this.Path = path;
			}

			[DllImport ("kernel32")]
			private static extern int GetPrivateProfileString (string section, string key, string def, StringBuilder retVal, int size, string filePath);
			[DllImport ("kernel32")]
			private static extern long WritePrivateProfileString (string section, string key, string val, string filePath);

			// Properties
			internal string this [string section, string key] {
				get {
					StringBuilder retVal = new StringBuilder (0xff);
					GetPrivateProfileString (section, key, "", retVal, 0xff, this.Path);
					return retVal.ToString ();
				}
				set {
					WritePrivateProfileString (section, key, value, this.Path);
				}
			}
		}

		internal sealed class RegistryUtil {
			// Fields
			internal const string DEFAULT_EXPORT_PREFACE = "Windows Registry Editor Version 5.00";

			// Methods
			private RegistryUtil () {
			}

			internal static object Deserialize (string name, object defaultValue) {
				return Deserialize (name, defaultValue, StreamingContextStates.Persistence);
			}

			internal static object Deserialize (RegistryKey key, string name, object defaultValue) {
				return Deserialize (key, name, defaultValue, StreamingContextStates.Persistence);
			}

			internal static object Deserialize (string name, object defaultValue, StreamingContextStates streamingContext) {
				return Deserialize (Application.UserAppDataRegistry, name, defaultValue, streamingContext);
			}

			internal static object Deserialize (RegistryKey key, string name, object defaultValue, StreamingContextStates streamingContext) {
				object obj3;
				BinaryFormatter formatter = new BinaryFormatter (null, new StreamingContext (streamingContext));
				try {
					byte [] buffer;
					if (SharedUtil.IsEmpty ((ICollection) (buffer = GetBytes (key, name, null)))) {
						obj3 = defaultValue;
					} else {
						using (MemoryStream stream = new MemoryStream (buffer, false)) {
							object obj2;
							if (((obj2 = formatter.Deserialize (stream)) != null) && ((defaultValue == null) || defaultValue.GetType ().IsAssignableFrom (obj2.GetType ()))) {
								return obj2;
							}
							obj3 = defaultValue;
						}
					}
				} catch {
					obj3 = defaultValue;
				}
				return obj3;
			}

			internal static void Export (RegistryKey key, string filePath) {
				Export (key, filePath, true, "Windows Registry Editor Version 5.00", Encoding.Unicode);
			}

			internal static void Export (RegistryKey key, string filePath, bool recursive, string preface, Encoding encoding) {
				if (key == null) {
					throw new ArgumentNullException ("key");
				}
				if (SharedUtil.IsEmpty (filePath)) {
					throw new ArgumentNullException ("filePath");
				}
				if (SharedUtil.IsEmpty (preface)) {
					preface = "Windows Registry Editor Version 5.00";
				}
				using (StreamWriter writer = new StreamWriter (filePath, false, encoding)) {
					writer.WriteLine (preface);
					ExportKey (key, recursive, writer);
				}
			}

			internal static void ExportKey (RegistryKey key, bool recursive, StreamWriter writer) {
				string [] strArray;
				string [] strArray2;
				writer.WriteLine ("\r\n[{0}]", TrimHex (key.ToString ()));
				ExportValue ("@", key.GetValue (null), writer, false);
				if (!SharedUtil.IsEmpty ((ICollection) (strArray2 = key.GetValueNames ()))) {
					foreach (string str in strArray2) {
						ExportValue (str, key.GetValue (str), writer, false);
					}
				}
				if (recursive && !SharedUtil.IsEmpty ((ICollection) (strArray = key.GetSubKeyNames ()))) {
					foreach (string str2 in strArray) {
						using (RegistryKey key2 = key.OpenSubKey (str2, false)) {
							ExportKey (key2, true, writer);
						}
					}
				}
			}

			internal static void ExportValue (string valueName, int value, StreamWriter writer) {
				writer.WriteLine ("\"{0}\"=dword:{1}", Normalize (valueName), value.ToString ("x"));
			}

			internal static void ExportValue (string valueName, byte [] value, StreamWriter writer) {
				StringBuilder builder = new StringBuilder ("\"" + Normalize (valueName) + "\"=hex:");
				if (!SharedUtil.IsEmpty ((ICollection) value)) {
					builder.Append (value [0].ToString ("x"));
				}
				if (value.Length > 1) {
					for (int i = 1; i < value.Length; i++) {
						builder.AppendFormat (",{0}", value [i].ToString ("x"));
					}
				}
				writer.WriteLine (builder.ToString ());
			}

			internal static void ExportValue (string valueName, string value, StreamWriter writer) {
				writer.WriteLine ("\"{0}\"=\"{1}\"", Normalize (valueName), Normalize (value));
			}

			internal static void ExportValue (string valueName, object value, StreamWriter writer, bool @throw) {
				if ((value == null) && @throw) {
					throw new ArgumentNullException ("value");
				}
				if (value != null) {
					if (value is string) {
						ExportValue (valueName, value as string, writer);
					} else if (value is int) {
						ExportValue (valueName, (int) value, writer);
					} else if (value is byte []) {
						ExportValue (valueName, value as byte [], writer);
					} else if (@throw) {
						throw new ArgumentException (null, "value");
					}
				}
			}

			internal static bool GetBoolean (string name, bool defaultValue) {
				return GetBoolean (Application.UserAppDataRegistry, name, defaultValue);
			}

			internal static bool GetBoolean (RegistryKey key, string name, bool defaultValue) {
				try {
					return bool.Parse (GetString (key, name, defaultValue.ToString ()));
				} catch {
					return defaultValue;
				}
			}

			internal static byte [] GetBytes (string name, params byte [] defaultValue) {
				return GetBytes (Application.UserAppDataRegistry, name, defaultValue);
			}

			internal static byte [] GetBytes (RegistryKey key, string name, params byte [] defaultValue) {
				try {
					return (byte []) key.GetValue (name, defaultValue);
				} catch {
					return defaultValue;
				}
			}

			internal static Enum GetEnum (string name, Enum defaultValue) {
				return GetEnum (Application.UserAppDataRegistry, name, defaultValue);
			}

			internal static Enum GetEnum (RegistryKey key, string name, Enum defaultValue) {
				try {
					Enum enum2 = (Enum) Enum.Parse (defaultValue.GetType (), GetString (key, name, defaultValue.ToString ()), true);
					return ((enum2.GetType () == defaultValue.GetType ()) ? enum2 : defaultValue);
				} catch {
					return defaultValue;
				}
			}

			internal static int GetInt32 (string name, int defaultValue) {
				return GetInt32 (Application.UserAppDataRegistry, name, defaultValue);
			}

			internal static int GetInt32 (RegistryKey key, string name, int defaultValue) {
				try {
					return (int) key.GetValue (name, defaultValue);
				} catch {
					return defaultValue;
				}
			}

			internal static string GetString (string name, string defaultValue) {
				return GetString (Application.UserAppDataRegistry, name, defaultValue);
			}

			internal static string GetString (string name, string defaultValue, string entropy) {
				return GetString (Application.UserAppDataRegistry, name, defaultValue, entropy);
			}

			internal static string GetString (RegistryKey key, string name, string defaultValue, string entropy) {
				try {
					return Encoding.Unicode.GetString (ProtectedData.Unprotect (GetBytes (key, name, Encoding.Unicode.GetBytes (defaultValue)), Encoding.Unicode.GetBytes (entropy), DataProtectionScope.CurrentUser));
				} catch {
					return null;
				}
			}

			internal static string GetString (RegistryKey key, string name, string defaultValue) {
				try {
					return (string) key.GetValue (name, defaultValue);
				} catch {
					return defaultValue;
				}
			}

			internal static string [] GetStrings (string name, params string [] defaultValue) {
				return GetStrings (Application.UserAppDataRegistry, name, defaultValue);
			}

			internal static string [] GetStrings (RegistryKey key, string name, params string [] defaultValue) {
				try {
					return (string []) key.GetValue (name, defaultValue);
				} catch {
					return defaultValue;
				}
			}

			internal static object GetValue (string name, object defaultValue) {
				return GetValue (Application.UserAppDataRegistry, name, defaultValue);
			}

			internal static object GetValue (RegistryKey key, string name, object defaultValue) {
				try {
					TypeConverter converter;
					object obj2;
					if (((defaultValue == null) && ((obj2 = Deserialize (key, name, defaultValue)) == null)) || (defaultValue is string)) {
						return GetString (key, name, (defaultValue == null) ? null : defaultValue.ToString ());
					}
					if (defaultValue is string []) {
						return GetStrings (key, name, (string []) defaultValue);
					}
					if (defaultValue is bool) {
						return GetBoolean (key, name, (bool) defaultValue);
					}
					if (defaultValue is byte []) {
						return GetBytes (key, name, (byte []) defaultValue);
					}
					if (defaultValue is Enum) {
						return GetEnum (key, name, (Enum) defaultValue);
					}
					if (defaultValue is int) {
						return GetInt32 (key, name, (int) defaultValue);
					}
					if ((((defaultValue != null) && ((converter = TypeDescriptor.GetConverter (defaultValue)) != null)) && (converter.CanConvertFrom (typeof (string)) && converter.CanConvertTo (typeof (string)))) && (((obj2 = converter.ConvertFrom (null, CultureInfo.InvariantCulture, GetString (key, name, (string) converter.ConvertTo (null, CultureInfo.InvariantCulture, defaultValue, typeof (string))))) != null) && defaultValue.GetType ().IsAssignableFrom (obj2.GetType ()))) {
						return obj2;
					}
					return Deserialize (key, name, defaultValue);
				} catch {
					return defaultValue;
				}
			}

			private static string Normalize (string value) {
				return value.Replace (@"\", @"\\").Replace ("\"", "\\\"");
			}

			internal static void Serialize (string name, object value) {
				Serialize (Application.UserAppDataRegistry, name, value);
			}

			internal static void Serialize (RegistryKey key, string name, object value) {
				Serialize (key, name, value, StreamingContextStates.Persistence);
			}

			internal static void Serialize (string name, object value, StreamingContextStates streamingContext) {
				Serialize (Application.UserAppDataRegistry, name, value, streamingContext);
			}

			internal static void Serialize (RegistryKey key, string name, object value, StreamingContextStates streamingContext) {
				BinaryFormatter formatter = new BinaryFormatter (null, new StreamingContext (streamingContext));
				using (MemoryStream stream = new MemoryStream ()) {
					formatter.Serialize (stream, value);
					SetBytes (key, name, stream.ToArray ());
				}
			}

			internal static void SetBoolean (string name, bool value) {
				SetBoolean (Application.UserAppDataRegistry, name, value);
			}

			internal static void SetBoolean (RegistryKey key, string name, bool value) {
				try {
					SetString (key, name, value.ToString ());
				} catch {
				}
			}

			internal static void SetBytes (string name, params byte [] value) {
				SetBytes (Application.UserAppDataRegistry, name, value);
			}

			internal static void SetBytes (RegistryKey key, string name, params byte [] value) {
				try {
					key.SetValue (name, value);
				} catch {
				}
			}

			internal static void SetEnum (string name, Enum value) {
				SetEnum (Application.UserAppDataRegistry, name, value);
			}

			internal static void SetEnum (RegistryKey key, string name, Enum value) {
				try {
					SetString (key, name, value.ToString ());
				} catch {
				}
			}

			internal static void SetInt32 (string name, int value) {
				SetInt32 (Application.UserAppDataRegistry, name, value);
			}

			internal static void SetInt32 (RegistryKey key, string name, int value) {
				try {
					key.SetValue (name, value);
				} catch {
				}
			}

			internal static void SetString (string name, string value) {
				SetString (Application.UserAppDataRegistry, name, value);
			}

			internal static void SetString (string name, string value, string entropy) {
				SetString (Application.UserAppDataRegistry, name, value, entropy);
			}

			internal static void SetString (RegistryKey key, string name, string value, string entropy) {
				SetBytes (key, name, ProtectedData.Protect (Encoding.Unicode.GetBytes (value), Encoding.Unicode.GetBytes (entropy), System.Security.Cryptography.DataProtectionScope.CurrentUser));
			}

			internal static void SetString (RegistryKey key, string name, string value) {
				try {
					if ((value == null) && (Array.IndexOf<string> (key.GetValueNames (), name) >= 0)) {
						key.DeleteValue (name);
					} else if (value != null) {
						key.SetValue (name, value);
					}
				} catch {
				}
			}

			internal static void SetStrings (string name, params string [] value) {
				SetStrings (Application.UserAppDataRegistry, name, value);
			}

			internal static void SetStrings (RegistryKey key, string name, params string [] value) {
				try {
					if (value == null) {
						key.DeleteValue (name, false);
					} else {
						key.SetValue (name, value);
					}
				} catch {
				}
			}

			internal static void SetValue (string name, object value) {
				SetValue (Application.UserAppDataRegistry, name, value);
			}

			internal static void SetValue (RegistryKey key, string name, object value) {
				TypeConverter converter = (value == null) ? null : TypeDescriptor.GetConverter (value);
				if (value == null) {
					key.DeleteValue (name, false);
				} else if (value is string) {
					SetString (key, name, (string) value);
				} else if (value is string []) {
					SetStrings (key, name, (string []) value);
				} else if (value is bool) {
					SetBoolean (key, name, (bool) value);
				} else if (value is byte []) {
					SetBytes (key, name, (byte []) value);
				} else if (value is Enum) {
					SetEnum (key, name, (Enum) value);
				} else if (value is int) {
					SetInt32 (key, name, (int) value);
				} else {
					string str;
					if (((converter != null) && converter.CanConvertFrom (typeof (string))) && (converter.CanConvertTo (typeof (string)) && ((str = converter.ConvertTo (null, CultureInfo.InvariantCulture, value, typeof (string)) as string) != null))) {
						SetString (key, name, str);
					} else {
						Serialize (key, name, value);
					}
				}
			}

			private static string TrimHex (string keyName) {
				int num;
				if (SharedUtil.IsEmpty (keyName)) {
					throw new ArgumentNullException ("keyName");
				}
				if (((keyName.LastIndexOf (']') == (keyName.Length - 1)) && ((num = keyName.LastIndexOf (" [0x")) > 0)) && (num < (keyName.Length - 1))) {
					return keyName.Substring (0, num);
				}
				return keyName;
			}
		}

	}

	#endregion

	#region Xml Namespace

	namespace Xml {

		internal static class XmlUtil {
			// Methods
			internal static string Attribute (XmlNode node, string name) {
				return Attribute (node, name, string.Empty);
			}

			internal static string Attribute (XmlNode node, string name, string defaultValue) {
				XmlAttribute attribute;
				if (((attribute = node.Attributes [name]) != null) && !SharedUtil.IsEmpty (attribute.Value)) {
					return attribute.Value;
				}
				return defaultValue;
			}

			internal static XmlDocument Compress (XmlDocument document) {
				if ((document != null) && (document.DocumentElement != null)) {
					Dictionary<string, string> dictionary;
					Dictionary<string, string> dictionary2;
					XmlNodeList list;
					XmlNode node;
					if (!SharedUtil.IsEmpty (list = document.SelectNodes ("processing-instruction()"))) {
						foreach (XmlProcessingInstruction instruction in list) {
							if (instruction.Name.StartsWith ("n_")) {
								return document;
							}
						}
					}
					Compress (dictionary2 = new Dictionary<string, string> (), dictionary = new Dictionary<string, string> (), document.DocumentElement);
					foreach (KeyValuePair<string, string> pair in dictionary2) {
						document.InsertBefore (document.CreateProcessingInstruction (pair.Value, pair.Key), document.DocumentElement);
					}
					if (!SharedUtil.IsEmpty (list = document.DocumentElement.SelectNodes ("dsxc_values"))) {
						foreach (XmlNode node2 in list) {
							document.DocumentElement.RemoveChild (node2);
						}
					}
					document.DocumentElement.InsertBefore (node = document.CreateElement ("dsxc_values"), document.DocumentElement.FirstChild);
					foreach (KeyValuePair<string, string> pair2 in dictionary) {
						node.AppendChild (document.CreateElement (pair2.Value)).AppendChild (document.CreateCDataSection (pair2.Key));
					}
				}
				return document;
			}

			internal static void Compress (Dictionary<string, string> symbolTable, Dictionary<string, string> valueTable, XmlNode node) {
				XmlDocument document;
				if ((node != null) && ((document = node.OwnerDocument) != null)) {
					string str;
					if (!symbolTable.TryGetValue (node.LocalName, out str)) {
						symbolTable [node.LocalName] = str = "n_" + symbolTable.Count;
					}
					XmlNode newChild = document.CreateElement (str);
					while ((node.Attributes != null) && (node.Attributes.Count > 0)) {
						XmlAttribute attribute;
						string str2;
						node.Attributes.Remove (attribute = node.Attributes [0]);
						if (!symbolTable.TryGetValue (attribute.LocalName, out str)) {
							symbolTable [attribute.LocalName] = str = "n_" + symbolTable.Count;
						}
						if (!valueTable.TryGetValue (attribute.Value, out str2)) {
							if (attribute.Value.Length > ("v" + valueTable.Count).Length) {
								valueTable [attribute.Value] = str2 = "v" + valueTable.Count;
							} else {
								str2 = attribute.Value;
							}
						}
						newChild.Attributes.Append (document.CreateAttribute (str)).Value = str2;
					}
					while (node.HasChildNodes) {
						XmlNode node3;
						node.RemoveChild (node3 = node.FirstChild);
						newChild.AppendChild (node3);
						if (node3.NodeType == XmlNodeType.Element) {
							Compress (symbolTable, valueTable, node3);
						}
					}
					node.ParentNode.ReplaceChild (newChild, node);
				}
			}

			internal static string EnsureAttribute (XmlNode node, string attributeName, string defaultValue) {
				return EnsureAttribute (node, attributeName, defaultValue, false);
			}

			internal static string EnsureAttribute (XmlNode node, string attributeName, string defaultValue, bool overwrite) {
				XmlAttribute orCreateAttribute = GetOrCreateAttribute (node, attributeName);
				if (overwrite || SharedUtil.IsEmpty (orCreateAttribute.Value)) {
					orCreateAttribute.Value = defaultValue;
				}
				return orCreateAttribute.Value;
			}

			internal static XmlDocument Expand (XmlDocument document) {
				XmlNode node = null;
				XmlNodeList list;
				if (((document != null) && (document.DocumentElement != null)) && !SharedUtil.IsEmpty (list = document.DocumentElement.SelectNodes ("dsxc_values"))) {
					foreach (XmlNode node2 in list) {
						document.DocumentElement.RemoveChild (node = node2);
					}
					Dictionary<string, string> valueTable = new Dictionary<string, string> ();
					Dictionary<string, string> symbolTable = new Dictionary<string, string> ();
					if (SharedUtil.IsEmpty (list = document.SelectNodes ("processing-instruction()"))) {
						return document;
					}
					foreach (XmlProcessingInstruction instruction in list) {
						if (instruction.Name.StartsWith ("n_")) {
							document.RemoveChild (instruction);
							symbolTable [instruction.Name] = instruction.Value;
						}
					}
					if (SharedUtil.IsEmpty ((ICollection) symbolTable)) {
						return document;
					}
					foreach (XmlNode node3 in node) {
						if ((node3.FirstChild != null) && (node3.FirstChild.NodeType == XmlNodeType.CDATA)) {
							valueTable [node3.LocalName] = node3.FirstChild.Value;
						}
					}
					Expand (symbolTable, valueTable, document.DocumentElement);
				}
				return document;
			}

			internal static void Expand (Dictionary<string, string> symbolTable, Dictionary<string, string> valueTable, XmlNode node) {
				XmlDocument document;
				if ((node != null) && ((document = node.OwnerDocument) != null)) {
					string localName;
					if (!symbolTable.TryGetValue (node.LocalName, out localName)) {
						localName = node.LocalName;
					}
					XmlNode newChild = document.CreateElement (localName);
					while ((node.Attributes != null) && (node.Attributes.Count > 0)) {
						XmlAttribute attribute;
						string str2;
						node.Attributes.Remove (attribute = node.Attributes [0]);
						if (!symbolTable.TryGetValue (attribute.LocalName, out localName)) {
							localName = attribute.LocalName;
						}
						if (!valueTable.TryGetValue (attribute.Value, out str2)) {
							str2 = attribute.Value;
						}
						newChild.Attributes.Append (document.CreateAttribute (localName)).Value = str2;
					}
					while (node.HasChildNodes) {
						XmlNode node3;
						node.RemoveChild (node3 = node.FirstChild);
						newChild.AppendChild (node3);
						if (node3.NodeType == XmlNodeType.Element) {
							Expand (symbolTable, valueTable, node3);
						}
					}
					node.ParentNode.ReplaceChild (newChild, node);
				}
			}

			internal static void ForEach (XmlNode node, string xpath, Action<XmlNode> action) {
				XmlNodeList list = node.SelectNodes (xpath);
				if (!SharedUtil.IsEmpty (list)) {
					foreach (XmlNode node2 in list) {
						action (node2);
					}
				}
			}

			internal static string GetAttributeValue (XmlNode node, string name, string defaultValue) {
				XmlAttribute attribute;
				if (((node != null) && (node.Attributes != null)) && ((attribute = node.Attributes [name]) != null)) {
					return attribute.Value;
				}
				return defaultValue;
			}

			internal static Duo<XmlNode, string> GetNodeAtPosition (XmlNode node, int selectionStart, int selectionLength, XmlNodeType moveUpTo, params XmlNodeType [] moveUpIf) {
				XmlNode parentNode = null;
				string str = null;
				string str2;
				if ((((node != null) && !SharedUtil.IsEmpty (str2 = node.OuterXml)) && ((selectionStart >= 0) && (selectionStart <= str2.Length))) && ((selectionLength >= 0) && ((selectionStart + selectionLength) <= str2.Length))) {
					str = str2.Substring (selectionStart, selectionLength);
					if (!node.HasChildNodes) {
						parentNode = node;
					} else {
						int num;
						string innerXml = node.InnerXml;
						int num2 = num = str2.IndexOf (innerXml, str2.IndexOf ('>'));
						if (num2 < 0) {
							num2 = num = 0;
						}
						if (selectionStart >= num) {
							foreach (XmlNode node3 in node.ChildNodes) {
								if (((selectionStart >= num2) && (selectionStart < (num2 + node3.OuterXml.Length))) && ((parentNode = GetNodeAtPosition (node3, selectionStart - num2, selectionLength, moveUpTo, new XmlNodeType [0]).Value1) != null)) {
									break;
								}
								num2 += node3.OuterXml.Length;
							}
						}
						if (parentNode == null) {
							parentNode = node;
						}
					}
					if (moveUpTo != XmlNodeType.None) {
						while (((parentNode != null) && (parentNode.NodeType != moveUpTo)) && (parentNode.ParentNode != null)) {
							parentNode = parentNode.ParentNode;
						}
					} else if (!SharedUtil.IsEmpty ((ICollection) moveUpIf)) {
						while (((parentNode != null) && SharedUtil.In<XmlNodeType> (parentNode.NodeType, moveUpIf)) && (parentNode.ParentNode != null)) {
							parentNode = parentNode.ParentNode;
						}
					}
				}
				return new Duo<XmlNode, string> (parentNode, str);
			}

			internal static int GetNodePosition (XmlNode parentNode, XmlNode targetNode) {
				if ((targetNode != null) && (parentNode != null)) {
					if ((targetNode == parentNode) || ((targetNode.OuterXml == parentNode.OuterXml) && (GetXPath (targetNode) == GetXPath (parentNode)))) {
						return 0;
					}
					if (parentNode.HasChildNodes) {
						int index = parentNode.OuterXml.IndexOf (parentNode.InnerXml, parentNode.OuterXml.IndexOf ('>'));
						if (index < 0) {
							index = 0;
						}
						foreach (XmlNode node in parentNode.ChildNodes) {
							int nodePosition = GetNodePosition (node, targetNode);
							if (nodePosition >= 0) {
								return (index + nodePosition);
							}
							index += node.OuterXml.Length;
						}
					}
				}
				return -1;
			}

			internal static XmlAttribute GetOrCreateAttribute (XmlNode node, string name) {
				XmlAttribute attribute = node.Attributes [name];
				if (attribute != null) {
					return attribute;
				}
				return (attribute = node.Attributes.Append (node.OwnerDocument.CreateAttribute (name)));
			}

			internal static XmlNode GetOrCreateCData (XmlNode node, string data) {
				if (node == null) {
					return null;
				}
				if (!node.HasChildNodes || (node.FirstChild.NodeType != XmlNodeType.CDATA)) {
					XmlNode newChild = node.OwnerDocument.CreateCDataSection (data);
					if (node.HasChildNodes) {
						node.InsertBefore (newChild, node.FirstChild);
					} else {
						node.AppendChild (newChild);
					}
				}
				return node.FirstChild;
			}

			internal static XmlNode GetOrCreateNode (XmlNode node, string name) {
				if (node == null) {
					return null;
				}
				XmlNode node2 = node.SelectSingleNode (name);
				if (node2 != null) {
					return node2;
				}
				return (node2 = node.AppendChild (node.OwnerDocument.CreateElement (name)));
			}

			internal static string GetXPath (XmlNode node) {
				return GetXPath (node, null, null);
			}

			internal static string GetXPath (XmlNode node, XmlNode stopBefore, string encodeSlash) {
				string str = null;
				int num = 0;
				if (node != null) {
					string str2;
					str = "/" + node.LocalName;
					if (!SharedUtil.IsEmpty ((ICollection) node.Attributes)) {
						str = str + " [";
						foreach (XmlAttribute attribute in node.Attributes) {
							str = str + string.Format ("@{0} = '{1}' and ", attribute.LocalName, attribute.Value.Replace ("'", "&apos;").Replace ("/", SharedUtil.IsEmpty (encodeSlash) ? "/" : encodeSlash));
						}
						str = str.Substring (0, str.Length - " and ".Length) + "]";
					} else {
						XmlNodeList list;
						if (((node.ParentNode != null) && (node.NodeType == XmlNodeType.Element)) && (!SharedUtil.IsEmpty (list = node.ParentNode.SelectNodes (node.LocalName)) && (list.Count > 1))) {
							foreach (XmlNode node2 in list) {
								num++;
								if (node2 == node) {
									break;
								}
								if (num == list.Count) {
									num = 0;
								}
							}
							if (num > 0) {
								object obj2 = str;
								str = string.Concat (new object [] { obj2, " [", num, "]" });
							}
						}
					}
					if (((node.ParentNode != null) && (node.ParentNode != node.OwnerDocument)) && ((node.ParentNode != stopBefore) && !SharedUtil.IsEmpty (str2 = GetXPath (node.ParentNode)))) {
						str = str2 + str;
					}
				}
				return str;
			}

			internal static XmlDocument LoadXmlDocument (string filePath) {
				return LoadXmlDocument (filePath, null);
			}

			internal static XmlDocument LoadXmlDocument (string filePath, XmlNameTable nameTable) {
				XmlDocument document = (nameTable == null) ? new XmlDocument () : new XmlDocument (nameTable);
				document.Load (filePath);
				return document;
			}

			internal static XmlNode LoadXmlNode (XmlDocument document, string xml) {
				document.LoadXml (xml);
				return document.DocumentElement;
			}

			internal static XmlDocument ParseXmlDocument (string xml) {
				return ParseXmlDocument (xml, null);
			}

			internal static XmlDocument ParseXmlDocument (string xml, XmlNameTable nameTable) {
				XmlDocument document = (nameTable == null) ? new XmlDocument () : new XmlDocument (nameTable);
				document.LoadXml (xml);
				return document;
			}

			internal static XmlNode ParseXmlNode (XmlDocument document, string xml, XmlNameTable nameTable) {
				XmlDocument document2 = new XmlDocument (nameTable);
				document2.LoadXml (xml);
				return document.ImportNode (document2.DocumentElement, true);
			}

			internal static int TotalNodeCount (XmlNode node) {
				int num = 0;
				if (node != null) {
					num++;
					if (!node.HasChildNodes) {
						return num;
					}
					foreach (XmlNode node2 in node.ChildNodes) {
						num += TotalNodeCount (node2);
					}
				}
				return num;
			}
		}

	}

	#endregion

}
