using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace syscom.Text
{
	public enum EncodeType
	{
		MD5 = 1 << 0,
		DES = 1 << 1
	}


	[Serializable]
	public class EncodeKey
	{
		public Byte[] Key { get; private set; }

		/// <summary>Key Initialization Vector</summary>
		public Byte[] KeyInitVector { get; private set; }

		public EncodeKey() : this( CreateKey(), CreateKey() ) { }

		public EncodeKey( Byte[] key, Byte[] keyInitVector )
		{
			Key = key;
			KeyInitVector = keyInitVector;
		}

		/// <summary>產生8組byte</summary>
		static Byte[] CreateKey() { return DES.Create().Key; }
	}

	public static class EncodeUtils
	{
		public static String ArtEncrypt( EncodeType type, Tuple<EncodeKey, EncodeKey> keys, String source )
		{
			var result = source;
			try
			{
				if ( type == EncodeType.DES )
				{
					result = EncryptMd5By( keys.Item1.Key, keys.Item1.KeyInitVector, result );
					result = EncryptDesBy( keys.Item2.Key, keys.Item2.KeyInitVector, result );
				}
				else
				{
					result = EncryptDesBy( keys.Item2.Key, keys.Item2.KeyInitVector, result );
					result = EncryptMd5By( keys.Item1.Key, keys.Item1.KeyInitVector, result );
				}
			}
			catch ( Exception ) { throw; }

			return result;
		}

		public static String ArtDecrypt( EncodeType type, Tuple<EncodeKey, EncodeKey> keys, String source )
		{
			var result = source;
			try
			{
				if ( type == EncodeType.DES )
				{
					result = DecryptDesBy( keys.Item2.Key, keys.Item2.KeyInitVector, result );
					result = DecryptMd5By( keys.Item1.Key, keys.Item1.KeyInitVector, result );
				}
				else
				{
					result = DecryptMd5By( keys.Item1.Key, keys.Item1.KeyInitVector, result );
					result = DecryptDesBy( keys.Item2.Key, keys.Item2.KeyInitVector, result );
				}
			}
			catch ( Exception ) { throw; }

			return result;
		}


		//public static String EncryptMd5By( String source, String key )
		//{
		//	return EncryptMd5By( source, ASCIIEncoding.ASCII.GetBytes( key ) );
		//}
		//public static String EncryptMd5By( String source, Byte[] key )
		//{
		//	return EncryptMd5By( source, key, key );
		//}
		//public static String EncryptMd5By( String source, EncodeKey encodeKey )
		//{
		//	return EncryptMd5By( source, encodeKey.Key, encodeKey.KeyInitVector );
		//}

		public static String EncryptBy( EncodeType type, Byte[] key, Byte[] keyV, String source )
		{
			switch ( type )
			{
				case EncodeType.MD5:
					return EncryptMd5By( key, keyV, source );

				case EncodeType.DES:
					return EncryptDesBy( key, keyV, source );
			}

			throw Err.Utility( "錯誤的EncodeType [ " + type + " ]" );
		}

		public static String DecryptBy( EncodeType type, Byte[] key, Byte[] keyV, String source )
		{
			switch ( type )
			{
				case EncodeType.MD5:
					return DecryptMd5By( key, keyV, source );

				case EncodeType.DES:
					return DecryptDesBy( key, keyV, source );
			}

			throw Err.Utility( "錯誤的EncodeType [ " + type + " ]" );
		}

		static String EncryptMd5By( Byte[] key, Byte[] keyVector, String source )
		{
			var result = String.Empty;
			var desProvider = new DESCryptoServiceProvider() { Key = key, IV = keyVector };
			var inputByteArray = Encoding.Default.GetBytes( source );

			using ( var ms = new MemoryStream() )
			using ( var cs = new CryptoStream( ms, desProvider.CreateEncryptor(), CryptoStreamMode.Write ) )
			{
				cs.Write( inputByteArray, 0, inputByteArray.Length );
				cs.FlushFinalBlock();
				var ret = new StringBuilder();
				foreach ( var b in ms.ToArray() ) ret.AppendFormat( "{0:X2}", b );
				result = ret.ToString();
				return result;
			}
		}

		static String DecryptMd5By( Byte[] key, Byte[] keyVector, String source )
		{
			var result = String.Empty;
			var des = new DESCryptoServiceProvider();
			var inputByteArray = new Byte[source.Length / 2];
			for ( var x = 0; x < source.Length / 2; x++ )
			{
				var i = Convert.ToInt32( source.Substring( x * 2, 2 ), 16 );
				inputByteArray[x] = (Byte) i;
			}

			des.Key = key;
			des.IV = keyVector;
			using ( var ms = new MemoryStream() )
			using ( var cs = new CryptoStream( ms, des.CreateDecryptor(), CryptoStreamMode.Write ) )
			{
				cs.Write( inputByteArray, 0, inputByteArray.Length );
				cs.FlushFinalBlock();
				result = Encoding.Default.GetString( ms.ToArray() );
			}

			return result;
		}


		//public static String EncryptDesBy( String source, String key )
		//{
		//	return EncryptDesBy( source, ASCIIEncoding.ASCII.GetBytes( key ) );
		//}
		//public static String EncryptDesBy( String source, Byte[] key )
		//{
		//	return EncryptDesBy( source, key, key );
		//}
		//public static String EncryptDesBy( String source, EncodeKey encodeKey )
		//{
		//	return EncryptDesBy( source, encodeKey.Key, encodeKey.KeyInitVector );
		//}
		static String EncryptDesBy( Byte[] key, Byte[] keyVector, String source )
		{
			var data = Encoding.UTF8.GetBytes( source );
			var des = new DESCryptoServiceProvider { Key = key, IV = keyVector };

			var desencrypt = des.CreateEncryptor();
			var result = desencrypt.TransformFinalBlock( data, 0, data.Length );

			return BitConverter.ToString( result );
		}

		static String DecryptDesBy( Byte[] key, Byte[] keyVector, String source )
		{
			var sInput = source.Split( "-".ToCharArray() );
			var data = new Byte[sInput.Length];
			for ( var i = 0; i < sInput.Length; i++ ) data[i] = Byte.Parse( sInput[i], NumberStyles.HexNumber );
			using ( var des = new DESCryptoServiceProvider { Key = key, IV = keyVector } )
			{
				var desencrypt = des.CreateDecryptor();
				var result = desencrypt.TransformFinalBlock( data, 0, data.Length );

				return Encoding.UTF8.GetString( result );
			}
		}
	}
}