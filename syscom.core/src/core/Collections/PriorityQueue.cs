using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace syscom.Collections
{
	public class PriorityQueue<T>
	{
		Object locker;
		Int32 maxSize;
		Int32 count;
		LinkedList<T>[] Buckets;

		public PriorityQueue( Int32 maxPriority, Int32 maxSize )
		{
			locker = new Object();
			if ( maxSize < 0 )
				throw new IndexOutOfRangeException( "maxSize" );
			else
				this.maxSize = maxSize;
			count = 0;
			if ( maxPriority < 0 )
			{
				throw new IndexOutOfRangeException( "maxPriority" );
			}
			else
			{
				Buckets = new LinkedList<T>[maxPriority];
				for ( var i = 0; i < Buckets.Length; i++ ) Buckets[i] = new LinkedList<T>();
			}
		}

		public Boolean TryUnsafeEnqueue( T item, Int32 priority )
		{
			if ( priority < 0 || priority >= Buckets.Length )
				throw new IndexOutOfRangeException( "priority" );

			Buckets[priority].AddLast( item );
			count++;

			if ( count > maxSize )
			{
				UnsafeDiscardLowestItem();
				Debug.Assert( count <= maxSize, "Collection Count should be less than or equal to MaxSize" );
				return false;
			}

			return true; // always succeeds
		}

		public Boolean TryUnsafeDequeue( out T res )
		{
			var bucket = Buckets.FirstOrDefault( x => x.Count > 0 );
			if ( bucket != null )
			{
				res = bucket.First.Value;
				bucket.RemoveFirst();
				count--;
				return true; // found item, succeeds
			}

			res = default( T );
			return false; // didn't find an item, fail
		}

		void UnsafeDiscardLowestItem()
		{
			var bucket = Buckets.Reverse().FirstOrDefault( x => x.Count > 0 );
			if ( bucket != null )
			{
				bucket.RemoveLast();
				count--;
			}
		}

		public Boolean TryEnqueue( T item, Int32 priority )
		{
			lock ( locker )
			{
				return TryUnsafeEnqueue( item, priority );
			}
		}

		public Boolean TryDequeue( out T res )
		{
			lock ( locker )
			{
				return TryUnsafeDequeue( out res );
			}
		}

		public Int32 Count
		{
			get
			{
				lock ( locker ) { return count; }
			}
		}

		public Int32 MaxSize => maxSize;

		public Object SyncRoot => locker;
	}
}