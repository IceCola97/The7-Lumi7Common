using Lumi7Common.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供最基本的IL字节码反编译能力
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadUnsafe]
	public sealed class ILDecompiler : IEnumerator<ILCode>, IEnumerator
	{
		private readonly MemoryStream m_ILStream;
		private readonly BinaryReader m_ILReader;
		private ILCode m_ILCode = default;

		static ILDecompiler() => DynamicSupport.AssertDynamicSupport();

		public ILDecompiler(byte[] ilCodeData)
		{
			ArgumentNullException.ThrowIfNull(ilCodeData);

			m_ILStream = new MemoryStream(ilCodeData);
			m_ILReader = new BinaryReader(m_ILStream);
		}

		public ILDecompiler(MethodBody methodBody)
			: this(methodBody.GetILAsByteArray()
				  ?? throw new ArgumentException(ET("给定的方法体没有IL字节码数据")))
		{
		}

		public ILDecompiler(MethodBase methodBase)
			: this(methodBase.GetMethodBody()
				  ?? throw new ArgumentException(ET("给定的方法没有IL方法体")))
		{
		}

		/// <summary>
		/// 获取当前反编译的IL字节码
		/// </summary>
		public ILCode Current => m_ILCode;

		object IEnumerator.Current => Current;

		/// <summary>
		/// 释放内存占用
		/// </summary>
		public void Dispose() => m_ILStream.Dispose();

		/// <summary>
		/// 反编译下一个IL字节码
		/// </summary>
		/// <returns></returns>
		public bool MoveNext()
		{
			if (m_ILStream.Position == m_ILStream.Length)
				return false;

			try
			{
				m_ILCode = ReadILCode();
				return true;
			}
			catch (EndOfStreamException)
			{
				return false;
			}
		}

		/// <summary>
		/// 重置当前反编译的位置
		/// </summary>
		public void Reset()
		{
			m_ILStream.Position = 0;
			m_ILCode = default;
		}

		private ILCode ReadILCode()
		{
			OpCode opCode = ReadOpCode();
			ILCode code = default;

			code.OpCode = opCode;

			try
			{
				switch (opCode.OperandType)
				{
					case OperandType.InlineNone:
						break;
					case OperandType.ShortInlineI:
						code.OperandU8 = m_ILReader.ReadByte();
						break;
					case OperandType.InlineI:
						code.Operand32 = m_ILReader.ReadInt32();
						break;
					case OperandType.InlineI8:
						code.Operand64 = m_ILReader.ReadInt64();
						break;
					case OperandType.ShortInlineR:
						code.OperandF32 = m_ILReader.ReadSingle();
						break;
					case OperandType.InlineR:
						code.OperandF64 = m_ILReader.ReadDouble();
						break;
					case OperandType.ShortInlineVar:
						code.OperandU8 = m_ILReader.ReadByte();
						break;
					case OperandType.InlineVar:
						code.Operand16 = m_ILReader.ReadInt16();
						break;
					case OperandType.ShortInlineBrTarget:
						code.OperandS8 = m_ILReader.ReadSByte();
						break;
					case OperandType.InlineBrTarget:
						code.Operand32 = m_ILReader.ReadInt32();
						break;
					case OperandType.InlineString:
					case OperandType.InlineType:
					case OperandType.InlineField:
					case OperandType.InlineMethod:
					case OperandType.InlineSig:
					case OperandType.InlineTok:
						code.Operand32 = m_ILReader.ReadInt32();
						break;
					case OperandType.InlineSwitch:
						{
							code.Operand32 = m_ILReader.ReadInt32();

							int[] labels = new int[code.Operand32];

							for (int i = 0; i < labels.Length; i++)
							{
								labels[i] = m_ILReader.ReadInt32();
							}

							code.SwitchLabels = labels;
						}
						break;
				}
			}
			catch (EndOfStreamException)
			{
				throw new InvalidProgramException(ET("未完成的IL码"));
			}

			return code;
		}

		private OpCode ReadOpCode()
		{
			int value = m_ILStream.ReadByte();
			if (value >= 0xF8)
				value = value << 8 | m_ILStream.ReadByte();

			if (TryResolveOpCode(value, out OpCode opCode))
				return opCode;

			int offset = (int)m_ILStream.Position;
			throw new InvalidProgramException(ET("无效的IL字节码 ({0:X04}) 在位置 IL_{1:X04}",
				value, value >= 0xF800 ? offset - 2 : offset - 1));
		}

		/// <summary>
		/// 通过给定的字节码获取IL字节码原型
		/// </summary>
		/// <param name="code"></param>
		/// <param name="opCode"></param>
		/// <returns></returns>
		public static bool TryResolveOpCode(int code, out OpCode opCode)
		{
			opCode = OpCodes.Nop;

			switch (code)
			{
				case 0x0000:
					opCode = OpCodes.Nop;
					break;
				case 0x0001:
					opCode = OpCodes.Break;
					break;
				case 0x0002:
					opCode = OpCodes.Ldarg_0;
					break;
				case 0x0003:
					opCode = OpCodes.Ldarg_1;
					break;
				case 0x0004:
					opCode = OpCodes.Ldarg_2;
					break;
				case 0x0005:
					opCode = OpCodes.Ldarg_3;
					break;
				case 0x0006:
					opCode = OpCodes.Ldloc_0;
					break;
				case 0x0007:
					opCode = OpCodes.Ldloc_1;
					break;
				case 0x0008:
					opCode = OpCodes.Ldloc_2;
					break;
				case 0x0009:
					opCode = OpCodes.Ldloc_3;
					break;
				case 0x000A:
					opCode = OpCodes.Stloc_0;
					break;
				case 0x000B:
					opCode = OpCodes.Stloc_1;
					break;
				case 0x000C:
					opCode = OpCodes.Stloc_2;
					break;
				case 0x000D:
					opCode = OpCodes.Stloc_3;
					break;
				case 0x000E:
					opCode = OpCodes.Ldarg_S;
					break;
				case 0x000F:
					opCode = OpCodes.Ldarga_S;
					break;
				case 0x0010:
					opCode = OpCodes.Starg_S;
					break;
				case 0x0011:
					opCode = OpCodes.Ldloc_S;
					break;
				case 0x0012:
					opCode = OpCodes.Ldloca_S;
					break;
				case 0x0013:
					opCode = OpCodes.Stloc_S;
					break;
				case 0x0014:
					opCode = OpCodes.Ldnull;
					break;
				case 0x0015:
					opCode = OpCodes.Ldc_I4_M1;
					break;
				case 0x0016:
					opCode = OpCodes.Ldc_I4_0;
					break;
				case 0x0017:
					opCode = OpCodes.Ldc_I4_1;
					break;
				case 0x0018:
					opCode = OpCodes.Ldc_I4_2;
					break;
				case 0x0019:
					opCode = OpCodes.Ldc_I4_3;
					break;
				case 0x001A:
					opCode = OpCodes.Ldc_I4_4;
					break;
				case 0x001B:
					opCode = OpCodes.Ldc_I4_5;
					break;
				case 0x001C:
					opCode = OpCodes.Ldc_I4_6;
					break;
				case 0x001D:
					opCode = OpCodes.Ldc_I4_7;
					break;
				case 0x001E:
					opCode = OpCodes.Ldc_I4_8;
					break;
				case 0x001F:
					opCode = OpCodes.Ldc_I4_S;
					break;
				case 0x0020:
					opCode = OpCodes.Ldc_I4;
					break;
				case 0x0021:
					opCode = OpCodes.Ldc_I8;
					break;
				case 0x0022:
					opCode = OpCodes.Ldc_R4;
					break;
				case 0x0023:
					opCode = OpCodes.Ldc_R8;
					break;
				case 0x0025:
					opCode = OpCodes.Dup;
					break;
				case 0x0026:
					opCode = OpCodes.Pop;
					break;
				case 0x0027:
					opCode = OpCodes.Jmp;
					break;
				case 0x0028:
					opCode = OpCodes.Call;
					break;
				case 0x0029:
					opCode = OpCodes.Calli;
					break;
				case 0x002A:
					opCode = OpCodes.Ret;
					break;
				case 0x002B:
					opCode = OpCodes.Br_S;
					break;
				case 0x002C:
					opCode = OpCodes.Brfalse_S;
					break;
				case 0x002D:
					opCode = OpCodes.Brtrue_S;
					break;
				case 0x002E:
					opCode = OpCodes.Beq_S;
					break;
				case 0x002F:
					opCode = OpCodes.Bge_S;
					break;
				case 0x0030:
					opCode = OpCodes.Bgt_S;
					break;
				case 0x0031:
					opCode = OpCodes.Ble_S;
					break;
				case 0x0032:
					opCode = OpCodes.Blt_S;
					break;
				case 0x0033:
					opCode = OpCodes.Bne_Un_S;
					break;
				case 0x0034:
					opCode = OpCodes.Bge_Un_S;
					break;
				case 0x0035:
					opCode = OpCodes.Bgt_Un_S;
					break;
				case 0x0036:
					opCode = OpCodes.Ble_Un_S;
					break;
				case 0x0037:
					opCode = OpCodes.Blt_Un_S;
					break;
				case 0x0038:
					opCode = OpCodes.Br;
					break;
				case 0x0039:
					opCode = OpCodes.Brfalse;
					break;
				case 0x003A:
					opCode = OpCodes.Brtrue;
					break;
				case 0x003B:
					opCode = OpCodes.Beq;
					break;
				case 0x003C:
					opCode = OpCodes.Bge;
					break;
				case 0x003D:
					opCode = OpCodes.Bgt;
					break;
				case 0x003E:
					opCode = OpCodes.Ble;
					break;
				case 0x003F:
					opCode = OpCodes.Blt;
					break;
				case 0x0040:
					opCode = OpCodes.Bne_Un;
					break;
				case 0x0041:
					opCode = OpCodes.Bge_Un;
					break;
				case 0x0042:
					opCode = OpCodes.Bgt_Un;
					break;
				case 0x0043:
					opCode = OpCodes.Ble_Un;
					break;
				case 0x0044:
					opCode = OpCodes.Blt_Un;
					break;
				case 0x0045:
					opCode = OpCodes.Switch;
					break;
				case 0x0046:
					opCode = OpCodes.Ldind_I1;
					break;
				case 0x0047:
					opCode = OpCodes.Ldind_U1;
					break;
				case 0x0048:
					opCode = OpCodes.Ldind_I2;
					break;
				case 0x0049:
					opCode = OpCodes.Ldind_U2;
					break;
				case 0x004A:
					opCode = OpCodes.Ldind_I4;
					break;
				case 0x004B:
					opCode = OpCodes.Ldind_U4;
					break;
				case 0x004C:
					opCode = OpCodes.Ldind_I8;
					break;
				case 0x004D:
					opCode = OpCodes.Ldind_I;
					break;
				case 0x004E:
					opCode = OpCodes.Ldind_R4;
					break;
				case 0x004F:
					opCode = OpCodes.Ldind_R8;
					break;
				case 0x0050:
					opCode = OpCodes.Ldind_Ref;
					break;
				case 0x0051:
					opCode = OpCodes.Stind_Ref;
					break;
				case 0x0052:
					opCode = OpCodes.Stind_I1;
					break;
				case 0x0053:
					opCode = OpCodes.Stind_I2;
					break;
				case 0x0054:
					opCode = OpCodes.Stind_I4;
					break;
				case 0x0055:
					opCode = OpCodes.Stind_I8;
					break;
				case 0x0056:
					opCode = OpCodes.Stind_R4;
					break;
				case 0x0057:
					opCode = OpCodes.Stind_R8;
					break;
				case 0x0058:
					opCode = OpCodes.Add;
					break;
				case 0x0059:
					opCode = OpCodes.Sub;
					break;
				case 0x005A:
					opCode = OpCodes.Mul;
					break;
				case 0x005B:
					opCode = OpCodes.Div;
					break;
				case 0x005C:
					opCode = OpCodes.Div_Un;
					break;
				case 0x005D:
					opCode = OpCodes.Rem;
					break;
				case 0x005E:
					opCode = OpCodes.Rem_Un;
					break;
				case 0x005F:
					opCode = OpCodes.And;
					break;
				case 0x0060:
					opCode = OpCodes.Or;
					break;
				case 0x0061:
					opCode = OpCodes.Xor;
					break;
				case 0x0062:
					opCode = OpCodes.Shl;
					break;
				case 0x0063:
					opCode = OpCodes.Shr;
					break;
				case 0x0064:
					opCode = OpCodes.Shr_Un;
					break;
				case 0x0065:
					opCode = OpCodes.Neg;
					break;
				case 0x0066:
					opCode = OpCodes.Not;
					break;
				case 0x0067:
					opCode = OpCodes.Conv_I1;
					break;
				case 0x0068:
					opCode = OpCodes.Conv_I2;
					break;
				case 0x0069:
					opCode = OpCodes.Conv_I4;
					break;
				case 0x006A:
					opCode = OpCodes.Conv_I8;
					break;
				case 0x006B:
					opCode = OpCodes.Conv_R4;
					break;
				case 0x006C:
					opCode = OpCodes.Conv_R8;
					break;
				case 0x006D:
					opCode = OpCodes.Conv_U4;
					break;
				case 0x006E:
					opCode = OpCodes.Conv_U8;
					break;
				case 0x006F:
					opCode = OpCodes.Callvirt;
					break;
				case 0x0070:
					opCode = OpCodes.Cpobj;
					break;
				case 0x0071:
					opCode = OpCodes.Ldobj;
					break;
				case 0x0072:
					opCode = OpCodes.Ldstr;
					break;
				case 0x0073:
					opCode = OpCodes.Newobj;
					break;
				case 0x0074:
					opCode = OpCodes.Castclass;
					break;
				case 0x0075:
					opCode = OpCodes.Isinst;
					break;
				case 0x0076:
					opCode = OpCodes.Conv_R_Un;
					break;
				case 0x0079:
					opCode = OpCodes.Unbox;
					break;
				case 0x007A:
					opCode = OpCodes.Throw;
					break;
				case 0x007B:
					opCode = OpCodes.Ldfld;
					break;
				case 0x007C:
					opCode = OpCodes.Ldflda;
					break;
				case 0x007D:
					opCode = OpCodes.Stfld;
					break;
				case 0x007E:
					opCode = OpCodes.Ldsfld;
					break;
				case 0x007F:
					opCode = OpCodes.Ldsflda;
					break;
				case 0x0080:
					opCode = OpCodes.Stsfld;
					break;
				case 0x0081:
					opCode = OpCodes.Stobj;
					break;
				case 0x0082:
					opCode = OpCodes.Conv_Ovf_I1_Un;
					break;
				case 0x0083:
					opCode = OpCodes.Conv_Ovf_I2_Un;
					break;
				case 0x0084:
					opCode = OpCodes.Conv_Ovf_I4_Un;
					break;
				case 0x0085:
					opCode = OpCodes.Conv_Ovf_I8_Un;
					break;
				case 0x0086:
					opCode = OpCodes.Conv_Ovf_U1_Un;
					break;
				case 0x0087:
					opCode = OpCodes.Conv_Ovf_U2_Un;
					break;
				case 0x0088:
					opCode = OpCodes.Conv_Ovf_U4_Un;
					break;
				case 0x0089:
					opCode = OpCodes.Conv_Ovf_U8_Un;
					break;
				case 0x008A:
					opCode = OpCodes.Conv_Ovf_I_Un;
					break;
				case 0x008B:
					opCode = OpCodes.Conv_Ovf_U_Un;
					break;
				case 0x008C:
					opCode = OpCodes.Box;
					break;
				case 0x008D:
					opCode = OpCodes.Newarr;
					break;
				case 0x008E:
					opCode = OpCodes.Ldlen;
					break;
				case 0x008F:
					opCode = OpCodes.Ldelema;
					break;
				case 0x0090:
					opCode = OpCodes.Ldelem_I1;
					break;
				case 0x0091:
					opCode = OpCodes.Ldelem_U1;
					break;
				case 0x0092:
					opCode = OpCodes.Ldelem_I2;
					break;
				case 0x0093:
					opCode = OpCodes.Ldelem_U2;
					break;
				case 0x0094:
					opCode = OpCodes.Ldelem_I4;
					break;
				case 0x0095:
					opCode = OpCodes.Ldelem_U4;
					break;
				case 0x0096:
					opCode = OpCodes.Ldelem_I8;
					break;
				case 0x0097:
					opCode = OpCodes.Ldelem_I;
					break;
				case 0x0098:
					opCode = OpCodes.Ldelem_R4;
					break;
				case 0x0099:
					opCode = OpCodes.Ldelem_R8;
					break;
				case 0x009A:
					opCode = OpCodes.Ldelem_Ref;
					break;
				case 0x009B:
					opCode = OpCodes.Stelem_I;
					break;
				case 0x009C:
					opCode = OpCodes.Stelem_I1;
					break;
				case 0x009D:
					opCode = OpCodes.Stelem_I2;
					break;
				case 0x009E:
					opCode = OpCodes.Stelem_I4;
					break;
				case 0x009F:
					opCode = OpCodes.Stelem_I8;
					break;
				case 0x00A0:
					opCode = OpCodes.Stelem_R4;
					break;
				case 0x00A1:
					opCode = OpCodes.Stelem_R8;
					break;
				case 0x00A2:
					opCode = OpCodes.Stelem_Ref;
					break;
				case 0x00A3:
					opCode = OpCodes.Ldelem;
					break;
				case 0x00A4:
					opCode = OpCodes.Stelem;
					break;
				case 0x00A5:
					opCode = OpCodes.Unbox_Any;
					break;
				case 0x00B3:
					opCode = OpCodes.Conv_Ovf_I1;
					break;
				case 0x00B4:
					opCode = OpCodes.Conv_Ovf_U1;
					break;
				case 0x00B5:
					opCode = OpCodes.Conv_Ovf_I2;
					break;
				case 0x00B6:
					opCode = OpCodes.Conv_Ovf_U2;
					break;
				case 0x00B7:
					opCode = OpCodes.Conv_Ovf_I4;
					break;
				case 0x00B8:
					opCode = OpCodes.Conv_Ovf_U4;
					break;
				case 0x00B9:
					opCode = OpCodes.Conv_Ovf_I8;
					break;
				case 0x00BA:
					opCode = OpCodes.Conv_Ovf_U8;
					break;
				case 0x00C2:
					opCode = OpCodes.Refanyval;
					break;
				case 0x00C3:
					opCode = OpCodes.Ckfinite;
					break;
				case 0x00C6:
					opCode = OpCodes.Mkrefany;
					break;
				case 0x00D0:
					opCode = OpCodes.Ldtoken;
					break;
				case 0x00D1:
					opCode = OpCodes.Conv_U2;
					break;
				case 0x00D2:
					opCode = OpCodes.Conv_U1;
					break;
				case 0x00D3:
					opCode = OpCodes.Conv_I;
					break;
				case 0x00D4:
					opCode = OpCodes.Conv_Ovf_I;
					break;
				case 0x00D5:
					opCode = OpCodes.Conv_Ovf_U;
					break;
				case 0x00D6:
					opCode = OpCodes.Add_Ovf;
					break;
				case 0x00D7:
					opCode = OpCodes.Add_Ovf_Un;
					break;
				case 0x00D8:
					opCode = OpCodes.Mul_Ovf;
					break;
				case 0x00D9:
					opCode = OpCodes.Mul_Ovf_Un;
					break;
				case 0x00DA:
					opCode = OpCodes.Sub_Ovf;
					break;
				case 0x00DB:
					opCode = OpCodes.Sub_Ovf_Un;
					break;
				case 0x00DC:
					opCode = OpCodes.Endfinally;
					break;
				case 0x00DD:
					opCode = OpCodes.Leave;
					break;
				case 0x00DE:
					opCode = OpCodes.Leave_S;
					break;
				case 0x00DF:
					opCode = OpCodes.Stind_I;
					break;
				case 0x00E0:
					opCode = OpCodes.Conv_U;
					break;
				case 0x00F8:
					opCode = OpCodes.Prefix7;
					break;
				case 0x00F9:
					opCode = OpCodes.Prefix6;
					break;
				case 0x00FA:
					opCode = OpCodes.Prefix5;
					break;
				case 0x00FB:
					opCode = OpCodes.Prefix4;
					break;
				case 0x00FC:
					opCode = OpCodes.Prefix3;
					break;
				case 0x00FD:
					opCode = OpCodes.Prefix2;
					break;
				case 0x00FE:
					opCode = OpCodes.Prefix1;
					break;
				case 0x00FF:
					opCode = OpCodes.Prefixref;
					break;
				case 0xFE00:
					opCode = OpCodes.Arglist;
					break;
				case 0xFE01:
					opCode = OpCodes.Ceq;
					break;
				case 0xFE02:
					opCode = OpCodes.Cgt;
					break;
				case 0xFE03:
					opCode = OpCodes.Cgt_Un;
					break;
				case 0xFE04:
					opCode = OpCodes.Clt;
					break;
				case 0xFE05:
					opCode = OpCodes.Clt_Un;
					break;
				case 0xFE06:
					opCode = OpCodes.Ldftn;
					break;
				case 0xFE07:
					opCode = OpCodes.Ldvirtftn;
					break;
				case 0xFE09:
					opCode = OpCodes.Ldarg;
					break;
				case 0xFE0A:
					opCode = OpCodes.Ldarga;
					break;
				case 0xFE0B:
					opCode = OpCodes.Starg;
					break;
				case 0xFE0C:
					opCode = OpCodes.Ldloc;
					break;
				case 0xFE0D:
					opCode = OpCodes.Ldloca;
					break;
				case 0xFE0E:
					opCode = OpCodes.Stloc;
					break;
				case 0xFE0F:
					opCode = OpCodes.Localloc;
					break;
				case 0xFE11:
					opCode = OpCodes.Endfilter;
					break;
				case 0xFE12:
					opCode = OpCodes.Unaligned;
					break;
				case 0xFE13:
					opCode = OpCodes.Volatile;
					break;
				case 0xFE14:
					opCode = OpCodes.Tailcall;
					break;
				case 0xFE15:
					opCode = OpCodes.Initobj;
					break;
				case 0xFE16:
					opCode = OpCodes.Constrained;
					break;
				case 0xFE17:
					opCode = OpCodes.Cpblk;
					break;
				case 0xFE18:
					opCode = OpCodes.Initblk;
					break;
				case 0xFE1A:
					opCode = OpCodes.Rethrow;
					break;
				case 0xFE1C:
					opCode = OpCodes.Sizeof;
					break;
				case 0xFE1D:
					opCode = OpCodes.Refanytype;
					break;
				case 0xFE1E:
					opCode = OpCodes.Readonly;
					break;
				default:
					return false;
			}

			return true;
		}
	}
}
