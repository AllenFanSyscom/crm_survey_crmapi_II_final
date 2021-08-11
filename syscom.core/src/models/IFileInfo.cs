using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace syscom.models
{
	public interface IFileInfo
	{
		String DirectoryName { get; set; }
		String Name { get; set; }
		String FullName { get; set; }
		String Extension { get; set; }
		DateTime CreationTime { get; set; }
		DateTime LastWriteTime { get; set; }
		Int64 Length { get; set; }
		Boolean Exists { get; set; }
		Byte[] Content { get; set; }
	}
}