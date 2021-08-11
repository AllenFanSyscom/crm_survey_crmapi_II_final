using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace syscom.utils.win32
{
	internal delegate Boolean ConsoleCtrlHandlerDelegate( ConsoleCtrlHandlerCode eventCode );

	internal enum ConsoleCtrlHandlerCode : uint
	{
		CTRL_C_EVENT = 0,
		CTRL_BREAK_EVENT = 1,
		CTRL_CLOSE_EVENT = 2,
		CTRL_LOGOFF_EVENT = 5,
		CTRL_SHUTDOWN_EVENT = 6
	}

	[System.Security.SuppressUnmanagedCodeSecurity]
	internal static partial class Kernel32
	{
		[DllImport( "kernel32.dll", CharSet = CharSet.Auto )]
		public static extern Boolean SetConsoleCtrlHandler( ConsoleCtrlHandlerDelegate handleAction, Boolean isAttach );

		[return: MarshalAs( UnmanagedType.Bool )]
		[DllImport( "kernel32.dll", CharSet = CharSet.Auto, SetLastError = true )]
		public static extern Boolean GlobalMemoryStatusEx( [In] [Out] MemoryStatusEx lpBuffer );


		[DllImport( "kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi )]
		[return: MarshalAs( UnmanagedType.Bool )]
		public static extern Boolean IsWow64Process( [In] IntPtr process, [Out] out Boolean wow64Process );


		[UnmanagedFunctionPointer( CallingConvention.StdCall )]
		public delegate Int32 CreateObjectDelegate( [In] ref Guid classID, [In] ref Guid interfaceID, [MarshalAs( UnmanagedType.Interface )] out Object outObject );

		[DllImport( "kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true )]
		public static extern IntPtr LoadLibrary( [MarshalAs( UnmanagedType.LPStr )] String fileName );

		[DllImport( "kernel32.dll" )]
		[return: MarshalAs( UnmanagedType.Bool )]
		public static extern Boolean FreeLibrary( IntPtr hModule );

		[DllImport( "kernel32.dll", BestFitMapping = false, ThrowOnUnmappableChar = true )]
		public static extern IntPtr GetProcAddress( IntPtr hModule, [MarshalAs( UnmanagedType.LPStr )] String procName );
	}
}