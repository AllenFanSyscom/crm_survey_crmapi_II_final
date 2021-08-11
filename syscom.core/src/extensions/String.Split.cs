using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using syscom;

namespace System
{
	public static partial class StringExtensions
	{
		//============================================================================================================ 切割

		/// <summary>Split Items by "," - RazgrizHsu</summary>
		/// 以","符號作為分割字串的基準
		public static String[] SplitByComma( this String value ) { return SplitBy( value, SepatatorComma ); }

		/// <summary>Split Items by "." - RazgrizHsu</summary>
		public static String[] SplitByPeriod( this String value ) { return SplitBy( value, SepatatorPeriod ); }

		//Semicolon
		/// <summary>Split Items by ";" - RazgrizHsu</summary>
		/// 以";"符號作為分割字串的基準
		public static String[] SplitBySemicolon( this String value ) { return SplitBy( value, SepatatorSemicolon ); }

		/// <summary>Split Items by "(=.=)" - RazgrizHsu</summary>
		/// 以"(=.=)"符號作為分割字串的基準
		public static String[] SplitByFace( this String value ) { return SplitBy( value, SepatatorFace ); }

		/// <summary>依指定字元進行切割</summary>
		/// 以指定的字元作為分割字串的基準
		public static String[] SplitBy( this String value, String separator ) { return SplitBy( value, separator, StringSplitOptions.RemoveEmptyEntries ); }

		/// <summary>依指定字元進行切割, 指定StringSplitOptions(RemoveEmptyEntries: 分割後空字串不會加入; None: 分割後空字串加入)</summary>
		public static String[] SplitBy( this String value, String separator, StringSplitOptions splitOptions )
		{
			if ( String.IsNullOrEmpty( value ) ) return new String[] { };
			return value.Split( new String[] { separator }, splitOptions );
		}

		/// <summary>計算由 "," 所組合的字串之元素數量</summary>
		public static Int32 SplitCountByCpmma( this String value ) { return SplitCountBy( value, SepatatorComma ); }

		/// <summary>計算由 "." 所組合的字串之元素數量</summary>
		public static Int32 SplitCountByPeriod( this String value ) { return SplitCountBy( value, SepatatorPeriod ); }

		/// <summary>計算由 ";" 所組合的字串之元素數量</summary>
		public static Int32 SplitCountBySemicolon( this String value ) { return SplitCountBy( value, SepatatorSemicolon ); }

		/// <summary>計算由 "(=.=)" 所組合的字串之元素數量</summary>
		public static Int32 SplitCountByRazgrizHsu( this String value ) { return SplitCountBy( value, SepatatorFace ); }

		/// <summary>計算依指定字元進行切割的總數量, 指定StringSplitOptions</summary>
		public static Int32 SplitCountBy( this String value, String separator ) { return SplitCountBy( value, separator, StringSplitOptions.RemoveEmptyEntries ); }

		/// <summary>計算依指定字元進行切割的總數量, 指定StringSplitOptions</summary>
		public static Int32 SplitCountBy( this String value, String separator, StringSplitOptions splitOptions )
		{
			if ( String.IsNullOrEmpty( value ) ) return 0;
			return value.Split( new String[] { separator }, splitOptions ).Length;
		}


		//============================================================================================================ 組合
		public const String SepatatorComma = ",";
		public const String SepatatorPeriod = ".";
		public const String SepatatorSemicolon = ";";
		public const String SepatatorFace = "(=.=)";

		/// <summary>Join Items By "(=.=)" - RazgrizHsu</summary>
		/// 在字串之間加入"(=.=)"
		public static String JoinByFace( this IList<String> values ) { return JoinBy( values, SepatatorFace ); }

		/// <summary>Join Items By "," - RazgrizHsu</summary>
		/// 在字串之間加入","
		public static String JoinByComma( this IList<String> values ) { return JoinBy( values, SepatatorComma ); }

		/// <summary>Join Items By "." - RazgrizHsu</summary>
		/// 在字串之間加入"."
		public static String JoinByPeriod( this IList<String> values ) { return JoinBy( values, SepatatorPeriod ); }

		/// <summary>Join Items By ";" - RazgrizHsu</summary>
		/// 在字串之間加入";"
		public static String JoinBySemicolon( this IList<String> values ) { return JoinBy( values, SepatatorSemicolon ); }

		/// <summary>Join Items By NewLine</summary>
		public static String JoinByNewLine( this IList<String> values ) { return JoinBy( values, Environment.NewLine ); }

		/// <summary>依指定字串進行String.Join</summary>
		/// 在字串之間加入指定字串
		public static String JoinBy( this IList<String> values, String separator )
		{
			if ( values == null ) throw Err.Extension( "輸入格式錯誤" );
			if ( values.Count <= 0 ) return null;
			return String.Join( separator, values );
		}
	}
}