using Lumi7Common.Threading;
using System.Reflection;
using System.Reflection.Emit;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 接口<see cref="ITokenProvider"/>在<see cref="DynamicILInfo"/>上的封装实现
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	/// <param name="ilInfo"></param>
	[ThreadSafe]
	public sealed class DynamicTokenProvider
		(DynamicILInfo ilInfo) : ITokenProvider
	{
		private readonly DynamicILInfo m_ILInfo = ilInfo;

		public int GetTokenFor(MethodInfo method)
		{
			if (method is DynamicMethod dynamicMethod)
				return GetTokenFor(dynamicMethod);

			if (method.DeclaringType is not null
				&& method.DeclaringType.IsGenericType)
				return m_ILInfo.GetTokenFor(method.MethodHandle, method.DeclaringType.TypeHandle);

			return m_ILInfo.GetTokenFor(method.MethodHandle);
		}

		public int GetTokenFor(ConstructorInfo ctor)
		{
			if (ctor.DeclaringType is not null
				&& ctor.DeclaringType.IsGenericType)
				return m_ILInfo.GetTokenFor(ctor.MethodHandle, ctor.DeclaringType.TypeHandle);

			return m_ILInfo.GetTokenFor(ctor.MethodHandle);
		}

		public int GetTokenFor(DynamicMethod method)
		{
			return m_ILInfo.GetTokenFor(method);
		}

		public int GetTokenFor(FieldInfo field)
		{
			if (field.DeclaringType is not null
				&& field.DeclaringType.IsGenericType)
				return m_ILInfo.GetTokenFor(field.FieldHandle, field.DeclaringType.TypeHandle);

			return m_ILInfo.GetTokenFor(field.FieldHandle);
		}

		public int GetTokenFor(Type type)
		{
			return m_ILInfo.GetTokenFor(type.TypeHandle);
		}

		public int GetTokenFor(string literal)
		{
			return m_ILInfo.GetTokenFor(literal);
		}

		public int GetTokenFor(byte[] signature)
		{
			return m_ILInfo.GetTokenFor(signature);
		}

		public int GetTokenFor(SignatureHelper signature)
		{
			return m_ILInfo.GetTokenFor(signature.GetSignature());
		}
	}
}
