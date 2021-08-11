using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
	public static class X509Certificate2Extensions
	{
		public static String Encrypt( this X509Certificate2 cer, String data )
		{
			var cryptoProvidor = (RSACryptoServiceProvider) cer.PublicKey.Key;
			var encryptedTokenBytes = cryptoProvidor.Encrypt( Encoding.UTF8.GetBytes( data ), true );
			return Convert.ToBase64String( encryptedTokenBytes );
		}

		public static String Decrypt( this X509Certificate2 cer, String data )
		{
			var cryptoProvidor = (RSACryptoServiceProvider) cer.PrivateKey;
			var decryptedTokenBytes = cryptoProvidor.Decrypt( Convert.FromBase64String( data ), true );
			return Encoding.UTF8.GetString( decryptedTokenBytes );
		}
	}
}