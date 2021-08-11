using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Web;
using syscom;

namespace System.Web
{
	/// <summary>
	/// WebApi的呼叫器, 請繼承後再實作呼叫
	/// - 讀取設定檔中的"WebApi:Servers"設定值 (e.g. http://domain/,https://domain2/,http://domain:8086/ )
	/// - 依序發送Api至目的伺服器, 若有任一伺服器取得回傳值則停止, 若全部伺服器皆通訊失敗則拋出異常
	/// </summary>
	public abstract class WebApiCaller
	{
		static readonly syscom.ILogger log = syscom.LogUtils.GetLoggerForCurrentClass();
		static readonly List<String> ServerDomains;

		static WebApiCaller()
		{
			ServerDomains = ConfigUtils.GetAppSettingOr( "WebApi:Servers", str => str.SplitByComma().ToList(), new List<String>() );
            log.Debug( $"[WebApi] Servers: {ServerDomains.ToJson()}" );
		}

		/// <summary>檢查當前設定檔狀態, 若有問題拋出異常</summary>
		protected static void CheckSettings()
		{
			if ( ServerDomains.Count <= 0 ) throw new Exception( "[WebApiCaller] cannot found setting of WebApiServerDomains" );
			ServerDomains.ForEach( ip =>
			{
				if ( !NetUtils.Ping( ip ) ) log.Warn( $"Cannot Ping to serverIP[{ip}]" );
			} );
		}

		static String CallToServers( String urlPath, Func<String, String> actExecute )
		{
			var url = new Url { Host = "localhost", Path = urlPath };

			var exs = new List<Exception>();

			foreach ( var serverAddress in ServerDomains )
			{
				var domain = serverAddress;
				log.Debug( $"[WebApi] Call domain[{domain}]" );

				url.Scheme = domain.Contains( "https://" ) ? "https" : "http";
				domain = domain.Replace( "https://", "" ).Replace( "http://", "" );
				if ( !domain.Contains( ":" ) )
				{
					url.Host = domain;
				}
				else
				{
					var parts = domain.SplitBy( ":" );
					url.Host = parts[0];
					url.Port = Int32.Parse( parts[1] );
				}

				var apiEndpoint = url.FullPath;

				try
				{
                    log.Debug( $"[WebApi] Call to Endpoint[{apiEndpoint}]" );
					return actExecute( apiEndpoint );
				}
				catch ( Exception ex )
				{
                    log.Error( $"[WebApi] server[{domain}] url[{apiEndpoint}] failed, {ex.Message}", ex );
					exs.Add( ex );
				}
			}

			throw new Exception( $"[WebApi] 所有的ApiServer皆通訊失敗, urlPath[{urlPath}]", exs.Count >= 1 ? exs[0] : null );
		}


		/// <summary>向ApiServers發起http-get, 若每部server都不成功將拋出異常</summary>
		public static String CallApiGet( String urlPath )
		{
			Func<String, String> onExecute = ( endpoint ) =>
			{
				return HttpUtils.Get( endpoint );
			};

			return CallToServers( urlPath, onExecute );
		}

		public static String CallApiGet<TData>( String urlPath, TData data ) where TData : class
		{
			var queryString = HttpUtils.ConvertToQueryParameter( data );

			Func<String, String> onExecute = ( endpoint ) =>
			{
				return HttpUtils.Get( $"{urlPath}?{queryString}" );
			};

			return CallToServers( urlPath, onExecute );
		}

		/// <summary>向ApiServers發起http-post, 若每部server都不成功, 將回傳valueOnFailed內容</summary>
		public static String CallApiPost<TData>( String urlPath, TData data ) where TData : class
		{
			Func<String, String> onExecute = ( endpoint ) =>
			{
				return HttpUtils.Post( endpoint, data );
			};

			return CallToServers( urlPath, onExecute );
		}
	}
}
