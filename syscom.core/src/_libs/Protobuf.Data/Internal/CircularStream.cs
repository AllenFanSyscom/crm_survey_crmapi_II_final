// Copyright (c) 2012, Alex Regueiro http://circularbuffer.codeplex.com
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without 
// modification, are permitted provided that the following conditions are met:
//
// * Redistributions of source code must retain the above copyright notice, 
//   this list of conditions and the following disclaimer.
//
// * Redistributions in binary form must reproduce the above copyright notice,
//   this list of conditions and the following disclaimer in the documentation
//   and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
// AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
// ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
// LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR 
// CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF 
// SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS 
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
// CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
// ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

namespace libs.ProtoBuf.Data.Internal
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using System.IO;

	[ExcludeFromCodeCoverage]
	[SuppressMessage( "StyleCop.CSharp.*", "*", Justification = "Third party" )]
	internal class CircularStream : Stream
	{
		CircularBuffer<Byte> buffer;

		public CircularStream( Int32 bufferCapacity )
			: base()
		{
			buffer = new CircularBuffer<Byte>( bufferCapacity );
		}

		public override Int64 Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

		public Int32 Capacity { get => buffer.Capacity; set => buffer.Capacity = value; }

		public override Int64 Length => buffer.Size;

		public override Boolean CanSeek => true;

		public override Boolean CanRead => true;

		public override Boolean CanWrite => true;

		public Byte[] GetBuffer() { return buffer.GetBuffer(); }

		public Byte[] ToArray() { return buffer.ToArray(); }

		public override void Flush() { }

		public override void Write( Byte[] buffer, Int32 offset, Int32 count ) { this.buffer.Put( buffer, offset, count ); }

		public override void WriteByte( Byte value ) { buffer.Put( value ); }

		public override Int32 Read( Byte[] buffer, Int32 offset, Int32 count ) { return this.buffer.Get( buffer, offset, count ); }

		public override Int32 ReadByte() { return buffer.Get(); }

		public override Int64 Seek( Int64 offset, SeekOrigin origin ) { throw new NotSupportedException(); }

		public override void SetLength( Int64 value ) { throw new NotSupportedException(); }
	}
}