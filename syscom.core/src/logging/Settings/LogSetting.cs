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
	/// <summary>Log的公用設定值</summary>
	public class LogSetting
	{
		public String? DirPath { get; private set; }
		public String NameFormat = Utils.FORMAT_FILENAME_DEFAULT;

		public Int32 RotateDays = 10;
		public Int32 RotateFileMB = 50;
		public Int32 IntervalWriteMs = 500;

		public LogSetting()
		{
			if ( String.IsNullOrWhiteSpace( DirPath ) ) SetPathBy( Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "Logs" ) );
		}

		// 輸出路徑, 可以是完整路徑或相對路徑, 相對路徑會由BaseDirectory產生
		public void SetPathBy( String path )
		{
			try
			{
				//如果是相對路徑 ( 不含/或\號 )
				if( !path.Contains( "/" ) && !path.Contains( "\\" ) ) path = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, path );

				var dir = new DirectoryInfo( path );
				if ( !dir.Exists ) dir.Create();

				DirPath = dir.FullName;
			}
			catch ( Exception ex )
			{
				throw new InvalidOperationException( $"[logging] 無法設將輸出目錄設定為[{path}], {ex.Message}", ex );
			}
		}
	}
}
