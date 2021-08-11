using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace syscom.Runtime.Serialization
{
	[Flags]
	public enum SerializerType
	{
		Binary,
		DataContract,
		Xml,
	}


	public static class SerializeUtils
	{
		public static Byte[] SerializeBy<TModel>( SerializerType type, TModel source )
		{
			switch ( type )
			{
				case SerializerType.Xml:
					return SerializeByXml( source );
				case SerializerType.Binary:
					return SerializeByBinary( source );

				case SerializerType.DataContract:
					return SerializeByDataContract( source );
			}

			throw Err.Utility( "無法判定的序列化方式[ " + type + " ]" );
		}

		//Note[Raz]: 若改一下ConvertToDefault可想看看要不要拿掉generic約束
		public static TModel DeserializeBy<TModel>( SerializerType type, Byte[] source ) where TModel : class
		{
			switch ( type )
			{
				case SerializerType.Xml:
					return DeserializeByXml<TModel>( source );
				case SerializerType.Binary:
					return DeserializeByBinary<TModel>( source );

				case SerializerType.DataContract:
					return DeserializeByDCSBytes<TModel>( source );
			}

			throw Err.Utility( "無法判定的序列化方式[ " + type + " ]" );
		}


		//==============================================================================================
		static Byte[] SerializeByDataContract<T>( T target )
		{
			var dcs = new DataContractSerializer( typeof( T ) );
			using ( var ms = new MemoryStream() )
			{
				dcs.WriteObject( ms, target );
				return ms.ToArray();
			}
		}

		//============================================================================================== BinaryFormatter

		static Byte[] SerializeByBinary<TModel>( TModel target )
		{
			var formatter = new BinaryFormatter();
			using ( var ms = new MemoryStream() )
			{
				formatter.Serialize( ms, target );
				return ms.ToArray();
			}
		}


		//==================================================================================================Serialize
		static Byte[] SerializeByXml<TModel>( TModel target )
		{
			var xmlSer = new XmlSerializer( typeof( TModel ) );
			using ( var s = new MemoryStream() )
			{
				xmlSer.Serialize( s, target );
				return s.ToArray();
			}
		}



		//==================================================================================================Deserialize
		static TModel DeserializeByDCSBytes<TModel>( Byte[] source ) where TModel : class
		{
			var dcs = new DataContractSerializer( typeof( TModel ) );
			using ( var ms = new MemoryStream( source ) )
			{
				return dcs.ReadObject( ms ) as TModel;
			}
		}

		static TModel DeserializeByXml<TModel>( Byte[] source ) where TModel : class
		{
			var xser = new XmlSerializer( typeof( TModel ) );
			using ( var ms = new MemoryStream( source ) )
			{
				return xser.Deserialize( ms ) as TModel;
			}
		}

		//============================================================================================== BinaryFormatter
		static TModel DeserializeByBinary<TModel>( Byte[] source ) where TModel : class
		{
			var formatter = new BinaryFormatter();
			using ( var ms = new MemoryStream( source ) )
			{
				return formatter.Deserialize( ms ) as TModel;
			}
		}

	}
}
