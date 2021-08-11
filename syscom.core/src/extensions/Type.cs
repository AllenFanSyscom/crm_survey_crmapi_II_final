using syscom.logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using syscom;

namespace System
{
	public static partial class TypeExtensions
	{
		public static TDst GetOr<TSrc, TDst>( this TSrc current, Func<TSrc, TDst> onConvert, TDst defaultValue )
		{
			try
			{
				if ( current == null ) return defaultValue;

				return onConvert( current );
			}
			catch { return defaultValue; }
		}


		/// <summary>試著使用Converter轉換，若失敗傳回該型別預設值 (參考型別則為null)</summary>
		public static TResult ConvertOrDefault<TResult>( this Object current )
		{
			if ( current == null ) return default( TResult );
			var targetType = typeof( TResult );
			var currentType = current.GetType();
			try
			{
				if ( current.GetType() == typeof( TResult ) ) return (TResult) current;
				if ( targetType.IsNullableType() )
					return (TResult) Activator.CreateInstance( targetType, current );
				else if ( targetType.IsEnum ) return EnumUtils.TryParseFromValueBy<TResult>( current );

				var converter = TypeDescriptor.GetConverter( typeof( TResult ) );
				if ( !converter.CanConvertFrom( currentType ) ) throw new InvalidCastException( "無法將[ " + current + " ](" + currentType + ") 轉型為型別[" + targetType + "]" );
				return (TResult) converter.ConvertFrom( current );
			}
			catch { return default( TResult ); }
		}

		/// <summary>試著使用Converter轉換，若轉換失敗時，則回傳傳入參數的defaultValue</summary>
		public static TResult ConvertOrDefault<TResult>( this Object current, TResult defaultValue )
		{
			if ( current == null ) return defaultValue;
			var targetType = typeof( TResult );
			var currentType = current.GetType();
			try
			{
				if ( current.GetType() == typeof( TResult ) ) return (TResult) current;
				if ( targetType.IsNullableType() )
					return (TResult) Activator.CreateInstance( targetType, current );
				else if ( targetType.IsEnum ) return EnumUtils.TryParseFromValueBy<TResult>( current );

				var converter = TypeDescriptor.GetConverter( typeof( TResult ) );
				if ( !converter.CanConvertFrom( currentType ) ) throw new InvalidCastException( "無法將[ " + current + " ](" + currentType + ") 轉型為型別[" + targetType + "]" );
				return (TResult) converter.ConvertFrom( current );
			}
			catch { return defaultValue; }
		}


		/// <summary>取得該型別預設值 (只有實值類型回傳預設值,其餘回傳null)</summary>
		public static Object GetDefaultValue( this Type type ) { return type.IsValueType ? Activator.CreateInstance( type ) : null; }

		/// <summary>判斷型別是否為nullable型別</summary>
		public static Boolean IsNullableType( this Type type ) { return type.IsGenericType && type.GetGenericTypeDefinition() == typeof( Nullable<> ); }

		internal static Boolean IsAnonymousType( Type type )
		{
			if ( type == null ) throw new ArgumentNullException( "type" );

			return Attribute.IsDefined( type, typeof( CompilerGeneratedAttribute ), false )
			       && type.IsGenericType && type.Name.Contains( "AnonymousType" )
			       && ( type.Name.StartsWith( "<>", StringComparison.OrdinalIgnoreCase ) || type.Name.StartsWith( "VB$", StringComparison.OrdinalIgnoreCase ) )
			       && ( type.Attributes & TypeAttributes.NotPublic ) == TypeAttributes.NotPublic;
		}

		///// <summary>試著使用TypeConverter轉換，若失敗使用Activator做CreateInstance</summary>
		//public static Object TryConvertOrDefault( this Object current, Type targetType )
		//{
		//	try
		//	{
		//		var currentType = current.GetType();
		//		if ( currentType == targetType ) return current;
		//		return Convert.ChangeType( current, targetType );
		//	}
		//	catch ( MissingMethodException )
		//	{
		//		//razgriz mark reason : bcoz roughly into this scope it's string can't create.
		//		//if( currentType.Equals( typeof( String ) ) )
		//		return String.Empty;
		//	}
		//	catch
		//	{
		//		return Activator.CreateInstance( targetType );
		//	}
		//}

		static readonly Type TYPE_IList = typeof( IList<> );
		static readonly Type TYPE_ICollection = typeof( ICollection<> );

		public static Boolean IsGenericList( this Type type )
		{
			if ( type == null ) throw new ArgumentNullException( "type" );

			foreach ( Type @interface in type.GetInterfaces() )
			{
				if ( @interface.IsGenericType )
				{
					var gtype = @interface.GetGenericTypeDefinition();
					if ( gtype == TYPE_IList || gtype == TYPE_ICollection ) return true;
				}
			}

			return false;
		}
	}
}
