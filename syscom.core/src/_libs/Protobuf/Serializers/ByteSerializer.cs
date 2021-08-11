#if !NO_RUNTIME
using System;

#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
#endif


namespace libs.ProtoBuf.Serializers
{
	internal sealed class ByteSerializer : IProtoSerializer
	{
		public Type ExpectedType => expectedType;

#if FEAT_IKVM
        readonly Type expectedType;
#else
		static readonly Type expectedType = typeof( Byte );
#endif
		public ByteSerializer( Meta.TypeModel model )
		{
#if FEAT_IKVM
            expectedType = model.MapType(typeof(byte));
#endif
		}

		Boolean IProtoSerializer.RequiresOldValue => false;
		Boolean IProtoSerializer.ReturnsValue => true;
#if !FEAT_IKVM
		public void Write( Object value, ProtoWriter dest ) { ProtoWriter.WriteByte( (Byte) value, dest ); }
		public Object Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( value == null ); // since replaces
			return source.ReadByte();
		}
#endif

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteByte", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadByte", ExpectedType);
        }
#endif
	}
}
#endif