#if !NO_RUNTIME
using System;

#if FEAT_COMPILER
using System.Reflection.Emit;
#endif

#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
#endif


namespace libs.ProtoBuf.Serializers
{
	internal sealed class BlobSerializer : IProtoSerializer
	{
		public Type ExpectedType => expectedType;

#if FEAT_IKVM
        readonly Type expectedType;
#else
		static readonly Type expectedType = typeof( Byte[] );
#endif
		public BlobSerializer( Meta.TypeModel model, Boolean overwriteList )
		{
#if FEAT_IKVM
            expectedType = model.MapType(typeof(byte[]));
#endif
			this.overwriteList = overwriteList;
		}

		readonly Boolean overwriteList;
#if !FEAT_IKVM
		public Object Read( Object value, ProtoReader source ) { return ProtoReader.AppendBytes( overwriteList ? null : (Byte[]) value, source ); }
		public void Write( Object value, ProtoWriter dest ) { ProtoWriter.WriteBytes( (Byte[]) value, dest ); }
#endif
		Boolean IProtoSerializer.RequiresOldValue => !overwriteList;
		Boolean IProtoSerializer.ReturnsValue => true;
#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicWrite("WriteBytes", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            if (overwriteList)
            {
                ctx.LoadNullRef();
            }
            else
            {
                ctx.LoadValue(valueFrom);
            }
            ctx.LoadReaderWriter();
            ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod("AppendBytes"));
        }
#endif
	}
}
#endif