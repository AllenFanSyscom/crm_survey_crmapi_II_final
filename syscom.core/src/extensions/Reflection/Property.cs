using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Reflection;
using syscom;
using syscom.Collections;

namespace System.Reflection
{
	public static partial class ReflectionExtensions
	{
		static ReflectionExtensions()
		{
			SingletonCacherBy<Tuple<Type, Boolean>, List<PropertyInfo>>.DataGenerator = ( args ) =>
			{
				var ps =
					args.Item2
						? args.Item1.GetProperties().Union( args.Item1.GetInterfaces().SelectMany( interfaces => interfaces.GetPropertyInfos() ) )
						: args.Item1.GetProperties();
				return ps.ToList();
			};
		}

		/// <summary>排除Type來自官方的dlls (GAC, System, Microsoft)</summary>
		public static IEnumerable<PropertyInfo> ExceptTypeFromNetFrameworkDlls( this IEnumerable<PropertyInfo> properties )
		{
			return properties.Where
			( p =>
				  !p.PropertyType.Assembly.GlobalAssemblyCache &&
				  !p.PropertyType.Assembly.FullName.StartsWith( "System" ) &&
				  !p.PropertyType.Assembly.FullName.StartsWith( "Microsoft" )
			);
		}

		/// <summary>
		/// 取得該Type的所有Property
		/// </summary>
		public static List<PropertyInfo> GetPropertyInfos( this Type type, Boolean includeInherit = false )
		{
			if ( type == null ) throw Err.Extension( "輸入不可為Null" );

			return SingletonCacherBy<Tuple<Type, Boolean>, List<PropertyInfo>>.GetBy( new Tuple<Type, Boolean>( type, includeInherit ) );
		}

		/// <summary>
		/// 在該Type的Property List中取得指定的Property
		/// </summary>
		public static PropertyInfo GetPropertyInfoBy( this Type type, String propertyName )
		{
			if ( type == null ) throw Err.Extension( "輸入不可為Null" );
			return type.GetPropertyInfos().FirstOrDefault( i => i.Name == propertyName );
		}


		//public static PropertyInfo GetPropertyInfoBy<T>( this T target, String propertyName ) where T : class
		//{
		//    var type = typeof( T );
		//    var properties = type.UnderlyingSystemType.GetPropertyInfos();
		//    return properties.FirstOrDefault( info => info.Name == propertyName );
		//}
	}
}