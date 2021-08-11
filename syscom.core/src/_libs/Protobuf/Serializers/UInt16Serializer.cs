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
	internal class UInt16Serializer : IProtoSerializer
	{
#if FEAT_IKVM
        readonly Type expectedType;
#else
		static readonly Type expectedType = typeof( UInt16 );
#endif
		public UInt16Serializer( Meta.TypeModel model )
		{
#if FEAT_IKVM
            expectedType = model.MapType(typeof(ushort));
#endif
		}

		public virtual Type ExpectedType => expectedType;

		Boolean IProtoSerializer.RequiresOldValue => false;
		Boolean IProtoSerializer.ReturnsValue => true;
#if !FEAT_IKVM
		public virtual Object Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( value == null ); // since replaces
			return source.ReadUInt16();
		}

		public virtual void Write( Object value, ProtoWriter dest ) { ProtoWriter.WriteUInt16( (UInt16) value, dest ); }
#endif
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteUInt16", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadUInt16", ctx.MapType(typeof(ushort)));
        }
#endif
	}
}
#endif