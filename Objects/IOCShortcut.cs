using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Objects
{
	/// <summary>
	/// IOC助手函数
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class IOCShortcut
	{
		/// <summary>
		/// 向容器要求指定类型的组件对象
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T IOCRequire<T>() where T : class => ObjectContainer.Require<T>();

		/// <summary>
		/// 向容器要求指定名称指定类型的组件对象
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T IOCRequire<T>(string name) where T : class => ObjectContainer.Require<T>(name);

		/// <summary>
		/// 从容器中获取指定类型的组件对象
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T? IOCGet<T>() where T : class => ObjectContainer.Get<T>();

		/// <summary>
		/// 从容器中获取指定名称指定类型的组件对象
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T? IOCGet<T>(string name) where T : class => ObjectContainer.Get<T>(name);
	}
}
