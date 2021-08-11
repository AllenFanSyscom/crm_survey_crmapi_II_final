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

namespace libs.ProtoBuf.Data.Internal
{
	using System;
	using System.Collections.Generic;
	using System.Data;

	internal sealed class ProtoDataColumnFactory
	{
		static readonly Boolean IsRunningOnMono;

		static ProtoDataColumnFactory()
		{
			// From http://stackoverflow.com/a/721194/91551
			IsRunningOnMono = Type.GetType( "Mono.Runtime" ) != null;
		}

		public IList<ProtoDataColumn> GetColumns
		(
			IDataReader reader,
			ProtoDataWriterOptions options
		)
		{
			if ( reader == null ) throw new ArgumentNullException( "reader" );

			if ( options == null ) throw new ArgumentNullException( "options" );

			using ( var schema = reader.GetSchemaTable() )
			{
				var schemaSupportsExpressions = schema.Columns.Contains( "Expression" );

				var columns = new List<ProtoDataColumn>( schema.Rows.Count );
				for ( var i = 0; i < schema.Rows.Count; i++ )
				{
					// Assumption: rows in the schema table are always ordered by
					// Ordinal position, ascending
					var row = schema.Rows[i];

					// Skip computed columns unless requested.
					if ( schemaSupportsExpressions )
					{
						Boolean isComputedColumn;

						if ( IsRunningOnMono )
							isComputedColumn = Equals( row["Expression"], String.Empty );
						else
							isComputedColumn = !( row["Expression"] is DBNull );

						if ( isComputedColumn && !options.IncludeComputedColumns ) continue;
					}

					var col = new ProtoDataColumn
					{
						ColumnIndex = i,
						ProtoDataType = ConvertProtoDataType.FromClrType( (Type) row["DataType"] ),
						ColumnName = (String) row["ColumnName"]
					};

					columns.Add( col );
				}

				return columns;
			}
		}
	}
}