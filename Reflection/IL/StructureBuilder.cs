using Lumi7Common.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供动态创建结构体的方法
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[ThreadSafe]
	public sealed class StructureBuilder
	{
		private static readonly DynamicNamespace m_Namespace
			= new(typeof(StructureBuilder).FullName ?? nameof(StructureBuilder));

		private static volatile int m_TypeId = 0x20000;

		private readonly TypeBuilder m_TypeBuilder;

		private static int NextId() => Interlocked.Increment(ref m_TypeId) - 1;

		public StructureBuilder(string name)
		{
			DynamicSupport.AssertDynamicSupport();

			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException(ET("结构体的名称无效"));

			m_TypeBuilder = m_Namespace.DefineType($"Structure{NextId()}.{name}",
				TypeAttributes.SequentialLayout, typeof(ValueType), (PackingSize)nint.Size);
		}

		/// <summary>
		/// 向结构体追加字段
		/// </summary>
		/// <param name="type"></param>
		/// <param name="name"></param>
		public void AppendField(Type type, string name)
			=> m_TypeBuilder.DefineField(name, type, FieldAttributes.Public);

		/// <summary>
		/// 构建结构体类型
		/// </summary>
		/// <returns></returns>
		public Type Create() => m_TypeBuilder.CreateType();
	}
}
