using Lumi7Common.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Types
{
	/// <summary>
	/// 提供Luson的Tuple类型
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public sealed class StateTuple : ICollection, IEnumerable<object>, IEquatable<StateTuple?>,
		IEqualityOperators<StateTuple?, StateTuple?, bool>
	{
		private readonly ImmutableArray<object> m_Source;

		private static void EnsureAllowedValue(object? value)
		{
			if (value is null)
				throw new NotSupportedException(ET("字段不支持为空对象"));

			if (!IStateFieldStandard.Instance.IsAllowedFieldType(value.GetType()))
				throw new NotSupportedException(ET("不支持的字段类型: {0}", value.GetType().FullName));
		}

		public StateTuple(ICollection source)
		{
			var array = new object[source.Count];
			source.CopyTo(array, 0);

			for (int i = array.Length - 1; i >= 0; i--)
				EnsureAllowedValue(array[i]);

			m_Source = [.. array];
		}

		public StateTuple(IEnumerable<object> source)
		{
			var array = source.ToArray();

			for (int i = array.Length - 1; i >= 0; i--)
				EnsureAllowedValue(array[i]);

			m_Source = [.. array];
		}

		public object this[int index]
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Source[index];
		}

		public int Count
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => m_Source.Length;
		}

		public bool IsSynchronized => true;

		public object SyncRoot => this;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Contains(object value) => m_Source.Contains(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public int IndexOf(object value) => m_Source.IndexOf(value);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void CopyTo(Array array, int index)
		{
			((ICollection)m_Source).CopyTo(array, index);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((ICollection)m_Source).GetEnumerator();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		IEnumerator<object> IEnumerable<object>.GetEnumerator()
		{
			return ((IEnumerable<object>)m_Source).GetEnumerator();
		}

		private static bool CompareArray(ImmutableArray<object> left, ImmutableArray<object> right)
		{
			if (left.Length != right.Length)
				return false;

			for (int i = left.Length - 1; i >= 0; i--)
			{
				var leftItem = left[i];
				var rightItem = right[i];

				if (leftItem is null)
				{
					if (rightItem is not null)
						return false;
				}
				else if (!leftItem.Equals(rightItem))
					return false;
			}

			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override bool Equals(object? obj)
		{
			return Equals(obj as StateTuple);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool Equals(StateTuple? other)
		{
			return other is not null &&
				   CompareArray(m_Source, other.m_Source);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public override int GetHashCode()
		{
			return HashCode.Combine(m_Source);
		}

		public override string ToString()
		{
			var items = m_Source;
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator ==(StateTuple? left, StateTuple? right)
			=> left is null ? right is null : left.Equals(right);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool operator !=(StateTuple? left, StateTuple? right)
			=> !(left == right);
	}
}
