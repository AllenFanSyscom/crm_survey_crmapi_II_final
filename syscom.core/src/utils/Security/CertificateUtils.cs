using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using syscom.Runtime;

namespace syscom.Security
{
	public static class CertificateUtils
	{

#if net45
		public static X509Certificate2 GetX509FromWebBy( String pathOfPFX, String password )
		{
			if ( HttpContext.Current == null ) throw Err.NoSupport( "目前不在Web環境之中, 無法使用" );
			var pfxPath = HttpContext.Current.Server.MapPath( pathOfPFX );
			var cert = new X509Certificate2( pfxPath, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet );
			return cert;
		}
#endif

		public static X509Certificate2 GetX509CertificateBy( String subjectName, StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.LocalMachine, OpenFlags openFlags = OpenFlags.ReadOnly )
		{
			return GetX509CertificateBy
			(
				cert => cert.SubjectName.Name != null && cert.SubjectName.Name.Equals( subjectName ),
				storeName,
				storeLocation,
				openFlags
			);
		}

		public static X509Certificate2 GetX509CertificateBy
		(
			Func<X509Certificate2, Boolean> funcSelect,
			StoreName storeName = StoreName.My, StoreLocation storeLocation = StoreLocation.LocalMachine, OpenFlags openFlags = OpenFlags.ReadOnly
		)
		{
			var store = new X509Store( storeName, storeLocation );
			store.Open( openFlags );
			X509Certificate2 cert;

			try
			{
				cert = store.Certificates.OfType<X509Certificate2>().FirstOrDefault( funcSelect );
			}
			finally
			{
				store.Close();
			}

			if ( cert == null ) throw Err.BusinessLogic( "找不到正確的憑證, 請確認伺服器已安裝AA憑證" );
			return cert;
		}
	}
}
