using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using syscom.Win32;
using syscom.utils.win32;

namespace syscom.Win32.Console
{
}

namespace syscom
{
	internal class ConsoleUtils
	{
		public const UInt32 SC_CLOSE = 0xF060;
		public const UInt32 MF_ENABLED = 0x00000000;
		public const UInt32 MF_GRAYED = 0x00000001;
		public const UInt32 MF_DISABLED = 0x00000002;
		public const UInt32 MF_BYCOMMAND = 0x00000000;

		public static void DisableCloseButtonBy( Process consoleProcess )
		{
			var hMenu = consoleProcess.MainWindowHandle;
			var hSystemMenu = User32.GetSystemMenu( hMenu, false );

			User32.EnableMenuItem( hSystemMenu, SC_CLOSE, MF_GRAYED );
			User32.RemoveMenu( hSystemMenu, SC_CLOSE, MF_BYCOMMAND );
		}


		public static void Pause() { Msvcrt.System( "pause" ); }


		////ConsoleUtils.SetConsoleControlHandleBy( OnConsoleCtrlHandler, true );
		//private Boolean OnConsoleCtrlHandler( ConsoleCtrlHandlerCode eventCode )
		//{
		//	Log.LogFile( "OnConsoleCtrlHandler: " + eventCode.ToString() );
		//	switch ( eventCode )
		//	{
		//		case ConsoleCtrlHandlerCode.CTRL_CLOSE_EVENT:
		//		case ConsoleCtrlHandlerCode.CTRL_BREAK_EVENT:
		//		case ConsoleCtrlHandlerCode.CTRL_LOGOFF_EVENT:
		//		case ConsoleCtrlHandlerCode.CTRL_SHUTDOWN_EVENT:

		//		_Thread_ForceShutdown.Start();
		//		//_Thread_OnServiceShutdown.Start();

		//		Monitor.Enter( _test );
		//		_Semaphore_Main.WaitOne();
		//		break;
		//	}
		//	return ( false );
		//}

		public static void SetConsoleControlHandleBy( ConsoleCtrlHandlerDelegate handle, Boolean isAttach ) { Kernel32.SetConsoleCtrlHandler( handle, isAttach ); }
	}
}
