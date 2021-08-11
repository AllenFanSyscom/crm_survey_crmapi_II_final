using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using syscom.Utilities.Text;

namespace syscom.Text
{
	public class FileBaseXmlModel
	{
		[XmlIgnore]
		public String FilePath { get; set; }

		public void Save() { XmlUtils.SerializeToFile( this, FilePath, Encoding.UTF8 ); }

		public static TModel Load<TModel>( String filePath ) where TModel : FileBaseXmlModel
		{
			try
			{
				if ( !File.Exists( filePath ) ) throw new FileNotFoundException( $"找不到路徑[{filePath}],無法讀取Xml檔案" );
				var model = XmlUtils.DeserializeFromFile<TModel>( filePath, Encoding.UTF8 );
				model.FilePath = filePath;

				return model;
			}
			catch ( Exception ex )
			{
				throw new InvalidDataException( $"解析Xml格式[{typeof( TModel ).FullName}]檔案[{filePath}]時發生異常, " + ex.Message, ex );
			}
		}
	}
}