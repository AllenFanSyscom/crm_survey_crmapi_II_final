using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using syscom;

namespace System
{
	public static class EnumExtensions
	{
		public static String GetDescription( this Enum currentEnum )
		{
			var type = currentEnum.GetType();
			var memInfo = type.GetMember( currentEnum.ToString() );

			if ( memInfo.Length > 0 )
			{
				var attr = memInfo[0].GetCustomAttributes( typeof( DescriptionAttribute ), false ).FirstOrDefault() as DescriptionAttribute;
				if ( attr != null ) return attr.Description;
			}

			return String.Empty;
		}


		/// <summary>判斷目前的Enum值中, 是否包含指定的Enum值</summary>
		public static Boolean HasFlagBy( this Enum currentEnum, Enum value )
		{
			if ( currentEnum == null ) return false;
			if ( value == null ) throw new ArgumentNullException( "value" );

			// Not as good as the .NET 4 version of this function, but should be good enough
			if ( !Enum.IsDefined( currentEnum.GetType(), value ) ) throw new ArgumentException( $"Enumeration type mismatch.  The flag is of type '{value.GetType()}', was expecting '{currentEnum.GetType()}'." );

			var num = Convert.ToUInt64( value );
			return ( Convert.ToUInt64( currentEnum ) & num ) == num;
		}

		/// <summary>
		/// 判斷該列舉值是否包含values 內所有列舉。
		/// </summary>
		/// <param name="currentEnum">目標列舉。</param>
		/// <param name="enums">允許數個相同列舉型別。</param>
		public static Boolean Contains( this Enum currentEnum, params Enum[] enums )
		{
			for ( var idx = 0; idx < enums.Length; idx++ )
			{
				var item = enums[idx];
				if ( !currentEnum.HasFlag( item ) ) return false;
			}

			return true;
		}

		/// <summary>嘗試從字串值解析取得目前Enum型別, 可選參數defaultType若有指定, 則會在從字串值轉換失敗時, 帶回預設值</summary>
		/// <typeparam name="TEnum">指定的Enum型別</typeparam>
		/// <param name="original">原始的Enum值</param>
		/// <param name="value">準備解析的值</param>
		/// <param name="defaultType">若轉換失敗時帶回之預設值</param>
		public static TEnum TryResolveFromValueBy<TEnum>( this TEnum original, Object value, TEnum? defaultType = null ) where TEnum : struct
		{
			var temp = default( TEnum );
			EnumUtils.TryParseFromValueBy( ref temp, value, defaultType );
			return temp;
		}


		/// <summary>
		/// 指定列舉型別，取得該列舉值所代表的所有列舉。
		/// ex. a = TestEnum.A | TestEnum.B | TestEnum.E
		/// items = a.GetFlags&lt;TestEnum&gt;()
		/// items包含TestEnum.A、TestEnum.B、TestEnum.E
		/// </summary>
		public static IEnumerable<TEnum> SplitEnumFlags<TEnum>( this Enum value ) { return value.SplitEnumFlags().Cast<TEnum>(); }

		/// <summary>
		/// 取得該列舉值所代表的所有列舉。
		/// ex. a = TestEnum.A | TestEnum.B | TestEnum.E
		/// items = a.GetFlags&lt;TestEnum&gt;()
		/// items包含TestEnum.A、TestEnum.B、TestEnum.E
		/// </summary>
		public static IEnumerable<Enum> SplitEnumFlags( this Enum value )
		{
			var enumValues = Enum.GetValues( value.GetType() ).Cast<Enum>().ToArray();
			return InternalSplitFlagsBy( value, enumValues );
		}

		static IEnumerable<Enum> InternalSplitFlagsBy( Enum value, Enum[] values )
		{
			var bits = Convert.ToUInt64( value );
			var results = new List<Enum>();
			for ( var i = values.Length - 1; i >= 0; i-- )
			{
				var mask = Convert.ToUInt64( values[i] );
				if ( ( bits & mask ) != mask ) continue;

				results.Add( values[i] );
				bits -= mask;
			}

			if ( bits != 0L ) return Enumerable.Empty<Enum>();
			if ( Convert.ToUInt64( value ) != 0L ) return results.Reverse<Enum>();
			if ( bits == Convert.ToUInt64( value ) && values.Length > 0 && Convert.ToUInt64( values[0] ) == 0L ) return values;
			return Enumerable.Empty<Enum>();
		}


		/// <summary>在原有的Enum為基礎下, 新增新的Enum值 (合併)</summary>
		public static T AppendFlagEnumBy<T>( this Enum currentEnum, T targetEnum )
		{
			try
			{
				var type = Enum.GetUnderlyingType( currentEnum.GetType() );
				var original = Convert.ToInt64( currentEnum );
				var right = Convert.ToInt64( targetEnum );
				var final = (Object) ( original | right );

				return (T) Convert.ChangeType( final, type );
				//return (T)final;
			}
			catch ( Exception ex )
			{
				throw new ArgumentException( $"Could not append flag value {targetEnum} to enum {typeof( T ).Name}", ex );
			}
		}

		/// <summary>在原有的Enum移除指定的Enum值</summary>
		public static T RemoveFlagEnumBy<T>( this Enum currentEnum, T targetEnum )
		{
			try
			{
				var type = Enum.GetUnderlyingType( currentEnum.GetType() );
				var original = Convert.ToInt64( currentEnum );
				var right = Convert.ToInt64( targetEnum );
				var final = (Object) ( original & ~ right );

				return (T) Convert.ChangeType( final, type );
				//return (T)final;
			}
			catch ( Exception ex )
			{
				throw new ArgumentException( $"Could not append flag value {targetEnum} to enum {typeof( T ).Name}", ex );
			}
		}

		public static T SetFlagEnumBy<T>( this Enum type, T enumFlag, Boolean addMode = true ) { return addMode ? type.AppendFlagEnumBy( enumFlag ) : type.RemoveFlagEnumBy( enumFlag ); }


		public static Boolean IsDefined( this Enum value ) { return Enum.IsDefined( value.GetType(), value ); }

		public static Boolean TypeHasFlagsAttribute( this Enum value ) { return value.GetType().GetCustomAttributes( typeof( FlagsAttribute ), false ).Length > 0; }

		public static Boolean IsValidEnumValue( this Enum value ) { return value.TypeHasFlagsAttribute() ? IsFlagsEnumDefined( value ) : value.IsDefined(); }

		static Boolean IsFlagsEnumDefined( Enum value )
		{
			// modeled after Enum's InternalFlagsFormat
			var underlyingenumtype = Enum.GetUnderlyingType( value.GetType() );
			switch ( Type.GetTypeCode( underlyingenumtype ) )
			{
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.Single:
				{
					var obj = Activator.CreateInstance( underlyingenumtype );
					var svalue = Convert.ToInt64( value );
					if ( svalue < 0 ) throw new ArgumentException( $"Can't process negative {svalue} as {value.GetType().Name} enum with flags" );
				}
					break;
				default:
					break;
			}

			var flagsset = Convert.ToUInt64( value );
			var values = Enum.GetValues( value.GetType() ); //.Cast<ulong />().ToArray<ulong />();
			var flagno = values.Length - 1;
			var initialflags = flagsset;
			var flag = 0UL;


			while ( flagno >= 0 )
			{
				flag = Convert.ToUInt64( values.GetValue( flagno ) );
				if ( flagno == 0 && flag == 0 ) break;

				//if the flags set contain this flag
				if ( ( flagsset & flag ) == flag )
				{
					//unset this flag
					flagsset -= flag;
					if ( flagsset == 0 ) return true;
				}

				flagno--;
			}

			if ( flagsset != 0 ) return false;

			return initialflags != 0 || flag == 0;
		}
	}
}
