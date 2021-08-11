using System;
using System.Diagnostics;

namespace syscom
{
	public class CodeUtils
	{
		public static TimeSpan MeasureBy( Action action )
		{
			var sw = Stopwatch.StartNew();

			action();
			sw.Stop();

			return sw.Elapsed;
		}

		public static TimeSpan MeasureBy( Action action, Int32 count )
		{
			var sw = Stopwatch.StartNew();

			for ( var idx = 0; idx < count; idx++ ) action();

			sw.Stop();

			return sw.Elapsed;
		}
	}
}
