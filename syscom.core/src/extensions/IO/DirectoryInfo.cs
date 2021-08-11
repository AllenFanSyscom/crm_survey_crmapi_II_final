using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
	public static class DirectoryInfoExtensions
	{
		public static void ForceCreate( this DirectoryInfo dir, Int32 delayTime = 100 )
		{
			var watcher = new Stopwatch();
			watcher.Start();
			while ( !Directory.Exists( dir.FullName ) )
			{
				Directory.CreateDirectory( dir.FullName );
				Threading.Thread.Sleep( delayTime );
				watcher.Stop();
				if ( watcher.Elapsed.TotalMinutes > 1 ) throw new Exception( "建立[ " + dir.FullName + " ]逾時1分鐘, 請檢查是否有異常" );
				watcher.Start();
			}

			watcher.Stop();
		}

		public static void CopyBy( this DirectoryInfo dirSource, String destPath, Boolean overwrite = false, Boolean keepTime = true ) { dirSource.MoveBy( destPath, false, overwrite, keepTime ); }

		/// <summary>清除該資料夾底下的所有檔案及子目錄</summary>
		public static void CleanContent( this DirectoryInfo dir )
		{
			if ( !Directory.Exists( dir.FullName ) ) return;
			var files = dir.GetFiles();
			foreach ( var file in files )
			{
				file.IsReadOnly = false;
				file.Delete();
			}

			var dirs = dir.GetDirectories();
			foreach ( var d in dirs ) dir.ForceDelete();
		}

		public static void MoveBy( this DirectoryInfo dirSource, String destPath, Boolean deleteSource = true, Boolean overwrite = false, Boolean keepTime = true )
		{
			var dirDest = new DirectoryInfo( destPath );
			if ( !dirDest.Exists ) dirDest.Create();

			var files = dirSource.GetFiles();
			foreach ( var file in files )
			{
				var newPath = Path.Combine( dirDest.FullName, file.Name );

				file.CopyBy( newPath, deleteSource, overwrite, keepTime );
			}

			var dirs = dirSource.GetDirectories();
			foreach ( var dir in dirs ) dir.MoveBy( Path.Combine( destPath, dir.Name ), deleteSource, overwrite );
		}

		public static void ForceDelete( this DirectoryInfo dirSource )
		{
			if ( !Directory.Exists( dirSource.FullName ) ) return;

			var files = dirSource.GetFiles();
			foreach ( var file in files ) file.IsReadOnly = false;

			var dirs = dirSource.GetDirectories();
			foreach ( var dir in dirs ) dir.ForceDelete();
			dirSource.Delete( true );
		}
	}
}