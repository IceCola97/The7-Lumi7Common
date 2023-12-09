using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Types
{
	/// <summary>
	/// 提供Luson字段类型的相关转换
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class FieldConvert
	{
		/// <summary>
		/// 将给定类型转换为字符串
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		public static string ToString(object? value)
		{
			if (Nil.IsNil(value))
				return Nil.Value.ToString();

			var fieldType = IStateFieldStandard.Instance.GetFieldType(value.GetType());

			return fieldType switch
			{
				FieldType.Dual => value.ToString()!.ToLower(),
				FieldType.Integral
				or FieldType.Real
				or FieldType.Point
				or FieldType.Location
				or FieldType.Text
				or FieldType.Tuple
				or FieldType.List
				or FieldType.Map => value.ToString()!,
				FieldType.Raw => $"[RawObject@{value.GetType().Name}]",
				_ => throw new NotSupportedException(ET("不支持的类型")),
			};
		}
	}
}
