using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using syscom;
using System.Threading;

namespace syscom.logging
{
	public partial class Logger
	{
		internal static readonly List<ILogger> Loggers = new List<ILogger>();
	}

	public partial class Logger : ILogger
	{
		public override String ToString() { return $"Log:{Id}"; }

		internal ConcurrentQueue<ILogMessage> LogMessages;

		public List<ILoggerTransporter> Transporters { get; internal set; }

		public Action<ILogMessage> OnDispatch { get; set; }

		public String Id { get; set; }

		public Logger() : this(""){}
		public Logger( String id )
		{
			Id = id;
			this.Transporters = new List<ILoggerTransporter>();

			Initialize();
		}


		protected virtual void Initialize()
		{
			lock ( Loggers ) { Loggers.Add( this ); }
		}

		public void Dispose()
		{
			lock ( Loggers ) { Loggers.Remove( this ); }
			lock ( Transporters )
			{
				foreach ( var transporter in Transporters )
				{
					Transporters.Remove( transporter );
					transporter.Dispose();
				}
			}
		}

		public void Log( LogLevel level, String message, Exception? ex = null, String? code = null, [CallerMemberName] String? callerName = null, [CallerFilePath] String? callerPath = null, [CallerLineNumber] Int32 callerLineNumber = 0 )
		{
			var msg = new LogMessage( level, message, ex, code ) { CallerName = callerName, CallerPath = callerPath, CallerLineNumber = callerLineNumber, LoggerId = Id };

			if ( OnDispatch != null )
			{
				foreach ( var evt in OnDispatch.GetInvocationList() )
				{
					try
					{
						evt.DynamicInvoke( msg );
					}
					catch ( Exception exw )
					{
						throw new Exception( $"[Logger] Error InvokeOnDispatch, {exw.Message}", exw );
					}
				}
			}

			foreach ( var transporter in Transporters )
			{
				try{ transporter.OnMessage( msg ); }
				catch( Exception exw )
				{
					throw new Exception( $"[Logger] Error Transporter, { exw.Message }", exw );
				}
			}
		}

		public void Trace( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? cn = null, [CallerFilePath] String? cp = null, [CallerLineNumber] Int32 cln = 0 ) { Log( LogLevel.Trace, message, ex, code, cn, cp, cln ); }
		public void Debug( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? cn = null, [CallerFilePath] String? cp = null, [CallerLineNumber] Int32 cln = 0 ) { Log( LogLevel.Debug, message, ex, code, cn, cp, cln ); }
		public void Info( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? cn = null, [CallerFilePath] String? cp = null, [CallerLineNumber] Int32 cln = 0 ) { Log( LogLevel.Info, message, ex, code, cn, cp, cln ); }
		public void Warn( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? cn = null, [CallerFilePath] String? cp = null, [CallerLineNumber] Int32 cln = 0 ) { Log( LogLevel.Warn, message, ex, code, cn, cp, cln ); }
		public void Error( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? cn = null, [CallerFilePath] String? cp = null, [CallerLineNumber] Int32 cln = 0 ) { Log( LogLevel.Error, message, ex, code, cn, cp, cln ); }
		public void Fatal( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? cn = null, [CallerFilePath] String? cp = null, [CallerLineNumber] Int32 cln = 0 ) { Log( LogLevel.Fatal, message, ex, code, cn, cp, cln ); }
	}
}
