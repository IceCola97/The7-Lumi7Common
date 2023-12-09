using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Threading
{
	/// <summary>
	/// 线程持有锁的实现
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public class ThreadHold : IThreadHold
	{
		private volatile Thread? m_Holder = null;
		private volatile int m_HoldingCounter = 0;

		public void CheckObjectHolder()
		{
			if (m_Holder != Thread.CurrentThread)
				throw new ThreadStateException(ET("当前线程未持有此对象"));
		}

		public Thread? GetObjectHolder() => m_Holder;

		public void ObjectHold()
		{
			var spinWait = new SpinWait();

			while (true)
			{
				if (TryObjectHold())
					return;

				spinWait.SpinOnce();
			}
		}

		public void ObjectRelease()
		{
			var currentThread = Thread.CurrentThread;

			if (currentThread.Equals(m_Holder))
			{
				if (Interlocked.Decrement(ref m_HoldingCounter) <= 0)
					m_Holder = null;

				return;
			}

			throw new ThreadStateException(ET("当前线程未持有此对象"));
		}

		public bool TryObjectHold()
		{
			var currentThread = Thread.CurrentThread;
			var oldHolder = Interlocked.CompareExchange(ref m_Holder, currentThread, null);

			if (oldHolder is null
				|| oldHolder.Equals(currentThread))
			{
				Interlocked.Increment(ref m_HoldingCounter);
				return true;
			}

			return false;
		}
	}
}
