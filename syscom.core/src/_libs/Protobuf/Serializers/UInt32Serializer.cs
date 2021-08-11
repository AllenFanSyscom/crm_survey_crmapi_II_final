#if !NO_RUNTIME
using System;
#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;

#endif


namespace libs.ProtoBuf.Serializers
{
	internal sealed class UInt32Serializer : IProtoSerializer
	{
#if FEAT_IKVM
        readonly Type expectedType;
#else
		static readonly Type expectedType = typeof( UInt32 );
#endif
		public UInt32Serializer( Meta.TypeModel model )
		{
#if FEAT_IKVM
            expectedType = model.MapType(typeof(uint));
#endif
		}

		public Type ExpectedType => expectedType;

		Boolean IProtoSerializer.RequiresOldValue => false;
		Boolean IProtoSerializer.ReturnsValue => true;
#if !FEAT_IKVM
		public Object Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( value == null ); // since replaces
			return source.ReadUInt32();
		}

		public void Write( Object value, ProtoWriter dest ) { ProtoWriter.WriteUInt32( (UInt32) value, dest ); }
#endif
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteUInt32", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadUInt32", ctx.MapType(typeof(uint)));
        }
#endif
	}
}
#endif