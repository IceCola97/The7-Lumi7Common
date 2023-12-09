using Lumi7Common.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Entities
{
	/// <summary>
	/// 实体类型通用接口
	/// <br/><strong><em>暂定</em></strong>
	/// </summary>
	public interface IRealizedEntity
	{
		/// <summary>
		/// 当前实体的类型<br/>
		/// 不同类型的实体将被单独编译
		/// </summary>
		int EntityType { get; }
	}

	/// <summary>
	/// 实体类型泛型接口
	/// <br/>暂定
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IRealizedEntity<T> : IRealizedEntity where T : struct, Enum
	{
		/// <summary>
		/// 当前实体的类型<br/>
		/// 不同类型的实体将被单独编译
		/// </summary>
		new T EntityType { get; }

		int IRealizedEntity.EntityType => EntityType.ToInt32();
	}
}
