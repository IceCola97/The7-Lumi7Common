using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Text
{
	/// <summary>
	/// 解析简单转义字符串的实现
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadUnsafe]
	public static class Escape
	{
		/// <summary>
		/// 在给定文本中读取由特定字符包裹的字符串，支持'\'开始的单个转义字符<br/>
		/// 特定字符由<paramref name="input"/>[<paramref name="index"/>]指定，必须是单引号（'）或双引号（"）<br/>
		/// 参数<paramref name="escapeMap"/>指定转义字符的对应关系，键值对 ('n' => '\n') 代表将字符串中的"\\n"映射为"\n"<br/>
		/// 参数<paramref name="strict"/>指定未定义的转义字符应如何处理，为<see langword="true"/>则抛出异常，否则将忽略转义的'\'字符<br/>
		/// 请注意，自转义行为'\\'已经被定义，无需通过<paramref name="escapeMap"/>再次定义
		/// </summary>
		/// <param name="input"></param>
		/// <param name="index"></param>
		/// <param name="escapeMap"></param>
		/// <param name="strict"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		/// <exception cref="InvalidDataException"></exception>
		public static string ReadQuotedString(
			string input,
			ref int index,
			Dictionary<char, char>? escapeMap = null,
			bool strict = false
		)
		{
			if (index < 0 || index >= input.Length)
				throw new ArgumentOutOfRangeException(nameof(index));

			int start = index;
			var builder = new StringBuilder();
			char quoteChar = input[index];
			bool escaping = false;

			if (quoteChar != '"' && quoteChar != '\'')
				throw new NotSupportedException(ET("起始字符应该是单引号或双引号"));

			while (index < input.Length)
			{
				char ch = input[index];

				if (ch == quoteChar)
				{
					if (escaping)
						builder.Append(ch);
					else
						return builder.ToString();
				}
				else if (ch == '\\')
				{
					if (escaping)
						builder.Append(ch);
					else
						escaping = true;
				}
				else if (escaping)
				{
					if (escapeMap is not null && escapeMap.TryGetValue(ch, out var escaped))
						builder.Append(escaped);
					else if (strict)
						throw new InvalidDataException(ET("无效的转义符: {0}", ch));
					else
						builder.Append(ch);
				}
				else
					builder.Append(ch);

				index++;
			}

			throw new InvalidDataException(ET("字符串没有关闭(在 {0})", start));
		}
	}
}
