using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection
{
	/// <summary>
	/// 提供运算符方法的获取
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class OperatorReflection
	{
		/// <summary>
		/// 获取指定的运算符方法
		/// </summary>
		/// <param name="type"></param>
		/// <param name="operator"></param>
		/// <param name="operand"></param>
		/// <returns></returns>
		public static MethodInfo? GetOperator(this Type type,
			SharpOperator @operator, Type operand)
		{
			var method = type.GetMethod($"op_{@operator}",
				BindingFlags.Public | BindingFlags.Static, [operand]);

			if (method is null || !method.IsSpecialName)
				return null;

			return method;
		}

		/// <summary>
		/// 获取指定的运算符方法
		/// </summary>
		/// <param name="type"></param>
		/// <param name="operator"></param>
		/// <param name="left"></param>
		/// <param name="right"></param>
		/// <returns></returns>
		public static MethodInfo? GetOperator(this Type type,
			SharpOperator @operator, Type left, Type right)
		{
			var method = type.GetMethod($"op_{@operator}",
				BindingFlags.Public | BindingFlags.Static, [left, right]);

			if (method is null || !method.IsSpecialName)
				return null;

			return method;
		}

		/// <summary>
		/// 判断指定的运算符方法是否存在
		/// </summary>
		/// <param name="type"></param>
		/// <param name="operator"></param>
		/// <param name="operand"></param>
		/// <param name="returnType"></param>
		/// <returns></returns>
		public static bool HasOperator(this Type type, SharpOperator @operator,
			Type operand, Type returnType)
		{
			var method = GetOperator(type, @operator, operand);

			if (method is null)
				return false;

			return method.ReturnType == returnType;
		}

		/// <summary>
		/// 判断指定的运算符方法是否存在
		/// </summary>
		/// <param name="type"></param>
		/// <param name="operator"></param>
		/// <param name="operand"></param>
		/// <param name="returnType"></param>
		/// <returns></returns>
		public static bool HasOperator(this Type type, SharpOperator @operator,
			Type left, Type right, Type returnType)
		{
			var method = GetOperator(type, @operator, left, right);

			if (method is null)
				return false;

			return method.ReturnType == returnType;
		}

		/// <summary>
		/// 获取指定的运算符方法
		/// </summary>
		/// <typeparam name="TOperand"></typeparam>
		/// <param name="type"></param>
		/// <param name="operator"></param>
		/// <returns></returns>
		public static MethodInfo? GetOperator<TOperand>(this Type type, SharpOperator @operator)
			=> GetOperator(type, @operator, typeof(TOperand));

		/// <summary>
		/// 获取指定的运算符方法
		/// </summary>
		/// <typeparam name="TOperand"></typeparam>
		/// <param name="type"></param>
		/// <param name="operator"></param>
		/// <returns></returns>
		public static MethodInfo? GetOperator<TLeft, TRight>(this Type type, SharpOperator @operator)
			=> GetOperator(type, @operator, typeof(TLeft), typeof(TRight));

		/// <summary>
		/// 判断指定的运算符方法是否存在
		/// </summary>
		/// <typeparam name="TOperand"></typeparam>
		/// <param name="type"></param>
		/// <param name="operator"></param>
		/// <returns></returns>
		public static bool HasOperator<TOperand, TReturn>(this Type type, SharpOperator @operator)
			=> HasOperator(type, @operator, typeof(TOperand), typeof(TReturn));

		/// <summary>
		/// 判断指定的运算符方法是否存在
		/// </summary>
		/// <typeparam name="TOperand"></typeparam>
		/// <param name="type"></param>
		/// <param name="operator"></param>
		/// <returns></returns>
		public static bool HasOperator<TLeft, TRight, TReturn>(this Type type, SharpOperator @operator)
			=> HasOperator(type, @operator, typeof(TLeft), typeof(TRight), typeof(TReturn));
	}

	/// <summary>
	/// C#运算符枚举
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public enum SharpOperator
	{
		Addition,
		CheckedAddition,
		Subtraction,
		CheckedSubtraction,
		Multiply,
		CheckedMultiply,
		Division,
		CheckedDivision,
		Modulus,
		ExclusiveOr,
		BitwiseAnd,
		BitwiseOr,
		LeftShift,
		UnsignedRightShift,
		RightShift,
		Equality,
		Inequality,
		LessThan,
		GreaterThan,
		LessThanOrEqual,
		GreaterThanOrEqual,
		UnaryPlus,
		UnaryNegation,
		LogicalNot,
		OnesComplement,
		Increment,
		CheckedIncrement,
		Decrement,
		CheckedDecrement,
		True,
		False,
	}
}
