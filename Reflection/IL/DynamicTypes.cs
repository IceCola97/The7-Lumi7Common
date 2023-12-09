using Lumi7Common.Objects;
using Lumi7Common.Threading;
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
	/// 提供快速创建动态类型的方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public static class DynamicTypes
	{
		private const string DefaultModuleName = "DynamicTypes.DefaultModule";

		private static readonly AssemblyBuilder m_Builder;
		private static readonly InstanceProvider<ModuleBuilder> m_DefaultModule;

		static DynamicTypes()
		{
			DynamicSupport.AssertDynamicSupport();

			m_Builder = AssemblyBuilder.DefineDynamicAssembly(
				new AssemblyName("DynamicTypes"), AssemblyBuilderAccess.RunAndCollect);
			m_DefaultModule = new(() => DefineModule(DefaultModuleName));
		}

		/// <summary>
		/// 快速定义一个动态模块
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static ModuleBuilder DefineModule(string name)
		{
			if (!m_DefaultModule.IsCreatingByCurrent && DefaultModuleName.Equals(name))
				throw new ArgumentException(ET("不应该使用默认模块的名称"));

			return m_Builder.DefineDynamicModule(name);
		}

		/// <summary>
		/// 使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(string name)
			=> m_DefaultModule.Instance.DefineType(name);

		/// <summary>
		/// 使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(string name, TypeAttributes attributes)
			=> m_DefaultModule.Instance.DefineType(name, attributes);

		/// <summary>
		/// 使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(string name, TypeAttributes attributes, Type? parent)
			=> m_DefaultModule.Instance.DefineType(name, attributes, parent);

		/// <summary>
		/// 使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(string name,
			TypeAttributes attributes, Type? parent, int typeSize)
			=> m_DefaultModule.Instance.DefineType(name, attributes, parent, typeSize);

		/// <summary>
		/// 使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(string name,
			TypeAttributes attributes, Type? parent, PackingSize packingSize)
			=> m_DefaultModule.Instance.DefineType(name, attributes, parent, packingSize);

		/// <summary>
		/// 使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(string name,
			TypeAttributes attributes, Type? parent, Type[]? interfaces)
			=> m_DefaultModule.Instance.DefineType(name, attributes, parent, interfaces);

		/// <summary>
		/// 使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(string name, TypeAttributes attributes,
			Type? parent, PackingSize packingSize, int typeSize)
			=> m_DefaultModule.Instance.DefineType(name, attributes, parent, packingSize, typeSize);

		/// <summary>
		/// 在给定命名空间下使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(this DynamicNamespace @namespace, string name)
			=> DefineType($"{@namespace.Name}.{name}");

		/// <summary>
		/// 在给定命名空间下使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(this DynamicNamespace @namespace,
			string name, TypeAttributes attributes)
			=> DefineType($"{@namespace.Name}.{name}", attributes);

		/// <summary>
		/// 在给定命名空间下使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(this DynamicNamespace @namespace,
			string name, TypeAttributes attributes, Type? parent)
			=> DefineType($"{@namespace.Name}.{name}", attributes, parent);

		/// <summary>
		/// 在给定命名空间下使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(this DynamicNamespace @namespace,
			string name, TypeAttributes attributes, Type? parent, int typeSize)
			=> DefineType($"{@namespace.Name}.{name}", attributes, parent, typeSize);

		/// <summary>
		/// 在给定命名空间下使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(this DynamicNamespace @namespace,
			string name, TypeAttributes attributes, Type? parent, PackingSize packingSize)
			=> DefineType($"{@namespace.Name}.{name}", attributes, parent, packingSize);

		/// <summary>
		/// 在给定命名空间下使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(this DynamicNamespace @namespace,
			string name, TypeAttributes attributes, Type? parent, Type[]? interfaces)
			=> DefineType($"{@namespace.Name}.{name}", attributes, parent, interfaces);

		/// <summary>
		/// 在给定命名空间下使用默认模块来快速定义一个动态类型
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static TypeBuilder DefineType(this DynamicNamespace @namespace,
			string name, TypeAttributes attributes, Type? parent, PackingSize packingSize, int typeSize)
			=> DefineType($"{@namespace.Name}.{name}", attributes, parent, packingSize, typeSize);
	}

	public sealed record DynamicNamespace(string Name);
}
