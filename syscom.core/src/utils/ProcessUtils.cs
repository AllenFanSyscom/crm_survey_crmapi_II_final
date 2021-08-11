using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using syscom.Win32;
using System.Threading;
using syscom.utils.win32;

namespace syscom
{
	public static class ProcessUtils
	{
		/// <summary>目前執行的Process</summary>
		public static Process Current { get; private set; }

		static ProcessUtils() { Current = Process.GetCurrentProcess(); }
		//================================================================================================================================================
		// Public - Generic
		//================================================================================================================================================


		//================================================================================================================================================
		// Public - Process
		//================================================================================================================================================

		/// <summary>啟動無視窗的程序</summary>
		public static Process StartNoWindowBy( String fileName, String arguments )
		{
			var process = new Process();
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.FileName = fileName;
			process.StartInfo.Arguments = arguments;
			process.Start();
			return process;
		}


		public static Boolean IsWin64( Process process )
		{
			if ( Environment.OSVersion.Version.Major > 5 || Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1 )
				try
				{
					Boolean retVal;
					return Kernel32.IsWow64Process( process.Handle, out retVal ) && retVal;
				}
				catch { return false; /* access is denied to the process */ }

			return false; // not on 64-bit Windows
		}

	}
}
