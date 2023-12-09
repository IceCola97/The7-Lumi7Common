using Lumi7Common.Threading;
using System.Reflection;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 提供<see cref="MethodBody"/>的IL枚举器扩展
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public static class ILDecompilerExtension
	{
		[ThreadUnsafe]
		public static ILDecompiler GetEnumerator(this MethodBody methodBody) => new(methodBody);
	}
}
