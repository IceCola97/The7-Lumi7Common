using Lumi7Common.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Types
{
	/// <summary>
	/// 提供Luson的List类型
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadUnsafe]
	public class StateList : IList
	{
		protected readonly IList m_List;

		protected static void EnsureAllowedValue(object? value)
		{
			if (value is null)
				throw new NotSupportedException(ET("字段不支持为空对象"));

			if (!IStateFieldStandard.Instance.IsAllowedFieldType(value.GetType()))
				throw new NotSupportedException(ET("不支持的字段类型: {0}", value.GetType().FullName));
		}

		protected StateList(IList source) => m_List = source;

		public StateList() => m_List = new ArrayList();

		public StateList(int capacity) => m_List = new ArrayList(capacity);

		public StateList(ICollection objects) => m_List = new ArrayList(objects);

		[IndexerName("Item")]
		public virtual object? this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_List[index];
			set
			{
				EnsureAllowedValue(value);
				m_List[index] = value;
			}
		}

		public virtual bool IsFixedSize => m_List.IsFixedSize;

		public virtual bool IsReadOnly => m_List.IsReadOnly;

		public virtual int Count
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_List.Count;
		}

		public virtual bool IsSynchronized => m_List.IsSynchronized;

		public virtual object SyncRoot => m_List.SyncRoot;

		public virtual int Add(object? value)
		{
			EnsureAllowedValue(value);
			return m_List.Add(value);
		}

		public virtual void AddRange(ICollection range)
		{
			foreach (var value in range)
				EnsureAllowedValue(value);

			AddRangeNoCheck(range);
		}

		protected void AddRangeNoCheck(ICollection range)
		{
			if (m_List is ArrayList arrayList)
				arrayList.AddRange(range);
			else if (m_List is StateList stateList)
				stateList.AddRange(range);
			else
				foreach (var value in range)
					m_List.Add(value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual void Clear() => m_List.Clear();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual bool Contains(object? value) => m_List.Contains(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual void CopyTo(Array array, int index) => m_List.CopyTo(array, index);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual IEnumerator GetEnumerator() => m_List.GetEnumerator();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual int IndexOf(object? value) => m_List.IndexOf(value);

		public virtual void Insert(int index, object? value)
		{
			EnsureAllowedValue(value);
			m_List.Insert(index, value);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual void Remove(object? value) => m_List.Remove(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public virtual void RemoveAt(int index) => m_List.RemoveAt(index);

		/// <summary>
		/// 将当前列表包裹为线程安全的
		/// </summary>
		/// <returns></returns>
		public StateList AsSafe() => IsSynchronized ? this : new SafeStateList(this);

		/// <summary>
		/// 将当前列表的所有内容复制到一个新的线程安全的列表
		/// </summary>
		/// <returns></returns>
		public StateList ToSafe() => new SafeStateList((ICollection)this);

		/// <summary>
		/// 将当前列表包裹为只读的
		/// </summary>
		/// <returns></returns>
		public StateList AsReadOnly() => IsReadOnly ? this : new ReadOnlyStateList(this);

		/// <summary>
		/// 将当前列表的所有内容复制到一个新的只读的列表
		/// </summary>
		/// <returns></returns>
		public StateList ToReadOnly() => new ReadOnlyStateList((ICollection)this);

		protected virtual object[] ToArray()
		{
			if (m_List is StateList stateList)
				return stateList.ToArray();

			var array = new object[m_List.Count];
			m_List.CopyTo(array, 0);
			return array;
		}

		public override string ToString()
		{
			var items = ToArray();
			int count = Math.Min(items.Length, 10);
			int more = items.Length - count;

			var sb = new StringBuilder();

			sb.Append("{ ");

			if (count > 0)
			{
				sb.Append(FieldConvert.ToString(items[0]));

				if (count > 1)
				{
					for (int i = 1; i < count; i++)
					{
						sb.Append(", ");
						sb.Append(FieldConvert.ToString(items[i]));
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
	/// <see cref="StateList"/>的线程安全实现
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public sealed class SafeStateList : StateList
	{
		private readonly ReadWriteLock m_Lock = new();

		public SafeStateList(StateList source) : base(source) { }

		public SafeStateList() : base() { }

		public SafeStateList(int capacity) : base(capacity) { }

		public SafeStateList(ICollection objects) : base(objects) { }

		public override object? this[int index]
		{
			get
			{
				using (m_Lock.Read())
				{
					return base[index];
				}
			}
			set
			{
				using (m_Lock.Write())
				{
					base[index] = value;
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

		public override int Add(object? value)
		{
			using (m_Lock.Write())
			{
				return base.Add(value);
			}
		}

		public override void AddRange(ICollection range)
		{
			foreach (var item in range)
				EnsureAllowedValue(item);

			using (m_Lock.Write())
			{
				AddRangeNoCheck(range);
			}
		}

		public override void Clear()
		{
			using (m_Lock.Write())
			{
				base.Clear();
			}
		}

		public override bool Contains(object? value)
		{
			using (m_Lock.Read())
			{
				return base.Contains(value);
			}
		}

		public override void CopyTo(Array array, int index)
		{
			using (m_Lock.Read())
			{
				base.CopyTo(array, index);
			}
		}

		public override IEnumerator GetEnumerator()
		{
			using (m_Lock.Read())
			{
				return new SafeEnumerator(m_Lock, base.GetEnumerator());
			}
		}

		public override int IndexOf(object? value)
		{
			using (m_Lock.Read())
			{
				return base.IndexOf(value);
			}
		}

		public override void Insert(int index, object? value)
		{
			using (m_Lock.Write())
			{
				base.Insert(index, value);
			}
		}

		public override void Remove(object? value)
		{
			using (m_Lock.Write())
			{
				base.Remove(value);
			}
		}

		public override void RemoveAt(int index)
		{
			using (m_Lock.Write())
			{
				base.RemoveAt(index);
			}
		}

		protected override object[] ToArray()
		{
			using (m_Lock.Read())
			{
				return base.ToArray();
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
	}

	/// <summary>
	/// <see cref="StateList"/>的只读实现
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public sealed class ReadOnlyStateList : StateList
	{
		public ReadOnlyStateList(StateList source) : base(source) { }

		public ReadOnlyStateList(ICollection objects) : base(objects) { }

		public override object? this[int index]
		{
			get => base[index];
			set => throw new InvalidOperationException(ET("当前集合是只读的"));
		}

		public override bool IsFixedSize => true;

		public override bool IsReadOnly => true;

		public override int Add(object? value)
			=> throw new InvalidOperationException(ET("当前集合是只读的"));

		public override void AddRange(ICollection range)
			=> throw new InvalidOperationException(ET("当前集合是只读的"));

		public override void Clear()
			=> throw new InvalidOperationException(ET("当前集合是只读的"));

		public override void Insert(int index, object? value)
			=> throw new InvalidOperationException(ET("当前集合是只读的"));

		public override void Remove(object? value)
			=> throw new InvalidOperationException(ET("当前集合是只读的"));

		public override void RemoveAt(int index)
			=> throw new InvalidOperationException(ET("当前集合是只读的"));
	}
}
