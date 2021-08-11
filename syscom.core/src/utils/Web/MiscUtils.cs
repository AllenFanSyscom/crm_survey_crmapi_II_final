using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace syscom
{
	/// <summary>
	///
	/// </summary>
	public static class MiscUtils
	{
		#region Unit Test Sample,將字串陣列合併成字串

		/// <summary>
		/// Unit Test Sample,將字串陣列合併成字串
		/// </summary>
		/// <param name="stringArray">字串陣列</param>
		/// <returns>合併成一個字串</returns>
		public static String? CombineArrayStringWithSpace( String[] stringArray )
		{
			//initialized string to NULL
			var str = default( String );
			foreach ( var item in stringArray ) str += item + " ";
			return str.Trim();
		}

		#endregion

		#region 轉換成中文貨幣

		/// <summary>
		/// 轉換成中文貨幣
		/// </summary>
		/// <param name="num">double輸入</param>
		/// <returns>貨幣字串</returns>
		public static String TurnToChinaMoney( Double num ) { return TurnToChinaMoney( Decimal.Parse( num.ToString() ) ); }

		/// <summary>
		/// 轉換成中文貨幣
		/// </summary>
		/// <param name="num">int輸入</param>
		/// <returns>貨幣字串</returns>
		public static String TurnToChinaMoney( Int32 num ) { return TurnToChinaMoney( Decimal.Parse( num.ToString() ) ); }

		/// <summary>
		/// 轉換成中文貨幣
		/// </summary>
		/// <param name="num">long輸入</param>
		/// <returns>貨幣字串</returns>
		public static String TurnToChinaMoney( Int64 num ) { return TurnToChinaMoney( Decimal.Parse( num.ToString() ) ); }

		/// <summary>
		/// 轉換成中文貨幣
		/// </summary>
		/// <param name="num">float輸入</param>
		/// <returns>貨幣字串</returns>
		public static String TurnToChinaMoney( Single num ) { return TurnToChinaMoney( Decimal.Parse( num.ToString() ) ); }

		/// <summary>
		/// 轉換成中文貨幣
		/// </summary>
		/// <param name="number">decimal輸入</param>
		/// <returns>貨幣字串</returns>
		public static String TurnToChinaMoney( Decimal number )
		{
			// 格式化数字为两位小数的,带有位标志的数字,正数、负数和零分别对应分号隔开的格式
			var result = number.ToString( "#穰'.'#仟#佰#拾#秭'.'#仟#佰#拾#垓'.'#仟#佰#拾#京'.'#仟#佰#拾#兆'.'#仟#佰#拾#億'.'#仟#佰#拾#萬'.'#仟#佰#拾#元.0角0分;負#穰'.'#仟#佰#拾#秭'.'#仟#佰#拾#垓'.'#仟#佰#拾#京'.'#仟#佰#拾#兆'.'#仟#佰#拾#億'.'#仟#佰#拾#萬'.'#仟#佰#拾#元.0角0分;零元" );
			// 从字符串左侧开始替换子字符串，遇到汉字“零”或者阿拉伯数字 0 - 9，替换就结束。
			// 替换内容是捕获组 $1，该捕获组表示 0 个或 1 个在字符串最左侧的汉字“负”
			result = Regex.Replace( result, @"^(負?)[^零\d]*", "$1" );
			// 替换字符串中以一个 0 开头，后跟一个字符，这种组合的连续。
			// 因为肯定不会出现两个阿拉伯数字连续的情况，所以后跟字符确定为汉字。
			// 规定该汉字不可以匹配汉字“元”。也不可以匹配右侧带“.”的汉字（特殊单位）。
			// 将所有找到的匹配替换为“0”，注意：结果可能会产生“0”的连续的情况。
			result = Regex.Replace( result, @"(0[^元](?!\.))+", "0" );
			// 删除字符串最右侧的，或右侧带“.”的汉字左侧的，一个或多个“0”的连续。
			// 所有特殊单位的右侧带“.”，例如“億”“萬”“元”，这保证了上句不会替换它们。
			result = Regex.Replace( result, @"0+(\D\.|$)", "$1" );
			// 替换所有以“.”开头的，一个汉字和一个“.”的组合，这是特殊单位左侧的个十百千位都是 0 造成的。
			// 所以删除该特殊单位的字符，左侧会有一个“0”和一个汉字的组合，该组合会包含被删掉的特殊单位的含义。
			result = Regex.Replace( result, @"(?<=\.)[^元]\.", "" );
			// 删除掉所有“.”。
			result = Regex.Replace( result, @"\.", "" );
			// 如果字符串最后以“元”结束，就换成“元整”。
			result = Regex.Replace( result, @"(元)$", "$1整" );
			// 用匿名方法作代理，将每个数字替换成汉字形式。
			result = Regex.Replace( result, @"\d", delegate( Match m ) { return "零壹貳參肆伍陸柒捌玖"[m.Value[0] - '0'].ToString(); } );

			return result;
		}

		#endregion 轉換成中文貨幣
	}
}
