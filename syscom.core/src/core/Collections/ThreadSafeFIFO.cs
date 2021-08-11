using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace syscom.Collections
{
	public class ThreadSafeFIFO<T>
	{
		Queue<T> _poolQueue;

		public ThreadSafeFIFO() { _poolQueue = new Queue<T>(); }
		public ThreadSafeFIFO( Int32 poolCapacity ) { _poolQueue = new Queue<T>( poolCapacity ); }

		//[Note] KD Count跟HasData兩個methods都必須包含Lock
		/// <summary>取得Pool的數量</summary>
		public Int32 Count => _poolQueue.Count;

		/// <summary>經由Count判斷是否有資料</summary>
		public Boolean HasData => Count != 0;

		public T Peek()
		{
			try { return _poolQueue.Peek(); }
			catch { return default( T ); }
		}

		public T Dequeue()
		{
			Monitor.Enter( _poolQueue );
			try
			{
				return _poolQueue.Dequeue();
			}
			catch ( Exception ex )
			{
				throw ex;
			}
			finally
			{
				Monitor.Exit( _poolQueue );
			}
		}

		public void Enqueue( T item )
		{
			if ( item == null ) throw Err.Utility( "item" );

			Monitor.Enter( _poolQueue );
			try
			{
				_poolQueue.Enqueue( item );
			}
			catch ( Exception ex )
			{
				throw ex;
			}
			finally
			{
				Monitor.Exit( _poolQueue );
			}
		}
	}
}