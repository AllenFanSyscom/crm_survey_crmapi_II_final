// Copyright 2012 Richard Dingwall - http://richarddingwall.name
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

namespace libs.ProtoBuf.Data
{
	using System;
	using System.Collections.Generic;
	using System.Data;
	using System.IO;
	using Internal;

	/// <summary>
	/// Serializes an <see cref="System.Data.IDataReader"/> to a binary stream
	/// which can be read (it serializes additional rows with subsequent calls
	/// to <see cref="Read"/>). Useful for scenarios like WCF where you cannot
	/// write to the output stream directly.
	/// </summary>
	/// <remarks>Not guaranteed to be thread safe.</remarks>
	public class ProtoDataStream : Stream
	{
		/// <summary>
		/// Buffer size.
		/// </summary>
		public const Int32 DefaultBufferSize = 128 * 1024;

		readonly ProtoDataWriterOptions options;
		readonly ProtoDataColumnFactory columnFactory;

		IDataReader reader;
		ProtoWriter writer;
		Stream bufferStream;
		Boolean disposed;
		Int32 resultIndex;
		Boolean isHeaderWritten;
		RowWriter rowWriter;
		SubItemToken currentResultToken;
		Boolean readerIsClosed;

		/// <summary>
		/// Initializes a new instance of the <see cref="ProtoDataStream"/> class.
		/// </summary>
		/// <param name="dataSet">The <see cref="DataSet"/>who's contents to serialize.</param>
		/// <param name="bufferSize">Buffer size to use when serializing rows. 
		/// You should not need to change this unless you have exceptionally
		/// large rows or an exceptionally high number of columns.</param>
		public ProtoDataStream
		(
			DataSet dataSet, Int32 bufferSize = DefaultBufferSize
		)
			: this( dataSet.CreateDataReader(), new ProtoDataWriterOptions(), bufferSize )
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProtoDataStream"/> class.
		/// </summary>
		/// <param name="dataSet">The <see cref="DataSet"/>who's contents to serialize.</param>
		/// <param name="options"><see cref="ProtoDataWriterOptions"/> specifying any custom serialization options.</param>
		/// <param name="bufferSize">Buffer size to use when serializing rows. 
		/// You should not need to change this unless you have exceptionally
		/// large rows or an exceptionally high number of columns.</param>
		public ProtoDataStream
		(
			DataSet dataSet,
			ProtoDataWriterOptions options,
			Int32 bufferSize = DefaultBufferSize
		)
			: this( dataSet.CreateDataReader(), options, bufferSize )
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProtoDataStream"/> class.
		/// </summary>
		/// <param name="dataTable">The <see cref="DataTable"/>who's contents to serialize.</param>
		/// <param name="bufferSize">Buffer size to use when serializing rows. 
		/// You should not need to change this unless you have exceptionally
		/// large rows or an exceptionally high number of columns.</param>
		public ProtoDataStream
		(
			DataTable dataTable, Int32 bufferSize = DefaultBufferSize
		)
			: this( dataTable.CreateDataReader(), new ProtoDataWriterOptions(), bufferSize )
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProtoDataStream"/> class.
		/// </summary>
		/// <param name="dataTable">The <see cref="DataTable"/>who's contents to serialize.</param>
		/// <param name="options"><see cref="ProtoDataWriterOptions"/> specifying any custom serialization options.</param>
		/// <param name="bufferSize">Buffer size to use when serializing rows. 
		/// You should not need to change this unless you have exceptionally
		/// large rows or an exceptionally high number of columns.</param>
		public ProtoDataStream
		(
			DataTable dataTable,
			ProtoDataWriterOptions options,
			Int32 bufferSize = DefaultBufferSize
		)
			: this( dataTable.CreateDataReader(), options, bufferSize )
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProtoDataStream"/> class.
		/// </summary>
		/// <param name="reader">The <see cref="IDataReader"/>who's contents to serialize.</param>
		/// <param name="bufferSize">Buffer size to use when serializing rows. 
		/// You should not need to change this unless you have exceptionally
		/// large rows or an exceptionally high number of columns.</param>
		public ProtoDataStream( IDataReader reader, Int32 bufferSize = DefaultBufferSize )
			: this( reader, new ProtoDataWriterOptions(), bufferSize )
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ProtoDataStream"/> class.
		/// </summary>
		/// <param name="reader">The <see cref="IDataReader"/>who's contents to serialize.</param>
		/// <param name="options"><see cref="ProtoDataWriterOptions"/> specifying any custom serialization options.</param>
		/// <param name="bufferSize">Buffer size to use when serializing rows. 
		/// You should not need to change this unless you have exceptionally
		/// large rows or an exceptionally high number of columns.</param>
		public ProtoDataStream
		(
			IDataReader reader,
			ProtoDataWriterOptions options,
			Int32 bufferSize = DefaultBufferSize
		)
		{
			if ( reader == null ) throw new ArgumentNullException( "reader" );

			if ( options == null ) throw new ArgumentNullException( "options" );

			this.reader = reader;
			this.options = options;

			resultIndex = 0;
			columnFactory = new ProtoDataColumnFactory();
			bufferStream = new CircularStream( bufferSize );
			writer = new ProtoWriter( bufferStream, null, null );
		}

		~ProtoDataStream() { Dispose( false ); }

		public override Boolean CanRead => true;

		public override Boolean CanSeek => false;

		public override Boolean CanWrite => false;

		public override Int64 Length => -1;

		public override Int64 Position { get => bufferStream.Position; set => throw new InvalidOperationException( "Cannot set stream position." ); }

		public override void Flush() { }

		public override Int64 Seek( Int64 offset, SeekOrigin origin ) { throw new InvalidOperationException( "This stream cannot seek." ); }

		public override void SetLength( Int64 value ) { throw new InvalidOperationException(); }

		public override Int32 Read( Byte[] buffer, Int32 offset, Int32 count )
		{
			if ( bufferStream.Length == 0 && readerIsClosed ) return 0;

			if ( !readerIsClosed ) FillBuffer( count );

			return bufferStream.Read( buffer, offset, count );
		}

		public override void Write( Byte[] buffer, Int32 offset, Int32 count ) { throw new InvalidOperationException( "This is a stream for reading serialized bytes. Writing is not supported." ); }

		public override void Close()
		{
			readerIsClosed = true;
			reader?.Close();
		}

		protected override void Dispose( Boolean disposing )
		{
			if ( !disposed )
			{
				if ( disposing )
				{
					if ( writer != null )
					{
						( (IDisposable) writer ).Dispose();
						writer = null;
					}

					if ( reader != null )
					{
						reader.Dispose();
						reader = null;
					}

					if ( bufferStream != null )
					{
						bufferStream.Dispose();
						bufferStream = null;
					}
				}

				disposed = true;
			}
		}

		void WriteHeaderIfRequired()
		{
			if ( isHeaderWritten ) return;

			ProtoWriter.WriteFieldHeader( 1, WireType.StartGroup, writer );

			currentResultToken = ProtoWriter.StartSubItem( resultIndex, writer );

			var columns = columnFactory.GetColumns( reader, options );
			new HeaderWriter( writer ).WriteHeader( columns );

			rowWriter = new RowWriter( writer, columns, options );

			isHeaderWritten = true;
		}

		void FillBuffer( Int32 requestedLength )
		{
			// Only supports 1 data table currently.
			WriteHeaderIfRequired();

			// write the rows
			while ( bufferStream.Length < requestedLength )
				// NB protobuf-net only flushes every 1024 bytes. So
				// it might take a few iterations for bufferStream.Length to
				// see any change.
				if ( reader.Read() )
				{
					rowWriter.WriteRow( reader );
				}
				else
				{
					resultIndex++;
					ProtoWriter.EndSubItem( currentResultToken, writer );

					if ( reader.NextResult() )
					{
						// Start next data table.
						isHeaderWritten = false;
						FillBuffer( requestedLength );
					}
					else
					{
						// All done, no more results.
						writer.Close();
						Close();
					}

					break;
				}
		}
	}
}