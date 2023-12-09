using Lumi7Common.Threading;
using System.Buffers.Binary;
using System.Reflection;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供将<see cref="ExceptionHandlingClause"/>列表转为二进制异常数据的方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public static class ExceptionWriter
	{
		public static byte[] WriteAll(
			IEnumerable<ExceptionHandlingClause> exceptions,
			ITokenProvider typeTokenProvider
		)
		{
			var stream = new MemoryStream();
			var binaryWriter = new BinaryWriter(stream);

			bool useFatFormat = false;

			foreach (var item in exceptions)
			{
				if (item.TryLength > ushort.MaxValue
					|| item.TryOffset > byte.MaxValue
					|| item.HandlerOffset > ushort.MaxValue
					|| item.HandlerLength > byte.MaxValue)
				{
					useFatFormat = true;
					break;
				}
			}

			int totalSize = 4;

			if (useFatFormat)
			{
				binaryWriter.Write(0x41);

				foreach (var item in exceptions)
				{
					binaryWriter.Write((uint)item.Flags);
					binaryWriter.Write((uint)item.TryOffset);
					binaryWriter.Write((uint)item.TryLength);
					binaryWriter.Write((uint)item.HandlerOffset);
					binaryWriter.Write((uint)item.HandlerLength);

					switch (item.Flags)
					{
						case ExceptionHandlingClauseOptions.Clause:
							var type = item.CatchType
								?? throw new InvalidDataException(ET("Catch必须要有一个捕获类型"));

							binaryWriter.Write((uint)typeTokenProvider.GetTokenFor(type));
							break;
						case ExceptionHandlingClauseOptions.Filter:
							binaryWriter.Write((uint)item.FilterOffset);
							break;
						case ExceptionHandlingClauseOptions.Finally:
						case ExceptionHandlingClauseOptions.Fault:
							binaryWriter.Write(0);
							break;
					}

					totalSize += 0x18;
				}
			}
			else
			{
				binaryWriter.Write(0x01);

				foreach (var item in exceptions)
				{
					binaryWriter.Write((ushort)item.Flags);
					binaryWriter.Write((ushort)item.TryOffset);
					binaryWriter.Write((byte)item.TryLength);
					binaryWriter.Write((ushort)item.HandlerOffset);
					binaryWriter.Write((byte)item.HandlerLength);

					switch (item.Flags)
					{
						case ExceptionHandlingClauseOptions.Clause:
							var type = item.CatchType
								?? throw new InvalidDataException(ET("Catch必须要有一个捕获类型"));

							binaryWriter.Write((uint)typeTokenProvider.GetTokenFor(type));
							break;
						case ExceptionHandlingClauseOptions.Filter:
							binaryWriter.Write((uint)item.FilterOffset);
							break;
						case ExceptionHandlingClauseOptions.Finally:
						case ExceptionHandlingClauseOptions.Fault:
							binaryWriter.Write(0);
							break;
					}

					totalSize += 0x0C;
				}
			}

			stream.Position = 1;

			Span<byte> buffer = stackalloc byte[4];
			BinaryPrimitives.WriteInt32LittleEndian(buffer, totalSize);
			stream.Write(buffer[..3]);

			return stream.ToArray();
		}
	}
}
