# Lumi7Common

一些.NET的通用代码，包含一个简单的依赖注入容器实现

(2023/12/09 更新)
- 包含如下目录模块:
	- Exceptions\
		- 若干异常类
		- 异常杂项方法
	- Extensions\
		- 通用的扩展方法
	- Objects\
		- 简单的依赖注入相关实现
		- 其余杂项
	- Reflection\
		- 自动属性、操作符反射相关操作
		- 方法的提取与搜索操作
		- 反射杂项
		- IL\
			- 委托类型构造
			- 方法复制、代理、替换
			- 轻型的IL反编译器
			- 依赖注入相关的动态代码构造
			- 动态代码杂项
	- Text\
		- 简单的文本翻译功能
		- 简单的转义字符串读取
	- Threading\
		- 读写锁封装
		- 线程安全与线程不安全标注
		- 线程独占对象接口