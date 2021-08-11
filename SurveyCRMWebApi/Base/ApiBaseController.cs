using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace SurveyCRMWebApiV2.Controllers
{
    public enum MediaType
	{
		TextPlain
	}


	[RoutePrefix( "api/ApiBaseController" )]
	public abstract class ApiBaseController : ApiController
	{
		//==========================================================================================
		//==========================================================================================
		const String MediaType_JSON = "application/json";
		const String MediaType_TextPlain = "text/plain";

		//==========================================================================================
		// utils methods
		//==========================================================================================
		protected HttpResponseMessage ReturnBy( HttpStatusCode code, String message )
		{
			var rep = Request.CreateResponse( code );
			rep.Content = new StringContent( message, Encoding.UTF8 );
			return rep;
		}

		protected HttpResponseMessage ReturnBy( String message )
		{
			var rep = Request.CreateResponse( HttpStatusCode.OK );
			rep.Content = new StringContent( message, Encoding.UTF8 );

			return rep;
		}

		protected HttpResponseMessage ReturnJsonBy( HttpStatusCode code, String jsonText )
		{
			var rep = Request.CreateResponse( code );
			rep.Content = new StringContent( jsonText, Encoding.UTF8, MediaType_JSON );
			return rep;
		}

		protected HttpResponseMessage ReturnJsonBy( String jsonText )
		{
			var rep = Request.CreateResponse( HttpStatusCode.OK );
			rep.Content = new StringContent( jsonText, Encoding.UTF8, MediaType_JSON );
			return rep;
		}

		protected HttpResponseMessage ReturnJsonBy( Object message )
		{
			var rep = Request.CreateResponse( HttpStatusCode.OK );
			rep.Content = new StringContent( message.ToString(), Encoding.UTF8, MediaType_JSON );
			return rep;
		}

		/// <summary>回傳{error:message}物件</summary>
		protected HttpResponseMessage ReturnErrorJsonBy( String message )
		{
			var rep = Request.CreateResponse( HttpStatusCode.OK );
			rep.Content = new StringContent( $"{{ \"error\":\"{message}\" }}", Encoding.UTF8, MediaType_JSON );
			return rep;
		}
	}
}
