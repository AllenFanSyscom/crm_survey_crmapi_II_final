using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BOOL = System.Boolean;
using DWORD = System.UInt32;
using LPWSTR = System.String;
using NET_API_STATUS = System.UInt32;

namespace syscom.Net
{
	public class UncAccessor : IDisposable
	{
		[StructLayout( LayoutKind.Sequential, CharSet = CharSet.Unicode )]
		internal struct USE_INFO_2
		{
			internal LPWSTR ui2_local;
			internal LPWSTR ui2_remote;
			internal LPWSTR ui2_password;
			internal DWORD ui2_status;
			internal DWORD ui2_asg_type;
			internal DWORD ui2_refcount;
			internal DWORD ui2_usecount;
			internal LPWSTR ui2_username;
			internal LPWSTR ui2_domainname;
		}

		[DllImport( "NetApi32.dll", SetLastError = true, CharSet = CharSet.Unicode )]
		internal static extern NET_API_STATUS NetUseAdd( LPWSTR UncServerName, DWORD Level, ref USE_INFO_2 Buf, out DWORD ParmError );

		[DllImport( "NetApi32.dll", SetLastError = true, CharSet = CharSet.Unicode )]
		internal static extern NET_API_STATUS NetUseDel( LPWSTR UncServerName, LPWSTR UseName, DWORD ForceCond );

		Boolean disposed = false;

		String PathOfUNC;
		String User;
		String Password;
		String Domain;
		Int32 iLastError;

		public Win32Exception Exception { get; set; }

		/// <summary>
		/// A disposeable class that allows access to a UNC resource with credentials.
		/// </summary>
		public UncAccessor() { }

		~UncAccessor() { Dispose(); }

		public void Dispose()
		{
			if ( !disposed )
			{
				Disconnect();
			}

			disposed = true;
			GC.SuppressFinalize( this );
		}


		/// <summary>
		/// The last system error code returned from NetUseAdd or NetUseDel.  Success = 0
		/// </summary>
		public Int32 LastError => iLastError;


		public void ConnectBy( String pathOfUNC, String user, String password, String? domain = null )
		{
			if ( String.IsNullOrWhiteSpace( pathOfUNC ) ) throw new ArgumentNullException( "pathOfUNC" );
			PathOfUNC = pathOfUNC;
			User = user;
			Password = password;
			Domain = domain;

			PathOfUNC = PathOfUNC.Replace( @"/", @"\" );
			if ( PathOfUNC.EndsWith( @"\" ) ) PathOfUNC = PathOfUNC.Substring( 0, PathOfUNC.Length - 1 );

			ConnectBy();
		}

		void ConnectBy()
		{
			UInt32 returncode;
			try
			{
				var useinfo = new USE_INFO_2
				{
					ui2_remote = PathOfUNC,
					ui2_username = User,
					ui2_domainname = Domain,
					ui2_password = Password,
					ui2_asg_type = 0,
					ui2_usecount = 1
				};

				UInt32 paramErrorIndex;
				returncode = NetUseAdd( null, 2, ref useinfo, out paramErrorIndex );
				iLastError = (Int32) returncode;
			}
			catch
			{
				iLastError = Marshal.GetLastWin32Error();
			}

			if ( iLastError != 0 ) throw new Win32Exception( iLastError );
		}

		/// <summary>
		/// Ends the connection to the remote resource
		/// </summary>
		/// <returns>True if it succeeds.  Use LastError to get the system error code</returns>
		public void Disconnect()
		{
			if ( String.IsNullOrWhiteSpace( PathOfUNC ) ) return;
			if ( iLastError != 0 ) return;
			UInt32 returncode;
			try
			{
				returncode = NetUseDel( null, PathOfUNC, 2 );
				iLastError = (Int32) returncode;
				if ( iLastError == 0 ) PathOfUNC = String.Empty;
			}
			catch
			{
				iLastError = Marshal.GetLastWin32Error();
			}

			if ( iLastError != 0 ) throw new Win32Exception( iLastError );
		}
	}
}
