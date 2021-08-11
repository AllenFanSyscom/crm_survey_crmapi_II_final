using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syscom
{
	public class SysInfo
	{
		public String IP { get; set; }
		public Boolean IsConnected { get; set; }
		public Int64 MaxMemory { get; set; }
		public Int64 UsageOfMemory { get; set; }
		public DateTime DateTime { get; set; }
		public List<CPUInfo> CPU = new List<CPUInfo>();
		public List<HdInfo> HD_Physic = new List<HdInfo>();
		public List<LogicalHDInfo> HD_Logical = new List<LogicalHDInfo>();
		public List<NetWorkInfo> NetWork = new List<NetWorkInfo>();
	}

	public class HdInfo
	{
		public String Name { get; set; }
		public Int64 MaxSpace { get; set; }
		public Int64 UsageOfSpace { get; set; }
		public Int64 Fan { get; set; }
	}

	public class LogicalHDInfo
	{
		public String Name { get; set; }
		public Int64 MaxSpace { get; set; }
		public Int64 UsageOfSpace { get; set; }
	}

	public class CPUInfo
	{
		public String Name { get; set; }
		public Double Temperature { get; set; }
		public Double UsageOfCPU { get; set; }
		public Int64 Fan { get; set; }
	}

	public class NetWorkInfo
	{
		public String Name { get; set; }
		public Int64 CurrentRecive { get; set; }
		public Int64 CurrentSend { get; set; }
		public Int64 TotalRecive { get; set; }
		public Int64 TotalSend { get; set; }
		public Double Speed { get; set; }
	}
}
