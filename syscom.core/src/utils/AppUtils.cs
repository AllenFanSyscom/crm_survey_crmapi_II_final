using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using syscom.utils.win32;
using syscom.Win32;
using System.Threading;


namespace syscom
{
	public static class AppUtils
	{
		static Process Current = ProcessUtils.Current;

		public static DirectoryInfo CurrentAppDirectory => new DirectoryInfo( AppDomain.CurrentDomain.BaseDirectory );

		//================================================================================================================================================
		// Public - Lock Process
		//================================================================================================================================================

		public static Boolean CurrentIs64Bit()
		{
			switch ( IntPtr.Size )
			{
				case 4:
					return false;
				case 8:
					return true;
				default:
					throw new NotSupportedException( "Current Runtime Platform is Unknown" );
			}
		}

		public static void AttachUnhandledExceptionToFileLog()
		{
			var procName = Process.GetCurrentProcess().ProcessName;
			var procArgs = String.Join( ".", Environment.GetCommandLineArgs().Skip( 1 ).ToArray() );

			var args = String.IsNullOrEmpty( procArgs ) ? "" : "_" + procArgs;
			var basicName = $"_Unhandled_{procName}{args}_{DateTime.Now:yyyyMMddHHmmss}.log";
			var lockerName = AppDomain.CurrentDomain.BaseDirectory + FileUtils.GetSafeFileNameBy( basicName );
			if ( lockerName.Length >= 259 ) lockerName = lockerName.Substring( 0, 253 ) + ".log";

			AppDomain.CurrentDomain.UnhandledException += ( sender, senderArgs ) =>
			{
				var content = ( (Exception) senderArgs.ExceptionObject ).DumpDetail();
				FileUtils.AppendToFileBy( lockerName, content );
			};
		}

		//================================================================================================================================================
		// Public - Lock Process
		//================================================================================================================================================


		/// <summary>
		/// 呼叫此Instance將以本程式名稱及參數進行同步鎖, 同樣的參數程式在一台機器中只能啟動一支
		/// <para>若啟動失敗將回傳InvalidOperationException，並註明程式名稱及參數</para>
		/// </summary>
		public static void OnlyAllowOnceInstanceWhenEqualArgs()
		{
			var askThreadId = Thread.CurrentThread.ManagedThreadId;

			var procName = Current.ProcessName;
			var procArgs = String.Join( ".", Environment.GetCommandLineArgs().Skip( 1 ).ToArray() );
			var basicName = $"Global\\AppLock_{procName}_{procArgs}";
			var lockerName = FileUtils.GetSafeFileNameBy( basicName );
			if ( lockerName.Length >= 259 ) lockerName = lockerName.Substring( 0, 259 );

			Boolean createdNew;
			var locker = new Mutex( true, lockerName, out createdNew );
			if ( !createdNew ) throw new InvalidOperationException( "[多重啟動限制] 執行檔 " + procName + " 已設定為相同參數不能多重啟動, 請先關閉相同程式" );

			AppDomain.CurrentDomain.ProcessExit += ( s, a ) =>
			{
				if ( Thread.CurrentThread.ManagedThreadId != askThreadId ) return;

				locker?.ReleaseMutex();
				locker = null;
			};
			AppDomain.CurrentDomain.UnhandledException += ( s, a ) =>
			{
				if ( Thread.CurrentThread.ManagedThreadId != askThreadId ) return;

				locker?.ReleaseMutex();
				locker = null;
			};
		}

		public static Boolean AskGlobalMutexBy( String lockKey )
		{
			var askThreadId = Thread.CurrentThread.ManagedThreadId;
			var key = $@"Global\Locker_{lockKey}";
			var lockerName = FileUtils.GetSafeFileNameBy( key );
			if ( lockerName.Length >= 259 ) lockerName = lockerName.Substring( 0, 259 );

			Boolean createdNew;
			var locker = new Mutex( true, lockerName, out createdNew );

			AppDomain.CurrentDomain.ProcessExit += ( s, a ) =>
			{
				if ( Thread.CurrentThread.ManagedThreadId != askThreadId ) return;

				locker?.ReleaseMutex();
				locker = null;
			};
			return createdNew;
		}

		public static List<Process> DetectOtherSameProcesses( Process current )
		{
			var otherSameProcesses = Process.GetProcessesByName( current.ProcessName )
			                                .Where( process => process.Id != current.Id && process.MainModule.FileName == current.MainModule.FileName )
			                                .ToList();
			return otherSameProcesses;
		}

		/// <summary>設定同名的ProcessName若已在執行中, 則拋出異常</summary>
		public static void OnlyAllowOnceInstanceByProgramName()
		{
			var processes = DetectOtherSameProcesses( Current );
			if ( processes.Count > 0 ) throw new InvalidOperationException( "[多重啟動限制] 執行檔 " + Current.MainModule.FileName + " 已設定為不能多重啟動, 請先關閉相同程式" );
		}

		/// <summary>檢測是否有同名的Process在執行 (popupOtherToTop參數指出是否將其顯示到最上層)</summary>
		public static Boolean DetectHaveSameProcessExist( Boolean popupOtherToTop = true )
		{
			var processes = DetectOtherSameProcesses( Current );
			if ( processes.Count == 0 ) return false;

			if ( popupOtherToTop )
			{
				var process = processes.First();
				User32.ShowWindow( process.MainWindowHandle, ShowWindowEnum.ShowNormal );
				User32.SetForegroundWindow( process.MainWindowHandle );
			}

			return true;
		}
	}
}
