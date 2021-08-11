using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using syscom.CodeDom;
using syscom.Reflection;

namespace syscom.Dynamic
{
	//public static class DynamicImplementUtils
	//{
	//	public const String MSG_ERROR_PREFIX = "產生動態實作Interface之時, 目標型別->";
	//	public const String MSG_ERROR_ArgMustBeInterface = ", 傳入參數必需為interface";
	//	public const String MSG_ERROR_ArgMustNameStartWithI = ", 傳入的Interface型別必需以 'I' 字母為開頭名稱";

	//	static Object _mutex = new Object();
	//	static Dictionary<String, Type> _DicDynamicRuntimeTypes = new Dictionary<String, Type>();

	//	static Type _InterfaceAllow = typeof( IDynamicType );

	//	static List<Type> _AllHasInterfaceTypes;
	//	static DynamicImplementUtils()
	//	{

	//		_AllHasInterfaceTypes = TypeUtils.GetAllUserTypes
	//			.Where
	//			( c =>
	//				c.IsClass &&
	//				c.GetInterfaces().Length > 0
	//			).ToList();
	//	}


	//	/// <summary>產生動態實作Interface型別的實體(將先進行產生過程再實例化)</summary>
	//	public static TInterface CreateDynamicImplementInstanceBy<TInterface>( Boolean fillDIProperties = false ) where TInterface : IDynamicType
	//	{
	//		var targetInterface = typeof( TInterface );
	//		return (TInterface)CreateDynamicImplementInstanceBy( targetInterface, fillDIProperties );
	//	}

	//	/// <summary>產生動態實作Interface型別的實體(將先進行產生過程再實例化)</summary>
	//	public static Object CreateDynamicImplementInstanceBy( Type targetInterface, Boolean fillDIProperties = false )
	//	{
	//		var instanceType = GetDynamicImplementTypeBy( targetInterface );

	//		var instance = Activator.CreateInstance( instanceType );

	//		if ( fillDIProperties )
	//		{
	//			var properties = instanceType.GetPropertyInfos().FilterDynamicImplement( true );

	//			foreach ( var p in properties )
	//			{
	//				Object? propertyInstance = null;

	//				propertyInstance =
	//					p.PropertyType.IsGenericType ?
	//					Activator.CreateInstance( p.PropertyType ) : CreateDynamicImplementInstanceBy( p.PropertyType, true );

	//				instance.DynamicSetValueBy( p.Name, propertyInstance );
	//			}
	//		}
	//		return instance;
	//	}


	//	/// <summary>
	//	/// 產生實作Interface的動態Class於同Namespace下的DynamicTypes空間裡
	//	/// <para>(若已有實作的則傳回實作之型別)</para>
	//	/// <para>【注意】該Interface必需繼承自IDynamicImplementType, 且為了參考問題, 若該Interface下有Property亦繼承自IDynamicImplementType將會自動產生其實作類別</para>
	//	/// </summary>
	//	public static Type GetDynamicImplementTypeBy<TInterface>() where TInterface : IDynamicType
	//	{
	//		var targetInterface = typeof( TInterface );
	//		return GetDynamicImplementTypeBy( targetInterface );
	//	}

	//	/// <summary>
	//	/// 產生實作Interface的動態Class於同Namespace下的DynamicTypes空間裡
	//	/// <para>(若已有實作的則傳回實作之型別)</para>
	//	/// <para>【注意】該Interface必需繼承自IDynamicImplementType, 且為了參考問題, 若該Interface下有Property亦繼承自IDynamicImplementType將會自動產生其實作類別</para>
	//	/// </summary>
	//	public static Type GetDynamicImplementTypeBy( Type targetInterface )
	//	{
	//		var dynamicType = GetOrGenerateDynamicTypeBy( targetInterface, new[] { targetInterface.Namespace }, Keys.CoreDllNames );

	//		////Note[Raz]: 要注意是否會有雙向參考問題
	//		//#region recursive Members
	//		//var properties = dynamicType.GetPropertyInfos().ExceptTypeFromNetFrameworkDlls();
	//		//var targetMembers = properties
	//		//	.Where
	//		//	( p =>
	//		//		p.PropertyType.IsInterface &&
	//		//		!_AllHasInterfaceTypes.Any( t => p.PropertyType.IsAssignableFrom( t ) )
	//		//	);

	//		////recursive Members
	//		//foreach ( var member in targetMembers )
	//		//{
	//		//	DynamicImplementUtils.GetDynamicImplementTypeBy( member.PropertyType );
	//		//}
	//		//#endregion

	//		return dynamicType;
	//	}


	//	/// <summary>產生一整個Package</summary>
	//	public static List<Type> GetDynamicImplementTypesBy( IEnumerable<Type> targetInterfaceTypes, String outputDllName, String version )
	//	{
	//		var needImplementTypes = new List<Type>();
	//		Type? generateType = null;
	//		foreach ( var sourceType in targetInterfaceTypes )
	//		{
	//			if ( !sourceType.IsInterface ) throw Err.Utility( MSG_ERROR_PREFIX + sourceType.FullName + MSG_ERROR_ArgMustBeInterface );
	//			if ( !sourceType.Name.StartsWith( "I" ) ) throw Err.Utility( MSG_ERROR_PREFIX + sourceType.FullName + MSG_ERROR_ArgMustNameStartWithI );
	//			if ( !_InterfaceAllow.IsAssignableFrom( sourceType ) ) throw Err.Utility( MSG_ERROR_PREFIX + sourceType.FullName + ", 該Interface必需繼承自[ " + _InterfaceAllow + " ]" );

	//			var targetTypeName = sourceType.Name.Substring( 1 );
	//			var dynamicNamespace = sourceType.Namespace + ".DynamicTypes";
	//			var targetFullName = dynamicNamespace + "." + targetTypeName;
	//			if ( _DicDynamicRuntimeTypes.TryGetValue( targetFullName, out generateType ) ) continue;

	//			needImplementTypes.Add( sourceType );
	//		}

	//		var generatedList = new List<Type>( needImplementTypes.Count );
	//		lock ( _mutex )
	//		{
	//			generatedList = CodeDomUtils.CreateInterfaceDynamicRuntimeTypesBy( needImplementTypes, outputDllName, version );
	//			foreach ( var t in generatedList )
	//			{
	//				_DicDynamicRuntimeTypes.Add( t.FullName , t );
	//			}
	//		}
	//		return generatedList;
	//	}

	//	internal static Type GetOrGenerateDynamicTypeBy( Type sourceType, IEnumerable<String> includeNamespaces, IEnumerable<String> includeDlls )
	//	{
	//		Type? generateType = null;

	//		//先試著取得該型別, 如果沒有才繼續產生
	//		if ( _DicDynamicRuntimeTypes.TryGetValue( sourceType.FullName, out generateType ) ) return generateType;

	//		if ( !sourceType.IsInterface ) throw Err.Utility( MSG_ERROR_PREFIX + sourceType.FullName + MSG_ERROR_ArgMustBeInterface );
	//		if ( !sourceType.Name.StartsWith( "I" ) ) throw Err.Utility( MSG_ERROR_PREFIX + sourceType.FullName + MSG_ERROR_ArgMustNameStartWithI );
	//		if ( !_InterfaceAllow.IsAssignableFrom( sourceType ) ) throw Err.Utility( MSG_ERROR_PREFIX + sourceType.FullName + ", 該Interface必需繼承自[ " + _InterfaceAllow + " ]" );

	//		var targetTypeName = sourceType.Name.Substring( 1 );
	//		var dynamicNamespace = sourceType.Namespace + ".DynamicTypes";
	//		var targetFullName = dynamicNamespace + "." + targetTypeName;

	//		if ( _DicDynamicRuntimeTypes.TryGetValue( targetFullName, out generateType ) ) return generateType;

	//		lock ( _mutex )
	//		{
	//			if ( _DicDynamicRuntimeTypes.TryGetValue( targetFullName, out generateType ) ) return generateType;

	//			generateType = CodeDomUtils.CreateInterfaceDynamicRuntimeTypeBy( sourceType, dynamicNamespace, targetTypeName, includeNamespaces, includeDlls );

	//			_DicDynamicRuntimeTypes.Add( targetFullName, generateType );
	//		}
	//		return generateType;
	//	}
	//}
}
