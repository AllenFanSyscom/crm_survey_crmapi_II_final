using System;
using System.Collections;

namespace libs.ProtoBuf.Meta
{
	internal sealed class MutableList : BasicList
	{
		/*  Like BasicList, but allows existing values to be changed
		 */
		public new Object this[ Int32 index ] { get => head[index]; set => head[index] = value; }
		public void RemoveLast() { head.RemoveLastWithMutate(); }

		public void Clear() { head.Clear(); }
	}

	internal class BasicList : IEnumerable
	{
		/* Requirements:
		 *   - Fast access by index
		 *   - Immutable in the tail, so a node can be read (iterated) without locking
		 *   - Lock-free tail handling must match the memory mode; struct for Node
		 *     wouldn't work as "read" would not be atomic
		 *   - Only operation required is append, but this shouldn't go out of its
		 *     way to be inefficient
		 *   - Assume that the caller is handling thread-safety (to co-ordinate with
		 *     other code); no attempt to be thread-safe
		 *   - Assume that the data is private; internal data structure is allowed to
		 *     be mutable (i.e. array is fine as long as we don't screw it up)
		 */
		static readonly Node nil = new Node( null, 0 );
		public void CopyTo( Array array, Int32 offset ) { head.CopyTo( array, offset ); }
		protected Node head = nil;
		public Int32 Add( Object value ) { return ( head = head.Append( value ) ).Length - 1; }
		public Object this[ Int32 index ] => head[index];

		//public object TryGet(int index)
		//{
		//    return head.TryGet(index);
		//}
		public void Trim() { head = head.Trim(); }
		public Int32 Count => head.Length;
		IEnumerator IEnumerable.GetEnumerator() { return new NodeEnumerator( head ); }
		public NodeEnumerator GetEnumerator() { return new NodeEnumerator( head ); }

		public struct NodeEnumerator : IEnumerator
		{
			Int32 position;
			readonly Node node;

			internal NodeEnumerator( Node node )
			{
				position = -1;
				this.node = node;
			}

			void IEnumerator.Reset() { position = -1; }
			public Object Current => node[position];

			public Boolean MoveNext()
			{
				var len = node.Length;
				return position <= len && ++position < len;
			}
		}

		internal sealed class Node
		{
			public Object this[ Int32 index ]
			{
				get
				{
					if ( index >= 0 && index < length ) return data[index];
					throw new ArgumentOutOfRangeException( "index" );
				}
				set
				{
					if ( index >= 0 && index < length )
						data[index] = value;
					else
						throw new ArgumentOutOfRangeException( "index" );
				}
			}

			//public object TryGet(int index)
			//{
			//    return (index >= 0 && index < length) ? data[index] : null;
			//}
			readonly Object[] data;

			Int32 length;
			public Int32 Length => length;

			internal Node( Object[] data, Int32 length )
			{
				Helpers.DebugAssert( data == null && length == 0 ||
				                     data != null && length > 0 && length <= data.Length );
				this.data = data;

				this.length = length;
			}

			public void RemoveLastWithMutate()
			{
				if ( length == 0 ) throw new InvalidOperationException();
				length -= 1;
			}

			public Node Append( Object value )
			{
				Object[] newData;
				var newLength = length + 1;
				if ( data == null )
				{
					newData = new Object[10];
				}
				else if ( length == data.Length )
				{
					newData = new Object[data.Length * 2];
					Array.Copy( data, newData, length );
				}
				else
				{
					newData = data;
				}

				newData[length] = value;
				return new Node( newData, newLength );
			}

			public Node Trim()
			{
				if ( length == 0 || length == data.Length ) return this;
				var newData = new Object[length];
				Array.Copy( data, newData, length );
				return new Node( newData, length );
			}

			internal Int32 IndexOfString( String value )
			{
				for ( var i = 0; i < length; i++ )
					if ( (String) value == (String) data[i] )
						return i;
				return -1;
			}

			internal Int32 IndexOfReference( Object instance )
			{
				for ( var i = 0; i < length; i++ )
					if ( (Object) instance == (Object) data[i] )
						return i;
				// to be a reference check
				return -1;
			}

			internal Int32 IndexOf( MatchPredicate predicate, Object ctx )
			{
				for ( var i = 0; i < length; i++ )
					if ( predicate( data[i], ctx ) )
						return i;
				return -1;
			}

			internal void CopyTo( Array array, Int32 offset )
			{
				if ( length > 0 ) Array.Copy( data, 0, array, offset, length );
			}

			internal void Clear()
			{
				if ( data != null ) Array.Clear( data, 0, data.Length );
				length = 0;
			}
		}

		internal Int32 IndexOf( MatchPredicate predicate, Object ctx ) { return head.IndexOf( predicate, ctx ); }
		internal Int32 IndexOfString( String value ) { return head.IndexOfString( value ); }
		internal Int32 IndexOfReference( Object instance ) { return head.IndexOfReference( instance ); }

		internal delegate Boolean MatchPredicate( Object value, Object ctx );

		internal Boolean Contains( Object value )
		{
			foreach ( var obj in this )
				if ( Equals( obj, value ) )
					return true;
			return false;
		}

		internal sealed class Group
		{
			public readonly Int32 First;
			public readonly BasicList Items;

			public Group( Int32 first )
			{
				First = first;
				Items = new BasicList();
			}
		}

		internal static BasicList GetContiguousGroups( Int32[] keys, Object[] values )
		{
			if ( keys == null ) throw new ArgumentNullException( "keys" );
			if ( values == null ) throw new ArgumentNullException( "values" );
			if ( values.Length < keys.Length ) throw new ArgumentException( "Not all keys are covered by values", "values" );
			var outer = new BasicList();
			Group? group = null;
			for ( var i = 0; i < keys.Length; i++ )
			{
				if ( i == 0 || keys[i] != keys[i - 1] ) @group = null;
				if ( group == null )
				{
					group = new Group( keys[i] );
					outer.Add( group );
				}

				group.Items.Add( values[i] );
			}

			return outer;
		}
	}
}
