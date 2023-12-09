using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Objects
{
	/// <summary>
	/// 指示此项在设计计划变更之后可能被废弃
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public sealed class MaybeObsoleteAttribute : Attribute
	{
		public MaybeObsoleteAttribute() { }

		public MaybeObsoleteAttribute(string message) => Message = message;

		public string? Message { get; }
	}
}
