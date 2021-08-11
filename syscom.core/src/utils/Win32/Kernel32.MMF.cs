using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Microsoft.Win32.SafeHandles;


namespace syscom.utils.win32
{
	[StructLayout( LayoutKind.Sequential )]
	internal struct SYSTEM_INFO
	{
		internal _PROCESSOR_INFO_UNION uProcessorInfo;
		public UInt32 dwPageSize;
		public IntPtr lpMinimumApplicationAddress;
		public IntPtr lpMaximumApplicationAddress;
		public IntPtr dwActiveProcessorMask;
		public UInt32 dwNumberOfProcessors;
		public UInt32 dwProcessorType;
		public UInt32 dwAllocationGranularity;
		public UInt16 dwProcessorLevel;
		public UInt16 dwProcessorRevision;
	}

	[StructLayout( LayoutKind.Explicit )]
	internal struct _PROCESSOR_INFO_UNION
	{
		[FieldOffset( 0 )]
		internal UInt32 dwOemId;

		[FieldOffset( 0 )]
		internal UInt16 wProcessorArchitecture;

		[FieldOffset( 2 )]
		internal UInt16 wReserved;
	}

	[Flags]
	public enum FileMapAccess : uint
	{
		FileMapCopy = 0x0001,
		FileMapWrite = 0x0002,
		FileMapRead = 0x0004,
		FileMapAllAccess = 0x001f,
		FileMapExecute = 0x0020
	}

	[Flags]
	internal enum FileMapProtection : uint
	{
		PageReadonly = 0x02,
		PageReadWrite = 0x04,
		PageWriteCopy = 0x08,
		PageExecuteRead = 0x20,
		PageExecuteReadWrite = 0x40,
		SectionCommit = 0x8000000,
		SectionImage = 0x1000000,
		SectionNoCache = 0x10000000,
		SectionReserve = 0x4000000
	}

	internal static partial class Kernel32
	{
		/// <summary>
		/// Allow copying memory from one IntPtr to another. Required as the <see cref="System.Runtime.InteropServices.Marshal.Copy(System.IntPtr, System.IntPtr[], int, int)"/> implementation does not provide an appropriate override.
		/// </summary>
		[SecurityCritical] [DllImport( "kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false )]
		public static extern void CopyMemory( IntPtr dest, IntPtr src, UInt32 count );

		[SecurityCritical] [DllImport( "kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false )]
		public static extern unsafe void CopyMemoryPtr( void* dest, void* src, UInt32 count );

		[SecurityCritical] [DllImport( "kernel32.dll", BestFitMapping = false, CharSet = CharSet.Auto, ExactSpelling = false )]
		public static extern Int32 FormatMessage( Int32 dwFlags, IntPtr lpSource, Int32 dwMessageId, Int32 dwLanguageId, StringBuilder lpBuffer, Int32 nSize, IntPtr va_list_arguments );

		//[SecurityCritical]
		//public static string GetMessage( Int32 errorCode )
		//{
		//	StringBuilder stringBuilder = new StringBuilder( 512 );
		//	if ( FormatMessage( 12800, IntPtr.Zero, errorCode, 0, stringBuilder, stringBuilder.Capacity, IntPtr.Zero ) != 0 )
		//	{
		//		return stringBuilder.ToString();
		//	}
		//	return string.Concat( "UnknownError_Num ", errorCode );
		//}


		/// <summary>
		/// Cannot create a file when that file already exists.
		/// </summary>
		internal const Int32 ERROR_ALREADY_EXISTS = 0xB7; // 183

		/// <summary>
		/// The system cannot open the file.
		/// </summary>
		internal const Int32 ERROR_TOO_MANY_OPEN_FILES = 0x4; // 4

		/// <summary>
		/// Access is denied.
		/// </summary>
		internal const Int32 ERROR_ACCESS_DENIED = 0x5; // 5

		/// <summary>
		/// The system cannot find the file specified.
		/// </summary>
		internal const Int32 ERROR_FILE_NOT_FOUND = 0x2; // 2

		[SecurityCritical] [DllImport( "kernel32.dll", CharSet = CharSet.None, SetLastError = true )]
		public static extern Boolean CloseHandle( IntPtr handle );

		[SecurityCritical] [DllImport( "kernel32.dll", BestFitMapping = false, CharSet = CharSet.Auto, SetLastError = true, ThrowOnUnmappableChar = true )]
		public static extern SafeMemoryMappedFileHandle CreateFileMapping( SafeFileHandle hFile, IntPtr lpAttributes, FileMapProtection fProtect, Int32 dwMaxSizeHi, Int32 dwMaxSizeLo, String lpName );

		//public static SafeMemoryMappedFileHandle CreateFileMapping( SafeFileHandle hFile, FileMapProtection flProtect, Int64 ddMaxSize, string lpName )
		//{
		//	var hi = (Int32)( ddMaxSize / Int32.MaxValue );
		//	var lo = (Int32)( ddMaxSize % Int32.MaxValue );
		//	return CreateFileMapping( hFile, IntPtr.Zero, flProtect, hi, lo, lpName );
		//}

		[DllImport( "kernel32.dll" )]
		public static extern void GetSystemInfo( [MarshalAs( UnmanagedType.Struct )] ref SYSTEM_INFO lpSystemInfo );

		[DllImport( "kernel32.dll", SetLastError = true )]
		public static extern SafeMemoryMappedViewHandle MapViewOfFile( SafeMemoryMappedFileHandle hFileMappingObject, FileMapAccess dwDesiredAccess, UInt32 dwFileOffsetHigh, UInt32 dwFileOffsetLow, UIntPtr dwNumberOfBytesToMap );

		//public static SafeMemoryMappedViewHandle MapViewOfFile( SafeMemoryMappedFileHandle hFileMappingObject, FileMapAccess dwDesiredAccess, UInt64 ddFileOffset, UIntPtr dwNumberofBytesToMap )
		//{
		//	var hi = (UInt32)( ddFileOffset / UInt32.MaxValue );
		//	var lo = (UInt32)( ddFileOffset % UInt32.MaxValue );
		//	return MapViewOfFile( hFileMappingObject, dwDesiredAccess, hi, lo, dwNumberofBytesToMap );
		//}

		[DllImport( "kernel32.dll", BestFitMapping = false, CharSet = CharSet.Auto, SetLastError = true, ThrowOnUnmappableChar = true )]
		public static extern SafeMemoryMappedFileHandle OpenFileMapping( UInt32 dwDesiredAccess, Boolean bInheritHandle, String lpName );

		[DllImport( "kernel32.dll", SetLastError = true )]
		public static extern Boolean UnmapViewOfFile( IntPtr lpBaseAddress );
	}
}