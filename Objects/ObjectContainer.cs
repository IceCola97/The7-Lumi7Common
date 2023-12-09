using Lumi7Common.Reflection;
using Lumi7Common.Reflection.IL;
using Lumi7Common.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Objects
{
	/// <summary>
	/// IOC对象容器实现
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public class ObjectContainer
	{
		private static readonly ConcurrentDictionary<Type, ObjectLoader?> m_Container = new();
		private static readonly ConcurrentDictionary<string, ObjectLoader> m_NamedContainer = new();
		private static readonly ConcurrentDictionary<MethodBase, Delegate> m_CachedWrapped = new();

		static ObjectContainer()
		{
			AssemblySpider.TypeDiscovered += OnTypeDiscovered;
		}

		private static string? GetRequiredName(ParameterInfo parameter)
		{
			var attr = parameter.GetCustomAttribute<IOCNamedRequiredAttribute>(true);

			if (attr is not null && attr.Name is not null)
				return attr.Name ?? parameter.Name
					?? throw new NullReferenceException(ET("参数或字段在依赖注入时IOC组件名不应该为空"));

			return null;
		}

		private static string? GetRequiredName(FieldInfo field)
		{
			var attr = field.GetCustomAttribute<IOCNamedRequiredAttribute>(true);

			if (attr is not null && attr.Name is not null)
				return attr.Name ?? field.Name
					?? throw new NullReferenceException(ET("参数或字段在依赖注入时IOC组件名不应该为空"));

			return null;
		}

		private static string? GetRequiredName(PropertyInfo property)
		{
			var attr = property.GetCustomAttribute<IOCNamedRequiredAttribute>(true);

			if (attr is not null && attr.Name is not null)
				return attr.Name ?? property.Name
					?? throw new NullReferenceException(ET("参数或字段在依赖注入时IOC组件名不应该为空"));

			return null;
		}

		private static ObjectLoaderCore WrapLoadMethod(MethodBase methodBase)
		{
			if (methodBase is MethodInfo method)
				return WrapLoadMethod(method);
			else if (methodBase is ConstructorInfo constructor)
				return WrapLoadMethod(constructor);

			throw new ImpossibleException();
		}

		private static ObjectLoaderCore WrapLoadMethod(MethodInfo method)
		{
			if (method.GetParameters().Length == 0)
				return () => method.Invoke(null, null);

			var builder = WrapMethod(method, false);
			var wrapped = DIService.WrapDefaultCall(builder.Build());
			return () => wrapped.DynamicInvoke(null);
		}

		private static ObjectLoaderCore WrapLoadMethod(ConstructorInfo constructor)
		{
			var builder = WrapConstructor(constructor, false);
			var wrapped = DIService.WrapDefaultCall(builder.Build());
			return () => wrapped.DynamicInvoke(null);
		}

		private static IDIMethodBuilder WrapMethod(MethodInfo method, bool copyMethod)
		{
			var parameters = method.GetParameters();
			var builder = DIService.Wrap(method, copyMethod);
			var configAttr = method.GetCustomAttribute<IOCConfigSourceAttribute>(false);

			for (int i = 0; i < parameters.Length; i++)
			{
				var name = GetRequiredName(parameters[i]);
				builder.InjectParameter(i, DIService.MakeProvider(
					parameters[i].ParameterType, configAttr, name));
			}

			return builder;
		}

		private static IDIConstructorBuilder WrapConstructor(ConstructorInfo constructor, bool copyMethod)
		{
			var builder = DIService.Wrap(constructor, copyMethod);
			var configAttr = constructor.GetCustomAttribute<IOCConfigSourceAttribute>(false);
			var parameters = constructor.GetParameters();

			for (int i = 0; i < parameters.Length; i++)
			{
				var name = GetRequiredName(parameters[i]);
				builder.InjectParameter(i, DIService.MakeProvider(
					parameters[i].ParameterType, configAttr, name));
			}

			var declaring = constructor.DeclaringType
				?? throw new ImpossibleException(ET("构造函数没有所属类"));

			var members = declaring.GetMembers(BindingFlags.Instance
				| BindingFlags.Public | BindingFlags.NonPublic)
				.Where(m => ((m is PropertyInfo p && p.IsAutoProperty())
					|| (m is FieldInfo)) && (m.GetCustomAttribute<IOCRequiredAttribute>(true) is not null
					|| m.GetCustomAttribute<IOCNamedRequiredAttribute>(true) is not null));

			foreach (var member in members)
			{
				if (member is FieldInfo field)
				{
					var name = GetRequiredName(field);
					builder.InjectField(field.Name, DIService.MakeProvider(
						field.FieldType, configAttr, name));
				}
				else if (member is PropertyInfo property)
				{
					var name = GetRequiredName(property);
					builder.InjectField(property.Name, DIService.MakeProvider(
						property.PropertyType, configAttr, name));
				}
			}

			return builder;
		}

		private static void OnTypeDiscovered(Type type)
		{
			if (type.GetCustomAttribute<IOCComponentAttribute>(false) is not null)
				m_Container.TryAdd(type, null);

			IEnumerable<MethodBase> methods = type.GetMethods(BindingFlags.Static
				| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
			methods = methods.Concat(type.GetConstructors(BindingFlags.Instance
				| BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly));

			foreach (MethodBase methodBase in methods)
			{
				var resultType = methodBase.DeclaringType;

				if (methodBase is MethodInfo method)
					resultType = method.ReturnType;

				if (resultType == null
					|| resultType == typeof(void))
					continue;

				ObjectLoader? loader = null;

				var unnamed = methodBase.GetCustomAttributes<IOCComponentProviderAttribute>(false).ToArray();

				if (unnamed.Length > 0)
				{
					loader ??= new ObjectLoader(methodBase,
						WrapLoadMethod(methodBase));

					foreach (var attribute in unnamed)
					{
						var targetType = attribute.ComponentType ?? resultType;

						if (resultType != targetType
							&& !resultType.IsAssignableTo(targetType))
							throw new IOCComponentNotSupportedException(ET("在提供器 '{0}::{1}' 指定IOC组件类型是提供器不支持的: {2}",
								methodBase.GetDeclaringName(), methodBase.Name, targetType.FullName));

						if (!m_Container.ContainsKey(targetType))
						{
							if (targetType.GetCustomAttribute<IOCComponentAttribute>() is not null)
								m_Container.TryAdd(targetType, null);
							else
								throw new IOCComponentNotSupportedException(ET("IOC组件类型不存在或不支持: {0}",
									targetType.FullName));
						}

						if (!m_Container.TryUpdate(targetType, loader, null))
							throw new IOCAmbiguousProviderException(ET("IOC组件类型 '{0}' 的提供器被 '{1}::{2}' 重复声明",
								targetType.FullName, methodBase.GetDeclaringName(), methodBase.Name));
					}
				}

				var named = methodBase.GetCustomAttributes<IOCNamedProviderAttribute>(false).ToArray();

				if (named.Length > 0)
				{
					loader ??= new ObjectLoader(methodBase,
						WrapLoadMethod(methodBase));

					foreach (var attribute in named)
					{
						if (!m_NamedContainer.TryAdd(attribute.ComponentName, loader))
							throw new IOCAmbiguousProviderException(ET("IOC命名组件 '{0}' 的提供器被 '{1}::{2}' 重复声明",
								attribute.ComponentName, methodBase.GetDeclaringName(), methodBase.Name));
					}
				}
			}
		}

		/// <summary>
		/// 从容器中获取指定类型的组件对象
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T? Get<T>() where T : class => (T?)Get(typeof(T));

		/// <summary>
		/// 从容器中获取指定类型的组件对象
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="ImpossibleException"></exception>
		public static object? Get(Type type)
		{
			if (!m_Container.TryGetValue(type, out var loader))
				return null;

			if (loader is null)
				return null;

			var result = loader.LoadObject();

			if (!type.IsInstanceOfType(result))
				throw new ImpossibleException("ObjectProvider.LoadObject的类型检查失效");

			return result;
		}

		/// <summary>
		/// 从容器中获取指定名称指定类型的组件对象
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public static T? Get<T>(string name) where T : class => Get(name) as T;

		/// <summary>
		/// 从容器中获取指定名称指定类型的组件对象
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static object? Get(string name, Type type)
		{
			var result = Get(name);

			if (type.IsInstanceOfType(result))
				return result;

			return null;
		}

		/// <summary>
		/// 从容器中获取指定名称的组件对象
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public static object? Get(string name)
		{
			if (!m_NamedContainer.TryGetValue(name, out var contained))
				return null;

			return contained.LoadObject();
		}

		/// <summary>
		/// 向容器要求指定类型的组件对象
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T Require<T>() where T : class => (T)Require(typeof(T));

		/// <summary>
		/// 向容器要求指定类型的组件对象
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="IOCComponentNotSupportedException"></exception>
		/// <exception cref="IOCMissingObjectException"></exception>
		/// <exception cref="ImpossibleException"></exception>
		public static object Require(Type type)
		{
			if (!m_Container.TryGetValue(type, out var loader))
				throw new IOCComponentNotSupportedException(ET("IOC组件类型不存在或不支持: {0}", type.FullName));

			if (loader is null)
				throw new IOCMissingObjectException(ET("IOC组件类型 '{0}' 没有对应的对象提供器", type.FullName));

			var result = loader.LoadObject();

			if (!type.IsInstanceOfType(result))
				throw new ImpossibleException("ObjectProvider.LoadObject的类型检查失效");

			return result;
		}

		/// <summary>
		/// 向容器要求指定名称指定类型的组件对象
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		public static T Require<T>(string name) where T : class => (T)Require(name, typeof(T));

		/// <summary>
		/// 向容器要求指定名称指定类型的组件对象
		/// </summary>
		/// <param name="name"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="IOCMissingObjectException"></exception>
		public static object Require(string name, Type type)
		{
			var result = Require(name);

			if (type.IsInstanceOfType(result))
				return result;

			throw new IOCMissingObjectException(ET("IOC组件 '{0}' 提供的组件类型与预期的类型 '{1}' 不符", name, type.FullName));
		}

		/// <summary>
		/// 向容器要求指定名称的组件对象
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <exception cref="IOCMissingObjectException"></exception>
		public static object Require(string name)
		{
			if (!m_NamedContainer.TryGetValue(name, out var contained))
				throw new IOCMissingObjectException(ET("IOC组件 '{0}' 没有对应的对象提供器", name));

			return contained.LoadObject();
		}

		/// <summary>
		/// 将给定方法包装为携带IOC依赖注入的方法
		/// </summary>
		/// <param name="methodBase"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		public static Delegate Wrap(MethodBase methodBase)
		{
			if (m_CachedWrapped.TryGetValue(methodBase, out var @delegate))
				return @delegate;

			if (methodBase is MethodInfo method)
				@delegate = WrapMethod(method, false).Build();
			else if (methodBase is ConstructorInfo constructor)
				@delegate = WrapConstructor(constructor, false).Build();
			else
				throw new ArgumentException(ET("无效的方法对象"));

			m_CachedWrapped.TryAdd(methodBase, @delegate);
			return @delegate;
		}

		/// <summary>
		/// 将给定方法包装为携带IOC依赖注入的方法并将包装后的方法替换原方法
		/// </summary>
		/// <param name="methodBase"></param>
		public static void Patch(MethodBase methodBase)
		{
			Delegate @delegate;

			if (methodBase is MethodInfo method)
				@delegate = WrapMethod(method, true).Build();
			else if (methodBase is ConstructorInfo constructor)
				@delegate = WrapConstructor(constructor, true).BuildMethodOnly();
			else
				throw new ArgumentException(ET("无效的方法对象"));

			MethodPatch.RuntimePatch(methodBase, @delegate);
		}

		/// <summary>
		/// 通过IOC依赖注入来自动填充指定方法的参数并调用
		/// </summary>
		/// <param name="method"></param>
		/// <param name="instance"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		/// <exception cref="NullReferenceException"></exception>
		public static object? Invoke(MethodInfo method, object? instance, params object?[]? args)
		{
			object?[]? callArgs = null;
			int count = 0;

			if (instance is not null)
				count++;
			if (args is not null)
				count += args.Length;

			if (count > 0)
				callArgs = new object[count];

			if (instance is not null)
			{
				callArgs![0] = instance;
				count = 1;
			}
			else
				count = 0;

			if (args is not null && callArgs is not null)
				Array.Copy(args, 0, callArgs, count, args.Length);

			var result = Wrap(method).DynamicInvoke(callArgs);

			if (args is not null && callArgs is not null)
				Array.Copy(callArgs, count, args, 0, args.Length);

			return result;
		}

		/// <summary>
		/// 通过IOC依赖注入来自动填充构造函数的参数并调用
		/// </summary>
		/// <param name="ctor"></param>
		/// <param name="args"></param>
		/// <returns></returns>
		/// <exception cref="NullReferenceException"></exception>
		public static object? Create(ConstructorInfo ctor, params object?[]? args)
		{
			return Wrap(ctor).DynamicInvoke(args);
		}

		/// <summary>
		/// 注册指定类型为IOC组件类型
		/// </summary>
		/// <param name="type"></param>
		/// <exception cref="IOCComponentNotSupportedException"></exception>
		public static void Register(Type type)
		{
			if (type.IsValueType)
				throw new IOCComponentNotSupportedException(ET("无法将值类型注册为IOC组件类型"));

			m_Container.TryAdd(type, null);
		}

		/// <summary>
		/// 向容器中提供指定IOC类型组件对象
		/// </summary>
		/// <param name="type"></param>
		/// <param name="component"></param>
		/// <exception cref="IOCComponentNotSupportedException"></exception>
		/// <exception cref="IOCAmbiguousProviderException"></exception>
		public static void Provide(Type type, object component)
		{
			if (!type.IsInstanceOfType(component))
				throw new IOCComponentNotSupportedException(ET("无法对IOC组件类型 '{0}' 提供IOC组件类型为 '{1}' 的IOC组件对象", type.FullName, component.GetType().FullName));
			if (!m_Container.ContainsKey(type))
				throw new IOCComponentNotSupportedException(ET("IOC组件类型不存在或不支持: {0}", type.FullName));

			if (!m_Container.TryUpdate(type, new ObjectLoader(component), null))
				throw new IOCAmbiguousProviderException(ET("IOC组件类型被重复提供了组件对象: {0}", type.FullName));
		}

		/// <summary>
		/// 向容器中提供IOC命名组件对象
		/// </summary>
		/// <param name="name"></param>
		/// <param name="component"></param>
		/// <exception cref="IOCComponentNotSupportedException"></exception>
		/// <exception cref="IOCAmbiguousProviderException"></exception>
		public static void Provide(string name, object component)
		{
			if (component is ValueType)
				throw new IOCComponentNotSupportedException(ET("无法将值类型作为IOC组件对象"));

			if (!m_NamedContainer.TryAdd(name, new ObjectLoader(component)))
				throw new IOCAmbiguousProviderException(ET("IOC组件 '{0}' 被重复提供了组件对象: {1}", name, component.GetType().FullName));
		}

		/// <summary>
		/// 获取容器当前使用的<see cref="IConfigDataSource"/>，此方法等效于<c>Require&lt;IConfigDataSource&gt;()</c>
		/// </summary>
		/// <returns></returns>
		public static IConfigDataSource GetConfigDataSource()
			=> Require<IConfigDataSource>();

		private delegate object? ObjectLoaderCore();

		[ThreadSafe]
		private sealed class ObjectLoader
		{
			private static readonly ObjectLoaderCore m_DummyLoader = DummyProviderMethod;
			private static readonly MethodInfo m_DummyMethod = m_DummyLoader.Method;

			private readonly MethodBase m_Method;
			private readonly ObjectLoaderCore m_CoreLoader;
			private volatile object? m_LoadedObject = null;

			public ObjectLoader(object givenObject)
			{
				m_Method = m_DummyMethod;
				m_CoreLoader = m_DummyLoader;
				m_LoadedObject = givenObject
					?? throw new ArgumentNullException(nameof(givenObject));

				if (givenObject is ValueType)
					throw new ArgumentException(ET("不应该向IOC提供一个值类型的组件"));
			}

			public ObjectLoader(MethodBase method, ObjectLoaderCore coreLoader)
			{
				m_Method = method;
				m_CoreLoader = coreLoader;
			}

			public object LoadObject()
			{
				if (m_LoadedObject is not null)
					return m_LoadedObject;

				if (Monitor.IsEntered(this))
					throw new InvalidOperationException(ET("不应该让IOC对象提供器 '{0}::{1}' 循环引用自身",
						m_Method.GetDeclaringName(), m_Method.Name));

				lock (this)
				{
					if (m_LoadedObject is not null)
						return m_LoadedObject;

					var result = m_CoreLoader.Invoke()
						?? throw new IOCMissingObjectException(ET("IOC对象提供器 '{0}::{1}' 提供了一个空对象",
							m_Method.GetDeclaringName(), m_Method.Name));

					if (result is ValueType)
						throw new IOCComponentNotSupportedException(ET("IOC对象提供器 '{0}::{1}' 试图提供值类型的对象",
							m_Method.GetDeclaringName(), m_Method.Name));

					m_LoadedObject = result;
					return result;
				}
			}

			private static object? DummyProviderMethod() => null;
		}

		private delegate object? CachedInvoker(object? instance, object?[]? args);
		private delegate object? CachedActivator(object?[]? args);
	}
}
