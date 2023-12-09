using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection
{
	/// <summary>
	/// 提供杂项的反射方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class MiscReflection
	{
		/// <summary>
		/// 获取当前成员的声明类型或模块名
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		[ThreadSafe]
		public static string GetDeclaringName(this MemberInfo member)
			=> member.DeclaringType?.FullName ?? $"<module: {member.Module.Name}>";

		/// <summary>
		/// 判断当前类型是否是对象类型或<see cref="Nullable{T}"/>泛型
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		[ThreadSafe]
		public static bool IsNullable(this Type type)
		{
			if (type.IsByRef || type.IsByRefLike || type.IsPointer || type.IsFunctionPointer)
				return false;
			if (type.IsValueType && Nullable.GetUnderlyingType(type) is null)
				return false;

			return true;
		}

		/// <summary>
		/// 获取给定类型的默认值，等效于<see langword="default"/>
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		[ThreadSafe]
		public static object? GetDefaultValue(this Type type)
		{
			if (type.IsByRef)
				return (type.GetElementType()
					?? throw new ImpossibleException(ET("引用类型没有包含被引用的基础类型: {0}",
					type.FullName))).GetDefaultValue();

			if (type.IsPointer || type.IsFunctionPointer)
				return nint.Zero;

			if (type.IsValueType)
			{
				if (Nullable.GetUnderlyingType(type) is null)
					return RuntimeHelpers.GetUninitializedObject(type);
			}

			return null;
		}

		/// <summary>
		/// 构造List<>的泛型类型
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		[ThreadSafe]
		public static Type MakeGenericList(this Type type)
			=> typeof(List<>).MakeGenericType(type);

		/// <summary>
		/// 构造Dictionary<>的泛型类型
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		[ThreadSafe]
		public static Type MakeGenericDictionary(this Type key, Type value)
			=> typeof(Dictionary<,>).MakeGenericType(key, value);

		/// <summary>
		/// 将一个委托对象转换为给定的委托类型
		/// </summary>
		/// <typeparam name="TDelegate"></typeparam>
		/// <param name="delegate"></param>
		/// <returns></returns>
		public static TDelegate Convert<TDelegate>(this Delegate @delegate)
			where TDelegate : class, MulticastDelegate
		{
			if (@delegate is TDelegate result)
				return result;

			if (@delegate.Method is DynamicMethod dynamicMethod)
				return dynamicMethod.CreateDelegate<TDelegate>(@delegate.Target);

			return (TDelegate)Delegate.CreateDelegate(typeof(TDelegate),
				@delegate.Target, @delegate.Method);
		}
	}
}
