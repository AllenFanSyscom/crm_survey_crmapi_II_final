using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

#pragma warning disable 1591

namespace syscom
{
	public static class EnumUtils
	{
		/// <summary>嘗試從Object型態之值，轉換為指定的TEnum型態, 成功回傳轉換之值，否則回傳該Enum型態之預設值 (一般為0)</summary>
		public static TEnum TryParseFromValueBy<TEnum>( Object value )
		{
			var result = default( TEnum );
			TryParseFromValueBy<TEnum>( ref result, value );
			return result;
		}

		/// <summary>嘗試從物件value轉換為指定的TEnum型態, 若成功則回傳true、並修改ref指向Enum之值, 否則回傳false、ref指向之值不變</summary>
		public static Boolean TryParseFromValueBy<TEnum>( ref TEnum originalValue, Object value )
		{
			var targetType = typeof( TEnum );
			try
			{
				value = Convert.ChangeType( value, Enum.GetUnderlyingType( targetType ) );

				var defined = Enum.IsDefined( targetType, value );
				if ( !defined ) return false;
				var newValue = (TEnum) Enum.Parse( targetType, value.ToString() );
				originalValue = newValue;
				return true;
			}
			catch ( Exception )
			{
				return false;
			}
		}

		/// <summary>嘗試從字串值解析取得目前Enum型別, 可選參數defaultType若有指定, 則會在從字串值轉換失敗時, 帶回預設值</summary>
		/// <typeparam name="TEnum">目前欲解析的Enum型別</typeparam>
		/// <param name="originalValue">原始的Enum值 (By Reference)</param>
		/// <param name="value">準備解析的字串值</param>
		/// <param name="defaultValue">若由字串值轉換失敗時, 將設定為此預設值</param>
		/// <returns>回傳由字串值解析是否成功</returns>
		public static Boolean TryParseFromValueBy<TEnum>( ref TEnum originalValue, Object value, TEnum? defaultValue = null ) where TEnum : struct
		{
			try
			{
				var result = default( TEnum );
				var success = TryParseFromValueBy<TEnum>( ref result, value );
				if ( !success )
				{
					if ( defaultValue != null ) originalValue = defaultValue.Value;
					return false;
				}

				originalValue = result;
				return true;
			}
			catch ( Exception )
			{
				return false;
			}
		}
	}
}
