using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;

namespace SurveyCRMWebApiV2.Controllers
{
    [RoutePrefix( "api/NotFound" )]
	public class NotFoundController : ApiBaseController
	{
		//==========================================================================================
		//==========================================================================================
		


		//==========================================================================================
		//
		//==========================================================================================
		/// <summary></summary>
		[HttpGet]
		public String Index()
		{
			var msg = $"NoApi: {Request.RequestUri.OriginalString}";
			return msg;
		}


		internal static Func<HttpRequestMessage,HttpResponseMessage> OnApiNotFound;

		[HttpGet, HttpPost, HttpPut, HttpPatch, HttpDelete, HttpHead, HttpOptions, AcceptVerbs("patch")]
		public String ApiNotFound()
		{
			// Log.LogFile( $"[GetApiNotFound] Request.Uri[ { Request.RequestUri } ]" );

			// if( OnApiNotFound != null )
			// {
			// 	return OnApiNotFound( Request );
			// }
			//
			// var msg = $"NoApiEndPoint: {Request.RequestUri}";
			// Log.LogFile( $"[ApiNotFound] {msg}" );

			// return Request.CreateResponse( HttpStatusCode.BadGateway, "NoApi", Configuration.Formatters.JsonFormatter );
			return "ApiNotFound";
		}
	}

	public static class HandleWebApiConfigExtension
	{
		public static void SetMapHandleApiNotFound( this HttpConfiguration config, Func<HttpRequestMessage,HttpResponseMessage> handler = null )
		{
			if( handler != null ) { NotFoundController.OnApiNotFound = handler; }

			//對應上面的
			config.Routes.MapHttpRoute(
				name: "Route:ApiNotFound",
				routeTemplate: "{*url}",
				defaults: new { controller = "NotFound", action = "ApiNotFound" }
			);

			// Catch 404
			config.Services.Replace( typeof( IHttpControllerSelector ), new HttpNotFoundAwareDefaultHttpControllerSelector( config ) );
			config.Services.Replace( typeof( IHttpActionSelector ), new HttpNotFoundAwareControllerActionSelector() );
		}
	}

	public class HttpNotFoundAwareDefaultHttpControllerSelector : DefaultHttpControllerSelector
	{
		public HttpNotFoundAwareDefaultHttpControllerSelector( HttpConfiguration configuration ) : base( configuration )
		{
		}

		public override HttpControllerDescriptor SelectController( HttpRequestMessage request )
		{
			HttpControllerDescriptor decriptor = null;
			try
			{
				decriptor = base.SelectController( request );
			}
			catch ( HttpResponseException ex )
			{
				var code = ex.Response.StatusCode;
				if ( code != HttpStatusCode.NotFound ) throw;
				var routeValues = request.GetRouteData().Values;
				routeValues["controller"] = "NotFound";
				routeValues["action"] = "ApiNotFound";
				decriptor = base.SelectController( request );
			}

			return decriptor;
		}
	}

	public class HttpNotFoundAwareControllerActionSelector : ApiControllerActionSelector
	{
		public override HttpActionDescriptor SelectAction( HttpControllerContext controllerContext )
		{
			HttpActionDescriptor decriptor = null;
			try
			{
				decriptor = base.SelectAction( controllerContext );
			}
			catch ( HttpResponseException ex )
			{
				var code = ex.Response.StatusCode;
				if ( code != HttpStatusCode.NotFound && code != HttpStatusCode.MethodNotAllowed ) throw;

				var routeData = controllerContext.RouteData;
				routeData.Values["action"] = "ApiNotFound";
				var httpController = new NotFoundController();
				controllerContext.Controller = httpController;
				controllerContext.ControllerDescriptor = new HttpControllerDescriptor( controllerContext.Configuration, "NotFound", httpController.GetType() );
				decriptor = base.SelectAction( controllerContext );
			}

			return decriptor;
		}
	}
}
