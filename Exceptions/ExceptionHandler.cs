using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Exceptions
{
	/// <summary>
	/// 提供异常捕获的函数式处理方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public sealed class ExceptionHandler
	{
		private readonly Exception m_Exception;

		public ExceptionHandler(Exception exception)
		{
			m_Exception = exception;
		}

		[DebuggerHidden]
		public ExceptionHandler? CatchIf<TException>(CatchIfBlock<TException> catchBlock)
			where TException : Exception
		{
			if (m_Exception is TException exception)
			{
				if (catchBlock.Invoke(exception))
					return null;
			}

			return this;
		}

		[DebuggerHidden]
		public ExceptionHandler? CatchIf(CatchIfBlock catchBlock)
		{
			if (catchBlock.Invoke(m_Exception))
				return null;

			return this;
		}

		[DebuggerHidden]
		public ExceptionHandler? Catch<TException>(CatchBlock<TException> catchBlock)
			where TException : Exception
		{
			if (m_Exception is TException exception)
			{
				catchBlock.Invoke(exception);
				return null;
			}

			return this;
		}

		[DebuggerHidden]
		public void Catch(CatchBlock catchBlock)
		{
			catchBlock.Invoke(m_Exception);
		}
	}

	public delegate void TryBlock();
	public delegate void TryBlock<TParameter>(TParameter parameter);
	public delegate bool CatchIfBlock<TException>(TException exception) where TException : Exception;
	public delegate bool CatchIfBlock(Exception exception);
	public delegate void CatchBlock<TException>(TException exception) where TException : Exception;
	public delegate void CatchBlock(Exception exception);
}
