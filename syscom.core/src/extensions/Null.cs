using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace System
{
	public static class NullExtensions
	{
		/// <summary>若該物件為Null則回傳Null,否則回傳指定的Lambda內容</summary>
		public static TResult GetNullOr<TClass, TResult>( this TClass currentObject, Func<TClass, TResult> lambda )
			where TClass : class
			where TResult : class
		{
			return currentObject == null ? null : lambda( currentObject );
		}

		///// <summary>若該物件為Null則回傳Default值, 否則回傳指定的Lambda內容</summary>
		//public static TResult GetDefaultOr<TClass, TResult>( this TClass currentObject, TResult defaultValue, Func<TClass, TResult> lambda )
		//	where TClass : class
		//	where TResult : class
		//{
		//	return ( currentObject == null ) ? defaultValue : lambda( currentObject );
		//}


		/// <summary>嘗試取得值型別的屬性, 若取得失敗則回傳Nullable型別</summary>
		public static TValue GetPropertyOrNullBy<TClass, TValue>( this TClass targetClass, Func<TClass, TValue> selectExpr )
			where TClass : class
		{
			try
			{
				var value = selectExpr( targetClass );
				return value;
			}
			catch
			{
				return default( TValue );
			}
		}


		/// <summary>將target轉型成指定類型並回傳，若target為null則回傳defaultValue</summary>
		public static TResult GetValueOrDefault<TResult>( this Object target )
		{
			if ( target == null ) return default( TResult );
			return (TResult) target;
		}

		/// <summary>
		/// 將target轉型成指定類型並回傳，若target為null則回傳defaultValue
		/// </summary>
		public static TResult GetValueOrDefault<TResult>( this Object target, TResult defaultValue )
		{
			if ( target == null ) return defaultValue;
			return (TResult) target;
		}

		public static Object GetValueOrDBNull<TValue>( this Nullable<TValue> target ) where TValue : struct { return target ?? (Object) DBNull.Value; }
	}
}

namespace syscom.data
{
	public static class NullExtensions
	{
		public static Object GetValueOrDBNull<TValue>( this TValue target ) where TValue : class { return target ?? (Object) DBNull.Value; }
	}
}
