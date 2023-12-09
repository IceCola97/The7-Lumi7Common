using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lumi7Common.Objects
{
	/// <summary>
	/// 注册IOC组件类型
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
	public sealed class IOCComponentAttribute : Attribute
	{
		public IOCComponentAttribute()
		{
		}
	}

	/// <summary>
	/// 当方法需要IOC依赖注入时指定读取配置时的配置类型或配置路径
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = true)]
	public sealed class IOCConfigSourceAttribute : Attribute
	{
		public IOCConfigSourceAttribute(Type configType)
		{
			ConfigType = configType
				?? throw new ArgumentNullException(nameof(configType));
		}

		public IOCConfigSourceAttribute(string configPath)
		{
			ConfigPath = configPath
				?? throw new ArgumentNullException(nameof(configPath));
		}

		/// <summary>
		/// 当提供器需要常量参数时指示容器应该使用什么类作为<see cref="IConfigDataSource.FetchItem{TClass}(string)"/>的参数
		/// </summary>
		public Type? ConfigType { get; } = null;

		/// <summary>
		/// 当提供器需要常量参数时指示容器应该使用什么路径作为<see cref="IConfigDataSource.FetchItem(string, string)"/>的参数
		/// </summary>
		public string? ConfigPath { get; } = null;
	}

	/// <summary>
	/// 注册IOC组件提供器，需要标注在有效的构造函数或静态有返回的方法上，错误的标注会被忽略<br/>
	/// 当没有指定组件类型时，注册的IOC组件提供器将以返回值类型作为组件类型<br/>
	/// 组件提供器的参数会自动接入依赖注入，如果是常量参数则通过<see cref="IConfigDataSource"/>来获取配置项，并将参数名作为配置项名称<br/>
	/// 如果要指定依赖注入的名称或常量配置项的名称，请使用<see cref="IOCNamedRequiredAttribute"/>
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = true)]
	public sealed class IOCComponentProviderAttribute : Attribute
	{
		public IOCComponentProviderAttribute(Type? type = null)
		{
			ComponentType = type;
		}

		public Type? ComponentType { get; }
	}

	/// <summary>
	/// 注册IOC命名组件提供器，需要标注在有效的构造函数或静态有返回的方法上，错误的标注会被忽略<br/>
	/// 组件提供器的参数会自动接入依赖注入，如果是常量参数则通过<see cref="IConfigDataSource"/>来获取配置项，并将参数名作为配置项名称<br/>
	/// 如果要指定依赖注入的名称或常量配置项的名称，请使用<see cref="IOCNamedRequiredAttribute"/>
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = true)]
	public sealed class IOCNamedProviderAttribute : Attribute
	{
		public IOCNamedProviderAttribute(string componentName)
		{
			ComponentName = componentName;
		}

		public string ComponentName { get; }
	}

	/// <summary>
	/// 标注参数或字段需要IOC依赖注入
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public sealed class IOCRequiredAttribute : Attribute
	{
		public IOCRequiredAttribute()
		{
		}
	}

	/// <summary>
	/// 标注参数或字段需要IOC命名依赖注入<br/>
	/// 如果IOC组件名称未指定，则将参数或字段名作为IOC组件名称<br/>
	/// 当<see cref="IOCNamedRequiredAttribute"/>与<see cref="IOCRequiredAttribute"/>同时标注了一个参数或字段时，<see cref="IOCNamedRequiredAttribute"/>优先生效<br/>
	/// 另外，当标注了一个组件提供器的参数或字段时，此标注将指定依赖注入的名称或常量配置名
	/// <br/><strong><em>已完成</em></strong>
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
	public sealed class IOCNamedRequiredAttribute : Attribute
	{
		public IOCNamedRequiredAttribute(string? name = null)
		{
			Name = name;
		}

		public string? Name { get; }
	}
}
