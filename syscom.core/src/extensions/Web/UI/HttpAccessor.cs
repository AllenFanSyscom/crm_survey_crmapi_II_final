#if net45
using System.Web.SessionState;

namespace System.Web.UI
{
	public class HttpAccessor
	{
		internal static HttpContext Http { get => HttpContext.Current; }

		public static HttpSessionState Session { get => Http.Session; }

		public static HttpResponse Response { get => Http.Response; }
		public static HttpRequest Request { get => Http.Request; }
	}
}
#endif
