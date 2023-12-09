using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Threading
{
	/// <summary>
	/// 指示标注对象是线程安全的，多线程使用前无需采用同步措施
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Event | AttributeTargets.Delegate | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
	public class ThreadSafeAttribute : Attribute
	{
	}

	/// <summary>
	/// 指示标注对象是线程安全的，并且实现方法是通过同步锁
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class SynchronizedAttribute : ThreadSafeAttribute
	{
	}

	/// <summary>
	/// 指示标注对象是线程安全的，并且实现方法是通过读写分离锁
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class ReadWriteLockedAttribute : ThreadSafeAttribute
	{
	}

	/// <summary>
	/// 指示标注对象是线程安全的，并且实现方法是通过CAS与回滚操作确保线程安全
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class CASOperationAttribute : ThreadSafeAttribute
	{
	}
}
