using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Types
{
	/// <summary>
	/// Luson的字段类型
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	public enum FieldType
	{
		Invalid = -1,
		Nil,
		Dual,
		Integral,
		Real,
		Point,
		Location,
		Text,
		Tuple,
		List,
		Map,
		Raw,
	}
}
