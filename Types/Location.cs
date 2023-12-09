using Lumi7Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Types
{
	/// <summary>
	/// 提供Luson的Location类型
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public readonly struct Location : IEquatable<Location>,
		IAdditionOperators<Location, Point, Location>,
		ISubtractionOperators<Location, Point, Location>,
		IEqualityOperators<Location, Location, bool>
	{
		[FieldOffset(0)]
		private readonly long m_Id;
		[FieldOffset(sizeof(long))]
		private readonly Point m_Pt;

		public Location(long id, in Point pt)
		{
			m_Id = id;
			m_Pt = pt;
		}

		public Location(long id, double x, double y)
		{
			m_Id = id;
			m_Pt = new Point(x, y);
		}

		public long Id => m_Id;

		public double X => m_Pt.X;

		public double Y => m_Pt.Y;

		public Point Point => m_Pt;

		public bool IsFinite => m_Pt.IsFinite;

		public static bool TryParse(string text, out Location result)
		{
			text = text.Trim();
			result = default;

			int open = text.IndexOf('<');
			string sid;

			Point point;

			if (open >= 0)
			{
				int close = text.IndexOf('>', open);

				if (close < 0)
					return false;

				sid = text[(open + 1)..close];
				text = text[(close + 1)..];
			}
			else
			{
				open = text.IndexOf('(');

				if (open >= 0)
				{
					if (!text.EndsWith(')'))
						return false;

					text = text[(open + 1)..^1];
				}

				int mark = text.IndexOfAny([',', ';', '|', ':', '@', '#', '!']);

				sid = text[..mark];
				text = text[(mark + 1)..];
			}

			if (!long.TryParse(sid, out var id))
				return false;
			if (!Point.TryParse(text, out point))
				return false;

			result = new Location(id, point);
			return true;
		}

		public override bool Equals(object? obj) => obj is Location location && Equals(location);

		public bool Equals(Location other) => m_Id == other.m_Id && m_Pt.Equals(other.m_Pt);

		public override int GetHashCode() => HashCode.Combine(m_Id, m_Pt);

		public override string ToString() => $"Location<{m_Id}>({m_Pt.X}, {m_Pt.Y})";

		public static bool operator ==(Location left, Location right) => left.Equals(right);

		public static bool operator !=(Location left, Location right) => !(left == right);

		public static Location operator +(Location left, Point right)
			=> new(left.m_Id, left.m_Pt + right);

		public static Location operator checked +(Location left, Point right)
			=> new(left.m_Id, checked(left.m_Pt + right));

		public static Location operator -(Location left, Point right)
			=> new(left.m_Id, left.m_Pt - right);

		public static Location operator checked -(Location left, Point right)
			=> new(left.m_Id, checked(left.m_Pt - right));
	}
}
