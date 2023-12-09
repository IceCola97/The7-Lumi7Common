using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供相同签名委托类型的缓存
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	internal static class DelegateCache
	{
		private static readonly ReadWriteLock m_Lock = new();
		private static readonly Dictionary<string, Type> m_CachedDelegate = [];

		/// <summary>
		/// 根据给定签名分配一个对应的委托类型
		/// </summary>
		/// <param name="signature"></param>
		/// <returns></returns>
		public static Type ObtainDelegate(string signature)
		{
			Type? delegateType;

			using (m_Lock.Read())
			{
				m_CachedDelegate.TryGetValue(signature, out delegateType);
			}

			if (delegateType is null)
			{
				using (m_Lock.UpgradeableRead())
				{
					if (!m_CachedDelegate.TryGetValue(signature, out delegateType))
					{
						using (m_Lock.Write())
						{
							delegateType = DelegateBuilder.CreateDelegate(
								"CachedDelegate", signature);

							m_CachedDelegate.TryAdd(signature, delegateType);
						}
					}
				}
			}

			return delegateType;
		}
	}
}
