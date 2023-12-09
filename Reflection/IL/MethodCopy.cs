using Lumi7Common.Threading;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using static System.Buffers.Binary.BinaryPrimitives;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供运行时复制方法体的手段
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public static class MethodCopy
	{
		/// <summary>
		/// 创建一个动态方法，并使用给定方法的方法体去填充<br/>
		/// 如果给定方法是实例方法，则<see langword="this"/>将作为第一个参数插入参数列表<br/>
		/// 请注意，此方法不支持复制动态方法
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		/// <exception cref="NotSupportedException"></exception>
		/// <exception cref="InvalidOperationException"></exception>
		public static DynamicMethod CopyMethod(this MethodBase method)
		{
			DynamicSupport.AssertDynamicSupport();

			if (method is DynamicMethod)
				throw new NotSupportedException(ET("不支持复制动态方法"));
			if (method.IsGenericMethodDefinition)
				throw new NotSupportedException(ET("不支持复制泛型方法定义"));
			if (method.CallingConvention.HasFlag(CallingConventions.VarArgs))
				throw new NotSupportedException(ET("不支持可变参数列表"));

			var body = method.GetMethodBody()
				?? throw new InvalidOperationException(ET("方法复制只支持IL方法体"));

			var declaring = method.DeclaringType;
			var returnType = method is MethodInfo methodInfo ? methodInfo.ReturnType : typeof(void);
			var paramTypes = method.GetParameters().Select(p => p.ParameterType);

			if (!method.IsStatic)
			{
				if (declaring is null)
					throw new InvalidDataException(ET("全局方法必须是静态方法"));

				paramTypes = paramTypes.Prepend(declaring);
			}

			var paramArray = paramTypes.ToArray();

			DynamicMethod newMethod;

			if (declaring is not null)
				newMethod = new DynamicMethod(method.Name, returnType,
					paramArray, declaring, true);
			else
				newMethod = new DynamicMethod(method.Name, returnType,
					paramArray, method.Module, true);

			var ilInfo = newMethod.GetDynamicILInfo();
			var provider = new DynamicTokenProvider(ilInfo);

			int localSig = body.LocalSignatureMetadataToken;

			if (localSig != 0)
				ilInfo.SetLocalSignature(method.Module.ResolveSignature(localSig));
			else
				ilInfo.SetLocalSignature([0x07, 0x00]);

			var exceptItems = body.ExceptionHandlingClauses;

			if (exceptItems.Count > 0)
				ilInfo.SetExceptions(ExceptionWriter.WriteAll(exceptItems, provider));

			ilInfo.SetCode(RewriteCode(method.Module, body, provider), body.MaxStackSize);

			newMethod.InitLocals = body.InitLocals;

			return newMethod;
		}

		[SkipLocalsInit]
		private static byte[] RewriteCode
			(Module methodModule, MethodBody body, DynamicTokenProvider tokenProvider)
		{
			Span<byte> buffer = stackalloc byte[sizeof(long)];
			Span<byte> zero = stackalloc byte[sizeof(int)];

			var stream = new MemoryStream();

			zero.Clear();

			foreach (var code in body)
			{
				if (code.OpCode.Size == sizeof(short))
				{
					WriteInt16BigEndian(buffer, code.OpCode.Value);
					stream.Write(buffer[..sizeof(short)]);
				}
				else
				{
					buffer[0] = (byte)code.OpCode.Value;
					stream.Write(buffer[..sizeof(byte)]);
				}

				switch (code.OpCode.OperandType)
				{
					case OperandType.InlineNone:
						break;
					case OperandType.ShortInlineBrTarget:
						buffer[0] = (byte)code.OperandS8;
						stream.Write(buffer[..sizeof(sbyte)]);
						break;
					case OperandType.ShortInlineI:
					case OperandType.ShortInlineVar:
						buffer[0] = code.OperandU8;
						stream.Write(buffer[..sizeof(byte)]);
						break;
					case OperandType.InlineVar:
						WriteInt16LittleEndian(buffer, code.Operand16);
						stream.Write(buffer[..sizeof(short)]);
						break;
					case OperandType.InlineI:
					case OperandType.InlineBrTarget:
						WriteInt32LittleEndian(buffer, code.Operand32);
						stream.Write(buffer[..sizeof(int)]);
						break;
					case OperandType.InlineI8:
						WriteInt64LittleEndian(buffer, code.Operand64);
						stream.Write(buffer[..sizeof(long)]);
						break;
					case OperandType.ShortInlineR:
						WriteSingleLittleEndian(buffer, code.OperandF32);
						stream.Write(buffer[..sizeof(float)]);
						break;
					case OperandType.InlineR:
						WriteDoubleLittleEndian(buffer, code.OperandF64);
						stream.Write(buffer[..sizeof(double)]);
						break;
					case OperandType.InlineString:
						if (code.Operand32 != 0)
						{
							int token = tokenProvider.GetTokenFor(
								methodModule.ResolveString(code.Operand32));

							WriteInt32LittleEndian(buffer, token);
							stream.Write(buffer[..sizeof(int)]);
						}
						else
							stream.Write(zero[..sizeof(int)]);

						break;
					case OperandType.InlineField:
					case OperandType.InlineMethod:
					case OperandType.InlineType:
					case OperandType.InlineTok:
						if (code.Operand32 != 0)
						{
							var member = methodModule.ResolveMember(code.Operand32)
								?? throw new NullReferenceException(ET("原代码指向的MetadataToken解析为空成员"));

							int token;

							if (member is DynamicMethod dynamicMethod)
								token = tokenProvider.GetTokenFor(dynamicMethod);
							else if (member is MethodInfo methodInfo)
								token = tokenProvider.GetTokenFor(methodInfo);
							else if (member is ConstructorInfo constructorInfo)
								token = tokenProvider.GetTokenFor(constructorInfo);
							else if (member is FieldInfo fieldInfo)
								token = tokenProvider.GetTokenFor(fieldInfo);
							else if (member is Type type)
								token = tokenProvider.GetTokenFor(type);
							else
								throw new NotSupportedException(ET("不支持解析的成员类型: {0}",
									member.GetType().FullName));

							WriteInt32LittleEndian(buffer, token);
							stream.Write(buffer[..sizeof(int)]);
						}
						else
							stream.Write(zero[..sizeof(int)]);

						break;
					case OperandType.InlineSig:
						if (code.Operand32 != 0)
						{
							int token = tokenProvider.GetTokenFor(
								methodModule.ResolveSignature(code.Operand32));

							WriteInt32LittleEndian(buffer, token);
							stream.Write(buffer[..sizeof(int)]);
						}
						else
							stream.Write(zero[..sizeof(int)]);

						break;
					case OperandType.InlineSwitch:
						{
							var labels = code.SwitchLabels;

							WriteInt32LittleEndian(buffer, labels.Length);
							stream.Write(buffer[..sizeof(int)]);

							for (int i = 0; i < labels.Length; i++)
							{
								WriteInt32LittleEndian(buffer, labels[i]);
								stream.Write(buffer[..sizeof(int)]);
							}
						}
						break;
				}
			}

			return stream.ToArray();
		}
	}
}
