using Lumi7Common.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Types
{
	/// <summary>
	/// 提供Luson的Map类型
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadUnsafe]
	public class StateMap : IDictionary
	{
		protected readonly IDictionary m_Table;

		protected static void EnsureAllowedValue(object? value)
		{
			if (value is null)
				throw new NotSupportedException(ET("字段不支持为空对象"));

			if (!IStateFieldStandard.Instance.IsAllowedFieldType(value.GetType()))
				throw new NotSupportedException(ET("不支持的字段类型: {0}", value.GetType().FullName));
		}

		protected StateMap(StateMap source) => m_Table = source;

		public StateMap() => m_Table = new Hashtable();

		public StateMap(int capacity) => m_Table = new Hashtable(capacity);

		public StateMap(IDictionary objects) => m_Table = new Hashtable(objects);

		[IndexerName("Item")]
		public virtual object? this[object key]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Table[key]
					?? throw new KeyNotFoundException(ET("给定键在Map中没有找到: {0}", key));
			set
			{
				EnsureAllowedValue(key);
				EnsureAllowedValue(value);
				m_Table[key] = value;
			}
		}

		public virtual bool IsFixedSize => m_Table.IsFixedSize;

		public virtual bool IsReadOnly => m_Table.IsReadOnly;

		public virtual ICollection Keys
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Table.Keys;
		}

		public virtual ICollection Values
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Table.Values;
		}

		public virtual int Count
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Table.Count;
		}

		public virtual bool IsSynchronized => m_Table.IsSynchronized;

		public virtual object SyncRoot => m_Table.SyncRoot;

		public virtual void Add(object key, object? value)
		{
			EnsureAllowedValue(key);
			EnsureAllowedValue(value);
			m_Table.Add(key, value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual void Clear() => m_Table.Clear();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual bool Contains(object key) => m_Table.Contains(key);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual void CopyTo(Array array, int index) => m_Table.CopyTo(array, index);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual IDictionaryEnumerator GetEnumerator() => m_Table.GetEnumerator();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual void Remove(object key) => m_Table.Remove(key);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual bool TryGetValue(object key, [NotNullWhen(true)] out object? value)
			=> (value = m_Table[key]) is not null;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		/// <summary>
		/// 将当前字典包裹为线程安全的
		/// </summary>
		/// <returns></returns>
		public StateMap AsSafe() => IsSynchronized ? this : new SafeStateMap(this);

		/// <summary>
		/// 将当前字典的所有内容复制到一个新的线程安全的字典
		/// </summary>
		/// <returns></returns>
		public StateMap ToSafe() => new SafeStateMap((IDictionary)this);

		/// <summary>
		/// 将当前字典包裹为只读的
		/// </summary>
		/// <returns></returns>
		public StateMap AsReadOnly() => IsReadOnly ? this : new ReadOnlyStateMap(this);

		/// <summary>
		/// 将当前字典的所有内容复制到一个新的只读的字典
		/// </summary>
		/// <returns></returns>
		public StateMap ToReadOnly() => new ReadOnlyStateMap((IDictionary)this);

		private static string EntryToString(DictionaryEntry entry)
			=> $"<{FieldConvert.ToString(entry.Key)}: {FieldConvert.ToString(entry.Value)}>";

		protected virtual DictionaryEntry[] ToEntries()
		{
			if (m_Table is StateMap stateMap)
				return stateMap.ToEntries();

			var array = new DictionaryEntry[m_Table.Count];
			m_Table.CopyTo(array, 0);
			return array;
		}

		public override string ToString()
		{
			var items = ToEntries();
			int count = Math.Min(items.Length, 10);
			int more = items.Length - count;
			var sb = new StringBuilder();

			sb.Append("{ ");

			if (count > 0)
			{
				sb.Append(EntryToString(items[0]));

				if (count > 1)
				{
					for (int i = 1; i < count; i++)
					{
						sb.Append(", ");
						sb.Append(EntryToString(items[i]));
					}
				}

				sb.Append(' ');
			}

			if (more > 0)
				sb.Append("... ");

			sb.Append('}');

			return sb.ToString();
		}
	}

	/// <summary>
	/// <see cref="StateMap"/>的线程安全实现
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public sealed class SafeStateMap : StateMap
	{
		private readonly ReadWriteLock m_Lock = new();

		public SafeStateMap(StateMap source) : base(source) { }

		public SafeStateMap() : base() { }

		public SafeStateMap(int capacity) : base(capacity) { }

		public SafeStateMap(IDictionary objects) : base(objects) { }

		public override object? this[object key]
		{
			get
			{
				using (m_Lock.Read())
				{
					return base[key];
				}
			}
			set
			{
				using (m_Lock.Write())
				{
					base[key] = value;
				}
			}
		}

		public override int Count
		{
			get
			{
				using (m_Lock.Read())
				{
					return base.Count;
				}
			}
		}

		public override bool IsSynchronized => true;

		public ReadWriteLock Lock => m_Lock;

		public override object SyncRoot => m_Lock;

		public override ICollection Keys
		{
			get
			{
				using (m_Lock.Read())
				{
					return new SafeCollection(m_Lock, base.Keys);
				}
			}
		}

		public override ICollection Values
		{
			get
			{
				using (m_Lock.Read())
				{
					return new SafeCollection(m_Lock, base.Values);
				}
			}
		}

		public override void Add(object key, object? value)
		{
			using (m_Lock.Write())
			{
				base.Add(key, value);
			}
		}

		public override void Clear()
		{
			using (m_Lock.Write())
			{
				base.Clear();
			}
		}

		public override bool Contains(object key)
		{
			using (m_Lock.Read())
			{
				return base.Contains(key);
			}
		}

		public override void CopyTo(Array array, int index)
		{
			using (m_Lock.Read())
			{
				base.CopyTo(array, index);
			}
		}

		public override IDictionaryEnumerator GetEnumerator()
		{
			using (m_Lock.Read())
			{
				return new SafeDictionaryEnumerator(m_Lock, base.GetEnumerator());
			}
		}

		public override void Remove(object key)
		{
			using (m_Lock.Write())
			{
				base.Remove(key);
			}
		}

		public override bool TryGetValue(object key, [NotNullWhen(true)] out object? value)
		{
			using (m_Lock.Read())
			{
				return base.TryGetValue(key, out value);
			}
		}

		protected override DictionaryEntry[] ToEntries()
		{
			using (m_Lock.Read())
			{
				return base.ToEntries();
			}
		}

		private sealed class SafeEnumerator
			(ReadWriteLock @lock, IEnumerator source) : IEnumerator
		{
			private readonly ReadWriteLock m_Lock = @lock;
			private readonly IEnumerator m_Source = source;

			public object Current
			{
				get
				{
					using (m_Lock.Read())
					{
						return m_Source.Current;
					}
				}
			}

			public bool MoveNext()
			{
				using (m_Lock.Read())
				{
					return m_Source.MoveNext();
				}
			}

			public void Reset()
			{
				using (m_Lock.Read())
				{
					m_Source.Reset();
				}
			}
		}

		private sealed class SafeCollection
			(ReadWriteLock @lock, ICollection source) : ICollection
		{
			private readonly ReadWriteLock m_Lock = @lock;
			private readonly ICollection m_Source = source;

			public int Count
			{
				get
				{
					using (m_Lock.Read())
					{
						return m_Source.Count;
					}
				}
			}

			public bool IsSynchronized => true;

			public object SyncRoot => m_Lock;

			public void CopyTo(Array array, int index)
			{
				using (m_Lock.Read())
				{
					m_Source.CopyTo(array, index);
				}
			}

			public IEnumerator GetEnumerator()
			{
				using (m_Lock.Read())
				{
					return new SafeEnumerator(m_Lock, m_Source.GetEnumerator());
				}
			}
		}

		private sealed class SafeDictionaryEnumerator
			(ReadWriteLock @lock, IDictionaryEnumerator source) : IDictionaryEnumerator
		{
			private readonly ReadWriteLock m_Lock = @lock;
			private readonly IDictionaryEnumerator m_Source = source;

			public DictionaryEntry Entry
			{
				get
				{
					using (m_Lock.Read())
					{
						return m_Source.Entry;
					}
				}
			}

			public object Key
			{
				get
				{
					using (m_Lock.Read())
					{
						return m_Source.Key;
					}
				}
			}

			public object? Value
			{
				get
				{
					using (m_Lock.Read())
					{
						return m_Source.Value;
					}
				}
			}

			public object Current
			{
				get
				{
					using (m_Lock.Read())
					{
						return m_Source.Current;
					}
				}
			}

			public bool MoveNext()
			{
				using (m_Lock.Read())
				{
					return m_Source.MoveNext();
				}
			}

			public void Reset()
			{
				using (m_Lock.Read())
				{
					m_Source.Reset();
				}
			}
		}
	}

	/// <summary>
	/// <see cref="StateList"/>的只读实现
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public sealed class ReadOnlyStateMap : StateMap
	{
		public ReadOnlyStateMap(StateMap source) : base(source) { }

		public ReadOnlyStateMap(IDictionary objects) : base(objects) { }

		public override object? this[object key]
		{
			get => base[key];
			set => throw new InvalidOperationException(ET("当前集合是只读的"));
		}

		public override bool IsFixedSize => true;

		public override bool IsReadOnly => true;

		public override void Add(object key, object? value)
			=> throw new InvalidOperationException(ET("当前集合是只读的"));

		public override void Clear()
			=> throw new InvalidOperationException(ET("当前集合是只读的"));

		public override void Remove(object key)
			=> throw new InvalidOperationException(ET("当前集合是只读的"));
	}
}
