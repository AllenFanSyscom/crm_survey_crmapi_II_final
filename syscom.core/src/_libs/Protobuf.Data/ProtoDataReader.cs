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
	/// A custom <see cref="System.Data.IDataReader"/> for de-serializing a protocol-buffer binary stream back
	/// into a tabular form.
	/// </summary>
	public sealed class ProtoDataReader : IDataReader
	{
		readonly List<ColReader> colReaders;

		Stream stream;
		Object[] currentRow;
		DataTable dataTable;
		Boolean disposed;
		ProtoReader reader;
		Int32 currentField;
		SubItemToken currentTableToken;
		Boolean reachedEndOfCurrentTable;

		/// <summary>
		/// Initializes a new instance of the <see cref="ProtoDataReader"/> class.
		/// </summary>
		/// <param name="stream">
		/// The <see cref="System.IO.Stream"/> to read from.
		/// </param>
		public ProtoDataReader( Stream stream )
		{
			if ( stream == null ) throw new ArgumentNullException( "stream" );

			this.stream = stream;
			reader = new ProtoReader( stream, null, null );
			colReaders = new List<ColReader>();

			AdvanceToNextField();
			if ( currentField != 1 ) throw new InvalidOperationException( "No results found! Invalid/corrupt stream." );

			ReadNextTableHeader();
		}

		~ProtoDataReader() { Dispose( false ); }

		delegate Object ColReader();

		public Int32 FieldCount
		{
			get
			{
				ErrorIfClosed();
				return dataTable.Columns.Count;
			}
		}

		public Int32 Depth
		{
			get
			{
				ErrorIfClosed();
				return 1;
			}
		}

		public Boolean IsClosed { get; private set; }

		/// <summary>
		/// Gets the number of rows changed, inserted, or deleted.
		/// </summary>
		/// <returns>This is always zero in the case of <see cref="ProtoBuf.Data.ProtoDataReader" />.</returns>
		public Int32 RecordsAffected => 0;

		Object IDataRecord.this[ Int32 i ]
		{
			get
			{
				ErrorIfClosed();
				return GetValue( i );
			}
		}

		Object IDataRecord.this[ String name ]
		{
			get
			{
				ErrorIfClosed();
				return GetValue( GetOrdinal( name ) );
			}
		}

#pragma warning disable 1591 // Missing XML comment

		public String GetName( Int32 i )
		{
			ErrorIfClosed();
			return dataTable.Columns[i].ColumnName;
		}

		public String GetDataTypeName( Int32 i )
		{
			ErrorIfClosed();
			return dataTable.Columns[i].DataType.Name;
		}

		public Type GetFieldType( Int32 i )
		{
			ErrorIfClosed();
			return dataTable.Columns[i].DataType;
		}

		public Object GetValue( Int32 i )
		{
			ErrorIfClosed();
			return currentRow[i];
		}

		public Int32 GetValues( Object[] values )
		{
			ErrorIfClosed();

			var length = Math.Min( values.Length, dataTable.Columns.Count );

			Array.Copy( currentRow, values, length );

			return length;
		}

		public Int32 GetOrdinal( String name )
		{
			ErrorIfClosed();
			return dataTable.Columns[name].Ordinal;
		}

		public Boolean GetBoolean( Int32 i )
		{
			ErrorIfClosed();
			return (Boolean) currentRow[i];
		}

		public Byte GetByte( Int32 i )
		{
			ErrorIfClosed();
			return (Byte) currentRow[i];
		}

		public Int64 GetBytes( Int32 i, Int64 fieldOffset, Byte[] buffer, Int32 bufferoffset, Int32 length )
		{
			ErrorIfClosed();
			var sourceBuffer = (Byte[]) currentRow[i];
			length = Math.Min( length, currentRow.Length - (Int32) fieldOffset );
			Array.Copy( sourceBuffer, fieldOffset, buffer, bufferoffset, length );
			return length;
		}

		public Char GetChar( Int32 i )
		{
			ErrorIfClosed();
			return (Char) currentRow[i];
		}

		public Int64 GetChars( Int32 i, Int64 fieldoffset, Char[] buffer, Int32 bufferoffset, Int32 length )
		{
			ErrorIfClosed();
			var sourceBuffer = (Char[]) currentRow[i];
			length = Math.Min( length, currentRow.Length - (Int32) fieldoffset );
			Array.Copy( sourceBuffer, fieldoffset, buffer, bufferoffset, length );
			return length;
		}

		public Guid GetGuid( Int32 i )
		{
			ErrorIfClosed();
			return (Guid) currentRow[i];
		}

		public Int16 GetInt16( Int32 i )
		{
			ErrorIfClosed();
			return (Int16) currentRow[i];
		}

		public Int32 GetInt32( Int32 i )
		{
			ErrorIfClosed();
			return (Int32) currentRow[i];
		}

		public Int64 GetInt64( Int32 i )
		{
			ErrorIfClosed();
			return (Int64) currentRow[i];
		}

		public Single GetFloat( Int32 i )
		{
			ErrorIfClosed();
			return (Single) currentRow[i];
		}

		public Double GetDouble( Int32 i )
		{
			ErrorIfClosed();
			return (Double) currentRow[i];
		}

		public String GetString( Int32 i )
		{
			ErrorIfClosed();
			return (String) currentRow[i];
		}

		public Decimal GetDecimal( Int32 i )
		{
			ErrorIfClosed();
			return (Decimal) currentRow[i];
		}

		public DateTime GetDateTime( Int32 i )
		{
			ErrorIfClosed();
			return (DateTime) currentRow[i];
		}

		public IDataReader GetData( Int32 i )
		{
			ErrorIfClosed();
			return ( (DataTable) currentRow[i] ).CreateDataReader();
		}

		public Boolean IsDBNull( Int32 i )
		{
			ErrorIfClosed();
			return currentRow[i] == null || currentRow[i] is DBNull;
		}

		public void Close()
		{
			stream.Close();
			IsClosed = true;
		}

		public Boolean NextResult()
		{
			ErrorIfClosed();

			ConsumeAnyRemainingRows();

			AdvanceToNextField();

			if ( currentField == 0 )
			{
				IsClosed = true;
				return false;
			}

			reachedEndOfCurrentTable = false;

			ReadNextTableHeader();

			return true;
		}

		public DataTable GetSchemaTable()
		{
			ErrorIfClosed();
			using ( var schemaReader = dataTable.CreateDataReader() )
			{
				return schemaReader.GetSchemaTable();
			}
		}

		public Boolean Read()
		{
			ErrorIfClosed();

			if ( reachedEndOfCurrentTable ) return false;

			if ( currentField == 0 )
			{
				ProtoReader.EndSubItem( currentTableToken, reader );
				reachedEndOfCurrentTable = true;
				return false;
			}

			ReadCurrentRow();
			AdvanceToNextField();

			return true;
		}

		public void Dispose()
		{
			Dispose( true );
			GC.SuppressFinalize( this );
		}

		void Dispose( Boolean disposing )
		{
			if ( !disposed )
			{
				if ( disposing )
				{
					if ( reader != null )
					{
						reader.Dispose();
						reader = null;
					}

					if ( stream != null )
					{
						stream.Dispose();
						stream = null;
					}

					if ( dataTable != null )
					{
						dataTable.Dispose();
						dataTable = null;
					}
				}

				disposed = true;
			}
		}

#pragma warning restore 1591 // Missing XML comment

		void ConsumeAnyRemainingRows()
		{
			// Unfortunately, protocol buffers doesn't let you seek - we have
			// to consume all the remaining tokens up anyway
			while ( Read() )
			{
			}
		}

		void ReadNextTableHeader()
		{
			ResetSchemaTable();
			currentRow = null;

			currentTableToken = ProtoReader.StartSubItem( reader );

			AdvanceToNextField();

			if ( currentField == 0 )
			{
				reachedEndOfCurrentTable = true;
				ProtoReader.EndSubItem( currentTableToken, reader );
				return;
			}

			if ( currentField != 2 ) throw new InvalidOperationException( "No header found! Invalid/corrupt stream." );

			ReadHeader();
		}

		void AdvanceToNextField() { currentField = reader.ReadFieldHeader(); }

		void ResetSchemaTable()
		{
			dataTable = new DataTable();
			colReaders.Clear();
		}

		void ReadHeader()
		{
			do
			{
				ReadColumn();
				AdvanceToNextField();
			}
			while ( currentField == 2 );
		}

		void ReadColumn()
		{
			var token = ProtoReader.StartSubItem( reader );
			Int32 field;
			String? name = null;
			var protoDataType = (ProtoDataType) ( -1 );
			while ( ( field = reader.ReadFieldHeader() ) != 0 )
				switch ( field )
				{
					case 1:
						name = reader.ReadString();
						break;
					case 2:
						protoDataType = (ProtoDataType) reader.ReadInt32();
						break;
					default:
						reader.SkipField();
						break;
				}

			switch ( protoDataType )
			{
				case ProtoDataType.Int:
					colReaders.Add( () => reader.ReadInt32() );
					break;
				case ProtoDataType.Short:
					colReaders.Add( () => reader.ReadInt16() );
					break;
				case ProtoDataType.Decimal:
					colReaders.Add( () => BclHelpers.ReadDecimal( reader ) );
					break;
				case ProtoDataType.String:
					colReaders.Add( () => reader.ReadString() );
					break;
				case ProtoDataType.Guid:
					colReaders.Add( () => BclHelpers.ReadGuid( reader ) );
					break;
				case ProtoDataType.DateTime:
					colReaders.Add( () => BclHelpers.ReadDateTime( reader ) );
					break;
				case ProtoDataType.Bool:
					colReaders.Add( () => reader.ReadBoolean() );
					break;

				case ProtoDataType.Byte:
					colReaders.Add( () => reader.ReadByte() );
					break;

				case ProtoDataType.Char:
					colReaders.Add( () => (Char) reader.ReadInt16() );
					break;

				case ProtoDataType.Double:
					colReaders.Add( () => reader.ReadDouble() );
					break;

				case ProtoDataType.Float:
					colReaders.Add( () => reader.ReadSingle() );
					break;

				case ProtoDataType.Long:
					colReaders.Add( () => reader.ReadInt64() );
					break;

				case ProtoDataType.ByteArray:
					colReaders.Add( () => ProtoReader.AppendBytes( null, reader ) );
					break;

				case ProtoDataType.CharArray:
					colReaders.Add( () => reader.ReadString().ToCharArray() );
					break;

				case ProtoDataType.TimeSpan:
					colReaders.Add( () => BclHelpers.ReadTimeSpan( reader ) );
					break;

				default:
					throw new NotSupportedException( protoDataType.ToString() );
			}

			ProtoReader.EndSubItem( token, reader );
			dataTable.Columns.Add( name, ConvertProtoDataType.ToClrType( protoDataType ) );
		}

		void ReadCurrentRow()
		{
			Int32 field;

			if ( currentRow == null )
				currentRow = new Object[colReaders.Count];
			else
				Array.Clear( currentRow, 0, currentRow.Length );

			var token = ProtoReader.StartSubItem( reader );
			while ( ( field = reader.ReadFieldHeader() ) != 0 )
				if ( field > currentRow.Length )
				{
					reader.SkipField();
				}
				else
				{
					var i = field - 1;
					currentRow[i] = colReaders[i]();
				}

			ProtoReader.EndSubItem( token, reader );
		}

		void ErrorIfClosed()
		{
			if ( IsClosed ) throw new InvalidOperationException( "Attempt to access ProtoDataReader which was already closed." );
		}
	}
}
