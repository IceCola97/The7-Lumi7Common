using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Exceptions
{
	/// <summary>
	/// 当指定或给出一个IOC不支持的组件类型时抛出
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public sealed class IOCComponentNotSupportedException : Exception
	{
		public IOCComponentNotSupportedException()
		{
		}

		public IOCComponentNotSupportedException(string? message) : base(message)
		{
		}

		public IOCComponentNotSupportedException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}
