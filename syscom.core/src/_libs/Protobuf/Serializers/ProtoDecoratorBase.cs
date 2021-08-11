#if !NO_RUNTIME
using System;
using libs.ProtoBuf.Meta;
#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;

#endif

namespace libs.ProtoBuf.Serializers
{
	internal abstract class ProtoDecoratorBase : IProtoSerializer
	{
		public abstract Type ExpectedType { get; }
		protected readonly IProtoSerializer Tail;
		protected ProtoDecoratorBase( IProtoSerializer tail ) { Tail = tail; }
		public abstract Boolean ReturnsValue { get; }
		public abstract Boolean RequiresOldValue { get; }
#if !FEAT_IKVM
		public abstract void Write( Object value, ProtoWriter dest );
		public abstract Object Read( Object value, ProtoReader source );
#endif

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom) { EmitWrite(ctx, valueFrom); }
        protected abstract void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom);
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom) { EmitRead(ctx, valueFrom); }
        protected abstract void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom);
#endif
	}
}
#endif