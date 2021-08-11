using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net
{
	public static class SocketExtensions
	{
		//================================================================================================================================================
		// Common
		//================================================================================================================================================
		/// <summary>測試Socket是否已經斷線</summary>
		public static Boolean IsConnected( this Socket socket )
		{
			try { return !( socket.Poll( 1, SelectMode.SelectRead ) && socket.Available == 0 ); }
			catch ( ObjectDisposedException ) { return false; }
			catch ( SocketException ) { return false; }
		}

		//================================================================================================================================================
		// Sync
		//================================================================================================================================================

		/// <summary>
		/// 遞迴接收指定長度的資料, 回傳最終接收到資料的長度 (除非斷線, 否則會一直接收到收完指定的長度, 當預期接收資料超過對方傳送資料時, 會停留在Block模式 )
		/// </summary>
		public static Int32 RecursiveReceiveBy( this Socket socket, ref Byte[] totalBuffer )
		{
			if ( totalBuffer == null ) throw new ArgumentNullException( "totalBuffer", "Buffer不得為空" ); //totalBuffer = new Byte[sizeOfExpect];

			var totalReceiveLength = 0;

			while ( totalReceiveLength < totalBuffer.Length )
			{
				var needReceiveLength = totalBuffer.Length - totalReceiveLength;
				var receivedLength = socket.Receive( totalBuffer, totalReceiveLength, needReceiveLength, SocketFlags.None );
				if ( receivedLength == 0 )
				{
					if ( totalBuffer.Length == 0 ) return 0; //如果預期接收的就是0的話, 就不報錯誤
					throw new Exception( "Socket Received Zero Length" );
				}

				totalReceiveLength += receivedLength;
			}

			//var receiveLength = socket.Receive( totalBuffer, offset, sizeOfExpect, SocketFlags.None );
			//if ( receiveLength == 0 )
			//{
			//	if ( sizeOfExpect == 0 ) return 0; //如果預期接收的就是0的話, 就不報錯誤
			//	throw new Exception( "Socket Receive Zero Length" );
			//}
			//totalReceiveLength += receiveLength;

			//if ( receiveLength >= sizeOfExpect ) return totalReceiveLength;


			//var sizeOfLeave = sizeOfExpect - receiveLength;
			//var childReceivedLength = socket.RecursiveReceiveBy( sizeOfLeave, ref totalBuffer, receiveLength );
			//totalReceiveLength += childReceivedLength;

			return totalReceiveLength;
		}

		//================================================================================================================================================
		// Async
		//================================================================================================================================================

		public static Task<Socket> AcceptAsync( this Socket socket )
		{
			var tcs = new TaskCompletionSource<Socket>( socket );
			socket.BeginAccept
			(
				iar =>
				{
					var t = (TaskCompletionSource<Socket>) iar.AsyncState;
					var s = (Socket) t.Task.AsyncState;
					try { t.TrySetResult( s.EndAccept( iar ) ); }
					catch ( Exception exc ) { t.TrySetException( exc ); }
				},
				tcs
			);
			return tcs.Task;
		}

		public static Task<Int32> SendAsync( this Socket socket, Byte[] buffer, Int32 offset, Int32 size, SocketFlags socketFlags = SocketFlags.None )
		{
			var tcs = new TaskCompletionSource<Int32>( socket );
			socket.BeginSend( buffer, offset, size, socketFlags, iar =>
			{
				var t = (TaskCompletionSource<Int32>) iar.AsyncState;
				var s = (Socket) t.Task.AsyncState;
				try { t.TrySetResult( s.EndSend( iar ) ); }
				catch ( Exception exc ) { t.TrySetException( exc ); }
			}, tcs );
			return tcs.Task;
		}

		//with a slightly more efficient implementation that avoids some of the extra allocations here:
		public static Task<Int32> ReceiveAsync( this Socket socket, Byte[] buffer, Int32 offset, Int32 size, SocketFlags socketFlags = SocketFlags.None )
		{
			var tcs = new TaskCompletionSource<Int32>( socket );
			socket.BeginReceive( buffer, offset, size, socketFlags, iar =>
			{
				var t = (TaskCompletionSource<Int32>) iar.AsyncState;
				var s = (Socket) t.Task.AsyncState;
				try { t.TrySetResult( s.EndReceive( iar ) ); }
				catch ( Exception exc ) { t.TrySetException( exc ); }
			}, tcs );
			return tcs.Task;
		}

		public static async Task<Byte[]> ReceiveAsync( this Socket socket, Int32 count )
		{
			var buffer = new Byte[count];
			var length = 0;
			do
			{
				var num = await ReceiveAsync( socket, buffer, length, count );
				if ( num == 0 ) break;

				length += num;
				count -= num;
			}
			while ( count > 0 );

			if ( length != buffer.Length )
			{
				if ( !socket.IsConnected() ) throw new SocketException();
				throw new IOException( "packet is truncated." );
			}

			return buffer;
		}


		public static Boolean ConnectAsync( this Socket socket, IPEndPoint remoteEndpoint, Int32 timeout )
		{
			var locker = new AutoResetEvent( false );
			var connected = false;
			socket.BeginConnect
			(
				remoteEndpoint,
				iar =>
				{
					try
					{
						socket.EndConnect( iar );
						connected = socket.Connected;
						locker.Set();
					}
					catch ( Exception ) { }
				},
				locker
			);

			locker.WaitOne( timeout );

			//if ( !connected ) throw new TimeoutException( "連線目標[" + remoteEndpoint + "]逾時" );

			return connected;
		}

		#region waiting implement or testing

		//public static Task<Socket> AcceptAsync( this Socket socket )
		//{
		//	return Task<Socket>.Factory.FromAsync( socket.BeginAccept, socket.EndAccept, null );
		//}


		//public static Task<Socket> AcceptAsync( this Socket socket )
		//{
		//	var tcs = new TaskCompletionSource<Socket>( socket );
		//	socket.BeginAccept
		//	(
		//		iar =>
		//		{
		//			var t = (TaskCompletionSource<Socket>)iar.AsyncState;
		//			var s = (Socket)t.Task.AsyncState;
		//			try { t.TrySetResult( s.EndAccept( iar ) ); }
		//			catch ( Exception exc ) { t.TrySetException( exc ); }
		//		},
		//		tcs
		//	);
		//	return tcs.Task;
		//}

		#endregion
	}
}