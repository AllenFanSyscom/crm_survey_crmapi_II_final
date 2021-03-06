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
	internal sealed class SurrogateSerializer : IProtoTypeSerializer
	{
		Boolean IProtoTypeSerializer.HasCallbacks( TypeModel.CallbackType callbackType ) { return false; }
#if FEAT_COMPILER
        void IProtoTypeSerializer.EmitCallback(Compiler.CompilerContext ctx, Compiler.Local valueFrom, ProtoBuf.Meta.TypeModel.CallbackType callbackType) { }
        void IProtoTypeSerializer.EmitCreateInstance(Compiler.CompilerContext ctx) { throw new NotSupportedException(); }
#endif
		Boolean IProtoTypeSerializer.CanCreateInstance() { return false; }
#if !FEAT_IKVM
		Object IProtoTypeSerializer.CreateInstance( ProtoReader source ) { throw new NotSupportedException(); }
		void IProtoTypeSerializer.Callback( Object value, TypeModel.CallbackType callbackType, SerializationContext context ) { }
#endif

		public Boolean ReturnsValue => false;
		public Boolean RequiresOldValue => true;
		public Type ExpectedType => forType;
		readonly Type forType, declaredType;
		readonly MethodInfo toTail, fromTail;
		IProtoTypeSerializer rootTail;

		public SurrogateSerializer( TypeModel model, Type forType, Type declaredType, IProtoTypeSerializer rootTail )
		{
			Helpers.DebugAssert( forType != null, "forType" );
			Helpers.DebugAssert( declaredType != null, "declaredType" );
			Helpers.DebugAssert( rootTail != null, "rootTail" );
			Helpers.DebugAssert( rootTail.RequiresOldValue, "RequiresOldValue" );
			Helpers.DebugAssert( !rootTail.ReturnsValue, "ReturnsValue" );
			Helpers.DebugAssert( declaredType == rootTail.ExpectedType || Helpers.IsSubclassOf( declaredType, rootTail.ExpectedType ) );
			this.forType = forType;
			this.declaredType = declaredType;
			this.rootTail = rootTail;
			toTail = GetConversion( model, true );
			fromTail = GetConversion( model, false );
		}

		static Boolean HasCast( TypeModel model, Type type, Type from, Type to, out MethodInfo op )
		{
#if WINRT
            System.Collections.Generic.List<MethodInfo> list = new System.Collections.Generic.List<MethodInfo>();
            foreach (var item in type.GetRuntimeMethods())
            {
                if (item.IsStatic) list.Add(item);
            }
            MethodInfo[] found = list.ToArray();
#else
			const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			var found = type.GetMethods( flags );
#endif
			ParameterInfo[] paramTypes;
			Type? convertAttributeType = null;
			for ( var i = 0; i < found.Length; i++ )
			{
				var m = found[i];
				if ( m.ReturnType != to ) continue;
				paramTypes = m.GetParameters();
				if ( paramTypes.Length == 1 && paramTypes[0].ParameterType == from )
				{
					if ( convertAttributeType == null )
					{
						convertAttributeType = model.MapType( typeof( ProtoConverterAttribute ), false );
						if ( convertAttributeType == null )
							// attribute isn't defined in the source assembly: stop looking
							break;
					}

					if ( m.IsDefined( convertAttributeType, true ) )
					{
						op = m;
						return true;
					}
				}
			}

			for ( var i = 0; i < found.Length; i++ )
			{
				var m = found[i];
				if ( m.Name != "op_Implicit" && m.Name != "op_Explicit" || m.ReturnType != to ) continue;
				paramTypes = m.GetParameters();
				if ( paramTypes.Length == 1 && paramTypes[0].ParameterType == from )
				{
					op = m;
					return true;
				}
			}

			op = null;
			return false;
		}

		public MethodInfo GetConversion( TypeModel model, Boolean toTail )
		{
			var to = toTail ? declaredType : forType;
			var from = toTail ? forType : declaredType;
			MethodInfo op;
			if ( HasCast( model, declaredType, from, to, out op ) || HasCast( model, forType, from, to, out op ) ) return op;
			throw new InvalidOperationException( "No suitable conversion operator found for surrogate: " +
			                                     forType.FullName + " / " + declaredType.FullName );
		}

#if !FEAT_IKVM
		public void Write( Object value, ProtoWriter writer ) { rootTail.Write( toTail.Invoke( null, new Object[] { value } ), writer ); }
		public Object Read( Object value, ProtoReader source )
		{
			// convert the incoming value
			Object[] args = { value };
			value = toTail.Invoke( null, args );

			// invoke the tail and convert the outgoing value
			args[0] = rootTail.Read( value, source );
			return fromTail.Invoke( null, args );
		}
#endif

#if FEAT_COMPILER
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Helpers.DebugAssert(valueFrom != null); // don't support stack-head for this
            using (Compiler.Local converted = new Compiler.Local(ctx, declaredType)) // declare/re-use local
            {
                ctx.LoadValue(valueFrom); // load primary onto stack
                ctx.EmitCall(toTail); // static convert op, primary-to-surrogate
                ctx.StoreValue(converted); // store into surrogate local

                rootTail.EmitRead(ctx, converted); // downstream processing against surrogate local

                ctx.LoadValue(converted); // load from surrogate local
                ctx.EmitCall(fromTail);  // static convert op, surrogate-to-primary
                ctx.StoreValue(valueFrom); // store back into primary
            }
        }

        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.LoadValue(valueFrom);
            ctx.EmitCall(toTail);
            rootTail.EmitWrite(ctx, null);
        }
#endif
	}
}
#endif
