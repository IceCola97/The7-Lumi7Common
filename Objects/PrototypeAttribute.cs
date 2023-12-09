using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Objects
{
	/// <summary>
	/// 指示标记项目前处于原型状态
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.All)]
	public sealed class PrototypeAttribute : Attribute
	{
	}
}
