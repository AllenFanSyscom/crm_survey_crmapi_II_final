using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using syscom;
using syscom.Collections;

namespace System.Reflection
{
	public static partial class ReflectionExtensions
	{
		/// <summary>
		/// 將該Type的各別Property與Property中的TAttribute加入Dictionary中作對應
		/// </summary>
		public static Dictionary<PropertyInfo, TAttribute> GetHasAttributePropertyDictionarysBy<TAttribute>( this Type type ) where TAttribute : Attribute
		{
			if ( type == null ) throw Err.Extension( "輸入不可為Null" );
			SingletonCacherBy<Type, Dictionary<PropertyInfo, TAttribute>>.DataGenerator = ( t ) =>
			{
				var dic = new Dictionary<PropertyInfo, TAttribute>();
				var propertries = t.GetPropertyInfos().ToList();

				foreach ( var p in propertries )
				{
					var attrs = p.GetCustomAttributes<TAttribute>( false );
					var attr = attrs.FirstOrDefault();
					if ( attr != null ) dic.Add( p, attr );
				}

				return dic;
			};

			return SingletonCacherBy<Type, Dictionary<PropertyInfo, TAttribute>>.GetBy( type );
		}


		//=========================================================================================================== By Type
		/// <summary>取得該型別所有非繼承的Attributes</summary>
		public static List<Attribute> GetAttributes( this Type type )
		{
			if ( type == null ) throw Err.Extension( "輸入不可為Null" );
			SingletonCacherBy<Type, List<Attribute>>.DataGenerator = ( t ) => t.GetCustomAttributes( false ).Select( c => (Attribute) c ).ToList();

			return SingletonCacherBy<Type, List<Attribute>>.GetBy( type );
		}

		/// <summary>取得該Type是否有指定型別的Attribute，則否回傳null</summary>
		public static TAttribute GetAttributeOrNullFor<TAttribute>( this Type currentType ) where TAttribute : Attribute
		{
			if ( currentType == null ) return null;

			var targetType = typeof( TAttribute );
			var attrs = currentType.GetAttributes();
			return attrs.FirstOrDefault( a => a.GetType() == targetType ) as TAttribute;
		}


		/// <summary>取得該Type是否有繼承自指定型別的Attribute，則否回傳null</summary>
		public static TAttribute GetInheritAttributeOrNullFor<TAttribute>( this Type currentType ) where TAttribute : Attribute
		{
			if ( currentType == null ) return null;

			var attrs = currentType.GetAttributes();
			return attrs.OfType<TAttribute>().FirstOrDefault();
		}

		/// <summary>
		/// 取得該Type是否有指定型別的Attribute，如果為null或有傳入錯誤處理的Func，則會丟出處理後的Exception
		/// </summary>
		public static TAttribute GetAttributeOrException<TAttribute>( this Type currentType, Func<Exception> makeEx ) where TAttribute : Attribute
		{
			var attr = currentType.GetAttributeOrNullFor<TAttribute>();
			if ( attr == null && makeEx != null )
			{
				var ex = makeEx();
				if ( ex != null ) throw ex;
			}

			return attr;
		}


		//=========================================================================================================== By PropertyInfo

		/// <summary>
		/// 取得該Property所有非繼承的Attributes
		/// </summary>
		public static List<Attribute> GetAttributes( this PropertyInfo propertyInfo )
		{
			SingletonCacherBy<PropertyInfo, List<Attribute>>.DataGenerator = ( p ) => p.GetCustomAttributes( false ).Select( c => (Attribute) c ).ToList();

			return SingletonCacherBy<PropertyInfo, List<Attribute>>.GetBy( propertyInfo );
		}

		/// <summary>
		/// 取得該Property是否有指定型別的Attribute，則否回傳null，如果Property為null就丟出處理後的Exception
		/// </summary>
		public static TAttribute GetAttributeOrNullFor<TAttribute>( this PropertyInfo propertyInfo ) where TAttribute : Attribute
		{
			if ( propertyInfo == null ) throw Err.Extension( "PropertyInfo can't be Null." );

			var attrs = propertyInfo.GetAttributes();
			var attr = attrs.FirstOrDefault( a => a.GetType() == typeof( TAttribute ) ) as TAttribute;
			return attr;
		}

		/// <summary>
		/// 取得該Property是否有指定型別的Attribute，如果為null就丟出處理後的Exception
		/// </summary>
		public static TAttribute GetAttributeOrException<TAttribute>( this PropertyInfo propertyInfo ) where TAttribute : Attribute
		{
			var attr = propertyInfo.GetAttributeOrNullFor<TAttribute>();
			if ( attr == null ) throw Err.Extension( "PropertyInfo [ " + propertyInfo.Name + " ] not taged " + typeof( TAttribute ).FullName + " Attribute" );

			return attr;
		}
	}
}