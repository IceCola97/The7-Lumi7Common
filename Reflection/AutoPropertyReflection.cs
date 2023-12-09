using Lumi7Common.Reflection.IL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection
{
	/// <summary>
	/// 提供自动属性相关反射
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class AutoPropertyReflection
	{
		/// <summary>
		/// 获取当前自动属性访问的字段<br/>
		/// 如果当前属性不是自动属性则返回<see langword="null"/>
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public static FieldInfo? GetAutoAccessField(this PropertyInfo property)
		{
			FieldInfo? accessField = null;

			if (property.CanRead)
			{
				var getter = property.GetGetMethod(true);

				if (getter is null
					|| !getter.IsAutoPropertyGetterHead())
					return null;

				accessField = getter.GetAutoGetterField();

				if (accessField is null)
					return null;
			}

			if (property.CanWrite)
			{
				var setter = property.GetSetMethod(true);

				if (setter is null
					|| !setter.IsAutoPropertySetterHead())
					return null;

				var setterAccessField = setter.GetAutoSetterField();

				if (setterAccessField is null
					|| (accessField is not null && accessField != setterAccessField))
					return null;

				accessField = setterAccessField;
			}

			return accessField;
		}

		/// <summary>
		/// 判断当前属性是否是自动属性
		/// </summary>
		/// <param name="property"></param>
		/// <returns></returns>
		public static bool IsAutoProperty(this PropertyInfo property)
		{
			return property.GetAutoAccessField() is null;
		}

		/// <summary>
		/// 获取当前方法作为属性索引器所属的属性<br/>
		/// 如果当前方法不是隶属于任何属性的访问器则返回<see langword="null"/>
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static PropertyInfo? GetDeclaringProperty(this MethodInfo method)
		{
			var declaring = method.DeclaringType;

			if (declaring is null
				|| !method.IsSpecialName
				|| method.Name.Length <= 4
				|| method.Name.Substring(1, 3) != "et_"
				|| (method.Name[0] != 'g' && method.Name[0] != 's'))
				return null;

			var propertyName = method.Name[4..];
			var property = declaring.GetProperty(propertyName,
				BindingFlags.Instance | BindingFlags.Static
				| BindingFlags.Public | BindingFlags.NonPublic);

			if (property is null)
				return null;

			switch (method.Name[0])
			{
				case 'g':
					if (!property.CanRead
						|| property.GetGetMethod(true) != method)
						return null;

					break;
				case 's':
					if (!property.CanWrite
						|| property.GetSetMethod(true) != method)
						return null;

					break;
				default:
					throw new ImpossibleException();
			}

			return property;
		}

		/// <summary>
		/// 判断当前方法是否是自动属性的索引器
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static bool IsAutoPropertyAccessor(this MethodInfo method)
		{
			return method.GetDeclaringProperty()?
				.IsAutoProperty() ?? false;
		}

		private static FieldInfo? GetAutoGetterField(this MethodInfo method)
		{
			var ilData = method.GetMethodBody()?.GetILAsByteArray();

			if (ilData is null)
				return null;

			var ilDecomp = new ILDecompiler(ilData);
			var ilCodes = new List<ILCode>(3);

			int required = method.IsStatic ? 2 : 3;

			while (ilDecomp.MoveNext())
			{
				if (ilCodes.Count >= required)
					return null;

				ilCodes.Add(ilDecomp.Current);
			}

			int metadataToken;

			if (method.IsStatic)
			{
				if (ilCodes.Count != required
					|| ilCodes[0].OpCode != OpCodes.Ldsfld
					|| ilCodes[1].OpCode != OpCodes.Ret
					|| ilCodes[0].Operand32 == 0)
					return null;

				metadataToken = ilCodes[0].Operand32;
			}
			else
			{
				if (ilCodes.Count != required
					|| ilCodes[0].OpCode != OpCodes.Ldarg_0
					|| ilCodes[1].OpCode != OpCodes.Ldfld
					|| ilCodes[2].OpCode != OpCodes.Ret
					|| ilCodes[1].Operand32 == 0)
					return null;

				metadataToken = ilCodes[1].Operand32;
			}

			try
			{
				return method.Module.ResolveField(metadataToken);
			}
			catch
			{
				return null;
			}
		}

		private static FieldInfo? GetAutoSetterField(this MethodInfo method)
		{
			var ilData = method.GetMethodBody()?.GetILAsByteArray();

			if (ilData is null)
				return null;

			var ilDecomp = new ILDecompiler(ilData);
			var ilCodes = new List<ILCode>(4);

			int required = method.IsStatic ? 3 : 4;

			while (ilDecomp.MoveNext())
			{
				if (ilCodes.Count >= required)
					return null;

				ilCodes.Add(ilDecomp.Current);
			}

			int metadataToken;

			if (method.IsStatic)
			{
				if (ilCodes.Count != required
					|| ilCodes[0].OpCode != OpCodes.Ldarg_1
					|| ilCodes[1].OpCode != OpCodes.Stsfld
					|| ilCodes[2].OpCode != OpCodes.Ret
					|| ilCodes[1].Operand32 == 0)
					return null;

				metadataToken = ilCodes[1].Operand32;
			}
			else
			{
				if (ilCodes.Count != required
					|| ilCodes[0].OpCode != OpCodes.Ldarg_0
					|| ilCodes[1].OpCode != OpCodes.Ldarg_1
					|| ilCodes[2].OpCode != OpCodes.Stfld
					|| ilCodes[3].OpCode != OpCodes.Ret
					|| ilCodes[2].Operand32 == 0)
					return null;

				metadataToken = ilCodes[2].Operand32;
			}

			try
			{
				return method.Module.ResolveField(metadataToken);
			}
			catch
			{
				return null;
			}
		}

		private static bool IsAutoPropertyGetterHead(this MethodInfo method)
		{
			return method.IsSpecialName && method.Name.StartsWith("get_")
				&& method.GetMethodImplementationFlags().HasFlag(MethodImplAttributes.IL)
				&& method.GetCustomAttribute<CompilerGeneratedAttribute>() is not null;
		}

		private static bool IsAutoPropertySetterHead(this MethodInfo method)
		{
			return method.IsSpecialName && method.Name.StartsWith("set_")
				&& method.GetMethodImplementationFlags().HasFlag(MethodImplAttributes.IL)
				&& method.GetCustomAttribute<CompilerGeneratedAttribute>() is not null;
		}
	}
}
