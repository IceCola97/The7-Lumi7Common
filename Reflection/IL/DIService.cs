using Lumi7Common.Extensions;
using Lumi7Common.Objects;
using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 通过动态代码的方式提供DI的封装
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class DIService
	{
		private static readonly MethodInfo m_DIService_CheckNotnull =
			typeof(DIService).ExtractMethod(nameof(CheckNotnull));
		private static readonly MethodInfo m_DIService_CheckConvert =
			typeof(DIService).ExtractMethod(nameof(CheckConvert));
		private static readonly MethodInfo m_DIService_AllocateInstance =
			typeof(DIService).ExtractMethod(nameof(AllocateInstance));
		private static readonly MethodInfo m_ObjectProvider_Invoke =
			typeof(ObjectProvider).ExtractMethod(nameof(ObjectProvider.Invoke));
		private static readonly MethodInfo m_ObjectContainer_Require =
			typeof(ObjectContainer).ExtractMethod(nameof(ObjectContainer.Require), []);
		private static readonly MethodInfo m_ObjectContainer_Require_string =
			typeof(ObjectContainer).ExtractGenericMethod(nameof(ObjectContainer.Require), 1, [typeof(string)]);
		private static readonly MethodInfo m_IConfigDataSource_FetchItem_string =
			typeof(IConfigDataSource).ExtractMethod(nameof(IConfigDataSource.FetchItem), [typeof(string)]);
		private static readonly MethodInfo m_IConfigDataSource_FetchItem_string_string =
			typeof(IConfigDataSource).ExtractMethod(nameof(IConfigDataSource.FetchItem), [typeof(string), typeof(string)]);

		/// <summary>
		/// 将给定方法封装为保留返回值类型的无参委托，并在调用时使用<see langword="default"/>关键字填充所有参数
		/// </summary>
		/// <param name="delegate"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ThreadSafe]
		public static Delegate WrapDefaultCall(MethodInfo method)
		{
			return WrapDefaultCallCore(null, method);
		}

		/// <summary>
		/// 将给定委托封装为保留返回值类型的无参委托，并在调用时使用<see langword="default"/>关键字填充所有参数
		/// </summary>
		/// <param name="delegate"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[ThreadSafe]
		public static Delegate WrapDefaultCall(Delegate @delegate)
		{
			var method = @delegate.GetType().GetMethod("Invoke")
				?? throw new ArgumentException(ET("给定的委托对象无效"));

			return WrapDefaultCallCore(@delegate, method);
		}

		private static Delegate WrapDefaultCallCore(object? instance, MethodInfo method)
		{
			DynamicSupport.AssertDynamicSupport();

			if (method.CallingConvention.HasFlag(CallingConventions.VarArgs))
				throw new NotSupportedException(ET("不支持给定的委托或方法"));

			var parameters = method.GetParameters();

			if (parameters.Any(p => p.ParameterType.IsByRef))
				throw new NotSupportedException(ET("参数不能包含引用类型"));

			var paramInstance = instance is null ? null
				: Expression.Constant(instance);
			var paramArray = new List<Expression>();

			for (int i = 0; i < parameters.Length; i++)
			{
				var paramType = parameters[i].ParameterType;
				paramArray.Add(Expression.Default(paramType));
			}

			return Expression.Lambda(Expression.Call(paramInstance,
				method, paramArray)).Compile();
		}

		/// <summary>
		/// 根据给定方法创建DI包装构建器
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IDIMethodBuilder Wrap(MethodInfo method) => Wrap(method, false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IDIMethodBuilder Wrap(MethodInfo method, bool copyMethod)
		{
			if (method.CallingConvention.HasFlag(CallingConventions.VarArgs))
				throw new NotSupportedException(ET("不支持给定的方法"));

			return new DIMethodBuilderImpl(method, copyMethod);
		}

		/// <summary>
		/// 根据给定构造函数创建DI包装构建器
		/// </summary>
		/// <param name="constructor"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IDIConstructorBuilder Wrap(ConstructorInfo constructor) => Wrap(constructor, false);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static IDIConstructorBuilder Wrap(ConstructorInfo constructor, bool copyMethod)
		{
			if (constructor.CallingConvention.HasFlag(CallingConventions.VarArgs))
				throw new NotSupportedException(ET("不支持给定的构造函数"));

			return new DIConstructorBuilderImpl(constructor, copyMethod);
		}

		/// <summary>
		/// 根据给定的对象提供器标注与对应参数或字段名称构造一个对象提供器
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="providerAttribute"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ImpossibleException"></exception>
		[ThreadSafe]
		public static ObjectProvider<T> MakeProvider<T>
			(IOCConfigSourceAttribute? providerAttribute, string? name)
		{
			return (ObjectProvider<T>)MakeProviderCore(
				typeof(ObjectProvider<T>), typeof(T), providerAttribute, name);
		}

		/// <summary>
		/// 根据给定的对象提供器标注与对应参数或字段名称构造一个对象提供器
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="providerAttribute"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException"></exception>
		/// <exception cref="ImpossibleException"></exception>
		[ThreadSafe]
		public static Delegate MakeProvider(Type returnType,
			IOCConfigSourceAttribute? providerAttribute, string? name)
		{
			return MakeProviderCore(typeof(ObjectProvider<>).MakeGenericType(returnType),
				returnType, providerAttribute, name);
		}

		private static Delegate MakeProviderCore(Type delegateType, Type returnType,
			IOCConfigSourceAttribute? providerAttribute, string? name)
		{
			DynamicSupport.AssertDynamicSupport();

			if (returnType.IsValueType
				&& (providerAttribute is null || name is null))
				throw new InvalidOperationException(ET("值类型应该指定配置项因为值类型必须依赖于配置项的读取"));

			var configSource = ObjectContainer.GetConfigDataSource();
			var configBody = default(Expression);
			var requireBody = default(Expression);

			if (providerAttribute is not null && name is not null)
			{
				if (providerAttribute.ConfigPath is not null)
					configBody = Expression.Call(Expression.Constant(configSource),
						m_IConfigDataSource_FetchItem_string_string
						.MakeGenericMethod(returnType),
						Expression.Constant(providerAttribute.ConfigPath),
						Expression.Constant(name));
				else if (providerAttribute.ConfigType is not null)
					configBody = Expression.Call(Expression.Constant(configSource),
						m_IConfigDataSource_FetchItem_string
						.MakeGenericMethod(providerAttribute.ConfigType, returnType),
						Expression.Constant(name));
			}

			if (!returnType.IsValueType)
			{
				if (name is null)
					requireBody = Expression.Call(null,
						m_ObjectContainer_Require
						.MakeGenericMethod(returnType));
				else
					requireBody = Expression.Call(null,
						m_ObjectContainer_Require_string
						.MakeGenericMethod(returnType),
						Expression.Constant(name));
			}

			var expBody = default(Expression);

			if (configBody is not null)
			{
				if (requireBody is not null)
				{
					var resultExp = Expression.Variable(returnType);
					var tryBlock = Expression.TryCatch(Expression.Assign(resultExp, configBody),
						Expression.Catch(typeof(object), Expression.Assign(resultExp, requireBody)));

					expBody = Expression.Block([resultExp], [tryBlock, resultExp]);
				}
				else
					expBody = configBody;
			}
			else if (requireBody is not null)
				expBody = requireBody;
			else
				throw new ImpossibleException();

			if (delegateType == typeof(ObjectProvider)
				&& returnType.IsValueType)
				expBody = Expression.Convert(expBody, typeof(object));

			return Expression.Lambda(delegateType, expBody).Compile();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TResult AllocateInstance<TResult>()
			=> (TResult)RuntimeHelpers.GetUninitializedObject(typeof(TResult));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TResult CheckNotnull<TResult>(TResult? value)
		{
			if (value is not TResult result)
				throw new IOCMissingObjectException(ET("DI对象提供器给出了null导致无法满足要求类型: {0}",
					typeof(TResult).FullName));

			return result;
		}

		private static MethodCallExpression CheckNotnullExpression(Expression expression)
		{
			return Expression.Call(null, m_DIService_CheckNotnull
				.MakeGenericMethod(expression.Type), expression);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TResult CheckConvert<TResult>(object? value)
		{
			if (value is not TResult result)
				throw new IOCMissingObjectException(ET("DI对象提供器给出的对象类型 '{0}' 无法满足要求类型: {1}",
					value is null ? "null" : value.GetType().FullName, typeof(TResult).FullName));

			return result;
		}

		private static MethodCallExpression CheckConvertExpression
			(Expression expression, Type type)
		{
			return Expression.Call(null, m_DIService_CheckConvert
				.MakeGenericMethod(type), expression);
		}

		private static MethodCallExpression AccessProvider(Delegate provider, Type resultType)
		{
			if (provider is ObjectProvider)
			{
				return CheckConvertExpression(Expression.Call(
					Expression.Constant(provider),
					m_ObjectProvider_Invoke), resultType);
			}
			else
			{
				var providerType = provider.GetType();

				if (providerType.GetGenericTypeDefinition()
					== typeof(ObjectProvider<>))
				{
					var invokeMethod = providerType.GetMethod("Invoke")
						?? throw new InvalidDataException(ET("无效的委托对象"));

					if (!invokeMethod.ReturnType
						.IsAssignableTo(resultType))
						throw new InvalidDataException(ET("DI对象提供器提供的类型 '{0}' 无法满足目标类型: {1}",
							invokeMethod.ReturnType.FullName, resultType.FullName));

					if (invokeMethod.ReturnType.IsNullable())
						return Expression.Call(
							Expression.Constant(provider), invokeMethod);
					else
						return CheckNotnullExpression(Expression.Call(
							Expression.Constant(provider), invokeMethod));
				}
				else
					throw new InvalidDataException(ET("无效的委托类型: {0}",
						providerType.FullName));
			}
		}

		private static (List<ParameterExpression>, List<Expression>) BuildParameters
			(ParameterInfo[] parameters, Delegate[] providers)
		{
			var paramList = new List<ParameterExpression>();
			var paramArray = new List<Expression>();

			for (int i = 0; i < parameters.Length; i++)
			{
				var param = parameters[i];
				var paramExp = Expression.Parameter(param.ParameterType);
				paramList.Add(paramExp);

				if (providers[i] is not null)
					paramArray.Add(AccessProvider(providers[i], param.ParameterType));
				else
					paramArray.Add(paramExp);
			}

			return (paramList, paramArray);
		}

		private static DynamicMethod CreateFieldInit(FieldInfo fieldInfo)
		{
			var declaring = fieldInfo.DeclaringType ?? throw new ImpossibleException();
			var method = new DynamicMethod($"FieldInit@{fieldInfo.Name}",
				typeof(void), [declaring, fieldInfo.FieldType]);
			var ilGen = method.GetILGenerator();

			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Ldarg_1);
			ilGen.Emit(OpCodes.Stfld, fieldInfo);
			ilGen.Emit(OpCodes.Ret);

			return method;
		}

		private sealed class DIMethodBuilderImpl : IDIMethodBuilder
		{
			private readonly MethodInfo m_Method;
			private readonly ParameterInfo[] m_Parameters;
			private readonly Delegate[] m_Providers;
			private readonly bool m_CopyMethod;
			private volatile bool m_Created = false;

			public DIMethodBuilderImpl(MethodInfo method, bool copyMethod)
			{
				DynamicSupport.AssertDynamicSupport();

				m_Method = method;
				m_Parameters = method.GetParameters();
				m_Providers = new Delegate[m_Parameters.Length];
				m_CopyMethod = copyMethod;
			}

			public MethodInfo Method => m_Method;

			private void CheckNotCreated()
			{
				if (m_Created)
					throw new InvalidOperationException(ET("当前方法包装委托已创建"));
			}

			public Delegate Build()
			{
				CheckNotCreated();

				return Build(DelegateCache.ObtainDelegate(
					DelegateBuilder.GenerateSignature(m_Method, true)));
			}

			public TDelegate Build<TDelegate>()
				where TDelegate : class, MulticastDelegate
			{
				CheckNotCreated();

				return (TDelegate)Build(typeof(TDelegate));
			}

			private Delegate Build(Type delegateType)
			{
				CheckNotCreated();

				var paramInstance = default(ParameterExpression);
				var (paramList, paramArray) = BuildParameters(m_Parameters, m_Providers);

				if (!m_Method.IsStatic)
				{
					paramInstance = Expression.Parameter(m_Method.DeclaringType
						?? throw new ImpossibleException("一个全局方法是一个实例方法"));
					paramList.Insert(0, paramInstance);
				}

				Delegate result;

				if (m_CopyMethod)
				{
					if (paramInstance is not null)
						paramArray.Insert(0, paramInstance);

					result = Expression.Lambda(delegateType, Expression.Call(null,
						m_Method.CopyMethod(), paramArray), paramList).Compile();
				}
				else
					result = Expression.Lambda(delegateType, Expression.Call(
						paramInstance, m_Method, paramArray), paramList).Compile();

				m_Created = true;
				return result;
			}

			public void InjectParameter(int index, ObjectProvider provider)
			{
				CheckNotCreated();

				if (m_Providers[index] is not null)
					throw new ArgumentException(ET("指定的参数已经被添加了依赖注入"));

				m_Providers[index] = provider;
			}

			public void InjectParameter<TResult>(int index, ObjectProvider<TResult> provider)
			{
				CheckNotCreated();

				if (m_Providers[index] is not null)
					throw new ArgumentException(ET("指定的参数已经被添加了依赖注入"));
				if (!typeof(TResult).IsAssignableTo(m_Parameters[index].ParameterType))
					throw new ArgumentException(ET("给定的对象提供器提供的类型是 '{0}' 不能转换成要求的目标类型: {1}",
						typeof(TResult).FullName, m_Parameters[index].ParameterType.FullName));

				m_Providers[index] = provider;
			}

			public void InjectParameter(int index, Delegate provider)
			{
				CheckNotCreated();

				if (m_Providers[index] is not null)
					throw new ArgumentException(ET("指定的参数已经被添加了依赖注入"));

				var type = provider.GetType();

				if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(ObjectProvider<>))
					throw new ArgumentException(ET("当前方法仅接受ObjectProvider的泛型"));

				var targetType = type.GetGenericArguments()[0];

				if (!targetType.IsAssignableTo(m_Parameters[index].ParameterType))
					throw new ArgumentException(ET("给定的对象提供器提供的类型是 '{0}' 不能转换成要求的目标类型: {1}",
						targetType.FullName, m_Parameters[index].ParameterType.FullName));

				m_Providers[index] = provider;
			}
		}

		private sealed class DIConstructorBuilderImpl : IDIConstructorBuilder
		{
			private const BindingFlags DefaultFlags = BindingFlags.Instance
				| BindingFlags.Public | BindingFlags.NonPublic;

			private readonly Type m_InstanceType;
			private readonly ConstructorInfo m_Constructor;
			private readonly ParameterInfo[] m_Parameters;
			private readonly Delegate[] m_Providers;
			private readonly Dictionary<FieldInfo, Delegate> m_FieldProviders = [];
			private readonly bool m_CopyMethod;
			private volatile bool m_Created = false;

			public DIConstructorBuilderImpl(ConstructorInfo ctor, bool copyMethod)
			{
				DynamicSupport.AssertDynamicSupport();

				m_InstanceType = ctor.DeclaringType
					?? throw new ImpossibleException();
				m_Constructor = ctor;
				m_Parameters = ctor.GetParameters();
				m_Providers = new Delegate[m_Parameters.Length];
				m_CopyMethod = copyMethod;
			}

			public ConstructorInfo Constructor => m_Constructor;

			private void CheckNotCreated()
			{
				if (m_Created)
					throw new InvalidOperationException(ET("当前方法包装委托已创建"));
			}

			public Delegate Build()
			{
				CheckNotCreated();

				var ctorSig = DelegateBuilder.GenerateSignature(m_Constructor, true);

				if (!ctorSig.StartsWith("v(t0"))
					throw new InvalidDataException(ET("无效的构造函数: {0}", ctorSig));

				ctorSig = $"t0(" + ctorSig[4..];
				return BuildCreator(DelegateCache.ObtainDelegate(ctorSig));
			}

			public TDelegate Build<TDelegate>()
				where TDelegate : class, MulticastDelegate
			{
				CheckNotCreated();

				var delegateType = typeof(TDelegate);
				var invokeMethod = delegateType.GetMethod("Invoke")
					?? throw new ArgumentException(ET("无效的委托对象"));

				if (invokeMethod.ReturnType == typeof(void))
					return (TDelegate)BuildMethod(delegateType);
				else
					return (TDelegate)BuildCreator(delegateType);
			}

			public Delegate BuildMethodOnly()
			{
				CheckNotCreated();

				return BuildMethod(DelegateCache.ObtainDelegate(
					DelegateBuilder.GenerateSignature(m_Constructor, true)));
			}

			private Delegate BuildCreator(Type delegateType)
			{
				CheckNotCreated();

				var instance = Expression.Variable(m_InstanceType);
				var body = BuildBody(instance, out var paramList);
				var assignment = Expression.Assign(instance, Expression.Call(null,
					m_DIService_AllocateInstance.MakeGenericMethod(m_InstanceType)));
				body.Insert(0, assignment);
				body.Add(instance);

				var result = Expression.Lambda(delegateType,
					Expression.Block([instance], body), paramList).Compile();
				m_Created = true;
				return result;
			}

			private Delegate BuildMethod(Type delegateType)
			{
				CheckNotCreated();

				var instance = Expression.Parameter(m_InstanceType);
				var body = BuildBody(instance, out var paramList);
				paramList.Insert(0, instance);

				var result = Expression.Lambda(delegateType, Expression.Block(body),
					paramList).Compile();
				m_Created = true;
				return result;
			}

			private List<Expression> BuildBody
				(Expression instance, out List<ParameterExpression> paramList)
			{
				CheckNotCreated();

				var wrapped = m_CopyMethod
					? m_Constructor.CopyMethod()
					: ConstructorInvoker.WrapAsMethod(m_Constructor);
				var expList = new List<Expression>();
				(paramList, var paramArray) = BuildParameters(m_Parameters, m_Providers);

				expList.Add(Expression.Call(null, wrapped, paramArray.Prepend(instance)));

				foreach (var entry in m_FieldProviders)
				{
					if (!entry.Key.IsInitOnly)
						expList.Add(Expression.Assign(Expression.Field(instance, entry.Key),
							AccessProvider(entry.Value, entry.Key.FieldType)));
					else
						expList.Add(Expression.Call(null, CreateFieldInit(entry.Key),
							instance, AccessProvider(entry.Value, entry.Key.FieldType)));
				}

				return expList;
			}

			private FieldInfo? FindField(string name)
			{
				CheckNotCreated();

				var field = m_InstanceType.GetField(name, DefaultFlags);

				if (field is null)
				{
					var property = m_InstanceType.GetProperty(name, DefaultFlags);

					if (property is not null)
						field = property.GetAutoAccessField();
				}

				return field;
			}

			public void InjectField(string name, ObjectProvider provider)
			{
				CheckNotCreated();

				var field = FindField(name)
					?? throw new MissingFieldException(ET("给定的字段 '{0}' 没有找到", name));

				if (!m_FieldProviders.TryAdd(field, provider))
					throw new InvalidOperationException(ET("指定的字段 '{0}' 已经被添加了依赖注入", name));
			}

			public void InjectField<TResult>(string name, ObjectProvider<TResult> provider)
			{
				CheckNotCreated();

				var field = FindField(name)
					?? throw new MissingFieldException(ET("给定的字段 '{0}' 没有找到", name));

				if (!typeof(TResult).IsAssignableTo(field.FieldType))
					throw new ArgumentException(ET("给定的对象提供器提供的类型是 '{0}' 不能转换成要求的目标类型: {1}",
						typeof(TResult).FullName, field.FieldType.FullName));

				if (!m_FieldProviders.TryAdd(field, provider))
					throw new InvalidOperationException(ET("指定的字段 '{0}' 已经被添加了依赖注入", name));
			}

			public void InjectField(string name, Delegate provider)
			{
				CheckNotCreated();

				var field = FindField(name)
					?? throw new MissingFieldException(ET("给定的字段 '{0}' 没有找到", name));

				var type = provider.GetType();

				if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(ObjectProvider<>))
					throw new ArgumentException(ET("当前方法仅接受ObjectProvider的泛型"));

				var targetType = type.GetGenericArguments()[0];

				if (!targetType.IsAssignableTo(field.FieldType))
					throw new ArgumentException(ET("给定的对象提供器提供的类型是 '{0}' 不能转换成要求的目标类型: {1}",
						targetType.FullName, field.FieldType.FullName));

				if (!m_FieldProviders.TryAdd(field, provider))
					throw new InvalidOperationException(ET("指定的字段 '{0}' 已经被添加了依赖注入", name));
			}

			public void InjectParameter(int index, ObjectProvider provider)
			{
				CheckNotCreated();

				if (m_Providers[index] is not null)
					throw new ArgumentException(ET("指定的参数已经被添加了依赖注入"));

				m_Providers[index] = provider;
			}

			public void InjectParameter<TResult>(int index, ObjectProvider<TResult> provider)
			{
				CheckNotCreated();

				if (m_Providers[index] is not null)
					throw new ArgumentException(ET("指定的参数已经被添加了依赖注入"));
				if (!typeof(TResult).IsAssignableTo(m_Parameters[index].ParameterType))
					throw new ArgumentException(ET("给定的对象提供器提供的类型是 '{0}' 不能转换成要求的目标类型: {1}",
						typeof(TResult).FullName, m_Parameters[index].ParameterType.FullName));

				m_Providers[index] = provider;
			}

			public void InjectParameter(int index, Delegate provider)
			{
				CheckNotCreated();

				if (m_Providers[index] is not null)
					throw new ArgumentException(ET("指定的参数已经被添加了依赖注入"));

				var type = provider.GetType();

				if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(ObjectProvider<>))
					throw new ArgumentException(ET("当前方法仅接受ObjectProvider的泛型"));

				var targetType = type.GetGenericArguments()[0];

				if (!targetType.IsAssignableTo(m_Parameters[index].ParameterType))
					throw new ArgumentException(ET("给定的对象提供器提供的类型是 '{0}' 不能转换成要求的目标类型: {1}",
						targetType.FullName, m_Parameters[index].ParameterType.FullName));

				m_Providers[index] = provider;
			}
		}
	}

	public delegate object? ObjectProvider();
	public delegate TResult? ObjectProvider<TResult>();

	/// <summary>
	/// 对普通方法进行DI包装的构建器接口
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadUnsafe]
	public interface IDIMethodBuilder
	{
		/// <summary>
		/// 获取被包装的方法
		/// </summary>
		MethodInfo Method { get; }

		/// <summary>
		/// 对给定名称的参数添加依赖注入
		/// </summary>
		/// <param name="index"></param>
		/// <param name="provider"></param>
		void InjectParameter(int index, ObjectProvider provider);

		/// <summary>
		/// <inheritdoc cref="InjectParameter(int, ObjectProvider)" path="/summary"/>
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="index"></param>
		/// <param name="provider"></param>
		void InjectParameter<TResult>(int index, ObjectProvider<TResult> provider);

		/// <summary>
		/// <inheritdoc cref="InjectParameter(int, ObjectProvider)" path="/summary"/><br/>
		/// 此方法仅接受<see cref="ObjectProvider{TResult}"/>的泛型
		/// </summary>
		/// <param name="index"></param>
		/// <param name="provider"></param>
		void InjectParameter(int index, Delegate provider);

		/// <summary>
		/// 构建携带依赖注入的方法入口
		/// </summary>
		/// <returns></returns>
		Delegate Build();

		/// <summary>
		/// 构建携带依赖注入的方法入口
		/// </summary>
		/// <typeparam name="TDelegate"></typeparam>
		/// <returns></returns>
		TDelegate Build<TDelegate>()
			where TDelegate : class, MulticastDelegate;
	}

	/// <summary>
	/// 对构造函数进行DI包装的构建器接口
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadUnsafe]
	public interface IDIConstructorBuilder
	{
		/// <summary>
		/// 获取被包装的构造函数
		/// </summary>
		ConstructorInfo Constructor { get; }

		/// <summary>
		/// 对给定名称的参数添加依赖注入
		/// </summary>
		/// <param name="index"></param>
		/// <param name="provider"></param>
		void InjectParameter(int index, ObjectProvider provider);

		/// <summary>
		/// <inheritdoc cref="InjectParameter(int, ObjectProvider)" path="/summary"/>
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="index"></param>
		/// <param name="provider"></param>
		void InjectParameter<TResult>(int index, ObjectProvider<TResult> provider);

		/// <summary>
		/// <inheritdoc cref="InjectParameter(int, ObjectProvider)" path="/summary"/><br/>
		/// 此方法仅接受<see cref="ObjectProvider{TResult}"/>的泛型
		/// </summary>
		/// <param name="index"></param>
		/// <param name="provider"></param>
		void InjectParameter(int index, Delegate provider);

		/// <summary>
		/// 对给定名称的字段或自动属性添加依赖注入<br/>
		/// 字段或自动属性的依赖注入将在构造函数执行完成后执行
		/// </summary>
		/// <param name="name"></param>
		/// <param name="provider"></param>
		void InjectField(string name, ObjectProvider provider);

		/// <summary>
		/// <inheritdoc cref="InjectField(string, ObjectProvider)" path="/summary"/>
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="name"></param>
		/// <param name="provider"></param>
		void InjectField<TResult>(string name, ObjectProvider<TResult> provider);

		/// <summary>
		/// <inheritdoc cref="InjectField(string, ObjectProvider)" path="/summary"/><br/>
		/// 此方法仅接受<see cref="ObjectProvider{TResult}"/>的泛型
		/// </summary>
		/// <param name="name"></param>
		/// <param name="provider"></param>
		void InjectField(string name, Delegate provider);

		/// <summary>
		/// 构建携带依赖注入的构造函数入口
		/// </summary>
		/// <returns></returns>
		Delegate Build();

		/// <summary>
		/// 构建携带依赖注入的构造函数入口<br/>
		/// 此方法构造的入口不会创建对象实例，需要将已经创建的对象实例作为第一个参数传入
		/// </summary>
		/// <returns></returns>
		Delegate BuildMethodOnly();

		/// <summary>
		/// 构建携带依赖注入的构造函数入口
		/// </summary>
		/// <typeparam name="TDelegate"></typeparam>
		/// <returns></returns>
		TDelegate Build<TDelegate>()
			where TDelegate : class, MulticastDelegate;
	}
}
