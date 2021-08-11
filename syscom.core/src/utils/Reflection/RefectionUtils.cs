using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using syscom.Collections;


namespace syscom.Reflection
{
	public static partial class ReflectionUtils
	{
		public static Delegate BuildDynamicDelegate( MethodInfo methodInfo )
		{
			if ( methodInfo == null )
				throw new ArgumentNullException( "methodInfo" );

			var paramExpressions = methodInfo.GetParameters().Select( ( p, i ) =>
			{
				var name = "param" + ( i + 1 ).ToString( CultureInfo.InvariantCulture );
				return Expression.Parameter( p.ParameterType, name );
			} ).ToList();

			MethodCallExpression callExpression;
			if ( methodInfo.IsStatic )
			{
				//Call(params....)
				callExpression = Expression.Call( methodInfo, paramExpressions );
			}
			else
			{
				var instanceExpression = Expression.Parameter( typeof( Object ), "instance" );
				//((T)instance)
				var castExpression = Expression.Convert( instanceExpression, methodInfo.ReflectedType );
				//((T)instance).Call(params)
				callExpression = Expression.Call( castExpression, methodInfo, paramExpressions );
				paramExpressions.Insert( 0, instanceExpression );
			}

			var lambdaExpression = Expression.Lambda( callExpression, paramExpressions );
			return lambdaExpression.Compile();
		}

		//使用
		public static Action<Object> BuildAction( MethodInfo methodInfo ) { return (Action<Object>) BuildDynamicDelegate( methodInfo ); }

		public static Action<Object, T1> BuildAction<T1>( MethodInfo methodInfo ) { return (Action<Object, T1>) BuildDynamicDelegate( methodInfo ); }

		public static Action<Object, TProperty> BuildSetPropertyAction<TProperty>( PropertyInfo propertyInfo )
		{
			var instanceParam = Expression.Parameter( typeof( Object ), "instance" );
			var valueParam = Expression.Parameter( typeof( TProperty ), "value" );
			//((T)instance)
			var castExpression = Expression.Convert( instanceParam, propertyInfo.ReflectedType );
			//((T)instance).Property
			var propertyProperty = Expression.Property( castExpression, propertyInfo );
			//((T)instance).Property = value
			var assignExpression = Expression.Assign( propertyProperty, valueParam );
			var lambdaExpression = Expression.Lambda<Action<Object, TProperty>>( assignExpression, instanceParam, valueParam );
			return lambdaExpression.Compile();
		}
	}


	public static class ReflectionExtensions
	{
		internal static PropertyInfo GetPropertyInfo( this Type type, String propertyName )
		{
			var propertyInfo = type.GetPropertyInfoBy( propertyName );
			if ( propertyInfo == null ) throw new ArgumentNullException( "在Type[ " + type.FullName + " ]中無法取得[ " + propertyName + " ]" );
			return propertyInfo;
		}

		public static void DynamicSetByCustoms<TType>( this Object instance, String propertyName, TType value )
		{
			var type = instance.GetType();
			var info = type.GetPropertyInfo( propertyName );

			SingletonCacherBy<PropertyInfo, Action<Object, TType>>.DataGenerator = ReflectionUtils.BuildSetPropertyAction<TType>;

			var action = SingletonCacherBy<PropertyInfo, Action<Object, TType>>.GetBy( info );
			//Action<Object, TType>? action = null;
			//dic.TryGetValue( info, out action );

			//var action = ReflectionUtils.BuildSetPropertyAction<TType>( info );
			action( instance, value );
		}
	}
}
