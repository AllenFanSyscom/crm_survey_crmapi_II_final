using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using syscom.logging;
using syscom.Net;

namespace syscom.conn.socket
{
	public class SocketClientConnector : IDisposable
	{
		public ILogger Log { get; internal set; }

		Int32 SocketErrorCode = 0;
		public IPEndPoint TargetEndPoint { get; private set; }
		public IPEndPoint LocalEndPoint { get; private set; }
		Socket clientSocket;

		Boolean IsClosed;

		public CommunicateStatus Status { get; private set; }

		public SocketClientConnector( String hostName, Int32 port )
		{
			Log = LogUtils.GetLogger( "SOCKET" );

			var host = Dns.GetHostEntry( hostName );
			var addressList = host.AddressList;

			TargetEndPoint = new IPEndPoint( addressList[addressList.Length - 1], port );
		}

		public SocketClientConnector( IPEndPoint remoteEndpoint, IPEndPoint? localEndpoint = null )
		{
			Log = LogUtils.GetLogger( "SOCKET" );

			TargetEndPoint = remoteEndpoint;
			if ( localEndpoint != null ) LocalEndPoint = localEndpoint;
		}

		public SocketClientConnector( Socket socket )
		{
			Log = LogUtils.GetLogger( "SOCKET" );

			clientSocket = socket;
			TargetEndPoint = clientSocket.RemoteEndPoint as IPEndPoint;
		}

		Object _mutex = new Object();

		public void Close( [CallerMemberName] String callerName = "", [CallerLineNumber] Int32 callerLineNumber = 0 )
		{
			//Log.LogFile( "[SocketConnector] call Start to Close -  from " + callerName + " : " + callerLineNumber );
			lock ( _mutex )
			{
				if ( IsClosed )
					//Log.LogFile( "[SocketConnector] already Closed -  from " + callerName + " : " + callerLineNumber );
					return;
				try
				{
					if ( clientSocket != null )
					{
						if ( clientSocket.Connected )
						{
							clientSocket.Shutdown( SocketShutdown.Both );
							clientSocket.Disconnect( true );
						}

						clientSocket.Close();
						clientSocket.Dispose();
						clientSocket = null;
					}

					Status = CommunicateStatus.None;
					//Log.LogFile( "[SocketConnector] call End to Close -  from " + callerName + " : " + callerLineNumber );
					IsClosed = true;
				}
				catch ( Exception ex )
				{
					Log.Trace( "Close Error SocketClientConnector: " + ex.Message );
				}
			}
		}


		public void Dispose() { Close(); }

		public Int32 ConnectSync()
		{
			lock ( _mutex )
			{
				IsClosed = false;
				if ( clientSocket == null )
				{
					clientSocket = new Socket( TargetEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp )
					{
						NoDelay = true,
						LingerState = new LingerOption( false, 0 )
					};
					clientSocket.SetSocketOption( SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true );
					try
					{
						if ( LocalEndPoint != null ) clientSocket.Bind( LocalEndPoint );
					}
					catch ( SocketException sex )
					{
						Log.Error( "Binding failed [ " + LocalEndPoint + " ], ErrorCode[" + sex.ErrorCode + "], SocketErrorCode[" + sex.SocketErrorCode + "], " + sex.Message );
						Status = CommunicateStatus.Error;
						return -sex.ErrorCode;
					}
				}


				try
				{
					clientSocket.Connect( TargetEndPoint );

					Status = CommunicateStatus.Connected;
					return 0;
				}
				catch ( SocketException sex )
				{
					SocketErrorCode++;
					Log.Error( "Connect failed [ " + TargetEndPoint + " ], ErrorCode[" + sex.ErrorCode + "], SocketErrorCode[" + sex.SocketErrorCode + "], " + sex.Message );
					//Log.Trace( "Connect Fail, ErrorCode["+ sex.ErrorCode +"], SocketErrorCode["+ sex.SocketErrorCode +"], " + sex.Message + ", ex Json: " + sex.ToJson() );
					Status = CommunicateStatus.Error;
					return -sex.ErrorCode;
				}
				catch ( Exception ex )
				{
					Log.Error( "Connect Fail, " + ex.Message + ", ex Json: " + ex.ToJson(), ex );
					Status = CommunicateStatus.Error;
					return -1;
				}
			}
		}

		//public Int32 ConnectAsync( Int32 timeout )
		//{
		//	if ( LocalEndPoint != null ) clientSocket.Bind( LocalEndPoint );
		//	var result = clientSocket.BeginConnect( TargetEndPoint, null, null );
		//	var success = result.AsyncWaitHandle.WaitOne( timeout, true );
		//	if ( success ) { IsConnected = true; return 0; }

		//	IsConnected = false;
		//	clientSocket.Close();
		//	return -1;
		//}


		/// <summary>
		/// 同步傳送資料, 正常回傳正值(已傳送資料長度)
		/// <para>異常傳回負值的SocketErrorCode, 其他問題回傳-1</para>
		/// </summary>
		public Int32 SendSync( String message, Encoding encoding )
		{
			var buffer = encoding.GetBytes( message );
			var returnCode = SendSync( ref buffer );
			return returnCode;
		}

		/// <summary>
		/// 同步傳送資料, 正常回傳正值(已傳送資料長度)
		/// <para>異常傳回負值的SocketErrorCode, 其他問題回傳-1</para>
		/// </summary>
		public Int32 SendSync( ref Byte[] buffer )
		{
			try
			{
				return clientSocket.Send( buffer, SocketFlags.Partial );
			}
			catch ( SocketException sex )
			{
				Log.Error( "SCC底層異常, " + sex.Message );
				Status = CommunicateStatus.Error;
				return -sex.ErrorCode;
			}
			catch ( Exception ex )
			{
				Log.Error( "SCC底層異常, " + ex.Message );
				Status = CommunicateStatus.Error;
				return -1;
			}
		}


		/// <summary>
		/// 同步傳送資料, 正常回傳正值(已接收資料長度)
		/// <para>異常傳回負值的SocketErrorCode, 其他問題回傳-1</para>
		/// </summary>
		public Int32 ReceiveSync( ref Byte[] buffer, Int32 size )
		{
			try
			{
				return clientSocket.Receive( buffer, size, SocketFlags.Partial );
			}
			//catch ( SocketException sex ) { return -sex.ErrorCode; }
			//catch ( Exception ) { return -1; }
			catch ( SocketException sex )
			{
				Log.Error( "SCC底層異常, " + sex.Message );
				Status = CommunicateStatus.Error;
				return -sex.ErrorCode;
			}
			catch ( Exception ex )
			{
				Log.Error( "SCC底層異常, " + ex.Message );
				Status = CommunicateStatus.Error;
				return -1;
			}
		}


		#region Async

		//private const Int32 SEND = 0, RECEVIE = 1;
		//private static AutoResetEvent[] resetEvents =
		//{
		//	new AutoResetEvent(false),
		//	new AutoResetEvent(false)
		//};
		//private static AutoResetEvent resetEventConnect = new AutoResetEvent( false );

		//public Int32 ConnectAsync()
		//{
		//	if ( Log == null ) Log = new SimpleProcessLogger();

		//	var connectArgs = new SocketAsyncEventArgs();
		//	connectArgs.UserToken = this.clientSocket;
		//	connectArgs.RemoteEndPoint = this.TargetEndPoint;
		//	connectArgs.Completed += OnConnect;

		//	clientSocket.ConnectAsync( connectArgs );
		//	resetEventConnect.WaitOne();

		//	var errorCode = connectArgs.SocketError;
		//	if ( errorCode != SocketError.Success )
		//	{
		//		return (Int32)errorCode;
		//	}
		//	return 0;
		//}
		//void OnConnect( object sender, SocketAsyncEventArgs saea )
		//{
		//	resetEventConnect.Set();
		//	this.connected = ( saea.SocketError == SocketError.Success );
		//}

		//public void SendASync( String message, Encoding encoder )
		//{
		//	if ( this.connected )
		//	{
		//		var sendBuffer = encoder.GetBytes( message );

		//		var completeArgs = new SocketAsyncEventArgs();
		//		completeArgs.SetBuffer( sendBuffer, 0, sendBuffer.Length );
		//		completeArgs.UserToken = this.clientSocket;
		//		completeArgs.RemoteEndPoint = this.TargetEndPoint;
		//		completeArgs.Completed += OnSend;

		//		clientSocket.SendAsync( completeArgs );

		//		resetEvents[SEND].WaitOne(); //等待傳送完畢
		//		//AutoResetEvent.WaitAll( resetEvents );
		//	}
		//	else
		//	{
		//		throw new SocketException( (Int32)SocketError.NotConnected );
		//	}
		//}
		//void OnSend( object sender, SocketAsyncEventArgs saea )
		//{
		//	if ( saea.SocketError == SocketError.Success )
		//	{
		//	}
		//	else
		//	{
		//		this.ProcessError( saea );
		//	}
		//	resetEvents[SEND].Set();
		//}


		static Int32 _receiveBufferSize = 1024 * 8; //8K
		public Action OnAsyncReceiveError { get; set; }
		public Action<Byte[]> OnDataReceived { get; set; }

		public void ReceiveAsync()
		{
			var saea = new SocketAsyncEventArgs();
			saea.UserToken = clientSocket;
			saea.RemoteEndPoint = TargetEndPoint;
			saea.Completed += OnReceived;

			saea.SetBuffer( new Byte[_receiveBufferSize], 0, _receiveBufferSize );

			if ( !clientSocket.ReceiveAsync( saea ) )
				OnReceived( this, saea );
			//Log.Warn( "Async ReceiveAsync Error, manual switch to continue..." );
		}

		void OnReceived( Object sender, SocketAsyncEventArgs saea )
		{
			var state = saea.SocketError;
			var transfLength = saea.BytesTransferred;
			if ( transfLength < 0 && OnAsyncReceiveError != null )
			{
				OnAsyncReceiveError();
				Status = CommunicateStatus.Error;
				return;
			}

			if ( state != SocketError.Success && OnAsyncReceiveError != null )
			{
				OnAsyncReceiveError();
				Status = CommunicateStatus.Error;
				return;
			}


			var receiveBuf = new Byte[transfLength];
			saea.Buffer.BlockCloneTo( ref receiveBuf, saea.Offset, transfLength );

			//resetEvents[RECEVIE].Set();
			OnDataReceived?.Invoke( receiveBuf );
		}

		#endregion

		//void ProcessError( SocketAsyncEventArgs saea )
		//{
		//	var socket = saea.UserToken as Socket;
		//	if ( socket.IsConnected )
		//	{
		//		try
		//		{
		//			socket.Shutdown( SocketShutdown.Both );
		//		}
		//		catch ( Exception )
		//		{
		//		}
		//		finally
		//		{
		//			if ( socket.IsConnected ) socket.Close();
		//		}
		//	}
		//	throw new SocketException( (Int32)saea.SocketError );
		//}
	}
}
