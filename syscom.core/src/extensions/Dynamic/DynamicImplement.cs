using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using syscom;

namespace syscom.Dynamic
{
	//public static class DynamicImplementExtensions
	//{
	//	internal static Type TYPE_DynamicImplement = typeof( IDynamicType );
	//	internal static Type TYPE_DynamicInhert = typeof( IDynamicTypeInhertFrom<> );
	//	//internal static Type TYPE_DynamicBase = typeof( IDynamicTypeBaseClass );
	//	internal static Type TYPE_DynamicBaseExtend = typeof( IDynamicTypeRuntimeExtension );
	//	internal static List<Type> TYPES = new List<Type> {  TYPE_DynamicBaseExtend, TYPE_DynamicImplement, TYPE_DynamicInhert };

	//	/// <summary>用來判斷該型別是否為繼承自IDynamicImplement型別</summary>
	//	public static Boolean IsDynamicImplementType( this Type type )
	//	{
	//		return
	//			type.IsInterface &&
	//			!TYPES.Contains( type ) &&
	//			TYPE_DynamicImplement.IsAssignableFrom( type );
	//	}

	//	/// <summary>用來判斷該型別是否為泛型型別，並含有繼承自IDynamicImplement之子型別</summary>
	//	public static Boolean IsDynamicImplementTypeInGenericType( this Type type )
	//	{
	//		return
	//			type.IsGenericType &&
	//			type.GenericTypeArguments[0].IsDynamicImplementType();
	//	}

	//	/// <summary>過濾掉非DynamicImplement型別</summary>
	//	public static IList<Type> FilterDynamicImplement( this IEnumerable<Type> types )
	//	{
	//		return types
	//			.Where
	//			( t =>
	//				t.IsDynamicImplementType()
	//			)
	//			.ToList();
	//	}

	//	/// <summary>過濾掉非DynamicImplement型別, 可選參數決定是否包含泛型型別</summary>
	//	public static IList<PropertyInfo> FilterDynamicImplement( this IEnumerable<PropertyInfo> properties, Boolean includeGenericType = false )
	//	{
	//		return properties
	//			.Where
	//			( p =>
	//				(
	//					p.PropertyType.IsDynamicImplementType()
	//				)
	//				||
	//				(
	//					includeGenericType && p.PropertyType.IsDynamicImplementTypeInGenericType()
	//				)
	//			)
	//			.ToList();
	//	}

	//	public static Object CreateDynamicImplementInstance( this Type type, Boolean fillDIProperties = false )
	//	{
	//		return DynamicImplementUtils.CreateDynamicImplementInstanceBy( type, fillDIProperties );
	//	}
	//	public static TInterface CreateDynamicImplementInstance<TInterface>( Boolean fillDIProperties = false ) where TInterface : IDynamicType
	//	{
	//		return DynamicImplementUtils.CreateDynamicImplementInstanceBy<TInterface>( fillDIProperties );
	//	}
	//}
}