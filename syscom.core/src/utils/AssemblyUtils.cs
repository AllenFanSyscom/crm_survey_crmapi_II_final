using syscom.logging;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;

namespace syscom
{
	public static class AssemblyUtils
	{
		/// <summary>取得目前AppDomain下的所有Assembly</summary>
		static Assembly[] GetCurrentAssemblies => AppDomain.CurrentDomain.GetAssemblies();

		/// <summary>取得排除官方後的Assemblies</summary>
		public static IEnumerable<Assembly> GetAssembliesExcludeSystem()
		{
			var assemblies = GetCurrentAssemblies;
			return assemblies.ExceptNetFrameworkDlls();
		}

		/// <summary>依傳入名稱載入Dlls, 若已讀取過則略過</summary>
		public static void LoadAssemblyBy( String assemblyName, Action<Assembly>? loadDoneAction = null, Func<String, Exception, Exception>? exceptionAction = null, IEnumerable<Assembly>? loadedAssemblies = null )
		{
			if ( String.IsNullOrWhiteSpace( assemblyName ) ) throw new ArgumentNullException( "assemblyName", "載入的AssemblyName不得為空值" );
			if ( assemblyName.ToUpper().EndsWith( ".DLL" ) ) assemblyName = assemblyName.Substring( 0, assemblyName.Length - 4 );

			var assemblies = loadedAssemblies ?? GetCurrentAssemblies;
			if ( assemblies.Any( a => a.FullName.Contains( assemblyName ) ) ) return;

			try
			{
				var assembly = Assembly.Load( assemblyName );
				loadDoneAction?.Invoke( assembly );
			}
			catch ( Exception ex )
			{
				if ( exceptionAction != null ) throw exceptionAction( assemblyName, ex );
				throw;
			}
		}

		/// <summary>以傳入FileInfo嘗試進行載入</summary>
		public static void LoadAssemblyBy( FileInfo fi )
		{
			if ( !fi.Exists ) throw new ArgumentException( $"檔案路徑[{fi.FullName}]並不存在" );

			var bytes = File.ReadAllBytes( fi.FullName );
			try { Assembly.Load( bytes ); }
			catch ( Exception ex ) { throw new Exception( $"無法自[{fi.FullName}]載入Assembly, {ex.Message}", ex ); }
		}

		/// <summary>讀取執行資料夾內所有的組件</summary>
		public static void LoadBaseDirectoryDlls( Action<Assembly>? doneAction = null, List<String>? excludeNames = null )
		{
			var folder = new DirectoryInfo( AppDomain.CurrentDomain.BaseDirectory );
			var files = folder.GetFiles( "*.dll" );

			foreach ( var info in files )
			{
				var name = info.Name.Replace( info.Extension, "" );
				if ( excludeNames != null && excludeNames.Any( n => n.Equals( name, StringComparison.CurrentCultureIgnoreCase ) ) ) continue; //如果有符合的就排除載入

				try
				{
					var assembly = Assembly.Load( name );
					doneAction?.Invoke( assembly );
				}
				catch ( Exception ex ) { throw Err.Utility( "[LoadBaseDirectoryDlls] 無法載入Dll -> " + info + ", " + ex.Message, ex ); }
			}
		}


		/// <summary>讀取Dll內部資源</summary>
		public static void LoadEmbeddedResourceBy( String embeddedResource, String outputFilePath )
		{
			var isManaged = false;
			Byte[]? bytes = null;
			Assembly? assembly = null;
			var curAsm = Assembly.GetExecutingAssembly();

			using ( var resourceStream = curAsm.GetManifestResourceStream( embeddedResource ) )
			{
				if ( resourceStream == null ) throw new Exception( embeddedResource + " is not found in Embedded Resources." ); // Either the file is not existed or it is not mark as embedded resource


				bytes = new Byte[(Int32) resourceStream.Length]; // Get byte[] from the file from embedded resource
				resourceStream.Read( bytes, 0, (Int32) resourceStream.Length );
				try
				{
					assembly = Assembly.Load( bytes );
					isManaged = true;
				}
				catch ( BadImageFormatException )
				{
					isManaged = false;
				}
				catch ( Exception ex )
				{
					// Purposely do nothing,  Unmanaged dll or assembly cannot be loaded directly from byte[], Let the process fall through for next part
					throw new Exception( "Cannot Load Resource [ " + embeddedResource + " ], " + ex.Message, ex );
				}
			}

			//do not save to output
			if ( isManaged && String.IsNullOrEmpty( outputFilePath ) ) return;

			var targetPath = Path.GetDirectoryName( outputFilePath );
			var targetFileName = Path.GetFileName( outputFilePath );

			if ( String.IsNullOrEmpty( targetFileName ) ) throw new ArgumentException( "Wrong FileName" );
			if ( String.IsNullOrEmpty( targetPath ) ) targetPath = AppDomain.CurrentDomain.BaseDirectory;

			var fileOk = false;
			var tempFile = "";

			using ( var sha1 = new SHA1CryptoServiceProvider() )
			{
				var fileHash = BitConverter.ToString( sha1.ComputeHash( bytes ) ).Replace( "-", String.Empty );
				;

				tempFile = String.Concat( targetPath, @"\", targetFileName );

				if ( File.Exists( tempFile ) )
				{
					var bb = File.ReadAllBytes( tempFile );
					var fileHash2 = BitConverter.ToString( sha1.ComputeHash( bb ) ).Replace( "-", String.Empty );

					fileOk = fileHash == fileHash2;
				}
				else { fileOk = false; }
			}

			if ( !fileOk ) File.WriteAllBytes( tempFile, bytes );
			if ( isManaged ) assembly = Assembly.LoadFile( tempFile );
		}
	}
}
