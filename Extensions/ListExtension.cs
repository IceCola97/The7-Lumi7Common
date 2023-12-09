using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Lumi7Common.Extensions
{
	/// <summary>
	/// 列表杂项扩展方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class ListExtension
	{
		/// <summary>
		/// 尝试获取给定索引的元素，如果索引超出返回则获取失败
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="index"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		[ThreadUnsafe]
		public static bool TryGetValue<T>(this IList<T> list, int index, [NotNullWhen(true)] out T? value) where T : notnull
		{
			value = default;

			if (index < 0 || index >= list.Count)
				return false;

			value = list[index];
			return true;
		}

		/// <summary>
		/// 尝试获取给定索引的非空元素
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="index"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		[ThreadUnsafe]
		public static bool TryGetNotNullValue<T>(this IList<T?> list, int index, [NotNullWhen(true)] out T? value)
		{
			value = default;

			if (index < 0 || index >= list.Count)
				return false;

			value = list[index];
			if (value is null)
				return false;

			return true;
		}

		/// <summary>
		/// 弹出列表第一个元素
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <returns></returns>
		[ThreadUnsafe]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Pop<T>(this IList<T> list)
		{
			var element = list[0];
			list.RemoveAt(0);
			return element;
		}

		/// <summary>
		/// 插入到列表第一个元素
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		[ThreadUnsafe]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Push<T>(this IList<T> list, T value) => list.Insert(0, value);

		/// <summary>
		/// 弹出列表最后一个元素
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <returns></returns>
		[ThreadUnsafe]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Drop<T>(this IList<T> list)
		{
			int count = list.Count;
			var element = list[count - 1];
			list.RemoveAt(count - 1);
			return element;
		}

		/// <summary>
		/// 插入到列表最后一个元素
		/// </summary>
		/// <param name="list"></param>
		/// <param name="value"></param>
		[ThreadUnsafe]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Append<T>(this IList<T> list, T value) => list.Add(value);

		/// <summary>
		/// 使用二分法搜索不大于给定键值的最大键所在的索引
		/// </summary>
		/// <typeparam name="TKey"></typeparam>
		/// <typeparam name="TValue"></typeparam>
		/// <param name="list"></param>
		/// <param name="key"></param>
		/// <param name="comparer"></param>
		/// <returns></returns>
		[ThreadUnsafe]
		public static int BinarySearchRange<TKey, TValue>(this SortedList<TKey, TValue> list,
			TKey key, IComparer<TKey>? comparer = null) where TKey : notnull
		{
			if (list.Count == 0)
				return -1;

			comparer ??= Comparer<TKey>.Default;

			int left = 0;
			int right = list.Count;
			int mid = ((right - left) >> 1) + left;

			while (left < right)
			{
				var midKey = list.GetKeyAtIndex(mid);

				if (comparer.Compare(midKey, key) <= 0)
				{
					var midNext = mid + 1;

					if (midNext >= right)
						return mid;

					var midNextKey = list.GetKeyAtIndex(midNext);

					if (comparer.Compare(midNextKey, key) > 0)
						return mid;

					left = midNext;
					continue;
				}

				right = mid;
			}

			return -1;
		}
	}
}
