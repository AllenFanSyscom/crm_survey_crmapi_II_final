using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using syscom;


namespace System.Reflection
{
	public static class AssemblyExtensions
	{
		static AssemblyExtensions() { }

		/// <summary>排除官方的dlls (GAC, System, Microsoft)</summary>
		public static IEnumerable<Assembly> ExceptNetFrameworkDlls( this IEnumerable<Assembly> assemblies )
		{
			if ( assemblies == null ) throw Err.Extension( "輸入格式錯誤" );
			return assemblies
				.Where
				( a =>
					  !a.GlobalAssemblyCache &&
					  !a.FullName.StartsWith( "System" ) &&
					  !a.FullName.StartsWith( "Microsoft" ) &&
					  !a.FullName.StartsWith( "mscorlib" ) &&
					  !a.FullName.StartsWith( "LINQ" )
				);
		}
	}
}