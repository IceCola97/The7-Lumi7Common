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
	/// 将构造函数包装成动态方法
	/// </summary>
	public static class ConstructorInvoker
	{
		/// <summary>
		/// 将给定的构造函数包装成动态方法
		/// </summary>
		/// <param name="constructor"></param>
		/// <returns></returns>
		public static DynamicMethod WrapAsMethod(ConstructorInfo constructor)
		{
			var declaring = constructor.DeclaringType
				?? throw new ImpossibleException();

			var types = constructor.GetParameters()
				.Select(p => p.ParameterType).Prepend(declaring).ToArray();

			var dynMethod = new DynamicMethod("WrappedCtor", typeof(void), types, true);
			var ilGen = dynMethod.GetILGenerator();

			for (int i = 0; i < types.Length; i++)
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
			ilGen.Emit(OpCodes.Call, constructor);
			ilGen.Emit(OpCodes.Ret);

			MethodPatch.PrepareMethod(dynMethod);
			return dynMethod;
		}
	}
}
