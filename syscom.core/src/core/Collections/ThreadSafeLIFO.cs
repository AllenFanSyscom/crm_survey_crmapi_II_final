using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace syscom.Collections
{
	public class ThreadSafeLIFO<T>
	{
		Stack<T> _poolStack;

		public ThreadSafeLIFO() { _poolStack = new Stack<T>(); }
		public ThreadSafeLIFO( Int32 poolCapacity ) { _poolStack = new Stack<T>( poolCapacity ); }

		/// <summary>取得Pool的數量</summary>
		public Int32 Count => _poolStack.Count;

		/// <summary>經由Count判斷是否有資料</summary>
		public Boolean HasData => Count != 0;

		public T Peek()
		{
			try { return _poolStack.Peek(); }
			catch { return default( T ); }
		}

		public T Pop()
		{
			Monitor.Enter( _poolStack );
			try
			{
				return _poolStack.Pop();
			}
			catch ( Exception ex )
			{
				throw ex;
			}
			finally
			{
				Monitor.Exit( _poolStack );
			}
		}

		public void Push( T item )
		{
			if ( item == null ) throw Err.Utility( "item" );

			Monitor.Enter( _poolStack );
			try
			{
				_poolStack.Push( item );
			}
			catch ( Exception ex )
			{
				throw ex;
			}
			finally
			{
				Monitor.Exit( _poolStack );
			}
		}
	}
}