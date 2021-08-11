using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
	public static class StreamExtensions
	{
		public static Byte[] ReadToEnd( this Stream stream, Int32 readBufferLength = 8192 )
		{
			Int64 originalPosition = 0;

			if ( stream.CanSeek )
			{
				originalPosition = stream.Position;
				stream.Position = 0;
			}

			try
			{
				var readBuffer = new Byte[readBufferLength];

				var totalBytesRead = 0;
				var BytesRead = 0;

				while ( ( BytesRead = stream.Read( readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead ) ) > 0 )
				{
					totalBytesRead += BytesRead;

					if ( totalBytesRead == readBuffer.Length )
					{
						var nextByte = stream.ReadByte();
						if ( nextByte == -1 ) continue;
						var temp = new Byte[readBuffer.Length * 2];
						Buffer.BlockCopy( readBuffer, 0, temp, 0, readBuffer.Length );
						Buffer.SetByte( temp, totalBytesRead, (Byte) nextByte );
						readBuffer = temp;
						totalBytesRead++;
					}
				}

				var buffer = readBuffer;
				if ( readBuffer.Length == totalBytesRead ) return buffer;

				buffer = new Byte[totalBytesRead];
				Buffer.BlockCopy( readBuffer, 0, buffer, 0, totalBytesRead );
				return buffer;
			}
			finally
			{
				if ( stream.CanSeek ) stream.Position = originalPosition;
			}
		}
	}
}