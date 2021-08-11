using System;
#if !NO_RUNTIME
#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;

#endif

namespace libs.ProtoBuf.Serializers
{
	internal sealed class SystemTypeSerializer : IProtoSerializer
	{
#if FEAT_IKVM
        readonly Type expectedType;
#else
		static readonly Type expectedType = typeof( Type );
#endif
		public SystemTypeSerializer( Meta.TypeModel model )
		{
#if FEAT_IKVM
            expectedType = model.MapType(typeof(System.Type));
#endif
		}

		public Type ExpectedType => expectedType;

#if !FEAT_IKVM
		void IProtoSerializer.Write( Object value, ProtoWriter dest ) { ProtoWriter.WriteType( (Type) value, dest ); }

		Object IProtoSerializer.Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( value == null ); // since replaces
			return source.ReadType();
		}
#endif
		Boolean IProtoSerializer.RequiresOldValue => false;
		Boolean IProtoSerializer.ReturnsValue => true;

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteType", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadType", ExpectedType);
        }
#endif
	}
}

#endif