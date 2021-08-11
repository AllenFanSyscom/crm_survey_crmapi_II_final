using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace syscom
{
	public static class NetUtils
	{
		public static Boolean Ping( String addressOrName )
		{
			var pingable = false;
			Ping? pinger = null;
			try
			{
				pinger = new Ping();
				var reply = pinger.Send( addressOrName );
				pingable = reply.Status == IPStatus.Success;
			}
			catch ( PingException )
			{
				// Discard PingExceptions and return false;
			}
			finally
			{
				pinger?.Dispose();
			}

			return pingable;
		}

		/// <summary>檢查當前網路狀態是否正常</summary>
		public static Boolean IsNetworkAvailable()
		{
			return NetworkInterface.GetIsNetworkAvailable();
			//return syscom.core.Utilities.Win32.Wininet.GetInternetIsConnected();
		}

		/// <summary>檢查當前網路狀態是否正常, 傳入最小速度, 有網路超過這個速度即回傳true，否則回傳false (可帶入0表示任意速度)</summary>
		public static Boolean IsNetworkAvailable( Int64 minimumSpeed )
		{
			if ( !IsNetworkAvailable() ) return false;

			foreach ( var ni in NetworkInterface.GetAllNetworkInterfaces() )
			{
				// discard because of standard reasons
				if ( ni.OperationalStatus != OperationalStatus.Up || ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel ) continue;

				// this allow to filter modems, serial, etc.
				// I use 10000000 as a minimum speed for most cases
				if ( ni.Speed < minimumSpeed ) continue;

				// discard virtual cards (virtual box, virtual pc, etc.)
				if ( ni.Description.IndexOf( "virtual", StringComparison.OrdinalIgnoreCase ) >= 0 || ni.Name.IndexOf( "virtual", StringComparison.OrdinalIgnoreCase ) >= 0 ) continue;

				// discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
				if ( ni.Description.Equals( "Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase ) ) continue;

				return true;
			}

			return false;
		}

		//========================================================================================================================
		// IP Address
		//========================================================================================================================
		/// <summary>取得當前第一個IPv4的IPAddress,若取得不到將傳回Null (多張網卡請使用IPV4s)</summary>
		/// <returns></returns>
		public static IPAddress GetCurrentIPv4() { return Dns.GetHostAddresses( Dns.GetHostName() ).FirstOrDefault( p => p.AddressFamily == AddressFamily.InterNetwork ); }

		/// <summary>取得當前第一個IPv4的IPAddress,若取得不到將傳回Null (多張網卡請使用IPV4s)</summary>
		/// <returns></returns>
		public static List<IPAddress> GetCurrentIPv4s() { return Dns.GetHostAddresses( Dns.GetHostName() ).Where( p => p.AddressFamily == AddressFamily.InterNetwork ).ToList(); }

		public static String GetCurrentIPv4sString()
		{
			try
			{
				return GetCurrentIPv4s().Select( ip => ip.ToString() ).ToArray().JoinByComma();
			}
			catch ( Exception )
			{
				return Dns.GetHostName();
			}
		}

		/// <summary>將指定IP及Port轉換為IPEndPoint, 若轉換錯誤會拋出異常</summary>
		public static IPEndPoint ParseToIPEndPointOrExceptionBy( String ip, Int32 port )
		{
			IPAddress? address = null;
			if ( !IPAddress.TryParse( ip, out address ) ) throw new InvalidCastException( "無法將輸入IP[ " + ip + " ]轉換為有效的IPAddress, 請檢查參數是否正確" );
			return new IPEndPoint( address, port );
		}

		//========================================================================================================================
		// Logs
		//========================================================================================================================
		const String _HR = "--------------------------------------------------------------------------------------";

		public static String GetNetworkAvailabilityMessage( Object sender, NetworkAvailabilityEventArgs e )
		{
			var now = DateTime.Now;
			var buffer = new StringBuilder();
			buffer.AppendLine( _HR );
			buffer.AppendLine( "➜ NetworkAvailability Changed - " + now );
			buffer.AppendLine( "Change To: " + e.IsAvailable );
			buffer.AppendLine( _HR );
			return buffer.ToString();
		}

		public static String GetNetworkInterfaceDetailMessage( Boolean addDateTime = false )
		{
			var now = DateTime.Now;
			var nowString = addDateTime ? " - " + now : "";
			var cards = NetworkInterface.GetAllNetworkInterfaces().ToList();
			var buffer = new StringBuilder();
			buffer.AppendLine( _HR );
			buffer.AppendLine( "➜ NetworkInterface Messages ( " + cards.Count + " ) " + nowString );
			buffer.AppendLine( _HR );
			for ( var idx = 0; idx < cards.Count; idx++ )
			{
				var card = cards[idx];
				buffer.AppendLine( "Interface No[" + ( idx + 1 ) + "] " + card.ToJson() );
				//buffer.AppendLine( "----------" );

				var address = card.GetIPProperties().UnicastAddresses;

				var cidx = 0;
				foreach ( var addr in address ) buffer.AppendFormat( "[{0}] {1}\n", cidx++, addr.ToJson() );

				//var count = 0;
				//foreach ( var addr in card.GetIPProperties().UnicastAddresses )
				//{
				//	count++;
				//	buffer.AppendLine
				//	(
				//		String.Format
				//		(
				//			"[" + count + "] IP[{0}]{1}",
				//			addr.Address,
				//			( addDateTime ? ( " ( lease expires: " + ( DateTime.Now + new TimeSpan( 0, 0, (int)addr.DhcpLeaseLifetime ) ).ToString( "yyyy-MM-dd HH:mm:ss.ffff" ) + " )" ) : "" )

				//		)
				//	);
				//}
				buffer.AppendLine( "-----------------------------------" );
			}

			buffer.Append( _HR );
			return buffer.ToString();
		}

		//========================================================================================================================
		// Ports
		//========================================================================================================================
		/// <summary>取得指定範圍內, 可使用的Port</summary>
		public static Int32 GetFreePortBy( Int32 min = 1000, Int32 max = 65535 )
		{
			if ( min < 1000 ) throw new ArgumentException( "最小值不得低於1000" );
			if ( max > 65535 ) throw new ArgumentException( "最大值不得大於65535" );
			var properties = IPGlobalProperties.GetIPGlobalProperties();
			var tcpEndPoints = properties.GetActiveTcpListeners();

			var usedPorts = tcpEndPoints.Select( p => p.Port ).ToList();
			for ( var port = min; port < max; port++ )
			{
				if ( usedPorts.Contains( port ) ) continue;
				return port;
			}

			throw new SystemException( "無法取得可使用的Port, 系統異常" );
		}

		/// <summary>取得可使用的Port</summary>
		public static Int32 GetFreePort()
		{
			using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
			{
				socket.Bind( new IPEndPoint( IPAddress.Parse( "127.0.0.1" ), 0 ) );
				var endpoint = (IPEndPoint) socket.LocalEndPoint;
				return endpoint.Port;
			}
		}
	}
}
