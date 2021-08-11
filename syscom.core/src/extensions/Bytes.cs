using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using syscom;

namespace System
{
	public static class BytesExtensions
	{
		public static String ToHexString( this Byte currentByte ) { return currentByte < 0x10 ? String.Concat( "0", currentByte.ToString( "X" ) ) : currentByte.ToString( "X" ); }

		public static String ToBase64String( this Byte[] value ) { return Convert.ToBase64String( value ); }

		public static String ToAsciiString( this Byte[] value ) { return Encoding.ASCII.GetString( value ); }

		/// <summary>移除Bytes尾端的零</summary>
		public static Byte[] RemoveRightZero( this Byte[] source, Int32 overCount = 1 )
		{
			var count = 0;
			var index = source.Length;
			while ( index > 0 && source[index - 1] == 0 )
			{
				index--;
				count++;
			}

			if ( count < overCount ) return source; //如果尾巴0x00不超過3組, 則不砍掉尾巴

			var newAry = new Byte[index];
			Buffer.BlockCopy( source, 0, newAry, 0, index );
			return newAry;
		}

		public static Byte[] ReverseBytesBy( String bytesString )
		{
			var length = ( bytesString.Length + 1 ) / 3;
			var results = new Byte[length];
			for ( var i = 0; i < length; i++ )
			{
				var sixteen = bytesString[3 * i];
				if ( sixteen > '9' ) sixteen = (Char) ( sixteen - 'A' + 10 );
				else sixteen -= '0';

				var ones = bytesString[3 * i + 1];
				if ( ones > '9' ) ones = (Char) ( ones - 'A' + 10 );
				else ones -= '0';

				results[i] = (Byte) ( 16 * sixteen + ones );
			}

			return results;
		}


		/// <summary>
		/// 取得BigEndian byte array所表示的int數值，targetIndex為指定該array的起始位置。
		/// 該array的大小不能小於targetIndex + 4。
		/// </summary>
		public static Int32 GetBigEndianIntBy( this Byte[] bytes, Int32 targetIndex )
		{
			var targetBytes = new Byte[] { bytes[targetIndex], bytes[targetIndex + 1], bytes[targetIndex + 2], bytes[targetIndex + 3] }.Reverse().ToArray();
			return BitConverter.ToInt32( targetBytes, 0 );
		}

		/// <summary>
		/// 取得byte array所表示的int數值，targetIndex為指定該array的起始位置。
		/// </summary>
		public static Int32 GetIntBy( this Byte[] bytes, Int32 targetIndex ) { return BitConverter.ToInt32( bytes, targetIndex ); }

		/// <summary>
		/// 取得byte array所表示的字串，targetIndex為指定該array的起始位置，valueLength為array指定長度，encoder為字串的編碼。
		/// </summary>
		public static String GetStringBy( this Byte[] bytes, Int32 targetIndex, Int32 valueLength, Encoding encoder )
		{
			try
			{
				return encoder.GetString( bytes, targetIndex, valueLength );
			}
			catch ( ArgumentOutOfRangeException aore )
			{
				throw Err.Extension( "與預期長度( " + valueLength + " )不符, 資料長度共有[ " + bytes.Length + " ], 但從位置[ " + targetIndex + " ]開始僅剩下[ " + ( bytes.Length - targetIndex ) + " ]", aore );
			}
		}

		/// <summary>
		/// 設置int數值為BigEndian byte array表示，value為指定數值，targetIndex為目標array的起始位置。
		/// 該array的大小不能小於targetIndex + 4。
		/// </summary>
		public static void SetBigEndianIntBy( this Byte[] bytes, Int32 value, Int32 targetIndex )
		{
			var intBytes = BitConverter.GetBytes( value );
			bytes.SetValueBy( intBytes.Reverse().ToArray(), targetIndex, 4 );
		}

		/// <summary>
		/// 設置int數值為byte array表示，value為指定數值，targetIndex為目標array的起始位置。
		/// 該array的大小不能小於targetIndex + 4。
		/// </summary>
		public static void SetIntBy( this Byte[] bytes, Int32 value, Int32 targetIndex )
		{
			var intBytes = BitConverter.GetBytes( value );
			bytes.SetValueBy( intBytes, targetIndex, 4 );
		}

		/// <summary>
		/// 設置字串為byte array表示，value為指定數值，targetIndex為目標array的起始位置，encoder為指定的字串編碼。
		/// 該array的大小不能小於targetIndex + 字串轉換後的array長度。
		/// </summary>
		public static void SetStringBy( this Byte[] bytes, String value, Int32 targetIndex, Encoding encoder )
		{
			var strBytes = encoder.GetBytes( value );
			bytes.SetValueBy( strBytes, targetIndex, strBytes.Length );
		}

		/// <summary>
		/// 設置字串為byte array表示，value為指定數值，targetIndex為目標array的起始位置，valueLength為轉換後的指定長度，encoder為指定的字串編碼。
		/// 該array的大小不能小於targetIndex + valueLength。
		/// </summary>
		public static void SetStringBy( this Byte[] bytes, String value, Int32 targetIndex, Int32 valueLength, Encoding encoder )
		{
			var strBytes = encoder.GetBytes( value );
			bytes.SetValueBy( strBytes, targetIndex, valueLength );
		}

		/// <summary>
		/// 設置array的值。value為來源array，targetIndex為目標array起始位置，valueLength為指定的長度。
		/// </summary>
		public static void SetValueBy( this Byte[] bytes, Byte[] value, Int32 targetIndex, Int32 valueLength ) { Buffer.BlockCopy( value, 0, bytes, targetIndex, valueLength ); }

		/// <summary>
		/// 複製array。target目標array，startIndex為要被複製的起始位置，count為指定的長度。
		/// </summary>
		public static void BlockCloneTo( this Byte[] bytes, ref Byte[] target, Int32 startIndex, Int32 count )
		{
			target = new Byte[count];
			Buffer.BlockCopy( bytes, startIndex, target, 0, count );
		}

		/// <summary>以自已的資料為主，把後面的Bytes加進去，取得新的Bytes</summary>
		public static Byte[] BlockAppendBy( this Byte[] bytes, ref Byte[] appendBytes )
		{
			var newBytes = new Byte[bytes.Length + appendBytes.Length];
			Buffer.BlockCopy( bytes, 0, newBytes, 0, bytes.Length );
			Buffer.BlockCopy( appendBytes, 0, newBytes, bytes.Length, appendBytes.Length );

			return newBytes;
		}

		/// <summary>以自已的資料為主，把後面的Bytes加進去，取得新的Bytes</summary>
		public static Byte[] BlockAppendBy( this Byte[] bytes, params Byte[][] arrays )
		{
			var rv = new Byte[bytes.Length + arrays.Sum( a => a.Length )];
			Buffer.BlockCopy( bytes, 0, rv, 0, bytes.Length );

			var offset = bytes.Length;
			foreach ( var array in arrays )
			{
				Buffer.BlockCopy( array, 0, rv, offset, array.Length );
				offset += array.Length;
			}

			return rv;
		}
	}
}