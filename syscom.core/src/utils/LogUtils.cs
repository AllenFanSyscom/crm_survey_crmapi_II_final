using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using syscom.logging;
using syscom.logging.transports;
using syscom;

namespace syscom
{
	public static class LogUtils
	{
		public static ILogger GetLoggerForCurrentClass()
		{
			var className = StackUtils.GetClassFullName();

			if ( String.IsNullOrEmpty( className ) ) throw new Exception( "無法取得呼叫者的ClassName" );

			className = className.Substring( className.LastIndexOf( "." ) + 1 );

			return GetLogger( className );
		}

		public static ILogger GetLogger( String id = "" )
		{
			var logger = new Logger( id );
			logger.Transporters.Add( new ConsoleTransport() );

			if ( Utils.GlobalFileTransporter != null )
			{
				logger.Transporters.Add( Utils.GlobalFileTransporter );
				InitializeInformation( logger );
				foreach ( var item in Logger.Loggers )
				{
					if ( !item.Transporters.Contains( Utils.GlobalFileTransporter ) ) item.Transporters.Add( Utils.GlobalFileTransporter );
				}
			}

			return logger;
		}

		static Boolean isInit;

		internal static void InitializeInformation( ILogger log, Process? proc = null )
		{
			if ( isInit ) return;
			if ( Utils.GlobalFileTransporter == null ) return;
			if ( !log.Transporters.Contains( Utils.GlobalFileTransporter ) ) log.Transporters.Add( Utils.GlobalFileTransporter );

			if ( proc == null ) proc = Process.GetCurrentProcess();

			var domain = AppDomain.CurrentDomain;

			log.Trace( "==============================================================================", callerName: "" );
			log.Trace( $"= 程式啟動 - {proc.StartTime:yyyy-MM-dd HH:mm:ss.ffffff}", callerName: "" );
			log.Trace( $"= 本機地址 [ {NetUtils.GetCurrentIPv4sString()} ]", callerName: "" );
			log.Trace( $"= Process ID[ {proc.Id} ] ThreadId[ {Thread.CurrentThread.ManagedThreadId} ]", callerName: "" );
			log.Trace( $"= CommandLine: {Environment.GetCommandLineArgs().ToArray().JoinBy( " " )}", callerName: "" );
			log.Trace( $"= Process Name[{proc.ProcessName} - {( Environment.Is64BitProcess ? "64" : "86" )}] Path[{domain.BaseDirectory}]", callerName: "" );
			log.Trace( $"= Machine [{Environment.MachineName}] - {Environment.OSVersion} ({( Environment.Is64BitOperatingSystem ? "x64" : "x86" )})", callerName: "" );
			log.Trace( $"= User [{Environment.UserDomainName} / {Environment.UserName} ({CultureInfo.CurrentCulture.Name})", callerName: "" );
			log.Trace( "==============================================================================", callerName: "" );

			isInit = true;
		}
	}
}
