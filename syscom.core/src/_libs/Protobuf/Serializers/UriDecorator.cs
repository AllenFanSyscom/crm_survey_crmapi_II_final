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
	internal sealed class UriDecorator : ProtoDecoratorBase
	{
#if FEAT_IKVM
        readonly Type expectedType;
#else
		static readonly Type expectedType = typeof( Uri );
#endif
		public UriDecorator( Meta.TypeModel model, IProtoSerializer tail ) : base( tail )
		{
#if FEAT_IKVM
            expectedType = model.MapType(typeof(Uri));
#endif
		}

		public override Type ExpectedType => expectedType;
		public override Boolean RequiresOldValue => false;
		public override Boolean ReturnsValue => true;


#if !FEAT_IKVM
		public override void Write( Object value, ProtoWriter dest ) { Tail.Write( ( (Uri) value ).AbsoluteUri, dest ); }
		public override Object Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( value == null ); // not expecting incoming
			var s = (String) Tail.Read( null, source );
			return s.Length == 0 ? null : new Uri( s );
		}
#endif

#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.LoadValue(typeof(Uri).GetProperty("AbsoluteUri"));
            Tail.EmitWrite(ctx, null);
        }
        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Tail.EmitRead(ctx, valueFrom);
            ctx.CopyValue();
            Compiler.CodeLabel @nonEmpty = ctx.DefineLabel(), @end = ctx.DefineLabel();
            ctx.LoadValue(typeof(string).GetProperty("Length"));
            ctx.BranchIfTrue(@nonEmpty, true);
            ctx.DiscardValue();
            ctx.LoadNullRef();
            ctx.Branch(@end, true);
            ctx.MarkLabel(@nonEmpty);
            ctx.EmitCtor(ctx.MapType(typeof(Uri)), ctx.MapType(typeof(string)));
            ctx.MarkLabel(@end);
            
        }
#endif
	}
}
#endif