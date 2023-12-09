using Lumi7Common.Objects;

namespace Lumi7Common.Types
{
	/// <summary>
	/// 提供<see cref="FieldType"/>的相关操作标准
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[IOCComponent]
	public interface IStateFieldStandard
	{
		private static readonly InstanceProvider<IStateFieldStandard> m_Standard = new();

		/// <summary>
		/// 获取IOC支持下的<see cref="IStateFieldStandard"/>的实例对象
		/// </summary>
		public static IStateFieldStandard Instance => m_Standard.Instance;

		/// <summary>
		/// 判断给定的<see cref="Type"/>是否是支持的字段类型<br/>
		/// 简单来说就是可以在<see cref="GetFieldType(Type)"/>中返回有效<see cref="FieldType"/>值的类型
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		bool IsAllowedFieldType(Type type);

		/// <summary>
		/// 根据给定的<see cref="FieldType"/>得到对应的<see cref="Type"/>对象<br/>
		/// 对于不支持的<see cref="FieldType"/>将抛出异常
		/// </summary>
		/// <param name="fieldType"></param>
		/// <returns></returns>
		Type GetRawType(FieldType fieldType);

		/// <summary>
		/// 根据给定的<see cref="Type"/>得到对应的<see cref="FieldType"/>枚举值<br/>
		/// 对于不支持的<see cref="Type"/>将抛出异常，此函数不应该返回<see cref="FieldType.Invalid"/>
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		FieldType GetFieldType(Type type);
	}
}
