// Copyright (c) 2012, Alex Regueiro http://circularbuffer.codeplex.com
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions are met:
//
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

namespace libs.ProtoBuf.Data.Internal
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Threading;

	[ExcludeFromCodeCoverage]
	[SuppressMessage( "StyleCop.CSharp.*", "*", Justification = "Third party" )]
	internal class CircularBuffer<T> : ICollection<T>, IEnumerable<T>, ICollection, IEnumerable
	{
		Int32 capacity;
		Int32 size;
		Int32 head;
		Int32 tail;
		T[] buffer;

		[NonSerialized()] Object syncRoot;

		public CircularBuffer( Int32 capacity )
			: this( capacity, false )
		{
		}

		public CircularBuffer( Int32 capacity, Boolean allowOverflow )
		{
			if ( capacity < 0 )
				throw new ArgumentException( "The buffer capacity must be greater than or equal to zero.", "capacity" );

			this.capacity = capacity;
			size = 0;
			head = 0;
			tail = 0;
			buffer = new T[capacity];
			AllowOverflow = allowOverflow;
		}

		public Boolean AllowOverflow { get; set; }

		public Int32 Capacity
		{
			get => capacity;
			set
			{
				if ( value == capacity )
					return;

				if ( value < size )
					throw new ArgumentOutOfRangeException( "value", "The new capacity must be greater than or equal to the buffer size." );

				var dst = new T[value];
				if ( size > 0 )
					CopyTo( dst );
				buffer = dst;

				capacity = value;
			}
		}

		public Int32 Size => size;

		public Boolean Contains( T item )
		{
			var bufferIndex = head;
			var comparer = EqualityComparer<T>.Default;
			for ( var i = 0; i < size; i++, bufferIndex++ )
			{
				if ( bufferIndex == capacity )
					bufferIndex = 0;

				if ( item == null && buffer[bufferIndex] == null )
					return true;
				else if ( buffer[bufferIndex] != null &&
				          comparer.Equals( buffer[bufferIndex], item ) )
					return true;
			}

			return false;
		}

		public void Clear()
		{
			size = 0;
			head = 0;
			tail = 0;
		}

		public Int32 Put( T[] src ) { return Put( src, 0, src.Length ); }

		public Int32 Put( T[] src, Int32 offset, Int32 count )
		{
			if ( !AllowOverflow && count > capacity - size )
				throw new InvalidOperationException( "The buffer does not have sufficient capacity to put new items." );

			var srcIndex = offset;
			for ( var i = 0; i < count; i++, tail++, srcIndex++ )
			{
				if ( tail == capacity )
					tail = 0;
				buffer[tail] = src[srcIndex];
			}

			size = Math.Min( size + count, capacity );
			return count;
		}

		public void Put( T item )
		{
			if ( !AllowOverflow && size == capacity )
				throw new InvalidOperationException( "The buffer does not have sufficient capacity to put new items." );

			buffer[tail] = item;
			if ( ++tail == capacity )
				tail = 0;
			size++;
		}

		public void Skip( Int32 count )
		{
			head += count;
			if ( head >= capacity )
				head -= capacity;
		}

		public T[] Get( Int32 count )
		{
			var dst = new T[count];
			Get( dst );
			return dst;
		}

		public Int32 Get( T[] dst ) { return Get( dst, 0, dst.Length ); }

		public Int32 Get( T[] dst, Int32 offset, Int32 count )
		{
			var realCount = Math.Min( count, size );
			var dstIndex = offset;
			for ( var i = 0; i < realCount; i++, head++, dstIndex++ )
			{
				if ( head == capacity )
					head = 0;
				dst[dstIndex] = buffer[head];
			}

			size -= realCount;
			return realCount;
		}

		public T Get()
		{
			if ( size == 0 )
				throw new InvalidOperationException( "The buffer is empty." );

			var item = buffer[head];
			if ( ++head == capacity )
				head = 0;
			size--;
			return item;
		}

		public void CopyTo( T[] array ) { CopyTo( array, 0 ); }

		public void CopyTo( T[] array, Int32 arrayIndex ) { CopyTo( 0, array, arrayIndex, size ); }

		public void CopyTo( Int32 index, T[] array, Int32 arrayIndex, Int32 count )
		{
			if ( count > size )
				throw new ArgumentOutOfRangeException( "count", "The read count cannot be greater than the buffer size." );

			var bufferIndex = head;
			for ( var i = 0; i < count; i++, bufferIndex++, arrayIndex++ )
			{
				if ( bufferIndex == capacity )
					bufferIndex = 0;
				array[arrayIndex] = buffer[bufferIndex];
			}
		}

		public IEnumerator<T> GetEnumerator()
		{
			var bufferIndex = head;
			for ( var i = 0; i < size; i++, bufferIndex++ )
			{
				if ( bufferIndex == capacity )
					bufferIndex = 0;

				yield return buffer[bufferIndex];
			}
		}

		public T[] GetBuffer() { return buffer; }

		public T[] ToArray()
		{
			var dst = new T[size];
			CopyTo( dst );
			return dst;
		}

		#region ICollection<T> Members

		Int32 ICollection<T>.Count => Size;

		Boolean ICollection<T>.IsReadOnly => false;

		void ICollection<T>.Add( T item ) { Put( item ); }

		Boolean ICollection<T>.Remove( T item )
		{
			if ( size == 0 )
				return false;

			Get();
			return true;
		}

		#endregion

		#region IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator() { return GetEnumerator(); }

		#endregion

		#region ICollection Members

		Int32 ICollection.Count => Size;

		Boolean ICollection.IsSynchronized => false;

		Object ICollection.SyncRoot
		{
			get
			{
				if ( syncRoot == null )
					Interlocked.CompareExchange( ref syncRoot, new Object(), null );
				return syncRoot;
			}
		}

		void ICollection.CopyTo( Array array, Int32 arrayIndex ) { CopyTo( (T[]) array, arrayIndex ); }

		#endregion

		#region IEnumerable Members

		IEnumerator IEnumerable.GetEnumerator() { return (IEnumerator) GetEnumerator(); }

		#endregion
	}
}