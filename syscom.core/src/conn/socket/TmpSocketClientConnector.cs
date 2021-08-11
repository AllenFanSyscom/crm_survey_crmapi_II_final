using syscom.logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace syscom.conn.socket
{
	public class TmpSocketClientConnector : SocketClientConnector
	{
		public static Encoding Encoder { get; internal set; }
		static TmpSocketClientConnector() { Encoder = Encoding.ASCII; }


		public TmpSocketClientConnector( String hostName, Int32 port ) : base( hostName, port )
		{
			Log.Id = "SLM";
		}

		public TmpSocketClientConnector( IPEndPoint remoteEndpoint, IPEndPoint localEndpoint ) : base( remoteEndpoint, localEndpoint )
		{
			Log.Id = "SLM";
		}

		public TmpSocketClientConnector( Socket socket ) : base( socket )
		{
			Log.Id = "SLM";
		}


		static readonly Byte[] BYTES_Header = new Byte[] { 0xfe, 0xfe };
		static readonly Byte[] BYTES_CTRL_00 = new Byte[] { 0x30, 0x30 };
		static readonly Byte[] BYTES_CTRL_11 = new Byte[] { 0x31, 0x31 };
		static readonly Byte[] BYTES_CTRL_ZeroInt = new Byte[] { 0x00, 0x00 };
		static readonly Byte[] BYTES_Tailer = new Byte[] { 0xef, 0xef };

		/// <summary>正常接收TMP則回傳0, 接收到SLM-010確定連線回傳1, SLM-030心跳回傳2, SLM錯誤回傳ErrorCode(代碼將變為負數)</summary>
		/// <returns></returns>
		public Int32 TryReceiveTmp( ref Byte[] contentBuffer )
		{
			Byte[] buffer_total = null;

			var returnCode = 0;
			var buffer_head = new Byte[2];
			var buffer_control = new Byte[2];
			var buffer_length = new Byte[2];
			var buffer_tailer = new Byte[2];

			//Log.Trace( "[SLM] Start Receive Header.." );

			//header
			if ( SLMReceiveWithLogStep( ref buffer_head, 2, ref buffer_total, BYTES_Header, "Header" ) != 0 ) return -1;

			//Log.Trace( "[SLM] Start Receive ControlCode.." );
			if ( SLMReceiveWithLogStep( ref buffer_control, 2, ref buffer_total ) != 0 ) return -1;

			var control = Encoder.GetString( buffer_control );
			if ( control.Equals( "10" ) )
				returnCode = 1;
			else if ( control.Equals( "11" ) )
				returnCode = 2;
			else if ( !control.Equals( "00" ) ) //Other, SLM Error
				returnCode = -Int32.Parse( control );
			//Log.Trace( "接收到SLM ControlCode帶錯誤碼, ControlCode[ " + returnCode + " ]" );

			//Log.Trace( "[SLM] Start Receive Length.." );
			//process to here, so Normal = 00
			if ( SLMReceiveWithLogStep( ref buffer_length, 2, ref buffer_total ) != 0 ) return -1;

			var length = buffer_length[0] * 256 + buffer_length[1]; //count real length


			//Log.Trace( "[SLM] Start Receive Content, Length[" + length + "], 直接Convert[" + Encoder.GetString( buffer_length ) + "], raw["+ buffer_length.ToHexString() +"]" );
			contentBuffer = new Byte[length];
			if ( SLMReceiveWithLogStep( ref contentBuffer, length, ref buffer_total ) != 0 ) return -1;


			//Log.Trace( "[SLM] Start Receive Tailer.." );
			//tailer
			if ( SLMReceiveWithLogStep( ref buffer_tailer, 2, ref buffer_total, BYTES_Tailer, "Tailer" ) != 0 ) return -1;


			//Log.Trace( "[SLM] Receive Done, Total[" + Encoder.GetString( buffer_total ) + "]" );
			return returnCode;
		}

		void ReceiveAllandLog( ref Byte[] buffer_Total )
		{
			while ( true )
			{
				var buffer = new Byte[1];
				if ( ReceiveSync( ref buffer, buffer.Length ) == 0 ) break; //預期接收1長度的byte

				buffer_Total = buffer_Total.BlockAppendBy( buffer );
			}

			Log.Debug( "[SLM] Receive all remnant data done, TotalData[" + Encoder.GetString( buffer_Total ) + "]" );
		}

		/// <summary>接收指定長度的資料至buffer, 並append到totalBuffer, 若compare條件有設定, 則進行比對</summary>
		/// <param name="currentBuffer">目前接收用的buffer</param>
		/// <param name="size">要接收的長度</param>
		/// <param name="totalBuffer">把接收到的資料Append進這個buffer</param>
		/// <param name="compareTargetBytes">要拿來比對接收資料的Bytes</param>
		/// <param name="compareFailMsg">當比對接收資料失敗時顯示的Message</param>
		/// <returns>回傳0表示正常, 小於零則是錯誤</returns>
		Int32 SLMReceiveWithLogStep( ref Byte[] currentBuffer, Int32 size, ref Byte[] totalBuffer, Byte[]? compareTargetBytes = null, String? compareFailMsg = null )
		{
			var receiveLength = ReceiveSync( ref currentBuffer, size );
			if ( receiveLength == 0 )
			{
				if ( size == 0 ) return 0; //如果預期接收的就是0的話, 就不報錯誤
				Log.Debug( "[SLM-Recv] SLM層接收到長度為0的資料" );
				return -888;
			}

			if ( totalBuffer == null )
				currentBuffer.BlockCloneTo( ref totalBuffer, 0, size );
			else
				totalBuffer = totalBuffer.BlockAppendBy( ref currentBuffer );

			if ( receiveLength > 0 )
			{
				if ( receiveLength != size )
				{
					//Log.LogFile( "[SLM-Recv] SLM預期接收長度[" + size + "], 實際接收長度[" + receiveLength + "], 進行遞迴取值" );
					var newSize = size - receiveLength;
					var newBuffer = new Byte[newSize];
					if ( SLMReceiveWithLogStep( ref newBuffer, newSize, ref totalBuffer, compareTargetBytes, compareFailMsg ) != 0 ) return -1; //遞迴取值

					Buffer.BlockCopy( newBuffer, 0, currentBuffer, receiveLength, newSize ); //copyback
				}
			}
			else
			{
				Log.Error( "[SLM-Recv] SLM層接收錯誤, Socket錯誤碼: " + receiveLength );
				return receiveLength; //負數
			}

			//compare
			if ( compareTargetBytes == null || compareTargetBytes.SequenceEqual( currentBuffer ) ) return 0;

			Log.Info( "[SLM-Recv] " + compareFailMsg.GetNullOr( msg => "SLM-" + msg ) + "比對失敗, 預期[ " + Encoder.GetString( compareTargetBytes ) + " ], 實際[ " + Encoder.GetString( currentBuffer ) + " ]" );

			//debug
			Log.Debug( "[SLM-Recv] 開始接收剩餘的資料..." );
			Log.Debug( "[SLM-Recv-Debug] 目前接收到的資料[" + Encoder.GetString( totalBuffer ) + "]" );
			Log.Debug( "[SLM-Recv-Debug] 目前接收到的資料[" + totalBuffer.ToHexString() + "]" );
			ReceiveAllandLog( ref totalBuffer );
			Log.Debug( "[SLM-Recv] 已接收全部的資料." );

			return -999; //return this bcoz slm error also it's negative number
		}


		/// <summary></summary>
		public Int32 TrySendTmp( ref Byte[] buffer )
		{
			var bytes = new List<Byte>();
			bytes.AddRange( BYTES_Header );
			bytes.AddRange( BYTES_CTRL_00 );
			bytes.Add( (Byte) ( buffer.Length / 256 ) );
			bytes.Add( (Byte) ( buffer.Length % 256 ) );
			bytes.AddRange( buffer );
			bytes.AddRange( BYTES_Tailer );

			var buffer_Send = bytes.ToArray();
			if ( SendSync( ref buffer_Send ) <= 0 ) return -1;

			return 0;
		}


		/// <summary>
		/// 傳送HeartBeat, 在沒有資料需傳送時, 每25秒一次
		/// <para>2015-07-13: 因為有發生HeartBeatTimeout狀態, Tony說改為10秒一次比較保險</para>
		/// </summary>
		public virtual Int32 SendSLM030()
		{
			var bytes = new List<Byte>();
			bytes.AddRange( BYTES_Header );
			bytes.AddRange( BYTES_CTRL_11 );
			bytes.AddRange( BYTES_CTRL_ZeroInt );
			bytes.AddRange( BYTES_Tailer );

			var buffer_Send = bytes.ToArray();
			if ( SendSync( ref buffer_Send ) <= 0 ) return -1;

			return 0;
		}


		public virtual Int32 SendSLM010()
		{
			var bytes = new List<Byte>();
			bytes.AddRange( BYTES_Header );
			bytes.AddRange( new Byte[] { 0x31, 0x30 } ); //10
			bytes.AddRange( BYTES_CTRL_ZeroInt );
			bytes.AddRange( BYTES_Tailer );

			var buffer_Send = bytes.ToArray();
			if ( SendSync( ref buffer_Send ) <= 0 ) return -1;

			return 0;
		}
	}
}
