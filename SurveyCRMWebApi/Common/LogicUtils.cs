using System;

namespace SurveyCRMWebApiV2
{
	public static class LogicUtils
	{
		public const Int64 Maximum = 78364164095L;
		internal static readonly Char[] Chars =
		{
			// 48 - 57
			'0', '1', '2', '3', '4', '5', '6', '7', '8', '9',

			// 65 - 90
			'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
		};
		static LogicUtils() { }

		public static String ToStrSN( Int64 value )
		{
			if ( value >= Maximum ) throw new NotSupportedException( $"OnlySupport Maximum[{Maximum}] now[{value}]" );

			var i = 32;
			var buffer = new Char[i];
			var targetBase = Chars.Length;

			do
			{
				buffer[--i] = Chars[value % targetBase];
				value = value / targetBase;
			}
			while ( value > 0 );

			var result = new Char[32 - i];
			Array.Copy( buffer, i, result, 0, 32 - i );

			return new String( result ).PadLeft( 7, '0' );
		}

		public static Int64 ToIntSN( String strings )
		{
			if ( strings.Length >= 8 ) throw new NotSupportedException( "OnlySupport Maximum 7 code" );

			strings = strings.ToUpper();
			var total = 0L;

			for ( var idx = 0; idx < strings.Length; idx++ )
			{
				var c = strings[strings.Length - ( idx + 1 )];

				var rate = Math.Pow( Chars.Length, idx );
				var intV = (Int32) c;

				if ( intV >= 65 ) intV -= 55;      //offset A(65)-Z(90)
				else if ( intV >= 48 ) intV -= 48; //offset 0(48)-9(57)
				else throw new InvalidCastException( $"cannot using code[ {c} ]" );

				total += (Int64) ( intV * rate );
			}

			return total;
		}
	}
}
