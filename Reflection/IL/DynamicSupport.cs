using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供动态代码支持判断
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class DynamicSupport
	{
		/// <summary>
		/// 判断当前框架是否支持动态代码
		/// </summary>
		public static bool IsDynamicCodeSupported =>
			RuntimeFeature.IsDynamicCodeCompiled && RuntimeFeature.IsDynamicCodeSupported;

		/// <summary>
		/// 断言当前框架支持动态代码
		/// </summary>
		/// <exception cref="NotSupportedException"></exception>
		public static void AssertDynamicSupport()
		{
			if (!IsDynamicCodeSupported)
				throw new NotSupportedException(ET("当前框架不支持动态代码"));
		}
	}
}
