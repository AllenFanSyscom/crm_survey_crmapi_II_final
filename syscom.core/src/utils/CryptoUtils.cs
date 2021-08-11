using System.Security.Cryptography;
using System.Text;

namespace System
{
	public static partial class CryptoUtils
	{
		public enum Type
		{
			SHA1, MD5, SHA256, SHA384, SHA512
		}

		/// <summary>全域的預設Encoding</summary>
		public static Encoding Encode = Encoding.UTF8;

		public static String? EncryptBy( Type type, String data )
		{
			using ( var sha1 = GetHashAlgorithm( type ) )
			{
				var bytes = Encode.GetBytes( data );
				var hashed = sha1.ComputeHash( bytes );
				return BinaryToHex( hashed );
			}
		}

		public static String? BinaryToHex( Byte[] data )
		{
			if ( data == null ) return null;
			var chArray = new Char[checked( data.Length * 2 )];
			for ( var index = 0; index < data.Length; ++index )
			{
				var num = data[index];
				chArray[2 * index] = NibbleToHex( (Byte) ( (UInt32) num >> 4 ) );
				chArray[2 * index + 1] = NibbleToHex( (Byte) ( num & 15U ) );
			}

			return new String( chArray );
		}

		public static Char NibbleToHex( Byte nibble ) { return nibble < (Byte) 10 ? (Char) ( nibble + 48 ) : (Char) ( nibble - 10 + 65 ); }
	}


	static partial class CryptoUtils
	{
		public static HashAlgorithm GetHashAlgorithm( Type type )
		{
			switch ( type )
			{
				case Type.MD5:    return New.MD5;
				case Type.SHA256: return New.SHA256;
				case Type.SHA384: return New.SHA384;
				case Type.SHA512: return New.SHA512;

				default: return New.SHA1;
			}
		}

		public static class New
		{
			public static Aes Aes => Aes.Create();
			public static DES DES => DES.Create();
			public static HMACSHA1 HMACSHA1 => new HMACSHA1();
			public static HMACSHA256 HMACSHA256 => new HMACSHA256();
			public static HMACSHA384 HMACSHA384 => new HMACSHA384();

			public static HMACSHA512 HMACSHA512 => new HMACSHA512();

			public static HMACSHA512 CreateHMACSHA512( Byte[] key ) { return new HMACSHA512( key ); }

#if net45
			public static MD5 MD5 => new MD5Cng();
			public static SHA1 SHA1 => new SHA1Cng();
			public static SHA256 SHA256 => new SHA256Cng();
			public static SHA384 SHA384 => new SHA384Cng();
			public static SHA512 SHA512 => new SHA512Cng();
#else

			public static MD5 MD5 => MD5.Create();
			public static SHA1 SHA1 => SHA1.Create();
			public static SHA256 SHA256 => SHA256.Create();
			public static SHA384 SHA384 => SHA384.Create();
			public static SHA512 SHA512 => SHA512.Create();
#endif
			public static TripleDES TripleDES => new TripleDESCryptoServiceProvider();
		}
	}
}
