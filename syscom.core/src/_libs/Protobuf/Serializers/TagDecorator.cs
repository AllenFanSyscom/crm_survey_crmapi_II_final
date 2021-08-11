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
	internal sealed class TagDecorator : ProtoDecoratorBase, IProtoTypeSerializer
	{
		public Boolean HasCallbacks( TypeModel.CallbackType callbackType )
		{
			var pts = Tail as IProtoTypeSerializer;
			return pts != null && pts.HasCallbacks( callbackType );
		}

		public Boolean CanCreateInstance()
		{
			var pts = Tail as IProtoTypeSerializer;
			return pts != null && pts.CanCreateInstance();
		}
#if !FEAT_IKVM
		public Object CreateInstance( ProtoReader source ) { return ( (IProtoTypeSerializer) Tail ).CreateInstance( source ); }
		public void Callback( Object value, TypeModel.CallbackType callbackType, SerializationContext context )
		{
			var pts = Tail as IProtoTypeSerializer;
			pts?.Callback( value, callbackType, context );
		}
#endif
#if FEAT_COMPILER
        public void EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, TypeModel.CallbackType callbackType)
        {
            // we only expect this to be invoked if HasCallbacks returned true, so implicitly Tail
            // **must** be of the correct type
            ((IProtoTypeSerializer)Tail).EmitCallback(ctx, valueFrom, callbackType);
        }
        public void EmitCreateInstance(Compiler.CompilerContext ctx)
        {
            ((IProtoTypeSerializer)Tail).EmitCreateInstance(ctx);
        }
#endif
		public override Type ExpectedType => Tail.ExpectedType;

		public TagDecorator( Int32 fieldNumber, WireType wireType, Boolean strict, IProtoSerializer tail )
			: base( tail )
		{
			this.fieldNumber = fieldNumber;
			this.wireType = wireType;
			this.strict = strict;
		}

		public override Boolean RequiresOldValue => Tail.RequiresOldValue;
		public override Boolean ReturnsValue => Tail.ReturnsValue;
		readonly Boolean strict;
		readonly Int32 fieldNumber;
		readonly WireType wireType;

		Boolean NeedsHint => ( (Int32) wireType & ~7 ) != 0;
#if !FEAT_IKVM
		public override Object Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( fieldNumber == source.FieldNumber );
			if ( strict )
				source.Assert( wireType );
			else if ( NeedsHint ) source.Hint( wireType );
			return Tail.Read( value, source );
		}

		public override void Write( Object value, ProtoWriter dest )
		{
			ProtoWriter.WriteFieldHeader( fieldNumber, wireType, dest );
			Tail.Write( value, dest );
		}
#endif

#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue((int)fieldNumber);
            ctx.LoadValue((int)wireType);
            ctx.LoadReaderWriter();
            ctx.EmitCall(ctx.MapType(typeof(ProtoWriter)).GetMethod("WriteFieldHeader"));
            Tail.EmitWrite(ctx, valueFrom);    
        }
        protected override void EmitRead(ProtoBuf.Compiler.CompilerContext ctx, ProtoBuf.Compiler.Local valueFrom)
        {
            if (strict || NeedsHint)
            {
                ctx.LoadReaderWriter();
                ctx.LoadValue((int)wireType);
                ctx.EmitCall(ctx.MapType(typeof(ProtoReader)).GetMethod(strict ? "Assert" : "Hint"));
            }
            Tail.EmitRead(ctx, valueFrom);
        }
#endif
	}
}
#endif