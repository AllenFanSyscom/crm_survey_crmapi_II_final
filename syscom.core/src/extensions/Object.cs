using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace System.Reflection
{
	public static partial class ObjectExtensions
	{
		/// <summary>取得該型別所有值的字串化清單</summary>
		public static String DumpAllPropertyValueInfos<TModel>( this TModel item ) where TModel : class
		{
			var type = typeof( TModel );
			var properties = type.GetPropertyInfos( true );

			var data = new StringBuilder( properties.Count * 2 );
			foreach ( var p in properties )
			{
				var value = p.GetValue( item );
				data.AppendLine( String.Concat( "【", p.Name, "】=[", value, "]" ) );
			}

			return data.ToString();
		}
	}
}