using syscom.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syscom
{
	public static class CompressUtils
	{
		//static CompressUtils()
		//{
		//	if ( AppUtils.CurrentIs64Bit() )
		//	{
		//		AssemblyUtils.LoadEmbeddedResourceBy( "syscom.configPlugins.Compress.Win32_7z_x64.dll", "7z.dll" );
		//	}
		//	else
		//	{
		//		AssemblyUtils.LoadEmbeddedResourceBy( "syscom.configPlugins.Compress.Win32_7z_x86.dll", "7z.dll" );
		//	}
		//	//AssemblyUtils.LoadEmbeddedResourceBy( "syscom.configPlugins.Compress.SevenZipSharp.dll", "SevenZipSharp.dll" );
		//	//AssemblyUtils.LoadEmbeddedResourceBy( "syscom.configPlugins.Compress.SevenZipSharp.pdb", "SevenZipSharp.pdb" );
		//}

		//public static void Archive( String source, String output )
		//{
		//	var libPath = AppDomain.CurrentDomain.BaseDirectory + @"\7z.dll";
		//	SevenZip.SevenZipCompressor.SetLibraryPath( libPath );
		//	var compress = new SevenZip.SevenZipCompressor();
		//	compress.ArchiveFormat = SevenZip.OutArchiveFormat.SevenZip;
		//	compress.CompressionLevel = SevenZip.CompressionLevel.Ultra;
		//	compress.CompressDirectory( source, output );

		//}
	}
}
