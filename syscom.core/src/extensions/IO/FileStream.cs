using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.IO
{
	public static class FileStreamExtensions
	{
		/// <summary>
		/// 取得從檔案後方算回來的單行資料 (傳回來的資料將不包括換行符號)
		/// <para>(注意: 若該行資料為空行, 則傳回0長度字串, 若找不到指定行則傳回null)</para>
		/// </summary>
		/// <param name="fs">檔案的Stream</param>
		/// <param name="offset">offset由0開始, 代表第一行</param>
		/// <param name="newLine">NewLine字元(預設\n)</param>
		/// <param name="encoder">使用的Enoding, 若未指定或為null則使用Ascii</param>
		/// <returns>若取得資料將回傳字串, 否則回傳null</returns>
		public static String GetLastLine( this FileStream fs, Int32 offset = 0, String newLine = "\n", Encoding? encoder = null )
		{
			if ( encoder == null ) encoder = Encoding.ASCII;
			var charsize = encoder.GetByteCount( newLine );
			var buffer = encoder.GetBytes( newLine );

			var stream = fs;
			var endpos = stream.Length / charsize;
			var idx = 0;
			var lastFindNewLinePos = stream.Length;

			for ( Int64 pos = charsize; pos <= endpos; pos += charsize )
			{
				stream.Seek( -pos, SeekOrigin.End );
				stream.Read( buffer, 0, buffer.Length );

				var bufferSize = 0L;
				if ( encoder.GetString( buffer ) == newLine )
				{
					if ( offset == idx )
					{
						bufferSize = lastFindNewLinePos - stream.Position - charsize;
						if ( bufferSize < 0 ) bufferSize = 0;
						buffer = new Byte[bufferSize];
						stream.Read( buffer, 0, buffer.Length );
						return encoder.GetString( buffer );
					}

					idx++;
					lastFindNewLinePos = stream.Position;
				}

				if ( pos != endpos || idx != offset ) continue;
				bufferSize = lastFindNewLinePos - stream.Position - charsize;
				if ( bufferSize < 0 ) bufferSize = 0;
				buffer = new Byte[bufferSize];
				stream.Read( buffer, 0, buffer.Length );
				return encoder.GetString( buffer );
			}

			return null;
		}
	}
}
