using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供将委托封装到静态方法的能力
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class DelegateWrapper
	{
		private static readonly DynamicNamespace m_Namespace = new(typeof(DelegateWrapper).FullName
			?? nameof(DelegateWrapper));

		private static volatile int m_TypeId = 0x60000;

		/// <summary>
		/// 将给定的委托封装到一个静态方法
		/// </summary>
		/// <param name="delegate"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="ImpossibleException"></exception>
		public static MethodBase Wrap(Delegate @delegate)
		{
			DynamicSupport.AssertDynamicSupport();

			var delegateType = @delegate.GetType();
			var invokeMethod = delegateType.GetMethod("Invoke", BindingFlags.Public | BindingFlags.Instance)
				?? throw new ArgumentException(ET("给定的委托对象无效"));

			if (invokeMethod.CallingConvention.HasFlag(CallingConventions.VarArgs))
				throw new NotSupportedException(ET("不支持给定的方法"));

			var fieldType = delegateType ?? typeof(DynamicMethod);

			var builder = m_Namespace.DefineType($"WrapType{Interlocked.Increment(ref m_TypeId)}",
				TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Abstract | TypeAttributes.Sealed);

			var field = builder.DefineField("m_Wrapped", fieldType,
				FieldAttributes.Private | FieldAttributes.Static);

			var parameters = invokeMethod.GetParameters();
			var method = builder.DefineMethod("WrapCall", MethodAttributes.Public | MethodAttributes.Static,
				invokeMethod.ReturnType, parameters.Select(p => p.ParameterType).ToArray());
			var ilGen = method.GetILGenerator();

			ilGen.Emit(OpCodes.Ldsfld, field);

			for (int i = 0; i < parameters.Length; i++)
			{
				switch (i)
				{
					case 0:
						ilGen.Emit(OpCodes.Ldarg_0);
						break;
					case 1:
						ilGen.Emit(OpCodes.Ldarg_1);
						break;
					case 2:
						ilGen.Emit(OpCodes.Ldarg_2);
						break;
					case 3:
						ilGen.Emit(OpCodes.Ldarg_3);
						break;
					case <= 0xFFFF:
						ilGen.Emit(OpCodes.Ldarg_S, (short)i);
						break;
					default:
						ilGen.Emit(OpCodes.Ldarg, i);
						break;
				}
			}

			ilGen.Emit(OpCodes.Tailcall);
			ilGen.Emit(OpCodes.Call, invokeMethod);
			ilGen.Emit(OpCodes.Ret);

			var type = builder.CreateType();
			(type.GetField("m_Wrapped", BindingFlags.NonPublic | BindingFlags.Static)
				?? throw new ImpossibleException()).SetValue(null, @delegate);
			return type.GetMethod("WrapCall") ?? throw new ImpossibleException();
		}
	}
}
