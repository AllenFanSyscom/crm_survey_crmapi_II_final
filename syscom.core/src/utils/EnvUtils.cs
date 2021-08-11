using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Threading;
using syscom.utils.win32;

namespace syscom
{

	public static class EnvUtils
	{
		static EnvUtils()
		{
			var p = PlatformId;
			IsUnixBasePlatform = p == PlatformID.Unix || p == PlatformID.MacOSX;
			Is64Bit = IntPtr.Size == 8;
		}


		public static PlatformID PlatformId => Environment.OSVersion.Platform;
		public static Boolean Is64Bit { get; }
		public static Boolean IsUnixBasePlatform { get; }
		public static Boolean IsWindowsBasePlatform => !IsUnixBasePlatform;


		public static MemoryStatusEx GetMemoryInfo()
		{
			var memStatus = new MemoryStatusEx();
			return Kernel32.GlobalMemoryStatusEx( memStatus ) ? memStatus : null;
		}

		/// <summary>注意，此方法需使用500ms計算cpu頻率</summary>
		// public static Single GetPercentOfCpuUsage()
		// {
		// 	var cpuCounter = new PerformanceCounter( "Processor", "% Processor Time", "_Total" );
		// 	cpuCounter.NextValue();
		// 	Thread.Sleep( 500 );
		// 	return (Int32) cpuCounter.NextValue();
		// }

		/// <summary>
		/// 取得該機器上的系統資訊
		/// </summary>
		public static SysInfo GetAgentSysInfo()
		{
			//TEST DATA=========================================
			var info = new SysInfo() { MaxMemory = 1024, UsageOfMemory = 256 };
			info.CPU.Add( new CPUInfo() { Name = "core #1", Fan = 1000, Temperature = 52.6, UsageOfCPU = 25.4 } );
			info.CPU.Add( new CPUInfo() { Name = "core #2", Fan = 1000, Temperature = 52.6, UsageOfCPU = 25.4 } );
			info.HD_Physic.Add( new HdInfo() { Name = "Segate", Fan = 2000, MaxSpace = 256, UsageOfSpace = 156 } );
			info.HD_Physic.Add( new HdInfo() { Name = "Segate2", Fan = 2000, MaxSpace = 256, UsageOfSpace = 156 } );
			info.HD_Logical.Add( new LogicalHDInfo() { Name = "C", UsageOfSpace = 4096, MaxSpace = 256 } );
			info.HD_Logical.Add( new LogicalHDInfo() { Name = "D", UsageOfSpace = 4096, MaxSpace = 256 } );
			info.NetWork.Add( new NetWorkInfo() { Name = "WIFI", CurrentRecive = 1, CurrentSend = 2, Speed = 256, TotalRecive = 1096, TotalSend = 2564 } );
			info.NetWork.Add( new NetWorkInfo() { Name = "Ethernet", CurrentRecive = 1, CurrentSend = 2, Speed = 256, TotalRecive = 1096, TotalSend = 2564 } );
			//==============================================
			return info;
		}
	}
}
