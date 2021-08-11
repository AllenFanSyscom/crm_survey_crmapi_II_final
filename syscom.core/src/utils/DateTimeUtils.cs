using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace syscom
{
	public static class DateTimeUtils
	{
		/// <summary>轉換DateTime為16位的字串格式(yyyyMMddHHmmssff)【注意, 若再次轉換回DateTime, Milliseconds精度會遺失】</summary>
		public static String Get16CharsDateTime( this DateTime datetime ) { return datetime.ToString( "yyyyMMddHHmmssff" ); }

		/// <summary>轉換DateTime為8位的字串格式(yyyyMMdd)【注意, 若再次轉換回DateTime, Milliseconds精度會遺失】</summary>
		public static String Get8CharsDate( this DateTime datetime ) { return datetime.ToString( "yyyyMMdd" ); }

		/// <summary>轉換DateTime為8位的字串格式(HHmmssff)【注意, 若再次轉換回DateTime, Milliseconds精度會遺失】</summary>
		public static String Get8CharsTime( this DateTime datetime ) { return datetime.ToString( "HHmmssff" ); }


		/// <summary>由各8位的字串格式 date(yyyyMMdd) time(HHmmssff) 轉換回DateTime【注意, 轉換回DateTime, Milliseconds精度會遺失】</summary>
		public static DateTime GetDateTimeBy( String date8chars, String time8chars )
		{
			if ( date8chars.Length != 8 ) throw new ArgumentException( "日期長度必需為8碼-yyyyMMdd" );
			if ( time8chars.Length != 8 ) throw new ArgumentException( "時間長度必需為8碼-HHmmssff" );

			try
			{
				var year = Int32.Parse( date8chars.Substring( 0, 4 ) );
				var month = Int32.Parse( date8chars.Substring( 4, 2 ) );
				var day = Int32.Parse( date8chars.Substring( 6, 2 ) );

				var hour = Int32.Parse( time8chars.Substring( 0, 2 ) );
				var minute = Int32.Parse( time8chars.Substring( 2, 2 ) );
				var second = Int32.Parse( time8chars.Substring( 4, 2 ) );
				var millisecond = Int32.Parse( time8chars.Substring( 6, 2 ) );

				var datetime = new DateTime( year, month, day, hour, minute, second, millisecond * 10 );
				return datetime;
			}
			catch ( Exception ex )
			{
				throw new InvalidCastException( "無法轉換8碼日期+8碼時間為DateTime, 輸入資料Date[ " + date8chars + " ] Time[ " + time8chars + " ]", ex );
			}
		}

		/// <summary>由各8位的字串格式 date(yyyyMMdd) time(HHmmssff) 轉換回DateTime【注意, 轉換回DateTime, Milliseconds精度會遺失】</summary>
		public static DateTime? TryGetDateTimeFrom8CharsFormatBy( String date8chars, String time8chars )
		{
			if ( String.IsNullOrWhiteSpace( date8chars ) || String.IsNullOrWhiteSpace( time8chars ) ) return null;
			try
			{
				return GetDateTimeBy( date8chars, time8chars );
			}
			catch
			{
				return null;
			}

			//get { return ( String.IsNullOrWhiteSpace( FileBeginDate ) || String.IsNullOrWhiteSpace( FileBeginTime ) ) ? (DateTime?)null : DateTimeUtils.GetDateTimeBy( FileBeginDate, FileBeginTime ); }
			//set { if ( !value.HasValue ) return; FileBeginDate = value.Value.Get8CharsDate(); FileBeginTime = value.Value.Get8CharsTime(); }
		}

		/// <summary>由DateTime格式寫入為各8位的字串格式 date(yyyyMMdd) time(HHmmssff)【注意, 轉換回DateTime, Milliseconds精度會遺失】</summary>
		public static void TrySetDateTimeTo8CharsFormatBy<TClass, TProperty>( this TClass target, DateTime? source, Expression<Func<TClass, TProperty>> selectDateExpr, Expression<Func<TClass, TProperty>> selectTimeExpr )
			where TClass : class
		{
			if ( !source.HasValue ) return;
			var date8chars = source.Value.Get8CharsDate();
			var time8chars = source.Value.Get8CharsTime();

			Action<Expression<Func<TClass, TProperty>>, String> act = ( expr, value ) =>
			{
				var bodyExpression = expr.Body;
				var memberExpression = bodyExpression as MemberExpression;
				if ( memberExpression == null ) throw new Exception( "錯誤的Lambda指定, 只能指定到屬性上" );
				var property = memberExpression.Member as PropertyInfo;
				if ( property == null ) throw new Exception( "錯誤的Lambda指定, 只能指定到屬性上" );
				property.SetValue( target, value );
			};

			act( selectDateExpr, date8chars );
			act( selectTimeExpr, time8chars );
		}
	}
}
