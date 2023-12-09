using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Objects
{
	/// <summary>
	/// 向IOC对象容器提供给定类或给定路径下某个配置项的值
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[IOCComponent]
	public interface IConfigDataSource
	{
		/// <summary>
		/// 通过给定路径获取配置项的值
		/// </summary>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="path"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		TResult FetchItem<TResult>(string path, string name);

		/// <summary>
		/// 通过给定类获取配置项的值
		/// </summary>
		/// <typeparam name="TClass"></typeparam>
		/// <typeparam name="TResult"></typeparam>
		/// <param name="name"></param>
		/// <returns></returns>
		TResult FetchItem<TClass, TResult>(string name);
	}
}
