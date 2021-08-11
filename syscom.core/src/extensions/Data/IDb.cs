using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using syscom;
using System.Collections.Concurrent;
using syscom.data;

namespace System.Data
{
	public static partial class IDbExtension
	{
		public static String DumpValues<T>( this IEnumerable<T>? parameters ) where T : IDbDataParameter
		{
			if ( parameters == null ) return "No Parameters";

			var str = new StringBuilder();
			var paras = parameters.ToList();
			for ( var idx = 0; idx < paras.Count; idx++ )
			{
				var p = paras[idx];
				str.AppendFormat( "[ Key({0}) = Value({1}) Type({2}) ]", p.ParameterName, p.Value, p.Value == null ? null : p.Value.GetType() );
			}

			return str.ToString();
		}
	}
}
