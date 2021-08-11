using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using syscom;
using syscom.utils.win32;
using syscom.Win32;

namespace System
{
	partial class ExceptionExtensions
	{
		public const String MSG_SPACE = @"   ";

		public static String GetLayersDetail( this Exception ex )
		{
			var msg = new StringBuilder();
			msg.AppendLine( "----------------------------------------------------------------[Layers Start]" );

			var count = 0;
			var target = ex;
			do
			{
				msg.AppendLine( "------------------------------------" );
				msg.AppendFormat( "No.【{0}】:\n", count++ );
				msg.AppendLine( "------------------------------------" );
				msg.AppendFormat( "類型:\t{0} ( {1} )\n", target.GetType(), target.HResult );
				//msg.AppendFormat( "Json:	{0}\n", target.ToJson( "StackTrace", "StackTraceString" ) );
				msg.AppendFormat( "訊息:\t{0}\n", target.Message );
				if ( !String.IsNullOrWhiteSpace( target.StackTrace ) ) msg.AppendFormat( "堆疊:\n{0}\n", target.StackTrace );

				target = target.InnerException;
				if ( target == null ) msg.AppendLine( "------------------------------------" );
			}
			while ( target != null );


			msg.Append( "----------------------------------------------------------------[Layers End]" );
			return msg.ToString();
		}

		public static String GetExceptionTypeStack( this Exception ex )
		{
			if ( ex.InnerException != null )
			{
				var message = new StringBuilder();
				message.AppendLine( GetExceptionTypeStack( ex.InnerException ) );
				message.AppendLine( MSG_SPACE + ex.GetType().ToString() );
				return message.ToString();
			}
			else
			{
				return MSG_SPACE + ex.GetType();
			}
		}

		public static String GetExceptionMessageStack( this Exception ex )
		{
			if ( ex.InnerException != null )
			{
				var message = new StringBuilder();
				message.AppendLine( GetExceptionMessageStack( ex.InnerException ) );
				message.AppendLine( MSG_SPACE + ex.Message );
				return message.ToString();
			}
			else
			{
				return MSG_SPACE + ex.Message;
			}
		}

		public static String GetExceptionCallStack( this Exception e )
		{
			if ( e.InnerException != null )
			{
				var message = new StringBuilder();
				message.AppendLine( GetExceptionCallStack( e.InnerException ) );
				message.AppendLine( String.Empty );
				message.AppendLine( MSG_SPACE + "Next Call Stack:" );
				message.AppendLine( e.StackTrace );
				return message.ToString();
			}
			else
			{
				return e.StackTrace;
			}
		}

		//Note[Raz]: System.InvalidOperationException: Category does not exist.
		//static Object _outputLock = new Object();
		//static TimeSpan GetSystemUpTime()
		//{
		//		var upTime = new PerformanceCounter( "System", "System Up Time" );
		//		upTime.NextValue();
		//		var result = TimeSpan.FromSeconds( upTime.NextValue() );
		//		upTime.RemoveInstance();
		//		return result;
		//}

		public static String DumpDetail( this Exception ex )
		{
			var domain = AppDomain.CurrentDomain;
			var process = Process.GetCurrentProcess();
			var msg = new StringBuilder();
			msg.AppendLine();
			msg.AppendLine( "============================================================================================" );
			msg.AppendLine( "= Exception Dump - " + DateTime.Now.ToString( "dd/MM/yyyy HH:mm:ss" ) );
			msg.AppendLine( "============================================================================================" );
			msg.AppendLine( "執行路徑:			" + domain.BaseDirectory );
			msg.AppendLine( "執行歷時:			" + ( DateTime.Now - process.StartTime ) );
			msg.AppendLine( "本機地址:			" + NetUtils.GetCurrentIPv4sString() );
			msg.AppendLine( "當前語言:			" + CultureInfo.CurrentCulture.Name );
			msg.AppendLine( "GC Memory Size:		" + GC.GetTotalMemory( false ) / ( 1024 * 1024 ) + "MB" );
			//error.AppendLine( "Domain Memory Size:	" + AppDomain.MonitoringSurvivedProcessMemorySize / ( 1024 * 1024 ) + "MB" );
			//error.AppendLine( "系統啟動歷時:		" + GetSystemUpTime() );

			if ( !EnvUtils.IsUnixBasePlatform )
			{
				try
				{
					var memStatus = new MemoryStatusEx();
					Kernel32.GlobalMemoryStatusEx( memStatus );
					msg.AppendLine( "記憶體總量:		" + memStatus.ullTotalPhys / ( 1024 * 1024 ) + "MB" );
					msg.AppendLine( "可用記憶體:		" + memStatus.ullAvailPhys / ( 1024 * 1024 ) + "MB" );
				}
				catch ( Exception exm )
				{
					msg.AppendLine( "取得記憶體:		" + exm.Message );
				}
			}

			msg.Append( "已載入Assemblies: " );
			try
			{
				var assemblies = AppDomain.CurrentDomain.GetAssemblies();
				var txt = assemblies.OrderBy( a => a.GlobalAssemblyCache ).ThenBy( a => a.FullName ).Select( a => a.ToString() ).ToArray().JoinBy( ";" );
				msg.AppendLine( txt );
			}
			catch ( Exception iex )
			{
				msg.AppendLine( "處理載入Assemblies時發生異常, " + iex );
			}


			msg.AppendLine( ex.GetLayersDetail() );


			//msg.AppendLine( "----------------------------------------------------------------" );
			//msg.AppendLine( "程式已載入模組:" );
			//List<ProcessModule>? modules = null;
			//try
			//{
			//	modules = process.Modules.Cast<ProcessModule>().OrderBy( m => m.FileName ).ToList();
			//}
			//catch ( Exception iex )
			//{
			//	msg.AppendLine( "讀取程式已載入模組時異常, " + iex.Message );
			//}
			//if ( modules != null )
			//{
			//	foreach ( var module in modules )
			//	{
			//		try
			//		{
			//			msg.Append( module.FileName + " " );
			//			var fv = module.FileVersionInfo.GetNullOr( fi => fi.ProductVersion );
			//			if ( !String.IsNullOrEmpty( fv ) ) msg.Append( fv );
			//		}
			//		catch ( Exception iex ) { msg.AppendLine( "讀取程式已載入模組時異常, " + iex.Message ); }
			//		msg.Append( Environment.NewLine );
			//	}
			//}

			msg.AppendLine( "============================================================================================" );
			return msg.ToString();
		}
	}
}
