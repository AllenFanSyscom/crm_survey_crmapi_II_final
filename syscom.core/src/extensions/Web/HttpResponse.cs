
using System;

namespace System.Web
{
	public enum HttpResponseStatus
	{
		Found,
		Moved,
		NotFound,
		OK,
	}

	public enum HttpContentType
	{
		Text,
		Json,
		JavaScript,
		Html,
	}

	public static class HttpResponseExtensions
	{
		const String baseHtmlCode = "<html><body>{0}</body></html>";
		const String javascriptTag = "<script type=\"text/javascript\">{0}</script>";

#if net45

		public static void RenderHtml( this HttpResponse response, String html )
		{
			response.SetContentTypeTo( HttpContentType.Html );
			var s = $"<html><body>{(Object) html}</body></html>";
			response.Write( s );
		}

		public static void RenderHtmlJavaScript( this HttpResponse response, String jsCode )
		{
			var html = $"<script type=\"text/javascript\">{(Object) jsCode}</script>";
			response.RenderHtml( html );
		}

		public static void SetNonCache( this HttpResponse response )
		{
			response.Cache.SetCacheability( HttpCacheability.NoCache );
			response.Cache.SetNoStore();
			response.Cache.SetExpires( DateTime.MinValue );
		}

		public static void StatusSetTo( this HttpResponse response, HttpResponseStatus status )
		{
			switch ( status )
			{
				case HttpResponseStatus.Found:
				response.Status = "302 Found";
				break;
				case HttpResponseStatus.Moved:
				response.Status = "301 Moved Permanently";
				break;
				case HttpResponseStatus.NotFound:
				response.Status = "404 Not Found";
				break;
				default:
				response.Status = "200 OK";
				break;
			}
		}

		public static void AddHeadLocation( this HttpResponse response, String redirectTo )
		{
			response.AppendHeader( "Location", redirectTo );
		}

		public static void SetContentTypeTo( this HttpResponse response, HttpContentType type )
		{
			switch ( type )
			{
				case HttpContentType.Json:
				response.ContentType = "application/json";
				break;
				case HttpContentType.JavaScript:
				response.ContentType = "application/x-javascript";
				break;
				case HttpContentType.Html:
				response.ContentType = "text/html";
				break;
				default:
				response.ContentType = "text/plain";
				break;
			}
		}
#endif
	}
}


