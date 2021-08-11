#if !NO_RUNTIME
using System;
using libs.ProtoBuf.Meta;

namespace libs.ProtoBuf.Serializers
{
	internal interface IProtoTypeSerializer : IProtoSerializer
	{
		Boolean HasCallbacks( TypeModel.CallbackType callbackType );
		Boolean CanCreateInstance();
#if !FEAT_IKVM
		Object CreateInstance( ProtoReader source );
		void Callback( Object value, TypeModel.CallbackType callbackType, SerializationContext context );
#endif
#if FEAT_COMPILER
        void EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType);
#endif
#if FEAT_COMPILER
        void EmitCreateInstance(Compiler.CompilerContext ctx);
#endif
	}
}
#endif