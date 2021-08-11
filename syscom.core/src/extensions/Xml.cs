using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using syscom;

namespace System.Xml
{
	public static partial class XmlExtensions
	{
		/// <summary>取得XmlNode的XPath</summary>
		public static String FindXPath( this XmlNode node )
		{
			var builder = new StringBuilder();
			while ( node != null )
				switch ( node.NodeType )
				{
					case XmlNodeType.Attribute:
						//===================================
						builder.Insert( 0, "/@" + node.Name );
						node = ( (XmlAttribute) node ).OwnerElement;
						//===================================
						break;
					case XmlNodeType.Element:
						//===================================
						var index = FindElementLevel( (XmlElement) node );
						builder.Insert( 0, "/" + node.Name + "[" + index + "]" );
						node = node.ParentNode;
						//===================================
						break;
					case XmlNodeType.Document:
						//===================================
						return builder.ToString();
					//===================================
					default:
						throw Err.Extension( "只有XmlNode及Attribute可以使用" );
				}

			throw new ArgumentException( "Node[ " + node + " ]無法取得完整XPath, 請檢查Xml格式" );
		}

		/// <summary>取得XmlElement的階層</summary>
		public static Int32 FindElementLevel( this XmlElement element )
		{
			var parentNode = element.ParentNode;
			if ( parentNode is XmlDocument ) return 1;

			var parent = (XmlElement) parentNode;
			var index = 1;
			foreach ( XmlNode candidate in parent.ChildNodes )
			{
				if ( !( candidate is XmlElement ) || candidate.Name != element.Name ) continue;
				if ( candidate == element ) return index;
				index++;
			}

			throw new ArgumentException( "Couldn't find element within parent" );
		}
	}
}