using Lumi7Common.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Lumi7Common.Reflection.IL
{
	/// <summary>
	/// 接口<see cref="ITokenProvider"/>在<see cref="ModuleBuilder"/>上的封装实现
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	/// <param name="module"></param>
	[ThreadSafe]
	public sealed class ModuleTokenProvider
		(ModuleBuilder module) : ITokenProvider
	{
		private static readonly PutSignature m_PutSignature;

		private delegate void PutSignature(SignatureHelper helper, byte[] sigData);

		static ModuleTokenProvider()
		{
			var assembly_CoreLib = typeof(void).Assembly;
			var type_SignatureHelper = assembly_CoreLib.GetType("System.Reflection.Emit.SignatureHelper")
				?? throw new ImpossibleException(ET("当前.NET框架版本超出适配范围"));
			var field_SignatureHelper_m_sigDone = type_SignatureHelper.GetField("m_sigDone", BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new ImpossibleException(ET("当前.NET框架版本超出适配范围"));
			var field_SignatureHelper_m_currSig = type_SignatureHelper.GetField("m_currSig", BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new ImpossibleException(ET("当前.NET框架版本超出适配范围"));
			var field_SignatureHelper_m_signature = type_SignatureHelper.GetField("m_signature", BindingFlags.Instance | BindingFlags.NonPublic)
				?? throw new ImpossibleException(ET("当前.NET框架版本超出适配范围"));

			var method = new DynamicMethod("PutSignature", typeof(void),
				[typeof(SignatureHelper), typeof(byte[])], typeof(SignatureHelper), true);
			var ilGen = method.GetILGenerator();

			ilGen.Emit(OpCodes.Ldarg_0);
			ilGen.Emit(OpCodes.Dup);
			ilGen.Emit(OpCodes.Ldc_I4_1);
			ilGen.Emit(OpCodes.Stfld, field_SignatureHelper_m_sigDone);
			ilGen.Emit(OpCodes.Dup);
			ilGen.Emit(OpCodes.Ldarg_1);
			ilGen.Emit(OpCodes.Ldlen);
			ilGen.Emit(OpCodes.Stfld, field_SignatureHelper_m_currSig);
			ilGen.Emit(OpCodes.Ldarg_1);
			ilGen.Emit(OpCodes.Stfld, field_SignatureHelper_m_signature);
			ilGen.Emit(OpCodes.Ret);

			m_PutSignature = method.CreateDelegate<PutSignature>();
		}

		private readonly ModuleBuilder m_Module = module;

		public int GetTokenFor(MethodInfo method)
		{
			return m_Module.GetMethodMetadataToken(method);
		}

		public int GetTokenFor(ConstructorInfo ctor)
		{
			return m_Module.GetMethodMetadataToken(ctor);
		}

		public int GetTokenFor(DynamicMethod method)
		{
			return m_Module.GetMethodMetadataToken(method);
		}

		public int GetTokenFor(FieldInfo field)
		{
			return m_Module.GetFieldMetadataToken(field);
		}

		public int GetTokenFor(Type type)
		{
			return m_Module.GetTypeMetadataToken(type);
		}

		public int GetTokenFor(string literal)
		{
			return m_Module.GetStringMetadataToken(literal);
		}

		public int GetTokenFor(byte[] signature)
		{
			var helper = (SignatureHelper)RuntimeHelpers.GetUninitializedObject(typeof(SignatureHelper));
			m_PutSignature.Invoke(helper, signature);
			return m_Module.GetSignatureMetadataToken(helper);
		}

		public int GetTokenFor(SignatureHelper signature)
		{
			return m_Module.GetSignatureMetadataToken(signature);
		}
	}
}
