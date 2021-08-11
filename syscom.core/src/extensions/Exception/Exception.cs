using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System
{
	public static partial class ExceptionExtensions
	{
		public static Exception GetLastInnerException( this Exception ex )
		{
			while ( ex.InnerException != null ) ex = ex.InnerException;
			return ex;
		}

		/// <summary>在自身或內部階層尋找指定型別, 若找不到符合的則返回null</summary>
		public static TException FindEqualTypeBy<TException>( this Exception ex ) where TException : Exception
		{
			do
			{
				if ( ex is TException ) return (TException) ex;
				ex = ex.InnerException;
			}
			while ( ex != null );

			return null;
		}

		public static Boolean IsContain<TException>( this Exception ex )
		{
			do
			{
				if ( ex is TException ) return true;
				ex = ex.InnerException;
			}
			while ( ex != null );

			return false;
		}

		/// <summary>呼叫InternalPreserveStackTrace, 讓接著拋出的Exception不清除原始的StackTrace屬性</summary>
		public static TException InvokeInternalPreserveStackTrace<TException>( this TException ex ) where TException : Exception
		{
			var savestack = Delegate.CreateDelegate( typeof( ThreadStart ), ex, "InternalPreserveStackTrace", false, false ) as ThreadStart;
			savestack?.Invoke();
			//throw ex;// -- next u can throw that exception without trashing the stack
			return ex;
		}

		public static void DynamicSetMessageBy<TException>( this TException ex, String message ) where TException : Exception
		{
			var field = ex.GetType().GetField( "_message", BindingFlags.Instance | BindingFlags.NonPublic );
			if ( field == null ) return;
			field.SetValue( ex, message );
		}

		public static void DynamicSetStackTraceBy<TException>( this TException ex, String stackTrace ) where TException : Exception
		{
			var field = typeof( Exception ).GetField( "_stackTraceString", BindingFlags.Instance | BindingFlags.NonPublic );
			if ( field == null ) return;
			field.SetValue( ex, stackTrace );
		}

		//public static void DynamicTargetSiteBy<TException>( this TException ex, MethodBase method ) where TException : Exception
		//{
		//	var field = typeof( Exception ).GetField( "_exceptionMethod", BindingFlags.Instance | BindingFlags.NonPublic );
		//	if ( field == null ) return;
		//	field.SetValue( ex, method );
		//}

		//public static void DynamicTargetSiteBy<TException>( this TException ex, Exception oldEx ) where TException : Exception
		//{
		//	var field = typeof( Exception ).GetField( "_exceptionMethodString", BindingFlags.Instance | BindingFlags.NonPublic );
		//	if ( field == null ) return;

		//	var value = field.GetValue( oldEx );

		//	field.SetValue( ex, value );
		//}
	}
}