using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Entities
{
	/// <summary>
	/// 所有实体的基类
	/// <br/><strong><em>暂定</em></strong>
	/// </summary>
	public abstract class EntityBase(int entityType) : IRealizedEntity
	{
		/// <summary>
		/// 当前实体的族
		/// </summary>
		public abstract EntityFamily EntityFamily { get; }

		public int EntityType => entityType;
	}
}
