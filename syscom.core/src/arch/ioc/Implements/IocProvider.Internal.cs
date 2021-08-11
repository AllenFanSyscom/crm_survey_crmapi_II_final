using syscom.logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libs.DryIoc;
using syscom.Reflection;

namespace syscom.arch.ioc.impls
{
	partial class DryIocProvider
	{
		Container _container;
		readonly Object _locker = new Object();
		readonly ConcurrentDictionary<String, Action<Container>> _actions;

		public DryIocProvider() { _actions = new ConcurrentDictionary<String, Action<Container>>(); }

		public void AutoBuildByRegisters()
		{
			List<Type>? allUserTypes = null;
			try
			{
				allUserTypes = TypeUtils.GetAllUserTypes;
			}
			catch ( Exception ex ) { throw Err.Module( "IoC", $"[IoC自動註冊] 無法取得所有Class型別, 請確認引用的DLL是否正確", ex ); }

			var syscomTypes = allUserTypes.Where( t => !String.IsNullOrEmpty( t.FullName ) && t.FullName.StartsWith( "syscom" ) ).ToList();
			var autoRegisters = syscomTypes.Where( t => t.GetInterfaces().Any( i => i == typeof( IAutoIocRegister ) ) ).ToList();

			if ( autoRegisters.Count != 0 ) //throw Err.Module( "IoC", "找不到任何IoC映射模組, 請確認載入的Dll中有包含實作了IAutoIoCRegister的public類別" );
				foreach ( var registerType in autoRegisters )
				{
					IAutoIocRegister? register = null;
					try
					{
						register = Activator.CreateInstance( registerType ) as IAutoIocRegister;
						if ( register == null ) throw Err.Module( "IoC", "錯誤的對應, 找不到指定的IoC映射模組" );
					}
					catch ( CoreException ) { throw; }
					catch ( Exception ex )
					{
						throw Err.Module( "IoC", $"[IoC自動註冊] IoCRegister無法正常實例化, 請確認型別[ {registerType.FullName} ]不得包含需帶參數的建構子", ex );
					}


					try
					{
						register.SettingRegisters( this, allUserTypes );
					}
					catch ( Exception ex )
					{
						throw Err.Module( "IoC", $"[IoC自動註冊] 註冊IoC發生異常, 型別[ {registerType.FullName} ], " + ex.Message, ex );
					}
				}

			Build();
		}


		public void Verify()
		{
			if ( _container != null ) return;
			try
			{
				AutoBuildByRegisters();
			}
			catch ( Exception ex ) { throw Err.Module( "IoC", "IoC Provider 無法自動初始化, 請確認相關設定是否正確", ex ); }

			//var containerHasFbcs = _container.Rules.FallbackContainers != null && _container.Rules.FallbackContainers.Length > 0;
			//var containerHasRules = _container.Rules.UnknownServiceResolvers != null && _container.Rules.UnknownServiceResolvers.Length > 0;
			//if ( !containerHasFbcs && !containerHasRules ) throw Err.Module( "IoC", "IoC Provider尚未初始化, 無法使用, 請確認您是否已初始化Framework." );
		}


		public void Build()
		{
			lock ( _locker )
			{
				if ( _actions.Count == 0 ) throw Err.Module( "IoC", "IoC未設定任何對應, 無法進行建置" );
				if ( _container != null )
				{
					_container.Dispose();
					_container = null;
				}

				if ( _container == null ) _container = new Container( rules => rules.WithUnknownServiceResolvers( Rules.AutoResolveConcreteTypeRule() ) );

				foreach ( var actionKey in _actions.Keys )
				{
					var actionBy = _actions[actionKey];
					try
					{
						actionBy( _container );
					}
					catch ( Exception ex ) { throw Err.Module( "IoC", "[IoC建置] 執行IoC設定動作[ " + actionKey + " ]時發生錯誤", ex ); }
				}
			}
		}


		public void Clear( Boolean isClearSettings = false )
		{
			lock ( _locker )
			{
				if ( _container != null )
				{
					_container.Dispose();
					GC.SuppressFinalize( _container );
				}

				_container = null;

				if ( isClearSettings ) _actions.Clear();
			}
		}
	}
}
