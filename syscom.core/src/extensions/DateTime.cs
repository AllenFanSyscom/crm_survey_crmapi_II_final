using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace System
{
	public static class DateTimeExtensions
	{
		public static DateTime BaseDateTimeSince19700101 = new DateTime( 1970, 1, 1, 0, 0, 0 );

		/// <summary>取得自1970.01.01 00:00:00 UTC至此的總秒數。
		/// UNIX及Linux的時間系統是由「新紀元時間」Epoch開始計算起，單位為秒，Epoch則是指定為1970年一月一日凌晨零點零分零秒，格林威治時間。  
		/// <para>like in java System.currentTimeMillis() and like Flash Date.getTime()</para>
		/// <para>2010-01-07 am03:21 - RazgrizHsu</para></summary>
		public static Int64 CurrentTimeMillis( this DateTime dateTime ) { return ( DateTime.UtcNow - BaseDateTimeSince19700101 ).Ticks / 10000; }

		/// <summary>取得yyyy-MM-dd HH:mm:ss格式字串</summary>
		public static String ToFullDateTime( this DateTime datetime ) { return datetime.ToString( "yyyy-MM-dd HH:mm:ss" ); }

		/// <summary>有值則傳回yyyy-MM-dd HH:mm:ss格式,無則傳回String.Empty</summary>
		public static String ToFullDateTimeOrEmpty( this Nullable<DateTime> datetime ) { return datetime.HasValue ? datetime.Value.ToString( "yyyy-MM-dd HH:mm:ss" ) : String.Empty; }


		/// <summary>有值則傳回yyyy-MM-dd HH:mm:ss格式,無則傳回輸入字串</summary>
		public static String ToFullDateTimeOr( this Nullable<DateTime> datetime, String defaultValue ) { return datetime.HasValue ? datetime.Value.ToString( "yyyy-MM-dd HH:mm:ss" ) : defaultValue; }

		/// <summary>取得yyyy-MM-dd格式字串</summary>
		public static String ToFullDate( this DateTime datetime ) { return datetime.ToString( "yyyy-MM-dd" ); }

		/// <summary>有值則傳回yyyy-MM-dd格式,無則傳回String.Empty</summary>
		public static String ToFullDateOrEmpty( this Nullable<DateTime> datetime ) { return datetime.HasValue ? datetime.Value.ToString( "yyyy-MM-dd" ) : String.Empty; }


		/// <summary>有值則傳回yyyy-MM-dd格式,無則傳回輸入字串</summary>
		public static String ToFullDateOr( this Nullable<DateTime> datetime, String defaultValue ) { return datetime.HasValue ? datetime.Value.ToString( "yyyy-MM-dd" ) : defaultValue; }


		/// <summary>試轉型為DateTime後取得yyyy-MM-dd格式字串, 若失敗則傳回String.Empty</summary>
		public static String ToFullDateString( this String dateTimeString ) { return dateTimeString.ToFullDateString( String.Empty ); }

		/// <summary>試轉型為DateTime後取得yyyy-MM-dd格式字串, 若失敗則傳回defaultValue</summary>
		public static String ToFullDateString( this String dateTimeString, String defaultValue )
		{
			try
			{
				var dt = DateTime.Parse( dateTimeString );
				return dt.ToFullDate();
			}
			catch { return defaultValue; }
		}

		/// <summary>試轉型為DateTime後取得yyyy-MM-dd HH:mm:ss格式字串, 若失敗則傳回String.Empty</summary>
		public static String ToFullDateTimeString( this String dateTimeString ) { return dateTimeString.ToFullDateTimeString( String.Empty ); }

		/// <summary>試轉型為DateTime後取得yyyy-MM-dd HH:mm:ss格式字串, 若失敗則傳回defaultValue</summary>
		public static String ToFullDateTimeString( this String dateTimeString, String defaultValue )
		{
			try
			{
				var dt = DateTime.Parse( dateTimeString );
				return dt.ToFullDateTime();
			}
			catch { return defaultValue; }
		}


		/// <summary>
		/// support date time format
		/// </summary>
		/// <example>
		///     var tdate = "2013-01-23".ToTaiwanDate();
		///     var tdate2 = "2013-01-23".ToTaiwanDate("yyyy-MM-dd");
		/// </example>
		public static String ToTaiwanDate( this String dateTimeString, String pattern = "yyyy-MM-dd" )
		{
			if ( String.IsNullOrEmpty( dateTimeString ) ) return String.Empty;
			return Convert.ToDateTime( dateTimeString ).ToTaiwanDateString( pattern );
		}


		/// <summary>
		/// 西元年轉民國年
		///  var tdate = DateTime.Now.ToTaiwanDate();
		///  var tdate2 = DateTime.Now.ToTaiwanDate("yyyy-MM-dd");
		/// </summary>
		public static String ToTaiwanDate( this DateTime dateTime, String format = "yyyy-MM-dd" )
		{
			if ( dateTime == DateTime.MinValue ) return String.Empty;
			return dateTime.ToTaiwanDateString( format );
		}

		/// <summary>
		/// 西元年轉民國年
		/// </summary>
		static String ToTaiwanDateString( this DateTime datetime, String format )
		{
			var info = new CultureInfo( "zh-TW" );

			var calendar = new TaiwanCalendar();

			info.DateTimeFormat.Calendar = calendar;

			String tmpString;

			if ( datetime.Year < 1912 )
			{
				var offsetYear = 1912 - datetime.Year;

				datetime = datetime.AddYears( offsetYear * 2 - 1 );

				tmpString = datetime.ToString( format, info );

				tmpString = "民國前" + tmpString;
			}
			else
			{
				tmpString = datetime.ToString( format, info );
			}

			return tmpString;
		}

		/// <summary>
		///  民國年轉西元年
		///  支援自定Pattern
		/// </summary>
		/// <example>
		///     var tdate = "1010229"; //民國年月日
		///     var dd = tdate.ToDateFromTaiwanDate();
		///     var ee = dd.ToString("yyyy-MM-dd");
		///     
		///     var tdate = "101-2-29";
		///     var dd = tdate.ToDateFromTaiwanDate("-");
		///     var ee = dd.ToString("yyyy-MM-dd");
		/// </example>
		/// <param name="taiwanDateString">例如 101/2/29</param>
		/// <param name="pattern">例如 yyyy/MM/dd</param>
		/// <returns></returns>
		public static DateTime ToDateFromTaiwanDate( this String taiwanDateString, String pattern = "" )
		{
			var dtNow = DateTime.MinValue;

			var format = String.Format( "yyyy{0}MM{0}dd", pattern );

			var newDateString = taiwanDateString;

			if ( taiwanDateString != null && taiwanDateString != String.Empty )
			{
				newDateString = taiwanDateString.Split( new String[] { pattern }, StringSplitOptions.RemoveEmptyEntries ).Aggregate( ( s1, s2 ) =>
				{
					return s1.PadLeft( 2, '0' ) + pattern + s2.PadLeft( 2, '0' );
				} );

				var info = new CultureInfo( "zh-TW" );

				var calendar = new TaiwanCalendar();

				info.DateTimeFormat.Calendar = calendar;

				dtNow = DateTime.ParseExact( newDateString.PadLeft( format.Length, '0' ), format, info );
			}

			return dtNow;
		}
	}
}