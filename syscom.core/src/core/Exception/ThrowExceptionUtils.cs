using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;
using syscom;

namespace syscom
{
	public static partial class Err
	{
		public static CoreException AppLayer( String message, Exception? ex = null, [CallerMemberName] String? caller = null, [CallerFilePath] String? callerFile = null, [CallerLineNumber] Int32 callerLineNumber = 0 )
		{
			var throwerMethod = StackUtils.GetPrevious().GetMethod();
			var thrower = throwerMethod.DeclaringType;
			return new CoreException( "[應用程式層級]" + message, ex, thrower, throwerMethod, CoreExceptionType.AppLayer );
		}

		public static CoreException BusinessLogic( String message, Exception? ex = null )
		{
			var throwerMethod = StackUtils.GetPrevious().GetMethod();
			var thrower = throwerMethod.DeclaringType;
			return new CoreException( "[商業邏輯層級]" + message, ex, thrower, throwerMethod, CoreExceptionType.BusinessLogic );
		}

		public static CoreException Module( String module, String message, Exception? ex = null )
		{
			var throwerMethod = StackUtils.GetPrevious().GetMethod();
			var thrower = throwerMethod.DeclaringType;
			return new CoreException( "[" + module + "]" + message, ex, thrower, throwerMethod, CoreExceptionType.Module );
		}

		public static CoreException Utility( String message, Exception? ex = null )
		{
			var throwerMethod = StackUtils.GetPrevious().GetMethod();
			var thrower = throwerMethod.DeclaringType;
			return new CoreException( message, ex, thrower, throwerMethod, CoreExceptionType.Utility );
		}

		public static CoreException Extension( String message, Exception? ex = null )
		{
			var throwerMethod = StackUtils.GetPrevious().GetMethod();
			var thrower = throwerMethod.DeclaringType;
			return new CoreException( message, ex, thrower, throwerMethod, CoreExceptionType.Extension );
		}

		public static CoreException Testing( String message, Exception? ex = null )
		{
			var throwerMethod = StackUtils.GetPrevious().GetMethod();
			var thrower = throwerMethod.DeclaringType;
			return new CoreException( message, ex, thrower, throwerMethod, CoreExceptionType.Testing );
		}

		public static CoreException NoSupport( String message, Exception? ex = null )
		{
			var throwerMethod = StackUtils.GetPrevious().GetMethod();
			var thrower = throwerMethod.DeclaringType;
			return new CoreException( "[NoSupport]" + message, ex, thrower, throwerMethod, CoreExceptionType.NoSupport );
		}
	}


	partial class Err
	{
		public static void TryBy<TException>( String errorMessage, Action maybeErrorAction ) where TException : Exception { TryBy( errorMessage, maybeErrorAction, typeof( TException ) ); }

		public static void TryBy( String errorMessage, Action maybeErrorAction, Type? exceptionType = null )
		{
			if ( exceptionType == null ) exceptionType = typeof( Exception );
			try
			{
				maybeErrorAction();
			}
			catch ( Exception ex )
			{
				if ( String.IsNullOrEmpty( errorMessage ) ) throw;

				Exception? newEx = null;
				try { newEx = (Exception) Activator.CreateInstance( exceptionType, new Object[] { errorMessage, ex } ); }
				catch ( Exception nex ) { throw new CoreException( $"無法實例化異常型別[{exceptionType.FullName}]", nex ); }

				throw newEx;
			}
		}
	}
}
