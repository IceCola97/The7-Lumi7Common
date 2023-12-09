using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Exceptions
{
	/// <summary>
	/// 当IOC提供者的标注或行为不明确时抛出
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public sealed class IOCAmbiguousProviderException : Exception
	{
		public IOCAmbiguousProviderException()
		{
		}

		public IOCAmbiguousProviderException(string? message) : base(message)
		{
		}

		public IOCAmbiguousProviderException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}
