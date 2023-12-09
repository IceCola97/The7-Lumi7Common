using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection
{
	[Obsolete("Nullable类型在装箱的时候会自动解除Nullable的包裹")]
	public static class NullableReflection
	{
		[ThreadStatic]
		private static Dictionary<Type, Extractor>? m_Extractors;

		private static Extractor GetExtractor(Type type)
		{
			m_Extractors ??= [];

			if (m_Extractors.TryGetValue(type, out var extractor))
				return extractor;

			var extractorType = typeof(NullableExtractor<>).MakeGenericType(type);
			var extractorMethod = extractorType.GetMethod("Extract")
				?? throw new ImpossibleException();

			var returnLabel = Expression.Label();
			var parameter = Expression.Parameter(typeof(object));
			var converted = Expression.Call(null, extractorMethod, parameter);
			var statement = Expression.Return(returnLabel, converted);
			var body = Expression.Block(converted, Expression.Label(returnLabel));

			if (body.CanReduce)
				body.Reduce();

			extractor = Expression.Lambda<Extractor>(body, parameter).Compile();
			m_Extractors[type] = extractor;
			return extractor;
		}

		[return: NotNullIfNotNull(nameof(nullable))]
		public static object? GetValue(object? nullable)
		{
			if (nullable is null)
				return null;

			var type = nullable.GetType();

			if (type.IsValueType && type.IsGenericType && !type.IsGenericTypeDefinition
				&& type.GetGenericTypeDefinition() == typeof(Nullable<>))
				return GetExtractor(type).Invoke(nullable);

			throw new NotSupportedException(ET("给定的对象不是一个Nullable"));
		}

		private static class NullableExtractor<T> where T : struct
		{
			public static object Extract(object nullable)
			{
				return ((T?)nullable).Value;
			}
		}

		private delegate object Extractor(object nullable);
	}
}
