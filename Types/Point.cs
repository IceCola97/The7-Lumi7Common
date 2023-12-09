using Lumi7Common.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Types
{
	/// <summary>
	/// 提供Luson的Point类型
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public readonly struct Point : IEquatable<Point>,
		IAdditionOperators<Point, Point, Point>,
		ISubtractionOperators<Point, Point, Point>,
		IMultiplyOperators<Point, double, Point>,
		IEqualityOperators<Point, Point, bool>
	{
		[FieldOffset(0)]
		private readonly double m_X;
		[FieldOffset(sizeof(double))]
		private readonly double m_Y;

		public Point(double x, double y)
		{
			m_X = x;
			m_Y = y;
		}

		public Point(Location location)
		{
			m_X = location.X;
			m_Y = location.Y;
		}

		public bool IsFinite => double.IsFinite(m_X) && double.IsFinite(m_Y);

		public double X => m_X;

		public double Y => m_Y;

		public static bool TryParse(string text, out Point result)
		{
			text = text.Trim();
			result = default;

			int open = text.IndexOf('(');

			if (open >= 0)
			{
				if (!text.EndsWith(')'))
					return false;

				text = text[(open + 1)..^1];
			}

			var (sx, sy) = text.Split2(',');

			if (sx is null || sy is null)
				return false;

			if (!double.TryParse(sx.Trim(), out var x)
				|| !double.TryParse(sy.Trim(), out var y))
				return false;

			result = new Point(x, y);
			return true;
		}

		public override bool Equals(object? obj) => obj is Point point && Equals(point);

		public bool Equals(Point other) => m_X == other.m_X && m_Y == other.m_Y;

		public override int GetHashCode() => HashCode.Combine(m_X, m_Y);

		public override string ToString() => $"Point({m_X}, {m_Y})";

		public static bool operator ==(Point left, Point right) => left.Equals(right);

		public static bool operator !=(Point left, Point right) => !(left == right);

		public static Point operator +(Point left, Point right)
			=> new(left.m_X + right.m_X, left.m_Y + right.m_Y);

		public static Point operator checked +(Point left, Point right)
			=> new(checked(left.m_X + right.m_X), checked(left.m_Y + right.m_Y));

		public static Point operator -(Point left, Point right)
			=> new(left.m_X - right.m_X, left.m_Y - right.m_Y);

		public static Point operator checked -(Point left, Point right)
			=> new(checked(left.m_X - right.m_X), checked(left.m_Y - right.m_Y));

		public static Point operator *(Point left, double right)
			=> new(left.m_X * right, left.m_Y * right);

		public static Point operator checked *(Point left, double right)
			=> new(checked(left.m_X * right), checked(left.m_Y * right));
	}
}
