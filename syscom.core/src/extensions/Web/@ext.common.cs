using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace System.Web
{
	/// <summary>CommonExtensions for objects.</summary>
	public static class CommonExtensions
	{
		/// <summary>
		/// Returns a key-value-pairs representation of the object.
		/// For strings, URL query string format assumed and pairs are parsed from that.
		/// For objects that already implement IEnumerable&lt;KeyValuePair&gt;, the object itself is simply returned.
		/// For all other objects, all publicly readable properties are extracted and returned as pairs.
		/// </summary>
		/// <param name="obj">The object to parse into key-value pairs</param>
		/// <exception cref="ArgumentNullException"><paramref name="obj"/> is <see langword="null" />.</exception>
		public static IEnumerable<KeyValuePair<String, Object>> ToKeyValuePairs( this Object obj )
		{
			if ( obj == null ) throw new ArgumentNullException( nameof( obj ) );

			return
				obj is String s ? StringToKV( s ) :
				obj is IEnumerable e ? CollectionToKV( e ) :
				ObjectToKV( obj );
		}

		/// <summary>
		/// Returns a string that represents the current object, using CultureInfo.InvariantCulture where possible.
		/// Dates are represented in IS0 8601.
		/// </summary>
		public static String ToInvariantString( this Object obj )
		{
			// inspired by: http://stackoverflow.com/a/19570016/62600
			return
				obj == null ? null :
				obj is DateTime dt ? dt.ToString( "o", CultureInfo.InvariantCulture ) :
				obj is DateTimeOffset dto ? dto.ToString( "o", CultureInfo.InvariantCulture ) :
#if !NETSTANDARD1_0
				obj is IConvertible c ? c.ToString( CultureInfo.InvariantCulture ) :
#endif
				obj is IFormattable f ? f.ToString( null, CultureInfo.InvariantCulture ) :
				obj.ToString();
		}

		/// <summary>Splits at the first occurence of the given separator.</summary>
		/// <param name="s">The string to split.</param>
		/// <param name="separator">The separator to split on.</param>
		/// <returns>Array of at most 2 strings. (1 if separator is not found.)</returns>
		public static String[] SplitOnFirstOccurence( this String s, String separator )
		{
			// Needed because full PCL profile doesn't support Split(char[], int) (#119)
			if ( String.IsNullOrEmpty( s ) )
				return new[] { s };

			var i = s.IndexOf( separator );
			return ( i == -1 ) ? new[] { s } : new[] { s.Substring( 0, i ), s.Substring( i + separator.Length ) };
		}

		private static IEnumerable<KeyValuePair<String, Object>> StringToKV( String s )
		{
			return Url.ParseQueryParams( s ).Select( p => new KeyValuePair<String, Object>( p.Name, p.Value ) );
		}

		private static IEnumerable<KeyValuePair<String, Object>> ObjectToKV( Object obj )
		{
#if NETSTANDARD1_0
			return from prop in obj.GetType().GetRuntimeProperties()
				let getter = prop.GetMethod
				where getter?.IsPublic == true
				let val = getter.Invoke(obj, null)
				select new KeyValuePair<string, object>(prop.Name, val);
#else
			return from prop in obj.GetType().GetProperties()
			       let getter = prop.GetGetMethod( false )
			       where getter != null
			       let val = getter.Invoke( obj, null )
			       select new KeyValuePair<String, Object>( prop.Name, val );
#endif
		}

		private static IEnumerable<KeyValuePair<String, Object>> CollectionToKV( IEnumerable col )
		{
			// Accepts KeyValuePairs or any arbitrary types that contain a property called "Key" or "Name" and a property called "Value".
			foreach ( var item in col )
			{
				if ( item == null )
					continue;

				String key;
				Object val;

				var type = item.GetType();
#if NETSTANDARD1_0
				var keyProp = type.GetRuntimeProperty("Key") ?? type.GetRuntimeProperty("key") ?? type.GetRuntimeProperty("Name") ?? type.GetRuntimeProperty("name");
				var valProp = type.GetRuntimeProperty("Value") ?? type.GetRuntimeProperty("value");
#else
				var keyProp = type.GetProperty( "Key" ) ?? type.GetProperty( "key" ) ?? type.GetProperty( "Name" ) ?? type.GetProperty( "name" );
				var valProp = type.GetProperty( "Value" ) ?? type.GetProperty( "value" );
#endif

				if ( keyProp != null && valProp != null )
				{
					key = keyProp.GetValue( item, null )?.ToInvariantString();
					val = valProp.GetValue( item, null );
				}
				else
				{
					key = item.ToInvariantString();
					val = null;
				}

				if ( key != null )
					yield return new KeyValuePair<String, Object>( key, val );
			}
		}

		/// <summary>Merges the key/value pairs from d2 into d1, without overwriting those already set in d1</summary>
		public static void Merge<TKey, TValue>( this IDictionary<TKey, TValue> d1, IDictionary<TKey, TValue> d2 )
		{
			foreach ( var kv in d2.Where( x => !d1.ContainsKey( x.Key ) ).ToList() )
			{
				d1[kv.Key] = kv.Value;
			}
		}

		/// <summary>Strips any single quotes or double quotes from the beginning and end of a string</summary>
		public static String StripQuotes( this String s ) => Regex.Replace( s, "^\\s*['\"]+|['\"]+\\s*$", "" );
	}
}
