using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Threading
{
	/// <summary>
	/// 类<see cref="ReaderWriterLockSlim"/>的便捷版封装
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	/// <param name="allowRecursion"></param>
	[ThreadSafe]
	public sealed class ReadWriteLock(bool allowRecursion = true)
	{
		private readonly ReaderWriterLockSlim m_Core = new(allowRecursion
			? LockRecursionPolicy.SupportsRecursion
			: LockRecursionPolicy.NoRecursion);

		public bool IsHeldRead => m_Core.IsReadLockHeld;

		public bool IsHeldUpgradeableRead => m_Core.IsUpgradeableReadLockHeld;

		public bool IsHeldWrite => m_Core.IsWriteLockHeld;

		public void EnterRead() => m_Core.EnterReadLock();

		public bool TryEnterRead(int msTimeout) => m_Core.TryEnterReadLock(msTimeout);

		public void ExitRead() => m_Core.ExitReadLock();

		public void EnterUpgradeableRead() => m_Core.EnterUpgradeableReadLock();

		public bool TryEnterUpgradeableRead(int msTimeout) => m_Core.TryEnterUpgradeableReadLock(msTimeout);

		public void ExitUpgradeableRead() => m_Core.ExitUpgradeableReadLock();

		public void EnterWrite() => m_Core.EnterWriteLock();

		public bool TryEnterWrite(int msTimeout) => m_Core.TryEnterWriteLock(msTimeout);

		public void ExitWrite() => m_Core.ExitWriteLock();

		public ReadLock Read()
		{
			EnterRead();
			return new ReadLock(this);
		}

		public ReadLock TryRead(int msTimeout, out bool lockTaken)
		{
			lockTaken = TryEnterRead(msTimeout);

			if (!lockTaken)
				return default;

			return new ReadLock(this);
		}

		public UpgradeableReadLock UpgradeableRead()
		{
			EnterUpgradeableRead();
			return new UpgradeableReadLock(this);
		}

		public UpgradeableReadLock TryUpgradeableRead(int msTimeout, out bool lockTaken)
		{
			lockTaken = TryEnterUpgradeableRead(msTimeout);

			if (!lockTaken)
				return default;

			return new UpgradeableReadLock(this);
		}

		public WriteLock Write()
		{
			EnterWrite();
			return new WriteLock(this);
		}

		public WriteLock TryWrite(int msTimeout, out bool lockTaken)
		{
			lockTaken = TryEnterWrite(msTimeout);

			if (!lockTaken)
				return default;

			return new WriteLock(this);
		}

		public readonly struct ReadLock
			(ReadWriteLock host) : IDisposable
		{
			private readonly ReadWriteLock? m_Host = host;

			public void Dispose()
			{
				m_Host?.ExitRead();
			}
		}

		public readonly struct UpgradeableReadLock
			(ReadWriteLock host) : IDisposable
		{
			private readonly ReadWriteLock? m_Host = host;

			public void Dispose()
			{
				m_Host?.ExitUpgradeableRead();
			}
		}

		public readonly struct WriteLock
			(ReadWriteLock host) : IDisposable
		{
			private readonly ReadWriteLock? m_Host = host;

			public void Dispose()
			{
				m_Host?.ExitWrite();
			}
		}
	}
}
