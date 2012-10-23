/* Playtomic Unity3d API
-----------------------------------------------------------------------
 Documentation is available at: 
 	https://playtomic.com/api/unity

 Support is available at:
 	https://playtomic.com/community 
 	https://playtomic.com/issues
 	https://playtomic.com/support has more options if you're a premium user

 Github repositories:
 	https://github.com/playtomic

You may modify this SDK if you wish but be kind to our servers.  Be
careful about modifying the analytics stuff as it may give you 
borked reports.

Pull requests are welcome if you spot a bug or know a more efficient
way to implement something.

Copyright (c) 2011 Playtomic Inc.  Playtomic APIs and SDKs are licensed 
under the MIT license.  Certain portions may come from 3rd parties and 
carry their own licensing terms and are referenced where applicable.
*/ 
#define WWW_SUPPORT
#if UNITY_FLASH
#undef WWW_SUPPORT
#endif
#if WWW_SUPPORT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

// Unity doesn't support namespaces...
//namespace Procurios.Public
//{
	/// <summary>
	/// This class encodes and decodes Playtomic_JSON strings.
	/// Spec. details, see http://www.json.org/
	/// 
	/// Playtomic_JSON uses Arrays and Objects. These correspond here to the datatypes List<object> and Dictionary<string,object>.
	/// All numbers are parsed to doubles.
	/// </summary>
	public class Playtomic_JSON
	{
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

		private const int BUILDER_CAPACITY = 2000;

		static Playtomic_JSON instance = new Playtomic_JSON();

		/// <summary>
		/// On decoding, this value holds the position at which the parse failed (-1 = no error).
		/// </summary>
		protected int lastErrorIndex = -1;
		protected string lastDecode = "";

		/// <summary>
		/// Parses the string json into a value
		/// </summary>
		/// <param name="json">A Playtomic_JSON string.</param>
		/// <returns>A List<object>, a Dictionary<string,object>, a double, a string, null, true, or false</returns>
		public static object JsonDecode(string json)
		{
			// save the string for debug information
			Playtomic_JSON.instance.lastDecode = json;

			if (json != null) {
                char[] charArray = json.ToCharArray();
                int index = 0;
				bool success = true;
				object value = Playtomic_JSON.instance.ParseValue(charArray, ref index, ref success);
				if (success) {
					Playtomic_JSON.instance.lastErrorIndex = -1;
				} else {
					Playtomic_JSON.instance.lastErrorIndex = index;
				}
				return value;
            } else {
                return null;
            }
		}

		/// <summary>
		/// Converts a Dictionary<string,object> / List<object> object into a Playtomic_JSON string
		/// </summary>
		/// <param name="json">A Dictionary<string,object> / List<object></param>
		/// <returns>A Playtomic_JSON encoded string, or null if object 'json' is not serializable</returns>
		public static string JsonEncode(object json)
		{
			StringBuilder builder = new StringBuilder(BUILDER_CAPACITY);
			bool success = Playtomic_JSON.instance.SerializeValue(json, builder);
			return (success ? builder.ToString() : null);
		}

		/// <summary>
		/// On decoding, this function returns the position at which the parse failed (-1 = no error).
		/// </summary>
		/// <returns></returns>
		public static bool LastDecodeSuccessful()
		{
			return (Playtomic_JSON.instance.lastErrorIndex == -1);
		}

		/// <summary>
		/// On decoding, this function returns the position at which the parse failed (-1 = no error).
		/// </summary>
		/// <returns></returns>
		public static int GetLastErrorIndex()
		{
			return Playtomic_JSON.instance.lastErrorIndex;
		}

		/// <summary>
		/// If a decoding error occurred, this function returns a piece of the Playtomic_JSON string 
		/// at which the error took place. To ease debugging.
		/// </summary>
		/// <returns></returns>
		public static string GetLastErrorSnippet()
		{
			if (Playtomic_JSON.instance.lastErrorIndex == -1) {
				return "";
			} else {
				int startIndex = Playtomic_JSON.instance.lastErrorIndex - 5;
				int endIndex = Playtomic_JSON.instance.lastErrorIndex + 15;
				if (startIndex < 0) {
					startIndex = 0;
				}
				if (endIndex >= Playtomic_JSON.instance.lastDecode.Length) {
					endIndex = Playtomic_JSON.instance.lastDecode.Length - 1;
				}

				return Playtomic_JSON.instance.lastDecode.Substring(startIndex, endIndex - startIndex + 1);
			}
		}

		protected Dictionary<string, object> ParseObject(char[] json, ref int index)
		{
			Dictionary<string, object> table = new Dictionary<string, object>();
			int token;

			// {
			NextToken(json, ref index);

			bool done = false;
			while (!done) {
				token = LookAhead(json, index);
				if (token == Playtomic_JSON.TOKEN_NONE) {
					return null;
				} else if (token == Playtomic_JSON.TOKEN_COMMA) {
					NextToken(json, ref index);
				} else if (token == Playtomic_JSON.TOKEN_CURLY_CLOSE) {
					NextToken(json, ref index);
					return table;
				} else {

					// name
					string name = ParseString(json, ref index);
					if (name == null) {
						return null;
					}

					// :
					token = NextToken(json, ref index);
					if (token != Playtomic_JSON.TOKEN_COLON) {
						return null;
					}

					// value
					bool success = true;
					object value = ParseValue(json, ref index, ref success);
					if (!success) {
						return null;
					}

					table[name] = value;
				}
			}

			return table;
		}

		protected List<object> ParseArray(char[] json, ref int index)
		{
			List<object> array = new List<object>();

			// [
			NextToken(json, ref index);

			bool done = false;
			while (!done) {
				int token = LookAhead(json, index);
				if (token == Playtomic_JSON.TOKEN_NONE) {
					return null;
				} else if (token == Playtomic_JSON.TOKEN_COMMA) {
					NextToken(json, ref index);
				} else if (token == Playtomic_JSON.TOKEN_SQUARED_CLOSE) {
					NextToken(json, ref index);
					break;
				} else {
					bool success = true;
					object value = ParseValue(json, ref index, ref success);
					if (!success) {
						return null;
					}

					array.Add(value);
				}
			}

			return array;
		}

		protected object ParseValue(char[] json, ref int index, ref bool success)
		{
			switch (LookAhead(json, index)) {
				case Playtomic_JSON.TOKEN_STRING:
					return ParseString(json, ref index);
				case Playtomic_JSON.TOKEN_NUMBER:
					return ParseNumber(json, ref index);
				case Playtomic_JSON.TOKEN_CURLY_OPEN:
					return ParseObject(json, ref index);
				case Playtomic_JSON.TOKEN_SQUARED_OPEN:
					return ParseArray(json, ref index);
				case Playtomic_JSON.TOKEN_TRUE:
					NextToken(json, ref index);
					return Boolean.Parse("TRUE");
				case Playtomic_JSON.TOKEN_FALSE:
					NextToken(json, ref index);
					return Boolean.Parse("FALSE");
				case Playtomic_JSON.TOKEN_NULL:
					NextToken(json, ref index);
					return null;
				case Playtomic_JSON.TOKEN_NONE:
					break;
			}

			success = false;
			return null;
		}

		protected string ParseString(char[] json, ref int index)
		{
			string s = "";
			char c;

			EatWhitespace(json, ref index);
			
			// "
			c = json[index++];

			bool complete = false;
			while (!complete) {

				if (index == json.Length) {
					break;
				}

				c = json[index++];
				if (c == '"') {
					complete = true;
					break;
				} else if (c == '\\') {

					if (index == json.Length) {
						break;
					}
					c = json[index++];
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
							char[] unicodeCharArray = new char[4];
							Array.Copy(json, index, unicodeCharArray, 0, 4);
							// parse the 32 bit hex into an integer codepoint
							uint codePoint = UInt32.Parse(new string(unicodeCharArray), NumberStyles.HexNumber);
							// convert the integer codepoint to a unicode char and add to string
							s += Char.ConvertFromUtf32((int)codePoint);
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

		protected double ParseNumber(char[] json, ref int index)
		{
			EatWhitespace(json, ref index);

			int lastIndex = GetLastIndexOfNumber(json, index);
			int charLength = (lastIndex - index) + 1;
			char[] numberCharArray = new char[charLength];

			Array.Copy(json, index, numberCharArray, 0, charLength);
			index = lastIndex + 1;
			return Double.Parse(new string(numberCharArray), CultureInfo.InvariantCulture);
		}

		protected int GetLastIndexOfNumber(char[] json, int index)
		{
			int lastIndex;
			for (lastIndex = index; lastIndex < json.Length; lastIndex++) {
				if ("0123456789+-.eE".IndexOf(json[lastIndex]) == -1) {
					break;
				}
			}
			return lastIndex - 1;
		}

		protected void EatWhitespace(char[] json, ref int index)
		{
			for (; index < json.Length; index++) {
				if (" \t\n\r".IndexOf(json[index]) == -1) {
					break;
				}
			}
		}

		protected int LookAhead(char[] json, int index)
		{
			int saveIndex = index;
			return NextToken(json, ref saveIndex);
		}

		protected int NextToken(char[] json, ref int index)
		{
			EatWhitespace(json, ref index);

			if (index == json.Length) {
				return Playtomic_JSON.TOKEN_NONE;
			}
			
			char c = json[index];
			index++;
			switch (c) {
				case '{':
					return Playtomic_JSON.TOKEN_CURLY_OPEN;
				case '}':
					return Playtomic_JSON.TOKEN_CURLY_CLOSE;
				case '[':
					return Playtomic_JSON.TOKEN_SQUARED_OPEN;
				case ']':
					return Playtomic_JSON.TOKEN_SQUARED_CLOSE;
				case ',':
					return Playtomic_JSON.TOKEN_COMMA;
				case '"':
					return Playtomic_JSON.TOKEN_STRING;
				case '0': case '1': case '2': case '3': case '4': 
				case '5': case '6': case '7': case '8': case '9':
				case '-': 
					return Playtomic_JSON.TOKEN_NUMBER;
				case ':':
					return Playtomic_JSON.TOKEN_COLON;
			}
			index--;

			int remainingLength = json.Length - index;

			// false
			if (remainingLength >= 5) {
				if (json[index] == 'f' &&
					json[index + 1] == 'a' &&
					json[index + 2] == 'l' &&
					json[index + 3] == 's' &&
					json[index + 4] == 'e') {
					index += 5;
					return Playtomic_JSON.TOKEN_FALSE;
				}
			}

			// true
			if (remainingLength >= 4) {
				if (json[index] == 't' &&
					json[index + 1] == 'r' &&
					json[index + 2] == 'u' &&
					json[index + 3] == 'e') {
					index += 4;
					return Playtomic_JSON.TOKEN_TRUE;
				}
			}

			// null
			if (remainingLength >= 4) {
				if (json[index] == 'n' &&
					json[index + 1] == 'u' &&
					json[index + 2] == 'l' &&
					json[index + 3] == 'l') {
					index += 4;
					return Playtomic_JSON.TOKEN_NULL;
				}
			}

			return Playtomic_JSON.TOKEN_NONE;
		}

		protected bool SerializeObjectOrArray(object objectOrArray, StringBuilder builder)
		{
			if (objectOrArray is Dictionary<string,object>) {
				return SerializeObject((Dictionary<string, object>)objectOrArray, builder);
			} else if (objectOrArray is List<object>) {
				return SerializeArray((List<object>)objectOrArray, builder);
			} else {
				return false;
			}
		}

		protected bool SerializeObject(Dictionary<string, object> anObject, StringBuilder builder)
		{
			builder.Append("{");
			
			bool first = true;
			foreach (KeyValuePair<string, object> e in anObject) {
				string key = e.Key.ToString();
				object value = e.Value;

				if (!first) {
					builder.Append(", ");
				}

				SerializeString(key, builder);
				builder.Append(":");
				if (!SerializeValue(value, builder)) {
					return false;
				}

				first = false;
			}

			builder.Append("}");
			return true;
		}

		protected bool SerializeArray(List<object> anArray, StringBuilder builder)
		{
			builder.Append("[");

			bool first = true;
			foreach (object value in anArray) {
				if (!first) {
					builder.Append(", ");
				}

				if (!SerializeValue(value, builder)) {
					return false;
				}

				first = false;
			}

			builder.Append("]");
			return true;
		}

		protected bool SerializeValue(object value, StringBuilder builder)
		{
			if (value is string) {
				SerializeString((string)value, builder);
			} else if (value is Dictionary<string, object>) {
				SerializeObject((Dictionary<string, object>)value, builder);
			} else if (value is List<object>) {
				SerializeArray((List<object>)value, builder);
			} else if (IsNumeric(value)) {
				SerializeNumber(Convert.ToDouble(value), builder);
			} else if ((value is Boolean) && ((Boolean)value == true)) {
				builder.Append("true");
			} else if ((value is Boolean) && ((Boolean)value == false)) {
				builder.Append("false");
			} else if (value == null) {
				builder.Append("null");
			} else {
				return false;
			}
			return true;
		}

		protected void SerializeString(string aString, StringBuilder builder)
		{
			builder.Append("\"");

			char[] charArray = aString.ToCharArray();
			for (int i = 0; i < charArray.Length; i++) {
				char c = charArray[i];
				if (c == '"') {
					builder.Append("\\\"");
				} else if (c == '\\') {
					builder.Append("\\\\");
				} else if (c == '\b') {
					builder.Append("\\b");
				} else if (c == '\f') {
					builder.Append("\\f");
				} else if (c == '\n') {
					builder.Append("\\n");
				} else if (c == '\r') {
					builder.Append("\\r");
				} else if (c == '\t') {
					builder.Append("\\t");
				} else {
					int codepoint = Convert.ToInt32(c);
					if ((codepoint >= 32) && (codepoint <= 126)) {
						builder.Append(c);
					} else {
						builder.Append("\\u" + Convert.ToString(codepoint, 16).PadLeft(4, '0'));
					}
				}
			}

			builder.Append("\"");
		}

		protected void SerializeNumber(double number, StringBuilder builder)
		{
			builder.Append(Convert.ToString(number, CultureInfo.InvariantCulture));
		}

		/// <summary>
		/// Determines if a given object is numeric in any way
		/// (can be integer, double, etc). C# has no pretty way to do this.
		/// </summary>
		protected bool IsNumeric(object o)
		{
			try {
				Double.Parse(o.ToString());
			} catch (Exception) {
				return false;
			}
			return true;
		}
	}
//}
#endif