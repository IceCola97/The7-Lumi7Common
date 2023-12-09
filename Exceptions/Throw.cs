using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Exceptions
{
	/// <summary>
	/// 提供一些异常相关的便捷操作
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class Throw
	{
		#region Functional Try

		[DebuggerHidden]
		public static Exception Rethrow(this Exception exception)
		{
			ExceptionDispatchInfo.Capture(exception).Throw();
			throw exception;
		}

		[DebuggerHidden]
		public static ExceptionHandler? Try(TryBlock tryBlock)
		{
			try
			{
				tryBlock.Invoke();
				return null;
			}
			catch (Exception ex)
			{
				return new ExceptionHandler(ex);
			}
		}

		[DebuggerHidden]
		public static ExceptionHandler? Try<TParameter>(TryBlock<TParameter> tryBlock, TParameter parameter)
		{
			try
			{
				tryBlock.Invoke(parameter);
				return null;
			}
			catch (Exception ex)
			{
				return new ExceptionHandler(ex);
			}
		}

		[DebuggerHidden]
		public static ExceptionHandler? TryIf(TryBlock tryBlock, bool useTry)
		{
			if (!useTry)
			{
				tryBlock.Invoke();
				return null;
			}

			try
			{
				tryBlock.Invoke();
				return null;
			}
			catch (Exception ex)
			{
				return new ExceptionHandler(ex);
			}
		}

		[DebuggerHidden]
		public static ExceptionHandler? TryIf<TParameter>(TryBlock<TParameter> tryBlock, TParameter parameter, bool useTry)
		{
			if (!useTry)
			{
				tryBlock.Invoke(parameter);
				return null;
			}

			try
			{
				tryBlock.Invoke(parameter);
				return null;
			}
			catch (Exception ex)
			{
				return new ExceptionHandler(ex);
			}
		}

		#endregion Functional Try

		#region NotImplementedCausedBy

		[DoesNotReturn]
		[DebuggerHidden]
		public static NotImplementedException NotImplementedCausedBy(string typeName)
		{
			throw new NotImplementedException(ET("依赖类 '{0}' 的实现", typeName));
		}

		[DoesNotReturn]
		[DebuggerHidden]
		public static NotImplementedException NotImplementedCausedBy(string typeName, string methodName)
		{
			throw new NotImplementedException(ET("依赖方法 '{0}::{1}' 的实现", typeName, methodName));
		}

		[DoesNotReturn]
		[DebuggerHidden]
		public static NotImplementedException NotImplementedCausedBy<T>()
		{
			throw new NotImplementedException(ET("依赖类 '{0}' 的实现", typeof(T).FullName));
		}

		[DoesNotReturn]
		[DebuggerHidden]
		public static NotImplementedException NotImplementedCausedBy<T>(string methodName)
		{
			throw new NotImplementedException(ET("依赖类 '{0}::{1}' 的实现", typeof(T).FullName, methodName));
		}

		[DoesNotReturn]
		[DebuggerHidden]
		public static NotImplementedException NotImplementedCausedBy(Type type)
		{
			throw new NotImplementedException(ET("依赖类 '{0}' 的实现", type.FullName));
		}

		[DoesNotReturn]
		[DebuggerHidden]
		public static NotImplementedException NotImplementedCausedBy(Type type, string methodName)
		{
			throw new NotImplementedException(ET("依赖类 '{0}::{1}' 的实现", type.FullName, methodName));
		}

		#endregion NotImplementedCausedBy

		#region ArgumentOutOfRange

		[DoesNotReturn]
		[DebuggerHidden]
		public static ArgumentOutOfRangeException ArgumentOutOfRange(int value, Array array, [CallerArgumentExpression(nameof(value))] string? expression = null)
		{
			throw new ArgumentOutOfRangeException(expression ?? nameof(value), value,
				ET("索引 '{0}' 的值超出数组边界: [ Index = {1}, Size = {2} ]", expression ?? nameof(value), value, array.LongLength));
		}

		[DoesNotReturn]
		[DebuggerHidden]
		public static ArgumentOutOfRangeException ArgumentOutOfRange(long value, Array array, [CallerArgumentExpression(nameof(value))] string? expression = null)
		{
			throw new ArgumentOutOfRangeException(expression ?? nameof(value), value,
				ET("索引 '{0}' 的值超出数组边界: [ Index = {1}, Size = {2} ]", expression ?? nameof(value), value, array.LongLength));
		}

		[DoesNotReturn]
		[DebuggerHidden]
		public static ArgumentOutOfRangeException ArgumentOutOfRange<T>(T value, T size, [CallerArgumentExpression(nameof(value))] string? expression = null)
		{
			throw new ArgumentOutOfRangeException(expression ?? nameof(value), value,
				ET("索引 '{0}' 的值超出边界: [ Index = {1}, Size = {2} ]", expression ?? nameof(value), value, size));
		}

		[DoesNotReturn]
		[DebuggerHidden]
		public static ArgumentOutOfRangeException ArgumentOutOfRange<T>(T value, T min, T max, [CallerArgumentExpression(nameof(value))] string? expression = null)
		{
			throw new ArgumentOutOfRangeException(expression ?? nameof(value), value,
				ET("参数 '{0}' 的值超出范围: [ Value = {1}, Min = {2}, Max = {3} ]", expression ?? nameof(value), value, min, max));
		}

		[DoesNotReturn]
		[DebuggerHidden]
		public static ArgumentOutOfRangeException ArgumentOutOfRangeLess<T>(T value, T min, [CallerArgumentExpression(nameof(value))] string? expression = null)
		{
			throw new ArgumentOutOfRangeException(expression ?? nameof(value), value,
				ET("参数 '{0}' 的值超出范围: [ Value = {1}, Min = {2} ]", expression ?? nameof(value), value, min));
		}

		[DoesNotReturn]
		[DebuggerHidden]
		public static ArgumentOutOfRangeException ArgumentOutOfRangeGreater<T>(T value, T max, [CallerArgumentExpression(nameof(value))] string? expression = null)
		{
			throw new ArgumentOutOfRangeException(expression ?? nameof(value), value,
				ET("参数 '{0}' 的值超出范围: [ Value = {1}, Max = {2} ]", expression ?? nameof(value), value, max));
		}

		#endregion ArgumentOutOfRange

		#region Impossible

		[DoesNotReturn]
		[DebuggerHidden]
		public static ImpossibleException Impossible()
			=> throw new ImpossibleException();

		[DoesNotReturn]
		[DebuggerHidden]
		public static ImpossibleException Impossible(string unless)
			=> throw new ImpossibleException(unless);

		#endregion
	}
}
