using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace syscom.utils.win32
{
	public enum ShowWindowEnum
	{
		Hide = 0,
		ShowNormal = 1, ShowMinimized = 2, ShowMaximized = 3,
		Maximize = 3, ShowNormalNoActivate = 4, Show = 5,
		Minimize = 6, ShowMinNoActivate = 7, ShowNoActivate = 8,
		Restore = 9, ShowDefault = 10, ForceMinimized = 11
	};

	public static partial class User32
	{
		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		public static extern Boolean EnableMenuItem( IntPtr hMenu, UInt32 uIDEnableItem, UInt32 uEnable );

		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		public static extern IntPtr GetSystemMenu( IntPtr hWnd, Boolean bRevert );

		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		public static extern IntPtr RemoveMenu( IntPtr hMenu, UInt32 nPosition, UInt32 wFlags );


		[DllImport( "user32.dll" )]
		public static extern IntPtr DeleteMenu( IntPtr hMenu, Int32 nPosition, Int32 wFlags );

		[DllImport( "kernel32.dll", ExactSpelling = true )]
		public static extern IntPtr GetConsoleWindow();


		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		public static extern IntPtr FindWindow( String lpClassName, String lpWindowName );

		/// <summary>取得對像Window的Rectangle</summary>
		[DllImport( "user32.dll" )]
		public static extern Boolean GetWindowRect( IntPtr hWnd, ref Rectangle bounds );

		/// <summary>重繪對像window</summary>
		[DllImport( "user32.dll ", EntryPoint = "RedrawWindow" )]
		public static extern Boolean RedrawWindow( IntPtr hWnd, IntPtr prect, IntPtr hrgnUpdate, UInt32 flags );

		[DllImport( "user32.dll" )]
		public static extern Boolean ShowWindowAsync( IntPtr hWnd, Int32 cmdShow );

		[DllImport( "user32.dll" )]
		public static extern Boolean SetForegroundWindow( IntPtr hWnd );


		/// <summary>取得DesktopWindow</summary>
		[DllImport( "user32.dll", CharSet = CharSet.Auto, ExactSpelling = true )]
		public static extern IntPtr GetDesktopWindow();

		/// <summary>取得DCEx</summary>
		[DllImport( "user32.dll", EntryPoint = "GetDCEx", CharSet = CharSet.Auto, ExactSpelling = true )]
		public static extern IntPtr GetDCEx( IntPtr hWnd, IntPtr hrgnClip, Int32 flags );


		[DllImport( "user32.dll", CharSet = CharSet.Auto, ExactSpelling = true )]
		public static extern IntPtr SetFocus( HandleRef hWnd );

		[DllImport( "user32.dll" )]
		[return: MarshalAs( UnmanagedType.Bool )]
		public static extern Boolean ShowWindow( IntPtr hWnd, ShowWindowEnum flags );

		[DllImport( "user32.dll" )]
		public static extern IntPtr SetActiveWindow( IntPtr hwnd );


		[DllImport( "user32.dll", CharSet = CharSet.Auto )]
		public static extern Int32 PostMessage( IntPtr hWnd, Int32 msg, IntPtr wParam, IntPtr lParam );
	}
}