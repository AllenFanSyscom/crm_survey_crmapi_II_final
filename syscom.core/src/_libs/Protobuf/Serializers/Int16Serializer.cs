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
	internal sealed class Int16Serializer : IProtoSerializer
	{
#if FEAT_IKVM
        readonly Type expectedType;
#else
		static readonly Type expectedType = typeof( Int16 );
#endif
		public Int16Serializer( Meta.TypeModel model )
		{
#if FEAT_IKVM
            expectedType = model.MapType(typeof(short));
#endif
		}

		public Type ExpectedType => expectedType;

		Boolean IProtoSerializer.RequiresOldValue => false;
		Boolean IProtoSerializer.ReturnsValue => true;
#if !FEAT_IKVM
		public Object Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( value == null ); // since replaces
			return source.ReadInt16();
		}

		public void Write( Object value, ProtoWriter dest ) { ProtoWriter.WriteInt16( (Int16) value, dest ); }
#endif
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteInt16", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadInt16", ExpectedType);
        }
#endif
	}
}
#endif