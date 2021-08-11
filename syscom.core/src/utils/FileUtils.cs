using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace syscom
{
	public static class FileUtils
	{
		static readonly List<String> KEYS_InvalidFileNameChars = Path.GetInvalidFileNameChars().Select( c => c.ToString() ).ToList();
		static readonly List<String> KEYS_InvalidFilePathChars = Path.GetInvalidPathChars().Select( c => c.ToString() ).ToList();

		public static String GetSafeFileNameBy( String name )
		{
			if ( String.IsNullOrEmpty( name ) ) return name;
			return KEYS_InvalidFileNameChars.Aggregate( name, ( current, key ) => current.Replace( key, String.Empty ) );
		}

		public static String GetSafeFilePathBy( String path )
		{
			if ( String.IsNullOrEmpty( path ) ) return path;
			return KEYS_InvalidFilePathChars.Aggregate( path, ( current, key ) => current.Replace( key, String.Empty ) );
		}

		public static ArrayList ReadToArrayList( String path )
		{
			var bytes = File.ReadAllBytes( path );

			var list = new ArrayList();

			var buff = new Byte[1024];
			var lineIndex = 0;
			Byte temp = 0, temp2;
			for ( var index = 0; index < bytes.Length; index++ )
			{
				temp2 = temp;
				temp = bytes[index];
				buff[lineIndex] = temp;

				if ( temp == 0x0A && temp2 == 0x0D )
				{
					var newBytes = new Byte[lineIndex + 1];
					Array.Copy( buff, newBytes, lineIndex + 1 );
					lineIndex = 0;
					list.Add( newBytes );
				}
				else
				{
					lineIndex++;
				}
			}

			return list;
		}

		static readonly Encoding Encode_UTF8 = Encoding.UTF8;

		public static void AppendToFileBy( String fileName, String content, Boolean isAppend = true )
		{
			var path = fileName.Contains( @"\" ) || fileName.Contains( @"/" ) ? fileName : AppDomain.CurrentDomain.BaseDirectory + fileName;
			using ( var w = new StreamWriter( path, isAppend, Encode_UTF8 ) )
			{
				w.Write( content );
			}
		}


		static Int32 _MaxPathLength = -1;

		/// <summary>取得當前操作系統最長的FilePath數量</summary>
		public static Int32 GetCurrentOperationSystemFilePathMaxLength()
		{
			if ( _MaxPathLength != -1 ) return _MaxPathLength;

			try
			{
				var maxPathField = typeof( Path ).GetField( "MaxPath", BindingFlags.Static | BindingFlags.GetField | BindingFlags.NonPublic );
				if ( maxPathField != null ) _MaxPathLength = (Int32) maxPathField.GetValue( null );
			}
			catch ( Exception ex )
			{
				Console.WriteLine( ex );
			}

			return _MaxPathLength;
		}
	}
}
