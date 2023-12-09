using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供动态的委托类型生成方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadUnsafe]
	public sealed class DelegateBuilder
	{
		private static readonly ConstructorInfo m_MarshalAs_Ctor;

		private static readonly AssemblyBuilder m_AssemblyBuilder;
		private static readonly ModuleBuilder m_ModuleBuilder;

		private static volatile int m_TypeId = 0xA00A00;

		private volatile bool m_Created = false;
		private readonly string m_Name;
		private readonly DelegateParameter m_ReturnParamter;
		private readonly List<DelegateParameter> m_Parameters = [];

		static DelegateBuilder()
		{
			DynamicSupport.AssertDynamicSupport();

			string prefix = typeof(DelegateBuilder).FullName ?? nameof(DelegateBuilder);
			m_MarshalAs_Ctor = typeof(MarshalAsAttribute).ExtractCtor([typeof(UnmanagedType)]);

			m_AssemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
				new AssemblyName($"{prefix}@DynamicDelegates"),
				AssemblyBuilderAccess.RunAndCollect);
			m_ModuleBuilder = m_AssemblyBuilder.DefineDynamicModule($"{prefix}@DynamicDelegates");
		}

		/// <summary>
		/// 创建委托构造器并指定委托名称和委托返回值
		/// </summary>
		/// <param name="name"></param>
		/// <param name="retrunParameter"></param>
		public DelegateBuilder(string name, DelegateParameter retrunParameter)
		{
			retrunParameter.Attributes |= ParameterAttributes.Retval;

			m_Name = name;
			m_ReturnParamter = retrunParameter;
		}

		/// <summary>
		/// 创建委托构造器并指定委托名称和返回值类型
		/// </summary>
		/// <param name="name"></param>
		/// <param name="returnType"></param>
		public DelegateBuilder(string name, Type returnType)
		{
			m_Name = name;
			m_ReturnParamter = new DelegateParameter
			{
				Type = returnType,
				Name = null,
				Attributes = ParameterAttributes.Retval,
				MarshalAs = null,
				DefaultValue = null,
			};
		}

		/// <summary>
		/// 创建委托构造器并指定委托名称和ref返回值类型
		/// </summary>
		/// <param name="name"></param>
		/// <param name="returnType"></param>
		public DelegateBuilder(string name, ref Type returnType)
			: this(name, returnType.MakeByRefType()) { }

		/// <summary>
		/// 创建委托构造器并指定委托名称和void返回值类型
		/// </summary>
		/// <param name="name"></param>
		public DelegateBuilder(string name)
			: this(name, typeof(void)) { }

		private void CheckNotCreated()
		{
			if (m_Created)
				throw new InvalidOperationException(ET("当前的委托类型已经创建"));
		}

		/// <summary>
		/// 添加一个参数
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public DelegateParameter DefineParameter(string? name, Type type)
		{
			CheckNotCreated();

			var param = new DelegateParameter
			{
				Type = type,
				Name = name,
			};

			m_Parameters.Add(param);
			return param;
		}

		/// <summary>
		/// 添加一个参数
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public DelegateParameter DefineParameter(Type type)
			=> DefineParameter(null, type);

		/// <summary>
		/// 添加一个ref参数
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public DelegateParameter DefineRefParameter(string? name, Type type)
		{
			CheckNotCreated();

			var param = new DelegateParameter
			{
				Type = type.MakeByRefType(),
				Name = name,
			};

			m_Parameters.Add(param);
			return param;
		}

		/// <summary>
		/// 添加一个in参数
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public DelegateParameter DefineInParameter(string? name, Type type)
		{
			CheckNotCreated();

			var param = new DelegateParameter
			{
				Type = type.MakeByRefType(),
				Name = name,
				Attributes = ParameterAttributes.In,
			};

			m_Parameters.Add(param);
			return param;
		}

		/// <summary>
		/// 添加一个out参数
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public DelegateParameter DefineOutParameter(string? name, Type type)
		{
			CheckNotCreated();

			var param = new DelegateParameter
			{
				Type = type.MakeByRefType(),
				Name = name,
				Attributes = ParameterAttributes.Out,
			};

			m_Parameters.Add(param);
			return param;
		}

		/// <summary>
		/// 创建委托类型
		/// </summary>
		/// <returns></returns>
		public Type CreateDelegate()
		{
			var type = CreateDelegate(m_Name, m_ReturnParamter, [.. m_Parameters]);
			m_Created = true;
			return type;
		}

		private static string GenerateName(string name)
		{
			return $"DynamicDelegate.Generation{Interlocked.Increment(ref m_TypeId)}.{name}";
		}

		private static Type[] GetParameterTypes(DelegateParameter[] parameters)
		{
			Type[] types = new Type[parameters.Length];

			for (int i = parameters.Length - 1; i >= 0; i--)
				types[i] = parameters[i].Type;

			return types;
		}

		private static void ExtractAttributeValues(Attribute attribute, string[] names,
			List<FieldInfo> outFields, List<object?> outFieldValues,
			List<PropertyInfo> outProperties, List<object?> outPropertyValues)
		{
			Type type = attribute.GetType();

			foreach (var name in names)
			{
				var field = type.GetField(name);

				if (field is null || field.IsStatic)
				{
					var property = type.GetProperty(name)
						?? throw new MissingMemberException(string.Format("Property or field '{0}' not found in type '{1}'", name, type));

					var getMethod = property.GetGetMethod();
					if (getMethod is null || getMethod.IsStatic || getMethod.GetParameters().Length > 0)
						throw new MissingMemberException(string.Format("Property or field '{0}' not found in type '{1}'", name, type));

					var defaultValue = property.PropertyType.GetDefaultValue();
					var value = property.GetValue(attribute, null);

					if (value != defaultValue)
					{
						outProperties.Add(property);
						outPropertyValues.Add(value);
					}
				}
				else
				{
					var defaultValue = field.FieldType.GetDefaultValue();
					var value = field.GetValue(attribute);

					if (value != defaultValue)
					{
						outFields.Add(field);
						outFieldValues.Add(value);
					}
				}
			}
		}

		private static void SetMarshalAs(ParameterBuilder parameterBuilder, MarshalAsAttribute marshalAs)
		{
			var fields = new List<FieldInfo>();
			var fieldValues = new List<object?>();
			var properties = new List<PropertyInfo>();
			var propertyValues = new List<object?>();

			ExtractAttributeValues(marshalAs,
			[
				"SafeArraySubType",
				"SafeArrayUserDefinedSubType",
				"IidParameterIndex",
				"ArraySubType",
				"SizeParamIndex",
				"SizeConst",
				"MarshalType",
				"MarshalTypeRef",
				"MarshalCookie",
			], fields, fieldValues, properties, propertyValues);

			parameterBuilder.SetCustomAttribute(
				new CustomAttributeBuilder(
					m_MarshalAs_Ctor, new object[] { marshalAs.Value },
					[.. properties], [.. propertyValues],
					[.. fields], [.. fieldValues]
				)
			);
		}

		private static MarshalAsAttribute MakeMarshalAs(string optionStr, int errorPos)
		{
			string[] items = optionStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (items.Length > 2)
				throw new FormatException($"Too many marshal option item:unmanaged type and size are allowed only(pos:{errorPos})");

			UnmanagedType unmanagedType;

			if (int.TryParse(items[0], out int unmanagedTypeInt))
				unmanagedType = (UnmanagedType)unmanagedTypeInt;
			else if (!Enum.TryParse(items[0], out unmanagedType))
				throw new InvalidEnumArgumentException($"Unknown unmanaged type value:'{items[0]}'(pos:{errorPos})");

			if (items.Length < 2)
				return new MarshalAsAttribute(unmanagedType);

			if (!int.TryParse(items[1], out int size) || size <= 0)
				throw new FormatException($"Size should be positive decimal but it is '{items[1]}'(pos:{errorPos})");

			return new MarshalAsAttribute(unmanagedType) { SizeConst = size };
		}

		private static int CountRepeated(string str, int pos, char ch)
		{
			for (int i = pos; i < str.Length; i++)
			{
				if (str[i] != ch)
					return i - pos;
			}

			return str.Length - pos;
		}

		private static int MeasureNextInt(string str, int pos)
		{
			int end = pos;

			for (int i = pos; i < str.Length; i++)
			{
				if (char.IsDigit(str[i]))
					end = i + 1;
				else
					break;
			}

			return end - pos;
		}

		/// <summary>
		/// 为方法生成不带封送标注的字符串方法签名<br/>
		/// 方法签名规则参考<see cref="CreateDelegate(string, string, Type[])"/><br/>
		/// 当方法签名带有 't0' 这类 'tx' 的不定类型，将在签名后追加不定类型列表<br/>
		/// 例如: Type (string, bool) -> $"t0($?);{typeof(Type).AssemblyQualifiedName}" (追加以 ';' 分隔的带程序集全名的类型名)
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static string GenerateSignature(MethodBase method, bool includeThis = false)
		{
			if (method.IsGenericMethodDefinition)
				throw new NotSupportedException(ET("无法对泛型方法定义生成签名"));

			var sb = new StringBuilder();
			var types = new List<Type>();

			Type returnType;

			if (method is MethodInfo methodInfo)
				returnType = methodInfo.ReturnType;
			else
				returnType = typeof(void);

			AppendTypeSignature(sb, returnType, types);

			sb.Append('(');

			if (includeThis && !method.IsStatic)
			{
				var declaring = method.DeclaringType;

				if (declaring is not null)
					AppendTypeSignature(sb, declaring, types);
			}

			var ps = method.GetParameters();

			foreach (var p in ps)
				AppendTypeSignature(sb, p.ParameterType, types);

			sb.Append(')');

			if (types.Count > 0)
			{
				foreach (var type in types)
					sb.Append(';').Append(type.AssemblyQualifiedName);
			}

			return sb.ToString();
		}

		/// <summary>
		/// 将携带不定类型列表的签名中的不定类型列表分离到类型数组
		/// </summary>
		/// <param name="signature"></param>
		/// <returns></returns>
		/// <exception cref="ImpossibleException"></exception>
		public static (string, Type[]) SplitSignature(string signature)
		{
			string[] sigItems = signature.Split(';');
			string sigBody = sigItems[0];
			var types = new List<Type>();

			for (int i = 1; i < sigItems.Length; i++)
			{
				types.Add(Type.GetType(sigItems[i])
					?? throw new ImpossibleException(ET("签名的程序集类型全名无效: {0}", sigItems[i])));
			}

			return (sigBody, [.. types]);
		}

		private static void AppendTypeSignature(StringBuilder sb, Type type, List<Type> types)
		{
			var isb = new StringBuilder();

			while (true)
			{
				if (type.IsByRef)
					isb.Insert(0, '&');
				else if (type.IsArray)
					isb.Insert(0, ']');
				else if (type.IsPointer)
					isb.Insert(0, '*');
				else
					break;

				type = type.GetElementType()
					?? throw new ImpossibleException(ET("给定类型应有元素类型但是元素类型为空: {0}", type.FullName));
			}

			if (type == typeof(void))
				isb.Append('v');
			else if (type == typeof(bool))
				isb.Append('?');
			else if (type == typeof(byte))
				isb.Append('b');
			else if (type == typeof(sbyte))
				isb.Append('B');
			else if (type == typeof(char))
				isb.Append('c');
			else if (type == typeof(short))
				isb.Append('s');
			else if (type == typeof(ushort))
				isb.Append('S');
			else if (type == typeof(int))
				isb.Append('i');
			else if (type == typeof(uint))
				isb.Append('I');
			else if (type == typeof(long))
				isb.Append('l');
			else if (type == typeof(ulong))
				isb.Append('L');
			else if (type == typeof(float))
				isb.Append('f');
			else if (type == typeof(double))
				isb.Append('d');
			else if (type == typeof(nint))
				isb.Append('p');
			else if (type == typeof(object))
				isb.Append('@');
			else if (type == typeof(string))
				isb.Append('$');
			else
			{
				int index = types.IndexOf(type);

				if (index < 0)
				{
					index = types.Count;
					types.Add(type);
				}

				isb.Append('t').Append(index);
			}

			sb.Append(isb);
		}

		/// <summary>
		/// 通过给定字符串签名生成对应委托类型<br/>
		/// 此方法支持签名追加不定类型列表, 可以直接接受方法<see cref="GenerateSignature(MethodBase)"/>的返回值
		/// </summary>
		/// <param name="name"></param>
		/// <param name="signature"></param>
		/// <returns></returns>
		/// <exception cref="ImpossibleException"></exception>
		public static Type CreateDelegate(string name, string signature)
		{
			var (sigBody, types) = SplitSignature(signature);
			return CreateDelegate(name, sigBody, types);
		}

		/// <summary>
		/// 通过给定字符串签名生成对应委托类型<br/>
		/// 委托参数定义语法:<br/>
		/// v -> void, ? -> bool, b -> byte, B -> sbyte,<br/>
		/// c -> char, s -> short, S -> ushort, i -> int,<br/>
		/// I -> uint, l -> long, L -> ulong, f -> float,<br/>
		/// d -> double, p -> nint, @ -> object, $ -> string,<br/>
		/// t0 -> <paramref name="refTypes"/>[0], tx -> <paramref name="refTypes"/>[x],<br/>
		/// m[Bool] -> MarshalAs(<see cref="UnmanagedType.Bool"/>) (封送标注),<br/>
		/// m[xxx] -> MarshalAs(<see cref="UnmanagedType"/>.xxx) (封送标注),<br/>
		/// * -> 指针类型标记, &amp; -> 引用类型标记,<br/>
		/// ] -> 一维数组标记, ]] -> 二维数组标记, ...<br/>
		/// <br/>
		/// 定义优先级: m[xxx] -> &amp; -> * 或 ] -> type<br/>
		/// 例如: [MarshalAs(<see cref="UnmanagedType.U2"/>)] char*[]**&amp; 应该写为 m[U2]&amp;**]*c<br/>
		/// <br/>
		/// 用法示例:<br/>
		/// v($$i) -> void (string, string, int);<br/>
		/// m[Bool]i(p*i) -> MarshalAs(<see cref="UnmanagedType.Bool"/>) int (IntPtr, int*)<br/>
		/// ]]]b($]]i) -> byte[][][] (string, int[][])<br/>
		/// &amp;]b(dd) -> byte[]&amp; (double, double)
		/// </summary>
		/// <param name="name"></param>
		/// <param name="signature"></param>
		/// <param name="refTypes"></param>
		/// <returns></returns>
		public static Type CreateDelegate(string name, string signature, params Type[] refTypes)
		{
			ArgumentNullException.ThrowIfNull(name);
			ArgumentNullException.ThrowIfNull(signature);
			ArgumentNullException.ThrowIfNull(refTypes);

			DelegateParameter? returnParam = null;

			var paramList = new List<DelegateParameter>();

			bool startParams = false;
			bool endParams = false;

			bool refParam = false;
			var markList = new List<char>();

			MarshalAsAttribute? marshalAs = null;

			int pos = 0;
			char ch;

			int endPos;

			while (pos < signature.Length)
			{
				if (endParams)
					throw new FormatException(ET("参数列表之后不应该再包含任何字符"));

				ch = signature[pos];

				if (ch == 'm')
				{
					if (marshalAs != null)
						throw new FormatException(ET("重复的封送标记"));

					if (pos + 2 >= signature.Length || signature[pos + 1] != '[')
						throw new FormatException(ET("封送标记没有携带封送选项列表"));

					pos += 2;
					endPos = signature.IndexOf(']', pos);
					if (endPos < 0)
						throw new FormatException(ET("封送选项列表没有关闭"));

					marshalAs = MakeMarshalAs(signature[pos..endPos], pos - 2);
					pos = endPos + 1;
					continue;
				}
				else if (ch == '*')
				{
					if (refParam)
						throw new FormatException(ET("引用传参标记不能再添加指针标记"));

					markList.Add(ch);
				}
				else if (ch == '&')
				{
					if (refParam)
						throw new FormatException(ET("引用传参标记重复"));

					refParam = true;
				}
				else if (ch == ']')
				{
					if (refParam)
						throw new FormatException(ET("引用传参标记不能再添加数组标记"));

					markList.Add(ch);
				}
				else if (ch == '(')
				{
					if (startParams)
						throw new FormatException(ET("参数列表内不应该包含左括号"));

					startParams = true;
				}
				else if (ch == ')')
				{
					if (!startParams)
						throw new FormatException(ET("参数列表外不应该包含右括号"));
					if (marshalAs != null)
						throw new FormatException(ET("封送标记没有对应的修饰参数"));
					if (refParam)
						throw new FormatException(ET("引用传参标记 IN/OUT/REF 没有对应的修饰参数"));
					if (markList.Count > 0)
						throw new FormatException(ET("数组/指针标记没有对应的修饰参数"));

					endParams = true;
				}
				else
				{
					Type type;

					switch (ch)
					{
						case 'v':
							type = typeof(void);
							break;
						case '?':
							type = typeof(bool);
							break;
						case 'b':
							type = typeof(byte);
							break;
						case 'B':
							type = typeof(sbyte);
							break;
						case 's':
							type = typeof(short);
							break;
						case 'S':
							type = typeof(ushort);
							break;
						case 'i':
							type = typeof(int);
							break;
						case 'I':
							type = typeof(uint);
							break;
						case 'l':
							type = typeof(long);
							break;
						case 'L':
							type = typeof(ulong);
							break;
						case 'f':
							type = typeof(float);
							break;
						case 'd':
							type = typeof(double);
							break;
						case 'p':
							type = typeof(IntPtr);
							break;
						case 'P':
							type = typeof(UIntPtr);
							break;
						case '@':
							type = typeof(object);
							break;
						case '$':
							type = typeof(string);
							break;
						case 't':
							int nextIntLen = MeasureNextInt(signature, pos + 1);

							if (nextIntLen <= 0)
								throw new FormatException(ET("类型引用没有给出引用索引"));

							int nextInt = int.Parse(signature.Substring(pos + 1, nextIntLen));

							if (nextInt >= refTypes.Length)
								throw new FormatException(ET("给定的类型引用没有找到"));

							type = refTypes[nextInt];
							pos += nextIntLen;
							break;
						default:
							throw new FormatException(ET("无效的标记字符: {0}", ch));
					}

					if (markList.Count > 0)
					{
						for (int i = markList.Count - 1; i >= 0; i--)
						{
							char mark = markList[i];

							if (mark == '*')
								type.MakePointerType();
							else if (mark == ']')
								type.MakeArrayType();
						}
					}

					if (refParam)
						type = type.MakeByRefType();

					if (!startParams)
					{
						returnParam = new DelegateParameter
						{
							Type = type,
							Name = null,
							Attributes = ParameterAttributes.Retval |
							(marshalAs != null ? ParameterAttributes.HasFieldMarshal : 0),
							MarshalAs = marshalAs,
							DefaultValue = null
						};
					}
					else
					{
						var newParam = new DelegateParameter
						{
							Type = type,
							Name = null,
							Attributes =
							(marshalAs != null ? ParameterAttributes.HasFieldMarshal : 0),
							MarshalAs = marshalAs,
							DefaultValue = null
						};

						paramList.Add(newParam);
					}

					marshalAs = null;
					refParam = false;
					markList.Clear();
				}

				pos++;
			}

			if (returnParam is null)
				throw new FormatException(ET("返回值类型没有指定"));

			if (startParams != endParams)
				throw new FormatException(ET("参数列表没有关闭"));

			return CreateDelegate(name, returnParam, [.. paramList]);
		}

		/// <summary>
		/// 通过给定返回类型和参数创建委托类型
		/// </summary>
		/// <param name="name"></param>
		/// <param name="returnType"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static Type CreateDelegate
			(string name, Type returnType, DelegateParameter[] parameters)
		{
			return CreateDelegate(name, new DelegateParameter()
			{
				Type = returnType,
				Name = null,
				Attributes = ParameterAttributes.None,
				MarshalAs = null,
				DefaultValue = null,
			}, parameters);
		}

		/// <summary>
		/// 通过给定返回类型和参数创建委托类型
		/// </summary>
		/// <param name="classFullName"></param>
		/// <param name="returnParameter"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public static Type CreateDelegate
			(string name, DelegateParameter returnParameter, DelegateParameter[] parameters)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException(ET("委托类型的名称无效"));

			ArgumentNullException.ThrowIfNull(parameters);

			string classFullName = GenerateName(name);

			TypeBuilder delegateBuilder = m_ModuleBuilder.DefineType(classFullName,
				TypeAttributes.Public | TypeAttributes.Sealed
				| TypeAttributes.AnsiClass | TypeAttributes.Class
				| TypeAttributes.AutoLayout, typeof(MulticastDelegate));

			ConstructorBuilder ctorBuilder = delegateBuilder.DefineConstructor(MethodAttributes.Public
				| MethodAttributes.PrivateScope | MethodAttributes.HideBySig
				| MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
				CallingConventions.Standard | CallingConventions.HasThis,
				[typeof(object), typeof(IntPtr)]);

			ctorBuilder.DefineParameter(1, ParameterAttributes.None, "object");
			ctorBuilder.DefineParameter(2, ParameterAttributes.None, "method");
			ctorBuilder.SetImplementationFlags(MethodImplAttributes.Runtime);

			Type[] parameterTypes = GetParameterTypes(parameters);

			MethodBuilder invokeBuilder = delegateBuilder.DefineMethod("Invoke",
				MethodAttributes.Public | MethodAttributes.PrivateScope
				| MethodAttributes.VtableLayoutMask | MethodAttributes.Virtual
				| MethodAttributes.HideBySig, CallingConventions.Standard
				| CallingConventions.HasThis, returnParameter.Type, parameterTypes);

			ParameterBuilder invokeReturn = invokeBuilder.DefineParameter(0,
				returnParameter.Attributes, returnParameter.Name);

			if ((returnParameter.Attributes & ParameterAttributes.HasDefault) != 0)
				invokeReturn.SetConstant(returnParameter.DefaultValue);

			if (returnParameter.MarshalAs != null)
				SetMarshalAs(invokeReturn, returnParameter.MarshalAs);

			for (int i = 0; i < parameters.Length; i++)
			{
				ParameterBuilder invokeParam = invokeBuilder.DefineParameter(i + 1,
					parameters[i].Attributes, parameters[i].Name);

				if ((parameters[i].Attributes & ParameterAttributes.HasDefault) != 0)
					invokeParam.SetConstant(parameters[i].DefaultValue);

				var marshalAs = parameters[i].MarshalAs;

				if (marshalAs is not null)
					SetMarshalAs(invokeParam, marshalAs);
			}

			invokeBuilder.SetImplementationFlags(MethodImplAttributes.Runtime);

			return delegateBuilder.CreateType()
				?? throw new TypeLoadException(ET("无法创建委托类型"));
		}
	}

	/// <summary>
	/// 委托类型参数信息
	/// </summary>
	public sealed class DelegateParameter
	{
		/// <summary>
		/// 参数类型
		/// </summary>
		public required Type Type { get; init; }

		/// <summary>
		/// 参数名称
		/// </summary>
		public string? Name { get; set; }

		/// <summary>
		/// 参数修饰属性
		/// </summary>
		public ParameterAttributes Attributes { get; set; }

		/// <summary>
		/// 参数封送标注
		/// </summary>
		public MarshalAsAttribute? MarshalAs { get; set; }

		/// <summary>
		/// 参数默认值
		/// </summary>
		public object? DefaultValue { get; set; }
	}
}
