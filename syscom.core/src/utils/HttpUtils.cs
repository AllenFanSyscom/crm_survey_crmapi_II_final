using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Cache;
using System.Text;
using syscom;

namespace System.Net
{
	// https://stackoverflow.com/questions/4137106/are-there-net-implementation-of-tls-1-2/29221917#29221917
	// https://stackoverflow.com/questions/42746190/https-request-fails-using-httpclient

	public class CustomWebClient : WebClient
	{
		public static Int32 TimeoutSeconds = 20;

		static CustomWebClient()
		{
			TimeoutSeconds = ConfigUtils.GetAppSettingOr( "WebClient:Timeout", 20 );
		}


		protected override WebRequest GetWebRequest( Uri uri )
		{
			var wr = base.GetWebRequest( uri );
			wr.Timeout = TimeoutSeconds * 1000;
			return wr;
		}
	}

	public class HttpUtils
	{
		static readonly syscom.ILogger log = syscom.LogUtils.GetLoggerForCurrentClass();

		/// <summary>全域設定的Encoding, 每個Http動作將以此設定為預設值</summary>
		public static Encoding Encode = Encoding.UTF8;

		static RequestCachePolicy Policy_NoCache = new System.Net.Cache.RequestCachePolicy( System.Net.Cache.RequestCacheLevel.NoCacheNoStore );

		//==========================================================================================
		// 公用方法
		//==========================================================================================
		public static String Get( String url, Encoding? encode = null )
		{
			try
			{
				using ( var client = new CustomWebClient { Encoding = Encode } )
				{
					client.CachePolicy = Policy_NoCache;
					ServicePointManager.ServerCertificateValidationCallback += ( sender, cert, chain, sslPolicyErrors ) => true;
					if ( encode != null ) client.Encoding = encode;

					return client.DownloadString( url );
				}
			}
			catch ( WebException ex )
			{
				throw new WebException( $"[HttpUtils] url[{url}] status[{ex.Status}] ex[{ex.Message}]", ex, ex.Status, ex.Response );
			}
			catch ( Exception ex )
			{
				throw new Exception( $"[HttpUtils] url[{url}] ex[{ex.Message}]", ex );
			}
		}

		public static String Post( String url, String postStr = "", Encoding? encode = null )
		{
			try
			{
				log.Debug( $"[HttpUtils] POST[{url}] data[{postStr}]" );

				using ( var client = new CustomWebClient { Encoding = Encode } )
				{
					client.CachePolicy = Policy_NoCache;
					ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
					if ( encode != null ) client.Encoding = encode;

					var sendData = client.Encoding.GetBytes( postStr );

					// client.Headers.Add( "Content-Type", "application/x-www-form-urlencoded" );
					client.Headers.Add( "ContentLength", sendData.Length.ToString( CultureInfo.InvariantCulture ) );

					var readData = client.UploadData( url, "POST", sendData );

					return client.Encoding.GetString( readData );
				}
			}
			catch ( WebException ex )
			{
				throw new WebException( $"[HttpUtils] url[{url}] status[{ex.Status}] ex[{ex.Message}]", ex, ex.Status, ex.Response );
			}
			catch ( Exception ex )
			{
				throw new Exception( $"[HttpUtils] url[{url}] ex[{ex.Message}]", ex );
			}
		}

		public static String ConvertToQueryParameter<TData>( TData data ) where TData : class
		{
			var list = new List<String>();

			if ( data != null )
			{
				var props = data.GetType().GetProperties();
				foreach ( var p in props )
				{
					var key = p.Name;
					var val = p.GetValue( data );
					list.Add( $"{key}={val}" );
				}
			}

			return String.Join( "&", list );
		}

		public static String Post<TData>( String url, TData? data = null, Encoding? encode = null ) where TData : class
		{
			return Post( url, data == null ? "" : data.ToJson(), encode );
		}

		public static void GetAsync( String url, DownloadStringCompletedEventHandler? onComplete = null, Encoding? encode = null )
		{
			var client = new CustomWebClient { Encoding = Encode };
			client.CachePolicy = Policy_NoCache;

			if ( encode != null ) client.Encoding = encode;
			if ( onComplete != null ) client.DownloadStringCompleted += onComplete;

			client.DownloadStringAsync( new Uri( url ) );
		}

		public static void PostAsync( String url, String postStr = "", UploadDataCompletedEventHandler? onComplete = null, Encoding? encode = null )
		{
			var client = new CustomWebClient { Encoding = Encode };
			client.CachePolicy = Policy_NoCache;

			if ( encode != null ) client.Encoding = encode;

			var sendData = client.Encoding.GetBytes( postStr );

			client.Headers.Add( "Content-Type", "application/x-www-form-urlencoded" );
			client.Headers.Add( "ContentLength", sendData.Length.ToString() );

			if ( onComplete != null ) client.UploadDataCompleted += onComplete;

			client.UploadDataAsync( new Uri( url ), "POST", sendData );
		}
	}
}
