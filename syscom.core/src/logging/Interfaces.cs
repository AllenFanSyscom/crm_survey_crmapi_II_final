using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using libs.DryIoc;
using syscom.logging;

namespace syscom
{
	/// <summary>Log的層級</summary>
	public enum LogLevel : byte
	{
		None = 0x00,
		Trace = 0x01,
		Debug = 0x01 << 1,
		Info = 0x01 << 2,
		Warn = 0x01 << 3,
		Error = 0x01 << 4,

		Fatal = 0x01 << 5
		//All = 0x01 << 6 //可視情況看是否需要
	};

	//public delegate void OnLogDispatchHandler( String log );
	//public delegate void OnLogMessageDispatchHandler( LogMessage log );

	public interface ILogMessage
	{
		DateTime DateTime { get; set; }
		LogLevel Level { get; set; }
		String LoggerId { get; set; }
		Int32 ThreadId { get; set; }
		String Code { get; set; }
		String Message { get; set; }
		Exception Exception { get; set; }

		/// <summary>傾印LogMessage成為文字型態</summary>
		/// <param name="withoutException">是否要包含Exception的全部資訊</param>
		String DumpMessage( Boolean withoutException = false );

		/// <summary></summary>
		String ToShortMessage();
	}

	public interface ILoggerTransporter : IDisposable
	{
		void OnMessage( ILogMessage message );
	}


	/// <summary>Log的記錄器</summary>
	public interface ILogger
	{
		String Id { get; set; }

		List<ILoggerTransporter> Transporters { get; }

		Action<ILogMessage> OnDispatch { get; set; }

		void Trace( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? callerName = null, [CallerFilePath] String? callerPath = null, [CallerLineNumber] Int32 callerLineNumber = 0 );
		void Debug( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? callerName = null, [CallerFilePath] String? callerPath = null, [CallerLineNumber] Int32 callerLineNumber = 0 );
		void Info( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? callerName = null, [CallerFilePath] String? callerPath = null, [CallerLineNumber] Int32 callerLineNumber = 0 );
		void Warn( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? callerName = null, [CallerFilePath] String? callerPath = null, [CallerLineNumber] Int32 callerLineNumber = 0 );
		void Error( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? callerName = null, [CallerFilePath] String? callerPath = null, [CallerLineNumber] Int32 callerLineNumber = 0 );
		void Fatal( String message, Exception? ex = null, String? code = null, [CallerMemberName] String? callerName = null, [CallerFilePath] String? callerPath = null, [CallerLineNumber] Int32 callerLineNumber = 0 );
	}

	public static class ILoggerExtensions
	{
		/// <summary></summary>
		public static void SetILoggerInject( this Container container )
		{
			container.Register<ILogger>
			(
				made: Made.Of( () => new Logger( Arg.Index<String>( 0 ) ), reqInfo => reqInfo.ImplementationType.ToString() ),
				setup: Setup.With( allowDisposableTransient: true )
			);
		}
		public static ConsoleColor GetColor( this LogLevel lv )
		{
			if ( lv == LogLevel.Trace ) return ConsoleColor.DarkGray;
			if ( lv == LogLevel.Debug ) return ConsoleColor.DarkCyan;
			if ( lv == LogLevel.Warn ) return ConsoleColor.DarkYellow;
			if ( lv == LogLevel.Error ) return ConsoleColor.Red;
			if ( lv == LogLevel.Fatal ) return ConsoleColor.Magenta;

			//normal it's white
			return ConsoleColor.White;
		}
	}
}
