
using System;
using System.Data;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace roxority.Shared {

	/// <summary>
	/// This class encodes and decodes JSON strings.
	/// Spec. details, see http://www.json.org/
	/// 
	/// JSON uses Arrays and Objects. These correspond here to the datatypes ArrayList and Hashtable.
	/// All numbers are parsed to doubles.
	/// </summary>

	public class JSON {
		public const int TOKEN_NONE = 0;
		public const int TOKEN_CURLY_OPEN = 1;
		public const int TOKEN_CURLY_CLOSE = 2;
		public const int TOKEN_SQUARED_OPEN = 3;
		public const int TOKEN_SQUARED_CLOSE = 4;
		public const int TOKEN_COLON = 5;
		public const int TOKEN_COMMA = 6;
		public const int TOKEN_STRING = 7;
		public const int TOKEN_NUMBER = 8;
		public const int TOKEN_TRUE = 9;
		public const int TOKEN_FALSE = 10;
		public const int TOKEN_NULL = 11;

		protected static JSON instance = new JSON ();

		/// <summary>
		/// On decoding, this value holds the position at which the parse failed (-1 = no error).
		/// </summary>
		protected int lastErrorIndex = -1;
		protected string lastDecode = "";
		protected bool ordered = false;

		public static object ConvertToGeneric (object v) {
			IList list = v as IList, genList;
			IDictionary dict = v as IDictionary, genDict;
			Type keyType = null, valueType = null;
			if (dict != null) {
				foreach (DictionaryEntry entry in dict) {
					if (entry.Key != null)
						if (keyType == null)
							keyType = entry.Key.GetType ();
						else if (keyType != entry.Key.GetType ())
							keyType = typeof (object);
					if (entry.Value != null)
						if (valueType == null)
							valueType = entry.Value.GetType ();
						else if (valueType != entry.Value.GetType ())
							valueType = typeof (object);
				}
				if ((keyType != null) && (valueType != null)) {
					genDict = Activator.CreateInstance (typeof (Dictionary<,>).MakeGenericType (keyType, valueType)) as IDictionary;
					foreach (DictionaryEntry entry in dict)
						genDict [ConvertToGeneric (entry.Key)] = ConvertToGeneric (entry.Value);
					return genDict;
				}
			} else if (list != null) {
				foreach (object obj in list)
					if (obj != null)
						if (valueType == null)
							valueType = obj.GetType ();
						else if (valueType != obj.GetType ())
							valueType = typeof (object);
				if (valueType != null) {
					genList = Activator.CreateInstance (typeof (List<>).MakeGenericType (valueType)) as IList;
					foreach (object obj in list)
						genList.Add (ConvertToGeneric (obj));
					return genList;
				}
			}
			return v;
		}

		public static object JsonDecode (string json) {
			return JsonDecode (json, null);
		}

		public static T JsonDecode<T> (string json)
			where T : class, ISerializable {
			return JsonDecode (json, typeof (T)) as T;
		}

		/// <summary>
		/// Parses the string json into a value
		/// </summary>
		/// <param name="json">A JSON string.</param>
		/// <returns>An ArrayList, a Hashtable, a double, a string, null, true, or false</returns>
		public static object JsonDecode (string json, Type returnType) {
			bool ordered;
			ConstructorInfo ctor;
			IDictionary dict;
			SerializationInfo info;
			// save the string for debug information
			JSON.instance.lastDecode = json;
			object val;
			if (json != null) {
				char [] charArray = json.ToCharArray ();
				int index = 0;
				bool success = true;
				ordered = JSON.instance.ordered;
				if (JSON.instance.ordered = (returnType == typeof (OrderedDictionary)))
					returnType = null;
				object value = JSON.instance.ParseValue (charArray, ref index, ref success);
				JSON.instance.ordered = ordered;
				if (success) {
					JSON.instance.lastErrorIndex = -1;
				} else {
					JSON.instance.lastErrorIndex = index;
				}
				if ((returnType != null) && ((dict = value as IDictionary) != null) && ((ctor = returnType.GetConstructor (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type [] { typeof (SerializationInfo), typeof (StreamingContext) }, null)) != null)) {
					info = new SerializationInfo (returnType, new FormatterConverter ());
					foreach (DictionaryEntry entry in dict)
						if ((entry.Key != null) && (entry.Value != null) && ((val = ConvertToGeneric (entry.Value)) != null))
							info.AddValue (entry.Key.ToString (), val, val.GetType ());
					return ctor.Invoke (new object [] { info, new StreamingContext (StreamingContextStates.Persistence) });
				}
				return value;
			} else {
				return null;
			}
		}

		/// <summary>
		/// Converts a Hashtable / ArrayList object into a JSON string
		/// </summary>
		/// <param name="json">A Hashtable / ArrayList</param>
		/// <returns>A JSON encoded string, or null if object 'json' is not serializable</returns>
		public static string JsonEncode (object json) {
			StringBuilder builder = new StringBuilder ();
			bool success = JSON.instance.SerializeValue (json, builder);
			return (success ? builder.ToString () : null);
		}

		/// <summary>
		/// On decoding, this function returns the position at which the parse failed (-1 = no error).
		/// </summary>
		/// <returns></returns>
		public static bool LastDecodeSuccessful () {
			return (JSON.instance.lastErrorIndex == -1);
		}

		/// <summary>
		/// On decoding, this function returns the position at which the parse failed (-1 = no error).
		/// </summary>
		/// <returns></returns>
		public static int GetLastErrorIndex () {
			return JSON.instance.lastErrorIndex;
		}

		/// <summary>
		/// If a decoding error occurred, this function returns a piece of the JSON string 
		/// at which the error took place. To ease debugging.
		/// </summary>
		/// <returns></returns>
		public static string GetLastErrorSnippet () {
			if (JSON.instance.lastErrorIndex == -1) {
				return "";
			} else {
				int startIndex = JSON.instance.lastErrorIndex - 5;
				int endIndex = JSON.instance.lastErrorIndex + 15;
				if (startIndex < 0) {
					startIndex = 0;
				}
				if (endIndex >= JSON.instance.lastDecode.Length) {
					endIndex = JSON.instance.lastDecode.Length - 1;
				}

				return JSON.instance.lastDecode.Substring (startIndex, endIndex - startIndex + 1);
			}
		}

		protected IDictionary ParseObject (char [] json, ref int index) {
			IDictionary table = (ordered) ? new OrderedDictionary () : (IDictionary) new Hashtable ();
			int token;

			// {
			NextToken (json, ref index);

			bool done = false;
			while (!done) {
				token = LookAhead (json, index);
				if (token == JSON.TOKEN_NONE) {
					return null;
				} else if (token == JSON.TOKEN_COMMA) {
					NextToken (json, ref index);
				} else if (token == JSON.TOKEN_CURLY_CLOSE) {
					NextToken (json, ref index);
					return table;
				} else {

					// name
					string name = ParseString (json, ref index);
					if (name == null) {
						return null;
					}

					// :
					token = NextToken (json, ref index);
					if (token != JSON.TOKEN_COLON) {
						return null;
					}

					// value
					bool success = true;
					object value = ParseValue (json, ref index, ref success);
					if (!success) {
						return null;
					}

					table [name] = value;
				}
			}

			return table;
		}

		protected ArrayList ParseArray (char [] json, ref int index) {
			ArrayList array = new ArrayList ();

			// [
			NextToken (json, ref index);

			bool done = false;
			while (!done) {
				int token = LookAhead (json, index);
				if (token == JSON.TOKEN_NONE) {
					return null;
				} else if (token == JSON.TOKEN_COMMA) {
					NextToken (json, ref index);
				} else if (token == JSON.TOKEN_SQUARED_CLOSE) {
					NextToken (json, ref index);
					break;
				} else {
					bool success = true;
					object value = ParseValue (json, ref index, ref success);
					if (!success) {
						return null;
					}

					array.Add (value);
				}
			}

			return array;
		}

		protected object ParseValue (char [] json, ref int index, ref bool success) {
			switch (LookAhead (json, index)) {
				case JSON.TOKEN_STRING:
					return ParseString (json, ref index);
				case JSON.TOKEN_NUMBER:
					return ParseNumber (json, ref index);
				case JSON.TOKEN_CURLY_OPEN:
					return ParseObject (json, ref index);
				case JSON.TOKEN_SQUARED_OPEN:
					return ParseArray (json, ref index);
				case JSON.TOKEN_TRUE:
					NextToken (json, ref index);
					return Boolean.Parse ("TRUE");
				case JSON.TOKEN_FALSE:
					NextToken (json, ref index);
					return Boolean.Parse ("FALSE");
				case JSON.TOKEN_NULL:
					NextToken (json, ref index);
					return null;
				case JSON.TOKEN_NONE:
					break;
			}

			success = false;
			return null;
		}

		protected string ParseString (char [] json, ref int index) {
			string s = "";
			char c;

			EatWhitespace (json, ref index);

			// "
			c = json [index++];

			bool complete = false;
			while (!complete) {

				if (index == json.Length) {
					break;
				}

				c = json [index++];
				if (c == '"') {
					complete = true;
					break;
				} else if (c == '\\') {

					if (index == json.Length) {
						break;
					}
					c = json [index++];
					if (c == '"') {
						s += '"';
					} else if (c == '\\') {
						s += '\\';
					} else if (c == '/') {
						s += '/';
					} else if (c == 'b') {
						s += '\b';
					} else if (c == 'f') {
						s += '\f';
					} else if (c == 'n') {
						s += '\n';
					} else if (c == 'r') {
						s += '\r';
					} else if (c == 't') {
						s += '\t';
					} else if (c == 'u') {
						int remainingLength = json.Length - index;
						if (remainingLength >= 4) {
							// fetch the next 4 chars
							char [] unicodeCharArray = new char [4];
							Array.Copy (json, index, unicodeCharArray, 0, 4);
							// parse the 32 bit hex into an integer codepoint
							uint codePoint = UInt32.Parse (new string (unicodeCharArray), NumberStyles.HexNumber);
							// convert the integer codepoint to a unicode char and add to string
							s += Char.ConvertFromUtf32 ((int) codePoint);
							// skip 4 chars
							index += 4;
						} else {
							break;
						}
					}

				} else {
					s += c;
				}

			}

			if (!complete) {
				return null;
			}

			return s;
		}

		protected object ParseNumber (char [] json, ref int index) {
			string s;
			int i;
			double dd;
			EatWhitespace (json, ref index);

			int lastIndex = GetLastIndexOfNumber (json, index);
			int charLength = (lastIndex - index) + 1;
			char [] numberCharArray = new char [charLength];

			Array.Copy (json, index, numberCharArray, 0, charLength);
			index = lastIndex + 1;
			if (int.TryParse (s = new string (numberCharArray), NumberStyles.Any, CultureInfo.InvariantCulture, out i))
				return i;
			else if (double.TryParse (s, NumberStyles.Any, CultureInfo.InvariantCulture, out dd))
				return dd;
			else
				return decimal.Parse (s, CultureInfo.InvariantCulture);
		}

		protected int GetLastIndexOfNumber (char [] json, int index) {
			int lastIndex;
			for (lastIndex = index; lastIndex < json.Length; lastIndex++) {
				if ("0123456789+-.eE".IndexOf (json [lastIndex]) == -1) {
					break;
				}
			}
			return lastIndex - 1;
		}

		protected void EatWhitespace (char [] json, ref int index) {
			for (; index < json.Length; index++) {
				if (" \t\n\r".IndexOf (json [index]) == -1) {
					break;
				}
			}
		}

		protected int LookAhead (char [] json, int index) {
			int saveIndex = index;
			return NextToken (json, ref saveIndex);
		}

		protected int NextToken (char [] json, ref int index) {
			EatWhitespace (json, ref index);

			if (index == json.Length) {
				return JSON.TOKEN_NONE;
			}

			char c = json [index];
			index++;
			switch (c) {
				case '{':
					return JSON.TOKEN_CURLY_OPEN;
				case '}':
					return JSON.TOKEN_CURLY_CLOSE;
				case '[':
					return JSON.TOKEN_SQUARED_OPEN;
				case ']':
					return JSON.TOKEN_SQUARED_CLOSE;
				case ',':
					return JSON.TOKEN_COMMA;
				case '"':
					return JSON.TOKEN_STRING;
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case '-':
					return JSON.TOKEN_NUMBER;
				case ':':
					return JSON.TOKEN_COLON;
			}
			index--;

			int remainingLength = json.Length - index;

			// false
			if (remainingLength >= 5) {
				if (json [index] == 'f' &&
					json [index + 1] == 'a' &&
					json [index + 2] == 'l' &&
					json [index + 3] == 's' &&
					json [index + 4] == 'e') {
					index += 5;
					return JSON.TOKEN_FALSE;
				}
			}

			// true
			if (remainingLength >= 4) {
				if (json [index] == 't' &&
					json [index + 1] == 'r' &&
					json [index + 2] == 'u' &&
					json [index + 3] == 'e') {
					index += 4;
					return JSON.TOKEN_TRUE;
				}
			}

			// null
			if (remainingLength >= 4) {
				if (json [index] == 'n' &&
					json [index + 1] == 'u' &&
					json [index + 2] == 'l' &&
					json [index + 3] == 'l') {
					index += 4;
					return JSON.TOKEN_NULL;
				}
			}

			return JSON.TOKEN_NONE;
		}

		protected bool SerializeObjectOrArray (object objectOrArray, StringBuilder builder) {
			if (objectOrArray is IDictionary) {
				return SerializeObject ((IDictionary) objectOrArray, builder);
			} else if (objectOrArray is IList) {
				return SerializeArray ((IList) objectOrArray, builder);
			} else if (objectOrArray is ISerializable) {
				return SerializeSerializable ((ISerializable) objectOrArray, builder);
			} else {
				return false;
			}
		}

		protected bool SerializeSerializable (ISerializable obj, StringBuilder builder) {
			IDictionary ht = ordered ? new OrderedDictionary () : (IDictionary) new Hashtable ();
			SerializationInfo info = new SerializationInfo (obj.GetType (), new FormatterConverter ());
			obj.GetObjectData (info, new StreamingContext (StreamingContextStates.Persistence));
			foreach (SerializationEntry entry in info)
				ht [entry.Name] = entry.Value;
			return SerializeObject (ht, builder);
		}

		protected bool SerializePair (object kvp, StringBuilder builder) {
			Hashtable ht = new Hashtable (2);
			ht ["k"] = kvp.GetType ().GetProperty ("Key").GetValue (kvp, null);
			ht ["v"] = kvp.GetType ().GetProperty ("Value").GetValue (kvp, null);
			return SerializeObject (ht, builder);
		}

		protected bool SerializeObject (IDictionary anObject, StringBuilder builder) {
			builder.Append ("{");

			IDictionaryEnumerator e = anObject.GetEnumerator ();
			bool first = true;
			while (e.MoveNext ()) {
				string key = e.Key.ToString ();
				object value = e.Value;

				if (!first) {
					builder.Append (",");
				}

				SerializeString (key, builder);
				builder.Append (":");
				if (!SerializeValue (value, builder)) {
					return false;
				}

				first = false;
			}

			builder.Append ("}");
			return true;
		}

		protected bool SerializeArray (IList anArray, StringBuilder builder) {
			builder.Append ("[");

			bool first = true;
			for (int i = 0; i < anArray.Count; i++) {
				object value = anArray [i];

				if (!first) {
					builder.Append (",");
				}

				if (!SerializeValue (value, builder)) {
					return false;
				}

				first = false;
			}

			builder.Append ("]");
			return true;
		}

		protected bool SerializeValue (object value, StringBuilder builder) {
			if (value is string) {
				SerializeString ((string) value, builder);
			} else if (value is IDictionary) {
				SerializeObject ((IDictionary) value, builder);
			} else if (value is IList) {
				SerializeArray ((IList) value, builder);
			} else if (value is ISerializable) {
				SerializeSerializable ((ISerializable) value, builder);
			} else if (IsNumeric (value)) {
				SerializeNumber (Convert.ToDouble (value), builder);
			} else if ((value is Boolean) && ((Boolean) value == true)) {
				builder.Append ("true");
			} else if ((value is Boolean) && ((Boolean) value == false)) {
				builder.Append ("false");
			} else if (value is Enum)
				SerializeString (value.ToString (), builder);
			else if (value == null) {
				builder.Append ("null");
			} else if (value.GetType ().FullName.StartsWith ("System.Collections.Generic.KeyValuePair`2[[")) {
				SerializePair (value, builder);
			} else {
				return false;
			}
			return true;
		}

		protected void SerializeString (string aString, StringBuilder builder) {
			builder.Append ("\"");

			char [] charArray = aString.ToCharArray ();
			for (int i = 0; i < charArray.Length; i++) {
				char c = charArray [i];
				if (c == '"') {
					builder.Append ("\\\"");
				} else if (c == '\\') {
					builder.Append ("\\\\");
				} else if (c == '\b') {
					builder.Append ("\\b");
				} else if (c == '\f') {
					builder.Append ("\\f");
				} else if (c == '\n') {
					builder.Append ("\\n");
				} else if (c == '\r') {
					builder.Append ("\\r");
				} else if (c == '\t') {
					builder.Append ("\\t");
				} else {
					int codepoint = Convert.ToInt32 (c);
					if ((codepoint >= 32) && (codepoint <= 126)) {
						builder.Append (c);
					} else {
						builder.Append ("\\u" + Convert.ToString (codepoint, 16).PadLeft (4, '0'));
					}
				}
			}

			builder.Append ("\"");
		}

		protected void SerializeNumber (double number, StringBuilder builder) {
			builder.Append (Convert.ToString (number, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Determines if a given object is numeric in any way
		/// (can be integer, double, etc). C# has no pretty way to do this.
		/// </summary>
		protected bool IsNumeric (object o) {
			decimal d1;
			double d2;
			return ((o != null) && (decimal.TryParse (o.ToString (), out d1) || double.TryParse (o.ToString (), out d2)));
		}

	}

}
