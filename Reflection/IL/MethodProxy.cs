using Lumi7Common.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供为不定委托类型动态创建代理对象的能力
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class MethodProxy
	{
		private static readonly MethodInfo m_HandlerInvoke;

		static MethodProxy()
		{
			m_HandlerInvoke = typeof(MethodProxyHandler).GetMethod("Invoke")
				?? throw new ImpossibleException();
		}

		private static Delegate CreateProxyCore(MethodProxyHandler handler,
			Type delegateType, bool isThisCall, object? embeddedInstance)
		{
			DynamicSupport.AssertDynamicSupport();

			if (delegateType.BaseType != typeof(MulticastDelegate))
				throw new ArgumentException(ET("无效的委托类型"));

			ArgumentNullException.ThrowIfNull(handler);

			var invokeMethod = delegateType.GetMethod("Invoke", BindingFlags.Instance | BindingFlags.Public)
				?? throw new ArgumentException(ET("无效的委托类型"));

			if (invokeMethod.CallingConvention.HasFlag(CallingConventions.VarArgs))
				throw new NotSupportedException(ET("不支持给定的委托类型"));
			if (invokeMethod.ReturnType.IsByRef)
				throw new NotSupportedException(ET("不支持给定的委托类型"));

			int paramBase = isThisCall ? 1 : 0;

			var ivkParams = invokeMethod.GetParameters();
			var expList = new List<Expression>();
			var paramList = new List<Expression>();
			var refParams = new List<int>();
			var dynParams = new List<ParameterExpression>();
			var varList = new List<ParameterExpression>();
			var paramArray = Expression.Variable(typeof(object[]));
			var instanceValue = default(Expression);

			varList.Add(paramArray);

			ParameterExpression? returnValue = null;

			if (invokeMethod.ReturnType != typeof(void))
			{
				returnValue = Expression.Variable(invokeMethod.ReturnType);
				varList.Add(returnValue);
			}

			if (isThisCall)
			{
				var thisParam = ivkParams[0];

				if (thisParam.ParameterType.IsByRef)
					throw new NotSupportedException(ET("this指针不应该为引用类型"));

				var paramInstance = Expression.Parameter(thisParam.ParameterType);
				dynParams.Add(paramInstance);

				instanceValue = paramInstance;
			}
			else if (embeddedInstance is not null)
				instanceValue = Expression.Constant(embeddedInstance);

			for (int i = paramBase; i < ivkParams.Length; i++)
			{
				var param = ivkParams[i];
				var ptype = param.ParameterType;

				if (ptype.IsByRef)
					refParams.Add(i);

				var paramExp = Expression.Parameter(ptype);
				dynParams.Add(paramExp);

				if (paramExp.Type.IsValueType)
					paramList.Add(Expression.Convert(paramExp, typeof(object)));
				else
					paramList.Add(paramExp);
			}

			expList.Add(Expression.Assign(paramArray,
				Expression.NewArrayInit(typeof(object), paramList)));

			var callExp = instanceValue is null
				? Expression.Call(Expression.Constant(handler),
					m_HandlerInvoke, Expression.Constant(null), paramArray)
				: Expression.Call(Expression.Constant(handler),
					m_HandlerInvoke, instanceValue, paramArray);

			if (returnValue is not null)
			{
				Expression valueExp = callExp;

				if (invokeMethod.ReturnType != typeof(object))
					valueExp = Expression.Convert(callExp, invokeMethod.ReturnType);

				expList.Add(Expression.Assign(returnValue, valueExp));
			}
			else
				expList.Add(callExp);

			foreach (var refIdx in refParams)
			{
				var ptype = ivkParams[refIdx].ParameterType;
				var totype = ptype.GetElementType()
					?? throw new ImpossibleException(ET("参数类型是引用类型但是没有元素类型: {0}", ptype.FullName));

				Expression valueExp = Expression.ArrayIndex(paramArray, Expression.Constant(refIdx));

				if (totype != typeof(object))
					valueExp = Expression.Convert(valueExp, totype);

				expList.Add(Expression.Assign(dynParams[refIdx], valueExp));
			}

			if (returnValue is not null)
				expList.Add(returnValue);

			var body = Expression.Block(invokeMethod.ReturnType, varList, expList);

			return Expression.Lambda(delegateType, body, dynParams).Compile();
		}

		/// <summary>
		/// 通过给定委托类型包装指定的代理方法，此方法会将参数<paramref name="embedded"/>的值作为代理的instance参数
		/// </summary>
		/// <param name="handler"></param>
		/// <param name="delegateType"></param>
		/// <param name="embedded"></param>
		/// <returns></returns>
		public static Delegate CreateEmbeddedProxy(MethodProxyHandler handler, Type delegateType, object embedded)
		{
			ArgumentNullException.ThrowIfNull(embedded);
			return CreateProxyCore(handler, delegateType, false, embedded);
		}

		/// <summary>
		/// 通过给定委托类型包装指定的代理方法，此方法会将参数<paramref name="embedded"/>的值作为代理的instance参数
		/// </summary>
		/// <typeparam name="TDelegate"></typeparam>
		/// <param name="handler"></param>
		/// <param name="embedded"></param>
		/// <returns></returns>
		public static TDelegate CreateEmbeddedProxy<TDelegate>(MethodProxyHandler handler, object embedded)
			where TDelegate : class, MulticastDelegate => (TDelegate)CreateEmbeddedProxy(handler, typeof(TDelegate), embedded);

		/// <summary>
		/// 通过给定委托类型包装指定的代理方法，此方法会将给定委托的第一个参数作为代理的instance参数传递
		/// </summary>
		/// <param name="delegateType"></param>
		/// <param name="handler"></param>
		/// <returns></returns>
		public static Delegate CreateThisCallProxy(MethodProxyHandler handler, Type delegateType)
			=> CreateProxyCore(handler, delegateType, true, null);

		/// <summary>
		/// 通过给定委托类型包装指定的代理方法，此方法会将给定委托的第一个参数作为代理的instance参数传递
		/// </summary>
		/// <typeparam name="TDelegate"></typeparam>
		/// <param name="handler"></param>
		/// <returns></returns>
		public static TDelegate CreateThisCallProxy<TDelegate>(MethodProxyHandler handler)
			where TDelegate : class, MulticastDelegate => (TDelegate)CreateThisCallProxy(handler, typeof(TDelegate));

		/// <summary>
		/// 通过给定委托类型包装指定的代理方法，此方法生成的代理的第一个参数永远为<see langword="null"/>
		/// </summary>
		/// <param name="delegateType"></param>
		/// <param name="handler"></param>
		/// <returns></returns>
		public static Delegate CreateStaticProxy(MethodProxyHandler handler, Type delegateType)
			=> CreateProxyCore(handler, delegateType, false, null);

		/// <summary>
		/// 通过给定委托类型包装指定的代理方法，此方法生成的代理的第一个参数永远为<see langword="null"/>
		/// </summary>
		/// <typeparam name="TDelegate"></typeparam>
		/// <param name="handler"></param>
		/// <returns></returns>
		public static TDelegate CreateStaticProxy<TDelegate>(MethodProxyHandler handler)
			where TDelegate : class, MulticastDelegate => (TDelegate)CreateStaticProxy(handler, typeof(TDelegate));
	}

	public delegate object? MethodProxyHandler(object? instance, object?[] parameters);
}
