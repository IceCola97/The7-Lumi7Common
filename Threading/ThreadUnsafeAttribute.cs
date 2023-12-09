namespace Lumi7Common.Threading
{
	/// <summary>
	/// 指示标注对象是线程不安全的，多线程使用前应该采用同步措施
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Struct | AttributeTargets.Event | AttributeTargets.Delegate | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
	public class ThreadUnsafeAttribute : Attribute
	{
	}

	/// <summary>
	/// 指示标注对象是线程不安全的，因为当前方法的锁是不完整的，需要特定的方法调用才能实现线程安全
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
	public class IncompleteLockAttribute : ThreadUnsafeAttribute
	{
	}
}
