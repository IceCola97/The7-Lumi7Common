using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Objects
{
	public static class IOCExtension
	{
		/// <summary>
		/// 通过IOC依赖注入来自动填充指定方法的参数并调用
		/// <br/><strong><em>已完成</em></strong>
		/// </summary>
		/// <param name="method"></param>
		/// <param name="instance"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static object? IOCInvoke(this MethodInfo method, object? instance, params object?[]? args)
		{
			return ObjectContainer.Invoke(method, instance, args);
		}

		/// <summary>
		/// 通过IOC依赖注入来自动填充构造函数的参数并调用
		/// <br/><strong><em>已完成</em></strong>
		/// </summary>
		/// <param name="ctor"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		public static object? IOCCreate(this ConstructorInfo ctor, params object?[]? args)
		{
			return ObjectContainer.Create(ctor, args);
		}
	}
}
