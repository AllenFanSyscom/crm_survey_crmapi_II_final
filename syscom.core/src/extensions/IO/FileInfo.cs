using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using syscom.io;

namespace System.IO
{
	public static class FileInfoExtensions
	{
		public static Byte[] GetMd5( this FileInfo fi ) { return CheckSum.GetHashBy( fi.FullName, AlgorithmType.MD5 ); }

		public static String GetMd5String( this FileInfo fi )
		{
			var bytes = fi.GetMd5();
			return BitConverter.ToString( bytes ).Replace( "-", "" );
		}

		public static void CopyBy( this FileInfo file, String newPath, Boolean overwrite = false, Boolean deleteSource = false, Boolean keepTime = true )
		{
			file.CopyTo( newPath, overwrite );

			if ( keepTime )
			{
				var destination = new FileInfo( newPath );
				destination.IsReadOnly = false;
				destination.CreationTime = file.CreationTime;
				destination.LastWriteTime = file.LastWriteTime;
				destination.LastAccessTime = file.LastAccessTime;
			}

			if ( deleteSource )
			{
				if ( file.IsReadOnly ) file.IsReadOnly = false;
				file.Delete();
			}
		}
	}
}