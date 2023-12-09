using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Extensions
{
	/// <summary>
	/// 字符串杂项扩展方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class StringExtension
	{
		/// <summary>
		/// 判断给定字符串是否是以字母或下划线（'_'）开头，并且后续字符（如果有）是由字母、数字或下划线（'_'）组成
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>

		[ThreadSafe]
		public static bool IsSymbol(this string? text)
		{
			if (string.IsNullOrWhiteSpace(text))
				return false;

			char first = text[0];

			if (!char.IsLetter(first) && first != '_')
				return false;

			for (int i = text.Length - 1; i > 0; i--)
			{
				if (!char.IsLetterOrDigit(text[i]) && text[i] != '_')
					return false;
			}

			return true;
		}

		/// <summary>
		/// 将字符串分割为两部分，如果无法分割则返回(<see langword="null"/>, <see langword="null"/>)
		/// </summary>
		/// <param name="text"></param>
		/// <param name="delimiter"></param>
		/// <param name="ignoreEmpty"></param>
		/// <returns></returns>
		[ThreadSafe]
		public static (string?, string?) Split2(this string text, char delimiter, bool ignoreEmpty = false)
		{
			var options = ignoreEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

			if (text.Split(delimiter, 1, options) is [var item1, var item2])
				return (item1, item2);

			return (null, null);
		}

		/// <summary>
		/// 将字符串分割为两部分，如果无法分割则返回(<see langword="null"/>, <see langword="null"/>)
		/// </summary>
		/// <param name="text"></param>
		/// <param name="delimiter"></param>
		/// <param name="ignoreEmpty"></param>
		/// <returns></returns>
		[ThreadSafe]
		public static (string?, string?) Split2(this string text, string delimiter, bool ignoreEmpty = false)
		{
			var options = ignoreEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

			if (text.Split(delimiter, 1, options) is [var item1, var item2])
				return (item1, item2);

			return (null, null);
		}

		/// <summary>
		/// 将字符串分割为三部分，如果无法分割则返回(<see langword="null"/>, <see langword="null"/>, <see langword="null"/>)
		/// </summary>
		/// <param name="text"></param>
		/// <param name="delimiter"></param>
		/// <param name="ignoreEmpty"></param>
		/// <returns></returns>
		[ThreadSafe]
		public static (string?, string?, string?) Split3(this string text, char delimiter, bool ignoreEmpty = false)
		{
			var options = ignoreEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

			if (text.Split(delimiter, 2, options) is [var item1, var item2, var item3])
				return (item1, item2, item3);

			return (null, null, null);
		}

		/// <summary>
		/// 将字符串分割为三部分，如果无法分割则返回(<see langword="null"/>, <see langword="null"/>, <see langword="null"/>)
		/// </summary>
		/// <param name="text"></param>
		/// <param name="delimiter"></param>
		/// <param name="ignoreEmpty"></param>
		/// <returns></returns>
		[ThreadSafe]
		public static (string?, string?, string?) Split3(this string text, string delimiter, bool ignoreEmpty = false)
		{
			var options = ignoreEmpty ? StringSplitOptions.RemoveEmptyEntries : StringSplitOptions.None;

			if (text.Split(delimiter, 2, options) is [var item1, var item2, var item3])
				return (item1, item2, item3);

			return (null, null, null);
		}

		/// <summary>
		/// 在当前字符串中搜索给定字符出现的次数
		/// </summary>
		/// <param name="text"></param>
		/// <param name="delimiter"></param>
		/// <returns></returns>
		public static int CountOf(this string text, char delimiter)
		{
			int start = 0;
			int count = 0;

			while ((start = text.IndexOf(delimiter, start)) >= 0)
			{
				count++;
				start++;
			}

			return count;
		}

		/// <summary>
		/// 在当前字符串中搜索子字符串出现的次数<br/>
		/// 参数<paramref name="intersect"/>指定子串是否可以重叠<br/>
		/// 例如: 在 "aaaa" 中搜索 "aaa", 可以重叠则是2次 ([aaa]a, a[aaa]), 否则是1次 ([aaa]a)
		/// </summary>
		/// <param name="text"></param>
		/// <param name="delimiter"></param>
		/// <param name="intersect"></param>
		/// <returns></returns>
		public static int CountOf(this string text, string delimiter, bool intersect = false)
		{
			int skip = intersect ? 1 : delimiter.Length;
			int start = 0;
			int count = 0;

			while ((start = text.IndexOf(delimiter, start)) >= 0)
			{
				count++;
				start += skip;
			}

			return count;
		}

		/// <summary>
		/// 在当前字符串中搜索所有给定字符出现的次数
		/// </summary>
		/// <param name="text"></param>
		/// <param name="delimiters"></param>
		/// <returns></returns>
		public static int CountOfAny(this string text, params char[] delimiters)
		{
			int start = 0;
			int count = 0;

			while ((start = text.IndexOfAny(delimiters, start)) >= 0)
			{
				count++;
				start++;
			}

			return count;
		}
	}
}
