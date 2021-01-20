using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpeniT.SMTP.Web.Helpers
{
	public static class StringHelper
	{
		public static string CapitalizeFirstLetter(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
				return str;
			else if (str.Length == 1)
				return char.ToUpper(str[0]).ToString();
			else
				return char.ToUpper(str[0]) + str.Substring(1);
		}

		public static string ToUrlParam(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
				return str;
			else
				return System.Web.HttpUtility.UrlEncode(str).Replace("%252b", "%25252b").Replace("%2b", "%252b");
		}

		public static string UrlDecode(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
				return str;
			else
				return System.Web.HttpUtility.UrlDecode(str);
		}

		public static int GetHashInt(this string str)
		{
			try
			{
				System.Security.Cryptography.MD5 md5Hasher = System.Security.Cryptography.MD5.Create();
				var hashed = md5Hasher.ComputeHash(System.Text.Encoding.UTF8.GetBytes(str));
				var intHash = BitConverter.ToInt32(hashed, 0);

				return intHash;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return str.GetHashCode();
		}

		public static string ReadMore(this string str, int startIndex, int length)
		{
			if (string.IsNullOrWhiteSpace(str))
				return str;
			else if (str.Length <= length)
				return str;
			else
				return str.Substring(startIndex, length) + "...";
		}

		public static string Highlight(this string str, string highlightedStr, int? maxLength = null)
		{
			if (string.IsNullOrWhiteSpace(str) || string.IsNullOrWhiteSpace(highlightedStr))
			{
				return maxLength == null ? str : str.ReadMore(0, maxLength.Value);
			}
			else
			{
				var firstReplacedIndex = str.IndexOf(highlightedStr, StringComparison.CurrentCultureIgnoreCase);
				var highlightedRegex = new Regex(highlightedStr, RegexOptions.IgnoreCase);
				str = highlightedRegex.Replace(str, new MatchEvaluator(m => $"<b>{m.Value}</b>"));

				if (maxLength == null)
				{
					return str;
				}
				else
				{
					if (str.Length <= maxLength.Value)
					{
						return str;
					}
					else
					{
						if (firstReplacedIndex > maxLength.Value)
						{
							var start = firstReplacedIndex - (maxLength.Value / 2);
							var end = firstReplacedIndex + (maxLength.Value / 2);

							if (end > str.Length)
							{
								var excessEnd = end - str.Length;
								end = end - excessEnd;
								start = start - excessEnd;
							}

							if (start != 0 && end == str.Length)
							{
								return "..." + str.Substring(start, end - start);
							}
							else
							{
								return "..." + str.Substring(start, end - start) + "...";
							}
						}
						else
						{
							return str.Substring(0, maxLength.Value) + "...";
						}
					}
				}
			}
		}

		public static string GetFileName(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
				return str;
			else
				return string.Join(".", str.Split(".").SkipLast(1));
		}

		public static string GetFileExtension(this string str)
		{
			if (string.IsNullOrWhiteSpace(str))
				return str;
			else
				return str.Split(".").Last();
		}

		public static int? ToNullableInt(this string str)
		{
			int i;
			if (int.TryParse(str, out i)) return i;
			return null;
		}

		public static bool TextEditorEmpty(this string str)
		{
			if (str == null || str.Length == 0)
				return true;
			else if (str.Equals("<p><br></p>") || str.Equals("<p><br /></p>") || str.Equals("<p><br/></p>"))
				return true;
			else
				return false;
		}

		public static int NullableCompareTo(this string str1, string str2)
		{
			if (str1 == null && str2 == null)
			{
				return 0;
			}
			else if ((str1 == null && str2 != null) || (str1 != null && str2 == null))
			{
				return -1;
			}
			else
			{
				return str1.CompareTo(str2);
			}
		}

		public static bool HasUpperCase(this string str)
		{
			if (string.IsNullOrWhiteSpace(str)) return false;

			foreach (var char_ in str.ToCharArray())
			{
				if (char.IsUpper(char_))
				{
					return true;
				}
			}
			return false;
		}

		public static bool HasLowerCase(this string str)
		{
			if (string.IsNullOrWhiteSpace(str)) return false;

			foreach (var char_ in str.ToCharArray())
			{
				if (char.IsLower(char_))
				{
					return true;
				}
			}
			return false;
		}

		public static bool HasDigit(this string str)
		{
			if (string.IsNullOrWhiteSpace(str)) return false;

			foreach (var char_ in str.ToCharArray())
			{
				if (char.IsDigit(char_))
				{
					return true;
				}
			}
			return false;
		}

		public static string Pluralize(this string text, int number = 2)
		{
			if (number == 1)
			{
				return text;
			}
			else
			{
				// Create a dictionary of exceptions that have to be checked first
				// This is very much not an exhaustive list!
				Dictionary<string, string> exceptions = new Dictionary<string, string>() {
				{ "man", "men" },
				{ "woman", "women" },
				{ "child", "children" },
				{ "tooth", "teeth" },
				{ "foot", "feet" },
				{ "mouse", "mice" },
				{ "belief", "beliefs" } };

				if (exceptions.ContainsKey(text.ToLowerInvariant()))
				{
					return exceptions[text.ToLowerInvariant()];
				}
				else if (text.EndsWith("y", StringComparison.OrdinalIgnoreCase) &&
					!text.EndsWith("ay", StringComparison.OrdinalIgnoreCase) &&
					!text.EndsWith("ey", StringComparison.OrdinalIgnoreCase) &&
					!text.EndsWith("iy", StringComparison.OrdinalIgnoreCase) &&
					!text.EndsWith("oy", StringComparison.OrdinalIgnoreCase) &&
					!text.EndsWith("uy", StringComparison.OrdinalIgnoreCase))
				{
					return text.Substring(0, text.Length - 1) + "ies";
				}
				else if (text.EndsWith("us", StringComparison.InvariantCultureIgnoreCase))
				{
					// http://en.wikipedia.org/wiki/Plural_form_of_words_ending_in_-us
					return text + "es";
				}
				else if (text.EndsWith("ss", StringComparison.InvariantCultureIgnoreCase))
				{
					return text + "es";
				}
				else if (text.EndsWith("s", StringComparison.InvariantCultureIgnoreCase))
				{
					return text;
				}
				else if (text.EndsWith("x", StringComparison.InvariantCultureIgnoreCase) ||
					text.EndsWith("ch", StringComparison.InvariantCultureIgnoreCase) ||
					text.EndsWith("sh", StringComparison.InvariantCultureIgnoreCase))
				{
					return text + "es";
				}
				else if (text.EndsWith("f", StringComparison.InvariantCultureIgnoreCase) && text.Length > 1)
				{
					return text.Substring(0, text.Length - 1) + "ves";
				}
				else if (text.EndsWith("fe", StringComparison.InvariantCultureIgnoreCase) && text.Length > 2)
				{
					return text.Substring(0, text.Length - 2) + "ves";
				}
				else
				{
					return text + "s";
				}
			}
		}
	}
}
