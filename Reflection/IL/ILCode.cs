using System.Reflection.Emit;
using System.Runtime.InteropServices;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 包含具体数据的IL字节码信息
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[StructLayout(LayoutKind.Explicit)]
	public struct ILCode
	{
		/// <summary>
		/// IL字节码原型
		/// </summary>
		[FieldOffset(24)]
		public OpCode OpCode;

		/// <summary>
		/// 当前IL字节码所在方法体中的偏移量
		/// </summary>
		[FieldOffset(0)]
		public int Offset;

		/// <summary>
		/// <see cref="byte"/>类型的操作数
		/// </summary>
		[FieldOffset(8)]
		public byte OperandU8;

		/// <summary>
		/// <see cref="sbyte"/>类型的操作数
		/// </summary>
		[FieldOffset(8)]
		public sbyte OperandS8;

		/// <summary>
		/// <see cref="short"/>类型的操作数
		/// </summary>
		[FieldOffset(8)]
		public short Operand16;

		/// <summary>
		/// <see cref="int"/>类型的操作数
		/// </summary>
		[FieldOffset(8)]
		public int Operand32;

		/// <summary>
		/// <see cref="long"/>类型的操作数
		/// </summary>
		[FieldOffset(8)]
		public long Operand64;

		/// <summary>
		/// <see cref="float"/>类型的操作数
		/// </summary>
		[FieldOffset(8)]
		public float OperandF32;

		/// <summary>
		/// <see cref="double"/>类型的操作数
		/// </summary>
		[FieldOffset(8)]
		public double OperandF64;

		/// <summary>
		/// 当IL字节码是<see cref="OpCodes.Switch"/>时的所有标签在方法体中的偏移量
		/// </summary>
		[FieldOffset(16)]
		public int[] SwitchLabels;
	}
}
