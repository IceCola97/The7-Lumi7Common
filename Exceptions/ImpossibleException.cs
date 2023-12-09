using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Exceptions
{
	/// <summary>
	/// 按照编写者本意中不可能抛出的异常，通常用于必须抛出异常来消除错误与警告的地方
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public sealed class ImpossibleException : Exception
	{
#if TRACE
		public ImpossibleException(
			[CallerFilePath] string? callerFile = null,
			[CallerLineNumber] int? callerLine = null,
			[CallerMemberName] string? callerMember = null
		)
			: base(ET("此异常是不可能产生的:\n引发文件: {0}\n引发行数: {1}\n引发成员: {2}",
				callerFile ?? "<未知>", callerLine?.ToString() ?? "<未知>", callerMember ?? "<未知>"))
		{
		}
#else
		public ImpossibleException()
			: base(ET("此异常是不可能产生的")) { }
#endif

		public ImpossibleException(string unless)
			: base(ET("此异常是不可能产生的, 除非{0}", unless)) { }

		public ImpossibleException(Exception exception)
			: base(ET("此处的产生了不可能异常 '{0}', 造成原因是: {1}", exception.GetType().FullName, exception.Message), exception) { }
	}
}
