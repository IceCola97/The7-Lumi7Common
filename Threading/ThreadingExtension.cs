using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Threading
{
	/// <summary>
	/// 线程相关扩展函数
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class ThreadingExtension
	{
		[ThreadSafe]
		public static bool IsObjectHolding(this IThreadHold threadHold)
			=> Thread.CurrentThread.Equals(threadHold.GetObjectHolder());
	}
}
