using System;
using System.Linq;

public static class Int32Extensions
{
	public static Byte[] ToBigEndianBytes<T>( this Int32 source )
	{
		Byte[] bytes;

		var type = typeof( T );
		if ( type == typeof( UInt16 ) ) bytes = BitConverter.GetBytes( (UInt16) source );
		else if ( type == typeof( UInt64 ) ) bytes = BitConverter.GetBytes( (UInt64) source );
		else if ( type == typeof( Int32 ) ) bytes = BitConverter.GetBytes( source );
		else throw new InvalidCastException( "Cannot be cast to T" );

		if ( BitConverter.IsLittleEndian ) Array.Reverse( bytes );
		return bytes;
	}

	public static Int32 ToLittleEndianInt( this Byte[] source )
	{
		if ( BitConverter.IsLittleEndian ) Array.Reverse( source );

		if ( source.Length == 2 ) return BitConverter.ToUInt16( source, 0 );
		if ( source.Length == 8 ) return (Int32) BitConverter.ToUInt64( source, 0 );

		throw new ArgumentException( "Unsupported Size" );
	}
}