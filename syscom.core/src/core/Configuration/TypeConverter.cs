using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace syscom.config
{
	public class ListStringTypeConverter : TypeConverter
	{
		public override Object ConvertFrom( ITypeDescriptorContext context, CultureInfo culture, Object value )
		{
			var str = value as String;
			var list = new List<String>();
			if ( !String.IsNullOrWhiteSpace( str ) ) list = str.SplitByComma().ToList();
			return list;
		}

		public override Boolean CanConvertFrom( ITypeDescriptorContext context, Type sourceType )
		{
			if ( sourceType == typeof( String ) ) return true;

			return base.CanConvertFrom( context, sourceType );
		}
	}
}