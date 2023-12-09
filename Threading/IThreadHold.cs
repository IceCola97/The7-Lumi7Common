using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Threading
{
	/// <summary>
	/// 线程持有锁的接口
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public interface IThreadHold
	{
		/// <summary>
		/// 检查当前执行线程并在未持有对象的情况下抛出异常
		/// </summary>
		void CheckObjectHolder();

		/// <summary>
		/// 持有当前对象，将当前对象标记为当前线程独有<br/>
		/// 如果当前对象已经被其他线程持有则等待其他线程释放<br/>
		/// 持有与释放操作可以递归，但必须确保持有和释放的操作数相同
		/// </summary>
		void ObjectHold();

		/// <summary>
		/// 尝试持有当前对象，如果如果当前对象已经被其他线程持有则返回<see langword="false"/><br/>
		/// 持有与释放操作可以递归，但必须确保持有和释放的操作数相同
		/// </summary>
		/// <returns></returns>
		bool TryObjectHold();

		/// <summary>
		/// 释放当前对象，将当前对象的独有标记释放<br/>
		/// 如果当前线程没有持有当前对象将会抛出异常<br/>
		/// 持有与释放操作可以递归，但必须确保持有和释放的操作数相同
		/// </summary>
		void ObjectRelease();

		/// <summary>
		/// 获取持有当前对象的线程
		/// </summary>
		/// <returns></returns>
		Thread? GetObjectHolder();
	}
}
