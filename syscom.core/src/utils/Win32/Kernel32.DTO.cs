using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;


namespace syscom.utils.win32
{
	// use to get memory available
	[StructLayout( LayoutKind.Sequential, CharSet = CharSet.Auto )]
	public class MemoryStatusEx
	{
		public UInt32 dwLength;
		public UInt32 dwMemoryLoad;
		public UInt64 ullTotalPhys;
		public UInt64 ullAvailPhys;
		public UInt64 ullTotalPageFile;
		public UInt64 ullAvailPageFile;
		public UInt64 ullTotalVirtual;
		public UInt64 ullAvailVirtual;
		public UInt64 ullAvailExtendedVirtual;

		public Int64 MegaBytesOfTotal => (Int64) ullTotalPhys / 1024 / 1024;
		public Int64 MegaBytesOfAvailable => (Int64) ullAvailPhys / 1024 / 1024;

		public MemoryStatusEx() { dwLength = (UInt32) Marshal.SizeOf( typeof( MemoryStatusEx ) ); }
	}
}