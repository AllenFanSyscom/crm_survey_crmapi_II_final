using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace syscom.utils.win32
{
	[Flags]
	public enum ConnectionStates : int
	{
		/// <summary>Local system uses a modem to connect to the Internet.</summary>
		Modem = 0x1,

		/// <summary>Local system uses a local area network to connect to the Internet.</summary>
		Lan = 0x2,

		/// <summary>Local system uses a proxy server to connect to the Internet.</summary>
		Proxy = 0x4,

		/// <summary>No longer used.</summary>
		ModemBusy = 0x08,

		/// <summary>Local system has RAS installed.</summary>
		RasInstalled = 0x10,

		/// <summary>Local system is in offline mode.Local system is in offline mode.</summary>
		Offline = 0x20,

		/// <summary>Local system has a valid connection to the Internet, but it might or might not be currently connected.</summary>
		Configured = 0x40,

		Modem_Configured_RasInstalled = Modem | Configured | RasInstalled,

		Lan_RasInstalled = Lan | RasInstalled
		//INTERNET_CONNECTION_LAN (0x02) and INTERNET_CONNECTION_RAS_INSTALLED (0x10)
	}

	public class Wininet
	{
		[DllImport( "wininet.dll", SetLastError = true )]
		public static extern Boolean InternetGetConnectedState( out ConnectionStates lpdwFlags, Int32 dwReserved );

		public static ConnectionStates GetInternetConnectedState()
		{
			ConnectionStates flags;
			var isConnected = InternetGetConnectedState( out flags, 0 );

			return flags;
		}

		public static Boolean GetInternetIsConnected()
		{
			if( EnvUtils.IsUnixBasePlatform ) throw new NotSupportedException( "Only Support Windows Platform" );

			ConnectionStates flags;
			var isConnected = InternetGetConnectedState( out flags, 0 );

			if ( isConnected == false )
			{
				var errorCode = Marshal.GetLastWin32Error();
				Debug.WriteLine( "[LastWin32Error] ErrorCode[" + errorCode + "]" );
			}

			return isConnected;
		}
	}
}
