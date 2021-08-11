using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace System.Web
{
	/// <summary>A mutable object for fluently building and parsing URLs.</summary>
	public class Url
	{
		const Int32 MAX_URL_LENGTH = 65519;


		private String _baseUrl;
		private Boolean _parsed;

		private String _scheme;
		private String _userInfo;
		private String _host;
		private List<String> _pathSegments;
		private QueryParamCollection _queryParams;
		private String _fragment;
		private Int32? _port;
		private Boolean _leadingSlash;
		private Boolean _trailingSlash;

		/// <summary>The scheme of the URL, i.e. "http". Does not include ":" delimiter. Empty string if the URL is relative</summary>
		public String Scheme { get => EnsureParsed()._scheme; set => EnsureParsed()._scheme = value; }

		/// <summary>i.e. "user:pass" in "https://user:pass@www.site.com". Empty string if not present</summary>
		public String UserInfo { get => EnsureParsed()._userInfo; set => EnsureParsed()._userInfo = value; }

		/// <summary>i.e. "www.site.com" in "https://www.site.com:8080/path". Does not include user info or port</summary>
		public String Host
		{
			get { return EnsureParsed()._host; }
			set
			{
				var host = String.Empty;
				var scheme = String.Empty;

				var vars = value.SplitBy( "://" );
				switch ( vars.Length )
				{
					case 1:
						scheme = _scheme;
						host = vars[0];
						break;
					case 2:
						scheme = vars[0];
						host = vars[1];
						break;
					default: throw new ArgumentOutOfRangeException( nameof(value) );
				}

				var parsed = EnsureParsed();
				if( !String.IsNullOrEmpty( host ) ) parsed._scheme = scheme;
				parsed._host = host;
			}
		}

		/// <summary>Port number of the URL. Null if not explicitly specified.</summary>
		public Int32? Port { get => EnsureParsed()._port; set => EnsureParsed()._port = value; }

		/// <summary>i.e. "www.site.com:8080" in "https://www.site.com:8080/path". Includes both user info and port, if included</summary>
		public String Authority => String.Concat(
			UserInfo,
			UserInfo?.Length > 0 ? "@" : "",
			Host,
			Port.HasValue ? ":" : "",
			Port );

		/// <summary>i.e. "https://www.site.com:8080" in "https://www.site.com:8080/path" (everything before the path)</summary>
		public String Root => String.Concat(
			Scheme,
			Scheme?.Length > 0 ? ":" : "",
			Authority?.Length > 0 ? "//" : "",
			Authority );

		/// <summary>i.e. "/path" in "https://www.site.com/path". Empty string if not present. Leading and trailing "/" retained exactly as specified by user</summary>
		public String Path
		{
			get
			{
				EnsureParsed();
				return String.Concat(
					_leadingSlash ? "/" : "",
					String.Join( "/", PathSegments ),
					_trailingSlash && PathSegments.Any() ? "/" : "" );
			}
			set
			{
				PathSegments.Clear();
				_trailingSlash = false;
				if ( String.IsNullOrEmpty( value ) )
					_leadingSlash = false;
				else if ( value == "/" )
					_leadingSlash = true;
				else
					AppendPathSegment( value ?? "" );
			}
		}

		/// <summary>The "/"-delimited segments of the path, not including leading or trailing "/" characters</summary>
		public IList<String> PathSegments => EnsureParsed()._pathSegments;

		/// <summary>i.e. "x=1&amp;y=2" in "https://www.site.com/path?x=1&amp;y=2". Does not include "?"</summary>
		public String Query
		{
			get => Queries.ToString();
			set
			{
				Queries.Clear();
				Queries.AddRange( ParseQueryParams( value ) );
			}
		}

		/// <summary>Query parsed to name/value pairs.</summary>
		public QueryParamCollection Queries => EnsureParsed()._queryParams;

		/// <summary>i.e. "frag" in "https://www.site.com/path?x=y#frag". Does not include "#"</summary>
		public String Fragment { get => EnsureParsed()._fragment; set => EnsureParsed()._fragment = value; }

		/// <summary>True if URL does not start with a non-empty scheme. i.e. true for "https://www.site.com", false for "//www.site.com"</summary>
		public Boolean IsRelative => String.IsNullOrEmpty( Scheme );

		/// <summary>True if Url is absolute and scheme is https or wss.</summary>
		public Boolean IsSecureScheme => !IsRelative && ( Scheme.ToLowerInvariant() == "https" || Scheme.ToLowerInvariant() == "wss" );


		/// <summary>Constructs a Url object from a string.</summary>
		/// <param name="baseUrl">The URL to use as a starting point (required)</param>
		/// <exception cref="ArgumentNullException"><paramref name="baseUrl"/> is <see langword="null" />.</exception>
		public Url( String? baseUrl = null )
		{
			_baseUrl = baseUrl ?? String.Empty;
		}

		/// <summary>Constructs a Url object from a System.Uri.</summary>
		/// <param name="uri">The System.Uri (required)</param>
		/// <exception cref="ArgumentNullException"><paramref name="uri"/> is <see langword="null" />.</exception>
		public Url( Uri uri )
		{
			_baseUrl = ( uri ?? throw new ArgumentNullException( nameof( uri ) ) ).OriginalString;
			ParseInternal( uri ); // parse eagerly, taking advantage of the fact that we already have a parsed Uri
		}

		/// <summary>Parses a URL string into a Flurl.Url object.</summary>
		public static Url Parse( String url ) { return new Url( url ); }

		private Url EnsureParsed()
		{
			if ( !_parsed ) ParseInternal();
			return this;
		}

		private void ParseInternal( Uri? uri = null )
		{
			_parsed = true;

			uri = uri ?? new Uri( _baseUrl, UriKind.RelativeOrAbsolute );

			if ( uri.IsAbsoluteUri )
			{
				_scheme = uri.Scheme;
				_userInfo = uri.UserInfo;
				_host = uri.Host;
				_port = uri.Authority.EndsWith( $":{uri.Port}" ) ? uri.Port : (Int32?) null; // don't default Port if not included
				_pathSegments = new List<String>();
				if ( uri.AbsolutePath.Length > 0 && uri.AbsolutePath != "/" )
					AppendPathSegment( uri.AbsolutePath );
				_queryParams = new QueryParamCollection( uri.Query );
				_fragment = uri.Fragment.TrimStart( '#' ); // quirk - formal def of fragment does not include the #

				_leadingSlash = uri.OriginalString.StartsWith( Root + "/" );
				_trailingSlash = _pathSegments.Any() && uri.AbsolutePath.EndsWith( "/" );

				// more quirk fixes
				var hasAuthority = uri.OriginalString.StartsWith( $"{Scheme}://" );
				if ( hasAuthority && Authority.Length == 0 && PathSegments.Any() )
				{
					// Uri didn't parse Authority when it should have
					_host = _pathSegments[0];
					_pathSegments.RemoveAt( 0 );
				}
				else if ( !hasAuthority && Authority.Length > 0 )
				{
					// Uri parsed Authority when it should not have
					_pathSegments.Insert( 0, Authority );
					_userInfo = "";
					_host = "";
					_port = null;
				}
			}
			// if it's relative, System.Uri refuses to parse any of it. these hacks will force the matter
			else if ( uri.OriginalString.StartsWith( "//" ) )
			{
				ParseInternal( new Uri( "http:" + uri.OriginalString ) );
				_scheme = "";
			}
			else if ( uri.OriginalString.StartsWith( "/" ) )
			{
				ParseInternal( new Uri( "http://temp.com" + uri.OriginalString ) );
				_scheme = "";
				_host = "";
				_leadingSlash = true;
			}
			else
			{
				ParseInternal( new Uri( "http://temp.com/" + uri.OriginalString ) );
				_scheme = "";
				_host = "";
				_leadingSlash = false;
			}
		}

		/// <summary>Parses a URL query to a QueryParamCollection dictionary.</summary>
		public static IEnumerable<QueryParameter> ParseQueryParams( String query )
		{
			query = query?.TrimStart( '?' );
			if ( String.IsNullOrEmpty( query ) )
				return Enumerable.Empty<QueryParameter>();

			return
				from p in query.Split( '&' )
				let pair = p.SplitOnFirstOccurence( "=" )
				let name = pair[0]
				let value = ( pair.Length == 1 ) ? null : pair[1]
				select new QueryParameter( name, value, true );
		}

		/// <summary>Splits the given path into segments, encoding illegal characters, "?", and "#".</summary>
		public static IEnumerable<String> ParsePathSegments( String path )
		{
			return EncodeIllegalCharacters( path )
			       //.Replace( "?", "%3F" )
			       //.Replace( "#", "%23" )
			       .Trim( '/' )
			       .Split( '/' );
		}


		/// <summary>Appends a segment to the URL path, ensuring there is one and only one '/' character as a separator.</summary>
		/// <param name="segment">The segment to append</param>
		/// <param name="fullyEncode">If true, URL-encodes reserved characters such as '/', '+', and '%'. Otherwise, only encodes strictly illegal characters (including '%' but only when not followed by 2 hex characters).</param>
		/// <returns>the Url object with the segment appended</returns>
		/// <exception cref="ArgumentNullException"><paramref name="segment"/> is <see langword="null" />.</exception>
		public Url AppendPathSegment( Object segment, Boolean fullyEncode = false )
		{
			if ( segment == null )
				throw new ArgumentNullException( nameof( segment ) );

			if ( fullyEncode )
			{
				PathSegments.Add( Uri.EscapeDataString( segment.ToInvariantString() ) );
				_trailingSlash = false;
			}
			else
			{
				var subpath = segment.ToInvariantString();
				foreach ( var s in ParsePathSegments( subpath ) )
					PathSegments.Add( s );
				_trailingSlash = subpath.EndsWith( "/" );
			}

			_leadingSlash = true;
			return this;
		}

		/// <summary>Appends multiple segments to the URL path, ensuring there is one and only one '/' character as a seperator.</summary>
		/// <param name="segments">The segments to append</param>
		/// <returns>the Url object with the segments appended</returns>
		public Url AppendPathSegments( params Object[] segments )
		{
			foreach ( var segment in segments )
				AppendPathSegment( segment );

			return this;
		}

		/// <summary>Appends multiple segments to the URL path, ensuring there is one and only one '/' character as a seperator.</summary>
		/// <param name="segments">The segments to append</param>
		/// <returns>the Url object with the segments appended</returns>
		public Url AppendPathSegments( IEnumerable<Object> segments )
		{
			foreach ( var s in segments )
				AppendPathSegment( s );

			return this;
		}

		/// <summary>Removes the last path segment from the URL.</summary>
		public Url RemovePathSegment()
		{
			if ( PathSegments.Any() )
				PathSegments.RemoveAt( PathSegments.Count - 1 );
			return this;
		}

		/// <summary>Removes the entire path component of the URL, including the leading slash.</summary>
		public Url RemovePath()
		{
			PathSegments.Clear();
			_leadingSlash = _trailingSlash = false;
			return this;
		}

		/// <summary>Adds a parameter to the query, overwriting the value if name exists.</summary>
		/// <param name="name">Name of query parameter</param>
		/// <param name="value">Value of query parameter</param>
		/// <param name="nullValueHandling">Indicates how to handle null values. Defaults to Remove (any existing)</param>
		/// <returns>The Url object with the query parameter added</returns>
		public Url SetQueryParam( String name, Object value, NullValueHandling nullValueHandling = NullValueHandling.Remove )
		{
			Queries.Merge( name, value, false, nullValueHandling );
			return this;
		}

		/// <summary>Adds a parameter to the query, overwriting the value if name exists.</summary>
		/// <param name="name">Name of query parameter</param>
		/// <param name="value">Value of query parameter</param>
		/// <param name="isEncoded">Set to true to indicate the value is already URL-encoded</param>
		/// <param name="nullValueHandling">Indicates how to handle null values. Defaults to Remove (any existing)</param>
		/// <returns>The Url object with the query parameter added</returns>
		/// <exception cref="ArgumentNullException"><paramref name="name"/> is <see langword="null" />.</exception>
		public Url SetQueryParam( String name, String value, Boolean isEncoded = false, NullValueHandling nullValueHandling = NullValueHandling.Remove )
		{
			Queries.Merge( name, value, isEncoded, nullValueHandling );
			return this;
		}

		/// <summary>Adds a parameter without a value to the query, removing any existing value.</summary>
		/// <param name="name">Name of query parameter</param>
		/// <returns>The Url object with the query parameter added</returns>
		public Url SetQueryParam( String name )
		{
			Queries.Merge( name, null, false, NullValueHandling.NameOnly );
			return this;
		}

		/// <summary>Parses values (usually an anonymous object or dictionary) into name/value pairs and adds them to the query, overwriting any that already exist.</summary>
		/// <param name="values">Typically an anonymous object, ie: new { x = 1, y = 2 }</param>
		/// <param name="nullValueHandling">Indicates how to handle null values. Defaults to Remove (any existing)</param>
		/// <returns>The Url object with the query parameters added</returns>
		public Url SetQueryParams( Object values, NullValueHandling nullValueHandling = NullValueHandling.Remove )
		{
			Queries.Merge( values, nullValueHandling );
			return this;
		}

		/// <summary>Adds multiple parameters without values to the query.</summary>
		/// <param name="names">Names of query parameters.</param>
		/// <returns>The Url object with the query parameter added</returns>
		public Url SetQueryParams( IEnumerable<String> names )
		{
			foreach ( var name in names.Where( n => n != null ) )
				SetQueryParam( name );
			return this;
		}

		/// <summary>Adds multiple parameters without values to the query.</summary>
		/// <param name="names">Names of query parameters</param>
		/// <returns>The Url object with the query parameter added.</returns>
		public Url SetQueryParams( params String[] names ) => SetQueryParams( names as IEnumerable<String> );

		/// <summary>Removes a name/value pair from the query by name.</summary>
		/// <param name="name">Query string parameter name to remove</param>
		/// <returns>The Url object with the query parameter removed</returns>
		public Url RemoveQueryParam( String name )
		{
			Queries.Remove( name );
			return this;
		}

		/// <summary>Removes multiple name/value pairs from the query by name.</summary>
		/// <param name="names">Query string parameter names to remove</param>
		/// <returns>The Url object.</returns>
		public Url RemoveQueryParams( params String[] names )
		{
			foreach ( var name in names )
				Queries.Remove( name );
			return this;
		}

		/// <summary>Removes multiple name/value pairs from the query by name.</summary>
		/// <param name="names">Query string parameter names to remove</param>
		/// <returns>The Url object with the query parameters removed</returns>
		public Url RemoveQueryParams( IEnumerable<String> names )
		{
			foreach ( var name in names )
				Queries.Remove( name );
			return this;
		}

		/// <summary>Removes the entire query component of the URL.</summary>
		/// <returns>The Url object.</returns>
		public Url RemoveQuery()
		{
			Queries.Clear();
			return this;
		}

		/// <summary>Set the URL fragment fluently.</summary>
		/// <param name="fragment">The part of the URL after #</param>
		/// <returns>The Url object with the new fragment set</returns>
		public Url SetFragment( String fragment )
		{
			Fragment = fragment ?? "";
			return this;
		}

		/// <summary>Removes the URL fragment including the #.</summary>
		/// <returns>The Url object with the fragment removed</returns>
		public Url RemoveFragment() => SetFragment( "" );

		/// <summary>Resets the URL to its root, including the scheme, any user info, host, and port (if specified).</summary>
		/// <returns>The Url object trimmed to its root.</returns>
		public Url ResetToRoot()
		{
			PathSegments.Clear();
			Queries.Clear();
			Fragment = "";
			_leadingSlash = false;
			_trailingSlash = false;
			return this;
		}

		/// <summary>Resets the URL to its original state as set in the constructor.</summary>
		public Url Reset()
		{
			if ( _parsed )
			{
				_scheme = null;
				_userInfo = null;
				_host = null;
				_port = null;
				_pathSegments = null;
				_queryParams = null;
				_fragment = null;
				_leadingSlash = false;
				_trailingSlash = false;
				_parsed = false;
			}

			return this;
		}

		/// <summary>Creates a copy of this Url.</summary>
		public Url Clone() => new Url( this );


		/// <summary>Create FullPath</summary>
		public String FullPath => this.ToString();

		/// <summary>Converts this Url object to its string representation.</summary>
		/// <param name="encodeSpaceAsPlus">Indicates whether to encode spaces with the "+" character instead of "%20"</param>
		public String ToString( Boolean encodeSpaceAsPlus )
		{
			if ( !_parsed ) return _baseUrl;
			return String.Concat
			(
				Root,
				encodeSpaceAsPlus ? Path.Replace( "%20", "+" ) : Path,
				Queries.Any() ? "?" : "",
				Queries.ToString( encodeSpaceAsPlus ),
				Fragment?.Length > 0 ? "#" : "",
				Fragment
			);
		}

		/// <summary>Converts this Url object to its string representation.</summary>
		public override String ToString() => ToString( false );

		/// <summary>Converts this Url object to System.Uri</summary>
		public Uri ToUri() => new Uri( this, UriKind.RelativeOrAbsolute );

		/// <summary>Implicit conversion from Url to String.</summary>
		public static implicit operator String( Url url ) => url?.ToString();

		/// <summary>Implicit conversion from String to Url.</summary>
		public static implicit operator Url( String url ) => new Url( url );

		/// <summary>Implicit conversion from System.Uri to Flurl.Url.</summary>
		public static implicit operator Url( Uri uri ) => new Url( uri.ToString() );

		/// <summary>True if obj is an instance of Url and its string representation is equal to this instance's string representation.</summary>
		public override Boolean Equals( Object obj ) => obj is Url url && this.ToString().Equals( url.ToString() );

		/// <summary>Returns the hashcode for this Url</summary>
		public override Int32 GetHashCode() => this.ToString().GetHashCode();


		/// <summary>
		/// Basically a Path.Combine for URLs. Ensures exactly one '/' separates each segment, and exactly on '&amp;' separates each query parameter.
		/// URL-encodes illegal characters but not reserved characters.
		/// </summary>
		public static String Combine( params String[] parts )
		{
			if ( parts == null )
				throw new ArgumentNullException( nameof( parts ) );

			var result = "";
			Boolean inQuery = false, inFragment = false;

			String CombineEnsureSingleSeparator( String a, String b, Char separator )
			{
				if ( String.IsNullOrEmpty( a ) ) return b;
				if ( String.IsNullOrEmpty( b ) ) return a;
				return a.TrimEnd( separator ) + separator + b.TrimStart( separator );
			}

			foreach ( var part in parts )
			{
				if ( String.IsNullOrEmpty( part ) )
					continue;

				if ( result.EndsWith( "?" ) || part.StartsWith( "?" ) )
					result = CombineEnsureSingleSeparator( result, part, '?' );
				else if ( result.EndsWith( "#" ) || part.StartsWith( "#" ) )
					result = CombineEnsureSingleSeparator( result, part, '#' );
				else if ( inFragment )
					result += part;
				else if ( inQuery )
					result = CombineEnsureSingleSeparator( result, part, '&' );
				else
					result = CombineEnsureSingleSeparator( result, part, '/' );

				if ( part.Contains( "#" ) )
				{
					inQuery = false;
					inFragment = true;
				}
				else if ( !inFragment && part.Contains( "?" ) )
				{
					inQuery = true;
				}
			}

			return EncodeIllegalCharacters( result );
		}

		/// <summary>Decodes a URL-encoded string</summary>
		/// <param name="s">The URL-encoded string.</param>
		/// <param name="interpretPlusAsSpace">If true, any '+' character will be decoded to a space.</param>
		public static String Decode( String s, Boolean interpretPlusAsSpace ) { return String.IsNullOrEmpty( s ) ? s : Uri.UnescapeDataString( interpretPlusAsSpace ? s.Replace( "+", " " ) : s ); }


		/// <summary>URL-encodes a string, including reserved characters such as '/' and '?'</summary>
		/// <param name="s">The string to encode.</param>
		/// <param name="encodeSpaceAsPlus">If true, spaces will be encoded as + signs. Otherwise, they'll be encoded as %20.</param>
		public static String Encode( String s, Boolean encodeSpaceAsPlus = false )
		{
			if ( String.IsNullOrEmpty( s ) )
				return s;

			if ( s.Length > MAX_URL_LENGTH )
			{
				// Uri.EscapeDataString is going to throw because the string is "too long", so break it into pieces and concat them
				var parts = new String[(Int32) Math.Ceiling( (Double) s.Length / MAX_URL_LENGTH )];
				for ( var i = 0; i < parts.Length; i++ )
				{
					var start = i * MAX_URL_LENGTH;
					var len = Math.Min( MAX_URL_LENGTH, s.Length - start );
					parts[i] = Uri.EscapeDataString( s.Substring( start, len ) );
				}

				s = String.Concat( parts );
			}
			else
			{
				s = Uri.EscapeDataString( s );
			}

			return encodeSpaceAsPlus ? s.Replace( "%20", "+" ) : s;
		}

		/// <summary>URL-encodes characters in a string that are neither reserved nor unreserved. Avoids encoding reserved characters such as '/' and '?'. Avoids encoding '%' if it begins a %-hex-hex sequence (i.e. avoids double-encoding)</summary>
		/// <param name="s">The string to encode.</param>
		/// <param name="encodeSpaceAsPlus">If true, spaces will be encoded as + signs. Otherwise, they'll be encoded as %20.</param>
		public static String EncodeIllegalCharacters( String s, Boolean encodeSpaceAsPlus = false )
		{
			if ( String.IsNullOrEmpty( s ) )
				return s;

			if ( encodeSpaceAsPlus )
				s = s.Replace( " ", "+" );

			// Uri.EscapeUriString mostly does what we want - encodes illegal characters only - but it has a quirk
			// in that % isn't illegal if it's the start of a %-encoded sequence https://stackoverflow.com/a/47636037/62600

			// no % characters, so avoid the regex overhead
			if ( !s.Contains( "%" ) )
				return Uri.EscapeUriString( s );

			// pick out all %-hex-hex matches and avoid double-encoding
			return Regex.Replace( s, "(.*?)((%[0-9A-Fa-f]{2})|$)", c =>
			{
				var a = c.Groups[1].Value; // group 1 is a sequence with no %-encoding - encode illegal characters
				var b = c.Groups[2].Value; // group 2 is a valid 3-character %-encoded sequence - leave it alone!
				return Uri.EscapeUriString( a ) + b;
			} );
		}

		/// <summary>Checks if a string is a well-formed absolute URL</summary>
		/// <param name="url">The string to check</param>
		/// <returns>true if the string is a well-formed absolute URL</returns>
		public static Boolean IsValid( String url ) => url != null && Uri.IsWellFormedUriString( url, UriKind.Absolute );
	}
}
