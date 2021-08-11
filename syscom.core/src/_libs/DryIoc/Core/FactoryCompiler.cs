using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace libs.DryIoc
{
	public static partial class FactoryCompiler
	{
		static partial void CompileToDelegate( Expression expression, ref FactoryDelegate result )
		{
			var method = new DynamicMethod( String.Empty,
			                                typeof( Object ), _factoryDelegateArgTypes,
			                                typeof( Container ).Module, true );

			var il = method.GetILGenerator();

			var emitted = EmittingVisitor.TryVisit( expression, il );
			if ( emitted )
			{
				il.Emit( OpCodes.Ret );
				result = (FactoryDelegate) method.CreateDelegate( typeof( FactoryDelegate ) );
			}
		}

		static readonly Type[] _factoryDelegateArgTypes = { typeof( Object[] ), typeof( IResolverContext ), typeof( IScope ) };

		/// <summary>Supports emitting of selected expressions, e.g. lambda are not supported yet.
		/// When emitter find not supported expression it will return false from <see cref="TryVisit"/>, so I could fallback
		/// to normal and slow Expression.Compile.</summary>
		static class EmittingVisitor
		{
			public static Boolean TryVisit( Expression expr, ILGenerator il )
			{
				switch ( expr.NodeType )
				{
					case ExpressionType.Convert:
						return VisitConvert( (UnaryExpression) expr, il );
					case ExpressionType.ArrayIndex:
						return VisitArrayIndex( (BinaryExpression) expr, il );
					case ExpressionType.Constant:
						return VisitConstant( (ConstantExpression) expr, il );
					case ExpressionType.Parameter:
						return VisitFactoryDelegateParameters( expr, il );
					case ExpressionType.New:
						return VisitNew( (NewExpression) expr, il );
					case ExpressionType.NewArrayInit:
						return VisitNewArray( (NewArrayExpression) expr, il );
					case ExpressionType.MemberInit:
						return VisitMemberInit( (MemberInitExpression) expr, il );
					case ExpressionType.Call:
						return VisitMethodCall( (MethodCallExpression) expr, il );
					case ExpressionType.MemberAccess:
						return VisitMemberAccess( (MemberExpression) expr, il );
					default:
						// Not supported yet: nested lambdas (Invoke)
						return false;
				}
			}

			static Boolean VisitFactoryDelegateParameters( Expression expr, ILGenerator il )
			{
				var paramExpr = (ParameterExpression) expr;
				if ( paramExpr == Container.StateParamExpr )
					il.Emit( OpCodes.Ldarg_0 );
				else if ( paramExpr == Container.ResolverContextParamExpr )
					il.Emit( OpCodes.Ldarg_1 );
				else if ( paramExpr == Container.ResolutionScopeParamExpr )
					il.Emit( OpCodes.Ldarg_2 );
				return true;
			}

			static Boolean VisitBinary( BinaryExpression b, ILGenerator il )
			{
				var ok = TryVisit( b.Left, il );
				if ( ok )
					ok = TryVisit( b.Right, il );
				// skips TryVisit(b.Conversion) for NodeType.Coalesce (?? operation)
				return ok;
			}

			static Boolean VisitExpressionList( IList<Expression> eList, ILGenerator state )
			{
				var ok = true;
				for ( Int32 i = 0, n = eList.Count; i < n && ok; i++ )
					ok = TryVisit( eList[i], state );
				return ok;
			}

			static Boolean VisitConvert( UnaryExpression node, ILGenerator il )
			{
				var ok = TryVisit( node.Operand, il );
				if ( ok )
				{
					var convertTargetType = node.Type;
					if ( convertTargetType == typeof( Object ) ) // not supported, probably required for converting ValueType
						return false;
					il.Emit( OpCodes.Castclass, convertTargetType );
				}

				return ok;
			}

			static Boolean VisitConstant( ConstantExpression node, ILGenerator il )
			{
				var value = node.Value;
				if ( value == null )
					il.Emit( OpCodes.Ldnull );
				else if ( value is Int32 || value.GetType().IsEnum() )
					EmitLoadConstantInt( il, (Int32) value );
				else if ( value is String )
					il.Emit( OpCodes.Ldstr, (String) value );
				else
					return false;
				return true;
			}

			static Boolean VisitNew( NewExpression node, ILGenerator il )
			{
				var ok = VisitExpressionList( node.Arguments, il );
				if ( ok )
					il.Emit( OpCodes.Newobj, node.Constructor );
				return ok;
			}

			static Boolean VisitNewArray( NewArrayExpression node, ILGenerator il )
			{
				var elems = node.Expressions;
				var arrType = node.Type;
				var elemType = arrType.GetArrayElementTypeOrNull();
				var isElemOfValueType = elemType.IsValueType();

				var arrVar = il.DeclareLocal( arrType );

				EmitLoadConstantInt( il, elems.Count );
				il.Emit( OpCodes.Newarr, elemType );
				il.Emit( OpCodes.Stloc, arrVar );

				var ok = true;
				for ( Int32 i = 0, n = elems.Count; i < n && ok; i++ )
				{
					il.Emit( OpCodes.Ldloc, arrVar );
					EmitLoadConstantInt( il, i );

					// loading element address for later copying of value into it.
					if ( isElemOfValueType )
						il.Emit( OpCodes.Ldelema, elemType );

					ok = TryVisit( elems[i], il );
					if ( ok )
					{
						if ( isElemOfValueType )
							il.Emit( OpCodes.Stobj, elemType ); // store element of value type by array element address
						else
							il.Emit( OpCodes.Stelem_Ref );
					}
				}

				il.Emit( OpCodes.Ldloc, arrVar );
				return ok;
			}

			static Boolean VisitArrayIndex( BinaryExpression node, ILGenerator il )
			{
				var ok = VisitBinary( node, il );
				if ( ok )
					il.Emit( OpCodes.Ldelem_Ref );
				return ok;
			}

			static Boolean VisitMemberInit( MemberInitExpression mi, ILGenerator il )
			{
				var ok = VisitNew( mi.NewExpression, il );
				if ( !ok ) return false;

				var obj = il.DeclareLocal( mi.Type );
				il.Emit( OpCodes.Stloc, obj );

				var bindings = mi.Bindings;
				for ( Int32 i = 0, n = bindings.Count; i < n; i++ )
				{
					var binding = bindings[i];
					if ( binding.BindingType != MemberBindingType.Assignment )
						return false;
					il.Emit( OpCodes.Ldloc, obj );

					ok = TryVisit( ( (MemberAssignment) binding ).Expression, il );
					if ( !ok ) return false;

					var prop = binding.Member as PropertyInfo;
					if ( prop != null )
					{
						var setMethod = prop.GetSetMethodOrNull();
						if ( setMethod == null )
							return false;
						EmitMethodCall( setMethod, il );
					}
					else
					{
						var field = binding.Member as FieldInfo;
						if ( field == null )
							return false;
						il.Emit( OpCodes.Stfld, field );
					}
				}

				il.Emit( OpCodes.Ldloc, obj );
				return true;
			}

			static Boolean VisitMethodCall( MethodCallExpression expr, ILGenerator il )
			{
				var ok = true;
				if ( expr.Object != null )
					ok = TryVisit( expr.Object, il );

				if ( ok && expr.Arguments.Count != 0 )
					ok = VisitExpressionList( expr.Arguments, il );

				if ( ok )
					EmitMethodCall( expr.Method, il );

				return ok;
			}

			static Boolean VisitMemberAccess( MemberExpression expr, ILGenerator il )
			{
				if ( expr.Expression != null )
				{
					var ok = TryVisit( expr.Expression, il );
					if ( !ok ) return false;
				}

				var field = expr.Member as FieldInfo;
				if ( field != null )
				{
					il.Emit( field.IsStatic() ? OpCodes.Ldsfld : OpCodes.Ldfld, field );
					return true;
				}

				var property = expr.Member as PropertyInfo;
				if ( property != null )
				{
					var getMethod = property.GetGetMethod();
					if ( getMethod == null )
						return false;
					EmitMethodCall( getMethod, il );
				}

				return true;
			}

			static void EmitMethodCall( MethodInfo method, ILGenerator il ) { il.Emit( method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, method ); }

			static void EmitLoadConstantInt( ILGenerator il, Int32 i )
			{
				switch ( i )
				{
					case 0:
						il.Emit( OpCodes.Ldc_I4_0 );
						break;
					case 1:
						il.Emit( OpCodes.Ldc_I4_1 );
						break;
					case 2:
						il.Emit( OpCodes.Ldc_I4_2 );
						break;
					case 3:
						il.Emit( OpCodes.Ldc_I4_3 );
						break;
					case 4:
						il.Emit( OpCodes.Ldc_I4_4 );
						break;
					case 5:
						il.Emit( OpCodes.Ldc_I4_5 );
						break;
					case 6:
						il.Emit( OpCodes.Ldc_I4_6 );
						break;
					case 7:
						il.Emit( OpCodes.Ldc_I4_7 );
						break;
					case 8:
						il.Emit( OpCodes.Ldc_I4_8 );
						break;
					default:
						il.Emit( OpCodes.Ldc_I4, i );
						break;
				}
			}
		}
	}
}
