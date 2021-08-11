using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using syscom;

namespace System
{
	public static class RandomExtensions
	{
		static String _EnglishAndNumber = "abcdefghijklmnopqrstuvwxyz0123456789";


		/// <summary>依指定的文字產生亂數字元</summary>
		public static String GenerateString( this Random random, String allowStrings, Int32 length )
		{
			var builder = new StringBuilder();
			for ( var i = 0; i < length; i++ )
			{
				var _char = _EnglishAndNumber[random.Next( 0, _EnglishAndNumber.Length )];
				builder.Append( _char );
			}

			return builder.ToString();
		}

		/// <summary>產生亂數字元, (a-z與0-9)</summary>
		public static String GenerateString( this Random random, Int32 length ) { return random.GenerateString( _EnglishAndNumber, length ); }
	}
}