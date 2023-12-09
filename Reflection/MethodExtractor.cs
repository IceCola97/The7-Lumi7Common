using Lumi7Common.Threading;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection
{
	/// <summary>
	/// 提供获取类方法的快捷手段，并在借助<see langword="typeof"/>与<see langword="nameof"/>时可以在重命名类与方法后同步更新名称
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class MethodExtractor
	{
		private const BindingFlags DefaultFlags =
			BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

		private static readonly ConvertOptions m_ConvertOptions = new ConvertOptions()
		{
			MixBoolean = false,
			MixCharacter = false,
			MixNumber = false,
			MixPointer = false,
			MixStringIn = false,
			MixStringOut = false,
			MixValueType = false,
		}.Seal();

		/// <summary>
		/// 在给定类上提取给定方法
		/// </summary>
		/// <param name="type"></param>
		/// <param name="methodName"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		/// <exception cref="MissingMethodException"></exception>
		public static MethodInfo ExtractMethod(this Type type, string methodName, params Type[] types)
		{
			try
			{
				var method = type.GetMethod(methodName, DefaultFlags);

				if (method is not null)
					return method;
			}
			catch { }

			return type.GetMethod(methodName, DefaultFlags, types)
					?? throw new MissingMethodException(ET("无法找到指定的方法: {0}::{1}", type.FullName, methodName));
		}

		/// <summary>
		/// 在给定类上提取给定的泛型方法
		/// </summary>
		/// <param name="type"></param>
		/// <param name="methodName"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		/// <exception cref="MissingMethodException"></exception>
		public static MethodInfo ExtractGenericMethod(this Type type, string methodName, int genericCount, params Type[] types)
		{
			try
			{
				return type.ExtractMethod(methodName, types);
			}
			catch (AmbiguousMatchException) { }

			Func<MethodInfo, bool> condition = genericCount <= 0
				? m => !m.IsGenericMethod && m.Name == methodName
				: m => m.IsGenericMethod && m.GetGenericArguments().Length == genericCount && m.Name == methodName;

			return (MethodInfo)(type.GetMethods().Where(condition)
				.FindBestMatch(types, m_ConvertOptions)
				?? throw new MissingMethodException(ET("无法找到指定的方法: {0}::{1}",
				type.FullName, methodName)));
		}

		/// <summary>
		/// 在给定类上提取给定的属性读取器
		/// </summary>
		/// <param name="type"></param>
		/// <param name="propertyName"></param>
		/// <returns></returns>
		/// <exception cref="MissingMemberException"></exception>
		public static MethodInfo ExtractGetter(this Type type, string propertyName)
		{
			var property = type.GetProperty(propertyName, DefaultFlags)
					?? throw new MissingMemberException(ET("无法找到指定的属性: {0}::{1}", type.FullName, propertyName));

			var method = property.GetGetMethod(true)
				?? throw new MissingMemberException(ET("无法找到指定属性的访问器: {0}::{1}", type.FullName, propertyName));

			return method;
		}

		/// <summary>
		/// 在给定类上提取给定的属性写入器
		/// </summary>
		/// <param name="type"></param>
		/// <param name="propertyName"></param>
		/// <returns></returns>
		/// <exception cref="MissingMemberException"></exception>
		public static MethodInfo ExtractSetter(this Type type, string propertyName)
		{
			var property = type.GetProperty(propertyName, DefaultFlags)
					?? throw new MissingMemberException(ET("无法找到指定的属性: {0}::{1}", type.FullName, propertyName));

			var method = property.GetSetMethod(true)
				?? throw new MissingMemberException(ET("无法找到指定属性的访问器: {0}::{1}", type.FullName, propertyName));

			return method;
		}

		/// <summary>
		/// 在给定类上提取索引器的读取方法
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="MissingMemberException"></exception>
		/// <exception cref="AmbiguousMatchException"></exception>
		public static MethodInfo ExtractIndexerGetter(this Type type, params Type[] types)
		{
			var indexers = TypeIndexers.GetIndexers(type);
			var getter = indexers.GetUniqueGetter();

			if (getter is not null)
				return getter;

			try
			{
				getter = indexers.GetGetter(types);

				if (getter is null)
					throw new MissingMemberException(ET("在给定的类上无法找到最合适的索引器: {0}", type.FullName));

				return getter;
			}
			catch (AmbiguousMatchException)
			{
				throw new AmbiguousMatchException(ET("在给定的类上有多个合适的索引器: {0}", type.FullName));
			}
		}

		/// <summary>
		/// 在给定类上提取索引器的写入方法
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		/// <exception cref="MissingMemberException"></exception>
		/// <exception cref="AmbiguousMatchException"></exception>
		public static MethodInfo ExtractIndexerSetter(this Type type, params Type[] types)
		{
			var indexers = TypeIndexers.GetIndexers(type);
			var setter = indexers.GetUniqueSetter();

			if (setter is not null)
				return setter;

			try
			{
				setter = indexers.GetSetter(types);

				if (setter is null)
					throw new MissingMemberException(ET("在给定的类上无法找到最合适的索引器: {0}", type.FullName));

				return setter;
			}
			catch (AmbiguousMatchException)
			{
				throw new AmbiguousMatchException(ET("在给定的类上有多个合适的索引器: {0}", type.FullName));
			}
		}

		/// <summary>
		/// 在给定类上提取指定的构造函数
		/// </summary>
		/// <param name="type"></param>
		/// <param name="types"></param>
		/// <returns></returns>
		public static ConstructorInfo ExtractCtor(this Type type, params Type[] types)
		{
			var ctors = type.GetConstructors(DefaultFlags);

			if (ctors.Length == 0)
				throw new MissingMemberException(ET("给定类型没有构造函数: {0}", type.FullName));
			if (ctors.Length == 1)
				return ctors[0];

			return type.GetConstructor(types)
				?? throw new MissingMemberException(ET("在给定类型上没有找到指定的构造函数: {0}", type.FullName));
		}

		private sealed class TypeIndexers
		{
			private static readonly Dictionary<Type, TypeIndexers> m_Indexers = [];
			private static readonly ReadWriteLock m_Lock = new();

			private readonly MethodInfo[] m_Getters;
			private readonly MethodInfo[] m_Setters;

			private TypeIndexers(Type type)
			{
				var properties = type.GetProperties(DefaultFlags)
					.Where(p => p.GetIndexParameters().Length > 0)
					.ToArray();

				var getters = new List<MethodInfo>();
				var setters = new List<MethodInfo>();

				foreach (var property in properties)
				{
					var getter = property.GetGetMethod(true);
					var setter = property.GetSetMethod(true);

					if (getter is not null)
						getters.Add(getter);
					if (setter is not null)
						setters.Add(setter);
				}

				m_Getters = [.. getters];
				m_Setters = [.. setters];
			}

			public MethodInfo? GetUniqueGetter()
			{
				if (m_Getters.Length == 1)
					return m_Getters[0];

				return null;
			}

			public MethodInfo? GetUniqueSetter()
			{
				if (m_Setters.Length == 1)
					return m_Setters[0];

				return null;
			}

			public MethodInfo? GetGetter(params Type[] types)
			{
				return (MethodInfo?)m_Getters.FindBestMatch(types, m_ConvertOptions);
			}

			public MethodInfo? GetSetter(params Type[] types)
			{
				return (MethodInfo?)m_Setters.FindBestMatch(types, m_ConvertOptions);
			}

			public static TypeIndexers GetIndexers(Type type)
			{
				using (m_Lock.Read())
				{
					if (m_Indexers.TryGetValue(type, out var indexers))
						return indexers;
				}

				using (m_Lock.UpgradeableRead())
				{
					if (m_Indexers.TryGetValue(type, out var indexers))
						return indexers;

					using (m_Lock.Write())
					{
						indexers = new TypeIndexers(type);
						m_Indexers.Add(type, indexers);
					}

					return indexers;
				}
			}
		}
	}
}
