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
	internal sealed class BooleanSerializer : IProtoSerializer
	{
#if FEAT_IKVM
        readonly Type expectedType;
#else
		static readonly Type expectedType = typeof( Boolean );
#endif
		public BooleanSerializer( Meta.TypeModel model )
		{
#if FEAT_IKVM
            expectedType = model.MapType(typeof(bool));
#endif
		}

		public Type ExpectedType => expectedType;

#if !FEAT_IKVM
		public void Write( Object value, ProtoWriter dest ) { ProtoWriter.WriteBoolean( (Boolean) value, dest ); }
		public Object Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( value == null ); // since replaces
			return source.ReadBoolean();
		}
#endif
		Boolean IProtoSerializer.RequiresOldValue => false;
		Boolean IProtoSerializer.ReturnsValue => true;
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteBoolean", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadBoolean", ExpectedType);
        }
#endif
	}
}
#endif