using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable CS8500 // 这会获取托管类型的地址、获取其大小或声明指向它的指针

namespace Lumi7Common.Types
{
	/// <summary>
	/// 提供通用、安全、快速的枚举与基元类型的转换方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public unsafe static class EnumConverter
	{
		/// <summary>
		/// 将给定枚举值转换为<see cref="byte"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static byte ToByte<T>(this T value) where T : struct, Enum
		{
			byte result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForByte);
			return result;
		}

		/// <summary>
		/// 将给定枚举值转换为<see cref="sbyte"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static sbyte ToSByte<T>(this T value) where T : struct, Enum
		{
			sbyte result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForByte);
			return result;
		}

		/// <summary>
		/// 将给定枚举值转换为<see cref="short"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static short ToInt16<T>(this T value) where T : struct, Enum
		{
			short result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForShort);
			return result;
		}

		/// <summary>
		/// 将给定枚举值转换为<see cref="ushort"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static ushort ToUInt16<T>(this T value) where T : struct, Enum
		{
			ushort result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForShort);
			return result;
		}

		/// <summary>
		/// 将给定枚举值转换为<see cref="int"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static int ToInt32<T>(this T value) where T : struct, Enum
		{
			int result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForInt);
			return result;
		}

		/// <summary>
		/// 将给定枚举值转换为<see cref="uint"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static uint ToUInt32<T>(this T value) where T : struct, Enum
		{
			uint result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForInt);
			return result;
		}

		/// <summary>
		/// 将给定枚举值转换为<see cref="long"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static long ToInt64<T>(this T value) where T : struct, Enum
		{
			long result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForLong);
			return result;
		}

		/// <summary>
		/// 将给定枚举值转换为<see cref="ulong"/>
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static ulong ToUInt64<T>(this T value) where T : struct, Enum
		{
			ulong result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForLong);
			return result;
		}

		/// <summary>
		/// 将<see cref="byte"/>值转换为给定枚举
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static T ToEnum<T>(this byte value) where T : struct, Enum
		{
			T result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForByte);
			return result;
		}

		/// <summary>
		/// 将<see cref="sbyte"/>值转换为给定枚举
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static T ToEnum<T>(this sbyte value) where T : struct, Enum
		{
			T result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForByte);
			return result;
		}

		/// <summary>
		/// 将<see cref="short"/>值转换为给定枚举
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static T ToEnum<T>(this short value) where T : struct, Enum
		{
			T result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForShort);
			return result;
		}

		/// <summary>
		/// 将<see cref="ushort"/>值转换为给定枚举
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static T ToEnum<T>(this ushort value) where T : struct, Enum
		{
			T result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForShort);
			return result;
		}

		/// <summary>
		/// 将<see cref="int"/>值转换为给定枚举
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static T ToEnum<T>(this int value) where T : struct, Enum
		{
			T result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForInt);
			return result;
		}

		/// <summary>
		/// 将<see cref="uint"/>值转换为给定枚举
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static T ToEnum<T>(this uint value) where T : struct, Enum
		{
			T result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForInt);
			return result;
		}

		/// <summary>
		/// 将<see cref="long"/>值转换为给定枚举
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static T ToEnum<T>(this long value) where T : struct, Enum
		{
			T result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForLong);
			return result;
		}

		/// <summary>
		/// 将<see cref="ulong"/>值转换为给定枚举
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		[SkipLocalsInit]
		public static T ToEnum<T>(this ulong value) where T : struct, Enum
		{
			T result;
			Unsafe.CopyBlockUnaligned(&result, &value, EnumMetadata<T>.SizeForLong);
			return result;
		}

		private static class EnumMetadata<TEnum> where TEnum : struct, Enum
		{
			public static readonly uint Size;

			public static readonly uint SizeForByte;
			public static readonly uint SizeForShort;
			public static readonly uint SizeForInt;
			public static readonly uint SizeForLong;

			static EnumMetadata()
			{
				Size = (uint)Marshal.SizeOf(
					typeof(TEnum).GetFields(
						BindingFlags.Instance
						| BindingFlags.NonPublic
						| BindingFlags.Public
					)[0].FieldType
				);

				SizeForByte = Math.Min(Size, sizeof(byte));
				SizeForShort = Math.Min(Size, sizeof(short));
				SizeForInt = Math.Min(Size, sizeof(int));
				SizeForLong = Math.Min(Size, sizeof(long));
			}
		}
	}
}
