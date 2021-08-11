using syscom.logging;
using syscom.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libs.DryIoc;

namespace syscom.arch.ioc.impls
{
	public partial class DryIocProvider : IIocProvider
	{
		public Object GetBy( Type type )
		{
			Verify();
			try
			{
				return _container.Resolve( type );
			}
			catch ( Exception ex )
			{
				throw Err.Module( "IoC", "IoC取得型別失敗, " + ex.Message, ex );
			}
		}

		public TInterface GetBy<TInterface>()
		{
			Verify();
			try
			{
				return _container.Resolve<TInterface>();
			}
			catch ( Exception ex )
			{
				throw Err.Module( "IoC", $"嘗試取得型別[{typeof( TInterface ).FullName}]失敗, " + ex.Message, ex );
			}
		}

		public IEnumerable<Object> GetAllBy( Type serviceType )
		{
			Verify();
			try
			{
				return _container.ResolveMany( serviceType );
			}
			catch ( Exception ex )
			{
				throw Err.Module( "IoC", "IoC取得型別失敗, " + ex.Message, ex );
			}
		}

		public IEnumerable<TInterface> GetAllBy<TInterface>()
		{
			Verify();
			try
			{
				return _container.ResolveMany<TInterface>();
			}
			catch ( Exception ex )
			{
				throw Err.Module( "IoC", "IoC取得型別失敗, " + ex.Message, ex );
			}
		}


		public void SetBy<TService, TImplementation>( Boolean build = false ) where TImplementation : TService
		{
			lock ( _locker )
			{
				var id = $"{typeof( TService ).FullName}+{typeof( TImplementation ).FullName}";
				if ( _actions.ContainsKey( id ) ) throw Err.Module( "IoC", $"欲設定的IoC名稱[ {id} ]已被使用, 請確認設定是否重覆" );

				Action<Container> newAction = ( container ) =>
				{
					container.Register<TService, TImplementation>();
				};
				var added = _actions.TryAdd( id, newAction );
				if ( added == false ) throw Err.Module( "IoC", $"欲設定的IoC名稱[ {id} ] 加入註冊失敗, 請確認設定是否正確" );

				if ( build ) Build();
			}
		}

		public void SetBy( Type typeOfService, Type typeOfImplement, Boolean build = false )
		{
			lock ( _locker )
			{
				var id = $"{typeOfService.FullName}+{typeOfImplement.FullName}";
				if ( _actions.ContainsKey( id ) ) throw Err.Module( "IoC", $"欲設定的IoC名稱[ {id} ]已被使用, 請確認設定是否重覆" );
				Action<Container> newAction = ( container ) =>
				{
					container.Register( typeOfService, typeOfImplement );
				};
				var added = _actions.TryAdd( id, newAction );
				if ( added == false ) throw Err.Module( "IoC", $"欲設定的IoC名稱[ {id} ] 加入註冊失敗, 請確認設定是否正確" );

				if ( build ) Build();
			}
		}

		public void SetContainerBy<TContainer>( String id, Action<TContainer> action, Boolean build = false ) where TContainer : class
		{
			lock ( _locker )
			{
				//if ( VerifyInitialized() ) throw Err.Module( "IoC", "IoC已建置完成, 無法進行更新" );
				if ( _actions.ContainsKey( id ) ) throw Err.Module( "IoC", $"欲設定的IoC名稱[ {id} ]已被使用, 請確認設定是否重覆" );

				var unboxAction = action as Action<Container>;
				if ( unboxAction == null ) throw Err.Module( "IoC", $"欲設定的IoC名稱[ {id} ] 目標型別異常, 請確認您知道如何使用此方法" );

				var success = _actions.TryAdd( id, unboxAction );
				if ( success == false ) throw Err.Module( "IoC", $"欲設定的IoC名稱[ {id} ] 目標寫入異常, 請確認您知道如何使用此方法" );

				if ( build ) Build();
			}
		}
	}
}