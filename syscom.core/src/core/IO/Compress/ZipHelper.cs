using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syscom.io.Compress
{
	//Todo: Implement archive Folder
	public class ZipHelper : IDisposable
	{
		public CompressionLevel Level { get; set; }
		public FileInfo OutputFile { get; private set; }

		ZipArchive zip;

		public ZipHelper( FileInfo outputFile, CompressionLevel level = CompressionLevel.Fastest )
		{
			OutputFile = outputFile;
			zip = ZipFile.Open( OutputFile.FullName, ZipArchiveMode.Create, Encoding.UTF8 );
		}

		//public void ArchiveBy( FileInfo fi )
		//{
		//	var entry = zip.CreateEntry( fi.Name, Level );
		//	//new FileStream(this.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false);
		//	var fs = fi.OpenRead();
		//	using ( var stream = entry.Open() )
		//	using( fs )
		//	{
		//		fs.CopyTo( stream );
		//	}
		//}

		public void ArchiveBy( FileInfo fi ) { zip.CreateEntryFromFile( fi.FullName, fi.Name, Level ); }

		public void ArchiveBy( DirectoryInfo dir )
		{
			foreach ( var file in dir.GetFiles() ) ArchiveBy( file );
		}


		#region IDisposable Members

		public void Dispose() { zip?.Dispose(); }

		#endregion
	}
}