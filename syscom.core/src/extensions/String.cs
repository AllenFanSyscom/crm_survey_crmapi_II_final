using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using syscom;

namespace System
{
	public enum StringCountMode
	{
		Default = 0,
		Unicode2Bytes
	}

	public static partial class StringExtensions
	{
		//============================================================================================================
		/// <summary>安全的Trim, 若該值為null則回傳 String.Empty</summary>
		public static String TrimSafely( this String value ) { return String.IsNullOrEmpty( value ) ? String.Empty : value.Trim(); }

		public static String RemoveNewLine( this String value ) { return String.IsNullOrEmpty( value ) ? String.Empty : value.Replace( "\n", "" ); }


		//============================================================================================================ 長度
		/// <summary>
		/// 限制顯示最長長度
		/// <para>1. 若為空值或空白，回傳String.Empty</para>
		/// </summary>
		public static String SubstringMaxBy( this String value, Int32 totlaLength, StringCountMode countMode = StringCountMode.Default )
		{
			if ( String.IsNullOrEmpty( value ) ) return String.Empty;

			if ( countMode == StringCountMode.Default )
			{
				return value.Length <= totlaLength ? value : value.Substring( 0, totlaLength );
			}
			else
			{
				var count = 0;
				for ( var idx = 0; idx < value.Length; idx++ )
				{
					var c = value[idx];
					count += c >= '\u4E00' && c <= '\u9FFF' || c >= '\uFF00' ? 2 : 1;
					if ( count >= totlaLength ) return value.Substring( 0, idx + 1 );
				}

				return value;
			}
		}

		/// <summary>移除所有Unicode字元(只保留Ascii)</summary>
		public static String RemoveUnicodeCharacters( this String value ) { return Text.RegularExpressions.Regex.Replace( value, @"[^\u0000-\u007F]", String.Empty ); }

		/// <summary>計算並回傳總長度, Ascii為1碼, Unicode為2碼</summary>
		public static Int32 GetUnicode2ByteLength( this String value )
		{
			var totalBytes = 0;
			for ( var idx = 0; idx < value.Length; idx++ )
			{
				var c = value[idx];
				totalBytes += c >= '\u4E00' && c <= '\u9FFF' || c >= '\uFF00' ? 2 : 1;
			}

			return totalBytes;
		}

		//============================================================================================================ 轉型
		/// <summary>取得Base64編碼的Bytes</summary>
		public static Byte[] ToBase64Bytes( this String value )
		{
			if ( String.IsNullOrEmpty( value ) ) throw Err.Extension( "輸入格式錯誤" );
			return Convert.FromBase64String( value );
		}

		/// <summary>轉換成目標字串編碼</summary>
		public static String ConvertEncodingBy( this String value, Encoding source, Encoding target )
		{
			if ( String.IsNullOrEmpty( value ) || source == null || target == null ) throw Err.Extension( "輸入格式錯誤" );
			var bytes = Encoding.Convert( source, target, source.GetBytes( value ) );
			return target.GetString( bytes );
		}

		/// <summary>轉換成目標編碼</summary>
		public static Byte[] ConvertEncodingBy( this Byte[] value, Encoding source, Encoding target )
		{
			if ( value == null || source == null || target == null ) throw Err.Extension( "輸入格式錯誤" );
			return Encoding.Convert( source, target, value );
		}

		//============================================================================================================ Hex

		/// <summary>轉換成Hex型字串 ( 01-02-03-04 )</summary>
		public static String ToHexString( this IEnumerable<Byte> bytes )
		{
			if ( bytes == null ) throw Err.Extension( "輸入格式錯誤" );
			return BitConverter.ToString( bytes.ToArray() );
		}

		/// <summary>依照傳入Encoding轉換成Hex型字串 ( 01-02-03-04 )</summary>
		public static String ToHexString( this String currentString, Encoding encoding )
		{
			if ( String.IsNullOrEmpty( currentString ) ) throw Err.Extension( "輸入格式錯誤" );
			var b = encoding.GetBytes( currentString );
			var sb = new StringBuilder();
			for ( var idx = 0; idx < b.Length; idx++ )
			{
				var Byte = b[idx];
				var format = ( b[idx] < 0x10 ? "0" : "" ) + "{0:X} ";
				sb.AppendFormat( format, Byte );
			}

			return sb.ToString();
		}

		/// <summary>Hex型字串轉換成Bytes</summary>
		public static Byte[] HexToBytes( this String currentHex )
		{
			if ( String.IsNullOrWhiteSpace( currentHex ) ) throw new ArgumentNullException( "輸入參數不得為空值" );
			try
			{
				var strings = currentHex.Split( new Char[] { '-' }, StringSplitOptions.RemoveEmptyEntries );
				var raw = new Byte[strings.Length];
				for ( var idx = 0; idx < strings.Length; idx++ )
				{
					var byteString = strings[idx];
					raw[idx] = Convert.ToByte( byteString, 16 );
				}

				return raw;
			}
			catch ( Exception ex )
			{
				throw new ArgumentException( "輸入格式錯誤", ex );
			}
		}

		/// <summary>Hex型字串轉換成原始字串</summary>
		public static String HexToString( this String currentHex, Encoding encoding )
		{
			if ( String.IsNullOrEmpty( currentHex ) || encoding == null ) throw Err.Extension( "輸入格式錯誤" );
			var hexs = currentHex.HexToBytes();
			return encoding.GetString( hexs );
		}

		/// <summary>移除空白</summary>
		public static String RemoveEmptys( this String stringSource )
		{
			if ( String.IsNullOrEmpty( stringSource ) ) return String.Empty;
			return stringSource.Replace( " ", "" );
		}


		//============================================================================================================ 切割並轉換
		/// <summary>將鍵/值配對的字串轉為字典型態 ( ex: name=Alice;age=22;sex=g; )</summary>
		public static Dictionary<String, String> SplitThenToDictionary( this String keyValue, String outerSeparator = ";", String pairSeparator = "=" )
		{
			return keyValue.Split( new[] { outerSeparator }, StringSplitOptions.RemoveEmptyEntries )
			               .Select( part => part.Split( new[] { pairSeparator }, StringSplitOptions.None ) )
			               .ToDictionary( split => split[0], split => split[1] );
		}
	}
}