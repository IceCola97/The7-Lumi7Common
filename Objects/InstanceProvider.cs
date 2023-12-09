using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Objects
{
	/// <summary>
	/// 提供一个缓存指定类型对象的实例托管器
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[ThreadSafe]
	public sealed class InstanceProvider<T> where T : class
	{
		private readonly object m_Lock = new();
		private readonly InstanceFactory<T>? m_Factory;

		private volatile Thread? m_CreatingThread = null;
		private volatile T? m_Instance = null;

		/// <summary>
		/// 创建一个将IOC容器作为对象提供器的实例托管器
		/// </summary>
		public InstanceProvider() => m_Factory = null;

		/// <summary>
		/// 创建一个指定对象提供器的实例托管器
		/// </summary>
		/// <param name="factory">对象提供器</param>
		public InstanceProvider(InstanceFactory<T>? factory) => m_Factory = factory;

		/// <summary>
		/// 获取托管的实例对象
		/// </summary>
		public T Instance
		{
			get
			{
				if (m_Instance is not null)
					return m_Instance;

				lock (m_Lock)
				{
					if (m_Instance is not null)
						return m_Instance;

					if (m_CreatingThread is not null)
						throw new InvalidOperationException(ET("在实例创建工厂方法中访问实例托管方法是不应该的"));

					m_CreatingThread = Thread.CurrentThread;

					try
					{
						m_Instance = m_Factory is null
							? ObjectContainer.Require<T>()
							: m_Factory.Invoke();

						if (m_Instance is null)
							throw new NullReferenceException(ET("实例创建工厂方法给出的实例对象不应该为空"));
					}
					finally
					{
						m_CreatingThread = null;
					}
				}

				return m_Instance;
			}
		}

		/// <summary>
		/// 判断是否正在调用实例创建工厂方法
		/// </summary>
		public bool IsCreating => m_CreatingThread is not null;

		/// <summary>
		/// 判断是否是当前线程正在调用实例创建工厂方法
		/// </summary>
		public bool IsCreatingByCurrent => Thread.CurrentThread.Equals(m_CreatingThread);

		/// <summary>
		/// 判断实例对象是否已经创建
		/// </summary>
		public bool IsCreated => m_Instance is not null;
	}

	public delegate T InstanceFactory<T>() where T : class;
}
