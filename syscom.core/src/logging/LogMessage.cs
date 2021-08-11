using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace syscom.logging
{
	public class LogMessage : ILogMessage
	{
		const String CHAR_SPLIT = @"|";
		const String CHAR_M = @":";

		internal String CallerName;
		internal String CallerPath;
		internal Int32 CallerLineNumber;

		public DateTime DateTime { get; set; }
		public LogLevel Level { get; set; }
		public String LoggerId { get; set; }
		public Int32 ThreadId { get; set; }
		public String Code { get; set; }
		public String Message { get; set; }
		public Exception Exception { get; set; }


		internal LogMessage( LogLevel lv, String msg, Exception? ex = null, String? code = null )
		{
			ThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
			DateTime = DateTime.Now;

			Level = lv;
			Code = code;
			Message = msg;
			Exception = ex;
		}

		public override String ToString() { return DumpMessage(); }


		internal String _String;

		public String DumpMessage( Boolean withoutException = false )
		{
			if ( String.IsNullOrEmpty( _String ) )
			{
				_String = String.Concat
				(
					DateTime.ToString( "HH:mm:ss.ffffff" ), CHAR_SPLIT, ThreadId.ToString( "00" ), CHAR_SPLIT, Level.ToString().PadRight( 5, ' ' ), CHAR_SPLIT,
					String.IsNullOrEmpty( LoggerId ) ? String.Empty : String.Concat( LoggerId, CHAR_SPLIT ),

					Level == LogLevel.Info ? String.Empty : String.IsNullOrEmpty( CallerName ) ? String.Empty : String.Concat( CallerName, CHAR_SPLIT ),

					String.IsNullOrEmpty( Code ) ? String.Empty : String.Concat( "Code", CHAR_M, Code, CHAR_SPLIT ),
					" ",
					Message,
					Exception == null ? String.Empty : String.Concat( CHAR_SPLIT, Exception.DumpDetail() )
				);
			}

			return _String;
		}


		public String ToShortMessage()
		{
			return String.Concat
			(
				DateTime.ToString( "HH:mm:ss.ffffff" ), CHAR_SPLIT, Level, CHAR_SPLIT,
				String.IsNullOrEmpty( Code ) ? String.Empty : String.Concat( "C:", CHAR_M, Code, CHAR_SPLIT ),
				Exception == null ? String.Empty : String.Concat( CHAR_SPLIT, Exception.GetType().Name, CHAR_SPLIT ),
				" ",
				Message
			);
		}
	}
}
