using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace syscom.io
{
	public enum AlgorithmType
	{
		MD5,
		SHA1,
		SHA256,
		SHA384,
		SHA512,
		RIPEMD160
	}

	public static class CheckSum
	{
		public static readonly HashAlgorithm MD5 = new MD5CryptoServiceProvider();
		public static readonly HashAlgorithm SHA1 = new SHA1Managed();
		public static readonly HashAlgorithm SHA256 = new SHA256Managed();
		public static readonly HashAlgorithm SHA384 = new SHA384Managed();
		public static readonly HashAlgorithm SHA512 = new SHA512Managed();

		public static Byte[] GetHashBy( String fileName, AlgorithmType type )
		{
			HashAlgorithm? ha = null;
			using ( var stream = File.OpenRead( fileName ) )
			{
				switch ( type )
				{
					case AlgorithmType.MD5:
						ha = MD5;
						break;
					case AlgorithmType.SHA1:
						ha = SHA1;
						break;
					case AlgorithmType.SHA256:
						ha = SHA256;
						break;
					case AlgorithmType.SHA384:
						ha = SHA384;
						break;
					case AlgorithmType.SHA512:
						ha = SHA512;
						break;
				}

				return ha.ComputeHash( stream );
			}
		}

		public static String GetHashStringBy( String filePath, AlgorithmType type ) { return BitConverter.ToString( GetHashBy( filePath, type ) ).Replace( "-", String.Empty ); }
	}
}
