using Lumi7Common.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Types
{
	/// <summary>
	/// 提供Luson的Nil类型
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public sealed class Nil
	{
		private static readonly InstanceProvider<Nil> m_Instance = new(() => new Nil());

		private Nil()
		{
			if (!m_Instance.IsCreatingByCurrent)
				throw new InvalidOperationException(ET("Nil的实例对象只能有一个"));
		}

		public static Nil Value => m_Instance.Instance;

		public static bool IsNil([NotNullWhen(false)] object? obj) => Value.Equals(obj);

		public override bool Equals(object? obj) => obj is null || ReferenceEquals(obj, this);

		public override int GetHashCode() => 0;

		public override string ToString() => "nil";

		public static bool operator ==(Nil _, object? value) => Value.Equals(value);

		public static bool operator !=(Nil _, object? value) => !Value.Equals(value);

		public static bool operator ==(object? value, Nil _) => Value.Equals(value);

		public static bool operator !=(object? value, Nil _) => !Value.Equals(value);
	}
}
