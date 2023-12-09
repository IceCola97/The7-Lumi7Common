using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Exceptions
{
	/// <summary>
	/// 字段类型不匹配异常
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public sealed class FieldTypeMismatchException : Exception
	{
		public FieldTypeMismatchException()
		{
		}

		public FieldTypeMismatchException(string? message) : base(message)
		{
		}

		public FieldTypeMismatchException(string? message, Exception? innerException) : base(message, innerException)
		{
		}
	}
}
