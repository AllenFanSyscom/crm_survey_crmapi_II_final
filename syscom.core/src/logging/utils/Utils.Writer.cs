using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

// ReSharper disable ExplicitCallerInfoArgument
namespace syscom.logging
{
	/*============================================================================================================
	// 產生寫檔Thread的入口
	============================================================================================================*/
	partial class Utils
    {
        static void SaveExceptionFile(String msg, Exception ex = null)
        {
            using var sw = File.AppendText(AppDomain.CurrentDomain.BaseDirectory + "_LoggerException_" + DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".log");
            sw.Write(msg);
            if (ex != null) sw.WriteLine(ex.ToString());
            sw.Flush();
        }

        static Thread MakeWriteThreadBy( String name, CancellationTokenSource canceller, ConcurrentQueue<ILogMessage> queue, String targetFullPath, LogSetting setting, Action<FileInfo>? onOutputFileGenerated = null )
		{
			var actWrite = MakeLogWriteActionBy( targetFullPath, setting, onOutputFileGenerated );

			actWrite( queue );

			void Core()
			{
				Restart:
				try
				{
					while ( !canceller.IsCancellationRequested )
					{
						actWrite( queue );
						Thread.Sleep( setting.IntervalWriteMs );
					}

					actWrite( queue );
				}
				catch ( ThreadAbortException )
				{
					/*在Web底下可能因為w3wp終止而強制停止*/
				}
				catch ( Exception ex )
				{
					var msg = "[Log] Log寫檔異常, " + ex.Message + "\r\n" + ex.DumpDetail();
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine( msg );
					SaveExceptionFile( msg );

					goto Restart;
				}
			}

			var thread = new Thread( Core ) { Name = "Log:" + name };
			thread.Start();

#if net45
			System.Windows.Forms.Application.ApplicationExit += ( sender, args ) =>
			{
				if ( canceller != null && !canceller.IsCancellationRequested ) canceller.Cancel();
			};
#endif
			AppDomain.CurrentDomain.ProcessExit += ( sender, args ) =>
			{
				if ( canceller != null && !canceller.IsCancellationRequested ) canceller.Cancel();
			};

			return thread;
		}
	}

	/*============================================================================================================
	// 寫檔Thread
	============================================================================================================*/
	partial class Utils
	{
		static readonly CultureInfo enUS = new CultureInfo( "en-US" );

		public static Action<ConcurrentQueue<ILogMessage>> MakeLogWriteActionBy( String settingFullPath, LogSetting setting, Action<FileInfo>? onOutputFileGenerated = null )
		{
			var pathOfProgramFiles = Environment.GetEnvironmentVariable( "ProgramFiles" );

			// prevent vs-debug attached
			if ( settingFullPath.Contains( pathOfProgramFiles ?? "ProgramFiles" ) )
			{
				return logQueue =>
				{
					while ( logQueue.TryDequeue( out var msg ) ) Debug.WriteLine( msg.ToString() );
				};
			}

			var templateFI = new FileInfo( settingFullPath );
			var templateName = templateFI.Name.Replace( templateFI.Extension, "" ); // w3wp-ap.test.v4.0-yyyyMMdd
			var templatePrefix = templateName.Replace( KEY_REPLACE_APPDATE, "" );   // w3wp-ap.test.v4.0-

			var failCount = 0;
			var currentMaxMB = setting.RotateFileMB * 1024L * 1024L;

			var dtLastCheckRotate = DateTime.Now;

			void newAction( ConcurrentQueue<ILogMessage> logQueue )
			{
				var fullPath = settingFullPath.Replace( KEY_REPLACE_APPDATE, DateTime.Now.ToString( KEY_REPLACE_APPDATE ) );

				var fi = new FileInfo( fullPath );
				if ( fi.Directory == null ) throw new Exception( "[Logger] Wrong file path: " + fullPath );
				var dirPath = fi.Directory.FullName;
				onOutputFileGenerated?.Invoke( fi );

				if ( fi.Exists && fi.Length >= currentMaxMB )
				{
					var count = 1;
					var nfi = new FileInfo( Path.Combine( dirPath, fi.Name.Replace( fi.Extension, "" ) + "-" + count + fi.Extension ) );
					while ( nfi.Exists )
					{
						count++;
						nfi = new FileInfo( Path.Combine( dirPath, fi.Name.Replace( fi.Extension, "" ) + "-" + count + fi.Extension ) );
					}

					fi.MoveTo( nfi.FullName );
				}

				var sb = new StringBuilder();
				var watch = Stopwatch.StartNew();
				while ( logQueue.TryDequeue( out var msg ) )
				{
					var ss = msg.ToString();
					sb.AppendLine( ss );
					if ( watch.Elapsed.TotalMilliseconds >= setting.IntervalWriteMs ) break;
				}

				var totalMessages = sb.ToString();
				if ( String.IsNullOrWhiteSpace( totalMessages ) ) return;

				RetryOpenFile:

				try
				{
					using ( var fs = File.Open( fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete ) )
					using ( var sw = new StreamWriter( fs, Encoding.UTF8 ) )
					{
						fs.Seek( 0, SeekOrigin.End );
						sw.Write( totalMessages );
						sw.Flush();
					}
				}
				catch ( Exception ex )
				{
					failCount++;
					Thread.Sleep( 5000 + failCount * 1000 );
					if ( failCount >= 10 ) throw new IOException( "路徑[ " + fullPath + " ], 寫檔錯誤, 已超過重試次數, " + ex.Message, ex );
					goto RetryOpenFile;
				}


				//==========================================================================================
				// 檢查過期檔案
				//==========================================================================================
				if ( ( DateTime.Now - dtLastCheckRotate ).Minutes >= 10 )
				{
					try
					{
						var files = fi.Directory.GetFiles( "*.log" );
						foreach ( var fileInfo in files )
						{
							if ( !fileInfo.Name.StartsWith( templatePrefix ) ) continue;

							var name = fileInfo.Name.Replace( ".log", "" );
							var dateStr = name.Replace( templatePrefix, "" ).SplitBy( "-", StringSplitOptions.RemoveEmptyEntries )[0];
							if ( !DateTime.TryParseExact( dateStr, "yyyyMMdd", enUS, DateTimeStyles.None, out var date ) ) { continue; }

							var days = ( DateTime.Now.Date - date ).Days;

							if ( days >= setting.RotateDays )
							{
								Utils.SaveExceptionFile( $"[Log] delete outdated Log File[${fileInfo.Name}] date[{date:yyyy-MM-dd}] days[{ days }]" );
								fileInfo.Delete();
							}
						}

						dtLastCheckRotate = DateTime.Now;
					}
					catch ( Exception ex )
					{
						SaveExceptionFile( $"刪除過期Log檔案異常", ex );
					}
				}
			}

			return newAction;
		}
	}
}
