using System;
using System.Linq;

#if net45
using System.Web.SessionState;

namespace System.Web.UI
{
	public static class SessionExtensions
	{
		/// <summary>嘗試取得Session的值並轉型為指定類型, 若無法取得或轉型失敗將回傳該型別預設值</summary>
		public static T Get<T>( this HttpSessionState session, String key )
		{
			var value = session[ key ];
			return value.ConvertOrDefault<T>();
		}

		/// <summary>嘗試取得Session的值並轉型為指定類型, 若無法取得或轉型失敗將回傳該型別預設值</summary>
		public static T Get<T>( this HttpSessionState session, String key, T defaultValue )
		{
			var value = session[ key ];
			return value.ConvertOrDefault<T>( defaultValue );
		}


		public static void Set<T>( this HttpSessionState session, String key, T value )
		{
			session[ key ] = value;
		}

	}
}


#endif
