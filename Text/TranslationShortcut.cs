using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Text
{
	/// <summary>
	/// 翻译助手函数
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public static class TranslationShortcut
	{
		[ThreadStatic]
		private static object?[][]? m_TempArrays;

		/// <summary>
		/// 指示异常消息分类
		/// </summary>
		public const string TRANCATE_EXCEPTION = Translation.CATEGORY_EXCEPTION;

		[MemberNotNull(nameof(m_TempArrays))]
		private static void EnsureTempArrays()
		{
			m_TempArrays ??= new object[][]
			{
				new object[0],
				new object[1],
				new object[2],
				new object[3],
				new object[4],
				new object[5],
			};
		}

		/// <summary>
		/// 在指定分类中翻译指定文本
		/// </summary>
		/// <param name="category"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Translate(string category, string text)
			=> Translation.Translate(category, text);

		/// <summary>
		/// 在指定分类中翻译指定文本，并携带参数
		/// </summary>
		/// <param name="category"></param>
		/// <param name="text"></param>
		/// <param name="arg0"></param>
		/// <returns></returns>
		public static string Translate(string category, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string text, object? arg0)
		{
			EnsureTempArrays();
			text = Translation.Translate(category, text);
			m_TempArrays[1][0] = arg0;
			return string.Format(text, m_TempArrays[1]);
		}

		/// <summary>
		/// 在指定分类中翻译指定文本，并携带参数
		/// </summary>
		/// <param name="category"></param>
		/// <param name="text"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <returns></returns>
		public static string Translate(string category, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string text, object? arg0, object? arg1)
		{
			EnsureTempArrays();
			text = Translation.Translate(category, text);
			m_TempArrays[2][0] = arg0;
			m_TempArrays[2][1] = arg1;
			return string.Format(text, m_TempArrays[2]);
		}

		/// <summary>
		/// 在指定分类中翻译指定文本，并携带参数
		/// </summary>
		/// <param name="category"></param>
		/// <param name="text"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		/// <returns></returns>
		public static string Translate(string category, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string text, object? arg0, object? arg1, object? arg2)
		{
			EnsureTempArrays();
			text = Translation.Translate(category, text);
			m_TempArrays[3][0] = arg0;
			m_TempArrays[3][1] = arg1;
			m_TempArrays[3][2] = arg2;
			return string.Format(text, m_TempArrays[3]);
		}

		/// <summary>
		/// 在指定分类中翻译指定文本，并携带参数
		/// </summary>
		/// <param name="category"></param>
		/// <param name="text"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string Translate(string category, [StringSyntax(StringSyntaxAttribute.CompositeFormat)] string text, params object?[] args)
		{
			text = Translation.Translate(category, text);
			return string.Format(text, args);
		}

		/// <summary>
		/// 在异常消息分类中翻译指定文本
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ET(string text)
			=> Translation.Translate(TRANCATE_EXCEPTION, text);

		/// <summary>
		/// 在异常消息分类中翻译指定文本，并携带参数
		/// </summary>
		/// <param name="text"></param>
		/// <param name="arg0"></param>
		/// <returns></returns>
		public static string ET([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string text, object? arg0)
		{
			EnsureTempArrays();
			text = Translation.Translate(TRANCATE_EXCEPTION, text);
			m_TempArrays[1][0] = arg0;
			return string.Format(text, m_TempArrays[1]);
		}

		/// <summary>
		/// 在异常消息分类中翻译指定文本，并携带参数
		/// </summary>
		/// <param name="text"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <returns></returns>
		public static string ET([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string text, object? arg0, object? arg1)
		{
			EnsureTempArrays();
			text = Translation.Translate(TRANCATE_EXCEPTION, text);
			m_TempArrays[2][0] = arg0;
			m_TempArrays[2][1] = arg1;
			return string.Format(text, m_TempArrays[2]);
		}

		/// <summary>
		/// 在异常消息分类中翻译指定文本，并携带参数
		/// </summary>
		/// <param name="text"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		/// <returns></returns>
		public static string ET([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string text, object? arg0, object? arg1, object? arg2)
		{
			EnsureTempArrays();
			text = Translation.Translate(TRANCATE_EXCEPTION, text);
			m_TempArrays[3][0] = arg0;
			m_TempArrays[3][1] = arg1;
			m_TempArrays[3][2] = arg2;
			return string.Format(text, m_TempArrays[3]);
		}

		/// <summary>
		/// 在异常消息分类中翻译指定文本，并携带参数
		/// </summary>
		/// <param name="text"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		/// <param name="arg3"></param>
		/// <returns></returns>
		public static string ET([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string text, object? arg0, object? arg1, object? arg2, object? arg3)
		{
			EnsureTempArrays();
			text = Translation.Translate(TRANCATE_EXCEPTION, text);
			m_TempArrays[4][0] = arg0;
			m_TempArrays[4][1] = arg1;
			m_TempArrays[4][2] = arg2;
			m_TempArrays[4][3] = arg3;
			return string.Format(text, m_TempArrays[4]);
		}

		/// <summary>
		/// 在异常消息分类中翻译指定文本，并携带参数
		/// </summary>
		/// <param name="text"></param>
		/// <param name="arg0"></param>
		/// <param name="arg1"></param>
		/// <param name="arg2"></param>
		/// <param name="arg3"></param>
		/// <param name="arg4"></param>
		/// <returns></returns>
		public static string ET([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string text, object? arg0, object? arg1, object? arg2, object? arg3, object? arg4)
		{
			EnsureTempArrays();
			text = Translation.Translate(TRANCATE_EXCEPTION, text);
			m_TempArrays[5][0] = arg0;
			m_TempArrays[5][1] = arg1;
			m_TempArrays[5][2] = arg2;
			m_TempArrays[5][3] = arg3;
			m_TempArrays[5][4] = arg4;
			return string.Format(text, m_TempArrays[5]);
		}

		/// <summary>
		/// 在异常消息分类中翻译指定文本，并携带参数
		/// </summary>
		/// <param name="text"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static string ET([StringSyntax(StringSyntaxAttribute.CompositeFormat)] string text, params object?[] args)
		{
			text = Translation.Translate(TRANCATE_EXCEPTION, text);
			return string.Format(text, args);
		}
	}
}
