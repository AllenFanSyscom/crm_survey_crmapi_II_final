using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace syscom.Collections
{
	public class AnonymousObjectDictionary : IDictionary<String, Object>, ICollection<KeyValuePair<String, Object>>, IEnumerable<KeyValuePair<String, Object>>, IEnumerable
	{
		Dictionary<String, Object> _dictionary;

		public Int32 Count => _dictionary.Count;

		public Dictionary<String, Object>.KeyCollection Keys => _dictionary.Keys;

		public Dictionary<String, Object>.ValueCollection Values => _dictionary.Values;

		public Object this[ String key ]
		{
			get
			{
				Object obj;
				TryGetValue( key, out obj );
				return obj;
			}
			set => _dictionary[key] = value;
		}

		ICollection<String> IDictionary<String, Object>.Keys => (ICollection<String>) _dictionary.Keys;

		ICollection<Object> IDictionary<String, Object>.Values => (ICollection<Object>) _dictionary.Values;

		Boolean ICollection<KeyValuePair<String, Object>>.IsReadOnly => ( (ICollection<KeyValuePair<String, Object>>) _dictionary ).IsReadOnly;

		public AnonymousObjectDictionary() { _dictionary = new Dictionary<String, Object>( (IEqualityComparer<String>) StringComparer.OrdinalIgnoreCase ); }

		public AnonymousObjectDictionary( Object keyValues )
		{
			_dictionary = new Dictionary<String, Object>( (IEqualityComparer<String>) StringComparer.OrdinalIgnoreCase );
			AddValues( keyValues );
		}

		public AnonymousObjectDictionary( IDictionary<String, Object> dictionary ) { _dictionary = new Dictionary<String, Object>( dictionary, (IEqualityComparer<String>) StringComparer.OrdinalIgnoreCase ); }

		public void Add( String key, Object value ) { _dictionary.Add( key, value ); }

		public void Clear() { _dictionary.Clear(); }

		public Boolean ContainsKey( String key ) { return _dictionary.ContainsKey( key ); }

		public Boolean ContainsValue( Object value ) { return _dictionary.ContainsValue( value ); }

		public Dictionary<String, Object>.Enumerator GetEnumerator() { return _dictionary.GetEnumerator(); }

		public Boolean Remove( String key ) { return _dictionary.Remove( key ); }

		public Boolean TryGetValue( String key, out Object value ) { return _dictionary.TryGetValue( key, out value ); }

		void ICollection<KeyValuePair<String, Object>>.Add( KeyValuePair<String, Object> item ) { ( (ICollection<KeyValuePair<String, Object>>) _dictionary ).Add( item ); }

		Boolean ICollection<KeyValuePair<String, Object>>.Contains( KeyValuePair<String, Object> item ) { return ( (ICollection<KeyValuePair<String, Object>>) _dictionary ).Contains( item ); }

		void ICollection<KeyValuePair<String, Object>>.CopyTo( KeyValuePair<String, Object>[] array, Int32 arrayIndex ) { ( (ICollection<KeyValuePair<String, Object>>) _dictionary ).CopyTo( array, arrayIndex ); }

		Boolean ICollection<KeyValuePair<String, Object>>.Remove( KeyValuePair<String, Object> item ) { return ( (ICollection<KeyValuePair<String, Object>>) _dictionary ).Remove( item ); }

		IEnumerator<KeyValuePair<String, Object>> IEnumerable<KeyValuePair<String, Object>>.GetEnumerator() { return (IEnumerator<KeyValuePair<String, Object>>) GetEnumerator(); }

		IEnumerator IEnumerable.GetEnumerator() { return (IEnumerator) GetEnumerator(); }

		void AddValues( Object values )
		{
			if ( values == null ) return;
			foreach ( System.ComponentModel.PropertyDescriptor propertyDescriptor in System.ComponentModel.TypeDescriptor.GetProperties( values ) )
			{
				var obj = propertyDescriptor.GetValue( values );
				Add( propertyDescriptor.Name, obj );
			}
		}
	}
}