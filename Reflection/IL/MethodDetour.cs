using Lumi7Common.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 结合<see cref="MethodPatch"/>、<see cref="MethodProxy"/>、<see cref="MethodCopy"/>等类的能力提供动态地向指定方法增加、删减、替换、修补代码的方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public sealed class MethodDetour
	{
		private static readonly ReadWriteLock m_TableLock = new();
		private static readonly Dictionary<MethodBase, MethodDetour> m_DetourRefTable = [];

		private readonly MethodBase m_TargetMethod;
		private readonly DynamicMethod m_ClonedMethod;
		private readonly ChainNext m_NextCloned;
		private readonly Delegate m_HookedDelegate;

		private readonly ReadWriteLock m_ChainLock = new();
		private readonly LinkedList<ChainCallEvent> m_CallChain = [];
		private readonly ThreadLocal<ChainCallEvent[]?> m_CallingChains = new(false);

		/// <summary>
		/// 调用前事件，当目标方法被调用时，按照安装顺序，从最先安装的开始调用<br/>
		/// 每个调用节点都可以通过指定参数prevent为true来跳过后续的调用前事件、链式调用事件、原方法克隆体，并将当前方法的返回值作为第一个调用后事件的returned参数
		/// </summary>
		public event BeforeCallEvent? BeforeCall;

		/// <summary>
		/// 调用后事件，当目标方法被调用时，按照安装顺序，从最先安装的开始调用<br/>
		/// 每个调用节点都可以通过参数returned获取上一个节点的返回值，并通过返回决定下一个节点的returned参数<br/>
		/// 第一个调用后事件节点的参数returned的值取决于链式调用中最后安装的节点的返回值，如果链式调用中没有节点，则使用原函数克隆的返回值
		/// </summary>
		public event AfterCallEvent? AfterCall;

		/// <summary>
		/// 链式调用事件，处于调用前与调用后之间，当目标方法被调用时，链式调用将按照安装顺序，从最后安装的开始调用<br/>
		/// 如果每个调用节点都调用下一个节点，那么最后的调用顺序是: nodeN -> ... -> node2 -> node1 -> 原方法克隆体
		/// </summary>
		public event ChainCallEvent? ChainCall
		{
			add
			{
				if (value is null)
					return;

				using (m_ChainLock.UpgradeableRead())
				{
					if (m_CallChain.Contains(value))
						return;

					using (m_ChainLock.Write())
						m_CallChain.AddLast(value);
				}
			}
			remove
			{
				if (value is null)
					return;

				using (m_ChainLock.UpgradeableRead())
				{
					var node = m_CallChain.Find(value);

					if (node is null)
						return;

					using (m_ChainLock.Write())
						m_CallChain.Remove(node);
				}
			}
		}

		static MethodDetour()
		{
			DynamicSupport.AssertDynamicSupport();

			if (!MethodPatch.IsRuntimePatchSupported)
				throw new PlatformNotSupportedException(ET("当前平台不支持"));
		}

		private MethodDetour(MethodBase method)
		{
			if (method is DynamicMethod)
				throw new NotSupportedException(ET("不支持给定的方法"));
			if (method.IsGenericMethodDefinition)
				throw new NotSupportedException(ET("不支持给定的方法"));
			if (method.CallingConvention.HasFlag(CallingConventions.VarArgs))
				throw new NotSupportedException(ET("不支持给定的方法"));
			if (method is MethodInfo methodInfo && methodInfo.ReturnType.IsByRef)
				throw new NotSupportedException(ET("不支持给定的方法"));

			using (m_TableLock.Read())
				if (m_DetourRefTable.ContainsKey(method))
					throw new SpecifiedMethodDetouredException();

			m_TargetMethod = method;
			m_ClonedMethod = method.CopyMethod();
			m_NextCloned = method.IsStatic
				? CallStaticCloned
				: CallInstanceCloned;

			string signature = DelegateBuilder.GenerateSignature(method, true);
			var delegateType = DelegateCache.ObtainDelegate(signature);

			m_HookedDelegate = method.IsStatic
				? MethodProxy.CreateStaticProxy(HandleMethodCall, delegateType)
				: MethodProxy.CreateThisCallProxy(HandleMethodCall, delegateType);

			using (m_TableLock.UpgradeableRead())
			{
				if (m_DetourRefTable.ContainsKey(method))
					throw new SpecifiedMethodDetouredException();

				MethodPatch.RuntimePatch(method, m_HookedDelegate);

				using (m_TableLock.Write())
					m_DetourRefTable[method] = this;
			}
		}

		/// <summary>
		/// 替换的目标方法，无论通过什么方式调用，将依次触发当前对象的<see cref="BeforeCall"/>、<see cref="ChainCall"/>、<see cref="AfterCall"/>
		/// </summary>
		public MethodBase Target => m_TargetMethod;

		/// <summary>
		/// 目标方法的克隆体，替换方法会对目标方法造成不可逆的损伤，所以将目标方法的源代码克隆并生成克隆体来代替原方法工作
		/// </summary>
		public DynamicMethod Cloned => m_ClonedMethod;

		/// <summary>
		/// 为原方法克隆体进行委托包裹
		/// </summary>
		/// <param name="delegateType"></param>
		/// <returns></returns>
		public Delegate WrapCloned(Type delegateType)
			=> m_ClonedMethod.CreateDelegate(delegateType);

		/// <summary>
		/// 为原方法克隆体进行委托包裹
		/// </summary>
		/// <param name="delegateType"></param>
		/// <returns></returns>
		public TDelegate WrapCloned<TDelegate>() where TDelegate : class, MulticastDelegate
			=> m_ClonedMethod.CreateDelegate<TDelegate>();

		/// <summary>
		/// 执行原方法克隆体
		/// </summary>
		/// <param name="instance"></param>
		/// <param name="parameters"></param>
		/// <returns></returns>
		public object? InvokeCloned(object? instance, object?[] parameters)
			=> m_NextCloned.Invoke(instance, parameters);

		/// <summary>
		/// 创建或获取现有的关于给定方法的<see cref="MethodDetour"/>对象
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static MethodDetour Detour(MethodBase method)
		{
			MethodDetour? detour;

			using (m_TableLock.Read())
				if (m_DetourRefTable.TryGetValue(method, out detour))
					return detour;

			try
			{
				detour = new MethodDetour(method);
				return detour;
			}
			catch (SpecifiedMethodDetouredException)
			{
				using (m_TableLock.Read())
					if (m_DetourRefTable.TryGetValue(method, out detour))
						return detour;
			}

			throw new ImpossibleException();
		}

		[DebuggerHidden]
		[SkipLocalsInit]
		private object? HandleMethodCall(object? instance, object?[] parameters)
		{
			object? returned;

			var beforeCall = BeforeCall;

			if (beforeCall is not null)
			{
				foreach (var item in beforeCall.GetInvocationList())
				{
					if (item is BeforeCallEvent beforeCallEvent)
					{
						returned = beforeCallEvent.Invoke(out bool prevent,
							instance, parameters);

						if (prevent)
							goto BeforePrevent;
					}
				}
			}

			Unsafe.SkipInit(out ChainNode lastNode);
			bool hasLastNode;

			using (m_ChainLock.Read())
			{
				hasLastNode =
					(m_CallingChains.Value =
						[.. m_CallChain]).Length > 0;

				if (hasLastNode)
					lastNode = ChainNode.GetLastNode(this);
			}

			try
			{
				if (hasLastNode)
					returned = CallChainNode(ref lastNode, instance, parameters);
				else
					returned = m_NextCloned.Invoke(instance, parameters);
			}
			finally
			{
				m_CallingChains.Value = null;
			}

		BeforePrevent:
			var afterCall = AfterCall;

			if (afterCall is not null)
			{
				foreach (var item in afterCall.GetInvocationList())
				{
					if (item is AfterCallEvent afterCallEvent)
					{
						returned = afterCallEvent.Invoke(returned,
							instance, parameters);
					}
				}
			}

			return returned;
		}

		[DebuggerHidden]
		[SkipLocalsInit]
		private static object? CallChainNode(ref ChainNode node, object? instance, object?[] parameters)
		{
			ChainNext callNext;

			if (node.HasNext)
			{
				ChainNode nextNode = node.Next;

				callNext = (instance, parameters) =>
					CallChainNode(ref nextNode, instance, parameters);
			}
			else
				callNext = node.Cloned;

			return node.Current.Invoke(callNext, instance, parameters);
		}

		[DebuggerHidden]
		[SkipLocalsInit]
		private object? CallInstanceCloned(object? instance, object?[] parameters)
		{
			object?[] args = new object?[parameters.Length + 1];
			args[0] = instance;
			Array.Copy(parameters, 0, args, 1, parameters.Length);

			object? returned;

			try
			{
				returned = m_ClonedMethod.Invoke(null, args);
			}
			catch (TargetInvocationException ex)
			{
				throw ex.GetBaseException();
			}

			Array.Copy(args, 1, parameters, 0, parameters.Length);
			return returned;
		}

		[DebuggerHidden]
		[SkipLocalsInit]
		private object? CallStaticCloned(object? instance, object?[] parameters)
		{
			object? returned;

			try
			{
				returned = m_ClonedMethod.Invoke(null, parameters);
			}
			catch (TargetInvocationException ex)
			{
				throw ex.GetBaseException();
			}

			return returned;
		}

		private readonly struct ChainNode
		{
			private readonly MethodDetour m_Detour;
			private readonly int m_Index;

			private ChainNode(MethodDetour detour, int index)
			{
				m_Detour = detour;
				m_Index = index;
			}

			private static ChainCallEvent[] GetCurrentCallingChain(MethodDetour detour)
			{
				if (!detour.m_CallingChains.IsValueCreated
					|| detour.m_CallingChains.Value is not ChainCallEvent[] chain)
					throw new InvalidOperationException(ET("当前线程没有任何调用链"));

				return chain;
			}

			public static ChainNode GetLastNode(MethodDetour detour)
			{
				var chain = GetCurrentCallingChain(detour);
				return new ChainNode(detour, chain.Length - 1);
			}

			public bool HasNext => m_Index > 0;

			public ChainNode Next
			{
				get
				{
					if (m_Index <= 0)
						throw new InvalidOperationException(ET("当前调用链节点没有下一个节点"));

					return new(m_Detour, m_Index - 1);
				}
			}

			public ChainCallEvent Current => GetCurrentCallingChain(m_Detour)[m_Index];

			public ChainNext Cloned => m_Detour.m_NextCloned;
		}

		private sealed class SpecifiedMethodDetouredException : Exception
		{
			public SpecifiedMethodDetouredException()
				: base(ET("给定方法已经被替换")) { }
		}
	}

	public delegate object? BeforeCallEvent(out bool prevent, object? instance, object?[] parameters);
	public delegate object? ChainCallEvent(ChainNext next, object? instance, object?[] parameters);
	public delegate object? AfterCallEvent(object? returned, object? instance, object?[] parameters);
	public delegate object? ChainNext(object? instance, object?[] parameters);
}
