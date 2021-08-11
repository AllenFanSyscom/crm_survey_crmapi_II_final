using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using syscom;

namespace System
{
	public static class IDictionaryExtensions
	{
		/// <summary>取得指定Key的值, 若取不到值, 則回傳DefaultValue</summary>
		public static TValue GetKeyValueOrDefaultBy<TValue>( this IDictionary<String, TValue> dic, String key, TValue defaultValue )
		{
			if ( !dic.ContainsKey( key ) ) return defaultValue;
			var value = dic[key];
			if ( value == null ) return defaultValue;
			try
			{
				return value;
			}
			catch ( Exception )
			{
				return defaultValue;
			}
		}

		/// <summary>取得指定Key的值並轉型為指定型別, 若轉型失敗或取不到值, 則回傳DefaultValue</summary>
		public static TValue GetKeyValueOrDefaultBy<TValue>( this IDictionary<String, Object> dic, String key, TValue defaultValue )
		{
			if ( !dic.ContainsKey( key ) ) return defaultValue;
			var value = dic[key];
			return value.ConvertOrDefault<TValue>( defaultValue );
		}


		public static TValue? GetKeyValueOrNullableBy<TValue>( this IDictionary<String, Object> dic, String key ) where TValue : struct
		{
			if ( !dic.ContainsKey( key ) ) return new TValue?();
			var value = dic[key];
			if ( value == null ) return new TValue?();
			try
			{
				return (TValue) value;
			}
			catch
			{
				return new TValue?();
			}
		}


		/// <summary>更新已存在的key/value或新增，使用另一個Dictionary</summary>
		public static void AddOrUpdateBy( this IDictionary<String, Object> current, IDictionary<String, Object> newer )
		{
			if ( newer == null ) throw new ArgumentNullException( "newer", "傳入的Dictionary為null" );

			foreach ( var key in newer.Keys )
				if ( current.ContainsKey( key ) )
					current[key] = newer[key];
				else
					current.Add( key, newer[key] );
		}
	}
}