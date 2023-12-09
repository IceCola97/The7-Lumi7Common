using Lumi7Common.Extensions;
using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lumi7Common.Text
{
	/// <summary>
	/// 提供带分类的动态加载翻译功能
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public static class Translation
	{
		/// <summary>
		/// 指示异常消息分类
		/// </summary>
		public const string CATEGORY_EXCEPTION = "exception";

		/// <summary>
		/// 指定翻译时获取读锁的超时时间
		/// </summary>
		public const int TRANSLATE_TIMEOUT = 800;

		private static readonly Dictionary<char, char> m_EscapeChars = new()
		{
			{ '\'', '\'' },
			{ '"', '"' },
			{ 't', '\t' },
			{ 'n', '\n' },
			{ 'r', '\r' },
		};

		private static volatile CultureInfo m_CurrentLanguage;
		private static volatile bool m_Strict = false;

		private static readonly ReadWriteLock m_Lock = new();
		private static readonly TranslationMap m_Map = new();

		static Translation()
		{
			m_CurrentLanguage = CultureInfo.CurrentCulture;
		}

		/// <summary>
		/// 当前翻译所使用的语言
		/// </summary>
		public static string CurrentLanguage
		{
			get
			{
				return m_CurrentLanguage.Name;
			}
			set
			{
				var culture = ParseCulture(value)
					?? throw new CultureNotFoundException(ET("给定的语言名称未找到: {0}", value)
						+ string.Format("\nThe given language name was not found: {0}", value));

				m_CurrentLanguage = culture;
			}
		}

		/// <summary>
		/// 指示在当加载翻译文件出现无效翻译行、无效的引号字符串、行尾的额外字符及在不允许覆盖的情况下出现重复键时应该抛出异常(值为<see langword="true"/>时)还是忽略此行(值为<see langword="false"/>时)<br/>
		/// 此项在加载翻译文件的调用<see cref="Escape.ReadQuotedString(string, ref int, Dictionary{char, char}?, bool)"/>时会作为<paramref name="strict"/>参数传递
		/// </summary>
		public static bool Strict
		{
			get => m_Strict;
			set => m_Strict = value;
		}

		/// <summary>
		/// 根据给定的语言名称获取<see cref="CultureInfo"/>，如果不存在则返回<see langword="null"/>
		/// </summary>
		/// <param name="language"></param>
		/// <returns></returns>
		public static CultureInfo? ParseCulture(string language)
		{
			try
			{
				return CultureInfo.GetCultureInfo(language);
			}
			catch (CultureNotFoundException)
			{
				return null;
			}
		}

		private static string GetStandardName(string language)
		{
			return ParseCulture(language)?.Name ?? language;
		}

		/// <summary>
		/// 加载给定文本中的翻译文件数据<br/>
		/// 参数<paramref name="overwrite"/>指定是否允许重复定义一个键<br/>
		/// <br/>
		/// 翻译文件数据支持指定分类、引号字符串、注释文本<br/>
		/// 其中，要指定接下来所有翻译的分类名称，请使用: [分类名]<br/>
		/// 引号字符串(支持单引号与双引号两种)在键与值当中都可以使用，并且支持简单的转义<br/>
		/// 例如: "错误信息" = "error message" (等号两边的空格不是必须的)<br/>
		/// 例如: "类型 \"{0}\" 是无效的" = "Type \"{0}\" is invalid"<br/>
		/// 转义支持字符: \'(单引号)、\"(双引号)、\t(制表符)、\n(换行符)、\r(回车符)<br/>
		/// 如果需要注释，请在行开头标注 '#' 字符，如果翻译键需要 '#' 字符开头，请考虑使用引号字符串<br/>
		/// 例如: # 这是一行注释<br/>
		/// 例如: "#####有效翻译#####" = "#####Effective Translation#####"
		/// </summary>
		/// <param name="language"></param>
		/// <param name="reader"></param>
		/// <param name="overwrite"></param>
		/// <exception cref="InvalidDataException"></exception>
		public static void Load(string language, TextReader reader, bool overwrite = false)
		{
			string? line;
			Dictionary<string, Dictionary<string, string>> map = [];
			Dictionary<string, string>? category = null;

			language = GetStandardName(language);

			while ((line = reader.ReadLine()) is not null)
			{
				line = line.Trim();

				if (line.Length == 0 || line.StartsWith('#'))
					continue;

				if (line[0] == '[' && line[^1] == ']')
				{
					string label = line[1..^1];

					if (label.Length > 0 && !char.IsDigit(label[0])
						&& label.All(c => char.IsLetterOrDigit(c) || c == '_'))
					{
						if (!map.TryGetValue(label, out category))
							category = map[label] = [];
					}
					else
						throw new InvalidDataException(ET("无效的翻译分类名称: {0}", label));
				}
				else
				{
					if (!line.Contains('='))
					{
						if (m_Strict)
							throw new InvalidDataException(ET("无效的翻译条目: {0}", line));

						continue;
					}

					if (category == null)
						throw new InvalidDataException(ET("翻译条目出现前没有声明任何的翻译分类"));

					string key;
					string value;

					if (line[0] == '"' || line[0] == '\'')
					{
						int pos = 0;

						try
						{
							key = Escape.ReadQuotedString(line, ref pos, m_EscapeChars, m_Strict);
						}
						catch
						{
							if (m_Strict)
								throw;

							continue;
						}

						if (key.Length == 0)
							throw new InvalidDataException(ET("翻译条目的翻译目标(等号左侧)不能为空"));

						line = line[(pos + 1)..].TrimStart();

						if (line.Length == 0 || line[0] != '=')
						{
							if (m_Strict)
								throw new InvalidDataException(ET("无效的翻译条目: {0}", line));

							continue;
						}

						value = line[1..].TrimStart();
					}
					else
					{
						string[] pair = line.Split('=', 1);
						key = pair[0].TrimEnd();
						value = pair[1].TrimStart();

						if (key.Length == 0)
							throw new InvalidDataException(ET("翻译条目的翻译目标(等号左侧)不能为空"));
					}

					if (value[0] == '"' || value[0] == '\'')
					{
						string read;
						int pos = 0;

						try
						{
							read = Escape.ReadQuotedString(value, ref pos, m_EscapeChars, m_Strict);
						}
						catch
						{
							if (m_Strict)
								throw;

							continue;
						}

						string tail = value[(pos + 1)..].TrimStart();

						if (tail.Length != 0 && m_Strict)
							throw new InvalidDataException(ET("无效的追加文本: {0}", tail));

						value = read;
					}

					if (!category.TryAdd(key, value))
					{
						if (overwrite)
							category[key] = value;
						else if (m_Strict)
							throw new InvalidDataException(ET("重复的翻译目标(等号左侧): {0}", key));
					}
				}
			}

			using (m_Lock.UpgradeableRead())
			{
				if (!overwrite && m_Strict)
				{
					foreach (var categoryPair in map)
					{
						var textMap = m_Map.Category(language, categoryPair.Key);

						foreach (var textPair in categoryPair.Value)
						{
							if (textMap.ContainsKey(textPair.Key))
								throw new InvalidDataException(ET("重复的翻译目标(等号左侧): {0}", textPair.Key));
						}
					}
				}

				using (m_Lock.Write())
				{
					foreach (var categoryPair in map)
					{
						var textMap = m_Map.Category(language, categoryPair.Key);

						foreach (var textPair in categoryPair.Value)
						{
							if (overwrite)
								textMap[textPair.Key] = textPair.Value;
							else if (!textMap.ContainsKey(textPair.Key))
								textMap.Add(textPair.Key, textPair.Value);
						}
					}
				}
			}
		}

		/// <summary>
		/// <inheritdoc cref="Load(string, TextReader, bool)" path="/summary"/>
		/// </summary>
		/// <param name="language"></param>
		/// <param name="stream"></param>
		/// <param name="encoding"></param>
		/// <param name="overwrite"></param>
		public static void Load(string language, Stream stream, Encoding? encoding = null, bool overwrite = false)
		{
			encoding ??= Encoding.Default;
			Load(language, new StreamReader(stream,
				encoding: encoding,
				detectEncodingFromByteOrderMarks: true,
				leaveOpen: true
			), overwrite);
		}

		/// <summary>
		/// <inheritdoc cref="Load(string, TextReader, bool)" path="/summary"/>
		/// </summary>
		/// <param name="language"></param>
		/// <param name="fileName"></param>
		/// <param name="encoding"></param>
		/// <param name="overwrite"></param>
		public static void Load(string language, string fileName, Encoding? encoding = null, bool overwrite = false)
		{
			using FileStream fileStream = new(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
			Load(language, fileStream, encoding, overwrite);
		}

		/// <summary>
		/// 在指定分类中翻译指定文本
		/// </summary>
		/// <param name="category"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		public static string Translate(string category, string text)
		{
			if (m_Lock.IsHeldRead || m_Lock.IsHeldWrite
				|| m_Lock.IsHeldUpgradeableRead)
				return m_Map.Get(CurrentLanguage, category, text) ?? text;

			var readLock = m_Lock.TryRead(TRANSLATE_TIMEOUT, out var lockTaken);

			if (!lockTaken)
				return text;

			using (readLock)
			{
				return m_Map.Get(CurrentLanguage, category, text) ?? text;
			}
		}

		private class TranslationMap
		{
			private readonly Dictionary<string, Dictionary<string, Dictionary<string, string>>> m_Map = new();

			public string? Get(string language, string category, string text)
			{
				if (!m_Map.TryGetValue(language, out var categoryMap))
					return null;
				if (!categoryMap.TryGetValue(category, out var textMap))
					return null;
				return textMap.TryGetValue(text, out var result)
					? result : null;
			}

			public Dictionary<string, string> Category(string language, string category)
			{
				if (!m_Map.TryGetValue(language, out var categoryMap))
					categoryMap = m_Map[language] = new Dictionary<string, Dictionary<string, string>>();
				if (!categoryMap.TryGetValue(category, out var textMap))
					textMap = categoryMap[category] = new Dictionary<string, string>();
				return textMap;
			}
		}
	}
}
