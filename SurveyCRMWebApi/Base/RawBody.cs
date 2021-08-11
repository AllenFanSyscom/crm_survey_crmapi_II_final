using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Metadata;

namespace SurveyCRMWebApiV2.Controllers
{
    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Parameter )]
	public sealed class RawBodyAttribute : ParameterBindingAttribute
	{
		public override HttpParameterBinding GetBinding( HttpParameterDescriptor parameter )
		{
			if ( parameter == null ) throw new ArgumentException( "Invalid parameter" );

			return new RawBodyParameterBinding( parameter );
		}
	}

	public class RawBodyParameterBinding : HttpParameterBinding
	{
		public RawBodyParameterBinding( HttpParameterDescriptor descriptor ) : base( descriptor ) { }

		public override Task ExecuteBindingAsync
		(
			ModelMetadataProvider metadataProvider,
			HttpActionContext actionContext,
			CancellationToken cancellationToken
		)
		{
			var binding = actionContext
			              .ActionDescriptor
			              .ActionBinding;

			if ( binding.ParameterBindings.Length > 1 ||
			     actionContext.Request.Method == HttpMethod.Get )
				return EmptyTask.Start();

			var type = binding
			           .ParameterBindings[0]
			           .Descriptor.ParameterType;

			if ( type == typeof( String ) )
			{
				return actionContext.Request.Content
				                    .ReadAsStringAsync()
				                    .ContinueWith( ( task ) =>
				                    {
					                    var stringResult = task.Result;
					                    SetValue( actionContext, stringResult );
				                    }, cancellationToken );
			}

			if ( type == typeof( Newtonsoft.Json.Linq.JObject ) )
			{
				return actionContext.Request.Content
				                    .ReadAsStringAsync()
				                    .ContinueWith( ( task ) =>
				                    {
					                    var result = task.Result;
					                    var json = new Newtonsoft.Json.Linq.JObject();
					                    try{ json = Newtonsoft.Json.Linq.JObject.Parse( result ); }
					                    catch( Exception ex )
					                    {
						                    json.Add( "raw:error", ex.Message );
						                    json.Add( "raw:body", result );
					                    }

					                    SetValue( actionContext, json );
				                    }, cancellationToken );
			}

			if ( type == typeof( Byte[] ) )
			{
				return actionContext.Request.Content
				                    .ReadAsByteArrayAsync()
				                    .ContinueWith( ( task ) =>
				                    {
					                    var result = task.Result;
					                    SetValue( actionContext, result );
				                    }, cancellationToken );
			}



			throw new NotSupportedException( "Only Supported String & Byte[] and libs.Json.Linq.Newtonsoft.Json.Linq.JObject" );
		}


		public override Boolean WillReadBody { get { return true; } }
	}


	public class EmptyTask
	{
		public static Task Start()
		{
			var taskSource = new TaskCompletionSource<AsyncVoid>();
			taskSource.SetResult( default( AsyncVoid ) );
			return taskSource.Task as Task;
		}

		private struct AsyncVoid
		{
		}
	}
}
