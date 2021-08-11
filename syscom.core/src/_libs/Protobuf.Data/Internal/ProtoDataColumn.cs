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

using System;

namespace libs.ProtoBuf.Data.Internal
{
	internal struct ProtoDataColumn
	{
		public ProtoDataType ProtoDataType;
		public String ColumnName;
		public Int32 ColumnIndex;

		public override String ToString() { return $"{ColumnName} ({ProtoDataType})"; }
	}
}