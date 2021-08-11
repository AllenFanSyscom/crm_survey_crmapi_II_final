using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace System.Web
{
	/// <summary>Describes how to handle null values in query parameters.</summary>
	public enum NullValueHandling
	{
		/// <summary>Set as name without value in query string.</summary>
		NameOnly,

		/// <summary>Don't add to query string, remove any existing value.</summary>
		Remove,

		/// <summary>Don't add to query string, but leave any existing value unchanged.</summary>
		Ignore
	}

	/// <summary>Represents an individual name/value pair within a URL query</summary>
	public class QueryParameter
	{
		private Object _value;
		private String _encodedValue;

		/// <summary>Creates a new instance of a query parameter. Allows specifying whether string value provided has already been URL-encoded</summary>
		public QueryParameter( String name, Object value, Boolean isEncoded = false )
		{
			Name = name;
			if ( isEncoded && value != null )
			{
				_encodedValue = value as String;
				_value = Url.Decode( _encodedValue, true );
			}
			else
			{
				Value = value;
			}
		}

		/// <summary>The name (left side) of the query parameter.</summary>
		public String Name { get; set; }

		/// <summary>The value (right side) of the query parameter.</summary>
		public Object Value
		{
			get => _value;
			set
			{
				_value = value;
				_encodedValue = null;
			}
		}

		/// <summary>
		/// Returns the string ("name=value") representation of the query parameter.
		/// </summary>
		/// <param name="encodeSpaceAsPlus">Indicates whether to encode space characters with "+" instead of "%20".</param>
		public String ToString( Boolean encodeSpaceAsPlus )
		{
			var name = Url.EncodeIllegalCharacters( Name, encodeSpaceAsPlus );
			var value =
				( _encodedValue != null ) ? _encodedValue :
				( Value != null ) ? Url.Encode( Value.ToInvariantString(), encodeSpaceAsPlus ) :
				null;

			return ( value == null ) ? name : $"{name}={value}";
		}
	}

	/// <summary>Represents a URL query as a key-value dictionary. Insertion order is preserved</summary>
	public class QueryParamCollection : List<QueryParameter>
	{
		/// <summary>Returns a new instance of QueryParamCollection</summary>
		/// <param name="query">Optional query string to parse.</param>
		public QueryParamCollection( String? query = null )
		{
			if ( query != null ) AddRange( Url.ParseQueryParams( query ) );
		}

		/// <summary>Returns serialized, encoded query string. Insertion order is preserved.</summary>
		public override String ToString() { return ToString( false ); }

		/// <summary>Returns serialized, encoded query string. Insertion order is preserved.</summary>
		public String ToString( Boolean encodeSpaceAsPlus ) { return String.Join( "&", this.Where( p => p != null ).Select( p => p.ToString( encodeSpaceAsPlus ) ) ); }

		/// <summary>Adds a new query parameter.</summary>
		public void Add( String key, Object value ) { Add( new QueryParameter( key, value ) ); }

		/// <summary>Adds a new query parameter, allowing you to specify whether the value is already encoded.</summary>
		public void Add( String key, String value, Boolean isEncoded ) { Add( new QueryParameter( key, value, isEncoded ) ); }

		/// <summary>True if the collection contains a query parameter with the given name.</summary>
		public Boolean ContainsKey( String name ) { return this.Any( p => p.Name == name ); }

		/// <summary>Removes all parameters of the given name.</summary>
		/// <returns>The number of parameters that were removed</returns>
		/// <exception cref="ArgumentNullException"><paramref name="name" /> is null.</exception>
		public Int32 Remove( String name ) { return RemoveAll( p => p.Name == name ); }

		/// <summary>
		/// Replaces an existing QueryParameter or appends one to the end. If object is a collection type (array, IEnumerable, etc.),
		/// multiple parameters are added, i.e. x=1&amp;x=2. If any of the same name already exist, they are overwritten one by one
		/// (preserving order) and any remaining are appended to the end. If fewer values are specified than already exist,
		/// remaining existing values are removed.
		/// </summary>
		public void Merge( String name, Object value, Boolean isEncoded, NullValueHandling nullValueHandling )
		{
			if ( value == null && nullValueHandling != NullValueHandling.NameOnly )
			{
				if ( nullValueHandling == NullValueHandling.Remove ) Remove( name );
				return;
			}

			// This covers some complex edge cases involving multiple values of the same name.
			// example: x has values at positions 2 and 4 in the query string, then we set x to
			// an array of 4 values. We want to replace the values at positions 2 and 4 with the
			// first 2 values of the new array, then append the remaining 2 values to the end.
			var parameters = this.Where( p => p.Name == name ).ToArray();
			var values = ( !( value is String ) && value is IEnumerable en ) ? en.Cast<Object>().ToArray() : new[] { value };

			for ( var i = 0;; i++ )
			{
				if ( i < parameters.Length && i < values.Length )
				{
					if ( values[i] is QueryParameter qp )
						this[IndexOf( parameters[i] )] = qp;
					else
						parameters[i].Value = values[i];
				}
				else if ( i < parameters.Length ) Remove( parameters[i] );
				else if ( i < values.Length )
				{
					var qp = values[i] as QueryParameter ?? new QueryParameter( name, values[i], isEncoded );
					Add( qp );
				}
				else
					break;
			}
		}

		/// <summary>
		/// Parses properties of values into key-value pairs and replaces existing QueryParameters or appends them to the end.
		/// If a property value is a collection type (array, IEnumerable, etc.), multiple parameters are added, i.e. x=1&amp;x=2.
		/// If any of the same name already exist, they are overwritten one by one (preserving order) and any remaining are
		/// appended to the end. If fewer values are specified than already exist, remaining existing values are removed.
		/// </summary>
		public void Merge( Object values, NullValueHandling nullValueHandling )
		{
			if ( values == null ) return;
			foreach ( var kv in values.ToKeyValuePairs() ) Merge( kv.Key, kv.Value, false, nullValueHandling );
		}

		/// <summary>
		/// Gets or sets a query parameter value by name. A query may contain multiple values of the same name
		/// (i.e. "x=1&amp;x=2"), in which case the value is an array, which works for both getting and setting.
		/// </summary>
		/// <param name="name">The query parameter name</param>
		/// <returns>The query parameter value or array of values</returns>
		public Object this[ String name ]
		{
			get
			{
				var all = this.Where( p => p.Name == name ).Select( p => p.Value ).ToArray();
				if ( all.Length == 0 ) return null;
				if ( all.Length == 1 ) return all[0];
				return all;
			}
			set => Merge( name, value, false, NullValueHandling.Remove );
		}
	}
}
