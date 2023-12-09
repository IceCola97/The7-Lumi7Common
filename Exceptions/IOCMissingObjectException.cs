using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Exceptions
{
	/// <summary>
	/// 当给定类型或名称的IOC对象不存在时抛出
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public sealed class IOCMissingObjectException : Exception
	{
		public IOCMissingObjectException()
		{
		}

		public IOCMissingObjectException(string? message) : base(message)
		{
		}

		public IOCMissingObjectException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}
