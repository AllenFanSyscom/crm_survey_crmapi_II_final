using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace syscom.Utilities.Text
{
	public static class XmlUtils
	{
		/// <summary>將物件序列化至Stream</summary>
		public static void SerializeBy( Object target, Stream stream, Encoding? encoding = null )
		{
			if ( target == null ) throw new ArgumentNullException( nameof( target ) );
			if ( encoding == null ) encoding = Encoding.UTF8;

			var serializer = new XmlSerializer( target.GetType() );

			var settings = new XmlWriterSettings
			{
				Indent = true,
				NewLineChars = "\r\n",
				Encoding = encoding,
				IndentChars = "\t"
			};

			using ( var writer = XmlWriter.Create( stream, settings ) )
			{
				serializer.Serialize( writer, target );
			}
		}

		public static T DeserializeBy<T>( Stream stream, Encoding? encoding = null )
		{
			if ( encoding == null ) encoding = Encoding.UTF8;

			var mySerializer = new XmlSerializer( typeof( T ) );

			var settings = new XmlReaderSettings();
			using ( var reader = XmlReader.Create( stream, settings ) )
			{
				try
				{
					return (T) mySerializer.Deserialize( reader );
				}
				catch ( Exception ex )
				{
					if ( ex.InnerException != null ) throw ex.InnerException;
					throw;
				}
			}
		}


		/// <summary>將物件序列化為XML字串</summary>
		public static String SerializeBy( Object target, Encoding? encoding = null )
		{
			using ( var stream = new MemoryStream() )
			{
				SerializeBy( target, stream, encoding );

				if ( encoding == null ) encoding = Encoding.UTF8;

				stream.Position = 0;
				using ( var reader = new StreamReader( stream, encoding ) )
				{
					return reader.ReadToEnd();
				}
			}
		}


		public static void SerializeToFile( Object target, String path, Encoding? encoding = null )
		{
			if ( String.IsNullOrEmpty( path ) ) throw new ArgumentNullException( nameof( path ) );

			using ( var fileStream = new FileStream( path, FileMode.Create, FileAccess.Write ) )
			{
				SerializeBy( target, fileStream, encoding );
			}
		}


		/// <summary>將XML字串反序列化為物件</summary>
		public static T DeserializeBy<T>( String source, Encoding? encoding = null )
		{
			if ( String.IsNullOrEmpty( source ) ) throw new ArgumentNullException( nameof( source ) );
			if ( encoding == null ) encoding = Encoding.UTF8;

			using ( var ms = new MemoryStream( encoding.GetBytes( source ) ) )
			{
				return DeserializeBy<T>( ms, encoding );
			}
		}

		public static T DeserializeFromFile<T>( String path, Encoding? encoding = null )
		{
			if ( String.IsNullOrEmpty( path ) ) throw new ArgumentNullException( nameof( path ) );
			if ( encoding == null ) encoding = Encoding.UTF8;
			try
			{
				var xml = File.ReadAllText( path, encoding );
				return DeserializeBy<T>( xml, encoding );
			}
			catch ( Exception ex )
			{
				if ( ex.InnerException != null ) throw ex.InnerException;
				throw;
			}
		}
	}
}
