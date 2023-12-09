using Lumi7Common.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection
{
	/// <summary>
	/// 提供一种动态的、定制性的根据具体参数来查找方法的手段
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class MethodMatcher
	{
		private const BindingFlags DefaultFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
		private static readonly ConvertOptions m_Default = new ConvertOptions().Seal();

		/// <summary>
		/// 在指定类型中搜索最符合给定参数的方法，并将参数转换为可以直接调用的类型
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		/// <param name="args"></param>
		/// <param name="bindingFlags"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static MethodInfo? FindBestMatch(this Type type, string name, object?[] args,
			BindingFlags bindingFlags = DefaultFlags, ConvertOptions? options = null)
		{
			options ??= m_Default;

			if (!options.IsSealed)
				throw new ArgumentException(ET("转换规则必须要是只读的"));

			var methods = type.GetMethods();

			if ((bindingFlags & BindingFlags.IgnoreCase) != 0)
				methods = methods.Where(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).ToArray();
			else
				methods = methods.Where(m => m.Name == name).ToArray();

			return (MethodInfo?)FindBestMatch(methods, args, options);
		}

		/// <summary>
		/// 在给定方法中搜索最符合给定参数的方法，并将参数转换为可以直接调用的类型<br/>
		/// 当没有符合条件的方法时返回<see langword="null"/>
		/// </summary>
		/// <param name="methods"></param>
		/// <param name="args"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="AmbiguousMatchException"></exception>
		public static MethodBase? FindBestMatch(this IEnumerable<MethodBase> methods, object?[] args, ConvertOptions? options = null)
		{
			options ??= m_Default;

			if (!options.IsSealed)
				throw new ArgumentException(ET("转换规则必须要是只读的"));

			var sorted = new SortedList<int, MethodBase>(MethodSorter.Instance);

			foreach (var method in methods)
			{
				var ps = method.GetParameters();

				if (ps.Length != args.Length)
					continue;

				int score = 0;

				for (int i = ps.Length - 1; i >= 0; i--)
				{
					int itemScore = EvaluateConvert(args[i]?.GetType(), ps[i].ParameterType, options);

					if (itemScore < 0)
					{
						score = -1;
						break;
					}

					score += itemScore;
				}

				if (score < 0)
					continue;

				sorted.Add(score, method);
			}

			object?[] converted = [.. args];

			while (true)
			{
				if (sorted.Count == 0)
					return null;

				var first = sorted.GetValueAtIndex(0);

				if (sorted.Count == 1)
				{
					if (TryConvertAll(first, converted, options))
					{
						converted.CopyTo(args, 0);
						return first;
					}

					args.CopyTo(converted, 0);
					sorted.RemoveAt(0);
					continue;
				}

				var second = sorted.GetValueAtIndex(1);
				int firstScore = sorted.GetKeyAtIndex(0);
				int secondScore = sorted.GetKeyAtIndex(1);

				if (firstScore > secondScore)
				{
					if (TryConvertAll(first, args, options))
					{
						converted.CopyTo(args, 0);
						return first;
					}

					args.CopyTo(converted, 0);
					sorted.RemoveAt(0);
					continue;
				}

				int idx = 0;
				int count = sorted.Count;
				int removed = 0;

				while (idx < count)
				{
					int score = sorted.GetKeyAtIndex(idx);
					var method = sorted.GetValueAtIndex(idx);

					if (!TryConvertAll(method, converted, options))
					{
						args.CopyTo(converted, 0);
						sorted.RemoveAt(idx);
						count--;
						removed++;
						continue;
					}

					args.CopyTo(converted, 0);
					idx++;
				}

				if (removed == 0)
					throw new AmbiguousMatchException(ET("给定参数在指定的方法组中有歧义性"));
			}
		}

		/// <summary>
		/// 在给定方法中搜索最符合给定参数类型的方法<br/>
		/// 当没有符合条件的方法时返回<see langword="null"/>
		/// </summary>
		/// <param name="methods"></param>
		/// <param name="args"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="AmbiguousMatchException"></exception>
		public static MethodBase? FindBestMatch(this IEnumerable<MethodBase> methods, Type[] types, ConvertOptions? options = null)
		{
			options ??= m_Default;

			if (!options.IsSealed)
				throw new ArgumentException(ET("转换规则必须要是只读的"));

			var sorted = new SortedList<int, MethodBase>(MethodSorter.Instance);

			foreach (var method in methods)
			{
				var ps = method.GetParameters();

				if (ps.Length != types.Length)
					continue;

				int score = 0;

				for (int i = ps.Length - 1; i >= 0; i--)
				{
					int itemScore = EvaluateConvert(types[i], ps[i].ParameterType, options);

					if (itemScore < 0)
					{
						score = -1;
						break;
					}

					score += itemScore;
				}

				if (score < 0)
					continue;

				sorted.Add(score, method);
			}

			if (sorted.Count == 0)
				return null;

			var first = sorted.GetValueAtIndex(0);

			if (sorted.Count == 1)
				return first;

			int firstScore = sorted.GetKeyAtIndex(0);
			int secondScore = sorted.GetKeyAtIndex(1);

			if (firstScore > secondScore)
				return first;

			throw new AmbiguousMatchException(ET("给定参数类型在指定的方法组中有歧义性"));
		}

		private static bool TryConvertAll(MethodBase method, object?[] args, ConvertOptions? options = null)
		{
			var ps = method.GetParameters();

			for (int i = ps.Length - 1; i >= 0; i--)
			{
				if (!TryConvertValue(ref args[i], ps[i].ParameterType, options))
					return false;
			}

			return true;
		}

		/// <summary>
		/// 将给定的对象转换成目标类型，可以通过指定其余参数来增加类型的隐式转换
		/// </summary>
		/// <param name="value"></param>
		/// <param name="slot"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static bool TryConvertValue(ref object? value, Type slot, ConvertOptions? options = null)
		{
			options ??= m_Default;

			if (!options.IsSealed)
				throw new ArgumentException(ET("转换规则必须要是只读的"));

			if (value is null)
			{
				switch (slot.GetPrimitiveType())
				{
					case PrimitiveType.Nullable:
					case PrimitiveType.String:
						return true;
					case PrimitiveType.Unknown:
						return !slot.IsValueType;
					case PrimitiveType.Pointer:
						value = nint.Zero;
						return true;
					default:
						return false;
				}
			}

			var type = value.GetType();

			if (type.IsAssignableTo(slot))
				return true;

			if (slot.IsByRef)
				return value is not null
					&& value.GetType() == slot.GetElementType();

			var slotNotnull = slot.GetNotnullableType();

			var valueType = type.GetPrimitiveType();
			var slotType = slotNotnull.GetPrimitiveType();

			if (options.HasTypeConverter(type, slotNotnull))
			{
				object? converted = value;

				if (options.TryTypeConvert(type, slotNotnull, ref converted, slot))
				{
					value = converted;
					return true;
				}

				return false;
			}
			else if (options.HasConverter(valueType, slotType))
			{
				object? converted = value;

				if (options.TryConvert(valueType, slotType, ref converted, slot))
				{
					value = converted;
					return true;
				}

				return false;
			}

			switch (valueType)
			{
				case PrimitiveType.Integer:
				case PrimitiveType.Float:
					if (!options.MixNumber)
						return false;

					if (!options.MixValueType || !slotNotnull.IsValueType
						|| Marshal.SizeOf(value) != Marshal.SizeOf(slotNotnull))
					{
						if (!(slotType switch
						{
							PrimitiveType.Pointer => options.MixPointer,
							PrimitiveType.Boolean => options.MixBoolean,
							PrimitiveType.Character => options.MixCharacter,
							PrimitiveType.String => options.MixStringIn,
							_ => false,
						}))
							return false;
					}
					else
					{
						value = SameSizeConvert(value, slotNotnull);
						return true;
					}

					if (slotType == PrimitiveType.Pointer)
						return TryConvertToPointer(ref value, slotNotnull);

					value = ((IConvertible)value).ToType(slotNotnull, null);
					return true;
				case PrimitiveType.Pointer:
					if (!options.MixPointer)
						return false;

					if (!options.MixValueType || !slotNotnull.IsValueType
						|| IntPtr.Size != Marshal.SizeOf(slotNotnull))
					{
						if (!(slotType switch
						{
							PrimitiveType.Integer => options.MixNumber,
							PrimitiveType.Float => options.MixNumber,
							PrimitiveType.Boolean => options.MixBoolean,
							PrimitiveType.Character => options.MixCharacter,
							PrimitiveType.String => options.MixStringIn,
							_ => false,
						}))
							return false;
					}
					else
					{
						value = SameSizeConvert(value, slotNotnull);
						return true;
					}

					return TryConvertFromPointer(ref value, slotNotnull);
				case PrimitiveType.Boolean:
					if (!options.MixBoolean)
						return false;

					if (!options.MixValueType || !slotNotnull.IsValueType
						|| sizeof(bool) != Marshal.SizeOf(slotNotnull))
					{
						if (!(slotType switch
						{
							PrimitiveType.Integer => options.MixNumber,
							PrimitiveType.Float => options.MixNumber,
							PrimitiveType.String => options.MixStringIn,
							_ => false,
						}))
							return false;
					}
					else
					{
						value = SameSizeConvert(value, slotNotnull);
						return true;
					}

					if (slotType == PrimitiveType.String)
					{
						if (Equals(value, true))
							value = "true";
						else
							value = "false";

						return true;
					}

					value = ((IConvertible)value).ToType(slotNotnull, null);
					return true;
				case PrimitiveType.Character:
					if (!options.MixCharacter)
						return false;

					if (!options.MixValueType || !slotNotnull.IsValueType
						|| sizeof(char) != Marshal.SizeOf(slotNotnull))
					{
						if (!(slotType switch
						{
							PrimitiveType.Integer => options.MixNumber,
							PrimitiveType.Float => options.MixNumber,
							PrimitiveType.Boolean => options.MixBoolean,
							PrimitiveType.String => options.MixStringIn,
							_ => false,
						}))
							return false;
					}
					else
					{
						value = SameSizeConvert(value, slotNotnull);
						return true;
					}

					value = ((IConvertible)value).ToType(slotNotnull, null);
					return true;
				case PrimitiveType.String:
					if (!options.MixStringOut)
						return false;

					if (!(slotType switch
					{
						PrimitiveType.Integer => options.MixNumber,
						PrimitiveType.Float => options.MixNumber,
						PrimitiveType.Pointer => options.MixPointer,
						PrimitiveType.Boolean => options.MixBoolean,
						PrimitiveType.Character => options.MixCharacter,
						_ => false,
					}))
						return false;

					if (slotType == PrimitiveType.Pointer)
						return TryConvertToPointer(ref value, slotNotnull);

					try
					{
						value = ((IConvertible)value).ToType(slotNotnull, null);
					}
					catch
					{
						return false;
					}

					return true;
				default:
					if (type.IsValueType && slotNotnull.IsValueType
						&& Marshal.SizeOf(type) == Marshal.SizeOf(slotNotnull))
					{
						value = SameSizeConvert(value, slotNotnull);
						return true;
					}

					return false;
			}
		}

		/// <summary>
		/// 在<paramref name="leaf"/>的继承树上搜索<paramref name="target"/><br/>
		/// 参数<paramref name="target"/>可以是类或接口<br/>
		/// 如果<paramref name="target"/>与<paramref name="leaf"/>相同或是<paramref name="leaf"/>实现的接口返回0<br/>
		/// 如果<paramref name="target"/>是<paramref name="leaf"/>直接基类或直接基类实现的接口返回1<br/>
		/// 后续依此类推，如果没有搜索到则返回-1
		/// </summary>
		/// <param name="leaf"></param>
		/// <param name="target"></param>
		/// <returns></returns>
		public static int SearchInheritDepth(Type leaf, Type target)
		{
			var type = leaf;
			int depth = 0;

			while (type is not null)
			{
				if (type == target
					|| (target.IsInterface
					&& type.GetInterfaces().Contains(target)))
					return depth;

				type = type.BaseType;
				depth++;
			}

			return -1;
		}

		/// <summary>
		/// 通过值的类型与变量类型来评估值与变量在隐式转换方面的契合程度，并返回一个自然数的契合数值，契合数值越低（越接近零）契合程度越高，如果无法契合返回-1
		/// </summary>
		/// <param name="value"></param>
		/// <param name="slot"></param>
		/// <param name="options"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static int EvaluateConvert(Type? value, Type slot, ConvertOptions? options = null)
		{
			options ??= m_Default;

			if (!options.IsSealed)
				throw new ArgumentException(ET("转换规则必须要是只读的"));

			if (value == slot)
				return 0;

			if (slot.IsByRef)
				return value is not null
					&& value == slot.GetElementType()
					? 0 : -1;

			if (value is null)
			{
				if (!slot.IsValueType
					|| Nullable.GetUnderlyingType(slot) is not null)
					return 0;

				return -1;
			}

			int depth = SearchInheritDepth(value, slot);

			if (depth >= 0)
			{
				if (slot.IsInterface)
					depth++;

				return depth << 5;
			}

			if (IsMeaninglessConvert(value, slot))
				return -1;

			var valueNotnull = value.GetNotnullableType();
			var slotNotnull = slot.GetNotnullableType();

			var valueType = valueNotnull.GetPrimitiveType();
			var slotType = slotNotnull.GetPrimitiveType();

			bool allowConvert = options.HasConverter(valueType, slotType)
				|| options.HasTypeConverter(valueNotnull, slotNotnull);

			switch (valueType)
			{
				case PrimitiveType.Integer:
				case PrimitiveType.Float:
					if (!allowConvert && !options.MixNumber)
						return -1;

					if (allowConvert || !options.MixValueType || !slotNotnull.IsValueType
						|| Marshal.SizeOf(value) != Marshal.SizeOf(slotNotnull))
					{
						if (!allowConvert && !(slotType switch
						{
							PrimitiveType.Pointer => options.MixPointer,
							PrimitiveType.Boolean => options.MixBoolean,
							PrimitiveType.Character => options.MixCharacter,
							PrimitiveType.String => options.MixStringIn,
							_ => false,
						}))
							return -1;

						depth = EvaluatePrimitive(value, slot);
					}
					else
					{
						depth = 3;

						if (valueNotnull != value)
							depth++;
						if (slotNotnull != slot)
							depth++;
					}

					break;
				case PrimitiveType.Pointer:
					if (!allowConvert && !options.MixPointer)
						return -1;

					if (allowConvert || !options.MixValueType || !slotNotnull.IsValueType
						|| IntPtr.Size != Marshal.SizeOf(slotNotnull))
					{
						if (!allowConvert && !(slotType switch
						{
							PrimitiveType.Integer => options.MixNumber,
							PrimitiveType.Float => options.MixNumber,
							PrimitiveType.Boolean => options.MixBoolean,
							PrimitiveType.Character => options.MixCharacter,
							PrimitiveType.String => options.MixStringIn,
							_ => false,
						}))
							return -1;

						depth = EvaluatePrimitive(value, slot);
					}
					else
					{
						depth = 3;

						if (valueNotnull != value)
							depth++;
						if (slotNotnull != slot)
							depth++;
					}

					break;
				case PrimitiveType.Boolean:
					if (!allowConvert && !options.MixBoolean)
						return -1;

					if (allowConvert || !options.MixValueType || !slotNotnull.IsValueType
						|| sizeof(bool) != Marshal.SizeOf(slotNotnull))
					{
						if (!allowConvert && !(slotType switch
						{
							PrimitiveType.Integer => options.MixNumber,
							PrimitiveType.Float => options.MixNumber,
							PrimitiveType.String => options.MixStringIn,
							_ => false,
						}))
							return -1;

						depth = EvaluatePrimitive(value, slot);
					}
					else
					{
						depth = 3;

						if (valueNotnull != value)
							depth++;
						if (slotNotnull != slot)
							depth++;
					}

					break;
				case PrimitiveType.Character:
					if (!allowConvert && !options.MixCharacter)
						return -1;

					if (allowConvert || !options.MixValueType || !slotNotnull.IsValueType
						|| sizeof(char) != Marshal.SizeOf(slotNotnull))
					{
						if (!allowConvert && !(slotType switch
						{
							PrimitiveType.Integer => options.MixNumber,
							PrimitiveType.Float => options.MixNumber,
							PrimitiveType.Boolean => options.MixBoolean,
							PrimitiveType.String => options.MixStringIn,
							_ => false,
						}))
							return -1;

						depth = EvaluatePrimitive(value, slot);
					}
					else
					{
						depth = 3;

						if (valueNotnull != value)
							depth++;
						if (slotNotnull != slot)
							depth++;
					}

					break;
				case PrimitiveType.String:
					if (!allowConvert && !options.MixStringOut)
						return -1;

					if (!allowConvert && !(slotType switch
					{
						PrimitiveType.Integer => options.MixNumber,
						PrimitiveType.Float => options.MixNumber,
						PrimitiveType.Pointer => options.MixPointer,
						PrimitiveType.Boolean => options.MixBoolean,
						PrimitiveType.Character => options.MixCharacter,
						_ => false,
					}))
						return -1;

					depth = EvaluatePrimitive(value, slot);
					break;
				default:
					if (allowConvert)
					{
						depth = 6;

						if (valueNotnull != value)
							depth++;
						if (slotNotnull != slot)
							depth++;
					}
					else if (valueNotnull.IsValueType && slotNotnull.IsValueType
						&& Marshal.SizeOf(valueNotnull) == Marshal.SizeOf(slotNotnull))
					{
						depth = 3;

						if (valueNotnull != value)
							depth++;
						if (slotNotnull != slot)
							depth++;
					}
					else
						return -1;

					break;
			}

			if (depth < 0)
			{
				if (!allowConvert)
					return -1;

				depth = 6;

				if (valueNotnull != value)
					depth++;
				if (slotNotnull != slot)
					depth++;
			}

			return depth << 5;
		}

		private static object SameSizeConvert(object value, Type slot)
		{
			int size = Marshal.SizeOf(slot);
			nint ptr = Marshal.AllocHGlobal(size);

			if (ptr == nint.Zero)
				throw new InsufficientMemoryException(ET("无法分配结构体内存"));

			try
			{
				Marshal.StructureToPtr(value, ptr, false);
				var result = Marshal.PtrToStructure(ptr, slot)
					?? throw new ImpossibleException();
				Marshal.DestroyStructure(ptr, value.GetType());
				return result;
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}

		private static bool TryConvertToPointer([NotNullWhen(true)] ref object? value, Type slot)
		{
			nint ptrValue = nint.Zero;

			if (value is not null)
			{
				if (IsMeaninglessConvert(value.GetType(), slot))
					return false;

				if (value is string strValue)
				{
					if (strValue.StartsWith("0x"))
					{
						if (nint.Size == 4)
						{
							try
							{
								ptrValue = Convert.ToInt32(strValue[2..], 16);
								return true;
							}
							catch
							{
								return false;
							}
						}
						else if (nint.Size == 8)
						{
							try
							{
								ptrValue = (nint)Convert.ToInt64(strValue[2..], 16);
							}
							catch
							{
								return false;
							}
						}
						else
							throw new PlatformNotSupportedException(ET("当前系统的指针大小不支持"));
					}

					return false;
				}

				if (value is not IConvertible convertible)
					return false;

				if (nint.Size == 4)
					ptrValue = convertible.ToInt32(null);
				else if (nint.Size == 4)
					ptrValue = (nint)convertible.ToInt64(null);
				else
					throw new PlatformNotSupportedException(ET("当前系统的指针大小不支持"));
			}

			if (slot == typeof(nint))
				value = ptrValue;
			else if (slot == typeof(nuint))
				value = (nuint)ptrValue;
			else if (slot.IsPointer)
				value = ptrValue;
			else
				throw new ArgumentException(ET("变量类型应该是指针类型"));

			return true;
		}

		private static bool TryConvertFromPointer([NotNullWhen(true)] ref object value, Type slot)
		{
			if (IsMeaninglessConvert(value.GetType(), slot))
				return false;

			nint ptrValue;

			if (value is nint nintValue)
				ptrValue = nintValue;
			else if (value is nuint nuintValue)
				ptrValue = (nint)nuintValue;
			else
				throw new ArgumentException(ET("无效的指针值"));

			if (slot == typeof(string))
			{
				if (nint.Size == 4)
					value = string.Format("0x{0:X08}", ptrValue.ToInt32());
				else if (nint.Size == 8)
					value = string.Format("0x{0:X016}", ptrValue.ToInt64());
				else
					throw new PlatformNotSupportedException(ET("当前系统的指针大小不支持"));

				return true;
			}

			if (!slot.IsAssignableTo(typeof(IConvertible)))
				return false;

			if (nint.Size == 4)
				value = ((IConvertible)ptrValue.ToInt32()).ToType(slot, null);
			else if (nint.Size == 8)
				value = ((IConvertible)ptrValue.ToInt64()).ToType(slot, null);
			else
				throw new PlatformNotSupportedException(ET("当前系统的指针大小不支持"));

			return true;
		}

		/// <summary>
		/// 根据基元类型或数字类型的大小分级：<br/>
		/// 一个字节是一级，两个字节是二级，四个字节是三级，依此类推...
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static int GetPrimitiveLevel(Type type)
		{
			if (!type.IsPrimitive && !type.IsNumberType())
				throw new ArgumentException(ET("类型应当是基元类型或数字类型"));

			return int.Log2(Marshal.SizeOf(type)) + 1;
		}

		private static Type GetNotnullableType(this Type type)
		{
			return Nullable.GetUnderlyingType(type) ?? type;
		}

		private static bool IsMeaninglessConvert(Type value, Type slot)
		{
			if (value == typeof(bool))
			{
				if (slot == typeof(char)
					|| slot == typeof(nint)
					|| slot == typeof(nuint)
					|| slot.IsPointer)
					return true;
			}
			else if (value == typeof(char))
			{
				if (slot == typeof(nint)
					|| slot == typeof(nuint)
					|| slot.IsPointer)
					return true;
			}
			else if (value.IsPointer)
			{
				if (slot == typeof(byte)
					|| slot == typeof(sbyte)
					|| slot == typeof(short)
					|| slot == typeof(ushort)
					|| slot == typeof(char)
					|| slot == typeof(Half))
					return true;
			}
			else if (slot.IsPointer)
			{
				if (value == typeof(byte)
					|| value == typeof(sbyte)
					|| value == typeof(short)
					|| value == typeof(ushort)
					|| value == typeof(Half))
					return true;
			}

			return false;
		}

		private static int EvaluatePrimitive(Type? value, Type? slot, int appendDepth = 0)
		{
			if (value == slot)
				return appendDepth;
			if (value is null || slot is null)
				return -1;
			if (IsMeaninglessConvert(value, slot))
				return -1;

			var valueType = value.GetPrimitiveType();
			var slotType = slot.GetPrimitiveType();

			if (valueType == PrimitiveType.Nullable)
			{
				if (slotType == PrimitiveType.Nullable)
					return EvaluatePrimitive(Nullable.GetUnderlyingType(value), Nullable.GetUnderlyingType(slot), appendDepth + 2);
				else
					return EvaluatePrimitive(Nullable.GetUnderlyingType(value), slot, appendDepth + 1);
			}
			else if (slotType == PrimitiveType.Nullable)
				return EvaluatePrimitive(value, Nullable.GetUnderlyingType(slot), appendDepth + 1);

			int valueLevel = GetPrimitiveLevel(value);
			int slotLevel = GetPrimitiveLevel(slot);

			int depth = Math.Abs(valueLevel - slotLevel) + 1;

			if (valueLevel > slotLevel)
				depth += 2;

			depth = valueType switch
			{
				PrimitiveType.Integer => slotType switch
				{
					PrimitiveType.Integer => depth,
					PrimitiveType.Float => depth + 1,
					PrimitiveType.Pointer => depth + 1,
					PrimitiveType.Boolean => depth + 1,
					PrimitiveType.Character => depth + 1,
					PrimitiveType.String => depth + 2,
					_ => -1,
				},
				PrimitiveType.Float => slotType switch
				{
					PrimitiveType.Integer => depth + 2,
					PrimitiveType.Float => depth,
					PrimitiveType.Pointer => depth + 2,
					PrimitiveType.Boolean => depth + 1,
					PrimitiveType.Character => depth + 2,
					PrimitiveType.String => depth + 2,
					_ => -1,
				},
				PrimitiveType.Pointer => slotType switch
				{
					PrimitiveType.Integer => depth + 1,
					PrimitiveType.Float => depth + 2,
					PrimitiveType.Pointer => depth,
					PrimitiveType.Boolean => depth + 1,
					PrimitiveType.String => depth + 2,
					_ => -1,
				},
				PrimitiveType.Boolean => slotType switch
				{
					PrimitiveType.Integer => depth + 1,
					PrimitiveType.Float => depth + 2,
					PrimitiveType.Boolean => depth,
					PrimitiveType.String => depth + 2,
					_ => -1,
				},
				PrimitiveType.Character => slotType switch
				{
					PrimitiveType.Integer => depth + 1,
					PrimitiveType.Float => depth + 2,
					PrimitiveType.Boolean => depth + 2,
					PrimitiveType.Character => depth,
					PrimitiveType.String => depth + 1,
					_ => -1,
				},
				PrimitiveType.String => slotType switch
				{
					PrimitiveType.Integer => depth + 1,
					PrimitiveType.Float => depth + 2,
					PrimitiveType.Boolean => depth + 2,
					PrimitiveType.Character => depth + 1,
					PrimitiveType.String => depth,
					_ => -1,
				},
				_ => -1,
			};

			if (depth < 0)
				return -1;

			return depth + appendDepth;
		}

		/// <summary>
		/// 判断给定类型在<see cref="GetPrimitiveLevel(Type)"/>中返回的是否是<see cref="PrimitiveType.Integer"/>或<see cref="PrimitiveType.Float"/>
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsNumberType(this Type? type)
		{
			var primType = GetPrimitiveType(type);
			return primType == PrimitiveType.Integer || primType == PrimitiveType.Float;
		}

		/// <summary>
		/// 通过给定类型返回<see cref="PrimitiveType"/>的不同枚举值<br/>
		/// <see cref="PrimitiveType.Integer"/>：<br/>
		/// <see cref="byte"/>、
		/// <see cref="sbyte"/>、
		/// <see cref="short"/>、
		/// <see cref="ushort"/>、
		/// <see cref="int"/>、
		/// <see cref="uint"/>、
		/// <see cref="long"/>、
		/// <see cref="ulong"/>、
		/// <see cref="Int128"/>、
		/// <see cref="UInt128"/><br/>
		/// <see cref="PrimitiveType.Float"/>：<br/>
		/// <see cref="Half"/>、
		/// <see cref="float"/>、
		/// <see cref="double"/>、
		/// <see cref="decimal"/><br/>
		/// <see cref="PrimitiveType.Pointer"/>：<br/>
		/// <see cref="nint"/>、
		/// <see cref="nuint"/>、
		/// <see cref="void"/>* （所有指针类型）<br/>
		/// <see cref="PrimitiveType.Boolean"/>：<br/>
		/// <see cref="bool"/><br/>
		/// <see cref="PrimitiveType.Character"/>：<br/>
		/// <see cref="char"/><br/>
		/// <see cref="PrimitiveType.String"/>：<br/>
		/// <see cref="string"/><br/>
		/// <see cref="PrimitiveType.Nullable"/>：<br/>
		/// <see cref="Nullable{T}"/>包裹的任意类型<br/>
		/// 其他类型均返回<see cref="PrimitiveType.Unknown"/>
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static PrimitiveType GetPrimitiveType(this Type? type)
		{
			if (type is null)
				return PrimitiveType.Unknown;

			if (type == typeof(byte)
				|| type == typeof(sbyte)
				|| type == typeof(short)
				|| type == typeof(ushort)
				|| type == typeof(int)
				|| type == typeof(uint)
				|| type == typeof(long)
				|| type == typeof(ulong)
				|| type == typeof(Int128)
				|| type == typeof(UInt128))
				return PrimitiveType.Integer;

			if (type == typeof(Half)
				|| type == typeof(float)
				|| type == typeof(double)
				|| type == typeof(decimal))
				return PrimitiveType.Float;

			if (type == typeof(nint)
				|| type == typeof(nuint)
				|| type.IsPointer)
				return PrimitiveType.Pointer;

			if (type == typeof(bool))
				return PrimitiveType.Boolean;

			if (type == typeof(char))
				return PrimitiveType.Character;

			if (type == typeof(string))
				return PrimitiveType.String;

			if (type.IsGenericType && !type.IsGenericTypeDefinition
				&& type.GetGenericTypeDefinition() == typeof(Nullable<>))
				return PrimitiveType.Nullable;

			return PrimitiveType.Unknown;
		}

		private sealed class MethodSorter : IComparer<int>
		{
			public static readonly MethodSorter Instance = new();

			public int Compare(int x, int y) => y.CompareTo(x);
		}

		[Obsolete("反射执行时不支持忽略参数")]
		private sealed class MethodParams
		{
			private readonly MethodBase m_Method;
			private readonly ParameterInfo[] m_Parameters;
			private readonly int m_RequiredCount;
			private readonly int m_OptionalCount;
			private readonly bool m_IsParamArray;
			private readonly Type? m_ParamArrayEleType;

			public MethodParams(MethodBase method)
			{
				m_Method = method;
				m_Parameters = method.GetParameters();

				int length = m_Parameters.Length;

				if (m_Method.GetCustomAttribute<ParamArrayAttribute>() is not null
					&& m_Parameters[length - 1].ParameterType.IsArray)
				{
					m_IsParamArray = true;
					m_ParamArrayEleType = m_Parameters[length - 1].ParameterType.GetElementType()
						?? throw new ImpossibleException(ET("给定的参数数组没有指定元素类型"));
					length--;
				}

				m_RequiredCount = length;

				for (int i = 0; i < length; i++)
				{
					if (m_Parameters[i].IsOptional)
					{
						m_RequiredCount = i;
						break;
					}
				}

				m_OptionalCount = length - m_RequiredCount;
			}

			public MethodBase Method => m_Method;

			public Type ReturnType => m_Method is MethodInfo methodInfo
				? methodInfo.ReturnType : typeof(void);

			public int Required => m_RequiredCount;

			public int Optional => m_OptionalCount;

			public bool IsParamArray => m_IsParamArray;

			public Type? ParamArrayElement => m_ParamArrayEleType;

			public Type ParamTypeAt(int index) => m_Parameters[index].ParameterType;

			public object? DefaultValueAt(int index)
			{
				if (index < m_RequiredCount
					|| index >= m_RequiredCount + m_OptionalCount)
					throw new ArgumentOutOfRangeException(nameof(index));

				var defValue = m_Parameters[index].DefaultValue;

				if (defValue == DBNull.Value)
					//return Missing.Value;
					throw new InvalidOperationException(ET("指定的参数没有默认值"));

				return defValue;
			}

			public bool IsAcceptableAt(int index, Type? type)
			{
				var pType = m_Parameters[index].ParameterType;

				if (type is null)
				{
					return !pType.IsValueType
						|| Nullable.GetUnderlyingType(pType) is not null;
				}

				if (type.IsAssignableTo(pType)
					|| (pType.IsByRef && pType.GetElementType() == type))
					return true;

				if (m_ParamArrayEleType is not null
					&& m_Parameters.Length - 1 == index)
				{
					if (type.IsAssignableTo(m_ParamArrayEleType))
						return true;
				}

				return false;
			}

			public bool IsAcceptable(int parameterCount)
			{
				if (parameterCount < m_RequiredCount)
					return false;
				if (m_IsParamArray)
					return true;
				if (parameterCount > m_RequiredCount + m_OptionalCount)
					return false;

				return true;
			}

			public bool IsAcceptable(Type?[]? types)
			{
				if (types is null || types.Length == 0)
					return IsAcceptable(0);

				for (int i = types.Length - 1; i >= 0; i--)
				{
					if (!IsAcceptableAt(i, types[i]))
						return false;
				}

				return true;
			}

			public bool IsAcceptable(object?[]? args)
			{
				if (args is null || args.Length == 0)
					return IsAcceptable(0);

				for (int i = args.Length - 1; i >= 0; i--)
				{
					if (!IsAcceptableAt(i, args[i]?.GetType()))
						return false;
				}

				return true;
			}
		}
	}

	public delegate bool ArgumentConverter(ref object? value, Type slot);

	/// <summary>
	/// 提供参数在类型转换的规则<br/>
	/// 通过指定属性或手动调用<see cref="AddConverter(PrimitiveType, PrimitiveType, ArgumentConverter)"/>来添加隐式转换<br/>
	/// <br/>
	/// 请注意，通过指定属性来启用隐式转换是需要双方统一的，例如：<br/>
	/// 要启用布尔类型到字符串类型的隐式转换，应同时指定<see cref="MixBoolean"/>与<see cref="MixStringIn"/>为<see langword="true"/><br/>
	/// 要启用字符串类型到数字类型的隐式转换，应同时指定<see cref="MixNumber"/>与<see cref="MixStringOut"/>为<see langword="true"/><br/>
	/// 要启用数字类型与布尔类型互相的隐式转换，应同时指定<see cref="MixNumber"/>与<see cref="MixBoolean"/>为<see langword="true"/><br/>
	/// <br/>
	/// 另外，有些隐式转换是无意义的，即使对应的参数被指定也将被判断为无法契合<br/>
	/// 无意义的隐式转换如下：<br/>
	/// <see cref="bool"/> -> <see cref="char"/>、
	/// <see cref="bool"/> -> <see cref="nint"/>、
	/// <see cref="bool"/> -> <see cref="nuint"/>、
	/// <see cref="bool"/> -> <see cref="void"/>* (所有指针)、
	/// <see cref="char"/> -> <see cref="nint"/>、
	/// <see cref="char"/> -> <see cref="nuint"/>、
	/// <see cref="char"/> &lt;-> <see cref="void"/>* (所有指针)、
	/// <see cref="void"/>* (所有指针) &lt;-> <see cref="byte"/>、
	/// <see cref="void"/>* (所有指针) &lt;-> <see cref="sbyte"/>、
	/// <see cref="void"/>* (所有指针) &lt;-> <see cref="short"/>、
	/// <see cref="void"/>* (所有指针) &lt;-> <see cref="ushort"/>、
	/// <see cref="void"/>* (所有指针) &lt;-> <see cref="Half"/>
	/// </summary>
	public sealed class ConvertOptions()
	{
		private Dictionary<(PrimitiveType, PrimitiveType), ArgumentConverter>? m_ConverterTable = null;
		private Dictionary<(Type, Type), ArgumentConverter>? m_TypeConverterTable = null;
		private bool m_Sealed = false;

		private bool m_MixNumber = false;
		private bool m_MixBoolean = false;
		private bool m_MixCharacter = false;
		private bool m_MixStringIn = false;
		private bool m_MixStringOut = false;
		private bool m_MixPointer = false;
		private bool m_MixValueType = false;

		/// <summary>
		/// 判断当前对象是否冻结
		/// </summary>
		public bool IsSealed => m_Sealed;

		/// <summary>
		/// 冻结当前对象，让当前对象不可逆的变成只读对象
		/// </summary>
		public ConvertOptions Seal()
		{
			m_Sealed = true;
			return this;
		}

		private void CheckSealed()
		{
			if (m_Sealed)
				throw new InvalidOperationException(ET("当前对象已经被冻结"));
		}

		/// <summary>
		/// 添加一个转换器作为隐式转换，以此自定义转换方式，并达到单独启用一种转换的效果
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="converter"></param>
		public void AddConverter(PrimitiveType from, PrimitiveType to, ArgumentConverter converter)
		{
			if (from == PrimitiveType.Nullable)
				throw new ArgumentException(ET("无效的转换类型: {0}", from));
			if (to == PrimitiveType.Nullable)
				throw new ArgumentException(ET("无效的转换类型: {0}", to));

			CheckSealed();
			m_ConverterTable ??= [];
			m_ConverterTable[(from, to)] = converter;
		}

		/// <summary>
		/// 添加一个类型转换器作为隐式转换，以此自定义转换方式，并达到单独启用一种转换的效果
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <param name="converter"></param>
		public void AddTypeConverter(Type from, Type to, ArgumentConverter converter)
		{
			if (from == to)
				throw new ArgumentException(ET("转换前后的类型不能相同: {0}", from.FullName));

			CheckSealed();
			m_TypeConverterTable ??= [];
			m_TypeConverterTable[(from, to)] = converter;
		}

		/// <summary>
		/// 判断是否包含指定的转换器
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		public bool HasConverter(PrimitiveType from, PrimitiveType to)
		{
			if (m_ConverterTable is null)
				return false;

			return m_ConverterTable.ContainsKey((from, to));
		}

		/// <summary>
		/// 判断是否包含指定的类型转换器
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		/// <returns></returns>
		public bool HasTypeConverter(Type from, Type to)
		{
			if (m_TypeConverterTable is null)
				return false;

			return m_TypeConverterTable.ContainsKey((from, to));
		}

		/// <summary>
		/// 尝试调用添加的转换器，如果转换器不存在、转换器发生异常或者转换器返回<see langword="false"/>，都将导致此方法返回<see langword="false"/>
		/// </summary>
		/// <returns></returns>
		public bool TryConvert(PrimitiveType from, PrimitiveType to, ref object? value, Type slot)
		{
			if (m_ConverterTable is null)
				return false;

			if (!m_ConverterTable.TryGetValue((from, to), out var converter))
				return false;

			try
			{
				return converter.Invoke(ref value, slot);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// 尝试调用添加的类型转换器，如果类型转换器不存在、类型转换器发生异常或者类型转换器返回<see langword="false"/>，都将导致此方法返回<see langword="false"/>
		/// </summary>
		/// <returns></returns>
		public bool TryTypeConvert(Type from, Type to, ref object? value, Type slot)
		{
			if (m_TypeConverterTable is null)
				return false;

			if (!m_TypeConverterTable.TryGetValue((from, to), out var converter))
				return false;

			try
			{
				return converter.Invoke(ref value, slot);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// 指定是否允许数字类型之间以及数字类型与其他类型的隐式转换
		/// </summary>
		public bool MixNumber
		{
			get => m_MixNumber;
			set
			{
				CheckSealed();
				m_MixNumber = value;
			}
		}

		/// <summary>
		/// 指定是否允许布尔类型与其他类型的隐式转换
		/// </summary>
		public bool MixBoolean
		{
			get => m_MixBoolean;
			set
			{
				CheckSealed();
				m_MixBoolean = value;
			}
		}

		/// <summary>
		/// 指定是否允许字符类型与其他类型的隐式转换
		/// </summary>
		public bool MixCharacter
		{
			get => m_MixCharacter;
			set
			{
				CheckSealed();
				m_MixCharacter = value;
			}
		}

		/// <summary>
		/// 指定是否允许其他类型隐式转换到字符串类型
		/// </summary>
		public bool MixStringIn
		{
			get => m_MixStringIn;
			set
			{
				CheckSealed();
				m_MixStringIn = value;
			}
		}

		/// <summary>
		/// 指定是否允许字符串类型隐式转换到其他类型
		/// </summary>
		public bool MixStringOut
		{
			get => m_MixStringOut;
			set
			{
				CheckSealed();
				m_MixStringOut = value;
			}
		}

		/// <summary>
		/// 指定是否允许<see cref="nint"/>、<see cref="nuint"/>以及所有的指针类型与其他类型的隐式转换
		/// </summary>
		public bool MixPointer
		{
			get => m_MixPointer;
			set
			{
				CheckSealed();
				m_MixPointer = value;
			}
		}

		/// <summary>
		/// 指定是否允许相同大小的值类型进行互相的隐式转换
		/// </summary>
		public bool MixValueType
		{
			get => m_MixValueType;
			set
			{
				CheckSealed();
				m_MixValueType = value;
			}
		}
	}

	public enum PrimitiveType
	{
		Integer,
		Float,
		Pointer,
		Boolean,
		Character,
		String,
		Nullable,
		Unknown,
	}
}
