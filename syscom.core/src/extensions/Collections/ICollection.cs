using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System
{
	public static class ICollectionExtensions
	{
		/// <summary>
		/// 將list內的所有資料取出，若為System.Type則輸出其值。標籤[]代表一層Collection，
		/// 若具備Dictionary則於資料前加入該資料的key ex. 0:"aaa"(0代表key,"aaa"代表value)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <returns></returns>
		public static String ToDebugInfo<T>( this ICollection<T> list )
		{
			var builder = new StringBuilder();
			builder.Append( '[' );
			var first = true;
			foreach ( var e in list ) // Loop through all strings
			{
				if ( first )
					first = false;
				else
					builder.Append( ", " );
				builder.Append( ElementToString( e ) ); // Append string to StringBuilder
			}

			builder.Append( ']' );
			return builder.ToString(); // Get string from StringBuilder
		}


		static TypeFilter typeFilter = new TypeFilter( InterfaceFilter );

		//public static String ToString<T>( ICollection<T> list )
		//{
		//	StringBuilder builder = new StringBuilder();
		//	builder.Append( '[' );
		//	bool first = true;
		//	foreach ( T e in list ) // Loop through all strings
		//	{
		//		if ( first )
		//			first = false;
		//		else
		//			builder.Append( ", " );
		//		builder.Append( ElementToString( e ) ); // Append string to StringBuilder
		//	}
		//	builder.Append( ']' );
		//	return builder.ToString(); // Get string from StringBuilder
		//}

		static Boolean InterfaceFilter( Type typeObj, Object criteriaArrary )
		{
			var sa = (String[]) criteriaArrary;
			foreach ( var iName in sa )
				if ( typeObj.ToString().StartsWith( iName ) )
					return true;
			return false;
		}

		static String DynamicToString( Object o, Type argType )
		{
			var types = argType.GetGenericArguments();
			var castMethod = GetMethod( argType ).MakeGenericMethod( types );
			//MethodInfo castMethod = typeof(Utility).GetMethod("ToString", new Type[]{argType}).MakeGenericMethod(type.GetGenericArguments());
			return (String) castMethod.Invoke( null, new Object[] { o } );
		}

		static MethodInfo GetMethod( Type argType )
		{
			var mia = typeof( ICollectionExtensions ).GetMethods();
			foreach ( var mi in mia )
			{
				var pia = mi.GetParameters();
				foreach ( var pi in pia )
					if ( pi.ParameterType.Name.Equals( argType.Name ) )
						return mi;
			}

			return null;
		}

		static String ElementToString( Object o )
		{
			var type = o.GetType();
			if ( type.ToString().StartsWith( "System.Collections.Generic.KeyValuePair" ) )
				return KeyValuePairToString( o );

			var interfaces = type.FindInterfaces( typeFilter,
			                                      new String[] { "System.Collections.Generic.ICollection" } );
			if ( interfaces.Length == 0 )
				return o.ToString();
			else
				return DynamicToString( o, interfaces[0] );
		}

		static String KeyValuePairToString( Object o )
		{
			var key = o.GetType().GetProperty( "Key" ).GetValue( o, BindingFlags.GetProperty, null, null, null ).ToString();
			var value = o.GetType().GetProperty( "Value" ).GetValue( o, BindingFlags.GetProperty, null, null, null );
			return key + " : " + ElementToString( value );
		}
	}
}