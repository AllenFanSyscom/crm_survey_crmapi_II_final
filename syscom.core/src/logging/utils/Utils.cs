using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using syscom.logging.transports;

// ReSharper disable ExplicitCallerInfoArgument
namespace syscom.logging
{
	public static partial class Utils
	{
		static syscom.ILogger log = syscom.LogUtils.GetLogger();

		//default = 260 , but real testing it's 259,  and more 3 make when retry add suffix number safe
		//public static readonly Int32 MAXLENGTH_FILEPATH = FileUtils.GetCurrentOperationSystemFilePathMaxLength() - 1 - 3;
		public static readonly Int32 MAXLENGTH_FILEPATH = 250;

		public const String SUFFIX_LOGFILE = @".log";
		internal const String KEY_APPNAME = @"{App.Name}";
		internal const String KEY_APPARGS = @"{App.Args}";
		internal const String KEY_APPDATE = @"{App.Date}";

		internal const String KEY_REPLACE_APPDATE = @"yyyyMMdd";
		internal const String FORMAT_FILENAME_DEFAULT = KEY_APPNAME + "_" + KEY_APPARGS + "_" + KEY_APPDATE + SUFFIX_LOGFILE;


		/// <summary>建立寫檔的執行緒</summary>
		public static Thread CreateWriterThreadBy( String name, CancellationTokenSource canceller, ConcurrentQueue<ILogMessage> queue )
		{
			var settings = GetLogSettingsFromConfig();
			var fullPath = GetFormatFullPathBy( settings.DirPath, settings.NameFormat );

			var thread = MakeWriteThreadBy( name, canceller, queue, fullPath, settings );

			return thread;
		}
	}

	/*============================================================================================================
	//
	============================================================================================================*/
	partial class Utils
	{
		internal static readonly LogSetting NowLogSetting;
		internal static readonly ILoggerTransporter? GlobalFileTransporter;

		static Utils()
		{
			NowLogSetting = GetLogSettingsFromConfig();

			if ( !String.IsNullOrEmpty( NowLogSetting.DirPath ) )
			{
				GlobalFileTransporter = new FileTransport( NowLogSetting.DirPath, NowLogSetting.NameFormat, NowLogSetting.IntervalWriteMs );
			}
		}

		public static LogSetting GetLogSettingsFromConfig()
		{
			//==============================================================================
			// 先找Machine, 再找local config
			//==============================================================================
			var settings = new LogSetting();

			//machineConfig?.Logs?.ReplaceLogSettingsTo( settings );
			//currentConfig?.Logs?.ReplaceLogSettingsTo( settings );

			var logPath = ConfigUtils.GetAppSettingOrNull( "Log:Path" );
			if ( !String.IsNullOrEmpty( logPath ) ) settings.SetPathBy( logPath! );


			if ( String.IsNullOrEmpty( settings.DirPath ) ) settings.SetPathBy( Path.GetTempPath() );

			return settings;
		}

		/// <summary>從設定檔中取得值並寫入到LogSettings中</summary>
		// internal static void ReplaceLogSettingsTo( this ConfigOfLogs configOfLogs, Settings settings )
		// {
		// 	if ( configOfLogs == null ) return;
		//
		//
		// 	if ( !String.IsNullOrEmpty( configOfLogs.GlobalSaveFolder ) )
		// 	{
		// 		var dir = new DirectoryInfo( configOfLogs.GlobalSaveFolder );
		// 		if ( !dir.Exists ) dir.Create();
		//
		// 		settings.OutputFolder = dir.FullName;
		// 	}
		//
		// 	if ( !String.IsNullOrEmpty( settings.OutputFolder ) && !settings.OutputFolder.EndsWith( @"\" ) ) settings.OutputFolder += @"\";
		//
		//
		// 	var fileNamePolicy = configOfLogs.FileNamePolicy;
		// 	var existPolicy = configOfLogs.SpecialLogFileNamePolicys?.FirstOrDefault( policy => String.Equals( policy.ApplicationName, Current.ProcessMainModuleName, StringComparison.CurrentCultureIgnoreCase ) );
		// 	if ( existPolicy != null ) fileNamePolicy = existPolicy;
		//
		// 	if ( fileNamePolicy != null )
		// 	{
		// 		if ( fileNamePolicy.SplitByMegaByte != 0 ) settings.SplitMegaByte = fileNamePolicy.SplitByMegaByte;
		// 		if ( !String.IsNullOrWhiteSpace( fileNamePolicy.FileNameFormat ) )
		// 		{
		// 			settings.LogNameFormat = fileNamePolicy.FileNameFormat;
		// 		}
		// 		else
		// 		{
		// 			if ( !fileNamePolicy.IncludeArguments ) settings.LogNameFormat = settings.LogNameFormat.Replace( KEY_APPARGS, "" );
		// 			if ( !fileNamePolicy.IncludeDate ) settings.LogNameFormat = settings.LogNameFormat.Replace( KEY_APPDATE, "" );
		// 			settings.LogNameFormat = settings.LogNameFormat.Replace( @"__", !fileNamePolicy.IncludeArguments && !fileNamePolicy.IncludeDate ? @"" : @"_" );
		// 		}
		// 	}
		// }


		internal static class Current
		{
			public static readonly Process Process = Process.GetCurrentProcess();
			public static readonly String Process_ArgsOriginal = Environment.GetCommandLineArgs().Skip( 1 ).ToArray().JoinBy( "." ).GetValueOrDefault( String.Empty );
			public static readonly String Args = FileUtils.GetSafeFileNameBy( Process_ArgsOriginal );

			public static readonly String ProcessName = Process.ProcessName;
			public static readonly String ProcessMainModuleName = Process.MainModule?.ModuleName!;
		}

		/// <summary>格式化檔案名稱</summary>
		internal static String GetFormatFullPathBy( String path, String fileNameFormat )
		{
			var tmpFormat = fileNameFormat;
			if ( tmpFormat.Contains( KEY_APPNAME ) ) tmpFormat = tmpFormat.Replace( KEY_APPNAME, Current.ProcessName );

			//for runtime replace
			if ( tmpFormat.Contains( KEY_APPDATE ) ) tmpFormat = tmpFormat.Replace( KEY_APPDATE, KEY_REPLACE_APPDATE );

			var isContainArgs = tmpFormat.Contains( KEY_APPARGS );
			if ( isContainArgs )
			{
				var args = Current.Args;
				var tmpNameWithArgs = tmpFormat.Replace( KEY_APPARGS, Current.Args ).Length;
				var tmpFullPath = path.Length + tmpNameWithArgs;
				if ( tmpFullPath > MAXLENGTH_FILEPATH )
				{
					var residueCount = tmpFullPath - ( MAXLENGTH_FILEPATH + 5 ); // 保留 "-0000" 5碼 BUFFER
					// if ( residueCount >= Current.Process_Args.Length ) throw new Exception( $"過長的程式執行參數 Args:({ Current.Process_Args.Length })[{ Current.Process_Args }]" );
					args = args.Substring( 0, args.Length - residueCount );
				}

				tmpFormat = tmpFormat.Replace( KEY_APPARGS, args );
				tmpFormat = tmpFormat.Replace( @"__", "_" ).Replace( "..", "." ).Replace( "_-", "-" );



				//如果是IIS程式, 取代為 w3wp-yyyyMMdd.log
				if( tmpFormat.StartsWith( "w3wp" ) )
				{
					//.-l.webengine4.dll.-a.pipeiisipm17209b9c-e433-422c-bf37-d0744ed297bb.-h.Cinetpubtempapppoolssurl.webapisurl.webapi.config.-w.-m.0.-t.20.-ta.0_

					// old iis
					//w3wp.exe -a \\.\pipe\iisipm7f6f7808-e054-48bb-b3ab-bd50bb45f702 -v v4.0 -l webengine4.dll -h C:\inetpub\temp\apppools\shortner\shortner.config -w  -m 0 -t 20 -ap shortner
					//w3wp-a.pipeiisipm7f6f7808-e054-48bb-b3ab-bd50bb45f702.v4.0-20200804

					tmpFormat = $"w3wp-{ KEY_REPLACE_APPDATE }{ SUFFIX_LOGFILE }";
				}
			}

			return Path.Combine( path, tmpFormat );
		}
	}
}
