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
	internal sealed class NetObjectSerializer : IProtoSerializer
	{
		readonly Int32 key;
		readonly Type type;

		readonly BclHelpers.NetObjectOptions options;

		public NetObjectSerializer( TypeModel model, Type type, Int32 key, BclHelpers.NetObjectOptions options )
		{
			var dynamicType = ( options & BclHelpers.NetObjectOptions.DynamicType ) != 0;
			this.key = dynamicType ? -1 : key;
			this.type = dynamicType ? model.MapType( typeof( Object ) ) : type;
			this.options = options;
		}

		public Type ExpectedType => type;

		public Boolean ReturnsValue => true;
		public Boolean RequiresOldValue => true;
#if !FEAT_IKVM
		public Object Read( Object value, ProtoReader source ) { return BclHelpers.ReadNetObject( value, source, key, type == typeof( Object ) ? null : type, options ); }
		public void Write( Object value, ProtoWriter dest ) { BclHelpers.WriteNetObject( value, dest, key, options ); }
#endif

#if FEAT_COMPILER
        public void EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.CastToObject(type);
            ctx.LoadReaderWriter();
            ctx.LoadValue(ctx.MapMetaKeyToCompiledKey(key));
            if (type ==  ctx.MapType(typeof(object))) ctx.LoadNullRef();
            else ctx.LoadValue(type);
            ctx.LoadValue((int)options);
            ctx.EmitCall(ctx.MapType(typeof(BclHelpers)).GetMethod("ReadNetObject"));
            ctx.CastFromObject(type);
        }
        public void EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.CastToObject(type);
            ctx.LoadReaderWriter();
            ctx.LoadValue(ctx.MapMetaKeyToCompiledKey(key));
            ctx.LoadValue((int)options);
            ctx.EmitCall(ctx.MapType(typeof(BclHelpers)).GetMethod("WriteNetObject"));
        }
#endif
	}
}
#endif