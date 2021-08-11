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
	internal sealed class FieldDecorator : ProtoDecoratorBase
	{
		public override Type ExpectedType => forType;
		readonly FieldInfo field;
		readonly Type forType;
		public override Boolean RequiresOldValue => true;
		public override Boolean ReturnsValue => false;
		public FieldDecorator( Type forType, FieldInfo field, IProtoSerializer tail ) : base( tail )
		{
			Helpers.DebugAssert( forType != null );
			Helpers.DebugAssert( field != null );
			this.forType = forType;
			this.field = field;
		}
#if !FEAT_IKVM
		public override void Write( Object value, ProtoWriter dest )
		{
			Helpers.DebugAssert( value != null );
			value = field.GetValue( value );
			if ( value != null ) Tail.Write( value, dest );
		}

		public override Object Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( value != null );
			var newValue = Tail.Read( Tail.RequiresOldValue ? field.GetValue( value ) : null, source );
			if ( newValue != null ) field.SetValue( value, newValue );
			return null;
		}
#endif

#if FEAT_COMPILER
        protected override void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadAddress(valueFrom, ExpectedType);
            ctx.LoadValue(field);
            ctx.WriteNullCheckedTail(field.FieldType, Tail, null);
        }
        protected override void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            using (Compiler.Local loc = ctx.GetLocalWithValue(ExpectedType, valueFrom))
            {
                if (Tail.RequiresOldValue)
                {
                    ctx.LoadAddress(loc, ExpectedType);
                    ctx.LoadValue(field);  
                }
                // value is either now on the stack or not needed
                ctx.ReadNullCheckedTail(field.FieldType, Tail, null);

                if (Tail.ReturnsValue)
                {
                    using (Compiler.Local newVal = new Compiler.Local(ctx, field.FieldType))
                    {
                        ctx.StoreValue(newVal);
                        if (field.FieldType.IsValueType)
                        {
                            ctx.LoadAddress(loc, ExpectedType);
                            ctx.LoadValue(newVal);
                            ctx.StoreValue(field);
                        }
                        else
                        {
                            Compiler.CodeLabel allDone = ctx.DefineLabel();
                            ctx.LoadValue(newVal);
                            ctx.BranchIfFalse(allDone, true); // interpret null as "don't assign"

                            ctx.LoadAddress(loc, ExpectedType);
                            ctx.LoadValue(newVal);
                            ctx.StoreValue(field);

                            ctx.MarkLabel(allDone);
                        }
                    }
                }
            }
        }
#endif
	}
}
#endif