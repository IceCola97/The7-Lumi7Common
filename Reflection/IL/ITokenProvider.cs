using System.Reflection;
using System.Reflection.Emit;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// MetadataToken提供接口
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public interface ITokenProvider
	{
		int GetTokenFor(MethodInfo method);

		int GetTokenFor(ConstructorInfo ctor);

		int GetTokenFor(DynamicMethod method);

		int GetTokenFor(FieldInfo field);

		int GetTokenFor(Type type);

		int GetTokenFor(string literal);

		int GetTokenFor(byte[] signature);

		int GetTokenFor(SignatureHelper signature);
	}
}
