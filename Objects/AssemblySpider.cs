using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Objects
{
	/// <summary>
	/// 提供更方便的程序集、模块、类型及扩展函数的扫描方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public static class AssemblySpider
	{
		private static readonly List<Assembly> m_Assemblies = [];
		private static readonly List<Module> m_Modules = [];
		private static readonly List<Type> m_Types = [];
		private static readonly List<MethodInfo> m_ExtensionMethods = [];
		private static readonly bool m_Initialized = false;

		private static readonly ReadWriteLock m_Lock = new();

		private static event AssemblyDiscovered? AssemblyLoaded;
		private static event ModuleDiscovered? ModuleLoaded;
		private static event TypeDiscovered? TypeLoaded;
		private static event ExtensionMethodDiscovered? ExtensionMethodLoaded;

		/// <summary>
		/// 对每一个程序集，无论是现有的还是之后动态载入的都触发一次事件
		/// </summary>
		public static event AssemblyDiscovered? AssemblyDiscovered
		{
			add
			{
				if (value is null)
					return;

				AssemblyLoaded += value;

				Assembly[] assemblies;

				using (m_Lock.Read())
				{
					assemblies = m_Assemblies.ToArray();
				}

				foreach (var assembly in assemblies)
				{
					value.Invoke(assembly);
				}
			}
			remove
			{
				if (value is null)
					return;

				AssemblyLoaded -= value;
			}
		}

		/// <summary>
		/// 对每一个模块，无论是现有的还是之后动态载入的都触发一次事件
		/// </summary>
		public static event ModuleDiscovered? ModuleDiscovered
		{
			add
			{
				if (value is null)
					return;

				ModuleLoaded += value;

				Module[] modules;

				using (m_Lock.Read())
				{
					modules = m_Modules.ToArray();
				}

				foreach (var module in modules)
				{
					value.Invoke(module);
				}
			}
			remove
			{
				if (value is null)
					return;

				ModuleLoaded -= value;
			}
		}

		/// <summary>
		/// 对每一个类型，无论是现有的还是之后动态载入的都触发一次事件
		/// </summary>
		public static event TypeDiscovered? TypeDiscovered
		{
			add
			{
				if (value is null)
					return;

				TypeLoaded += value;

				Type[] types;

				using (m_Lock.Read())
				{
					types = [.. m_Types];
				}

				foreach (var type in types)
				{
					value.Invoke(type);
				}
			}
			remove
			{
				if (value is null)
					return;

				TypeLoaded -= value;
			}
		}

		/// <summary>
		/// 对每一个扩展方法，无论是现有的还是之后动态载入的都触发一次事件<br/>
		/// 扩展方法的标准是: 声明类是静态类，方法是静态的，但不一定是公开方法，并且方法上通过<see cref="ExtensionAttribute"/>标注且参数至少有一个
		/// </summary>
		public static event ExtensionMethodDiscovered? ExtensionMethodDiscovered
		{
			add
			{
				if (value is null)
					return;

				ExtensionMethodLoaded += value;

				MethodInfo[] methods;

				using (m_Lock.Read())
				{
					methods = m_ExtensionMethods.ToArray();
				}

				foreach (var method in methods)
				{
					value.Invoke(method);
				}
			}
			remove
			{
				if (value is null)
					return;

				ExtensionMethodLoaded -= value;
			}
		}

		static AssemblySpider()
		{
			AppDomain.CurrentDomain.AssemblyLoad += PerformAssemblyLoad;

			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
			{
				DispatchAssembly(assembly);
			}

			m_Initialized = true;
		}

		private static void CollectAssembly(Assembly assembly,
			out List<Module> modules,
			out List<Type> types,
			out List<MethodInfo> extensionMethods)
		{
			modules = [];
			types = [];
			extensionMethods = [];

			if (m_Assemblies.Contains(assembly))
				return;

			foreach (var module in assembly.GetModules())
			{
				if (!m_Modules.Contains(module))
				{
					modules.Add(module);

					foreach (var type in module.GetTypes())
					{
						if (!m_Types.Contains(type))
						{
							types.Add(type);

							if (type.IsAbstract && type.IsSealed)
							{
								foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
								{
									if (method.GetParameters().Length > 0
										&& method.GetCustomAttribute<ExtensionAttribute>() is not null)
									{
										if (!m_ExtensionMethods.Contains(method))
											extensionMethods.Add(method);
									}
								}
							}
						}
					}
				}
			}

			using (m_Lock.Write())
			{
				m_Assemblies.Add(assembly);
				m_Modules.AddRange(modules);
				m_Types.AddRange(types);
				m_ExtensionMethods.AddRange(extensionMethods);
			}
		}

		private static void DispatchAssembly(Assembly assembly)
		{
			if (m_Initialized)
			{
				List<Module> modules;
				List<Type> types;
				List<MethodInfo> extensionMethods;

				using (m_Lock.UpgradeableRead())
				{
					CollectAssembly(assembly,
						out modules,
						out types,
						out extensionMethods);
				}

				AssemblyLoaded?.Invoke(assembly);

				if (ModuleLoaded is not null)
				{
					foreach (var module in modules)
					{
						ModuleLoaded?.Invoke(module);
					}
				}

				if (TypeLoaded is not null)
				{
					foreach (var type in types)
					{
						TypeLoaded?.Invoke(type);
					}
				}

				if (ExtensionMethodLoaded is not null)
				{
					foreach (var method in extensionMethods)
					{
						ExtensionMethodLoaded?.Invoke(method);
					}
				}

			}
			else
				CollectAssembly(assembly, out _, out _, out _);
		}

		private static void PerformAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
		{
			Assembly assembly = args.LoadedAssembly;

			using (m_Lock.Read())
			{
				if (m_Assemblies.Contains(assembly))
					return;
			}

			DispatchAssembly(assembly);
		}
	}

	public delegate void AssemblyDiscovered(Assembly assembly);
	public delegate void ModuleDiscovered(Module module);
	public delegate void TypeDiscovered(Type type);
	public delegate void ExtensionMethodDiscovered(MethodInfo extensionMethod);
}
