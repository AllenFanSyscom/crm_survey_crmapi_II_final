#if !NO_RUNTIME
using System;
using System.Net;
using libs.ProtoBuf.Meta;
#if FEAT_IKVM
using Type = IKVM.Reflection.Type;
using IKVM.Reflection;
#else
using System.Reflection;

#endif

namespace libs.ProtoBuf.Serializers
{
	internal sealed class ParseableSerializer : IProtoSerializer
	{
		readonly MethodInfo parse;

		public static ParseableSerializer TryCreate( Type type, TypeModel model )
		{
			if ( type == null ) throw new ArgumentNullException( "type" );
#if WINRT || PORTABLE
            MethodInfo? method = null;

#if WINRT
            foreach (MethodInfo tmp in type.GetTypeInfo().GetDeclaredMethods("Parse"))
#else
            foreach (MethodInfo tmp in type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
#endif
            {
                ParameterInfo[] p;
                if (tmp.Name == "Parse" && tmp.IsPublic && tmp.IsStatic && tmp.DeclaringType == type && (p = tmp.GetParameters()) != null && p.Length == 1 && p[0].ParameterType == typeof(string))
                {
                    method = tmp;
                    break;
                }
            }
#else
			var method = type.GetMethod( "Parse",
			                             BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
			                             null, new Type[] { model.MapType( typeof( String ) ) }, null );
#endif
			if ( method != null && method.ReturnType == type )
			{
				if ( Helpers.IsValueType( type ) )
				{
					var toString = GetCustomToString( type );
					if ( toString == null || toString.ReturnType != model.MapType( typeof( String ) ) ) return null; // need custom ToString, fools
				}

				return new ParseableSerializer( method );
			}

			return null;
		}

		static MethodInfo GetCustomToString( Type type )
		{
#if WINRT
            foreach (MethodInfo method in type.GetTypeInfo().GetDeclaredMethods("ToString"))
            {
                if (method.IsPublic && !method.IsStatic && method.GetParameters().Length == 0) return method;
            }
            return null;
#elif PORTABLE
            MethodInfo method = Helpers.GetInstanceMethod(type, "ToString", Helpers.EmptyTypes);
            if (method == null || !method.IsPublic || method.IsStatic || method.DeclaringType != type) return null;
            return method;
#else

			return type.GetMethod( "ToString", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
			                       null, Helpers.EmptyTypes, null );
#endif
		}

		ParseableSerializer( MethodInfo parse ) { this.parse = parse; }
		public Type ExpectedType => parse.DeclaringType;

		Boolean IProtoSerializer.RequiresOldValue => false;
		Boolean IProtoSerializer.ReturnsValue => true;

#if !FEAT_IKVM
		public Object Read( Object value, ProtoReader source )
		{
			Helpers.DebugAssert( value == null ); // since replaces
			return parse.Invoke( null, new Object[] { source.ReadString() } );
		}

		public void Write( Object value, ProtoWriter dest ) { ProtoWriter.WriteString( value.ToString(), dest ); }
#endif

#if FEAT_COMPILER
        void IProtoSerializer.EmitWrite(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            Type type = ExpectedType;
            if (type.IsValueType)
            {   // note that for structs, we've already asserted that a custom ToString
                // exists; no need to handle the box/callvirt scenario

                // force it to a variable if needed, so we can take the address
                using (Compiler.Local loc = ctx.GetLocalWithValue(type, valueFrom))
                {
                    ctx.LoadAddress(loc, type);
                    ctx.EmitCall(GetCustomToString(type));
                }
            }
            else {
                ctx.EmitCall(ctx.MapType(typeof(object)).GetMethod("ToString"));
            }
            ctx.EmitBasicWrite("WriteString", valueFrom);
        }
        void IProtoSerializer.EmitRead(Compiler.CompilerContext ctx, Compiler.Local valueFrom)
        {
            ctx.EmitBasicRead("ReadString", ctx.MapType(typeof(string)));
            ctx.EmitCall(parse);
        }
#endif
	}
}
#endif
