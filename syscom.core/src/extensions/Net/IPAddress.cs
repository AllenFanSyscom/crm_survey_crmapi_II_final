using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace System.Net
{
	public static class IPAddressExtensions
	{
		/// <summary>從IPAddress實體判斷是否為IPv4類型</summary>
		public static Boolean IsIPv4( this IPAddress address ) { return address.AddressFamily == AddressFamily.InterNetwork; }
	}
}