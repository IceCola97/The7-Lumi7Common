using Lumi7Common.Threading;
using System;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供方法的动态修补能力
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public static partial class MethodPatch
	{
		private static readonly nint m_GetMethodDescriptor;

		static MethodPatch()
		{
			DynamicSupport.AssertDynamicSupport();

			var method = typeof(DynamicMethod).GetMethod("GetMethodDescriptor",
				BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new PlatformNotSupportedException(ET("当前的.NET版本不是8.0"));

			RuntimeHelpers.PrepareMethod(method.MethodHandle);
			m_GetMethodDescriptor = method.MethodHandle.GetFunctionPointer();
		}

		public static bool IsRuntimePatchSupported
		{
			get
			{
				return RuntimeInformation.ProcessArchitecture switch
				{
					Architecture.X86
					or Architecture.X64 => true,
					_ => false,
				};
			}
		}

		/// <summary>
		/// 在运行时将目标方法替换为修补方法，会对目标方法造成不可逆的修改<br/>
		/// 如果目标方法已经被替换，则覆盖上一次的替换<br/>
		/// 请注意，如果替换方法是一个动态方法，必须确保此方法不会被GC回收，否则将导致内存访问越界
		/// </summary>
		/// <param name="target"></param>
		/// <param name="patched"></param>
		public static void RuntimePatch(MethodBase target, MethodBase patched)
		{
			if (!IsRuntimePatchSupported)
				throw new PlatformNotSupportedException(ET("当前CPU架构不支持使用此方法"));

			unsafe
			{
				void* targetAddr = GetMethodPointer(target);
				void* patchAddr = GetMethodPointer(patched);
				PatchProvider.Patch(targetAddr, patchAddr);
			}
		}

		/// <summary>
		/// 在运行时将目标方法替换为修补方法，会对目标方法造成不可逆的修改<br/>
		/// 如果目标方法已经被替换，则覆盖上一次的替换
		/// </summary>
		/// <param name="target"></param>
		/// <param name="patched"></param>
		public static void RuntimePatch(MethodBase target, Delegate patched)
		{
			if (!IsRuntimePatchSupported)
				throw new PlatformNotSupportedException(ET("当前CPU架构不支持使用此方法"));

			var wrapped = DelegateWrapper.Wrap(patched);
			RuntimePatch(target, wrapped);
		}

		/// <summary>
		/// 确保给定方法通过了JIT编译
		/// </summary>
		/// <param name="method"></param>
		public static void PrepareMethod(MethodBase method)
		{
			if (method is not DynamicMethod dynamicMethod)
			{
				RuntimeHelpers.PrepareMethod(method.MethodHandle);
				return;
			}

			WrapDynamicMethod(dynamicMethod);
		}

		/// <summary>
		/// 确保给定方法通过了JIT编译，并获取给定方法的方法句柄（包括动态方法）
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public static RuntimeMethodHandle GetMethodHandle(this MethodBase method)
		{
			if (method is not DynamicMethod dynamicMethod)
			{
				RuntimeHelpers.PrepareMethod(method.MethodHandle);
				return method.MethodHandle;
			}
			else
			{
				unsafe
				{
					return ((delegate* managed<DynamicMethod, RuntimeMethodHandle>)
						m_GetMethodDescriptor)(dynamicMethod);
				}
			}
		}

		private static unsafe void* GetMethodPointer(MethodBase method)
			=> (void*)method.GetMethodHandle().GetFunctionPointer();

		private static Delegate WrapDynamicMethod(DynamicMethod dynamicMethod)
		{
			string sig = DelegateBuilder.GenerateSignature(dynamicMethod);
			return dynamicMethod.CreateDelegate(DelegateCache.ObtainDelegate(sig));
		}

		private static class Atomic
		{
			private static unsafe readonly delegate* managed<void*, short, void>
				m_WriteInt16LittleEndianCore;
			private static unsafe readonly delegate* managed<void*, short>
				m_ReadInt16LittleEndianCore;

			static unsafe Atomic()
			{
				if (nint.Size == 4)
				{
					m_WriteInt16LittleEndianCore = &WriteInt16LittleEndian32;
					m_ReadInt16LittleEndianCore = &ReadInt16LittleEndian32;
				}
				else if (nint.Size == 8)
				{
					m_WriteInt16LittleEndianCore = &WriteInt16LittleEndian64;
					m_ReadInt16LittleEndianCore = &ReadInt16LittleEndian64;
				}
				else
					throw new PlatformNotSupportedException(ET("仅支持在32位于64位下的原子操作"));
			}

			public static unsafe void WriteInt16LittleEndian(void* ptr, short value)
				=> m_WriteInt16LittleEndianCore(ptr, value);

			[SkipLocalsInit]
			private static unsafe void WriteInt16LittleEndian32(void* ptr, short value)
			{
				unchecked
				{
					byte* buffer = stackalloc byte[8];
					int pos = ((int)ptr) & 3;

					if (pos == 3)
					{
						// 指定的内存地址没有对齐，CPU需要花费两个指令周期去执行读取指令
						// 这将导致读取操作是非原子性的，为确保不会脏读，需要通过Interlocked读取

						do
						{
							*(long*)buffer /* CAS操作的旧值 */ = Interlocked.Read(ref *(long*)ptr);
							pos /* CAS操作的新值 */ = *(int*)buffer;
							*(short*)&pos = value;
						}
						while (Interlocked.CompareExchange(
							ref *(int*)ptr, pos, *(int*)buffer) == *(int*)buffer);
					}
					else
					{
						// 指定的内存地址可以对齐，通过计算确保读取操作只花费一个CPU指令周期

						do
						{
							*(int*)buffer /* CAS操作的旧值 */ =
								*(int*)(buffer + 4) /* CAS操作的新值 */ = *(int*)((byte*)ptr - pos);
							*(short*)(buffer + 4 + pos) = value;
						}
						while (Interlocked.CompareExchange(
							ref *(int*)ptr, *(int*)(buffer + 4), *(int*)buffer) == *(int*)buffer);
					}
				}
			}

			[SkipLocalsInit]
			private static unsafe void WriteInt16LittleEndian64(void* ptr, short value)
			{
				unchecked
				{
					byte* buffer = stackalloc byte[8];
					int pos = ((int)ptr) & 7;

					if (pos == 7)
					{
						// 指定的内存地址没有对齐，CPU需要花费两个指令周期去执行读取指令
						// 这将导致读取操作是非原子性的，为确保不会脏读，需要通过Interlocked读取

						do
						{
							*(long*)buffer /* CAS操作的旧值 */ = Interlocked.Read(ref *(long*)ptr);
							pos /* CAS操作的新值 */ = *(int*)buffer;
							*(short*)&pos = value;
						}
						while (Interlocked.CompareExchange(
							ref *(int*)ptr, pos, *(int*)buffer) == *(int*)buffer);
					}
					else
					{
						// 指定的内存地址可以对齐，通过计算确保读取操作只花费一个CPU指令周期

						do
						{
							*(int*)buffer /* CAS操作的旧值 */ =
								*(int*)(buffer + 4) /* CAS操作的新值 */ = *(int*)((byte*)ptr - pos);
							*(short*)(buffer + 4 + pos) = value;
						}
						while (Interlocked.CompareExchange(
							ref *(int*)ptr, *(int*)(buffer + 4), *(int*)buffer) == *(int*)buffer);
					}
				}
			}

			public static unsafe short ReadInt16LittleEndian(void* ptr)
				=> m_ReadInt16LittleEndianCore(ptr);

			[SkipLocalsInit]
			private static unsafe short ReadInt16LittleEndian32(void* ptr)
			{
				unchecked
				{
					int pos = ((int)ptr) & 3;

					if (pos == 3)
						return (short)Interlocked.CompareExchange(
							ref *(int*)ptr, 0, 0);

					return (short)((*(int*)((byte*)ptr - pos)) >> (pos << 3));
				}
			}

			[SkipLocalsInit]
			private static unsafe short ReadInt16LittleEndian64(void* ptr)
			{
				unchecked
				{
					int pos = ((int)ptr) & 7;

					if (pos == 7)
						return (short)Interlocked.Read(ref *(long*)ptr);

					return (short)((*(int*)((byte*)ptr - pos)) >> (pos << 3));
				}
			}
		}

		private sealed unsafe class PatchProvider
		{
			private static readonly delegate* managed<void*, void*, void> m_PatchCore;

			static PatchProvider()
			{
				m_PatchCore = RuntimeInformation.ProcessArchitecture switch
				{
					Architecture.X86
					or Architecture.X64 => X86X64PatchImpl.GetPatchCore(),
					_ => throw new NotSupportedException(ET("不支持的CPU架构")),
				};
			}

			public static void Patch(void* targetAddr, void* patchAddr) => m_PatchCore(targetAddr, patchAddr);
		}

		private interface IPatchProvider
		{
			static abstract unsafe delegate* managed<void*, void*, void> GetPatchCore();
		}

		private sealed unsafe partial class X86X64PatchImpl : IPatchProvider
		{
			private static readonly delegate* managed<void*, void*, void> m_PatchCore;

			static X86X64PatchImpl()
			{
				if (nint.Size == 4)
					m_PatchCore = &X86Patch;
				else if (nint.Size == 8)
					m_PatchCore = &X64Patch;
				else
					throw new NotSupportedException();
			}

			private static bool BeginWrite(void* address, int length, out int oldAccess)
			{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT)
					return VirtualProtectEx(GetCurrentProcess(), address,
						length, PAGE_EXECUTE_READWRITE, out oldAccess);

				oldAccess = default;
				return true;
			}

			private static void EndWrite(void* address, int length, int oldAccess)
			{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT)
					VirtualProtectEx(GetCurrentProcess(), address,
						length, oldAccess, out _);
			}

			private static void EnsureRealAddress(ref byte* targetAddr)
			{
				// *(ushort*)targetAddr == 0x25FF
				if (Atomic.ReadInt16LittleEndian(targetAddr) == 0x25FF)
				{
					int offset = *(int*)(targetAddr + 2);
					targetAddr = *(byte**)(targetAddr + offset + 6);
				}
			}

			private static void X86Patch(void* targetAddr, void* patchAddr)
			{
				byte* bTargetAddr = (byte*)targetAddr;
				byte* bPatchAddr = (byte*)patchAddr;

				//byte[] originHeader = new byte[5];
				byte[] hookedHeader = new byte[5];

				int jumpSize = (int)bPatchAddr - (int)bTargetAddr - 5;
				hookedHeader[0] = 0xE9;
				Array.Copy(BitConverter.GetBytes(jumpSize), 0, hookedHeader, 1, 4);

				EnsureRealAddress(ref bTargetAddr);

				if (!BeginWrite(bTargetAddr, 5, out int oldAccess))
					throw new InvalidOperationException(ET("无法更改目标方法的内存保护"));

				try
				{
					// 已废弃，因为X64Patch中不能确保此处的原子读取
					//fixed (byte* ptrOrig = originHeader)
					//{
					//	Unsafe.CopyBlock(ptrOrig, bTargetAddr, 5);
					//}

					// *(ushort*)bTargetAddr = 0xFEEB;
					Atomic.WriteInt16LittleEndian(bTargetAddr, unchecked((short)0xFEEB));

					Sleep(3);

					fixed (byte* ptrHook = hookedHeader)
					{
						Unsafe.CopyBlock(bTargetAddr + 2, ptrHook + 2, 3);

						// *(ushort*)bTargetAddr = *(ushort*)ptrHook;
						Atomic.WriteInt16LittleEndian(bTargetAddr, *(short*)ptrHook);
					}
				}
				finally
				{
					EndWrite(bTargetAddr, 5, oldAccess);
				}
			}

			private static void X64Patch(void* targetAddr, void* patchAddr)
			{
				byte* bTargetAddr = (byte*)targetAddr;
				byte* bPatchAddr = (byte*)patchAddr;

				//byte[] originHeader = new byte[12];
				byte[] hookedHeader = new byte[12];

				hookedHeader[0] = 0x48;
				hookedHeader[1] = 0xB8;
				hookedHeader[10] = 0x50;
				hookedHeader[11] = 0xC3;
				BinaryPrimitives.WriteInt64LittleEndian(hookedHeader.AsSpan(2, 8), (long)bPatchAddr);

				EnsureRealAddress(ref bTargetAddr);

				if (!BeginWrite(bTargetAddr, 12, out int oldAccess))
					throw new InvalidOperationException(ET("无法更改目标方法的内存保护"));

				try
				{
					// 已废弃，因为不能确保此处的原子读取
					//fixed (byte* ptrOrig = originHeader)
					//{
					//	Unsafe.CopyBlock(ptrOrig, bTargetAddr, 12);
					//}

					// *(ushort*)bTargetAddr = 0xFEEB;
					Atomic.WriteInt16LittleEndian(bTargetAddr, unchecked((short)0xFEEB));

					Sleep(3);

					fixed (byte* ptrHook = hookedHeader)
					{
						Unsafe.CopyBlock(bTargetAddr + 2, ptrHook + 2, 10);

						// *(ushort*)bTargetAddr = *(ushort*)ptrHook;
						Atomic.WriteInt16LittleEndian(bTargetAddr, *(short*)ptrHook);
					}
				}
				finally
				{
					EndWrite(bTargetAddr, 12, oldAccess);
				}
			}

			public static delegate*<void*, void*, void> GetPatchCore() => m_PatchCore;

			private const int PAGE_EXECUTE_READWRITE = 0x40;

			[LibraryImport("kernel32.dll")]
			private static partial void Sleep(int milliseconds);

			[LibraryImport("kernel32.dll")]
			private static partial nint GetCurrentProcess();

			[LibraryImport("kernel32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			private static partial bool VirtualProtectEx(nint hProcess, void* lpAddress,
				int dwSize, int flNewProtect, out int lpflOldProtect);
		}
	}
}
