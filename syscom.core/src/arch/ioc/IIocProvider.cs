using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syscom
{
	/// <summary>提供自動向IoC註冊的Interface，只要有實作就會被加入IoC中</summary>
	public interface IAutoIocRegister
	{
		/// <summary>IoC註冊的識別Id</summary>
		String Identity { get; }

		/// <summary>請在此方法中對IoCProvider進行設定</summary>
		void SettingRegisters( IIocProvider provider, List<Type> allSystemTypes );
	}


	public interface IIocProvider
	{
		//===================================================================
		// 常用公開方法
		//===================================================================
		/// <summary>自動依照內建的RegisterInterface進行Ioc設定</summary>
		void AutoBuildByRegisters();

		/// <summary>取得指定型別的實例</summary>
		TInterface GetBy<TInterface>();


		/// <summary>取得指定型別的所有實例</summary>
		IEnumerable<Object> GetAllBy( Type type );

		/// <summary>取得指定型別的所有實例</summary>
		IEnumerable<TInterface> GetAllBy<TInterface>();


		//===================================================================
		// 註冊方法
		//===================================================================
		void SetBy<TService, TImplementation>( Boolean build = false ) where TImplementation : TService;

		void SetBy( Type typeOfService, Type typeOfImplement, Boolean build = false );

		void SetContainerBy<TContainer>( String id, Action<TContainer> action, Boolean build = false ) where TContainer : class;
	}
}