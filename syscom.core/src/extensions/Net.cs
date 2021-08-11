using System;
using System.Linq;
using System.Net;

public static class NetExtensions
{

	public static Int64 ToLong( this IPAddress ip )
	{
		return BitConverter.ToInt64( ip.GetAddressBytes(), 0 );
	}
}