using System;
using System.Threading;

namespace libs.ProtoBuf
{
	internal sealed class BufferPool
	{
		internal static void Flush()
		{
#if PLAT_NO_INTERLOCKED
            lock(pool)
            {
                for (int i = 0; i < pool.Length; i++) pool[i] = null;
            }
#else
			for ( var i = 0; i < pool.Length; i++ ) Interlocked.Exchange( ref pool[i], null ); // and drop the old value on the floor
#endif
		}

		BufferPool() { }
		const Int32 PoolSize = 20;
		internal const Int32 BufferLength = 1024;
		static readonly Object[] pool = new Object[PoolSize];

		internal static Byte[] GetBuffer()
		{
			Object tmp;
#if PLAT_NO_INTERLOCKED
            lock(pool)
            {
                for (int i = 0; i < pool.Length; i++)
                {
                    if((tmp = pool[i]) != null)
                    {
                        pool[i] = null;
                        return (byte[])tmp;
                    }
                }
            }
#else
			for ( var i = 0; i < pool.Length; i++ )
				if ( ( tmp = Interlocked.Exchange( ref pool[i], null ) ) != null )
					return (Byte[]) tmp;
#endif
			return new Byte[BufferLength];
		}

		internal static void ResizeAndFlushLeft( ref Byte[] buffer, Int32 toFitAtLeastBytes, Int32 copyFromIndex, Int32 copyBytes )
		{
			Helpers.DebugAssert( buffer != null );
			Helpers.DebugAssert( toFitAtLeastBytes > buffer.Length );
			Helpers.DebugAssert( copyFromIndex >= 0 );
			Helpers.DebugAssert( copyBytes >= 0 );

			// try doubling, else match
			var newLength = buffer.Length * 2;
			if ( newLength < toFitAtLeastBytes ) newLength = toFitAtLeastBytes;

			var newBuffer = new Byte[newLength];
			if ( copyBytes > 0 ) Helpers.BlockCopy( buffer, copyFromIndex, newBuffer, 0, copyBytes );
			if ( buffer.Length == BufferLength ) ReleaseBufferToPool( ref buffer );
			buffer = newBuffer;
		}

		internal static void ReleaseBufferToPool( ref Byte[] buffer )
		{
			if ( buffer == null ) return;
			if ( buffer.Length == BufferLength )
			{
#if PLAT_NO_INTERLOCKED
                lock (pool)
                {
                    for (int i = 0; i < pool.Length; i++)
                    {
                        if(pool[i] == null)
                        {
                            pool[i] = buffer;
                            break;
                        }
                    }
                }
#else
				for ( var i = 0; i < pool.Length; i++ )
					if ( Interlocked.CompareExchange( ref pool[i], buffer, null ) == null )
						break; // found a null; swapped it in
#endif
			}

			// if no space, just drop it on the floor
			buffer = null;
		}
	}
}